namespace Calculator.Models;

public class DistributionItem
{
    public List<Face> West  { get; set; }
    public List<Face> East  { get; set; }
    public int Occurrences { get; set; }
    public double Probability { get; set; }
}