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
// Ported from: src/Mod/Unit.h, src/Mod/Unit.cpp (script binding)
#nullable disable

using static Acom.ScriptParam;

namespace Acom;

/// <summary>Reference to a <see cref="UnitStats"/> member of an object (C++ <c>UnitStats T::*</c>).</summary>
public delegate ref UnitStats UnitStatsRef<T>(T t);

/// <summary>Reference to an int member of an object (C++ <c>int T::*</c>).</summary>
public delegate ref int IntMemberRef<T>(T t);

/// <summary>Script helpers of <see cref="UnitStats"/> (C++ static templates of UnitStats).</summary>
public static class UnitStatsScript
{
	/// <summary>C++ <c>getMaxStatScript&lt;T, Stat, StatMax&gt;</c>: <c>(const T*, int&amp;)</c>.</summary>
	public static ScriptAction GetMaxStatScript<T>(UnitStatsRef<T> stat, UnitStats.Ptr statMax) where T : class => (ref ScriptArgs a) =>
	{
		var t = a.Ptr<T>(0);
		if (t != null)
		{
			a.IntRef(1) = stat(t)[statMax];
		}
		else
		{
			a.IntRef(1) = 0;
		}
	};

	/// <summary>C++ <c>setMaxStatScript&lt;T, Stat, StatMax&gt;</c>: <c>(T*, int)</c>.</summary>
	public static ScriptAction SetMaxStatScript<T>(UnitStatsRef<T> stat, UnitStats.Ptr statMax) where T : class => (ref ScriptArgs a) =>
	{
		var t = a.Ptr<T>(0);
		if (t != null)
		{
			SetMaxStat(ref stat(t), statMax, a.Int(1));
		}
	};

	private static void SetMaxStat(ref UnitStats s, UnitStats.Ptr statMax, int val)
	{
		val = Math.Min(Math.Max(val, 1), UnitStats.BaseStatLimit);
		s[statMax] = (short)val;
	}

	/// <summary>C++ <c>addMaxStatScript&lt;T, Stat, StatMax&gt;</c>: <c>(T*, int)</c>.</summary>
	public static ScriptAction AddMaxStatScript<T>(UnitStatsRef<T> stat, UnitStats.Ptr statMax) where T : class => (ref ScriptArgs a) =>
	{
		var t = a.Ptr<T>(0);
		if (t != null)
		{
			//limit range to prevent overflow
			int val = Math.Min(Math.Max(a.Int(1), -UnitStats.BaseStatLimit), UnitStats.BaseStatLimit);
			ref UnitStats s = ref stat(t);
			SetMaxStat(ref s, statMax, val + s[statMax]);
		}
	};

	private static void SetMaxAndCurrStat<T>(T t, UnitStatsRef<T> stat, IntMemberRef<T> curr, UnitStats.Ptr statMax, int val)
	{
		val = Math.Min(Math.Max(val, 1), UnitStats.BaseStatLimit);
		stat(t)[statMax] = (short)val;

		//update current value
		ref int c = ref curr(t);
		if (c > val)
		{
			c = val;
		}
	}

	/// <summary>C++ <c>setMaxAndCurrStatScript&lt;T, Stat, Curr, StatMax&gt;</c>: <c>(T*, int)</c>.</summary>
	public static ScriptAction SetMaxAndCurrStatScript<T>(UnitStatsRef<T> stat, IntMemberRef<T> curr, UnitStats.Ptr statMax) where T : class => (ref ScriptArgs a) =>
	{
		var t = a.Ptr<T>(0);
		if (t != null)
		{
			SetMaxAndCurrStat(t, stat, curr, statMax, a.Int(1));
		}
	};

	/// <summary>C++ <c>addMaxAndCurrStatScript&lt;T, Stat, Curr, StatMax&gt;</c>: <c>(T*, int)</c>.</summary>
	public static ScriptAction AddMaxAndCurrStatScript<T>(UnitStatsRef<T> stat, IntMemberRef<T> curr, UnitStats.Ptr statMax) where T : class => (ref ScriptArgs a) =>
	{
		var t = a.Ptr<T>(0);
		if (t != null)
		{
			//limit range to prevent overflow
			int val = Math.Min(Math.Max(a.Int(1), -UnitStats.BaseStatLimit), UnitStats.BaseStatLimit);
			SetMaxAndCurrStat(t, stat, curr, statMax, val + stat(t)[statMax]);
		}
	};

