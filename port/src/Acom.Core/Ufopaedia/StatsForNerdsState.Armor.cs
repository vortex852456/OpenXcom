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
	/// <summary>Shows the "raw" data of the topic (Armor).</summary>
	private void InitArmorList()
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
		Armor armorRule = mod.GetArmor(_topicId);
		if (armorRule == null)
			return;

		_filterOptions.Clear();
		_cbxRelatedStuff.Visible = false;

		var ss = new StringBuilder();

		AddUnitStatBonus(ss, armorRule.Stats, "stats");

		AddInteger(ss, armorRule.Weight, "weight");

		AddInteger(ss, armorRule.Size, "size", 1);

		AddSpecialAbility(ss, (SpecialAbility)armorRule.SpecialAbility, "specab");
		AddMovementType(ss, armorRule.MovementType, "movementType");

		AddBoolean(ss, armorRule.AllowsRunning(), "allowsRunning", true);
		AddBoolean(ss, armorRule.AllowsStrafing(), "allowsStrafing", true);
		AddBoolean(ss, armorRule.AllowsSneaking(), "allowsSneaking", true);
		AddBoolean(ss, armorRule.AllowsKneeling(), "allowsKneeling", true);
		AddBoolean(ss, armorRule.AllowsMoving(), "allowsMoving", true);

		bool fearImmuneDefault = false;
		bool bleedImmuneDefault = false;
		bool painImmuneDefault = false;
		bool zombiImmuneDefault = false;
		bool ignoresMeleeThreatDefault = false;
		bool createsMeleeThreatDefault = true;

		if (armorRule.Size != 1)
		{
			fearImmuneDefault = true;
			bleedImmuneDefault = true;
			painImmuneDefault = true;
			zombiImmuneDefault = true;
			ignoresMeleeThreatDefault = true;
			createsMeleeThreatDefault = false;
		}

		AddBoolean(ss, armorRule.GetFearImmune(), "fearImmune", fearImmuneDefault);
		AddBoolean(ss, armorRule.GetBleedImmune(), "bleedImmune", bleedImmuneDefault); // not considering alienBleeding option!
		AddBoolean(ss, armorRule.GetPainImmune(), "painImmune", painImmuneDefault);
		AddBoolean(ss, armorRule.GetZombiImmune(), "zombiImmune", zombiImmuneDefault);
		AddBoolean(ss, armorRule.GetIgnoresMeleeThreat(), "ignoresMeleeThreat", ignoresMeleeThreatDefault);
		AddBoolean(ss, armorRule.GetCreatesMeleeThreat(), "createsMeleeThreat", createsMeleeThreatDefault);

		_filterOptions.Clear();
		foreach (var biw in armorRule.BuiltInWeapons)
		{
			// ignore dummy inventory padding
			if (!biw.Type.Contains("INV_NULL", StringComparison.Ordinal) || _showDebug)
			{
				_filterOptions.Add(biw.Type);
			}
		}
		AddVectorOfStrings(ss, _filterOptions, "builtInWeapons");
		AddRule(ss, armorRule.SpecialWeapon, "specialWeapon");

		if (armorRule.SpecialWeapon != null)
		{
			_filterOptions.Add(armorRule.SpecialWeapon.Type);
		}

		_filterOptions.Insert(0, "STR_BUILT_IN_ITEMS");
		_cbxRelatedStuff.SetOptions(_filterOptions, true);
		_cbxRelatedStuff.Visible = _filterOptions.Count > 1;
		if (_filterOptions.Count > 1)
		{
			_txtTitle.Align = TextHAlign.ALIGN_LEFT;
		}

		AddIntegerPercent(ss, armorRule.VisibilityThroughSmoke, "heatVision"); // visibilityThroughSmoke
		AddIntegerPercent(ss, armorRule.VisibilityThroughFire, "visibilityThroughFire", 100);
		AddInteger(ss, armorRule.PsiVision, "psiVision");
		AddInteger(ss, armorRule.PsiCamouflage, "psiCamouflage");

		AddInteger(ss, armorRule.VisibilityAtDay, "visibilityAtDay");
		AddInteger(ss, armorRule.VisibilityAtDark, "visibilityAtDark");
		AddInteger(ss, armorRule.CamouflageAtDay, "camouflageAtDay");
		AddInteger(ss, armorRule.CamouflageAtDark, "camouflageAtDark");
		AddInteger(ss, armorRule.AntiCamouflageAtDay, "antiCamouflageAtDay");
		AddInteger(ss, armorRule.AntiCamouflageAtDark, "antiCamouflageAtDark");

		AddRuleStatBonus(ss, armorRule.PsiDefenceRaw, "psiDefence");
		AddRuleStatBonus(ss, armorRule.MeleeDodgeRaw, "meleeDodge");
		AddFloatAsPercentage(ss, armorRule.MeleeDodgeBackPenalty, "meleeDodgeBackPenalty");

		AddHeading("recovery");
		{
			AddRuleStatBonus(ss, armorRule.TimeRecoveryRaw, "time");
			AddRuleStatBonus(ss, armorRule.EnergyRecoveryRaw, "energy");
			AddRuleStatBonus(ss, armorRule.MoraleRecoveryRaw, "morale");
			AddRuleStatBonus(ss, armorRule.HealthRecoveryRaw, "health");
			AddRuleStatBonus(ss, armorRule.StunRegenerationRaw, "stun");
			AddRuleStatBonus(ss, armorRule.ManaRecoveryRaw, "mana");
			EndHeading();
		}

		AddVectorOfRules(ss, armorRule.UnitsRaw, "units");
		AddVectorOfIntegers(ss, armorRule.RanksRaw, "ranks");
		AddRule(ss, armorRule.RequiredAward, "requiresAward");
		AddRuleNamed(ss, armorRule.RequiredBonus, "requiresBonus");

		// missing: script hooks (ModScript::scriptCallback<StatsForNerds...>)

		if (_showDebug)
		{
			AddSection("{Modding section}", "You don't need this info as a player", _white, true);

			AddSection("{Naming}", "", _white);
			AddSingleString(ss, armorRule.Type, "type");
			AddSingleString(ss, armorRule.UfopediaType, "ufopediaType");
			AddInteger(ss, armorRule.Group, "group");
			AddInteger(ss, armorRule.ListOrder, "listOrder");
			AddRuleNamed(ss, armorRule.RequiredResearch, "requires");

			AddSection("{Recovery}", "", _white);
			AddVectorOfRules(ss, armorRule.CorpseBattlescape, "corpseBattle");
			AddRule(ss, armorRule.CorpseGeoscape, "corpseGeo");
			AddRule(ss, armorRule.SelfDestructItem, "selfDestructItem");
			AddRule(ss, armorRule.StoreItem, "storeItem");

			AddSection("{Inventory}", "", _white);
			AddSingleString(ss, armorRule.SpriteInventory, "spriteInv", "", false);
			AddBoolean(ss, armorRule.HasInventory, "allowInv", true);
			AddSingleString(ss, armorRule.LayersDefaultPrefix, "layersDefaultPrefix", "", false);
			AddBoolean(ss, armorRule.LayersSpecificPrefix.Count != 0, "layersSpecificPrefix*", false); // just say if there are any or not
			AddBoolean(ss, armorRule.LayersDefinition.Count != 0, "layersDefinition*", false); // just say if there are any or not

			AddSection("{Sprites}", "", _white);
			AddVectorOfIntegers(ss, armorRule.CustomArmorPreviewIndex, "customArmorPreviewIndex");
			AddSingleString(ss, armorRule.SpriteSheet, "spriteSheet", "", false);
			AddInteger(ss, armorRule.FaceColorGroup, "spriteFaceGroup");
			AddVectorOfIntegers(ss, armorRule.FaceColorRaw, "spriteFaceColor");
			AddInteger(ss, armorRule.HairColorGroup, "spriteHairGroup");
			AddVectorOfIntegers(ss, armorRule.HairColorRaw, "spriteHairColor");
			AddInteger(ss, armorRule.UtileColorGroup, "spriteUtileGroup");
			AddVectorOfIntegers(ss, armorRule.UtileColorRaw, "spriteUtileColor");
			AddInteger(ss, armorRule.RankColorGroup, "spriteRankGroup");
			AddVectorOfIntegers(ss, armorRule.RankColorRaw, "spriteRankColor");

			AddSection("{Sounds}", "", _white);
			AddInteger(ss, armorRule.MoveSound, "moveSound", -1);
			List<int> tmpSoundVector = [];
			tmpSoundVector.Add(armorRule.MoveSound);
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", tmpSoundVector);
			AddVectorOfIntegers(ss, armorRule.MaleDeathSounds, "deathMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.MaleDeathSounds);
			AddVectorOfIntegers(ss, armorRule.FemaleDeathSounds, "deathFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.FemaleDeathSounds);
			AddVectorOfIntegers(ss, armorRule.MaleSelectUnitSounds, "selectUnitMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.MaleSelectUnitSounds);
			AddVectorOfIntegers(ss, armorRule.FemaleSelectUnitSounds, "selectUnitFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.FemaleSelectUnitSounds);
			AddVectorOfIntegers(ss, armorRule.MaleStartMovingSounds, "startMovingMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.MaleStartMovingSounds);
			AddVectorOfIntegers(ss, armorRule.FemaleStartMovingSounds, "startMovingFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.FemaleStartMovingSounds);
			AddVectorOfIntegers(ss, armorRule.MaleSelectWeaponSounds, "selectWeaponMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.MaleSelectWeaponSounds);
			AddVectorOfIntegers(ss, armorRule.FemaleSelectWeaponSounds, "selectWeaponFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.FemaleSelectWeaponSounds);
			AddVectorOfIntegers(ss, armorRule.MaleAnnoyedSounds, "annoyedMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.MaleAnnoyedSounds);
			AddVectorOfIntegers(ss, armorRule.FemaleAnnoyedSounds, "annoyedFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", armorRule.FemaleAnnoyedSounds);

			AddSection("{Animations}", "", _white);
			AddDrawingRoutine(ss, armorRule.DrawingRoutine, "drawingRoutine");
			AddBoolean(ss, armorRule.DrawBubbles(), "drawBubbles");
			AddBoolean(ss, armorRule.ConstantAnimation, "constantAnimation");
			AddForcedTorso(ss, armorRule.ForcedTorso, "forcedTorso");
			AddInteger(ss, armorRule.DeathFrames, "deathFrames", 3);

			AddSection("{Calculations}", "", _white);
			AddVectorOfIntegers(ss, armorRule.LoftempsSet, "loftempsSet");
			AddInteger(ss, armorRule.PersonalLightFriend, "personalLight", 15);
			AddInteger(ss, armorRule.PersonalLightHostile, "personalLightHostile", 0);
			AddInteger(ss, armorRule.PersonalLightNeutral, "personalLightNeutral", 0);
			AddInteger(ss, armorRule.StandHeight, "standHeight", -1);
			AddInteger(ss, armorRule.KneelHeight, "kneelHeight", -1);
			AddInteger(ss, armorRule.FloatHeight, "floatHeight", -1);
			AddInteger(ss, armorRule.MeleeOriginVoxelVerticalOffset, "meleeOriginVoxelVerticalOffset");
			AddFloat(ss, armorRule.OverKill, "overKill", 0.5f);
			AddBoolean(ss, armorRule.IsPilotArmor, "isPilotArmor");
			AddBoolean(ss, armorRule.AllowTwoMainWeapons, "allowTwoMainWeapons");
			AddBoolean(ss, armorRule.InstantWoundRecovery, "instantWoundRecovery");

			AddHeading("moveCost");
			{
				AddRuleArmorMoveCost(ss, armorRule.MoveCostBase, "basePercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 100 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostBaseFly, "baseFlyPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 100 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostBaseClimb, "baseClimbPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 100 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostBaseNormal, "baseNormalPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 100 });

				AddRuleArmorMoveCost(ss, armorRule.MoveCostWalk, "walkPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 50 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostRun, "runPercent", new ArmorMoveCost { TimePercent = 75, EnergyPercent = 75 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostStrafe, "strafePercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 50 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostSneak, "sneakPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 50 });

				AddRuleArmorMoveCost(ss, armorRule.MoveCostFlyWalk, "flyWalkPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 50 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostFlyRun, "flyRunPercent", new ArmorMoveCost { TimePercent = 75, EnergyPercent = 75 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostFlyStrafe, "flyStrafePercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 50 });

				AddRuleArmorMoveCost(ss, armorRule.MoveCostFlyUp, "flyUpPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 0 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostFlyDown, "flyDownPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 0 });

				AddRuleArmorMoveCost(ss, armorRule.MoveCostClimbUp, "climbUpPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 50 });
				AddRuleArmorMoveCost(ss, armorRule.MoveCostClimbDown, "climbDownPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 50 });

				AddRuleArmorMoveCost(ss, armorRule.MoveCostGravLift, "gravLiftPercent", new ArmorMoveCost { TimePercent = 100, EnergyPercent = 0 });

				EndHeading();
			}

			AddSection("{Basics}", "Stuff from the main article", _white, true);
			AddInteger(ss, armorRule.FrontArmor, "frontArmor");
			AddInteger(ss, armorRule.RightSideArmor, "sideArmor");
			AddInteger(ss, armorRule.LeftSideArmor - armorRule.RightSideArmor, "leftArmorDiff");
			AddInteger(ss, armorRule.RearArmor, "rearArmor");
			AddInteger(ss, armorRule.UnderArmor, "underArmor");

			AddArmorDamageModifiers(ss, armorRule.DamageModifiersRaw, "damageModifier");

			AddSection("{Script tags}", "", _white, true);
			{
				// missing: script hooks (addScriptTags)
				EndHeading();
			}

			AddSection("{Mod info}", "", _white);
			{
				AddSingleString(ss, mod.GetModCreatingRule(armorRule).name, "createdByMod");
				AddSingleString(ss, mod.GetModLastUpdatingRule(armorRule).name, "updatedByMod");
				EndHeading();
			}
		}
	}

	/// <summary>Shows the "raw" data of the topic (SoldierBonus).</summary>
	private void InitSoldierBonusList()
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
		RuleSoldierBonus bonusRule = mod.GetSoldierBonus(_topicId);
		if (bonusRule == null)
			return;

		_filterOptions.Clear();
		_cbxRelatedStuff.Visible = false;

		var ss = new StringBuilder();

		AddUnitStatBonus(ss, bonusRule.Stats, "stats");

		AddInteger(ss, bonusRule.FrontArmor, "frontArmor");
		AddInteger(ss, bonusRule.RightSideArmor, "sideArmor");
		AddInteger(ss, bonusRule.LeftSideArmor - bonusRule.RightSideArmor, "leftArmorDiff");
		AddInteger(ss, bonusRule.RearArmor, "rearArmor");
		AddInteger(ss, bonusRule.UnderArmor, "underArmor");

		AddInteger(ss, bonusRule.VisibilityAtDark, "visibilityAtDark");
		AddInteger(ss, bonusRule.VisibilityAtDay, "visibilityAtDay");
		AddInteger(ss, bonusRule.PsiVision, "psiVision");
		AddInteger(ss, bonusRule.VisibilityThroughSmoke, "heatVision"); // visibilityThroughSmoke
		AddInteger(ss, bonusRule.VisibilityThroughFire, "visibilityThroughFire", 0);

		AddHeading("recovery");
		{
			AddRuleStatBonus(ss, bonusRule.TimeRecoveryRaw, "time");
			AddRuleStatBonus(ss, bonusRule.EnergyRecoveryRaw, "energy");
			AddRuleStatBonus(ss, bonusRule.MoraleRecoveryRaw, "morale");
			AddRuleStatBonus(ss, bonusRule.HealthRecoveryRaw, "health");
			AddRuleStatBonus(ss, bonusRule.StunRegenerationRaw, "stun");
			AddRuleStatBonus(ss, bonusRule.ManaRecoveryRaw, "mana");
			EndHeading();
		}

		if (_showDebug)
		{
			AddSection("{Script tags}", "", _white, true);
			{
				// missing: script hooks (addScriptTags)
				EndHeading();
			}

			AddSection("{Mod info}", "", _white);
			{
				AddSingleString(ss, mod.GetModCreatingRule(bonusRule).name, "createdByMod");
				AddSingleString(ss, mod.GetModLastUpdatingRule(bonusRule).name, "updatedByMod");
				EndHeading();
			}
		}
	}
}
#pragma warning restore CA1822
