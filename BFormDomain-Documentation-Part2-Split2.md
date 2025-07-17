# BFormDomain Part2 Documentation (Split 2)

#### Implementing Attachment System
```csharp
public class AttachmentService : IEntityAttachmentManager
{
    public async Task<AttachmentReference> CreateOrUpdateAttachmentAsync(
        ITransactionContext tc,
        ManagedFileAction action,
        string? existingUrl = null)
    {
        AttachmentReference result;
        
        switch (action.Type)
        {
            case ManagedFileActionType.Upload:
                // Upload new file
                var file = await _fileLogic.UploadFile(
                    tc,
                    action.UserId,
                    action.FileStream,
                    action.FileName,
                    action.ContentType,
                    new FileUploadOptions
                    {
                        WorkSet = action.WorkSet,
                        WorkItem = action.WorkItem,
                        Tags = new[] { "attachment", action.EntityType },
                        IsTemporary = false,
                        AccessLevel = FileAccessLevel.Internal
                    });
                    
                result = new AttachmentReference
                {
                    Url = GenerateAttachmentUrl(file.Id),
                    Name = file.OriginalFileName,
                    ContentType = file.ContentType,
                    SizeInBytes = (int)file.SizeInBytes,
                    UploadedAt = file.CreatedDate,
                    UploadedBy = file.Creator.Value
                };
                break;
                
            case ManagedFileActionType.Link:
                // Link existing file
                var linkedFile = await _fileLogic.LoadFile(action.FileId.Value);
                
                result = new AttachmentReference
                {
                    Url = GenerateAttachmentUrl(linkedFile.Id),
                    Name = linkedFile.OriginalFileName,
                    ContentType = linkedFile.ContentType,
                    SizeInBytes = (int)linkedFile.SizeInBytes,
                    UploadedAt = linkedFile.CreatedDate,
                    UploadedBy = linkedFile.Creator.Value
                };
                break;
                
            case ManagedFileActionType.Remove:
                // Remove attachment
                if (!string.IsNullOrEmpty(existingUrl))
                {
                    var fileId = ExtractFileIdFromUrl(existingUrl);
                    await RemoveAttachmentReference(tc, fileId, action.EntityId);
                }
                return null;
                
            default:
                throw new NotSupportedException($"Action {action.Type} not supported");
        }
        
        // Create attachment reference
        await CreateAttachmentReference(tc, result, action.EntityType, action.EntityId);
        
        return result;
    }
    
    // Handle entity attachments
    public async Task<List<AttachmentReference>> GetEntityAttachments(
        string entityType,
        Guid entityId)
    {
        var (references, _) = await _attachmentRefRepo.GetAsync(
            r => r.EntityType == entityType && r.EntityId == entityId);
            
        var attachments = new List<AttachmentReference>();
        
        foreach (var reference in references)
        {
            try
            {
                var file = await _fileLogic.LoadFile(reference.FileId);
                
                attachments.Add(new AttachmentReference
                {
                    Url = GenerateAttachmentUrl(file.Id),
                    Name = file.OriginalFileName,
                    ContentType = file.ContentType,
                    SizeInBytes = (int)file.SizeInBytes,
                    UploadedAt = file.CreatedDate,
                    UploadedBy = file.Creator.Value
                });
            }
            catch
            {
                // File might be deleted
            }
        }
        
        return attachments;
    }
}
```

### Best Practices

1. **Always validate files** before accepting uploads (size, type, content)
2. **Use appropriate storage locations** based on file type and access patterns
3. **Implement virus scanning** for user-uploaded content
4. **Set expiration dates** for temporary files
5. **Track file access** for auditing and compliance
6. **Use encryption** for sensitive files
7. **Implement versioning** for important documents
8. **Generate thumbnails** for images to improve performance
9. **Clean up orphaned files** regularly
10. **Monitor storage usage** and implement quotas

---

## KPI System

The KPI (Key Performance Indicator) System provides sophisticated metrics calculation, tracking, and signal detection capabilities. It supports complex computations, time-series analysis, threshold monitoring, and automated alerting based on statistical patterns.

