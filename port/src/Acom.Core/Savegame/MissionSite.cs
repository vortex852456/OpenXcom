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
// Ported from: src/Savegame/MissionSite.h, src/Savegame/MissionSite.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents an alien mission site on the world.
/// </summary>
public class MissionSite : Target
{
	private readonly RuleAlienMission _rules;
	private readonly AlienDeployment _deployment;
	private readonly AlienDeployment _missionCustomDeploy;
	private int _texture;
	/// <summary>C++ size_t.</summary>
	private int _secondsRemaining;
	private string _race = "", _city = "";
	private bool _inBattlescape, _detected;
	private Ufo _ufo;
	private int _ufoUniqueId;

	/// <summary>Initializes a mission site.</summary>
	public MissionSite(RuleAlienMission rules, AlienDeployment deployment, AlienDeployment alienWeaponDeploy) : base()
	{
		_rules = rules;
		_deployment = deployment;
		_missionCustomDeploy = alienWeaponDeploy;
		_texture = -1;
		_secondsRemaining = 0;
		_inBattlescape = false;
		_detected = false;
		_ufo = null;
		_ufoUniqueId = -1;
	}

	/// <summary>Loads the mission site from a YAML file.</summary>
	/// <param name="reader">YAML node.</param>
	public override void Load(YamlNodeReader reader)
	{
		base.Load(reader);
		reader.TryRead("texture", ref _texture);
		reader.TryRead("secondsRemaining", ref _secondsRemaining);
		reader.TryRead("race", ref _race);
		reader.TryRead("inBattlescape", ref _inBattlescape);
		reader.TryRead("detected", ref _detected);
		//_missionCustomDeploy loaded outside
		reader.TryRead("ufoUniqueId", ref _ufoUniqueId);
		// _ufo loaded outside
	}

	/// <summary>Saves the mission site to a YAML file.</summary>
	public override void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		base.Save(writer);

		writer.Write("type", _rules.Type);
		writer.Write("deployment", _deployment.Type);
		if (_missionCustomDeploy != null)
			writer.Write("missionCustomDeploy", _missionCustomDeploy.Type);
		writer.Write("texture", _texture);
		if (_secondsRemaining != 0)
			writer.Write("secondsRemaining", _secondsRemaining);
		writer.Write("race", _race);
		if (_inBattlescape)
			writer.Write("inBattlescape", _inBattlescape);
		writer.Write("detected", _detected);
		if (_ufo != null)
			writer.Write("ufoUniqueId", _ufo.UniqueId);
	}

	/// <summary>The mission's unique type used for savegame purposes.</summary>
	public override string Type => _deployment.MarkerName;

	/// <summary>The ruleset for the mission's type.</summary>
	public RuleAlienMission Rules => _rules;

	/// <summary>The ruleset for the mission's deployment.</summary>
	public AlienDeployment Deployment => _deployment;

	/// <summary>The mission's custom deployment of weapons (optional).</summary>
	public AlienDeployment MissionCustomDeploy => _missionCustomDeploy;

	/// <summary>The name on the globe for the mission.</summary>
	public override string MarkerName => Type;

	/// <summary>The globe marker for the mission site (-1 if none).</summary>
	public override int Marker
	{
		get
		{
			if (!_detected)
				return -1;
			if (_deployment.MarkerIcon == -1)
				return 5;
			return _deployment.MarkerIcon;
		}
	}

	/// <summary>The number of seconds remaining before the mission site expires.</summary>
	public int SecondsRemaining { get => _secondsRemaining; set => _secondsRemaining = value; }

	/// <summary>The alien race currently residing in the mission site.</summary>
	public string AlienRace { get => _race; set => _race = value; }

	/// <summary>Sets the mission site's battlescape status.</summary>
	/// <param name="inbattle">True if it's in battle, False otherwise.</param>
	public void SetInBattlescape(bool inbattle) => _inBattlescape = inbattle;

	/// <summary>Gets if the mission site is in battlescape.</summary>
	public bool IsInBattlescape => _inBattlescape;

	/// <summary>The mission site's associated texture.</summary>
	public int Texture { get => _texture; set => _texture = value; }

	/// <summary>The mission site's associated city, "" if none.</summary>
	public string City { get => _city; set => _city = value; }

	/// <summary>
	/// The detection state for this mission site.
	/// Used for popups of sites spawned directly rather than by UFOs.
	/// </summary>
	public bool Detected { get => _detected; set => _detected = value; }

	/// <summary>The mission site's corresponding Ufo.</summary>
	public Ufo Ufo { get => _ufo; set => _ufo = value; }

	/// <summary>DO NOT USE! Used only for loading saved games.</summary>
	public int UfoUniqueId => _ufoUniqueId;
}
