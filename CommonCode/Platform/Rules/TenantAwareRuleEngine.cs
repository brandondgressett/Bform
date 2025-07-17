using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Tenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Platform.Rules;

/// <summary>
/// Tenant-aware rule engine that loads and executes rules specific to each tenant.
/// Each tenant has its own isolated set of rules loaded from tenant-specific content repositories.
/// </summary>
public class TenantAwareRuleEngine : IAppEventConsumer
{
    private readonly ConcurrentDictionary<Guid, RuleEngineInstance> _tenantRuleEngines = new();
    private readonly ITenantContentRepositoryFactory _contentRepositoryFactory;
    private readonly TopicRegistrations _topicRegistrations;
    private readonly RuleEvaluator _ruleEvaluator;
    private readonly ITenantContext _tenantContext;
    private readonly MultiTenancyOptions _multiTenancyOptions;
    private readonly ILogger<TenantAwareRuleEngine> _logger;
    private readonly object _lockObject = new();

    /// <summary>
    /// Represents a rule engine instance for a specific tenant.
    /// </summary>
    private class RuleEngineInstance
    {
        public Guid TenantId { get; init; }
        public ConcurrentDictionary<string, Rule> Rules { get; } = new();
        public bool IsInitialized { get; set; }
        public DateTime LastInitialized { get; set; }
        public object InitializationLock { get; } = new();
    }

