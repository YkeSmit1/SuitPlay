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
    public void TestFilterCombinations(string[] combination, string cardsNS, string[] expected)
    {
        var actual = combination.Select(x => x.Select(Utils.CharToCard)).ToList();
        actual.RemoveAll(faces => Calculate.SimilarCombinationsCount(actual, faces.ToList(), cardsNS.Select(Utils.CharToCard)) > 0);
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
}