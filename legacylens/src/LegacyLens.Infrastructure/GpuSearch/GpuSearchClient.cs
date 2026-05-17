using System.Net.Http.Json;
using System.Text.Json;
using LegacyLens.Application.Search;
using LegacyLens.Domain.Search;

namespace LegacyLens.Infrastructure.GpuSearch;

public sealed class GpuSearchClient(HttpClient httpClient) : IGpuSearchClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<GpuSearchHealth> GetHealthAsync(CancellationToken cancellationToken)
        => GetRequiredAsync<GpuSearchHealth>("/health", cancellationToken);

    public async Task<GpuSearchStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("/stats", cancellationToken);
        EnsureSuccess(response, "/stats");
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = ReadString(root, "status")
            ?? ReadNestedString(root, "status", "pattern")
            ?? "ok";
        var backend = ReadString(root, "backend")
            ?? ReadNestedString(root, "device", "backend");
        var device = ReadString(root, "device")
            ?? ReadNestedString(root, "device", "torchDevice")
            ?? backend;
        var indexedFileCount = ReadInt(root, "indexedFileCount")
            ?? ReadNestedInt(root, "pattern", "files");

        return new GpuSearchStats(status, backend, device, indexedFileCount);
    }

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

    public async Task<GpuSearchIndexRootResponse> IndexRootAsync(
        GpuSearchIndexRootRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("/index/root", request, JsonOptions, cancellationToken);
        EnsureSuccess(response, "/index/root");
        return await response.Content.ReadFromJsonAsync<GpuSearchIndexRootResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("gpu-search-mcp returned empty response for /index/root.");
    }

    public Task<GpuSearchIndexStatusResponse> GetIndexStatusAsync(CancellationToken cancellationToken)
        => GetRequiredAsync<GpuSearchIndexStatusResponse>("/index/status", cancellationToken);

    public async Task<SignalScanResponse> ScanSignalsAsync(
        SignalScanRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("/scan/signals", request, JsonOptions, cancellationToken);
        EnsureSuccess(response, "/scan/signals");
        return await response.Content.ReadFromJsonAsync<SignalScanResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("gpu-search-mcp returned empty response for /scan/signals.");
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
                $"gpu-search-mcp returned {(int)response.StatusCode} for {path}.",
                null,
                response.StatusCode);
        }
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        EnsureSuccess(response, path);
        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return result ?? throw new InvalidOperationException($"gpu-search-mcp returned empty response for {path}.");
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string? ReadNestedString(JsonElement root, string objectName, string propertyName)
    {
        if (!root.TryGetProperty(objectName, out var obj) || obj.ValueKind != JsonValueKind.Object)
            return null;

        return ReadString(obj, propertyName);
    }

    private static int? ReadInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), out var parsed))
            return parsed;

        return null;
    }

    private static int? ReadNestedInt(JsonElement root, string objectName, string propertyName)
    {
        if (!root.TryGetProperty(objectName, out var obj) || obj.ValueKind != JsonValueKind.Object)
            return null;

        return ReadInt(obj, propertyName);
    }
}

