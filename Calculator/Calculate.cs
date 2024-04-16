using System.Collections.Concurrent;

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
    private static IList<Card> allCards;
    
    public static IEnumerable<IGrouping<IList<Card>, int>> GetAverageTrickCount(string north, string south, IList<Card> cardsInSuit)
    {
        allCards = cardsInSuit;
        var tree = CalculateBestPlay(north, south);
        var groupedTricks = tree.GroupBy(
            x => x.Item1,
            x => x.Item2,
            new ListComparer<Card>());
        
        var averageTrickCountOrdered = groupedTricks.OrderBy(x => x.Key.Count).ThenBy(z => z.Key.First());
        return averageTrickCountOrdered;
    }
    
    private static List<(IList<Card>, int )> CalculateBestPlay(string north, string south)
    {
        var cardsEW = allCards.Except(north.Select(CharToCard).ToList()).Except(south.Select(CharToCard).ToList()).ToList();
        var combinations = AllCombinations(cardsEW);
        var cardsN = north.Select(CharToCard);
        var cardsS = south.Select(CharToCard);
        ConcurrentBag<List<(IList<Card>, int)>> results = [];
        Parallel.ForEach(combinations, combination =>
        {
            var enumerable = combination.ToList();
            var cardsW = cardsEW.Except(enumerable);
            var calculateBestPlayForCombination = CalculateBestPlayForCombination(cardsN, cardsS, enumerable, cardsW);
            // Remove suboptimal plays
            RemoveBadPlays();
            results.Add(calculateBestPlayForCombination);
            return;

            void RemoveBadPlays()
            {
                //RemoveBadPlaysSingle(3);
                return;

                void RemoveBadPlaysSingle(int nrOfCards)
                {
                    var cardPlays = calculateBestPlayForCombination.Where(x => x.Item1.Count == nrOfCards).ToList();
                    var cardSubOptimalPays = cardPlays.Where(x =>
                        cardPlays.Any(y => y.Item1.Take(nrOfCards - 2).SequenceEqual(x.Item1.Take(nrOfCards - 2)) && (nrOfCards % 2 == 0 ? y.Item2 > x.Item2 : y.Item2 < x.Item2))).ToList();
                    calculateBestPlayForCombination.RemoveAll(x => cardSubOptimalPays.Any(y => y.Item1.SequenceEqual(x.Item1)));
                }
            }
            
        });
        return results.SelectMany(x => x).ToList();    
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
            _ => throw new ArgumentOutOfRangeException(nameof(card), card, null)
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
        
    private static List<(IList<Card>, int)> CalculateBestPlayForCombination(params IEnumerable<Card>[] cards)
    {
        var tree = new List<(IList<Card>, int)>();
        var initialCards = new Dictionary<Player, IList<Card>>
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
            var playedCards = new List<Card>();
            var playableCards = GetPlayableCards(playedCards);
            foreach (var card in playableCards)
            {
                playedCards.Add(card);
                var value = Minimax(playedCards, int.MinValue, int.MaxValue, false);
                tree.Add((playedCards.Select(x => x).ToList(), value));
                playedCards.RemoveAt(playedCards.Count - 1);
            }
        }

        int Minimax(IList<Card> playedCards, int alpha, int beta, bool maximizingPlayer)
        {
            if (playedCards.Count(card => card != Card.Dummy) == allCards.Count ||
                playedCards.Chunk(4).Last().First() == Card.Dummy)
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

        List<Card> GetPlayableCards(IList<Card> playedCards)
        {
            var availableCards = (playedCards.Count % 4 == 0
                ? GetAvailableCards(playedCards, Player.North).Concat(GetAvailableCards(playedCards, Player.South))
                : GetAvailableCards(playedCards, NextPlayer(GetCurrentPlayer(playedCards)))).ToList();
            return availableCards.Count == 0 ? [Card.Dummy] : availableCards;
        }

        IEnumerable<Card> GetAvailableCards(IList<Card> playedCards, Player player)
        {
            if (player >= Player.None)
                return [];
            var playedCardsByPlayer = playedCards.Where(x => initialCards[player].Contains(x));
            var availableCards = initialCards[player].Except(playedCardsByPlayer).ToList();
            availableCards.RemoveAll(x => availableCards.Contains(x + 1));
            return availableCards;
        }

        static Player NextPlayer(Player player)
        {
            return player == Player.West ? Player.North : player + 1;
        }

        Player GetCurrentPlayer(IList<Card> playedCards)
        {
            var lastTrick = playedCards.Chunk(4).Last();
            if (playedCards.Count == 0 || lastTrick.Length == 4 || lastTrick.First() == Card.Dummy)
                return Player.None;
            var playerToLead = initialCards.Single(x => x.Value.Contains(lastTrick.First())).Key;
            return (Player)((lastTrick.Length + (int)playerToLead) % 4 - 1);
        }
    }

    public static List<string> PlayToString(IEnumerable<(IList<Card>, int)> play)
    {
        return play.Select(x => $"Count:{x.Item1.Count} Play:{string.Join(",", x.Item1)} Tricks:{x.Item2}").ToList();
    }
    
    public static List<string> GroupingToString(IEnumerable<IGrouping<IList<Card>, int>> play)
    {
        return play.Select(x => $"Count:{x.Key.Count} Play:{string.Join(",", x.Key)} Tricks:{string.Join(";", x)}").ToList();
    }
}