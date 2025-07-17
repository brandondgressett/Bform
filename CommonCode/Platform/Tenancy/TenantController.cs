using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using BFormDomain.CommonCode.Platform.Authorization;
using System.Linq;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// API controller for managing tenants.
/// Provides CRUD operations for tenant management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all tenant operations
public class TenantController : ControllerBase
{
    private readonly TenantRepository _tenantRepository;
    private readonly TenantConnectionRepository _connectionRepository;
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ITenantContext _tenantContext;
    private readonly MultiTenancyOptions _options;
    private readonly ILogger<TenantController> _logger;
    private readonly TenantInitializationService _tenantInitializationService;
    private readonly TenantWorkflowService _tenantWorkflowService;

    public TenantController(
        TenantRepository tenantRepository,
        TenantConnectionRepository connectionRepository,
        ITenantConnectionProvider connectionProvider,
        ITenantContext tenantContext,
        IOptions<MultiTenancyOptions> options,
        ILogger<TenantController> logger,
        TenantInitializationService tenantInitializationService,
        TenantWorkflowService tenantWorkflowService)
    {
        _tenantRepository = tenantRepository;
        _connectionRepository = connectionRepository;
        _connectionProvider = connectionProvider;
        _tenantContext = tenantContext;
        _options = options.Value;
        _logger = logger;
        _tenantInitializationService = tenantInitializationService;
        _tenantWorkflowService = tenantWorkflowService;
    }

