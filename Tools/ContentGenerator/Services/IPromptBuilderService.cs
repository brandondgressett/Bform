using System.Threading.Tasks;
using BFormDomain.Tools.ContentGenerator.Models;

namespace BFormDomain.Tools.ContentGenerator.Services
{
    public interface IPromptBuilderService
    {
        Task<string> BuildPromptAsync(ContentGenerationRequest request);
        Task<string> BuildRetryPromptAsync(ContentGenerationRequest request, string previousAttempt, ValidationResult validationResult);
    }
}