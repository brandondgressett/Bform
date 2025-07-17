using System.Threading.Tasks;
using BFormDomain.Tools.ContentGenerator.Models;

namespace BFormDomain.Tools.ContentGenerator.Services
{
    public interface ISchemaValidationService
    {
        ValidationResult ValidateContent(string jsonContent, BFormContentType contentType);
        Task<string> GetSchemaForContentTypeAsync(BFormContentType contentType);
    }
}