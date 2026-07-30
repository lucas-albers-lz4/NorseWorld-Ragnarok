# 05 — Terrain stack

## NWLayer

[`project/Universe/NWLayer.cs`](../../project/Universe/NWLayer.cs) ~306–347  
Save/load iterate `fH × fW` fields in the same order; load Clear + Load + Normalize pass. **OK**.

## NWField

[`project/Universe/NWField.cs`](../../project/Universe/NWField.cs) ~925–971

1. All tiles (FieldHeight × FieldWidth)
2. Creatures EntityList
3. Items EntityList (+ Owner fixup on load)
4. Features EntityList
5. Visited (bool)

Order matches. **OK**.

## NWTile

[`project/Universe/NWTile.cs`](../../project/Universe/NWTile.cs) ~60–96  
Background/Foreground words, States byte, FogID word, FogAge sbyte, Trap_Discovered bool, Lake_LiquidID int. Runtime fields (FogExtID, CreaturePtr, …) not streamed. **OK**.

## Building

[`project/Universe/Building.cs`](../../project/Universe/Building.cs) ~328–374  
ID int, Area rect, door count byte + (x,y,dir,state), Holder index into field Creatures (-1 if null). Load resolves Holder by index after creatures loaded (Features load after creatures on field — **order dependency OK**). **OK**.

## Village

[`project/Universe/Village.cs`](../../project/Universe/Village.cs) ~172–182  
Area rect only. **OK**.

## Gate

[`project/Universe/Gate.cs`](../../project/Universe/Gate.cs) ~88–108  
Base + TargetLayer int + TargetField X/Y sbytes + TargetPos X/Y sbytes. **OK**.

## Features registration

SID_BUILDING, SID_VILLAGE, SID_GATE registered for Features EntityList. Unregistered feature types would be skipped (`SerializeKind <= 0`) — none expected in Features beyond these.
