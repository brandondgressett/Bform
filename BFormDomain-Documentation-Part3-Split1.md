# BFormDomain Comprehensive Documentation - Part 3

## Reports System

The Reports System provides flexible report generation capabilities with support for multiple output formats, dynamic data binding, scheduled generation, and template-based layouts. It integrates with the table and entity systems to create rich, data-driven reports.

### Core Components

#### ReportTemplate
Defines the structure and data sources for reports:

```csharp
public class ReportTemplate : IContentType
{
    public string Name { get; set; }                // Unique identifier
    public string Title { get; set; }               // Display name
    public List<string> Tags { get; set; }          // Categorization
    
    // Data sources
    public List<ReportDataSource> DataSources { get; set; } = new();
    
    // Layout
    public ReportLayout Layout { get; set; }         // Layout configuration
    public string? TemplateHtml { get; set; }        // HTML template
    public string? TemplateCss { get; set; }         // Custom CSS
    
    // Output options
    public List<ReportOutputFormat> SupportedFormats { get; set; } = new();
    public ReportOrientation DefaultOrientation { get; set; }
    public string? PageSize { get; set; }            // A4, Letter, etc.
    
    // Parameters
    public List<ReportParameter> Parameters { get; set; } = new();
    
    // Scheduling
    public List<ScheduledEventTemplate> Schedules { get; set; } = new();
    
    // Permissions
    public bool IsVisibleToUsers { get; set; }       // User visibility
    public List<string> AllowedRoles { get; set; } = new();
    
    // Caching
    public int? CacheMinutes { get; set; }           // Cache duration
}

public class ReportDataSource
{
    public string Name { get; set; }                // Source identifier
    public DataSourceType Type { get; set; }        // Source type
    
    // Query configuration
    public string? TableTemplate { get; set; }      // Table source
    public string? EntityType { get; set; }         // Entity source
    public string? QueryName { get; set; }          // Registered query
    public JObject? Filter { get; set; }            // Filter criteria
    public string? OrderBy { get; set; }            // Sort field
    public bool Ascending { get; set; } = true;     // Sort direction
    
    // Aggregation
    public List<AggregationDef>? Aggregations { get; set; }
    public List<string>? GroupBy { get; set; }
    
    // Pagination
    public int? MaxRows { get; set; }               // Row limit
}

public enum DataSourceType
{
    Table = 0,       // Table data
    Entity = 1,      // Entity data
    Query = 2,       // Registered query
    KPI = 3,         // KPI data
    Aggregation = 4  // Aggregated data
}

public class ReportParameter
{
    public string Name { get; set; }                // Parameter name
    public string DisplayName { get; set; }         // Display label
    public ParameterType Type { get; set; }         // Data type
    public bool Required { get; set; }              // Is required
    public JToken? DefaultValue { get; set; }       // Default value
    public JObject? Options { get; set; }           // Type-specific options
}

public enum ParameterType
{
    String = 0,
    Number = 1,
    Date = 2,
    DateRange = 3,
    Boolean = 4,
    Select = 5,      // Dropdown
    MultiSelect = 6  // Multi-select
}
```

#### ReportInstance
Represents a generated report:

```csharp
public class ReportInstance : IAppEntity
{
    // Standard IAppEntity properties...
    
    // Generation metadata
    public DateTime GeneratedAt { get; set; }        // Generation time
    public TimeSpan GenerationTime { get; set; }     // Processing time
    public string GeneratedBy { get; set; }          // User or system
    
    // Parameters used
    public JObject Parameters { get; set; }          // Parameter values
    public DateTime? DataAsOf { get; set; }          // Data timestamp
    
    // Output
    public ReportOutputFormat Format { get; set; }   // Output format
    public string? OutputPath { get; set; }          // File location
    public byte[]? OutputData { get; set; }          // Inline data
    public long OutputSize { get; set; }             // File size
    
    // Status
    public ReportStatus Status { get; set; }         // Generation status
    public string? ErrorMessage { get; set; }        // Error details
    
    // Distribution
    public List<ReportDistribution> Distributions { get; set; } = new();
    public bool IsPublic { get; set; }              // Public access
    public DateTime? ExpiresAt { get; set; }         // Expiration
}

public enum ReportStatus
{
    Pending = 0,     // Queued
    Generating = 1,  // In progress
    Completed = 2,   // Success
    Failed = 3,      // Error
    Expired = 4      // Expired
}

public enum ReportOutputFormat
{
    HTML = 0,
    PDF = 1,
    Excel = 2,
    CSV = 3,
    JSON = 4,
    XML = 5
}

public class ReportDistribution
{
    public DistributionType Type { get; set; }      // Distribution method
    public string Recipient { get; set; }           // Recipient identifier
    public DateTime SentAt { get; set; }            // When sent
    public bool Delivered { get; set; }             // Delivery status
}

public enum DistributionType
{
    Email = 0,
    FileShare = 1,
    API = 2,
    Dashboard = 3
}
```

#### ChartSpec
Defines chart visualizations within reports:

```csharp
public class ChartSpec
{
    public string ChartId { get; set; }             // Unique identifier
    public ChartType Type { get; set; }             // Chart type
    public string DataSource { get; set; }          // Data source name
    
    // Data mapping
    public string? XAxis { get; set; }              // X-axis field
    public List<string> YAxis { get; set; } = new(); // Y-axis fields
    public string? GroupBy { get; set; }            // Series grouping
    
    // Appearance
    public string Title { get; set; }               // Chart title
    public ChartOptions Options { get; set; }       // Chart options
    public Dictionary<string, string> Colors { get; set; } = new();
    
    // Layout
    public int Width { get; set; } = 100;           // Width percentage
    public int Height { get; set; } = 400;          // Height pixels
}

public enum ChartType
{
    Line = 0,
    Bar = 1,
    Pie = 2,
    Area = 3,
    Scatter = 4,
    Heatmap = 5,
    Gauge = 6,
    Table = 7
}

public class ChartOptions
{
    public bool ShowLegend { get; set; } = true;
    public bool ShowTooltips { get; set; } = true;
    public bool Stacked { get; set; }
    public bool ShowDataLabels { get; set; }
    public string? NumberFormat { get; set; }
    public JObject? CustomOptions { get; set; }     // Chart library options
}
```

### Report Services

#### ReportLogic
Primary service for report operations:

```csharp
public class ReportLogic
{
    // Generate report
    public async Task<ReportInstance> GenerateReport(
        Guid currentUser,
        string templateName,
        JObject parameters,
        ReportOutputFormat format = ReportOutputFormat.PDF,
        GenerationOptions? options = null)
    {
        var template = await LoadReportTemplate(templateName);
        
        // Validate parameters
        ValidateParameters(template, parameters);
        
        // Check permissions
        if (!await CanGenerateReport(currentUser, template))
        {
            throw new UnauthorizedException("Cannot generate this report");
        }
        
        // Create report instance
        var instance = new ReportInstance
        {
            Template = templateName,
            Creator = currentUser,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = "user",
            Parameters = parameters,
            Format = format,
            Status = ReportStatus.Pending
        };
        
        using (var tc = await _reportRepo.OpenTransactionAsync())
        {
            await _reportRepo.CreateAsync(tc, instance);
            
            // Queue generation
            await QueueReportGeneration(tc, instance.Id, options);
            
            await tc.CommitAsync();
        }
        
        // Generate immediately if requested
        if (options?.GenerateImmediately == true)
        {
            await ProcessReportGeneration(instance.Id);
        }
        
        return instance;
    }
    
    // Process report generation
    private async Task ProcessReportGeneration(Guid reportId)
    {
        var instance = await LoadReportInstance(reportId);
        var template = await LoadReportTemplate(instance.Template);
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Update status
            instance.Status = ReportStatus.Generating;
            await _reportRepo.UpdateAsync(instance);
            
            // Load data
            var dataSets = await LoadReportData(template, instance.Parameters);
            
            // Generate report
            var output = await GenerateReportOutput(
                template, 
                dataSets, 
                instance.Parameters,
                instance.Format);
                
            // Store output
            if (output.Data.Length > _options.MaxInlineSize)
            {
                // Store as file
                var path = await StoreReportFile(instance.Id, output);
                instance.OutputPath = path;
            }
            else
            {
                // Store inline
                instance.OutputData = output.Data;
            }
            
            instance.OutputSize = output.Data.Length;
            instance.Status = ReportStatus.Completed;
            instance.GenerationTime = stopwatch.Elapsed;
            instance.DataAsOf = DateTime.UtcNow;
            
            await _reportRepo.UpdateAsync(instance);
            
            // Distribute if configured
            if (template.Schedules.Any())
            {
                await DistributeReport(instance, template);
            }
        }
        catch (Exception ex)
        {
            instance.Status = ReportStatus.Failed;
            instance.ErrorMessage = ex.Message;
            await _reportRepo.UpdateAsync(instance);
            throw;
        }
    }
    
    // Load report data
    private async Task<Dictionary<string, DataSet>> LoadReportData(
        ReportTemplate template,
        JObject parameters)
    {
        var dataSets = new Dictionary<string, DataSet>();
        
        foreach (var source in template.DataSources)
        {
            var data = await LoadDataSource(source, parameters);
            dataSets[source.Name] = data;
        }
        
        return dataSets;
    }
    
    // Load individual data source
    private async Task<DataSet> LoadDataSource(
        ReportDataSource source,
        JObject parameters)
    {
        switch (source.Type)
        {
            case DataSourceType.Table:
                return await LoadTableData(source, parameters);
                
            case DataSourceType.Entity:
                return await LoadEntityData(source, parameters);
                
            case DataSourceType.Query:
                return await LoadQueryData(source, parameters);
                
            case DataSourceType.KPI:
                return await LoadKPIData(source, parameters);
                
            case DataSourceType.Aggregation:
                return await LoadAggregatedData(source, parameters);
                
            default:
                throw new NotSupportedException($"Data source type {source.Type} not supported");
        }
    }
}
```

