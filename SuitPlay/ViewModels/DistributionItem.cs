using Calculator;

namespace SuitPlay.ViewModels;

public class TricksItem
{
    public char Line  { get; set; }
    public int NrOfTricks { get; set; }
}

public class DistributionItem
{
    public List<Face> West  { get; set; }
    public List<Face> East  { get; set; }
    public int Occurrences { get; set; }
    public double Probability { get; set; }
    public List<TricksItem> Tricks { get; set; }
}