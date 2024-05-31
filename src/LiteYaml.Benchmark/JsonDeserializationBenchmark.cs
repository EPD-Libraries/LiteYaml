using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization.NamingConventions;
using LiteYaml.Benchmark.Examples;
using LiteYaml.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace LiteYaml.Benchmark;

[MemoryDiagnoser]
public class JsonDeserializationBenchmark
{
    byte[]? jsonBytes;
    string? jsonString;

    YamlDotNet.Serialization.IDeserializer yamlDotNetDeserializer = default!;

    readonly System.Text.Json.JsonSerializerOptions systemTextJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    readonly JsonSerializerSettings newtonsoftJsonSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };


    [GlobalSetup]
    public void Setup()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Examples", "sample_envoy.json");
        jsonBytes = File.ReadAllBytes(path);
        jsonString = Encoding.UTF8.GetString(jsonBytes);

        yamlDotNetDeserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    }

    [Benchmark]
    public void YamlDotNet_Deserialize()
    {
        yamlDotNetDeserializer.Deserialize<SampleEnvoy>(jsonString!);
    }

    [Benchmark]
    public void SystemTextJson_Deserialize()
    {
        JsonSerializer.Deserialize<SampleEnvoy>(jsonBytes, systemTextJsonOptions);
    }

    [Benchmark]
    public void NewtonsoftJson_Deserialize()
    {
        JsonConvert.DeserializeObject<SampleEnvoy>(jsonString!, newtonsoftJsonSettings);
    }

    [Benchmark]
    public void LiteYaml_Deserialize()
    {
        YamlSerializer.Deserialize<SampleEnvoy>(jsonBytes);
    }
}
