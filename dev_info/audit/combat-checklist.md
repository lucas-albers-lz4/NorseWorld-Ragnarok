# Combat resolution checklist

Issue [#7](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/7). Baseline: `./dev_info/run-tests.sh` all passed on `audit/combat-resolution` (2026-07-30).

Status: **OK** · **BUG** · **DEFER** · **N/A**

| Area | Location | Status | Summary |
|------|----------|--------|---------|
| Call graph / entry points | NWMainWindow → NWGameSpace → AttackTo | OK | Documented in 07 |
| Melee hit vs AC | CalcAttackInfo projectile==null | BUG | ArmorClass unused; armor Defense does not affect melee ToHit |
| Projectile hit vs AC | CalcAttackInfo ~1763 | OK* | Higher AC reduces pHit (*remake scale, not DOS AC-10) |
| Math.Abs(ToHit) | CalcAttackInfo ~1810 | BUG | Negative ToHit becomes positive hit chance |
| Parry on miss | AttackTo ~1865 | BUG | Chance(ToHit−Parry) trains Parry on most misses |
| Damage floor / Fragile / Invuln | CalcAttackInfo | OK | Clamps and flags applied |
| Weapon damage order | RDatabase Runesword vs WarHammer | OK | Runesword 18–45 &gt; WarHammer 11–20; no Katana entry |
| Armor equip Defense | Item.InUse | OK* | Adds to ArmorClass; +1 quirk in GetAttribute |
| ApplyDamage / Immortal | ApplyDamage | OK | Death vs second life |
| Death / DropAll | Death, DropAll | OK | Drops and state |
| Nidhogg PostDeath | PostDeath ~2669 | BUG | LastAttacker null → NRE |
| GS PostDeath | NWGameSpace.PostDeath | OK | Null creature guarded |
| GetAttackExp | ~4275 | OK | Min 1 XP |
| Speed Raud-10 | Viking/Woodsman Speed=10 | OK | Matches manual |
| Integer overflow | damage/HP path | DEFER | No checked ops; ranges practical |
| DOS AC-10 naming | manual vs DB AC=0 | DEFER | Remake higher-is-better defense rating |
| Armor skill on damage | FIXME in AttackTo | DEFER | Unimplemented |
| AttackSpecialEffect stubs | AttackSpecialEffect | DEFER | Mostly ExStub; for #8/#9 as needed |

## Filed bugs (feed #20)

| Issue | Title |
|-------|-------|
| [#31](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/31) | Melee hit ignores ArmorClass / armor Defense |
| [#32](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/32) | Math.Abs(ToHit) can invert miss chances |
| [#33](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/33) | Miss path trains Parry via Chance(ToHit−Parry) |
| [#34](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/34) | Nidhogg PostDeath null-dereferences LastAttacker |
