using System.Text.Json;
using Calculator;
using Calculator.Models;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace TestCalculator;

[TestSubject(typeof(Calculate))]
public class CalculateTest
{
    private static readonly JsonSerializerOptions JsonSerializerOptions  = new() { WriteIndented = false, IncludeFields = true };
    [UsedImplicitly] private readonly ITestOutputHelper testOutputHelper;

    public CalculateTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
        Utils.SetupLogging();
    }

    [Theory]
    [InlineData(new[] {Face.Ten, Face.Jack, Face.Queen, Face.Dummy, Face.Ace, Face.Dummy, Face.Nine, Face.King}, 2)]
    [InlineData(new[] {Face.Ten, Face.Jack, Face.Ace, Face.Dummy, Face.Queen, Face.Dummy, Face.Nine, Face.King}, 1)]
    public void TestGetTrickCount(Face[] tricks, int expected)
    {
        var dictionary = tricks.Select((x, index) => (x, index)).
            GroupBy(x => (Player)(x.index % 4), y => y.x).
            ToDictionary(key => key.Key, value => value.Select(x => x).ToList());
        Assert.Equal(expected, MiniMax.GetTrickCount(new Cards(tricks.ToList()), dictionary));
    }

    [Theory]
    [InlineData(new[] { "K74", "K65", "54", "76" }, "AQ32", new[]{"K74", "54"})]
    [InlineData(new[] { "KJ7", "KJ6", "K76", "54", "76" }, "AQT2", new[]{"KJ6", "K76", "54"})]
    [InlineData(new[] { "K76", "K75", "K65", "76", "75", "65" }, "AQT98432", new[]{"K65", "65"})]
    public void TestFilterCombinations(string[] combination, string cardsNS, string[] expected)
    {
        var actual = combination.Select(x => x.Select(Utils.CharToCard)).ToList();
        actual.RemoveAll(faces => Calculate.SimilarCombinationsCount(actual, faces.ToList(), cardsNS.Select(Utils.CharToCard)) > 0);
        Assert.Equal(expected.Select(x => x.Select(Utils.CharToCard).ToList()).ToList(), actual.Select(x => x.ToList()));
    }
    
    [Theory]
    [InlineData("K74", "AQ32", "K4", "")]
    [InlineData("KJT43", "AQ92", "KT3", "")]
    [InlineData("KJT76", "AQ985432", "KT6", "")]
    [InlineData("KJT762", "AQ98543", "KT62", "")]
    [InlineData("KJT76", "AQ98543", "KT6", "")]
    [InlineData("KJT2", "AQ98543", "KT2", "")]
    [InlineData("6", "AQT985432", "6", "")]
    [InlineData("KJ7", "AQT985432", "KJ7", "")]
    [InlineData("AQT98", "KJ76", "AQ8", "")]
    [InlineData("QT9", "KJ765", "Q9", "")]
    [InlineData("QT9", "KJ765", "9", "852J")]
    public void TestFilterAvailableCards(string cardsPlayer, string cardsOtherTeam, string expected, string playedCards)
    {
        // Arrange
        var cardsPlayerList = cardsPlayer.Select(Utils.CharToCard);
        var cardsOtherTeamList = cardsOtherTeam.Select(Utils.CharToCard);
        var expectedList = expected.Select(Utils.CharToCard);
        var playedCardsList = new Cards(playedCards.Select(Utils.CharToCard).ToList());
        // Act
        var actual = MiniMax.AvailableCardsFiltered(cardsPlayerList.ToList(), cardsOtherTeamList.ToList(), playedCardsList);
        // Assert
        Assert.Equal(expectedList, actual);
    }

    [Theory]
    [InlineData("K74", "AQ32", "Kxx")]
    [InlineData("KJ52", "AQT3", "KJ5x")]
    [InlineData("9852", "AKQJT", "xxxx")]
    [InlineData("268", "AQT97543", "x68")]
    [InlineData("32Q", "AQT98543", "3xQ")]
    [InlineData("36Q", "AQT98543", "36Q")]
    public void TestConvertToSmallCards(string combination, string cardsNS, string expected) 
    {
        // Arrange
        var hand = combination.Select(Utils.CharToCard); 
        var cardsNSOrdered = cardsNS.Select(Utils.CharToCard);
        // Act
        var actual = hand.ConvertToSmallCards(cardsNSOrdered);
        // Assert
        Assert.Equal(expected.Select(Utils.CharToCard), actual);
    }

    [Theory]
    [InlineData("AQT98", "5432")]
    [InlineData("QT98", "A432")]
    [InlineData("AJ92", "K843")]
    [InlineData("AQJ", "T987654")]
    [InlineData("AQJ", "T9876543")]
    [InlineData("AT32", "Q654")]
    public void TestEqualToEtalon(string north, string south)
    {
        // Arrange 
        var northHand = Utils.StringToCardArray(north);
        var southHand = Utils.StringToCardArray(south);
        // Act
        var bestPlay = Calculate.CalculateBestPlay(northHand, southHand);
        var filename = $"{north}-{south}.json";
        var result = Calculate.GetResult(bestPlay, northHand, southHand);
        Utils.SaveTrees(result, filename);
        
        // Assert
        var json = File.ReadAllText(filename);
        var etalon = File.ReadAllText(Path.Combine("etalons", filename));
        Assert.Equal(etalon, json);
    }
    
    [Theory]
    [InlineData("AQT98", "5432")]
    [InlineData("QT98", "A432")]
    [InlineData("AJ92", "K843")]
    [InlineData("AQJ", "T987654")]
    [InlineData("AQJ", "T9876543")]
    [InlineData("AT32", "Q654")]
    [InlineData("QJ2", "AT3")]
    public void TestEqualToEtalon2(string north, string south)
    {
        // Arrange 
        var northHand = Utils.StringToCardArray(north);
        var southHand = Utils.StringToCardArray(south);
        // Act
        var bestPlay = Calculate.CalculateBestPlay(northHand, southHand);
        var filename2 = $"{north}-{south}-2.json";
        var result2 = Calculate.GetResult2(bestPlay, northHand, southHand);
        Utils.SaveTrees2(result2, filename2);
        
        // Assert
        var json = File.ReadAllText(filename2);
        var etalon = File.ReadAllText(Path.Combine("etalons-2", filename2));
        Assert.Equal(etalon, json);
    }

    [Theory]
    [InlineData("AQT98-5432.json", new[] {"2xQ", "2xA", "2x8"})]
    [InlineData("QT98-A432.json", new[] {"8x2", "Qx2"})]
    [InlineData("AJ92-K843.json", new[] {"2xK", "Ax3", "3xJ"})]
    [InlineData("AQJ-T987654.json", new[] {"4xJ"})]
    [InlineData("AQJ-T9876543.json", new[] {"3xA"})]
    [InlineData("AT32-Q654.json", new[] {"Ax4", "2xQ"})]
    public void CompareWithOld(string fileName, string[] plays)
    {
        using var fileStreamOld = new FileStream(Path.Combine("etalons-suitplay", fileName), FileMode.Open);
        var resultsOld = JsonSerializer.Deserialize<(Dictionary<string, List<int>> treesForJson, IEnumerable<string>)>(fileStreamOld, JsonSerializerOptions);
        
        using var fileStreamNew = new FileStream(Path.Combine("etalons", fileName), FileMode.Open);
        var resultsNew = JsonSerializer.Deserialize<(Dictionary<string, List<int>> treesForJson, IEnumerable<string>)>(fileStreamNew, JsonSerializerOptions);

        Assert.Equal(resultsOld.Item2, resultsNew.Item2);
        var combinations = resultsOld.Item2.ToList();
        foreach (var play in plays)
        {
            var zipped = resultsOld.treesForJson[play].Zip(resultsNew.treesForJson[play]);
            foreach (var tuple in zipped.Index()) 
            {
                if (tuple.Item.First != tuple.Item.Second)
                    testOutputHelper.WriteLine($"Values not equal. file:{fileName} play:{play} East:{combinations[tuple.Index]} old:{tuple.Item.First} new:{tuple.Item.Second} ");
            }
        }
    }

    [Theory]
    [InlineData("2xAx", "2xAx")]
    [InlineData("2xA_", "2xA")]
    [InlineData("2xAK", "2xA")]
    [InlineData("2_Ax", "2")]
    [InlineData("2xAxK", "2xAxK")]
    [InlineData("2_", "2")]
    public void TestOnlySmallCardsEW(string play, string expected)
    {
        var actual = Utils.CardsToString(Utils.StringToCardArray(play).OnlySmallCardsEW());
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("A", "", false, false)]
    [InlineData("", "2", false, false)]
    [InlineData("A", "2", false, false)]
    [InlineData("432", "AQJ", false, true)]
    [InlineData("QJ", "A2", false, true)]
    [InlineData("QJT", "A32", false, true)]
    [InlineData("QT9", "A32", true, true)]
    [InlineData("QT2", "AJ3", false, true)]
    [InlineData("QJ2", "AT3", false, true)]
    [InlineData("QJ2", "A43", false, true)]
    [InlineData("QJ32", "A54", true, true)]
    [InlineData("QT32", "AJ4", false, true)]
    [InlineData("32", "K4", false, true)]
    [InlineData("JT2", "K43", false, true)]
    [InlineData("Q32", "AT4", true, true)]
    [InlineData("K32", "Q54", true, true)]
    [InlineData("KJ2", "AT3", true, true)]
    [InlineData("QT98", "A432", true, true)]
    [InlineData("AQT98", "5432", true, false)]
    [InlineData("AT32", "Q654", true, true)]
    [InlineData("AJ92", "K843", true, true)]
    public void TestHasForks(string north, string south, bool expectedHasForksNorth, bool expectedHasForksSouth)
    {
        // Arrange
        var northHand = Utils.StringToCardArray(north);
        var southHand = Utils.StringToCardArray(south);
        var cardsEW = Enum.GetValues<Face>().Except(northHand).Except(southHand).Except([Face.Dummy, Face.SmallCard]).ToList();
        // Act
        var hasForksNorth = MiniMax.HasForks(northHand.ToList(), southHand.ToList(), cardsEW);
        var hasForksSouth = MiniMax.HasForks(southHand.ToList(), northHand.ToList(), cardsEW);
        // Assert
        Assert.Equal(expectedHasForksNorth, hasForksNorth);
        Assert.Equal(expectedHasForksSouth, hasForksSouth);
    }
}