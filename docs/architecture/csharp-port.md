# C#-Port: ACOM

## Entscheidungen (2026-10)
| Thema | Entscheidung | Folge |
|---|---|---|
| Lizenz | GPLv3-Port (GPL-3.0-or-later) | C++-Code darf 1:1 übersetzt werden; Quellcode bleibt offen. Copyright-Hinweise der OpenXcom-Entwickler bleiben in jeder Datei. |
| Technik | .NET 10 + MonoGame DesktopGL | Läuft auf Windows/Linux/macOS; Mobilgeräte später möglich. |
| Inhalte | OXCE-Datenformat, eigenes Setting | `.rul`/YAML, PNG/GIF, MAP/RMP bleiben ladbar; Inhalte, Namen und Texte werden neu erstellt (siehe `content-rights.md`). |
| Grafik | HD als Pixelart mit 256-Farben-Palette (bestätigt) | Logisches 320×200-Layout bleibt, Grafiken in höherer Pixeldichte (siehe unten). |
| Name | **ACOM** (Namespaces `Acom.*`, Programm `acom`) | Untertitel ohne Fremdmarke wählen, siehe `content-rights.md`. |

## Struktur
```
port/
  Acom.slnx, Directory.Build.props, Directory.Packages.props, global.json
  src/Acom.Core      Spiel + Engine, plattformunabhängig (Namespaces spiegeln src/: Acom.Engine, später Acom.Mod, ...)
  src/Acom.Desktop   MonoGame-Host: Fenster, Hauptschleife, SDL2-Eingabe, GPU-Ausgabe
  tests/Acom.Core.Tests  xUnit v3 (Microsoft.Testing.Platform), Golden-Dateien unter Golden/
  tools/                golden/ (C++-Referenz), port-status.py, port-skip.txt, license-header.txt
```
- **Ein Core-Assembly statt eines pro Ordner:** `Engine`, `Mod`, `Savegame` und `Battlescape` inkludieren sich in C++ gegenseitig (z. B. Engine → Savegame, Savegame → Battlescape). Assemblies dürfen keine Zyklen haben.
- `Acom.Core` kennt MonoGame nicht. Der Host übergibt Eingaben als SDL-1.2-artige Ereignisse und bekommt fertige RGBA-Frames.

## Portierungsregeln
1. **Verhalten vor Idiomatik.** Erst gleich verhalten wie C++, dann aufräumen. Abweichungen im Code kommentieren.
2. **Dateikopf:** GPL-Header (`port/tools/license-header.txt`) + `// Ported from: src/...` + bei Lücken `// Port status: partial (missing: ...)`. `port/tools/port-status.py` wertet das aus.
3. **Namen:** Klassen-/Dateinamen wie in C++. `getFoo()`/`setFoo()` → Property `Foo`; Methoden PascalCase. Protected Felder (`_x`, `_surfaces`) und C++/SDL-Konstanten (`ORIGINAL_WIDTH`, `SDLK_*`) bleiben (Analyzer-Ausnahmen in `port/.editorconfig`).
4. **SDL-Nachbildung:** `SDL_Event`, `SDLKey`, `SDLMod`, `SDL_Rect` haben die SDL-1.2-Feldnamen und -Zahlenwerte. So bleibt Eventcode beim Übersetzen fast unverändert, und Tastencodes in bestehenden `options.cfg` bleiben gültig.
5. **Golden-Tests:** Deterministische Logik (Zufall, Regeln, Pfadsuche, Sichtlinien, Saves) wird gegen das Original geprüft. `port/tools/golden/golden.cpp` kompiliert die Original-Quellen und schreibt Referenzwerte nach `tests/.../Golden/`; die C#-Tests vergleichen bitgenau.
6. **Bewusst nicht portiert:** steht mit Begründung in `port/tools/port-skip.txt` (z. B. Software-Scaler, OpenGL, Template-Blitmaschinerie).

## Hauptschleife
C++ `Game::run()` wird aufgeteilt, weil MonoGame die Schleife besitzt:
- `Game.Update()` – Zustände aufräumen, aktiven State initialisieren, Ereignisse abarbeiten, `think()`.
- `Game.Render()` – ab oberstem Vollbild-State blitten, `Screen.Flip()` (Palette → RGBA).

