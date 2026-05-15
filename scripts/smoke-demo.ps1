<#
.SYNOPSIS
    Runs smoke checks against a running LegacyLens + gpu-search-mcp demo session.

.PARAMETER RepoPath
    Repository path to use for optional diff preview/review checks.
    If omitted, only health and status checks run.

.PARAMETER GpuSearchPort
    Port for gpu-search-mcp HTTP mode. Default: 8765.

.PARAMETER LegacyLensPort
    Port for the LegacyLens API. Default: 5096.

.EXAMPLE
    .\scripts\smoke-demo.ps1 -RepoPath "D:\Projects\MyRepo"
#>

[CmdletBinding()]
param (
    [string]$RepoPath = "",
    [int]$GpuSearchPort = 8765,
    [int]$LegacyLensPort = 5096
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$gpuBaseUrl  = "http://127.0.0.1:$GpuSearchPort"
$apiBaseUrl  = "http://127.0.0.1:$LegacyLensPort"
$pass        = 0
$fail        = 0

function Test-Endpoint {
    param(
        [string]$Label,
        [string]$Uri,
        [string]$Method = "GET",
        [object]$Body = $null
    )

    Write-Host "  [$Method] $Uri ... " -NoNewline
    try {
        $params = @{ Uri = $Uri; Method = $Method; TimeoutSec = 10 }
        if ($Body) {
            $params.Body        = ($Body | ConvertTo-Json -Depth 5)
            $params.ContentType = "application/json"
        }
        $response = Invoke-RestMethod @params
        Write-Host "OK" -ForegroundColor Green
        $script:pass++
        return $response
    }
    catch {
        Write-Host "FAIL — $_" -ForegroundColor Red
        $script:fail++
        return $null
    }
}

Write-Host ""
Write-Host "=============================" -ForegroundColor Cyan
Write-Host "  LegacyLens Smoke Checks" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

Write-Host "--- gpu-search-mcp ---"
Test-Endpoint -Label "gpu-search health" -Uri "$gpuBaseUrl/health"

Write-Host ""
Write-Host "--- LegacyLens API ---"
Test-Endpoint -Label "LegacyLens health"  -Uri "$apiBaseUrl/health"
Test-Endpoint -Label "gpu-search status"  -Uri "$apiBaseUrl/api/search/status"
Test-Endpoint -Label "saved reports list" -Uri "$apiBaseUrl/api/reports"

if ($RepoPath) {
    Write-Host ""
    Write-Host "--- Diff checks (RepoPath: $RepoPath) ---"
    Test-Endpoint -Label "diff preview" `
        -Uri "$apiBaseUrl/api/review/diff/preview" `
        -Method "POST" `
        -Body @{ repoPath = $RepoPath }

    Test-Endpoint -Label "diff review (deterministic)" `
        -Uri "$apiBaseUrl/api/review/diff" `
        -Method "POST" `
        -Body @{ repoPath = $RepoPath; useLlm = $false }
}

Write-Host ""
Write-Host "=============================" -ForegroundColor Cyan
Write-Host "  Results: $pass passed, $fail failed" -ForegroundColor $(if ($fail -gt 0) { "Red" } else { "Green" })
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

if ($fail -gt 0) { exit 1 }
