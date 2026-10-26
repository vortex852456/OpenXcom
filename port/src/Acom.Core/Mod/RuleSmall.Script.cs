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
// Ported from: src/Mod/RuleUfo.cpp, src/Mod/RuleCraft.h, src/Mod/RuleCraft.cpp, src/Mod/RuleSoldier.cpp, src/Mod/RuleInventory.cpp,
// src/Mod/RuleCountry.cpp, src/Mod/RuleResearch.cpp, src/Mod/RuleSoldierBonus.cpp, src/Mod/RuleSkill.cpp (script binding)
#nullable disable

using static Acom.ScriptParam;

namespace Acom;

/// <summary>Script helpers of <see cref="RuleCraftStats"/> (C++ <c>RuleCraftStats::addGetStatsScript</c>, <c>RuleUfoStats::addGetStatsScript</c>).</summary>
public static class RuleCraftStatsScript
{
	/// <summary>C++ <c>RuleCraftStats::addGetStatsScript&lt;Stat&gt;(b, prefix)</c>: field getters with description "Get field of ...".</summary>
	public static void AddGetStatsScript<T>(Bind<T> b, Func<T, RuleCraftStats> stat, string prefix) where T : class
	{
		b.AddField(prefix + "getFuelMax", t => stat(t).fuelMax);
		b.AddField(prefix + "getDamageMax", t => stat(t).damageMax);
		b.AddField(prefix + "getSpeedMax", t => stat(t).speedMax);
		b.AddField(prefix + "getAccel", t => stat(t).accel);
		b.AddField(prefix + "getRadarRange", t => stat(t).radarRange);
		b.AddField(prefix + "getRadarChance", t => stat(t).radarChance);
		b.AddField(prefix + "getSightRange", t => stat(t).sightRange);
		b.AddField(prefix + "getHitBonus", t => stat(t).hitBonus);
		b.AddField(prefix + "getAvoidBonus", t => stat(t).avoidBonus);
		b.AddField(prefix + "getAvoidBonus2", t => stat(t).avoidBonus2);
		b.AddField(prefix + "getPowerBonus", t => stat(t).powerBonus);
		b.AddField(prefix + "getArmor", t => stat(t).armor);
		b.AddField(prefix + "getShieldCapacity", t => stat(t).shieldCapacity);
		b.AddField(prefix + "getShieldRecharge", t => stat(t).shieldRecharge);
		b.AddField(prefix + "getShieldRechargeInGeoscape", t => stat(t).shieldRechargeInGeoscape);
		b.AddField(prefix + "getShieldBleedThrough", t => stat(t).shieldBleedThrough);
		b.AddField(prefix + "getMaxUnits", t => stat(t).soldiers);
		b.AddField(prefix + "getMaxVehiclesAndLargeSoldiers", t => stat(t).vehicles);
		b.AddField(prefix + "getMaxItems", t => stat(t).maxItems);
		//b.template addField<Stat, &RuleCraftStats::maxStorageSpace>(prefix + "getMaxStorageSpace");
	}

	/// <summary>C++ <c>getTypeScript(const T*, ScriptText&amp;)</c>.</summary>
	public static ScriptAction GetTypeScript<T>(Func<T, string> type) where T : class => (ref ScriptArgs a) =>
	{
		var r = a.Ptr<T>(0);
		a.SetText(1, r != null ? new ScriptText(type(r)) : ScriptText.empty);
	};

	/// <summary>Debug display <c>Name(&lt;field&gt;: "value")</c> used by most rules.</summary>
	public static Func<T, string> DebugDisplay<T>(string scriptName, string field, Func<T, string> value) where T : class
		=> r => r != null ? scriptName + "(" + field + ": \"" + value(r) + "\")" : "null";
}

public partial class RuleUfo
{
	private ScriptGroupContainer _ufoScripts;
	private readonly ScriptValues<RuleUfo> _scriptValues = new();

	/// <summary>Get script of this ufo (index from <see cref="ModScript"/>).</summary>
	public ScriptContainerEvents GetScript(int index) => (ScriptContainerEvents)_ufoScripts?.Get(index);

	/// <summary>Get all script values.</summary>
	public ScriptValues<RuleUfo> GetScriptValuesRaw() => _scriptValues;

	private void LoadScripts(YamlNodeReader reader, ModScript parsers)
	{
		_ufoScripts ??= parsers.ufoScripts.CreateContainer();
		_ufoScripts.Load(_type, reader, parsers.ufoScripts);
		_scriptValues.Load(reader, parsers.GetShared());
	}

	/// <summary>Register RuleUfo in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		var ar = new Bind<RuleUfo>(parser);

