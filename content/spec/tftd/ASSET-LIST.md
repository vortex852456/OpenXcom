# Asset-Liste: TFTD

> Automatisch erzeugt von `port/tools/Acom.AssetSpec` aus lokal vorhandenen Originaldaten.
> Enthält nur technische Fakten (Größen, Frames, belegte Bereiche, Farbgruppen, Verwendung laut `bin/standard/xcom2`) –
> keine Pixel, Farbwerte, Texte oder Töne. Einträge unter „Verwendung“ sind Slot-Bezeichner der Engine, keine Namen für ACOM-Inhalte.
> HD-Zielgröße = klassische Größe × 4. Regeln für neue Inhalte: `docs/architecture/content-rights.md`.

## Übersicht

| Kategorie | Dateien | Frames/Einträge | Bytes |
|---|---:|---:|---:|
| data | 12 | 226 | 84924 |
| font | 2 | 346 | 56744 |
| images | 224 | 171 | 5607258 |
| maps | 298 | 0 | 935694 |
| music | 3 | 66 | 185759 |
| palette | 2 | 11 | 2706 |
| program | 6 | 0 | 33185 |
| routes | 297 | 3223 | 77352 |
| sounds | 2 | 141 | 1244693 |
| sprites | 33 | 4847 | 896499 |
| terrain | 129 | 6462 | 1991073 |
| text | 9 | 0 | 244719 |
| unknown | 2 | 0 | 56916 |
| video | 10 | 2425 | 50476061 |

## Sprite-Sets

### GEODATA/SCANG.DAT

Kleine Markierungen auf dem Globus (4×4) · Frame 4×4 → HD 16×16 · 550 Frames (550 belegt) · Farbgruppen: 0,1,3,5,6,7,10,11

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–549

### GEOGRAPH/BASEBITS.PCK

Basisansicht: Gebäude-Bausteine, Fahrzeuge im Hangar, Rangabzeichen, Fahrzeugwaffen · Frame 32×40 → HD 128×160 · 54 Frames (54 belegt) · Farbgruppen: 0,1,2,3,4,5,6,7,8,9,10,11,12,13,15

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 1 | 1,1,30×30 | 0,1,5,6,7,8,10,15 | facilities/STR_LIVING_QUARTERS: spriteShape; facilities/STR_LABORATORY: spriteShape; facilities/STR_WORKSHOP: spriteShape; facilities/STR_WIDE_ARRAY_SONAR: spriteShape; facilities/STR_GENERAL_STORES: spriteShape; facilities/STR_ALIEN_CONTAINMENT: spriteShape; facilities/STR_MC_LAB: spriteShape |
| 2 | 1,1,30×30 | 0,1,2,5,6,7,8,10,13,15 | facilities/STR_AIR_LOCK: spriteShape; facilities/STR_STANDARD_SONAR: spriteShape; facilities/STR_TORPEDO_DEFENSES: spriteShape; facilities/STR_BOMBARDMENT_SHIELD: spriteShape; facilities/STR_MC_GENERATOR: spriteShape; facilities/STR_TRANSMISSION_RESOLVER: spriteShape |
| 3 | 1,1,30×30 | 0,1,6,7,8,10,15 | facilities/STR_GAUSS_DEFENSES: spriteShape; facilities/STR_SONIC_DEFENSES: spriteShape; facilities/STR_PWT_DEFENSES: spriteShape |
| 9 | 1,1,31×31 | 0,1,2,3,5,6,8,10,15 | facilities/STR_SUB_PEN: spriteShape; facilities/STR_SUB_PEN: spriteFacility |
| 17 | 7,7,19×18 | 1,3,10,11,12,13,15 | facilities/STR_AIR_LOCK: spriteFacility |
| 18 | 3,3,24×24 | 0,1,7,8,10,11,13,15 | facilities/STR_LIVING_QUARTERS: spriteFacility |
| 19 | 2,2,25×25 | 0,1,2,5,6,7,8,10,12,13,15 | facilities/STR_LABORATORY: spriteFacility |
| 20 | 3,3,24×24 | 0,1,2,4,6,7,8,10,11,13,15 | facilities/STR_WORKSHOP: spriteFacility |
| 21 | 3,3,24×24 | 1,2,4,5,7,10,11,15 | facilities/STR_STANDARD_SONAR: spriteFacility |
| 22 | 3,3,24×24 | 1,5,7,10,11 | facilities/STR_WIDE_ARRAY_SONAR: spriteFacility |
| 23 | 6,6,20×20 | 0,1,4,7,8,10,12 | facilities/STR_TORPEDO_DEFENSES: spriteFacility |
| 24 | 2,2,25×25 | 0,1,2,4,5,7,8,10,11,12,13,15 | facilities/STR_GENERAL_STORES: spriteFacility |
| 25 | 4,8,25×20 | 1,2,3,5,6,7,12,13 | facilities/STR_ALIEN_CONTAINMENT: spriteFacility |
| 26 | 5,5,19×20 | 1,2,10,13 | facilities/STR_GAUSS_DEFENSES: spriteFacility |
| 27 | 5,5,21×21 | 1,2,8,10 | facilities/STR_SONIC_DEFENSES: spriteFacility |
| 28 | 4,4,22×22 | 1,2,10 | facilities/STR_PWT_DEFENSES: spriteFacility |
| 29 | 2,2,25×25 | 1,2,3,5,7,8,10,11,12,13,15 | facilities/STR_BOMBARDMENT_SHIELD: spriteFacility |
| 30 | 2,2,25×25 | 1,3,4,5,6,7,8,10,12,13,15 | facilities/STR_MC_GENERATOR: spriteFacility |
| 31 | 2,2,25×25 | 0,1,2,6,7,8,10,12,13,15 | facilities/STR_MC_LAB: spriteFacility |
| 32 | 2,2,25×25 | 1,5,7,8,12,13,15 | facilities/STR_TRANSMISSION_RESOLVER: spriteFacility |
| 33 | 0,4,31×31 | 0,2,5,6,7,8,10,12,15 | crafts/STR_TRITON: sprite (Hangar, +33) |
| 34 | 1,11,27×21 | 5,6,8,15 | crafts/STR_HAMMERHEAD: sprite (Hangar, +33) |
| 35 | 2,5,27×30 | 5,6,8,15 | crafts/STR_LEVIATHAN: sprite (Hangar, +33) |
| 36 | 2,4,27×33 | 0,4,7,8,10,11,12 | crafts/STR_BARRACUDA: sprite (Hangar, +33) |
| 37 | 2,13,27×16 | 3,5,6,12 | crafts/STR_MANTA: sprite (Hangar, +33) |
| 48 | 0,0,15×17 | 2,10,15 | craftWeapons/STR_AJAX: sprite (Basis, +48) |
| 49 | 0,0,15×17 | 2,10,15 | craftWeapons/STR_DUP_HEAD: sprite (Basis, +48) |
| 50 | 0,0,15×17 | 10,15 | craftWeapons/STR_CRAFT_GAS_CANNON_UC: sprite (Basis, +48) |
| 51 | 0,0,15×17 | 10,15 | craftWeapons/STR_PWT_CANNON_UC: sprite (Basis, +48) |
| 52 | 0,0,15×17 | 2,10,15 | craftWeapons/STR_GAUSS_CANNON_UC: sprite (Basis, +48) |
| 53 | 0,0,15×17 | 10,15 | craftWeapons/STR_SONIC_OSCILLATOR_UC: sprite (Basis, +48) |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0, 4–8, 10–16, 38–47

### GEOGRAPH/INTICON.PCK

Geoscape/Abfangen: Fahrzeug- und Waffensymbole · Frame 32×40 → HD 128×160 · 16 Frames (16 belegt) · Farbgruppen: 0,7,9,15

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 0,0,32×20 | 7,15 | crafts/STR_TRITON: sprite (Liste) |
| 1 | 0,0,32×20 | 15 | crafts/STR_HAMMERHEAD: sprite (Liste) |
| 2 | 0,0,32×20 | 15 | crafts/STR_LEVIATHAN: sprite (Liste) |
| 3 | 0,0,32×20 | 15 | crafts/STR_BARRACUDA: sprite (Liste) |
| 4 | 0,0,32×20 | 15 | crafts/STR_MANTA: sprite (Liste) |
| 5 | 0,0,15×17 | 7,9 | craftWeapons/STR_AJAX: sprite (Abfangfenster, +5) |
| 6 | 0,0,15×17 | 7,9 | craftWeapons/STR_DUP_HEAD: sprite (Abfangfenster, +5) |
| 7 | 0,0,15×17 | 7,9 | craftWeapons/STR_CRAFT_GAS_CANNON_UC: sprite (Abfangfenster, +5) |
| 8 | 0,0,15×17 | 7,9 | craftWeapons/STR_PWT_CANNON_UC: sprite (Abfangfenster, +5) |
| 9 | 0,0,15×17 | 7,9 | craftWeapons/STR_GAUSS_CANNON_UC: sprite (Abfangfenster, +5) |
| 10 | 0,0,15×17 | 7,9 | craftWeapons/STR_SONIC_OSCILLATOR_UC: sprite (Abfangfenster, +5) |
| 11 | 1,4,21×20 | 0,15 | crafts/STR_TRITON: sprite (Abfangfenster, +11) |
| 12 | 1,4,21×18 | 0 | crafts/STR_HAMMERHEAD: sprite (Abfangfenster, +11) |
| 13 | 1,1,21×24 | 0 | crafts/STR_LEVIATHAN: sprite (Abfangfenster, +11) |
| 14 | 1,3,20×20 | 0 | crafts/STR_BARRACUDA: sprite (Abfangfenster, +11) |
| 15 | 1,7,21×13 | 0 | crafts/STR_MANTA: sprite (Abfangfenster, +11) |

### GEOGRAPH/TEXTURE.DAT

Globus-Oberflächentexturen (32×32) · Frame 32×32 → HD 128×128 · 39 Frames (39 belegt) · Farbgruppen: 3,4,6,12,13

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–38

### UFOGRAPH/CURSOR.PCK

Kampfkarten-Cursor und Zielmarkierungen · Frame 32×40 → HD 128×160 · 17 Frames (17 belegt) · Farbgruppen: 0,9

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–16

### UFOGRAPH/DETBLOB.DAT

Bewegungsscanner: Signal-Punkte (16×16) · Frame 16×16 → HD 64×64 · 15 Frames (15 belegt) · Farbgruppen: 0,1,5

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–14

### UFOGRAPH/HIT.PCK

Nahkampf-/Treffer-Animationen · Frame 32×40 → HD 128×160 · 4 Frames (4 belegt) · Farbgruppen: 1,2

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 6,14,22×21 | 1,2 | items/STR_VIBRO_BLADE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_THERMIC_LANCE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HEAVY_THERMIC_LANCE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 1–3

- Hinweis: Verweise auf Frames, die in den Originaldaten fehlen (vom Repo/Mod ergänzt): 8, 26, 46 – z. B. items/STR_HWP_AQUA_JET_MISSILES: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HWP_DISPLACER_PWT: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_GC_HE_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_GC_P_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HJC_HE_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HJC_P_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_SMALL_TORPEDO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_LARGE_TORPEDO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_PHOSPHOROUS_TORPEDO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_MAGNA_BLAST_GRENADE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_DYE_GRENADE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_PARTICLE_DISTURBANCE_GRENADE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_MAGNA_PACK_EXPLOSIVE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_DISRUPTOR_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_THERMAL_SHOK_BOMB: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_SONIC_PULSER: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK)

### UFOGRAPH/MEDIBITS.DAT

Medikit-Fenster: Körperteile (52×58) · Frame 52×58 → HD 208×232 · 6 Frames (6 belegt) · Farbgruppen: 4

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–5

### UFOGRAPH/SMOKE.PCK

Rauch, Feuer, Treffereffekte, Rangabzeichen im Kampf · Frame 32×40 → HD 128×160 · 68 Frames (68 belegt) · Farbgruppen: 0,1,2,3,4,9,12,15

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 2,9,28×29 | 0,1,4 | items/STR_VIBRO_BLADE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_THERMIC_LANCE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HEAVY_THERMIC_LANCE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |
| 8 | 3,4,27×35 | 3,4,12 | items/STR_HWP_AQUA_JET_MISSILES: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HWP_DISPLACER_PWT: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_GC_HE_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_GC_P_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HJC_HE_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HJC_P_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_SMALL_TORPEDO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_LARGE_TORPEDO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_PHOSPHOROUS_TORPEDO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_MAGNA_BLAST_GRENADE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_DYE_GRENADE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_PARTICLE_DISTURBANCE_GRENADE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_MAGNA_PACK_EXPLOSIVE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_DISRUPTOR_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_THERMAL_SHOK_BOMB: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_SONIC_PULSER: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |
| 26 | 13,13,6×6 | 1 | items/STR_HWP_SOLID_HARPOON_BOLTS: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_DART_POD: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HARPOON_POD: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_GC_AP_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HJC_AP_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |
| 46 | 13,13,6×6 | 15 | items/STR_GAUSS_CANNON_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_DISPLACER_SONIC: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_GAUSS_PISTOL_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_GAUSS_RIFLE_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HEAVY_GAUSS_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_CANNON_POWER_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_BLASTA_POWER_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_PISTOL_POWER_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_TRIBIO_SONIC_WEAPON: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/XARQUID_WEAPON: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 1–7, 9–25, 27–45, 47–67

### UFOGRAPH/SPICONS.DAT

Kleine Symbole (Spezialaktionen, Fähigkeiten, 32×24) · Frame 32×24 → HD 128×96 · 2 Frames (2 belegt) · Farbgruppen: 0,1,2,5,14

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–1

### UFOGRAPH/X1.PCK

Große Explosion (128×64) · Frame 128×64 → HD 512×256 · 16 Frames (16 belegt) · Farbgruppen: 1,2,3,7,8,10,11,13

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–15

### UNITS/AQUA.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 259 Frames (259 belegt) · Farbgruppen: 0,1,2,5,6,11,12

Verwendung: armors/AQUATOID_ARMOR0: spriteSheet (drawingRoutine 15)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–258

### UNITS/BIGOBS.PCK

Gegenstände im Inventar (große Darstellung, 32×48) · Frame 32×48 → HD 128×192 · 66 Frames (66 belegt) · Farbgruppen: 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 0,2,16×43 | 1,2,3,8,10,11,12 | items/STR_GAUSS_RIFLE: bigSprite |
| 1 | 0,2,16×45 | 0,1,2,3,5,9,12 | items/STR_JET_HARPOON: bigSprite |
| 2 | 5,2,7×13 | 0,1,12 | items/STR_HARPOON_POD: bigSprite |
| 3 | 2,4,14×24 | 0,1,3,9,12 | items/STR_DART_PISTOL: bigSprite |
| 4 | 6,2,5×13 | 0,1,12 | items/STR_DART_POD: bigSprite |
| 5 | 0,2,16×29 | 1,3,8,10,12 | items/STR_GAUSS_PISTOL: bigSprite |
| 6 | 3,3,23×43 | 1,2,3,8,10,11,12 | items/STR_HEAVY_GAUSS: bigSprite |
| 7 | 6,1,21×45 | 0,1,2,3,5,9,12 | items/STR_HYDRO_JET_CANNON: bigSprite |
| 8 | 1,3,29×10 | 2,3,5,12 | items/STR_HJC_AP_AMMO: bigSprite |
| 9 | 1,3,29×10 | 0,3,10,11,12 | items/STR_HJC_HE_AMMO: bigSprite |
| 10 | 1,3,29×10 | 3,10,11,12 | items/STR_HJC_P_AMMO: bigSprite |
| 11 | 5,2,21×44 | 0,1,2,3,5,8,9 | items/STR_GAS_CANNON: bigSprite |
| 12 | 1,1,27×14 | 1,2,3,5 | items/STR_GC_AP_AMMO: bigSprite |
| 13 | 1,1,27×14 | 0,1,2,3 | items/STR_GC_HE_AMMO: bigSprite |
| 14 | 1,1,27×14 | 1,2,3,10 | items/STR_GC_P_AMMO: bigSprite |
| 15 | 8,1,19×46 | 0,1,2,3,5,8,9 | items/STR_TORPEDO_LAUNCHER: bigSprite |
| 16 | 3,2,11×44 | 0,3,9,10 | items/STR_SMALL_TORPEDO: bigSprite |
| 17 | 3,2,11×44 | 0,3,8,9,10,11 | items/STR_LARGE_TORPEDO: bigSprite |
| 18 | 3,2,11×44 | 3,5,9 | items/STR_PHOSPHOROUS_TORPEDO: bigSprite |
| 19 | 5,5,9×9 | 0 | items/STR_MAGNA_BLAST_GRENADE: bigSprite |
| 20 | 4,3,10×10 | 0 | items/STR_DYE_GRENADE: bigSprite |
| 21 | 3,4,10×6 | 0,11 | items/STR_PARTICLE_DISTURBANCE_GRENADE: bigSprite |
| 22 | 1,0,30×15 | 0,1,2,3,5 | items/STR_MAGNA_PACK_EXPLOSIVE: bigSprite |
| 23 | 0,1,14×14 | 0,1,6,9,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: bigSprite |
| 24 | 0,0,16×31 | 0,1,2,5,8,9,11,12,13,14,15 | items/STR_MEDI_KIT: bigSprite |
| 25 | 1,1,13×14 | 1,2,3,6,15 | items/STR_CANNON_POWER_CLIP: bigSprite |
| 26 | 4,1,10×45 | 0,1,2,3,9,10 | items/STR_THERMAL_TAZER: bigSprite |
| 28 | 0,0,31×47 | 3,6,11,12,15 | items/STR_CORPSE: bigSprite |
| 29 | 3,8,25×16 | 0,1,2,3,5,6,8,10,15 | items/STR_MC_READER: bigSprite |
| 30 | 2,1,29×45 | 1,2,3,6,12,15 | items/STR_SONIC_CANNON: bigSprite |
| 31 | 0,3,16×44 | 1,2,3,5,6,11,12,15 | items/STR_SONIC_BLASTA_RIFLE: bigSprite |
| 32 | 0,3,16×28 | 1,2,3,5,6,11,12,15 | items/STR_SONIC_PISTOL: bigSprite |
| 33 | 1,3,14×38 | 0,3,6,8,10,14,15 | items/STR_MC_DISRUPTOR: bigSprite |
| 34 | 3,1,11×9 | 1,2,3,6,15 | items/STR_PISTOL_POWER_CLIP: bigSprite |
| 35 | 7,1,19×46 | 0,1,3,6,10,11,12,13,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: bigSprite |
| 36 | 2,5,11×15 | 0,1,3,6,15 | items/STR_DISRUPTOR_AMMO: bigSprite |
| 37 | 6,1,22×31 | 1,3,6,8,10,11,12,15 | items/STR_THERMAL_SHOK_LAUNCHER: bigSprite |
| 38 | 4,3,9×9 | 0,1,3,6,14,15 | items/STR_THERMAL_SHOK_BOMB: bigSprite |
| 39 | 3,3,11×9 | 0,6,10,11,15 | items/STR_SONIC_PULSER: bigSprite |
| 40 | 1,8,30×39 | 1,2,3,8,10 | items/STR_COELACANTH_GAS_CANNON: bigSprite; items/STR_COELACANTH_GAUSS: bigSprite |
| 41 | 2,2,13×11 | 1,2,3,6,15 | items/STR_BLASTA_POWER_CLIP: bigSprite |
| 42 | 1,0,30×47 | 0,1,2,3,8,9,10,12 | items/STR_COELACANTH_AQUA_JET: bigSprite |
| 43 | 5,1,26×46 | 0,2,3,9,10 | items/STR_DISPLACER_PWT: bigSprite |
| 44 | 0,1,31×46 | 1,3,6,11,12,15 | items/STR_CORPSE_ARMOR: bigSprite |
| 45 | 0,0,31×47 | 1,2,3,6,10,11,12,15 | items/STR_CORPSE_ION: bigSprite |
| 46 | 0,1,31×46 | 2,5,10,11,12 | items/STR_AQUATOID_CORPSE: bigSprite |
| 47 | 0,0,31×47 | 2,3,5,11,14 | items/STR_DEEP_ONE_CORPSE: bigSprite |
| 48 | 0,0,31×47 | 1,3,5,8,10,12,14 | items/STR_TASOTH_CORPSE: bigSprite |
| 49 | 1,0,30×47 | 1,6,8,9,10,12 | items/STR_LOBSTERMAN_CORPSE: bigSprite |
| 50 | 0,0,31×47 | 3,5,7,11,14 | items/STR_GILLMAN_CORPSE: bigSprite |
| 51 | 0,15,31×32 | 1,2,3,5,6,7,8,10,11,12,14,15 | items/STR_BIODRONE_CORPSE: bigSprite |
| 52 | 0,0,31×47 | 3,4,5,6,8,10,15 | items/STR_CALCINITE_CORPSE: bigSprite |
| 53 | 0,0,31×47 | 0,1,2,3,4,5,6,7,8,9,10,11,13,14 | items/STR_TENTACULAT_CORPSE: bigSprite |
| 54 | 2,0,29×46 | 1,2,3 | items/STR_DISPLACER_SONIC: bigSprite |
| 55 | 6,1,6×13 | 0,3,5 | items/STR_CHEMICAL_FLARE: bigSprite |
| 56 | 3,3,26×9 | 1,2,3 | items/STR_ZRBITE: bigSprite |
| 57 | 6,5,5×8 | 11,12 | items/STR_GAUSS_PISTOL_CLIP: bigSprite |
| 58 | 4,2,24×44 | 1,2,3,6,8,10,11,12,15 | items/STR_HEAVY_THERMIC_LANCE: bigSprite |
| 59 | 6,6,19×39 | 1,2,3,6,8,10,11,12,15 | items/STR_THERMIC_LANCE: bigSprite |
| 60 | 5,0,20×47 | 1,3,6,12,15 | items/STR_VIBRO_BLADE: bigSprite |
| 61 | 4,2,8×11 | 11,12 | items/STR_GAUSS_RIFLE_CLIP: bigSprite |
| 62 | 2,2,12×11 | 11,12 | items/STR_HEAVY_GAUSS_CLIP: bigSprite |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 27, 63–65

