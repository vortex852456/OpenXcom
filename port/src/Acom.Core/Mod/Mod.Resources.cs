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
// Ported from: src/Mod/Mod.h, src/Mod/Mod.cpp
// Port status: partial (missing: script hooks)
//
// Resource part of Mod: fonts, surfaces, palettes, sounds, music, transparency LUTs and the vanilla resource loading.
#nullable disable

using System.Text;

namespace Acom;

public partial class Mod
{
	/// <summary>
	/// Silent music. Deviation: created on first use instead of in the constructor, so that a Mod
	/// can exist without the audio backend.
	/// </summary>
	private Music MuteMusic => _muteMusic ??= new Music();

	/// <summary>Silent sound, created on first use (see <see cref="MuteMusic"/>).</summary>
	private Sound MuteSound => _muteSound ??= new Sound();

	/// <summary>Returns a specific font from the mod.</summary>
	/// <param name="name">Name of the font.</param>
	/// <param name="error">Throw an error if not found.</param>
	public Font GetFont(string name, bool error = true) => GetRule(name, "Font", _fonts, error);

	/// <summary>Loads any extra sprites associated to a surface when it's first requested.</summary>
	/// <param name="name">Surface name.</param>
	private void LazyLoadSurface(string name)
	{
		if (Options.lazyLoadResources)
		{
			if (_extraSprites.TryGetValue(name, out var list))
			{
				foreach (var extraSprites in list)
				{
					LoadExtraSprite(extraSprites);
				}
			}
		}
	}

	/// <summary>Returns a specific surface from the mod.</summary>
	public Surface GetSurface(string name, bool error = true)
	{
		LazyLoadSurface(name);
		return GetRule(name, "Sprite", _surfaces, error);
	}

	/// <summary>Returns a specific surface set from the mod.</summary>
	public SurfaceSet GetSurfaceSet(string name, bool error = true)
	{
		LazyLoadSurface(name);
		return GetRule(name, "Sprite Set", _sets, error);
	}

	/// <summary>Returns a specific music from the mod.</summary>
	public Music GetMusic(string name, bool error = true)
	{
		if (Options.mute)
		{
			return MuteMusic;
		}
		else
		{
			return GetRule(name, "Music", _musics, error);
		}
	}

	/// <summary>Returns the list of all music tracks provided by the mod.</summary>
	public SortedDictionary<string, Music> MusicTrackList => _musics;

	/// <summary>Name of the track that is playing.</summary>
	public string CurrentMusicTrack
	{
		get => _currentMusicTrack;
		set => _currentMusicTrack = value;
	}

	/// <summary>Returns a random music from the mod.</summary>
	/// <param name="name">Name of the music to pick from.</param>
	private Music GetRandomMusic(string name)
	{
		if (Options.mute)
		{
			return MuteMusic;
		}
		else
		{
			var music = new List<Music>();
			foreach (var pair in _musics)
			{
				if (pair.Key.Contains(name, StringComparison.Ordinal))
				{
					music.Add(pair.Value);
				}
			}
			if (music.Count == 0)
			{
				return MuteMusic;
			}
			else
			{
				return music[Rng.Seedless(0, music.Count - 1)];
			}
		}
	}

	/// <summary>Plays the specified track if it's not already playing.</summary>
	/// <param name="name">Name of the music.</param>
	/// <param name="id">Id of the music, 0 for random.</param>
	public void PlayMusic(string name, int id = 0)
	{
		if (!Options.mute && _playingMusic != name)
		{
			int loop = -1;
			// hacks
			if (!Options.musicAlwaysLoop && (name == "GMSTORY" || name == "GMWIN" || name == "GMLOSE"))
			{
				loop = 0;
			}

			Music music;
			if (id == 0)
			{
				music = GetRandomMusic(name);
			}
			else
			{
				music = GetMusic($"{name}{id}");
			}
			music.Play(loop);
			// _muteMusic is null if it was never needed, then no track can be it
			if (!ReferenceEquals(music, _muteMusic))
			{
				_playingMusic = name;
				foreach (var item in _musics)
				{
					if (ReferenceEquals(item.Value, music))
					{
						CurrentMusicTrack = item.Key;
						break;
					}
				}
			}
			Logger.Verbose("Mod::playMusic('" + name + "'): playing " + _playingMusic);
		}
	}

	/// <summary>Returns a specific sound set from the mod. Private, use <see cref="GetSound"/> instead.</summary>
	private SoundSet GetSoundSet(string name, bool error = true) => GetRule(name, "Sound Set", _sounds, error);

	/// <summary>Returns a specific sound from the mod.</summary>
	/// <param name="set">Name of the sound set.</param>
	/// <param name="sound">ID of the sound.</param>
	public Sound GetSound(string set, int sound)
	{
		if (Options.mute)
		{
			return MuteSound;
		}
		else
		{
			SoundSet ss = GetSoundSet(set, false);
			if (ss != null)
			{
				Sound s = ss.GetSound(sound);
				if (s == null)
				{
					Logger.Error($"Sound {sound} in {set} not found");
					return MuteSound;
				}
				return s;
			}
			else
			{
				Logger.Error("SoundSet " + set + " not found");
				return MuteSound;
			}
		}
	}

	/// <summary>Gets all palettes.</summary>
	public SortedDictionary<string, Palette> Palettes => _palettes;

	/// <summary>Returns a specific palette from the mod.</summary>
	public Palette GetPalette(string name, bool error = true) => GetRule(name, "Palette", _palettes, error);

	/// <summary>Returns the list of voxeldata in the mod.</summary>
	public List<ushort> VoxelData => _voxelData;

	/// <summary>Returns a specific sound from either the land or underwater sound set.</summary>
	/// <param name="depth">The depth of the battlescape.</param>
	/// <param name="sound">ID of the sound.</param>
	public Sound GetSoundByDepth(uint depth, uint sound)
	{
		if (depth == 0 || _disableUnderwaterSounds)
			return GetSound("BATTLE.CAT", (int)sound);
		else
			return GetSound("BATTLE2.CAT", (int)sound);
	}

	/// <summary>Returns the list of color LUTs in the mod.</summary>
	public List<List<byte>> LUTs => _transparencyLUTs;

	// ---- file helpers (the C++ loaders take VFS paths, the ported ones take streams) ----

	private static void LoadPaletteDat(Palette palette, string path, int ncolors, int offset = 0)
	{
		using var stream = FileMap.GetStream(path);
		palette.LoadDat(stream, ncolors, offset);
	}

	private static void LoadScr(Surface surface, string path)
	{
		using var stream = FileMap.GetStream(path);
		surface.LoadScr(stream);
	}

