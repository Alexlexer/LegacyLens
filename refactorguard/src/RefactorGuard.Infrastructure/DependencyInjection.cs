using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RefactorGuard.Application.DotNetAnalysis;
using RefactorGuard.Application.Git;
using RefactorGuard.Application.Review;
using RefactorGuard.Application.Search;
using RefactorGuard.Infrastructure.DotNetAnalysis;
using RefactorGuard.Infrastructure.Git;
using RefactorGuard.Infrastructure.GpuSearch;
using RefactorGuard.Infrastructure.Llm;
using RefactorGuard.Infrastructure.Persistence;
using RefactorGuard.Infrastructure.Security;

namespace RefactorGuard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRefactorGuardInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddSingleton<IRepoPathValidator>(new RepoPathValidator(
            Prefer(configuration, "LegacyLens:AllowedRoots", "RefactorGuard:AllowedRoots")
                .Get<string[]>() ?? []));
        services.AddScoped<IGitDiffService, GitDiffService>();
        services.AddScoped<IDotNetWorkspaceDiscovery, DotNetWorkspaceDiscovery>();
        services.AddScoped<RoslynWorkspaceLoader>();
        services.AddScoped<IRoslynWorkspaceLoader>(serviceProvider =>
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
        services.AddScoped<RefactorGuard.Application.Reports.IReportRepository, SqliteReportRepository>();
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
