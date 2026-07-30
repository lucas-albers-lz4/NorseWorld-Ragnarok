# 03 — Item / Effect / EntityList

## EntityList\<T\> (ZRLib)

[`../ZRLib/ZRLib/Core/EntityList.cs`](../../../ZRLib/ZRLib/Core/EntityList.cs) ~131–180

Save: count of items with `SerializeKind > 0`, then for each: kind byte + `SaveToStream`.  
Load: count, then kind + `CreateSerializable` + `LoadFromStream`.  
Skips `SerializeKind <= 0` on both sides when counting/writing. **OK**.

## GameEntity base

CLSID int only. **OK**.

## Item

[`project/Items/Item.cs`](../../project/Items/Item.cs) ~897–931

After base CLSID (+ `CLSID = CLSID` on load):

1. Count (word)
2. If `Container`: Contents EntityList
3. Bonus (int)
4. Identified (bool)
5. State (byte)
6. InUse (bool)
7. Weight (float)

Container branch depends on entry after CLSID refresh — same predicate on save and load. Nested contents covered by Tier B `ContainerRoundTrip` and Tier C `container-roundtrip`. **OK**.

## Effect

[`project/Effects/Effect.cs`](../../project/Effects/Effect.cs) ~153–169

After base: Action (byte), Duration (int), Magnitude (int). **OK** for field layout.

### Design note — Source-backed effects not persisted

`SerializeKind` returns `SID_EFFECT` only when `Source == null`; otherwise **0** (skipped by EntityList). Item-sourced buffs are omitted from the stream by design (expect re-apply from equipped items). Not a field asymmetry; document risk if re-apply fails after load. Harness `effect-persist` uses source-less prowling — covered.

## EffectsList

[`project/Effects/EffectsList.cs`](../../project/Effects/EffectsList.cs) — no custom stream; inherits EntityList. Execute() has Prowling duration hack (runtime). **N/A** for stream pair; lifecycle note for #8.
