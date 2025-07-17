using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Tables.RuleActions;


public class RuleActionInsertTableData : IRuleActionEvaluator
{
    private readonly TableLogic _logic;
    private readonly IApplicationAlert _alerts;
    private readonly ILogger<RuleActionInsertTableData> _logger;

    public RuleActionInsertTableData(
        IApplicationAlert alerts,
        TableLogic logic,
        ILogger<RuleActionInsertTableData> logger)
        
    {
        _alerts = alerts;
        _logic = logic;
        _logger = logger;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionInsertTableData));

    

    public class Arguments
    {
        public string TableTemplate { get; set; } = null!;

        public List<Mapping> Map { get; set; } = new();

        public List<string> Tags { get; set; } = new();
        public string? QueryTags { get; set; }

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
        using (PerfTrack.Stopwatch(nameof(RuleActionInsertTableData)))
        {
            try
            {
                args.Requires().IsNotNull();
                string resultProperty = "CreatedForm";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;

                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                
                var tags = RuleUtil.MaybeLoadArrayProp<string>(eventData, inputs.QueryTags, inputs.Tags);

                var origin = sourceEvent.ToPreceding(Name);

                _logger.LogInformation("{eventJson}", eventData);

                var id = await _logic.EventMapInsertTableRow(
                    origin,
                    inputs.TableTemplate, 
                    BuiltIn.SystemWorkSet, 
                    BuiltIn.SystemWorkItem,
                    eventData, 
                    inputs.Map, 
                    tags,
                    sealEvents, 
                    eventTags, 
                    trx);

                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultProperty, id);

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }




}
