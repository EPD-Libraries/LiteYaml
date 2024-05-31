using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using LiteYaml.Internal;

namespace LiteYaml.Emitter;

public class YamlEmitterException(string message) : Exception(message)
{
}

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
    static byte[] _whiteSpaces =
    {
        (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ',
        (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ',
        (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ',
        (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ',
    };
    static readonly byte[] _blockSequenceEntryHeader = { (byte)'-', (byte)' ' };
    static readonly byte[] FlowSequenceEmpty = { (byte)'[', (byte)']' };
    static readonly byte[] FlowSequenceSeparator = { (byte)',', (byte)' ' };
    static readonly byte[] MappingKeyFooter = { (byte)':', (byte)' ' };
    static readonly byte[] FlowMappingHeader = { (byte)'{', (byte)' ' };
    static readonly byte[] FlowMappingFooter = { (byte)' ', (byte)'}' };
    static readonly byte[] FlowMappingEmpty = { (byte)'{', (byte)'}' };

    [ThreadStatic]
    static ExpandBuffer<char>? stringBufferStatic;

    [ThreadStatic]
    static ExpandBuffer<EmitState>? stateBufferStatic;

    [ThreadStatic]
    static ExpandBuffer<int>? elementCountBufferSTatic;

    EmitState CurrentState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stateStack[^1];
    }

    EmitState PreviousState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stateStack[^2];
    }

    bool IsFirstElement
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => currentElementCount <= 0;
    }

    readonly IBufferWriter<byte> writer;
    readonly YamlEmitOptions options;

    readonly ExpandBuffer<char> stringBuffer;
    readonly ExpandBuffer<EmitState> stateStack;
    readonly ExpandBuffer<int> elementCountStack;
    readonly ExpandBuffer<string> tagStack;

    int currentIndentLevel;
    int currentElementCount;

    public Utf8YamlEmitter(IBufferWriter<byte> writer, YamlEmitOptions? options = null)
    {
        this.writer = writer;
        this.options = options ?? YamlEmitOptions.Default;

        currentIndentLevel = 0;

        stringBuffer = stringBufferStatic ??= new ExpandBuffer<char>(1024);
        stringBuffer.Clear();

        stateStack = stateBufferStatic ??= new ExpandBuffer<EmitState>(16);
        stateStack.Clear();

        elementCountStack = elementCountBufferSTatic ??= new ExpandBuffer<int>(16);
        elementCountStack.Clear();

        stateStack.Add(EmitState.None);
        currentElementCount = 0;

        tagStack = new ExpandBuffer<string>(4);
    }

    internal readonly IBufferWriter<byte> GetWriter()
    {
        return writer;
    }

    public void BeginSequence(SequenceStyle style = SequenceStyle.Block)
    {
        switch (style)
        {
            case SequenceStyle.Block:
                {
                    switch (CurrentState)
                    {
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
            case SequenceStyle.Flow:
                {
                    switch (CurrentState)
                    {
                        case EmitState.BlockMappingKey:
                            throw new YamlEmitterException("To start flow-sequence in the mapping key is not supported.");

                        case EmitState.BlockSequenceEntry:
                            {
                                Span<byte> output = writer.GetSpan(currentIndentLevel * options.IndentWidth + _blockSequenceEntryHeader.Length + 1);
                                int offset = 0;
                                WriteBlockSequenceEntryHeader(output, ref offset);
                                output[offset++] = YamlCodes.FlowSequenceStart;
                                writer.Advance(offset);
                                break;
                            }
                        case EmitState.FlowSequenceEntry:
                            {
                                Span<byte> output = writer.GetSpan(FlowSequenceSeparator.Length + 1);
                                int offset = 0;
                                if (currentElementCount > 0)
                                {
                                    FlowSequenceSeparator.CopyTo(output);
                                    offset += FlowSequenceSeparator.Length;
                                }
                                output[offset++] = YamlCodes.FlowSequenceStart;
                                writer.Advance(offset);
                                break;
                            }
                        default:
                            {
                                Span<byte> output = writer.GetSpan(GetTagLength() + 2);
                                int offset = 0;
                                if (TryWriteTag(output, ref offset))
                                {
                                    output[offset++] = YamlCodes.Space;
                                }
                                output[offset++] = YamlCodes.FlowSequenceStart;
                                writer.Advance(offset);
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
        switch (CurrentState)
        {
            case EmitState.BlockSequenceEntry:
                {
                    bool isEmptySequence = currentElementCount <= 0;
                    PopState();

                    // Empty sequence
                    if (isEmptySequence)
                    {
                        bool lineBreak = CurrentState is EmitState.BlockSequenceEntry or EmitState.BlockMappingValue;
                        WriteRaw(FlowSequenceEmpty, false, lineBreak);
                    }

                    switch (CurrentState)
                    {
                        case EmitState.BlockSequenceEntry:
                            if (!isEmptySequence)
                            {
                                DecreaseIndent();
                            }
                            currentElementCount++;
                            break;

                        case EmitState.BlockMappingKey:
                            throw new YamlEmitterException("Complex key is not supported.");

                        case EmitState.BlockMappingValue:
                            ReplaceCurrentState(EmitState.BlockMappingKey);
                            currentElementCount++;
                            break;

                        case EmitState.FlowSequenceEntry:
                            currentElementCount++;
                            break;
                    }
                    break;
                }
            case EmitState.FlowSequenceEntry:
                {
                    PopState();

                    bool needsLineBreak = false;
                    switch (CurrentState)
                    {
                        case EmitState.BlockSequenceEntry:
                            needsLineBreak = true;
                            currentElementCount++;
                            break;
                        case EmitState.BlockMappingValue:
                            ReplaceCurrentState(EmitState.BlockMappingKey); // end mapping value
                            needsLineBreak = true;
                            currentElementCount++;
                            break;
                        case EmitState.FlowSequenceEntry:
                            currentElementCount++;
                            break;
                        case EmitState.FlowMappingValue:
                            ReplaceCurrentState(EmitState.FlowMappingKey);
                            currentElementCount++;
                            break;
                    }

                    int suffixLength = 1;
                    if (needsLineBreak)
                    {
                        suffixLength++;
                    }

                    int offset = 0;
                    Span<byte> output = writer.GetSpan(suffixLength);
                    output[offset++] = YamlCodes.FlowSequenceEnd;
                    if (needsLineBreak)
                    {
                        output[offset++] = YamlCodes.Lf;
                    }
                    writer.Advance(offset);
                    break;
                }
            default:
                throw new YamlEmitterException($"Current state is not sequence: {CurrentState}");
        }
    }

    public void BeginMapping(MappingStyle style = MappingStyle.Block)
    {
        switch (style)
        {
            case MappingStyle.Block:
                {
                    switch (CurrentState)
                    {
                        case EmitState.BlockMappingKey:
                            throw new YamlEmitterException("To start block-mapping in the mapping key is not supported.");

                        case EmitState.FlowSequenceEntry:
                            throw new YamlEmitterException("Cannot start block-mapping in the flow-sequence");

                        case EmitState.BlockSequenceEntry:
                            {
                                WriteBlockSequenceEntryHeader();
                                break;
                            }
                    }
                    PushState(EmitState.BlockMappingKey);
                    break;
                }
            case MappingStyle.Flow:
                switch (CurrentState)
                {
                    case EmitState.BlockMappingKey:
                        throw new YamlEmitterException("To start flow-mapping in the mapping key is not supported.");

                    case EmitState.BlockSequenceEntry:
                        {
                            Span<byte> output = writer.GetSpan(currentIndentLevel * options.IndentWidth + _blockSequenceEntryHeader.Length + FlowMappingHeader.Length + GetTagLength() + 1);
                            int offset = 0;
                            WriteBlockSequenceEntryHeader(output, ref offset);
                            if (TryWriteTag(output, ref offset))
                            {
                                output[offset++] = YamlCodes.Space;
                            }
                            output[offset++] = YamlCodes.FlowMapStart;
                            writer.Advance(offset);
                            break;
                        }
                    case EmitState.FlowSequenceEntry:
                        {
                            Span<byte> output = writer.GetSpan(FlowSequenceSeparator.Length + FlowMappingHeader.Length);
                            int offset = 0;
                            if (!IsFirstElement)
                            {
                                FlowSequenceSeparator.CopyTo(output);
                                offset += FlowSequenceSeparator.Length;
                            }
                            output[offset++] = YamlCodes.FlowMapStart;
                            writer.Advance(offset);
                            break;
                        }
                    default:
                        {
                            Span<byte> output = writer.GetSpan(GetTagLength() + 2);
                            int offset = 0;
                            if (TryWriteTag(output, ref offset))
                            {
                                output[offset++] = YamlCodes.Space;
                            }
                            output[offset++] = YamlCodes.FlowMapStart;
                            writer.Advance(offset);
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
        switch (CurrentState)
        {
            case EmitState.BlockMappingKey:
                {
                    bool isEmptyMapping = currentElementCount <= 0;
                    PopState();

                    if (isEmptyMapping)
                    {
                        bool lineBreak = CurrentState is EmitState.BlockSequenceEntry or EmitState.BlockMappingValue;
                        if (tagStack.TryPop(out string? tag))
                        {
                            byte[] tagBytes = StringEncoding.Utf8.GetBytes(tag + " "); // TODO:
                            WriteRaw(tagBytes, FlowMappingEmpty, false, lineBreak);
                        }
                        else
                        {
                            WriteRaw(FlowMappingEmpty, false, lineBreak);
                        }
                    }

                    switch (CurrentState)
                    {
                        case EmitState.BlockSequenceEntry:
                            if (!isEmptyMapping)
                            {
                                DecreaseIndent();
                            }
                            currentElementCount++;
                            break;

                        case EmitState.BlockMappingValue:
                            if (!isEmptyMapping)
                            {
                                DecreaseIndent();
                            }
                            ReplaceCurrentState(EmitState.BlockMappingKey);
                            currentElementCount++;
                            break;
                    }
                    break;
                }
            case EmitState.FlowMappingKey:
                {
                    bool isEmptyMapping = currentElementCount <= 0;
                    PopState();

                    bool needsLineBreak = false;
                    switch (CurrentState)
                    {
                        case EmitState.BlockSequenceEntry:
                            needsLineBreak = true;
                            currentElementCount++;
                            break;
                        case EmitState.BlockMappingValue:
                            ReplaceCurrentState(EmitState.BlockMappingKey); // end mapping value
                            needsLineBreak = true;
                            currentElementCount++;
                            break;
                        case EmitState.FlowSequenceEntry:
                            currentElementCount++;
                            break;
                        case EmitState.FlowMappingValue:
                            ReplaceCurrentState(EmitState.FlowMappingKey);
                            currentElementCount++;
                            break;
                    }

                    int suffixLength = FlowMappingFooter.Length;
                    if (needsLineBreak)
                    {
                        suffixLength++;
                    }

                    int offset = 0;
                    Span<byte> output = writer.GetSpan(suffixLength);
                    if (!isEmptyMapping)
                    {
                        output[offset++] = YamlCodes.Space;
                    }
                    output[offset++] = YamlCodes.FlowMapEnd;
                    if (needsLineBreak)
                    {
                        output[offset++] = YamlCodes.Lf;
                    }
                    writer.Advance(offset);
                    break;
                }
            default:
                throw new YamlEmitterException($"Invalid mapping end: {CurrentState}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteRaw(ReadOnlySpan<byte> value, bool indent, bool lineBreak)
    {
        int length = value.Length +
                     (indent ? currentIndentLevel * options.IndentWidth : 0) +
                     (lineBreak ? 1 : 0);

        int offset = 0;
        Span<byte> output = writer.GetSpan(length);
        if (indent)
        {
            WriteIndent(output, ref offset);
        }
        value.CopyTo(output[offset..]);
        if (lineBreak)
        {
            output[length - 1] = YamlCodes.Lf;
        }
        writer.Advance(length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteRaw(ReadOnlySpan<byte> value1, ReadOnlySpan<byte> value2, bool indent, bool lineBreak)
    {
        int length = value1.Length + value2.Length +
                     (indent ? currentIndentLevel * options.IndentWidth : 0) +
                     (lineBreak ? 1 : 0);
        int offset = 0;
        Span<byte> output = writer.GetSpan(length);
        if (indent)
        {
            WriteIndent(output, ref offset);
        }

        value1.CopyTo(output[offset..]);
        offset += value1.Length;

        value2.CopyTo(output[offset..]);
        if (lineBreak)
        {
            output[length - 1] = YamlCodes.Lf;
        }
        writer.Advance(length);
    }

    public void Tag(string value)
    {
        tagStack.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteScalar(ReadOnlySpan<byte> value)
    {
        int offset = 0;
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(value.Length));

        BeginScalar(output, ref offset);
        value.CopyTo(output[offset..]);
        offset += value.Length;
        EndScalar(output, ref offset);

        writer.Advance(offset);
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
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(11)); // -2147483648

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten))
        {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);
        writer.Advance(offset);
    }

    public void WriteUInt32(uint value)
    {
        int offset = 0;
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(10)); // 4294967295

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten))
        {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        writer.Advance(offset);
    }

    public void WriteInt64(long value)
    {
        int offset = 0;
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(20)); // -9223372036854775808

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten))
        {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        writer.Advance(offset);
    }

    public void WriteUInt64(ulong value)
    {
        int offset = 0;
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(20)); // 18446744073709551615

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten))
        {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        writer.Advance(offset);
    }

    public void WriteFloat(float value)
    {
        int offset = 0;
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(12));

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten))
        {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        writer.Advance(offset);
    }

    public void WriteDouble(double value)
    {
        int offset = 0;
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(17));

        BeginScalar(output, ref offset);
        if (!Utf8Formatter.TryFormat(value, output[offset..], out int bytesWritten))
        {
            throw new YamlEmitterException($"Failed to emit : {value}");
        }
        offset += bytesWritten;
        EndScalar(output, ref offset);

        writer.Advance(offset);
    }

    public void WriteString(string value, ScalarStyle style = ScalarStyle.Any)
    {
        WriteString(value.AsSpan(), style);
    }

    public void WriteString(ReadOnlySpan<char> value, ScalarStyle style = ScalarStyle.Any)
    {
        if (style == ScalarStyle.Any)
        {
            EmitStringInfo analyzeInfo = EmitStringAnalyzer.Analyze(value);
            style = analyzeInfo.SuggestScalarStyle();
        }

        switch (style)
        {
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
        int stringMaxByteCount = StringEncoding.Utf8.GetMaxByteCount(value.Length);
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(stringMaxByteCount));
        int offset = 0;
        BeginScalar(output, ref offset);
        offset += StringEncoding.Utf8.GetBytes(value, output[offset..]);
        EndScalar(output, ref offset);
        writer.Advance(offset);
    }

    void WriteLiteralScalar(ReadOnlySpan<char> value)
    {
        int indentCharCount = (currentIndentLevel + 1) * options.IndentWidth;
        System.Text.StringBuilder scalarStringBuilt = EmitStringAnalyzer.BuildLiteralScalar(value, indentCharCount);
        Span<char> scalarChars = stringBuffer.AsSpan(scalarStringBuilt.Length);
        scalarStringBuilt.CopyTo(0, scalarChars, scalarStringBuilt.Length);

        if (CurrentState is EmitState.BlockMappingValue or EmitState.BlockSequenceEntry)
        {
            scalarChars = scalarChars[..^1]; // Remove duplicate last line-break;
        }

        int maxByteCount = StringEncoding.Utf8.GetMaxByteCount(scalarChars.Length);
        int offset = 0;
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(maxByteCount));
        BeginScalar(output, ref offset);
        offset += StringEncoding.Utf8.GetBytes(scalarChars, output[offset..]);
        EndScalar(output, ref offset);
        writer.Advance(offset);
    }

    void WriteQuotedScalar(ReadOnlySpan<char> value, bool doubleQuote = true)
    {
        System.Text.StringBuilder scalarStringBuilt = EmitStringAnalyzer.BuildQuotedScalar(value, doubleQuote);
        Span<char> scalarChars = stringBuffer.AsSpan(scalarStringBuilt.Length);
        scalarStringBuilt.CopyTo(0, scalarChars, scalarStringBuilt.Length);

        int maxByteCount = StringEncoding.Utf8.GetMaxByteCount(scalarChars.Length);
        int offset = 0;
        Span<byte> output = writer.GetSpan(CalculateMaxScalarBufferLength(maxByteCount));
        BeginScalar(output, ref offset);
        offset += StringEncoding.Utf8.GetBytes(scalarChars, output[offset..]);
        EndScalar(output, ref offset);
        writer.Advance(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteBlockSequenceEntryHeader()
    {
        Span<byte> output = writer.GetSpan(_blockSequenceEntryHeader.Length + currentIndentLevel * options.IndentWidth + 2);
        int offset = 0;
        WriteBlockSequenceEntryHeader(output, ref offset);
        writer.Advance(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteBlockSequenceEntryHeader(Span<byte> output, ref int offset)
    {
        if (IsFirstElement)
        {
            switch (PreviousState)
            {
                case EmitState.BlockSequenceEntry:
                    output[offset++] = YamlCodes.Lf;
                    IncreaseIndent();
                    break;
                case EmitState.BlockMappingValue:
                    output[offset++] = YamlCodes.Lf;
                    break;
            }
        }
        WriteIndent(output, ref offset);
        _blockSequenceEntryHeader.CopyTo(output[offset..]);
        offset += _blockSequenceEntryHeader.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteIndent(Span<byte> output, ref int offset, int forceWidth = -1)
    {
        int length;
        if (forceWidth > -1)
        {
            if (forceWidth <= 0)
            {
                return;
            }

            length = forceWidth;
        }
        else if (currentIndentLevel > 0)
        {
            length = currentIndentLevel * options.IndentWidth;
        }
        else
        {
            return;
        }

        if (length > _whiteSpaces.Length)
        {
            _whiteSpaces = Enumerable.Repeat(YamlCodes.Space, length * 2).ToArray();
        }
        _whiteSpaces.AsSpan(0, length).CopyTo(output[offset..]);
        offset += length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int CalculateMaxScalarBufferLength(int length)
    {
        return length + (currentIndentLevel + 1) * options.IndentWidth + 3 + GetTagLength();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void BeginScalar(Span<byte> output, ref int offset)
    {
        switch (CurrentState)
        {
            case EmitState.BlockSequenceEntry:
                {
                    WriteBlockSequenceEntryHeader(output, ref offset);

                    if (TryWriteTag(output, ref offset))
                    {
                        output[offset++] = YamlCodes.Space;
                    }
                    break;
                }
            case EmitState.BlockMappingKey:
                {
                    if (IsFirstElement)
                    {
                        switch (PreviousState)
                        {
                            case EmitState.BlockSequenceEntry:
                                {
                                    IncreaseIndent();

                                    // Try write tag
                                    if (tagStack.TryPop(out string? tag))
                                    {
                                        offset += StringEncoding.Utf8.GetBytes(tag, output[offset..]);
                                        output[offset++] = YamlCodes.Lf;
                                        WriteIndent(output, ref offset);
                                    }
                                    else
                                    {
                                        WriteIndent(output, ref offset, options.IndentWidth - 2);
                                    }
                                    // The first key in block-sequence is like so that: "- key: .."
                                    break;
                                }
                            case EmitState.BlockMappingValue:
                                {
                                    IncreaseIndent();
                                    TryWriteTag(output, ref offset);
                                    output[offset++] = YamlCodes.Lf;
                                    WriteIndent(output, ref offset);
                                    break;
                                }
                            default:
                                WriteIndent(output, ref offset);
                                break;
                        }

                        if (TryWriteTag(output, ref offset))
                        {
                            output[offset++] = YamlCodes.Lf;
                            WriteIndent(output, ref offset);
                        }
                    }
                    else
                    {
                        WriteIndent(output, ref offset);
                    }
                    break;
                }
            case EmitState.BlockMappingValue:
                if (TryWriteTag(output, ref offset))
                {
                    output[offset++] = YamlCodes.Space;
                }
                break;

            case EmitState.FlowSequenceEntry:
                if (!IsFirstElement)
                {
                    FlowSequenceSeparator.CopyTo(output[offset..]);
                    offset += FlowSequenceSeparator.Length;
                }
                if (TryWriteTag(output, ref offset))
                {
                    output[offset++] = YamlCodes.Space;
                }
                break;
            case EmitState.FlowMappingKey:
                if (IsFirstElement)
                {
                    output[offset++] = YamlCodes.Space;
                }
                else
                {
                    FlowSequenceSeparator.CopyTo(output[offset..]);
                    offset += FlowSequenceSeparator.Length;
                }
                break;
            case EmitState.FlowMappingValue:
            case EmitState.None:
                if (TryWriteTag(output, ref offset))
                {
                    output[offset++] = YamlCodes.Space;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EndScalar(Span<byte> output, ref int offset)
    {
        switch (CurrentState)
        {
            case EmitState.BlockSequenceEntry:
                output[offset++] = YamlCodes.Lf;
                currentElementCount++;
                break;
            case EmitState.BlockMappingKey:
                MappingKeyFooter.CopyTo(output[offset..]);
                offset += MappingKeyFooter.Length;
                ReplaceCurrentState(EmitState.BlockMappingValue);
                break;
            case EmitState.BlockMappingValue:
                output[offset++] = YamlCodes.Lf;
                ReplaceCurrentState(EmitState.BlockMappingKey);
                currentElementCount++;
                break;
            case EmitState.FlowSequenceEntry:
                currentElementCount++;
                break;
            case EmitState.FlowMappingKey:
                MappingKeyFooter.CopyTo(output[offset..]);
                offset += MappingKeyFooter.Length;
                ReplaceCurrentState(EmitState.FlowMappingValue);
                break;
            case EmitState.FlowMappingValue:
                ReplaceCurrentState(EmitState.FlowMappingKey);
                currentElementCount++;
                break;
            case EmitState.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReplaceCurrentState(EmitState newState)
    {
        stateStack[^1] = newState;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void PushState(EmitState state)
    {
        stateStack.Add(state);
        elementCountStack.Add(currentElementCount);
        currentElementCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void PopState()
    {
        stateStack.Pop();
        currentElementCount = elementCountStack.Length > 0 ? elementCountStack.Pop() : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IncreaseIndent()
    {
        currentIndentLevel++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void DecreaseIndent()
    {
        if (currentIndentLevel > 0)
        {
            currentIndentLevel--;
        }
    }

    bool TryWriteTag(Span<byte> output, ref int offset)
    {
        if (tagStack.TryPop(out string? tag))
        {
            offset += StringEncoding.Utf8.GetBytes(tag, output[offset..]);
            return true;
        }
        return false;
    }

    int GetTagLength()
    {
        return tagStack.Length > 0 ? StringEncoding.Utf8.GetMaxByteCount(tagStack.Peek().Length) : 0;
    }
}