using System.Diagnostics;

namespace Calculator;

public enum Suit
{
    Spades = 0,
    Hearts = 1,
    Diamonds = 2,
    Clubs = 3,
    NoTrump = 4
}

public enum Face
{
    Dummy,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace
}

[DebuggerDisplay("{Suit,nq} {Face,nq}")]
public class Card
{
    protected bool Equals(Card other)
    {
        return Suit == other.Suit && Face == other.Face;
    }
    
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Card)obj);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine((int)Suit, (int)Face);
    }

    public Suit Suit;
    public Face Face;
    public Player Player;
}