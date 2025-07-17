using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Tables;

/// <summary>
/// TableEntityLoaderModule implements CanLoad and LoadJson from IEntityLoaderModule to manage loading table entities from JSON
///     -Usage
///         >EntityReferenceLoader.cs
///         >WorkItemLoaderModule.cs
///     -Functions
///         >CanLoad
///         >LoadJson
/// </summary>
public class TableEntityLoaderModule : IEntityLoaderModule
{
    private readonly TableLogic _logic;

    public TableEntityLoaderModule(TableLogic logic)
    {
        _logic = logic;
    }

    public bool CanLoad(string uri)
    {
        var res = new Uri(uri);
        var host = res.Host.ToLowerInvariant();
        return host == nameof(TableTemplate).ToLowerInvariant();
    }

    /// <summary>
    /// Types of loads:
    ///     table template.
    ///     table query command.
    ///         -needs table template
    ///         -needs a query command. 
    ///             -this is too large to fit into the query string.
    ///             -therefore, refer to a registered table query via name
    ///         -optionally a page.
    ///     table summarization.
    ///         -needs all query command needs plus
    ///         -needs a summarization command.
    ///            
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="tzid"></param>
    /// <returns></returns>
    public async Task<JObject?> LoadJson(string uri, string? tzid = null)
    {
        var res = new Uri(uri);
        JObject? retval = null!;
        
        bool wantsTemplate = res.Segments.Any(it => it.ToLowerInvariant() == "template");
        var templateName = res.Segments.Last();
        var queryParameters = res.ParseQueryString();

        if(wantsTemplate)
        {
            var tableTemplate = _logic.GetTemplate(templateName)!;
            tableTemplate.Guarantees().IsNotNull();
            retval = JObject.FromObject(tableTemplate);

        }

        if(queryParameters.ContainsKey("query") && 
           !queryParameters.ContainsKey("summary"))
        {
            TableViewModel result = null!;
            
            if(queryParameters.ContainsKey("page"))
            {
                result = await _logic.RegisteredQueryDataTablePage(
                    templateName,
                    queryParameters["query"],
                    int.Parse(queryParameters["page"]));
                    
            } else
            {
                result = await _logic.RegisteredQueryDataTableAll(
                    templateName,
                    queryParameters["query"]);
            }

            result.Guarantees().IsNotNull();
            retval = JObject.FromObject(result);
            
        } else 
        if(queryParameters.ContainsKey("query") && 
           queryParameters.ContainsKey("summary"))
        {
            var result = await _logic.RegisteredDataTableSummary(
                templateName,
                queryParameters["query"],
                queryParameters["summary"]);

            result.Guarantees().IsNotNull();
            retval = JObject.FromObject(result);
        }

        retval.Guarantees().IsNotNull();
        return retval!;

    }
}
