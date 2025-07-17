using Hangfire.Storage.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Authorization;

public static class ApplicationRoles
{
    public const string UserAdministratorRole = "ApplicationUser Administrator";
    public const string UserReaderRole = "ApplicationUser Reader";
    public const string SiteManagerRole = "Site Manager";
    public const string SiteReaderRole = "Site Reader";
    public const string WorkSetManagerRole = "Work Set Manager";
    public const string WorkItemManagerRole = "Work Item Manager";
    public const string WorkItemContributerRole = "Work Item Contributer";
    public const string WorkItemReaderRole = "Work Item Reader";

    /// <summary>
    /// Returns every role defined
    /// </summary>
    /// <returns></returns>
    public static string[] AnyRole()//Be sure to add any new roles to the array below
    {
        return new string[] { UserAdministratorRole, UserReaderRole, SiteManagerRole,
                                SiteReaderRole, SiteReaderRole , WorkSetManagerRole ,
                                WorkItemManagerRole , WorkItemContributerRole , WorkItemReaderRole };
    }
}
