namespace BFormDomain.HelperClasses;

public static class EnumerableEx
{
    public static T Second<T>(this IEnumerable<T> that)
    {
        return that.Skip(1).First();
    }

    public static T Third<T>(this IEnumerable<T> that)
    {
        return that.Skip(2).First();
    }


    public static IEnumerable<T> OfOne<T>(T item)
    {
        return Enumerable.Repeat(item, 1);
    }


    public static IEnumerable<T> OfTwo<T>(T itemOne, T itemTwo)
    {
        yield return itemOne;
        yield return itemTwo;
    }

    public static IEnumerable<T> OfThree<T>(T itemOne, T itemTwo, T itemThree)
    {
        yield return itemOne;
        yield return itemTwo;
        yield return itemThree;
    }

    public static IEnumerable<T> OfFour<T>(T itemOne, T itemTwo, T itemThree, T itemFour)
    {
        yield return itemOne;
        yield return itemTwo;
        yield return itemThree;
        yield return itemFour;
    }

    public static IEnumerable<T> OfFive<T>(T i1, T i2, T i3, T i4, T i5)
    {
        yield return i1;
        yield return i2;
        yield return i3;
        yield return i4;
        yield return i5;
    }

    public static IEnumerable<T> ConcatMany<T>(params IEnumerable<T>[] many)
    {
        return many.SelectMany(c => c);
    }
}
