namespace BFormDomain.HelperClasses;

public static class GoodSeedRandom
{
    public static Random Create()
    {
        return
            new Random(Hash.GetCombinedHashCodeForHashes(Guid.NewGuid().GetHashCode(), Environment.TickCount,
                                                         (int) Environment.WorkingSet));
    }

    public static string RandomString(int len)
    {
        var random = Create();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, len).Select(it => it[random.Next(it.Length)]).ToArray());
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
    {
        T[] elements = source.ToArray();
        for (int i = elements.Length - 1; i >= 0; i--)
        {
            // Swap element "i" with a random earlier element it (or itself)
            // ... except we don't really need to swap it fully, as we can
            // return it immediately, and afterwards it's irrelevant.
            int swapIndex = rng.Next(i + 1);
            yield return elements[swapIndex];
            elements[swapIndex] = elements[i];
        }
    }


    public static T RandomPick<T>(this IEnumerable<T> source, Random rng)
    {
        if (source.Count() == 1)
            return source.First();

        var elem = rng.Next(0, source.Count() - 1);
        return source.ElementAt(elem);
    }

    public static T RandomPick<T>(this IEnumerable<T> source, Random rng, int count)
    {
        if (count == 1)
            return source.First();

        var elem = rng.Next(0, count - 1);
        return source.ElementAt(elem);
    }

    public static T RandomPickEnum<T>(Random rng)
    {
        var possibles = Enum.GetValues(typeof (T));
        var pick = rng.Next(possibles.GetLength(0) - 1);
        return (T) possibles.GetValue(pick)!;
    }
}
