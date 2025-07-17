using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BFormDomain.CommonCode.Repository.Mongo;

/// <summary>
/// Represents a cursor for efficient pagination through large datasets
/// </summary>
public class CursorPaginationRequest
{
    /// <summary>
    /// The cursor position to start from (null for first page)
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Number of items to retrieve (max 1000)
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Direction of pagination (forward or backward)
    /// </summary>
    public CursorDirection Direction { get; set; } = CursorDirection.Forward;

    /// <summary>
    /// Field to use for cursor (must be unique and sortable)
    /// </summary>
    public string CursorField { get; set; } = "_id";

    /// <summary>
    /// Sort direction for the cursor field
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
}

/// <summary>
/// Cursor pagination direction
/// </summary>
public enum CursorDirection
{
    Forward,
    Backward
}

/// <summary>
/// Sort direction for cursor field
/// </summary>
public enum SortDirection
{
    Ascending = 1,
    Descending = -1
}

/// <summary>
/// Result of a cursor-based pagination query
/// </summary>
public class CursorPaginationResult<T>
{
    /// <summary>
    /// The retrieved items
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Cursor for the next page (null if no more pages)
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Cursor for the previous page (null if at first page)
    /// </summary>
    public string? PreviousCursor { get; set; }

    /// <summary>
    /// Whether there are more items after this page
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Whether there are items before this page
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Total count (only if requested, can be expensive)
    /// </summary>
    public long? TotalCount { get; set; }

    /// <summary>
    /// The actual page size returned
    /// </summary>
    public int PageSize => Items.Count;
}

/// <summary>
/// Helper class for cursor encoding/decoding
/// </summary>
public static class CursorHelper
{
    /// <summary>
    /// Encodes a cursor value to a base64 string
    /// </summary>
    public static string EncodeCursor(object value, string fieldName)
    {
        var doc = new BsonDocument
        {
            { "field", fieldName },
            { "value", BsonValue.Create(value) },
            { "timestamp", DateTime.UtcNow }
        };
        
        var bytes = doc.ToBson();
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decodes a cursor from base64 string
    /// </summary>
    public static (string field, BsonValue value, DateTime timestamp) DecodeCursor(string cursor)
    {
        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var doc = BsonDocument.Parse(json);
            
            return (
                doc["field"].AsString,
                doc["value"],
                doc["timestamp"].ToUniversalTime()
            );
        }
        catch
        {
            throw new ArgumentException("Invalid cursor format");
        }
    }

    /// <summary>
    /// Creates a filter for cursor-based pagination
    /// </summary>
    public static FilterDefinition<T> CreateCursorFilter<T>(
        string? cursor,
        string fieldName,
        CursorDirection direction,
        SortDirection sortDirection,
        FilterDefinition<T>? baseFilter = null)
    {
        var builder = Builders<T>.Filter;
        var cursorFilter = FilterDefinition<T>.Empty;

        if (!string.IsNullOrEmpty(cursor))
        {
            var (field, value, _) = DecodeCursor(cursor);
            
            if (field != fieldName)
            {
                throw new ArgumentException($"Cursor field mismatch. Expected: {fieldName}, Got: {field}");
            }

            // Determine comparison operator based on direction and sort
            if (direction == CursorDirection.Forward)
            {
                cursorFilter = sortDirection == SortDirection.Ascending
                    ? builder.Gt(fieldName, value)
                    : builder.Lt(fieldName, value);
            }
            else // Backward
            {
                cursorFilter = sortDirection == SortDirection.Ascending
                    ? builder.Lt(fieldName, value)
                    : builder.Gt(fieldName, value);
            }
        }

        // Combine with base filter if provided
        return baseFilter != null 
            ? builder.And(baseFilter, cursorFilter) 
            : cursorFilter;
    }
}