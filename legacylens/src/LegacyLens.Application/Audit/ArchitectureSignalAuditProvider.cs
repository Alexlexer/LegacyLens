namespace LegacyLens.Application.Audit;

public sealed class ArchitectureSignalAuditProvider : IAuditProvider
{
    public string Name => "ArchitectureSignal";

    public Task<AuditProviderResult> AnalyzeAsync(
        AuditContext context,
        CancellationToken cancellationToken)
    {
        var architectureSignals = new List<ArchitectureSignal>();
        var techSignals = context.TechnologySignals;
        var roslyn = context.RoslynSummary;
        var di = context.DependencyInjectionSummary;

        var hasLegacyFramework = techSignals.Any(s =>
            s.Category == "Framework" &&
            (s.Name.Contains(".NET Framework") || s.Name.Contains("System.Web") || s.Name.Contains("Global.asax") || s.Name.Contains("App_Start")));

        if (hasLegacyFramework)
        {
            architectureSignals.Add(new ArchitectureSignal(
                "Legacy .NET Framework application",
                "Multiple signals indicate this is a legacy .NET Framework application rather than a .NET Core/.NET 5+ application.",
                string.Join("; ", techSignals.Where(s => s.Category == "Framework").Select(s => s.Name)),
                "medium"));
        }

        if (roslyn is { WorkspaceLoaded: true })
        {
            if (roslyn.InterfaceCount > 0 && roslyn.ClassCount > 0)
            {
                var ratio = (double)roslyn.InterfaceCount / roslyn.ClassCount;
                if (ratio >= 0.15)
                {
                    architectureSignals.Add(new ArchitectureSignal(
                        "Interface-driven design",
                        "The interface-to-class ratio suggests interface-driven architecture patterns.",
                        $"{roslyn.InterfaceCount} interfaces vs {roslyn.ClassCount} classes (ratio: {ratio:F2})",
                        "medium"));
                }
            }

            if (roslyn.ProjectCount > 5)
            {
                architectureSignals.Add(new ArchitectureSignal(
                    "Multi-project solution",
                    $"Solution contains {roslyn.ProjectCount} projects. Review project dependencies and layering.",
                    $"{roslyn.ProjectCount} C# projects loaded by Roslyn",
                    "high"));
            }
        }

        if (di is { RegistrationCount: > 0 })
        {
            architectureSignals.Add(new ArchitectureSignal(
                "Dependency injection container in use",
                $"{di.RegistrationCount} DI registration(s) detected.",
                "DI registrations found by static analysis",
                "high"));
        }

        return Task.FromResult(new AuditProviderResult(Name, ArchitectureSignals: architectureSignals));
    }
}
