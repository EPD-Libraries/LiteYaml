#nullable enable
using LiteYaml.Emitter;
using LiteYaml.Parser;

namespace LiteYaml.Serialization.Formatters
{
    public class BooleanFormatter : IYamlFormatter<bool>
    {
        public static readonly BooleanFormatter Instance = new();

        public void Serialize(ref Utf8YamlEmitter emitter, bool value, YamlSerializationContext context)
        {
            emitter.WriteBool(value);
        }

        public bool Deserialize(ref YamlParser parser, YamlDeserializationContext context)
        {
            bool result = parser.GetScalarAsBool();
            parser.Read();
            return result;
        }
    }

    public class NullableBooleanFormatter : IYamlFormatter<bool?>
    {
        public static readonly NullableBooleanFormatter Instance = new();

        public void Serialize(ref Utf8YamlEmitter emitter, bool? value, YamlSerializationContext context)
        {
            if (value == null)
            {
                emitter.WriteNull();
            }
            else
            {
                emitter.WriteBool(value.Value);
            }
        }

        public bool? Deserialize(ref YamlParser parser, YamlDeserializationContext context)
        {
            if (parser.IsNullScalar())
            {
                parser.Read();
                return default;
            }

            bool result = parser.GetScalarAsBool();
            parser.Read();
            return result;
        }
    }
}
