namespace LegacyLens.Application.Audit;

public sealed record ArchitectureSignal(
    string Name,
    string Message,
    string Evidence,
    string Confidence);
