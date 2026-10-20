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
// Ported from: src/Savegame/WeightedOptions.h, src/Savegame/WeightedOptions.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Holds pairs of relative weights and IDs.
/// It is used to store options and make a random choice between them.
/// </summary>
public class WeightedOptions
{
	/// <summary>Options and weights (C++ std::map, sorted by key).</summary>
	private readonly SortedDictionary<string, int> _choices = new(StringComparer.Ordinal);
	/// <summary>The total weight of all options.</summary>
	private int _totalWeight;

	/// <summary>Create an empty set.</summary>
	public WeightedOptions()
	{
		// Empty by design.
	}

	/// <summary>Copies another set (C++ copy constructor).</summary>
	public WeightedOptions(WeightedOptions other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_choices = new SortedDictionary<string, int>(other._choices, StringComparer.Ordinal);
		_totalWeight = other._totalWeight;
	}

	/// <summary>
	/// Select a random choice from among the contents.
	/// This MUST be called on non-empty objects.
	/// Each time this is called, the returned value can be different.
	/// </summary>
	/// <returns>The key of the selected choice.</returns>
	public string Choose()
	{
		if (_totalWeight == 0)
		{
			return "";
		}
		int pick = Rng.Generate(1, _totalWeight);
		string chosen = null;
		foreach (var ii in _choices)
		{
			chosen = ii.Key;
			if (pick <= ii.Value)
				break;
			pick -= ii.Value;
		}
		// We always have a valid iterator here.
		return chosen;
	}

	/// <summary>
	/// Set an option's weight.
	/// If <paramref name="weight"/> is set to 0, the option is removed from the list of choices.
	/// If <paramref name="id"/> already exists, the new weight replaces the old one, otherwise
	/// <paramref name="id"/> is added to the list of choices, with <paramref name="weight"/> as the weight.
	/// </summary>
	/// <param name="id">The option name.</param>
	/// <param name="weight">The option's new weight.</param>
	public void Set(string id, int weight)
	{
		if (_choices.TryGetValue(id, out int old))
		{
			_totalWeight -= old;
			if (0 != weight)
			{
				_choices[id] = weight;
				_totalWeight += weight;
			}
			else
			{
				_choices.Remove(id);
			}
		}
		else if (0 != weight)
		{
			_choices.Add(id, weight);
			_totalWeight += weight;
		}
	}

	/// <summary>Is this empty?</summary>
	public bool Empty() => 0 == _totalWeight;

	/// <summary>Remove all entries.</summary>
	public void Clear()
	{
		_totalWeight = 0;
		_choices.Clear();
	}

	/// <summary>
	/// Read all the weighted options from a node reader and add them to the WeightedOptions.
	/// The weight option list is not replaced, only values in <paramref name="reader"/> will be added / changed.
	/// </summary>
	/// <param name="reader">The YAML node (containing a map) with the new values.</param>
	public void Load(YamlNodeReader reader)
	{
		foreach (var option in reader.Children())
		{
			// C++ reads size_t (negative weights are a read error); stored as int like the RNG range
			Set(option.ReadKey<string>(), unchecked((int)option.ReadVal<ulong>()));
		}
	}

	/// <summary>Store our list in YAML.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		if (_choices.Count == 0) // for compatibility reasons, we output null instead of empty map
		{
			writer.SetValueNull();
			return;
		}
		writer.SetAsMap();
		foreach (var pair in _choices)
			writer.Write(pair.Key, pair.Value);
	}

	/// <summary>Get the list of strings associated with these weights.</summary>
	public List<string> Names
	{
		get
		{
			var names = new List<string>();
			foreach (var pair in _choices)
			{
				names.Add(pair.Key);
			}
			return names;
		}
	}
}