#### HTMLReportEngine
Generates HTML-based reports:

```csharp
public class HTMLReportEngine
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IChartRenderer _chartRenderer;
    
    public async Task<ReportOutput> GenerateReport(
        ReportTemplate template,
        Dictionary<string, DataSet> dataSets,
        JObject parameters,
        ReportOutputFormat format)
    {
        // Render HTML
        var html = await RenderHTML(template, dataSets, parameters);
        
        // Convert to requested format
        switch (format)
        {
            case ReportOutputFormat.HTML:
                return new ReportOutput
                {
                    Data = Encoding.UTF8.GetBytes(html),
                    ContentType = "text/html"
                };
                
            case ReportOutputFormat.PDF:
                return await ConvertToPDF(html, template);
                
            case ReportOutputFormat.Excel:
                return await ConvertToExcel(dataSets, template);
                
            default:
                throw new NotSupportedException($"Format {format} not supported");
        }
    }
    
    private async Task<string> RenderHTML(
        ReportTemplate template,
        Dictionary<string, DataSet> dataSets,
        JObject parameters)
    {
        // Prepare template context
        var context = new TemplateContext
        {
            Title = template.Title,
            Parameters = parameters,
            DataSets = dataSets,
            GeneratedAt = DateTime.UtcNow
        };
        
        // Render charts
        if (template.Layout.Charts != null)
        {
            context.Charts = await RenderCharts(template.Layout.Charts, dataSets);
        }
        
        // Apply template
        var html = await _templateEngine.Render(template.TemplateHtml, context);
        
        // Inject CSS
        if (!string.IsNullOrEmpty(template.TemplateCss))
        {
            html = InjectCSS(html, template.TemplateCss);
        }
        
        return html;
    }
    
    private async Task<Dictionary<string, ChartOutput>> RenderCharts(
        List<ChartSpec> charts,
        Dictionary<string, DataSet> dataSets)
    {
        var chartOutputs = new Dictionary<string, ChartOutput>();
        
        foreach (var chart in charts)
        {
            var dataSet = dataSets[chart.DataSource];
            var output = await _chartRenderer.RenderChart(chart, dataSet);
            chartOutputs[chart.ChartId] = output;
        }
        
        return chartOutputs;
    }
}
```

### Report Rule Actions

#### RuleActionCreateReport
Creates reports from events:

```csharp
public class RuleActionCreateReport : IRuleActionEvaluator
{
    public class Args
    {
        public string ReportTemplate { get; set; }
        public JObject? Parameters { get; set; }
        public string? ParametersQuery { get; set; }
        public ReportOutputFormat Format { get; set; } = ReportOutputFormat.PDF;
        public bool GenerateImmediately { get; set; } = false;
        public List<string>? EmailTo { get; set; }
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        // Build parameters
        var parameters = a.Parameters ?? new JObject();
        
        if (!string.IsNullOrEmpty(a.ParametersQuery))
        {
            var queryParams = JsonPathQuery(eventData, a.ParametersQuery) as JObject;
            if (queryParams != null)
            {
                parameters.Merge(queryParams);
            }
        }
        
        // Generate report
        var instance = await _reportLogic.EventGenerateReport(
            templateName: a.ReportTemplate,
            parameters: parameters,
            format: a.Format,
            options: new GenerationOptions
            {
                GenerateImmediately = a.GenerateImmediately,
                EmailTo = a.EmailTo
            });
            
        eventData[$"report_{a.ReportTemplate}_Id"] = instance.Id;
        
        return eventData;
    }
}
```

### Report Examples

#### Creating a Sales Report Template
```csharp
var salesReportTemplate = new ReportTemplate
{
    Name = "MonthlySalesReport",
    Title = "Monthly Sales Report",
    Tags = new List<string> { "sales", "monthly", "executive" },
    
    DataSources = new List<ReportDataSource>
    {
        new ReportDataSource
        {
            Name = "orders",
            Type = DataSourceType.Table,
            TableTemplate = "Orders",
            Filter = JObject.FromObject(new
            {
                OrderDate = new
                {
                    $gte = "@startDate",
                    $lt = "@endDate"
                },
                Status = new { $ne = "cancelled" }
            }),
            OrderBy = "OrderDate",
            Ascending = true
        },
        new ReportDataSource
        {
            Name = "summary",
            Type = DataSourceType.Aggregation,
            TableTemplate = "Orders",
            Filter = JObject.FromObject(new
            {
                OrderDate = new
                {
                    $gte = "@startDate",
                    $lt = "@endDate"
                }
            }),
            Aggregations = new List<AggregationDef>
            {
                new AggregationDef { Field = "OrderId", Function = AggregationFunction.Count, Alias = "orderCount" },
                new AggregationDef { Field = "TotalAmount", Function = AggregationFunction.Sum, Alias = "totalRevenue" },
                new AggregationDef { Field = "TotalAmount", Function = AggregationFunction.Average, Alias = "avgOrderValue" }
            }
        },
        new ReportDataSource
        {
            Name = "dailySales",
            Type = DataSourceType.Aggregation,
            TableTemplate = "Orders",
            GroupBy = new List<string> { "date(OrderDate)" },
            Aggregations = new List<AggregationDef>
            {
                new AggregationDef { Field = "TotalAmount", Function = AggregationFunction.Sum, Alias = "dailyRevenue" }
            }
        }
    },
    
    Layout = new ReportLayout
    {
        Sections = new List<ReportSection>
        {
            new ReportSection
            {
                Type = SectionType.Header,
                Content = @"
                    <h1>{{Title}}</h1>
                    <p>Report Period: {{formatDate Parameters.startDate}} to {{formatDate Parameters.endDate}}</p>
                    <p>Generated: {{formatDateTime GeneratedAt}}</p>
                "
            },
            new ReportSection
            {
                Type = SectionType.Summary,
                Content = @"
                    <div class='summary-box'>
                        <h2>Executive Summary</h2>
                        <div class='metrics'>
                            <div class='metric'>
                                <span class='label'>Total Orders</span>
                                <span class='value'>{{DataSets.summary.orderCount}}</span>
                            </div>
                            <div class='metric'>
                                <span class='label'>Total Revenue</span>
                                <span class='value'>${{formatNumber DataSets.summary.totalRevenue}}</span>
                            </div>
                            <div class='metric'>
                                <span class='label'>Average Order Value</span>
                                <span class='value'>${{formatNumber DataSets.summary.avgOrderValue 2}}</span>
                            </div>
                        </div>
                    </div>
                "
            },
            new ReportSection
            {
                Type = SectionType.Chart,
                ChartId = "dailySalesChart"
            },
            new ReportSection
            {
                Type = SectionType.Table,
                Content = @"
                    <h2>Order Details</h2>
                    <table class='data-table'>
                        <thead>
                            <tr>
                                <th>Order ID</th>
                                <th>Date</th>
                                <th>Customer</th>
                                <th>Items</th>
                                <th>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {{#each DataSets.orders}}
                            <tr>
                                <td>{{OrderId}}</td>
                                <td>{{formatDate OrderDate}}</td>
                                <td>{{CustomerName}}</td>
                                <td>{{ItemCount}}</td>
                                <td>${{formatNumber TotalAmount}}</td>
                            </tr>
                            {{/each}}
                        </tbody>
                    </table>
                "
            }
        },
        
        Charts = new List<ChartSpec>
        {
            new ChartSpec
            {
                ChartId = "dailySalesChart",
                Type = ChartType.Line,
                DataSource = "dailySales",
                Title = "Daily Sales Trend",
                XAxis = "date",
                YAxis = new List<string> { "dailyRevenue" },
                Options = new ChartOptions
                {
                    ShowDataLabels = false,
                    NumberFormat = "$#,##0"
                }
            }
        }
    },
    
    TemplateCss = @"
        .summary-box { background: #f5f5f5; padding: 20px; margin: 20px 0; }
        .metrics { display: flex; justify-content: space-around; }
        .metric { text-align: center; }
        .metric .label { display: block; font-size: 14px; color: #666; }
        .metric .value { display: block; font-size: 24px; font-weight: bold; color: #333; }
        .data-table { width: 100%; border-collapse: collapse; }
        .data-table th, .data-table td { padding: 8px; border: 1px solid #ddd; }
        .data-table th { background: #f0f0f0; font-weight: bold; }
    ",
    
    Parameters = new List<ReportParameter>
    {
        new ReportParameter
        {
            Name = "startDate",
            DisplayName = "Start Date",
            Type = ParameterType.Date,
            Required = true,
            DefaultValue = JToken.FromObject("firstDayOfMonth()")
        },
        new ReportParameter
        {
            Name = "endDate",
            DisplayName = "End Date",
            Type = ParameterType.Date,
            Required = true,
            DefaultValue = JToken.FromObject("today()")
        }
    },
    
    SupportedFormats = new List<ReportOutputFormat>
    {
        ReportOutputFormat.PDF,
        ReportOutputFormat.Excel,
        ReportOutputFormat.HTML
    },
    
    DefaultOrientation = ReportOrientation.Portrait,
    PageSize = "Letter",
    
    Schedules = new List<ScheduledEventTemplate>
    {
        new ScheduledEventTemplate
        {
            Name = "MonthlyGeneration",
            Schedule = "0 0 1 * *",  // First day of month
            Topic = "GenerateMonthlyReport"
        }
    },
    
    IsVisibleToUsers = true,
    AllowedRoles = new List<string> { "Manager", "Executive" },
    CacheMinutes = 60
};
```

