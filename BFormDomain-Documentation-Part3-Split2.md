# BFormDomain Part3 Documentation (Split 2)

#### RuleEvaluator
Evaluates individual rules:

```csharp
public class RuleEvaluator
{
    private readonly Dictionary<string, IRuleActionEvaluator> _actionEvaluators;
    private readonly Dictionary<string, IEventAppender> _eventAppenders;
    private readonly ILogger<RuleEvaluator> _logger;
    
    public async Task<RuleEvaluationResult> EvaluateRuleAsync(Rule rule, AppEvent appEvent)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new RuleEvaluationResult
        {
            RuleName = rule.Name,
            EventId = appEvent.Id,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            // Prepare event data
            var eventData = JObject.Parse(appEvent.EntityPayload.ToJson());
            
            // Apply event appenders
            await ApplyEventAppenders(rule, eventData, appEvent);
            
            // Evaluate conditions
            var conditionsResult = await EvaluateConditions(rule, eventData);
            result.ConditionsMet = conditionsResult;
            
            if (!conditionsResult)
            {
                result.Status = RuleEvaluationStatus.ConditionsNotMet;
                return result;
            }
            
            // Execute actions
            await ExecuteActions(rule, eventData, result);
            
            result.Status = RuleEvaluationStatus.Success;
        }
        catch (Exception ex)
        {
            result.Status = RuleEvaluationStatus.Failed;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, $"Rule evaluation failed for rule {rule.Name}");
        }
        finally
        {
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    private async Task<bool> EvaluateConditions(Rule rule, JObject eventData)
    {
        if (!rule.Conditions.Any())
        {
            return true; // No conditions means always execute
        }
        
        var results = new List<bool>();
        
        foreach (var condition in rule.Conditions)
        {
            var conditionResult = await EvaluateCondition(condition, eventData);
            results.Add(conditionResult);
        }
        
        // Apply logic operator
        return rule.ConditionLogic == RuleLogicOperator.And
            ? results.All(r => r)
            : results.Any(r => r);
    }
    
    private async Task<bool> EvaluateCondition(RuleCondition condition, JObject eventData)
    {
        try
        {
            // Get value from event data
            JToken? actualValue = null;
            
            if (!string.IsNullOrEmpty(condition.Query))
            {
                actualValue = eventData.SelectToken(condition.Query);
            }
            
            // Perform comparison
            var result = condition.Check switch
            {
                RuleConditionCheck.Exists => actualValue != null,
                RuleConditionCheck.NotExists => actualValue == null,
                RuleConditionCheck.Equals => CompareValues(actualValue, condition.Value, condition),
                RuleConditionCheck.NotEquals => !CompareValues(actualValue, condition.Value, condition),
                RuleConditionCheck.GreaterThan => CompareNumeric(actualValue, condition.Value, (a, b) => a > b),
                RuleConditionCheck.LessThan => CompareNumeric(actualValue, condition.Value, (a, b) => a < b),
                RuleConditionCheck.GreaterOrEqual => CompareNumeric(actualValue, condition.Value, (a, b) => a >= b),
                RuleConditionCheck.LessOrEqual => CompareNumeric(actualValue, condition.Value, (a, b) => a <= b),
                RuleConditionCheck.Contains => ContainsValue(actualValue, condition.Value, condition),
                RuleConditionCheck.StartsWith => StartsWithValue(actualValue, condition.Value, condition),
                RuleConditionCheck.EndsWith => EndsWithValue(actualValue, condition.Value, condition),
                RuleConditionCheck.Matches => MatchesRegex(actualValue, condition.Value),
                RuleConditionCheck.In => IsInValues(actualValue, condition.Values),
                RuleConditionCheck.Empty => IsEmpty(actualValue),
                RuleConditionCheck.NotEmpty => !IsEmpty(actualValue),
                _ => false
            };
            
            return condition.Negate ? !result : result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to evaluate condition: {condition.Query}");
            return false;
        }
    }
    
    private async Task ExecuteActions(Rule rule, JObject eventData, RuleEvaluationResult result)
    {
        var sortedActions = rule.Actions.OrderBy(a => a.Priority).ToList();
        
        foreach (var action in sortedActions)
        {
            var actionResult = new ActionEvaluationResult
            {
                ActionName = action.Invocation.Name,
                StartTime = DateTime.UtcNow
            };
            
            try
            {
                // Check action-specific conditions
                if (action.Conditions != null && action.Conditions.Any())
                {
                    var actionConditionsRule = new Rule
                    {
                        Conditions = action.Conditions,
                        ConditionLogic = RuleLogicOperator.And
                    };
                    
                    var conditionsMet = await EvaluateConditions(actionConditionsRule, eventData);
                    if (!conditionsMet)
                    {
                        actionResult.Status = ActionEvaluationStatus.ConditionsNotMet;
                        result.ActionResults.Add(actionResult);
                        continue;
                    }
                }
                
                // Execute action
                var evaluator = _actionEvaluators[action.Invocation.Name];
                var updatedEventData = await evaluator.EvaluateAsync(action.Invocation.Args, eventData);
                
                // Update event data with results
                if (!string.IsNullOrEmpty(action.Invocation.ResultProperty))
                {
                    eventData[action.Invocation.ResultProperty] = JToken.FromObject(updatedEventData);
                }
                else
                {
                    eventData.Merge(updatedEventData);
                }
                
                actionResult.Status = ActionEvaluationStatus.Success;
            }
            catch (Exception ex)
            {
                actionResult.Status = ActionEvaluationStatus.Failed;
                actionResult.ErrorMessage = ex.Message;
                
                if (!action.ContinueOnError)
                {
                    result.ActionResults.Add(actionResult);
                    throw;
                }
            }
            finally
            {
                actionResult.Duration = DateTime.UtcNow - actionResult.StartTime;
                result.ActionResults.Add(actionResult);
            }
        }
    }
}
```

### Event Appenders

Event appenders enhance event data before rule evaluation:

#### IEventAppender
Base interface for event appenders:

```csharp
public interface IEventAppender
{
    string Name { get; }
    Task AppendAsync(JObject eventData, JObject parameters);
}
```

#### CurrentDateTimeAppender
Adds current timestamp:

```csharp
public class CurrentDateTimeAppender : IEventAppender
{
    public string Name => "CurrentDateTime";
    
    public Task AppendAsync(JObject eventData, JObject parameters)
    {
        var property = parameters["property"]?.ToString() ?? "currentDateTime";
        var format = parameters["format"]?.ToString();
        
        var now = DateTime.UtcNow;
        
        if (!string.IsNullOrEmpty(format))
        {
            eventData[property] = now.ToString(format);
        }
        else
        {
            eventData[property] = now;
        }
        
        return Task.CompletedTask;
    }
}
```

#### LoadEntityDataFromReferenceAppender
Loads related entity data:

```csharp
public class LoadEntityDataFromReferenceAppender : IEventAppender
{
    private readonly EntityReferenceLoader _entityLoader;
    
    public string Name => "LoadEntityData";
    
    public async Task AppendAsync(JObject eventData, JObject parameters)
    {
        var referenceQuery = parameters["referenceQuery"]?.ToString();
        var targetProperty = parameters["targetProperty"]?.ToString() ?? "entityData";
        var fields = parameters["fields"]?.ToObject<string[]>();
        
        if (string.IsNullOrEmpty(referenceQuery))
        {
            return;
        }
        
        var reference = eventData.SelectToken(referenceQuery)?.ToString();
        if (string.IsNullOrEmpty(reference))
        {
            return;
        }
        
        try
        {
            var entityData = await _entityLoader.LoadJson(reference);
            
            if (entityData != null)
            {
                if (fields != null && fields.Any())
                {
                    // Only include specified fields
                    var filteredData = new JObject();
                    foreach (var field in fields)
                    {
                        var value = entityData.SelectToken(field);
                        if (value != null)
                        {
                            filteredData[field] = value;
                        }
                    }
                    eventData[targetProperty] = filteredData;
                }
                else
                {
                    eventData[targetProperty] = entityData;
                }
            }
        }
        catch (Exception ex)
        {
            eventData[$"{targetProperty}_error"] = ex.Message;
        }
    }
}
```

### Rule Examples

