using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;
using LiteYaml.Internal;

namespace LiteYaml.Emitter;

enum EmitState
{
    None,
    BlockSequenceEntry,
    BlockMappingKey,
    BlockMappingValue,
    FlowSequenceEntry,
    FlowMappingKey,
    FlowMappingValue,
}

public ref struct Utf8YamlEmitter
{
    private static byte[] _whiteSpaces = [
        (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ',
        (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ',
        (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ',
        (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ',
    ];
    private static readonly byte[] _blockSequenceEntryHeader = [(byte)'-', (byte)' '];
    private static readonly byte[] _flowSequenceEmpty = [(byte)'[', (byte)']'];
    private static readonly byte[] _flowSequenceSeparator = [(byte)',', (byte)' '];
    private static readonly byte[] _mappingKeyFooter = [(byte)':', (byte)' '];
    private static readonly byte[] _flowMappingHeader = [(byte)'{', (byte)' '];
    private static readonly byte[] _flowMappingFooter = [(byte)' ', (byte)'}'];
    private static readonly byte[] _flowMappingEmpty = [(byte)'{', (byte)'}'];

    [ThreadStatic]
    private static ExpandBuffer<char>? _stringBufferStatic;

    [ThreadStatic]
    private static ExpandBuffer<EmitState>? _stateBufferStatic;

    [ThreadStatic]
    private static ExpandBuffer<int>? _elementCountBufferStatic;

    private readonly EmitState CurrentState {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _stateStack[^1];
    }

    private readonly EmitState PreviousState {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _stateStack[^2];
    }

    private readonly bool IsFirstElement {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _currentElementCount <= 0;
    }

    readonly IBufferWriter<byte> _writer;
    readonly YamlEmitterOptions _options;

    readonly ExpandBuffer<char> _stringBuffer;
    readonly ExpandBuffer<EmitState> _stateStack;
    readonly ExpandBuffer<int> _elementCountStack;
    readonly ExpandBuffer<string> _tagStack;

    int _currentIndentLevel;
    int _currentElementCount;

    public Utf8YamlEmitter(IBufferWriter<byte> writer, YamlEmitterOptions? options = null)
    {
        _writer = writer;
        _options = options ?? YamlEmitterOptions.Default;

        _currentIndentLevel = 0;

        _stringBuffer = _stringBufferStatic ??= new ExpandBuffer<char>(1024);
        _stringBuffer.Clear();

        _stateStack = _stateBufferStatic ??= new ExpandBuffer<EmitState>(16);
        _stateStack.Clear();

        _elementCountStack = _elementCountBufferStatic ??= new ExpandBuffer<int>(16);
        _elementCountStack.Clear();

        _stateStack.Add(EmitState.None);
        _currentElementCount = 0;

        _tagStack = new ExpandBuffer<string>(4);
    }

    internal readonly IBufferWriter<byte> GetWriter()
    {
        return _writer;
    }

    public void BeginSequence(SequenceStyle style = SequenceStyle.Block)
    {
        switch (style) {
            case SequenceStyle.Block: {
                switch (CurrentState) {
                    case EmitState.BlockSequenceEntry:
                        WriteBlockSequenceEntryHeader();
                        break;
                    case EmitState.FlowSequenceEntry:
                        throw new YamlEmitterException(
                            "To start block-sequence in the flow-sequence is not supported.");
                    case EmitState.BlockMappingKey:
                        throw new YamlEmitterException(
                            "To start block-sequence in the mapping key is not supported.");
                }

                PushState(EmitState.BlockSequenceEntry);
                break;
            }
            case SequenceStyle.Flow: {
                switch (CurrentState) {
                    case EmitState.BlockMappingKey:
                        throw new YamlEmitterException("To start flow-sequence in the mapping key is not supported.");

                    case EmitState.BlockSequenceEntry: {
                        Span<byte> output = _writer.GetSpan(_currentIndentLevel * _options.IndentWidth + _blockSequenceEntryHeader.Length + 1);
                        int offset = 0;
                        WriteBlockSequenceEntryHeader(output, ref offset);
                        output[offset++] = YamlCodes.FLOW_SEQUENCE_START;
                        _writer.Advance(offset);
                        break;
                    }
                    case EmitState.FlowSequenceEntry: {
                        Span<byte> output = _writer.GetSpan(_flowSequenceSeparator.Length + 1);
                        int offset = 0;
                        if (_currentElementCount > 0) {
                            _flowSequenceSeparator.CopyTo(output);
                            offset += _flowSequenceSeparator.Length;
                        }
                        output[offset++] = YamlCodes.FLOW_SEQUENCE_START;
                        _writer.Advance(offset);
                        break;
                    }
                    default: {
                        Span<byte> output = _writer.GetSpan(GetTagBufferLength() + 2);
                        int offset = 0;
                        if (TryWriteTag(output, ref offset)) {
                            output[offset++] = YamlCodes.SPACE;
                        }
                        output[offset++] = YamlCodes.FLOW_SEQUENCE_START;
                        _writer.Advance(offset);
                        break;
                    }
                }
                PushState(EmitState.FlowSequenceEntry);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }
    }

    public void EndSequence()
    {
        switch (CurrentState) {
            case EmitState.BlockSequenceEntry: {
                bool isEmptySequence = _currentElementCount <= 0;
                PopState();

                // Empty sequence
                if (isEmptySequence) {
                    bool lineBreak = CurrentState is EmitState.BlockSequenceEntry or EmitState.BlockMappingValue;
                    WriteRaw(_flowSequenceEmpty, false, lineBreak);
                }

                switch (CurrentState) {
                    case EmitState.BlockSequenceEntry:
                        if (!isEmptySequence) {
                            DecreaseIndent();
                        }
                        _currentElementCount++;
                        break;

                    case EmitState.BlockMappingKey:
                        throw new YamlEmitterException("Complex key is not supported.");

                    case EmitState.BlockMappingValue:
                        ReplaceCurrentState(EmitState.BlockMappingKey);
                        _currentElementCount++;
                        break;

                    case EmitState.FlowSequenceEntry:
                        _currentElementCount++;
                        break;
                }
                break;
            }
            case EmitState.FlowSequenceEntry: {
                PopState();

                bool needsLineBreak = false;
                switch (CurrentState) {
                    case EmitState.BlockSequenceEntry:
                        needsLineBreak = true;
                        _currentElementCount++;
                        break;
                    case EmitState.BlockMappingValue:
                        ReplaceCurrentState(EmitState.BlockMappingKey); // end mapping value
                        needsLineBreak = true;
                        _currentElementCount++;
                        break;
                    case EmitState.FlowSequenceEntry:
                        _currentElementCount++;
                        break;
                    case EmitState.FlowMappingValue:
                        ReplaceCurrentState(EmitState.FlowMappingKey);
                        _currentElementCount++;
                        break;
                }

                int suffixLength = 1;
                if (needsLineBreak) {
                    suffixLength++;
                }

                int offset = 0;
                Span<byte> output = _writer.GetSpan(suffixLength);
                output[offset++] = YamlCodes.FLOW_SEQUENCE_END;
                if (needsLineBreak) {
                    output[offset++] = YamlCodes.LF;
                }
                _writer.Advance(offset);
                break;
            }
            default:
                throw new YamlEmitterException($"Current state is not sequence: {CurrentState}");
        }
    }

    public void BeginMapping(MappingStyle style = MappingStyle.Block)
    {
        switch (style) {
            case MappingStyle.Block: {
                switch (CurrentState) {
                    case EmitState.BlockMappingKey:
                        throw new YamlEmitterException("To start block-mapping in the mapping key is not supported.");

                    case EmitState.FlowSequenceEntry:
                        throw new YamlEmitterException("Cannot start block-mapping in the flow-sequence");

                    case EmitState.BlockSequenceEntry: {
                        WriteBlockSequenceEntryHeader();
                        break;
                    }
                }
                PushState(EmitState.BlockMappingKey);
                break;
            }
            case MappingStyle.Flow:
                switch (CurrentState) {
                    case EmitState.BlockMappingKey:
                        throw new YamlEmitterException("To start flow-mapping in the mapping key is not supported.");

                    case EmitState.BlockSequenceEntry: {
                        Span<byte> output = _writer.GetSpan(_currentIndentLevel * _options.IndentWidth + _blockSequenceEntryHeader.Length + _flowMappingHeader.Length + GetTagBufferLength() + 1);
                        int offset = 0;
                        WriteBlockSequenceEntryHeader(output, ref offset);
                        if (TryWriteTag(output, ref offset)) {
                            output[offset++] = YamlCodes.SPACE;
                        }
                        output[offset++] = YamlCodes.FLOW_MAP_START;
                        _writer.Advance(offset);
                        break;
                    }
                    case EmitState.FlowSequenceEntry: {
                        Span<byte> output = _writer.GetSpan(_flowSequenceSeparator.Length + _flowMappingHeader.Length);
                        int offset = 0;
                        if (!IsFirstElement) {
                            _flowSequenceSeparator.CopyTo(output);
                            offset += _flowSequenceSeparator.Length;
                        }
                        output[offset++] = YamlCodes.FLOW_MAP_START;
                        _writer.Advance(offset);
                        break;
                    }
                    default: {
                        Span<byte> output = _writer.GetSpan(GetTagBufferLength() + 2);
                        int offset = 0;
                        if (TryWriteTag(output, ref offset)) {
                            output[offset++] = YamlCodes.SPACE;
                        }
                        output[offset++] = YamlCodes.FLOW_MAP_START;
                        _writer.Advance(offset);
                        break;
                    }
                }
                PushState(EmitState.FlowMappingKey);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }
    }

    public void EndMapping()
    {
        switch (CurrentState) {
            case EmitState.BlockMappingKey: {
                bool isEmptyMapping = _currentElementCount <= 0;
                PopState();

                if (isEmptyMapping) {
                    bool lineBreak = CurrentState is EmitState.BlockSequenceEntry or EmitState.BlockMappingValue;
                    if (_tagStack.TryPop(out string? tag)) {
                        byte[] tagBytes = Encoding.UTF8.GetBytes(tag + " "); // TODO:
                        WriteRaw(tagBytes, _flowMappingEmpty, false, lineBreak);
                    }
                    else {
                        WriteRaw(_flowMappingEmpty, false, lineBreak);
                    }
                }

                switch (CurrentState) {
                    case EmitState.BlockSequenceEntry:
                        if (!isEmptyMapping) {
                            DecreaseIndent();
                        }
                        _currentElementCount++;
                        break;

                    case EmitState.BlockMappingValue:
                        if (!isEmptyMapping) {
                            DecreaseIndent();
                        }
                        ReplaceCurrentState(EmitState.BlockMappingKey);
                        _currentElementCount++;
                        break;
                }
                break;
            }
            case EmitState.FlowMappingKey: {
                bool isEmptyMapping = _currentElementCount <= 0;
                PopState();

                bool needsLineBreak = false;
                switch (CurrentState) {
                    case EmitState.BlockSequenceEntry:
                        needsLineBreak = true;
                        _currentElementCount++;
                        break;
                    case EmitState.BlockMappingValue:
                        ReplaceCurrentState(EmitState.BlockMappingKey); // end mapping value
                        needsLineBreak = true;
                        _currentElementCount++;
                        break;
                    case EmitState.FlowSequenceEntry:
                        _currentElementCount++;
                        break;
                    case EmitState.FlowMappingValue:
                        ReplaceCurrentState(EmitState.FlowMappingKey);
                        _currentElementCount++;
                        break;
                }

                int suffixLength = _flowMappingFooter.Length;
                if (needsLineBreak) {
                    suffixLength++;
                }

                int offset = 0;
                Span<byte> output = _writer.GetSpan(suffixLength);
                if (!isEmptyMapping) {
                    output[offset++] = YamlCodes.SPACE;
                }
                output[offset++] = YamlCodes.FLOW_MAP_END;
                if (needsLineBreak) {
                    output[offset++] = YamlCodes.LF;
                }
                _writer.Advance(offset);
                break;
            }
            default:
                throw new YamlEmitterException($"Invalid mapping end: {CurrentState}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void WriteRaw(ReadOnlySpan<byte> value, bool indent, bool lineBreak)
    {
        int length = value.Length +
                     (indent ? _currentIndentLevel * _options.IndentWidth : 0) +
                     (lineBreak ? 1 : 0);

        int offset = 0;
        Span<byte> output = _writer.GetSpan(length);
        if (indent) {
            WriteIndent(output, ref offset);
        }
        value.CopyTo(output[offset..]);
        if (lineBreak) {
            output[length - 1] = YamlCodes.LF;
        }
        _writer.Advance(length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void WriteRaw(ReadOnlySpan<byte> value1, ReadOnlySpan<byte> value2, bool indent, bool lineBreak)
    {
        int length = value1.Length + value2.Length +
                     (indent ? _currentIndentLevel * _options.IndentWidth : 0) +
                     (lineBreak ? 1 : 0);
        int offset = 0;
        Span<byte> output = _writer.GetSpan(length);
        if (indent) {
            WriteIndent(output, ref offset);
        }

        value1.CopyTo(output[offset..]);
        offset += value1.Length;

        value2.CopyTo(output[offset..]);
        if (lineBreak) {
            output[length - 1] = YamlCodes.LF;
        }
        _writer.Advance(length);
    }

    public readonly void SetTag(string value)
    {
        _tagStack.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteScalar(ReadOnlySpan<byte> value)
    {
        int offset = 0;
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(value.Length));

        BeginScalar(output, ref offset);
        value.CopyTo(output[offset..]);
        offset += value.Length;
        EndScalar(output, ref offset);

        _writer.Advance(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNull()
    {
        WriteScalar(YamlCodes.Null0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
        WriteScalar(value ? YamlCodes.True0 : YamlCodes.False0);
    }

    public void WriteInt32(int value)
    {
        int offset = 0;
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(11)); // -2147483648

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten)) {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);
        _writer.Advance(offset);
    }

    public void WriteUInt32(uint value)
    {
        int offset = 0;
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(10)); // 4294967295

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten)) {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        _writer.Advance(offset);
    }

    public void WriteInt64(long value)
    {
        int offset = 0;
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(20)); // -9223372036854775808

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten)) {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        _writer.Advance(offset);
    }

    public void WriteUInt64(ulong value)
    {
        int offset = 0;
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(20)); // 18446744073709551615

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten)) {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        _writer.Advance(offset);
    }

    public void WriteFloat(float value)
    {
        int offset = 0;
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(12));

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten)) {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        _writer.Advance(offset);
    }

    public void WriteDouble(double value)
    {
        int offset = 0;
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(17));

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten)) {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        _writer.Advance(offset);
    }

    public void WriteString(string value, ScalarStyle style = ScalarStyle.Any)
    {
        WriteString(value.AsSpan(), style);
    }

    public void WriteString(ReadOnlySpan<char> value, ScalarStyle style = ScalarStyle.Any)
    {
        if (style == ScalarStyle.Any) {
            EmitStringInfo analyzeInfo = EmitStringAnalyzer.Analyze(value);
            style = analyzeInfo.SuggestScalarStyle();
        }

        switch (style) {
            case ScalarStyle.Plain:
                WritePlainScalar(value);
                break;

            case ScalarStyle.SingleQuoted:
                WriteQuotedScalar(value, doubleQuote: false);
                break;

            case ScalarStyle.DoubleQuoted:
                WriteQuotedScalar(value, doubleQuote: true);
                break;

            case ScalarStyle.Literal:
                WriteLiteralScalar(value);
                break;

            case ScalarStyle.Folded:
                throw new NotSupportedException();

            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }
    }

    void WritePlainScalar(ReadOnlySpan<char> value)
    {
        int stringMaxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(stringMaxByteCount));
        int offset = 0;
        BeginScalar(output, ref offset);
        offset += Encoding.UTF8.GetBytes(value, output[offset..]);
        EndScalar(output, ref offset);
        _writer.Advance(offset);
    }

    void WriteLiteralScalar(ReadOnlySpan<char> value)
    {
        int indentCharCount = (_currentIndentLevel + 1) * _options.IndentWidth;
        StringBuilder scalarStringBuilt = EmitStringAnalyzer.BuildLiteralScalar(value, indentCharCount);
        Span<char> scalarChars = _stringBuffer.AsSpan(scalarStringBuilt.Length);
        scalarStringBuilt.CopyTo(0, scalarChars, scalarStringBuilt.Length);

        if (CurrentState is EmitState.BlockMappingValue or EmitState.BlockSequenceEntry) {
            scalarChars = scalarChars[..^1]; // Remove duplicate last line-break;
        }

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(scalarChars.Length);
        int offset = 0;
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(maxByteCount));
        BeginScalar(output, ref offset);
        offset += Encoding.UTF8.GetBytes(scalarChars, output[offset..]);
        EndScalar(output, ref offset);
        _writer.Advance(offset);
    }

    void WriteQuotedScalar(ReadOnlySpan<char> value, bool doubleQuote = true)
    {
        StringBuilder scalarStringBuilt = EmitStringAnalyzer.BuildQuotedScalar(value, doubleQuote);
        Span<char> scalarChars = _stringBuffer.AsSpan(scalarStringBuilt.Length);
        scalarStringBuilt.CopyTo(0, scalarChars, scalarStringBuilt.Length);

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(scalarChars.Length);
        int offset = 0;
        Span<byte> output = _writer.GetSpan(GetMaxScalarBufferLength(maxByteCount));
        BeginScalar(output, ref offset);
        offset += Encoding.UTF8.GetBytes(scalarChars, output[offset..]);
        EndScalar(output, ref offset);
        _writer.Advance(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteBlockSequenceEntryHeader()
    {
        Span<byte> output = _writer.GetSpan(_blockSequenceEntryHeader.Length + _currentIndentLevel * _options.IndentWidth + 2);
        int offset = 0;
        WriteBlockSequenceEntryHeader(output, ref offset);
        _writer.Advance(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteBlockSequenceEntryHeader(Span<byte> output, ref int offset)
    {
        if (IsFirstElement) {
            switch (PreviousState) {
                case EmitState.BlockSequenceEntry:
                    output[offset++] = YamlCodes.LF;
                    IncreaseIndent();
                    break;
                case EmitState.BlockMappingValue:
                    output[offset++] = YamlCodes.LF;
                    break;
            }
        }

        WriteIndent(output, ref offset);
        _blockSequenceEntryHeader.CopyTo(output[offset..]);
        offset += _blockSequenceEntryHeader.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void WriteIndent(Span<byte> output, ref int offset, int forceWidth = -1)
    {
        int length;
        if (forceWidth > -1) {
            if (forceWidth <= 0) {
                return;
            }

            length = forceWidth;
        }
        else if (_currentIndentLevel > 0) {
            length = _currentIndentLevel * _options.IndentWidth;
        }
        else {
            return;
        }

        if (length > _whiteSpaces.Length) {
            _whiteSpaces = Enumerable.Repeat(YamlCodes.SPACE, length * 2).ToArray();
        }
        _whiteSpaces.AsSpan(0, length).CopyTo(output[offset..]);
        offset += length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetMaxScalarBufferLength(int length)
    {
        return length + (_currentIndentLevel + 1) * _options.IndentWidth + 3 + GetTagBufferLength();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BeginScalar(Span<byte> output, ref int offset)
    {
        switch (CurrentState) {
            case EmitState.BlockSequenceEntry: {
                WriteBlockSequenceEntryHeader(output, ref offset);

                if (TryWriteTag(output, ref offset)) {
                    output[offset++] = YamlCodes.SPACE;
                }
                break;
            }
            case EmitState.BlockMappingKey: {
                if (IsFirstElement) {
                    switch (PreviousState) {
                        case EmitState.BlockSequenceEntry: {
                            IncreaseIndent();

                            // Try write tag
                            if (_tagStack.TryPop(out string? tag)) {
                                offset += Encoding.UTF8.GetBytes(tag, output[offset..]);
                                output[offset++] = YamlCodes.LF;
                                WriteIndent(output, ref offset);
                            }
                            else {
                                WriteIndent(output, ref offset, _options.IndentWidth - 2);
                            }
                            // The first key in block-sequence is like so that: "- key: .."
                            break;
                        }
                        case EmitState.BlockMappingValue: {
                            IncreaseIndent();
                            TryWriteTag(output, ref offset);
                            output[offset++] = YamlCodes.LF;
                            WriteIndent(output, ref offset);
                            break;
                        }
                        default:
                            WriteIndent(output, ref offset);
                            break;
                    }

                    if (TryWriteTag(output, ref offset)) {
                        output[offset++] = YamlCodes.LF;
                        WriteIndent(output, ref offset);
                    }
                }
                else {
                    WriteIndent(output, ref offset);
                }
                break;
            }
            case EmitState.BlockMappingValue:
                if (TryWriteTag(output, ref offset)) {
                    output[offset++] = YamlCodes.SPACE;
                }
                break;
            case EmitState.FlowSequenceEntry:
                if (!IsFirstElement) {
                    _flowSequenceSeparator.CopyTo(output[offset..]);
                    offset += _flowSequenceSeparator.Length;
                }
                if (TryWriteTag(output, ref offset)) {
                    output[offset++] = YamlCodes.SPACE;
                }
                break;
            case EmitState.FlowMappingKey:
                if (IsFirstElement) {
                    output[offset++] = YamlCodes.SPACE;
                }
                else {
                    _flowSequenceSeparator.CopyTo(output[offset..]);
                    offset += _flowSequenceSeparator.Length;
                }
                break;
            case EmitState.FlowMappingValue:
            case EmitState.None:
                if (TryWriteTag(output, ref offset)) {
                    output[offset++] = YamlCodes.SPACE;
                }
                break;
            default:
                throw YamlEmitterExceptions.InvalidEmitState;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EndScalar(Span<byte> output, ref int offset)
    {
        switch (CurrentState) {
            case EmitState.BlockSequenceEntry:
                output[offset++] = YamlCodes.LF;
                _currentElementCount++;
                break;
            case EmitState.BlockMappingKey:
                _mappingKeyFooter.CopyTo(output[offset..]);
                offset += _mappingKeyFooter.Length;
                ReplaceCurrentState(EmitState.BlockMappingValue);
                break;
            case EmitState.BlockMappingValue:
                output[offset++] = YamlCodes.LF;
                ReplaceCurrentState(EmitState.BlockMappingKey);
                _currentElementCount++;
                break;
            case EmitState.FlowSequenceEntry:
                _currentElementCount++;
                break;
            case EmitState.FlowMappingKey:
                _mappingKeyFooter.CopyTo(output[offset..]);
                offset += _mappingKeyFooter.Length;
                ReplaceCurrentState(EmitState.FlowMappingValue);
                break;
            case EmitState.FlowMappingValue:
                ReplaceCurrentState(EmitState.FlowMappingKey);
                _currentElementCount++;
                break;
            case EmitState.None:
                break;
            default:
                throw YamlEmitterExceptions.InvalidEmitState;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void ReplaceCurrentState(EmitState newState)
    {
        _stateStack[^1] = newState;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PushState(EmitState state)
    {
        _stateStack.Add(state);
        _elementCountStack.Add(_currentElementCount);
        _currentElementCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PopState()
    {
        _stateStack.Pop();
        _currentElementCount = _elementCountStack.Length switch {
            > 0 => _elementCountStack.Pop(),
            _ => 0
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncreaseIndent()
    {
        _currentIndentLevel++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DecreaseIndent()
    {
        _currentIndentLevel -= _currentIndentLevel switch {
            > 0 => 1,
            _ => 0
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool TryWriteTag(Span<byte> output, ref int offset)
    {
        return _tagStack.Length switch {
            < 1 => false,
            _ => (offset += Encoding.UTF8.GetBytes(_tagStack.Pop(), output[offset..])) > -1
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetTagBufferLength()
    {
        return _tagStack.Length switch {
            < 1 => 0,
            _ => Encoding.UTF8.GetMaxByteCount(_tagStack.Peek().Length)
        };
    }
}