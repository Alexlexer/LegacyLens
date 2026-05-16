using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Infrastructure.DotNetAnalysis;

public sealed class RoslynDependencyInjectionAnalyzer(
    IRoslynWorkspaceCache workspaceCache) : IRoslynDependencyInjectionAnalyzer
{
    private static readonly HashSet<string> RegistrationMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "AddSingleton", "AddScoped", "AddTransient",
        "TryAddSingleton", "TryAddScoped", "TryAddTransient",
        "AddHostedService", "AddOptions"
    };

    public async Task<DependencyInjectionAnalysisResult> AnalyzeAsync(
        string repoPath,
        CancellationToken cancellationToken)
    {
        using var lease = await workspaceCache.GetOrLoadAsync(repoPath, cancellationToken);
        var loaded = lease.Workspace;

        if (!loaded.Result.Success || loaded.Solution is null)
        {
            return new DependencyInjectionAnalysisResult(
                false,
                loaded.Result.WorkspacePath,
                loaded.Result.WorkspaceKind,
                [],
                [],
                [],
                loaded.Result.Warnings,
                loaded.Result.ErrorMessage);
        }

        var registrations = new List<ServiceRegistrationInfo>();
        var constructorDependencies = new List<ConstructorDependencyInfo>();

        foreach (var project in loaded.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
                continue;

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

                if (syntaxRoot is null || semanticModel is null || document.FilePath is null)
                    continue;

                registrations.AddRange(
                    CollectRegistrations(syntaxRoot, semanticModel, document.FilePath, project.Name));
                constructorDependencies.AddRange(
                    CollectConstructorDependencies(syntaxRoot, semanticModel, document.FilePath, project.Name));
            }
        }

        var findings = BuildFindings(registrations, constructorDependencies);

        return new DependencyInjectionAnalysisResult(
            true,
            loaded.Result.WorkspacePath,
            loaded.Result.WorkspaceKind,
            registrations,
            constructorDependencies,
            findings,
            loaded.Result.Warnings,
            null);
    }

    private static IEnumerable<ServiceRegistrationInfo> CollectRegistrations(
        SyntaxNode root,
        SemanticModel semanticModel,
        string filePath,
        string projectName)
    {
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess is null)
                continue;

            var methodName = memberAccess.Name.Identifier.Text;
            if (!RegistrationMethods.Contains(methodName))
                continue;

            var receiverType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
            if (receiverType is null || !IsServiceCollectionCall(receiverType, semanticModel, invocation))
                continue;

            var lifetime = ExtractLifetime(methodName);
            var typeArgs = invocation.Expression is MemberAccessExpressionSyntax ma
                ? (ma.Name as GenericNameSyntax)?.TypeArgumentList.Arguments
                : null;

            string? serviceType = null;
            string? implType = null;

            if (typeArgs is not null && typeArgs.Value.Count >= 1)
            {
                serviceType = semanticModel.GetTypeInfo(typeArgs.Value[0]).Type?.ToDisplayString();

                if (typeArgs.Value.Count >= 2)
                    implType = semanticModel.GetTypeInfo(typeArgs.Value[1]).Type?.ToDisplayString();
            }

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var col = invocation.GetLocation().GetLineSpan().StartLinePosition.Character + 1;

            yield return new ServiceRegistrationInfo(
                serviceType,
                implType,
                lifetime,
                filePath,
                line,
                col,
                projectName,
                invocation.ToString().Length > 120
                    ? string.Concat(invocation.ToString().AsSpan(0, 120), "...")
                    : invocation.ToString());
        }
    }

    private static IEnumerable<ConstructorDependencyInfo> CollectConstructorDependencies(
        SyntaxNode root,
        SemanticModel semanticModel,
        string filePath,
        string projectName)
    {
        foreach (var ctor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
        {
            if (ctor.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword)))
                continue;

            var containingType = (ctor.Parent as TypeDeclarationSyntax)?.Identifier.Text ?? "Unknown";
            var line = ctor.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var col = ctor.GetLocation().GetLineSpan().StartLinePosition.Character + 1;

            foreach (var param in ctor.ParameterList.Parameters)
            {
                if (param.Type is null)
                    continue;

                var paramType = semanticModel.GetTypeInfo(param.Type).Type?.ToDisplayString();
                if (paramType is null || IsValueOrPrimitiveType(paramType) || IsFrameworkType(paramType))
                    continue;

                yield return new ConstructorDependencyInfo(
                    containingType,
                    paramType,
                    filePath,
                    line,
                    col,
                    projectName);
            }
        }
    }

    private static bool IsServiceCollectionCall(
        ITypeSymbol receiverType,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation)
    {
        if (receiverType.Name == "IServiceCollection" ||
            receiverType.AllInterfaces.Any(i => i.Name == "IServiceCollection"))
            return true;

        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        if (memberAccess is null)
            return false;

        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (symbol is null || !symbol.IsExtensionMethod || symbol.Parameters.Length == 0)
            return false;

        var firstParamType = symbol.Parameters[0].Type;
        return firstParamType.Name == "IServiceCollection" ||
               firstParamType.AllInterfaces.Any(i => i.Name == "IServiceCollection");
    }

    private static IReadOnlyList<DependencyInjectionFinding> BuildFindings(
        IReadOnlyList<ServiceRegistrationInfo> registrations,
        IReadOnlyList<ConstructorDependencyInfo> constructorDeps)
    {
        var findings = new List<DependencyInjectionFinding>();

        var byServiceType = registrations
            .Where(r => r.ServiceType is not null)
            .GroupBy(r => r.ServiceType!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in byServiceType)
        {
            findings.Add(new DependencyInjectionFinding(
                "Warning",
                "multiple-registrations",
                $"'{group.Key}' is registered {group.Count()} times.",
                group.First().FilePath,
                group.First().Line,
                group.First().Column));
        }

        var singletonTypes = registrations
            .Where(r => r.Lifetime == "Singleton" && r.ImplementationType is not null)
            .Select(r => r.ImplementationType!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scopedServiceTypes = registrations
            .Where(r => r.Lifetime == "Scoped" && r.ServiceType is not null)
            .Select(r => r.ServiceType!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dep in constructorDeps)
        {
            if (!singletonTypes.Contains(dep.ContainingType))
                continue;

            if (scopedServiceTypes.Contains(dep.DependencyType))
            {
                findings.Add(new DependencyInjectionFinding(
                    "Warning",
                    "singleton-depends-on-scoped",
                    $"Singleton '{dep.ContainingType}' depends on scoped '{dep.DependencyType}'.",
                    dep.FilePath,
                    dep.Line,
                    dep.Column));
            }
        }

        foreach (var reg in registrations.Where(r => r.ServiceType is not null && !IsLikelyInterfaceType(r.ServiceType!)))
        {
            findings.Add(new DependencyInjectionFinding(
                "Info",
                "concrete-type-injection",
                $"Concrete type '{reg.ServiceType}' registered directly. Consider registering against an interface.",
                reg.FilePath,
                reg.Line,
                reg.Column));
        }

        var registeredServiceTypes = registrations
            .Where(r => r.ServiceType is not null)
            .Select(r => r.ServiceType!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dep in constructorDeps)
        {
            if (!IsLikelyInterfaceType(dep.DependencyType))
                continue;

            if (IsFrameworkType(dep.DependencyType))
                continue;

            if (!registeredServiceTypes.Contains(dep.DependencyType))
            {
                findings.Add(new DependencyInjectionFinding(
                    "Info",
                    "missing-registration-candidate",
                    $"'{dep.DependencyType}' is injected in '{dep.ContainingType}' but no matching registration was found.",
                    dep.FilePath,
                    dep.Line,
                    dep.Column));
            }
        }

        return findings;
    }

    private static string ExtractLifetime(string methodName)
    {
        if (methodName.Contains("Singleton", StringComparison.OrdinalIgnoreCase)) return "Singleton";
        if (methodName.Contains("Scoped", StringComparison.OrdinalIgnoreCase)) return "Scoped";
        if (methodName.Contains("Transient", StringComparison.OrdinalIgnoreCase)) return "Transient";
        if (methodName == "AddHostedService") return "Singleton";
        if (methodName == "AddOptions") return "Singleton";
        return "Unknown";
    }

    private static bool IsLikelyInterfaceType(string typeName)
    {
        var simpleName = typeName.Split('.').Last().Split('<').First();
        return simpleName.Length > 1 && simpleName[0] == 'I' && char.IsUpper(simpleName[1]);
    }

    private static bool IsFrameworkType(string typeName)
    {
        return typeName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
            || typeName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
            || typeName.StartsWith("ILogger", StringComparison.OrdinalIgnoreCase)
            || typeName.StartsWith("IOptions", StringComparison.OrdinalIgnoreCase)
            || typeName.StartsWith("IConfiguration", StringComparison.OrdinalIgnoreCase)
            || typeName.StartsWith("IHostEnvironment", StringComparison.OrdinalIgnoreCase)
            || typeName.StartsWith("IWebHostEnvironment", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValueOrPrimitiveType(string typeName)
    {
        return typeName is "string" or "int" or "long" or "bool" or "double" or "float"
            or "decimal" or "byte" or "char" or "object";
    }
}