#### Implementing a Dashboard Report Service
```csharp
public class DashboardReportService
{
    // Generate dashboard reports
    public async Task<DashboardReportResult> GenerateDashboardReports(
        Guid userId,
        Guid workSetId,
        DateRange period)
    {
        var result = new DashboardReportResult();
        
        // Load user's dashboard configuration
        var dashboardConfig = await LoadDashboardConfig(userId, workSetId);
        
        // Generate each widget report
        var tasks = dashboardConfig.Widgets.Select(async widget =>
        {
            try
            {
                var reportInstance = await GenerateWidgetReport(
                    widget,
                    period,
                    userId,
                    workSetId);
                    
                return new WidgetReportResult
                {
                    WidgetId = widget.Id,
                    Success = true,
                    ReportInstance = reportInstance
                };
            }
            catch (Exception ex)
            {
                return new WidgetReportResult
                {
                    WidgetId = widget.Id,
                    Success = false,
                    Error = ex.Message
                };
            }
        });
        
        result.WidgetReports = await Task.WhenAll(tasks);
        result.GeneratedAt = DateTime.UtcNow;
        
        return result;
    }
    
    private async Task<ReportInstance> GenerateWidgetReport(
        DashboardWidget widget,
        DateRange period,
        Guid userId,
        Guid workSetId)
    {
        // Build parameters
        var parameters = new JObject
        {
            ["startDate"] = period.Start,
            ["endDate"] = period.End,
            ["userId"] = userId,
            ["workSetId"] = workSetId
        };
        
        // Add widget-specific parameters
        if (widget.Parameters != null)
        {
            parameters.Merge(widget.Parameters);
        }
        
        // Generate report
        return await _reportLogic.GenerateReport(
            currentUser: userId,
            templateName: widget.ReportTemplate,
            parameters: parameters,
            format: ReportOutputFormat.JSON, // JSON for dashboard
            options: new GenerationOptions
            {
                GenerateImmediately = true,
                CacheKey = $"dashboard_{widget.Id}_{period.GetHashCode()}"
            });
    }
}
```

#### Creating a Compliance Report
```csharp
public class ComplianceReportTemplate
{
    public static ReportTemplate CreateAuditLogReport()
    {
        return new ReportTemplate
        {
            Name = "AuditLogReport",
            Title = "System Audit Log Report",
            Tags = new List<string> { "compliance", "audit", "security" },
            
            DataSources = new List<ReportDataSource>
            {
                new ReportDataSource
                {
                    Name = "auditEvents",
                    Type = DataSourceType.Table,
                    TableTemplate = "AuditLog",
                    Filter = JObject.FromObject(new
                    {
                        Timestamp = new
                        {
                            $gte = "@startDate",
                            $lt = "@endDate"
                        },
                        EventType = "@eventType"  // Optional filter
                    }),
                    OrderBy = "Timestamp",
                    Ascending = false,
                    MaxRows = 10000
                },
                new ReportDataSource
                {
                    Name = "userActivity",
                    Type = DataSourceType.Aggregation,
                    TableTemplate = "AuditLog",
                    GroupBy = new List<string> { "UserId", "UserName" },
                    Aggregations = new List<AggregationDef>
                    {
                        new AggregationDef { Field = "EventId", Function = AggregationFunction.Count, Alias = "eventCount" }
                    }
                },
                new ReportDataSource
                {
                    Name = "eventTypes",
                    Type = DataSourceType.Aggregation,
                    TableTemplate = "AuditLog",
                    GroupBy = new List<string> { "EventType" },
                    Aggregations = new List<AggregationDef>
                    {
                        new AggregationDef { Field = "EventId", Function = AggregationFunction.Count, Alias = "count" }
                    }
                }
            },
            
            Parameters = new List<ReportParameter>
            {
                new ReportParameter
                {
                    Name = "startDate",
                    DisplayName = "Start Date",
                    Type = ParameterType.Date,
                    Required = true
                },
                new ReportParameter
                {
                    Name = "endDate",
                    DisplayName = "End Date",
                    Type = ParameterType.Date,
                    Required = true
                },
                new ReportParameter
                {
                    Name = "eventType",
                    DisplayName = "Event Type",
                    Type = ParameterType.Select,
                    Required = false,
                    Options = JObject.FromObject(new
                    {
                        choices = new[]
                        {
                            new { value = "", label = "All Events" },
                            new { value = "Login", label = "Login" },
                            new { value = "Logout", label = "Logout" },
                            new { value = "Create", label = "Create" },
                            new { value = "Update", label = "Update" },
                            new { value = "Delete", label = "Delete" },
                            new { value = "Export", label = "Export" },
                            new { value = "Permission", label = "Permission Change" }
                        }
                    })
                }
            },
            
            // Compliance features
            SupportedFormats = new List<ReportOutputFormat>
            {
                ReportOutputFormat.PDF,  // For archival
                ReportOutputFormat.CSV   // For analysis
            },
            
            // Restricted access
            IsVisibleToUsers = false,
            AllowedRoles = new List<string> { "Auditor", "Administrator" }
        };
    }
}
```

### Best Practices

1. **Design reusable templates** - Create parameterized templates for flexibility
2. **Optimize data queries** - Use aggregations and limits to manage data volume
3. **Cache frequently used reports** - Reduce generation overhead
4. **Implement access controls** - Ensure data security and compliance
5. **Version report templates** - Track changes to report definitions
6. **Handle large datasets** - Implement pagination and streaming
7. **Provide multiple formats** - Support various output requirements
8. **Schedule off-peak generation** - Reduce system load
9. **Monitor report performance** - Track generation times and resource usage
10. **Archive historical reports** - Maintain audit trail and compliance

---

## Comments System

The Comments System provides threaded discussion capabilities for any entity in the platform. It supports nested replies, mentions, attachments, moderation, and real-time notifications.

### Core Components

#### Comment
The primary comment entity:

```csharp
public class Comment : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // Entity reference
    public string EntityType { get; set; }          // Entity type
    public Guid EntityId { get; set; }              // Entity ID
    public string? EntityTemplate { get; set; }      // Entity template
    
    // Content
    public string Text { get; set; }                // Comment text
    public string? FormattedText { get; set; }      // HTML formatted
    public List<string> Mentions { get; set; } = new(); // @mentions
    public List<AttachmentReference> Attachments { get; set; } = new();
    
    // Threading
    public Guid? ParentCommentId { get; set; }      // Parent for replies
    public int NestingLevel { get; set; }           // Reply depth
    public string ThreadPath { get; set; }          // Hierarchical path
    
    // Metadata
    public Guid Author { get; set; }                // Comment author
    public DateTime CreatedAt { get; set; }         // Creation time
    public DateTime? EditedAt { get; set; }         // Last edit time
    public bool IsEdited { get; set; }              // Edit flag
    
    // Status
    public CommentStatus Status { get; set; }       // Comment status
    public bool IsDeleted { get; set; }             // Soft delete
    public DateTime? DeletedAt { get; set; }        // Deletion time
    public Guid? DeletedBy { get; set; }            // Who deleted
    
    // Moderation
    public bool IsFlagged { get; set; }             // Flagged for review
    public int FlagCount { get; set; }              // Number of flags
    public List<CommentFlag> Flags { get; set; } = new();
    public ModerationStatus? ModerationStatus { get; set; }
    
    // Engagement
    public int LikeCount { get; set; }              // Number of likes
    public List<Guid> LikedBy { get; set; } = new(); // Users who liked
    public int ReplyCount { get; set; }             // Direct replies
    
    // Permissions
    public bool AllowReplies { get; set; } = true;  // Can be replied to
    public bool AllowEditing { get; set; } = true;  // Can be edited
}

public enum CommentStatus
{
    Active = 0,      // Normal state
    Hidden = 1,      // Hidden by moderation
    Pending = 2,     // Awaiting moderation
    Locked = 3       // No further interaction
}

public enum ModerationStatus
{
    Approved = 0,
    Rejected = 1,
    Pending = 2,
    AutoApproved = 3
}

public class CommentFlag
{
    public Guid UserId { get; set; }               // Who flagged
    public DateTime FlaggedAt { get; set; }         // When flagged
    public FlagReason Reason { get; set; }          // Why flagged
    public string? Details { get; set; }            // Additional info
}

public enum FlagReason
{
    Spam = 0,
    Inappropriate = 1,
    Offensive = 2,
    Misinformation = 3,
    Other = 4
}
```

