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
// Ported from: src/Mod/RuleSoldierBonus.h, src/Mod/RuleSoldierBonus.cpp
// Port status: partial (missing: script hooks)
#nullable disable

namespace Acom;

/// <summary>
/// Represents an assignable extra bonus to soldier's stats, regen and night vision.
/// Bonus is awarded either via SoldierCommendations or via SoldierTransformations.
/// </summary>
public class RuleSoldierBonus
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleSoldierBonus";

	private readonly string _name;

	private int _visibilityAtDark;
	private int _visibilityAtDay;
	private int _psiVision;
	private int _visibilityThroughSmoke;
	private int _visibilityThroughFire;

	private int _frontArmor, _sideArmor, _leftArmorDiff, _rearArmor, _underArmor;
	private UnitStats _stats;
	private int _listOrder;
	private readonly RuleStatBonus _timeRecovery = new(), _energyRecovery = new(), _moraleRecovery = new(), _healthRecovery = new(), _stunRecovery = new(), _manaRecovery = new();

	/// <summary>Creates a blank RuleSoldierBonus.</summary>
	public RuleSoldierBonus(string name, int listOrder)
	{
		_name = name;
		_frontArmor = 0;
		_sideArmor = 0;
		_leftArmorDiff = 0;
		_rearArmor = 0;
		_underArmor = 0;
		_listOrder = listOrder;
	}

	/// <summary>Loads the soldier bonus definition from YAML (C++ also takes the ModScript parsers, see port status).</summary>
	public void Load(YamlNodeReader node, Mod mod)
	{
		var reader = node.UseIndex();
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("visibilityAtDark", ref _visibilityAtDark);
		reader.TryRead("visibilityAtDay", ref _visibilityAtDay);
		reader.TryRead("psiVision", ref _psiVision);
		reader.TryRead("heatVision", ref _visibilityThroughSmoke);
		reader.TryRead("visibilityThroughFire", ref _visibilityThroughFire);

		reader.TryRead("frontArmor", ref _frontArmor);
		reader.TryRead("sideArmor", ref _sideArmor);
		reader.TryRead("leftArmorDiff", ref _leftArmorDiff);
		reader.TryRead("rearArmor", ref _rearArmor);
		reader.TryRead("underArmor", ref _underArmor);
		_stats.Merge(reader["stats"].ReadVal(_stats));
		var rec = reader["recovery"];
		{
			// property names of the ModScript::*SoldierRecoveryStatBonus parsers
			_timeRecovery.Load(_name, rec, "time");
			_energyRecovery.Load(_name, rec, "energy");
			_moraleRecovery.Load(_name, rec, "morale");
			_healthRecovery.Load(_name, rec, "health");
			_manaRecovery.Load(_name, rec, "mana");
			_stunRecovery.Load(_name, rec, "stun");
		}

		// _soldierBonusScripts and _scriptValues: script hooks missing

		reader.TryRead("listOrder", ref _listOrder);
	}

	/// <summary>Gets the soldier bonus unique name/type.</summary>
	public string Name { get => _name; }

	/// <summary>Gets the bonus to night vision (in tiles).</summary>
	public int VisibilityAtDark { get => _visibilityAtDark; }
	/// <summary>Gets the bonus to day vision (in tiles).</summary>
	public int VisibilityAtDay { get => _visibilityAtDay; }
	/// <summary>Gets the bonus to psi vision (in tiles).</summary>
	public int PsiVision { get => _psiVision; }
	/// <summary>Gets the bonus to heat vision.</summary>
	public int VisibilityThroughSmoke { get => _visibilityThroughSmoke; }
	/// <summary>Gets the bonus to visibility through fire.</summary>
	public int VisibilityThroughFire { get => _visibilityThroughFire; }

	/// <summary>Gets the bonus to front armor.</summary>
	public int FrontArmor { get => _frontArmor; }
	/// <summary>Gets the bonus to left side armor.</summary>
	public int LeftSideArmor { get => _sideArmor + _leftArmorDiff; }
	/// <summary>Gets the bonus to right side armor.</summary>
	public int RightSideArmor { get => _sideArmor; }
	/// <summary>Gets the bonus to rear armor.</summary>
	public int RearArmor { get => _rearArmor; }
	/// <summary>Gets the bonus to under armor.</summary>
	public int UnderArmor { get => _underArmor; }
	/// <summary>Gets the bonus stats.</summary>
	public UnitStats Stats { get => _stats; }
	/// <summary>Gets the list order for display purposes.</summary>
	public int ListOrder { get => _listOrder; }

	/// <summary>Gets the bonus TU recovery.</summary>
	public int GetTimeRecovery(BattleUnit unit) => _timeRecovery.GetBonus(unit);
	public RuleStatBonus TimeRecoveryRaw { get => _timeRecovery; }
	/// <summary>Gets the bonus Energy recovery.</summary>
	public int GetEnergyRecovery(BattleUnit unit) => _energyRecovery.GetBonus(unit);
	public RuleStatBonus EnergyRecoveryRaw { get => _energyRecovery; }
	/// <summary>Gets the bonus Morale recovery.</summary>
	public int GetMoraleRecovery(BattleUnit unit) => _moraleRecovery.GetBonus(unit);
	public RuleStatBonus MoraleRecoveryRaw { get => _moraleRecovery; }
	/// <summary>Gets the bonus Health recovery.</summary>
	public int GetHealthRecovery(BattleUnit unit) => _healthRecovery.GetBonus(unit);
	public RuleStatBonus HealthRecoveryRaw { get => _healthRecovery; }
	/// <summary>Gets the bonus Mana recovery.</summary>
	public int GetManaRecovery(BattleUnit unit) => _manaRecovery.GetBonus(unit);
	public RuleStatBonus ManaRecoveryRaw { get => _manaRecovery; }
	/// <summary>Gets the bonus Stun recovery.</summary>
	public int GetStunRegeneration(BattleUnit unit) => _stunRecovery.GetBonus(unit);
	public RuleStatBonus StunRegenerationRaw { get => _stunRecovery; }
}
