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
  src/Acom.Core      Spiel + Engine, plattformunabhängig (Ordner spiegeln src/, ein Namespace `Acom` wie C++ `OpenXcom`)
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

## Gerüste (generierte API-Stubs)
`tools/gen/gen_skeleton.py` (libclang, `pip install libclang==18.1.1`) liest alle Header und schreibt für jede offene Unit `Acom.Core/<Bereich>/<Name>.cs` mit der öffentlichen/geschützten API; Rümpfe werfen `NotImplementedException`, Kopfzeile `// Port status: stub`. So kompiliert der Port jederzeit, und Units lassen sich unabhängig (auch parallel) portieren.
- **Portieren = Gerüst ersetzen:** Signaturen beibehalten, private Member ergänzen, Kopf auf `partial`/fertig setzen. Muss eine Signatur abweichen, Aufrufer mitziehen. Neu erzeugen überschreibt nur Dateien mit Stub-Markierung.
- **Namen:** `getX()`/`isX()`/`hasX()` ohne Parameter → Property `X`/`IsX`/`HasX`, mit `setX(T)` als Setter; sonst Methoden in PascalCase; Felder/Konstanten behalten C++-Namen; geerbte Member folgen der Basisklasse.
- **Typen:** `vector`/`list`/`deque` → `List<T>`, `map` mit Wert-Schlüssel → `SortedDictionary` (Reihenfolge wie C++), mit Zeiger-Schlüssel → `Dictionary`, `pair` → `(A first, B second)`, `Uint8` → `byte`, `size_t` → `int`, `SDL_Color` → `Rgba`. Werttypen (`struct`): `VALUE_TYPES` im Generator (z. B. `Position`, `UnitStats`); alle anderen C++-Klassen werden Klassen, Kopien also explizit.
- **Nullbarkeit:** Spielcode außerhalb von `Engine/` beginnt mit `#nullable disable` (Zeiger wie in C++); Engine-Code bleibt annotiert.
- **Ohne Gerüst:** Skript-Engine (`Engine/Script*`, `Mod/ModScript`), von Hand portiert. Skript-Member (`ScriptRegister`, `ScriptFill`, Skriptwerte) fehlen in den Gerüsten und stehen in `<Klasse>.Script.cs`-Partials.
- Ein Namespace `Acom` für alles (wie C++ `OpenXcom`); getrennte Namespaces kollidierten mit Klassennamen (`Mod`, `Ufopaedia`).

## Skript-Engine
`Engine/Script.cs`, `Script.Parse.cs`, `ScriptBind.cs` (Tokenizer, Parser, Operationen, Tags, Events) und `Mod/ModScript.cs` (alle Parser/Gruppen) verhalten sich wie `Script.cpp`, inkl. Fehlermeldungen und bekannter C++-Eigenheiten (im Code kommentiert).
- **Aufbau:** Programm = `ScriptInstr[]` (Delegate + Operanden); Register per C++-Byte-Offset (64-Bit-Layout) in `int[]`/`object[]`; `ScriptParam` beschreibt die C++-Argumenttypen. Bindings registriert `ModScript.RegisterBindTypes` (Typ → Skriptname + `ScriptRegister`).
- **Hooks:** `ModScript.Execute/ScriptCallback/ScriptFunc1/ScriptFunc2` wie in C++; leere Container werden übersprungen. Regeln ohne geladene Skripte (nicht aus Rulesets) nutzen inline die eingebauten Default-Skripte (Psi/Nahkampf, Dampfpartikel, Sprites).
- **Prüfung:** `ScriptGoldenTests` gegen kompiliertes `Script.cpp` (`tools/golden/script.cpp`); `ModScriptTests` vergleicht die Metadaten aller Parser (Typen, Register, Konstanten, Funktionssignaturen) mit dem Debug-Log des C++-Spiels (`Golden/script-metadata.txt.gz`). Gleichnamige Überladungen sortiert `StdSort` wie libstdc++ `std::sort`.

## YAML
`Engine/Yaml.cs` liest mit dem Event-Parser von YamlDotNet in einen eigenen Knotenbaum, damit das Verhalten von rapidyaml erhalten bleibt: doppelte Schlüssel erlaubt (erster Treffer gewinnt), Merge-Schlüssel `<<` wie `Tree::resolve()`, Zahlen/Bools wie c4core (`0x`, `True`, ...). Die Selbsttests aus `src/Engine/Yaml.cpp` sind in `YamlTests` übernommen; `RepoYamlTests` liest alle `.rul`/`.yml` des Repos.