### Core Components

#### KPITemplate
Defines the structure and calculation rules for KPIs:

```csharp
public class KPITemplate : IContentType
{
    public string Name { get; set; }                // Unique identifier
    public string Title { get; set; }               // Display name
    public List<string> Tags { get; set; }          // Categorization
    
    // Scheduling
    public string ScheduleTemplate { get; set; }     // Cron expression
    
    // Subject scoping
    public KPISubjectType? SubjectType { get; set; } // What to measure
    public string? SubjectEntityType { get; set; }  // Entity type filter
    public string? SubjectTemplate { get; set; }    // Template filter
    
    // Time configuration
    public TimeFrame? TimeFrame { get; set; }       // Time window
    public string? TimeField { get; set; }          // Date field to use
    
    // Data sources
    public List<KPISource> Sources { get; set; } = new();
    
    // Computation stages
    public List<KPIComputeStage> ComputeStages { get; set; } = new();
    
    // Signal detection
    public List<KPISignal> Signals { get; set; } = new();
    
    // Display
    public bool IsVisibleToUsers { get; set; }      // User visibility
    public string? IconClass { get; set; }          // Display icon
    public string? ChartType { get; set; }          // Visualization type
}

public enum KPISubjectType
{
    None = 0,        // Global KPI
    User = 1,        // Per-user metrics
    WorkSet = 2,     // Per-workspace metrics
    WorkItem = 3     // Per-work item metrics
}

public class KPISource
{
    public string Name { get; set; }                // Source identifier
    public KPISourceType Type { get; set; }         // Source type
    public string? TableTemplate { get; set; }      // Table source
    public string? EventTopic { get; set; }         // Event source
    public string? EntityType { get; set; }         // Entity source
    public JObject? Filter { get; set; }            // Filter criteria
    public RelativeTableQueryCommand? Query { get; set; } // Query config
}

public enum KPISourceType
{
    Table = 0,       // Table data
    Event = 1,       // Event stream
    Entity = 2,      // Entity data
    KPI = 3          // Other KPIs
}

public class KPIComputeStage
{
    public string Name { get; set; }                // Stage name
    public KPIComputeType Type { get; set; }        // Computation type
    public string? Script { get; set; }             // Computation script
    public string? Field { get; set; }              // Field to compute
    public string? GroupBy { get; set; }            // Grouping field
    public string ResultProperty { get; set; }      // Output property
    public JObject? Parameters { get; set; }        // Additional params
}

public enum KPIComputeType
{
    Script = 0,      // Custom script
    Count = 1,       // Count records
    Sum = 2,         // Sum values
    Average = 3,     // Calculate mean
    Min = 4,         // Find minimum
    Max = 5,         // Find maximum
    Percentile = 6,  // Calculate percentile
    StdDev = 7,      // Standard deviation
    Trend = 8        // Trend analysis
}
```

#### KPIInstance
Represents a calculated KPI value:

```csharp
public class KPIInstance : IAppEntity
{
    // Standard IAppEntity properties...
    
    // Subject
    public Guid? SubjectUser { get; set; }          // User KPI
    public Guid? SubjectWorkSet { get; set; }       // WorkSet KPI
    public Guid? SubjectWorkItem { get; set; }      // WorkItem KPI
    
    // Event tracking
    public string EventTopic { get; set; }          // Source event
    public Guid? EventId { get; set; }              // Event reference
    public Guid EventLine { get; set; }             // Event correlation
    public int EventGeneration { get; set; }        // Event distance
    
    // Computation results
    public JObject ComputedValues { get; set; }     // Calculated values
    public DateTime ComputedAt { get; set; }        // Calculation time
    public TimeSpan ComputationTime { get; set; }   // Processing time
    
    // Data window
    public DateTime? WindowStart { get; set; }      // Time window start
    public DateTime? WindowEnd { get; set; }        // Time window end
    public int RecordCount { get; set; }            // Records processed
}
```

#### KPISample
Time-series data point for KPIs:

