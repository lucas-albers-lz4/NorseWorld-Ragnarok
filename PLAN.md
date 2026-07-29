# NorseWorld: Ragnarok — Revival Plan

## Context

We inherit a C# port of the classic NorseWorld: Ragnarok roguelike (~35k lines). The previous maintainers archived it after porting from Java. The game is not currently playable in C# (Java v0.11 is still the recommended distribution). Our goal is to produce a working, testable C# build and validate it via an autonomous test framework.

This plan breaks the work into reviewable, ordered issues. Each issue is self-contained enough to be assigned, reviewed, and merged independently.

All issues are tracked at: https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues

---

## Reference Document: DOS Game Manual

The original Ragnarok (1992) manual was retrieved from Abandonware DOS and saved to `dev_info/ragnarok_dos_manual.txt`. It contains detailed information on:

- **Quest objectives** — the main artifact quests (Gungnir, Mimming, Mjollner), Mimer's well / Aspenth, Thokk's tears, Balder's rescue. This is the specification for quest verification tests.
- **Character classes** — Viking, Alchemist, Sage, Woodsman, Conjurer, Blacksmith with stat ranges, starting equipment, and class-specific abilities. Cross-reference against the C# `GlobalVars.cid_*` constants and `InitBegin/SelectHero` path.
- **Skills and Innate Powers** — 30+ documented skills (Fennling, Ironworking, Precognition, Teleportation, etc.) that the C# code should implement. Many of these map to `SkillID`/`AbilityID` enums in `Game/Types/`.
- **Items** — detailed lists of armor (with AC values), weapons (with qualitative rankings), wands (8 types with effects), potions (7+ types), scrolls (12+ types), rings, amulets. This is the gold standard for item behavior verification.
- **Creatures** — 60+ creature races with behaviors, special attacks, and lore. Cross-reference against `CreatureEntry` in the database and the `Brain/` AI implementations.
- **Commands** — Full keybindings, which map to `UserAction` / `CreatureAction` enums. The `FAR MOVE/REST`, `DIG`, `AID FELLOW NORSEMAN` etc. commands define the action surface the test framework should exercise.
- **World geography** — Midgard, Asgard, Niflheim, Azare's Plane, Crossroads, Limbo, Chaos, Wasteland. Maps to `NWGameSpace` layer definitions and the Bifrost/Jormungand story state.
- **Strategy tips** — projectile usage, diagonal movement, auto-pickup, message monitoring — these define "expected behavior" that tests should validate (e.g. auto-pickup toggling, message delivery).

**Note:** The manual is for the 1992 DOS original by Thomas Boyd/Robert Vawter. The NorseWorld: Ragnarok codebase is a reimagining/expansion. Many mechanics should match but some may diverge. Use the manual as a *specification reference* — if the C# code does something fundamentally different from the manual, that's a potential bug *or* a deliberate design change. Each discrepancy needs triage.

---

## Phase 0: Environment & Baseline
*Goal: Verify the C# build boots and the existing tests pass. Establish a reproducible dev environment.*

### Issue [#3](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/3) — Establish Linux Build Environment
**Priority:** P0 — Blocking
**Depends on:** Nothing

Set up Mono + SDL2 dev environment. Run `./play-cs.sh` which clones sibling repos (BSLib, ZRLib) and builds with xbuild/msbuild. Verify `NWR.exe` exists and launches.

### Issue [#4](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/4) — Run Existing Test Suite
**Priority:** P0 — Blocking
**Depends on:** #3

Build and run `NWR.Tests` (NUnit) and `NWR.Harness` (standalone exe). 10 existing scenarios covering save/load roundtrips, potion use, teleport traps, turn processing. Establish baseline pass/fail and document known failures.

### Issue [#5](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/5) — Generate Baseline Fixtures
**Priority:** P0 — Blocking
**Depends on:** #3

Regenerate golden saves in `dev_info/fixtures/save/` (slot8, container, effects). May be stale if RGF version changed.

---

## Phase 1: Targeted Architecture Audit
*Goal: Identify high-risk code areas with targeted review — not a blanket MCR, but focused analysis of the subsystems most likely to contain bugs.*

### Issue [#6](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/6) — Serialization Correctness Audit
**Priority:** P1 — High
**Depends on:** #4

Binary save format (RGF v1.21) uses hand-written LoadFromStream/SaveToStream on every GameEntity subclass. Compare every save/load method pair for field order, type, and count mismatches. Covers: NWGameSpace, Player, NWCreature, Item, Effect, BrainEntity, Journal, Ghost, Score, Memory, Debt.

