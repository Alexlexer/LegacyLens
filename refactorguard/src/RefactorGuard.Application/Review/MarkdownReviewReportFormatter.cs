using System.Text;

namespace RefactorGuard.Application.Review;

public sealed class MarkdownReviewReportFormatter : IReviewReportFormatter
{
    public string Format(DiffReviewReport report)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine("# RefactorGuard Diff Review");
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

        if (!string.IsNullOrWhiteSpace(report.LlmSummary))
        {
            markdown.AppendLine();
            markdown.AppendLine($"## LLM Summary ({report.LlmProvider})");
            markdown.AppendLine();
            markdown.AppendLine(report.LlmSummary);
        }

        return markdown.ToString();
    }
}
