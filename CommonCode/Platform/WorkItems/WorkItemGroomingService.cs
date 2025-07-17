using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using Microsoft.Extensions.Hosting;

namespace BFormDomain.CommonCode.Platform.WorkItems;

/// <summary>
/// WorkItemGroomingService uses WorkItemLogic.DeleteGroomableWorkItems() to groom work items 
///     -References:
///         >Service
///     -Functions:
///         >ExecuteAsync
/// </summary>
public class WorkItemGroomingService: BackgroundService
{
    private readonly WorkItemLogic _logic;
    private readonly IApplicationAlert _alerts;

    public WorkItemGroomingService(
        WorkItemLogic logic,
        IApplicationAlert alerts)
    {
        _logic = logic;
        _alerts = alerts;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _logic.DeleteGroomableWorkItems();

                Thread.Sleep(10000);
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
                                    ex.TraceInformation(), 5);
            }
        }
    }
}
