using System.ComponentModel.DataAnnotations;

namespace RefactorGuard.Infrastructure.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "RefactorGuard:Persistence";

    [Required]
    public string DatabasePath { get; init; } = "data/refactorguard.db";
}
