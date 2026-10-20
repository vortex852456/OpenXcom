# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

@AGENTS.md

## Projektkontext
- Persoenlicher Fork von **OpenXcom Extended (OXCE)**, C++17 / SDL 1.2. Arbeitsbranch: `vortex-2025`.
- Eigene Aenderungen sind meist kleine Komfort-/Cheat-Optionen (`oxce*`), Commit-Praefixe `cheat:`, `fix:`, `docs:`.
- Architektur-Detailwissen: `docs/architecture/README.md`, `build.md`, `runtime-paths.md`, `csharp-port.md`, `content-rights.md` (dort pflegen, nicht hier).

## C#-Port (`port/`)
GPLv3-Port nach .NET 10 + MonoGame (Arbeitstitel „Bulwark“), Details: `docs/architecture/csharp-port.md`.
```sh
cd port
dotnet build                                    # Warnungen sind Fehler
dotnet test --project tests/Bulwark.Core.Tests  # xUnit v3 via Microsoft.Testing.Platform (global.json)
dotnet test --project tests/Bulwark.Core.Tests --filter-method "*RngTests*"   # einzelne Tests
xvfb-run -a dotnet run --project src/Bulwark.Desktop -- --screenshot out.png --frames 60   # headless Screenshot
tools/golden/generate.sh                        # Golden-Referenzwerte aus dem C++-Original neu erzeugen
python3 tools/port-status.py [Bereich]          # Fortschritt / offene C++-Units
```
- .NET 10 SDK im Container: `apt-get install dotnet-sdk-10.0` (dot.net-Downloads sind geblockt).
- Jede portierte Datei: GPL-Header (`port/tools/license-header.txt`) + `// Ported from: src/...`, bei Lücken `// Port status: partial (missing: ...)`.
- Verhalten 1:1 zum C++-Code; deterministische Logik per Golden-Test gegen das Original prüfen.
- `Bulwark.Core` ohne MonoGame-Abhängigkeit; Plattformcode nur in `Bulwark.Desktop`.

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