#### Creating User Notification Rules
```csharp
public class UserNotificationRules
{
    // Welcome new users
    public static Rule CreateWelcomeUserRule()
    {
        return new Rule
        {
            Name = "WelcomeNewUser",
            Title = "Welcome New User",
            Tags = new List<string> { "user", "onboarding", "notification" },
            
            TopicBindings = new List<string> { "User.Created" },
            
            Conditions = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Query = "$.EmailVerified",
                    Check = RuleConditionCheck.Equals,
                    Value = JToken.FromObject(true)
                }
            },
            
            Actions = new List<RuleAction>
            {
                // Send welcome email
                new RuleAction
                {
                    Priority = 1,
                    Invocation = new RuleExpressionInvocation
                    {
                        Name = "RuleActionRequestNotification",
                        Args = JObject.FromObject(new
                        {
                            NotificationTemplate = "WelcomeEmail",
                            UserIdQuery = "$.Id",
                            Parameters = new
                            {
                                UserName = "$.DisplayName",
                                LoginUrl = "#{BaseUrl}/login"
                            }
                        })
                    }
                },
                // Create personal workspace
                new RuleAction
                {
                    Priority = 2,
                    Invocation = new RuleExpressionInvocation
                    {
                        Name = "RuleActionCreateWorkSet",
                        Args = JObject.FromObject(new
                        {
                            TemplateName = "PersonalWorkspace",
                            TitleQuery = "$.DisplayName + '''s Workspace'",
                            Description = "Personal workspace for organizing your work",
                            Settings = new
                            {
                                isPersonal = true,
                                theme = "default"
                            }
                        }),
                        ResultProperty = "personalWorkspaceId"
                    }
                },
                // Log onboarding event
                new RuleAction
                {
                    Priority = 3,
                    Invocation = new RuleExpressionInvocation
                    {
                        Name = "RuleActionLogEventData",
                        Args = JObject.FromObject(new
                        {
                            EventType = "UserOnboarded",
                            Message = "User successfully onboarded with personal workspace",
                            Data = new
                            {
                                UserId = "$.Id",
                                WorkspaceId = "$.personalWorkspaceId"
                            }
                        })
                    }
                }
            },
            
            IsEnabled = true,
            Priority = 100
        };
    }
    
    // Notify on high priority work items
    public static Rule CreateHighPriorityWorkItemRule()
    {
        return new Rule
        {
            Name = "HighPriorityWorkItemNotification",
            Title = "High Priority Work Item Notification",
            Tags = new List<string> { "workitem", "priority", "notification" },
            
            TopicBindings = new List<string> 
            { 
                "WorkItem.Created", 
                "WorkItem.Updated" 
            },
            
            Conditions = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Query = "$.Priority",
                    Check = RuleConditionCheck.GreaterOrEqual,
                    Value = JToken.FromObject(3) // High or Critical
                },
                new RuleCondition
                {
                    Query = "$.Status",
                    Check = RuleConditionCheck.NotEquals,
                    Value = JToken.FromObject(4) // Not closed
                }
            },
            
            Actions = new List<RuleAction>
            {
                new RuleAction
                {
                    Invocation = new RuleExpressionInvocation
                    {
                        Name = "RuleActionRequestNotification",
                        Args = JObject.FromObject(new
                        {
                            GroupByTags = new[] { "manager", "supervisor" },
                            Subject = "High Priority Work Item Requires Attention",
                            EmailText = "A high priority work item has been created or updated and requires immediate attention.",
                            ToastText = "High priority work item needs attention",
                            Channels = new[] { "Email", "Toast", "SMS" },
                            Priority = "High",
                            ActionUrl = "/workitem/$.Id"
                        })
                    }
                }
            },
            
            // Rate limiting to prevent spam
            CooldownPeriod = TimeSpan.FromMinutes(15),
            MaxExecutionsPerHour = 4,
            
            IsEnabled = true,
            Priority = 50 // Higher priority than normal rules
        };
    }
}
```

#### Creating Data Processing Rules
```csharp
public class DataProcessingRules
{
    // Process form submissions
    public static Rule CreateFormSubmissionProcessingRule()
    {
        return new Rule
        {
            Name = "ProcessFormSubmission",
            Title = "Process Form Submission",
            Tags = new List<string> { "form", "data-processing", "automation" },
            
            TopicBindings = new List<string> { "Form.Submitted" },
            
            Conditions = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Query = "$.Template",
                    Check = RuleConditionCheck.In,
                    Values = new List<JToken>
                    {
                        JToken.FromObject("CustomerFeedback"),
                        JToken.FromObject("ContactForm"),
                        JToken.FromObject("SupportRequest")
                    }
                }
            },
            
            Actions = new List<RuleAction>
            {
                // Insert into analytics table
                new RuleAction
                {
                    Priority = 1,
                    Invocation = new RuleExpressionInvocation
                    {
                        Name = "RuleActionInsertTableData",
                        Args = JObject.FromObject(new
                        {
                            TableTemplate = "FormSubmissions",
                            Map = new[]
                            {
                                new { ToField = "formId", FromQuery = "$.Id" },
                                new { ToField = "formTemplate", FromQuery = "$.Template" },
                                new { ToField = "submittedAt", FromQuery = "$.UpdatedDate" },
                                new { ToField = "submittedBy", FromQuery = "$.LastModifier" },
                                new { ToField = "workSetId", FromQuery = "$.HostWorkSet" },
                                new { ToField = "dataSize", FromQuery = "$.Content | length" }
                            },
                            Tags = new[] { "analytics", "submission" }
                        })
                    }
                },
                // Create work item for follow-up
                new RuleAction
                {
                    Priority = 2,
                    Conditions = new List<RuleCondition>
                    {
                        new RuleCondition
                        {
                            Query = "$.Template",
                            Check = RuleConditionCheck.Equals,
                            Value = JToken.FromObject("SupportRequest")
                        }
                    },
                    Invocation = new RuleExpressionInvocation
                    {
                        Name = "RuleActionCreateWorkItem",
                        Args = JObject.FromObject(new
                        {
                            TemplateName = "SupportTicket",
                            WorkSetQuery = "$.HostWorkSet",
                            TitleQuery = "'Support Request: ' + $.Content.subject",
                            Description = "Support request submitted via form",
                            UserAssigneeQuery = "#{SupportTeamLeadId}",
                            Priority = 2,
                            Sections = new
                            {
                                CustomerInfo = new
                                {
                                    EntityType = "Form",
                                    EntityId = "$.Id"
                                }
                            }
                        }),
                        ResultProperty = "supportTicketId"
                    }
                },
                // Send confirmation
                new RuleAction
                {
                    Priority = 3,
                    Invocation = new RuleExpressionInvocation
                    {
                        Name = "RuleActionRequestNotification",
                        Args = JObject.FromObject(new
                        {
                            UserIdQuery = "$.LastModifier",
                            NotificationTemplate = "FormSubmissionConfirmation",
                            Parameters = new
                            {
                                FormType = "$.Template",
                                SubmissionId = "$.Id",
                                TicketId = "$.supportTicketId"
                            }
                        })
                    }
                }
            },
            
            IsEnabled = true,
            Priority = 100
        };
    }
    
    // Automated data quality checks
    public static Rule CreateDataQualityRule()
    {
        return new Rule
        {
            Name = "DataQualityCheck",
            Title = "Automated Data Quality Check",
            Tags = new List<string> { "data-quality", "validation", "monitoring" },
            
            TopicBindings = new List<string> 
            { 
                "Table.*.RowInserted",
                "Table.*.RowUpdated" 
            },
            
            Actions = new List<RuleAction>
            {
                // Check for duplicate records
                new RuleAction
                {
                    Priority = 1,
                    Invocation = new RuleExpressionInvocation
                    {
                        Name = "RuleActionSelectTableRows",
                        Args = JObject.FromObject(new
                        {
                            TableTemplateQuery = "$.OriginTemplate.replace('Table.', '').replace('.RowInserted', '')",
                            Filter = new
                            {
                                email = "$.EntityPayload.email",
                                Id = new { $ne = "$.OriginId" }
                            },
                            MaxRows = 1
                        }),
                        ResultProperty = "duplicateCheck"
                    }
                },
                // Flag potential duplicates
                new RuleAction
                {
                    Priority = 2,
                    Conditions = new List<RuleCondition>
                    {
                        new RuleCondition
                        {
                            Query = "$.duplicateCheck.length",
                            Check = RuleConditionCheck.GreaterThan,
                            Value = JToken.FromObject(0)
                        }
                    },
                    Invocation = new RuleExpressionInvocation
                    {
                        Name = "RuleActionCustomEvent",
                        Args = JObject.FromObject(new
                        {
                            Topic = "DataQuality.DuplicateDetected",
                            Data = new
                            {
                                TableTemplate = "$.OriginTemplate",
                                RecordId = "$.OriginId",
                                DuplicateIds = "$.duplicateCheck[*].Id",
                                MatchedField = "email"
                            }
                        })
                    }
                }
            },
            
            IsEnabled = true,
            Priority = 200
        };
    }
}
```

