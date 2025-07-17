using BFormDomain.CommonCode.Notification;
using BFormDomain.CommonCode.Platform;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BFormDomain.CommonCode.Authorization;

/// <summary>
/// UserManagementLogic manages actions that admins execute 
///     -References:
///         >Service
///     -Functions:
///         >LockoutUser
///         >EnableUser
///         >DeleteUser
///         >ResetUserPasswordAsync
///         >AddUserToRole
///         >RemoveUserFromRole
///         >TagUser
///         >UpdateUserTags
///         >UntagUser
///         >GetRoleNames
///         >FindTaggedUsers
/// </summary>
public class UserManagementLogic
{
    private readonly INotificationCore _notificationCore;
    private readonly IApplicationTerms _appTerms;
    private readonly IRepository<UserTagsDataModel> _userTags;
    private readonly AppEventSink _eventSink;
    private readonly IDataEnvironment _dataEnv;
    private readonly CustomUserManager _userManager;
    private readonly CustomRoleManager _roleManager;
    

    private static readonly string ResetSubject = "PasswordResetSubject";
    private static readonly string ResetBody = "PasswordResetBody";

    public UserManagementLogic(
        INotificationCore nCore,
        IApplicationTerms appTerms,
        IDataEnvironment dataEnv,
        IRepository<UserTagsDataModel> userTags,
        AppEventSink sink,
        CustomUserManager userManager,
        CustomRoleManager roleManager)
    {
        _notificationCore = nCore;
        _appTerms = appTerms;
        _eventSink = sink;
        _dataEnv = dataEnv;
        _userTags = userTags;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // TODO: also by user name

    /// <summary>
    /// LockoutUser allows admin to lock a user by email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task LockoutUser(string email)
    {
        var cancel = new CancellationToken();

        email.Requires().IsNotNullOrEmpty();

        var user = await _userManager.FindByEmailAsync(email, cancel);
        user.Guarantees().IsNotNull();

        var result = await _userManager.SetLockoutEnabledAsync(user!, true);
        result.Succeeded.Requires().IsTrue();

        var trx = await _dataEnv.OpenTransactionAsync(CancellationToken.None);
        try
        {
            _eventSink.BeginBatch(trx);
            var action = GuidEncoder.Encode(user!.Id);
            var userEntityWrapper = new EntityWrapping<ApplicationUser>()
            {
                Id = user.Id,
                Creator = user.Id,
                Version = 0,
            };


            await _eventSink.Enqueue(null, "users.action.lockout", action, userEntityWrapper, user.Id, null, false);

            await _eventSink.CommitBatch();
            await trx.CommitAsync();
        }
        catch (Exception)
        {
            await trx.AbortAsync();
            throw;
        }
    }

    /// <summary>
    /// EnableUser allows admin to enable a disabled user by email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task EnableUser(string email)
    {
        var cancel = new CancellationToken();

        email.Requires().IsNotNullOrEmpty();

        var user = await _userManager.FindByEmailAsync(email, cancel);
        user.Guarantees().IsNotNull();

        var result = await _userManager.SetLockoutEnabledAsync(user!, false);
        result.Succeeded.Requires().IsTrue();

        var trx = await _dataEnv.OpenTransactionAsync(CancellationToken.None);
        try
        {
            _eventSink.BeginBatch(trx);
            var action = GuidEncoder.Encode(user!.Id);
            var userEntityWrapper = new EntityWrapping<ApplicationUser>()
            {
                Id = user.Id,
                Creator = user.Id,
                Version = 0
            };


            await _eventSink.Enqueue(null, "users.action.enable", action, userEntityWrapper, user.Id, null, false);

            await _eventSink.CommitBatch();
            await trx.CommitAsync();
        }
        catch (Exception)
        {
            await trx.AbortAsync();
            throw;
        }
    }

    /// <summary>
    /// DeleteUser allows admin to delete user by email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task DeleteUser(string email)
    {
        var cancel = new CancellationToken();

        email.Requires().IsNotNullOrEmpty();

        var user = await _userManager.FindByEmailAsync(email, cancel);
        user.Guarantees().IsNotNull();

        var result = await _userManager.DeleteAsync(user!, cancel);
        result.Succeeded.Requires().IsTrue();

        var trx = await _dataEnv.OpenTransactionAsync(CancellationToken.None);

        try
        {
            _eventSink.BeginBatch(trx);
            var action = GuidEncoder.Encode(user!.Id);
            var userEntityWrapper = new EntityWrapping<ApplicationUser>()
            {
                Id = user.Id,
                Creator = user.Id,
                Version = 0,
                EntityType = nameof(ApplicationUser)
            };

           
            await _eventSink.Enqueue(null, "users.action.deleted", action, userEntityWrapper, user.Id, null, false);

            await _eventSink.CommitBatch();
            await trx.CommitAsync();
        }
        catch (Exception)
        {
            await trx.AbortAsync();
            throw;
        }
    }
    /// <summary>
    /// ResetUserPasswordAsync allows admin to reset password of user by email
    /// </summary>
    /// <param name="email"></param>
    /// <param name="newPassword"></param>
    /// <param name="sendEmail"></param>
    /// <returns></returns>
    public async Task ResetUserPasswordAsync(string email, string newPassword, bool sendEmail)
    {
        var cancel = new CancellationToken();

        newPassword.Requires().IsNotNullOrEmpty();
        newPassword.Requires().IsLongerOrEqual(8);

        email.Requires().IsNotNullOrEmpty();

        var user = await _userManager.FindByEmailAsync(email, cancel);
        user.Guarantees().IsNotNull();
        
        var token = await _userManager.GeneratePasswordResetTokenAsync(user!);

        var result = await _userManager.ResetPasswordAsync(user!, token, newPassword);
        result.Succeeded.Guarantees().IsTrue();

        if(sendEmail)
        {
            var subject = _appTerms.ApplicationTerms[ResetSubject];
            var body = _appTerms.ApplicationTerms[ResetBody];
            body = body.Replace("{password}", newPassword);
            string? htmlBody = null!;
            if (body.StartsWith(BuiltIn.BFormHtml))
            {
                htmlBody = body;
                body = null!;
            }

            await _notificationCore.SendEmail(email, user!.UserName ?? email, subject, body, htmlBody);
        }

    }

    /// <summary>
    /// AddUserToRole allows admin to asign role to user by email
    /// </summary>
    /// <param name="email"></param>
    /// <param name="roleName"></param>
    /// <returns></returns>
    public async Task AddUserToRole(string email, string roleName)
    {
        var cancel = new CancellationToken();

        var user = await _userManager.FindByEmailAsync(email, cancel);
        user.Guarantees().IsNotNull();

        var role = await _roleManager.FindByNameAsync(roleName, cancel);
        role.Guarantees().IsNotNull();

        await _userManager.AddToRoleAsync(user!, roleName);

    }

    /// <summary>
    /// RemoveUserFromRole allows admin to unassign role to user by email
    /// </summary>
    /// <param name="email"></param>
    /// <param name="roleName"></param>
    /// <returns></returns>
    public async Task RemoveUserFromRole(string email, string roleName)
    {
        var cancel = new CancellationToken();

        var user = await _userManager.FindByEmailAsync(email, cancel);
        user.Guarantees().IsNotNull();

        var role = await _roleManager.FindByNameAsync(roleName, cancel);
        role.Guarantees().IsNotNull();

        await _userManager.RemoveFromRoleAsync(user!, roleName);
        
    }


    /// <summary>
    /// TagUser allows admin to add tags to user by email
    /// </summary>
    /// <param name="email"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    public async Task TagUser(string email, IEnumerable<string> tags)
    {
        var cancel = new CancellationToken();

        var user = await _userManager.FindByEmailAsync(email, cancel);
        user.Guarantees().IsNotNull();

        var readyTags = TagUtil.MakeTags(tags);
        readyTags = readyTags.Where(tg => !user!.Tags.Contains(tg));
        user!.Tags.AddRange(readyTags);
        await UpdateUserTags(user);

        await _userManager.UpdateAsync(user!, cancel);

    }

    /// <summary>
    /// UpdateUserTags allows admin to update user tags
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    private async Task UpdateUserTags(ApplicationUser? user)
    {
        if (user is null)
            return;

        var (ut, rc) = await _userTags.LoadAsync(user.Id);
        ut.Guarantees().IsNotNull();
        ut.Tags = user.Tags.ToList();
        await _userTags.UpdateAsync((ut, rc));
    }

    /// <summary>
    /// UntagUser allows admin to untag uder by email
    /// </summary>
    /// <param name="email"></param>
    /// <param name="untags"></param>
    /// <returns></returns>
    public async Task UntagUser(string email, IEnumerable<string> untags)
    {
        var cancel = new CancellationToken();

        var user = await _userManager.FindByEmailAsync(email, cancel);
        user.Guarantees().IsNotNull();

        var readyTags = TagUtil.MakeTags(untags);
        readyTags = readyTags.Where(tg => user!.Tags.Contains(tg));
        foreach (var tag in readyTags)
            user!.Tags.Remove(tag);
        await UpdateUserTags(user);

        await _userManager.UpdateAsync(user!, cancel);
    }

    /// <summary>
    /// GetRoleNames returns the names of the role IDs passed in
    /// </summary>
    /// <param name="roleIds"></param>
    /// <returns></returns>
    public async Task<Dictionary<Guid,string>> GetRoleNames(IEnumerable<Guid> roleIds)
    {
        var cancel = new CancellationToken();

        var lookup = new Dictionary<Guid, string>();
        var work = new List<Task<ApplicationRole?>>();
        foreach (var role in roleIds)
        {
            work.Add(_roleManager.FindByIdAsync(role.ToString(), cancel));
        }

        await Task.WhenAll(work);
        var results = work.Select(it => it.Result);

        foreach(var role in results)
        {
            if (role != null && role.Name != null)
                lookup[role.Id] = role.Name;
        }
        return lookup;  
    }

    /// <summary>
    /// FindTaggedUsers returns list of users filtered by tags passed in
    /// </summary>
    /// <param name="tags"></param>
    /// <returns></returns>
    public async Task<IList<ApplicationUserViewModel>> FindTaggedUsers(IEnumerable<string> tags)
    {
        var cancel = new CancellationToken();

        var readyTags = TagUtil.MakeTags(tags);
        var (foundTaggedUsers, _) = await _userTags.GetAllAsync(it => readyTags.Any(tg => it.Tags.Contains(tg)));

        var work = new List<Task<ApplicationUser?>>();
        foreach(var found in foundTaggedUsers)
            work.Add(_userManager.FindByEmailAsync(found.Email, cancel));
        
        await Task.WhenAll(work);

        var results = work.Select(t => t.Result);

        var roleIds = results.Where(it => it != null).SelectMany(it => it!.Roles).Distinct();
        var roleNames = await GetRoleNames(roleIds);
        

        var convert = from t in work
                      let user = t.Result
                      where user != null
                      let userRoleNames = user.Roles.Select(ur=>roleNames[ur])
                      select new ApplicationUserViewModel
                      {
                          UserName = user.UserName ?? string.Empty,
                          RoleNames = userRoleNames.ToList(),
                          TimeZoneId = user.TimeZoneId,
                          Email = user.Email ?? string.Empty,
                          Tags = user.Tags ?? new List<string>()
                      };

        return convert.ToList();

    }


}
