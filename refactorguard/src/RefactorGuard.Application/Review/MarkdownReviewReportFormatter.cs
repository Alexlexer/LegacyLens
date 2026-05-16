using System.Text;

namespace RefactorGuard.Application.Review;

public sealed class MarkdownReviewReportFormatter : IReviewReportFormatter
{
    public string Format(DiffReviewReport report)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine("# LegacyLens Diff Review");
        markdown.AppendLine();
        markdown.AppendLine($"- Report ID: `{report.ReportId}`");
        markdown.AppendLine($"- Repository: `{report.RepoPath}`");
        markdown.AppendLine($"- Generated UTC: `{report.GeneratedAtUtc:O}`");
        markdown.AppendLine($"- Changed files: `{report.ChangedFileCount}`");
        markdown.AppendLine();
        markdown.AppendLine("## Changed Files");
        markdown.AppendLine();

        if (report.Files.Count == 0)
        {
            markdown.AppendLine("No changed files detected.");
        }
        else
        {
            foreach (var file in report.Files)
            {
                markdown.AppendLine($"- `{file.Path}` ({file.Status}, +{file.Additions}/-{file.Deletions})");
            }
        }

        markdown.AppendLine();
        markdown.AppendLine("## Findings");
        markdown.AppendLine();

        foreach (var finding in report.Findings)
        {
            var path = finding.Path is null ? "repository" : $"`{finding.Path}`";
            markdown.AppendLine($"- **{finding.Severity}** `{finding.RuleId}` on {path}: {finding.Title}. {finding.Description}");
        }

        AppendGpuSearchSection(markdown, report.GpuSearchContext);

        if (!string.IsNullOrWhiteSpace(report.LlmSummary))
        {
            markdown.AppendLine();
            markdown.AppendLine($"## LLM Summary ({report.LlmProvider})");
            markdown.AppendLine();
            markdown.AppendLine(report.LlmSummary);
        }

        return markdown.ToString();
    }

    private static void AppendGpuSearchSection(StringBuilder markdown, GpuSearchReviewContext? context)
    {
        if (context is null)
            return;

        markdown.AppendLine();
        markdown.AppendLine("## gpu-search Context");
        markdown.AppendLine();

        if (!context.WasAvailable)
        {
            markdown.AppendLine("gpu-search-mcp was unavailable or returned errors. Deterministic review still completed.");
            if (!string.IsNullOrWhiteSpace(context.UnavailableReason))
            {
                markdown.AppendLine();
                markdown.AppendLine($"> {context.UnavailableReason}");
            }
            return;
        }

        if (context.Files.Count == 0)
        {
            markdown.AppendLine("No files were enriched.");
            return;
        }

        foreach (var fileCtx in context.Files)
        {
            markdown.AppendLine($"### `{fileCtx.FilePath}`");
            markdown.AppendLine();

            if (fileCtx.DependencyImpact is not null)
            {
                markdown.AppendLine("**Dependency impact:**");
                markdown.AppendLine();

                var di = fileCtx.DependencyImpact;
                markdown.AppendLine($"- Impacted files: {di.TotalImpacted}");

                if (di.Confidence is not null)
                    markdown.AppendLine($"- Confidence: {di.Confidence}");

                if (di.AnalysisMode is not null)
                    markdown.AppendLine($"- Analysis mode: {di.AnalysisMode}");

                var impactedFiles = di.ImpactedFiles ?? di.DirectImporters
                    .Select(file => new DependencyImpactedFile(file, 1))
                    .ToList();

                if (impactedFiles.Count > 0)
                {
                    markdown.AppendLine("- Impacted file details:");
                    foreach (var impacted in impactedFiles.Take(10))
                    {
                        var reason = string.IsNullOrWhiteSpace(impacted.Reason)
                            ? string.Empty
                            : $" — {impacted.Reason}";
                        markdown.AppendLine($"  - `{impacted.File}` (hops: {impacted.Hops}){reason}");
                    }
                }

                var warnings = di.Warnings ?? [];
                foreach (var warning in warnings)
                    markdown.AppendLine($"- Warning: {warning}");

                markdown.AppendLine();

                var limitations = di.Limitations ?? [];
                if (limitations.Count > 0)
                {
                    markdown.AppendLine("**Dependency impact limitations** *(advisory only — not compiler-accurate):*");
                    foreach (var limitation in limitations)
                        markdown.AppendLine($"- {limitation}");
                    markdown.AppendLine();
                }
            }

            if (fileCtx.RelatedResults.Count > 0)
            {
                markdown.AppendLine("**Related results:**");
                foreach (var result in fileCtx.RelatedResults)
                {
                    var lineRange = result.LineStart.HasValue
                        ? result.LineEnd.HasValue && result.LineEnd != result.LineStart
                            ? $" L{result.LineStart}–L{result.LineEnd}"
                            : $" L{result.LineStart}"
                        : string.Empty;
                    var engine = result.Engine is not null ? $" — {result.Engine}" : string.Empty;
                    markdown.AppendLine($"- `{result.File}`{lineRange}{engine}");
                }
                markdown.AppendLine();
            }

            if (fileCtx.Skeleton is not null)
            {
                markdown.AppendLine("**Skeleton:**");
                markdown.AppendLine();
                var lang = fileCtx.Skeleton.Language ?? string.Empty;
                markdown.AppendLine($"```{lang}");
                markdown.AppendLine(fileCtx.Skeleton.Content);
                markdown.AppendLine("```");
                markdown.AppendLine();
            }
        }
    }
}
