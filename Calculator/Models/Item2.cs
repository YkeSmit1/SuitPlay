namespace Calculator.Models;

public class Item2
{
    public Face[] Combination { get; init; }
    public int[] Tricks => Items.Count != 0 ? Items.Select(x => x.Tricks).Distinct().ToArray() : [-1];
    public bool IsDifferent { get; set; }
    public int TricksInSuitPlay { get; set; }
    public List<Item> Items { get; init; }

    public Item2 Clone()
    {
        var newItem = new Item2
        {
            Combination = Combination,
            IsDifferent = IsDifferent,
            TricksInSuitPlay = TricksInSuitPlay,
            Items = Items.ToList(),
        };
        return newItem;
    }
}