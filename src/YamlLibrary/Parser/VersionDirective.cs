#nullable enable
namespace YamlLibrary.Parser
{
    struct VersionDirective : ITokenContent
    {
        public readonly int Major;
        public readonly int Minor;

        public VersionDirective(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }
    }
}

