using BFormDomain.CommonCode.Platform.Entity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace BFormDomain.CommonCode.Platform.Offers.Analytics
{
    /// <summary>
    /// Aggregated analytics data for a promotional offer over a specific time period
    /// </summary>
    public class OfferAnalytics : AppEntityBase
    {
        public OfferAnalytics()
        {
            EntityType = "OfferAnalytics";
        }

        /// <summary>
        /// The promotional offer this analytics data relates to
        /// </summary>
        public string OfferId { get; set; } = string.Empty;

        /// <summary>
        /// The analytics period this data covers
        /// </summary>
        public AnalyticsPeriod Period { get; set; }

        /// <summary>
        /// Start of the period this analytics covers
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// End of the period this analytics covers
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Core metrics for the period
        /// </summary>
        public OfferMetrics Metrics { get; set; } = new();

        /// <summary>
        /// Conversion funnel data
        /// </summary>
        public ConversionFunnel ConversionFunnel { get; set; } = new();

        /// <summary>
        /// Breakdown by source/channel
        /// </summary>
        public Dictionary<string, ChannelMetrics> ChannelBreakdown { get; set; } = new();

        /// <summary>
        /// Daily breakdown (for longer periods)
        /// </summary>
        public List<DailyMetrics> DailyBreakdown { get; set; } = new();

        /// <summary>
        /// When this analytics record was last updated
        /// </summary>
        public DateTime LastCalculated { get; set; }

        /// <summary>
        /// Version number for analytics calculation logic
        /// </summary>
        public int CalculationVersion { get; set; } = 1;

        public override Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
        {
            var baseUri = $"/analytics/offers/{(template ? "template" : "instance")}/{Id}";
            if (vm)
                baseUri += "/vm";
            if (!string.IsNullOrWhiteSpace(queryParameters))
                baseUri += $"?{queryParameters}";
            return new Uri(baseUri, UriKind.Relative);
        }
    }

    /// <summary>
    /// Core metrics for offer performance
    /// </summary>
    public class OfferMetrics
    {
        /// <summary>
        /// Number of times the offer was displayed
        /// </summary>
        public long Impressions { get; set; }

        /// <summary>
        /// Number of unique users who saw the offer
        /// </summary>
        public long UniqueVisitors { get; set; }

        /// <summary>
        /// Number of clicks/interactions with the offer
        /// </summary>
        public long Clicks { get; set; }

        /// <summary>
        /// Number of completed purchases
        /// </summary>
        public long Conversions { get; set; }

        /// <summary>
        /// Total revenue generated in cents
        /// </summary>
        public long RevenueInCents { get; set; }

        /// <summary>
        /// Number of abandoned checkouts
        /// </summary>
        public long AbandonedCheckouts { get; set; }

        /// <summary>
        /// Average time to conversion in seconds
        /// </summary>
        public double AverageTimeToConversionSeconds { get; set; }

        /// <summary>
        /// Calculated average order value
        /// </summary>
        public decimal AverageOrderValue => Conversions > 0 ? RevenueInCents / (decimal)Conversions / 100m : 0;

        /// <summary>
        /// Calculated conversion rate as percentage
        /// </summary>
        public decimal ConversionRate => Clicks > 0 ? (decimal)Conversions / Clicks * 100 : 0;

        /// <summary>
        /// Calculated click-through rate as percentage
        /// </summary>
        public decimal ClickThroughRate => Impressions > 0 ? (decimal)Clicks / Impressions * 100 : 0;

        /// <summary>
        /// Cart abandonment rate as percentage
        /// </summary>
        public decimal AbandonmentRate => 
            (Conversions + AbandonedCheckouts) > 0 
                ? (decimal)AbandonedCheckouts / (Conversions + AbandonedCheckouts) * 100 
                : 0;
    }

    /// <summary>
    /// Conversion funnel tracking
    /// </summary>
    public class ConversionFunnel
    {
        /// <summary>
        /// Users who viewed the offer
        /// </summary>
        public long Viewed { get; set; }

        /// <summary>
        /// Users who clicked/interacted
        /// </summary>
        public long Clicked { get; set; }

        /// <summary>
        /// Users who started checkout
        /// </summary>
        public long StartedCheckout { get; set; }

        /// <summary>
        /// Users who completed purchase
        /// </summary>
        public long Completed { get; set; }

        /// <summary>
        /// View to click conversion rate
        /// </summary>
        public decimal ViewToClickRate => Viewed > 0 ? (decimal)Clicked / Viewed * 100 : 0;

        /// <summary>
        /// Click to checkout conversion rate
        /// </summary>
        public decimal ClickToCheckoutRate => Clicked > 0 ? (decimal)StartedCheckout / Clicked * 100 : 0;

        /// <summary>
        /// Checkout to completion rate
        /// </summary>
        public decimal CheckoutCompletionRate => StartedCheckout > 0 ? (decimal)Completed / StartedCheckout * 100 : 0;

        /// <summary>
        /// Overall conversion rate
        /// </summary>
        public decimal OverallConversionRate => Viewed > 0 ? (decimal)Completed / Viewed * 100 : 0;
    }

    /// <summary>
    /// Metrics broken down by channel/source
    /// </summary>
    public class ChannelMetrics
    {
        public string ChannelName { get; set; } = string.Empty;
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public long Conversions { get; set; }
        public long RevenueInCents { get; set; }
        public decimal ConversionRate => Clicks > 0 ? (decimal)Conversions / Clicks * 100 : 0;
    }

    /// <summary>
    /// Daily metrics for trend analysis
    /// </summary>
    public class DailyMetrics
    {
        public DateTime Date { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public long Conversions { get; set; }
        public long RevenueInCents { get; set; }
    }

    /// <summary>
    /// Time periods for analytics aggregation
    /// </summary>
    public enum AnalyticsPeriod
    {
        Hour,
        Day,
        Week,
        Month,
        Quarter,
        Year,
        AllTime,
        Custom
    }

    /// <summary>
    /// Individual analytics event for tracking
    /// </summary>
    public class AnalyticsEvent : AppEntityBase
    {
        public AnalyticsEvent()
        {
            EntityType = "AnalyticsEvent";
        }

        /// <summary>
        /// Type of analytics event
        /// </summary>
        public AnalyticsEventType Type { get; set; }

        /// <summary>
        /// The offer this event relates to
        /// </summary>
        public string OfferId { get; set; } = string.Empty;

        /// <summary>
        /// User who triggered the event (if authenticated)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Anonymous session identifier
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Value associated with the event (e.g., purchase amount in cents)
        /// </summary>
        public long? ValueInCents { get; set; }

        /// <summary>
        /// When the event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Source/channel of the event
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// User agent string
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// IP address (for geo-location)
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Additional event metadata
        /// </summary>
        [BsonExtraElements]
        public Dictionary<string, object> Metadata { get; set; } = new();

        public override Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
        {
            var baseUri = $"/analytics/events/{(template ? "template" : "instance")}/{Id}";
            if (vm)
                baseUri += "/vm";
            if (!string.IsNullOrWhiteSpace(queryParameters))
                baseUri += $"?{queryParameters}";
            return new Uri(baseUri, UriKind.Relative);
        }
    }

    /// <summary>
    /// Types of analytics events
    /// </summary>
    public enum AnalyticsEventType
    {
        /// <summary>
        /// Offer was displayed to user
        /// </summary>
        Impression,

        /// <summary>
        /// User clicked on or interacted with offer
        /// </summary>
        Click,

        /// <summary>
        /// User started checkout process
        /// </summary>
        CheckoutStart,

        /// <summary>
        /// User completed purchase
        /// </summary>
        Purchase,

        /// <summary>
        /// User abandoned checkout
        /// </summary>
        Abandonment,

        /// <summary>
        /// Offer was shared by user
        /// </summary>
        Share,

        /// <summary>
        /// Special offer code was validated
        /// </summary>
        CodeValidation,

        /// <summary>
        /// Special offer code validation failed
        /// </summary>
        CodeValidationFailed
    }
}