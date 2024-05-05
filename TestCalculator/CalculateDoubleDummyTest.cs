using Calculator;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace TestCalculator;

[TestSubject(typeof(CalculateDoubleDummy))]
public class CalculateDoubleDummyTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public CalculateDoubleDummyTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TestCalculateDoubleDummy4Cards()
    {
        DoCalculate("lesson2c.pbn");
    }

    [Fact]
    public void TestCalculateDoubleDummy6Cards()
    {
        DoCalculate("lesson2b.pbn");
    }

    [Fact]
    public void TestCalculateDoubleDummy8Cards()
    {
        DoCalculate("lesson2a.pbn");
    }
    
    [Fact]
    public void TestCalculateDoubleDummy()
    {
        DoCalculate("lesson2.pbn");
    }
    
    [Fact]
    public void TestPlayerToPlay()
    {
        var initialCards = new Dictionary<Player, IEnumerable<Card>>
        {
            [Player.North] = new[] { new Card { Suit = Suit.Spades, Face = Face.Ace }, 
                new Card { Suit = Suit.Clubs, Face = Face.King } },
            [Player.East] = new[] { new Card { Suit = Suit.Spades, Face = Face.King } },
            [Player.South] = new[] { new Card { Suit = Suit.Spades, Face = Face.Queen } },
            [Player.West] = new[] { new Card { Suit = Suit.Spades, Face = Face.Jack } }
        };

        Assert.Equal(Player.East,
            CalculateDoubleDummy.GetPlayerToPlay([new Card { Suit = Suit.Spades, Face = Face.Ace }], initialCards, Suit.Spades));

        Assert.Equal(Player.South, CalculateDoubleDummy.GetPlayerToPlay(
                [new Card { Suit = Suit.Spades, Face = Face.Ace }, new Card { Suit = Suit.Spades, Face = Face.King }], initialCards, Suit.Spades));
        
        Assert.Equal(Player.North, CalculateDoubleDummy.GetPlayerToPlay(
            [new Card { Suit = Suit.Spades, Face = Face.Queen }, new Card { Suit = Suit.Spades, Face = Face.King }], initialCards, Suit.Spades));
        
        Assert.Equal(Player.North, CalculateDoubleDummy.GetPlayerToPlay(
            [new Card { Suit = Suit.Spades, Face = Face.King }, 
                new Card { Suit = Suit.Spades, Face = Face.Queen }, 
                new Card { Suit = Suit.Spades, Face = Face.Jack }, 
            new Card { Suit = Suit.Spades, Face = Face.Ace }], 
            initialCards, Suit.Spades));
        
        Assert.Equal(Player.West, CalculateDoubleDummy.GetPlayerToPlay(
        [new Card { Suit = Suit.Clubs, Face = Face.King }, 
            new Card { Suit = Suit.Diamonds, Face = Face.Ace }, 
            new Card { Suit = Suit.Hearts, Face = Face.Queen }, 
            new Card { Suit = Suit.Spades, Face = Face.Jack }], initialCards, Suit.Spades));
        
        
        Assert.Equal(Player.West, CalculateDoubleDummy.GetPlayerToPlay(
        [new Card { Suit = Suit.Spades, Face = Face.Jack }, 
            new Card { Suit = Suit.Diamonds, Face = Face.Queen }, 
            new Card { Suit = Suit.Diamonds, Face = Face.King }, 
            new Card { Suit = Suit.Diamonds, Face = Face.Ace }], initialCards, Suit.Spades));
    }    
    
    private void DoCalculate(string filePath)
    {
        var pbn = new Common.Pbn();
        pbn.Load(filePath);
        var firstBoard = pbn.Boards[0];
        var hand = StringToCards(firstBoard.Deal);
        var res = CalculateDoubleDummy.CalculateBestPlayForCombination(Suit.Spades, hand);
        Assert.True(res.Tricks.Count == hand[Player.North].Count());
        testOutputHelper.WriteLine($"NrOfNodes:{res.NrOfEndNodes}");
        foreach (var card in res.Tricks)
        {
            testOutputHelper.WriteLine($"Card:{card.Item1.Suit}{card.Item1.Face} Tricks:{card.Item2}");
        }
    }    

    private static IDictionary<Player, IEnumerable<Card>> StringToCards(Dictionary<Common.Player, string> board)
    {
        return board.ToDictionary(x => CommonPlayerToPlayer(x.Key), y => HandToCards(y.Value));
        
        static Player CommonPlayerToPlayer(Common.Player player)
        {
            return player switch
            {
                Common.Player.West => Player.West,
                Common.Player.North => Player.North,
                Common.Player.East => Player.East,
                Common.Player.South => Player.South,
                _ => throw new ArgumentOutOfRangeException(nameof(player), player, null)
            };
        }
    }

    private static IEnumerable<Card> HandToCards(string s)
    {
        var ret = s.Split(',').Select((x, index) =>
            x.Select(y => new Card() { Face = GetFaceFromDescription(y), Suit = (Suit)(index) })).SelectMany(x => x);
        return ret;
    }

    private static Face GetFaceFromDescription(char c)
    {
        return c switch
        {
            '2' => Face.Two,
            '3' => Face.Three,
            '4' => Face.Four,
            '5' => Face.Five,
            '6' => Face.Six,
            '7' => Face.Seven,
            '8' => Face.Eight,
            '9' => Face.Nine,
            'T' => Face.Ten,
            'J' => Face.Jack,
            'Q' => Face.Queen,
            'K' => Face.King,
            'A' => Face.Ace,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
        };
    }
}