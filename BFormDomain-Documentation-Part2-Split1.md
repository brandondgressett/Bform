# BFormDomain Comprehensive Documentation - Part 2

## WorkSets System

WorkSets provide organizational containers for grouping related work items, forms, and other entities. They serve as the primary multi-tenancy and access control mechanism, enabling workspace isolation and team collaboration.

### Core Components

#### WorkSet
The primary container entity:

```csharp
public class WorkSet : IAppEntity
{
    // Standard IAppEntity properties...
    
    // Core properties
    public string Title { get; set; }               // WorkSet display name
    public string? Description { get; set; }         // Detailed description
    public string? IconClass { get; set; }           // UI icon class
    
    // Visibility
    public bool IsListed { get; set; } = true;      // Show in lists
    public bool IsVisible { get; set; } = true;     // General visibility
    
    // Membership tracking
    public List<WorkSetMember> Members { get; set; } = new();  // Member list
    
    // Settings
    public JObject? Settings { get; set; }           // Custom settings
}
```

#### WorkSetTemplate
Defines the structure and behavior of work sets:

```csharp
public class WorkSetTemplate : IContentType
{
    public string Name { get; set; }                // Unique identifier
    public string Title { get; set; }               // Display name
    public List<string> Tags { get; set; }          // Categorization
    
    // Permissions
    public bool AllowUserEdit { get; set; }         // Allow metadata editing
    public bool AllowUserAddMembers { get; set; }   // Allow member management
    public bool AllowComments { get; set; }         // Enable commenting
    public bool AllowFileAttachments { get; set; }  // Enable file uploads
    
    // Display options
    public bool IsVisibleToUsers { get; set; }      // User visibility
    public string? DefaultIconClass { get; set; }    // Default icon
    
    // Initialization
    public List<WorkSetInitialMember> InitialMembers { get; set; } = new();
    public JObject? DefaultSettings { get; set; }    // Default settings
}
```

#### WorkSetMember
Represents membership in a work set:

```csharp
public class WorkSetMember
{
    public Guid UserId { get; set; }                // Member user ID
    public WorkSetMemberRole Role { get; set; }     // Member role
    public DateTime JoinedDate { get; set; }        // When joined
    public Guid? InvitedBy { get; set; }            // Who invited
    public bool IsActive { get; set; } = true;      // Active status
    public JObject? MemberSettings { get; set; }    // Member-specific settings
}

public enum WorkSetMemberRole
{
    Viewer = 0,      // Read-only access
    Member = 1,      // Standard access
    Admin = 2,       // Administrative access
    Owner = 3        // Full control
}
```

#### DashboardCandidate
Represents content available for dashboard display:

```csharp
public class DashboardCandidate : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // Reference to enrolled entity
    public string EntityType { get; set; }          // Entity type
    public Guid EntityId { get; set; }              // Entity ID
    public string? EntityTemplate { get; set; }      // Template name
    
    // Context
    public Guid? WorkSet { get; set; }              // Owning work set
    public Guid? WorkItem { get; set; }             // Owning work item
    
    // Display metadata
    public string Title { get; set; }               // Display title
    public string? Description { get; set; }         // Description
    public string? IconClass { get; set; }          // Icon
    public int DisplayOrder { get; set; }           // Sort order
    
    // Enrollment info
    public Guid EnrolledBy { get; set; }            // Who enrolled
    public DateTime EnrolledDate { get; set; }      // When enrolled
    public List<string> Tags { get; set; } = new(); // Categorization
}
```

### WorkSet Services

#### WorkSetLogic
Primary service for work set operations:

```csharp
public class WorkSetLogic
{
    // Create a new work set
    public async Task<Guid> ActionCreateWorkSet(
        Guid currentUser,
        string templateName,
        string title,
        string? description = null,
        string? iconClass = null,
        bool? isListed = null,
        bool? isVisible = null,
        List<WorkSetMemberInvite>? memberInvites = null,
        JObject? settings = null,
        string[]? tags = null)
    {
        // Load template
        var template = await LoadWorkSetTemplate(templateName);
        
        // Create work set
        var workSet = new WorkSet
        {
            Template = templateName,
            Title = title,
            Description = description,
            IconClass = iconClass ?? template.DefaultIconClass,
            IsListed = isListed ?? true,
            IsVisible = isVisible ?? true,
            Creator = currentUser,
            LastModifier = currentUser,
            Settings = settings ?? template.DefaultSettings,
            Tags = tags?.ToList() ?? new List<string>()
        };
        
        // Add creator as owner
        workSet.Members.Add(new WorkSetMember
        {
            UserId = currentUser,
            Role = WorkSetMemberRole.Owner,
            JoinedDate = DateTime.UtcNow,
            IsActive = true
        });
        
        // Add initial members from template
        foreach (var initialMember in template.InitialMembers)
        {
            var userId = await ResolveUserId(initialMember);
            if (userId.HasValue && userId.Value != currentUser)
            {
                workSet.Members.Add(new WorkSetMember
                {
                    UserId = userId.Value,
                    Role = initialMember.Role,
                    JoinedDate = DateTime.UtcNow,
                    InvitedBy = currentUser,
                    IsActive = true
                });
            }
        }
        
        using (var tc = await _workSetRepo.OpenTransactionAsync())
        {
            // Save work set
            await _workSetRepo.CreateAsync(tc, workSet);
            
            // Process member invites
            if (memberInvites != null)
            {
                foreach (var invite in memberInvites)
                {
                    await ProcessMemberInvite(tc, workSet.Id, invite, currentUser);
                }
            }
            
            // Generate creation event
            await _eventSink.SinkAsync(tc, new AppEvent
            {
                Topic = "WorkSet.Created",
                OriginEntityType = "WorkSet",
                OriginTemplate = templateName,
                OriginId = workSet.Id,
                EntityPayload = workSet.ToBsonDocument()
            });
            
            await tc.CommitAsync();
        }
        
        return workSet.Id;
    }
    
    // Add member to work set
    public async Task ActionAddWorkSetMember(
        Guid currentUser,
        Guid workSetId,
        Guid userId,
        WorkSetMemberRole role = WorkSetMemberRole.Member,
        JObject? memberSettings = null)
    {
        using (var tc = await _workSetRepo.OpenTransactionAsync())
        {
            var (workSets, _) = await _workSetRepo.GetAsync(tc, w => w.Id == workSetId);
            var workSet = workSets.FirstOrDefault();
            
            if (workSet == null)
                throw new NotFoundException($"WorkSet {workSetId} not found");
                
            // Check permissions
            var currentMember = workSet.Members
                .FirstOrDefault(m => m.UserId == currentUser && m.IsActive);
                
            if (currentMember == null || currentMember.Role < WorkSetMemberRole.Admin)
                throw new UnauthorizedException("Insufficient permissions to add members");
                
            // Check if already member
            var existingMember = workSet.Members
                .FirstOrDefault(m => m.UserId == userId);
                
            if (existingMember != null)
            {
                if (existingMember.IsActive)
                    throw new ValidationException("User is already a member");
                    
                // Reactivate inactive member
                existingMember.IsActive = true;
                existingMember.Role = role;
                existingMember.JoinedDate = DateTime.UtcNow;
                existingMember.InvitedBy = currentUser;
                existingMember.MemberSettings = memberSettings;
            }
            else
            {
                // Add new member
                workSet.Members.Add(new WorkSetMember
                {
                    UserId = userId,
                    Role = role,
                    JoinedDate = DateTime.UtcNow,
                    InvitedBy = currentUser,
                    IsActive = true,
                    MemberSettings = memberSettings
                });
            }
            
            workSet.LastModifier = currentUser;
            workSet.UpdatedDate = DateTime.UtcNow;
            
            await _workSetRepo.UpdateAsync(tc, workSet);
            
            // Generate member added event
            await _eventSink.SinkAsync(tc, new AppEvent
            {
                Topic = "WorkSet.MemberAdded",
                OriginEntityType = "WorkSet",
                OriginTemplate = workSet.Template,
                OriginId = workSet.Id,
                EntityPayload = new BsonDocument
                {
                    ["WorkSetId"] = workSet.Id.ToString(),
                    ["UserId"] = userId.ToString(),
                    ["Role"] = role.ToString()
                }
            });
            
            await tc.CommitAsync();
        }
    }
    
    // Get user's work sets
    public async Task<List<WorkSetListVM>> GetUserWorkSets(
        Guid userId,
        bool includeInactive = false)
    {
        // Get work sets where user is a member
        var (workSets, _) = await _workSetRepo.GetAsync(
            w => w.Members.Any(m => m.UserId == userId && 
                             (includeInactive || m.IsActive)) &&
                 w.IsVisible);
                 
        var vms = new List<WorkSetListVM>();
        
        foreach (var workSet in workSets.OrderBy(w => w.Title))
        {
            var membership = workSet.Members
                .First(m => m.UserId == userId);
                
            vms.Add(new WorkSetListVM
            {
                Id = workSet.Id,
                Title = workSet.Title,
                Description = workSet.Description,
                IconClass = workSet.IconClass,
                Role = membership.Role,
                MemberCount = workSet.Members.Count(m => m.IsActive),
                CreatedDate = workSet.CreatedDate,
                Tags = workSet.Tags,
                HasDashboardItems = await HasDashboardItems(workSet.Id)
            });
        }
        
        return vms;
    }
}
```