```csharp
public class KPISample : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    public string KPITemplate { get; set; }         // KPI definition
    public Guid KPIInstanceId { get; set; }         // Instance reference
    
    // Subject
    public Guid? SubjectUser { get; set; }
    public Guid? SubjectWorkSet { get; set; }
    public Guid? SubjectWorkItem { get; set; }
    
    // Sample data
    public DateTime SampleTime { get; set; }        // Sample timestamp
    public double Value { get; set; }               // Sample value
    public string? ValueProperty { get; set; }      // Value field name
    
    // Statistics
    public double? Mean { get; set; }               // Running mean
    public double? StdDev { get; set; }             // Standard deviation
    public double? Min { get; set; }                // Minimum value
    public double? Max { get; set; }                // Maximum value
    public int SampleCount { get; set; }            // Sample size
    
    // Signal detection
    public double? ZScore { get; set; }             // Statistical z-score
    public bool IsAnomaly { get; set; }             // Anomaly flag
    public string? AnomalyReason { get; set; }      // Anomaly details
}
```

#### KPISignal
Signal detection configuration:

```csharp
public class KPISignal
{
    public string Name { get; set; }                // Signal name
    public KPISignalType Type { get; set; }         // Detection type
    public KPISignalStage Stage { get; set; }       // When to check
    public string? Property { get; set; }           // Property to check
    
    // Threshold detection
    public double? ThresholdValue { get; set; }     // Absolute threshold
    public KPIThresholdDirection? Direction { get; set; } // Above/below
    
    // Statistical detection
    public double? ZScoreThreshold { get; set; }    // Z-score threshold
    public int? LookbackSamples { get; set; }       // History window
    
    // Pattern detection
    public string? Pattern { get; set; }            // Pattern expression
    public int? ConsecutiveCount { get; set; }      // Consecutive matches
    
    // Actions
    public string? NotificationGroup { get; set; }  // Who to notify
    public string? EventTopic { get; set; }         // Event to raise
    public JObject? EventData { get; set; }         // Event payload
}

public enum KPISignalType
{
    Threshold = 0,   // Fixed threshold
    Statistical = 1, // Statistical anomaly
    Trend = 2,       // Trend detection
    Pattern = 3      // Pattern matching
}

public enum KPISignalStage
{
    PreCompute = 0,  // Before calculation
    PostCompute = 1, // After calculation
    Sample = 2       // During sampling
}
```

### KPI Services

#### KPIEvaluator
Core service for KPI calculation:

