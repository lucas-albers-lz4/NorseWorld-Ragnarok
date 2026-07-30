# 14 — Goals and priority

Issue [#9](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/9).

## Selection model

Goals live in a **list** on the brain. Each Think tick:

1. `PrepareGoals` may create/update goals
2. Each goal gets a `Value` via `EvaluateGoal`
3. Exactly **one** goal runs: maximum `Value` (first wins on ties)

There is no sticky “current goal” field across ticks.

## Typical BeastBrain priorities (approx.)

| Goal | Typical Value |
|------|---------------|
| DebtTake (Trader, debtor in range) | 1.0 |
| WareReturn (in shop) | 0.9 |
| PlayerFind (Raven) | 0.8 |
| Evade | 0.75 |
| Chase | 0.6 |
| Stalk | 0.55 |
| Escort | ~0.3+ |
| ItemAcquire | 0.25 |
| Travel | 0.225 |
| Flock | 0.22 |
| Point/Area guard | ~0.2+ |

WarriorBrain boosts chase/guard/alarm-travel; may leave plain Travel at Value 0 if `EmitterID == 0` (DEFER).

## Goal catalog

| Goal | Purpose | Complete when |
|------|---------|---------------|
| TravelGoal | Path to point | At position |
| StalkGoal | Scent chase (extends Travel) | As Travel |
| PointGuardGoal | Stay near point (**persisted**) | Never |
| AreaGuardGoal | Stay in area (**persisted**) | Never |
| EscortGoal | Follow leader slot | Never (refreshes) |
| EnemyChaseGoal | Attack enemy | Enemy gone/dead/unavailable |
| EnemyEvadeGoal | Flee / ranged poke | Enemy gone/unavailable |
| ItemAcquireGoal | Walk to / pickup | Item gone or acquired |
| FlockGoal | Boid cohesion | Duration expiry |
| PlayerFindGoal | Raven seeks player | Adjacent |
| ShopReturnGoal | Trader returns to shop | At return point |
| DebtTakeGoal | Chase debtor | Never sets complete |
| WareReturnGoal | Drop stolen ware in shop | After drop |
| gk_Friend | Evaluated in BeastBrain | **Never created** in `CreateGoalEx` |

## Serialization (cross-ref #6)

Only **PointGuard** / **AreaGuard** have non-zero `SerializeKind`. Other goals rebuild next Think. Escort reattached via mercenary reset after load. See [04-brain-goals.md](04-brain-goals.md).

## Null / crash notes

| Path | Risk |
|------|------|
| EnemyChase / Evade with null Enemy | NRE in Execute |
| PrepareEscort with null Leader | NRE |
| DebtTake with null Debtor | NRE |
| WareReturn / Trader StepTo with null house | NRE |

Outer Think/`DoTurn` try/catch usually logs rather than aborting Mono.

## Status

| Item | Status |
|------|--------|
| Priority / conflict rules | OK — documented |
| Persistent vs transient goals | OK — matches 04 |
| gk_Friend dead case | DEFER — remove or implement |
| Null guards on goals | BUG — file under AI fixes |
