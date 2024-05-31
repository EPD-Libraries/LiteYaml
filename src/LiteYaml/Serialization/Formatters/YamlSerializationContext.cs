#nullable enable
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using LiteYaml.Emitter;

namespace LiteYaml.Serialization
{
    public readonly struct SequenceStyleScope
    {
    }

    public readonly struct ScalarStyleScope
    {
    }

    public class YamlSerializationContext(YamlSerializerOptions options) : IDisposable
    {
        public YamlSerializerOptions Options { get; set; } = options;
        public IYamlFormatterResolver Resolver { get; set; } = options.Resolver;
        public YamlEmitOptions EmitOptions { get; set; } = options.EmitOptions;

        readonly byte[] primitiveValueBuffer = ArrayPool<byte>.Shared.Rent(64);
        ArrayBufferWriter<byte>? arrayBufferWriter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize<T>(ref Utf8YamlEmitter emitter, T value)
        {
            Resolver.GetFormatterWithVerify<T>().Serialize(ref emitter, value, this);
        }

        public ArrayBufferWriter<byte> GetArrayBufferWriter()
        {
            return arrayBufferWriter ??= new ArrayBufferWriter<byte>(65536);
        }

        public void Reset()
        {
            arrayBufferWriter?.Clear();
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(primitiveValueBuffer);
        }

        public byte[] GetBuffer64() => primitiveValueBuffer;

        // readonly Stack<SequenceStyle> sequenceStyleStack = new();
        // readonly Stack<ScalarStyle> sequenceStyleStack = new();
    }
}
