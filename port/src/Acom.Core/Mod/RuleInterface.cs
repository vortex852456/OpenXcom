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
// Ported from: src/Mod/RuleInterface.h, src/Mod/RuleInterface.cpp
#nullable disable

namespace Acom;

public class Element
{
	/// <summary>Basic rect info.</summary>
	public int x = int.MaxValue;
	public int y = int.MaxValue;
	public int w = int.MaxValue;
	public int h = int.MaxValue;
	/// <summary>3 colors.</summary>
	public int color = int.MaxValue;
	public int color2 = int.MaxValue;
	public int border = int.MaxValue;
	/// <summary>Custom value.</summary>
	public int custom;
	/// <summary>Defines inversion behaviour.</summary>
	public bool TFTDMode;
}

public class RuleInterface
{
	private readonly string _type;
	private string _palette = "";
	private string _parent = "";
	private string _backgroundImage = "";
	private string _altBackgroundImage = "";
	private string _music = "";
	private int _sound;
	private List<(string first, string second)> _upgBackgroundImage = [];
	private readonly SortedDictionary<string, Element> _elements = new(StringComparer.Ordinal);

	/// <summary>
	/// Creates a blank ruleset for a certain
	/// type of interface, containing an index of elements that make it up.
	/// </summary>
	/// <param name="type">String defining the type.</param>
	public RuleInterface(string type)
	{
		_type = type;
		_sound = -1;
	}

	/// <summary>Loads the elements from a YAML file.</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		if (reader["refNode"] is { IsValid: true } parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("palette", ref _palette);
		reader.TryRead("parent", ref _parent);
		reader.TryRead("backgroundImage", ref _backgroundImage);
		reader.TryRead("altBackgroundImage", ref _altBackgroundImage);
		reader.TryRead("upgBackgroundImage", ref _upgBackgroundImage);
		reader.TryRead("music", ref _music);
		// Mod::loadSoundOffset does nothing for a missing node; checking here keeps loading usable without a Mod
		if (reader["sound"])
			mod.LoadSoundOffset(_type, ref _sound, reader["sound"], "GEO.CAT");

		foreach (var elementReader in reader["elements"].Children())
		{
			string id = elementReader["id"].ReadVal<string>();
			if (!_elements.TryGetValue(id, out var element))
			{
				element = new Element();
				_elements[id] = element;
			}
			if (elementReader["size"])
			{
				var pos = elementReader["size"].ReadVal<(int, int)>();
				element.w = pos.Item1;
				element.h = pos.Item2;
			}
			if (elementReader["pos"])
			{
				var pos = elementReader["pos"].ReadVal<(int, int)>();
				element.x = pos.Item1;
				element.y = pos.Item2;
			}
			elementReader.TryRead("color", ref element.color);
			elementReader.TryRead("color2", ref element.color2);
			elementReader.TryRead("border", ref element.border);
			elementReader.TryRead("custom", ref element.custom);
			elementReader.TryRead("TFTDMode", ref element.TFTDMode);
		}
	}

	/// <summary>Retrieves info on an element. Can be null.</summary>
	public Element GetElementOptional(string id)
	{
		return _elements.TryGetValue(id, out var element) ? element : null;
	}

	/// <summary>Retrieves info on an element.</summary>
	public Element GetElement(string id)
	{
		var i = GetElementOptional(id);
		if (i == null)
		{
			throw new InvalidOperationException("Missing interface Element '" + id + "' in '" + _type + "'");
		}
		return i;
	}

	public string Palette => _palette;

	public string Parent => _parent;

	public string GetBackgroundImage(Mod mod, SavedGame save)
	{
		if (save != null)
		{
			foreach (var pair in _upgBackgroundImage)
			{
				var r = mod.GetResearch(pair.first, false);
				if (r != null && save.IsResearched(r))
				{
					return pair.second;
				}
			}
		}
		return _backgroundImage;
	}

	public string AltBackgroundImage => _altBackgroundImage;

	public string Music => _music;

	public int Sound => _sound;
}
