namespace Calculator.Models;

public class Result 
{
    public List<PlayItem> PlayList { get; init; }
    public List<List<Face>> AllPlays { get; init; }
    public List<DistributionItem> DistributionList { get; init; }
    public List<int> PossibleNrOfTricks { get; init; }
    public Dictionary<List<Face>, PlayItem> RelevantPlays { get; init; }
    public List<List<Face>> CombinationsInTree { get; init; }
}