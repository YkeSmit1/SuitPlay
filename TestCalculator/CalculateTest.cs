using System.Text.Json;
using Calculator;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace TestCalculator;

[TestSubject(typeof(Calculate))]
public class CalculateTest
{
    [UsedImplicitly] private readonly ITestOutputHelper testOutputHelper;

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
    [InlineData("K74", "AQ32", "K4")]
    [InlineData("KJT43", "AQ92", "KT3")]
    [InlineData("KJT76", "AQ985432", "KT6")]
    [InlineData("KJT762", "AQ98543", "KT62")]
    [InlineData("KJT76", "AQ98543", "KT6")]
    [InlineData("KJT2", "AQ98543", "KT2")]
    [InlineData("6", "AQT985432", "6")]
    [InlineData("KJ7", "AQT985432", "KJ7")]
    [InlineData("AQT98", "KJ76", "AQ8")]
    public void TestFilterAvailableCards(string cardsPlayer, string cardsOtherTeam, string expected)
    {
        // Arrange
        var cardsPlayerList = cardsPlayer.Select(x => new Card {Face = Utils.CharToCard(x)});
        var cardsOtherTeamList = cardsOtherTeam.Select(x => new Card {Face = Utils.CharToCard(x)});
        var expectedList = expected.Select(Utils.CharToCard);
        // Act
        var actual = Calculate.AvailableCardsFiltered(cardsPlayerList.ToList(), cardsOtherTeamList.ToList()).Select(x => x.Face);
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
    [InlineData("QT98", "A543")]
    public void TestEqualToEtalon(string north, string south)
    {
        // Arrange 
        var northHand = Utils.StringToCardList(north);
        var southHand = Utils.StringToCardList(south);
        var cardsNS = northHand.Concat(southHand).OrderByDescending(x => x).ToList();
        // Act
        var bestPlay = Calculate.CalculateBestPlay(northHand, southHand);
        var filteredTrees = bestPlay.ToDictionary(x => x.Key, y => y.Value.Where(x => x.Item1.Count == 3), new ListEqualityComparer<Face>());
        var filename = $"{north}-{south}";
        var result = Calculate.GetResult(filteredTrees, cardsNS, $"{filename}.json");
        // Assert
        var west = result.DistributionList.Select(x => x.West).ToList();
        Assert.Equal(west.Distinct(new ListEqualityComparer<Face>()).ToList(), west);
        
        var east = result.DistributionList.Select(x => x.East).ToList();
        Assert.Equal(east.Distinct(new ListEqualityComparer<Face>()).ToList(), east);
        
        var json = File.ReadAllText($"{filename}.json");
        var etalon = File.ReadAllText(Path.Combine("etalons", $"{filename}.etalon.json"));
        Assert.Equal(etalon, json);
    }
}