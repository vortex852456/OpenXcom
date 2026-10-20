# Asset-Liste: UFO

> Automatisch erzeugt von `port/tools/Acom.AssetSpec` aus lokal vorhandenen Originaldaten.
> Enthält nur technische Fakten (Größen, Frames, belegte Bereiche, Farbgruppen, Verwendung laut `bin/standard/xcom1`) –
> keine Pixel, Farbwerte, Texte oder Töne. Einträge unter „Verwendung“ sind Slot-Bezeichner der Engine, keine Namen für ACOM-Inhalte.
> HD-Zielgröße = klassische Größe × 4. Regeln für neue Inhalte: `docs/architecture/content-rights.md`.

## Übersicht

| Kategorie | Dateien | Frames/Einträge | Bytes |
|---|---:|---:|---:|
| data | 8 | 112 | 52586 |
| font | 2 | 346 | 56744 |
| images | 157 | 149 | 3569796 |
| maps | 168 | 0 | 277816 |
| music | 5 | 69 | 384840 |
| palette | 2 | 13 | 4254 |
| routes | 199 | 1425 | 34236 |
| sounds | 21 | 212 | 2320637 |
| sprites | 33 | 3615 | 610151 |
| terrain | 54 | 2511 | 632352 |
| text | 7 | 0 | 161102 |
| unknown | 9 | 0 | 31529 |
| video | 1 | 890 | 2474590 |

## Sprite-Sets

### GEODATA/SCANG.DAT

Kleine Markierungen auf dem Globus (4×4) · Frame 4×4 → HD 16×16 · 454 Frames (454 belegt) · Farbgruppen: 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–453

### GEOGRAPH/BASEBITS.PCK

Basisansicht: Gebäude-Bausteine, Fahrzeuge im Hangar, Rangabzeichen, Fahrzeugwaffen · Frame 32×40 → HD 128×160 · 54 Frames (54 belegt) · Farbgruppen: 0,1,2,3,4,5,6,7,8,9,10,11

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 1 | 1,1,30×30 | 0,4 | facilities/STR_LIVING_QUARTERS: spriteShape; facilities/STR_LABORATORY: spriteShape; facilities/STR_WORKSHOP: spriteShape; facilities/STR_LARGE_RADAR_SYSTEM: spriteShape; facilities/STR_GENERAL_STORES: spriteShape; facilities/STR_ALIEN_CONTAINMENT: spriteShape; facilities/STR_PSIONIC_LABORATORY: spriteShape |
| 2 | 1,1,30×30 | 0,4 | facilities/STR_ACCESS_LIFT: spriteShape; facilities/STR_SMALL_RADAR_SYSTEM: spriteShape; facilities/STR_MISSILE_DEFENSES: spriteShape; facilities/STR_GRAV_SHIELD: spriteShape; facilities/STR_MIND_SHIELD: spriteShape; facilities/STR_HYPER_WAVE_DECODER: spriteShape |
| 3 | 1,1,30×30 | 0,4 | facilities/STR_LASER_DEFENSES: spriteShape; facilities/STR_PLASMA_DEFENSES: spriteShape; facilities/STR_FUSION_BALL_DEFENSES: spriteShape |
| 9 | 1,1,31×31 | 0,1,4,5,9 | facilities/STR_HANGAR: spriteShape; facilities/STR_HANGAR: spriteFacility |
| 17 | 7,7,18×18 | 0,2,5 | facilities/STR_ACCESS_LIFT: spriteFacility |
| 18 | 3,3,24×24 | 0,2,3,4,5,6,10,11 | facilities/STR_LIVING_QUARTERS: spriteFacility |
| 19 | 3,3,24×24 | 0,4,5,11 | facilities/STR_LABORATORY: spriteFacility |
| 20 | 3,3,24×24 | 0,4,5,6,9,10 | facilities/STR_WORKSHOP: spriteFacility |
| 21 | 3,3,24×24 | 3,4,5,9 | facilities/STR_SMALL_RADAR_SYSTEM: spriteFacility |
| 22 | 3,3,24×24 | 2,3,4,5,8,9 | facilities/STR_LARGE_RADAR_SYSTEM: spriteFacility |
| 23 | 6,6,20×20 | 0,2,4,5,9 | facilities/STR_MISSILE_DEFENSES: spriteFacility |
| 24 | 2,2,25×25 | 0,2,3,4,6,9 | facilities/STR_GENERAL_STORES: spriteFacility |
| 25 | 4,3,24×24 | 0,2,4,8,11 | facilities/STR_ALIEN_CONTAINMENT: spriteFacility |
| 26 | 5,5,22×22 | 6 | facilities/STR_LASER_DEFENSES: spriteFacility |
| 27 | 5,5,22×22 | 4 | facilities/STR_PLASMA_DEFENSES: spriteFacility |
| 28 | 4,4,23×23 | 1,10 | facilities/STR_FUSION_BALL_DEFENSES: spriteFacility |
| 29 | 2,2,25×25 | 4,6 | facilities/STR_GRAV_SHIELD: spriteFacility |
| 30 | 3,3,24×24 | 4,5,9,10 | facilities/STR_MIND_SHIELD: spriteFacility |
| 31 | 4,4,23×23 | 0,6 | facilities/STR_PSIONIC_LABORATORY: spriteFacility |
| 32 | 3,3,24×24 | 0,3,4,5,9 | facilities/STR_HYPER_WAVE_DECODER: spriteFacility |
| 33 | 0,4,31×32 | 0,1,2,4 | crafts/STR_SKYRANGER: sprite (Hangar, +33) |
| 34 | 5,10,24×23 | 0,4,5,9 | crafts/STR_LIGHTNING: sprite (Hangar, +33) |
| 35 | 4,7,27×30 | 0,1,4,5 | crafts/STR_AVENGER: sprite (Hangar, +33) |
| 36 | 4,8,26×26 | 0,4,5,8 | crafts/STR_INTERCEPTOR: sprite (Hangar, +33) |
| 37 | 8,13,18×17 | 0,4,5 | crafts/STR_FIRESTORM: sprite (Hangar, +33) |
| 48 | 0,0,15×17 | 3 | craftWeapons/STR_STINGRAY: sprite (Basis, +48) |
| 49 | 0,0,15×17 | 3 | craftWeapons/STR_AVALANCHE: sprite (Basis, +48) |
| 50 | 0,0,15×17 | 3 | craftWeapons/STR_CANNON_UC: sprite (Basis, +48) |
| 51 | 0,0,15×17 | 3 | craftWeapons/STR_FUSION_BALL_UC: sprite (Basis, +48) |
| 52 | 0,0,15×17 | 3 | craftWeapons/STR_LASER_CANNON_UC: sprite (Basis, +48) |
| 53 | 0,0,15×17 | 3 | craftWeapons/STR_PLASMA_BEAM_UC: sprite (Basis, +48) |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0, 4–8, 10–16, 38–47

### GEOGRAPH/INTICON.PCK

Geoscape/Abfangen: Fahrzeug- und Waffensymbole · Frame 32×40 → HD 128×160 · 16 Frames (16 belegt) · Farbgruppen: 5,10

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 0,0,32×20 | 5 | crafts/STR_SKYRANGER: sprite (Liste) |
| 1 | 0,0,32×20 | 5 | crafts/STR_LIGHTNING: sprite (Liste) |
| 2 | 0,0,32×20 | 5 | crafts/STR_AVENGER: sprite (Liste) |
| 3 | 0,0,32×20 | 5 | crafts/STR_INTERCEPTOR: sprite (Liste) |
| 4 | 0,0,32×20 | 5 | crafts/STR_FIRESTORM: sprite (Liste) |
| 5 | 0,0,15×17 | 5 | craftWeapons/STR_STINGRAY: sprite (Abfangfenster, +5) |
| 6 | 0,0,15×17 | 5 | craftWeapons/STR_AVALANCHE: sprite (Abfangfenster, +5) |
| 7 | 0,0,15×17 | 5 | craftWeapons/STR_CANNON_UC: sprite (Abfangfenster, +5) |
| 8 | 0,0,15×17 | 5 | craftWeapons/STR_FUSION_BALL_UC: sprite (Abfangfenster, +5) |
| 9 | 0,0,15×17 | 5 | craftWeapons/STR_LASER_CANNON_UC: sprite (Abfangfenster, +5) |
| 10 | 0,0,15×17 | 5 | craftWeapons/STR_PLASMA_BEAM_UC: sprite (Abfangfenster, +5) |
| 11 | 2,0,18×25 | 10 | crafts/STR_SKYRANGER: sprite (Abfangfenster, +11) |
| 12 | 1,3,20×20 | 10 | crafts/STR_LIGHTNING: sprite (Abfangfenster, +11) |
| 13 | 1,0,21×25 | 10 | crafts/STR_AVENGER: sprite (Abfangfenster, +11) |
| 14 | 1,0,21×25 | 10 | crafts/STR_INTERCEPTOR: sprite (Abfangfenster, +11) |
| 15 | 1,5,21×15 | 10 | crafts/STR_FIRESTORM: sprite (Abfangfenster, +11) |

### GEOGRAPH/TEXTURE.DAT

Globus-Oberflächentexturen (32×32) · Frame 32×32 → HD 128×128 · 39 Frames (39 belegt) · Farbgruppen: 1,2,3,4,9,10,13

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–38

### UFOGRAPH/CURSOR.PCK

Kampfkarten-Cursor und Zielmarkierungen · Frame 32×40 → HD 128×160 · 17 Frames (17 belegt) · Farbgruppen: 0,1,2,5,8,9

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–16

### UFOGRAPH/DETBLOB.DAT

Bewegungsscanner: Signal-Punkte (16×16) · Frame 16×16 → HD 64×64 · 15 Frames (15 belegt) · Farbgruppen: 0,1,5

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–14

### UFOGRAPH/HIT.PCK

Nahkampf-/Treffer-Animationen · Frame 32×40 → HD 128×160 · 4 Frames (4 belegt) · Farbgruppen: 1,2

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 6,14,22×21 | 1,2 | items/STR_HWP_ROCKETS: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HWP_FUSION_BOMB: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HC_HE_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HC_I_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_AC_HE_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_AC_I_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_SMALL_ROCKET: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_LARGE_ROCKET: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_INCENDIARY_ROCKET: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_BLASTER_BOMB: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_STUN_BOMB: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 1–3

- Hinweis: Verweise auf Frames, die in den Originaldaten fehlen (vom Repo/Mod ergänzt): 26, 36, 46 – z. B. items/STR_HWP_CANNON_SHELLS: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_PISTOL_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_RIFLE_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HC_AP_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_AC_AP_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK)

### UFOGRAPH/INTICON.PCK

 · Frame 32×40 → HD 128×160 · 5 Frames (5 belegt) · Farbgruppen: 0,7

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 0,0,24×24 | 0,7 | crafts/STR_SKYRANGER: sprite (Liste) |
| 1 | 0,0,24×24 | 0,7 | crafts/STR_LIGHTNING: sprite (Liste) |
| 2 | 0,0,24×24 | 0,7 | crafts/STR_AVENGER: sprite (Liste) |
| 3 | 0,0,24×24 | 0,7 | crafts/STR_INTERCEPTOR: sprite (Liste) |
| 4 | 0,0,24×24 | 0,7 | crafts/STR_FIRESTORM: sprite (Liste) |

- Hinweis: Verweise auf Frames, die in den Originaldaten fehlen (vom Repo/Mod ergänzt): 5–15 – z. B. craftWeapons/STR_STINGRAY: sprite (Abfangfenster, +5)

### UFOGRAPH/MEDIBITS.DAT

Medikit-Fenster: Körperteile (52×58) · Frame 52×58 → HD 208×232 · 6 Frames (6 belegt) · Farbgruppen: 4

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–5

### UFOGRAPH/SMOKE.PCK

Rauch, Feuer, Treffereffekte, Rangabzeichen im Kampf · Frame 32×40 → HD 128×160 · 56 Frames (56 belegt) · Farbgruppen: 0,1,3,4,5,8,9,14

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 2,9,28×29 | 0,1,4,9 | items/STR_HWP_ROCKETS: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HWP_FUSION_BOMB: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HC_HE_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HC_I_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_AC_HE_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_AC_I_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_SMALL_ROCKET: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_LARGE_ROCKET: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_INCENDIARY_ROCKET: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_BLASTER_BOMB: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_STUN_BOMB: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |
| 26 | 13,13,6×6 | 0,8 | items/STR_HWP_CANNON_SHELLS: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_PISTOL_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_RIFLE_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HC_AP_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_AC_AP_AMMO: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |
| 36 | 12,12,7×7 | 1 | items/STR_TANK_LASER_CANNON: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_LASER_PISTOL: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_LASER_RIFLE: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HEAVY_LASER: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |
| 46 | 13,13,6×6 | 4 | items/STR_HOVERTANK_PLASMA: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_HEAVY_PLASMA_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_PLASMA_RIFLE_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/STR_PLASMA_PISTOL_CLIP: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/CYBERDISC_WEAPON: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/CELATID_WEAPON: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK); items/SECTOPOD_WEAPON: hitAnimation (je nach Trefferart SMOKE.PCK oder HIT.PCK) |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 1–25, 27–35, 37–45, 47–55

### UFOGRAPH/SPICONS.DAT

Kleine Symbole (Spezialaktionen, Fähigkeiten, 32×24) · Frame 32×24 → HD 128×96 · 2 Frames (2 belegt) · Farbgruppen: 0,1,2,5,14

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–1

### UFOGRAPH/X1.PCK

Große Explosion (128×64) · Frame 128×64 → HD 512×256 · 8 Frames (8 belegt) · Farbgruppen: 1,5,6,9

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–7

### UNITS/BIGOBS.PCK