### Best Practices

1. **Design atomic rules** - Keep rules focused on single responsibilities
2. **Use meaningful conditions** - Make rule logic clear and maintainable
3. **Implement proper error handling** - Use ContinueOnError appropriately
4. **Apply rate limiting** - Prevent rule execution spam
5. **Order actions by priority** - Control execution sequence
6. **Use wildcards judiciously** - Balance flexibility with performance
7. **Test rule logic thoroughly** - Validate conditions and actions
8. **Monitor rule performance** - Track execution times and failures
9. **Document rule purposes** - Explain business logic clearly
10. **Version rule definitions** - Track changes over time

---

## AppEvents System

The AppEvents System provides the foundational event-driven architecture that enables loose coupling, audit trails, and real-time processing across the platform. It handles event generation, distribution, and consumption with reliability and scalability.

### Core Components

#### AppEvent
The central event model:

```csharp
public class AppEvent : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // Event classification
    public string? Topic { get; set; }              // Event type/category
    public string? OriginEntityType { get; set; }   // Source entity type
    public string? OriginTemplate { get; set; }     // Source template
    public Guid? OriginId { get; set; }             // Source entity ID
    
    // Event correlation
    public Guid EventLine { get; set; }            // Groups related events
    public int EventGeneration { get; set; }        // Distance from root event
    public bool IsNatural { get; set; }            // User vs system generated
    
    // Processing state
    public AppEventState State { get; set; }        // Current state
    public DateTime CreatedDate { get; set; }       // When created
    public DateTime DeferredUntil { get; set; }     // When to process
    public DateTime TakenExpiration { get; set; }   // Processing timeout
    public string? ProcessorId { get; set; }        // Who's processing
    
    // Event data
    public BsonDocument EntityPayload { get; set; } // Event payload
    public List<string> Tags { get; set; } = new(); // Event tags
    public List<string> EntityTags { get; set; } = new(); // Source entity tags
    
    // Metadata
    public JObject? Metadata { get; set; }          // Additional data
    public string? CorrelationId { get; set; }      // External correlation
    public string? CausationId { get; set; }        // Causing event
}

public enum AppEventState
{
    Pending = 0,        // Waiting to be processed
    Taken = 1,          // Being processed
    Completed = 2,      // Successfully processed
    Failed = 3,         // Processing failed
    Deferred = 4,       // Postponed processing
    Cancelled = 5       // Cancelled
}
```

#### AppEventOrigin
Tracks event sources:

```csharp
public class AppEventOrigin
{
    public string EntityType { get; set; }         // Source entity type
    public string? Template { get; set; }          // Source template
    public Guid Id { get; set; }                   // Source entity ID
    public List<string> Tags { get; set; } = new(); // Source tags
    
    public static AppEventOrigin FromEntity<T>(T entity) where T : IAppEntity
    {
        return new AppEventOrigin
        {
            EntityType = entity.EntityType,
            Template = entity.Template,
            Id = entity.Id,
            Tags = entity.Tags
        };
    }
}
```

### Event Services

#### AppEventSink
Receives and stores events:

```csharp
public class AppEventSink
{
    private readonly IRepository<AppEvent> _eventRepo;
    private readonly AppEventSinkOptions _options;
    
    public async Task SinkAsync(
        string topic,
        AppEventOrigin origin,
        BsonDocument payload,
        bool isNatural = true,
        Guid? eventLine = null,
        int eventGeneration = 0,
        CancellationToken cancellationToken = default)
    {
        var appEvent = new AppEvent
        {
            Topic = topic,
            OriginEntityType = origin.EntityType,
            OriginTemplate = origin.Template,
            OriginId = origin.Id,
            EntityPayload = payload,
            EntityTags = origin.Tags,
            IsNatural = isNatural,
            EventLine = eventLine ?? Guid.NewGuid(),
            EventGeneration = eventGeneration,
            State = AppEventState.Pending,
            CreatedDate = DateTime.UtcNow,
            DeferredUntil = DateTime.UtcNow
        };
        
        // Apply event enrichment
        await EnrichEvent(appEvent);
        
        using (var tc = await _eventRepo.OpenTransactionAsync(cancellationToken))
        {
            await _eventRepo.CreateAsync(tc, appEvent, cancellationToken);
            await tc.CommitAsync(cancellationToken);
        }
    }
    
    public async Task SinkAsync(
        ITransactionContext transactionContext,
        string topic,
        AppEventOrigin origin,
        BsonDocument payload,
        bool isNatural = true,
        Guid? eventLine = null,
        int eventGeneration = 0,
        CancellationToken cancellationToken = default)
    {
        var appEvent = new AppEvent
        {
            Topic = topic,
            OriginEntityType = origin.EntityType,
            OriginTemplate = origin.Template,
            OriginId = origin.Id,
            EntityPayload = payload,
            EntityTags = origin.Tags,
            IsNatural = isNatural,
            EventLine = eventLine ?? Guid.NewGuid(),
            EventGeneration = eventGeneration,
            State = AppEventState.Pending,
            CreatedDate = DateTime.UtcNow,
            DeferredUntil = DateTime.UtcNow
        };
        
        await EnrichEvent(appEvent);
        await _eventRepo.CreateAsync(transactionContext, appEvent, cancellationToken);
    }
    
    private async Task EnrichEvent(AppEvent appEvent)
    {
        // Add standard metadata
        appEvent.Metadata = JObject.FromObject(new
        {
            machineName = Environment.MachineName,
            processId = Environment.ProcessId,
            threadId = Thread.CurrentThread.ManagedThreadId,
            timestamp = DateTime.UtcNow
        });
        
        // Apply configured enrichers
        foreach (var enricher in _options.Enrichers)
        {
            await enricher.EnrichAsync(appEvent);
        }
    }
}
```

#### AppEventPump
Distributes events from storage to message bus:

```csharp
public class AppEventPump : BackgroundService
{
    private readonly IRepository<AppEvent> _eventRepo;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ApplicationTopologyCatalog _topology;
    private readonly AppEventPumpOptions _options;
    private readonly ILogger<AppEventPump> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check if this server should run the pump
        if (!await ShouldRunPump())
        {
            _logger.LogInformation("AppEventPump not enabled for this server role");
            return;
        }
        
        _logger.LogInformation("AppEventPump starting");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEvents(stoppingToken);
                await GroomFailedEvents(stoppingToken);
                
                await Task.Delay(_options.PumpInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AppEventPump execution");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        
        _logger.LogInformation("AppEventPump stopped");
    }
    
    private async Task ProcessPendingEvents(CancellationToken cancellationToken)
    {
        var batchSize = _options.BatchSize;
        var processingTimeout = _options.ProcessingTimeout;
        
        // Get pending events
        var (events, _) = await _eventRepo.GetOrderedAsync(
            e => e.State == AppEventState.Pending && 
                 e.DeferredUntil <= DateTime.UtcNow,
            e => e.CreatedDate,
            ascending: true);
            
        if (!events.Any())
        {
            return;
        }
        
        var batch = events.Take(batchSize).ToList();
        
        foreach (var appEvent in batch)
        {
            try
            {
                // Mark as taken
                appEvent.State = AppEventState.Taken;
                appEvent.ProcessorId = Environment.MachineName;
                appEvent.TakenExpiration = DateTime.UtcNow.Add(processingTimeout);
                
                await _eventRepo.UpdateAsync(appEvent, cancellationToken);
                
                // Publish to message bus
                await PublishEvent(appEvent, cancellationToken);
                
                // Mark as completed
                appEvent.State = AppEventState.Completed;
                await _eventRepo.UpdateAsync(appEvent, cancellationToken);
                
                _logger.LogDebug($"Published event {appEvent.Id} with topic {appEvent.Topic}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish event {appEvent.Id}");
                
                // Mark as failed
                appEvent.State = AppEventState.Failed;
                await _eventRepo.UpdateAsync(appEvent, cancellationToken);
            }
        }
    }
    
    private async Task PublishEvent(AppEvent appEvent, CancellationToken cancellationToken)
    {
        var message = new EventMessage
        {
            EventId = appEvent.Id,
            Topic = appEvent.Topic,
            OriginEntityType = appEvent.OriginEntityType,
            OriginTemplate = appEvent.OriginTemplate,
            OriginId = appEvent.OriginId,
            EventLine = appEvent.EventLine,
            EventGeneration = appEvent.EventGeneration,
            IsNatural = appEvent.IsNatural,
            EntityPayload = appEvent.EntityPayload,
            Tags = appEvent.Tags,
            EntityTags = appEvent.EntityTags,
            CreatedDate = appEvent.CreatedDate,
            Metadata = appEvent.Metadata
        };
        
        await _messagePublisher.SendAsync(message, appEvent.Topic ?? "default");
    }
    
    private async Task GroomFailedEvents(CancellationToken cancellationToken)
    {
        var retryThreshold = DateTime.UtcNow.Subtract(_options.FailedEventRetryInterval);
        
        // Find events that have been taken but expired
        var (expiredEvents, _) = await _eventRepo.GetAsync(
            e => e.State == AppEventState.Taken && 
                 e.TakenExpiration < DateTime.UtcNow);
                 
        foreach (var expiredEvent in expiredEvents)
        {
            // Reset to pending for retry
            expiredEvent.State = AppEventState.Pending;
            expiredEvent.ProcessorId = null;
            expiredEvent.DeferredUntil = DateTime.UtcNow.Add(_options.RetryDelay);
            
            await _eventRepo.UpdateAsync(expiredEvent, cancellationToken);
        }
        
        // Find old failed events for cleanup
        var (oldFailedEvents, _) = await _eventRepo.GetAsync(
            e => e.State == AppEventState.Failed && 
                 e.CreatedDate < retryThreshold);
                 
        foreach (var failedEvent in oldFailedEvents)
        {
            if (_options.DeleteFailedEvents)
            {
                await _eventRepo.DeleteAsync(e => e.Id == failedEvent.Id, cancellationToken);
            }
            else
            {
                failedEvent.State = AppEventState.Cancelled;
                await _eventRepo.UpdateAsync(failedEvent, cancellationToken);
            }
        }
    }
}
```

