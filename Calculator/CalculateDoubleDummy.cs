namespace Calculator;

public enum Suit
{
    Spades = 0,
    Hearts = 1,
    Diamonds = 2,
    Clubs = 3,
    NoTrump = 4
}

public enum Face
{
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace,
}

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
            var playableCards = GetPlayableCards(playedCards);
            foreach (var card in playableCards)
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

            var playerToPlay = GetPlayerToPlay(playedCards, initialCards, trumpSuit);
            var availableCards = GetAvailableCards(playedCards, playerToPlay).ToList();
            if (playerToPlay is Player.North or Player.South)
            {
                var bestValue = int.MinValue;
                foreach (var card in availableCards)
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
                foreach (var card in availableCards)
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

        List<Card> GetPlayableCards(IList<Card> playedCards)
        {
            var availableCards = GetAvailableCards(playedCards, GetPlayerToPlay(playedCards, initialCards, trumpSuit)).ToList();
            return availableCards;
        }

        IEnumerable<Card> GetAvailableCards(IList<Card> playedCards, Player player)
        {
            var availableCards = initialCards[player].Except(playedCards.Where(x => initialCards[player].Contains(x))).ToList();
            availableCards.RemoveAll(x => availableCards.Contains(new Card() {Suit = x.Suit, Face = x.Face + 1 }));
            var tricks = playedCards.Chunk(4);
            if (playedCards.Count % 4 != 0 && availableCards.Any(y => y.Suit == tricks.Last().First().Suit))
            {
                availableCards.RemoveAll(x => x.Suit != tricks.Last().First().Suit);
            }

            return availableCards;
        }
       
        int GetTrickCount(IEnumerable<Card> play)
        {
            return play.Chunk(4).Count(trick => TrickWon(trick, initialCards, trumpSuit) is Player.North or Player.South);
        }
    }
    
    public static Player GetPlayerToPlay(IList<Card> playedCards, IDictionary<Player, IEnumerable<Card>> initialCards, Suit trumpSuit)
    {
        if (playedCards.Count == 0)
            return Player.West;
        var lastTrick = playedCards.Chunk(4).Last();
        if (lastTrick.Length == 4)
            return TrickWon(lastTrick, initialCards, trumpSuit);
        var playerToLead = initialCards.Single(x => x.Value.Contains(lastTrick.First())).Key;
        var playerToPlay = (Player)((lastTrick.Length + (int)playerToLead) % 4);
        return playerToPlay;
    }

    private static Player TrickWon(Card[] trick, IDictionary<Player, IEnumerable<Card>> initialCards, Suit trumpSuit)
    {
        var playerToLead = initialCards.Single(x => x.Value.Contains(trick.First())).Key; 
        var highestCard = trumpSuit != Suit.NoTrump && trick.Any(x => x.Suit == trumpSuit)
            ? trick.Where(x => x.Suit == trumpSuit).MaxBy(x => x.Face)
            : trick.Where(x => x.Suit == trick.First().Suit).MaxBy(x => x.Face);
        var indexTrickWon = trick.ToList().IndexOf(highestCard);
        return (Player)((int)(playerToLead + indexTrickWon) % 4);
    }
}