using System.Net.Http.Json;
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
