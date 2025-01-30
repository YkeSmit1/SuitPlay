using System.Collections.Concurrent;
using System.Diagnostics;
using MoreLinq;
using Serilog;

namespace Calculator;

public class Calculate
{
    public class Result 
    {
        public List<PlayItem> PlayList;
        public List<List<Face>> AllPlays;
        public List<DistributionItem> DistributionList;
        public List<int> PossibleNrOfTricks;
        public Dictionary<List<Face>, PlayItem> RelevantPlays;
        public List<List<Face>> CombinationsInTree;
    }
    
    public static Result GetResult(IDictionary<List<Face>, List<(List<Face> play, int nrOfTricks)>> bestPlay, List<Face> cardsNS)
    {
        var cardsEW = Utils.GetAllCards().Except(cardsNS).ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
        var combinationsInTree = bestPlay.Keys.OrderBy(x => x.ToList(), new FaceListComparer()).ToList();
        var segmentsNS = cardsNS.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        
        var distributionList = combinationsInTree.ToDictionary(key => key.ToList(), value =>
        {
            var eastHand = value.ToList();
            var westHand = cardsEW.Except(eastHand).ToList();
            var similarCombinationsCount = SimilarCombinations(combinations, westHand, cardsNS).Count();
            return new DistributionItem
            {
                West = westHand.ConvertToSmallCards(cardsNS),
                East = eastHand.ConvertToSmallCards(cardsNS),
                Occurrences = similarCombinationsCount,
                Probability = Utils.GetDistributionProbabilitySpecific(eastHand.Count, westHand.Count) * similarCombinationsCount,
            };
        }, new ListEqualityComparer<Face>());

        var possibleNrOfTricks = bestPlay.SelectMany(x => x.Value).Select(x => x.nrOfTricks).Distinct().OrderByDescending(x => x).SkipLast(1).ToList();

        var filteredTrees = bestPlay.ToDictionary(x => x.Key, y => y.Value.Where(x => x.Item1.Count == 3).ToList(), new ListEqualityComparer<Face>());
        var filteredTrees4 = bestPlay.ToDictionary(x => x.Key, y => y.Value.Where(x => x.Item1.Count == 4).ToList(), new ListEqualityComparer<Face>());
        var filteredTrees7 = bestPlay.ToDictionary(x => x.Key, y => y.Value.Where(x => x.Item1.Count == 7).ToList(), new ListEqualityComparer<Face>());
        
        var averages7 = filteredTrees7
            .SelectMany(x => x.Value, (parent, child) => (combi: parent.Key, child.play, child.nrOfTricks))
            .GroupBy(x => x.play, y => (y.combi, y.nrOfTricks), new ListEqualityComparer<Face>()).ToList()
            .ToDictionary(key => key.Key, value => value.Average(x => GetProbability(x) * x.nrOfTricks) / value.Select(GetProbability).Average());

        var playItems = filteredTrees
            .SelectMany(x => x.Value, (parent, child) => (combi: parent.Key, child.play, nrOfTricks: GetNrOfTricks(parent.Key, child.play, child.nrOfTricks)))
            .GroupBy(x => x.play.ConvertToSmallCards(cardsNS).ToList(), y => (y.combi, y.nrOfTricks), new ListEqualityComparer<Face>()).ToList()
            .ToDictionary(key => key.Key, value => new PlayItem
            {
                Play = value.Key.ToList(),
                NrOfTricks = combinationsInTree.Select(x => value.SingleOrDefault(y => x.SequenceEqual(y.combi), GetDefaultValue(value.Key, x.ToList())).nrOfTricks).ToList(),
                Average = value.Average(x => GetProbability(x) * x.nrOfTricks) / value.Select(GetProbability).Average(),
                Probabilities = possibleNrOfTricks.Select(y => value.Where(x => x.nrOfTricks >= y).Sum(GetProbability) / value.Sum(GetProbability)).ToList(),
            })
            .OrderByDescending(x => x.Value.Average);
        
        var relevantPlays = playItems.Where(x => x.Key[1] == Face.SmallCard ).ToDictionary(key => key.Key, value => value.Value);
        var result = new Result
        {
            CombinationsInTree = combinationsInTree,
            RelevantPlays = relevantPlays,
            PlayList = relevantPlays.Values.ToList(),
            AllPlays = relevantPlays.Keys.ToList(),
            DistributionList = distributionList.Select(x => x.Value).ToList(),
            PossibleNrOfTricks = possibleNrOfTricks.ToList()
        };
        
        return result;

        double GetProbability((List<Face> combi, int nrOfTricks) x) => distributionList[x.combi].Probability;
        
        int GetNrOfTricks(List<Face> combination, List<Face> play, int defaultNrOfTricks)
        {
            if (!Utils.IsSmallCard(play[1], segmentsNS))
                return defaultNrOfTricks;
            var possiblePlays4 = filteredTrees4[combination].Where(y => y.play.Take(3).SequenceEqual(play)).ToList();
            var bestPossiblePlay4 = possiblePlays4.MinBy(x => x.nrOfTricks).play;
            
            var possiblePlays7 = filteredTrees7[combination].Where(y => y.play.Take(4).SequenceEqual(bestPossiblePlay4) && Utils.IsSmallCard(y.play[5], segmentsNS) ).Select(x => x.play).ToList();
            var averages = averages7.Where(x => possiblePlays7.Contains(x.Key, new ListEqualityComparer<Face>())).ToList();
            if (averages.Count == 0)
                return defaultNrOfTricks;
            var bestPossiblePlay7 = averages.MaxBy(x => x.Value).Key;
            var tuple = filteredTrees7[combination].Single(x => x.play.SequenceEqual(bestPossiblePlay7));
            return tuple.nrOfTricks;
        }

        (List<Face> combi, int nrOfTricks) GetDefaultValue(List<Face> play, List<Face> combination)
        {
            return play[1] != Face.SmallCard ? (combination, -1) : (combination, filteredTrees[combination].Where(x => x.play.First() == play.First()).ToList().Max(x => x.nrOfTricks));
        }
    }

