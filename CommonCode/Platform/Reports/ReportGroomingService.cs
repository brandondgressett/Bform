using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Reports;

/// <summary>
/// ReportGroomingService grooms report data from the report instance data where GroomDate is not null and GroomDate is in the past
///     -References:
///         >Service
///     -Functions:
///         >ExecuteAsync
/// </summary>
public class ReportGroomingService: BackgroundService
{
    
    private readonly IRepository<ReportInstance> _reports;
    private readonly IApplicationAlert _alerts;
    private readonly Tagger _tagger;

    public ReportGroomingService(
        IRepository<ReportInstance> reports,
        Tagger tagger,
        IApplicationAlert alerts)
    {
       _tagger = tagger;
        _alerts = alerts;
        _reports = reports;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // TODO: page in descending order
                var (needsGrooming, rc) = await _reports.GetAllAsync(it => it.GroomDate != null && it.GroomDate < DateTime.UtcNow);

                foreach(var report in needsGrooming)
                {
                    try
                    {
                        // groomed reports don't create events. Should they?
                        await _reports.DeleteAsync(report);
                        await _tagger.Untag(report, _reports, report.Tags);

                    } catch(Exception x)
                    {
                        _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Information,
                                    x.TraceInformation(), 1);
                    }
                }



                await Task.Delay(60 * 1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // catch the cancellation exception
                // to stop execution
                return;
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Information,
                    ex.TraceInformation(), 1);
            }
        }
    }






}
