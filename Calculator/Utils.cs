﻿using System.Text.Json;
using MoreLinq;
using Serilog;

namespace Calculator;

using Alias = Universal.Common.Mathematics.Math;

public class ListEqualityComparer<T> : IEqualityComparer<List<T>>
{
    public bool Equals(List<T> left, List<T> right)
    {
        if (left == null && right == null)
            return true;
        if (left == null || right == null)
            return false;
        return left.SequenceEqual(right);
    }

    public int GetHashCode(List<T> list)
    {
        return list.Aggregate(19, (current, total) => current * 31 + total.GetHashCode());
    }
}

public class FaceListComparer : IComparer<IList<Face>>
{
    public int Compare(IList<Face> left, IList<Face> right)
    {
        if (left == null)
            return 1;
        if (right == null)
            return -1;
        for (var i = 0; i < left.Count && i < right.Count; i++) {
            var c = left[i].CompareTo(right[i]);
            if (c != 0) return c;
        }
        return left.Count.CompareTo(right.Count);
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
    private static readonly FaceListComparer FaceListComparer = new();
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false, IncludeFields = true };

    public static Face CharToCard(char card)
    {
        return card switch
        {
            'x' => Face.SmallCard,
            '_' => Face.Dummy,
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
    
    public static double GetDistributionProbabilitySpecific(int a, int b)
    {
        return GetDistributionProbability(a, b) / GetDistributionOccurrence(a, b);
    }

    public static double GetDistributionProbability(int a, int b)
    {
        var combinations = Alias.Factorial(a + b) / (Alias.Factorial(a) * Alias.Factorial(b));
        var nominator = Math.Pow(Alias.Factorial(13), 2) * Alias.Factorial(26 - a - b);
        var denominator = Alias.Factorial(26) * Alias.Factorial(13 - a) * Alias.Factorial(13 - b);
        var res = combinations * (nominator / denominator);
        return res;
    }

    private static int GetDistributionOccurrence(int a, int b)
    {
        return (int)(Alias.Factorial(a + b) / (Alias.Factorial(a) * Alias.Factorial(b)));
    }

    public static string CardsToString(IEnumerable<Face> cards)
    {
        return string.Join("", cards.Select(CardToChar));
    }
    
    public static List<Face> StringToCardList(string cards)
    {
        return cards.Select(CharToCard).ToList();
    }
    
    public static Face GetFaceFromDescription(char c)
    {
        return c switch
        {
            'A' => Face.Ace,
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
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
        };
    }
    
    public static string GetSuitDescriptionASCII(Suit suit)
    {
        return suit switch
        {
            Suit.Clubs => "C",
            Suit.Diamonds => "D",
            Suit.Hearts => "H",
            Suit.Spades => "S",
            Suit.NoTrump => "NT",
            _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
        };
    }

    public static IEnumerable<Face> GetAllCards()
    {
        return Enum.GetValues<Face>().Where(x => x >= Face.Two).Reverse();
    }

    public static List<Face> ConvertToSmallCards(this IEnumerable<Face> cards, IEnumerable<Face> cardsNS)
    {
        var enumerable = cardsNS.ToList();
        var segmentsNS = enumerable.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        return cards.Select(x => !enumerable.Contains(x) && IsSmallCard(x, segmentsNS) ? Face.SmallCard : x).ToList();
    }
    
    public static List<Face> RemoveAfterDummy(this IEnumerable<Face> cards)
    {
        var enumerable = cards.ToList();
        return enumerable.IndexOf(Face.Dummy) == -1 ? enumerable : enumerable.Take(enumerable.IndexOf(Face.Dummy) + 1).ToList();
    }
    
    public static List<Face> OnlySmallCardsEW(this IEnumerable<Face> cards)
    {
        var enumerable = cards.ToList();
        return enumerable.TakeWhile(x => enumerable.IndexOf(x) % 2 == 0 || x == Face.SmallCard).ToList();
    }

    public static bool IsSmallCard(Face face, List<IEnumerable<Face>> segmentsNS)
    {
        if (face == Face.Dummy) return false;
        if (segmentsNS.Count <= 1) return true;
        var index = segmentsNS.Last().Last() == Face.Two ? 2 : 1;
        return (int)face < (int)segmentsNS[^index].Last();
    }

    public static void SaveTrees(Calculate.Result result, string filename)
    {
        using var stream = new FileStream(filename, FileMode.Create);
        var treesForJson = result.RelevantPlays.Where(x => x.Key.Count == 3)
            .OrderByDescending(x => x.Value.Play, FaceListComparer)
            .ToDictionary(x => CardsToString(x.Key), x => x.Value.NrOfTricks);
        JsonSerializer.Serialize(stream, (treesForJson, result.CombinationsInTree.Select(CardsToString)), JsonSerializerOptions);
    }

    public static void SetupLogging()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log.txt");
        Log.Logger = new LoggerConfiguration()
            .Destructure.ByTransforming<Calculate.Item>(x => new { x.Play, x.Tricks, x.Combination, Children = x.Children.Count})
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(filePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}