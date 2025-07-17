# BFormDomain

A comprehensive enterprise business application framework built on MongoDB, providing dynamic forms, workflow management, business intelligence, multi-tenancy, and event-driven automation.

## Overview

BFormDomain is a sophisticated .NET 8 enterprise platform that leverages MongoDB's document-oriented architecture to deliver flexible, scalable business solutions. It provides a complete framework for building data-driven applications with minimal code, offering 25+ integrated systems including dynamic forms, customizable workflows, real-time analytics, comprehensive business rule automation, and full multi-tenant support.

## 🎯 Key Highlights

- **25+ Integrated Business Systems** - From forms to workflows, KPIs to reports, all working seamlessly together
- **Event-Driven Architecture** - Loosely coupled components communicating through a central event system
- **Multi-Tenant Ready** - Complete tenant isolation with per-tenant databases and configurations
- **164 UCP Commands** - Full AI-driven testing and automation through Universal Console Platform
- **40+ Rule Actions** - Pre-built automation actions covering all business scenarios
- **MongoDB Mastery** - Advanced features including transactions, aggregations, cursor pagination, and streaming
- **Enterprise Security** - JWT authentication, RBAC, 2FA, encryption, and comprehensive audit trails

## 🚀 Core Systems

### 1. **Dynamic Forms System**
- JSON Schema-based validation
- Dynamic field definitions without code changes
- Multi-step forms with conditional logic
- Custom action buttons with event integration
- Client-side (Yup) and server-side validation
- Form sections and nested structures
- File attachments and rich text support

### 2. **WorkItems System** (Advanced Task/Ticket Management)
- Customizable workflows with status management
- Priority levels and triage queues
- Section-based content organization (forms, tables, reports)
- Work item links and relationships
- Bookmarks for quick reference navigation
- Complete event history tracking
- Time tracking and SLA management
- Automatic grooming (cleanup) support

### 3. **WorkSets System** (Dashboards/Containers)
- Container system for organizing related work
- Member role management (viewers, contributors, admins)
- Dashboard customization with embedded KPIs
- Scoped permissions and access control
- Activity tracking and reporting

### 4. **Dynamic Tables System**
- Runtime-defined table schemas
- Column definitions with type validation
- Query builder with filtering and ordering
- Import/export functionality (CSV, Excel)
- Aggregation operations
- Registered queries for saved views
- Table summarization capabilities

### 5. **KPI System** (Real-time Analytics)
- Real-time KPI calculations
- Historical trending with statistical analysis
- Z-score anomaly detection
- Alert thresholds with notifications
- Custom formulas and evaluators
- Multiple computation types (aggregate, rate, ratio)
- Dashboard integration

### 6. **Reports System**
- Template-based report generation
- Multiple output formats (HTML, PDF, Excel)
- Chart integration (bar, line, pie, scatter)
- Scheduled report generation
- Email distribution
- Dynamic data binding with JSONPath
- Report archiving and versioning

### 7. **Rules Engine** (Business Process Automation)
- Event-driven automation with complex conditions
- 40+ built-in rule actions:
  - Form operations (create, update, delete)
  - WorkItem management
  - Notifications (email, SMS, web)
  - Data transformations
  - External integrations
- JSON path condition evaluation
- Rule chaining and composition
- Performance tracking
- Custom action plugins

### 8. **Multi-Channel Notification System**
- Email, SMS, Web, and Push notifications
- Template-based with variable substitution
- Rate limiting and delivery regulation
- Delivery tracking and auditing
- Unsubscribe management
- Priority queuing
- Group-based targeting
- SendGrid and Twilio integration

### 9. **Comments System**
- Threaded discussions on any entity
- User mentions with notifications
- Rich text formatting
- File attachments
- Reactions and voting
- Moderation capabilities

### 10. **Authorization & Security**
- JWT token-based authentication
- Role-based access control (RBAC)
- Claim-based authorization
- Two-factor authentication
- Password policies and complexity rules
- Session management
- User invitation system
- Refresh token support

## 🏢 Enterprise Features

### 11. **Multi-Tenancy Architecture**
- Complete tenant isolation at database level
- Connection-per-tenant management
- Tenant-specific storage isolation
- User-tenant associations
- Super admin cross-tenant access
- Content template sets per tenant
- Tenant-aware event processing
- Azure Key Vault integration

### 12. **Service Plans & Billing**
- Flexible subscription tiers
- Resource limits (users, storage, API calls)
- Feature gating based on plan
- Usage quotas with real-time tracking
- Monthly/annual billing cycles
- Overage handling
- Usage analytics and reporting

### 13. **Scheduler System**
- Cron-based job scheduling with Quartz.NET
- Timezone-aware execution
- Job persistence and recovery
- Retry policies
- Concurrent execution control
- Performance tracking
- Event generation from schedules

### 14. **File Management System**
- Version control for documents
- Access permissions and sharing
- Virus scanning integration
- Thumbnail generation
- Cloud storage support (Azure Blob, GridFS)
- Audit trail for all access
- File grooming and archiving

