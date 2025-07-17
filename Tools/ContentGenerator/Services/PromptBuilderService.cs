using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BFormDomain.Tools.ContentGenerator.Models;

namespace BFormDomain.Tools.ContentGenerator.Services
{
    public class PromptBuilderService : IPromptBuilderService
    {
        private readonly ISchemaValidationService _schemaValidationService;
        private readonly ILogger<PromptBuilderService> _logger;

        private readonly Dictionary<BFormContentType, string> _contentTypeDescriptions = new()
        {
            { BFormContentType.Form, "a dynamic form with JSON schema validation, UI layout, and optional action buttons" },
            { BFormContentType.WorkItem, "a work item (ticket/task) template with customizable status, priority, triage levels, and sections" },
            { BFormContentType.WorkSet, "a work set (dashboard) that contains collections of work items with layout and filtering options" },
            { BFormContentType.Table, "a dynamic table template with column definitions, data types, and display properties" },
            { BFormContentType.KPI, "a key performance indicator with calculation rules, thresholds, and display format" },
            { BFormContentType.Report, "a report template with data sources, layout, and formatting options" },
            { BFormContentType.Html, "an HTML content template with dynamic placeholders and rendering options" },
            { BFormContentType.ScheduledEvent, "a scheduled event for automated tasks with cron expressions and event configurations" },
            { BFormContentType.RegisteredTableQuery, "a saved query definition for table data with filters and projections" },
            { BFormContentType.RegisteredTableSummarization, "a table summarization configuration for aggregating and grouping data" },
            { BFormContentType.Rule, "a business rule that triggers actions based on events with conditions and action configurations" }
        };

        public PromptBuilderService(ISchemaValidationService schemaValidationService, ILogger<PromptBuilderService> logger)
        {
            _schemaValidationService = schemaValidationService;
            _logger = logger;
        }

