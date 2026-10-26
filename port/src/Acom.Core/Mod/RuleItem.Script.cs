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
// Ported from: src/Mod/RuleItem.h, src/Mod/RuleItem.cpp (script binding)
#nullable disable

using static Acom.ScriptParam;

namespace Acom;

public partial class RuleItem
{
	private ScriptGroupContainer _battleItemScripts;
	private readonly ScriptValues<RuleItem> _scriptValues = new();

	/// <summary>Get script of this item (C++ <c>getScript&lt;ModScript::X&gt;()</c>, index from <see cref="ModScript"/>).</summary>
	public IScriptContainer GetScript(int index) => _battleItemScripts?.Get(index);

	/// <summary>Get script with events of this item.</summary>
	public ScriptContainerEvents GetScriptEvents(int index) => (ScriptContainerEvents)GetScript(index);

	/// <summary>Get all script values.</summary>
	public ScriptValues<RuleItem> GetScriptValuesRaw() => _scriptValues;

	/// <summary>Load scripts and script values (C++ end of <c>RuleItem::load</c>).</summary>
	private void LoadScripts(YamlNodeReader reader, ModScript parsers)
	{
		_scriptValues.Load(reader, parsers.GetShared());

		_battleItemScripts ??= parsers.battleItemScripts.CreateContainer();
		_battleItemScripts.Load(_type, reader, parsers.battleItemScripts);
	}

	private static ScriptAction DamageToScript(Func<RuleDamageType, float> f) => (ref ScriptArgs a) =>
	{
		var rdt = a.Ptr<RuleDamageType>(0);
		a.IntRef(1) = rdt != null ? (int)(f(rdt) * a.Int(2)) : 0;
	};

	private static string DebugDisplayDamageType(RuleDamageType rdt)
	{
		if (rdt != null)
		{
			return "RuleDamageType(resist: " + (int)rdt.ResistType + " random: " + (int)rdt.RandomType + ")";
		}
		else
		{
			return "null";
		}
	}

	/// <summary>Register RuleItem in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		{
			const string name = "RuleDamageType";
			parser.RegisterRawPointerType<RuleDamageType>(name);
			var rs = new Bind<RuleDamageType>(parser, name);
			var self = Bind<RuleDamageType>.Self;

			rs.AddGet("isDirect", r => r.IsDirect, "if this damage type affects only one target");
			rs.Add("isAreaOfEffect", [self, IntRef], (ref ScriptArgs a) =>
			{
				var rdt = a.Ptr<RuleDamageType>(0);
				a.IntRef(1) = rdt != null ? (!rdt.IsDirect ? 1 : 0) : 0;
			}, "if this damage type can affect multiple targets");

			rs.Add("getResistType", [self, IntRef], (ref ScriptArgs a) =>
			{
				var rdt = a.Ptr<RuleDamageType>(0);
				a.IntRef(1) = rdt != null ? (int)rdt.ResistType : 0;
			}, "which damage resistance type is used for damage reduction");
			rs.Add("getRandomType", [self, IntRef], (ref ScriptArgs a) =>
			{
				var rdt = a.Ptr<RuleDamageType>(0);
				a.IntRef(1) = rdt != null ? (int)rdt.RandomType : 0;
			}, "how to calculate randomized weapon damage from the weapon's power");

			rs.Add("getArmorEffectiveness", [self, IntRef], (ref ScriptArgs a) =>
			{
				var rdt = a.Ptr<RuleDamageType>(0);
				a.IntRef(1) = rdt != null ? (int)Math.Round((double)(rdt.ArmorEffectiveness * 100), MidpointRounding.AwayFromZero) : 0;
			}, "how effective is a unit's armor against this damage, % (value multiplied by 100 compared to ruleset value)");

			ScriptParam[] damageSig = [self, IntRef, Int];
			rs.Add("getDamageToArmorPre", damageSig, DamageToScript(r => r.ToArmorPre), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToArmor", damageSig, DamageToScript(r => r.ToArmor), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToEnergy", damageSig, DamageToScript(r => r.ToEnergy), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToHealth", damageSig, DamageToScript(r => r.ToHealth), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToItem", damageSig, DamageToScript(r => r.ToItem), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToMana", damageSig, DamageToScript(r => r.ToMana), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToMorale", damageSig, DamageToScript(r => r.ToMorale), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToStun", damageSig, DamageToScript(r => r.ToStun), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToTile", damageSig, DamageToScript(r => r.ToTile), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToTime", damageSig, DamageToScript(r => r.ToTime), "calculated damage value multiplied by the corresponding modifier");
			rs.Add("getDamageToWound", damageSig, DamageToScript(r => r.ToWound), "calculated damage value multiplied by the corresponding modifier");

			rs.Add("getRandomDamage", [self, IntRef, Int, PtrE<Rng.RandomState>()], (ref ScriptArgs a) =>
			{
				var rdt = a.Ptr<RuleDamageType>(0);
				var rng = a.Ptr<Rng.RandomState>(3);
				ref int ret = ref a.IntRef(1);
				ret = 0;
				if (rdt != null && rng != null)
				{
					ret = rdt.GetRandomDamage(a.Int(2), (min, max) => rng.Generate(min, max));
				}
			}, "calculated damage value (based on weapon's power)");

			rs.AddDebugDisplay(DebugDisplayDamageType);
		}

