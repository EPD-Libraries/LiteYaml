using Microsoft.CodeAnalysis;

namespace YamlLibrary.SourceGenerator;

public class ReferenceSymbols
{
    public static ReferenceSymbols? Create(Compilation compilation)
    {
        var yamlObjectAttribute = compilation.GetTypeByMetadataName("YamlLibrary.Annotations.YamlObjectAttribute");
        if (yamlObjectAttribute is null)
            return null;

        return new ReferenceSymbols {
            YamlObjectAttribute = yamlObjectAttribute,
            YamlMemberAttribute = compilation.GetTypeByMetadataName("YamlLibrary.Annotations.YamlMemberAttribute")!,
            YamlIgnoreAttribute = compilation.GetTypeByMetadataName("YamlLibrary.Annotations.YamlIgnoreAttribute")!,
            YamlConstructorAttribute = compilation.GetTypeByMetadataName("YamlLibrary.Annotations.YamlConstructorAttribute")!,
            YamlObjectUnionAttribute = compilation.GetTypeByMetadataName("YamlLibrary.Annotations.YamlObjectUnionAttribute")!,
            NamingConventionEnum = compilation.GetTypeByMetadataName("YamlLibrary.Annotations.NamingConvention")!
        };
    }

    public INamedTypeSymbol YamlObjectAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlMemberAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlIgnoreAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlConstructorAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlObjectUnionAttribute { get; private set; } = default!;
    public INamedTypeSymbol NamingConventionEnum { get; private set; } = default!;
}
