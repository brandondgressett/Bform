# BFormDomain Comprehensive Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Core Interfaces](#core-interfaces)
4. [Domain Models and Entities](#domain-models-and-entities)
5. [Services and Implementations](#services-and-implementations)
6. [Platform Capabilities](#platform-capabilities)
7. [Infrastructure Components](#infrastructure-components)
8. [Validation Framework](#validation-framework)
9. [Utility Components](#utility-components)
10. [Design Patterns](#design-patterns)
11. [Usage Examples](#usage-examples)

## Overview

BFormDomain is a comprehensive business application framework built on .NET 8 that provides a flexible platform for creating and managing forms, tables, KPIs, reports, and workflows with rule-based automation. It follows Domain-Driven Design principles with a strong emphasis on event-driven architecture and extensibility.

### Key Features
- **Dynamic Forms Engine** with JSON Schema validation
- **Work Item Management** with customizable workflows
- **Table Management** with dynamic schemas and querying
- **KPI Engine** with advanced calculations and signal detection
- **Report Generation** with templates and scheduling
- **Rule Engine** with event-driven automation
- **Notification System** supporting multiple channels
- **Scheduling System** with cron support
- **Multi-tenant Architecture** with work container isolation
- **MongoDB Persistence** with transaction support
- **Message Bus Integration** for distributed processing

## Architecture

### High-Level Architecture

The project follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    Platform Layer                             │
│  (Forms, WorkItems, Tables, KPIs, Reports, Rules, etc.)     │
├─────────────────────────────────────────────────────────────┤
│                    Service Layer                              │
│  (Business Logic, Event Processing, Scheduling)              │
├─────────────────────────────────────────────────────────────┤
│                Infrastructure Layer                           │
│  (Repository, MessageBus, Validation, Diagnostics)          │
├─────────────────────────────────────────────────────────────┤
│                    Utility Layer                              │
│  (Helpers, Extensions, Common Functions)                      │
└─────────────────────────────────────────────────────────────┘
```

### Project Structure

```
BFormDomain/
├── CommonCode/
│   ├── Platform/           # Core business domain
│   │   ├── Entity/         # Base entity system
│   │   ├── Forms/          # Forms subsystem
│   │   ├── WorkItems/      # Work item management
│   │   ├── WorkSets/       # Work container management
│   │   ├── Tables/         # Dynamic table system
│   │   ├── KPIs/           # Key performance indicators
│   │   ├── Reports/        # Report generation
│   │   ├── Rules/          # Business rules engine
│   │   ├── AppEvents/      # Event system
│   │   ├── Scheduler/      # Job scheduling
│   │   ├── Authorization/  # Security and auth
│   │   ├── Notification/   # Multi-channel notifications
│   │   ├── Comments/       # Commentary system
│   │   ├── ManagedFile/    # File management
│   │   ├── Tags/           # Tagging system
│   │   ├── Content/        # Content management
│   │   └── Terminology/    # Business terminology
│   ├── Repository/         # Data access layer
│   ├── MessageBus/         # Messaging infrastructure
│   ├── Diagnostics/        # Performance and alerts
│   ├── Validation/         # Validation framework
│   ├── Utility/            # Common utilities
│   └── SimilarEntityTracking/ # Deduplication
└── BFormDomain.csproj
```

## Core Interfaces

### Entity System Interfaces

#### IAppEntity
The core interface for all domain entities, combining data model capabilities with tagging functionality.

```csharp
public interface IAppEntity : IDataModel, ITaggable
{
    string EntityType { get; set; }
    string Template { get; set; }
    DateTime CreatedDate { get; set; }
    DateTime UpdatedDate { get; set; }
    Guid? Creator { get; set; }
    Guid? LastModifier { get; set; }
    Guid? HostWorkSet { get; set; }
    Guid? HostWorkItem { get; set; }
    List<string> AttachedSchedules { get; set; }
    
    bool Tagged(params string[] anyTags);
    JObject ToJson();
    Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null);
}
```

#### IEntityLoaderModule
Module interface for loading entities from URIs.

```csharp
public interface IEntityLoaderModule
{
    bool CanLoad(string uri);
    Task<JObject?> LoadJson(string uri, string? tzid = null);
}
```

#### IEntityReferenceBuilder
Dependency injection interface for building entity references.

```csharp
public interface IEntityReferenceBuilder
{
    Uri MakeReference(string templateName, Guid id, bool template = false, 
                      bool vm = false, string? queryParameters = null);
}
```

### Repository Interfaces

#### IRepository<T>
Generic repository pattern providing comprehensive data operations.

```csharp
public interface IRepository<T> where T : class, IDataModel
{
    // Transaction Management
    Task<ITransactionContext> OpenTransactionAsync(CancellationToken ct = default);
    ITransactionContext OpenTransaction(CancellationToken ct = default);
    
    // CRUD Operations (with and without transactions)
    Task<T> CreateAsync(T item, CancellationToken ct = default);
    Task<T> CreateAsync(ITransactionContext tc, T item, CancellationToken ct = default);
    
    Task<(List<T>, RepositoryContext)> GetAsync(Expression<Func<T, bool>> predicate, 
                                                CancellationToken ct = default);
    
    Task<(T, RepositoryContext)> UpdateAsync(T item, CancellationToken ct = default);
    Task DeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    
    // Query Operations
    Task<(List<T>, RepositoryContext)> GetOrderedAsync<TProperty>(
        Expression<Func<T, bool>> predicate, 
        Expression<Func<T, TProperty>> orderBy,
        bool ascending = true,
        CancellationToken ct = default);
    
    Task<(List<T>, RepositoryContext)> GetPageAsync(
        Expression<Func<T, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);
    
    // Special Operations
    Task<(T, RepositoryContext)> IncrementOneAsync<TProperty>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TProperty>> property,
        TProperty incrementValue,
        CancellationToken ct = default);
}
```

#### IDataModel
Base interface for all data models in the repository pattern.

```csharp
public interface IDataModel
{
    Guid Id { get; set; }
    int Version { get; set; }
}
```

### MessageBus Interfaces

#### IMessagePublisher
Publisher interface for sending messages to AMQP queues.

```csharp
public interface IMessagePublisher : IDisposable
{
    void Initialize(string exchangeName);
    void Send<T>(T msg, string routeKey);
    void Send<T>(T msg, Enum routeKey);
    Task SendAsync<T>(T msg, string routeKey);
    Task SendAsync<T>(T msg, Enum routeKey);
}
```

#### IMessageListener
Listener interface for receiving messages from AMQP queues.

```csharp
public interface IMessageListener : IDisposable
{
    bool Paused { get; set; }
    event EventHandler<IEnumerable<object>> ListenAborted;
    
    void Initialize(string exchangeName, string qName);
    void Listen(params KeyValuePair<Type, Action<object, CancellationToken, 
                                               IMessageAcknowledge>>[] listener);
}
```

### Validation Interfaces

#### IValidator<T>
Generic validation interface for type-safe validation rules.

```csharp
public interface IValidator<T>
{
    T? Value { get; }
    string? ArgumentName { get; }
    string? Message { get; }
    
    void Initialize(T? value, string? argumentName, string? message);
    IValidator<T> Otherwise<TException>(TException ex) where TException : Exception;
    Exception BuildValidationException(string? message, ExceptionType exceptionType);
}
```

## Domain Models and Entities

### Core Entity Models

#### FormInstance
Represents a form data instance with dynamic content storage.

```csharp
public class FormInstance : IAppEntity
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Template { get; set; }
    public BsonDocument Content { get; set; }  // MongoDB BSON for flexibility
    public JObject JsonContent { get; set; }   // JSON representation
    public FormInstanceHome Home { get; set; } // Where form resides
    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
    public List<string> AttachedSchedules { get; set; }
    public List<string> Tags { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }
}
```

#### WorkItem
Represents a task or ticket with workflow support.

```csharp
public class WorkItem : IAppEntity
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public int Status { get; set; }
    public int Priority { get; set; }
    public Guid? UserAssignee { get; set; }
    public int? TriageAssignee { get; set; }
    public bool IsListed { get; set; }
    public bool IsVisible { get; set; }
    public DateTime StartUnresolved { get; set; }
    public List<WorkItemEventHistory> EventHistory { get; set; }
    public List<WorkItemBookmark> Bookmarks { get; set; }
    public List<WorkItemLink> Links { get; set; }
    public List<Section> Sections { get; set; }
}
```

#### TableRowData
Dynamic table row storage with flexible schema.

```csharp
public class TableRowData : IDataModel, ITaggable
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public BsonDocument PropertyBag { get; set; }  // Dynamic properties
    
    // Indexed keys for efficient querying
    public string? KeyRowId { get; set; }
    public DateTime? KeyDate { get; set; }
    public Guid? KeyUser { get; set; }
    public Guid? KeyWorkSet { get; set; }
    public Guid? KeyWorkItem { get; set; }
    public double? KeyNumeric { get; set; }
    
    public List<string> Tags { get; set; }
    public DateTime Created { get; set; }
}
```

#### KPIInstance
Represents calculated KPI values.

```csharp
public class KPIInstance : IAppEntity
{
    public Guid? SubjectUser { get; set; }
    public Guid? SubjectWorkSet { get; set; }
    public Guid? SubjectWorkItem { get; set; }
    public string EventTopic { get; set; }
    // Additional IAppEntity properties...
}
```

#### AppEvent
Core event model for the event-driven architecture.

```csharp
public class AppEvent : IDataModel
{
    public string? Topic { get; set; }
    public string? OriginEntityType { get; set; }
    public string? OriginTemplate { get; set; }
    public Guid? OriginId { get; set; }
    public Guid EventLine { get; set; }          // Groups related events
    public int EventGeneration { get; set; }     // Distance from original
    public bool IsNatural { get; set; }          // User vs system generated
    public AppEventState State { get; set; }     // Processing state
    public BsonDocument EntityPayload { get; set; }
    public DateTime DeferredUntil { get; set; }
    public DateTime TakenExpiration { get; set; }
    public List<string> Tags { get; set; }
    public List<string> EntityTags { get; set; }
}
```

### Template Models

Each entity type has a corresponding template that defines its structure and behavior:

- **FormTemplate**: Defines form schema, UI, validation, and actions
- **WorkItemTemplate**: Defines workflow states, sections, and permissions
- **TableTemplate**: Defines columns, data types, and retention policies
- **KPITemplate**: Defines metrics, calculations, and thresholds
- **ReportTemplate**: Defines data sources, layouts, and parameters

## Services and Implementations

### Core Platform Services

#### NotificationService
Processes notification messages with channel routing and regulation.

```csharp
public class NotificationService : IHostedService
{
    // Dependencies: IMessageListener, IRegulatedNotificationLogic, ILogger
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Initialize message listener
        // Start listening for NotificationMessage instances
    }
    
    public Task ProcessMessage(NotificationMessage message)
    {
        // Route to appropriate notification channel
        // Apply regulation (rate limiting, suppression)
    }
}
```

#### RuleEngine
Evaluates business rules by consuming events and executing actions.

```csharp
public class RuleEngine : IAppEventConsumer
{
    // Dependencies: IApplicationPlatformContent, TopicRegistrations, RuleEvaluator
    
    public void ConsumeEvents(AppEvent appEvent)
    {
        // Match event to rule topic bindings
        // Evaluate rule conditions
        // Execute rule actions in priority order
    }
}
```

#### AppEventPump
Background service that distributes events from MongoDB to message bus.

```csharp
public class AppEventPump : BackgroundService
{
    // Dependencies: IRepository<AppEvent>, IMessagePublisher, ApplicationTopologyCatalog
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check server role eligibility
        // Pump events from repository to message bus
        // Handle retry logic and failure grooming
    }
}
```

### Entity Services

#### FormLogic
Manages CRUD operations for form instances.

```csharp
public class FormLogic
{
    public async Task<FormInstance> ActionCreateForm(
        string templateName, 
        JObject? content,
        Guid? workSet, 
        Guid? workItem)
    {
        // Validate against JSON schema
        // Create form instance
        // Apply tags and schedules
        // Generate creation event
    }
    
    public async Task ActionUpdateFormContent(
        Guid formId, 
        JObject updates,
        bool merge = true)
    {
        // Load form
        // Validate updates
        // Apply changes
        // Generate update event
    }
}
```

#### WorkItemLogic
Manages work item lifecycle and operations.

```csharp
public class WorkItemLogic
{
    public async Task<WorkItem> ActionCreateWorkItem(
        string templateName,
        string title,
        string? description,
        WorkItemCreationData? creationData)
    {
        // Create work item
        // Generate sections from template
        // Set initial status/priority
        // Create event history entry
    }
}
```

#### TableLogic
Provides dynamic table operations with query support.

```csharp
public class TableLogic
{
    public async Task EventMapInsertTableRow(
        string tableTemplate,
        List<Mapping> mappings,
        JObject sourceData)
    {
        // Map source data using field mappings
        // Apply transformations
        // Insert into dynamic collection
    }
    
    public async Task<List<JObject>> QueryDataTableAll(
        string tableTemplate,
        JObject? filter,
        string? orderBy,
        bool ascending = true)
    {
        // Build MongoDB query
        // Execute with projections
        // Return results
    }
}
```

## Platform Capabilities

### Rule Engine System

The rule engine provides event-driven automation through a comprehensive set of rule actions:

#### Rule Actions by Category

**Forms**
- `RuleActionCreateForm` - Creates form instances
- `RuleActionDeleteForm` - Deletes forms
- `RuleActionEditFormProperty` - Updates form fields
- `RuleActionTagForm/UntagForm` - Manages form tags
- `RuleActionFormEnrollDashboard` - Makes forms visible in dashboards

**Work Items**
- `RuleActionCreateWorkItem` - Creates work items with full metadata
- `RuleActionEditWorkItemMetadata` - Updates work item properties
- `RuleActionAddWorkItemBookmark/Link` - Manages relationships
- `RuleActionWorkItemEnrollDashboard` - Dashboard visibility

**Tables**
- `RuleActionInsertTableData` - Inserts rows with field mapping
- `RuleActionEditTableRow` - Updates existing rows
- `RuleActionDeleteTableRow` - Removes rows
- `RuleActionSummarizeTable` - Creates aggregated views

**KPIs**
- `RuleActionCreateKPI` - Defines new KPIs
- `RuleActionEvaluateKPIs` - Triggers KPI calculations
- `RuleActionKPIEnrollDashboard` - Dashboard integration

**Reports**
- `RuleActionCreateReport` - Generates reports from templates
- `RuleActionReportEnrollDashboard` - Makes reports accessible

**Notifications**
- `RuleActionRequestNotification` - Sends multi-channel notifications

**Core Actions**
- `RuleActionForEach` - Iterates over collections
- `RuleActionCustomEvent` - Triggers custom events

### Event Appenders

Transform and augment event data during rule processing:

- `CurrentDateTimeAppender` - Adds current timestamp
- `ComputedDateTimeAppender` - Calculates dates based on business rules
- `AddEntityReferenceAppender` - Creates entity URIs
- `JsonWinnowerAppender` - Extracts specific JSON fields
- `FindReplaceAppender` - Text transformations

### Scheduling System

Supports multiple scheduling patterns:
- One-time execution
- Recurring with count limit
- Infinite recurrence
- Cron expressions
- Business date calculations

## Infrastructure Components

### Repository Layer

#### MongoRepository<T>
Base repository implementation with comprehensive features:

```csharp
public abstract class MongoRepository<T> : IRepository<T> where T : class, IDataModel
{
    // Version-based optimistic concurrency
    // Transaction support
    // Batch operations
    // Query with LINQ expressions
    // Pagination support
    // Atomic increment operations
}
```

#### Transaction Support
```csharp
using (var tc = await repository.OpenTransactionAsync())
{
    await repository.CreateAsync(tc, entity1);
    await repository.UpdateAsync(tc, entity2);
    await tc.CommitAsync();  // All or nothing
}
```

### Message Bus

#### MemMessageBus
In-memory implementation for development and testing:
- Exchange management
- Topic-based routing
- Queue bindings
- Thread-safe operations

### Diagnostics

#### Performance Tracking
```csharp
using (PerfTrack.Stopwatch("OperationName"))
{
    // Code to measure
}
```

#### Application Alerts
```csharp
_alert.RaiseAlert(ApplicationAlertKind.SystemError, 
                  "Operation failed", 
                  exception);
```

## Validation Framework

### Fluent Validation API

The framework provides two main validation entry points:

#### RequiresValidator
For parameter validation:
```csharp
public void ProcessData(string input, int count)
{
    input.Requires(nameof(input)).IsNotNullOrEmpty();
    count.Requires(nameof(count)).IsGreaterThan(0);
}
```

#### GuaranteesValidator
For state/invariant validation:
```csharp
var result = await CalculateAsync();
result.Guarantees("Calculation must produce result").IsNotNull();
result.Value.Guarantees().IsInRange(0, 100);
```

### Validation Methods

The framework supports 48 validation types including:
- Null checks: `IsNull()`, `IsNotNull()`, `IsNullOrEmpty()`
- Type checks: `IsOfType<T>()`, `IsNotOfType<T>()`
- String operations: `StartsWith()`, `Contains()`, `Matches(regex)`
- Numeric comparisons: `IsGreaterThan()`, `IsLessThan()`, `IsInRange()`
- Collection operations: `ContainsAny()`, `ContainsAll()`, `HasCount()`
- Boolean checks: `IsTrue()`, `IsFalse()`

### Custom Exceptions

```csharp
value.Requires()
    .IsPositive()
    .Otherwise(new BusinessException("Value must be positive"));
```

## Utility Components

### AsyncHelper
Enables synchronous execution of async methods:
```csharp
var result = AsyncHelper.RunSync(() => GetDataAsync());
```

### JsonWinnower
Advanced JSON querying with aggregation:
```csharp
var winnower = new JsonWinnower()
    .Plan(new JsonPathWinnow("$.items[*].price", 
                            summarize: JArraySummarize.Sum, 
                            asSub: "total"))
    .Plan(new JsonPathWinnow("$.customer", 
                            appendTo: "result"));

var result = winnower.Winnow(jsonData);
```

### TemporalCollocator
Business date calculations:
```csharp
var tc = new TemporalCollocator();
tc.CollocateUtcNow();

var nextBusinessDay = tc.NextWeekday;
var quarterEnd = tc.LastOfThisQuarter;
var reportPeriod = tc.HandleTimeQuery("last 30 days");
```

### Caching
Thread-safe in-memory caching with expiration:
```csharp
var cache = new InMemoryCachedData<string, UserData>(
    expireAfterWrite: TimeSpan.FromMinutes(15),
    maximumCount: 1000);

if (!cache.MaybeGetItem(userId, out var userData))
{
    userData = await LoadUserDataAsync(userId);
    cache.Add(userId, userData);
}
```

### Retry Logic
```csharp
await Retry.Times(3, TimeSpan.FromSeconds(1))
    .When<TransientException>()
    .ExecuteAsync(() => CallExternalServiceAsync());
```

## Design Patterns

### Key Architectural Patterns

1. **Domain-Driven Design**
   - Rich domain models with business logic
   - Aggregate roots (WorkItem, FormInstance)
   - Value objects (WorkItemBookmark, Section)

2. **Event-Driven Architecture**
   - AppEvent as the central communication mechanism
   - Loose coupling through event topics
   - Eventual consistency

3. **Repository Pattern**
   - Generic repository interface
   - MongoDB-specific implementation
   - Transaction support

4. **Template Method Pattern**
   - Separate templates from instances
   - Configurable behavior through templates

5. **Command/Event Separation**
   - Action methods for user-initiated operations
   - Event methods for system-triggered operations

6. **Plugin Architecture**
   - Rule actions as plugins
   - Entity loader modules
   - Event appenders

7. **Factory Pattern**
   - Entity creation through logic classes
   - Dynamic repository creation

8. **Observer Pattern**
   - Event distribution to consumers
   - Topic-based subscriptions

9. **Strategy Pattern**
   - KPI computation strategies
   - Notification channel strategies

10. **Decorator Pattern**
    - Event appenders augment event data
    - Validation decorators chain validations

## Usage Examples

### Creating a Form

```csharp
// Inject dependencies
var formLogic = serviceProvider.GetRequiredService<FormLogic>();

// Create form instance
var form = await formLogic.ActionCreateForm(
    templateName: "CustomerFeedback",
    content: JObject.FromObject(new 
    {
        rating = 5,
        comment = "Excellent service!",
        email = "customer@example.com"
    }),
    workSet: workSetId,
    tags: new[] { "feedback", "positive" }
);
```

### Defining a Rule

```json
{
  "name": "ProcessHighPriorityTickets",
  "topicBindings": ["WorkItem.Created", "WorkItem.Updated"],
  "conditions": [{
    "query": "$.priority",
    "check": "Single",
    "value": 1
  }],
  "actions": [{
    "invocation": {
      "name": "RuleActionRequestNotification",
      "args": {
        "GroupByTags": ["support-team"],
        "Subject": "High Priority Ticket",
        "EmailText": "A high priority ticket requires attention"
      }
    }
  }]
}
```

### Querying Table Data

```csharp
var tableLogic = serviceProvider.GetRequiredService<TableLogic>();

var results = await tableLogic.QueryDataTablePage(
    tableTemplate: "CustomerOrders",
    filter: JObject.FromObject(new { status = "pending" }),
    orderBy: "created",
    ascending: false,
    pageNumber: 1,
    pageSize: 50
);
```

### Creating a KPI

```csharp
var kpiTemplate = new KPITemplate
{
    Name = "AverageResponseTime",
    Title = "Average Ticket Response Time",
    ScheduleTemplate = "0 */4 * * *", // Every 4 hours
    Sources = new List<KPISource>
    {
        new KPISource
        {
            Name = "tickets",
            TableTemplate = "WorkItems",
            Query = new RelativeTableQueryCommand
            {
                TimeFrame = TimeFrame.Last7Days,
                TimeField = "CreatedDate"
            }
        }
    },
    ComputeStages = new List<KPIComputeStage>
    {
        new KPIComputeStage
        {
            Script = "Mean(tickets.ResponseTime)",
            ResultProperty = "avgResponseTime"
        }
    }
};
```

### Transaction Example

```csharp
using (var tc = await workItemRepo.OpenTransactionAsync())
{
    try
    {
        // Create work item
        var workItem = await workItemRepo.CreateAsync(tc, newWorkItem);
        
        // Create related form
        var form = await formRepo.CreateAsync(tc, relatedForm);
        
        // Update references
        workItem.Sections.Add(new Section 
        { 
            EntityType = "Form", 
            EntityId = form.Id 
        });
        await workItemRepo.UpdateAsync(tc, workItem);
        
        // Commit all changes
        await tc.CommitAsync();
    }
    catch
    {
        await tc.AbortAsync();
        throw;
    }
}
```

## Best Practices

1. **Always use transactions** for multi-entity operations
2. **Validate early** using the validation framework
3. **Use tags** for categorization and filtering
4. **Generate events** for audit trails and automation
5. **Cache frequently accessed data** with appropriate expiration
6. **Monitor performance** using PerfTrack
7. **Handle errors gracefully** with retry logic
8. **Use templates** for consistent entity creation
9. **Leverage the rule engine** for business automation
10. **Follow DDD principles** for domain modeling

## Security Considerations

- JWT authentication with MongoDB-backed identity
- Role-based authorization with custom attributes
- Entity-level permissions through work containers
- Audit trails via event history
- Input validation at all entry points
- Secure file storage with access controls

## Performance Considerations

- Indexed MongoDB queries for efficiency
- Pagination for large result sets
- Caching for frequently accessed data
- Async operations throughout
- Batch operations where applicable
- Connection pooling for MongoDB
- Message bus for distributed processing
- Background services for long-running operations

## Conclusion

BFormDomain provides a comprehensive, extensible platform for building business applications with sophisticated form handling, workflow management, reporting, and automation capabilities. Its event-driven architecture, combined with a flexible rule engine and robust infrastructure components, enables the creation of complex business solutions while maintaining clean separation of concerns and excellent maintainability.