using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


namespace BFormDomain.CommonCode.Platform.Tables.RuleActions;

public class RuleActionSelectTableRows : IRuleActionEvaluator
{
    private readonly TableLogic _logic;
    private readonly IApplicationAlert _alerts;

    public RuleActionSelectTableRows(
        IApplicationAlert alerts,
        TableLogic logic)

    {
        _alerts = alerts;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionSelectTableRows));

    public class Arguments
    {
        public RelativeTableQueryCommand Query { get; set; } = null!;
        public bool Paging { get; set; }
        public int Page { get; set; }
        public string? PageQuery { get; set; }
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
        using(PerfTrack.Stopwatch(nameof(RuleActionSelectTableRows)))
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

                var query = q.BuildQuery(eventData,out wsSpec, out wiSpec);

                TableViewModel? view = null!;

                if (inputs.Paging)
                {
                    var page = RuleUtil.MaybeLoadProp(eventData, inputs.PageQuery, inputs.Page);
                    view = await _logic.QueryDataTablePage(q.TableTemplate,
                        query, page, wsSpec, wiSpec);
                }
                else
                {
                    view = await _logic.QueryDataTableAll(
                        q.TableTemplate,
                        query,
                        wsSpec, wiSpec);
                }
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