```csharp
public class KPIEvaluator
{
    // Evaluate all KPIs
    public async Task EvaluateAllKPIs(CancellationToken cancellationToken)
    {
        var templates = await LoadActiveKPITemplates();
        
        foreach (var template in templates)
        {
            try
            {
                await EvaluateKPI(template, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to evaluate KPI {template.Name}");
            }
        }
    }
    
    // Evaluate single KPI
    public async Task<KPIInstance> EvaluateKPI(
        KPITemplate template,
        CancellationToken cancellationToken,
        Guid? specificSubject = null,
        JObject? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Determine subjects
        var subjects = await DetermineSubjects(template, specificSubject);
        
        foreach (var subject in subjects)
        {
            // Create KPI instance
            var instance = new KPIInstance
            {
                Template = template.Name,
                SubjectUser = subject.UserId,
                SubjectWorkSet = subject.WorkSetId,
                SubjectWorkItem = subject.WorkItemId,
                ComputedAt = DateTime.UtcNow
            };
            
            try
            {
                // Load data from sources
                var sourceData = await LoadSourceData(template, subject, parameters);
                
                // Execute computation stages
                var context = new KPIComputeContext
                {
                    Template = template,
                    Subject = subject,
                    SourceData = sourceData,
                    Parameters = parameters ?? new JObject()
                };
                
                foreach (var stage in template.ComputeStages)
                {
                    await ExecuteComputeStage(stage, context);
                }
                
                // Store results
                instance.ComputedValues = context.Results;
                instance.RecordCount = context.RecordCount;
                instance.WindowStart = context.WindowStart;
                instance.WindowEnd = context.WindowEnd;
                instance.ComputationTime = stopwatch.Elapsed;
                
                // Save instance
                using (var tc = await _kpiRepo.OpenTransactionAsync())
                {
                    await _kpiRepo.CreateAsync(tc, instance);
                    
                    // Create samples
                    await CreateSamples(tc, template, instance, context);
                    
                    // Check signals
                    await CheckSignals(tc, template, instance, context);
                    
                    // Generate event
                    await _eventSink.SinkAsync(tc, new AppEvent
                    {
                        Topic = $"KPI.{template.Name}.Computed",
                        OriginEntityType = "KPI",
                        OriginTemplate = template.Name,
                        OriginId = instance.Id,
                        EntityPayload = instance.ToBsonDocument()
                    });
                    
                    await tc.CommitAsync();
                }
            }
            catch (KPIInsufficientDataException)
            {
                // Not enough data to compute
                _logger.LogWarning($"Insufficient data for KPI {template.Name}");
            }
        }
        
        return instance;
    }
    
    // Execute computation stage
    private async Task ExecuteComputeStage(
        KPIComputeStage stage,
        KPIComputeContext context)
    {
        switch (stage.Type)
        {
            case KPIComputeType.Count:
                context.Results[stage.ResultProperty] = 
                    context.SourceData[stage.Name].Count();
                break;
                
            case KPIComputeType.Sum:
                context.Results[stage.ResultProperty] = 
                    context.SourceData[stage.Name]
                        .Sum(d => d.Value<double>(stage.Field));
                break;
                
            case KPIComputeType.Average:
                context.Results[stage.ResultProperty] = 
                    context.SourceData[stage.Name]
                        .Average(d => d.Value<double>(stage.Field));
                break;
                
            case KPIComputeType.Script:
                var result = await _scriptEngine.Execute(
                    stage.Script,
                    new Dictionary<string, object>
                    {
                        ["data"] = context.SourceData,
                        ["params"] = context.Parameters,
                        ["subject"] = context.Subject
                    });
                context.Results[stage.ResultProperty] = JToken.FromObject(result);
                break;
                
            case KPIComputeType.Trend:
                var trendData = CalculateTrend(
                    context.SourceData[stage.Name],
                    stage.Field);
                context.Results[stage.ResultProperty] = JToken.FromObject(trendData);
                break;
                
            // Additional computation types...
        }
    }
}
```

#### KPIMath
Statistical calculations for KPIs:

```csharp
public static class KPIMath
{
    // Calculate z-score
    public static double CalculateZScore(
        double value, 
        double mean, 
        double stdDev)
    {
        if (stdDev == 0) return 0;
        return (value - mean) / stdDev;
    }
    
    // Calculate percentile
    public static double CalculatePercentile(
        IEnumerable<double> values, 
        double percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (!sorted.Any()) return 0;
        
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        
        if (lower == upper) return sorted[lower];
        
        var weight = index - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }
    
    // Detect trend
    public static TrendAnalysis AnalyzeTrend(
        IEnumerable<DataPoint> dataPoints,
        int windowSize = 10)
    {
        var points = dataPoints.OrderBy(p => p.Time).ToList();
        if (points.Count < 2) return new TrendAnalysis { Direction = TrendDirection.Flat };
        
        // Calculate linear regression
        var n = points.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;
        
        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += points[i].Value;
            sumXY += i * points[i].Value;
            sumX2 += i * i;
        }
        
        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;
        
        // Calculate R-squared
        var yMean = sumY / n;
        var ssTotal = points.Sum(p => Math.Pow(p.Value - yMean, 2));
        var ssResidual = points.Select((p, i) => 
            Math.Pow(p.Value - (slope * i + intercept), 2)).Sum();
        var rSquared = 1 - (ssResidual / ssTotal);
        
        return new TrendAnalysis
        {
            Direction = slope > 0.1 ? TrendDirection.Up :
                       slope < -0.1 ? TrendDirection.Down : 
                       TrendDirection.Flat,
            Slope = slope,
            Intercept = intercept,
            RSquared = rSquared,
            Confidence = CalculateConfidence(rSquared, n)
        };
    }
}
```