Gegenstände im Inventar (große Darstellung, 32×48) · Frame 32×48 → HD 128×192 · 57 Frames (57 belegt) · Farbgruppen: 0,1,2,3,4,5,6,7,8,9,10,11,12,14,15

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 0,5,16×43 | 2,9,14 | items/STR_LASER_RIFLE: bigSprite |
| 1 | 0,5,16×41 | 0 | items/STR_RIFLE: bigSprite |
| 2 | 5,4,7×12 | 0,10 | items/STR_RIFLE_CLIP: bigSprite |
| 3 | 0,5,16×23 | 0 | items/STR_PISTOL: bigSprite |
| 4 | 8,5,4×10 | 0,6 | items/STR_PISTOL_CLIP: bigSprite |
| 5 | 0,2,16×28 | 5,6,14 | items/STR_LASER_PISTOL: bigSprite |
| 6 | 4,4,27×41 | 2,5 | items/STR_HEAVY_LASER: bigSprite |
| 7 | 4,3,22×43 | 0,2,5 | items/STR_AUTO_CANNON: bigSprite |
| 8 | 1,3,30×10 | 0,5,14 | items/STR_AC_AP_AMMO: bigSprite |
| 9 | 1,3,30×10 | 0,2,5 | items/STR_AC_HE_AMMO: bigSprite |
| 10 | 1,3,30×10 | 0,1,5 | items/STR_AC_I_AMMO: bigSprite |
| 11 | 4,3,28×42 | 0,5,14 | items/STR_HEAVY_CANNON: bigSprite |
| 12 | 1,1,30×13 | 0,5,14 | items/STR_HC_AP_AMMO: bigSprite |
| 13 | 1,1,30×13 | 0,2,5,14 | items/STR_HC_HE_AMMO: bigSprite |
| 14 | 1,1,30×13 | 0,1,5,14 | items/STR_HC_I_AMMO: bigSprite |
| 15 | 2,3,28×45 | 0,2,5 | items/STR_ROCKET_LAUNCHER: bigSprite |
| 16 | 2,2,12×44 | 0,1,5 | items/STR_SMALL_ROCKET: bigSprite |
| 17 | 2,2,12×45 | 0,2,5,11 | items/STR_LARGE_ROCKET: bigSprite |
| 18 | 0,2,16×45 | 0,2,5,9,11,14 | items/STR_INCENDIARY_ROCKET: bigSprite |
| 19 | 5,5,8×9 | 0 | items/STR_GRENADE: bigSprite |
| 20 | 4,3,10×11 | 9,14 | items/STR_SMOKE_GRENADE: bigSprite |
| 21 | 3,2,10×12 | 1,6,9,10,14 | items/STR_PROXIMITY_GRENADE: bigSprite |
| 22 | 1,2,30×13 | 1,2,3,4,5 | items/STR_HIGH_EXPLOSIVE: bigSprite |
| 23 | 2,0,12×16 | 1,4,5 | items/STR_MOTION_SCANNER: bigSprite |
| 24 | 0,0,16×32 | 2,4,5,14 | items/STR_MEDI_KIT: bigSprite |
| 25 | 1,1,11×14 | 14 | items/STR_HEAVY_PLASMA_CLIP: bigSprite |
| 26 | 5,0,10×48 | 0,5,7,14 | items/STR_STUN_ROD: bigSprite |
| 28 | 0,1,32×47 | 2,5,6,11,14 | items/STR_CORPSE_ARMOR: bigSprite |
| 29 | 6,7,19×19 | 14 | items/STR_MIND_PROBE: bigSprite |
| 30 | 4,1,26×46 | 0,5,14 | items/STR_HEAVY_PLASMA: bigSprite |
| 31 | 1,3,15×43 | 0,14 | items/STR_PLASMA_RIFLE: bigSprite |
| 32 | 0,1,16×29 | 2,5,11,14 | items/STR_PLASMA_PISTOL: bigSprite |
| 33 | 0,2,16×38 | 0,2,12,14 | items/STR_PSI_AMP: bigSprite |
| 34 | 6,1,6×11 | 5 | items/STR_PLASMA_PISTOL_CLIP: bigSprite |
| 35 | 2,1,27×47 | 0,14 | items/STR_BLASTER_LAUNCHER: bigSprite |
| 36 | 3,5,10×22 | 5 | items/STR_BLASTER_BOMB: bigSprite |
| 37 | 2,1,25×31 | 3,4,5,9,10,14 | items/STR_SMALL_LAUNCHER: bigSprite |
| 38 | 4,3,9×9 | 11 | items/STR_STUN_BOMB: bigSprite |
| 39 | 3,3,8×10 | 5,6 | items/STR_ALIEN_GRENADE: bigSprite |
| 40 | 5,4,25×41 | 0,5,14,15 | items/STR_HOVERTANK_PLASMA: bigSprite; items/STR_HOVERTANK_LAUNCHER: bigSprite |
| 41 | 3,1,7×14 | 0,2 | items/STR_PLASMA_RIFLE_CLIP: bigSprite |
| 42 | 0,2,32×45 | 0,5,14 | items/STR_TANK_ROCKET_LAUNCHER: bigSprite |
| 43 | 3,4,27×41 | 0,5,14 | items/STR_TANK_CANNON: bigSprite |
| 44 | 0,0,32×48 | 2,5,14 | items/STR_CORPSE_SUIT: bigSprite |
| 45 | 0,0,31×48 | 2,5,6,8,9,14 | items/STR_CORPSE: bigSprite; items/CIVM_CORPSE: bigSprite; items/CIVF_CORPSE: bigSprite |
| 46 | 0,1,32×43 | 4,5 | items/STR_SECTOID_CORPSE: bigSprite |
| 47 | 0,1,32×46 | 4,5,7,11 | items/STR_CELATID_CORPSE: bigSprite |
| 48 | 0,0,32×48 | 3,4,9,12 | items/STR_MUTON_CORPSE: bigSprite |
| 49 | 0,1,32×47 | 1,5,14 | items/STR_ETHEREAL_CORPSE: bigSprite |
| 50 | 0,1,32×47 | 3,4,5,6,10,14 | items/STR_SNAKEMAN_CORPSE: bigSprite |
| 51 | 0,1,32×47 | 9,11,12 | items/STR_SILACOID_CORPSE: bigSprite |
| 52 | 0,0,32×48 | 6,11,12,14 | items/STR_FLOATER_CORPSE: bigSprite |
| 53 | 0,0,32×47 | 0,4,9,11,14 | items/STR_CHRYSSALID_CORPSE: bigSprite |
| 54 | 1,4,29×41 | 0,1,2,5,15 | items/STR_TANK_LASER_CANNON: bigSprite |
| 55 | 4,5,7×7 | 0,1 | items/STR_ELECTRO_FLARE: bigSprite |
| 56 | 0,0,16×16 | 0,1,9 | items/STR_ELERIUM_115: bigSprite |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 27

- Hinweis: Verweise auf Frames, die in den Originaldaten fehlen (vom Repo/Mod ergänzt): 57–61 – z. B. ersetzt/ergänzt durch Repo-Datei Resources/Weapons/Zombie.png (extraSprites); items/ZOMBIE_WEAPON: bigSprite

### UNITS/CELATID.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 28 Frames (28 belegt) · Farbgruppen: 4,7,11

Verwendung: armors/CELATID_ARMOR: spriteSheet (drawingRoutine 9)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–27

### UNITS/CHRYS.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 227 Frames (227 belegt) · Farbgruppen: 0,4,9,11

Verwendung: armors/CHRYSSALID_ARMOR: spriteSheet (drawingRoutine 7)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–226

### UNITS/CIVF.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 75 Frames (75 belegt) · Farbgruppen: 0,2,5,6

Verwendung: armors/CIVF_ARMOR: spriteSheet (drawingRoutine 4)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–74

### UNITS/CIVM.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 76 Frames (76 belegt) · Farbgruppen: 0,2,5,6

Verwendung: armors/CIVM_ARMOR: spriteSheet (drawingRoutine 4)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–75

### UNITS/CYBER.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 56 Frames (56 belegt) · Farbgruppen: 2,4,5,8,11

Verwendung: armors/CYBERDISC_ARMOR: spriteSheet (drawingRoutine 3)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–55

### UNITS/ETHEREAL.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 75 Frames (75 belegt) · Farbgruppen: 1,2,14

Verwendung: armors/ETHEREAL_ARMOR0: spriteSheet (drawingRoutine 4); armors/ETHEREAL_ARMOR1: spriteSheet (drawingRoutine 4); armors/ETHEREAL_ARMOR2: spriteSheet (drawingRoutine 4)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–74

### UNITS/FLOATER.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 99 Frames (99 belegt) · Farbgruppen: 6,11,12,14

Verwendung: armors/FLOATER_ARMOR0: spriteSheet (drawingRoutine 1); armors/FLOATER_ARMOR1: spriteSheet (drawingRoutine 1); armors/FLOATER_ARMOR2: spriteSheet (drawingRoutine 1)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–98

### UNITS/FLOOROB.PCK

Gegenstände auf dem Boden der Kampfkarte · Frame 32×40 → HD 128×160 · 73 Frames (73 belegt) · Farbgruppen: 0,1,2,3,4,5,6,7,9,10,11,12,13,14,15

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 4,29,23×5 | 2,9,14 | items/STR_TANK_CANNON: floorSprite; items/STR_TANK_ROCKET_LAUNCHER: floorSprite; items/STR_TANK_LASER_CANNON: floorSprite; items/STR_HOVERTANK_PLASMA: floorSprite; items/STR_HOVERTANK_LAUNCHER: floorSprite; items/STR_LASER_RIFLE: floorSprite |
| 1 | 4,30,24×5 | 0,2,15 | items/STR_RIFLE: floorSprite |
| 2 | 12,30,8×4 | 0,10 | items/STR_RIFLE_CLIP: floorSprite |
| 3 | 10,30,11×5 | 0 | items/STR_PISTOL: floorSprite |
| 4 | 12,31,7×3 | 0,10 | items/STR_PISTOL_CLIP: floorSprite |
| 5 | 10,30,11×5 | 5,6 | items/STR_LASER_PISTOL: floorSprite |
| 6 | 6,28,22×8 | 5,14 | items/STR_HEAVY_LASER: floorSprite |
| 7 | 2,26,25×9 | 2,5,15 | items/STR_AUTO_CANNON: floorSprite |
| 8 | 10,29,12×4 | 0,5,14 | items/STR_AC_AP_AMMO: floorSprite |
| 9 | 10,29,12×4 | 2,5,15 | items/STR_AC_HE_AMMO: floorSprite |
| 10 | 10,29,12×4 | 0,1,5 | items/STR_AC_I_AMMO: floorSprite |
| 11 | 6,27,21×10 | 0,5,14 | items/STR_HEAVY_CANNON: floorSprite |
| 12 | 9,28,14×5 | 0,5,14 | items/STR_HC_AP_AMMO: floorSprite |
| 13 | 9,28,14×5 | 2,5,14,15 | items/STR_HC_HE_AMMO: floorSprite |
| 14 | 8,28,14×5 | 0,1,5,14 | items/STR_HC_I_AMMO: floorSprite |
| 15 | 4,29,24×7 | 0,2,5 | items/STR_ROCKET_LAUNCHER: floorSprite |
| 16 | 9,30,17×4 | 0,1,5 | items/STR_SMALL_ROCKET: floorSprite |
| 17 | 7,29,19×6 | 0,5 | items/STR_LARGE_ROCKET: floorSprite |
| 18 | 6,28,20×7 | 0,5,9,11,14 | items/STR_INCENDIARY_ROCKET: floorSprite |
| 19 | 14,29,4×4 | 0 | items/STR_GRENADE: floorSprite |
| 20 | 13,27,6×7 | 9,14 | items/STR_SMOKE_GRENADE: floorSprite |
| 21 | 14,28,5×5 | 1,2,6,9,10,14 | items/STR_PROXIMITY_GRENADE: floorSprite |
| 22 | 10,28,13×6 | 2,3,5 | items/STR_HIGH_EXPLOSIVE: floorSprite |
| 23 | 13,28,6×7 | 1,4,5 | items/STR_MOTION_SCANNER: floorSprite |
| 24 | 9,28,15×6 | 2,4,5,14 | items/STR_MEDI_KIT: floorSprite |
| 26 | 4,30,24×4 | 5,7,14 | items/STR_STUN_ROD: floorSprite |
| 28 | 13,28,6×6 | 14 | items/STR_MIND_PROBE: floorSprite |
| 29 | 5,28,23×7 | 0,5,14 | items/STR_HEAVY_PLASMA: floorSprite |
| 30 | 5,29,22×5 | 0,15 | items/STR_PLASMA_RIFLE: floorSprite |
| 31 | 11,30,11×4 | 2,5,15 | items/STR_PLASMA_PISTOL: floorSprite; items/CYBERDISC_WEAPON: floorSprite; items/REAPER_WEAPON: floorSprite; items/CHRYSSALID_WEAPON: floorSprite; items/CELATID_WEAPON: floorSprite; items/SILACOID_WEAPON: floorSprite; items/SECTOPOD_WEAPON: floorSprite |
| 32 | 7,29,19×6 | 0,12,14 | items/STR_PSI_AMP: floorSprite |
| 33 | 12,29,7×4 | 2,4,5 | items/STR_HEAVY_PLASMA_CLIP: floorSprite; items/STR_PLASMA_RIFLE_CLIP: floorSprite; items/STR_PLASMA_PISTOL_CLIP: floorSprite |
| 34 | 4,27,24×9 | 0,14,15 | items/STR_BLASTER_LAUNCHER: floorSprite |
| 35 | 11,30,10×4 | 5,7,13 | items/STR_BLASTER_BOMB: floorSprite |
| 36 | 8,28,16×6 | 0,4,5,10,15 | items/STR_SMALL_LAUNCHER: floorSprite |
| 37 | 14,29,4×4 | 2,11 | items/STR_STUN_BOMB: floorSprite; items/STR_ELERIUM_115: floorSprite |
| 38 | 14,28,4×5 | 5,6 | items/STR_ALIEN_GRENADE: floorSprite |
| 39 | 4,22,24×15 | 2,5,6,9 | items/STR_CORPSE: floorSprite |
| 40 | 2,22,24×15 | 2,6,14 | items/STR_CORPSE_ARMOR: floorSprite |
| 41 | 3,20,23×16 | 2,5,11 | items/STR_CORPSE_SUIT: floorSprite |
| 42 | 1,24,26×12 | 4,5 | items/STR_SECTOID_CORPSE: floorSprite |
| 43 | 0,25,28×14 | 0,4,7,11 | items/STR_CELATID_CORPSE: floorSprite |
| 44 | 0,23,31×12 | 4,9,12 | items/STR_MUTON_CORPSE: floorSprite |
| 45 | 2,15,28×23 | 1,2,9,14 | items/STR_ETHEREAL_CORPSE: floorSprite |
| 46 | 2,19,30×19 | 4,5,6,14 | items/STR_SNAKEMAN_CORPSE: floorSprite |
| 47 | 1,22,30×17 | 9,11,12 | items/STR_SILACOID_CORPSE: floorSprite |
| 48 | 3,21,28×15 | 6,11,12,14 | items/STR_FLOATER_CORPSE: floorSprite |
| 49 | 1,22,28×16 | 0,4,9,11 | items/STR_CHRYSSALID_CORPSE: floorSprite |
| 50 | 0,22,27×15 | 0,2,5,6 | items/CIVM_CORPSE: floorSprite |
| 51 | 5,22,23×15 | 0,2,5,6 | items/CIVF_CORPSE: floorSprite |
| 52 | 0,25,32×15 | 0,4,5,9 | items/REAPER_CORPSE_1: floorSprite |
| 53 | 4,25,22×10 | 0,3,4,9,11 | items/REAPER_CORPSE_4: floorSprite |
| 54 | 9,17,23×22 | 0,4,9 | items/REAPER_CORPSE_3: floorSprite |
| 55 | 0,19,25×19 | 0,4,9 | items/REAPER_CORPSE_2: floorSprite |
| 56 | 1,28,30×12 | 5 | items/CYBERDISC_CORPSE_1: floorSprite |
| 57 | 1,25,22×14 | 2,5,11 | items/CYBERDISC_CORPSE_2: floorSprite |
| 58 | 9,25,23×14 | 2,5,11 | items/CYBERDISC_CORPSE_3: floorSprite |
| 59 | 3,25,26×8 | 2,5,11 | items/CYBERDISC_CORPSE_4: floorSprite |
| 60 | 0,16,31×24 | 0,2,3,5,11 | items/SECTOPOD_CORPSE_1: floorSprite |
| 61 | 0,9,30×29 | 0,2,4,5,9,11 | items/SECTOPOD_CORPSE_2: floorSprite |
| 62 | 2,15,30×23 | 0,2,5,11 | items/SECTOPOD_CORPSE_3: floorSprite |
| 63 | 5,25,21×8 | 0,2,3,4,5,11 | items/SECTOPOD_CORPSE_4: floorSprite |
| 64 | 0,27,32×13 | 0,1,2,13,14 | items/HOVERTANK_CORPSE_1: floorSprite |
| 65 | 0,24,23×15 | 0,1,2,11,13,14 | items/HOVERTANK_CORPSE_2: floorSprite |
| 66 | 9,21,23×19 | 0,1,2,11,13,14 | items/HOVERTANK_CORPSE_3: floorSprite |
| 67 | 4,25,24×9 | 0,2 | items/HOVERTANK_CORPSE_4: floorSprite |
| 68 | 0,24,32×16 | 0,1,5 | items/TANK_CORPSE_1: floorSprite |
| 69 | 0,24,24×15 | 0,1,5 | items/TANK_CORPSE_2: floorSprite |
| 70 | 11,22,21×17 | 0,1,5 | items/TANK_CORPSE_3: floorSprite |
| 71 | 2,25,27×11 | 0,4,5,14 | items/TANK_CORPSE_4: floorSprite |
| 72 | 14,30,4×3 | 0,1,9 | items/STR_ELECTRO_FLARE: floorSprite |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 25, 27

