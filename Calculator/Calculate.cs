using System.Collections.Concurrent;
using System.Text.Json;
using Calculator.Models;
using Cartesian;
using MoreLinq;
using Serilog;

namespace Calculator;

public class Calculate
{
    private static readonly ArrayEqualityComparer<Face> ListEqualityComparer = new();
    private static readonly FaceArrayComparer FaceListComparer = new();
    private static readonly JsonSerializerOptions JsonSerializerOptions  = new() { WriteIndented = false, IncludeFields = true };

    public static Result GetResult(IDictionary<Face[], List<Item>> bestPlay, Face[] north, Face[] south)
    {
        Log.Information("Start GetResult");
        var cardsNS = north.Concat(south).OrderDescending().ToArray();
        var combinationsInTree = bestPlay.Keys.OrderBy(x => x.ToArray(), FaceListComparer).ToList();
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

        var playItems = bestPlayFlattened.Where(x => x.Play.Count() is 3 or 4 or 7 /*&& x.Children.All(y => y.Children.Count > 0)*/)
            .GroupBy(x => x.Play.ConvertToSmallCards(cardsNS), y => y).ToList()
            .ToDictionary(key => key.Key, value =>
            {
                var allCombinations = combinationsInTree.Select(x => value.SingleOrDefault(y => x.SequenceEqual(y.Combination), GetDefaultValue(value.Key, x))).ToList();
                return new PlayItem
                {
                    Play = value.Key,
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
            DistributionList = distributionList.Values.ToList(),
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
                var averages = bestPlayFlattened.Where(x => x.Play.Count() == i + 2 && Utils.IsSmallCard(x.Play[1], segmentsNS))
                    .GroupBy(x => x.Play, y => y).ToList()
                    .Select(x => (play: x.Key, averages: x.Average(y => GetProbability(y) * y.Tricks) / x.Select(GetProbability).Average())).ToList();

                foreach (var item in bestPlayFlattened.Where(x => x.Play.Count() == i && Utils.IsSmallCard(x.Play[1], segmentsNS)))
                {
                    var bestPlayEW = item.Children.GroupBy(x => x.Tricks).OrderBy(x => x.Key).First().MinBy(x => x.Play[i]);
                    var sameAverages = averages.Where(x => x.play.StartsWith(bestPlayEW.Play)).ToList();
                    if (sameAverages.Count == 0) continue;
                    var bestAverages = sameAverages.OrderBy(x => x.averages).Segment((lItem, prevItem, _) => lItem.averages - prevItem.averages > 0.00001).Last().ToList();
                    if (bestAverages.Count > 1)
                        Log.Debug("Duplicate plays found.({@item})", item);
                    var tuple = bestPlayEW.Children.Where(x => bestAverages.Any(y => y.play == x.Play)).ToList();
                    if (tuple.Select(x => x.Tricks).Distinct().Count() != 1)
                        Log.Warning("Duplicate plays found with different values. ({@item})", item);
                    var tricks = tuple.First().Tricks;
                    item.Tricks = tricks;
                }
            }
        }

        Item GetDefaultValue(Cards play, Face[] combination)
        {
            return bestPlay[combination].Where(x => x.Play.First() == play.First()).ToList().MaxBy(x => x.Tricks);
        }
    }

    private static IEnumerable<Item> GetDescendents(Item resultItem)
    {
        return resultItem.Children == null ? [] : resultItem.Children.Concat(resultItem.Children.SelectMany(GetDescendents));
    }

    private static Dictionary<Face[], DistributionItem> GetDistributionItems(Face[] cardsNS, List<Face[]> combinationsInTree)
    {
        var cardsEW = Utils.GetAllCards().Except(cardsNS).ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
        var distributionList = combinationsInTree.ToDictionary(key => key, value =>
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

    public static Result2 GetResult2(IDictionary<Face[], List<Item>> bestPlay, Face[] north, Face[] south)
    {
        Log.Information("Start GetResult2");
        var cardsNS = north.Concat(south).OrderDescending().ToArray();
        var combinationsInTree = bestPlay.Keys.OrderBy(x => x, FaceListComparer).ToList();
        var distributionList = GetDistributionItems(cardsNS, combinationsInTree);
        var possibleNrOfTricks = bestPlay.SelectMany(x => x.Value).Select(x => x.Tricks).Distinct().OrderDescending().SkipLast(1).ToList();

        return new Result2
        {
            DistributionItems = distributionList.Values.ToList(), 
            LineItems = AssignLines(),
            PossibleNrOfTricks = possibleNrOfTricks, 
            North = north,
            South = south
        };

        List<LineItem> AssignLines()
        {
            var treeItems = bestPlay.Select(x => new
            {
                Combination = x.Key,
                Items = x.Value.SelectMany(GetDescendents)
                    .Where(y => y.Children == null)
                    .Select(z => new Item(z.Play.RemoveAfterDummy().ConvertToSmallCards(cardsNS), z.Tricks)).ToList()
            }).OrderBy(x => x.Combination, FaceListComparer).ToList();

            var lines2NdHigh = treeItems.SelectMany(x => x.Items).Where(x => x.Play.Count() > 1 && x.Play.Data[1] != Face.SmallCard).Select(x => x.Play).ToList();
            var lines2NdDummy = treeItems.SelectMany(x => x.Items).Where(x => x.Play.Count() == 1).Select(x => x.Play).ToList();
            var lineItems = CreateLines(treeItems.SelectMany(x => x.Items).Select(x => x.OnlySmallCardsEW).Distinct().Where(y => y.Count() > 1))
                .Select(line =>
                {
                    var lineItem = new LineItem
                    {
                        Line = line,
                        Items2 = treeItems.Select(type =>
                        {
                            var similarItems = GetSimilarItems(type.Items, line);
                            var item2 = new Item2
                            {
                                Combination = type.Combination,
                                Tricks = similarItems.Count != 0 ? similarItems.Select(x => x.Tricks).Distinct().ToArray() : [-1],
                                Probability = distributionList[type.Combination].Probability * distributionList[type.Combination].Occurrences,
                                Count = similarItems.Count,
                            };
                            return item2;
                        }).ToList(),
                    };
                    return lineItem;
                }).ToList();
            
            AddStatistics();
            
            AddSuitPlayStatistics();

            return lineItems;
            
            void AddStatistics()
            {
                foreach (var lineItem in lineItems)
                {
                    lineItem.Average = lineItem.Items2.Average(y => y.Probability * y.Tricks.First()) / lineItem.Items2.Select(z => z.Probability).Average();
                    lineItem.Probabilities = possibleNrOfTricks.Select(y => lineItem.Items2.Where(z => z.Tricks.First() >= y).Sum(z => z.Probability) / lineItem.Items2.Sum(z => z.Probability)).ToList();
                }
            }
            
            void AddSuitPlayStatistics()
            {
                var filename = $"{Utils.CardsToString(north)}-{Utils.CardsToString(south)}.json";
                using var fileStream = new FileStream(Path.Combine(AppContext.BaseDirectory, "etalons-suitplay", filename), FileMode.Open);
                var results = JsonSerializer.Deserialize<(Dictionary<string, List<int>> treesForJson, IEnumerable<string>)>(fileStream, JsonSerializerOptions);
            
                foreach (var lineItem in lineItems)
                {
                    var data = results.treesForJson.SingleOrDefault(a => lineItem.DescriptiveLine.ToString().StartsWith(a.Key)).Value;
                    if (data == null) continue;
                    lineItem.LineInSuitPlay = true;
                    var dataCounter = 0;
                    foreach (var item2 in lineItem.Items2)
                    {
                        item2.IsDifferent = item2.Tricks.First() != -1 && item2.Tricks.First() != data[dataCounter];
                        item2.TricksInSuitPlay = data[dataCounter++];
                    }
                }
            }

            List<Item> GetSimilarItems(List<Item> items, List<Cards> line)
            {
                var similarItems = items.Where(z => line.Any(u => u == z.OnlySmallCardsEW)).ToList();
                if (similarItems.Count != 0) 
                    return similarItems;
                var lines2NdHighItems = items.Where(z => lines2NdHigh.Any(u => u == z.Play)).ToList();
                if (lines2NdHighItems.Count > 0)
                    return lines2NdHighItems;
                var lines2NdDummyItems = items.Where(z => lines2NdDummy.Any(u => u == z.Play)).ToList();
                return lines2NdDummyItems.Count > 0 ? lines2NdDummyItems : similarItems;
            }
        }
    }    
    
    public static List<List<Cards>> CreateLines(IEnumerable<Cards> cardList)
    {
        var result = GetLines(cardList.ToList(), 0);
        
        return result.ToList();
        
        static IEnumerable<List<Cards>> GetLines(List<Cards> cardList, int depth)
        {
            if (depth != 0 && cardList.Where(x => x.Count() > depth).Select(x => x.ToString()[..depth]).Distinct().Count() != 1)
                yield return cardList;
            else
            {
                var groupBy = cardList.Where(x => x.Count() > depth).GroupBy(x => x.Data[depth]).ToList();
                var cardsSmaller = cardList.Where(x => x.Count() <= depth).ToList();
                if (depth % 2 == 1)
                {
                    if (groupBy.Count > 1 &&
                        groupBy.Any(x => GenerateLines(x.Select(y => y).ToList(), depth).Count() > 1))
                    {
                        foreach (var item in CartesianEnumerable.Enumerate(groupBy.Select(x => x.Select(y => y).ToArray())))
                            yield return cardsSmaller.Concat(item).ToList();
                    }
                    else
                    {
                        foreach (var line in GenerateLines(cardList, depth))
                            yield return line;
                    }
                }
                else
                {
                    foreach (var group in groupBy)
                    {
                        foreach (var line in GenerateLines(cardsSmaller.Concat(group.Select(x => x)).ToList(), depth).ToList())
                            yield return line;
                    }
                }
            }

            yield break;

            static IEnumerable<List<Cards>> GenerateLines(List<Cards> cardList, int depth)
            {
                if (cardList.All(x => x.Count() <= depth + 1))
                    yield return cardList;
                else
                    foreach (var line in GetLines(cardList, depth + 1))
                        yield return line;
            }
        }
    }    

    public static IDictionary<Face[], List<Item>> CalculateBestPlay(Face[] north, Face[] south)
    {
        Log.Information("Calculating best play North:{@north} South:{@south}",  north, south);
        var cardsEW = Utils.GetAllCards().Except(north).Except(south).ToArray();
        var combinations = Combinations.AllCombinations(cardsEW.ToList());
        var cardsNS = north.Concat(south).OrderDescending();
        combinations.RemoveAll(faces => SimilarCombinationsCount(combinations, faces, cardsNS) > 0);
        var result = new ConcurrentDictionary<Face[], List<Item>>(ListEqualityComparer);
        Parallel.ForEach(combinations,combination =>
        {
            var cardsE = combination.ToArray();
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
        var reversedList = list.AsEnumerable().Reverse().ToArray();
        var hasSimilar = similarCombinations.Count(x => FaceListComparer.Compare(x.Reverse().ToArray(), reversedList) < 0);
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