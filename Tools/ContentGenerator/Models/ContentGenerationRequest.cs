using System.Collections.Generic;

namespace BFormDomain.Tools.ContentGenerator.Models
{
    public class ContentGenerationRequest
    {
        public BFormContentType ContentType { get; set; }
        public string UserPrompt { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
    }
}