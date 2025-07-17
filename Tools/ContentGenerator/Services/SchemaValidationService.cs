using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using BFormDomain.Tools.ContentGenerator.Models;

namespace BFormDomain.Tools.ContentGenerator.Services
{
    public class SchemaValidationService : ISchemaValidationService
    {
        private readonly ILogger<SchemaValidationService> _logger;
        private readonly Dictionary<BFormContentType, string> _schemaFileMap;

        public SchemaValidationService(ILogger<SchemaValidationService> logger)
        {
            _logger = logger;
            _schemaFileMap = new Dictionary<BFormContentType, string>
            {
                { BFormContentType.Form, "BForm-Schema-FormTemplate.json" },
                { BFormContentType.WorkItem, "BForm-Schema-WorkItemTemplate.json" },
                { BFormContentType.WorkSet, "BForm-Schema-WorkSetTemplate.json" },
                { BFormContentType.Table, "BForm-Schema-TableTemplate.json" },
                { BFormContentType.KPI, "BForm-Schema-KPITemplate.json" },
                { BFormContentType.Report, "BForm-Schema-ReportTemplate.json" },
                { BFormContentType.Html, "BForm-Schema-HtmlTemplate.json" },
                { BFormContentType.ScheduledEvent, "BForm-Schema-ScheduledEventTemplate.json" },
                { BFormContentType.RegisteredTableQuery, "BForm-Schema-RegisteredTableQueryTemplate.json" },
                { BFormContentType.RegisteredTableSummarization, "BForm-Schema-RegisteredTableSummarizationTemplate.json" },
                { BFormContentType.Rule, "BForm-Schema-Rule.json" }
            };
        }

        public ValidationResult ValidateContent(string jsonContent, BFormContentType contentType)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                var schemaTask = GetSchemaForContentTypeAsync(contentType);
                schemaTask.Wait();
                var schemaJson = schemaTask.Result;

                if (string.IsNullOrEmpty(schemaJson))
                {
                    _logger.LogWarning("No schema found for content type {ContentType}", contentType);
                    return result; // Consider it valid if no schema exists
                }

                var schema = JSchema.Parse(schemaJson);
                var content = JToken.Parse(jsonContent);

                IList<string> errors;
                var isValid = content.IsValid(schema, out errors);

                if (!isValid)
                {
                    result.IsValid = false;
                    result.Errors = errors.Select((e, i) => new Models.ValidationError
                    {
                        Path = "$",
                        Message = e,
                        SchemaPath = "$"
                    }).ToList();
                }
            }
            catch (JsonReaderException ex)
            {
                result.IsValid = false;
                result.Errors.Add(new Models.ValidationError
                {
                    Path = "$",
                    Message = $"Invalid JSON: {ex.Message}",
                    SchemaPath = "$"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating content for {ContentType}", contentType);
                result.IsValid = false;
                result.Errors.Add(new Models.ValidationError
                {
                    Path = "$",
                    Message = $"Validation error: {ex.Message}",
                    SchemaPath = "$"
                });
            }

            return result;
        }

        public async Task<string> GetSchemaForContentTypeAsync(BFormContentType contentType)
        {
            if (!_schemaFileMap.TryGetValue(contentType, out var schemaFileName))
            {
                _logger.LogWarning("No schema file mapped for content type {ContentType}", contentType);
                return string.Empty;
            }

            var schemaPath = Path.Combine("Schemas", schemaFileName);
            
            if (!File.Exists(schemaPath))
            {
                _logger.LogWarning("Schema file not found: {SchemaPath}", schemaPath);
                return string.Empty;
            }

            return await File.ReadAllTextAsync(schemaPath);
        }
    }
}