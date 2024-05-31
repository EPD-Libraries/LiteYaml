#nullable enable
using System;

namespace LiteYaml.Annotations
{
    public enum NamingConvention
    {
        LowerCamelCase,
        UpperCamelCase,
        SnakeCase,
        KebabCase,
    }

    [AttributeUsage(AttributeTargets.Class |
                    AttributeTargets.Struct |
                    AttributeTargets.Interface |
                    AttributeTargets.Enum,
        Inherited = false)]
    public class YamlObjectAttribute(NamingConvention namingConvention = NamingConvention.LowerCamelCase) : Attribute
    {
        public NamingConvention NamingConvention { get; } = namingConvention;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class YamlMemberAttribute(string? name = null) : Attribute
    {
        public string? Name { get; } = name;
        public int Order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class YamlIgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class YamlConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface |
                    AttributeTargets.Class,
        AllowMultiple = true,
        Inherited = false)]
    public class YamlObjectUnionAttribute(string tagString, Type subType) : Attribute
    {
        public string Tag { get; } = tagString;
        public Type SubType { get; } = subType;
    }

    /// <summary>
    /// Preserve for Unity IL2CPP(internal but used for code generator)
    /// </summary>
    /// <remarks>
    /// > For 3rd party libraries that do not want to take on a dependency on UnityEngine.dll, it is also possible to define their own PreserveAttribute. The code stripper will respect that too, and it will consider any attribute with the exact name "PreserveAtribute" as a reason not to strip the thing it is applied on, regardless of the namespace or assembly of the attribute.
    /// </remarks>
    public sealed class PreserveAttribute : Attribute
    {
    }
}
