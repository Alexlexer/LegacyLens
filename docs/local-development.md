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

Build the frontend from `refactorguard/ui`:

```bash
npm install
npm run build
```

The Vite build outputs static files to `src/RefactorGuard.Api/wwwroot`, which the API serves at `/`.

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

## Diff Review with gpu-search Enrichment

`POST /api/review/diff` now collects dependency impact, file skeletons, and related search results from gpu-search-mcp for each changed file (up to 10 files, 5 results per file). The Markdown report includes a **gpu-search Context** section with per-file findings.

The review degrades gracefully: if gpu-search-mcp is not reachable, the deterministic review still completes and an `Info` finding is added.

Recommended two-process workflow:

Terminal 1 — start gpu-search-mcp pointing at the repository to analyse:

```text
gpu-search-mcp --directory D:\Projects\ExampleRepo --http --port 8765
```

Terminal 2 — start RefactorGuard:

```text
dotnet run --project src/RefactorGuard.Api
```

Then call:

```text
POST /api/review/diff
{ "repoPath": "D:\\Projects\\ExampleRepo" }
```

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

Use `POST /api/review/diff` with the same body to generate an enriched Markdown review report. Append `"useLlm": true` to also include an LM Studio summary.

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

## Frontend UI

The minimal UI provides repository path input, diff preview, deterministic or LM Studio review, .NET preset analysis, gpu-search status, and saved report management. During UI-only development, run Vite from `refactorguard/ui`:

```bash
npm run dev
```

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
