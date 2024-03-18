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
    private static IEnumerable<Card> cardsWest;

    public static IEnumerable<(IEnumerable<Card>, (Card, int))> CalculateBestPlay(string north, string south)
    {
        allCards = Enum.GetValues<Card>().Except([Card.Dummy, Card.Two, Card.Three, Card.Four, Card.Five, Card.Six]);
        InitialCards[Player.North] = north.Select(CharToCard);
        InitialCards[Player.South] = south.Select(CharToCard);
        var cardsEW = allCards.Except(InitialCards[Player.North]).Except(InitialCards[Player.South]).ToList();
        var combinations = AllCombinations(cardsEW);
        foreach (var combination in combinations)
        {
            InitialCards[Player.East] = combination;
            cardsWest = cardsEW.Except(InitialCards[Player.East]);
            InitialCards[Player.West] = cardsWest;
            yield return (combination, FindBestMove());
        }
    }

    public static int GetTrickCount(IEnumerable<Card> play)
    {
        return play.Chunk(4).Where(x => x.First() != Card.Dummy).Count(trick =>
            PlayersNS.Contains((Player)trick.Select((card, index) => (card, index)).MaxBy((y) => y.card).index));
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

    private static List<IEnumerable<T>> AllCombinations<T>(IEnumerable<T> elements)
    {
        List<IEnumerable<T>> ret = [];
        for (var k = 0; k <= elements.Count(); k++)
        {
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

    private static (Card, int) FindBestMove()
    {
        var playedCards = new List<Card>();
        var bestValue = int.MinValue;
        var bestCard = Card.Dummy;
        var playableCards = GetPlayableCards(playedCards);
        foreach (var card in playableCards)
        {
            playedCards.Add(card);
            var value = Minimax(playedCards, false);
            playedCards.RemoveAt(playedCards.Count - 1);
            
            if (value > bestValue)
            {
                bestCard = card;
                bestValue = value; 
            } 
        }

        return (bestCard, bestValue);
    }

    private static int Minimax(IList<Card> playedCards, bool maximizingPlayer)
    {
        if (playedCards.Count(card => card != Card.Dummy) == allCards.Count() || playedCards.Chunk(4).Last().First() == Card.Dummy)
        {
            return GetTrickCount(playedCards);
        }

        if (maximizingPlayer)
        {
            var value = int.MinValue;
            foreach (var card in GetPlayableCards(playedCards))
            {
                playedCards.Add(card);
                value = Math.Max(value, Minimax(playedCards, false));
                playedCards.RemoveAt(playedCards.Count - 1);
            }
            return value;
            
        }
        else
        {
            var value = int.MaxValue;
            foreach (var card in GetPlayableCards(playedCards))
            {
                playedCards.Add(card);
                value = Math.Min(value, Minimax(playedCards, true));
                playedCards.RemoveAt(playedCards.Count - 1);
            }
            return value;
        }
    }

    private static List<Card> GetPlayableCards(IList<Card> playedCards)
    {
        var availableCards = playedCards.Count % 4 == 0
            ? GetAvailableCards(playedCards, Player.North).Concat(GetAvailableCards(playedCards, Player.South))
            : GetAvailableCards(playedCards, NextPlayer(GetCurrentPlayer(playedCards)));
        return (!availableCards.Any() ? new []{Card.Dummy} : availableCards).ToList();
    }

    private static IEnumerable<Card> GetAvailableCards(IList<Card> playedCards, Player player)
    {
        if (player >= Player.None)
            return [];
        var playedCardsByPlayer = playedCards.Where(x => InitialCards[player].Contains(x));
        var availableCards = InitialCards[player].Except(playedCardsByPlayer);
        return availableCards;
    }

    private static Player NextPlayer(Player player)
    {
        return player == Player.West ? Player.North : player + 1;
    }

    private static Player GetCurrentPlayer(IList <Card> playedCards)
    {
        var lastTrick = playedCards.Chunk(4).Last();
        if (playedCards.Count == 0 || lastTrick.Length == 4 || lastTrick.First() == Card.Dummy)
            return Player.None;
        var playerToLead = InitialCards.Single(x => x.Value.Contains(lastTrick.First())).Key;
        return (Player)((lastTrick.Length + (int)playerToLead) % 4 - 1);
    }
}