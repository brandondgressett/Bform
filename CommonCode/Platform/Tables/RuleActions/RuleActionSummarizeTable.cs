using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


namespace BFormDomain.CommonCode.Platform.Tables.RuleActions;

public class RuleActionSummarizeTable : IRuleActionEvaluator
{
    private readonly TableLogic _logic;
    private readonly IApplicationAlert _alerts;

    public RuleActionSummarizeTable(
        IApplicationAlert alerts,
        TableLogic logic)

    {
        _alerts = alerts;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionSummarizeTable));

    public class Arguments
    {
        public RelativeTableQueryCommand Query { get; set; } = null!;
        public TableSummarizationCommand Summarization { get; set; } = null!;  
       
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
        using (PerfTrack.Stopwatch(nameof(RuleActionSummarizeTable)))
        {
            try
            {
                args.Requires().IsNotNull();

                string resultProperty = "CreatedData";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;

                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();
                Guid? wsSpec, wiSpec;
                var q = inputs.Query;
                q.Guarantees().IsNotNull();
                inputs.Summarization.Guarantees().IsNotNull();

                var query = q.BuildQuery(eventData, out wsSpec, out wiSpec);

                TableSummaryViewModel? view = null!;
                
                view = await _logic.QueryDataTableSummary(
                    q.TableTemplate,
                    query,
                    inputs.Summarization,
                    wsSpec, wiSpec);
                
                view.Guarantees().IsNotNull();

                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultProperty, JObject.FromObject(view));

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }


}
