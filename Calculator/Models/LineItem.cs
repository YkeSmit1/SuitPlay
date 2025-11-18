namespace Calculator.Models;

public class LineItem
{
    public List<Cards> Line { get; init; }
    public List<Item2> Items2 { get; set; } = [];
    public double Average { get; set; }
    public List<double> Probabilities { get; set; }
    public bool LineInSuitPlay { get; set; }
    public List<Cards> GeneratedLines { get; init; } = [];
    public Cards LongestLine => Line.MaxBy(x => x.Count()); 
    public string Header => $"{LongestLine};{string.Join(";", GeneratedLines)}";
    public Cards LongestLineIncludingGenerated => Line.Concat(GeneratedLines).MaxBy(x => x.Count());
}