#### AppEventDistributer
Distributes events to consumers:

```csharp
public class AppEventDistributer : IHostedService
{
    private readonly IMessageListener _messageListener;
    private readonly List<IAppEventConsumer> _consumers;
    private readonly ILogger<AppEventDistributer> _logger;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _messageListener.Initialize("events", "app_events");
        
        // Set up message handling
        _messageListener.Listen(
            new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(
                typeof(EventMessage),
                (message, ct, ack) => ProcessEventMessage((EventMessage)message, ct, ack)
            )
        );
        
        _logger.LogInformation("AppEventDistributer started");
        return Task.CompletedTask;
    }
    
    private void ProcessEventMessage(
        EventMessage message, 
        CancellationToken cancellationToken,
        IMessageAcknowledge acknowledge)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var appEvent = new AppEvent
                {
                    Id = message.EventId,
                    Topic = message.Topic,
                    OriginEntityType = message.OriginEntityType,
                    OriginTemplate = message.OriginTemplate,
                    OriginId = message.OriginId,
                    EventLine = message.EventLine,
                    EventGeneration = message.EventGeneration,
                    IsNatural = message.IsNatural,
                    EntityPayload = message.EntityPayload,
                    Tags = message.Tags,
                    EntityTags = message.EntityTags,
                    CreatedDate = message.CreatedDate,
                    Metadata = message.Metadata
                };
                
                // Distribute to all consumers
                foreach (var consumer in _consumers)
                {
                    try
                    {
                        consumer.ConsumeEvents(appEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Consumer {consumer.GetType().Name} failed to process event {appEvent.Id}");
                    }
                }
                
                acknowledge.Ack();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process event message {message.EventId}");
                acknowledge.Nack(requeue: true);
            }
        }, cancellationToken);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _messageListener?.Dispose();
        _logger.LogInformation("AppEventDistributer stopped");
        return Task.CompletedTask;
    }
}
```

### Event Examples

#### Creating Application Events
```csharp
public class EventExamples
{
    // User lifecycle events
    public static async Task DemonstrateUserEvents()
    {
        // User registration
        await _eventSink.SinkAsync(
            topic: "User.Registered",
            origin: AppEventOrigin.FromEntity(user),
            payload: user.ToBsonDocument(),
            isNatural: true
        );
        
        // Email verification
        await _eventSink.SinkAsync(
            topic: "User.EmailVerified",
            origin: AppEventOrigin.FromEntity(user),
            payload: BsonDocument.Parse(JsonConvert.SerializeObject(new
            {
                userId = user.Id,
                email = user.Email,
                verifiedAt = DateTime.UtcNow
            })),
            isNatural: false // System generated
        );
        
        // User login
        await _eventSink.SinkAsync(
            topic: "User.LoggedIn",
            origin: AppEventOrigin.FromEntity(user),
            payload: BsonDocument.Parse(JsonConvert.SerializeObject(new
            {
                userId = user.Id,
                loginTime = DateTime.UtcNow,
                ipAddress = "192.168.1.100",
                userAgent = "Mozilla/5.0...",
                sessionId = Guid.NewGuid()
            }))
        );
    }
    
    // Business process events
    public static async Task DemonstrateBusinessProcessEvents()
    {
        // Order processing workflow
        var order = new Order { Id = Guid.NewGuid(), CustomerId = Guid.NewGuid() };
        var eventLine = Guid.NewGuid(); // Group related events
        
        // Order created
        await _eventSink.SinkAsync(
            topic: "Order.Created",
            origin: new AppEventOrigin 
            { 
                EntityType = "Order", 
                Id = order.Id 
            },
            payload: order.ToBsonDocument(),
            eventLine: eventLine,
            eventGeneration: 0
        );
        
        // Payment processed (child event)
        await _eventSink.SinkAsync(
            topic: "Order.PaymentProcessed",
            origin: new AppEventOrigin 
            { 
                EntityType = "Order", 
                Id = order.Id 
            },
            payload: BsonDocument.Parse(JsonConvert.SerializeObject(new
            {
                orderId = order.Id,
                amount = 99.99,
                paymentMethod = "CreditCard",
                transactionId = "TXN-12345"
            })),
            eventLine: eventLine,
            eventGeneration: 1
        );
        
        // Inventory updated (child event)
        await _eventSink.SinkAsync(
            topic: "Inventory.Updated",
            origin: new AppEventOrigin 
            { 
                EntityType = "Order", 
                Id = order.Id 
            },
            payload: BsonDocument.Parse(JsonConvert.SerializeObject(new
            {
                orderId = order.Id,
                items = new[]
                {
                    new { productId = "PROD-001", quantity = 2 },
                    new { productId = "PROD-002", quantity = 1 }
                }
            })),
            eventLine: eventLine,
            eventGeneration: 1
        );
        
        // Order fulfilled (child event)
        await _eventSink.SinkAsync(
            topic: "Order.Fulfilled",
            origin: new AppEventOrigin 
            { 
                EntityType = "Order", 
                Id = order.Id 
            },
            payload: BsonDocument.Parse(JsonConvert.SerializeObject(new
            {
                orderId = order.Id,
                fulfilledAt = DateTime.UtcNow,
                trackingNumber = "TRACK-98765",
                carrier = "UPS"
            })),
            eventLine: eventLine,
            eventGeneration: 2
        );
    }
}
```

### Best Practices

1. **Use meaningful topic hierarchies** - Structure topics logically (Entity.Action)
2. **Include sufficient event data** - Balance completeness with performance
3. **Implement proper correlation** - Use EventLine for related events
4. **Handle failures gracefully** - Implement retry and dead letter patterns
5. **Monitor event processing** - Track throughput and latency
6. **Keep events immutable** - Never modify event data after creation
7. **Design for idempotency** - Handle duplicate event processing
8. **Use natural vs system flags** - Distinguish user actions from system events
9. **Implement event versioning** - Handle schema evolution
10. **Archive old events** - Manage storage growth over time

---

## Authorization System

The Authorization System provides comprehensive authentication and authorization capabilities built on ASP.NET Core Identity with MongoDB persistence. It includes JWT token management, role-based authorization, user invitation workflows, and secure password handling.

### Core Components

#### ApplicationUser
The primary user entity extending MongoDB Identity:

```csharp
[CollectionName("ApplicationUsers")]
public class ApplicationUser : MongoIdentityUser<Guid>, IDataModel
{
    public string? TimeZoneId { get; set; }        // User timezone
    public List<string> Tags { get; set; } = new(); // User categorization tags
    
    public ApplicationUser() : base() { }
    
    public ApplicationUser(string userName, string email, string tzid) 
        : base(userName, email)
    {
        TimeZoneId = tzid;
    }
}
```

#### ApplicationRole
Role entity for role-based access control:

```csharp
[CollectionName("ApplicationRoles")]
public class ApplicationRole : MongoIdentityRole<Guid>, IDataModel
{
    public ApplicationRole() : base() { }
    
    public ApplicationRole(string roleName) : base(roleName) { }
}
```

