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

using System.Text.Json;

namespace Acom.Placeholder;

/// <summary>One sound of an archive in the asset list (technical facts only).</summary>
public sealed class SpecSound
{
	public int Index { get; init; }
	public string Format { get; init; } = "";
	public int? SampleRate { get; init; }
	public double? Seconds { get; init; }
}

/// <summary>One file of the asset list <c>content/spec/&lt;game&gt;/assets.json</c> (written by Acom.AssetSpec).</summary>
public sealed class SpecFile
{
	public string Path { get; init; } = "";
	public string Category { get; init; } = "";
	public string Format { get; init; } = "";
	public long Bytes { get; init; }
	public int? Width { get; init; }
	public int? Height { get; init; }
	public int? Frames { get; init; }
	public int? Records { get; init; }
	public int[]? MapSize { get; init; }
	public List<SpecSound>? SoundDetails { get; init; }
}

/// <summary>The asset list: which files the engine expects, with sizes and frame counts.</summary>
public sealed class AssetList
{
	public string Game { get; init; } = "";
	public List<SpecFile> Files { get; init; } = [];

	private static readonly JsonSerializerOptions s_json = new() { PropertyNameCaseInsensitive = true };

	/// <summary>Reads an assets.json file.</summary>
	public static AssetList Load(string path)
	{
		using var stream = File.OpenRead(path);
		return JsonSerializer.Deserialize<AssetList>(stream, s_json) ?? throw new InvalidDataException("Empty asset list: " + path);
	}

	/// <summary>Looks up a file by its path (upper case, '/' separated), or null.</summary>
	public SpecFile? Find(string path) => Files.Find(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase));
}
