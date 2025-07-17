using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Authorization;
using Microsoft.Extensions.Logging;
using System;

namespace BFormDomain.CommonCode.Platform.AppEvents
{
    /// <summary>
    /// Factory for creating AppEvents with proper tenant context and inheritance rules.
    /// Ensures all events are properly stamped with tenant information.
    /// </summary>
    public class TenantAwareEventFactory
    {
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<TenantAwareEventFactory>? _logger;

        public TenantAwareEventFactory(
            ITenantContext tenantContext,
            ILogger<TenantAwareEventFactory>? logger = null)
        {
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _logger = logger;
        }

        /// <summary>
        /// Creates a new AppEvent with proper tenant context
        /// </summary>
        public AppEvent CreateEvent(
            string topic,
            string? action,
            IAppEntity entity,
            Guid? userId,
            AppEventOrigin? origin = null)
        {
            // Determine tenant ID based on inheritance rules
            var tenantId = DetermineTenantId(entity, origin, userId);

            var appEvent = new AppEvent
            {
                Id = Guid.NewGuid(),
                Version = 0,
                TenantId = tenantId,
                Topic = topic,
                ActionId = action,
                OriginEntityType = entity.EntityType,
                OriginEntityTemplate = entity.Template,
                OriginEntityId = entity.Id,
                HostWorkSet = entity.HostWorkSet,
                HostWorkItem = entity.HostWorkItem,
                IsNatural = origin?.Preceding == null,
                OriginUser = userId,
                Created = DateTime.UtcNow,
                TakenExpiration = DateTime.MinValue,
                State = AppEventState.New,
                EntityTags = entity.Tags.ToList()
            };

            // Handle event lineage if this is a derived event
            if (origin?.Preceding != null)
            {
                InheritFromPrecedingEvent(appEvent, origin);
            }
            else
            {
                // This is a natural event (user-initiated)
                appEvent.EventLine = Guid.NewGuid();
                appEvent.EventGeneration = 0;
            }

            return appEvent;
        }

        /// <summary>
        /// Determines the tenant ID for an event based on inheritance rules:
        /// 1. Entity's tenant (if entity implements ITenantScoped)
        /// 2. Preceding event's tenant (if this is a derived event)
        /// 3. Current user's tenant context
        /// 4. Global tenant (for system events)
        /// </summary>
        private Guid DetermineTenantId(IAppEntity entity, AppEventOrigin? origin, Guid? userId)
        {
            // Rule 1: If entity is tenant-scoped, use its tenant
            if (entity is ITenantScoped tenantScopedEntity && tenantScopedEntity.TenantId != Guid.Empty)
            {
                _logger?.LogDebug(
                    "Event tenant determined from entity: {EntityType} {EntityId} -> Tenant {TenantId}",
                    entity.EntityType, entity.Id, tenantScopedEntity.TenantId);
                return tenantScopedEntity.TenantId;
            }

            // Rule 2: If this is a derived event, inherit from preceding event
            if (origin?.Preceding != null && origin.Preceding.TenantId != Guid.Empty)
            {
                _logger?.LogDebug(
                    "Event tenant inherited from preceding event: {PrecedingId} -> Tenant {TenantId}",
                    origin.Preceding.Id, origin.Preceding.TenantId);
                return origin.Preceding.TenantId;
            }

            // Rule 3: Use current tenant context
            if (_tenantContext.CurrentTenantId.HasValue)
            {
                _logger?.LogDebug(
                    "Event tenant from current context: Tenant {TenantId}",
                    _tenantContext.CurrentTenantId.Value);
                return _tenantContext.CurrentTenantId.Value;
            }

            // Rule 4: System event - use global tenant
            if (!_tenantContext.IsMultiTenancyEnabled)
            {
                // Single-tenant mode - get global tenant from context
                var globalTenant = _tenantContext.CurrentTenantId ?? Guid.Empty;
                _logger?.LogDebug("Event tenant set to global tenant (single-tenant mode): {TenantId}", globalTenant);
                return globalTenant;
            }

            // This should not happen in a properly configured system
            _logger?.LogWarning(
                "Unable to determine tenant for event. Entity: {EntityType} {EntityId}, User: {UserId}",
                entity.EntityType, entity.Id, userId);
            throw new InvalidOperationException(
                $"Cannot determine tenant context for event. Entity: {entity.EntityType} {entity.Id}");
        }

        /// <summary>
        /// Inherits properties from a preceding event in an event chain
        /// </summary>
        private void InheritFromPrecedingEvent(AppEvent newEvent, AppEventOrigin origin)
        {
            var parent = origin.Preceding!;

            // Inherit event lineage
            newEvent.EventLine = parent.EventLine;
            newEvent.EventGeneration = parent.EventGeneration + 1;
            newEvent.GeneratorId = origin.Generator;
            newEvent.ActionId = origin.Action;
            newEvent.IsNatural = false;

            // Inherit tags from parent
            if (parent.Tags.Any())
            {
                newEvent.Tags.AddRange(parent.Tags);
            }

            // Inherit seal status
            newEvent.Seal = parent.Seal;

            // Log inheritance chain
            _logger?.LogDebug(
                "Event inherited from parent: {ParentId} -> {NewId}, Generation: {Generation}, Tenant: {TenantId}",
                parent.Id, newEvent.Id, newEvent.EventGeneration, newEvent.TenantId);
        }

        /// <summary>
        /// Validates that an event belongs to the expected tenant
        /// </summary>
        public bool ValidateEventTenant(AppEvent appEvent, Guid expectedTenantId)
        {
            if (appEvent.TenantId != expectedTenantId)
            {
                _logger?.LogWarning(
                    "Event tenant mismatch: Event {EventId} has tenant {EventTenant} but expected {ExpectedTenant}",
                    appEvent.Id, appEvent.TenantId, expectedTenantId);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Creates a system event (e.g., monitoring, alerts) that uses the global tenant
        /// </summary>
        public AppEvent CreateSystemEvent(
            string topic,
            string action,
            IAppEntity entity,
            Guid? affectedTenantId = null)
        {
            // System events always use the global tenant context
            var systemEvent = new AppEvent
            {
                Id = Guid.NewGuid(),
                Version = 0,
                TenantId = Guid.Empty, // Will be set by global tenant context
                Topic = topic,
                ActionId = action,
                OriginEntityType = entity.EntityType,
                OriginEntityTemplate = entity.Template,
                OriginEntityId = entity.Id,
                IsNatural = false,
                Created = DateTime.UtcNow,
                TakenExpiration = DateTime.MinValue,
                State = AppEventState.New,
                EventLine = Guid.NewGuid(),
                EventGeneration = 0,
                EntityTags = entity.Tags.ToList()
            };

            // Track affected tenant if applicable
            if (affectedTenantId.HasValue)
            {
                systemEvent.AddTag($"AffectedTenant:{affectedTenantId}");
            }

            _logger?.LogInformation(
                "Created system event: {Topic} {Action} for entity {EntityType} {EntityId}",
                topic, action, entity.EntityType, entity.Id);

            return systemEvent;
        }
    }
}