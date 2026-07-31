#!/usr/bin/env bash
# Dump Java RGF / save-sign constants; used by ab-diff scenario.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
JDIR="$ROOT/nwr-dist-v0.11.0-win"
SRC="$ROOT/dev_info/ab/DumpJavaConstants.java"
OUT="$ROOT/dev_info/ab/DumpJavaConstants.class"

if [[ ! -f "$JDIR/Ragnarok.jar" ]]; then
  echo "ERROR: missing Java dist. Run ./dev_info/fetch-java-dist.sh" >&2
  exit 1
fi

CP="$JDIR/Ragnarok.jar:$JDIR/lib/JZRLib.jar"
javac -cp "$CP" -d "$ROOT/dev_info/ab" "$SRC"
java -cp "$ROOT/dev_info/ab:$CP" DumpJavaConstants
