using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Tables.RuleActions
{
    public class RuleActionDeleteTableRow : IRuleActionEvaluator
    {
        private readonly TableLogic _logic;
        private readonly IApplicationAlert _alerts;
        private readonly ILogger<RuleActionDeleteTableRow> _logger;

        public RuleActionDeleteTableRow(
            IApplicationAlert alerts,
            TableLogic logic,
            ILogger<RuleActionDeleteTableRow> logger)
        {
            _logger = logger;
            _alerts = alerts;
            _logic = logic;
        }

        public string Name => RuleUtil.FixActionName(nameof(RuleActionDeleteTableRow));

        public class Arguments
        {
            public string TableTemplate { get; set; } = null!;

            public Guid ID { get; set; }

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
            args.Requires().IsNotNull();
            string resultProperty = "CreatedForm";
            if (!string.IsNullOrEmpty(result))
                resultProperty = result;

            var inputs = args!.ToObject<Arguments>();
            inputs.Guarantees().IsNotNull();

            var origin = sourceEvent.ToPreceding(Name);

            _logger.LogInformation("{eventJson}", eventData);

            await _logic.ActionDeleteTableRow(
                "testharness",
                "TestTableTemplate",
                inputs!.ID,
                BuiltIn.SystemUser,
                BuiltIn.SystemWorkSet,
                BuiltIn.SystemWorkItem);

        }
    }
}
