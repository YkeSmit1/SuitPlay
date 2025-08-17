namespace Calculator.Models;

public class Item2
{
    public Face[] Combination { get; init; }
    public int Tricks { get; set; }
    public bool IsSubstitute { get; init; }
    public bool IsDifferent { get; set; }
    public double Probability { get; init; }
}