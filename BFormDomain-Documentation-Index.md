# BFormDomain Documentation Index

## Complete Documentation Guide

This index provides navigation to all BFormDomain documentation files. The documentation is organized into 8 parts covering all aspects of the framework.

### Quick Links

- 📖 [Complete Reference](BFormDomain-Documentation-Complete.md) - Single-file comprehensive reference
- 🚀 [Quick Start Guide](#quick-start)
- 🏗️ [Architecture Overview](#architecture-overview)
- 💡 [Examples](#examples)
- 🔍 [API Reference](#api-reference)

---

## Documentation Parts

### [Part 1: Core Entity and Form Systems](BFormDomain-Documentation-Part1.md)
- **Entity System** - Base interfaces, entity loading, references
- **Forms System** - Dynamic forms, templates, validation, rule actions
- **WorkItems System** - Task management, workflows, sections, history
- **WorkSets System** - Work containers, members, dashboards

### [Part 2: Data and Analytics Systems](BFormDomain-Documentation-Part2.md)
- **Tables System** - Dynamic tables, queries, metadata
- **ManagedFile System** - File management, versioning, attachments
- **KPI System** - Performance indicators, evaluators, signals
- **Reports System** - Report generation, templates, charts

### [Part 3: Communication and Automation](BFormDomain-Documentation-Part3.md)
- **Comments System** - Threading, mentions, moderation
- **Content System** - Templates, localization, domains
- **Rules Engine** - Event-driven automation, 40+ actions
- **AppEvents System** - Event bus, routing, persistence

### [Part 4: Security and Platform Services](BFormDomain-Documentation-Part4.md)
- **Authorization System** - JWT auth, users, roles, invitations
- **Scheduler System** - Cron jobs, background tasks
- **Notification System** - Multi-channel delivery, regulation
- **Tags System** - Entity tagging, analytics
- **Terminology System** - Customizable terms
- **ApplicationTopology** - Server management, load balancing
- **HtmlEntity System** - HTML content management

### [Part 5: Infrastructure and Integration](BFormDomain-Documentation-Part5.md)
- **Repository Infrastructure** - Data access, transactions
- **MessageBus Infrastructure** - Pub/sub messaging, AMQP
- **SimilarEntityTracking** - Duplicate detection, consolidation
- **Diagnostics System** - Performance, metrics, alerts

### [Part 6: Validation and Extensions](BFormDomain-Documentation-Part6.md)
- **Validation Framework** - Fluent API, 75+ validators
- **Utility Components** - Helpers, retry, expressions
- **Plugin Architecture** - Extension points, lifecycle
- **Dynamic vs Application-Specific Patterns** - Design guidance

### [Part 7: Advanced Patterns and Examples](BFormDomain-Documentation-Part7.md)
- **JSON Schemas and Validation** - Schema patterns, evolution
- **Comprehensive Examples** - Complete purchase order system
- **Best Practices** - Architecture and design guidance

### [Part 8: Complete Reference](BFormDomain-Documentation-Complete.md)
- **Full Documentation** - All parts in one file
- **Quick Reference** - Common interfaces and patterns
- **Getting Started** - Setup and configuration

---

## Quick Start

### 1. Installation

```bash
# Add BFormDomain to your project
dotnet add package BFormDomain

# Add MongoDB driver
dotnet add package MongoDB.Driver
```

### 2. Basic Configuration

```csharp
// Startup.cs or Program.cs
services.AddBFormDomain(options =>
{
    options.ConnectionString = Configuration.GetConnectionString("MongoDB");
    options.DatabaseName = "myapp";
});

// Add authentication
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Jwt:Issuer"],
            ValidAudience = Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
        };
    });
```

### 3. Create Your First Entity

```csharp
public class Product : IAppEntity
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = nameof(Product);
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public List<string> Tags { get; set; } = new();
    
    // Your domain properties
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int StockLevel { get; set; }
}
```

### 4. Create Repository

```csharp
public class ProductRepository : MongoRepository<Product>
{
    public ProductRepository(IDataEnvironment env, IApplicationAlert alerts) 
        : base(env, alerts)
    {
    }
    
    public async Task<List<Product>> GetLowStockAsync(int threshold)
    {
        var (products, _) = await GetAllAsync(p => p.StockLevel < threshold);
        return products;
    }
}
```

### 5. Set Up Rules

```csharp
var lowStockRule = new Rule
{
    Name = "low-stock-alert",
    Topic = "inventory.checked",
    Conditions = new[]
    {
        new RuleCondition
        {
            Type = "expression",
            Expression = "entity.StockLevel < 10"
        }
    },
    Actions = new[]
    {
        new RuleActionDefinition
        {
            ActionType = "SendNotification",
            Parameters = new Dictionary<string, object>
            {
                ["template"] = "low-stock-alert",
                ["channel"] = "email"
            }
        }
    }
};
```

---

## Architecture Overview

### Core Principles

1. **Domain-Driven Design**
   - Rich domain models implementing `IAppEntity`
   - Aggregate roots with clear boundaries
   - Value objects for complex types

2. **Event-Driven Architecture**
   - Central `AppEvent` system
   - Loose coupling between components
   - Async event processing

3. **Template Pattern**
   - Separate templates from instances
   - Reusable configurations
   - Dynamic schema support

4. **Repository Pattern**
   - Generic data access layer
   - MongoDB implementation
   - Transaction support

5. **Plugin Architecture**
   - Extensible rule actions
   - Custom validators
   - Entity loaders

### System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
├─────────────────────────────────────────────────────────────┤
│                    Application Services                      │
│  ┌─────────┐  ┌──────────┐  ┌─────────┐  ┌────────────┐  │
│  │  Forms  │  │WorkItems │  │  Rules  │  │   Reports  │  │
│  └─────────┘  └──────────┘  └─────────┘  └────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                      Domain Layer                            │
│  ┌─────────┐  ┌──────────┐  ┌─────────┐  ┌────────────┐  │
│  │Entities │  │Templates │  │  KPIs   │  │   Events   │  │
│  └─────────┘  └──────────┘  └─────────┘  └────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                       │
│  ┌─────────┐  ┌──────────┐  ┌─────────┐  ┌────────────┐  │
│  │MongoDB  │  │MessageBus│  │  Auth   │  │Diagnostics │  │
│  └─────────┘  └──────────┘  └─────────┘  └────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Examples

### Dynamic Form Creation

```csharp
// Create form template
var customerForm = new FormTemplate
{
    Name = "customer-intake",
    Title = "Customer Information",
    Fields = new List<FormFieldDefinition>
    {
        new() { Name = "name", Type = "text", Label = "Full Name", Required = true },
        new() { Name = "email", Type = "email", Label = "Email", Required = true },
        new() { Name = "phone", Type = "tel", Label = "Phone" },
        new() { Name = "type", Type = "select", Label = "Customer Type", 
                Options = new[] { "Individual", "Business" } }
    }
};

// Submit form data
var formData = new Dictionary<string, object>
{
    ["name"] = "John Doe",
    ["email"] = "john@example.com",
    ["phone"] = "555-1234",
    ["type"] = "Individual"
};

var instance = await formLogic.CreateInstanceAsync("customer-intake", formData);
```

### Workflow Automation

```csharp
// Define approval workflow
var approvalRule = new Rule
{
    Name = "purchase-approval",
    Topic = "purchase-order.created",
    Conditions = new[]
    {
        new RuleCondition
        {
            Type = "expression",
            Expression = "eventData.amount > 1000"
        }
    },
    Actions = new[]
    {
        new RuleActionDefinition
        {
            ActionType = "CreateWorkItem",
            Parameters = new Dictionary<string, object>
            {
                ["template"] = "approval-request",
                ["assignTo"] = "managers",
                ["priority"] = "high"
            }
        }
    }
};
```

### KPI Tracking

```csharp
// Define KPI
var responseTimeKPI = new KPITemplate
{
    Name = "avg-response-time",
    Title = "Average Response Time",
    Query = @"
        SELECT AVG(DATEDIFF(minute, CreatedDate, FirstResponseDate)) 
        FROM WorkItems 
        WHERE Status = 'Resolved' 
        AND CreatedDate > DATEADD(day, -7, GETDATE())",
    Unit = "minutes",
    TargetValue = 30,
    WarningThreshold = 60
};

// Track KPI
await kpiLogic.EvaluateKPIAsync(responseTimeKPI.Name);
```

---

## API Reference

### Core Interfaces

| Interface | Purpose | Key Methods |
|-----------|---------|-------------|
| `IAppEntity` | Base entity interface | Id, CreatedDate, UpdatedDate |
| `IRepository<T>` | Data access | CreateAsync, GetAsync, UpdateAsync |
| `IRuleAction` | Custom rule actions | ExecuteAsync |
| `IValidator<T>` | Data validation | Validate |
| `IMessagePublisher` | Event publishing | PublishAsync |

### Common Services

| Service | Purpose | Key Operations |
|---------|---------|----------------|
| `FormLogic` | Form operations | CreateInstance, ValidateData |
| `WorkItemLogic` | Task management | CreateWorkItem, UpdateStatus |
| `RuleEngine` | Rule execution | EvaluateRules, ExecuteActions |
| `NotificationService` | Notifications | SendEmail, SendSMS |
| `KPIEvaluator` | KPI calculation | Evaluate, GetTrend |

### Rule Actions

| Category | Actions | Count |
|----------|---------|-------|
| Forms | Create, Update, Validate, Export, Notify | 5 |
| WorkItems | Create, Update, Assign, Link, Clone, Archive | 7 |
| Tables | Create, Add, Update, Query, Export, Import | 6 |
| Files | Upload, Update, Share, Archive, Restore | 5 |
| KPIs | Create, Update, Alert, Calculate, Export | 5 |
| Reports | Generate, Schedule, Email, Export, Archive | 5 |
| Notifications | Email, SMS, Web, Teams | 4 |

---

## Best Practices

### 1. Entity Design
- Keep entities focused on single responsibility
- Use value objects for complex properties
- Implement domain logic in entities

### 2. Event Usage
- Use events for cross-boundary communication
- Keep event data minimal
- Version your event schemas

### 3. Validation
- Validate at boundaries
- Use both client and server validation
- Provide clear error messages

### 4. Performance
- Use async/await throughout
- Implement caching strategically
- Monitor with diagnostics

### 5. Security
- Always authenticate API endpoints
- Use role-based permissions
- Audit sensitive operations

---

## Troubleshooting

### Common Issues

1. **MongoDB Connection Failed**
   - Check connection string
   - Verify MongoDB is running
   - Check firewall settings

2. **JWT Token Invalid**
   - Verify secret key configuration
   - Check token expiration
   - Ensure clock synchronization

3. **Rule Not Firing**
   - Check event topic matches
   - Verify conditions are met
   - Review rule engine logs

4. **Performance Issues**
   - Enable diagnostics
   - Check database indexes
   - Review async patterns

---

## Resources

- **Source Code**: [GitHub Repository](https://github.com/bformdomain)
- **NuGet Package**: [BFormDomain on NuGet](https://nuget.org/packages/BFormDomain)
- **Support**: support@bformdomain.com
- **Community**: [Discord Server](https://discord.gg/bformdomain)

---

*This documentation covers BFormDomain version 1.0. Last updated: 2025-07-08*