Scenario JSON fixtures
======================
Declarative scenarios for the Scenario DSL (#13). Run via:

  mono NWR.Tests.exe dsl-json-fixtures
  mono NWR.Harness.exe dsl-json-fixtures

Or as part of --all (NWR.Tests).

Format: { "name": "...", "steps": [ { "op": "...", "arg": "...", "n": 1, "flag": true } ] }

Ops: loadFixture, spawnItem, spawnCreature, useItem, applyEffect, waitTurns,
     saveGame, loadGame, saveLoadRoundtrip, capture, check, requireLog, halfHp, param.

Named checks: turnAdvanced, hpIncreased, effectsUnchanged, effectPresent, effectAbsent.
Params: ${name} substitution after param steps.
