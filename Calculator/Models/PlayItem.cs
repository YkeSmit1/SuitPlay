namespace Calculator.Models;

public class PlayItem
{
    public List<Face> Play { get; init; }
    public List<int> NrOfTricks { get; set; }
    public double Average { get; set; }
    public List<double> Probabilities { get; set; }
}