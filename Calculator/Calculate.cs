using System.Collections.Concurrent;
using System.Text.Json;
using MoreLinq;
using Serilog;

namespace Calculator;

public class Calculate
{
    private static readonly ListEqualityComparer<Face> ListEqualityComparer = new();
    private static readonly FaceListComparer FaceListComparer = new();
    private static readonly JsonSerializerOptions JsonSerializerOptions  = new() { WriteIndented = false, IncludeFields = true };

    public class Result 
    {
        public List<PlayItem> PlayList;
        public List<List<Face>> AllPlays;
        public List<DistributionItem> DistributionList;
        public List<int> PossibleNrOfTricks;
        public Dictionary<List<Face>, PlayItem> RelevantPlays;
        public List<List<Face>> CombinationsInTree;
    }

    public static Result GetResult(IDictionary<List<Face>, List<Item>> bestPlay, List<Face> north, List<Face> south)
    {
        Log.Information("Start GetResult");
        var cardsNS = north.Concat(south).OrderDescending().ToList();
        var combinationsInTree = bestPlay.Keys.OrderBy(x => x.ToList(), FaceListComparer).ToList();
        var distributionList = GetDistributionItems(cardsNS, combinationsInTree);

        foreach (var keyValuePair in bestPlay)
        {
            foreach (var item in keyValuePair.Value)
            {
                item.Combination = keyValuePair.Key;
                foreach (var item1 in GetDescendents(item))
                {
                    item1.Combination = keyValuePair.Key;
                }
            }
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
        
        double GetProbability(Item x) => distributionList[x.Combination].Probability * distributionList[x.Combination].Occurrences;

        void BackTracking()
        {
            var segmentsNS = cardsNS.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
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

    private static IEnumerable<Item> GetDescendents(Item resultItem)
    {
        return resultItem.Children == null ? [] : resultItem.Children.Concat(resultItem.Children.SelectMany(GetDescendents));
    }

    private static Dictionary<List<Face>, DistributionItem> GetDistributionItems(List<Face> cardsNS, List<List<Face>> combinationsInTree)
    {
        var cardsEW = Utils.GetAllCards().Except(cardsNS).ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
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
        return distributionList;
    }

    private class TreeItem
    {
        public List<Face> Combination { get; init; }
        public List<Item> Items { get; init; }
    }
    
    public class Item2
    {
        public List<Face> Combination { get; init; }
        public int Tricks { get; set; }
        public bool IsSubstitute { get; init; }
        public bool IsDifferent { get; init; }
    }  

    public class LineItem
    {
        public List<Face> Line { get; set; }
        public List<Item2> Items2 { get; set; } = [];
        public double Average { get; set; }
        public List<double> Probabilities { get; set; }
    }

    public class Result2
    {
        public List<DistributionItem> DistributionItems { get; init; }
        public List<LineItem> LineItems { get; init; }
        public List<int> PossibleNrOfTricks { get; init; }
        public string Combination { get; init; }
    }

    public static Result2 GetResult2(IDictionary<List<Face>, List<Item>> bestPlay, List<Face> north, List<Face> south)
    {
        Log.Information("Start GetResult2");
        var cardsNS = north.Concat(south).OrderDescending().ToList();
        var combinationsInTree = bestPlay.Keys.OrderBy(x => x.ToList(), FaceListComparer).ToList();
        var distributionList = GetDistributionItems(cardsNS, combinationsInTree);
        
        var treeItems = bestPlay.OrderBy(x => x.Key, FaceListComparer).Select(x => new TreeItem
        {
            Combination = x.Key,
            Items = x.Value.SelectMany(GetDescendents)
                .Where(y => y.Children == null && y.Play.First() is Face.Ace or Face.Two)
                .OrderBy(z => z.Play, FaceListComparer)
                .Select(z => new Item(z.Play.RemoveAfterDummy().ConvertToSmallCards(cardsNS), z.Tricks)).ToList()
        }).ToList();

        var possibleNrOfTricks = bestPlay.SelectMany(x => x.Value).Select(x => x.Tricks).Distinct().OrderDescending().SkipLast(1).ToList();
        var items = AssignLines();

        return new Result2
        {
            DistributionItems = distributionList.Values.ToList(), LineItems = items,
            PossibleNrOfTricks = possibleNrOfTricks, 
            Combination = $"{Utils.CardsToString(north)} - {Utils.CardsToString(south)}" };

        List<LineItem> AssignLines()
        {
            var filename = $"{Utils.CardsToString(north)}-{Utils.CardsToString(south)}.json";
            using var fileStream = new FileStream(Path.Combine(AppContext.BaseDirectory, "etalons-suitplay", filename), FileMode.Open);
            var results = JsonSerializer.Deserialize<(Dictionary<string, List<int>> treesForJson, IEnumerable<string>)>(fileStream, JsonSerializerOptions);
            var lineItems = treeItems.SelectMany(x => x.Items).Select(x => x.OnlySmallCardsEW).Distinct(ListEqualityComparer).Where(y => y.Count > 1).ToList()
                .Select(x =>
                {
                    var data = results.treesForJson.SingleOrDefault(a => a.Key.StartsWith(Utils.CardsToString(x), default)).Value;
                    var dataCounter = 0;
                    var lineItem = new LineItem
                    {
                        Line = x,
                        Items2 = treeItems.Select(y =>
                        {
                            var similarItems = y.Items.Where(z => z.OnlySmallCardsEW.StartsWith(x)).ToList();
                            var bestItem = similarItems.Count != 0 ? similarItems.MaxBy(z => z.Tricks) : GetBestItem(y, x);
                            var item2 = new Item2
                            {
                                Combination = y.Combination, Tricks = bestItem.Tricks,
                                IsSubstitute = similarItems.Count == 0,
                                IsDifferent = data != null && bestItem.Tricks != data[dataCounter++]
                            };
                            return item2;
                        }).OrderBy(y => y.Combination, FaceListComparer).ToList()
                    };
                    lineItem.Average = lineItem.Items2.Average(y => GetProbability(y) * y.Tricks) / lineItem.Items2.Select(GetProbability).Average();
                    lineItem.Probabilities = possibleNrOfTricks.Select(y => lineItem.Items2.Where(z => z.Tricks >= y).Sum(GetProbability) / lineItem.Items2.Sum(GetProbability)).ToList();
                    return lineItem;
                }).OrderByDescending(x => x.Line, FaceListComparer).ToList();
            
            lineItems.RemoveAll(x => lineItems.Any(y => IsBetterLine(y, x)));
            
            return lineItems;

            Item GetBestItem(TreeItem treeItem, List<Face> line)
            {
                var list = treeItem.Items.Where(z => line.Zip(z.OnlySmallCardsEW, (a, b) => (a, b)).All(u => u.a == u.b)).ToList();
                return list.Count == 0 ? new Item([], -1) : list.Where(z => z.OnlySmallCardsEW.Count == list.Max(y => y.OnlySmallCardsEW.Count)).MaxBy(v => v.Tricks);
            }

            static bool IsBetterLine(LineItem first, LineItem second)
            {
                var valueTuples = first.Items2.Zip(second.Items2).ToList();
                return first.Line.SkipLast(1).SequenceEqual(second.Line.SkipLast(1)) && 
                       valueTuples.Any(x => x.First.Tricks > x.Second.Tricks) && !valueTuples.Any(x => x.Second.Tricks > x.First.Tricks);
            }
        }
        double GetProbability(Item2 x) => distributionList[x.Combination].Probability * distributionList[x.Combination].Occurrences;
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
            var calculateBestPlayForCombination = MiniMax.CalculateBestPlayForCombination(north, cardsE, south, cardsW);
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
        var segmentsNS = cardsNS.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        var segments = combination.Select(GetSegment).ToList();
        var similarCombinations = combinationList.Where(x => x.Select(GetSegment).SequenceEqual(segments));
        return similarCombinations;

        int GetSegment(Face face)
        {
            return segmentsNS.FindIndex(x => x.First() < face);
        }
    }
}