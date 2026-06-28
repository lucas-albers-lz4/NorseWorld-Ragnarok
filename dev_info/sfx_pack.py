#!/usr/bin/env python3
"""Build Tier C minimal playable sfx/ tree from master clips + manifest."""

from __future__ import annotations

import argparse
import fnmatch
import json
import os
import re
import shutil
import sys
from pathlib import Path

SONG_MENU = {"startup.ogg", "intro.ogg"}
ITEM_KINDS = [
    "Armor", "Bottle", "Bow", "Clothing", "Coin", "Crossbow", "Flute", "Food",
    "GlassOcarina", "Helmet", "Knife", "Leather", "Mace", "Ocarina", "Potion",
    "Quiver", "Ring", "Scroll", "Shield", "Stylus", "Sword",
]
ITEM_ACTIONS = ["Drop", "Pickup", "Remove", "Wear", "Break", "Use", "Mix"]
CREATURE_ACTIONS = ["Attack", "Killed", "Move", "Shot", "Slay", "Wounded"]
MIN_COMPLETE = 480


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def dev_info_dir() -> Path:
    return Path(__file__).resolve().parent


def load_manifest() -> dict:
    path = dev_info_dir() / "sfx-manifest.json"
    with path.open(encoding="utf-8") as f:
        return json.load(f)


def scan_static_ogg_refs(root: Path) -> set[str]:
    refs: set[str] = set()
    pattern = re.compile(r"[\w./\\-]+\.ogg")
    for base in (root / "project", root / "resources"):
        if not base.is_dir():
            continue
        for path in base.rglob("*"):
            if path.suffix not in (".cs", ".xml"):
                continue
            text = path.read_text(encoding="utf-8", errors="ignore")
            for match in pattern.findall(text):
                refs.add(match.replace("\\", "/"))
    return refs


def land_song_names(root: Path) -> set[str]:
    xml = (root / "resources" / "RDatabase.xml").read_text(encoding="utf-8", errors="ignore")
    return set(re.findall(r"<Song>([^<]+\.ogg)</Song>", xml))


def creature_sfx_names(root: Path) -> list[str]:
    xml = (root / "resources" / "RDatabase.xml").read_text(encoding="utf-8", errors="ignore")
    return sorted(set(re.findall(r"<sfx>([^<]+)</sfx>", xml)))


def static_to_disk_paths(refs: set[str], songs: set[str]) -> set[str]:
    paths: set[str] = set()
    for ref in refs:
        if ref.startswith("ambient/") or ref.startswith("faith/"):
            paths.add(ref)
        elif ref.startswith("E_"):
            paths.add("effects/" + ref)
        elif "/" not in ref:
            if ref in songs or ref in SONG_MENU:
                paths.add("songs/" + ref)
            else:
                paths.add(ref)
        else:
            paths.add(ref)
    return paths


def enumerate_targets(root: Path) -> set[str]:
    refs = scan_static_ogg_refs(root)
    songs = land_song_names(root)
    targets = static_to_disk_paths(refs, songs)

    for sfx in creature_sfx_names(root):
        for action in CREATURE_ACTIONS:
            targets.add(f"creatures/{sfx}_{action}.ogg")

    for kind in ITEM_KINDS:
        for action in ITEM_ACTIONS:
            targets.add(f"items/{kind}_{action}.ogg")

    return targets


def resolve_master(path: str, manifest: dict) -> str | None:
    creature_actions = manifest.get("creature_actions", {})
    if path.startswith("creatures/") and path.endswith(".ogg"):
        action = path.rsplit("_", 1)[-1].removesuffix(".ogg")
        if action in creature_actions:
            return creature_actions[action]

    for rule in manifest.get("rules", []):
        if fnmatch.fnmatch(path, rule["glob"]):
            return rule["master"]

    if path.startswith("items/"):
        return manifest.get("item_master")

    return None


def master_abs_path(manifest: dict, master_key: str, dev_info: Path) -> Path:
    rel = manifest["masters"][master_key]
    return (dev_info / rel).resolve()


def count_ogg_files(sfx_dir: Path) -> int:
    if not sfx_dir.is_dir():
        return 0
    return sum(1 for _ in sfx_dir.rglob("*.ogg"))


def build_pack(root: Path, force: bool, copy_mode: bool) -> int:
    dev_info = dev_info_dir()
    manifest = load_manifest()
    sfx_dir = root / "sfx"
    targets = enumerate_targets(root)

    if sfx_dir.exists() and not force:
        count = count_ogg_files(sfx_dir)
        if count >= MIN_COMPLETE:
            print(f"sfx/ already exists with {count} files (>= {MIN_COMPLETE}); use --force to regenerate.")
            return 0

    if sfx_dir.exists() and force:
        shutil.rmtree(sfx_dir)

    unmapped: list[str] = []
    mapping: dict[str, str] = {}
    for target in sorted(targets):
        master_key = resolve_master(target, manifest)
        if not master_key:
            unmapped.append(target)
            continue
        mapping[target] = master_key

    if unmapped:
        print("Unmapped sfx paths:", file=sys.stderr)
        for p in unmapped:
            print(f"  {p}", file=sys.stderr)
        return 1

    for master_key in set(mapping.values()):
        master_path = master_abs_path(manifest, master_key, dev_info)
        if not master_path.is_file():
            print(f"Missing master clip: {master_path}", file=sys.stderr)
            print("Run dev_info/sfx-src/generate-masters.sh first.", file=sys.stderr)
            return 1

    created = 0
    for rel_path, master_key in sorted(mapping.items()):
        dest = sfx_dir / rel_path
        dest.parent.mkdir(parents=True, exist_ok=True)
        src = master_abs_path(manifest, master_key, dev_info)
        if copy_mode:
            shutil.copy2(src, dest)
        else:
            if dest.exists() or dest.is_symlink():
                dest.unlink()
            dest.symlink_to(src)
        created += 1

    print(f"Created {created} entries under {sfx_dir} ({'copy' if copy_mode else 'symlink'} mode).")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--force", action="store_true", help="Remove existing sfx/ and rebuild")
    parser.add_argument("--copy", action="store_true", help="Copy files instead of symlinks")
    parser.add_argument("--list", action="store_true", help="List enumerated target paths and exit")
    args = parser.parse_args()

    root = repo_root()
    if args.list:
        for p in sorted(enumerate_targets(root)):
            print(p)
        return 0

    return build_pack(root, args.force, args.copy)


if __name__ == "__main__":
    sys.exit(main())
