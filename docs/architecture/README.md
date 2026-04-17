# Architekturueberblick (Initial)

## Zweck
Diese Datei ist die kompakte Architektur-Referenz fuer `OpenXcomExtended`.
Sie soll Orientierung geben, ohne in API-Details zu gehen.

## Systemgrenzen
- **Code:** `src/`
- **Laufzeitdaten und Standardinhalte:** `bin/`
- **Build-/Packaging-Logik:** `CMakeLists.txt`, `src/CMakeLists.txt`, `install/`, `cmake/`
- **Ergaenzende Doku:** `README.md`, `README-macOS.md`, `docs/`

## High-Level-Komponenten
- `src/Engine`: technische Laufzeitbasis (Game-Loop, Rendering, Audio, Optionen, YAML/Scripting-Helfer).
- `src/Mod`: Regeln/Definitionsobjekte (`Rule*`) und Mod-Lade-/Verarbeitungslogik.
- `src/Savegame`: persistente Kampagnen-/Gefechtsdaten und Serialisierung.
- `src/Basescape`: Basisverwaltungs-Gameplay und zugehoerige UI-States.
- `src/Geoscape`: globale Strategieebene (Globus, Interception, Events, Reports).
- `src/Battlescape`: taktische Missionen, KI, Pathfinding, Kampf-UI.
- `src/Menu`: Hauptmenue, Optionen, Laden/Speichern, Startfluesse.
- `src/Interface`: generische UI-Bausteine (Buttons, Listen, Fenster, Text, Slider).
- `src/Ufopaedia`: Ingame-Enzyklopaedie-Ansichten und Artikel-States.

## Typischer Datenfluss (vereinfacht)
1. Start in `src/main.cpp` und Kerninitialisierung in `src/Engine`.
2. Regeln/Content werden ueber `src/Mod` eingelesen.
3. Runtime-Zustaende laufen in `*scape`/`Menu`-States.
4. Persistenz von Kampagnen/Gefechten ueber `src/Savegame`.
5. UI-Darstellung via `src/Interface` + subsystemspezifische States.

## Build- und Packaging-Architektur
- Root-`CMakeLists.txt` definiert globale Optionen (u. a. C++17, Packaging, Plattformflags) und bindet `docs` + `src` ein.
- `src/CMakeLists.txt` aggregiert Subsystem-Quellen und erstellt `openxcom`.
- Laufzeitdaten aus `bin/{TFTD,UFO,common,standard}` werden in Build/Install kopiert (wenn nicht eingebettet).
- Windows-spezifisch zusaetzlich Visual-Studio-Projektdateien in `src/` und NSIS-Installer unter `install/win/`.

## Externe Abhaengigkeiten (Auszug)
- SDL 1.2 Familie (`SDL`, `SDL_image`, `SDL_mixer`, `SDL_gfx`)
- OpenGL (optional, je nach Plattform/Fund)
- Vendor-Libs im Repo: `libs/rapidyaml`, `libs/miniz`, `libs/cereal`
- Windows-Prebuilt-Struktur: `deps/include`, `deps/lib`

## Laufzeitdaten und Inhalte
- `bin/common`: gemeinsame Ressourcen (u. a. Language, Palettes, weitere Assets).
- `bin/standard`: mitgelieferte Standard-Mods/Regelerweiterungen.
- `bin/UFO` und `bin/TFTD`: Spiel-Datenstruktur fuer Basisspiele.

## Dokumentierte Suchpfade fuer User/Config/Data
- Projektweit in `README.md` unter "Directory Locations" beschrieben.
- Wichtig fuer Debugging: Probleme sind oft Pfad-/Datenordner-bezogen, nicht rein C++-Logik.

## Bekannte Architekturprinzipien (aus Struktur abgeleitet)
- Starke Trennung zwischen Engine, Regeln/Modell, Savegame und UI-States.
- Feature-Logik liegt oft in zustandsspezifischen Klassen (`*State`).
- Content- und Regelanpassungen sind stark datengetrieben (`.rul`/YAML-nahe Verarbeitung).

## Pflegehinweise
- Diese Datei bleibt bewusst kurz.
- Bei neuen Erkenntnissen nur dauerhafte, repo-weite Aussagen aufnehmen.
- Tiefere Details in neue, kleine Unterdokumente auslagern (z. B. `docs/architecture/build.md`).

## Weiterlesen
- Build, Packaging, typische Build-Fehler: `docs/architecture/build.md`
- Runtime-Pfade und schnelle Laufzeit-Diagnose: `docs/architecture/runtime-paths.md`