	private static void LoadSpk(Surface surface, string path)
	{
		using var stream = FileMap.GetStream(path);
		surface.LoadSpk(stream);
	}

	private static void LoadBdy(Surface surface, string path)
	{
		using var stream = FileMap.GetStream(path);
		surface.LoadBdy(stream);
	}

	private static void LoadPck(SurfaceSet set, string pck, string tab)
	{
		using var pckStream = FileMap.GetStream(pck);
		using var tabStream = FileMap.GetStream(tab);
		set.LoadPck(pckStream, tabStream);
	}

	private static void LoadDat(SurfaceSet set, string path)
	{
		using var stream = FileMap.GetStream(path);
		set.LoadDat(stream);
	}

	private static CatFile OpenCatFile(string path)
	{
		using var stream = FileMap.GetStream(path);
		return new CatFile(stream, path);
	}

	/// <summary>C <c>toupper</c> on every character (ASCII only, like the "C" locale).</summary>
	private static string ToUpperAscii(string s) => string.Create(s.Length, s, (span, src) =>
	{
		for (int i = 0; i < src.Length; ++i)
			span[i] = char.IsAsciiLetterLower(src[i]) ? (char)(src[i] - 32) : src[i];
	});

	/// <summary>C <c>tolower</c> on every character (ASCII only, like the "C" locale).</summary>
	private static string ToLowerAscii(string s) => string.Create(s.Length, s, (span, src) =>
	{
		for (int i = 0; i < src.Length; ++i)
			span[i] = char.IsAsciiLetterUpper(src[i]) ? (char)(src[i] + 32) : src[i];
	});

	// ---- recolor functions used by the "battleHairBleach" fix ----

	private const byte ShadeMax = 15;

	/// <summary>Recolor class used in UFO.</summary>
	private static class HairXCOM1
	{
		public const byte Hair = 9 << 4;
		public const byte Face = 6 << 4;

		public static void Func(ref byte src, byte cutoff)
		{
			if (src > cutoff && src <= Face + ShadeMax)
			{
				src = (byte)(Hair + (src & ShadeMax) - 6); //make hair color like male in xcom_0.pck
			}
		}
	}

	/// <summary>Recolor class used in TFTD.</summary>
	private static class HairXCOM2
	{
		public const byte ManHairColor = 4 << 4;
		public const byte WomanHairColor = 1 << 4;

		public static void Func(ref byte src)
		{
			if (src >= WomanHairColor && src <= WomanHairColor + ShadeMax)
			{
				src = (byte)(ManHairColor + (src & ShadeMax));
			}
		}
	}

	/// <summary>Recolor class used in TFTD.</summary>
	private static class FaceXCOM2
	{
		public const byte FaceColor = 10 << 4;
		public const byte PinkColor = 14 << 4;

		public static void Func(ref byte src)
		{
			if (src >= FaceColor && src <= FaceColor + ShadeMax)
			{
				src = (byte)(PinkColor + (src & ShadeMax));
			}
		}
	}

	/// <summary>Recolor class used in TFTD.</summary>
	private static class BodyXCOM2
	{
		public const byte IonArmorColor = 8 << 4;

		public static void Func(ref byte src)
		{
			if (src == 153)
			{
				src = IonArmorColor + 12;
			}
			else if (src == 151)
			{
				src = IonArmorColor + 10;
			}
			else if (src == 148)
			{
				src = IonArmorColor + 4;
			}
			else if (src == 147)
			{
				src = IonArmorColor + 2;
			}
			else if (src >= HairXCOM2.WomanHairColor && src <= HairXCOM2.WomanHairColor + ShadeMax)
			{
				src = (byte)(IonArmorColor + (src & ShadeMax));
			}
		}
	}

	/// <summary>Recolor class used in TFTD.</summary>
	private static class FallXCOM2
	{
		public const byte RoguePixel = 151;

		public static void Func(ref byte src)
		{
			if (src == RoguePixel)
			{
				src = (byte)(FaceXCOM2.PinkColor + (src & ShadeMax) + 2);
			}
			else if (src >= BodyXCOM2.IonArmorColor && src <= BodyXCOM2.IonArmorColor + ShadeMax)
			{
				src = (byte)(FaceXCOM2.PinkColor + (src & ShadeMax));
			}
		}
	}

	private delegate void PixelFunc(ref byte src);

	/// <summary>
	/// C++ <c>ShaderDraw&lt;F&gt;(ShaderMove&lt;Uint8&gt;(surf))</c> with a domain: applies the function to every
	/// pixel of the rectangle [begX, endX) x [begY, endY), clipped to the surface.
	/// </summary>
	private static void RecolorSurface(Surface surf, int begX, int endX, int begY, int endY, PixelFunc func)
	{
		begX = Math.Max(begX, 0);
		begY = Math.Max(begY, 0);
		endX = Math.Min(endX, surf.Width);
		endY = Math.Min(endY, surf.Height);
		for (int y = begY; y < endY; ++y)
		{
			for (int x = begX; x < endX; ++x)
			{
				byte p = surf.GetPixel(x, y);
				func(ref p);
				surf.SetPixel(x, y, p);
			}
		}
	}

	private static void RecolorSurface(Surface surf, PixelFunc func) => RecolorSurface(surf, 0, surf.Width, 0, surf.Height, func);