### Comment Services

#### CommentsLogic
Primary service for comment operations:

```csharp
public class CommentsLogic
{
    // Add comment
    public async Task<Comment> AddComment(
        Guid currentUser,
        string entityType,
        Guid entityId,
        string text,
        Guid? parentCommentId = null,
        List<ManagedFileAction>? attachments = null)
    {
        // Validate entity exists and user has access
        await ValidateEntityAccess(currentUser, entityType, entityId);
        
        // Check if comments are allowed
        var template = await GetEntityTemplate(entityType, entityId);
        if (!template.AllowComments)
        {
            throw new InvalidOperationException("Comments not allowed for this entity");
        }
        
        // Process text
        var processedText = await ProcessCommentText(text);
        
        Comment comment;
        
        using (var tc = await _commentRepo.OpenTransactionAsync())
        {
            // Create comment
            comment = new Comment
            {
                EntityType = entityType,
                EntityId = entityId,
                EntityTemplate = template.Name,
                Text = processedText.Text,
                FormattedText = processedText.FormattedHtml,
                Mentions = processedText.Mentions,
                Author = currentUser,
                CreatedAt = DateTime.UtcNow,
                Status = CommentStatus.Active
            };
            
            // Handle threading
            if (parentCommentId.HasValue)
            {
                var parent = await LoadComment(tc, parentCommentId.Value);
                
                if (!parent.AllowReplies)
                {
                    throw new InvalidOperationException("Replies not allowed for this comment");
                }
                
                comment.ParentCommentId = parentCommentId;
                comment.NestingLevel = Math.Min(parent.NestingLevel + 1, _options.MaxNestingLevel);
                comment.ThreadPath = $"{parent.ThreadPath}/{comment.Id}";
                
                // Update parent reply count
                parent.ReplyCount++;
                await _commentRepo.UpdateAsync(tc, parent);
            }
            else
            {
                comment.NestingLevel = 0;
                comment.ThreadPath = comment.Id.ToString();
            }
            
            // Apply moderation
            comment.ModerationStatus = await ApplyModeration(comment, currentUser);
            if (comment.ModerationStatus == ModerationStatus.Rejected)
            {
                comment.Status = CommentStatus.Hidden;
            }
            
            // Handle attachments
            if (attachments != null && attachments.Any())
            {
                comment.Attachments = await ProcessAttachments(tc, attachments, currentUser);
            }
            
            await _commentRepo.CreateAsync(tc, comment);
            
            // Send notifications
            await SendCommentNotifications(tc, comment);
            
            // Generate event
            await _eventSink.SinkAsync(tc, new AppEvent
            {
                Topic = "Comment.Created",
                OriginEntityType = "Comment",
                OriginId = comment.Id,
                EntityPayload = comment.ToBsonDocument()
            });
            
            await tc.CommitAsync();
        }
        
        return comment;
    }
    
    // Edit comment
    public async Task<Comment> EditComment(
        Guid currentUser,
        Guid commentId,
        string newText)
    {
        var comment = await LoadComment(commentId);
        
        // Check permissions
        if (comment.Author != currentUser && !await IsModeratorAsync(currentUser))
        {
            throw new UnauthorizedException("Cannot edit this comment");
        }
        
        if (!comment.AllowEditing || comment.Status != CommentStatus.Active)
        {
            throw new InvalidOperationException("Comment cannot be edited");
        }
        
        // Check edit time limit
        if (_options.EditTimeLimit.HasValue && 
            DateTime.UtcNow - comment.CreatedAt > _options.EditTimeLimit.Value)
        {
            throw new InvalidOperationException("Edit time limit exceeded");
        }
        
        // Process new text
        var processedText = await ProcessCommentText(newText);
        
        using (var tc = await _commentRepo.OpenTransactionAsync())
        {
            comment.Text = processedText.Text;
            comment.FormattedText = processedText.FormattedHtml;
            comment.Mentions = processedText.Mentions;
            comment.IsEdited = true;
            comment.EditedAt = DateTime.UtcNow;
            
            await _commentRepo.UpdateAsync(tc, comment);
            
            // Notify mentioned users
            await NotifyNewMentions(tc, comment, processedText.Mentions);
            
            await tc.CommitAsync();
        }
        
        return comment;
    }
    
    // Get comments for entity
    public async Task<CommentThreadViewModel> GetEntityComments(
        Guid currentUser,
        string entityType,
        Guid entityId,
        CommentQueryOptions? options = null)
    {
        // Validate access
        await ValidateEntityAccess(currentUser, entityType, entityId);
        
        options ??= new CommentQueryOptions();
        
        // Load comments
        var (comments, _) = await _commentRepo.GetAsync(
            c => c.EntityType == entityType && 
                 c.EntityId == entityId &&
                 !c.IsDeleted &&
                 (c.Status == CommentStatus.Active || 
                  await IsModeratorAsync(currentUser)));
                  
        // Build thread structure
        var rootComments = comments
            .Where(c => !c.ParentCommentId.HasValue)
            .OrderBy(c => GetSortValue(c, options.SortBy))
            .Skip(options.Skip)
            .Take(options.Take)
            .ToList();
            
        var thread = new CommentThreadViewModel
        {
            EntityType = entityType,
            EntityId = entityId,
            TotalComments = comments.Count,
            Comments = new List<CommentViewModel>()
        };
        
        // Build comment tree
        foreach (var root in rootComments)
        {
            var commentVm = await BuildCommentViewModel(root, comments, currentUser);
            thread.Comments.Add(commentVm);
        }
        
        return thread;
    }
    
    // Like/unlike comment
    public async Task<Comment> ToggleLike(
        Guid currentUser,
        Guid commentId)
    {
        var comment = await LoadComment(commentId);
        
        using (var tc = await _commentRepo.OpenTransactionAsync())
        {
            if (comment.LikedBy.Contains(currentUser))
            {
                // Unlike
                comment.LikedBy.Remove(currentUser);
                comment.LikeCount--;
            }
            else
            {
                // Like
                comment.LikedBy.Add(currentUser);
                comment.LikeCount++;
                
                // Notify author
                if (comment.Author != currentUser)
                {
                    await NotifyCommentLiked(tc, comment, currentUser);
                }
            }
            
            await _commentRepo.UpdateAsync(tc, comment);
            await tc.CommitAsync();
        }
        
        return comment;
    }
    
    // Process comment text
    private async Task<ProcessedText> ProcessCommentText(string text)
    {
        var result = new ProcessedText { Text = text };
        
        // Extract mentions
        var mentionRegex = new Regex(@"@(\w+)");
        var mentions = mentionRegex.Matches(text);
        
        foreach (Match match in mentions)
        {
            var username = match.Groups[1].Value;
            var user = await _userService.FindByUsername(username);
            
            if (user != null)
            {
                result.Mentions.Add(username);
            }
        }
        
        // Convert to HTML (markdown, mentions, etc.)
        result.FormattedHtml = await _formatter.FormatComment(text);
        
        return result;
    }
}
```

### Comment Rule Actions

#### RuleActionCommentCreate
Creates comments from events:

```csharp
public class RuleActionCommentCreate : IRuleActionEvaluator
{
    public class Args
    {
        public string? EntityTypeQuery { get; set; }
        public string? EntityIdQuery { get; set; }
        public string? Text { get; set; }
        public string? TextQuery { get; set; }
        public string? AuthorQuery { get; set; }
        public bool SystemComment { get; set; } = false;
    }
    
    public async Task<JObject> EvaluateAsync(JObject args, JObject eventData)
    {
        var a = args.ToObject<Args>();
        
        // Resolve parameters
        var entityType = ResolveString(eventData, a.EntityTypeQuery) ?? 
            eventData["OriginEntityType"]?.ToString();
        var entityId = ResolveGuid(eventData, a.EntityIdQuery) ?? 
            Guid.Parse(eventData["OriginId"].ToString());
        var text = a.Text ?? ResolveString(eventData, a.TextQuery);
        var author = ResolveGuid(eventData, a.AuthorQuery) ?? 
            (a.SystemComment ? Guid.Empty : throw new InvalidOperationException("Author required"));
            
        // Create comment
        var comment = await _commentsLogic.EventCreateComment(
            entityType: entityType,
            entityId: entityId.Value,
            text: text,
            author: author.Value,
            isSystem: a.SystemComment);
            
        eventData["createdCommentId"] = comment.Id;
        
        return eventData;
    }
}
```

