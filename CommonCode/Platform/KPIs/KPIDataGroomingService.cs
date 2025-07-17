using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using Microsoft.Extensions.Hosting;

namespace BFormDomain.CommonCode.Platform.KPIs;

/// <summary>
/// KPIDataGroomingService grooms KPI data from the KPI template data where KPITemplate.DataGroomingTimeFrame is not null
///     -References:
///         >Service
///     -Funtions:
///         >ExecuteAsync
/// </summary>
public class KPIDataGroomingService: BackgroundService
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationPlatformContent _content;
    private readonly KPILogic _logic;


    public KPIDataGroomingService(
        IApplicationAlert alerts, 
        IApplicationPlatformContent content,
        KPILogic logic)
    {
        _alerts = alerts;
        _content = content; 
        _logic = logic;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var groomable = _content.GetAllContent<KPITemplate>()
                    .Where(t=>t.DataGroomingTimeFrame is not null);

                Parallel.ForEach(groomable, async templ =>
                {
                    try
                    {
                        await _logic.GroomKPIData(templ.Name);
                    }
                    catch (Exception x)
                    {
                        _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Information,
                                    x.TraceInformation(), 1);
                    }
                });



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
