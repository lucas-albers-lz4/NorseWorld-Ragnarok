# Serialization correctness audit

Tracks GitHub issue [#6](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/6).

## Method

For each `SaveToStream` / `LoadFromStream` (or `Save`/`Load`) pair:

1. Compare field order, types, and counts side-by-side.
2. Note version branching against current `NWGameSpace.RGF_Version` (1.21).
3. Flag conditional writes that load always reads (or vice versa).
4. Mark **OK**, **ASYMMETRY**, or **N/A** in [serialization-checklist.md](serialization-checklist.md).

Fixes belong under issue #19 — this directory is review notes only.

## Save formats

| Files | Sign | Version | Contents |
|-------|------|---------|----------|
| `save/rgame_N.rgp` | RGP | RGF 1.21 | Player stream |
| `save/rgame_N.rgt` | RGT | RGF 1.21 | Time + layers + volatiles |
| `save/rgame_N.rgj` | RSJ | 1.0 | Journal messages |
| Ghosts list (separate) | RGL | 1.0 | Ghost creatures |
| Scores list (separate) | RSL | 1.0 | High scores |

## Baseline (this audit)

Ran `./dev_info/run-tests.sh` on branch `audit/serialization-correctness` (2026-07-30):

- Tier B: FileHeader, JournalStream, ItemStream — all passed
- Tier C: bootstrap, save-load-roundtrip, save-overwrite, save-erase, player-metadata, container-roundtrip, effect-persist, wait-turns, item-use-potion, teleport-trap — all passed

Fixtures were **not** regenerated.

## Documents

| File | Coverage |
|------|----------|
| [01-gamespace-orchestration.md](01-gamespace-orchestration.md) | SaveGame / LoadGame / LoadPlayer / volatiles |
| [02-player-creature.md](02-player-creature.md) | Player, NWCreature, CustomBody, AttributeList |
| [03-item-effect.md](03-item-effect.md) | Item, Effect, EffectsList, EntityList |
| [04-brain-goals.md](04-brain-goals.md) | NWBrainEntity, goals |
| [05-terrain.md](05-terrain.md) | Layer, Field, Tile, Building, Village, Gate |
| [06-secondary.md](06-secondary.md) | Journal, Memory, Debt, Ghosts, Scores, etc. |
| [serialization-checklist.md](serialization-checklist.md) | Status table |

## Re-run harness

```bash
./dev_info/run-tests.sh
```