- Hinweis: Verweise auf Frames, die in den Originaldaten fehlen (vom Repo/Mod ergänzt): 66–74 – z. B. ersetzt/ergänzt durch Repo-Datei Resources/Weapons/Terror.png (extraSprites); items/CALCINITE_WEAPON: bigSprite

### UNITS/BIODRON.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 11 Frames (11 belegt) · Farbgruppen: 3,6,7,11,13,14,15

Verwendung: armors/BIODRONE_ARMOR0: spriteSheet (drawingRoutine 16)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–10

### UNITS/BREATH-1.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 16 Frames (16 belegt) · Farbgruppen: 3,15

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–15

### UNITS/CALCIN.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 259 Frames (259 belegt) · Farbgruppen: 2,3,5,8,10,14,15

Verwendung: armors/CALCINITE_ARMOR0: spriteSheet (drawingRoutine 14)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–258

### UNITS/CIVIL_1.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 152 Frames (152 belegt) · Farbgruppen: 1,2,3,10,11,12,13,14,15

Verwendung: armors/DOCKER_ARMOR: spriteSheet (drawingRoutine 17); armors/SURFER_ARMOR: spriteSheet (drawingRoutine 18)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–151

### UNITS/CIVIL_2.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 152 Frames (152 belegt) · Farbgruppen: 1,2,3,4,5,6,8,10,11,12,13,15

Verwendung: armors/SAILOR_ARMOR: spriteSheet (drawingRoutine 17); armors/BIKINI_GIRL_ARMOR: spriteSheet (drawingRoutine 18)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–151

### UNITS/DEEPONE.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 260 Frames (260 belegt) · Farbgruppen: 0,2,3,4,7,8,10,11,14

Verwendung: armors/DEEP_ONE_ARMOR0: spriteSheet (drawingRoutine 14)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–259

### UNITS/FLOOROB.PCK

Gegenstände auf dem Boden der Kampfkarte · Frame 32×40 → HD 128×160 · 82 Frames (82 belegt) · Farbgruppen: 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 6,26,21×9 | 11,12 | items/STR_COELACANTH_GAS_CANNON: floorSprite; items/STR_GAUSS_RIFLE: floorSprite |
| 1 | 5,25,23×9 | 1,2,4,11 | items/STR_JET_HARPOON: floorSprite |
| 2 | 14,27,5×6 | 8,10,12 | items/STR_HARPOON_POD: floorSprite |
| 3 | 10,27,11×7 | 1,2,11 | items/STR_DART_PISTOL: floorSprite |
| 4 | 14,27,4×6 | 8,10,12 | items/STR_DART_POD: floorSprite |
| 5 | 10,26,12×9 | 11,12 | items/STR_GAUSS_PISTOL: floorSprite |
| 6 | 5,24,23×9 | 11,12 | items/STR_HEAVY_GAUSS: floorSprite |
| 7 | 6,25,20×9 | 1,11,12 | items/STR_HYDRO_JET_CANNON: floorSprite |
| 8 | 12,29,7×5 | 8,12 | items/STR_GC_AP_AMMO: floorSprite |
| 9 | 12,29,7×5 | 8,12 | items/STR_GC_HE_AMMO: floorSprite |
| 10 | 12,29,7×5 | 8,12 | items/STR_GC_P_AMMO: floorSprite |
| 11 | 6,27,18×7 | 1,2,11 | items/STR_GAS_CANNON: floorSprite |
| 12 | 11,29,12×4 | 0,5,14 | items/STR_HJC_P_AMMO: floorSprite |
| 13 | 11,29,12×4 | 2,5,15 | items/STR_HJC_AP_AMMO: floorSprite |
| 14 | 11,29,12×4 | 0,1,5 | items/STR_HJC_HE_AMMO: floorSprite |
| 15 | 3,26,25×9 | 1,2,7,11,12 | items/STR_TORPEDO_LAUNCHER: floorSprite |
| 16 | 6,27,22×6 | 11,12 | items/STR_SMALL_TORPEDO: floorSprite |
| 17 | 6,27,22×6 | 11,12 | items/STR_LARGE_TORPEDO: floorSprite |
| 18 | 5,27,22×6 | 6,12 | items/STR_PHOSPHOROUS_TORPEDO: floorSprite |
| 19 | 14,29,4×4 | 4,12,13,14 | items/STR_MAGNA_BLAST_GRENADE: floorSprite; items/STR_DYE_GRENADE: floorSprite |
| 21 | 13,30,5×3 | 11,12 | items/STR_PARTICLE_DISTURBANCE_GRENADE: floorSprite |
| 22 | 8,24,15×10 | 8,10,12 | items/STR_MAGNA_PACK_EXPLOSIVE: floorSprite |
| 23 | 13,27,7×8 | 8,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: floorSprite |
| 24 | 11,28,12×6 | 1,8,11,12,13,14,15 | items/STR_MEDI_KIT: floorSprite |
| 26 | 5,29,23×5 | 8,11 | items/STR_THERMAL_TAZER: floorSprite |
| 28 | 7,28,18×5 | 11,15 | items/STR_MC_DISRUPTOR: floorSprite |
| 29 | 6,24,21×9 | 7,8,11,15 | items/STR_SONIC_CANNON: floorSprite; items/STR_TRIBIO_SONIC_WEAPON: floorSprite; items/XARQUID_WEAPON: floorSprite |
| 30 | 6,27,21×5 | 7,8,11,15 | items/STR_SONIC_BLASTA_RIFLE: floorSprite |
| 31 | 9,27,14×5 | 8,11,15 | items/STR_SONIC_PISTOL: floorSprite |
| 33 | 14,29,5×5 | 8,15 | items/STR_CANNON_POWER_CLIP: floorSprite; items/STR_BLASTA_POWER_CLIP: floorSprite; items/STR_PISTOL_POWER_CLIP: floorSprite |
| 34 | 7,24,21×11 | 7,11,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: floorSprite |
| 35 | 13,29,5×5 | 11,15 | items/STR_DISRUPTOR_AMMO: floorSprite |
| 36 | 8,25,16×10 | 7,11,15 | items/STR_THERMAL_SHOK_LAUNCHER: floorSprite |
| 38 | 13,28,7×6 | 11,15 | items/STR_SONIC_PULSER: floorSprite |
| 39 | 4,24,22×13 | 6,15 | items/STR_CORPSE: floorSprite |
| 40 | 4,24,22×13 | 2,8,15 | items/STR_CORPSE_ARMOR: floorSprite |
| 41 | 4,24,21×12 | 1,2,8,12 | items/STR_CORPSE_ION: floorSprite |
| 42 | 1,24,31×12 | 2,4,5,6,11,12 | items/STR_AQUATOID_CORPSE: floorSprite |
| 43 | 0,24,31×13 | 2,3,4,7,8,11,14 | items/STR_DEEP_ONE_CORPSE: floorSprite |
| 44 | 1,21,27×17 | 0,1,2,7,8,11,14 | items/STR_TASOTH_CORPSE: floorSprite |
| 45 | 4,21,25×15 | 5,7,8,10 | items/STR_LOBSTERMAN_CORPSE: floorSprite |
| 46 | 5,24,27×13 | 1,2,3,5,7,10 | items/STR_GILLMAN_CORPSE: floorSprite |
| 47 | 2,24,23×12 | 3,7,11,13,14,15 | items/STR_BIODRONE_CORPSE: floorSprite |
| 48 | 4,21,22×16 | 2,5,8,10,15 | items/STR_CALCINITE_CORPSE: floorSprite |
| 49 | 2,25,27×13 | 1,3,7,8,10,14 | items/STR_TENTACULAT_CORPSE: floorSprite |
| 50 | 4,22,23×14 | 2,4,13 | items/CIVM_CORPSE: floorSprite |
| 51 | 5,22,25×14 | 1,4,8,10,11,13 | items/CIVF_CORPSE: floorSprite |
| 52 | 1,26,31×14 | 1,2,3,4,5,6,11,12,13,14 | items/TRISCENE_CORPSE_1: floorSprite |
| 53 | 4,25,21×9 | 1,2,3,4,5,6,7,10,11,12,13,14 | items/TRISCENE_CORPSE_4: floorSprite |
| 54 | 6,23,26×16 | 1,2,3,4,5,6,11,12,13,14 | items/TRISCENE_CORPSE_3: floorSprite |
| 55 | 1,24,23×13 | 1,2,3,4,5,6,10,11,12,13,14,15 | items/TRISCENE_CORPSE_2: floorSprite |
| 56 | 2,23,30×17 | 1,2,3,4,5,6,8,10,11,13 | items/XARQUID_CORPSE_1: floorSprite |
| 57 | 1,25,15×12 | 1,2,3,4,5,8,10,11,14 | items/XARQUID_CORPSE_2: floorSprite |
| 58 | 14,25,18×13 | 1,2,3,4,5,7,8,10,11,13,14 | items/XARQUID_CORPSE_3: floorSprite |
| 59 | 6,25,20×5 | 1,2,6,8,10,11,12 | items/XARQUID_CORPSE_4: floorSprite |
| 60 | 1,29,31×11 | 1,2,3,4,5,10,11,12,13,14 | items/HALLUCINOID_CORPSE_1: floorSprite |
| 61 | 0,25,32×12 | 1,2,3,4,5,8,10,11,12,13,14 | items/HALLUCINOID_CORPSE_2: floorSprite |
| 62 | 3,25,29×14 | 1,2,3,4,5,10,11,12,13,14 | items/HALLUCINOID_CORPSE_3: floorSprite |
| 63 | 6,25,18×6 | 2,3,4,8,10,11,12,13,14 | items/HALLUCINOID_CORPSE_4: floorSprite |
| 64 | 0,39,1×1 | 4 | items/DISPLACER_CORPSE_1: floorSprite |
| 65 | 14,24,11×16 | 0,2,3,7 | items/DISPLACER_CORPSE_2: floorSprite |
| 66 | 8,23,10×17 | 0,2,3,7,12 | items/DISPLACER_CORPSE_3: floorSprite |
| 67 | 2,10,28×28 | 0,2,3,7 | items/DISPLACER_CORPSE_4: floorSprite |
| 68 | 0,37,1×1 | 4 | items/COELACANTH_CORPSE_1: floorSprite |
| 69 | 14,14,16×24 | 1,2,3,4,7,10,13 | items/COELACANTH_CORPSE_2: floorSprite |
| 70 | 7,9,22×30 | 1,2,3,4,7,10,13 | items/COELACANTH_CORPSE_3: floorSprite |
| 71 | 2,2,28×33 | 1,2,3,4,7,10 | items/COELACANTH_CORPSE_4: floorSprite |
| 72 | 14,30,4×3 | 0,1,9 | items/STR_CHEMICAL_FLARE: floorSprite |
| 73 | 5,23,22×11 | 1,11,15 | items/STR_HEAVY_THERMIC_LANCE: floorSprite |
| 74 | 7,25,20×9 | 1,11,15 | items/STR_THERMIC_LANCE: floorSprite |
| 75 | 6,23,23×10 | 1,11,15 | items/STR_VIBRO_BLADE: floorSprite |
| 77 | 12,27,7×7 | 10,11,15 | items/STR_MC_READER: floorSprite |
| 78 | 14,29,3×4 | 11,12 | items/STR_GAUSS_PISTOL_CLIP: floorSprite; items/STR_GAUSS_RIFLE_CLIP: floorSprite; items/STR_HEAVY_GAUSS_CLIP: floorSprite |
| 79 | 12,30,8×3 | 8 | items/STR_ZRBITE: floorSprite |
| 81 | 14,30,4×4 | 3,11,15 | items/STR_THERMAL_SHOK_BOMB: floorSprite |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 20, 25, 27, 32, 37, 76, 80

### UNITS/GILLMAN.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 259 Frames (259 belegt) · Farbgruppen: 0,1,2,3,4,5,7,10

Verwendung: armors/GILLMAN_ARMOR0: spriteSheet (drawingRoutine 14)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–258

### UNITS/HALLUCIN.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 32 Frames (32 belegt) · Farbgruppen: 1,2,3,4,5,6,8,10,11,12,13,14

Verwendung: armors/HALLUCINOID_ARMOR0: spriteSheet (drawingRoutine 12)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–31

### UNITS/HANDOB.PCK

