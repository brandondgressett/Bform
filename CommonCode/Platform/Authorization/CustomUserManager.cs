using AspNetCore.Identity.MongoDbCore.Models;
using BFormDomain.CommonCode.Authorization;
using BFormDomain.Repository;
using LinqKit;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Authorization
{
    public class CustomUserManager : IUserStore<ApplicationUser>, IUserEmailStore<ApplicationUser>, 
        IUserPasswordStore<ApplicationUser>, IUserClaimStore<ApplicationUser>
    {
        IRepository<ApplicationUser> _userRepo;
        IRepository<ApplicationRole> _roleRepo;
        CustomRoleManager _customRoleManager;
        public CustomUserManager(IRepository<ApplicationUser> userRepo, CustomRoleManager roleManager, IRepository<ApplicationRole> roleRepo)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _customRoleManager = roleManager;
        }
        public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            await _userRepo.CreateAsync(user);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            await _userRepo.DeleteAsync(user);

            return IdentityResult.Success;
        }

        public void Dispose()
        {
            
        }

        public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == Guid.Parse(userId));
            var user = await _userRepo.GetOneAsync(pred);

            return user.Item1;
        }

        public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.NormalizedUserName == normalizedUserName);

            var user = await _userRepo.GetOneAsync(pred);

            return user.Item1;
        }

        public async Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var userRes = await _userRepo.GetOneAsync(pred);

            return userRes.Item1?.NormalizedUserName;
        }

        public async Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var userID = user.Id.ToString();
            var userRes = await FindByIdAsync(userID, cancellationToken);

            return userRes?.Id.ToString() ?? string.Empty;
        }

        public async Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var userRes = await FindByIdAsync(user.Id.ToString(), cancellationToken);

            var userName = userRes?.UserName;

            return userName;
        }

        public async Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;

            await _userRepo.UpdateAsync(user);
        }

        public async Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;

            await _userRepo.UpdateAsync(user);
        }

        public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            await _userRepo.UpdateAsync(user);
            
            return IdentityResult.Success;
        }

        public async Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Email == email);

            var user = await _userRepo.GetOneAsync(pred);

            return user.Item1;
        }

        public async Task<bool> AddClaimAsync(ApplicationUser user, MongoClaim claim)
        {
            try
            {
                var cancel = new CancellationToken();
                var confirmedUser = await FindByIdAsync(user.Id.ToString(), cancel);

                confirmedUser?.Claims.Add(claim);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            
            await _userRepo.UpdateAsync(user);
        }

        public async Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var resUser = await _userRepo.GetOneAsync(pred);

            return resUser.Item1?.Email;
        }

        public async Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var resUser = await _userRepo.GetOneAsync(pred);

            return resUser.Item1?.EmailConfirmed ?? false;
        }

        public async Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;

            await _userRepo.UpdateAsync(user);
        }

        public async Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var resUser = await _userRepo.GetOneAsync(pred);

            return resUser.Item1?.NormalizedEmail;
        }

        public async Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;

            await _userRepo.UpdateAsync(user);
        }

        public async Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;

            await _userRepo.UpdateAsync(user);
        }

        public async Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var passUser = await _userRepo.GetOneAsync(pred);

            return passUser.Item1?.PasswordHash;
        }

        public async Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var passHash = await GetPasswordHashAsync(user, cancellationToken);

            if(!String.IsNullOrEmpty(passHash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<IdentityResult> AddToRolesAsync (ApplicationUser user, IEnumerable<string> roles)
        {
            var cancel = new CancellationToken();

            foreach(var role in roles)
            {
                
                var rawRole = await _customRoleManager.FindByNameAsync(role, cancel);
                if (rawRole != null)
                {
                    user.Roles.Add(rawRole.Id);
                }
            }

            await _userRepo.UpdateAsync(user);

            return IdentityResult.Success;
        }

        public async Task<IList<Claim>> GetClaimsAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var resUser = await _userRepo.GetOneAsync(pred);

            if (resUser.Item1?.Claims != null)
            {
                var claimList = new List<Claim>();

                foreach(var claim in resUser.Item1.Claims)
                {
                    var standardClaim = new Claim(claim.Type, claim.Value, claim.GetType().ToString(), claim.Issuer ?? string.Empty);

                    claimList.Add(standardClaim);
                }

                return claimList;
            }
            else
            {
                return new List<Claim>();
            }
        }

        public async Task AddClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var resUser = await _userRepo.GetOneAsync(pred);

            foreach (var claim in claims)
            {
                var mongoClaim = new MongoClaim();

                mongoClaim.Issuer = claim.Issuer;
                mongoClaim.Value = claim.Value;
                mongoClaim.Type = claim.Type;

                resUser.Item1?.Claims.Add(mongoClaim);
            }
            
        }

        public async Task ReplaceClaimAsync(ApplicationUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var resUser = await _userRepo.GetOneAsync(pred);

            if (resUser.Item1 != null)
            {
                for(int i = 0; i < resUser.Item1.Claims.Count; i++)
                {
                    if(resUser.Item1.Claims[i].Type == claim.Type)
                    {
                        resUser.Item1.Claims[i].Type = newClaim.Type;
                        resUser.Item1.Claims[i].Value = newClaim.Value;
                        resUser.Item1.Claims[i].Issuer = newClaim.Issuer ?? string.Empty;
                    }
                }

                await _userRepo.UpdateAsync(resUser.Item1);
            }
            
        }

        public async Task RemoveClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var resUser = await _userRepo.GetOneAsync(pred);

            foreach(var claim in claims)
            {
                var mongoClaim = new MongoClaim();

                mongoClaim.Issuer = claim.Issuer;
                mongoClaim.Type = claim.Type;
                mongoClaim.Value = claim.Value;

                resUser.Item1?.Claims.Remove(mongoClaim);

            }
        }

        public async Task<IList<ApplicationUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            var mongoClaim = new MongoClaim();

            mongoClaim.Issuer = claim.Issuer;
            mongoClaim.Type = claim.Type;
            mongoClaim.Value = claim.Value;

            pred = pred.And(it => it.Claims.Contains(mongoClaim));

            var resUsers = await _userRepo.GetAllAsync(pred);

            return resUsers.Item1;
        }

        public async Task<List<Guid>> GetRolesAsync(ApplicationUser user)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Id == user.Id);

            var resUser = await _userRepo.GetOneAsync(pred);

            return resUser.Item1?.Roles ?? new List<Guid>();
        }

        public async Task<List<ApplicationUser>> GetUsersInRoleAsync(string roleName)
        {
            var pred = PredicateBuilder.New<ApplicationUser>();

            pred = pred.And(it => it.Roles.Contains(Guid.Parse(roleName)));//CAG Issue roleName is a string containing the actual rolename
            //where as we're trying to search for guids making this ^ statement useless

            var users = await _userRepo.GetAllAsync(pred);

            return users.Item1;
        }

        public async Task<IdentityResult> SetLockoutEnabledAsync(ApplicationUser user, bool lockoutUser)
        {
            user.LockoutEnabled = lockoutUser;

            await _userRepo.UpdateAsync(user);

            return IdentityResult.Success;
        }

        public async Task AddToRoleAsync(ApplicationUser user, string roleName)
        {
            var pred = PredicateBuilder.New<ApplicationRole>();

            pred = pred.And(it => it.Name == roleName);

            var appRole = await _roleRepo.GetOneAsync(pred);

            if (appRole.Item1 != null)
                user.Roles.Add(appRole.Item1.Id);

            await _userRepo.UpdateAsync(user);
        }

        public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName)
        {
            var pred = PredicateBuilder.New<ApplicationRole>();

            pred = pred.And(it => it.Name == roleName);

            var appRole = await _roleRepo.GetOneAsync(pred);

            if (appRole.Item1 != null)
                user.Roles.Remove(appRole.Item1.Id);
        }

        public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, Token token, string newPassword)
        {
            if(VerifyPasswordResetToken(token, user))
            {
                user.PasswordHash = newPassword;
                await _userRepo.UpdateAsync(user);

                return IdentityResult.Success;
            }
            else
            {
                return IdentityResult.Failed();
            }
        }

        public async Task<Token> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            var token = new Token();

            user.Tokens.Add(token);

            await _userRepo.UpdateAsync(user);

            return token;
        }

        private bool VerifyPasswordResetToken(Token token, ApplicationUser user)
        {
            if(user.Tokens.Contains(token))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