	/// <summary>Loads the vanilla resources required by the game.</summary>
	private void LoadVanillaResources()
	{
		// Create Geoscape surface
		_sets["GlobeMarkers"] = new SurfaceSet(3, 3);
		// dummy resources, that need to be defined in order for mod loading to work correctly
		_sets["CustomArmorPreviews"] = new SurfaceSet(12, 20);
		_sets["CustomItemPreviews"] = new SurfaceSet(12, 20);
		_sets["TinyRanks"] = new SurfaceSet(7, 7);
		_sets["Touch"] = new SurfaceSet(32, 24);

		// Load palettes
		string[] pal = ["PAL_GEOSCAPE", "PAL_BASESCAPE", "PAL_GRAPHS", "PAL_UFOPAEDIA", "PAL_BATTLEPEDIA"];
		for (int i = 0; i < pal.Length; ++i)
		{
			string s = "GEODATA/PALETTES.DAT";
			_palettes[pal[i]] = new Palette();
			LoadPaletteDat(_palettes[pal[i]], s, 256, Palette.PalOffset(i));
		}
		{
			string s1 = "GEODATA/BACKPALS.DAT";
			string s2 = "BACKPALS.DAT";
			_palettes[s2] = new Palette();
			LoadPaletteDat(_palettes[s2], s1, 128);
		}

		// Correct Battlescape palette
		{
			string s1 = "GEODATA/PALETTES.DAT";
			string s2 = "PAL_BATTLESCAPE";
			_palettes[s2] = new Palette();
			LoadPaletteDat(_palettes[s2], s1, 256, Palette.PalOffset(4));

			// Last 16 colors are a greyish gradient
			Rgba[] gradient = [new(140, 152, 148, 255),
				new(132, 136, 140, 255),
				new(116, 124, 132, 255),
				new(108, 116, 124, 255),
				new(92, 104, 108, 255),
				new(84, 92, 100, 255),
				new(76, 80, 92, 255),
				new(56, 68, 84, 255),
				new(48, 56, 68, 255),
				new(40, 48, 56, 255),
				new(32, 36, 48, 255),
				new(24, 28, 32, 255),
				new(16, 20, 24, 255),
				new(8, 12, 16, 255),
				new(3, 4, 8, 255),
				new(3, 3, 6, 255)];
			for (int i = 0; i < gradient.Length; ++i)
			{
				_palettes[s2].Colors[Palette.BackPos + 16 + i] = gradient[i];
			}
		}

		// Load surfaces
		{
			string s1 = "GEODATA/INTERWIN.DAT";
			string s2 = "INTERWIN.DAT";
			_surfaces[s2] = new Surface(160, 600);
			LoadScr(_surfaces[s2], s1);
		}

		var geographFiles = FileMap.GetVFolderContents("GEOGRAPH");
		var scrs = FileMap.FilterFiles(geographFiles, "SCR");
		foreach (var name in scrs)
		{
			string fname = ToUpperAscii(name);
			_surfaces[fname] = new Surface(320, 200);
			LoadScr(_surfaces[fname], "GEOGRAPH/" + fname);
		}
		var bdys = FileMap.FilterFiles(geographFiles, "BDY");
		foreach (var name in bdys)
		{
			string fname = ToUpperAscii(name);
			_surfaces[fname] = new Surface(320, 200);
			LoadBdy(_surfaces[fname], "GEOGRAPH/" + fname);
		}

		var spks = FileMap.FilterFiles(geographFiles, "SPK");
		foreach (var name in spks)
		{
			string fname = ToUpperAscii(name);
			_surfaces[fname] = new Surface(320, 200);
			LoadSpk(_surfaces[fname], "GEOGRAPH/" + fname);
		}

		// Load surface sets
		string[] sets = ["BASEBITS.PCK", "INTICON.PCK", "TEXTURE.DAT"];

		for (int i = 0; i < sets.Length; ++i)
		{
			string s = "GEOGRAPH/" + sets[i];

			string ext = sets[i][(sets[i].LastIndexOf('.') + 1)..];
			if (ext == "PCK")
			{
				string tab = CrossPlatform.NoExt(sets[i]) + ".TAB";
				string s2 = "GEOGRAPH/" + tab;
				_sets[sets[i]] = new SurfaceSet(32, 40);
				LoadPck(_sets[sets[i]], s, s2);
			}
			else
			{
				_sets[sets[i]] = new SurfaceSet(32, 32);
				LoadDat(_sets[sets[i]], s);
			}
		}
		{
			string s1 = "GEODATA/SCANG.DAT";
			string s2 = "SCANG.DAT";
			_sets[s2] = new SurfaceSet(4, 4);
			LoadDat(_sets[s2], s1);
		}

		// construct sound sets
		_sounds["GEO.CAT"] = new SoundSet();
		_sounds["BATTLE.CAT"] = new SoundSet();
		_sounds["BATTLE2.CAT"] = new SoundSet();
		_sounds["SAMPLE3.CAT"] = new SoundSet();
		_sounds["INTRO.CAT"] = new SoundSet();

		if (!Options.mute) // TBD: ain't it wrong? can Options::mute be reset without a reload?
		{
			// Load sounds
			var contents = FileMap.GetVFolderContents("SOUND");
			var soundFiles = FileMap.FilterFiles(contents, "CAT");
			if (_soundDefs.Count == 0)
			{
				string[] catsId = ["GEO.CAT", "BATTLE.CAT"];
				string[] catsDos = ["SOUND2.CAT", "SOUND1.CAT"];
				string[] catsWin = ["SAMPLE.CAT", "SAMPLE2.CAT"];

				// Try the preferred format first, otherwise use the default priority
				string[][] cats = [null, catsWin, catsDos];
				if (Options.preferredSound == SoundFormat.SOUND_14)
					cats[0] = catsWin;
				else if (Options.preferredSound == SoundFormat.SOUND_10)
					cats[0] = catsDos;

				Options.currentSound = SoundFormat.SOUND_AUTO;
				for (int i = 0; i < catsId.Length; ++i)
				{
					SoundSet sound = _sounds[catsId[i]];
					for (int j = 0; j < cats.Length; ++j)
					{
						bool wav = true;
						if (cats[j] == null)
							continue;
						else if (cats[j] == catsDos)
							wav = false;
						string fname = "SOUND/" + cats[j][i];
						if (FileMap.FileExists(fname))
						{
							Logger.Verbose(catsId[i] + ": loading sound " + fname);
							CatFile catfile = OpenCatFile(fname);
							sound.LoadCat(catfile);
							Options.currentSound = wav ? SoundFormat.SOUND_14 : SoundFormat.SOUND_10;
							break;
						}
						else
						{
							Logger.Verbose(catsId[i] + ": sound file not found: " + fname);
						}
					}
					if (sound.TotalSounds == 0)
					{
						Logger.Error(catsId[i] + " not found: " + catsWin[i] + " or " + catsDos[i] + " required");
					}
				}
			}
			else
			{
				// we're here if and only if this is the first mod loading
				// and it got soundDefs in the ruleset, which basically means it's xcom2.
				foreach (var i in _soundDefs)
				{
					if (!_sounds.TryGetValue(i.Key, out var soundSet))
					{
						soundSet = new SoundSet();
						_sounds[i.Key] = soundSet;
						Logger.Verbose("TFTD: adding soundset" + i.Key);
					}
					string fname = "SOUND/" + i.Value.CATFile;
					if (FileMap.FileExists(fname))
					{
						CatFile catfile = OpenCatFile(fname);
						foreach (int j in i.Value.SoundList)
						{
							soundSet.LoadCatByIndex(catfile, j, true);
							Logger.Verbose($"TFTD: adding sound {j} to {i.Key}");
						}
					}
					else
					{
						Logger.Error("TFTD sound file not found:" + fname);
					}
				}
			}

			if (soundFiles.Contains("intro.cat"))
			{
				var catfile = OpenCatFile("SOUND/INTRO.CAT");
				_sounds["INTRO.CAT"].LoadCat(catfile);
			}

			if (soundFiles.Contains("sample3.cat"))
			{
				var catfile = OpenCatFile("SOUND/SAMPLE3.CAT");
				_sounds["SAMPLE3.CAT"].LoadCat(catfile);
			}
		}

		LoadBattlescapeResources(); // TODO load this at battlescape start, unload at battlescape end?

		//update number of shared indexes in surface sets and sound sets
		{
			string[] surfaceNames =
			[
				"BIGOBS.PCK",
				"FLOOROB.PCK",
				"HANDOB.PCK",
				"SMOKE.PCK",
				"HIT.PCK",
				"BASEBITS.PCK",
				"INTICON.PCK",
				"CustomArmorPreviews",
				"CustomItemPreviews",
			];

			for (int i = 0; i < surfaceNames.Length; ++i)
			{
				SurfaceSet s = GetOrInsertNull(_sets, surfaceNames[i]);
				if (s != null)
				{
					s.MaxSharedFrames = s.TotalFrames;
				}
				else
				{
					Logger.Error("Surface set " + surfaceNames[i] + " not found.");
					throw new InvalidOperationException("Surface set " + surfaceNames[i] + " not found.");
				}
			}
			//special case for surface set that is loaded later
			{
				SurfaceSet s = _sets["Projectiles"];
				s.MaxSharedFrames = 385;
			}
			{
				SurfaceSet s = _sets["UnderwaterProjectiles"];
				s.MaxSharedFrames = 385;
			}
			{
				SurfaceSet s = _sets["GlobeMarkers"];
				s.MaxSharedFrames = 9;
			}
			//HACK: because of value "hitAnimation" from item that is used as offset in "X1.PCK", this set need have same number of shared frames as "SMOKE.PCK".
			{
				SurfaceSet s = _sets["X1.PCK"];
				s.MaxSharedFrames = _sets["SMOKE.PCK"].MaxSharedFrames;
			}
			{
				SurfaceSet s = _sets["TinyRanks"];
				s.MaxSharedFrames = 6;
			}
			{
				SurfaceSet s = _sets["Touch"];
				s.MaxSharedFrames = 10;
			}
		}
		{
			string[] soundNames =
			[
				"BATTLE.CAT",
				"GEO.CAT",
			];

			for (int i = 0; i < soundNames.Length; ++i)
			{
				SoundSet s = _sounds[soundNames[i]];
				s.MaxSharedSounds = s.TotalSounds;
			}
			//HACK: case for underwater surface, it should share same offsets as "BATTLE.CAT"
			{
				SoundSet s = _sounds["BATTLE2.CAT"];
				s.MaxSharedSounds = _sounds["BATTLE.CAT"].TotalSounds;
			}
		}
	}

