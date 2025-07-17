using BFormDomain.CommonCode.Platform.Offers.Domain;
using BFormDomain.CommonCode.Platform.Offers.DTOs;
using BFormDomain.CommonCode.Platform.Offers.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Services
{
    /// <summary>
    /// Core service interface for promotional offer operations
    /// </summary>
    public interface IPromotionalOfferService
    {
        /// <summary>
        /// Creates a new promotional offer
        /// </summary>
        Task<PromotionalOffer> CreateOfferAsync(string tenantId, CreateOfferDto dto);

        /// <summary>
        /// Updates an existing promotional offer
        /// </summary>
        Task<PromotionalOffer> UpdateOfferAsync(string tenantId, UpdateOfferDto dto);

        /// <summary>
        /// Deletes a promotional offer
        /// </summary>
        Task<bool> DeleteOfferAsync(string tenantId, string offerId, string deletedBy);

        /// <summary>
        /// Gets a promotional offer by ID
        /// </summary>
        Task<PromotionalOffer?> GetOfferAsync(string tenantId, string offerId);

        /// <summary>
        /// Gets all active offers for a tenant
        /// </summary>
        Task<List<OfferDisplayDto>> GetActiveOffersAsync(string tenantId, string? userId = null, int? limit = null);

        /// <summary>
        /// Validates an offer for a user
        /// </summary>
        Task<OfferValidationResult> ValidateOfferAsync(string tenantId, ValidateOfferDto dto);

        /// <summary>
        /// Redeems an offer
        /// </summary>
        Task<RedeemOfferResultDto> RedeemOfferAsync(string tenantId, RedeemOfferDto dto);

        /// <summary>
        /// Searches offers based on criteria
        /// </summary>
        Task<OfferSearchResult> SearchOffersAsync(string tenantId, OfferSearchCriteria criteria);

        /// <summary>
        /// Gets offers by special code
        /// </summary>
        Task<OfferDisplayDto?> GetOfferByCodeAsync(string tenantId, string code, string? userId = null);

        /// <summary>
        /// Performs bulk operations on offers
        /// </summary>
        Task<BulkOperationResult> PerformBulkOperationAsync(string tenantId, BulkOfferOperationDto dto);

        /// <summary>
        /// Archives expired offers
        /// </summary>
        Task<int> ArchiveExpiredOffersAsync(string tenantId);

        /// <summary>
        /// Updates offer priorities
        /// </summary>
        Task UpdateOfferPrioritiesAsync(string tenantId, List<OfferPriorityUpdate> updates);

        /// <summary>
        /// Gets low stock offers
        /// </summary>
        Task<List<OfferDisplayDto>> GetLowStockOffersAsync(string tenantId, int thresholdPercentage = 10);

        /// <summary>
        /// Generates a unique special offer code
        /// </summary>
        Task<string> GenerateUniqueCodeAsync(string tenantId, int length = 8);

        /// <summary>
        /// Validates if a special code is available
        /// </summary>
        Task<bool> IsCodeAvailableAsync(string tenantId, string code, string? excludeOfferId = null);

        /// <summary>
        /// Gets offer statistics
        /// </summary>
        Task<OfferStatistics> GetOfferStatisticsAsync(string tenantId, string offerId);

        /// <summary>
        /// Clones an existing offer
        /// </summary>
        Task<PromotionalOffer> CloneOfferAsync(string tenantId, string offerId, string clonedBy);

        /// <summary>
        /// Sends test email for an offer
        /// </summary>
        Task<bool> SendTestEmailAsync(string tenantId, string offerId, string testEmail);

        /// <summary>
        /// Gets offers expiring soon
        /// </summary>
        Task<List<OfferDisplayDto>> GetExpiringOffersAsync(string tenantId, int daysAhead = 7);

        /// <summary>
        /// Extends offer expiration
        /// </summary>
        Task<bool> ExtendOfferExpirationAsync(string tenantId, string offerId, DateTime newExpiration, string extendedBy);

        /// <summary>
        /// Gets offer recommendations for a user
        /// </summary>
        Task<List<OfferDisplayDto>> GetRecommendedOffersAsync(string tenantId, string userId, int limit = 5);
    }

    /// <summary>
    /// Result of bulk operations
    /// </summary>
    public class BulkOperationResult
    {
        public int TotalItems { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkOperationError> Errors { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Error during bulk operation
    /// </summary>
    public class BulkOperationError
    {
        public string ItemId { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Offer statistics
    /// </summary>
    public class OfferStatistics
    {
        public string OfferId { get; set; } = string.Empty;
        public string OfferName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal ConversionRate { get; set; }
        public DateTime? LastSoldDate { get; set; }
        public TimeSpan? AveragePurchaseTime { get; set; }
        public Dictionary<string, int> SalesByDay { get; set; } = new();
        public List<string> TopUserIds { get; set; } = new();
    }
}