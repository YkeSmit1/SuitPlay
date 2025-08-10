namespace Calculator.Models;

public class Result2
{
    public List<DistributionItem> DistributionItems { get; init; }
    public List<LineItem> LineItems { get; init; }
    public List<int> PossibleNrOfTricks { get; init; }
    public List<Face> North { get; init; }
    public List<Face> South { get; init; }
    
}