#### JwtComponent
Manages JWT token generation, validation, and refresh:

```csharp
public class JwtComponent
{
    private readonly JwtConfig _jwtConfig;
    private readonly IRepository<RefreshToken> _jwtRepository;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly CustomUserManager _userManager;
    private readonly CustomRoleManager _roleManager;
    
    // Generate JWT token with claims
    public async Task<AuthResponse> GenerateJwtToken(ApplicationUser user)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
        
        var userClaims = await AssembleClaimsAsync(user);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(userClaims),
            Expires = DateTime.UtcNow.AddSeconds(600.0), // 10 minutes
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = jwtTokenHandler.WriteToken(token);
        
        // Create refresh token
        var refreshToken = new RefreshToken
        {
            JwtId = token.Id,
            IsUsed = false,
            IsRevoked = false,
            UserId = user.Id,
            Added = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            Token = GoodSeedRandom.RandomString(35) + Guid.NewGuid()
        };
        
        await _jwtRepository.CreateAsync(refreshToken);
        
        return new AuthResponse
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Roles = user.Roles.Select(r => r.ToString()).ToList()
        };
    }
    
    // Assemble user claims including roles and permissions
    private async Task<List<Claim>> AssembleClaimsAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim("Id", user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        // Add existing user claims
        var userClaims = await _userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);
        
        // Add role claims
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var userRole in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.ToString()));
            
            var role = await _roleManager.FindByNameAsync(userRole.ToString());
            if (role != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(role);
                foreach (var roleClaim in roleClaims)
                {
                    claims.Add(new Claim(roleClaim.Type, roleClaim.Value));
                }
            }
        }
        
        return claims;
    }
    
    // Refresh expired tokens
    public async Task<AuthResponse> RefreshToken(TokenRequest request)
    {
        // Validate token structure and expiration
        // Verify refresh token exists and is valid
        // Generate new token pair
        return await VerifyAndGenerateToken(request);
    }
}
```

#### LoginLogic
Handles user authentication operations:

```csharp
public class LoginLogic
{
    private readonly JwtComponent _jwt;
    private readonly CustomUserManager _userManager;
    private readonly CustomSignInManager _signInManager;
    
    public async Task<AuthResponse> Login(string email, string password)
    {
        email.Requires().IsNotNullOrEmpty();
        password.Requires().IsNotNullOrEmpty();
        
        var user = await _userManager.FindByEmailAsync(email);
        user.Guarantees("Login failed: Email or password incorrect.").IsNotNull();
        
        var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
        result.Succeeded.Guarantees("Login failed: Email or password incorrect.").IsTrue();
        
        return await _jwt.GenerateJwtToken(user);
    }
    
    public async Task Logout(ApplicationUser user, string returnUrl)
    {
        await _signInManager.SignOutAsync(user, returnUrl);
    }
    
    public async Task<AuthResponse> Refresh(TokenRequest tokenRequest)
    {
        return await _jwt.RefreshToken(tokenRequest);
    }
}
```

#### InvitationLogic
Manages user invitation workflow:

```csharp
public class InvitationLogic
{
    private readonly IRepository<InvitationDataModel> _invitationRepository;
    private readonly CustomUserManager _userManager;
    private readonly IRegulatedNotificationLogic _notificationLogic;
    
    public async Task<InvitationDataModel> CreateInvitation(
        string email,
        List<string> roles,
        Guid invitedBy,
        DateTime? expirationDate = null)
    {
        var invitation = new InvitationDataModel
        {
            Email = email.ToLowerInvariant(),
            Roles = roles,
            InvitedBy = invitedBy,
            InvitationToken = Guid.NewGuid().ToString(),
            ExpirationDate = expirationDate ?? DateTime.UtcNow.AddDays(7),
            Status = InvitationStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };
        
        await _invitationRepository.CreateAsync(invitation);
        
        // Send invitation email
        await _notificationLogic.RequestNotification(new RequestNotification
        {
            ToEmail = email,
            Subject = "You've been invited to join our platform",
            EmailText = $"Click here to accept your invitation: /accept-invitation/{invitation.InvitationToken}",
            Tags = new[] { "invitation", "onboarding" }
        });
        
        return invitation;
    }
    
    public async Task<ApplicationUser> AcceptInvitation(
        string token,
        string password,
        string firstName,
        string lastName)
    {
        var invitation = await GetValidInvitation(token);
        
        // Create user account
        var user = new ApplicationUser(invitation.Email, invitation.Email, "UTC")
        {
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true
        };
        
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new ValidationException("Failed to create user account");
        }
        
        // Assign roles
        foreach (var role in invitation.Roles)
        {
            await _userManager.AddToRoleAsync(user, role);
        }
        
        // Mark invitation as accepted
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedDate = DateTime.UtcNow;
        invitation.AcceptedByUserId = user.Id;
        await _invitationRepository.UpdateAsync(invitation);
        
        return user;
    }
}
```

#### CustomUserManager
Extended user manager with additional functionality:

```csharp
public class CustomUserManager : UserManager<ApplicationUser>
{
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly IRepository<UserTagsDataModel> _userTagsRepository;
    
    public async Task<ApplicationUser> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var (users, _) = await _userRepository.GetAsync(u => u.Email == email.ToLowerInvariant(), ct);
        return users.FirstOrDefault();
    }
    
    public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
    {
        // Hash password
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.CreatedDate = DateTime.UtcNow;
        user.UpdatedDate = DateTime.UtcNow;
        
        try
        {
            await _userRepository.CreateAsync(user);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CreateUserFailed",
                Description = ex.Message
            });
        }
    }
    
    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success;
    }
    
    public async Task AddTagsAsync(Guid userId, params string[] tags)
    {
        var userTags = new UserTagsDataModel
        {
            UserId = userId,
            Tags = tags.ToList(),
            AddedDate = DateTime.UtcNow
        };
        
        await _userTagsRepository.CreateAsync(userTags);
    }
}
```

#### AuthResponse and Supporting Models
Authentication response and supporting data models:

```csharp
public class AuthResponse
{
    public string Token { get; set; }              // JWT access token
    public string RefreshToken { get; set; }       // Refresh token
    public List<string> Roles { get; set; }        // User roles
    public bool Success { get; set; } = true;      // Operation success
    public List<string> Errors { get; set; } = new(); // Error messages
}

public class TokenRequest
{
    public string Token { get; set; }              // Expired JWT token
    public string RefreshToken { get; set; }       // Refresh token
}

public class RefreshToken : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string JwtId { get; set; }              // JWT token ID
    public string Token { get; set; }              // Refresh token value
    public bool IsUsed { get; set; }               // Token usage status
    public bool IsRevoked { get; set; }            // Token revocation status
    public Guid UserId { get; set; }               // Associated user
    public DateTime Added { get; set; }            // Creation date
    public DateTime ExpiryDate { get; set; }       // Expiration date
}

public class InvitationDataModel : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Email { get; set; }              // Invited email
    public List<string> Roles { get; set; }        // Assigned roles
    public Guid InvitedBy { get; set; }            // Inviting user
    public string InvitationToken { get; set; }    // Unique invitation token
    public InvitationStatus Status { get; set; }   // Invitation status
    public DateTime CreatedDate { get; set; }      // Creation timestamp
    public DateTime ExpirationDate { get; set; }   // Expiration timestamp
    public DateTime? AcceptedDate { get; set; }    // Acceptance timestamp
    public Guid? AcceptedByUserId { get; set; }    // Accepting user
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}
```

### Service Registration and Configuration

#### Dependency Injection Setup
```csharp
public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddBFormAuthorization(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // JWT Configuration
        var jwtConfig = new JwtConfig();
        configuration.Bind("JwtConfig", jwtConfig);
        services.Configure<JwtConfig>(configuration.GetSection("JwtConfig"));
        
        // Token validation parameters
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtConfig.Secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RequireExpirationTime = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        services.AddSingleton(tokenValidationParameters);
        
        // Identity services
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<ApplicationRole>()
        .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>(
            configuration.GetConnectionString("MongoDB"));
        
        // Custom managers
        services.AddScoped<CustomUserManager>();
        services.AddScoped<CustomRoleManager>();
        services.AddScoped<CustomSignInManager>();
        
        // Business logic services
        services.AddScoped<JwtComponent>();
        services.AddScoped<LoginLogic>();
        services.AddScoped<RegistrationLogic>();
        services.AddScoped<InvitationLogic>();
        services.AddScoped<UserManagementLogic>();
        
        // JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(jwt =>
        {
            jwt.SaveToken = true;
            jwt.TokenValidationParameters = tokenValidationParameters;
        });
        
        return services;
    }
}
```

