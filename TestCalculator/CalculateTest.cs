using System.Collections;
using System.Diagnostics.CodeAnalysis;
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
        
        BasicChecks(output);
        LogAllPlays(output);
    }

    [Theory]
    [InlineData("AQT", "432", new[] { Card.Four, Card.Five, Card.Ten }, 2.0)]
    [InlineData("AQT", "432", new[] { Card.Four, Card.Five, Card.Queen }, 1.75)]
    [InlineData("A32", "QT9", new[] { Card.Ten, Card.Four, Card.Three }, 1.75)]
    [InlineData("AJ9", "432", new[] { Card.Four, Card.Five, Card.Nine }, 1.375)] // Fails because alpha beta pruning eliminates 459T
    [InlineData("KJ5", "432", new[] { Card.Four, Card.Six, Card.Jack }, 1.0)]
    [InlineData("KJ5", "432", new[] { Card.Four, Card.Six, Card.King }, 0.75)]
    [InlineData("AKJT98", "32", new[] { Card.Three, Card.Four, Card.Jack }, 5.49)]
    public void TestAverageTrickCountCheck(string north, string south, Card[] bestPlay, double expected)
    {
        var cardsInDeck = Enum.GetValues<Card>().Except([Card.Dummy]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, cardsInDeck).OrderBy(x => x.Key.Count)
            .ThenBy(z => z.Key.First()).ToList();
        
        BasicChecks(output);
        LogSpecificPlay(bestPlay, output);

        var actual = GetGrouping(bestPlay, output).Average();
        Assert.Equal(expected, actual, 0.01);
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
        var output = Calculate.GetAverageTrickCount(north, south, cardsInDeck).OrderBy(x => x.Key.Count)
            .ThenBy(z => z.Key.First()).ToList();
        
        BasicChecks(output);
        LogSpecificPlay(bestPlay, output);

        var actual = GetGrouping(bestPlay, output).Average();
        Assert.Equal(expected, actual,0.01);
    }

    [Theory]
    [InlineData("AQ", "T", new[] { Card.Ten, Card.Jack, Card.Queen }, 1.5)]
    [InlineData("AQ", "T", new[] { Card.Ten, Card.Jack, Card.Ace }, 1.5)]
    [InlineData("KJ", "T", new[] { Card.Ten, Card.Queen, Card.King }, 1.0)]
    [InlineData("KJ", "T", new[] { Card.Ten, Card.Ace, Card.Jack }, 1.0)]
    [InlineData("AJ", "T", new[] { Card.Ten, Card.Queen, Card.Ace }, 1.5)] // Fails because W having KQ is optimised away
    [InlineData("AJ", "T", new[] { Card.Ten, Card.King, Card.Ace }, 1.5)]
    public void TestAverageTrickCountCheck5Cards(string north, string south, Card[] bestPlay, double expected)
    {
        var cardsInDeck = Enum.GetValues<Card>().Except([Card.Dummy, Card.Two, Card.Three, Card.Four, Card.Five, Card.Six, Card.Seven, Card.Eight, Card.Nine]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, cardsInDeck).OrderBy(x => x.Key.Count)
            .ThenBy(z => z.Key.First()).ToList();
        
        BasicChecks(output);
        LogSpecificPlay(bestPlay, output);

        var actual = GetGrouping(bestPlay, output).Average();
        Assert.Equal(expected, actual,0.01);
    }
    
    [Theory]
    [ClassData(typeof(CalculatorTestData))]
    public void TestRemoveBadPlaysSingle(TestDataPlayAndExpected testData)
    {
        Calculate.RemoveBadPlaysSingle(testData.Plays, 3);
        Assert.Equal(testData.Expected, testData.Plays.Count);
    }

    public class TestDataPlayAndExpected
    {
        public required List<(IList<Card>, int)> Plays;
        public int Expected;
    }

    private class CalculatorTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new TestDataPlayAndExpected {Plays = [
                (new[] {Card.Nine, Card.Ten, Card.Jack}, 2), 
                (new[] { Card.Nine, Card.King, Card.Jack }, 1)], Expected = 1}
            ];
            yield return [new TestDataPlayAndExpected {Plays = [
                    (new[] {Card.Nine, Card.Ten, Card.Jack}, 2), 
                    (new[] { Card.Nine, Card.King, Card.Jack }, 2)], Expected = 2}
            ];
            yield return [new TestDataPlayAndExpected {Plays = [
                    (new[] {Card.Nine, Card.Ten, Card.Jack}, 2), 
                    (new[] { Card.Nine, Card.Ten, Card.Jack }, 1)], Expected = 2}
            ];
            yield return [new TestDataPlayAndExpected {Plays = [
                    (new[] {Card.Ten, Card.Jack, Card.Queen}, 2), 
                    (new[] { Card.Ten, Card.King, Card.Queen }, 1),
                    (new[] { Card.Ten, Card.Jack, Card.Ace }, 1),
                    (new[] { Card.Ten, Card.King, Card.Ace }, 2)
                ], Expected = 2}
            ];
            yield return [new TestDataPlayAndExpected {Plays = [
                    (new[] {Card.Ten, Card.Queen, Card.King}, 1), 
                    (new[] { Card.Ten, Card.Ace, Card.King }, 0),
                    (new[] { Card.Ten, Card.Queen, Card.Jack }, 0),
                    (new[] { Card.Ten, Card.Ace, Card.Jack }, 1)
                ], Expected = 2}
            ];
            
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    private void LogAllPlays(List<IGrouping<IList<Card>, int>> output)
    {
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
    
    private void LogSpecificPlay(Card[] cards, List<IGrouping<IList<Card>, int>> output)
    {
        var play = output.Single(x => x.Key.SequenceEqual(cards));
        {
            testOutputHelper.WriteLine($"NS cards: {string.Join(",", play.Key)} Average:{play.Average():0.##} Count:{play.Count()}");
        
            foreach (var groupedTrick in play.GroupBy(x => x))
            {
                testOutputHelper.WriteLine($"Tricks:{groupedTrick.Key} Count:{groupedTrick.Count()}");
            }
        }
    }

    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
    private static void BasicChecks(List<IGrouping<IList<Card>, int>> output)
    {
        Assert.NotEmpty(output);
        Assert.Contains(output, x => x.Key.Count == 1);
        Assert.Contains(output, x => x.Key.Count > 1);
        Assert.Contains(output, x => x.Any(y => y != 0));
    }
    
    IEnumerable<int> GetGrouping(Card[] cards, List<IGrouping<IList<Card>, int>> output) => output.Single(x => x.Key.SequenceEqual(cards));
}