### Comment Examples

#### Implementing a Discussion Forum
```csharp
public class ForumService
{
    // Create forum post with initial comment
    public async Task<ForumPostResult> CreateForumPost(
        Guid userId,
        string title,
        string content,
        string[] tags,
        Guid forumId)
    {
        using (var tc = await _workItemRepo.OpenTransactionAsync())
        {
            // Create forum post as work item
            var post = await _workItemLogic.CreateWorkItem(
                tc,
                currentUser: userId,
                templateName: "ForumPost",
                workSet: forumId,
                title: title,
                description: content.Substring(0, Math.Min(200, content.Length)) + "...",
                creationData: new WorkItemCreationData
                {
                    Tags = tags,
                    Metadata = JObject.FromObject(new
                    {
                        viewCount = 0,
                        lastActivity = DateTime.UtcNow,
                        isPinned = false,
                        isLocked = false
                    })
                });
                
            // Add content as first comment
            var comment = await _commentsLogic.AddComment(
                tc,
                currentUser: userId,
                entityType: "WorkItem",
                entityId: post.Id,
                text: content);
                
            await tc.CommitAsync();
            
            return new ForumPostResult
            {
                PostId = post.Id,
                CommentId = comment.Id,
                ForumId = forumId
            };
        }
    }
    
    // Get forum thread with pagination
    public async Task<ForumThreadViewModel> GetForumThread(
        Guid userId,
        Guid postId,
        int page = 1,
        int pageSize = 20)
    {
        var post = await _workItemLogic.LoadWorkItem(postId);
        
        // Update view count
        await IncrementViewCount(postId);
        
        // Get comments
        var comments = await _commentsLogic.GetEntityComments(
            currentUser: userId,
            entityType: "WorkItem",
            entityId: postId,
            options: new CommentQueryOptions
            {
                Skip = (page - 1) * pageSize,
                Take = pageSize,
                SortBy = CommentSortBy.Chronological,
                IncludeDeleted = false
            });
            
        return new ForumThreadViewModel
        {
            Post = BuildPostViewModel(post),
            Comments = comments,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(comments.TotalComments / (double)pageSize)
        };
    }
}
```

#### Implementing Comment Moderation
```csharp
public class CommentModerationService
{
    // Auto-moderation rules
    public async Task<ModerationStatus> ApplyAutoModeration(
        Comment comment,
        Guid authorId)
    {
        var author = await _userService.GetUser(authorId);
        
        // New user check
        if (author.CreatedDate > DateTime.UtcNow.AddDays(-7))
        {
            // Check for spam patterns
            if (await IsLikelySpam(comment.Text))
            {
                return ModerationStatus.Rejected;
            }
            
            // Require moderation for links
            if (ContainsLinks(comment.Text))
            {
                return ModerationStatus.Pending;
            }
        }
        
        // Trusted user - auto approve
        if (author.Tags.Contains("trusted"))
        {
            return ModerationStatus.AutoApproved;
        }
        
        // Check banned words
        if (await ContainsBannedWords(comment.Text))
        {
            return ModerationStatus.Pending;
        }
        
        return ModerationStatus.Approved;
    }
    
    // Manual moderation queue
    public async Task<ModerationQueueViewModel> GetModerationQueue(
        Guid moderatorId,
        ModerationFilter filter)
    {
        // Verify moderator role
        if (!await IsModerator(moderatorId))
        {
            throw new UnauthorizedException("Not a moderator");
        }
        
        // Build query
        Expression<Func<Comment, bool>> predicate = c => 
            c.ModerationStatus == ModerationStatus.Pending ||
            c.IsFlagged;
            
        if (filter.EntityType != null)
        {
            predicate = predicate.And(c => c.EntityType == filter.EntityType);
        }
        
        if (filter.DateFrom.HasValue)
        {
            predicate = predicate.And(c => c.CreatedAt >= filter.DateFrom.Value);
        }
        
        // Load comments
        var (comments, _) = await _commentRepo.GetOrderedAsync(
            predicate,
            c => c.FlagCount,
            ascending: false);
            
        return new ModerationQueueViewModel
        {
            Comments = comments.Select(c => BuildModerationView(c)).ToList(),
            TotalCount = comments.Count,
            Filter = filter
        };
    }
    
    // Moderate comment
    public async Task ModerateComment(
        Guid moderatorId,
        Guid commentId,
        ModerationAction action,
        string? reason = null)
    {
        var comment = await LoadComment(commentId);
        
        using (var tc = await _commentRepo.OpenTransactionAsync())
        {
            switch (action)
            {
                case ModerationAction.Approve:
                    comment.ModerationStatus = ModerationStatus.Approved;
                    comment.Status = CommentStatus.Active;
                    comment.IsFlagged = false;
                    break;
                    
                case ModerationAction.Reject:
                    comment.ModerationStatus = ModerationStatus.Rejected;
                    comment.Status = CommentStatus.Hidden;
                    break;
                    
                case ModerationAction.Delete:
                    comment.IsDeleted = true;
                    comment.DeletedAt = DateTime.UtcNow;
                    comment.DeletedBy = moderatorId;
                    comment.Status = CommentStatus.Hidden;
                    break;
                    
                case ModerationAction.Lock:
                    comment.Status = CommentStatus.Locked;
                    comment.AllowReplies = false;
                    comment.AllowEditing = false;
                    break;
            }
            
            await _commentRepo.UpdateAsync(tc, comment);
            
            // Log moderation action
            await LogModerationAction(tc, comment, moderatorId, action, reason);
            
            // Notify author if rejected
            if (action == ModerationAction.Reject || action == ModerationAction.Delete)
            {
                await NotifyCommentModerated(tc, comment, action, reason);
            }
            
            await tc.CommitAsync();
        }
    }
}
```

### Best Practices

1. **Implement rate limiting** - Prevent comment spam and abuse
2. **Use threading wisely** - Limit nesting depth for readability
3. **Process mentions** - Notify mentioned users appropriately
4. **Handle moderation** - Balance automation with manual review
5. **Support rich formatting** - Allow markdown or limited HTML
6. **Enable real-time updates** - Use SignalR or polling for live comments
7. **Implement edit history** - Track changes for transparency
8. **Cache comment counts** - Avoid expensive queries
9. **Handle deleted comments** - Show placeholders in threads
10. **Respect privacy** - Allow users to delete their own comments

---

## Content System

The Content System provides a unified content management framework that handles templates, schemas, and configuration data across all platform components. It supports file-based content, JSON schemas, localization, and version control.

### Core Components

#### IApplicationPlatformContent
The central interface for content management:

```csharp
public interface IApplicationPlatformContent
{
    // Template management
    Task<T?> GetContentTypeAsync<T>(string name) where T : class, IContentType;
    Task<List<T>> GetAllContentTypesAsync<T>() where T : class, IContentType;
    Task SaveContentTypeAsync<T>(T contentType) where T : class, IContentType;
    Task DeleteContentTypeAsync<T>(string name) where T : class, IContentType;
    
    // JSON Schema support
    Task<JSchema?> GetJsonSchemaAsync(string name);
    Task<bool> ValidateAgainstSchemaAsync(string schemaName, JObject data);
    
    // Content domains
    Task<ContentDomain?> GetContentDomainAsync(string name);
    Task<List<ContentDomain>> GetAllContentDomainsAsync();
    
    // Satellite content
    Task<SatelliteJson?> GetSatelliteContentAsync(string name);
    Task<Dictionary<string, object>> GetAllSatelliteContentAsync();
    
    // Localization
    Task<string?> GetLocalizedStringAsync(string key, string? culture = null);
    Task<Dictionary<string, string>> GetAllLocalizedStringsAsync(string culture);
    
    // Events
    event EventHandler<ContentChangedEventArgs> ContentChanged;
}
```

#### IContentType
Base interface for all content types:

```csharp
public interface IContentType
{
    string Name { get; set; }               // Unique identifier
    string Title { get; set; }              // Display name
    List<string> Tags { get; set; }         // Categorization tags
}
```

#### ContentDomain
Represents a logical grouping of content:

