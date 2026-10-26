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
// Ported from: src/Mod/RuleSkill.h, src/Mod/RuleSkill.cpp
// Port status: partial (missing: script hooks)
#nullable disable

namespace Acom;

/// <summary>
/// Represents a soldier skill (an action usable in battle, optionally bound to weapons and soldier bonuses).
/// </summary>
public partial class RuleSkill
{
	/// <summary>Name of class used in script.</summary>
	public const string ScriptName = "RuleSkill";

	private readonly string _type;
	private BattleActionType _targetMode;
	private BattleType _compatibleBattleType;
	private bool _isPsiRequired;
	private bool _checkHandsOnly;
	private bool _checkHandsOnly2;
	private readonly RuleItemUseCostRule _cost = new();
	private readonly RuleItemUseFlatRule _flat = new();
	private readonly List<string> _compatibleWeaponNames = [];
	private readonly List<string> _requiredBonusNames = [];
	private readonly List<RuleItem> _compatibleWeapons = [];
	private readonly List<RuleSoldierBonus> _requiredBonuses = [];

	/// <summary>Creates a blank soldier skill ruleset.</summary>
	public RuleSkill(string type)
	{
		_type = type;
		_targetMode = BattleActionType.BA_NONE;
		_compatibleBattleType = BattleType.BT_NONE;
		_isPsiRequired = false;
		_checkHandsOnly = true;
		_checkHandsOnly2 = false;
	}

	/// <summary>Loads the skill from YAML (C++ also takes the ModScript parsers, see port status).</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		int targetMode = (int)reader["targetMode"].ReadVal(_targetMode);
		targetMode = targetMode < 0 ? 0 : targetMode;
		targetMode = targetMode > (int)BattleActionType.BA_CQB ? 0 : targetMode;
		_targetMode = (BattleActionType)targetMode;

		int compBattleType = (int)reader["battleType"].ReadVal(_compatibleBattleType);
		compBattleType = compBattleType < 0 ? 0 : compBattleType;
		compBattleType = compBattleType > (int)BattleType.BT_CORPSE ? 0 : compBattleType;
		_compatibleBattleType = (BattleType)compBattleType;

		reader.TryRead("isPsiRequired", ref _isPsiRequired);
		reader.TryRead("checkHandsOnly", ref _checkHandsOnly);
		reader.TryRead("checkHandsOnly2", ref _checkHandsOnly2);

		_cost.LoadCost(reader, "Use");
		_flat.LoadFlat(reader, "Use");

		mod.LoadUnorderedNames(_type, _compatibleWeaponNames, reader["compatibleWeapons"]);
		mod.LoadUnorderedNames(_type, _requiredBonusNames, reader["requiredBonuses"]);

		LoadScripts(reader, mod.ScriptParsers);
	}

	/// <summary>Cross link with other Rules.</summary>
	public void AfterLoad(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		mod.LinkRule(_compatibleWeapons, _compatibleWeaponNames);
		mod.LinkRule(_requiredBonuses, _requiredBonusNames);

		//remove not needed data
		_compatibleWeaponNames.Clear();
		_requiredBonusNames.Clear();
	}

	/// <summary>Gets the skill's type.</summary>
	public string Type { get => _type; }
	/// <summary>Gets the targeting mode this skill will be using.</summary>
	public BattleActionType TargetMode { get => _targetMode; }
	/// <summary>Gets the BattleType for potentially compatible items.</summary>
	public BattleType CompatibleBattleType { get => _compatibleBattleType; }
	/// <summary>Is Psi Skill required for this skill to work?</summary>
	public bool IsPsiRequired { get => _isPsiRequired; }
	/// <summary>Should the check for compatible items only consider the hands (or also the inventory and specialweapon)?</summary>
	public bool CheckHandsOnly() => _checkHandsOnly;
	/// <summary>Should the check for compatible items (by battle type) only consider the hands (or also the inventory and specialweapon)?</summary>
	public bool CheckHandsOnly2() => _checkHandsOnly2;
	/// <summary>Gets the use cost for this skill.</summary>
	public RuleItemUseCost Cost { get => _cost.GetDefault(); }
	/// <summary>Gets the flat vs. percentage cost flags for the use cost of this skill.</summary>
	public RuleItemUseFlat Flat { get => _flat.GetDefault(); }
	/// <summary>Gets the list of weapons which are compatible with this skill.</summary>
	public List<RuleItem> CompatibleWeapons { get => _compatibleWeapons; }
	/// <summary>Gets the list of required soldier bonuses for this skill.</summary>
	public List<RuleSoldierBonus> RequiredBonuses { get => _requiredBonuses; }
}
