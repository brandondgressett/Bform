using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules.EventAppenders;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Platform.Rules;

/// <summary>
/// Must be a singleton.
/// 
///     -References:
///         >RuleActionForEach.cs
///         >RuleEvaluator.cs
///         >RuleServiceCollectionExtensions.cs
///     -Functions:
///         >ValidateRule
///         >PerformAction
///         >AppendData
///         >ExecuteRule
///         >EvaluateConditions
///         >ExecuteActions
/// </summary>
public class RuleEvaluator
{

    private readonly IApplicationAlert _alerts;
    private readonly ILogger<RuleEvaluator> _logger;
    private readonly ConcurrentDictionary<string, IEventAppender> _appenders = new();
    private readonly ConcurrentDictionary<string, IRuleActionEvaluator> _actions = new();
    private readonly JObject _jPathTestObject;
    private readonly RuleEvaluatorOptions _options;
    private readonly IDataEnvironment _dataEnvironment;
    private readonly ITenantContext _tenantContext;
    private readonly MultiTenancyOptions _multiTenancyOptions;
    private readonly TenantBoundaryEnforcer _boundaryEnforcer;

    public RuleEvaluator(
        IApplicationAlert alerts, 
        ILogger<RuleEvaluator> logger,
        IDataEnvironment dataEnvironment,
        IOptions<RuleEvaluatorOptions> options,
        IEnumerable<IEventAppender> appenders,
        IEnumerable<IRuleActionEvaluator> actions,
        ITenantContext tenantContext,
        IOptions<MultiTenancyOptions> multiTenancyOptions,
        TenantBoundaryEnforcer boundaryEnforcer)
    {

        _alerts = alerts;
        _logger = logger;
        _dataEnvironment = dataEnvironment;
        _options = options.Value;
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _multiTenancyOptions = multiTenancyOptions?.Value ?? throw new ArgumentNullException(nameof(multiTenancyOptions));
        _boundaryEnforcer = boundaryEnforcer ?? throw new ArgumentNullException(nameof(boundaryEnforcer));

        foreach(var appender in appenders)
            _appenders[appender.Name] = appender;

        foreach(var action in actions)
            _actions[action.Name] = action;

        _jPathTestObject = JObject.Parse("{ }");
    }