### Security Features

#### Password Security
- **BCrypt Hashing**: Strong password hashing using BCrypt
- **Password Requirements**: Configurable complexity requirements
- **Salt Generation**: Automatic salt generation for each password
- **Verification**: Secure password verification

#### Token Security
- **Short-lived Access Tokens**: 10-minute JWT expiration
- **Long-lived Refresh Tokens**: 6-month refresh token lifetime
- **Token Rotation**: New tokens generated on refresh
- **Revocation Support**: Ability to revoke refresh tokens
- **Signature Validation**: HMAC-SHA256 signature verification

#### Role-Based Authorization
- **Hierarchical Roles**: Support for role inheritance
- **Claims-Based Authorization**: Fine-grained permissions via claims
- **Dynamic Role Assignment**: Runtime role management
- **Role Scoping**: Context-specific role application

### Usage Examples

#### User Registration and Login
```csharp
public class AuthController : ControllerBase
{
    private readonly LoginLogic _loginLogic;
    private readonly RegistrationLogic _registrationLogic;
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var user = await _registrationLogic.RegisterUser(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.TimeZoneId
            );
            
            var authResponse = await _loginLogic.Login(request.Email, request.Password);
            return Ok(authResponse);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var authResponse = await _loginLogic.Login(request.Email, request.Password);
            return Ok(authResponse);
        }
        catch (ValidationException ex)
        {
            return Unauthorized(new { errors = new[] { ex.Message } });
        }
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(TokenRequest request)
    {
        try
        {
            var authResponse = await _loginLogic.Refresh(request);
            return Ok(authResponse);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }
}
```

#### User Invitation Workflow
```csharp
public class InvitationController : ControllerBase
{
    private readonly InvitationLogic _invitationLogic;
    
    [HttpPost("invite")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> InviteUser(InviteRequest request)
    {
        var currentUserId = User.GetUserId();
        
        var invitation = await _invitationLogic.CreateInvitation(
            email: request.Email,
            roles: request.Roles,
            invitedBy: currentUserId,
            expirationDate: DateTime.UtcNow.AddDays(7)
        );
        
        return Ok(new { invitationId = invitation.Id });
    }
    
    [HttpPost("accept-invitation/{token}")]
    public async Task<IActionResult> AcceptInvitation(
        string token, 
        AcceptInvitationRequest request)
    {
        try
        {
            var user = await _invitationLogic.AcceptInvitation(
                token,
                request.Password,
                request.FirstName,
                request.LastName
            );
            
            var authResponse = await _loginLogic.Login(user.Email, request.Password);
            return Ok(authResponse);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }
}
```

#### Role Management
```csharp
public class RoleManagementExample
{
    private readonly CustomUserManager _userManager;
    private readonly CustomRoleManager _roleManager;
    
    public async Task AssignUserRoles(Guid userId, params string[] roleNames)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        
        foreach (var roleName in roleNames)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }
    }
    
    public async Task CreateRoleWithClaims(string roleName, params string[] permissions)
    {
        var role = new ApplicationRole(roleName);
        await _roleManager.CreateAsync(role);
        
        foreach (var permission in permissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
        }
    }
}
```

#### Authorization Attributes
```csharp
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        // Only accessible to Admin role
    }
}

[Authorize]
[RequireClaim("permission", "CanViewReports")]
public class ReportsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReports()
    {
        // Requires specific permission claim
    }
}
```

### Best Practices

1. **Always use HTTPS** - Protect tokens in transit
2. **Implement token rotation** - Regularly refresh access tokens
3. **Use short token lifetimes** - Minimize exposure window
4. **Validate all inputs** - Use the validation framework
5. **Log security events** - Monitor authentication attempts
6. **Implement rate limiting** - Prevent brute force attacks
7. **Use strong passwords** - Enforce password complexity
8. **Secure token storage** - Store refresh tokens securely
9. **Implement proper logout** - Revoke tokens on logout
10. **Monitor for anomalies** - Track unusual access patterns

---

## Scheduler System

The Scheduler System provides comprehensive job scheduling capabilities with support for one-time execution, recurring schedules, and cron expressions. It integrates with the AppEvents system to trigger scheduled operations and supports multiple schedule types for flexible timing requirements.

### Core Components

#### ScheduledEventTemplate
Defines the structure and timing for scheduled events:

```csharp
public class ScheduledEventTemplate : IContentType
{
    public string Name { get; set; }                    // Unique identifier
    public List<string> Tags { get; set; } = new();     // Categorization tags
    public Dictionary<string, string>? SatelliteData { get; set; } = new();
    
    // Event configuration
    public string EventTopic { get; set; }              // Last part of event topic
    
    // Schedule specification - supports multiple formats:
    // "ts:{timespan}" - One-time execution after timespan
    // "rf:{timespan}" - Repeat forever with timespan interval
    // "rc:{timespan}|{count}" - Repeat specified count with timespan
    // "cr:{cron}" - Cron expression scheduling
    public string Schedule { get; set; }
    
    public ScheduledEventIdentifier CreateIdentifier(Guid schId)
    {
        var group = $"{Name}.{EventTopic}";
        var id = GuidEncoder.Encode(schId);
        return new ScheduledEventIdentifier(nameof(ScheduledEvent), id, group, id);
    }
    
    public string CreateEventTopic(string wsTemplateName, string wiTemplateName)
    {
        return $"{wsTemplateName}.{wiTemplateName}.scheduled.event.{EventTopic.ToLowerInvariant()}";
    }
}
```

#### CronExpression
Handles cron expression parsing and evaluation:

```csharp
public class CronExpression
{
    private CrontabSchedule _schedule;
    
    public CronExpression(string cronExpression)
    {
        try
        {
            _schedule = CrontabSchedule.Parse(cronExpression);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid cron expression.", ex);
        }
    }
    
    public TimeStatus CheckStatus(
        DateTime currentUtcDateTime,
        TimeZoneInfo timeZone,
        DateTime? previousInvocationTime)
    {
        DateTime currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(currentUtcDateTime, timeZone);
        DateTime? previousLocalInvocationTime = previousInvocationTime.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(previousInvocationTime.Value, timeZone)
            : (DateTime?)null;
        
        DateTime nextOccurrence = _schedule.GetNextOccurrence(
            previousLocalInvocationTime ?? DateTime.MinValue);
        
        if (nextOccurrence <= currentLocalDateTime)
        {
            if (previousLocalInvocationTime.HasValue && 
                previousLocalInvocationTime.Value == nextOccurrence)
            {
                return TimeStatus.NotDue;
            }
            return TimeStatus.Overdue;
        }
        
        return TimeStatus.NotDue;
    }
    
    public enum TimeStatus
    {
        Due = 0,
        NotDue = 1,
        Overdue = 2
    }
}
```

#### SchedulerBackgroundWorker
Background service that processes scheduled jobs:

```csharp
public class SchedulerBackgroundWorker : BackgroundService
{
    private readonly IRepository<ScheduledJobEntity> _repo;
    private readonly AppEventSink _eventSink;
    
    public SchedulerBackgroundWorker(
        IRepository<ScheduledJobEntity> repo,
        AppEventSink eventSink)
    {
        _repo = repo;
        _eventSink = eventSink;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var task = new Task(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ScheduleJobs();
                Thread.Yield();
            }
        });
        
        task.Start();
        return Task.CompletedTask;
    }
    
    private async Task ScheduleJobs()
    {
        var (schedules, ctx) = await _repo.GetOrderedAsync(
            sch => sch.NextDeadline,
            descending: true,
            start: 0,
            count: 1000);
        
        foreach (var schedule in schedules)
        {
            if (schedule.NextDeadline < DateTime.UtcNow)
            {
                bool scheduleDone = await ProcessSchedule(schedule, ctx);
                
                if (scheduleDone)
                {
                    await _repo.DeleteAsync((schedule, ctx));
                }
                else
                {
                    await UpdateNextDeadline(schedule, ctx);
                }
                
                // Send event to trigger scheduled operation
                await TriggerScheduledEvent(schedule);
            }
        }
    }
    
    private async Task<bool> ProcessSchedule(ScheduledJobEntity schedule, RepositoryContext ctx)
    {
        switch (schedule.Payload.Type)
        {
            case ScheduleType.Once:
                return true; // One-time execution, mark as done
                
            case ScheduleType.RecurringX:
                schedule.Payload.InvocationCount++;
                return schedule.Payload.InvocationCount >= schedule.Payload.RecurrenceCount;
                
            case ScheduleType.RecurringInfinite:
                schedule.Payload.InvocationCount++;
                return false; // Never stops
                
            case ScheduleType.Cron:
                return false; // Cron schedules continue indefinitely
                
            default:
                return false;
        }
    }
    
    private async Task UpdateNextDeadline(ScheduledJobEntity schedule, RepositoryContext ctx)
    {
        if (!string.IsNullOrWhiteSpace(schedule.Payload.CronExpression))
        {
            var cron = CrontabSchedule.Parse(schedule.Payload.CronExpression);
            schedule.NextDeadline = cron.GetNextOccurrence(DateTime.UtcNow);
        }
        else
        {
            schedule.NextDeadline = DateTime.UtcNow + schedule.RecurrenceSchedule;
        }
        
        await _repo.UpdateAsync((schedule, ctx));
    }
    
    private async Task TriggerScheduledEvent(ScheduledJobEntity schedule)
    {
        var handle = _repo.OpenTransaction();
        _eventSink.BeginBatch(handle);
        
        await _eventSink.Enqueue(
            new AppEventOrigin(nameof(SchedulerBackgroundWorker), nameof(ScheduleJobs), null),
            schedule.Payload.Topic,
            nameof(ScheduleJobs),
            schedule,
            null,
            null);
        
        await _eventSink.CommitBatch();
    }
}
```