### KPI Rule Actions

#### RuleActionCreateKPI
Creates KPI instances from events:

```csharp
public class RuleActionCreateKPI : IRuleActionEvaluator
{
    public class Args
    {
        public string KPITemplate { get; set; }
        public string? SubjectUserQuery { get; set; }
        public string? SubjectWorkSetQuery { get; set; }
        public string? SubjectWorkItemQuery { get; set; }
        public JObject? Parameters { get; set; }
        public bool EvaluateImmediately { get; set; } = true;
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        // Resolve subject
        var subject = new KPISubject
        {
            UserId = ResolveGuid(eventData, a.SubjectUserQuery),
            WorkSetId = ResolveGuid(eventData, a.SubjectWorkSetQuery),
            WorkItemId = ResolveGuid(eventData, a.SubjectWorkItemQuery)
        };
        
        // Create KPI
        var template = await LoadKPITemplate(a.KPITemplate);
        
        if (a.EvaluateImmediately)
        {
            // Evaluate now
            var instance = await _kpiEvaluator.EvaluateKPI(
                template,
                CancellationToken.None,
                subject,
                a.Parameters);
                
            eventData[$"kpi_{a.KPITemplate}_Id"] = instance.Id;
            eventData[$"kpi_{a.KPITemplate}_Value"] = instance.ComputedValues;
        }
        else
        {
            // Schedule evaluation
            await _scheduler.ScheduleKPIEvaluation(
                template,
                subject,
                a.Parameters);
        }
        
        return eventData;
    }
}
```

### KPI Examples

#### Creating a Response Time KPI
```csharp
var responseTimeKPI = new KPITemplate
{
    Name = "TicketResponseTime",
    Title = "Average Ticket Response Time",
    Tags = new List<string> { "support", "performance" },
    
    ScheduleTemplate = "0 */4 * * *",  // Every 4 hours
    
    SubjectType = KPISubjectType.WorkSet,
    SubjectEntityType = "WorkItem",
    SubjectTemplate = "SupportTicket",
    
    Sources = new List<KPISource>
    {
        new KPISource
        {
            Name = "tickets",
            Type = KPISourceType.Entity,
            EntityType = "WorkItem",
            Filter = JObject.FromObject(new
            {
                Template = "SupportTicket",
                Status = new { $in = new[] { 2, 3, 4 } }  // Resolved statuses
            }),
            Query = new RelativeTableQueryCommand
            {
                TimeFrame = TimeFrame.Last7Days,
                TimeField = "ResolvedDate"
            }
        }
    },
    
    ComputeStages = new List<KPIComputeStage>
    {
        new KPIComputeStage
        {
            Name = "CalculateResponseTimes",
            Type = KPIComputeType.Script,
            Script = @"
                var responseTimes = data.tickets
                    .Where(t => t.ResolvedDate != null)
                    .Select(t => (t.ResolvedDate - t.CreatedDate).TotalHours)
                    .ToList();
                
                return new {
                    average = responseTimes.Average(),
                    median = CalculateMedian(responseTimes),
                    p95 = CalculatePercentile(responseTimes, 95),
                    count = responseTimes.Count
                };
            ",
            ResultProperty = "responseMetrics"
        }
    },
    
    Signals = new List<KPISignal>
    {
        new KPISignal
        {
            Name = "SlowResponse",
            Type = KPISignalType.Threshold,
            Stage = KPISignalStage.PostCompute,
            Property = "responseMetrics.average",
            ThresholdValue = 24,  // 24 hours
            Direction = KPIThresholdDirection.Above,
            NotificationGroup = "support-managers",
            EventTopic = "KPI.SlowResponseDetected"
        }
    }
};
```

