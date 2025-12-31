using Calculator.Models;
using MoreLinq;

namespace Calculator;

public static class MiniMax
{
    public static List<Item> CalculateBestPlayForCombination(params IEnumerable<Face>[] cards)
    {
        var tree = new List<Item>();
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
                    var value = Minimax(playedCards, true);
                    resultItem.Tricks = Math.Min(resultItem.Tricks, value.Tricks);

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
            var cardsEWNotPlayed = cardsEW.Except(playedCards.Data).ToList();
            if (playedCards.Count() % 4 == 0)
                return ApplyStrategyPosition1();

            var nextPlayer = NextPlayer(GetCurrentPlayer(playedCards));
            var availableCards = initialCards[nextPlayer].Except(playedCards.Data).ToList();
                
            if (availableCards.Count == 0) 
                return [Face.Dummy];
            var lastTrick = playedCards.Chunk(4).Last();

            var playableCards = lastTrick.Length switch
            {
                1 => ApplyStrategyPosition2(),
                2 => ApplyStrategyPosition3(),
                3 => ApplyStrategyPosition4(),
                _ => availableCards
            };

            return playableCards.Count == 0 ? [Face.Dummy] : FilterAvailableCards(nextPlayer, playedCards, playableCards) ;

            List<Face> ApplyStrategyPosition1()
            {
                var availableCardsNorth = initialCards[Player.North].Except(playedCards.Data).ToList();
                var availableCardsSouth = initialCards[Player.South].Except(playedCards.Data).ToList();
                var availableCardsNS = availableCardsNorth.Concat(availableCardsSouth).OrderDescending().ToList();
                if (availableCardsNS.Count == 0)
                    return [Face.Dummy];
                // Play only high cards when the rest of the tricks is certain
                if (cardsEWNotPlayed.Count == 1)
                    return [availableCardsNS.Max()];
                // Only play from the hand without forks
                var cardsResult = GetAvailableCardsForks(availableCardsNorth.ToArray(), availableCardsSouth.ToArray(), cardsEWNotPlayed.ToArray());
                return cardsResult;
            }
            
            List<Face> ApplyStrategyPosition2()
            {
                // TODO maybe use falsecards
                // Play lowest card when first hand plays a high one
                if (availableCards.All(x => x < lastTrick[0]))
                    return [availableCards.Min()];
                // Play the lowest card when partner has no cards and a high card cannot benefit
                var player = NextPlayer(nextPlayer);
                var cardsNextPlayer = initialCards[player].Except(playedCards.Data).ToList();
                var cardsPartner = initialCards[NextPlayer(player)].Except(playedCards.Data).ToList();
                if (cardsPartner.Count == 0 && cardsNextPlayer.Count > 1 && availableCards.Count > 1 &&
                    cardsNextPlayer.Max() > availableCards.Max() && cardsNextPlayer.Skip(1).Max() > availableCards.Skip(1).Max())
                    return [availableCards.Min()];

                return availableCards;
            }
            
            List<Face> ApplyStrategyPosition3()
            {
                // Play the lowest card when it's not possible to win the trick
                var availableCardsNorth = initialCards[Player.North].Except(playedCards.Data).ToList();
                var availableCardsSouth = initialCards[Player.South].Except(playedCards.Data).ToList();
                
                if (availableCards.All(x => x < lastTrick[0]) || availableCards.All(x => x < lastTrick[1]))
                    return [availableCards.Min()];
                if (lastTrick[0] < lastTrick[1])
                {
                    // Play a high card when 2nd hand plays the highest card
                    if (lastTrick[1] > cardsEWNotPlayed.Max()) 
                        return availableCards.Where(x => x > lastTrick[1]).ToList();
                    // Play a high card if you have the highest card
                    if (availableCards.Any(x => x > cardsEWNotPlayed.Max()))
                        return availableCards.Where(x => x > lastTrick[1]).ToList();
                    // Cover if it's free
                    if (availableCards.Contains(lastTrick[1] + 1) &&
                        (availableCardsNorth.Contains(lastTrick[1] - 1) || availableCardsSouth.Contains(lastTrick[1] - 1)))
                        return availableCards.Where(x => x > lastTrick[1]).ToList();
                }

                // Don't play an unnecessary high card
                var partnersCards = GetCurrentPlayer(playedCards) == Player.East ? availableCardsNorth : availableCardsSouth;
                if (lastTrick[1] != Face.Dummy && !partnersCards.All(x => x > lastTrick[0]) && lastTrick[1] < lastTrick[0])
                    return [availableCards.Min()];
                return availableCards;
            }

            List<Face> ApplyStrategyPosition4()
            {
                // TODO maybe use falsecards
                var highestCardOtherTeam = (Face)Math.Max((int)lastTrick[0], (int)lastTrick[2]);
                var highestCardsOurTeam = availableCards.Where(x => x > highestCardOtherTeam && highestCardOtherTeam > lastTrick[1]).ToList();
                // Win is cheap is possible
                if (highestCardsOurTeam.Count > 0) 
                    return [highestCardsOurTeam.Min()];
                return [availableCards.Min()];
            }
        }

