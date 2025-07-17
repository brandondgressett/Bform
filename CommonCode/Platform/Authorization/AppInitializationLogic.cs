using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.Authorization;

/// <summary>
/// AppInitializationLogic is used to initialize a client application with:
///     initial manager account
///     roles specified
///     initial users
///     
///     -References:
///         >Service
///     -Functions:
///         >Initialize
///         >InitializeDemoUsers
///         >InitializeRoles
/// </summary>
public class AppInitializationLogic
{
    

    private readonly RegistrationLogic _registrar;
    private readonly string _ownershipCode;
    private readonly IServiceProvider _serviceProvider;
    private readonly CustomUserManager _userManager;
    private readonly CustomRoleManager _roleManager;

    public AppInitializationLogic(
        CustomRoleManager roleManager, 
        RegistrationLogic registrar,
        CustomUserManager userManager,
        IOptions<AuthorizationInitOptions> options,
        IServiceProvider serviceProvider)
    {

        _registrar = registrar;
        var optionVal = options.Value;
        _ownershipCode = optionVal.OwnershipCode;
        _serviceProvider = serviceProvider;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<bool> IsInitialized()
    {
        var existingRoles = await _roleManager.RoleExistsAsync(ApplicationRoles.UserAdministratorRole);
        var existingAdmins = await _userManager.GetUsersInRoleAsync("f359c86b-ee47-43f4-bce5-2030940bf9f2");
        
        return existingRoles && existingAdmins.Any();
    }


    /// <summary>
    /// Take ownership of the site, creating a user manager account to manage,
    /// creating application roles.
    /// </summary>
    /// <param name="ownerName"></param>
    /// <param name="ownerEmail"></param>
    /// <param name="ownerPassword"></param>
    /// <param name="code"></param>
    /// <param name="tzId"></param>
    /// <param name="textNumber"></param>
    /// <param name="callNumber"></param>
    /// <param name="withDemoUsers"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public async Task Initialize(
        string ownerName, 
        string ownerEmail, 
        string ownerPassword, 
        string code, 
        string tzId, string? textNumber, string? callNumber,
        bool withDemoUsers)
    {
        bool isInitialized = await IsInitialized();
        if (isInitialized)
            return;

        if (_ownershipCode != code)
            throw new UnauthorizedAccessException("Invalid BForm Initialization Code.");

        ownerName.Requires().IsNotNullOrEmpty();
        ownerEmail.Requires().IsNotNullOrEmpty();
        ownerPassword.Requires().IsNotNullOrEmpty();
        code.Requires().IsNotNullOrEmpty();

        await Initialize(withDemoUsers);

        await _registrar.DirectRegister(
            ownerName, ownerEmail, ownerPassword, 
            EnumerableEx.OfTwo(
                ApplicationRoles.UserAdministratorRole, ApplicationRoles.SiteManagerRole),
                true, tzId, textNumber, callNumber);
    }

    private async Task Initialize(bool withDemoUsers)
    {
        await InitializeRoles();

        if (withDemoUsers)
            await InitializeDemoUsers();
    }

    private async Task InitializeDemoUsers()
    {
        var cancel = new CancellationToken();

        bool exists;
        ApplicationUser? user;
        var zone = TimeZoneInfo.Local;

        user = await _userManager.FindByEmailAsync(BuiltIn.UserReaderEmail, cancel);
        exists = user is not null;
        if(!exists)
            await _registrar.DirectRegister(
                BuiltIn.UserReaderName,
                BuiltIn.UserReaderEmail,
                BuiltIn.UserReaderPassword,
                EnumerableEx.OfOne(ApplicationRoles.UserReaderRole),
                true,
                zone.Id, 
                null, null);

        user = await _userManager.FindByEmailAsync(BuiltIn.SiteReaderEmail, cancel);
        exists = user is not null;
        if(!exists)
            await _registrar.DirectRegister(
                BuiltIn.SiteReaderName, 
                BuiltIn.SiteReaderEmail, 
                BuiltIn.SiteReaderPassword, 
                EnumerableEx.OfOne(ApplicationRoles.SiteReaderRole),
                true,
                zone.Id,
                null, null);        
    }

    private async Task InitializeRoles()
    {
        var cancel = new CancellationToken();

        IdentityResult result;
        bool exists;

        exists = await _roleManager.RoleExistsAsync(ApplicationRoles.UserAdministratorRole);
        if (!exists)
        {
            result = await _roleManager.CreateAsync(new ApplicationRole { Id = new Guid("f359c86b-ee47-43f4-bce5-2030940bf9f2"), Name = ApplicationRoles.UserAdministratorRole }, cancel);
            result.Succeeded.Requires().IsTrue();
        }

        exists = await _roleManager.RoleExistsAsync(ApplicationRoles.UserReaderRole);
        if (!exists)
        {
            result = await _roleManager.CreateAsync(new ApplicationRole { Id = new Guid("c9dfd105-dac1-419e-9ec6-2daa40739578"), Name = ApplicationRoles.UserReaderRole }, cancel);
            result.Succeeded.Requires().IsTrue();
        }

        exists = await _roleManager.RoleExistsAsync(ApplicationRoles.SiteManagerRole);
        if (!exists)
        {
            result = await _roleManager.CreateAsync(new ApplicationRole { Id = new Guid("7a3ea070-dc1c-427b-805e-f5688ae88b87"), Name = ApplicationRoles.SiteManagerRole }, cancel);
            result.Succeeded.Requires().IsTrue();
        }

        exists = await _roleManager.RoleExistsAsync(ApplicationRoles.SiteReaderRole);
        if (!exists)
        {
            result = await _roleManager.CreateAsync(new ApplicationRole { Id = new Guid("96d78d9a-bb15-4f18-aa87-d4c0a59623f3"), Name = ApplicationRoles.SiteReaderRole }, cancel);
            result.Succeeded.Requires().IsTrue();
        }

        exists = await _roleManager.RoleExistsAsync(ApplicationRoles.WorkSetManagerRole);
        if (!exists)
        {
            result = await _roleManager.CreateAsync(new ApplicationRole { Id = new Guid("03a02c96-4b32-4ed0-9ecf-b6efd752bb25"), Name = ApplicationRoles.WorkSetManagerRole }, cancel);
            result.Succeeded.Requires().IsTrue();
        }

        exists = await _roleManager.RoleExistsAsync(ApplicationRoles.WorkItemManagerRole);
        if (!exists)
        {
            result = await _roleManager.CreateAsync(new ApplicationRole { Id = new Guid("f121649f-1d98-447c-8819-3b1d43afbe59"), Name = ApplicationRoles.WorkItemManagerRole }, cancel);
            result.Succeeded.Requires().IsTrue();
        }

        exists = await _roleManager.RoleExistsAsync(ApplicationRoles.WorkItemContributerRole);
        if (!exists)
        {
            result = await _roleManager.CreateAsync(new ApplicationRole { Id = new Guid("dee350c5-37d9-43b0-858c-a23f40c55193"), Name = ApplicationRoles.WorkItemContributerRole }, cancel);
            result.Succeeded.Requires().IsTrue();
        }

        exists = await _roleManager.RoleExistsAsync(ApplicationRoles.WorkItemReaderRole);
        if (!exists)
        {
            result = await _roleManager.CreateAsync(new ApplicationRole { Id = new Guid("0388b452-6caa-4fe6-bb3b-f54fc344a82b"), Name = ApplicationRoles.WorkItemReaderRole }, cancel);
            result.Succeeded.Requires().IsTrue();
        }
    }
}