### UNITS/HANDOB.PCK

Gegenstände in der Hand von Einheiten, je 8 Blickrichtungen · Frame 32×40 → HD 128×160 · 128 Frames (128 belegt) · Farbgruppen: 0,2,4,5,9,10,12,13,14,15

| Frame | Bereich x,y,b×h | Farbgruppen | Verwendung |
|---:|---|---|---|
| 0 | 11,12,13×8 | 0 | items/STR_TANK_CANNON: handSprite (Richtung 0); items/STR_TANK_ROCKET_LAUNCHER: handSprite (Richtung 0); items/STR_TANK_LASER_CANNON: handSprite (Richtung 0); items/STR_HOVERTANK_PLASMA: handSprite (Richtung 0); items/STR_HOVERTANK_LAUNCHER: handSprite (Richtung 0); items/STR_RIFLE: handSprite (Richtung 0) |
| 1 | 22,13,2×9 | 0 | items/STR_TANK_CANNON: handSprite (Richtung 1); items/STR_TANK_ROCKET_LAUNCHER: handSprite (Richtung 1); items/STR_TANK_LASER_CANNON: handSprite (Richtung 1); items/STR_HOVERTANK_PLASMA: handSprite (Richtung 1); items/STR_HOVERTANK_LAUNCHER: handSprite (Richtung 1); items/STR_RIFLE: handSprite (Richtung 1) |
| 2 | 11,14,13×8 | 0 | items/STR_TANK_CANNON: handSprite (Richtung 2); items/STR_TANK_ROCKET_LAUNCHER: handSprite (Richtung 2); items/STR_TANK_LASER_CANNON: handSprite (Richtung 2); items/STR_HOVERTANK_PLASMA: handSprite (Richtung 2); items/STR_HOVERTANK_LAUNCHER: handSprite (Richtung 2); items/STR_RIFLE: handSprite (Richtung 2) |
| 3 | 5,16,20×7 | 0 | items/STR_TANK_CANNON: handSprite (Richtung 3); items/STR_TANK_ROCKET_LAUNCHER: handSprite (Richtung 3); items/STR_TANK_LASER_CANNON: handSprite (Richtung 3); items/STR_HOVERTANK_PLASMA: handSprite (Richtung 3); items/STR_HOVERTANK_LAUNCHER: handSprite (Richtung 3); items/STR_RIFLE: handSprite (Richtung 3) |
| 4 | 7,14,12×8 | 0 | items/STR_TANK_CANNON: handSprite (Richtung 4); items/STR_TANK_ROCKET_LAUNCHER: handSprite (Richtung 4); items/STR_TANK_LASER_CANNON: handSprite (Richtung 4); items/STR_HOVERTANK_PLASMA: handSprite (Richtung 4); items/STR_HOVERTANK_LAUNCHER: handSprite (Richtung 4); items/STR_RIFLE: handSprite (Richtung 4) |
| 5 | 8,13,2×8 | 0 | items/STR_TANK_CANNON: handSprite (Richtung 5); items/STR_TANK_ROCKET_LAUNCHER: handSprite (Richtung 5); items/STR_TANK_LASER_CANNON: handSprite (Richtung 5); items/STR_HOVERTANK_PLASMA: handSprite (Richtung 5); items/STR_HOVERTANK_LAUNCHER: handSprite (Richtung 5); items/STR_RIFLE: handSprite (Richtung 5) |
| 6 | 8,12,12×8 | 0 | items/STR_TANK_CANNON: handSprite (Richtung 6); items/STR_TANK_ROCKET_LAUNCHER: handSprite (Richtung 6); items/STR_TANK_LASER_CANNON: handSprite (Richtung 6); items/STR_HOVERTANK_PLASMA: handSprite (Richtung 6); items/STR_HOVERTANK_LAUNCHER: handSprite (Richtung 6); items/STR_RIFLE: handSprite (Richtung 6) |
| 7 | 6,15,20×7 | 0 | items/STR_TANK_CANNON: handSprite (Richtung 7); items/STR_TANK_ROCKET_LAUNCHER: handSprite (Richtung 7); items/STR_TANK_LASER_CANNON: handSprite (Richtung 7); items/STR_HOVERTANK_PLASMA: handSprite (Richtung 7); items/STR_HOVERTANK_LAUNCHER: handSprite (Richtung 7); items/STR_RIFLE: handSprite (Richtung 7) |
| 8 | 10,11,14×10 | 2,9,14 | items/STR_LASER_RIFLE: handSprite (Richtung 0) |
| 9 | 22,12,3×10 | 2,9,14 | items/STR_LASER_RIFLE: handSprite (Richtung 1) |
| 10 | 10,14,14×10 | 2,9,14 | items/STR_LASER_RIFLE: handSprite (Richtung 2) |
| 11 | 5,16,20×5 | 2,9,14 | items/STR_LASER_RIFLE: handSprite (Richtung 3) |
| 12 | 6,13,14×10 | 2,9,14 | items/STR_LASER_RIFLE: handSprite (Richtung 4) |
| 13 | 7,12,3×9 | 2,9,14 | items/STR_LASER_RIFLE: handSprite (Richtung 5) |
| 14 | 10,10,14×10 | 2,9,14 | items/STR_LASER_RIFLE: handSprite (Richtung 6) |
| 15 | 7,15,20×5 | 2,9,14 | items/STR_LASER_RIFLE: handSprite (Richtung 7) |
| 16 | 11,11,14×9 | 5 | items/STR_HEAVY_LASER: handSprite (Richtung 0) |
| 17 | 21,12,3×10 | 5 | items/STR_HEAVY_LASER: handSprite (Richtung 1) |
| 18 | 10,14,14×9 | 5 | items/STR_HEAVY_LASER: handSprite (Richtung 2) |
| 19 | 5,14,20×8 | 5 | items/STR_HEAVY_LASER: handSprite (Richtung 3) |
| 20 | 8,14,14×9 | 5 | items/STR_HEAVY_LASER: handSprite (Richtung 4) |
| 21 | 8,12,3×10 | 5 | items/STR_HEAVY_LASER: handSprite (Richtung 5) |
| 22 | 8,12,14×9 | 5 | items/STR_HEAVY_LASER: handSprite (Richtung 6) |
| 23 | 7,13,20×8 | 5 | items/STR_HEAVY_LASER: handSprite (Richtung 7) |
| 24 | 10,10,14×11 | 0,5 | items/STR_HEAVY_CANNON: handSprite (Richtung 0) |
| 25 | 21,10,3×12 | 0,5,14 | items/STR_HEAVY_CANNON: handSprite (Richtung 1) |
| 26 | 12,12,14×11 | 0,5,14 | items/STR_HEAVY_CANNON: handSprite (Richtung 2) |
| 27 | 5,13,21×9 | 0,5,14 | items/STR_HEAVY_CANNON: handSprite (Richtung 3) |
| 28 | 6,12,14×9 | 0,5,14 | items/STR_HEAVY_CANNON: handSprite (Richtung 4) |
| 29 | 8,10,3×11 | 0,5,14 | items/STR_HEAVY_CANNON: handSprite (Richtung 5) |
| 30 | 7,12,14×9 | 0,5,14 | items/STR_HEAVY_CANNON: handSprite (Richtung 6) |
| 31 | 6,12,21×9 | 0,5,14 | items/STR_HEAVY_CANNON: handSprite (Richtung 7) |
| 32 | 9,11,15×11 | 5,15 | items/STR_AUTO_CANNON: handSprite (Richtung 0) |
| 33 | 21,9,4×12 | 5,15 | items/STR_AUTO_CANNON: handSprite (Richtung 1) |
| 34 | 10,11,15×11 | 5,15 | items/STR_AUTO_CANNON: handSprite (Richtung 2) |
| 35 | 6,14,21×7 | 5,15 | items/STR_AUTO_CANNON: handSprite (Richtung 3) |
| 36 | 7,12,15×10 | 5,15 | items/STR_AUTO_CANNON: handSprite (Richtung 4) |
| 37 | 7,11,4×11 | 5,15 | items/STR_AUTO_CANNON: handSprite (Richtung 5) |
| 38 | 6,10,15×10 | 5,15 | items/STR_AUTO_CANNON: handSprite (Richtung 6) |
| 39 | 5,13,21×7 | 5,15 | items/STR_AUTO_CANNON: handSprite (Richtung 7) |
| 40 | 11,11,12×11 | 5,14 | items/STR_HEAVY_PLASMA: handSprite (Richtung 0) |
| 41 | 22,12,2×10 | 5,14 | items/STR_HEAVY_PLASMA: handSprite (Richtung 1) |
| 42 | 12,13,12×11 | 5,14 | items/STR_HEAVY_PLASMA: handSprite (Richtung 2) |
| 43 | 7,16,20×8 | 5,14 | items/STR_HEAVY_PLASMA: handSprite (Richtung 3) |
| 44 | 8,13,12×9 | 5,14 | items/STR_HEAVY_PLASMA: handSprite (Richtung 4) |
| 45 | 8,13,2×10 | 5 | items/STR_HEAVY_PLASMA: handSprite (Richtung 5) |
| 46 | 9,12,12×9 | 5,14 | items/STR_HEAVY_PLASMA: handSprite (Richtung 6) |
| 47 | 5,15,20×8 | 5,14 | items/STR_HEAVY_PLASMA: handSprite (Richtung 7) |
| 48 | 11,13,12×7 | 0 | items/STR_PLASMA_RIFLE: handSprite (Richtung 0) |
| 49 | 22,12,3×8 | 0 | items/STR_PLASMA_RIFLE: handSprite (Richtung 1) |
| 50 | 12,15,12×7 | 0 | items/STR_PLASMA_RIFLE: handSprite (Richtung 2) |
| 51 | 8,17,20×4 | 0,15 | items/STR_PLASMA_RIFLE: handSprite (Richtung 3) |
| 52 | 9,15,12×8 | 0 | items/STR_PLASMA_RIFLE: handSprite (Richtung 4) |
| 53 | 7,12,3×9 | 0 | items/STR_PLASMA_RIFLE: handSprite (Richtung 5) |
| 54 | 7,12,12×8 | 0 | items/STR_PLASMA_RIFLE: handSprite (Richtung 6) |
| 55 | 4,16,20×4 | 0,15 | items/STR_PLASMA_RIFLE: handSprite (Richtung 7) |
| 56 | 9,11,15×9 | 0 | items/STR_BLASTER_LAUNCHER: handSprite (Richtung 0) |
| 57 | 21,9,5×12 | 0 | items/STR_BLASTER_LAUNCHER: handSprite (Richtung 1) |
| 58 | 11,13,15×9 | 0 | items/STR_BLASTER_LAUNCHER: handSprite (Richtung 2) |
| 59 | 4,14,24×7 | 0 | items/STR_BLASTER_LAUNCHER: handSprite (Richtung 3) |
| 60 | 7,12,15×10 | 0 | items/STR_BLASTER_LAUNCHER: handSprite (Richtung 4) |
| 61 | 6,9,5×12 | 0 | items/STR_BLASTER_LAUNCHER: handSprite (Richtung 5) |
| 62 | 7,10,15×10 | 0 | items/STR_BLASTER_LAUNCHER: handSprite (Richtung 6) |
| 63 | 4,13,24×7 | 0 | items/STR_BLASTER_LAUNCHER: handSprite (Richtung 7) |
| 64 | 11,12,11×8 | 4,5,10 | items/STR_SMALL_LAUNCHER: handSprite (Richtung 0) |
| 65 | 21,12,4×8 | 4,5,10 | items/STR_SMALL_LAUNCHER: handSprite (Richtung 1) |
| 66 | 13,13,11×8 | 4,5,10 | items/STR_SMALL_LAUNCHER: handSprite (Richtung 2) |
| 67 | 8,15,18×6 | 4,5,10 | items/STR_SMALL_LAUNCHER: handSprite (Richtung 3) |
| 68 | 8,13,12×8 | 4,5,10 | items/STR_SMALL_LAUNCHER: handSprite (Richtung 4) |
| 69 | 7,13,4×8 | 5,10 | items/STR_SMALL_LAUNCHER: handSprite (Richtung 5) |
| 70 | 10,12,12×8 | 4,5,10 | items/STR_SMALL_LAUNCHER: handSprite (Richtung 6) |
| 71 | 6,14,18×6 | 4,5,10 | items/STR_SMALL_LAUNCHER: handSprite (Richtung 7) |
| 72 | 9,10,14×11 | 0,5 | items/STR_ROCKET_LAUNCHER: handSprite (Richtung 0) |
| 73 | 21,10,4×11 | 0,5 | items/STR_ROCKET_LAUNCHER: handSprite (Richtung 1) |
| 74 | 11,12,14×11 | 0,5 | items/STR_ROCKET_LAUNCHER: handSprite (Richtung 2) |
| 75 | 5,14,22×8 | 0,5 | items/STR_ROCKET_LAUNCHER: handSprite (Richtung 3) |
| 76 | 7,11,15×11 | 0,5 | items/STR_ROCKET_LAUNCHER: handSprite (Richtung 4) |
| 77 | 7,11,4×11 | 0,5 | items/STR_ROCKET_LAUNCHER: handSprite (Richtung 5) |
| 78 | 8,10,15×11 | 0,5 | items/STR_ROCKET_LAUNCHER: handSprite (Richtung 6) |
| 79 | 5,13,22×8 | 0,5 | items/STR_ROCKET_LAUNCHER: handSprite (Richtung 7) |
| 80 | 10,13,13×7 | 13,14 | items/STR_STUN_ROD: handSprite (Richtung 0) |
| 81 | 22,11,3×9 | 13,14 | items/STR_STUN_ROD: handSprite (Richtung 1) |
| 82 | 12,15,13×7 | 13,14 | items/STR_STUN_ROD: handSprite (Richtung 2) |
| 83 | 8,17,21×3 | 13,14 | items/STR_STUN_ROD: handSprite (Richtung 3) |
| 84 | 9,15,13×9 | 13,14 | items/STR_STUN_ROD: handSprite (Richtung 4) |
| 85 | 7,12,3×10 | 13,14 | items/STR_STUN_ROD: handSprite (Richtung 5) |
| 86 | 6,12,13×9 | 13,14 | items/STR_STUN_ROD: handSprite (Richtung 6) |
| 87 | 3,16,21×3 | 13,14 | items/STR_STUN_ROD: handSprite (Richtung 7) |
| 88 | 10,13,14×7 | 0,12 | items/STR_PSI_AMP: handSprite (Richtung 0) |
| 89 | 22,10,4×11 | 0,12 | items/STR_PSI_AMP: handSprite (Richtung 1) |
| 90 | 11,15,14×7 | 0,12 | items/STR_PSI_AMP: handSprite (Richtung 2) |
| 91 | 8,14,19×7 | 0,12,14 | items/STR_PSI_AMP: handSprite (Richtung 3) |
| 92 | 9,12,11×11 | 0,12 | items/STR_PSI_AMP: handSprite (Richtung 4) |
| 93 | 7,12,3×10 | 0,12 | items/STR_PSI_AMP: handSprite (Richtung 5) |
| 94 | 8,9,11×11 | 0,12 | items/STR_PSI_AMP: handSprite (Richtung 6) |
| 95 | 5,13,19×7 | 0,12,14 | items/STR_PSI_AMP: handSprite (Richtung 7) |
| 96 | 22,12,6×4 | 0 | items/STR_PISTOL: handSprite (Richtung 0) |
| 97 | 19,15,9×3 | 0 | items/STR_PISTOL: handSprite (Richtung 1) |
| 98 | 14,16,7×6 | 0 | items/STR_PISTOL: handSprite (Richtung 2) |
| 99 | 8,16,3×6 | 0 | items/STR_PISTOL: handSprite (Richtung 3) |
| 100 | 4,13,7×6 | 0 | items/STR_PISTOL: handSprite (Richtung 4) |
| 101 | 4,12,9×3 | 0 | items/STR_PISTOL: handSprite (Richtung 5) |
| 102 | 12,10,6×4 | 0 | items/STR_PISTOL: handSprite (Richtung 6) |
| 103 | 21,11,3×4 | 0 | items/STR_PISTOL: handSprite (Richtung 7) |
| 104 | 22,12,6×4 | 5 | items/STR_PLASMA_PISTOL: handSprite (Richtung 0); items/CYBERDISC_WEAPON: handSprite (Richtung 0); items/REAPER_WEAPON: handSprite (Richtung 0); items/CHRYSSALID_WEAPON: handSprite (Richtung 0); items/CELATID_WEAPON: handSprite (Richtung 0); items/SILACOID_WEAPON: handSprite (Richtung 0); items/SECTOPOD_WEAPON: handSprite (Richtung 0) |
| 105 | 19,15,9×3 | 5,14 | items/STR_PLASMA_PISTOL: handSprite (Richtung 1); items/CYBERDISC_WEAPON: handSprite (Richtung 1); items/REAPER_WEAPON: handSprite (Richtung 1); items/CHRYSSALID_WEAPON: handSprite (Richtung 1); items/CELATID_WEAPON: handSprite (Richtung 1); items/SILACOID_WEAPON: handSprite (Richtung 1); items/SECTOPOD_WEAPON: handSprite (Richtung 1) |
| 106 | 14,16,7×6 | 5,14 | items/STR_PLASMA_PISTOL: handSprite (Richtung 2); items/CYBERDISC_WEAPON: handSprite (Richtung 2); items/REAPER_WEAPON: handSprite (Richtung 2); items/CHRYSSALID_WEAPON: handSprite (Richtung 2); items/CELATID_WEAPON: handSprite (Richtung 2); items/SILACOID_WEAPON: handSprite (Richtung 2); items/SECTOPOD_WEAPON: handSprite (Richtung 2) |
| 107 | 8,16,3×6 | 5,14 | items/STR_PLASMA_PISTOL: handSprite (Richtung 3); items/CYBERDISC_WEAPON: handSprite (Richtung 3); items/REAPER_WEAPON: handSprite (Richtung 3); items/CHRYSSALID_WEAPON: handSprite (Richtung 3); items/CELATID_WEAPON: handSprite (Richtung 3); items/SILACOID_WEAPON: handSprite (Richtung 3); items/SECTOPOD_WEAPON: handSprite (Richtung 3) |
| 108 | 4,13,7×6 | 5,14 | items/STR_PLASMA_PISTOL: handSprite (Richtung 4); items/CYBERDISC_WEAPON: handSprite (Richtung 4); items/REAPER_WEAPON: handSprite (Richtung 4); items/CHRYSSALID_WEAPON: handSprite (Richtung 4); items/CELATID_WEAPON: handSprite (Richtung 4); items/SILACOID_WEAPON: handSprite (Richtung 4); items/SECTOPOD_WEAPON: handSprite (Richtung 4) |
| 109 | 4,12,9×3 | 5,14 | items/STR_PLASMA_PISTOL: handSprite (Richtung 5); items/CYBERDISC_WEAPON: handSprite (Richtung 5); items/REAPER_WEAPON: handSprite (Richtung 5); items/CHRYSSALID_WEAPON: handSprite (Richtung 5); items/CELATID_WEAPON: handSprite (Richtung 5); items/SILACOID_WEAPON: handSprite (Richtung 5); items/SECTOPOD_WEAPON: handSprite (Richtung 5) |
| 110 | 12,10,6×4 | 5 | items/STR_PLASMA_PISTOL: handSprite (Richtung 6); items/CYBERDISC_WEAPON: handSprite (Richtung 6); items/REAPER_WEAPON: handSprite (Richtung 6); items/CHRYSSALID_WEAPON: handSprite (Richtung 6); items/CELATID_WEAPON: handSprite (Richtung 6); items/SILACOID_WEAPON: handSprite (Richtung 6); items/SECTOPOD_WEAPON: handSprite (Richtung 6) |
| 111 | 21,11,3×4 | 5 | items/STR_PLASMA_PISTOL: handSprite (Richtung 7); items/CYBERDISC_WEAPON: handSprite (Richtung 7); items/REAPER_WEAPON: handSprite (Richtung 7); items/CHRYSSALID_WEAPON: handSprite (Richtung 7); items/CELATID_WEAPON: handSprite (Richtung 7); items/SILACOID_WEAPON: handSprite (Richtung 7); items/SECTOPOD_WEAPON: handSprite (Richtung 7) |
| 112 | 22,12,6×4 | 5 | items/STR_LASER_PISTOL: handSprite (Richtung 0) |
| 113 | 19,15,9×3 | 0,5 | items/STR_LASER_PISTOL: handSprite (Richtung 1) |
| 114 | 14,16,7×6 | 0,5 | items/STR_LASER_PISTOL: handSprite (Richtung 2) |
| 115 | 8,16,3×6 | 0,5 | items/STR_LASER_PISTOL: handSprite (Richtung 3) |
| 116 | 4,13,7×6 | 0,5 | items/STR_LASER_PISTOL: handSprite (Richtung 4) |
| 117 | 4,12,9×3 | 0,5 | items/STR_LASER_PISTOL: handSprite (Richtung 5) |
| 118 | 12,10,6×4 | 5 | items/STR_LASER_PISTOL: handSprite (Richtung 6) |
| 119 | 21,11,3×4 | 5 | items/STR_LASER_PISTOL: handSprite (Richtung 7) |

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 120–127

