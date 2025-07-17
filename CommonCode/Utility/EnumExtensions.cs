using BFormDomain.Validation;

namespace BFormDomain.HelperClasses;

public static class EnumExtensions
{
    public static string EnumName<T>(this T that)
    {
        that = that ?? throw new ArgumentNullException(nameof(that));

        CheckIsEnum<T>(false);
        string? name = Enum.GetName(that!.GetType(), that);
        name.Guarantees().IsNotNull();

        return name!;
    }

    public static int EnumValue<T>(this T that)
    {
        CheckIsEnum<T>(false);
        return Convert.ToInt32(that);
    }

    public static IEnumerable<string> RenderTokens(IEnumerable<Enum> tokens)
    {
        tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        return tokens.OrderBy(t => t.EnumName()).Select(t => t.EnumName());
    }

    public static string CombinedToken(IEnumerable<Enum> tokens)
    {
        return string.Join("&&", RenderTokens(tokens));
    }

    public static void CheckIsEnum<T>(bool withFlags)
    {
        if (!typeof(T).IsEnum && typeof(T) != typeof(Enum))
            throw new ArgumentException(string.Format("Type '{0}' is not an enum", typeof(T).FullName));

        if (withFlags && !Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute",
                                                      typeof(T).FullName));
    }

    public static bool IsFlagSet<T>(this T value, T flag) where T : struct
    {
        CheckIsEnum<T>(true);
        long lValue = Convert.ToInt64(value);
        long lFlag = Convert.ToInt64(flag);
        return (lValue & lFlag) != 0;
    }

    public static IEnumerable<T> GetFlags<T>(this T value) where T : struct
    {
        CheckIsEnum<T>(true);
        foreach (T flag in Enum.GetValues(typeof(T)).Cast<T>())
        {
            if (value.IsFlagSet(flag))
                yield return flag;
        }
    }

    public static T SetFlags<T>(this T value, T flags, bool on) where T : struct
    {
        CheckIsEnum<T>(true);
        long lValue = Convert.ToInt64(value);
        long lFlag = Convert.ToInt64(flags);
        if (on)
        {
            lValue |= lFlag;
        }
        else
        {
            lValue &= (~lFlag);
        }
        return (T)Enum.ToObject(typeof(T), lValue);
    }

    public static T SetFlags<T>(this T value, T flags) where T : struct
    {
        return value.SetFlags(flags, true);
    }

    public static T ClearFlags<T>(this T value, T flags) where T : struct
    {
        return value.SetFlags(flags, false);
    }

    public static T CombineFlags<T>(this IEnumerable<T> flags) where T : struct
    {
        CheckIsEnum<T>(true);
        long lValue = 0;
        foreach (T flag in flags)
        {
            long lFlag = Convert.ToInt64(flag);
            lValue |= lFlag;
        }
        return (T)Enum.ToObject(typeof(T), lValue);
    }

   
}
