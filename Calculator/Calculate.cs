using System.Collections.Concurrent;
using System.Diagnostics;
using MoreLinq;
using Serilog;

namespace Calculator;

public class Calculate
{
    private static readonly ListEqualityComparer<Face> ListEqualityComparer = new();
    private static readonly FaceListComparer FaceListComparer = new();

    public class Result 
    {
        public List<PlayItem> PlayList;
        public List<List<Face>> AllPlays;
        public List<DistributionItem> DistributionList;
        public List<int> PossibleNrOfTricks;
        public Dictionary<List<Face>, PlayItem> RelevantPlays;
        public List<List<Face>> CombinationsInTree;
    }

    public class Item(List<Face> play, int tricks, List<Item> children = null)
    {
        private int tricks = tricks;
        public List<Face> Combination { get; set; }
        public List<Face> Play { get; } = play;

        public int Tricks
        {
            get => TranspositionRef?.Tricks ?? tricks;
            set => tricks = value;
        }

        public List<Item> Children { get; set; } = children;
        public Item TranspositionRef { get; init; }
        public int Line { get; set; }
    }

    public static Result GetResult(IDictionary<List<Face>, List<Item>> bestPlay, List<Face> cardsNS)
    {
        Log.Information("Start GetResult");
        var cardsEW = Utils.GetAllCards().Except(cardsNS).ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
        var combinationsInTree = bestPlay.Keys.OrderBy(x => x.ToList(), FaceListComparer).ToList();
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
        }, ListEqualityComparer);

        foreach (var item in bestPlay)
        {
            item.Value.ForEach(x =>
            {
                x.Combination = item.Key;
                GetDescendents(x).ForEach(y => y.Combination = item.Key);
            }); 
        }
        var bestPlayFlattened = bestPlay.SelectMany(x => x.Value.SelectMany(GetDescendents)).ToList();
        Log.Information("BestPlay has {count:n} items", bestPlayFlattened.Count);

        BackTracking();
        var possibleNrOfTricks = bestPlay.SelectMany(x => x.Value).Select(x => x.Tricks).Distinct().OrderDescending().SkipLast(1).ToList();

        var playItems = bestPlayFlattened.Where(x => x.Play.Count is 3 or 4 or 7 /*&& x.Children.All(y => y.Children.Count > 0)*/)
            .GroupBy(x => x.Play.ConvertToSmallCards(cardsNS).ToList(), y => y, ListEqualityComparer).ToList()
            .ToDictionary(key => key.Key, value =>
            {
                var allCombinations = combinationsInTree.Select(x => value.SingleOrDefault(y => x.SequenceEqual(y.Combination), GetDefaultValue(value.Key, x))).ToList();
                return new PlayItem
                {
                    Play = value.Key.ToList(),
                    NrOfTricks = allCombinations.Select(x => x.Tricks).ToList(),
                    Average = allCombinations.Average(x => GetProbability(x) * x.Tricks) / allCombinations.Select(GetProbability).Average(),
                    Probabilities = possibleNrOfTricks.Select(y => allCombinations.Where(x => x.Tricks >= y).Sum(GetProbability) / allCombinations.Sum(GetProbability)).ToList(),
                };
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

        IEnumerable<Item> GetDescendents(Item resultItem)
        {
            return resultItem.Children == null ? [] : resultItem.Children.Concat(resultItem.Children.SelectMany(GetDescendents));
        }
        
        double GetProbability(Item x) => distributionList[x.Combination].Probability * distributionList[x.Combination].Occurrences;

        void BackTracking()
        {
            //DoBackTracking(7);
            DoBackTracking(5);
            DoBackTracking(3);
            return;

            void DoBackTracking(int i)
            {
                var averages = bestPlayFlattened.Where(x => x.Play.Count == i + 2 && Utils.IsSmallCard(x.Play[1], segmentsNS))
                    .GroupBy(x => x.Play, y => y, ListEqualityComparer).ToList()
                    .Select(x => (play: x.Key, averages: x.Average(y => GetProbability(y) * y.Tricks) / x.Select(GetProbability).Average())).ToList();

                foreach (var item in bestPlayFlattened.Where(x => x.Play.Count == i && Utils.IsSmallCard(x.Play[1], segmentsNS)))
                {
                    var bestPlayEW = item.Children.GroupBy(x => x.Tricks).OrderBy(x => x.Key).First().MinBy(x => x.Play[i]);
                    var sameAverages = averages.Where(x => x.play.StartsWith(bestPlayEW.Play)).ToList();
                    if (sameAverages.Count == 0) continue;
                    var bestAverages = sameAverages.OrderBy(x => x.averages).Segment((lItem, prevItem, _) => lItem.averages - prevItem.averages > 0.00001).Last().ToList();
                    if (bestAverages.Count > 1)
                        Log.Debug("Duplicate plays found.({@item})", item);
                    var tuple = bestPlayEW.Children.Where(x => bestAverages.Any(y => y.play.SequenceEqual(x.Play))).ToList();
                    if (tuple.Select(x => x.Tricks).Distinct().Count() != 1)
                        Log.Warning("Duplicate plays found with different values. ({@item})", item);
                    var tricks = tuple.First().Tricks;
                    item.Tricks = tricks;
                }
            }
        }

        Item GetDefaultValue(List<Face> play, List<Face> combination)
        {
            return bestPlay[combination].Where(x => x.Play.First() == play.First()).ToList().MaxBy(x => x.Tricks);
        }
    }
    
    public class TreeItem
    {
        public List<Face> EastHand { get; set; }
        public List<Face> WestHand { get; set; }
        public List<Item> Items { get; init; }
    }
    
    public class Item2
    {
        public List<Face> EastHand { get; init; }
        public List<Face> WestHand { get; set; }
        public int Tricks { get; set; }
    }  
    

    public class LineItem
    {
        public List<Face> Line { get; set; }
        public List<Item2> Items2 { get; set; } = [];
        public double Average { get; set; }
    }

    public class Result2
    {
        public List<TreeItem> TreeItems { get; init; }
        public List<LineItem> LineItems { get; init; }
    }

    public static Result2 GetResult2(IDictionary<List<Face>, List<Item>> play, List<Face> cardsNS)
    {
        var cardsEW = Utils.GetAllCards().Except(cardsNS).ToList();
        var treeItems = play.OrderBy(x => x.Key, FaceListComparer).Select(x => new TreeItem
        {
            EastHand = x.Key.ConvertToSmallCards(cardsNS),
            WestHand = cardsEW.Except(x.Key).ConvertToSmallCards(cardsNS),
            Items = x.Value.SelectMany(GetDescendents)
                .Where(y => y.Children == null && y.Play.First() is Face.Ace or Face.Two)
                .OrderBy(z => z.Play, FaceListComparer)
                .Select(z => new Item(z.Play.RemoveAfterDummy().ConvertToSmallCards(cardsNS), z.Tricks)).ToList()
        }).ToList();
        
        var items = AssignLines();

        return new Result2 {  TreeItems = treeItems, LineItems = items };

        IEnumerable<Item> GetDescendents(Item resultItem)
        {
            return resultItem.Children == null ? [] : resultItem.Children.Concat(resultItem.Children.SelectMany(GetDescendents));
        }

        List<LineItem> AssignLines()
        {
            var lineItems = treeItems.SelectMany(x => x.Items)
                .Select(y => y.Play.OnlySmallCardsEW())
                .Distinct(ListEqualityComparer)
                .Select(y => new LineItem() { Line = y }).Distinct()
                .ToList();
            foreach (var lineItem in lineItems)
            {
                foreach (var treeItem in treeItems)
                {
                    var item = treeItem.Items.Where(x => lineItem.Line.Zip(x.Play.OnlySmallCardsEW(), (a, b) => (a, b)).All(y => y.a == y.b)).ToList();
                    var bestItem = item.MaxBy(x => x.Tricks);
                    lineItem.Items2.Add(new Item2 { EastHand = treeItem.EastHand, WestHand = treeItem.WestHand, Tricks = bestItem.Tricks });
                }
            }

            foreach (var lineItem in lineItems)
            {
                lineItem.Items2 = lineItem.Items2.OrderBy(x => x.EastHand, FaceListComparer).ToList();
            }

            return lineItems;
        }
    }    

    public static IDictionary<List<Face>, List<Item>> CalculateBestPlay(List<Face> north, List<Face> south)
    {
        Log.Information("Calculating best play North:{@north} South:{@south}",  north, south);
        var cardsEW = Utils.GetAllCards().Except(north).Except(south).ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
        var cardsNS = north.Concat(south).OrderDescending();
        combinations.RemoveAll(faces => SimilarCombinationsCount(combinations, faces, cardsNS) > 0);
        var result = new ConcurrentDictionary<List<Face>, List<Item>>(ListEqualityComparer);
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
        var reversedList = list.AsEnumerable().Reverse().ToList();
        var hasSimilar = similarCombinations.Count(x => FaceListComparer.Compare(x.Reverse().ToList(), reversedList) < 0);
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
            return list.SequenceEqual(list.OrderDescending());
        }
    }

    public static int GetTrickCount(IEnumerable<Face> play, Dictionary<Player, List<Face>> initialCards)
    {
        return play.Chunk(4).Where(x => x.First() != Face.Dummy).Count(trick => 
            initialCards.Single(y => y.Value.Contains(trick.Max())).Key is Player.North or Player.South);
    }

    private static List<Item> CalculateBestPlayForCombination(params IEnumerable<Face>[] cards)
    {
        var tree = new List<Item>();
        var transpositionTable = new Dictionary<List<Face>, Item>(new ListEqualityComparer<Face>());
        var initialCards = cards.Select((x, index) => (x, index)).ToDictionary(item => (Player)item.index, item => item.x.ToList());
        var cardsNS = initialCards[Player.North].Concat(initialCards[Player.South]).OrderDescending().ToList();
        var cardsEW = initialCards[Player.East].Concat(initialCards[Player.West]).OrderDescending().ToList();
        FindBestMove();
        return tree;

        void FindBestMove()
        {
            var playedCards = new List<Face>();
            var playableCards = GetPlayableCards(playedCards);
            foreach (var card in playableCards)
            {
                playedCards.Add(card);
                var resultItem = Minimax(playedCards, false);
                tree.Add(resultItem);
                playedCards.RemoveAt(playedCards.Count - 1);
            }
        }

        Item Minimax(List<Face> playedCards, bool maximizingPlayer)
        {
            if (playedCards.Count(card => card != Face.Dummy) == initialCards.Values.Sum(x => x.Count) ||
                playedCards.Chunk(4).Last().First() == Face.Dummy)
            {
                var trickCount = GetTrickCount(playedCards, initialCards);
                return new Item(playedCards.ToList(), trickCount);
            }
            
            if (!cardsEW.Except(playedCards).Any()) 
            {
                var trickCount = GetTrickCount(playedCards, initialCards) +
                                 Math.Max(initialCards[Player.North].Count, initialCards[Player.South].Count) -
                                 playedCards.Chunk(4).Count();
                return new Item(playedCards.ToList(), trickCount);
            }

            if (maximizingPlayer)
            {
                var resultItem = new Item (playedCards.ToList(), int.MinValue, []);
                foreach (var card in GetPlayableCards(playedCards))
                {
                    playedCards.Add(card);
                    var value = Minimax(playedCards, false);
                    resultItem.Tricks = Math.Max(resultItem.Tricks, value.Tricks);
                    resultItem.Children.Add(value);
                    playedCards.RemoveAt(playedCards.Count - 1);
                }
                if (playedCards.Any(x => x == Face.Dummy)) 
                    resultItem.Children = [resultItem.Children.First(x => x.Tricks == resultItem.Tricks)];
                    
                return resultItem;
            }
            else
            {
                var resultItem = new Item ( playedCards.ToList(),  int.MaxValue, []);
                foreach (var card in GetPlayableCards(playedCards))
                {
                    playedCards.Add(card);
                    var value = playedCards.Count % 4 == 0 && transpositionTable.TryGetValue(playedCards.Chunk(4).Select(x => x.Order()).SelectMany(x => x).ToList(), out var item)
                        ? new Item(playedCards.ToList(), 0, []) {TranspositionRef = item}
                        : Minimax(playedCards, true);

                    resultItem.Tricks = Math.Min(resultItem.Tricks, value.Tricks);
                    // if (playedCards.Count % 4 == 0 && !transpositionTable.TryGetValue(playedCards.Chunk(4).Select(x => x.Order()).SelectMany(x => x).ToList(), out _))
                    //     transpositionTable.Add(playedCards.Chunk(4).Select(x => x.Order()).SelectMany(x => x).ToList(), value);

                    resultItem.Children.Add(value);
                    playedCards.RemoveAt(playedCards.Count - 1);
                }

                resultItem.Children.RemoveAll(x => x.Tricks > resultItem.Tricks);

                return resultItem;
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
            
            if (playedCards.Count % 4 == 3)
            {
                var lastTrick = playedCards.Chunk(4).Last();
                var highestCardOtherTeam = ((List<Face>)[lastTrick.First(), lastTrick.Last()]).Max();
                var highestCards = availableCards.Where(x => x > highestCardOtherTeam && highestCardOtherTeam > lastTrick[1]).ToList();
                if (highestCards.Count > 0) return [highestCards.Min()];
            }
            
            var cardsOtherTeam = player is Player.North or Player.South ? cardsEW : cardsNS;
            var availableCardsFiltered = AvailableCardsFiltered(availableCards, cardsOtherTeam, playedCards);

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

    public static IEnumerable<Face> AvailableCardsFiltered(List<Face> availableCards, List<Face> cardsOtherTeam, List<Face> playedCards)
    {
        var playedCardsPreviousTricks = playedCards.SkipLast(playedCards.Count % 4);
        var cardsOtherTeamNotPlayed = cardsOtherTeam.Except(playedCardsPreviousTricks).ToList();
        var segmentsCardsOtherTeam = cardsOtherTeamNotPlayed.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
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