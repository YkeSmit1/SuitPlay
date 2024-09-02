namespace Calculator;



public class CalculateOptions
{
    internal static readonly CalculateOptions DefaultCalculateOptions = new CalculateOptions();
    public IList<Face> CardsInSuit { get; set; }
    public bool FilterBadPlaysByEW { get; set; } = false;
    public bool UsePruning { get; set; } = true;
}