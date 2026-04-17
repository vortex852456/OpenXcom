# AGENTS.md

## Ziel
Diese Datei ist der erste Einstiegspunkt fuer KI-Agenten in diesem Repository.
Primarziel: **Tokens sparen**, ohne Qualitaet oder Nachvollziehbarkeit zu verlieren.

## Kurzprofil des Repos
- Projekt: `OpenXcomExtended` (C++-Codebasis, Build/Assets gemischt)
- Arbeitsmodus: erst lokalisieren, dann gezielt lesen, dann minimal aendern
- Grundsatz: nie unnoetig breit suchen oder lange Dateien komplett einlesen

## Token-Sparregeln (Pflicht)
1. **Narrow first**
   - Erst Dateimenge eingrenzen (`Glob`, gezieltes `rg`), dann einzelne Dateien lesen.
2. **Nur relevante Ausschnitte lesen**
   - Bei grossen Dateien mit Offset/Limit arbeiten statt Volltext.
3. **Kein redundantes Re-Reading**
   - Bereits bekannte Inhalte nicht erneut laden, ausser bei echtem Aenderungsbedarf.
4. **Batching**
   - Unabhaengige Tool-Abfragen parallelisieren.
5. **Minimal invasive Edits**
   - Kleine, lokale Patches statt grosser Refactors ohne Auftrag.
6. **Knappe Kommunikation**
   - Kurz berichten, keine langen Wiederholungen von Tool-Output.

## Arbeitsablauf pro Task
1. Auftrag in 1-2 Saetzen konkretisieren (intern).
2. Zielbereiche mit Datei-/Textsuche eingrenzen.
3. Nur noetige Dateien/Abschnitte lesen.
4. Aenderung mit kleinstmoeglichem Diff umsetzen.
5. Nur relevante Checks ausfuehren (z. B. betroffene Datei/Target).
6. Ergebnis kurz dokumentieren.

## Semi-automatische Pflege dieser Datei
Bei neuen stabilen Erkenntnissen soll der Agent `AGENTS.md` aktualisieren.

### Update-Trigger
- Wiederkehrende Build-/Test-/Debug-Pfade festgestellt
- Neue bevorzugte Suchmuster fuer dieses Repo
- Geaenderte Projektkonventionen oder Workflows
- Wiederholte Token-Kostenfallen entdeckt

### Update-Regeln
- Nur **dauerhafte**, repo-weite Erkenntnisse uebernehmen (keine Session-Notizen).
- Pro Update maximal 3-6 Bullet-Points aendern.
- Datei kurz halten (Richtwert: < 200 Zeilen).
- Deduplizieren: keine inhaltlich doppelten Regeln.
- Veraltete Punkte aktiv entfernen.

## Was nicht hierher gehoert
- Lange Logs, komplette Fehlermeldungs-Historien
- Temporaere Task-Details
- Umfangreiche Architektur-Dokumentation (separat pflegen)

## Architektur-Dokumentation (fester Ort)
- Primarer Ort: `docs/architecture/README.md`
- Optional fuer spaetere Vertiefung: weitere kurze Dateien unter `docs/architecture/` (z. B. Build, Runtime, Modding).
- Wenn Architektur-Wissen aktualisiert wird:
  - `AGENTS.md` nur um 1-3 knappe Regeln/Leitplanken erweitern.
  - Detailwissen immer in `docs/architecture/README.md` pflegen.

## Quickstart fuer Agenten
- Lies zuerst diese Datei.
- Ermittle dann nur die minimal noetigen Dateien fuer den Task.
- Fuehre kleine, pruefbare Schritte aus.
- Wenn neue dauerhafte Erkenntnisse entstehen: `AGENTS.md` knapp aktualisieren.
