#nullable enable
using System;
using LiteYaml.Emitter;
using LiteYaml.Parser;

namespace LiteYaml.Serialization
{
    public class UriFormatter : IYamlFormatter<Uri>
    {
        public static readonly UriFormatter Instance = new();

        public void Serialize(ref Utf8YamlEmitter emitter, Uri value, YamlSerializationContext context)
        {
            emitter.WriteString(value.ToString());
        }

        public Uri Deserialize(ref YamlParser parser, YamlDeserializationContext context)
        {
            if (parser.TryGetScalarAsString(out var scalar) && scalar != null)
            {
                var uri = new Uri(scalar, UriKind.RelativeOrAbsolute);
                parser.Read();
                return uri;
            }
            throw new YamlSerializerException($"Cannot detect a scalar value of Uri : {parser.CurrentEventType} {parser.GetScalarAsString()}");
        }
    }
}