```csharp
public class ContentDomain
{
    public string Name { get; set; }                // Domain name
    public string Title { get; set; }               // Display title
    public string? Description { get; set; }         // Description
    public List<string> Tags { get; set; } = new(); // Tags
    
    // Content organization
    public List<ContentElement> Elements { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Schema validation
    public string? SchemaName { get; set; }         // Associated schema
    public bool ValidateContent { get; set; }       // Enable validation
    
    // Access control
    public List<string> AllowedRoles { get; set; } = new();
    public bool IsPublic { get; set; }              // Public access
    
    // Versioning
    public string Version { get; set; } = "1.0";    // Content version
    public DateTime LastModified { get; set; }      // Last update
    public string? ModifiedBy { get; set; }         // Who modified
}
```

#### ContentElement
Individual content item within a domain:

```csharp
public class ContentElement
{
    public string Key { get; set; }                // Element key
    public string? Title { get; set; }              // Display title
    public ContentElementType Type { get; set; }    // Element type
    public JToken Value { get; set; }               // Content value
    
    // Metadata
    public Dictionary<string, object> Attributes { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    
    // Localization
    public Dictionary<string, JToken> Localizations { get; set; } = new();
    
    // Validation
    public string? SchemaRef { get; set; }          // Schema reference
    public bool IsRequired { get; set; }            // Required element
    
    // Lifecycle
    public DateTime CreatedAt { get; set; }         // Creation time
    public DateTime UpdatedAt { get; set; }         // Last update
    public bool IsActive { get; set; } = true;      // Active status
}

public enum ContentElementType
{
    String = 0,      // Text content
    Number = 1,      // Numeric value
    Boolean = 2,     // Boolean flag
    Object = 3,      // JSON object
    Array = 4,       // JSON array
    Reference = 5,   // Reference to other content
    Template = 6,    // Template content
    Schema = 7       // JSON schema
}
```

#### SatelliteJson
External JSON content reference:

```csharp
public class SatelliteJson
{
    public string Name { get; set; }                // Satellite name
    public string FilePath { get; set; }            // File path
    public JObject Content { get; set; }            // Parsed content
    
    // Metadata
    public DateTime LastLoaded { get; set; }        // Last load time
    public long FileSize { get; set; }              // File size
    public string ContentHash { get; set; }         // Content hash
    
    // Configuration
    public bool WatchForChanges { get; set; }       // Auto-reload
    public TimeSpan? RefreshInterval { get; set; }  // Refresh frequency
    public bool CacheContent { get; set; }          // Cache in memory
    
    // Validation
    public string? SchemaFile { get; set; }         // Validation schema
    public bool ValidateOnLoad { get; set; }        // Validate content
}
```

### Content Services

#### FileApplicationPlatformContent
File-based content implementation:

```csharp
public class FileApplicationPlatformContent : IApplicationPlatformContent
{
    private readonly FileApplicationPlatformContentOptions _options;
    private readonly ILogger<FileApplicationPlatformContent> _logger;
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly FileSystemWatcher _watcher;
    
    public FileApplicationPlatformContent(
        IOptions<FileApplicationPlatformContentOptions> options,
        ILogger<FileApplicationPlatformContent> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        // Setup file watcher
        if (_options.WatchForChanges)
        {
            _watcher = new FileSystemWatcher(_options.ContentPath)
            {
                Filter = "*.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };
            
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
        }
    }
    
    public async Task<T?> GetContentTypeAsync<T>(string name) where T : class, IContentType
    {
        var cacheKey = $"{typeof(T).Name}:{name}";
        
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached as T;
        }
        
        var filePath = GetContentFilePath<T>(name);
        
        if (!File.Exists(filePath))
        {
            return null;
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var content = JsonConvert.DeserializeObject<T>(json);
            
            if (_options.CacheContent)
            {
                _cache.TryAdd(cacheKey, content);
            }
            
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load content type {name} of type {typeof(T).Name}");
            return null;
        }
    }
    
    public async Task<List<T>> GetAllContentTypesAsync<T>() where T : class, IContentType
    {
        var directory = GetContentDirectory<T>();
        
        if (!Directory.Exists(directory))
        {
            return new List<T>();
        }
        
        var files = Directory.GetFiles(directory, "*.json");
        var results = new List<T>();
        
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var content = JsonConvert.DeserializeObject<T>(json);
                
                if (content != null)
                {
                    results.Add(content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load content file {file}");
            }
        }
        
        return results;
    }
    
    public async Task SaveContentTypeAsync<T>(T contentType) where T : class, IContentType
    {
        var filePath = GetContentFilePath<T>(contentType.Name);
        var directory = Path.GetDirectoryName(filePath);
        
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        var json = JsonConvert.SerializeObject(contentType, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
        
        // Update cache
        var cacheKey = $"{typeof(T).Name}:{contentType.Name}";
        _cache.TryRemove(cacheKey, out _);
        
        // Raise event
        ContentChanged?.Invoke(this, new ContentChangedEventArgs
        {
            ContentType = typeof(T).Name,
            Name = contentType.Name,
            ChangeType = ContentChangeType.Updated
        });
    }
    
    // JSON Schema support
    public async Task<JSchema?> GetJsonSchemaAsync(string name)
    {
        var cacheKey = $"schema:{name}";
        
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached as JSchema;
        }
        
        var filePath = Path.Combine(_options.ContentPath, "schemas", $"{name}.json");
        
        if (!File.Exists(filePath))
        {
            return null;
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var schema = JSchema.Parse(json);
            
            if (_options.CacheContent)
            {
                _cache.TryAdd(cacheKey, schema);
            }
            
            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load schema {name}");
            return null;
        }
    }
    
    public async Task<bool> ValidateAgainstSchemaAsync(string schemaName, JObject data)
    {
        var schema = await GetJsonSchemaAsync(schemaName);
        
        if (schema == null)
        {
            _logger.LogWarning($"Schema {schemaName} not found");
            return true; // Allow if no schema
        }
        
        return data.IsValid(schema, out IList<string> errors);
    }
    
    // Satellite content
    public async Task<SatelliteJson?> GetSatelliteContentAsync(string name)
    {
        var cacheKey = $"satellite:{name}";
        
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            var satellite = cached as SatelliteJson;
            
            // Check if refresh needed
            if (satellite?.RefreshInterval.HasValue == true &&
                DateTime.UtcNow - satellite.LastLoaded > satellite.RefreshInterval.Value)
            {
                return await LoadSatelliteContent(name);
            }
            
            return satellite;
        }
        
        return await LoadSatelliteContent(name);
    }
    
    private async Task<SatelliteJson?> LoadSatelliteContent(string name)
    {
        var configPath = Path.Combine(_options.ContentPath, "satellites", $"{name}.config.json");
        
        if (!File.Exists(configPath))
        {
            return null;
        }
        
        try
        {
            var configJson = await File.ReadAllTextAsync(configPath);
            var config = JsonConvert.DeserializeObject<SatelliteConfig>(configJson);
            
            var contentPath = Path.IsPathRooted(config.FilePath) 
                ? config.FilePath 
                : Path.Combine(_options.ContentPath, config.FilePath);
                
            if (!File.Exists(contentPath))
            {
                _logger.LogWarning($"Satellite content file not found: {contentPath}");
                return null;
            }
            
            var contentJson = await File.ReadAllTextAsync(contentPath);
            var content = JObject.Parse(contentJson);
            
            var satellite = new SatelliteJson
            {
                Name = name,
                FilePath = config.FilePath,
                Content = content,
                LastLoaded = DateTime.UtcNow,
                FileSize = new FileInfo(contentPath).Length,
                ContentHash = ComputeHash(contentJson),
                WatchForChanges = config.WatchForChanges,
                RefreshInterval = config.RefreshInterval,
                CacheContent = config.CacheContent,
                SchemaFile = config.SchemaFile,
                ValidateOnLoad = config.ValidateOnLoad
            };
            
            // Validate if configured
            if (satellite.ValidateOnLoad && !string.IsNullOrEmpty(satellite.SchemaFile))
            {
                var isValid = await ValidateAgainstSchemaAsync(satellite.SchemaFile, content);
                if (!isValid)
                {
                    _logger.LogError($"Satellite content {name} failed schema validation");
                }
            }
            
            if (satellite.CacheContent)
            {
                _cache.TryAdd($"satellite:{name}", satellite);
            }
            
            return satellite;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load satellite content {name}");
            return null;
        }
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Clear cache for changed file
        var fileName = Path.GetFileNameWithoutExtension(e.Name);
        var keysToRemove = _cache.Keys
            .Where(k => k.Contains(fileName))
            .ToList();
            
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
        
        // Raise event
        ContentChanged?.Invoke(this, new ContentChangedEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = ContentChangeType.FileChanged
        });
    }
    
    public event EventHandler<ContentChangedEventArgs>? ContentChanged;
}
```

### Content Examples

