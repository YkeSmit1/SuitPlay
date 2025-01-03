namespace Calculator;

public static class Combinations
{
    public static List<IEnumerable<T>> AllCombinations<T>(IEnumerable<T> elements)
    {
        List<IEnumerable<T>> ret = [];
        var list = elements.ToList();
        for (var k = 0; k <= list.Count; k++)
        {
            ret.AddRange(k == 0 ? [Array.Empty<T>()] : Combinations(list, k));
        }

        return ret;

        static IEnumerable<IEnumerable<TU>> Combinations<TU>(IEnumerable<TU> elements, int k)
        {
            var list = elements.ToList();
            return k == 0 ? [Array.Empty<TU>()] : list.SelectMany((e, index) =>
                    Combinations(list.Skip(index + 1), k - 1).Select(c => new[] { e }.Concat(c)));
        }
    }   
    
}