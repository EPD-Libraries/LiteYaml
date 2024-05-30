using BenchmarkDotNet.Attributes;
using System.Text;
using YamlLibrary.Serialization;

namespace YamlLibrary.Benchmark;

[MemoryDiagnoser]
public class DynamicDeserializationBenchmark
{
    byte[]? yamlBytes;
    string? yamlString;

    YamlDotNet.Serialization.IDeserializer yamlDotNetDeserializer = default!;

    [GlobalSetup]
    public void Setup()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Examples", "sample_envoy.yaml");
        yamlBytes = File.ReadAllBytes(path);
        yamlString = Encoding.UTF8.GetString(yamlBytes);
        yamlDotNetDeserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
    }


    [Benchmark]
    public void YamlDotNet_Deserialize()
    {
        yamlDotNetDeserializer.Deserialize<dynamic>(yamlString!);
    }

    [Benchmark]
    public void YamlLibrary_Deserialize()
    {
        YamlSerializer.Deserialize<dynamic>(yamlBytes);
    }
}