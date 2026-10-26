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

namespace Acom.Placeholder;

/// <summary>Summary of a generator run.</summary>
public sealed class GeneratorReport
{
	/// <summary>Written files (relative paths).</summary>
	public List<string> Written { get; } = [];

	/// <summary>Files of the asset list that are not generated, with the reason.</summary>
	public List<string> Skipped { get; } = [];

	/// <summary>Total size of the written files.</summary>
	public long Bytes { get; set; }
}

/// <summary>
/// Writes a structurally valid placeholder data set (layout of the original data folder: GEODATA, GEOGRAPH, UFOGRAPH,
/// UNITS, TERRAIN, MAPS, ROUTES, SOUND) from the asset list and the master mod's rulesets. All content is procedural
/// and deterministic for a given seed.
/// </summary>
public sealed class PlaceholderGenerator
{
	/// <summary>Default seed (any value gives a valid data set).</summary>
	public const uint DefaultSeed = 0xAC0Du;

	/// <summary>Frames per MCD record kind: record i is floor, west wall, north wall, object for i % 4 (record 0 is always a floor).</summary>
	private const int PartCycle = 4;

	/// <summary>Number of shapes in LOFTEMPS used by the MCD records (empty, full, west wall, north wall, object).</summary>
	private const int LoftEmpty = 0, LoftFull = 1, LoftWest = 2, LoftNorth = 3, LoftObject = 4;

	private readonly AssetList _spec;
	private readonly Rules _rules;
	private readonly string? _modDir;
	private readonly uint _seed;
	private string _out = "";
	private GeneratorReport _report = new();

	/// <param name="spec">Asset list (file names, formats, sizes, frame counts).</param>
	/// <param name="rules">Rulesets of the master mod.</param>
	/// <param name="modDir">Folder of the master mod (to count MCD records of terrains the mod ships itself), or null.</param>
	/// <param name="seed">Seed of the procedural content.</param>
	public PlaceholderGenerator(AssetList spec, Rules rules, string? modDir, uint seed = DefaultSeed)
	{
		_spec = spec ?? throw new ArgumentNullException(nameof(spec));
		_rules = rules ?? throw new ArgumentNullException(nameof(rules));
		_modDir = modDir;
		_seed = seed;
	}

	/// <summary>Generates everything into <paramref name="outDir"/> (created if missing; existing files are overwritten).</summary>
	public GeneratorReport Generate(string outDir)
	{
		ArgumentNullException.ThrowIfNull(outDir);
		_out = outDir;
		_report = new GeneratorReport();
		Directory.CreateDirectory(outDir);

		var handled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		WritePalettes(handled);
		WriteLoftemps(handled);
		WriteWorld(handled);
		var terrainSets = WriteTerrains(handled);
		WriteMaps(handled, terrainSets);
		foreach (var file in _spec.Files)
		{
			if (handled.Contains(file.Path))
				continue;
			if (!WriteGeneric(file))
				_report.Skipped.Add($"{file.Path} ({file.Category}/{file.Format})");
		}
		return _report;
	}

