#!/usr/bin/env bash
# prepare-sample-diff.sh
# Initialises the sample-dotnet-repo fixture and leaves a deterministic dirty diff
# so LegacyLens can review it.
#
# Idempotent: repeated runs reset the fixture repo to a known baseline commit and
# re-apply the same working-tree change.
#
# Usage:
#   ./scripts/prepare-sample-diff.sh [FixtureDir]
#
# Arguments:
#   FixtureDir  Path to the fixture directory.
#               Defaults to test-fixtures/sample-dotnet-repo relative to the repo root.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
FIXTURE_DIR="${1:-$REPO_ROOT/test-fixtures/sample-dotnet-repo}"
FIXTURE_DIR="$(cd "$FIXTURE_DIR" && pwd)"

echo "Fixture directory : $FIXTURE_DIR"

git_fixture() {
    git -C "$FIXTURE_DIR" "$@"
}

# 1. Initialise git repo if needed
if [[ ! -d "$FIXTURE_DIR/.git" ]]; then
    echo "Initialising git repository..."
    git_fixture init -b main
fi

# 2. Set local identity (does not touch global git config)
git_fixture config user.name  "LegacyLens Test"
git_fixture config user.email "test@legacylens.local"

# 3. Reset any previous dirty state
if [[ -n "$(git_fixture status --porcelain)" ]]; then
    echo "Resetting working tree to HEAD..."
    git_fixture checkout -- . 2>/dev/null || true
    git_fixture clean -fd
fi

# 4. Stage and commit all fixture source files as baseline (idempotent)
git_fixture add --all

if ! git_fixture log --oneline -1 &>/dev/null; then
    echo "Creating baseline commit..."
    git_fixture commit -m "chore: baseline sample-dotnet-repo fixture"
else
    staged="$(git_fixture diff --cached --name-only)"
    if [[ -n "$staged" ]]; then
        echo "Amending baseline commit with current fixture files..."
        git_fixture commit --amend --no-edit
    else
        echo "Baseline commit is up to date."
    fi
fi

# 5. Apply a deterministic working-tree change to UserService.cs
USER_SERVICE="$FIXTURE_DIR/src/UserService.cs"

cat > "$USER_SERVICE" << 'CSHARP'
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
        _auditLog.Add($"GetById:{id}");
        return await _repository.FindAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _auditLog.Add("GetAll");
        return await _repository.ListAsync(cancellationToken);
    }

    public async Task<User> CreateAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        _auditLog.Add($"Create:{name}");
        var user = new User(0, name, email, DateTime.UtcNow);
        return await _repository.InsertAsync(user, cancellationToken);
    }
}
CSHARP

echo ""
echo "Done. Fixture diff is ready."
echo ""
echo "Changed file : $USER_SERVICE"
echo ""
echo "Verify with :"
echo "  git -C \"$FIXTURE_DIR\" diff"
echo ""
echo "Pass to LegacyLens as :"
echo "  { \"repoPath\": \"$FIXTURE_DIR\" }"
echo ""