#### Setting Up Content Structure
```csharp
public class ContentSetupService
{
    // Initialize content structure
    public async Task InitializeContentStructure()
    {
        // Create form templates
        await CreateFormTemplates();
        
        // Create work item templates
        await CreateWorkItemTemplates();
        
        // Create table templates
        await CreateTableTemplates();
        
        // Create notification templates
        await CreateNotificationTemplates();
        
        // Create report templates
        await CreateReportTemplates();
    }
    
    private async Task CreateFormTemplates()
    {
        // Customer feedback form
        var feedbackForm = new FormTemplate
        {
            Name = "CustomerFeedback",
            Title = "Customer Feedback Form",
            Tags = new List<string> { "feedback", "customer", "survey" },
            
            JsonSchema = JObject.FromObject(new
            {
                type = "object",
                properties = new
                {
                    customerName = new
                    {
                        type = "string",
                        title = "Customer Name",
                        minLength = 2,
                        maxLength = 100
                    },
                    email = new
                    {
                        type = "string",
                        title = "Email Address",
                        format = "email"
                    },
                    rating = new
                    {
                        type = "integer",
                        title = "Overall Rating",
                        minimum = 1,
                        maximum = 5
                    },
                    comments = new
                    {
                        type = "string",
                        title = "Comments",
                        maxLength = 1000
                    },
                    category = new
                    {
                        type = "string",
                        title = "Feedback Category",
                        @enum = new[] { "Product", "Service", "Support", "Other" }
                    }
                },
                required = new[] { "customerName", "email", "rating" }
            }),
            
            UISchema = JObject.FromObject(new
            {
                customerName = new { "ui:placeholder" = "Enter customer name" },
                email = new { "ui:placeholder" = "customer@example.com" },
                rating = new { "ui:widget" = "radio" },
                comments = new { "ui:widget" = "textarea", "ui:options" = new { rows = 4 } },
                category = new { "ui:widget" = "select" }
            }),
            
            // Form behavior
            AllowUserEdit = true,
            AllowComments = true,
            AllowFileAttachments = true,
            IsVisibleToUsers = true,
            
            // Actions
            Actions = new List<ActionButton>
            {
                new ActionButton
                {
                    Name = "submit",
                    Title = "Submit Feedback",
                    ButtonType = ActionButtonType.Submit,
                    Style = "primary"
                },
                new ActionButton
                {
                    Name = "draft",
                    Title = "Save as Draft",
                    ButtonType = ActionButtonType.Custom,
                    Style = "secondary"
                }
            }
        };
        
        await _content.SaveContentTypeAsync(feedbackForm);
        
        // Contact form
        var contactForm = new FormTemplate
        {
            Name = "ContactForm",
            Title = "Contact Us",
            Tags = new List<string> { "contact", "inquiry" },
            
            JsonSchema = JObject.FromObject(new
            {
                type = "object",
                properties = new
                {
                    firstName = new { type = "string", title = "First Name" },
                    lastName = new { type = "string", title = "Last Name" },
                    email = new { type = "string", title = "Email", format = "email" },
                    phone = new { type = "string", title = "Phone Number" },
                    subject = new { type = "string", title = "Subject" },
                    message = new { type = "string", title = "Message" },
                    urgency = new
                    {
                        type = "string",
                        title = "Urgency",
                        @enum = new[] { "Low", "Medium", "High", "Urgent" },
                        @default = "Medium"
                    }
                },
                required = new[] { "firstName", "lastName", "email", "subject", "message" }
            })
        };
        
        await _content.SaveContentTypeAsync(contactForm);
    }
    
    private async Task CreateWorkItemTemplates()
    {
        // Support ticket template
        var supportTicket = new WorkItemTemplate
        {
            Name = "SupportTicket",
            Title = "Support Ticket",
            Tags = new List<string> { "support", "ticket", "helpdesk" },
            
            // Status configuration
            StatusTemplates = new List<StatusTemplate>
            {
                new StatusTemplate { Value = 0, Name = "New", Color = "#blue", IsResolved = false },
                new StatusTemplate { Value = 1, Name = "In Progress", Color = "#yellow", IsResolved = false },
                new StatusTemplate { Value = 2, Name = "Waiting for Customer", Color = "#orange", IsResolved = false },
                new StatusTemplate { Value = 3, Name = "Resolved", Color = "#green", IsResolved = true },
                new StatusTemplate { Value = 4, Name = "Closed", Color = "#gray", IsResolved = true }
            },
            
            // Priority configuration
            PriorityTemplates = new List<PriorityTemplate>
            {
                new PriorityTemplate { Value = 1, Name = "Low", Color = "#gray" },
                new PriorityTemplate { Value = 2, Name = "Medium", Color = "#blue" },
                new PriorityTemplate { Value = 3, Name = "High", Color = "#orange" },
                new PriorityTemplate { Value = 4, Name = "Critical", Color = "#red" }
            },
            
            // Sections
            SectionTemplates = new List<SectionTemplate>
            {
                new SectionTemplate
                {
                    Name = "CustomerInfo",
                    Title = "Customer Information",
                    EntityTypes = new[] { "Form" },
                    AllowedTemplates = new[] { "CustomerContactForm" },
                    IsRequired = true
                },
                new SectionTemplate
                {
                    Name = "IssueDetails",
                    Title = "Issue Details",
                    EntityTypes = new[] { "Form" },
                    AllowedTemplates = new[] { "IssueDescriptionForm" },
                    IsRequired = true
                },
                new SectionTemplate
                {
                    Name = "Resolution",
                    Title = "Resolution",
                    EntityTypes = new[] { "Form" },
                    AllowedTemplates = new[] { "ResolutionForm" },
                    IsRequired = false
                }
            },
            
            // Permissions
            AllowUserEdit = true,
            AllowUserAddMembers = false,
            AllowComments = true,
            AllowFileAttachments = true,
            IsVisibleToUsers = true
        };
        
        await _content.SaveContentTypeAsync(supportTicket);
    }
}
```

#### Creating a Content Domain System
```csharp
public class ContentDomainService
{
    // Create application settings domain
    public async Task<ContentDomain> CreateApplicationSettingsDomain()
    {
        var domain = new ContentDomain
        {
            Name = "ApplicationSettings",
            Title = "Application Settings",
            Description = "Global application configuration and settings",
            Tags = new List<string> { "settings", "configuration", "admin" },
            
            Elements = new List<ContentElement>
            {
                new ContentElement
                {
                    Key = "app.name",
                    Title = "Application Name",
                    Type = ContentElementType.String,
                    Value = JToken.FromObject("BForm Platform"),
                    IsRequired = true
                },
                new ContentElement
                {
                    Key = "app.version",
                    Title = "Application Version",
                    Type = ContentElementType.String,
                    Value = JToken.FromObject("1.0.0")
                },
                new ContentElement
                {
                    Key = "features.enableComments",
                    Title = "Enable Comments",
                    Type = ContentElementType.Boolean,
                    Value = JToken.FromObject(true)
                },
                new ContentElement
                {
                    Key = "features.enableFileAttachments",
                    Title = "Enable File Attachments",
                    Type = ContentElementType.Boolean,
                    Value = JToken.FromObject(true)
                },
                new ContentElement
                {
                    Key = "limits.maxFileSize",
                    Title = "Maximum File Size (MB)",
                    Type = ContentElementType.Number,
                    Value = JToken.FromObject(50)
                },
                new ContentElement
                {
                    Key = "security.requireEmailVerification",
                    Title = "Require Email Verification",
                    Type = ContentElementType.Boolean,
                    Value = JToken.FromObject(true)
                },
                new ContentElement
                {
                    Key = "ui.theme",
                    Title = "UI Theme",
                    Type = ContentElementType.Object,
                    Value = JObject.FromObject(new
                    {
                        primaryColor = "#007bff",
                        secondaryColor = "#6c757d",
                        successColor = "#28a745",
                        warningColor = "#ffc107",
                        errorColor = "#dc3545"
                    })
                }
            },
            
            SchemaName = "ApplicationSettingsSchema",
            ValidateContent = true,
            AllowedRoles = new List<string> { "Administrator" },
            IsPublic = false,
            Version = "1.0",
            LastModified = DateTime.UtcNow
        };
        
        await _content.SaveContentDomainAsync(domain);
        return domain;
    }
    
    // Create localization content domain
    public async Task CreateLocalizationDomain()
    {
        var uiStringsDomain = new ContentDomain
        {
            Name = "UIStrings",
            Title = "User Interface Strings",
            Description = "Localized UI text and messages",
            Tags = new List<string> { "localization", "ui", "strings" },
            
            Elements = new List<ContentElement>
            {
                new ContentElement
                {
                    Key = "common.save",
                    Title = "Save Button Text",
                    Type = ContentElementType.String,
                    Value = JToken.FromObject("Save"),
                    Localizations = new Dictionary<string, JToken>
                    {
                        ["es"] = JToken.FromObject("Guardar"),
                        ["fr"] = JToken.FromObject("Enregistrer"),
                        ["de"] = JToken.FromObject("Speichern")
                    }
                },
                new ContentElement
                {
                    Key = "common.cancel",
                    Title = "Cancel Button Text",
                    Type = ContentElementType.String,
                    Value = JToken.FromObject("Cancel"),
                    Localizations = new Dictionary<string, JToken>
                    {
                        ["es"] = JToken.FromObject("Cancelar"),
                        ["fr"] = JToken.FromObject("Annuler"),
                        ["de"] = JToken.FromObject("Abbrechen")
                    }
                },
                new ContentElement
                {
                    Key = "validation.required",
                    Title = "Required Field Message",
                    Type = ContentElementType.String,
                    Value = JToken.FromObject("This field is required"),
                    Localizations = new Dictionary<string, JToken>
                    {
                        ["es"] = JToken.FromObject("Este campo es obligatorio"),
                        ["fr"] = JToken.FromObject("Ce champ est obligatoire"),
                        ["de"] = JToken.FromObject("Dieses Feld ist erforderlich")
                    }
                }
            }
        };
        
        await _content.SaveContentDomainAsync(uiStringsDomain);
    }
}
```