### UNITS/MUTON.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 267 Frames (267 belegt) · Farbgruppen: 4,9,12

Verwendung: armors/MUTON_ARMOR0: spriteSheet (drawingRoutine 10); armors/MUTON_ARMOR1: spriteSheet (drawingRoutine 10); armors/MUTON_ARMOR2: spriteSheet (drawingRoutine 10)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–266

### UNITS/SECTOID.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 267 Frames (267 belegt) · Farbgruppen: 4,5

Verwendung: armors/SECTOID_ARMOR0: spriteSheet

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–266

### UNITS/SILACOID.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 9 Frames (9 belegt) · Farbgruppen: 9,11,12

Verwendung: armors/SILACOID_ARMOR: spriteSheet (drawingRoutine 8)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–8

### UNITS/SNAKEMAN.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 131 Frames (131 belegt) · Farbgruppen: 2,4,5,6,14

Verwendung: armors/SNAKEMAN_ARMOR0: spriteSheet (drawingRoutine 6); armors/SNAKEMAN_ARMOR1: spriteSheet (drawingRoutine 6); armors/SNAKEMAN_ARMOR2: spriteSheet (drawingRoutine 6)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–130

### UNITS/TANKS.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 128 Frames (128 belegt) · Farbgruppen: 0,2,4,5,8,11,13,14

Verwendung: armors/TANK_ARMOR: spriteSheet (drawingRoutine 2); armors/HOVERTANK_ARMOR: spriteSheet (drawingRoutine 2)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–127

### UNITS/XCOM_0.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 275 Frames (275 belegt) · Farbgruppen: 0,2,5,6,8,9,11,14

Verwendung: armors/STR_NONE_UC: spriteSheet

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–274

### UNITS/XCOM_1.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 275 Frames (275 belegt) · Farbgruppen: 2,5,6,9,14

Verwendung: armors/STR_PERSONAL_ARMOR_UC: spriteSheet

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–274

### UNITS/XCOM_2.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 283 Frames (283 belegt) · Farbgruppen: 1,2,5,11

Verwendung: armors/STR_POWER_SUIT_UC: spriteSheet; armors/STR_FLYING_SUIT_UC: spriteSheet

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–282

### UNITS/X_REAP.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 160 Frames (160 belegt) · Farbgruppen: 0,1,2,3,9

Verwendung: armors/REAPER_ARMOR: spriteSheet (drawingRoutine 5)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–159

### UNITS/X_ROB.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 160 Frames (160 belegt) · Farbgruppen: 0,1,2,3,5

Verwendung: armors/SECTOPOD_ARMOR: spriteSheet (drawingRoutine 5)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–159

### UNITS/ZOMBIE.PCK

Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung) · Frame 32×40 → HD 128×160 · 90 Frames (90 belegt) · Farbgruppen: 0,1,2,4,5,6,10,11

Verwendung: armors/ZOMBIE_ARMOR: spriteSheet (drawingRoutine 4)

Belegte Frames ohne Ruleset-Bezug (Animationen/engine-intern): 0–89

## Vollbilder und Einzelgrafiken

