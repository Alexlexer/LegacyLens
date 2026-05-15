<#
.SYNOPSIS
    Integration smoke test for LegacyLens + gpu-search-mcp.

.DESCRIPTION
    Assumes both services are already running. Checks health endpoints, calls
    review endpoints against the sample fixture repo, and verifies the responses
    contain expected content. Exits non-zero on any failure.

.PARAMETER LegacyLensBaseUrl
    Base URL of the LegacyLens API. Default: http://127.0.0.1:5096

.PARAMETER GpuSearchBaseUrl
    Base URL of the gpu-search-mcp service. Default: http://127.0.0.1:8765

.PARAMETER RepoPath
    Absolute path of the repository to review. Defaults to test-fixtures/sample-dotnet-repo
    relative to the LegacyLens repository root.

.EXAMPLE
    .\scripts\integration-smoke.ps1

.EXAMPLE
    .\scripts\integration-smoke.ps1 -LegacyLensBaseUrl "http://127.0.0.1:5096" -RepoPath "D:\Projects\LegacyLens\test-fixtures\sample-dotnet-repo"
#>

[CmdletBinding()]
param (
    [string]$LegacyLensBaseUrl = "http://127.0.0.1:5096",
    [string]$GpuSearchBaseUrl  = "http://127.0.0.1:8765",
    [string]$RepoPath          = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $RepoPath) {
    $scriptRoot = Split-Path -Parent $PSScriptRoot
    $RepoPath   = Join-Path $scriptRoot "test-fixtures\sample-dotnet-repo"
}
$RepoPath = (Resolve-Path $RepoPath).Path

$pass = 0
$fail = 0

function Write-Pass([string]$label) {
    Write-Host ("  [PASS] " + $label) -ForegroundColor Green
    $script:pass++
}

function Write-Fail([string]$label, [string]$detail = "") {
    Write-Host ("  [FAIL] " + $label) -ForegroundColor Red
    if ($detail) { Write-Host "         $detail" -ForegroundColor DarkRed }
    $script:fail++
}

function Invoke-Check {
    param(
        [string]$Label,
        [string]$Uri,
        [string]$Method = "GET",
        [hashtable]$Body = $null,
        [scriptblock]$Validate = $null
    )

    try {
        $params = @{
            Uri        = $Uri
            Method     = $Method
            TimeoutSec = 30
        }
        if ($Body) {
            $params.Body        = ($Body | ConvertTo-Json -Depth 5)
            $params.ContentType = "application/json"
        }

        $response = Invoke-RestMethod @params

        if ($Validate) {
            $validationError = & $Validate $response
            if ($validationError) {
                Write-Fail $Label $validationError
                return $null
            }
        }

        Write-Pass $Label
        return $response
    }
    catch {
        Write-Fail $Label ($_.ToString())
        return $null
    }
}

# ─────────────────────────────────────────────
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  LegacyLens Integration Smoke Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  LegacyLens : $LegacyLensBaseUrl"
Write-Host "  gpu-search  : $GpuSearchBaseUrl"
Write-Host "  RepoPath    : $RepoPath"
Write-Host ""

# ─────────────────────────────────────────────
Write-Host "--- Health checks ---"

Invoke-Check -Label "gpu-search /health" -Uri "$GpuSearchBaseUrl/health" `
    -Validate { param($r)
        if (-not $r) { return "Empty response" }
        return $null
    }

Invoke-Check -Label "LegacyLens /health" -Uri "$LegacyLensBaseUrl/health" `
    -Validate { param($r)
        if ($r.status -ne "ok") { return "Expected status=ok, got: $($r.status)" }
        if ($r.service -ne "LegacyLens") { return "Expected service=LegacyLens, got: $($r.service)" }
        return $null
    }

# ─────────────────────────────────────────────
Write-Host ""
Write-Host "--- Search status ---"

$searchStatus = Invoke-Check -Label "GET /api/search/status" -Uri "$LegacyLensBaseUrl/api/search/status" `
    -Validate { param($r)
        if ($null -eq $r.isAvailable) { return "Missing isAvailable field" }
        return $null
    }

if ($searchStatus) {
    $gpuAvailable = $searchStatus.isAvailable
    Write-Host "         gpu-search available: $gpuAvailable" -ForegroundColor DarkGray
}

# ─────────────────────────────────────────────
Write-Host ""
Write-Host "--- Diff preview ---"

$previewBody = @{ repoPath = $RepoPath }
$preview = Invoke-Check -Label "POST /api/review/diff/preview" `
    -Uri    "$LegacyLensBaseUrl/api/review/diff/preview" `
    -Method "POST" `
    -Body   $previewBody `
    -Validate { param($r)
        if ($null -eq $r.changedFileCount) { return "Missing changedFileCount" }
        if ($r.changedFileCount -lt 1)     { return "Expected at least 1 changed file; got $($r.changedFileCount). Run prepare-sample-diff.ps1 first." }
        if (-not $r.files)                 { return "Missing files array" }
        return $null
    }

if ($preview) {
    Write-Host "         Changed files: $($preview.changedFileCount)" -ForegroundColor DarkGray
    $preview.files | ForEach-Object {
        Write-Host "           $($_.status) $($_.path) (+$($_.additions)/-$($_.deletions))" -ForegroundColor DarkGray
    }
}

# ─────────────────────────────────────────────
Write-Host ""
Write-Host "--- Diff review (deterministic) ---"

$reviewBody = @{ repoPath = $RepoPath; useLlm = $false }
$review = Invoke-Check -Label "POST /api/review/diff" `
    -Uri    "$LegacyLensBaseUrl/api/review/diff" `
    -Method "POST" `
    -Body   $reviewBody `
    -Validate { param($r)
        if (-not $r.reportId)         { return "Missing reportId" }
        if (-not $r.markdown)         { return "Missing markdown" }
        if ($r.markdown -notmatch "LegacyLens Diff Review") {
            return "Markdown header 'LegacyLens Diff Review' not found"
        }
        if ($r.changedFileCount -lt 1) { return "Expected changedFileCount >= 1" }
        return $null
    }

if ($review) {
    Write-Host "         Report ID     : $($review.reportId)" -ForegroundColor DarkGray
    Write-Host "         Changed files : $($review.changedFileCount)" -ForegroundColor DarkGray
    Write-Host "         LLM provider  : $($review.llmProvider)" -ForegroundColor DarkGray

    # Check for gpu-search context or graceful fallback
    if ($review.markdown -match "gpu-search Context") {
        Write-Pass "gpu-search context section present in report"
    } elseif ($review.markdown -match "gpu-search-unavailable") {
        Write-Pass "gpu-search graceful fallback finding present in report"
    } else {
        Write-Fail "Neither gpu-search context nor fallback finding found in report"
    }
}

# ─────────────────────────────────────────────
Write-Host ""
Write-Host "--- Saved reports ---"

Invoke-Check -Label "GET /api/reports" -Uri "$LegacyLensBaseUrl/api/reports" `
    -Validate { param($r)
        if ($null -eq $r) { return "Null response" }
        return $null
    }

# ─────────────────────────────────────────────
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
$color = if ($fail -gt 0) { "Red" } else { "Green" }
Write-Host "  Results: $pass passed, $fail failed" -ForegroundColor $color
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($fail -gt 0) { exit 1 }
