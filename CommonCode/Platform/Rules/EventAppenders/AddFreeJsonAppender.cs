using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules.EventAppenders;

/// <summary>
/// 
///     -References:
///         >
///     -Functions:
///         >
/// </summary>
public class AddFreeJsonAppender : IEventAppender
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationPlatformContent _content;

    public AddFreeJsonAppender(
        IApplicationPlatformContent content,
        IApplicationAlert alerts)
    {
        _content = content;
        _alerts = alerts;
    }

    public string Name => EventAppenderUtility.FixName(nameof(AddFreeJsonAppender));

    public class Arguments
    {
        public string Name { get; set; } = null!;

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

            var jsonText = _content.GetFreeJson(args.Name)!;
            jsonText.Guarantees().IsNotNullOrEmpty();
            var data = JObject.Parse(jsonText)!;
            data.Guarantees().IsNotNull();
            
            var appendix = RuleUtil.GetAppendix(eventData);
            appendix.Add(resultName, data);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General,
                Microsoft.Extensions.Logging.LogLevel.Information,
                ex.TraceInformation(),
                15);

            throw;
        }
    }
}
