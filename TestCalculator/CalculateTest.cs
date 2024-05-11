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
    [InlineData(new[] {CardFace.Ten, CardFace.Jack, CardFace.Queen, CardFace.Dummy, CardFace.Ace, CardFace.Dummy, CardFace.Nine, CardFace.King}, 2)]
    [InlineData(new[] {CardFace.Ten, CardFace.Jack, CardFace.Ace, CardFace.Dummy, CardFace.Queen, CardFace.Dummy, CardFace.Nine, CardFace.King}, 1)]
    public void TestGetTrickCount(CardFace[] tricks, int expected)
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
        var cardsInDeck = Enum.GetValues<CardFace>().Except([CardFace.Dummy]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, new CalculateOptions {CardsInSuit = cardsInDeck}).ToList();
        
        BasicChecks(output);
        LogAllPlays(output);
    }

    [Theory]
    [InlineData("AQT", "432", new[] { CardFace.Four, CardFace.Five, CardFace.Ten }, 2.0, true)]
    [InlineData("AQT", "432", new[] { CardFace.Four, CardFace.Five, CardFace.Queen }, 1.75, true)]
    [InlineData("A32", "QT9", new[] { CardFace.Ten, CardFace.Four, CardFace.Three }, 1.75, false)] // Fails because alpha beta pruning
    [InlineData("AJ9", "432", new[] { CardFace.Four, CardFace.Five, CardFace.Nine }, 1.375, false)] // Fails because alpha beta pruning eliminates 459T
    [InlineData("KJ5", "432", new[] { CardFace.Four, CardFace.Six, CardFace.Jack }, 1.0, true)]
    [InlineData("KJ5", "432", new[] { CardFace.Four, CardFace.Six, CardFace.King }, 0.75, true)]
    [InlineData("AKJT98", "32", new[] { CardFace.Three, CardFace.Four, CardFace.Jack }, 5.5, false)] // Fails because alpha beta pruning
    public void TestAverageTrickCountCheck(string north, string south, CardFace[] bestPlay, double expected, bool usePruning)
    {
        var cardsInDeck = Enum.GetValues<CardFace>().Except([CardFace.Dummy]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, new CalculateOptions {CardsInSuit = cardsInDeck, UsePruning = usePruning}).
            OrderBy(x => x.Key.Count).ThenBy(z => z.Key.First()).ToList();
        
        BasicChecks(output);
        LogSpecificPlay(bestPlay, output);

        var actual = GetGrouping(bestPlay, output).Average();
        Assert.Equal(expected, actual, 0.01);
    }
    
    [Theory]
    [InlineData("AQ", "9", new[] { CardFace.Nine, CardFace.Jack, CardFace.Queen }, 1.5)]
    [InlineData("AQ", "9", new[] { CardFace.Nine, CardFace.Jack, CardFace.Ace }, 1.25)]
    [InlineData("KJ", "9", new[] { CardFace.Nine, CardFace.Ten, CardFace.King }, 0.5)]
    [InlineData("KJ", "9", new[] { CardFace.Nine, CardFace.Ten, CardFace.Jack }, 0.5)]
    [InlineData("AJ", "9", new[] { CardFace.Nine, CardFace.Ten, CardFace.Jack }, 1.0)] // Fails because comp plays T from KQT
    [InlineData("AJ", "9", new[] { CardFace.Nine, CardFace.Ten, CardFace.Ace }, 1.0)]
    public void TestAverageTrickCountCheck6Cards(string north, string south, CardFace[] bestPlay, double expected)
    {
        var cardsInDeck = Enum.GetValues<CardFace>().Except([CardFace.Dummy, CardFace.Two, CardFace.Three, CardFace.Four, CardFace.Five, CardFace.Six, CardFace.Seven, CardFace.Eight]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, new CalculateOptions {CardsInSuit = cardsInDeck, FilterBadPlaysByEW = true}).
            OrderBy(x => x.Key.Count).ThenBy(z => z.Key.First()).ToList();
        
        BasicChecks(output);
        LogSpecificPlay(bestPlay, output);

        var actual = GetGrouping(bestPlay, output).Average();
        Assert.Equal(expected, actual,0.01);
    }

    [Theory]
    [InlineData("AQ", "T", new[] { CardFace.Ten, CardFace.Jack, CardFace.Queen }, 1.5)]
    [InlineData("AQ", "T", new[] { CardFace.Ten, CardFace.Jack, CardFace.Ace }, 1.5)]
    [InlineData("KJ", "T", new[] { CardFace.Ten, CardFace.Queen, CardFace.King }, 1.0)]
    [InlineData("KJ", "T", new[] { CardFace.Ten, CardFace.Ace, CardFace.Jack }, 1.0)]
    //[InlineData("AJ", "T", new[] { CardFace.Ten, CardFace.Queen, CardFace.Ace }, 1.5)] // Fails because W having KQ is optimised away
    [InlineData("AJ", "T", new[] { CardFace.Ten, CardFace.King, CardFace.Ace }, 1.5)]
    public void TestAverageTrickCountCheck5Cards(string north, string south, CardFace[] bestPlay, double expected)
    {
        var cardsInDeck = Enum.GetValues<CardFace>().Except([CardFace.Dummy, CardFace.Two, CardFace.Three, CardFace.Four, CardFace.Five, CardFace.Six, CardFace.Seven, CardFace.Eight, CardFace.Nine]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, new CalculateOptions {CardsInSuit = cardsInDeck}).
            OrderBy(x => x.Key.Count).ThenBy(z => z.Key.First()).ToList();
        
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
        public required List<(IList<CardFace>, int)> Plays;
        public int Expected;
    }

    private class CalculatorTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new TestDataPlayAndExpected {Plays = [
                (new[] {CardFace.Nine, CardFace.Ten, CardFace.Jack}, 2), 
                (new[] { CardFace.Nine, CardFace.King, CardFace.Jack }, 1)], Expected = 1}
            ];
            yield return [new TestDataPlayAndExpected {Plays = [
                    (new[] {CardFace.Nine, CardFace.Ten, CardFace.Jack}, 2), 
                    (new[] { CardFace.Nine, CardFace.King, CardFace.Jack }, 2)], Expected = 2}
            ];
            yield return [new TestDataPlayAndExpected {Plays = [
                    (new[] {CardFace.Nine, CardFace.Ten, CardFace.Jack}, 2), 
                    (new[] { CardFace.Nine, CardFace.Ten, CardFace.Jack }, 1)], Expected = 2}
            ];
            yield return [new TestDataPlayAndExpected {Plays = [
                    (new[] { CardFace.Ten, CardFace.Jack, CardFace.Queen }, 2),
                    (new[] { CardFace.Ten, CardFace.King, CardFace.Queen }, 1),
                    (new[] {CardFace.Ten, CardFace.Jack, CardFace.Ace}, 1), 
                    (new[] { CardFace.Ten, CardFace.King, CardFace.Ace }, 2)
                ], Expected = 4}
            ];
            yield return [new TestDataPlayAndExpected {Plays = [
                    (new[] {CardFace.Ten, CardFace.Queen, CardFace.King}, 1), 
                    (new[] { CardFace.Ten, CardFace.Ace, CardFace.King }, 0),
                    (new[] { CardFace.Ten, CardFace.Queen, CardFace.Jack }, 0),
                    (new[] { CardFace.Ten, CardFace.Ace, CardFace.Jack }, 1)
                ], Expected = 4}
            ];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    private void LogAllPlays(List<IGrouping<IList<CardFace>, int>> output)
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
    
    private void LogSpecificPlay(CardFace[] cards, List<IGrouping<IList<CardFace>, int>> output)
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
    private static void BasicChecks(List<IGrouping<IList<CardFace>, int>> output)
    {
        Assert.NotEmpty(output);
        Assert.Contains(output, x => x.Key.Count == 1);
        Assert.Contains(output, x => x.Key.Count > 1);
        Assert.Contains(output, x => x.Any(y => y != 0));
    }
    
    IEnumerable<int> GetGrouping(CardFace[] cards, List<IGrouping<IList<CardFace>, int>> output) => output.Single(x => x.Key.SequenceEqual(cards));
}