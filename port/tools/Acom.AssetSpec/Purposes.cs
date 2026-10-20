/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This file is part of ACOM, a C# port of OpenXcom Extended.
 *
 * ACOM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ACOM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Acom.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace Acom.AssetSpec;

/// <summary>
/// Functional description of each original file: what the engine uses it for.
/// Describes the role only, never the look of the original content.
/// </summary>
internal static class Purposes
{
	private static readonly Dictionary<string, string> s_exact = new(StringComparer.Ordinal)
	{
		["UNITS/BIGOBS.PCK"] = "Gegenstände im Inventar (große Darstellung, 32×48)",
		["UNITS/FLOOROB.PCK"] = "Gegenstände auf dem Boden der Kampfkarte",
		["UNITS/HANDOB.PCK"] = "Gegenstände in der Hand von Einheiten, je 8 Blickrichtungen",
		["GEOGRAPH/BASEBITS.PCK"] = "Basisansicht: Gebäude-Bausteine, Fahrzeuge im Hangar, Rangabzeichen, Fahrzeugwaffen",
		["GEOGRAPH/INTICON.PCK"] = "Geoscape/Abfangen: Fahrzeug- und Waffensymbole",
		["GEOGRAPH/TEXTURE.DAT"] = "Globus-Oberflächentexturen (32×32)",
		["GEODATA/SCANG.DAT"] = "Kleine Markierungen auf dem Globus (4×4)",
		["GEODATA/INTERWIN.DAT"] = "Abfangfenster: Vorschaubilder der Flugobjekte, untereinander gestapelt",
		["GEODATA/PALETTES.DAT"] = "Paletten (Geoscape, Basis, Grafiken, Enzyklopädie, Kampf)",
		["GEODATA/BACKPALS.DAT"] = "Hintergrund-Farbverläufe für Fenster",
		["GEODATA/WORLD.DAT"] = "Weltkarte als Polygone (Landmassen) – durch freie Geodaten ersetzen",
		["GEODATA/LOFTEMPS.DAT"] = "Kollisions-/Sichtlinien-Vorlagen der Geländekacheln (16×16 Bit je Ebene)",
		["GEODATA/BIGLETS.DAT"] = "Große Bitmap-Schrift des Originals (ACOM nutzt eigene Schriften)",
		["GEODATA/SMALLSET.DAT"] = "Kleine Bitmap-Schrift des Originals (ACOM nutzt eigene Schriften)",
		["UFOGRAPH/CURSOR.PCK"] = "Kampfkarten-Cursor und Zielmarkierungen",
		["UFOGRAPH/SMOKE.PCK"] = "Rauch, Feuer, Treffereffekte, Rangabzeichen im Kampf",
		["UFOGRAPH/HIT.PCK"] = "Nahkampf-/Treffer-Animationen",
		["UFOGRAPH/X1.PCK"] = "Große Explosion (128×64)",
		["UFOGRAPH/SPICONS.DAT"] = "Kleine Symbole (Spezialaktionen, Fähigkeiten, 32×24)",
		["UFOGRAPH/MEDIBITS.DAT"] = "Medikit-Fenster: Körperteile (52×58)",
		["UFOGRAPH/DETBLOB.DAT"] = "Bewegungsscanner: Signal-Punkte (16×16)",
		["UFOGRAPH/TAC00.SCR"] = "Kampf-Oberfläche (Bedienleiste)",
		["UFOGRAPH/TAC01.SCR"] = "Kampf-Oberfläche (Variante)",
		["UFOGRAPH/ICONS.PCK"] = "Kampf-Bedienleiste (Vollbild-Grafik)",
		["UFOGRAPH/DETBORD.PCK"] = "Fensterrahmen: Einheitendetails",
		["UFOGRAPH/DETBORD2.PCK"] = "Fensterrahmen: Einheitendetails (Variante)",
		["UFOGRAPH/MEDIBORD.PCK"] = "Fensterrahmen: Medikit",
		["UFOGRAPH/SCANBORD.PCK"] = "Fensterrahmen: Bewegungsscanner",
		["UFOGRAPH/UNIBORD.PCK"] = "Fensterrahmen: Einheiteninfo",
		["TERRAIN/BLANKS.PCK"] = "Leere/Platzhalter-Kacheln",
		["SOUND/GM.CAT"] = "Musik (General MIDI)",
		["SOUND/ADLIB.CAT"] = "Musik (AdLib)",
		["SOUND/ROLAND.CAT"] = "Musik (Roland MT-32)",
		["SOUND/INTRO.CAT"] = "Intro-Soundeffekte",
		["SOUND/SAMPLE3.CAT"] = "Zusätzliche Soundeffekte",
	};

	/// <summary>Gets the purpose of a file (empty if unknown).</summary>
	public static string For(string path)
	{
		if (s_exact.TryGetValue(path, out var purpose))
			return purpose;
		string name = path[(path.LastIndexOf('/') + 1)..];
		string dir = path.Contains('/', StringComparison.Ordinal) ? path[..path.IndexOf('/', StringComparison.Ordinal)] : "";
		return (dir, Path.GetExtension(name)) switch
		{
			("UNITS", ".PCK") => "Einheiten-Spritesheet (Animationsphasen × Blickrichtungen, Aufbau siehe drawingRoutine der Rüstung)",
			("TERRAIN", ".PCK") => "Geländekacheln (Boden, Wände, Objekte) eines Geländesatzes",
			("UFOGRAPH", ".SPK") when name.StartsWith("MAN_", StringComparison.Ordinal) => "Inventar-Ganzkörperbild einer Rüstung (Geschlecht/Aussehen-Variante)",
			("UFOGRAPH", ".SPK") when name.StartsWith("UP", StringComparison.Ordinal) => "Enzyklopädie-Bild (Kampfthemen)",
			("UFOGRAPH", ".BDY") => "Vollbild-Grafik (Kampf), Variante im BDY-Format",
			("UFOGRAPH", ".LBM") => "Kampf-Palette/Grafik im LBM-Format",
			("GEOGRAPH", ".SPK") when name.StartsWith("UP", StringComparison.Ordinal) => "Enzyklopädie-Bild",
			("GEOGRAPH", ".SCR") or ("GEOGRAPH", ".SPK") or ("GEOGRAPH", ".BDY") => "Vollbild-Grafik (Hintergrund, Menü, Ereignis)",
			("GEOGRAPH", ".LBM") => "Vollbild-Grafik im LBM-Format",
			("UFOINTRO", _) or ("FLOP_INT", _) or ("ANIMS", ".VID") or ("ANIMS", ".FLI") or ("ANIMS", ".FLC") => "Video-Sequenz",
			("SOUND", ".CAT") => "Soundeffekte",
			_ => "",
		};
	}
}
