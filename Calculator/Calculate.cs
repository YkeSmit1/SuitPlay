using System.Collections;
using System.Diagnostics;
using MoreLinq.Extensions;

namespace Calculator;

public enum Card
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

public enum Player
{
    North,
    East,
    South,
    West,
    None
}

public class Calculate
{
    private static readonly Player[] PlayersNS = [Player.North, Player.South];
    private static readonly Dictionary<Player, IEnumerable<Card>> InitialCards = new();
    private static Dictionary<IEnumerable<Card>, int> treeWithNumberOfTricksNS;
    private static IEnumerable<Card> allCards;

    public static IEnumerable<(string, double)> CalculateBestPlay(string north, string south)
    {
        allCards = Enum.GetValues<Card>().Except([Card.Dummy, Card.Two, Card.Three, Card.Four, Card.Five, Card.Six, Card.Seven]);
        InitialCards[Player.North] = north.Select(CharToCard);
        InitialCards[Player.South] = south.Select(CharToCard);
        var cardsEW = allCards.Except(InitialCards[Player.North]).Except(InitialCards[Player.South]);
        var combinations = AllCombinations(cardsEW);
        foreach (var combination in combinations)
        {
            InitialCards[Player.East] = combination;
            InitialCards[Player.West] = cardsEW.Except(InitialCards[Player.East]);
            var tree = GenerateTree(InitialCards[Player.North], InitialCards[Player.South], InitialCards[Player.East], InitialCards[Player.West]);
            treeWithNumberOfTricksNS = tree.ToDictionary(play => play, GetTrickCount);
            yield return (
                string.Join('|',
                    treeWithNumberOfTricksNS.Take(3).Select(x =>
                        $"Play: {string.Join(",", x.Key.Select(y => string.Join(" ", y)))} Tricks:{x.Value}")),
                treeWithNumberOfTricksNS.Average(x => x.Value));
        }

        yield break;

        int GetTrickCount(IEnumerable<Card> play)
        {
            return play.Chunk(4).Count(x => PlayersNS.Contains((Player)Enumerable.MaxBy(play.Select((cardsEW1, index) => (cardsEW: cardsEW1, index)), y => y.index).index));
        }
    }
    
    public static Card CharToCard(char card)
    {
        return card switch
        {
            '2' => Card.Two,
            '3' => Card.Three,
            '4' => Card.Four,
            '5' => Card.Five,
            '6' => Card.Six,
            '7' => Card.Seven,
            '8' => Card.Eight,
            '9' => Card.Nine,
            'T' => Card.Ten,
            'J' => Card.Jack,
            'Q' => Card.Queen,
            'K' => Card.King,
            'A' => Card.Ace,
            _ => throw new InvalidOperationException()
        };
    }

    private static char CardToChar(Card card)
    {
        return card switch
        {
            Card.Dummy => 'D',
            Card.Two => '2',
            Card.Three => '3',
            Card.Four => '4',
            Card.Five => '5',
            Card.Six => '6',
            Card.Seven => '7',
            Card.Eight => '8',
            Card.Nine => '9',
            Card.Ten => 'T',
            Card.Jack => 'J',
            Card.Queen => 'Q',
            Card.King => 'K',
            Card.Ace => 'A',
            _ => throw new ArgumentOutOfRangeException(nameof(card), card, null)
        };
    }    

    private static IEnumerable<IEnumerable<Card>> GenerateTree(params IEnumerable<Card>[] hands)
    {
        var permutations = hands.Select(x => x.Permutations()).ToArray();
        var enumerable = permutations[0].Cartesian(permutations[1], permutations[2], permutations[3],
            (x, y, z, u) => new List<IList<Card>> { x, y, z, u });
        var cartesian = enumerable.Select(y => y.SelectMany(x => x));
        Debug.Assert(cartesian.All(x => x.Count() == allCards.Count()));
        return cartesian;
    }

    private static IEnumerable<IEnumerable<T>> AllCombinations<T>(IEnumerable<T> elements)
    {
        List<IEnumerable<T>> ret = [];
        for (var k = 0; k < elements.Count(); k++)
        {
            ret.AddRange(k == 0 ? new[] { Array.Empty<T>() } : Combinations(elements, k));
            ret.AddRange(k == 0 ? new[] { Array.Empty<T>() } : Combinations(elements, k));
        }

        return ret;

        static IEnumerable<IEnumerable<TU>> Combinations<TU>(IEnumerable<TU> elements, int k)
        {
            return k == 0
                ? new[] { Array.Empty<TU>() }
                : elements.SelectMany((e, index) =>
                    Combinations(elements.Skip(index + 1), k - 1).Select(c => new[] { e }.Concat(c)));
        }
    }

    private Card FindBestMove()
    {
        var playedCards = new List<Card>();
        var bestValue = int.MinValue;
        var bestCard = Card.Dummy;
        foreach (var card in GetPlayableCards(playedCards))
        {
            playedCards.Add(card);
            int value = Minimax(playedCards, false);
            playedCards.Remove(card);
            
            if (value > bestValue)
            {
                bestCard = card;
                bestValue = value; 
            } 
        }

        return bestCard;
    }

    private int Minimax(List<Card> playedCards, bool maximizingPlayer)
    {
        if (playedCards.Count == allCards.Count())
            return treeWithNumberOfTricksNS[playedCards];
        var value = maximizingPlayer ? int.MinValue: int.MaxValue;
        foreach (var card in GetPlayableCards(playedCards))
        {
            playedCards.Add(card);
            value = Math.Max(value, Minimax(playedCards, !maximizingPlayer));
            playedCards.Remove(card);
        }

        return value;

    }

    private IEnumerable<Card> GetPlayableCards(IEnumerable<Card> playedCards)
    {
        var availableCards = playedCards.Count() % 4 == 0
            ? GetAvailableCards(playedCards, Player.North).Concat(GetAvailableCards(playedCards, Player.South))
            : GetAvailableCards(playedCards, NextPlayer((Player)(playedCards.Count() % 4)));
        return !availableCards.Any() ? new []{Card.Dummy} : availableCards;
    }

    private static IEnumerable<Card> GetAvailableCards(IEnumerable<Card> playedCards, Player player)
    {
        return InitialCards[player].Except(playedCards.Select((card, index) => (card, index)).Where(x => x.index == (int)player).Select(x => x.card));
    }

    private Player NextPlayer(Player player)
    {
        return player == Player.West ? Player.North : player + 1;
    }
}