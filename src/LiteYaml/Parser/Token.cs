#nullable enable
namespace LiteYaml.Parser
{
    readonly struct Token(TokenType type, ITokenContent? content = null)
    {
        public readonly TokenType Type = type;
        public readonly ITokenContent? Content = content;

        public override string ToString() => $"{Type} \"{Content}\"";
    }
}

