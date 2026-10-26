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
// Ported from: src/Ufopaedia/StatsForNerdsState.cpp
// Port status: partial (missing: script hooks)
#nullable disable

using System.Text;

namespace Acom;

#pragma warning disable CA1822 // instance members in C++ (State._game is static here)
public partial class StatsForNerdsState
{
	/// <summary>Shows the "raw" data of the topic (Item).</summary>
	private void InitItemList()
	{
		_lstRawData.ClearList();
		_lstRawData.SetIgnoreSeparators(true);

		var ssTopic = new StringBuilder();
		ssTopic.Append(Tr(_topicId));
		if (_showIds)
		{
			ssTopic.Append(" [").Append(_topicId).Append(']');
		}

		_txtArticle.SetText(Tr("STR_ARTICLE").Arg(ssTopic.ToString()));

		Mod mod = _game.Mod;
		RuleItem itemRule = mod.GetItem(_topicId);
		if (itemRule == null)
			return;

		_filterOptions.Clear();
		_filterOptions.Add("STR_COMPATIBLE_AMMO");
		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			foreach (var ammo in itemRule.GetCompatibleAmmoForSlot(slot))
			{
				_filterOptions.Add(ammo.Type);
			}
		}
		_cbxRelatedStuff.SetOptions(_filterOptions, true);
		_cbxRelatedStuff.Visible = _filterOptions.Count > 1;
		if (_filterOptions.Count > 1)
		{
			_txtTitle.Align = TextHAlign.ALIGN_LEFT;
		}

		var itemBattleType = itemRule.BattleType;
		var ss = new StringBuilder();

		AddBattleType(ss, itemBattleType, "battleType");
		AddExperienceTrainingMode(ss, itemRule.ExperienceTrainingMode, "experienceTrainingMode");
		AddInteger(ss, itemRule.ManaExperience, "manaExperience");
		AddBoolean(ss, itemRule.ArcingShot, "arcingShot");
		AddBoolean(ss, itemRule.IsFireExtinguisher, "isFireExtinguisher");
		AddInteger(ss, itemRule.Waypoints, "waypoints");
		AddInteger(ss, itemRule.SprayWaypoints, "sprayWaypoints");

		AddInteger(ss, itemRule.ShotgunPellets, "shotgunPellets");
		AddInteger(ss, itemRule.ShotgunBehaviorType, "shotgunBehavior", 0, false, "STR_SHOTGUN_BEHAVIOR_OXCE", 1);
		AddIntegerPercent(ss, itemRule.ShotgunSpread, "shotgunSpread", 100);
		AddIntegerPercent(ss, itemRule.ShotgunChoke, "shotgunChoke", 100);

		AddBoolean(ss, itemRule.IsWaterOnly, "underwaterOnly");
		AddBoolean(ss, itemRule.IsLandOnly, "landOnly");
		bool psiRequiredDefault = itemBattleType == BattleType.BT_PSIAMP ? true : false;
		AddBoolean(ss, itemRule.IsPsiRequired, "psiRequired", psiRequiredDefault);
		AddBoolean(ss, itemRule.IsManaRequired, "manaRequired");
		int targetMatrixDefault = itemBattleType == BattleType.BT_PSIAMP ? 6 : 7;
		AddItemTargets(ss, itemRule, "targetMatrix", targetMatrixDefault);
		AddBoolean(ss, itemRule.ConvertToCivilian(), "convertToCivilian");
		AddBoolean(ss, itemRule.IsLOSRequired, "LOSRequired");

		if (itemBattleType == BattleType.BT_GRENADE || _showDebug)
		{
			AddInteger(ss, itemRule.GetExplodeInventory(mod), "explodeInventory", 0); // not raw!
		}
		if (itemBattleType == BattleType.BT_FIREARM
			|| itemBattleType == BattleType.BT_GRENADE
			|| itemBattleType == BattleType.BT_PROXIMITYGRENADE
			|| itemBattleType == BattleType.BT_FLARE
			|| _showDebug)
		{
			AddIntegerPercent(ss, itemRule.GetNoLOSAccuracyPenalty(mod), "noLOSAccuracyPenalty", -1); // not raw!
		}
		if (itemBattleType == BattleType.BT_FIREARM || _showDebug)
		{
			AddIntegerPercent(ss, itemRule.GetKneelBonus(mod), "kneelBonus", 115); // not raw!
		}
		AddBoolean(ss, itemRule.IsTwoHanded, "twoHanded");
		AddBoolean(ss, itemRule.IsBlockingBothHands, "blockBothHands");
		int oneHandedPenaltyCurrent = itemRule.GetOneHandedPenalty(mod);
		int oneHandedPenaltyDefault = itemRule.IsTwoHanded ? 80 : oneHandedPenaltyCurrent;
		AddIntegerPercent(ss, oneHandedPenaltyCurrent, "oneHandedPenalty", oneHandedPenaltyDefault); // not raw!

		AddInteger(ss, itemRule.MinRange, "minRange");
		AddInteger(ss, itemRule.MaxRange, "maxRange", 200);
		int aimRangeDefault = itemBattleType == BattleType.BT_PSIAMP ? 0 : 200;
		AddInteger(ss, itemRule.AimRange, "aimRange", aimRangeDefault);
		AddInteger(ss, itemRule.AutoRange, "autoRange", 7);
		AddInteger(ss, itemRule.SnapRange, "snapRange", 15);
		int dropoffDefault = itemBattleType == BattleType.BT_PSIAMP ? 1 : 2;
		AddInteger(ss, itemRule.Dropoff, "dropoff", dropoffDefault);