	/// <summary>Loads the resources required by the Battlescape.</summary>
	private void LoadBattlescapeResources()
	{
		// Load Battlescape ICONS
		_sets["SPICONS.DAT"] = new SurfaceSet(32, 24);
		LoadDat(_sets["SPICONS.DAT"], "UFOGRAPH/SPICONS.DAT");
		_sets["CURSOR.PCK"] = new SurfaceSet(32, 40);
		LoadPck(_sets["CURSOR.PCK"], "UFOGRAPH/CURSOR.PCK", "UFOGRAPH/CURSOR.TAB");
		_sets["SMOKE.PCK"] = new SurfaceSet(32, 40);
		LoadPck(_sets["SMOKE.PCK"], "UFOGRAPH/SMOKE.PCK", "UFOGRAPH/SMOKE.TAB");
		_sets["HIT.PCK"] = new SurfaceSet(32, 40);
		LoadPck(_sets["HIT.PCK"], "UFOGRAPH/HIT.PCK", "UFOGRAPH/HIT.TAB");
		_sets["X1.PCK"] = new SurfaceSet(128, 64);
		LoadPck(_sets["X1.PCK"], "UFOGRAPH/X1.PCK", "UFOGRAPH/X1.TAB");
		_sets["MEDIBITS.DAT"] = new SurfaceSet(52, 58);
		LoadDat(_sets["MEDIBITS.DAT"], "UFOGRAPH/MEDIBITS.DAT");
		_sets["DETBLOB.DAT"] = new SurfaceSet(16, 16);
		LoadDat(_sets["DETBLOB.DAT"], "UFOGRAPH/DETBLOB.DAT");
		_sets["Projectiles"] = new SurfaceSet(3, 3);
		_sets["UnderwaterProjectiles"] = new SurfaceSet(3, 3);

		// Load Battlescape Terrain (only blanks are loaded, others are loaded just in time)
		_sets["BLANKS.PCK"] = new SurfaceSet(32, 40);
		LoadPck(_sets["BLANKS.PCK"], "TERRAIN/BLANKS.PCK", "TERRAIN/BLANKS.TAB");

		// Load Battlescape units
		var unitsContents = FileMap.GetVFolderContents("UNITS");
		var usets = FileMap.FilterFiles(unitsContents, "PCK");
		foreach (var name in usets)
		{
			string fname = ToUpperAscii(name);
			if (fname != "BIGOBS.PCK")
				_sets[fname] = new SurfaceSet(32, 40);
			else
				_sets[fname] = new SurfaceSet(32, 48);
			LoadPck(_sets[fname], "UNITS/" + name, "UNITS/" + CrossPlatform.NoExt(name) + ".TAB");
		}
		// incomplete chryssalid set: 1.0 data: stop loading.
		if (_sets.TryGetValue("CHRYS.PCK", out var chrys) && chrys.GetFrame(225) == null)
		{
			Logger.Fatal("Version 1.0 data detected");
			throw new InvalidOperationException("Invalid CHRYS.PCK, please patch your X-COM data to the latest version");
		}
		// TFTD uses the loftemps dat from the terrain folder, but still has enemy unknown's version in the geodata folder, which is short by 2 entries.
		var terrainContents = FileMap.GetVFolderContents("TERRAIN");
		if (terrainContents.Contains("loftemps.dat"))
		{
			MapDataSet.LoadLOFTEMPS("TERRAIN/LOFTEMPS.DAT", _voxelData);
		}
		else
		{
			MapDataSet.LoadLOFTEMPS("GEODATA/LOFTEMPS.DAT", _voxelData);
		}

		string[] scrs = ["TAC00.SCR"];

		for (int i = 0; i < scrs.Length; ++i)
		{
			_surfaces[scrs[i]] = new Surface(320, 200);
			LoadScr(_surfaces[scrs[i]], "UFOGRAPH/" + scrs[i]);
		}

		// lower case so we can find them in the contents map
		string[] lbms = ["d0.lbm", "d1.lbm", "d2.lbm", "d3.lbm"];
		string[] pals = ["PAL_BATTLESCAPE", "PAL_BATTLESCAPE_1", "PAL_BATTLESCAPE_2", "PAL_BATTLESCAPE_3"];

		Rgba[] backPal = [new(0, 5, 4, 255), new(0, 10, 34, 255), new(2, 9, 24, 255), new(2, 0, 24, 255)];

		var ufographContents = FileMap.GetVFolderContents("UFOGRAPH");
		for (int i = 0; i < lbms.Length; ++i)
		{
			if (!ufographContents.Contains(lbms[i]))
			{
				continue;
			}

			// C++ deletes the old PAL_BATTLESCAPE here (i == 0); it is replaced below
			// TODO: if we need only the palette, say so.
			Surface tempSurface = new Surface(1, 1);
			tempSurface.LoadImage("UFOGRAPH/" + lbms[i]);
			_palettes[pals[i]] = new Palette();
			Rgba[] colors = tempSurface.Palette.ToArray();
			colors[255] = backPal[i];
			_palettes[pals[i]].SetColors(colors, 256);
			CreateTransparencyLUT(_palettes[pals[i]]);
		}

		string[] spks = ["TAC01.SCR", "DETBORD.PCK", "DETBORD2.PCK", "ICONS.PCK", "MEDIBORD.PCK", "SCANBORD.PCK", "UNIBORD.PCK"];

		for (int i = 0; i < spks.Length; ++i)
		{
			string fname = ToLowerAscii(spks[i]);
			if (!ufographContents.Contains(fname))
			{
				continue;
			}

			_surfaces[spks[i]] = new Surface(320, 200);
			LoadSpk(_surfaces[spks[i]], "UFOGRAPH/" + spks[i]);
		}

		var bdys = FileMap.FilterFiles(ufographContents, "BDY");
		foreach (var name in bdys)
		{
			string idxName = ToUpperAscii(name);
			idxName = idxName[..(idxName.Length - 3)];
			if (idxName.StartsWith("MAN", StringComparison.Ordinal))
			{
				idxName += "SPK";
			}
			else if (idxName == "TAC01.")
			{
				idxName += "SCR";
			}
			else
			{
				idxName += "PCK";
			}
			_surfaces[idxName] = new Surface(320, 200);
			LoadBdy(_surfaces[idxName], "UFOGRAPH/" + name);
		}

		// Load Battlescape inventory
		var invs = FileMap.FilterFiles(ufographContents, "SPK");
		foreach (var name in invs)
		{
			string fname = ToUpperAscii(name);
			_surfaces[fname] = new Surface(320, 200);
			LoadSpk(_surfaces[fname], "UFOGRAPH/" + fname);
		}

		//"fix" of color index in original solders sprites
		if (Options.battleHairBleach)
		{
			string name;

			//personal armor
			name = "XCOM_1.PCK";
			if (_sets.TryGetValue(name, out var xcom_1))
			{
				for (int i = 0; i < 8; ++i)
				{
					//chest frame
					Surface surf = xcom_1.GetFrame(4 * 8 + i);
					RecolorSurface(surf, 0, surf.Width, 6, 9, (ref byte src) => HairXCOM1.Func(ref src, HairXCOM1.Face + 5));
					RecolorSurface(surf, 0, surf.Width, 9, 10, (ref byte src) => HairXCOM1.Func(ref src, HairXCOM1.Face + 6));
				}

				for (int i = 0; i < 3; ++i)
				{
					//fall frame
					Surface surf = xcom_1.GetFrame(264 + i);
					RecolorSurface(surf, 11, 20, 0, 24, (ref byte src) => HairXCOM1.Func(ref src, HairXCOM1.Face + 6));
				}
			}

			//all TFTD armors
			for (int j = 0; j < 3; ++j)
			{
				name = "TDXCOM_" + (char)('0' + j) + ".PCK";
				if (_sets.TryGetValue(name, out var xcom_2))
				{
					for (int i = 0; i < 16; ++i)
					{
						//chest frame without helm
						Surface surf = xcom_2.GetFrame(262 + i);
						if (i < 8)
						{
							//female chest frame
							RecolorSurface(surf, 0, surf.Width, 6, 18, HairXCOM2.Func);

							if (j == 2)
							{
								//fix some pixels in ION armor that was overwrite by previous function
								if (i == 0)
								{
									surf.SetPixel(18, 14, 16);
								}
								else if (i == 3)
								{
									surf.SetPixel(19, 12, 20);
								}
								else if (i == 6)
								{
									surf.SetPixel(13, 14, 16);
								}
							}
						}

						//we change face to pink, to prevent mixup with ION armor backpack that have same color group.
						RecolorSurface(surf, FaceXCOM2.Func);
					}

					for (int i = 0; i < 2; ++i)
					{
						//fall frame (first and second)
						Surface surf = xcom_2.GetFrame(256 + i);

						int endY;
						if (j == 3)
						{
							endY = 11 + 5 * i;
						}
						else
						{
							endY = 17;
						}
						RecolorSurface(surf, 0, surf.Width, 0, endY, FallXCOM2.Func);

						//we change face to pink, to prevent mixup with ION armor backpack that have same color group.
						RecolorSurface(surf, FaceXCOM2.Func);
					}

					//Palette fix for ION armor
					if (j == 2)
					{
						int size = xcom_2.TotalFrames;
						for (int i = 0; i < size; ++i)
						{
							Surface surf = xcom_2.GetFrame(i);
							RecolorSurface(surf, BodyXCOM2.Func);
						}
					}
				}
			}
		}
	}