### WorkSet Rule Actions

#### RuleActionCreateWorkSet
Creates work sets in response to events:

```csharp
public class RuleActionCreateWorkSet : IRuleActionEvaluator
{
    public class Args
    {
        public string? TemplateName { get; set; }
        public string? TemplateNameQuery { get; set; }
        public string? Title { get; set; }
        public string? TitleQuery { get; set; }
        public string? Description { get; set; }
        public string? DescriptionQuery { get; set; }
        public string? IconClass { get; set; }
        public JObject? Settings { get; set; }
        public string? SettingsQuery { get; set; }
        public List<string>? InitialTags { get; set; }
        public List<WorkSetMemberInvite>? MemberInvites { get; set; }
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        // Resolve parameters
        var templateName = a.TemplateName ?? 
            JsonPathQuery(eventData, a.TemplateNameQuery);
        var title = a.Title ?? 
            JsonPathQuery(eventData, a.TitleQuery)?.ToString();
        var description = a.Description ?? 
            JsonPathQuery(eventData, a.DescriptionQuery)?.ToString();
        var settings = a.Settings ?? 
            ResolveJson(a.SettingsQuery, eventData);
            
        // Create work set
        var workSetId = await _workSetLogic.EventCreateWorkSet(
            templateName: templateName,
            title: title,
            description: description,
            iconClass: a.IconClass,
            memberInvites: a.MemberInvites,
            settings: settings,
            tags: a.InitialTags?.ToArray()
        );
        
        // Add to event data
        eventData["createdWorkSetId"] = workSetId;
        
        return eventData;
    }
}
```

#### RuleActionWorkSetAddMember
Adds members to work sets:

```csharp
public class RuleActionWorkSetAddMember : IRuleActionEvaluator
{
    public class Args
    {
        public string? WorkSetIdQuery { get; set; }
        public string? UserIdQuery { get; set; }
        public WorkSetMemberRole Role { get; set; } = WorkSetMemberRole.Member;
        public JObject? MemberSettings { get; set; }
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        var workSetId = ResolveGuid(eventData, a.WorkSetIdQuery);
        var userId = ResolveGuid(eventData, a.UserIdQuery);
        
        if (!workSetId.HasValue || !userId.HasValue)
            return eventData;
            
        await _workSetLogic.EventAddWorkSetMember(
            workSetId: workSetId.Value,
            userId: userId.Value,
            role: a.Role,
            memberSettings: a.MemberSettings
        );
        
        return eventData;
    }
}
```

### Dashboard Integration

WorkSets integrate with the dashboard system to provide customizable views:

```csharp
public class DashboardService
{
    // Enroll entity in dashboard
    public async Task EnrollInDashboard(
        Guid currentUser,
        string entityType,
        Guid entityId,
        Guid? workSetId,
        string title,
        string? description = null,
        string? iconClass = null,
        int? displayOrder = null,
        string[]? tags = null)
    {
        // Verify permissions
        if (workSetId.HasValue)
        {
            await VerifyWorkSetAccess(currentUser, workSetId.Value, WorkSetMemberRole.Member);
        }
        
        // Load entity to get template
        var entity = await LoadEntity(entityType, entityId);
        
        // Create dashboard candidate
        var candidate = new DashboardCandidate
        {
            EntityType = entityType,
            EntityId = entityId,
            EntityTemplate = entity.Template,
            WorkSet = workSetId,
            Title = title,
            Description = description,
            IconClass = iconClass ?? GetDefaultIcon(entityType),
            DisplayOrder = displayOrder ?? await GetNextDisplayOrder(workSetId),
            EnrolledBy = currentUser,
            EnrolledDate = DateTime.UtcNow,
            Tags = tags?.ToList() ?? new List<string>()
        };
        
        await _dashboardRepo.CreateAsync(candidate);
    }
    
    // Get dashboard for work set
    public async Task<WorkSetDashboardVM> GetWorkSetDashboard(
        Guid currentUser,
        Guid workSetId)
    {
        // Verify access
        var memberRole = await GetMemberRole(currentUser, workSetId);
        if (!memberRole.HasValue)
            throw new UnauthorizedException("Not a member of this work set");
            
        // Load work set
        var workSet = await LoadWorkSet(workSetId);
        
        // Load dashboard items
        var (candidates, _) = await _dashboardRepo.GetAsync(
            c => c.WorkSet == workSetId);
            
        // Build dashboard
        var dashboard = new WorkSetDashboardVM
        {
            WorkSet = BuildWorkSetVM(workSet, memberRole.Value),
            Items = new List<DashboardItemVM>()
        };
        
        // Load and convert each item
        foreach (var candidate in candidates.OrderBy(c => c.DisplayOrder))
        {
            var item = await BuildDashboardItem(candidate, currentUser);
            if (item != null)
            {
                dashboard.Items.Add(item);
            }
        }
        
        return dashboard;
    }
    
    private async Task<DashboardItemVM?> BuildDashboardItem(
        DashboardCandidate candidate,
        Guid currentUser)
    {
        try
        {
            // Load entity data
            var entityData = await _entityLoader.LoadJson(
                $"entity://{candidate.EntityType}/{candidate.EntityId}?vm=true");
                
            if (entityData == null)
                return null;
                
            return new DashboardItemVM
            {
                Id = candidate.Id,
                EntityType = candidate.EntityType,
                EntityId = candidate.EntityId,
                Title = candidate.Title,
                Description = candidate.Description,
                IconClass = candidate.IconClass,
                DisplayOrder = candidate.DisplayOrder,
                Tags = candidate.Tags,
                EntityData = entityData,
                CanEdit = await CanEditEntity(currentUser, candidate.EntityType, candidate.EntityId),
                CanRemove = await CanRemoveFromDashboard(currentUser, candidate)
            };
        }
        catch
        {
            // Entity might be deleted or inaccessible
            return null;
        }
    }
}
```

### WorkSet Examples

#### Creating a Project WorkSet Template
```csharp
var projectTemplate = new WorkSetTemplate
{
    Name = "Project",
    Title = "Project Workspace",
    Tags = new List<string> { "project", "collaboration" },
    
    // Permissions
    AllowUserEdit = true,
    AllowUserAddMembers = true,
    AllowComments = true,
    AllowFileAttachments = true,
    
    // Display
    IsVisibleToUsers = true,
    DefaultIconClass = "fa-project-diagram",
    
    // Default settings
    DefaultSettings = JObject.FromObject(new
    {
        features = new
        {
            enableKanbanBoard = true,
            enableGanttChart = false,
            enableTimeTracking = true,
            enableBudgetTracking = false
        },
        notifications = new
        {
            emailOnNewTask = true,
            emailOnStatusChange = true,
            dailyDigest = false
        }
    }),
    
    // Initial members (e.g., project manager role)
    InitialMembers = new List<WorkSetInitialMember>
    {
        new WorkSetInitialMember
        {
            UserTag = "project-manager",
            Role = WorkSetMemberRole.Admin
        }
    }
};
```

#### Implementing a Team Onboarding Rule
```csharp
var teamOnboardingRule = new Rule
{
    Name = "OnboardNewTeamMember",
    TopicBindings = new List<string> { "User.Created" },
    
    Conditions = new List<RuleCondition>
    {
        new RuleCondition
        {
            Query = "$.Tags[*]",
            Check = RuleConditionCheck.Any,
            Value = "employee"
        }
    },
    
    Actions = new List<RuleAction>
    {
        // Create personal workspace
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionCreateWorkSet",
                Args = JObject.FromObject(new
                {
                    TemplateName = "PersonalWorkspace",
                    TitleQuery = "$.DisplayName + '''s Workspace'",
                    Description = "Personal workspace for tasks and projects",
                    Settings = new
                    {
                        isPersonal = true,
                        defaultView = "list"
                    }
                }),
                ResultProperty = "personalWorkspaceId"
            }
        },
        // Add to team work set
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionWorkSetAddMember",
                Args = JObject.FromObject(new
                {
                    WorkSetIdQuery = "$.Department == 'Engineering' ? '#{EngineeringTeamWorkSet}' : '#{GeneralTeamWorkSet}'",
                    UserIdQuery = "$.Id",
                    Role = WorkSetMemberRole.Member
                })
            }
        },
        // Create onboarding checklist
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionCreateWorkItem",
                Args = JObject.FromObject(new
                {
                    TemplateName = "OnboardingChecklist",
                    WorkSetQuery = "$.personalWorkspaceId",
                    TitleQuery = "'Onboarding Checklist for ' + $.DisplayName",
                    UserAssigneeQuery = "$.Id",
                    Priority = 1
                })
            }
        }
    }
};
```