Gegenstände in der Hand von Einheiten, je 8 Blickrichtungen · Frame 32×40 → HD 128×160 · 440 Frames (440 belegt) · Farbgruppen: 0,1,2,3,4,5,7,8,10,11,12,13,14,15

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 15,12,10×5 | 1,2,11 | items/STR_COELACANTH_GAS_CANNON: handSprite (Richtung 0); items/STR_JET_HARPOON: handSprite (Richtung 0) |
| 1 | 22,11,3×9 | 1,2,11 | items/STR_COELACANTH_GAS_CANNON: handSprite (Richtung 1); items/STR_JET_HARPOON: handSprite (Richtung 1) |
| 2 | 15,15,10×5 | 1,2,11 | items/STR_COELACANTH_GAS_CANNON: handSprite (Richtung 2); items/STR_JET_HARPOON: handSprite (Richtung 2) |
| 3 | 11,17,16×4 | 1,2,11 | items/STR_COELACANTH_GAS_CANNON: handSprite (Richtung 3); items/STR_JET_HARPOON: handSprite (Richtung 3) |
| 4 | 9,16,11×7 | 1,2,11 | items/STR_COELACANTH_GAS_CANNON: handSprite (Richtung 4); items/STR_JET_HARPOON: handSprite (Richtung 4) |
| 5 | 7,14,3×9 | 1,2,11 | items/STR_COELACANTH_GAS_CANNON: handSprite (Richtung 5); items/STR_JET_HARPOON: handSprite (Richtung 5) |
| 6 | 4,14,11×7 | 1,2,11 | items/STR_COELACANTH_GAS_CANNON: handSprite (Richtung 6); items/STR_JET_HARPOON: handSprite (Richtung 6) |
| 7 | 6,14,16×4 | 1,2,11 | items/STR_COELACANTH_GAS_CANNON: handSprite (Richtung 7); items/STR_JET_HARPOON: handSprite (Richtung 7) |
| 8 | 14,8,13×9 | 11,12 | items/STR_GAUSS_RIFLE: handSprite (Richtung 0) |
| 9 | 22,12,4×10 | 11,12 | items/STR_GAUSS_RIFLE: handSprite (Richtung 1) |
| 10 | 15,11,13×9 | 11,12 | items/STR_GAUSS_RIFLE: handSprite (Richtung 2) |
| 11 | 10,15,17×6 | 0,11,12 | items/STR_GAUSS_RIFLE: handSprite (Richtung 3) |
| 12 | 7,14,15×9 | 7,11,12 | items/STR_GAUSS_RIFLE: handSprite (Richtung 4) |
| 13 | 6,13,4×11 | 7,11,12 | items/STR_GAUSS_RIFLE: handSprite (Richtung 5) |
| 14 | 2,12,15×9 | 7,11,12 | items/STR_GAUSS_RIFLE: handSprite (Richtung 6) |
| 15 | 5,13,17×6 | 0,11,12 | items/STR_GAUSS_RIFLE: handSprite (Richtung 7) |
| 16 | 12,8,16×13 | 0,11,12 | items/STR_HEAVY_GAUSS: handSprite (Richtung 0) |
| 17 | 22,11,4×12 | 11,12 | items/STR_HEAVY_GAUSS: handSprite (Richtung 1) |
| 18 | 14,10,16×13 | 0,11,12 | items/STR_HEAVY_GAUSS: handSprite (Richtung 2) |
| 19 | 6,14,23×8 | 0,11,12 | items/STR_HEAVY_GAUSS: handSprite (Richtung 3) |
| 20 | 5,13,18×12 | 7,11,12 | items/STR_HEAVY_GAUSS: handSprite (Richtung 4) |
| 21 | 6,11,4×15 | 7,11,12 | items/STR_HEAVY_GAUSS: handSprite (Richtung 5) |
| 22 | 1,11,18×12 | 7,11,12 | items/STR_HEAVY_GAUSS: handSprite (Richtung 6) |
| 23 | 1,13,23×8 | 0,11,12 | items/STR_HEAVY_GAUSS: handSprite (Richtung 7) |
| 24 | 13,10,12×9 | 1,11 | items/STR_GAS_CANNON: handSprite (Richtung 0) |
| 25 | 22,11,4×11 | 1,2,11 | items/STR_GAS_CANNON: handSprite (Richtung 1) |
| 26 | 15,13,12×8 | 1,11 | items/STR_GAS_CANNON: handSprite (Richtung 2) |
| 27 | 10,18,18×5 | 1,2,11 | items/STR_GAS_CANNON: handSprite (Richtung 3) |
| 28 | 9,16,12×9 | 1,2,11 | items/STR_GAS_CANNON: handSprite (Richtung 4) |
| 29 | 6,14,4×11 | 1,11 | items/STR_GAS_CANNON: handSprite (Richtung 5) |
| 30 | 3,12,12×9 | 1,2,11 | items/STR_GAS_CANNON: handSprite (Richtung 6) |
| 31 | 4,15,18×5 | 1,2,11 | items/STR_GAS_CANNON: handSprite (Richtung 7) |
| 32 | 12,8,15×11 | 1,2,7,10,11,12 | items/STR_HYDRO_JET_CANNON: handSprite (Richtung 0) |
| 33 | 22,10,5×11 | 1,12 | items/STR_HYDRO_JET_CANNON: handSprite (Richtung 1) |
| 34 | 13,11,15×10 | 1,2,7,10,11,12 | items/STR_HYDRO_JET_CANNON: handSprite (Richtung 2) |
| 35 | 8,14,20×7 | 1,11,12 | items/STR_HYDRO_JET_CANNON: handSprite (Richtung 3) |
| 36 | 7,12,16×11 | 1,7,10,11,12 | items/STR_HYDRO_JET_CANNON: handSprite (Richtung 4) |
| 37 | 5,11,5×13 | 1,12 | items/STR_HYDRO_JET_CANNON: handSprite (Richtung 5) |
| 38 | 1,10,16×11 | 1,7,10,11,12 | items/STR_HYDRO_JET_CANNON: handSprite (Richtung 6) |
| 39 | 4,11,20×7 | 1,11,12 | items/STR_HYDRO_JET_CANNON: handSprite (Richtung 7) |
| 40 | 14,9,13×12 | 2,7,8,15 | items/STR_SONIC_CANNON: handSprite (Richtung 0); items/STR_TRIBIO_SONIC_WEAPON: handSprite (Richtung 0); items/XARQUID_WEAPON: handSprite (Richtung 0) |
| 41 | 20,11,9×14 | 7,8,15 | items/STR_SONIC_CANNON: handSprite (Richtung 1); items/STR_TRISCENE_MELEE_WEAPON: handSprite (Richtung 0); items/STR_TRIBIO_SONIC_WEAPON: handSprite (Richtung 1); items/XARQUID_WEAPON: handSprite (Richtung 1) |
| 42 | 13,11,13×12 | 2,7,8,15 | items/STR_SONIC_CANNON: handSprite (Richtung 2); items/STR_TRISCENE_MELEE_WEAPON: handSprite (Richtung 1); items/STR_TRIBIO_SONIC_WEAPON: handSprite (Richtung 2); items/XARQUID_WEAPON: handSprite (Richtung 2) |
| 43 | 3,14,21×9 | 7,8,15 | items/STR_SONIC_CANNON: handSprite (Richtung 3); items/STR_TRISCENE_MELEE_WEAPON: handSprite (Richtung 2); items/STR_TRIBIO_SONIC_WEAPON: handSprite (Richtung 3); items/XARQUID_WEAPON: handSprite (Richtung 3) |
| 44 | 7,12,15×11 | 7,8,15 | items/STR_SONIC_CANNON: handSprite (Richtung 4); items/STR_TRISCENE_MELEE_WEAPON: handSprite (Richtung 3); items/STR_TRIBIO_SONIC_WEAPON: handSprite (Richtung 4); items/XARQUID_WEAPON: handSprite (Richtung 4) |
| 45 | 3,5,9×15 | 7,8,15 | items/STR_SONIC_CANNON: handSprite (Richtung 5); items/STR_TRISCENE_MELEE_WEAPON: handSprite (Richtung 4); items/STR_TRIBIO_SONIC_WEAPON: handSprite (Richtung 5); items/XARQUID_WEAPON: handSprite (Richtung 5) |
| 46 | 1,10,16×11 | 7,8,15 | items/STR_SONIC_CANNON: handSprite (Richtung 6); items/STR_TRISCENE_MELEE_WEAPON: handSprite (Richtung 5); items/STR_TRIBIO_SONIC_WEAPON: handSprite (Richtung 6); items/XARQUID_WEAPON: handSprite (Richtung 6) |
| 47 | 9,11,21×9 | 7,8,15 | items/STR_SONIC_CANNON: handSprite (Richtung 7); items/STR_TRISCENE_MELEE_WEAPON: handSprite (Richtung 6); items/STR_TRIBIO_SONIC_WEAPON: handSprite (Richtung 7); items/XARQUID_WEAPON: handSprite (Richtung 7) |
| 48 | 11,9,15×10 | 2,7,8,15 | items/STR_SONIC_BLASTA_RIFLE: handSprite (Richtung 0); items/STR_TRISCENE_MELEE_WEAPON: handSprite (Richtung 7) |
| 49 | 22,11,5×14 | 7,8,15 | items/STR_SONIC_BLASTA_RIFLE: handSprite (Richtung 1) |
| 50 | 11,13,15×10 | 2,7,8,15 | items/STR_SONIC_BLASTA_RIFLE: handSprite (Richtung 2) |
| 51 | 4,16,19×5 | 7,8,15 | items/STR_SONIC_BLASTA_RIFLE: handSprite (Richtung 3) |
| 52 | 7,13,15×10 | 7,8,15 | items/STR_SONIC_BLASTA_RIFLE: handSprite (Richtung 4) |
| 53 | 5,6,5×14 | 7,8,15 | items/STR_SONIC_BLASTA_RIFLE: handSprite (Richtung 5) |
| 54 | 0,11,15×10 | 7,8,15 | items/STR_SONIC_BLASTA_RIFLE: handSprite (Richtung 6) |
| 55 | 9,14,19×5 | 7,8,15 | items/STR_SONIC_BLASTA_RIFLE: handSprite (Richtung 7) |
| 56 | 11,8,17×11 | 3,7,11,12,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: handSprite (Richtung 0) |
| 57 | 21,9,8×15 | 7,12,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: handSprite (Richtung 1) |
| 58 | 11,12,17×11 | 3,7,11,12,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: handSprite (Richtung 2) |
| 59 | 4,14,21×8 | 3,7,11,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: handSprite (Richtung 3) |
| 60 | 8,14,16×12 | 3,7,11,12,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: handSprite (Richtung 4) |
| 61 | 4,6,8×16 | 7,12,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: handSprite (Richtung 5) |
| 62 | 0,11,16×12 | 3,7,11,12,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: handSprite (Richtung 6) |
| 63 | 8,11,21×8 | 3,7,11,15 | items/STR_DISRUPTOR_PULSE_LAUNCHER: handSprite (Richtung 7) |
| 64 | 15,10,13×9 | 7,11,15 | items/STR_THERMAL_SHOK_LAUNCHER: handSprite (Richtung 0) |
| 65 | 20,11,7×10 | 7,11,15 | items/STR_THERMAL_SHOK_LAUNCHER: handSprite (Richtung 1) |
| 66 | 13,13,13×9 | 7,11,15 | items/STR_THERMAL_SHOK_LAUNCHER: handSprite (Richtung 2) |
| 67 | 11,13,14×7 | 7,11,15 | items/STR_THERMAL_SHOK_LAUNCHER: handSprite (Richtung 3) |
| 68 | 10,13,10×9 | 7,11,15 | items/STR_THERMAL_SHOK_LAUNCHER: handSprite (Richtung 4) |
| 69 | 4,12,7×10 | 7,11,15 | items/STR_THERMAL_SHOK_LAUNCHER: handSprite (Richtung 5) |
| 70 | 5,12,10×9 | 7,11,15 | items/STR_THERMAL_SHOK_LAUNCHER: handSprite (Richtung 6) |
| 71 | 8,12,14×7 | 7,11,15 | items/STR_THERMAL_SHOK_LAUNCHER: handSprite (Richtung 7) |
| 72 | 10,9,21×13 | 1,2,7,10,11,12 | items/STR_TORPEDO_LAUNCHER: handSprite (Richtung 0) |
| 73 | 22,11,5×15 | 1,12 | items/STR_TORPEDO_LAUNCHER: handSprite (Richtung 1) |
| 74 | 9,11,21×13 | 1,2,7,10,11,12 | items/STR_TORPEDO_LAUNCHER: handSprite (Richtung 2) |
| 75 | 6,14,25×7 | 1,2,7,11,12 | items/STR_TORPEDO_LAUNCHER: handSprite (Richtung 3) |
| 76 | 5,12,19×13 | 1,7,10,11,12 | items/STR_TORPEDO_LAUNCHER: handSprite (Richtung 4) |
| 77 | 6,11,5×15 | 1,7,12 | items/STR_TORPEDO_LAUNCHER: handSprite (Richtung 5) |
| 78 | 0,10,19×13 | 1,7,10,11,12 | items/STR_TORPEDO_LAUNCHER: handSprite (Richtung 6) |
| 79 | 1,13,25×6 | 1,2,7,11,12 | items/STR_TORPEDO_LAUNCHER: handSprite (Richtung 7) |
| 96 | 19,14,8×5 | 1,2,11 | items/STR_DART_PISTOL: handSprite (Richtung 0) |
| 97 | 19,16,9×2 | 1,2,11 | items/STR_DART_PISTOL: handSprite (Richtung 1) |
| 98 | 12,18,8×4 | 1,2,11 | items/STR_DART_PISTOL: handSprite (Richtung 2) |
| 99 | 9,18,2×5 | 1,2,11 | items/STR_DART_PISTOL: handSprite (Richtung 3) |
| 100 | 5,15,8×4 | 1,2,11 | items/STR_DART_PISTOL: handSprite (Richtung 4) |
| 101 | 6,14,9×2 | 1,2,11 | items/STR_DART_PISTOL: handSprite (Richtung 5) |
| 102 | 11,12,8×5 | 1,2,11 | items/STR_DART_PISTOL: handSprite (Richtung 6) |
| 103 | 22,13,2×5 | 1,2,11 | items/STR_DART_PISTOL: handSprite (Richtung 7) |
| 104 | 21,11,9×7 | 8,15 | items/STR_SONIC_PISTOL: handSprite (Richtung 0) |
| 105 | 19,15,11×5 | 8,15 | items/STR_SONIC_PISTOL: handSprite (Richtung 1) |
| 106 | 12,16,10×7 | 7,8,15 | items/STR_SONIC_PISTOL: handSprite (Richtung 2) |
| 107 | 9,15,5×9 | 7,8,15 | items/STR_SONIC_PISTOL: handSprite (Richtung 3) |
| 108 | 3,14,10×7 | 7,8,15 | items/STR_SONIC_PISTOL: handSprite (Richtung 4) |
| 109 | 4,14,11×5 | 8,15 | items/STR_SONIC_PISTOL: handSprite (Richtung 5) |
| 110 | 11,9,9×7 | 8,15 | items/STR_SONIC_PISTOL: handSprite (Richtung 6) |
| 111 | 20,11,5×7 | 8,15 | items/STR_SONIC_PISTOL: handSprite (Richtung 7) |
| 120 | 23,12,4×4 | 4,12,13,14 | items/STR_MAGNA_BLAST_GRENADE: handSprite (Richtung 0); items/STR_DYE_GRENADE: handSprite (Richtung 0); items/STR_PARTICLE_DISTURBANCE_GRENADE: handSprite (Richtung 0) |
| 121 | 19,15,4×4 | 4,12,13,14 | items/STR_MAGNA_BLAST_GRENADE: handSprite (Richtung 1); items/STR_DYE_GRENADE: handSprite (Richtung 1); items/STR_PARTICLE_DISTURBANCE_GRENADE: handSprite (Richtung 1) |
| 122 | 14,16,4×4 | 4,12,13,14 | items/STR_MAGNA_BLAST_GRENADE: handSprite (Richtung 2); items/STR_DYE_GRENADE: handSprite (Richtung 2); items/STR_PARTICLE_DISTURBANCE_GRENADE: handSprite (Richtung 2) |
| 123 | 7,16,4×4 | 4,12,13,14 | items/STR_MAGNA_BLAST_GRENADE: handSprite (Richtung 3); items/STR_DYE_GRENADE: handSprite (Richtung 3); items/STR_PARTICLE_DISTURBANCE_GRENADE: handSprite (Richtung 3) |
| 124 | 7,13,4×4 | 4,12,13,14 | items/STR_MAGNA_BLAST_GRENADE: handSprite (Richtung 4); items/STR_DYE_GRENADE: handSprite (Richtung 4); items/STR_PARTICLE_DISTURBANCE_GRENADE: handSprite (Richtung 4) |
| 125 | 9,11,4×4 | 4,12,13,14 | items/STR_MAGNA_BLAST_GRENADE: handSprite (Richtung 5); items/STR_DYE_GRENADE: handSprite (Richtung 5); items/STR_PARTICLE_DISTURBANCE_GRENADE: handSprite (Richtung 5) |
| 126 | 14,10,4×4 | 4,12,13,14 | items/STR_MAGNA_BLAST_GRENADE: handSprite (Richtung 6); items/STR_DYE_GRENADE: handSprite (Richtung 6); items/STR_PARTICLE_DISTURBANCE_GRENADE: handSprite (Richtung 6) |
| 127 | 21,11,4×4 | 4,12,13,14 | items/STR_MAGNA_BLAST_GRENADE: handSprite (Richtung 7); items/STR_DYE_GRENADE: handSprite (Richtung 7); items/STR_PARTICLE_DISTURBANCE_GRENADE: handSprite (Richtung 7) |
| 128 | 20,13,8×6 | 11,12 | items/STR_GAUSS_PISTOL: handSprite (Richtung 0) |
| 129 | 20,14,9×5 | 11,12 | items/STR_GAUSS_PISTOL: handSprite (Richtung 1) |
| 130 | 13,17,8×5 | 7,11,12 | items/STR_GAUSS_PISTOL: handSprite (Richtung 2) |
| 131 | 10,17,3×8 | 7,11,12 | items/STR_GAUSS_PISTOL: handSprite (Richtung 3) |
| 132 | 5,15,8×5 | 7,11,12 | items/STR_GAUSS_PISTOL: handSprite (Richtung 4) |
| 133 | 5,13,9×5 | 11,12 | items/STR_GAUSS_PISTOL: handSprite (Richtung 5) |
| 134 | 12,11,8×6 | 11,12 | items/STR_GAUSS_PISTOL: handSprite (Richtung 6) |
| 135 | 23,13,3×7 | 11,12 | items/STR_GAUSS_PISTOL: handSprite (Richtung 7) |
| 152 | 19,14,11×6 | 8,11 | items/STR_THERMAL_TAZER: handSprite (Richtung 0) |
| 153 | 17,16,13×4 | 8,11 | items/STR_THERMAL_TAZER: handSprite (Richtung 1) |
| 154 | 11,16,11×7 | 8,11 | items/STR_THERMAL_TAZER: handSprite (Richtung 2) |
| 155 | 8,16,4×8 | 8,11 | items/STR_THERMAL_TAZER: handSprite (Richtung 3) |
| 156 | 4,13,11×7 | 8,11 | items/STR_THERMAL_TAZER: handSprite (Richtung 4) |
| 157 | 3,13,13×4 | 8,11 | items/STR_THERMAL_TAZER: handSprite (Richtung 5) |
| 158 | 8,11,11×6 | 8,11 | items/STR_THERMAL_TAZER: handSprite (Richtung 6) |
| 159 | 21,11,4×7 | 8,11 | items/STR_THERMAL_TAZER: handSprite (Richtung 7) |
| 168 | 13,15,12×6 | 7,11,15 | items/STR_VIBRO_BLADE: handSprite (Richtung 0) |
| 169 | 19,16,8×7 | 11,15 | items/STR_VIBRO_BLADE: handSprite (Richtung 1) |
| 170 | 13,18,15×6 | 7,11,15 | items/STR_VIBRO_BLADE: handSprite (Richtung 2) |
| 171 | 7,16,15×15 | 7,11,15 | items/STR_VIBRO_BLADE: handSprite (Richtung 3) |
| 172 | 5,15,13×7 | 7,11,15 | items/STR_VIBRO_BLADE: handSprite (Richtung 4) |
| 173 | 5,13,6×11 | 11,15 | items/STR_VIBRO_BLADE: handSprite (Richtung 5) |
| 174 | 4,15,10×4 | 7,11,15 | items/STR_VIBRO_BLADE: handSprite (Richtung 6) |
| 175 | 12,15,14×13 | 7,11,15 | items/STR_VIBRO_BLADE: handSprite (Richtung 7) |
| 176 | 13,15,12×6 | 11,15 | items/STR_THERMIC_LANCE: handSprite (Richtung 0) |
| 177 | 19,16,8×7 | 11,15 | items/STR_THERMIC_LANCE: handSprite (Richtung 1) |
| 178 | 13,18,15×6 | 11,15 | items/STR_THERMIC_LANCE: handSprite (Richtung 2) |
| 179 | 7,18,13×11 | 7,11,15 | items/STR_THERMIC_LANCE: handSprite (Richtung 3) |
| 180 | 5,15,13×7 | 7,11,15 | items/STR_THERMIC_LANCE: handSprite (Richtung 4) |
| 181 | 5,13,6×11 | 11,15 | items/STR_THERMIC_LANCE: handSprite (Richtung 5) |
| 182 | 4,15,10×4 | 11,15 | items/STR_THERMIC_LANCE: handSprite (Richtung 6) |
| 183 | 14,15,12×11 | 7,11,15 | items/STR_THERMIC_LANCE: handSprite (Richtung 7) |
| 184 | 13,15,12×7 | 7,11,15 | items/STR_HEAVY_THERMIC_LANCE: handSprite (Richtung 0) |
| 185 | 19,16,8×7 | 11,15 | items/STR_HEAVY_THERMIC_LANCE: handSprite (Richtung 1) |
| 186 | 13,18,15×7 | 7,11,15 | items/STR_HEAVY_THERMIC_LANCE: handSprite (Richtung 2) |
| 187 | 7,16,15×15 | 7,11,15 | items/STR_HEAVY_THERMIC_LANCE: handSprite (Richtung 3) |
| 188 | 5,15,13×8 | 7,11,15 | items/STR_HEAVY_THERMIC_LANCE: handSprite (Richtung 4) |
| 189 | 5,13,6×11 | 11,15 | items/STR_HEAVY_THERMIC_LANCE: handSprite (Richtung 5) |
| 190 | 4,15,10×5 | 7,11,15 | items/STR_HEAVY_THERMIC_LANCE: handSprite (Richtung 6) |
| 191 | 11,15,15×14 | 7,11,15 | items/STR_HEAVY_THERMIC_LANCE: handSprite (Richtung 7) |
| 192 | 21,11,5×6 | 8,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: handSprite (Richtung 0) |
| 193 | 21,14,3×6 | 8,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: handSprite (Richtung 1) |
| 194 | 14,17,5×6 | 8,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: handSprite (Richtung 2) |
| 195 | 8,17,7×6 | 8,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: handSprite (Richtung 3) |
| 196 | 6,13,5×6 | 8,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: handSprite (Richtung 4) |
| 197 | 8,11,3×6 | 8,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: handSprite (Richtung 5) |
| 198 | 12,11,5×6 | 8,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: handSprite (Richtung 6) |
| 199 | 20,13,7×6 | 8,12 | items/STR_PARTICLE_DISTURBANCE_SENSOR: handSprite (Richtung 7) |
| 200 | 22,14,3×6 | 8,15 | items/STR_CANNON_POWER_CLIP: handSprite (Richtung 0) |
| 201 | 23,16,3×6 | 8,15 | items/STR_CANNON_POWER_CLIP: handSprite (Richtung 1) |
| 202 | 16,17,3×6 | 8,15 | items/STR_CANNON_POWER_CLIP: handSprite (Richtung 2) |
| 203 | 11,17,3×6 | 8,15 | items/STR_CANNON_POWER_CLIP: handSprite (Richtung 3) |
| 204 | 9,15,3×6 | 8,15 | items/STR_CANNON_POWER_CLIP: handSprite (Richtung 4) |
| 205 | 7,14,3×6 | 8,15 | items/STR_CANNON_POWER_CLIP: handSprite (Richtung 5) |
| 206 | 13,14,3×6 | 8,15 | items/STR_CANNON_POWER_CLIP: handSprite (Richtung 6) |
| 207 | 17,13,3×6 | 8,15 | items/STR_CANNON_POWER_CLIP: handSprite (Richtung 7) |
| 208 | 18,17,6×12 | 1,8,12,13 | items/STR_MEDI_KIT: handSprite (Richtung 0) |
| 209 | 20,19,3×12 | 1,8,12,13,14 | items/STR_MEDI_KIT: handSprite (Richtung 1) |
| 210 | 13,19,6×12 | 1,8,11,12,13,14,15 | items/STR_MEDI_KIT: handSprite (Richtung 2) |
| 211 | 6,19,6×12 | 1,8,11,12,13,14,15 | items/STR_MEDI_KIT: handSprite (Richtung 3) |
| 212 | 6,18,6×12 | 1,8,11,12,13,14,15 | items/STR_MEDI_KIT: handSprite (Richtung 4) |
| 213 | 9,16,3×12 | 1,8,12,13,14 | items/STR_MEDI_KIT: handSprite (Richtung 5) |
| 214 | 15,15,6×12 | 1,8,12,13 | items/STR_MEDI_KIT: handSprite (Richtung 6) |
| 215 | 19,17,6×12 | 1,8,12,13 | items/STR_MEDI_KIT: handSprite (Richtung 7) |
| 216 | 23,16,2×5 | 12 | items/STR_GAUSS_PISTOL_CLIP: handSprite (Richtung 0) |
| 217 | 22,17,3×5 | 12 | items/STR_GAUSS_PISTOL_CLIP: handSprite (Richtung 1) |
| 218 | 15,19,3×5 | 12 | items/STR_GAUSS_PISTOL_CLIP: handSprite (Richtung 2) |
| 219 | 11,18,3×5 | 11,12 | items/STR_GAUSS_PISTOL_CLIP: handSprite (Richtung 3) |
| 220 | 9,16,3×5 | 12 | items/STR_GAUSS_PISTOL_CLIP: handSprite (Richtung 4) |
| 221 | 7,14,3×5 | 11,12 | items/STR_GAUSS_PISTOL_CLIP: handSprite (Richtung 5) |
| 222 | 12,15,3×5 | 11,12 | items/STR_GAUSS_PISTOL_CLIP: handSprite (Richtung 6) |
| 223 | 17,15,3×5 | 11,12 | items/STR_GAUSS_PISTOL_CLIP: handSprite (Richtung 7) |
| 224 | 22,16,4×5 | 11,12 | items/STR_GAUSS_RIFLE_CLIP: handSprite (Richtung 0) |
| 225 | 22,17,4×5 | 11,12 | items/STR_GAUSS_RIFLE_CLIP: handSprite (Richtung 1) |
| 226 | 16,18,4×5 | 11,12 | items/STR_GAUSS_RIFLE_CLIP: handSprite (Richtung 2) |
| 227 | 10,18,4×5 | 11,12 | items/STR_GAUSS_RIFLE_CLIP: handSprite (Richtung 3) |
| 228 | 9,16,4×5 | 11,12 | items/STR_GAUSS_RIFLE_CLIP: handSprite (Richtung 4) |
| 229 | 6,15,4×5 | 11,12 | items/STR_GAUSS_RIFLE_CLIP: handSprite (Richtung 5) |
| 230 | 12,15,4×5 | 11,12 | items/STR_GAUSS_RIFLE_CLIP: handSprite (Richtung 6) |
| 231 | 17,15,4×5 | 11,12 | items/STR_GAUSS_RIFLE_CLIP: handSprite (Richtung 7) |
| 232 | 22,15,4×5 | 11,12 | items/STR_HEAVY_GAUSS_CLIP: handSprite (Richtung 0) |
| 233 | 23,17,4×5 | 11,12 | items/STR_HEAVY_GAUSS_CLIP: handSprite (Richtung 1) |
| 234 | 15,19,5×5 | 11,12 | items/STR_HEAVY_GAUSS_CLIP: handSprite (Richtung 2) |
| 235 | 10,18,5×5 | 11,12 | items/STR_HEAVY_GAUSS_CLIP: handSprite (Richtung 3) |
| 236 | 9,16,5×5 | 11,12 | items/STR_HEAVY_GAUSS_CLIP: handSprite (Richtung 4) |
| 237 | 5,14,5×5 | 11,12 | items/STR_HEAVY_GAUSS_CLIP: handSprite (Richtung 5) |
| 238 | 11,15,5×5 | 11,12 | items/STR_HEAVY_GAUSS_CLIP: handSprite (Richtung 6) |
| 239 | 17,15,5×5 | 11,12 | items/STR_HEAVY_GAUSS_CLIP: handSprite (Richtung 7) |
| 240 | 23,16,1×1 | 12 | items/STR_DART_POD: handSprite (Richtung 0) |
| 241 | 23,17,1×1 | 12 | items/STR_DART_POD: handSprite (Richtung 1) |
| 242 | 16,19,1×1 | 12 | items/STR_DART_POD: handSprite (Richtung 2) |
| 243 | 11,18,1×1 | 12 | items/STR_DART_POD: handSprite (Richtung 3) |
| 244 | 10,16,1×1 | 12 | items/STR_DART_POD: handSprite (Richtung 4) |
| 245 | 8,14,1×4 | 1,10,12 | items/STR_DART_POD: handSprite (Richtung 5) |
| 246 | 13,15,1×4 | 1,10,12 | items/STR_DART_POD: handSprite (Richtung 6) |
| 247 | 18,15,1×4 | 1,10,12 | items/STR_DART_POD: handSprite (Richtung 7) |
| 248 | 23,16,2×4 | 12 | items/STR_HARPOON_POD: handSprite (Richtung 0) |
| 249 | 23,17,2×1 | 12 | items/STR_HARPOON_POD: handSprite (Richtung 1) |
| 250 | 16,18,2×2 | 1,12 | items/STR_HARPOON_POD: handSprite (Richtung 2) |
| 251 | 11,18,2×1 | 12 | items/STR_HARPOON_POD: handSprite (Richtung 3) |
| 252 | 9,16,2×1 | 12 | items/STR_HARPOON_POD: handSprite (Richtung 4) |
| 253 | 7,15,2×4 | 1,10,12 | items/STR_HARPOON_POD: handSprite (Richtung 5) |
| 254 | 13,15,2×4 | 1,10,12 | items/STR_HARPOON_POD: handSprite (Richtung 6) |
| 255 | 18,16,2×4 | 1,10,12 | items/STR_HARPOON_POD: handSprite (Richtung 7) |
| 256 | 21,15,4×7 | 1,11,12 | items/STR_GC_HE_AMMO: handSprite (Richtung 0) |
| 257 | 23,15,2×8 | 11,12 | items/STR_GC_HE_AMMO: handSprite (Richtung 1) |
| 258 | 17,17,4×7 | 1,11,12 | items/STR_GC_HE_AMMO: handSprite (Richtung 2) |
| 259 | 11,18,5×6 | 1,11,12 | items/STR_GC_HE_AMMO: handSprite (Richtung 3) |
| 260 | 9,16,4×7 | 1,11,12 | items/STR_GC_HE_AMMO: handSprite (Richtung 4) |
| 261 | 7,15,2×8 | 1,11,12 | items/STR_GC_HE_AMMO: handSprite (Richtung 5) |
| 262 | 11,15,4×7 | 1,11,12 | items/STR_GC_HE_AMMO: handSprite (Richtung 6) |
| 263 | 15,15,5×6 | 1,11,12 | items/STR_GC_HE_AMMO: handSprite (Richtung 7) |
| 264 | 21,15,4×7 | 1,10,12 | items/STR_GC_P_AMMO: handSprite (Richtung 0) |
| 265 | 23,15,2×8 | 10,12 | items/STR_GC_P_AMMO: handSprite (Richtung 1) |
| 266 | 17,17,4×7 | 1,10,12 | items/STR_GC_P_AMMO: handSprite (Richtung 2) |
| 267 | 11,18,5×6 | 1,10,12 | items/STR_GC_P_AMMO: handSprite (Richtung 3) |
| 268 | 9,16,4×7 | 1,10,12 | items/STR_GC_P_AMMO: handSprite (Richtung 4) |
| 269 | 7,15,2×8 | 1,10,12 | items/STR_GC_P_AMMO: handSprite (Richtung 5) |
| 270 | 11,15,4×7 | 1,10,12 | items/STR_GC_P_AMMO: handSprite (Richtung 6) |
| 271 | 15,15,5×6 | 1,10,12 | items/STR_GC_P_AMMO: handSprite (Richtung 7) |
| 272 | 21,15,4×7 | 1,5,12 | items/STR_GC_AP_AMMO: handSprite (Richtung 0) |
| 273 | 23,15,2×8 | 5,12 | items/STR_GC_AP_AMMO: handSprite (Richtung 1) |
| 274 | 17,17,4×7 | 1,5,12 | items/STR_GC_AP_AMMO: handSprite (Richtung 2) |
| 275 | 11,18,5×6 | 1,5,12 | items/STR_GC_AP_AMMO: handSprite (Richtung 3) |
| 276 | 9,16,4×7 | 1,5,12 | items/STR_GC_AP_AMMO: handSprite (Richtung 4) |
| 277 | 7,15,2×8 | 1,5,12 | items/STR_GC_AP_AMMO: handSprite (Richtung 5) |
| 278 | 11,15,4×7 | 1,5,12 | items/STR_GC_AP_AMMO: handSprite (Richtung 6) |
| 279 | 15,15,5×6 | 1,5,12 | items/STR_GC_AP_AMMO: handSprite (Richtung 7) |
| 280 | 19,14,6×4 | 3,11 | items/STR_HJC_HE_AMMO: handSprite (Richtung 0) |
| 281 | 23,15,1×3 | 3 | items/STR_HJC_HE_AMMO: handSprite (Richtung 1) |
| 282 | 15,17,6×4 | 3,11 | items/STR_HJC_HE_AMMO: handSprite (Richtung 2) |
| 283 | 11,18,8×3 | 3,11 | items/STR_HJC_HE_AMMO: handSprite (Richtung 3) |
| 284 | 9,16,6×5 | 3,11 | items/STR_HJC_HE_AMMO: handSprite (Richtung 4) |
| 285 | 8,16,1×4 | 3 | items/STR_HJC_HE_AMMO: handSprite (Richtung 5) |
| 286 | 9,15,6×5 | 3,11 | items/STR_HJC_HE_AMMO: handSprite (Richtung 6) |
| 287 | 13,16,8×3 | 3,11 | items/STR_HJC_HE_AMMO: handSprite (Richtung 7) |
| 288 | 19,14,6×4 | 3,10 | items/STR_HJC_P_AMMO: handSprite (Richtung 0) |
| 289 | 23,15,1×3 | 3 | items/STR_HJC_P_AMMO: handSprite (Richtung 1) |
| 290 | 15,17,6×4 | 3,10 | items/STR_HJC_P_AMMO: handSprite (Richtung 2) |
| 291 | 11,18,8×3 | 3,10 | items/STR_HJC_P_AMMO: handSprite (Richtung 3) |
| 292 | 9,16,6×5 | 3,10 | items/STR_HJC_P_AMMO: handSprite (Richtung 4) |
| 293 | 8,16,1×4 | 3 | items/STR_HJC_P_AMMO: handSprite (Richtung 5) |
| 294 | 9,15,6×5 | 3,10 | items/STR_HJC_P_AMMO: handSprite (Richtung 6) |
| 295 | 13,16,8×3 | 3,10 | items/STR_HJC_P_AMMO: handSprite (Richtung 7) |
| 296 | 19,14,6×4 | 3,5 | items/STR_HJC_AP_AMMO: handSprite (Richtung 0) |
| 297 | 23,15,1×3 | 3 | items/STR_HJC_AP_AMMO: handSprite (Richtung 1) |
| 298 | 15,17,6×4 | 3,5 | items/STR_HJC_AP_AMMO: handSprite (Richtung 2) |
| 299 | 11,18,8×3 | 3,5 | items/STR_HJC_AP_AMMO: handSprite (Richtung 3) |
| 300 | 9,16,6×5 | 3,5 | items/STR_HJC_AP_AMMO: handSprite (Richtung 4) |
| 301 | 8,16,1×4 | 3 | items/STR_HJC_AP_AMMO: handSprite (Richtung 5) |
| 302 | 9,15,6×5 | 3,5 | items/STR_HJC_AP_AMMO: handSprite (Richtung 6) |
| 303 | 13,16,8×3 | 3,5 | items/STR_HJC_AP_AMMO: handSprite (Richtung 7) |
| 304 | 21,13,4×11 | 3,11,12 | items/STR_SMALL_TORPEDO: handSprite (Richtung 0) |
| 305 | 21,15,4×11 | 3,11,12 | items/STR_SMALL_TORPEDO: handSprite (Richtung 1) |
| 306 | 15,16,4×11 | 3,11,12 | items/STR_SMALL_TORPEDO: handSprite (Richtung 2) |
| 307 | 10,15,4×11 | 3,11,12 | items/STR_SMALL_TORPEDO: handSprite (Richtung 3) |
| 308 | 8,13,4×11 | 3,11,12 | items/STR_SMALL_TORPEDO: handSprite (Richtung 4) |
| 309 | 6,11,4×11 | 3,11,12 | items/STR_SMALL_TORPEDO: handSprite (Richtung 5) |
| 310 | 12,12,4×11 | 3,11,12 | items/STR_SMALL_TORPEDO: handSprite (Richtung 6) |
| 311 | 17,12,4×11 | 3,11,12 | items/STR_SMALL_TORPEDO: handSprite (Richtung 7) |
| 312 | 21,12,5×12 | 3,11 | items/STR_LARGE_TORPEDO: handSprite (Richtung 0) |
| 313 | 21,13,5×12 | 3,11 | items/STR_LARGE_TORPEDO: handSprite (Richtung 1) |
| 314 | 14,15,5×12 | 3,11 | items/STR_LARGE_TORPEDO: handSprite (Richtung 2) |
| 315 | 10,14,5×12 | 3,11 | items/STR_LARGE_TORPEDO: handSprite (Richtung 3) |
| 316 | 8,12,5×12 | 3,11 | items/STR_LARGE_TORPEDO: handSprite (Richtung 4) |
| 317 | 5,10,5×12 | 3,11 | items/STR_LARGE_TORPEDO: handSprite (Richtung 5) |
| 318 | 11,11,5×12 | 3,11 | items/STR_LARGE_TORPEDO: handSprite (Richtung 6) |
| 319 | 17,10,5×12 | 3,11 | items/STR_LARGE_TORPEDO: handSprite (Richtung 7) |
| 320 | 21,14,4×11 | 3,5 | items/STR_PHOSPHOROUS_TORPEDO: handSprite (Richtung 0) |
| 321 | 21,15,4×11 | 3,5 | items/STR_PHOSPHOROUS_TORPEDO: handSprite (Richtung 1) |
| 322 | 15,17,4×11 | 3,5 | items/STR_PHOSPHOROUS_TORPEDO: handSprite (Richtung 2) |
| 323 | 10,16,4×11 | 3,5 | items/STR_PHOSPHOROUS_TORPEDO: handSprite (Richtung 3) |
| 324 | 9,14,4×11 | 3,5 | items/STR_PHOSPHOROUS_TORPEDO: handSprite (Richtung 4) |
| 325 | 6,12,4×11 | 3,5 | items/STR_PHOSPHOROUS_TORPEDO: handSprite (Richtung 5) |
| 326 | 12,13,4×11 | 3,5 | items/STR_PHOSPHOROUS_TORPEDO: handSprite (Richtung 6) |
| 327 | 17,13,4×11 | 3,5 | items/STR_PHOSPHOROUS_TORPEDO: handSprite (Richtung 7) |
| 328 | 23,16,3×4 | 11,12 | items/STR_DISRUPTOR_AMMO: handSprite (Richtung 0) |
| 329 | 23,17,3×4 | 11,12 | items/STR_DISRUPTOR_AMMO: handSprite (Richtung 1) |
| 330 | 15,18,2×2 | 11,12 | items/STR_DISRUPTOR_AMMO: handSprite (Richtung 2) |
| 331 | 11,18,3×4 | 11,12 | items/STR_DISRUPTOR_AMMO: handSprite (Richtung 3) |
| 332 | 9,16,3×3 | 11,12 | items/STR_DISRUPTOR_AMMO: handSprite (Richtung 4) |
| 333 | 6,14,3×4 | 11,12 | items/STR_DISRUPTOR_AMMO: handSprite (Richtung 5) |
| 334 | 12,15,3×4 | 11,12 | items/STR_DISRUPTOR_AMMO: handSprite (Richtung 6) |
| 335 | 17,15,3×4 | 11,12 | items/STR_DISRUPTOR_AMMO: handSprite (Richtung 7) |
| 352 | 18,14,7×5 | 5,7,8,11,12 | items/STR_MAGNA_PACK_EXPLOSIVE: handSprite (Richtung 0) |
| 353 | 23,14,4×7 | 5,8,11,12 | items/STR_MAGNA_PACK_EXPLOSIVE: handSprite (Richtung 1) |
| 354 | 15,17,7×5 | 5,7,8,11,12 | items/STR_MAGNA_PACK_EXPLOSIVE: handSprite (Richtung 2) |
| 355 | 10,17,9×5 | 5,7,8,11,12 | items/STR_MAGNA_PACK_EXPLOSIVE: handSprite (Richtung 3) |
| 356 | 9,15,8×6 | 5,7,8,11,12 | items/STR_MAGNA_PACK_EXPLOSIVE: handSprite (Richtung 4) |
| 357 | 5,14,4×7 | 5,7,8,11,12 | items/STR_MAGNA_PACK_EXPLOSIVE: handSprite (Richtung 5) |
| 358 | 7,14,8×6 | 5,7,8,11,12 | items/STR_MAGNA_PACK_EXPLOSIVE: handSprite (Richtung 6) |
| 359 | 12,14,9×5 | 5,7,8,11,12 | items/STR_MAGNA_PACK_EXPLOSIVE: handSprite (Richtung 7) |
| 360 | 23,15,2×5 | 5,7,12 | items/STR_CHEMICAL_FLARE: handSprite (Richtung 0) |
| 361 | 23,16,2×2 | 5,7,12 | items/STR_CHEMICAL_FLARE: handSprite (Richtung 1) |
| 362 | 17,17,2×2 | 5,7,12 | items/STR_CHEMICAL_FLARE: handSprite (Richtung 2) |
| 363 | 11,17,2×2 | 5,7,12 | items/STR_CHEMICAL_FLARE: handSprite (Richtung 3) |
| 364 | 9,15,2×2 | 5,7,12 | items/STR_CHEMICAL_FLARE: handSprite (Richtung 4) |
| 365 | 7,14,2×5 | 5,7,12 | items/STR_CHEMICAL_FLARE: handSprite (Richtung 5) |
| 366 | 13,14,2×5 | 5,7,12 | items/STR_CHEMICAL_FLARE: handSprite (Richtung 6) |
| 367 | 18,15,2×5 | 5,7,12 | items/STR_CHEMICAL_FLARE: handSprite (Richtung 7) |
| 368 | 22,15,3×2 | 7,11,15 | items/STR_THERMAL_SHOK_BOMB: handSprite (Richtung 0); items/STR_SONIC_PULSER: handSprite (Richtung 0) |
| 369 | 23,16,3×3 | 7,11,15 | items/STR_THERMAL_SHOK_BOMB: handSprite (Richtung 1); items/STR_SONIC_PULSER: handSprite (Richtung 1) |
| 370 | 16,18,3×2 | 11,15 | items/STR_THERMAL_SHOK_BOMB: handSprite (Richtung 2); items/STR_SONIC_PULSER: handSprite (Richtung 2) |
| 371 | 11,18,3×1 | 15 | items/STR_THERMAL_SHOK_BOMB: handSprite (Richtung 3); items/STR_SONIC_PULSER: handSprite (Richtung 3) |
| 372 | 10,16,3×3 | 7,11,15 | items/STR_THERMAL_SHOK_BOMB: handSprite (Richtung 4); items/STR_SONIC_PULSER: handSprite (Richtung 4) |
| 373 | 7,14,3×4 | 7,11,15 | items/STR_THERMAL_SHOK_BOMB: handSprite (Richtung 5); items/STR_SONIC_PULSER: handSprite (Richtung 5) |
| 374 | 12,15,3×4 | 7,11,15 | items/STR_THERMAL_SHOK_BOMB: handSprite (Richtung 6); items/STR_SONIC_PULSER: handSprite (Richtung 6) |
| 375 | 17,15,3×4 | 7,11,15 | items/STR_THERMAL_SHOK_BOMB: handSprite (Richtung 7); items/STR_SONIC_PULSER: handSprite (Richtung 7) |
| 376 | 18,13,6×7 | 7,8,11,15 | items/STR_MC_READER: handSprite (Richtung 0) |
| 377 | 23,14,2×8 | 11 | items/STR_MC_READER: handSprite (Richtung 1) |
| 378 | 17,16,6×5 | 7,11 | items/STR_MC_READER: handSprite (Richtung 2) |
| 379 | 11,18,8×4 | 7,11 | items/STR_MC_READER: handSprite (Richtung 3) |
| 380 | 10,16,6×7 | 7,11 | items/STR_MC_READER: handSprite (Richtung 4) |
| 381 | 7,14,2×8 | 11 | items/STR_MC_READER: handSprite (Richtung 5) |
| 382 | 9,15,6×7 | 7,8,11,15 | items/STR_MC_READER: handSprite (Richtung 6) |
| 383 | 13,16,8×4 | 7,8,11,15 | items/STR_MC_READER: handSprite (Richtung 7) |
| 384 | 21,13,4×11 | 11,15 | items/STR_MC_DISRUPTOR: handSprite (Richtung 0) |
| 385 | 22,14,4×11 | 11,15 | items/STR_MC_DISRUPTOR: handSprite (Richtung 1) |
| 386 | 16,15,4×11 | 11,15 | items/STR_MC_DISRUPTOR: handSprite (Richtung 2) |
| 387 | 12,15,5×11 | 11,15 | items/STR_MC_DISRUPTOR: handSprite (Richtung 3) |
| 388 | 9,13,5×11 | 11,15 | items/STR_MC_DISRUPTOR: handSprite (Richtung 4) |
| 389 | 5,11,5×11 | 11,15 | items/STR_MC_DISRUPTOR: handSprite (Richtung 5) |
| 390 | 11,12,6×11 | 11,15 | items/STR_MC_DISRUPTOR: handSprite (Richtung 6) |
| 391 | 16,13,6×11 | 11,15 | items/STR_MC_DISRUPTOR: handSprite (Richtung 7) |
| 424 | 22,14,3×4 | 8,15 | items/STR_PISTOL_POWER_CLIP: handSprite (Richtung 0) |
| 425 | 23,16,3×4 | 8,15 | items/STR_PISTOL_POWER_CLIP: handSprite (Richtung 1) |
| 426 | 16,17,3×3 | 8,15 | items/STR_PISTOL_POWER_CLIP: handSprite (Richtung 2) |
| 427 | 11,17,3×2 | 8,15 | items/STR_PISTOL_POWER_CLIP: handSprite (Richtung 3) |
| 428 | 9,15,3×4 | 8,15 | items/STR_PISTOL_POWER_CLIP: handSprite (Richtung 4) |
| 429 | 7,14,3×4 | 8,15 | items/STR_PISTOL_POWER_CLIP: handSprite (Richtung 5) |
| 430 | 13,14,3×4 | 8,15 | items/STR_PISTOL_POWER_CLIP: handSprite (Richtung 6) |
| 431 | 17,13,3×4 | 8,15 | items/STR_PISTOL_POWER_CLIP: handSprite (Richtung 7) |
| 432 | 22,14,3×5 | 8,15 | items/STR_BLASTA_POWER_CLIP: handSprite (Richtung 0) |
| 433 | 23,16,3×5 | 8,15 | items/STR_BLASTA_POWER_CLIP: handSprite (Richtung 1) |
| 434 | 16,17,3×3 | 8,15 | items/STR_BLASTA_POWER_CLIP: handSprite (Richtung 2) |
| 435 | 11,17,3×5 | 8,15 | items/STR_BLASTA_POWER_CLIP: handSprite (Richtung 3) |
| 436 | 9,15,3×4 | 8,15 | items/STR_BLASTA_POWER_CLIP: handSprite (Richtung 4) |
| 437 | 7,14,3×5 | 8,15 | items/STR_BLASTA_POWER_CLIP: handSprite (Richtung 5) |
| 438 | 13,14,3×5 | 8,15 | items/STR_BLASTA_POWER_CLIP: handSprite (Richtung 6) |
| 439 | 17,13,3×5 | 8,15 | items/STR_BLASTA_POWER_CLIP: handSprite (Richtung 7) |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 80–95, 112–119, 136–151, 160–167, 336–351, 392–423

