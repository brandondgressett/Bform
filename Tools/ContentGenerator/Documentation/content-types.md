# BForm Content Types Documentation

## Form Templates

Forms are the core building blocks for data collection in BForm. They use JSON Schema for validation and support dynamic UI rendering.

### Key Properties:
- `name`: Unique identifier for the form
- `title`: Display name
- `contentSchema`: JSON Schema defining data structure and validation
- `uiSchema`: UI rendering hints (widgets, layout)
- `actionButtons`: Custom actions that trigger events
- `tags`: Categorization tags
- `schedules`: Optional scheduling configuration

### Common Use Cases:
- Customer registration forms
- Survey/feedback forms
- Application forms
- Configuration forms

## Work Item Templates

Work items represent tasks, tickets, or cases that flow through defined processes.

### Key Properties:
- `name`: Unique identifier
- `title`: Display name
- `statusTemplates`: Workflow states (e.g., New, In Progress, Done)
- `priorityTemplates`: Priority levels
- `triageTemplates`: Triage/severity levels
- `sectionTemplates`: Field groupings within the work item
- `defaultFormId`: Form to use for data collection

### Common Use Cases:
- Bug tracking
- Help desk tickets
- Change requests
- Project tasks

## Work Set Templates

Work sets are dashboards that organize and display collections of work items.

### Key Properties:
- `name`: Unique identifier
- `title`: Display name
- `layout`: Dashboard layout configuration
- `filters`: Default filtering options
- `views`: Different view configurations
- `permissions`: Role-based access control

### Common Use Cases:
- Team dashboards
- Project boards
- Management views
- Customer portals

## Table Templates

Tables provide flexible data storage with dynamic schemas.

### Key Properties:
- `name`: Unique identifier
- `title`: Display name
- `columns`: Column definitions with data types
- `indexes`: Performance optimization
- `validation`: Data validation rules
- `displayOptions`: How data is presented

### Common Use Cases:
- Product catalogs
- Customer lists
- Inventory tracking
- Reference data

## KPI Templates

KPIs track business metrics with calculations and thresholds.

### Key Properties:
- `name`: Unique identifier
- `title`: Display name
- `calculation`: Formula or aggregation logic
- `dataSource`: Where to pull data from
- `thresholds`: Red/yellow/green levels
- `aggregationPeriod`: Time window for calculations
- `displayFormat`: How to show the metric

### Common Use Cases:
- Customer satisfaction scores
- Revenue metrics
- Performance indicators
- Quality metrics

## Report Templates

Reports combine data from multiple sources into formatted outputs.

### Key Properties:
- `name`: Unique identifier
- `title`: Display name
- `sections`: Report layout sections
- `dataSources`: Tables/queries to pull from
- `parameters`: User-configurable filters
- `format`: Output format (PDF, Excel, etc.)
- `schedule`: Automated generation schedule

### Common Use Cases:
- Monthly summaries
- Executive dashboards
- Compliance reports
- Analytics reports

## Business Rules

Rules automate processes by triggering actions based on events.

### Key Properties:
- `name`: Unique identifier
- `eventType`: What triggers the rule
- `conditions`: When to execute
- `actions`: What to do (40+ built-in actions)
- `priority`: Execution order
- `enabled`: Active/inactive state

### Common Use Cases:
- Email notifications
- Data updates
- Workflow automation
- Integration triggers

## HTML Templates

HTML templates store reusable content with dynamic placeholders.

### Key Properties:
- `name`: Unique identifier
- `title`: Display name
- `content`: HTML with placeholders
- `tokens`: Dynamic replacement values
- `styles`: CSS styling
- `scripts`: Optional JavaScript

### Common Use Cases:
- Email templates
- Report headers/footers
- Landing pages
- Notifications

## Scheduled Events

Scheduled events trigger actions at specified times.

### Key Properties:
- `name`: Unique identifier
- `cronExpression`: When to run (cron format)
- `eventType`: What event to trigger
- `payload`: Data to include
- `timezone`: Execution timezone
- `enabled`: Active/inactive

### Common Use Cases:
- Daily reports
- Data cleanup
- Reminder notifications
- Batch processing

## Table Queries

Saved queries provide reusable data retrieval patterns.

### Key Properties:
- `name`: Unique identifier
- `tableName`: Source table
- `filter`: Query conditions
- `projection`: Fields to return
- `sort`: Ordering
- `limit`: Result count

### Common Use Cases:
- Frequently used filters
- Complex queries
- API endpoints
- Report data sources

## Table Summarizations

Summarizations aggregate table data for analytics.

### Key Properties:
- `name`: Unique identifier
- `tableName`: Source table
- `groupBy`: Grouping fields
- `aggregations`: Sum, count, avg, etc.
- `filters`: Pre-aggregation filtering
- `schedule`: When to recalculate

### Common Use Cases:
- Sales summaries
- Usage statistics
- Performance metrics
- Trend analysis