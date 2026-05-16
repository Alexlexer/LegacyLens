namespace LegacyLens.Application.Audit;

public sealed record LegacyAuditRequest(
    string? RepoPath,
    bool UseLlm = false,
    bool IncludeRoslyn = true,
    bool IncludeGpuSearch = true,
    bool IncludeDotNetPresets = true,
    bool IncludeDependencyInjection = true);
