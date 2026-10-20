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

/// <summary>Metadata of one frame of a sprite set. Contains no pixel data.</summary>
internal sealed class FrameInfo
{
	public int Index { get; init; }
	public bool Empty { get; init; }

	/// <summary>Bounding box of non-transparent pixels: x, y, width, height (classic pixels).</summary>
	public int[]? Bounds { get; init; }

	/// <summary>Palette colour groups (index / 16) used by the frame.</summary>
	public int[] ColorGroups { get; init; } = [];

	public List<string> UsedBy { get; } = [];
}

/// <summary>Metadata of one entry of a sound archive. Contains no audio data.</summary>
internal sealed class SoundInfo
{
	public int Index { get; init; }
	public int Bytes { get; init; }
	public string Format { get; init; } = "";
	public int? SampleRate { get; init; }
	public double? Seconds { get; init; }
	public List<string> UsedBy { get; } = [];
}

/// <summary>Metadata of one file of the original data.</summary>
internal sealed class AssetEntry
{
	/// <summary>Path relative to the data folder, upper case, '/' separated.</summary>
	public required string Path { get; init; }

	/// <summary>sprites, images, terrain, maps, routes, sounds, music, video, palette, font, text, data, unknown.</summary>
	public required string Category { get; set; }

	public required string Format { get; init; }
	public string Purpose { get; set; } = "";
	public long Bytes { get; init; }

	/// <summary>Frame or image size in classic pixels.</summary>
	public int? Width { get; set; }
	public int? Height { get; set; }

	/// <summary>Target size for HD art (classic size x HD scale).</summary>
	public int? HdWidth { get; set; }
	public int? HdHeight { get; set; }

	public int? Frames { get; set; }
	public int? NonEmptyFrames { get; set; }

	/// <summary>Palette colour groups used anywhere in the file.</summary>
	public int[]? ColorGroups { get; set; }

	/// <summary>Map block size: x (width), y (length), z (levels).</summary>
	public int[]? MapSize { get; set; }

	/// <summary>Number of records (MCD tiles, RMP nodes, archive entries, ...).</summary>
	public int? Records { get; set; }

	public List<FrameInfo>? FrameDetails { get; set; }
	public List<SoundInfo>? SoundDetails { get; set; }
	public List<string> UsedBy { get; } = [];
	public List<string> Notes { get; } = [];
}

/// <summary>Complete specification written to assets.json.</summary>
internal sealed class AssetSpecification
{
	public string Generator { get; init; } = "Acom.AssetSpec";
	public int FormatVersion { get; init; } = 1;
	public required string Game { get; init; }
	public required string Rules { get; init; }
	public int HdScale { get; init; }
	public List<AssetEntry> Files { get; init; } = [];

	/// <summary>Logical sound sets (BATTLE.CAT, GEO.CAT) with the rules using each index.</summary>
	public SortedDictionary<string, SortedDictionary<int, List<string>>> SoundSets { get; init; } = [];

	/// <summary>Ruleset references that point to files missing in the data folder.</summary>
	public List<string> UnresolvedReferences { get; init; } = [];
}
