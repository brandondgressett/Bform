namespace BFormDomain.HelperClasses;

public static class EmptyIfNullExtension
{
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source)
    {
        if (source is null)
            return Enumerable.Empty<T>();

        return source;
    }
}