### UNITS/LOBSTER.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 260 Frames (260 belegt) · Farbgruppen: 2,5,7,8,10,11

Verwendung: armors/LOBSTERMAN_ARMOR0: spriteSheet (drawingRoutine 14); armors/LOBSTERMAN_ARMOR1: spriteSheet (drawingRoutine 14); armors/LOBSTERMAN_ARMOR2: spriteSheet (drawingRoutine 14)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–259

### UNITS/TANK01.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 296 Frames (296 belegt) · Farbgruppen: 1,2,3,4,5,7,8,10,11,12,13,15

Verwendung: armors/COELACANTH_ARMOR: spriteSheet (drawingRoutine 11); armors/DISPLACER_ARMOR: spriteSheet (drawingRoutine 11)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–295

### UNITS/TASOTHS.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 260 Frames (260 belegt) · Farbgruppen: 0,1,2,3,4,7,8,10,11,14

Verwendung: armors/TASOTH_ARMOR0: spriteSheet (drawingRoutine 14)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–259

### UNITS/TDXCOM_0.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 286 Frames (286 belegt) · Farbgruppen: 1,2,3,4,5,6,7,8,10,14,15

Verwendung: armors/STR_NONE_UC: spriteSheet (drawingRoutine 13)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–285

### UNITS/TDXCOM_1.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 278 Frames (278 belegt) · Farbgruppen: 1,2,3,4,5,6,8,10,12,15

