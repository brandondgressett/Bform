using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Utility;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Reports;

public class ReportEntityLoaderModule : IEntityLoaderModule
{

    private readonly ReportLogic _logic;

    public ReportEntityLoaderModule(ReportLogic logic)
    {
        _logic = logic;
    }

    public bool CanLoad(string uri)
    {
        var res = new Uri(uri);
        var host = res.Host.ToLowerInvariant();
        return host == nameof(ReportInstance).ToLowerInvariant();
    }

    public async Task<JObject?> LoadJson(string uri, string? tzid = null)
    {
        var res = new Uri(uri);
        JObject? retval = null!;
        bool wantsTemplate = res.Segments.Any(it => it.ToLowerInvariant() == "template");
        var queryParameters = res.ParseQueryString();

        if (wantsTemplate)
        {
            var templateName = res.Segments.Last();
            var vm = _logic.GetReportTemplateVM(templateName);
            if(vm is not null)
                retval = JObject.FromObject(vm);
        } else
        {
            var id = new Guid(res.Segments.Last());
            if(queryParameters.ContainsKey("summary") && queryParameters["summary"] == "true")
            {
                var vm = await _logic.GetReportInstanceSummary(id, tzid!);
                if(vm is not null)
                    retval = JObject.FromObject(vm);
            } else
            {
                var vm = await _logic.GetReportInstance(id, tzid!);
                if (vm is not null)
                    retval = JObject.FromObject(vm);
            }

        }

        return retval;
    }
}
