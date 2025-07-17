namespace BFormDomain.HelperClasses;

public static partial class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> that, Action<T> action)
    {
        foreach (var item in that) action(item);
    }

    public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> source)
    {
        foreach (var enumeration in source)
        {
            foreach (var item in enumeration)
            {
                yield return item;
            }
        }
    }

    public static IEnumerable<T> SelectMany<T>(this IEnumerable<T[]> source)
    {
        foreach (var enumeration in source)
        {
            foreach (var item in enumeration)
            {
                yield return item;
            }
        }
    }
}
