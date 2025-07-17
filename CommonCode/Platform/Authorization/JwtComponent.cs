using AspNetCore.Identity.MongoDbCore.Models;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BFormDomain.CommonCode.Authorization;

/// <summary>
/// 
/// Following https://www.youtube.com/watch?v=LgpC4tYtc6Y&list=PLcvTyQIWJ_ZpumOgCCify-wDY_G-Kx34a&index=2
/// </summary>
/// <summary>
/// JwtComponent manages JSON web tokens for authorization procedures
///     -References:
///         >LoginLogic.cs
///         >RegistrationLogic.cs
///     -Functions:
///         >GenerateJwtToken
///         >AssembleClaimsAsync
///         >RefreshToken
///         >VerifyAndGenerateToken
/// </summary>
public class JwtComponent
{
    private readonly JwtConfig _jwtConfig;
    private readonly IRepository<RefreshToken> _jwtRepository;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IApplicationAlert _alerts;
    private readonly CustomRoleManager _roleManager;
    private readonly CustomUserManager _userManager;


    public JwtComponent(
        IOptions<JwtConfig> jwtConfig, 
        IRepository<RefreshToken> jwtRepository,
        TokenValidationParameters tokenValidationParameters,
        IApplicationAlert alerts,
        CustomRoleManager roleManager,
        CustomUserManager userManager)
    {
        _jwtConfig = jwtConfig.Value;
        _jwtRepository = jwtRepository;
        _tokenValidationParameters = tokenValidationParameters;
        _alerts = alerts;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// GenerateJwtToken creates a JWT by user
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<AuthResponse> GenerateJwtToken(ApplicationUser user)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

        var userClaims = await AssembleClaimsAsync(user);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(userClaims),
            Expires = DateTime.UtcNow.AddSeconds(600.0),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = jwtTokenHandler.WriteToken(token);

        var refreshToken = new RefreshToken
        {
            JwtId = token.Id,
            IsUsed = false,
            IsRevoked = false,
            UserId = user.Id,
            Added = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            Token = GoodSeedRandom.RandomString(35) + Guid.NewGuid()
        };

        await _jwtRepository.CreateAsync(refreshToken);

        return new AuthResponse
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Roles = user.Roles.Select(it=>it.ToString()).ToList()
        };

    }

    /// <summary>
    /// AssembleClaimsAsync returns list of claims associated to user 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    private async Task<List<Claim>> AssembleClaimsAsync(ApplicationUser user)
    {
        var cancel = new CancellationToken();

        // basic claims
        var claims = new List<Claim>
        {
            new Claim("Id", user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // pre-existing claims
        var userClaims = await _userManager.GetClaimsAsync(user, cancel);
        claims.AddRange(userClaims);

        // role claims
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var userRole in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.ToString()));
            var role = await _roleManager.FindByNameAsync(userRole.ToString(), cancel);
            if(role is not null)
            {
                // claims for the role.
                var roleClaims = await _roleManager.GetClaimsAsync(role);
                foreach (var roleClaim in roleClaims)
                {
                    var claim = new Claim(roleClaim.Type, roleClaim.Value, roleClaim.Value.GetType().ToString(), roleClaim.Issuer);
                    claims.Add(claim);
                }

            }
        }



        return claims;
    }

    /// <summary>
    /// RefreshToken refreshes token by token request
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<AuthResponse> RefreshToken(TokenRequest request)
    {
        request.Requires().IsNotNull();
        request.Token.Requires().IsNotNull();
        request.RefreshToken.Requires().IsNotNull();

        return await VerifyAndGenerateToken(request);
    }

    /// <summary>
    /// VerifyAndGenerateToken verifies token by token request and creates a new one
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private async Task<AuthResponse> VerifyAndGenerateToken(TokenRequest request)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();

        var cancel = new CancellationToken();

        try
        {
            // 1. Is a jwt token?
            var tokenInVerification = jwtTokenHandler.ValidateToken(request.Token, _tokenValidationParameters, out var validatedToken);

            // 2. validate encryption
            if(validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCulture);
                result.Guarantees("Token has wrong encryption.").IsTrue();
            }

            // 3. validate expiration
            var expClaim = tokenInVerification.Claims.FirstOrDefault(it => it.Type == JwtRegisteredClaimNames.Exp);
            expClaim.Guarantees().IsNotNull();
            var utcExpiration = long.Parse(expClaim!.Value);
            var expiryDate = DateTimeUtility.UnixTimeStampToDateTime(utcExpiration);
            expiryDate.Guarantees("Token has not expired.").IsGreaterThan(DateTime.UtcNow);

            // 4. validate token exists
            var (storedToken, ctx) = await _jwtRepository.GetOneAsync(it => it.Token == request.RefreshToken);
            storedToken.Guarantees("Refresh token does not exist.").IsNotNull();

            // 5. validate it isn't in use already
            storedToken!.IsUsed.Guarantees("Token has been used.").IsFalse();

            // 6. validate it isn't revoked
            storedToken!.IsRevoked.Guarantees("Token has been revoked.").IsFalse();

            // 7. validate token matches.
            var jtiClaim = tokenInVerification.Claims.FirstOrDefault(it => it.Type == JwtRegisteredClaimNames.Jti);
            jtiClaim.Guarantees().IsNotNull();
            var jti = jtiClaim!.Value;
            jti.Guarantees("Token does not match.").Equals(storedToken.JwtId);

            // update token
            storedToken.IsUsed = true;
            await _jwtRepository.UpdateAsync((storedToken, ctx));

            // get matching user.
            var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString(), cancel);
            user.Guarantees().IsNotNull();

            return await GenerateJwtToken(user!);


        } catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.System, Microsoft.Extensions.Logging.LogLevel.Warning,
                ex.TraceInformation(), 60, nameof(JwtComponent));
            throw;
        }
    }

}
