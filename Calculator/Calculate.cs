using System.Diagnostics;

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
    private static IEnumerable<Card> allCards;

    public static IEnumerable<IEnumerable<(Card, int)>> CalculateBestPlay(string north, string south)
    {
        allCards = Enum.GetValues<Card>().Except([Card.Dummy, Card.Two, Card.Three, Card.Four, Card.Five, Card.Six, Card.Seven, Card.Eight]);
        InitialCards[Player.North] = north.Select(CharToCard);
        InitialCards[Player.South] = south.Select(CharToCard);
        var cardsEW = allCards.Except(InitialCards[Player.North]).Except(InitialCards[Player.South]);
        var combinations = AllCombinations(cardsEW);
        foreach (var combination in combinations)
        {
            InitialCards[Player.East] = combination;
            InitialCards[Player.West] = cardsEW.Except(InitialCards[Player.East]);
            yield return FindBestMove();
            break;
        }
    }

    public static int GetTrickCount(IEnumerable<Card> play)
    {
        return play.Chunk(4).Count(trick =>
            PlayersNS.Contains((Player)Enumerable.MaxBy(trick.Select((card, index) => (card, index)), (y) => y.card).index));
    }

    private static Card CharToCard(char card)
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

    private static IEnumerable<(Card, int)> FindBestMove()
    {
        var playedCards = new List<Card>();
        var bestValue = int.MinValue;
        var bestCard = Card.Dummy;
        var playableCards = GetPlayableCards(playedCards);
        foreach (var card in playableCards)
        {
            playedCards.Add(card);
            var value = Minimax(playedCards, false);
            playedCards.Remove(card);
            yield return (card, value);
            
            if (value > bestValue)
            {
                bestCard = card;
                bestValue = value; 
            } 
        }

        //return (bestCard, bestValue);
    }

    private static int Minimax(ICollection<Card> playedCards, bool maximizingPlayer)
    {
        if (playedCards.Count(card => card != Card.Dummy) == allCards.Count())
        {
            var trickCount = GetTrickCount(playedCards);
            Console.WriteLine($"Cards:{string.Join(",", playedCards)} Tricks:{trickCount}");
            return trickCount;
        }

        if (maximizingPlayer)
        {
            var value = int.MinValue;
            var playableCards = GetPlayableCards(playedCards);
            foreach (var card in playableCards)
            {
                playedCards.Add(card);
                value = Math.Max(value, Minimax(playedCards, false));
                playedCards.Remove(card);
            }
            return value;
            
        }
        else
        {
            var value = int.MaxValue;
            var playableCards = GetPlayableCards(playedCards);
            foreach (var card in playableCards)
            {
                playedCards.Add(card);
                value = Math.Min(value, Minimax(playedCards, true));
                playedCards.Remove(card);
            }
            return value;
        }
    }

    private static IEnumerable<Card> GetPlayableCards(IEnumerable<Card> playedCards)
    {
        var availableCards = playedCards.Count() % 4 == 0
            ? GetAvailableCards(playedCards, Player.North)
                .Concat(GetAvailableCards(playedCards, Player.South))
            : GetAvailableCards(playedCards, NextPlayer((Player)(playedCards.Count() % 4)));
        return !availableCards.Any() ? new []{Card.Dummy} : availableCards;
    }

    private static IEnumerable<Card> GetAvailableCards(IEnumerable<Card> playedCards, Player player)
    {
        var playedCardsByPlayer = playedCards.Select((card, index) => (card, index)).Where(x => x.index == (int)player).Select(x => x.card);
        return InitialCards[player].Except(playedCardsByPlayer);
    }

    private static Player NextPlayer(Player player)
    {
        return player == Player.West ? Player.North : player + 1;
    }
}