    private static void SetupLogging()
    {
        var combine = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log.txt");
        Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(combine, rollingInterval: RollingInterval.Day)
                .CreateLogger();
    }

    public static IDictionary<List<Face>, List<(List<Face>, int)>> CalculateBestPlay(List<Face> north, List<Face> south)
    {
        SetupLogging();
        var cardsEW = Utils.GetAllCards().Except(north).Except(south).ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
        var cardsNS = north.Concat(south).OrderByDescending(x => x);
        combinations.RemoveAll(faces => SimilarCombinationsCount(combinations, faces, cardsNS) > 0);
        var result = new ConcurrentDictionary<List<Face>, List<(List<Face>, int)>>();
        Parallel.ForEach(combinations, combination =>
        {
            var cardsE = combination.ToList();
            var cardsW = cardsEW.Except(cardsE);
            var calculateBestPlayForCombination = CalculateBestPlayForCombination(north, cardsE, south, cardsW);
            result[cardsE] = calculateBestPlayForCombination;
        });
        
        return result;
    }

    public static int SimilarCombinationsCount(IEnumerable<IEnumerable<Face>> combinationList,  IEnumerable<Face> combination, IEnumerable<Face> cardsNS)
    {
        var list = combination.ToList();
        if (list.Count == 0)
            return 0;
        var similarCombinations = SimilarCombinations(combinationList, list, cardsNS);
        var comp = new FaceListComparer();
        var reversedList = list.AsEnumerable().Reverse().ToList();
        var hasSimilar = similarCombinations.Count(x => comp.Compare(x.Reverse().ToList(), reversedList) < 0);
        return hasSimilar;
    }

    private static IEnumerable<IEnumerable<Face>> SimilarCombinations(IEnumerable<IEnumerable<Face>> combinationList, IEnumerable<Face> combination, IEnumerable<Face> cardsNS)
    {
        // ReSharper disable PossibleMultipleEnumeration
        Debug.Assert(combinationList.All(IsOrderedDescending));
        Debug.Assert(IsOrderedDescending(combination));
        Debug.Assert(IsOrderedDescending(cardsNS));
        var segmentsNS = cardsNS.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        var segments = combination.Select(GetSegment).ToList();
        var similarCombinations = combinationList.Where(x => x.Select(GetSegment).SequenceEqual(segments));
        // ReSharper restore PossibleMultipleEnumeration
        return similarCombinations;

        int GetSegment(Face face)
        {
            return segmentsNS.FindIndex(x => x.First() < face);
        }
        
        static bool IsOrderedDescending(IEnumerable<Face> x)
        {
            var list = x.ToList();
            return list.SequenceEqual(list.OrderByDescending(y => y));
        }
    }

    public static int GetTrickCount(IEnumerable<Face> play, Dictionary<Player, List<Face>> initialCards)
    {
        return play.Chunk(4).Where(x => x.First() != Face.Dummy).Count(trick => 
            initialCards.Single(y => y.Value.Contains(trick.Max())).Key is Player.North or Player.South);
    }

