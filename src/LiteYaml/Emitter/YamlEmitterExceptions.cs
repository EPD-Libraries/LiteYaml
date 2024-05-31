namespace LiteYaml.Emitter;

internal static class YamlEmitterExceptions
{
    public static readonly ArgumentException InvalidEmitState = new("""
        Invalid emit state encountered.
        """);
}
