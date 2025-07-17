using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Rules.EventAppenders;
using Microsoft.Extensions.DependencyInjection;

namespace BFormDomain.CommonCode.Platform.Rules;

public static class RuleServiceCollectionExtensions
{
    public static void AddStandardRuleElements(this IServiceCollection that)
    {
        that.AddSingleton<IEventAppender, ComputedDateTimeAppender>();
        that.AddSingleton<IEventAppender, CurrentDateTimeAppender>();

        // that.AddSingleton<IRuleActionEvaluator, SomeAction>();

        that.AddSingleton<RuleEvaluator>();
        
        // Register tenant-aware content factory
        that.AddSingleton<ITenantContentRepositoryFactory, TenantContentRepositoryFactory>();
        
        // Register RuleEngine with tenant-aware content support
        that.AddSingleton<IAppEventConsumer, TenantAwareRuleEngine>();
        
        
    }
}
