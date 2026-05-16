using System.Text.Json.Serialization;

namespace LegacyLens.Application.DotNetAnalysis;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DotNetWorkspaceKind
{
    Slnx,
    Sln,
    Csproj
}
