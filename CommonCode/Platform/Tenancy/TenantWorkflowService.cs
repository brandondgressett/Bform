using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Orchestrates complex tenant management workflows including creation, 
/// deactivation, reactivation, and deletion processes.
/// </summary>
public class TenantWorkflowService
{
    private readonly TenantRepository _tenantRepo;
    private readonly TenantConnectionRepository _connectionRepo;
    private readonly TenantInitializationService _initService;
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly UserRepository _userRepo;
    private readonly RoleRepository _roleRepo;
    private readonly ILogger<TenantWorkflowService> _logger;
    private readonly MultiTenancyOptions _options;

    public TenantWorkflowService(
        TenantRepository tenantRepo,
        TenantConnectionRepository connectionRepo,
        TenantInitializationService initService,
        ITenantConnectionProvider connectionProvider,
        UserRepository userRepo,
        RoleRepository roleRepo,
        IOptions<MultiTenancyOptions> options,
        ILogger<TenantWorkflowService> logger)
    {
        _tenantRepo = tenantRepo ?? throw new ArgumentNullException(nameof(tenantRepo));
        _connectionRepo = connectionRepo ?? throw new ArgumentNullException(nameof(connectionRepo));
        _initService = initService ?? throw new ArgumentNullException(nameof(initService));
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new tenant with complete initialization workflow.
    /// </summary>
    public async Task<TenantCreationResult> CreateTenantAsync(
        TenantCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting tenant creation workflow for '{TenantName}'", request.Name);

        try
        {
            // Validate the request
            var validationErrors = ValidateCreationRequest(request);
            if (validationErrors.Any())
            {
                return new TenantCreationResult
                {
                    Success = false,
                    Errors = validationErrors
                };
            }

            // Check if tenant name already exists
            var existingTenant = await _tenantRepo.GetByNameAsync(request.Name, cancellationToken);
            if (existingTenant != null)
            {
                return new TenantCreationResult
                {
                    Success = false,
                    Errors = new[] { $"Tenant with name '{request.Name}' already exists" }
                };
            }

            // Create the tenant entity
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name.ToLowerInvariant(),
                DisplayName = request.DisplayName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                Settings = request.Settings ?? new Dictionary<string, string>(),
                Tags = request.Tags ?? new List<string>()
            };

            if (request.TemplateSetId.HasValue)
            {
                tenant.ContentTemplateSetId = request.TemplateSetId.Value;
            }

            // Save the tenant
            await _tenantRepo.CreateAsync(tenant);
            _logger.LogInformation("Created tenant entity {TenantId} for '{TenantName}'", tenant.Id, tenant.Name);

            // Create database connection if provided
            if (request.DatabaseConnection != null)
            {
                var dbConnection = new TenantConnection
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Type = ConnectionType.Database,
                    Provider = request.DatabaseConnection.Provider,
                    DatabaseName = request.DatabaseConnection.DatabaseName ?? $"tenant_{tenant.Id:N}",
                    AdditionalSettings = request.DatabaseConnection.AdditionalSettings ?? new Dictionary<string, string>()
                };

                // Encrypt and store connection string
                if (!string.IsNullOrEmpty(request.DatabaseConnection.ConnectionString))
                {
                    if (_connectionProvider is AzureKeyVaultConnectionProvider kvProvider)
                    {
                        await kvProvider.SaveConnectionAsync(
                            tenant.Id, 
                            ConnectionType.Database, 
                            request.DatabaseConnection.ConnectionString, 
                            cancellationToken);
                    }
                    else
                    {
                        // For local provider, encrypt the connection string
                        var encryptionKey = _options.LocalEncryptionKey ?? ConnectionStringEncryption.GenerateKey();
                        dbConnection.EncryptConnectionString(encryptionKey, request.DatabaseConnection.ConnectionString);
                    }
                }

                await _connectionRepo.CreateAsync(dbConnection);
                _logger.LogInformation("Created database connection for tenant {TenantId}", tenant.Id);
            }

            // Create storage connection if provided
            if (request.StorageConnection != null)
            {
                var storageConnection = new TenantConnection
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Type = ConnectionType.Storage,
                    Provider = request.StorageConnection.Provider,
                    ContainerPrefix = request.StorageConnection.ContainerPrefix ?? $"tenant_{tenant.Id:N}",
                    AdditionalSettings = request.StorageConnection.AdditionalSettings ?? new Dictionary<string, string>()
                };

                // Encrypt and store connection string if provided
                if (!string.IsNullOrEmpty(request.StorageConnection.ConnectionString))
                {
                    if (_connectionProvider is AzureKeyVaultConnectionProvider kvProvider)
                    {
                        await kvProvider.SaveConnectionAsync(
                            tenant.Id, 
                            ConnectionType.Storage, 
                            request.StorageConnection.ConnectionString, 
                            cancellationToken);
                    }
                    else
                    {
                        // For local provider, encrypt the connection string
                        var encryptionKey = _options.LocalEncryptionKey ?? ConnectionStringEncryption.GenerateKey();
                        storageConnection.EncryptConnectionString(encryptionKey, request.StorageConnection.ConnectionString);
                    }
                }

                await _connectionRepo.CreateAsync(storageConnection);
                _logger.LogInformation("Created storage connection for tenant {TenantId}", tenant.Id);
            }

