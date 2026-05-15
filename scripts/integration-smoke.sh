#!/usr/bin/env bash
# integration-smoke.sh
# Integration smoke test for LegacyLens + gpu-search-mcp.
# Assumes both services are already running.
# Exits non-zero on any failure.
#
# Usage:
#   ./scripts/integration-smoke.sh [LegacyLensBaseUrl] [GpuSearchBaseUrl] [RepoPath]
#
# Defaults:
#   LegacyLensBaseUrl  http://127.0.0.1:5096
#   GpuSearchBaseUrl   http://127.0.0.1:8765
#   RepoPath           test-fixtures/sample-dotnet-repo (relative to repo root)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

LEGACYLENS_BASE="${1:-http://127.0.0.1:5096}"
GPUSEARCH_BASE="${2:-http://127.0.0.1:8765}"
REPO_PATH="${3:-$REPO_ROOT/test-fixtures/sample-dotnet-repo}"
REPO_PATH="$(cd "$REPO_PATH" && pwd)"

pass=0
fail=0

green='\033[0;32m'
red='\033[0;31m'
gray='\033[0;37m'
cyan='\033[0;36m'
reset='\033[0m'

check_pass() { echo -e "  ${green}[PASS]${reset} $1"; ((pass++)); }
check_fail() { echo -e "  ${red}[FAIL]${reset} $1${2:+ — $2}"; ((fail++)); }

get_json() {
    curl -sf --max-time 30 "$1" 2>&1 || true
}

post_json() {
    curl -sf --max-time 60 -X POST -H "Content-Type: application/json" -d "$2" "$1" 2>&1 || true
}

echo ""
echo -e "${cyan}============================================${reset}"
echo -e "${cyan}  LegacyLens Integration Smoke Test${reset}"
echo -e "${cyan}============================================${reset}"
echo ""
echo "  LegacyLens : $LEGACYLENS_BASE"
echo "  gpu-search  : $GPUSEARCH_BASE"
echo "  RepoPath    : $REPO_PATH"
echo ""

# ── Health checks ─────────────────────────────
echo "--- Health checks ---"

gpu_health=$(get_json "$GPUSEARCH_BASE/health")
if [[ -n "$gpu_health" ]]; then
    check_pass "gpu-search /health"
else
    check_fail "gpu-search /health" "Empty or no response"
fi

ll_health=$(get_json "$LEGACYLENS_BASE/health")
if echo "$ll_health" | python3 -c "import sys,json; d=json.load(sys.stdin); assert d['status']=='ok' and d['service']=='LegacyLens'" 2>/dev/null; then
    check_pass "LegacyLens /health"
else
    check_fail "LegacyLens /health" "$ll_health"
fi

# ── Search status ──────────────────────────────
echo ""
echo "--- Search status ---"

status_resp=$(get_json "$LEGACYLENS_BASE/api/search/status")
if echo "$status_resp" | python3 -c "import sys,json; d=json.load(sys.stdin); assert 'isAvailable' in d" 2>/dev/null; then
    check_pass "GET /api/search/status"
    gpu_avail=$(echo "$status_resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['isAvailable'])" 2>/dev/null || echo "unknown")
    echo -e "         ${gray}gpu-search available: $gpu_avail${reset}"
else
    check_fail "GET /api/search/status" "$status_resp"
fi

# ── Diff preview ───────────────────────────────
echo ""
echo "--- Diff preview ---"

preview_body="{\"repoPath\":\"$REPO_PATH\"}"
preview_resp=$(post_json "$LEGACYLENS_BASE/api/review/diff/preview" "$preview_body")

if echo "$preview_resp" | python3 -c "
import sys, json
d = json.load(sys.stdin)
assert 'changedFileCount' in d
count = d['changedFileCount']
assert count >= 1, f'Expected >=1 changed file, got {count}. Run prepare-sample-diff.sh first.'
" 2>/dev/null; then
    check_pass "POST /api/review/diff/preview"
    changed=$(echo "$preview_resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['changedFileCount'])" 2>/dev/null || echo "?")
    echo -e "         ${gray}Changed files: $changed${reset}"
else
    check_fail "POST /api/review/diff/preview" "$preview_resp"
fi

# ── Diff review ────────────────────────────────
echo ""
echo "--- Diff review (deterministic) ---"

review_body="{\"repoPath\":\"$REPO_PATH\",\"useLlm\":false}"
review_resp=$(post_json "$LEGACYLENS_BASE/api/review/diff" "$review_body")

if echo "$review_resp" | python3 -c "
import sys, json
d = json.load(sys.stdin)
assert d.get('reportId'), 'Missing reportId'
assert d.get('markdown'), 'Missing markdown'
assert 'LegacyLens Diff Review' in d['markdown'], 'Header not found'
assert d.get('changedFileCount', 0) >= 1, 'Expected changedFileCount >= 1'
" 2>/dev/null; then
    check_pass "POST /api/review/diff"
    report_id=$(echo "$review_resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['reportId'])" 2>/dev/null || echo "?")
    echo -e "         ${gray}Report ID: $report_id${reset}"

    # Check gpu-search context or fallback
    markdown=$(echo "$review_resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('markdown',''))" 2>/dev/null || echo "")
    if echo "$markdown" | grep -q "gpu-search Context"; then
        check_pass "gpu-search context section present in report"
    elif echo "$markdown" | grep -q "gpu-search-unavailable"; then
        check_pass "gpu-search graceful fallback finding present in report"
    else
        check_fail "Neither gpu-search context nor fallback finding found in report"
    fi
else
    check_fail "POST /api/review/diff" "$review_resp"
fi

# ── Saved reports ──────────────────────────────
echo ""
echo "--- Saved reports ---"

reports_resp=$(get_json "$LEGACYLENS_BASE/api/reports")
if [[ -n "$reports_resp" ]]; then
    check_pass "GET /api/reports"
else
    check_fail "GET /api/reports" "Empty response"
fi

# ── Summary ────────────────────────────────────
echo ""
echo -e "${cyan}============================================${reset}"
if [[ $fail -gt 0 ]]; then
    echo -e "${red}  Results: $pass passed, $fail failed${reset}"
else
    echo -e "${green}  Results: $pass passed, $fail failed${reset}"
fi
echo -e "${cyan}============================================${reset}"
echo ""

[[ $fail -eq 0 ]]