#### Creating a Sales Performance KPI
```csharp
var salesKPI = new KPITemplate
{
    Name = "SalesPerformance",
    Title = "Sales Performance Metrics",
    Tags = new List<string> { "sales", "revenue" },
    
    ScheduleTemplate = "0 0 * * *",  // Daily at midnight
    
    SubjectType = KPISubjectType.User,
    
    Sources = new List<KPISource>
    {
        new KPISource
        {
            Name = "orders",
            Type = KPISourceType.Table,
            TableTemplate = "Orders",
            Query = new RelativeTableQueryCommand
            {
                TimeFrame = TimeFrame.Today,
                TimeField = "OrderDate"
            }
        },
        new KPISource
        {
            Name = "previousOrders",
            Type = KPISourceType.Table,
            TableTemplate = "Orders",
            Query = new RelativeTableQueryCommand
            {
                TimeFrame = TimeFrame.Yesterday,
                TimeField = "OrderDate"
            }
        }
    },
    
    ComputeStages = new List<KPIComputeStage>
    {
        new KPIComputeStage
        {
            Name = "TodayMetrics",
            Type = KPIComputeType.Script,
            Script = @"
                var userOrders = data.orders
                    .Where(o => o.SalesRepId == subject.UserId);
                
                return new {
                    orderCount = userOrders.Count(),
                    revenue = userOrders.Sum(o => o.TotalAmount),
                    avgOrderValue = userOrders.Any() ? 
                        userOrders.Average(o => o.TotalAmount) : 0
                };
            ",
            ResultProperty = "today"
        },
        new KPIComputeStage
        {
            Name = "YesterdayMetrics",
            Type = KPIComputeType.Script,
            Script = @"
                var userOrders = data.previousOrders
                    .Where(o => o.SalesRepId == subject.UserId);
                
                return new {
                    orderCount = userOrders.Count(),
                    revenue = userOrders.Sum(o => o.TotalAmount)
                };
            ",
            ResultProperty = "yesterday"
        },
        new KPIComputeStage
        {
            Name = "CalculateGrowth",
            Type = KPIComputeType.Script,
            Script = @"
                var todayRevenue = (double)results.today.revenue;
                var yesterdayRevenue = (double)results.yesterday.revenue;
                
                var growth = yesterdayRevenue > 0 ? 
                    ((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100 : 0;
                
                return growth;
            ",
            ResultProperty = "dailyGrowth"
        }
    },
    
    Signals = new List<KPISignal>
    {
        new KPISignal
        {
            Name = "ExceptionalGrowth",
            Type = KPISignalType.Threshold,
            Stage = KPISignalStage.PostCompute,
            Property = "dailyGrowth",
            ThresholdValue = 50,  // 50% growth
            Direction = KPIThresholdDirection.Above,
            EventTopic = "Sales.ExceptionalPerformance"
        },
        new KPISignal
        {
            Name = "StatisticalAnomaly",
            Type = KPISignalType.Statistical,
            Stage = KPISignalStage.Sample,
            Property = "revenue",
            ZScoreThreshold = 3.0,
            LookbackSamples = 30,
            EventTopic = "Sales.AnomalyDetected"
        }
    }
};
```

