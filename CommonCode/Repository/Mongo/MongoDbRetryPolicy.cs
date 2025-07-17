using MongoDB.Driver;
using Polly;
using Polly.Retry;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Repository.Mongo;

/// <summary>
/// Provides retry policies for MongoDB operations with exponential backoff and circuit breaker patterns
/// </summary>
public static class MongoDbRetryPolicy
{
    /// <summary>
    /// Creates an async retry policy for MongoDB operations
    /// </summary>
    public static AsyncRetryPolicy CreateAsyncRetryPolicy(ILogger? logger = null, int maxRetryCount = 3)
    {
        return Policy
            .Handle<MongoException>()
            .Or<TimeoutException>()
            .Or<System.IO.IOException>()
            .WaitAndRetryAsync(
                maxRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)), // Exponential backoff: 1s, 2s, 4s
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger?.LogWarning(exception,
                        "MongoDB operation failed on attempt {RetryCount}. Waiting {TimeSpan}ms before next retry",
                        retryCount, timeSpan.TotalMilliseconds);
                });
    }

    /// <summary>
    /// Creates a sync retry policy for MongoDB operations
    /// </summary>
    public static RetryPolicy CreateSyncRetryPolicy(ILogger? logger = null, int maxRetryCount = 3)
    {
        return Policy
            .Handle<MongoException>()
            .Or<TimeoutException>()
            .Or<System.IO.IOException>()
            .WaitAndRetry(
                maxRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger?.LogWarning(exception,
                        "MongoDB operation failed on attempt {RetryCount}. Waiting {TimeSpan}ms before next retry",
                        retryCount, timeSpan.TotalMilliseconds);
                });
    }

    /// <summary>
    /// Creates an advanced async policy with circuit breaker and retry
    /// </summary>
    public static IAsyncPolicy CreateAdvancedAsyncPolicy(
        ILogger? logger = null,
        int maxRetryCount = 3,
        int circuitBreakerThreshold = 5,
        TimeSpan circuitBreakerDuration = default)
    {
        if (circuitBreakerDuration == default)
        {
            circuitBreakerDuration = TimeSpan.FromSeconds(30);
        }

        // Create retry policy
        var retryPolicy = CreateAsyncRetryPolicy(logger, maxRetryCount);

        // Create circuit breaker policy
        var circuitBreakerPolicy = Policy
            .Handle<MongoException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                circuitBreakerThreshold,
                circuitBreakerDuration,
                onBreak: (exception, duration) =>
                {
                    logger?.LogError(exception,
                        "MongoDB circuit breaker opened for {Duration}s after {Threshold} failures",
                        duration.TotalSeconds, circuitBreakerThreshold);
                },
                onReset: () =>
                {
                    logger?.LogInformation("MongoDB circuit breaker reset - operations resuming");
                },
                onHalfOpen: () =>
                {
                    logger?.LogInformation("MongoDB circuit breaker is half-open - testing connection");
                });

        // Combine policies: circuit breaker wraps retry
        return Policy.WrapAsync(circuitBreakerPolicy, retryPolicy);
    }

    /// <summary>
    /// Creates a policy for bulk operations with specific handling
    /// </summary>
    public static AsyncRetryPolicy CreateBulkOperationPolicy(ILogger? logger = null)
    {
        return Policy
            .Handle<MongoBulkWriteException>()
            .Or<MongoException>()
            .WaitAndRetryAsync(
                2, // Fewer retries for bulk operations
                retryAttempt => TimeSpan.FromSeconds(retryAttempt * 2),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    if (exception is MongoBulkWriteException bulkException)
                    {
                        logger?.LogWarning(
                            "MongoDB bulk operation partially failed. Processed: {Processed}, " +
                            "Errors: {ErrorCount}. Retrying in {TimeSpan}ms",
                            0, // MongoDB 3.x doesn't expose processed count
                            bulkException.WriteErrors?.Count ?? 0,
                            timeSpan.TotalMilliseconds);
                    }
                    else
                    {
                        logger?.LogWarning(exception,
                            "MongoDB bulk operation failed on attempt {RetryCount}. Waiting {TimeSpan}ms",
                            retryCount, timeSpan.TotalMilliseconds);
                    }
                });
    }

    /// <summary>
    /// Executes an async operation with retry policy
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var policy = CreateAsyncRetryPolicy(logger);
        return await policy.ExecuteAsync(operation, cancellationToken);
    }

    /// <summary>
    /// Executes a sync operation with retry policy
    /// </summary>
    public static T Execute<T>(
        Func<T> operation,
        ILogger? logger = null)
    {
        var policy = CreateSyncRetryPolicy(logger);
        return policy.Execute(operation);
    }

    /// <summary>
    /// Determines if an exception should trigger a retry
    /// </summary>
    public static bool ShouldRetry(Exception exception)
    {
        return exception switch
        {
            MongoConnectionException => true,
            MongoNodeIsRecoveringException => true,
            MongoNotPrimaryException => true,
            TimeoutException => true,
            System.IO.IOException => true,
            MongoCommandException cmdEx when IsRetryableCommandException(cmdEx) => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if a command exception is retryable
    /// </summary>
    private static bool IsRetryableCommandException(MongoCommandException exception)
    {
        // MongoDB error codes that indicate transient issues
        var retryableErrorCodes = new[] { 
            11600, // InterruptedAtShutdown
            11602, // InterruptedDueToReplStateChange
            13435, // NotMasterNoSlaveOk
            13436, // NotMasterOrSecondary
            189,   // PrimarySteppedDown
            91,    // ShutdownInProgress
            7,     // HostNotFound
            6,     // HostUnreachable
            89,    // NetworkTimeout
            9001,  // SocketException
            10107  // NotMaster
        };

        return Array.IndexOf(retryableErrorCodes, exception.Code) >= 0;
    }
}