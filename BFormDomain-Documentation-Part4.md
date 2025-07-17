# BFormDomain Comprehensive Documentation - Part 4

## Tags System

The Tags System provides a flexible categorization mechanism for all entities in BFormDomain. It supports hierarchical tagging, tag counting for analytics, and efficient tag-based querying across entity types. Tags enable cross-cutting concerns like search, filtering, and dynamic grouping.

### Core Components

#### ITaggable Interface
The fundamental interface implemented by all taggable entities:

```csharp
public interface ITaggable
{
    List<string> Tags { get; }
}
```

#### Tagger Service
Central service for managing tags across all entity types:

```csharp
public class Tagger
{
    private readonly IRepository<TagCountsDataModel> _repo;
    private readonly IApplicationAlert _alerts;
    
    // Find entities by tags
    public async Task<IEnumerable<Guid>> IdsFromTags<T>(
        IEnumerable<string> allTags,
        IRepository<T> entityRepo,
        Guid? host = null)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        using (PerfTrack.Stopwatch(nameof(IdsFromTags)))
        {
            var lallTags = TagUtil.MakeTags(allTags).ToArray();
            List<T> matches;
            
            if (host is null)
            {
                (matches, _) = await entityRepo.GetAllAsync(
                    it => lallTags.All(at => it.Tags.Contains(at)));
            }
            else
            {
                var hostWorkSet = host.Value;
                (matches, _) = await entityRepo.GetAllAsync(
                    it => it.HostWorkSet == hostWorkSet && 
                          lallTags.All(at => it.Tags.Contains(at)));
            }
            
            return matches.Select(it => it.Id);
        }
    }
    
    // Tag an entity
    public async Task Tag<T>(
        T item,
        IRepository<T> entityRepo,
        IEnumerable<string> tags)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        using var trx = await entityRepo.OpenTransactionAsync();
        
        try
        {
            bool changed = await TagAndCount(item, true, entityRepo, tags, trx, 1);
            
            if (changed)
                await trx.CommitAsync();
            else
                await trx.AbortAsync();
        }
        catch (Exception ex)
        {
            try { await trx.AbortAsync(); } catch { }
            _alerts.RaiseAlert(ApplicationAlertKind.Services, LogLevel.Warning,
                $"Cannot tag entity. {ex.TraceInformation()}", 1);
        }
    }
    
    // Remove tags from entity
    public async Task Untag<T>(
        T item,
        IRepository<T> entityRepo,
        IEnumerable<string> tags)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        using var trx = await entityRepo.OpenTransactionAsync();
        
        try
        {
            bool changed = await TagAndCount(item, false, entityRepo, tags, trx, -1);
            
            if (changed)
                await trx.CommitAsync();
            else
                await trx.AbortAsync();
        }
        catch (Exception ex)
        {
            try { await trx.AbortAsync(); } catch { }
            _alerts.RaiseAlert(ApplicationAlertKind.Services, LogLevel.Warning,
                $"Cannot untag entity. {ex.TraceInformation()}", 1);
        }
    }
    
    // Reconcile tags - add new, remove old
    public async Task<bool> ReconcileTags<T>(
        ITransactionContext trx,
        T item,
        IEnumerable<string> setToTags)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        try
        {
            var existingTags = item.Tags;
            var readyTags = setToTags.Select(tg => TagUtil.MakeTag(tg));
            var newTags = readyTags.Where(st => !existingTags.Contains(st));
            var removedTags = existingTags.Where(et => !readyTags.Contains(et));
            
            if (newTags.Any() || removedTags.Any())
            {
                item.Tags.AddRange(newTags);
                item.Tags.RemoveAll(tg => removedTags.Contains(tg));
                
                await CountTags(item, true, newTags, trx, 1);
                await CountTags(item, false, removedTags, trx, -1);
            }
            
            return newTags.Any() || removedTags.Any();
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services, LogLevel.Warning,
                $"Cannot reconcile tags. {ex.TraceInformation()}", 1);
            throw;
        }
    }
    
    // Track tag usage counts
    private async Task CountTags<T>(
        T item,
        ITransactionContext trx,
        int amount,
        string addTag)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        using (PerfTrack.Stopwatch("Count Tags"))
        {
            var existingCountResult = await _repo.GetOneAsync(
                trx,
                it => it.Tag == addTag &&
                      it.EntityType == item.EntityType &&
                      it.TemplateType == item.Template);
            
            var (existingCount, context) = existingCountResult;
            
            if (existingCount is null)
            {
                TagCountsDataModel newTag = new()
                {
                    Id = Guid.NewGuid(),
                    Count = 1,
                    EntityType = item.EntityType,
                    TemplateType = item.Template,
                    Tag = addTag,
                    Version = 0
                };
                
                await _repo.CreateAsync(trx, newTag);
            }
            else
            {
                await _repo.IncrementOneByIdAsync(
                    trx,
                    existingCount.Id,
                    it => it.Count,
                    amount);
            }
        }
    }
}
```

#### TagCountsDataModel
Tracks tag usage statistics across entity types:

```csharp
public class TagCountsDataModel : IDataModel
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    public string Tag { get; set; } = "";                  // The tag value
    public string? EntityType { get; set; } = null;        // Entity type (Form, WorkItem, etc.)
    public string? TemplateType { get; set; } = null;      // Template name
    public int Count { get; set; } = 0;                    // Usage count
}
```

#### TagUtil
Utility functions for tag normalization and processing:

```csharp
public static class TagUtil
{
    // Normalize a single tag
    public static string MakeTag(string tag)
    {
        return tag.ToLowerInvariant()
                  .Trim()
                  .Replace(' ', '-')
                  .Replace('_', '-');
    }
    
    // Normalize a collection of tags
    public static IEnumerable<string> MakeTags(IEnumerable<string> tags)
    {
        return tags.Select(MakeTag)
                   .Where(t => !string.IsNullOrWhiteSpace(t))
                   .Distinct();
    }
    
    // Parse comma-separated tags
    public static List<string> ParseTags(string tagString)
    {
        if (string.IsNullOrWhiteSpace(tagString))
            return new List<string>();
        
        return tagString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(MakeTag)
                        .Distinct()
                        .ToList();
    }
    
    // Check if entity has all specified tags
    public static bool HasAllTags(ITaggable entity, params string[] tags)
    {
        var normalizedTags = MakeTags(tags);
        return normalizedTags.All(t => entity.Tags.Contains(t));
    }
    
    // Check if entity has any of the specified tags
    public static bool HasAnyTags(ITaggable entity, params string[] tags)
    {
        var normalizedTags = MakeTags(tags);
        return normalizedTags.Any(t => entity.Tags.Contains(t));
    }
}
```

### Tag Hierarchies and Conventions

#### Hierarchical Tag Structure
Tags can represent hierarchies using dot notation:

```csharp
// Category hierarchy
"category.sales"
"category.sales.leads"
"category.sales.opportunities"

// Status hierarchy  
"status.active"
"status.inactive"
"status.archived"

// Priority hierarchy
"priority.high"
"priority.medium"
"priority.low"

// Department hierarchy
"dept.engineering"
"dept.engineering.frontend"
"dept.engineering.backend"
```

#### System Tags
Reserved tags used by the platform:

```csharp
public static class SystemTags
{
    // Entity lifecycle
    public const string Draft = "system.draft";
    public const string Published = "system.published";
    public const string Archived = "system.archived";
    
    // Processing status
    public const string Processing = "system.processing";
    public const string Completed = "system.completed";
    public const string Failed = "system.failed";
    
    // Visibility
    public const string Public = "system.public";
    public const string Private = "system.private";
    public const string Internal = "system.internal";
    
    // Data quality
    public const string Validated = "system.validated";
    public const string Unvalidated = "system.unvalidated";
    public const string Duplicate = "system.duplicate";
}
```

### Usage Examples