		ar.AddGet("getRadius", r => r.Radius);
		ar.AddGet("getWeaponRange", r => r.WeaponRange);
		ar.AddGet("getWeaponPower", r => r.WeaponPower);
		ar.AddGet("getWeaponReload", r => r.WeaponReload);

		RuleCraftStatsScript.AddGetStatsScript(ar, r => r._stats, "");

		ar.AddScriptValue(r => r._scriptValues, false);
		ar.AddDebugDisplay(RuleCraftStatsScript.DebugDisplay<RuleUfo>(ScriptName, "name", r => r.Type));
	}
}

public partial class RuleCraft
{
	private ScriptGroupContainer _craftScripts;
	private readonly ScriptValues<RuleCraft> _scriptValues = new();

	/// <summary>Get script of this craft (index from <see cref="ModScript"/>).</summary>
	public ScriptContainerEvents GetScript(int index) => (ScriptContainerEvents)_craftScripts?.Get(index);

	/// <summary>Get all script values.</summary>
	public ScriptValues<RuleCraft> GetScriptValuesRaw() => _scriptValues;

	private void LoadScripts(YamlNodeReader reader, ModScript parsers)
	{
		_craftScripts ??= parsers.craftScripts.CreateContainer();
		_craftScripts.Load(_type, reader, parsers.craftScripts);
		_scriptValues.Load(reader, parsers.GetShared());
	}

	/// <summary>Register Type in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		var b = new Bind<RuleCraft>(parser);

		b.Add("getType", [Bind<RuleCraft>.Self, TextRef], RuleCraftStatsScript.GetTypeScript<RuleCraft>(r => r.Type));

		b.AddGet("getWeaponsMax", r => r.Weapons);
		b.AddGet("getSoldiersMax", r => r.MaxUnits);
		b.AddGet("getVehiclesMax", r => r.MaxVehiclesAndLargeSoldiers);
		b.AddGet("getPilotsMax", r => r.Pilots);

		RuleCraftStatsScript.AddGetStatsScript(b, r => r._stats, "Stats.");

		b.AddScriptValue(r => r._scriptValues);
		b.AddDebugDisplay(rc => rc != null ? ScriptName + "(type: \"" + rc.Type + "\")" : "null");
	}
}

public partial class RuleSoldier
{
	private readonly ScriptValues<RuleSoldier> _scriptValues = new();

	/// <summary>Get all script values.</summary>
	public ScriptValues<RuleSoldier> GetScriptValuesRaw() => _scriptValues;

	/// <summary>Register RuleSoldier in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		var ra = new Bind<RuleSoldier>(parser);

		UnitStatsScript.AddGetStatsScript(ra, r => ref r._statCaps, "StatsCap.");
		UnitStatsScript.AddGetStatsScript(ra, r => ref r._minStats, "StatsMin.");
		UnitStatsScript.AddGetStatsScript(ra, r => ref r._maxStats, "StatsMax.");

		// C++ binds getTypeScript(const RuleSkill*, ScriptText&) here (bug kept: first argument is RuleSkill)
		ra.Add("getType", [Ptr<RuleSkill>(), TextRef], RuleCraftStatsScript.GetTypeScript<RuleSkill>(r => r.Type));

		ra.AddScriptValue(r => r._scriptValues, false);
		ra.AddDebugDisplay(RuleCraftStatsScript.DebugDisplay<RuleSoldier>(ScriptName, "name", r => r.Type));
	}
}

public partial class RuleInventory
{
	/// <summary>Register RuleInventory in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		var bu = new Bind<RuleInventory>(parser);

		bu.Add("getId", [Bind<RuleInventory>.Self, TextRef], RuleCraftStatsScript.GetTypeScript<RuleInventory>(r => r.Id));
		bu.Add("getType", [Bind<RuleInventory>.Self, IntRef], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<RuleInventory>(0);
			a.IntRef(1) = r != null ? (int)r.Type : 0;
		});
		bu.AddGet("isRightHand", r => r.IsRightHand);
		bu.AddGet("isLeftHand", r => r.IsLeftHand);
		bu.Add("getMoveToCost", [Bind<RuleInventory>.Self, IntRef, Ptr<RuleInventory>()], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<RuleInventory>(0);
			a.IntRef(1) = r != null ? r.GetCost(a.Ptr<RuleInventory>(2)) : 0;
		}, "Cost of moving item from slot in first arg to slot from last arg");

		bu.AddDebugDisplay(RuleCraftStatsScript.DebugDisplay<RuleInventory>(ScriptName, "id", r => r.Id));

		bu.AddCustomConst("INV_GROUND", (int)InventoryType.INV_GROUND);
		bu.AddCustomConst("INV_SLOT", (int)InventoryType.INV_SLOT);
		bu.AddCustomConst("INV_HAND", (int)InventoryType.INV_HAND);
	}
}

