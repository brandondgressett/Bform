using System.Threading;
using System.Threading.Tasks;
using BFormDomain.Tools.ContentGenerator.Models;

namespace BFormDomain.Tools.ContentGenerator.Services
{
    public interface IContentGeneratorService
    {
        Task<ContentGenerationResponse> GenerateContentAsync(ContentGenerationRequest request, CancellationToken cancellationToken = default);
    }
}