### Issue [#7](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/7) — Combat Resolution Audit
**Priority:** P1 — High
**Depends on:** #4

NWCreature.cs (4400 lines) attack pipeline. Trace combat cycle, check AC direction (manual: "lower = better, 10 normal"), overflow paths, null refs through CurrentField/FindCreature. Cross-ref weapon damage rankings from manual.

### Issue [#8](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/8) — Effect System Audit
**Priority:** P1 — High
**Depends on:** #4

230+ effects, ray subclasses, area clouds (FyleischCloud). Known exit exceptions. Review lifecycle, verify each ray subclass side effects, check persistence through save/load.

### Issue [#9](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/9) — Creature AI Health Check
**Priority:** P2 — Medium
**Depends on:** #4

Goal-oriented AI system (9 brain types, 14+ goal types). Document dispatch mechanism, verify each brain ticks without throwing, review goal priority. Cross-ref manual creature behaviors.

---

## Phase 2: Test Framework Foundation
*Goal: Build the scaffolding for automated, data-driven testing.*

### Issue [#10](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/10) — Item Catalog Generator
**Priority:** P1 — High
**Depends on:** #4

Iterate GlobalVars.dbItems, spawn each on player, dump full properties to `dev_info/fixtures/catalogs/items.json`. Enables data-driven testing of every item type.

### Issue [#11](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/11) — Creature Catalog Generator
**Priority:** P2 — Medium
**Depends on:** #4

Enumerate all creature types to `dev_info/fixtures/catalogs/creatures.json`. Includes brain type mapping, speed, AC, hit dice, special abilities.

### Issue [#12](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/12) — Effect Catalog Generator
**Priority:** P2 — Medium
**Depends on:** #4

Enumerate all 230+ effects to `dev_info/fixtures/catalogs/effects.json`. Action kind, flags, target mode, ray handler class.

### Issue [#13](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/13) — Test Scenario DSL
**Priority:** P1 — High
**Depends on:** #10, #11, #12

Design C# fluent DSL for declarative test scenarios. Load fixture, spawn items/creatures, use items, wait turns, save/load roundtrip, check conditions. JSON-serializable so scenarios can be authored externally.

---

## Phase 3: Autonomous Test Framework
*Goal: Build and run an autonomous tester that exercises game code paths without human direction.*

### Issue [#14](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/14) — Single-Operation Testing
**Priority:** P1 — High
**Depends on:** #10, #13

For each item, effect, equipment, and recipe: spawn/apply, verify no crash, save/load roundtrip. First automated test tier.

### Issue [#15](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/15) — Sequence Fuzzing
**Priority:** P1 — High
**Depends on:** #14

Generate random 3-8 step sequences from item/effect/action catalogs. Run headless, check invariants after each step. Record minimal reproduction cases for failures.

### Issue [#16](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/16) — Quest Path Coverage
**Priority:** P2 — Medium
**Depends on:** #11, #14

Construct scenarios exercising quest system end-to-end: artifact pickup, deity delivery, save/load at each quest stage. Refer to DOS manual for quest specification.

### Issue [#17](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/17) — Edge Case & Invariant Testing
**Priority:** P2 — Medium
**Depends on:** #14

Targeted tests for: empty/full inventory, zero HP, negative values, container nesting >1, 100+ creature fields, field edge bounds, dead entity interaction.

### Issue [#18](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/18) — Differential Testing (Java vs C#)
**Priority:** P3 — Low
**Depends on:** #14, Java dist available

Run equivalent operations in Java v0.11 (reference impl) and C#, compare player state snapshots.

---

## Phase 4: Fix & Validate
*Goal: Fix discovered bugs and re-verify with the automated test framework.*

### Issue [#19](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/19) — Serialization Bug Fixes
**Priority:** P1 — High
**Depends on:** #6, #14-#17

Fix all serialization bugs from audit + fuzzer. Each fix: description, LoadFromStream/SaveToStream fix, regression test, fixture regeneration.

### Issue [#20](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/20) — Combat/Mechanic Bug Fixes
**Priority:** P1 — High
**Depends on:** #7, #8, #9, #15, #17

Fix combat, effect, and AI bugs. Same PR structure as #19.

### Issue [#21](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/21) — Quest Completion Validation
**Priority:** P2 — Medium
**Depends on:** #16, #19, #20

Run full end-to-end quest completion scenario. Verify all stage transitions, journal entries, save/load at each stage.

---

## Phase 5: Playability
*Goal: Make the game launchable and playable for manual testing.*

