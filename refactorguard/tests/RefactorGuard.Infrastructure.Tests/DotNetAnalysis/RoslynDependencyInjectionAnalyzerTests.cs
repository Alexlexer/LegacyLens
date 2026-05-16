using RefactorGuard.Infrastructure.DotNetAnalysis;

namespace RefactorGuard.Infrastructure.Tests.DotNetAnalysis;

public sealed class RoslynDependencyInjectionAnalyzerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"legacylens-di-{Guid.NewGuid():N}");

    public RoslynDependencyInjectionAnalyzerTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsAddScopedRegistration()
    {
        WriteProject(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Sample;

            public interface IUserService { }
            public class UserService : IUserService { }

            public static class ServiceExtensions
            {
                public static void Register(IServiceCollection services)
                {
                    services.AddScoped<IUserService, UserService>();
                }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.Registrations, r =>
            r.Lifetime == "Scoped" &&
            r.ServiceType != null && r.ServiceType.Contains("IUserService") &&
            r.ImplementationType != null && r.ImplementationType.Contains("UserService"));
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsAddSingletonRegistration()
    {
        WriteProject(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Sample;

            public interface ICache { }
            public class MemoryCache : ICache { }

            public static class Setup
            {
                public static void Register(IServiceCollection services)
                {
                    services.AddSingleton<ICache, MemoryCache>();
                }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.Registrations, r =>
            r.Lifetime == "Singleton" &&
            r.ServiceType != null && r.ServiceType.Contains("ICache"));
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsAddTransientRegistration()
    {
        WriteProject(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Sample;

            public interface IEmailSender { }
            public class SmtpEmailSender : IEmailSender { }

            public static class Setup
            {
                public static void Register(IServiceCollection services)
                {
                    services.AddTransient<IEmailSender, SmtpEmailSender>();
                }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.Registrations, r =>
            r.Lifetime == "Transient" &&
            r.ServiceType != null && r.ServiceType.Contains("IEmailSender"));
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsBuilderServicesRegistration()
    {
        WriteProject(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Sample;

            public interface IOrderService { }
            public class OrderService : IOrderService { }

            public static class Startup
            {
                public static void Configure(Microsoft.AspNetCore.Builder.WebApplicationBuilder builder)
                {
                    builder.Services.AddScoped<IOrderService, OrderService>();
                }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.Registrations, r =>
            r.Lifetime == "Scoped" &&
            r.ServiceType != null && r.ServiceType.Contains("IOrderService"));
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsConstructorDependencies()
    {
        WriteProject(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Sample;

            public interface IUserRepository { }

            public class UserService
            {
                public UserService(IUserRepository repository) { }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.ConstructorDependencies, d =>
            d.ContainingType.Contains("UserService") &&
            d.DependencyType.Contains("IUserRepository"));
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsMultipleRegistrations()
    {
        WriteProject(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Sample;

            public interface IUserService { }
            public class UserServiceV1 : IUserService { }
            public class UserServiceV2 : IUserService { }

            public static class Setup
            {
                public static void Register(IServiceCollection services)
                {
                    services.AddScoped<IUserService, UserServiceV1>();
                    services.AddScoped<IUserService, UserServiceV2>();
                }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.Findings, f => f.Code == "multiple-registrations");
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsSingletonDependsOnScoped()
    {
        WriteProject(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Sample;

            public interface IScopedRepo { }
            public class ScopedRepo : IScopedRepo { }
            public interface ISingletonCache { }

            public class SingletonCache : ISingletonCache
            {
                public SingletonCache(IScopedRepo repo) { }
            }

            public static class Setup
            {
                public static void Register(IServiceCollection services)
                {
                    services.AddScoped<IScopedRepo, ScopedRepo>();
                    services.AddSingleton<ISingletonCache, SingletonCache>();
                }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.Findings, f => f.Code == "singleton-depends-on-scoped");
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsConcreteTypeInjection()
    {
        WriteProject(
            """
            namespace Sample;

            public class OrderRepository { }

            public class OrderService
            {
                public OrderService(OrderRepository repository) { }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.Findings, f =>
            f.Code == "concrete-type-injection" &&
            f.Message.Contains("OrderRepository"));
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsMissingRegistrationCandidate()
    {
        WriteProject(
            """
            namespace Sample;

            public interface IPaymentGateway { }

            public class PaymentService
            {
                public PaymentService(IPaymentGateway gateway) { }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.Findings, f =>
            f.Code == "missing-registration-candidate" &&
            f.Message.Contains("IPaymentGateway"));
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotFlagMissingRegistration_WhenTypeIsRegistered()
    {
        WriteProject(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Sample;

            public interface IUserRepository { }
            public class UserRepository : IUserRepository { }

            public class UserService
            {
                public UserService(IUserRepository repository) { }
            }

            public static class Setup
            {
                public static void Register(IServiceCollection services)
                {
                    services.AddScoped<IUserRepository, UserRepository>();
                }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.DoesNotContain(result.Findings, f =>
            f.Code == "missing-registration-candidate" &&
            f.Message.Contains("IUserRepository"));
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotFlagFrameworkTypesAsConcreteInjection()
    {
        WriteProject(
            """
            using Microsoft.Extensions.Logging;
            using Microsoft.Extensions.Options;

            namespace Sample;

            public class MyService
            {
                public MyService(ILogger<MyService> logger, IOptions<MyOptions> options) { }
            }

            public class MyOptions { }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.DoesNotContain(result.Findings, f =>
            f.Code == "concrete-type-injection" &&
            (f.Message.Contains("ILogger") || f.Message.Contains("IOptions")));
    }

    [Fact]
    public async Task AnalyzeAsync_HandlesNoSolutionGracefully()
    {
        // Empty directory — no .sln or .csproj
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Empty(result.Registrations);
        Assert.Empty(result.ConstructorDependencies);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsTryAddScopedRegistration()
    {
        WriteProject(
            """
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.DependencyInjection.Extensions;

            namespace Sample;

            public interface INotificationService { }
            public class NotificationService : INotificationService { }

            public static class Setup
            {
                public static void Register(IServiceCollection services)
                {
                    services.TryAddScoped<INotificationService, NotificationService>();
                }
            }
            """);
        var result = await CreateAnalyzer().AnalyzeAsync(_root, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(result.Registrations, r =>
            r.Lifetime == "Scoped" &&
            r.ServiceType != null &&
            r.ServiceType.Contains("INotificationService"));
    }

    private RoslynDependencyInjectionAnalyzer CreateAnalyzer()
        => new(new DotNetWorkspaceDiscovery(), new RoslynWorkspaceLoader());

    private void WriteProject(string code)
    {
        File.WriteAllText(
            Path.Combine(_root, "Sample.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(Path.Combine(_root, "Code.cs"), code);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
