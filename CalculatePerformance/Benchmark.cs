using BenchmarkDotNet.Attributes;
using Calculator;

namespace CalculatePerformance;

[MemoryDiagnoser]
public class Benchmark
{
    [Benchmark]
    public Results GetResults()
    {
        IDictionary<Player, IEnumerable<Card>> hand = new Dictionary<Player, IEnumerable<Card>>
        {
            [Player.North] = [new Card {Suit = Suit.Spades, Face = Face.Ace, Player = Player.North},
                new Card {Suit = Suit.Hearts, Face = Face.Ace, Player = Player.North},
                new Card {Suit = Suit.Hearts, Face = Face.King, Player = Player.North},
                new Card {Suit = Suit.Diamonds, Face = Face.Ace, Player = Player.North}],
                [Player.East] = [new Card {Suit = Suit.Spades, Face = Face.King, Player = Player.East},
                    new Card {Suit = Suit.Hearts, Face = Face.Queen, Player = Player.East},
                    new Card {Suit = Suit.Diamonds, Face = Face.King, Player = Player.East},
                    new Card {Suit = Suit.Diamonds, Face = Face.Jack, Player = Player.East}],
                [Player.South] = [new Card {Suit = Suit.Spades, Face = Face.Queen, Player = Player.South},
                    new Card {Suit = Suit.Diamonds, Face = Face.Queen, Player = Player.South},
                    new Card {Suit = Suit.Clubs, Face = Face.Ace, Player = Player.South},
                    new Card {Suit = Suit.Clubs, Face = Face.Queen, Player = Player.South}],
                [Player.West] = [new Card {Suit = Suit.Spades, Face = Face.Jack, Player = Player.West},
                    new Card {Suit = Suit.Hearts, Face = Face.Jack, Player = Player.West},
                    new Card {Suit = Suit.Clubs, Face = Face.King, Player = Player.West},
                    new Card {Suit = Suit.Clubs, Face = Face.Jack, Player = Player.West}]
        };
        return CalculateDoubleDummy.CalculateBestPlayForCombination(Suit.Spades, hand);
    }

    [Benchmark]
    public Player GetPlayer()
    {
        var player = Player.North;
        Card[] lastTrick = [
            new Card { Suit = Suit.Spades, Face = Face.Jack, Player = Player.West },
            new Card { Suit = Suit.Diamonds, Face = Face.Queen, Player = Player.North },
            new Card { Suit = Suit.Diamonds, Face = Face.King, Player = Player.East },
            new Card { Suit = Suit.Diamonds, Face = Face.Ace, Player = Player.South }
        ];
        for (var i = 0; i < 10000; i++)
        {
            player = CalculateDoubleDummy.GetPlayerToPlay(lastTrick, Suit.Spades);
        }

        return player;
    }
}