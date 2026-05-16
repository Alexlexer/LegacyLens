using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RefactorGuard.Application.DotNetAnalysis;

namespace RefactorGuard.Infrastructure.DotNetAnalysis;

public sealed class RoslynDependencyInjectionAnalyzer(
    IDotNetWorkspaceDiscovery discovery,
    RoslynWorkspaceLoader workspaceLoader) : IRoslynDependencyInjectionAnalyzer
{
    // Method names that represent DI registrations on IServiceCollection
    private static readonly HashSet<string> RegistrationMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "AddSingleton", "AddScoped", "AddTransient",
        "TryAddSingleton", "TryAddScoped", "TryAddTransient",
        "AddHostedService", "AddOptions",
    };

    private static readonly Dictionary<string, string> LifetimeByMethod = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AddSingleton"] = "Singleton",
        ["TryAddSingleton"] = "Singleton",
        ["AddScoped"] = "Scoped",
        ["TryAddScoped"] = "Scoped",
        ["AddTransient"] = "Transient",
        ["TryAddTransient"] = "Transient",
        ["AddHostedService"] = "Singleton",
        ["AddOptions"] = "Singleton",
    };

    public async Task<DependencyInjectionAnalysisResult> AnalyzeAsync(
        string repoPath,
        CancellationToken cancellationToken)
    {
        var discoveryResult = await discovery.DiscoverAsync(repoPath, cancellationToken);
        var loaded = await workspaceLoader.LoadWorkspaceForScanAsync(discoveryResult, cancellationToken);
        using (loaded)
        {
            if (!loaded.Result.Success)
            {
                return new DependencyInjectionAnalysisResult(
                    false,
                    loaded.Result.WorkspacePath,
                    loaded.Result.WorkspaceKind,
                    [], [], [],
                    loaded.Result.Warnings,
                    loaded.Result.ErrorMessage);
            }

            var registrations = new List<ServiceRegistrationInfo>();
            var constructorDeps = new List<ConstructorDependencyInfo>();
            var warnings = loaded.Result.Warnings
                .Concat(loaded.Result.Diagnostics)
                .Distinct()
                .ToList();

            foreach (var project in loaded.Projects)
            {
                foreach (var document in project.Documents.Where(d => d.SourceCodeKind == SourceCodeKind.Regular))
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    var model = await document.GetSemanticModelAsync(cancellationToken);
                    if (root is null || model is null)
                        continue;

                    var filePath = document.FilePath ?? document.Name;
                    CollectRegistrations(root, model, filePath, project.Name, registrations);
                    CollectConstructorDependencies(root, model, filePath, project.Name, constructorDeps);
                }
            }

            var findings = BuildFindings(registrations, constructorDeps);

            return new DependencyInjectionAnalysisResult(
                true,
                loaded.Result.WorkspacePath,
                loaded.Result.WorkspaceKind,
                registrations,
                constructorDeps,
                findings,
                warnings,
                null);
        }
    }

    private static void CollectRegistrations(
        SyntaxNode root,
        SemanticModel model,
        string filePath,
        string projectName,
        List<ServiceRegistrationInfo> registrations)
    {
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var methodName = GetMethodName(invocation);
            if (methodName is null || !RegistrationMethods.Contains(methodName))
                continue;

            // Verify the receiver is an IServiceCollection-compatible type
            if (!IsServiceCollectionCall(invocation, model))
                continue;

            var lifetime = LifetimeByMethod.GetValueOrDefault(methodName, "Unknown");
            var (serviceType, implType) = ExtractTypeArguments(invocation, model);
            var expression = invocation.ToString();
            if (expression.Length > 120)
                expression = string.Concat(expression.AsSpan(0, 120), "…");

            var span = invocation.SyntaxTree.GetLineSpan(invocation.Span);
            registrations.Add(new ServiceRegistrationInfo(
                serviceType,
                implType,
                lifetime,
                filePath,
                span.StartLinePosition.Line + 1,
                span.StartLinePosition.Character + 1,
                projectName,
                expression));
        }
    }

    private static void CollectConstructorDependencies(
        SyntaxNode root,
        SemanticModel model,
        string filePath,
        string projectName,
        List<ConstructorDependencyInfo> deps)
    {
        foreach (var ctor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
        {
            var ctorSymbol = model.GetDeclaredSymbol(ctor);
            if (ctorSymbol is null)
                continue;

            // Skip private/protected constructors and static ctors
            if (ctorSymbol.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected)
                continue;

            var containingType = ctorSymbol.ContainingType.ToDisplayString(
                SymbolDisplayFormat.CSharpErrorMessageFormat);

            foreach (var param in ctor.ParameterList.Parameters)
            {
                var paramSymbol = model.GetDeclaredSymbol(param);
                if (paramSymbol?.Type is null)
                    continue;

                var depType = paramSymbol.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                var span = param.SyntaxTree.GetLineSpan(param.Span);
                deps.Add(new ConstructorDependencyInfo(
                    containingType,
                    depType,
                    filePath,
                    span.StartLinePosition.Line + 1,
                    span.StartLinePosition.Character + 1,
                    projectName));
            }
        }
    }

    private static bool IsServiceCollectionCall(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        if (memberAccess is null)
            return false;

        var receiverType = model.GetTypeInfo(memberAccess.Expression).Type;
        if (receiverType is null)
            return false;

        // Accept IServiceCollection, IServiceCollection implementations, and extension method receivers
        return IsServiceCollectionType(receiverType)
            || IsServiceCollectionMethodSymbol(invocation, model);
    }

    private static bool IsServiceCollectionType(ITypeSymbol type)
    {
        if (type.Name is "IServiceCollection" or "ServiceCollection")
            return true;

        // Check interfaces
        return type.AllInterfaces.Any(i => i.Name == "IServiceCollection");
    }

    private static bool IsServiceCollectionMethodSymbol(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (symbol is null)
            return false;

        // Extension methods: first parameter is IServiceCollection
        if (symbol.IsExtensionMethod && symbol.Parameters.Length > 0)
        {
            var firstParam = symbol.Parameters[0].Type;
            return IsServiceCollectionType(firstParam);
        }

        return false;
    }

    private static (string? serviceType, string? implType) ExtractTypeArguments(
        InvocationExpressionSyntax invocation,
        SemanticModel model)
    {
        var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (symbol is null)
            return (null, null);

        // Generic type arguments on the method call
        if (symbol.TypeArguments.Length >= 2)
        {
            var svc = symbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            var impl = symbol.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            return (svc, impl);
        }

        if (symbol.TypeArguments.Length == 1)
        {
            var svc = symbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            return (svc, null);
        }

        // Try to get runtime type arguments from syntax
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var genericName = memberAccess.Name as GenericNameSyntax;
            if (genericName?.TypeArgumentList.Arguments.Count >= 1)
            {
                var args = genericName.TypeArgumentList.Arguments;
                var svc = args[0].ToString();
                var impl = args.Count >= 2 ? args[1].ToString() : null;
                return (svc, impl);
            }
        }

        return (null, null);
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax m when m.Name is SimpleNameSyntax s => s.Identifier.ValueText,
            MemberAccessExpressionSyntax m when m.Name is GenericNameSyntax g => g.Identifier.ValueText,
            _ => null
        };
    }

    private static IReadOnlyList<DependencyInjectionFinding> BuildFindings(
        IReadOnlyList<ServiceRegistrationInfo> registrations,
        IReadOnlyList<ConstructorDependencyInfo> constructorDeps)
    {
        var findings = new List<DependencyInjectionFinding>();

        // multiple-registrations: same service type registered more than once
        var byService = registrations
            .Where(r => r.ServiceType is not null)
            .GroupBy(r => r.ServiceType!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in byService)
        {
            findings.Add(new DependencyInjectionFinding(
                "Warning",
                "multiple-registrations",
                $"'{group.Key}' is registered {group.Count()} times.",
                group.First().FilePath,
                group.First().Line,
                group.First().Column));
        }

        // singleton-depends-on-scoped: singleton impl has constructor dep on a scoped service
        var singletonImpls = registrations
            .Where(r => r.Lifetime == "Singleton" && r.ImplementationType is not null)
            .Select(r => r.ImplementationType!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scopedServices = registrations
            .Where(r => r.Lifetime == "Scoped" && r.ServiceType is not null)
            .Select(r => r.ServiceType!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dep in constructorDeps)
        {
            if (!IsSingletonImplementation(dep.ContainingType, singletonImpls))
                continue;

            if (!scopedServices.Contains(dep.DependencyType))
                continue;

            findings.Add(new DependencyInjectionFinding(
                "Warning",
                "singleton-depends-on-scoped",
                $"'{dep.ContainingType}' is registered as Singleton but depends on '{dep.DependencyType}' which is Scoped.",
                dep.FilePath,
                dep.Line,
                dep.Column));
        }

        // concrete-type-injection: constructor param is a concrete class (not interface/abstract)
        foreach (var dep in constructorDeps)
        {
            // Simple heuristic: interfaces start with 'I' followed by uppercase, or are known abstractions
            if (!IsLikelyConcreteType(dep.DependencyType))
                continue;

            findings.Add(new DependencyInjectionFinding(
                "Info",
                "concrete-type-injection",
                $"'{dep.ContainingType}' depends on concrete type '{dep.DependencyType}'. Consider depending on an abstraction.",
                dep.FilePath,
                dep.Line,
                dep.Column));
        }

        // missing-registration-candidate: constructor dep type has no matching registration
        var registeredTypes = registrations
            .SelectMany(r => new[] { r.ServiceType, r.ImplementationType })
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dep in constructorDeps)
        {
            if (IsFrameworkType(dep.DependencyType))
                continue;

            if (registeredTypes.Contains(dep.DependencyType))
                continue;

            // Only report for interface deps (concrete type injection is already flagged separately)
            if (!IsLikelyInterface(dep.DependencyType))
                continue;

            findings.Add(new DependencyInjectionFinding(
                "Info",
                "missing-registration-candidate",
                $"'{dep.DependencyType}' injected into '{dep.ContainingType}' has no matching registration found in this analysis.",
                dep.FilePath,
                dep.Line,
                dep.Column));
        }

        return findings;
    }

    private static bool IsSingletonImplementation(string containingType, HashSet<string> singletonImpls)
    {
        // Check both fully-qualified and simple name
        if (singletonImpls.Contains(containingType))
            return true;

        var simpleName = containingType.Contains('.')
            ? containingType[(containingType.LastIndexOf('.') + 1)..]
            : containingType;
        return singletonImpls.Any(s =>
        {
            var sSimple = s.Contains('.') ? s[(s.LastIndexOf('.') + 1)..] : s;
            return string.Equals(sSimple, simpleName, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool IsLikelyConcreteType(string typeName)
    {
        var simple = typeName.Contains('.') ? typeName[(typeName.LastIndexOf('.') + 1)..] : typeName;
        // Not an interface pattern (I + uppercase)
        if (simple.Length >= 2 && simple[0] == 'I' && char.IsUpper(simple[1]))
            return false;

        // Not an abstract base class pattern (Abstract + name)
        if (simple.StartsWith("Abstract", StringComparison.Ordinal))
            return false;

        // Not a framework type
        if (IsFrameworkType(typeName))
            return false;

        // Not a primitive or common value type
        if (IsValueOrPrimitiveType(typeName))
            return false;

        return true;
    }

    private static bool IsLikelyInterface(string typeName)
    {
        var simple = typeName.Contains('.') ? typeName[(typeName.LastIndexOf('.') + 1)..] : typeName;
        return simple.Length >= 2 && simple[0] == 'I' && char.IsUpper(simple[1]);
    }

    private static bool IsFrameworkType(string typeName)
    {
        return typeName.StartsWith("Microsoft.", StringComparison.Ordinal)
            || typeName.StartsWith("System.", StringComparison.Ordinal)
            || typeName is "string" or "int" or "bool" or "object"
            || typeName.StartsWith("ILogger", StringComparison.Ordinal)
            || typeName.StartsWith("IOptions", StringComparison.Ordinal)
            || typeName.StartsWith("IConfiguration", StringComparison.Ordinal)
            || typeName.StartsWith("IServiceProvider", StringComparison.Ordinal)
            || typeName.StartsWith("IHostEnvironment", StringComparison.Ordinal)
            || typeName.StartsWith("IWebHostEnvironment", StringComparison.Ordinal);
    }

    private static bool IsValueOrPrimitiveType(string typeName)
    {
        return typeName is "string" or "int" or "long" or "bool" or "double"
            or "float" or "decimal" or "byte" or "char" or "Guid"
            or "DateTime" or "DateTimeOffset" or "TimeSpan" or "Uri"
            or "CancellationToken";
    }
}
