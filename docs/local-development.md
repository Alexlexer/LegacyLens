# Local Development

## Target Setup

Primary workstation:

- Windows PC
- Intel i5 13th gen
- 32 GB RAM
- RTX 4060 8 GB
- `gpu-search-mcp`
- RefactorGuard API
- local LLM runtime or configured external LLM client

Secondary client:

- MacBook Air M4
- Browser, VS Code, documentation, and remote client workflows

## Expected Commands

After the .NET solution exists, run these from the repository root:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

If Python `gpu-search-mcp` is changed, also run available Python checks:

```bash
python -m pytest
python -m ruff check .
python -m mypy .
```

## Development Flow

1. Create a focused branch.
2. Implement only the requested scope.
3. Add or update tests.
4. Update docs.
5. Run checks.
6. Commit with a conventional commit message.
7. Push the branch and open a PR.

## Diff Preview Configuration

Diff preview is read-only and only works for repositories under configured allowed roots:

```json
{
  "RefactorGuard": {
    "AllowedRoots": ["D:\\Projects"]
  }
}
```

Use `POST /api/review/diff/preview` with a JSON body such as `{ "repoPath": "D:\\Projects\\ExampleRepo" }`.

Use `POST /api/review/diff` with the same body to generate a deterministic Markdown review report. This does not call an LLM.

To include an LM Studio summary, start LM Studio's local OpenAI-compatible server and send:

```json
{
  "repoPath": "D:\\Projects\\ExampleRepo",
  "useLlm": true
}
```

## gpu-search-mcp Configuration

RefactorGuard expects `gpu-search-mcp` HTTP mode to be reachable through configured options:

```json
{
  "RefactorGuard": {
    "GpuSearch": {
      "BaseUrl": "http://127.0.0.1:8765",
      "TimeoutSeconds": 10
    }
  }
}
```

Use `GET /api/search/status` to verify connectivity without running a review.

Use `POST /api/dotnet/analyze` to run .NET analysis presets through `gpu-search-mcp`. Supported presets include `async-blocking`, `broad-exceptions`, `entity-framework-n-plus-one`, and `nullable-suppression`.

## Report Persistence

Diff review reports are saved to SQLite. The default database path is `data/refactorguard.db` relative to the API working directory:

```json
{
  "RefactorGuard": {
    "Persistence": {
      "DatabasePath": "data/refactorguard.db"
    }
  }
}
```

Use `GET /api/reports`, `GET /api/reports/{id}`, and `DELETE /api/reports/{id}` to manage saved reports.

## LM Studio Configuration

LM Studio is optional. Deterministic review remains the safe default unless `useLlm` is true and the provider is configured for LM Studio:

```json
{
  "RefactorGuard": {
    "Review": {
      "Provider": "LmStudio"
    },
    "LmStudio": {
      "BaseUrl": "http://127.0.0.1:1234/v1/",
      "Model": "local-model",
      "TimeoutSeconds": 60
    }
  }
}
```
