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
// Ported from: src/Savegame/AlienBase.h, src/Savegame/AlienBase.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents an alien base on the world.
/// </summary>
public class AlienBase : Target
{
	private string _pactCountry = "";
	private string _race = "";
	private bool _inBattlescape, _discovered;
	private AlienDeployment _deployment;
	private int _startMonth;
	private int _minutesSinceLastHuntMissionGeneration;
	private int _genMissionCount;

	/// <summary>Initializes an alien base.</summary>
	public AlienBase(AlienDeployment deployment, int startMonth) : base()
	{
		_inBattlescape = false;
		_discovered = false;
		_deployment = deployment;
		_startMonth = startMonth;
		_genMissionCount = 0;
		// allow spawning hunt missions immediately after the base is created, i.e. no initial delay
		_minutesSinceLastHuntMissionGeneration = _deployment.HuntMissionMaxFrequency;
	}

	/// <summary>Loads the alien base from a YAML file.</summary>
	/// <param name="reader">YAML node.</param>
	public override void Load(YamlNodeReader reader)
	{
		base.Load(reader);
		reader.TryRead("pactCountry", ref _pactCountry);
		reader.TryRead("race", ref _race);
		reader.TryRead("inBattlescape", ref _inBattlescape);
		reader.TryRead("discovered", ref _discovered);
		reader.TryRead("startMonth", ref _startMonth);
		reader.TryRead("minutesSinceLastHuntMissionGeneration", ref _minutesSinceLastHuntMissionGeneration);
		reader.TryRead("genMissionCount", ref _genMissionCount);
	}

	/// <summary>Saves the alien base to a YAML file.</summary>
	public override void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		base.Save(writer);

		writer.Write("pactCountry", _pactCountry);
		writer.Write("race", _race);
		if (_inBattlescape)
			writer.Write("inBattlescape", _inBattlescape);
		if (_discovered)
			writer.Write("discovered", _discovered);
		writer.Write("deployment", _deployment.Type);
		writer.Write("startMonth", _startMonth);
		writer.Write("minutesSinceLastHuntMissionGeneration", _minutesSinceLastHuntMissionGeneration);
		writer.Write("genMissionCount", _genMissionCount);
	}

	/// <summary>The alien base's unique type used for savegame purposes.</summary>
	public override string Type => _deployment.MarkerName;

	/// <summary>The globe marker for the alien base (-1 if none).</summary>
	public override int Marker
	{
		get
		{
			if (!_discovered)
				return -1;
			return _deployment.MarkerIcon;
		}
	}

	/// <summary>The country this base has a pact with.</summary>
	public string PactCountry { get => _pactCountry; set => _pactCountry = value; }

	/// <summary>The alien race currently residing in the alien base.</summary>
	public string AlienRace { get => _race; set => _race = value; }

	/// <summary>Sets the alien base's battlescape status.</summary>
	/// <param name="inbattle">True if it's in battle, False otherwise.</param>
	public void SetInBattlescape(bool inbattle) => _inBattlescape = inbattle;

	/// <summary>Gets the alien base's battlescape status.</summary>
	public bool IsInBattlescape => _inBattlescape;

	/// <summary>Gets the alien base's geoscape status.</summary>
	public bool IsDiscovered => _discovered;

	/// <summary>Sets the alien base's geoscape status.</summary>
	/// <param name="discovered">Has the base been discovered?</param>
	public void SetDiscovered(bool discovered) => _discovered = discovered;

	/// <summary>The base's deployment; setting it resets the hunt mission timer.</summary>
	public AlienDeployment Deployment
	{
		get => _deployment;
		set
		{
			_deployment = value;

			// allow spawning hunt missions immediately after the base is upgraded, i.e. no initial delay
			_minutesSinceLastHuntMissionGeneration = _deployment.HuntMissionMaxFrequency;
		}
	}

	/// <summary>The month on which the base spawned.</summary>
	public int StartMonth { get => _startMonth; set => _startMonth = value; }

	/// <summary>The number of minutes passed since the last hunt mission was generated.</summary>
	public int MinutesSinceLastHuntMissionGeneration { get => _minutesSinceLastHuntMissionGeneration; set => _minutesSinceLastHuntMissionGeneration = value; }

	/// <summary>The number of genMissions generated so far by this base.</summary>
	public int GenMissionCount { get => _genMissionCount; set => _genMissionCount = value; }
}
