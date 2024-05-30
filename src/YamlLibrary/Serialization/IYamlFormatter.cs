#nullable enable
using YamlLibrary.Emitter;
using YamlLibrary.Parser;

namespace YamlLibrary.Serialization
{
    public interface IYamlFormatter
    {
    }

    public interface IYamlFormatter<T> : IYamlFormatter
    {
        void Serialize(ref Utf8YamlEmitter emitter, T value, YamlSerializationContext context);
        T Deserialize(ref YamlParser parser, YamlDeserializationContext context);
    }
}
