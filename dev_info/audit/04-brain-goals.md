# 04 — Brain / goals

## NWBrainEntity

[`project/Creatures/Brain/NWBrainEntity.cs`](../../project/Creatures/Brain/NWBrainEntity.cs)

Save: count of goals with `SerializeKind > 0`, then kind byte + goal stream.  
Load: clear goals, count, CreateSerializable(kind), LoadFromStream, add.  
Same skip rule as EntityList. **OK**.

`SerializeKind` on brain itself is 0 (brain is embedded in creature stream, not listed).

## NWGoalEntity base

[`project/Creatures/Brain/NWGoalEntity.cs`](../../project/Creatures/Brain/NWGoalEntity.cs)  
Duration, EmitterID, Kind, SourceID (four ints). Default `SerializeKind` = 0 (not persisted). **OK**.

## LocatedGoal / AreaGoal

[`LocatedGoal.cs`](../../project/Creatures/Brain/Goals/LocatedGoal.cs) — base + Position point.  
[`AreaGoal.cs`](../../project/Creatures/Brain/Goals/AreaGoal.cs) — base + Area rect.  
Field pairs match. **OK**.

## Persisted goal kinds (only these register SID > 0)

| Class | SID | Extra fields |
|-------|-----|--------------|
| PointGuardGoal | SID_POINTGUARD_GOAL (9) | via LocatedGoal |
| AreaGuardGoal | SID_AREAGUARD_GOAL (10) | via AreaGoal |

Registered in `NWGameSpace` SerializablesManager (~2622+).

## Non-persisted goals (SerializeKind 0) — N/A

EnemyChaseGoal, EnemyEvadeGoal, TravelGoal, ItemAcquireGoal, FlockGoal, EscortGoal, StalkGoal, PlayerFindGoal, DebtTakeGoal, ShopReturnGoal, WareReturnGoal, etc.

Skipped intentionally on save/load. Runtime AI rebuilds transient goals after load. Documented, not asymmetries.

## LeaderBrain.Formation

Persisted on **Player** stream (see 02), not inside brain goal list. **OK**.
