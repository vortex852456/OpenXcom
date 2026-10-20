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
// Ported from: src/Mod/SoundDefinition.h, src/Mod/SoundDefinition.cpp
#nullable disable

namespace Acom;

public class SoundDefinition
{
#pragma warning disable IDE0052 // kept like the C++ member (unused there too)
	private readonly string _type;
#pragma warning restore IDE0052
	private string _catFile = "";
	private readonly List<int> _soundList = [];

	public SoundDefinition(string type)
	{
		_type = type;
	}

	public void Load(YamlNodeReader reader)
	{
		foreach (var soundRange in reader["soundRanges"].Children())
		{
			var range = soundRange.ReadVal((0, 0));
			for (int j = range.Item1; j <= range.Item2; ++j)
			{
				_soundList.Add(j);
			}
		}
		foreach (var sound in reader["sounds"].Children())
		{
			_soundList.Add(sound.ReadVal(-1));
		}
		reader.TryRead("file", ref _catFile);
	}

	public List<int> SoundList => _soundList;

	public string CATFile => _catFile;
}
