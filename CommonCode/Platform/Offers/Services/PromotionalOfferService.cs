using BFormDomain.CommonCode.Platform.Offers.Domain;
using BFormDomain.CommonCode.Platform.Offers.DTOs;
using BFormDomain.CommonCode.Platform.Offers.Repository;
using BFormDomain.CommonCode.Platform.Offers.Analytics;
using BFormDomain.CommonCode.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Services
{
    /// <summary>
    /// Core implementation of promotional offer business logic
    /// </summary>
    public class PromotionalOfferService : IPromotionalOfferService
    {
        private readonly IPromotionalOfferRepository _offerRepository;
        private readonly IOfferAnalyticsRepository _analyticsRepository;
        private readonly IOfferServicePlanIntegration _servicePlanIntegration;
        private readonly ICacheService? _cache;
        private readonly ILogger<PromotionalOfferService>? _logger;
        private readonly Random _random = new();

        public PromotionalOfferService(
            IPromotionalOfferRepository offerRepository,
            IOfferAnalyticsRepository analyticsRepository,
            IOfferServicePlanIntegration servicePlanIntegration,
            ICacheService? cache = null,
            ILogger<PromotionalOfferService>? logger = null)
        {
            _offerRepository = offerRepository;
            _analyticsRepository = analyticsRepository;
            _servicePlanIntegration = servicePlanIntegration;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PromotionalOffer> CreateOfferAsync(string tenantId, CreateOfferDto dto)
        {
            // Check service plan limits
            var planCheck = await _servicePlanIntegration.CanCreateOfferAsync(tenantId);
            if (!planCheck.CanProceed)
            {
                throw new InvalidOperationException($"Cannot create offer: {planCheck.Message}");
            }

            // Validate special code uniqueness if provided
            if (!string.IsNullOrWhiteSpace(dto.SpecialOfferCode))
            {
                var isUnique = await _offerRepository.IsSpecialCodeUniqueAsync(tenantId, dto.SpecialOfferCode);
                if (!isUnique)
                {
                    throw new ArgumentException("Special offer code is already in use");
                }
            }

            // Create the offer entity
            var offer = new PromotionalOffer
            {
                TenantId = tenantId,
                ServicePlanId = dto.ServicePlanId ?? string.Empty,
                Name = dto.Name,
                Description = dto.Description,
                HookText = dto.HookText,
                BackgroundImageUrl = dto.BackgroundImageUrl,
                PriceInCents = (long)(dto.Price * 100),
                Currency = dto.Currency,
                ServiceUnitCount = dto.ServiceUnitCount,
                IsActive = dto.IsActive,
                Priority = dto.Priority,
                ExpiresAt = dto.ExpiresAt,
                MaxQuantity = dto.MaxQuantity,
                Visibility = dto.Visibility,
                SpecialOfferCode = dto.SpecialOfferCode?.Trim().ToUpperInvariant(),
                IsSpecialOffer = dto.IsSpecialOffer,
                TargetAudience = dto.TargetAudience,
                Features = dto.Features,
                GiveawayChancePercent = dto.GiveawayChancePercent,
                HighlightBadge = dto.HighlightBadge,
                Tags = dto.Tags,
                ExternalIds = dto.ExternalIds?.ToDictionary(k => k.Key, k => (object)k.Value) ?? new Dictionary<string, object>(),
                CreatedByUserId = dto.CreatedByUserId,
                EmailTemplate = new EmailTemplate
                {
                    Subject = dto.EmailTemplate.Subject,
                    HtmlContent = dto.EmailTemplate.HtmlContent,
                    Variables = dto.EmailTemplate.Variables
                },
                AdminTasks = dto.AdminTasks.Select(at => new AdminTask
                {
                    Description = at.Description,
                    NotifyEmails = at.NotifyEmails,
                    IncludeUserDetails = at.IncludeUserDetails,
                    Priority = at.Priority,
                    Category = at.Category,
                    CompletionDeadline = at.CompletionDeadlineMinutes > 0 ? TimeSpan.FromMinutes(at.CompletionDeadlineMinutes.Value) : null,
                    Metadata = at.Metadata
                }).ToList()
            };

            // Save the offer
            _offerRepository.Create(offer);

            // Track usage
            await _servicePlanIntegration.TrackOfferUsageAsync(tenantId, offer.Id.ToString(), OfferUsageType.Created);

            // Invalidate cache
            if (_cache != null)
            {
                await _cache.RemoveAsync($"offers:active:{tenantId}:*");
            }

            _logger?.LogInformation("Created promotional offer {OfferId} for tenant {TenantId}", offer.Id, tenantId);

            return offer;
        }

        public async Task<PromotionalOffer> UpdateOfferAsync(string tenantId, UpdateOfferDto dto)
        {
            var (offer, _) = await _offerRepository.LoadAsync(Guid.Parse(dto.Id));
            if (offer?.TenantId != tenantId)
            {
                throw new ArgumentException("Offer not found");
            }

            // Check special code uniqueness if changed
            if (!string.IsNullOrWhiteSpace(dto.SpecialOfferCode) && 
                dto.SpecialOfferCode.Trim().ToUpperInvariant() != offer.SpecialOfferCode)
            {
                var isUnique = await _offerRepository.IsSpecialCodeUniqueAsync(tenantId, dto.SpecialOfferCode, dto.Id);
                if (!isUnique)
                {
                    throw new ArgumentException("Special offer code is already in use");
                }
            }

            // Update fields that are provided
            if (dto.Name != null) offer.Name = dto.Name;
            if (dto.Description != null) offer.Description = dto.Description;
            if (dto.HookText != null) offer.HookText = dto.HookText;
            if (dto.BackgroundImageUrl != null) offer.BackgroundImageUrl = dto.BackgroundImageUrl;
            if (dto.Price.HasValue) offer.PriceInCents = (long)(dto.Price.Value * 100);
            if (dto.Currency != null) offer.Currency = dto.Currency;
            if (dto.ServiceUnitCount.HasValue) offer.ServiceUnitCount = dto.ServiceUnitCount.Value;
            if (dto.IsActive.HasValue) offer.IsActive = dto.IsActive.Value;
            if (dto.Priority.HasValue) offer.Priority = dto.Priority.Value;
            if (dto.ExpiresAt.HasValue) offer.ExpiresAt = dto.ExpiresAt;
            if (dto.MaxQuantity.HasValue) offer.MaxQuantity = dto.MaxQuantity;
            if (dto.Visibility.HasValue) offer.Visibility = dto.Visibility.Value;
            if (dto.SpecialOfferCode != null) offer.SpecialOfferCode = dto.SpecialOfferCode.Trim().ToUpperInvariant();
            if (dto.IsSpecialOffer.HasValue) offer.IsSpecialOffer = dto.IsSpecialOffer.Value;
            if (dto.TargetAudience.HasValue) offer.TargetAudience = dto.TargetAudience.Value;
            if (dto.Features != null) offer.Features = dto.Features;
            if (dto.GiveawayChancePercent.HasValue) offer.GiveawayChancePercent = dto.GiveawayChancePercent.Value;
            if (dto.HighlightBadge != null) offer.HighlightBadge = dto.HighlightBadge;
            if (dto.Tags != null) offer.Tags = dto.Tags;
            if (dto.ExternalIds != null) offer.ExternalIds = dto.ExternalIds.ToDictionary(k => k.Key, k => (object)k.Value);

            if (dto.EmailTemplate != null)
            {
                offer.EmailTemplate = new EmailTemplate
                {
                    Subject = dto.EmailTemplate.Subject,
                    HtmlContent = dto.EmailTemplate.HtmlContent,
                    Variables = dto.EmailTemplate.Variables
                };
            }

            if (dto.AdminTasks != null)
            {
                offer.AdminTasks = dto.AdminTasks.Select(at => new AdminTask
                {
                    Description = at.Description,
                    NotifyEmails = at.NotifyEmails,
                    IncludeUserDetails = at.IncludeUserDetails,
                    Priority = at.Priority,
                    Category = at.Category,
                    CompletionDeadline = at.CompletionDeadlineMinutes > 0 ? TimeSpan.FromMinutes(at.CompletionDeadlineMinutes.Value) : null,
                    Metadata = at.Metadata
                }).ToList();
            }

            offer.UpdatedByUserId = dto.UpdatedByUserId;
            offer.UpdatedDate = DateTime.UtcNow;

            // Save the updated offer
            await _offerRepository.UpdateAsync(offer);

            // Track usage
            await _servicePlanIntegration.TrackOfferUsageAsync(tenantId, offer.Id.ToString(), OfferUsageType.Updated);

            // Invalidate cache
            if (_cache != null)
            {
                await _cache.RemoveAsync($"offers:active:{tenantId}:*");
            }

            _logger?.LogInformation("Updated promotional offer {OfferId} for tenant {TenantId}", offer.Id, tenantId);

            return offer;
        }

        public async Task<bool> DeleteOfferAsync(string tenantId, string offerId, string deletedBy)
        {
            var offer = await _offerRepository.GetByIdAsync(tenantId, offerId);
            if (offer == null)
            {
                return false;
            }

            // Soft delete - mark as inactive and add deleted tag
            offer.IsActive = false;
            offer.UpdatedByUserId = deletedBy;
            offer.UpdatedDate = DateTime.UtcNow;
            
            if (!offer.Tags.Contains("deleted"))
            {
                offer.Tags.Add("deleted");
            }

            await _offerRepository.UpdateAsync(offer);

            // Track usage
            await _servicePlanIntegration.TrackOfferUsageAsync(tenantId, offerId, OfferUsageType.Deleted);

            // Invalidate cache
            if (_cache != null)
            {
                await _cache.RemoveAsync($"offers:active:{tenantId}:*");
            }

            _logger?.LogInformation("Deleted promotional offer {OfferId} for tenant {TenantId}", offerId, tenantId);

            return true;
        }

        public async Task<PromotionalOffer?> GetOfferAsync(string tenantId, string offerId)
        {
            return await _offerRepository.GetByIdAsync(tenantId, offerId);
        }

        public async Task<List<OfferDisplayDto>> GetActiveOffersAsync(string tenantId, string? userId = null, int? limit = null)
        {
            var offers = await _offerRepository.GetVisibleOffersAsync(tenantId, userId, limit);
            return offers.Select(MapToDisplayDto).ToList();
        }

        public async Task<OfferValidationResult> ValidateOfferAsync(string tenantId, ValidateOfferDto dto)
        {
            return await _offerRepository.ValidateOfferAsync(tenantId, dto.OfferId, dto.UserId);
        }

        public async Task<RedeemOfferResultDto> RedeemOfferAsync(string tenantId, RedeemOfferDto dto)
        {
            // Validate the offer first
            var validation = await _offerRepository.ValidateOfferAsync(tenantId, dto.OfferId, dto.UserId);
            if (!validation.IsValid)
            {
                return new RedeemOfferResultDto
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", validation.Errors)
                };
            }

            var offer = validation.Offer!;

            // Calculate discount and final amount
            var discountAmount = offer.PriceInCents / 100m;
            var finalAmount = Math.Max(0, dto.PurchaseAmount - discountAmount);

            // Increment sold count
            var incrementSuccess = await _offerRepository.IncrementSoldCountAsync(tenantId, dto.OfferId);
            if (!incrementSuccess)
            {
                return new RedeemOfferResultDto
                {
                    Success = false,
                    ErrorMessage = "Failed to process offer redemption"
                };
            }

            // Record analytics event
            var analyticsEvent = new AnalyticsEvent
            {
                TenantId = tenantId,
                OfferId = dto.OfferId,
                UserId = dto.UserId,
                Type = AnalyticsEventType.Purchase,
                Timestamp = DateTime.UtcNow,
                ValueInCents = (long)(discountAmount * 100),
                Source = "redemption",
                UserAgent = "",
                IpAddress = "",
                SessionId = Guid.NewGuid().ToString(),
                Metadata = dto.Metadata?.ToDictionary(k => k.Key, k => (object)k.Value) ?? new Dictionary<string, object>()
            };

            await _analyticsRepository.RecordEventAsync(analyticsEvent);

            // Track usage
            await _servicePlanIntegration.TrackOfferUsageAsync(tenantId, dto.OfferId, OfferUsageType.Redeemed);

            var redemptionId = Guid.NewGuid().ToString();

            _logger?.LogInformation("Redeemed offer {OfferId} for user {UserId} in tenant {TenantId}, redemption {RedemptionId}", 
                dto.OfferId, dto.UserId, tenantId, redemptionId);

            return new RedeemOfferResultDto
            {
                Success = true,
                RedemptionId = redemptionId,
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                Metadata = new Dictionary<string, object>
                {
                    ["offerName"] = offer.Name,
                    ["originalAmount"] = dto.PurchaseAmount,
                    ["redemptionDate"] = DateTime.UtcNow
                }
            };
        }

        public async Task<OfferSearchResult> SearchOffersAsync(string tenantId, OfferSearchCriteria criteria)
        {
            return await _offerRepository.SearchOffersAsync(tenantId, criteria);
        }

        public async Task<OfferDisplayDto?> GetOfferByCodeAsync(string tenantId, string code, string? userId = null)
        {
            var offer = await _offerRepository.GetBySpecialCodeAsync(tenantId, code);
            if (offer == null)
            {
                return null;
            }

            // Validate visibility
            if (offer.Visibility == OfferVisibility.MemberOnly && string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            // Validate offer is still active and not expired
            var validation = await _offerRepository.ValidateOfferAsync(tenantId, offer.Id.ToString(), userId);
            if (!validation.IsValid)
            {
                return null;
            }

            return MapToDisplayDto(offer);
        }

        public async Task<BulkOperationResult> PerformBulkOperationAsync(string tenantId, BulkOfferOperationDto dto)
        {
            var result = new BulkOperationResult
            {
                TotalItems = dto.OfferIds.Count
            };

            foreach (var offerId in dto.OfferIds)
            {
                try
                {
                    switch (dto.Operation)
                    {
                        case BulkOperationType.Activate:
                            await UpdateOfferStatusAsync(tenantId, offerId, true, dto.PerformedBy);
                            break;
                        case BulkOperationType.Deactivate:
                            await UpdateOfferStatusAsync(tenantId, offerId, false, dto.PerformedBy);
                            break;
                        case BulkOperationType.Archive:
                            await _offerRepository.ArchiveOfferAsync(tenantId, offerId);
                            break;
                        case BulkOperationType.UpdateVisibility:
                            if (dto.Parameters.TryGetValue("visibility", out var visibilityObj) && 
                                Enum.TryParse<OfferVisibility>(visibilityObj.ToString(), out var visibility))
                            {
                                await _offerRepository.BulkUpdateVisibilityAsync(tenantId, new List<string> { offerId }, visibility);
                            }
                            break;
                        case BulkOperationType.Delete:
                            await DeleteOfferAsync(tenantId, offerId, dto.PerformedBy);
                            break;
                        case BulkOperationType.ExtendExpiration:
                            if (dto.Parameters.TryGetValue("newExpiration", out var expirationObj) && 
                                DateTime.TryParse(expirationObj.ToString(), out var newExpiration))
                            {
                                await ExtendOfferExpirationAsync(tenantId, offerId, newExpiration, dto.PerformedBy);
                            }
                            break;
                    }
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        ItemId = offerId,
                        ErrorCode = "OPERATION_FAILED",
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Track usage
            await _servicePlanIntegration.TrackOfferUsageAsync(tenantId, string.Empty, OfferUsageType.BulkOperation);

            return result;
        }

        public async Task<int> ArchiveExpiredOffersAsync(string tenantId)
        {
            var expiredOffers = await _offerRepository.GetExpiringOffersAsync(tenantId, DateTime.UtcNow);
            var count = 0;

            foreach (var offer in expiredOffers)
            {
                await _offerRepository.ArchiveOfferAsync(tenantId, offer.Id.ToString());
                count++;
            }

            _logger?.LogInformation("Archived {Count} expired offers for tenant {TenantId}", count, tenantId);

            return count;
        }

        public async Task UpdateOfferPrioritiesAsync(string tenantId, List<OfferPriorityUpdate> updates)
        {
            await _offerRepository.UpdateOfferPrioritiesAsync(tenantId, updates);

            // Invalidate cache
            if (_cache != null)
            {
                await _cache.RemoveAsync($"offers:active:{tenantId}:*");
            }
        }

        public async Task<List<OfferDisplayDto>> GetLowStockOffersAsync(string tenantId, int thresholdPercentage = 10)
        {
            var offers = await _offerRepository.GetLowStockOffersAsync(tenantId, thresholdPercentage);
            return offers.Select(MapToDisplayDto).ToList();
        }

        public async Task<string> GenerateUniqueCodeAsync(string tenantId, int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var maxAttempts = 100;
            
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var code = new StringBuilder();
                for (var i = 0; i < length; i++)
                {
                    code.Append(chars[_random.Next(chars.Length)]);
                }

                var codeString = code.ToString();
                var isUnique = await _offerRepository.IsSpecialCodeUniqueAsync(tenantId, codeString);
                if (isUnique)
                {
                    return codeString;
                }
            }

            throw new InvalidOperationException("Unable to generate unique offer code after maximum attempts");
        }

        public async Task<bool> IsCodeAvailableAsync(string tenantId, string code, string? excludeOfferId = null)
        {
            return await _offerRepository.IsSpecialCodeUniqueAsync(tenantId, code, excludeOfferId);
        }

        public async Task<OfferStatistics> GetOfferStatisticsAsync(string tenantId, string offerId)
        {
            var offer = await _offerRepository.GetByIdAsync(tenantId, offerId);
            if (offer == null)
            {
                throw new ArgumentException("Offer not found");
            }

            // Get analytics data for the last 30 days
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);
            var analytics = await _analyticsRepository.GetAnalyticsRangeAsync(tenantId, offerId, startDate, endDate);

            var totalRevenue = analytics.Sum(a => a.Metrics.RevenueInCents);
            var totalConversions = analytics.Sum(a => a.Metrics.Conversions);
            var totalImpressions = analytics.Sum(a => a.Metrics.Impressions);

            return new OfferStatistics
            {
                OfferId = offerId,
                OfferName = offer.Name,
                TotalSold = offer.SoldCount,
                TotalRevenue = totalRevenue,
                RemainingQuantity = offer.MaxQuantity.HasValue ? offer.MaxQuantity.Value - offer.SoldCount : -1,
                ConversionRate = totalImpressions > 0 ? (decimal)totalConversions / totalImpressions * 100 : 0,
                LastSoldDate = offer.UpdatedDate, // Approximation
                SalesByDay = analytics.GroupBy(a => a.PeriodStart.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => (int)g.Sum(a => a.Metrics.Conversions))
            };
        }

        public async Task<PromotionalOffer> CloneOfferAsync(string tenantId, string offerId, string clonedBy)
        {
            var originalOffer = await _offerRepository.GetByIdAsync(tenantId, offerId);
            if (originalOffer == null)
            {
                throw new ArgumentException("Original offer not found");
            }

            var clonedOffer = new PromotionalOffer
            {
                TenantId = tenantId,
                ServicePlanId = originalOffer.ServicePlanId,
                Name = $"{originalOffer.Name} (Copy)",
                Description = originalOffer.Description,
                HookText = originalOffer.HookText,
                BackgroundImageUrl = originalOffer.BackgroundImageUrl,
                PriceInCents = originalOffer.PriceInCents,
                Currency = originalOffer.Currency,
                ServiceUnitCount = originalOffer.ServiceUnitCount,
                IsActive = false, // Start cloned offers as inactive
                Priority = originalOffer.Priority,
                ExpiresAt = originalOffer.ExpiresAt,
                MaxQuantity = originalOffer.MaxQuantity,
                Visibility = originalOffer.Visibility,
                SpecialOfferCode = null, // Don't copy special codes
                IsSpecialOffer = originalOffer.IsSpecialOffer,
                TargetAudience = originalOffer.TargetAudience,
                Features = new List<string>(originalOffer.Features),
                GiveawayChancePercent = originalOffer.GiveawayChancePercent,
                HighlightBadge = originalOffer.HighlightBadge,
                Tags = new List<string>(originalOffer.Tags) { "cloned" },
                ExternalIds = new Dictionary<string, object>(originalOffer.ExternalIds),
                CreatedByUserId = clonedBy,
                EmailTemplate = new EmailTemplate
                {
                    Subject = originalOffer.EmailTemplate.Subject,
                    HtmlContent = originalOffer.EmailTemplate.HtmlContent,
                    Variables = new List<string>(originalOffer.EmailTemplate.Variables)
                },
                AdminTasks = originalOffer.AdminTasks.Select(at => new AdminTask
                {
                    Description = at.Description,
                    NotifyEmails = new List<string>(at.NotifyEmails),
                    IncludeUserDetails = at.IncludeUserDetails,
                    Priority = at.Priority,
                    Category = at.Category,
                    CompletionDeadline = at.CompletionDeadline,
                    Metadata = new Dictionary<string, string>(at.Metadata)
                }).ToList()
            };

            _offerRepository.Create(clonedOffer);

            _logger?.LogInformation("Cloned offer {OriginalOfferId} to {ClonedOfferId} for tenant {TenantId}", 
                offerId, clonedOffer.Id, tenantId);

            return clonedOffer;
        }

        public async Task<bool> SendTestEmailAsync(string tenantId, string offerId, string testEmail)
        {
            // Implementation would integrate with email service
            // For now, just log the test email request
            _logger?.LogInformation("Test email requested for offer {OfferId} to {TestEmail} in tenant {TenantId}", 
                offerId, testEmail, tenantId);

            return true;
        }

        public async Task<List<OfferDisplayDto>> GetExpiringOffersAsync(string tenantId, int daysAhead = 7)
        {
            var beforeDate = DateTime.UtcNow.AddDays(daysAhead);
            var offers = await _offerRepository.GetExpiringOffersAsync(tenantId, beforeDate);
            return offers.Select(MapToDisplayDto).ToList();
        }

        public async Task<bool> ExtendOfferExpirationAsync(string tenantId, string offerId, DateTime newExpiration, string extendedBy)
        {
            var offer = await _offerRepository.GetByIdAsync(tenantId, offerId);
            if (offer == null)
            {
                return false;
            }

            offer.ExpiresAt = newExpiration;
            offer.UpdatedByUserId = extendedBy;
            offer.UpdatedDate = DateTime.UtcNow;

            await _offerRepository.UpdateAsync(offer);

            _logger?.LogInformation("Extended offer {OfferId} expiration to {NewExpiration} for tenant {TenantId}", 
                offerId, newExpiration, tenantId);

            return true;
        }

        public async Task<List<OfferDisplayDto>> GetRecommendedOffersAsync(string tenantId, string userId, int limit = 5)
        {
            // Simple recommendation: get active offers sorted by priority
            // In a real implementation, this would use user preferences, purchase history, etc.
            var offers = await _offerRepository.GetVisibleOffersAsync(tenantId, userId, limit);
            return offers.Select(MapToDisplayDto).ToList();
        }

        private async Task UpdateOfferStatusAsync(string tenantId, string offerId, bool isActive, string updatedBy)
        {
            var offer = await _offerRepository.GetByIdAsync(tenantId, offerId);
            if (offer != null)
            {
                offer.IsActive = isActive;
                offer.UpdatedByUserId = updatedBy;
                offer.UpdatedDate = DateTime.UtcNow;
                await _offerRepository.UpdateAsync(offer);
            }
        }

        private static OfferDisplayDto MapToDisplayDto(PromotionalOffer offer)
        {
            return new OfferDisplayDto
            {
                Id = offer.Id.ToString(),
                Name = offer.Name,
                Description = offer.Description,
                HookText = offer.HookText,
                BackgroundImageUrl = offer.BackgroundImageUrl,
                DisplayPrice = $"{offer.PriceInCents / 100m:C}",
                ServiceUnitCount = offer.ServiceUnitCount,
                Features = offer.Features,
                HighlightBadge = offer.HighlightBadge,
                RemainingQuantity = offer.MaxQuantity.HasValue ? offer.MaxQuantity.Value - offer.SoldCount : null,
                ExpiresAt = offer.ExpiresAt,
                RequiresCode = offer.Visibility == OfferVisibility.SpecialCode,
                GiveawayChancePercent = offer.GiveawayChancePercent,
                Visibility = offer.Visibility
            };
        }
    }
}