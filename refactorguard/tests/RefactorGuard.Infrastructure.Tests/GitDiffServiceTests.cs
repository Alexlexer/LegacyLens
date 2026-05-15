using System.Diagnostics;
using RefactorGuard.Domain.Git;
using RefactorGuard.Infrastructure.Git;
using RefactorGuard.Infrastructure.Security;

namespace RefactorGuard.Infrastructure.Tests;

public sealed class GitDiffServiceTests : IDisposable
{
    private readonly string _root = CreateTempDirectory();

    [Fact]
    public async Task GetCurrentDiffAsync_ReturnsChangedFilesAndDiff()
    {
        RunGit(_root, "init");
        RunGit(_root, "config", "user.email", "test@example.local");
        RunGit(_root, "config", "user.name", "Test User");
        File.WriteAllText(Path.Combine(_root, "sample.txt"), "old\n");
        RunGit(_root, "add", "sample.txt");
        RunGit(_root, "commit", "-m", "initial");
        File.WriteAllText(Path.Combine(_root, "sample.txt"), "old\nnew\n");

        var service = new GitDiffService(new RepoPathValidator([_root]));

        var response = await service.GetCurrentDiffAsync(new GitDiffPreviewRequest(_root), CancellationToken.None);

        Assert.Equal(1, response.ChangedFileCount);
        Assert.Contains(response.Files, file => file.Path == "sample.txt" && file.Additions == 1);
        Assert.Contains("+new", response.Diff);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(_root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"refactorguard-git-{Guid.NewGuid():N}")).FullName;
    }

    private static void RunGit(string workingDirectory, params string[] arguments)
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
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(error);
        }
    }
}
