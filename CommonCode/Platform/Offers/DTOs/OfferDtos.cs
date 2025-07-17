using BFormDomain.CommonCode.Platform.Offers.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BFormDomain.CommonCode.Platform.Offers.DTOs
{
    /// <summary>
    /// DTO for creating a new promotional offer
    /// </summary>
    public class CreateOfferDto
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        public string HookText { get; set; } = string.Empty;

        [Url]
        public string? BackgroundImageUrl { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [RegularExpression("^[A-Z]{3}$")]
        public string Currency { get; set; } = "USD";

        [Required]
        [Range(1, int.MaxValue)]
        public int ServiceUnitCount { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(0, 10000)]
        public int Priority { get; set; } = 100;

        public DateTime? ExpiresAt { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxQuantity { get; set; }

        [Required]
        public OfferVisibility Visibility { get; set; } = OfferVisibility.Public;

        public string? SpecialOfferCode { get; set; }

        public bool IsSpecialOffer { get; set; }

        public TargetAudience TargetAudience { get; set; } = TargetAudience.All;

        public List<string> Features { get; set; } = new();

        [Range(0, 100)]
        public int GiveawayChancePercent { get; set; } = 0;

        [StringLength(50)]
        public string? HighlightBadge { get; set; }

        [Required]
        public EmailTemplateDto EmailTemplate { get; set; } = new();

        public List<AdminTaskDto> AdminTasks { get; set; } = new();

        public string? ServicePlanId { get; set; }

        public List<string> Tags { get; set; } = new();

        public Dictionary<string, string> ExternalIds { get; set; } = new();

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating an existing promotional offer
    /// </summary>
    public class UpdateOfferDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [StringLength(200, MinimumLength = 3)]
        public string? Name { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? HookText { get; set; }

        [Url]
        public string? BackgroundImageUrl { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        [RegularExpression("^[A-Z]{3}$")]
        public string? Currency { get; set; }

        [Range(1, int.MaxValue)]
        public int? ServiceUnitCount { get; set; }

        public bool? IsActive { get; set; }

        [Range(0, 10000)]
        public int? Priority { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxQuantity { get; set; }

        public OfferVisibility? Visibility { get; set; }

        public string? SpecialOfferCode { get; set; }

        public bool? IsSpecialOffer { get; set; }

        public TargetAudience? TargetAudience { get; set; }

        public List<string>? Features { get; set; }

        [Range(0, 100)]
        public int? GiveawayChancePercent { get; set; }

        [StringLength(50)]
        public string? HighlightBadge { get; set; }

        public EmailTemplateDto? EmailTemplate { get; set; }

        public List<AdminTaskDto>? AdminTasks { get; set; }

        public List<string>? Tags { get; set; }

        public Dictionary<string, string>? ExternalIds { get; set; }

        [Required]
        public string UpdatedByUserId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for validating an offer
    /// </summary>
    public class ValidateOfferDto
    {
        [Required]
        public string OfferId { get; set; } = string.Empty;

        public string? UserId { get; set; }

        public string? SpecialCode { get; set; }

        public string? SessionId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PurchaseAmount { get; set; }

        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// DTO for redeeming an offer
    /// </summary>
    public class RedeemOfferDto
    {
        [Required]
        public string OfferId { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string? SpecialCode { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PurchaseAmount { get; set; }

        public string? PaymentIntentId { get; set; }

        public string? OrderId { get; set; }

        public Dictionary<string, string> UserDetails { get; set; } = new();

        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// DTO for email template
    /// </summary>
    public class EmailTemplateDto
    {
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string HtmlContent { get; set; } = string.Empty;

        public List<string> Variables { get; set; } = new();
    }

    /// <summary>
    /// DTO for admin task
    /// </summary>
    public class AdminTaskDto
    {
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<string> NotifyEmails { get; set; } = new();

        public bool IncludeUserDetails { get; set; } = true;

        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        [StringLength(100)]
        public string Category { get; set; } = "General";

        public int? CompletionDeadlineMinutes { get; set; }

        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// DTO for offer display
    /// </summary>
    public class OfferDisplayDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HookText { get; set; } = string.Empty;
        public string? BackgroundImageUrl { get; set; }
        public string DisplayPrice { get; set; } = string.Empty;
        public int ServiceUnitCount { get; set; }
        public List<string> Features { get; set; } = new();
        public string? HighlightBadge { get; set; }
        public int? RemainingQuantity { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool RequiresCode { get; set; }
        public int GiveawayChancePercent { get; set; }
        public OfferVisibility Visibility { get; set; }
    }

    /// <summary>
    /// DTO for offer analytics summary
    /// </summary>
    public class OfferAnalyticsSummaryDto
    {
        public string OfferId { get; set; } = string.Empty;
        public string OfferName { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public long Conversions { get; set; }
        public decimal Revenue { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal ClickThroughRate { get; set; }
        public decimal AverageOrderValue { get; set; }
        public Dictionary<string, long> ConversionFunnel { get; set; } = new();
    }

    /// <summary>
    /// DTO for bulk offer operations
    /// </summary>
    public class BulkOfferOperationDto
    {
        [Required]
        [MinLength(1)]
        public List<string> OfferIds { get; set; } = new();

        [Required]
        public BulkOperationType Operation { get; set; }

        public Dictionary<string, object> Parameters { get; set; } = new();

        [Required]
        public string PerformedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of bulk operations
    /// </summary>
    public enum BulkOperationType
    {
        Activate,
        Deactivate,
        Archive,
        UpdatePriority,
        UpdateVisibility,
        Delete,
        ExtendExpiration
    }

    /// <summary>
    /// Result of offer redemption
    /// </summary>
    public class RedeemOfferResultDto
    {
        public bool Success { get; set; }
        public string? RedemptionId { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}