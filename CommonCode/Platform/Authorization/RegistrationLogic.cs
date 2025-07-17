using BFormDomain.CommonCode.Notification;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BFormDomain.CommonCode.Authorization;

/// <summary>
/// RegistrationLogic registers new user information either directly or via invitation
///     -References:
///         >Service
///     -Functions:
///         >RegisterUsingInvitation
///         >DirectRegister
/// </summary>
public class RegistrationLogic
{
    private readonly IRepository<InvitationDataModel> _inviteRepo;
    private readonly JwtComponent _jwt;
    private readonly IRepository<NotificationContact> _contactRepo;
    private readonly IRepository<UserTagsDataModel> _userTags;
    private readonly AppEventSink _eventSink;
    private readonly IDataEnvironment _dataEnv;
    private readonly CustomUserManager _userManager;

    public RegistrationLogic(
        JwtComponent jwt, 
        IRepository<InvitationDataModel> inviteRepo,
        IRepository<NotificationContact> notifRepo,
        IRepository<UserTagsDataModel> userTagsRepo,
        AppEventSink sink,
        IDataEnvironment env,
        CustomUserManager userManager)
    {
        _inviteRepo = inviteRepo;
        _jwt = jwt;
        _contactRepo = notifRepo;
        _eventSink = sink;
        _dataEnv = env;
        _userTags = userTagsRepo;
        _userManager = userManager;
    }

    /// <summary>
    /// RegisterUsingInvitation registers new user information via invitation
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="inviteCode"></param>
    /// <param name="createContact"></param>
    /// <param name="tzId"></param>
    /// <param name="textNumber"></param>
    /// <param name="callNumber"></param>
    /// <returns></returns>
    public async Task<AuthResponse> RegisterUsingInvitation(
        string userName,
        string email,
        string password,
        string inviteCode,
        bool createContact,
        string tzId,
        string? textNumber,
        string? callNumber)
    {
        
        inviteCode.Requires().IsNotNullOrEmpty();

        var (invitation, _) = await _inviteRepo.GetOneAsync(it => it.InvitationCode == inviteCode && it.Expiration > DateTime.UtcNow);
        invitation!.Requires("Invitation Code Not Found.").IsNotNull();
        var userRoleNames = invitation!.InvitedRoles;

        return await DirectRegister(userName,email,password, userRoleNames, createContact, tzId, textNumber, callNumber);

    }

    /// <summary>
    /// DirectRegister registers new user information 
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="userRoleNames"></param>
    /// <param name="createContact"></param>
    /// <param name="tzId"></param>
    /// <param name="textNumber"></param>
    /// <param name="callNumber"></param>
    /// <returns></returns>
    public async Task<AuthResponse> DirectRegister(
        string userName,
        string email,
        string password,
        IEnumerable<string> userRoleNames,
        bool createContact,
        string tzId,
        string? textNumber,
        string? callNumber)
    {
        userName.Requires().IsNotNullOrEmpty();
        userName.Requires().IsLongerOrEqual(8);
        email.Requires().IsNotNullOrEmpty();
        password.Requires().IsNotNullOrEmpty();
        password.Requires().IsLongerOrEqual(8);
        tzId.Requires().IsNotNullOrEmpty();

        var cancel = new CancellationToken();

        var existing = await _userManager.FindByEmailAsync(email, cancel);
        existing.Guarantees("Email already in use.").IsNull();
        
        existing = await _userManager.FindByNameAsync(userName, cancel);
        existing.Guarantees("ApplicationUser name already in use.").IsNull();

        var appUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            TimeZoneId = tzId,
            PasswordHash = password,
        };

        var result = await _userManager.CreateAsync(appUser, cancel);
        result.Succeeded.Requires().IsTrue();

        if (userRoleNames.Any())
        {
            result = await _userManager.AddToRolesAsync(appUser, userRoleNames);
            result.Succeeded.Requires().IsTrue();
        }

        await _userTags.CreateAsync(new UserTagsDataModel
        {
            Id = appUser.Id,
            UserName = userName,
            Email = email,
            Version = 0,
            Tags = new List<string>()
        });

        if(createContact)
        {
            tzId.Requires().IsNotNullOrEmpty(); 

            await _contactRepo.CreateAsync(
                new NotificationContact
                {
                    Active = true,
                    EmailAddress = email,
                    ContactTitle = userName,
                    Id = Guid.NewGuid(),
                    TimeZoneInfoId = tzId!,
                    UserRef = appUser.Id,
                    TimeSeverityTable = new(),
                    TextNumber = textNumber,
                    CallNumber = callNumber
                });
        }

        var jwtToken = await _jwt.GenerateJwtToken(appUser);
        var trx = await _dataEnv.OpenTransactionAsync(CancellationToken.None);

        try
        {
            _eventSink.BeginBatch(trx);
            var action = GuidEncoder.Encode(appUser.Id);
            var userEntityWrapper = new EntityWrapping<ApplicationUser>()
            {
                Id = appUser.Id,
                CreatedDate = DateTime.UtcNow,
                Creator = appUser.Id,
                LastModifier = appUser.Id,
                Version = 0,
            };

            foreach (var role in userRoleNames)
                await _eventSink.Enqueue(null, $"users.{role}.action.register", action, userEntityWrapper, appUser.Id, null, false);

            await _eventSink.CommitBatch();
            await trx.CommitAsync();
        } catch (Exception)
        {
            await trx.AbortAsync();
            throw;
        }

        return jwtToken;
    }

}