		AddRuleStatBonus(ss, itemRule.AccuracyMultiplierRaw, "accuracyMultiplier");
		AddIntegerPercent(ss, itemRule.ConfigAimed.accuracy, "accuracyAimed");
		AddIntegerPercent(ss, itemRule.ConfigAuto.accuracy, "accuracyAuto");
		AddIntegerPercent(ss, itemRule.ConfigSnap.accuracy, "accuracySnap");
		AddRuleItemUseCostFull(ss, itemRule.CostAimed, "costAimed", new RuleItemUseCost(), true, itemRule.FlatAimed);
		AddRuleItemUseCostFull(ss, itemRule.CostAuto, "costAuto", new RuleItemUseCost(), true, itemRule.FlatAuto);
		AddRuleItemUseCostFull(ss, itemRule.CostSnap, "costSnap", new RuleItemUseCost(), true, itemRule.FlatSnap);

		AddRuleStatBonus(ss, itemRule.MeleeMultiplierRaw, "meleeMultiplier");
		AddIntegerPercent(ss, itemRule.ConfigMelee.accuracy, "accuracyMelee");
		AddRuleItemUseCostFull(ss, itemRule.CostMelee, "costMelee", new RuleItemUseCost(), true, itemRule.FlatMelee);

		AddSingleString(ss, itemRule.PsiAttackName, "psiAttackName");
		AddIntegerPercent(ss, itemRule.AccuracyUse, "accuracyUse");
		if (itemBattleType == BattleType.BT_PSIAMP || _showDebug)
		{
			AddIntegerPercent(ss, itemRule.AccuracyMind, "accuracyMindControl");
			AddIntegerPercent(ss, itemRule.AccuracyPanic, "accuracyPanic", 20);
		}
		int tuUseDefault = (itemBattleType == BattleType.BT_PSIAMP/* && itemRule->getPsiAttackName().empty()*/) ? 0 : 25;
		AddRuleItemUseCostFull(ss, itemRule.CostUse, "costUse", new RuleItemUseCost((short)tuUseDefault), true, itemRule.FlatUse);
		if (itemBattleType == BattleType.BT_PSIAMP || _showDebug)
		{
			// using flatUse! there are no flatMindcontrol and flatPanic
			// always show! as if default was 0 instead of 25
			// don't show if Time == 0, for the game it means disabled (even if other costs are non-zero)
			if (itemRule.CostMind.Time > 0 || _showDebug)
			{
				AddRuleItemUseCostFull(ss, itemRule.CostMind, "costMindcontrol", new RuleItemUseCost(0), true, itemRule.FlatUse);
			}
			if (itemRule.CostPanic.Time > 0 || _showDebug)
			{
				AddRuleItemUseCostFull(ss, itemRule.CostPanic, "costPanic", new RuleItemUseCost(0), true, itemRule.FlatUse);
			}
		}

		AddInteger(ss, itemRule.Weight, "weight", 3);
		AddInteger(ss, itemRule.ThrowRange, "throwRange", 200);
		AddInteger(ss, itemRule.UnderwaterThrowRange, "underwaterThrowRange", 200);

		AddInteger(ss, itemRule.ThrowDropoffRange, "throwDropoffRange", 99);
		AddInteger(ss, itemRule.UnderwaterThrowDropoffRange, "underwaterThrowDropoffRange", 99);
		AddInteger(ss, itemRule.ThrowDropoff, "throwDropoff", 5);

		AddRuleStatBonus(ss, itemRule.ThrowMultiplierRaw, "throwMultiplier");
		AddIntegerPercent(ss, itemRule.AccuracyThrow, "accuracyThrow", 100);
		AddRuleItemUseCostFull(ss, itemRule.CostThrow, "costThrow", new RuleItemUseCost(25), true, itemRule.FlatThrow);
		AddRuleItemUseCostFull(ss, itemRule.CostPrime, "costPrime", new RuleItemUseCost(50), true, itemRule.FlatPrime);
		AddRuleItemUseCostFull(ss, itemRule.CostUnprime, "costUnprime", new RuleItemUseCost(25), true, itemRule.FlatUnprime);

		if ((mod.EnableCloseQuartersCombat != 0 && itemBattleType == BattleType.BT_FIREARM) || _showDebug)
		{
			AddRuleStatBonus(ss, itemRule.CloseQuartersMultiplierRaw, "closeQuartersMultiplier");
			AddIntegerPercent(ss, itemRule.GetAccuracyCloseQuarters(mod), "accuracyCloseQuarters", 100); // not raw!
		}

