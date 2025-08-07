namespace Calculator;

public class LineItem
{
    public List<Face> Line { get; set; }
    public List<Item2> Items2 { get; set; } = [];
    public double Average { get; set; }
    public List<double> Probabilities { get; set; }
}