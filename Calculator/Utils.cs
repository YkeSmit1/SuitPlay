namespace Calculator;

public class ListComparer<T> : IEqualityComparer<IList<T>>
{
    public bool Equals(IList<T> left, IList<T> right)
    {
        if (left == null && right == null)
            return true;
        if (left == null || right == null)
            return false;
        return left.SequenceEqual(right);
    }

    public int GetHashCode(IList<T> list)
    {
        return list.Aggregate(19, (current, total) => current * 31 + total.GetHashCode());
    }
}

public enum Player
{
    North,
    East,
    South,
    West,
    None
}

public enum CardFace
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

public static class Utils
{
    public static CardFace CharToCard(char card)
    {
        return card switch
        {
            '2' => CardFace.Two,
            '3' => CardFace.Three,
            '4' => CardFace.Four,
            '5' => CardFace.Five,
            '6' => CardFace.Six,
            '7' => CardFace.Seven,
            '8' => CardFace.Eight,
            '9' => CardFace.Nine,
            'T' => CardFace.Ten,
            'J' => CardFace.Jack,
            'Q' => CardFace.Queen,
            'K' => CardFace.King,
            'A' => CardFace.Ace,
            _ => throw new ArgumentOutOfRangeException(nameof(card), card, null)
        };
    }

    private static char CardToChar(CardFace card)
    {
        return card switch
        {
            CardFace.Dummy => 'D',
            CardFace.Two => '2',
            CardFace.Three => '3',
            CardFace.Four => '4',
            CardFace.Five => '5',
            CardFace.Six => '6',
            CardFace.Seven => '7',
            CardFace.Eight => '8',
            CardFace.Nine => '9',
            CardFace.Ten => 'T',
            CardFace.Jack => 'J',
            CardFace.Queen => 'Q',
            CardFace.King => 'K',
            CardFace.Ace => 'A',
            _ => throw new ArgumentOutOfRangeException(nameof(card), card, null)
        };
    }
}