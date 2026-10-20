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
// Ported from: src/Mod/RuleMusic.h, src/Mod/RuleMusic.cpp
#nullable disable

namespace Acom;

public class RuleMusic
{
	private readonly string _type;
	private string _name = "";
	private int _catPos;
	private float _normalization;

	/// <summary>
	/// Initialize catpos as int_max to prevent trying to load files from cats that don't exist,
	/// but allow for optional music to be listed regardless for loading .ogg or .mp3 versions
	/// of said files, should they be present.
	/// Default normalization value is 0.76, this only applies to the adlib mixer;
	/// 0.76 is roughly optimal for all the TFTD tracks.
	/// </summary>
	/// <param name="type">String defining the type.</param>
	public RuleMusic(string type)
	{
		_type = type;
		_catPos = int.MaxValue;
		_normalization = 0.76f;
	}

	/// <summary>Loads info about the music track.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("name", ref _name);
		reader.TryRead("catPos", ref _catPos);
		reader.TryRead("normalization", ref _normalization);
	}

	/// <summary>Gets the track's filename in the SOUND folder (no extension).</summary>
	public string Name => _name.Length == 0 ? _type : _name;

	/// <summary>Gets the track's index in the catalog file.</summary>
	public int CatPos => _catPos;

	/// <summary>Gets the track's normalization level (Adlib only).</summary>
	public float Normalization => _normalization;
}
