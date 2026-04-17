# Build-Architektur (Kurz)

## Ziel
Schnelle Orientierung fuer Build, Packaging und typische Fehlerbilder.

## Build-Einstiegspunkte
- Root: `CMakeLists.txt` (globale Optionen, Dependencies, Packaging, `add_subdirectory(docs/src)`).
- App-Build: `src/CMakeLists.txt` (Quellgruppen, Target `openxcom`, Linkage, Datenkopie).
- VS/MSBuild: `src/OpenXcom.2010.sln`, `src/OpenXcom.2010.vcxproj`.
- Installer/Packaging:
  - CPack-Konfiguration in Root-`CMakeLists.txt`
  - Windows NSIS: `install/win/installer.nsi`
  - Linux distro packaging: `install/debian/`, `install/opensuse/`, `install/gentoo/`

## Wichtige CMake-Optionen (Auszug)
- `DEV_BUILD`: Development-Build-Verhalten.
- `BUILD_PACKAGE`: aktiviert Paketvorbereitung (CPack).
- `EMBED_ASSETS`: bettet `bin/common` + `bin/standard` in Artefakte ein.
- `DATADIR`: setzt Suchpfad fuer Data-Files zur Compilezeit.
- `FATAL_WARNING`: Warnings als Errors.
- `FORCE_INSTALL_DATA_TO_BIN`: erzwingt Dateninstallation in `bin`.

## Plattform-Hinweise
- **Windows**
  - nutzt oft `deps/` fuer Include/Lib/DLL.
  - Post-Build kopiert DLLs nach Build-Ausgabe.
- **Linux/Unix**
  - Abhaengigkeiten ueber `pkg-config` (SDL 1.2 Familie, zlib, etc.).
- **macOS**
  - Buildpfad in `README-macOS.md` beschrieben.
  - Optional App-Bundle-Flow via CMake/CPack.

## Datenkopie im Build
- Ohne `EMBED_ASSETS` werden `bin/TFTD`, `bin/UFO`, `bin/common`, `bin/standard` in die Build-Ausgabe kopiert.
- Das ist haeufige Fehlerquelle bei "laeuft, aber Inhalte fehlen".

## Typische Fehlerbilder und First Checks
1. **CMake findet SDL/OpenGL nicht**
   - Pruefe `deps/` (Windows) oder `pkg-config`-Pfad (Unix).
2. **Executable startet, aber Assets fehlen**
   - Pruefe, ob Datenordner in Ausgabeverzeichnis kopiert wurden.
3. **Packaging-Name/Version unklar**
   - Root-`CMakeLists.txt`: Git-Versionsermittlung + CPack-Generator.
4. **Windows Linkerfehler**
   - VS-Konfiguration (Win32/x64) und passende `deps/lib/*` pruefen.

## Update-Regel
- Nur dauerhafte Build-Erkenntnisse dokumentieren.
- Keine einmaligen CI-Ausfaelle oder lokale Maschinenprobleme aufnehmen.
