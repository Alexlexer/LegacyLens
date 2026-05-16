using System.Diagnostics;
using LegacyLens.Application.Git;
using LegacyLens.Domain.Git;
using LegacyLens.Infrastructure.Security;

namespace LegacyLens.Infrastructure.Git;

public sealed class GitDiffService(IRepoPathValidator repoPathValidator) : IGitDiffService
{
    public async Task<GitDiffPreviewResponse> GetCurrentDiffAsync(
        GitDiffPreviewRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RepoPath))
        {
            throw new ArgumentException("Repository path is required.", nameof(request));
        }

        var repoPath = repoPathValidator.Validate(request.RepoPath);
        await EnsureGitRepositoryAsync(repoPath, cancellationToken);

        var nameStatus = await RunGitAsync(repoPath, ["diff", "--name-status", "--"], cancellationToken);
        var numStat = await RunGitAsync(repoPath, ["diff", "--numstat", "--"], cancellationToken);
        var diff = await RunGitAsync(repoPath, ["diff", "--"], cancellationToken);

        var files = BuildFiles(nameStatus, numStat);
        return new GitDiffPreviewResponse(repoPath, files.Count, files, diff);
    }

    private static async Task EnsureGitRepositoryAsync(string repoPath, CancellationToken cancellationToken)
    {
        var result = await RunGitAsync(repoPath, ["rev-parse", "--is-inside-work-tree"], cancellationToken);
        if (!string.Equals(result.Trim(), "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The provided path is not a Git work tree.");
        }
    }

    private static async Task<string> RunGitAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(SanitizeGitError(error));
        }

        return output;
    }

    private static List<GitDiffFile> BuildFiles(string nameStatusOutput, string numStatOutput)
    {
        var stats = ParseNumStat(numStatOutput);
        return nameStatusOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => ParseNameStatusLine(line, stats))
            .ToList();
    }

    private static Dictionary<string, (int Additions, int Deletions)> ParseNumStat(string numStatOutput)
    {
        var stats = new Dictionary<string, (int Additions, int Deletions)>(StringComparer.Ordinal);

        foreach (var line in numStatOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length < 3)
            {
                continue;
            }

            var path = parts[^1];
            stats[path] = (ParseCount(parts[0]), ParseCount(parts[1]));
        }

        return stats;
    }

    private static GitDiffFile ParseNameStatusLine(
        string line,
        IReadOnlyDictionary<string, (int Additions, int Deletions)> stats)
    {
        var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("Git returned an unexpected name-status line.");
        }

        var status = parts[0];
        var path = parts[^1];
        stats.TryGetValue(path, out var counts);
        return new GitDiffFile(path, status, counts.Additions, counts.Deletions);
    }

    private static int ParseCount(string value)
    {
        return int.TryParse(value, out var count) ? count : 0;
    }

    private static string SanitizedFallback => "Git command failed. Verify the repository path and working tree state.";

    private static string SanitizeGitError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return SanitizedFallback;
        }

        var firstLine = error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstLine) ? SanitizedFallback : firstLine;
    }
}
