# Asset-Spezifikation (aus den Originaldaten abgeleitet)

Dieser Ordner beschreibt, **welche Grafiken, Sounds und Karten** ACOM braucht – als Grundlage für eigene (auch KI-erzeugte) Inhalte.
Er enthält ausschließlich technische Fakten, die das Werkzeug `port/tools/Acom.AssetSpec` aus lokal vorhandenen Originaldaten liest:

- Bildgrößen, Anzahl Frames, belegter Bereich je Frame, verwendete Palettengruppen (Index ÷ 16)
- Kartenblock-Größen, Anzahl Wegpunkte, Anzahl Geländekacheln
- Anzahl, Format und Länge von Soundeffekten
- welche Ruleset-Einträge (`bin/standard/xcom1` bzw. `xcom2`) welchen Frame/Sound benutzen
- HD-Zielgrößen (klassische Größe × 4)

**Nicht enthalten:** Pixel, Farbwerte, Texte, Töne oder Originaldateien. Die Originaldaten bleiben außerhalb des Repos (oder in den ignorierten Ordnern `bin/UFO`, `bin/TFTD`).

| Datei | Inhalt |
|---|---|
| `ufo/ASSET-LIST.md`, `tftd/ASSET-LIST.md` | Lesbare Übersicht je Spiel |
| `ufo/assets.json`, `tftd/assets.json` | Dieselben Daten maschinenlesbar (für Werkzeuge/KI-Prompts) |

## Neu erzeugen
```sh
dotnet run --project port/tools/Acom.AssetSpec -c Release -- --data "<Ordner mit GEODATA, GEOGRAPH, UFOGRAPH, UNITS, ...>" --game ufo
dotnet run --project port/tools/Acom.AssetSpec -c Release -- --data "<TFTD-Ordner>" --game tftd
```
Optionen: `--rules <Ruleset-Ordner>` (Standard `bin/standard/xcom1|xcom2`), `--out <Ordner>` (Standard `content/spec/<spiel>`), `--hd-scale <n>` (Standard 4).

## Benutzung für neue Inhalte
- Die Spalte „Verwendung“ nennt Slot-Bezeichner der Engine (z. B. `items/STR_…: bigSprite`). Sie sagt, **welche Funktion** ein Frame hat – nicht, wie er aussehen soll. Namen und Aussehen der ACOM-Inhalte werden neu erfunden.
- Für KI-Prompts nur Funktion, Maße, Frame-Anzahl, Blickrichtungen und Farbgruppen verwenden; nie Originalbilder als Eingabe oder Stil-Vorlage. Regeln: `docs/architecture/content-rights.md`.