		for (int i = 0; i < 2; i++)
		{
			RuleDamageType rule = (i == 0) ? itemRule.DamageType : itemRule.MeleeType;
			RuleDamageType ruleByResistType = mod.GetDamageType(rule.ResistType);

			if (i == 0)
			{
				ItemDamageType damageTypeDefault = ItemDamageType.DT_NONE;
				if (rule.ResistType == ItemDamageType.DT_NONE)
				{
					if (itemBattleType == BattleType.BT_FIREARM || itemBattleType == BattleType.BT_AMMO || itemBattleType == BattleType.BT_MELEE)
					{
						if (itemRule.ClipSize != 0)
						{
							damageTypeDefault = ItemDamageType.DAMAGE_TYPES;
						}
					}
					else if (itemBattleType == BattleType.BT_PSIAMP)
					{
						if (!string.IsNullOrEmpty(itemRule.PsiAttackName))
						{
							damageTypeDefault = ItemDamageType.DAMAGE_TYPES;
						}
					}
				}
				AddDamageType(ss, rule.ResistType, "damageType", damageTypeDefault);
				AddInteger(ss, itemRule.Power, "power");
				AddFloat(ss, itemRule.PowerRangeReductionRaw, "powerRangeReduction");
				AddFloat(ss, itemRule.PowerRangeThresholdRaw, "powerRangeThreshold");
				AddRuleStatBonus(ss, itemRule.DamageBonusRaw, "damageBonus");
				if (!_showDebug)
				{
					AddInteger(ss, rule.FixRadius, "blastRadius", ruleByResistType.FixRadius);
				}
				AddHeading("damageAlter");
			}
			else
			{
				AddDamageType(ss, rule.ResistType, "meleeType", ItemDamageType.DT_MELEE);
				AddInteger(ss, itemRule.MeleePower, "meleePower");
				AddRuleStatBonus(ss, itemRule.MeleeBonusRaw, "meleeBonus");
				AddHeading("meleeAlter");
			}

			if (_showDebug || i > 0)
			{
				AddInteger(ss, rule.FixRadius, "FixRadius", ruleByResistType.FixRadius);
			}

			bool isCustomDefault = (i == 0 ? itemRule.IsDamageTypeSet : itemRule.IsMeleeTypeSet);
			if (!isCustomDefault)
			{
				// default RandomType is 8 (DRT_STANDARD)
				// but the RandomType of a default ResistType 0 (DT_NONE) is 5 (DRT_NONE)
				// so we need this exceptional handling here to correctly show what's modded/changed and what's not
				AddDamageRandomType(ss, rule.RandomType, "RandomType", ItemDamageRandomType.DRT_STANDARD);
			}
			else
			{
				AddDamageRandomType(ss, rule.RandomType, "RandomType", ruleByResistType.RandomType);
			}

			AddBoolean(ss, rule.FireBlastCalc, "FireBlastCalc", ruleByResistType.FireBlastCalc);
			AddBoolean(ss, rule.IgnoreDirection, "IgnoreDirection", ruleByResistType.IgnoreDirection);
			AddBoolean(ss, rule.IgnoreSelfDestruct, "IgnoreSelfDestruct", ruleByResistType.IgnoreSelfDestruct);
			AddBoolean(ss, rule.IgnorePainImmunity, "IgnorePainImmunity", ruleByResistType.IgnorePainImmunity);
			AddBoolean(ss, rule.IgnoreNormalMoraleLose, "IgnoreNormalMoraleLose", ruleByResistType.IgnoreNormalMoraleLose);
			AddBoolean(ss, rule.IgnoreOverKill, "IgnoreOverKill", ruleByResistType.IgnoreOverKill);

			AddFloatAsPercentage(ss, rule.ArmorEffectiveness, "ArmorEffectiveness", ruleByResistType.ArmorEffectiveness);
			AddFloatAsPercentage(ss, rule.RadiusEffectiveness, "RadiusEffectiveness", ruleByResistType.RadiusEffectiveness);
			AddFloat(ss, rule.RadiusReduction, "RadiusReduction", ruleByResistType.RadiusReduction);

			AddInteger(ss, rule.FireThreshold, "FireThreshold", ruleByResistType.FireThreshold);
			AddInteger(ss, rule.SmokeThreshold, "SmokeThreshold", ruleByResistType.SmokeThreshold);

			AddFloatAsPercentage(ss, rule.ToArmorPre, "ToArmorPre", ruleByResistType.ToArmorPre);
			AddBoolean(ss, rule.RandomArmorPre, "RandomArmorPre", ruleByResistType.RandomArmorPre);

			AddFloatAsPercentage(ss, rule.ToArmor, "ToArmor", ruleByResistType.ToArmor);
			AddBoolean(ss, rule.RandomArmor, "RandomArmor", ruleByResistType.RandomArmor);

			AddFloatAsPercentage(ss, rule.ToHealth, "ToHealth", ruleByResistType.ToHealth);
			AddBoolean(ss, rule.RandomHealth, "RandomHealth", ruleByResistType.RandomHealth);

			AddFloatAsPercentage(ss, rule.ToStun, "ToStun", ruleByResistType.ToStun);
			AddBoolean(ss, rule.RandomStun, "RandomStun", ruleByResistType.RandomStun);

			AddFloatAsPercentage(ss, rule.ToWound, "ToWound", ruleByResistType.ToWound);
			AddBoolean(ss, rule.RandomWound, "RandomWound", ruleByResistType.RandomWound);

			AddFloatAsPercentage(ss, rule.ToTime, "ToTime", ruleByResistType.ToTime);
			AddBoolean(ss, rule.RandomTime, "RandomTime", ruleByResistType.RandomTime);

			AddFloatAsPercentage(ss, rule.ToEnergy, "ToEnergy", ruleByResistType.ToEnergy);
			AddBoolean(ss, rule.RandomEnergy, "RandomEnergy", ruleByResistType.RandomEnergy);

			AddFloatAsPercentage(ss, rule.ToMorale, "ToMorale", ruleByResistType.ToMorale);
			AddBoolean(ss, rule.RandomMorale, "RandomMorale", ruleByResistType.RandomMorale);

			AddFloatAsPercentage(ss, rule.ToItem, "ToItem", ruleByResistType.ToItem);
			AddBoolean(ss, rule.RandomItem, "RandomItem", ruleByResistType.RandomItem);

			AddFloatAsPercentage(ss, rule.ToMana, "ToMana", ruleByResistType.ToMana);
			AddBoolean(ss, rule.RandomMana, "RandomMana", ruleByResistType.RandomMana);

			AddFloatAsPercentage(ss, rule.ToTile, "ToTile", ruleByResistType.ToTile);
			AddBoolean(ss, rule.RandomTile, "RandomTile", ruleByResistType.RandomTile);
			AddInteger(ss, rule.TileDamageMethod, "TileDamageMethod", ruleByResistType.TileDamageMethod);
			AddInteger(ss, rule.TileDamageLimit, "TileDamageLimit", ruleByResistType.TileDamageLimit);

			EndHeading();
		}

