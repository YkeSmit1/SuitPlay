namespace Calculator.Models;

public class Item2
{
    public Face[] Combination { get; init; }
    public int[] Tricks { get; set; }
    public bool IsDifferent { get; set; }
    public double Probability { get; init; }
    public int TricksInSuitPlay { get; set; }
    public List<Item> Items { get; init; }

    public Item2 Clone()
    {
        var newItem = new Item2
        {
            Combination = Combination,
            Tricks = Tricks,
            IsDifferent = IsDifferent,
            Probability = Probability,
            TricksInSuitPlay = TricksInSuitPlay,
            Items = Items.ToList(),
        };
        return newItem;
    }
}