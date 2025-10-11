using Calculator.Models;
using MoreLinq;

namespace Calculator;

public static class MiniMax
{
    private static ArrayEqualityComparer<Face> arrayEqualityComparer;

    public static List<Item> CalculateBestPlayForCombination(params IEnumerable<Face>[] cards)
    {
        var tree = new List<Item>();
        arrayEqualityComparer = new ArrayEqualityComparer<Face>();
        var transpositionTable = new Dictionary<Face[], Item>(arrayEqualityComparer);
        var initialCards = cards.Select((x, index) => (x, index)).ToDictionary(item => (Player)item.index, item => item.x.ToList());
        var cardsNS = initialCards[Player.North].Concat(initialCards[Player.South]).OrderDescending().ToList();
        var cardsEW = initialCards[Player.East].Concat(initialCards[Player.West]).OrderDescending().ToList();
        FindBestMove();
        return tree;

        void FindBestMove()
        {
            var playedCards = new Cards([]);
            var playableCards = GetPlayableCards(playedCards);
            foreach (var card in playableCards)
            {
                playedCards.Add(card);
                var resultItem = Minimax(playedCards, false);
                tree.Add(resultItem);
                playedCards.RemoveAt(playedCards.Count() - 1);
            }
        }

        Item Minimax(Cards playedCards, bool maximizingPlayer)
        {
            if (playedCards.Count(card => card != Face.Dummy) == initialCards.Values.Sum(x => x.Count) ||
                playedCards.Chunk(4).Last().First() == Face.Dummy)
            {
                var trickCount = GetTrickCount(playedCards, initialCards);
                return new Item(playedCards.Clone(), trickCount);
            }
            
            if (playedCards.Count() % 4 == 0 && !cardsEW.Except(playedCards.Data).Any()) 
            {
                var trickCount = GetTrickCount(playedCards, initialCards) +
                                 Math.Max(initialCards[Player.North].Count, initialCards[Player.South].Count) -
                                 playedCards.Chunk(4).Count();
                return new Item(playedCards.Clone(), trickCount);
            }

            if (maximizingPlayer)
            {
                var resultItem = new Item (playedCards.Clone(), int.MinValue, []);
                foreach (var card in GetPlayableCards(playedCards))
                {
                    playedCards.Add(card);
                    var value = Minimax(playedCards, false);
                    resultItem.Tricks = Math.Max(resultItem.Tricks, value.Tricks);
                    resultItem.Children.Add(value);
                    playedCards.RemoveAt(playedCards.Count() - 1);
                }
                if (playedCards.Any(x => x == Face.Dummy)) 
                    resultItem.Children = [resultItem.Children.First(x => x.Tricks == resultItem.Tricks)];
                    
                return resultItem;
            }
            else
            {
                var resultItem = new Item ( playedCards.Clone(),  int.MaxValue, []);
                foreach (var card in GetPlayableCards(playedCards))
                {
                    playedCards.Add(card);
                    var value = playedCards.Count() % 4 == 0 && transpositionTable.TryGetValue(playedCards.Chunk(4).Select(x => x.Order()).SelectMany(x => x).ToArray(), out var item)
                        ? new Item(playedCards.Clone(), 0, []) {TranspositionRef = item}
                        : Minimax(playedCards, true);

                    resultItem.Tricks = Math.Min(resultItem.Tricks, value.Tricks);
                    // if (playedCards.Count() % 4 == 0 && !transpositionTable.TryGetValue(playedCards.Chunk(4).Select(x => x.Order()).SelectMany(x => x).ToList(), out _))
                    //     transpositionTable.Add(playedCards.Chunk(4).Select(x => x.Order()).SelectMany(x => x).ToList(), value);

                    resultItem.Children.Add(value);
                    playedCards.RemoveAt(playedCards.Count() - 1);
                }

                if (playedCards.Any(x => x == Face.Dummy)) 
                    resultItem.Children = [resultItem.Children.First(x => x.Tricks == resultItem.Tricks)];
                resultItem.Children.RemoveAll(x => x.Tricks > resultItem.Tricks);

                return resultItem;
            }
        }

        List<Face> GetPlayableCards(Cards playedCards)
        {
            List<Face> availableCards;
            if (playedCards.Count() % 4 == 0)
            {
                var availableCardsNorth = GetAvailableCards(playedCards, Player.North);
                var availableCardsSouth = GetAvailableCards(playedCards, Player.South);
                availableCards = ApplyStrategyPosition1(availableCardsNorth, availableCardsSouth);
            }
            else
            {
                availableCards = GetAvailableCards(playedCards, NextPlayer(GetCurrentPlayer(playedCards))).ToList();
                if (availableCards.Count == 0) 
                    return [Face.Dummy];
                var lastTrick = playedCards.Chunk(4).Last();

                availableCards = lastTrick.Length switch
                {
                    1 => ApplyStrategyPosition2(lastTrick),
                    2 => ApplyStrategyPosition3(lastTrick),
                    3 => ApplyStrategyPosition4(lastTrick),
                    _ => availableCards
                };
            }

            return availableCards.Count == 0 ? [Face.Dummy] : availableCards;

            List<Face> ApplyStrategyPosition1(List<Face> availableCardsNorth, List<Face> availableCardsSouth)
            {
                var availableCardsNS = availableCardsNorth.Concat(availableCardsSouth).ToList();
                if (availableCardsNS.Count == 0)
                    return [];
                var cardsEWNotPlayed = cardsEW.Except(playedCards.Data).ToList();
                if (cardsEWNotPlayed.Count == 1)
                    return [availableCardsNS.Max()];
                if (!HasForks(availableCardsSouth))
                    return availableCardsSouth;
                if (!HasForks(availableCardsNorth))
                    return availableCardsNorth;

                return availableCardsNS.ToList();
                
                bool HasForks(List<Face> cardsPlayer)
                {
                    // TODO filter out small cards
                    return cardsPlayer.Select(x => cardsEWNotPlayed.Where(y => y > x).ToArray()).Distinct(arrayEqualityComparer).Count() != 1;
                }
            }
            
            List<Face> ApplyStrategyPosition2(Face[] lastTrick)
            {
                // TODO maybe use falsecards
                return availableCards.All(x => x < lastTrick[0]) ? [availableCards.Min(y => y)] : availableCards;
            }
            
            List<Face> ApplyStrategyPosition3(Face[] lastTrick)
            {
                if (availableCards.All(x => x < lastTrick[0]) || availableCards.All(x => x < lastTrick[1]))
                    return [availableCards.Min(y => y)];
                if (lastTrick[0] < lastTrick[1])
                    return availableCards.Where(x => x > lastTrick[1]).ToList();
                return availableCards;
            }

            List<Face> ApplyStrategyPosition4(Face[] lastTrick)
            {
                var highestCardOtherTeam = (Face)Math.Max((int)lastTrick[0], (int)lastTrick[2]);
                var highestCards = availableCards.Where(x => x > highestCardOtherTeam && highestCardOtherTeam > lastTrick[1]).ToList();
                if (highestCards.Count > 0) 
                    availableCards = [highestCards.Min()];
                return availableCards;
            }
        }

        List<Face> GetAvailableCards(Cards playedCards, Player player)
        {
            var availableCards = initialCards[player].Except(playedCards.Data).ToList();
            if (availableCards.Count == 0)
                return [];
            
            var cardsOtherTeam = player is Player.North or Player.South ? cardsEW : cardsNS;
            var availableCardsFiltered = AvailableCardsFiltered(availableCards, cardsOtherTeam, playedCards).ToList();

            return availableCardsFiltered.ToList();
        }

        Player GetCurrentPlayer(Cards playedCards)
        {
            var lastTrick = playedCards.Chunk(4).Last();
            var playerToLead = initialCards.Single(x => x.Value.Contains(lastTrick.First())).Key;
            return (Player)((lastTrick.Length + (int)playerToLead) % 4 - 1);
        }
        
        static Player NextPlayer(Player player)
        {
            return player == Player.West ? Player.North : player + 1;
        }
    }
    
    public static int GetTrickCount(Cards play, Dictionary<Player, List<Face>> initialCards)
    {
        return play.Chunk(4).Where(x => x.First() != Face.Dummy).Count(trick => 
            initialCards.Single(y => y.Value.Contains(trick.Max())).Key is Player.North or Player.South);
    }

    public static IEnumerable<Face> AvailableCardsFiltered(List<Face> availableCards, List<Face> cardsOtherTeam, Cards playedCards)
    {
        var playedCardsPreviousTricks = playedCards.SkipLast(playedCards.Count() % 4);
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
}