#!/usr/bin/env bash
# Package failure context for optional local LLM triage (advisory only; never gates CI).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCENARIO="${1:-unknown}"
OUT_DIR="$ROOT/dev_info/triage"
STAMP="$(date +%Y%m%d-%H%M%S)"
BUNDLE="$OUT_DIR/${STAMP}-${SCENARIO}.md"

mkdir -p "$OUT_DIR"

{
    echo "# Test failure triage: ${SCENARIO}"
    echo
    echo "Generated: $(date -Iseconds)"
    echo "Branch: $(git -C "$ROOT" rev-parse --abbrev-ref HEAD 2>/dev/null || echo unknown)"
    echo
    echo "## Scenario"
    echo "${SCENARIO}"
    echo
    echo "## harness.log (last 80 lines)"
    echo '```'
    if [[ -f "$ROOT/harness.log" ]]; then
        tail -80 "$ROOT/harness.log"
    else
        echo "(no harness.log)"
    fi
    echo '```'
    echo
    echo "## Ragnarok.log (last 40 lines)"
    echo '```'
    if [[ -f "$ROOT/Ragnarok.log" ]]; then
        tail -40 "$ROOT/Ragnarok.log"
    else
        echo "(no Ragnarok.log)"
    fi
    echo '```'
    echo
    echo "## git diff (last 50 lines)"
    echo '```'
    git -C "$ROOT" diff 2>/dev/null | tail -50 || echo "(not a git repo)"
    echo '```'
    echo
    echo "## References"
    echo "- dev_info/save-load-test.txt"
    echo "- dev_info/test-harness.txt"
} > "$BUNDLE"

echo "Wrote $BUNDLE"
echo "Paste into local LLM or: ollama run MODEL < $BUNDLE"
exit 0
