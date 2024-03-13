using Calculator;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace TestCalculator;

[TestSubject(typeof(Calculate))]
public class CalculateTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CalculateTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TestCalculate()
    {
        var output = Calculate.CalculateBestPlay("AQ", "98").Take(3).ToList();
        foreach (var line in output)
        {
            _testOutputHelper.WriteLine($"Average:{line.Item2:0.##}");
        }
    }
}