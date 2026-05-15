using System.Net.Http.Json;
using System.Text.Json;
using RefactorGuard.Application.Search;
using RefactorGuard.Domain.Search;

namespace RefactorGuard.Infrastructure.GpuSearch;

public sealed class GpuSearchClient(HttpClient httpClient) : IGpuSearchClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken)
        => GetRequiredAsync<GpuSearchHealth>("/health", cancellationToken);

    public Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
        => GetRequiredAsync<GpuSearchStats>("/stats", cancellationToken);

    public Task<IReadOnlyList<GpuSearchResult>> SearchCodeAsync(
        CodeSearchRequest request,
        CancellationToken cancellationToken)
        => PostSearchAsync("/search/code", request, cancellationToken);

    public Task<IReadOnlyList<GpuSearchResult>> SearchSemanticAsync(
        CodeSearchRequest request,
        CancellationToken cancellationToken)
        => PostSearchAsync("/search/semantic", request, cancellationToken);

    public Task<IReadOnlyList<GpuSearchResult>> SearchHybridAsync(
        SearchHybridRequest request,
        CancellationToken cancellationToken)
        => PostSearchAsync("/search/hybrid", request, cancellationToken);

    public async Task<ReadBlockResponse> ReadBlockAsync(
        ReadBlockRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("/read/block", request, JsonOptions, cancellationToken);
        EnsureSuccess(response, "/read/block");
        return await response.Content.ReadFromJsonAsync<ReadBlockResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("gpu-search-mcp returned empty response for /read/block.");
    }

    public async Task<ReadSkeletonResponse> ReadSkeletonAsync(
        ReadSkeletonRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("/read/skeleton", request, JsonOptions, cancellationToken);
        EnsureSuccess(response, "/read/skeleton");
        return await response.Content.ReadFromJsonAsync<ReadSkeletonResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("gpu-search-mcp returned empty response for /read/skeleton.");
    }

    public async Task<DependencyImpactResponse> GetDependencyImpactAsync(
        DependencyImpactRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("/dependency/impact", request, JsonOptions, cancellationToken);
        EnsureSuccess(response, "/dependency/impact");
        return await response.Content.ReadFromJsonAsync<DependencyImpactResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("gpu-search-mcp returned empty response for /dependency/impact.");
    }

    private async Task<IReadOnlyList<GpuSearchResult>> PostSearchAsync<TRequest>(
        string path,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(path, request, JsonOptions, cancellationToken);
        EnsureSuccess(response, path);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
            doc.RootElement.TryGetProperty("results", out var resultsEl))
        {
            return JsonSerializer.Deserialize<IReadOnlyList<GpuSearchResult>>(
                resultsEl.GetRawText(), JsonOptions) ?? [];
        }
        return JsonSerializer.Deserialize<IReadOnlyList<GpuSearchResult>>(json, JsonOptions) ?? [];
    }

    private static void EnsureSuccess(HttpResponseMessage response, string path)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"gpu-search-mcp returned {(int)response.StatusCode} for {path}.");
        }
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        EnsureSuccess(response, path);
        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return result ?? throw new InvalidOperationException($"gpu-search-mcp returned empty response for {path}.");
    }
}
