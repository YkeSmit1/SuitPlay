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
    [InlineData("AQ8", "T9")]
    [InlineData("AQ", "T9")]
    [InlineData("A2", "KT987")]
    [InlineData("A87", "QT9")]
    [InlineData("AQT8", "642")]
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
                testOutputHelper.WriteLine($"Number of cards:{play.Key.Count}");
                currentLength = play.Key.Count;
            }

            testOutputHelper.WriteLine($"NS cards: {string.Join(",", play.Key)} Tricks:{play.tricks:0.##}");
        }
    }
}