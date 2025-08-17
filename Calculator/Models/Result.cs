namespace Calculator.Models;

public class Result 
{
    public List<PlayItem> PlayList { get; init; }
    public List<Cards> AllPlays { get; init; }
    public List<DistributionItem> DistributionList { get; init; }
    public List<int> PossibleNrOfTricks { get; init; }
    public Dictionary<Cards, PlayItem> RelevantPlays { get; init; }
    public List<Face[]> CombinationsInTree { get; init; }
}