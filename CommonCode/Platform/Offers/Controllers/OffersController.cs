using BFormDomain.CommonCode.Platform.Offers.DTOs;
using BFormDomain.CommonCode.Platform.Offers.Services;
using BFormDomain.CommonCode.Platform.Offers.Domain;
using BFormDomain.CommonCode.Platform.Offers.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Controllers
{
    /// <summary>
    /// API controller for promotional offer management
    /// </summary>
    [ApiController]
    [Route("api/v1/tenants/{tenantId}/offers")]
    [Authorize]
    public class OffersController : ControllerBase
    {
        private readonly IPromotionalOfferService _offerService;
        private readonly ILogger<OffersController> _logger;

        public OffersController(
            IPromotionalOfferService offerService,
            ILogger<OffersController> logger)
        {
            _offerService = offerService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new promotional offer
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PromotionalOffer>> CreateOffer(
            [FromRoute] string tenantId, 
            [FromBody] CreateOfferDto dto)
        {
            try
            {
                var offer = await _offerService.CreateOfferAsync(tenantId, dto);
                return CreatedAtAction(nameof(GetOffer), new { tenantId, offerId = offer.Id }, offer);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific promotional offer
        /// </summary>
        [HttpGet("{offerId}")]
        public async Task<ActionResult<PromotionalOffer>> GetOffer(
            [FromRoute] string tenantId, 
            [FromRoute] string offerId)
        {
            var offer = await _offerService.GetOfferAsync(tenantId, offerId);
            if (offer == null)
            {
                return NotFound();
            }

            return Ok(offer);
        }

        /// <summary>
        /// Updates an existing promotional offer
        /// </summary>
        [HttpPut("{offerId}")]
        public async Task<ActionResult<PromotionalOffer>> UpdateOffer(
            [FromRoute] string tenantId, 
            [FromRoute] string offerId,
            [FromBody] UpdateOfferDto dto)
        {
            try
            {
                dto.Id = offerId; // Ensure ID matches route
                var offer = await _offerService.UpdateOfferAsync(tenantId, dto);
                return Ok(offer);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a promotional offer (soft delete)
        /// </summary>
        [HttpDelete("{offerId}")]
        public async Task<ActionResult> DeleteOffer(
            [FromRoute] string tenantId, 
            [FromRoute] string offerId,
            [FromQuery] string? deletedBy = null)
        {
            var success = await _offerService.DeleteOfferAsync(tenantId, offerId, deletedBy ?? "system");
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Searches promotional offers with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<OfferSearchResult>> SearchOffers(
            [FromRoute] string tenantId,
            [FromQuery] string? searchText = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] OfferVisibility? visibility = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] DateTime? expiringBefore = null,
            [FromQuery] DateTime? createdAfter = null,
            [FromQuery] string? servicePlanId = null,
            [FromQuery] bool? hasSpecialCode = null,
            [FromQuery] TargetAudience? targetAudience = null,
            [FromQuery] int? minServiceUnits = null,
            [FromQuery] int? maxServiceUnits = null,
            [FromQuery] string? tags = null,
            [FromQuery] OfferSortBy sortBy = OfferSortBy.Priority,
            [FromQuery] bool sortDescending = true,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            var criteria = new OfferSearchCriteria
            {
                SearchText = searchText,
                IsActive = isActive,
                Visibility = visibility,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                ExpiringBefore = expiringBefore,
                CreatedAfter = createdAfter,
                ServicePlanId = servicePlanId,
                HasSpecialCode = hasSpecialCode,
                TargetAudience = targetAudience,
                MinServiceUnits = minServiceUnits,
                MaxServiceUnits = maxServiceUnits,
                Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Split(',').ToList(),
                SortBy = sortBy,
                SortDescending = sortDescending,
                Skip = skip,
                Take = Math.Min(take, 100) // Limit maximum page size
            };

            var result = await _offerService.SearchOffersAsync(tenantId, criteria);
            return Ok(result);
        }

        /// <summary>
        /// Gets active offers for display (filtered by visibility)
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<OfferDisplayDto>>> GetActiveOffers(
            [FromRoute] string tenantId,
            [FromQuery] string? userId = null,
            [FromQuery] int? limit = null)
        {
            var offers = await _offerService.GetActiveOffersAsync(tenantId, userId, limit);
            return Ok(offers);
        }

        /// <summary>
        /// Validates an offer for a user
        /// </summary>
        [HttpPost("{offerId}/validate")]
        public async Task<ActionResult<OfferValidationResult>> ValidateOffer(
            [FromRoute] string tenantId,
            [FromRoute] string offerId,
            [FromBody] ValidateOfferDto dto)
        {
            dto.OfferId = offerId; // Ensure ID matches route
            var result = await _offerService.ValidateOfferAsync(tenantId, dto);
            return Ok(result);
        }

        /// <summary>
        /// Redeems a promotional offer
        /// </summary>
        [HttpPost("{offerId}/redeem")]
        public async Task<ActionResult<RedeemOfferResultDto>> RedeemOffer(
            [FromRoute] string tenantId,
            [FromRoute] string offerId,
            [FromBody] RedeemOfferDto dto)
        {
            dto.OfferId = offerId; // Ensure ID matches route
            var result = await _offerService.RedeemOfferAsync(tenantId, dto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        /// <summary>
        /// Performs bulk operations on multiple offers
        /// </summary>
        [HttpPost("bulk")]
        public async Task<ActionResult<BulkOperationResult>> BulkOperation(
            [FromRoute] string tenantId,
            [FromBody] BulkOfferOperationDto dto)
        {
            var result = await _offerService.PerformBulkOperationAsync(tenantId, dto);
            return Ok(result);
        }

        /// <summary>
        /// Updates offer priorities
        /// </summary>
        [HttpPut("priorities")]
        public async Task<ActionResult> UpdatePriorities(
            [FromRoute] string tenantId,
            [FromBody] List<OfferPriorityUpdate> updates)
        {
            await _offerService.UpdateOfferPrioritiesAsync(tenantId, updates);
            return NoContent();
        }

        /// <summary>
        /// Gets offers with low stock
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<ActionResult<List<OfferDisplayDto>>> GetLowStockOffers(
            [FromRoute] string tenantId,
            [FromQuery] int thresholdPercentage = 10)
        {
            var offers = await _offerService.GetLowStockOffersAsync(tenantId, thresholdPercentage);
            return Ok(offers);
        }

        /// <summary>
        /// Gets offers expiring soon
        /// </summary>
        [HttpGet("expiring")]
        public async Task<ActionResult<List<OfferDisplayDto>>> GetExpiringOffers(
            [FromRoute] string tenantId,
            [FromQuery] int daysAhead = 7)
        {
            var offers = await _offerService.GetExpiringOffersAsync(tenantId, daysAhead);
            return Ok(offers);
        }

        /// <summary>
        /// Generates a unique special offer code
        /// </summary>
        [HttpPost("generate-code")]
        public async Task<ActionResult<object>> GenerateUniqueCode(
            [FromRoute] string tenantId,
            [FromQuery] int length = 8)
        {
            try
            {
                var code = await _offerService.GenerateUniqueCodeAsync(tenantId, length);
                return Ok(new { code });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Checks if a special code is available
        /// </summary>
        [HttpGet("check-code/{code}")]
        public async Task<ActionResult<object>> CheckCodeAvailability(
            [FromRoute] string tenantId,
            [FromRoute] string code,
            [FromQuery] string? excludeOfferId = null)
        {
            var isAvailable = await _offerService.IsCodeAvailableAsync(tenantId, code, excludeOfferId);
            return Ok(new { available = isAvailable });
        }

        /// <summary>
        /// Gets statistics for a specific offer
        /// </summary>
        [HttpGet("{offerId}/statistics")]
        public async Task<ActionResult<OfferStatistics>> GetOfferStatistics(
            [FromRoute] string tenantId,
            [FromRoute] string offerId)
        {
            try
            {
                var statistics = await _offerService.GetOfferStatisticsAsync(tenantId, offerId);
                return Ok(statistics);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Clones an existing offer
        /// </summary>
        [HttpPost("{offerId}/clone")]
        public async Task<ActionResult<PromotionalOffer>> CloneOffer(
            [FromRoute] string tenantId,
            [FromRoute] string offerId,
            [FromQuery] string? clonedBy = null)
        {
            try
            {
                var clonedOffer = await _offerService.CloneOfferAsync(tenantId, offerId, clonedBy ?? "system");
                return CreatedAtAction(nameof(GetOffer), 
                    new { tenantId, offerId = clonedOffer.Id }, clonedOffer);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Sends a test email for an offer
        /// </summary>
        [HttpPost("{offerId}/test-email")]
        public async Task<ActionResult> SendTestEmail(
            [FromRoute] string tenantId,
            [FromRoute] string offerId,
            [FromBody] object request)
        {
            // Extract email from request body
            if (request is not System.Text.Json.JsonElement jsonElement ||
                !jsonElement.TryGetProperty("email", out var emailProperty))
            {
                return BadRequest(new { error = "Email address is required" });
            }

            var testEmail = emailProperty.GetString();
            if (string.IsNullOrWhiteSpace(testEmail))
            {
                return BadRequest(new { error = "Valid email address is required" });
            }

            var success = await _offerService.SendTestEmailAsync(tenantId, offerId, testEmail);
            return Ok(new { success });
        }

        /// <summary>
        /// Extends the expiration date of an offer
        /// </summary>
        [HttpPut("{offerId}/extend")]
        public async Task<ActionResult> ExtendExpiration(
            [FromRoute] string tenantId,
            [FromRoute] string offerId,
            [FromBody] object request)
        {
            // Extract new expiration from request body
            if (request is not System.Text.Json.JsonElement jsonElement ||
                !jsonElement.TryGetProperty("newExpiration", out var expirationProperty))
            {
                return BadRequest(new { error = "New expiration date is required" });
            }

            if (!DateTime.TryParse(expirationProperty.GetString(), out var newExpiration))
            {
                return BadRequest(new { error = "Invalid expiration date format" });
            }

            var extendedBy = "system";
            if (jsonElement.TryGetProperty("extendedBy", out var extendedByProperty))
            {
                extendedBy = extendedByProperty.GetString() ?? "system";
            }

            var success = await _offerService.ExtendOfferExpirationAsync(tenantId, offerId, newExpiration, extendedBy);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Gets recommended offers for a user
        /// </summary>
        [HttpGet("recommendations")]
        public async Task<ActionResult<List<OfferDisplayDto>>> GetRecommendedOffers(
            [FromRoute] string tenantId,
            [FromQuery] string userId,
            [FromQuery] int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new { error = "User ID is required" });
            }

            var offers = await _offerService.GetRecommendedOffersAsync(tenantId, userId, limit);
            return Ok(offers);
        }

        /// <summary>
        /// Archives expired offers for a tenant
        /// </summary>
        [HttpPost("archive-expired")]
        public async Task<ActionResult<object>> ArchiveExpiredOffers([FromRoute] string tenantId)
        {
            var count = await _offerService.ArchiveExpiredOffersAsync(tenantId);
            return Ok(new { archivedCount = count });
        }
    }
}