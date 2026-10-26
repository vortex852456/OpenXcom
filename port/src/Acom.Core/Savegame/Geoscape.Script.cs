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
// Ported from: src/Savegame/Country.cpp, src/Savegame/Craft.cpp, src/Savegame/Ufo.cpp, src/Savegame/Soldier.cpp (script binding)
#nullable disable

using System.Globalization;
using static Acom.ScriptParam;

namespace Acom;

public partial class Country
{
	private readonly ScriptValues<Country> _scriptValues = new();

	/// <summary>Get script values.</summary>
	public ScriptValues<Country> GetScriptValuesRaw() => _scriptValues;

	/// <summary>Register Country in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<RuleCountry>();

		var c = new Bind<Country>(parser);

		c.AddRules<RuleCountry>("getRuleCountry", x => x.Rules);

		c.AddGet("getPact", x => x.Pact, "Get if the country has signed an alien pact or not.");

		c.AddGet("getCurrentFunding", x => x.Funding[^1], "Get the country's current funding.");
		c.AddGet("getCurrentActivityAlien", x => x.ActivityAlien[^1], "Get the country's current alien activity.");
		c.AddGet("getCurrentActivityXcom", x => x.ActivityXcom[^1], "Get the country's current xcom activity.");

		c.AddScriptValue(x => x._scriptValues);
		c.AddDebugDisplay(x => x != null ? ScriptName + "(name: \"" + x.Rules.Type + "\")" : "null");

		c.AddCustomConst("SATISFACTION_ALIENPACT", 0);
		c.AddCustomConst("SATISFACTION_UNHAPPY", 1);
		c.AddCustomConst("SATISFACTION_SATISIFIED", 2);
		c.AddCustomConst("SATISFACTION_HAPPY", 3);
	}
}

public partial class Craft
{
	private readonly ScriptValues<Craft> _scriptValues = new();

	/// <summary>Get script values.</summary>
	public ScriptValues<Craft> GetScriptValuesRaw() => _scriptValues;

	/// <summary>Register Type in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<RuleCraft>();

		var b = new Bind<Craft>(parser);

		// C++ add<&Target::getId>: the self argument is the unregistered base class `const Target*`, so the binding is listed as `[ptr ]` and cannot be called
		b.Add("getId", [Ptr<Target>(), IntRef], (ref ScriptArgs a) => { var t = a.Ptr<Target>(0); a.IntRef(1) = t != null ? t.Id : 0; });

		b.AddGet("getDamage", x => x.Damage);
		b.AddField("getDamageMax", x => x._stats.damageMax);
		b.AddGet("getDamagePercentage", x => x.DamagePercentage);

		b.AddGet("getShield", x => x.Shield);
		b.AddField("getShieldMax", x => x._stats.shieldCapacity);
		b.AddGet("getShieldPercentage", x => x.ShieldPercentage);

		b.AddRules<RuleCraft>("getRuleCraft", x => x.Rules);

		RuleCraftStatsScript.AddGetStatsScript(b, x => x._stats, "Stats.");

		b.AddScriptValue(x => x._rules.GetScriptValuesRaw(), false);
		b.AddScriptValue(x => x._scriptValues);
		b.AddDebugDisplay(c => c != null
			? ScriptName + "(type: \"" + c.Type + "\" id: " + c.Id.ToString(CultureInfo.InvariantCulture) + " damage: " + c.DamagePercentage.ToString(CultureInfo.InvariantCulture) + "%)"
			: "null");
	}
}

public partial class Ufo
{
	private readonly ScriptValues<Ufo> _scriptValues = new();

	/// <summary>Get script values.</summary>
	public ScriptValues<Ufo> GetScriptValuesRaw() => _scriptValues;

	/// <summary>Register Ufo in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<RuleUfo>();

		var u = new Bind<Ufo>(parser);

		u.AddGet("getAltitude", x => x.AltitudeInt);
		// C++ add<&Target::getId>: the self argument is the unregistered base class `const Target*`, so the binding is listed as `[ptr ]` and cannot be called
		u.Add("getId", [Ptr<Target>(), IntRef], (ref ScriptArgs a) => { var t = a.Ptr<Target>(0); a.IntRef(1) = t != null ? t.Id : 0; });
		u.AddGetPtr("getRules", x => x.Rules);
		u.AddGet("getStatus", x => (int)x.Status);
		u.AddGet("getVisibility", x => x.Visibility);

