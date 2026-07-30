# 01 — NWGameSpace orchestration

## SaveGame / LoadGame

Files: [`project/Game/NWGameSpace.cs`](../../project/Game/NWGameSpace.cs) ~949–1123  
Version constant: `RGF_Version = (1, 21)`.

### Slot layout (current revision)

**Save (.rgp):** header (RGP + 1.21) → `fPlayer.SaveToStream`  
**Save (.rgt):** header (RGT + 1.21) → `fTime.SaveToStream` → each layer → `SaveVolatiles`  
**Save (.rgj):** `fJournal.Save` (separate RSJ 1.0)

**Load:** `LoadPlayer` → open .rgt → if `Revision >= 4` load time else `ResetTime` → layers → if `Revision >= 3`: old extincted list (`Revision <= 14`) else `LoadVolatiles` → require zero trailing bytes → journal → transfer player.

Current save always writes time + volatiles in the modern form. Matches load path for 1.21. **OK** for version gates.

### LoadVolatiles / SaveVolatiles (~556–591)

Save: count of non-`None` volatiles, then `(GUID, RuntimeState byte)` each.  
Load: count, then same pairs applied to DB entries. Field order/types match. **OK**.

### ASYMMETRY — LoadPlayer soft-fail

`LoadPlayer` (~949–972):

- On `IOException`, logs and **returns without rethrow** — `LoadGame` continues with a partially/unloaded player.
- On trailing bytes after player stream, logs `playerLoad(): fail` and **does not throw** (unlike `terrainsLoad(): fail`, which throws `IOException`).

Terrains path treats corruption as fatal; player path does not. High risk for silent bad loads.

### ASYMMETRY — SaveGame IO swallowed

`SaveGame` catch for `IOException` logs `saveGame.IO()` and **does not rethrow** (`//throw ex;` commented out). Caller may believe save succeeded when files were incomplete.

### Notes

- Progress callbacks wrap layer load/save; not stream-related.
- Journal load failures are soft (see 06-secondary.md).
