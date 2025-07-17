namespace BFormDomain.Mongo;

public class MongoRepositoryOptions
{
    // Basic connection settings
    public string MongoConnectionString { get; set; } = "";
    public string DatabaseName { get; set; } = "BForm";

    // Pagination settings
    public int DefaultPageSize { get; set; } = 50;
    public int FaultLimit { get; set; } = 15;

    // Connection pooling settings
    public int MaxConnectionPoolSize { get; set; } = 100;
    public int MinConnectionPoolSize { get; set; } = 10;
    public int WaitQueueTimeoutMs { get; set; } = 60000; // 60 seconds
    public int ConnectionIdleTimeoutMs { get; set; } = 600000; // 10 minutes
    public int ConnectionLifetimeMs { get; set; } = 1800000; // 30 minutes

    // Performance settings
    public int CommandTimeoutMs { get; set; } = 30000; // 30 seconds default
    public int SocketTimeoutMs { get; set; } = 300000; // 5 minutes
    public bool EnableQueryLogging { get; set; } = false;
    public int SlowQueryThresholdMs { get; set; } = 1000; // 1 second
    public bool UseSsl { get; set; } = true;

    // Retry policy settings
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 100;
    public int RetryBackoffMultiplier { get; set; } = 2;
    public int MaxRetryDelayMs { get; set; } = 5000;

    // Read preference settings
    public string ReadPreference { get; set; } = "Primary"; // Primary, PrimaryPreferred, Secondary, SecondaryPreferred, Nearest
    public int MaxStalenessSeconds { get; set; } = 90;

    // Write concern settings  
    public string WriteConcern { get; set; } = "Acknowledged"; // Acknowledged, W1, W2, W3, Majority, Journaled
    public int WriteConcernTimeoutMs { get; set; } = 10000;

    // Performance monitoring
    public bool EnablePerformanceCounters { get; set; } = false;
    public bool EnableServerMonitoring { get; set; } = true;
    public int HeartbeatIntervalMs { get; set; } = 10000;

    // Bulk operation settings
    public int BulkWriteBatchSize { get; set; } = 1000;
    public bool BulkWriteOrdered { get; set; } = false;

    // Index hint settings
    public bool EnableIndexHints { get; set; } = true;
    public Dictionary<string, string> IndexHints { get; set; } = new Dictionary<string, string>();

    // Advanced retry policy settings (using Polly)
    public bool EnableRetryPolicy { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3; // For Polly retry policy
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    // Auto-index creation
    public bool AutoCreateIndexes { get; set; } = true;
}
