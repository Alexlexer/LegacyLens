namespace RefactorGuard.Application.DotNetAnalysis;

public sealed class DotNetAnalysisPresetCatalog : IDotNetAnalysisPresetCatalog
{
    private static readonly IReadOnlyList<DotNetAnalysisPreset> Presets =
    [
        new(
            "async-blocking",
            "Async blocking calls",
            "Task.Result OR Task.Wait OR GetAwaiter().GetResult",
            "Blocking async calls can cause thread-pool starvation or deadlocks in .NET applications."),
        new(
            "broad-exceptions",
            "Broad exception handling",
            "catch (Exception) OR catch(Exception)",
            "Broad exception handlers can hide operational failures unless converted to explicit domain errors."),
        new(
            "entity-framework-n-plus-one",
            "Entity Framework query risks",
            "DbContext Include ToList foreach async Entity Framework",
            "Entity Framework query patterns should be reviewed for N+1 queries and premature materialization."),
        new(
            "nullable-suppression",
            "Nullable suppression usage",
            "null! OR #nullable disable",
            "Nullable suppression can hide null-safety problems and should be justified.")
    ];

    public IReadOnlyList<DotNetAnalysisPreset> GetAll() => Presets;

    public IReadOnlyList<DotNetAnalysisPreset> Resolve(IReadOnlyList<string>? presetIds)
    {
        if (presetIds is null || presetIds.Count == 0)
        {
            return Presets;
        }

        var requested = presetIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var resolved = Presets.Where(preset => requested.Contains(preset.Id)).ToList();
        var unknown = requested.Except(resolved.Select(preset => preset.Id), StringComparer.OrdinalIgnoreCase).ToList();
        if (unknown.Count > 0)
        {
            throw new ArgumentException($"Unknown .NET analysis preset(s): {string.Join(", ", unknown)}.");
        }

        return resolved;
    }
}