#### Implementing System Health KPIs
```csharp
public class SystemHealthKPIService
{
    public async Task SetupSystemHealthKPIs()
    {
        // Error rate KPI
        var errorRateKPI = new KPITemplate
        {
            Name = "SystemErrorRate",
            Title = "System Error Rate",
            Tags = new List<string> { "system", "health", "monitoring" },
            
            ScheduleTemplate = "*/5 * * * *",  // Every 5 minutes
            
            Sources = new List<KPISource>
            {
                new KPISource
                {
                    Name = "errors",
                    Type = KPISourceType.Event,
                    EventTopic = "System.Error",
                    Query = new RelativeTableQueryCommand
                    {
                        TimeFrame = TimeFrame.Last5Minutes
                    }
                },
                new KPISource
                {
                    Name = "requests",
                    Type = KPISourceType.Event,
                    EventTopic = "System.Request",
                    Query = new RelativeTableQueryCommand
                    {
                        TimeFrame = TimeFrame.Last5Minutes
                    }
                }
            },
            
            ComputeStages = new List<KPIComputeStage>
            {
                new KPIComputeStage
                {
                    Name = "CalculateErrorRate",
                    Type = KPIComputeType.Script,
                    Script = @"
                        var errorCount = data.errors.Count();
                        var requestCount = data.requests.Count();
                        
                        var errorRate = requestCount > 0 ? 
                            (errorCount / (double)requestCount) * 100 : 0;
                        
                        return new {
                            errorCount = errorCount,
                            requestCount = requestCount,
                            errorRate = errorRate,
                            timestamp = DateTime.UtcNow
                        };
                    ",
                    ResultProperty = "metrics"
                }
            },
            
            Signals = new List<KPISignal>
            {
                new KPISignal
                {
                    Name = "HighErrorRate",
                    Type = KPISignalType.Threshold,
                    Stage = KPISignalStage.PostCompute,
                    Property = "metrics.errorRate",
                    ThresholdValue = 5,  // 5% error rate
                    Direction = KPIThresholdDirection.Above,
                    NotificationGroup = "ops-team",
                    EventTopic = "System.HighErrorRate",
                    EventData = JObject.FromObject(new
                    {
                        severity = "critical",
                        escalate = true
                    })
                },
                new KPISignal
                {
                    Name = "ErrorSpike",
                    Type = KPISignalType.Statistical,
                    Stage = KPISignalStage.Sample,
                    Property = "errorCount",
                    ZScoreThreshold = 2.5,
                    LookbackSamples = 12,  // 1 hour of 5-min samples
                    NotificationGroup = "ops-team"
                }
            }
        };
        
        await _contentService.SaveKPITemplate(errorRateKPI);
        
        // Response time KPI
        var responseTimeKPI = new KPITemplate
        {
            Name = "APIResponseTime",
            Title = "API Response Time",
            
            Sources = new List<KPISource>
            {
                new KPISource
                {
                    Name = "apiCalls",
                    Type = KPISourceType.Table,
                    TableTemplate = "APIMetrics",
                    Query = new RelativeTableQueryCommand
                    {
                        TimeFrame = TimeFrame.Last5Minutes,
                        TimeField = "Timestamp"
                    }
                }
            },
            
            ComputeStages = new List<KPIComputeStage>
            {
                new KPIComputeStage
                {
                    Name = "ResponseTimeStats",
                    Type = KPIComputeType.Script,
                    Script = @"
                        var responseTimes = data.apiCalls
                            .Select(c => c.ResponseTimeMs)
                            .ToList();
                        
                        return new {
                            avg = responseTimes.Average(),
                            p50 = CalculatePercentile(responseTimes, 50),
                            p95 = CalculatePercentile(responseTimes, 95),
                            p99 = CalculatePercentile(responseTimes, 99),
                            max = responseTimes.Max(),
                            count = responseTimes.Count()
                        };
                    ",
                    ResultProperty = "performance"
                }
            },
            
            Signals = new List<KPISignal>
            {
                new KPISignal
                {
                    Name = "SlowResponse",
                    Type = KPISignalType.Threshold,
                    Stage = KPISignalStage.PostCompute,
                    Property = "performance.p95",
                    ThresholdValue = 1000,  // 1 second
                    Direction = KPIThresholdDirection.Above,
                    ConsecutiveCount = 3,  // 3 consecutive slow periods
                    NotificationGroup = "ops-team"
                }
            }
        };
        
        await _contentService.SaveKPITemplate(responseTimeKPI);
    }
}
```

### Best Practices

1. **Design meaningful KPIs** - Focus on actionable metrics that drive decisions
2. **Set appropriate time windows** - Balance between data freshness and stability
3. **Use statistical signals** - Detect anomalies beyond simple thresholds
4. **Implement data validation** - Handle insufficient data gracefully
5. **Optimize computation** - Use efficient queries and calculations
6. **Cache intermediate results** - Avoid redundant calculations
7. **Monitor KPI performance** - Track computation time and resource usage
8. **Document business logic** - Make calculations transparent and auditable
9. **Version KPI definitions** - Track changes to calculations over time
10. **Test signal sensitivity** - Avoid alert fatigue from over-sensitive signals

---
