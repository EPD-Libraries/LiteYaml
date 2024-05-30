using BenchmarkDotNet.Attributes;
using System.Text;
using YamlDotNet.Serialization.NamingConventions;
using YamlLibrary.Benchmark.Examples;
using YamlLibrary.Serialization;

namespace YamlLibrary.Benchmark;

[MemoryDiagnoser]
public class DeserializationBenchmark
{
    const int N = 100;

    byte[]? yamlBytes;
    string? yamlString;

    YamlDotNet.Serialization.IDeserializer yamlDotNetDeserializer = default!;

    [GlobalSetup]
    public void Setup()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Examples", "sample_envoy.yaml");
        yamlBytes = File.ReadAllBytes(path);
        yamlString = Encoding.UTF8.GetString(yamlBytes);
        yamlDotNetDeserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }


    [Benchmark]
    public void YamlDotNet_Deserialize()
    {
        yamlDotNetDeserializer.Deserialize<SampleEnvoy>(yamlString!);
    }

    [Benchmark]
    public void YamlLibrary_Deserialize()
    {
        YamlSerializer.Deserialize<SampleEnvoy>(yamlBytes);
    }
}