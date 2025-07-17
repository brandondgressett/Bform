using System.Collections.Generic;

namespace BFormDomain.Tools.ContentGenerator.Models
{
    public class ContentGenerationResponse
    {
        public bool Success { get; set; }
        public string GeneratedContent { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new();
        public int RetryCount { get; set; }
    }
}