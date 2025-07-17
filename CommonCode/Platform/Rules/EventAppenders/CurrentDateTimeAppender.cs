using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
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
public class CurrentDateTimeAppender : IEventAppender
{
    private readonly IApplicationAlert _alerts;

    public CurrentDateTimeAppender(IApplicationAlert alerts)
    {
        _alerts = alerts;
    }

    public string Name => EventAppenderUtility.FixName(nameof(CurrentDateTimeAppender));

    public Task AddToAppendix(
        string? resultName, 
        JObject eventData, 
        JObject? __)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resultName))
                resultName = Name;

            var now = DateTime.UtcNow;

            var appendix = RuleUtil.GetAppendix(eventData);
            appendix.Add(resultName, now);
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General,
                Microsoft.Extensions.Logging.LogLevel.Information,
                ex.TraceInformation(),
                15);
        }

        return Task.CompletedTask;
    }
}
