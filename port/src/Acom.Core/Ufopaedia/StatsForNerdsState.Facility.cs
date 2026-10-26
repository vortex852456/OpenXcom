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
#nullable disable

using System.Text;

namespace Acom;

#pragma warning disable CA1822 // instance members in C++ (State._game is static here)
public partial class StatsForNerdsState
{
	/// <summary>Shows the "raw" data of the topic (Facility).</summary>
	private void InitFacilityList()
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
		RuleBaseFacility facilityRule = mod.GetBaseFacility(_topicId);
		if (facilityRule == null)
			return;

		_filterOptions.Clear();
		_cbxRelatedStuff.Visible = false;

		var ss = new StringBuilder();

		AddVectorOfStrings(ss, facilityRule.Requirements, "requires");

		if (facilityRule.SizeX == facilityRule.SizeY)
		{
			AddInteger(ss, facilityRule.SizeX, "size", 1);
		}
		else
		{
			AddInteger(ss, facilityRule.SizeX, "sizeX", 0);
			AddInteger(ss, facilityRule.SizeY, "sizeY", 0);
		}
		AddInteger(ss, facilityRule.BuildCost, "buildCost", 0, true);
		AddHeading("buildCostItems");
		{
			foreach (var costItem in facilityRule.BuildCostItems)
			{
				AddBuildCostItem(ss, costItem);
			}
			EndHeading();
		}
		AddInteger(ss, facilityRule.BuildTime, "buildTime");
		AddInteger(ss, facilityRule.MonthlyCost, "monthlyCost", 0, true);
		AddInteger(ss, facilityRule.RefundValue, "refundValue", 0, true);

		AddVectorOfStrings(ss, mod.GetBaseFunctionNames(facilityRule.RequireBaseFunc), "requiresBaseFunc");
		AddVectorOfStrings(ss, mod.GetBaseFunctionNames(facilityRule.ProvidedBaseFunc), "provideBaseFunc");
		AddVectorOfStrings(ss, mod.GetBaseFunctionNames(facilityRule.ForbiddenBaseFunc), "forbiddenBaseFunc");

		AddBoolean(ss, facilityRule.IsLift, "lift");
		AddBoolean(ss, facilityRule.IsHyperwave, "hyper");
		AddBoolean(ss, facilityRule.IsMindShield, "mind");
		AddInteger(ss, facilityRule.MindShieldPower, "mindPower", 1);
		AddBoolean(ss, facilityRule.IsGravShield, "grav");

		AddInteger(ss, facilityRule.Storage, "storage");
		AddInteger(ss, facilityRule.Personnel, "personnel");
		AddInteger(ss, facilityRule.Aliens, "aliens");
		AddInteger(ss, facilityRule.PrisonType, "prisonType");
		AddInteger(ss, facilityRule.Crafts, "crafts");
		AddInteger(ss, facilityRule.Laboratories, "labs");
		AddInteger(ss, facilityRule.Workshops, "workshops");
		AddInteger(ss, facilityRule.PsiLaboratories, "psiLabs");
		AddInteger(ss, facilityRule.TrainingFacilities, "trainingRooms");

		AddIntegerNauticalMiles(ss, facilityRule.SightRange, "sightRange");
		AddIntegerPercent(ss, facilityRule.SightChance, "sightChance");
		AddIntegerNauticalMiles(ss, facilityRule.RadarRange, "radarRange");
		AddIntegerPercent(ss, facilityRule.RadarChance, "radarChance");
		AddInteger(ss, facilityRule.DefenseValue, "defense");
		AddIntegerPercent(ss, facilityRule.HitRatio, "hitRatio");
		AddInteger(ss, facilityRule.AmmoMax, "ammoMax", 0);
		AddInteger(ss, facilityRule.RearmRate, "rearmRate", 1);
		AddInteger(ss, facilityRule.AmmoNeeded, "ammoNeeded", 1);
		AddBoolean(ss, facilityRule.UnifiedDamageFormula(), "unifiedDamageFormula");
		AddIntegerPercent(ss, facilityRule.ShieldDamageModifier, "shieldDamageModifier", 100);
		AddRule(ss, facilityRule.AmmoItem, "ammoItem");

