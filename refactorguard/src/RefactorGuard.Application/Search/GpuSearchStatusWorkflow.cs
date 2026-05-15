using RefactorGuard.Domain.Search;

namespace RefactorGuard.Application.Search;

public sealed class GpuSearchStatusWorkflow(IGpuSearchClient gpuSearchClient)
{
    public async Task<GpuSearchStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var health = await gpuSearchClient.GetHealthAsync(cancellationToken);
            var stats = await gpuSearchClient.GetStatsAsync(cancellationToken);
            return new GpuSearchStatusResponse(true, health, stats, null);
        }
        catch (HttpRequestException ex)
        {
            return Unavailable(ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Unavailable(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Unavailable(ex.Message);
        }
    }

    private static GpuSearchStatusResponse Unavailable(string error)
    {
        return new GpuSearchStatusResponse(false, null, null, error);
    }
}
