using System.Text.Json.Serialization;

namespace RefactorGuard.Application.DotNetAnalysis;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DotNetWorkspaceKind
{
    Slnx,
    Sln,
    Csproj
}
