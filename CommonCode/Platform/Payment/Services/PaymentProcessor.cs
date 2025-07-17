using BFormDomain.CommonCode.Platform.Offers.Services;
using BFormDomain.CommonCode.Platform.Offers.DTOs;
using BFormDomain.CommonCode.Platform.Payment.Repository;
using BFormDomain.CommonCode.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PaymentEntity = BFormDomain.CommonCode.Platform.Payment.Domain.Payment;

namespace BFormDomain.CommonCode.Platform.Payment.Services
{
    /// <summary>
    /// Main payment processor implementation with promotional offer integration
    /// </summary>
    public class PaymentProcessor : IPaymentProcessor
    {
        private readonly IPromotionalOfferService _offerService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentProviderFactory _providerFactory;
        private readonly ICacheService? _cache;
        private readonly ILogger<PaymentProcessor>? _logger;

        public PaymentProcessor(
            IPromotionalOfferService offerService,
            IPaymentRepository paymentRepository,
            IPaymentProviderFactory providerFactory,
            ICacheService? cache = null,
            ILogger<PaymentProcessor>? logger = null)
        {
            _offerService = offerService;
            _paymentRepository = paymentRepository;
            _providerFactory = providerFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                _logger?.LogInformation("Processing payment for tenant {TenantId}, amount {Amount}", 
                    request.TenantId, request.Amount);

                // Calculate final amount with promotional offers
                var calculation = await CalculatePaymentAsync(new PaymentCalculationRequest
                {
                    TenantId = request.TenantId,
                    UserId = request.UserId,
                    OriginalAmount = request.Amount,
                    Currency = request.Currency,
                    PromotionalOfferId = request.PromotionalOfferId,
                    SpecialOfferCode = request.SpecialOfferCode,
                    Metadata = request.Metadata
                });

                // If there's a promotional offer, validate and redeem it
                if (!string.IsNullOrWhiteSpace(request.PromotionalOfferId))
                {
                    var redemptionResult = await _offerService.RedeemOfferAsync(request.TenantId, new RedeemOfferDto
                    {
                        OfferId = request.PromotionalOfferId,
                        UserId = request.UserId,
                        SpecialCode = request.SpecialOfferCode,
                        PurchaseAmount = request.Amount,
                        OrderId = request.OrderId,
                        Metadata = request.Metadata
                    });

                    if (!redemptionResult.Success)
                    {
                        return new PaymentResult
                        {
                            Success = false,
                            Status = PaymentStatus.Failed,
                            ErrorCode = "OFFER_REDEMPTION_FAILED",
                            ErrorMessage = redemptionResult.ErrorMessage,
                            OriginalAmount = request.Amount,
                            Currency = request.Currency
                        };
                    }
                }

                // Get payment provider
                var provider = await _providerFactory.GetProviderAsync(request.TenantId, request.PaymentMethod.Type);

                // Process payment with provider
                var providerResult = await provider.ProcessPaymentAsync(new ProviderPaymentRequest
                {
                    Amount = calculation.FinalAmount,
                    Currency = request.Currency,
                    PaymentMethod = request.PaymentMethod,
                    Description = request.Description,
                    Metadata = request.Metadata
                });

                // Create payment record
                var paymentRecord = new PaymentEntity
                {
                    TenantId = request.TenantId,
                    UserId = request.UserId,
                    OriginalAmount = calculation.OriginalAmount,
                    DiscountAmount = calculation.DiscountAmount,
                    ProcessedAmount = calculation.FinalAmount,
                    Currency = request.Currency,
                    Status = providerResult.Success ? PaymentStatus.Succeeded : PaymentStatus.Failed,
                    PaymentMethodType = request.PaymentMethod.Type,
                    ProviderName = provider.ProviderName,
                    ProviderTransactionId = providerResult.TransactionId,
                    PromotionalOfferId = request.PromotionalOfferId,
                    OrderId = request.OrderId,
                    Description = request.Description,
                    ProcessedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };

                if (providerResult.Success)
                {
                    paymentRecord.PaymentId = providerResult.PaymentId;
                    _paymentRepository.Create(paymentRecord);
                }

                return new PaymentResult
                {
                    Success = providerResult.Success,
                    PaymentId = paymentRecord.Id.ToString(),
                    TransactionId = providerResult.TransactionId,
                    Status = paymentRecord.Status,
                    OriginalAmount = calculation.OriginalAmount,
                    DiscountAmount = calculation.DiscountAmount,
                    ProcessedAmount = calculation.FinalAmount,
                    Fees = providerResult.Fees,
                    Currency = request.Currency,
                    ErrorCode = providerResult.ErrorCode,
                    ErrorMessage = providerResult.ErrorMessage,
                    ProcessedAt = paymentRecord.ProcessedAt,
                    PromotionalOfferId = request.PromotionalOfferId,
                    ProviderInfo = new PaymentProviderInfo
                    {
                        Provider = provider.ProviderName,
                        ProviderTransactionId = providerResult.TransactionId,
                        ProviderPaymentMethodId = providerResult.PaymentMethodId,
                        ProviderData = providerResult.ProviderData
                    },
                    Metadata = request.Metadata.ToDictionary(k => k.Key, k => (object)k.Value)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing payment for tenant {TenantId}", request.TenantId);
                return new PaymentResult
                {
                    Success = false,
                    Status = PaymentStatus.Failed,
                    ErrorCode = "PROCESSING_ERROR",
                    ErrorMessage = "An error occurred while processing the payment",
                    OriginalAmount = request.Amount,
                    Currency = request.Currency
                };
            }
        }

