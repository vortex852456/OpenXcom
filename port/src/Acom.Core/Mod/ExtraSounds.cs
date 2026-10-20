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
// Ported from: src/Mod/ExtraSounds.h, src/Mod/ExtraSounds.cpp
#nullable disable

namespace Acom;

/// <summary>For adding a set of extra sound data to the game.</summary>
public class ExtraSounds
{
	private string _type = "";
	private SortedDictionary<int, string> _sounds = [];
	private ModData _current;

	/// <summary>Creates a blank set of extra sound data.</summary>
	public ExtraSounds()
	{
		_current = null;
	}

	/// <summary>Loads the extra sound set from YAML.</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="current">The data of the associated mod.</param>
	public void Load(YamlNodeReader reader, ModData current)
	{
		reader.TryRead("type", ref _type);
		reader.TryRead("files", ref _sounds);
		_current = current;
	}

	/// <summary>Gets the filename that this sound set represents.</summary>
	public string Type => _type;

	/// <summary>Gets the list of sounds defined by this mod.</summary>
	public SortedDictionary<int, string> Sounds => _sounds;

	/// <summary>Loads the external sounds into a new or existing soundset.</summary>
	/// <param name="set">Existing soundset.</param>
	/// <returns>New soundset.</returns>
	public SoundSet LoadSoundSet(SoundSet set)
	{
		if (set == null)
		{
			Logger.Warning("Creating new sound set: " + _type + ", this will likely have no in-game use.");
			set = new SoundSet();
		}
		else
		{
			Logger.Verbose("Adding/Replacing items in sound set: " + _type);
		}

		foreach (var pair in _sounds)
		{
			int startSound = pair.Key;
			string fileName = pair.Value;
			if (fileName.EndsWith('/'))
			{
				Logger.Verbose("Loading sound set from folder: " + fileName + " starting at index: " + startSound);
				int offset = startSound;
				var contents = new List<string>(FileMap.GetVFolderContents(fileName));
				contents.Sort(Unicode.NaturalComparison);
				foreach (var name in contents)
				{
					try
					{
						LoadSound(set, offset, fileName + name);
						offset++;
					}
					catch (Exception e) when (e is InvalidOperationException or InvalidDataException or IOException)
					{
						Logger.Warning(e.Message);
					}
				}
			}
			else
			{
				LoadSound(set, startSound, fileName);
			}
		}
		return set;
	}

	private void LoadSound(SoundSet set, int index, string fileName)
	{
		int indexWithOffset = index;
		if (indexWithOffset >= set.MaxSharedSounds)
		{
			if (indexWithOffset >= _current.size)
			{
				throw new InvalidOperationException("ExtraSounds '" + _type + "' sound '" + indexWithOffset + "' exceeds mod '" + _current.name + "' size limit " + _current.size);
			}
			indexWithOffset += _current.offset;
		}

		Sound sound = set.GetSound(indexWithOffset);
		if (sound != null)
		{
			Logger.Verbose("Replacing sound: " + index + ", using index: " + indexWithOffset);
		}
		else
		{
			Logger.Verbose("Adding sound: " + index + ", using index: " + indexWithOffset);
			sound = set.AddSound(indexWithOffset);
		}
		sound.Load(fileName);
	}

	/// <summary>Gets mod data that define this sounds.</summary>
	public ModData ModOwner => _current;
}
