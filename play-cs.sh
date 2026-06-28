#!/usr/bin/env bash
# Run the C# build (Mono) from repo root for A/B comparison with nwr-dist-v0.11.0-win/play.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GITROOT="$(cd "$ROOT/.." && pwd)"
PROJECT="$ROOT/project"
BUILD="$PROJECT/bin/Release"
LIBS_DIR="$GITROOT"

require_cmd() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Missing required command: $1" >&2
        exit 1
    fi
}

ensure_sdl_mixer() {
    if ! ldconfig -p 2>/dev/null | grep -q 'libSDL2_mixer-2.0.so.0'; then
        echo "Missing libSDL2_mixer-2.0.so.0 — install with:" >&2
        echo "  sudo apt install libsdl2-mixer-2.0-0" >&2
        exit 1
    fi
}

ensure_sibling_libs() {
    if [[ ! -f "$LIBS_DIR/BSLib/BSLib/BSLib.csproj" ]]; then
        echo "Cloning BSLib into $LIBS_DIR/BSLib ..."
        git clone --depth 1 https://github.com/Serg-Norseman/BSLib.git "$LIBS_DIR/BSLib"
    fi
    if [[ ! -f "$LIBS_DIR/ZRLib/ZRLib/ZRLib.csproj" ]]; then
        echo "Cloning ZRLib into $LIBS_DIR/ZRLib ..."
        git clone --depth 1 https://github.com/Serg-Norseman/ZRLib.git "$LIBS_DIR/ZRLib"
    fi
}

build_if_needed() {
    if [[ ! -f "$BUILD/NWR.exe" ]] || [[ "${NWR_FORCE_BUILD:-0}" == 1 ]]; then
        echo "Building C# Release ..."
        (cd "$PROJECT" && xbuild NWR.sln /p:Configuration=Release /verbosity:quiet)
    fi
}

stage_runtime() {
    cp -f "$BUILD/NWR.exe" "$BUILD/BSLib.dll" "$BUILD/ZRLib.dll" "$BUILD/Jint.dll" "$BUILD/NVorbis.dll" "$ROOT/" 2>/dev/null || \
    cp -f "$BUILD/NWR.exe" "$BUILD/BSLib.dll" "$BUILD/ZRLib.dll" "$ROOT/"
    cp -f "$BUILD/Jint.dll" "$ROOT/" 2>/dev/null || cp -f "$PROJECT/libs/Jint.dll" "$ROOT/"
    cp -f "$BUILD/NVorbis.dll" "$ROOT/" 2>/dev/null || cp -f "$PROJECT/libs/NVorbis.dll" "$ROOT/"
    cp -f "$BUILD"/*.dll "$ROOT/" 2>/dev/null || true
    cp -f "$BUILD/NWR.exe.config" "$ROOT/" 2>/dev/null || true
    cp -f "$LIBS_DIR/ZRLib/ZRLib/bin/Release/ZRLib.dll.config" "$ROOT/" 2>/dev/null || \
        cp -f "$ROOT/ZRLib.dll.config" "$ROOT/" 2>/dev/null || true

    # Optional: reuse audio assets from Java distribution if present
    if [[ -d "$ROOT/nwr-dist-v0.11.0-win/sfx" && ! -e "$ROOT/sfx" ]]; then
        ln -sf "$ROOT/nwr-dist-v0.11.0-win/sfx" "$ROOT/sfx"
    fi
    if [[ -d "$ROOT/nwr-dist-v0.11.0-win/songs" && ! -e "$ROOT/songs" ]]; then
        ln -sf "$ROOT/nwr-dist-v0.11.0-win/songs" "$ROOT/songs"
    fi

    if [[ ! -d "$ROOT/sfx" ]]; then
        echo "Note: sfx/ not found — game audio will be silent until you add sfx/ (and songs/) beside NWR.exe." >&2
    fi
}

main() {
    require_cmd mono
    require_cmd xbuild
    ensure_sdl_mixer
    ensure_sibling_libs
    build_if_needed
    stage_runtime
    cd "$ROOT"
    echo "Starting C# build (Mono). Log: $ROOT/Ragnarok.log"
    exec mono "$ROOT/NWR.exe" "$@"
}

main "$@"
