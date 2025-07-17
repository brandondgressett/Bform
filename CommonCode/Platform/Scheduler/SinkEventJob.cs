using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;

namespace BFormDomain.CommonCode.Platform.Scheduler;

/// <summary>
/// Scheduled job for sinking a given app event.
///     -References:
///         >QuartzISchedulerLogic.cs
///     -Functions:
///         >Execute
/// </summary>
public class SinkEventJob
{
    private readonly AppEventSink _sink;
    private readonly IApplicationAlert _alerts;
    private readonly IDataEnvironment _env;

    public SinkEventJob(AppEventSink sink, IApplicationAlert alerts, IDataEnvironment env)
    {
        _sink = sink;
        _alerts = alerts;
        _env = env;
    }

    public async Task Execute(JObject jsonObject)
    {
        using (PerfTrack.Stopwatch(nameof(SinkEventJob)))
        {
            try
            {
                var trx = await _env.OpenTransactionAsync(CancellationToken.None);
                _sink.BeginBatch(trx);

                var jobj = JObject.FromObject(jsonObject);
                var json = jobj.ToString();
                json.Guarantees().IsNotNullOrEmpty();
                var schEvent = JsonConvert.DeserializeObject<ScheduledEvent>(json)!;
                schEvent.Guarantees().IsNotNull();

                await _sink.Enqueue(
                    new AppEventOrigin(nameof(SinkEventJob), null, null), 
                    schEvent.EventTopic, null,
                    schEvent, null, schEvent.Tags, false);

                await _sink.CommitBatch();
            } catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Information,
                    ex.TraceInformation(), 5);
                throw new JobExecutionException(ex);
            }

        }
    }
}
