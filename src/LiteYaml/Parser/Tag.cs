#nullable enable
using System;

namespace LiteYaml.Parser
{
    public class Tag(string handle, string suffix) : ITokenContent
    {
        public string Handle { get; } = handle;
        public string Suffix { get; } = suffix;

        public override string ToString() => $"{Handle}{Suffix}";

        public bool Equals(string tagString)
        {
            if (tagString.Length != Handle.Length + Suffix.Length)
            {
                return false;
            }
            var handleIndex = tagString.IndexOf(Handle, StringComparison.Ordinal);
            if (handleIndex < 0)
            {
                return false;
            }
            return tagString.IndexOf(Suffix, handleIndex, StringComparison.Ordinal) > 0;
        }
    }
}

