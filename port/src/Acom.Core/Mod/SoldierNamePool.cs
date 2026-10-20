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
// Ported from: src/Mod/SoldierNamePool.h, src/Mod/SoldierNamePool.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Pool of soldier names to generate random names.
/// Each pool contains a set of first names (male or female) and last names.
/// The first names define the soldier's gender, and are randomly associated with a last name.
/// </summary>
public class SoldierNamePool
{
	private List<string> _maleFirst = [], _femaleFirst = [], _maleLast = [], _femaleLast = [], _maleCallsign = [], _femaleCallsign = [];
	private List<int> _lookWeights = [];
	private int _totalWeight, _femaleFrequency;
	private int _globalWeight;
	private string _country = "", _region = "";

	/// <summary>Initializes a new pool with blank lists of names.</summary>
	public SoldierNamePool()
	{
		_totalWeight = 0;
		_femaleFrequency = -1;
		_globalWeight = 100;
	}

	/// <summary>Loads the pool from a YAML file (path in the virtual file system).</summary>
	public void Load(string filename) => Load(FileMap.GetYaml(filename).ToBase(), filename);

	/// <summary>Loads the pool from already parsed YAML (<paramref name="filename"/> is for error messages).</summary>
	internal void Load(YamlNodeReader reader, string filename)
	{
		reader.TryRead("maleFirst", ref _maleFirst);
		reader.TryRead("femaleFirst", ref _femaleFirst);
		reader.TryRead("maleLast", ref _maleLast);
		reader.TryRead("femaleLast", ref _femaleLast);
		reader.TryRead("maleCallsign", ref _maleCallsign);
		reader.TryRead("femaleCallsign", ref _femaleCallsign);
		if (_femaleCallsign.Count == 0)
		{
			_femaleCallsign = [.. _maleCallsign];
		}
		if (_femaleFirst.Count == 0)
		{
			_femaleFirst = [.. _maleFirst];
		}
		if (_femaleLast.Count == 0)
		{
			_femaleLast = [.. _maleLast];
		}
		reader.TryRead("lookWeights", ref _lookWeights);
		_totalWeight = 0;
		foreach (int lw in _lookWeights)
		{
			_totalWeight += lw;
		}
		reader.TryRead("femaleFrequency", ref _femaleFrequency);
		reader.TryRead("globalWeight", ref _globalWeight);
		if (_globalWeight <= 0)
		{
			// can't let the modders break this completely
			_globalWeight = 100;
		}

		reader.TryRead("country", ref _country);
		reader.TryRead("region", ref _region);

		// Note: each name pool *instance* is only ever loaded once,
		// there are no overrides, so we can do checks here instead of needing afterLoad()
		if (_maleFirst.Count == 0)
		{
			throw new InvalidOperationException("A name pool cannot have an empty 'maleFirst:' list. File name: " + filename);
		}
		if (_femaleFirst.Count == 0)
		{
			throw new InvalidOperationException("A name pool cannot have an empty 'femaleFirst:' list. File name: " + filename);
		}
	}

	/// <summary>Returns a new random name (first + last) from the lists of names contained within.</summary>
	/// <param name="gender">Returned gender of the name.</param>
	/// <param name="femaleFrequency">Female percentage, used when the pool does not define its own.</param>
	public string GenName(ref SoldierGender gender, int femaleFrequency)
	{
		string name;
		bool female;
		if (_femaleFrequency > -1)
		{
			female = Rng.Percent(_femaleFrequency);
		}
		else
		{
			female = Rng.Percent(femaleFrequency);
		}

		if (!female)
		{
			gender = SoldierGender.GENDER_MALE;
			int first = Rng.Generate(0, _maleFirst.Count - 1);
			name = _maleFirst[first];
			if (_maleLast.Count != 0)
			{
				int last = Rng.Generate(0, _maleLast.Count - 1);
				name += " " + _maleLast[last];
			}
		}
		else
		{
			gender = SoldierGender.GENDER_FEMALE;
			int first = Rng.Generate(0, _femaleFirst.Count - 1);
			name = _femaleFirst[first];
			if (_femaleLast.Count != 0)
			{
				int last = Rng.Generate(0, _femaleLast.Count - 1);
				name += " " + _femaleLast[last];
			}
		}
		return name;
	}

	/// <summary>Returns a new random callsign from the lists of names contained within.</summary>
	public string GenCallsign(SoldierGender gender)
	{
		string callsign = "";
		if (_femaleCallsign.Count != 0)
		{
			if (gender == SoldierGender.GENDER_MALE)
			{
				int first = Rng.Generate(0, _maleCallsign.Count - 1);
				callsign = _maleCallsign[first];
			}
			else
			{
				int first = Rng.Generate(0, _femaleCallsign.Count - 1);
				callsign = _femaleCallsign[first];
			}
		}
		return callsign;
	}

	/// <summary>Generates the index of the soldier's look, when passed the maximum index value.</summary>
	/// <param name="numLooks">The maximum index.</param>
	public int GenLook(int numLooks)
	{
		int look = 0;
		const int minimumChance = 2; // minimum chance of a look being selected if it isn't enumerated. This ensures that looks MUST be zeroed to not appear.

		while (_lookWeights.Count < numLooks)
		{
			_lookWeights.Add(minimumChance);
			_totalWeight += minimumChance;
		}
		while (_lookWeights.Count > numLooks)
		{
			_totalWeight -= _lookWeights[^1];
			_lookWeights.RemoveAt(_lookWeights.Count - 1);
		}

		if (_totalWeight < 1)
		{
			return Rng.Generate(0, numLooks - 1);
		}

		int random = Rng.Generate(1, _totalWeight);
		foreach (int lw in _lookWeights)
		{
			if (random <= lw)
			{
				return look;
			}
			random -= lw;
			++look;
		}

		return Rng.Generate(0, numLooks - 1);
	}

	/// <summary>Gets a global weight of this name pool.</summary>
	public int GlobalWeight { get => _globalWeight; }

	/// <summary>Gets the country assigned to this name pool.</summary>
	public string Country { get => _country; }

	/// <summary>Gets the region assigned to this name pool.</summary>
	public string Region { get => _region; }
}