		// skillApplied*
		// strengthApplied*
		// autoShots*

		AddHeading("confAimed");
		{
			AddInteger(ss, itemRule.ConfigAimed.shots, "shots", 1);
			AddInteger(ss, itemRule.ConfigAimed.spendPerShot, "spendPerShot", 1);
			AddBoolean(ss, itemRule.ConfigAimed.followProjectiles, "followProjectiles", true);
			AddSingleString(ss, itemRule.ConfigAimed.name, "name", "STR_AIMED_SHOT");
			AddSingleString(ss, itemRule.ConfigAimed.shortName, "shortName");
			AddInteger(ss, itemRule.ConfigAimed.ammoSlot, "ammoSlot");
			AddInteger(ss, itemRule.ConfigAimed.ammoZombieUnitChanceOverride, "ammoZombieUnitChanceOverride", -1);
			AddInteger(ss, itemRule.ConfigAimed.ammoSpawnUnitChanceOverride, "ammoSpawnUnitChanceOverride", -1);
			AddInteger(ss, itemRule.ConfigAimed.ammoSpawnItemChanceOverride, "ammoSpawnItemChanceOverride", -1);
			AddBoolean(ss, itemRule.ConfigAimed.arcing, "arcing");
			EndHeading();
		}

		AddHeading("confAuto");
		{
			AddInteger(ss, itemRule.ConfigAuto.shots, "shots", 3);
			AddInteger(ss, itemRule.ConfigAuto.spendPerShot, "spendPerShot", 1);
			AddBoolean(ss, itemRule.ConfigAuto.followProjectiles, "followProjectiles", true);
			AddSingleString(ss, itemRule.ConfigAuto.name, "name", "STR_AUTO_SHOT");
			AddSingleString(ss, itemRule.ConfigAuto.shortName, "shortName");
			AddInteger(ss, itemRule.ConfigAuto.ammoSlot, "ammoSlot");
			AddInteger(ss, itemRule.ConfigAuto.ammoZombieUnitChanceOverride, "ammoZombieUnitChanceOverride", -1);
			AddInteger(ss, itemRule.ConfigAuto.ammoSpawnUnitChanceOverride, "ammoSpawnUnitChanceOverride", -1);
			AddInteger(ss, itemRule.ConfigAuto.ammoSpawnItemChanceOverride, "ammoSpawnItemChanceOverride", -1);
			AddBoolean(ss, itemRule.ConfigAuto.arcing, "arcing");
			EndHeading();
		}

		AddHeading("confSnap");
		{
			AddInteger(ss, itemRule.ConfigSnap.shots, "shots", 1);
			AddInteger(ss, itemRule.ConfigSnap.spendPerShot, "spendPerShot", 1);
			AddBoolean(ss, itemRule.ConfigSnap.followProjectiles, "followProjectiles", true);
			AddSingleString(ss, itemRule.ConfigSnap.name, "name", "STR_SNAP_SHOT");
			AddSingleString(ss, itemRule.ConfigSnap.shortName, "shortName");
			AddInteger(ss, itemRule.ConfigSnap.ammoSlot, "ammoSlot");
			AddInteger(ss, itemRule.ConfigSnap.ammoZombieUnitChanceOverride, "ammoZombieUnitChanceOverride", -1);
			AddInteger(ss, itemRule.ConfigSnap.ammoSpawnUnitChanceOverride, "ammoSpawnUnitChanceOverride", -1);
			AddInteger(ss, itemRule.ConfigSnap.ammoSpawnItemChanceOverride, "ammoSpawnItemChanceOverride", -1);
			AddBoolean(ss, itemRule.ConfigSnap.arcing, "arcing");
			EndHeading();
		}

