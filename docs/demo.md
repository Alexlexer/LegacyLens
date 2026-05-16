# Local Demo Walkthrough

This guide covers running LegacyLens together with `gpu-search-mcp` on a local machine for a full end-to-end demo.

> **Read-only.** LegacyLens never modifies the repository being analysed.

## Prerequisites

| Requirement | Notes |
|---|---|
| .NET 10 SDK | Run `dotnet --version` to verify |
| `gpu-search-mcp` | Install and confirm it is on PATH or note its full path |
| A Git repository with at least one committed change | Used as the analysis target |

Optional: LM Studio or Ollama running locally for LLM-enhanced summaries.

---

## Step 1 — Configure allowed roots

LegacyLens rejects repository paths that are not under a configured allowed root.

Copy the example config and edit it:

```bash
cp LegacyLens/appsettings.Local.example.json LegacyLens/appsettings.Local.json
```

Edit `LegacyLens/appsettings.Local.json`:

```json
{
  "LegacyLens": {
    "AllowedRoots": ["D:\\Projects"]
  }
}
```

Set `AllowedRoots` to a list that contains the parent directory of the repository you want to analyse. ASP.NET Core loads `appsettings.Local.json` automatically when it exists alongside `appsettings.json`.

> `appsettings.Local.json` is git-ignored — it will not be committed.

---

## Step 2 — Start gpu-search-mcp (Terminal 1)

```text
gpu-search-mcp --directory D:\Projects\SomeRepo --http --port 8765
```

Replace `D:\Projects\SomeRepo` with the path to the repository you want to analyse.

Wait until you see:

```text
Uvicorn running on http://127.0.0.1:8765
```

Verify it is up:

```text
GET http://127.0.0.1:8765/health
```

---

## Step 3 — Start LegacyLens (Terminal 2)

```bash
cd LegacyLens
dotnet run --project src/LegacyLens.Api
```

Wait until you see:

```text
Now listening on: http://localhost:5096
```

---

## Step 4 — Open the UI

Navigate to:

```text
http://localhost:5096
```

The LegacyLens UI loads in your browser.

---

## Step 5 — Enter the repository path

In the **Repository path** field, enter the full path to the repository you want to analyse:

```text
D:\Projects\SomeRepo
```

This must be the same repository you pointed `gpu-search-mcp` at in Step 2, and it must be under one of the configured `AllowedRoots`.

---

## Step 6 — Run the demo features

### Check gpu-search status

Click **Check gpu-search**. The output panel shows health, backend, device, and indexed file count.

### Preview diff

Click **Preview diff**. LegacyLens calls `git diff HEAD` on the repository and shows changed files with addition/deletion counts plus the raw diff text.

### Run review

Click **Run review**. LegacyLens generates a Markdown report with:

- **Deterministic findings** — empty diff, large change, test changes, config changes.
- **Roslyn Reference Context** — for each changed `.cs` file, LegacyLens identifies the primary symbol by filename convention and runs Roslyn reference analysis to find how many callers reference it across the solution. This is compiler-accurate. An `Info` finding is added if Roslyn workspace loading fails (e.g. no `.sln`/`.csproj` found).
- **gpu-search Context** — when gpu-search-mcp is running: dependency impact with confidence/analysis-mode metadata, impacted-file reasons, related code, and file skeletons for each changed file. An `Info` finding is added if gpu-search-mcp was not reachable.

> **Dependency impact is advisory.** gpu-search-mcp analyses imports and type/name heuristics — it is not compiler-accurate. Roslyn references are compiler-verified facts and should be weighted more heavily.

Enrichment limits are configurable in `LegacyLens:ReviewEnrichment`:

```json
{
  "LegacyLens": {
    "ReviewEnrichment": {
      "MaxFilesToEnrich": 10,
      "MaxSearchResultsPerFile": 5,
      "MaxSkeletonLength": 4000,
      "MaxBlockLength": 4000,
      "MaxRelatedResultSnippetLength": 1000,
      "MaxSymbolsForReferenceAnalysis": 10,
      "MaxReferencesPerSymbol": 50,
      "MaxTotalRoslynReferences": 200
    }
  }
}
```

Defaults are safe and preserve demo behavior. Lower values are faster and create smaller reports; higher values provide deeper context but can increase token usage and local LLM summary time.

The Roslyn limits control how many C# files receive reference analysis (`MaxSymbolsForReferenceAnalysis`), how many references are collected per symbol (`MaxReferencesPerSymbol`), and the global cap across all symbols (`MaxTotalRoslynReferences`). Roslyn analysis requires a `.sln`, `.slnx`, or `.csproj` discoverable from the repository root and a local .NET SDK installation.

The report is saved to SQLite and appears in the **Saved reports** panel.

To include a local LLM summary, configure LM Studio or Ollama as the review provider, then tick **Include local LLM summary** before clicking **Run review**. The checkbox enables `useLlm=true`; deterministic review remains the default when it is unchecked.

