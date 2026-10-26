# Acom.Placeholder

Erzeugt einen vollständigen, prozedural generierten **Platzhalter-Datensatz** für den Master-Mod `xcom1`, damit der Port ohne Originaldaten startet und getestet werden kann. Die Ausgabe hat das Layout des Original-Datenordners (`GEODATA/`, `GEOGRAPH/`, `UFOGRAPH/`, `UNITS/`, `TERRAIN/`, `MAPS/`, `ROUTES/`, `SOUND/`).

```sh
cd port
dotnet run --project tools/Acom.Placeholder -- --out <Zielordner>/UFO [--spec <assets.json>] [--rules <Mod-Ordner>] [--seed <n>]
```
- Vorgaben: `--spec content/spec/ufo/assets.json`, `--rules bin/standard/xcom1` (Repo wird vom Arbeitsordner aus gesucht).
- Deterministisch: gleicher Seed → byte-gleiche Dateien. Erzeugte Dateien **nicht** einchecken.

## Mit dem Spiel nutzen
Die Engine sucht die `loadResources`-Ordner des Masters (`UFO`) zuerst im Benutzerordner, dann in den Datenordnern:
```sh
dotnet run --project tools/Acom.Placeholder -- --out /tmp/acom/UFO
xvfb-run -a dotnet run --project src/Acom.Desktop -- --screenshot out.png --frames 120 -data "$PWD/../bin" -user /tmp/acom/
```
`-data` liefert `standard/` und `common/` aus dem Repo, `-user` den Platzhalter-Ordner. Mit `-load <datei>.sav` (aus `<user>/xcom1/`) startet direkt ein Spielstand.

Der Smoke-Test `tests/Acom.Core.Tests/Tools/PlaceholderSmokeTests.cs` erzeugt die Daten in einen Temp-Ordner, lädt `xcom1` mit allen Ressourcen und spielt headless: Startbildschirm → Hauptmenü → neues Spiel → erste Basis → 35 Tage Geoscape (inkl. Monatsbericht, Speichern/Laden). Weitere Tests erzeugen über „Neue Schlacht“ jede Mission auf jedem Gelände und spielen drei Kämpfe einige Runden lang (Soldaten laufen und schießen, KI-Züge); die noch fehlende `BriefingState` wird dabei übersprungen. Dabei werden `MAPS/ROUTES/TERRAIN/UFOGRAPH/UNITS` des Mods weggelassen, alle Kampfdaten kommen also aus dem Platzhalter-Satz.

## Inhalt
| Bereich | Erzeugt | Regeln |
|---|---|---|
| Paletten | `PALETTES.DAT` (5), `BACKPALS.DAT` | eigene 16×16-Rampen (Schattierung 0 = hell); Gruppen 12+13 bilden die 32-stufige Ozean-Rampe des Globus |
| Bilder | alle `SCR`/`SPK` (inkl. `UFOGRAPH/*.PCK` im SPK-Format, `INTERWIN.DAT`) | Rauschen, Streifen, Rahmen, geometrisches Emblem |
| Sprites | alle `PCK`+`TAB`, DAT-Bögen (`TEXTURE`, `SCANG`, `DETBLOB`, `MEDIBITS`, `SPICONS`) | Maße und Frame-Anzahl aus der Asset-Liste; Figuren, Kisten, Scheiben, Iso-Kacheln |
| Globus | `WORLD.DAT` | 5°-Vierecke über allen Ländern und Missionszonen (außer Flugzone 0) → einige große Kontinente |
| Gelände | `TERRAIN/*.MCD` + `PCK` | Datensatz *i* ist Boden/Westwand/Nordwand/Objekt (*i* mod 4); LOFT-Verweise passend zu `LOFTEMPS.DAT`; Böden der Fahrzeug- und Basisgelände sind Startpunkte |
| Karten | `MAPS/*.MAP`, `ROUTES/*.RMP` für jeden Kartenblock der Rulesets | Bodenebene belegt, einzelne Wände/Objekte; Knotengitter mit allen Rängen und Spawn-Priorität |
| Sounds | `SAMPLE*.CAT` (WAV), `SOUND1/2.CAT`, `INTRO.CAT` (DOS-Format) | kurze Sinus-Töne |

**Nicht erzeugt** (die Engine braucht sie nicht oder kommt ohne aus): Musik (`GM.CAT`, `ADLIB.CAT`, MIDI – das Spiel bleibt still), Intro (`UFOINT.FLI`, `*.LBM`), Original-Schriften und -Texte (`*.DAT` in GEODATA), DOS-Programme, nicht referenzierte RMP-Dateien. Das Programm listet sie am Ende auf.

## Rechte
Aus den Originaldaten stammen nur technische Fakten der Asset-Liste (Dateinamen, Formate, Maße, Frame- und Datensatz-Anzahlen, Kartengrößen), siehe `docs/architecture/content-rights.md`. Alle Pixel, Töne, Karten und Polygone entstehen prozedural aus Formeln und einem festen Seed.
