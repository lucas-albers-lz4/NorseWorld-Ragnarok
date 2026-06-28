#!/usr/bin/env bash
# Build and run Tier B (NUnit) + Tier C (headless integration scenarios).
# No SDL, no sound — safe for CI on Linux/Mono.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/project"
BUILD="$PROJECT/bin/Release"
TESTS="$PROJECT/NWR.Tests/bin/Release"

require_cmd() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Missing required command: $1" >&2
        exit 1
    fi
}

stage_runtime() {
    cp -f "$BUILD/NWR.exe" "$BUILD/BSLib.dll" "$BUILD/ZRLib.dll" "$ROOT/"
    cp -f "$TESTS/NWR.Tests.exe" "$ROOT/"
    cp -f "$PROJECT/NWR.Tests/nunit.framework.dll" "$ROOT/" 2>/dev/null || true
    if [[ -f "$PROJECT/NWR.Harness/bin/Release/NWR.Harness.exe" ]]; then
        cp -f "$PROJECT/NWR.Harness/bin/Release/NWR.Harness.exe" "$ROOT/"
    fi
}

main() {
    require_cmd mono
    require_cmd xbuild

    echo "Building Release ..."
    (cd "$PROJECT" && xbuild NWR.sln /p:Configuration=Release /verbosity:quiet)

    stage_runtime
    cd "$ROOT"

    echo "Tier B: NWR.Tests (serialization unit tests) ..."
    mono NWR.Tests.exe
    echo

    echo "Tier C: integration scenarios (--all) ..."
    mono NWR.Tests.exe --all

    echo
    echo "All tests passed."
}

main "$@"
