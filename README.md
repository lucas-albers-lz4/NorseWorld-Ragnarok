# NorseWorld: Ragnarok

Copyright (c) 2002-2008, Alchemist Team

The development of this game was begun at november 2002, 
as remake of Ragnarok game, created in 1992-1995
by Thomas Boyd and Robert Vawter.

In this roguelike-game, created on base of scandinavian mythology, 
you are brave viking, who must help the aces (the gods) to win in Ragnarok - 
the final battle against evil. This battle will determine the fate of nine worlds.

You begin in your village and can choose between the ways of viking warrior, woodsman, 
blacksmith, alchemist, conjurer or sage. Each way is different from others and has 
its own advantages and disadvantages, each new game is totally unique:
worlds and their levels, lands, creatures, items and even
merchants are randomly generated.

Playing the game you will pass through Midgard - the world of mortals, 
visit Jotenheim - the land of giants-jotuns, and Nidavellir - 
the labyrinth of caves, populated strange creatures, come down 
in dwarven great caves; you have a chance to visit lands, 
created by gods and greatest of ancient wizards, get through
terrible Niflheim - the world of deads and search the mighty 
artifacts of the gods. Then you'll go to Asgard - the world of gods, 
to take part in final battle against powers of evil.

## Common features

- 6 player classes: viking, woodsman, blacksmith, sage, conjurer and alchemist.
- More than 150 creature races.
- More than 200 item types.
- More than 230 different magic effects.
- More than 15 dynamically generated different worlds and 110 levels.
- Possibility to add new story branches, levels, creatures, items and artifacts.
- Complex game database editor (all game space objects and their attributes are available).
- Built-in dialog/event scripting via **Jint** (NPC conditions/actions on Mono; restored on this Linux fork).
- Support operating systems MS Windows and **Linux** (this fork: Mono + SDL2).
- Flat tiles and isometric views of design.
- Support for Russian and English (the correctness of the English localization ~90%).
- Possibility to change side for evil.

## Running on Linux (this fork)

Primary play path is the **C# Mono** build:

```bash
./play-cs.sh                 # build if needed + run
NWR_FORCE_BUILD=1 ./play-cs.sh
```

Requires `mono-complete`, SDL2 / SDL2_image / SDL2_mixer, and sibling repos `../BSLib` and `../ZRLib` (auto-cloned by `play-cs.sh` if missing).

Optional **Java v0.11** baseline (Wine), for A/B comparison only:

```bash
./dev_info/fetch-java-dist.sh
cd nwr-dist-v0.11.0-win && ./play.sh
```

A/B protocol and differential harness: [dev_info/ab-test-java-vs-cs.txt](dev_info/ab-test-java-vs-cs.txt) (`mono NWR.Tests.exe ab-diff` after fetch).

### Headless tests

```bash
./dev_info/run-tests.sh      # NUnit + integration scenarios
```

See [dev_info/test-harness.txt](dev_info/test-harness.txt).

### C# sound (SDL_mixer)

Audio uses **SDL2_mixer** (via ZRLib) and **NVorbis** for OGG sound effects. NVorbis needs `System.Memory.dll`, `System.Buffers.dll`, and `System.Runtime.CompilerServices.Unsafe.dll` beside `NWR.exe` (staged automatically by `play-cs.sh`). Install:

```bash
sudo apt install libsdl2-mixer-2.0-0
```

Options in-game: **Music** and **Sounds** volume sliders.

Place game audio next to `NWR.exe` at repo root after staging:

- `sfx/` — sound effects, ambient loops, and land songs (`sfx/songs/*.ogg`; gitignored)
- The official v0.10.0 soundpak is no longer hosted; use the **Tier C** placeholder pack:

```bash
./dev_info/generate-sfx-pack.sh
```

`play-cs.sh` auto-generates `sfx/` when missing (set `NWR_SKIP_SFX_GENERATE=1` to skip). See [dev_info/sfx-pack.txt](dev_info/sfx-pack.txt).

If you have a full release with real `sfx/`, copy or symlink it beside `NWR.exe`. `play-cs.sh` can symlink from `nwr-dist-v0.11.0-win/sfx` when that folder exists (the v0.11.0 Java dist does not ship audio).

## Authors

Project, coding and graphics processing:
  - Sergey Zhdanovskih (aka Alchemist, aka Norseman)

Manuals, graphics, audio, testing and support:
  - Dmitry Buzhinsky (Bu)
  - Gleb Buzhinsky (Quiet)

Dungeons generator development, FOV algoritm debugging:
  - Ruslan Garipov (Brigadir)

FOV algoritm improvement:
  - Dmitry Buzhinsky (Bu)

Linux-release and GUI-engine improvements:
  - Aerton
