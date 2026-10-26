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
// Ported from: src/Savegame/BattleUnit.h, src/Savegame/BattleUnit.cpp (script binding)
#nullable disable

using System.Globalization;
using static Acom.ScriptParam;

namespace Acom;

public partial class BattleUnit
{
	private readonly ScriptValues<BattleUnit> _scriptValues = new();

	/// <summary>Get script values.</summary>
	public ScriptValues<BattleUnit> GetScriptValuesRaw() => _scriptValues;

	private static readonly ScriptParam s_self = Bind<BattleUnit>.Self;
	private static readonly ScriptParam s_selfE = Bind<BattleUnit>.SelfE;

	private static ScriptAction StatusScript(UnitStatus status) => (ref ScriptArgs a) =>
	{
		var bu = a.Ptr<BattleUnit>(0);
		a.IntRef(1) = bu != null ? (bu.Status == status ? 1 : 0) : 0;
	};

	/// <summary>C++ <c>setBaseStatScript&lt;StatCurr, StatMax, NegativeLimitMult&gt;</c>.</summary>
	private static void SetBaseStat(BattleUnit bu, IntMemberRef<BattleUnit> statCurr, UnitStats.Ptr statMax, int negativeLimitMult, int val)
	{
		int max = bu.BaseStats[statMax];
		statCurr(bu) = BattleItem.ScriptClamp(val, -negativeLimitMult * max, +max);
	}

	private static ScriptAction SetBaseStatScript(IntMemberRef<BattleUnit> statCurr, UnitStats.Ptr statMax, int negativeLimitMult = 0) => (ref ScriptArgs a) =>
	{
		var bu = a.Ptr<BattleUnit>(0);
		if (bu != null)
		{
			SetBaseStat(bu, statCurr, statMax, negativeLimitMult, a.Int(1));
		}
	};

	private static ScriptAction AddBaseStatScript(IntMemberRef<BattleUnit> statCurr, UnitStats.Ptr statMax, int negativeLimitMult = 0) => (ref ScriptArgs a) =>
	{
		var bu = a.Ptr<BattleUnit>(0);
		if (bu != null)
		{
			//limit range to prevent overflow
			int val = BattleItem.ScriptClamp(a.Int(1), -UnitStats.BaseStatLimit, UnitStats.BaseStatLimit);
			SetBaseStat(bu, statCurr, statMax, negativeLimitMult, val + statCurr(bu));
		}
	};

	private static void SetStun(BattleUnit bu, int val)
	{
		bu._stunlevel = BattleItem.ScriptClamp(val, 0, bu.BaseStats.health * UnitStats.StunMultipler);
	}

	private static ScriptAction SetBaseStatRangeScript(IntMemberRef<BattleUnit> statCurr, int min, int max) => (ref ScriptArgs a) =>
	{
		var bu = a.Ptr<BattleUnit>(0);
		if (bu != null)
		{
			statCurr(bu) = BattleItem.ScriptClamp(a.Int(1), min, max);
		}
	};

	private static ScriptAction AddBaseStatRangeScript(IntMemberRef<BattleUnit> statCurr, int min, int max) => (ref ScriptArgs a) =>
	{
		var bu = a.Ptr<BattleUnit>(0);
		if (bu != null)
		{
			//limit range to prevent overflow
			int val = BattleItem.ScriptClamp(a.Int(1), -UnitStats.BaseStatLimit, UnitStats.BaseStatLimit);
			statCurr(bu) = BattleItem.ScriptClamp(val + statCurr(bu), min, max);
		}
	};

	private static ScriptAction GetInventoryItemScript(bool withSlot, bool withRules) => (ref ScriptArgs a) =>
	{
		var bu = a.Ptr<BattleUnit>(0);
		RuleInventory inv = withSlot ? a.Ptr<RuleInventory>(2) : null;
		RuleItem itemRules = withRules ? a.Ptr<RuleItem>(withSlot ? 3 : 2) : null;
		BattleItem foundItem = null;
		if (bu != null)
		{
			foreach (var i in bu.Inventory)
			{
				if ((!withSlot || i.Slot == inv) && (!withRules || i.Rules == itemRules))
				{
					foundItem = i;
					break;
				}
			}
		}
		a.SetPtr(1, foundItem);
	};