The report viewer is structured for demos:

- **Summary** shows repository, created time, changed files, and provider/review mode.
- **Findings** are shown as severity-badged cards.
- **gpu-search Context** shows dependency impact, confidence, analysis mode, impacted files, warnings, limitations, related search results, and skeleton previews.
- **LLM Summary** appears when LM Studio returns a summary.
- **Raw Markdown** remains available in a collapsible block.

Use the copy buttons to copy the full Markdown report, gpu-search context, or LLM summary.

### .NET analysis

Click **.NET analysis**. LegacyLens runs all built-in preset searches through `gpu-search-mcp`:

- `async-blocking` — `.Result` / `.Wait()` on async calls
- `broad-exceptions` — `catch (Exception)` without re-throw
- `entity-framework-n-plus-one` — N+1 query patterns
- `nullable-suppression` — `!` null-forgiving operator

Findings include file path, line number, and a code snippet.

### Saved reports

The **Saved reports** panel lists all generated reports. Click **View** to reload a report or **Delete** to remove it.

Saved **Legacy Audit** reports also include **Export Markdown** and **Export HTML** buttons. These trigger local downloads from:

```text
GET /api/audit/reports/{id}/export/markdown
GET /api/audit/reports/{id}/export/html
```

The HTML export is self-contained with embedded CSS and no CDN dependencies. Exported reports may include repository paths, snippets, findings, and optional LLM summaries, so review them before sharing externally.

---

## Step 7 — Find saved reports

Reports are stored in SQLite at the path configured in `Persistence:DatabasePath` (default: `data/legacylens.db` relative to the API working directory, i.e. `LegacyLens/data/legacylens.db`).

The `data/` directory and `*.db` files are git-ignored.

---

## Helper scripts

Use the scripts in `scripts/` to get the exact startup commands printed to your terminal:

**Windows (PowerShell):**

```powershell
.\scripts\start-demo.ps1 -RepoPath "D:\Projects\SomeRepo"
```

**macOS / Linux (bash):**

```bash
./scripts/start-demo.sh /Users/you/Projects/SomeRepo
```

Both scripts print labelled command blocks for each terminal. Nothing is started automatically.

**Smoke checks (Windows):**

```powershell
.\scripts\smoke-demo.ps1 -RepoPath "D:\Projects\SomeRepo"
```

This runs GET and POST checks against the running services and prints pass/fail for each endpoint.

---

## Troubleshooting

### gpu-search unavailable

The review still completes. An `Info` finding labelled `gpu-search-unavailable` is added to the report. Check that:

- `gpu-search-mcp` is running and the health URL responds.
- The `GpuSearch:BaseUrl` in config matches the port you started it on.

### Repository path outside allowed roots

LegacyLens returns a `400 Bad Request`. Add the parent directory to `AllowedRoots` in `appsettings.Local.json` and restart the API.

### No Git diff shown

`git diff HEAD` returns nothing if the working tree is clean. Make an uncommitted change, or use `git diff <commit>` manually to confirm there is something to diff.

### Dependency graph still indexing

gpu-search-mcp builds its index on first start. The `/stats` endpoint shows `indexedFileCount`. If it is 0 or very low, wait a moment and retry.

### LM Studio unavailable

The review falls back to the deterministic provider. Check that LM Studio's local server is running on the configured `LmStudio:BaseUrl`. The default is `http://127.0.0.1:1234/v1/`.

### Ollama provider

Ollama is optional and local-first. Pull a model and start Ollama:

```bash
ollama pull qwen2.5-coder:7b
ollama serve
```

Configure LegacyLens:

```json
{
  "LegacyLens": {
    "Review": {
      "Provider": "Ollama"
    },
    "Ollama": {
      "BaseUrl": "http://127.0.0.1:11434",
      "Model": "qwen2.5-coder:7b",
      "TimeoutSeconds": 120
    }
  }
}
```

`useLlm=true` is still required. Prompts may contain diffs and code snippets, so use a trusted local/private Ollama instance. Ollama works well with an RTX GPU when configured to use it.

### SQLite database path

If the API reports a database error, confirm the `data/` directory exists relative to where you ran `dotnet run`. The directory is created automatically on first run if the process has write access.

### Windows execution policy for PowerShell scripts

If PowerShell blocks the scripts, run:

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

