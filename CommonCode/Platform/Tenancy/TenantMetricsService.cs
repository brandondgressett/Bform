using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Service for collecting and tracking tenant-specific metrics.
/// Provides insights into tenant usage patterns and performance.
/// </summary>
public class TenantMetricsService
{
    private readonly TenantRepository _tenantRepository;
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ILogger<TenantMetricsService> _logger;
    
    // In-memory metrics storage (could be replaced with a proper metrics store)
    private readonly ConcurrentDictionary<Guid, TenantMetrics> _metricsCache = new();
    private readonly ConcurrentDictionary<Guid, Queue<TenantOperation>> _operationHistory = new();
    private readonly int _maxOperationHistory = 1000;

    public TenantMetricsService(
        TenantRepository tenantRepository,
        ITenantConnectionProvider connectionProvider,
        ILogger<TenantMetricsService> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records an operation for a tenant.
    /// </summary>
    public void RecordOperation(
        Guid tenantId, 
        string operationType, 
        TimeSpan duration, 
        bool success,
        string? details = null)
    {
        try
        {
            var operation = new TenantOperation
            {
                Timestamp = DateTime.UtcNow,
                Type = operationType,
                Duration = duration,
                Success = success,
                Details = details
            };

            // Update operation history
            var history = _operationHistory.GetOrAdd(tenantId, _ => new Queue<TenantOperation>());
            lock (history)
            {
                history.Enqueue(operation);
                while (history.Count > _maxOperationHistory)
                {
                    history.Dequeue();
                }
            }

            // Update metrics
            var metrics = _metricsCache.GetOrAdd(tenantId, _ => new TenantMetrics { TenantId = tenantId });
            UpdateMetrics(metrics, operation);

            _logger.LogDebug(
                "Recorded operation for tenant {TenantId}: Type={OperationType}, Duration={Duration}ms, Success={Success}",
                tenantId, operationType, duration.TotalMilliseconds, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record operation for tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Gets current metrics for a specific tenant.
    /// </summary>
    public async Task<TenantMetrics?> GetMetricsAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if tenant exists
            var (tenant, _) = await _tenantRepository.LoadAsync(tenantId);
            if (tenant == null)
            {
                return null;
            }

            // Get or create metrics
            var metrics = _metricsCache.GetOrAdd(tenantId, _ => new TenantMetrics { TenantId = tenantId });

            // Update connection health
            var connectionTests = await _connectionProvider.TestConnectionAsync(tenantId, cancellationToken);
            metrics.DatabaseConnectionHealthy = connectionTests.GetValueOrDefault(ConnectionType.Database);
            metrics.StorageConnectionHealthy = connectionTests.GetValueOrDefault(ConnectionType.Storage);
            metrics.LastHealthCheck = DateTime.UtcNow;

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for tenant {TenantId}", tenantId);
            return null;
        }
    }

    /// <summary>
    /// Gets aggregated metrics for all active tenants.
    /// </summary>
    public async Task<AggregatedTenantMetrics> GetAggregatedMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var aggregated = new AggregatedTenantMetrics
        {
            Timestamp = DateTime.UtcNow
        };

        try
        {
            var activeTenants = await _tenantRepository.GetActiveTenantsAsync(cancellationToken);
            aggregated.TotalTenants = activeTenants.Count;

            foreach (var tenant in activeTenants)
            {
                if (_metricsCache.TryGetValue(tenant.Id, out var metrics))
                {
                    aggregated.TotalOperations += metrics.TotalOperations;
                    aggregated.TotalErrors += metrics.ErrorCount;
                    aggregated.AverageResponseTime = 
                        ((aggregated.AverageResponseTime * (aggregated.TotalTenants - 1)) + metrics.AverageResponseTime) 
                        / aggregated.TotalTenants;

                    if (!metrics.DatabaseConnectionHealthy || !metrics.StorageConnectionHealthy)
                    {
                        aggregated.UnhealthyTenants++;
                    }
                }
            }

            aggregated.HealthyTenants = aggregated.TotalTenants - aggregated.UnhealthyTenants;
            aggregated.ErrorRate = aggregated.TotalOperations > 0 
                ? (double)aggregated.TotalErrors / aggregated.TotalOperations 
                : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get aggregated metrics");
        }

        return aggregated;
    }

    /// <summary>
    /// Gets operation history for a tenant.
    /// </summary>
    public List<TenantOperation> GetOperationHistory(Guid tenantId, int? limit = null)
    {
        if (!_operationHistory.TryGetValue(tenantId, out var history))
        {
            return new List<TenantOperation>();
        }

        lock (history)
        {
            var operations = history.ToList();
            if (limit.HasValue && limit.Value > 0)
            {
                operations = operations.TakeLast(limit.Value).ToList();
            }
            return operations;
        }
    }

    /// <summary>
    /// Clears metrics for a specific tenant.
    /// </summary>
    public void ClearMetrics(Guid tenantId)
    {
        _metricsCache.TryRemove(tenantId, out _);
        _operationHistory.TryRemove(tenantId, out _);
        _logger.LogInformation("Cleared metrics for tenant {TenantId}", tenantId);
    }

    /// <summary>
    /// Updates metrics based on a new operation.
    /// </summary>
    private void UpdateMetrics(TenantMetrics metrics, TenantOperation operation)
    {
        lock (metrics)
        {
            metrics.TotalOperations++;
            metrics.LastActivity = operation.Timestamp;

            if (!operation.Success)
            {
                metrics.ErrorCount++;
                metrics.LastError = operation.Timestamp;
            }

            // Update operation counts
            if (!metrics.OperationCounts.TryAdd(operation.Type, 1))
            {
                metrics.OperationCounts[operation.Type]++;
            }

            // Update average response time
            var totalDuration = metrics.AverageResponseTime * (metrics.TotalOperations - 1);
            metrics.AverageResponseTime = (totalDuration + operation.Duration.TotalMilliseconds) / metrics.TotalOperations;

            // Track peak response time
            if (operation.Duration.TotalMilliseconds > metrics.PeakResponseTime)
            {
                metrics.PeakResponseTime = operation.Duration.TotalMilliseconds;
            }
        }
    }
}

/// <summary>
/// Metrics for a specific tenant.
/// </summary>
public class TenantMetrics
{
    public Guid TenantId { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.MinValue;
    public DateTime? LastError { get; set; }
    public long TotalOperations { get; set; }
    public long ErrorCount { get; set; }
    public double AverageResponseTime { get; set; } // in milliseconds
    public double PeakResponseTime { get; set; } // in milliseconds
    public bool DatabaseConnectionHealthy { get; set; } = true;
    public bool StorageConnectionHealthy { get; set; } = true;
    public DateTime? LastHealthCheck { get; set; }
    public ConcurrentDictionary<string, long> OperationCounts { get; set; } = new();

    public double ErrorRate => TotalOperations > 0 ? (double)ErrorCount / TotalOperations : 0;
}

/// <summary>
/// Aggregated metrics across all tenants.
/// </summary>
public class AggregatedTenantMetrics
{
    public DateTime Timestamp { get; set; }
    public int TotalTenants { get; set; }
    public int HealthyTenants { get; set; }
    public int UnhealthyTenants { get; set; }
    public long TotalOperations { get; set; }
    public long TotalErrors { get; set; }
    public double ErrorRate { get; set; }
    public double AverageResponseTime { get; set; } // in milliseconds
}

/// <summary>
/// Represents a single operation performed by a tenant.
/// </summary>
public class TenantOperation
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? Details { get; set; }
}