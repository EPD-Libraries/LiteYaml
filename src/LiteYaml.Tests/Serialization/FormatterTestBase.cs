using NUnit.Framework;
using LiteYaml.Internal;
using LiteYaml.Serialization;

namespace LiteYaml.Tests.Serialization
{
    public class FormatterTestBase
    {
        protected static string Serialize<T>(T value)
        {
            return YamlSerializer.SerializeToString(value);
        }

        protected static T Deserialize<T>(string yaml)
        {
            var bytes = StringEncoding.Utf8.GetBytes(yaml);
            var result = YamlSerializer.Deserialize<T>(bytes);
            Assert.That(result, Is.InstanceOf<T>());
            return result;
        }
    }
}