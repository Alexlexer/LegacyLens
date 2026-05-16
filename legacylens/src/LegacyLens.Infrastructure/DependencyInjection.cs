using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LegacyLens.Application.DotNetAnalysis;
using LegacyLens.Application.Git;
using LegacyLens.Application.Review;
using LegacyLens.Application.Search;
using LegacyLens.Infrastructure.DotNetAnalysis;
using LegacyLens.Infrastructure.Git;
using LegacyLens.Infrastructure.GpuSearch;
using LegacyLens.Infrastructure.Llm;
using LegacyLens.Infrastructure.Persistence;
using LegacyLens.Infrastructure.Security;

namespace LegacyLens.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLegacyLensInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddSingleton<IRepoPathValidator>(new RepoPathValidator(
            Prefer(configuration, "LegacyLens:AllowedRoots", "RefactorGuard:AllowedRoots")
                .Get<string[]>() ?? []));
        services.AddScoped<IGitDiffService, GitDiffService>();
        services.AddSingleton<IDotNetWorkspaceDiscovery, DotNetWorkspaceDiscovery>();
        services.AddSingleton<RoslynWorkspaceLoader>();
        services.AddOptions<RoslynOptions>()
            .Bind(Prefer(configuration, "LegacyLens:Roslyn", RoslynOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRoslynWorkspaceCache, RoslynWorkspaceCache>();
        services.AddSingleton<IRoslynWorkspaceLoader>(serviceProvider =>
            serviceProvider.GetRequiredService<RoslynWorkspaceLoader>());
        services.AddScoped<IRoslynSymbolScanner, RoslynSymbolScanner>();
        services.AddScoped<IRoslynReferenceAnalyzer, RoslynReferenceAnalyzer>();
        services.AddScoped<IRoslynDependencyInjectionAnalyzer, RoslynDependencyInjectionAnalyzer>();
        services.AddOptions<GpuSearchOptions>()
            .Bind(Prefer(configuration, "LegacyLens:GpuSearch", GpuSearchOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.BaseUrl.IsAbsoluteUri, "GpuSearch BaseUrl must be absolute.")
            .ValidateOnStart();
        services.AddHttpClient<IGpuSearchClient, GpuSearchClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<GpuSearchOptions>>()
                .Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddOptions<LlmProviderOptions>()
            .Bind(Prefer(configuration, "LegacyLens:Review", LlmProviderOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "Review Provider is required.")
            .ValidateOnStart();
        services.AddOptions<ReviewEnrichmentOptions>()
            .Bind(Prefer(configuration, "LegacyLens:ReviewEnrichment", ReviewEnrichmentOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(serviceProvider => serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<ReviewEnrichmentOptions>>()
            .Value);
        services.AddOptions<LmStudioOptions>()
            .Bind(Prefer(configuration, "LegacyLens:LmStudio", LmStudioOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.BaseUrl.IsAbsoluteUri, "LM Studio BaseUrl must be absolute.")
            .ValidateOnStart();
        services.AddOptions<OllamaOptions>()
            .Bind(Prefer(configuration, "LegacyLens:Ollama", OllamaOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.BaseUrl.IsAbsoluteUri, "Ollama BaseUrl must be absolute.")
            .ValidateOnStart();
        services.AddHttpClient<IOllamaModelService, OllamaModelService>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>()
                .Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(options.TimeoutSeconds, options.PullTimeoutSeconds));
        });
        services.AddKeyedSingleton<IReviewLlmProvider, DeterministicReviewLlmProvider>("deterministic");
        services.AddHttpClient<LmStudioReviewLlmProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<LmStudioOptions>>()
                .Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddKeyedScoped<IReviewLlmProvider>("lmstudio", (serviceProvider, _) =>
            serviceProvider.GetRequiredService<LmStudioReviewLlmProvider>());
        services.AddHttpClient<OllamaReviewLlmProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>()
                .Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddKeyedScoped<IReviewLlmProvider>("ollama", (serviceProvider, _) =>
            serviceProvider.GetRequiredService<OllamaReviewLlmProvider>());
        services.AddScoped<IReviewLlmProvider, ReviewLlmProviderFactory>();
        services.AddOptions<PersistenceOptions>()
            .Bind(Prefer(configuration, "LegacyLens:Persistence", PersistenceOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => !Path.IsPathRooted(options.DatabasePath), "Persistence DatabasePath must be relative.")
            .ValidateOnStart();
        services.AddScoped<LegacyLens.Application.Reports.IReportRepository, SqliteReportRepository>();
        return services;
    }

    // LegacyLens config section is preferred; RefactorGuard section is accepted for backward compatibility.
    private static IConfigurationSection Prefer(
        IConfiguration configuration,
        string preferredKey,
        string fallbackKey)
    {
        var preferred = configuration.GetSection(preferredKey);
        return preferred.Exists() ? preferred : configuration.GetSection(fallbackKey);
    }
}