#### Creating a Multi-Tenant Application Structure
```csharp
public class TenantService
{
    // Create a new tenant with full workspace setup
    public async Task<TenantSetupResult> CreateTenant(
        string organizationName,
        Guid adminUserId,
        TenantPlan plan)
    {
        var result = new TenantSetupResult();
        
        using (var tc = await _workSetRepo.OpenTransactionAsync())
        {
            try
            {
                // Create main organization work set
                var orgWorkSetId = await _workSetLogic.CreateWorkSet(
                    tc,
                    currentUser: adminUserId,
                    templateName: "Organization",
                    title: organizationName,
                    description: $"Main workspace for {organizationName}",
                    settings: JObject.FromObject(new
                    {
                        plan = plan.ToString(),
                        limits = GetPlanLimits(plan),
                        branding = new
                        {
                            primaryColor = "#007bff",
                            logoUrl = null
                        }
                    }),
                    tags: new[] { "organization", "tenant", plan.ToString().ToLower() }
                );
                
                result.OrganizationWorkSetId = orgWorkSetId;
                
                // Create department work sets
                var departments = new[] { "Sales", "Support", "Operations" };
                foreach (var dept in departments)
                {
                    var deptWorkSetId = await _workSetLogic.CreateWorkSet(
                        tc,
                        currentUser: adminUserId,
                        templateName: "Department",
                        title: $"{organizationName} - {dept}",
                        description: $"{dept} department workspace",
                        settings: JObject.FromObject(new
                        {
                            department = dept,
                            parentOrganization = orgWorkSetId
                        }),
                        tags: new[] { "department", dept.ToLower() }
                    );
                    
                    result.DepartmentWorkSets[dept] = deptWorkSetId;
                    
                    // Link department to organization
                    await _workSetLogic.AddWorkSetMember(
                        tc,
                        workSetId: orgWorkSetId,
                        userId: adminUserId,
                        role: WorkSetMemberRole.Admin,
                        memberSettings: JObject.FromObject(new
                        {
                            linkedDepartment = deptWorkSetId
                        })
                    );
                }
                
                // Create shared resources work set
                var sharedResourcesId = await _workSetLogic.CreateWorkSet(
                    tc,
                    currentUser: adminUserId,
                    templateName: "SharedResources",
                    title: $"{organizationName} - Shared Resources",
                    description: "Shared forms, templates, and resources",
                    tags: new[] { "resources", "shared" }
                );
                
                result.SharedResourcesWorkSetId = sharedResourcesId;
                
                // Set up initial content
                await SetupInitialContent(tc, result, adminUserId);
                
                await tc.CommitAsync();
            }
            catch
            {
                await tc.AbortAsync();
                throw;
            }
        }
        
        return result;
    }
    
    private async Task SetupInitialContent(
        ITransactionContext tc,
        TenantSetupResult setup,
        Guid adminUserId)
    {
        // Create default forms
        var forms = new[]
        {
            ("ContactForm", "Contact Information"),
            ("FeedbackForm", "Customer Feedback"),
            ("RequestForm", "Service Request")
        };
        
        foreach (var (template, title) in forms)
        {
            var formId = await _formLogic.CreateForm(
                tc,
                templateName: template,
                workSet: setup.SharedResourcesWorkSetId,
                name: title,
                tags: new[] { "default", "template" }
            );
            
            // Enroll in dashboard
            await _dashboardService.EnrollInDashboard(
                tc,
                currentUser: adminUserId,
                entityType: "Form",
                entityId: formId,
                workSetId: setup.SharedResourcesWorkSetId,
                title: title,
                tags: new[] { "template" }
            );
        }
        
        // Create workflow templates
        await CreateDefaultWorkflows(tc, setup, adminUserId);
    }
}
```

### Best Practices

1. **Use WorkSets for multi-tenancy** - Isolate customer data effectively
2. **Implement proper role checks** - Enforce permissions at service layer
3. **Design member roles carefully** - Balance security with usability
4. **Use settings for customization** - Store workspace-specific configuration
5. **Leverage dashboard enrollment** - Make important content easily accessible
6. **Track membership changes** - Maintain audit trail for compliance
7. **Handle inactive members** - Soft delete for historical tracking
8. **Use tags for categorization** - Enable cross-workspace discovery
9. **Initialize with templates** - Ensure consistent workspace setup
10. **Monitor workspace growth** - Implement limits based on plan/tier

---

## Tables System

The Tables System provides dynamic, schema-flexible data storage that allows end-users to define custom data structures without code changes. It supports complex querying, aggregation, and integration with other platform components.

### Core Components

#### TableTemplate
Defines the structure and behavior of dynamic tables:

```csharp
public class TableTemplate : IContentType
{
    public string Name { get; set; }                // Unique identifier
    public string Title { get; set; }               // Display name
    public List<string> Tags { get; set; }          // Categorization
    
    // Storage configuration
    public string CollectionName { get; set; }      // MongoDB collection
    public Guid? CollectionId { get; set; }         // Optional GUID for collection
    
    // Schema definition
    public List<ColDef> Columns { get; set; }       // Column definitions
    public SatelliteJson? AgGridColumnDefs { get; set; }  // AG-Grid config
    
    // Scoping
    public bool IsPerWorkSet { get; set; }          // Scope to work set
    public bool IsPerWorkItem { get; set; }         // Scope to work item
    
    // Display options
    public bool DisplayMasterDetail { get; set; }    // Master-detail view
    public string? DetailFormTemplate { get; set; }  // Form for details
    public bool IsVisibleToUsers { get; set; }      // User visibility
    
    // Data retention
    public int? MonthsRetained { get; set; }        // Retention in months
    public int? DaysRetained { get; set; }          // Retention in days
    public int? CountRetained { get; set; }         // Max row count
    public DataRetentionBehavior? RetentionBehavior { get; set; }
    
    // User permissions
    public bool IsUserEditAllowed { get; set; }     // Allow editing
    public bool IsUserAddAllowed { get; set; }      // Allow adding
    public bool IsUserDeleteAllowed { get; set; }   // Allow deletion
    public bool IsUserQueryAllowed { get; set; }    // Allow queries
    
    // Automation
    public List<ScheduledEventTemplate> Schedules { get; set; } = new();
}

public class ColDef
{
    public string ColKey { get; set; }              // Column identifier
    public string? ColName { get; set; }            // Display name
    public ColType ColType { get; set; }            // Data type
    public bool IsIndexed { get; set; }             // Create index
    public bool IsRequired { get; set; }            // Required field
    public bool IsUnique { get; set; }              // Unique constraint
    public string? DefaultValue { get; set; }        // Default value
    public string? ValidationRule { get; set; }      // Validation expression
}

public enum ColType
{
    String = 0,
    Number = 1,
    Boolean = 2,
    DateTime = 3,
    Json = 4,
    Reference = 5,  // Reference to another entity
    Tags = 6        // Array of strings
}
```

#### TableRowData
Dynamic row storage with flexible schema:

```csharp
public class TableRowData : IDataModel, ITaggable
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // Dynamic data storage
    public BsonDocument PropertyBag { get; set; }   // Flexible properties
    
    // Indexed keys for efficient querying
    public string? KeyRowId { get; set; }           // String identifier
    public DateTime? KeyDate { get; set; }          // Date for time-based queries
    public Guid? KeyUser { get; set; }              // User reference
    public Guid? KeyWorkSet { get; set; }           // WorkSet scope
    public Guid? KeyWorkItem { get; set; }          // WorkItem scope
    public double? KeyNumeric { get; set; }         // Numeric for ranges
    
    // Metadata
    public List<string> Tags { get; set; } = new();
    public DateTime Created { get; set; }
    
    // Methods
    public void SetProperties(JObject data, TableTemplate template)
    {
        PropertyBag = data.ToBsonDocument();
        
        // Map to indexed keys based on column definitions
        foreach (var col in template.Columns.Where(c => c.IsIndexed))
        {
            var value = data[col.ColKey];
            if (value == null) continue;
            
            switch (col.ColKey.ToLower())
            {
                case "rowid":
                case "id":
                case "key":
                    KeyRowId = value.ToString();
                    break;
                case "date":
                case "created":
                case "timestamp":
                    KeyDate = value.Type == JTokenType.Date 
                        ? value.Value<DateTime>() 
                        : DateTime.Parse(value.ToString());
                    break;
                case "user":
                case "userid":
                case "owner":
                    KeyUser = Guid.Parse(value.ToString());
                    break;
                case "value":
                case "amount":
                case "score":
                    KeyNumeric = value.Value<double>();
                    break;
            }
        }
    }
}
```

#### TableMetadata
Metadata for table management:

```csharp
public class TableMetadata : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    public string TableTemplate { get; set; }       // Template reference
    public string CollectionName { get; set; }      // MongoDB collection
    
    // Statistics
    public long RowCount { get; set; }              // Current row count
    public DateTime LastModified { get; set; }      // Last update time
    public long TotalSizeBytes { get; set; }        // Storage size
    
    // Indexing
    public List<TableIndex> Indexes { get; set; } = new();
    public DateTime LastIndexUpdate { get; set; }
    
    // Performance
    public double AverageQueryTime { get; set; }    // Avg query ms
    public long QueryCount { get; set; }            // Total queries
}
```

### Table Services

#### TableLogic
Primary service for table operations:

