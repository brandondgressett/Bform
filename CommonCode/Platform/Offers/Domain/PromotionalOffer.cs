using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Offers.Repository;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BFormDomain.CommonCode.Platform.Offers.Domain
{
    /// <summary>
    /// Represents a promotional offer that can be applied to services or products.
    /// Supports various visibility levels, time limits, and quantity restrictions.
    /// </summary>
    public class PromotionalOffer : AppEntityBase
    {
        public PromotionalOffer()
        {
            EntityType = "PromotionalOffer";
        }

        // Service Plan Association
        public string ServicePlanId { get; set; } = string.Empty;

        // Basic Information
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HookText { get; set; } = string.Empty;

        // Visual
        public string? BackgroundImageUrl { get; set; }

        // Pricing
        public long PriceInCents { get; set; }
        public string Currency { get; set; } = "USD";
        public int ServiceUnitCount { get; set; }

        // Display Control
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 100;
        public DateTime? ExpiresAt { get; set; }
        public int? MaxQuantity { get; set; }
        public int SoldCount { get; set; } = 0;

        // Visibility
        public OfferVisibility Visibility { get; set; } = OfferVisibility.Public;
        public string? SpecialOfferCode { get; set; }
        public bool IsSpecialOffer { get; set; }
        public TargetAudience TargetAudience { get; set; } = TargetAudience.All;

        // Features
        public List<string> Features { get; set; } = new();
        public int GiveawayChancePercent { get; set; } = 0;
        public string? HighlightBadge { get; set; }

        // Email Template
        public EmailTemplate EmailTemplate { get; set; } = new();

        // Admin Tasks
        public List<AdminTask> AdminTasks { get; set; } = new();

        // External Integration
        [BsonExtraElements]
        public Dictionary<string, object> ExternalIds { get; set; } = new();

        // Additional tracking
        public string CreatedByUserId { get; set; } = string.Empty;
        public string? UpdatedByUserId { get; set; }

        /// <summary>
        /// Validates the promotional offer
        /// </summary>
        public OfferValidationResult Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name is required");

            if (PriceInCents < 0)
                errors.Add("Price cannot be negative");

            if (ServiceUnitCount <= 0)
                errors.Add("Service unit count must be positive");

            if (GiveawayChancePercent < 0 || GiveawayChancePercent > 100)
                errors.Add("Giveaway chance must be between 0 and 100");

            if (Visibility == OfferVisibility.SpecialCode && string.IsNullOrWhiteSpace(SpecialOfferCode))
                errors.Add("Special offer code is required for special code visibility");

            if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow)
                errors.Add("Expiration date must be in the future");

            if (MaxQuantity.HasValue && MaxQuantity.Value <= 0)
                errors.Add("Max quantity must be positive");

            if (!EmailTemplate.Validate(out var emailErrors))
                errors.AddRange(emailErrors);

            foreach (var task in AdminTasks)
            {
                if (!task.Validate(out var taskErrors))
                    errors.AddRange(taskErrors);
            }

            return new OfferValidationResult 
            { 
                IsValid = errors.Count == 0, 
                Errors = errors,
                Offer = this
            };
        }

        /// <summary>
        /// Checks if the offer is currently available for purchase
        /// </summary>
        public bool IsAvailable()
        {
            if (!IsActive)
                return false;

            if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
                return false;

            if (MaxQuantity.HasValue && SoldCount >= MaxQuantity.Value)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the display price as a formatted string
        /// </summary>
        public string GetDisplayPrice()
        {
            var amount = PriceInCents / 100m;
            return Currency switch
            {
                "USD" => $"${amount:F2}",
                "EUR" => $"€{amount:F2}",
                "GBP" => $"£{amount:F2}",
                _ => $"{amount:F2} {Currency}"
            };
        }

        public override Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
        {
            var baseUri = $"/offers/{(template ? "template" : "instance")}/{Id}";
            if (vm)
                baseUri += "/vm";
            if (!string.IsNullOrWhiteSpace(queryParameters))
                baseUri += $"?{queryParameters}";
            return new Uri(baseUri, UriKind.Relative);
        }
    }

    public enum OfferVisibility
    {
        Public,
        MemberOnly,
        SpecialCode
    }

    public enum TargetAudience
    {
        All,
        MembersOnly,
        NewUsersOnly,
        ReturningUsers
    }
}