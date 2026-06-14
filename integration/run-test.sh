#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
KEY_FILE="$SCRIPT_DIR/key.txt"
SCENARIOS_FILE="$SCRIPT_DIR/scenarios.json"

# Configuration
ENDPOINT="${EVAL_ENDPOINT:-https://opencode.ai/zen/go/v1}"
MODEL="${EVAL_MODEL:-deepseek-v4-flash}"

# Read API key
if [ ! -f "$KEY_FILE" ]; then
    echo "ERROR: key.txt not found at $KEY_FILE"
    echo "Create integration/key.txt with your API key (single line)."
    exit 1
fi
API_KEY=$(head -1 "$KEY_FILE" | tr -d '\r\n')

# Build eval-cli if needed
if [ ! -f "$PROJECT_DIR/src/AiEvalCli/bin/Release/net10.0/win-x64/eval-cli.dll" ]; then
    echo "Building eval-cli..."
    dotnet build "$PROJECT_DIR/src/AiEvalCli" -c Release --nologo -v q
fi

# Run evaluation
echo "========================================="
echo "  eval-cli Integration Test"
echo "========================================="
echo "  Endpoint : $ENDPOINT"
echo "  Model    : $MODEL"
echo "  Scenarios: $(jq length "$SCENARIOS_FILE")"
echo "========================================="
echo ""

dotnet run --project "$PROJECT_DIR/src/AiEvalCli" -c Release --no-build -- \
    --provider openai \
    --endpoint "$ENDPOINT" \
    --model "$MODEL" \
    --api-key "$API_KEY" \
    --input "$SCENARIOS_FILE" \
    --evaluators relevance,coherence,fluency \
    --parallel 3

echo ""
echo "========================================="
echo "  Test complete"
echo "========================================="