## Bilder laden
`Engine/ImageDecoder.cs` ersetzt lodepng und SDL_image, weil gängige .NET-Bibliotheken nach RGBA wandeln und die Palettenindizes verlieren. Wie `Surface::loadImage`: `.png` mit 8 Bit pro Pixel nach lodepng-Regeln (erster Paletteneintrag mit Alpha 0 wird transparent), alles andere nach SDL_image-Regeln (PNG mit 1–16 Bit Palette/Grau, GIF, BMP mit 1/4/8 Bit inkl. RLE4/RLE8, IFF-LBM als ILBM/PBM mit ByteRun1; Colour Key wird transparent). Golden-Test: `tools/golden/images.cpp` lädt alle Repo-Bilder und die synthetischen Testbilder (`tools/golden/make_test_images.py`) mit lodepng/SDL_image. Paletteneinträge, die die Datei nicht setzt, zählen nicht mit (SDL 1.2: schwarz, sdl12-compat: weiß; der Port nimmt schwarz).
`Engine/PngEncoder.cs` ersetzt `lodepng::encode` (Screenshots, Debug-Dumps der Battlescape): Palette (PLTE/tRNS) und RGB, gleiche Pixel, andere Bytes.

## Generierter Code
`Options.Values.cs` und `Options.Create.cs` erzeugt `port/tools/gen/gen_options.py` aus `Options.inc.h`/`Options.cpp` (C-Präprozessor mit Desktop-Defines). Neue Optionen im C++-Stil dort ergänzen und neu generieren, nicht von Hand editieren.

## Hauptschleife
C++ `Game::run()` wird aufgeteilt, weil MonoGame die Schleife besitzt:
- `Game.Update()` – Zustände aufräumen, aktiven State initialisieren, Ereignisse abarbeiten, `think()`.
- `Game.Render()` – ab oberstem Vollbild-State blitten, `Screen.Flip()` (Palette → RGBA).

Wie im Original läuft die Logik jeden Durchlauf, das Zeichnen darf ausfallen. Taktung wie `Game::run`: Ein MonoGame-Tick ist ein Durchlauf (`IsFixedTimeStep = false`); `Game.FrameDue` sagt, ob das Bildbudget (`1000 / Options::FPS` bzw. `FPSInactive` ohne Fokus, 0 bei OpenGL+vSync) abgelaufen ist, danach wartet der Host `Game.LoopDelay` (SDL_Delay: 1 ms, verlangsamt/pausiert 100 ms). Eingaben kommen über einen SDL2-Event-Watch (`SdlInput`), weil MonoGames Polling Klicks verliert, die kürzer als ein Frame sind.

## HD-Konzept
- **Logische Koordinaten bleiben:** Layout, Rulesets und Mods arbeiten weiter im 320×200-Raster (bzw. der konfigurierten Basisauflösung).
- **Pixeldichte pro Surface (`Surface.Scale`):** Der Puffer hat `Breite×Scale × Höhe×Scale` Pixel. Klassische Grafik = 1, HD = z. B. 4 (Bildschirm 1280×800). Beim Blitten zwischen Dichten wird per Nearest-Neighbour abgebildet; alte Grafiken und Mods funktionieren also weiter.
- **Palettenmodell bleibt (8 Bit, 16 Farbgruppen × 16 Schattierungen):** Schattierung, Umfärben (`blitNShade`), Invertieren und Palettenwechsel arbeiten auf Indizes und bleiben exakt wie im Original. HD-Grafiken sind daher **hochauflösende Pixelart mit 256-Farben-Palette**. Erst `Screen.Flip()` wandelt in Echtfarben um, die GPU skaliert aufs Fenster.
- **Spätere Erweiterung (nach dem Port):** eine Echtfarb-Ebene für einzelne Grafiken (Hintergründe, Porträts), deren Schattierung über Paletten-Rampen nachgebildet wird. Bis dahin gilt das Palettenmodell für alle Grafiken.

