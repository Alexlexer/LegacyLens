<#
.SYNOPSIS
    Initializes the sample-dotnet-repo fixture and leaves a deterministic dirty diff
    so LegacyLens can review it.

.DESCRIPTION
    Idempotent: repeated runs reset the fixture repo to a known baseline commit and
    re-apply the same working-tree change. No global Git config is required — a local
    user.name and user.email are configured inside the fixture repo only.

.PARAMETER FixtureDir
    Path to the sample-dotnet-repo fixture directory.
    Defaults to test-fixtures/sample-dotnet-repo relative to the repository root.

.EXAMPLE
    .\scripts\prepare-sample-diff.ps1

.EXAMPLE
    .\scripts\prepare-sample-diff.ps1 -FixtureDir "C:\Work\LegacyLens\test-fixtures\sample-dotnet-repo"
#>

[CmdletBinding()]
param (
    [string]$FixtureDir = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $FixtureDir) {
    $scriptRoot  = Split-Path -Parent $PSScriptRoot
    $FixtureDir  = Join-Path $scriptRoot "test-fixtures\sample-dotnet-repo"
}

$FixtureDir = (Resolve-Path $FixtureDir).Path

Write-Host "Fixture directory : $FixtureDir"

# Helper: run git inside the fixture repo
function Invoke-Git {
    param([string[]]$Arguments)
    $result = & git -C $FixtureDir @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "git $($Arguments -join ' ') failed: $result"
    }
    return $result
}

# 1. Initialise git repo if needed
if (-not (Test-Path (Join-Path $FixtureDir ".git"))) {
    Write-Host "Initialising git repository..."
    Invoke-Git @("init", "-b", "main") | Out-Null
}

# 2. Set local identity (does not touch global git config)
Invoke-Git @("config", "user.name", "LegacyLens Test") | Out-Null
Invoke-Git @("config", "user.email", "test@legacylens.local") | Out-Null

# 3. Reset any previous dirty state so we get a clean baseline (only if commits exist)
$hasCommits = & git -C $FixtureDir log --oneline -1 2>&1
if ($LASTEXITCODE -eq 0 -and $hasCommits) {
    $status = Invoke-Git @("status", "--porcelain")
    if ($status) {
        Write-Host "Resetting working tree to HEAD..."
        Invoke-Git @("checkout", "--", ".") | Out-Null
        Invoke-Git @("clean", "-fd") | Out-Null
    }
}

# 4. Stage and commit all fixture source files as baseline (idempotent)
Invoke-Git @("add", "--all") | Out-Null

$hasCommits2 = & git -C $FixtureDir log --oneline -1 2>&1
if ($LASTEXITCODE -ne 0 -or -not $hasCommits2) {
    Write-Host "Creating baseline commit..."
    Invoke-Git @("commit", "-m", "chore: baseline sample-dotnet-repo fixture") | Out-Null
} else {
    $staged = Invoke-Git @("diff", "--cached", "--name-only")
    if ($staged) {
        Write-Host "Amending baseline commit with current fixture files..."
        Invoke-Git @("commit", "--amend", "--no-edit") | Out-Null
    } else {
        Write-Host "Baseline commit is up to date."
    }
}

# 5. Apply a deterministic working-tree change to UserService.cs
$userServicePath = Join-Path $FixtureDir "src\UserService.cs"

$patch = @"
namespace SampleApp;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _repository;

    // CHANGED: added logging for observability
    private readonly List<string> _auditLog = [];

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _auditLog.Add(`$"GetById:{id}");
        return await _repository.FindAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _auditLog.Add("GetAll");
        return await _repository.ListAsync(cancellationToken);
    }

    public async Task<User> CreateAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        _auditLog.Add(`$"Create:{name}");
        var user = new User(0, name, email, DateTime.UtcNow);
        return await _repository.InsertAsync(user, cancellationToken);
    }
}
"@

Set-Content -Path $userServicePath -Value $patch -Encoding UTF8 -NoNewline

Write-Host ""
Write-Host "Done. Fixture diff is ready." -ForegroundColor Green
Write-Host ""
Write-Host "Changed file : $userServicePath"
Write-Host ""
Write-Host "Verify with :"
Write-Host "  git -C `"$FixtureDir`" diff"
Write-Host ""
Write-Host "Pass to LegacyLens as :"
Write-Host "  { `"repoPath`": `"$FixtureDir`" }"
Write-Host ""
