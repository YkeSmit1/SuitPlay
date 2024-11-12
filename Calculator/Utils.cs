namespace Calculator;

using Alias = Universal.Common.Mathematics.Math;

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

public static class Utils
{
    public static Face CharToCard(char card)
    {
        return card switch
        {
            '2' => Face.Two,
            '3' => Face.Three,
            '4' => Face.Four,
            '5' => Face.Five,
            '6' => Face.Six,
            '7' => Face.Seven,
            '8' => Face.Eight,
            '9' => Face.Nine,
            'T' => Face.Ten,
            'J' => Face.Jack,
            'Q' => Face.Queen,
            'K' => Face.King,
            'A' => Face.Ace,
            _ => throw new ArgumentOutOfRangeException(nameof(card), card, null)
        };
    }

    public static char CardToChar(Face card)
    {
        return card switch
        {
            Face.SmallCard => 'x',
            Face.Dummy => '_',
            Face.Two => '2',
            Face.Three => '3',
            Face.Four => '4',
            Face.Five => '5',
            Face.Six => '6',
            Face.Seven => '7',
            Face.Eight => '8',
            Face.Nine => '9',
            Face.Ten => 'T',
            Face.Jack => 'J',
            Face.Queen => 'Q',
            Face.King => 'K',
            Face.Ace => 'A',
            _ => throw new ArgumentOutOfRangeException(nameof(card), card, null)
        };
    }

    public static double GetDistributionProbability(int a, int b)
    {
        var combinations = Alias.Factorial(a + b) / (Alias.Factorial(a) * Alias.Factorial(b));
        var nominator = Math.Pow(Alias.Factorial(13), 2) * Alias.Factorial(26 - a - b);
        var denominator = Alias.Factorial(26) * Alias.Factorial(13 - a) * Alias.Factorial(13 - b);
        var res = combinations * (nominator / denominator);
        return res;
    }
}