Verwendung: armors/STR_PLASTIC_AQUA_ARMOR_UC: spriteSheet (drawingRoutine 13)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–277

### UNITS/TDXCOM_2.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 302 Frames (302 belegt) · Farbgruppen: 1,2,3,4,5,6,7,8,9,10,11,12,15

Verwendung: armors/STR_ION_ARMOR_UC: spriteSheet (drawingRoutine 13); armors/STR_MAGNETIC_ION_ARMOR_UC: spriteSheet (drawingRoutine 13)

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 233 | 18,17,1×1 | 8 | ersetzt/ergänzt durch Repo-Datei Resources/Sprite/missing_arm.png (extraSprites) |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–232, 234–301

### UNITS/TENTAC.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 20 Frames (20 belegt) · Farbgruppen: 1,2,3,7,8,10,12,14

Verwendung: armors/TENTACULAT_ARMOR0: spriteSheet (drawingRoutine 19)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–19

### UNITS/TRISCEN.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 160 Frames (160 belegt) · Farbgruppen: 1,2,3,4,5,6,7,8,10,11,12,13,14,15

Verwendung: armors/TRISCENE_ARMOR0: spriteSheet (drawingRoutine 20)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–159

### UNITS/XARQUID.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 128 Frames (128 belegt) · Farbgruppen: 1,2,3,4,5,6,7,8,10,11,12,13,14

Verwendung: armors/XARQUID_ARMOR0: spriteSheet (drawingRoutine 21)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–127

### UNITS/ZOMBIE.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 82 Frames (82 belegt) · Farbgruppen: 1,2,3,5,6,7,8,10,11,14,15

Verwendung: armors/ZOMBIE_ARMOR: spriteSheet (drawingRoutine 17)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–81

## Vollbilder und Einzelgrafiken

| Datei | Format | Größe → HD | Farbgruppen | Zweck | Verwendung |
|---|---|---|---|---|---|
| FLOP_INT/GAO01.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/GAO02.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/GAO03.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/GAO04.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/GAO05.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT00.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT01.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT02.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT03.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT04.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT05.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT06.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT07.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT08.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT09.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT10.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT11.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT12.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT13.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT14.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT15.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT16.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT17.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT18.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT19.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT20.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/INT21.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT01.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT02.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT03.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT04.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT05.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT06.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT07.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT08.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT09.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT10.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT11.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/OUT12.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/TWE01.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/TWE02.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/TWE03.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/TWE04.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/TWE05.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| FLOP_INT/TWE06.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| GEODATA/INTERWIN.DAT | SCR | 160×598 → 640×2392 | 0,3,4,7,8,10,11 | Abfangfenster: Vorschaubilder der Flugobjekte, untereinander gestapelt | ufos/STR_SURVEY_SHIP: sprite (Vorschau-Zeile 0); ufos/STR_ESCORT: sprite (Vorschau-Zeile 1); ufos/STR_CRUISER: sprite (Vorschau-Zeile 2); ufos/STR_HEAVY_CRUISER: sprite (Vorschau-Zeile 3); ufos/STR_HUNTER: sprite (Vorschau-Zeile 4); ufos/STR_BATTLESHIP: sprite (Vorschau-Zeile 7); ufos/STR_DREADNOUGHT: sprite (Vorschau-Zeile 6); ufos/STR_FLEET_SUPPLY_CRUISER: sprite (Vorschau-Zeile 5) |
| GEOGRAPH/BACK01.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/mainMenu: backgroundImage; interfaces/newBattleMenu: backgroundImage; interfaces/newGameMenu: backgroundImage; interfaces/endGameStatistics: backgroundImage; interfaces/optionsMenu: backgroundImage; interfaces/modsMenu: backgroundImage; interfaces/saveMenus: backgroundImage; interfaces/pauseMenu: backgroundImage; interfaces/noteMenu: backgroundImage; interfaces/selectMusicTrack: backgroundImage; interfaces/dogfightExperience: backgroundImage; interfaces/targetInfo: backgroundImage; interfaces/baseNaming: backgroundImage; interfaces/psiTraining: backgroundImage; interfaces/allocatePsi: backgroundImage; interfaces/oxceLinks: backgroundImage; interfaces/ufopaedia: backgroundImage; interfaces/promotions: backgroundImage; interfaces/cannotReequip: backgroundImage; interfaces/debriefing: backgroundImage; interfaces/commendations: backgroundImage; interfaces/geoscape: backgroundImage; interfaces/placeFacility: backgroundImage |
| GEOGRAPH/BACK02.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/trainingFinished: backgroundImage; interfaces/martialTraining: backgroundImage; interfaces/allocateMartial: backgroundImage; interfaces/commendationsLate: backgroundImage; interfaces/soldierList: backgroundImage; interfaces/soldierMemorial: backgroundImage; interfaces/soldierDiary: backgroundImage; interfaces/soldierDiaryPerformance: backgroundImage; interfaces/craftSoldiers: backgroundImage; interfaces/craftPilots: backgroundImage; interfaces/craftPilotsSelect: backgroundImage; interfaces/soldierTransformation: backgroundImage; interfaces/transformationList: backgroundImage |
| GEOGRAPH/BACK03.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK04.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/baseDefense: backgroundImage; interfaces/baseDefense_geo: backgroundImage; interfaces/craftEquipment: backgroundImage; interfaces/craftEquipmentSave: backgroundImage; interfaces/craftEquipmentLoad: backgroundImage |
| GEOGRAPH/BACK05.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/tests: backgroundImage; interfaces/geoResearch: backgroundImage; interfaces/geoResearchComplete: backgroundImage; interfaces/geoResearchRequired: backgroundImage; interfaces/manageContainment: backgroundImage; interfaces/globalContainmentMenu: backgroundImage; interfaces/selectFacility: backgroundImage; interfaces/researchMenu: backgroundImage; interfaces/globalResearchMenu: backgroundImage; interfaces/globalResearchDiary: backgroundImage; interfaces/selectNewResearch: backgroundImage; interfaces/allocateResearch: backgroundImage; interfaces/techTreeViewer: backgroundImage; interfaces/techTreeSelect: backgroundImage; interfaces/statsForNerds: backgroundImage |
| GEOGRAPH/BACK06.SCR | SCR | 320×200 → 1280×800 | 0,4,12 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK07.SCR | SCR | 320×200 → 1280×800 | 3,4 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK08.SCR | SCR | 320×200 → 1280×800 | 0,7,9 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/articleTFTD: backgroundImage; interfaces/articleCraftTFTD: backgroundImage; interfaces/articleCraftWeaponTFTD: backgroundImage; interfaces/articleVehicleTFTD: backgroundImage; interfaces/articleItemTFTD: backgroundImage; interfaces/articleArmorTFTD: backgroundImage; interfaces/articleBaseFacilityTFTD: backgroundImage; interfaces/articleUsoTFTD: backgroundImage |
| GEOGRAPH/BACK12.SCR | SCR | 320×200 → 1280×800 | 14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/geoCraft: backgroundImage; interfaces/intercept: backgroundImage; interfaces/ufoTracker: backgroundImage; interfaces/craftPatrol: backgroundImage; interfaces/craftError: backgroundImage; interfaces/craftPilotError: backgroundImage; interfaces/confirmDestination: backgroundImage; interfaces/confirmCydonia: backgroundImage; interfaces/lowFuel: backgroundImage |
| GEOGRAPH/BACK13.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/alienBase: backgroundImage; interfaces/itemsArriving: backgroundImage; interfaces/geoNewCraft: backgroundImage; interfaces/geoNewFacility: backgroundImage; interfaces/geoNewItem: backgroundImage; interfaces/monthlyReport: backgroundImage; interfaces/fundingWindow: backgroundImage; interfaces/sellMenu: backgroundImage; interfaces/dependencyTree: backgroundImage; interfaces/itemLocations: backgroundImage; interfaces/buyMenu: backgroundImage; interfaces/transferBaseSelect: backgroundImage; interfaces/transferMenu: backgroundImage; interfaces/transferConfirm: backgroundImage; interfaces/transferInfo: backgroundImage; interfaces/dismantleFacility: backgroundImage; interfaces/storesInfo: backgroundImage; interfaces/costsInfo: backgroundImage; interfaces/sackSoldier: backgroundImage |
| GEOGRAPH/BACK14.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/soldierArmor: backgroundImage; interfaces/soldierTransform: backgroundImage; interfaces/soldierRank: backgroundImage; interfaces/soldierAvatar: backgroundImage; interfaces/soldierDiaryLight: backgroundImage; interfaces/soldierBonus: backgroundImage; interfaces/craftArmor: backgroundImage; interfaces/craftInfo: backgroundImage; interfaces/craftWeapons: backgroundImage; interfaces/craftSelect: backgroundImage |
| GEOGRAPH/BACK15.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/confirmLanding: backgroundImage; interfaces/UFOInfo: backgroundImage; interfaces/UFOLost: backgroundImage; interfaces/multipleTargets: backgroundImage; interfaces/baseDestroyed: backgroundImage; interfaces/dogfightInfo: backgroundImage |
| GEOGRAPH/BACK16.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/soldierDiaryMission: backgroundImage |
| GEOGRAPH/BACK17.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/geoManufacture: backgroundImage; interfaces/geoManufactureComplete: backgroundImage; interfaces/manufactureMenu: backgroundImage; interfaces/globalManufactureMenu: backgroundImage; interfaces/selectNewManufacture: backgroundImage; interfaces/allocateManufacture: backgroundImage; interfaces/manufactureInfo: backgroundImage |
| GEOGRAPH/BACKG.LBM | LBM | 320×200 → 1280×800 | – | Vollbild-Grafik im LBM-Format |  |
| GEOGRAPH/GEOBORD.SCR | SCR | 320×200 → 1280×800 | 0,1,2,5,6,9,11,12,13,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/GRAPH.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,6,7,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/GRAPHS.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,8,11,12,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/UP001.BDY | BDY | 320×200 → 1280×800 | 1,2,5,6,7,10,12,13 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TRITON: image_id |
| GEOGRAPH/UP002.BDY | BDY | 320×200 → 1280×800 | 1,3,5,6,7,12,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MANTA: image_id |
| GEOGRAPH/UP003.BDY | BDY | 320×200 → 1280×800 | 1,3,5,6,7,8,12 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_LEVIATHAN: image_id |
| GEOGRAPH/UP004.BDY | BDY | 320×200 → 1280×800 | 1,2,3,5,6,7,10,12,13 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_BARRACUDA: image_id |
| GEOGRAPH/UP005.BDY | BDY | 320×200 → 1280×800 | 5,6,7,8,10,12,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_HAMMERHEAD: image_id |
| GEOGRAPH/UP006.BDY | BDY | 320×200 → 1280×800 | 1,3,4,5,6,7,12,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_AQUATOID: image_id |
| GEOGRAPH/UP007.BDY | BDY | 320×200 → 1280×800 | 1,2,3,5,6,7,8,11,12,13,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_AQUATOID_AUTOPSY: image_id |
| GEOGRAPH/UP008.BDY | BDY | 320×200 → 1280×800 | 1,2,3,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GILLMAN: image_id |
| GEOGRAPH/UP009.BDY | BDY | 320×200 → 1280×800 | 1,2,3,5,6,7,8,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GILLMAN_AUTOPSY: image_id |
| GEOGRAPH/UP010.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_LOBSTERMAN: image_id |
| GEOGRAPH/UP011.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_LOBSTERMAN_AUTOPSY: image_id |
| GEOGRAPH/UP012.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TASOTH: image_id |
| GEOGRAPH/UP013.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DEEP_ONE: image_id |
| GEOGRAPH/UP014.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DEEP_ONE_AUTOPSY: image_id |
| GEOGRAPH/UP015.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TENTACULAT: image_id |
| GEOGRAPH/UP016.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TENTACULAT_AUTOPSY: image_id |
| GEOGRAPH/UP017.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,6,7,8,10,11,12,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TRISCENE: image_id |
| GEOGRAPH/UP018.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_HALLUCINOID: image_id |
| GEOGRAPH/UP019.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_HALLUCINOID_AUTOPSY: image_id |
| GEOGRAPH/UP020.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_XARQUID: image_id |
| GEOGRAPH/UP021.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_XARQUID_AUTOPSY: image_id |
| GEOGRAPH/UP022.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MAGNETIC_ION_ARMOR_UC: image_id |
| GEOGRAPH/UP023.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_PLASTIC_AQUA_ARMOR_UC: image_id |
| GEOGRAPH/UP024.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ION_ARMOR_UC: image_id |
| GEOGRAPH/UP025.BDY | BDY | 320×200 → 1280×800 | 1,2,3,7,10,11,12,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_CALCINITE: image_id |
| GEOGRAPH/UP026.BDY | BDY | 320×200 → 1280×800 | 1,3,5,6,7,8,12,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_CALCINITE_AUTOPSY: image_id |
| GEOGRAPH/UP027.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_COELACANTH_GAS_CANNON: image_id |
| GEOGRAPH/UP028.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,12,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DISPLACER_SONIC: image_id |
| GEOGRAPH/UP029.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TRISCENE_AUTOPSY: image_id |
| GEOGRAPH/UP030.BDY | BDY | 320×200 → 1280×800 | 0,6,7,10,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_PARTICLE_DISTURBANCE_SENSOR: image_id |
| GEOGRAPH/UP031.BDY | BDY | 320×200 → 1280×800 | 0,1,6,7,10,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GAUSS_PISTOL: image_id |
| GEOGRAPH/UP032.BDY | BDY | 320×200 → 1280×800 | 0,1,2,6,7,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GAUSS_RIFLE: image_id |
| GEOGRAPH/UP033.BDY | BDY | 320×200 → 1280×800 | 0,1,6,7,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_HEAVY_GAUSS: image_id |
| GEOGRAPH/UP034.BDY | BDY | 320×200 → 1280×800 | 0,6,7,10,12,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DART_PISTOL: image_id |
| GEOGRAPH/UP035.BDY | BDY | 320×200 → 1280×800 | 0,1,5,6,7,10,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_JET_HARPOON: image_id |
| GEOGRAPH/UP036.BDY | BDY | 320×200 → 1280×800 | 0,1,5,6,7,10,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GAS_CANNON: image_id |
| GEOGRAPH/UP037.BDY | BDY | 320×200 → 1280×800 | 0,1,2,5,6,7,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_HYDRO_JET_CANNON: image_id |
| GEOGRAPH/UP038.BDY | BDY | 320×200 → 1280×800 | 0,1,2,5,6,7,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TORPEDO_LAUNCHER: image_id |
| GEOGRAPH/UP039.BDY | BDY | 320×200 → 1280×800 | 0,6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MAGNA_BLAST_GRENADE: image_id |
| GEOGRAPH/UP040.BDY | BDY | 320×200 → 1280×800 | 0,6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DYE_GRENADE: image_id |
| GEOGRAPH/UP041.BDY | BDY | 320×200 → 1280×800 | 0,6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_PARTICLE_DISTURBANCE_GRENADE: image_id |
| GEOGRAPH/UP042.BDY | BDY | 320×200 → 1280×800 | 0,1,5,6,7,10,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MAGNA_PACK_EXPLOSIVE: image_id |
| GEOGRAPH/UP043.BDY | BDY | 320×200 → 1280×800 | 0,5,6,7,10 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_CHEMICAL_FLARE: image_id |
| GEOGRAPH/UP044.BDY | BDY | 320×200 → 1280×800 | 0,1,5,6,7,10,12 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_THERMAL_TAZER: image_id |
| GEOGRAPH/UP045.BDY | BDY | 320×200 → 1280×800 | 0,6,7,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_SONIC_PULSER: image_id |
| GEOGRAPH/UP046.BDY | BDY | 320×200 → 1280×800 | 0,1,2,5,6,7,8,10,12 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MC_READER: image_id |
| GEOGRAPH/UP047.BDY | BDY | 320×200 → 1280×800 | 0,1,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DISRUPTOR_PULSE_LAUNCHER: image_id |
| GEOGRAPH/UP048.BDY | BDY | 320×200 → 1280×800 | 6,7,8,10,11,12,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_THERMAL_SHOK_LAUNCHER: image_id |
| GEOGRAPH/UP049.BDY | BDY | 320×200 → 1280×800 | 1,5,6,7,8,10,11,12,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_SONIC_PISTOL: image_id |
| GEOGRAPH/UP050.BDY | BDY | 320×200 → 1280×800 | 1,5,6,7,8,10,11,12,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_SONIC_BLASTA_RIFLE: image_id |
| GEOGRAPH/UP051.BDY | BDY | 320×200 → 1280×800 | 1,2,6,7,8,10,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_SONIC_CANNON: image_id |
| GEOGRAPH/UP052.BDY | BDY | 320×200 → 1280×800 | 0,1,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MEDI_KIT: image_id |
| GEOGRAPH/UP053.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TASOTH_AUTOPSY: image_id |
| GEOGRAPH/UP054.BDY | BDY | 320×200 → 1280×800 | 0,4,6,7,10 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_AJAX: image_id |
| GEOGRAPH/UP055.BDY | BDY | 320×200 → 1280×800 | 0,3,6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DUP_HEAD: image_id |
| GEOGRAPH/UP056.BDY | BDY | 320×200 → 1280×800 | 1,2,4,5,6,7,8,10,12,13 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_CRAFT_GAS_CANNON_UC: image_id |
| GEOGRAPH/UP057.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_COELACANTH_AQUA_JET: image_id |
| GEOGRAPH/UP058.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,12,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DISPLACER_PWT: image_id |
| GEOGRAPH/UP059.BDY | BDY | 320×200 → 1280×800 | 0,1,3,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ION_BEAM_ACCELERATORS: image_id |
| GEOGRAPH/UP060.BDY | BDY | 320×200 → 1280×800 | 0,1,3,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MAGNETIC_NAVIGATION: image_id |
| GEOGRAPH/UP061.BDY | BDY | 320×200 → 1280×800 | 0,1,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ALIEN_CRYOGENICS: image_id |
| GEOGRAPH/UP062.BDY | BDY | 320×200 → 1280×800 | 0,1,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ALIEN_CLONING: image_id |
| GEOGRAPH/UP063.BDY | BDY | 320×200 → 1280×800 | 0,1,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ALIEN_LEARNING_ARRAYS: image_id |
| GEOGRAPH/UP064.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ALIEN_IMPLANTER: image_id |
| GEOGRAPH/UP065.BDY | BDY | 320×200 → 1280×800 | 0,1,3,4,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_EXAMINATION_ROOM: image_id |
| GEOGRAPH/UP066.BDY | BDY | 320×200 → 1280×800 | 3,6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_AQUA_PLASTICS: image_id |
| GEOGRAPH/UP067.BDY | BDY | 320×200 → 1280×800 | 6,7,10,11 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ZRBITE: image_id |
| GEOGRAPH/UP068.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_COELACANTH_GAUSS: image_id |
| GEOGRAPH/UP069.BDY | BDY | 320×200 → 1280×800 | 5,6,7,12,13 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ALIEN_SUB_CONSTRUCTION: image_id |
| GEOGRAPH/UP070.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_BIODRONE: image_id |
| GEOGRAPH/UP071.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_BIODRONE_AUTOPSY: image_id |
| GEOGRAPH/UP072.BDY | BDY | 320×200 → 1280×800 | 0,6,7,8,12 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DISRUPTOR_AMMO: image_id |
| GEOGRAPH/UP073.BDY | BDY | 320×200 → 1280×800 | 1,2,6,7,10,12,13 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GAUSS_CANNON_UC: image_id |
| GEOGRAPH/UP074.BDY | BDY | 320×200 → 1280×800 | 0,3,4,6,7,8,12,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_PWT_CANNON_UC: image_id |
| GEOGRAPH/UP075.BDY | BDY | 320×200 → 1280×800 | 6,7,8,10 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_SONIC_OSCILLATOR_UC: image_id |
| GEOGRAPH/UP076.BDY | BDY | 320×200 → 1280×800 | 0,1,2,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_LIVING_QUARTERS: image_id |
| GEOGRAPH/UP077.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_STANDARD_SONAR: image_id |
| GEOGRAPH/UP078.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_AIR_LOCK: image_id |
| GEOGRAPH/UP079.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_WIDE_ARRAY_SONAR: image_id |
| GEOGRAPH/UP080.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ALIEN_CONTAINMENT: image_id |
| GEOGRAPH/UP081.BDY | BDY | 320×200 → 1280×800 | 0,1,2,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GENERAL_STORES: image_id |
| GEOGRAPH/UP082.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TORPEDO_DEFENSES: image_id |
| GEOGRAPH/UP083.BDY | BDY | 320×200 → 1280×800 | 0,1,2,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GAUSS_DEFENSES: image_id |
| GEOGRAPH/UP084.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_LABORATORY: image_id |
| GEOGRAPH/UP085.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_SONIC_DEFENSES: image_id |
| GEOGRAPH/UP086.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_PWT_DEFENSES: image_id |
| GEOGRAPH/UP087.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_WORKSHOP: image_id |
| GEOGRAPH/UP088.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MC_LAB: image_id |
| GEOGRAPH/UP089.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_SUB_PEN: image_id |
| GEOGRAPH/UP090.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_BOMBARDMENT_SHIELD: image_id |
| GEOGRAPH/UP091.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_TRANSMISSION_RESOLVER: image_id |
| GEOGRAPH/UP092.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MC_GENERATOR: image_id |
| GEOGRAPH/UP093.BDY | BDY | 320×200 → 1280×800 | 6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_SURVEY_SHIP: image_id |
| GEOGRAPH/UP094.BDY | BDY | 320×200 → 1280×800 | 6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ESCORT: image_id |
| GEOGRAPH/UP095.BDY | BDY | 320×200 → 1280×800 | 6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_CRUISER: image_id |
| GEOGRAPH/UP096.BDY | BDY | 320×200 → 1280×800 | 6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_HEAVY_CRUISER: image_id |
| GEOGRAPH/UP097.BDY | BDY | 320×200 → 1280×800 | 6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_HUNTER: image_id |
| GEOGRAPH/UP098.BDY | BDY | 320×200 → 1280×800 | 6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_BATTLESHIP: image_id |
| GEOGRAPH/UP099.BDY | BDY | 320×200 → 1280×800 | 6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_DREADNOUGHT: image_id |
| GEOGRAPH/UP100.BDY | BDY | 320×200 → 1280×800 | 6,7 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_FLEET_SUPPLY_CRUISER: image_id |
| GEOGRAPH/UP101.BDY | BDY | 320×200 → 1280×800 | 0,6,7,8,11,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_THERMAL_SHOK_BOMB: image_id |
| GEOGRAPH/UP102.BDY | BDY | 320×200 → 1280×800 | 1,6,7,8,10,12 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_PISTOL_POWER_CLIP: image_id |
| GEOGRAPH/UP103.BDY | BDY | 320×200 → 1280×800 | 1,6,7,8,10,12 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_BLASTA_POWER_CLIP: image_id |
| GEOGRAPH/UP104.BDY | BDY | 320×200 → 1280×800 | 1,6,7,8,10,12 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_CANNON_POWER_CLIP: image_id |
| GEOGRAPH/UP105.BDY | BDY | 320×200 → 1280×800 | 3 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_ALIEN_ORIGINS: image_id; ufopaedia/STR_THE_ULTIMATE_THREAT: image_id; ufopaedia/STR_TLETH_THE_ALIEN_CITY: image_id; ufopaedia/STR_ALIEN_PROBE_MISSION: image_id; ufopaedia/STR_ALIEN_INTERDICTION: image_id; ufopaedia/STR_ALIEN_RESOURCE_RAID: image_id; ufopaedia/STR_ALIEN_INFILTRATION: image_id; ufopaedia/STR_ALIEN_BASE: image_id; ufopaedia/STR_ALIEN_SURFACE_ATTACK: image_id; ufopaedia/STR_ALIEN_RETALIATION: image_id; ufopaedia/STR_ALIEN_SUPPLY: image_id |
| GEOGRAPH/UP106.BDY | BDY | 320×200 → 1280×800 | 0,6,7,8,10,12,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_MC_DISRUPTOR: image_id |
| GEOGRAPH/UP107.BDY | BDY | 320×200 → 1280×800 | 0,1,2,6,7,8,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_VIBRO_BLADE: image_id |
| GEOGRAPH/UP108.BDY | BDY | 320×200 → 1280×800 | 0,1,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_THERMIC_LANCE: image_id |
| GEOGRAPH/UP109.BDY | BDY | 320×200 → 1280×800 | 0,1,6,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_HEAVY_THERMIC_LANCE: image_id |
| GEOGRAPH/UP110.BDY | BDY | 320×200 → 1280×800 | 6,7,11,12,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GAUSS_PISTOL_CLIP: image_id |
| GEOGRAPH/UP111.BDY | BDY | 320×200 → 1280×800 | 6,7,11,12,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_GAUSS_RIFLE_CLIP: image_id |
| GEOGRAPH/UP112.BDY | BDY | 320×200 → 1280×800 | 6,7,11,12,14,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | ufopaedia/STR_HEAVY_GAUSS_CLIP: image_id |
| GEOGRAPH/UP_BORD2.SCR | SCR | 320×200 → 1280×800 | 0,8,10,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| TERRAIN/ISLAND1.LBM | LBM | 320×200 → 1280×800 | – |  |  |
| TERRAIN/ISLAND2.LBM | LBM | 320×200 → 1280×800 | – |  |  |
| TERRAIN/ISLAND3.LBM | LBM | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/D0.LBM | LBM | 320×200 → 1280×800 | – | Kampf-Palette/Grafik im LBM-Format |  |
| UFOGRAPH/D1.LBM | LBM | 320×200 → 1280×800 | – | Kampf-Palette/Grafik im LBM-Format |  |
| UFOGRAPH/D2.LBM | LBM | 320×200 → 1280×800 | – | Kampf-Palette/Grafik im LBM-Format |  |
| UFOGRAPH/D3.LBM | LBM | 320×200 → 1280×800 | – | Kampf-Palette/Grafik im LBM-Format |  |
| UFOGRAPH/DETBORD.BDY | BDY | 320×200 → 1280×800 | 0,2,3,4,6,7,8,9,10,11,12,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format |  |
| UFOGRAPH/DETBORD2.BDY | BDY | 320×200 → 1280×800 | 0,9,12 | Vollbild-Grafik (Kampf), Variante im BDY-Format |  |
| UFOGRAPH/ICONS.BDY | BDY | 320×200 → 1280×800 | 0,2,3,7,8,9,10,11,12 | Vollbild-Grafik (Kampf), Variante im BDY-Format |  |
| UFOGRAPH/MAN_0F0.BDY | BDY | 320×200 → 1280×800 | 2,6,7,10,11,12,13,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0F1.BDY | BDY | 320×200 → 1280×800 | 2,4,6,7,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0F2.BDY | BDY | 320×200 → 1280×800 | 2,4,6,7,10,11,12,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0F3.BDY | BDY | 320×200 → 1280×800 | 2,4,6,7,10,11,12,13,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0M0.BDY | BDY | 320×200 → 1280×800 | 2,6,7,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0M1.BDY | BDY | 320×200 → 1280×800 | 2,4,6,7,10,11,12,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0M2.BDY | BDY | 320×200 → 1280×800 | 2,4,6,7,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0M3.BDY | BDY | 320×200 → 1280×800 | 2,6,7,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_1F0.BDY | BDY | 320×200 → 1280×800 | 2,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_PLASTIC_AQUA_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1F1.BDY | BDY | 320×200 → 1280×800 | 2,4,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_PLASTIC_AQUA_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1F2.BDY | BDY | 320×200 → 1280×800 | 2,4,6,7,8,9,10,11,12,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_PLASTIC_AQUA_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1F3.BDY | BDY | 320×200 → 1280×800 | 2,4,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_PLASTIC_AQUA_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1M0.BDY | BDY | 320×200 → 1280×800 | 1,2,4,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_PLASTIC_AQUA_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1M1.BDY | BDY | 320×200 → 1280×800 | 1,2,4,7,8,10,12,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_PLASTIC_AQUA_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1M2.BDY | BDY | 320×200 → 1280×800 | 1,2,4,7,8,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_PLASTIC_AQUA_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1M3.BDY | BDY | 320×200 → 1280×800 | 1,2,4,7,8,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_PLASTIC_AQUA_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_2F0.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_2F1.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_2F2.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,12,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_2F3.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_2M0.BDY | BDY | 320×200 → 1280×800 | 1,2,3,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_2M1.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,12,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_2M2.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_2M3.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_3F0.BDY | BDY | 320×200 → 1280×800 | 1,2,3,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_MAGNETIC_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_3F1.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_MAGNETIC_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_3F2.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,12,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_MAGNETIC_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_3F3.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_MAGNETIC_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_3M0.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_MAGNETIC_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_3M1.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,12,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_MAGNETIC_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_3M2.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_MAGNETIC_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_3M3.BDY | BDY | 320×200 → 1280×800 | 1,2,3,4,7,8,9,10,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format | armors/STR_MAGNETIC_ION_ARMOR_UC: spriteInv |
| UFOGRAPH/MEDIBORD.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | Vollbild-Grafik (Kampf), Variante im BDY-Format |  |
| UFOGRAPH/SCANBORD.BDY | BDY | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,11,12 | Vollbild-Grafik (Kampf), Variante im BDY-Format |  |
| UFOGRAPH/TAC00.SCR | SCR | 320×200 → 1280×800 | 6,14,15 | Kampf-Oberfläche (Bedienleiste) |  |
| UFOGRAPH/TAC01.BDY | BDY | 320×200 → 1280×800 | 0,2,3,9 | Vollbild-Grafik (Kampf), Variante im BDY-Format |  |
| UFOGRAPH/UNIBORD.PCK | SPK | 320×200 → 1280×800 | 0,3,4 | Fensterrahmen: Einheiteninfo |  |