		AddHeading("confMelee");
		{
			AddInteger(ss, itemRule.ConfigMelee.shots, "shots", 1);
			AddInteger(ss, itemRule.ConfigMelee.spendPerShot, "spendPerShot", 1);
			AddBoolean(ss, itemRule.ConfigMelee.followProjectiles, "followProjectiles", true);
			AddSingleString(ss, itemRule.ConfigMelee.name, "name");
			AddSingleString(ss, itemRule.ConfigMelee.shortName, "shortName");
			int ammoSlotCurrent = itemRule.ConfigMelee.ammoSlot;
			int ammoSlotDefault = itemBattleType == BattleType.BT_MELEE ? 0 : RuleItem.AmmoSlotSelfUse;
			if (itemBattleType == BattleType.BT_NONE)
			{
				// exception for unspecified battle type, e.g. Elerium-115 in vanilla
				if (ammoSlotCurrent <= 0)
				{
					ammoSlotDefault = ammoSlotCurrent;
				}
			}
			AddInteger(ss, ammoSlotCurrent, "ammoSlot", ammoSlotDefault);
			AddInteger(ss, itemRule.ConfigMelee.ammoZombieUnitChanceOverride, "ammoZombieUnitChanceOverride", -1);
			AddInteger(ss, itemRule.ConfigMelee.ammoSpawnUnitChanceOverride, "ammoSpawnUnitChanceOverride", -1);
			AddInteger(ss, itemRule.ConfigMelee.ammoSpawnItemChanceOverride, "ammoSpawnItemChanceOverride", -1);
			AddBoolean(ss, itemRule.ConfigMelee.arcing, "arcing");
			EndHeading();
		}

		AddInteger(ss, itemRule.ClipSize, "clipSize", 0, false, "STR_CLIP_SIZE_UNLIMITED", -1);
		AddBoolean(ss, itemRule.IsAmmoRechargeable, "isAmmoRechargeable");

		// compatibleAmmo*
		// tuLoad*
		// tuUnload*

		AddHeading("primaryAmmo");
		{
			AddVectorOfRules(ss, itemRule.GetCompatibleAmmoForSlot(0), "compatibleAmmo");
			AddInteger(ss, itemRule.GetTULoad(0), "tuLoad", 15);
			AddInteger(ss, itemRule.GetTUUnload(0), "tuUnload", 8);
			EndHeading();
		}

		AddHeading("ammo[1]");
		{
			AddVectorOfRules(ss, itemRule.GetCompatibleAmmoForSlot(1), "compatibleAmmo");
			AddInteger(ss, itemRule.GetTULoad(1), "tuLoad", 15);
			AddInteger(ss, itemRule.GetTUUnload(1), "tuUnload", 8);
			EndHeading();
		}

		AddHeading("ammo[2]");
		{
			AddVectorOfRules(ss, itemRule.GetCompatibleAmmoForSlot(2), "compatibleAmmo");
			AddInteger(ss, itemRule.GetTULoad(2), "tuLoad", 15);
			AddInteger(ss, itemRule.GetTUUnload(2), "tuUnload", 8);
			EndHeading();
		}

		AddHeading("ammo[3]");
		{
			AddVectorOfRules(ss, itemRule.GetCompatibleAmmoForSlot(3), "compatibleAmmo");
			AddInteger(ss, itemRule.GetTULoad(3), "tuLoad", 15);
			AddInteger(ss, itemRule.GetTUUnload(3), "tuUnload", 8);
			EndHeading();
		}

		AddInteger(ss, itemRule.Armor, "armor", 20);

		AddBattleMediKitType(ss, itemRule.MediKitType, "medikitType");
		AddSingleString(ss, itemRule.PainKillerActionName, "painKillerActionName", "STR_PAIN_KILLER");
		AddSingleString(ss, itemRule.StimulantActionName, "stimulantActionName", "STR_STIMULANT");
		AddSingleString(ss, itemRule.HealActionName, "healActionName", "STR_HEAL");
		AddSingleString(ss, itemRule.MedikitActionName, "medikitActionName", "STR_USE_MEDI_KIT");
		AddBoolean(ss, itemRule.AllowTargetSelf, "medikitTargetSelf");
		AddBoolean(ss, itemRule.AllowTargetImmune, "medikitTargetImmune");
		AddMediKitTargets(ss, itemRule, "medikitTargetMatrix", 63);
		AddBoolean(ss, itemRule.IsConsumable, "isConsumable");
		AddInteger(ss, itemRule.PainKillerQuantity, "painKiller");
		AddInteger(ss, itemRule.HealQuantity, "heal");
		AddInteger(ss, itemRule.StimulantQuantity, "stimulant");
		AddInteger(ss, itemRule.WoundRecovery, "woundRecovery");
		AddInteger(ss, itemRule.HealthRecovery, "healthRecovery");
		AddInteger(ss, itemRule.StunRecovery, "stunRecovery");
		AddInteger(ss, itemRule.EnergyRecovery, "energyRecovery");
		AddInteger(ss, itemRule.ManaRecovery, "manaRecovery");
		AddInteger(ss, itemRule.MoraleRecovery, "moraleRecovery");
		AddFloatAsPercentage(ss, itemRule.PainKillerRecovery, "painKillerRecovery", 1.0f);

		AddVectorOfResearch(ss, itemRule.Requirements, "requires");
		AddVectorOfResearch(ss, itemRule.BuyRequirements, "requiresBuy");
		AddVectorOfStrings(ss, mod.GetBaseFunctionNames(itemRule.RequiresBuyBaseFunc), "requiresBuyBaseFunc");
		AddSingleString(ss, itemRule.RequiresBuyCountry, "requiresBuyCountry");
		AddVectorOfStrings(ss, itemRule.Categories, "categories");
		AddVectorOfRulesId(ss, itemRule.SupportedInventorySections, "supportedInventorySections");

