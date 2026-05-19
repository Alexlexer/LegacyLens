using System.Net;
using System.Text.Json;
using LegacyLens.Domain.Search;
using LegacyLens.Infrastructure.GpuSearch;

namespace LegacyLens.Infrastructure.Tests.GpuSearch;

public sealed class GpuSearchClientTests
{
    [Fact]
    public async Task GetHealthAsync_ReturnsHealthResponse()
    {
        var client = CreateClient(new { status = "ok" });
        var gpuSearchClient = new GpuSearchClient(client);

        var health = await gpuSearchClient.GetHealthAsync(CancellationToken.None);

        Assert.Equal("ok", health.Status);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsStatsResponse()
    {
        var client = CreateClient(new
        {
            status = "ok",
            backend = "cuda",
            device = "RTX 4060",
            indexedFileCount = 42
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var stats = await gpuSearchClient.GetStatsAsync(CancellationToken.None);

        Assert.Equal("ok", stats.Status);
        Assert.Equal("cuda", stats.Backend);
        Assert.Equal("RTX 4060", stats.Device);
        Assert.Equal(42, stats.IndexedFileCount);
    }


    [Fact]
    public async Task GetStatsAsync_ReturnsStatsFromStructuredGpuSearchResponse()
    {
        var client = CreateClient(new
        {
            pattern = new { files = 158, baseDir = "D:/Repo" },
            dependency = new { files = 132, edges = 47 },
            status = new { pattern = "done: 158 files", deps = "done: 132 files", semantic = "" },
            device = new { backend = "cuda", torchDevice = "cuda", reason = "CUDA available" }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var stats = await gpuSearchClient.GetStatsAsync(CancellationToken.None);

        Assert.Equal("done: 158 files", stats.Status);
        Assert.Equal("cuda", stats.Backend);
        Assert.Equal("cuda", stats.Device);
        Assert.Equal(158, stats.IndexedFileCount);
    }
    [Fact]
    public async Task GetHealthAsync_ThrowsWhenServerReturnsFailure()
    {
        var client = CreateClient(new { error = "failed" }, HttpStatusCode.ServiceUnavailable);
        var gpuSearchClient = new GpuSearchClient(client);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            gpuSearchClient.GetHealthAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SearchHybridAsync_ReturnsWrappedResults()
    {
        var client = CreateClient(new
        {
            results = new[]
            {
                new { file = "src/App.cs", lineStart = 10, snippet = "Task.Result", score = 0.9, engine = "hybrid" }
            }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var results = await gpuSearchClient.SearchHybridAsync(
            new SearchHybridRequest("Task.Result", null, 5),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("src/App.cs", results[0].File);
        Assert.Equal(10, results[0].LineStart);
        Assert.Equal("Task.Result", results[0].Snippet);
        Assert.Equal(0.9, results[0].Score);
        Assert.Equal("hybrid", results[0].Engine);
    }

    [Fact]
    public async Task SearchCodeAsync_ReturnsStructuredResults()
    {
        var client = CreateClient(new
        {
            results = new[]
            {
                new { file = "src/UserService.cs", lineStart = 20, lineEnd = 25, snippet = "AddSingleton", score = 1.0, engine = "exact", reason = "token match" }
            }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var results = await gpuSearchClient.SearchCodeAsync(
            new CodeSearchRequest("AddSingleton", 5),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("src/UserService.cs", results[0].File);
        Assert.Equal(20, results[0].LineStart);
        Assert.Equal(25, results[0].LineEnd);
        Assert.Equal("exact", results[0].Engine);
        Assert.Equal("token match", results[0].Reason);
    }

    [Fact]
    public async Task SearchSemanticAsync_ReturnsStructuredResults()
    {
        var client = CreateClient(new
        {
            results = new[]
            {
                new { file = "src/Auth.cs", lineStart = 5, snippet = "async Task Login", score = 0.85, engine = "semantic" }
            }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var results = await gpuSearchClient.SearchSemanticAsync(
            new CodeSearchRequest("user authentication", 3),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("src/Auth.cs", results[0].File);
        Assert.Equal("semantic", results[0].Engine);
    }

    [Fact]
    public async Task ReadBlockAsync_ReturnsBlockResponse()
    {
        var client = CreateClient(new
        {
            result = "ok",
            file = "src/App.cs",
            lineStart = 30,
            lineEnd = 50,
            content = "public class App { }",
            language = "csharp"
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var block = await gpuSearchClient.ReadBlockAsync(
            new ReadBlockRequest("src/App.cs", 40, 10),
            CancellationToken.None);

        Assert.Equal("ok", block.Result);
        Assert.Equal("src/App.cs", block.File);
        Assert.Equal(30, block.LineStart);
        Assert.Equal(50, block.LineEnd);
        Assert.Equal("public class App { }", block.Content);
        Assert.Equal("csharp", block.Language);
    }

    [Fact]
    public async Task ReadSkeletonAsync_ReturnsSkeletonResponse()
    {
        var client = CreateClient(new
        {
            result = "ok",
            file = "src/UserService.cs",
            content = "public class UserService { ... }",
            matchLines = new[] { 1, 10, 20 },
            language = "csharp"
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var skeleton = await gpuSearchClient.ReadSkeletonAsync(
            new ReadSkeletonRequest("src/UserService.cs"),
            CancellationToken.None);

        Assert.Equal("ok", skeleton.Result);
        Assert.Equal("src/UserService.cs", skeleton.File);
        Assert.Contains("UserService", skeleton.Content);
        Assert.Equal(3, skeleton.MatchLines?.Count);
        Assert.Equal("csharp", skeleton.Language);
    }

    [Fact]
    public async Task GetDependencyImpactAsync_ReturnsDependencyImpactResponse()
    {
        var client = CreateClient(new
        {
            result = "ok",
            file = "src/UserService.cs",
            impactedFiles = new[]
            {
                new { file = "src/AuthController.cs", hops = 1 },
                new { file = "src/AdminController.cs", hops = 1 },
                new { file = "src/RootController.cs", hops = 2 }
            }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var impact = await gpuSearchClient.GetDependencyImpactAsync(
            new DependencyImpactRequest("src/UserService.cs"),
            CancellationToken.None);

        Assert.Equal("ok", impact.Result);
        Assert.Equal("src/UserService.cs", impact.File);
        Assert.Equal(3, impact.ImpactedFiles.Count);
        Assert.Equal("src/AuthController.cs", impact.ImpactedFiles[0].File);
        Assert.Equal(1, impact.ImpactedFiles[0].Hops);
        Assert.Equal(2, impact.ImpactedFiles[2].Hops);
    }

    [Fact]
    public async Task SearchHybridAsync_ReturnsEmpty_WhenResultsArrayIsEmpty()
    {
        var client = CreateClient(new { results = Array.Empty<object>() });
        var gpuSearchClient = new GpuSearchClient(client);

        var results = await gpuSearchClient.SearchHybridAsync(
            new SearchHybridRequest("nothing", null, 5),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetDependencyImpactAsync_ParsesConfidenceAndAnalysisMode()
    {
        var client = CreateClient(new
        {
            result = "ok",
            file = "src/UserService.cs",
            confidence = "medium",
            analysisMode = "heuristic",
            impactedFiles = new[]
            {
                new { file = "src/AuthController.cs", hops = 1 }
            }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var impact = await gpuSearchClient.GetDependencyImpactAsync(
            new DependencyImpactRequest("src/UserService.cs"),
            CancellationToken.None);

        Assert.Equal("medium", impact.Confidence);
        Assert.Equal("heuristic", impact.AnalysisMode);
    }

    [Fact]
    public async Task GetDependencyImpactAsync_ParsesLimitationsAndWarnings()
    {
        var client = CreateClient(new
        {
            result = "ok",
            file = "src/UserService.cs",
            confidence = "low",
            analysisMode = "heuristic",
            limitations = new[] { "C# analysis does not use Roslyn.", "Dynamic dispatch not tracked." },
            warnings = new[] { "file not present in graph" },
            impactedFiles = Array.Empty<object>()
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var impact = await gpuSearchClient.GetDependencyImpactAsync(
            new DependencyImpactRequest("src/UserService.cs"),
            CancellationToken.None);

        Assert.Equal(2, impact.Limitations?.Count);
        Assert.Equal("C# analysis does not use Roslyn.", impact.Limitations![0]);
        Assert.Single(impact.Warnings!);
        Assert.Equal("file not present in graph", impact.Warnings![0]);
    }

    [Fact]
    public async Task GetDependencyImpactAsync_ParsesImpactedFileReason()
    {
        var client = CreateClient(new
        {
            result = "ok",
            file = "src/UserService.cs",
            impactedFiles = new[]
            {
                new { file = "src/UserController.cs", hops = 1, reason = "references type UserService" }
            }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var impact = await gpuSearchClient.GetDependencyImpactAsync(
            new DependencyImpactRequest("src/UserService.cs"),
            CancellationToken.None);

        Assert.Single(impact.ImpactedFiles);
        Assert.Equal("references type UserService", impact.ImpactedFiles[0].Reason);
    }

    [Fact]
    public async Task GetDependencyImpactAsync_HandlesMissingImpactedFileReason()
    {
        var client = CreateClient(new
        {
            result = "ok",
            file = "src/UserService.cs",
            impactedFiles = new[]
            {
                new { file = "src/UserController.cs", hops = 1 }
            }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var impact = await gpuSearchClient.GetDependencyImpactAsync(
            new DependencyImpactRequest("src/UserService.cs"),
            CancellationToken.None);

        Assert.Single(impact.ImpactedFiles);
        Assert.Null(impact.ImpactedFiles[0].Reason);
    }

    [Fact]
    public async Task GetDependencyImpactAsync_HandlesAbsentMetadataFields_Safely()
    {
        var client = CreateClient(new
        {
            result = "ok",
            file = "src/UserService.cs",
            impactedFiles = new[]
            {
                new { file = "src/AuthController.cs", hops = 1 }
            }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var impact = await gpuSearchClient.GetDependencyImpactAsync(
            new DependencyImpactRequest("src/UserService.cs"),
            CancellationToken.None);

        Assert.Null(impact.Confidence);
        Assert.Null(impact.AnalysisMode);
        Assert.Null(impact.Limitations);
        Assert.Null(impact.Warnings);
        Assert.Single(impact.ImpactedFiles);
    }


    [Fact]
    public async Task GetIndexStatusAsync_ReturnsIndexStatusResponse()
    {
        var client = CreateClient(new
        {
            indexedRoots = new object[] { new { path = "D:/Repos/App" } },
            pattern = new { ready = true, files = 12 },
            dependency = new { ready = true },
            semantic = new { ready = false },
            status = "ok",
            lastIndexResult = new { ok = true, normalizedDirectory = "D:/Repos/App" }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var status = await gpuSearchClient.GetIndexStatusAsync(CancellationToken.None);

        Assert.Single(status.IndexedRoots);
        Assert.True(status.Pattern?.Ready);
        Assert.Equal(12, status.Pattern?.Files);
        Assert.Equal("ok", status.Status);
        Assert.True(status.LastIndexResult?.Ok);
    }

    [Fact]
    public async Task IndexRootAsync_ReturnsIndexRootResponse()
    {
        var client = CreateClient(new
        {
            ok = true,
            directory = "D:/Repos/App",
            normalizedDirectory = "D:/Repos/App",
            started = true,
            completed = true,
            pattern = new { ready = true, files = 12, fromCache = false },
            dependency = new { ready = true },
            semantic = new { requested = false, ready = false },
            message = "indexed"
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var result = await gpuSearchClient.IndexRootAsync(
            new GpuSearchIndexRootRequest("D:/Repos/App"),
            CancellationToken.None);

        Assert.True(result.Ok);
        Assert.True(result.Completed);
        Assert.True(result.Pattern?.Ready);
        Assert.Equal(12, result.Pattern?.Files);
        Assert.False(result.Semantic?.Requested);
        Assert.Equal("indexed", result.Message);
    }
    private static HttpClient CreateClient(object response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpClient(new StubHandler(response, statusCode))
        {
            BaseAddress = new Uri("http://127.0.0.1:8765")
        };
    }

    private sealed class StubHandler(object response, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}