#### ScheduledJobEntity
Represents a scheduled job in the system:

```csharp
public class ScheduledJobEntity : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // Scheduling information
    public DateTime NextDeadline { get; set; }           // When job should run next
    public TimeSpan RecurrenceSchedule { get; set; }     // Interval for recurring jobs
    
    // Job configuration
    public Schedule Payload { get; set; }                // Job details and parameters
    
    // Execution tracking
    public DateTime? LastExecuted { get; set; }          // Last execution time
    public int ExecutionCount { get; set; }              // Total executions
    public ScheduleStatus Status { get; set; }           // Current status
}

public class Schedule
{
    public ScheduleType Type { get; set; }               // Schedule type
    public string Topic { get; set; }                    // Event topic to trigger
    public string? CronExpression { get; set; }          // Cron expression (if applicable)
    public int InvocationCount { get; set; }             // Current invocation count
    public int RecurrenceCount { get; set; }             // Max recurrences (for RecurringX)
    public JObject? Parameters { get; set; }             // Job parameters
    public List<string> Tags { get; set; } = new();      // Job tags
}

public enum ScheduleType
{
    Once = 0,              // Execute once
    RecurringX = 1,        // Execute X times
    RecurringInfinite = 2, // Execute forever
    Cron = 3               // Use cron expression
}

public enum ScheduleStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
```

#### SinkEventJob
Specific job type for sinking events:

```csharp
public class SinkEventJob
{
    public string EventTopic { get; set; }               // Topic to sink to
    public JObject EventPayload { get; set; }            // Event data
    public AppEventOrigin Origin { get; set; }           // Event origin
    public List<string> Tags { get; set; } = new();      // Event tags
    public int Priority { get; set; } = 0;               // Event priority
    
    public async Task Execute(AppEventSink eventSink)
    {
        await eventSink.SinkAsync(
            topic: EventTopic,
            origin: Origin,
            payload: BsonDocument.Parse(EventPayload.ToString()),
            tags: Tags.ToArray(),
            priority: Priority);
    }
}
```

#### SchedulerLogic
Business logic for schedule management:

```csharp
public class SchedulerLogic
{
    private readonly IRepository<ScheduledJobEntity> _scheduleRepository;
    private readonly AppEventSink _eventSink;
    
    public async Task<ScheduledJobEntity> CreateSchedule(
        string topic,
        string scheduleExpression,
        JObject? parameters = null,
        string[]? tags = null)
    {
        var schedule = ParseScheduleExpression(scheduleExpression);
        
        var scheduledJob = new ScheduledJobEntity
        {
            Payload = new Schedule
            {
                Type = schedule.Type,
                Topic = topic,
                CronExpression = schedule.CronExpression,
                RecurrenceCount = schedule.RecurrenceCount,
                Parameters = parameters,
                Tags = tags?.ToList() ?? new List<string>()
            },
            NextDeadline = CalculateNextDeadline(schedule),
            RecurrenceSchedule = schedule.Interval,
            Status = ScheduleStatus.Active
        };
        
        return await _scheduleRepository.CreateAsync(scheduledJob);
    }
    
    public async Task<ScheduledJobEntity> CreateOneTimeSchedule(
        string topic,
        DateTime executeAt,
        JObject? parameters = null)
    {
        var scheduledJob = new ScheduledJobEntity
        {
            Payload = new Schedule
            {
                Type = ScheduleType.Once,
                Topic = topic,
                Parameters = parameters
            },
            NextDeadline = executeAt,
            Status = ScheduleStatus.Active
        };
        
        return await _scheduleRepository.CreateAsync(scheduledJob);
    }
    
    public async Task<ScheduledJobEntity> CreateRecurringSchedule(
        string topic,
        TimeSpan interval,
        int? maxExecutions = null,
        JObject? parameters = null)
    {
        var scheduledJob = new ScheduledJobEntity
        {
            Payload = new Schedule
            {
                Type = maxExecutions.HasValue ? ScheduleType.RecurringX : ScheduleType.RecurringInfinite,
                Topic = topic,
                RecurrenceCount = maxExecutions ?? 0,
                Parameters = parameters
            },
            NextDeadline = DateTime.UtcNow.Add(interval),
            RecurrenceSchedule = interval,
            Status = ScheduleStatus.Active
        };
        
        return await _scheduleRepository.CreateAsync(scheduledJob);
    }
    
    public async Task<ScheduledJobEntity> CreateCronSchedule(
        string topic,
        string cronExpression,
        JObject? parameters = null)
    {
        var cron = CrontabSchedule.Parse(cronExpression);
        
        var scheduledJob = new ScheduledJobEntity
        {
            Payload = new Schedule
            {
                Type = ScheduleType.Cron,
                Topic = topic,
                CronExpression = cronExpression,
                Parameters = parameters
            },
            NextDeadline = cron.GetNextOccurrence(DateTime.UtcNow),
            Status = ScheduleStatus.Active
        };
        
        return await _scheduleRepository.CreateAsync(scheduledJob);
    }
    
    private (ScheduleType Type, string? CronExpression, TimeSpan Interval, int RecurrenceCount) 
        ParseScheduleExpression(string expression)
    {
        if (expression.StartsWith("ts:"))
        {
            var timespan = TimeSpan.Parse(expression.Substring(3));
            return (ScheduleType.Once, null, timespan, 0);
        }
        else if (expression.StartsWith("rf:"))
        {
            var timespan = TimeSpan.Parse(expression.Substring(3));
            return (ScheduleType.RecurringInfinite, null, timespan, 0);
        }
        else if (expression.StartsWith("rc:"))
        {
            var parts = expression.Substring(3).Split('|');
            var timespan = TimeSpan.Parse(parts[0]);
            var count = int.Parse(parts[1]);
            return (ScheduleType.RecurringX, null, timespan, count);
        }
        else if (expression.StartsWith("cr:"))
        {
            var cronExpression = expression.Substring(3);
            return (ScheduleType.Cron, cronExpression, TimeSpan.Zero, 0);
        }
        
        throw new ArgumentException($"Invalid schedule expression: {expression}");
    }
}
```

### Schedule Expression Formats

The system supports four schedule expression formats:

#### 1. One-time Execution (`ts:`)
```csharp
// Execute once after 30 minutes
"ts:00:30:00"

// Execute once after 2 hours
"ts:02:00:00"

// Execute once after 1 day
"ts:1.00:00:00"
```

#### 2. Infinite Recurrence (`rf:`)
```csharp
// Repeat every 15 minutes forever
"rf:00:15:00"

// Repeat every hour forever
"rf:01:00:00"

// Repeat every day forever
"rf:1.00:00:00"
```

#### 3. Limited Recurrence (`rc:`)
```csharp
// Repeat every 30 minutes, 10 times total
"rc:00:30:00|10"

// Repeat every 2 hours, 5 times total
"rc:02:00:00|5"

// Repeat daily, 30 times total
"rc:1.00:00:00|30"
```

#### 4. Cron Expression (`cr:`)
```csharp
// Every day at 2:30 AM
"cr:30 2 * * *"

// Every Monday at 9:00 AM
"cr:0 9 * * 1"

// Every 15 minutes during business hours (9-5, Mon-Fri)
"cr:*/15 9-17 * * 1-5"

// First day of every month at midnight
"cr:0 0 1 * *"
```

