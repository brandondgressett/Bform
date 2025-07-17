using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Entity;
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
public class AddEntityReferenceAppender: IEventAppender
{
    private readonly IApplicationAlert _alerts;
    private readonly KeyInject<string, IEntityReferenceBuilder>.ServiceResolver _refBuilderFactory;

    public AddEntityReferenceAppender(
        IApplicationAlert alerts,
        KeyInject<string, IEntityReferenceBuilder>.ServiceResolver factory)
    {
        _alerts = alerts;
        _refBuilderFactory = factory;
    }

    public string Name => EventAppenderUtility.FixName(nameof(AddEntityReferenceAppender));

    public class Arguments
    {
        public bool IsTemplateReference { get; set; }
        public bool IsVMReference { get; set; }

        public Dictionary<string, string> QueryParameters { get; set; } = new();
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

            bool isTemplateReference = false;
            bool isVMReference = false;
            string? queryParameters = null!;
            if(appendArguments is not null)
            {
                var inputs = appendArguments.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();
                isTemplateReference = inputs.IsTemplateReference;
                if(inputs.QueryParameters.Any())
                {
                    var items = inputs.QueryParameters.Select(kvp => $"{kvp.Key}={kvp}");
                    queryParameters = string.Join('&', items);
                }
            }

            var eventView = eventData.ToObject<AppEventRuleView>()!;
            eventView.Guarantees().IsNotNull();

            var entityType = eventView.EntityType!;
            entityType.Guarantees().IsNotNullOrEmpty();
            eventView.EntityTemplate.Guarantees().IsNotNullOrEmpty();
            eventView.EntityId.HasValue.Guarantees().IsTrue();

            var refBuilder = _refBuilderFactory(entityType);
            string reference = null!;
            var uri = refBuilder.MakeReference(eventView.EntityTemplate!, eventView.EntityId!.Value,
                isTemplateReference, isVMReference, queryParameters);

            var appendix = RuleUtil.GetAppendix(eventData);
            appendix.Add(resultName, reference);

        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General,
                Microsoft.Extensions.Logging.LogLevel.Information,
                ex.TraceInformation(),
                10);
        }


        return Task.CompletedTask;

    }


}
