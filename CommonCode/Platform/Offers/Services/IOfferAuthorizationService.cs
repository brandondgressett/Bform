using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Services
{
    /// <summary>
    /// Interface for offer authorization and role-based access control
    /// </summary>
    public interface IOfferAuthorizationService
    {
        /// <summary>
        /// Checks if a user can create offers for a tenant
        /// </summary>
        Task<bool> CanCreateOfferAsync(string tenantId, string userId);

        /// <summary>
        /// Checks if a user can update a specific offer
        /// </summary>
        Task<bool> CanUpdateOfferAsync(string tenantId, string offerId, string userId);

        /// <summary>
        /// Checks if a user can delete a specific offer
        /// </summary>
        Task<bool> CanDeleteOfferAsync(string tenantId, string offerId, string userId);

        /// <summary>
        /// Checks if a user can view offer management features
        /// </summary>
        Task<bool> CanViewOfferManagementAsync(string tenantId, string userId);

        /// <summary>
        /// Checks if a user can view offer analytics
        /// </summary>
        Task<bool> CanViewAnalyticsAsync(string tenantId, string userId);

        /// <summary>
        /// Checks if a user can perform bulk operations
        /// </summary>
        Task<bool> CanPerformBulkOperationsAsync(string tenantId, string userId);

        /// <summary>
        /// Checks if a user can manage offer settings
        /// </summary>
        Task<bool> CanManageOfferSettingsAsync(string tenantId, string userId);

        /// <summary>
        /// Checks if a user can send test emails
        /// </summary>
        Task<bool> CanSendTestEmailsAsync(string tenantId, string userId);

        /// <summary>
        /// Checks if a user can clone offers
        /// </summary>
        Task<bool> CanCloneOffersAsync(string tenantId, string userId);

        /// <summary>
        /// Checks if a user can extend offer expirations
        /// </summary>
        Task<bool> CanExtendExpirationAsync(string tenantId, string userId);

        /// <summary>
        /// Gets the access level for a user in a tenant
        /// </summary>
        Task<OfferAccessLevel> GetUserAccessLevelAsync(string tenantId, string userId);

        /// <summary>
        /// Gets all permissions for a user in a tenant
        /// </summary>
        Task<OfferPermissions> GetUserPermissionsAsync(string tenantId, string userId);

        /// <summary>
        /// Validates that a user has the required permission
        /// </summary>
        Task<AuthorizationResult> ValidatePermissionAsync(string tenantId, string userId, OfferPermission permission);

        /// <summary>
        /// Validates that a user can access a specific offer
        /// </summary>
        Task<AuthorizationResult> ValidateOfferAccessAsync(string tenantId, string offerId, string userId, OfferAccessType accessType);

        /// <summary>
        /// Gets users with specific offer permissions
        /// </summary>
        Task<List<string>> GetUsersWithPermissionAsync(string tenantId, OfferPermission permission);

        /// <summary>
        /// Checks if a user can redeem offers (for member-only offers)
        /// </summary>
        Task<bool> CanRedeemOffersAsync(string tenantId, string userId);

        /// <summary>
        /// Gets offer restrictions for a user
        /// </summary>
        Task<OfferRestrictions> GetUserRestrictionsAsync(string tenantId, string userId);
    }

    /// <summary>
    /// Levels of access to offer management
    /// </summary>
    public enum OfferAccessLevel
    {
        None = 0,
        Viewer = 1,
        Editor = 2,
        Manager = 3,
        Administrator = 4
    }

    /// <summary>
    /// Types of access to offers
    /// </summary>
    public enum OfferAccessType
    {
        View,
        Edit,
        Delete,
        Clone,
        Redeem,
        Analytics
    }

    /// <summary>
    /// Specific permissions for offer operations
    /// </summary>
    public enum OfferPermission
    {
        ViewOffers,
        CreateOffers,
        EditOffers,
        DeleteOffers,
        ViewAnalytics,
        ManageSettings,
        BulkOperations,
        SendTestEmails,
        CloneOffers,
        ExtendExpiration,
        ManagePermissions,
        ViewAllOffers,
        ArchiveOffers
    }

    /// <summary>
    /// Set of permissions for a user
    /// </summary>
    public class OfferPermissions
    {
        public bool ViewOffers { get; set; }
        public bool CreateOffers { get; set; }
        public bool EditOffers { get; set; }
        public bool DeleteOffers { get; set; }
        public bool ViewAnalytics { get; set; }
        public bool ManageSettings { get; set; }
        public bool BulkOperations { get; set; }
        public bool SendTestEmails { get; set; }
        public bool CloneOffers { get; set; }
        public bool ExtendExpiration { get; set; }
        public bool ManagePermissions { get; set; }
        public bool ViewAllOffers { get; set; }
        public bool ArchiveOffers { get; set; }

        /// <summary>
        /// Checks if a specific permission is granted
        /// </summary>
        public bool HasPermission(OfferPermission permission)
        {
            return permission switch
            {
                OfferPermission.ViewOffers => ViewOffers,
                OfferPermission.CreateOffers => CreateOffers,
                OfferPermission.EditOffers => EditOffers,
                OfferPermission.DeleteOffers => DeleteOffers,
                OfferPermission.ViewAnalytics => ViewAnalytics,
                OfferPermission.ManageSettings => ManageSettings,
                OfferPermission.BulkOperations => BulkOperations,
                OfferPermission.SendTestEmails => SendTestEmails,
                OfferPermission.CloneOffers => CloneOffers,
                OfferPermission.ExtendExpiration => ExtendExpiration,
                OfferPermission.ManagePermissions => ManagePermissions,
                OfferPermission.ViewAllOffers => ViewAllOffers,
                OfferPermission.ArchiveOffers => ArchiveOffers,
                _ => false
            };
        }
    }

    /// <summary>
    /// Restrictions on a user's offer access
    /// </summary>
    public class OfferRestrictions
    {
        public bool CanOnlyViewOwnOffers { get; set; }
        public bool CanOnlyEditOwnOffers { get; set; }
        public List<string> AllowedServicePlans { get; set; } = new();
        public int MaxOffersPerMonth { get; set; } = -1; // -1 = unlimited
        public List<string> ForbiddenOperations { get; set; } = new();
        public DateTime? AccessExpiresAt { get; set; }
        public Dictionary<string, object> CustomRestrictions { get; set; } = new();
    }

    /// <summary>
    /// Result of an authorization check
    /// </summary>
    public class AuthorizationResult
    {
        public bool IsAuthorized { get; set; }
        public string? ReasonCode { get; set; }
        public string? Message { get; set; }
        public OfferAccessLevel RequiredLevel { get; set; }
        public OfferAccessLevel UserLevel { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public static AuthorizationResult Allow()
        {
            return new AuthorizationResult { IsAuthorized = true };
        }

        public static AuthorizationResult Deny(string reasonCode, string message, OfferAccessLevel required = OfferAccessLevel.None, OfferAccessLevel userLevel = OfferAccessLevel.None)
        {
            return new AuthorizationResult
            {
                IsAuthorized = false,
                ReasonCode = reasonCode,
                Message = message,
                RequiredLevel = required,
                UserLevel = userLevel
            };
        }
    }
}