using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Audit;

public sealed class RoslynAuditProvider(
    IRoslynSymbolScanner symbolScanner) : IAuditProvider
{
    public string Name => "Roslyn";

    public async Task<AuditProviderResult> AnalyzeAsync(
        AuditContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.IncludeRoslyn)
            return new AuditProviderResult(Name);

        try
        {
            var scanResponse = await symbolScanner.ScanAsync(context.RepoPath, cancellationToken);
            var loadResult = scanResponse.LoadResult;

            if (!loadResult.Success)
            {
                if (scanResponse.SymbolCount > 0)
                {
                    var fallbackClassCount = GetSymbolKindCount(scanResponse, "class");
                    var fallbackInterfaceCount = GetSymbolKindCount(scanResponse, "interface");
                    var fallbackMethodCount = GetSymbolKindCount(scanResponse, "method");
                    var fallbackWarnings = loadResult.Warnings
                        .Concat(scanResponse.Warnings)
                        .Distinct()
                        .ToList();

                    return new AuditProviderResult(
                        Name,
                        RiskFindings:
                        [
                            new AuditFinding(
                                "Info",
                                "roslyn-syntax-fallback",
                                "Roslyn workspace unavailable; syntax-only fallback used",
                                "MSBuildWorkspace could not load the solution, so LegacyLens counted C# symbols directly from source files. Reference analysis and compiler-aware facts remain unavailable.",
                                Evidence: loadResult.ErrorMessage)
                        ],
                        RoslynSummary: new AuditRoslynSummary(
                            false,
                            loadResult.WorkspacePath,
                            loadResult.WorkspaceKind,
                            loadResult.ProjectCount,
                            scanResponse.DocumentCount,
                            scanResponse.SymbolCount,
                            fallbackClassCount,
                            fallbackInterfaceCount,
                            fallbackMethodCount,
                            fallbackWarnings,
                            loadResult.ErrorMessage));
                }

                return new AuditProviderResult(
                    Name,
                    RiskFindings:
                    [
                        new AuditFinding(
                            "Info",
                            "roslyn-unavailable",
                            "Roslyn workspace could not be loaded",
                            loadResult.ErrorMessage ?? "Workspace load failed. File-based signals were still collected.",
                            Evidence: loadResult.ErrorMessage)
                    ],
                    RoslynSummary: new AuditRoslynSummary(
                        false,
                        loadResult.WorkspacePath,
                        loadResult.WorkspaceKind,
                        0, 0, 0, 0, 0, 0,
                        loadResult.Warnings,
                        loadResult.ErrorMessage));
            }

            var classCount = GetSymbolKindCount(scanResponse, "class");
            var interfaceCount = GetSymbolKindCount(scanResponse, "interface");
            var methodCount = GetSymbolKindCount(scanResponse, "method");

            return new AuditProviderResult(
                Name,
                RoslynSummary: new AuditRoslynSummary(
                    true,
                    loadResult.WorkspacePath,
                    loadResult.WorkspaceKind,
                    loadResult.ProjectCount,
                    loadResult.DocumentCount,
                    scanResponse.Symbols.Count,
                    classCount,
                    interfaceCount,
                    methodCount,
                    loadResult.Warnings,
                    null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AuditProviderResult(
                Name,
                RiskFindings:
                [
                    new AuditFinding(
                        "Info",
                        "roslyn-unavailable",
                        "Roslyn analysis failed",
                        ex.Message,
                        Evidence: ex.Message)
                ],
                RoslynSummary: new AuditRoslynSummary(
                    false, null, null, 0, 0, 0, 0, 0, 0, [], ex.Message));
        }
    }

    private static int GetSymbolKindCount(DotNetWorkspaceScanResponse scanResponse, string kind)
    {
        if (scanResponse.SymbolKindCounts.TryGetValue(kind, out var count))
            return count;

        return scanResponse.Symbols.Count(s => string.Equals(s.Kind, kind, StringComparison.OrdinalIgnoreCase));
    }
}