    /// <summary>
    /// Get all tenants (admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (tenants, _) = await _tenantRepository.GetAllAsync(t => true);
            var tenantDtos = tenants.Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                DisplayName = t.DisplayName,
                IsActive = t.IsActive,
                CreatedDate = t.CreatedDate,
                UpdatedDate = t.UpdatedDate ?? t.CreatedDate,
                Tags = t.Tags
                // Note: Sensitive settings are not exposed in DTOs
            });

            return Ok(tenantDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tenants");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get active tenants only
    /// </summary>
    [HttpGet("active")]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tenants = await _tenantRepository.GetActiveTenantsAsync(cancellationToken);
            var tenantDtos = tenants.Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                DisplayName = t.DisplayName,
                IsActive = t.IsActive,
                CreatedDate = t.CreatedDate,
                UpdatedDate = t.UpdatedDate ?? t.CreatedDate,
                Tags = t.Tags
            });

            return Ok(tenantDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active tenants");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TenantDto>> GetTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user has access to this tenant
            if (!_tenantContext.IsRootUser && !_tenantContext.HasAccessToTenant(id))
            {
                return Forbid("Access denied to this tenant");
            }

            var (tenant, _) = await _tenantRepository.LoadAsync(id);
            if (tenant == null)
            {
                return NotFound($"Tenant {id} not found");
            }

            var tenantDto = new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                IsActive = tenant.IsActive,
                CreatedDate = tenant.CreatedDate,
                UpdatedDate = tenant.UpdatedDate ?? tenant.CreatedDate,
                Tags = tenant.Tags
            };

            return Ok(tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant {TenantId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get tenant by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<TenantDto>> GetTenantByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _tenantRepository.GetByNameAsync(name);
            if (tenant == null)
            {
                return NotFound($"Tenant '{name}' not found");
            }

            var tenantDto = new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                IsActive = tenant.IsActive,
                CreatedDate = tenant.CreatedDate,
                UpdatedDate = tenant.UpdatedDate ?? tenant.CreatedDate,
                Tags = tenant.Tags
            };

            return Ok(tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant by name {TenantName}", name);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new tenant (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<TenantDto>> CreateTenantAsync([FromBody] CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Tenant name is required");
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return BadRequest("Tenant display name is required");
            }

            // Check if tenant name already exists
            var existingTenant = await _tenantRepository.GetByNameAsync(request.Name);
            if (existingTenant != null)
            {
                return Conflict($"Tenant with name '{request.Name}' already exists");
            }

            // Create new tenant
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayName = request.DisplayName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                ContentTemplateSetId = request.ContentTemplateSetId ?? Guid.Empty,
                Settings = request.Settings?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty) ?? new Dictionary<string, string>(),
                Tags = request.Tags ?? new List<string>()
            };

            await _tenantRepository.CreateAsync(tenant);

            _logger.LogInformation("Created tenant {TenantId} with name '{TenantName}'", tenant.Id, tenant.Name);
            
            // Initialize tenant with content from template set
            await _tenantInitializationService.InitializeTenantAsync(tenant, request.ContentTemplateSetId, cancellationToken);
            _logger.LogInformation("Initialized tenant {TenantId} with template set", tenant.Id);

            var tenantDto = new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                IsActive = tenant.IsActive,
                CreatedDate = tenant.CreatedDate,
                UpdatedDate = tenant.UpdatedDate ?? tenant.CreatedDate,
                Tags = tenant.Tags
            };

            return CreatedAtAction(nameof(GetTenantAsync), new { id = tenant.Id }, tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing tenant (admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<TenantDto>> UpdateTenantAsync(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var (tenant, _) = await _tenantRepository.LoadAsync(id);
            if (tenant == null)
            {
                return NotFound($"Tenant {id} not found");
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                tenant.DisplayName = request.DisplayName;
            }

            if (request.IsActive.HasValue)
            {
                tenant.IsActive = request.IsActive.Value;
            }

            if (request.ContentTemplateSetId.HasValue)
            {
                tenant.ContentTemplateSetId = request.ContentTemplateSetId.Value;
            }

            if (request.Settings != null)
            {
                // Merge settings
                foreach (var setting in request.Settings)
                {
                    tenant.Settings[setting.Key] = setting.Value?.ToString() ?? string.Empty;
                }
            }

            if (request.Tags != null)
            {
                tenant.Tags = request.Tags;
            }

            tenant.UpdatedDate = DateTime.UtcNow;

            await _tenantRepository.UpdateAsync(tenant);

            _logger.LogInformation("Updated tenant {TenantId}", id);

            var tenantDto = new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                IsActive = tenant.IsActive,
                CreatedDate = tenant.CreatedDate,
                UpdatedDate = tenant.UpdatedDate ?? tenant.CreatedDate,
                Tags = tenant.Tags
            };

            return Ok(tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Test tenant connections (admin only)
    /// </summary>
    [HttpPost("{id}/test-connections")]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<Dictionary<ConnectionType, bool>>> TestTenantConnectionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var (tenant, _) = await _tenantRepository.LoadAsync(id);
            if (tenant == null)
            {
                return NotFound($"Tenant {id} not found");
            }

            var results = await _connectionProvider.TestConnectionAsync(id, cancellationToken);

            _logger.LogInformation("Tested connections for tenant {TenantId}: {Results}", 
                id, string.Join(", ", results.Select(r => $"{r.Key}={r.Value}")));

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connections for tenant {TenantId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new tenant with complete workflow
    /// </summary>
    [HttpPost("create-workflow")]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<TenantCreationResult>> CreateTenantWorkflowAsync(
        [FromBody] TenantCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _tenantWorkflowService.CreateTenantAsync(request, cancellationToken);
        
        if (result.Success)
        {
            return CreatedAtAction(nameof(GetTenantAsync), new { id = result.TenantId }, result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Deactivate a tenant
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<TenantOperationResult>> DeactivateTenantAsync(
        Guid id,
        [FromBody] DeactivateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason))
        {
            return BadRequest("Deactivation reason is required");
        }

        var result = await _tenantWorkflowService.DeactivateTenantAsync(id, request.Reason, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Reactivate a tenant
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<TenantOperationResult>> ReactivateTenantAsync(
        Guid id,
        [FromBody] ReactivateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _tenantWorkflowService.ReactivateTenantAsync(
            id, 
            request?.ReactivateUsers ?? true, 
            cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Update tenant settings
    /// </summary>
    [HttpPut("{id}/settings")]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<TenantOperationResult>> UpdateTenantSettingsAsync(
        Guid id,
        [FromBody] Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        if (settings == null || !settings.Any())
        {
            return BadRequest("Settings are required");
        }

        var result = await _tenantWorkflowService.UpdateTenantSettingsAsync(id, settings, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get tenant diagnostics information
    /// </summary>
    [HttpGet("{id}/diagnostics")]
    [Authorize(Roles = "admin,root")]
    public async Task<ActionResult<TenantDiagnosticsDto>> GetTenantDiagnosticsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (tenant, _) = await _tenantRepository.LoadAsync(id);
            if (tenant == null)
            {
                return NotFound($"Tenant with ID {id} not found");
            }

            // Get connection test results
            var connectionTests = await _connectionProvider.TestConnectionAsync(id, cancellationToken);
            
            // Get user count
            var userRepo = HttpContext.RequestServices.GetRequiredService<UserRepository>();
            var (users, _) = await userRepo.GetAsync(predicate: u => u.TenantId == id);
            
            var diagnostics = new TenantDiagnosticsDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                IsActive = tenant.IsActive,
                CreatedDate = tenant.CreatedDate,
                DeactivatedDate = tenant.DeactivatedDate,
                ReactivatedDate = tenant.ReactivatedDate,
                DatabaseConnectionHealthy = connectionTests.GetValueOrDefault(ConnectionType.Database),
                StorageConnectionHealthy = connectionTests.GetValueOrDefault(ConnectionType.Storage),
                UserCount = users.Count(),
                ActiveUserCount = users.Count(u => u.IsActive),
                Settings = tenant.Settings
            };

            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diagnostics for tenant {TenantId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// DTO for tenant information (excludes sensitive data)
/// </summary>
public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Request model for creating a new tenant
/// </summary>
public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid? ContentTemplateSetId { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request model for updating an existing tenant
/// </summary>
public class UpdateTenantRequest
{
    public string? DisplayName { get; set; }
    public bool? IsActive { get; set; }
    public Guid? ContentTemplateSetId { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request model for deactivating a tenant
/// </summary>
public class DeactivateTenantRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request model for reactivating a tenant
/// </summary>
public class ReactivateTenantRequest
{
    public bool ReactivateUsers { get; set; } = true;
}

/// <summary>
/// DTO for tenant diagnostics information
/// </summary>
public class TenantDiagnosticsDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? DeactivatedDate { get; set; }
    public DateTime? ReactivatedDate { get; set; }
    public bool DatabaseConnectionHealthy { get; set; }
    public bool StorageConnectionHealthy { get; set; }
    public int UserCount { get; set; }
    public int ActiveUserCount { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
}