		parser.RegisterPointerType<Mod>();

		var ri = new Bind<RuleItem>(parser);

		ri.AddCustomConst("BT_NONE", (int)BattleType.BT_NONE);
		ri.AddCustomConst("BT_FIREARM", (int)BattleType.BT_FIREARM);
		ri.AddCustomConst("BT_AMMO", (int)BattleType.BT_AMMO);
		ri.AddCustomConst("BT_MELEE", (int)BattleType.BT_MELEE);
		ri.AddCustomConst("BT_GRENADE", (int)BattleType.BT_GRENADE);
		ri.AddCustomConst("BT_PROXIMITYGRENADE", (int)BattleType.BT_PROXIMITYGRENADE);
		ri.AddCustomConst("BT_MEDIKIT", (int)BattleType.BT_MEDIKIT);
		ri.AddCustomConst("BT_SCANNER", (int)BattleType.BT_SCANNER);
		ri.AddCustomConst("BT_MINDPROBE", (int)BattleType.BT_MINDPROBE);
		ri.AddCustomConst("BT_PSIAMP", (int)BattleType.BT_PSIAMP);
		ri.AddCustomConst("BT_FLARE", (int)BattleType.BT_FLARE);
		ri.AddCustomConst("BT_CORPSE", (int)BattleType.BT_CORPSE);

		ri.Add("getType", [Bind<RuleItem>.Self, TextRef], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<RuleItem>(0);
			a.SetText(1, r != null ? new ScriptText(r.Type) : ScriptText.empty);
		});

		ri.AddGet("getAccuracyAimed", r => r.AccuracyAimed);
		ri.AddGet("getAccuracyAuto", r => r.AccuracyAuto);
		ri.AddGet("getAccuracyMelee", r => r.AccuracyMelee);
		ri.AddGet("getAccuracyMind", r => r.AccuracyMind);
		ri.AddGet("getAccuracyPanic", r => r.AccuracyPanic);
		ri.AddGet("getAccuracySnap", r => r.AccuracySnap);
		ri.AddGet("getAccuracyThrow", r => r.AccuracyThrow);
		ri.AddGet("getAccuracyUse", r => r.AccuracyUse);

		ri.AddGet("getPower", r => r.Power, "primary power, before applying unit bonuses, random rolls or other modifiers");
		ri.AddGetPtr("getDamageType", r => r.DamageType, "primary damage type");
		ri.AddGet("getMeleePower", r => r.MeleePower, "secondary power (gunbutt), before applying unit bonuses, random rolls or other modifiers");
		ri.AddGetPtr("getMeleeDamageType", r => r.MeleeType, "secondary damage type (gunbutt)");

		ri.AddGet("getArmorValue", r => r.Armor);
		ri.AddGet("getWeight", r => r.Weight);
		ri.Add("getBattleType", [Bind<RuleItem>.Self, IntRef], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<RuleItem>(0);
			a.IntRef(1) = r != null ? (int)r.BattleType : (int)BattleType.BT_NONE;
		});
		ri.AddGet("getWaypoints", r => r.Waypoints);
		ri.AddGet("isWaterOnly", r => r.IsWaterOnly);
		ri.AddGet("isTwoHanded", r => r.IsTwoHanded);
		ri.AddGet("isBlockingBothHands", r => r.IsBlockingBothHands);
		ri.Add("isSingleTarget", [Bind<RuleItem>.Self, IntRef], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<RuleItem>(0);
			a.IntRef(1) = r != null ? (r.DamageType.FixRadius == 0 ? 1 : 0) : 0;
		});
		ri.Add("hasCategory", [Bind<RuleItem>.Self, IntRef, ScriptParam.Text], (ref ScriptArgs a) =>
		{
			var r = a.Ptr<RuleItem>(0);
			a.IntRef(1) = r != null && r.Categories.Contains(a.Str(2)) ? 1 : 0;
		});

		ri.AddScriptValue(r => r._scriptValues, false);
		ri.AddDebugDisplay(r => r != null ? ScriptName + "(type: \"" + r.Type + "\")" : "null");
	}
}
