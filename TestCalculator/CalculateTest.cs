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
       
    [Theory]
    [InlineData("AQ", "T9",
        new[] { Card.Ten, Card.Ten, Card.Ten, Card.Ten, Card.Ace, Card.Ten, Card.Ten, Card.Ace, Card.Ten, Card.Ace, Card.Ace,
            Card.Ace, Card.Ace, Card.Ace, Card.Ace, Card.Ace },
        new[] { 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 2, 1, 1, 1, 1 })]
    [InlineData("AQ8", "T9",
        new[]
        {
            Card.Ten, Card.Ten, Card.Ten, Card.Ace, Card.Ace, Card.Ace, Card.Ace, Card.Ace
        }, new[] { 3, 3, 3, 3, 3, 2, 2, 1})]
    //[InlineData("A2", "KT987", new[] {Card.Ace, Card.Ten, Card.Ace, Card.Ace}, new[] {3, 3, 3, 3})]
    public void TestCalculateExpected(string north, string south, Card[] expectedPlay, int[] expectedTricks)
    {
        var output = Calculate.CalculateBestPlay(north, south).ToList();
        var counter = 0;
        foreach (var play in output)
        {
            _testOutputHelper.WriteLine($"East: {string.Join(",", play.Item1)} Best play: {play.Item2.Item1} Tricks:{play.Item2.Item2}");
            Assert.Equal(expectedPlay[counter], play.Item2.Item1);
            Assert.Equal(expectedTricks[counter], play.Item2.Item2);
            counter++;
        }
    }
    
}