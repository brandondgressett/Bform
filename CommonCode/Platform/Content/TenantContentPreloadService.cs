using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Platform.Content;

/// <summary>
/// Background service that preloads tenant content on application startup.
/// This improves performance by warming up the content cache for all active tenants.
/// </summary>
public class TenantContentPreloadService : BackgroundService
{
    private readonly IApplicationPlatformContent _content;
    private readonly ILogger<TenantContentPreloadService> _logger;

    public TenantContentPreloadService(
        IApplicationPlatformContent content,
        ILogger<TenantContentPreloadService> logger)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting tenant content preload service");

            // Only preload if we have a tenant-aware content implementation
            if (_content is TenantAwareApplicationPlatformContent tenantAwareContent)
            {
                await tenantAwareContent.PreloadAllTenantContentAsync(stoppingToken);
                _logger.LogInformation("Tenant content preload completed successfully");
            }
            else
            {
                _logger.LogInformation("Content implementation is not tenant-aware, skipping preload");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload tenant content");
            // Don't throw - we don't want to prevent app startup if preload fails
        }
    }
}