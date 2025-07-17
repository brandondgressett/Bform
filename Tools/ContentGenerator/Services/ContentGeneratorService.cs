using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BFormDomain.Tools.ContentGenerator.Abstractions;
using BFormDomain.Tools.ContentGenerator.Models;

namespace BFormDomain.Tools.ContentGenerator.Services
{
    public class ContentGeneratorService : IContentGeneratorService
    {
        private readonly ILlmProvider _llmProvider;
        private readonly IPromptBuilderService _promptBuilder;
        private readonly ISchemaValidationService _validator;
        private readonly ILogger<ContentGeneratorService> _logger;

        private const int MAX_RETRIES = 3;

        public ContentGeneratorService(
            ILlmProvider llmProvider,
            IPromptBuilderService promptBuilder,
            ISchemaValidationService validator,
            ILogger<ContentGeneratorService> logger)
        {
            _llmProvider = llmProvider;
            _promptBuilder = promptBuilder;
            _validator = validator;
            _logger = logger;
        }

        public async Task<ContentGenerationResponse> GenerateContentAsync(ContentGenerationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating {ContentType} content using {Provider}", request.ContentType, _llmProvider.Name);

            try
            {
                // Build initial prompt
                var prompt = await _promptBuilder.BuildPromptAsync(request);
                _logger.LogDebug("Initial prompt built for {ContentType}", request.ContentType);

                // Generate content
                var generatedJson = await _llmProvider.GenerateContentAsync(prompt, cancellationToken);
                _logger.LogDebug("Initial content generated");

                // Validate
                var validation = _validator.ValidateContent(generatedJson, request.ContentType);

                // Retry loop if invalid
                int retries = 0;
                while (!validation.IsValid && retries < MAX_RETRIES)
                {
                    _logger.LogWarning("Validation failed on attempt {Attempt}, retrying...", retries + 1);
                    
                    var retryPrompt = await _promptBuilder.BuildRetryPromptAsync(request, generatedJson, validation);
                    generatedJson = await _llmProvider.GenerateContentAsync(retryPrompt, cancellationToken);
                    validation = _validator.ValidateContent(generatedJson, request.ContentType);
                    retries++;
                }

                var response = new ContentGenerationResponse
                {
                    Success = validation.IsValid,
                    GeneratedContent = generatedJson,
                    ValidationErrors = validation.Errors.Select(e => $"{e.Path}: {e.Message}").ToList(),
                    RetryCount = retries
                };

                if (response.Success)
                {
                    _logger.LogInformation("Successfully generated valid {ContentType} content after {Retries} retries", 
                        request.ContentType, retries);
                }
                else
                {
                    _logger.LogWarning("Failed to generate valid {ContentType} content after {Retries} retries. Errors: {Errors}", 
                        request.ContentType, retries, string.Join("; ", response.ValidationErrors));
                }

                return response;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Content generation cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {ContentType} content", request.ContentType);
                return new ContentGenerationResponse
                {
                    Success = false,
                    ValidationErrors = new() { $"Generation error: {ex.Message}" }
                };
            }
        }
    }
}