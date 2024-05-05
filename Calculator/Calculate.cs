using System.Collections.Concurrent;
using MoreLinq;

namespace Calculator;

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

public enum Player
{
    North,
    East,
    South,
    West,
    None
}

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

public class Calculate
{
    private static readonly Player[] PlayersNS = [Player.North, Player.South];
    private static IList<CardFace> allCards;
    
    public static IEnumerable<IGrouping<IList<CardFace>, int>> GetAverageTrickCount(string north, string south, IList<CardFace> cardsInSuit = null)
    {
        allCards = cardsInSuit ?? Enum.GetValues<CardFace>().Except([CardFace.Dummy]).ToList();
        var tree = CalculateBestPlay(north, south);
        //LogTreeForPlay(tree, cards);
        var groupedTricks = tree.Values.SelectMany(x => x).GroupBy(
            x => x.Item1,
            x => x.Item2,
            new ListComparer<CardFace>());
        
        var averageTrickCountOrdered = groupedTricks.OrderBy(x => x.Key.Count).ThenBy(z => z.Key.First());
        return averageTrickCountOrdered;
    }

    private static ConcurrentDictionary<List<CardFace>, List<(IList<CardFace>, int)>> CalculateBestPlay(string north, string south)
    {
        var cardsEW = allCards.Except(north.Select(CharToCard).ToList()).Except(south.Select(CharToCard).ToList()).ToList();
        var combinations = AllCombinations(cardsEW);
        var cardsN = north.Select(CharToCard);
        var cardsS = south.Select(CharToCard);
        ConcurrentDictionary<List<CardFace>, List<(IList<CardFace>, int)>> results = [];
        Parallel.ForEach(combinations, combination =>
        {
            var enumerable = combination.ToList();
            var cardsW = cardsEW.Except(enumerable);
            var calculateBestPlayForCombination = CalculateBestPlayForCombination(cardsN, cardsS, enumerable, cardsW);
            // Remove suboptimal plays
            RemoveBadPlays();
            results[enumerable] = calculateBestPlayForCombination;
            return;

            void RemoveBadPlays()
            {
                RemoveBadPlaysSingle(calculateBestPlayForCombination, 3);
            }
            
        });
        return results;    
    }
    
    private static void LogTreeForPlay(ConcurrentDictionary<List<CardFace>,List<(IList<CardFace>, int)>> tree, CardFace[] cards)
    {
        foreach (var combination in tree)
        {
            foreach (var play in combination.Value.Where(play => play.Item1.StartsWith(cards) && play.Item1.Count < 5))
            {
                Console.WriteLine($"East:{string.Join(',', combination.Key)}\t\t{PlayToString(play)}");
            }
        }
    }

    public static void RemoveBadPlaysSingle(List<(IList<CardFace> play, int tricks)> bestPlays, int nrOfCards)
    {
        var cardPlays = bestPlays.Where(x => x.play.Count == nrOfCards).ToList();
        bestPlays.RemoveAll(x => cardPlays.Where(HasBetterPlay).Any(y => y.play.SequenceEqual(x.play)));
        return;

        bool HasBetterPlay((IList<CardFace> play, int tricks) playToCheck)
        {
            var similarPlays = cardPlays.Where(x => IsSimilar(x.play, playToCheck.play)).ToList();
            return similarPlays.Count != 0 && similarPlays.All(play => play.tricks < playToCheck.tricks);
            
            static bool IsSimilar(IList<CardFace> a, IList<CardFace> b)
            {
                return a[0] == b[0] && a[1] != b[1];
            }
        }
    }

    public static int GetTrickCount(IEnumerable<CardFace> play)
    {
        return play.Chunk(4).Where(x => x.First() != CardFace.Dummy).Count(trick =>
            PlayersNS.Contains((Player)trick.Select((card, index) => (card, index)).MaxBy((y) => y.card).index));
    }

    private static CardFace CharToCard(char card)
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
        
