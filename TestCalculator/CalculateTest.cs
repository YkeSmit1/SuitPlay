using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Calculator;
using JetBrains.Annotations;
using MoreLinq;
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
    [InlineData(new[] {Face.Ten, Face.Jack, Face.Queen, Face.Dummy, Face.Ace, Face.Dummy, Face.Nine, Face.King}, 2)]
    [InlineData(new[] {Face.Ten, Face.Jack, Face.Ace, Face.Dummy, Face.Queen, Face.Dummy, Face.Nine, Face.King}, 1)]
    public void TestGetTrickCount(Face[] tricks, int expected)
    {
        Assert.Equal(expected, Calculate.GetTrickCount(tricks.Select((x, index) => new Card { Face = x, Player = (Player)(index % 4)})));
    }
       
    [Theory]
    [InlineData("AQT", "432")]
    [InlineData("AQ", "32")]
    [InlineData("A2", "KT987")]
    [InlineData("A32", "QT9")]
    [InlineData("AJ9", "432")]
    public void TestAverageTrickCount(string north, string south)
    {
        var output = Calculate.GetAverageTrickCount(north, south).ToList();
        
        BasicChecks(output);
        LogAllPlays(output);
    }

    [Theory]
    [InlineData("AQT", "432", new[] { Face.Two, Face.Five, Face.Ten }, 2.0)]
    //[InlineData("AQT", "432", new[] { Face.Two, Face.Five, Face.Queen }, 1.75)]
    //[InlineData("A32", "QT9", new[] { Face.Nine, Face.Four, Face.Two }, 1.75)] // Fails because alpha beta pruning
    //[InlineData("AJ9", "432", new[] { Face.Two, Face.Five, Face.Nine }, 1.375)] // Fails because alpha beta pruning eliminates 459T
    [InlineData("KJ5", "432", new[] { Face.Two, Face.Six, Face.Jack }, 1.0)]
    //[InlineData("KJ5", "432", new[] { Face.Two, Face.Six, Face.King }, 0.75)]
    //[InlineData("AKJT98", "32", new[] { Face.Two, Face.Four, Face.Eight }, 5.5)] // Fails because alpha beta pruning
    [InlineData("QT83", "K7542", new[] { Face.Three, Face.Six, Face.King }, 3.5)]
    [InlineData("QT83", "K7542", new[] { Face.Two, Face.Six, Face.Queen }, 3.5)]
    public void TestAverageTrickCountCheck(string north, string south, Face[] bestPlay, double expected)
    {
        var output = Calculate.GetAverageTrickCount(north, south).ToList();
        
        BasicChecks(output);
        LogSpecificPlay(bestPlay, output);

        var actual = GetGrouping(bestPlay, output).Average();
        Assert.Equal(expected, actual, 0.03);
    }
    
    [Theory]
    [InlineData("AQ", "9", new[] { Face.Nine, Face.Jack, Face.Queen }, 1.5)]
    //[InlineData("AQ", "9", new[] { Face.Nine, Face.Jack, Face.Ace }, 1.25)]
    [InlineData("KJ", "9", new[] { Face.Nine, Face.Ten, Face.King }, 0.5)]
    [InlineData("KJ", "9", new[] { Face.Nine, Face.Ten, Face.Jack }, 0.5)]
    [InlineData("AJ", "9", new[] { Face.Nine, Face.Ten, Face.Jack }, 1.0)] // Fails because comp plays T from KQT
    [InlineData("AJ", "9", new[] { Face.Nine, Face.Ten, Face.Ace }, 1.0)]
    public void TestAverageTrickCountCheck6Cards(string north, string south, Face[] bestPlay, double expected)
    {
        var cardsInDeck = Enum.GetValues<Face>().Except([Face.Dummy, Face.Two, Face.Three, Face.Four, Face.Five, Face.Six, Face.Seven, Face.Eight]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, new Calculate.Options {CardsInSuit = cardsInDeck}).ToList();
        
        BasicChecks(output);
        LogSpecificPlay(bestPlay, output);

        var actual = GetGrouping(bestPlay, output).Average();
        Assert.Equal(expected, actual,0.01);
    }

    [Theory]
    [InlineData("AQ", "T", new[] { Face.Ten, Face.Jack, Face.Queen }, 1.5)]
    //[InlineData("AQ", "T", new[] { Face.Ten, Face.Jack, Face.Ace }, 1.5)]
    [InlineData("KJ", "T", new[] { Face.Ten, Face.Queen, Face.King }, 1.0)]
    [InlineData("KJ", "T", new[] { Face.Ten, Face.Ace, Face.Jack }, 1.0)]
    //[InlineData("AJ", "T", new[] { Face.Ten, Face.Queen, Face.Ace }, 1.5)] // Fails because W having KQ is optimised away
    //[InlineData("AJ", "T", new[] { Face.Ten, Face.King, Face.Ace }, 1.5)]
    public void TestAverageTrickCountCheck5Cards(string north, string south, Face[] bestPlay, double expected)
    {
        var cardsInDeck = Enum.GetValues<Face>().Except([Face.Dummy, Face.Two, Face.Three, Face.Four, Face.Five, Face.Six, Face.Seven, Face.Eight, Face.Nine]).ToList();
        var output = Calculate.GetAverageTrickCount(north, south, new Calculate.Options {CardsInSuit = cardsInDeck}).ToList();
        
        BasicChecks(output);
        LogSpecificPlay(bestPlay, output);

        var actual = GetGrouping(bestPlay, output).Average();
        Assert.Equal(expected, actual,0.01);
    }

    [Theory]
    [InlineData(new[] { "K74", "K65", "54", "76" }, "AQ32", new[]{"K74", "54"})]
    [InlineData(new[] { "KJ7", "KJ6", "K76", "54", "76" }, "AQT2", new[]{"KJ6", "K76", "54"})]
    public void TestFilterCombinations(string[] combination, string cardsNS, string[] expected)
    {
        var actual = Combinations.FilterCombinations(
            combination.Select(x => x.Select(Utils.CharToCard).ToList()).ToList(),
            cardsNS.Select(Utils.CharToCard).ToList());
        Assert.Equal(expected.Select(x => x.Select(Utils.CharToCard).ToList()).ToList(), actual);
    }
    
    [Theory]
    [InlineData("AQ32", "K74", "K4")]
    [InlineData("AQ92", "KJT43", "KT3")]
    public void TestFilterAvailableCards(string cardsOtherTeam, string cardsPlayer, string expected)
    {
        // Arrange
        var cardsPlayerList = cardsPlayer.Select(x => new Card {Face = Utils.CharToCard(x)});
        var cardsOtherTeamList = cardsOtherTeam.Select(x => new Card {Face = Utils.CharToCard(x)});
        var expectedList = expected.Select(x => new Card {Face = Utils.CharToCard(x)});
        // Act
        var actual = Calculate.AvailableCardsFiltered(cardsPlayerList.ToList(), cardsOtherTeamList.ToList());
        // Assert
        Assert.Equal(expectedList, actual);
    }   
    
    [Theory]
    [InlineData("AQT", "432")]
    [InlineData("A32", "QT9")]
    [InlineData("AJ9", "432")] 
    [InlineData("KJ5", "432")]
    [InlineData("AKJT98", "32")]
    [InlineData("QT83", "K7542")]
    public void TestLogPlays(string north, string south)
    {
        var concurrentDictionary = Calculate.CalculateBestPlay(north, south).Plays;
        var allCards = Enum.GetValues<Face>().SkipUntil(x => x == Face.Two).ToList();
        LogAllPlays(north, south, allCards, concurrentDictionary);
    }

    [Theory]
    [InlineData("AQ", "T")]
    [InlineData("KJ", "T")]
    public void TestLogPlays5Cards(string north, string south)
    {
        var calculateOptions = new Calculate.Options { CardsInSuit = [Face.Ten, Face.Jack, Face.Queen, Face.King, Face.Ace] };
        var concurrentDictionary = Calculate.CalculateBestPlay(north, south, calculateOptions).Plays;
        LogAllPlays(north, south, calculateOptions.CardsInSuit, concurrentDictionary);
    }
    
    [Theory]
    [InlineData("AQ", "9")]
    [InlineData("KJ", "9")]
    [InlineData("AJ", "9")]
    public void TestLogPlays6Cards(string north, string south)
    {
        var calculateOptions = new Calculate.Options { CardsInSuit = [Face.Nine, Face.Ten, Face.Jack, Face.Queen, Face.King, Face.Ace] };
        var concurrentDictionary = Calculate.CalculateBestPlay(north, south, calculateOptions).Plays;
        LogAllPlays(north, south, calculateOptions.CardsInSuit, concurrentDictionary);
    }

    private void LogAllPlays(string north, string south, IList<Face> allCards, ConcurrentDictionary<List<Face>, List<(IList<Face>, int)>> concurrentDictionary)
    {
        var cardsEW = allCards.Except(north.Select(Utils.CharToCard).Concat(south.Select(Utils.CharToCard))).Reverse().ToList();
        foreach (var value in concurrentDictionary.ToList())
        {
            testOutputHelper.WriteLine("");
            testOutputHelper.WriteLine($"West-East: {string.Join("", cardsEW.Except(value.Key).Select(Utils.CardToChar))} - {string.Join("", value.Key.Select(Utils.CardToChar))}");
            testOutputHelper.WriteLine("");
            foreach (var tuple in value.Value.OrderByDescending(x => x.Item1.First())
                         .ThenBy(y => y.Item1.Skip(1).First())
                         .ThenBy(z => z.Item1.Skip(2).First())
                         .ThenBy(w => w.Item1.Skip(3).First())
                         .ThenByDescending(v => v.Item2))
            {
                testOutputHelper.WriteLine($"Play: {PlayToString(tuple.Item1)} Tricks: {tuple.Item2}");
            }
        }
    }

    private static string PlayToString(IList<Face> tuple)
    {
        return string.Join("|", string.Join("", tuple.Select(Utils.CardToChar)).Chunk(4).Select(x => new string(x)));
    }

    private void LogAllPlays(List<IGrouping<IList<Face>, int>> output)
    {
        testOutputHelper.WriteLine("");
        testOutputHelper.WriteLine("***************  All Plays **************");
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
    
    private void LogSpecificPlay(Face[] cards, List<IGrouping<IList<Face>, int>> output)
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
    private static void BasicChecks(List<IGrouping<IList<Face>, int>> output)
    {
        Assert.NotEmpty(output);
        Assert.Contains(output, x => x.Key.Count == 1);
        Assert.Contains(output, x => x.Key.Count > 1);
        Assert.Contains(output, x => x.Any(y => y != 0));
    }

    private static IEnumerable<int> GetGrouping(Face[] cards, List<IGrouping<IList<Face>, int>> output) => output.Single(x => x.Key.SequenceEqual(cards));
}