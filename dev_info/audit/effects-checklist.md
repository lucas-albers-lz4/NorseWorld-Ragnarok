# Effects system checklist

Issue [#8](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/8).  
Baseline: `./dev_info/run-tests.sh` all passed on `audit/effects-system` (2026-07-30).

| Area | Status | Summary |
|------|--------|---------|
| Lifecycle create → tick → expire | OK* | Documented in 10; *Prowling FinAction unsafe |
| Stacking `ef_Cumulative` | OK | Assign merges duration/magnitude |
| Persist Source==null | OK | harness effect-persist |
| Persist Source!=null skip | OK | intentional |
| Ray TileProc null guards | OK | most rays |
| EffectRay null Field | BUG | CurrentField null unsafe |
| ProwlingEnd mid-Execute | BUG | list clear → Index OOR |
| ProwlingEnd null ProwlImage | BUG | NRE |
| FyleischCloud | OK | fog generation |
| Unbounded ep_Increase | DEFER | |
| Factory ExStub effects | DEFER | incomplete content |

## Filed bugs

| Issue | Title |
|-------|-------|
| [#37](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/37) | Umbrella: Effect / Prowling bug fixes |
| [#38](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/38) | ProwlingEnd clears EffectsList mid-Execute |
| [#39](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/39) | ProwlingEnd NRE when ProwlImage null |
| [#40](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/40) | EffectRay.Exec null CurrentField |
