namespace Calculator.Models;

public class DistributionItem
{
    public Face[] West  { get; set; }
    public Face[] East  { get; set; }
    public int Occurrences { get; set; }
    public double Probability { get; set; }
}