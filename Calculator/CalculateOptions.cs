namespace Calculator;

public class CalculateOptions
{
    internal static readonly CalculateOptions DefaultCalculateOptions = new();
    public List<Face> CardsInSuit { get; init; }
    public bool UsePruning { get; init; } = true;
}