namespace Calculator;

public static class Combinations
{
    public static List<IEnumerable<T>> AllCombinations<T>(IEnumerable<T> elements)
    {
        List<IEnumerable<T>> ret = [];
        for (var k = 0; k <= elements.Count(); k++)
        {
            ret.AddRange(k == 0 ? new[] { Array.Empty<T>() } : Combinations(elements, k));
        }

        return ret;

        static IEnumerable<IEnumerable<TU>> Combinations<TU>(IEnumerable<TU> elements, int k)
        {
            return k == 0
                ? [Array.Empty<TU>()]
                : elements.SelectMany((e, index) =>
                    Combinations(elements.Skip(index + 1), k - 1).Select(c => new[] { e }.Concat(c)));
        }
    }   
    
}