        public async Task<PaymentIntentResult> CreatePaymentIntentAsync(PaymentIntentRequest request)
        {
            try
            {
                // Calculate amount with offers
                var calculation = await CalculatePaymentAsync(new PaymentCalculationRequest
                {
                    TenantId = request.TenantId,
                    UserId = request.UserId,
                    OriginalAmount = request.Amount,
                    Currency = request.Currency,
                    PromotionalOfferId = request.PromotionalOfferId,
                    SpecialOfferCode = request.SpecialOfferCode,
                    Metadata = request.Metadata
                });

                // Get payment provider (default to first available)
                var provider = await _providerFactory.GetProviderAsync(request.TenantId, PaymentMethodType.CreditCard);

                var providerResult = await provider.CreatePaymentIntentAsync(new ProviderPaymentIntentRequest
                {
                    Amount = calculation.FinalAmount,
                    Currency = request.Currency,
                    Description = request.Description,
                    CaptureMethod = request.CaptureMethod,
                    Metadata = request.Metadata
                });

                return new PaymentIntentResult
                {
                    Success = providerResult.Success,
                    PaymentIntentId = providerResult.PaymentIntentId,
                    ClientSecret = providerResult.ClientSecret,
                    Amount = calculation.FinalAmount,
                    Currency = request.Currency,
                    Status = providerResult.Status,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = request.ExpiresAt,
                    ErrorCode = providerResult.ErrorCode,
                    ErrorMessage = providerResult.ErrorMessage,
                    Metadata = request.Metadata.ToDictionary(k => k.Key, k => (object)k.Value)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating payment intent for tenant {TenantId}", request.TenantId);
                return new PaymentIntentResult
                {
                    Success = false,
                    ErrorCode = "INTENT_CREATION_ERROR",
                    ErrorMessage = "An error occurred while creating the payment intent"
                };
            }
        }

        public async Task<PaymentResult> ConfirmPaymentIntentAsync(string paymentIntentId, string tenantId)
        {
            try
            {
                var provider = await _providerFactory.GetProviderAsync(tenantId, PaymentMethodType.CreditCard);
                var providerResult = await provider.ConfirmPaymentIntentAsync(paymentIntentId);

                return new PaymentResult
                {
                    Success = providerResult.Success,
                    PaymentId = providerResult.PaymentId,
                    TransactionId = providerResult.TransactionId,
                    Status = providerResult.Status,
                    ProcessedAmount = providerResult.Amount,
                    Currency = providerResult.Currency,
                    ErrorCode = providerResult.ErrorCode,
                    ErrorMessage = providerResult.ErrorMessage,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error confirming payment intent {PaymentIntentId}", paymentIntentId);
                return new PaymentResult
                {
                    Success = false,
                    Status = PaymentStatus.Failed,
                    ErrorCode = "CONFIRMATION_ERROR",
                    ErrorMessage = "An error occurred while confirming the payment"
                };
            }
        }

        public async Task<bool> CancelPaymentIntentAsync(string paymentIntentId, string tenantId)
        {
            try
            {
                var provider = await _providerFactory.GetProviderAsync(tenantId, PaymentMethodType.CreditCard);
                return await provider.CancelPaymentIntentAsync(paymentIntentId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error canceling payment intent {PaymentIntentId}", paymentIntentId);
                return false;
            }
        }

        public async Task<RefundResult> ProcessRefundAsync(RefundRequest request)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(request.TenantId, request.PaymentId);
                if (payment == null)
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorCode = "PAYMENT_NOT_FOUND",
                        ErrorMessage = "Payment not found"
                    };
                }

                var provider = await _providerFactory.GetProviderByNameAsync(payment.ProviderName);
                var providerResult = await provider.ProcessRefundAsync(new ProviderRefundRequest
                {
                    PaymentId = payment.ProviderTransactionId ?? string.Empty,
                    Amount = request.Amount ?? payment.ProcessedAmount,
                    Reason = request.Reason,
                    Metadata = request.Metadata
                });

                return new RefundResult
                {
                    Success = providerResult.Success,
                    RefundId = providerResult.RefundId,
                    RefundedAmount = providerResult.RefundedAmount,
                    Currency = payment.Currency,
                    Status = providerResult.Status,
                    ProcessedAt = DateTime.UtcNow,
                    ErrorCode = providerResult.ErrorCode,
                    ErrorMessage = providerResult.ErrorMessage,
                    Metadata = request.Metadata.ToDictionary(k => k.Key, k => (object)k.Value)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing refund for payment {PaymentId}", request.PaymentId);
                return new RefundResult
                {
                    Success = false,
                    ErrorCode = "REFUND_ERROR",
                    ErrorMessage = "An error occurred while processing the refund"
                };
            }
        }

