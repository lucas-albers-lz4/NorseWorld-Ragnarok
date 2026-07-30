# 11 — Rays, FyleischCloud, and side effects

Base: [`EffectRay.Exec`](../../project/Effects/Rays/EffectRay.cs) — requires direction or creature target; sets `Field = Creature.CurrentField`; optional `MapObject` in Features; `DoLine` → `TileProc`.

## Per-ray review

| Ray | Side effects | Notes |
|-----|--------------|-------|
| AnnihilationRay | Barrier stop; `Death` on creature | Null tile guarded |
| BlackGemRay | HP drain; heal caster unless soul ring | Caps drain to HPCur |
| CancellationRay | Dispel path on creature | Null creature guarded |
| DeanimationRay | Undead destroy / death | |
| DeathRay | `HasAffect(eid_Death)` then `Death` | |
| FireRay | Fire damage path | |
| FireVisionRay | Gaze; continue while no creature | |
| FlayingRay | Radiation damage 13–36 if affected | Uses `DamageKind.Radiation` |
| GrapplingHookRay | Damage or pull self; clear PitTrap/Quicksand | Stops after first cell |
| IceRay | Cold/slow; death if magnitude ≥ 30 | |
| MonsterSkillRay | Damage; may drop item to field | |
| PolymorphRay | `e_Transformation` | |
| StoningRay | Petrify if `HasAffect(eid_Stoning)` | |
| TransmutationRay | Transform creature | |
| TunnelingRay | Dig; null tile guarded | |
| FyleischCloud | Area fog tiles + age | Not a ray; `Gen_RarefySpace` |

## Findings

| Status | Item |
|--------|------|
| BUG | `EffectRay.Exec` does not null-check `Creature.CurrentField` before `Field.Features` / linecast |
| OK | Most rays null-check target creature / tile |
| OK | Cumulative merge logic documented in 10 |
| DEFER | Flaying using Radiation damage kind; many factory stubs (`ExStub`) |

No separate field-order bugs in ray classes beyond field-null and lifecycle issues in 10/12.