### Usage Examples

#### Creating Different Schedule Types
```csharp
public class SchedulingExamples
{
    private readonly SchedulerLogic _schedulerLogic;
    
    // One-time reminder
    public async Task ScheduleReminder()
    {
        await _schedulerLogic.CreateOneTimeSchedule(
            topic: "User.Reminder",
            executeAt: DateTime.UtcNow.AddHours(24),
            parameters: JObject.FromObject(new
            {
                userId = Guid.NewGuid(),
                message = "Don't forget your appointment tomorrow!",
                type = "appointment"
            }));
    }
    
    // Daily backup
    public async Task ScheduleDailyBackup()
    {
        await _schedulerLogic.CreateCronSchedule(
            topic: "System.Backup",
            cronExpression: "0 2 * * *", // Every day at 2 AM
            parameters: JObject.FromObject(new
            {
                backupType = "full",
                retentionDays = 30
            }));
    }
    
    // Recurring health check
    public async Task ScheduleHealthCheck()
    {
        await _schedulerLogic.CreateRecurringSchedule(
            topic: "System.HealthCheck",
            interval: TimeSpan.FromMinutes(5),
            maxExecutions: null, // Infinite
            parameters: JObject.FromObject(new
            {
                checkServices = new[] { "database", "cache", "messagebus" },
                alertOnFailure = true
            }));
    }
    
    // Weekly report generation
    public async Task ScheduleWeeklyReport()
    {
        await _schedulerLogic.CreateCronSchedule(
            topic: "Reports.Weekly",
            cronExpression: "0 8 * * 1", // Every Monday at 8 AM
            parameters: JObject.FromObject(new
            {
                reportType = "sales",
                recipients = new[] { "manager@company.com", "sales@company.com" },
                includeCharts = true
            }));
    }
}
```

#### Event Handling for Scheduled Jobs
```csharp
public class ScheduledEventHandler : IAppEventConsumer
{
    private readonly ILogger<ScheduledEventHandler> _logger;
    private readonly EmailService _emailService;
    private readonly BackupService _backupService;
    
    public void ConsumeEvents(AppEvent appEvent)
    {
        switch (appEvent.Topic)
        {
            case "User.Reminder":
                HandleUserReminder(appEvent);
                break;
                
            case "System.Backup":
                HandleSystemBackup(appEvent);
                break;
                
            case "System.HealthCheck":
                HandleHealthCheck(appEvent);
                break;
                
            case "Reports.Weekly":
                HandleWeeklyReport(appEvent);
                break;
        }
    }
    
    private async Task HandleUserReminder(AppEvent appEvent)
    {
        var payload = BsonSerializer.Deserialize<ReminderPayload>(appEvent.EntityPayload);
        
        await _emailService.SendReminderAsync(
            payload.UserId,
            payload.Message,
            payload.Type);
    }
    
    private async Task HandleSystemBackup(AppEvent appEvent)
    {
        var payload = BsonSerializer.Deserialize<BackupPayload>(appEvent.EntityPayload);
        
        await _backupService.PerformBackupAsync(
            payload.BackupType,
            payload.RetentionDays);
    }
    
    private async Task HandleHealthCheck(AppEvent appEvent)
    {
        var payload = BsonSerializer.Deserialize<HealthCheckPayload>(appEvent.EntityPayload);
        
        var results = await _healthCheckService.CheckServicesAsync(payload.CheckServices);
        
        if (payload.AlertOnFailure && results.Any(r => !r.IsHealthy))
        {
            await _alertService.SendHealthAlertAsync(results);
        }
    }
}
```

#### Integration with Work Items
```csharp
public class WorkItemSchedulingExample
{
    private readonly SchedulerLogic _schedulerLogic;
    private readonly WorkItemLogic _workItemLogic;
    
    // Schedule follow-up tasks
    public async Task ScheduleFollowUp(Guid workItemId, DateTime followUpDate)
    {
        await _schedulerLogic.CreateOneTimeSchedule(
            topic: "WorkItem.FollowUp",
            executeAt: followUpDate,
            parameters: JObject.FromObject(new
            {
                workItemId = workItemId,
                action = "reminder",
                message = "Follow up required on this work item"
            }));
    }
    
    // Schedule automatic status updates
    public async Task ScheduleStatusEscalation(Guid workItemId)
    {
        // Escalate to manager if not resolved in 24 hours
        await _schedulerLogic.CreateOneTimeSchedule(
            topic: "WorkItem.Escalate",
            executeAt: DateTime.UtcNow.AddHours(24),
            parameters: JObject.FromObject(new
            {
                workItemId = workItemId,
                escalationLevel = "manager",
                reason = "Unresolved for 24 hours"
            }));
        
        // Auto-close if not resolved in 7 days
        await _schedulerLogic.CreateOneTimeSchedule(
            topic: "WorkItem.AutoClose",
            executeAt: DateTime.UtcNow.AddDays(7),
            parameters: JObject.FromObject(new
            {
                workItemId = workItemId,
                reason = "Auto-closed due to inactivity"
            }));
    }
}
```

### Best Practices

1. **Use appropriate schedule types** - Choose the right scheduling pattern for your needs
2. **Handle timezone considerations** - Be aware of UTC vs local time conversions
3. **Implement idempotency** - Ensure scheduled operations can be safely retried
4. **Monitor schedule health** - Track execution success and failure rates
5. **Use meaningful event topics** - Structure topics for easy event routing
6. **Include sufficient parameters** - Pass all necessary data in schedule parameters
7. **Handle long-running operations** - Use appropriate timeout and cancellation patterns
8. **Validate cron expressions** - Test cron expressions before creating schedules
9. **Plan for schedule cleanup** - Remove completed or obsolete schedules
10. **Log schedule execution** - Maintain audit trail of scheduled operations

---

## Notification System

The Notification System provides comprehensive multi-channel notification capabilities with advanced features including message regulation, duplicate suppression, digest consolidation, and audit trails. It supports email, SMS, voice calls, and in-app toast notifications with sophisticated timing and routing controls.

### Core Components

#### NotificationMessage
The primary message model supporting multiple notification channels:

```csharp
public class NotificationMessage
{
    // Message content
    public string Subject { get; set; } = "unknown";        // Message subject/title
    public string CreatorId { get; set; } = "unknown";      // Message creator ID
    
    // Channel-specific content
    public string? SMSText { get; set; }                    // SMS message text
    public string? EmailText { get; set; }                  // Email plain text
    public string? EmailHtmlText { get; set; }              // Email HTML content
    public string? ToastText { get; set; }                  // In-app toast text
    public string? CallText { get; set; }                   // Voice call text
    
    // Targeting
    public List<Guid> NotificationGroups { get; set; } = new(); // Multiple groups
    public Guid? NotificationGroup { get; set; }            // Single group
    public Guid? NotificationContact { get; set; }          // Direct contact
    
    // Message properties
    public LogLevel Severity { get; set; }                  // Message severity level
    
    // Regulation settings
    public bool WantDigest { get; set; }                    // Enable digest consolidation
    public bool WantSuppression { get; set; }               // Enable duplicate suppression
    public int SuppressionMinutes { get; set; }             // Suppression window
    public int DigestMinutes { get; set; }                  // Digest consolidation window
    public int DigestHead { get; set; }                     // Head items in digest
    public int DigestTail { get; set; }                     // Tail items in digest
}
```

#### NotificationService
Background service that processes notification messages:

```csharp
public class NotificationService : IHostedService, IDisposable
{
    public const string ExchangeName = "user_notification";
    public const string RouteName = "q_user_notification";
    
    private IMessageListener? _qListener;
    private readonly IMessageBusSpecifier _busSpec;
    private readonly IRegulatedNotificationLogic _logic;
    private readonly ILogger<NotificationService> _logger;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_qListener != null)
        {
            // Configure message bus topology
            _busSpec
                .DeclareExchange(ExchangeName, ExchangeTypes.Direct)
                .SpecifyExchange(ExchangeName)
                .DeclareQueue(RouteName, RouteName);
            
            _logger.LogInformation($"User Notification Service listening on {ExchangeName}.{RouteName}");
            
            // Initialize listener
            _qListener.Initialize(ExchangeName, RouteName);
            _qListener.Listen(new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(
                typeof(NotificationMessage), ProcessMessage));
        }
        
        _logic.Initialize();
    }
    
    private void ProcessMessage(object msg, CancellationToken ct, IMessageAcknowledge ack)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            
            if (msg is NotificationMessage notification)
                AsyncHelper.RunSync(() => _logic.Notify(notification));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{trace}", ex.TraceInformation());
        }
    }
}
```
