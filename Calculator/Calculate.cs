using System.Collections.Concurrent;
using System.Diagnostics;
using MoreLinq;

namespace Calculator;

public class Calculate
{
    public class Result 
    {
        public List<PlayItem> PlayList;
        public List<List<Face>> AllPlays;
        public List<DistributionItem> DistributionList;
        public List<int> PossibleNrOfTricks;
    }
    
    public static IEnumerable<IGrouping<IList<Face>, int>> GetAverageTrickCount(IDictionary<List<Face>, List<(IList<Face>, int)>> bestPlay, List<Face> cardsNS)
    {
        var flattenedResults = bestPlay.Values.SelectMany(x => x);
        var segmentsNS = cardsNS.Segment((item, prevItem, _) => (int)item - (int)prevItem > 1).ToList();
        var resultsWithSmallCards = flattenedResults.Select(x => (x.Item1.Select(y => y < segmentsNS.Skip(1).First().First() ? Face.SmallCard : y), x.Item2));
        var groupedTricks = resultsWithSmallCards.GroupBy(x => x.Item1.Take(3).ToList(), x => x.Item2, new ListComparer<Face>());
        var averageTrickCountOrdered = groupedTricks.OrderByDescending(z => z.Average());
        return averageTrickCountOrdered;
    }
    
    public static Result GetResult(IDictionary<List<Face>, List<(IList<Face>, int)>> bestPlay, List<Face> cardsNS)
    {   
        var result = new Result();
        var segmentsNS = cardsNS.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        var filteredTrees = bestPlay.ToDictionary(x => x.Key, y => y.Value.Where(x => x.Item1.Count == 3 && x.Item1.All(z => z != Face.Dummy)));
        var cardsEW = Utils.GetAllCards().Except(cardsNS).ToList();
        var combinations = Combinations.AllCombinations(cardsEW).Select(x => x.OrderByDescending(y => y));
        var combinationsInTree = filteredTrees.Keys.Select(x => x.OrderByDescending(y => y)).OrderBy(x => string.Join("", x.Select(Utils.CardToChar))).ToList();
        
        var distributionList = combinationsInTree.ToDictionary(key => key.ToList(), value =>
        {
            var eastHand = value.ToList();
            var westHand = cardsEW.Except(eastHand).ToList();
            var similarCombinationsCount = SimilarCombinations(combinations, westHand, cardsNS).Count();
            return new DistributionItem
            {
                West = westHand.ConvertToSmallCards(segmentsNS).ToList(),
                East = eastHand.ConvertToSmallCards(segmentsNS).ToList(),
                Occurrences = similarCombinationsCount,
                Probability = Utils.GetDistributionProbabilitySpecific(eastHand.Count, westHand.Count) * similarCombinationsCount,
            };
        }, new ListComparer<Face>());

        var possibleNrOfTricks = filteredTrees.SelectMany(x => x.Value).Select(x => x.Item2).Distinct().OrderByDescending(x => x).SkipLast(1);

        var playItems = filteredTrees
            .SelectMany(x => x.Value, (parent, child) => new { combi = parent.Key, play = child.Item1.ConvertToSmallCards(segmentsNS), nrOfTricks = child.Item2 })
            .GroupBy(x => x.play.ToList(), y => new { combi2 = y.combi, nrOfTricks2 = y.nrOfTricks }, new ListComparer<Face>()).ToList()
            .ToDictionary(key => key.Key, value => new PlayItem
            {
                Play = value.Key.ToList(),
                NrOfTricks = combinationsInTree.Select(x => value.SingleOrDefault(y => x.SequenceEqual(y.combi2), new {combi2 = (List<Face>)[], nrOfTricks2 = -1}).nrOfTricks2).ToList(),
                Average = value.Average(x => distributionList[x.combi2].Probability * x.nrOfTricks2) / value.Select(x => distributionList[x.combi2].Probability).Average(),
                Probabilities = possibleNrOfTricks.Select(y => value.Where(x => x.nrOfTricks2 >= y).Sum(x => distributionList[x.combi2].Probability) / value.Sum(x => distributionList[x.combi2].Probability)).ToList(), 
                
            })
            .OrderByDescending(x => x.Value.Average)
            .ToDictionary(key => key.Key, value => value.Value, new ListComparer<Face>());

        result.PlayList = playItems.Select(x => x.Value).ToList();
        result.AllPlays = playItems.Select(x => x.Key).Select(x => x.ConvertToSmallCards(segmentsNS).ToList()).ToList();
        result.DistributionList = distributionList.Select(x => x.Value).ToList();
        result.PossibleNrOfTricks = possibleNrOfTricks.ToList();

        return result;
    }

    public static IDictionary<List<Face>, List<(IList<Face>, int)>> CalculateBestPlay(List<Face> north, List<Face> south)
    {
        var allCards = Utils.GetAllCards();
        var cardsEW = allCards.Except(north).Except(south).ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
        var cardsNS = north.Concat(south).OrderByDescending(x => x);
        combinations.RemoveAll(faces => SimilarCombinationsCount(combinations, faces.ToList(), cardsNS) > 0);
        var result = new ConcurrentDictionary<List<Face>, List<(IList<Face>, int)>>();
        Parallel.ForEach(combinations, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, combination =>
        {
            var cardsE = combination.ToList();
            var cardsW = cardsEW.Except(cardsE);
            var calculateBestPlayForCombination = CalculateBestPlayForCombination(north, south, cardsE, cardsW);
            result[cardsE] = calculateBestPlayForCombination;
        });

        return result;
    }

