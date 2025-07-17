namespace BFormDomain.Tools.ContentGenerator.Providers
{
    public class GeminiConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-pro";
        public double Temperature { get; set; } = 0.7;
        public int MaxOutputTokens { get; set; } = 8192;
    }
}