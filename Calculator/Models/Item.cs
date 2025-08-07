namespace Calculator.Models;

public class Item(List<Face> play, int tricks, List<Item> children = null)
{
    private int tricks = tricks;
    public List<Face> Combination { get; set; }
    public List<Face> Play { get; } = play;
    public List<Face> OnlySmallCardsEW { get; } = play.OnlySmallCardsEW();

    public int Tricks
    {
        get => TranspositionRef?.Tricks ?? tricks;
        set => tricks = value;
    }

    public List<Item> Children { get; set; } = children;
    public Item TranspositionRef { get; init; }
}