```csharp
public class TableLogic
{
    private readonly KeyInject<string, TableDataRepository> _repoFactory;
    
    // Insert data with field mapping
    public async Task<Guid> EventMapInsertTableRow(
        string tableTemplate,
        List<Mapping> mappings,
        JObject sourceData,
        Guid? workSet = null,
        Guid? workItem = null,
        string[]? tags = null)
    {
        // Load template
        var template = await LoadTableTemplate(tableTemplate);
        
        // Create row data by applying mappings
        var rowData = new JObject();
        
        foreach (var mapping in mappings)
        {
            var value = ExtractValue(sourceData, mapping);
            
            // Apply preprocessing
            if (mapping.Preprocess != null)
            {
                value = ApplyPreprocessing(value, mapping.Preprocess);
            }
            
            // Validate against column definition
            var column = template.Columns.FirstOrDefault(c => c.ColKey == mapping.ToField);
            if (column != null)
            {
                ValidateColumnValue(value, column);
            }
            
            rowData[mapping.ToField] = value;
        }
        
        // Create table row
        var row = new TableRowData
        {
            Created = DateTime.UtcNow,
            Tags = tags?.ToList() ?? new List<string>()
        };
        
        // Apply scoping
        if (template.IsPerWorkSet)
        {
            row.KeyWorkSet = workSet ?? throw new ValidationException("WorkSet required");
        }
        if (template.IsPerWorkItem)
        {
            row.KeyWorkItem = workItem ?? throw new ValidationException("WorkItem required");
        }
        
        // Set properties with indexing
        row.SetProperties(rowData, template);
        
        // Get repository for this table
        var repo = _repoFactory.Get(template.CollectionName);
        
        using (var tc = await repo.OpenTransactionAsync())
        {
            await repo.CreateAsync(tc, row);
            
            // Update metadata
            await UpdateTableMetadata(tc, template, 1);
            
            // Generate event
            await _eventSink.SinkAsync(tc, new AppEvent
            {
                Topic = $"Table.{tableTemplate}.RowInserted",
                OriginEntityType = "TableRow",
                OriginTemplate = tableTemplate,
                OriginId = row.Id,
                EntityPayload = row.ToBsonDocument()
            });
            
            await tc.CommitAsync();
        }
        
        return row.Id;
    }
    
    // Query table data
    public async Task<List<JObject>> QueryDataTableAll(
        string tableTemplate,
        JObject? filter = null,
        string? orderBy = null,
        bool ascending = true,
        Guid? workSet = null,
        Guid? workItem = null)
    {
        var template = await LoadTableTemplate(tableTemplate);
        var repo = _repoFactory.Get(template.CollectionName);
        
        // Build query expression
        Expression<Func<TableRowData, bool>> predicate = row => true;
        
        // Apply scoping
        if (template.IsPerWorkSet && workSet.HasValue)
        {
            predicate = predicate.And(row => row.KeyWorkSet == workSet.Value);
        }
        if (template.IsPerWorkItem && workItem.HasValue)
        {
            predicate = predicate.And(row => row.KeyWorkItem == workItem.Value);
        }
        
        // Apply filter
        if (filter != null)
        {
            predicate = predicate.And(BuildFilterExpression(filter, template));
        }
        
        // Execute query
        List<TableRowData> rows;
        
        if (!string.IsNullOrEmpty(orderBy))
        {
            var orderExpression = BuildOrderExpression(orderBy, template);
            (rows, _) = await repo.GetOrderedAsync(
                predicate, 
                orderExpression, 
                ascending);
        }
        else
        {
            (rows, _) = await repo.GetAsync(predicate);
        }
        
        // Convert to JObjects
        return rows.Select(row => 
        {
            var obj = JObject.Parse(row.PropertyBag.ToJson());
            obj["_id"] = row.Id;
            obj["_created"] = row.Created;
            obj["_tags"] = JArray.FromObject(row.Tags);
            return obj;
        }).ToList();
    }
    
    // Execute registered query
    public async Task<List<JObject>> RegisteredQueryDataTableAll(
        string queryName,
        JObject? parameters = null,
        Guid? workSet = null,
        Guid? workItem = null)
    {
        // Load registered query
        var query = await LoadRegisteredQuery(queryName);
        
        // Apply parameter substitution
        var filter = query.Filter != null 
            ? SubstituteParameters(query.Filter, parameters) 
            : null;
            
        // Execute query
        return await QueryDataTableAll(
            tableTemplate: query.TableTemplate,
            filter: filter,
            orderBy: query.OrderBy,
            ascending: query.Ascending,
            workSet: workSet,
            workItem: workItem
        );
    }
    
    // Summarize table data
    public async Task<JObject> SummarizeTable(
        string tableTemplate,
        string summarizationTemplate,
        JObject? parameters = null,
        Guid? workSet = null,
        Guid? workItem = null)
    {
        // Load summarization template
        var summary = await LoadSummarizationTemplate(summarizationTemplate);
        
        // Query base data
        var data = await QueryDataTableAll(
            tableTemplate: tableTemplate,
            filter: summary.Filter,
            workSet: workSet,
            workItem: workItem
        );
        
        // Perform aggregation
        var result = new JObject();
        
        foreach (var aggregation in summary.Aggregations)
        {
            var values = data
                .Select(row => row[aggregation.Field])
                .Where(v => v != null && v.Type != JTokenType.Null);
                
            result[aggregation.Alias ?? aggregation.Field] = aggregation.Function switch
            {
                AggregationFunction.Count => values.Count(),
                AggregationFunction.Sum => values.Sum(v => v.Value<double>()),
                AggregationFunction.Average => values.Average(v => v.Value<double>()),
                AggregationFunction.Min => values.Min(v => v.Value<double>()),
                AggregationFunction.Max => values.Max(v => v.Value<double>()),
                AggregationFunction.Distinct => values.Distinct().Count(),
                _ => throw new NotSupportedException($"Aggregation {aggregation.Function} not supported")
            };
        }
        
        // Apply grouping if specified
        if (summary.GroupBy != null && summary.GroupBy.Any())
        {
            var grouped = data.GroupBy(row => 
                string.Join("|", summary.GroupBy.Select(g => row[g]?.ToString() ?? "null"))
            );
            
            result["groups"] = JArray.FromObject(
                grouped.Select(g => new
                {
                    key = g.Key,
                    count = g.Count(),
                    aggregations = PerformGroupAggregations(g, summary.Aggregations)
                })
            );
        }
        
        return result;
    }
}

// Supporting class for field mapping
public class Mapping
{
    public string ToField { get; set; }             // Target field name
    public string? FromField { get; set; }          // Source field name
    public string? FromQuery { get; set; }          // JSONPath query
    public JToken? StaticValue { get; set; }        // Static value
    public MapPreprocess? Preprocess { get; set; }  // Preprocessing
    public bool Nullable { get; set; }              // Allow null
}

public enum MapPreprocess
{
    None = 0,
    MakeEntityReference = 1,    // Convert to entity://Type/Id
    TruncateDate = 2,           // Remove time component
    BinNumeric = 3,             // Bin numeric values
    RoundNumeric = 4,           // Round to nearest integer
    ExtractTags = 5             // Extract tags from string
}
```

### Table Rule Actions

#### RuleActionInsertTableData
Inserts data into tables from events:

```csharp
public class RuleActionInsertTableData : IRuleActionEvaluator
{
    public class Args
    {
        public string TableTemplate { get; set; }
        public List<Mapping> Map { get; set; }
        public List<string>? Tags { get; set; }
        public string? QueryTags { get; set; }
        public string? WorkSetQuery { get; set; }
        public string? WorkItemQuery { get; set; }
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        // Resolve tags
        var tags = a.Tags ?? 
            (a.QueryTags != null 
                ? JsonPathQuery(eventData, a.QueryTags) as JArray
                : null)?.ToObject<List<string>>();
                
        // Resolve scoping
        var workSet = ResolveGuid(eventData, a.WorkSetQuery);
        var workItem = ResolveGuid(eventData, a.WorkItemQuery);
        
        // Insert row
        var rowId = await _tableLogic.EventMapInsertTableRow(
            tableTemplate: a.TableTemplate,
            mappings: a.Map,
            sourceData: eventData,
            workSet: workSet,
            workItem: workItem,
            tags: tags?.ToArray()
        );
        
        // Add to event data
        eventData[$"inserted_{a.TableTemplate}_RowId"] = rowId;
        
        return eventData;
    }
}
```

### Registered Queries and Summarizations

#### RegisteredTableQueryTemplate
Saved queries for reuse:

```csharp
public class RegisteredTableQueryTemplate : IContentType
{
    public string Name { get; set; }                // Query name
    public string Title { get; set; }               // Display title
    public string TableTemplate { get; set; }       // Target table
    
    // Query definition
    public JObject? Filter { get; set; }            // Filter criteria
    public string? OrderBy { get; set; }            // Sort field
    public bool Ascending { get; set; } = true;     // Sort direction
    
    // Parameters
    public List<QueryParameter> Parameters { get; set; } = new();
    
    // Display
    public bool IsVisibleToUsers { get; set; }      // User visibility
    public string? Description { get; set; }         // Query description
}

public class QueryParameter
{
    public string Name { get; set; }                // Parameter name
    public string Type { get; set; }                // Data type
    public bool Required { get; set; }              // Is required
    public JToken? DefaultValue { get; set; }       // Default value
}
```

#### RegisteredTableSummarizationTemplate
Saved aggregation definitions:

