using BFormDomain.CommonCode.Platform.Offers.Domain;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Offers.Repository.MongoDB
{
    /// <summary>
    /// MongoDB implementation of the promotional offer repository
    /// </summary>
    public class MongoPromotionalOfferRepository : MongoRepository<PromotionalOffer>, IPromotionalOfferRepository
    {
        private readonly ICacheService? _cache;
        private readonly new ILogger<MongoPromotionalOfferRepository>? _logger;

        protected override string CollectionName => "promotionalOffers";

        public MongoPromotionalOfferRepository(
            IOptions<MongoRepositoryOptions> options,
            SimpleApplicationAlert alerts,
            ICacheService? cache = null,
            ILogger<MongoPromotionalOfferRepository>? logger = null)
            : base(options, alerts, logger)
        {
            _cache = cache;
            _logger = logger;
            
            // Create indexes on initialization
            Task.Run(() => CreateIndexesAsync());
        }

        protected override IMongoCollection<PromotionalOffer> CreateCollection()
        {
            return OpenCollection();
        }

        /// <summary>
        /// Creates optimized indexes for offer queries
        /// </summary>
        private async Task CreateIndexesAsync()
        {
            try
            {
                var collection = OpenCollection();
                var indexKeys = Builders<PromotionalOffer>.IndexKeys;

                // Compound index for tenant + active + priority
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<PromotionalOffer>(
                    indexKeys.Ascending(x => x.TenantId)
                        .Ascending(x => x.IsActive)
                        .Descending(x => x.Priority),
                    new CreateIndexOptions { Name = "TenantActivePriority" }));

                // Special code index (unique within tenant)
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<PromotionalOffer>(
                    indexKeys.Ascending(x => x.TenantId)
                        .Ascending(x => x.SpecialOfferCode),
                    new CreateIndexOptions 
                    { 
                        Name = "TenantSpecialCode",
                        Unique = true, 
                        Sparse = true 
                    }));

                // Service plan index
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<PromotionalOffer>(
                    indexKeys.Ascending(x => x.TenantId)
                        .Ascending(x => x.ServicePlanId),
                    new CreateIndexOptions { Name = "TenantServicePlan" }));

                // Expiration index
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<PromotionalOffer>(
                    indexKeys.Ascending(x => x.ExpiresAt),
                    new CreateIndexOptions 
                    { 
                        Name = "ExpiresAt",
                        Sparse = true 
                    }));

                // Text search index
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<PromotionalOffer>(
                    indexKeys.Text(x => x.Name)
                        .Text(x => x.Description)
                        .Text(x => x.HookText),
                    new CreateIndexOptions { Name = "TextSearch" }));

                _logger?.LogInformation("Promotional offer indexes created successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create promotional offer indexes");
            }
        }

        public async Task<PromotionalOffer?> GetByIdAsync(string tenantId, string offerId)
        {
            if (!Guid.TryParse(offerId, out var guidId))
                return null;

            var (offer, _) = await LoadAsync(guidId);
            return offer?.TenantId == tenantId ? offer : null;
        }

        public async Task<List<PromotionalOffer>> GetActiveOffersAsync(string tenantId, int? limit = null)
        {
            var cacheKey = $"offers:active:{tenantId}:{limit}";
            
            if (_cache != null)
            {
                var cached = await _cache.GetAsync<List<PromotionalOffer>>(cacheKey);
                if (cached != null) return cached;
            }

            var now = DateTime.UtcNow;
            var filter = Builders<PromotionalOffer>.Filter.And(
                Builders<PromotionalOffer>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<PromotionalOffer>.Filter.Eq(x => x.IsActive, true),
                Builders<PromotionalOffer>.Filter.Or(
                    Builders<PromotionalOffer>.Filter.Exists(x => x.ExpiresAt, false),
                    Builders<PromotionalOffer>.Filter.Gte(x => x.ExpiresAt, now)
                )
            );

            // Add sold out filter
            var notSoldOutFilter = Builders<PromotionalOffer>.Filter.Or(
                Builders<PromotionalOffer>.Filter.Exists(x => x.MaxQuantity, false),
                Builders<PromotionalOffer>.Filter.Where(x => x.MaxQuantity == null || x.SoldCount < x.MaxQuantity)
            );

            filter = Builders<PromotionalOffer>.Filter.And(filter, notSoldOutFilter);

            var collection = OpenCollection();
            var sort = Builders<PromotionalOffer>.Sort
                .Descending(x => x.Priority)
                .Descending(x => x.CreatedDate);
            
            var query = collection.Find(filter).Sort(sort);

            if (limit.HasValue)
                query = query.Limit(limit.Value);

            var result = await query.ToListAsync();

            if (_cache != null)
            {
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            }

            return result;
        }

        public async Task<PromotionalOffer?> GetBySpecialCodeAsync(string tenantId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var normalizedCode = code.Trim().ToUpperInvariant();
            var filter = Builders<PromotionalOffer>.Filter.And(
                Builders<PromotionalOffer>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<PromotionalOffer>.Filter.Eq(x => x.SpecialOfferCode, normalizedCode)
            );

            var collection = OpenCollection();
            return await collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<PromotionalOffer>> GetByServicePlanAsync(string tenantId, string servicePlanId)
        {
            var filter = Builders<PromotionalOffer>.Filter.And(
                Builders<PromotionalOffer>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<PromotionalOffer>.Filter.Eq(x => x.ServicePlanId, servicePlanId)
            );

            var collection = OpenCollection();
            return await collection.Find(filter)
                .SortByDescending(x => x.Priority)
                .ToListAsync();
        }

        public async Task<List<PromotionalOffer>> GetExpiringOffersAsync(string tenantId, DateTime beforeDate)
        {
            var filter = Builders<PromotionalOffer>.Filter.And(
                Builders<PromotionalOffer>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<PromotionalOffer>.Filter.Exists(x => x.ExpiresAt, true),
                Builders<PromotionalOffer>.Filter.Lt(x => x.ExpiresAt, beforeDate),
                Builders<PromotionalOffer>.Filter.Gt(x => x.ExpiresAt, DateTime.UtcNow)
            );

            var collection = OpenCollection();
            return await collection.Find(filter)
                .SortBy(x => x.ExpiresAt)
                .ToListAsync();
        }

        public async Task<OfferSearchResult> SearchOffersAsync(string tenantId, OfferSearchCriteria criteria)
        {
            var collection = OpenCollection();
            var filterBuilder = Builders<PromotionalOffer>.Filter;
            var filters = new List<FilterDefinition<PromotionalOffer>>
            {
                filterBuilder.Eq(x => x.TenantId, tenantId)
            };

            // Apply search filters
            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                filters.Add(filterBuilder.Text(criteria.SearchText));
            }

            if (criteria.IsActive.HasValue)
            {
                filters.Add(filterBuilder.Eq(x => x.IsActive, criteria.IsActive.Value));
            }

            if (criteria.Visibility.HasValue)
            {
                filters.Add(filterBuilder.Eq(x => x.Visibility, criteria.Visibility.Value));
            }

            if (criteria.MinPrice.HasValue)
            {
                filters.Add(filterBuilder.Gte(x => x.PriceInCents, (long)(criteria.MinPrice.Value * 100)));
            }

            if (criteria.MaxPrice.HasValue)
            {
                filters.Add(filterBuilder.Lte(x => x.PriceInCents, (long)(criteria.MaxPrice.Value * 100)));
            }

            if (criteria.ExpiringBefore.HasValue)
            {
                filters.Add(filterBuilder.And(
                    filterBuilder.Exists(x => x.ExpiresAt, true),
                    filterBuilder.Lt(x => x.ExpiresAt, criteria.ExpiringBefore.Value)
                ));
            }

            if (criteria.CreatedAfter.HasValue)
            {
                filters.Add(filterBuilder.Gte(x => x.CreatedDate, criteria.CreatedAfter.Value));
            }

            if (!string.IsNullOrWhiteSpace(criteria.ServicePlanId))
            {
                filters.Add(filterBuilder.Eq(x => x.ServicePlanId, criteria.ServicePlanId));
            }

            if (criteria.HasSpecialCode.HasValue)
            {
                if (criteria.HasSpecialCode.Value)
                {
                    filters.Add(filterBuilder.And(
                        filterBuilder.Exists(x => x.SpecialOfferCode, true),
                        filterBuilder.Ne(x => x.SpecialOfferCode, string.Empty)
                    ));
                }
                else
                {
                    filters.Add(filterBuilder.Or(
                        filterBuilder.Exists(x => x.SpecialOfferCode, false),
                        filterBuilder.Eq(x => x.SpecialOfferCode, string.Empty)
                    ));
                }
            }

            if (criteria.TargetAudience.HasValue)
            {
                filters.Add(filterBuilder.Eq(x => x.TargetAudience, criteria.TargetAudience.Value));
            }

            if (criteria.MinServiceUnits.HasValue)
            {
                filters.Add(filterBuilder.Gte(x => x.ServiceUnitCount, criteria.MinServiceUnits.Value));
            }

            if (criteria.MaxServiceUnits.HasValue)
            {
                filters.Add(filterBuilder.Lte(x => x.ServiceUnitCount, criteria.MaxServiceUnits.Value));
            }

            if (criteria.Tags != null && criteria.Tags.Any())
            {
                filters.Add(filterBuilder.AnyIn(x => x.Tags, criteria.Tags));
            }

            var finalFilter = filterBuilder.And(filters);

            // Count total
            var totalCount = await collection.CountDocumentsAsync(finalFilter);

            // Build sort
            var sortBuilder = Builders<PromotionalOffer>.Sort;
            SortDefinition<PromotionalOffer> sort = criteria.SortBy switch
            {
                OfferSortBy.Name => criteria.SortDescending ? 
                    sortBuilder.Descending(x => x.Name) : sortBuilder.Ascending(x => x.Name),
                OfferSortBy.Price => criteria.SortDescending ? 
                    sortBuilder.Descending(x => x.PriceInCents) : sortBuilder.Ascending(x => x.PriceInCents),
                OfferSortBy.CreatedAt => criteria.SortDescending ? 
                    sortBuilder.Descending(x => x.CreatedDate) : sortBuilder.Ascending(x => x.CreatedDate),
                OfferSortBy.UpdatedAt => criteria.SortDescending ? 
                    sortBuilder.Descending(x => x.UpdatedDate) : sortBuilder.Ascending(x => x.UpdatedDate),
                OfferSortBy.SoldCount => criteria.SortDescending ? 
                    sortBuilder.Descending(x => x.SoldCount) : sortBuilder.Ascending(x => x.SoldCount),
                OfferSortBy.ExpiresAt => criteria.SortDescending ? 
                    sortBuilder.Descending(x => x.ExpiresAt) : sortBuilder.Ascending(x => x.ExpiresAt),
                OfferSortBy.ServiceUnitCount => criteria.SortDescending ? 
                    sortBuilder.Descending(x => x.ServiceUnitCount) : sortBuilder.Ascending(x => x.ServiceUnitCount),
                _ => criteria.SortDescending ? 
                    sortBuilder.Descending(x => x.Priority) : sortBuilder.Ascending(x => x.Priority)
            };

            // Execute query
            var offers = await collection.Find(finalFilter)
                .Sort(sort)
                .Skip(criteria.Skip)
                .Limit(criteria.Take)
                .ToListAsync();

            return new OfferSearchResult
            {
                Offers = offers,
                TotalCount = (int)totalCount,
                PageNumber = criteria.Skip / criteria.Take,
                PageSize = criteria.Take
            };
        }

        public async Task<bool> IncrementSoldCountAsync(string tenantId, string offerId, int increment = 1)
        {
            if (!Guid.TryParse(offerId, out var guidId))
                return false;

            var filter = Builders<PromotionalOffer>.Filter.And(
                Builders<PromotionalOffer>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<PromotionalOffer>.Filter.Eq(x => x.Id, guidId)
            );

            var update = Builders<PromotionalOffer>.Update
                .Inc(x => x.SoldCount, increment)
                .Set(x => x.UpdatedDate, DateTime.UtcNow);

            var collection = OpenCollection();
            var result = await collection.UpdateOneAsync(filter, update);

            // Invalidate cache
            if (_cache != null)
            {
                await _cache.RemoveAsync($"offers:active:{tenantId}:*");
            }

            return result.ModifiedCount > 0;
        }

        public async Task UpdateOfferPrioritiesAsync(string tenantId, List<OfferPriorityUpdate> updates)
        {
            var collection = OpenCollection();
            var bulkOps = new List<WriteModel<PromotionalOffer>>();

            foreach (var update in updates)
            {
                if (Guid.TryParse(update.OfferId, out var guidId))
                {
                    var filter = Builders<PromotionalOffer>.Filter.And(
                        Builders<PromotionalOffer>.Filter.Eq(x => x.TenantId, tenantId),
                        Builders<PromotionalOffer>.Filter.Eq(x => x.Id, guidId)
                    );

                    var updateDef = Builders<PromotionalOffer>.Update
                        .Set(x => x.Priority, update.Priority)
                        .Set(x => x.UpdatedDate, DateTime.UtcNow);

                    bulkOps.Add(new UpdateOneModel<PromotionalOffer>(filter, updateDef));
                }
            }

            if (bulkOps.Any())
            {
                await collection.BulkWriteAsync(bulkOps);

                // Invalidate cache
                if (_cache != null)
                {
                    await _cache.RemoveAsync($"offers:active:{tenantId}:*");
                }
            }
        }

        public async Task<bool> IsSpecialCodeUniqueAsync(string tenantId, string code, string? excludeOfferId = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var normalizedCode = code.Trim().ToUpperInvariant();
            var filterBuilder = Builders<PromotionalOffer>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(x => x.TenantId, tenantId),
                filterBuilder.Eq(x => x.SpecialOfferCode, normalizedCode)
            );

            if (!string.IsNullOrWhiteSpace(excludeOfferId) && Guid.TryParse(excludeOfferId, out var excludeGuid))
            {
                filter = filterBuilder.And(filter, filterBuilder.Ne(x => x.Id, excludeGuid));
            }

            var collection = OpenCollection();
            var count = await collection.CountDocumentsAsync(filter);
            return count == 0;
        }

        public async Task<OfferValidationResult> ValidateOfferAsync(string tenantId, string offerId, string? userId)
        {
            var result = new OfferValidationResult();
            
            var offer = await GetByIdAsync(tenantId, offerId);
            if (offer == null)
            {
                result.IsValid = false;
                result.Errors.Add("Offer not found");
                result.FailureReason = ValidationFailureReason.NotFound;
                return result;
            }

            result.Offer = offer;

            // Check active
            if (!offer.IsActive)
            {
                result.IsValid = false;
                result.Errors.Add("Offer is not active");
                result.FailureReason = ValidationFailureReason.Inactive;
                return result;
            }

            // Check expiration
            if (offer.ExpiresAt.HasValue && offer.ExpiresAt.Value < DateTime.UtcNow)
            {
                result.IsValid = false;
                result.Errors.Add("Offer has expired");
                result.FailureReason = ValidationFailureReason.Expired;
                return result;
            }

            // Check quantity limit
            if (offer.MaxQuantity.HasValue && offer.SoldCount >= offer.MaxQuantity.Value)
            {
                result.IsValid = false;
                result.Errors.Add("Offer is sold out");
                result.FailureReason = ValidationFailureReason.SoldOut;
                return result;
            }

            // Check visibility
            if (offer.Visibility == OfferVisibility.MemberOnly && string.IsNullOrWhiteSpace(userId))
            {
                result.IsValid = false;
                result.Errors.Add("This offer is for tenant members only");
                result.FailureReason = ValidationFailureReason.InvalidVisibility;
                return result;
            }

            result.IsValid = true;
            return result;
        }

        public async Task<List<PromotionalOffer>> GetVisibleOffersAsync(string tenantId, string? userId = null, int? limit = null)
        {
            var offers = await GetActiveOffersAsync(tenantId, limit);
            
            // Filter by visibility
            return offers.Where(o =>
            {
                return o.Visibility switch
                {
                    OfferVisibility.Public => true,
                    OfferVisibility.MemberOnly => !string.IsNullOrWhiteSpace(userId),
                    OfferVisibility.SpecialCode => false, // Don't show special code offers in general lists
                    _ => false
                };
            }).ToList();
        }

        public async Task<long> CountOffersAsync(string tenantId, bool activeOnly = false)
        {
            var filterBuilder = Builders<PromotionalOffer>.Filter;
            var filter = filterBuilder.Eq(x => x.TenantId, tenantId);

            if (activeOnly)
            {
                filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.IsActive, true));
            }

            var collection = OpenCollection();
            return await collection.CountDocumentsAsync(filter);
        }

        public async Task<List<PromotionalOffer>> GetOffersByDateRangeAsync(string tenantId, DateTime startDate, DateTime endDate)
        {
            var filter = Builders<PromotionalOffer>.Filter.And(
                Builders<PromotionalOffer>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<PromotionalOffer>.Filter.Gte(x => x.CreatedDate, startDate),
                Builders<PromotionalOffer>.Filter.Lte(x => x.CreatedDate, endDate)
            );

            var collection = OpenCollection();
            return await collection.Find(filter)
                .SortByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task ArchiveOfferAsync(string tenantId, string offerId)
        {
            if (!Guid.TryParse(offerId, out var guidId))
                return;

            var filter = Builders<PromotionalOffer>.Filter.And(
                Builders<PromotionalOffer>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<PromotionalOffer>.Filter.Eq(x => x.Id, guidId)
            );

            var update = Builders<PromotionalOffer>.Update
                .Set(x => x.IsActive, false)
                .Set(x => x.UpdatedDate, DateTime.UtcNow)
                .AddToSet(x => x.Tags, "archived");

            var collection = OpenCollection();
            await collection.UpdateOneAsync(filter, update);

            // Invalidate cache
            if (_cache != null)
            {
                await _cache.RemoveAsync($"offers:active:{tenantId}:*");
            }
        }

        public async Task BulkUpdateVisibilityAsync(string tenantId, List<string> offerIds, OfferVisibility visibility)
        {
            var guidIds = offerIds
                .Where(id => Guid.TryParse(id, out _))
                .Select(id => Guid.Parse(id))
                .ToList();

            if (!guidIds.Any())
                return;

            var filter = Builders<PromotionalOffer>.Filter.And(
                Builders<PromotionalOffer>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<PromotionalOffer>.Filter.In(x => x.Id, guidIds)
            );

            var update = Builders<PromotionalOffer>.Update
                .Set(x => x.Visibility, visibility)
                .Set(x => x.UpdatedDate, DateTime.UtcNow);

            var collection = OpenCollection();
            await collection.UpdateManyAsync(filter, update);

            // Invalidate cache
            if (_cache != null)
            {
                await _cache.RemoveAsync($"offers:active:{tenantId}:*");
            }
        }

        public async Task<List<PromotionalOffer>> GetLowStockOffersAsync(string tenantId, int thresholdPercentage = 10)
        {
            var collection = OpenCollection();
            
            // This requires a more complex query using aggregation
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "TenantId", tenantId },
                    { "IsActive", true },
                    { "MaxQuantity", new BsonDocument("$exists", true) }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "stockPercentage", new BsonDocument("$multiply", new BsonArray 
                        { 
                            new BsonDocument("$divide", new BsonArray 
                                { 
                                    new BsonDocument("$subtract", new BsonArray { "$MaxQuantity", "$SoldCount" }), 
                                    "$MaxQuantity" 
                                }), 
                            100 
                        })
                    }
                }),
                new BsonDocument("$match", new BsonDocument
                {
                    { "stockPercentage", new BsonDocument("$lte", thresholdPercentage) }
                }),
                new BsonDocument("$sort", new BsonDocument("stockPercentage", 1))
            };

            var result = await collection.Aggregate<PromotionalOffer>(pipeline).ToListAsync();
            return result;
        }
    }
}