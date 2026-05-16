#!/usr/bin/env bash
# start-demo.sh
# Prints the exact commands to start a local LegacyLens + gpu-search-mcp demo session.
# Nothing is started automatically — run each command block in a separate terminal.
#
# Usage:
#   ./scripts/start-demo.sh <RepoPath> [GpuSearchPort] [LegacyLensPort]
#
# Arguments:
#   RepoPath        Absolute path to the Git repository to analyse (required)
#   GpuSearchPort   Port for gpu-search-mcp HTTP mode (default: 8765)
#   LegacyLensPort  Port for the LegacyLens API (default: 5096)
#
# Examples:
#   ./scripts/start-demo.sh /Users/you/Projects/MyRepo
#   ./scripts/start-demo.sh /Users/you/Projects/MyRepo 8766 5097

set -euo pipefail

REPO_PATH="${1:-}"
GPU_SEARCH_PORT="${2:-8765}"
LEGACYLENS_PORT="${3:-5096}"

if [[ -z "$REPO_PATH" ]]; then
    echo "Usage: $0 <RepoPath> [GpuSearchPort] [LegacyLensPort]" >&2
    exit 1
fi

if [[ ! -d "$REPO_PATH" ]]; then
    echo "Error: RepoPath '$REPO_PATH' does not exist or is not a directory." >&2
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
LegacyLens_DIR="$REPO_ROOT/LegacyLens"

GPU_HEALTH_URL="http://127.0.0.1:$GPU_SEARCH_PORT/health"
LEGACYLENS_URL="http://127.0.0.1:$LEGACYLENS_PORT"
LEGACYLENS_HEALTH="$LEGACYLENS_URL/health"
STATUS_URL="$LEGACYLENS_URL/api/search/status"

echo ""
echo "========================================="
echo "  LegacyLens + gpu-search-mcp Demo Setup"
echo "========================================="
echo ""
echo "Repository : $REPO_PATH"
echo "gpu-search : $GPU_HEALTH_URL"
echo "LegacyLens : $LEGACYLENS_HEALTH"
echo ""

echo "-----------------------------------------"
echo " TERMINAL 1 — Start gpu-search-mcp"
echo "-----------------------------------------"
echo ""
echo "  gpu-search-mcp --directory \"$REPO_PATH\" --http --port $GPU_SEARCH_PORT"
echo ""
echo "  Wait until you see: Uvicorn running on http://127.0.0.1:$GPU_SEARCH_PORT"
echo ""

echo "-----------------------------------------"
echo " TERMINAL 2 — Start LegacyLens API"
echo "-----------------------------------------"
echo ""
echo "  cd \"$LegacyLens_DIR\""
echo "  dotnet run --project src/LegacyLens.Api --urls \"http://127.0.0.1:$LEGACYLENS_PORT\""
echo ""
echo "  Wait until you see: Now listening on: http://127.0.0.1:$LEGACYLENS_PORT"
echo ""

echo "-----------------------------------------"
echo " TERMINAL 3 — Smoke checks (optional)"
echo "-----------------------------------------"
echo ""
echo "  curl -s '$GPU_HEALTH_URL' | python3 -m json.tool"
echo "  curl -s '$LEGACYLENS_HEALTH' | python3 -m json.tool"
echo "  curl -s '$STATUS_URL' | python3 -m json.tool"
echo ""
echo "  # Or run the smoke script:"
echo "  ./scripts/smoke-demo.sh \"$REPO_PATH\" $GPU_SEARCH_PORT $LEGACYLENS_PORT"
echo ""

echo "-----------------------------------------"
echo " BROWSER — Open the UI"
echo "-----------------------------------------"
echo ""
echo "  $LEGACYLENS_URL"
echo ""
echo "  Enter repo path : $REPO_PATH"
echo "  Then click      : Preview diff, Run review, .NET analysis"
echo ""
echo "  See docs/demo.md for full walkthrough and troubleshooting."
echo ""