```csharp
public class RegisteredTableSummarizationTemplate : IContentType
{
    public string Name { get; set; }                // Summarization name
    public string Title { get; set; }               // Display title
    public string TableTemplate { get; set; }       // Target table
    
    // Filter
    public JObject? Filter { get; set; }            // Pre-filter data
    
    // Grouping
    public List<string> GroupBy { get; set; } = new();
    
    // Aggregations
    public List<AggregationDef> Aggregations { get; set; } = new();
    
    // Display
    public bool IsVisibleToUsers { get; set; }
    public string? Description { get; set; }
}

public class AggregationDef
{
    public string Field { get; set; }               // Field to aggregate
    public AggregationFunction Function { get; set; } // Aggregation function
    public string? Alias { get; set; }              // Result field name
}
```

### Table Examples

#### Creating a Customer Orders Table
```csharp
var ordersTemplate = new TableTemplate
{
    Name = "CustomerOrders",
    Title = "Customer Orders",
    Tags = new List<string> { "orders", "sales" },
    
    CollectionName = "customer_orders",
    
    Columns = new List<ColDef>
    {
        new ColDef 
        { 
            ColKey = "orderId", 
            ColName = "Order ID", 
            ColType = ColType.String,
            IsIndexed = true,
            IsRequired = true,
            IsUnique = true
        },
        new ColDef 
        { 
            ColKey = "customerId", 
            ColName = "Customer ID", 
            ColType = ColType.Reference,
            IsIndexed = true,
            IsRequired = true
        },
        new ColDef 
        { 
            ColKey = "orderDate", 
            ColName = "Order Date", 
            ColType = ColType.DateTime,
            IsIndexed = true,
            IsRequired = true,
            DefaultValue = "now()"
        },
        new ColDef 
        { 
            ColKey = "status", 
            ColName = "Status", 
            ColType = ColType.String,
            IsRequired = true,
            DefaultValue = "pending",
            ValidationRule = "in:pending,processing,shipped,delivered,cancelled"
        },
        new ColDef 
        { 
            ColKey = "items", 
            ColName = "Order Items", 
            ColType = ColType.Json,
            IsRequired = true
        },
        new ColDef 
        { 
            ColKey = "totalAmount", 
            ColName = "Total Amount", 
            ColType = ColType.Number,
            IsIndexed = true,
            IsRequired = true,
            ValidationRule = "min:0"
        },
        new ColDef 
        { 
            ColKey = "tags", 
            ColName = "Tags", 
            ColType = ColType.Tags,
            IsRequired = false
        }
    },
    
    // Scoping
    IsPerWorkSet = true,  // Orders are per organization
    
    // Display
    DisplayMasterDetail = true,
    DetailFormTemplate = "OrderDetailsForm",
    IsVisibleToUsers = true,
    
    // Retention
    MonthsRetained = 24,  // Keep 2 years
    CountRetained = 100000,  // Max 100k rows
    RetentionBehavior = DataRetentionBehavior.ArchiveOldest,
    
    // Permissions
    IsUserEditAllowed = true,
    IsUserAddAllowed = true,
    IsUserDeleteAllowed = false,  // No deletion
    IsUserQueryAllowed = true,
    
    // Automation
    Schedules = new List<ScheduledEventTemplate>
    {
        new ScheduledEventTemplate
        {
            Name = "DailyOrderReport",
            Schedule = "0 9 * * *",  // 9 AM daily
            Topic = "GenerateDailyOrderReport"
        }
    }
};
```

#### Implementing Order Processing Rules
```csharp
// Rule to capture new orders
var captureOrderRule = new Rule
{
    Name = "CaptureNewOrder",
    TopicBindings = new List<string> { "Order.Placed" },
    
    Actions = new List<RuleAction>
    {
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionInsertTableData",
                Args = JObject.FromObject(new
                {
                    TableTemplate = "CustomerOrders",
                    Map = new List<Mapping>
                    {
                        new Mapping 
                        { 
                            ToField = "orderId", 
                            FromQuery = "$.orderId" 
                        },
                        new Mapping 
                        { 
                            ToField = "customerId", 
                            FromQuery = "$.customer.id",
                            Preprocess = MapPreprocess.MakeEntityReference
                        },
                        new Mapping 
                        { 
                            ToField = "orderDate", 
                            FromQuery = "$.timestamp" 
                        },
                        new Mapping 
                        { 
                            ToField = "status", 
                            StaticValue = "pending" 
                        },
                        new Mapping 
                        { 
                            ToField = "items", 
                            FromQuery = "$.items" 
                        },
                        new Mapping 
                        { 
                            ToField = "totalAmount", 
                            FromQuery = "$.total" 
                        }
                    },
                    Tags = new[] { "new-order", "pending" },
                    WorkSetQuery = "$.workSetId"
                })
            }
        }
    }
};

// Rule to update order status
var updateOrderStatusRule = new Rule
{
    Name = "UpdateOrderStatus",
    TopicBindings = new List<string> { "Order.StatusChanged" },
    
    Actions = new List<RuleAction>
    {
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionEditTableRow",
                Args = JObject.FromObject(new
                {
                    TableTemplate = "CustomerOrders",
                    RowIdQuery = "$.orderId",
                    Updates = new
                    {
                        status = "$.newStatus",
                        lastUpdated = "now()"
                    }
                })
            }
        }
    }
};
```

#### Creating Sales Analytics Queries
```csharp
// Daily sales summary
var dailySalesSummary = new RegisteredTableSummarizationTemplate
{
    Name = "DailySalesSummary",
    Title = "Daily Sales Summary",
    TableTemplate = "CustomerOrders",
    
    Filter = JObject.FromObject(new
    {
        orderDate = new { 
            $gte = "today()", 
            $lt = "tomorrow()" 
        },
        status = new { 
            $ne = "cancelled" 
        }
    }),
    
    Aggregations = new List<AggregationDef>
    {
        new AggregationDef 
        { 
            Field = "orderId", 
            Function = AggregationFunction.Count, 
            Alias = "orderCount" 
        },
        new AggregationDef 
        { 
            Field = "totalAmount", 
            Function = AggregationFunction.Sum, 
            Alias = "totalRevenue" 
        },
        new AggregationDef 
        { 
            Field = "totalAmount", 
            Function = AggregationFunction.Average, 
            Alias = "averageOrderValue" 
        },
        new AggregationDef 
        { 
            Field = "customerId", 
            Function = AggregationFunction.Distinct, 
            Alias = "uniqueCustomers" 
        }
    },
    
    IsVisibleToUsers = true,
    Description = "Summary of today's sales performance"
};

// Monthly sales by status
var monthlySalesByStatus = new RegisteredTableSummarizationTemplate
{
    Name = "MonthlySalesByStatus",
    Title = "Monthly Sales by Status",
    TableTemplate = "CustomerOrders",
    
    Filter = JObject.FromObject(new
    {
        orderDate = new { 
            $gte = "startOfMonth()", 
            $lt = "startOfNextMonth()" 
        }
    }),
    
    GroupBy = new List<string> { "status" },
    
    Aggregations = new List<AggregationDef>
    {
        new AggregationDef 
        { 
            Field = "orderId", 
            Function = AggregationFunction.Count, 
            Alias = "count" 
        },
        new AggregationDef 
        { 
            Field = "totalAmount", 
            Function = AggregationFunction.Sum, 
            Alias = "revenue" 
        }
    },
    
    IsVisibleToUsers = true
};
```

#### Dynamic Table Service
```csharp
public class DynamicTableService
{
    // Create table from user definition
    public async Task<TableTemplate> CreateUserDefinedTable(
        Guid currentUser,
        string tableName,
        string title,
        List<UserColumnDef> columns,
        TableOptions options)
    {
        // Validate table name
        if (!Regex.IsMatch(tableName, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            throw new ValidationException("Invalid table name");
            
        // Convert user columns to system columns
        var systemColumns = new List<ColDef>();
        
        foreach (var userCol in columns)
        {
            var colDef = new ColDef
            {
                ColKey = SanitizeColumnKey(userCol.Name),
                ColName = userCol.DisplayName,
                ColType = MapUserType(userCol.Type),
                IsRequired = userCol.Required,
                IsIndexed = userCol.Indexed,
                IsUnique = userCol.Unique,
                DefaultValue = userCol.DefaultValue,
                ValidationRule = BuildValidationRule(userCol)
            };
            
            systemColumns.Add(colDef);
        }
        
        // Create table template
        var template = new TableTemplate
        {
            Name = $"UserTable_{tableName}",
            Title = title,
            Tags = new List<string> { "user-defined", "dynamic" },
            CollectionName = $"user_table_{tableName.ToLower()}",
            Columns = systemColumns,
            IsPerWorkSet = options.ScopeToWorkSet,
            IsVisibleToUsers = true,
            IsUserEditAllowed = options.AllowEdit,
            IsUserAddAllowed = options.AllowAdd,
            IsUserDeleteAllowed = options.AllowDelete,
            IsUserQueryAllowed = true,
            MonthsRetained = options.RetentionMonths
        };
        
        // Save template
        await _contentService.SaveTableTemplate(template);
        
        // Create indexes
        await CreateTableIndexes(template);
        
        // Generate creation event
        await _eventSink.SinkAsync(new AppEvent
        {
            Topic = "Table.Created",
            OriginEntityType = "TableTemplate",
            OriginTemplate = template.Name,
            EntityPayload = template.ToBsonDocument()
        });
        
        return template;
    }
    
    // Import CSV data
    public async Task<ImportResult> ImportCSVData(
        Guid currentUser,
        string tableTemplate,
        Stream csvStream,
        ImportOptions options)
    {
        var template = await LoadTableTemplate(tableTemplate);
        var result = new ImportResult();
        
        using (var reader = new StreamReader(csvStream))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            // Map CSV columns to table columns
            csv.Read();
            csv.ReadHeader();
            var columnMap = MapCSVColumns(csv.HeaderRecord, template.Columns);
            
            // Process rows in batches
            var batch = new List<TableRowData>();
            
            while (csv.Read())
            {
                try
                {
                    var rowData = new JObject();
                    
                    foreach (var (csvCol, tableCol) in columnMap)
                    {
                        var value = csv.GetField(csvCol);
                        rowData[tableCol.ColKey] = ConvertValue(value, tableCol.ColType);
                    }
                    
                    var row = new TableRowData();
                    row.SetProperties(rowData, template);
                    batch.Add(row);
                    
                    if (batch.Count >= options.BatchSize)
                    {
                        await SaveBatch(batch, template);
                        result.SuccessCount += batch.Count;
                        batch.Clear();
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        Row = csv.Parser.Row,
                        Error = ex.Message
                    });
                    
                    if (!options.ContinueOnError)
                        break;
                }
            }
            
            // Save final batch
            if (batch.Any())
            {
                await SaveBatch(batch, template);
                result.SuccessCount += batch.Count;
            }
        }
        
        return result;
    }
}
```

