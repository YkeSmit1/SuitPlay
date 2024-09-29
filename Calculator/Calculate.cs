using System.Collections.Concurrent;
using MoreLinq;

namespace Calculator;

public class Calculate
{
    public class Options
    {
        internal static readonly Options DefaultCalculateOptions = new();
        public List<Face> CardsInSuit { get; init; }
    }
    
    public class Result
    {
        public ConcurrentDictionary<List<Face>, List<(IList<Face>, int)>> Trees { get; } = new();
        public ConcurrentDictionary<List<Face>, List<(IList<Face>, int)>> Plays { get; } = new();
    }

    private static readonly Player[] PlayersNS = [Player.North, Player.South];
    private static Options options;

    public static IEnumerable<IGrouping<IList<Face>, int>> GetAverageTrickCount(string north, string south, Options calculateOptions = null)
    {
        var result = CalculateBestPlay(north, south, calculateOptions);
        var groupedTricks = result.Trees.Values.SelectMany(x => x).GroupBy(
            x => x.Item1, x => x.Item2, new ListComparer<Face>());
        var averageTrickCountOrdered = groupedTricks.OrderBy(x => x.Key.Count).ThenBy(z => z.Key.First());
        return averageTrickCountOrdered;
    }

    public static Result CalculateBestPlay(string north, string south, Options calculateOptions = null)
    {
        options = calculateOptions ?? Options.DefaultCalculateOptions;
        var allCards = options.CardsInSuit ?? Enum.GetValues<Face>().Except([Face.Dummy]).ToList();
        var cardsN = north.Select(Utils.CharToCard).ToList();
        var cardsS = south.Select(Utils.CharToCard).ToList();
        var cardsEW = allCards.Except(cardsN).Except(cardsS).Reverse().ToList();
        var combinations = Combinations.AllCombinations(cardsEW);
        var result = new Result();
        Parallel.ForEach(combinations, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, combination =>
        {
            var cardsE = combination.ToList();
            var cardsW = cardsEW.Except(cardsE);
            var calculateBestPlayForCombination = CalculateBestPlayForCombination(cardsN, cardsS, cardsE, cardsW);
            result.Trees[cardsE] = calculateBestPlayForCombination.tree;
            result.Plays[cardsE] = calculateBestPlayForCombination.results;
        });

        return result;
    }

    public static int GetTrickCount(IEnumerable<Card> play)
    {
        return play.Chunk(4).Where(x => x.First().Face != Face.Dummy).Count(trick =>
            PlayersNS.Contains(trick.MaxBy(x => x.Face).Player));
    }

    private static (List<(IList<Face>, int)> tree, List<(IList<Face>, int)> results) CalculateBestPlayForCombination(params IEnumerable<Face>[] cards)
    {
        var tree = new List<(IList<Face>, int)>();
        var results = new List<(IList<Face>, int)>();
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
        return (tree, results);

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
                results.Add((playedCards.Select(x => x.Face).ToList(), trickCount));
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
                var cardTrickDictionary = new Dictionary<Card, int>();
                var bestValue = int.MaxValue;
                foreach (var card in GetPlayableCards(playedCards))
                {
                    playedCards.Add(card);
                    var value = Minimax(playedCards, alpha, beta, true);
                    bestValue = Math.Min(bestValue, value);
                    tree.Add((playedCards.Select(x => x.Face).ToList(), value));
                    cardTrickDictionary[card] = bestValue;
                    playedCards.RemoveAt(playedCards.Count - 1);
                    beta = Math.Min(beta, bestValue);
                    if (bestValue <= alpha)
                        break;
                }

                var badCards = cardTrickDictionary.Where(x => x.Value > cardTrickDictionary.Values.Min()).Select(y => y.Key);
                foreach (var card in badCards)
                {
                    results.RemoveAll(x =>
                        x.Item1.StartsWith(playedCards.Concat(new List<Card> { card }).Select(x1 => x1.Face)));
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
            if (player >= Player.None)
                return [];
            var availableCards = initialCards[player].Except(playedCards).ToList();
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
            var enumerable = cardsOtherTeam.Where(y => y.Face < x.Face);
            var equal = enumerable.SequenceEqual(nsCardsLower);
            var sequenceEqual = equal && card.Face > x.Face;
            return sequenceEqual;
        }
    }

    private static Player NextPlayer(Player player)
    {
        return player == Player.West ? Player.North : player + 1;
    }
}