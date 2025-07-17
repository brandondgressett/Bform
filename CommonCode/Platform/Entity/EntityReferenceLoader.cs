using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Entity;

/// <summary>
/// EntityReferenceLoader loads JSON entity by URI
///     References:
///         >WorkItemLogic.cs
///         >WorkItemViewModel.cs
///         >WorkSetLogic.cs
///         >WorkSetViewModel.cs
///     -Functions:
///         >LoadEntityJsonFromReference
/// </summary>
public class EntityReferenceLoader
{
    private readonly List<IEntityLoaderModule> _loaders;
    private readonly IApplicationAlert _alerts;

    public EntityReferenceLoader(IEnumerable<IEntityLoaderModule> loaders,
        IApplicationAlert alerts)
    {
        _loaders = loaders.ToList();
        _alerts = alerts;
    }

    public async Task<JObject?> LoadEntityJsonFromReference(string uri)
    {
        try
        {
            var loader = _loaders.First(it => it.CanLoad(uri));
            return await loader.LoadJson(uri);

        } catch(Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Information,
                ex.TraceInformation());
            throw;
        }
    }


}
