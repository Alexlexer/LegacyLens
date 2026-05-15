using System.Net.Http.Json;
using System.Text.Json;
using RefactorGuard.Application.Search;
using RefactorGuard.Domain.Search;

namespace RefactorGuard.Infrastructure.GpuSearch;

public sealed class GpuSearchClient(HttpClient httpClient) : IGpuSearchClient
{
    public async Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken)
    {
        return await GetRequiredAsync<GpuSearchHealth>("/health", cancellationToken);
    }

    public async Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        return await GetRequiredAsync<GpuSearchStats>("/stats", cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchHybridAsync(
        SearchHybridRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("/search/hybrid", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"gpu-search-mcp returned {(int)response.StatusCode} for /search/hybrid.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var wrapped = JsonSerializer.Deserialize<SearchResultsResponse>(json, JsonOptions);
        if (wrapped is not null)
        {
            return wrapped.Results;
        }

        var direct = JsonSerializer.Deserialize<IReadOnlyList<SearchResult>>(json, JsonOptions);
        return direct ?? throw new InvalidOperationException("gpu-search-mcp returned an empty search response.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"gpu-search-mcp returned {(int)response.StatusCode} for {path}.");
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return result ?? throw new InvalidOperationException($"gpu-search-mcp returned an empty response for {path}.");
    }
}