## Geländesätze (TERRAIN)

| Satz | Kacheln 32×40 (belegt) | MCD-Einträge | Farbgruppen | Verwendet von |
|---|---:|---:|---|---|
| TERRAIN/ASUNK | 50 (50) | 50 | 2,3,4,6,7,8,10 | terrains/GAL: mapDataSets |
| TERRAIN/ATLANTIS | 72 (72) | 77 | 2,3,8,10,12,13 | terrains/ATLAN: mapDataSets |
| TERRAIN/BLANKS | 2 (2) | 2 | 0,4,13 | terrains/SEABED: mapDataSets; terrains/PIPES: mapDataSets; terrains/PLANE: mapDataSets; terrains/ATLAN: mapDataSets; terrains/MU: mapDataSets; terrains/GAL: mapDataSets; terrains/MSUNK: mapDataSets; terrains/VOLC: mapDataSets; terrains/CORAL: mapDataSets; terrains/ALART: mapDataSets; terrains/PORT: mapDataSets; terrains/ISLAND: mapDataSets; terrains/CARGO: mapDataSets; terrains/XBASES: mapDataSets; terrains/ENTRY: mapDataSets; terrains/GRUNGE: mapDataSets; terrains/LINERT: mapDataSets; terrains/LINERB: mapDataSets; terrains/A_BASE: mapDataSets; terrains/ALSHIP: mapDataSets; terrains/LEVEL: mapDataSets; terrains/CRYPT: mapDataSets |
| TERRAIN/CARGO1 | 105 (105) | 103 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/CARGO: mapDataSets |
| TERRAIN/CARGO2 | 71 (71) | 13 | 2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/CARGO: mapDataSets |
| TERRAIN/CARGO3 | 13 (13) | 6 | 2,3,4,6,7,12 | terrains/CARGO: mapDataSets |
| TERRAIN/CARGO4 | 40 (40) |  | 2,3,4,5,6,7,10,11,12,13,14,15 |  |
| TERRAIN/CARGO5 | 40 (40) |  | 2,3,4,5,6,7,10,11,12,13,14,15 |  |
| TERRAIN/CORAL | 34 (34) | 28 | 7,11,12,13,14,15 | terrains/CORAL: mapDataSets |
| TERRAIN/CRYPT1 | 45 (45) | 45 | 1,2,3,7,8,10,11,12,15 | terrains/CRYPT: mapDataSets |
| TERRAIN/CRYPT2 | 49 (49) | 49 | 1,2,3,7,8,10,11,12,14,15 | terrains/CRYPT: mapDataSets |
| TERRAIN/CRYPT3 | 47 (47) | 12 | 1,2,3,8,10,11,12,13,14,15 | terrains/CRYPT: mapDataSets |
| TERRAIN/CRYPT4 | 27 (27) | 7 | 1,2,3,4,7,8,10,15 | terrains/CRYPT: mapDataSets |
| TERRAIN/DEBRIS | 67 (67) | 50 | 0,1,2,3,4,5,7,8,10,11,12,13,14,15 | terrains/SEABED: mapDataSets |
| TERRAIN/DECKC | 18 (18) | 18 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/LINERT: mapDataSets; terrains/LINERB: mapDataSets |
| TERRAIN/ENTRY | 80 (80) | 12 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/ENTRY: mapDataSets |
| TERRAIN/GRUNGE1 | 50 (50) | 38 | 0,1,2,3,4,5,6,7,8,10,11,13 | terrains/GRUNGE: mapDataSets; terrains/ALSHIP: mapDataSets |
| TERRAIN/GRUNGE2 | 50 (50) | 8 | 0,2,3,4,5,6,7,8,10,11 | terrains/GRUNGE: mapDataSets; terrains/ALSHIP: mapDataSets |
| TERRAIN/GRUNGE3 | 50 (50) | 10 | 0,1,3,4,5,6,7,8,10,11 | terrains/GRUNGE: mapDataSets; terrains/ALSHIP: mapDataSets |
| TERRAIN/GRUNGE4 | 98 (98) | 37 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14 | terrains/GRUNGE: mapDataSets; terrains/ALSHIP: mapDataSets |
| TERRAIN/GRUNGE5 | 40 (40) | 4 | 0,1,2,3,4,5,6,7,8,10,13,14 | terrains/GRUNGE: mapDataSets |
| TERRAIN/HAMMER | 55 (55) | 44 | 2,3,10,12,13,14 |  |
| TERRAIN/ISLAND1 | 50 (50) | 50 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/ISLAND: mapDataSets |
| TERRAIN/ISLAND2 | 50 (50) | 50 | 1,2,3,4,5,6,7,8,10,11,12,13 | terrains/ISLAND: mapDataSets |
| TERRAIN/ISLAND3 | 39 (39) | 35 | 1,2,3,4,5,6,7,8,10,11,12,13,15 | terrains/ISLAND: mapDataSets |
| TERRAIN/ISLAND4 |  () | 67 | – |  |
| TERRAIN/LEVIATH | 50 (50) | 47 | 2,3,6,7,12,13,14 |  |
| TERRAIN/LINERA | 118 (118) | 99 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/LINERT: mapDataSets; terrains/LINERB: mapDataSets |
| TERRAIN/LINERB | 116 (116) | 24 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/LINERT: mapDataSets; terrains/LINERB: mapDataSets |
| TERRAIN/LINERC | 40 (40) | 40 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/LINERT: mapDataSets; terrains/LINERB: mapDataSets |
| TERRAIN/LINERD | 42 (42) | 23 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/LINERT: mapDataSets; terrains/LINERB: mapDataSets |
| TERRAIN/MSUNK1 | 50 (50) | 50 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/MSUNK: mapDataSets |
| TERRAIN/MSUNK2 | 29 (29) | 29 | 1,2,3,4,5,7,10,11,12,13,14,15 | terrains/MSUNK: mapDataSets |
| TERRAIN/MU | 80 (80) | 80 | 1,2,3,4,5,7,8,10,12 | terrains/MU: mapDataSets |
| TERRAIN/ORGANIC | 139 (139) |  | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 |  |
| TERRAIN/ORGANIC1 | 139 (139) | 96 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/ENTRY: mapDataSets; terrains/A_BASE: mapDataSets; terrains/LEVEL: mapDataSets |
| TERRAIN/ORGANIC2 | 64 (64) | 10 | 0,1,2,3,4,5,7,8,10,11,12,13,14,15 | terrains/A_BASE: mapDataSets; terrains/LEVEL: mapDataSets |
| TERRAIN/ORGANIC3 | 43 (43) | 4 | 1,4,5,8,10,11,12,13,14,15 | terrains/A_BASE: mapDataSets; terrains/LEVEL: mapDataSets |
| TERRAIN/PIPES | 96 (96) | 55 | 1,2,3,4,5,6,7,8,10,11,12,13,14 | terrains/PIPES: mapDataSets |
| TERRAIN/PLANE | 80 (80) | 79 | 0,2,3,4,5,6,7,8,10,11,12,14,15 | terrains/PLANE: mapDataSets |
| TERRAIN/PORT01 | 95 (95) | 95 | 1,2,3,4,5,7,8,10,11,12,13,14,15 | terrains/PORT: mapDataSets |
| TERRAIN/PORT02 | 93 (93) | 78 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/PORT: mapDataSets |
| TERRAIN/PORT1 | 95 (95) | 95 | 1,2,3,4,5,7,8,10,11,12,13,14,15 |  |
| TERRAIN/PSYNOM | 16 (16) | 2 | 1,2,3,4,5,6,7,8,10,12,13,14 | terrains/A_BASE: mapDataSets; terrains/LEVEL: mapDataSets |
| TERRAIN/PYRAMID | 47 (47) | 24 | 0,1,2,3,4,5,6,7,8,10,12,13,14,15 | terrains/ALART: mapDataSets |
| TERRAIN/ROCKS | 17 (17) | 15 | 2,4,7,11,12,13 | terrains/SEABED: mapDataSets; terrains/GAL: mapDataSets; terrains/CORAL: mapDataSets; terrains/ALART: mapDataSets |
| TERRAIN/SAND | 22 (22) | 20 | 1,2,10 | terrains/SEABED: mapDataSets; terrains/PIPES: mapDataSets; terrains/PLANE: mapDataSets; terrains/ATLAN: mapDataSets; terrains/MU: mapDataSets; terrains/GAL: mapDataSets; terrains/MSUNK: mapDataSets; terrains/VOLC: mapDataSets; terrains/CORAL: mapDataSets; terrains/ALART: mapDataSets; terrains/ENTRY: mapDataSets |
| TERRAIN/SEA | 144 (144) | 18 | 6,7,15 | terrains/PORT: mapDataSets; terrains/ISLAND: mapDataSets; terrains/CARGO: mapDataSets; terrains/LINERB: mapDataSets |
| TERRAIN/TRITON | 70 (70) | 46 | 0,1,2,3,4,5,6,7,8,12,13,14 |  |
| TERRAIN/UEXT1 | 32 (32) | 4 | 0,1,5,6,8,10,11,13 |  |
| TERRAIN/UEXT2 | 66 (66) | 9 | 1,2,3,4,6,7,8,10,11,12,13 | terrains/ENTRY: mapDataSets |
| TERRAIN/UEXT3 | 25 (25) | 25 | 1,2,3,4,6,7,8,10,11,12,13 | terrains/ENTRY: mapDataSets |
| TERRAIN/UFOBITS | 12 (12) | 5 | 7,15 | terrains/SEABED: mapDataSets; terrains/PIPES: mapDataSets; terrains/PLANE: mapDataSets; terrains/ATLAN: mapDataSets; terrains/MU: mapDataSets; terrains/GAL: mapDataSets; terrains/MSUNK: mapDataSets; terrains/VOLC: mapDataSets; terrains/ALART: mapDataSets; terrains/GRUNGE: mapDataSets; terrains/ALSHIP: mapDataSets; terrains/CRYPT: mapDataSets |
| TERRAIN/UINT | 14 (14) |  | 0,1,2 |  |
| TERRAIN/UINT1 | 46 (46) | 41 | 0,1,2,3,5,6,10,11,14 |  |
| TERRAIN/UINT2 | 44 (44) | 11 | 1,2,3,4,5,6,8,10,11,12,15 |  |
| TERRAIN/UINT3 | 33 (33) | 14 | 1,2,3,4,5,6,7,10,11 |  |
| TERRAIN/URBITS | 25 (25) | 25 | 0,1,2,3,4,5,6,8,10,11,15 |  |
| TERRAIN/VOLC | 142 (142) | 80 | 2,4,7,9,10,11 | terrains/VOLC: mapDataSets |
| TERRAIN/WEEDS | 5 (5) | 10 | 1,5,7,10,11,14 | terrains/SEABED: mapDataSets; terrains/CORAL: mapDataSets; terrains/ALART: mapDataSets |
| TERRAIN/XBASE1 |  () | 97 | – |  |
| TERRAIN/XBASE2 |  () | 62 | – |  |
| TERRAIN/XBASES01 | 124 (124) | 87 | 1,2,3,4,5,6,7,8,10,11,12,15 | terrains/XBASES: mapDataSets |
| TERRAIN/XBASES02 | 89 (89) | 42 | 1,2,3,4,5,6,8,10,11,12 | terrains/XBASES: mapDataSets |
| TERRAIN/XBASES03 | 121 (121) | 37 | 1,2,3,4,5,6,8,10,11,12,13,15 | terrains/XBASES: mapDataSets |
| TERRAIN/XBASES04 | 120 (120) | 43 | 1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/XBASES: mapDataSets |
| TERRAIN/XBASES05 | 74 (74) | 42 | 1,2,3,4,5,6,7,8,10,11,12 | terrains/XBASES: mapDataSets |
| TERRAIN/XBITS | 46 (46) | 39 | 0,1,2,3,4,5,6,7,8,10,11,12,13,14,15 | terrains/CARGO: mapDataSets; terrains/XBASES: mapDataSets |