### Best Practices

1. **Design column indexes carefully** - They significantly impact query performance
2. **Use appropriate data types** - Leverage typed columns for better querying
3. **Implement data retention** - Manage table growth with retention policies
4. **Scope tables appropriately** - Use WorkSet/WorkItem scoping for multi-tenancy
5. **Create registered queries** - Encapsulate complex queries for reuse
6. **Use summarizations** - Pre-define aggregations for reporting
7. **Validate data on insert** - Enforce data quality at entry point
8. **Map fields explicitly** - Use mapping configurations for ETL operations
9. **Monitor table growth** - Track metadata for capacity planning
10. **Leverage tags** - Enable flexible categorization and filtering

---

## ManagedFile System

The ManagedFile System provides comprehensive file management capabilities including storage, versioning, access control, auditing, and automatic cleanup. It integrates with the entity system to support attachments across all entity types.

### Core Components

#### ManagedFileInstance
Represents a managed file with metadata:

```csharp
public class ManagedFileInstance : IAppEntity
{
    // Standard IAppEntity properties...
    
    // File metadata
    public string OriginalFileName { get; set; }    // Original name
    public string StoredFileName { get; set; }      // Storage name
    public string ContentType { get; set; }         // MIME type
    public long SizeInBytes { get; set; }           // File size
    
    // Storage location
    public string StoragePath { get; set; }         // Relative path
    public FileStorageLocation Location { get; set; } // Storage type
    
    // Access control
    public FileAccessLevel AccessLevel { get; set; } // Access permissions
    public List<Guid> AllowedUsers { get; set; } = new(); // Specific users
    public List<string> AllowedRoles { get; set; } = new(); // Role access
    
    // Lifecycle
    public DateTime? ExpiresAt { get; set; }        // Expiration date
    public bool IsTemporary { get; set; }           // Temporary flag
    public int DownloadCount { get; set; }          // Access count
    public DateTime? LastAccessedAt { get; set; }   // Last access
    
    // Versioning
    public int FileVersion { get; set; } = 1;       // Version number
    public Guid? PreviousVersionId { get; set; }    // Previous version
    
    // Security
    public string? ChecksumSHA256 { get; set; }     // File hash
    public bool IsEncrypted { get; set; }           // Encryption flag
    public string? EncryptionKeyId { get; set; }    // Key reference
    
    // Processing
    public FileProcessingStatus ProcessingStatus { get; set; }
    public JObject? ProcessingMetadata { get; set; } // Processing data
    public string? ThumbnailPath { get; set; }      // Thumbnail location
}

public enum FileStorageLocation
{
    LocalFileSystem = 0,
    BlobStorage = 1,
    S3 = 2,
    Database = 3
}

public enum FileAccessLevel
{
    Private = 0,      // Creator only
    Internal = 1,     // Authenticated users
    Public = 2,       // Anyone with link
    Restricted = 3    // Specific users/roles
}

public enum FileProcessingStatus
{
    None = 0,
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}
```

#### ManagedFileAudit
Audit trail for file operations:

```csharp
public class ManagedFileAudit : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    public Guid FileId { get; set; }               // File reference
    public FileAuditAction Action { get; set; }     // Action type
    public Guid Actor { get; set; }                 // Who performed
    public DateTime Timestamp { get; set; }         // When
    public string? IpAddress { get; set; }          // From where
    public string? UserAgent { get; set; }          // Client info
    public JObject? Metadata { get; set; }          // Additional data
}

public enum FileAuditAction
{
    Created = 0,
    Downloaded = 1,
    Updated = 2,
    Deleted = 3,
    Shared = 4,
    PermissionChanged = 5,
    Expired = 6,
    Restored = 7
}
```

#### AttachmentReference
Simplified reference for entity attachments:

```csharp
public class AttachmentReference
{
    public string Url { get; set; }                // Access URL
    public string Name { get; set; }               // Display name
    public string ContentType { get; set; }        // MIME type
    public int SizeInBytes { get; set; }           // File size
    public DateTime UploadedAt { get; set; }       // Upload time
    public Guid UploadedBy { get; set; }           // Uploader
}
```

### File Services

#### ManagedFileLogic
Primary service for file operations:

