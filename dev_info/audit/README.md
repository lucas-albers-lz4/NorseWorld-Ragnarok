# Game audits (`dev_info/audit`)

Review notes for Phase 1 audits. Fixes go to Phase 4 issues (#19 serialization done; #20 combat done; #37 effects done; #45 AI pending).

## Method (serialization)

For each `SaveToStream` / `LoadFromStream` pair: compare field order/types; note version branches; flag conditional I/O. See [serialization-checklist.md](serialization-checklist.md).

## Method (combat)

Trace attack → hit → damage → death; verify AC/damage vs DOS manual notes; document overflow/null risks. See [combat-checklist.md](combat-checklist.md).

## Method (effects)

Trace create → tick → expire; review rays; document Prowling/quit-time list mutation. See [effects-checklist.md](effects-checklist.md).

## Method (creature AI)

Document brain dispatch, goal priority, DoTurn/Think safety; cross-ref DOS manual creature specials. See [ai-checklist.md](ai-checklist.md).

## Save formats

| Files | Sign | Version | Contents |
|-------|------|---------|----------|
| `save/rgame_N.rgp` | RGP | RGF 1.21 | Player stream |
| `save/rgame_N.rgt` | RGT | RGF 1.21 | Time + layers + volatiles |
| `save/rgame_N.rgj` | RSJ | 1.0 | Journal messages |
| Ghosts list (separate) | RGL | 1.0 | Ghost creatures |
| Scores list (separate) | RSL | 1.0 | High scores |

## Baseline harness

`./dev_info/run-tests.sh` — record date/result in the audit that ran it.

- Serialization audit (`audit/serialization-correctness`, 2026-07-30): all Tier B + C passed.
- Combat audit (`audit/combat-resolution`, 2026-07-30): all Tier B + C passed (incl. `player-load-trailing-fail`).
- Effects audit (`audit/effects-system`, 2026-07-30): all Tier B + C passed (incl. combat tests).
- Creature AI audit (`audit/creature-ai`, 2026-07-30): all Tier B + C passed (incl. quest tests).

Fixtures were **not** regenerated.

## Documents

| File | Coverage |
|------|----------|
| [01-gamespace-orchestration.md](01-gamespace-orchestration.md) | SaveGame / LoadGame / LoadPlayer / volatiles |
| [02-player-creature.md](02-player-creature.md) | Player, NWCreature, CustomBody, AttributeList |
| [03-item-effect.md](03-item-effect.md) | Item, Effect, EffectsList, EntityList |
| [04-brain-goals.md](04-brain-goals.md) | NWBrainEntity, goals (serialization) |
| [05-terrain.md](05-terrain.md) | Layer, Field, Tile, Building, Village, Gate |
| [06-secondary.md](06-secondary.md) | Journal, Memory, Debt, Ghosts, Scores, etc. |
| [serialization-checklist.md](serialization-checklist.md) | Serialization status table |
| [07-combat-call-graph.md](07-combat-call-graph.md) | Attack entry points and sequence |
| [08-combat-ac-damage.md](08-combat-ac-damage.md) | AC scale, hit/damage formulas, weapons |
| [09-combat-death-speed.md](09-combat-death-speed.md) | Death, XP, Speed, stats |
| [combat-checklist.md](combat-checklist.md) | Combat status table |
| [10-effects-lifecycle.md](10-effects-lifecycle.md) | Effect create/tick/expire/stack |
| [11-effects-rays.md](11-effects-rays.md) | Ray subclasses + FyleischCloud |
| [12-effects-prowling-quit.md](12-effects-prowling-quit.md) | ProwlingEnd / quit-time failures |
| [effects-checklist.md](effects-checklist.md) | Effects status table |
| [13-ai-brain-dispatch.md](13-ai-brain-dispatch.md) | InitBrain mapping + Think/DoTurn |
| [14-ai-goals-priority.md](14-ai-goals-priority.md) | Goal catalog + Value priority |
| [15-ai-manual-behaviors.md](15-ai-manual-behaviors.md) | Blur / Cockatrice / Werewolf / Fyleisch / Trader |
| [ai-checklist.md](ai-checklist.md) | AI status table |

## Re-run harness

```bash
./dev_info/run-tests.sh
```