	private void Write(string path, byte[] data)
	{
		string full = Path.Combine(_out, path.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(Path.GetDirectoryName(full)!);
		File.WriteAllBytes(full, data);
		_report.Written.Add(path);
		_report.Bytes += data.Length;
	}

	private uint SeedOf(string path) => Noise.Hash(_seed, Noise.Of(path.ToUpperInvariant()));

	// ---- palettes ----

	private void WritePalettes(HashSet<string> handled)
	{
		int count = _spec.Find("GEODATA/PALETTES.DAT")?.Records ?? 5;
		var palettes = Enumerable.Range(0, count).Select(Art.Palette).ToList();
		Write("GEODATA/PALETTES.DAT", Formats.Palettes(palettes, padded: true));
		Write("GEODATA/BACKPALS.DAT", Formats.Palettes([Art.BackPalettes()], padded: false));
		handled.Add("GEODATA/PALETTES.DAT");
		handled.Add("GEODATA/BACKPALS.DAT");
	}

	// ---- voxel templates ----

	/// <summary>LOFTEMPS.DAT: 16 rows of 16 bits per template (MapDataSet::loadLOFTEMPS).</summary>
	private void WriteLoftemps(HashSet<string> handled)
	{
		int count = Math.Max(_spec.Find("GEODATA/LOFTEMPS.DAT")?.Records ?? 112, LoftObject + 1);
		var words = new List<ushort>();
		for (int t = 0; t < count; ++t)
		{
			for (int row = 0; row < 16; ++row)
			{
				ushort bits = t switch
				{
					LoftEmpty => 0,
					LoftFull => 0xFFFF,
					LoftWest => 0x0003, // x = 0..1 along the west edge
					LoftNorth => row < 2 ? (ushort)0xFFFF : (ushort)0,
					LoftObject => row is >= 4 and < 12 ? (ushort)0x0FF0 : (ushort)0,
					// further templates: boxes of growing size
					_ => row < 1 + t % 16 ? (ushort)((1 << (1 + t % 16)) - 1) : (ushort)0,
				};
				words.Add(bits);
			}
		}
		Write("GEODATA/LOFTEMPS.DAT", Formats.Words(words));
		handled.Add("GEODATA/LOFTEMPS.DAT");
	}

	// ---- globe ----

	/// <summary>Cell size of the globe polygons in degrees.</summary>
	private const int GlobeCell = 5;

	/// <summary>
	/// WORLD.DAT: land quads of <see cref="GlobeCell"/> degrees wherever a country or mission zone lies,
	/// which joins into a few large continents; everything else is ocean.
	/// </summary>
	private void WriteWorld(HashSet<string> handled)
	{
		using var ms = new MemoryStream();
		int[] landTextures = [0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11];
		for (int lat = -85; lat < 85; lat += GlobeCell)
		{
			for (int lon = 0; lon < 360; lon += GlobeCell)
			{
				if (!IsLand(lon, lat))
					continue;
				int texture = Math.Abs(lat + GlobeCell / 2.0) > 60
					? (Noise.Range(_seed, 2, lon / 20, lat / 20, 7) == 0 ? 9 : 12)
					: landTextures[Noise.Range(_seed, landTextures.Length, lon / 20, lat / 20)];
				int l0 = lon * 8, l1 = (lon + GlobeCell) * 8, a0 = lat * 8, a1 = (lat + GlobeCell) * 8;
				Formats.WorldPolygon(ms, [(l0, a0), (l1, a0), (l1, a1), (l0, a1)], texture);
			}
		}
		Write("GEODATA/WORLD.DAT", ms.ToArray());
		handled.Add("GEODATA/WORLD.DAT");
	}

	private bool IsLand(int lon, int lat)
	{
		foreach (var a in _rules.LandAreas)
		{
			if (a.LatMax <= lat || a.LatMin >= lat + GlobeCell)
				continue;
			bool lonHit = a.LonMin <= a.LonMax
				? a.LonMax > lon && a.LonMin < lon + GlobeCell
				: a.LonMin < lon + GlobeCell || a.LonMax > lon; // wraps around 0
			if (!lonHit)
				continue;
			// zones covering whole latitude bands (poles) only become land in patches
			if (a.LonMax - a.LonMin >= 300)
				return Noise.Range(_seed, 3, lon / 15, lat / 15, 1) == 0;
			return true;
		}
		return false;
	}

	// ---- terrain ----

	/// <summary>Writes MCD + PCK/TAB for every terrain set of the asset list and of the rulesets; returns the record counts.</summary>
	private Dictionary<string, int> WriteTerrains(HashSet<string> handled)
	{
		var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var f in _spec.Files)
		{
			if (f.Format == "MCD" && f.Path.StartsWith("TERRAIN/", StringComparison.OrdinalIgnoreCase))
				names.Add(Path.GetFileNameWithoutExtension(f.Path));
		}
		foreach (var block in _rules.Blocks.Values)
			foreach (var set in block.MapDataSets)
				names.Add(set);

		foreach (var name in names)
		{
			string mcdPath = $"TERRAIN/{name.ToUpperInvariant()}.MCD", pckPath = $"TERRAIN/{name.ToUpperInvariant()}.PCK";
			var mcdSpec = _spec.Find(mcdPath);
			var pckSpec = _spec.Find(pckPath);
			if (mcdSpec == null && ModFileSize(mcdPath) is long modSize)
			{
				// shipped by the mod itself: only its record count is needed for the map blocks
				counts[name] = (int)(modSize / Formats.McdRecord);
				continue;
			}
			int records = mcdSpec?.Records ?? 16;
			int frames = pckSpec?.Frames ?? records;
			counts[name] = records;
			Write(mcdPath, Mcd(name, records, frames, _rules.StartPointSets.Contains(name)));
			WritePck(pckPath, SpriteKind.Terrain, pckSpec?.Width ?? 32, pckSpec?.Height ?? 40, frames);
			handled.Add(mcdPath);
			handled.Add(pckPath);
		}
		return counts;
	}

