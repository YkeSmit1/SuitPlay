namespace Calculator;

public static class Combinations
{
    public static List<IEnumerable<T>> AllCombinations<T>(List<T> elements)
    {
        List<IEnumerable<T>> ret = [];
        for (var k = 0; k <= elements.Count; k++)
        {
            ret.AddRange(k == 0 ? [[]] : Combinations(elements, k));
        }

        return ret;

        static IEnumerable<IEnumerable<TU>> Combinations<TU>(List<TU> elements, int k)
        {
            return k == 0 ? [[]] : elements.SelectMany((e, index) =>
                    Combinations(elements.Skip(index + 1).ToList(), k - 1).Select(c => new[] { e }.Concat(c)));
        }
    }   
    
}