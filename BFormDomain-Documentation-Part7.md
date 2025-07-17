# BFormDomain Comprehensive Documentation - Part 7

## JSON Schemas and Validation Patterns

BFormDomain uses JSON Schema extensively for validating dynamic data structures, especially in the Forms and Tables systems. This provides runtime validation for user-defined schemas while maintaining flexibility.

### Core JSON Schema Usage

#### Form Field Validation

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "customerInfo": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "minLength": 1,
          "maxLength": 100,
          "pattern": "^[a-zA-Z\\s\\-']+$"
        },
        "email": {
          "type": "string",
          "format": "email"
        },
        "phone": {
          "type": "string",
          "pattern": "^\\+?[1-9]\\d{1,14}$"
        },
        "dateOfBirth": {
          "type": "string",
          "format": "date"
        }
      },
      "required": ["name", "email"]
    },
    "address": {
      "type": "object",
      "properties": {
        "street": { "type": "string" },
        "city": { "type": "string" },
        "state": { 
          "type": "string",
          "enum": ["CA", "NY", "TX", "FL", "WA"]
        },
        "zipCode": {
          "type": "string",
          "pattern": "^\\d{5}(-\\d{4})?$"
        }
      },
      "required": ["street", "city", "state", "zipCode"]
    }
  },
  "required": ["customerInfo"]
}
```

#### Dynamic Form Field Definition

```csharp
public class FormFieldDefinition
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Label { get; set; }
    public bool Required { get; set; }
    
    // JSON Schema validation
    public JObject? Validation { get; set; }
    
    // Convert to JSON Schema property
    public JObject ToJsonSchemaProperty()
    {
        var property = new JObject();
        
        switch (Type)
        {
            case "text":
                property["type"] = "string";
                if (Validation?["minLength"] != null)
                    property["minLength"] = Validation["minLength"];
                if (Validation?["maxLength"] != null)
                    property["maxLength"] = Validation["maxLength"];
                if (Validation?["pattern"] != null)
                    property["pattern"] = Validation["pattern"];
                break;
                
            case "number":
                property["type"] = "number";
                if (Validation?["minimum"] != null)
                    property["minimum"] = Validation["minimum"];
                if (Validation?["maximum"] != null)
                    property["maximum"] = Validation["maximum"];
                break;
                
            case "select":
                property["type"] = "string";
                if (Options != null)
                    property["enum"] = new JArray(Options);
                break;
                
            case "date":
                property["type"] = "string";
                property["format"] = "date";
                break;
                
            case "email":
                property["type"] = "string";
                property["format"] = "email";
                break;
        }
        
        return property;
    }
}
```

### Schema Generation and Validation

#### Dynamic Schema Builder

```csharp
public class JsonSchemaBuilder
{
    public JObject BuildFormSchema(FormTemplate template)
    {
        var schema = new JObject
        {
            ["$schema"] = "http://json-schema.org/draft-07/schema#",
            ["type"] = "object",
            ["properties"] = new JObject(),
            ["required"] = new JArray()
        };
        
        foreach (var field in template.Fields)
        {
            schema["properties"][field.Name] = field.ToJsonSchemaProperty();
            
            if (field.Required)
            {
                ((JArray)schema["required"]).Add(field.Name);
            }
        }
        
        // Add additional validation rules
        if (template.ValidationRules != null)
        {
            foreach (var rule in template.ValidationRules)
            {
                ApplyValidationRule(schema, rule);
            }
        }
        
        return schema;
    }
    
    private void ApplyValidationRule(JObject schema, ValidationRule rule)
    {
        switch (rule.Type)
        {
            case "dependencies":
                schema["dependencies"] = rule.Configuration;
                break;
                
            case "conditionalRequired":
                if (schema["allOf"] == null)
                    schema["allOf"] = new JArray();
                    
                ((JArray)schema["allOf"]).Add(new JObject
                {
                    ["if"] = rule.Condition,
                    ["then"] = new JObject
                    {
                        ["required"] = rule.RequiredFields
                    }
                });
                break;
                
            case "oneOf":
                schema["oneOf"] = rule.Options;
                break;
        }
    }
}
```

#### Schema Validator Service

```csharp
public class JsonSchemaValidationService
{
    private readonly ILogger<JsonSchemaValidationService> _logger;
    