    public bool ValidateRule(Rule rule)
    {
        bool isValid = true;

        foreach(var condition in rule.Conditions)
        {
            foreach(var appender in condition.Append.EmptyIfNull())
            {
                if(!_appenders.ContainsKey(appender.Name))
                {
                    isValid = false;
                    _alerts.RaiseAlert(ApplicationAlertKind.Defect,
                        LogLevel.Error,
                        $"Rule {rule.Name} refers to invalid appender {appender.Name}.");
                }
            }

            try
            {
                _jPathTestObject.SelectTokens(condition.Query);
            }
            catch (JsonException)
            {
                isValid = false;
                _alerts.RaiseAlert(ApplicationAlertKind.Defect,
                    LogLevel.Error,
                    $"Rule {rule.Name} has invalid condition query '{condition.Query}'");
            }
        }

        foreach(var action in rule.Actions)
        {
            foreach(var appender in action.AppendBefore.EmptyIfNull())
            {
                if (!_appenders.ContainsKey(appender.Name))
                {
                    isValid = false;
                    _alerts.RaiseAlert(ApplicationAlertKind.Defect,
                        LogLevel.Error,
                        $"Rule {rule.Name} refers to invalid appender {appender.Name}.");
                }
            }

            foreach (var appender in action.AppendAfter.EmptyIfNull())
            {
                if (!_appenders.ContainsKey(appender.Name))
                {
                    isValid = false;
                    _alerts.RaiseAlert(ApplicationAlertKind.Defect,
                        LogLevel.Error,
                        $"Rule {rule.Name} refers to invalid appender {appender.Name}.");
                }
            }

            if(!_actions.ContainsKey(action.Invoke.Name))
            {
                isValid = false;
                _alerts.RaiseAlert(ApplicationAlertKind.Defect,
                    LogLevel.Error,
                    $"Rule {rule.Name} refers to invalid action {action.Invoke.Name}");
            }
        }

        if(!isValid)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Defect,
                LogLevel.Error,
                $"Rule {rule.Name} failed validation.");
        }

        return isValid; 
    }


    private async Task PerformAction(ITransactionContext ctx, AppEvent appEvent, JObject jsonEvent, RuleAction action, IEnumerable<string>? ruleTags)
    {
        var invocation = action.Invoke;
        var executor = _actions[invocation.Name];
        
        // Enforce tenant boundary for rule action execution
        if (!_boundaryEnforcer.ValidateEventAccess(appEvent, $"RuleAction:{invocation.Name}"))
        {
            throw _boundaryEnforcer.CreateViolationException(
                $"RuleAction:{invocation.Name}",
                _tenantContext.TenantId,
                appEvent.TenantId.ToString());
        }
        
        // Log tenant context for rule action execution if multi-tenancy is enabled
        if (_multiTenancyOptions.Enabled && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Executing rule action {ActionName} for tenant {TenantId} from event {EventId}",
                invocation.Name, appEvent.TenantId, appEvent.Id);
        }
        
        await executor.Execute(ctx, invocation.Result, jsonEvent, invocation.Args, appEvent, action.Invoke.SealEvents, ruleTags);
    }

    private async Task AppendData(JObject jsonEvent, RuleExpressionInvocation invoke)
    {
        var appender = _appenders[invoke.Name];
        await appender.AddToAppendix(invoke.Result, jsonEvent, invoke.Args);
    }

    public async Task ExecuteRule(Rule rule, AppEvent @event)
    {
        using (PerfTrack.Stopwatch($"ExecuteRule: {rule.Name}"))
        {
            try
            {

                var jsonEvent = @event.ToRuleView();
                bool doActions = await EvaluateConditions(rule, jsonEvent);

                if (doActions)
                    await ExecuteActions(rule, jsonEvent, @event);
       
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information,
                    ex.TraceInformation());
            }
        }
    }


    private async Task<bool> EvaluateConditions(Rule rule, JObject jsonEvent)
    {
        bool doActions = true;
        foreach (var condition in rule.Conditions)
        {
            

            foreach (var invoke in condition.Append.EmptyIfNull())
            {
                if(_options.DebugRules)
                {
                    _logger.LogDebug("rule {Name} appending {appender}", rule.Name, invoke.Name);
                }
                await AppendData(jsonEvent, invoke);
            }

            if (_options.DebugRules)
            {
                _logger.LogDebug("rule {Name} evaluating condition: {condition} on prepped event: {event}", rule.Name, condition.Query, jsonEvent.ToString());
            }

            var jsonPathQuery = condition.Query;
            var queryResult = jsonEvent.SelectTokens(jsonPathQuery);

            var passCondition = condition.Check switch
            {
                QueryResult.Single => queryResult.Count() == 1,
                QueryResult.None => !queryResult.Any(),
                _ => queryResult.Any(),
            };

            if (condition.Negate)
                passCondition = !passCondition;

            if(_options.DebugRules)
            {
                _logger.LogDebug("rule {Name}, condition {Condition} evaluation: {PassCondition}", rule.Name, condition.Query, passCondition);
            }

            doActions = doActions && passCondition;

            if (!doActions) // short circuit eval
                break;
        }

        if (_options.DebugRules)
        {
            _logger.LogDebug("Rule {Name} conditions passed: {Conditions}", rule.Name, doActions);
        }

        return doActions;
    }

    private async Task ExecuteActions(Rule rule, JObject jsonEvent, AppEvent appEvent)
    {
        var trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

        try
        {

            foreach (var action in rule.Actions)
            {
                await ExecuteAction(rule, jsonEvent, appEvent, trx, action);

            }

            await trx.CommitAsync();
        }catch
        {
            await trx.AbortAsync();
            throw;
        }
    }

    public async Task ExecuteAction(
        Rule rule, JObject jsonEvent, AppEvent appEvent, 
        ITransactionContext trx, RuleAction action)
    {
        await ExecuteAction(rule.Name, rule.EventTags,
            jsonEvent, appEvent, trx, action);
    }

    public async Task ExecuteAction(
        string ruleName, IEnumerable<string>? ruleTags, 
        JObject jsonEvent, AppEvent appEvent,
        ITransactionContext trx, RuleAction action)
    {
        foreach (var invoke in action.AppendBefore.EmptyIfNull())
        {
            if (_options.DebugRules)
            {
                _logger.LogDebug("rule {Name} pre-appending {appender}", ruleName, invoke.Name);
            }
            await AppendData(jsonEvent, invoke);
        }

        if (_options.DebugRules)
        {
            _logger.LogDebug("rule {Name} executing action: {action} on prepped event: {event}", ruleName, action.Invoke.Name, jsonEvent.ToString());
        }
        await PerformAction(trx, appEvent, jsonEvent, action, ruleTags);

        foreach (var invoke in action.AppendAfter.EmptyIfNull())
        {
            if (_options.DebugRules)
            {
                _logger.LogDebug("rule {Name} post-appending {appender}", ruleName, invoke.Name);
            }
            await AppendData(jsonEvent, invoke);
        }

        if (_options.DebugRules)
        {
            _logger.LogDebug("rule {Name} post action {action} final event: {event}", ruleName, action.Invoke.Name, jsonEvent.ToString());
        }
    }

}
