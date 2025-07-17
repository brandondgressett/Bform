using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Payment.Services;
using BFormDomain.DataModels;
using System;
using System.Collections.Generic;

namespace BFormDomain.CommonCode.Platform.Payment.Domain
{
    /// <summary>
    /// Payment transaction record
    /// </summary>
    public class Payment : AppEntityBase
    {
        public Payment()
        {
            EntityType = "Payment";
        }

        /// <summary>
        /// User who made the payment
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Original amount before any discounts
        /// </summary>
        public decimal OriginalAmount { get; set; }

        /// <summary>
        /// Total discount amount applied
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Final amount that was actually processed
        /// </summary>
        public decimal ProcessedAmount { get; set; }

        /// <summary>
        /// Payment processing fees
        /// </summary>
        public decimal Fees { get; set; }

        /// <summary>
        /// Currency code (USD, EUR, etc.)
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Current status of the payment
        /// </summary>
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// Type of payment method used
        /// </summary>
        public PaymentMethodType PaymentMethodType { get; set; }

        /// <summary>
        /// Name of the payment provider used
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Payment ID from the provider
        /// </summary>
        public string? PaymentId { get; set; }

        /// <summary>
        /// Transaction ID from the provider
        /// </summary>
        public string? ProviderTransactionId { get; set; }

        /// <summary>
        /// Payment method ID from the provider
        /// </summary>
        public string? ProviderPaymentMethodId { get; set; }

        /// <summary>
        /// Promotional offer that was applied (if any)
        /// </summary>
        public string? PromotionalOfferId { get; set; }

        /// <summary>
        /// Special offer code that was used (if any)
        /// </summary>
        public string? SpecialOfferCode { get; set; }

        /// <summary>
        /// Order ID that this payment is for
        /// </summary>
        public string? OrderId { get; set; }

        /// <summary>
        /// Description of what the payment is for
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When the payment was processed
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// When the payment was authorized (may be different from processed)
        /// </summary>
        public DateTime? AuthorizedAt { get; set; }

        /// <summary>
        /// When the payment was captured (for auth-only payments)
        /// </summary>
        public DateTime? CapturedAt { get; set; }

        /// <summary>
        /// Related payment intent ID (if created via intent flow)
        /// </summary>
        public string? PaymentIntentId { get; set; }

        /// <summary>
        /// Error code if payment failed
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Error message if payment failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// IP address of the user when payment was made
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent of the user when payment was made
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Whether this payment has been refunded
        /// </summary>
        public bool IsRefunded { get; set; }

        /// <summary>
        /// Amount that has been refunded
        /// </summary>
        public decimal RefundedAmount { get; set; }

        /// <summary>
        /// When the payment was refunded (if applicable)
        /// </summary>
        public DateTime? RefundedAt { get; set; }

        /// <summary>
        /// Billing address for the payment
        /// </summary>
        public PaymentBillingAddress? BillingAddress { get; set; }

        /// <summary>
        /// Additional metadata about the payment
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Provider-specific data
        /// </summary>
        public Dictionary<string, object> ProviderData { get; set; } = new();

        /// <summary>
        /// List of refunds associated with this payment
        /// </summary>
        public List<PaymentRefund> Refunds { get; set; } = new();

        /// <summary>
        /// Risk assessment score (0-100, higher = riskier)
        /// </summary>
        public int? RiskScore { get; set; }

        /// <summary>
        /// Risk assessment details
        /// </summary>
        public Dictionary<string, object> RiskData { get; set; } = new();

        /// <summary>
        /// Whether the payment is disputed
        /// </summary>
        public bool IsDisputed { get; set; }

        /// <summary>
        /// Dispute information if applicable
        /// </summary>
        public PaymentDispute? Dispute { get; set; }


        /// <summary>
        /// Calculates the net amount (processed - fees - refunded)
        /// </summary>
        public decimal NetAmount => ProcessedAmount - Fees - RefundedAmount;

        /// <summary>
        /// Whether the payment can be refunded
        /// </summary>
        public bool CanBeRefunded => 
            Status == PaymentStatus.Succeeded && 
            !IsRefunded && 
            ProcessedAmount > RefundedAmount;

        /// <summary>
        /// Whether the payment is fully refunded
        /// </summary>
        public bool IsFullyRefunded => 
            IsRefunded && RefundedAmount >= ProcessedAmount;

        public override Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
        {
            var baseUri = $"/payments/{(template ? "template" : "instance")}/{Id}";
            if (vm)
                baseUri += "/vm";
            if (!string.IsNullOrWhiteSpace(queryParameters))
                baseUri += $"?{queryParameters}";
            return new Uri(baseUri, UriKind.Relative);
        }
    }

    /// <summary>
    /// Billing address for payment
    /// </summary>
    public class PaymentBillingAddress
    {
        public string Line1 { get; set; } = string.Empty;
        public string? Line2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    /// <summary>
    /// Payment refund record
    /// </summary>
    public class PaymentRefund
    {
        public string RefundId { get; set; } = string.Empty;
        public string? ProviderRefundId { get; set; }
        public decimal Amount { get; set; }
        public RefundStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Payment dispute information
    /// </summary>
    public class PaymentDispute
    {
        public string DisputeId { get; set; } = string.Empty;
        public string? ProviderDisputeId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DisputeStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? Evidence { get; set; }
        public Dictionary<string, object> DisputeData { get; set; } = new();
    }

    /// <summary>
    /// Dispute status enumeration
    /// </summary>
    public enum DisputeStatus
    {
        Open,
        UnderReview,
        Won,
        Lost,
        Accepted
    }
}