## Bewusste Abweichungen vom Original
| Stelle | Original | ACOM | Grund |
|---|---|---|---|
| Benutzer-/Konfig-/Datenordner | `openxcom`/`OpenXcom` | `acom`/`ACOM` | Eigenes Projekt, keine Vermischung mit OpenXcom-Installationen |
| Ordnerinhalte (`getFolderContents`) | Reihenfolge des Dateisystems (Linux zufällig) | sortiert, Groß/Klein ignoriert (wie NTFS) | Deterministische Ruleset-Reihenfolge auf allen Systemen |
| `oxceUpdateCheck`, `startUpdateProcess` | nur Windows, prüft OXCE-Server und startet den Updater | entfällt | Kein Update-Check gegen fremde Server |
| Palette `setColors`/`setColor` | +1 auf doppelte Schwarztöne | entfällt | Workaround für SDL-1.2-Farbabgleich, der hier nicht stattfindet |
| YAML-Suche mit Duplikaten | ohne Index: Suche ab letzter Fundstelle | immer erster Treffer | Einfacher, deckt sich mit der indizierten Suche des Originals |
| Eingebettete Assets (`exe:common.zip`/`exe:standard.zip`, `getEmbeddedAsset`) | optional (`EMBED_ASSETS`) | entfällt; `oxceEmbeddedOnly` wirkungslos | Inhalte kommen als Dateien/Zip |
| Absturzbericht (`crashDump`/`stackTrace`) | Windows-Minidump bzw. native Backtrace, Text nennt OpenXcom-Forum | .NET-Ausnahme und Stacktrace ins Log, Text nennt ACOM; Fehlerfenster unter Windows über SDL (`ErrorDialogHandler`) | Kein nativer Code; eigenes Projekt |
| Bildformate (`loadImage`) | alles, was SDL_image kann (auch PCX, TGA, PNM, XPM, XCF, ...) | PNG, GIF, BMP, LBM | Mods nutzen praktisch nur PNG/GIF (BMP/LBM aus dem Originalumfeld); weitere Leser bei Bedarf |
| LBM mit 2, 3, 5–7 Bitebenen | SDL_image 1.2.12 ohne Patch: lesbar | abgelehnt („unsupported number of color planes“) | Wie die Sicherheitskorrektur in Debians SDL_image 1.2 und SDL2_image ≥ 2.0.5 (Referenz der Golden-Tests) |
| Palettenalpha auf dem SDL_image-Weg | SDL 1.2: `unused` = 0, sdl12-compat: 255 | 255 | Byte ist in SDL 1.2 unbenutzt; Transparenz kommt dort aus dem Colour Key |
| `Timer`-Spielzeit (`slowTick`) | erster Aufruf springt einmalig, 32-Bit-Überlauf nach 74 h | gleichmäßig, 64 Bit | Offensichtlicher Fehler ohne gewollte Wirkung |
| Font-Zeichen außerhalb der BMP (z. B. Emoji) | eigene Glyphe | belegen ihren Platz, sind aber nicht abrufbar | Text ist im Port UTF-16 (`char`) |
| Abgeschnittene GIF-Dateien | SDL_image hängt in Erweiterungen bzw. liest alte Pufferbytes | Fehler bzw. Bildende | Kein Hänger, kein Datenmüll |
| Container mit Zeiger-Schlüsseln (`std::map<T*,…>`, `std::set<T*>`) | Reihenfolge nach Speicheradresse | Einfügereihenfolge (Regel-Listen nach Mod-Reihenfolge) | C++-Reihenfolge ist nicht reproduzierbar |
| `std::sort` bei gleichen Schlüsseln | instabil | stabil, bzw. Nachbau der libstdc++-Sortierung (`StdAlgorithm`, Pfadsuche/Listen) | Deterministisch; gleiche Ergebnisse wie der GCC-Build wo es zählt |
| Undefiniertes Verhalten in C++ (Lesen hinter Listenende, ungültige Iteratoren, 64-Bit-`size_t`-Vergleiche) | Absturz oder Datenmüll | Ausnahme bzw. Begrenzung auf gültigen Bereich (im Code kommentiert) | Kein Absturz bei gültigen Daten gleich |
| Locale (`CrossPlatform::getLocale`) | Linux-Zweig schneidet `de_DE.UTF-8` falsch ab | `LC_ALL`/`LC_MESSAGES`/`LANG` auf allen Systemen | Offensichtlicher Fehler |
| Spielstand-Kopf `build` | Git-Version von OXCE | `ACOM <Version>` | Eigenes Projekt; alle Schlüssel/Reihenfolgen sonst gleich |
| Natürliche Sortierung von Namenslisten (`.nam`) | `StrCmpLogicalW` bzw. Locale | eigene deterministische natürliche Sortierung | Gleiches Ergebnis auf allen Systemen |
| Nicht initialisierte C++-Felder (z. B. Zähler in Mission-/Arc-Skripten) | unbestimmt | 0 bzw. -1 | Definierter Startwert |
| YAML-Gleitkommazahlen | c4core/fast_float direkt nach `float` | über `double` nach `float`, keine Hex-Floats | Praktisch gleiche Werte |
| Fenster/Anzeige (`Screen`) | SDL-Videomodus, Software-Skalierer | Host (MonoGame) besitzt das Fenster; Pixeldichte auf 2560×1600 begrenzt | GPU-Skalierung, HD-Konzept |
| Filter (`OptionsVideoState`) | Software-Skalierer (Scale/HQX/xBRZ), OpenGL-Shader | wählbar, aber ohne Wirkung; OpenGL-Glättung = lineare Filterung, vSync = `vSyncForOpenGL` | GPU skaliert; Shader-Programme (GLSL von SDL 1.2/OpenGL 2) nicht portiert |
| Screenshot (Taste `keyScreenshot`) | liest das Fensterbild (SDL-Surface bzw. `glReadPixels`) | Bild im Core per Nearest-Neighbour ins Fenster skaliert (mit Rändern) | Das GPU-Bild liegt nur im Host; ohne Glättung gleiche Pixel |
| Ladebildschirm (`StartState`) | eigener SDL-Thread | `Task` | Gleicher Ablauf, MonoGame besitzt die Schleife |
| Destruktoren mit Logik | `delete` | `State.Destroy()` (vom Game aufgerufen) bzw. `Target.Destroy()` | Kein deterministisches `delete` in .NET |

