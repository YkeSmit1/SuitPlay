using Calculator;
using Calculator.Models;

namespace TestCalculator;

public class CardsTests
{
    [Fact]
    public void Constructor_InitializesDataCorrectly()
    {
        var faces = new List<Face> { Face.Ace, Face.King, Face.Queen };
        var cards = new Cards(faces);
        
        Assert.Equal(faces, cards.Data);
    }

    [Fact]
    public void Equals_WithSameFaces_ReturnsTrue()
    {
        var faces1 = new List<Face> { Face.Ace, Face.King };
        var faces2 = new List<Face> { Face.Ace, Face.King };
        var cards1 = new Cards(faces1);
        var cards2 = new Cards(faces2);
        
        Assert.True(cards1.Equals(cards2));
    }

    [Fact]
    public void Equals_WithDifferentFaces_ReturnsFalse()
    {
        var faces1 = new List<Face> { Face.Ace, Face.King };
        var faces2 = new List<Face> { Face.Ace, Face.Queen };
        var cards1 = new Cards(faces1);
        var cards2 = new Cards(faces2);
        
        Assert.False(cards1.Equals(cards2));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectOrdering()
    {
        var faces1 = new List<Face> { Face.Ace };
        var faces2 = new List<Face> { Face.King };
        var faces3 = new List<Face> { Face.Ace, Face.King };
        
        var cards1 = new Cards(faces1);
        var cards2 = new Cards(faces2);
        var cards3 = new Cards(faces3);
        
        Assert.True(cards1.CompareTo(cards2) > 0);
        Assert.True(cards2.CompareTo(cards1) < 0);
        Assert.Equal(0, cards1.CompareTo(new Cards(faces1)));
        
        // Test with multiple faces - comparison should be based on first differing element
        Assert.True(cards1.CompareTo(cards3) < 0);
    }

    [Fact]
    public void TestSameLine()
    {
        Assert.True(new Cards("Ax").IsSameLine(new Cards("AK")));
        Assert.False(new Cards("2xA").IsSameLine(new Cards("2xQ")));
    }
}