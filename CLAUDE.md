# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

@AGENTS.md

## Projektkontext
- Persoenlicher Fork von **OpenXcom Extended (OXCE)**, C++17 / SDL 1.2. Arbeitsbranch: `vortex-2025`.
- Eigene Aenderungen sind meist kleine Komfort-/Cheat-Optionen (`oxce*`), Commit-Praefixe `cheat:`, `fix:`, `docs:`.
- Architektur-Detailwissen: `docs/architecture/README.md`, `build.md`, `runtime-paths.md` (dort pflegen, nicht hier).

## Build (Linux)
```sh
cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release   # einmalig, ~2 s
cmake --build build -j$(nproc)                            # Vollbuild, mehrere Minuten
ninja -C build src/CMakeFiles/openxcom.dir/Savegame/Ufo.cpp.o   # nur eine Datei kompilieren (~10 s)
```
- Ergebnis: `build/bin/openxcom`; `bin/{UFO,TFTD,common,standard}` werden nach `build/bin/` kopiert.
- Schnellster Check nach einer Aenderung: betroffene `.o`-Targets einzeln bauen statt Vollbuild.
- `-DFATAL_WARNING=ON` macht Warnings zu Errors (GCC/Clang bauen mit `-Wall -Wextra`).

## Tests / Verifikation
- Es gibt **keine Unit-Tests** und keine aktive CI (`.github/workflows/nightly.test` ist durch die Endung deaktiviert, baut nur Windows via MSBuild).
- Verifikation = erfolgreicher Compile. Spielstart braucht Original-UFO/TFTD-Daten, die nicht im Repo sind (`bin/UFO`, `bin/TFTD` enthalten nur `README.txt`).

## Architektur-Leitplanken (dateiuebergreifend)
- **State-Stack:** UI/Spiellogik laeuft in `State`-Subklassen (`src/Engine/State.h`: `init/handle/think/blit`), verwaltet ueber `Game::pushState/popState/setState`. Features leben meist im passenden `*State` des Subsystems (`Geoscape/`, `Battlescape/`, `Basescape/`, `Menu/`).
- **Regeln vs. Laufzeitzustand:** `src/Mod/Rule*` = statische Definitionen aus `.rul`-YAML (geladen in `Mod::loadAll`, rapidyaml); `src/Savegame/*` = veraenderlicher Spielzustand mit Serialisierung (z. B. `Ufo` referenziert `RuleUfo`).
- **Mod-Scripting:** OXCE-Skriptengine in `src/Engine/Script*.h` + `src/Mod/ModScript.h`; Bindings je Klasse via statischem `ScriptRegister(ScriptParserBase*)` (z. B. `Ufo`, `RuleUfo`).

## Neue Option hinzufuegen (bewaehrtes Muster)
1. Deklarieren: `OPT bool oxceFoo;` in `src/Engine/Options.inc.h`.
2. Registrieren in `Options::create()` (`src/Engine/Options.cpp`):
   `_info.push_back(OptionInfo(OPTION_OXCE, "oxceFoo", &oxceFoo, false));`
   - Ohne Beschreibung/Kategorie = nur per Config-Datei setzbar.
   - Mit `"STR_FOO", "STR_GENERAL"` (bzw. `STR_GEOSCAPE`, `STR_BATTLESCAPE`, ...) erscheint sie unter Optionen → Advanced → OXCE; Kategorie `"HIDDEN"` blendet sie aus; Strings dann in `bin/common/Language/OXCE/en-US.yml` (weitere Sprachen dort optional).
3. Verwenden: `if (Options::oxceFoo) ...`.
- Jede Option ist auch per Kommandozeile ueberschreibbar: `openxcom -oxceFoo true`.

## Fallstricke
- **SDL 1.2**, nicht SDL2: z. B. `SDLKey` statt `SDL_Keycode`.
- Neue `.cpp/.h` muessen in **zwei** Build-Systeme: passende `*_src`-Liste in `src/CMakeLists.txt` **und** `src/OpenXcom.2010.vcxproj` (+ `.vcxproj.filters`).
- Stil: Tabs, Allman-Klammern (`.editorconfig`, `.clang-format`, `.astylerc`). Keine Fremdzeilen umformatieren.
- `.rul`/`.yml`: 2 Leerzeichen Einrueckung.
- Upstream-OXCE nimmt keine PRs ohne vorherige Forum-Diskussion an (`.github/pull_request_template.md`).