    private static List<(List<Face>, int)> CalculateBestPlayForCombination(params IEnumerable<Face>[] cards)
    {
        var tree = new List<(List<Face>, int)>();
        var initialCards = cards.Select((x, index) => (x, index)).ToDictionary(item => (Player)item.index, item => item.x.ToList());
        var cardsNS = initialCards[Player.North].Concat(initialCards[Player.South]).OrderByDescending(x => x).ToList();
        var cardsEW = initialCards[Player.East].Concat(initialCards[Player.West]).OrderByDescending(x => x).ToList();
        FindBestMove();
        return tree;

        void FindBestMove()
        {
            var playedCards = new List<Face>();
            var playableCards = GetPlayableCards(playedCards);
            foreach (var card in playableCards)
            {
                playedCards.Add(card);
                var value = Minimax(playedCards, false);
                tree.Add((playedCards.ToList(), value));
                playedCards.RemoveAt(playedCards.Count - 1);
            }
        }

        int Minimax(List<Face> playedCards, bool maximizingPlayer)
        {
            if (playedCards.Count(card => card != Face.Dummy) == initialCards.Values.Sum(x => x.Count) ||
                playedCards.Chunk(4).Last().First() == Face.Dummy)
            {
                var trickCount = GetTrickCount(playedCards, initialCards);
                return trickCount;
            }

            if (maximizingPlayer)
            {
                var bestValue = int.MinValue;
                foreach (var card in GetPlayableCards(playedCards))
                {
                    playedCards.Add(card);
                    var value = Minimax(playedCards, false);
                    bestValue = Math.Max(bestValue, value);
                    tree.Add((playedCards.ToList(), value));
                    playedCards.RemoveAt(playedCards.Count - 1);
                }
                return bestValue;
            }
            else
            {
                var bestValue = int.MaxValue;
                var cardValueList = new List<(Face, int)>();
                foreach (var card in GetPlayableCards(playedCards))
                {
                    playedCards.Add(card);
                    var value = Minimax(playedCards, true);
                    bestValue = Math.Min(bestValue, value);
                    cardValueList.Add((card, value));
                    playedCards.RemoveAt(playedCards.Count - 1);
                }
                
                var badPlays = cardValueList.Where(x => x.Item2 != bestValue).Select(x => playedCards.Concat([x.Item1]).ToList()).ToList();
                foreach (var play in badPlays)
                {
                    tree.RemoveAll(x => x.Item1.StartsWith(play));
                }

                tree.AddRange(cardValueList.Where(x => x.Item2 == bestValue).Select(x => (playedCards.Concat([x.Item1]).ToList(), bestValue)));

                return bestValue;
            }
        }

        List<Face> GetPlayableCards(List<Face> playedCards)
        {
            var availableCards = (playedCards.Count % 4 == 0
                ? GetAvailableCards(playedCards, Player.North).Concat(GetAvailableCards(playedCards, Player.South))
                : GetAvailableCards(playedCards, NextPlayer(GetCurrentPlayer(playedCards)))).ToList();
            return availableCards.Count == 0 ? [Face.Dummy] : availableCards;
        }

        IEnumerable<Face> GetAvailableCards(List<Face> playedCards, Player player)
        {
            if (player == Player.None)
                return [];
            
            var availableCards = initialCards[player].Except(playedCards).ToList();
            if (availableCards.Count == 0)
                return [];
            
            // if (playedCards.Count % 4 == 1)
            // {
            //     var lowestCard = availableCards.Min();
            //     var lastTrick = playedCards.Chunk(4).Last();
            //     var coverCards = availableCards.Where(x => x > lastTrick.First()).ToList();
            //     if (coverCards.Count == 0)
            //         return new List<Face> {lowestCard};
            //     var coverCard = coverCards.Min();
            //     return coverCard - lowestCard == 2
            //         ? new List<Face> {coverCard}
            //         : new List<Face> {lowestCard, coverCard }.Distinct();
            // }
            //
            if (playedCards.Count % 4 == 3)
            {
                var lastTrick = playedCards.Chunk(4).Last();
                var highestCardOtherTeam = ((List<Face>)[lastTrick.First(), lastTrick.Last()]).Max();
                var highestCards = availableCards.Where(x => x > highestCardOtherTeam && highestCardOtherTeam > lastTrick[1]).ToList();
                return highestCards.Count > 0 ? [highestCards.Min()] : [availableCards.Min()];
            }
            
            var cardsOtherTeam = player is Player.North or Player.South ? cardsEW : cardsNS;
            var availableCardsFiltered = AvailableCardsFiltered(availableCards, cardsOtherTeam);

            return availableCardsFiltered.ToList();
        }

        Player GetCurrentPlayer(List<Face> playedCards)
        {
            var lastTrick = playedCards.Chunk(4).Last();
            if (playedCards.Count == 0 || lastTrick.Length == 4 || lastTrick.First() == Face.Dummy)
                return Player.None;
            var playerToLead = initialCards.Single(x => x.Value.Contains(lastTrick.First())).Key;
            return (Player)((lastTrick.Length + (int)playerToLead) % 4 - 1);
        }
    }

    public static IEnumerable<Face> AvailableCardsFiltered(List<Face> availableCards, List<Face> cardsOtherTeam)
    {
        //return availableCards.Where(card => !availableCards.Any(x => cardsOtherTeam.Where(y => y < x).SequenceEqual(cardsOtherTeam.Where(z => z < card)) && card > x));
        var segmentsCardsOtherTeam = cardsOtherTeam.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        var segmentsAvailableCards = availableCards.Segment((item, prevItem, _) => GetSegment(item) != GetSegment(prevItem));
        var availableCardsFiltered = segmentsAvailableCards.Select(x => x.Last());
        return availableCardsFiltered;
        
        int GetSegment(Face card)
        {
            return segmentsCardsOtherTeam.FindIndex(x => x.First() < card);
        }
    }

    private static Player NextPlayer(Player player)
    {
        return player == Player.West ? Player.North : player + 1;
    }
}