#### Basic Tagging Operations
```csharp
public class TaggingExamples
{
    private readonly Tagger _tagger;
    private readonly IRepository<WorkItem> _workItemRepo;
    
    // Add tags to an entity
    public async Task TagWorkItem(Guid workItemId, params string[] tags)
    {
        var (workItem, _) = await _workItemRepo.LoadAsync(workItemId);
        
        await _tagger.Tag(workItem, _workItemRepo, tags);
        
        // Tags are normalized: "High Priority" becomes "high-priority"
    }
    
    // Remove tags from an entity
    public async Task UntagWorkItem(Guid workItemId, params string[] tags)
    {
        var (workItem, _) = await _workItemRepo.LoadAsync(workItemId);
        
        await _tagger.Untag(workItem, _workItemRepo, tags);
    }
    
    // Update all tags on an entity
    public async Task UpdateWorkItemTags(Guid workItemId, string[] newTags)
    {
        var (workItem, ctx) = await _workItemRepo.LoadAsync(workItemId);
        
        using var trx = await _workItemRepo.OpenTransactionAsync();
        
        // This will add missing tags and remove extra tags
        var changed = await _tagger.ReconcileTags(trx, workItem, newTags);
        
        if (changed)
        {
            await _workItemRepo.UpdateAsync(trx, (workItem, ctx));
            await trx.CommitAsync();
        }
    }
}
```

#### Tag-Based Queries
```csharp
public class TagQueryExamples
{
    private readonly Tagger _tagger;
    private readonly IRepository<FormInstance> _formRepo;
    
    // Find all entities with specific tags
    public async Task<List<FormInstance>> FindFormsByTags(params string[] requiredTags)
    {
        var formIds = await _tagger.IdsFromTags(requiredTags, _formRepo);
        
        var forms = new List<FormInstance>();
        foreach (var id in formIds)
        {
            var (form, _) = await _formRepo.LoadAsync(id);
            if (form != null)
                forms.Add(form);
        }
        
        return forms;
    }
    
    // Find entities with tags in a specific workset
    public async Task<List<FormInstance>> FindFormsInWorkSetByTags(
        Guid workSetId,
        params string[] requiredTags)
    {
        var formIds = await _tagger.IdsFromTags(
            requiredTags,
            _formRepo,
            host: workSetId);
        
        // Load and return forms...
    }
    
    // Complex tag queries
    public async Task<List<WorkItem>> FindUrgentUnassignedWorkItems(Guid workSetId)
    {
        // Find by multiple tags
        var urgentIds = await _tagger.IdsFromTags(
            new[] { "priority.high", "status.unassigned" },
            _workItemRepo,
            host: workSetId);
        
        // Additional filtering if needed
        var workItems = new List<WorkItem>();
        foreach (var id in urgentIds)
        {
            var (item, _) = await _workItemRepo.LoadAsync(id);
            if (item != null && item.UserAssignee == null)
                workItems.Add(item);
        }
        
        return workItems;
    }
}
```

#### Tag Analytics
```csharp
public class TagAnalytics
{
    private readonly IRepository<TagCountsDataModel> _tagCountsRepo;
    
    // Get most popular tags
    public async Task<List<TagStatistics>> GetPopularTags(
        string? entityType = null,
        int top = 10)
    {
        var query = _tagCountsRepo.GetQueryable();
        
        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(t => t.EntityType == entityType);
        
        var popularTags = await query
            .OrderByDescending(t => t.Count)
            .Take(top)
            .ToListAsync();
        
        return popularTags.Select(t => new TagStatistics
        {
            Tag = t.Tag,
            Count = t.Count,
            EntityType = t.EntityType,
            TemplateType = t.TemplateType
        }).ToList();
    }
    
    // Get tag usage by entity type
    public async Task<Dictionary<string, List<string>>> GetTagsByEntityType()
    {
        var (allTags, _) = await _tagCountsRepo.GetAllAsync();
        
        return allTags
            .GroupBy(t => t.EntityType ?? "Unknown")
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => t.Tag).Distinct().ToList());
    }
    
    // Find related tags (tags that appear together)
    public async Task<List<string>> FindRelatedTags(
        string tag,
        IRepository<WorkItem> repo,
        int maxResults = 10)
    {
        var normalizedTag = TagUtil.MakeTag(tag);
        
        // Find all entities with this tag
        var entityIds = await _tagger.IdsFromTags(new[] { normalizedTag }, repo);
        
        // Collect all tags from these entities
        var relatedTags = new Dictionary<string, int>();
        foreach (var id in entityIds)
        {
            var (entity, _) = await repo.LoadAsync(id);
            if (entity != null)
            {
                foreach (var entityTag in entity.Tags)
                {
                    if (entityTag != normalizedTag)
                    {
                        relatedTags[entityTag] = relatedTags.GetValueOrDefault(entityTag) + 1;
                    }
                }
            }
        }
        
        // Return most common related tags
        return relatedTags
            .OrderByDescending(kvp => kvp.Value)
            .Take(maxResults)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
```

#### Rule Actions for Tagging
```csharp
public class RuleActionTagForm : IRuleAction
{
    private readonly Tagger _tagger;
    private readonly IRepository<FormInstance> _formRepo;
    
    public async Task<object> ExecuteAsync(
        Dictionary<string, object?> args,
        AppEvent appEvent,
        RuleEvaluationContext context)
    {
        var formId = Guid.Parse(args["FormId"]?.ToString() ?? 
            throw new ArgumentException("FormId required"));
        var tags = args["Tags"] as string[] ?? 
            throw new ArgumentException("Tags required");
        
        var (form, _) = await _formRepo.LoadAsync(formId);
        if (form == null)
            throw new InvalidOperationException($"Form {formId} not found");
        
        await _tagger.Tag(form, _formRepo, tags);
        
        return new { tagged = true, tags = tags };
    }
}

public class RuleActionUntagForm : IRuleAction
{
    private readonly Tagger _tagger;
    private readonly IRepository<FormInstance> _formRepo;
    
    public async Task<object> ExecuteAsync(
        Dictionary<string, object?> args,
        AppEvent appEvent,
        RuleEvaluationContext context)
    {
        var formId = Guid.Parse(args["FormId"]?.ToString() ?? 
            throw new ArgumentException("FormId required"));
        var tags = args["Tags"] as string[] ?? 
            throw new ArgumentException("Tags required");
        
        var (form, _) = await _formRepo.LoadAsync(formId);
        if (form == null)
            throw new InvalidOperationException($"Form {formId} not found");
        
        await _tagger.Untag(form, _formRepo, tags);
        
        return new { untagged = true, tags = tags };
    }
}
```

### Tag-Based Features

#### Auto-Tagging
Automatically apply tags based on entity properties:

```csharp
public class AutoTaggingService
{
    private readonly Tagger _tagger;
    
    public async Task AutoTagWorkItem(
        WorkItem workItem,
        IRepository<WorkItem> repo)
    {
        var autoTags = new List<string>();
        
        // Priority-based tags
        autoTags.Add($"priority.{GetPriorityName(workItem.Priority)}");
        
        // Status-based tags
        autoTags.Add($"status.{GetStatusName(workItem.Status)}");
        
        // Assignment tags
        if (workItem.UserAssignee.HasValue)
            autoTags.Add("assigned");
        else
            autoTags.Add("unassigned");
        
        // Date-based tags
        var age = DateTime.UtcNow - workItem.CreatedDate;
        if (age.TotalDays > 30)
            autoTags.Add("aged.old");
        else if (age.TotalDays > 7)
            autoTags.Add("aged.week");
        else
            autoTags.Add("aged.new");
        
        await _tagger.Tag(workItem, repo, autoTags);
    }
}
```

#### Tag Inheritance
Propagate tags from parent to child entities:

```csharp
public class TagInheritanceService
{
    private readonly Tagger _tagger;
    
    public async Task InheritTagsFromWorkSet<T>(
        T entity,
        IRepository<T> entityRepo,
        IRepository<WorkSet> workSetRepo)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        if (!entity.HostWorkSet.HasValue)
            return;
        
        var (workSet, _) = await workSetRepo.LoadAsync(entity.HostWorkSet.Value);
        if (workSet == null)
            return;
        
        // Inherit organizational tags
        var inheritableTags = workSet.Tags
            .Where(t => t.StartsWith("org.") || t.StartsWith("project."))
            .ToList();
        
        if (inheritableTags.Any())
        {
            await _tagger.Tag(entity, entityRepo, inheritableTags);
        }
    }
}
```

### Best Practices

1. **Use consistent tag hierarchies** - Establish naming conventions early
2. **Normalize tags automatically** - Always use TagUtil for consistency
3. **Limit tag proliferation** - Use controlled vocabularies where possible
4. **Track tag usage** - Monitor TagCountsDataModel for cleanup opportunities
5. **Use system tags appropriately** - Reserve system.* namespace
6. **Index tag queries** - Ensure MongoDB indexes on Tags array
7. **Batch tag operations** - Use transactions for multiple tag changes
8. **Document tag meanings** - Maintain a tag dictionary
9. **Consider tag permissions** - Some tags may need access control
10. **Plan for tag migration** - Tags evolve, plan for updates

---

## ApplicationTopology System

The ApplicationTopology System manages distributed server topology, role assignment, and load balancing across multiple application servers. It ensures high availability by dynamically assigning server roles based on server health and load patterns.

### Core Components

#### ApplicationServerRecord
Represents a server instance in the topology:

```csharp
public class ApplicationServerRecord : IDataModel
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    public string ServerName { get; set; } = "";           // Machine name
    public DateTime LastPingTime { get; set; }             // Last heartbeat
    public List<string> ServerRoles { get; set; } = new(); // Assigned roles
}
```

#### ApplicationServerMonitor
Background service that monitors server health and manages role distribution:

```csharp
public class ApplicationServerMonitor : BackgroundService
{
    private const string ApplicationServerMonitorRole = "application monitor";
    
    private readonly List<IServerRoleSpecifier> _serverRoles;
    private readonly IRepository<ApplicationServerRecord> _repo;
    private readonly string _serverName;
    private readonly ApplicationTopologyCatalog _catalog;
    
    public ApplicationServerMonitor(
        IEnumerable<IServerRoleSpecifier> serverRoles,
        IRepository<ApplicationServerRecord> repo,
        ApplicationTopologyCatalog catalog,
        IApplicationAlert alerts)
    {
        _serverRoles = serverRoles.ToList();
        _repo = repo;
        _serverName = Environment.MachineName;
        _catalog = catalog;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Update heartbeat for current server
                var (me, rc) = await _repo.GetOneAsync(it => it.ServerName == _serverName);
                if (me is null)
                {
                    await SetupCurrentServer();
                    continue;
                }
                
                me.LastPingTime = DateTime.UtcNow;
                await _repo.UpdateAsync((me, rc));
                _catalog.RefreshServerRoles(me.ServerRoles);
                
                // Get all active servers (pinged within last minute)
                var (runningServers, _) = await _repo.GetAllAsync(
                    it => it.LastPingTime > DateTime.UtcNow.AddMinutes(-1.0));
                
                if (runningServers.Count <= 1)
                {
                    // Single server - take all roles
                    await AssignAllRolesToSingleServer(me, rc);
                }
                else if (runningServers.Any())
                {
                    // Multiple servers - distribute roles
                    await DistributeRolesAcrossServers(runningServers, me, rc);
                }
                
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, 
                    LogLevel.Information, ex.TraceInformation(), 4);
            }
        }
    }
    
    private async Task DistributeRolesAcrossServers(
        List<ApplicationServerRecord> runningServers,
        ApplicationServerRecord currentServer,
        RepositoryContext context)
    {
        // Ensure one server is the monitor
        var monitor = runningServers
            .OrderBy(it => it.ServerName)
            .FirstOrDefault(server => server.ServerRoles.Contains(ApplicationServerMonitorRole));
        
        if (monitor is null)
        {
            currentServer.ServerRoles.Add(ApplicationServerMonitorRole);
            await _repo.UpdateIgnoreVersionAsync((currentServer, context));
            return;
        }
        
        // Only the monitor distributes roles
        if (monitor.ServerName != _serverName)
        {
            await Task.Delay(5000);
            return;
        }
        
        // Distribute unassigned roles
        foreach (var role in _serverRoles)
        {
            if (runningServers.Any(server => server.ServerRoles.Contains(role.RoleName)))
                continue;
            
            var maxLoaded = runningServers.MaxBy(it => it.ServerRoles.Count);
            var minLoaded = runningServers.MinBy(it => it.ServerRoles.Count);
            
            switch (role.RoleBalance)
            {
                case ServerRoleBalance.StackOnWorkhorse:
                    maxLoaded!.ServerRoles.Add(role.RoleName);
                    await _repo.UpdateAsync((maxLoaded, context));
                    break;
                    
                case ServerRoleBalance.Balance:
                    minLoaded!.ServerRoles.Add(role.RoleName);
                    await _repo.UpdateAsync((minLoaded, context));
                    break;
            }
        }
    }
}
```

#### IServerRoleSpecifier
Interface for defining server roles:

```csharp
public interface IServerRoleSpecifier
{
    string RoleName { get; }                    // Unique role identifier
    ServerRoleBalance RoleBalance { get; }      // Load balancing strategy
    bool IsEligible();                          // Check if server can take role
}

public enum ServerRoleBalance
{
    Balance = 0,            // Distribute to least loaded server
    StackOnWorkhorse = 1    // Assign to most loaded server
}
```

#### ApplicationTopologyCatalog
Central registry of server topology:

```csharp
public class ApplicationTopologyCatalog
{
    private readonly string _serverName;
    private HashSet<string> _myRoles = new();
    private readonly object _lock = new();
    
    public ApplicationTopologyCatalog()
    {
        _serverName = Environment.MachineName;
    }
    
    public void RefreshServerRoles(List<string> roles)
    {
        lock (_lock)
        {
            _myRoles = new HashSet<string>(roles);
        }
    }
    
    public bool HasRole(string roleName)
    {
        lock (_lock)
        {
            return _myRoles.Contains(roleName);
        }
    }
    
    public List<string> GetMyRoles()
    {
        lock (_lock)
        {
            return _myRoles.ToList();
        }
    }
    
    public string ServerName => _serverName;
}
```

### Server Role Examples

#### EventPumpRoleSpecifier
Determines which server processes the event queue:

```csharp
public class EventPumpRoleSpecifier : IServerRoleSpecifier
{
    public string RoleName => "EventPump";
    
    public ServerRoleBalance RoleBalance => ServerRoleBalance.Balance;
    
    public bool IsEligible()
    {
        // Check if server has necessary resources
        return HasSufficientMemory() && HasDatabaseAccess();
    }
    
    private bool HasSufficientMemory()
    {
        var availableMemory = GC.GetTotalMemory(false);
        return availableMemory < 1_000_000_000; // Less than 1GB used
    }
    
    private bool HasDatabaseAccess()
    {
        // Check database connectivity
        return true; // Simplified
    }
}
```

#### NotificationServiceRoleSpecifier
Assigns notification processing responsibility:

```csharp
public class NotificationServiceRoleSpecifier : IServerRoleSpecifier
{
    public string RoleName => "NotificationService";
    
    public ServerRoleBalance RoleBalance => ServerRoleBalance.StackOnWorkhorse;
    
    public bool IsEligible()
    {
        // Notification service can run on any server
        return true;
    }
}
```

#### SchedulerRoleSpecifier
Manages scheduled job execution:

```csharp
public class SchedulerRoleSpecifier : IServerRoleSpecifier
{
    public string RoleName => "Scheduler";
    
    public ServerRoleBalance RoleBalance => ServerRoleBalance.Balance;
    
    public bool IsEligible()
    {
        // Scheduler requires accurate time sync
        return IsTimeSync() && HasDatabaseAccess();
    }
    
    private bool IsTimeSync()
    {
        // Check NTP sync status
        return true; // Simplified
    }
}
```

### Usage Examples

#### Role-Aware Service Implementation
```csharp
public class RoleAwareEventPump : BackgroundService
{
    private readonly ApplicationTopologyCatalog _topology;
    private readonly ILogger<RoleAwareEventPump> _logger;
    
    public RoleAwareEventPump(
        ApplicationTopologyCatalog topology,
        ILogger<RoleAwareEventPump> logger)
    {
        _topology = topology;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_topology.HasRole("EventPump"))
            {
                _logger.LogDebug("EventPump role not assigned to this server");
                await Task.Delay(5000, stoppingToken);
                continue;
            }
            
            _logger.LogInformation("Processing events on server: {Server}", 
                _topology.ServerName);
            
            // Process events...
            await ProcessEvents(stoppingToken);
            
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

#### Dynamic Role Registration
```csharp
public class DynamicRoleService
{
    private readonly IServiceCollection _services;
    
    public void RegisterServerRoles(IServiceCollection services)
    {
        // Core roles
        services.AddSingleton<IServerRoleSpecifier, EventPumpRoleSpecifier>();
        services.AddSingleton<IServerRoleSpecifier, NotificationServiceRoleSpecifier>();
        services.AddSingleton<IServerRoleSpecifier, SchedulerRoleSpecifier>();
        
        // Feature-specific roles
        if (IsFeatureEnabled("Reporting"))
        {
            services.AddSingleton<IServerRoleSpecifier, ReportGeneratorRoleSpecifier>();
        }
        
        if (IsFeatureEnabled("DataSync"))
        {
            services.AddSingleton<IServerRoleSpecifier, DataSyncRoleSpecifier>();
        }
        
        // Register topology services
        services.AddSingleton<ApplicationTopologyCatalog>();
        services.AddHostedService<ApplicationServerMonitor>();
    }
}
```

#### Health Check Integration
```csharp
public class TopologyHealthCheck : IHealthCheck
{
    private readonly IRepository<ApplicationServerRecord> _repo;
    private readonly ApplicationTopologyCatalog _catalog;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if current server is registered
            var serverName = Environment.MachineName;
            var (server, _) = await _repo.GetOneAsync(
                it => it.ServerName == serverName,
                cancellationToken);
            
            if (server == null)
            {
                return HealthCheckResult.Unhealthy("Server not registered in topology");
            }
            
            // Check heartbeat recency
            var timeSinceLastPing = DateTime.UtcNow - server.LastPingTime;
            if (timeSinceLastPing > TimeSpan.FromMinutes(2))
            {
                return HealthCheckResult.Unhealthy(
                    $"Server heartbeat is stale: {timeSinceLastPing}");
            }
            
            // Check role assignments
            var roles = _catalog.GetMyRoles();
            if (!roles.Any())
            {
                return HealthCheckResult.Degraded("No roles assigned to server");
            }
            
            // Get cluster status
            var (activeServers, _) = await _repo.GetAllAsync(
                it => it.LastPingTime > DateTime.UtcNow.AddMinutes(-1),
                cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["ServerName"] = serverName,
                ["AssignedRoles"] = roles,
                ["ActiveServers"] = activeServers.Count,
                ["LastHeartbeat"] = server.LastPingTime
            };
            
            return HealthCheckResult.Healthy("Topology is healthy", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Topology check failed", ex);
        }
    }
}
```

#### Monitoring and Metrics
```csharp
public class TopologyMetricsService
{
    private readonly IRepository<ApplicationServerRecord> _repo;
    private readonly IMetrics _metrics;
    
    public async Task CollectMetrics()
    {
        var (servers, _) = await _repo.GetAllAsync();
        
        // Server count metrics
        _metrics.Measure.Gauge.SetValue("topology.servers.total", servers.Count);
        _metrics.Measure.Gauge.SetValue("topology.servers.active", 
            servers.Count(s => s.LastPingTime > DateTime.UtcNow.AddMinutes(-1)));
        
        // Role distribution metrics
        var roleDistribution = servers
            .SelectMany(s => s.ServerRoles)
            .GroupBy(r => r)
            .ToDictionary(g => g.Key, g => g.Count());
        
        foreach (var (role, count) in roleDistribution)
        {
            _metrics.Measure.Gauge.SetValue($"topology.role.{role}", count);
        }
        
        // Load balance metrics
        var loadDistribution = servers
            .Where(s => s.LastPingTime > DateTime.UtcNow.AddMinutes(-1))
            .Select(s => s.ServerRoles.Count);
        
        if (loadDistribution.Any())
        {
            _metrics.Measure.Gauge.SetValue("topology.load.min", loadDistribution.Min());
            _metrics.Measure.Gauge.SetValue("topology.load.max", loadDistribution.Max());
            _metrics.Measure.Gauge.SetValue("topology.load.avg", loadDistribution.Average());
        }
    }
}
```

### Advanced Topology Features

#### Role Priority and Dependencies
```csharp
public abstract class PrioritizedServerRole : IServerRoleSpecifier
{
    public abstract string RoleName { get; }
    public abstract ServerRoleBalance RoleBalance { get; }
    public abstract int Priority { get; }
    public abstract List<string> Dependencies { get; }
    
    public virtual bool IsEligible()
    {
        return true;
    }
}

public class DatabaseMaintenanceRole : PrioritizedServerRole
{
    public override string RoleName => "DatabaseMaintenance";
    public override ServerRoleBalance RoleBalance => ServerRoleBalance.Balance;
    public override int Priority => 100; // High priority
    public override List<string> Dependencies => new(); // No dependencies
}

public class ReportGeneratorRole : PrioritizedServerRole
{
    public override string RoleName => "ReportGenerator";
    public override ServerRoleBalance RoleBalance => ServerRoleBalance.StackOnWorkhorse;
    public override int Priority => 50; // Medium priority
    public override List<string> Dependencies => new() { "DatabaseMaintenance" };
}
```

#### Graceful Role Migration
```csharp
public class GracefulRoleMigration
{
    private readonly ApplicationTopologyCatalog _topology;
    private readonly ILogger<GracefulRoleMigration> _logger;
    
    public async Task MigrateRole(string roleName, string targetServer)
    {
        _logger.LogInformation("Initiating role migration: {Role} to {Server}", 
            roleName, targetServer);
        
        // Phase 1: Mark role for migration
        await MarkRoleForMigration(roleName);
        
        // Phase 2: Wait for current operations to complete
        await WaitForGracefulShutdown(roleName);
        
        // Phase 3: Transfer role
        await TransferRole(roleName, targetServer);
        
        // Phase 4: Verify migration
        await VerifyMigration(roleName, targetServer);
        
        _logger.LogInformation("Role migration completed: {Role} to {Server}", 
            roleName, targetServer);
    }
}
```

### Best Practices

1. **Implement health checks** - Monitor server and role health
2. **Use appropriate balance strategies** - Choose between Balance and StackOnWorkhorse
3. **Handle role transitions gracefully** - Implement proper shutdown procedures
4. **Monitor role distribution** - Track metrics for optimization
5. **Plan for server failures** - Ensure roles redistribute automatically
6. **Use role dependencies** - Define prerequisites for complex roles
7. **Implement role-specific eligibility** - Check resources before assignment
8. **Log role changes** - Maintain audit trail of topology changes
9. **Test failover scenarios** - Verify behavior during server outages
10. **Document role responsibilities** - Clear documentation for each role

---

## Terminology System

The Terminology System provides configurable business terminology and labels throughout the application. It enables customization of UI text, field labels, and business terms without code changes, supporting multi-tenant scenarios with different terminology needs.

### Core Components

#### IApplicationTerms Interface
The main interface for accessing application terminology:

```csharp
public interface IApplicationTerms
{
    string Get(TermKey key);
    string Get(string key);
    string GetFormat(TermKey key, params object[] args);
    string GetFormat(string key, params object[] args);
    Dictionary<string, string> GetAll();
    void Reload();
}
```

#### TermKey Enumeration
Defines all available terminology keys:

```csharp
public enum TermKey
{
    // Entity terms
    WorkItem = 1,
    WorkItems = 2,
    WorkSet = 3,
    WorkSets = 4,
    Form = 5,
    Forms = 6,
    Report = 7,
    Reports = 8,
    
