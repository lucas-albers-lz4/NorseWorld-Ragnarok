#!/usr/bin/env bash
# Verify save slot N has three non-empty files and scan Ragnarok.log for save/load errors.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SLOT="${1:-}"

if [[ -z "$SLOT" || ! "$SLOT" =~ ^[0-9]$ ]]; then
    echo "Usage: $0 <slot 0-9>" >&2
    exit 1
fi

fail=0
for ext in rgp rgt rgj; do
    path="$ROOT/save/rgame_${SLOT}.${ext}"
    if [[ ! -f "$path" ]]; then
        echo "MISSING: $path" >&2
        fail=1
    elif [[ ! -s "$path" ]]; then
        echo "EMPTY: $path" >&2
        fail=1
    else
        echo "OK: $path ($(stat -c%s "$path") bytes)"
    fi
done

LOG="$ROOT/Ragnarok.log"
if [[ -f "$LOG" ]]; then
    patterns='saveGame\.IO\(\)|saveGame\(\):|loadGame\(\):|loadGame\.io\(\)|terrainsLoad\(\): fail|playerLoad\(\): fail|Critical error'
    if grep -E "$patterns" "$LOG" >/dev/null 2>&1; then
        echo "LOG issues:" >&2
        grep -E "$patterns" "$LOG" | tail -10 >&2
        fail=1
    else
        echo "LOG: no save/load failure patterns"
    fi
else
    echo "LOG: Ragnarok.log not found (skip)"
fi

exit "$fail"
