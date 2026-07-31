# 08 — AC, hit chance, and damage

## DOS manual cross-ref

From [`dev_info/ragnarok_dos_manual.txt`](../ragnarok_dos_manual.txt):

- Lower armor class = harder to hit; **AC 10 normal for unarmored human**.
- Speed on Raud scale; **10 normal**.
- Weapon flavor: Runesword > Katana > War hammer (qualitative).

## ArmorClass semantics in C# remake

| Fact | Evidence |
|------|----------|
| Unarmored Viking/Woodsman | `RDatabase.xml`: `<AC>0</AC>`, `<Speed>10</Speed>` |
| Equipping armor | `Item.InUse`: `ArmorClass += GetAttribute(ia_Defense)` ([Item.cs](../../project/Items/Item.cs) ~108) |
| Projectile hit | `pHit = Strength/7 - 2*enemy.ArmorClass + …` ([NWCreature.cs](../../project/Creatures/NWCreature.cs) ~1763) |

**Conclusion:** In this codebase **higher `ArmorClass` = better defense** (subtracted from projectile ToHit). That is the **opposite naming** of the DOS manual (lower AC better, baseline 10). Treat as remake/Java-era scale (defense rating stored in a field named ArmorClass), not classic AD&D AC. Display string uses `rs_Armor`.

`GetAttribute(ia_Defense)` also always adds `+1` after reading the entry value (~140–145) — Leather `Defense=1` applies as 2.

## Hit formulas

### Melee (`projectile == null`)

- `pHit = ToHit` (attacker), then ability factor − enemy Parry/10, visibility, Strength>18 bonus, Phase −40.
- **Enemy `ArmorClass` is not used.** Armor Defense therefore does not reduce melee hit chance.
- Then `pHit = Math.Abs(pHit)`.

### Projectile

- Base includes `- 2 * enemy.ArmorClass` (higher AC → fewer hits).
- Same modifiers + Abs.

### Hit check (`AttackTo`)

- Miss if `!Chance(ToHit)`.
- On miss, may call `CheckActionAbility(caAttackParry)` if `Chance(ToHit - Parry)` — trains parry often on miss when ToHit is high.

## Damage

- Melee: `DamageBase` = random in `[DBMin, DBMax]` (creature base + equipped weapon attrs via `InUse`).
- Projectile: `projectile.Damage + Bonus + Strength/14 + Luck/40 + Level/6`.
- Clamps: damage &lt; 0 → 0; Fragile doubles; Invulnerable → 0.
- No integer overflow guards; practical ranges from DB are within `int`.

## Weapon ranking (DB damage ranges)

| Item | DamageMin–Max | Notes |
|------|---------------|-------|
| Runesword | 18–45 | Top mundane named sword |
| BattleAxe | 7–22 | |
| WarHammer | 11–20 | Below Runesword (matches manual spirit) |
| Katana | — | **Not present** in `RDatabase.xml` (DOS name only) |

Artifacts (Mjollnir, Gungnir, Scythe, …) outrank Runesword as expected.

## Findings

| ID | Severity | Summary |
|----|----------|---------|
| BUG | High | Melee hit ignores `ArmorClass`; worn armor Defense only affects projectile formula |
| BUG | Medium | `Math.Abs(pHit)` can turn negative ToHit (Phase / high AC) into a positive hit chance |
| BUG | Low | Miss path trains Parry via `Chance(ToHit - Parry)` (near-always when ToHit ≫ Parry) |
| DEFER | — | DOS AC-10 scale vs remake AC-0 higher-is-better naming |
| FIXED | — | Enemy armor skill reduces damage by skill/10 in CalcAttackInfo (#60); trains on non-lethal hits |
| OK | — | Runesword damage &gt; WarHammer; Speed 10 for human classes |
