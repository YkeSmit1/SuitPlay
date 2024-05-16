namespace Calculator;

public class Results
{
    public IList<(Card, int)> Tricks;
    public int NrOfEndNodes;
}

public class CalculateDoubleDummy
{
    public static Results CalculateBestPlayForCombination(Suit trumpSuit, IDictionary<Player, IEnumerable<Card>> initialCards)
    {
        var results = new Results();
        var calculateBestPlayForCombination = FindBestMove().ToList();
        results.Tricks = calculateBestPlayForCombination; 
        return results;

        IEnumerable<(Card, int)> FindBestMove()
        {
            var playedCards = new List<Card>();
            var availableCards = GetAvailableCards(playedCards, Player.West).ToList();
            foreach (var card in availableCards)
            {
                playedCards.Add(card);
                var value = Minimax(playedCards, int.MinValue, int.MaxValue);
                playedCards.RemoveAt(playedCards.Count - 1);
                yield return (card, value); 
            }
        }

        int Minimax(IList<Card> playedCards, int alpha, int beta)
        {
            if (playedCards.Count == initialCards.Sum(x => x.Value.Count()))
            {
                results.NrOfEndNodes++;
                return GetTrickCount(playedCards);
            }

            var lastTrick = playedCards.Chunk(4).Last();
            var playerToPlay = GetPlayerToPlay(lastTrick, trumpSuit);
            var availableCards = GetAvailableCards(playedCards, playerToPlay).ToList();
            if (playerToPlay is Player.North or Player.South)
            {
                var bestValue = int.MinValue;
                foreach (var card in availableCards.Where(card => CanPlay(lastTrick, card, availableCards)))
                {
                    playedCards.Add(card);
                    bestValue = Math.Max(bestValue, Minimax(playedCards, alpha, beta));
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
                foreach (var card in availableCards.Where(card => CanPlay(lastTrick, card, availableCards)))
                {
                    playedCards.Add(card);
                    bestValue = Math.Min(bestValue, Minimax(playedCards, alpha, beta));
                    playedCards.RemoveAt(playedCards.Count - 1);
                    beta = Math.Min(beta, bestValue);
                    if (bestValue <= alpha)
                        break;
                }
                return bestValue;
            }
        }

        IEnumerable<Card> GetAvailableCards(ICollection<Card> playedCards, Player player)
        {
            var availableCards = initialCards[player].Except(playedCards).ToList();
            availableCards.RemoveAll(x => availableCards.Contains(new Card {Suit = x.Suit, Face = x.Face + 1 }));
            return availableCards;
        }
       
        int GetTrickCount(IEnumerable<Card> play)
        {
            return play.Chunk(4).Count(trick => TrickWon(trick, trumpSuit) is Player.North or Player.South);
        }

        bool CanPlay(IReadOnlyCollection<Card> lastTrick, Card card, IEnumerable<Card> availableCards)
        {
            return lastTrick.Count == 4 || card.Suit == lastTrick.First().Suit || availableCards.All(x => x.Suit != lastTrick.First().Suit);
        }
    }
    
    public static Player GetPlayerToPlay(Card[] lastTrick, Suit trumpSuit)
    {
        if (lastTrick.Length == 4)
            return TrickWon(lastTrick, trumpSuit);
        return (Player)((lastTrick.Length + (int)lastTrick.First().Player) % 4);
    }

    private static Player TrickWon(Card[] trick, Suit trumpSuit)
    {
        var highestCard = trumpSuit != Suit.NoTrump && trick.Any(x => x.Suit == trumpSuit)
            ? trick.Where(x => x.Suit == trumpSuit).MaxBy(x => x.Face)
            : trick.Where(x => x.Suit == trick.First().Suit).MaxBy(x => x.Face);
        return highestCard.Player;
    }
}