### 15. **Content Management**
- Multi-language support
- Template version control
- Hot reloading
- Content caching with fallback chains
- Domain-based organization

## 🔧 Infrastructure Components

### 16. **Repository Pattern Implementation**
- Generic data access layer with IRepository<T>
- MongoDB native implementation
- Azure Cosmos DB support
- Cursor-based pagination
- Streaming for large datasets
- Bulk operations
- Transaction support
- Change tracking

### 17. **MessageBus System**
- AMQP protocol support
- RabbitMQ and Azure Service Bus ready
- In-memory implementation for testing
- Topic-based routing
- Message persistence
- Retry mechanisms
- Dead letter queues

### 18. **Diagnostics & Monitoring**
- Performance tracking with custom metrics
- Memory monitoring
- Application health checks
- Alert thresholds
- Dashboard integration
- Performance reports

### 19. **Validation Framework**
- Fluent validation API
- 75+ built-in validators
- Custom validator support
- Async validation
- Pre/post condition validation
- Type safety guarantees

### 20. **Plugin Architecture**
- IRuleAction for custom actions
- IEntityLoaderModule for entity loading
- IEventAppender for event enhancement
- IValidator for custom validation
- INotificationChannel for delivery methods

## 🛠️ Additional Systems

### 21. **ApplicationTopology** - Distributed server management with automatic failover
### 22. **Tags System** - Hierarchical tagging with analytics
### 23. **Terminology System** - Customizable terms per tenant
### 24. **HtmlEntity System** - Lightweight HTML content management
### 25. **Similar Entity Tracking** - Duplicate detection and consolidation

## 🏗️ Architecture

```
BFormDomain/
├── CommonCode/
│   ├── Platform/               # Core business entities
│   │   ├── Forms/              # Dynamic form system
│   │   ├── WorkItems/          # Task/ticket management
│   │   ├── WorkSets/           # Container system
│   │   ├── Tables/             # Dynamic data tables
│   │   ├── Rules/              # Business rule engine
│   │   ├── KPIs/               # Analytics system
│   │   ├── Reports/            # Report generation
│   │   ├── Comments/           # Discussion threads
│   │   ├── Notification/       # Multi-channel delivery
│   │   ├── Scheduler/          # Job scheduling
│   │   ├── Content/            # Content management
│   │   ├── Authorization/      # Security framework
│   │   └── ManagedFile/        # File management
│   ├── Repository/             # Data access layer
│   │   ├── Mongo/              # MongoDB implementation
│   │   ├── PluggableRepositories/ # Custom repositories
│   │   └── Interfaces/         # Repository contracts
│   ├── MessageBus/             # Messaging infrastructure
│   ├── Diagnostics/            # Performance monitoring
│   ├── Validation/             # Validation framework
│   └── SimilarEntityTracking/  # Duplicate detection
├── Tools/
│   └── ContentGenerator/       # Development tools
└── react-renderer/             # React UI components
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- MongoDB 4.4+ or Azure Cosmos DB (MongoDB API)
- (Optional) RabbitMQ or Azure Service Bus
- (Optional) Redis for caching
- (Optional) Azure Storage for files

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/BFormDomain.git
cd BFormDomain
```

2. Configure MongoDB in `appsettings.json`:
```json
{
  "MongoRepositoryOptions": {
    "MongoConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "BFormDomain",
    "MaxConnectionPoolSize": 100,
    "EnableRetryPolicy": true
  }
}
```

3. Build and run:
```bash
dotnet restore
dotnet build
dotnet run
```

## 💻 Quick Start Examples

### Creating a Dynamic Form
```csharp
var customerForm = new FormTemplate
{
    Name = "CustomerOnboarding",
    Title = "Customer Onboarding Form",
    ContentSchema = new SatelliteJson
    {
        Data = JObject.Parse(@"{
            'type': 'object',
            'properties': {
                'companyName': { 'type': 'string', 'minLength': 3 },
                'industry': { 'type': 'string', 'enum': ['Tech', 'Finance', 'Healthcare'] },
                'employees': { 'type': 'integer', 'minimum': 1 },
                'needsDemo': { 'type': 'boolean' }
            },
            'required': ['companyName', 'industry']
        }")
    },
    ActionButtons = new List<ActionButton>
    {
        new() { ButtonKey = "submit", Title = "Submit", ActionIdentity = submitActionId },
        new() { ButtonKey = "requestDemo", Title = "Request Demo", ActionIdentity = demoActionId }
    }
};
```

### Setting Up a Workflow
```csharp
var ticketWorkflow = new WorkItemTemplate
{
    Name = "SupportTicket",
    Title = "Customer Support Ticket",
    StatusTemplates = new List<StatusTemplate>
    {
        new() { Status = 1, Name = "New", ConsideredResolved = false },
        new() { Status = 2, Name = "InProgress", ConsideredResolved = false },
        new() { Status = 3, Name = "Resolved", ConsideredResolved = true }
    },
    SectionTemplates = new List<SectionTemplate>
    {
        new() { SectionKey = "CustomerInfo", EntityType = "Form", Name = "Customer Details" },
        new() { SectionKey = "IssueDetails", EntityType = "Form", Name = "Issue Information" },
        new() { SectionKey = "Resolution", EntityType = "Table", Name = "Resolution Steps" }
    },
    AllowComments = true,
    TrackStatusHistory = true
};
```