		u.AddGet("getDamage", x => x.Damage);
		u.AddGet("getDamageMax", x => x.CraftStats.damageMax);
		u.AddGet("getDamagePercentage", x => x.DamagePercentage);

		u.AddGet("getShield", x => x.Shield);
		u.AddField("getShieldMax", x => x._stats.shieldCapacity);
		u.AddGet("getShieldPercentage", x => x.ShieldPercentage);

		u.AddGet("isHunterKiller", x => x.IsHunterKiller);
		u.AddGet("isHunting", x => x.IsHunting);
		u.AddGet("isEscorting", x => x.IsEscorting);
		u.AddGet("getHuntBehavior", x => x.HuntBehavior);
		u.AddGet("getHuntMode", x => x.HuntMode);

		u.AddGet("getDetected", x => x.Detected);
		u.AddGet("getHyperDetected", x => x.HyperDetected);

		u.AddRules<RuleUfo>("getRuleUfo", x => x.Rules);

		RuleCraftStatsScript.AddGetStatsScript(u, x => x._stats, "Stats.");

		u.AddScriptValue(x => x._rules.GetScriptValuesRaw(), false);
		u.AddScriptValue(x => x._scriptValues);
		// C++ prints a stray quote before "damage"
		u.AddDebugDisplay(x => x != null
			? ScriptName + "(type: \"" + x.Rules.Type + "\" id: " + x.Id.ToString(CultureInfo.InvariantCulture) + "\" damage: " + x.DamagePercentage.ToString(CultureInfo.InvariantCulture) + "%)"
			: "null");

		u.AddCustomConst("UFO_FLYING", (int)UfoStatus.FLYING);
		u.AddCustomConst("UFO_LANDED", (int)UfoStatus.LANDED);
		u.AddCustomConst("UFO_CRASHED", (int)UfoStatus.CRASHED);
		u.AddCustomConst("UFO_DESTROYED", (int)UfoStatus.DESTROYED);
		u.AddCustomConst("UFO_IGNORE_ME", (int)UfoStatus.IGNORE_ME);

		u.AddCustomConst("DETECTION_NONE", (int)UfoDetection.DETECTION_NONE);
		u.AddCustomConst("DETECTION_RADAR", (int)UfoDetection.DETECTION_RADAR);
		u.AddCustomConst("DETECTION_HYPERWAVE", (int)UfoDetection.DETECTION_HYPERWAVE);
	}
}

public partial class Soldier
{
	private readonly ScriptValues<Soldier> _scriptValues = new();

	/// <summary>Get script values.</summary>
	public ScriptValues<Soldier> GetScriptValuesRaw() => _scriptValues;

	/// <summary>Register Soldier in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<RuleSoldier>();

		var so = new Bind<Soldier>(parser);

		so.AddField("getId", x => x._id);
		so.AddGet("getRank", x => (int)x.Rank);
		so.AddGet("getGender", x => (int)x.Gender);
		so.AddGet("getLook", x => (int)x.Look);
		so.AddGet("getLookVariant", x => x.LookVariant);

		UnitStatsScript.AddGetStatsScript(so, x => ref x._currentStats, "Stats.");
		UnitStatsScript.AddSetStatsScript(so, x => ref x._currentStats, "Stats.");

		so.AddGetPtr("getRuleSoldier", x => x.Rules);
		so.AddGet("getWoundRecovery", x => x.WoundRecoveryInt);
		so.Add("setWoundRecovery", [Bind<Soldier>.SelfE, Int], (ref ScriptArgs a) => { a.Ptr<Soldier>(0)?.SetWoundRecovery(a.Int(1)); });
		so.AddGet("getManaMissing", x => x.ManaMissing);
		so.Add("setManaMissing", [Bind<Soldier>.SelfE, Int], (ref ScriptArgs a) => { var s = a.Ptr<Soldier>(0); if (s != null) s.ManaMissing = a.Int(1); });
		so.AddGet("getHealthMissing", x => x.HealthMissing);
		so.Add("setHealthMissing", [Bind<Soldier>.SelfE, Int], (ref ScriptArgs a) => { var s = a.Ptr<Soldier>(0); if (s != null) s.HealthMissing = a.Int(1); });

		so.AddScriptValue(x => x._rules.GetScriptValuesRaw(), false);
		so.AddScriptValue(x => x._scriptValues);
		so.AddDebugDisplay(x => x != null
			? ScriptName + "(type: \"" + x.Rules.Type + "\" id: " + x.Id.ToString(CultureInfo.InvariantCulture) + " name: \"" + x.GetName(false, 0) + "\")"
			: "null");
	}
}
