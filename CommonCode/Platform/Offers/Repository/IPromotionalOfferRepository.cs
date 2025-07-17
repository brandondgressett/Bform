using BFormDomain.CommonCode.Platform.Offers.Domain;
using BFormDomain.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Repository
{
    /// <summary>
    /// Repository interface for promotional offer operations
    /// </summary>
    public interface IPromotionalOfferRepository : IRepository<PromotionalOffer>
    {
        /// <summary>
        /// Gets an offer by ID for a specific tenant
        /// </summary>
        Task<PromotionalOffer?> GetByIdAsync(string tenantId, string offerId);

        /// <summary>
        /// Gets all active offers for a tenant, optionally limited
        /// </summary>
        Task<List<PromotionalOffer>> GetActiveOffersAsync(string tenantId, int? limit = null);

        /// <summary>
        /// Gets an offer by its special code
        /// </summary>
        Task<PromotionalOffer?> GetBySpecialCodeAsync(string tenantId, string code);

        /// <summary>
        /// Gets all offers associated with a service plan
        /// </summary>
        Task<List<PromotionalOffer>> GetByServicePlanAsync(string tenantId, string servicePlanId);

        /// <summary>
        /// Gets offers that will expire before a given date
        /// </summary>
        Task<List<PromotionalOffer>> GetExpiringOffersAsync(string tenantId, DateTime beforeDate);

        /// <summary>
        /// Searches offers based on criteria
        /// </summary>
        Task<OfferSearchResult> SearchOffersAsync(string tenantId, OfferSearchCriteria criteria);

        /// <summary>
        /// Increments the sold count for an offer
        /// </summary>
        Task<bool> IncrementSoldCountAsync(string tenantId, string offerId, int increment = 1);

        /// <summary>
        /// Updates priorities for multiple offers
        /// </summary>
        Task UpdateOfferPrioritiesAsync(string tenantId, List<OfferPriorityUpdate> updates);

        /// <summary>
        /// Checks if a special code is unique within a tenant
        /// </summary>
        Task<bool> IsSpecialCodeUniqueAsync(string tenantId, string code, string? excludeOfferId = null);

        /// <summary>
        /// Validates if an offer can be redeemed by a user
        /// </summary>
        Task<OfferValidationResult> ValidateOfferAsync(string tenantId, string offerId, string? userId);

        /// <summary>
        /// Gets offers visible to a specific user (or anonymous)
        /// </summary>
        Task<List<PromotionalOffer>> GetVisibleOffersAsync(string tenantId, string? userId = null, int? limit = null);

        /// <summary>
        /// Counts total offers for a tenant
        /// </summary>
        Task<long> CountOffersAsync(string tenantId, bool activeOnly = false);

        /// <summary>
        /// Gets offers created within a date range
        /// </summary>
        Task<List<PromotionalOffer>> GetOffersByDateRangeAsync(string tenantId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Archives an offer instead of deleting it
        /// </summary>
        Task ArchiveOfferAsync(string tenantId, string offerId);

        /// <summary>
        /// Bulk updates offer visibility
        /// </summary>
        Task BulkUpdateVisibilityAsync(string tenantId, List<string> offerIds, OfferVisibility visibility);

        /// <summary>
        /// Gets offers with low stock (approaching max quantity)
        /// </summary>
        Task<List<PromotionalOffer>> GetLowStockOffersAsync(string tenantId, int thresholdPercentage = 10);
    }

    /// <summary>
    /// Search criteria for offers
    /// </summary>
    public class OfferSearchCriteria
    {
        public string? SearchText { get; set; }
        public bool? IsActive { get; set; }
        public OfferVisibility? Visibility { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public DateTime? ExpiringBefore { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public string? ServicePlanId { get; set; }
        public bool? HasSpecialCode { get; set; }
        public TargetAudience? TargetAudience { get; set; }
        public int? MinServiceUnits { get; set; }
        public int? MaxServiceUnits { get; set; }
        public List<string>? Tags { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
        public OfferSortBy SortBy { get; set; } = OfferSortBy.Priority;
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Sort options for offers
    /// </summary>
    public enum OfferSortBy
    {
        Priority,
        Name,
        Price,
        CreatedAt,
        UpdatedAt,
        SoldCount,
        ExpiresAt,
        ServiceUnitCount
    }

    /// <summary>
    /// Search result with metadata
    /// </summary>
    public class OfferSearchResult
    {
        public List<PromotionalOffer> Offers { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasMore => PageNumber < TotalPages - 1;
    }

    /// <summary>
    /// Priority update model
    /// </summary>
    public class OfferPriorityUpdate
    {
        public string OfferId { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    /// <summary>
    /// Offer validation result
    /// </summary>
    public class OfferValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public PromotionalOffer? Offer { get; set; }
        public ValidationFailureReason? FailureReason { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Reasons for validation failure
    /// </summary>
    public enum ValidationFailureReason
    {
        NotFound,
        Inactive,
        Expired,
        SoldOut,
        InvalidVisibility,
        InvalidCode,
        UserNotAuthorized,
        Other
    }
}