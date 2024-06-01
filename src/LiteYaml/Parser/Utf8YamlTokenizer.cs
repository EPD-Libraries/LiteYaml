using LiteYaml.Internal;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace LiteYaml.Parser;

class YamlTokenizerException(in Marker marker, string message) : Exception($"{message} at {marker}")
{
}

struct SimpleKeyState
{
    public bool Possible;
    public bool Required;
    public int TokenNumber;
    public Marker Start;
}

public ref struct Utf8YamlTokenizer
{
    [ThreadStatic]
    static InsertionQueue<Token>? _tokensBufferStatic;

    [ThreadStatic]
    static ExpandBuffer<SimpleKeyState>? _simpleKeyBufferStatic;

    [ThreadStatic]
    static ExpandBuffer<int>? _indentsBufferStatic;

    [ThreadStatic]
    static ExpandBuffer<byte>? _lineBreaksBufferStatic;

    public readonly TokenType CurrentTokenType {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _currentToken.Type;
    }

    public readonly Marker CurrentMark {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _mark;
    }

    SequenceReader<byte> _reader;
    Marker _mark;
    Token _currentToken;

    bool _streamStartProduced;
    bool _streamEndProduced;
    byte _currentCode;
    int _indent;
    bool _simpleKeyAllowed;
    int _adjacentValueAllowedAt;
    int _flowLevel;
    int _tokensParsed;
    bool _tokenAvailable;

    readonly InsertionQueue<Token> _tokens;
    readonly ExpandBuffer<SimpleKeyState> _simpleKeyCandidates;
    readonly ExpandBuffer<int> _indents;

    public Utf8YamlTokenizer(ReadOnlySequence<byte> sequence)
    {
        _reader = new SequenceReader<byte>(sequence);
        _mark = new Marker(0, 1, 0);

        _indent = -1;
        _flowLevel = 0;
        _adjacentValueAllowedAt = 0;
        _tokensParsed = 0;
        _simpleKeyAllowed = false;
        _streamStartProduced = false;
        _streamEndProduced = false;
        _tokenAvailable = false;

        _currentToken = default;

        _tokens = _tokensBufferStatic ??= new InsertionQueue<Token>(16);
        _tokens.Clear();

        _simpleKeyCandidates = _simpleKeyBufferStatic ??= new ExpandBuffer<SimpleKeyState>(16);
        _simpleKeyCandidates.Clear();

        _indents = _indentsBufferStatic ??= new ExpandBuffer<int>(16);
        _indents.Clear();

        _reader.TryPeek(out _currentCode);
    }

    public bool Read()
    {
        if (_streamEndProduced) {
            return false;
        }

        if (!_tokenAvailable) {
            ConsumeMoreTokens();
        }

        if (_currentToken.Content is Scalar scalar) {
            ScalarPool.Shared.Return(scalar);
        }
        _currentToken = _tokens.Dequeue();
        _tokenAvailable = false;
        _tokensParsed += 1;

        if (_currentToken.Type == TokenType.StreamEnd) {
            _streamEndProduced = true;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T TakeCurrentTokenContent<T>() where T : ITokenContent
    {
        Token result = _currentToken;
        _currentToken = default;
        return (T)result.Content!;
    }

    internal bool TrySkipUnityStrippedSymbol()
    {
        while (_currentCode == YamlCodes.SPACE) {
            Advance(1);
        }
        if (_reader.IsNext(YamlCodes.UnityStrippedSymbol)) {
            Advance(YamlCodes.UnityStrippedSymbol.Length);
            return true;
        }
        return false;
    }

    void ConsumeMoreTokens()
    {
        while (true) {
            bool needMore = _tokens.Count <= 0;
            if (!needMore) {
                StaleSimpleKeyCandidates();
                for (int i = 0; i < _simpleKeyCandidates.Length; i++) {
                    ref SimpleKeyState simpleKeyState = ref _simpleKeyCandidates[i];
                    if (simpleKeyState.Possible && simpleKeyState.TokenNumber == _tokensParsed) {
                        needMore = true;
                        break;
                    }
                }
            }
            if (!needMore) {
                break;
            }
            ConsumeNextToken();
        }
        _tokenAvailable = true;
    }

    void ConsumeNextToken()
    {
        if (!_streamStartProduced) {
            ConsumeStreamStart();
            return;
        }

        SkipToNextToken();
        StaleSimpleKeyCandidates();
        UnrollIndent(_mark.Col);

        if (_reader.End) {
            ConsumeStreamEnd();
            return;
        }

        if (_mark.Col == 0) {
            switch (_currentCode) {
                case (byte)'%':
                    ConsumeDirective();
                    return;
                case (byte)'-' when _reader.IsNext(YamlCodes.StreamStart) && IsEmptyNext(YamlCodes.StreamStart.Length):
                    ConsumeDocumentIndicator(TokenType.DocumentStart);
                    return;
                case (byte)'.' when _reader.IsNext(YamlCodes.DocStart) && IsEmptyNext(YamlCodes.DocStart.Length):
                    ConsumeDocumentIndicator(TokenType.DocumentEnd);
                    return;
            }
        }

        switch (_currentCode) {
            case YamlCodes.FLOW_SEQUENCE_START:
                ConsumeFlowCollectionStart(TokenType.FlowSequenceStart);
                break;
            case YamlCodes.FLOW_MAP_START:
                ConsumeFlowCollectionStart(TokenType.FlowMappingStart);
                break;
            case YamlCodes.FLOW_SEQUENCE_END:
                ConsumeFlowCollectionEnd(TokenType.FlowSequenceEnd);
                break;
            case YamlCodes.FLOW_MAP_END:
                ConsumeFlowCollectionEnd(TokenType.FlowMappingEnd);
                break;
            case YamlCodes.COMMA:
                ConsumeFlowEntryStart();
                break;
            case YamlCodes.BLOCK_ENTRY_INDENT when !TryPeek(1, out byte nextCode) ||
                                                 YamlCodes.IsEmpty(nextCode):
                ConsumeBlockEntry();
                break;
            case YamlCodes.EXPLICIT_KEY_INDENT when !TryPeek(1, out byte nextCode) ||
                                                  YamlCodes.IsEmpty(nextCode):
                ConsumeComplexKeyStart();
                break;
            case YamlCodes.MAP_VALUE_INDENT
                when (TryPeek(1, out byte nextCode) && YamlCodes.IsEmpty(nextCode)) ||
                     (_flowLevel > 0 && (YamlCodes.IsAnyFlowSymbol(nextCode) || _mark.Position == _adjacentValueAllowedAt)):
                ConsumeValueStart();
                break;
            case YamlCodes.ALIAS:
                ConsumeAnchor(true);
                break;
            case YamlCodes.ANCHOR:
                ConsumeAnchor(false);
                break;
            case YamlCodes.TAG:
                ConsumeTag();
                break;
            case YamlCodes.LITERAL_SCALER_HEADER when _flowLevel == 0:
                ConsumeBlockScaler(true);
                break;
            case YamlCodes.FOLDED_SCALER_HEADER when _flowLevel == 0:
                ConsumeBlockScaler(false);
                break;
            case YamlCodes.SINGLE_QUOTE:
                ConsumeFlowScaler(true);
                break;
            case YamlCodes.DOUBLE_QUOTE:
                ConsumeFlowScaler(false);
                break;
            // Plain Scaler
            case YamlCodes.BLOCK_ENTRY_INDENT when !TryPeek(1, out byte nextCode) ||
                                                 YamlCodes.IsBlank(nextCode):
                ConsumePlainScalar();
                break;
            case YamlCodes.MAP_VALUE_INDENT or YamlCodes.EXPLICIT_KEY_INDENT
                when _flowLevel == 0 &&
                     (!TryPeek(1, out byte nextCode) || YamlCodes.IsBlank(nextCode)):
                ConsumePlainScalar();
                break;
            case (byte)'%' or (byte)'@' or (byte)'`':
                throw new YamlTokenizerException(in _mark, $"Unexpected character: '{_currentCode}'");
            default:
                ConsumePlainScalar();
                break;
        }
    }

    void ConsumeStreamStart()
    {
        _indent = -1;
        _streamStartProduced = true;
        _simpleKeyAllowed = true;
        _tokens.Enqueue(new Token(TokenType.StreamStart));
        _simpleKeyCandidates.Add(new SimpleKeyState());
    }

    void ConsumeStreamEnd()
    {
        // force new line
        if (_mark.Col != 0) {
            _mark.Col = 0;
            _mark.Line += 1;
        }
        UnrollIndent(-1);
        RemoveSimpleKeyCandidate();
        _simpleKeyAllowed = false;
        _tokens.Enqueue(new Token(TokenType.StreamEnd));
    }

    void ConsumeBom()
    {
        if (_reader.IsNext(YamlCodes.Utf8Bom)) {
            bool isStreamStart = _mark.Position == 0;
            Advance(YamlCodes.Utf8Bom.Length);
            // should BOM count towards col?
            _mark.Col = 0;
            if (!isStreamStart) {
                if (CurrentTokenType == TokenType.DocumentEnd ||
                    _tokens.Count > 0 && _tokens.Peek().Type == TokenType.DocumentEnd) {
                    // explicitly ended document, fine
                }
                else if (_reader.IsNext(YamlCodes.DocStart)) {
                    // fine, next is explicit directive-end/doc-start
                }
                else {
                    throw new YamlTokenizerException(CurrentMark, "BOM must be at the beginning of the stream or document.");
                }
            }
        }
    }

    void ConsumeDirective()
    {
        UnrollIndent(-1);
        RemoveSimpleKeyCandidate();
        _simpleKeyAllowed = false;

        Advance(1);

        Scalar name = ScalarPool.Shared.Rent();
        try {
            ConsumeDirectiveName(name);
            if (name.SequenceEqual(YamlCodes.YamlDirectiveName)) {
                ConsumeVersionDirectiveValue();
            }
            else if (name.SequenceEqual(YamlCodes.TagDirectiveName)) {
                ConsumeTagDirectiveValue();
            }
            else {
                // Skip current line
                while (!_reader.End && !YamlCodes.IsLineBreak(_currentCode)) {
                    Advance(1);
                }

                // TODO: This should be error ?
                _tokens.Enqueue(new Token(TokenType.TagDirective));
            }
        }
        finally {
            ScalarPool.Shared.Return(name);
        }

        while (YamlCodes.IsBlank(_currentCode)) {
            Advance(1);
        }

        if (_currentCode == YamlCodes.COMMENT) {
            while (!_reader.End && !YamlCodes.IsLineBreak(_currentCode)) {
                Advance(1);
            }
        }

        if (!_reader.End && !YamlCodes.IsLineBreak(_currentCode)) {
            throw new YamlTokenizerException(CurrentMark,
                "While scanning a directive, did not find expected comment or line break");
        }

        // Eat a line break
        if (YamlCodes.IsLineBreak(_currentCode)) {
            ConsumeLineBreaks();
        }
    }

    void ConsumeDirectiveName(Scalar result)
    {
        while (YamlCodes.IsAlphaNumericDashOrUnderscore(_currentCode)) {
            result.Write(_currentCode);
            Advance(1);
        }

        if (result.Length <= 0) {
            throw new YamlTokenizerException(CurrentMark,
                "While scanning a directive, could not find expected directive name");
        }

        if (!_reader.End && !YamlCodes.IsBlank(_currentCode)) {
            throw new YamlTokenizerException(CurrentMark,
                "While scanning a directive, found unexpected non-alphabetical character");
        }
    }

    void ConsumeVersionDirectiveValue()
    {
        while (YamlCodes.IsBlank(_currentCode)) {
            Advance(1);
        }

        int major = ConsumeVersionDirectiveNumber();

        if (_currentCode != '.') {
            throw new YamlTokenizerException(CurrentMark,
                "while scanning a YAML directive, did not find expected digit or '.' character");
        }

        Advance(1);
        int minor = ConsumeVersionDirectiveNumber();
        _tokens.Enqueue(new Token(TokenType.VersionDirective, new VersionDirective(major, minor)));
    }

    int ConsumeVersionDirectiveNumber()
    {
        int value = 0;
        int length = 0;
        while (YamlCodes.IsNumber(_currentCode)) {
            if (length + 1 > 9) {
                throw new YamlTokenizerException(CurrentMark,
                    "While scanning a YAML directive, found exteremely long version number");
            }

            length++;
            value = value * 10 + YamlCodes.AsHex(_currentCode);
            Advance(1);
        }

        if (length == 0) {
            throw new YamlTokenizerException(CurrentMark,
                "While scanning a YAML directive, did not find expected version number");
        }
        return value;
    }

    void ConsumeTagDirectiveValue()
    {
        Scalar handle = ScalarPool.Shared.Rent();
        Scalar prefix = ScalarPool.Shared.Rent();
        try {
            // Eat whitespaces.
            while (YamlCodes.IsBlank(_currentCode)) {
                Advance(1);
            }

            ConsumeTagHandle(true, handle);

            // Eat whitespaces
            if (!YamlCodes.IsBlank(_currentCode)) {
                throw new YamlTokenizerException(CurrentMark,
                    "While scanning a TAG directive, did not find expected whitespace after tag handle.");
            }
            while (YamlCodes.IsBlank(_currentCode)) {
                Advance(1);
            }

            ConsumeTagPrefix(prefix);

            if (YamlCodes.IsEmpty(_currentCode) || _reader.End) {
                _tokens.Enqueue(new Token(TokenType.TagDirective, new Tag(handle.ToString(), prefix.ToString())));
            }
            else {
                throw new YamlTokenizerException(CurrentMark,
                    "While scanning TAG, did not find expected whitespace or line break");
            }
        }
        finally {
            ScalarPool.Shared.Return(handle);
            ScalarPool.Shared.Return(prefix);
        }
    }

    void ConsumeDocumentIndicator(TokenType tokenType)
    {
        UnrollIndent(-1);
        RemoveSimpleKeyCandidate();
        _simpleKeyAllowed = false;
        Advance(3);
        _tokens.Enqueue(new Token(tokenType));
    }

    void ConsumeFlowCollectionStart(TokenType tokenType)
    {
        // The indicators '[' and '{' may start a simple key.
        SaveSimpleKeyCandidate();
        IncreaseFlowLevel();

        _simpleKeyAllowed = true;

        Advance(1);
        _tokens.Enqueue(new Token(tokenType));
    }

    void ConsumeFlowCollectionEnd(TokenType tokenType)
    {
        RemoveSimpleKeyCandidate();
        DecreaseFlowLevel();

        _simpleKeyAllowed = false;

        Advance(1);
        _tokens.Enqueue(new Token(tokenType));
    }

    void ConsumeFlowEntryStart()
    {
        RemoveSimpleKeyCandidate();
        _simpleKeyAllowed = true;

        Advance(1);
        _tokens.Enqueue(new Token(TokenType.FlowEntryStart));
    }

    void ConsumeBlockEntry()
    {
        if (_flowLevel != 0) {
            throw new YamlTokenizerException(in _mark, "'-' is only valid inside a block");
        }
        // Check if we are allowed to start a new entry.
        if (!_simpleKeyAllowed) {
            throw new YamlTokenizerException(in _mark, "Block sequence entries are not allowed in this context");
        }
        RollIndent(_mark.Col, new Token(TokenType.BlockSequenceStart));
        RemoveSimpleKeyCandidate();
        _simpleKeyAllowed = true;
        Advance(1);
        _tokens.Enqueue(new Token(TokenType.BlockEntryStart));
    }

    void ConsumeComplexKeyStart()
    {
        if (_flowLevel == 0) {
            // Check if we are allowed to start a new key (not necessarily simple).
            if (!_simpleKeyAllowed) {
                throw new YamlTokenizerException(in _mark, "Mapping keys are not allowed in this context");
            }
            RollIndent(_mark.Col, new Token(TokenType.BlockMappingStart));
        }
        RemoveSimpleKeyCandidate();

        _simpleKeyAllowed = _flowLevel == 0;
        Advance(1);
        _tokens.Enqueue(new Token(TokenType.KeyStart));
    }

    void ConsumeValueStart()
    {
        ref SimpleKeyState simpleKey = ref _simpleKeyCandidates[^1];
        if (simpleKey.Possible) {
            // insert simple key
            Token token = new(TokenType.KeyStart);
            _tokens.Insert(simpleKey.TokenNumber - _tokensParsed, token);

            // Add the BLOCK-MAPPING-START token if needed
            RollIndent(simpleKey.Start.Col, new Token(TokenType.BlockMappingStart), simpleKey.TokenNumber);
            ref SimpleKeyState lastKey = ref _simpleKeyCandidates[^1];
            lastKey.Possible = false;
            _simpleKeyAllowed = false;
        }
        else {
            // The ':' indicator follows a complex key.
            if (_flowLevel == 0) {
                if (!_simpleKeyAllowed) {
                    throw new YamlTokenizerException(in _mark, "Mapping values are not allowed in this context");
                }
                RollIndent(_mark.Col, new Token(TokenType.BlockMappingStart));
            }
            _simpleKeyAllowed = _flowLevel == 0;
        }
        Advance(1);
        _tokens.Enqueue(new Token(TokenType.ValueStart));
    }

    void ConsumeAnchor(bool alias)
    {
        SaveSimpleKeyCandidate();
        _simpleKeyAllowed = false;

        Scalar scalar = ScalarPool.Shared.Rent();
        Advance(1);

        while (YamlCodes.IsAlphaNumericDashOrUnderscore(_currentCode)) {
            scalar.Write(_currentCode);
            Advance(1);
        }

        if (scalar.Length <= 0) {
            throw new YamlTokenizerException(_mark,
                "while scanning an anchor or alias, did not find expected alphabetic or numeric character");
        }

        if (!YamlCodes.IsEmpty(_currentCode) &&
            !_reader.End &&
            _currentCode != '?' &&
            _currentCode != ':' &&
            _currentCode != ',' &&
            _currentCode != ']' &&
            _currentCode != '}' &&
            _currentCode != '%' &&
            _currentCode != '@' &&
            _currentCode != '`') {
            throw new YamlTokenizerException(in _mark,
                "while scanning an anchor or alias, did not find expected alphabetic or numeric character");
        }

        _tokens.Enqueue(alias
            ? new Token(TokenType.Alias, scalar)
            : new Token(TokenType.Anchor, scalar));
    }

    void ConsumeTag()
    {
        SaveSimpleKeyCandidate();
        _simpleKeyAllowed = false;

        Scalar handle = ScalarPool.Shared.Rent();
        Scalar suffix = ScalarPool.Shared.Rent();

        try {
            // Tag spec: https://yaml.org/spec/1.2.2/#rule-c-ns-tag-property
            // Check if the tag is in the canonical form (verbatim).
            if (TryPeek(1, out byte nextCode) && nextCode == '<') {
                // Spec: https://yaml.org/spec/1.2.2/#rule-c-verbatim-tag
                // Eat '!<'
                Advance(2);

                while (TryConsumeUriChar(suffix)) { }

                if (suffix.Length <= 0) {
                    throw new YamlTokenizerException(_mark, "While scanning a verbatim tag, did not find valid characters.");
                }
                if (_currentCode != '>') {
                    throw new YamlTokenizerException(_mark, "While scanning a tag, did not find the expected '>'");
                }

                // Eat '>'
                Advance(1);
            }
            else {
                // The tag has either the '!suffix' or the '!handle!suffix'
                ConsumeTagHandle(false, handle);
                if (handle.Length >= 2 && handle.AsSpan()[^1] == '!') {
                    // Spec: https://yaml.org/spec/1.2.2/#rule-c-ns-shorthand-tag
                    // if the handle is at least 2 long and ends with '!':
                    // it's either a Named Tag Handle or if '!!' - a Secondary Tag Handle
                    // There has to be a non-zero length suffix.
                    while (TryConsumeTagChar(suffix)) { }
                    if (suffix.Length <= 0) {
                        throw new YamlTokenizerException(_mark, "While scanning a tag, did not find any tag-shorthand suffix.");
                    }
                }
                else {
                    // Spec: https://yaml.org/spec/1.2.2/#rule-c-ns-shorthand-tag
                    // It's either a Primary Tag Handle with Suffix, or a Non-Specific Tag '!'.
                    // Rewrite the handle into suffix except initial '!'
                    suffix.Write(handle.AsSpan(1, handle.Length - 1));
                    handle.Clear();
                    handle.Write((byte)'!');
                    // Now append any remaining suffix-valid characters to the suffix.
                    while (TryConsumeTagChar(suffix)) { }
                    if (suffix.Length <= 0) {
                        // Spec: https://yaml.org/spec/1.2.2/#rule-c-non-specific-tag
                        // A special case: the '!' tag.  Set the handle to '' and the
                        // suffix to '!'.
                        (handle, suffix) = (suffix, handle);
                    }
                }
            }

            if (YamlCodes.IsEmpty(_currentCode) || _reader.End || YamlCodes.IsAnyFlowSymbol(_currentCode)) {
                // ex 7.2, an empty scalar can follow a secondary tag
                _tokens.Enqueue(new Token(TokenType.Tag, new Tag(handle.ToString(), suffix.ToString())));
            }
            else {
                throw new YamlTokenizerException(_mark,
                    "While scanning a tag, did not find expected whitespace or line break or flow");
            }
        }
        finally {
            ScalarPool.Shared.Return(handle);
            ScalarPool.Shared.Return(suffix);
        }
    }

    void ConsumeTagHandle(bool directive, Scalar buf)
    {
        if (_currentCode != '!') {
            throw new YamlTokenizerException(_mark,
                "While scanning a tag, did not find expected '!'");
        }

        buf.Write(_currentCode);
        Advance(1);

        while (YamlCodes.IsWordChar(_currentCode)) {
            buf.Write(_currentCode);
            Advance(1);
        }

        // Check if the trailing character is '!' and copy it.
        if (_currentCode == '!') {
            buf.Write(_currentCode);
            Advance(1);
        }
        else if (directive) {
#pragma warning disable IDE0079 // Do not suggest removing the warning disable below
#pragma warning disable IDE0302 // Do not suggest collection expression (behaviour is not the same)
            if (!buf.SequenceEqual(stackalloc byte[] { (byte)'!' })) {
#pragma warning restore
                // It's either the '!' tag or not really a tag handle.  If it's a %TAG
                // directive, it's an error.  If it's a tag token, it must be a part of
                // URI.
                throw new YamlTokenizerException(_mark, "While parsing a tag directive, did not find expected '!'");
            }
        }
    }

    void ConsumeTagPrefix(Scalar prefix)
    {
        // Spec: https://yaml.org/spec/1.2.2/#rule-ns-tag-prefix
        if (_currentCode == YamlCodes.TAG) {
            // https://yaml.org/spec/1.2.2/#rule-c-ns-local-tag-prefix
            prefix.Write(_currentCode);
            Advance(1);

            while (TryConsumeUriChar(prefix)) { }
        }
        else if (YamlCodes.IsTagChar(_currentCode)) {
            // https://yaml.org/spec/1.2.2/#rule-ns-global-tag-prefix
            prefix.Write(_currentCode);
            Advance(1);

            while (TryConsumeUriChar(prefix)) { }
        }
        else {
            throw new YamlTokenizerException(_mark, "While parsing a tag, did not find expected tag prefix");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool TryConsumeUriChar(Scalar scalar)
    {
        if (_currentCode == '%') {
            scalar.WriteUnicodeCodepoint(ConsumeUriEscapes());
            return true;
        }
        else if (YamlCodes.IsUriChar(_currentCode)) {
            scalar.Write(_currentCode);
            Advance(1);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool TryConsumeTagChar(Scalar scalar)
    {
        if (_currentCode == '%') {
            scalar.WriteUnicodeCodepoint(ConsumeUriEscapes());
            return true;
        }
        else if (YamlCodes.IsTagChar(_currentCode)) {
            scalar.Write(_currentCode);
            Advance(1);
            return true;
        }
        return false;
    }

    // TODO: Use Uri
    int ConsumeUriEscapes()
    {
        int width = 0;
        int codepoint = 0;

        while (!_reader.End) {
            TryPeek(1, out byte hexcode0);
            TryPeek(2, out byte hexcode1);
            if (_currentCode != '%' || !YamlCodes.IsHex(hexcode0) || !YamlCodes.IsHex(hexcode1)) {
                throw new YamlTokenizerException(_mark, "While parsing a tag, did not find URI escaped octet");
            }

            int octet = (YamlCodes.AsHex(hexcode0) << 4) + YamlCodes.AsHex(hexcode1);
            if (width == 0) {
                width = octet switch {
                    _ when (octet & 0b1000_0000) == 0b0000_0000 => 1,
                    _ when (octet & 0b1110_0000) == 0b1100_0000 => 2,
                    _ when (octet & 0b1111_0000) == 0b1110_0000 => 3,
                    _ when (octet & 0b1111_1000) == 0b1111_0000 => 4,
                    _ => throw new YamlTokenizerException(_mark,
                        "While parsing a tag, found an incorrect leading utf8 octet")
                };
                codepoint = octet;
            }
            else {
                if ((octet & 0xc0) != 0x80) {
                    throw new YamlTokenizerException(_mark,
                        "While parsing a tag, found an incorrect trailing utf8 octet");
                }
                codepoint = (_currentCode << 8) + octet;
            }

            Advance(3);

            width -= 1;
            if (width == 0) {
                break;
            }
        }

        return codepoint;
    }

    void ConsumeBlockScaler(bool literal)
    {
        SaveSimpleKeyCandidate();
        _simpleKeyAllowed = true;

        int chomping = 0;
        int increment = 0;
        int blockIndent = 0;

        bool trailingBlank;
        bool leadingBlank = false;
        LineBreakState leadingBreak = LineBreakState.None;
        Scalar scalar = ScalarPool.Shared.Rent();

        _lineBreaksBufferStatic ??= new ExpandBuffer<byte>(64);
        _lineBreaksBufferStatic.Clear();

        // skip '|' or '>'
        Advance(1);

        if (_currentCode is (byte)'+' or (byte)'-') {
            chomping = _currentCode == (byte)'+' ? 1 : -1;
            Advance(1);
            if (YamlCodes.IsNumber(_currentCode)) {
                if (_currentCode == (byte)'0') {
                    throw new YamlTokenizerException(in _mark,
                        "While scanning a block scalar, found an indentation indicator equal to 0");
                }

                increment = YamlCodes.AsHex(_currentCode);
                Advance(1);
            }
        }
        else if (YamlCodes.IsNumber(_currentCode)) {
            if (_currentCode == (byte)'0') {
                throw new YamlTokenizerException(in _mark,
                    "While scanning a block scalar, found an indentation indicator equal to 0");
            }
            increment = YamlCodes.AsHex(_currentCode);
            Advance(1);

            if (_currentCode is (byte)'+' or (byte)'-') {
                chomping = _currentCode == (byte)'+' ? 1 : -1;
                Advance(1);
            }
        }

        // Eat whitespaces and comments to the end of the line.
        while (YamlCodes.IsBlank(_currentCode)) {
            Advance(1);
        }

        if (_currentCode == YamlCodes.COMMENT) {
            while (!_reader.End && !YamlCodes.IsLineBreak(_currentCode)) {
                Advance(1);
            }
        }

        // Check if we are at the end of the line.
        if (!_reader.End && !YamlCodes.IsLineBreak(_currentCode)) {
            throw new YamlTokenizerException(in _mark,
                "While scanning a block scalar, did not find expected commnet or line break");
        }

        if (YamlCodes.IsLineBreak(_currentCode)) {
            ConsumeLineBreaks();
        }

        if (increment > 0) {
            blockIndent = _indent >= 0 ? _indent + increment : increment;
        }

        // Scan the leading line breaks and determine the indentation level if needed.
        ConsumeBlockScalarBreaks(ref blockIndent, ref _lineBreaksBufferStatic);

        while (_mark.Col == blockIndent) {
            // We are at the beginning of a non-empty line.
            trailingBlank = YamlCodes.IsBlank(_currentCode);
            if (!literal &&
                leadingBreak != LineBreakState.None &&
                !leadingBlank &&
                !trailingBlank) {
                if (_lineBreaksBufferStatic.Length <= 0) {
                    scalar.Write(YamlCodes.SPACE);
                }
            }
            else {
                scalar.Write(leadingBreak);
            }

            scalar.Write(_lineBreaksBufferStatic.AsSpan());
            leadingBlank = YamlCodes.IsBlank(_currentCode);
            _lineBreaksBufferStatic.Clear();

            while (!_reader.End && !YamlCodes.IsLineBreak(_currentCode)) {
                scalar.Write(_currentCode);
                Advance(1);
            }
            // break on EOF
            if (_reader.End) {
                // treat EOF as LF for chomping
                leadingBreak = LineBreakState.Lf;
                break;
            }

            leadingBreak = ConsumeLineBreaks();
            // Eat the following indentation spaces and line breaks.
            ConsumeBlockScalarBreaks(ref blockIndent, ref _lineBreaksBufferStatic);
        }

        // Chomp the tail.
        if (chomping != -1) {
            scalar.Write(leadingBreak);
        }
        if (chomping == 1) {
            scalar.Write(_lineBreaksBufferStatic.AsSpan());
        }

        TokenType tokenType = literal ? TokenType.LiteralScalar : TokenType.FoldedScalar;
        _tokens.Enqueue(new Token(tokenType, scalar));
    }

    void ConsumeBlockScalarBreaks(ref int blockIndent, ref ExpandBuffer<byte> blockLineBreaks)
    {
        int maxIndent = 0;
        while (true) {
            while ((blockIndent == 0 || _mark.Col < blockIndent) &&
                   _currentCode == YamlCodes.SPACE) {
                Advance(1);
            }

            if (_mark.Col > maxIndent) {
                maxIndent = _mark.Col;
            }

            // Check for a tab character messing the indentation.
            if ((blockIndent == 0 || _mark.Col < blockIndent) && _currentCode == YamlCodes.TAB) {
                throw new YamlTokenizerException(in _mark,
                    "while scanning a block scalar, found a tab character where an indentation space is expected");
            }

            if (!YamlCodes.IsLineBreak(_currentCode)) {
                break;
            }

            switch (ConsumeLineBreaks()) {
                case LineBreakState.Lf:
                    blockLineBreaks.Add(YamlCodes.LF);
                    break;
                case LineBreakState.CrLf:
                    blockLineBreaks.Add(YamlCodes.CR);
                    blockLineBreaks.Add(YamlCodes.LF);
                    break;
                case LineBreakState.Cr:
                    blockLineBreaks.Add(YamlCodes.CR);
                    break;
            }
        }

        if (blockIndent == 0) {
            blockIndent = maxIndent;
            if (blockIndent < _indent + 1) {
                blockIndent = _indent + 1;
            }
            else if (blockIndent < 1) {
                blockIndent = 1;
            }
        }
    }

    void ConsumeFlowScaler(bool singleQuote)
    {
        SaveSimpleKeyCandidate();
        _simpleKeyAllowed = false;

        LineBreakState leadingBreak = default;
        LineBreakState trailingBreak = default;
        bool isLeadingBlanks = false;
        Scalar scalar = ScalarPool.Shared.Rent();

        Span<byte> whitespaceBuffer = stackalloc byte[32];
        int whitespaceLength = 0;

        // Eat the left quote
        Advance(1);

        while (true) {
            if (_mark.Col == 0 &&
                (_reader.IsNext(YamlCodes.StreamStart) ||
                 _reader.IsNext(YamlCodes.DocStart)) &&
                !TryPeek(3, out _)) {
                throw new YamlTokenizerException(_mark,
                    "while scanning a quoted scalar, found unexpected document indicator");
            }

            if (_reader.End) {
                throw new YamlTokenizerException(_mark,
                    "while scanning a quoted scalar, found unexpected end of stream");
            }

            isLeadingBlanks = false;

            // Consume non-blank characters
            while (!_reader.End && !YamlCodes.IsEmpty(_currentCode)) {
                switch (_currentCode) {
                    // Check for an escaped single quote
                    case YamlCodes.SINGLE_QUOTE when TryPeek(1, out byte nextCode) &&
                                                    nextCode == YamlCodes.SINGLE_QUOTE && singleQuote:
                        scalar.Write((byte)'\'');
                        Advance(2);
                        break;
                    // Check for the right quote.
                    case YamlCodes.SINGLE_QUOTE when singleQuote:
                    case YamlCodes.DOUBLE_QUOTE when !singleQuote:
                        goto LOOPEND;
                    // Check for an escaped line break.
                    case (byte)'\\' when !singleQuote &&
                                         TryPeek(1, out byte nextCode) &&
                                         YamlCodes.IsLineBreak(nextCode):
                        Advance(1);
                        ConsumeLineBreaks();
                        isLeadingBlanks = true;
                        break;
                    // Check for an escape sequence.
                    case (byte)'\\' when !singleQuote:
                        int codeLength = 0;
                        TryPeek(1, out byte escaped);
                        switch (escaped) {
                            case (byte)'0':
                                scalar.Write((byte)'\0');
                                break;
                            case (byte)'a':
                                scalar.Write((byte)'\a');
                                break;
                            case (byte)'b':
                                scalar.Write((byte)'\b');
                                break;
                            case (byte)'t':
                                scalar.Write((byte)'\t');
                                break;
                            case (byte)'n':
                                scalar.Write((byte)'\n');
                                break;
                            case (byte)'v':
                                scalar.Write((byte)'\v');
                                break;
                            case (byte)'f':
                                scalar.Write((byte)'\f');
                                break;
                            case (byte)'r':
                                scalar.Write((byte)'\r');
                                break;
                            case (byte)'e':
                                scalar.Write(0x1b);
                                break;
                            case (byte)' ':
                                scalar.Write((byte)' ');
                                break;
                            case (byte)'"':
                                scalar.Write((byte)'"');
                                break;
                            case (byte)'\'':
                                scalar.Write((byte)'\'');
                                break;
                            case (byte)'\\':
                                scalar.Write((byte)'\\');
                                break;
                            // NEL (#x85)
                            case (byte)'N':
                                scalar.WriteUnicodeCodepoint(0x85);
                                break;
                            // #xA0
                            case (byte)'_':
                                scalar.WriteUnicodeCodepoint(0xA0);
                                break;
                            // LS (#x2028)
                            case (byte)'L':
                                scalar.WriteUnicodeCodepoint(0x2028);
                                break;
                            // PS (#x2029)
                            case (byte)'P':
                                scalar.WriteUnicodeCodepoint(0x2029);
                                break;
                            case (byte)'x':
                                codeLength = 2;
                                break;
                            case (byte)'u':
                                codeLength = 4;
                                break;
                            case (byte)'U':
                                codeLength = 8;
                                break;
                            default:
                                throw new YamlTokenizerException(_mark,
                                    "while parsing a quoted scalar, found unknown escape character");
                        }

                        Advance(2);
                        // Consume an arbitrary escape code.
                        if (codeLength > 0) {
                            int codepoint = 0;
                            for (int i = 0; i < codeLength; i++) {
                                if (TryPeek(i, out byte hex) && YamlCodes.IsHex(hex)) {
                                    codepoint = (codepoint << 4) + YamlCodes.AsHex(hex);
                                }
                                else {
                                    throw new YamlTokenizerException(_mark,
                                        "While parsing a quoted scalar, did not find expected hexadecimal number");
                                }
                            }
                            scalar.WriteUnicodeCodepoint(codepoint);
                        }

                        Advance(codeLength);
                        break;
                    default:
                        scalar.Write(_currentCode);
                        Advance(1);
                        break;
                }
            }

            // Consume blank characters.
            while (YamlCodes.IsBlank(_currentCode) || YamlCodes.IsLineBreak(_currentCode)) {
                if (YamlCodes.IsBlank(_currentCode)) {
                    // Consume a space or a tab character.
                    if (!isLeadingBlanks) {
                        if (whitespaceBuffer.Length <= whitespaceLength) {
                            whitespaceBuffer = new byte[whitespaceBuffer.Length * 2];
                        }
                        whitespaceBuffer[whitespaceLength++] = _currentCode;
                    }
                    Advance(1);
                }
                else {
                    // Check if it is a first line break.
                    if (isLeadingBlanks) {
                        trailingBreak = ConsumeLineBreaks();
                    }
                    else {
                        whitespaceLength = 0;
                        leadingBreak = ConsumeLineBreaks();
                        isLeadingBlanks = true;
                    }
                }
            }

            // Join the whitespaces or fold line breaks.
            if (isLeadingBlanks) {
                if (leadingBreak == LineBreakState.None) {
                    scalar.Write(trailingBreak);
                    trailingBreak = LineBreakState.None;
                }
                else {
                    if (trailingBreak == LineBreakState.None) {
                        scalar.Write(YamlCodes.SPACE);
                    }
                    else {
                        scalar.Write(trailingBreak);
                        trailingBreak = LineBreakState.None;
                    }
                    leadingBreak = LineBreakState.None;
                }
            }
            else {
                scalar.Write(whitespaceBuffer[..whitespaceLength]);
                whitespaceLength = 0;
            }
        }

    // Eat the right quote
    LOOPEND:
        Advance(1);
        _simpleKeyAllowed = isLeadingBlanks;

        // From spec: To ensure JSON compatibility, if a key inside a flow mapping is JSON-like,
        // YAML allows the following value to be specified adjacent to the “:”.
        _adjacentValueAllowedAt = _mark.Position;

        _tokens.Enqueue(new Token(singleQuote
            ? TokenType.SingleQuotedScaler
            : TokenType.DoubleQuotedScaler,
            scalar));
    }

    void ConsumePlainScalar()
    {
        SaveSimpleKeyCandidate();
        _simpleKeyAllowed = false;

        int currentIndent = _indent + 1;
        LineBreakState leadingBreak = default;
        LineBreakState trailingBreak = default;
        bool isLeadingBlanks = false;
        Scalar scalar = ScalarPool.Shared.Rent();

        Span<byte> whitespaceBuffer = stackalloc byte[16];
        int whitespaceLength = 0;

        while (true) {
            // Check for a document indicator
            if (_mark.Col == 0) {
                if (_currentCode == (byte)'-' && _reader.IsNext(YamlCodes.StreamStart) && IsEmptyNext(YamlCodes.StreamStart.Length)) {
                    break;
                }
                if (_currentCode == (byte)'.' && _reader.IsNext(YamlCodes.DocStart) && IsEmptyNext(YamlCodes.DocStart.Length)) {
                    break;
                }
            }
            if (_currentCode == YamlCodes.COMMENT) {
                break;
            }

            while (!_reader.End && !YamlCodes.IsEmpty(_currentCode)) {
                if (_currentCode == YamlCodes.MAP_VALUE_INDENT) {
                    bool hasNext = TryPeek(1, out byte nextCode);
                    if (!hasNext ||
                        YamlCodes.IsEmpty(nextCode) ||
                        (_flowLevel > 0 && YamlCodes.IsAnyFlowSymbol(nextCode))) {
                        break;
                    }
                }
                else if (_flowLevel > 0 && YamlCodes.IsAnyFlowSymbol(_currentCode)) {
                    break;
                }

                if (isLeadingBlanks || whitespaceLength > 0) {
                    if (isLeadingBlanks) {
                        if (leadingBreak == LineBreakState.None) {
                            scalar.Write(trailingBreak);
                            trailingBreak = LineBreakState.None;
                        }
                        else {
                            if (trailingBreak == LineBreakState.None) {
                                scalar.Write(YamlCodes.SPACE);
                            }
                            else {
                                scalar.Write(trailingBreak);
                                trailingBreak = LineBreakState.None;
                            }
                            leadingBreak = LineBreakState.None;
                        }
                        isLeadingBlanks = false;
                    }
                    else {
                        scalar.Write(whitespaceBuffer[..whitespaceLength]);
                        whitespaceLength = 0;
                    }
                }

                scalar.Write(_currentCode);
                Advance(1);
            }

            // is the end?
            if (!YamlCodes.IsEmpty(_currentCode)) {
                break;
            }

            // whitespaces or line-breaks
            while (YamlCodes.IsEmpty(_currentCode)) {
                // whitespaces
                if (YamlCodes.IsBlank(_currentCode)) {
                    if (isLeadingBlanks && _mark.Col < currentIndent && _currentCode == YamlCodes.TAB) {
                        throw new YamlTokenizerException(_mark, "While scanning a plain scaler, found a tab");
                    }
                    if (!isLeadingBlanks) {
                        // If the buffer on the stack is insufficient, it is decompressed.
                        // This is probably a very rare case.
                        if (whitespaceLength >= whitespaceBuffer.Length) {
                            whitespaceBuffer = new byte[whitespaceBuffer.Length * 2];
                        }
                        whitespaceBuffer[whitespaceLength++] = _currentCode;
                    }
                    Advance(1);
                }
                // line-break
                else {
                    // Check if it is a first line break
                    if (isLeadingBlanks) {
                        trailingBreak = ConsumeLineBreaks();
                    }
                    else {
                        leadingBreak = ConsumeLineBreaks();
                        isLeadingBlanks = true;
                        whitespaceLength = 0;
                    }
                }
            }

            // check indentation level
            if (_flowLevel == 0 && _mark.Col < currentIndent) {
                break;
            }
        }

        _simpleKeyAllowed = isLeadingBlanks;
        _tokens.Enqueue(new Token(TokenType.PlainScalar, scalar));
    }

    void SkipToNextToken()
    {
        while (true) {
            switch (_currentCode) {
                case YamlCodes.SPACE:
                    Advance(1);
                    break;
                case YamlCodes.TAB when _flowLevel > 0 || !_simpleKeyAllowed:
                    Advance(1);
                    break;
                case YamlCodes.LF:
                case YamlCodes.CR:
                    ConsumeLineBreaks();
                    if (_flowLevel == 0) {
                        _simpleKeyAllowed = true;
                    }

                    break;
                case YamlCodes.COMMENT:
                    while (!_reader.End && !YamlCodes.IsLineBreak(_currentCode)) {
                        Advance(1);
                    }
                    break;
                case 0xEF:
                    ConsumeBom();
                    break;
                default:
                    return;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Advance(int offset)
    {
        for (int i = 0; i < offset; i++) {
            _mark.Position += 1;
            if (_currentCode == YamlCodes.LF) {
                _mark.Line += 1;
                _mark.Col = 0;
            }
            else {
                _mark.Col += 1;
            }
            _reader.Advance(1);
            _reader.TryPeek(out _currentCode);
        }
    }

    LineBreakState ConsumeLineBreaks()
    {
        if (_reader.End) {
            return LineBreakState.None;
        }

        switch (_currentCode) {
            case YamlCodes.CR:
                if (TryPeek(1, out byte secondCode) && secondCode == YamlCodes.LF) {
                    Advance(2);
                    return LineBreakState.CrLf;
                }
                Advance(1);
                return LineBreakState.Cr;
            case YamlCodes.LF:
                Advance(1);
                return LineBreakState.Lf;
        }
        return LineBreakState.None;
    }

    readonly void StaleSimpleKeyCandidates()
    {
        for (int i = 0; i < _simpleKeyCandidates.Length; i++) {
            ref SimpleKeyState simpleKey = ref _simpleKeyCandidates[i];
            if (simpleKey.Possible &&
                (simpleKey.Start.Line < _mark.Line || simpleKey.Start.Position + 1024 < _mark.Position)) {
                if (simpleKey.Required) {
                    throw new YamlTokenizerException(_mark, "Simple key expect ':'");
                }
                simpleKey.Possible = false;
            }
        }
    }

    void SaveSimpleKeyCandidate()
    {
        if (!_simpleKeyAllowed) {
            return;
        }

        ref SimpleKeyState last = ref _simpleKeyCandidates[^1];
        if (last is { Possible: true, Required: true }) {
            throw new YamlTokenizerException(_mark, "Simple key expected");
        }

        _simpleKeyCandidates[^1] = new SimpleKeyState {
            Start = _mark,
            Possible = true,
            Required = _flowLevel > 0 && _indent == _mark.Col,
            TokenNumber = _tokensParsed + _tokens.Count
        };
    }

    readonly void RemoveSimpleKeyCandidate()
    {
        ref SimpleKeyState last = ref _simpleKeyCandidates[^1];
        if (last is { Possible: true, Required: true }) {
            throw new YamlTokenizerException(_mark, "Simple key expected");
        }
        last.Possible = false;
    }

    void RollIndent(int colTo, in Token nextToken, int insertNumber = -1)
    {
        if (_flowLevel > 0 || _indent >= colTo) {
            return;
        }

        _indents.Add(_indent);
        _indent = colTo;
        if (insertNumber >= 0) {
            _tokens.Insert(insertNumber - _tokensParsed, nextToken);
        }
        else {
            _tokens.Enqueue(nextToken);
        }
    }

    void UnrollIndent(int col)
    {
        if (_flowLevel > 0) {
            return;
        }
        while (_indent > col) {
            _tokens.Enqueue(new Token(TokenType.BlockEnd));
            _indent = _indents.Pop();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IncreaseFlowLevel()
    {
        _simpleKeyCandidates.Add(new SimpleKeyState());
        _flowLevel++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void DecreaseFlowLevel()
    {
        if (_flowLevel <= 0) {
            return;
        }

        _flowLevel--;
        _simpleKeyCandidates.Pop();
    }

    readonly bool IsEmptyNext(int offset)
    {
        if (_reader.End || _reader.Remaining <= offset) {
            return true;
        }

        // If offset doesn't fall inside current segment move to next until we find correct one
        if (_reader.CurrentSpanIndex + offset <= _reader.CurrentSpan.Length - 1) {
            byte nextCode = _reader.CurrentSpan[_reader.CurrentSpanIndex + offset];
            return YamlCodes.IsEmpty(nextCode);
        }

        int remainingOffset = offset;
        SequencePosition nextPosition = _reader.Position;
        ReadOnlyMemory<byte> currentMemory;

        while (_reader.Sequence.TryGet(ref nextPosition, out currentMemory, advance: true)) {
            // Skip empty segment
            if (currentMemory.Length > 0) {
                if (remainingOffset >= currentMemory.Length) {
                    // Subtract current non consumed data
                    remainingOffset -= currentMemory.Length;
                }
                else {
                    break;
                }
            }
        }
        return YamlCodes.IsEmpty(currentMemory.Span[remainingOffset]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly bool TryPeek(long offset, out byte value)
    {
        // If we've got data and offset is not out of bounds
        if (_reader.End || _reader.Remaining <= offset) {
            value = default;
            return false;
        }

        // If offset doesn't fall inside current segment move to next until we find correct one
        if (_reader.CurrentSpanIndex + offset <= _reader.CurrentSpan.Length - 1) {
            value = _reader.CurrentSpan[_reader.CurrentSpanIndex + (int)offset];
            return true;
        }

        long remainingOffset = offset;
        SequencePosition nextPosition = _reader.Position;
        ReadOnlyMemory<byte> currentMemory;

        while (_reader.Sequence.TryGet(ref nextPosition, out currentMemory, advance: true)) {
            // Skip empty segment
            if (currentMemory.Length > 0) {
                if (remainingOffset >= currentMemory.Length) {
                    // Subtract current non consumed data
                    remainingOffset -= currentMemory.Length;
                }
                else {
                    break;
                }
            }
        }

        value = currentMemory.Span[(int)remainingOffset];
        return true;
    }
}