```csharp
public class ManagedFileLogic
{
    // Upload a new file
    public async Task<ManagedFileInstance> UploadFile(
        Guid currentUser,
        Stream fileStream,
        string fileName,
        string contentType,
        FileUploadOptions options)
    {
        // Validate file
        await ValidateFile(fileStream, fileName, contentType, options);
        
        // Generate storage name
        var storedFileName = GenerateStorageFileName(fileName);
        var storagePath = GenerateStoragePath(options);
        
        // Calculate checksum
        var checksum = await CalculateChecksum(fileStream);
        fileStream.Position = 0;
        
        // Check for duplicates
        if (options.PreventDuplicates)
        {
            var duplicate = await FindDuplicateByChecksum(checksum);
            if (duplicate != null)
            {
                return duplicate;
            }
        }
        
        // Create file record
        var file = new ManagedFileInstance
        {
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            SizeInBytes = fileStream.Length,
            StoragePath = storagePath,
            Location = options.StorageLocation,
            AccessLevel = options.AccessLevel,
            Creator = currentUser,
            LastModifier = currentUser,
            ChecksumSHA256 = checksum,
            IsTemporary = options.IsTemporary,
            ExpiresAt = options.ExpiresAt,
            HostWorkSet = options.WorkSet,
            HostWorkItem = options.WorkItem,
            Tags = options.Tags ?? new List<string>()
        };
        
        using (var tc = await _fileRepo.OpenTransactionAsync())
        {
            // Save file record
            await _fileRepo.CreateAsync(tc, file);
            
            // Store physical file
            try
            {
                await _storage.StoreFileAsync(
                    storagePath, 
                    storedFileName, 
                    fileStream,
                    options.EncryptionKey);
                    
                file.IsEncrypted = options.EncryptionKey != null;
                if (file.IsEncrypted)
                {
                    file.EncryptionKeyId = options.EncryptionKeyId;
                }
            }
            catch
            {
                await tc.AbortAsync();
                throw;
            }
            
            // Process file if needed
            if (options.ProcessFile)
            {
                file.ProcessingStatus = FileProcessingStatus.Pending;
                await QueueFileProcessing(file.Id, options.ProcessingOptions);
            }
            
            // Generate thumbnail for images
            if (IsImageFile(contentType))
            {
                try
                {
                    file.ThumbnailPath = await GenerateThumbnail(
                        storagePath, 
                        storedFileName);
                }
                catch
                {
                    // Thumbnail generation is optional
                }
            }
            
            await _fileRepo.UpdateAsync(tc, file);
            
            // Create audit entry
            await CreateAuditEntry(tc, file.Id, FileAuditAction.Created, currentUser);
            
            // Generate event
            await _eventSink.SinkAsync(tc, new AppEvent
            {
                Topic = "ManagedFile.Created",
                OriginEntityType = "ManagedFile",
                OriginId = file.Id,
                EntityPayload = file.ToBsonDocument()
            });
            
            await tc.CommitAsync();
        }
        
        return file;
    }
    
    // Download file
    public async Task<FileDownloadResult> DownloadFile(
        Guid currentUser,
        Guid fileId,
        bool incrementCount = true)
    {
        var file = await LoadFile(fileId);
        
        // Check access
        await VerifyFileAccess(currentUser, file);
        
        // Get file stream
        var stream = await _storage.GetFileStreamAsync(
            file.StoragePath,
            file.StoredFileName,
            file.IsEncrypted ? file.EncryptionKeyId : null);
            
        if (incrementCount)
        {
            // Update access tracking
            file.DownloadCount++;
            file.LastAccessedAt = DateTime.UtcNow;
            await _fileRepo.UpdateIgnoreVersionAsync(file);
            
            // Audit download
            await CreateAuditEntry(
                fileId: file.Id, 
                action: FileAuditAction.Downloaded, 
                actor: currentUser);
        }
        
        return new FileDownloadResult
        {
            Stream = stream,
            FileName = file.OriginalFileName,
            ContentType = file.ContentType,
            SizeInBytes = file.SizeInBytes
        };
    }
    
    // Create new version
    public async Task<ManagedFileInstance> CreateNewVersion(
        Guid currentUser,
        Guid fileId,
        Stream newFileStream,
        string? newFileName = null)
    {
        var currentVersion = await LoadFile(fileId);
        
        // Verify update permission
        if (currentVersion.Creator != currentUser && 
            !await HasAdminAccess(currentUser))
        {
            throw new UnauthorizedException("Cannot update file");
        }
        
        // Create new version
        var newVersion = new ManagedFileInstance
        {
            // Copy metadata
            OriginalFileName = newFileName ?? currentVersion.OriginalFileName,
            ContentType = currentVersion.ContentType,
            AccessLevel = currentVersion.AccessLevel,
            AllowedUsers = new List<Guid>(currentVersion.AllowedUsers),
            AllowedRoles = new List<string>(currentVersion.AllowedRoles),
            HostWorkSet = currentVersion.HostWorkSet,
            HostWorkItem = currentVersion.HostWorkItem,
            Tags = new List<string>(currentVersion.Tags),
            
            // New version info
            FileVersion = currentVersion.FileVersion + 1,
            PreviousVersionId = currentVersion.Id,
            Creator = currentUser,
            LastModifier = currentUser,
            
            // New file data
            SizeInBytes = newFileStream.Length,
            StoredFileName = GenerateStorageFileName(
                newFileName ?? currentVersion.OriginalFileName),
            StoragePath = currentVersion.StoragePath,
            Location = currentVersion.Location
        };
        
        // Upload new version
        var uploadOptions = new FileUploadOptions
        {
            StorageLocation = currentVersion.Location,
            AccessLevel = currentVersion.AccessLevel,
            WorkSet = currentVersion.HostWorkSet,
            WorkItem = currentVersion.HostWorkItem,
            Tags = currentVersion.Tags.ToArray()
        };
        
        return await UploadFile(
            currentUser, 
            newFileStream, 
            newVersion.OriginalFileName,
            newVersion.ContentType,
            uploadOptions);
    }
    
    // Share file
    public async Task<FileSharingResult> ShareFile(
        Guid currentUser,
        Guid fileId,
        FileSharingOptions options)
    {
        var file = await LoadFile(fileId);
        
        // Verify sharing permission
        await VerifyFileAccess(currentUser, file, requireOwner: true);
        
        using (var tc = await _fileRepo.OpenTransactionAsync())
        {
            // Update access level
            if (options.AccessLevel.HasValue)
            {
                file.AccessLevel = options.AccessLevel.Value;
            }
            
            // Add specific users
            if (options.ShareWithUsers != null)
            {
                foreach (var userId in options.ShareWithUsers)
                {
                    if (!file.AllowedUsers.Contains(userId))
                    {
                        file.AllowedUsers.Add(userId);
                    }
                }
            }
            
            // Add roles
            if (options.ShareWithRoles != null)
            {
                foreach (var role in options.ShareWithRoles)
                {
                    if (!file.AllowedRoles.Contains(role))
                    {
                        file.AllowedRoles.Add(role);
                    }
                }
            }
            
            // Set expiration
            if (options.ExpiresAt.HasValue)
            {
                file.ExpiresAt = options.ExpiresAt.Value;
            }
            
            file.LastModifier = currentUser;
            file.UpdatedDate = DateTime.UtcNow;
            
            await _fileRepo.UpdateAsync(tc, file);
            
            // Generate sharing link
            var shareLink = GenerateShareLink(file, options);
            
            // Audit sharing
            await CreateAuditEntry(tc, file.Id, FileAuditAction.Shared, currentUser,
                metadata: JObject.FromObject(new
                {
                    sharedWith = options.ShareWithUsers,
                    roles = options.ShareWithRoles,
                    expiresAt = options.ExpiresAt
                }));
                
            // Send notifications if requested
            if (options.NotifyUsers && options.ShareWithUsers != null)
            {
                foreach (var userId in options.ShareWithUsers)
                {
                    await _notificationService.NotifyFileShared(
                        userId, file, currentUser, shareLink);
                }
            }
            
            await tc.CommitAsync();
            
            return new FileSharingResult
            {
                ShareLink = shareLink,
                ExpiresAt = options.ExpiresAt
            };
        }
    }
}
```

#### PhysicalFilePersistence
Handles actual file storage:

```csharp
public interface IPhysicalFilePersistence
{
    Task StoreFileAsync(
        string path, 
        string fileName, 
        Stream fileStream,
        string? encryptionKey = null);
        
    Task<Stream> GetFileStreamAsync(
        string path, 
        string fileName,
        string? encryptionKeyId = null);
        
    Task DeleteFileAsync(string path, string fileName);
    
    Task<bool> FileExistsAsync(string path, string fileName);
    
    Task<long> GetFileSizeAsync(string path, string fileName);
}

public class LocalFileSystemPersistence : IPhysicalFilePersistence
{
    private readonly string _rootPath;
    private readonly IEncryptionService _encryption;
    
    public async Task StoreFileAsync(
        string path, 
        string fileName, 
        Stream fileStream,
        string? encryptionKey = null)
    {
        var fullPath = Path.Combine(_rootPath, path);
        Directory.CreateDirectory(fullPath);
        
        var filePath = Path.Combine(fullPath, fileName);
        
        using (var fileOut = File.Create(filePath))
        {
            if (!string.IsNullOrEmpty(encryptionKey))
            {
                using (var cryptoStream = _encryption.CreateEncryptStream(
                    fileOut, encryptionKey))
                {
                    await fileStream.CopyToAsync(cryptoStream);
                }
            }
            else
            {
                await fileStream.CopyToAsync(fileOut);
            }
        }
    }
    
    public async Task<Stream> GetFileStreamAsync(
        string path, 
        string fileName,
        string? encryptionKeyId = null)
    {
        var filePath = Path.Combine(_rootPath, path, fileName);
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {fileName}");
            
        var fileStream = File.OpenRead(filePath);
        
        if (!string.IsNullOrEmpty(encryptionKeyId))
        {
            var key = await _encryption.GetKey(encryptionKeyId);
            return _encryption.CreateDecryptStream(fileStream, key);
        }
        
        return fileStream;
    }
}
```

#### ManagedFileGroomingService
Background service for file cleanup:

```csharp
public class ManagedFileGroomingService : IHostedService
{
    private Timer? _timer;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Run every 4 hours
        _timer = new Timer(
            GroomFiles, 
            null, 
            TimeSpan.Zero, 
            TimeSpan.FromHours(4));
            
        return Task.CompletedTask;
    }
    
    private async void GroomFiles(object? state)
    {
        try
        {
            await GroomExpiredFiles();
            await GroomOrphanedFiles();
            await GroomOldVersions();
            await GroomLargeFiles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error grooming files");
        }
    }
    
    private async Task GroomExpiredFiles()
    {
        var (expiredFiles, _) = await _fileRepo.GetAsync(
            f => f.ExpiresAt != null && f.ExpiresAt < DateTime.UtcNow);
            
        foreach (var file in expiredFiles)
        {
            try
            {
                await _fileLogic.DeleteFile(
                    systemUser: Guid.Empty, 
                    fileId: file.Id,
                    reason: "Expired");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete expired file {file.Id}");
            }
        }
    }
    
    private async Task GroomOrphanedFiles()
    {
        // Find files not referenced by any entity
        var cutoffDate = DateTime.UtcNow.AddDays(-7);
        
        var (orphanedFiles, _) = await _fileRepo.GetAsync(
            f => f.IsTemporary && 
                 f.CreatedDate < cutoffDate &&
                 f.DownloadCount == 0);
                 
        foreach (var file in orphanedFiles)
        {
            var isReferenced = await CheckIfReferenced(file.Id);
            if (!isReferenced)
            {
                await _fileLogic.DeleteFile(
                    systemUser: Guid.Empty,
                    fileId: file.Id,
                    reason: "Orphaned");
            }
        }
    }
}
```

### File Processing

#### FileProcessor
Processes uploaded files:

```csharp
public class FileProcessor
{
    public async Task ProcessFile(
        Guid fileId,
        FileProcessingOptions options)
    {
        var file = await LoadFile(fileId);
        
        try
        {
            file.ProcessingStatus = FileProcessingStatus.Processing;
            await _fileRepo.UpdateAsync(file);
            
            var metadata = new JObject();
            
            // Virus scanning
            if (options.ScanForViruses)
            {
                var scanResult = await _virusScanner.ScanFile(
                    file.StoragePath, 
                    file.StoredFileName);
                    
                metadata["virusScan"] = JObject.FromObject(scanResult);
                
                if (scanResult.IsInfected)
                {
                    await QuarantineFile(file);
                    return;
                }
            }
            
            // Image processing
            if (IsImageFile(file.ContentType))
            {
                var imageInfo = await ProcessImage(file, options);
                metadata["image"] = JObject.FromObject(imageInfo);
            }
            
            // Document processing
            if (IsDocumentFile(file.ContentType))
            {
                var docInfo = await ProcessDocument(file, options);
                metadata["document"] = JObject.FromObject(docInfo);
            }
            
            // Video processing
            if (IsVideoFile(file.ContentType))
            {
                var videoInfo = await ProcessVideo(file, options);
                metadata["video"] = JObject.FromObject(videoInfo);
            }
            
            file.ProcessingStatus = FileProcessingStatus.Completed;
            file.ProcessingMetadata = metadata;
        }
        catch (Exception ex)
        {
            file.ProcessingStatus = FileProcessingStatus.Failed;
            file.ProcessingMetadata = JObject.FromObject(new
            {
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
        
        await _fileRepo.UpdateAsync(file);
    }
    
    private async Task<ImageProcessingResult> ProcessImage(
        ManagedFileInstance file,
        FileProcessingOptions options)
    {
        using (var stream = await GetFileStream(file))
        using (var image = Image.Load(stream))
        {
            var result = new ImageProcessingResult
            {
                Width = image.Width,
                Height = image.Height,
                Format = image.Metadata.DecodedImageFormat?.Name,
                HasTransparency = HasAlphaChannel(image)
            };
            
            // Generate thumbnails
            if (options.GenerateThumbnails)
            {
                var sizes = new[] { 150, 300, 600 };
                
                foreach (var size in sizes)
                {
                    var thumbnail = await GenerateThumbnail(image, size);
                    var thumbPath = await StoreThumbnail(file, thumbnail, size);
                    
                    result.Thumbnails.Add(new ThumbnailInfo
                    {
                        Size = size,
                        Path = thumbPath
                    });
                }
            }
            
            // Extract metadata
            if (options.ExtractMetadata)
            {
                result.Metadata = ExtractImageMetadata(image);
            }
            
            return result;
        }
    }
}
```

### File Examples

#### Creating a Document Management System
```csharp
public class DocumentManagementService
{
    // Upload document with versioning
    public async Task<DocumentUploadResult> UploadDocument(
        Guid userId,
        Stream documentStream,
        string fileName,
        string documentType,
        Guid? folderId = null,
        Dictionary<string, string>? metadata = null)
    {
        // Determine content type
        var contentType = GetContentType(fileName);
        
        // Check if document exists
        var existingDoc = await FindExistingDocument(fileName, folderId);
        
        ManagedFileInstance file;
        
        if (existingDoc != null)
        {
            // Create new version
            file = await _fileLogic.CreateNewVersion(
                userId,
                existingDoc.Id,
                documentStream,
                fileName);
        }
        else
        {
            // Upload new document
            var options = new FileUploadOptions
            {
                StorageLocation = FileStorageLocation.BlobStorage,
                AccessLevel = FileAccessLevel.Internal,
                WorkSet = folderId,
                Tags = new[] { "document", documentType },
                ProcessFile = true,
                ProcessingOptions = new FileProcessingOptions
                {
                    ExtractMetadata = true,
                    ScanForViruses = true,
                    ExtractText = IsTextExtractable(contentType)
                }
            };
            
            file = await _fileLogic.UploadFile(
                userId,
                documentStream,
                fileName,
                contentType,
                options);
        }
        
        // Store additional metadata
        if (metadata != null)
        {
            await StoreDocumentMetadata(file.Id, metadata);
        }
        
        // Create document record
        var document = new Document
        {
            FileId = file.Id,
            Name = fileName,
            Type = documentType,
            FolderId = folderId,
            Version = file.FileVersion,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            Metadata = metadata
        };
        
        await _documentRepo.CreateAsync(document);
        
        return new DocumentUploadResult
        {
            DocumentId = document.Id,
            FileId = file.Id,
            Version = file.FileVersion,
            IsNewDocument = existingDoc == null
        };
    }
    
    // Create document workflow
    public async Task<WorkflowResult> CreateDocumentWorkflow(
        Guid documentId,
        string workflowType,
        List<Guid> approvers)
    {
        var document = await LoadDocument(documentId);
        var file = await _fileLogic.LoadFile(document.FileId);
        
        // Lock document during workflow
        file.AccessLevel = FileAccessLevel.Restricted;
        file.AllowedUsers = approvers;
        await _fileRepo.UpdateAsync(file);
        
        // Create workflow work item
        var workItem = await _workItemLogic.CreateWorkItem(
            templateName: $"{workflowType}Workflow",
            workSet: file.HostWorkSet,
            title: $"{workflowType} - {document.Name}",
            description: $"Approval required for {document.Name}",
            creationData: new WorkItemCreationData
            {
                Sections = new Dictionary<string, JObject>
                {
                    ["DocumentInfo"] = JObject.FromObject(new
                    {
                        documentId = documentId,
                        fileId = file.Id,
                        documentName = document.Name,
                        documentType = document.Type,
                        version = file.FileVersion
                    }),
                    ["ApprovalTracking"] = JObject.FromObject(new
                    {
                        approvers = approvers,
                        approved = new List<Guid>(),
                        rejected = new List<Guid>(),
                        comments = new List<object>()
                    })
                }
            }
        );
        
        // Notify approvers
        foreach (var approver in approvers)
        {
            await _notificationService.SendNotification(new NotificationMessage
            {
                UserId = approver,
                Subject = $"Document Approval Required: {document.Name}",
                EmailText = $"Please review and approve the document: {document.Name}",
                ToastText = "New document awaiting approval",
                ActionUrl = $"/workflow/{workItem.Id}"
            });
        }
        
        return new WorkflowResult
        {
            WorkflowId = workItem.Id,
            DocumentId = documentId,
            Status = "Pending"
        };
    }
}
```

#### Implementing File Sharing Portal
```csharp
public class FileSharingPortalService
{
    // Create share link with options
    public async Task<ShareLinkResult> CreateShareLink(
        Guid userId,
        Guid fileId,
        ShareLinkOptions options)
    {
        var file = await _fileLogic.LoadFile(fileId);
        
        // Verify user can share
        await VerifySharePermission(userId, file);
        
        // Create share record
        var shareLink = new FileShareLink
        {
            FileId = fileId,
            ShareCode = GenerateShareCode(),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = options.ExpiresAt,
            MaxDownloads = options.MaxDownloads,
            RequirePassword = options.Password != null,
            PasswordHash = HashPassword(options.Password),
            AllowedEmails = options.AllowedEmails,
            TrackDownloads = options.TrackDownloads,
            ShowPreview = options.ShowPreview
        };
        
        await _shareLinkRepo.CreateAsync(shareLink);
        
        // Update file sharing options
        await _fileLogic.ShareFile(userId, fileId, new FileSharingOptions
        {
            AccessLevel = FileAccessLevel.Public,
            ExpiresAt = options.ExpiresAt
        });
        
        var url = $"{_baseUrl}/share/{shareLink.ShareCode}";
        
        return new ShareLinkResult
        {
            ShareUrl = url,
            ShareCode = shareLink.ShareCode,
            ExpiresAt = shareLink.ExpiresAt
        };
    }
    
    // Handle shared file access
    public async Task<FileAccessResult> AccessSharedFile(
        string shareCode,
        string? password = null,
        string? accessorEmail = null)
    {
        var shareLink = await LoadShareLink(shareCode);
        
        // Validate share link
        if (shareLink.ExpiresAt.HasValue && 
            shareLink.ExpiresAt.Value < DateTime.UtcNow)
        {
            throw new ValidationException("Share link has expired");
        }
        
        if (shareLink.MaxDownloads.HasValue && 
            shareLink.DownloadCount >= shareLink.MaxDownloads.Value)
        {
            throw new ValidationException("Download limit reached");
        }
        
        // Verify password
        if (shareLink.RequirePassword)
        {
            if (string.IsNullOrEmpty(password) || 
                !VerifyPassword(password, shareLink.PasswordHash))
            {
                throw new UnauthorizedException("Invalid password");
            }
        }
        
        // Verify email restriction
        if (shareLink.AllowedEmails != null && shareLink.AllowedEmails.Any())
        {
            if (string.IsNullOrEmpty(accessorEmail) || 
                !shareLink.AllowedEmails.Contains(accessorEmail))
            {
                throw new UnauthorizedException("Email not authorized");
            }
        }
        
        // Load file
        var file = await _fileLogic.LoadFile(shareLink.FileId);
        
        // Track access
        if (shareLink.TrackDownloads)
        {
            await TrackShareAccess(shareLink, accessorEmail);
        }
        
        // Update download count
        shareLink.DownloadCount++;
        shareLink.LastAccessedAt = DateTime.UtcNow;
        await _shareLinkRepo.UpdateAsync(shareLink);
        
        return new FileAccessResult
        {
            File = file,
            AllowDownload = true,
            AllowPreview = shareLink.ShowPreview,
            WatermarkText = shareLink.WatermarkText
        };
    }
}
```
