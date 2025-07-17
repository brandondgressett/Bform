using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.HelperClasses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Utility
{
    public static class ControllerUserAuthorizationExtensions
    {
        
        public static string GetRoleID(CustomRoleManager roleStore, string roleName)
        {
            var cancel = new CancellationToken();
            var role = new ApplicationRole();
            role.Name = roleName;

            var roleID = AsyncHelper.RunSync(() => roleStore.GetRoleIdAsync(role, cancel));

            return roleID;
        }

        public static bool CheckUserAuthorization(this Controller controller, CustomRoleManager roleManager, params string[] authorizedClaims)
        {
            List<string> roleIDs = new List<string>();
            foreach(var claim in authorizedClaims)
            {
                roleIDs.Add(GetRoleID(roleManager, claim));
            }

            bool authorized = false;
            string token = "";

            if (controller.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                string? authHeader = controller.HttpContext.Request.Headers["Authorization"];

                string[] headerParts = authHeader?.Split(' ') ?? Array.Empty<string>();

                if (headerParts.Length == 2 && headerParts[0].Equals("Bearer"))
                {
                    token = headerParts[1];
                }
                else if (headerParts.Count() > 0)
                {
                    token = headerParts[0];
                }
            }

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token);
            var tokenS = jsonToken as JwtSecurityToken;

            if (tokenS != null)
            {
                foreach (var claim in tokenS.Claims)
                {
                    //if (claim.Type == ApplicationRoles.UserAdministratorRole)
                    if (roleIDs.Contains(claim.Value))
                    {
                        authorized = true;
                    }
                }
            }

            return authorized;
        }

        public static Guid GetUserIDFromHeader(this Controller controller)
        {
            string token = "";

            if (controller.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                string? authHeader = controller.HttpContext.Request.Headers["Authorization"];

                string[] headerParts = authHeader?.Split(' ') ?? Array.Empty<string>();

                if (headerParts.Length == 2 && headerParts[0].Equals("Bearer"))
                {
                    token = headerParts[1];
                }
                else if (headerParts.Count() > 0)
                {
                    token = headerParts[0];
                }
            }

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token);
            var tokenS = jsonToken as JwtSecurityToken;

            return Guid.Parse(tokenS!.Claims.ElementAt(0).Value);
        }
    }
}
