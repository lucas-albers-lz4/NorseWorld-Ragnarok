#!/usr/bin/env bash
# Generate Tier C placeholder master OGG clips with ffmpeg.
# Looping tracks (menu/land/ambient) use soft pads or quiet noise — not single sine tones.
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

require_ffmpeg() {
    if ! command -v ffmpeg >/dev/null 2>&1; then
        echo "ffmpeg is required to generate master clips." >&2
        exit 1
    fi
}

# Short combat / UI one-shots: noise burst + tone so they cut through ambient loops.
make_impact() {
    local name="$1" dur="$2" freq="$3"
    local out="$DIR/${name}.ogg"
    local fade_out
    fade_out="$(awk -v d="$dur" 'BEGIN { print d - 0.08 }')"
    ffmpeg -y -hide_banner -loglevel error \
        -f lavfi -i "anoisesrc=d=${dur}:c=white:a=0.35" \
        -f lavfi -i "sine=frequency=${freq}:duration=${dur}" \
        -filter_complex \
        "[0][1]amix=inputs=2:duration=longest,volume=0.9,highpass=f=120,lowpass=f=3000,afade=t=in:st=0:d=0.01,afade=t=out:st=${fade_out}:d=0.08" \
        -c:a libvorbis -q:a 4 "$out"
    echo "  ${name}.ogg"
}

# Quiet footstep: short low thump + filtered noise (creature Move actions).
make_footstep() {
    local name="$1"
    local out="$DIR/${name}.ogg"
    ffmpeg -y -hide_banner -loglevel error \
        -f lavfi -i "sine=frequency=95:duration=0.07,volume=0.2" \
        -f lavfi -i "anoisesrc=d=0.07:c=pink:a=0.08" \
        -filter_complex \
        "[0][1]amix=inputs=2:duration=longest,volume=0.28,highpass=f=80,lowpass=f=450,afade=t=in:st=0:d=0.005,afade=t=out:st=0.04:d=0.03" \
        -c:a libvorbis -q:a 4 "$out"
    echo "  ${name}.ogg"
}

# Brief UI blip.
make_tone() {
    local name="$1" dur="$2" freq="$3" filter="${4:-}"
    local out="$DIR/${name}.ogg"
    local af="sine=frequency=${freq}:duration=${dur}"
    if [[ -n "$filter" ]]; then
        af="${af},${filter}"
    fi
    local fade_out
    fade_out="$(awk -v d="$dur" 'BEGIN { if (d > 0.15) print d - 0.1; else print d * 0.5 }')"
    af="${af},afade=t=in:st=0:d=0.05,afade=t=out:st=${fade_out}:d=0.1"
    ffmpeg -y -hide_banner -loglevel error \
        -f lavfi -i "$af" \
        -c:a libvorbis -q:a 4 "$out"
    echo "  ${name}.ogg"
}

# Looping music/ambient: mixed detuned tones + tremolo, kept very quiet.
make_loop_pad() {
    local name="$1" dur="$2" base="$3"
    local out="$DIR/${name}.ogg"
    local f2=$((base * 5 / 4))
    local f3=$((base * 3 / 2))
    ffmpeg -y -hide_banner -loglevel error \
        -f lavfi -i "sine=frequency=${base}:duration=${dur}" \
        -f lavfi -i "sine=frequency=${f2}:duration=${dur}" \
        -f lavfi -i "sine=frequency=${f3}:duration=${dur}" \
        -filter_complex \
        "[0][1][2]amix=inputs=3:duration=longest,volume=0.12,lowpass=f=800,tremolo=f=0.1:d=0.25,afade=t=in:st=0:d=1,afade=t=out:st=$(awk -v d="$dur" 'BEGIN { print d - 1 }'):d=1" \
        -c:a libvorbis -q:a 4 "$out"
    echo "  ${name}.ogg"
}

# Looping ambient bed: very quiet filtered noise (not a pure tone).
make_ambient_loop() {
    local name="$1" dur="$2" extra="${3:-}"
    local out="$DIR/${name}.ogg"
    local chain="anoisesrc=d=${dur}:c=brown:a=0.06,lowpass=f=300,volume=0.35"
    if [[ -n "$extra" ]]; then
        chain="${chain},${extra}"
    fi
    chain="${chain},afade=t=in:st=0:d=0.5,afade=t=out:st=$(awk -v d="$dur" 'BEGIN { print d - 0.5 }'):d=0.5"
    ffmpeg -y -hide_banner -loglevel error \
        -f lavfi -i "$chain" \
        -c:a libvorbis -q:a 4 "$out"
    echo "  ${name}.ogg"
}

require_ffmpeg
echo "Generating Tier C master clips in $DIR ..."

# These loop in-game (Mix_PlayMusic / ambient channel -1).
make_loop_pad song_menu   16 196
make_loop_pad song_land   24 164
make_ambient_loop ambient_generic 20
make_ambient_loop ambient_cave 20 "highpass=f=80,lowpass=f=250"
make_ambient_loop ambient_water 20 "lowpass=f=400,tremolo=f=0.5:d=0.15"

make_tone ui_click       0.15 880 "volume=0.7"
make_tone ui_system      0.4 660 "volume=0.6"
make_tone ui_door_open   0.5 300 "volume=0.65"
make_tone ui_door_close  0.5 200 "volume=0.65"
make_tone ui_positive    0.8 523 "volume=0.7"
make_tone ui_negative    0.8 196 "volume=0.7"
make_tone ui_self        0.3 740 "volume=0.6"

make_impact combat_hit     0.55 180
make_footstep combat_move
make_impact combat_ranged  0.45 520

make_tone item_generic   0.2 500 "volume=0.55"
make_tone spell_generic  0.6 880 "volume=0.65,tremolo=f=8:d=0.2"
make_tone faith_fanfare  1.5 440 "volume=0.7"

echo "Done ($(ls -1 "$DIR"/*.ogg 2>/dev/null | wc -l) files)."
