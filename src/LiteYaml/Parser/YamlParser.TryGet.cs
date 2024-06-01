using System.Runtime.CompilerServices;

namespace LiteYaml.Parser;

public ref partial struct YamlParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsNullScalar()
    {
        return CurrentEventType == ParseEventType.Scalar &&
               (_currentScalar == null ||
                _currentScalar.IsNull());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly string? GetScalarAsString()
    {
        return _currentScalar?.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> GetScalarAsUtf8()
    {
        if (_currentScalar is { } scalar) {
            return scalar.AsUtf8();
        }
        YamlParserException.Throw(CurrentMark, $"Cannot detect a scalar value as utf8 : {CurrentEventType} {_currentScalar}");
        return default!;
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public readonly bool IsScalar
    // {
    //     if (currentScalar is { } scalar)
    //     {
    //         return scalar.AsUtf8();
    //     }
    //     YamlParserException.Throw(CurrentMark, $"Cannot detect a scalar value as utf8 : {CurrentEventType} {currentScalar}");
    //     return default!;
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetScalarAsSpan(out ReadOnlySpan<byte> span)
    {
        if (_currentScalar is null) {
            span = default;
            return false;
        }
        span = _currentScalar.AsSpan();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool GetScalarAsBool()
    {
        if (_currentScalar is { } scalar && scalar.TryGetBool(out bool value)) {
            return value;
        }
        YamlParserException.Throw(CurrentMark, $"Cannot detect a scalar value as bool : {CurrentEventType} {_currentScalar}");
        return default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetScalarAsInt32()
    {
        if (_currentScalar is { } scalar && scalar.TryGetInt32(out int value)) {
            return value;
        }
        YamlParserException.Throw(CurrentMark, $"Cannot detect a scalar value as Int32: {CurrentEventType} {_currentScalar}");
        return default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly long GetScalarAsInt64()
    {
        if (_currentScalar is { } scalar && scalar.TryGetInt64(out long value)) {
            return value;
        }
        YamlParserException.Throw(CurrentMark, $"Cannot detect a scalar value as Int64: {CurrentEventType} {_currentScalar}");
        return default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint GetScalarAsUInt32()
    {
        if (_currentScalar is { } scalar && scalar.TryGetUInt32(out uint value)) {
            return value;
        }
        YamlParserException.Throw(CurrentMark, $"Cannot detect a scalar value as UInt32 : {CurrentEventType} {_currentScalar}");
        return default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ulong GetScalarAsUInt64()
    {
        if (_currentScalar is { } scalar && scalar.TryGetUInt64(out ulong value)) {
            return value;
        }
        YamlParserException.Throw(CurrentMark, $"Cannot detect a scalar value as UInt64 : {CurrentEventType} ({_currentScalar})");
        return default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float GetScalarAsFloat()
    {
        if (_currentScalar is { } scalar && scalar.TryGetFloat(out float value)) {
            return value;
        }
        YamlParserException.Throw(CurrentMark, $"Cannot detect scalar value as float : {CurrentEventType} {_currentScalar}");
        return default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly double GetScalarAsDouble()
    {
        if (_currentScalar is { } scalar && scalar.TryGetDouble(out double value)) {
            return value;
        }
        YamlParserException.Throw(CurrentMark, $"Cannot detect a scalar value as double : {CurrentEventType} {_currentScalar}");
        return default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? ReadScalarAsString()
    {
        string? result = _currentScalar?.ToString();
        ReadWithVerify(ParseEventType.Scalar);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadScalarAsBool()
    {
        bool result = GetScalarAsBool();
        ReadWithVerify(ParseEventType.Scalar);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadScalarAsInt32()
    {
        int result = GetScalarAsInt32();
        ReadWithVerify(ParseEventType.Scalar);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadScalarAsInt64()
    {
        long result = GetScalarAsInt64();
        ReadWithVerify(ParseEventType.Scalar);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadScalarAsUInt32()
    {
        uint result = GetScalarAsUInt32();
        ReadWithVerify(ParseEventType.Scalar);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadScalarAsUInt64()
    {
        ulong result = GetScalarAsUInt64();
        ReadWithVerify(ParseEventType.Scalar);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadScalarAsFloat()
    {
        float result = GetScalarAsFloat();
        ReadWithVerify(ParseEventType.Scalar);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadScalarAsDouble()
    {
        double result = GetScalarAsDouble();
        ReadWithVerify(ParseEventType.Scalar);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadScalarAsString(out string? result)
    {
        if (CurrentEventType != ParseEventType.Scalar) {
            result = default;
            return false;
        }
        result = _currentScalar?.ToString();
        ReadWithVerify(ParseEventType.Scalar);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadScalarAsBool(out bool result)
    {
        if (TryGetScalarAsBool(out result)) {
            ReadWithVerify(ParseEventType.Scalar);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadScalarAsInt32(out int result)
    {
        if (TryGetScalarAsInt32(out result)) {
            ReadWithVerify(ParseEventType.Scalar);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadScalarAsInt64(out long result)
    {
        if (TryGetScalarAsInt64(out result)) {
            ReadWithVerify(ParseEventType.Scalar);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadScalarAsUInt32(out uint result)
    {
        if (TryGetScalarAsUInt32(out result)) {
            ReadWithVerify(ParseEventType.Scalar);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadScalarAsUInt64(out ulong result)
    {
        if (TryGetScalarAsUInt64(out result)) {
            ReadWithVerify(ParseEventType.Scalar);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadScalarAsFloat(out float result)
    {
        if (TryGetScalarAsFloat(out result)) {
            ReadWithVerify(ParseEventType.Scalar);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadScalarAsDouble(out double result)
    {
        if (TryGetScalarAsDouble(out result)) {
            ReadWithVerify(ParseEventType.Scalar);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetScalarAsString(out string? value)
    {
        if (_currentScalar is { } scalar) {
            value = scalar.IsNull() ? null : scalar.ToString();
            return true;
        }
        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetScalarAsBool(out bool value)
    {
        if (_currentScalar is { } scalar) {
            return scalar.TryGetBool(out value);
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetScalarAsInt32(out int value)
    {
        if (_currentScalar is { } scalar) {
            return scalar.TryGetInt32(out value);
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetScalarAsUInt32(out uint value)
    {
        if (_currentScalar is { } scalar) {
            return scalar.TryGetUInt32(out value);
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetScalarAsInt64(out long value)
    {
        if (_currentScalar is { } scalar) {
            return scalar.TryGetInt64(out value);
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetScalarAsUInt64(out ulong value)
    {
        if (_currentScalar is { } scalar) {
            return scalar.TryGetUInt64(out value);
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetScalarAsFloat(out float value)
    {
        if (_currentScalar is { } scalar) {
            return scalar.TryGetFloat(out value);
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetScalarAsDouble(out double value)
    {
        if (_currentScalar is { } scalar) {
            return scalar.TryGetDouble(out value);
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetCurrentTag(out Tag tag)
    {
        if (_currentTag != null) {
            tag = _currentTag;
            return true;
        }
        tag = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetCurrentAnchor(out Anchor anchor)
    {
        if (_currentAnchor != null) {
            anchor = _currentAnchor;
            return true;
        }
        anchor = default!;
        return false;
    }
}
