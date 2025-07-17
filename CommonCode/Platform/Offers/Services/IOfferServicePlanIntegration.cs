using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Services
{
    /// <summary>
    /// Interface for integrating promotional offers with tenant service plans
    /// </summary>
    public interface IOfferServicePlanIntegration
    {
        /// <summary>
        /// Checks if a tenant can create a new promotional offer based on their plan limits
        /// </summary>
        Task<ServicePlanCheckResult> CanCreateOfferAsync(string tenantId);

        /// <summary>
        /// Checks if a tenant can use a specific offer feature based on their plan
        /// </summary>
        Task<bool> CanUseFeatureAsync(string tenantId, string featureName);

        /// <summary>
        /// Gets the offer-related limits for a tenant's service plan
        /// </summary>
        Task<OfferServicePlanLimits> GetOfferLimitsAsync(string tenantId);

        /// <summary>
        /// Tracks offer-related usage against plan limits
        /// </summary>
        Task TrackOfferUsageAsync(string tenantId, string offerId, OfferUsageType usageType);

        /// <summary>
        /// Gets current offer usage statistics for a tenant
        /// </summary>
        Task<OfferUsageStatistics> GetOfferUsageAsync(string tenantId);

        /// <summary>
        /// Validates if an offer's features are compatible with the service plan
        /// </summary>
        Task<OfferPlanCompatibilityResult> ValidateOfferCompatibilityAsync(string tenantId, string servicePlanId);

        /// <summary>
        /// Gets available offer features for a specific service plan
        /// </summary>
        Task<List<OfferFeature>> GetAvailableFeaturesAsync(string servicePlanId);

        /// <summary>
        /// Checks if a tenant has reached their monthly offer creation limit
        /// </summary>
        Task<bool> HasReachedMonthlyLimitAsync(string tenantId);

        /// <summary>
        /// Resets monthly usage counters (typically called by a scheduled job)
        /// </summary>
        Task ResetMonthlyUsageAsync(string tenantId);
    }

    /// <summary>
    /// Result of a service plan check
    /// </summary>
    public class ServicePlanCheckResult
    {
        public bool CanProceed { get; set; }
        public string? ReasonCode { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public ServicePlanLimitInfo? LimitInfo { get; set; }
    }

    /// <summary>
    /// Information about service plan limits
    /// </summary>
    public class ServicePlanLimitInfo
    {
        public int CurrentUsage { get; set; }
        public int? MaxAllowed { get; set; }
        public int? Remaining => MaxAllowed.HasValue ? MaxAllowed.Value - CurrentUsage : null;
        public DateTime? ResetsAt { get; set; }
    }

    /// <summary>
    /// Offer-specific limits from a service plan
    /// </summary>
    public class OfferServicePlanLimits
    {
        public int? MaxPromotionalOffers { get; set; }
        public int? MaxActiveOffers { get; set; }
        public int? MaxOffersPerMonth { get; set; }
        public int? MaxSpecialCodeOffers { get; set; }
        public int? MaxGiveawayPercent { get; set; }
        public bool AllowEmailCustomization { get; set; } = true;
        public bool AllowAdminTasks { get; set; } = true;
        public bool AllowAnalytics { get; set; } = true;
        public bool AllowBulkOperations { get; set; } = true;
        public bool AllowExpiringOffers { get; set; } = true;
        public int? MaxEmailTemplateLength { get; set; }
        public List<string> AllowedVisibilityTypes { get; set; } = new();
        public Dictionary<string, object> CustomLimits { get; set; } = new();
    }

    /// <summary>
    /// Types of offer-related usage to track
    /// </summary>
    public enum OfferUsageType
    {
        Created,
        Updated,
        Deleted,
        Redeemed,
        EmailSent,
        AnalyticsViewed,
        BulkOperation,
        SpecialCodeGenerated
    }

    /// <summary>
    /// Offer usage statistics for a tenant
    /// </summary>
    public class OfferUsageStatistics
    {
        public string TenantId { get; set; } = string.Empty;
        public int TotalOffers { get; set; }
        public int ActiveOffers { get; set; }
        public int OffersCreatedThisMonth { get; set; }
        public int OffersRedeemedThisMonth { get; set; }
        public int EmailsSentThisMonth { get; set; }
        public DateTime LastOfferCreated { get; set; }
        public DateTime LastOfferRedeemed { get; set; }
        public Dictionary<OfferUsageType, int> UsageByType { get; set; } = new();
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    /// <summary>
    /// Result of offer compatibility check with service plan
    /// </summary>
    public class OfferPlanCompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public List<string> IncompatibleFeatures { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Represents an offer-related feature in a service plan
    /// </summary>
    public class OfferFeature
    {
        public string FeatureId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FeatureCategory Category { get; set; }
        public bool IsEnabled { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    /// <summary>
    /// Categories of offer features
    /// </summary>
    public enum FeatureCategory
    {
        Core,
        Marketing,
        Analytics,
        Automation,
        Advanced,
        Enterprise
    }
}