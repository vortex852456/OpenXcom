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
// Ported from: src/Mod/AlienRace.h, src/Mod/AlienRace.cpp
#nullable disable

namespace Acom;

/// <summary>Alien rank (index into the race members); declared outside the namespace in C++.</summary>
public enum AlienRank
{
	AR_HUMAN = -1,
	AR_COMMANDER = 0,
	AR_LEADER = 1,
	AR_ENGINEER = 2,
	AR_MEDIC = 3,
	AR_NAVIGATOR = 4,
	AR_SOLDIER = 5,
	AR_TERRORIST = 6,
	AR_TERRORIST2 = 7,
}

/// <summary>
/// Represents a specific race "family", or a "main race" if you wish.
/// Here is defined which ranks it contains and also which accompanying terror units.
/// </summary>
public class AlienRace
{
	private readonly string _id;
	private string _baseCustomDeploy = "";
	private string _baseCustomMission = "";
	private readonly List<(int first, WeightedOptions second)> _retaliationMissionDistribution = [];
	private List<string> _members = [];
	private List<List<string>> _membersRandom = [];
	private int _retaliationAggression;
	private int _listOrder;

	/// <summary>Creates a blank alien race.</summary>
	public AlienRace(string id, int listOrder)
	{
		_id = id;
		_retaliationAggression = 0;
		_listOrder = listOrder;
	}

	/// <summary>Loads the alien race from YAML.</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("baseCustomDeploy", ref _baseCustomDeploy);
		reader.TryRead("baseCustomMission", ref _baseCustomMission);
		reader.TryRead("members", ref _members);
		reader.TryRead("membersRandom", ref _membersRandom);
		reader.TryRead("retaliationAggression", ref _retaliationAggression);

		foreach (var weights in reader["retaliationMissionWeights"].Children())
		{
			var nw = new WeightedOptions();
			nw.Load(weights);
			_retaliationMissionDistribution.Add((weights.ReadKey(0), nw));
		}
		reader.TryRead("listOrder", ref _listOrder);
	}

	/// <summary>Gets the alien race's id (also its language string).</summary>
	public string Id { get => _id; }
	/// <summary>Gets the optional weapon deploy for aliens in their base.</summary>
	public string BaseCustomDeploy { get => _baseCustomDeploy; }
	/// <summary>Gets the custom alien base mission.</summary>
	public string BaseCustomMission { get => _baseCustomMission; }

	/// <summary>Gets a certain member of this alien race family (a random one of the rank if membersRandom is used).</summary>
	public string GetMember(int id)
	{
		if (_membersRandom.Count != 0)
		{
			if ((uint)id >= (uint)_membersRandom.Count)
			{
				throw new InvalidOperationException("Race " + _id + " does not have a random member at position/rank " + id);
			}
			int rng = Rng.Generate(0, _membersRandom[id].Count - 1);
			return _membersRandom[id][rng];
		}

		if ((uint)id >= (uint)_members.Count)
		{
			throw new InvalidOperationException("Race " + _id + " does not have a member at position/rank " + id);
		}
		return _members[id];
	}

	/// <summary>Gets the total number of members of this alien race family.</summary>
	public int Members
	{
		get
		{
			if (_membersRandom.Count != 0)
			{
				return _membersRandom.Count;
			}

			return _members.Count;
		}
	}

	/// <summary>Gets how aggressive aliens are to spawn a retaliation mission after destroying a ufo.</summary>
	public int RetaliationAggression { get => _retaliationAggression; }

	/// <summary>Returns a list of retaliation missions based on the given month.</summary>
	/// <param name="monthsPassed">The number of months that have passed in the game world.</param>
	/// <returns>The list of missions. Can be null.</returns>
	public WeightedOptions RetaliationMissionWeights(int monthsPassed)
	{
		if (_retaliationMissionDistribution.Count == 0)
			return null;

		int rw = _retaliationMissionDistribution.Count - 1;
		while (monthsPassed < _retaliationMissionDistribution[rw].first)
			--rw;
		return _retaliationMissionDistribution[rw].second;
	}

	/// <summary>Get the list weight for this alien race.</summary>
	public int ListOrder { get => _listOrder; }
}
