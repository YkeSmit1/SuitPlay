namespace Calculator;

public class Result 
{
    public List<PlayItem> PlayList;
    public List<List<Face>> AllPlays;
    public List<DistributionItem> DistributionList;
    public List<int> PossibleNrOfTricks;
    public Dictionary<List<Face>, PlayItem> RelevantPlays;
    public List<List<Face>> CombinationsInTree;
}