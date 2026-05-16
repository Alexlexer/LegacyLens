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

            var classCount = scanResponse.Symbols.Count(s => s.Kind == "class");
            var interfaceCount = scanResponse.Symbols.Count(s => s.Kind == "interface");
            var methodCount = scanResponse.Symbols.Count(s => s.Kind == "method");

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
}