        public async Task<string> BuildPromptAsync(ContentGenerationRequest request)
        {
            var promptBuilder = new StringBuilder();

            // System context
            promptBuilder.AppendLine("You are a BForm content generation assistant. BForm is an enterprise business application framework that uses JSON schemas to define dynamic forms, workflows, and business entities.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"You need to generate {_contentTypeDescriptions[request.ContentType]}.");
            promptBuilder.AppendLine();

            // Include the JSON schema
            var schema = await _schemaValidationService.GetSchemaForContentTypeAsync(request.ContentType);
            if (!string.IsNullOrEmpty(schema))
            {
                promptBuilder.AppendLine("The generated JSON must conform to this JSON Schema:");
                promptBuilder.AppendLine("```json");
                promptBuilder.AppendLine(schema);
                promptBuilder.AppendLine("```");
                promptBuilder.AppendLine();
            }

            // Include content-type specific guidance
            await AppendContentTypeGuidanceAsync(promptBuilder, request.ContentType);

            // Include examples if available
            await AppendExamplesAsync(promptBuilder, request.ContentType);

            // Add user's specific request
            promptBuilder.AppendLine("User's request:");
            promptBuilder.AppendLine(request.UserPrompt);
            promptBuilder.AppendLine();

            // Instructions for output
            promptBuilder.AppendLine("Generate valid JSON that:");
            promptBuilder.AppendLine("1. Satisfies the user's request");
            promptBuilder.AppendLine("2. Conforms to the provided JSON schema");
            promptBuilder.AppendLine("3. Includes meaningful values based on the context");
            promptBuilder.AppendLine("4. Uses proper naming conventions (camelCase for properties)");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Return ONLY the JSON content, without any markdown formatting or explanations.");

            return promptBuilder.ToString();
        }

        public async Task<string> BuildRetryPromptAsync(ContentGenerationRequest request, string previousAttempt, ValidationResult validationResult)
        {
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine("The previous JSON generation had validation errors. Please fix the following issues:");
            promptBuilder.AppendLine();

            foreach (var error in validationResult.Errors)
            {
                promptBuilder.AppendLine($"- At path '{error.Path}': {error.Message}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Previous attempt:");
            promptBuilder.AppendLine("```json");
            promptBuilder.AppendLine(previousAttempt);
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();

            // Re-include the schema for reference
            var schema = await _schemaValidationService.GetSchemaForContentTypeAsync(request.ContentType);
            if (!string.IsNullOrEmpty(schema))
            {
                promptBuilder.AppendLine("JSON Schema for reference:");
                promptBuilder.AppendLine("```json");
                promptBuilder.AppendLine(schema);
                promptBuilder.AppendLine("```");
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine("Please generate corrected JSON that fixes all validation errors.");
            promptBuilder.AppendLine("Return ONLY the JSON content, without any markdown formatting or explanations.");

            return promptBuilder.ToString();
        }

        private async Task AppendContentTypeGuidanceAsync(StringBuilder promptBuilder, BFormContentType contentType)
        {
            promptBuilder.AppendLine($"Guidance for {contentType}:");

            switch (contentType)
            {
                case BFormContentType.Form:
                    promptBuilder.AppendLine("- Forms require both contentSchema (JSON Schema) and uiSchema (UI hints)");
                    promptBuilder.AppendLine("- The contentSchema defines data validation rules");
                    promptBuilder.AppendLine("- The uiSchema defines how fields are rendered (widgets, layout)");
                    promptBuilder.AppendLine("- Action buttons can trigger custom events through the rule engine");
                    promptBuilder.AppendLine("- Forms can be associated with WorkItems or WorkSets");
                    break;

                case BFormContentType.WorkItem:
                    promptBuilder.AppendLine("- Work items represent tasks, tickets, or cases");
                    promptBuilder.AppendLine("- Must define status templates (workflow states)");
                    promptBuilder.AppendLine("- Can have priority and triage levels");
                    promptBuilder.AppendLine("- Sections organize fields within the work item");
                    promptBuilder.AppendLine("- Supports custom forms per status");
                    break;

                case BFormContentType.WorkSet:
                    promptBuilder.AppendLine("- Work sets are dashboards containing collections of work items");
                    promptBuilder.AppendLine("- Define layout and filtering options");
                    promptBuilder.AppendLine("- Can have associated forms and views");
                    promptBuilder.AppendLine("- Support role-based access control");
                    break;

                case BFormContentType.Table:
                    promptBuilder.AppendLine("- Tables store structured data with flexible schemas");
                    promptBuilder.AppendLine("- Define columns with data types and validation");
                    promptBuilder.AppendLine("- Support indexing and querying");
                    promptBuilder.AppendLine("- Can be referenced by forms and reports");
                    break;

                case BFormContentType.KPI:
                    promptBuilder.AppendLine("- KPIs track business metrics");
                    promptBuilder.AppendLine("- Define calculation formulas");
                    promptBuilder.AppendLine("- Set thresholds for alerts");
                    promptBuilder.AppendLine("- Specify aggregation periods");
                    break;

                case BFormContentType.Report:
                    promptBuilder.AppendLine("- Reports combine data from multiple sources");
                    promptBuilder.AppendLine("- Define layout with sections and charts");
                    promptBuilder.AppendLine("- Support parameters and filters");
                    promptBuilder.AppendLine("- Can be scheduled for automatic generation");
                    break;

                case BFormContentType.Rule:
                    promptBuilder.AppendLine("- Rules automate business processes");
                    promptBuilder.AppendLine("- Trigger on specific events (AppEvents)");
                    promptBuilder.AppendLine("- Define conditions and actions");
                    promptBuilder.AppendLine("- Support over 40 built-in actions");
                    promptBuilder.AppendLine("- Can chain multiple actions");
                    break;

                case BFormContentType.ScheduledEvent:
                    promptBuilder.AppendLine("- Scheduled events run at specified times");
                    promptBuilder.AppendLine("- Use cron expressions for scheduling");
                    promptBuilder.AppendLine("- Can trigger rules or custom actions");
                    promptBuilder.AppendLine("- Support timezone configuration");
                    break;
            }

            promptBuilder.AppendLine();
        }

        private async Task AppendExamplesAsync(StringBuilder promptBuilder, BFormContentType contentType)
        {
            var examplePath = Path.Combine("Examples", $"{contentType.ToString().ToLower()}-example.json");
            
            if (File.Exists(examplePath))
            {
                try
                {
                    var example = await File.ReadAllTextAsync(examplePath);
                    promptBuilder.AppendLine($"Example {contentType}:");
                    promptBuilder.AppendLine("```json");
                    promptBuilder.AppendLine(example);
                    promptBuilder.AppendLine("```");
                    promptBuilder.AppendLine();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load example for {ContentType}", contentType);
                }
            }
        }
    }
}