        public async Task<PaymentCalculationResult> CalculatePaymentAsync(PaymentCalculationRequest request)
        {
            var result = new PaymentCalculationResult
            {
                OriginalAmount = request.OriginalAmount,
                Currency = request.Currency,
                FinalAmount = request.OriginalAmount
            };

            try
            {
                // Apply promotional offer if provided
                if (!string.IsNullOrWhiteSpace(request.PromotionalOfferId))
                {
                    var validation = await _offerService.ValidateOfferAsync(request.TenantId, new ValidateOfferDto
                    {
                        OfferId = request.PromotionalOfferId,
                        UserId = request.UserId,
                        SpecialCode = request.SpecialOfferCode,
                        PurchaseAmount = request.OriginalAmount,
                        Metadata = request.Metadata
                    });

                    if (validation.IsValid && validation.Offer != null)
                    {
                        var discountAmount = Math.Min(validation.Offer.PriceInCents / 100m, request.OriginalAmount);
                        result.DiscountAmount = discountAmount;
                        result.FinalAmount = Math.Max(0, request.OriginalAmount - discountAmount);
                        result.PromotionalOfferId = request.PromotionalOfferId;
                        result.OfferName = validation.Offer.Name;
                        result.IsValidPromotion = true;
                        result.AppliedDiscounts.Add($"{validation.Offer.Name}: -{discountAmount:C}");
                    }
                }

                // Estimate fees (typically 2.9% + $0.30 for credit cards)
                result.EstimatedFees = Math.Round(result.FinalAmount * 0.029m + 0.30m, 2);

                result.Breakdown["originalAmount"] = result.OriginalAmount;
                result.Breakdown["discountAmount"] = result.DiscountAmount;
                result.Breakdown["finalAmount"] = result.FinalAmount;
                result.Breakdown["estimatedFees"] = result.EstimatedFees;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating payment for tenant {TenantId}", request.TenantId);
            }

            return result;
        }

