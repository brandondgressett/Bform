using BFormDomain.CommonCode.Platform.Payment.Domain;
using BFormDomain.CommonCode.Platform.Payment.Services;
using BFormDomain.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaymentEntity = BFormDomain.CommonCode.Platform.Payment.Domain.Payment;

namespace BFormDomain.CommonCode.Platform.Payment.Repository
{
    /// <summary>
    /// Repository interface for payment operations
    /// </summary>
    public interface IPaymentRepository : IRepository<PaymentEntity>
    {
        /// <summary>
        /// Gets a payment by ID for a specific tenant
        /// </summary>
        Task<PaymentEntity?> GetByIdAsync(string tenantId, string paymentId);

        /// <summary>
        /// Gets payments for a specific user
        /// </summary>
        Task<List<PaymentEntity>> GetByUserAsync(string tenantId, string userId, int skip = 0, int take = 50);

        /// <summary>
        /// Gets payments for a specific order
        /// </summary>
        Task<List<PaymentEntity>> GetByOrderAsync(string tenantId, string orderId);

        /// <summary>
        /// Gets payments by status
        /// </summary>
        Task<List<PaymentEntity>> GetByStatusAsync(string tenantId, PaymentStatus status, int skip = 0, int take = 50);

        /// <summary>
        /// Gets payments in a date range
        /// </summary>
        Task<List<PaymentEntity>> GetByDateRangeAsync(string tenantId, DateTime startDate, DateTime endDate, int skip = 0, int take = 50);

        /// <summary>
        /// Gets payments by promotional offer
        /// </summary>
        Task<List<PaymentEntity>> GetByPromotionalOfferAsync(string tenantId, string offerId, int skip = 0, int take = 50);

        /// <summary>
        /// Searches payments based on criteria
        /// </summary>
        Task<PaymentSearchResult> SearchPaymentsAsync(string tenantId, PaymentSearchCriteria criteria);

        /// <summary>
        /// Gets payment statistics for a tenant
        /// </summary>
        Task<PaymentStatistics> GetPaymentStatisticsAsync(string tenantId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets failed payments that might need retry
        /// </summary>
        Task<List<PaymentEntity>> GetFailedPaymentsAsync(string tenantId, DateTime afterDate, int skip = 0, int take = 50);

        /// <summary>
        /// Gets disputed payments
        /// </summary>
        Task<List<PaymentEntity>> GetDisputedPaymentsAsync(string tenantId, int skip = 0, int take = 50);

        /// <summary>
        /// Updates payment status
        /// </summary>
        Task<bool> UpdatePaymentStatusAsync(string tenantId, string paymentId, PaymentStatus status, string? errorCode = null, string? errorMessage = null);

        /// <summary>
        /// Adds a refund to a payment
        /// </summary>
        Task<bool> AddRefundAsync(string tenantId, string paymentId, PaymentRefund refund);

        /// <summary>
        /// Sets dispute information for a payment
        /// </summary>
        Task<bool> SetDisputeAsync(string tenantId, string paymentId, PaymentDispute dispute);

        /// <summary>
        /// Gets payments requiring manual review
        /// </summary>
        Task<List<PaymentEntity>> GetPaymentsForReviewAsync(string tenantId, int skip = 0, int take = 50);

        /// <summary>
        /// Gets payment volume by time period
        /// </summary>
        Task<Dictionary<DateTime, PaymentVolume>> GetPaymentVolumeAsync(string tenantId, DateTime startDate, DateTime endDate, TimePeriod period);

        /// <summary>
        /// Gets payments by provider
        /// </summary>
        Task<List<PaymentEntity>> GetByProviderAsync(string tenantId, string providerName, int skip = 0, int take = 50);

        /// <summary>
        /// Counts payments by various criteria
        /// </summary>
        Task<long> CountPaymentsAsync(string tenantId, PaymentStatus? status = null, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets total payment amounts by various criteria
        /// </summary>
        Task<decimal> GetTotalAmountAsync(string tenantId, PaymentStatus? status = null, DateTime? startDate = null, DateTime? endDate = null);
    }

    /// <summary>
    /// Criteria for searching payments
    /// </summary>
    public class PaymentSearchCriteria
    {
        public string? SearchText { get; set; }
        public string? UserId { get; set; }
        public string? OrderId { get; set; }
        public PaymentStatus? Status { get; set; }
        public PaymentMethodType? PaymentMethodType { get; set; }
        public string? ProviderName { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PromotionalOfferId { get; set; }
        public bool? IsRefunded { get; set; }
        public bool? IsDisputed { get; set; }
        public List<string>? Tags { get; set; }
        public PaymentSortBy SortBy { get; set; } = PaymentSortBy.ProcessedAt;
        public bool SortDescending { get; set; } = true;
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }

    /// <summary>
    /// Result of payment search
    /// </summary>
    public class PaymentSearchResult
    {
        public List<PaymentEntity> Payments { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public PaymentSearchStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// Statistics from payment search
    /// </summary>
    public class PaymentSearchStatistics
    {
        public decimal TotalAmount { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalRefunded { get; set; }
        public int SuccessfulCount { get; set; }
        public int FailedCount { get; set; }
        public int DisputedCount { get; set; }
        public Dictionary<PaymentStatus, int> StatusCounts { get; set; } = new();
        public Dictionary<PaymentMethodType, int> MethodCounts { get; set; } = new();
    }

    /// <summary>
    /// Payment statistics for a tenant
    /// </summary>
    public class PaymentStatistics
    {
        public string TenantId { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalPayments { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public int DisputedPayments { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalRefunded { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal SuccessRate { get; set; }
        public Dictionary<PaymentMethodType, PaymentMethodStats> MethodStats { get; set; } = new();
        public Dictionary<string, decimal> RevenueByProvider { get; set; } = new();
        public List<TopPromotionalOffer> TopOffers { get; set; } = new();
    }

    /// <summary>
    /// Statistics for a payment method
    /// </summary>
    public class PaymentMethodStats
    {
        public PaymentMethodType Type { get; set; }
        public int Count { get; set; }
        public decimal Volume { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageAmount { get; set; }
    }

    /// <summary>
    /// Top promotional offer usage
    /// </summary>
    public class TopPromotionalOffer
    {
        public string OfferId { get; set; } = string.Empty;
        public string OfferName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalRevenueImpact { get; set; }
    }

    /// <summary>
    /// Payment volume for a time period
    /// </summary>
    public class PaymentVolume
    {
        public DateTime Period { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public int SuccessfulCount { get; set; }
        public int FailedCount { get; set; }
    }

    /// <summary>
    /// Sort options for payments
    /// </summary>
    public enum PaymentSortBy
    {
        ProcessedAt,
        Amount,
        Status,
        Provider,
        User,
        Order
    }

    /// <summary>
    /// Time period for aggregation
    /// </summary>
    public enum TimePeriod
    {
        Hour,
        Day,
        Week,
        Month,
        Quarter,
        Year
    }
}