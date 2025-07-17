using BFormDomain.CommonCode.Platform.Offers.DTOs;
using BFormDomain.CommonCode.Platform.Offers.Services;
using BFormDomain.CommonCode.Platform.Offers.Analytics;
using BFormDomain.CommonCode.Platform.Offers.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Controllers
{
    /// <summary>
    /// Public API controller for displaying promotional offers to end users
    /// </summary>
    [ApiController]
    [Route("api/v1/public/tenants/{tenantId}/offers")]
    public class PublicOffersController : ControllerBase
    {
        private readonly IPromotionalOfferService _offerService;
        private readonly IOfferAnalyticsRepository _analyticsRepository;
        private readonly ILogger<PublicOffersController> _logger;

        public PublicOffersController(
            IPromotionalOfferService offerService,
            IOfferAnalyticsRepository analyticsRepository,
            ILogger<PublicOffersController> logger)
        {
            _offerService = offerService;
            _analyticsRepository = analyticsRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets active offers visible to public or authenticated users
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<OfferDisplayDto>>> GetActiveOffers(
            [FromRoute] string tenantId,
            [FromQuery] string? userId = null,
            [FromQuery] int? limit = null,
            [FromQuery] string? source = null,
            [FromQuery] string? sessionId = null)
        {
            try
            {
                var offers = await _offerService.GetActiveOffersAsync(tenantId, userId, limit);

                // Track impression events for analytics
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    foreach (var offer in offers)
                    {
                        await TrackAnalyticsEventAsync(tenantId, offer.Id, userId, AnalyticsEventType.Impression, source, sessionId);
                    }
                }

                return Ok(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active offers for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "An error occurred while fetching offers" });
            }
        }

        /// <summary>
        /// Gets a specific offer by ID (public view)
        /// </summary>
        [HttpGet("{offerId}")]
        public async Task<ActionResult<OfferDisplayDto>> GetOffer(
            [FromRoute] string tenantId,
            [FromRoute] string offerId,
            [FromQuery] string? userId = null,
            [FromQuery] string? source = null,
            [FromQuery] string? sessionId = null)
        {
            try
            {
                var offer = await _offerService.GetOfferAsync(tenantId, offerId);
                if (offer == null)
                {
                    return NotFound();
                }

                // Check visibility rules
                if (offer.Visibility == Domain.OfferVisibility.MemberOnly && string.IsNullOrWhiteSpace(userId))
                {
                    return Forbid("This offer is only available to authenticated users");
                }

                if (offer.Visibility == Domain.OfferVisibility.SpecialCode)
                {
                    return Forbid("This offer requires a special code");
                }

                // Validate offer is still available
                var validation = await _offerService.ValidateOfferAsync(tenantId, new ValidateOfferDto
                {
                    OfferId = offerId,
                    UserId = userId,
                    SessionId = sessionId
                });

                if (!validation.IsValid)
                {
                    return BadRequest(new { error = string.Join(", ", validation.Errors) });
                }

                // Track impression
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    await TrackAnalyticsEventAsync(tenantId, offerId, userId, AnalyticsEventType.Impression, source, sessionId);
                }

                // Convert to display DTO
                var displayDto = MapToDisplayDto(offer);
                return Ok(displayDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offer {OfferId} for tenant {TenantId}", offerId, tenantId);
                return StatusCode(500, new { error = "An error occurred while fetching the offer" });
            }
        }

        /// <summary>
        /// Gets an offer by special code
        /// </summary>
        [HttpGet("by-code/{code}")]
        public async Task<ActionResult<OfferDisplayDto>> GetOfferByCode(
            [FromRoute] string tenantId,
            [FromRoute] string code,
            [FromQuery] string? userId = null,
            [FromQuery] string? source = null,
            [FromQuery] string? sessionId = null)
        {
            try
            {
                var offer = await _offerService.GetOfferByCodeAsync(tenantId, code, userId);
                if (offer == null)
                {
                    return NotFound(new { error = "Offer not found or not available" });
                }

                // Track impression
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    await TrackAnalyticsEventAsync(tenantId, offer.Id, userId, AnalyticsEventType.Impression, source, sessionId, code);
                }

                return Ok(offer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offer by code {Code} for tenant {TenantId}", code, tenantId);
                return StatusCode(500, new { error = "An error occurred while fetching the offer" });
            }
        }

        /// <summary>
        /// Validates an offer for potential redemption
        /// </summary>
        [HttpPost("{offerId}/validate")]
        public async Task<ActionResult<object>> ValidateOffer(
            [FromRoute] string tenantId,
            [FromRoute] string offerId,
            [FromBody] ValidateOfferRequestDto request)
        {
            try
            {
                var validation = await _offerService.ValidateOfferAsync(tenantId, new ValidateOfferDto
                {
                    OfferId = offerId,
                    UserId = request.UserId,
                    SpecialCode = request.SpecialCode,
                    SessionId = request.SessionId,
                    PurchaseAmount = request.PurchaseAmount,
                    Metadata = request.Metadata
                });

                // Track validation attempt
                if (!string.IsNullOrWhiteSpace(request.SessionId))
                {
                    await TrackAnalyticsEventAsync(tenantId, offerId, request.UserId, 
                        AnalyticsEventType.Click, request.Source, request.SessionId);
                }

                return Ok(new
                {
                    isValid = validation.IsValid,
                    errors = validation.Errors,
                    failureReason = validation.FailureReason?.ToString(),
                    offer = validation.Offer != null ? MapToDisplayDto(validation.Offer) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating offer {OfferId} for tenant {TenantId}", offerId, tenantId);
                return StatusCode(500, new { error = "An error occurred while validating the offer" });
            }
        }

        /// <summary>
        /// Gets recommended offers for a user
        /// </summary>
        [HttpGet("recommendations")]
        public async Task<ActionResult<List<OfferDisplayDto>>> GetRecommendedOffers(
            [FromRoute] string tenantId,
            [FromQuery] string userId,
            [FromQuery] int limit = 5,
            [FromQuery] string? source = null,
            [FromQuery] string? sessionId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { error = "User ID is required for recommendations" });
                }

                var offers = await _offerService.GetRecommendedOffersAsync(tenantId, userId, limit);

                // Track impression events
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    foreach (var offer in offers)
                    {
                        await TrackAnalyticsEventAsync(tenantId, offer.Id, userId, AnalyticsEventType.Impression, source, sessionId);
                    }
                }

                return Ok(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations for user {UserId} in tenant {TenantId}", userId, tenantId);
                return StatusCode(500, new { error = "An error occurred while fetching recommendations" });
            }
        }

        /// <summary>
        /// Records an analytics event (click, view, etc.)
        /// </summary>
        [HttpPost("{offerId}/analytics")]
        public async Task<ActionResult> RecordAnalyticsEvent(
            [FromRoute] string tenantId,
            [FromRoute] string offerId,
            [FromBody] AnalyticsEventRequestDto request)
        {
            try
            {
                await TrackAnalyticsEventAsync(tenantId, offerId, request.UserId, request.EventType, 
                    request.Source, request.SessionId, request.SpecialCode, request.Value, request.Metadata);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording analytics event for offer {OfferId} in tenant {TenantId}", offerId, tenantId);
                return StatusCode(500, new { error = "An error occurred while recording the event" });
            }
        }

        /// <summary>
        /// Gets offer summary for preview (minimal data for card display)
        /// </summary>
        [HttpGet("{offerId}/preview")]
        public async Task<ActionResult<object>> GetOfferPreview(
            [FromRoute] string tenantId,
            [FromRoute] string offerId,
            [FromQuery] string? userId = null)
        {
            try
            {
                var offer = await _offerService.GetOfferAsync(tenantId, offerId);
                if (offer == null)
                {
                    return NotFound();
                }

                // Check basic visibility
                if (offer.Visibility == Domain.OfferVisibility.MemberOnly && string.IsNullOrWhiteSpace(userId))
                {
                    return Forbid();
                }

                if (offer.Visibility == Domain.OfferVisibility.SpecialCode)
                {
                    return Forbid();
                }

                // Return minimal preview data
                return Ok(new
                {
                    id = offer.Id,
                    name = offer.Name,
                    hookText = offer.HookText,
                    displayPrice = $"{offer.PriceInCents / 100m:C}",
                    highlightBadge = offer.HighlightBadge,
                    backgroundImageUrl = offer.BackgroundImageUrl,
                    expiresAt = offer.ExpiresAt,
                    isActive = offer.IsActive,
                    remainingQuantity = offer.MaxQuantity.HasValue ? offer.MaxQuantity.Value - offer.SoldCount : (int?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offer preview {OfferId} for tenant {TenantId}", offerId, tenantId);
                return StatusCode(500, new { error = "An error occurred while fetching the offer preview" });
            }
        }

        private async Task TrackAnalyticsEventAsync(
            string tenantId, 
            string offerId, 
            string? userId, 
            AnalyticsEventType eventType, 
            string? source = null, 
            string? sessionId = null,
            string? specialCode = null,
            decimal? value = null,
            Dictionary<string, string>? metadata = null)
        {
            try
            {
                var analyticsEvent = new AnalyticsEvent
                {
                    TenantId = tenantId,
                    OfferId = offerId,
                    UserId = userId,
                    Type = eventType,
                    Timestamp = DateTime.UtcNow,
                    Source = source ?? "web",
                    SessionId = sessionId ?? Guid.NewGuid().ToString(),
                    UserAgent = Request.Headers.TryGetValue("User-Agent", out var userAgent) ? userAgent.ToString() : "",
                    IpAddress = GetClientIpAddress(),
                    ValueInCents = value.HasValue ? (long)(value.Value * 100) : null,
                    Metadata = metadata?.ToDictionary(k => k.Key, k => (object)k.Value) ?? new Dictionary<string, object>()
                };

                if (!string.IsNullOrWhiteSpace(specialCode))
                {
                    analyticsEvent.Metadata["specialCode"] = specialCode;
                }

                await _analyticsRepository.RecordEventAsync(analyticsEvent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to track analytics event for offer {OfferId}", offerId);
                // Don't throw - analytics failures shouldn't break the main flow
            }
        }

        private string GetClientIpAddress()
        {
            // Try to get the real IP from reverse proxy headers
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.ToString().Split(',')[0].Trim();
            }

            if (Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                return realIp.ToString();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static OfferDisplayDto MapToDisplayDto(Domain.PromotionalOffer offer)
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
                RequiresCode = offer.Visibility == Domain.OfferVisibility.SpecialCode,
                GiveawayChancePercent = offer.GiveawayChancePercent,
                Visibility = offer.Visibility
            };
        }
    }

    /// <summary>
    /// Request DTO for offer validation
    /// </summary>
    public class ValidateOfferRequestDto
    {
        public string? UserId { get; set; }
        public string? SpecialCode { get; set; }
        public string? SessionId { get; set; }
        public decimal? PurchaseAmount { get; set; }
        public string? Source { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Request DTO for analytics events
    /// </summary>
    public class AnalyticsEventRequestDto
    {
        public string? UserId { get; set; }
        public AnalyticsEventType EventType { get; set; }
        public string? Source { get; set; }
        public string? SessionId { get; set; }
        public string? SpecialCode { get; set; }
        public decimal? Value { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}