using BFormDomain.CommonCode.Authorization;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetMemberViewModel
{
    public string WorkSetTitle { get; set; } = null!;
    public string UserName { get; set; } = null!;
    


    public static async Task<WorkSetMemberViewModel> Create(
        WorkSetMember member, 
        IRepository<WorkSet> worksets, 
        UserInformationCache cache, 
        IApplicationAlert alerts)
    {
        WorkSetMemberViewModel retval = null!;
        try
        {
            var (ws, _) = await worksets.LoadAsync(member.WorkSetId);
            ws.Guarantees().IsNotNull();
            var ui = await cache.Fetch(member.UserId)!;
            ui.Guarantees().IsNotNull();

            retval = new WorkSetMemberViewModel
            {
                UserName = ui!.UserName,
                WorkSetTitle = ws.Title
            };
        } catch(Exception ex)
        {
            alerts.RaiseAlert(ApplicationAlertKind.General,
                Microsoft.Extensions.Logging.LogLevel.Information,
                ex.TraceInformation());
        }

        return retval;
    }
}