public partial class RuleCountry
{
	private ScriptGroupContainer _countryScripts;
	private readonly ScriptValues<RuleCountry> _scriptValues = new();

	/// <summary>Get script of this country (index from <see cref="ModScript"/>).</summary>
	public ScriptContainerEvents GetScript(int index) => (ScriptContainerEvents)_countryScripts?.Get(index);

	/// <summary>Get all script values.</summary>
	public ScriptValues<RuleCountry> GetScriptValuesRaw() => _scriptValues;

	private void LoadScripts(YamlNodeReader reader, ModScript parsers)
	{
		_countryScripts ??= parsers.countryScripts.CreateContainer();
		_countryScripts.Load(_type, reader, parsers.countryScripts);
		_scriptValues.Load(reader, parsers.GetShared());
	}

	/// <summary>Register RuleCountry in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		var rcb = new Bind<RuleCountry>(parser);

		rcb.AddGet("getFundingCap", r => r.FundingCap, "Gets the predefined max funding cap for this country.");

		rcb.AddScriptValue(r => r._scriptValues, false);
		rcb.AddDebugDisplay(RuleCraftStatsScript.DebugDisplay<RuleCountry>(ScriptName, "name", r => r.Type));
	}
}

public partial class RuleResearch
{
	private readonly ScriptValues<RuleResearch> _scriptValues = new();

	/// <summary>Get all script values.</summary>
	public ScriptValues<RuleResearch> GetScriptValuesRaw() => _scriptValues;

	/// <summary>Register RuleResearch in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		var ar = new Bind<RuleResearch>(parser);

		ar.AddGet("getCost", r => r.Cost);
		ar.AddGet("getPoints", r => r.Points);
		ar.AddGetPtr("getLookup", r => r.Lookup);
		ar.AddGetPtr("getNeededItem", r => r.NeededItem);

		ar.AddScriptValue(r => r._scriptValues, false);
		ar.AddDebugDisplay(RuleCraftStatsScript.DebugDisplay<RuleResearch>(ScriptName, "name", r => r.Name));
	}
}

public partial class RuleSoldierBonus
{
	private ScriptGroupContainer _soldierBonusScripts;
	private readonly ScriptValues<RuleSoldierBonus> _scriptValues = new();

	/// <summary>Get script of this bonus (index from <see cref="ModScript"/>).</summary>
	public ScriptContainerEvents GetScript(int index) => (ScriptContainerEvents)_soldierBonusScripts?.Get(index);

	/// <summary>Get all script values.</summary>
	public ScriptValues<RuleSoldierBonus> GetScriptValuesRaw() => _scriptValues;

	private void LoadScripts(YamlNodeReader reader, ModScript parsers)
	{
		_soldierBonusScripts ??= parsers.soldierBonusScripts.CreateContainer();
		_soldierBonusScripts.Load(_name, reader, parsers.soldierBonusScripts);
		_scriptValues.Load(reader, parsers.GetShared());
	}

	/// <summary>Register RuleSoldierBonus in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<Mod>();

		var rsb = new Bind<RuleSoldierBonus>(parser);

		UnitStatsScript.AddGetStatsScript(rsb, r => ref r._stats, "Stats.");
		rsb.AddScriptValue(r => r._scriptValues, false);
		rsb.AddDebugDisplay(RuleCraftStatsScript.DebugDisplay<RuleSoldierBonus>(ScriptName, "name", r => r.Name));
	}
}

public partial class RuleSkill
{
	private ScriptGroupContainer _skillScripts;
	private readonly ScriptValues<RuleSkill> _scriptValues = new();

	/// <summary>Get script of this skill (index from <see cref="ModScript"/>).</summary>
	public ScriptContainerEvents GetScript(int index) => (ScriptContainerEvents)_skillScripts?.Get(index);

	/// <summary>Get all script values.</summary>
	public ScriptValues<RuleSkill> GetScriptValuesRaw() => _scriptValues;

	private void LoadScripts(YamlNodeReader reader, ModScript parsers)
	{
		_scriptValues.Load(reader, parsers.GetShared());
		_skillScripts ??= parsers.skillScripts.CreateContainer();
		_skillScripts.Load(_type, reader, parsers.skillScripts);
	}

	/// <summary>Register RuleSkill in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		var rs = new Bind<RuleSkill>(parser);

		rs.Add("getType", [Bind<RuleSkill>.Self, TextRef], RuleCraftStatsScript.GetTypeScript<RuleSkill>(r => r.Type));

		rs.AddScriptValue(r => r._scriptValues);
		rs.AddDebugDisplay(RuleCraftStatsScript.DebugDisplay<RuleSkill>(ScriptName, "name", r => r.Type));
	}
}