    // Status terms
    StatusOpen = 10,
    StatusInProgress = 11,
    StatusResolved = 12,
    StatusClosed = 13,
    StatusCancelled = 14,
    
    // Priority terms
    PriorityLow = 20,
    PriorityMedium = 21,
    PriorityHigh = 22,
    PriorityCritical = 23,
    
    // Action terms
    Create = 30,
    Edit = 31,
    Delete = 32,
    Save = 33,
    Cancel = 34,
    Submit = 35,
    Approve = 36,
    Reject = 37,
    Assign = 38,
    Unassign = 39,
    
    // Field labels
    Title = 40,
    Description = 41,
    CreatedDate = 42,
    UpdatedDate = 43,
    CreatedBy = 44,
    UpdatedBy = 45,
    AssignedTo = 46,
    DueDate = 47,
    
    // Messages
    SaveSuccessful = 50,
    SaveFailed = 51,
    DeleteConfirmation = 52,
    ValidationError = 53,
    RequiredField = 54,
    InvalidFormat = 55,
    
    // Business-specific terms
    Customer = 60,
    Customers = 61,
    Vendor = 62,
    Vendors = 63,
    Invoice = 64,
    Invoices = 65,
    Order = 66,
    Orders = 67,
    Product = 68,
    Products = 69
}
```

#### DefaultTerminology
Provides default English terminology:

```csharp
public class DefaultTerminology : IApplicationTerms
{
    private readonly Dictionary<string, string> _terms;
    
    public DefaultTerminology()
    {
        _terms = new Dictionary<string, string>
        {
            // Entity terms
            [TermKey.WorkItem.ToString()] = "Work Item",
            [TermKey.WorkItems.ToString()] = "Work Items",
            [TermKey.WorkSet.ToString()] = "Dashboard",
            [TermKey.WorkSets.ToString()] = "Dashboards",
            [TermKey.Form.ToString()] = "Form",
            [TermKey.Forms.ToString()] = "Forms",
            
            // Status terms
            [TermKey.StatusOpen.ToString()] = "Open",
            [TermKey.StatusInProgress.ToString()] = "In Progress",
            [TermKey.StatusResolved.ToString()] = "Resolved",
            [TermKey.StatusClosed.ToString()] = "Closed",
            
            // Priority terms
            [TermKey.PriorityLow.ToString()] = "Low",
            [TermKey.PriorityMedium.ToString()] = "Medium",
            [TermKey.PriorityHigh.ToString()] = "High",
            [TermKey.PriorityCritical.ToString()] = "Critical",
            
            // Action terms
            [TermKey.Create.ToString()] = "Create",
            [TermKey.Edit.ToString()] = "Edit",
            [TermKey.Delete.ToString()] = "Delete",
            [TermKey.Save.ToString()] = "Save",
            [TermKey.Submit.ToString()] = "Submit",
            
            // Messages
            [TermKey.SaveSuccessful.ToString()] = "Save successful",
            [TermKey.SaveFailed.ToString()] = "Save failed: {0}",
            [TermKey.DeleteConfirmation.ToString()] = "Are you sure you want to delete this {0}?",
            [TermKey.RequiredField.ToString()] = "{0} is required"
        };
    }
    
    public string Get(TermKey key)
    {
        return Get(key.ToString());
    }
    
    public string Get(string key)
    {
        return _terms.TryGetValue(key, out var value) ? value : key;
    }
    
    public string GetFormat(TermKey key, params object[] args)
    {
        return GetFormat(key.ToString(), args);
    }
    
    public string GetFormat(string key, params object[] args)
    {
        var template = Get(key);
        return string.Format(template, args);
    }
    
    public Dictionary<string, string> GetAll()
    {
        return new Dictionary<string, string>(_terms);
    }
    
    public void Reload()
    {
        // No-op for default terminology
    }
}
```

#### ApplicationTermsFile
File-based terminology for customization:

```csharp
public class ApplicationTermsFile : IApplicationTerms
{
    private readonly FileApplicationTermsOptions _options;
    private readonly ILogger<ApplicationTermsFile> _logger;
    private Dictionary<string, string> _terms;
    private readonly FileSystemWatcher _watcher;
    private readonly IApplicationTerms _fallback;
    
    public ApplicationTermsFile(
        IOptions<FileApplicationTermsOptions> options,
        ILogger<ApplicationTermsFile> logger)
    {
        _options = options.Value;
        _logger = logger;
        _fallback = new DefaultTerminology();
        _terms = new Dictionary<string, string>();
        
        LoadTerms();
        
        if (_options.WatchForChanges)
        {
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(_options.FilePath)!)
            {
                Filter = Path.GetFileName(_options.FilePath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            _watcher.Changed += OnFileChanged;
            _watcher.EnableRaisingEvents = true;
        }
    }
    
    private void LoadTerms()
    {
        try
        {
            if (File.Exists(_options.FilePath))
            {
                var json = File.ReadAllText(_options.FilePath);
                _terms = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) 
                         ?? new Dictionary<string, string>();
                
                _logger.LogInformation($"Loaded {_terms.Count} terms from {_options.FilePath}");
            }
            else
            {
                _logger.LogWarning($"Terms file not found: {_options.FilePath}");
                _terms = new Dictionary<string, string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load terms file");
            _terms = new Dictionary<string, string>();
        }
    }
    
    public string Get(TermKey key)
    {
        return Get(key.ToString());
    }
    
    public string Get(string key)
    {
        if (_terms.TryGetValue(key, out var value))
            return value;
        
        // Fall back to default terminology
        return _fallback.Get(key);
    }
    
    public string GetFormat(TermKey key, params object[] args)
    {
        return GetFormat(key.ToString(), args);
    }
    
    public string GetFormat(string key, params object[] args)
    {
        var template = Get(key);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, $"Invalid format string for key: {key}");
            return template;
        }
    }
    
    public Dictionary<string, string> GetAll()
    {
        var allTerms = _fallback.GetAll();
        
        // Override with custom terms
        foreach (var kvp in _terms)
        {
            allTerms[kvp.Key] = kvp.Value;
        }
        
        return allTerms;
    }
    
    public void Reload()
    {
        LoadTerms();
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Terms file changed, reloading...");
        Thread.Sleep(100); // Brief delay to ensure file write is complete
        Reload();
    }
}

public class FileApplicationTermsOptions
{
    public string FilePath { get; set; } = "terminology.json";
    public bool WatchForChanges { get; set; } = true;
}
```

### Terminology Customization

#### Custom Terminology Files
Example terminology.json for different industries:

```json
// Healthcare terminology
{
  "WorkItem": "Case",
  "WorkItems": "Cases",
  "WorkSet": "Department",
  "WorkSets": "Departments",
  "Customer": "Patient",
  "Customers": "Patients",
  "Form": "Medical Record",
  "Forms": "Medical Records",
  "StatusOpen": "New",
  "StatusInProgress": "Under Review",
  "StatusResolved": "Diagnosed",
  "StatusClosed": "Discharged"
}