        List<Face> FilterAvailableCards(Player player, Cards playedCards, List<Face> availableCards)
        {
            var cardsOtherTeam = player is Player.North or Player.South ? cardsEW : cardsNS;
            var playedCardsExceptLastTrick = playedCards.Data.Count > 4 ? playedCards.Data.Chunk(4).SkipLast(1).SelectMany(x => x): [];
            var cardsOtherTeamNotPlayed = cardsOtherTeam.Except(playedCardsExceptLastTrick).ToList();
            var availableCardsFiltered = AvailableCardsFiltered(availableCards, cardsOtherTeamNotPlayed, player is Player.West).ToList();

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

    public static List<Face> GetAvailableCardsForks(Face[] cardsNorth, Face[] cardsSouth, Face[] cardsEW)
    {
        var longestSuit = Math.Min(Math.Max(cardsNorth.Length, cardsSouth.Length), cardsEW.Length);
        var cardsNS = cardsNorth.Concat(cardsSouth).OrderDescending().ToList();
        var segmentsNS = GetSegments(cardsNS, cardsEW).ToList();
        var lastRelevantCard = segmentsNS.Single(x => x.Contains(cardsNS[longestSuit - 1])).Min();
        if (lastRelevantCard > cardsEW.Max())
            return [lastRelevantCard];
        // One or zero segment and the other more than one. Play the lowest of   
        var relevantSegmentsNorth = GetSegments(cardsNorth.Where(x => x >= lastRelevantCard), cardsEW).ToList();
        var relevantSegmentsSouth = GetSegments(cardsSouth.Where(x => x >= lastRelevantCard), cardsEW).ToList();
        if (relevantSegmentsNorth.Count < 2 && relevantSegmentsSouth.Count > 1 && LastSegmentIsTheSame(relevantSegmentsNorth, relevantSegmentsSouth))
            return [cardsNorth.Min()];
        if (relevantSegmentsSouth.Count < 2 && relevantSegmentsNorth.Count > 1 && LastSegmentIsTheSame(relevantSegmentsNorth, relevantSegmentsSouth))
            return [cardsSouth.Min()];
        // Both has one segment, play the lowest if the other player has the highest card
        if (relevantSegmentsNorth.Count == 1 && relevantSegmentsSouth.Count == 1)
        {
            var cardsPlayerNotHighestCard = cardsNorth.Contains(cardsNS.Max()) ? cardsSouth : cardsNorth;
            var segments = GetSegments(cardsPlayerNotHighestCard, cardsEW).ToList();
            if (segments.Count == 1)
                return [cardsPlayerNotHighestCard.Min()];
        }
        // Play a high card if we have the top highest cards
        if (relevantSegmentsNorth.Count > 1 && relevantSegmentsSouth.Count > 1 && cardsNS.Max() > cardsEW.Max())
        {
            if (segmentsNS[0].Count() > 1 && segmentsNS[1].Count() > 1)
                return [cardsNorth.Max(), cardsSouth.Max()];
        }
        
        // Return north and south cards
        var cardsNorthFiltered = AvailableCardsFiltered(cardsNorth, cardsEW);
        var cardsSouthFiltered = AvailableCardsFiltered(cardsSouth, cardsEW);
        return cardsNorthFiltered.Concat(cardsSouthFiltered).ToList();
        
        bool LastSegmentIsTheSame(List<IEnumerable<Face>> relevantSegmentsNorth1, List<IEnumerable<Face>> relevantSegmentsSouth1)
        {
            if (relevantSegmentsNorth1.Count == 0 || relevantSegmentsSouth1.Count == 0)
                return true;
            var segmentNorth = GetSegmentNS(relevantSegmentsNorth1.Last(), segmentsNS);
            var segmentSouth = GetSegmentNS(relevantSegmentsSouth1.Last(), segmentsNS);
            return segmentNorth.SequenceEqual(segmentSouth);
            
            static IEnumerable<Face> GetSegmentNS(IEnumerable<Face> segment, IEnumerable<IEnumerable<Face>> segmentsNS)
            {
                return segmentsNS.Single(x => x.Contains(segment.First()));
            }
        }
    }
    
    private static IEnumerable<IEnumerable<Face>> GetSegments(IEnumerable<Face> cardsPlayer, IEnumerable<Face> cardsEW)
    {
        return cardsPlayer.Segment((item, prevItem, _) => cardsEW.Any(x => x > item && x < prevItem));
    }

    public static int GetTrickCount(Cards play, Dictionary<Player, List<Face>> initialCards)
    {
        return play.Chunk(4).Where(x => x.First() != Face.Dummy).Count(trick => 
            initialCards.Single(y => y.Value.Contains(trick.Max())).Key is Player.North or Player.South);
    }

    public static IEnumerable<Face> AvailableCardsFiltered(IEnumerable<Face> availableCards, IEnumerable<Face> availableCardsOtherTeam, bool takeFirst = false)
    {
        var segmentsAvailableCards = GetSegments(availableCards, availableCardsOtherTeam).ToList();
        var availableCardsFiltered = segmentsAvailableCards.Index(0).Select(x => takeFirst && x.Key == 0 ? x.Value.First() : x.Value.Last());
        return availableCardsFiltered;
    }
}