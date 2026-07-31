# Catalog fixtures

Generated reference dumps of game DB entries for data-driven tests.

| File | Generator | Regenerates |
|------|-----------|-------------|
| `items.json` | `mono NWR.Tests.exe item-catalog` (or NUnit `ItemCatalogTests`) | After `RDatabase.xml` item changes |
| `creatures.json` | `mono NWR.Tests.exe creature-catalog` | After creature DB / brain mapping changes |
| `effects.json` | `mono NWR.Tests.exe effect-catalog` | After `EffectsData.dbEffects` changes |

Do not include catalog generators in `--all` (like `build-fixture`).