// Legal terminology
{
  "WorkItem": "Case",
  "WorkItems": "Cases",
  "WorkSet": "Practice Area",
  "WorkSets": "Practice Areas",
  "Customer": "Client",
  "Customers": "Clients",
  "Form": "Document",
  "Forms": "Documents",
  "StatusOpen": "Active",
  "StatusInProgress": "In Litigation",
  "StatusResolved": "Settled",
  "StatusClosed": "Closed"
}

// Manufacturing terminology
{
  "WorkItem": "Work Order",
  "WorkItems": "Work Orders",
  "WorkSet": "Production Line",
  "WorkSets": "Production Lines",
  "Customer": "Distributor",
  "Customers": "Distributors",
  "Form": "Quality Report",
  "Forms": "Quality Reports",
  "StatusOpen": "Scheduled",
  "StatusInProgress": "In Production",
  "StatusResolved": "Completed",
  "StatusClosed": "Shipped"
}
```

### Usage Examples

#### Basic Terminology Usage
```csharp
public class TerminologyUsageExamples
{
    private readonly IApplicationTerms _terms;
    
    // Get simple terms
    public string GetWorkItemLabel()
    {
        return _terms.Get(TermKey.WorkItem); // "Work Item" or custom
    }
    
    // Get formatted messages
    public string GetSaveFailedMessage(string error)
    {
        return _terms.GetFormat(TermKey.SaveFailed, error);
        // Returns: "Save failed: {error message}"
    }
    
    // Get delete confirmation
    public string GetDeleteConfirmation(string entityType)
    {
        var entityLabel = _terms.Get(entityType);
        return _terms.GetFormat(TermKey.DeleteConfirmation, entityLabel);
        // Returns: "Are you sure you want to delete this Work Item?"
    }
    
    // Build UI labels
    public Dictionary<string, string> GetFormLabels()
    {
        return new Dictionary<string, string>
        {
            ["title"] = _terms.Get(TermKey.Title),
            ["description"] = _terms.Get(TermKey.Description),
            ["assignedTo"] = _terms.Get(TermKey.AssignedTo),
            ["dueDate"] = _terms.Get(TermKey.DueDate),
            ["priority"] = _terms.Get("Priority"),
            ["status"] = _terms.Get("Status")
        };
    }
}
```

#### Dynamic UI Generation
```csharp
public class DynamicUIService
{
    private readonly IApplicationTerms _terms;
    
    public object GenerateWorkItemFormConfig()
    {
        return new
        {
            title = _terms.Get(TermKey.WorkItem),
            fields = new[]
            {
                new
                {
                    name = "title",
                    label = _terms.Get(TermKey.Title),
                    required = true,
                    placeholder = _terms.GetFormat("EnterFieldValue", _terms.Get(TermKey.Title))
                },
                new
                {
                    name = "status",
                    label = _terms.Get("Status"),
                    type = "select",
                    options = new[]
                    {
                        new { value = 0, label = _terms.Get(TermKey.StatusOpen) },
                        new { value = 1, label = _terms.Get(TermKey.StatusInProgress) },
                        new { value = 2, label = _terms.Get(TermKey.StatusResolved) },
                        new { value = 3, label = _terms.Get(TermKey.StatusClosed) }
                    }
                },
                new
                {
                    name = "priority",
                    label = _terms.Get("Priority"),
                    type = "select",
                    options = new[]
                    {
                        new { value = 0, label = _terms.Get(TermKey.PriorityLow) },
                        new { value = 1, label = _terms.Get(TermKey.PriorityMedium) },
                        new { value = 2, label = _terms.Get(TermKey.PriorityHigh) },
                        new { value = 3, label = _terms.Get(TermKey.PriorityCritical) }
                    }
                }
            },
            actions = new[]
            {
                new { name = "save", label = _terms.Get(TermKey.Save) },
                new { name = "cancel", label = _terms.Get(TermKey.Cancel) }
            }
        };
    }
}
```

#### Multi-Tenant Terminology
```csharp
public class TenantTerminologyService
{
    private readonly Dictionary<string, IApplicationTerms> _tenantTerms;
    
    public TenantTerminologyService()
    {
        _tenantTerms = new Dictionary<string, IApplicationTerms>();
    }
    
    public void LoadTenantTerminology(string tenantId, string termsFilePath)
    {
        var options = Options.Create(new FileApplicationTermsOptions
        {
            FilePath = termsFilePath,
            WatchForChanges = true
        });
        
        var logger = new NullLogger<ApplicationTermsFile>();
        _tenantTerms[tenantId] = new ApplicationTermsFile(options, logger);
    }
    
    public IApplicationTerms GetTermsForTenant(string tenantId)
    {
        return _tenantTerms.TryGetValue(tenantId, out var terms)
            ? terms
            : new DefaultTerminology();
    }
}
```

#### Terminology in Email Templates
```csharp
public class EmailTemplateService
{
    private readonly IApplicationTerms _terms;
    
    public string GenerateWorkItemNotificationEmail(WorkItem workItem)
    {
        var template = @"
            <h2>{{WorkItemLabel}} Update</h2>
            <p>The following {{WorkItemLabel}} has been updated:</p>
            <ul>
                <li><strong>{{TitleLabel}}:</strong> {{Title}}</li>
                <li><strong>{{StatusLabel}}:</strong> {{Status}}</li>
                <li><strong>{{PriorityLabel}}:</strong> {{Priority}}</li>
                <li><strong>{{AssignedToLabel}}:</strong> {{AssignedTo}}</li>
            </ul>
        ";
        
        return template
            .Replace("{{WorkItemLabel}}", _terms.Get(TermKey.WorkItem))
            .Replace("{{TitleLabel}}", _terms.Get(TermKey.Title))
            .Replace("{{StatusLabel}}", _terms.Get("Status"))
            .Replace("{{PriorityLabel}}", _terms.Get("Priority"))
            .Replace("{{AssignedToLabel}}", _terms.Get(TermKey.AssignedTo))
            .Replace("{{Title}}", workItem.Title)
            .Replace("{{Status}}", _terms.Get($"Status{GetStatusName(workItem.Status)}"))
            .Replace("{{Priority}}", _terms.Get($"Priority{GetPriorityName(workItem.Priority)}"))
            .Replace("{{AssignedTo}}", GetUserName(workItem.UserAssignee));
    }
}
```

### Best Practices

1. **Define all terms upfront** - Plan terminology needs early
2. **Use consistent key naming** - Follow TermKey pattern
3. **Provide meaningful defaults** - Always have fallback values
4. **Support formatting** - Use GetFormat for dynamic messages
5. **Watch for file changes** - Enable hot reload in development
6. **Cache terminology** - Avoid repeated file reads
7. **Version terminology files** - Track changes over time
8. **Document custom terms** - Maintain a glossary
9. **Test with different terminologies** - Ensure UI flexibility
10. **Consider localization** - Terminology system can support i18n

---

## ApplicationTopology System (continued)

### Load Balancing Strategies

The ApplicationTopology system supports different load balancing strategies:

```csharp
public enum ServerRoleBalance
{
    Balance,           // Distribute roles evenly across servers
    StackOnWorkhorse  // Concentrate roles on fewer servers
}
```

#### Example: Custom Load-Balanced Service

```csharp
public class DataProcessingService : IServerRoleSpecifier
{
    public string RoleName => "data-processor";
    public ServerRoleBalance RoleBalance => ServerRoleBalance.Balance;
    
    private readonly ApplicationTopologyCatalog _catalog;
    private readonly IRepository<WorkItem> _workItemRepo;
    
    public async Task ProcessDataAsync()
    {
        // Only process if this server has the role
        if (!_catalog.DoIHaveRole(RoleName))
            return;
            
        // Process data on this server
        var items = await _workItemRepo.GetAllAsync(
            w => w.Status == "pending-processing");
            
        foreach (var item in items)
        {
            await ProcessWorkItem(item);
        }
    }
}
```

#### Example: Monitoring and Health Checks

```csharp
public class HealthCheckService : BackgroundService
{
    private readonly ApplicationTopologyCatalog _catalog;
    private readonly IRepository<ApplicationServerRecord> _serverRepo;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var servers = await _serverRepo.GetAllAsync(
                s => s.LastPingTime > DateTime.UtcNow.AddMinutes(-2));
                
