using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace BFormDomain.CommonCode.Platform.Content.AzureBlob;

/// <summary>
/// Azure Blob Storage implementation of IApplicationPlatformContent.
/// Stores and retrieves content schemas and instances from Azure Blob Storage.
/// </summary>
public class AzureBlobApplicationPlatformContent : IApplicationPlatformContent
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureBlobApplicationPlatformContentOptions _options;
    private readonly ILogger<AzureBlobApplicationPlatformContent> _logger;
    private readonly IApplicationAlert _alerts;
    private readonly IEnumerable<IContentDomainSource> _contentDomainSources;
    private readonly IEnumerable<IEntityInstanceLogic> _instanceConsumers;
    private readonly IMemoryCache? _cache;
    
    private readonly ConcurrentBag<ContentDomain> _domains = new();
    private readonly ConcurrentBag<ContentElement> _elements = new();
    private readonly ConcurrentDictionary<string, string> _schemaCache = new();
    private readonly ConcurrentDictionary<string, string> _freeJson = new();
    
    private bool _initialized = false;
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);

    public IEnumerable<ContentDomain> Domains => _domains;

    public AzureBlobApplicationPlatformContent(
        ILogger<AzureBlobApplicationPlatformContent> logger,
        IApplicationAlert alerts,
        IEnumerable<IContentDomainSource> contentDomainSources,
        IEnumerable<IEntityInstanceLogic> instanceConsumers,
        IOptions<AzureBlobApplicationPlatformContentOptions> options,
        IMemoryCache? cache = null)
    {
        _logger = logger;
        _alerts = alerts;
        _contentDomainSources = contentDomainSources;
        _instanceConsumers = instanceConsumers;
        _options = options.Value;
        _cache = _options.EnableCaching ? cache : null;

        // Initialize BlobServiceClient
        if (_options.UseManagedIdentity)
        {
            if (string.IsNullOrEmpty(_options.BlobServiceEndpoint))
            {
                throw new InvalidOperationException("BlobServiceEndpoint must be specified when using Managed Identity");
            }

            var credential = new DefaultAzureCredential();
            _blobServiceClient = new BlobServiceClient(new Uri(_options.BlobServiceEndpoint), credential);
        }
        else
        {
            if (string.IsNullOrEmpty(_options.ConnectionString))
            {
                throw new InvalidOperationException("ConnectionString must be specified when not using Managed Identity");
            }

            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        }

        if (_options.AutoInitialize)
        {
            Task.Run(async () => await InitializeAsync());
        }
    }

    /// <summary>
    /// Initializes the content system from Azure Blob Storage.
    /// </summary>
    private async Task InitializeAsync()
    {
        await _initializationSemaphore.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            _logger.LogInformation("Initializing Azure Blob content system");

            // Ensure containers exist
            await EnsureContainersExistAsync();

            // Load schemas from blob storage
            await LoadSchemasAsync();

            // Load content from domain sources
            foreach (var source in _contentDomainSources)
            {
                var domain = source.Tell(this);
                _domains.Add(domain);
                
                // TODO: ContentDomain no longer has AvailableContent property
                // Need to determine how to load content elements from domain
                /*
                foreach (var element in domain.AvailableContent)
                {
                    _elements.Add(element);
                }
                */
            }

            // Load free JSON content
            await LoadFreeJsonAsync();

            // Process instance consumers
            // TODO: IEntityInstanceLogic no longer has AcceptedContentTags property
            // Need to determine how to match content to consumers
            /*
            foreach (var consumer in _instanceConsumers)
            {
                var contentList = GetMatchingAll<IContentType>(consumer.AcceptedContentTags.ToArray());
                foreach (var content in contentList)
                {
                    consumer.AcceptContentInstance(content);
                }
            }
            */

            _initialized = true;
            _logger.LogInformation("Azure Blob content system initialized with {DomainCount} domains and {ElementCount} elements", 
                _domains.Count, _elements.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Blob content system");
            _alerts.RaiseAlert(ApplicationAlertKind.System, LogLevel.Error, 
                $"Content initialization failed: {ex.Message}", 1, nameof(AzureBlobApplicationPlatformContent));
            throw;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    /// <summary>
    /// Ensures all required containers exist.
    /// </summary>
    private async Task EnsureContainersExistAsync()
    {
        var containers = new[] 
        { 
            _options.SchemaContainerName, 
            _options.ContentContainerName, 
            _options.FreeJsonContainerName 
        };

        foreach (var containerName in containers)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        }
    }

    /// <summary>
    /// Loads schemas from blob storage.
    /// </summary>
    private async Task LoadSchemasAsync()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.SchemaContainerName);
        
        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            try
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var response = await blobClient.DownloadContentAsync();
                var content = response.Value.Content.ToString();
                
                // Remove .json extension if present
                var schemaName = blobItem.Name.EndsWith(".json") 
                    ? blobItem.Name.Substring(0, blobItem.Name.Length - 5) 
                    : blobItem.Name;
                    
                _schemaCache[schemaName] = content;
                
                _logger.LogDebug("Loaded schema {SchemaName} from blob storage", schemaName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load schema {BlobName}", blobItem.Name);
            }
        }
    }

    /// <summary>
    /// Loads free JSON content from blob storage.
    /// </summary>
    private async Task LoadFreeJsonAsync()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.FreeJsonContainerName);
        
        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            try
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var response = await blobClient.DownloadContentAsync();
                var content = response.Value.Content.ToString();
                
                // Remove .json extension if present
                var name = blobItem.Name.EndsWith(".json") 
                    ? blobItem.Name.Substring(0, blobItem.Name.Length - 5) 
                    : blobItem.Name;
                    
                _freeJson[name] = content;
                
                _logger.LogDebug("Loaded free JSON {Name} from blob storage", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load free JSON {BlobName}", blobItem.Name);
            }
        }
    }

    /// <summary>
    /// Loads an embedded schema for a content type.
    /// </summary>
    public JSchema LoadEmbeddedSchema<T>() where T : IContentType
    {
        var schemaName = typeof(T).Name;
        var cacheKey = $"schema_{schemaName}";
        
        // Check cache first
        if (_cache != null && _cache.TryGetValue<JSchema>(cacheKey, out var cachedSchema))
        {
            return cachedSchema!;
        }

        // Try to find schema in loaded schemas
        if (_schemaCache.TryGetValue(schemaName, out var schemaJson))
        {
            var schema = JSchema.Parse(schemaJson);
            
            // Cache the parsed schema
            if (_cache != null)
            {
                _cache.Set(cacheKey, schema, TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
            }
            
            return schema;
        }

        // If not found, try to load from embedded resources (fallback)
        var assembly = typeof(T).Assembly;
        var resourceName = $"{assembly.GetName().Name}.{schemaName}.json";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var schema = JSchema.Parse(json);
            
            // Save to blob storage for future use
            Task.Run(async () => await SaveSchemaAsync(schemaName, json));
            
            // Cache the schema
            if (_cache != null)
            {
                _cache.Set(cacheKey, schema, TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
            }
            
            return schema;
        }

        throw new InvalidOperationException($"Schema for type {typeof(T).Name} not found");
    }

    /// <summary>
    /// Saves a schema to blob storage.
    /// </summary>
    private async Task SaveSchemaAsync(string schemaName, string schemaJson)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.SchemaContainerName);
            var blobName = $"{schemaName}.json";
            var blobClient = containerClient.GetBlobClient(blobName);
            
            var content = BinaryData.FromString(schemaJson);
            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/json"
                },
                Tags = _options.DefaultBlobTags
            };
            
            await blobClient.UploadAsync(content, options);
            _schemaCache[schemaName] = schemaJson;
            
            _logger.LogInformation("Saved schema {SchemaName} to blob storage", schemaName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save schema {SchemaName} to blob storage", schemaName);
        }
    }

    /// <summary>
    /// Gets all content of a specific type.
    /// </summary>
    public IList<T> GetAllContent<T>() where T : IContentType
    {
        EnsureInitialized();
        
        var typeName = typeof(T).Name;
        var cacheKey = $"content_all_{typeName}";
        
        // Check cache first
        if (_cache != null && _cache.TryGetValue<IList<T>>(cacheKey, out var cachedContent))
        {
            return cachedContent!;
        }

        var results = new List<T>();
        
        foreach (var element in _elements.Where(e => e.Deserialized?.GetType() == typeof(T)))
        {
            if (element.Deserialized is T typedInstance)
            {
                results.Add(typedInstance);
            }
        }

        // Cache the results
        if (_cache != null && results.Any())
        {
            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
        }

        return results;
    }

    /// <summary>
    /// Gets content by name.
    /// </summary>
    public T? GetContentByName<T>(string name) where T : IContentType
    {
        EnsureInitialized();
        
        var typeName = typeof(T).Name;
        var cacheKey = $"content_{typeName}_{name}";
        
        // Check cache first
        if (_cache != null && _cache.TryGetValue<T>(cacheKey, out var cachedContent))
        {
            return cachedContent;
        }

        var element = _elements.FirstOrDefault(e => 
            e.Deserialized?.GetType() == typeof(T) && 
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            
        var result = element?.Deserialized is T typed ? typed : default(T);
        
        // Cache the result
        if (_cache != null && result != null)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
        }

        return result;
    }

    /// <summary>
    /// Views content type information.
    /// </summary>
    public ContentElement? ViewContentType(string name)
    {
        EnsureInitialized();
        return _elements.FirstOrDefault(e => 
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets content matching any of the specified tags.
    /// </summary>
    public IList<T> GetMatchingAny<T>(params string[] tags) where T : IContentType
    {
        EnsureInitialized();
        
        if (!tags.Any())
        {
            return new List<T>();
        }

        var cacheKey = $"content_any_{typeof(T).Name}_{string.Join("_", tags.OrderBy(t => t))}";
        
        // Check cache first
        if (_cache != null && _cache.TryGetValue<IList<T>>(cacheKey, out var cachedContent))
        {
            return cachedContent!;
        }

        var results = new List<T>();
        
        foreach (var element in _elements.Where(e => e.Deserialized?.GetType() == typeof(T)))
        {
            if (element.Tags.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
            {
                if (element.Deserialized is T typedInstance)
                {
                    results.Add(typedInstance);
                }
            }
        }

        // Cache the results
        if (_cache != null && results.Any())
        {
            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
        }

        return results;
    }

    /// <summary>
    /// Gets content matching all of the specified tags.
    /// </summary>
    public IList<T> GetMatchingAll<T>(params string[] tags) where T : IContentType
    {
        EnsureInitialized();
        
        if (!tags.Any())
        {
            return GetAllContent<T>();
        }

        var cacheKey = $"content_all_{typeof(T).Name}_{string.Join("_", tags.OrderBy(t => t))}";
        
        // Check cache first
        if (_cache != null && _cache.TryGetValue<IList<T>>(cacheKey, out var cachedContent))
        {
            return cachedContent!;
        }

        var results = new List<T>();
        
        foreach (var element in _elements.Where(e => e.Deserialized?.GetType() == typeof(T)))
        {
            if (tags.All(tag => element.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            {
                if (element.Deserialized is T typedInstance)
                {
                    results.Add(typedInstance);
                }
            }
        }

        // Cache the results
        if (_cache != null && results.Any())
        {
            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
        }

        return results;
    }

    /// <summary>
    /// Gets free JSON content by name.
    /// </summary>
    public string? GetFreeJson(string name)
    {
        EnsureInitialized();
        
        var cacheKey = $"freejson_{name}";
        
        // Check cache first
        if (_cache != null && _cache.TryGetValue<string>(cacheKey, out var cachedJson))
        {
            return cachedJson;
        }

        if (_freeJson.TryGetValue(name, out var json))
        {
            // Cache the result
            if (_cache != null)
            {
                _cache.Set(cacheKey, json, TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
            }
            
            return json;
        }

        return null;
    }

    /// <summary>
    /// Ensures the content system is initialized.
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            InitializeAsync().GetAwaiter().GetResult();
        }
    }
}