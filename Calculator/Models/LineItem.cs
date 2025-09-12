namespace Calculator.Models;

public class LineItem
{
    public List<Cards> Line { get; set; }
    public List<Item2> Items2 { get; set; } = [];
    public double Average { get; set; }
    public List<double> Probabilities { get; set; }
    public bool LineInSuitPlay { get; set; }
    public Cards DescriptiveLine => Line.MaxBy(x => x.Count());
}