	private long? ModFileSize(string relative)
	{
		if (_modDir == null)
			return null;
		var file = new FileInfo(Path.Combine(_modDir, relative.Replace('/', Path.DirectorySeparatorChar)));
		return file.Exists ? file.Length : null;
	}

	/// <summary>
	/// MCD records (MapDataSet::loadData field offsets); record i is part i % 4, sprites and LOFTs consistent with it.
	/// Floors of craft and no-craft mission terrains are soldier start points.
	/// </summary>
	private static byte[] Mcd(string name, int records, int frames, bool startPoints)
	{
		var data = new byte[records * Formats.McdRecord];
		bool blanks = string.Equals(name, "BLANKS", StringComparison.OrdinalIgnoreCase);
		for (int i = 0; i < records; ++i)
		{
			var r = data.AsSpan(i * Formats.McdRecord, Formats.McdRecord);
			int part = blanks ? 0 : i % PartCycle;
			byte sprite = (byte)(i % Math.Max(frames, 1));
			for (int f = 0; f < 8; ++f)
				r[f] = sprite; // Frame[8]: no animation
			byte loft = part switch { 0 => LoftFull, 1 => LoftWest, 2 => LoftNorth, _ => LoftObject };
			for (int layer = 0; layer < 12; ++layer)
				r[8 + layer] = part == 0 ? (layer == 0 ? loft : (byte)LoftEmpty) : part == 3 && layer >= 8 ? (byte)LoftEmpty : loft;
			ushort scang = (ushort)(1 + part * 4 + i % 4); // ScanG: minimap tile (SCANG.DAT frame - 35 in the engine)
			r[20] = (byte)scang;
			r[21] = (byte)(scang >> 8);
			r[31] = (byte)(part is 1 or 2 ? 1 : 0); // Stop_LOS
			r[39] = part switch { 0 => 4, 3 => 255, _ => 255 }; // TU_Walk
			r[40] = r[39]; // TU_Slide
			r[41] = part == 0 ? (byte)4 : (byte)255; // TU_Fly
			r[42] = blanks ? (byte)255 : (byte)(20 + i % 4 * 10); // Armor
			r[43] = (byte)(part == 0 ? 0 : 5); // HE_Block
			r[44] = 0; // Die_MCD: nothing left
			r[45] = 255; // Flammable: does not burn
			r[46] = 0; // Alt_MCD
			r[48] = 0; // T_Level
			r[49] = 0; // P_Level
			r[51] = (byte)(part == 0 ? 0 : 2); // Light_Block
			r[52] = (byte)(part == 0 ? 1 + i % 6 : 0); // Footstep
			r[53] = (byte)part; // Tile_Type
			r[58] = 0; // Light_Source
			r[59] = (byte)(startPoints && part == 0 ? 1 : 0); // Target_Type: START_POINT
		}
		return data;
	}

	// ---- map blocks and routes ----

	private void WriteMaps(HashSet<string> handled, Dictionary<string, int> terrainSets)
	{
		foreach (var block in _rules.Blocks.Values)
		{
			string map = $"MAPS/{block.Name.ToUpperInvariant()}.MAP", rmp = $"ROUTES/{block.Name.ToUpperInvariant()}.RMP";
			int sizeZ = _spec.Find(map)?.MapSize is { Length: 3 } size ? size[2] : block.Vehicle ? 2 : 4;
			// object IDs count across the terrain's sets in order; use the first set with records after BLANKS
			int baseId = 0, setRecords = 0;
			foreach (var set in block.MapDataSets)
			{
				int n = terrainSets.GetValueOrDefault(set);
				if (!string.Equals(set, "BLANKS", StringComparison.OrdinalIgnoreCase) && n > 0)
				{
					setRecords = n;
					break;
				}
				baseId += n;
			}
			if (setRecords == 0 || baseId + PartCycle > 255)
				setRecords = 0; // only empty tiles possible
			Write(map, BlockMap(block, sizeZ, baseId, setRecords));
			Write(rmp, Routes(block.Width, block.Length, sizeZ));
			handled.Add(map);
			handled.Add(rmp);
		}
		// map files of the asset list that no ruleset uses: empty blocks of the listed size
		foreach (var f in _spec.Files)
		{
			if (handled.Contains(f.Path))
				continue;
			if (f.Format == "MAP" && f.MapSize is { Length: 3 } s)
			{
				Write(f.Path, Formats.Map(s[0], s[1], s[2], (_, _, _) => default));
				handled.Add(f.Path);
			}
		}
	}

