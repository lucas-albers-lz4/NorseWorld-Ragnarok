# 02 — Player / NWCreature / body / attributes

## NWCreature LoadFromStream / SaveToStream

[`project/Creatures/NWCreature.cs`](../../project/Creatures/NWCreature.cs) ~3777–3915

### Field order (after `GameEntity` CLSID)

| # | Save | Load |
|---|------|------|
| 1–3 | Sex, State, Alignment (bytes) | same |
| 4–5 | IsTrader (bool), Name (string) | same |
| 6–22 | Turn, LayerID, Field X/Y, Level, Exp, Strength, Speed, Attacks, ToHit, Luck, Constitution, AC, DamageBase, HPMax, HPCur, MPMax, MPCur, DBMin, DBMax (ints) | same |
| 23–26 | Effects, Items, Abilities, Skills (lists) | same + Owner fixups for effects/items |
| 27–33 | IsMercenary, Perception, Dexterity (word), Survey, Hear, Smell, LastDir | same |
| 34–35 | Brain (if non-null), Body (if non-null) | `InitBody`/`InitBrain` then load if non-null |

Load re-inits body/brain before reading their streams so types match current CLSID. Conditional brain/body I/O is symmetric (both skip when null). **OK**.

`CLSID = CLSID` after base load refreshes entry pointers (known prior fix).

## Player LoadFromStream / SaveToStream

[`project/Game/Player.cs`](../../project/Game/Player.cs) ~780–803

After `base` (NWCreature):

1. Satiety (word)
2. Memory list
3. Morality (byte)
4. LeaderBrain.Formation (byte)

`SerializeKind` returns **0** so the player is never written as a field creature via EntityList — expected. Formation lives on Player stream, not inside brain goals. **OK**.

## CustomBody

[`project/Creatures/CustomBody.cs`](../../project/Creatures/CustomBody.cs) ~50–83  
Count (byte) + pairs (part type byte, state byte). **OK**.

## AttributeList (abilities/skills)

[`project/Game/AttributeList.cs`](../../project/Game/AttributeList.cs) ~186–221  
Count (int) + (id, value) ints. **OK**.

## Notes

- Quit-time ProwlingEnd / EffectsList exceptions ([follow-ups.txt](../follow-ups.txt)) are lifecycle/dispose, not field-order mismatches. Tracked for #8; no stream asymmetry found here.
- Mercenary post-load `ResetMercenary` is load-only repair; not a save asymmetry.