	/// <summary>C++ <c>UnitStats::addGetStatsScript&lt;Stat&gt;(b, prefix, skipResorcesStats)</c>.</summary>
	public static void AddGetStatsScript<T>(Bind<T> b, UnitStatsRef<T> stat, string prefix, bool skipResorcesStats = false) where T : class
	{
		ScriptParam[] sig = [Bind<T>.Self, IntRef];
		if (!skipResorcesStats)
		{
			b.Add(prefix + "getTimeUnits", sig, GetMaxStatScript(stat, UnitStats.Ptr.tu));
			b.Add(prefix + "getStamina", sig, GetMaxStatScript(stat, UnitStats.Ptr.stamina));
			b.Add(prefix + "getHealth", sig, GetMaxStatScript(stat, UnitStats.Ptr.health));
			b.Add(prefix + "getManaPool", sig, GetMaxStatScript(stat, UnitStats.Ptr.mana));
		}

		b.Add(prefix + "getBravery", sig, GetMaxStatScript(stat, UnitStats.Ptr.bravery));
		b.Add(prefix + "getReactions", sig, GetMaxStatScript(stat, UnitStats.Ptr.reactions));
		b.Add(prefix + "getFiring", sig, GetMaxStatScript(stat, UnitStats.Ptr.firing));
		b.Add(prefix + "getThrowing", sig, GetMaxStatScript(stat, UnitStats.Ptr.throwing));
		b.Add(prefix + "getStrength", sig, GetMaxStatScript(stat, UnitStats.Ptr.strength));
		b.Add(prefix + "getPsiStrength", sig, GetMaxStatScript(stat, UnitStats.Ptr.psiStrength));
		b.Add(prefix + "getPsiSkill", sig, GetMaxStatScript(stat, UnitStats.Ptr.psiSkill));
		b.Add(prefix + "getMelee", sig, GetMaxStatScript(stat, UnitStats.Ptr.melee));
	}

	/// <summary>C++ <c>UnitStats::addSetStatsScript&lt;Stat&gt;(b, prefix, skipResorcesStats)</c>.</summary>
	public static void AddSetStatsScript<T>(Bind<T> b, UnitStatsRef<T> stat, string prefix, bool skipResorcesStats = false) where T : class
	{
		ScriptParam[] sig = [Bind<T>.SelfE, Int];
		if (!skipResorcesStats)
		{
			b.Add(prefix + "setTimeUnits", sig, SetMaxStatScript(stat, UnitStats.Ptr.tu));
			b.Add(prefix + "setStamina", sig, SetMaxStatScript(stat, UnitStats.Ptr.stamina));
			b.Add(prefix + "setHealth", sig, SetMaxStatScript(stat, UnitStats.Ptr.health));
			b.Add(prefix + "setManaPool", sig, SetMaxStatScript(stat, UnitStats.Ptr.mana));

			b.Add(prefix + "addTimeUnits", sig, AddMaxStatScript(stat, UnitStats.Ptr.tu));
			b.Add(prefix + "addStamina", sig, AddMaxStatScript(stat, UnitStats.Ptr.stamina));
			b.Add(prefix + "addHealth", sig, AddMaxStatScript(stat, UnitStats.Ptr.health));
			b.Add(prefix + "addManaPool", sig, AddMaxStatScript(stat, UnitStats.Ptr.mana));
		}

		b.Add(prefix + "setBravery", sig, SetMaxStatScript(stat, UnitStats.Ptr.bravery));
		b.Add(prefix + "setReactions", sig, SetMaxStatScript(stat, UnitStats.Ptr.reactions));
		b.Add(prefix + "setFiring", sig, SetMaxStatScript(stat, UnitStats.Ptr.firing));
		b.Add(prefix + "setThrowing", sig, SetMaxStatScript(stat, UnitStats.Ptr.throwing));
		b.Add(prefix + "setStrength", sig, SetMaxStatScript(stat, UnitStats.Ptr.strength));
		b.Add(prefix + "setPsiStrength", sig, SetMaxStatScript(stat, UnitStats.Ptr.psiStrength));
		b.Add(prefix + "setPsiSkill", sig, SetMaxStatScript(stat, UnitStats.Ptr.psiSkill));
		b.Add(prefix + "setMelee", sig, SetMaxStatScript(stat, UnitStats.Ptr.melee));

		b.Add(prefix + "addBravery", sig, AddMaxStatScript(stat, UnitStats.Ptr.bravery));
		b.Add(prefix + "addReactions", sig, AddMaxStatScript(stat, UnitStats.Ptr.reactions));
		b.Add(prefix + "addFiring", sig, AddMaxStatScript(stat, UnitStats.Ptr.firing));
		b.Add(prefix + "addThrowing", sig, AddMaxStatScript(stat, UnitStats.Ptr.throwing));
		b.Add(prefix + "addStrength", sig, AddMaxStatScript(stat, UnitStats.Ptr.strength));
		b.Add(prefix + "addPsiStrength", sig, AddMaxStatScript(stat, UnitStats.Ptr.psiStrength));
		b.Add(prefix + "addPsiSkill", sig, AddMaxStatScript(stat, UnitStats.Ptr.psiSkill));
		b.Add(prefix + "addMelee", sig, AddMaxStatScript(stat, UnitStats.Ptr.melee));
	}