            foreach (var server in servers)
            {
                var health = new ServerHealth
                {
                    ServerName = server.ServerName,
                    Roles = server.ServerRoles,
                    LastSeen = server.LastPingTime,
                    RoleCount = server.ServerRoles.Count,
                    IsHealthy = (DateTime.UtcNow - server.LastPingTime).TotalMinutes < 2
                };
                
                await PublishHealthMetric(health);
            }
            
            await Task.Delay(30000, stoppingToken);
        }
    }
}
```

### ApplicationTopology Usage Examples

#### Multi-Server Deployment Configuration

```csharp
// Startup.cs configuration for multi-server deployment
public void ConfigureServices(IServiceCollection services)
{
    // Register server roles
    services.AddSingleton<IServerRoleSpecifier, ReportGeneratorRole>();
    services.AddSingleton<IServerRoleSpecifier, NotificationProcessorRole>();
    services.AddSingleton<IServerRoleSpecifier, SchedulerRole>();
    services.AddSingleton<IServerRoleSpecifier, FileProcessorRole>();
    
    // Register topology services
    services.AddSingleton<ApplicationTopologyCatalog>();
    services.AddHostedService<ApplicationServerMonitor>();
    
    // Register role-aware services
    services.AddScoped<IReportService>(provider =>
    {
        var catalog = provider.GetRequiredService<ApplicationTopologyCatalog>();
        if (catalog.DoIHaveRole("report-generator"))
            return new LocalReportService(provider);
        else
            return new RemoteReportServiceProxy(provider);
    });
}
```

#### Dynamic Role Assignment Based on Load

```csharp
public class LoadAwareRoleAssigner : IServerRoleSpecifier
{
    public string RoleName => "heavy-processor";
    public ServerRoleBalance RoleBalance => ServerRoleBalance.Balance;
    
    private readonly ISystemMetrics _metrics;
    
    public async Task<bool> ShouldAssignRole(ApplicationServerRecord server)
    {
        var cpuUsage = await _metrics.GetCpuUsage(server.ServerName);
        var memoryUsage = await _metrics.GetMemoryUsage(server.ServerName);
        
        // Only assign heavy processing roles to servers with available resources
        return cpuUsage < 60 && memoryUsage < 70;
    }
}
```

#### Failover and High Availability

```csharp
public class FailoverCoordinator : BackgroundService
{
    private readonly IRepository<ApplicationServerRecord> _serverRepo;
    private readonly ApplicationTopologyCatalog _catalog;
    private readonly ILogger<FailoverCoordinator> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var (servers, _) = await _serverRepo.GetAllAsync();
            var deadServers = servers.Where(s => 
                (DateTime.UtcNow - s.LastPingTime).TotalMinutes > 2).ToList();
                
            foreach (var deadServer in deadServers)
            {
                _logger.LogWarning($"Server {deadServer.ServerName} is not responding");
                
                // Reassign critical roles from dead server
                foreach (var role in deadServer.ServerRoles)
                {
                    var activeServers = servers
                        .Where(s => s.LastPingTime > DateTime.UtcNow.AddMinutes(-1))
                        .OrderBy(s => s.ServerRoles.Count)
                        .ToList();
                        
                    if (activeServers.Any())
                    {
                        var targetServer = activeServers.First();
                        if (!targetServer.ServerRoles.Contains(role))
                        {
                            targetServer.ServerRoles.Add(role);
                            await _serverRepo.UpdateAsync((targetServer, 0));
                            
                            _logger.LogInformation(
                                $"Reassigned role {role} from {deadServer.ServerName} " +
                                $"to {targetServer.ServerName}");
                        }
                    }
                }
                
                // Remove dead server from topology
                await _serverRepo.DeleteByIdAsync(deadServer.Id);
            }
            
            await Task.Delay(30000, stoppingToken);
        }
    }
}
```

### Best Practices

1. **Define clear role responsibilities** - Each role should have a single purpose
2. **Use appropriate balance strategies** - Balance for performance, Stack for resource optimization
3. **Monitor server health** - Implement comprehensive health checks
4. **Handle failover gracefully** - Ensure critical roles are always assigned
5. **Scale horizontally** - Add servers to handle increased load
6. **Use role-aware service resolution** - Services should check role assignment
7. **Implement circuit breakers** - Protect against cascading failures
8. **Log role transitions** - Track when roles move between servers
9. **Test topology changes** - Simulate server failures and additions
10. **Monitor performance metrics** - Track role distribution impact

---

## HtmlEntity System

The HtmlEntity System provides a lightweight content management solution for HTML content within BFormDomain. It enables template-based HTML management with tagging, versioning, and seamless integration with the Work container hierarchy.

### Core Components

#### HtmlInstance
Entity representing HTML content instances:

```csharp
public class HtmlInstance : IAppEntity
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // IAppEntity implementation
    public string EntityType { get; set; } = nameof(HtmlInstance);
    public string Template { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }
    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
    public List<string> AttachedSchedules { get; set; } = new();
    
    // Tagging support
    public List<string> Tags { get; set; } = new();
    public bool Tagged(params string[] anyTags) => Tags.Any(t => anyTags.Contains(t));
    
    // HTML content
    public string Content { get; set; } = null!;
    
    public JObject ToJson() => JObject.FromObject(this);
    
    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        return HtmlEntityReferenceBuilderImplementation.MakeReference(Template, Id, template, vm, queryParameters);
    }
}
```

#### HtmlTemplate
Template definition for HTML content:

```csharp
public class HtmlTemplate : IContentType
{
    public string Name { get; set; } = nameof(HtmlTemplate);
    public int DescendingOrder { get; set; }
    public string? DomainName { get; set; }
    
    // Additional metadata
    public Dictionary<string, string>? SatelliteData { get; set; } = new();
    
    // Template tags applied to all instances
    public List<string> Tags { get; set; } = new();
    
    // HTML content template
    public string Content { get; set; } = null!;
}
```

#### HtmlLogic
Service for creating HTML instances from templates:

```csharp
public class HtmlLogic
{
    private readonly IApplicationPlatformContent _content;
    
    public HtmlLogic(IApplicationPlatformContent content)
    {
        _content = content;
    }
    
    public HtmlInstance GetHtml(string templateName)
    {
        var template = _content.GetContentByName<HtmlTemplate>(templateName)!;
        template.Requires().IsNotNull();
        
        return new HtmlInstance
        {
            Content = template.Content,
            CreatedDate = DateTime.Now,
            Creator = Constants.BuiltIn.SystemUser,
            EntityType = nameof(HtmlInstance),
            HostWorkItem = Constants.BuiltIn.SystemWorkItem,
            HostWorkSet = Constants.BuiltIn.SystemWorkSet,
            Id = Guid.NewGuid(),
            LastModifier = Constants.BuiltIn.SystemUser,
            Template = template.Name,
            UpdatedDate = DateTime.Now,
            Tags = template.Tags
        };
    }
}
```

### HtmlEntity Rule Actions

#### RuleActionHtmlEnrollDashboard
Enrolls HTML content as a dashboard widget:

```csharp
[RuleAction(Constants.RuleActions.HtmlEnrollDashboard)]
public class RuleActionHtmlEnrollDashboard : IRuleAction
{
    private readonly IRepository<WorkSet> _workSetRepo;
    private readonly IMessagePublisher _publisher;
    
