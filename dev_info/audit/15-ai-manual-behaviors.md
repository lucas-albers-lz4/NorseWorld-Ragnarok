# 15 — Manual creature behaviors vs code

Issue [#9](https://github.com/lucas-albers-lz4/NorseWorld-Ragnarok/issues/9).  
Sources: DOS manual / `languages/en_db.xml` lore vs `resources/RDatabase.xml` + combat/effect wiring.

## Blur — “fastest creature”

| Claim | Code |
|-------|------|
| Fastest | `Speed=60` in `RDatabase.xml` — top among Midgard-tier mobs; Aesir use 100 |
| Multiple attacks | **`Attacks=1`** — mismatch |
| Speedup corpse | `EffectsFactory` deadSign Blur → `e_Speedup` |
| Brain | SentientBrain (`esMind`) |

**Status:** Speed OK for Midgard; Attacks count DEFER (data/docs).

## Cockatrice — petrify gaze

| Claim | Code |
|-------|------|
| Gaze petrifies | `AttackSpecialEffect` / defense branches are **`ExStub` only** (`NWCreature.cs` ~1954–1955, ~2199+) |
| Skills | Entry Skills **empty**; has `Resist_Petrification` 100 |
| Petrify stack | `EffectID.eid_Stoning`, `StoningRay`, wand path exist — **not wired to Cockatrice** |
| Corpse | Touch/eat can kill non-resistant (`EffectsFactory`) |

**Status:** BUG — live gaze petrify missing.

## Werewolf — bite → lycanthropy

| Claim | Code |
|-------|------|
| Bite induces lycanthropy | No Werewolf branch in `AttackSpecialEffect` |
| Lycanthropy path | Potion `eid_Lycanthropy` → prowling → `InitEx(cid_Werewolf)` |
| Craft | `_Werewolf` + potion recipes |

**Status:** BUG — melee bite does not apply lycanthropy.

## Fyleisch — lethal mist

| Claim | Code |
|-------|------|
| Surrounded by mist | Skill `Sk_Fyleisch_Cloud` → `eid_Fyleisch_Cloud` → `FyleischCloud` fog tiles |
| Offence use | Via `GetAttackSkill` / `BeastBrain.Attack` when in range |
| Damage | `EnterFog` applies `ApplyDamage(1..40)` on tile enter if no `Resist_Acid` |
| Half HP / continuous aura | **Not matched** — enter-based, not half-HP per turn while standing |

**Status:** Cloud skill OK; continuous half-HP aura DEFER / known gap.

## Trader — friendly sells items

| Claim | Code |
|-------|------|
| Trader AI | `TraderBrain` when `IsTrader` |
| Services | SentientBrain: Teach, Trade, Exchange, Recruit |
| Shop return / debt / ware | TraderBrain goals + money/debt on `NWCreature` |
| Dialog | Merchant `_dlg_merchant.xml` |

**Status:** OK for trade services; house-null NRE risk under AI crash bugs.

## Checklist summary

| Manual behavior | Status |
|-----------------|--------|
| Blur Speed | OK |
| Blur multi-attack | DEFER |
| Cockatrice gaze | BUG |
| Werewolf bite lycanthropy | BUG |
| Fyleisch cloud skill | OK |
| Fyleisch continuous half-HP | DEFER |
| Trader sell/buy | OK |
