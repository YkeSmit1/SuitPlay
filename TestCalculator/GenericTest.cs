using Calculator;
using Common;
using Xunit.Abstractions;

namespace TestCalculator;

public class GenericTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public GenericTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }
    
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void TestGetDistributionProbability(int a)
    {
        for (var i = 0; i <= a; i++)
        {
            testOutputHelper.WriteLine($"{a - i}-{i} {Utils.GetDistributionProbability(a - i, i) * 100:F2}");
        }
        
    }
}