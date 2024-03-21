using Calculator;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace TestCalculator;

[TestSubject(typeof(Calculate))]
public class CalculateTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CalculateTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("AQ8", "T9")]
    [InlineData("AQ", "T9")]
    [InlineData("A2", "KT987")]
    [InlineData("A87", "QT9")]
    public void TestCalculateBestPlay(string north, string south)
    {
        var output = Calculate.CalculateBestPlay(north, south).ToList();
        Assert.NotEmpty(output);
        foreach (var play in output)
        {
            _testOutputHelper.WriteLine($"East: {string.Join(",", play.Item1)} Best play: {play.Item2.Item1} Tricks:{play.Item2.Item2}");
        }
    }

    [Theory]
    [InlineData(new[] {Card.Ten, Card.Jack, Card.Queen, Card.Dummy, Card.Ace, Card.Dummy, Card.Nine, Card.King}, 2)]
    [InlineData(new[] {Card.Ten, Card.Jack, Card.Ace, Card.Dummy, Card.Queen, Card.Dummy, Card.Nine, Card.King}, 1)]
    public void TestGetTrickCount(Card[] tricks, int expected)
    {
        Assert.Equal(expected, Calculate.GetTrickCount(tricks));
    }
       
    [Theory]
    [InlineData("AQ", "T9")]
    public void TestCalculateExpected(string north, string south)
    {
        var output = Calculate.CalculateBestPlay(north, south).ToList();
        Assert.NotEmpty(output);
        foreach (var play in output)
        {
            _testOutputHelper.WriteLine($"East: {string.Join(",", play.Item1)} Best play: {play.Item2.Item1} Tricks:{play.Item2.Item2}");
            if (play.Item1.Contains(Card.King) && play.Item1.Count() != 1)
            {
                Assert.Equal(Card.Ace, play.Item2.Item1);
                Assert.Equal(1, play.Item2.Item2);
            }
            
            if (play.Item1.Contains(Card.King) && play.Item1.Count() == 1)
            {
                Assert.Equal(Card.Ace, play.Item2.Item1);
                Assert.Equal(2, play.Item2.Item2);
            }
            
            if (!play.Item1.Contains(Card.King) && play.Item1.Count() != 8)
            {
                Assert.Equal(Card.Ten, play.Item2.Item1);
                Assert.Equal(2, play.Item2.Item2);
            }
            
            if (!play.Item1.Contains(Card.King) && play.Item1.Count() == 8)
            {
                Assert.Equal(Card.Ace, play.Item2.Item1);
                Assert.Equal(2, play.Item2.Item2);
            }
        }
    }

    [Theory]
    [InlineData("AQ8", "T9")]
    [InlineData("AQ", "T9")]
    [InlineData("A2", "KT987")]
    [InlineData("A87", "QT9")]
    public void TestAverageTrickCount(string north, string south)
    {
        var output = Calculate.GetAverageTrickCount(north, south).ToList();
        Assert.NotEmpty(output);
        Assert.Contains(output, x => x.Key.Count == 1);
        Assert.Contains(output, x => x.Key.Count > 1);
        Assert.Contains(output, x => x.tricks != 0);
        var currentLength = 0;
        foreach (var play in output)
        {
            if (play.Item1.Count != currentLength)
            {
                _testOutputHelper.WriteLine($"Number of cards:{play.Key.Count}");
                currentLength = play.Key.Count;
            }

            _testOutputHelper.WriteLine($"NS cards: {string.Join(",", play.Key)} Tricks:{play.tricks:0.##}");
        }
    }
}