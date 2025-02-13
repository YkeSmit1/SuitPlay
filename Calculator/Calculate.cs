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

    private class Item(List<Face> combination, List<Face> play, int tricks)
    {
        public List<Face> Combination { get; } = combination;
        public List<Face> Play { get; } = play;
        public int Tricks { get; set; } = tricks;
    }

    public static Result GetResult(IDictionary<List<Face>, List<(List<Face> play, int tricks)>> bestPlay, List<Face> cardsNS)
    {
        Log.Information("Start GetResult");
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

        var plays = bestPlay.SelectMany(x => x.Value, (parent, child) => new Item(parent.Key, child.play, child.tricks)).ToList();
        Log.Information("Backtracking started");
        BackTracking();
        Log.Information("Backtracking ended");
        var possibleNrOfTricks = bestPlay.SelectMany(x => x.Value).Select(x => x.tricks).Distinct().OrderByDescending(x => x).SkipLast(1).ToList();

        var playItems = plays.Where(x => x.Play.Count == 3)
            .GroupBy(x => x.Play.ConvertToSmallCards(cardsNS).ToList(), y => (combi: y.Combination, nrOfTricks: y.Tricks), new ListEqualityComparer<Face>()).ToList()
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
        
        Log.Information("End GetResult");
        return result;

        double GetProbability((List<Face> combi, int nrOfTricks) x) => distributionList[x.combi].Probability;

        void BackTracking()
        {
            //DoBackTracking(7);
            DoBackTracking(5);
            DoBackTracking(3);
            return;

            void DoBackTracking(int i)
            {
                var averages = plays.Where(x => x.Play.Count == i + 2 && Utils.IsSmallCard(x.Play[1], segmentsNS))
                    .GroupBy(x => x.Play, y => (combi: y.Combination, nrOfTricks: y.Tricks), new ListEqualityComparer<Face>()).ToList()
                    .Select(x => (play: x.Key, averages: x.Average(y => GetProbability(y) * y.nrOfTricks) / x.Select(GetProbability).Average())).ToList();

                foreach (var item in plays.Where(x => x.Play.Count == i && Utils.IsSmallCard(x.Play[1], segmentsNS)))
                {
                    var bestPlayEW = bestPlay[item.Combination].Where(y => y.play.Count == i + 1 && y.play.StartsWith(item.Play)).ToList().MinBy(z => z.tricks).play;
                    var bestAverages = averages.Where(x => x.play.StartsWith(bestPlayEW)).OrderBy(x => x.averages).Segment((lItem, prevItem, _) => lItem.averages - prevItem.averages > 0.00001).Last().ToList();
                    if (bestAverages.Count > 1) 
                        Log.Debug("Duplicate plays found.({@item})", item);
                    var tuple = bestPlay[item.Combination].Where(x => bestAverages.Any(y => y.play.SequenceEqual(x.play))).ToList();
                    if (tuple.Select(x => x.tricks).Distinct().Count() != 1) 
                        Log.Warning("Duplicate plays found with different values. ({@item})", item);
                    var valueTuple = plays.Single(x => x.Combination.SequenceEqual(item.Combination) && x.Play.SequenceEqual(tuple.First().play));
                    Log.Debug("Backtracking for {@item} : {@valueTuple}", item, valueTuple);
                    var tricks = valueTuple.Tricks;
                    item.Tricks = tricks;
                }
            }
        }

        (List<Face> combi, int nrOfTricks) GetDefaultValue(List<Face> play, List<Face> combination)
        {
            return play[1] != Face.SmallCard ? (combination, -1) : (combination, bestPlay[combination].Where(x => x.play.First() == play.First()).ToList().Max(x => x.tricks));
        }
    }

    public static IDictionary<List<Face>, List<(List<Face>, int)>> CalculateBestPlay(List<Face> north, List<Face> south)
    {
        Log.Information("Calculating best play North:{@north} South:{@south}",  north, south);
        var cardsEW = Utils.GetAllCards().Except(north).Except(south).ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
        var cardsNS = north.Concat(south).OrderByDescending(x => x);
        combinations.RemoveAll(faces => SimilarCombinationsCount(combinations, faces, cardsNS) > 0);
        var result = new ConcurrentDictionary<List<Face>, List<(List<Face>, int)>>(new ListEqualityComparer<Face>());
        Parallel.ForEach(combinations, combination =>
        {
            var cardsE = combination.ToList();
            var cardsW = cardsEW.Except(cardsE);
            var calculateBestPlayForCombination = CalculateBestPlayForCombination(north, cardsE, south, cardsW);
            result[cardsE] = calculateBestPlayForCombination;
        });
        
        Log.Information("CalculateBestPlay ended");
        return result;
    }

    public static int SimilarCombinationsCount(IEnumerable<IEnumerable<Face>> combinationList,  IEnumerable<Face> combination, IEnumerable<Face> cardsNS)
    {
        var list = combination.ToList();
        var similarCombinations = SimilarCombinations(combinationList, list, cardsNS);
        var hasSimilar = similarCombinations.Count(x => new FaceListComparer().Compare(x.ToList(), list) < 0);
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
                
                var badPlays = cardValueList.Where(x => x.Item2 != bestValue).Select(x => playedCards.Concat([x.Item1]).ToList());
                badPlays.ForEach(play => tree.RemoveAll(x => x.Item1.StartsWith(play)));
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