    public static int SimilarCombinationsCount(List<IEnumerable<Face>> combinationList, List<Face> combination, IEnumerable<Face> cardsNS)
    {
        if (combination.Count == 0)
            return 0;
        var similarCombinations = SimilarCombinations(combinationList, combination, cardsNS);
        var hasSimilar = similarCombinations.Count(x => x.Last() < combination.Last());
        return hasSimilar;
    }

    private static IEnumerable<IEnumerable<Face>> SimilarCombinations(IEnumerable<IEnumerable<Face>> combinationList, IEnumerable<Face> combination, IEnumerable<Face> cardsNS)
    {
        // ReSharper disable PossibleMultipleEnumeration
        Debug.Assert(combinationList.All(x => x.SequenceEqual(x.OrderByDescending(y => y))));
        Debug.Assert(combination.SequenceEqual(combination.OrderByDescending(y => y)));
        Debug.Assert(cardsNS.SequenceEqual(cardsNS.OrderByDescending(y => y)));
        var cardsNSOrdered = cardsNS.OrderByDescending(x => x);
        var segmentsNS = cardsNSOrdered.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        var segments = combination.Select(GetSegment).ToList();
        var similarCombinations = combinationList.Where(x => x.Select(GetSegment).SequenceEqual(segments));
        // ReSharper restore PossibleMultipleEnumeration
        return similarCombinations;

        int GetSegment(Face face)
        {
            return segmentsNS.FindIndex(x => x.First() < face);
        }
    }

    public static int GetTrickCount(IEnumerable<Card> play)
    {
        return play.Chunk(4).Where(x => x.First().Face != Face.Dummy).Count(trick =>
            ((List<Player>)[Player.North, Player.South]).Contains(trick.MaxBy(x => x.Face).Player));
    }

    private static List<(IList<Face>, int)> CalculateBestPlayForCombination(params IEnumerable<Face>[] cards)
    {
        var tree = new List<(IList<Face>, int)>();
        var initialCards = new Dictionary<Player, IList<Card>>
        {
            [Player.North] = cards[0].Select(x => new Card { Face = x, Player = Player.North }).ToList(),
            [Player.South] = cards[1].Select(x => new Card { Face = x, Player = Player.South }).ToList(),
            [Player.East] = cards[2].Select(x => new Card { Face = x, Player = Player.East }).ToList(),
            [Player.West] = cards[3].Select(x => new Card { Face = x, Player = Player.West }).ToList()
        };
        var cardsNS = initialCards[Player.North].Concat(initialCards[Player.South]).ToList();
        var cardsEW = initialCards[Player.East].Concat(initialCards[Player.West]).ToList();
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
            if (playedCards.Count(card => card.Face != Face.Dummy) == initialCards.Values.Sum(x => x.Count) ||
                playedCards.Chunk(4).Last().First().Face == Face.Dummy)
            {
                var trickCount = GetTrickCount(playedCards);
                return trickCount;
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
                    tree.Add((playedCards.Select(x => x.Face).ToList(), value));
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
            return availableCards.Count == 0 ? [new Card { Face = Face.Dummy }] : availableCards;
        }

        IEnumerable<Card> GetAvailableCards(IList<Card> playedCards, Player player)
        {
            if (player == Player.None)
                return [];
            
            var availableCards = initialCards[player].Except(playedCards).ToList();
            if (availableCards.Count == 0)
                return [];
            
            if (playedCards.Count % 4 == 1)
            {
                var lowestCard = availableCards.MinBy(x => x.Face);
                var lastTrick = playedCards.Chunk(4).Last();
                var coverCards = availableCards.Where(x => x.Face > lastTrick.First().Face).ToList();
                if (coverCards.Count == 0)
                    return new List<Card> {lowestCard};
                var coverCard = coverCards.MinBy(x => x.Face);
                return coverCard.Face - lowestCard.Face == 2
                    ? new List<Card> {coverCard}
                    : new List<Card> {lowestCard, coverCard }.Distinct();
            }
            
            if (playedCards.Count % 4 == 3)
            {
                var lastTrick = playedCards.Chunk(4).Last();
                var highestCards = availableCards.Where(x => x.Face > lastTrick.Max(y => y.Face)).ToList();
                return highestCards.Count > 0 ? [highestCards.MinBy(x => x.Face)] : [availableCards.MinBy(x => x.Face)];
            }
            
            var cardsOtherTeam = player is Player.North or Player.South ? cardsEW : cardsNS;
            var availableCardsFiltered = AvailableCardsFiltered(availableCards, cardsOtherTeam);

            return availableCardsFiltered.ToList();
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

    public static IEnumerable<Card> AvailableCardsFiltered(List<Card> availableCards, List<Card> cardsOtherTeam)
    {
        return availableCards.Where(card =>
        {
            var nsCardsLower = cardsOtherTeam.Where(x => x.Face < card.Face);
            var hasSimilarCard = availableCards.Any(x => SequenceEqual(x, nsCardsLower, card));
            return !hasSimilarCard;
        });

        bool SequenceEqual(Card x, IEnumerable<Card> nsCardsLower, Card card)
        {
            return cardsOtherTeam.Where(y => y.Face < x.Face).SequenceEqual(nsCardsLower) && card.Face > x.Face;
        }
    }

    private static Player NextPlayer(Player player)
    {
        return player == Player.West ? Player.North : player + 1;
    }
}