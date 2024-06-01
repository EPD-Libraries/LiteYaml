namespace LiteYaml.Parser;

public struct Marker(int position, int line, int col)
{
    public int Position = position;
    public int Line = line;
    public int Col = col;

    public override readonly string ToString()
    {
        return $"Line: {Line}, Col: {Col}, Idx: {Position}";
    }
}

