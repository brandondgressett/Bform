using System.Threading;
using System.Threading.Tasks;

namespace BFormDomain.Tools.ContentGenerator.Abstractions
{
    public interface ILlmProvider
    {
        Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default);
        string Name { get; }
    }
}