	private byte[] BlockMap(BlockRule block, int sizeZ, int baseId, int setRecords)
	{
		uint seed = SeedOf(block.Name);
		int step = NodeStep(block.Width, block.Length);
		return Formats.Map(block.Width, block.Length, sizeZ, (x, y, z) =>
		{
			if (z != 0 || setRecords == 0)
				return default;
			byte floor = (byte)baseId;
			if (setRecords < PartCycle || IsNodeTile(x, y, step))
				return (floor, 0, 0, 0);
			int h = Noise.Range(seed, 40, x, y);
			byte west = h == 1 && x > 0 ? (byte)(baseId + 1) : (byte)0;
			byte north = h == 2 && y > 0 ? (byte)(baseId + 2) : (byte)0;
			byte obj = h == 3 && !block.Vehicle ? (byte)(baseId + 3) : (byte)0;
			return (floor, west, north, obj);
		});
	}

	/// <summary>Route nodes sit on a grid with this spacing (and stay free of walls and objects).</summary>
	private static int NodeStep(int width, int length)
	{
		int step = 3;
		while ((width / step + 1) * (length / step + 1) > 200)
			++step;
		return step;
	}

	private static bool IsNodeTile(int x, int y, int step) => x % step == 1 && y % step == 1;

	/// <summary>
	/// RMP nodes (BattlescapeGenerator::loadRMP) on the ground level: a grid linked to its neighbours,
	/// ranks cycling through all node ranks and a spawn priority on every node, so every unit type finds a spawn point.
	/// </summary>
	private static byte[] Routes(int width, int length, int sizeZ)
	{
		int step = NodeStep(width, length);
		var nodes = new List<(int X, int Y)>();
		for (int y = 1; y < length; y += step)
			for (int x = 1; x < width; x += step)
				nodes.Add((x, y));
		int cols = (width - 2) / step + 1;
		var data = new byte[nodes.Count * Formats.RmpRecord];
		for (int i = 0; i < nodes.Count; ++i)
		{
			var r = data.AsSpan(i * Formats.RmpRecord, Formats.RmpRecord);
			var (x, y) = nodes[i];
			r[0] = (byte)y;
			r[1] = (byte)x;
			r[2] = (byte)(sizeZ - 1); // the file counts levels from the top: ground level
			int[] neighbours = [i % cols + 1 < cols ? i + 1 : -1, i % cols > 0 ? i - 1 : -1, i + cols < nodes.Count ? i + cols : -1, i - cols >= 0 ? i - cols : -1];
			int link = 0;
			foreach (int n in neighbours)
			{
				if (n < 0)
					continue;
				r[4 + link * 3] = (byte)n;
				r[4 + link * 3 + 1] = (byte)step; // distance
				r[4 + link * 3 + 2] = 0; // usable by all unit types
				++link;
			}
			for (; link < 5; ++link)
				r[4 + link * 3] = 255; // unused
			r[19] = 0; // type: any unit
			r[20] = (byte)(i % 9); // rank
			r[21] = (byte)(1 + i % 5); // patrol flags
			r[22] = 0; // reserved
			r[23] = (byte)(1 + i % 10); // spawn priority
		}
		return data;
	}

	// ---- images, sprites, sounds ----

	private bool WriteGeneric(SpecFile f)
	{
		string path = f.Path;
		string name = Path.GetFileName(path).ToUpperInvariant();
		uint seed = SeedOf(path);
		int group = 1 + (int)(seed % 15);
		switch (f.Format)
		{
			case "SCR" when f.Width is int w && f.Height is int h:
				Write(path, Formats.Raw([Screen(name, w, h, seed, group)]));
				return true;
			case "SPK" when f.Width is int w && f.Height is int h:
				Write(path, Formats.Spk(Screen(name, w, h, seed, group)));
				return true;
			case "PCK" when f.Width is int w && f.Height is int h && f.Frames is int n:
				WritePck(path, PckKind(path), w, h, n);
				return true;
			case "DAT" when f.Category == "sprites" && f.Width is int w && f.Height is int h && f.Frames is int n:
			{
				var kind = name switch
				{
					"TEXTURE.DAT" or "SCANG.DAT" => SpriteKind.Texture,
					"DETBLOB.DAT" => SpriteKind.Disc,
					_ => SpriteKind.Box,
				};
				var frames = Enumerable.Range(0, n).Select(i => name == "TEXTURE.DAT"
					? Art.Texture(w, h, s_textureGroups[i % s_textureGroups.Length], i, seed)
					: DatFrame(kind, w, h, i, i, seed)).ToList();
				Write(path, Formats.Raw(frames));
				return true;
			}
			case "CAT" when f.Category == "sounds" && f.Records is int count && name != "DRIVERS.CAT":
				Write(path, SoundArchive(f, count, seed));
				return true;
			default:
				return false;
		}
	}