	/// <summary>Loads the extra resources defined in rulesets.</summary>
	private void LoadExtraResources()
	{
		// Load fonts
		var reader = FileMap.GetYaml("Language/" + _fontName);
		Logger.Info("Loading fonts... " + _fontName);
		foreach (var fontReader in reader["fonts"].Children())
		{
			string id = fontReader["id"].ReadVal<string>();
			Font font = new Font();
			font.Load(fontReader);
			_fonts[id] = font;
		}

		// Load musics
		if (!Options.mute)
		{
			var soundFiles = FileMap.GetVFolderContents("SOUND");

			// Check which music version is available
			CatFile adlibcat = null, aintrocat = null;
			GMCatFile gmcat = null;

			foreach (var name in soundFiles)
			{
				if (name == "adlib.cat")
				{
					adlibcat = OpenCatFile("SOUND/" + name);
				}
				else if (name == "aintro.cat")
				{
					aintrocat = OpenCatFile("SOUND/" + name);
				}
				else if (name == "gm.cat")
				{
					using (var gmStream = FileMap.GetStream("SOUND/" + name)) gmcat = new GMCatFile(gmStream, "SOUND/" + name);
				}
			}

			// Try the preferred format first, otherwise use the default priority
			MusicFormat[] priority = [Options.preferredMusic, MusicFormat.MUSIC_FLAC, MusicFormat.MUSIC_OGG, MusicFormat.MUSIC_MP3, MusicFormat.MUSIC_MOD, MusicFormat.MUSIC_WAV, MusicFormat.MUSIC_ADLIB, MusicFormat.MUSIC_GM, MusicFormat.MUSIC_MIDI];
			foreach (var pair in _musicDefs)
			{
				Music music = null;
				for (int j = 0; j < priority.Length && music == null; ++j)
				{
					music = LoadMusic(priority[j], pair.Value, adlibcat, aintrocat, gmcat);
				}
				if (music != null)
				{
					_musics[pair.Key] = music;
				}
			}
		}

		Logger.Info("Lazy loading: " + (Options.lazyLoadResources ? "1" : "0"));
		if (!Options.lazyLoadResources)
		{
			Logger.Info("Loading extra resources from ruleset...");
			foreach (var pair in _extraSprites)
			{
				foreach (var extraSprites in pair.Value)
				{
					LoadExtraSprite(extraSprites);
				}
			}
		}

		if (!Options.mute)
		{
			foreach (var pair in _extraSounds)
			{
				var setName = pair.first;
				ExtraSounds soundPack = pair.second;
				_sounds.TryGetValue(setName, out SoundSet set);
				_sounds[setName] = soundPack.LoadSoundSet(set);
			}
		}

		Logger.Info("Loading custom palettes from ruleset...");
		foreach (var pair in _customPalettes)
		{
			CustomPalettes palDef = pair.Value;
			var palTargetName = palDef.Target;
			if (!_palettes.ContainsKey(palTargetName))
			{
				Logger.Info("Creating a new palette: " + palTargetName);
				_palettes[palTargetName] = new Palette();
				_palettes[palTargetName].InitBlack();
			}
			else
			{
				Logger.Verbose("Replacing items in target palette: " + palTargetName);
			}

			Palette target = _palettes[palTargetName];
			var fileName = palDef.File;
			if (fileName.Length == 0)
			{
				foreach (var def in palDef.Palette)
				{
					target.SetColor(def.Key, def.Value.x, def.Value.y, def.Value.z);
				}
			}
			else
			{
				// Load from JASC file
				LoadJascPalette(target, fileName);
			}
		}

		bool backupLogged = false;
		// snapshot: the C++ loop inserts while iterating (the new "BACKUP_" keys sort before "PAL_" and are never visited)
		foreach (var pal in _palettes.ToList())
		{
			if (pal.Key.StartsWith("PAL_", StringComparison.Ordinal))
			{
				if (!backupLogged) { Logger.Info("Making palette backups..."); backupLogged = true; }
				Logger.Verbose("Creating a backup for palette: " + pal.Key);
				string newName = "BACKUP_" + pal.Key;
				_palettes[newName] = new Palette();
				_palettes[newName].InitBlack();
				_palettes[newName].CopyFrom(pal.Value);
			}
		}

		// Support for UFO-based mods and hybrid mods
		if (_transparencyLUTs.Count == 0 && _transparencies.Count != 0)
		{
			if (GetOrInsertNull(_palettes, "PAL_BATTLESCAPE") != null)
			{
				Logger.Info("Creating transparency LUTs for PAL_BATTLESCAPE...");
				CreateTransparencyLUT(_palettes["PAL_BATTLESCAPE"]);
			}
			if (GetOrInsertNull(_palettes, "PAL_BATTLESCAPE_1") != null &&
				GetOrInsertNull(_palettes, "PAL_BATTLESCAPE_2") != null &&
				GetOrInsertNull(_palettes, "PAL_BATTLESCAPE_3") != null)
			{
				Logger.Info("Creating transparency LUTs for hybrid custom palettes...");
				CreateTransparencyLUT(_palettes["PAL_BATTLESCAPE_1"]);
				CreateTransparencyLUT(_palettes["PAL_BATTLESCAPE_2"]);
				CreateTransparencyLUT(_palettes["PAL_BATTLESCAPE_3"]);
			}
		}

		TextButton.soundPress = GetSound("GEO.CAT", BUTTON_PRESS);
		Window.soundPopup[0] = GetSound("GEO.CAT", WINDOW_POPUP[0]);
		Window.soundPopup[1] = GetSound("GEO.CAT", WINDOW_POPUP[1]);
		Window.soundPopup[2] = GetSound("GEO.CAT", WINDOW_POPUP[2]);
	}

