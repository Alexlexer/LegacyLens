namespace LegacyLens.Domain.Search;

public sealed class GpuSearchIndexComponentStatus
{
    public bool Ready { get; init; }

    public int? Files { get; init; }

    public bool? FromCache { get; init; }

    public bool? Requested { get; init; }

    public string? Message { get; init; }
}
