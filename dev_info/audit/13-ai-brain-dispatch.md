# 13 — Brain dispatch

Issue [#9](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/9).  
Baseline: `./dev_info/run-tests.sh` all passed on `audit/creature-ai` (2026-07-30).

Serialization of brains/goals remains in [04-brain-goals.md](04-brain-goals.md) (Issue #6). This doc covers **runtime** assignment and tick behavior.

## InitBrain

[`NWCreature.InitBrain`](../../project/Creatures/NWCreature.cs) (~1521–1554), also [`Player.InitBrain`](../../project/Game/Player.cs) (~224–231).

Decision order:

1. **Prowling** → `BeastBrain` (direct assign; early return)
2. **CLSID specials** → Victim / Eitri / Raven / Warrior
3. **`esMind`** → `TraderBrain` if `fIsTrader`, else `SentientBrain`
4. Else → `BeastBrain`
5. If result is `BeastBrain` and entry has **`esCohorts`** → `Flock = true`

`esUseItems` does **not** choose the brain class; it gates item-acquire goals and health checks inside sentient/`DoTurn` paths.

Shop merchants: `IsTrader = true` then re-`Init` as `cid_Merchant` so `InitBrain` sees the trader flag. Mercenary hire replaces brain with `WarriorBrain` + escort goal.

## Assignment table

| Brain | When |
|-------|------|
| BeastBrain | Default non-mind; prowling |
| SentientBrain | `esMind`, not trader / not CLSID special |
| TraderBrain | `esMind` + `fIsTrader` |
| WarriorBrain | `cid_Guardsman`, `cid_Jarl`; mercenaries |
| VictimBrain | `cid_Agnar`, `cid_Haddingr`, `cid_Ketill` |
| EitriBrain | `cid_Eitri` |
| RavenBrain | `cid_Raven` |
| LeaderBrain | Player only |

## Hierarchy

```
BrainEntity (ZRLib) — Think()
  └─ NWBrainEntity — goal list I/O
       ├─ BeastBrain — combat / wander / flock
       │    └─ SentientBrain — items / services
       │         ├─ WarriorBrain
       │         ├─ TraderBrain
       │         ├─ VictimBrain
       │         ├─ RavenBrain
       │         └─ EitriBrain
       └─ LeaderBrain — party geometry (player)
```

## Think / DoTurn

[`BrainEntity.Think`](../../../ZRLib/ZRLib/Core/Brain/BrainEntity.cs): `PrepareGoals` → evaluate all goals → execute **highest `Value`** (strict `>`; ties keep first).

[`NWCreature.DoTurn`](../../project/Creatures/NWCreature.cs) (~2732+): Think runs for non-players when stamina allows and `HasTurn()`; player only when prowling. Exceptions in Think/`DoTurn` are caught and logged (usually no process abort).

| Brain | Behavior summary |
|-------|------------------|
| BeastBrain | Travel, flock, scent-stalk, chase/evade |
| SentientBrain | + item acquire (`esUseItems`); Teach/Trade/Exchange/Recruit services |
| WarriorBrain | Guard/alarm emitter boosts; merc chase priority |
| TraderBrain | Shop-return, debt-take, door open/close, ware-return |
| VictimBrain | Sacrifice dialogs only; still runs full sentient/beast goals (not a pure “flee” brain) |
| RavenBrain | High-value `PlayerFindGoal` |
| EitriBrain | Anvil/arm dialog; default sentient goals otherwise |
| LeaderBrain | Formation helpers; `CreateGoalEx` returns null — no player goals |

## Status

| Item | Status |
|------|--------|
| Dispatch mapping | OK — documented |
| Each brain type has Think path | OK — via ZRLib BrainEntity |
| VictimBrain “flee” wording in #9 | DEFER — docs mismatch; brain is dialog + sentient AI |
| TraderBrain `FindHouse` null | BUG — NRE risk (caught in some paths) |
| Goal null Enemy/Leader/Debtor | BUG — NRE risk inside Execute |
