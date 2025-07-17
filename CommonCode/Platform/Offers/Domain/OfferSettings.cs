using BFormDomain.CommonCode.Platform.Entity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace BFormDomain.CommonCode.Platform.Offers.Domain
{
    /// <summary>
    /// Tenant-specific settings for promotional offers functionality
    /// </summary>
    public class OfferSettings : AppEntityBase
    {
        public OfferSettings()
        {
            EntityType = "OfferSettings";
        }

        /// <summary>
        /// Maximum number of offers to display to users at once
        /// </summary>
        public int MaxDisplayedOffers { get; set; } = 3;

        /// <summary>
        /// Whether to hide all offers when the tenant is at full capacity
        /// </summary>
        public bool HideOffersWhenFull { get; set; } = true;

        /// <summary>
        /// Whether the giveaway feature is enabled for this tenant
        /// </summary>
        public bool GiveawayEnabled { get; set; } = true;

        /// <summary>
        /// Default email settings for offer communications
        /// </summary>
        public OfferEmailSettings EmailSettings { get; set; } = new();

        /// <summary>
        /// Display preferences for the offer system
        /// </summary>
        public OfferDisplayPreferences DisplayPreferences { get; set; } = new();

        /// <summary>
        /// Analytics retention settings
        /// </summary>
        public AnalyticsSettings Analytics { get; set; } = new();

        /// <summary>
        /// Code generation settings for special offers
        /// </summary>
        public CodeGenerationSettings CodeGeneration { get; set; } = new();

        /// <summary>
        /// Notification preferences for admin tasks
        /// </summary>
        public AdminNotificationSettings AdminNotifications { get; set; } = new();

        public override Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
        {
            var baseUri = $"/offersettings/{(template ? "template" : "instance")}/{Id}";
            if (vm)
                baseUri += "/vm";
            if (!string.IsNullOrWhiteSpace(queryParameters))
                baseUri += $"?{queryParameters}";
            return new Uri(baseUri, UriKind.Relative);
        }
    }

    /// <summary>
    /// Email configuration for offer-related communications
    /// </summary>
    public class OfferEmailSettings
    {
        /// <summary>
        /// Whether to send automatic purchase confirmation emails
        /// </summary>
        public bool SendPurchaseConfirmation { get; set; } = true;

        /// <summary>
        /// Default "from" name for offer emails
        /// </summary>
        public string FromName { get; set; } = "Promotional Offers";

        /// <summary>
        /// Default "from" email address
        /// </summary>
        public string FromEmail { get; set; } = "offers@example.com";

        /// <summary>
        /// Reply-to email address
        /// </summary>
        public string ReplyToEmail { get; set; } = "support@example.com";

        /// <summary>
        /// BCC email addresses for all offer-related emails
        /// </summary>
        public List<string> BccEmails { get; set; } = new();

        /// <summary>
        /// Footer text to append to all offer emails
        /// </summary>
        public string FooterText { get; set; } = string.Empty;

        /// <summary>
        /// Whether to include company branding in emails
        /// </summary>
        public bool IncludeBranding { get; set; } = true;
    }

    /// <summary>
    /// Display preferences for offers
    /// </summary>
    public class OfferDisplayPreferences
    {
        /// <summary>
        /// Whether to show the sold count on offers
        /// </summary>
        public bool ShowSoldCount { get; set; } = false;

        /// <summary>
        /// Whether to show remaining quantity on limited offers
        /// </summary>
        public bool ShowRemainingQuantity { get; set; } = true;

        /// <summary>
        /// Whether to show expiration countdown
        /// </summary>
        public bool ShowExpirationCountdown { get; set; } = true;

        /// <summary>
        /// Default sort order for offers
        /// </summary>
        public OfferSortOrder DefaultSortOrder { get; set; } = OfferSortOrder.Priority;

        /// <summary>
        /// Whether to highlight new offers
        /// </summary>
        public bool HighlightNewOffers { get; set; } = true;

        /// <summary>
        /// Number of days an offer is considered "new"
        /// </summary>
        public int NewOfferDays { get; set; } = 7;

        /// <summary>
        /// Custom CSS for offer display
        /// </summary>
        public string CustomCss { get; set; } = string.Empty;
    }

    /// <summary>
    /// Analytics configuration
    /// </summary>
    public class AnalyticsSettings
    {
        /// <summary>
        /// How many days to retain detailed analytics data
        /// </summary>
        public int DetailedRetentionDays { get; set; } = 90;

        /// <summary>
        /// How many days to retain aggregated analytics data
        /// </summary>
        public int AggregatedRetentionDays { get; set; } = 365;

        /// <summary>
        /// Whether to track individual user interactions
        /// </summary>
        public bool TrackUserInteractions { get; set; } = true;

        /// <summary>
        /// Whether to enable real-time analytics
        /// </summary>
        public bool EnableRealTimeAnalytics { get; set; } = true;

        /// <summary>
        /// Minimum sample size for reporting
        /// </summary>
        public int MinimumSampleSize { get; set; } = 10;
    }

    /// <summary>
    /// Settings for special offer code generation
    /// </summary>
    public class CodeGenerationSettings
    {
        /// <summary>
        /// Default length for generated codes
        /// </summary>
        public int DefaultCodeLength { get; set; } = 8;

        /// <summary>
        /// Characters to use in code generation
        /// </summary>
        public string AllowedCharacters { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// Prefix to add to all generated codes
        /// </summary>
        public string CodePrefix { get; set; } = string.Empty;

        /// <summary>
        /// Whether to use case-sensitive codes
        /// </summary>
        public bool CaseSensitive { get; set; } = false;

        /// <summary>
        /// Characters to avoid in code generation (confusing characters)
        /// </summary>
        public string ExcludedCharacters { get; set; } = "0O1IL";

        /// <summary>
        /// Format pattern for codes (e.g., "XXXX-XXXX")
        /// </summary>
        public string FormatPattern { get; set; } = string.Empty;
    }

    /// <summary>
    /// Settings for admin notifications
    /// </summary>
    public class AdminNotificationSettings
    {
        /// <summary>
        /// Whether admin notifications are enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Default admin emails for all notifications
        /// </summary>
        public List<string> DefaultAdminEmails { get; set; } = new();

        /// <summary>
        /// Whether to batch notifications
        /// </summary>
        public bool BatchNotifications { get; set; } = false;

        /// <summary>
        /// Batch interval in minutes
        /// </summary>
        public int BatchIntervalMinutes { get; set; } = 60;

        /// <summary>
        /// Priority threshold for immediate notifications (bypasses batching)
        /// </summary>
        public TaskPriority ImmediateNotificationThreshold { get; set; } = TaskPriority.High;

        /// <summary>
        /// Whether to send daily summary emails
        /// </summary>
        public bool SendDailySummary { get; set; } = true;

        /// <summary>
        /// Time to send daily summary (in UTC hours, 0-23)
        /// </summary>
        public int DailySummaryHourUtc { get; set; } = 9;
    }

    /// <summary>
    /// Sort order options for offers
    /// </summary>
    public enum OfferSortOrder
    {
        Priority,
        Name,
        Price,
        CreatedDate,
        ExpirationDate,
        Popularity
    }
}