	/// <summary>
	/// Reads a JASC palette like the C++ code: three <c>std::getline</c> for the header, then 256 lines
	/// parsed with <c>ss &gt;&gt; r &gt;&gt; g &gt;&gt; b</c> (keeping the stream semantics for short or broken files).
	/// </summary>
	internal static void LoadJascPalette(Palette target, string fileName)
	{
		ArgumentNullException.ThrowIfNull(target);
		byte[] data = FileMap.ReadAll(fileName);
		int pos = 0;
		bool eof = false, fail = false;
		string line = "";

		void getLine()
		{
			if (eof || fail)
			{
				// the sentry fails: the line is not changed
				fail = true;
				return;
			}
			int nl = Array.IndexOf(data, (byte)'\n', pos);
			if (nl < 0)
			{
				bool any = pos < data.Length;
				line = Encoding.Latin1.GetString(data, pos, data.Length - pos);
				pos = data.Length;
				eof = true;
				if (!any)
					fail = true;
			}
			else
			{
				line = Encoding.Latin1.GetString(data, pos, nl - pos);
				pos = nl + 1;
			}
		}

		getLine(); // header
		getLine(); // file format
		getLine(); // number of colors
		int r = 0, g = 0, b = 0;
		for (int j = 0; j < 256; ++j)
		{
			getLine(); // j-th color index
			var ss = new IntExtractor(line);
			ss.Extract(ref r);
			ss.Extract(ref g);
			ss.Extract(ref b);
			target.SetColor(j, r, g, b);
		}
	}

