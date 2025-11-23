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
    [InlineData(new[] { "KJ", "K3" }, "AQT2", new[]{"KJ", "K3"})]
    [InlineData(new[] { "K5", "K4" }, "AQ32", new[]{"K4"})]
    [InlineData(new[] { "K74", "K65", "54", "76" }, "AQ32", new[]{"K74", "54"})]
    [InlineData(new[] { "KJ7", "KJ6", "K76", "54", "76" }, "AQT2", new[]{"KJ6", "K76", "54"})]
    [InlineData(new[] { "K76", "K75", "K65", "76", "75", "65" }, "AQT98432", new[]{"K65", "65"})]
    [InlineData(new[] { "KQ", "K7", "K6", "Q7", "Q6", "76" }, "AJT2", new[]{"KQ", "Q6", "76"})]
    public void TestFilterCombinations(string[] combination, string cardsNS, string[] expected)
    {
        var actual = combination.Select(x => x.Select(Utils.CharToCard)).ToList();
        actual.RemoveAll(faces => Calculate.SimilarCombinationsCount(actual, faces.ToList(), cardsNS.Select(Utils.CharToCard)) > 0);
        Assert.Equal(expected.Select(x => x.Select(Utils.CharToCard).ToList()).ToList(), actual.Select(x => x.ToList()));
    }
    
    [Theory]
    [InlineData("K74", "AQ32", "K4")]
    [InlineData("KJT43", "AQ92", "KT3")]
    [InlineData("KJT76", "AQ985432", "KT6")]
    [InlineData("KJT762", "AQ98543", "KT62")]
    [InlineData("KJT76", "AQ98543", "KT6")]
    [InlineData("KJT2", "AQ98543", "KT2")]
    [InlineData("6", "AQT985432", "6")]
    [InlineData("KJ7", "AQT985432", "KJ7")]
    [InlineData("AQT98", "KJ76", "AQ8")]
    [InlineData("QT9", "KJ765", "Q9")]
    public void TestFilterAvailableCards(string cardsPlayer, string cardsOtherTeam, string expected)
    {
        // Arrange
        var cardsPlayerList = cardsPlayer.Select(Utils.CharToCard);
        var cardsOtherTeamList = cardsOtherTeam.Select(Utils.CharToCard);
        var expectedList = expected.Select(Utils.CharToCard);
        // Act
        var actual = MiniMax.AvailableCardsFiltered(cardsPlayerList.ToList(), cardsOtherTeamList.ToList());
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
    [InlineData("QJ2", "AT3")]
    [InlineData("AJ2", "KT3")]
    [InlineData("AJ32", "K954")]
    [InlineData("J92", "A753")]
    [InlineData("J987", "A432")]
    public void TestEqualToEtalon2(string north, string south)
    {
        // Arrange 
        var northHand = Utils.StringToCardArray(north);
        var southHand = Utils.StringToCardArray(south);
        // Act
        var bestPlay = Calculate.CalculateBestPlay(northHand, southHand);
        var filename2 = $"{north}-{south}.json";
        var result2 = Calculate.GetResult2(bestPlay, northHand, southHand);
        Utils.SaveTrees2(result2, filename2);
        
        // Assert
        var json = File.ReadAllText(filename2);
        var etalon = File.ReadAllText(Path.Combine("etalons-2", filename2));
        Assert.Equal(etalon, json);
    }

    [Theory]
    [InlineData("AQT98-5432.json")]
    [InlineData("QT98-A432.json")]
    [InlineData("AJ92-K843.json")]
    [InlineData("AQJ-T987654.json")]
    [InlineData("AQJ-T9876543.json")]
    [InlineData("AT32-Q654.json")]
    [InlineData("AJ32-K954.json")]
    [InlineData("J92-A753.json")]
    [InlineData("J987-A432.json")]
    public void CompareWithOld(string fileName)
    {
        using var fileStreamOld = new FileStream(Path.Combine("etalons-suitplay", fileName), FileMode.Open);
        var resultsOld = JsonSerializer.Deserialize<(Dictionary<string, List<int>> treesForJson, IEnumerable<string>)>(fileStreamOld, JsonSerializerOptions);
        
        using var fileStreamNew = new FileStream(Path.Combine("etalons-2", fileName), FileMode.Open);
        var resultsNew = JsonSerializer.Deserialize<(Dictionary<string, List<int>> treesForJson, IEnumerable<string>)>(fileStreamNew, JsonSerializerOptions);

        var combinationsOld = resultsOld.Item2.ToList();
        var combinationsNew = resultsNew.Item2.ToList();
        foreach (var (lineOld, tricksOld) in resultsOld.treesForJson)
        {
            foreach (var (index, trickNew) in resultsNew.treesForJson[lineOld].Index())
            {
                var combination = combinationsNew[index];
                var indexOfCombination = combinationsOld.IndexOf(combination);
                if (indexOfCombination == -1) continue;
                var trickOld = tricksOld[indexOfCombination];
                if (trickNew != trickOld)
                    testOutputHelper.WriteLine($"Values not equal. file:{fileName} play:{lineOld} East:{combination} old:{trickOld} new:{trickNew} ");
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
    [InlineData(new[]{2}, new[]{1}, 1)]
    [InlineData(new[]{1}, new[]{1}, 0)]
    [InlineData(new[]{1}, new[]{2, 4}, -1)]
    [InlineData(new[]{2}, new[]{2, 4}, -1)]
    [InlineData(new[]{3}, new[]{2, 4}, 0)]
    [InlineData(new[]{4}, new[]{2, 4}, 1)]
    [InlineData(new[]{5}, new[]{2, 4}, 1)]
    [InlineData(new[]{1, 2}, new[]{3, 5}, -1)]
    [InlineData(new[]{1, 3}, new[]{3, 5}, -1)]
    [InlineData(new[]{2, 4}, new[]{3, 5}, 0)]
    [InlineData(new[]{3, 5}, new[]{3, 5}, 0)]
    [InlineData(new[]{4, 6}, new[]{3, 5}, 0)]
    [InlineData(new[]{5, 7}, new[]{3, 5}, 1)]
    [InlineData(new[]{6, 7}, new[]{3, 5}, 1)]
    public void TestIsBetterPlay(int[] a, int[] b, int expected)
    {
        Assert.Equal(Calculate.IsBetterPlay(a, b), expected);
        Assert.Equal(Calculate.IsBetterPlay(b, a), -expected);
    }
    
    [Theory]
    [InlineData("32", "AQ", "", "2")]
    [InlineData("QJ", "A2", "", "J")]
    [InlineData("Q2", "AJ", "", "2")]
    [InlineData("Q2", "A3", "", "AQ32")]
    [InlineData("432", "AQJ", "", "2")]
    [InlineData("432", "AQ5", "", "2")]
    [InlineData("QJT", "A32", "", "T")]
    [InlineData("QJ2", "AT3", "", "2")]
    [InlineData("QT2", "AJ3", "", "2")]
    [InlineData("J32", "AQT", "", "2")]
    [InlineData("QJ2", "A43", "", "AJ32")]
    [InlineData("QJ32", "A54", "", "AJ42")]
    [InlineData("Q32", "AT4", "", "AQT42")]
    [InlineData("Q2", "AJ43", "", "AQJ32")]
    [InlineData("JT2", "K43", "", "KT32")]
    [InlineData("J32", "K54", "", "KJ42")]
    [InlineData("32", "K4", "", "2")]
    [InlineData("AJ92", "K843", "", "AKJ9832")]
    public void TestGetAvailableCardsForks(string cardsNorth, string cardsSouth, string cardsEastWest, string expected)
    {
        // Arrange
        var cardsNorthArray = Utils.StringToCardArray(cardsNorth);
        var cardsSouthArray = Utils.StringToCardArray(cardsSouth);
        var cardsEWArray = cardsEastWest == ""
            ? Enum.GetValues<Face>().Except([Face.Dummy, Face.SmallCard]).Except(cardsNorthArray).Except(cardsSouthArray).ToArray()
            : Utils.StringToCardArray(cardsEastWest);
        // Act
        var availableCardsForks = MiniMax.GetAvailableCardsForks(cardsNorthArray, cardsSouthArray, cardsEWArray);
        var actual = Utils.CardsToString(availableCardsForks.OrderDescending());
        // Assert
        Assert.Equal(expected, actual);
    }    
}