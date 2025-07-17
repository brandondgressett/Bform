using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Utility;

/// <summary>
/// Newtonsoft doesn't support the aggregate functions of JsonPath!
/// So we'll manually add that functionality into the winnower.
/// Use with substitution variables and appendix to put the results
/// to use in subsequent winnows.
/// </summary>
public enum JArraySummarize
{
    // must be a list of tokens, each of a single numeric value convertible to double
    Min, 
    Max, 
    Sum, 
    Mean, 
    Median, 
    
    // any list of tokens
    Count 
}

public record class JsonPathWinnow(
    string jsonPath,                // path to find, including {substitution} variables
    bool errorIfNoMatch = false,    // throw an exception if none found
    [JsonConverter(typeof(StringEnumConverter))] JArraySummarize? summarize = null, // summarize multiple tokens  
    string? asSub=null,             // save into substitution variable with this name
    string? appendTo=null);         // append to as this property name to appendix, subsequent steps can reference it there
    

/// <summary>
/// Run multiple jsonpath queries to manipulate data.
/// Example: from a table, find the max value on a numeric column,
/// then in the second step, find the row containing that max value.
/// </summary>
public class JsonWinnower
{
    /// <summary>
    /// steps to run, in order.
    /// </summary>
    [JsonProperty]
    public List<JsonPathWinnow> Winnows { get; set; } = new();

    /// <summary>
    /// name of object property to add for appended data
    /// </summary>
    [JsonProperty]
    public string AppendixProperty { get; set; } = "Appendix";
    
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty]
    public string InitializationProperty { get; set; } = "Init";

    // TODO: Consider applying these at each step
    [JsonProperty]
    public string? OrderBy { get; set; }

    public bool Descending { get; set; }

    public int Take { get; set; }

    /// <summary>
    /// Tokens found from the last winnow step.
    /// </summary>
    [JsonIgnore]
    public List<JToken> Final { get; private set; } = Enumerable.Empty<JToken>().ToList();

    /// <summary>
    /// Anything provided here is initially added to the appendix property
    /// as the property "Init", which may be referenced by 
    /// queries for constant data values.
    /// </summary>
    [JsonProperty]
    public JObject? Initialization { get; set; } = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="winnow"></param>
    /// <returns></returns>
    public JsonWinnower Plan(JsonPathWinnow winnow)
    {
        Winnows.Add(winnow);
        return this;
    }


    /// <summary>
    /// Execute winnowing on the given source data. Will fill "Final" property.
    /// </summary>
    /// <param name="sourceData"></param>
    public void WinnowData(JObject sourceData)
    {
        if (Initialization is not null)
        {
            var appendix = GetAppendix(sourceData);
            appendix.Add(InitializationProperty, Initialization);
        }

        if (!Winnows.Any())
            return;
        
        Dictionary<string, string> substitutions = new();

        var lastIndex = Winnows.Count - 1;
        foreach(var (winnow,index) in Winnows.WithIndex())
        {
            var readyPath = winnow.jsonPath;
            
            if(readyPath.Contains("{")) // any substitutions in this path?
            {
                foreach(var sub in substitutions.Keys)
                    if(readyPath.Contains(sub))
                        readyPath = readyPath.Replace(sub, substitutions[sub]);
            }

            var foundTokens = sourceData.SelectTokens(readyPath, winnow.errorIfNoMatch);
            if(foundTokens.Any())
            {
                if (winnow.summarize is null)
                {
                    if (!string.IsNullOrWhiteSpace(winnow.asSub))
                    {
                        // only the first found token will be used,
                        // because substitutions should be single values.
                        var oneToken = foundTokens.First();
                        var subKey = "{" + winnow.asSub + "}";
                        var subValue = oneToken.Value<string>()!;
                        subValue.Guarantees().IsNotNull();
                        substitutions[subKey] = subValue;
                    }

                    if (!string.IsNullOrWhiteSpace(winnow.appendTo))
                    {
                        JObject appendix = GetAppendix(sourceData);
                        if (foundTokens.Count() == 1)
                            appendix.Add(winnow.appendTo, foundTokens.First());
                        else
                            appendix.Add(winnow.appendTo, new JArray(foundTokens.ToArray()));
                    }

                    if (index == lastIndex)
                    {
                        Final = foundTokens.ToList();
                        if(!string.IsNullOrWhiteSpace(OrderBy))
                        {
                            if(Descending)
                                Final = Final.OrderByDescending(jt => jt[OrderBy]).ToList();
                            else
                                Final = Final.OrderBy(jt => jt[OrderBy]).ToList();
                        }

                        if(Take > 0)
                            Final = Final.Take(Take).ToList();
                    }
                } else // summarize the data. 
                {
                    var method = winnow.summarize!;
                    double result = 0.0;

                    var inputValues = new List<double>();
                    if(method != JArraySummarize.Count)
                        inputValues = foundTokens.Select(token => token.Value<double>()).ToList();
                    
                    switch(method)
                    {
                        case JArraySummarize.Min: result = inputValues.Min(); break;
                        case JArraySummarize.Max: result = inputValues.Max(); break;
                        case JArraySummarize.Sum: result = inputValues.Sum(); break;
                        case JArraySummarize.Mean: result = inputValues.Average(); break;
                        case JArraySummarize.Median: result = inputValues.OrderBy(v => v).ElementAt(inputValues.Count / 2); break;
                        case JArraySummarize.Count: result = foundTokens.Count(); break;
                    }
                                       

                    if (!string.IsNullOrWhiteSpace(winnow.asSub))
                    {
                        var subKey = "{" + winnow.asSub + "}";
                        var subValue = result;
                        substitutions[subKey] = subValue.ToString("F4");
                    }

                    if (!string.IsNullOrWhiteSpace(winnow.appendTo))
                    {
                        JObject appendix = GetAppendix(sourceData);
                        appendix.Add(winnow.appendTo, result);
                    }

                    if (index == lastIndex)
                    {
                        Final.Clear();
                        Final.Add(result);
                    }
                }
            }

            
            

        }

        JObject GetAppendix(JObject sourceData)
        {
            if (!sourceData.ContainsKey(AppendixProperty))
                sourceData.Add(AppendixProperty, new JObject());

            var appendix = (JObject)sourceData[AppendixProperty]!;
            return appendix;
        }
    }


}