	/// <summary>C++ <c>std::stringstream &gt;&gt; int</c> on one line (C locale).</summary>
	private struct IntExtractor(string text)
	{
		private int _pos;
		private bool _eof, _fail;

		private static bool IsCSpace(char c) => c is ' ' or '\t' or '\n' or '\v' or '\f' or '\r';

		public void Extract(ref int value)
		{
			if (_eof || _fail)
			{
				_fail = true;
				return;
			}
			// sentry: skip white space
			while (_pos < text.Length && IsCSpace(text[_pos]))
				++_pos;
			if (_pos == text.Length)
			{
				_eof = true;
				_fail = true;
				return;
			}
			int start = _pos;
			bool negative = false;
			if (text[_pos] == '+' || text[_pos] == '-')
			{
				negative = text[_pos] == '-';
				++_pos;
			}
			int digits = _pos;
			long v = 0;
			bool overflow = false;
			while (_pos < text.Length && text[_pos] >= '0' && text[_pos] <= '9')
			{
				v = v * 10 + (text[_pos] - '0');
				if (v > (long)int.MaxValue + 1)
					overflow = true;
				++_pos;
			}
			if (_pos == text.Length)
				_eof = true;
			if (_pos == digits)
			{
				// no conversion: zero is written
				_pos = start;
				value = 0;
				_fail = true;
				return;
			}
			long result = negative ? -v : v;
			if (overflow || result > int.MaxValue || result < int.MinValue)
			{
				value = negative ? int.MinValue : int.MaxValue;
				_fail = true;
				return;
			}
			value = (int)result;
		}
	}

	/// <summary>Loads an external sprite.</summary>
	private void LoadExtraSprite(ExtraSprites spritePack)
	{
		if (spritePack.IsLoaded)
			return;

		if (spritePack.SingleImage)
		{
			_surfaces.TryGetValue(spritePack.Type, out Surface surface);

			_surfaces[spritePack.Type] = spritePack.LoadSurface(surface);
			if (_statePalette != null)
			{
				if (!spritePack.Type.Contains("_CPAL", StringComparison.Ordinal))
				{
					_surfaces[spritePack.Type].SetPalette(_statePalette);
				}
			}
		}
		else
		{
			_sets.TryGetValue(spritePack.Type, out SurfaceSet set);

			_sets[spritePack.Type] = spritePack.LoadSurfaceSet(set);
			if (_statePalette != null)
			{
				if (!spritePack.Type.Contains("_CPAL", StringComparison.Ordinal))
				{
					_sets[spritePack.Type].SetPalette(_statePalette);
				}
			}
		}
	}

	/// <summary>Applies necessary modifications to vanilla resources.</summary>
	private void ModResources()
	{
		// we're gonna need these
		GetSurface("GEOBORD.SCR");
		GetSurface("ALTGEOBORD.SCR", false);
		GetSurface("BACK07.SCR");
		GetSurface("ALTBACK07.SCR", false);
		GetSurface("BACK06.SCR");
		GetSurface("UNIBORD.PCK");
		GetSurfaceSet("HANDOB.PCK");
		GetSurfaceSet("FLOOROB.PCK");
		GetSurfaceSet("BIGOBS.PCK");

		// embiggen the geoscape background by mirroring the contents
		// modders can provide their own backgrounds via ALTGEOBORD.SCR
		if (!_surfaces.ContainsKey("ALTGEOBORD.SCR"))
		{
			int newWidth = 320 - 64, newHeight = 200;
			Surface newGeo = new Surface(newWidth * 3, newHeight * 3);
			Surface oldGeo = _surfaces["GEOBORD.SCR"];
			for (int x = 0; x < newWidth; ++x)
			{
				for (int y = 0; y < newHeight; ++y)
				{
					newGeo.SetPixel(newWidth + x, newHeight + y, oldGeo.GetPixel(x, y));
					newGeo.SetPixel(newWidth - x - 1, newHeight + y, oldGeo.GetPixel(x, y));
					newGeo.SetPixel(newWidth * 3 - x - 1, newHeight + y, oldGeo.GetPixel(x, y));

					newGeo.SetPixel(newWidth + x, newHeight - y - 1, oldGeo.GetPixel(x, y));
					newGeo.SetPixel(newWidth - x - 1, newHeight - y - 1, oldGeo.GetPixel(x, y));
					newGeo.SetPixel(newWidth * 3 - x - 1, newHeight - y - 1, oldGeo.GetPixel(x, y));

					newGeo.SetPixel(newWidth + x, newHeight * 3 - y - 1, oldGeo.GetPixel(x, y));
					newGeo.SetPixel(newWidth - x - 1, newHeight * 3 - y - 1, oldGeo.GetPixel(x, y));
					newGeo.SetPixel(newWidth * 3 - x - 1, newHeight * 3 - y - 1, oldGeo.GetPixel(x, y));
				}
			}
			_surfaces["ALTGEOBORD.SCR"] = newGeo;
		}

		// here we create an "alternate" background surface for the base info screen.
		if (!_surfaces.ContainsKey("ALTBACK07.SCR"))
		{
			_surfaces["ALTBACK07.SCR"] = new Surface(320, 200);
			LoadScr(_surfaces["ALTBACK07.SCR"], "GEOGRAPH/BACK07.SCR");
			Surface alt = _surfaces["ALTBACK07.SCR"];
			for (int y = 172; y >= 152; --y)
				for (int x = 5; x <= 314; ++x)
					alt.SetPixel(x, y + 4, alt.GetPixel(x, y));
			for (int y = 147; y >= 134; --y)
				for (int x = 5; x <= 314; ++x)
					alt.SetPixel(x, y + 9, alt.GetPixel(x, y));
			for (int y = 132; y >= 109; --y)
				for (int x = 5; x <= 314; ++x)
					alt.SetPixel(x, y + 10, alt.GetPixel(x, y));
		}

		// we create extra rows on the soldier stat screens by shrinking them all down one pixel/two pixels.
		int rowHeight = _manaEnabled ? 10 : 11;
		bool moveOnePixelUp = !_manaEnabled;

		// first, let's do the base info screen
		// erase the old lines, copying from a +2 offset to account for the dithering
		Surface back06 = _surfaces["BACK06.SCR"];
		for (int y = 91; y < 199; y += 12)
			for (int x = 0; x < 149; ++x)
				back06.SetPixel(x, y, back06.GetPixel(x, y + 2));
		// drawn new lines, use the bottom row of pixels as a basis
		for (int y = 89; y < 199; y += rowHeight)
			for (int x = 0; x < 149; ++x)
				back06.SetPixel(x, y, back06.GetPixel(x, 199));
		// finally, move the top of the graph up by one pixel, offset for the last iteration again due to dithering.
		if (moveOnePixelUp)
		{
			for (int y = 72; y < 80; ++y)
				for (int x = 0; x < 320; ++x)
				{
					back06.SetPixel(x, y, back06.GetPixel(x, y + (y == 79 ? 2 : 1)));
				}
		}

		// now, let's adjust the battlescape info screen.
		int startHere = _manaEnabled ? 191 : 190;
		int stopHere = _manaEnabled ? 28 : 37;
		bool moveDown = !_manaEnabled;

		// erase the old lines, no need to worry about dithering on this one.
		Surface unibord = _surfaces["UNIBORD.PCK"];
		for (int y = 39; y < 199; y += 10)
			for (int x = 0; x < 169; ++x)
				unibord.SetPixel(x, y, unibord.GetPixel(x, 30));
		// drawn new lines, use the bottom row of pixels as a basis
		for (int y = startHere; y > stopHere; y -= 9)
			for (int x = 0; x < 169; ++x)
				unibord.SetPixel(x, y, unibord.GetPixel(x, 199));
		// move the top of the graph down by eight pixels to erase the row we don't need (we actually created ~1.8 extra rows earlier)
		if (moveDown)
		{
			for (int y = 37; y > 29; --y)
				for (int x = 0; x < 320; ++x)
				{
					unibord.SetPixel(x, y, unibord.GetPixel(x, y - 8));
					unibord.SetPixel(x, y - 8, 0);
				}
		}
		else
		{
			// remove bottom line of the (entire) last row
			for (int x = 0; x < 320; ++x)
				unibord.SetPixel(x, 199, unibord.GetPixel(x, 30));
		}
	}