## Kartenblöcke (MAPS) und Wegpunkte (ROUTES)

| Block | Größe x×y×z | Wegpunkte | Verwendet von |
|---|---|---:|---|
| ALART00 | 10×10×4 | 1 | terrains/ALART: mapBlocks |
| ALART01 | 10×10×4 | 3 | terrains/ALART: mapBlocks |
| ALART02 | 10×10×4 | 3 | terrains/ALART: mapBlocks |
| ALART03 | 10×10×4 | 4 | terrains/ALART: mapBlocks |
| ALART04 | 10×10×4 | 4 | terrains/ALART: mapBlocks |
| ALART05 | 10×10×4 | 3 | terrains/ALART: mapBlocks |
| ALART06 | 10×10×4 | 4 | terrains/ALART: mapBlocks |
| ALART07 | 10×10×4 | 5 | terrains/ALART: mapBlocks |
| ALART08 | 10×10×4 | 4 | terrains/ALART: mapBlocks |
| ALART09 | 20×20×4 | 7 | terrains/ALART: mapBlocks |
| ALART10 | 20×20×4 | 7 | terrains/ALART: mapBlocks |
| ALART11 | 20×20×4 | 13 | terrains/ALART: mapBlocks |
| ALSHIP00 | 10×10×2 | 1 | terrains/ALSHIP: mapBlocks |
| ALSHIP01 | 10×10×2 | 13 | terrains/ALSHIP: mapBlocks |
| ALSHIP02 | 10×10×2 | 18 | terrains/ALSHIP: mapBlocks |
| ALSHIP03 | 10×10×2 | 8 | terrains/ALSHIP: mapBlocks |
| ALSHIP04 | 10×10×2 | 9 | terrains/ALSHIP: mapBlocks |
| ALSHIP05 | 10×10×2 | 6 | terrains/ALSHIP: mapBlocks |
| ALSHIP06 | 10×10×2 | 8 | terrains/ALSHIP: mapBlocks |
| ALSHIP07 | 10×10×2 | 13 | terrains/ALSHIP: mapBlocks |
| ALSHIP08 | 10×10×2 | 8 | terrains/ALSHIP: mapBlocks |
| ALSHIP09 | 10×10×2 | 13 | terrains/ALSHIP: mapBlocks |
| ALSHIP10 | 10×10×2 | 5 | terrains/ALSHIP: mapBlocks |
| ALSHIP11 | 20×20×2 | 40 | terrains/ALSHIP: mapBlocks |
| ALSHIP12 | 20×20×2 | 22 | terrains/ALSHIP: mapBlocks |
| ALSHIP13 | 20×20×2 | 30 | terrains/ALSHIP: mapBlocks |
| ALSHIP14 | 20×20×2 | 23 | terrains/ALSHIP: mapBlocks |
| ALSHIP15 | 20×20×2 | 26 | terrains/ALSHIP: mapBlocks |
| ALSHIP16 | 20×20×2 | 15 | terrains/ALSHIP: mapBlocks |
| ALSHIP17 | 20×20×2 | 22 | terrains/ALSHIP: mapBlocks |
| ATLAN00 | 10×10×3 | 1 | terrains/ATLAN: mapBlocks |
| ATLAN01 | 10×10×3 | 1 | terrains/ATLAN: mapBlocks |
| ATLAN02 | 10×10×3 | 4 | terrains/ATLAN: mapBlocks |
| ATLAN03 | 10×10×3 | 5 | terrains/ATLAN: mapBlocks |
| ATLAN04 | 10×10×3 | 4 | terrains/ATLAN: mapBlocks |
| ATLAN05 | 10×10×3 | 2 | terrains/ATLAN: mapBlocks |
| ATLAN06 | 10×10×3 | 2 | terrains/ATLAN: mapBlocks |
| ATLAN07 | 10×10×3 | 5 | terrains/ATLAN: mapBlocks |
| ATLAN08 | 10×10×3 | 2 | terrains/ATLAN: mapBlocks |
| ATLAN09 | 10×10×3 | 5 | terrains/ATLAN: mapBlocks |
| ATLAN10 | 20×20×3 | 14 | terrains/ATLAN: mapBlocks |
| ATLAN11 | 20×20×3 | 17 | terrains/ATLAN: mapBlocks |
| ATLAN12 | 20×20×3 | 20 |  |
| A_BASE00 | 10×10×4 | 23 | terrains/A_BASE: mapBlocks |
| A_BASE01 | 10×10×4 | 12 |  |
| A_BASE02 | 10×10×4 | 6 |  |
| A_BASE03 | 10×10×4 | 11 |  |
| A_BASE04 | 10×10×4 | 12 |  |
| A_BASE05 | 10×10×4 | 13 |  |
| A_BASE06 | 10×10×4 | 9 |  |
| A_BASE07 | 10×10×4 | 9 | terrains/A_BASE: mapBlocks |
| A_BASE08 | 10×10×4 | 8 | terrains/A_BASE: mapBlocks |
| A_BASE09 | 10×10×4 | 7 | terrains/A_BASE: mapBlocks |
| A_BASE10 | 10×10×4 | 6 |  |
| A_BASE11 | 20×20×4 | 19 | terrains/A_BASE: mapBlocks |
| A_BASE12 | 20×20×4 | 15 | terrains/A_BASE: mapBlocks |
| A_BASE13 | 20×20×4 | 17 | terrains/A_BASE: mapBlocks |
| A_BASE14 | 20×20×4 | 16 | terrains/A_BASE: mapBlocks |
| A_BASE15 | 20×20×4 | 13 | terrains/A_BASE: mapBlocks |
| A_BASE18 | – | 9 |  |
| A_BASE20 | – | 9 |  |
| CARGO00 | 30×70×4 | 90 | terrains/CARGO: mapBlocks |
| CARGO01 | 30×50×4 | 90 | terrains/CARGO: mapBlocks |
| CARGO02 | 30×20×4 | 31 | terrains/CARGO: mapBlocks |
| CORAL00 | 10×10×1 | 1 | terrains/CORAL: mapBlocks |
| CORAL01 | 10×10×3 | 1 | terrains/CORAL: mapBlocks |
| CORAL02 | 10×10×4 | 1 | terrains/CORAL: mapBlocks |
| CORAL03 | 10×10×3 | 1 | terrains/CORAL: mapBlocks |
| CORAL04 | 10×10×3 | 6 | terrains/CORAL: mapBlocks |
| CORAL05 | 10×10×3 | 2 | terrains/CORAL: mapBlocks |
| CORAL06 | 10×10×2 | 3 | terrains/CORAL: mapBlocks |
| CORAL07 | 10×10×1 | 1 | terrains/CORAL: mapBlocks |
| CORAL08 | 10×10×3 | 2 | terrains/CORAL: mapBlocks |
| CORAL09 | 20×20×1 | 6 | terrains/CORAL: mapBlocks |
| CORAL10 | 20×20×2 | 2 | terrains/CORAL: mapBlocks |
| CORAL11 | 20×20×4 | 5 | terrains/CORAL: mapBlocks |
| CORAL12 | 20×20×3 | 1 |  |
| CRYPT01 | 20×20×2 | 45 | terrains/CRYPT: mapBlocks |
| CRYPT02 | 20×20×2 | 14 | terrains/CRYPT: mapBlocks |
| CRYPT03 | 20×20×2 | 15 | terrains/CRYPT: mapBlocks |
| CRYPT04 | 20×20×2 | 30 | terrains/CRYPT: mapBlocks |
| CRYPT05 | 20×20×2 | 16 | terrains/CRYPT: mapBlocks |
| CRYPT06 | 20×20×2 | 16 | terrains/CRYPT: mapBlocks |
| CRYPT07 | 20×20×2 | 18 | terrains/CRYPT: mapBlocks |
| CRYPT08 | 20×20×2 | 19 | terrains/CRYPT: mapBlocks |
| CRYPT09 | 20×20×2 | 13 | terrains/CRYPT: mapBlocks |
| ENTRY00 | 20×20×3 | 5 | terrains/ENTRY: mapBlocks |
| ENTRY01 | 20×20×4 | 10 | terrains/ENTRY: mapBlocks |
| ENTRY02 | 20×20×3 | 6 | terrains/ENTRY: mapBlocks |
| ENTRY03 | 20×20×3 | 5 | terrains/ENTRY: mapBlocks |
| ENTRY04 | 20×20×3 | 16 | terrains/ENTRY: mapBlocks |
| ENTRY05 | 20×20×3 | 5 | terrains/ENTRY: mapBlocks |
| ENTRY06 | 20×20×3 | 3 | terrains/ENTRY: mapBlocks |
| ENTRY07 | 20×20×3 | 5 | terrains/ENTRY: mapBlocks |
| ENTRY08 | 20×20×3 | 5 | terrains/ENTRY: mapBlocks |
| GAL01 | 20×20×4 | 13 | terrains/GAL: mapBlocks |
| GAL02 | 20×20×4 | 12 | terrains/GAL: mapBlocks |
| GAL03 | 20×20×4 | 13 | terrains/GAL: mapBlocks |
| GAL04 | 10×10×4 | 2 | terrains/GAL: mapBlocks |
| GAL05 | 10×10×4 | 7 | terrains/GAL: mapBlocks |
| GAL06 | 10×10×4 | 5 | terrains/GAL: mapBlocks |
| GAL07 | 10×10×4 | 4 | terrains/GAL: mapBlocks |
| GAL08 | 10×10×4 | 1 | terrains/GAL: mapBlocks |
| GAL09 | 10×10×4 | 2 | terrains/GAL: mapBlocks |
| GAL10 | 10×10×4 | 1 | terrains/GAL: mapBlocks |
| GAL11 | 10×10×4 | 4 | terrains/GAL: mapBlocks |
| GAL12 | 10×10×4 | 2 | terrains/GAL: mapBlocks |
| GAL13 | 10×10×4 | 1 | terrains/GAL: mapBlocks |
| GAL14 | 10×10×4 | 1 | terrains/GAL: mapBlocks |
| GAL15 | 10×10×4 | 1 | terrains/GAL: mapBlocks |
| GAL16 | 10×10×4 | 1 | terrains/GAL: mapBlocks |
| GRUNGE00 | 10×10×4 | 2 | terrains/GRUNGE: mapBlocks |
| GRUNGE01 | 10×10×4 | 3 | terrains/GRUNGE: mapBlocks |
| GRUNGE02 | 10×10×4 | 2 | terrains/GRUNGE: mapBlocks |
| GRUNGE03 | 10×10×4 | 2 | terrains/GRUNGE: mapBlocks |
| GRUNGE04 | 10×10×4 | 11 | terrains/GRUNGE: mapBlocks |
| GRUNGE05 | 10×10×4 | 6 | terrains/GRUNGE: mapBlocks |
| GRUNGE06 | 10×10×4 | 10 | terrains/GRUNGE: mapBlocks |
| GRUNGE07 | 10×10×4 | 6 | terrains/GRUNGE: mapBlocks |
| GRUNGE08 | 10×10×4 | 7 | terrains/GRUNGE: mapBlocks |
| GRUNGE09 | 10×10×4 | 7 | terrains/GRUNGE: mapBlocks |
| GRUNGE10 | 10×10×4 | 7 | terrains/GRUNGE: mapBlocks |
| GRUNGE11 | 20×20×4 | 20 | terrains/GRUNGE: mapBlocks |
| GRUNGE12 | 20×20×4 | 32 | terrains/GRUNGE: mapBlocks |
| GRUNGE13 | 20×20×4 | 28 | terrains/GRUNGE: mapBlocks |
| GRUNGE14 | 20×20×4 | 29 | terrains/GRUNGE: mapBlocks |
| GRUNGE15 | 20×20×4 | 50 | terrains/GRUNGE: mapBlocks |
| GRUNGE16 | 20×20×4 | 28 |  |
| HAMMER | 10×10×2 | 2 |  |
| HAMMER1 | 10×10×1 | 1 |  |
| HAMMER2 | 20×10×2 | 4 |  |
| ISLAND00 | 10×10×4 | 2 | terrains/ISLAND: mapBlocks |
| ISLAND01 | 10×10×4 | 1 | terrains/ISLAND: mapBlocks |
| ISLAND02 | 20×20×4 | 27 | terrains/ISLAND: mapBlocks |
| ISLAND03 | 20×20×4 | 26 | terrains/ISLAND: mapBlocks |
| ISLAND04 | 20×20×4 | 16 | terrains/ISLAND: mapBlocks |
| ISLAND05 | 20×20×4 | 13 | terrains/ISLAND: mapBlocks |
| ISLAND06 | 20×20×4 | 12 | terrains/ISLAND: mapBlocks |
| ISLAND07 | 10×10×4 | 4 | terrains/ISLAND: mapBlocks |
| ISLAND08 | 10×10×4 | 3 | terrains/ISLAND: mapBlocks |
| ISLAND09 | 10×10×4 | 8 | terrains/ISLAND: mapBlocks |
| ISLAND10 | 10×10×4 | 4 | terrains/ISLAND: mapBlocks |
| ISLAND11 | 10×10×4 | 5 | terrains/ISLAND: mapBlocks |
| ISLAND12 | 10×10×4 | 4 | terrains/ISLAND: mapBlocks |
| ISLAND13 | 10×10×4 | 5 | terrains/ISLAND: mapBlocks |
| ISLAND14 | 10×10×4 | 4 | terrains/ISLAND: mapBlocks |
| LEVEL01 | 20×20×2 | 29 | terrains/LEVEL: mapBlocks |
| LEVEL02 | 20×20×2 | 9 | terrains/LEVEL: mapBlocks |
| LEVEL03 | 20×20×2 | 8 | terrains/LEVEL: mapBlocks |
| LEVEL04 | 20×20×2 | 15 | terrains/LEVEL: mapBlocks |
| LEVEL05 | 20×20×2 | 22 | terrains/LEVEL: mapBlocks |
| LEVEL06 | 20×20×2 | 14 | terrains/LEVEL: mapBlocks |
| LEVEL07 | 20×20×2 | 13 | terrains/LEVEL: mapBlocks |
| LEVEL08 | 20×20×2 | 7 | terrains/LEVEL: mapBlocks |
| LEVEL09 | 20×20×2 | 16 | terrains/LEVEL: mapBlocks |
| LEVIATH | 10×20×2 | 3 |  |
| LINERB02 | 30×50×4 | 98 | terrains/LINERB: mapBlocks |
| LINERB03 | 30×20×4 | 51 | terrains/LINERB: mapBlocks |
| LINERT00 | 30×20×4 | 25 | terrains/LINERT: mapBlocks |
| LINERT01 | 30×50×4 | 85 | terrains/LINERT: mapBlocks |
| LINERT02 | 30×50×4 | 85 | terrains/LINERT: mapBlocks |
| MSUNK00 | 10×10×4 | 3 | terrains/MSUNK: mapBlocks |
| MSUNK01 | 10×10×4 | 1 | terrains/MSUNK: mapBlocks |
| MSUNK02 | 20×20×4 | 12 | terrains/MSUNK: mapBlocks |
| MSUNK03 | 20×20×4 | 13 | terrains/MSUNK: mapBlocks |
| MSUNK04 | 20×20×4 | 18 | terrains/MSUNK: mapBlocks |
| MSUNK05 | 20×20×4 | 11 | terrains/MSUNK: mapBlocks |
| MSUNK06 | 20×20×4 | 14 | terrains/MSUNK: mapBlocks |
| MSUNK07 | 10×10×4 | 6 | terrains/MSUNK: mapBlocks |
| MSUNK08 | 10×10×4 | 6 | terrains/MSUNK: mapBlocks |
| MSUNK09 | 10×10×4 | 10 | terrains/MSUNK: mapBlocks |
| MSUNK10 | 10×10×4 | 11 | terrains/MSUNK: mapBlocks |
| MSUNK11 | 10×10×4 | 7 | terrains/MSUNK: mapBlocks |
| MSUNK12 | 10×10×4 | 6 | terrains/MSUNK: mapBlocks |
| MSUNK13 | 10×10×4 | 10 | terrains/MSUNK: mapBlocks |
| MU00 | 10×10×4 | 1 | terrains/MU: mapBlocks |
| MU01 | 10×10×4 | 1 | terrains/MU: mapBlocks |
| MU02 | 10×10×4 | 1 | terrains/MU: mapBlocks |
| MU03 | 10×10×4 | 7 | terrains/MU: mapBlocks |
| MU04 | 10×10×4 | 9 | terrains/MU: mapBlocks |
| MU05 | 10×10×4 | 5 | terrains/MU: mapBlocks |
| MU06 | 10×10×4 | 2 | terrains/MU: mapBlocks |
| MU07 | 10×10×4 | 4 | terrains/MU: mapBlocks |
| MU08 | 10×10×4 | 4 | terrains/MU: mapBlocks |
| MU09 | 20×20×4 | 14 | terrains/MU: mapBlocks |
| MU10 | 20×20×4 | 16 | terrains/MU: mapBlocks |
| MU11 | 20×20×4 | 15 | terrains/MU: mapBlocks |
| MU12 | 20×20×4 | 1 |  |
| MU13 | 20×20×4 |  |  |
| MU14 | 20×20×4 |  |  |
| PIPES00 | 10×10×1 | 1 | terrains/PIPES: mapBlocks |
| PIPES01 | 10×10×4 | 1 | terrains/PIPES: mapBlocks |
| PIPES02 | 10×10×3 | 8 | terrains/PIPES: mapBlocks |
| PIPES03 | 10×10×4 | 5 | terrains/PIPES: mapBlocks |
| PIPES04 | 10×10×4 | 3 | terrains/PIPES: mapBlocks |
| PIPES05 | 10×10×4 | 1 | terrains/PIPES: mapBlocks |
| PIPES06 | 10×10×4 | 1 | terrains/PIPES: mapBlocks |
| PIPES07 | 10×10×4 | 5 | terrains/PIPES: mapBlocks |
| PIPES08 | 20×20×3 | 18 | terrains/PIPES: mapBlocks |
| PIPES09 | 20×20×3 | 25 | terrains/PIPES: mapBlocks |
| PIPES10 | 20×20×3 | 3 | terrains/PIPES: mapBlocks |
| PIPES11 | 20×20×3 | 5 | terrains/PIPES: mapBlocks |
| PLANE | 10×20×3 |  |  |
| PLANE00 | 10×10×1 | 1 | terrains/PLANE: mapBlocks |
| PLANE01 | 10×10×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE02 | 10×10×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE03 | 10×10×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE04 | 10×10×4 | 9 | terrains/PLANE: mapBlocks |
| PLANE05 | 10×10×4 | 8 | terrains/PLANE: mapBlocks |
| PLANE06 | 10×10×4 | 6 | terrains/PLANE: mapBlocks |
| PLANE07 | 10×10×4 | 4 | terrains/PLANE: mapBlocks |
| PLANE08 | 10×10×4 | 3 | terrains/PLANE: mapBlocks |
| PLANE09 | 10×10×4 | 3 | terrains/PLANE: mapBlocks |
| PLANE10 | 10×10×4 | 5 | terrains/PLANE: mapBlocks |
| PLANE11 | 10×10×4 | 6 | terrains/PLANE: mapBlocks |
| PLANE12 | 10×10×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE13 | 20×20×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE14 | 20×20×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE15 | 20×20×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE16 | 20×20×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE17 | 10×10×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE18 | 10×10×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE19 | 10×10×4 | 1 | terrains/PLANE: mapBlocks |
| PLANE20 | 10×10×4 | 1 | terrains/PLANE: mapBlocks |
| PORT00 | 10×10×4 | 2 | terrains/PORT: mapBlocks |
| PORT01 | 10×10×4 | 3 | terrains/PORT: mapBlocks |
| PORT02 | 10×10×3 | 4 | terrains/PORT: mapBlocks |
| PORT03 | 10×10×3 | 7 | terrains/PORT: mapBlocks |
| PORT04 | 10×10×3 | 7 | terrains/PORT: mapBlocks |
| PORT05 | 10×10×3 | 8 | terrains/PORT: mapBlocks |
| PORT06 | 10×10×3 | 11 | terrains/PORT: mapBlocks |
| PORT07 | 10×10×3 | 6 | terrains/PORT: mapBlocks |
| PORT08 | 10×10×3 | 5 | terrains/PORT: mapBlocks |
| PORT09 | 10×10×3 | 9 | terrains/PORT: mapBlocks |
| PORT10 | 10×10×3 | 7 | terrains/PORT: mapBlocks |
| PORT11 | 10×10×3 | 12 | terrains/PORT: mapBlocks |
| PORT12 | 10×10×3 | 8 | terrains/PORT: mapBlocks |
| PORT13 | 10×10×3 | 6 | terrains/PORT: mapBlocks |
| PORT14 | 10×10×3 | 5 | terrains/PORT: mapBlocks |
| PORT15 | 10×10×4 | 10 | terrains/PORT: mapBlocks |
| PORT16 | 10×10×3 | 4 | terrains/PORT: mapBlocks |
| PORT17 | 20×20×4 | 23 | terrains/PORT: mapBlocks |
| PORT18 | 20×20×4 | 23 | terrains/PORT: mapBlocks |
| PORT19 | 20×20×4 | 19 | terrains/PORT: mapBlocks |
| PORT20 | 20×20×4 | 31 | terrains/PORT: mapBlocks |
| SEABED00 | 10×10×1 | 1 | terrains/SEABED: mapBlocks |
| SEABED01 | 10×10×1 | 1 | terrains/SEABED: mapBlocks |
| SEABED02 | 10×10×2 | 4 | terrains/SEABED: mapBlocks |
| SEABED03 | 10×10×1 | 1 | terrains/SEABED: mapBlocks |
| SEABED04 | 10×10×2 | 4 | terrains/SEABED: mapBlocks |
| SEABED05 | 10×10×2 | 4 | terrains/SEABED: mapBlocks |
| SEABED06 | 10×10×1 | 1 | terrains/SEABED: mapBlocks |
| SEABED07 | 10×10×1 | 2 | terrains/SEABED: mapBlocks |
| SEABED08 | 10×10×1 | 1 | terrains/SEABED: mapBlocks |
| SEABED09 | 10×10×1 | 2 | terrains/SEABED: mapBlocks |
| SEABED10 | 20×20×1 | 2 | terrains/SEABED: mapBlocks |
| SEABED11 | 20×20×2 | 1 | terrains/SEABED: mapBlocks |
| SEABED12 | 20×20×3 | 4 | terrains/SEABED: mapBlocks |
| TRITON | 10×20×2 | 3 |  |
| UFO01 | 10×10×2 | 10 |  |
| UFO02 | 10×10×2 | 9 |  |
| UFO03 | 20×20×4 | 31 |  |
| UFO04 | 20×20×3 | 33 |  |
| UFO05 | 20×20×4 | 42 |  |
| UFO06 | 20×30×4 | 64 |  |
| UFO07 | 30×30×4 | 100 |  |
| UFO08 | 20×30×4 | 80 |  |
| VOLC00 | 10×10×1 | 1 | terrains/VOLC: mapBlocks |
| VOLC01 | 10×10×2 | 1 | terrains/VOLC: mapBlocks |
| VOLC02 | 10×10×2 | 1 | terrains/VOLC: mapBlocks |
| VOLC03 | 10×10×2 | 1 | terrains/VOLC: mapBlocks |
| VOLC04 | 10×10×2 | 1 | terrains/VOLC: mapBlocks |
| VOLC05 | 10×10×2 | 2 | terrains/VOLC: mapBlocks |
| VOLC06 | 10×10×2 | 1 | terrains/VOLC: mapBlocks |
| VOLC07 | 10×10×2 | 1 | terrains/VOLC: mapBlocks |
| VOLC08 | 10×10×2 | 3 | terrains/VOLC: mapBlocks |
| VOLC09 | 20×20×4 | 6 | terrains/VOLC: mapBlocks |
| VOLC10 | 20×20×4 | 3 | terrains/VOLC: mapBlocks |
| VOLC11 | 20×20×4 | 4 | terrains/VOLC: mapBlocks |
| XBASES00 | 10×10×4 | 14 | terrains/XBASES: mapBlocks |
| XBASES01 | 10×10×4 | 10 | terrains/XBASES: mapBlocks |
| XBASES02 | 10×10×4 | 12 | terrains/XBASES: mapBlocks |
| XBASES03 | 10×10×4 | 14 | terrains/XBASES: mapBlocks |
| XBASES04 | 10×10×4 | 10 | terrains/XBASES: mapBlocks |
| XBASES05 | 10×10×4 | 11 | terrains/XBASES: mapBlocks |
| XBASES06 | 10×10×4 | 13 | terrains/XBASES: mapBlocks |
| XBASES07 | 10×10×4 | 9 | terrains/XBASES: mapBlocks |
| XBASES08 | 10×10×4 | 10 | terrains/XBASES: mapBlocks |
| XBASES09 | 10×10×4 | 9 | terrains/XBASES: mapBlocks |
| XBASES10 | 10×10×4 | 8 | terrains/XBASES: mapBlocks |
| XBASES11 | 10×10×4 | 7 | terrains/XBASES: mapBlocks |
| XBASES12 | 10×10×4 | 9 | terrains/XBASES: mapBlocks |
| XBASES13 | 10×10×4 | 10 | terrains/XBASES: mapBlocks |
| XBASES14 | 10×10×4 | 11 | terrains/XBASES: mapBlocks |
| XBASES15 | 10×10×4 | 10 | terrains/XBASES: mapBlocks |
| XBASES16 | 10×10×4 | 5 | terrains/XBASES: mapBlocks |
| XBASES17 | 10×10×4 | 3 | terrains/XBASES: mapBlocks |
| XBASES18 | 10×10×4 | 4 | terrains/XBASES: mapBlocks |
| XBASES19 | 10×10×4 | 3 | terrains/XBASES: mapBlocks |
| XBASES20 | 10×10×4 | 1 | terrains/XBASES: mapBlocks |

