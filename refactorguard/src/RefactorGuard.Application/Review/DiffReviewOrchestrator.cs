using RefactorGuard.Application.Git;
using RefactorGuard.Application.Reports;
using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Review;

public sealed class DiffReviewOrchestrator(
    IGitDiffService gitDiffService,
    IReviewReportFormatter reportFormatter,
    IReviewPromptBuilder promptBuilder,
    IReviewLlmProvider llmProvider,
    IReportRepository reportRepository) : IReviewOrchestrator
{
    public async Task<DiffReviewReport> ReviewDiffAsync(
        DiffReviewRequest request,
        CancellationToken cancellationToken)
    {
        var diff = await gitDiffService.GetCurrentDiffAsync(
            new GitDiffPreviewRequest(request.RepoPath),
            cancellationToken);

        var findings = BuildFindings(diff);
        var llmSummary = request.UseLlm
            ? await llmProvider.GenerateReviewAsync(promptBuilder.Build(diff, findings), cancellationToken)
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
            request.UseLlm ? llmProvider.Name : "Deterministic");

        var finalReport = report with
        {
            Markdown = reportFormatter.Format(report)
        };
        await reportRepository.SaveAsync(finalReport, cancellationToken);
        return finalReport;
    }

    private static IReadOnlyList<ReviewFinding> BuildFindings(GitDiffPreviewResponse diff)
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
