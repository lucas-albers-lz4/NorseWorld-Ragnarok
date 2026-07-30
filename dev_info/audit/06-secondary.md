# 06 — Secondary lists and memory entries

## Journal / JournalItem

[`Journal.cs`](../../project/Game/Story/Journal.cs), [`JournalItem.cs`](../../project/Game/Story/JournalItem.cs)

Save: RSJ header 1.0, message count, each item: Type, Text, Color, Turn; if `SIT_DAY`, nested `NWDateTime`.  
Load: same fields. Item pair **OK**.

`fEnemies` (kill stats) is **never** saved or loaded — symmetric omission. Stats regenerate only for current session. Design note, not stream asymmetry.

### Soft-fail (related to orchestration)

`Journal.Load` catches all exceptions and only logs; trailing-bytes `journalLoad(): fail` does not throw. Same soft-fail family as LoadPlayer.

## Memory

[`Memory.cs`](../../project/Game/Memory.cs)  
Count + (kind byte, entry stream) keyed by `Sign` on load. Unlike EntityList, does **not** skip `SerializeKind <= 0` on save — all registered memory types use SID > 0. **OK** for current types.

## MemoryEntry subclasses

| Type | Fields | Status |
|------|--------|--------|
| Debt | Lender string, Value int; Sign derived | OK |
| RecallPos | Layer + Field X/Y + Pos X/Y ints | OK |
| Knowledge | ID int, RefsCount word; Sign from DB | OK |
| SourceForm | SfID int | OK |

## Ghost / GhostsList

Ghost delegates to NWCreature — **OK** / N/A for extra fields.

### ASYMMETRY — GhostsList.Save passes null version

[`GhostsList.cs`](../../project/Game/Ghosts/GhostsList.cs) ~103–120:

```csharp
ghost.SaveToStream(dos, null);  // Save
ghost.LoadFromStream(dis, header.Version);  // Load uses RGL 1.0
```

Load and save disagree on `FileVersion`. Today most creature fields ignore version, but any future/versioned nested path can NRE or mis-branch. Should pass `RGL_Header.Version` (or clone) on save.

`GhostsList.Load` also swallows exceptions (soft-fail).

## ScoresList / Score

[`ScoresList.cs`](../../project/Game/Scores/ScoresList.cs)  
Inline fields: kind byte, name, desc, exp, level — same on save and load. Score class has no separate stream methods. Soft-fail on load exceptions. Field pair **OK**.

## IntList / NWDateTime

IntList: count + ints. **OK**.  
NWDateTime: Year word + Month/Day/Hour/Minute/Second/Dummy bytes. **OK**.