		AddDouble(ss, itemRule.Size, "size");
		if (_game.SavedGame.BuyPriceCoefficient == 100)
		{
			AddInteger(ss, itemRule.BuyCost, "costBuy", 0, true);
		}
		else
		{
			AddHeading("_calculatedValues", "STR_FOR_DIFFICULTY", true);
			{
				int adjustedCost = itemRule.BuyCost * _game.SavedGame.BuyPriceCoefficient / 100;
				AddInteger(ss, adjustedCost, "costBuy", 0, true);

				EndHeading();
			}
		}
		AddInteger(ss, itemRule.MonthlyBuyLimit, "monthlyBuyLimit");
		AddInteger(ss, itemRule.TransferTime, "transferTime", 24);
		AddInteger(ss, itemRule.MonthlySalary, "monthlySalary", 0, true);
		AddInteger(ss, itemRule.MonthlyMaintenance, "monthlyMaintenance", 0, true);
		if (_game.SavedGame.SellPriceCoefficient == 100)
		{
			AddInteger(ss, itemRule.SellCost, "costSell", 0, true);
		}
		else
		{
			AddHeading("_calculatedValues", "STR_FOR_DIFFICULTY", true);
			{
				int adjustedCost = itemRule.SellCost * _game.SavedGame.SellPriceCoefficient / 100;
				AddInteger(ss, adjustedCost, "costSell", 0, true);

				EndHeading();
			}
		}

		// missing: script hooks (ModScript::scriptCallback<StatsForNerds...>)

