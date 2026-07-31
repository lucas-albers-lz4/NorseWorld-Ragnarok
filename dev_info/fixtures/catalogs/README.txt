# Catalog fixtures

Generated reference dumps of game DB entries for data-driven tests.

| File | Generator | Regenerates |
|------|-----------|-------------|
| `items.json` | `mono NWR.Tests.exe item-catalog` (or NUnit `ItemCatalogTests`) | After `RDatabase.xml` item changes |

Do not include `item-catalog` in `--all` (generator, like `build-fixture`).
