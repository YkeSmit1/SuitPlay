using System.Collections.Concurrent;
using MoreLinq;

namespace Calculator;

public class Calculate
{
    private static readonly Player[] PlayersNS = [Player.North, Player.South];
    private static IList<Face> allCards = Enum.GetValues<Face>().Except([Face.Dummy]).ToList();
    private static CalculateOptions options = CalculateOptions.DefaultCalculateOptions;

    public static IEnumerable<IGrouping<IList<Face>, int>> GetAverageTrickCount(string north, string south)
    {
        return GetAverageTrickCount(north, south, CalculateOptions.DefaultCalculateOptions);
    }
    public static IEnumerable<IGrouping<IList<Face>, int>> GetAverageTrickCount(string north, string south, CalculateOptions calculateOptions)
    {
        options = calculateOptions;
        allCards = options?.CardsInSuit ?? Enum.GetValues<Face>().Except([Face.Dummy]).ToList();
        var tree = CalculateBestPlay(north, south);
        //LogTreeForPlay(tree, cards);
        var groupedTricks = tree.Values.SelectMany(x => x).GroupBy(
            x => x.Item1,
            x => x.Item2,
            new ListComparer<Face>());
        
        var averageTrickCountOrdered = groupedTricks.OrderBy(x => x.Key.Count).ThenBy(z => z.Key.First());
        return averageTrickCountOrdered;
    }

    public static ConcurrentDictionary<List<Face>, List<(IList<Face>, int)>> CalculateBestPlay(string north, string south)
    {
        var cardsEW = allCards.Except(north.Select(Utils.CharToCard).ToList()).Except(south.Select(Utils.CharToCard).ToList()).ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
        var cardsN = north.Select(Utils.CharToCard);
        var cardsS = south.Select(Utils.CharToCard);
        ConcurrentDictionary<List<Face>, List<(IList<Face>, int)>> results = [];
        Parallel.ForEach(combinations, combination =>
        {
            var enumerable = combination.ToList();
            var cardsW = cardsEW.Except(enumerable);
            var calculateBestPlayForCombination = CalculateBestPlayForCombination(cardsN, cardsS, enumerable, cardsW);
            // Remove suboptimal plays
            if (options.FilterBadPlaysByEW)
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
    
    private static void LogTreeForPlay(ConcurrentDictionary<List<Face>,List<(IList<Face>, int)>> tree, Face[] cards)
    {
        foreach (var combination in tree)
        {
            foreach (var play in combination.Value.Where(play => play.Item1.StartsWith(cards) && play.Item1.Count < 5))
            {
                Console.WriteLine($"East:{string.Join(',', combination.Key)}\t\t{PlayToString(play)}");
            }
        }
    }

    public static void RemoveBadPlaysSingle(List<(IList<Face> play, int tricks)> bestPlays, int nrOfCards)
    {
        var cardPlays = bestPlays.Where(x => x.play.Count == nrOfCards).ToList();
        bestPlays.RemoveAll(x => cardPlays.Where(HasBetterPlay).Any(y => y.play.SequenceEqual(x.play)));
        return;

        bool HasBetterPlay((IList<Face> play, int tricks) playToCheck)
        {
            var similarPlays = cardPlays.Where(x => IsSimilar(x.play, playToCheck.play)).ToList();
            return similarPlays.Count != 0 && similarPlays.All(play => play.tricks < playToCheck.tricks);
            
            static bool IsSimilar(IList<Face> a, IList<Face> b)
            {
                return a[0] == b[0] && a[1] != b[1];
            }
        }
    }

    public static int GetTrickCount(IEnumerable<Card> play)
    {
        return play.Chunk(4).Where(x => x.First().Face != Face.Dummy).Count(trick =>
            PlayersNS.Contains(trick.MaxBy(x => x.Face).Player));
    }
       
    private static List<(IList<Face>, int)> CalculateBestPlayForCombination(params IEnumerable<Face>[] cards)
    {
        var tree = new List<(IList<Face>, int)>();
        var initialCards = new Dictionary<Player, IList<Card>>
        {
            [Player.North] = cards[0].Select(x => new Card() {Face = x, Player = Player.North}).ToList(),
            [Player.South] = cards[1].Select(x => new Card() {Face = x, Player = Player.South}).ToList(),
            [Player.East] = cards[2].Select(x => new Card() {Face = x, Player = Player.East}).ToList(),
            [Player.West] = cards[3].Select(x => new Card() {Face = x, Player = Player.West}).ToList()
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
                tree.Add((playedCards.Select(x => x.Face).ToList(), value));
                playedCards.RemoveAt(playedCards.Count - 1);
            }
        }

        int Minimax(IList<Card> playedCards, int alpha, int beta, bool maximizingPlayer)
        {
            if (playedCards.Count(card => card.Face != Face.Dummy) == allCards.Count ||
                playedCards.Chunk(4).Last().First().Face == Face.Dummy)
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
                    tree.Add((playedCards.Select(x => x.Face).ToList(), value));
                    playedCards.RemoveAt(playedCards.Count - 1);
                    alpha = Math.Max(alpha, bestValue);
                    if (options.UsePruning && bestValue >= beta)
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
                    tree.Add((playedCards.Select(x => x.Face).ToList(), value));
                    playedCards.RemoveAt(playedCards.Count - 1);
                    beta = Math.Min(beta, bestValue);
                    if (options.UsePruning && bestValue <= alpha)
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
            return availableCards.Count == 0 ? [new Card {Face = Face.Dummy}] : availableCards;
        }

        IEnumerable<Card> GetAvailableCards(IList<Card> playedCards, Player player)
        {
            if (player >= Player.None)
                return [];
            var availableCards = initialCards[player].Except(playedCards).ToList();
            availableCards.RemoveAll(x => availableCards.Any(y => x.Face == y.Face + 1));
            //SortAvailableCards();

            void SortAvailableCards()
            {
                var lastTrick = playedCards.Chunk(4).Last();
                var indexPlayer = lastTrick.Length;
                // switch (indexPlayer)
                // {
                //     case 1: availableCards = availableCards.OrderBy(x => );
                // }
            }

            return availableCards;
        }

        Player GetCurrentPlayer(IList<Card> playedCards)
        {
            var lastTrick = playedCards.Chunk(4).Last();
            if (playedCards.Count == 0 || lastTrick.Length == 4 || lastTrick.First().Face == Face.Dummy)
                return Player.None;
            var playerToLead = initialCards.Single(x => x.Value.Contains(lastTrick.First())).Key;
            return (Player)((lastTrick.Length + (int)playerToLead) % 4 - 1);
        }
    }

    private static Player NextPlayer(Player player)
    {
        return player == Player.West ? Player.North : player + 1;
    }

    public static List<string> PlaysToString(IEnumerable<(IList<Face>, int)> play)
    {
        return play.Select(PlayToString).ToList();
    }

    public static List<string> GroupingsToString(IEnumerable<IGrouping<IList<Face>, int>> play)
    {
        return play.Select(GroupingToString).ToList();
    }

    private static string PlayToString((IList<Face>, int) x)
    {
        return $"Count:{x.Item1.Count} Play:{string.Join(",", x.Item1)} Tricks:{x.Item2}";
    }

    private static string GroupingToString(IGrouping<IList<Face>, int> x)
    {
        return $"Count:{x.Key.Count} Play:{string.Join(",", x.Key)} Tricks:{string.Join(";", x)}";
    }
}