	/// <summary>Loads the specified music file format.</summary>
	/// <param name="fmt">Format of the music.</param>
	/// <param name="rule">Parameters of the music.</param>
	/// <param name="adlibcat">ADLIB.CAT if available.</param>
	/// <param name="aintrocat">AINTRO.CAT if available.</param>
	/// <param name="gmcat">GM.CAT if available.</param>
	/// <returns>The music, or null if it couldn't be loaded.</returns>
	private static Music LoadMusic(MusicFormat fmt, RuleMusic rule, CatFile adlibcat, CatFile aintrocat, GMCatFile gmcat)
	{
		/* MUSIC_AUTO, MUSIC_FLAC, MUSIC_OGG, MUSIC_MP3, MUSIC_MOD, MUSIC_WAV, MUSIC_ADLIB, MUSIC_GM, MUSIC_MIDI */
		string[] exts = ["", ".flac", ".ogg", ".mp3", ".mod", ".wav", "", "", ".mid"];
		Music music = null;
		var soundContents = FileMap.GetVFolderContents("SOUND");
		// C++ size_t: a negative position never matches
		ulong track = unchecked((ulong)(long)rule.CatPos);
		try
		{
			// Try Adlib music
			if (fmt == MusicFormat.MUSIC_ADLIB)
			{
				if (adlibcat != null && Options.audioBitDepth == 16)
				{
					if (track < (ulong)adlibcat.Size)
					{
						music = new AdlibMusic(rule.Normalization);
						music.Load(new MemoryStream(adlibcat.GetItem((int)track).ToArray()));
					}
					// separate intro music
					else if (aintrocat != null)
					{
						track -= (ulong)adlibcat.Size;
						if (track < (ulong)aintrocat.Size)
						{
							music = new AdlibMusic(rule.Normalization);
							music.Load(new MemoryStream(aintrocat.GetItem((int)track).ToArray()));
						}
						else
						{
							music = null;
						}
					}
				}
			}
			// Try MIDI music (from GM.CAT)
			else if (fmt == MusicFormat.MUSIC_GM)
			{
				// DOS MIDI
				if (gmcat != null && track < (ulong)gmcat.Size)
				{
					music = gmcat.LoadMIDI((uint)track);
				}
			}
			// Try digital tracks
			else
			{
				string fname = ToLowerAscii(rule.Name + exts[(int)fmt]);

				if (soundContents.Contains(fname))
				{
					music = new Music();
					music.Load("SOUND/" + fname);
				}
			}
		}
		catch (Exception e) when (IsOxceException(e))
		{
			Logger.Info(e.Message);
			music = null;
		}
		return music;
	}

	/// <summary>
	/// Preamble:
	/// this is the most horrible function i've ever written, and it makes me sad.
	/// this is, however, a necessary evil, in order to save massive amounts of time in the draw function.
	/// when used with the default TFTD mod, this function loops 4,194,304 times
	/// (4 palettes, 4 tints, 4 levels of opacity, 256 colors, 256 comparisons per)
	/// each additional tint in the rulesets will result in over a million iterations more.
	/// </summary>
	/// <param name="pal">The palette to base the lookup table on.</param>
	internal void CreateTransparencyLUT(Palette pal)
	{
		ArgumentNullException.ThrowIfNull(pal);
		Rgba[] palColors = pal.Colors;
		var lookUpTable = new List<byte>(_transparencies.Count * TransparenciesPaletteColors * TransparenciesOpacityLevels);
		// start with the color sets
		foreach (var tintLevels in _transparencies)
		{
			// then the opacity levels, using the alpha channel as the step
			foreach (Rgba tint in tintLevels)
			{
				// then the palette itself
				for (int currentColor = 0; currentColor < TransparenciesPaletteColors; ++currentColor)
				{
					int desiredR = Math.Min(255, (palColors[currentColor].R * tint.A / 255) + tint.R);
					int desiredG = Math.Min(255, (palColors[currentColor].G * tint.A / 255) + tint.G);
					int desiredB = Math.Min(255, (palColors[currentColor].B * tint.A / 255) + tint.B);

					byte closest = (byte)currentColor;
					int lowestDifference = int.MaxValue;
					// if opacity is zero then we stay with current color, transparent color will stay same too
					if (tint.A != 0 && currentColor != 0)
					{
						// now compare each color in the palette to find the closest match to our desired one
						for (int comparator = 1; comparator < TransparenciesPaletteColors; ++comparator)
						{
							int currentDifference = Sqr(desiredR - palColors[comparator].R) +
								Sqr(desiredG - palColors[comparator].G) +
								Sqr(desiredB - palColors[comparator].B);

							if (currentDifference < lowestDifference)
							{
								closest = (byte)comparator;
								lowestDifference = currentDifference;
							}
						}
					}
					lookUpTable.Add(closest);
				}
			}
		}
		_transparencyLUTs.Add(lookUpTable);
	}
}
