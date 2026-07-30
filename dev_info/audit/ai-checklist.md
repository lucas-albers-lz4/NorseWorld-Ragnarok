# Creature AI checklist

Issue [#9](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/9).  
Baseline: `./dev_info/run-tests.sh` all passed on `audit/creature-ai` (2026-07-30).

| Area | Status | Summary |
|------|--------|---------|
| Brain dispatch mapping | OK | Documented in 13 |
| Brain Think without process abort | OK* | try/catch logs; *NRE paths exist |
| Goal priority / conflict | OK | Max Value per tick; documented in 14 |
| Goal serialization | OK | Point/Area guard only; see 04 |
| Blur Speed | OK | 60 Midgard-top |
| Blur Attacks=1 vs manual | DEFER | data/docs |
| Cockatrice petrify gaze | BUG | ExStub; no skill |
| Werewolf bite lycanthropy | BUG | potion-only path |
| Fyleisch cloud skill | OK | fog + EnterFog damage |
| Fyleisch continuous half-HP | DEFER | enter-based only |
| Trader services | OK | TraderBrain + Trade |
| Trader/goal null house/Enemy | BUG | NRE risk |

## Filed bugs

| Issue | Title |
|-------|-------|
| (umbrella) | Creature AI / specials bug fixes |
| (child) | Cockatrice gaze petrify never fires |
| (child) | Werewolf bite does not apply lycanthropy |
| (child) | AI goals NRE on null house / Enemy / Leader / Debtor |

*(Numbers filled after `gh issue create`.)*
