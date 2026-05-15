# Local Demo Walkthrough

This guide covers running LegacyLens together with `gpu-search-mcp` on a local machine for a full end-to-end demo.

> **Read-only.** LegacyLens never modifies the repository being analysed.

## Prerequisites

| Requirement | Notes |
|---|---|
| .NET 10 SDK | Run `dotnet --version` to verify |
| `gpu-search-mcp` | Install and confirm it is on PATH or note its full path |
| A Git repository with at least one committed change | Used as the analysis target |

Optional: LM Studio running locally for LLM-enhanced summaries.

---

## Step 1 — Configure allowed roots

LegacyLens rejects repository paths that are not under a configured allowed root.

Copy the example config and edit it:

```bash
cp refactorguard/appsettings.Local.example.json refactorguard/appsettings.Local.json
```

Edit `refactorguard/appsettings.Local.json`:

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
cd refactorguard
dotnet run --project src/RefactorGuard.Api
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

- Deterministic findings (empty diff, large change, test changes, config changes)
- gpu-search context: dependency impact, related code, and file skeletons for each changed file
- An `Info` finding if gpu-search-mcp was not reachable

The report is saved to SQLite and appears in the **Saved reports** panel.

To include an LM Studio summary, tick **Include LM Studio summary** before clicking **Run review**.

### .NET analysis

Click **.NET analysis**. LegacyLens runs all built-in preset searches through `gpu-search-mcp`:

- `async-blocking` — `.Result` / `.Wait()` on async calls
- `broad-exceptions` — `catch (Exception)` without re-throw
- `entity-framework-n-plus-one` — N+1 query patterns
- `nullable-suppression` — `!` null-forgiving operator

Findings include file path, line number, and a code snippet.

### Saved reports

The **Saved reports** panel lists all generated reports. Click **View** to reload a report or **Delete** to remove it.

---

## Step 7 — Find saved reports

Reports are stored in SQLite at the path configured in `Persistence:DatabasePath` (default: `data/legacylens.db` relative to the API working directory, i.e. `refactorguard/data/legacylens.db`).

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