		if (_showDebug)
		{
			AddSection("{Modding section}", "You don't need this info as a player", _white, true);

			AddSection("{Naming}", "", _white);
			AddSingleString(ss, itemRule.Type, "type");
			AddSingleString(ss, itemRule.UfopediaType, "ufopediaType");
			AddSingleString(ss, itemRule.Name, "name", itemRule.Type);
			AddSingleString(ss, itemRule.NameAsAmmo, "nameAsAmmo");
			AddInteger(ss, itemRule.LoadOrder, "loadOrder");
			AddInteger(ss, itemRule.ListOrder, "listOrder");
			AddBoolean(ss, itemRule.HidePower, "hidePower");
			AddBoolean(ss, itemRule.IgnoreAmmoPower, "ignoreAmmoPower");
			AddSingleString(ss, itemRule.MonthlyBuyLimitMessage, "monthlyBuyLimitMessage");

			AddSection("{Inventory}", "", _white);
			AddVectorOfIntegers(ss, itemRule.CustomItemPreviewIndex, "customItemPreviewIndex");
			AddInteger(ss, itemRule.InventoryWidth, "invWidth", -1); // always show!
			AddInteger(ss, itemRule.InventoryHeight, "invHeight", -1); // always show!
			AddRuleId(ss, itemRule.DefaultInventorySlot, "defaultInventorySlot");
			AddInteger(ss, itemRule.DefaultInventorySlotX, "defaultInvSlotX");
			AddInteger(ss, itemRule.DefaultInventorySlotY, "defaultInvSlotY");
			AddBoolean(ss, itemRule.IsFixed, "fixedWeapon");
			AddBoolean(ss, itemRule.IsSpecialUsingEmptyHand, "specialUseEmptyHand");
			AddBoolean(ss, itemRule.ShowSpecialInEmptyHand(), "specialUseEmptyHandShow");

			AddHeading("inventoryMoveCost");
			{
				AddInteger(ss, itemRule.InventoryMoveCostPercent, "basePercent", 100);

				EndHeading();
			}

			AddSection("{Recovery}", "", _white);
			AddBoolean(ss, !itemRule.CanBeEquippedBeforeBaseDefense(), "ignoreInBaseDefense"); // negated!
			AddBoolean(ss, !itemRule.CanBeEquippedToCraftInventory(), "ignoreInCraftEquip", !itemRule.IsUsefulBattlescapeItem); // negated!
			AddInteger(ss, itemRule.SpecialType, "specialType", -1);
			AddBoolean(ss, itemRule.RecoveryDividers.Count != 0, "recoveryDividers*", false); // just say if there are any or not
			AddBoolean(ss, itemRule.RecoveryTransformations.Count != 0, "recoveryTransformations*", false); // just say if there are any or not
			AddBoolean(ss, itemRule.IsRecoverable, "recover", true);
			AddBoolean(ss, itemRule.IsCorpseRecoverable, "recoverCorpse", true);
			AddInteger(ss, itemRule.RecoveryPoints, "recoveryPoints");
			AddBoolean(ss, itemRule.IsAlien, "liveAlien");
			AddInteger(ss, itemRule.PrisonType, "prisonType");
			AddSingleString(ss, itemRule.SellActionMessage, "sellActionMessage");
			AddInteger(ss, itemRule.VehicleFixedAmmoSlot, "vehicleFixedAmmoSlot");

			AddSection("{Explosives}", "", _white);
			AddInteger(ss, itemRule.PowerForAnimation, "powerForAnimation");
			AddBoolean(ss, itemRule.IsHiddenOnMinimap, "hiddenOnMinimap");
			AddSingleString(ss, itemRule.PrimeActionName, "primeActionName", "STR_PRIME_GRENADE");
			AddSingleString(ss, itemRule.PrimeActionMessage, "primeActionMessage", "STR_GRENADE_IS_ACTIVATED");
			AddSingleString(ss, itemRule.UnprimeActionName, "unprimeActionName");
			AddSingleString(ss, itemRule.UnprimeActionMessage, "unprimeActionMessage", "STR_GRENADE_IS_DEACTIVATED");
			BattleFuseType fuseTypeDefault = BattleFuseType.BFT_NONE;
			if (itemBattleType == BattleType.BT_PROXIMITYGRENADE)
			{
				fuseTypeDefault = BattleFuseType.BFT_INSTANT;
			}
			else if (itemBattleType == BattleType.BT_GRENADE)
			{
				fuseTypeDefault = BattleFuseType.BFT_SET;
			}
			AddBattleFuseType(ss, itemRule.FuseTimerType, "fuseType", fuseTypeDefault);
			AddHeading("fuseTriggerEvents:");
			{
				AddBoolean(ss, itemRule.FuseTriggerEvent.defaultBehavior, "defaultBehavior", true);
				AddBoolean(ss, itemRule.FuseTriggerEvent.throwTrigger, "throwTrigger");
				AddBoolean(ss, itemRule.FuseTriggerEvent.throwExplode, "throwExplode");
				AddBoolean(ss, itemRule.FuseTriggerEvent.proximityTrigger, "proximityTrigger");
				AddBoolean(ss, itemRule.FuseTriggerEvent.proximityExplode, "proximityExplode");
				EndHeading();
			}
			AddIntegerPercent(ss, itemRule.SpecialChance, "specialChance", 100);

			AddSection("{Spawning}", "", _white);
			AddBoolean(ss, itemRule.ZombieUnitByArmorMaleRaw.Count != 0, "zombieUnitByArmorMale*", false); // just say if there are any or not
			AddBoolean(ss, itemRule.ZombieUnitByArmorFemaleRaw.Count != 0, "zombieUnitByArmorFemale*", false); // just say if there are any or not
			AddBoolean(ss, itemRule.ZombieUnitByTypeRaw.Count != 0, "zombieUnitByType*", false); // just say if there are any or not
			AddSingleString(ss, itemRule.GetZombieUnit(null), "zombieUnit");
			AddIntegerPercent(ss, itemRule.ZombieUnitChance, "zombieUnitChance", 100); // default -1 means same as "specialChance"
			AddInteger(ss, (int)itemRule.ZombieUnitFaction, "zombieUnitFaction", 1); // hostile
			AddRule(ss, itemRule.SpawnUnit, "spawnUnit");
			AddIntegerPercent(ss, itemRule.SpawnUnitChance, "spawnUnitChance", 100); // default -1 means same as "specialChance"
			AddInteger(ss, (int)itemRule.SpawnUnitFaction, "spawnUnitFaction", -1); // none (i.e. same faction as attacker)
			AddRule(ss, itemRule.SpawnItem, "spawnItem");
			AddIntegerPercent(ss, itemRule.SpawnItemChance, "spawnItemChance", 100); // default -1 means same as "specialChance"

			AddSection("{Sprites}", "", _white);
			AddBoolean(ss, itemRule.FixedShow, "fixedWeaponShow");
			AddInteger(ss, itemRule.TurretType, "turretType", -1);

			AddInteger(ss, itemRule.BigSprite, "bigSprite", -1);
			AddSpriteResourcePath(ss, mod, "BIGOBS.PCK", itemRule.BigSprite);
			AddInteger(ss, itemRule.FloorSprite, "floorSprite", -1);
			AddSpriteResourcePath(ss, mod, "FLOOROB.PCK", itemRule.FloorSprite);
			AddInteger(ss, itemRule.HandSprite, "handSprite", 120);
			AddSpriteResourcePath(ss, mod, "HANDOB.PCK", itemRule.HandSprite);
			AddInteger(ss, itemRule.SpecialIconSprite, "specialIconSprite", -1);
			AddSpriteResourcePath(ss, mod, "SPICONS.DAT", itemRule.SpecialIconSprite);
			AddInteger(ss, itemRule.BulletSprite, "bulletSprite", -1);
			AddSingleString(ss, itemRule.MediKitCustomBackground, "medikitBackground");

			AddSection("{Sounds}", "", _white);
			AddVectorOfIntegers(ss, itemRule.ReloadSoundRaw, "reloadSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.ReloadSoundRaw);
			AddVectorOfIntegers(ss, itemRule.PrimeSoundRaw, "primeSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.PrimeSoundRaw);
			AddVectorOfIntegers(ss, itemRule.UnprimeSoundRaw, "unprimeSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.UnprimeSoundRaw);
			AddVectorOfIntegers(ss, itemRule.FireSoundRaw, "fireSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.FireSoundRaw);
			AddVectorOfIntegers(ss, itemRule.HitSoundRaw, "hitSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.HitSoundRaw);
			AddVectorOfIntegers(ss, itemRule.HitMissSoundRaw, "hitMissSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.HitMissSoundRaw);
			AddVectorOfIntegers(ss, itemRule.MeleeSoundRaw, "meleeSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.MeleeSoundRaw);
			AddVectorOfIntegers(ss, itemRule.MeleeHitSoundRaw, "meleeHitSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.MeleeHitSoundRaw);
			AddVectorOfIntegers(ss, itemRule.MeleeMissSoundRaw, "meleeMissSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.MeleeMissSoundRaw);
			AddVectorOfIntegers(ss, itemRule.PsiSoundRaw, "psiSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.PsiSoundRaw);
			AddVectorOfIntegers(ss, itemRule.PsiMissSoundRaw, "psiMissSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.PsiMissSoundRaw);
			AddVectorOfIntegers(ss, itemRule.ExplosionHitSoundRaw, "explosionHitSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", itemRule.ExplosionHitSoundRaw);

			AddSection("{Animations}", "", _white);
			AddInteger(ss, itemRule.HitAnimation, "hitAnimation");
			AddInteger(ss, itemRule.HitAnimationFrames, "hitAnimFrames", -1);
			AddInteger(ss, itemRule.HitMissAnimation, "hitMissAnimation", -1);
			AddInteger(ss, itemRule.HitMissAnimationFrames, "hitMissAnimFrames", -1);
			AddInteger(ss, itemRule.MeleeAnimation, "meleeAnimation");
			AddInteger(ss, itemRule.MeleeAnimationFrames, "meleeAnimFrames", -1);
			AddInteger(ss, itemRule.MeleeMissAnimation, "meleeMissAnimation", -1);
			AddInteger(ss, itemRule.MeleeMissAnimationFrames, "meleeMissAnimFrames", -1);
			AddInteger(ss, itemRule.PsiAnimation, "psiAnimation", -1);
			AddInteger(ss, itemRule.PsiAnimationFrames, "psiAnimFrames", -1);
			AddInteger(ss, itemRule.PsiMissAnimation, "psiMissAnimation", -1);
			AddInteger(ss, itemRule.PsiMissAnimationFrames, "psiMissAnimFrames", -1);

			AddInteger(ss, itemRule.BulletSpeed, "bulletSpeed");
			AddInteger(ss, itemRule.ExplosionSpeed, "explosionSpeed");

			AddInteger(ss, itemRule.GetVaporColor(1), "vaporColor", -1);
			AddInteger(ss, itemRule.GetVaporDensity(1), "vaporDensity");
			AddIntegerPercent(ss, itemRule.GetVaporProbability(1), "vaporProbability", 15);
			AddInteger(ss, itemRule.GetVaporColor(0), "vaporColorSurface", -1);
			AddInteger(ss, itemRule.GetVaporDensity(0), "vaporDensitySurface");
			AddIntegerPercent(ss, itemRule.GetVaporProbability(0), "vaporProbabilitySurface", 15);

			AddSection("{AI}", "", _white);
			AddInteger(ss, itemRule.Attraction, "attraction");
			AddHeading("ai:");
			{
				int useDelayCurrent = itemRule.GetAIUseDelay(mod);
				int useDelayDefault = useDelayCurrent > 0 ? -1 : useDelayCurrent;
				AddInteger(ss, useDelayCurrent, "useDelay", useDelayDefault); // not raw!
				AddInteger(ss, itemRule.AIMeleeHitCount, "meleeHitCount", 25);
				EndHeading();
			}

			AddSection("{TU/flat info}", "", _white);
			AddRuleItemUseCostBasic(ss, itemRule.CostAimed, "tuAimed");
			AddRuleItemUseCostBasic(ss, itemRule.CostAuto, "tuAuto");
			AddRuleItemUseCostBasic(ss, itemRule.CostSnap, "tuSnap");
			AddRuleItemUseCostBasic(ss, itemRule.CostMelee, "tuMelee");
			tuUseDefault = (itemBattleType == BattleType.BT_PSIAMP/* && itemRule->getPsiAttackName().empty()*/) ? 0 : 25;
			AddRuleItemUseCostBasic(ss, itemRule.CostUse, "tuUse", tuUseDefault);
			if (itemBattleType == BattleType.BT_PSIAMP || _showDebug)
			{
				// always show! as if default was 0 instead of 25
				AddRuleItemUseCostBasic(ss, itemRule.CostMind, "tuMindcontrol", 0);
				AddRuleItemUseCostBasic(ss, itemRule.CostPanic, "tuPanic", 0);
			}
			AddRuleItemUseCostBasic(ss, itemRule.CostThrow, "tuThrow", 25);
			AddRuleItemUseCostBasic(ss, itemRule.CostPrime, "tuPrime", 50);
			AddRuleItemUseCostBasic(ss, itemRule.CostUnprime, "tuUnprime", 25);

			// flatRate*

			AddRuleItemUseCostFull(ss, itemRule.FlatAimed, "flatAimed", new RuleItemUseFlat(false, true));
			AddRuleItemUseCostFull(ss, itemRule.FlatAuto, "flatAuto", new RuleItemUseFlat(false, true));
			AddRuleItemUseCostFull(ss, itemRule.FlatSnap, "flatSnap", new RuleItemUseFlat(false, true));
			AddRuleItemUseCostFull(ss, itemRule.FlatMelee, "flatMelee", new RuleItemUseFlat(false, true));
			AddRuleItemUseCostFull(ss, itemRule.FlatUse, "flatUse", new RuleItemUseFlat(false, true));
			AddRuleItemUseCostFull(ss, itemRule.FlatThrow, "flatThrow", new RuleItemUseFlat(false, true));
			AddRuleItemUseCostFull(ss, itemRule.FlatPrime, "flatPrime", new RuleItemUseFlat(false, true));
			AddRuleItemUseCostFull(ss, itemRule.FlatUnprime, "flatUnprime", new RuleItemUseFlat(false, true));

			AddSection("{Script tags}", "", _white, true);
			{
				// missing: script hooks (addScriptTags)
				EndHeading();
			}

			AddSection("{Mod info}", "", _white);
			{
				AddSingleString(ss, mod.GetModCreatingRule(itemRule).name, "createdByMod");
				AddSingleString(ss, mod.GetModLastUpdatingRule(itemRule).name, "updatedByMod");
				EndHeading();
			}
		}
	}
}
#pragma warning restore CA1822
