namespace Calculator;

public class CalculateOptions
{
    internal static readonly CalculateOptions DefaultCalculateOptions = new();
    public IList<Face> CardsInSuit { get; init; }
    public bool FilterBadPlaysByEW { get; init; }
    public bool UsePruning { get; init; } = true;
}