    public ValidationResult ValidateData(JObject data, JObject schema)
    {
        try
        {
            var schemaObj = JSchema.Parse(schema.ToString());
            var isValid = data.IsValid(schemaObj, out IList<string> errors);
            
            if (isValid)
            {
                return ValidationResult.Success();
            }
            
            return ValidationResult.Failure(errors.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation error");
            return ValidationResult.Failure(new[] { "Invalid schema format" });
        }
    }
    
    public async Task<ValidationResult> ValidateWithCustomRulesAsync(
        JObject data,
        JObject schema,
        IEnumerable<ICustomValidationRule> customRules)
    {
        // First validate against JSON Schema
        var schemaResult = ValidateData(data, schema);
        if (!schemaResult.IsValid)
            return schemaResult;
            
        // Then apply custom rules
        var errors = new List<string>();
        
        foreach (var rule in customRules)
        {
            var ruleResult = await rule.ValidateAsync(data);
            if (!ruleResult.IsValid)
            {
                errors.AddRange(ruleResult.Errors);
            }
        }
        
        return errors.Any() 
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
```

### Common Validation Patterns

#### Cross-Field Dependencies

```json
{
  "type": "object",
  "properties": {
    "employmentStatus": {
      "type": "string",
      "enum": ["employed", "self-employed", "unemployed", "retired"]
    },
    "employerName": {
      "type": "string"
    },
    "annualIncome": {
      "type": "number",
      "minimum": 0
    }
  },
  "allOf": [
    {
      "if": {
        "properties": {
          "employmentStatus": { "const": "employed" }
        }
      },
      "then": {
        "required": ["employerName", "annualIncome"]
      }
    }
  ]
}
```

#### Complex Nested Validation

```csharp
public class NestedValidationPattern
{
    public JObject CreateOrderSchema()
    {
        return JObject.Parse(@"
        {
          '$schema': 'http://json-schema.org/draft-07/schema#',
          'type': 'object',
          'properties': {
            'orderItems': {
              'type': 'array',
              'minItems': 1,
              'items': {
                'type': 'object',
                'properties': {
                  'productId': {
                    'type': 'string',
                    'format': 'uuid'
                  },
                  'quantity': {
                    'type': 'integer',
                    'minimum': 1
                  },
                  'price': {
                    'type': 'number',
                    'minimum': 0
                  },
                  'discounts': {
                    'type': 'array',
                    'items': {
                      'type': 'object',
                      'properties': {
                        'type': {
                          'type': 'string',
                          'enum': ['percentage', 'fixed']
                        },
                        'value': {
                          'type': 'number',
                          'minimum': 0
                        }
                      },
                      'allOf': [
                        {
                          'if': {
                            'properties': {
                              'type': { 'const': 'percentage' }
                            }
                          },
                          'then': {
                            'properties': {
                              'value': { 'maximum': 100 }
                            }
                          }
                        }
                      ]
                    }
                  }
                },
                'required': ['productId', 'quantity', 'price']
              }
            },
            'shippingAddress': {
              '$ref': '#/definitions/address'
            },
            'billingAddress': {
              '$ref': '#/definitions/address'
            }
          },
          'required': ['orderItems'],
          'definitions': {
            'address': {
              'type': 'object',
              'properties': {
                'street': { 'type': 'string' },
                'city': { 'type': 'string' },
                'state': { 'type': 'string' },
                'zipCode': { 
                  'type': 'string',
                  'pattern': '^\\d{5}(-\\d{4})?$'
                }
              },
              'required': ['street', 'city', 'state', 'zipCode']
            }
          }
        }");
    }
}
```

### Custom Validation Rules

#### Business Rule Validator

```csharp
public interface ICustomValidationRule
{
    string RuleName { get; }
    Task<ValidationResult> ValidateAsync(JObject data);
}

public class CreditLimitValidationRule : ICustomValidationRule
{
    private readonly ICreditService _creditService;
    
    public string RuleName => "credit-limit-check";
    
    public async Task<ValidationResult> ValidateAsync(JObject data)
    {
        var customerId = data["customerId"]?.Value<Guid>();
        var orderTotal = data["orderTotal"]?.Value<decimal>();
        
        if (!customerId.HasValue || !orderTotal.HasValue)
            return ValidationResult.Success();
            
        var creditLimit = await _creditService.GetCreditLimitAsync(customerId.Value);
        var currentBalance = await _creditService.GetCurrentBalanceAsync(customerId.Value);
        
        if (currentBalance + orderTotal > creditLimit)
        {
            return ValidationResult.Failure(new[]
            {
                $"Order total ${orderTotal} would exceed credit limit. " +
                $"Available credit: ${creditLimit - currentBalance}"
            });
        }
        
        return ValidationResult.Success();
    }
}
```

#### Async External Validation

```csharp
public class ExternalDataValidationRule : ICustomValidationRule
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public string RuleName => "external-data-validation";
    
    public async Task<ValidationResult> ValidateAsync(JObject data)
    {
        var taxId = data["taxId"]?.Value<string>();
        if (string.IsNullOrEmpty(taxId))
            return ValidationResult.Success();
            
        var client = _httpClientFactory.CreateClient("ValidationAPI");
        var response = await client.GetAsync($"/api/validate/taxid/{taxId}");
        
        if (!response.IsSuccessStatusCode)
        {
            return ValidationResult.Failure(new[]
            {
                "Unable to validate tax ID at this time"
            });
        }
        
        var result = await response.Content.ReadFromJsonAsync<ValidationApiResponse>();
        
        return result.IsValid
            ? ValidationResult.Success()
            : ValidationResult.Failure(new[] { result.ErrorMessage });
    }
}
```

### Schema Evolution and Migration

```csharp
public class SchemaEvolutionService
{
    private readonly IRepository<SchemaVersion> _schemaRepo;
    
    public async Task<JObject> GetCurrentSchemaAsync(string schemaName)
    {
        var (current, _) = await _schemaRepo.GetOneAsync(
            s => s.Name == schemaName && s.IsCurrent);
            
        return current?.Schema ?? throw new SchemaNotFoundException(schemaName);
    }
    
    public async Task<JObject> MigrateDataAsync(
        JObject data,
        string schemaName,
        int fromVersion,
        int toVersion)
    {
        var migrations = await GetMigrationsAsync(schemaName, fromVersion, toVersion);
        var migratedData = data.DeepClone() as JObject;
        
        foreach (var migration in migrations)
        {
            migratedData = await ApplyMigrationAsync(migratedData, migration);
        }
        
        return migratedData;
    }
    
    private async Task<JObject> ApplyMigrationAsync(
        JObject data,
        SchemaMigration migration)
    {
        foreach (var step in migration.Steps)
        {
            switch (step.Type)
            {
                case "rename":
                    RenameField(data, step.OldPath, step.NewPath);
                    break;
                    
                case "transform":
                    await TransformFieldAsync(data, step.Path, step.Transformer);
                    break;
                    
                case "setDefault":
                    SetDefaultValue(data, step.Path, step.DefaultValue);
                    break;
                    
                case "remove":
                    RemoveField(data, step.Path);
                    break;
            }
        }
        
        return data;
    }
}
```

### Best Practices

1. **Use strict schemas** - Be explicit about data types and formats
2. **Provide meaningful errors** - Help users understand validation failures
3. **Version your schemas** - Track changes over time
4. **Test edge cases** - Validate boundary conditions
5. **Cache compiled schemas** - Improve validation performance
6. **Document patterns** - Explain complex validation rules
7. **Support schema evolution** - Plan for changes
8. **Validate incrementally** - Check fields as users type
9. **Use references** - Avoid duplicating schema definitions
10. **Monitor validation failures** - Identify common issues

---

## Comprehensive Examples

### Complete Business Application Example

This example demonstrates how all BFormDomain systems work together in a real-world scenario:

```csharp
// 1. Define Domain Entities
public class PurchaseOrder : IAppEntity
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = nameof(PurchaseOrder);
    public string OrderNumber { get; set; }
    public Guid SupplierId { get; set; }
    public List<OrderLine> Lines { get; set; } = new();
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Work container references
    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
    
    // Standard entity fields
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public List<string> Tags { get; set; } = new();
}

// 2. Create Workflow with WorkItems
public class PurchaseOrderWorkflow
{
    private readonly WorkItemLogic _workItemLogic;
    private readonly IRepository<PurchaseOrder> _orderRepo;
    private readonly AppEventPump _eventPump;
    
    public async Task<WorkItem> CreatePurchaseOrderWorkflowAsync(
        PurchaseOrder order,
        Guid workSetId)
    {
        // Create work item from template
        var workItem = await _workItemLogic.CreateFromTemplateAsync(
            "purchase-order-approval",
            new Dictionary<string, object>
            {
                ["orderNumber"] = order.OrderNumber,
                ["totalAmount"] = order.TotalAmount,
                ["supplierId"] = order.SupplierId,
                ["orderId"] = order.Id
            });
            
        workItem.HostWorkSet = workSetId;
        
        // Link order to work item
        order.HostWorkItem = workItem.Id;
        order.HostWorkSet = workSetId;
        await _orderRepo.UpdateAsync(order);
        
        // Trigger workflow started event
        await _eventPump.RaiseEventAsync(new AppEvent
        {
            Topic = "workflow.started",
            EntityId = workItem.Id,
            EntityType = "WorkItem",
            EventData = new Dictionary<string, object?>
            {
                ["workflowType"] = "purchase-order-approval",
                ["orderId"] = order.Id,
                ["amount"] = order.TotalAmount
            }
        });
        
        return workItem;
    }
}

// 3. Define Rules for Automation
public class PurchaseOrderRules
{
    public static Rule AutoApproveSmallOrders => new()
    {
        Name = "auto-approve-small-orders",
        EventMatcher = new EventMatcher
        {
            Topic = "workflow.started",
            EventData = new Dictionary<string, object>
            {
                ["workflowType"] = "purchase-order-approval"
            }
        },
        Conditions = new List<RuleCondition>
        {
            new()
            {
                Type = "expression",
                Expression = "eventData.amount < 1000"
            }
        },
        Actions = new List<RuleActionDefinition>
        {
            new()
            {
                ActionType = Constants.RuleActions.UpdateWorkItemStatus,
                Parameters = new Dictionary<string, object>
                {
                    ["status"] = "approved",
                    ["comment"] = "Auto-approved: Under approval threshold"
                }
            },
            new()
            {
                ActionType = Constants.RuleActions.SendNotification,
                Parameters = new Dictionary<string, object>
                {
                    ["template"] = "order-auto-approved",
                    ["channel"] = "email"
                }
            }
        }
    };
}

// 4. Create Dynamic Approval Form
public class ApprovalFormBuilder
{
    private readonly FormLogic _formLogic;
    
    public async Task<FormTemplate> CreateApprovalFormAsync()
    {
        var template = new FormTemplate
        {
            Name = "purchase-order-approval",
            Title = "Purchase Order Approval",
            Fields = new List<FormFieldDefinition>
            {
                new()
                {
                    Name = "decision",
                    Type = "select",
                    Label = "Approval Decision",
                    Required = true,
                    Options = new[] { "approve", "reject", "request-info" }
                },
                new()
                {
                    Name = "comments",
                    Type = "textarea",
                    Label = "Comments",
                    Required = true,
                    Validation = new JObject
                    {
                        ["minLength"] = 10,
                        ["maxLength"] = 500
                    }
                },
                new()
                {
                    Name = "approvalLimit",
                    Type = "number",
                    Label = "Approved Amount Limit",
                    ShowWhen = new JObject
                    {
                        ["field"] = "decision",
                        ["equals"] = "approve"
                    }
                }
            },
            Schema = BuildApprovalSchema()
        };
        
        await _formLogic.SaveTemplateAsync(template);
        return template;
    }
}

// 5. Track KPIs
public class PurchaseOrderKPIs
{
    private readonly KPILogic _kpiLogic;
    
    public async Task SetupKPIsAsync(Guid workSetId)
    {
        // Average approval time KPI
        await _kpiLogic.CreateFromTemplateAsync("average-approval-time", new
        {
            Name = "PO Approval Time",
            WorkSetId = workSetId,
            Query = @"
                SELECT AVG(DATEDIFF(minute, CreatedDate, UpdatedDate)) as Value
                FROM WorkItems
                WHERE Template = 'purchase-order-approval'
                AND Status = 'approved'
                AND UpdatedDate > DATEADD(day, -30, GETDATE())
            ",
            Unit = "minutes",
            TargetValue = 60, // 1 hour target
            WarningThreshold = 120 // 2 hour warning
        });
        
        // Approval rate KPI
        await _kpiLogic.CreateFromTemplateAsync("approval-rate", new
        {
            Name = "PO Approval Rate",
            WorkSetId = workSetId,
            Expression = @"
                var approved = await CountAsync(w => 
                    w.Template == 'purchase-order-approval' && 
                    w.Status == 'approved');
                var total = await CountAsync(w => 
                    w.Template == 'purchase-order-approval' && 
                    w.Status.In(['approved', 'rejected']));
                return total > 0 ? (approved / total) * 100 : 0;
            ",
            Unit = "percentage",
            TargetValue = 95
        });
    }
}

// 6. Generate Reports
public class PurchaseOrderReporting
{
    private readonly ReportLogic _reportLogic;
    
    public async Task<ReportInstance> GenerateMonthlyReportAsync(
        Guid workSetId,
        DateTime month)
    {
        var report = await _reportLogic.CreateFromTemplateAsync(
            "monthly-po-summary",
            new Dictionary<string, object>
            {
                ["workSetId"] = workSetId,
                ["month"] = month,
                ["includedSections"] = new[]
                {
                    "summary",
                    "by-supplier",
                    "by-category",
                    "approval-metrics",
                    "trend-analysis"
                }
            });
            
        // Add charts
        report.Charts.Add(new ChartSpec
        {
            Type = "line",
            Title = "Daily Order Volume",
            DataQuery = @"
                SELECT DAY(CreatedDate) as Day, COUNT(*) as Orders
                FROM PurchaseOrders
                WHERE HostWorkSet = @workSetId
                AND MONTH(CreatedDate) = MONTH(@month)
                GROUP BY DAY(CreatedDate)
                ORDER BY Day
            "
        });
        
        return report;
    }
}

// 7. Handle Notifications
[RuleAction(Constants.RuleActions.NotifyApprovers)]
public class NotifyApproversAction : IRuleAction
{
    private readonly INotificationService _notifications;
    private readonly IRepository<ApplicationUser> _userRepo;
    
    public async Task<RuleActionResponse> ExecuteAsync(
        Rule rule,
        AppEvent appEvent,
        RuleActionParameters parameters)
    {
        var amount = appEvent.EventData.Get<decimal>("amount");
        var approverRole = amount > 10000 ? "senior-approver" : "approver";
        
        // Find approvers
        var (approvers, _) = await _userRepo.GetAllAsync(
            u => u.Roles.Contains(approverRole) && u.IsActive);
            
        // Send notifications
        foreach (var approver in approvers)
        {
            await _notifications.SendAsync(new NotificationMessage
            {
                RecipientId = approver.Id,
                Subject = "Purchase Order Pending Approval",
                Body = $"PO #{appEvent.EventData["orderNumber"]} for ${amount:N2} requires your approval.",
                Priority = amount > 50000 ? NotificationPriority.High : NotificationPriority.Normal,
                RelatedEntityId = appEvent.EntityId,
                RelatedEntityType = appEvent.EntityType,
                Actions = new List<NotificationAction>
                {
                    new()
                    {
                        Label = "Review",
                        Url = $"/orders/{appEvent.EventData["orderId"]}/approve"
                    }
                }
            });
        }
        
        return RuleActionResponse.Success(new
        {
            notifiedCount = approvers.Count
        });
    }
}

// 8. Complete Integration
public class PurchaseOrderService
{
    // ... dependency injection of all services ...
    
    public async Task<ProcessResult> ProcessPurchaseOrderAsync(
        PurchaseOrderRequest request,
        Guid userId,
        Guid workSetId)
    {
        using var transaction = await _orderRepo.OpenTransactionAsync();
        
        try
        {
            // 1. Create order entity
            var order = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = GenerateOrderNumber(),
                SupplierId = request.SupplierId,
                Lines = request.Lines,
                Status = OrderStatus.Draft,
                TotalAmount = request.Lines.Sum(l => l.Quantity * l.UnitPrice),
                CreatedDate = DateTime.UtcNow,
                Creator = userId,
                Tags = new[] { "pending-approval", $"supplier:{request.SupplierId}" }
            };
            
            await _orderRepo.CreateAsync(transaction, order);
            
            // 2. Create workflow
            var workItem = await _workflowService.CreatePurchaseOrderWorkflowAsync(
                order, workSetId);
                
            // 3. Attach documents
            foreach (var doc in request.Attachments)
            {
                await _fileLogic.AttachFileAsync(
                    order.Id,
                    order.EntityType,
                    doc.FileId,
                    doc.Description);
            }
            
            // 4. Add to dynamic table for reporting
            await _tableLogic.AddRowAsync("purchase-orders", new
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Supplier = request.SupplierName,
                Amount = order.TotalAmount,
                Status = order.Status.ToString(),
                CreatedBy = userId,
                CreatedDate = order.CreatedDate,
                WorkItemId = workItem.Id
            });
            
            // 5. Record in audit log
            await _auditService.LogAsync(new AuditEntry
            {
                EntityType = "PurchaseOrder",
                EntityId = order.Id,
                Action = "Create",
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Details = new
                {
                    OrderNumber = order.OrderNumber,
                    Amount = order.TotalAmount,
                    LineCount = order.Lines.Count
                }
            });
            
            await transaction.CommitAsync();
            
            // 6. Trigger post-creation events (outside transaction)
            await _eventPump.RaiseEventAsync(new AppEvent
            {
                Topic = "purchase-order.created",
                EntityId = order.Id,
                EntityType = "PurchaseOrder",
                EventData = new Dictionary<string, object?>
                {
                    ["orderId"] = order.Id,
                    ["workItemId"] = workItem.Id,
                    ["amount"] = order.TotalAmount,
                    ["supplierId"] = order.SupplierId
                }
            });
            
            return new ProcessResult
            {
                Success = true,
                OrderId = order.Id,
                WorkItemId = workItem.Id,
                Message = $"Purchase order {order.OrderNumber} created successfully"
            };
        }
        catch (Exception ex)
        {
            await transaction.AbortAsync();
            _logger.LogError(ex, "Failed to process purchase order");
            throw;
        }
    }
}
```

### Best Practices Summary

1. **Design for extensibility** - Use interfaces and plugins
2. **Embrace event-driven architecture** - Loose coupling through events
3. **Validate thoroughly** - Both static and dynamic validation
4. **Track everything** - Audit logs, event history, metrics
5. **Plan for scale** - Use async patterns, implement caching
6. **Secure by default** - Authentication, authorization, data encryption
7. **Monitor performance** - Use diagnostics and alerts
8. **Document patterns** - Clear examples and guidelines
9. **Test comprehensively** - Unit, integration, and system tests
10. **Iterate based on usage** - Monitor and improve

---