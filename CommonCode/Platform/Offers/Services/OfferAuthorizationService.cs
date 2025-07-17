using BFormDomain.CommonCode.Platform.Offers.Repository;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Utility;
using AspNetCore.Identity.MongoDbCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Services
{
    /// <summary>
    /// Implementation of offer authorization service with role-based access control
    /// </summary>
    public class OfferAuthorizationService : IOfferAuthorizationService
    {
        private readonly IPromotionalOfferRepository _offerRepository;
        private readonly ITenantContext _tenantContext;
        private readonly UserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<MongoIdentityRole<Guid>> _roleManager;
        private readonly ICacheService? _cache;
        private readonly ILogger<OfferAuthorizationService>? _logger;

        // Role-based permissions mapping
        private static readonly Dictionary<string, OfferPermissions> RolePermissions = new()
        {
            ["guest"] = new OfferPermissions
            {
                ViewOffers = false
            },
            ["member"] = new OfferPermissions
            {
                ViewOffers = true
            },
            ["editor"] = new OfferPermissions
            {
                ViewOffers = true,
                CreateOffers = true,
                EditOffers = true,
                CloneOffers = true
            },
            ["manager"] = new OfferPermissions
            {
                ViewOffers = true,
                CreateOffers = true,
                EditOffers = true,
                DeleteOffers = true,
                ViewAnalytics = true,
                CloneOffers = true,
                SendTestEmails = true,
                ExtendExpiration = true,
                ArchiveOffers = true
            },
            ["admin"] = new OfferPermissions
            {
                ViewOffers = true,
                CreateOffers = true,
                EditOffers = true,
                DeleteOffers = true,
                ViewAnalytics = true,
                ManageSettings = true,
                BulkOperations = true,
                SendTestEmails = true,
                CloneOffers = true,
                ExtendExpiration = true,
                ManagePermissions = true,
                ViewAllOffers = true,
                ArchiveOffers = true
            },
            ["system"] = new OfferPermissions
            {
                ViewOffers = true,
                CreateOffers = true,
                EditOffers = true,
                DeleteOffers = true,
                ViewAnalytics = true,
                ManageSettings = true,
                BulkOperations = true,
                SendTestEmails = true,
                CloneOffers = true,
                ExtendExpiration = true,
                ManagePermissions = true,
                ViewAllOffers = true,
                ArchiveOffers = true
            }
        };

        private static readonly Dictionary<string, OfferAccessLevel> RoleAccessLevels = new()
        {
            ["guest"] = OfferAccessLevel.None,
            ["member"] = OfferAccessLevel.Viewer,
            ["editor"] = OfferAccessLevel.Editor,
            ["manager"] = OfferAccessLevel.Manager,
            ["admin"] = OfferAccessLevel.Administrator,
            ["system"] = OfferAccessLevel.Administrator
        };

        public OfferAuthorizationService(
            IPromotionalOfferRepository offerRepository,
            ITenantContext tenantContext,
            UserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            RoleManager<MongoIdentityRole<Guid>> roleManager,
            ICacheService? cache = null,
            ILogger<OfferAuthorizationService>? logger = null)
        {
            _offerRepository = offerRepository;
            _tenantContext = tenantContext;
            _userRepository = userRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> CanCreateOfferAsync(string tenantId, string userId)
        {
            // Check tenant access first
            if (!HasTenantAccess(tenantId))
            {
                return false;
            }

            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            return permissions.CreateOffers;
        }

        public async Task<bool> CanUpdateOfferAsync(string tenantId, string offerId, string userId)
        {
            // Check tenant access first
            if (!HasTenantAccess(tenantId))
            {
                return false;
            }

            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            if (!permissions.EditOffers)
            {
                return false;
            }

            // Check if user can only edit their own offers
            var restrictions = await GetUserRestrictionsAsync(tenantId, userId);
            if (restrictions.CanOnlyEditOwnOffers)
            {
                var offer = await _offerRepository.GetByIdAsync(tenantId, offerId);
                return offer?.CreatedByUserId == userId;
            }

            return true;
        }

        public async Task<bool> CanDeleteOfferAsync(string tenantId, string offerId, string userId)
        {
            // Check tenant access first
            if (!HasTenantAccess(tenantId))
            {
                return false;
            }

            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            if (!permissions.DeleteOffers)
            {
                return false;
            }

            // Check if user can only delete their own offers
            var restrictions = await GetUserRestrictionsAsync(tenantId, userId);
            if (restrictions.CanOnlyEditOwnOffers)
            {
                var offer = await _offerRepository.GetByIdAsync(tenantId, offerId);
                return offer?.CreatedByUserId == userId;
            }

            return true;
        }

        public async Task<bool> CanViewOfferManagementAsync(string tenantId, string userId)
        {
            if (!HasTenantAccess(tenantId))
            {
                return false;
            }

            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            return permissions.ViewOffers;
        }

        public async Task<bool> CanViewAnalyticsAsync(string tenantId, string userId)
        {
            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            return permissions.ViewAnalytics;
        }

        public async Task<bool> CanPerformBulkOperationsAsync(string tenantId, string userId)
        {
            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            return permissions.BulkOperations;
        }

        public async Task<bool> CanManageOfferSettingsAsync(string tenantId, string userId)
        {
            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            return permissions.ManageSettings;
        }

        public async Task<bool> CanSendTestEmailsAsync(string tenantId, string userId)
        {
            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            return permissions.SendTestEmails;
        }

        public async Task<bool> CanCloneOffersAsync(string tenantId, string userId)
        {
            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            return permissions.CloneOffers;
        }

        public async Task<bool> CanExtendExpirationAsync(string tenantId, string userId)
        {
            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            return permissions.ExtendExpiration;
        }

        public async Task<OfferAccessLevel> GetUserAccessLevelAsync(string tenantId, string userId)
        {
            var cacheKey = $"offer_access_level:{tenantId}:{userId}";

            if (_cache != null)
            {
                var cached = await _cache.GetAsync<OfferAccessLevel?>(cacheKey);
                if (cached.HasValue) return cached.Value;
            }

            var userRole = await GetUserRoleAsync(tenantId, userId);
            var accessLevel = RoleAccessLevels.TryGetValue(userRole, out var level) ? level : OfferAccessLevel.None;

            if (_cache != null)
            {
                await _cache.SetAsync(cacheKey, accessLevel, TimeSpan.FromMinutes(30));
            }

            return accessLevel;
        }

        public async Task<OfferPermissions> GetUserPermissionsAsync(string tenantId, string userId)
        {
            var cacheKey = $"offer_permissions:{tenantId}:{userId}";

            if (_cache != null)
            {
                var cached = await _cache.GetAsync<OfferPermissions>(cacheKey);
                if (cached != null) return cached;
            }

            var userRole = await GetUserRoleAsync(tenantId, userId);
            var permissions = GetRolePermissions(userRole);

            // Apply any custom permissions or restrictions
            var customPermissions = await GetCustomPermissionsAsync(tenantId, userId);
            if (customPermissions != null)
            {
                ApplyCustomPermissions(permissions, customPermissions);
            }

            if (_cache != null)
            {
                await _cache.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(30));
            }

            return permissions;
        }

        public async Task<AuthorizationResult> ValidatePermissionAsync(string tenantId, string userId, OfferPermission permission)
        {
            var permissions = await GetUserPermissionsAsync(tenantId, userId);
            var hasPermission = permissions.HasPermission(permission);

            if (hasPermission)
            {
                return AuthorizationResult.Allow();
            }

            var userLevel = await GetUserAccessLevelAsync(tenantId, userId);
            var requiredLevel = GetRequiredAccessLevel(permission);

            return AuthorizationResult.Deny(
                "INSUFFICIENT_PERMISSIONS",
                $"User does not have permission: {permission}",
                requiredLevel,
                userLevel);
        }

        public async Task<AuthorizationResult> ValidateOfferAccessAsync(string tenantId, string offerId, string userId, OfferAccessType accessType)
        {
            var offer = await _offerRepository.GetByIdAsync(tenantId, offerId);
            if (offer == null)
            {
                return AuthorizationResult.Deny("OFFER_NOT_FOUND", "Offer not found");
            }

            var permissions = await GetUserPermissionsAsync(tenantId, userId);

            var hasAccess = accessType switch
            {
                OfferAccessType.View => permissions.ViewOffers,
                OfferAccessType.Edit => permissions.EditOffers,
                OfferAccessType.Delete => permissions.DeleteOffers,
                OfferAccessType.Clone => permissions.CloneOffers,
                OfferAccessType.Analytics => permissions.ViewAnalytics,
                OfferAccessType.Redeem => true, // Redemption has separate validation
                _ => false
            };

            if (!hasAccess)
            {
                return AuthorizationResult.Deny("INSUFFICIENT_PERMISSIONS", $"User cannot {accessType.ToString().ToLower()} offers");
            }

            // Check ownership restrictions
            var restrictions = await GetUserRestrictionsAsync(tenantId, userId);
            if ((restrictions.CanOnlyViewOwnOffers && accessType == OfferAccessType.View) ||
                (restrictions.CanOnlyEditOwnOffers && (accessType == OfferAccessType.Edit || accessType == OfferAccessType.Delete)))
            {
                if (offer.CreatedByUserId != userId)
                {
                    return AuthorizationResult.Deny("OWNERSHIP_REQUIRED", "User can only access their own offers");
                }
            }

            // Check service plan restrictions
            if (restrictions.AllowedServicePlans.Any() && !string.IsNullOrWhiteSpace(offer.ServicePlanId))
            {
                if (!restrictions.AllowedServicePlans.Contains(offer.ServicePlanId))
                {
                    return AuthorizationResult.Deny("SERVICE_PLAN_RESTRICTED", "User cannot access offers for this service plan");
                }
            }

            return AuthorizationResult.Allow();
        }

        public async Task<List<string>> GetUsersWithPermissionAsync(string tenantId, OfferPermission permission)
        {
            // In a real implementation, this would query a user/role service
            // For now, return empty list
            await Task.CompletedTask;
            return new List<string>();
        }

        public async Task<bool> CanRedeemOffersAsync(string tenantId, string userId)
        {
            // Basic check - user must be authenticated to redeem member-only offers
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            // Check if user has active membership/account
            var restrictions = await GetUserRestrictionsAsync(tenantId, userId);
            if (restrictions.AccessExpiresAt.HasValue && restrictions.AccessExpiresAt.Value < DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        public async Task<OfferRestrictions> GetUserRestrictionsAsync(string tenantId, string userId)
        {
            var cacheKey = $"offer_restrictions:{tenantId}:{userId}";

            if (_cache != null)
            {
                var cached = await _cache.GetAsync<OfferRestrictions>(cacheKey);
                if (cached != null) return cached;
            }

            var userRole = await GetUserRoleAsync(tenantId, userId);
            var restrictions = GetDefaultRestrictions(userRole);

            // Apply custom restrictions
            var customRestrictions = await GetCustomRestrictionsAsync(tenantId, userId);
            if (customRestrictions != null)
            {
                ApplyCustomRestrictions(restrictions, customRestrictions);
            }

            if (_cache != null)
            {
                await _cache.SetAsync(cacheKey, restrictions, TimeSpan.FromMinutes(30));
            }

            return restrictions;
        }

        private async Task<string> GetUserRoleAsync(string tenantId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return "guest";
            }

            try
            {
                // Try to get user from repository
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return "guest";
                }

                var (user, _) = await _userRepository.LoadAsync(userGuid);
                if (user == null)
                {
                    return "guest";
                }

                // Check for super admin
                if (user.IsSuperAdmin || user.Tags.Contains("root") || user.Tags.Contains("system"))
                {
                    return "system";
                }

                // Get user roles
                var userRoles = await _userManager.GetRolesAsync(user);
                
                // Map BFormDomain roles to offer authorization roles
                if (userRoles.Contains(ApplicationRoles.UserAdministratorRole))
                {
                    return "admin";
                }
                
                if (userRoles.Contains(ApplicationRoles.SiteManagerRole))
                {
                    return "manager";
                }
                
                if (userRoles.Contains(ApplicationRoles.WorkItemManagerRole) || 
                    userRoles.Contains(ApplicationRoles.WorkSetManagerRole))
                {
                    return "editor";
                }

                // Default to member for authenticated users
                return "member";
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error getting user role for user {UserId} in tenant {TenantId}", userId, tenantId);
                return "guest";
            }
        }

        private bool HasTenantAccess(string tenantId)
        {
            if (!Guid.TryParse(tenantId, out var tenantGuid))
            {
                return false;
            }

            // Check if current context has access to this tenant
            return _tenantContext.IsRootUser || _tenantContext.HasAccessToTenant(tenantGuid);
        }

        private async Task<Dictionary<string, object>?> GetCustomPermissionsAsync(string tenantId, string userId)
        {
            // In a real implementation, this would query custom permissions from database
            await Task.CompletedTask;
            return null;
        }

        private async Task<Dictionary<string, object>?> GetCustomRestrictionsAsync(string tenantId, string userId)
        {
            // In a real implementation, this would query custom restrictions from database
            await Task.CompletedTask;
            return null;
        }

        private static OfferPermissions GetRolePermissions(string role)
        {
            if (RolePermissions.TryGetValue(role, out var permissions))
            {
                // Return a copy to avoid modifying the original
                return new OfferPermissions
                {
                    ViewOffers = permissions.ViewOffers,
                    CreateOffers = permissions.CreateOffers,
                    EditOffers = permissions.EditOffers,
                    DeleteOffers = permissions.DeleteOffers,
                    ViewAnalytics = permissions.ViewAnalytics,
                    ManageSettings = permissions.ManageSettings,
                    BulkOperations = permissions.BulkOperations,
                    SendTestEmails = permissions.SendTestEmails,
                    CloneOffers = permissions.CloneOffers,
                    ExtendExpiration = permissions.ExtendExpiration,
                    ManagePermissions = permissions.ManagePermissions,
                    ViewAllOffers = permissions.ViewAllOffers,
                    ArchiveOffers = permissions.ArchiveOffers
                };
            }

            return new OfferPermissions(); // Default: no permissions
        }

        private static OfferRestrictions GetDefaultRestrictions(string role)
        {
            return role switch
            {
                "guest" => new OfferRestrictions
                {
                    CanOnlyViewOwnOffers = true,
                    CanOnlyEditOwnOffers = true,
                    MaxOffersPerMonth = 0,
                    ForbiddenOperations = new List<string> { "create", "edit", "delete", "bulk" }
                },
                "member" => new OfferRestrictions
                {
                    CanOnlyViewOwnOffers = true,
                    CanOnlyEditOwnOffers = true,
                    MaxOffersPerMonth = 0,
                    ForbiddenOperations = new List<string> { "create", "edit", "delete", "bulk" }
                },
                "editor" => new OfferRestrictions
                {
                    CanOnlyViewOwnOffers = false,
                    CanOnlyEditOwnOffers = true,
                    MaxOffersPerMonth = 10,
                    ForbiddenOperations = new List<string> { "bulk", "manage_permissions" }
                },
                "manager" => new OfferRestrictions
                {
                    CanOnlyViewOwnOffers = false,
                    CanOnlyEditOwnOffers = false,
                    MaxOffersPerMonth = 50,
                    ForbiddenOperations = new List<string> { "manage_permissions" }
                },
                _ => new OfferRestrictions
                {
                    CanOnlyViewOwnOffers = false,
                    CanOnlyEditOwnOffers = false,
                    MaxOffersPerMonth = -1 // Unlimited
                }
            };
        }

        private static void ApplyCustomPermissions(OfferPermissions permissions, Dictionary<string, object> customPermissions)
        {
            // Apply custom permission overrides
            foreach (var (key, value) in customPermissions)
            {
                if (value is bool boolValue)
                {
                    switch (key.ToLowerInvariant())
                    {
                        case "viewoffers": permissions.ViewOffers = boolValue; break;
                        case "createoffers": permissions.CreateOffers = boolValue; break;
                        case "editoffers": permissions.EditOffers = boolValue; break;
                        case "deleteoffers": permissions.DeleteOffers = boolValue; break;
                        case "viewanalytics": permissions.ViewAnalytics = boolValue; break;
                        case "managesettings": permissions.ManageSettings = boolValue; break;
                        case "bulkoperations": permissions.BulkOperations = boolValue; break;
                        case "sendtestemails": permissions.SendTestEmails = boolValue; break;
                        case "cloneoffers": permissions.CloneOffers = boolValue; break;
                        case "extendexpiration": permissions.ExtendExpiration = boolValue; break;
                        case "managepermissions": permissions.ManagePermissions = boolValue; break;
                        case "viewalloffers": permissions.ViewAllOffers = boolValue; break;
                        case "archiveoffers": permissions.ArchiveOffers = boolValue; break;
                    }
                }
            }
        }

        private static void ApplyCustomRestrictions(OfferRestrictions restrictions, Dictionary<string, object> customRestrictions)
        {
            // Apply custom restriction overrides
            foreach (var (key, value) in customRestrictions)
            {
                switch (key.ToLowerInvariant())
                {
                    case "canonlyviewownoffers" when value is bool boolValue:
                        restrictions.CanOnlyViewOwnOffers = boolValue;
                        break;
                    case "canonlyeditownoffers" when value is bool boolValue:
                        restrictions.CanOnlyEditOwnOffers = boolValue;
                        break;
                    case "maxofferspermonth" when value is int intValue:
                        restrictions.MaxOffersPerMonth = intValue;
                        break;
                    case "allowedserviceplans" when value is List<string> listValue:
                        restrictions.AllowedServicePlans = listValue;
                        break;
                    case "accessexpiresat" when value is DateTime dateValue:
                        restrictions.AccessExpiresAt = dateValue;
                        break;
                }
            }
        }

        private static OfferAccessLevel GetRequiredAccessLevel(OfferPermission permission)
        {
            return permission switch
            {
                OfferPermission.ViewOffers => OfferAccessLevel.Viewer,
                OfferPermission.CreateOffers => OfferAccessLevel.Editor,
                OfferPermission.EditOffers => OfferAccessLevel.Editor,
                OfferPermission.DeleteOffers => OfferAccessLevel.Manager,
                OfferPermission.ViewAnalytics => OfferAccessLevel.Manager,
                OfferPermission.ManageSettings => OfferAccessLevel.Administrator,
                OfferPermission.BulkOperations => OfferAccessLevel.Administrator,
                OfferPermission.SendTestEmails => OfferAccessLevel.Manager,
                OfferPermission.CloneOffers => OfferAccessLevel.Editor,
                OfferPermission.ExtendExpiration => OfferAccessLevel.Manager,
                OfferPermission.ManagePermissions => OfferAccessLevel.Administrator,
                OfferPermission.ViewAllOffers => OfferAccessLevel.Manager,
                OfferPermission.ArchiveOffers => OfferAccessLevel.Manager,
                _ => OfferAccessLevel.Administrator
            };
        }
    }
}