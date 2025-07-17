using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Tables.RuleActions
{
    public class RuleActionEditTableRow : IRuleActionEvaluator
    {
        private readonly TableLogic _logic;
        private readonly IApplicationAlert _alerts;
        private readonly ILogger<RuleActionEditTableRow> _logger;

        public RuleActionEditTableRow(
            IApplicationAlert alerts,
            TableLogic logic,
            ILogger<RuleActionEditTableRow> logger)
        {
            _alerts = alerts;
            _logic = logic;
            _logger = logger;
        }

        public string Name => RuleUtil.FixActionName(nameof(RuleActionEditTableRow));

        public class Arguments
        {
            public Guid Id { get; set; }

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
            using (PerfTrack.Stopwatch(nameof(RuleActionEditTableRow)))
            {
                try
                {
                    args.Requires().IsNotNull();

                    string resultProperty = "EditTable";
                    if (!string.IsNullOrEmpty(result))
                        resultProperty = result;

                    var inputs = args!.ToObject<Arguments>()!;

                    var tags = RuleUtil.MaybeLoadArrayProp<string>(eventData, inputs.QueryTags, inputs.Tags);

                    var origin = sourceEvent.ToPreceding(Name);

                    _logger.LogInformation("{eventJson}", eventData);

                    await _logic.EventMapEditTableRow(
                        origin,
                        inputs.Id,
                        inputs.TableTemplate,
                        BuiltIn.SystemWorkSet,
                        BuiltIn.SystemWorkItem,
                        eventData,
                        inputs.Map);

                    var appendix = RuleUtil.GetAppendix(eventData);
                    appendix.Add(resultProperty);
                }
                catch (Exception ex)
                {
                    _alerts.RaiseAlert(ApplicationAlertKind.General,
                        LogLevel.Information, ex.TraceInformation());
                }
            }

        }
    }
}
