using BFormDomain.CommonCode.Utility;
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
public class JsonWinnowerAppender: IEventAppender
{
    private readonly IApplicationAlert _alerts;


    public JsonWinnowerAppender(IApplicationAlert alerts)
    {
        _alerts = alerts;
    }

    public string Name => EventAppenderUtility.FixName(nameof(JsonWinnowerAppender));

    public Task AddToAppendix(
        string? resultName,
        JObject eventData,
        JObject? appendArguments)
    {
        try
        {
            appendArguments.Requires().IsNotNull();
            var args = appendArguments!.ToObject<JsonWinnower>()!;
            args.Requires().IsNotNull();

            args.WinnowData(eventData); // this object already adds to the appendix on its own

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
