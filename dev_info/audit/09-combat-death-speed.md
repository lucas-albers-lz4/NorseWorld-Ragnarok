# 09 — Death, XP, speed, and stats

## ApplyDamage → Death

[`ApplyDamage`](../../project/Creatures/NWCreature.cs) (~1688):

1. Skip if player + `Debug_Divinity`.
2. Radiation + RayAbsorb → **heal** by damage amount (`HPCur += damage`).
3. Else if `HPCur - damage <= 0`: Immortal effect → remove and survive; else `Death` + `event_Killed`.
4. Else `HPCur -= damage` + `event_Wounded`.

Note: lethal path calls `Death` without explicitly zeroing `HPCur` first; state becomes `Dead`.

## Death / PostDeath

[`Death`](../../project/Creatures/NWCreature.cs) (~2628): message, `State = Dead`, turn delay, unique volatile destroy, `DropAll()`, clear trader house holder, `PostDeath()`.

[`PostDeath`](../../project/Creatures/NWCreature.cs) (~2655): tile transforms for elemental forms; **Nidhogg** branch:

```csharp
if (LastAttacker.Equals(Space.Player))  // null LastAttacker → NRE
```

[`NWGameSpace.PostDeath`](../../project/Game/NWGameSpace.cs) (~2081): null-guards creature; soul-trap ring; Hela/Balder; extreme-mode hero spawn. **OK** for null creature.

`DropAll` unequips then drops every item — OK if field exists (normal combat deaths are on a field).

## XP

`GetAttackExp`: based on enemy `HPMax` and level ratio; minimum 1. Awarded in `AttackTo` only when `enemy.State == Dead` after `ApplyDamage`. **OK**.

## Speed / Raud

- `Speed` property = `fSpeed + SpeedMod` + effect mods (Speedup/Down, Sail, Lycanthropy).
- Human classes: `<Speed>10</Speed>` — matches DOS “10 is normal”.
- Logs `"error!!!"` if Speed &gt; 100 (no clamp). **DEFER** (debug noise).

## Stat bounds

- Init: HP from entry Min/Max; `HPCur = HPMax`; Strength/Constitution from entry; Luck hard-coded **7** on init.
- Recovery clamps `HPCur` to `HPMax`.
- Leadership uses `Strength + ArmorClass + Attacks + fSpeed` (higher AC increases leadership — consistent with remake AC scale).

## Overflow / negative damage

- Damage floored at 0 in `CalcAttackInfo`.
- No checked arithmetic; extreme DB items (e.g. Scythe 60–360) still fit `int`. **DEFER** overflow as non-practical.

## Findings

| ID | Severity | Summary |
|----|----------|---------|
| BUG | Medium | `PostDeath` Nidhogg uses `LastAttacker.Equals` without null check |
| OK | — | Speed 10 for Viking/Woodsman; DropAll + GS PostDeath null creature guard |
| OK | — | XP award path on confirmed Dead |
| DEFER | — | Speed&gt;100 only logs; no HPCur zero on Death |
