using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Background service that ensures the global tenant exists when multi-tenancy is disabled.
/// Runs during application startup to initialize the global tenant if needed.
/// </summary>
public class GlobalTenantInitializer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MultiTenancyOptions _options;
    private readonly ILogger<GlobalTenantInitializer> _logger;

    public GlobalTenantInitializer(
        IServiceProvider serviceProvider,
        IOptions<MultiTenancyOptions> options,
        ILogger<GlobalTenantInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Wait a moment for the application to fully start
            await Task.Delay(1000, stoppingToken);

            if (_options.AutoCreateGlobalTenant)
            {
                await EnsureGlobalTenantExistsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during global tenant initialization");
        }
    }

    private async Task EnsureGlobalTenantExistsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tenantRepository = scope.ServiceProvider.GetRequiredService<TenantRepository>();

        try
        {
            // Check if global tenant already exists
            var (existingGlobalTenant, _) = await tenantRepository.LoadAsync(_options.GlobalTenantId);
            
            if (existingGlobalTenant != null)
            {
                _logger.LogDebug("Global tenant {TenantId} already exists", _options.GlobalTenantId);
                
                // Ensure it's active
                if (!existingGlobalTenant.IsActive)
                {
                    existingGlobalTenant.IsActive = true;
                    existingGlobalTenant.UpdatedDate = DateTime.UtcNow;
                    await tenantRepository.UpdateAsync(existingGlobalTenant);
                    _logger.LogInformation("Activated global tenant {TenantId}", _options.GlobalTenantId);
                }
                
                return;
            }

            // Check if there's already a tenant with the global name
            var existingByName = await tenantRepository.GetByNameAsync(_options.GlobalTenantName);
            if (existingByName != null)
            {
                _logger.LogWarning(
                    "Tenant with name '{TenantName}' already exists with ID {ExistingId}, but global tenant ID is {GlobalId}",
                    _options.GlobalTenantName, existingByName.Id, _options.GlobalTenantId);
                
                // Update the existing tenant to use the global ID if needed
                if (existingByName.Id != _options.GlobalTenantId)
                {
                    _logger.LogInformation(
                        "Updating existing tenant '{TenantName}' from {OldId} to global ID {GlobalId}",
                        _options.GlobalTenantName, existingByName.Id, _options.GlobalTenantId);
                    
                    // This is a complex operation that might require data migration
                    // For Phase 1, we'll log a warning and continue
                    _logger.LogWarning(
                        "Manual intervention may be required to consolidate tenant data for global tenant");
                }
                
                return;
            }

            // Create the global tenant
            var globalTenant = new Tenant
            {
                Id = _options.GlobalTenantId,
                Name = _options.GlobalTenantName,
                DisplayName = _options.GlobalTenantDisplayName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Settings = new Dictionary<string, string>
                {
                    { "IsGlobalTenant", "true" },
                    { "CreatedBy", "GlobalTenantInitializer" },
                    { "MultiTenancyEnabled", _options.Enabled.ToString() }
                },
                Tags = new List<string> { "global", "system" }
            };

            await tenantRepository.CreateAsync(globalTenant);
            _logger.LogInformation(
                "Created global tenant {TenantId} with name '{TenantName}'", 
                _options.GlobalTenantId, _options.GlobalTenantName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to ensure global tenant {TenantId} exists", 
                _options.GlobalTenantId);
            throw;
        }
    }

    /// <summary>
    /// Public method to manually trigger global tenant initialization.
    /// Useful for testing or manual setup scenarios.
    /// </summary>
    public async Task InitializeGlobalTenantAsync(CancellationToken cancellationToken = default)
    {
        if (_options.AutoCreateGlobalTenant)
        {
            await EnsureGlobalTenantExistsAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Global tenant auto-creation is disabled");
        }
    }
}