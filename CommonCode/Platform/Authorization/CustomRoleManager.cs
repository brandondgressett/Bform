using AspNetCore.Identity.MongoDbCore.Models;
using BFormDomain.CommonCode.Authorization;
using BFormDomain.Repository;
using LinqKit;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Authorization
{
    public class CustomRoleManager : IRoleStore<ApplicationRole>
    {
        private readonly IRepository<ApplicationRole> _roleRepo;
        private readonly UIExceptionFunnel _exFunnel;
        public CustomRoleManager(IRepository<ApplicationRole> roleRepo, UIExceptionFunnel exFunnel)
        {
            _roleRepo = roleRepo;
            _exFunnel = exFunnel;

        }
        public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            try
            {
                await _roleRepo.CreateAsync(role);
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Create Role Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            try
            {
                await _roleRepo.DeleteAsync(role);
                return IdentityResult.Success;
            }
            catch(Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Delete Role Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public void Dispose()
        {
            
        }

        public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            try
            {
                var id = Guid.Parse(roleId);
                var pred = PredicateBuilder.New<ApplicationRole>();
                pred = pred.And(it => it.Id == id);

                var retval = await _roleRepo.GetOneAsync(pred);

                return retval.Item1!;
            }
            catch (Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Find Role By ID Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            try
            {
                var pred = PredicateBuilder.New<ApplicationRole>();
                pred = pred.And(it => it.Name == normalizedRoleName);

                var retval = await _roleRepo.GetOneAsync(pred);

                return retval.Item1!;
            }
            catch (Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Find Role By Name Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            try
            {
                var pred = PredicateBuilder.New<ApplicationRole>();
                pred = pred.And(it => it.Id == role.Id);

                var appRole = await FindByIdAsync(role.Id.ToString(), cancellationToken);

                return appRole?.NormalizedName;
            }
            catch(Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Get Normalized Role Name Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            try
            {
                var appRole = await FindByNameAsync(role.Name ?? string.Empty, cancellationToken);

                if (appRole != null)
                {
                    return appRole.Id.ToString();
                }
                else
                {
                    return "No Role";
                }
            }
            catch (Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Get Role ID Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            try
            {
                var appRole = await FindByIdAsync(role.Id.ToString(), cancellationToken);

                return appRole?.Name;
            }
            catch(Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Get Role Name Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
        {
            try
            {
                var appRole = await FindByIdAsync(role.Id.ToString(), cancellationToken);

                if (appRole != null)
                {
                    appRole.NormalizedName = normalizedName;
                    _roleRepo.Update(appRole);
                }
            }
            catch (Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Set Normalized Role Name Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
        {
            try
            {
                var appRole = await FindByIdAsync(role.Id.ToString(), cancellationToken);

                if (appRole != null)
                {
                    appRole.Name = roleName;

                    _roleRepo.Update(appRole);
                }
            }
            catch(Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Set Role Name Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            try
            {
                await _roleRepo.UpdateAsync(role);

                var res = new IdentityResult();

                res.Succeeded.Equals(true);

                return res;
            }
            catch (Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Update Role Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            try
            {
                var pred = PredicateBuilder.New<ApplicationRole>();

                pred = pred.And(it => it.Name == roleName || it.NormalizedName == roleName);

                var role = await _roleRepo.GetOneAsync(pred);

                if (role.Item1 != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Check If Role Exists");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }

        public async Task<List<MongoClaim>> GetClaimsAsync(ApplicationRole appRole)
        {
            try
            {
                var pred = PredicateBuilder.New<ApplicationRole>();
                pred = pred.And(it => it.Id == appRole.Id);

                var cancel = new CancellationToken();

                var role = await FindByIdAsync(appRole.Id.ToString(), cancel);

                return role?.Claims?.Select(c => new MongoClaim { Type = c, Value = "true" }).ToList() ?? new List<MongoClaim>();
            }
            catch (Exception ex)
            {
                var uiError = _exFunnel.FunnelAlert(ex, "Get Role Claims Async");

                var exception = new Exception(uiError.Message + " | " + uiError.DevDetail + " | " + uiError.RefCode);

                throw exception;
            }
        }
    }
}
