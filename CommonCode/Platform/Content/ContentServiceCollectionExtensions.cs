using BFormDomain.CommonCode.Platform.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace BFormDomain.CommonCode.Platform.Content;

public static class ContentServiceCollectionExtensions
{
    /// <summary>
    /// Adds the application platform content services with multi-tenancy support.
    /// </summary>
    public static IServiceCollection AddApplicationPlatformContent(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure content options
        services.Configure<FileApplicationPlatformContentOptions>(
            configuration.GetSection("ApplicationPlatformContent"));

        // Check if multi-tenancy is enabled
        var multiTenancyOptions = configuration.GetSection("MultiTenancy").Get<MultiTenancyOptions>() 
            ?? new MultiTenancyOptions();

        if (multiTenancyOptions.Enabled)
        {
            // Register tenant-aware content implementation
            services.AddSingleton<IApplicationPlatformContent, TenantAwareApplicationPlatformContent>();
            
            // Add hosted service for preloading tenant content (optional)
            services.AddHostedService<TenantContentPreloadService>();
        }
        else
        {
            // Register standard file-based content implementation
            services.AddSingleton<IApplicationPlatformContent, FileApplicationPlatformContent>();
        }

        return services;
    }

    /// <summary>
    /// Adds the application platform content services with explicit multi-tenancy setting.
    /// </summary>
    public static IServiceCollection AddApplicationPlatformContent(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableMultiTenancy)
    {
        // Configure content options
        services.Configure<FileApplicationPlatformContentOptions>(
            configuration.GetSection("ApplicationPlatformContent"));

        if (enableMultiTenancy)
        {
            // Register tenant-aware content implementation
            services.AddSingleton<IApplicationPlatformContent, TenantAwareApplicationPlatformContent>();
            
            // Add hosted service for preloading tenant content (optional)
            services.AddHostedService<TenantContentPreloadService>();
        }
        else
        {
            // Register standard file-based content implementation
            services.AddSingleton<IApplicationPlatformContent, FileApplicationPlatformContent>();
        }

        return services;
    }
}