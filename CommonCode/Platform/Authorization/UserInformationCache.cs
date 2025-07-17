using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Utility;
using BFormDomain.CommonCode.Utility.Caching;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BFormDomain.CommonCode.Authorization;

/// <summary>
/// UserInformationCache gets user information by user ID
///     -References:
///         >CommentsLogic.cs
///         >CommentViewModel.cs
///         >FormLogic.cs
///         >KPILogic.cs
///         >KPIViewModel.cs
///         >ManagedFileViewModel.cs
///         >ReportInstanceViewModel.cs
///         >ReportLogic.cs
///         >WorkItemLogic.cs
///         >WorkItemSummaryViewModel.cs
///         >WorkItemViewModel.cs
///         >WorkSetLogic.cs
///         >WorkSetMemberViewModel.cs
///         >WorkSetSummaryViewModel.cs
///         >WorkSetViewModel.cs
///     -Functions:
///         >Fetch
/// </summary>
public class UserInformationCache
{
    private readonly ICachedData<Guid, ApplicationUserViewModel> _cache;
    private readonly CustomUserManager _userManager;
    private readonly CustomRoleManager _roleManager;

    public UserInformationCache(CustomUserManager userManager, CustomRoleManager roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        
        _cache = new InMemoryCachedData<Guid, ApplicationUserViewModel>(maximumCacheItemsCount: 1024, expireItems:true, defaultExpireTime:TimeSpan.FromMinutes(20.0));
    }


    public async Task<ApplicationUserViewModel?> Fetch(Guid userId)
    {
        if(!_cache.MaybeGetItem(userId, out var item))
        {
            var cancel = new CancellationToken();

            var user = await _userManager.FindByIdAsync(userId.ToString(), cancel);
            if (user is null)
                item = null!;
            else
            {
                var roleNames = await _userManager.GetRolesAsync(user);

                var stringRoles = new List<string>();

                foreach(var role in roleNames)
                {
                    stringRoles.Add(role.ToString());
                }

                item = new ApplicationUserViewModel
                {
                    UserName = user.UserName ?? string.Empty,
                    TimeZoneId = user.TimeZoneId,
                    Email = user.Email ?? string.Empty,
                    RoleNames = stringRoles,
                    Tags = user.Tags ?? new List<string>()
                };

                _cache.Add(userId, item);
            }

        }

        return item;
    }





}
