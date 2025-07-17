using System.Collections.Generic;

namespace BFormDomain.Tools.ContentGenerator.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
    }

    public class ValidationError
    {
        public string Path { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string SchemaPath { get; set; } = string.Empty;
    }
}