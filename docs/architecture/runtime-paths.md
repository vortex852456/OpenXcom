# Runtime-Pfade und Debug-Checks (Kurz)

## Ziel
Schnelles Nachschlageblatt fuer Data/User/Config-Pfade und typische Laufzeitprobleme.

## Quellen der Pfadlogik
- `README.md` -> Abschnitt "Directory Locations"
- Laufzeit-/Plattformlogik in `src/Engine` (u. a. Optionen/Plattform-Helfer)

## Repo-relevante Datenordner
- `bin/common`: gemeinsame Ressourcen (Sprachen, Paletten, weitere Assets).
- `bin/standard`: Standard-Mods/Regelpakete.
- `bin/UFO`, `bin/TFTD`: Spieldatenstrukturen fuer Basisspielinhalte.

## Nutzerpfade (konzeptionell)
- **User**: Savegames, Screenshots, Mods.
- **Config**: globale Einstellungen.
- **Data**: Basisspieldaten + Ressourcen.
- Die konkreten OS-Pfade sind in `README.md` dokumentiert und sollen dort als Single Source of Truth gelten.

## Startparameter fuer Pfadsteuerung
- Anwendung kann Pfade via Argumente beeinflussen (z. B. `-data`, `-user`, `-config`).
- Bei lokalen Repros ist ein haeufiges Setup:
  - Daten relativ zur Build-Ausgabe (`bin/...`)
  - User/Config im benutzerspezifischen Standardpfad

## Typische Laufzeitprobleme
1. **"Datei/Ressource nicht gefunden"**
   - Meist falscher `data`-Pfad oder unvollstaendige UFO/TFTD-Inhalte.
2. **Mod wird nicht erkannt**
   - Mod nicht im richtigen User-Mod-Ordner oder mit zusaetzlicher Oberordner-Ebene entpackt.
3. **Save/Config scheint "weg"**
   - Anwendung nutzt einen anderen User/Config-Pfad als erwartet.
4. **Plattformwechsel (Win/Linux/macOS)**
   - Unterschiedliche Standardpfade; immer gegen `README.md` validieren.

## Debug-Checkliste (60 Sekunden)
- Existieren `bin/common` und `bin/standard` in der Laufzeitumgebung?
- Sind UFO/TFTD-Daten am erwarteten Ort vorhanden?
- Werden Startparameter (`-data/-user/-config`) korrekt gesetzt?
- Zeigt das Laufzeitlog auf einen anderen Suchpfad als vermutet?

## Update-Regel
- Nur stabile Pfadmuster und reproduzierbare Fehlerbilder dokumentieren.
- Keine session-spezifischen absoluten lokalen Pfade eintragen.
