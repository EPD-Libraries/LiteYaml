using Microsoft.CodeAnalysis;

namespace LiteYaml.SourceGenerator;

public class ReferenceSymbols
{
    public static ReferenceSymbols? Create(Compilation compilation)
    {
        var yamlObjectAttribute = compilation.GetTypeByMetadataName("LiteYaml.Annotations.YamlObjectAttribute");
        if (yamlObjectAttribute is null)
            return null;

        return new ReferenceSymbols
        {
            YamlObjectAttribute = yamlObjectAttribute,
            YamlMemberAttribute = compilation.GetTypeByMetadataName("LiteYaml.Annotations.YamlMemberAttribute")!,
            YamlIgnoreAttribute = compilation.GetTypeByMetadataName("LiteYaml.Annotations.YamlIgnoreAttribute")!,
            YamlConstructorAttribute = compilation.GetTypeByMetadataName("LiteYaml.Annotations.YamlConstructorAttribute")!,
            YamlObjectUnionAttribute = compilation.GetTypeByMetadataName("LiteYaml.Annotations.YamlObjectUnionAttribute")!,
            NamingConventionEnum = compilation.GetTypeByMetadataName("LiteYaml.Annotations.NamingConvention")!
        };
    }

    public INamedTypeSymbol YamlObjectAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlMemberAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlIgnoreAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlConstructorAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlObjectUnionAttribute { get; private set; } = default!;
    public INamedTypeSymbol NamingConventionEnum { get; private set; } = default!;
}