        public async Task<PaymentMethodValidationResult> ValidatePaymentMethodAsync(PaymentMethodInfo paymentMethod)
        {
            var result = new PaymentMethodValidationResult
            {
                DetectedType = paymentMethod.Type
            };

            try
            {
                switch (paymentMethod.Type)
                {
                    case PaymentMethodType.CreditCard:
                    case PaymentMethodType.DebitCard:
                        if (paymentMethod.CreditCard != null)
                        {
                            ValidateCreditCard(paymentMethod.CreditCard, result);
                        }
                        else
                        {
                            result.Errors.Add("Credit card information is required");
                        }
                        break;

                    case PaymentMethodType.BankTransfer:
                    case PaymentMethodType.ACH:
                        if (paymentMethod.BankAccount != null)
                        {
                            ValidateBankAccount(paymentMethod.BankAccount, result);
                        }
                        else
                        {
                            result.Errors.Add("Bank account information is required");
                        }
                        break;

                    default:
                        if (string.IsNullOrWhiteSpace(paymentMethod.Token))
                        {
                            result.Errors.Add("Payment method token is required");
                        }
                        break;
                }

                result.IsValid = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error validating payment method");
                result.Errors.Add("Validation error occurred");
            }

            return result;
        }

        public async Task<List<PaymentMethodType>> GetSupportedPaymentMethodsAsync(string tenantId)
        {
            try
            {
                var providers = await _providerFactory.GetAvailableProvidersAsync(tenantId);
                var supportedMethods = new HashSet<PaymentMethodType>();

                foreach (var provider in providers)
                {
                    var methods = await provider.GetSupportedPaymentMethodsAsync();
                    foreach (var method in methods)
                    {
                        supportedMethods.Add(method);
                    }
                }

                return supportedMethods.ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting supported payment methods for tenant {TenantId}", tenantId);
                return new List<PaymentMethodType> { PaymentMethodType.CreditCard };
            }
        }

        public async Task<WebhookHandleResult> HandleWebhookAsync(string tenantId, string provider, string payload, Dictionary<string, string> headers)
        {
            try
            {
                var paymentProvider = await _providerFactory.GetProviderByNameAsync(provider);
                return await paymentProvider.HandleWebhookAsync(payload, headers);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling webhook from provider {Provider}", provider);
                return new WebhookHandleResult
                {
                    Success = false,
                    ErrorMessage = "Webhook processing failed"
                };
            }
        }

        private static void ValidateCreditCard(CreditCardInfo creditCard, PaymentMethodValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(creditCard.Number))
            {
                result.Errors.Add("Credit card number is required");
            }
            else if (creditCard.Number.Length < 13 || creditCard.Number.Length > 19)
            {
                result.Errors.Add("Invalid credit card number length");
            }

            if (creditCard.ExpiryMonth < 1 || creditCard.ExpiryMonth > 12)
            {
                result.Errors.Add("Invalid expiry month");
            }

            if (creditCard.ExpiryYear < DateTime.UtcNow.Year)
            {
                result.Errors.Add("Credit card has expired");
            }

            if (string.IsNullOrWhiteSpace(creditCard.CVV) || creditCard.CVV.Length < 3 || creditCard.CVV.Length > 4)
            {
                result.Errors.Add("Invalid CVV");
            }
        }

        private static void ValidateBankAccount(BankAccountInfo bankAccount, PaymentMethodValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(bankAccount.AccountNumber))
            {
                result.Errors.Add("Bank account number is required");
            }

            if (string.IsNullOrWhiteSpace(bankAccount.RoutingNumber))
            {
                result.Errors.Add("Routing number is required");
            }
            else if (bankAccount.RoutingNumber.Length != 9)
            {
                result.Errors.Add("Routing number must be 9 digits");
            }

            if (string.IsNullOrWhiteSpace(bankAccount.AccountType))
            {
                result.Errors.Add("Account type is required");
            }
        }
    }
}