Wie im Original läuft die Logik jeden Durchlauf, das Zeichnen darf ausfallen. Eingaben kommen über einen SDL2-Event-Watch (`SdlInput`), weil MonoGames Polling Klicks verliert, die kürzer als ein Frame sind.

## HD-Konzept
- **Logische Koordinaten bleiben:** Layout, Rulesets und Mods arbeiten weiter im 320×200-Raster (bzw. der konfigurierten Basisauflösung).
- **Pixeldichte pro Surface (`Surface.Scale`):** Der Puffer hat `Breite×Scale × Höhe×Scale` Pixel. Klassische Grafik = 1, HD = z. B. 4 (Bildschirm 1280×800). Beim Blitten zwischen Dichten wird per Nearest-Neighbour abgebildet; alte Grafiken und Mods funktionieren also weiter.
- **Palettenmodell bleibt (8 Bit, 16 Farbgruppen × 16 Schattierungen):** Schattierung, Umfärben (`blitNShade`), Invertieren und Palettenwechsel arbeiten auf Indizes und bleiben exakt wie im Original. HD-Grafiken sind daher **hochauflösende Pixelart mit 256-Farben-Palette**. Erst `Screen.Flip()` wandelt in Echtfarben um, die GPU skaliert aufs Fenster.
- **Spätere Erweiterung (nach dem Port):** eine Echtfarb-Ebene für einzelne Grafiken (Hintergründe, Porträts), deren Schattierung über Paletten-Rampen nachgebildet wird. Bis dahin gilt das Palettenmodell für alle Grafiken.

## Phasen
Fortschritt: `python3 port/tools/port-status.py` (Zeilen je Bereich, offen/teilweise/fertig).

| Phase | Inhalt | Prüfung |
|---|---|---|
| 0 ✅ | Gerüst, RNG, Pluralregeln, Palette, Surface (HD), Action, InteractiveSurface, State, Game-Loop, Screen, MonoGame-Host | Golden-Tests, Headless-Screenshot |
| 1 | Engine-Rest: Logger, Options/OptionInfo (cfg-kompatibel), CrossPlatform (Pfade), FileMap (Mod-Dateisystem), YAML-Schicht, Language/Unicode/LocalizedText, Font, SurfaceSet + PNG/GIF mit Paletten-Indizes, Timer, Sound/Music; danach `Interface/` (Widgets) | Golden-Tests für Optionen/Text, Widget-Tests |
| 2 | `Mod/` (Rule*, Laden/Mergen von Rulesets) und `Savegame/` (Modell + YAML-Saves) | Rulesets/Saves in C++ und C# laden, Dumps vergleichen |
| 3 | `Menu/`, `Geoscape/`, `Basescape/`, `Ufopaedia/` | Bedienbare Kampagne mit Testinhalten |
| 4 | `Battlescape/` (Karte, Pfadsuche, Sichtlinien, KI, TileEngine) | Golden-Tests Pfadsuche/LOS/Schaden mit festem Seed |
| 5 | OXCE-Skript-Engine (`Engine/Script*`, ~9k Zeilen) | Mod-Skripte aus Testmods |
| parallel | Eigenes Inhaltspaket (Setting, Texte, HD-Grafik, Sound, Karten) | siehe `content-rights.md` |

## Offene Entscheidungen
- PNG/GIF-Decoder, der Palettenindizes erhält (eigener Decoder oder Bibliothek mit kompatibler Lizenz).
- YAML: YamlDotNet vs. eigener Reader mit rapidyaml-Semantik (Merge-Verhalten der Rulesets muss gleich bleiben).
- Skript-Engine: portieren oder ersetzen.

## Befehle
```sh
cd port
dotnet build                                   # alles bauen (Warnungen = Fehler)
dotnet test --project tests/Acom.Core.Tests # Tests
dotnet run --project src/Acom.Desktop       # Spiel/Demo starten
xvfb-run -a dotnet run --project src/Acom.Desktop -- --screenshot out.png --frames 60   # headless prüfen
tools/golden/generate.sh                       # Golden-Dateien aus C++ neu erzeugen (g++, SDL-1.2-Header)
python3 tools/port-status.py [Bereich]         # Fortschritt / offene Units
```