            // Initialize tenant content from template
            await _initService.InitializeTenantAsync(tenant, request.TemplateSetId, cancellationToken);
            _logger.LogInformation("Initialized content for tenant {TenantId}", tenant.Id);

            // Create initial admin user if requested
            ApplicationUser? adminUser = null;
            if (request.CreateAdminUser && request.AdminUser != null)
            {
                adminUser = await CreateTenantAdminUserAsync(tenant.Id, request.AdminUser, cancellationToken);
                _logger.LogInformation("Created admin user {UserId} for tenant {TenantId}", adminUser.Id, tenant.Id);
            }

            // Test connections if requested
            Dictionary<ConnectionType, bool>? connectionTests = null;
            if (request.TestConnections)
            {
                connectionTests = await _connectionProvider.TestConnectionAsync(tenant.Id, cancellationToken);
                _logger.LogInformation("Connection test results for tenant {TenantId}: Database={DbResult}, Storage={StorageResult}",
                    tenant.Id, 
                    connectionTests.GetValueOrDefault(ConnectionType.Database),
                    connectionTests.GetValueOrDefault(ConnectionType.Storage));
            }

            return new TenantCreationResult
            {
                Success = true,
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                AdminUserId = adminUser?.Id,
                ConnectionTestResults = connectionTests
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tenant '{TenantName}'", request.Name);
            return new TenantCreationResult
            {
                Success = false,
                Errors = new[] { $"Tenant creation failed: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Deactivates a tenant, preventing access while preserving data.
    /// </summary>
    public async Task<TenantOperationResult> DeactivateTenantAsync(
        Guid tenantId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Starting tenant deactivation for {TenantId}. Reason: {Reason}", tenantId, reason);

        try
        {
            var (tenant, _) = await _tenantRepo.LoadAsync(tenantId);
            if (tenant == null)
            {
                return new TenantOperationResult
                {
                    Success = false,
                    Error = "Tenant not found"
                };
            }

            if (!tenant.IsActive)
            {
                return new TenantOperationResult
                {
                    Success = false,
                    Error = "Tenant is already deactivated"
                };
            }

            // Update tenant status
            tenant.IsActive = false;
            tenant.DeactivatedDate = DateTime.UtcNow;
            tenant.DeactivationReason = reason;
            tenant.UpdatedDate = DateTime.UtcNow;

            await _tenantRepo.UpdateAsync(tenant);

            // Deactivate all users for this tenant
            await DeactivateTenantUsersAsync(tenantId, cancellationToken);

            _logger.LogWarning("Successfully deactivated tenant {TenantId}", tenantId);

            return new TenantOperationResult
            {
                Success = true,
                Message = $"Tenant '{tenant.Name}' has been deactivated"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate tenant {TenantId}", tenantId);
            return new TenantOperationResult
            {
                Success = false,
                Error = $"Deactivation failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Reactivates a previously deactivated tenant.
    /// </summary>
    public async Task<TenantOperationResult> ReactivateTenantAsync(
        Guid tenantId,
        bool reactivateUsers = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting tenant reactivation for {TenantId}", tenantId);

        try
        {
            var (tenant, _) = await _tenantRepo.LoadAsync(tenantId);
            if (tenant == null)
            {
                return new TenantOperationResult
                {
                    Success = false,
                    Error = "Tenant not found"
                };
            }

            if (tenant.IsActive)
            {
                return new TenantOperationResult
                {
                    Success = false,
                    Error = "Tenant is already active"
                };
            }

            // Test connections before reactivation
            var connectionTests = await _connectionProvider.TestConnectionAsync(tenantId, cancellationToken);
            if (connectionTests.Any(ct => !ct.Value))
            {
                var failedConnections = string.Join(", ", connectionTests.Where(ct => !ct.Value).Select(ct => ct.Key));
                return new TenantOperationResult
                {
                    Success = false,
                    Error = $"Cannot reactivate tenant. Failed connection tests: {failedConnections}"
                };
            }

            // Update tenant status
            tenant.IsActive = true;
            tenant.DeactivatedDate = null;
            tenant.DeactivationReason = null;
            tenant.ReactivatedDate = DateTime.UtcNow;
            tenant.UpdatedDate = DateTime.UtcNow;

            await _tenantRepo.UpdateAsync(tenant);

            // Optionally reactivate users
            if (reactivateUsers)
            {
                await ReactivateTenantUsersAsync(tenantId, cancellationToken);
            }

            _logger.LogInformation("Successfully reactivated tenant {TenantId}", tenantId);

            return new TenantOperationResult
            {
                Success = true,
                Message = $"Tenant '{tenant.Name}' has been reactivated"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reactivate tenant {TenantId}", tenantId);
            return new TenantOperationResult
            {
                Success = false,
                Error = $"Reactivation failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Updates tenant settings and metadata.
    /// </summary>
    public async Task<TenantOperationResult> UpdateTenantSettingsAsync(
        Guid tenantId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating settings for tenant {TenantId}", tenantId);

        try
        {
            var (tenant, _) = await _tenantRepo.LoadAsync(tenantId);
            if (tenant == null)
            {
                return new TenantOperationResult
                {
                    Success = false,
                    Error = "Tenant not found"
                };
            }

            // Merge settings
            foreach (var setting in settings)
            {
                tenant.Settings[setting.Key] = setting.Value;
            }

            tenant.UpdatedDate = DateTime.UtcNow;
            await _tenantRepo.UpdateAsync(tenant);

            _logger.LogInformation("Successfully updated settings for tenant {TenantId}", tenantId);

            return new TenantOperationResult
            {
                Success = true,
                Message = "Tenant settings updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings for tenant {TenantId}", tenantId);
            return new TenantOperationResult
            {
                Success = false,
                Error = $"Settings update failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Creates an admin user for a specific tenant.
    /// </summary>
    private async Task<ApplicationUser> CreateTenantAdminUserAsync(
        Guid tenantId,
        AdminUserCreationRequest request,
        CancellationToken cancellationToken)
    {
        // Create the user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = request.Username,
            Email = request.Email,
            EmailConfirmed = request.EmailConfirmed,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            CreatedOn = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        // Set password hash if provided
        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        // Create the user
        await _userRepo.CreateAsync(user);

        // Assign admin role
        var adminRole = await GetOrCreateTenantAdminRoleAsync(tenantId, cancellationToken);
        user.Roles.Add(adminRole.Id);
        await _userRepo.UpdateAsync(user);

        return user;
    }

    /// <summary>
    /// Gets or creates the tenant admin role.
    /// </summary>
    private async Task<ApplicationRole> GetOrCreateTenantAdminRoleAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        const string adminRoleName = "TenantAdmin";
        
        var (existingRoles, _) = await _roleRepo.GetAsync(
            predicate: r => r.Name == adminRoleName && r.TenantId == tenantId);
        
        if (existingRoles.Any())
        {
            return existingRoles.First();
        }

        // Create the admin role
        var adminRole = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = adminRoleName,
            Description = "Tenant Administrator",
            Claims = new List<string>
            {
                "tenant.manage",
                "users.manage",
                "roles.manage",
                "content.manage",
                "settings.manage"
            }
        };

        await _roleRepo.CreateAsync(adminRole);
        return adminRole;
    }

    /// <summary>
    /// Deactivates all users for a tenant.
    /// </summary>
    private async Task DeactivateTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var (users, _) = await _userRepo.GetAsync(predicate: u => u.TenantId == tenantId);
        
        foreach (var user in users.Where(u => u.IsActive))
        {
            user.IsActive = false;
            user.DeactivatedOn = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);
        }

        _logger.LogInformation("Deactivated {UserCount} users for tenant {TenantId}", users.Count(), tenantId);
    }

    /// <summary>
    /// Reactivates users for a tenant.
    /// </summary>
    private async Task ReactivateTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var (users, _) = await _userRepo.GetAsync(predicate: u => u.TenantId == tenantId);
        
        foreach (var user in users.Where(u => !u.IsActive && u.DeactivatedOn.HasValue))
        {
            user.IsActive = true;
            user.DeactivatedOn = null;
            await _userRepo.UpdateAsync(user);
        }

        _logger.LogInformation("Reactivated {UserCount} users for tenant {TenantId}", 
            users.Count(u => !u.IsActive), tenantId);
    }

    /// <summary>
    /// Validates a tenant creation request.
    /// </summary>
    private List<string> ValidateCreationRequest(TenantCreationRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Tenant name is required");
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Name, @"^[a-z0-9-]+$"))
        {
            errors.Add("Tenant name must contain only lowercase letters, numbers, and hyphens");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors.Add("Display name is required");
        }

        if (request.CreateAdminUser && request.AdminUser == null)
        {
            errors.Add("Admin user details are required when creating an admin user");
        }

        if (request.AdminUser != null)
        {
            if (string.IsNullOrWhiteSpace(request.AdminUser.Username))
            {
                errors.Add("Admin username is required");
            }
            if (string.IsNullOrWhiteSpace(request.AdminUser.Email))
            {
                errors.Add("Admin email is required");
            }
            if (string.IsNullOrWhiteSpace(request.AdminUser.Password))
            {
                errors.Add("Admin password is required");
            }
        }

        return errors;
    }
}

/// <summary>
/// Request model for creating a new tenant.
/// </summary>
public class TenantCreationRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid? TemplateSetId { get; set; }
    public Dictionary<string, string>? Settings { get; set; }
    public List<string>? Tags { get; set; }
    public bool CreateAdminUser { get; set; } = true;
    public AdminUserCreationRequest? AdminUser { get; set; }
    public ConnectionCreationRequest? DatabaseConnection { get; set; }
    public ConnectionCreationRequest? StorageConnection { get; set; }
    public bool TestConnections { get; set; } = true;
}

/// <summary>
/// Request model for creating an admin user.
/// </summary>
public class AdminUserCreationRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; } = false;
}

/// <summary>
/// Request model for creating a connection.
/// </summary>
public class ConnectionCreationRequest
{
    public string Provider { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
    public string? DatabaseName { get; set; }
    public string? ContainerPrefix { get; set; }
    public Dictionary<string, string>? AdditionalSettings { get; set; }
}

/// <summary>
/// Result model for tenant creation.
/// </summary>
public class TenantCreationResult
{
    public bool Success { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid? AdminUserId { get; set; }
    public Dictionary<ConnectionType, bool>? ConnectionTestResults { get; set; }
    public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();
}

/// <summary>
/// Result model for tenant operations.
/// </summary>
public class TenantOperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}