	/// <summary>Colour group of the 13 globe textures (zoom levels repeat them): greens, field colours, rock, sand, ice.</summary>
	private static readonly int[] s_textureGroups = [6, 4, 5, 3, 2, 15, 6, 3, 2, 0, 5, 7, 0];

	/// <summary>Frames of DAT sheets are opaque where the engine expects it (globe textures, minimap tiles).</summary>
	private static Image DatFrame(SpriteKind kind, int w, int h, int frame, int variant, uint seed) =>
		kind == SpriteKind.Texture
			? Art.Sprite(kind, w, h, frame, Noise.Hash(seed, variant))
			: Art.Sprite(kind, w, h, frame, seed);

	private static Image Screen(string name, int w, int h, uint seed, int group)
	{
		if (name == "GEOBORD.SCR")
		{
			// globe area dark, control panel on the right
			var img = new Image(w, h);
			for (int y = 0; y < h; ++y)
				for (int x = 0; x < w; ++x)
					img[x, y] = x < w - 64 ? Art.Color(13, 14 + Noise.Range(seed, 2, x, y)) : Art.Color(9, 9 + Noise.Range(seed, 2, x / 2, y / 2));
			Art.Border(img, w - 64, 0, 64, h, 9, 2);
			return img;
		}
		bool emblem = name.StartsWith("UP", StringComparison.Ordinal) || name.StartsWith("MAN_", StringComparison.Ordinal) || name.StartsWith("BIGOB_", StringComparison.Ordinal);
		return Art.Picture(w, h, seed, group, emblem);
	}

	private static SpriteKind PckKind(string path)
	{
		string p = path.ToUpperInvariant();
		if (p.StartsWith("TERRAIN/", StringComparison.Ordinal))
			return SpriteKind.Terrain;
		if (p is "UNITS/BIGOBS.PCK" or "UNITS/FLOOROB.PCK" or "UNITS/HANDOB.PCK" or "GEOGRAPH/BASEBITS.PCK")
			return SpriteKind.Box;
		if (p.StartsWith("UNITS/", StringComparison.Ordinal))
			return SpriteKind.Figure;
		if (p == "UFOGRAPH/CURSOR.PCK")
			return SpriteKind.Cursor;
		return SpriteKind.Disc;
	}

	private void WritePck(string path, SpriteKind kind, int w, int h, int frames)
	{
		uint seed = SeedOf(path);
		var images = Enumerable.Range(0, frames).Select(i => Art.Sprite(kind, w, h, i, seed)).ToList();
		var (pck, tab) = Formats.Pck(images);
		Write(path, pck);
		Write(Path.ChangeExtension(path, ".TAB"), tab);
	}

	/// <summary>
	/// A sound archive with one short sine beep per entry (pitch from the index, length from the asset list, capped):
	/// RIFF WAV for the Windows archives (SAMPLE*.CAT), 6-bit DOS samples for the others.
	/// </summary>
	private static byte[] SoundArchive(SpecFile f, int count, uint seed)
	{
		bool wav = f.SoundDetails?.Any(s => s.Format == "WAV") ?? false;
		var items = new List<byte[]>();
		for (int i = 0; i < count; ++i)
		{
			var detail = f.SoundDetails?.Find(s => s.Index == i);
			double seconds = Math.Clamp(detail?.Seconds ?? 0.2, 0.08, 0.35);
			int rate = wav ? 11025 : 8000;
			double freq = 220 * Math.Pow(2, (i + seed % 7) % 24 / 12.0);
			int n = (int)(seconds * rate);
			var samples = new byte[n];
			for (int s = 0; s < n; ++s)
			{
				double env = Math.Min(1, s / (rate * 0.005)) * (1 - (double)s / n);
				double v = Math.Sin(2 * Math.PI * freq * s / rate) * env * 0.6;
				samples[s] = wav ? (byte)Math.Round(128 + v * 127) : (byte)Math.Round(32 + v * 31);
			}
			string label = $"BEEP{i:D2}";
			items.Add(Formats.CatSoundItem(label, wav ? Formats.Wav(samples, rate) : Formats.DosSound(samples)));
		}
		return Formats.Cat(items);
	}
}
