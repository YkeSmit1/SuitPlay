namespace Calculator.Models;

public class Item(Cards play, int tricks, List<Item> children = null)
{
    public Cards Play { get; } = play;
    public Cards OnlySmallCardsEW { get; } = play.OnlySmallCardsEW();
    public int Tricks { get; set; } = tricks;
    public List<Item> Children { get; set; } = children;
}