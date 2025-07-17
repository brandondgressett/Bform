using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules;

/// <summary>
/// 
///     -References:
///         >RuleAction files
///     -Functions:
///         >FixActionName
///         >MaybeLoadProp
///         >GetAppendix
/// </summary>
public static class RuleUtil
{
    public static string FixActionName(string className)
    {
        className.Requires().IsNotNullOrEmpty();
        return className.Replace("RuleAction", string.Empty);
    }

    public static T? MaybeLoadProp<T>(JObject eventData, string? query, T? defaultVal)
    {
        var retval = defaultVal;
        if(!string.IsNullOrWhiteSpace(query))
        {
            var prop = eventData.SelectToken(query);
            prop.Guarantees().IsNotNull();
            retval = prop!.Value<T>();
        }

        return retval;
    }

    public static List<T>? MaybeLoadArrayProp<T>(JObject eventData, string? query, List<T>? defaultVal)
    {
        var retval = defaultVal;

        if(!string.IsNullOrWhiteSpace(query))
        {
            var prop = eventData.SelectToken(query);
            prop.Guarantees().IsNotNull();
            var arr = prop!.Value<JArray>();
            retval = prop!.ToObject<List<T>>();
        }

        return retval;
    }

    public const string AppendixProperty = "Appendix";

    public static JObject GetAppendix(JObject eventData)
    {
        if (!eventData.ContainsKey(AppendixProperty))
            eventData.Add(AppendixProperty, new JObject());

        return (JObject)eventData[AppendixProperty]!;
    }

    
}