		AddInteger(ss, facilityRule.MaxAllowedPerBase, "maxAllowedPerBase");
		AddInteger(ss, facilityRule.ManaRecoveryPerDay, "manaRecoveryPerDay");
		AddInteger(ss, facilityRule.HealthRecoveryPerDay, "healthRecoveryPerDay");
		AddFloat(ss, facilityRule.SickBayAbsoluteBonus, "sickBayAbsoluteBonus");
		AddFloat(ss, facilityRule.SickBayRelativeBonus, "sickBayRelativeBonus");

		AddRightClickActionType(ss, facilityRule.RightClickActionType, "rightClickActionType");

		AddVectorOfRules(ss, facilityRule.LeavesBehindOnSell, "leavesBehindOnSell");
		AddInteger(ss, facilityRule.RemovalTime, "removalTime");
		AddBoolean(ss, facilityRule.CanBeBuiltOver, "canBeBuiltOver");
		AddBoolean(ss, facilityRule.IsUpgradeOnly, "upgradeOnly");
		AddVectorOfRules(ss, facilityRule.BuildOverFacilities, "buildOverFacilities");

		if (facilityRule.DefenseValue > 0)
		{
			AddHeading("_calculatedValues");
			if (facilityRule.UnifiedDamageFormula() && facilityRule.AmmoItem != null)
			{
				var ss2 = new StringBuilder();
				ss2.Append(FormatInt(facilityRule.AmmoItem.DamageType.GetRandomDamage(facilityRule.DefenseValue, 1)));
				ss2.Append('-');
				ss2.Append(FormatInt(facilityRule.AmmoItem.DamageType.GetRandomDamage(facilityRule.DefenseValue, 2)));
				AddSingleString(ss, ss2.ToString(), "_damageRange", "", false);
			}
			else
			{
				// (damage) * (50-150% damage spread)
				var ss2 = new StringBuilder();
				ss2.Append(FormatInt(facilityRule.DefenseValue / 2));
				ss2.Append('-');
				ss2.Append(FormatInt(facilityRule.DefenseValue / 2 + facilityRule.DefenseValue));
				AddSingleString(ss, ss2.ToString(), "_damageRange", "", false);
			}
			EndHeading();
		}

