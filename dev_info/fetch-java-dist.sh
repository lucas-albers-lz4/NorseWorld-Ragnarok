#!/usr/bin/env bash
# Fetch official Java v0.11 Windows dist (A/B baseline for #18).
# Unpack to nwr-dist-v0.11.0-win/ at repo root (gitignored).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DEST="$ROOT/nwr-dist-v0.11.0-win"
URL="${NWR_JAVA_DIST_URL:-https://github.com/Serg-Norseman/NorseWorld-Ragnarok/releases/download/v0.11.0/nwr-dist-v0.11.0-win.x86.x64.zip}"

if [[ -f "$DEST/Ragnarok.jar" ]]; then
  echo "Java dist already present: $DEST"
  exit 0
fi

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
ZIP="$TMP/nwr-dist.zip"
echo "Downloading $URL ..."
curl -fsSL -L -o "$ZIP" "$URL"
echo "Unpacking to $ROOT ..."
unzip -qo "$ZIP" -d "$ROOT"
if [[ ! -f "$DEST/Ragnarok.jar" ]]; then
  echo "ERROR: expected $DEST/Ragnarok.jar after unzip" >&2
  exit 1
fi
# Linux launcher (Wine + host java if Wine javaw missing)
if [[ ! -f "$DEST/play.sh" ]]; then
  cat > "$DEST/play.sh" <<'EOF'
#!/usr/bin/env bash
# Launch Java v0.11 under Wine (Windows SDL DLLs) or native java -jar (may fail without Wine SDL).
set -euo pipefail
cd "$(dirname "$0")"
if command -v wine >/dev/null 2>&1; then
  exec wine java -jar Ragnarok.jar "$@"
fi
exec java -jar Ragnarok.jar "$@"
EOF
  chmod +x "$DEST/play.sh"
fi
echo "OK  $DEST"
