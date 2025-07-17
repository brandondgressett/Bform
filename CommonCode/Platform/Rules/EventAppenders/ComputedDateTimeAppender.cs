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
public class ComputedDateTimeAppender : IEventAppender
{
    private readonly IApplicationAlert _alerts;

    public ComputedDateTimeAppender(IApplicationAlert alerts)
    {
        _alerts = alerts;
    }

    public string Name => EventAppenderUtility.FixName(nameof(ComputedDateTimeAppender));

    public class Arguments
    {
        /// <summary>
        /// Json Path to a token containing the starting time. If not
        /// specified, current date and time are used.
        /// </summary>
        public string? StartQuery { get; set; } = null!;

        public TimeQuery TimeQuery { get; set; } = null!;
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
            var args = appendArguments!.ToObject<Arguments>();
            args.Requires().IsNotNull();

            DateTime startingTime = RuleUtil.MaybeLoadProp(eventData, args!.StartQuery, DateTime.UtcNow);
            
            args.TimeQuery.ExplicitFrom = startingTime;
            var computedTime = args.TimeQuery.Resolve();

            var appendix = RuleUtil.GetAppendix(eventData);
            appendix.Add(resultName, computedTime);

            

        } catch(Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General,
                Microsoft.Extensions.Logging.LogLevel.Information,
                ex.TraceInformation(),
                2);
        }

        return Task.CompletedTask;
    }
}
