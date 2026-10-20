# Inhalte und Rechte (Audit)

> Technische Bestandsaufnahme, **keine Rechtsberatung**. Vor einer Veröffentlichung prüfen lassen.

## Grundsätze
- **Code:** OpenXcom/OXCE steht unter GPLv3. Der C#-Port ist ein abgeleitetes Werk und steht unter GPL-3.0-or-later. Copyright-Hinweise der OpenXcom-Entwickler bleiben erhalten.
- **Dateiformate und Spielmechanik** (Formeln, Regeln) sind nicht urheberrechtlich geschützt, **konkrete Inhalte schon**: Grafiken, Karten, Sounds, Musik, Texte, Namen und Logos.
- **Marken:** „X-COM“, „XCOM“, „UFO: Enemy Unknown“, „UFO Defense“, „Terror from the Deep“ dürfen weder im Projektnamen noch im Inhalt vorkommen. „OpenXcom“ bleibt nur in Copyright-/Herkunftshinweisen.
- **Ziel:** Das Spiel läuft ohne jede Originaldatei. Das Laden von Original-Daten, die Spieler selbst besitzen, darf als optionale Kompatibilität bestehen bleiben, wird aber nicht mitgeliefert.

## Bestand im Repo
| Pfad | Inhalt | Bewertung | Maßnahme |
|---|---|---|---|
| `src/`, `libs/` | Engine-Code (GPLv3), rapidyaml/miniz (MIT) | frei | Port-Basis; C++-Bibliotheken werden im Port nicht benötigt |
| `bin/UFO`, `bin/TFTD` | nur `README.txt` (Originaldaten fehlen) | unkritisch | Durch eigenes Inhaltspaket ersetzen |
| `bin/standard/xcom1`, `xcom2` | Rulesets mit Original-Namen/Werten, **abgetippte Originaltexte** (z. B. `STR_SECTOID_UFOPEDIA`), 207 bzw. 14 Karten (MAP/RMP/MCD), aus Originalen abgeleitet | **geschützt** | Nicht ins neue Paket übernehmen; eigenes Setting schreiben |
| `bin/standard/xcom1/Resources/Weapons`, `.../Extended/Touch` (+ xcom2) | Grafiken © Niklas Jansson, Riccardo Gasperi, **CC-BY-SA 4.0** | frei mit Namensnennung | Weiterverwendbar; Namensnennung + ShareAlike beachten |
| übrige `bin/standard/xcom1/Resources/*` (UI, BulletSprites, Extended) | Herkunft unklar | prüfen | Bis zur Klärung als „ersetzen“ behandeln |
| `bin/standard/*` (XcomUtil_*, UFOextender_*, Demigod_Difficulty, …) | Regel-Mods für Originalinhalte, Fremd-Toolnamen | gebunden an Originalinhalt | Entfällt; gute Ideen als eigene Optionen neu umsetzen |
| `bin/common/Palettes` | Exporte der Original-Paletten (UFO/TFTD) | **abgeleitet** | Eigene Paletten (16×16-Rampen) erstellen |
| `bin/common/Language/*.yml`, `OXCE/*.yml` | Engine-UI-Texte (Transifex-Community), enthalten Originalbegriffe („Original X-Com“, „Xcom-controlled“) | Lizenz der Übersetzungen prüfen | Texte ins neue Setting überführen, Begriffe ersetzen |
| `bin/common/Language/Font*.png`, `Font.dat` | Bitmap-Schriften | Herkunft prüfen | Eigene HD-Schrift (z. B. aus freier OFL-Schrift gerastert) |
| `bin/common/Resources` (3 PNG) | OpenXcom-UI-Hilfsgrafiken | vermutlich OpenXcom-eigen | Prüfen, sonst neu zeichnen |
| `bin/common/SoldierName/*.nam` | Namenslisten | geringes Risiko | Prüfen; ggf. neu generieren |
| `bin/common/Shaders` | OpenGL-Shader (diverse Lizenzen) | nicht benötigt | Entfällt (MonoGame) |
| Projektname „OpenXcom“, Fenstertitel, Logos | Markenbezug | **ersetzen** | Neuer Name (Arbeitstitel „Bulwark“) |

## Eigenes Inhaltspaket (Plan)
- **Form:** ein OXCE-kompatibler Mod (`metadata.yml`, `Rulesets/*.rul`, `Language/*.yml`, `Resources/…`), damit Engine und Mod-Werkzeuge unverändert funktionieren. Arbeitsort: `content/` (noch anzulegen).
- **Setting:** eigene Fraktionen, Gegner, Fahrzeuge, Technologien und Texte. Mechanik darf sich am Genre orientieren, Namen und Beschreibungen nicht.
- **Grafik:** HD-Pixelart (Pixeldichte 4) auf eigenen 256-Farben-Paletten (16 Gruppen × 16 Schattierungen), siehe `csharp-port.md`.
- **Lizenz:** Empfehlung **CC-BY-SA 4.0** (passt zu den vorhandenen CC-BY-SA-Grafiken) oder CC0 für Beiträge ohne Namensnennungswunsch.
- **Herkunftsnachweis:** jede Datei mit Autor, Quelle und Lizenz in `content/ASSETS.md`. Bei KI-generierten Inhalten das Werkzeug und dessen Nutzungsbedingungen (kommerzielle Nutzung erlaubt?) vermerken.
- **Reihenfolge:** zuerst Platzhalter (prozedural erzeugt), damit der Port ohne Originaldaten testbar ist; danach schrittweise endgültige Grafiken.