    public TenantAwareRuleEngine(
        ITenantContentRepositoryFactory contentRepositoryFactory,
        TopicRegistrations registrations,
        RuleEvaluator ruleEvaluator,
        ITenantContext tenantContext,
        IOptions<MultiTenancyOptions> multiTenancyOptions,
        ILogger<TenantAwareRuleEngine> logger)
    {
        _contentRepositoryFactory = contentRepositoryFactory ?? throw new ArgumentNullException(nameof(contentRepositoryFactory));
        _topicRegistrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _ruleEvaluator = ruleEvaluator ?? throw new ArgumentNullException(nameof(ruleEvaluator));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _multiTenancyOptions = multiTenancyOptions?.Value ?? throw new ArgumentNullException(nameof(multiTenancyOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Id => nameof(TenantAwareRuleEngine);

    /// <summary>
    /// Gets or creates a rule engine instance for the specified tenant.
    /// </summary>
    private RuleEngineInstance GetTenantRuleEngine(Guid tenantId)
    {
        return _tenantRuleEngines.GetOrAdd(tenantId, id =>
        {
            _logger.LogInformation("Creating rule engine instance for tenant {TenantId}", id);
            return new RuleEngineInstance 
            { 
                TenantId = id,
                LastInitialized = DateTime.MinValue
            };
        });
    }

    /// <summary>
    /// Initializes rules for a specific tenant if not already initialized.
    /// </summary>
    private void MaybeInitializeTenant(Guid tenantId)
    {
        var ruleEngine = GetTenantRuleEngine(tenantId);
        
        if (ruleEngine.IsInitialized)
            return;

        lock (ruleEngine.InitializationLock)
        {
            if (!ruleEngine.IsInitialized)
            {
                _logger.LogInformation("Initializing rules for tenant {TenantId}", tenantId);
                
                try
                {
                    // Get tenant-specific content repository
                    var contentRepository = _contentRepositoryFactory.GetTenantContentRepository(tenantId);
                    
                    // Load rules specific to this tenant
                    var rules = contentRepository.GetAllContent<Rule>();
                    
                    foreach (var rule in rules)
                    {
                        var isValid = _ruleEvaluator.ValidateRule(rule);
                        if (isValid)
                        {
                            ruleEngine.Rules[rule.Name] = rule;

                            // Register topics for this tenant's rules
                            foreach (var topic in rule.TopicBindings)
                                _topicRegistrations.Register(topic);
                                
                            _logger.LogDebug("Loaded rule '{RuleName}' for tenant {TenantId}", rule.Name, tenantId);
                        }
                        else
                        {
                            _logger.LogWarning("Skipped invalid rule '{RuleName}' for tenant {TenantId}", rule.Name, tenantId);
                        }
                    }

                    ruleEngine.IsInitialized = true;
                    ruleEngine.LastInitialized = DateTime.UtcNow;
                    
                    _logger.LogInformation("Initialized {RuleCount} rules for tenant {TenantId}", 
                        ruleEngine.Rules.Count, tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize rules for tenant {TenantId}", tenantId);
                    throw;
                }
            }
        }
    }

    public async Task ConsumeEvents(AppEvent @event, IEnumerable<string> bindingId)
    {
        var eventTenantId = @event.TenantId;
        
        if (_multiTenancyOptions.Enabled)
        {
            // Validate that the event has a valid tenant ID
            if (eventTenantId == Guid.Empty)
            {
                _logger.LogWarning("Received event {EventId} with empty tenant ID, skipping rule processing", @event.Id);
                return;
            }

            _logger.LogDebug("Processing event {EventId} with topic {Topic} for tenant {TenantId}",
                @event.Id, @event.Topic, eventTenantId);
        }
        else
        {
            // In single-tenant mode, use global tenant
            eventTenantId = _multiTenancyOptions.GlobalTenantId;
        }

        // Initialize rules for this tenant if needed
        MaybeInitializeTenant(eventTenantId);
        
        var ruleEngine = GetTenantRuleEngine(eventTenantId);

        // Find matching rules for this tenant
        var matches = bindingId
            .Where(id => ruleEngine.Rules.ContainsKey(id))
            .Select(id => ruleEngine.Rules[id])
            .GroupBy(rule => rule.DescendingOrder)
            .OrderByDescending(group => group.Key);

        foreach (var match in matches) // not parallel, to respect rule order.
            foreach (var rule in match)
            {
                try
                {
                    // Tenant boundary enforcement: Only execute rule if event belongs to same tenant
                    if (_multiTenancyOptions.Enabled && eventTenantId != ruleEngine.TenantId)
                    {
                        _logger.LogWarning(
                            "Tenant boundary violation: Event {EventId} from tenant {EventTenantId} cannot execute rule {RuleName} from tenant {RuleTenantId}",
                            @event.Id, eventTenantId, rule.Name, ruleEngine.TenantId);
                        continue;
                    }

                    // Execute rule with tenant context from the event
                    await _ruleEvaluator.ExecuteRule(rule, @event);
                    
                    _logger.LogDebug("Executed rule {RuleName} for event {EventId} in tenant {TenantId}",
                        rule.Name, @event.Id, eventTenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error executing rule {RuleName} for event {EventId} in tenant {TenantId}",
                        rule.Name, @event.Id, eventTenantId);
                    // Continue processing other rules
                }
            }
    }

    public Task RegisterTopics(IAppEventConsumerRegistrar registrar)
    {
        // Register topics from all initialized tenants
        foreach (var (tenantId, ruleEngine) in _tenantRuleEngines)
        {
            if (ruleEngine.IsInitialized)
            {
                foreach (var rule in ruleEngine.Rules.Values)
                {
                    foreach (var topic in rule.TopicBindings)
                    {
                        registrar.RegisterTopic(new TopicBinding(
                            Consumer: this, 
                            Topic: topic, 
                            BindingId: rule.Name));
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Forces reinitialization of rules for a specific tenant.
    /// Useful when tenant content is updated.
    /// </summary>
    public void ReloadTenantRules(Guid tenantId)
    {
        if (_tenantRuleEngines.TryGetValue(tenantId, out var ruleEngine))
        {
            lock (ruleEngine.InitializationLock)
            {
                _logger.LogInformation("Reloading rules for tenant {TenantId}", tenantId);
                
                ruleEngine.Rules.Clear();
                ruleEngine.IsInitialized = false;
                ruleEngine.LastInitialized = DateTime.MinValue;
                
                // Force re-initialization on next event
                MaybeInitializeTenant(tenantId);
            }
        }
    }

    /// <summary>
    /// Removes a tenant's rule engine instance from memory.
    /// </summary>
    public void RemoveTenant(Guid tenantId)
    {
        if (_tenantRuleEngines.TryRemove(tenantId, out var ruleEngine))
        {
            _logger.LogInformation("Removed rule engine for tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Gets statistics about loaded rule engines.
    /// </summary>
    public (int TenantCount, int TotalRules) GetStatistics()
    {
        var tenantCount = _tenantRuleEngines.Count;
        var totalRules = _tenantRuleEngines.Values.Sum(re => re.Rules.Count);
        return (tenantCount, totalRules);
    }

    /// <summary>
    /// Gets rule count for a specific tenant.
    /// </summary>
    public int GetTenantRuleCount(Guid tenantId)
    {
        return _tenantRuleEngines.TryGetValue(tenantId, out var ruleEngine) 
            ? ruleEngine.Rules.Count 
            : 0;
    }
}