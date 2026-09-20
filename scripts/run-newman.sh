#!/bin/bash
# run-newman.sh — Run Postman collection via Newman
#
# Usage:
#   ./scripts/run-newman.sh [--bail] [--verbose]
#
# Prerequisites:
#   - Node.js (v16+)
#   - API running at http://localhost:5287
#
# Options:
#   --bail     Stop on first failure
#   --verbose  Show verbose output
#
# Docs: docs/qa/2026-09-20__qa__guide__poc-test-suite.md

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COLLECTION="$REPO_ROOT/postman/Dealoware-PoC-Negotiations.postman_collection.json"
BASE_URL="${BASE_URL:-http://localhost:5287}"

BAIL_FLAG=""
VERBOSE_FLAG=""

for arg in "$@"; do
    case $arg in
        --bail)
            BAIL_FLAG="--bail"
            ;;
        --verbose)
            VERBOSE_FLAG="-v"
            ;;
    esac
done

echo "=== Dealoware PoC Newman Test Runner ==="
echo "Base URL: $BASE_URL"
echo "Collection: $COLLECTION"
echo

# Check if API is running
echo "Checking API health..."
if ! curl -sf "$BASE_URL/health" > /dev/null 2>&1; then
    echo "ERROR: API not responding at $BASE_URL/health"
    echo "Start the API first: dotnet run --project src/Dealoware.Api"
    exit 1
fi
echo "API is healthy."
echo

# Check if newman is available
if ! command -v npx &> /dev/null; then
    echo "ERROR: npx not found. Please install Node.js (v16+)."
    exit 1
fi

# Install newman locally if needed
if [ ! -d "$REPO_ROOT/node_modules/newman" ]; then
    echo "Installing newman locally..."
    cd "$REPO_ROOT"
    npm install newman newman-reporter-htmlextra --save-dev
fi

# Run Newman
echo "Running Postman collection..."
echo

cd "$REPO_ROOT"
npx newman run "$COLLECTION" \
    --env-var "baseUrl=$BASE_URL" \
    --reporters cli,json \
    --reporter-json-export "$REPO_ROOT/test-results/newman-results.json" \
    $BAIL_FLAG \
    $VERBOSE_FLAG

NEWMAN_EXIT=$?

if [ $NEWMAN_EXIT -eq 0 ]; then
    echo
    echo "=== Newman tests PASSED ==="
else
    echo
    echo "=== Newman tests FAILED (exit code: $NEWMAN_EXIT) ==="
fi

exit $NEWMAN_EXIT
