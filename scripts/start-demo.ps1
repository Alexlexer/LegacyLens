<#
.SYNOPSIS
    Prints the exact commands to start a local LegacyLens + gpu-search-mcp demo session.

.DESCRIPTION
    Opens two clearly labelled terminal command blocks — one for gpu-search-mcp and one
    for LegacyLens — so each can be run in a separate terminal window.
    Nothing is started automatically; this avoids background-process juggling and keeps
    the demo reliable on any Windows machine.

.PARAMETER RepoPath
    Absolute path to the Git repository you want to analyse.

.PARAMETER GpuSearchPath
    Optional path to the gpu-search-mcp executable. Defaults to "gpu-search-mcp"
    (assumes it is on PATH).

.PARAMETER GpuSearchPort
    Port for gpu-search-mcp HTTP mode. Default: 8765.

.PARAMETER LegacyLensPort
    Port for the LegacyLens API. Default: 5096 (matches launchSettings.json).

.EXAMPLE
    .\scripts\start-demo.ps1 -RepoPath "D:\Projects\MyRepo"

.EXAMPLE
    .\scripts\start-demo.ps1 -RepoPath "D:\Projects\MyRepo" -GpuSearchPort 8766 -LegacyLensPort 5097
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$RepoPath,

    [string]$GpuSearchPath = "gpu-search-mcp",

    [int]$GpuSearchPort = 8765,

    [int]$LegacyLensPort = 5096
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Validate repo path exists
if (-not (Test-Path $RepoPath -PathType Container)) {
    Write-Error "RepoPath '$RepoPath' does not exist or is not a directory."
    exit 1
}

$gpuHealthUrl    = "http://127.0.0.1:$GpuSearchPort/health"
$legacyLensUrl   = "http://127.0.0.1:$LegacyLensPort"
$legacyLensHealth = "$legacyLensUrl/health"
$statusUrl       = "$legacyLensUrl/api/search/status"
$scriptDir       = Split-Path -Parent $PSScriptRoot
$LegacyLensDir = Join-Path $scriptDir "LegacyLens"

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  LegacyLens + gpu-search-mcp Demo Setup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Repository : $RepoPath"
Write-Host "gpu-search : $gpuHealthUrl"
Write-Host "LegacyLens : $legacyLensHealth"
Write-Host ""

Write-Host "-----------------------------------------" -ForegroundColor Yellow
Write-Host " TERMINAL 1 — Start gpu-search-mcp" -ForegroundColor Yellow
Write-Host "-----------------------------------------" -ForegroundColor Yellow
Write-Host ""
Write-Host "  $GpuSearchPath --directory `"$RepoPath`" --http --port $GpuSearchPort" -ForegroundColor White
Write-Host ""
Write-Host "  Wait until you see: Uvicorn running on http://127.0.0.1:$GpuSearchPort" -ForegroundColor DarkGray
Write-Host ""

Write-Host "-----------------------------------------" -ForegroundColor Yellow
Write-Host " TERMINAL 2 — Start LegacyLens API" -ForegroundColor Yellow
Write-Host "-----------------------------------------" -ForegroundColor Yellow
Write-Host ""
Write-Host "  cd `"$LegacyLensDir`"" -ForegroundColor White
Write-Host "  dotnet run --project src/LegacyLens.Api --urls `"http://127.0.0.1:$LegacyLensPort`"" -ForegroundColor White
Write-Host ""
Write-Host "  Wait until you see: Now listening on: http://127.0.0.1:$LegacyLensPort" -ForegroundColor DarkGray
Write-Host ""

Write-Host "-----------------------------------------" -ForegroundColor Yellow
Write-Host " TERMINAL 3 — Smoke checks (optional)" -ForegroundColor Yellow
Write-Host "-----------------------------------------" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Invoke-RestMethod '$gpuHealthUrl'" -ForegroundColor White
Write-Host "  Invoke-RestMethod '$legacyLensHealth'" -ForegroundColor White
Write-Host "  Invoke-RestMethod '$statusUrl'" -ForegroundColor White
Write-Host ""
Write-Host "  # Or run the smoke script:" -ForegroundColor DarkGray
Write-Host "  .\scripts\smoke-demo.ps1 -RepoPath `"$RepoPath`" -GpuSearchPort $GpuSearchPort -LegacyLensPort $LegacyLensPort" -ForegroundColor White
Write-Host ""

Write-Host "-----------------------------------------" -ForegroundColor Yellow
Write-Host " BROWSER — Open the UI" -ForegroundColor Yellow
Write-Host "-----------------------------------------" -ForegroundColor Yellow
Write-Host ""
Write-Host "  $legacyLensUrl" -ForegroundColor White
Write-Host ""
Write-Host "  Enter repo path : $RepoPath" -ForegroundColor DarkGray
Write-Host "  Then click      : Preview diff, Run review, .NET analysis" -ForegroundColor DarkGray
Write-Host ""

Write-Host "  See docs/demo.md for full walkthrough and troubleshooting." -ForegroundColor DarkGray
Write-Host ""
