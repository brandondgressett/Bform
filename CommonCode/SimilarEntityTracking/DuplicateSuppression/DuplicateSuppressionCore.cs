using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// -DuplicateSuppressionCore is used by the DuplicateSuppressionService to determine if messages should be suppressed.
/// </summary>
/// <typeparam name="T"></typeparam>
public class DuplicateSuppressionCore<T>
    where T: class, ICanShutUp, new()
{

    /// <summary>
    /// _logger is an injected instance used to log information specified. 
    /// </summary>
    private readonly ILogger<DuplicateSuppressionCore<T>> _logger;

    /// <summary>
    /// _suppressionPersistence evaluates suppression information on any given item
    /// </summary> 
    private readonly ISuppressionPersistence _suppressionPersistence;

    /// <summary>
    /// _emergencyPause is set when an exception is caught and checked for before continuing ShouldBeSuppressed()
    /// </summary>
    private DateTime _emergencyPause = DateTime.MinValue;

    /// <summary>
    /// DI Constructor. Register as transient.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="repo"></param>
    public DuplicateSuppressionCore(
        ILogger<DuplicateSuppressionCore<T>> logger,
        ISuppressionPersistence repo) =>
        (_logger,_suppressionPersistence) = (logger, repo);

    /// <summary>
    /// LogSuppressionStart() logs information about the item being suppressed and what time the items suppression starts
    /// </summary>
    /// <param name="item">Instance of ICanShutUp</param>
    private void LogSuppressionStart(T item)
    {
        _logger.LogInformation($"starting suppression for {item.ComparisonType} {item.TargetId} {item.ComparisonHash}, {item.SuppressionTimeMinutes} minutes.");
    }

    /// <summary>
    /// -ShouldBeSuppressed determines if a message should be suppressed
    /// -Each message is passed into injected instance _suppressionPersistence.GetSuppressionInfo() to determine if messages of its kind are already suppressed
    /// -Help! Help! I'm being suppressed! 
    /// BLOODY PEASANT.
    /// </summary>
    /// <param name="item">Instance of SuppressDuplicatesMessage</param>
    /// <returns></returns>
    public async Task<bool> ShouldBeSuppressed(T item)
    {
        
        item.Requires().IsNotNull();
        if (DateTime.UtcNow < _emergencyPause)
            return true;

        try
        {
            // any suppression info similar to this item in existence?
            var suppression = await _suppressionPersistence.GetSuppressionInfo(item);
            if (suppression is null)
            {
                await _suppressionPersistence.SuppressStartingNow(item);
                LogSuppressionStart(item);
                return false;
            }

            if (item.ComparisonHash == suppression.ComparisonHash &&
                item.TargetId == suppression.TargetId &&
                item.ComparisonPropertyString == suppression.ComparisonPropertyString)
            {
                var suppressionEnd = suppression.SuppressionStartTime + TimeSpan.FromMinutes(item.SuppressionTimeMinutes);
                if (DateTime.UtcNow < suppressionEnd)
                {
                    _logger.LogInformation($"Suppressing {item.ComparisonType} {item.TargetId} {item.ComparisonHash} until {suppressionEnd.ToShortDateString()} {suppressionEnd.ToShortTimeString()}");
                    return true;
                }

                await _suppressionPersistence.SuppressStartingNow(item); // You saw him suppressing me didn't you?
                LogSuppressionStart(item);
            }
        } catch(Exception ex)
        {
            // persistence layer may use application alerts.
            // application alerts may use notifications.
            // notifications will use duplicate suppression.
            // if we want to avoid an infinite loop, we need to stop it here.

            _logger.LogCritical(ex.TraceInformation());
            _emergencyPause = DateTime.UtcNow + TimeSpan.FromMinutes(15.0);
        }
        

        return false;

    }
        


}



