using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules.RuleActions;

public class RuleActionForEach : IRuleActionEvaluator
{
    private RuleEvaluator _ruleEvaluator;
    private readonly IApplicationAlert _alerts;
    private readonly IServiceProvider _serviceProvider;


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public RuleActionForEach(
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        IApplicationAlert alerts,
        IServiceProvider sp)
    {
        _serviceProvider = sp;
        _alerts = alerts;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionForEach));

    public const string SelectedItem = "SelectedItem";
    public const string SelectedIndex = "SelectedIndex";

    public class Arguments
    {
        public string SourceQuery { get; set; } = null!;
        public RuleAction Action { get; set; } = null!; // TODO: make an additional action that runs the rule.
    }

    public async Task Execute(
        ITransactionContext trx, 
        string? result, 
        JObject eventData, 
        JObject? args, 
        AppEvent sourceEvent, 
        bool sealEvents, 
        IEnumerable<string>? eventTags = null)
    {

        _ruleEvaluator = _serviceProvider.GetService<RuleEvaluator>()!;

        using(PerfTrack.Stopwatch(nameof(RuleActionForEach)))
        {
            try
            {
                args.Requires().IsNotNull();
                
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();
                inputs.SourceQuery.Requires().IsNotNullOrEmpty();
                inputs.Action.Requires().IsNotNull();

                // select the elements using the query
                var tokens = eventData.SelectTokens(inputs.SourceQuery);
                var appendix = RuleUtil.GetAppendix(eventData);

                if (tokens.Any())
                {
                    int index = 0;
                    foreach (var token in tokens)
                    {
                        if(!appendix.ContainsKey(SelectedItem))
                            appendix.Add(SelectedItem, token);
                        else
                            appendix[SelectedItem] = token;

                        if (!appendix.ContainsKey(SelectedIndex))
                            appendix.Add(SelectedIndex, index);
                        else
                            appendix[SelectedIndex] = index;

                        await _ruleEvaluator.ExecuteAction(Name, eventTags, eventData, sourceEvent, trx, inputs.Action);

                        index += 1;
                    }
                }

            }
            catch(Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