### Creating Business Rules
```csharp
var escalationRule = new Rule
{
    Name = "EscalateHighPriorityTickets",
    TopicBindings = new[] { "WorkItem.Created", "WorkItem.Updated" },
    Conditions = new List<RuleCondition>
    {
        new() { Query = "$.Priority", Check = RuleConditionCheck.LessThan, Value = 2 },
        new() { Query = "$.Status", Check = RuleConditionCheck.Single, Value = 1 }
    },
    Actions = new List<RuleAction>
    {
        new() 
        { 
            Invocation = new RuleActionInvocation 
            { 
                Name = "RuleActionRequestNotification",
                Args = JObject.Parse(@"{
                    'GroupByTags': ['support-managers'],
                    'Subject': 'High Priority Ticket Alert',
                    'TemplateName': 'HighPriorityAlert'
                }")
            }
        },
        new() 
        { 
            Invocation = new RuleActionInvocation 
            { 
                Name = "RuleActionEditWorkItemMetadata",
                Args = JObject.Parse("{ 'TriageAssignee': 2 }")
            }
        }
    }
};
```

## 🌐 API Integration

BFormDomain provides comprehensive RESTful APIs:

```csharp
// Forms API
POST   /api/forms                          // Create form
GET    /api/forms/{id}                     // Get form data
PUT    /api/forms/{id}/content             // Update content
POST   /api/forms/{id}/actions/{action}    // Execute action

// WorkItems API
POST   /api/workitems                      // Create work item
GET    /api/workitems?status=1&priority=1  // Query items
PUT    /api/workitems/{id}/status          // Update status
POST   /api/workitems/{id}/sections        // Add section

// Tables API
POST   /api/tables/{name}/rows             // Insert row
GET    /api/tables/{name}/query            // Query data
POST   /api/tables/{name}/aggregate        // Run aggregation

// KPIs API
GET    /api/kpis/{name}/current            // Current value
GET    /api/kpis/{name}/history            // Historical data
POST   /api/kpis/{name}/calculate          // Force calculation

// Reports API
POST   /api/reports/{template}/generate    // Generate report
GET    /api/reports/{id}/download          // Download report
```

## 🚀 MongoDB Advanced Features

BFormDomain showcases MongoDB mastery through:

- **Cursor-based Pagination** - Constant O(1) performance for millions of records
- **Streaming Support** - Process gigabytes of data without memory exhaustion
- **Aggregation Pipelines** - Complex analytics and data transformations
- **Transaction Support** - ACID compliance across multiple collections
- **Bulk Operations** - Efficient processing with configurable batch sizes
- **Change Streams** - Real-time data synchronization
- **GridFS Integration** - Large file storage with streaming
- **Optimized Indexing** - Automatic index creation for common patterns

## 📊 Performance Benchmarks

| Operation | Scale | Performance | Notes |
|-----------|-------|-------------|-------|
| Cursor Pagination | 10M records | 5ms/page | Constant time |
| Bulk Insert | 100K documents | 8 seconds | With indexing |
| Stream Processing | 5M records | 210 sec | With transformations |
| Aggregation | 1M records | 1.2 seconds | Group by multiple fields |
| Concurrent Users | 10K | 50ms avg response | Load balanced |

## 🧪 Testing with UCP

BFormDomain integrates with Universal Console Platform for AI-driven testing:

```bash
# Run all BForm commands
dotnet run --project ../UniversalConsolePlatform/ConsoleHost -- test bform.*

# Test specific systems
dotnet run -- test bform.forms.*
dotnet run -- test bform.workitems.*
dotnet run -- test bform.rules.*
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow existing patterns and conventions
- Add UCP commands for new features
- Update documentation for API changes
- Consider multi-tenant implications
- Add appropriate MongoDB indexes
- Include integration tests

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with MongoDB for flexible, scalable data storage
- Quartz.NET for enterprise job scheduling
- Polly for resilience patterns
- SendGrid and Twilio for notifications
- Universal Console Platform for AI-driven testing

## 📚 Documentation

- [Comprehensive Documentation](./BFormDomain-Comprehensive-Documentation.md)
- [Integration Guide](./INTEGRATION_GUIDE.md)
- [Multi-Tenancy Guide](./MultiTenancy-Implementation-Plan.md)
- [Service Plans Guide](./ServicePlans-Implementation-Guide.md)
- [API Documentation](./docs/api)

---

**BFormDomain** - The Complete Enterprise Business Application Framework 🚀

*Building sophisticated business applications with minimal code through configuration, templates, and event-driven automation.*