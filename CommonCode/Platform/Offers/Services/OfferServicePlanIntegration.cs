using BFormDomain.CommonCode.Platform.Offers.Repository;
using BFormDomain.CommonCode.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Services
{
    /// <summary>
    /// Implementation of service plan integration for promotional offers
    /// </summary>
    public class OfferServicePlanIntegration : IOfferServicePlanIntegration
    {
        private readonly IPromotionalOfferRepository _offerRepository;
        private readonly ICacheService? _cache;
        private readonly ILogger<OfferServicePlanIntegration>? _logger;

        // Default service plan limits - these would typically come from a service plan service
        private static readonly Dictionary<string, OfferServicePlanLimits> DefaultPlanLimits = new()
        {
            ["basic"] = new OfferServicePlanLimits
            {
                MaxPromotionalOffers = 5,
                MaxActiveOffers = 3,
                MaxOffersPerMonth = 5,
                MaxSpecialCodeOffers = 2,
                MaxGiveawayPercent = 10,
                AllowEmailCustomization = false,
                AllowAdminTasks = false,
                AllowAnalytics = false,
                AllowBulkOperations = false,
                AllowExpiringOffers = true,
                MaxEmailTemplateLength = 500,
                AllowedVisibilityTypes = new List<string> { "Public" }
            },
            ["professional"] = new OfferServicePlanLimits
            {
                MaxPromotionalOffers = 25,
                MaxActiveOffers = 15,
                MaxOffersPerMonth = 20,
                MaxSpecialCodeOffers = 10,
                MaxGiveawayPercent = 25,
                AllowEmailCustomization = true,
                AllowAdminTasks = true,
                AllowAnalytics = true,
                AllowBulkOperations = false,
                AllowExpiringOffers = true,
                MaxEmailTemplateLength = 2000,
                AllowedVisibilityTypes = new List<string> { "Public", "MemberOnly" }
            },
            ["enterprise"] = new OfferServicePlanLimits
            {
                MaxPromotionalOffers = null, // Unlimited
                MaxActiveOffers = null,
                MaxOffersPerMonth = null,
                MaxSpecialCodeOffers = null,
                MaxGiveawayPercent = 100,
                AllowEmailCustomization = true,
                AllowAdminTasks = true,
                AllowAnalytics = true,
                AllowBulkOperations = true,
                AllowExpiringOffers = true,
                MaxEmailTemplateLength = null,
                AllowedVisibilityTypes = new List<string> { "Public", "MemberOnly", "SpecialCode" }
            }
        };

        private static readonly Dictionary<string, List<OfferFeature>> PlanFeatures = new()
        {
            ["basic"] = new List<OfferFeature>
            {
                new() { FeatureId = "basic_offers", Name = "Basic Offers", Category = FeatureCategory.Core, IsEnabled = true },
                new() { FeatureId = "public_visibility", Name = "Public Offers", Category = FeatureCategory.Core, IsEnabled = true }
            },
            ["professional"] = new List<OfferFeature>
            {
                new() { FeatureId = "basic_offers", Name = "Basic Offers", Category = FeatureCategory.Core, IsEnabled = true },
                new() { FeatureId = "public_visibility", Name = "Public Offers", Category = FeatureCategory.Core, IsEnabled = true },
                new() { FeatureId = "member_only", Name = "Member-Only Offers", Category = FeatureCategory.Marketing, IsEnabled = true },
                new() { FeatureId = "email_customization", Name = "Email Customization", Category = FeatureCategory.Marketing, IsEnabled = true },
                new() { FeatureId = "admin_tasks", Name = "Admin Tasks", Category = FeatureCategory.Automation, IsEnabled = true },
                new() { FeatureId = "analytics", Name = "Offer Analytics", Category = FeatureCategory.Analytics, IsEnabled = true }
            },
            ["enterprise"] = new List<OfferFeature>
            {
                new() { FeatureId = "basic_offers", Name = "Basic Offers", Category = FeatureCategory.Core, IsEnabled = true },
                new() { FeatureId = "public_visibility", Name = "Public Offers", Category = FeatureCategory.Core, IsEnabled = true },
                new() { FeatureId = "member_only", Name = "Member-Only Offers", Category = FeatureCategory.Marketing, IsEnabled = true },
                new() { FeatureId = "special_codes", Name = "Special Code Offers", Category = FeatureCategory.Advanced, IsEnabled = true },
                new() { FeatureId = "email_customization", Name = "Email Customization", Category = FeatureCategory.Marketing, IsEnabled = true },
                new() { FeatureId = "admin_tasks", Name = "Admin Tasks", Category = FeatureCategory.Automation, IsEnabled = true },
                new() { FeatureId = "analytics", Name = "Advanced Analytics", Category = FeatureCategory.Analytics, IsEnabled = true },
                new() { FeatureId = "bulk_operations", Name = "Bulk Operations", Category = FeatureCategory.Enterprise, IsEnabled = true },
                new() { FeatureId = "unlimited_offers", Name = "Unlimited Offers", Category = FeatureCategory.Enterprise, IsEnabled = true }
            }
        };

        public OfferServicePlanIntegration(
            IPromotionalOfferRepository offerRepository,
            ICacheService? cache = null,
            ILogger<OfferServicePlanIntegration>? logger = null)
        {
            _offerRepository = offerRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ServicePlanCheckResult> CanCreateOfferAsync(string tenantId)
        {
            var usage = await GetOfferUsageAsync(tenantId);
            var limits = await GetOfferLimitsAsync(tenantId);

            // Check total offer limit
            if (limits.MaxPromotionalOffers.HasValue && usage.TotalOffers >= limits.MaxPromotionalOffers.Value)
            {
                return new ServicePlanCheckResult
                {
                    CanProceed = false,
                    ReasonCode = "MAX_OFFERS_EXCEEDED",
                    Message = $"Maximum number of promotional offers ({limits.MaxPromotionalOffers}) has been reached",
                    LimitInfo = new ServicePlanLimitInfo
                    {
                        CurrentUsage = usage.TotalOffers,
                        MaxAllowed = limits.MaxPromotionalOffers
                    }
                };
            }

            // Check monthly limit
            if (limits.MaxOffersPerMonth.HasValue && usage.OffersCreatedThisMonth >= limits.MaxOffersPerMonth.Value)
            {
                return new ServicePlanCheckResult
                {
                    CanProceed = false,
                    ReasonCode = "MONTHLY_LIMIT_EXCEEDED",
                    Message = $"Monthly offer creation limit ({limits.MaxOffersPerMonth}) has been reached",
                    LimitInfo = new ServicePlanLimitInfo
                    {
                        CurrentUsage = usage.OffersCreatedThisMonth,
                        MaxAllowed = limits.MaxOffersPerMonth,
                        ResetsAt = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1)
                    }
                };
            }

            return new ServicePlanCheckResult
            {
                CanProceed = true,
                LimitInfo = new ServicePlanLimitInfo
                {
                    CurrentUsage = usage.TotalOffers,
                    MaxAllowed = limits.MaxPromotionalOffers
                }
            };
        }

        public async Task<bool> CanUseFeatureAsync(string tenantId, string featureName)
        {
            var servicePlanId = await GetTenantServicePlanIdAsync(tenantId);
            var features = await GetAvailableFeaturesAsync(servicePlanId);

            return features.Any(f => f.FeatureId == featureName && f.IsEnabled);
        }

        public async Task<OfferServicePlanLimits> GetOfferLimitsAsync(string tenantId)
        {
            var cacheKey = $"offer_limits:{tenantId}";

            if (_cache != null)
            {
                var cached = await _cache.GetAsync<OfferServicePlanLimits>(cacheKey);
                if (cached != null) return cached;
            }

            var servicePlanId = await GetTenantServicePlanIdAsync(tenantId);
            var limits = GetPlanLimits(servicePlanId);

            if (_cache != null)
            {
                await _cache.SetAsync(cacheKey, limits, TimeSpan.FromMinutes(30));
            }

            return limits;
        }

        public async Task TrackOfferUsageAsync(string tenantId, string offerId, OfferUsageType usageType)
        {
            var cacheKey = $"offer_usage:{tenantId}";

            // For now, just invalidate the cache when usage changes
            // In a real implementation, this would update usage tracking in a database
            if (_cache != null)
            {
                await _cache.RemoveAsync(cacheKey);
            }

            _logger?.LogDebug("Tracked offer usage for tenant {TenantId}, offer {OfferId}, type {UsageType}", 
                tenantId, offerId, usageType);
        }

        public async Task<OfferUsageStatistics> GetOfferUsageAsync(string tenantId)
        {
            var cacheKey = $"offer_usage:{tenantId}";

            if (_cache != null)
            {
                var cached = await _cache.GetAsync<OfferUsageStatistics>(cacheKey);
                if (cached != null) return cached;
            }

            // Get current usage from repository
            var totalOffers = await _offerRepository.CountOffersAsync(tenantId, false);
            var activeOffers = await _offerRepository.CountOffersAsync(tenantId, true);

            // Get offers created this month
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var monthlyOffers = await _offerRepository.GetOffersByDateRangeAsync(tenantId, monthStart, monthEnd);

            var usage = new OfferUsageStatistics
            {
                TenantId = tenantId,
                TotalOffers = (int)totalOffers,
                ActiveOffers = (int)activeOffers,
                OffersCreatedThisMonth = monthlyOffers.Count,
                PeriodStart = monthStart,
                PeriodEnd = monthEnd,
                LastOfferCreated = monthlyOffers.Any() ? monthlyOffers.Max(o => o.CreatedDate) : DateTime.MinValue
            };

            if (_cache != null)
            {
                await _cache.SetAsync(cacheKey, usage, TimeSpan.FromMinutes(15));
            }

            return usage;
        }

        public async Task<OfferPlanCompatibilityResult> ValidateOfferCompatibilityAsync(string tenantId, string servicePlanId)
        {
            var limits = GetPlanLimits(servicePlanId);
            var features = GetPlanFeatures(servicePlanId);
            var usage = await GetOfferUsageAsync(tenantId);

            var result = new OfferPlanCompatibilityResult
            {
                IsCompatible = true
            };

            // Check if current usage exceeds new plan limits
            if (limits.MaxPromotionalOffers.HasValue && usage.TotalOffers > limits.MaxPromotionalOffers.Value)
            {
                result.IsCompatible = false;
                result.IncompatibleFeatures.Add($"Total offers ({usage.TotalOffers}) exceeds plan limit ({limits.MaxPromotionalOffers})");
            }

            if (limits.MaxActiveOffers.HasValue && usage.ActiveOffers > limits.MaxActiveOffers.Value)
            {
                result.Warnings.Add($"Active offers ({usage.ActiveOffers}) exceeds recommended limit ({limits.MaxActiveOffers})");
            }

            // Check feature compatibility
            if (!limits.AllowAnalytics)
            {
                result.Warnings.Add("Analytics features will be disabled with this plan");
            }

            if (!limits.AllowBulkOperations)
            {
                result.Warnings.Add("Bulk operations will be disabled with this plan");
            }

            return result;
        }

        public async Task<List<OfferFeature>> GetAvailableFeaturesAsync(string servicePlanId)
        {
            var cacheKey = $"plan_features:{servicePlanId}";

            if (_cache != null)
            {
                var cached = await _cache.GetAsync<List<OfferFeature>>(cacheKey);
                if (cached != null) return cached;
            }

            var features = GetPlanFeatures(servicePlanId);

            if (_cache != null)
            {
                await _cache.SetAsync(cacheKey, features, TimeSpan.FromHours(1));
            }

            return features;
        }

        public async Task<bool> HasReachedMonthlyLimitAsync(string tenantId)
        {
            var usage = await GetOfferUsageAsync(tenantId);
            var limits = await GetOfferLimitsAsync(tenantId);

            return limits.MaxOffersPerMonth.HasValue && 
                   usage.OffersCreatedThisMonth >= limits.MaxOffersPerMonth.Value;
        }

        public async Task ResetMonthlyUsageAsync(string tenantId)
        {
            // In a real implementation, this would reset usage counters in the database
            var cacheKey = $"offer_usage:{tenantId}";
            if (_cache != null)
            {
                await _cache.RemoveAsync(cacheKey);
            }

            _logger?.LogInformation("Reset monthly usage for tenant {TenantId}", tenantId);
        }

        private async Task<string> GetTenantServicePlanIdAsync(string tenantId)
        {
            // In a real implementation, this would fetch from a tenant service or database
            // For now, return a default plan based on tenant ID pattern
            return tenantId.StartsWith("ent_") ? "enterprise" : 
                   tenantId.StartsWith("pro_") ? "professional" : "basic";
        }

        private static OfferServicePlanLimits GetPlanLimits(string servicePlanId)
        {
            return DefaultPlanLimits.TryGetValue(servicePlanId.ToLowerInvariant(), out var limits) 
                ? limits 
                : DefaultPlanLimits["basic"];
        }

        private static List<OfferFeature> GetPlanFeatures(string servicePlanId)
        {
            return PlanFeatures.TryGetValue(servicePlanId.ToLowerInvariant(), out var features) 
                ? features 
                : PlanFeatures["basic"];
        }
    }
}