### Issue [#22](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/22) — Jint Scripting Replacement
**Priority:** P2 — Medium
**Depends on:** None (independent)

The Jint scripting engine from Java doesn't work. Replace or remove, or document as known gap.

### Issue [#23](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/23) — GUI Launch & Display
**Priority:** P2 — Medium
**Depends on:** #3

Get SDL2 window rendering on Linux/Mono. Full stack: NWMainWindow -> ZRLib -> SDL2-CS -> SDL2 -> X11/Wayland.

### Issue [#24](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/24) — Sound System
**Priority:** P3 — Low
**Depends on:** #23

Complete SDL_mixer + NVorbis audio. Ambient loops currently disabled.

---

## Quick Reference: Manual -> Code Coverage Map

| Manual Section | C# Code Area | Key Files | Priority |
|---|---|---|---|
| Quests/Main Arc | Story state, BifrostCollapsed, IsRagnarok | NWGameSpace.cs | High |
| Character Classes (6) | Player init, GlobalVars.cid_* | Player.cs, NWGameSpace.cs | High |
| Stats (HP, STR, MP, etc.) | NWCreature fields + Abilities | NWCreature.cs, Types/AbilityID.cs | High |
| Skills (30+) | SkillID enum, SkillRec | Types/SkillID.cs | Medium |
| Commands (30+) | UserAction, CreatureAction enums | Types/UserAction.cs | Medium |
| Items (200+) | ItemEntry, Item, GenItem | Items/Item.cs, Database/ItemEntry.cs | High |
| Armor/AC | Item usage, ArmorClass | NWCreature.cs | High |
| Wands (8 types) | Item.UseItem, Effect ray dispatch | Items/Item.cs, Effects/Rays/* | High |
| Potions (7+ types) | Item.UseItem -> effect application | Items/Item.cs | High |
| Scrolls (12+ types) | Item.UseItem -> effect application | Items/Item.cs | High |
| Rings/Amulets | Equipment system | Items/Item.cs, NWCreature.cs | High |
| Creatures (60+) | CreatureEntry, NWCreature, Brain/ | Database/CreatureEntry.cs | Medium |
| AI/Behaviors | Brain subclasses, Goal subclasses | Creatures/Brain/* | Medium |
| Combat | NWCreature DoAttack/Damage | NWCreature.cs | High |
| Effects (230+) | Effect system, EffectsData | Effects/* | High |
| World Geography | Layer defs, Region, Village | Universe/* | Low |

## Dependency Graph

```
Phase 0:
  #3 (build) -> #4 (test baseline) -> #5 (fixtures)

Phase 1 (parallel, after #4):
  #6 (serialization) ──────────────────────────┐
  #7 (combat) ───────┐                         │
  #8 (effects) ──────┤                         │
  #9 (AI) ───────────┤                         │
                     ▼                         ▼
Phase 2 (some can start after #4):
  #10 (item catalog) ──┐         Phase 3:
  #11 (creature cat) ──┤         #14 (single-op) -> #15 (fuzzer) -> #18 (diff)
  #12 (effect cat) ────┤           |                  |
  #13 (DSL) <──────────┘           +-> #16 (quests)   +-> #17 (edge cases)

Phase 4:
  #19 (serialization fixes) <── #6 + #14-#17 failures
  #20 (combat/mechanic fixes) <── #7-#9 + #14-#17 failures
  #21 (quest validation) <── #16 + #19/#20

Phase 5 (independent):
  #22 (Jint scripting)
  #23 (GUI) <- #3
  #24 (sound) <- #23
```

## MCR Recommendation

The user asked whether a Multi-Model Cooperative Review (MCR) would provide value for this codebase. My assessment:

**Don't run an MCR blanket.** The codebase is large (35k lines) but well-structured. An MCR would produce mostly generic observations (big files, static globals, fragile serialization) that are already obvious from file sizes alone. The token cost would be significant and the marginal insight low.

**DO run targeted MCR on specific subsystems** *after* the test framework reveals concrete failures. When a serialization bug is caught by the fuzzer, that's the time to feed the offending code + the failing test case to 2-3 models for root cause analysis. The MCR has a specific question to answer ("why does this save/load roundtrip fail?") instead of a vague one ("find bugs in 4400 lines of creature code").

**Exception:** If you want an MCR on one specific file, `NWCreature.cs` (4400 lines, combat + inventory + movement + effects) is the candidate. But I'd still wait until the fuzzer finds a concrete failure there so the reviewers have a starting point.
