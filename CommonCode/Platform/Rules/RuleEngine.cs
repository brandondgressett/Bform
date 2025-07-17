using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Tenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Platform.Rules;

/// <summary>
/// The AppEventDistributer will take an instance of this and distribute events into
/// the rules.
/// 
/// 
///     -References:
///         >AppEventDistributer.cs
///         >TopicBinding.cs
///         >RuleServiceCollectionExtensions.cs
///     -Functions:
///         >MaybeInitialize
///         >ConsumeEvents
///         >RegisterTopics
/// </summary>
public class RuleEngine : IAppEventConsumer
{
    private readonly object _lock = new();
    private bool _isInitialized = false;
    private readonly IApplicationPlatformContent _content;
    private readonly ConcurrentDictionary<string, Rule> _rules = new();
    private readonly TopicRegistrations _topicRegistrations;
    private readonly RuleEvaluator _ruleEvaluator;
    private readonly ITenantContext _tenantContext;
    private readonly MultiTenancyOptions _multiTenancyOptions;
    private readonly ILogger<RuleEngine>? _logger;

    public RuleEngine(
        IApplicationPlatformContent content,
        TopicRegistrations registrations,
        RuleEvaluator ruleEvaluator,
        ITenantContext tenantContext,
        IOptions<MultiTenancyOptions> multiTenancyOptions,
        ILogger<RuleEngine>? logger = null)
    {
        _content = content;
        _topicRegistrations = registrations;
        _ruleEvaluator = ruleEvaluator;
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _multiTenancyOptions = multiTenancyOptions?.Value ?? throw new ArgumentNullException(nameof(multiTenancyOptions));
        _logger = logger;
    }

    private void MaybeInitialize()
    {
        if (_isInitialized)
            return;

        lock(_lock)
        {
            if(!_isInitialized)
            {
                var rules = _content.GetAllContent<Rule>();
                foreach (var rule in rules)
                {
                    var isValid = _ruleEvaluator.ValidateRule(rule);
                    if (isValid)
                    {
                        _rules[rule.Name] = rule;

                        foreach (var topic in rule.TopicBindings)
                            _topicRegistrations.Register(topic);
                    }
                }

                _isInitialized = true;
            }
        }

    }

    public string Id => nameof(RuleEngine);

    public async Task ConsumeEvents(AppEvent @event, IEnumerable<string> bindingId)
    {
        MaybeInitialize();

        // Validate tenant context if multi-tenancy is enabled
        if (_multiTenancyOptions.Enabled)
        {
            // For now, in Phase 3, we process all rules but pass tenant context
            // In Phase 4, rules will be tenant-specific
            _logger?.LogDebug(
                "Processing event {EventId} with topic {Topic} for tenant {TenantId}",
                @event.Id, @event.Topic, @event.TenantId);
        }

        var matches = bindingId
            .Where(it => _rules.ContainsKey(it))
            .Select(it => _rules[it])
            .GroupBy(it => it.DescendingOrder)
            .OrderByDescending(it => it.Key);

        foreach(var match in matches) // not parallel, to respect rule order.
            foreach (var rule in match)
            {
                try
                {
                    // Execute rule with tenant context from the event
                    await _ruleEvaluator.ExecuteRule(rule, @event);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, 
                        "Error executing rule {RuleName} for event {EventId} in tenant {TenantId}",
                        rule.Name, @event.Id, @event.TenantId);
                    // Continue processing other rules
                }
            }
    }
       

    public Task RegisterTopics(IAppEventConsumerRegistrar registrar)
    {
        MaybeInitialize();

        var allRules = _rules.Values;
        foreach(var rule in allRules)
        {
            foreach (var topic in rule.TopicBindings)
            {
                registrar.RegisterTopic(new TopicBinding(Consumer: this, Topic: topic, BindingId: rule.Name));
            }
        }

        return Task.CompletedTask;
        
    }
}
