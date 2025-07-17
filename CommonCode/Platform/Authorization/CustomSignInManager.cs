using BFormDomain.CommonCode.Authorization;
using BFormDomain.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Authorization
{
    public class CustomSignInManager
    {
        IRepository<ApplicationUser> _userRepo;
        JwtComponent _jwtComp;
        CustomUserManager _UserManager;


        public CustomSignInManager(IRepository<ApplicationUser> userRepo, JwtComponent jwtComp, CustomUserManager userManager)
        {
            _userRepo = userRepo;
            _jwtComp = jwtComp;
            _UserManager = userManager;
        }

        public async Task<IdentityResult> PasswordSignInAsync(ApplicationUser user, string password, bool whatone, bool whattwo)
        {
            var cancel = new CancellationToken();

            var pass = await _UserManager.GetPasswordHashAsync(user, cancel);

            if(password == pass)
            {
                await _jwtComp.GenerateJwtToken(user);
                return IdentityResult.Success;
            }
            else
            {
                return IdentityResult.Failed();
            }
        }

        public async Task SignOutAsync(ApplicationUser user, string returnUrl)
        {
            user.Tokens.Clear();

            await _userRepo.UpdateAsync(user);
        }
    }
}
