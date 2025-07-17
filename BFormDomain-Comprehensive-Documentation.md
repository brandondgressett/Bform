# BFormDomain Comprehensive Documentation

## Table of Contents

### Core Systems
1. [Entity System](#entity-system)
2. [Forms System](#forms-system)
3. [WorkItems System](#workitems-system)
4. [WorkSets System](#worksets-system)
5. [Tables System](#tables-system)
6. [KPI System](#kpi-system)
7. [Reports System](#reports-system)
8. [Rules Engine](#rules-engine)
9. [AppEvents System](#appevents-system)
10. [Authorization System](#authorization-system)
11. [Scheduler System](#scheduler-system)
12. [Notification System](#notification-system)
13. [Comments System](#comments-system)
14. [ManagedFile System](#managedfile-system)
15. [Content System](#content-system)
16. [Tags System](#tags-system)
17. [Terminology System](#terminology-system)
18. [ApplicationTopology System](#applicationtopology-system)
19. [HtmlEntity System](#htmlentity-system)

### Infrastructure
20. [Repository Infrastructure](#repository-infrastructure)
21. [MessageBus Infrastructure](#messagebus-infrastructure)
22. [SimilarEntityTracking System](#similarentitytracking-system)
23. [Diagnostics System](#diagnostics-system)
24. [Validation Framework](#validation-framework)
25. [Utility Components](#utility-components)

### Architecture
26. [Plugin Architecture and Extension Points](#plugin-architecture)
27. [Dynamic vs Application-Specific Data Patterns](#dynamic-vs-application-specific)

---

## Entity System

The Entity System provides the foundational abstraction for all domain objects in BFormDomain. It establishes a common interface and behavior for entities while enabling extensibility through plugins and reference mechanisms.

### Core Components

#### IAppEntity Interface
The central interface that all domain entities implement:

```csharp
public interface IAppEntity : IDataModel, ITaggable
{
    // Entity metadata
    string EntityType { get; set; }      // Type identifier (e.g., "Form", "WorkItem")
    string Template { get; set; }        // Template name that defines structure
    
    // Timestamps
    DateTime CreatedDate { get; set; }   // UTC creation time
    DateTime UpdatedDate { get; set; }   // UTC last modification time
    
    // User tracking
    Guid? Creator { get; set; }          // User who created the entity
    Guid? LastModifier { get; set; }     // User who last modified the entity
    
    // Container references
    Guid? HostWorkSet { get; set; }      // Parent WorkSet container
    Guid? HostWorkItem { get; set; }     // Parent WorkItem container
    
    // Scheduling
    List<string> AttachedSchedules { get; set; }  // Attached scheduled events
    
    // Operations
    bool Tagged(params string[] anyTags);  // Check if entity has any of the tags
    JObject ToJson();                      // Convert to JSON representation
    Uri MakeReference(bool template = false, bool vm = false, 
                      string? queryParameters = null);  // Generate URI reference
}
```

#### IEntityLoaderModule Interface
Plugin interface for loading entities from URIs:

```csharp
public interface IEntityLoaderModule
{
    bool CanLoad(string uri);
    Task<JObject?> LoadJson(string uri, string? tzid = null);
}
```

#### IEntityReferenceBuilder Interface
Service for building entity references based on entity type:

```csharp
public interface IEntityReferenceBuilder
{
    Uri MakeReference(string templateName, Guid id, bool template = false, 
                      bool vm = false, string? queryParameters = null);
}
```

### Implementation Classes

#### EntityReferenceLoader
Central service that coordinates entity loading through registered modules:

```csharp
public class EntityReferenceLoader
{
    private readonly IEnumerable<IEntityLoaderModule> _modules;
    
    public async Task<JObject?> LoadJson(string uri, string? tzid = null)
    {
        // Find appropriate module for URI
        var module = _modules.FirstOrDefault(m => m.CanLoad(uri));
        if (module == null) return null;
        
        // Delegate to module
        return await module.LoadJson(uri, tzid);
    }
}
```

#### EntityWrapper Classes
Provide view model representations for entities:

- `AppEntityVM` - Base view model with common properties
- `AppEntityListWrap<T>` - Wrapper for entity lists
- `TitledAppEntityVM` - Adds title property for display

### Entity Attachment System

#### IEntityAttachmentManager Interface
Manages attachments for entities:

```csharp
public interface IEntityAttachmentManager
{
    Task<AttachmentReference> CreateOrUpdateAttachmentAsync(
        ITransactionContext tc,
        ManagedFileAction action,
        string? existingUrl = null);
        
    Task RemoveAttachmentAsync(
        ITransactionContext tc,
        string existingUrl);
}
```

#### AttachmentReference Model
Represents a reference to an attached file:

```csharp
public class AttachmentReference
{
    public string Url { get; set; }        // Access URL
    public string Name { get; set; }       // Display name
    public string ContentType { get; set; } // MIME type
    public int SizeInBytes { get; set; }   // File size
}
```

### Usage Examples

#### Creating a Custom Entity
```csharp
public class CustomEntity : IAppEntity
{
    // IDataModel properties
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Version { get; set; } = 0;
    
    // IAppEntity properties
    public string EntityType { get; set; } = "CustomEntity";
    public string Template { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }
    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
    public List<string> AttachedSchedules { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    
    // Custom properties
    public string CustomProperty { get; set; }
    
    // IAppEntity methods
    public bool Tagged(params string[] anyTags) => 
        Tags.Any(t => anyTags.Contains(t, StringComparer.OrdinalIgnoreCase));
        
    public JObject ToJson() => JObject.FromObject(this);
    
    public Uri MakeReference(bool template = false, bool vm = false, 
                            string? queryParameters = null)
    {
        var builder = new UriBuilder($"entity://CustomEntity/{Id}");
        var query = new List<string>();
        
        if (template) query.Add("template=true");
        if (vm) query.Add("vm=true");
        if (!string.IsNullOrEmpty(queryParameters)) query.Add(queryParameters);
        
        if (query.Any())
            builder.Query = string.Join("&", query);
            
        return builder.Uri;
    }
}
```

#### Creating an Entity Loader Module
```csharp
public class CustomEntityLoaderModule : IEntityLoaderModule
{
    private readonly IRepository<CustomEntity> _repository;
    
    public bool CanLoad(string uri) => 
        uri.StartsWith("entity://CustomEntity/", StringComparison.OrdinalIgnoreCase);
    
    public async Task<JObject?> LoadJson(string uri, string? tzid = null)
    {
        var uriObj = new Uri(uri);
        var segments = uriObj.AbsolutePath.Split('/');
        
        if (segments.Length < 2 || !Guid.TryParse(segments[1], out var id))
            return null;
            
        var (entities, _) = await _repository.GetAsync(e => e.Id == id);
        var entity = entities.FirstOrDefault();
        
        if (entity == null) return null;
        
        var json = entity.ToJson();
        
        // Apply timezone conversion if needed
        if (!string.IsNullOrEmpty(tzid))
        {
            ConvertDatesToTimezone(json, tzid);
        }
        
        return json;
    }
}
```

#### Working with Entity References
```csharp
// Building a reference
var entityRef = entity.MakeReference(vm: true);
// Result: entity://Form/123e4567-e89b-12d3-a456-426614174000?vm=true

// Loading from reference
var loader = serviceProvider.GetRequiredService<EntityReferenceLoader>();
var entityJson = await loader.LoadJson(entityRef.ToString());

// Using in rule actions
public class RuleActionLoadEntity : IRuleActionEvaluator
{
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var entityRef = args["entityRef"]?.ToString();
        if (string.IsNullOrEmpty(entityRef)) return eventData;
        
        var entityData = await _loader.LoadJson(entityRef);
        if (entityData != null)
        {
            eventData["loadedEntity"] = entityData;
        }
        
        return eventData;
    }
}
```

### Key Design Patterns

1. **Interface Segregation**: `IAppEntity` combines `IDataModel` and `ITaggable` for specific concerns
2. **Plugin Pattern**: `IEntityLoaderModule` allows new entity types to register loading logic
3. **URI-based References**: Entities use URIs for platform-agnostic references
4. **View Model Separation**: EntityWrapper classes provide UI-specific representations

### Best Practices

1. **Always implement IAppEntity** for domain entities to ensure consistency
2. **Use EntityReferenceLoader** instead of direct repository access when loading cross-entity references
3. **Register entity loader modules** in dependency injection for new entity types
4. **Include timezone parameter** when loading entities for user display
5. **Use AttachmentReference** for file associations rather than storing file data directly

---

## Forms System

The Forms System provides a dynamic, schema-driven form engine that allows end-users to define custom forms without code changes. It supports complex validation, custom actions, and integration with the broader platform through events and rules.

### Core Components

#### FormInstance
Represents a filled-out form with dynamic data:

```csharp
public class FormInstance : IAppEntity
{
    // Standard IAppEntity properties...
    
    // Form-specific properties
    public BsonDocument Content { get; set; }     // MongoDB BSON for flexibility
    public JObject JsonContent { get; set; }      // JSON view of content
    public FormInstanceHome Home { get; set; }    // Where form lives
    
    // Navigation helpers
    public Uri MakeReferenceToTemplate() => 
        MakeReference(template: true);
}

public enum FormInstanceHome
{
    None = 0,
    WorkSet = 1,
    WorkItem = 2
}
```

#### FormTemplate
Defines the structure and behavior of forms:

```csharp
public class FormTemplate : IContentType
{
    public string Name { get; set; }              // Unique identifier
    public string Title { get; set; }             // Display title
    public List<string> Tags { get; set; }        // Categorization
    
    // Schema definitions
    public SatelliteJson ContentSchema { get; set; }  // JSON Schema for validation
    public SatelliteJson UISchema { get; set; }       // UI rendering hints
    public SatelliteJson YupSchema { get; set; }      // Client-side validation
    
    // Behavior
    public List<ActionButton> ActionButtons { get; set; }  // Custom actions
    public List<ScheduledEventTemplate> Schedules { get; set; }  // Automated events
    
    // Display settings
    public bool IsVisibleToUsers { get; set; }    // User visibility
    public bool EventsOnly { get; set; }          // Only accessible via events
    public string CssClassName { get; set; }       // Custom styling
}

public class ActionButton
{
    public string ButtonKey { get; set; }         // Unique identifier
    public string Title { get; set; }             // Display text
    public Guid ActionIdentity { get; set; }      // Action ID
    public string? CssClassName { get; set; }     // Custom styling
    public JObject? ActionProperties { get; set; } // Action parameters
}
```

### Form Services

#### FormLogic
Primary service for form operations:

```csharp
public class FormLogic
{
    // Create a new form instance
    public async Task<Guid> ActionCreateForm(
        Guid currentUser,
        Guid workItemCreator,
        string workItemTemplateName,
        Guid? workSet,
        Guid? workItem,
        string formTemplateName,
        JObject? content = null,
        string[]? tags = null,
        string? name = null)
    {
        // Load template
        var template = await LoadFormTemplate(formTemplateName);
        
        // Validate content against schema
        if (content != null)
        {
            ValidateAgainstSchema(content, template.ContentSchema);
        }
        
        // Create form instance
        var form = new FormInstance
        {
            Template = formTemplateName,
            Content = content?.ToBsonDocument() ?? new BsonDocument(),
            Home = DetermineHome(workSet, workItem),
            HostWorkSet = workSet,
            HostWorkItem = workItem,
            Creator = currentUser,
            LastModifier = currentUser,
            Tags = tags?.ToList() ?? new List<string>()
        };
        
        // Apply template schedules
        form.AttachedSchedules = template.Schedules
            .Select(s => s.Name)
            .ToList();
        
        // Save to repository
        using (var tc = await _formRepo.OpenTransactionAsync())
        {
            await _formRepo.CreateAsync(tc, form);
            
            // Generate creation event
            await _eventSink.SinkAsync(tc, new AppEvent
            {
                Topic = "Form.Created",
                OriginEntityType = "Form",
                OriginTemplate = formTemplateName,
                OriginId = form.Id,
                EntityPayload = form.ToBsonDocument()
            });
            
            await tc.CommitAsync();
        }
        
        return form.Id;
    }
    
    // Update form content
    public async Task ActionUpdateFormContent(
        Guid currentUser,
        Guid formId,
        JObject updates,
        bool merge = true)
    {
        using (var tc = await _formRepo.OpenTransactionAsync())
        {
            // Load existing form
            var (forms, _) = await _formRepo.GetAsync(tc, f => f.Id == formId);
            var form = forms.FirstOrDefault();
            
            if (form == null)
                throw new NotFoundException($"Form {formId} not found");
            
            // Load template for validation
            var template = await LoadFormTemplate(form.Template);
            
            // Merge or replace content
            var newContent = merge 
                ? MergeJson(form.JsonContent, updates)
                : updates;
            
            // Validate against schema
            ValidateAgainstSchema(newContent, template.ContentSchema);
            
            // Update form
            form.Content = newContent.ToBsonDocument();
            form.LastModifier = currentUser;
            form.UpdatedDate = DateTime.UtcNow;
            
            await _formRepo.UpdateAsync(tc, form);
            
            // Generate update event
            await _eventSink.SinkAsync(tc, new AppEvent
            {
                Topic = "Form.Updated",
                OriginEntityType = "Form",
                OriginTemplate = form.Template,
                OriginId = form.Id,
                EntityPayload = form.ToBsonDocument()
            });
            
            await tc.CommitAsync();
        }
    }
    
    // Execute custom form action
    public async Task<JObject> ActionInvokeCustomAction(
        Guid currentUser,
        Guid formId,
        string buttonKey,
        JObject? parameters = null)
    {
        var form = await LoadForm(formId);
        var template = await LoadFormTemplate(form.Template);
        
        var button = template.ActionButtons
            .FirstOrDefault(b => b.ButtonKey == buttonKey);
            
        if (button == null)
            throw new ValidationException($"Action {buttonKey} not found");
        
        // Merge action properties with parameters
        var actionData = new JObject
        {
            ["formId"] = formId,
            ["buttonKey"] = buttonKey,
            ["actionId"] = button.ActionIdentity,
            ["formData"] = form.JsonContent,
            ["parameters"] = parameters ?? new JObject()
        };
        
        if (button.ActionProperties != null)
        {
            actionData["actionProperties"] = button.ActionProperties;
        }
        
        // Execute action through event
        await _eventSink.SinkAsync(new AppEvent
        {
            Topic = $"Form.Action.{buttonKey}",
            OriginEntityType = "Form",
            OriginTemplate = form.Template,
            OriginId = form.Id,
            EntityPayload = actionData.ToBsonDocument()
        });
        
        return new JObject
        {
            ["success"] = true,
            ["formId"] = formId,
            ["action"] = buttonKey
        };
    }
}
```

#### FormHelperLogic
Provides shared functionality for form operations:

```csharp
public class FormHelperLogic
{
    // Get form with all related data
    public async Task<FormInstanceVM> GetFormInstanceVM(
        Guid formId,
        string? tzid = null)
    {
        var form = await LoadForm(formId);
        var template = await LoadFormTemplate(form.Template);
        
        var vm = new FormInstanceVM
        {
            // Base entity properties
            Id = form.Id,
            Version = form.Version,
            EntityType = form.EntityType,
            Template = form.Template,
            CreatedDate = ConvertToTimezone(form.CreatedDate, tzid),
            UpdatedDate = ConvertToTimezone(form.UpdatedDate, tzid),
            
            // Form properties
            Title = template.Title,
            Content = form.JsonContent,
            Home = form.Home,
            
            // UI properties
            UISchema = template.UISchema?.Data,
            ContentSchema = template.ContentSchema?.Data,
            ActionButtons = template.ActionButtons,
            CssClassName = template.CssClassName,
            
            // Related data
            Creator = await LoadUserInfo(form.Creator),
            LastModifier = await LoadUserInfo(form.LastModifier),
            Comments = await LoadComments(form.Id),
            Attachments = await LoadAttachments(form.Id),
            Tags = form.Tags
        };
        
        return vm;
    }
}
```

### Form Rule Actions

#### RuleActionCreateForm
Creates forms in response to events:

```csharp
public class RuleActionCreateForm : IRuleActionEvaluator
{
    public class Args
    {
        public string? TemplateName { get; set; }
        public string? TemplateNameQuery { get; set; }
        public string? WorkSetQuery { get; set; }
        public string? WorkItemQuery { get; set; }
        public FormInstanceHome? Home { get; set; }
        public List<string>? InitialTags { get; set; }
        public string? InitName { get; set; }
        public string? InitNameQuery { get; set; }
        public JObject? InitialProps { get; set; }
        public string? InitialPropsQuery { get; set; }
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        // Resolve template name
        var templateName = a.TemplateName ?? 
            JsonPathQuery(eventData, a.TemplateNameQuery);
            
        // Resolve container references
        var workSet = ResolveGuid(eventData, a.WorkSetQuery);
        var workItem = ResolveGuid(eventData, a.WorkItemQuery);
        
        // Resolve initial content
        var content = a.InitialProps ?? 
            (a.InitialPropsQuery != null 
                ? JsonPathQuery(eventData, a.InitialPropsQuery) as JObject
                : null);
                
        // Create form
        var formId = await _formLogic.EventCreateForm(
            workSet: workSet,
            workItem: workItem,
            formTemplateName: templateName,
            content: content,
            tags: a.InitialTags?.ToArray(),
            name: a.InitName ?? JsonPathQuery(eventData, a.InitNameQuery)
        );
        
        // Add form ID to event data
        eventData["createdFormId"] = formId;
        
        return eventData;
    }
}
```

#### RuleActionEditFormProperty
Updates specific form fields:

```csharp
public class RuleActionEditFormProperty : IRuleActionEvaluator
{
    public class Args
    {
        public string? FormIdQuery { get; set; }
        public string? PropertyPath { get; set; }
        public JToken? Value { get; set; }
        public string? ValueQuery { get; set; }
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        // Resolve form ID
        var formId = ResolveGuid(eventData, a.FormIdQuery);
        if (!formId.HasValue) return eventData;
        
        // Resolve new value
        var newValue = a.Value ?? 
            (a.ValueQuery != null 
                ? JsonPathQuery(eventData, a.ValueQuery)
                : null);
                
        // Create update object
        var updates = new JObject();
        SetJsonPath(updates, a.PropertyPath, newValue);
        
        // Update form
        await _formLogic.EventUpdateFormContent(
            formId: formId.Value,
            updates: updates,
            merge: true
        );
        
        return eventData;
    }
}
```

### Form Validation

Forms support multiple layers of validation:

1. **JSON Schema Validation** (Server-side)
```json
{
  "type": "object",
  "properties": {
    "email": {
      "type": "string",
      "format": "email"
    },
    "age": {
      "type": "integer",
      "minimum": 18,
      "maximum": 120
    }
  },
  "required": ["email", "age"]
}
```

2. **Yup Schema Validation** (Client-side)
```javascript
{
  "email": {
    "type": "string",
    "email": true,
    "required": true
  },
  "age": {
    "type": "number",
    "min": 18,
    "max": 120,
    "required": true
  }
}
```

3. **Custom Validation Rules**
```csharp
public class FormValidationRule : IValidator<FormInstance>
{
    public ValidationResult Validate(FormInstance form)
    {
        // Custom business logic validation
        if (form.JsonContent["startDate"] > form.JsonContent["endDate"])
        {
            return ValidationResult.Error("Start date must be before end date");
        }
        
        return ValidationResult.Success();
    }
}
```

### UI Schema and Rendering

The UI Schema provides hints for form rendering:

```json
{
  "ui:order": ["firstName", "lastName", "email", "age"],
  "firstName": {
    "ui:widget": "text",
    "ui:placeholder": "Enter first name"
  },
  "age": {
    "ui:widget": "updown"
  },
  "bio": {
    "ui:widget": "textarea",
    "ui:options": {
      "rows": 5
    }
  },
  "agree": {
    "ui:widget": "checkbox"
  }
}
```

### Form Examples

#### Creating a Customer Feedback Form Template
```csharp
var feedbackTemplate = new FormTemplate
{
    Name = "CustomerFeedback",
    Title = "Customer Feedback Form",
    Tags = new List<string> { "feedback", "customer-service" },
    
    ContentSchema = new SatelliteJson
    {
        Data = JObject.Parse(@"{
            'type': 'object',
            'properties': {
                'rating': {
                    'type': 'integer',
                    'minimum': 1,
                    'maximum': 5
                },
                'comment': {
                    'type': 'string',
                    'maxLength': 1000
                },
                'email': {
                    'type': 'string',
                    'format': 'email'
                },
                'followUp': {
                    'type': 'boolean'
                }
            },
            'required': ['rating', 'comment']
        }")
    },
    
    UISchema = new SatelliteJson
    {
        Data = JObject.Parse(@"{
            'rating': {
                'ui:widget': 'radio',
                'ui:options': {
                    'inline': true
                }
            },
            'comment': {
                'ui:widget': 'textarea',
                'ui:placeholder': 'Tell us about your experience...'
            },
            'followUp': {
                'ui:widget': 'checkbox',
                'ui:help': 'Check if you would like us to follow up'
            }
        }")
    },
    
    ActionButtons = new List<ActionButton>
    {
        new ActionButton
        {
            ButtonKey = "submit",
            Title = "Submit Feedback",
            ActionIdentity = Guid.NewGuid(),
            CssClassName = "btn-primary"
        },
        new ActionButton
        {
            ButtonKey = "escalate",
            Title = "Escalate to Manager",
            ActionIdentity = Guid.NewGuid(),
            CssClassName = "btn-warning",
            ActionProperties = JObject.Parse(@"{
                'requiresComment': true,
                'minRating': 3
            }")
        }
    },
    
    IsVisibleToUsers = true,
    EventsOnly = false
};
```

#### Implementing a Custom Form Action
```csharp
// Rule to handle feedback escalation
var escalationRule = new Rule
{
    Name = "HandleFeedbackEscalation",
    TopicBindings = new List<string> { "Form.Action.escalate" },
    
    Conditions = new List<RuleCondition>
    {
        new RuleCondition
        {
            Query = "$.formData.rating",
            Check = RuleConditionCheck.Single,
            Value = JToken.FromObject(new[] { 1, 2 })  // Low ratings
        }
    },
    
    Actions = new List<RuleAction>
    {
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionCreateWorkItem",
                Args = JObject.FromObject(new
                {
                    TemplateName = "CustomerComplaint",
                    TitleQuery = "$.formData.email + ' - Escalated Feedback'",
                    DescriptionQuery = "$.formData.comment",
                    Priority = 1,
                    CreationData = new
                    {
                        FormIdQuery = "$.formId",
                        RatingQuery = "$.formData.rating"
                    }
                })
            }
        },
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionRequestNotification",
                Args = JObject.FromObject(new
                {
                    GroupByTags = new[] { "customer-service-manager" },
                    Subject = "Escalated Customer Feedback",
                    EmailTextQuery = "'Customer feedback escalated with rating: ' + $.formData.rating"
                })
            }
        }
    }
};
```

### Best Practices

1. **Use JSON Schema** for robust server-side validation
2. **Provide UI Schema** for better user experience
3. **Design reusable form templates** that can serve multiple use cases
4. **Use action buttons** for complex workflows beyond simple save
5. **Leverage events** for integration with other systems
6. **Tag forms appropriately** for easy categorization and search
7. **Consider form homes** (WorkSet vs WorkItem) based on scope
8. **Implement proper error handling** in custom actions
9. **Use form schedules** for time-based processing
10. **Version form templates** carefully as changes affect existing data

---

## WorkItems System

The WorkItems System provides a comprehensive work management platform supporting tickets, tasks, cases, and any other work-tracking scenarios. It features customizable workflows, rich metadata, section-based content organization, and full audit trails.

### Core Components

#### WorkItem
The central entity representing a unit of work:

```csharp
public class WorkItem : IAppEntity
{
    // Standard IAppEntity properties...
    
    // Core properties
    public string Title { get; set; }              // Work item title
    public string? Description { get; set; }        // Detailed description
    
    // Workflow state
    public int Status { get; set; }                 // Current status code
    public int Priority { get; set; }               // Priority level
    
    // Assignment
    public Guid? UserAssignee { get; set; }         // Assigned user
    public int? TriageAssignee { get; set; }        // Triage queue assignment
    
    // Visibility
    public bool IsListed { get; set; } = true;      // Show in lists
    public bool IsVisible { get; set; } = true;     // General visibility
    
    // Tracking
    public DateTime StartUnresolved { get; set; }    // When issue began
    
    // Content organization
    public List<Section> Sections { get; set; } = new();  // Content sections
    
    // Relationships
    public List<WorkItemBookmark> Bookmarks { get; set; } = new();  // Saved references
    public List<WorkItemLink> Links { get; set; } = new();          // Related items
    
    // History
    public List<WorkItemEventHistory> EventHistory { get; set; } = new();  // Audit trail
}
```

#### WorkItemTemplate
Defines the structure and behavior of work items:

```csharp
public class WorkItemTemplate : IContentType
{
    public string Name { get; set; }                // Unique identifier
    public string Title { get; set; }               // Display name
    public List<string> Tags { get; set; }          // Categorization
    
    // Workflow configuration
    public List<StatusTemplate> StatusTemplates { get; set; }    // Available statuses
    public List<TriageTemplate> TriageTemplates { get; set; }    // Triage queues
    public List<PriorityTemplate> PriorityTemplates { get; set; } // Priority levels
    
    // Content structure
    public List<SectionTemplate> SectionTemplates { get; set; }  // Section definitions
    
    // Permissions
    public bool AllowComments { get; set; }         // Enable commenting
    public bool AllowFileAttachments { get; set; }  // Enable file uploads
    public bool AllowEditTitle { get; set; }        // Title modification
    public bool AllowEditDescription { get; set; }  // Description modification
    public bool AllowSectionEdit { get; set; }      // Section editing
    public bool AllowSectionAppend { get; set; }    // Section additions
    public bool AllowEmptySections { get; set; }    // Empty section creation
    
    // Tracking options
    public bool TrackUserHistory { get; set; }      // Track user changes
    public bool TrackAssigneeHistory { get; set; }  // Track assignments
    public bool TrackStatusHistory { get; set; }    // Track status changes
    public bool TrackPriorityHistory { get; set; }  // Track priority changes
    public bool IsAnyUserAssignable { get; set; }   // Open assignment
    
    // Display options
    public bool IsVisibleToUsers { get; set; }      // User visibility
    public bool TriageQueuesAreVisibleToUsers { get; set; }
    
    // Grooming (cleanup)
    public bool IsGroomable { get; set; }           // Enable auto-cleanup
    public TimeFrame? GroomPeriod { get; set; }     // Cleanup timeframe
    public WorkItemGroomBehavior GroomBehavior { get; set; }  // Cleanup action
}

// Supporting types
public class StatusTemplate
{
    public int Status { get; set; }                 // Status code
    public string Name { get; set; }                // Internal name
    public string Title { get; set; }               // Display name
    public string? CssClassName { get; set; }       // Styling
    public bool ConsideredResolved { get; set; }    // Resolution flag
}

public class Section
{
    public string SectionKey { get; set; }          // Section identifier
    public string EntityType { get; set; }          // Entity type in section
    public Guid EntityId { get; set; }              // Entity reference
    public string? Name { get; set; }               // Section name
}

public class WorkItemBookmark
{
    public string Title { get; set; }               // Bookmark title
    public Uri Reference { get; set; }              // Target reference
    public Guid Creator { get; set; }               // Who bookmarked
    public DateTime CreateDate { get; set; }        // When bookmarked
}

public class WorkItemLink
{
    public LinkDirection Direction { get; set; }    // Link direction
    public Guid TargetWorkItem { get; set; }        // Target work item
    public string? Title { get; set; }              // Link description
    public DateTime CreateDate { get; set; }        // When linked
}

public class WorkItemEventHistory
{
    public Guid EventId { get; set; }               // Unique event ID
    public WorkItemHistorySummary Summary { get; set; }  // Event summary
    public Guid Actor { get; set; }                 // Who performed action
    public DateTime EventDate { get; set; }         // When it happened
    public string? Details { get; set; }            // Additional details
}
```

### WorkItem Services

#### WorkItemLogic
Primary service for work item operations:

```csharp
public class WorkItemLogic
{
    // Create a new work item
    public async Task<Guid> ActionCreateWorkItem(
        Guid currentUser,
        string templateName,
        Guid? workSet,
        string title,
        string? description = null,
        bool? isListed = null,
        bool? isVisible = null,
        Guid? userAssignee = null,
        int? triageAssignee = null,
        int? status = null,
        int? priority = null,
        WorkItemCreationData? creationData = null,
        string[]? tags = null)
    {
        // Load template
        var template = await LoadWorkItemTemplate(templateName);
        
        // Create work item
        var workItem = new WorkItem
        {
            Template = templateName,
            HostWorkSet = workSet,
            Title = title,
            Description = description,
            IsListed = isListed ?? true,
            IsVisible = isVisible ?? true,
            Creator = currentUser,
            LastModifier = currentUser,
            StartUnresolved = DateTime.UtcNow,
            Tags = tags?.ToList() ?? new List<string>()
        };
        
        // Set workflow state
        workItem.Status = status ?? template.StatusTemplates.First().Status;
        workItem.Priority = priority ?? template.PriorityTemplates.First().Priority;
        
        // Set assignment
        if (userAssignee.HasValue)
        {
            workItem.UserAssignee = userAssignee.Value;
        }
        else if (triageAssignee.HasValue)
        {
            workItem.TriageAssignee = triageAssignee.Value;
        }
        
        // Create initial event history
        workItem.EventHistory.Add(new WorkItemEventHistory
        {
            EventId = Guid.NewGuid(),
            Summary = WorkItemHistorySummary.Created,
            Actor = currentUser,
            EventDate = DateTime.UtcNow,
            Details = $"Created with status: {GetStatusTitle(template, workItem.Status)}"
        });
        
        using (var tc = await _workItemRepo.OpenTransactionAsync())
        {
            // Save work item
            await _workItemRepo.CreateAsync(tc, workItem);
            
            // Create sections from template
            foreach (var sectionTemplate in template.SectionTemplates)
            {
                var sectionEntity = await CreateSectionEntity(
                    tc, 
                    sectionTemplate, 
                    workItem.Id,
                    workSet,
                    creationData
                );
                
                workItem.Sections.Add(new Section
                {
                    SectionKey = sectionTemplate.SectionKey,
                    EntityType = sectionTemplate.EntityType,
                    EntityId = sectionEntity.Id,
                    Name = sectionTemplate.Name
                });
            }
            
            // Update with sections
            await _workItemRepo.UpdateAsync(tc, workItem);
            
            // Generate creation event
            await _eventSink.SinkAsync(tc, new AppEvent
            {
                Topic = "WorkItem.Created",
                OriginEntityType = "WorkItem",
                OriginTemplate = templateName,
                OriginId = workItem.Id,
                EntityPayload = workItem.ToBsonDocument()
            });
            
            await tc.CommitAsync();
        }
        
        return workItem.Id;
    }
    
    // Update work item metadata
    public async Task ActionEditWorkItemMetadata(
        Guid currentUser,
        Guid workItemId,
        string? title = null,
        string? description = null,
        int? status = null,
        int? priority = null,
        Guid? userAssignee = null,
        int? triageAssignee = null,
        bool? isListed = null,
        bool? isVisible = null)
    {
        using (var tc = await _workItemRepo.OpenTransactionAsync())
        {
            var (items, _) = await _workItemRepo.GetAsync(tc, w => w.Id == workItemId);
            var workItem = items.FirstOrDefault();
            
            if (workItem == null)
                throw new NotFoundException($"WorkItem {workItemId} not found");
                
            var template = await LoadWorkItemTemplate(workItem.Template);
            var changes = new List<string>();
            
            // Track changes for history
            if (title != null && workItem.Title != title)
            {
                changes.Add($"Title changed from '{workItem.Title}' to '{title}'");
                workItem.Title = title;
            }
            
            if (description != null && workItem.Description != description)
            {
                changes.Add("Description updated");
                workItem.Description = description;
            }
            
            if (status.HasValue && workItem.Status != status.Value)
            {
                var oldStatus = GetStatusTitle(template, workItem.Status);
                var newStatus = GetStatusTitle(template, status.Value);
                changes.Add($"Status changed from '{oldStatus}' to '{newStatus}'");
                workItem.Status = status.Value;
                
                // Update resolution tracking
                var statusTemplate = template.StatusTemplates
                    .First(s => s.Status == status.Value);
                if (statusTemplate.ConsideredResolved && workItem.StartUnresolved != default)
                {
                    workItem.StartUnresolved = default;
                }
                else if (!statusTemplate.ConsideredResolved && workItem.StartUnresolved == default)
                {
                    workItem.StartUnresolved = DateTime.UtcNow;
                }
            }
            
            if (priority.HasValue && workItem.Priority != priority.Value)
            {
                var oldPriority = GetPriorityTitle(template, workItem.Priority);
                var newPriority = GetPriorityTitle(template, priority.Value);
                changes.Add($"Priority changed from '{oldPriority}' to '{newPriority}'");
                workItem.Priority = priority.Value;
            }
            
            // Handle assignment changes
            if (userAssignee.HasValue || triageAssignee.HasValue)
            {
                if (userAssignee.HasValue)
                {
                    workItem.UserAssignee = userAssignee.Value;
                    workItem.TriageAssignee = null;
                    var userName = await GetUserName(userAssignee.Value);
                    changes.Add($"Assigned to user: {userName}");
                }
                else if (triageAssignee.HasValue)
                {
                    workItem.TriageAssignee = triageAssignee.Value;
                    workItem.UserAssignee = null;
                    var triageName = GetTriageTitle(template, triageAssignee.Value);
                    changes.Add($"Assigned to triage: {triageName}");
                }
            }
            
            if (isListed.HasValue && workItem.IsListed != isListed.Value)
            {
                workItem.IsListed = isListed.Value;
                changes.Add($"Listed: {isListed.Value}");
            }
            
            if (isVisible.HasValue && workItem.IsVisible != isVisible.Value)
            {
                workItem.IsVisible = isVisible.Value;
                changes.Add($"Visible: {isVisible.Value}");
            }
            
            // Update metadata
            workItem.LastModifier = currentUser;
            workItem.UpdatedDate = DateTime.UtcNow;
            
            // Add history entry
            if (changes.Any())
            {
                workItem.EventHistory.Add(new WorkItemEventHistory
                {
                    EventId = Guid.NewGuid(),
                    Summary = WorkItemHistorySummary.MetadataEdited,
                    Actor = currentUser,
                    EventDate = DateTime.UtcNow,
                    Details = string.Join("; ", changes)
                });
            }
            
            await _workItemRepo.UpdateAsync(tc, workItem);
            
            // Generate update event
            await _eventSink.SinkAsync(tc, new AppEvent
            {
                Topic = "WorkItem.Updated",
                OriginEntityType = "WorkItem",
                OriginTemplate = workItem.Template,
                OriginId = workItem.Id,
                EntityPayload = workItem.ToBsonDocument()
            });
            
            await tc.CommitAsync();
        }
    }
    
    // Add a bookmark to work item
    public async Task ActionAddWorkItemBookmark(
        Guid currentUser,
        Guid workItemId,
        string title,
        Uri reference)
    {
        using (var tc = await _workItemRepo.OpenTransactionAsync())
        {
            var (items, _) = await _workItemRepo.GetAsync(tc, w => w.Id == workItemId);
            var workItem = items.FirstOrDefault();
            
            if (workItem == null)
                throw new NotFoundException($"WorkItem {workItemId} not found");
            
            // Add bookmark
            workItem.Bookmarks.Add(new WorkItemBookmark
            {
                Title = title,
                Reference = reference,
                Creator = currentUser,
                CreateDate = DateTime.UtcNow
            });
            
            // Add history
            workItem.EventHistory.Add(new WorkItemEventHistory
            {
                EventId = Guid.NewGuid(),
                Summary = WorkItemHistorySummary.BookmarkAdded,
                Actor = currentUser,
                EventDate = DateTime.UtcNow,
                Details = $"Added bookmark: {title}"
            });
            
            workItem.LastModifier = currentUser;
            workItem.UpdatedDate = DateTime.UtcNow;
            
            await _workItemRepo.UpdateAsync(tc, workItem);
            await tc.CommitAsync();
        }
    }
}
```

#### Section Management
Work items organize content through sections:

```csharp
public interface IEntityInstanceLogic
{
    string EntityType { get; }
    Task<IAppEntity> CreateForSectionAsync(
        ITransactionContext tc,
        SectionTemplate template,
        Guid workItemId,
        Guid? workSet,
        JObject? creationData);
}

// Example: Form section creator
public class FormSectionCreator : IEntityInstanceLogic
{
    public string EntityType => "Form";
    
    public async Task<IAppEntity> CreateForSectionAsync(
        ITransactionContext tc,
        SectionTemplate template,
        Guid workItemId,
        Guid? workSet,
        JObject? creationData)
    {
        // Extract form template from section configuration
        var formTemplate = template.Configuration?["formTemplate"]?.ToString() 
            ?? throw new ValidationException("Form template required");
            
        // Extract initial data if provided
        var initialData = creationData?["sections"]?[template.SectionKey] as JObject;
        
        // Create form for section
        var form = new FormInstance
        {
            Template = formTemplate,
            Content = (initialData ?? new JObject()).ToBsonDocument(),
            Home = FormInstanceHome.WorkItem,
            HostWorkItem = workItemId,
            HostWorkSet = workSet,
            Creator = _currentUser,
            LastModifier = _currentUser
        };
        
        await _formRepo.CreateAsync(tc, form);
        
        return form;
    }
}
```

### WorkItem Rule Actions

#### RuleActionCreateWorkItem
Creates work items in response to events:

```csharp
public class RuleActionCreateWorkItem : IRuleActionEvaluator
{
    public class Args
    {
        public string? TemplateName { get; set; }
        public string? TemplateNameQuery { get; set; }
        public string? WorkSetQuery { get; set; }
        public string? Title { get; set; }
        public string? TitleQuery { get; set; }
        public string? Description { get; set; }
        public string? DescriptionQuery { get; set; }
        public bool? IsListed { get; set; }
        public string? IsListedQuery { get; set; }
        public bool? IsVisible { get; set; }
        public string? IsVisibleQuery { get; set; }
        public string? UserAssigneeQuery { get; set; }
        public int? TriageAssignee { get; set; }
        public string? TriageAssigneeQuery { get; set; }
        public int? Status { get; set; }
        public string? StatusQuery { get; set; }
        public int? Priority { get; set; }
        public string? PriorityQuery { get; set; }
        public JObject? CreationData { get; set; }
        public string? CreationDataQuery { get; set; }
        public List<string>? InitialTags { get; set; }
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        // Resolve all parameters
        var templateName = a.TemplateName ?? 
            JsonPathQuery(eventData, a.TemplateNameQuery);
        var workSet = ResolveGuid(eventData, a.WorkSetQuery);
        var title = a.Title ?? 
            JsonPathQuery(eventData, a.TitleQuery)?.ToString();
        var description = a.Description ?? 
            JsonPathQuery(eventData, a.DescriptionQuery)?.ToString();
            
        // Create work item
        var workItemId = await _workItemLogic.EventCreateWorkItem(
            templateName: templateName,
            workSet: workSet,
            title: title,
            description: description,
            isListed: ResolveBoolean(a.IsListed, a.IsListedQuery, eventData),
            isVisible: ResolveBoolean(a.IsVisible, a.IsVisibleQuery, eventData),
            userAssignee: ResolveGuid(eventData, a.UserAssigneeQuery),
            triageAssignee: ResolveInt(a.TriageAssignee, a.TriageAssigneeQuery, eventData),
            status: ResolveInt(a.Status, a.StatusQuery, eventData),
            priority: ResolveInt(a.Priority, a.PriorityQuery, eventData),
            creationData: a.CreationData ?? ResolveJson(a.CreationDataQuery, eventData),
            tags: a.InitialTags?.ToArray()
        );
        
        // Add to event data
        eventData["createdWorkItemId"] = workItemId;
        
        return eventData;
    }
}
```

#### RuleActionAddWorkItemLink
Creates relationships between work items:

```csharp
public class RuleActionAddWorkItemLink : IRuleActionEvaluator
{
    public class Args
    {
        public string? WorkItemIdQuery { get; set; }
        public string? TargetWorkItemIdQuery { get; set; }
        public LinkDirection Direction { get; set; } = LinkDirection.References;
        public string? Title { get; set; }
        public string? TitleQuery { get; set; }
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        var workItemId = ResolveGuid(eventData, a.WorkItemIdQuery);
        var targetId = ResolveGuid(eventData, a.TargetWorkItemIdQuery);
        
        if (!workItemId.HasValue || !targetId.HasValue)
            return eventData;
            
        var title = a.Title ?? 
            JsonPathQuery(eventData, a.TitleQuery)?.ToString();
            
        await _workItemLogic.EventAddWorkItemLink(
            workItemId: workItemId.Value,
            targetWorkItemId: targetId.Value,
            direction: a.Direction,
            title: title
        );
        
        return eventData;
    }
}
```

### WorkItem Examples

#### Creating a Support Ticket Template
```csharp
var supportTicketTemplate = new WorkItemTemplate
{
    Name = "SupportTicket",
    Title = "Customer Support Ticket",
    Tags = new List<string> { "support", "customer-service" },
    
    // Workflow configuration
    StatusTemplates = new List<StatusTemplate>
    {
        new StatusTemplate 
        { 
            Status = 1, 
            Name = "New", 
            Title = "New", 
            CssClassName = "status-new",
            ConsideredResolved = false 
        },
        new StatusTemplate 
        { 
            Status = 2, 
            Name = "InProgress", 
            Title = "In Progress", 
            CssClassName = "status-progress",
            ConsideredResolved = false 
        },
        new StatusTemplate 
        { 
            Status = 3, 
            Name = "WaitingCustomer", 
            Title = "Waiting on Customer", 
            CssClassName = "status-waiting",
            ConsideredResolved = false 
        },
        new StatusTemplate 
        { 
            Status = 4, 
            Name = "Resolved", 
            Title = "Resolved", 
            CssClassName = "status-resolved",
            ConsideredResolved = true 
        },
        new StatusTemplate 
        { 
            Status = 5, 
            Name = "Closed", 
            Title = "Closed", 
            CssClassName = "status-closed",
            ConsideredResolved = true 
        }
    },
    
    TriageTemplates = new List<TriageTemplate>
    {
        new TriageTemplate 
        { 
            Triage = 1, 
            Name = "GeneralSupport", 
            Title = "General Support" 
        },
        new TriageTemplate 
        { 
            Triage = 2, 
            Name = "TechnicalSupport", 
            Title = "Technical Support" 
        },
        new TriageTemplate 
        { 
            Triage = 3, 
            Name = "BillingSupport", 
            Title = "Billing Support" 
        }
    },
    
    PriorityTemplates = new List<PriorityTemplate>
    {
        new PriorityTemplate { Priority = 1, Name = "Critical", Title = "Critical" },
        new PriorityTemplate { Priority = 2, Name = "High", Title = "High" },
        new PriorityTemplate { Priority = 3, Name = "Normal", Title = "Normal" },
        new PriorityTemplate { Priority = 4, Name = "Low", Title = "Low" }
    },
    
    // Section configuration
    SectionTemplates = new List<SectionTemplate>
    {
        new SectionTemplate
        {
            SectionKey = "CustomerInfo",
            EntityType = "Form",
            Name = "Customer Information",
            Configuration = JObject.FromObject(new
            {
                formTemplate = "CustomerContactForm"
            })
        },
        new SectionTemplate
        {
            SectionKey = "IssueDetails",
            EntityType = "Form",
            Name = "Issue Details",
            Configuration = JObject.FromObject(new
            {
                formTemplate = "SupportIssueForm"
            })
        },
        new SectionTemplate
        {
            SectionKey = "Resolution",
            EntityType = "Form",
            Name = "Resolution",
            Configuration = JObject.FromObject(new
            {
                formTemplate = "ResolutionForm"
            })
        }
    },
    
    // Permissions
    AllowComments = true,
    AllowFileAttachments = true,
    AllowEditTitle = true,
    AllowEditDescription = true,
    AllowSectionEdit = true,
    
    // Tracking
    TrackStatusHistory = true,
    TrackAssigneeHistory = true,
    TrackPriorityHistory = true,
    
    // Visibility
    IsVisibleToUsers = true,
    TriageQueuesAreVisibleToUsers = true,
    
    // Grooming
    IsGroomable = true,
    GroomPeriod = TimeFrame.Days90,
    GroomBehavior = WorkItemGroomBehavior.Archive
};
```

#### Implementing a Ticket Escalation Rule
```csharp
var escalationRule = new Rule
{
    Name = "EscalateCriticalTickets",
    TopicBindings = new List<string> { "WorkItem.Created", "WorkItem.Updated" },
    
    Conditions = new List<RuleCondition>
    {
        new RuleCondition
        {
            Query = "$.Template",
            Check = RuleConditionCheck.Single,
            Value = "SupportTicket"
        },
        new RuleCondition
        {
            Query = "$.Priority",
            Check = RuleConditionCheck.Single,
            Value = 1  // Critical
        },
        new RuleCondition
        {
            Query = "$.Status",
            Check = RuleConditionCheck.Single,
            Value = 1  // New
        }
    },
    
    Actions = new List<RuleAction>
    {
        // Assign to senior support
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionEditWorkItemMetadata",
                Args = JObject.FromObject(new
                {
                    WorkItemIdQuery = "$.Id",
                    TriageAssignee = 2  // Technical Support
                })
            }
        },
        // Notify support manager
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionRequestNotification",
                Args = JObject.FromObject(new
                {
                    GroupByTags = new[] { "support-manager" },
                    Subject = "Critical Support Ticket",
                    EmailTextQuery = "'Critical ticket created: ' + $.Title",
                    Severity = LogLevel.Warning
                })
            }
        },
        // Create linked escalation task
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionCreateWorkItem",
                Args = JObject.FromObject(new
                {
                    TemplateName = "EscalationTask",
                    TitleQuery = "'Escalation: ' + $.Title",
                    Description = "Critical ticket requiring immediate attention",
                    Status = 1,
                    Priority = 1,
                    CreationData = new
                    {
                        OriginalTicketIdQuery = "$.Id"
                    }
                }),
                ResultProperty = "escalationTaskId"
            }
        },
        // Link the tickets
        new RuleAction
        {
            Invocation = new RuleActionInvocation
            {
                Name = "RuleActionAddWorkItemLink",
                Args = JObject.FromObject(new
                {
                    WorkItemIdQuery = "$.Id",
                    TargetWorkItemIdQuery = "$.escalationTaskId",
                    Direction = LinkDirection.References,
                    Title = "Escalation Task"
                })
            }
        }
    ]
};
```

#### Creating a Work Item Dashboard View
```csharp
public class WorkItemDashboardService
{
    public async Task<WorkItemDashboardVM> GetDashboard(
        Guid userId,
        string? filter = null)
    {
        // Get user's assigned items
        var (assignedItems, _) = await _workItemRepo.GetAsync(
            w => w.UserAssignee == userId && w.IsVisible);
            
        // Get items in user's triage queues
        var userTriageQueues = await GetUserTriageQueues(userId);
        var (triageItems, _) = await _workItemRepo.GetAsync(
            w => w.TriageAssignee.HasValue && 
                 userTriageQueues.Contains(w.TriageAssignee.Value) && 
                 w.IsVisible);
        
        // Build view models
        var dashboard = new WorkItemDashboardVM
        {
            AssignedToMe = await BuildWorkItemList(assignedItems, userId),
            InMyQueues = await BuildWorkItemList(triageItems, userId),
            
            // Statistics
            Stats = new DashboardStats
            {
                TotalAssigned = assignedItems.Count,
                Overdue = assignedItems.Count(w => IsOverdue(w)),
                HighPriority = assignedItems.Count(w => w.Priority <= 2),
                UnresolvedTime = CalculateAverageUnresolvedTime(assignedItems)
            }
        };
        
        return dashboard;
    }
    
    private async Task<List<WorkItemListVM>> BuildWorkItemList(
        List<WorkItem> items,
        Guid userId)
    {
        var vms = new List<WorkItemListVM>();
        
        foreach (var item in items.OrderBy(w => w.Priority).ThenBy(w => w.CreatedDate))
        {
            var template = await LoadTemplate(item.Template);
            
            vms.Add(new WorkItemListVM
            {
                Id = item.Id,
                Title = item.Title,
                Status = GetStatusDisplay(template, item.Status),
                Priority = GetPriorityDisplay(template, item.Priority),
                Age = GetAge(item.CreatedDate),
                Assignee = await GetAssigneeDisplay(item),
                HasComments = await HasComments(item.Id),
                HasAttachments = await HasAttachments(item.Id),
                Tags = item.Tags
            });
        }
        
        return vms;
    }
}
```

### Best Practices

1. **Design templates carefully** - They define the entire work item structure
2. **Use sections** for organizing complex content
3. **Track history** for audit trails and compliance
4. **Set appropriate permissions** based on user roles
5. **Use triage queues** for team-based assignment
6. **Implement grooming** to manage data retention
7. **Link related items** for better tracking
8. **Use bookmarks** for external references
9. **Tag appropriately** for categorization and search
10. **Monitor unresolved time** for SLA compliance

---
