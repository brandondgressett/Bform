using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Payment.Services
{
    /// <summary>
    /// Interface for payment provider implementations (Stripe, PayPal, etc.)
    /// </summary>
    public interface IPaymentProvider
    {
        /// <summary>
        /// The name of this payment provider
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Whether this provider is available for the tenant
        /// </summary>
        Task<bool> IsAvailableAsync(string tenantId);

        /// <summary>
        /// Processes a payment through this provider
        /// </summary>
        Task<ProviderPaymentResult> ProcessPaymentAsync(ProviderPaymentRequest request);

        /// <summary>
        /// Creates a payment intent for deferred processing
        /// </summary>
        Task<ProviderPaymentIntentResult> CreatePaymentIntentAsync(ProviderPaymentIntentRequest request);

        /// <summary>
        /// Confirms a payment intent
        /// </summary>
        Task<ProviderPaymentResult> ConfirmPaymentIntentAsync(string paymentIntentId);

        /// <summary>
        /// Cancels a payment intent
        /// </summary>
        Task<bool> CancelPaymentIntentAsync(string paymentIntentId);

        /// <summary>
        /// Processes a refund through this provider
        /// </summary>
        Task<ProviderRefundResult> ProcessRefundAsync(ProviderRefundRequest request);

        /// <summary>
        /// Gets supported payment methods for this provider
        /// </summary>
        Task<List<PaymentMethodType>> GetSupportedPaymentMethodsAsync();

        /// <summary>
        /// Handles webhook events from this provider
        /// </summary>
        Task<WebhookHandleResult> HandleWebhookAsync(string payload, Dictionary<string, string> headers);

        /// <summary>
        /// Validates configuration for this provider
        /// </summary>
        Task<ProviderConfigValidationResult> ValidateConfigurationAsync(string tenantId);
    }

    /// <summary>
    /// Factory for creating payment provider instances
    /// </summary>
    public interface IPaymentProviderFactory
    {
        /// <summary>
        /// Gets the best available provider for the tenant and payment method
        /// </summary>
        Task<IPaymentProvider> GetProviderAsync(string tenantId, PaymentMethodType paymentMethodType);

        /// <summary>
        /// Gets a provider by name
        /// </summary>
        Task<IPaymentProvider> GetProviderByNameAsync(string providerName);

        /// <summary>
        /// Gets all available providers for a tenant
        /// </summary>
        Task<List<IPaymentProvider>> GetAvailableProvidersAsync(string tenantId);

        /// <summary>
        /// Registers a new payment provider
        /// </summary>
        void RegisterProvider(IPaymentProvider provider);
    }

    /// <summary>
    /// Request for provider payment processing
    /// </summary>
    public class ProviderPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public PaymentMethodInfo PaymentMethod { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Request for provider payment intent creation
    /// </summary>
    public class ProviderPaymentIntentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Description { get; set; } = string.Empty;
        public bool CaptureMethod { get; set; } = true;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Request for provider refund processing
    /// </summary>
    public class ProviderRefundRequest
    {
        public string PaymentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of provider payment processing
    /// </summary>
    public class ProviderPaymentResult
    {
        public bool Success { get; set; }
        public string? PaymentId { get; set; }
        public string? TransactionId { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public decimal Fees { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? PaymentMethodId { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Result of provider payment intent creation
    /// </summary>
    public class ProviderPaymentIntentResult
    {
        public bool Success { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }
        public PaymentIntentStatus Status { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Result of provider refund processing
    /// </summary>
    public class ProviderRefundResult
    {
        public bool Success { get; set; }
        public string? RefundId { get; set; }
        public decimal RefundedAmount { get; set; }
        public RefundStatus Status { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Result of provider configuration validation
    /// </summary>
    public class ProviderConfigValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> ConfigDetails { get; set; } = new();
    }
}