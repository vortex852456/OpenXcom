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
	/// <summary>Shows the "raw" data of the topic (Ufo).</summary>
	private void InitUfoList()
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
		RuleUfo ufoRule = mod.GetUfo(_topicId);
		if (ufoRule == null)
			return;

		_filterOptions.Clear();
		_cbxRelatedStuff.Visible = false;

		var ss = new StringBuilder();

		AddSingleString(ss, ufoRule.Size, "size");

		AddIntegerPercent(ss, ufoRule.HunterKillerPercentage, "hunterKillerPercentage");
		AddHuntMode(ss, ufoRule.HuntMode, "huntMode");
		AddIntegerPercent(ss, ufoRule.HuntSpeed, "huntSpeed", 100);
		AddHuntBehavior(ss, ufoRule.HuntBehavior, "huntBehavior", 2);

		AddInteger(ss, ufoRule.WeaponPower, "power");
		AddIntegerKm(ss, ufoRule.WeaponRange, "range");
		AddIntegerSeconds(ss, ufoRule.WeaponReload, "reload");
		AddIntegerSeconds(ss, ufoRule.BreakOffTime, "breakOffTime");
		AddInteger(ss, ufoRule.Score, "score");
		AddInteger(ss, ufoRule.MissionScore, "missionScore", 1);

		AddHeading("stats");
		{
			AddInteger(ss, ufoRule.Stats.damageMax, "damageMax");
			AddInteger(ss, ufoRule.Stats.armor, "armor");
			AddIntegerPercent(ss, ufoRule.Stats.avoidBonus, "avoidBonus");
			AddIntegerPercent(ss, ufoRule.Stats.avoidBonus2, "avoidBonus2");
			AddIntegerPercent(ss, ufoRule.Stats.powerBonus, "powerBonus");
			AddIntegerPercent(ss, ufoRule.Stats.hitBonus, "hitBonus");
			AddInteger(ss, ufoRule.Stats.fuelMax, "fuelMax");
			AddIntegerKnots(ss, ufoRule.Stats.speedMax, "speedMax");
			AddInteger(ss, ufoRule.Stats.accel, "accel");
			AddIntegerNauticalMiles(ss, ufoRule.Stats.radarRange, "radarRange", 672);
			AddIntegerPercent(ss, ufoRule.Stats.radarChance, "radarChance");
			AddIntegerNauticalMiles(ss, ufoRule.Stats.sightRange, "sightRange", 268);
			AddInteger(ss, ufoRule.Stats.shieldCapacity, "shieldCapacity");
			AddInteger(ss, ufoRule.Stats.shieldRecharge, "shieldRecharge");
			AddInteger(ss, ufoRule.Stats.shieldRechargeInGeoscape, "shieldRechargeInGeoscape");
			AddInteger(ss, ufoRule.Stats.shieldBleedThrough, "shieldBleedThrough");
			// UFO-specific
			AddSingleString(ss, ufoRule.Stats.craftCustomDeploy, "craftCustomDeploy");
			AddSingleString(ss, ufoRule.Stats.missionCustomDeploy, "missionCustomDeploy");
			EndHeading();
		}

		AddHeading("_calculatedValues", "STR_FOR_DIFFICULTY", true);
		{
			int escapeCountdown = ufoRule.BreakOffTime - 30 * _game.SavedGame.DifficultyCoefficient;
			int escapeCountdownMax = escapeCountdown + ufoRule.BreakOffTime;
			{
				int diff = (int)_game.SavedGame.Difficulty;
				var custom = _game.Mod.UfoEscapeCountdownCoefficients;
				if ((uint)custom.Count > (uint)diff /* size_t comparison */)
				{
					escapeCountdown = ufoRule.BreakOffTime * custom[diff] / 100;
					escapeCountdownMax = ufoRule.BreakOffTime * 2 * custom[diff] / 100;
				}
			}
			AddIntegerSeconds(ss, Math.Max(1, escapeCountdown), "_escapeCountdown", 0, Math.Max(1, escapeCountdownMax));

			int fireCountdown = Math.Max(1, (ufoRule.WeaponReload - 2 * _game.SavedGame.DifficultyCoefficient));
			{
				int diff = (int)_game.SavedGame.Difficulty;
				var custom = _game.Mod.UfoFiringRateCoefficients;
				if ((uint)custom.Count > (uint)diff /* size_t comparison */)
				{
					fireCountdown = Math.Max(1, ufoRule.WeaponReload * custom[diff] / 100);
				}
			}
			AddIntegerSeconds(ss, fireCountdown, "_fireRate", 0, fireCountdown * 2);

			// not considering race bonus
			int totalPower = ufoRule.WeaponPower * (ufoRule.Stats.powerBonus + 100) / 100;
			// spread 0-100%
			double avgDamage = totalPower * 0.5;
			// not considering craft dodge, pilot dodge and evasive maneuvers
			int totalAccuracy = 60 + ufoRule.Stats.hitBonus;
			// spread 100-200%
			double avgFireRate = fireCountdown * 1.5;
			double avgDpm = avgDamage * (totalAccuracy / 100.0) * (60.0 / avgFireRate);
			AddInteger(ss, (int)Round(avgDpm), "_averageDPM");

			EndHeading();
		}

		foreach (var raceBonus in ufoRule.RaceBonusRaw)
		{
			if (raceBonus.Key.Length != 0)
			{
				AddHeading("STR_RACE_BONUS", raceBonus.Key);
				{
					AddInteger(ss, raceBonus.Value.damageMax, "damageMax");
					AddInteger(ss, raceBonus.Value.armor, "armor");
					AddIntegerPercent(ss, raceBonus.Value.avoidBonus, "avoidBonus");
					AddIntegerPercent(ss, raceBonus.Value.avoidBonus2, "avoidBonus2");
					AddIntegerPercent(ss, raceBonus.Value.powerBonus, "powerBonus");
					AddIntegerPercent(ss, raceBonus.Value.hitBonus, "hitBonus");
					AddInteger(ss, raceBonus.Value.fuelMax, "fuelMax");
					AddIntegerKnots(ss, raceBonus.Value.speedMax, "speedMax");
					AddInteger(ss, raceBonus.Value.accel, "accel");
					AddIntegerNauticalMiles(ss, raceBonus.Value.radarRange, "radarRange", 0);
					AddIntegerPercent(ss, raceBonus.Value.radarChance, "radarChance");
					AddIntegerNauticalMiles(ss, raceBonus.Value.sightRange, "sightRange", 0);
					AddInteger(ss, raceBonus.Value.shieldCapacity, "shieldCapacity");
					AddInteger(ss, raceBonus.Value.shieldRecharge, "shieldRecharge");
					AddInteger(ss, raceBonus.Value.shieldRechargeInGeoscape, "shieldRechargeInGeoscape");
					AddInteger(ss, raceBonus.Value.shieldBleedThrough, "shieldBleedThrough");
					// UFO-specific
					AddSingleString(ss, raceBonus.Value.craftCustomDeploy, "craftCustomDeploy");
					AddSingleString(ss, raceBonus.Value.missionCustomDeploy, "missionCustomDeploy");
					EndHeading();
				}
			}
		}

		ModScript.ScriptCallback(ufoRule.GetScript(ModScript.StatsForNerdsUfo), ufoRule, this, _game.SavedGame);

		if (_showDebug)
		{
			AddSection("{Modding section}", "You don't need this info as a player", _white, true);

			AddSection("{Naming}", "", _white);
			AddSingleString(ss, ufoRule.Type, "type");

			AddSection("{Exotic}", "", _white);
			AddInteger(ss, ufoRule.SoftlockThreshold, "softlockThreshold", 100);
			AddSingleString(ss, ufoRule.HitImage, "hitImage");
			AddInteger(ss, ufoRule.MissilePower, "missilePower");
			AddInteger(ss, ufoRule.MissileStopChance, "missileStopChance");
			AddBoolean(ss, ufoRule.IsUnmanned, "unmanned");
			AddBoolean(ss, ufoRule.IsInstaHyper, "instaHyper");
			AddBoolean(ss, ufoRule.IsNoAlert, "noAlert");
			AddInteger(ss, ufoRule.SplashdownSurvivalChance, "splashdownSurvivalChance", 100);
			AddInteger(ss, ufoRule.FakeWaterLandingChance, "fakeWaterLandingChance", 0);

			AddSection("{Visuals}", "", _white);
			AddInteger(ss, ufoRule.Radius, "radius", -1);
			AddInteger(ss, ufoRule.DefaultVisibility, "visibility");
			AddInteger(ss, ufoRule.BlobSize, "blobSize", -1);

			AddInteger(ss, ufoRule.Sprite, "sprite", -1); // INTERWIN.DAT
			AddSingleString(ss, ufoRule.ModSprite, "modSprite", "", false);
			AddInteger(ss, ufoRule.Marker, "marker", -1);
			AddInteger(ss, ufoRule.LandMarker, "markerLand", -1);
			AddInteger(ss, ufoRule.CrashMarker, "markerCrash", -1);
			AddBoolean(ss, ufoRule.BattlescapeTerrainData != null, "battlescapeTerrainData", false); // just say if there is any or not

			AddSection("{Sounds}", "", _white);
			AddInteger(ss, ufoRule.FireSound, "fireSound", -1);
			List<int> tmpSoundVector = [];
			tmpSoundVector.Add(ufoRule.FireSound);
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", tmpSoundVector);

			AddInteger(ss, ufoRule.AlertSound, "alertSound", -1);
			tmpSoundVector.Clear();
			tmpSoundVector.Add(ufoRule.AlertSound);
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", tmpSoundVector);

			AddInteger(ss, ufoRule.HuntAlertSound, "huntAlertSound", -1);
			tmpSoundVector.Clear();
			tmpSoundVector.Add(ufoRule.HuntAlertSound);
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", tmpSoundVector);

			AddInteger(ss, ufoRule.HitSound, "hitSound", -1);
			tmpSoundVector.Clear();
			tmpSoundVector.Add(ufoRule.HitSound);
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", tmpSoundVector);

			AddSection("{Script tags}", "", _white, true);
			{
				AddScriptTags(ss, ufoRule.GetScriptValuesRaw());
				EndHeading();
			}

			AddSection("{Mod info}", "", _white);
			{
				AddSingleString(ss, mod.GetModCreatingRule(ufoRule).name, "createdByMod");
				AddSingleString(ss, mod.GetModLastUpdatingRule(ufoRule).name, "updatedByMod");
				EndHeading();
			}
		}
	}

	/// <summary>Shows the "raw" data of the topic (CraftWeapon).</summary>
	private void InitCraftWeaponList()
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
		RuleCraftWeapon craftWeaponRule = mod.GetCraftWeapon(_topicId);
		if (craftWeaponRule == null)
			return;

		_filterOptions.Clear();
		_cbxRelatedStuff.Visible = false;

		var ss = new StringBuilder();

		AddInteger(ss, craftWeaponRule.TractorBeamPower, "tractorBeamPower");
		AddInteger(ss, craftWeaponRule.Damage, "damage");
		AddBoolean(ss, craftWeaponRule.UnifiedDamageFormula(), "unifiedDamageFormula");
		AddIntegerPercent(ss, craftWeaponRule.ShieldDamageModifier, "shieldDamageModifier", 100);
		AddIntegerKm(ss, craftWeaponRule.Range, "range");
		AddIntegerPercent(ss, craftWeaponRule.Accuracy, "accuracy");
		AddIntegerSeconds(ss, craftWeaponRule.CautiousReload, "reloadCautious");
		AddIntegerSeconds(ss, craftWeaponRule.StandardReload, "reloadStandard");
		AddIntegerSeconds(ss, craftWeaponRule.AggressiveReload, "reloadAggressive");

		AddRule(ss, craftWeaponRule.LauncherItem, "launcher");
		AddInteger(ss, craftWeaponRule.WeaponType, "weaponType");

		AddRule(ss, craftWeaponRule.ClipItem, "clip");
		AddInteger(ss, craftWeaponRule.AmmoMax, "ammoMax");
		AddInteger(ss, craftWeaponRule.RearmRate, "rearmRate", 1);
		AddBoolean(ss, craftWeaponRule.UseStatisticalBulletSaving(), "bulletSaving");

		AddHeading("stats");
		{
			AddInteger(ss, craftWeaponRule.BonusStats.damageMax, "damageMax");
			AddInteger(ss, craftWeaponRule.BonusStats.armor, "armor");
			AddIntegerPercent(ss, craftWeaponRule.BonusStats.avoidBonus, "avoidBonus");
			AddIntegerPercent(ss, craftWeaponRule.BonusStats.avoidBonus2, "avoidBonus2");
			AddIntegerPercent(ss, craftWeaponRule.BonusStats.powerBonus, "powerBonus");
			AddIntegerPercent(ss, craftWeaponRule.BonusStats.hitBonus, "hitBonus");
			AddInteger(ss, craftWeaponRule.BonusStats.fuelMax, "fuelMax");
			AddIntegerKnots(ss, craftWeaponRule.BonusStats.speedMax, "speedMax");
			AddInteger(ss, craftWeaponRule.BonusStats.accel, "accel");
			AddIntegerNauticalMiles(ss, craftWeaponRule.BonusStats.radarRange, "radarRange");
			AddIntegerPercent(ss, craftWeaponRule.BonusStats.radarChance, "radarChance");
			AddIntegerNauticalMiles(ss, craftWeaponRule.BonusStats.sightRange, "sightRange");
			AddInteger(ss, craftWeaponRule.BonusStats.shieldCapacity, "shieldCapacity");
			AddInteger(ss, craftWeaponRule.BonusStats.shieldRecharge, "shieldRecharge");
			AddInteger(ss, craftWeaponRule.BonusStats.shieldRechargeInGeoscape, "shieldRechargeInGeoscape");
			AddInteger(ss, craftWeaponRule.BonusStats.shieldBleedThrough, "shieldBleedThrough");
			AddInteger(ss, craftWeaponRule.BonusStats.soldiers, "soldiers");
			AddInteger(ss, craftWeaponRule.BonusStats.vehicles, "vehicles");
			AddInteger(ss, craftWeaponRule.BonusStats.maxItems, "maxItems", 0);
			AddDouble(ss, craftWeaponRule.BonusStats.maxStorageSpace, "maxStorageSpace", 0.0);
			EndHeading();
		}

		AddBoolean(ss, craftWeaponRule.IsWaterOnly, "underwaterOnly");

		if (craftWeaponRule.StandardReload > 0)
		{
			AddHeading("_calculatedValues");
			if (craftWeaponRule.UnifiedDamageFormula())
			{
				RuleItem damageItem = craftWeaponRule.ClipItem != null ? craftWeaponRule.ClipItem : craftWeaponRule.LauncherItem;

				var ss2 = new StringBuilder();
				ss2.Append(FormatInt(damageItem.DamageType.GetRandomDamage(craftWeaponRule.Damage, 1)));
				ss2.Append('-');
				ss2.Append(FormatInt(damageItem.DamageType.GetRandomDamage(craftWeaponRule.Damage, 2)));
				AddSingleString(ss, ss2.ToString(), "_damageRangeBasic", "", false);
			}
			else
			{
				// (damage) * (50-100% damage spread), not considering craft `powerBonus`
				var ss2 = new StringBuilder();
				ss2.Append(FormatInt(craftWeaponRule.Damage / 2));
				ss2.Append('-');
				ss2.Append(FormatInt(craftWeaponRule.Damage));
				AddSingleString(ss, ss2.ToString(), "_damageRangeBasic", "", false);

				// (damage / standard reload * 60) * (accuracy / 100) * (50-100% damage spread)
				int avgDPM = craftWeaponRule.Damage * craftWeaponRule.Accuracy * 60 * 3 / 4 / craftWeaponRule.StandardReload / 100;
				AddInteger(ss, avgDPM, "_averageDPM");

				// (damage * ammoMax) * (accuracy / 100) * (50-100% damage spread)
				int avgTotalDamage = craftWeaponRule.Damage * craftWeaponRule.AmmoMax * craftWeaponRule.Accuracy * 3 / 4 / 100;
				AddInteger(ss, avgTotalDamage, "_averageTotalDamage");
			}
			EndHeading();
		}

		if (_showDebug)
		{
			AddSection("{Modding section}", "You don't need this info as a player", _white, true);

			AddSection("{Naming}", "", _white);
			AddSingleString(ss, craftWeaponRule.Type, "type");
			AddSingleString(ss, craftWeaponRule.UfopediaType, "ufopediaType");
			AddBoolean(ss, craftWeaponRule.HidePediaInfo, "hidePediaInfo");

			AddSection("{Visuals}", "", _white);
			AddInteger(ss, (int)craftWeaponRule.ProjectileType, "projectileType", (int)CraftWeaponProjectileType.CWPT_CANNON_ROUND); // FIXME: CraftWeaponProjectileType ?
			AddInteger(ss, craftWeaponRule.ProjectileSpeed, "projectileSpeed");

			AddInteger(ss, craftWeaponRule.Sprite + 5, "_sprite (Dogfight)", 4);
			AddSpriteResourcePath(ss, mod, "INTICON.PCK", craftWeaponRule.Sprite + 5);
			AddInteger(ss, craftWeaponRule.Sprite + 48, "_sprite (Base)", 47);
			AddSpriteResourcePath(ss, mod, "BASEBITS.PCK", craftWeaponRule.Sprite + 48);

			AddSection("{Sounds}", "", _white);
			AddInteger(ss, craftWeaponRule.Sound, "sound", -1);
			List<int> tmpSoundVector = [];
			tmpSoundVector.Add(craftWeaponRule.Sound);
			AddSoundVectorResourcePaths(ss, mod, "GEO.CAT", tmpSoundVector);

			AddSection("{Mod info}", "", _white);
			{
				AddSingleString(ss, mod.GetModCreatingRule(craftWeaponRule).name, "createdByMod");
				AddSingleString(ss, mod.GetModLastUpdatingRule(craftWeaponRule).name, "updatedByMod");
				EndHeading();
			}
		}
	}

	/// <summary>Shows the "raw" data of the topic (Soldier).</summary>
	private void InitSoldierList()
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
		RuleSoldier soldierRule = mod.GetSoldier(_topicId);
		if (soldierRule == null)
			return;

		_filterOptions.Clear();
		_cbxRelatedStuff.Visible = false;

		var ss = new StringBuilder();

		AddBoolean(ss, soldierRule.AllowPiloting, "allowPiloting", true);

		AddRule(ss, soldierRule.DefaultArmor, "armor");
		AddRule(ss, soldierRule.SpecialWeapon, "specialWeapon");

		AddVectorOfStrings(ss, soldierRule.Requirements, "requires");
		AddVectorOfStrings(ss, mod.GetBaseFunctionNames(soldierRule.RequiresBuyBaseFunc), "requiresBuyBaseFunc");
		AddSingleString(ss, soldierRule.RequiresBuyCountry, "requiresBuyCountry");

		AddInteger(ss, soldierRule.BuyCost, "costBuy", 0, true);
		AddInteger(ss, soldierRule.MonthlyBuyLimit, "monthlyBuyLimit");

		int time = soldierRule.TransferTime;
		if (time == 0)
			time = _game.Mod.PersonnelTime;

		AddInteger(ss, time, "transferTime"); // not raw!

		int baseCost = soldierRule.GetSalaryCost(0);
		AddInteger(ss, baseCost, "costSalary", 0, true);
		AddInteger(ss, soldierRule.GetSalaryCost(1) - baseCost, "costSalarySquaddie", 0, true);
		AddInteger(ss, soldierRule.GetSalaryCost(2) - baseCost, "costSalarySergeant", 0, true);
		AddInteger(ss, soldierRule.GetSalaryCost(3) - baseCost, "costSalaryCaptain", 0, true);
		AddInteger(ss, soldierRule.GetSalaryCost(4) - baseCost, "costSalaryColonel", 0, true);
		AddInteger(ss, soldierRule.GetSalaryCost(5) - baseCost, "costSalaryCommander", 0, true);

		if (_showDebug)
		{
			AddSection("{Modding section}", "You don't need this info as a player", _white, true);

			AddSection("{Naming}", "", _white);
			AddSingleString(ss, soldierRule.Type, "type");
			AddBoolean(ss, soldierRule.ShowTypeInInventory, "showTypeInInventory");
			AddInteger(ss, soldierRule.Names.Count, "soldierNames*"); // size only
			AddInteger(ss, soldierRule.FemaleFrequency, "femaleFrequency", 50);
			AddInteger(ss, soldierRule.StatStrings.Count, "statStrings*"); // size only

			AddInteger(ss, soldierRule.RankStrings.Count, "rankStrings*"); // size only
			AddBoolean(ss, soldierRule.AllowPromotion, "allowPromotion", true);

			AddSingleString(ss, soldierRule.MonthlyBuyLimitMessage, "monthlyBuyLimitMessage");

			AddSection("{Stats}", "", _white);
			AddBoolean(ss, soldierRule.SpawnedSoldierTemplate.Length != 0, "spawnedSoldier*"); // just say if there are any or not

			AddInteger(ss, soldierRule.StandHeight, "standHeight");
			AddInteger(ss, soldierRule.KneelHeight, "kneelHeight");
			AddInteger(ss, soldierRule.FloatHeight, "floatHeight");
			AddInteger(ss, soldierRule.MoraleLossWhenKilled, "moraleLossWhenKilled", 100);
			AddInteger(ss, soldierRule.Value, "value", 20);

			AddUnitStatBonus(ss, soldierRule.MinStats, "minStats");
			AddUnitStatBonus(ss, soldierRule.MaxStats, "maxStats");
			AddUnitStatBonus(ss, soldierRule.StatCaps, "statCaps");
			AddUnitStatBonus(ss, soldierRule.TrainingStatCaps, "trainingStatCaps");

			AddUnitStatBonus(ss, soldierRule.DogfightExperience, "dogfightExperience");

			AddSection("{Other}", "", _white);
			AddInteger(ss, soldierRule.Group, "group");
			AddInteger(ss, soldierRule.Skills.Count, "skills*"); // size only

			AddSection("{Visuals}", "", _white);
			AddRule(ss, soldierRule.DefaultArmor, "armorForAvatar");
			AddInteger(ss, soldierRule.AvatarOffsetX, "avatarOffsetX", 67);
			AddInteger(ss, soldierRule.AvatarOffsetY, "avatarOffsetY", 48);
			AddInteger(ss, soldierRule.FlagOffset, "flagOffset");

			AddInteger(ss, soldierRule.RankSprite, "rankSprite", 42);
			AddSpriteResourcePath(ss, mod, "BASEBITS.PCK", soldierRule.RankSprite);
			AddInteger(ss, soldierRule.RankSpriteBattlescape, "rankBattleSprite", 20);
			AddSpriteResourcePath(ss, mod, "SMOKE.PCK", soldierRule.RankSpriteBattlescape);
			AddInteger(ss, soldierRule.RankSpriteTiny, "rankTinySprite", -1); // always show
			AddSpriteResourcePath(ss, mod, "TinyRanks", soldierRule.RankSpriteTiny);
			AddInteger(ss, soldierRule.SkillIconSprite, "skillIconSprite", 1);
			AddSpriteResourcePath(ss, mod, "SPICONS.DAT", soldierRule.SkillIconSprite);

			AddSection("{Sounds}", "", _white);
			AddVectorOfIntegers(ss, soldierRule.MaleDeathSounds, "deathMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.MaleDeathSounds);
			AddVectorOfIntegers(ss, soldierRule.FemaleDeathSounds, "deathFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.FemaleDeathSounds);
			AddVectorOfIntegers(ss, soldierRule.MaleSelectUnitSounds, "selectUnitMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.MaleSelectUnitSounds);
			AddVectorOfIntegers(ss, soldierRule.FemaleSelectUnitSounds, "selectUnitFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.FemaleSelectUnitSounds);
			AddVectorOfIntegers(ss, soldierRule.MaleStartMovingSounds, "startMovingMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.MaleStartMovingSounds);
			AddVectorOfIntegers(ss, soldierRule.FemaleStartMovingSounds, "startMovingFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.FemaleStartMovingSounds);
			AddVectorOfIntegers(ss, soldierRule.MaleSelectWeaponSounds, "selectWeaponMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.MaleSelectWeaponSounds);
			AddVectorOfIntegers(ss, soldierRule.FemaleSelectWeaponSounds, "selectWeaponFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.FemaleSelectWeaponSounds);
			AddVectorOfIntegers(ss, soldierRule.MaleAnnoyedSounds, "annoyedMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.MaleAnnoyedSounds);
			AddVectorOfIntegers(ss, soldierRule.FemaleAnnoyedSounds, "annoyedFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.FemaleAnnoyedSounds);
			AddVectorOfIntegers(ss, soldierRule.MalePanicSounds, "panicMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.MalePanicSounds);
			AddVectorOfIntegers(ss, soldierRule.FemalePanicSounds, "panicFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.FemalePanicSounds);
			AddVectorOfIntegers(ss, soldierRule.MaleBerserkSounds, "berserkMale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.MaleBerserkSounds);
			AddVectorOfIntegers(ss, soldierRule.FemaleBerserkSounds, "berserkFemale");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", soldierRule.FemaleBerserkSounds);

			AddSection("{Mod info}", "", _white);
			{
				AddSingleString(ss, mod.GetModCreatingRule(soldierRule).name, "createdByMod");
				AddSingleString(ss, mod.GetModLastUpdatingRule(soldierRule).name, "updatedByMod");
				EndHeading();
			}
		}
	}

	/// <summary>Shows the "raw" data of the topic (Unit).</summary>
	private void InitUnitList()
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
		Unit unitRule = mod.GetUnit(_topicId);
		if (unitRule == null)
			return;

		_filterOptions.Clear();
		_cbxRelatedStuff.Visible = false;

		var ss = new StringBuilder();

		AddSingleString(ss, unitRule.Race, "race");
		AddSingleString(ss, unitRule.Rank, "rank");

		AddRule(ss, unitRule.Armor, "armor");
		AddSpecialAbility(ss, (SpecialAbility)unitRule.SpecialAbility, "specab");
		AddBoolean(ss, unitRule.IsLivingWeapon, "livingWeapon");
		AddSingleString(ss, unitRule.MeleeWeapon, "meleeWeapon");
		AddSingleString(ss, unitRule.PsiWeapon, "psiWeapon", "ALIEN_PSI_WEAPON");

		AddBoolean(ss, unitRule.BuiltInWeapons.Count != 0, "_builtInWeaponSets"); // just say if there are any or not
		AddBoolean(ss, unitRule.WeightedBuiltInWeapons.Count != 0, "_weightedBuiltInWeaponSets"); // just say if there are any or not

		AddRule(ss, unitRule.SpawnUnit, "spawnUnit");

		bool capturableTotal = unitRule.Capturable && unitRule.Armor.CorpseBattlescape[0].IsRecoverable && unitRule.SpawnUnit == null;
		AddBoolean(ss, capturableTotal, "_capturable", true);

		AddBoolean(ss, unitRule.Capturable, "capturable", true);
		AddBoolean(ss, unitRule.CanSurrender(), "canSurrender", false);
		AddBoolean(ss, unitRule.AutoSurrender(), "autoSurrender", false);

		AddInteger(ss, unitRule.SpotterDuration, "spotter"); // not raw!
		AddInteger(ss, unitRule.SniperPercentage, "sniper");

		if (_showDebug)
		{
			AddSection("{Modding section}", "You don't need this info as a player", _white, true);

			AddSection("{Naming}", "", _white);
			AddSingleString(ss, unitRule.Type, "type");
			AddRule(ss, unitRule.LiveAlienGeoscape, "liveAlien");
			AddInteger(ss, unitRule.GetShowFullNameInAlienInventory(mod) ? 1 : 0, "showFullNameInAlienInventory"); // not raw!

			AddSection("{Stats}", "", _white);
			AddInteger(ss, unitRule.StandHeight, "standHeight");
			AddInteger(ss, unitRule.KneelHeight, "kneelHeight");
			AddInteger(ss, unitRule.FloatHeight, "floatHeight");
			AddInteger(ss, unitRule.MoraleLossWhenKilled, "moraleLossWhenKilled", 100);
			AddInteger(ss, unitRule.BerserkChance, "berserkChance", -1);
			AddInteger(ss, unitRule.EnergyRecovery, "energyRecovery", 30);

			AddUnitStatBonus(ss, unitRule.Stats, "stats");

			AddSection("{Flags}", "", _white);
			AddBoolean(ss, unitRule.CanPanic(), "canPanic", true);
			AddBoolean(ss, unitRule.CanBeMindControlled(), "canBeMindControlled", true);
			AddBoolean(ss, unitRule.IsIgnoredByAI, "ignoredByAI", false);
			AddBoolean(ss, unitRule.IsCosmetic, "cosmetic");
			AddBoolean(ss, unitRule.IsVIP, "vip");

			AddSection("{AI}", "", _white);
			AddInteger(ss, unitRule.Intelligence, "intelligence");
			AddInteger(ss, unitRule.Aggression, "aggression");
			AddInteger(ss, unitRule.PickUpWeaponsMoreActively, "pickUpWeaponsMoreActively", -1);
			AddBoolean(ss, unitRule.IsLeeroyJenkins, "isLeeroyJenkins");
			AddBoolean(ss, unitRule.AvoidsFire(), "avoidsFire", true); // default "_specab < SPECAB_BURNFLOOR"
			AddBoolean(ss, unitRule.WaitIfOutsideWeaponRange(), "waitIfOutsideWeaponRange");

			AddSection("{Debriefing}", "", _white);
			AddInteger(ss, unitRule.Value, "value", 0);
			AddBoolean(ss, unitRule.IsRecoverableAsCivilian, "civilianRecoveryType*"); // just say if there are any or not
			AddBoolean(ss, unitRule.IsRecoverableAsScientist, "_recoverScientist");
			AddBoolean(ss, unitRule.IsRecoverableAsEngineer, "_recoverEngineer");
			AddRule(ss, unitRule.CivilianRecoveryItemType, "_recoverItem");
			AddRule(ss, unitRule.CivilianRecoverySoldierType, "_recoverSoldier");
			AddSingleString(ss, unitRule.SpawnedPersonName, "spawnedPersonName");
			AddBoolean(ss, unitRule.SpawnedSoldierTemplate.Length != 0, "spawnedSoldier*"); // just say if there are any or not

			AddSection("{Sounds}", "", _white);
			AddInteger(ss, unitRule.MoveSound, "moveSound", -1);
			List<int> tmpSoundVector = [];
			tmpSoundVector.Add(unitRule.MoveSound);
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", tmpSoundVector);
			AddVectorOfIntegers(ss, unitRule.DeathSounds, "deathSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", unitRule.DeathSounds);
			AddVectorOfIntegers(ss, unitRule.SelectUnitSounds, "selectUnitSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", unitRule.SelectUnitSounds);
			AddVectorOfIntegers(ss, unitRule.StartMovingSounds, "startMovingSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", unitRule.StartMovingSounds);
			AddVectorOfIntegers(ss, unitRule.SelectWeaponSounds, "selectWeaponSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", unitRule.SelectWeaponSounds);
			AddVectorOfIntegers(ss, unitRule.AnnoyedSounds, "annoyedSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", unitRule.AnnoyedSounds);
			AddVectorOfIntegers(ss, unitRule.PanicSounds, "panicSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", unitRule.PanicSounds);
			AddVectorOfIntegers(ss, unitRule.BerserkSounds, "berserkSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", unitRule.BerserkSounds);
			AddVectorOfIntegers(ss, unitRule.AggroSounds, "aggroSound");
			AddSoundVectorResourcePaths(ss, mod, "BATTLE.CAT", unitRule.AggroSounds);

			AddSection("{Mod info}", "", _white);
			{
				AddSingleString(ss, mod.GetModCreatingRule(unitRule).name, "createdByMod");
				AddSingleString(ss, mod.GetModLastUpdatingRule(unitRule).name, "updatedByMod");
				EndHeading();
			}
		}
	}
}
#pragma warning restore CA1822
