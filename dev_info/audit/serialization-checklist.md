# Serialization checklist

Status: **OK** = field order/types match · **ASYMMETRY** = bug filed · **N/A** = no own stream / intentional skip

Baseline harness: all passed 2026-07-30 (see README).

| Type / path | Location | Status | Summary |
|-------------|----------|--------|---------|
| SaveGame / LoadGame orchestration | NWGameSpace.cs ~974–1123 | ASYMMETRY | Save IO swallowed; version gates OK for 1.21 |
| LoadPlayer | NWGameSpace.cs ~949–972 | ASYMMETRY | IOException + trailing bytes soft-fail (vs terrains throw) |
| LoadVolatiles / SaveVolatiles | NWGameSpace.cs ~556–591 | OK | count + (id, state) |
| GameEntity CLSID | ZRLib GameEntity.cs | OK | int CLSID |
| EntityList\<T\> | ZRLib EntityList.cs | OK | kind-filtered count |
| NWCreature | NWCreature.cs ~3777–3915 | OK | stats + lists + brain/body |
| Player | Player.cs ~780–803 | OK | satiety, memory, morality, formation |
| CustomBody | CustomBody.cs ~50–83 | OK | part list |
| AttributeList | AttributeList.cs ~186–221 | OK | id/value pairs |
| Item | Item.cs ~897–931 | OK | count, contents, bonus, flags, weight |
| Effect | Effect.cs ~153–169 | OK | action/duration/magnitude; Source≠null skipped |
| EffectsList | EffectsList.cs | N/A | inherits EntityList; lifecycle ≠ stream |
| NWBrainEntity | NWBrainEntity.cs | OK | serializable goals only |
| NWGoalEntity base | NWGoalEntity.cs | OK | four ints |
| LocatedGoal / AreaGoal | Goals/*.cs | OK | point / rect |
| PointGuardGoal / AreaGuardGoal | Goals/*.cs | OK | SID 9/10 |
| Other goals | Goals/*.cs | N/A | SerializeKind 0 |
| NWLayer | NWLayer.cs ~306–347 | OK | field grid |
| NWField | NWField.cs ~925–971 | OK | tiles, creatures, items, features, visited |
| NWTile | NWTile.cs ~60–96 | OK | bg/fg/fog/trap/lake |
| Building | Building.cs ~328–374 | OK | doors + holder index |
| Village | Village.cs ~172–182 | OK | area rect |
| Gate | Gate.cs ~88–108 | OK | target coords |
| Journal / JournalItem | Story/Journal*.cs | OK* | message fields OK; enemies not persisted (symmetric); soft-fail load |
| Memory | Memory.cs | OK | kind + entry |
| Debt | Debt.cs | OK | lender + value |
| RecallPos | RecallPos.cs | OK | layer/field/pos |
| Knowledge | Knowledge.cs | OK | id + refs |
| SourceForm | SourceForm.cs | OK | SfID |
| Ghost | Ghost.cs | N/A | delegates to NWCreature |
| GhostsList | GhostsList.cs | ASYMMETRY | Save passes `version: null` |
| ScoresList | ScoresList.cs | OK | inline score fields |
| IntList | IntList.cs | OK | int array |
| NWDateTime | NWDateTime.cs | OK | YMDHMS + dummy |

\* Journal soft-fail grouped under LoadPlayer / orchestration bugs rather than a separate field mismatch.

## Filed bugs (feed #19)

| Issue | Title |
|-------|-------|
| [#26](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/26) | LoadPlayer / SaveGame soft-fail |
| [#27](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/27) | GhostsList.Save null FileVersion |