    private static List<(IList<CardFace>, int)> CalculateBestPlayForCombination(params IEnumerable<CardFace>[] cards)
    {
        var tree = new List<(IList<CardFace>, int)>();
        var initialCards = new Dictionary<Player, IList<CardFace>>
        {
            [Player.North] = cards[0].ToList(),
            [Player.South] = cards[1].ToList(),
            [Player.East] = cards[2].ToList(),
            [Player.West] = cards[3].ToList()
        };
        FindBestMove();
        return tree;

        void FindBestMove()
        {
            var playedCards = new List<CardFace>();
            var playableCards = GetPlayableCards(playedCards);
            foreach (var card in playableCards)
            {
                playedCards.Add(card);
                var value = Minimax(playedCards, int.MinValue, int.MaxValue, false);
                tree.Add((playedCards.Select(x => x).ToList(), value));
                playedCards.RemoveAt(playedCards.Count - 1);
            }
        }

        int Minimax(IList<CardFace> playedCards, int alpha, int beta, bool maximizingPlayer)
        {
            if (playedCards.Count(card => card != CardFace.Dummy) == allCards.Count ||
                playedCards.Chunk(4).Last().First() == CardFace.Dummy)
            {
                return GetTrickCount(playedCards);
            }

            if (maximizingPlayer)
            {
                var bestValue = int.MinValue;
                foreach (var card in GetPlayableCards(playedCards))
                {
                    playedCards.Add(card);
                    var value = Minimax(playedCards, alpha, beta, false);
                    bestValue = Math.Max(bestValue, value);
                    tree.Add((playedCards.Select(x => x).ToList(), value));
                    playedCards.RemoveAt(playedCards.Count - 1);
                    alpha = Math.Max(alpha, bestValue);
                    if (bestValue >= beta)
                        break;
                }
                return bestValue;
            }
            else
            {
                var bestValue = int.MaxValue;
                foreach (var card in GetPlayableCards(playedCards))
                {
                    playedCards.Add(card);
                    var value = Minimax(playedCards, alpha, beta, true);
                    bestValue = Math.Min(bestValue, value);
                    tree.Add((playedCards.Select(x => x).ToList(), value));
                    playedCards.RemoveAt(playedCards.Count - 1);
                    beta = Math.Min(beta, bestValue);
                    if (bestValue <= alpha)
                        break;
                }
                return bestValue;
            }
        }

        List<CardFace> GetPlayableCards(IList<CardFace> playedCards)
        {
            var availableCards = (playedCards.Count % 4 == 0
                ? GetAvailableCards(playedCards, Player.North).Concat(GetAvailableCards(playedCards, Player.South))
                : GetAvailableCards(playedCards, NextPlayer(GetCurrentPlayer(playedCards)))).ToList();
            return availableCards.Count == 0 ? [CardFace.Dummy] : availableCards;
        }

        IEnumerable<CardFace> GetAvailableCards(IList<CardFace> playedCards, Player player)
        {
            if (player >= Player.None)
                return [];
            var playedCardsByPlayer = playedCards.Where(x => initialCards[player].Contains(x));
            var availableCards = initialCards[player].Except(playedCardsByPlayer).ToList();
            availableCards.RemoveAll(x => availableCards.Contains(x + 1));
            return availableCards;
        }

        Player GetCurrentPlayer(IList<CardFace> playedCards)
        {
            var lastTrick = playedCards.Chunk(4).Last();
            if (playedCards.Count == 0 || lastTrick.Length == 4 || lastTrick.First() == CardFace.Dummy)
                return Player.None;
            var playerToLead = initialCards.Single(x => x.Value.Contains(lastTrick.First())).Key;
            return (Player)((lastTrick.Length + (int)playerToLead) % 4 - 1);
        }
    }
    
    public static Player NextPlayer(Player player)
    {
        return player == Player.West ? Player.North : player + 1;
    }

    public static List<string> PlaysToString(IEnumerable<(IList<CardFace>, int)> play)
    {
        return play.Select(PlayToString).ToList();
    }

    public static List<string> GroupingsToString(IEnumerable<IGrouping<IList<CardFace>, int>> play)
    {
        return play.Select(GroupingToString).ToList();
    }

    private static string PlayToString((IList<CardFace>, int) x)
    {
        return $"Count:{x.Item1.Count} Play:{string.Join(",", x.Item1)} Tricks:{x.Item2}";
    }

    private static string GroupingToString(IGrouping<IList<CardFace>, int> x)
    {
        return $"Count:{x.Key.Count} Play:{string.Join(",", x.Key)} Tricks:{string.Join(";", x)}";
    }
}