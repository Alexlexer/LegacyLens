# LegacyLens

LegacyLens contains the initial RefactorGuard implementation: a local .NET service for AI-assisted review of Git diffs and legacy .NET code.

## Current Status

The repository now includes the RefactorGuard solution skeleton, layered projects, test projects, and CI configuration. Runtime review features will be added in focused follow-up PRs.

## Structure

```text
refactorguard/
  src/
    RefactorGuard.Api/
    RefactorGuard.Application/
    RefactorGuard.Domain/
    RefactorGuard.Infrastructure/
  tests/
    RefactorGuard.Application.Tests/
    RefactorGuard.Infrastructure.Tests/
    RefactorGuard.Api.Tests/
```

## Development

Run from `refactorguard/`:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Run the API locally:

```bash
dotnet run --project src/RefactorGuard.Api
```

Health check:

```text
GET /health
```

Preview the current working-tree diff for an allowed repository:

```text
POST /api/review/diff/preview
Content-Type: application/json

{ "repoPath": "D:\\Projects\\SomeRepo" }
```

Configure allowed repository roots before using diff preview:

```json
{
  "RefactorGuard": {
    "AllowedRoots": ["D:\\Projects"]
  }
}
```
