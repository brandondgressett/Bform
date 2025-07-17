using BFormDomain.HelperClasses;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BFormDomain.CommonCode.Platform.ManagedFiles;

namespace BFormDomain.CommonCode.Platform.ManagedFiles;
/// <summary>
/// This removes files that haven't been interacted with for a while.
///     NO, THAT'S NOT HOW IT WORKS. NEED A MORE DETAILED, ACCURATE DESCRIPTION ABOUT
///     WHY FILES ARE GROOMED.
/// </summary>
public class ManagedFileGroomingService: IHostedService, IDisposable
{
    /// <summary>
    /// Where the files are stored.
    /// </summary>
    private readonly ManagedFileLogic _store;
    
    /// <summary>
    /// The logger for the grooming service.
    /// </summary>
    private readonly ILogger<ManagedFileGroomingService> _logger;

    /// <summary>
    /// When to check to groom.
    /// </summary>
    private Timer _timer = null!;
    
    public ManagedFileGroomingService(
            ManagedFileLogic store, ILogger<ManagedFileGroomingService> logger) =>
        (_store, _logger) = (store, logger);
    
    /// <summary>
    /// Starts the timer for checking if files need to be removed.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns>Task.ComletedTask</returns>
    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.FromHours(4.0), TimeSpan.FromHours(4.0));
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Removes files that haven't been interacted with.
    /// </summary>
    /// <param name="_"></param>
    private void DoWork(object? _)
    {
        try
        {
            int page = 0;
            bool searching = true;

            while (searching)
            {
                // ugly, but simple
                var filePage = AsyncHelper.RunSync<IList<ManagedFileInstance>>(
                    ()=> _store.GetGroomableFiles(page));
                
                foreach(var file in filePage)
                {
                    try
                    {
                        AsyncHelper.RunSync(() => 
                            _store.GroomFileAsync(file.Id));
                    } catch // failing one doesn't mean we fail the others
                    {

                    }
                }

                searching = filePage.Any();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Managed File Grooming Failed: " + ex.TraceInformation());
        }

    }
    /// <summary>
    /// Stops the grooming timer.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }



}
