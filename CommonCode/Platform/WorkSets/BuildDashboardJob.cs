using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;

using BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;
namespace BFormDomain.CommonCode.Platform.WorkSets;

/// <summary>
/// BuildDashboardJob imploements Execute from IJob to build workset dashboard via an instance of WorkSetLogic
///     -References:
///         >QuartzISchedulerLogic.cs
///     -Functions:
///         >Execute 
/// </summary>
public class BuildDashboardJob: IJobIntegration
{
    private readonly WorkSetLogic _logic;
    private readonly IApplicationAlert _alerts;

    public BuildDashboardJob(WorkSetLogic logic, IApplicationAlert alerts)
    {
        _logic = logic;
        _alerts = alerts;
    }

    public async Task Execute()
    {
        using(PerfTrack.Stopwatch(nameof(BuildDashboardJob)))
        {
            try
            {
                //JobDataMap dataMap = context.JobDetail.JobDataMap;
                //var json = dataMap.GetString(nameof(BuildDashboardJob))!;
                //json.Guarantees().IsNotNullOrEmpty();

                var jobj = new { };// CAG Change need to somehow fill out jobdatamap
                var json = JsonConvert.SerializeObject(jobj);

                var workSet = JsonConvert.DeserializeObject<WorkSet>(json)!;
                workSet.Guarantees().IsNotNull();

                await _logic.BuildWorkSetDashboard(workSet);

            } catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Information,
                    ex.TraceInformation(), 5);
                throw new JobExecutionException(ex);
            }
    }
    }

}
