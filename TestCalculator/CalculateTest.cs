using Calculator;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace TestCalculator;

[TestSubject(typeof(Calculate))]
public class CalculateTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public CalculateTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(new[] {Card.Ten, Card.Jack, Card.Queen, Card.Dummy, Card.Ace, Card.Dummy, Card.Nine, Card.King}, 2)]
    [InlineData(new[] {Card.Ten, Card.Jack, Card.Ace, Card.Dummy, Card.Queen, Card.Dummy, Card.Nine, Card.King}, 1)]
    public void TestGetTrickCount(Card[] tricks, int expected)
    {
        Assert.Equal(expected, Calculate.GetTrickCount(tricks));
    }
       
    [Theory]
    [InlineData("AQT", "432")]
    [InlineData("AQ", "32")]
    [InlineData("A2", "KT987")]
    [InlineData("A32", "QT9")]
    [InlineData("AJ9", "432")]
    public void TestAverageTrickCount(string north, string south)
    {
        var cardsInDeck = Enum.GetValues<Card>().Except([Card.Dummy]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, cardsInDeck).ToList();
        
        Assert.NotEmpty(output);
        Assert.Contains(output, x => x.Key.Count == 1);
        Assert.Contains(output, x => x.Key.Count > 1);
        Assert.Contains(output, x => x.Average() != 0);
        var currentLength = 0;
        foreach (var play in output)
        {
            if (play.Key.Count != currentLength)
            {
                testOutputHelper.WriteLine("");
                testOutputHelper.WriteLine($"*****  Number of cards:{play.Key.Count}  *****");
                currentLength = play.Key.Count;
            }

            testOutputHelper.WriteLine($"NS cards: {string.Join(",", play.Key)} Average:{play.Average():0.##} Count:{play.Count()}");
            var groupedTricks = play.GroupBy(x => x);

            foreach (var groupedTrick in groupedTricks)
            {
                testOutputHelper.WriteLine($"Tricks:{groupedTrick.Key} Count:{groupedTrick.Count()}");
            }
        }
    }
    
    [Theory]
    [InlineData("AQT", "432", new[] { Card.Four, Card.Five, Card.Ten }, 2.0)]
    [InlineData("A32", "QT9", new[] { Card.Ten, Card.Four, Card.Three }, 1.75)]
    [InlineData("AJ9", "432", new[] { Card.Four, Card.Five, Card.Nine }, 1.375)]
    public void TestAverageTrickCountCheck(string north, string south, Card[] bestPlay, double expected)
    {
        var cardsInDeck = Enum.GetValues<Card>().Except([Card.Dummy]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, cardsInDeck).ToList();
        
        Assert.NotEmpty(output);
        Assert.Contains(output, x => x.Key.Count == 1);
        Assert.Contains(output, x => x.Key.Count > 1);
        Assert.Contains(output, x => x.Any(y => y != 0));

        var actual = GetGrouping(bestPlay).Average();
        Assert.Equal(expected, actual, 0.01);
        var expectedMax = GetMaxAverageForNrOfCards(bestPlay.Length);
        Assert.Equal(expectedMax, actual, 0.01);
        return;

        double GetMaxAverageForNrOfCards(int nrOfCards) => output.Where(x => x.Key.Count == nrOfCards).Max(x => x.Average());
        IEnumerable<int> GetGrouping(Card[] play) => output.Single(x => x.Key.SequenceEqual(play));
    }
    
    [Theory]
    [InlineData("AQ", "9", new[] { Card.Nine, Card.Jack, Card.Queen }, 1.5)]
    [InlineData("AQ", "9", new[] { Card.Nine, Card.Jack, Card.Ace }, 1.25)]
    [InlineData("KJ", "9", new[] { Card.Nine, Card.Ten, Card.King }, 0.5)]
    [InlineData("KJ", "9", new[] { Card.Nine, Card.Ten, Card.Jack }, 0.5)]
    [InlineData("AJ", "9", new[] { Card.Nine, Card.Ten, Card.Jack }, 1.0)]
    [InlineData("AJ", "9", new[] { Card.Nine, Card.Ten, Card.Ace }, 1.0)]
    public void TestAverageTrickCountCheck6Cards(string north, string south, Card[] bestPlay, double expected)
    {
        var cardsInDeck = Enum.GetValues<Card>().Except([Card.Dummy, Card.Two, Card.Three, Card.Four, Card.Five, Card.Six, Card.Seven, Card.Eight]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, cardsInDeck).ToList();
        
        Assert.NotEmpty(output);
        Assert.Contains(output, x => x.Key.Count == 1);
        Assert.Contains(output, x => x.Key.Count > 1);
        Assert.Contains(output, x => x.Any(y => y != 0));
        
        var play = output.Single(x => x.Key.SequenceEqual(bestPlay));
        {
            testOutputHelper.WriteLine($"NS cards: {string.Join(",", play.Key)} Average:{play.Average():0.##} Count:{play.Count()}");
        
            foreach (var groupedTrick in play.GroupBy(x => x))
            {
                testOutputHelper.WriteLine($"Tricks:{groupedTrick.Key} Count:{groupedTrick.Count()}");
            }
        }

        var actual = GetGrouping(bestPlay).Average();
        Assert.Equal(expected, actual,0.01);
        return;

        IEnumerable<int> GetGrouping(Card[] cards) => output.Single(x => x.Key.SequenceEqual(cards));
    }   
    
   [Theory]
    [InlineData("AQ", "T", new[] { Card.Ten, Card.Jack, Card.Queen }, 1.5)]
    [InlineData("AQ", "T", new[] { Card.Ten, Card.Jack, Card.Ace }, 1.5)]
    [InlineData("KJ", "T", new[] { Card.Ten, Card.Queen, Card.King }, 1.0)]
    [InlineData("KJ", "T", new[] { Card.Ten, Card.Ace, Card.Jack }, 1.0)]
    [InlineData("AJ", "T", new[] { Card.Ten, Card.Queen, Card.Ace }, 1.5)]
    [InlineData("AJ", "T", new[] { Card.Ten, Card.King, Card.Ace }, 1.5)]
    public void TestAverageTrickCountCheck5Cards(string north, string south, Card[] bestPlay, double expected)
    {
        var cardsInDeck = Enum.GetValues<Card>().Except([Card.Dummy, Card.Two, Card.Three, Card.Four, Card.Five, Card.Six, Card.Seven, Card.Eight, Card.Nine]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, cardsInDeck).ToList();
        
        Assert.NotEmpty(output);
        Assert.Contains(output, x => x.Key.Count == 1);
        Assert.Contains(output, x => x.Key.Count > 1);
        Assert.Contains(output, x => x.Any(y => y != 0));
        
        var play = output.Single(x => x.Key.SequenceEqual(bestPlay));
        {
            testOutputHelper.WriteLine($"NS cards: {string.Join(",", play.Key)} Average:{play.Average():0.##} Count:{play.Count()}");
        
            foreach (var groupedTrick in play.GroupBy(x => x))
            {
                testOutputHelper.WriteLine($"Tricks:{groupedTrick.Key} Count:{groupedTrick.Count()}");
            }
        }

        var actual = GetGrouping(bestPlay).Average();
        Assert.Equal(expected, actual,0.01);
        return;

        IEnumerable<int> GetGrouping(Card[] cards) => output.Single(x => x.Key.SequenceEqual(cards));
    }    
    
}