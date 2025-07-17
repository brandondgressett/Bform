using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Forms;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.CommonCode.Platform.WorkItems;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.CommonCode.Repository;
using BFormDomain.Repository;
using BFormDomain.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Service responsible for initializing new tenants with content from template sets.
/// This includes copying forms, rules, workflows, and other content to the new tenant.
/// </summary>
public class TenantInitializationService
{
    private readonly ITenantAwareRepositoryFactory _repositoryFactory;
    private readonly ITenantContentRepositoryFactory _contentRepositoryFactory;
    private readonly ILogger<TenantInitializationService> _logger;
    private readonly FileApplicationPlatformContentOptions _contentOptions;
    private readonly IRepositoryFactory _coreRepositoryFactory;

    public TenantInitializationService(
        ITenantAwareRepositoryFactory repositoryFactory,
        ITenantContentRepositoryFactory contentRepositoryFactory,
        IRepositoryFactory coreRepositoryFactory,
        IOptions<FileApplicationPlatformContentOptions> contentOptions,
        ILogger<TenantInitializationService> logger)
    {
        _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        _contentRepositoryFactory = contentRepositoryFactory ?? throw new ArgumentNullException(nameof(contentRepositoryFactory));
        _coreRepositoryFactory = coreRepositoryFactory ?? throw new ArgumentNullException(nameof(coreRepositoryFactory));
        _contentOptions = contentOptions?.Value ?? throw new ArgumentNullException(nameof(contentOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new tenant with content from the specified template set.
    /// </summary>
    /// <param name="tenant">The tenant to initialize</param>
    /// <param name="templateSetId">The ID of the template set to use (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task InitializeTenantAsync(
        Tenant tenant,
        Guid? templateSetId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing tenant {TenantId} ({TenantName}) with template set {TemplateSetId}",
            tenant.Id, tenant.Name, templateSetId);

        try
        {
            // Get the template set to use
            ContentTemplateSet? templateSet = null;
            if (templateSetId.HasValue)
            {
                var templateSetRepo = _coreRepositoryFactory.CreateRepository<ContentTemplateSet>();
                var (loadedTemplateSet, _) = await templateSetRepo.LoadAsync(templateSetId.Value);
                templateSet = loadedTemplateSet;
            }

            if (templateSet == null)
            {
                // Try to find default template set
                var templateSetRepo = _coreRepositoryFactory.CreateRepository<ContentTemplateSet>();
                var (defaultSets, _) = await templateSetRepo.GetAsync(0, 1, ts => ts.IsDefault && ts.IsActive);
                templateSet = defaultSets.FirstOrDefault();
            }

            if (templateSet == null)
            {
                _logger.LogWarning("No template set found for tenant {TenantId}. Tenant will be initialized empty.",
                    tenant.Id);
                return;
            }

            _logger.LogInformation("Using template set '{TemplateSetName}' for tenant {TenantId}",
                templateSet.Name, tenant.Id);

            // Update tenant with the template set ID
            tenant.ContentTemplateSetId = templateSet.Id;

            // Copy content from template folder to tenant folder
            await CopyTemplateContentAsync(templateSet, tenant.Id, cancellationToken);

            // Initialize tenant-specific repositories with seed data if needed
            await InitializeTenantRepositoriesAsync(tenant.Id, templateSet, cancellationToken);

            _logger.LogInformation("Successfully initialized tenant {TenantId} with {FormCount} forms, " +
                "{RuleCount} rules, {WorkItemCount} work items from template set '{TemplateSetName}'",
                tenant.Id, templateSet.Manifest.FormTemplates,
                templateSet.Manifest.Rules, templateSet.Manifest.WorkItemTemplates,
                templateSet.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tenant {TenantId} with template set {TemplateSetId}",
                tenant.Id, templateSetId);
            throw;
        }
    }

    /// <summary>
    /// Copies template content files from the template set folder to the tenant folder.
    /// </summary>
    private async Task CopyTemplateContentAsync(
        ContentTemplateSet templateSet,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var sourceFolder = Path.Combine(_contentOptions.BaseFolder, "templates", templateSet.ContentPath);
        var targetFolder = Path.Combine(_contentOptions.BaseFolder, "tenants", tenantId.ToString());

        if (!Directory.Exists(sourceFolder))
        {
            _logger.LogWarning("Template content folder not found: {SourceFolder}", sourceFolder);
            return;
        }

        _logger.LogDebug("Copying template content from {SourceFolder} to {TargetFolder}",
            sourceFolder, targetFolder);

        // Ensure target directory exists
        Directory.CreateDirectory(targetFolder);

        // Copy all content files
        await CopyDirectoryAsync(sourceFolder, targetFolder, cancellationToken);
    }

    /// <summary>
    /// Recursively copies a directory and its contents.
    /// </summary>
    private async Task CopyDirectoryAsync(
        string sourceDir,
        string targetDir,
        CancellationToken cancellationToken)
    {
        // Create all directories
        foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(dirPath.Replace(sourceDir, targetDir));
        }

        // Copy all files
        foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var targetPath = filePath.Replace(sourceDir, targetDir);
            await CopyFileAsync(filePath, targetPath, cancellationToken);
        }
    }

    /// <summary>
    /// Copies a single file asynchronously.
    /// </summary>
    private async Task CopyFileAsync(
        string sourceFile,
        string targetFile,
        CancellationToken cancellationToken)
    {
        using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
        using var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
        await sourceStream.CopyToAsync(targetStream, cancellationToken);
    }

    /// <summary>
    /// Initializes tenant-specific database collections with any seed data.
    /// </summary>
    private async Task InitializeTenantRepositoriesAsync(
        Guid tenantId,
        ContentTemplateSet templateSet,
        CancellationToken cancellationToken)
    {
        // This method can be extended to create initial database records
        // For now, content is handled through the file-based content system
        
        _logger.LogDebug("Initializing repositories for tenant {TenantId}", tenantId);
        
        // Force content repository initialization to load the copied files
        var contentRepo = _contentRepositoryFactory.GetTenantContentRepository(tenantId);
        
        // The content repository will automatically load content from the tenant folder
        // when first accessed, so we don't need to do anything else here
    }

    /// <summary>
    /// Removes all content for a tenant (used for cleanup or re-initialization).
    /// </summary>
    public async Task RemoveTenantContentAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Removing all content for tenant {TenantId}", tenantId);

        try
        {
            // Remove tenant content folder
            var tenantFolder = Path.Combine(_contentOptions.BaseFolder, "tenants", tenantId.ToString());
            if (Directory.Exists(tenantFolder))
            {
                Directory.Delete(tenantFolder, recursive: true);
                _logger.LogInformation("Removed content folder for tenant {TenantId}", tenantId);
            }

            // Clear cached content repository
            _contentRepositoryFactory.ClearTenantCache(tenantId);

            // Note: Database records should be handled by the repository layer
            // with proper cascading deletes
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove content for tenant {TenantId}", tenantId);
            throw;
        }
    }
}