| Datei | Format | Größe → HD | Farbgruppen | Zweck | Verwendung |
|---|---|---|---|---|---|
| GEODATA/INTERWIN.DAT | SCR | 160×556 → 640×2224 | 0,5,6,7,10 | Abfangfenster: Vorschaubilder der Flugobjekte, untereinander gestapelt | ufos/STR_SMALL_SCOUT: sprite (Vorschau-Zeile 0); ufos/STR_MEDIUM_SCOUT: sprite (Vorschau-Zeile 1); ufos/STR_LARGE_SCOUT: sprite (Vorschau-Zeile 2); ufos/STR_HARVESTER: sprite (Vorschau-Zeile 3); ufos/STR_ABDUCTOR: sprite (Vorschau-Zeile 4); ufos/STR_TERROR_SHIP: sprite (Vorschau-Zeile 5); ufos/STR_BATTLESHIP: sprite (Vorschau-Zeile 6); ufos/STR_SUPPLY_SHIP: sprite (Vorschau-Zeile 7) |
| GEOGRAPH/BACK01.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/mainMenu: backgroundImage; interfaces/newBattleMenu: backgroundImage; interfaces/newGameMenu: backgroundImage; interfaces/endGameStatistics: backgroundImage; interfaces/optionsMenu: backgroundImage; interfaces/modsMenu: backgroundImage; interfaces/saveMenus: backgroundImage; interfaces/pauseMenu: backgroundImage; interfaces/noteMenu: backgroundImage; interfaces/selectMusicTrack: backgroundImage; interfaces/dogfightExperience: backgroundImage; interfaces/targetInfo: backgroundImage; interfaces/baseNaming: backgroundImage; interfaces/psiTraining: backgroundImage; interfaces/allocatePsi: backgroundImage; interfaces/oxceLinks: backgroundImage; interfaces/ufopaedia: backgroundImage; interfaces/promotions: backgroundImage; interfaces/cannotReequip: backgroundImage; interfaces/debriefing: backgroundImage; interfaces/commendations: backgroundImage; interfaces/geoscape: backgroundImage; interfaces/placeFacility: backgroundImage |
| GEOGRAPH/BACK02.SCR | SCR | 320×200 → 1280×800 | 7,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/trainingFinished: backgroundImage; interfaces/martialTraining: backgroundImage; interfaces/allocateMartial: backgroundImage; interfaces/commendationsLate: backgroundImage; interfaces/soldierList: backgroundImage; interfaces/soldierMemorial: backgroundImage; interfaces/soldierDiary: backgroundImage; interfaces/soldierDiaryPerformance: backgroundImage; interfaces/craftSoldiers: backgroundImage; interfaces/craftPilots: backgroundImage; interfaces/craftPilotsSelect: backgroundImage; interfaces/soldierTransformation: backgroundImage; interfaces/transformationList: backgroundImage |
| GEOGRAPH/BACK03.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK04.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/baseDefense: backgroundImage; interfaces/baseDefense_geo: backgroundImage; interfaces/craftEquipment: backgroundImage; interfaces/craftEquipmentSave: backgroundImage; interfaces/craftEquipmentLoad: backgroundImage |
| GEOGRAPH/BACK05.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/tests: backgroundImage; interfaces/geoResearch: backgroundImage; interfaces/geoResearchComplete: backgroundImage; interfaces/geoResearchRequired: backgroundImage; interfaces/manageContainment: backgroundImage; interfaces/globalContainmentMenu: backgroundImage; interfaces/selectFacility: backgroundImage; interfaces/researchMenu: backgroundImage; interfaces/globalResearchMenu: backgroundImage; interfaces/globalResearchDiary: backgroundImage; interfaces/selectNewResearch: backgroundImage; interfaces/allocateResearch: backgroundImage; interfaces/techTreeViewer: backgroundImage; interfaces/techTreeSelect: backgroundImage; interfaces/statsForNerds: backgroundImage |
| GEOGRAPH/BACK06.SCR | SCR | 320×200 → 1280×800 | 0,4,12 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK07.SCR | SCR | 320×200 → 1280×800 | 10 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK08.SCR | SCR | 320×200 → 1280×800 | 0,7,8 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK09.SCR | SCR | 320×200 → 1280×800 | 0,8,10,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK10.SCR | SCR | 320×200 → 1280×800 | 6,8 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK11.SCR | SCR | 320×200 → 1280×800 | 5,6,11,12,13 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/BACK12.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/geoCraft: backgroundImage; interfaces/intercept: backgroundImage; interfaces/ufoTracker: backgroundImage; interfaces/craftPatrol: backgroundImage; interfaces/craftError: backgroundImage; interfaces/craftPilotError: backgroundImage; interfaces/confirmDestination: backgroundImage; interfaces/confirmCydonia: backgroundImage; interfaces/lowFuel: backgroundImage |
| GEOGRAPH/BACK13.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/alienBase: backgroundImage; interfaces/itemsArriving: backgroundImage; interfaces/geoNewCraft: backgroundImage; interfaces/geoNewFacility: backgroundImage; interfaces/geoNewItem: backgroundImage; interfaces/monthlyReport: backgroundImage; interfaces/fundingWindow: backgroundImage; interfaces/sellMenu: backgroundImage; interfaces/dependencyTree: backgroundImage; interfaces/itemLocations: backgroundImage; interfaces/buyMenu: backgroundImage; interfaces/transferBaseSelect: backgroundImage; interfaces/transferMenu: backgroundImage; interfaces/transferConfirm: backgroundImage; interfaces/transferInfo: backgroundImage; interfaces/dismantleFacility: backgroundImage; interfaces/storesInfo: backgroundImage; interfaces/costsInfo: backgroundImage; interfaces/sackSoldier: backgroundImage |
| GEOGRAPH/BACK14.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/soldierArmor: backgroundImage; interfaces/soldierTransform: backgroundImage; interfaces/soldierRank: backgroundImage; interfaces/soldierAvatar: backgroundImage; interfaces/soldierDiaryLight: backgroundImage; interfaces/soldierBonus: backgroundImage; interfaces/craftArmor: backgroundImage; interfaces/craftInfo: backgroundImage; interfaces/craftWeapons: backgroundImage; interfaces/craftSelect: backgroundImage |
| GEOGRAPH/BACK15.SCR | SCR | 320×200 → 1280×800 | 13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/confirmLanding: backgroundImage; interfaces/UFOInfo: backgroundImage; interfaces/UFOLost: backgroundImage; interfaces/multipleTargets: backgroundImage; interfaces/baseDestroyed: backgroundImage; interfaces/dogfightInfo: backgroundImage |
| GEOGRAPH/BACK16.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/soldierDiaryMission: backgroundImage |
| GEOGRAPH/BACK17.SCR | SCR | 320×200 → 1280×800 | 14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) | interfaces/geoManufacture: backgroundImage; interfaces/geoManufactureComplete: backgroundImage; interfaces/manufactureMenu: backgroundImage; interfaces/globalManufactureMenu: backgroundImage; interfaces/selectNewManufacture: backgroundImage; interfaces/allocateManufacture: backgroundImage; interfaces/manufactureInfo: backgroundImage |
| GEOGRAPH/GEOBORD.SCR | SCR | 320×200 → 1280×800 | 0,4,10,11,15 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/GRAPHS.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,8,11,12,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/UP001.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,9,10,11,12 | Enzyklopädie-Bild | ufopaedia/STR_AVENGER: image_id |
| GEOGRAPH/UP002.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,5,6,8,9,10,11,12,15 | Enzyklopädie-Bild | ufopaedia/STR_INTERCEPTOR: image_id |
| GEOGRAPH/UP003.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,4,5,10,11,12 | Enzyklopädie-Bild | ufopaedia/STR_LIGHTNING: image_id |
| GEOGRAPH/UP004.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,5,6,7,8,10,11,12 | Enzyklopädie-Bild | ufopaedia/STR_SKYRANGER: image_id |
| GEOGRAPH/UP005.SPK | SPK | 320×200 → 1280×800 | 1,2,3,5,6,8,9,10,11,12 | Enzyklopädie-Bild | ufopaedia/STR_FIRESTORM: image_id |
| GEOGRAPH/UP006.SPK | SPK | 320×200 → 1280×800 | 0,1,2,9,15 | Enzyklopädie-Bild | ufopaedia/STR_STINGRAY: image_id |
| GEOGRAPH/UP007.SPK | SPK | 320×200 → 1280×800 | 0,1,2,5,6,9,14,15 | Enzyklopädie-Bild | ufopaedia/STR_AVALANCHE: image_id |
| GEOGRAPH/UP008.SPK | SPK | 320×200 → 1280×800 | 0,1,14,15 | Enzyklopädie-Bild | ufopaedia/STR_CANNON_UC: image_id |
| GEOGRAPH/UP009.SPK | SPK | 320×200 → 1280×800 | 0,6,12,14,15 | Enzyklopädie-Bild | ufopaedia/STR_FUSION_BALL_UC: image_id |
| GEOGRAPH/UP010.SPK | SPK | 320×200 → 1280×800 | 0,6,13,14 | Enzyklopädie-Bild | ufopaedia/STR_LASER_CANNON_UC: image_id |
| GEOGRAPH/UP011.SPK | SPK | 320×200 → 1280×800 | 0,3,5,8,14,15 | Enzyklopädie-Bild | ufopaedia/STR_PLASMA_BEAM_UC: image_id |
| GEOGRAPH/UP012.SPK | SPK | 320×200 → 1280×800 | 5,6,10,14 | Enzyklopädie-Bild | ufopaedia/STR_CELATID: image_id |
| GEOGRAPH/UP013.SPK | SPK | 320×200 → 1280×800 | 4,5,6,10,13 | Enzyklopädie-Bild | ufopaedia/STR_CELATID_AUTOPSY: image_id |
| GEOGRAPH/UP014.SPK | SPK | 320×200 → 1280×800 | 2,3,5,6,10 | Enzyklopädie-Bild | ufopaedia/STR_CHRYSSALID: image_id |
| GEOGRAPH/UP015.SPK | SPK | 320×200 → 1280×800 | 1,2,3,4,5,6,7,10,12,13 | Enzyklopädie-Bild | ufopaedia/STR_CHRYSSALID_AUTOPSY: image_id |
| GEOGRAPH/UP016.SPK | SPK | 320×200 → 1280×800 | 1,5,6,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_ETHEREAL: image_id |
| GEOGRAPH/UP017.SPK | SPK | 320×200 → 1280×800 | 1,5,6,7,8,10,13,14 | Enzyklopädie-Bild | ufopaedia/STR_ETHEREAL_AUTOPSY: image_id |
| GEOGRAPH/UP018.SPK | SPK | 320×200 → 1280×800 | 1,2,3,5,6,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_FLOATER: image_id |
| GEOGRAPH/UP019.SPK | SPK | 320×200 → 1280×800 | 2,3,5,6,7,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_FLOATER_AUTOPSY: image_id |
| GEOGRAPH/UP020.SPK | SPK | 320×200 → 1280×800 | 2,3,4,5,6,7,10,13 | Enzyklopädie-Bild | ufopaedia/STR_MUTON: image_id |
| GEOGRAPH/UP021.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_MUTON_AUTOPSY: image_id |
| GEOGRAPH/UP022.SPK | SPK | 320×200 → 1280×800 | 1,2,3,5,6,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_REAPER: image_id |
| GEOGRAPH/UP023.SPK | SPK | 320×200 → 1280×800 | 1,2,3,5,6,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_REAPER_AUTOPSY: image_id |
| GEOGRAPH/UP024.SPK | SPK | 320×200 → 1280×800 | 3,5,6,10,12,14 | Enzyklopädie-Bild | ufopaedia/STR_SECTOID: image_id |
| GEOGRAPH/UP025.SPK | SPK | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,9,10,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_SECTOID_AUTOPSY: image_id |
| GEOGRAPH/UP026.SPK | SPK | 320×200 → 1280×800 | 1,2,3,5,6,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_SECTOPOD: image_id |
| GEOGRAPH/UP027.SPK | SPK | 320×200 → 1280×800 | 1,2,3,5,6,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_SECTOPOD_AUTOPSY: image_id |
| GEOGRAPH/UP028.SPK | SPK | 320×200 → 1280×800 | 5,6,7,10,14 | Enzyklopädie-Bild | ufopaedia/STR_SILACOID: image_id |
| GEOGRAPH/UP029.SPK | SPK | 320×200 → 1280×800 | 1,2,3,5,6,7,10,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_SILACOID_AUTOPSY: image_id |
| GEOGRAPH/UP030.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,5,6,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_SNAKEMAN: image_id |
| GEOGRAPH/UP031.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,4,5,6,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_SNAKEMAN_AUTOPSY: image_id |
| GEOGRAPH/UP032.SPK | SPK | 320×200 → 1280×800 | 1,2,3,5,6,7,12 | Enzyklopädie-Bild | ufopaedia/STR_UFO_POWER_SOURCE: image_id |
| GEOGRAPH/UP033.SPK | SPK | 320×200 → 1280×800 | 1,2,3,4,5,6,7,8,9,10,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_UFO_NAVIGATION: image_id |
| GEOGRAPH/UP034.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,5,6,9,11 | Enzyklopädie-Bild | ufopaedia/STR_UFO_CONSTRUCTION: image_id |
| GEOGRAPH/UP035.SPK | SPK | 320×200 → 1280×800 | 1,2,3,4,5,6,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_ALIEN_FOOD: image_id |
| GEOGRAPH/UP036.SPK | SPK | 320×200 → 1280×800 | 0,2,3,5,6,8,9,14 | Enzyklopädie-Bild | ufopaedia/STR_ALIEN_REPRODUCTION: image_id |
| GEOGRAPH/UP037.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,4,6,7,8,9,10,11,12 | Enzyklopädie-Bild | ufopaedia/STR_ALIEN_ENTERTAINMENT: image_id |
| GEOGRAPH/UP038.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_ALIEN_SURGERY: image_id |
| GEOGRAPH/UP039.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_EXAMINATION_ROOM: image_id |
| GEOGRAPH/UP040.SPK | SPK | 320×200 → 1280×800 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_ALIEN_ALLOYS: image_id |
| GEOGRAPH/UP041.SPK | SPK | 320×200 → 1280×800 | 1,3,5,6,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_CYBERDISC: image_id |
| GEOGRAPH/UP042.SPK | SPK | 320×200 → 1280×800 | 1,2,3,4,5,6,9,10,11,12,13,14 | Enzyklopädie-Bild | ufopaedia/STR_CYBERDISC_AUTOPSY: image_id |
| GEOGRAPH/UP_BORD1.SPK | SPK | 320×200 → 1280×800 | 0,7 | Enzyklopädie-Bild |  |
| GEOGRAPH/UP_BORD2.SCR | SCR | 320×200 → 1280×800 | 0,8,10,13,14 | Vollbild-Grafik (Hintergrund, Menü, Ereignis) |  |
| GEOGRAPH/UP_BORD2.SPK | SPK | 320×200 → 1280×800 | 0,7,9,13 | Enzyklopädie-Bild |  |
| GEOGRAPH/UP_BORD3.SPK | SPK | 320×200 → 1280×800 | 6,8 | Enzyklopädie-Bild |  |
| UFOGRAPH/BIGOB_00.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_01.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_02.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_03.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_04.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_05.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_06.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_07.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_08.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_09.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_10.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_11.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_12.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_13.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_14.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_15.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_16.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_17.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_18.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_19.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_20.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_21.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_22.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_23.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_24.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_25.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_26.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_27.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_28.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_29.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_30.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_31.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_32.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_33.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_34.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_35.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_36.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_37.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_38.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_39.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_40.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_41.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_42.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_43.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_44.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_45.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_46.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_47.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_48.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_49.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_50.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_51.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_52.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_53.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_54.PCK | SPK | 320×200 → 1280×800 | – |  |  |
| UFOGRAPH/BIGOB_55.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/BIGOB_56.PCK | SPK | 320×200 → 1280×800 | 15 |  |  |
| UFOGRAPH/DETBORD.PCK | SPK | 320×200 → 1280×800 | 0,2,3,4,5,7,15 | Fensterrahmen: Einheitendetails |  |
| UFOGRAPH/DETBORD2.PCK | SPK | 320×200 → 1280×800 | 2,3,4 | Fensterrahmen: Einheitendetails (Variante) |  |
| UFOGRAPH/ICONS.PCK | SPK | 320×200 → 1280×800 | 0,1,2,3,4,5,8,12,14 | Kampf-Bedienleiste (Vollbild-Grafik) |  |
| UFOGRAPH/MAN_0F0.SPK | SPK | 320×200 → 1280×800 | 1,2,5,6,8,9 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0F1.SPK | SPK | 320×200 → 1280×800 | 1,2,5,6,8,9,10 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0F2.SPK | SPK | 320×200 → 1280×800 | 0,2,5,8,9,10 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0F3.SPK | SPK | 320×200 → 1280×800 | 2,5,8,9,10 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0M0.SPK | SPK | 320×200 → 1280×800 | 1,2,5,6,8,9 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0M1.SPK | SPK | 320×200 → 1280×800 | 0,1,2,5,6,8,9 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0M2.SPK | SPK | 320×200 → 1280×800 | 0,2,5,8,9,10 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_0M3.SPK | SPK | 320×200 → 1280×800 | 2,5,8,9,10 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_NONE_UC: spriteInv |
| UFOGRAPH/MAN_1F0.SPK | SPK | 320×200 → 1280×800 | 1,2,6,9,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_PERSONAL_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1F1.SPK | SPK | 320×200 → 1280×800 | 1,2,6,9,10,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_PERSONAL_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1F2.SPK | SPK | 320×200 → 1280×800 | 0,2,9,10,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_PERSONAL_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1F3.SPK | SPK | 320×200 → 1280×800 | 2,9,10,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_PERSONAL_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1M0.SPK | SPK | 320×200 → 1280×800 | 1,6,9,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_PERSONAL_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1M1.SPK | SPK | 320×200 → 1280×800 | 1,6,9,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_PERSONAL_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1M2.SPK | SPK | 320×200 → 1280×800 | 0,9,10,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_PERSONAL_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_1M3.SPK | SPK | 320×200 → 1280×800 | 9,10,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_PERSONAL_ARMOR_UC: spriteInv |
| UFOGRAPH/MAN_2.SPK | SPK | 320×200 → 1280×800 | 0,5,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_POWER_SUIT_UC: spriteInv |
| UFOGRAPH/MAN_3.SPK | SPK | 320×200 → 1280×800 | 0,2,4,5,6,13,14 | Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante) | armors/STR_FLYING_SUIT_UC: spriteInv |
| UFOGRAPH/MEDIBORD.PCK | SPK | 320×200 → 1280×800 | 0,1,3,4,5,6,7,9,14,15 | Fensterrahmen: Medikit |  |
| UFOGRAPH/MOC1.LBM | LBM | 320×200 → 1280×800 | – | Kampf-Palette/Grafik im LBM-Format |  |
| UFOGRAPH/SCANBORD.PCK | SPK | 320×200 → 1280×800 | 0,1,2,4,5,8,9,11,12,14 | Fensterrahmen: Bewegungsscanner |  |
| UFOGRAPH/TAC00.SCR | SCR | 320×200 → 1280×800 | 0,2,5,10 | Kampf-Oberfläche (Bedienleiste) |  |
| UFOGRAPH/TAC01.SCR | SPK | 320×200 → 1280×800 | 5 | Kampf-Oberfläche (Variante) |  |
| UFOGRAPH/UNIBORD.PCK | SPK | 320×200 → 1280×800 | 0,3,4 | Fensterrahmen: Einheiteninfo |  |
| UFOINTRO/PICT1.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| UFOINTRO/PICT2.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| UFOINTRO/PICT3.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| UFOINTRO/PICT4.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| UFOINTRO/PICT5.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| UFOINTRO/PICT6.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |
| UFOINTRO/PICT7.LBM | LBM | 320×200 → 1280×800 | – | Video-Sequenz |  |

