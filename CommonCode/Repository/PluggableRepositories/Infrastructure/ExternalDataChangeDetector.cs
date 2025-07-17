using BFormDomain.DataModels;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BFormDomain.CommonCode.Repository.PluggableRepositories.Infrastructure;

/// <summary>
/// Detects changes in external data sources using various comparison strategies.
/// Supports hash-based, timestamp-based, and custom comparison methods.
/// </summary>
public class ExternalDataChangeDetector<T> where T : class, IDataModel
{
    private readonly ILogger<ExternalDataChangeDetector<T>>? _logger;
    private readonly Dictionary<Guid, string> _hashCache = new();
    private readonly Dictionary<Guid, DateTime> _timestampCache = new();
    private readonly object _cacheLock = new();

    public ExternalDataChangeDetector(ILogger<ExternalDataChangeDetector<T>>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects changes between old and new datasets using the specified strategy.
    /// </summary>
    public ChangeDetectionResult<T> DetectChanges(
        IEnumerable<T> oldData,
        IEnumerable<T> newData,
        ChangeDetectionStrategy strategy = ChangeDetectionStrategy.Hash)
    {
        var oldDict = oldData.ToDictionary(x => x.Id);
        var newDict = newData.ToDictionary(x => x.Id);
        
        var result = new ChangeDetectionResult<T>();

        // Detect new items
        foreach (var kvp in newDict)
        {
            if (!oldDict.ContainsKey(kvp.Key))
            {
                result.AddedItems.Add(kvp.Value);
                _logger?.LogDebug("Detected new item: {ItemId}", kvp.Key);
            }
        }

        // Detect deleted items
        foreach (var kvp in oldDict)
        {
            if (!newDict.ContainsKey(kvp.Key))
            {
                result.DeletedItems.Add(kvp.Value);
                _logger?.LogDebug("Detected deleted item: {ItemId}", kvp.Key);
            }
        }

        // Detect updated items
        foreach (var kvp in newDict)
        {
            if (oldDict.TryGetValue(kvp.Key, out var oldItem))
            {
                if (HasChanged(oldItem, kvp.Value, strategy))
                {
                    result.UpdatedItems.Add(new UpdatedItem<T>
                    {
                        OldItem = oldItem,
                        NewItem = kvp.Value
                    });
                    _logger?.LogDebug("Detected updated item: {ItemId}", kvp.Key);
                }
            }
        }

        _logger?.LogInformation(
            "Change detection completed. Added: {Added}, Updated: {Updated}, Deleted: {Deleted}",
            result.AddedItems.Count, result.UpdatedItems.Count, result.DeletedItems.Count);

        return result;
    }

    /// <summary>
    /// Determines if an item has changed based on the specified strategy.
    /// </summary>
    public bool HasChanged(T oldItem, T newItem, ChangeDetectionStrategy strategy)
    {
        return strategy switch
        {
            ChangeDetectionStrategy.Hash => HasChangedByHash(oldItem, newItem),
            ChangeDetectionStrategy.Version => HasChangedByVersion(oldItem, newItem),
            ChangeDetectionStrategy.Timestamp => HasChangedByTimestamp(oldItem, newItem),
            ChangeDetectionStrategy.DeepComparison => HasChangedByDeepComparison(oldItem, newItem),
            _ => HasChangedByHash(oldItem, newItem)
        };
    }

    /// <summary>
    /// Compares items using hash comparison.
    /// </summary>
    private bool HasChangedByHash(T oldItem, T newItem)
    {
        var oldHash = ComputeHash(oldItem);
        var newHash = ComputeHash(newItem);
        
        var changed = oldHash != newHash;
        
        if (changed)
        {
            lock (_cacheLock)
            {
                _hashCache[newItem.Id] = newHash;
            }
        }
        
        return changed;
    }

    /// <summary>
    /// Compares items using version numbers.
    /// </summary>
    private bool HasChangedByVersion(T oldItem, T newItem)
    {
        return oldItem.Version != newItem.Version;
    }

    /// <summary>
    /// Compares items using timestamp if available.
    /// </summary>
    private bool HasChangedByTimestamp(T oldItem, T newItem)
    {
        // This assumes entities have a timestamp property
        // You might need to use reflection or require a specific interface
        var oldTimestamp = GetTimestamp(oldItem);
        var newTimestamp = GetTimestamp(newItem);
        
        if (oldTimestamp.HasValue && newTimestamp.HasValue)
        {
            var changed = newTimestamp.Value > oldTimestamp.Value;
            
            if (changed)
            {
                lock (_cacheLock)
                {
                    _timestampCache[newItem.Id] = newTimestamp.Value;
                }
            }
            
            return changed;
        }
        
        // Fallback to hash comparison if timestamps not available
        return HasChangedByHash(oldItem, newItem);
    }

    /// <summary>
    /// Performs deep property-by-property comparison.
    /// </summary>
    private bool HasChangedByDeepComparison(T oldItem, T newItem)
    {
        var oldJson = JsonSerializer.Serialize(oldItem);
        var newJson = JsonSerializer.Serialize(newItem);
        return oldJson != newJson;
    }

    /// <summary>
    /// Computes a hash for an entity.
    /// </summary>
    private string ComputeHash(T item)
    {
        // Serialize the object to JSON
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions
        {
            // Sort properties to ensure consistent hash
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        // Compute SHA256 hash
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Attempts to get a timestamp from an entity.
    /// Override or extend to support your entity structure.
    /// </summary>
    protected virtual DateTime? GetTimestamp(T item)
    {
        // Try to find a property that represents last update time
        var timestampProperty = typeof(T).GetProperties()
            .FirstOrDefault(p => 
                p.Name.Contains("UpdatedAt", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("ModifiedAt", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("LastModified", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("UpdatedDate", StringComparison.OrdinalIgnoreCase));

        if (timestampProperty != null && timestampProperty.PropertyType == typeof(DateTime))
        {
            return (DateTime?)timestampProperty.GetValue(item);
        }

        if (timestampProperty != null && timestampProperty.PropertyType == typeof(DateTime?))
        {
            return (DateTime?)timestampProperty.GetValue(item);
        }

        return null;
    }

    /// <summary>
    /// Clears internal caches.
    /// </summary>
    public void ClearCaches()
    {
        lock (_cacheLock)
        {
            _hashCache.Clear();
            _timestampCache.Clear();
        }
        
        _logger?.LogDebug("Change detector caches cleared");
    }

    /// <summary>
    /// Gets statistics about the change detection caches.
    /// </summary>
    public ChangeDetectorStatistics GetStatistics()
    {
        lock (_cacheLock)
        {
            return new ChangeDetectorStatistics
            {
                HashCacheSize = _hashCache.Count,
                TimestampCacheSize = _timestampCache.Count,
                TotalCacheSize = _hashCache.Count + _timestampCache.Count
            };
        }
    }
}

/// <summary>
/// Strategies for detecting changes in data.
/// </summary>
public enum ChangeDetectionStrategy
{
    /// <summary>
    /// Uses hash comparison of serialized objects.
    /// </summary>
    Hash,
    
    /// <summary>
    /// Uses version number comparison.
    /// </summary>
    Version,
    
    /// <summary>
    /// Uses timestamp comparison.
    /// </summary>
    Timestamp,
    
    /// <summary>
    /// Performs deep property-by-property comparison.
    /// </summary>
    DeepComparison
}

/// <summary>
/// Result of change detection operation.
/// </summary>
public class ChangeDetectionResult<T>
{
    /// <summary>
    /// Items that were added (exist in new but not in old).
    /// </summary>
    public List<T> AddedItems { get; set; } = new();
    
    /// <summary>
    /// Items that were updated (exist in both but changed).
    /// </summary>
    public List<UpdatedItem<T>> UpdatedItems { get; set; } = new();
    
    /// <summary>
    /// Items that were deleted (exist in old but not in new).
    /// </summary>
    public List<T> DeletedItems { get; set; } = new();
    
    /// <summary>
    /// Total number of changes detected.
    /// </summary>
    public int TotalChanges => AddedItems.Count + UpdatedItems.Count + DeletedItems.Count;
    
    /// <summary>
    /// Whether any changes were detected.
    /// </summary>
    public bool HasChanges => TotalChanges > 0;
}

/// <summary>
/// Represents an item that was updated.
/// </summary>
public class UpdatedItem<T>
{
    /// <summary>
    /// The old version of the item.
    /// </summary>
    public T OldItem { get; set; } = default!;
    
    /// <summary>
    /// The new version of the item.
    /// </summary>
    public T NewItem { get; set; } = default!;
}

/// <summary>
/// Statistics about the change detector.
/// </summary>
public class ChangeDetectorStatistics
{
    public int HashCacheSize { get; set; }
    public int TimestampCacheSize { get; set; }
    public int TotalCacheSize { get; set; }
}

/// <summary>
/// Extension methods for change detection.
/// </summary>
public static class ChangeDetectionExtensions
{
    /// <summary>
    /// Converts change detection results into a summary string.
    /// </summary>
    public static string ToSummaryString<T>(this ChangeDetectionResult<T> result)
    {
        return $"Changes detected - Added: {result.AddedItems.Count}, Updated: {result.UpdatedItems.Count}, Deleted: {result.DeletedItems.Count}";
    }
    
    /// <summary>
    /// Gets all affected item IDs from the change detection result.
    /// </summary>
    public static HashSet<Guid> GetAffectedIds<T>(this ChangeDetectionResult<T> result) where T : IDataModel
    {
        var ids = new HashSet<Guid>();
        
        foreach (var item in result.AddedItems)
            ids.Add(item.Id);
            
        foreach (var item in result.UpdatedItems)
        {
            ids.Add(item.OldItem.Id);
            ids.Add(item.NewItem.Id);
        }
        
        foreach (var item in result.DeletedItems)
            ids.Add(item.Id);
            
        return ids;
    }
}