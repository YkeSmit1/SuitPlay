using System.Runtime.CompilerServices;
using Calculator;
using Calculator.Models;
using Xunit.Abstractions;

namespace TestCalculator;

public class CreateLinesTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public CreateLinesTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void CreateLines_EmptyInput_ReturnsEmptyList()
    {
        var result = CreateLines(new List<Cards>());
        
        Assert.Empty(result);
    }

    [Fact]
    public void CreateLines_SingleCard_ReturnsSingleLine()
    {
        var card = new Cards([Face.Ace, Face.King]);
        var input = new List<Cards> { card };
        var result = CreateLines(input);
        
        Assert.Single(result);
        Assert.Single(result[0]);
        Assert.Equal(card, result[0][0]);
    }

    [Fact]
    public void CreateLines_TwoCardsDifferentLines_ReturnsTwoSeparateLines()
    {
        var card1 = new Cards([Face.Ace, Face.King]);
        var card2 = new Cards([Face.Queen, Face.Jack]); // Different cards
        var input = new List<Cards> { card1, card2 };
        var result = CreateLines(input);
        
        Assert.Equal(2, result.Count);
        Assert.All(result, line => Assert.Single(line));
        Assert.Contains(result, line => line.Contains(card1));
        Assert.Contains(result, line => line.Contains(card2));
    }

    [Fact(Skip = "Fix this")]
    public void CreateLines_MultipleCardsWithComplexLineRelationships_CorrectlyGroups()
    {
        var card1 = new Cards([Face.Ace, Face.King, Face.Queen]);
        var card2 = new Cards([Face.Ace, Face.King, Face.Jack]);
        var card3 = new Cards([Face.Ace, Face.Queen, Face.Jack]);
        var card4 = new Cards([Face.King, Face.Queen, Face.Jack]);
        
        var input = new List<Cards> { card1, card2, card3, card4 };
        var result = CreateLines(input);
        
        // Debug output to understand the actual result
        testOutputHelper.WriteLine($"Total lines: {result.Count}");
        for (var i = 0; i < result.Count; i++)
        {
            testOutputHelper.WriteLine($"Line {i + 1} ({result[i].Count} cards): {string.Join(", ", result[i].Select(c => string.Join("-", c.Data)))}");
        }
        
        // With the current algorithm, let's see what actually happens:
        // Process card1: creates line1 = [card1]
        // Process card2: checks if card2 belongs to any existing line (line1)
        //   - card1.IsSameLine(card2) = false (2 matching pairs, even)
        //   - creates line2 = [card2]
        // Process card3: checks if card3 belongs to any existing lines
        //   - card1.IsSameLine(card3) = true (1 matching pair, odd) -> belongs to line1
        //   - card2.IsSameLine(card3) = false (1 matching pair, but wait - let's check:
        //     card2: [Ace, King, Jack], card3: [Ace, Queen, Jack]
        //     First pair: Ace vs Ace (match, count=1)
        //     Second pair: King vs Queen (mismatch, stop)
        //     count=1 (odd) -> true! So card3 belongs to BOTH line1 and line2
        //   - So card3 gets added to both line1 and line2
        
        // Process card4: checks if card4 belongs to any existing lines
        //   - card1.IsSameLine(card4) = false (0 matches)
        //   - card2.IsSameLine(card4) = false (0 matches)  
        //   - card3.IsSameLine(card4) = false (0 matches)
        //   - creates line3 = [card4]
        
        // Expected result:
        // Line 1: [card1, card3]
        // Line 2: [card2, card3]  (card3 is in both lines!)
        // Line 3: [card4]
        
        Assert.Equal(3, result.Count);
        
        // Find the lines
        var lineWithCard1 = result.First(line => line.Contains(card1));
        var lineWithCard2 = result.First(line => line.Contains(card2));
        var lineWithCard3 = result.Where(line => line.Contains(card3)).ToList();
        var lineWithCard4 = result.First(line => line.Contains(card4));
        
        // card3 should be in TWO lines (both line1 and line2)
        Assert.Equal(2, lineWithCard3.Count);
        
        // card1 should only be in one line with card3
        Assert.Equal(2, lineWithCard1.Count);
        Assert.Contains(card1, lineWithCard1);
        Assert.Contains(card3, lineWithCard1);
        
        // card2 should only be in one line with card3  
        Assert.Equal(2, lineWithCard2.Count);
        Assert.Contains(card2, lineWithCard2);
        Assert.Contains(card3, lineWithCard2);
        
        // card4 should be alone
        Assert.Single(lineWithCard4);
        Assert.Contains(card4, lineWithCard4);
        
        // Verify card3 appears exactly twice total (in two different lines)
        var allCard3Occurrences = result.SelectMany(line => line).Count(card => card == card3);
        Assert.Equal(2, allCard3Occurrences);
    }

    [Fact(Skip = "Fix this")]
    public void CreateLines_CardBelongsToThreeDifferentLines_AddedToAllThree()
    {
        // Create a card that belongs to three different existing lines
        var cardA = new Cards([Face.Ace, Face.King, Face.Queen]);
        var cardB = new Cards([Face.Ace, Face.King, Face.Jack]);     // Same first 2 as cardA
        var cardC = new Cards([Face.Ace, Face.Queen, Face.Jack]);    // Same first 1 as cardA, cardB
        var cardD = new Cards([Face.King, Face.Queen, Face.Jack]);    // Different from all
        var cardX = new Cards([Face.Ace, Face.Ten, Face.Nine]);      // Will match all Ace-starting cards
    
        var input = new List<Cards> { cardA, cardB, cardC, cardD, cardX };
        var result = CreateLines(input);

        testOutputHelper.WriteLine($"Total lines: {result.Count}");
        for (var i = 0; i < result.Count; i++)
        {
            testOutputHelper.WriteLine($"Line {i + 1} ({result[i].Count} cards): {string.Join(", ", result[i].Select(c => string.Join("-", c.Data)))}");
        }
    
        // cardX should be in the lines containing cardA, cardB, and cardC (all start with Ace)
        var linesWithCardX = result.Where(line => line.Contains(cardX)).ToList();
        Assert.Equal(2, linesWithCardX.Count);
    
        // Verify cardX appears in all three Ace lines
        Assert.Contains(linesWithCardX, line => line.Contains(cardA));
        Assert.Contains(linesWithCardX, line => line.Contains(cardB));
        Assert.Contains(linesWithCardX, line => line.Contains(cardC));
    
        // cardD should be alone
        var lineWithCardD = result.First(line => line.Contains(cardD));
        Assert.Single(lineWithCardD);
    }

    [Fact]
    public void CreateLines_NoCardsShareLines_AllCardsInSeparateLines()
    {
        // All cards are completely different - no IsSameLine relationships
        var card1 = new Cards([Face.Ace, Face.King]);
        var card2 = new Cards([Face.Queen, Face.Jack]);
        var card3 = new Cards([Face.Ten, Face.Nine]);
        var card4 = new Cards([Face.Eight, Face.Seven]);
    
        var input = new List<Cards> { card1, card2, card3, card4 };
        var result = CreateLines(input);

        testOutputHelper.WriteLine($"Total lines: {result.Count}");
        for (var i = 0; i < result.Count; i++)
        {
            testOutputHelper.WriteLine($"Line {i + 1} ({result[i].Count} cards): {string.Join(", ", result[i].Select(c => string.Join("-", c.Data)))}");
        }
    
        // All cards should be in separate lines
        Assert.Equal(4, result.Count);
        Assert.All(result, line => Assert.Single(line));
    
        // Verify all cards are present
        var allCards = result.SelectMany(line => line).ToList();
        Assert.Contains(card1, allCards);
        Assert.Contains(card2, allCards);
        Assert.Contains(card3, allCards);
        Assert.Contains(card4, allCards);
    }

    [Fact]
    public void CreateLines_OrderShouldNotMatter()
    {
        var card1 = new Cards(Utils.StringToCardArray("2xQ").ToList());
        var card2 = new Cards(Utils.StringToCardArray("2xA").ToList());
        var card3 = new Cards(Utils.StringToCardArray("2KA").ToList());
        
        var input = new List<Cards> { card1, card2, card3};
        var result = CreateLines(input);
        
        var result2 = CreateLines(input.AsEnumerable().Reverse());
        Assert.Equal(result.Count, result2.Count);
        Assert.Equal(result.OrderBy(x => x.Count).Select(x => x.Count), result2.OrderBy(x => x.Count).Select(x => x.Count));
    }

    
    [Fact(Skip = "Fix this")]
    public void CreateLines_ShouldProcessAllLinesTwoOptions()
    {
        var card1 = new Cards([Face.Two, Face.Dummy, Face.Queen]);
        var card2 = new Cards([Face.Two, Face.Dummy, Face.Ace]);
        var card3 = new Cards([Face.Two, Face.King, Face.Ace]);
        var card4 = new Cards([Face.Two, Face.King, Face.Queen]);
        
        var input = new List<Cards> { card1, card2, card3, card4};
        var result = CreateLines(input);
        
        var result2 = CreateLines(input.AsEnumerable().Reverse());
        Assert.All(result, x => Assert.Equal(2, x.Count));
        Assert.All(result2, x => Assert.Equal(2, x.Count));
        Assert.Equal(result.Count, result2.Count);
        Assert.Equal(4, result.Count);
    }
    
    [Fact(Skip = "Fix this")]
    public void CreateLines_ShouldProcessAllLinesThreeOptions()
    {
        var card1 = new Cards([Face.Two, Face.SmallCard, Face.Queen]);
        var card2 = new Cards([Face.Two, Face.SmallCard, Face.Ace]);
        var card3 = new Cards([Face.Two, Face.King, Face.Ace]);
        var card4 = new Cards([Face.Two, Face.King, Face.Queen]);
        var card5 = new Cards([Face.Two, Face.Jack, Face.Ace]);
        var card6 = new Cards([Face.Two, Face.Jack, Face.Queen]);
        
        var input = new List<Cards> { card1, card2, card3, card4, card5, card6};
        var result = CreateLines(input);
        
        var result2 = CreateLines(input.AsEnumerable().Reverse());
        Assert.All(result, x => Assert.Equal(3, x.Count));
        Assert.All(result2, x => Assert.Equal(3, x.Count));
        Assert.Equal(result.Count, result2.Count);
        Assert.Equal(8, result.Count);
    }

    public static TheoryData<List<List<Cards>>, List<Cards>> ListTestData()
    {
        return new TheoryData<List<List<Cards>>, List<Cards>>
        {
            // Two elements, Differ in 2nd pos 
            { [[new Cards("2x"), new Cards("2J")]], 
                [new Cards("2x"), new Cards("2J")] },
            // Two elements, Differ in 3rd pos 
            { [[new Cards("2x8")], 
                    [new Cards("2xQ")]], 
                [new Cards("2x8"), new Cards("2xQ")] },
            // Two elements, Differ in 4th pos
            { [[new Cards("234x"), new Cards("234J")]], 
                [new Cards("234x"), new Cards("234J")] },
            // Two elements, Differ in 5th pos
            { [[new Cards("234x8")], 
                    [new Cards("234xQ")]], 
            [new Cards("234x8"), new Cards("234xQ")] },
            // Three elements, Differ in 2nd pos
            { [[new Cards("2x"), new Cards("2J"), new Cards("2K")]], 
                [new Cards("2x"), new Cards("2J"), new Cards("2K")] },
            // Three elements, Differ in 3rd pos
            { [[new Cards("2x8")], 
                    [new Cards("2xQ")], 
                    [new Cards("2xA")]], 
                [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA")] },
            // Four elements, Differ in 2nd and 3rd pos
            { [[new Cards("2xQ"), new Cards("2J8"), new Cards("2JQ")],
                    [new Cards("2x8"), new Cards("2J8"), new Cards("2JQ")],
                    [new Cards("2x8"), new Cards("2xQ"), new Cards("2JQ")],
                    [new Cards("2x8"), new Cards("2xQ"), new Cards("2J8")]],
                [new Cards("2x8"), new Cards("2xQ"), new Cards("2J8"), new Cards("2JQ")] },
            // Six elements, Differ in 2nd and 3rd pos
            { [[new Cards("2xQ"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA")],
                    [new Cards("2x8"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA")],
                    [new Cards("2xA"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA")],
                    [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2JQ")],
                    [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2J8")],
                    [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2JA")]],
                [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA")] },
            // Nine elements, Differ in 2nd and 3rd pos
            { [[new Cards("2xQ"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA"), new Cards("2K8"), new Cards("2KQ"), new Cards("2KA")],
            [new Cards("2x8"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA"), new Cards("2K8"), new Cards("2KQ"), new Cards("2KA")],
            [new Cards("2xA"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA"), new Cards("2K8"), new Cards("2KQ"), new Cards("2KA")],
            [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2JQ"), new Cards("2K8"), new Cards("2KQ"), new Cards("2KA")],
            [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2J8"), new Cards("2K8"), new Cards("2KQ"), new Cards("2KA")],
            [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2JA"), new Cards("2K8"), new Cards("2KQ"), new Cards("2KA")],
            [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA"), new Cards("2K8")],
            [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA"), new Cards("2KQ")],
            [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA"), new Cards("2KA")]],
            [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA"), new Cards("2K8"), new Cards("2KQ"), new Cards("2KA")] },
            // Four elements, Differ in 2nd and 3rd pos with same extra
            { [[new Cards("2xQxA"), new Cards("2J8xA"), new Cards("2JQxA")],
            [new Cards("2x8xA"), new Cards("2J8xA"), new Cards("2JQxA")],
            [new Cards("2x8xA"), new Cards("2xQxA"), new Cards("2JQxA")],
            [new Cards("2x8xA"), new Cards("2xQxA"), new Cards("2J8xA")]],
            [new Cards("2x8xA"), new Cards("2xQxA"), new Cards("2J8xA"), new Cards("2JQxA")] },
            // Four elements, Differ in 2nd and 3rd pos with same extra but extras are different
            { [[new Cards("2xQx8"), new Cards("2J8xQ"), new Cards("2JQx8")],
                    [new Cards("2x8xQ"), new Cards("2J8xQ"), new Cards("2JQx8")],
                    [new Cards("2x8xQ"), new Cards("2xQx8"), new Cards("2JQx8")],
                    [new Cards("2x8xQ"), new Cards("2xQx8"), new Cards("2J8xQ")]],
                [new Cards("2x8xQ"), new Cards("2xQx8"), new Cards("2J8xQ"), new Cards("2JQx8")] },
        };
    }
    
    public static TheoryData<List<List<Cards>>, List<Cards>> ListTestData2()
    {
        return new TheoryData<List<List<Cards>>, List<Cards>>
        {
            // Four elements, Differ in 2nd and 3rd pos
            { [[new Cards("2xQ"), new Cards("2J8"), new Cards("2JQ")],
                    [new Cards("2x8"), new Cards("2J8"), new Cards("2JQ")],
                    [new Cards("2x8"), new Cards("2xQ"), new Cards("2JQ")],
                    [new Cards("2x8"), new Cards("2xQ"), new Cards("2J8")]],
                [new Cards("2x8"), new Cards("2xQ"), new Cards("2J8"), new Cards("2JQ")] }
        };
    }
    
    public static TheoryData<List<List<Cards>>, List<Cards>> ListTestData3()
    {
        return new TheoryData<List<List<Cards>>, List<Cards>>
        {
            // Two elements, Differ in 2nd pos 
            { [[new Cards("2x"), new Cards("2J")]], 
                [new Cards("2x"), new Cards("2J")] },
        };
    }
    
    public static TheoryData<List<List<Cards>>, List<Cards>> ListTestData4()
    {
        return new TheoryData<List<List<Cards>>, List<Cards>>
        {
            // Six elements, Differ in 2nd and 3rd pos
            { [[new Cards("2xQ"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA")],
                    [new Cards("2x8"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA")],
                    [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2JQ")],
                    [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2J8")]],
                [new Cards("2x8"), new Cards("2xQ"), new Cards("2xA"), new Cards("2J8"), new Cards("2JQ"), new Cards("2JA")] }
        };
    }
    
    public static TheoryData<List<List<Cards>>, List<Cards>> ListTestData5()
    {
        return new TheoryData<List<List<Cards>>, List<Cards>>
        {
            // Four elements, Differ in 2nd and 3rd pos with same extra
            { [[new Cards("2xQxA"), new Cards("2J8xA"), new Cards("2JQxA")],
                    [new Cards("2x8xA"), new Cards("2J8xA"), new Cards("2JQxA")],
                    [new Cards("2x8xA"), new Cards("2xQxA"), new Cards("2JQxA")],
                    [new Cards("2x8xA"), new Cards("2xQxA"), new Cards("2J8xA")]],
                [new Cards("2x8xA"), new Cards("2xQxA"), new Cards("2J8xA"), new Cards("2JQxA")] },
        };
    }
    
    
    

    [Theory]
    [MemberData(nameof(ListTestData))]
    public void TestAll(List<List<Cards>> expected , List<Cards> cardsList)
    {
        AssertResult(expected, cardsList);
    }

    private static void AssertResult(List<List<Cards>> expected, List<Cards> cardsList)
    {
        var result = CreateLines(cardsList);
        Assert.Equal(expected.Count, result.Count);
        foreach (var list in expected)
        {
            Assert.Contains(result, x => x.SequenceEqual(list));
        }
    }
    
    // { [[new Cards("2x8"), new Cards("2J8"), new Cards("2JQ")], 
    //     [new Cards("2xQ"), new Cards("2J8"), new Cards("2JQ")],
    //     [new Cards("2J8"), new Cards("2x8"), new Cards("2xQ")],
    //     [new Cards("2JQ"), new Cards("2x8"), new Cards("2xQ")]],
    //     [new Cards("2x8"), new Cards("2xQ"), new Cards("2J8"), new Cards("2JQ")] }
    private static List<List<Cards>> CreateLines(IEnumerable<Cards> cardList)
    {
        var result = GetLines(cardList.ToList(), 0);
        
        return result.ToList();
        
        static IEnumerable<List<Cards>> GetLines(List<Cards> cardList, int depth)
        {
            if (depth % 2 == 1)
            {
                var groupBy = cardList.GroupBy(x => x.Data[depth]).ToList();
                if (groupBy.Count > 1 && groupBy.Any(x => x.Count() > 1))
                {
                    foreach (var group in cardList.GroupBy(x => x.Data[depth]))
                    {
                        foreach (var card in group)
                        {
                            yield return cardList.Except(group.Except([card])).ToList();
                        }
                    }
                }
                else
                {
                    foreach (var lines in GenerateLines(cardList, depth)) 
                        yield return lines;
                    
                }
            }
            else
            {
                foreach (var group in cardList.GroupBy(x => x.Data[depth]))
                {
                    foreach (var lines in GenerateLines(group.Select(x => x).ToList(), depth)) 
                        yield return lines;
                }
            }

            yield break;

            static IEnumerable<List<Cards>> GenerateLines(List<Cards> cardList, int depth)
            {
                if (cardList.Any(x => x.Count() <= depth + 1))
                    yield return cardList;
                else
                    foreach (var line in GetLines(cardList, depth + 1))
                        yield return line;
            }
        }
    }
}