## Soundeffekte

### SOUND/DRIVERS.CAT

Soundeffekte · 18 Einträge

| # | Format | Rate | Sekunden | Verwendung |
|---:|---|---:|---:|---|
| 0 | DOS-Rohdaten |  |  |  |
| 1 | DOS-Rohdaten |  |  |  |
| 2 | DOS-Rohdaten |  |  |  |
| 3 | DOS-Rohdaten |  |  |  |
| 4 | DOS-Rohdaten |  |  |  |
| 5 | DOS-Rohdaten |  |  |  |
| 6 | DOS-Rohdaten |  |  |  |
| 7 | DOS-Rohdaten |  |  |  |
| 8 | DOS-Rohdaten |  |  |  |
| 9 | DOS-Rohdaten |  |  |  |
| 10 | DOS-Rohdaten |  |  |  |
| 11 | DOS-Rohdaten |  |  |  |
| 12 | DOS-Rohdaten |  |  |  |
| 13 | DOS-Rohdaten |  |  |  |
| 14 | DOS-Rohdaten |  |  |  |
| 15 | DOS-Rohdaten |  |  |  |
| 16 | DOS-Rohdaten |  |  |  |
| 17 | DOS-Rohdaten |  |  |  |

### SOUND/SAMPLE.CAT

Soundeffekte · 123 Einträge

- Hinweis: Enthält den logischen Soundsatz GEO.CAT
- Hinweis: Enthält den logischen Soundsatz BATTLE.CAT

| # | Format | Rate | Sekunden | Verwendung |
|---:|---|---:|---:|---|
| 0 | DOS-Rohdaten |  |  |  |
| 1 | DOS-Rohdaten |  |  |  |
| 2 | DOS-Rohdaten |  |  |  |
| 3 | DOS-Rohdaten |  |  |  |
| 4 | DOS-Rohdaten |  |  |  |
| 5 | DOS-Rohdaten |  |  |  |
| 6 | DOS-Rohdaten |  |  |  |
| 7 | DOS-Rohdaten |  |  |  |
| 8 | DOS-Rohdaten |  |  |  |
| 9 | DOS-Rohdaten |  |  |  |
| 10 | DOS-Rohdaten |  |  |  |
| 11 | DOS-Rohdaten |  |  |  |
| 12 | DOS-Rohdaten |  |  |  |
| 13 | DOS-Rohdaten |  |  | facilities/STR_TORPEDO_DEFENSES: fireSound (GEO.CAT #12) |
| 14 | DOS-Rohdaten |  |  | craftWeapons/STR_AJAX: sound (GEO.CAT #6); facilities/STR_GAUSS_DEFENSES: fireSound (GEO.CAT #13) |
| 15 | DOS-Rohdaten |  |  | craftWeapons/STR_DUP_HEAD: sound (GEO.CAT #7); facilities/STR_SONIC_DEFENSES: fireSound (GEO.CAT #14) |
| 16 | DOS-Rohdaten |  |  |  |
| 17 | DOS-Rohdaten |  |  |  |
| 18 | DOS-Rohdaten |  |  | craftWeapons/STR_SONIC_OSCILLATOR_UC: sound (GEO.CAT #11) |
| 19 | DOS-Rohdaten |  |  |  |
| 20 | DOS-Rohdaten |  |  |  |
| 21 | DOS-Rohdaten |  |  |  |
| 22 | DOS-Rohdaten |  |  |  |
| 23 | DOS-Rohdaten |  |  |  |
| 24 | DOS-Rohdaten |  |  | facilities/STR_PWT_DEFENSES: fireSound (GEO.CAT #15) |
| 25 | DOS-Rohdaten |  |  |  |
| 26 | DOS-Rohdaten |  |  |  |
| 27 | DOS-Rohdaten |  |  |  |
| 28 | DOS-Rohdaten |  |  |  |
| 29 | DOS-Rohdaten |  |  |  |
| 30 | DOS-Rohdaten |  |  |  |
| 31 | DOS-Rohdaten |  |  |  |
| 32 | DOS-Rohdaten |  |  |  |
| 33 | DOS-Rohdaten |  |  | craftWeapons/STR_CRAFT_GAS_CANNON_UC: sound (GEO.CAT #8) |
| 34 | DOS-Rohdaten |  |  |  |
| 35 | DOS-Rohdaten |  |  |  |
| 36 | DOS-Rohdaten |  |  |  |
| 37 | DOS-Rohdaten |  |  | craftWeapons/STR_GAUSS_CANNON_UC: sound (GEO.CAT #10); facilities/STR_TORPEDO_DEFENSES: hitSound (GEO.CAT #10); facilities/STR_GAUSS_DEFENSES: hitSound (GEO.CAT #10); facilities/STR_SONIC_DEFENSES: hitSound (GEO.CAT #10); facilities/STR_PWT_DEFENSES: hitSound (GEO.CAT #10) |
| 38 | DOS-Rohdaten |  |  |  |
| 39 | DOS-Rohdaten |  |  |  |
| 40 | DOS-Rohdaten |  |  |  |
| 41 | DOS-Rohdaten |  |  |  |
| 42 | DOS-Rohdaten |  |  |  |
| 43 | DOS-Rohdaten |  |  |  |
| 44 | DOS-Rohdaten |  |  |  |
| 45 | DOS-Rohdaten |  |  |  |
| 46 | DOS-Rohdaten |  |  |  |
| 47 | DOS-Rohdaten |  |  |  |
| 48 | DOS-Rohdaten |  |  |  |
| 49 | DOS-Rohdaten |  |  |  |
| 50 | DOS-Rohdaten |  |  |  |
| 51 | DOS-Rohdaten |  |  |  |
| 52 | DOS-Rohdaten |  |  |  |
| 53 | DOS-Rohdaten |  |  |  |
| 54 | DOS-Rohdaten |  |  |  |
| 55 | DOS-Rohdaten |  |  |  |
| 56 | DOS-Rohdaten |  |  |  |
| 57 | DOS-Rohdaten |  |  |  |
| 58 | DOS-Rohdaten |  |  |  |
| 59 | DOS-Rohdaten |  |  |  |
| 60 | DOS-Rohdaten |  |  |  |
| 61 | DOS-Rohdaten |  |  |  |
| 62 | DOS-Rohdaten |  |  |  |
| 63 | DOS-Rohdaten |  |  |  |
| 64 | DOS-Rohdaten |  |  |  |
| 65 | DOS-Rohdaten |  |  |  |
| 66 | DOS-Rohdaten |  |  |  |
| 67 | DOS-Rohdaten |  |  |  |
| 68 | DOS-Rohdaten |  |  |  |
| 69 | DOS-Rohdaten |  |  |  |
| 70 | DOS-Rohdaten |  |  |  |
| 71 | DOS-Rohdaten |  |  |  |
| 72 | DOS-Rohdaten |  |  |  |
| 73 | DOS-Rohdaten |  |  |  |
| 74 | DOS-Rohdaten |  |  |  |
| 75 | DOS-Rohdaten |  |  |  |
| 76 | DOS-Rohdaten |  |  |  |
| 77 | DOS-Rohdaten |  |  |  |
| 78 | DOS-Rohdaten |  |  |  |
| 79 | DOS-Rohdaten |  |  |  |
| 80 | DOS-Rohdaten |  |  |  |
| 81 | DOS-Rohdaten |  |  |  |
| 82 | DOS-Rohdaten |  |  |  |
| 83 | DOS-Rohdaten |  |  |  |
| 84 | DOS-Rohdaten |  |  |  |
| 85 | DOS-Rohdaten |  |  |  |
| 86 | DOS-Rohdaten |  |  |  |
| 87 | DOS-Rohdaten |  |  |  |
| 88 | DOS-Rohdaten |  |  |  |
| 89 | DOS-Rohdaten |  |  |  |
| 90 | DOS-Rohdaten |  |  |  |
| 91 | DOS-Rohdaten |  |  |  |
| 92 | DOS-Rohdaten |  |  |  |
| 93 | DOS-Rohdaten |  |  |  |
| 94 | DOS-Rohdaten |  |  |  |
| 95 | DOS-Rohdaten |  |  |  |
| 96 | DOS-Rohdaten |  |  |  |
| 97 | DOS-Rohdaten |  |  | craftWeapons/STR_PWT_CANNON_UC: sound (GEO.CAT #9) |
| 98 | DOS-Rohdaten |  |  |  |
| 99 | DOS-Rohdaten |  |  |  |
| 100 | DOS-Rohdaten |  |  |  |
| 101 | DOS-Rohdaten |  |  |  |
| 102 | DOS-Rohdaten |  |  |  |
| 103 | DOS-Rohdaten |  |  |  |
| 104 | DOS-Rohdaten |  |  |  |
| 105 | DOS-Rohdaten |  |  |  |
| 106 | DOS-Rohdaten |  |  |  |
| 107 | DOS-Rohdaten |  |  |  |
| 108 | DOS-Rohdaten |  |  |  |
| 109 | DOS-Rohdaten |  |  |  |
| 110 | DOS-Rohdaten |  |  |  |
| 111 | DOS-Rohdaten |  |  |  |
| 112 | DOS-Rohdaten |  |  |  |
| 113 | DOS-Rohdaten |  |  |  |
| 114 | DOS-Rohdaten |  |  |  |
| 115 | DOS-Rohdaten |  |  |  |
| 116 | DOS-Rohdaten |  |  |  |
| 117 | DOS-Rohdaten |  |  |  |
| 118 | DOS-Rohdaten |  |  |  |
| 119 | DOS-Rohdaten |  |  |  |
| 120 | DOS-Rohdaten |  |  |  |
| 121 | DOS-Rohdaten |  |  |  |
| 122 | DOS-Rohdaten |  |  | items/STR_GC_HE_AMMO: hitSound (BATTLE.CAT #0); items/STR_GC_P_AMMO: hitSound (BATTLE.CAT #0); items/STR_HJC_HE_AMMO: hitSound (BATTLE.CAT #0); items/STR_HJC_P_AMMO: hitSound (BATTLE.CAT #0); items/STR_SMALL_TORPEDO: hitSound (BATTLE.CAT #0); items/STR_LARGE_TORPEDO: hitSound (BATTLE.CAT #0); items/STR_PHOSPHOROUS_TORPEDO: hitSound (BATTLE.CAT #0); items/STR_DISRUPTOR_AMMO: hitSound (BATTLE.CAT #0); items/STR_THERMAL_SHOK_BOMB: hitSound (BATTLE.CAT #0) |

DOS-Programme/Treiber (für ACOM ohne Bedeutung): SOUND/MUSIC.COM, SOUND/SNDEND.EXE, SOUND/SNDSTART.EXE, SOUND/SOUND.CFG, SOUND/SOUNDRV.COM, SOUND/VECTOR.COM

## Musik, Videos, Paletten, Schriften, Texte, Daten

| Datei | Kategorie | Format | Details | Zweck | Verwendung |
|---|---|---|---|---|---|
| ANIMS/GAMEOVER.IDX | video | IDX | 1216 Bytes | Index zur gleichnamigen VID-Datei |  |
| ANIMS/GAMEOVER.VID | video | VID | 320×85, 303 Frames, 5540847 Bytes | Video-Sequenz | cutscenes/loseGame: videos |
| ANIMS/INTRO.IDX | video | IDX | 4388 Bytes | Index zur gleichnamigen VID-Datei |  |
| ANIMS/INTRO.VID | video | VID | 320×85, 1096 Frames, 25809984 Bytes | Video-Sequenz | cutscenes/intro: videos |
| ANIMS/LOGO.IDX | video | IDX | 804 Bytes | Index zur gleichnamigen VID-Datei |  |
| ANIMS/LOGO.VID | video | VID | 320×200, 200 Frames, 1111000 Bytes | Video-Sequenz | cutscenes/intro: videos |
| ANIMS/OUTRO.IDX | video | IDX | 2272 Bytes | Index zur gleichnamigen VID-Datei |  |
| ANIMS/OUTRO.VID | video | VID | 320×85, 567 Frames, 11963775 Bytes | Video-Sequenz | cutscenes/winGame: videos |
| ANIMS/RISE.IDX | video | IDX | 1040 Bytes | Index zur gleichnamigen VID-Datei |  |
| ANIMS/RISE.VID | video | VID | 320×85, 259 Frames, 6040735 Bytes | Video-Sequenz | cutscenes/tleth: videos |
| GEODATA/BACKPALS.DAT | palette | DAT | 8 Einträge, 384 Bytes, Nur Struktur erfasst; Farbwerte werden nicht exportiert. | Hintergrund-Farbverläufe für Fenster |  |
| GEODATA/BIGLETS.DAT | font | DAT | 16×16, 173 Einträge, 44288 Bytes | Große Bitmap-Schrift des Originals (ACOM nutzt eigene Schriften) |  |
| GEODATA/COS.DAT | data | DAT | 5762 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/ENGLISH.DAT | text | DAT | 54427 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/ENGLISH2.DAT | text | DAT | 2828 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/FRENCH.DAT | text | DAT | 58673 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/FRENCH2.DAT | text | DAT | 3157 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/GERMAN.DAT | text | DAT | 59613 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/GERMAN2.DAT | text | DAT | 3128 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/LANG1.DAT | data | DAT | 12800 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/LANG2.DAT | data | DAT | 12800 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/LANG3.DAT | data | DAT | 12800 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/LOFTEMPS.DAT | data | DAT | 112 Einträge, 3584 Bytes | Kollisions-/Sichtlinien-Vorlagen der Geländekacheln (16×16 Bit je Ebene) |  |
| GEODATA/OBDATA.DAT | data | DAT | 4320 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/PALETTES.DAT | palette | DAT | 3 Einträge, 2322 Bytes, Nur Struktur erfasst; Farbwerte werden nicht exportiert. | Paletten (Geoscape, Basis, Grafiken, Enzyklopädie, Kampf) |  |
| GEODATA/SINE.DAT | data | DAT | 5762 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/SMALLSET.DAT | font | DAT | 8×9, 173 Einträge, 12456 Bytes | Kleine Bitmap-Schrift des Originals (ACOM nutzt eigene Schriften) |  |
| GEODATA/SPANISH.DAT | text | DAT | 58844 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/SPANISH2.DAT | text | DAT | 3500 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/WORLD.DAT | data | DAT | 14660 Bytes | Weltkarte als Polygone (Landmassen) – durch freie Geodaten ersetzen |  |
| MAPS/MAPS | unknown |  | 56265 Bytes | Nicht ausgewertet |  |
| MAPS/ROUTES | unknown |  | 651 Bytes | Nicht ausgewertet |  |
| README.TXT | text | TXT | 549 Bytes | Text (Inhalt wird nicht ausgewertet) |  |
| SOUND/ADLIB.CAT | music | CAT | 22 Einträge, 62331 Bytes | Musik (AdLib) |  |
| SOUND/CDSPEED.DAT | data | DAT | 2 Bytes | Daten (nicht ausgewertet) |  |
| SOUND/GM.CAT | music | CAT | 22 Einträge, 61886 Bytes | Musik (General MIDI) | musics/GMNEWINT: catPos 0; musics/GMSIG: catPos 1; musics/GMWAITLO: catPos 2; musics/GMGEO1: catPos 3; musics/GMGEO2: catPos 4; musics/GMGEO3: catPos 5; musics/GMGEO4: catPos 6; musics/GMGEO5: catPos 7; musics/GMGEO6: catPos 8; musics/GMINTER1: catPos 9; musics/GMINTER2: catPos 10; musics/GMINTER3: catPos 11; musics/GMISPOSH: catPos 12; musics/GMINTER0: catPos 12; musics/GMBIGMAR: catPos 13; musics/GMDEFBAS: catPos 14; musics/GMATTBAS: catPos 15; musics/GMDEBGOO: catPos 16; musics/GMDEBPOR: catPos 17; musics/GMTACWET: catPos 18; musics/GMTACDRY: catPos 19; musics/GMWIN: catPos 20; musics/GMLOSE: catPos 21 |
| SOUND/MVOL.DAT | data | DAT | 2 Bytes | Daten (nicht ausgewertet) |  |
| SOUND/ROLAND.CAT | music | CAT | 22 Einträge, 61542 Bytes | Musik (Roland MT-32) |  |
| TERRAIN/LOFTEMPS.DAT | data | DAT | 114 Einträge, 3648 Bytes | Daten (nicht ausgewertet) |  |
| TERRAIN/SCANG.DAT | data | DAT | 8784 Bytes | Daten (nicht ausgewertet) |  |

