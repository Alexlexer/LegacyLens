using System.ComponentModel.DataAnnotations;

namespace LegacyLens.Application.Audit;

public sealed class GpuSearchAuditOptions
{
    public const string SectionName = "RefactorGuard:GpuSearch";

    public bool EnsureIndexedRootBeforeAudit { get; init; } = true;

    public bool RebuildCacheOnAudit { get; init; } = false;

    public bool IncludeSemanticIndexOnAudit { get; init; } = false;

    [Range(1, 1800)]
    public int IndexRootTimeoutSeconds { get; init; } = 120;
}
