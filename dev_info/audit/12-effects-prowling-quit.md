# 12 — ProwlingEnd, quit-time, and persistence

## Observed log failures

From playtesting / [`Ragnarok.log`](../../Ragnarok.log) (2026-06-28):

```
NWCreature.ProwlingEnd(): Object reference not set to an instance of an object
EffectsList.execute(): Index was out of range...
```

Matches [`dev_info/follow-ups.txt`](../follow-ups.txt) quit-time ProwlingEnd / EffectsList notes.

## Root cause chain

1. `EffectsList.Execute` calls `Effect.Execute` for `eid_Prowling` when `Duration == 1` with `im_FinAction`.
2. `EffectsFactory.e_Prowling` → `ProwlingEnd()`.
3. `ProwlingEnd` builds `MemoryStream(ProwlImage)` and `LoadFromStream` on **this** creature.
4. `LoadFromStream` → `fEffects.LoadFromStream` **clears the list being iterated**.
5. Loop continues → **IndexOutOfRangeException**.
6. Separately, if `ProwlImage` is null (failed begin, corrupted save, double end), `new MemoryStream(ProwlImage)` → **NRE**.

## Suggested fix direction (for Phase 4)

- Defer `ProwlingEnd` until after `Execute` finishes (queue flag), **or**
- Snapshot effect indices / copy list before ticking, **or**
- Have `ProwlingEnd` remove prowling effect without full creature stream reload mid-tick; restore form on next tick.
- Null-guard `ProwlImage` before restore.

## Persistence note

Source-less prowling survives save/load (`effect-persist` harness OK). Source-backed effects intentionally not serialized (see ser audit #6 / `03-item-effect.md`).

## Bounds

Duration/magnitude from `EffectsData` ranges; tick scales magnitude with duration ratio. No clamp if `ep_Increase` grows unbounded — **DEFER**.
