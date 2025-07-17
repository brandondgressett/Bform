using BFormDomain.CommonCode.Platform.Offers.Analytics;
using BFormDomain.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Repository
{
    /// <summary>
    /// Repository interface for offer analytics operations
    /// </summary>
    public interface IOfferAnalyticsRepository : IRepository<OfferAnalytics>
    {
        /// <summary>
        /// Gets or creates analytics for a specific period
        /// </summary>
        Task<OfferAnalytics> GetOrCreateAnalyticsAsync(
            string tenantId, 
            string offerId, 
            AnalyticsPeriod period, 
            DateTime date);

        /// <summary>
        /// Records an analytics event
        /// </summary>
        Task RecordEventAsync(AnalyticsEvent analyticsEvent);

        /// <summary>
        /// Gets analytics data for a date range
        /// </summary>
        Task<List<OfferAnalytics>> GetAnalyticsRangeAsync(
            string tenantId, 
            string offerId, 
            DateTime startDate, 
            DateTime endDate,
            AnalyticsPeriod? period = null);

        /// <summary>
        /// Compares metrics across multiple offers
        /// </summary>
        Task<Dictionary<string, OfferMetrics>> GetOfferComparisonAsync(
            string tenantId, 
            List<string> offerIds, 
            AnalyticsPeriod period,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Gets top performing offers by metric
        /// </summary>
        Task<List<OfferPerformanceRanking>> GetTopOffersAsync(
            string tenantId,
            PerformanceMetric metric,
            int limit = 10,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Aggregates analytics data for a higher period
        /// </summary>
        Task<OfferAnalytics> AggregateAnalyticsAsync(
            string tenantId,
            string offerId,
            AnalyticsPeriod targetPeriod,
            DateTime periodStart,
            DateTime periodEnd);

        /// <summary>
        /// Gets conversion funnel data for an offer
        /// </summary>
        Task<ConversionFunnel> GetConversionFunnelAsync(
            string tenantId,
            string offerId,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Gets analytics by channel/source
        /// </summary>
        Task<Dictionary<string, ChannelMetrics>> GetChannelAnalyticsAsync(
            string tenantId,
            string offerId,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Cleans up old analytics data based on retention policy
        /// </summary>
        Task<int> CleanupOldAnalyticsAsync(string tenantId, int retentionDays);

        /// <summary>
        /// Gets real-time analytics (last hour)
        /// </summary>
        Task<RealTimeAnalytics> GetRealTimeAnalyticsAsync(string tenantId, string offerId);

        /// <summary>
        /// Batch records multiple analytics events
        /// </summary>
        Task BatchRecordEventsAsync(List<AnalyticsEvent> events);

        /// <summary>
        /// Gets user journey analytics for a specific user
        /// </summary>
        Task<UserJourneyAnalytics> GetUserJourneyAsync(
            string tenantId,
            string userId,
            string offerId);

        /// <summary>
        /// Gets trending offers based on recent activity
        /// </summary>
        Task<List<TrendingOffer>> GetTrendingOffersAsync(
            string tenantId,
            int hoursBack = 24,
            int limit = 10);
    }

    /// <summary>
    /// Performance metrics for ranking
    /// </summary>
    public enum PerformanceMetric
    {
        Revenue,
        Conversions,
        ConversionRate,
        ClickThroughRate,
        AverageOrderValue,
        Impressions
    }

    /// <summary>
    /// Offer performance ranking
    /// </summary>
    public class OfferPerformanceRanking
    {
        public string OfferId { get; set; } = string.Empty;
        public string OfferName { get; set; } = string.Empty;
        public decimal MetricValue { get; set; }
        public int Rank { get; set; }
        public OfferMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Real-time analytics snapshot
    /// </summary>
    public class RealTimeAnalytics
    {
        public DateTime Timestamp { get; set; }
        public int ActiveUsers { get; set; }
        public int RecentImpressions { get; set; }
        public int RecentClicks { get; set; }
        public int RecentConversions { get; set; }
        public decimal RecentRevenue { get; set; }
        public List<RecentEvent> RecentEvents { get; set; } = new();
    }

    /// <summary>
    /// Recent event for real-time display
    /// </summary>
    public class RecentEvent
    {
        public DateTime Timestamp { get; set; }
        public AnalyticsEventType Type { get; set; }
        public string? UserId { get; set; }
        public string? Source { get; set; }
        public decimal? Value { get; set; }
    }

    /// <summary>
    /// User journey through offer interactions
    /// </summary>
    public class UserJourneyAnalytics
    {
        public string UserId { get; set; } = string.Empty;
        public string OfferId { get; set; } = string.Empty;
        public List<JourneyStep> Steps { get; set; } = new();
        public TimeSpan TotalJourneyTime { get; set; }
        public bool Converted { get; set; }
        public decimal? ConversionValue { get; set; }
    }

    /// <summary>
    /// Individual step in user journey
    /// </summary>
    public class JourneyStep
    {
        public DateTime Timestamp { get; set; }
        public AnalyticsEventType EventType { get; set; }
        public string? Source { get; set; }
        public TimeSpan TimeSincePrevious { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Trending offer information
    /// </summary>
    public class TrendingOffer
    {
        public string OfferId { get; set; } = string.Empty;
        public string OfferName { get; set; } = string.Empty;
        public decimal TrendScore { get; set; }
        public int RecentImpressions { get; set; }
        public int RecentClicks { get; set; }
        public int RecentConversions { get; set; }
        public decimal GrowthRate { get; set; }
    }

    /// <summary>
    /// Analytics by channel
    /// </summary>
    public class ChannelMetrics
    {
        public string Channel { get; set; } = string.Empty;
        public int Impressions { get; set; }
        public int Clicks { get; set; }
        public int Conversions { get; set; }
        public decimal Revenue { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal ClickThroughRate { get; set; }
    }
}