Or run the script explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-demo.ps1 -RepoPath "D:\Projects\SomeRepo"
```

---

## Integration smoke test

Use the sample fixture and integration smoke script to verify that LegacyLens and gpu-search-mcp work together end-to-end, without touching any of your real repositories.

### Step 1 — Prepare the sample diff

The fixture repo lives at `test-fixtures/sample-dotnet-repo`. Run the prepare script to initialise its Git repo and leave a deterministic dirty change in `src/UserService.cs`:

**Windows:**

```powershell
.\scripts\prepare-sample-diff.ps1
```

**macOS / Linux:**

```bash
./scripts/prepare-sample-diff.sh
```

The script is idempotent — repeated runs reset the fixture to a known state and re-apply the same change.

### Step 2 — Configure allowed roots

Ensure `test-fixtures` (or its parent) is in `AllowedRoots` in `LegacyLens/appsettings.Local.json`:

```json
{
  "LegacyLens": {
    "AllowedRoots": ["D:\\Projects\\LegacyLens\\test-fixtures"]
  }
}
```

Adjust the path to match where you cloned the repository.

### Step 3 — Start gpu-search-mcp (Terminal 1)

```text
gpu-search-mcp --directory ./test-fixtures/sample-dotnet-repo --http --port 8765
```

### Step 4 — Start LegacyLens (Terminal 2)

```bash
cd LegacyLens
dotnet run --project src/LegacyLens.Api
```

### Step 5 — Run the integration smoke script (Terminal 3)

**Windows:**

```powershell
.\scripts\integration-smoke.ps1 -RepoPath "D:\Projects\LegacyLens\test-fixtures\sample-dotnet-repo"
```

**macOS / Linux:**

```bash
./scripts/integration-smoke.sh
```

### Expected output

```text
============================================
  LegacyLens Integration Smoke Test
============================================

--- Health checks ---
  [PASS] gpu-search /health
  [PASS] LegacyLens /health

--- Search status ---
  [PASS] GET /api/search/status
         gpu-search available: True

--- Diff preview ---
  [PASS] POST /api/review/diff/preview
         Changed files: 1

--- Diff review (deterministic) ---
  [PASS] POST /api/review/diff
         Report ID: <uuid>
  [PASS] gpu-search context section present in report

--- Saved reports ---
  [PASS] GET /api/reports

============================================
  Results: 7 passed, 0 failed
============================================
```

If gpu-search-mcp is not running, the `[PASS]` for the gpu-search context check will instead read:

```text
  [PASS] gpu-search graceful fallback finding present in report
```

The overall smoke test still passes in that case — the review degrades gracefully.

---

## Legacy .NET Audit Report

The legacy audit generates a high-level analysis of a repository — useful for onboarding a legacy .NET codebase, planning modernization, or assessing architectural risk.

### Run the audit

```text
POST /api/audit/legacy-dotnet
Content-Type: application/json

{
  "repoPath": "D:\\Projects\\SomeRepo",
  "useLlm": false,
  "includeRoslyn": true,
  "includeGpuSearch": true,
  "includeDotNetPresets": true,
  "includeDependencyInjection": true
}
```

Or use the **Legacy Audit** panel in the LegacyLens UI — expand it under the repo path field, configure options, and click **Run legacy audit**.

### Recommended demo targets

Try the audit against these classic legacy repositories:

| Repository | Why it's interesting |
|---|---|
| OrchardCMS/Orchard | ASP.NET MVC 5, `packages.config`, `App_Start`, .NET Framework |
| BlogEngine.NET | Classic .NET Framework blog engine with `web.config` and `Global.asax` |
| DNN Platform | WebForms-based portal, heavy `System.Web` usage |
| nopCommerce (< 4.0 tag) | .NET Framework e-commerce, large multi-project solution |

Clone the target, add its root to `AllowedRoots` in your `appsettings.Local.json`, and run the audit.

### What the report includes

- **Summary** — one-paragraph overview of detected signals and finding counts.
- **Workspace** — solution/project file discovery (`.slnx`, `.sln`, `.csproj` counts).
- **Technology signals** — framework, dependency, and configuration signals with confidence levels.
- **Architecture signals** — derived signals such as legacy framework, multi-project solution, interface-driven design.
- **Risk findings** — code-level and structural risks with severity, code, message, and evidence.
- **Roslyn summary** — compiler-aware project/document/symbol counts (when workspace loads).
- **Dependency injection summary** — DI registrations, constructor dependencies, and advisory findings.
- **gpu-search signal scan** — when gpu-search-mcp is running, LegacyLens calls `POST /scan/signals` to retrieve categorized signals (Framework, Data, Quality, Architecture) in one request. Automatically falls back to individual hybrid search queries when `/scan/signals` is unavailable (older gpu-search-mcp versions) and adds an `Info` finding `gpu-search-scan-fallback`.
- **Recommended next steps** — deterministic, derived from detected findings.
- **LLM summary** — optional. Requires `useLlm: true` and a configured local LLM provider.

### Enrichment sources and confidence

| Source | Confidence | Notes |
|---|---|---|
| File discovery | High | Direct evidence — file exists on disk |
| Roslyn workspace | High | Compiler-accurate when workspace loads |
| DI static analysis | Medium | Advisory — not runtime container verification |
| gpu-search signal scan | Medium | Heuristic/retrieval-based, not compiler-verified. Uses `/scan/signals` when available, falls back to individual queries. |

All enrichment sources are local and read-only. No files are modified.
