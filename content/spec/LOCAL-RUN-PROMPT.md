# Prompt für den lokalen Lauf (Claude Code im eigenen Repo-Klon)

Pfade in `<…>` vorher ersetzen; TFTD-Teil weglassen, wenn nicht vorhanden.

```text
Erzeuge die Asset-Spezifikation aus meinen Original-Spieldaten. Regeln:
- Die Originaldaten liegen unter <PFAD_UFO> (UFO) und <PFAD_TFTD> (TFTD). Lies sie nur über das Werkzeug.
  Öffne, betrachte, beschreibe oder kopiere KEINE Originaldatei selbst (keine Bilder ansehen, keine Texte lesen,
  keine Sounds abspielen), und kopiere nichts davon ins Repo.
- Ändere keinen Code. Wenn das Werkzeug fehlschlägt, brich ab und zeig mir die Fehlermeldung.

Schritte:
1. git checkout vortex-2025 && git pull origin vortex-2025
2. dotnet --version  (muss 10.x sein; sonst abbrechen und mir sagen, dass das .NET 10 SDK fehlt)
3. dotnet run --project port/tools/Acom.AssetSpec -c Release -- --data "<PFAD_UFO>" --game ufo
4. dotnet run --project port/tools/Acom.AssetSpec -c Release -- --data "<PFAD_TFTD>" --game tftd
5. Prüfe mit `git status --porcelain`, dass sich nur Dateien unter content/spec/ufo/ und content/spec/tftd/ geändert haben.
   Falls irgendetwas anderes auftaucht: nicht committen, mir die Liste zeigen.
6. Prüfe mit `grep -rIl "<PFAD_UFO>" content/spec`, dass keine lokalen Pfade in der Ausgabe stehen.
7. git add content/spec/ufo content/spec/tftd
   git commit -m "content: Add asset specification generated from local original data"
   git pull --rebase origin vortex-2025 && git push origin vortex-2025
8. Fasse mir zusammen: Anzahl Dateien je Kategorie (Tabelle „Übersicht“ aus content/spec/*/ASSET-LIST.md)
   und die Zahl der Einträge unter „Verweise ohne Originaldatei“.
```
