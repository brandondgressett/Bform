using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BFormDomain.CommonCode.Authorization;

/// <summary>
/// LoginLogic manages user logins and logouts 
///     Authorized by JWT
///     -References:
///         >Service
///     -Functions:
///         >Login
///         >Logout
///         >Refresh
/// </summary>
public class LoginLogic
{
    private readonly JwtComponent _jwt;
    private readonly CustomUserManager _userManager;
    private readonly CustomSignInManager _signInManager;

    public LoginLogic(
        JwtComponent jwt,
        CustomUserManager userManager,
        CustomSignInManager signInManager)
    {
        _jwt = jwt;
        _userManager = userManager;
        _signInManager = signInManager;

    }

    public async Task<AuthResponse> Login(string email, string password)
    {
        email.Requires().IsNotNullOrEmpty();
        password.Requires().IsNotNullOrEmpty();

        var cancel = new CancellationToken();

        var user = await _userManager.FindByEmailAsync(email, cancel);
        user.Guarantees("Login failed: Email or password incorrect.").IsNotNull();
        var result = await _signInManager.PasswordSignInAsync(user!, password, false, false);
        result.Succeeded.Guarantees("Login failed: Email or password incorrect.").IsTrue();

        var token = await _jwt.GenerateJwtToken(user!);

        return token;
    }

    public async Task Logout(ApplicationUser user, string returnUrl)
    {
        await _signInManager.SignOutAsync(user, returnUrl);//Need to add user as a param
    }

    public async Task<AuthResponse> Refresh(TokenRequest tokenRequest)
    {
        return await _jwt.RefreshToken(tokenRequest);
    }


}
