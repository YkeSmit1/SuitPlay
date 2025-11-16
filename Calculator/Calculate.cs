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
            
            RemoveBadPlays();

            var lines2NdHigh = treeItems.SelectMany(x => x.Items).Where(x => x.Play.Count() > 1 && x.Play.Data[1] != Face.SmallCard).Select(x => x.Play).ToList();
            var lines2NdDummy = treeItems.SelectMany(x => x.Items).Where(x => x.Play.Count() == 1).Select(x => x.Play).ToList();
            var lineItems = CreateLines(treeItems.SelectMany(x => x.Items).Select(x => x.OnlySmallCardsEW).Distinct().Where(x => x.Count() > 1).OrderDescending())
                .Select(line =>
                {
                    var lineItem = new LineItem
                    {
                        Line = line,
                        Items2 = treeItems.Select(type =>
                        {
                            var items = GetSimilarItems(type.Items, line);
                            var item2 = new Item2
                            {
                                Combination = type.Combination,
                                Items = items
                            };
                            return item2;
                        }).ToList(),
                    };
                    return lineItem;
                }).ToList();

            RemoveDuplicateLines();
            CreateExtraLines(1);
            CreateExtraLines(3);
            CreateExtraLines(5);
            AddStatistics();
            AddSuitPlayStatistics();

            return lineItems.OrderByDescending(x => x.LongestLine).ToList();
            
            void RemoveBadPlays()
            {
                RemoveBadPlaysForTrick(0);
                RemoveBadPlaysForTrick(1);
                return;

                void RemoveBadPlaysForTrick(int i)
                {
                    var pos = i * 4;
                    
                    foreach (var treeItem in treeItems)
                    {
                        treeItem.Items.RemoveAll(item => IsBadPlay(item, treeItem.Items));
                    }

                    return;

                    bool IsBadPlay(Item item, List<Item> items)
                    {
                        if (item.Play.ToString().Length < pos + 2) return false;
                        var differentPlays = items.Where(x => UpToPos(x.Play) == UpToPos(item.Play) && AtPos(x.Play) != AtPos(item.Play)).ToList();
                        if (differentPlays.Count == 0) return false;
                        var differentPlaysTricks = differentPlays.GroupBy(x => x.Play[pos + 1]).Select(x => x.Select(y => y.Tricks)).ToList();

                        var samePlays = items.Where(x => UpToPos(x.Play) == UpToPos(item.Play) && AtPos(x.Play) == AtPos(item.Play));
                        var samePlaysTricks = samePlays.Select(y => y.Tricks).ToArray();
                        var isBadPlay = differentPlaysTricks.Any(x => IsBetterPlay(x.ToArray(), samePlaysTricks) == -1);
                        return isBadPlay;

                        string UpToPos(Cards play) => play.ToString()[..(pos + 1)];
                        char AtPos(Cards play) => play.ToString()[pos + 1];
                    }
                }
            }
            
            void RemoveDuplicateLines()
            {
                if (lineItems.Count == 0)
                    return;

                var seenLists = new HashSet<string>();
                var result = new List<LineItem>();

                foreach (var item in lineItems)
                {
                    var listSignature = string.Join(";", item.Items2.Select(x => string.Join(",", x.Tricks)));

                    if (seenLists.Add(listSignature))
                    {
                        result.Add(item);
                    }
                }

                lineItems = result;

            }
            
            void CreateExtraLines(int shortestCount)
            {
                var extraLines = new List<LineItem>();
                foreach (var lineItem in lineItems)
                {
                    var shortest = new Cards(lineItem.Line.MaxBy(x => x.Count()).Take(shortestCount).ToList());
                    var ambivalentItems = lineItem.Items2.Where(x => x.Tricks.Length > 1)
                        .Where(x => x.Items.Any(y => y.OnlySmallCardsEW == shortest)).ToList();
                    var sameItems = ambivalentItems.Where(x => HasSameItems(ambivalentItems, x)).ToList();
                    var nextCard = sameItems.SelectMany(x => x.Items).Select(x => x.Play[shortestCount]).Distinct().ToList();
                    foreach (var face in nextCard)
                    {
                        var cardsToNextCard = shortest.ToString() + Utils.CardToChar(face);
                        var sameItemsNextCard = sameItems.Where(x => x.Items.First().Play.ToString().StartsWith(cardsToNextCard)).ToList();
                        if (sameItemsNextCard.Count <= 1) 
                            continue;
                        var sameItem = sameItemsNextCard.First();
                        var affectedCombinations = sameItemsNextCard.Select(y => y.Combination).ToList();
                        var sameItemItems = sameItem.Items.Where(x => x.Play.ToString().StartsWith(cardsToNextCard)).ToList();
                        var index = sameItemItems.First().Play.Data
                                .Zip(sameItemItems.Last().Play.Data, (first, second) => (first, second))
                                .Select((pair, index) => (pair, index))
                                .FirstOrDefault(x => x.pair.first != x.pair.second)
                                .index + 1;
                        var affectedItems = lineItem.Items2.Where(x => affectedCombinations.Contains(x.Combination)).ToList();
                        var faces = sameItemItems.First().Play.Take(index);
                        if (affectedItems.Any(x => x.Items.All(y => faces.SequenceEqual(y.Play.Take(index)))))
                            continue;
                        
                        var newLineItems = GetNewLineItem(lineItem, sameItem, affectedCombinations, index);
                        extraLines.Add(newLineItems);
                        lineItem.GeneratedLines.Add(new Cards(sameItemItems.Last().Play.Take(index).ToList()));
                        foreach (var item2 in affectedItems)
                        {
                            item2.Items.RemoveAll(x => faces.SequenceEqual(x.Play.Take(index)));
                        }
                    }
                }
                lineItems.AddRange(extraLines);
                return;

                bool HasSameItems(List<Item2> item2S, Item2 item2)
                {
                    if (!item2.Items.All(x => x.Play.Count() > shortestCount))
                        return false;
                    if (!item2S.SelectMany(x => x.Items).All(x => x.Play.Count() > shortestCount))
                        return false;
                    return item2S.Where(y => y.Combination != item2.Combination).Any(x =>
                        x.Items.Select(x1 => x1.Play[shortestCount]).Intersect(item2.Items.Select(x2 => x2.Play[shortestCount])).Any());
                }

                LineItem GetNewLineItem(LineItem lineItem, Item2 sameItem, List<Face[]> affectedCombinations, int index)
                {
                    var newLineItems = new LineItem
                    {
                        Line = lineItem.Line.ToList(),
                        Items2 = lineItem.Items2.Select(x => x.Clone()).ToList(),
                        GeneratedLines = lineItem.GeneratedLines.ToList()
                    };
                    newLineItems.GeneratedLines.Add(new Cards(sameItem.Items.First().Play.Take(index).ToList()));
                    var enumerable = newLineItems.Items2.Where(x => affectedCombinations.Contains(x.Combination)).ToList();
                    var faces = sameItem.Items.Last().Play.Take(index).ToList();
                    foreach (var item2 in enumerable)
                    {
                        item2.Items.RemoveAll(x => faces.SequenceEqual(x.Play.Take(index)));
                    }

                    return newLineItems;
                }
            }
            
            void AddStatistics()
            {
                foreach (var lineItem in lineItems)
                {
                    lineItem.Average = lineItem.Items2.Average(y => distributionList[y.Combination].Probability * y.Tricks.Max()) /
                                       lineItem.Items2.Select(z => distributionList[z.Combination].Probability).Average();
                    lineItem.Probabilities = possibleNrOfTricks.Select(y => lineItem.Items2.Where(z => z.Tricks.Max() >= y)
                            .Sum(z => distributionList[z.Combination].Probability)).ToList();
                }
            }
            
            void AddSuitPlayStatistics()
            {
                var filename = $"{Utils.CardsToString(north)}-{Utils.CardsToString(south)}.json";
                var combine = Path.Combine(AppContext.BaseDirectory, "etalons-suitplay", filename);
                if (!File.Exists(combine))
                    return;
                using var fileStream = new FileStream(combine, FileMode.Open);
                var results = JsonSerializer.Deserialize<(Dictionary<string, List<int>> treesForJson, IEnumerable<string>)>(fileStream, JsonSerializerOptions);
            
                foreach (var lineItem in lineItems)
                {
                    var data = results.treesForJson.SingleOrDefault(a => lineItem.Header == a.Key).Value;
                    if (data == null) continue;
                    lineItem.LineInSuitPlay = true;
                    var dataCounter = 0;
                    foreach (var item2 in lineItem.Items2)
                    {
                        item2.IsDifferent = item2.Tricks.First() != -1 && item2.Tricks.Max() != data[dataCounter];
                        item2.TricksInSuitPlay = data[dataCounter++];
                    }
                }
            }

            List<Item> GetSimilarItems(List<Item> items, List<Cards> line)
            {
                var similarItems = items.Where(z => line.Any(u => u == z.OnlySmallCardsEW)).ToList();
                if (similarItems.Count != 0)
                    return similarItems;
                
                var similarLines2NdHigh = lines2NdHigh.Where(x => x[0] == line.First()[0]);
                var lines2NdHighItems = items.Where(z => similarLines2NdHigh.Any(u => u == z.Play)).ToList();
                if (lines2NdHighItems.Count > 0)
                    return lines2NdHighItems;
                
                var similarLines2NdDummy = lines2NdDummy.Where(x => x[0] == line.First()[0]);
                var lines2NdDummyItems = items.Where(z => similarLines2NdDummy.Any(u => u == z.Play)).ToList();
                if (lines2NdDummyItems.Count > 0)
                    return lines2NdDummyItems;
                
                return [];
            }
        }
    }

    public static int IsBetterPlay(int[] tricksA, int[] tricksB)
    {
        if (tricksA.Length == 1 && tricksB.Length == 1)
            return tricksA[0].CompareTo(tricksB[0]);
                
        if (tricksA.Length == 1)
        {
            return IsBetterPlayOnePlay(tricksA, tricksB);
        }
        if (tricksB.Length == 1)
        {
            return -IsBetterPlayOnePlay(tricksB, tricksA);
        }

        if (tricksA.Min() >= tricksB.Max())
            return 1;
        if (tricksB.Min() >= tricksA.Max())
            return -1;
        return 0;

        int IsBetterPlayOnePlay(int[] onePlay, int[] manyPlays)
        {
            if (onePlay[0] >= manyPlays.Max())
                return 1;
            if (onePlay[0] <= manyPlays.Min())
                return -1;
            return 0;
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