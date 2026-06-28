#!/usr/bin/env bash
# Build Tier C minimal playable sfx/ tree at repo root (symlinks by default).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEV_INFO="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

require_cmd() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Missing required command: $1" >&2
        exit 1
    fi
}

ensure_masters() {
    local src="$DEV_INFO/sfx-src"
    if [[ ! -f "$src/song_menu.ogg" ]]; then
        if [[ -x "$src/generate-masters.sh" ]] && command -v ffmpeg >/dev/null 2>&1; then
            echo "Master clips not found; running generate-masters.sh ..."
            "$src/generate-masters.sh"
        else
            echo "Master clips missing in $src — run dev_info/sfx-src/generate-masters.sh (needs ffmpeg)." >&2
            exit 1
        fi
    fi
}

main() {
    require_cmd python3
    ensure_masters
    exec python3 "$DEV_INFO/sfx_pack.py" "$@"
}

main "$@"
