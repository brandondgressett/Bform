using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;


namespace BFormDomain.CommonCode.Platform.Rules.RuleActions;

public class RuleActionCustomEvent : IRuleActionEvaluator
{
    private readonly AppEventSink _sink;
    private readonly IApplicationAlert _alerts;

    public RuleActionCustomEvent(
        AppEventSink sink,
        IApplicationAlert alerts)
    {
        _sink = sink;
        _alerts = alerts;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionCustomEvent));

    public class Arguments
    {
        public JsonWinnower? DataWinnower { get; set; }

        [JsonRequired]
        public string CustomEventTopic { get; set; } = null!;

        public List<string> EventTags { get; set; } = new();

        public bool IncludeOriginalPayload { get; set; }    
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
        using(PerfTrack.Stopwatch(nameof(RuleActionCustomEvent)))
        {
            try
            {
                string resultProperty = "WinnowedData";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;

                args.Requires().IsNotNull();
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var origin = sourceEvent.ToPreceding(Name);
                var eventInfo = eventData.ToObject<AppEventRuleView>()!;
                eventInfo.Guarantees().IsNotNull();

                var data = eventData;
                if (inputs.DataWinnower is not null)
                {
                    data = new JObject();
                    inputs.DataWinnower.WinnowData(data);
                    if (inputs.DataWinnower.Final.Count > 1)
                    {
                        data.Add(resultProperty, new JArray(inputs.DataWinnower.Final.ToArray()));
                    }
                    else if(inputs.DataWinnower.Final.Any())
                    {
                        data.Add(resultProperty, inputs.DataWinnower.Final.First());
                    }

                    if (inputs.IncludeOriginalPayload)
                        data.Add(nameof(AppEventRuleView), eventData);
                }


                // var bsData = data.ToBsonObject();

                _sink.BeginBatch(trx);
                await _sink.Enqueue(origin, inputs.CustomEventTopic, null,
                    new EntityWrapping<JObject>
                    {
                        CreatedDate = DateTime.Now,
                        Creator = Constants.BuiltIn.SystemUser,
                        EntityType = nameof(Rule),
                        HostWorkItem = eventInfo.HostWorkItem,
                        HostWorkSet = eventInfo.HostWorkSet,
                        Id = Guid.NewGuid(),
                        Payload = data,
                    }, null, inputs.EventTags, sealEvents);

                await _sink.CommitBatch();
                

            }
            catch(Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }

    }



}