		if (_showDebug)
		{
			AddSection("{Modding section}", "You don't need this info as a player", _white, true);

			AddSection("{Naming}", "", _white);
			AddSingleString(ss, facilityRule.Type, "type");
			AddSingleString(ss, facilityRule.UfopediaType, "ufopediaType");
			AddInteger(ss, facilityRule.ListOrder, "listOrder");
			AddInteger(ss, facilityRule.MissileAttraction, "missileAttraction", 100);
			AddRule(ss, facilityRule.DestroyedFacility, "destroyedFacility");

			AddSection("{Visuals}", "", _white);
			AddInteger(ss, facilityRule.FakeUnderwaterRaw, "fakeUnderwater", -1);
			AddSingleString(ss, facilityRule.MapName, "mapName");
			AddBoolean(ss, facilityRule.VerticalLevels.Count != 0, "verticalLevels*"); // just say if there are any or not
			AddVectorOfPositions(ss, facilityRule.StorageTiles, "storageTiles");

			AddInteger(ss, facilityRule.SpriteShape, "spriteShape", -1);
			AddSpriteResourcePath(ss, mod, "BASEBITS.PCK", facilityRule.SpriteShape);

			AddInteger(ss, facilityRule.SpriteFacility, "spriteFacility", -1);
			AddSpriteResourcePath(ss, mod, "BASEBITS.PCK", facilityRule.SpriteFacility);

			AddBoolean(ss, facilityRule.SpriteEnabledRaw, "spriteEnabled");
			AddBoolean(ss, facilityRule.ConnectorsDisabled(), "connectorsDisabled");

			AddSection("{Sounds}", "", _white);
			AddInteger(ss, facilityRule.FireSound, "fireSound");
			List<int> tmpSoundVector = [];
			tmpSoundVector.Add(facilityRule.FireSound);
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", tmpSoundVector);

			AddInteger(ss, facilityRule.HitSound, "hitSound");
			tmpSoundVector.Clear();
			tmpSoundVector.Add(facilityRule.HitSound);
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", tmpSoundVector);

			AddInteger(ss, facilityRule.PlaceSound, "placeSound", -1);
			tmpSoundVector.Clear();
			tmpSoundVector.Add(facilityRule.PlaceSound);
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", tmpSoundVector);

			AddSection("{Mod info}", "", _white);
			{
				AddSingleString(ss, mod.GetModCreatingRule(facilityRule).name, "createdByMod");
				AddSingleString(ss, mod.GetModLastUpdatingRule(facilityRule).name, "updatedByMod");
				EndHeading();
			}
		}
	}

	/// <summary>Shows the "raw" data of the topic (Craft).</summary>
	private void InitCraftList()
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
		RuleCraft craftRule = mod.GetCraft(_topicId);
		if (craftRule == null)
			return;

		_filterOptions.Clear();
		_cbxRelatedStuff.Visible = false;

		var ss = new StringBuilder();

		AddVectorOfStrings(ss, craftRule.Requirements, "requires");
		AddVectorOfStrings(ss, mod.GetBaseFunctionNames(craftRule.RequiresBuyBaseFunc), "requiresBuyBaseFunc");
		AddSingleString(ss, craftRule.RequiresBuyCountry, "requiresBuyCountry");

		AddInteger(ss, craftRule.BuyCost, "costBuy", 0, true);
		AddInteger(ss, craftRule.MonthlyBuyLimit, "monthlyBuyLimit");
		AddInteger(ss, craftRule.RentCost, "costRent", 0, true);
		AddInteger(ss, craftRule.SellCost, "costSell", 0, true);
		AddInteger(ss, craftRule.TransferTime, "transferTime", 24);

		AddInteger(ss, craftRule.MaxUnits, "soldiers");
		AddInteger(ss, craftRule.MaxUnitsLimit, "maxUnitsLimit", craftRule.MaxUnits);
		AddInteger(ss, craftRule.Pilots, "pilots");
		AddUnitStatBonus(ss, craftRule.PilotMinStatsRequired, "pilotMinStatsRequired");
		AddVectorOfRulesNamed(ss, craftRule.PilotSoldierBonusesRequired, "pilotSoldierBonusesRequired");
		AddInteger(ss, craftRule.MaxVehiclesAndLargeSoldiers, "vehicles");
		AddInteger(ss, craftRule.MaxVehiclesAndLargeSoldiersLimit, "maxHWPUnitsLimit", craftRule.MaxVehiclesAndLargeSoldiers);

		AddBoolean(ss, craftRule.IsOnlyOneSoldierGroupAllowed, "onlyOneSoldierGroupAllowed");
		AddVectorOfIntegers(ss, craftRule.AllowedSoldierGroups, "allowedSoldierGroups");
		AddVectorOfIntegers(ss, craftRule.AllowedArmorGroups, "allowedArmorGroups");
		AddMapOfIntegers(ss, craftRule.LimitArmorGroups, "limitArmorGroups");

		AddInteger(ss, craftRule.MaxSmallSoldiers, "maxSmallSoldiers", -1);
		AddInteger(ss, craftRule.MaxLargeSoldiers, "maxLargeSoldiers", -1);
		AddInteger(ss, craftRule.MaxSmallVehicles, "maxSmallVehicles", -1);
		AddInteger(ss, craftRule.MaxLargeVehicles, "maxLargeVehicles", -1);
		AddInteger(ss, craftRule.MaxSmallUnits, "maxSmallUnits", -1);
		AddInteger(ss, craftRule.MaxLargeUnits, "maxLargeUnits", -1);
		AddInteger(ss, craftRule.MaxSoldiers, "maxSoldiers", -1);
		AddInteger(ss, craftRule.MaxVehicles, "maxVehicles", -1);

		AddInteger(ss, craftRule.MaxItems, "maxItems", 999999);
		AddDouble(ss, craftRule.MaxStorageSpace, "maxStorageSpace", 99999.0);

		AddInteger(ss, craftRule.MaxAltitude, "maxAltitude", -1);
		AddInteger(ss, craftRule.Weapons, "weapons");

		// weaponStrings
		bool modded = false;
		for (int i = 0; i < RuleCraft.WeaponMax; ++i)
		{
			if (i == 0 && craftRule.GetWeaponSlotString(i) != "STR_WEAPON_ONE")
			{
				modded = true;
			}
			if (i == 1 && craftRule.GetWeaponSlotString(i) != "STR_WEAPON_TWO")
			{
				modded = true;
			}
			if (i > 1 && !string.IsNullOrEmpty(craftRule.GetWeaponSlotString(i)))
			{
				modded = true;
			}
		}
		List<string> tmp = [];
		if (modded)
		{
			for (int i = 0; i < RuleCraft.WeaponMax; ++i)
			{
				tmp.Add(craftRule.GetWeaponSlotString(i));
			}
		}
		AddVectorOfStrings(ss, tmp, "weaponStrings");

		// fixedWeapons
		modded = false;
		for (int i = 0; i < RuleCraft.WeaponMax; ++i)
		{
			if (!string.IsNullOrEmpty(craftRule.GetFixedWeaponInSlot(i)))
			{
				modded = true;
				break;
			}
		}
		tmp.Clear();
		if (modded)
		{
			for (int i = 0; i < RuleCraft.WeaponMax; ++i)
			{
				tmp.Add(craftRule.GetFixedWeaponInSlot(i));
			}
		}
		AddVectorOfStrings(ss, tmp, "fixedWeapons");

		// weaponTypes
		modded = false;
		for (int i = 0; i < RuleCraft.WeaponMax; ++i)
		{
			for (int j = 0; j < RuleCraft.WeaponTypeMax; ++j)
			{
				if (craftRule.GetWeaponTypesRaw(i, j) != 0)
				{
					modded = true;
					break;
				}
			}
			if (modded) break;
		}
		tmp.Clear();
		if (modded)
		{
			for (int i = 0; i < RuleCraft.WeaponMax; ++i)
			{
				var ss2 = new StringBuilder();
				int weaponType = craftRule.GetWeaponTypesRaw(i, 0);
				ss2.Append('{').Append(FormatInt(weaponType));
				for (int j = 1; j < RuleCraft.WeaponTypeMax; ++j)
				{
					if (weaponType != craftRule.GetWeaponTypesRaw(i, j))
					{
						weaponType = craftRule.GetWeaponTypesRaw(i, j);
						ss2.Append(',').Append(FormatInt(weaponType));
					}
				}
				ss2.Append('}');
				tmp.Add(ss2.ToString());
			}
		}
		AddVectorOfStrings(ss, tmp, "weaponTypes", false);

		AddHeading("stats");
		{
			AddInteger(ss, craftRule.MaxDamage, "damageMax");
			AddInteger(ss, craftRule.Stats.armor, "armor");
			AddIntegerPercent(ss, craftRule.Stats.avoidBonus, "avoidBonus");
			AddIntegerPercent(ss, craftRule.Stats.avoidBonus2, "avoidBonus2");
			AddIntegerPercent(ss, craftRule.Stats.powerBonus, "powerBonus");
			AddIntegerPercent(ss, craftRule.Stats.hitBonus, "hitBonus");
			AddInteger(ss, craftRule.MaxFuel, "fuelMax");
			AddIntegerKnots(ss, craftRule.MaxSpeed, "speedMax");
			AddInteger(ss, craftRule.Acceleration, "accel");
			AddIntegerNauticalMiles(ss, craftRule.RadarRange, "radarRange", 672);
			AddIntegerPercent(ss, craftRule.RadarChance, "radarChance", 100);
			AddIntegerNauticalMiles(ss, craftRule.SightRange, "sightRange", 1696);
			AddInteger(ss, craftRule.Stats.shieldCapacity, "shieldCapacity");
			AddInteger(ss, craftRule.Stats.shieldRecharge, "shieldRecharge");
			AddInteger(ss, craftRule.Stats.shieldRechargeInGeoscape, "shieldRechargeInGeoscape");
			AddInteger(ss, craftRule.Stats.shieldBleedThrough, "shieldBleedThrough");
			EndHeading();
		}
		AddInteger(ss, craftRule.ShieldRechargeAtBase, "shieldRechargedAtBase", 1000);

		AddRule(ss, craftRule.RefuelItem, "refuelItem");
		AddInteger(ss, craftRule.RefuelRate, "refuelRate", 1);
		AddInteger(ss, craftRule.RepairRate, "repairRate", 1);

		AddBoolean(ss, craftRule.Spacecraft, "spacecraft");
		AddBoolean(ss, craftRule.AllowLanding, "allowLanding", true);
		AddBoolean(ss, craftRule.NotifyWhenRefueled(), "notifyWhenRefueled");
		AddBoolean(ss, craftRule.CanAutoPatrol(), "autoPatrol");
		AddBoolean(ss, craftRule.IsUndetectable, "undetectable");
		AddBoolean(ss, craftRule.PatrolWithoutFuel(), "patrolWithoutFuel");

		AddBoolean(ss, craftRule.KeepCraftAfterFailedMission(), "keepCraftAfterFailedMission");

		AddHeading("_calculatedValues");
		{
			if (craftRule.CalculateRange(0) != -1)
			{
				AddIntegerNauticalMiles(ss, craftRule.CalculateRange(0), "_maxRange");
				AddIntegerNauticalMiles(ss, craftRule.CalculateRange(1), "_minRange");
				AddIntegerNauticalMiles(ss, craftRule.CalculateRange(2), "_avgRange");
			}
			else
			{
				AddSingleString(ss, "STR_INFINITE_RANGE", "_maxRange");
				AddSingleString(ss, "STR_INFINITE_RANGE", "_minRange");
				AddSingleString(ss, "STR_INFINITE_RANGE", "_avgRange");
			}
			EndHeading();
		}

		ModScript.ScriptCallback(craftRule.GetScript(ModScript.StatsForNerdsCraft), craftRule, this, _game.SavedGame);

		if (_showDebug)
		{
			AddSection("{Modding section}", "You don't need this info as a player", _white, true);

			AddSection("{Naming}", "", _white);
			AddSingleString(ss, craftRule.Type, "type");
			AddInteger(ss, craftRule.ListOrder, "listOrder");
			AddSingleString(ss, craftRule.MonthlyBuyLimitMessage, "monthlyBuyLimitMessage");

			AddSection("{Geoscape}", "", _white);
			AddSingleString(ss, craftRule.DefaultDisplayAltitude, "defaultAltitude", "STR_VERY_LOW");
			AddInteger(ss, craftRule.GetSprite(0), "sprite (Minimized)", -1);
			AddSpriteResourcePath(ss, mod, "INTICON.PCK", craftRule.GetSprite(0));
			AddInteger(ss, craftRule.GetSprite(0) + 11, "_sprite (Dogfight)", 10);
			AddSpriteResourcePath(ss, mod, "INTICON.PCK", craftRule.GetSprite(0) + 11);
			AddInteger(ss, craftRule.GetSprite(0) + 33, "_sprite (Base)", 32);
			AddSpriteResourcePath(ss, mod, "BASEBITS.PCK", craftRule.GetSprite(0) + 33);
			AddInteger(ss, craftRule.Marker, "marker", -1);
			AddInteger(ss, craftRule.Score, "score");
			AddInteger(ss, craftRule.MaxSkinIndex, "maxSkinIndex");
			AddBoolean(ss, craftRule.SkinSpritesRaw.Count != 0, "skinSprites", false); // just say if there is any or not

			AddSection("{Sounds}", "", _white);
			AddVectorOfIntegers(ss, craftRule.SelectSoundRaw, "selectSound");
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", craftRule.SelectSoundRaw);
			AddVectorOfIntegers(ss, craftRule.TakeoffSoundRaw, "takeoffSound");
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", craftRule.TakeoffSoundRaw);

			AddSection("{Battlescape}", "", _white);
			AddBoolean(ss, craftRule.BattlescapeTerrainData != null, "battlescapeTerrainData", false); // just say if there is any or not
			AddBoolean(ss, craftRule.IsMapVisible, "mapVisible", true);
			AddVectorOfIntegers(ss, craftRule.CraftInventoryTile, "craftInventoryTile");
			AddVectorOfIntegers(ss, craftRule.Groups, "groups");
			AddBoolean(ss, craftRule.UseAllStartTiles(), "useAllStartTiles");
			AddSingleString(ss, craftRule.CustomPreviewTypeRaw, "customPreview");
			if (craftRule.Deployment.Count == 0)
			{
				List<int> dummy = [];
				AddVectorOfIntegers(ss, dummy, "deployment");
			}
			else
			{
				AddHeading("deployment");
				{
					foreach (var d in craftRule.Deployment)
					{
						AddVectorOfIntegers(ss, d, "");
					}
					EndHeading();
				}
			}

			AddSection("{Script tags}", "", _white, true);
			{
				AddScriptTags(ss, craftRule.GetScriptValuesRaw());
				EndHeading();
			}

			AddSection("{Mod info}", "", _white);
			{
				AddSingleString(ss, mod.GetModCreatingRule(craftRule).name, "createdByMod");
				AddSingleString(ss, mod.GetModLastUpdatingRule(craftRule).name, "updatedByMod");
				EndHeading();
			}
		}
	}
}
#pragma warning restore CA1822
