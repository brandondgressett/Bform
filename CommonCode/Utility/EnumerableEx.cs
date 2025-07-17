using System.Data;

namespace BFormDomain.HelperClasses;

public static partial class EnumerableExtensions
{
    private static readonly Random _rng = new();

    public static IList<T> ToIList<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.ToList();
    }

    public static int FindIndex<T>(this IEnumerable<T> list, Predicate<T> finder)
    {
        return list.Select((item, index) => new {item, index})
                   .Where(p => finder(p.item))
                   .Select(p => p.index + 1)
                   .FirstOrDefault() - 1;
    }

    public static T RandomSelect<T>(this IEnumerable<T> list)
    {
        var index = _rng.Next()%list.Count();
        return list.ElementAt(index);
    }

    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
    {
        return source.Select((item, index) => (item, index));
    }
}