### Best Practices

1. **Structure content logically** - Organize templates and content into clear hierarchies
2. **Use JSON schemas** - Validate content structure and data integrity
3. **Implement version control** - Track changes to content over time
4. **Cache frequently accessed content** - Improve performance with smart caching
5. **Support localization** - Enable multi-language applications
6. **Separate concerns** - Keep configuration separate from business logic
7. **Validate content changes** - Ensure data quality and consistency
8. **Document content structure** - Provide clear guidance for content creators
9. **Use file watching** - Enable hot-reload for development
10. **Implement access controls** - Secure sensitive configuration data

---

## Rules Engine

The Rules Engine provides sophisticated event-driven automation through configurable business rules. It supports complex condition evaluation, action chaining, and plugin-based extensibility for custom behaviors.

### Core Components

#### Rule
Defines a complete business rule with conditions and actions:

```csharp
public class Rule : IContentType
{
    public string Name { get; set; }                // Unique identifier
    public string Title { get; set; }               // Display title
    public List<string> Tags { get; set; } = new(); // Categorization
    
    // Trigger configuration
    public List<string> TopicBindings { get; set; } = new(); // Event topics
    public List<RuleCondition> Conditions { get; set; } = new(); // When to execute
    public RuleLogicOperator ConditionLogic { get; set; } = RuleLogicOperator.And;
    
    // Actions
    public List<RuleAction> Actions { get; set; } = new(); // What to do
    public bool StopOnFirstFailure { get; set; } = false; // Error handling
    
    // Execution control
    public bool IsEnabled { get; set; } = true;    // Rule active
    public int Priority { get; set; } = 100;       // Execution order
    public TimeSpan? Timeout { get; set; }         // Max execution time
    
    // Rate limiting
    public TimeSpan? CooldownPeriod { get; set; }  // Minimum time between executions
    public int? MaxExecutionsPerHour { get; set; } // Rate limit
    
    // Scheduling
    public DateTime? ActiveFrom { get; set; }      // Activation date
    public DateTime? ActiveUntil { get; set; }     // Deactivation date
    public List<string> ActiveDays { get; set; } = new(); // Days of week
    public TimeSpan? ActiveStartTime { get; set; } // Daily start time
    public TimeSpan? ActiveEndTime { get; set; }   // Daily end time
    
    // Context
    public List<string> RequiredRoles { get; set; } = new(); // User roles
    public List<string> RequiredTags { get; set; } = new(); // Entity tags
    public JObject? Metadata { get; set; }         // Additional data
}

public enum RuleLogicOperator
{
    And = 0,    // All conditions must be true
    Or = 1      // Any condition must be true
}
```

#### RuleCondition
Defines when a rule should execute:

```csharp
public class RuleCondition
{
    public string? Query { get; set; }              // JSONPath query
    public RuleConditionCheck Check { get; set; }   // Comparison type
    public JToken? Value { get; set; }              // Expected value
    public bool Negate { get; set; }               // Invert result
    
    // Advanced comparisons
    public List<JToken>? Values { get; set; }      // Multiple values
    public double? NumericTolerance { get; set; }   // Floating point tolerance
    public bool CaseSensitive { get; set; } = true; // String comparison
    
    // Contextual conditions
    public string? EntityType { get; set; }        // Entity type filter
    public string? UserRole { get; set; }          // User role requirement
    public List<string>? RequiredTags { get; set; } // Tag requirements
}

public enum RuleConditionCheck
{
    Exists = 0,         // Value exists
    NotExists = 1,      // Value doesn't exist
    Equals = 2,         // Value equals
    NotEquals = 3,      // Value not equals
    GreaterThan = 4,    // Numeric comparison
    LessThan = 5,       // Numeric comparison
    GreaterOrEqual = 6, // Numeric comparison
    LessOrEqual = 7,    // Numeric comparison
    Contains = 8,       // String/array contains
    NotContains = 9,    // String/array doesn't contain
    StartsWith = 10,    // String starts with
    EndsWith = 11,      // String ends with
    Matches = 12,       // Regex match
    In = 13,           // Value in list
    NotIn = 14,        // Value not in list
    Any = 15,          // Any array element matches
    All = 16,          // All array elements match
    Count = 17,        // Array count
    Empty = 18,        // Array/string is empty
    NotEmpty = 19      // Array/string not empty
}
```

#### RuleAction
Base class for rule actions:

```csharp
public class RuleAction
{
    public RuleExpressionInvocation Invocation { get; set; } // Action to execute
    public int Priority { get; set; } = 100;     // Execution order
    public bool ContinueOnError { get; set; } = true; // Error handling
    public TimeSpan? Timeout { get; set; }      // Max execution time
    public List<RuleCondition>? Conditions { get; set; } // Action-specific conditions
}

public class RuleExpressionInvocation
{
    public string Name { get; set; }            // Action name
    public JObject Args { get; set; }           // Action arguments
    public string? ResultProperty { get; set; } // Where to store result
    public bool Required { get; set; } = true;  // Fail if action fails
}
```

### Rule Services

#### RuleEngine
Core rule processing engine:

```csharp
public class RuleEngine : IAppEventConsumer
{
    private readonly IApplicationPlatformContent _content;
    private readonly TopicRegistrations _topicRegistrations;
    private readonly RuleEvaluator _evaluator;
    private readonly ILogger<RuleEngine> _logger;
    
    public void ConsumeEvents(AppEvent appEvent)
    {
        _ = Task.Run(async () => await ProcessEventAsync(appEvent));
    }
    
    private async Task ProcessEventAsync(AppEvent appEvent)
    {
        try
        {
            // Get rules for this topic
            var rules = await GetRulesForTopic(appEvent.Topic);
            
            if (!rules.Any())
            {
                return;
            }
            
            // Process rules by priority
            var sortedRules = rules
                .Where(r => r.IsEnabled && IsRuleActive(r))
                .OrderBy(r => r.Priority)
                .ToList();
                
            foreach (var rule in sortedRules)
            {
                try
                {
                    // Check rate limiting
                    if (!await CheckRateLimit(rule, appEvent))
                    {
                        continue;
                    }
                    
                    // Evaluate rule
                    await _evaluator.EvaluateRuleAsync(rule, appEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to evaluate rule {rule.Name} for event {appEvent.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process event {appEvent.Id} in rule engine");
        }
    }
    
    private async Task<List<Rule>> GetRulesForTopic(string? topic)
    {
        if (string.IsNullOrEmpty(topic))
        {
            return new List<Rule>();
        }
        
        var allRules = await _content.GetAllContentTypesAsync<Rule>();
        
        return allRules
            .Where(r => r.TopicBindings.Any(binding => IsTopicMatch(topic, binding)))
            .ToList();
    }
    
    private bool IsTopicMatch(string eventTopic, string ruleBinding)
    {
        // Support wildcards in rule bindings
        if (ruleBinding.Contains("*"))
        {
            var pattern = ruleBinding.Replace("*", ".*");
            return Regex.IsMatch(eventTopic, pattern);
        }
        
        return eventTopic.Equals(ruleBinding, StringComparison.OrdinalIgnoreCase);
    }
    
    private bool IsRuleActive(Rule rule)
    {
        var now = DateTime.UtcNow;
        
        // Check date range
        if (rule.ActiveFrom.HasValue && now < rule.ActiveFrom.Value)
        {
            return false;
        }
        
        if (rule.ActiveUntil.HasValue && now > rule.ActiveUntil.Value)
        {
            return false;
        }
        
        // Check day of week
        if (rule.ActiveDays.Any())
        {
            var currentDay = now.DayOfWeek.ToString();
            if (!rule.ActiveDays.Contains(currentDay))
            {
                return false;
            }
        }
        
        // Check time of day
        if (rule.ActiveStartTime.HasValue || rule.ActiveEndTime.HasValue)
        {
            var currentTime = now.TimeOfDay;
            
            if (rule.ActiveStartTime.HasValue && currentTime < rule.ActiveStartTime.Value)
            {
                return false;
            }
            
            if (rule.ActiveEndTime.HasValue && currentTime > rule.ActiveEndTime.Value)
            {
                return false;
            }
        }
        
        return true;
    }
}
```
