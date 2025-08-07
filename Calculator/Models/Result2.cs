namespace Calculator.Models;

public class Result2
{
    public List<DistributionItem> DistributionItems { get; init; }
    public List<LineItem> LineItems { get; init; }
    public List<int> PossibleNrOfTricks { get; init; }
    public string Combination { get; init; }
}