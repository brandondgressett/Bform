using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Payment.Services
{
    /// <summary>
    /// Core interface for payment processing with offer integration
    /// </summary>
    public interface IPaymentProcessor
    {
        /// <summary>
        /// Processes a payment with optional promotional offer discount
        /// </summary>
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);

        /// <summary>
        /// Creates a payment intent for deferred processing
        /// </summary>
        Task<PaymentIntentResult> CreatePaymentIntentAsync(PaymentIntentRequest request);

        /// <summary>
        /// Confirms a previously created payment intent
        /// </summary>
        Task<PaymentResult> ConfirmPaymentIntentAsync(string paymentIntentId, string tenantId);

        /// <summary>
        /// Cancels a payment intent
        /// </summary>
        Task<bool> CancelPaymentIntentAsync(string paymentIntentId, string tenantId);

        /// <summary>
        /// Processes a refund for a completed payment
        /// </summary>
        Task<RefundResult> ProcessRefundAsync(RefundRequest request);

        /// <summary>
        /// Calculates payment amount with promotional offer applied
        /// </summary>
        Task<PaymentCalculationResult> CalculatePaymentAsync(PaymentCalculationRequest request);

        /// <summary>
        /// Validates payment method information
        /// </summary>
        Task<PaymentMethodValidationResult> ValidatePaymentMethodAsync(PaymentMethodInfo paymentMethod);

        /// <summary>
        /// Gets supported payment methods for a tenant
        /// </summary>
        Task<List<PaymentMethodType>> GetSupportedPaymentMethodsAsync(string tenantId);

        /// <summary>
        /// Handles webhook events from payment providers
        /// </summary>
        Task<WebhookHandleResult> HandleWebhookAsync(string tenantId, string provider, string payload, Dictionary<string, string> headers);
    }

    /// <summary>
    /// Request for processing a payment
    /// </summary>
    public class PaymentRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public PaymentMethodInfo PaymentMethod { get; set; } = new();
        public string? PromotionalOfferId { get; set; }
        public string? SpecialOfferCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Request for creating a payment intent
    /// </summary>
    public class PaymentIntentRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? PromotionalOfferId { get; set; }
        public string? SpecialOfferCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
        public bool CaptureMethod { get; set; } = true; // Auto-capture by default
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Request for calculating payment amounts
    /// </summary>
    public class PaymentCalculationRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public decimal OriginalAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? PromotionalOfferId { get; set; }
        public string? SpecialOfferCode { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Request for processing a refund
    /// </summary>
    public class RefundRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public decimal? Amount { get; set; } // Null for full refund
        public string Reason { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Payment method information
    /// </summary>
    public class PaymentMethodInfo
    {
        public PaymentMethodType Type { get; set; }
        public string? Token { get; set; } // Tokenized payment method
        public CreditCardInfo? CreditCard { get; set; }
        public BankAccountInfo? BankAccount { get; set; }
        public DigitalWalletInfo? DigitalWallet { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Credit card payment method details
    /// </summary>
    public class CreditCardInfo
    {
        public string Number { get; set; } = string.Empty; // Should be masked/tokenized
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string? CVV { get; set; } // Should not be stored
        public string? HolderName { get; set; }
        public BillingAddress? BillingAddress { get; set; }
    }

    /// <summary>
    /// Bank account payment method details
    /// </summary>
    public class BankAccountInfo
    {
        public string AccountNumber { get; set; } = string.Empty; // Should be masked/tokenized
        public string RoutingNumber { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty; // checking, savings
        public string? HolderName { get; set; }
    }

    /// <summary>
    /// Digital wallet payment method details
    /// </summary>
    public class DigitalWalletInfo
    {
        public string WalletType { get; set; } = string.Empty; // apple_pay, google_pay, paypal
        public string? Token { get; set; }
        public Dictionary<string, string> WalletData { get; set; } = new();
    }

    /// <summary>
    /// Billing address information
    /// </summary>
    public class BillingAddress
    {
        public string Line1 { get; set; } = string.Empty;
        public string? Line2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of payment methods
    /// </summary>
    public enum PaymentMethodType
    {
        CreditCard,
        DebitCard,
        BankTransfer,
        ACH,
        ApplePay,
        GooglePay,
        PayPal,
        Cryptocurrency,
        StoreCredit
    }

    /// <summary>
    /// Result of a payment processing operation
    /// </summary>
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? PaymentId { get; set; }
        public string? TransactionId { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ProcessedAmount { get; set; }
        public decimal Fees { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string? PromotionalOfferId { get; set; }
        public PaymentProviderInfo ProviderInfo { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of creating a payment intent
    /// </summary>
    public class PaymentIntentResult
    {
        public bool Success { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentIntentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of payment calculation
    /// </summary>
    public class PaymentCalculationResult
    {
        public decimal OriginalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal EstimatedFees { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? PromotionalOfferId { get; set; }
        public string? OfferName { get; set; }
        public bool IsValidPromotion { get; set; }
        public List<string> AppliedDiscounts { get; set; } = new();
        public Dictionary<string, object> Breakdown { get; set; } = new();
    }

    /// <summary>
    /// Result of payment method validation
    /// </summary>
    public class PaymentMethodValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public PaymentMethodType? DetectedType { get; set; }
        public Dictionary<string, object> ValidationDetails { get; set; } = new();
    }

    /// <summary>
    /// Result of refund processing
    /// </summary>
    public class RefundResult
    {
        public bool Success { get; set; }
        public string? RefundId { get; set; }
        public decimal RefundedAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of webhook handling
    /// </summary>
    public class WebhookHandleResult
    {
        public bool Success { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? ProcessedEventId { get; set; }
        public List<string> ActionsPerformed { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> EventData { get; set; } = new();
    }

    /// <summary>
    /// Information about the payment provider
    /// </summary>
    public class PaymentProviderInfo
    {
        public string Provider { get; set; } = string.Empty; // stripe, paypal, etc.
        public string? ProviderTransactionId { get; set; }
        public string? ProviderPaymentMethodId { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Payment processing status
    /// </summary>
    public enum PaymentStatus
    {
        Pending,
        Processing,
        Succeeded,
        Failed,
        Canceled,
        RequiresAction,
        RequiresConfirmation,
        PartiallyRefunded,
        Refunded
    }

    /// <summary>
    /// Payment intent status
    /// </summary>
    public enum PaymentIntentStatus
    {
        Created,
        RequiresPaymentMethod,
        RequiresConfirmation,
        RequiresAction,
        Processing,
        Succeeded,
        Canceled
    }

    /// <summary>
    /// Refund processing status
    /// </summary>
    public enum RefundStatus
    {
        Pending,
        Processing,
        Succeeded,
        Failed,
        Canceled
    }
}