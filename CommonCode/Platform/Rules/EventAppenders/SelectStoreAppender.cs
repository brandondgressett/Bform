using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules.EventAppenders;

/// <summary>
/// 
///     -References:
///         >RuleEvaluator.cs
///         >RuleServiceCollectionExtensions.cs
///     -Functions:
///         >AddToAppendix
/// </summary>
public class SelectStoreAppender : IEventAppender
{

    private readonly IApplicationAlert _alerts;
    

    public SelectStoreAppender(IApplicationAlert alerts)
    {
        _alerts = alerts;
    }

    public string Name => EventAppenderUtility.FixName(nameof(SelectStoreAppender));

    public class Arguments
    {
        public string Query { get; set; } = null!;
        
        public bool InArray { get; set; }
    }

    public Task AddToAppendix(
        string? resultName, 
        JObject eventData, 
        JObject? appendArguments)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resultName))
                resultName = Name;
            
            appendArguments.Requires().IsNotNull();
            var args = appendArguments!.ToObject<Arguments>()!;
            args.Requires().IsNotNull();
            args.Query.Requires().IsNotNullOrEmpty();

            JToken? found = null!;
            if(args.InArray)
            {
                var items = eventData.SelectTokens(args.Query);
                if(items.Any())
                    found = new JArray(items);
            } else
            {
                found = eventData.SelectToken(args.Query);
            }

            if (found is not null)
            {
                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultName, found);
            }

        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General,
                Microsoft.Extensions.Logging.LogLevel.Information,
                ex.TraceInformation(),
                2);
        }


        return Task.CompletedTask;
    }
}
