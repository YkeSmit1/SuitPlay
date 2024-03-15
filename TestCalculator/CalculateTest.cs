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

    [Fact]
    public void TestCalculate()
    {
        var output = Calculate.CalculateBestPlay("AQ", "T9").ToList();
        foreach (var play in output)
        {
            foreach (var tuple in play)
            {
                _testOutputHelper.WriteLine($"Card: {tuple.Item1} Trick: {tuple.Item2}");
            }
        }
    }

    [Fact]
    public void TestGetTrickCount()
    {
        Card[] tricks = [Card.Ten, Card.Jack, Card.Queen, Card.Dummy, Card.Ace, Card.Dummy, Card.Nine, Card.King];
        Assert.Equal(2, Calculate.GetTrickCount(tricks));
        Card[] tricks2 = [Card.Ten, Card.Jack, Card.Ace, Card.Dummy, Card.Queen, Card.Dummy, Card.Nine, Card.King];
        Assert.Equal(1, Calculate.GetTrickCount(tricks2));
    }
}