## Phasen
Fortschritt: `python3 port/tools/port-status.py` (Zeilen je Bereich, offen/teilweise/fertig).

| Phase | Inhalt | Prüfung |
|---|---|---|
| 0 ✅ | Gerüst, RNG, Pluralregeln, Palette, Surface (HD), Action, InteractiveSurface, State, Game-Loop, Screen, MonoGame-Host | Golden-Tests, Headless-Screenshot |
| 1 ✅ | Engine-Rest (Options, YAML, FileMap, Language, Font, PNG/GIF, Timer, Zeichenfunktionen, Audio mit Adlib-Emulation, FLC-Video) und `Interface/` | Golden-Tests (Optionen, Bilder, SDL_gfx, Widgets, Adlib/SDL_mixer) |
| 2 ✅ | `Mod/` (alle Rule*, `Mod.cpp`; alle `xcom1`-Regelwerke laden) und `Savegame/` (inkl. Konverter für Original-Spielstände) | YAML-Round-Trips, Ruleset-Ladetest |
| 3 ✅ | `Menu/`, `Geoscape/`, `Basescape/`, `Ufopaedia/` | Unit-Tests der Logik, End-to-End mit Platzhalterdaten |
| 4 ✅ | `Battlescape/` (Karte, Pfadsuche, Sichtlinien, KI, TileEngine, Generator, alle States) | Golden-Tests Pfadsuche/Flugbahnen |
| 5 ✅ | OXCE-Skript-Engine (`Engine/Script*`, `Mod/ModScript`, Bindings, Hooks) | Golden-Tests gegen `Script.cpp`, Metadaten-Vergleich aller Parser |
| parallel | Eigenes Inhaltspaket (Setting, Texte, HD-Grafik, Sound, Karten) | siehe `content-rights.md` |

## Offene Entscheidungen
- Keine. (Skript-Engine: wird 1:1 portiert, damit OXCE-Mods laufen; das Grundspiel `xcom1` nutzt sie nicht.)

## Befehle
```sh
cd port
dotnet build                                   # alles bauen (Warnungen = Fehler)
dotnet test --project tests/Acom.Core.Tests # Tests
dotnet run --project src/Acom.Desktop       # Spiel/Demo starten
xvfb-run -a dotnet run --project src/Acom.Desktop -- --screenshot out.png --frames 60   # headless prüfen
tools/golden/generate.sh                       # Golden-Dateien aus C++ neu erzeugen (g++, Dev-Pakete SDL 1.2, SDL_image, SDL_gfx, SDL_mixer)
python3 tools/port-status.py [Bereich]         # Fortschritt / offene Units
dotnet run --project tools/Acom.AssetSpec -- --data <Originaldaten> --game ufo   # Asset-Liste nach content/spec/ (nur Metadaten)
dotnet run --project tools/Acom.Placeholder -- --out /tmp/acom/UFO   # Platzhalter-Daten (dann: -data <repo>/bin -user /tmp/acom/), siehe README dort
```