	private static ScriptAction GetListScript(Func<BattleUnit, IReadOnlyList<BattleItem>> member) => (ref ScriptArgs a) =>
	{
		var bu = a.Ptr<BattleUnit>(0);
		int i = a.Int(2);
		BattleItem foundItem = null;
		if (bu != null)
		{
			var ptr = member(bu);
			if ((uint)i < (uint)ptr.Count)
			{
				foundItem = ptr[i];
			}
		}
		a.SetPtr(1, foundItem);
	};

	private static string DebugDisplayScript(BattleUnit bu)
	{
		if (bu != null)
		{
			string s = ScriptName + "(type: \"" + bu.Type;
			var unit = bu.UnitRules;
			if (unit != null)
			{
				s += "\" race: \"" + unit.Race;
			}
			var soldier = bu.GeoscapeSoldier;
			if (soldier != null)
			{
				s += "\" name: \"" + soldier.GetName();
			}
			s += "\" id: " + bu.Id.ToString(CultureInfo.InvariantCulture);
			s += " faction: ";
			s += bu.Faction switch
			{
				UnitFaction.FACTION_HOSTILE => "Hostile",
				UnitFaction.FACTION_NEUTRAL => "Neutral",
				UnitFaction.FACTION_PLAYER => "Player",
				_ => "???",
			};
			s += " hp: " + bu.Health.ToString(CultureInfo.InvariantCulture) + "/" + bu.BaseStats.health.ToString(CultureInfo.InvariantCulture) + ")";
			return s;
		}
		else
		{
			return "null";
		}
	}

	/// <summary>Register BattleUnit in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		parser.RegisterPointerType<Mod>();
		parser.RegisterPointerType<Armor>();
		parser.RegisterPointerType<RuleSoldier>();
		parser.RegisterPointerType<BattleItem>();
		parser.RegisterPointerType<Soldier>();
		parser.RegisterPointerType<RuleSkill>();
		parser.RegisterPointerType<Unit>();
		parser.RegisterPointerType<RuleInventory>();

		var bu = new Bind<BattleUnit>(parser);