## Geländesätze (TERRAIN)

| Satz | Kacheln 32×40 (belegt) | MCD-Einträge | Farbgruppen | Verwendet von |
|---|---:|---:|---|---|
| TERRAIN/AVENGER | 62 (62) | 59 | 0,1,2,5,6,7,8,9,10,11,12,13,14,15 |  |
| TERRAIN/BARN | 29 (29) | 29 | 0,5,9,10,15 | terrains/CULTA: mapDataSets |
| TERRAIN/BLANKS | 2 (2) | 2 | 0,6,9,10 | terrains/CULTA: mapDataSets; terrains/DESERT: mapDataSets; terrains/FOREST: mapDataSets; terrains/JUNGLE: mapDataSets; terrains/MARS: mapDataSets; terrains/MOUNT: mapDataSets; terrains/POLAR: mapDataSets; terrains/UBASE: mapDataSets; terrains/URBAN: mapDataSets; terrains/XBASE: mapDataSets |
| TERRAIN/BRAIN | 8 (8) | 4 | 0,1,3,4,5,11,14 | terrains/UBASE: mapDataSets |
| TERRAIN/CULTIVAT | 37 (37) | 37 | 0,1,3,4,5,8,9,10,11,14,15 | terrains/CULTA: mapDataSets |
| TERRAIN/DESERT | 42 (42) | 66 | 0,1,2,4,5,6,9,10,14 | terrains/DESERT: mapDataSets |
| TERRAIN/FOREST | 59 (59) | 83 | 0,1,3,4,5,6,10 | terrains/FOREST: mapDataSets |
| TERRAIN/FRNITURE | 26 (26) | 26 | 0,1,2,3,4,5,7,8,9,10,11,12,14 | terrains/URBAN: mapDataSets |
| TERRAIN/JUNGLE | 82 (82) | 82 | 0,1,2,3,4,5,6,7,8,9,10,15 | terrains/JUNGLE: mapDataSets |
| TERRAIN/LIGHTNIN | 51 (51) | 42 | 0,1,2,3,5,6,8,9,10,11,12,13,14,15 |  |
| TERRAIN/MARS | 37 (37) | 36 | 0,1,4,5,9,11,14 | terrains/MARS: mapDataSets |
| TERRAIN/MOUNT | 54 (54) | 78 | 0,3,4,5,10,11,15 | terrains/MOUNT: mapDataSets |
| TERRAIN/PLANE | 63 (63) | 65 | 0,1,2,3,5,6,8,9,10,14,15 |  |
| TERRAIN/POLAR | 123 (123) | 81 | 0,5,7,8,11,13,14 | terrains/POLAR: mapDataSets |
| TERRAIN/ROADS | 23 (23) | 23 | 0,1,2,5,6,8,9,14,15 | terrains/URBAN: mapDataSets |
| TERRAIN/UFO1 | 27 (27) | 20 | 0,5,7,8,12,14,15 |  |
| TERRAIN/URBAN | 116 (116) | 112 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | terrains/URBAN: mapDataSets |
| TERRAIN/URBITS | 25 (25) | 25 | 0,1,2,3,4,5,6,8,10,11,15 | terrains/URBAN: mapDataSets |
| TERRAIN/U_BASE | 71 (71) | 67 | 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 | terrains/UBASE: mapDataSets |
| TERRAIN/U_BITS | 8 (8) | 8 | 14 |  |
| TERRAIN/U_DISEC2 | 22 (22) | 17 | 0,1,2,3,5,6,7,8,9,10,11,12,13,14,15 |  |
| TERRAIN/U_EXT02 | 40 (40) | 34 | 0,3,8,12,14,15 |  |
| TERRAIN/U_OPER2 | 15 (15) | 15 | 0,8,12,14 |  |
| TERRAIN/U_PODS | 27 (27) | 11 | 0,1,2,5,6,7,8,9,10,11,12,13,14,15 | terrains/UBASE: mapDataSets |
| TERRAIN/U_WALL02 | 66 (66) | 47 | 0,1,2,3,4,7,8,11,12,14,15 | terrains/MARS: mapDataSets; terrains/UBASE: mapDataSets |
| TERRAIN/XBASE1 | 105 (105) | 97 | 0,1,2,3,5,7,8,9,10,12,13,14,15 | terrains/XBASE: mapDataSets |
| TERRAIN/XBASE2 | 63 (63) | 62 | 0,3,5,6,7,8,9,12,13,14,15 | terrains/XBASE: mapDataSets |

## Kartenblöcke (MAPS) und Wegpunkte (ROUTES)

| Block | Größe x×y×z | Wegpunkte | Verwendet von |
|---|---|---:|---|
| AVENGER | 10×20×3 | 3 |  |
| CULTA00 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA01 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA02 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA03 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA04 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA05 | 10×10×1 | 2 | terrains/CULTA: mapBlocks |
| CULTA06 | 10×10×1 | 2 | terrains/CULTA: mapBlocks |
| CULTA07 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA08 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA09 | 10×10×1 | 3 | terrains/CULTA: mapBlocks |
| CULTA10 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA11 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA12 | 10×10×1 | 1 | terrains/CULTA: mapBlocks |
| CULTA13 | 10×10×1 | 4 | terrains/CULTA: mapBlocks |
| CULTA14 | 10×10×1 | 4 | terrains/CULTA: mapBlocks |
| CULTA15 | 10×10×1 | 3 | terrains/CULTA: mapBlocks |
| CULTA16 | 10×10×3 | 9 | terrains/CULTA: mapBlocks |
| CULTA17 | 10×10×2 | 10 | terrains/CULTA: mapBlocks |
| CULTA18 | 10×10×3 | 16 | terrains/CULTA: mapBlocks |
| DESERT00 | 10×10×1 | 1 | terrains/DESERT: mapBlocks |
| DESERT01 | 10×10×1 | 1 | terrains/DESERT: mapBlocks |
| DESERT02 | 10×10×1 | 1 | terrains/DESERT: mapBlocks |
| DESERT03 | 10×10×1 | 4 | terrains/DESERT: mapBlocks |
| DESERT04 | 10×10×1 | 1 | terrains/DESERT: mapBlocks |
| DESERT05 | 10×10×1 | 1 | terrains/DESERT: mapBlocks |
| DESERT06 | 10×10×1 | 2 | terrains/DESERT: mapBlocks |
| DESERT07 | 10×10×1 | 5 | terrains/DESERT: mapBlocks |
| DESERT08 | 10×10×2 | 5 | terrains/DESERT: mapBlocks |
| DESERT09 | 20×20×3 | 15 | terrains/DESERT: mapBlocks |
| DESERT10 | 20×20×3 | 12 | terrains/DESERT: mapBlocks |
| DESERT11 | 20×20×3 | 11 | terrains/DESERT: mapBlocks |
| FOREST00 | 10×10×1 | 1 | terrains/FOREST: mapBlocks |
| FOREST01 | 10×10×1 | 1 | terrains/FOREST: mapBlocks |
| FOREST02 | 10×10×1 | 1 | terrains/FOREST: mapBlocks |
| FOREST03 | 10×10×1 | 2 | terrains/FOREST: mapBlocks |
| FOREST04 | 10×10×1 | 4 | terrains/FOREST: mapBlocks |
| FOREST05 | 10×10×3 | 4 | terrains/FOREST: mapBlocks |
| FOREST06 | 10×10×3 | 1 | terrains/FOREST: mapBlocks |
| FOREST07 | 10×10×3 | 4 | terrains/FOREST: mapBlocks |
| FOREST08 | 10×10×3 | 3 | terrains/FOREST: mapBlocks |
| FOREST09 | 10×10×1 | 5 | terrains/FOREST: mapBlocks |
| FOREST10 | 20×20×3 | 15 | terrains/FOREST: mapBlocks |
| FOREST11 | 20×20×4 | 12 | terrains/FOREST: mapBlocks |
| JUNGLE00 | 10×10×1 | 1 | terrains/JUNGLE: mapBlocks |
| JUNGLE01 | 10×10×1 | 1 | terrains/JUNGLE: mapBlocks |
| JUNGLE02 | 10×10×1 | 1 | terrains/JUNGLE: mapBlocks |
| JUNGLE03 | 10×10×3 | 1 | terrains/JUNGLE: mapBlocks |
| JUNGLE04 | 10×10×3 | 1 | terrains/JUNGLE: mapBlocks |
| JUNGLE05 | 10×10×3 | 1 | terrains/JUNGLE: mapBlocks |
| JUNGLE06 | 10×10×3 | 1 | terrains/JUNGLE: mapBlocks |
| JUNGLE07 | 10×10×3 | 4 | terrains/JUNGLE: mapBlocks |
| JUNGLE08 | 10×10×3 | 4 | terrains/JUNGLE: mapBlocks |
| JUNGLE09 | 10×10×3 | 4 | terrains/JUNGLE: mapBlocks |
| JUNGLE10 | 10×10×3 | 2 | terrains/JUNGLE: mapBlocks |
| JUNGLE11 | 10×10×3 | 2 | terrains/JUNGLE: mapBlocks |
| LIGHTNIN | 20×10×3 | 4 |  |
| MAP3 | 48×32×3 |  |  |
| MAP4 | 48×32×3 |  |  |
| MARS00 | 10×10×1 | 1 | terrains/MARS: mapBlocks |
| MARS01 | 10×10×1 | 1 | terrains/MARS: mapBlocks |
| MARS02 | 10×10×1 | 1 | terrains/MARS: mapBlocks |
| MARS03 | 10×10×1 | 1 | terrains/MARS: mapBlocks |
| MARS04 | 10×10×1 | 5 | terrains/MARS: mapBlocks |
| MARS05 | 10×10×1 | 4 | terrains/MARS: mapBlocks |
| MARS06 | 10×10×1 | 2 | terrains/MARS: mapBlocks |
| MARS07 | 10×10×4 | 2 | terrains/MARS: mapBlocks |
| MARS08 | 10×10×4 | 6 | terrains/MARS: mapBlocks |
| MARS09 | 10×10×4 | 8 | terrains/MARS: mapBlocks |
| MARS10 | 10×10×4 | 3 | terrains/MARS: mapBlocks |
| MOUNT00 | 10×10×1 | 1 | terrains/MOUNT: mapBlocks |
| MOUNT01 | 10×10×1 | 1 | terrains/MOUNT: mapBlocks |
| MOUNT02 | 10×10×1 | 1 | terrains/MOUNT: mapBlocks |
| MOUNT03 | 10×10×1 | 2 | terrains/MOUNT: mapBlocks |
| MOUNT04 | 10×10×1 | 3 | terrains/MOUNT: mapBlocks |
| MOUNT05 | 10×10×1 | 5 | terrains/MOUNT: mapBlocks |
| MOUNT06 | 10×10×2 | 5 | terrains/MOUNT: mapBlocks |
| MOUNT07 | 10×10×2 | 5 | terrains/MOUNT: mapBlocks |
| MOUNT08 | 10×10×2 | 6 | terrains/MOUNT: mapBlocks |
| MOUNT09 | 10×10×2 | 5 | terrains/MOUNT: mapBlocks |
| MOUNT10 | 10×10×4 | 6 | terrains/MOUNT: mapBlocks |
| MOUNT11 | 20×20×4 | 12 | terrains/MOUNT: mapBlocks |
| MOUNT12 | 20×20×4 | 19 | terrains/MOUNT: mapBlocks |
| PABDUCT | 20×10×4 |  |  |
| PHARVEST | 20×20×4 |  |  |
| PLANE | 10×20×3 | 10 |  |
| POLAR00 | 10×10×1 | 1 | terrains/POLAR: mapBlocks |
| POLAR01 | 10×10×1 | 1 | terrains/POLAR: mapBlocks |
| POLAR02 | 10×10×1 | 1 | terrains/POLAR: mapBlocks |
| POLAR03 | 10×10×1 | 2 | terrains/POLAR: mapBlocks |
| POLAR04 | 10×10×1 | 2 | terrains/POLAR: mapBlocks |
| POLAR05 | 10×10×1 | 4 | terrains/POLAR: mapBlocks |
| POLAR06 | 10×10×1 | 1 | terrains/POLAR: mapBlocks |
| POLAR07 | 10×10×1 | 1 | terrains/POLAR: mapBlocks |
| POLAR08 | 10×10×1 | 4 | terrains/POLAR: mapBlocks |
| POLAR09 | 10×10×1 | 1 | terrains/POLAR: mapBlocks |
| POLAR10 | 10×10×1 | 1 | terrains/POLAR: mapBlocks |
| POLAR11 | 10×10×1 | 1 | terrains/POLAR: mapBlocks |
| POLAR12 | 20×20×1 | 12 | terrains/POLAR: mapBlocks |
| POLAR13 | 20×20×1 | 12 | terrains/POLAR: mapBlocks |
| PSCOUT | 10×10×4 |  |  |
| TEMP | 10×10×1 | 0 |  |
| TEMP1 | 10×10×1 | 0 |  |
| TEMP2 | 20×20×1 | 0 |  |
| TEMP3 | 20×20×1 | 0 |  |
| UBASE_00 | 20×20×2 | 25 | terrains/UBASE: mapBlocks |
| UBASE_01 | 20×20×2 | 14 | terrains/UBASE: mapBlocks |
| UBASE_02 | 20×20×2 | 14 | terrains/UBASE: mapBlocks |
| UBASE_03 | 20×20×2 | 15 | terrains/UBASE: mapBlocks |
| UBASE_04 | 10×10×2 | 11 | terrains/UBASE: mapBlocks |
| UBASE_05 | 10×10×2 | 6 | terrains/UBASE: mapBlocks |
| UBASE_06 | 10×10×2 | 12 | terrains/UBASE: mapBlocks |
| UBASE_07 | 10×10×2 | 1 | terrains/UBASE: mapBlocks |
| UBASE_08 | 10×10×2 | 6 | terrains/UBASE: mapBlocks |
| UBASE_09 | 10×10×2 | 4 | terrains/UBASE: mapBlocks |
| UBASE_10 | 10×10×2 | 1 | terrains/UBASE: mapBlocks |
| UBASE_11 | 10×10×2 | 2 | terrains/UBASE: mapBlocks |
| UBASE_12 | 10×10×2 | 5 | terrains/UBASE: mapBlocks |
| UBASE_13 | 10×10×2 | 4 | terrains/UBASE: mapBlocks |
| UBASE_14 | 10×10×2 | 5 | terrains/UBASE: mapBlocks |
| UBASE_15 | 20×20×2 | 21 | terrains/UBASE: mapBlocks |
| UFO1A | 10×10×3 | 6 |  |
| UFO1B | 2×2×3 |  |  |
| UFO_000 | 10×10×2 | 5 |  |
| UFO_010 | 20×20×2 | 21 |  |
| UFO_110 | 10×10×2 | 8 |  |
| UFO_120 | 20×20×2 | 21 |  |
| UFO_130 | 20×20×4 | 44 |  |
| UFO_140 | 20×20×3 | 39 |  |
| UFO_150 | 30×20×3 | 47 |  |
| UFO_160 | 30×30×4 | 89 |  |
| UFO_170 | 30×20×4 | 64 |  |
| URBAN00 | 10×10×3 | 1 | terrains/URBAN: mapBlocks |
| URBAN01 | 10×10×3 | 1 | terrains/URBAN: mapBlocks |
| URBAN02 | 10×10×1 | 1 | terrains/URBAN: mapBlocks |
| URBAN03 | 10×10×1 | 1 | terrains/URBAN: mapBlocks |
| URBAN04 | 10×10×1 | 1 | terrains/URBAN: mapBlocks |
| URBAN05 | 20×20×3 | 29 | terrains/URBAN: mapBlocks |
| URBAN06 | 20×20×3 | 25 | terrains/URBAN: mapBlocks |
| URBAN07 | 20×20×1 | 9 | terrains/URBAN: mapBlocks |
| URBAN08 | 20×20×4 | 30 | terrains/URBAN: mapBlocks |
| URBAN09 | 20×20×2 | 16 | terrains/URBAN: mapBlocks |
| URBAN14 | 10×10×2 | 7 | terrains/URBAN: mapBlocks |
| URBAN15 | 10×10×3 | 8 | terrains/URBAN: mapBlocks |
| URBAN16 | 10×10×1 | 3 | terrains/URBAN: mapBlocks |
| URBAN17 | 10×10×3 | 9 | terrains/URBAN: mapBlocks |
| URBAN18 | 10×10×3 | 10 | terrains/URBAN: mapBlocks |
| XBASE_00 | 10×10×2 | 8 | terrains/XBASE: mapBlocks |
| XBASE_01 | 10×10×2 | 8 | terrains/XBASE: mapBlocks |
| XBASE_02 | 10×10×2 | 9 | terrains/XBASE: mapBlocks |
| XBASE_03 | 10×10×2 | 9 | terrains/XBASE: mapBlocks |
| XBASE_04 | 10×10×2 | 3 | terrains/XBASE: mapBlocks |
| XBASE_05 | 10×10×2 | 6 | terrains/XBASE: mapBlocks |
| XBASE_06 | 10×10×2 | 9 | terrains/XBASE: mapBlocks |
| XBASE_07 | 10×10×2 | 11 | terrains/XBASE: mapBlocks |
| XBASE_08 | 10×10×2 | 12 | terrains/XBASE: mapBlocks |
| XBASE_09 | 10×10×2 | 6 | terrains/XBASE: mapBlocks |
| XBASE_10 | 10×10×2 | 6 | terrains/XBASE: mapBlocks |
| XBASE_11 | 10×10×2 | 6 | terrains/XBASE: mapBlocks |
| XBASE_12 | 10×10×2 | 6 | terrains/XBASE: mapBlocks |
| XBASE_13 | 10×10×2 | 6 | terrains/XBASE: mapBlocks |
| XBASE_14 | 10×10×2 | 10 | terrains/XBASE: mapBlocks |
| XBASE_15 | 10×10×2 | 6 | terrains/XBASE: mapBlocks |
| XBASE_16 | 10×10×2 | 4 | terrains/XBASE: mapBlocks |
| XBASE_17 | 10×10×2 | 4 | terrains/XBASE: mapBlocks |
| XBASE_18 | 10×10×2 | 4 | terrains/XBASE: mapBlocks |
| XBASE_19 | 10×10×2 | 3 | terrains/XBASE: mapBlocks |
| XBASE_20 | 10×10×2 | 1 | terrains/XBASE: mapBlocks |

