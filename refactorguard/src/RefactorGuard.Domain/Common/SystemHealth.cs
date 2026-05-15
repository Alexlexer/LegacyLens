namespace RefactorGuard.Domain.Common;

public sealed record SystemHealth(string Status, string Service)
{
    public static SystemHealth Healthy() => new("ok", "LegacyLens");
}