		bu.AddField("getId", u => u._id);
		bu.AddField("getRank", u => u._rankInt);
		bu.AddField("getRankUnified", u => u._rankIntUnified);
		bu.AddGet("getGender", u => (int)u.Gender);
		bu.AddGet("getLook", u => u.GeoscapeSoldier != null ? (int)u.GeoscapeSoldier.Look : 0);
		bu.AddGet("getLookVariant", u => u.GeoscapeSoldier != null ? u.GeoscapeSoldier.LookVariant : 0);
		bu.Add("getRecolor", [s_self, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null)
			{
				ref int pixel = ref a.IntRef(1);
				var vec = u.Recolor;
				int g = pixel & ShaderDrawHelper.ColorGroup;
				int s = pixel & ShaderDrawHelper.ColorShade;
				foreach (var p in vec)
				{
					if (g == p.first)
					{
						pixel = s + p.second;
						return;
					}
				}
			}
		});
		bu.AddGet("isFloating", u => u.IsFloating);
		bu.AddGet("isKneeled", u => u.IsKneeled);
		bu.Add("isStunned", [s_self, IntRef], StatusScript(UnitStatus.STATUS_UNCONSCIOUS));
		bu.Add("isKilled", [s_self, IntRef], StatusScript(UnitStatus.STATUS_DEAD));
		bu.Add("isStanding", [s_self, IntRef], StatusScript(UnitStatus.STATUS_STANDING));
		bu.Add("isWalking", [s_self, IntRef], StatusScript(UnitStatus.STATUS_WALKING));
		bu.Add("isFlying", [s_self, IntRef], StatusScript(UnitStatus.STATUS_FLYING));
		bu.Add("isCollapsing", [s_self, IntRef], StatusScript(UnitStatus.STATUS_COLLAPSING));
		bu.Add("isAiming", [s_self, IntRef], StatusScript(UnitStatus.STATUS_AIMING));
		bu.AddGet("isFearable", u => u.IsFearable);
		bu.AddGet("isWoundable", u => u.IsWoundable);
		bu.AddGet("getReactionScore", u => (int)u.ReactionScore);
		bu.AddGet("getDirection", u => u.Direction);
		bu.AddGet("getIntelligence", u => u.Intelligence);
		bu.AddGet("getAggression", u => u.Aggression);
		bu.AddGet("getTurretDirection", u => u.TurretDirection);
		bu.AddGet("getWalkingPhase", u => u.WalkingPhase);
		bu.Add("disableIndicators", [s_selfE], (ref ScriptArgs a) => { a.Ptr<BattleUnit>(0)?.DisableIndicators(); });

		bu.AddGet("isVisible", u => u.Visible);
		bu.Add("makeVisible", [s_selfE], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null)
			{
				u.Visible = true;
			}
		});

		bu.Add("getMaxViewDistanceAtDark", [s_self, IntRef, Ptr<BattleUnit>()], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? u.GetMaxViewDistanceAtDark(a.Ptr<BattleUnit>(2)) : 0;
		}, "get maximum visibility distance in tiles to another unit at dark");
		bu.Add("getMaxViewDistanceAtDay", [s_self, IntRef, Ptr<BattleUnit>()], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? u.GetMaxViewDistanceAtDay(a.Ptr<BattleUnit>(2)) : 0;
		}, "get maximum visibility distance in tiles to another unit at day");
		bu.Add("getMaxViewDistance", [s_self, IntRef, Int, Int, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? u.GetMaxViewDistance(a.Int(2), a.Int(3), a.Int(4)) : 0;
		}, "calculate maximum visibility distance consider camouflage, first arg is base visibility, second arg is cammo reduction, third arg is anti-cammo boost");
		bu.AddGet("getPsiVision", u => u.PsiVision);
		bu.AddGet("getHeatVision", u => u.VisibilityThroughSmoke, "getVisibilityThroughSmoke");
		bu.AddGet("getVisibilityThroughFire", u => u.VisibilityThroughFire, "getVisibilityThroughFire");

		bu.Add("setSpawnUnit", [s_selfE, Ptr<Unit>()], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			var unitType = a.Ptr<Unit>(1);
			if (u != null && unitType != null && u.Armor.Size >= unitType.Armor.Size)
			{
				u.SpawnUnit = unitType;
				u.Respawn = true;
				u.SpawnUnitFaction = UnitFaction.FACTION_HOSTILE;
			}
			else if (u != null)
			{
				u.ClearSpawnUnit();
			}
		}, "set type of zombie will be spawn from current unit, it will reset everything to default (hostile & instant)");
		bu.Add("getSpawnUnit", [s_selfE, PtrRef<Unit>()], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.SetPtr(1, u?.SpawnUnit);
		}, "get type of zombie will be spawn from current unit");
		bu.Add("setSpawnUnitInstantRespawn", [s_selfE, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null && u.SpawnUnit != null)
			{
				u.Respawn = a.Int(1) != 0;
			}
		}, "set 1 to make unit instantly change to spawn zombie unit, other wise it will transform on death");
		bu.Add("getSpawnUnitInstantRespawn", [s_selfE, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? (u.Respawn ? 1 : 0) : 0;
		}, "get state of instant respawn");
		bu.Add("setSpawnUnitFaction", [s_selfE, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int faction = a.Int(1);
			if (u != null && u.SpawnUnit != null)
			{
				if (faction >= (int)UnitFaction.FACTION_PLAYER && faction <= (int)UnitFaction.FACTION_NEUTRAL)
				{
					u.SpawnUnitFaction = (UnitFaction)faction;
				}
			}
		}, "set faction of unit that will spawn");
		bu.Add("getSpawnUnitFaction", [s_selfE, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? (int)u.SpawnUnitFaction : 0;
		}, "get faction of unit that will spawn");

		bu.AddPair<BattleUnit>("getPreviousOwner", u => u.PreviousOwner);

		bu.AddField("getTimeUnits", u => u._tu);
		bu.Add("getTimeUnitsMax", [s_self, IntRef], UnitStatsScript.GetMaxStatScript<BattleUnit>(u => ref u._stats, UnitStats.Ptr.tu));
		bu.Add("setTimeUnits", [s_selfE, Int], SetBaseStatScript(u => ref u._tu, UnitStats.Ptr.tu));
		bu.Add("addTimeUnits", [s_selfE, Int], AddBaseStatScript(u => ref u._tu, UnitStats.Ptr.tu));

		bu.AddField("getHealth", u => u._health);
		bu.Add("getHealthMax", [s_self, IntRef], UnitStatsScript.GetMaxStatScript<BattleUnit>(u => ref u._stats, UnitStats.Ptr.health));
		bu.Add("setHealth", [s_selfE, Int], SetBaseStatScript(u => ref u._health, UnitStats.Ptr.health));
		bu.Add("addHealth", [s_selfE, Int], AddBaseStatScript(u => ref u._health, UnitStats.Ptr.health));
		bu.Add("setHealthWithOverkill", [s_selfE, Int], SetBaseStatScript(u => ref u._health, UnitStats.Ptr.health, UnitStats.OverkillMultipler), "same as setHealth but allow negative health values like with Overkill");
		bu.Add("addHealthWithOverkill", [s_selfE, Int], AddBaseStatScript(u => ref u._health, UnitStats.Ptr.health, UnitStats.OverkillMultipler), "same as addHealth but allow negative health values like with Overkill");

		bu.AddField("getMana", u => u._mana);
		bu.Add("getManaMax", [s_self, IntRef], UnitStatsScript.GetMaxStatScript<BattleUnit>(u => ref u._stats, UnitStats.Ptr.mana));
		bu.Add("setMana", [s_selfE, Int], SetBaseStatScript(u => ref u._mana, UnitStats.Ptr.mana));
		bu.Add("addMana", [s_selfE, Int], AddBaseStatScript(u => ref u._mana, UnitStats.Ptr.mana));

		bu.AddField("getEnergy", u => u._energy);
		bu.Add("getEnergyMax", [s_self, IntRef], UnitStatsScript.GetMaxStatScript<BattleUnit>(u => ref u._stats, UnitStats.Ptr.stamina));
		bu.Add("setEnergy", [s_selfE, Int], SetBaseStatScript(u => ref u._energy, UnitStats.Ptr.stamina));
		bu.Add("addEnergy", [s_selfE, Int], AddBaseStatScript(u => ref u._energy, UnitStats.Ptr.stamina));

		bu.AddField("getStun", u => u._stunlevel);
		bu.Add("getStunMax", [s_self, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? u.BaseStats.health * UnitStats.StunMultipler : 0;
		});
		bu.Add("setStun", [s_selfE, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null) SetStun(u, a.Int(1));
		});
		bu.Add("addStun", [s_selfE, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null)
			{
				//limit range to prevent overflow, 4 time bigger than normal as stun can be 4 time bigger than health
				int val = BattleItem.ScriptClamp(a.Int(1), -UnitStats.StunStatLimit, UnitStats.StunStatLimit);
				SetStun(u, val + u._stunlevel);
			}
		});

		bu.AddField("getMorale", u => u._morale);
		bu.AddFake("getMoraleMax", 100);
		bu.Add("setMorale", [s_selfE, Int], SetBaseStatRangeScript(u => ref u._morale, 0, 100));
		bu.Add("addMorale", [s_selfE, Int], AddBaseStatRangeScript(u => ref u._morale, 0, 100));

		bu.AddGet("getFire", u => u.Fire);
		bu.Add("setFire", [s_selfE, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null)
			{
				u.Fire = BattleItem.ScriptClamp(a.Int(1), 0, UnitStats.BaseStatLimit);
			}
		});

		bu.Add("setArmor", [s_selfE, Int, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int side = a.Int(1);
			if (u != null && 0 <= side && side < (int)UnitSide.SIDE_MAX)
			{
				u.SetArmor(a.Int(2), (UnitSide)side);
			}
		}, "first arg is side, second one is new value of armor");
		bu.Add("addArmor", [s_selfE, Int, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int side = a.Int(1);
			if (u != null && 0 <= side && side < (int)UnitSide.SIDE_MAX)
			{
				//limit range to prevent overflow
				int value = BattleItem.ScriptClamp(a.Int(2), -UnitStats.BaseStatLimit, UnitStats.BaseStatLimit);
				u.SetArmor(value + u.GetArmor((UnitSide)side), (UnitSide)side);
			}
		}, "first arg is side, second one is value to add to armor");
		bu.Add("getArmor", [s_self, IntRef, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int side = a.Int(2);
			a.IntRef(1) = u != null && 0 <= side && side < (int)UnitSide.SIDE_MAX ? u.GetArmor((UnitSide)side) : 0;
		}, "first arg return armor value, second arg is side");
		bu.Add("setArmorMax", [s_selfE, Int, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int side = a.Int(1);
			if (u != null && 0 <= side && side < (int)UnitSide.SIDE_MAX)
			{
				u.SetMaxArmor(a.Int(2), (UnitSide)side);
			}
		}, "first arg is side, second one is new value of max armor");
		bu.Add("getArmorMax", [s_self, IntRef, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int side = a.Int(2);
			a.IntRef(1) = u != null && 0 <= side && side < (int)UnitSide.SIDE_MAX ? u.GetMaxArmor((UnitSide)side) : 0;
		}, "first arg return max armor value, second arg is side");

		bu.AddGet("getFatalwoundsTotal", u => u.FatalWounds, "sum for every body part");
		bu.Add("setFatalwounds", [s_selfE, Int, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int part = a.Int(1);
			if (u != null && 0 <= part && part < (int)UnitBodyPart.BODYPART_MAX)
			{
				u.SetFatalWound(a.Int(2), (UnitBodyPart)part);
			}
		}, "first arg is body part, second one is new value of wounds");
		bu.Add("addFatalwounds", [s_selfE, Int, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int part = a.Int(1);
			if (u != null && 0 <= part && part < (int)UnitBodyPart.BODYPART_MAX)
			{
				//limit range to prevent overflow
				int val = BattleItem.ScriptClamp(a.Int(2), -UnitStats.BaseStatLimit, UnitStats.BaseStatLimit);
				u.SetFatalWound(val + u.GetFatalWound((UnitBodyPart)part), (UnitBodyPart)part);
			}
		}, "first arg is body part, second one is value to add to wounds");
		bu.Add("getFatalwounds", [s_self, IntRef, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int part = a.Int(2);
			a.IntRef(1) = u != null && 0 <= part && part < (int)UnitBodyPart.BODYPART_MAX ? u.GetFatalWound((UnitBodyPart)part) : 0;
		}, "first arg return wounds number, second arg is body part");
		bu.Add("getFatalwoundsMax", [s_self, IntRef, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int part = a.Int(2);
			a.IntRef(1) = u != null && 0 <= part && part < (int)UnitBodyPart.BODYPART_MAX ? 100 : 0;
		}, "first arg return max wounds number, second arg is body part");

		UnitStatsScript.AddGetStatsScript(bu, u => ref u._stats, "Stats.");
		UnitStatsScript.AddSetStatsWithCurrScript(bu, u => ref u._stats, u => ref u._tu, u => ref u._energy, u => ref u._health, u => ref u._mana, "Stats.");

		UnitStatsScript.AddGetStatsScript(bu, u => ref u._exp, "Exp.", true);

		ScriptAction getMovmentType = (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? (int)u.MovementType : 0;
		};
		bu.Add("getMovmentType", [s_self, IntRef], getMovmentType, BindBase.functionInvisible); //old bugged name
		bu.Add("getMovementType", [s_self, IntRef], getMovmentType, "get move type of unit");
		bu.Add("getOriginalMovementType", [s_self, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? (int)u.OriginalMovementType : 0;
		}, "get original move type of unit");
		bu.Add("setMovementType", [s_selfE, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int type = a.Int(1);
			if (u != null && 0 <= type && type <= (int)MovementType.MT_SLIDE)
			{
				u.MovementType = (MovementType)type;
			}
		}, "set move type of unit");

		bu.AddField("MoveCost.getBaseTimePercent", "MoveCost.setBaseTimePercent", u => u._moveCostBase.TimePercent, (u, v) => u._moveCostBase.TimePercent = v);
		bu.AddField("MoveCost.getBaseEnergyPercent", "MoveCost.setBaseEnergyPercent", u => u._moveCostBase.EnergyPercent, (u, v) => u._moveCostBase.EnergyPercent = v);
		bu.AddField("MoveCost.getBaseFlyTimePercent", "MoveCost.setBaseFlyTimePercent", u => u._moveCostBaseFly.TimePercent, (u, v) => u._moveCostBaseFly.TimePercent = v);
		bu.AddField("MoveCost.getBaseFlyEnergyPercent", "MoveCost.setBaseFlyEnergyPercent", u => u._moveCostBaseFly.EnergyPercent, (u, v) => u._moveCostBaseFly.EnergyPercent = v);
		bu.AddField("MoveCost.getBaseClimbTimePercent", "MoveCost.setBaseClimbTimePercent", u => u._moveCostBaseClimb.TimePercent, (u, v) => u._moveCostBaseClimb.TimePercent = v);
		bu.AddField("MoveCost.getBaseClimbEnergyPercent", "MoveCost.setBaseClimbEnergyPercent", u => u._moveCostBaseClimb.EnergyPercent, (u, v) => u._moveCostBaseClimb.EnergyPercent = v);
		bu.AddField("MoveCost.getBaseNormalTimePercent", "MoveCost.setBaseNormalTimePercent", u => u._moveCostBaseNormal.TimePercent, (u, v) => u._moveCostBaseNormal.TimePercent = v);
		bu.AddField("MoveCost.getBaseNormalEnergyPercent", "MoveCost.setBaseNormalEnergyPercent", u => u._moveCostBaseNormal.EnergyPercent, (u, v) => u._moveCostBaseNormal.EnergyPercent = v);

		bu.Add("getVisibleUnitsCount", [s_selfE, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null)
			{
				a.IntRef(1) = u.VisibleUnits.Count;
			}
		});
		bu.AddGet("getFaction", u => (int)u.Faction, "get current faction of unit");
		bu.AddGet("getOriginalFaction", u => (int)u.OriginalFaction, "get original faction of unit");

		bu.AddGet("getOverKillDamage", u => u.OverKillDamage);
		bu.AddRules<Armor>("getRuleArmor", u => u.Armor);
		bu.AddGetPtr("getRuleUnit", u => u.UnitRules); // Note: can be nullptr
		bu.AddGetPtr("getRuleSoldier", u => u.GeoscapeSoldier?.Rules);
		bu.AddGetPtrE("getGeoscapeSoldier", u => u.GeoscapeSoldier);
		bu.AddGetPtr("getGeoscapeSoldier", u => u.GeoscapeSoldier);
		bu.AddFunc("reduceByBravery", [s_self, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null)
			{
				ref int ret = ref a.IntRef(1);
				ret = u.ReduceByBravery(ret);
			}
			return ScriptArg.RetContinue;
		}, "change first arg1 to `(110 - bravery) * arg1 / 100`");
		bu.AddFunc("reduceByResistance", [s_self, IntRef, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int resistType = a.Int(2);
			if (u != null)
			{
				if (resistType >= 0 && resistType < (int)ItemDamageType.DAMAGE_TYPES)
				{
					ref int ret = ref a.IntRef(1);
					ret = u.ReduceByResistance(ret, (ItemDamageType)resistType);
				}
			}
			return ScriptArg.RetContinue;
		}, "change first arg1 to `arg1 * resist[arg2]`");

		bu.AddGetPtrE("getRightHandWeapon", u => u.RightHandWeapon);
		bu.AddGetPtr("getRightHandWeapon", u => u.RightHandWeapon);
		bu.AddGetPtrE("getLeftHandWeapon", u => u.LeftHandWeapon);
		bu.AddGetPtr("getLeftHandWeapon", u => u.LeftHandWeapon);
		bu.Add("getInventoryItem", [s_selfE, PtrERef<BattleItem>(), Ptr<RuleItem>()], GetInventoryItemScript(false, true));
		bu.Add("getInventoryItem", [s_selfE, PtrERef<BattleItem>(), Ptr<RuleInventory>(), Ptr<RuleItem>()], GetInventoryItemScript(true, true));
		bu.Add("getInventoryItem", [s_selfE, PtrERef<BattleItem>(), Ptr<RuleInventory>()], GetInventoryItemScript(true, false));
		bu.Add("getInventoryItem", [s_self, PtrRef<BattleItem>(), Ptr<RuleItem>()], GetInventoryItemScript(false, true));
		bu.Add("getInventoryItem", [s_self, PtrRef<BattleItem>(), Ptr<RuleInventory>(), Ptr<RuleItem>()], GetInventoryItemScript(true, true));
		bu.Add("getInventoryItem", [s_self, PtrRef<BattleItem>(), Ptr<RuleInventory>()], GetInventoryItemScript(true, false));
		bu.Add("getInventoryItem.size", [s_self, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? u._inventory.Count : 0;
		});
		bu.Add("getInventoryItem", [s_selfE, PtrERef<BattleItem>(), Int], GetListScript(u => u._inventory));
		bu.Add("getInventoryItem", [s_self, PtrRef<BattleItem>(), Int], GetListScript(u => u._inventory));
		bu.AddList<BattleItem>("getInventoryItem", s_selfE, [], PtrERef<BattleItem>(), u => u._inventory, (BattleUnit u, BattleItem item, ref ScriptArgs a, int off) => item != null);
		bu.AddList<BattleItem>("getInventoryItem", s_self, [], PtrRef<BattleItem>(), u => u._inventory, (BattleUnit u, BattleItem item, ref ScriptArgs a, int off) => item != null);
		bu.Add("getSpecialItem.size", [s_self, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			int i = 0;
			if (u != null)
			{
				//count number of elements until null, and interpret this as size of array
				int idx = Array.IndexOf(u._specWeapon, null);
				i = idx < 0 ? u._specWeapon.Length : idx;
			}
			a.IntRef(1) = i;
		});
		bu.Add("getSpecialItem", [s_selfE, PtrERef<BattleItem>(), Int], GetListScript(u => u._specWeapon));
		bu.Add("getSpecialItem", [s_self, PtrRef<BattleItem>(), Int], GetListScript(u => u._specWeapon));
		bu.AddList<BattleItem>("getSpecialItem", s_selfE, [], PtrERef<BattleItem>(), u => u._specWeapon, (BattleUnit u, BattleItem item, ref ScriptArgs a, int off) => item != null);
		bu.AddList<BattleItem>("getSpecialItem", s_self, [], PtrRef<BattleItem>(), u => u._specWeapon, (BattleUnit u, BattleItem item, ref ScriptArgs a, int off) => item != null);

		bu.AddGet("getPosition.getX", u => u.Position.x);
		bu.AddGet("getPosition.getY", u => u.Position.y);
		bu.AddGet("getPosition.getZ", u => u.Position.z);
		bu.Add("getPosition", [s_self, ValueRef<Position>()], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.ValueRef<Position>(1) = u != null ? u.Position : default;
		});

		bu.AddGet("getTurnsSinceSpotted", u => u.TurnsSinceSpotted);
		bu.Add("setTurnsSinceSpotted", [s_selfE, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null) u._turnsSinceSpotted[(int)UnitFaction.FACTION_HOSTILE] = (byte)BattleItem.ScriptClamp(a.Int(1), 0, 255);
		});

		// C++ takes UnitFaction (not int): no script value has that type
		bu.Add("getTurnsSinceSpottedByFaction", [s_self, IntRef, Value<UnitFaction>()], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? u.GetTurnsSinceSpottedByFaction(a.Value<UnitFaction>(2)) : 0;
		});
		bu.Add("setTurnsSinceSpottedByFaction", [s_selfE, Value<UnitFaction>(), Int], (ref ScriptArgs a) =>
		{
			a.Ptr<BattleUnit>(0)?.SetTurnsSinceSpottedByFaction(a.Value<UnitFaction>(1), a.Int(2));
		});

		bu.AddGet("getTurnsLeftSpottedForSnipers", u => u.TurnsLeftSpottedForSnipers);
		bu.Add("setTurnsLeftSpottedForSnipers", [s_selfE, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null) u._turnsLeftSpottedForSnipers[(int)UnitFaction.FACTION_HOSTILE] = (byte)BattleItem.ScriptClamp(a.Int(1), 0, 255);
		});

		bu.Add("getTurnsLeftSpottedForSnipersByFaction", [s_self, IntRef, Value<UnitFaction>()], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			a.IntRef(1) = u != null ? u.GetTurnsLeftSpottedForSnipersByFaction(a.Value<UnitFaction>(2)) : 0;
		});
		bu.Add("setTTurnsLeftSpottedForSnipersByFaction", [s_selfE, Value<UnitFaction>(), Int], (ref ScriptArgs a) =>
		{
			a.Ptr<BattleUnit>(0)?.SetTurnsLeftSpottedForSnipersByFaction(a.Value<UnitFaction>(1), a.Int(2));
		});

		bu.AddField("getTurnsSinceStunned", u => u._turnsSinceStunned);
		bu.Add("setTurnsSinceStunned", [s_selfE, Int], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			if (u != null) u._turnsSinceStunned = (byte)BattleItem.ScriptClamp(a.Int(1), 0, 255);
		});

		bu.AddScriptValue(u => u._armor.GetScriptValuesRaw(), false);
		bu.AddScriptValue(u => u._scriptValues);
		bu.AddDebugDisplay(DebugDisplayScript);

		bu.Add("getTileShade", [s_self, IntRef], (ref ScriptArgs a) =>
		{
			var u = a.Ptr<BattleUnit>(0);
			ref int shade = ref a.IntRef(1);
			if (u != null)
			{
				var tile = u.Tile;
				if (tile != null)
				{
					shade = tile.Shade;
					return;
				}
			}
			shade = 0;
		});

		bu.AddCustomConst("BODYPART_HEAD", (int)UnitBodyPart.BODYPART_HEAD);
		bu.AddCustomConst("BODYPART_TORSO", (int)UnitBodyPart.BODYPART_TORSO);
		bu.AddCustomConst("BODYPART_LEFTARM", (int)UnitBodyPart.BODYPART_LEFTARM);
		bu.AddCustomConst("BODYPART_RIGHTARM", (int)UnitBodyPart.BODYPART_RIGHTARM);
		bu.AddCustomConst("BODYPART_LEFTLEG", (int)UnitBodyPart.BODYPART_LEFTLEG);
		bu.AddCustomConst("BODYPART_RIGHTLEG", (int)UnitBodyPart.BODYPART_RIGHTLEG);

		bu.AddCustomConst("UNIT_RANK_ROOKIE", 0);
		bu.AddCustomConst("UNIT_RANK_SQUADDIE", 1);
		bu.AddCustomConst("UNIT_RANK_SERGEANT", 2);
		bu.AddCustomConst("UNIT_RANK_CAPTAIN", 3);
		bu.AddCustomConst("UNIT_RANK_COLONEL", 4);
		bu.AddCustomConst("UNIT_RANK_COMMANDER", 5);

		bu.AddCustomConst("COLOR_X1_HAIR", 6);
		bu.AddCustomConst("COLOR_X1_FACE", 9);

		bu.AddCustomConst("COLOR_X1_NULL", 0);
		bu.AddCustomConst("COLOR_X1_YELLOW", 1);
		bu.AddCustomConst("COLOR_X1_RED", 2);
		bu.AddCustomConst("COLOR_X1_GREEN0", 3);
		bu.AddCustomConst("COLOR_X1_GREEN1", 4);
		bu.AddCustomConst("COLOR_X1_GRAY", 5);
		bu.AddCustomConst("COLOR_X1_BROWN0", 6);
		bu.AddCustomConst("COLOR_X1_BLUE0", 7);
		bu.AddCustomConst("COLOR_X1_BLUE1", 8);
		bu.AddCustomConst("COLOR_X1_BROWN1", 9);
		bu.AddCustomConst("COLOR_X1_BROWN2", 10);
		bu.AddCustomConst("COLOR_X1_PURPLE0", 11);
		bu.AddCustomConst("COLOR_X1_PURPLE1", 12);
		bu.AddCustomConst("COLOR_X1_BLUE2", 13);
		bu.AddCustomConst("COLOR_X1_SILVER", 14);
		bu.AddCustomConst("COLOR_X1_SPECIAL", 15);

		bu.AddCustomConst("LOOK_BLONDE", (int)SoldierLook.LOOK_BLONDE);
		bu.AddCustomConst("LOOK_BROWNHAIR", (int)SoldierLook.LOOK_BROWNHAIR);
		bu.AddCustomConst("LOOK_ORIENTAL", (int)SoldierLook.LOOK_ORIENTAL);
		bu.AddCustomConst("LOOK_AFRICAN", (int)SoldierLook.LOOK_AFRICAN);

		bu.AddCustomConst("GENDER_MALE", (int)SoldierGender.GENDER_MALE);
		bu.AddCustomConst("GENDER_FEMALE", (int)SoldierGender.GENDER_FEMALE);

		bu.AddCustomConst("movement_type_walk", (int)MovementType.MT_WALK);
		bu.AddCustomConst("movement_type_fly", (int)MovementType.MT_FLY);
		bu.AddCustomConst("movement_type_slide", (int)MovementType.MT_SLIDE);
	}

	/// <summary>Register BattleUnitVisibility in script parser (C++ <c>BattleUnitVisibility::ScriptRegister</c>).</summary>
	public static void ScriptRegisterVisibility(ScriptParserBase parser)
	{
		var uv = new Bind<BattleUnitVisibility>(parser);

		uv.AddScriptTag();
	}

	/// <summary>Init all required data in script using object data.</summary>
	public static void ScriptFill(ScriptWorkerBlit w, BattleUnit unit, SavedBattleGame save, int body_part, int anim_frame, int shade, int burn)
	{
		ArgumentNullException.ThrowIfNull(w);
		w.Clear();
		if (unit != null)
		{
			var scr = unit.Armor.GetScript(ModScript.RecolorUnitSprite);
			if (scr != null)
			{
				w.Update(scr, unit, save, body_part, anim_frame, shade, burn);
			}
		}
	}
}