## Soundeffekte

### SOUND/DRIVERS.CAT

Soundeffekte · 17 Einträge

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

### SOUND/INTRO.CAT

Intro-Soundeffekte · 25 Einträge

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
| 18 | DOS-Rohdaten |  |  |  |
| 19 | DOS-Rohdaten |  |  |  |
| 20 | DOS-Rohdaten |  |  |  |
| 21 | DOS-Rohdaten |  |  |  |
| 22 | DOS-Rohdaten |  |  |  |
| 23 | DOS-Rohdaten |  |  |  |
| 24 | DOS-Rohdaten |  |  |  |

### SOUND/SAMPLE.CAT

Soundeffekte · 14 Einträge

- Hinweis: Enthält den logischen Soundsatz GEO.CAT

| # | Format | Rate | Sekunden | Verwendung |
|---:|---|---:|---:|---|
| 0 | WAV | 11025 | 0.18 |  |
| 1 | WAV | 11025 | 0.93 |  |
| 2 | WAV | 11025 | 0.40 |  |
| 3 | WAV | 11025 | 0.52 |  |
| 4 | WAV | 11025 | 0.80 | craftWeapons/STR_CANNON_UC: sound (GEO.CAT #4) |
| 5 | WAV | 11025 | 0.91 | craftWeapons/STR_STINGRAY: sound (GEO.CAT #5); craftWeapons/STR_AVALANCHE: sound (GEO.CAT #5); facilities/STR_MISSILE_DEFENSES: fireSound (GEO.CAT #5) |
| 6 | WAV | 11025 | 0.18 |  |
| 7 | WAV | 11025 | 1.16 | craftWeapons/STR_FUSION_BALL_UC: sound (GEO.CAT #7); facilities/STR_FUSION_BALL_DEFENSES: fireSound (GEO.CAT #7) |
| 8 | WAV | 11025 | 1.03 | craftWeapons/STR_LASER_CANNON_UC: sound (GEO.CAT #8); facilities/STR_LASER_DEFENSES: fireSound (GEO.CAT #8) |
| 9 | WAV | 11025 | 2.06 | craftWeapons/STR_PLASMA_BEAM_UC: sound (GEO.CAT #9); facilities/STR_PLASMA_DEFENSES: fireSound (GEO.CAT #9) |
| 10 | WAV | 11025 | 0.90 | facilities/STR_MISSILE_DEFENSES: hitSound (GEO.CAT #10); facilities/STR_LASER_DEFENSES: hitSound (GEO.CAT #10); facilities/STR_PLASMA_DEFENSES: hitSound (GEO.CAT #10); facilities/STR_FUSION_BALL_DEFENSES: hitSound (GEO.CAT #10) |
| 11 | WAV | 11025 | 2.03 |  |
| 12 | WAV | 11025 | 0.63 |  |
| 13 | WAV | 11025 | 2.26 |  |

### SOUND/SAMPLE2.CAT

Soundeffekte · 55 Einträge

- Hinweis: Enthält den logischen Soundsatz BATTLE.CAT

| # | Format | Rate | Sekunden | Verwendung |
|---:|---|---:|---:|---|
| 0 | WAV | 11025 | 0.99 | items/STR_HWP_ROCKETS: hitSound (BATTLE.CAT #0); items/STR_HWP_FUSION_BOMB: hitSound (BATTLE.CAT #0); items/STR_HC_HE_AMMO: hitSound (BATTLE.CAT #0); items/STR_HC_I_AMMO: hitSound (BATTLE.CAT #0); items/STR_AC_HE_AMMO: hitSound (BATTLE.CAT #0); items/STR_AC_I_AMMO: hitSound (BATTLE.CAT #0); items/STR_SMALL_ROCKET: hitSound (BATTLE.CAT #0); items/STR_LARGE_ROCKET: hitSound (BATTLE.CAT #0); items/STR_INCENDIARY_ROCKET: hitSound (BATTLE.CAT #0); items/STR_BLASTER_BOMB: hitSound (BATTLE.CAT #0); items/STR_STUN_BOMB: hitSound (BATTLE.CAT #0); units/STR_SNAKEMAN_SOLDIER: moveSound (BATTLE.CAT #0); units/STR_SNAKEMAN_NAVIGATOR: moveSound (BATTLE.CAT #0); units/STR_SNAKEMAN_ENGINEER: moveSound (BATTLE.CAT #0); units/STR_SNAKEMAN_LEADER: moveSound (BATTLE.CAT #0); units/STR_SNAKEMAN_COMMANDER: moveSound (BATTLE.CAT #0) |
| 1 | DOS-Rohdaten |  |  |  |
| 2 | WAV | 11025 | 1.09 |  |
| 3 | WAV | 11025 | 0.58 |  |
| 4 | WAV | 11025 | 0.81 | items/STR_PISTOL: fireSound (BATTLE.CAT #4); items/STR_RIFLE: fireSound (BATTLE.CAT #4) |
| 5 | WAV | 11025 | 1.88 |  |
| 6 | WAV | 11025 | 1.44 | units/STR_MUTON_SOLDIER: deathSound (BATTLE.CAT #6); units/STR_MUTON_NAVIGATOR: deathSound (BATTLE.CAT #6); units/STR_MUTON_ENGINEER: deathSound (BATTLE.CAT #6) |
| 7 | WAV | 11025 | 1.20 | units/STR_CELATID_TERRORIST: deathSound (BATTLE.CAT #7) |
| 8 | WAV | 11025 | 1.31 | units/STR_SNAKEMAN_SOLDIER: deathSound (BATTLE.CAT #8); units/STR_SNAKEMAN_NAVIGATOR: deathSound (BATTLE.CAT #8); units/STR_SNAKEMAN_ENGINEER: deathSound (BATTLE.CAT #8); units/STR_SNAKEMAN_LEADER: deathSound (BATTLE.CAT #8); units/STR_SNAKEMAN_COMMANDER: deathSound (BATTLE.CAT #8); units/STR_SILACOID_TERRORIST: deathSound (BATTLE.CAT #8) |
| 9 | WAV | 11025 | 1.09 | units/STR_CHRYSSALID_TERRORIST: deathSound (BATTLE.CAT #9); units/STR_ETHEREAL_SOLDIER: deathSound (BATTLE.CAT #9); units/STR_ETHEREAL_LEADER: deathSound (BATTLE.CAT #9); units/STR_ETHEREAL_COMMANDER: deathSound (BATTLE.CAT #9) |
| 10 | WAV | 11025 | 1.00 | units/STR_SECTOID_SOLDIER: deathSound (BATTLE.CAT #10); units/STR_SECTOID_ENGINEER: deathSound (BATTLE.CAT #10); units/STR_SECTOID_MEDIC: deathSound (BATTLE.CAT #10); units/STR_SECTOID_NAVIGATOR: deathSound (BATTLE.CAT #10); units/STR_SECTOID_LEADER: deathSound (BATTLE.CAT #10); units/STR_SECTOID_COMMANDER: deathSound (BATTLE.CAT #10); units/STR_FLOATER_SOLDIER: deathSound (BATTLE.CAT #10); units/STR_FLOATER_ENGINEER: deathSound (BATTLE.CAT #10); units/STR_FLOATER_NAVIGATOR: deathSound (BATTLE.CAT #10); units/STR_FLOATER_MEDIC: deathSound (BATTLE.CAT #10); units/STR_FLOATER_LEADER: deathSound (BATTLE.CAT #10); units/STR_FLOATER_COMMANDER: deathSound (BATTLE.CAT #10); units/STR_REAPER_TERRORIST: deathSound (BATTLE.CAT #10) |
| 11 | WAV | 11025 | 0.91 | items/STR_TANK_LASER_CANNON: fireSound (BATTLE.CAT #11); items/STR_LASER_PISTOL: fireSound (BATTLE.CAT #11); items/STR_LASER_RIFLE: fireSound (BATTLE.CAT #11); items/STR_HEAVY_LASER: fireSound (BATTLE.CAT #11) |
| 12 | WAV | 11025 | 1.33 | items/STR_TANK_CANNON: fireSound (BATTLE.CAT #12); items/STR_HEAVY_CANNON: fireSound (BATTLE.CAT #12); items/STR_AUTO_CANNON: fireSound (BATTLE.CAT #12) |
| 13 | WAV | 11025 | 0.86 | items/STR_HWP_CANNON_SHELLS: hitSound (BATTLE.CAT #13); items/STR_HC_AP_AMMO: hitSound (BATTLE.CAT #13); items/STR_AC_AP_AMMO: hitSound (BATTLE.CAT #13) |
| 14 | WAV | 11025 | 1.02 | units/STR_TANK_CANNON: moveSound (BATTLE.CAT #14); units/STR_TANK_ROCKET_LAUNCHER: moveSound (BATTLE.CAT #14); units/STR_TANK_LASER_CANNON: moveSound (BATTLE.CAT #14) |
| 15 | WAV | 11025 | 1.40 | units/STR_FLOATER_SOLDIER: moveSound (BATTLE.CAT #15); units/STR_FLOATER_ENGINEER: moveSound (BATTLE.CAT #15); units/STR_FLOATER_NAVIGATOR: moveSound (BATTLE.CAT #15); units/STR_FLOATER_MEDIC: moveSound (BATTLE.CAT #15); units/STR_FLOATER_LEADER: moveSound (BATTLE.CAT #15); units/STR_FLOATER_COMMANDER: moveSound (BATTLE.CAT #15) |
| 16 | WAV | 11025 | 0.99 | units/STR_CELATID_TERRORIST: moveSound (BATTLE.CAT #16) |
| 17 | WAV | 11025 | 0.31 |  |
| 18 | WAV | 11025 | 0.98 | items/STR_HOVERTANK_PLASMA: fireSound (BATTLE.CAT #18); items/STR_HEAVY_PLASMA: fireSound (BATTLE.CAT #18); items/STR_PLASMA_RIFLE: fireSound (BATTLE.CAT #18); items/STR_PLASMA_PISTOL: fireSound (BATTLE.CAT #18); items/CYBERDISC_WEAPON: fireSound (BATTLE.CAT #18); items/SECTOPOD_WEAPON: fireSound (BATTLE.CAT #18) |
| 19 | WAV | 11025 | 1.21 | items/STR_TANK_LASER_CANNON: hitSound (BATTLE.CAT #19); items/STR_HOVERTANK_PLASMA: hitSound (BATTLE.CAT #19); items/STR_LASER_PISTOL: hitSound (BATTLE.CAT #19); items/STR_LASER_RIFLE: hitSound (BATTLE.CAT #19); items/STR_HEAVY_LASER: hitSound (BATTLE.CAT #19); items/STR_HEAVY_PLASMA_CLIP: hitSound (BATTLE.CAT #19); items/STR_PLASMA_RIFLE_CLIP: hitSound (BATTLE.CAT #19); items/STR_PLASMA_PISTOL_CLIP: hitSound (BATTLE.CAT #19); items/CYBERDISC_WEAPON: hitSound (BATTLE.CAT #19); items/CELATID_WEAPON: hitSound (BATTLE.CAT #19); items/SECTOPOD_WEAPON: hitSound (BATTLE.CAT #19) |
| 20 | WAV | 11025 | 1.18 |  |
| 21 | WAV | 11025 | 0.77 |  |
| 22 | WAV | 11025 | 1.22 | items/STR_PISTOL_CLIP: hitSound (BATTLE.CAT #22); items/STR_RIFLE_CLIP: hitSound (BATTLE.CAT #22) |
| 23 | WAV | 11025 | 2.12 | units/STR_TANK_CANNON: deathSound (BATTLE.CAT #23); units/STR_TANK_ROCKET_LAUNCHER: deathSound (BATTLE.CAT #23); units/STR_TANK_LASER_CANNON: deathSound (BATTLE.CAT #23); units/STR_HOVERTANK_PLASMA: deathSound (BATTLE.CAT #23); units/STR_HOVERTANK_LAUNCHER: deathSound (BATTLE.CAT #23); units/STR_CYBERDISC_TERRORIST: deathSound (BATTLE.CAT #23); units/STR_SECTOPOD_TERRORIST: deathSound (BATTLE.CAT #23) |
| 24 | WAV | 11025 | 0.17 |  |
| 25 | WAV | 11025 | 0.24 |  |
| 26 | WAV | 11025 | 0.15 |  |
| 27 | WAV | 11025 | 0.19 |  |
| 28 | WAV | 11025 | 0.34 |  |
| 29 | WAV | 11025 | 0.27 |  |
| 30 | WAV | 11025 | 0.16 |  |
| 31 | WAV | 11025 | 0.20 |  |
| 32 | WAV | 11025 | 0.41 |  |
| 33 | WAV | 11025 | 0.32 |  |
| 34 | WAV | 11025 | 0.22 |  |
| 35 | WAV | 11025 | 0.23 |  |
| 36 | WAV | 11025 | 1.68 | items/STR_PSI_AMP: hitSound (BATTLE.CAT #36); items/ALIEN_PSI_WEAPON: hitSound (BATTLE.CAT #36) |
| 37 | WAV | 11025 | 2.07 | items/STR_MIND_PROBE: hitSound (BATTLE.CAT #37) |
| 38 | WAV | 11025 | 0.21 |  |
| 39 | WAV | 11025 | 0.26 |  |
| 40 | WAV | 11025 | 1.60 | units/STR_HOVERTANK_PLASMA: moveSound (BATTLE.CAT #40); units/STR_HOVERTANK_LAUNCHER: moveSound (BATTLE.CAT #40); units/STR_CYBERDISC_TERRORIST: moveSound (BATTLE.CAT #40) |
| 41 | WAV | 11025 | 0.89 | units/MALE_CIVILIAN: deathSound (BATTLE.CAT #41) |
| 42 | WAV | 11025 | 1.18 | units/MALE_CIVILIAN: deathSound (BATTLE.CAT #42) |
| 43 | WAV | 11025 | 0.76 | units/MALE_CIVILIAN: deathSound (BATTLE.CAT #43) |
| 44 | WAV | 11025 | 0.94 | units/FEMALE_CIVILIAN: deathSound (BATTLE.CAT #44) |
| 45 | WAV | 11025 | 0.75 | units/FEMALE_CIVILIAN: deathSound (BATTLE.CAT #45) |
| 46 | WAV | 11025 | 0.89 | units/FEMALE_CIVILIAN: deathSound (BATTLE.CAT #46) |
| 47 | WAV | 11025 | 1.79 | units/STR_SILACOID_TERRORIST: moveSound (BATTLE.CAT #47) |
| 48 | WAV | 11025 | 1.33 | items/REAPER_WEAPON: meleeSound (BATTLE.CAT #48); items/ZOMBIE_WEAPON: meleeSound (BATTLE.CAT #48) |
| 49 | WAV | 11025 | 1.33 | items/CHRYSSALID_WEAPON: meleeSound (BATTLE.CAT #49) |
| 50 | WAV | 11025 | 1.24 | items/SILACOID_WEAPON: meleeSound (BATTLE.CAT #50) |
| 51 | WAV | 11025 | 1.36 | items/CELATID_WEAPON: fireSound (BATTLE.CAT #51) |
| 52 | WAV | 11025 | 1.36 | items/STR_TANK_ROCKET_LAUNCHER: fireSound (BATTLE.CAT #52); items/STR_ROCKET_LAUNCHER: fireSound (BATTLE.CAT #52) |
| 53 | WAV | 11025 | 1.34 | items/STR_HOVERTANK_LAUNCHER: fireSound (BATTLE.CAT #53); items/STR_BLASTER_LAUNCHER: fireSound (BATTLE.CAT #53); items/STR_SMALL_LAUNCHER: fireSound (BATTLE.CAT #53) |
| 54 | WAV | 11025 | 1.16 | items/STR_STUN_ROD: meleeSound (BATTLE.CAT #54) |

### SOUND/SAMPLE3.CAT

Zusätzliche Soundeffekte · 32 Einträge

| # | Format | Rate | Sekunden | Verwendung |
|---:|---|---:|---:|---|
| 0 | WAV | 11025 | 0.01 |  |
| 1 | WAV | 11025 | 0.01 |  |
| 2 | WAV | 11025 | 4.77 |  |
| 3 | WAV | 11025 | 2.58 |  |
| 4 | WAV | 11025 | 2.12 |  |
| 5 | WAV | 11025 | 0.98 |  |
| 6 | WAV | 11025 | 0.71 |  |
| 7 | WAV | 11025 | 1.36 |  |
| 8 | WAV | 11025 | 0.75 |  |
| 9 | WAV | 11025 | 1.09 |  |
| 10 | WAV | 11025 | 0.01 |  |
| 11 | WAV | 11025 | 0.95 |  |
| 12 | WAV | 11025 | 5.21 |  |
| 13 | WAV | 11025 | 0.49 |  |
| 14 | WAV | 11025 | 1.10 |  |
| 15 | WAV | 11025 | 1.21 |  |
| 16 | WAV | 11025 | 0.01 |  |
| 17 | WAV | 11025 | 0.01 |  |
| 18 | WAV | 11025 | 3.39 |  |
| 19 | WAV | 11025 | 1.45 |  |
| 20 | WAV | 11025 | 3.39 |  |
| 21 | WAV | 11025 | 0.77 |  |
| 22 | WAV | 11025 | 0.01 |  |
| 23 | WAV | 11025 | 0.81 |  |
| 24 | WAV | 11025 | 1.46 |  |
| 25 | WAV | 11025 | 0.01 |  |
| 26 | WAV | 11025 | 1.20 |  |
| 27 | WAV | 11025 | 1.31 |  |
| 28 | WAV | 11025 | 1.09 |  |
| 29 | WAV | 11025 | 0.01 |  |
| 30 | WAV | 11025 | 1.10 |  |
| 31 | WAV | 11025 | 0.01 |  |

### SOUND/SOUND1.CAT

Soundeffekte · 55 Einträge

| # | Format | Rate | Sekunden | Verwendung |
|---:|---|---:|---:|---|
| 0 | DOS-Rohdaten |  |  |  |
| 1 | leer |  |  |  |
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
| 18 | DOS-Rohdaten |  |  |  |
| 19 | DOS-Rohdaten |  |  |  |
| 20 | DOS-Rohdaten |  |  |  |
| 21 | DOS-Rohdaten |  |  |  |
| 22 | DOS-Rohdaten |  |  |  |
| 23 | DOS-Rohdaten |  |  |  |
| 24 | DOS-Rohdaten |  |  |  |
| 25 | DOS-Rohdaten |  |  |  |
| 26 | DOS-Rohdaten |  |  |  |
| 27 | DOS-Rohdaten |  |  |  |
| 28 | DOS-Rohdaten |  |  |  |
| 29 | DOS-Rohdaten |  |  |  |
| 30 | DOS-Rohdaten |  |  |  |
| 31 | DOS-Rohdaten |  |  |  |
| 32 | DOS-Rohdaten |  |  |  |
| 33 | DOS-Rohdaten |  |  |  |
| 34 | DOS-Rohdaten |  |  |  |
| 35 | DOS-Rohdaten |  |  |  |
| 36 | DOS-Rohdaten |  |  |  |
| 37 | DOS-Rohdaten |  |  |  |
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

### SOUND/SOUND2.CAT

Soundeffekte · 14 Einträge

| # | Format | Rate | Sekunden | Verwendung |
|---:|---|---:|---:|---|
| 0 | DOS-Rohdaten |  |  |  |
| 1 | DOS-Rohdaten |  |  |  |
| 2 | DOS-Rohdaten |  |  |  |
| 3 | DOS-Rohdaten |  |  |  |
| 4 | DOS-Rohdaten |  |  |  |
| 5 | DOS-Rohdaten |  |  |  |
| 6 | leer |  |  |  |
| 7 | DOS-Rohdaten |  |  |  |
| 8 | DOS-Rohdaten |  |  |  |
| 9 | DOS-Rohdaten |  |  |  |
| 10 | DOS-Rohdaten |  |  |  |
| 11 | DOS-Rohdaten |  |  |  |
| 12 | DOS-Rohdaten |  |  |  |
| 13 | DOS-Rohdaten |  |  |  |

## Musik, Videos, Paletten, Schriften, Texte, Daten

| Datei | Kategorie | Format | Details | Zweck | Verwendung |
|---|---|---|---|---|---|
| GEODATA/BACKPALS.DAT | palette | DAT | 8 Einträge, 384 Bytes, Nur Struktur erfasst; Farbwerte werden nicht exportiert. | Hintergrund-Farbverläufe für Fenster |  |
| GEODATA/BIGLETS.DAT | font | DAT | 16×16, 173 Einträge, 44288 Bytes | Große Bitmap-Schrift des Originals (ACOM nutzt eigene Schriften) |  |
| GEODATA/COS.DAT | data | DAT | 5762 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/ENGLISH.DAT | text | DAT | 46601 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/ENGLISH2.DAT | text | DAT | 2272 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/FRENCH.DAT | text | DAT | 52672 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/FRENCH2.DAT | text | DAT | 2724 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/GERMAN.DAT | text | DAT | 53587 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/GERMAN2.DAT | text | DAT | 2695 Bytes | Spieltexte (Inhalt wird nicht ausgewertet) |  |
| GEODATA/LANG1.DAT | data | DAT | 9856 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/LANG2.DAT | data | DAT | 9856 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/LOFTEMPS.DAT | data | DAT | 112 Einträge, 3584 Bytes | Kollisions-/Sichtlinien-Vorlagen der Geländekacheln (16×16 Bit je Ebene) |  |
| GEODATA/OBDATA.DAT | data | DAT | 4320 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/PALETTES.DAT | palette | DAT | 5 Einträge, 3870 Bytes, Nur Struktur erfasst; Farbwerte werden nicht exportiert. | Paletten (Geoscape, Basis, Grafiken, Enzyklopädie, Kampf) |  |
| GEODATA/SINE.DAT | data | DAT | 5762 Bytes | Daten (nicht ausgewertet) |  |
| GEODATA/SMALLSET.DAT | font | DAT | 8×9, 173 Einträge, 12456 Bytes | Kleine Bitmap-Schrift des Originals (ACOM nutzt eigene Schriften) |  |
| GEODATA/WORLD.DAT | data | DAT | 13320 Bytes | Weltkarte als Polygone (Landmassen) – durch freie Geodaten ersetzen |  |
| GEOGRAPH/DP_PREFS | unknown |  | 324 Bytes | Nicht ausgewertet |  |
| MAPS/GEODATA.DAT | data | DAT | 126 Bytes | Daten (nicht ausgewertet) |  |
| README.TXT | text | TXT | 551 Bytes | Text (Inhalt wird nicht ausgewertet) |  |
| SOUND/ADLIB.CAT | music | CAT | 22 Einträge, 128686 Bytes | Musik (AdLib) |  |
| SOUND/AINTRO.CAT | music | CAT | 3 Einträge, 13839 Bytes | Soundeffekte |  |
| SOUND/GM.CAT | music | CAT | 22 Einträge, 134033 Bytes | Musik (General MIDI) | musics/GMDEFEND: catPos 3; musics/GMENBASE: catPos 6; musics/GMGEO1: catPos 0; musics/GMGEO2: catPos 18; musics/GMINTER: catPos 2; musics/GMINTRO1: catPos 19; musics/GMINTRO2: catPos 20; musics/GMINTRO3: catPos 21; musics/GMLOSE: catPos 10; musics/GMMARS: catPos 9; musics/GMNEWMAR: catPos 8; musics/GMSTORY: catPos 12; musics/GMTACTIC: catPos 17; musics/GMWIN: catPos 11 |
| SOUND/GMDEFEND.MID | sounds | MID | 5812 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMENBASE.MID | sounds | MID | 12928 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMGEO1.MID | sounds | MID | 51403 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMGEO2.MID | sounds | MID | 82573 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMINTER.MID | sounds | MID | 24503 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMINTRO1.MID | sounds | MID | 2148 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMINTRO2.MID | sounds | MID | 10736 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMINTRO3.MID | sounds | MID | 20903 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMLOSE.MID | sounds | MID | 13869 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMMARS.MID | sounds | MID | 11668 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMNEWMAR.MID | sounds | MID | 10807 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMSTORY.MID | sounds | MID | 6301 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMTACTIC.MID | sounds | MID | 20465 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/GMWIN.MID | sounds | MID | 18306 Bytes | Audiodatei (Inhalt wird nicht ausgewertet) |  |
| SOUND/LAPC1.PAT | unknown | PAT | 1068 Bytes | Nicht ausgewertet |  |
| SOUND/MUSIC.COM | unknown | COM | 6109 Bytes | Nicht ausgewertet |  |
| SOUND/RINTRO.CAT | music | CAT | 3 Einträge, 14429 Bytes | Soundeffekte |  |
| SOUND/ROLAND.CAT | music | CAT | 19 Einträge, 93853 Bytes | Musik (Roland MT-32) |  |
| SOUND/SNDEND.EXE | unknown | EXE | 7756 Bytes | Nicht ausgewertet |  |
| SOUND/SNDSTART.EXE | unknown | EXE | 10970 Bytes | Nicht ausgewertet |  |
| SOUND/SOUND.CFG | unknown | CFG | 18 Bytes | Nicht ausgewertet |  |
| SOUND/SOUNDRV.COM | unknown | COM | 4560 Bytes | Nicht ausgewertet |  |
| SOUND/VECTOR.COM | unknown | COM | 400 Bytes | Nicht ausgewertet |  |
| UFOGRAPH/DP_PREFS | unknown |  | 324 Bytes | Nicht ausgewertet |  |
| UFOINTRO/UFOINT.FLI | video | FLI | 320×200, 890 Frames, 2474590 Bytes | Video-Sequenz |  |

## Verweise ohne Originaldatei

Ruleset-Verweise auf Dateien, die in den Originaldaten fehlen (meist vom Repo/Mod bereitgestellt):

- UFOINTRO/UFOINT.FLI ← cutscenes/intro: videos

