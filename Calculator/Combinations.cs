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
                ? new[] { Array.Empty<TU>() }
                : elements.SelectMany((e, index) =>
                    Combinations(elements.Skip(index + 1), k - 1).Select(c => new[] { e }.Concat(c)));
        }
    }    
    
    public static IEnumerable<List<T>> FilterCombinations<T>(List<List<T>> elements, List<T> cardsNs) where T : Enum
    {
        return elements.Where(element => elements.All(x => !SimilarCombination(element, x, cardsNs))).ToList();

        static bool SimilarCombination(List<T> enumerable, List<T> enumerable1, List<T> cardsNS)
        {
            if (enumerable.Count != enumerable1.Count)
                return false;

            var except = enumerable.Except(enumerable1).ToList();
            var except1 = enumerable1.Except(enumerable).ToList();
            if (except.Count == 0 || except1.Count == 0)
                return false;
            if (except.Last().CompareTo(except1.Last()) < 0)
                return false;

            var nsCardsLower = except.Concat(except1).Select(x => cardsNS.Where(y => y.CompareTo(x) < 0)).ToList();
            var isSimilar = nsCardsLower.All(z => z.SequenceEqual(nsCardsLower.First()));
            return isSimilar;
        }

    }
}