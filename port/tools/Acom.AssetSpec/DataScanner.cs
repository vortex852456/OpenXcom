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

using System.Buffers.Binary;
using System.Text;
using Acom.Engine;

namespace Acom.AssetSpec;

/// <summary>
/// Reads the original game data and extracts metadata only: sizes, frame counts,
/// bounding boxes and used palette colour groups. No pixels, colours, texts or samples
/// leave this class.
/// </summary>
internal sealed class DataScanner
{
	private readonly int _hdScale;
	private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

	public DataScanner(string root, int hdScale)
	{
		_hdScale = hdScale;
		foreach (var full in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
			_files[Normalize(Path.GetRelativePath(root, full))] = full;
	}

	/// <summary>All files, as normalized relative paths.</summary>
	public IEnumerable<string> Files => _files.Keys.Order(StringComparer.Ordinal);

	public bool Exists(string path) => _files.ContainsKey(Normalize(path));

	public static string Normalize(string path) => path.Replace('\\', '/').ToUpperInvariant();

	private FileStream Open(string path) => File.OpenRead(_files[path]);

	/// <summary>Analyzes every file of the data folder.</summary>
	public List<AssetEntry> ScanAll()
	{
		var result = new List<AssetEntry>();
		foreach (var path in Files)
		{
			var entry = Analyze(path);
			if (entry != null)
				result.Add(entry);
		}
		return result;
	}

	private AssetEntry? Analyze(string path)
	{
		string dir = path.Contains('/', StringComparison.Ordinal) ? path[..path.LastIndexOf('/')] : "";
		string name = path[(path.LastIndexOf('/') + 1)..];
		string ext = Path.GetExtension(name).TrimStart('.');
		long bytes = new FileInfo(_files[path]).Length;
		string tab = path[..^ext.Length] + "TAB";

		try
		{
			switch (ext)
			{
				case "TAB":
					return null; // described together with its PCK
				case "PCK" when _files.ContainsKey(tab):
				{
					var (w, h) = PckFrameSize(path);
					return SpriteSet(path, "PCK", bytes, w, h, set =>
					{
						using var pck = Open(path);
						using var t = Open(tab);
						set.LoadPck(pck, t);
					});
				}
				case "PCK":
					// Without TAB the engine reads these as SPK images (UFOGRAPH/ICONS.PCK, DETBORD.PCK, ...).
					return Image(path, "SPK", bytes, 320, 200, s => { using var f = Open(path); s.LoadSpk(f); });
				case "SCR" when path == "UFOGRAPH/TAC01.SCR":
					return Image(path, "SPK", bytes, 320, 200, s => { using var f = Open(path); s.LoadSpk(f); });
				case "SCR":
					return Image(path, "SCR", bytes, 320, (int)Math.Max(1, bytes / 320), s => { using var f = Open(path); s.LoadScr(f); });
				case "SPK":
					return Image(path, "SPK", bytes, 320, 200, s => { using var f = Open(path); s.LoadSpk(f); });
				case "BDY":
					return Image(path, "BDY", bytes, 320, 200, s => { using var f = Open(path); s.LoadBdy(f); });
				case "LBM":
					return Lbm(path, bytes);
				case "DAT":
					return Dat(path, name, bytes);
				case "MAP":
					return MapBlock(path, bytes);
				case "RMP":
					return new AssetEntry { Path = path, Category = "routes", Format = "RMP", Bytes = bytes, Records = (int)(bytes / 24), Purpose = "KI-Wegpunkte eines Kartenblocks (Knoten à 24 Byte)" };
				case "MCD":
					return new AssetEntry { Path = path, Category = "terrain", Format = "MCD", Bytes = bytes, Records = (int)(bytes / 62), Purpose = "Gelände-Kacheldaten (Eigenschaften, Sprite-Verweise, à 62 Byte)" };
				case "CAT":
					return Cat(path, name, bytes);
				case "FLI":
				case "FLC":
				case "VID":
					return Video(path, bytes);
				case "IDX" when _files.ContainsKey(path[..^3] + "VID"):
					return new AssetEntry { Path = path, Category = "video", Format = ext, Bytes = bytes, Purpose = "Index zur gleichnamigen VID-Datei" };
				case "COM":
				case "EXE":
				case "CFG":
				case "PAT":
				case "BAT":
					return new AssetEntry { Path = path, Category = "program", Format = ext, Bytes = bytes, Purpose = "DOS-Programm, -Treiber oder -Konfiguration (für ACOM ohne Bedeutung)" };
				case "WAV":
				case "VOC":
				case "MID":
				case "OGG":
				case "MP3":
				case "FLAC":
				case "XMI":
					return new AssetEntry { Path = path, Category = dir.Contains("SOUND", StringComparison.Ordinal) ? "sounds" : "music", Format = ext, Bytes = bytes, Purpose = "Audiodatei (Inhalt wird nicht ausgewertet)" };
				case "TXT":
				case "DOC":
				case "HLP":
					return new AssetEntry { Path = path, Category = "text", Format = ext, Bytes = bytes, Purpose = "Text (Inhalt wird nicht ausgewertet)" };
				default:
					return new AssetEntry { Path = path, Category = "unknown", Format = ext, Bytes = bytes, Purpose = "Nicht ausgewertet" };
			}
		}
#pragma warning disable CA1031 // A broken file must not stop the inventory.
		catch (Exception e)
#pragma warning restore CA1031
		{
			var entry = new AssetEntry { Path = path, Category = "unknown", Format = ext, Bytes = bytes, Purpose = "Fehler beim Lesen" };
			entry.Notes.Add(e.GetType().Name + ": " + e.Message);
			return entry;
		}
	}

	/// <summary>Frame sizes as used by Mod::loadVanillaResources / loadBattlescapeResources.</summary>
	private static (int W, int H) PckFrameSize(string path) => path switch
	{
		"UNITS/BIGOBS.PCK" => (32, 48),
		"UFOGRAPH/X1.PCK" => (128, 64),
		_ => (32, 40),
	};

	private AssetEntry SpriteSet(string path, string format, long bytes, int w, int h, Action<SurfaceSet> load)
	{
		var set = new SurfaceSet(w, h);
		load(set);
		var entry = new AssetEntry
		{
			Path = path,
			Category = path.StartsWith("TERRAIN/", StringComparison.Ordinal) ? "terrain" : "sprites",
			Format = format,
			Bytes = bytes,
			Width = w,
			Height = h,
			HdWidth = w * _hdScale,
			HdHeight = h * _hdScale,
			Purpose = Purposes.For(path),
		};
		var frames = new List<FrameInfo>();
		var groups = new SortedSet<int>();
		for (int i = 0; i < set.TotalFrames; ++i)
		{
			var frame = set.GetFrame(i);
			var info = frame == null ? new FrameInfo { Index = i, Empty = true } : Describe(i, frame);
			groups.UnionWith(info.ColorGroups);
			frames.Add(info);
		}
		entry.Frames = frames.Count;
		entry.NonEmptyFrames = frames.Count(f => !f.Empty);
		entry.ColorGroups = [.. groups];
		entry.FrameDetails = frames;
		return entry;
	}

	private AssetEntry Image(string path, string format, long bytes, int w, int h, Action<Surface> load)
	{
		var surface = new Surface(w, h, 0, 0, 1);
		load(surface);
		var info = Describe(0, surface);
		var entry = new AssetEntry
		{
			Path = path,
			Category = "images",
			Format = format,
			Bytes = bytes,
			Width = w,
			Height = h,
			HdWidth = w * _hdScale,
			HdHeight = h * _hdScale,
			Frames = 1,
			NonEmptyFrames = info.Empty ? 0 : 1,
			ColorGroups = info.ColorGroups,
			Purpose = Purposes.For(path),
		};
		if (info.Bounds != null && (info.Bounds[2] != w || info.Bounds[3] != h))
			entry.Notes.Add($"Belegter Bereich: x={info.Bounds[0]} y={info.Bounds[1]} {info.Bounds[2]}×{info.Bounds[3]}");
		return entry;
	}

	/// <summary>Bounding box and used colour groups of one surface (classic density).</summary>
	internal static FrameInfo Describe(int index, Surface surface)
	{
		int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
		Span<bool> used = stackalloc bool[16];
		for (int y = 0; y < surface.Height; ++y)
		{
			for (int x = 0; x < surface.Width; ++x)
			{
				byte p = surface.GetPixel(x, y);
				if (p == 0)
					continue;
				used[p >> 4] = true;
				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
			}
		}
		if (maxX < 0)
			return new FrameInfo { Index = index, Empty = true };
		var groups = new List<int>();
		for (int g = 0; g < 16; ++g)
		{
			if (used[g])
				groups.Add(g);
		}
		return new FrameInfo
		{
			Index = index,
			Bounds = [minX, minY, maxX - minX + 1, maxY - minY + 1],
			ColorGroups = [.. groups],
		};
	}

	private AssetEntry Dat(string path, string name, long bytes)
	{
		// Image sets with fixed frame sizes (Mod.cpp)
		(int W, int H)? frame = path switch
		{
			"GEOGRAPH/TEXTURE.DAT" => (32, 32),
			"GEODATA/SCANG.DAT" => (4, 4),
			"UFOGRAPH/SPICONS.DAT" => (32, 24),
			"UFOGRAPH/MEDIBITS.DAT" => (52, 58),
			"UFOGRAPH/DETBLOB.DAT" => (16, 16),
			_ => null,
		};
		if (frame is { } f)
		{
			return SpriteSet(path, "DAT", bytes, f.W, f.H, set => { using var s = Open(path); set.LoadDat(s); });
		}
		if (path == "GEODATA/INTERWIN.DAT")
			return Image(path, "SCR", bytes, 160, (int)(bytes / 160), s => { using var st = Open(path); s.LoadScr(st); });

		var entry = new AssetEntry { Path = path, Category = "data", Format = "DAT", Bytes = bytes, Purpose = Purposes.For(path) };
		switch (name)
		{
			case "PALETTES.DAT":
				entry.Category = "palette";
				entry.Records = (int)(bytes / (768 + 6));
				entry.Notes.Add("Nur Struktur erfasst; Farbwerte werden nicht exportiert.");
				break;
			case "BACKPALS.DAT":
				entry.Category = "palette";
				entry.Records = (int)(bytes / 48);
				entry.Notes.Add("Nur Struktur erfasst; Farbwerte werden nicht exportiert.");
				break;
			case "LOFTEMPS.DAT":
				entry.Records = (int)(bytes / 32);
				break;
			case "BIGLETS.DAT":
				entry.Category = "font";
				entry.Width = 16;
				entry.Height = 16;
				entry.Records = (int)(bytes / 256);
				break;
			case "SMALLSET.DAT":
				entry.Category = "font";
				entry.Width = 8;
				entry.Height = 9;
				entry.Records = (int)(bytes / 72);
				break;
		}
		if (entry.Purpose.Length == 0 && IsTextFile(name))
		{
			entry.Category = "text";
			entry.Purpose = "Spieltexte (Inhalt wird nicht ausgewertet)";
		}
		if (entry.Purpose.Length == 0)
			entry.Purpose = "Daten (nicht ausgewertet)";
		return entry;
	}

	private static bool IsTextFile(string name) =>
		name.StartsWith("ENGLISH", StringComparison.Ordinal) || name.StartsWith("GERMAN", StringComparison.Ordinal) ||
		name.StartsWith("FRENCH", StringComparison.Ordinal) || name.StartsWith("ITALIAN", StringComparison.Ordinal) ||
		name.StartsWith("SPANISH", StringComparison.Ordinal);

	private AssetEntry MapBlock(string path, long bytes)
	{
		using var s = Open(path);
		Span<byte> header = stackalloc byte[3];
		s.ReadExactly(header);
		// MAP header: rows (y), columns (x), levels (z); then 4 bytes per tile
		var entry = new AssetEntry
		{
			Path = path,
			Category = "maps",
			Format = "MAP",
			Bytes = bytes,
			MapSize = [header[1], header[0], header[2]],
			Purpose = "Kartenblock (Kacheln je Ebene: Boden, Westwand, Nordwand, Objekt)",
		};
		long expected = 3 + (long)header[0] * header[1] * header[2] * 4;
		if (expected != bytes)
			entry.Notes.Add($"Dateigröße {bytes} weicht von {expected} ab");
		return entry;
	}

	private AssetEntry Lbm(string path, long bytes)
	{
		var entry = new AssetEntry { Path = path, Category = "images", Format = "LBM", Bytes = bytes, Purpose = Purposes.For(path) };
		using var s = Open(path);
		var data = new byte[Math.Min(bytes, 4096)];
		s.ReadAtLeast(data, data.Length, throwOnEndOfStream: false);
		// IFF: "FORM" size type, then chunks; BMHD holds width/height (big endian)
		for (int pos = 12; pos + 12 <= data.Length;)
		{
			string id = Encoding.ASCII.GetString(data, pos, 4);
			int size = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(pos + 4));
			if (id == "BMHD")
			{
				entry.Width = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 8));
				entry.Height = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 10));
				entry.HdWidth = entry.Width * _hdScale;
				entry.HdHeight = entry.Height * _hdScale;
				break;
			}
			pos += 8 + size + (size & 1);
		}
		return entry;
	}

	private AssetEntry Video(string path, long bytes)
	{
		var entry = new AssetEntry { Path = path, Category = "video", Format = Path.GetExtension(path).TrimStart('.'), Bytes = bytes, Purpose = Purposes.For(path) };
		using var s = Open(path);
		Span<byte> header = stackalloc byte[16];
		if (s.ReadAtLeast(header, 16, throwOnEndOfStream: false) == 16)
		{
			// FLI/FLC header: size u32, magic u16 (0xAF11/0xAF12), frames u16, width u16, height u16
			ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(header[4..]);
			if (magic is not (0xAF11 or 0xAF12))
				entry.Notes.Add("Kein FLI/FLC-Kopf erkannt (eigenes Videoformat); nur Größe erfasst");
			else
			{
				entry.Frames = BinaryPrimitives.ReadUInt16LittleEndian(header[6..]);
				entry.Width = BinaryPrimitives.ReadUInt16LittleEndian(header[8..]);
				entry.Height = BinaryPrimitives.ReadUInt16LittleEndian(header[10..]);
				entry.HdWidth = entry.Width * _hdScale;
				entry.HdHeight = entry.Height * _hdScale;
			}
		}
		return entry;
	}

	private AssetEntry Cat(string path, string name, long bytes)
	{
		using var s = Open(path);
		var cat = new CatFile(s, path);
		bool music = name is "GM.CAT" or "ADLIB.CAT" or "ROLAND.CAT" or "AINTRO.CAT" or "RINTRO.CAT" || path.Contains("MUSIC", StringComparison.Ordinal);
		var entry = new AssetEntry
		{
			Path = path,
			Category = music ? "music" : "sounds",
			Format = "CAT",
			Bytes = bytes,
			Records = cat.Size,
			Purpose = Purposes.For(path),
		};
		if (!music)
		{
			entry.SoundDetails = [];
			for (int i = 0; i < cat.Size; ++i)
				entry.SoundDetails.Add(DescribeSound(i, cat.GetItem(i).Span));
		}
		return entry;
	}

	/// <summary>Format and length of one CAT sound entry (see SoundSet::loadCatByIndex).</summary>
	internal static SoundInfo DescribeSound(int index, ReadOnlySpan<byte> item)
	{
		if (item.Length < 1)
			return new SoundInfo { Index = index, Bytes = 0, Format = "leer" };
		int nameSize = item[0];
		var data = item.Length > nameSize + 1 ? item[(nameSize + 1)..] : [];
		if (data.Length >= 44 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WAVE"u8))
		{
			int rate = BinaryPrimitives.ReadInt32LittleEndian(data[0x18..]);
			int byteRate = BinaryPrimitives.ReadInt32LittleEndian(data[0x1C..]);
			double? seconds = byteRate > 0 ? Math.Round((data.Length - 44) / (double)byteRate, 2) : null;
			return new SoundInfo { Index = index, Bytes = item.Length, Format = "WAV", SampleRate = rate, Seconds = seconds };
		}
		return new SoundInfo { Index = index, Bytes = item.Length, Format = data.Length < 12 ? "leer" : "DOS-Rohdaten" };
	}
}
