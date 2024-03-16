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
    public void TestCalculate(string north, string south)
    {
        var output = Calculate.CalculateBestPlay(north, south).ToList();
        foreach (var play in output)
        {
            _testOutputHelper.WriteLine($"East: {string.Join(",", play.Item1)} Best play: {play.Item2.Item1} Tricks:{play.Item2.Item2}");
        }
    }

    [Theory]
    [InlineData(new Card[] {Card.Ten, Card.Jack, Card.Queen, Card.Dummy, Card.Ace, Card.Dummy, Card.Nine, Card.King}, 2)]
    [InlineData(new Card[] {Card.Ten, Card.Jack, Card.Ace, Card.Dummy, Card.Queen, Card.Dummy, Card.Nine, Card.King}, 1)]
    public void TestGetTrickCount(Card[] tricks, int expected)
    {
        Assert.Equal(expected, Calculate.GetTrickCount(tricks));
    }
}