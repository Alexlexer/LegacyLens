using System.ComponentModel.DataAnnotations;

namespace LegacyLens.Infrastructure.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "RefactorGuard:Persistence";

    [Required]
    public string DatabasePath { get; init; } = "data/LegacyLens.db";
}
