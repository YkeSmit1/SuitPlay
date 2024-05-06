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
        DoCalculate("lesson2c.pbn", 3.0);
    }

    [Fact]
    public void TestCalculateDoubleDummy6Cards()
    {
        DoCalculate("lesson2b.pbn", 5.33);
    }

    [Fact]
    public void TestCalculateDoubleDummy8Cards()
    {
        DoCalculate("lesson2a.pbn", 7.125);
    }
    
    [Fact]
    public void TestCalculateDoubleDummy()
    {
        DoCalculate("lesson2.pbn", 0);
    }
    
    [Fact]
    public void TestCalculateDoubleDummy9Cards()
    {
        DoCalculate("lesson2d.pbn", 0);
    }
    
    
    [Fact]
    public void TestPlayerToPlay()
    {
        Assert.Equal(Player.East,
            CalculateDoubleDummy.GetPlayerToPlay([new Card { Suit = Suit.Spades, Face = Face.Ace, Player = Player.North}], Suit.Spades));

        Assert.Equal(Player.South, CalculateDoubleDummy.GetPlayerToPlay(
            [new Card { Suit = Suit.Spades, Face = Face.Ace, Player = Player.North}, 
                new Card { Suit = Suit.Spades, Face = Face.King, Player = Player.East}], Suit.Spades));

        Assert.Equal(Player.North, CalculateDoubleDummy.GetPlayerToPlay(
            [new Card { Suit = Suit.Spades, Face = Face.Queen, Player = Player.South}, 
                new Card { Suit = Suit.Spades, Face = Face.Jack, Player = Player.West}], Suit.Spades));

        Assert.Equal(Player.North, CalculateDoubleDummy.GetPlayerToPlay(
        [new Card { Suit = Suit.Spades, Face = Face.King, Player = Player.East}, 
            new Card { Suit = Suit.Spades, Face = Face.Queen, Player = Player.South}, 
            new Card { Suit = Suit.Spades, Face = Face.Jack, Player = Player.West}, 
            new Card { Suit = Suit.Spades, Face = Face.Ace, Player = Player.North}], 
        Suit.Spades));

        Assert.Equal(Player.West, CalculateDoubleDummy.GetPlayerToPlay(
        [new Card { Suit = Suit.Clubs, Face = Face.King, Player = Player.North}, 
            new Card { Suit = Suit.Diamonds, Face = Face.Ace, Player = Player.East}, 
            new Card { Suit = Suit.Hearts, Face = Face.Queen, Player = Player.South}, 
            new Card { Suit = Suit.Spades, Face = Face.Jack, Player = Player.West}], Suit.Spades));

        Assert.Equal(Player.West, CalculateDoubleDummy.GetPlayerToPlay(
        [new Card { Suit = Suit.Spades, Face = Face.Jack, Player = Player.West}, 
            new Card { Suit = Suit.Diamonds, Face = Face.Queen, Player = Player.North}, 
            new Card { Suit = Suit.Diamonds, Face = Face.King, Player = Player.East}, 
            new Card { Suit = Suit.Diamonds, Face = Face.Ace, Player = Player.South}], Suit.Spades));
    }    
    
    private void DoCalculate(string filePath, double average)
    {
        var pbn = new Common.Pbn();
        pbn.Load(filePath);
        var firstBoard = pbn.Boards[0];
        var hand = StringToCards(firstBoard.Deal);
        var res = CalculateDoubleDummy.CalculateBestPlayForCombination(Suit.Spades, hand);
        Assert.Equal(hand[Player.North].Count(), res.Tricks.Count);
        testOutputHelper.WriteLine($"NrOfNodes:{res.NrOfEndNodes}");
        foreach (var card in res.Tricks)
        {
            testOutputHelper.WriteLine($"Card:{card.Item1.Suit}{card.Item1.Face} Tricks:{card.Item2}");
        }
        Assert.Equal(average, res.Tricks.Average(x => x.Item2), 0.01);
    }    

    private static IDictionary<Player, IEnumerable<Card>> StringToCards(Dictionary<Common.Player, string> board)
    {
        return board.ToDictionary(x => CommonPlayerToPlayer(x.Key), y => HandToCards(y.Value, CommonPlayerToPlayer(y.Key)));
        
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

    private static IEnumerable<Card> HandToCards(string s, Player player)
    {
        var ret = s.Split(',').Select((x, index) =>
            x.Select(y => new Card() { Face = GetFaceFromDescription(y), Suit = (Suit)index , Player = player})).SelectMany(x => x);
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