    public async Task<RuleActionResponse> ExecuteAsync(
        Rule rule, 
        AppEvent appEvent, 
        RuleActionParameters parameters)
    {
        var htmlId = appEvent.EventData.Get<Guid>("htmlId");
        var workSetId = appEvent.EventData.Get<Guid>("workSetId");
        
        var (workSet, context) = await _workSetRepo.GetByIdAsync(workSetId);
        
        if (workSet != null)
        {
            workSet.DashboardCandidates.Add(new DashboardCandidate
            {
                EntityId = htmlId,
                EntityType = nameof(HtmlInstance),
                DisplayOrder = parameters.Get<int>("order", 100),
                WidgetType = "html-content",
                Configuration = new Dictionary<string, object>
                {
                    ["title"] = parameters.Get<string>("title", "HTML Content"),
                    ["width"] = parameters.Get<string>("width", "medium"),
                    ["refreshInterval"] = parameters.Get<int>("refreshInterval", 0)
                }
            });
            
            await _workSetRepo.UpdateAsync((workSet, context));
            
            await _publisher.PublishAsync(
                Constants.Topics.DashboardUpdated,
                new DashboardUpdatedEvent
                {
                    WorkSetId = workSetId,
                    WidgetAdded = htmlId
                });
        }
        
        return RuleActionResponse.Success();
    }
}
```

### HtmlEntity Usage Examples

#### Creating HTML Content from Templates

```csharp
// Define HTML templates in content files
// content/html-templates/welcome-message.json
{
  "name": "welcome-message",
  "domainName": "onboarding",
  "tags": ["welcome", "new-user"],
  "content": "<div class='welcome-container'><h1>Welcome {{userName}}!</h1><p>Get started with our platform...</p></div>",
  "satelliteData": {
    "category": "onboarding",
    "version": "1.0"
  }
}

// Service to generate personalized HTML
public class WelcomeMessageService
{
    private readonly HtmlLogic _htmlLogic;
    private readonly IRepository<HtmlInstance> _htmlRepo;
    
    public async Task<HtmlInstance> CreateWelcomeMessage(ApplicationUser user)
    {
        // Create instance from template
        var html = _htmlLogic.GetHtml("welcome-message");
        
        // Personalize content
        html.Content = html.Content.Replace("{{userName}}", user.DisplayName);
        html.HostWorkSet = user.DefaultWorkSetId;
        html.Creator = user.Id;
        html.LastModifier = user.Id;
        
        // Add user-specific tags
        html.Tags.Add($"user:{user.Id}");
        html.Tags.Add($"created:{DateTime.UtcNow:yyyy-MM-dd}");
        
        // Save to repository
        await _htmlRepo.CreateAsync(html);
        
        return html;
    }
}
```

#### Dynamic Dashboard Content

```csharp
public class DynamicDashboardService
{
    private readonly HtmlLogic _htmlLogic;
    private readonly IRepository<HtmlInstance> _htmlRepo;
    private readonly IRepository<KPISample> _kpiRepo;
    
    public async Task<HtmlInstance> CreateKPIDashboard(
        Guid workSetId,
        List<Guid> kpiIds)
    {
        // Get KPI data
        var kpiData = new List<dynamic>();
        foreach (var kpiId in kpiIds)
        {
            var samples = await _kpiRepo.GetAllAsync(
                s => s.HostKPI == kpiId && 
                     s.SampleTime > DateTime.UtcNow.AddDays(-7));
            
            kpiData.Add(new
            {
                KpiId = kpiId,
                Latest = samples.OrderByDescending(s => s.SampleTime).FirstOrDefault(),
                Average = samples.Average(s => s.Value),
                Trend = CalculateTrend(samples)
            });
        }
        
        // Generate HTML content
        var htmlContent = GenerateKPIHtml(kpiData);
        
        // Create HTML instance
        var html = _htmlLogic.GetHtml("kpi-dashboard-template");
        html.Content = htmlContent;
        html.HostWorkSet = workSetId;
        html.Tags.Add("dashboard");
        html.Tags.Add("kpi-summary");
        
        await _htmlRepo.CreateAsync(html);
        
        // Enroll in dashboard
        await EnrollInDashboard(html.Id, workSetId);
        
        return html;
    }
    
    private string GenerateKPIHtml(List<dynamic> kpiData)
    {
        var sb = new StringBuilder();
        sb.Append("<div class='kpi-dashboard'>");
        
        foreach (var kpi in kpiData)
        {
            sb.Append($@"
                <div class='kpi-card'>
                    <h3>KPI: {kpi.KpiId}</h3>
                    <div class='kpi-value'>{kpi.Latest?.Value ?? 0:F2}</div>
                    <div class='kpi-average'>Avg: {kpi.Average:F2}</div>
                    <div class='kpi-trend {kpi.Trend}'>{kpi.Trend}</div>
                </div>
            ");
        }
        
        sb.Append("</div>");
        return sb.ToString();
    }
}
```

#### Email Template System

```csharp
public class EmailTemplateService
{
    private readonly HtmlLogic _htmlLogic;
    private readonly IRepository<HtmlInstance> _htmlRepo;
    private readonly INotificationService _notificationService;
    
    public async Task SendTemplatedEmail(
        string templateName,
        string recipientEmail,
        Dictionary<string, string> replacements)
    {
        // Get HTML template
        var html = _htmlLogic.GetHtml(templateName);
        
        // Apply replacements
        foreach (var (key, value) in replacements)
        {
            html.Content = html.Content.Replace($"{{{{{key}}}}}", value);
        }
        
        // Save instance for audit
        html.Tags.Add("email-sent");
        html.Tags.Add($"recipient:{recipientEmail}");
        await _htmlRepo.CreateAsync(html);
        
        // Send email
        await _notificationService.SendEmailAsync(new EmailMessage
        {
            To = recipientEmail,
            Subject = replacements.GetValueOrDefault("subject", "Notification"),
            HtmlBody = html.Content,
            RelatedEntityId = html.Id,
            RelatedEntityType = nameof(HtmlInstance)
        });
    }
}
```

#### Report Generation with HTML

```csharp
public class HtmlReportGenerator
{
    private readonly HtmlLogic _htmlLogic;
    private readonly IRepository<HtmlInstance> _htmlRepo;
    private readonly IRepository<ReportInstance> _reportRepo;
    
    public async Task<HtmlInstance> GenerateHtmlReport(
        ReportInstance report,
        Dictionary<string, object> reportData)
    {
        // Get report template
        var html = _htmlLogic.GetHtml($"report-template-{report.Template}");
        
        // Build HTML content
        var contentBuilder = new StringBuilder(html.Content);
        
        // Replace data placeholders
        contentBuilder.Replace("{{reportTitle}}", report.Title);
        contentBuilder.Replace("{{generatedDate}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        
        // Add data sections
        if (reportData.ContainsKey("summary"))
        {
            contentBuilder.Replace("{{summarySection}}", 
                BuildSummaryHtml(reportData["summary"]));
        }
        
        if (reportData.ContainsKey("details"))
        {
            contentBuilder.Replace("{{detailsSection}}", 
                BuildDetailsTable(reportData["details"] as List<dynamic>));
        }
        
        // Create HTML instance
        html.Content = contentBuilder.ToString();
        html.HostWorkSet = report.HostWorkSet;
        html.HostWorkItem = report.Id;
        html.Tags.Add("report-output");
        html.Tags.Add($"report:{report.Id}");
        
        await _htmlRepo.CreateAsync(html);
        
        return html;
    }
}
```

### Best Practices

1. **Use templates for consistency** - Define reusable HTML templates
2. **Tag appropriately** - Use tags for categorization and search
3. **Maintain audit trail** - Track creator and modifier
4. **Escape user input** - Prevent XSS attacks in dynamic content
5. **Cache templates** - Avoid repeated content system calls
6. **Version control templates** - Track template changes
7. **Use satellite data** - Store metadata in templates
8. **Implement access control** - Check permissions before serving
9. **Optimize large content** - Consider pagination for lists
10. **Monitor usage** - Track which templates are most used

---