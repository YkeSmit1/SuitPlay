using System.Collections.Concurrent;

namespace Calculator;

public class Calculate
{
    public class Result
    {
        public ConcurrentDictionary<List<Face>, List<(IList<Face>, int)>> Trees { get; set; } = new();
        public ConcurrentDictionary<List<Face>, List<(IList<Face>, int)>> Plays { get; set; } = new();
    }
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
        var result = CalculateBestPlay(north, south);
        var groupedTricks = result.Trees.Values.SelectMany(x => x).GroupBy(
            x => x.Item1,
            x => x.Item2,
            new ListComparer<Face>());
        
        var averageTrickCountOrdered = groupedTricks.OrderBy(x => x.Key.Count).ThenBy(z => z.Key.First());
        return averageTrickCountOrdered;
    }

    public static Result CalculateBestPlay(string north, string south)
    {
        var cardsEW = allCards.Except(north.Select(Utils.CharToCard).ToList()).Except(south.Select(Utils.CharToCard).ToList()).ToList();
        cardsEW.Reverse();
        var combinations = Combinations.AllCombinations(cardsEW);
        var cardsN = north.Select(Utils.CharToCard);
        var cardsS = south.Select(Utils.CharToCard);
        var result = new Result();
        Parallel.ForEach(combinations, combination =>
        {
            var enumerable = combination.ToList();
            var cardsW = cardsEW.Except(enumerable);
            var calculateBestPlayForCombination = CalculateBestPlayForCombination(cardsN, cardsS, enumerable, cardsW);
            // Remove suboptimal plays
            if (options.FilterBadPlaysByEW)
                RemoveBadPlays();
            result.Trees[enumerable] = calculateBestPlayForCombination.tree;
            result.Plays[enumerable] = calculateBestPlayForCombination.results;
            return;

            void RemoveBadPlays()
            {
                RemoveBadPlaysSingle(calculateBestPlayForCombination.tree, 3);
            }
            
        });
        
        return result;    
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
       
    private static (List<(IList<Face>, int)> tree, List<(IList<Face>, int)> results) CalculateBestPlayForCombination(params IEnumerable<Face>[] cards)
    {
        var tree = new List<(IList<Face>, int)>();
        var results = new List<(IList<Face>, int)>();
        var initialCards = new Dictionary<Player, IList<Card>>
        {
            [Player.North] = cards[0].Select(x => new Card {Face = x, Player = Player.North}).ToList(),
            [Player.South] = cards[1].Select(x => new Card {Face = x, Player = Player.South}).ToList(),
            [Player.East] = cards[2].Select(x => new Card {Face = x, Player = Player.East}).ToList(),
            [Player.West] = cards[3].Select(x => new Card {Face = x, Player = Player.West}).ToList()
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
            if (playedCards.Count(card => card.Face != Face.Dummy) == allCards.Count ||
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