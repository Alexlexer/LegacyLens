using RefactorGuard.Application.Git;
using RefactorGuard.Application.Reports;
using RefactorGuard.Application.Search;
using RefactorGuard.Domain.Git;
using RefactorGuard.Domain.Search;

namespace RefactorGuard.Application.Review;

public sealed class DiffReviewOrchestrator(
    IGitDiffService gitDiffService,
    IGpuSearchClient gpuSearchClient,
    IReviewReportFormatter reportFormatter,
    IReviewPromptBuilder promptBuilder,
    IReviewLlmProvider llmProvider,
    IReportRepository reportRepository,
    ReviewEnrichmentOptions enrichmentOptions) : IReviewOrchestrator
{
    public async Task<DiffReviewReport> ReviewDiffAsync(
        DiffReviewRequest request,
        CancellationToken cancellationToken)
    {
        var diff = await gitDiffService.GetCurrentDiffAsync(
            new GitDiffPreviewRequest(request.RepoPath),
            cancellationToken);

        var findings = new List<ReviewFinding>(BuildDeterministicFindings(diff));
        var gpuSearchContext = await TryEnrichWithGpuSearchAsync(diff, findings, cancellationToken);

        var llmSummary = request.UseLlm
            ? await llmProvider.GenerateReviewAsync(
                promptBuilder.Build(diff, findings, gpuSearchContext),
                cancellationToken)
            : null;

        var report = new DiffReviewReport(
            Guid.NewGuid().ToString("N"),
            diff.RepoPath,
            DateTimeOffset.UtcNow,
            diff.ChangedFileCount,
            diff.Files,
            findings,
            string.Empty,
            llmSummary,
            request.UseLlm ? llmProvider.Name : "Deterministic",
            gpuSearchContext);

        var finalReport = report with { Markdown = reportFormatter.Format(report) };
        await reportRepository.SaveAsync(finalReport, cancellationToken);
        return finalReport;
    }

    private async Task<GpuSearchReviewContext> TryEnrichWithGpuSearchAsync(
        GitDiffPreviewResponse diff,
        List<ReviewFinding> findings,
        CancellationToken cancellationToken)
    {
        try
        {
            await gpuSearchClient.GetHealthAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            findings.Add(new ReviewFinding(
                "gpu-search-unavailable",
                "Info",
                null,
                "gpu-search-mcp unavailable",
                "gpu-search-mcp was not reachable. Review enrichment was skipped. Deterministic review still completed."));
            return new GpuSearchReviewContext(false, [], ex.Message);
        }

        var fileContexts = new List<ChangedFileContext>();
        foreach (var file in diff.Files.Take(enrichmentOptions.MaxFilesToEnrich))
        {
            var ctx = await TryGetFileContextAsync(file.Path, cancellationToken);
            fileContexts.Add(ctx);

            if (ctx.DependencyImpact is { TotalImpacted: >= 3 })
            {
                var severity = ctx.DependencyImpact.TotalImpacted >= 10 ? "High" : "Medium";
                var importerList = ctx.DependencyImpact.DirectImporters.Count > 0
                    ? $" Direct importers: {string.Join(", ", ctx.DependencyImpact.DirectImporters.Take(5))}."
                    : string.Empty;
                findings.Add(new ReviewFinding(
                    "high-impact-change",
                    severity,
                    file.Path,
                    $"High-impact change ({ctx.DependencyImpact.TotalImpacted} impacted files)",
                    $"This file is imported by {ctx.DependencyImpact.TotalImpacted} other file(s).{importerList}"));
            }
        }

        return new GpuSearchReviewContext(true, fileContexts);
    }

    private async Task<ChangedFileContext> TryGetFileContextAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        DependencyImpactSummary? impact = null;
        SkeletonSummary? skeleton = null;
        var relatedResults = new List<RelatedCodeResult>();
        string? error = null;

        try
        {
            var impactResponse = await gpuSearchClient.GetDependencyImpactAsync(
                new DependencyImpactRequest(filePath),
                cancellationToken);

            var directImporters = impactResponse.ImpactedFiles
                .Where(f => f.Hops == 1)
                .Select(f => f.File)
                .ToList();
            var impactedFiles = impactResponse.ImpactedFiles
                .Select(f => new DependencyImpactedFile(f.File, f.Hops, f.Reason))
                .ToList();
            impact = new DependencyImpactSummary(
                impactResponse.ImpactedFiles.Count,
                directImporters,
                impactResponse.Confidence,
                impactResponse.AnalysisMode,
                impactResponse.Limitations,
                impactResponse.Warnings,
                impactedFiles);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            error = $"Dependency impact unavailable: {ex.Message}";
        }

        try
        {
            var skeletonResponse = await gpuSearchClient.ReadSkeletonAsync(
                new ReadSkeletonRequest(filePath),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(skeletonResponse.Content))
            {
                var content = skeletonResponse.Content.Length > enrichmentOptions.MaxSkeletonLength
                    ? string.Concat(skeletonResponse.Content.AsSpan(0, enrichmentOptions.MaxSkeletonLength), "\n[skeleton truncated]")
                    : skeletonResponse.Content;
                skeleton = new SkeletonSummary(content, skeletonResponse.Language);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            error ??= $"Skeleton read unavailable: {ex.Message}";
        }

        try
        {
            var query = Path.GetFileNameWithoutExtension(filePath);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var searchResults = await gpuSearchClient.SearchHybridAsync(
                    new SearchHybridRequest(query, null, enrichmentOptions.MaxSearchResultsPerFile),
                    cancellationToken);

                relatedResults.AddRange(searchResults.Select(r =>
                    new RelatedCodeResult(
                        r.File,
                        r.LineStart,
                        r.LineEnd,
                        Truncate(r.Snippet, enrichmentOptions.MaxRelatedResultSnippetLength, "\n[snippet truncated]"),
                        r.Engine,
                        r.Score)));
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            error ??= $"Search unavailable: {ex.Message}";
        }

        return new ChangedFileContext(filePath, impact, skeleton, relatedResults, error);
    }

    private static string? Truncate(string? value, int maxLength, string suffix)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return string.Concat(value.AsSpan(0, maxLength), suffix);
    }

    private static List<ReviewFinding> BuildDeterministicFindings(GitDiffPreviewResponse diff)
    {
        var findings = new List<ReviewFinding>();

        foreach (var file in diff.Files)
        {
            if (file.Additions + file.Deletions >= 400)
            {
                findings.Add(new ReviewFinding(
                    "large-change",
                    "Medium",
                    file.Path,
                    "Large file change",
                    "This file has a large diff. Consider splitting the change or reviewing it carefully."));
            }

            if (IsProjectOrConfigFile(file.Path))
            {
                findings.Add(new ReviewFinding(
                    "project-or-config-change",
                    "Medium",
                    file.Path,
                    "Project or configuration changed",
                    "Project and configuration changes can affect build, runtime, or deployment behavior."));
            }

            if (IsTestFile(file.Path))
            {
                findings.Add(new ReviewFinding(
                    "test-change",
                    "Info",
                    file.Path,
                    "Test file changed",
                    "Verify the updated tests cover the intended behavior and still fail without the implementation."));
            }
        }

        if (diff.ChangedFileCount == 0)
        {
            findings.Add(new ReviewFinding(
                "empty-diff",
                "Info",
                null,
                "No working-tree diff",
                "There are no current working-tree changes to review."));
        }

        return findings;
    }

    private static bool IsProjectOrConfigFile(string path)
    {
        return path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTestFile(string path)
    {
        return path.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("\\tests\\", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase);
    }
}