	/// <summary>C++ <c>UnitStats::addSetStatsWithCurrScript&lt;Stat, CurrTu, CurrEnergy, CurrHealth, CurrMana&gt;(b, prefix)</c>.</summary>
	public static void AddSetStatsWithCurrScript<T>(Bind<T> b, UnitStatsRef<T> stat, IntMemberRef<T> currTu, IntMemberRef<T> currEnergy, IntMemberRef<T> currHealth, IntMemberRef<T> currMana, string prefix) where T : class
	{
		ScriptParam[] sig = [Bind<T>.SelfE, Int];
		// when we change stats of BattleUnit its resources should be adjust.
		b.Add(prefix + "setTimeUnits", sig, SetMaxAndCurrStatScript(stat, currTu, UnitStats.Ptr.tu));
		b.Add(prefix + "setStamina", sig, SetMaxAndCurrStatScript(stat, currEnergy, UnitStats.Ptr.stamina));
		b.Add(prefix + "setHealth", sig, SetMaxAndCurrStatScript(stat, currHealth, UnitStats.Ptr.health));
		b.Add(prefix + "setManaPool", sig, SetMaxAndCurrStatScript(stat, currMana, UnitStats.Ptr.mana));

		b.Add(prefix + "addTimeUnits", sig, AddMaxAndCurrStatScript(stat, currTu, UnitStats.Ptr.tu));
		b.Add(prefix + "addStamina", sig, AddMaxAndCurrStatScript(stat, currEnergy, UnitStats.Ptr.stamina));
		b.Add(prefix + "addHealth", sig, AddMaxAndCurrStatScript(stat, currHealth, UnitStats.Ptr.health));
		b.Add(prefix + "addManaPool", sig, AddMaxAndCurrStatScript(stat, currMana, UnitStats.Ptr.mana));

		AddSetStatsScript(b, stat, prefix, true);
	}
}

public partial class Unit
{
	/// <summary>Register all useful function used by script.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		var un = new Bind<Unit>(parser);

		un.Add("getType", [Bind<Unit>.Self, TextRef], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<Unit>(0);
			a.SetText(1, r != null ? new ScriptText(r.Type) : ScriptText.empty);
		});
		un.AddGetPtr("getSpawnUnit", u => u.SpawnUnit);
		un.AddGetPtr("getCivilianRecoveryItemType", u => u.CivilianRecoveryItemType);
		// un.add<&Unit::getCivilianRecoverySoldierType>("getCivilianRecoverySoldierType"); //TODO: add `RuleSoldier` to scripts
		un.AddGetPtr("getLiveAlienGeoscape", u => u.LiveAlienGeoscape);

		un.AddDebugDisplay(unit => unit != null ? "RuleUnit(name: \"" + unit.Type + "\")" : "null");
	}

	/// <summary>Register StatAdjustment in script parser (C++ <c>StatAdjustment::ScriptRegister</c>).</summary>
	public static void ScriptRegisterStatAdjustment(ScriptParserBase parser)
	{
		var sa = new Bind<StatAdjustment>(parser);

		UnitStatsScript.AddGetStatsScript(sa, s => ref s.statGrowth, "");
	}
}
