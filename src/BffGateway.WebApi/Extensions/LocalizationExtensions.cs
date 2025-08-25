using BffGateway.WebApi.Configuration;
using BffGateway.WebApi.Services;
using BffGateway.Application.Abstractions.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BffGateway.WebApi.Extensions;

/// <summary>
/// Extension methods for adding localization support
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Adds localization services to the application
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCustomLocalization(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure localization options
        services.Configure<LocalizationOptions>(
            configuration.GetSection(LocalizationOptions.SectionName));

        // Add localization services
        services.AddLocalization(options =>
        {
            var localizationOptions = configuration.GetSection(LocalizationOptions.SectionName)
                .Get<LocalizationOptions>() ?? new LocalizationOptions();
            options.ResourcesPath = localizationOptions.ResourcesPath;
        });

        // Register message service for Application layer only (single source of truth)
        services.AddSingleton<MessageService>();
        services.AddSingleton<IMessageService>(provider => provider.GetRequiredService<MessageService>());

        // Configure request localization options
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var localizationOptions = configuration.GetSection(LocalizationOptions.SectionName)
                .Get<LocalizationOptions>() ?? new LocalizationOptions();

            var supportedCultures = localizationOptions.SupportedLanguages
                .Select(c => new CultureInfo(c))
                .ToList();

            options.DefaultRequestCulture = new RequestCulture(localizationOptions.DefaultCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            // Configure request culture providers - only Accept-Language header
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                // Accept-Language header (only method)
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });

        return services;
    }

    /// <summary>
    /// Configures localization middleware in the application pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseCustomLocalization(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
        if (options?.Value != null)
        {
            app.UseRequestLocalization(options.Value);
        }

        return app;
    }
}
