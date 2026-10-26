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
// Ported from: src/Savegame/BattleUnit.h, src/Savegame/BattleUnit.cpp
#nullable disable

using static Acom.BattleActionType;
using static Acom.ItemDamageType;
using static Acom.SpecialAbility;
using static Acom.UnitBodyPart;
using static Acom.UnitFaction;
using static Acom.UnitSide;
using static Acom.UnitStatus;

namespace Acom;

// Health, stun, damage, action costs, accuracy, armor, wounds and turn recovery.
public partial class BattleUnit
{
	/// <summary>Gets or sets the unit's time units (setting clamps to 0..max).</summary>
	public int TimeUnits
	{
		get => _tu;
		set => _tu = Clamp(value, 0, _stats.tu);
	}

	/// <summary>Returns the soldier's amount of energy.</summary>
	public int Energy => _energy;

	/// <summary>Returns the soldier's amount of health.</summary>
	public int Health => _health;

	/// <summary>Returns the soldier's amount of mana.</summary>
	public int Mana => _mana;

	/// <summary>Returns the soldier's amount of morale.</summary>
	public int Morale => _morale;

	/// <summary>Get overkill damage to unit (damage over normal health).</summary>
	public int OverKillDamage => Math.Max(-_health - (int)(_stats.health * _armor.OverKill), 0);

	/// <summary>Do an amount of damage.</summary>
	/// <param name="relative">The relative position of which part of armor and/or bodypart is hit.</param>
	/// <param name="power">The amount of damage to inflict.</param>
	/// <param name="type">The type of damage being inflicted.</param>
	/// <param name="save">The battle.</param>
	/// <param name="attack">The attack (attacker, weapon, ammo, skill).</param>
	/// <param name="sideOverride">Forced armor side (used by environmental conditions only).</param>
	/// <param name="bodypartOverride">Forced body part (used by environmental conditions only).</param>
	/// <returns>damage done after adjustment</returns>
	public int Damage(Position relative, int power, RuleDamageType type, SavedBattleGame save, BattleActionAttack attack, UnitSide sideOverride = SIDE_MAX, UnitBodyPart bodypartOverride = BODYPART_MAX)
	{
		int damage = power;
		if (save.IsPreview)
		{
			return 0;
		}
		UnitSide side = SIDE_FRONT;
		UnitBodyPart bodypart = BODYPART_TORSO;

		_hitByAnything = true;
		if (_health <= 0)
		{
			return 0;
		}

		Rng.RandomState rand = Rng.GlobalRandomState.SubSequence();
		damage = ReduceByResistance(damage, type.ResistType);

		if (!type.IgnoreDirection)
		{
			CalculateHitLocation(relative, rand, ref side, ref bodypart);
		}

		int orgDamage = damage;
		int overKillMinimum = type.IgnoreOverKill ? 0 : -UnitStats.OverkillMultipler * _stats.health;

		{
			var args = new ScriptOutputArgs(damage, (int)bodypart, (int)side);
			object[] work = [this, attack.damage_item, attack.weapon_item, attack.attacker, save, attack.skill_rules, orgDamage, (int)type.ResistType, (int)attack.type];

			if (attack.damage_item != null)
			{
				ModScript.Execute(attack.damage_item.Rules.GetScriptEvents(ModScript.HitUnitAmmo), args, work);
			}

			ModScript.Execute(Armor.GetScript(ModScript.HitUnit), args, work);

			damage = args.GetFirst();
			bodypart = (UnitBodyPart)args.GetSecond();
			side = (UnitSide)args.GetThird();
			if ((uint)bodypart >= (uint)BODYPART_MAX) // C++ compares signed: a negative part is undefined behavior there (array index)
			{
				bodypart = default;
			}
			if (side >= SIDE_MAX) // Uint8 like C++
			{
				side = default;
			}
		}

		// side and bodypart overrides (used by environmental conditions only)
		if (sideOverride != SIDE_MAX)
		{
			side = sideOverride;
		}
		if (bodypartOverride != BODYPART_MAX)
		{
			bodypart = bodypartOverride;
		}

		RuleItem specialDamageTransform = attack.damage_item?.Rules;
		int specialDamageTransformChance = 0;

		if (specialDamageTransform != null
			&& !string.IsNullOrEmpty(specialDamageTransform.GetZombieUnit(this))
			&& Armor.GetZombiImmune() == false)
		{
			specialDamageTransformChance = specialDamageTransform.ZombieUnitChance;

			var conf = attack.weapon_item?.GetActionConf(attack.type);
			if (conf != null)
			{
				specialDamageTransformChance = UseIntNullable(conf.ammoZombieUnitChanceOverride, specialDamageTransformChance);
			}

			if (OriginalFaction == FACTION_HOSTILE)
			{
				if (attack.attacker == null || attack.attacker.OriginalFaction == FACTION_HOSTILE)
				{
					// (mind-controlled) chryssalid on snakeman action still not allowed
					specialDamageTransformChance = 0;
				}
			}
		}
		else
		{
			specialDamageTransform = null;
		}

		// update state of unit stats
		if (damage > 0)
		{
			// C++ ModScript::DamageUnit::Output (std::get<toHealth> ... std::get<toMana>)
			int toHealth = 0;
			int toArmor = 0;
			int toStun = 0;
			int toTime = 0;
			int toEnergy = 0;
			int toMorale = 0;
			int toWound = 0;
			int toTransform = 0;
			int toMana = 0;

			toTransform += specialDamageTransformChance;
			toArmor += type.GetArmorPreFinalDamage(damage);

			if (type.ArmorEffectiveness > 0.0f)
			{
				damage = (int)(damage - GetArmor(side) * type.ArmorEffectiveness);
			}

			if (damage > 0)
			{
				// stun level change
				toStun += type.GetStunFinalDamage(damage);

				// morale change
				toMorale += type.GetMoraleFinalDamage(damage);

				// time units change
				toTime += type.GetTimeFinalDamage(damage);

				// health change
				toHealth += type.GetHealthFinalDamage(damage);

				// mana change
				toMana += type.GetManaFinalDamage(damage);

				// energy change
				toEnergy += type.GetEnergyFinalDamage(damage);

				// fatal wounds change
				toWound += type.GetWoundFinalDamage(damage);

				// armor value change
				toArmor += type.GetArmorFinalDamage(damage);
			}

			{
				var args = new ScriptOutputArgs(toHealth, toArmor, toStun, toTime, toEnergy, toMorale, toWound, toTransform, toMana);
				object[] work = [this, attack.damage_item, attack.weapon_item, attack.attacker, save, attack.skill_rules, damage, orgDamage, (int)bodypart, (int)side, (int)type.ResistType, (int)attack.type];

				if (attack.damage_item != null)
				{
					ModScript.Execute(attack.damage_item.Rules.GetScriptEvents(ModScript.DamageUnitAmmo), args, work);
				}

				ModScript.Execute(Armor.GetScript(ModScript.DamageUnit), args, work);

				toHealth = args.GetInt(0);
				toArmor = args.GetInt(1);
				toStun = args.GetInt(2);
				toTime = args.GetInt(3);
				toEnergy = args.GetInt(4);
				toMorale = args.GetInt(5);
				toWound = args.GetInt(6);
				toTransform = args.GetInt(7);
				toMana = args.GetInt(8);
			}

			if (!_armor.GetPainImmune() || type.IgnorePainImmunity)
			{
				SetValueMax(ref _stunlevel, toStun, 0, UnitStats.StunMultipler * _stats.health);
			}

			MoraleChange(-ReduceByBravery(toMorale));

			SetValueMax(ref _tu, -toTime, 0, _stats.tu);

			SetValueMax(ref _health, -toHealth, Math.Min(overKillMinimum, _health), _stats.health); // `std::min` required because of script that could set `_health = -100`, if we do not have "overkill" `-100` become min value allowed by this line, if "overkill" then this line can go lower than this.

			SetValueMax(ref _mana, -toMana, 0, _stats.mana);

			SetValueMax(ref _energy, -toEnergy, 0, _stats.stamina);

			if (IsWoundable)
			{
				SetValueMax(ref _fatalWounds[(int)bodypart], toWound, 0, UnitStats.BaseStatLimit);
				MoraleChange(-toWound);
			}

			SetValueMax(ref _currentArmor[(int)side], -toArmor, 0, _maxArmor[(int)side]);

			SetFatalShotInfo(side, bodypart);

			damage = toHealth;
			specialDamageTransformChance = toTransform;
		}

		// special effects
		if (save.BattleState != null)
		{
			// C++ ModScript::DamageSpecialUnit::Output
			int arg_specialDamageTransformChance;
			bool arg_selfDestruct;
			int arg_selfDestructChance = 0;
			int arg_moraleLoss = 0;
			int arg_fire;
			int arg_attackerTurnsSinceSpotted;
			int arg_attackerTurnsLeftSpottedForSnipers;

			// chance to tranform
			arg_specialDamageTransformChance = specialDamageTransformChance;

			// morale loss based on final damage to health
			if (type.IgnoreNormalMoraleLose == false)
			{
				int bravery = ReduceByBravery(10);
				int modifier = Faction == FACTION_PLAYER ? save.GetFactionMoraleModifier(true) : 100;

				arg_moraleLoss = 100 * (damage * bravery / 10) / modifier;
			}

			// self destruction
			arg_selfDestruct = SpecialAbility == (int)SPECAB_EXPLODEONDEATH || SpecialAbility == (int)SPECAB_BURN_AND_EXPLODE;
			if (arg_selfDestruct && !IsOut && IsOutThresholdExceed)
			{
				if (type.IgnoreSelfDestruct == false)
				{
					arg_selfDestructChance = 100;
				}
			}

			// normal fire
			arg_fire = Fire;
			if (damage >= type.FireThreshold)
			{
				float resistance = Armor.GetDamageModifier(type.ResistType);
				if (resistance > 0.0)
				{
					int burnTime = rand.Generate(0, (int)(5.0f * resistance));
					if (arg_fire < burnTime)
					{
						arg_fire = burnTime; // catch fire and burn
					}
				}
			}
			// fire extinguisher
			if (arg_fire > 0)
			{
				if (attack.weapon_item != null && attack.weapon_item.Rules.IsFireExtinguisher)
				{
					// firearm, melee weapon, or even a grenade...
					arg_fire = 0;
				}
				else if (attack.damage_item != null && attack.damage_item.Rules.IsFireExtinguisher)
				{
					// bullet/ammo
					arg_fire = 0;
				}
			}

			// AI direct hit tracking
			arg_attackerTurnsSinceSpotted = 255;
			arg_attackerTurnsLeftSpottedForSnipers = 0;
			if (attack.attacker != null)
			{
				arg_attackerTurnsSinceSpotted = attack.attacker.GetTurnsSinceSpottedByFaction(Faction);
				arg_attackerTurnsLeftSpottedForSnipers = attack.attacker.GetTurnsLeftSpottedForSnipersByFaction(Faction);

				if (Faction != attack.attacker.Faction &&
					(attack.type == BA_AIMEDSHOT || attack.type == BA_SNAPSHOT || attack.type == BA_AUTOSHOT) &&
					attack.damage_item != null &&
					(relative == new Position(0, 0, 0) || (attack.damage_item.Rules.GetExplosionRadius(attack) == 0)))
				{
					AIModule ai = AIModule;
					ai?.SetWasHitBy(attack.attacker);

					arg_attackerTurnsSinceSpotted = 0;
					if (Mod.EXTENDED_SPOT_ON_HIT_FOR_SNIPING > 0)
					{
						// 0 = don't spot
						// 1 = spot only if the victim doesn't die or pass out
						// 2 = always spot
						if (Mod.EXTENDED_SPOT_ON_HIT_FOR_SNIPING > 1 || !IsOutThresholdExceed)
						{
							arg_attackerTurnsLeftSpottedForSnipers = Math.Max(arg_attackerTurnsLeftSpottedForSnipers, SpotterDuration);
						}
					}
				}
			}

			// script call
			{
				var args = new ScriptOutputArgs(specialDamageTransform != null ? 1 : 0, arg_specialDamageTransformChance, arg_selfDestruct ? 1 : 0, arg_selfDestructChance,
					arg_moraleLoss, arg_fire, arg_attackerTurnsSinceSpotted, arg_attackerTurnsLeftSpottedForSnipers);
				object[] work = [this, attack.damage_item, attack.weapon_item, attack.attacker, save, attack.skill_rules, damage, orgDamage, (int)bodypart, (int)side, (int)type.ResistType, (int)attack.type];

				if (attack.damage_item != null)
				{
					ModScript.Execute(attack.damage_item.Rules.GetScriptEvents(ModScript.DamageSpecialUnitAmmo), args, work);
				}

				ModScript.Execute(Armor.GetScript(ModScript.DamageSpecialUnit), args, work);

				arg_specialDamageTransformChance = args.GetInt(1);
				arg_selfDestructChance = args.GetInt(3);
				arg_moraleLoss = args.GetInt(4);
				arg_fire = args.GetInt(5);
				arg_attackerTurnsSinceSpotted = args.GetInt(6);
				arg_attackerTurnsLeftSpottedForSnipers = args.GetInt(7);
			}

			// update state
			MoraleChange(-arg_moraleLoss);
			Fire = arg_fire;

			// check if this unit turns others into zombies
			if (rand.Percent(arg_specialDamageTransformChance) && specialDamageTransform != null
				&& SpawnUnit == null)
			{
				var spawnName = specialDamageTransform.GetZombieUnit(this);
				var spawnType = save.Mod.GetUnit(spawnName);
				if (spawnType.Armor.Size <= Armor.Size)
				{
					UnitFaction faction = specialDamageTransform.ZombieUnitFaction;
					if (faction == FACTION_NONE)
					{
						if (attack.attacker != null)
						{
							faction = attack.attacker.Faction;
						}
						else
						{
							faction = FACTION_HOSTILE;
						}
					}

					// converts the victim to a zombie on death
					Respawn = true;
					SpawnUnitFaction = faction;
					SpawnUnit = spawnType;
				}
				else
				{
					Logger.Error($"Transforming armor type '{Armor.Type}' to unit type '{spawnName}' is not allowed because of bigger armor size");
				}
			}

			var selfDestructItem = GetSpecialWeapon(Armor.SelfDestructItem);
			if (rand.Percent(arg_selfDestructChance)
				&& !HasAlreadyExploded && selfDestructItem != null)
			{
				SetAlreadyExploded(true);
				Position p = Position.ToVoxel();
				save.BattleGame.StatePushNext(new ExplosionBState(save.BattleGame, new LastPositions(p), new BattleActionAttack { type = BA_SELF_DESTRUCT, attacker = this, weapon_item = selfDestructItem, damage_item = selfDestructItem }, null));
			}

			if (attack.attacker != null)
			{
				attack.attacker.SetTurnsSinceSpottedByFaction(Faction, arg_attackerTurnsSinceSpotted);
				attack.attacker.SetTurnsLeftSpottedForSnipersByFaction(Faction, arg_attackerTurnsLeftSpottedForSnipers);
			}
		}

		return damage;
	}

	/// <summary>
	/// Determines the armor side and body part hit from the relative position of the hit
	/// (the directional part of C++ <c>damage()</c>, split out for testing).
	/// </summary>
	/// <param name="relative">The relative position of which part of armor and/or bodypart is hit.</param>
	/// <param name="rand">The damage sub-sequence (picks the leg); the side uses the global generator like C++.</param>
	/// <param name="side">Armor side hit.</param>
	/// <param name="bodypart">Body part hit.</param>
	internal void CalculateHitLocation(Position relative, Rng.RandomState rand, ref UnitSide side, ref UnitBodyPart bodypart)
	{
		if (relative.x == 0 && relative.y == 0 && relative.z <= 0)
		{
			side = SIDE_UNDER;
		}
		else
		{
			int relativeDirection;
			int abs_x = Math.Abs((int)relative.x);
			int abs_y = Math.Abs((int)relative.y);
			if (abs_y > abs_x * 2)
				relativeDirection = 8 + 4 * (relative.y > 0 ? 1 : 0);
			else if (abs_x > abs_y * 2)
				relativeDirection = 10 + 4 * (relative.x < 0 ? 1 : 0);
			else
			{
				if (relative.x < 0)
				{
					if (relative.y > 0)
						relativeDirection = 13;
					else
						relativeDirection = 15;
				}
				else
				{
					if (relative.y > 0)
						relativeDirection = 11;
					else
						relativeDirection = 9;
				}
			}

			switch ((relativeDirection - _direction) % 8)
			{
				case 0: side = SIDE_FRONT; break;
				case 1: side = Rng.Generate(0, 2) < 2 ? SIDE_FRONT : SIDE_RIGHT; break;
				case 2: side = SIDE_RIGHT; break;
				case 3: side = Rng.Generate(0, 2) < 2 ? SIDE_REAR : SIDE_RIGHT; break;
				case 4: side = SIDE_REAR; break;
				case 5: side = Rng.Generate(0, 2) < 2 ? SIDE_REAR : SIDE_LEFT; break;
				case 6: side = SIDE_LEFT; break;
				case 7: side = Rng.Generate(0, 2) < 2 ? SIDE_FRONT : SIDE_LEFT; break;
			}
			if (relative.z >= Height)
			{
				bodypart = BODYPART_HEAD;
			}
			else if (relative.z > 4)
			{
				switch (side)
				{
					case SIDE_LEFT: bodypart = BODYPART_LEFTARM; break;
					case SIDE_RIGHT: bodypart = BODYPART_RIGHTARM; break;
					default: bodypart = BODYPART_TORSO; break;
				}
			}
			else
			{
				switch (side)
				{
					case SIDE_LEFT: bodypart = BODYPART_LEFTLEG; break;
					case SIDE_RIGHT: bodypart = BODYPART_RIGHTLEG; break;
					default:
						bodypart = (UnitBodyPart)rand.Generate((int)BODYPART_RIGHTLEG, (int)BODYPART_LEFTLEG);
						break;
				}
			}
		}
	}

	/// <summary>C++ <c>useIntNullable</c> (Mod/LoadYaml.h): -1 means "not set".</summary>
	private static int UseIntNullable(int value, int def) => value == -1 ? def : value;

	/// <summary>Do an amount of stun recovery.</summary>
	public void HealStun(int power)
	{
		_stunlevel -= power;
		if (_stunlevel < 0) _stunlevel = 0;
	}

	/// <summary>Gets the unit's stun level.</summary>
	public int Stunlevel => _stunlevel;

	/// <summary>Is the unit losing HP (due to negative health regeneration)?</summary>
	public bool HasNegativeHealthRegen
	{
		get
		{
			if (_health > 0)
			{
				int HPRecovery = 0;

				// apply soldier bonuses
				if (_geoscapeSoldier != null)
				{
					foreach (var bonusRule in _geoscapeSoldier.GetBonuses(null))
					{
						HPRecovery += bonusRule.GetHealthRecovery(this);
					}
				}

				return _armor.GetHealthRecovery(this, HPRecovery) < 0;
			}
			return false;
		}
	}

	/// <summary>
	/// Raises a unit's stun level sufficiently so that the unit is ready to become unconscious.
	/// Used when another unit falls on top of this unit.
	/// Zombified units first convert to their spawn unit.
	/// </summary>
	/// <param name="battle">Pointer to the battlescape game.</param>
	public void KnockOut(BattlescapeGame battle)
	{
		if (_spawnUnit != null)
		{
			Respawn = false;
			BattleUnit newUnit = battle.ConvertUnit(this);

			if (newUnit != null)
			{
				if (newUnit.SpawnUnit != null)
				{
					//scripts or rulesets could make new chryssalid from chryssalid, this means we could have infinite loop there
					//setting null will break it
					newUnit.ClearSpawnUnit();
				}

				newUnit.KnockOut(battle);
			}
		}
		else
		{
			_stunlevel = Math.Max(_health, 1);
		}
	}

	/// <summary>Get the number of time units a certain action takes.</summary>
	public RuleItemUseCost GetActionTUs(BattleActionType actionType, BattleItem item)
	{
		if (item == null)
		{
			return new RuleItemUseCost(0);
		}
		return GetActionTUs(actionType, item.Rules);
	}

	/// <summary>Get the number of time units a certain skill action takes.</summary>
	public RuleItemUseCost GetActionTUs(BattleActionType actionType, RuleSkill skillRules)
	{
		if (skillRules == null)
		{
			return new RuleItemUseCost(0);
		}
		RuleItemUseCost cost = new RuleItemUseCost(skillRules.Cost);
		ApplyPercentages(cost, skillRules.Flat);

		return cost;
	}

	/// <summary>Get the number of time units a certain action takes.</summary>
	public RuleItemUseCost GetActionTUs(BattleActionType actionType, RuleItem item)
	{
		RuleItemUseCost cost = new RuleItemUseCost();
		if (item != null)
		{
			RuleItemUseFlat flat = item.FlatUse;
			switch (actionType)
			{
				case BA_PRIME:
					flat = item.FlatPrime;
					cost = new RuleItemUseCost(item.CostPrime);
					break;
				case BA_UNPRIME:
					flat = item.FlatUnprime;
					cost = new RuleItemUseCost(item.CostUnprime);
					break;
				case BA_THROW:
					flat = item.FlatThrow;
					cost = new RuleItemUseCost(item.CostThrow);
					break;
				case BA_AUTOSHOT:
					flat = item.FlatAuto;
					cost = new RuleItemUseCost(item.CostAuto);
					break;
				case BA_SNAPSHOT:
					flat = item.FlatSnap;
					cost = new RuleItemUseCost(item.CostSnap);
					break;
				case BA_HIT:
					flat = item.FlatMelee;
					cost = new RuleItemUseCost(item.CostMelee);
					break;
				case BA_LAUNCH:
				case BA_AIMEDSHOT:
					flat = item.FlatAimed;
					cost = new RuleItemUseCost(item.CostAimed);
					break;
				case BA_USE:
					cost = new RuleItemUseCost(item.CostUse);
					break;
				case BA_MINDCONTROL:
					cost = new RuleItemUseCost(item.CostMind);
					break;
				case BA_PANIC:
					cost = new RuleItemUseCost(item.CostPanic);
					break;
				default:
					break;
			}

			ApplyPercentages(cost, flat);
		}
		return cost;
	}

	/// <summary>Applies percentual and/or flat adjustments to the use costs.</summary>
	private void ApplyPercentages(RuleItemUseCost cost, RuleItemUseFlat flat)
	{
		{
			// if it's a percentage, apply it to unit TUs
			if (!flat.Time && cost.Time != 0)
			{
				cost.Time = (short)Math.Max(1, (int)MathF.Floor(BaseStats.tu * cost.Time / 100.0f));
			}
			// if it's a percentage, apply it to unit Energy
			if (!flat.Energy && cost.Energy != 0)
			{
				cost.Energy = (short)Math.Max(1, (int)MathF.Floor(BaseStats.stamina * cost.Energy / 100.0f));
			}
			// if it's a percentage, apply it to unit Morale
			if (!flat.Morale && cost.Morale != 0)
			{
				cost.Morale = (short)Math.Max(1, (int)MathF.Floor((110 - BaseStats.bravery) * cost.Morale / 100.0f));
			}
			// if it's a percentage, apply it to unit Health
			if (!flat.Health && cost.Health != 0)
			{
				cost.Health = (short)Math.Max(1, (int)MathF.Floor(BaseStats.health * cost.Health / 100.0f));
			}
			// if it's a percentage, apply it to unit Health
			if (!flat.Stun && cost.Stun != 0)
			{
				cost.Stun = (short)Math.Max(1, (int)MathF.Floor(BaseStats.health * cost.Stun / 100.0f));
			}
			// if it's a percentage, apply it to unit Mana
			if (!flat.Mana && cost.Mana != 0)
			{
				cost.Mana = (short)Math.Max(1, (int)MathF.Floor(BaseStats.mana * cost.Mana / 100.0f));
			}
		}
	}

	/// <summary>Spend time units if it can. Return false if it can't.</summary>
	public bool SpendTimeUnits(int tu)
	{
		if (tu <= _tu)
		{
			_tu -= tu;
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>Spend energy if it can. Return false if it can't.</summary>
	public bool SpendEnergy(int energy)
	{
		if (energy <= _energy)
		{
			_energy -= energy;
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>Spend resources cost without checking.</summary>
	public void SpendCost(RuleItemUseCost cost)
	{
		_tu -= cost.Time;
		_energy -= cost.Energy;
		_morale -= cost.Morale;
		_health -= cost.Health;
		_stunlevel += cost.Stun;
		_mana -= cost.Mana;
	}

	/// <summary>Clear number of time units.</summary>
	public void ClearTimeUnits()
	{
		_tu = 0;
	}

	/// <summary>Reset time units and energy.</summary>
	public void ResetTimeUnitsAndEnergy()
	{
		_tu = _stats.tu;
		_energy = _stats.stamina;
	}

	/// <summary>Get accuracy of different types of psi attack.</summary>
	/// <param name="attack">Psi attack (type and Psi-Amp).</param>
	/// <returns>Attack bonus.</returns>
	public static int GetPsiAccuracy(BattleActionAttack attack)
	{
		var actionType = attack.type;
		var item = attack.weapon_item;

		int psiAcc = 0;
		if (actionType == BA_MINDCONTROL)
		{
			psiAcc = item.Rules.AccuracyMind;
		}
		else if (actionType == BA_PANIC)
		{
			psiAcc = item.Rules.AccuracyPanic;
		}
		else if (actionType == BA_USE)
		{
			psiAcc = item.Rules.AccuracyUse;
		}

		psiAcc += item.Rules.GetAccuracyMultiplier(attack);

		return psiAcc;
	}

	/// <summary>
	/// Calculate firing accuracy.
	/// Formula = accuracyStat * weaponAccuracy * kneeling bonus(1.15) * one-handPenalty(0.8) * woundsPenalty(% health) * critWoundsPenalty (-10%/wound)
	/// </summary>
	public static int GetFiringAccuracy(BattleActionAttack attack, Mod mod)
	{
		var actionType = attack.type;
		var item = attack.weapon_item;
		int modifier = attack.attacker.GetAccuracyModifier(item);
		int result = 0;
		bool kneeled = attack.attacker._kneeled;

		if (actionType == BA_SNAPSHOT)
		{
			result = item.Rules.GetAccuracyMultiplier(attack) * item.Rules.AccuracySnap / 100;
		}
		else if (actionType == BA_AIMEDSHOT || actionType == BA_LAUNCH)
		{
			result = item.Rules.GetAccuracyMultiplier(attack) * item.Rules.AccuracyAimed / 100;
		}
		else if (actionType == BA_AUTOSHOT)
		{
			result = item.Rules.GetAccuracyMultiplier(attack) * item.Rules.AccuracyAuto / 100;
		}
		else if (actionType == BA_HIT)
		{
			kneeled = false;
			result = item.Rules.GetMeleeMultiplier(attack) * item.Rules.AccuracyMelee / 100;
		}
		else if (actionType == BA_THROW)
		{
			kneeled = false;
			result = item.Rules.GetThrowMultiplier(attack) * item.Rules.AccuracyThrow / 100;
		}
		else if (actionType == BA_CQB)
		{
			kneeled = false;
			result = item.Rules.GetCloseQuartersMultiplier(attack) * item.Rules.GetAccuracyCloseQuarters(mod) / 100;
		}

		if (kneeled)
		{
			result = result * item.Rules.GetKneelBonus(mod) / 100;
		}

		if (item.Rules.IsTwoHanded && actionType != BA_THROW)
		{
			// two handed weapon, means one hand should be empty
			if (attack.attacker.RightHandWeapon != null && attack.attacker.LeftHandWeapon != null)
			{
				result = result * item.Rules.GetOneHandedPenalty(mod) / 100;
			}
			else if (item.Rules.IsSpecialUsingEmptyHand)
			{
				// for special weapons that use an empty hand... already one hand with an item is enough for the penalty to apply
				if (attack.attacker.RightHandWeapon != null || attack.attacker.LeftHandWeapon != null)
				{
					result = result * item.Rules.GetOneHandedPenalty(mod) / 100;
				}
			}
		}

		return result * modifier / 100;
	}

	/// <summary>
	/// To calculate firing accuracy. Takes health and fatal wounds into account.
	/// Formula = accuracyStat * woundsPenalty(% health) * critWoundsPenalty (-10%/wound)
	/// </summary>
	/// <param name="item">the item we are shooting right now.</param>
	/// <returns>modifier</returns>
	public int GetAccuracyModifier(BattleItem item = null)
	{
		int wounds = _fatalWounds[(int)BODYPART_HEAD];

		if (item != null)
		{
			if (item.Rules.IsTwoHanded)
			{
				wounds += _fatalWounds[(int)BODYPART_RIGHTARM] + _fatalWounds[(int)BODYPART_LEFTARM];
			}
			else
			{
				var slot = item.Slot;
				if (slot != null)
				{
					// why broken hands should affect your aim if you shoot not using them?
					if (slot.IsRightHand)
					{
						wounds += _fatalWounds[(int)BODYPART_RIGHTARM];
					}
					if (slot.IsLeftHand)
					{
						wounds += _fatalWounds[(int)BODYPART_LEFTARM];
					}
				}
			}
		}
		return Math.Max(10, 25 * _health / BaseStats.health + 75 + -10 * wounds);
	}

	/// <summary>Set the armor value of a certain armor side.</summary>
	public void SetArmor(int armor, UnitSide side)
	{
		_currentArmor[(int)side] = Clamp(armor, 0, _maxArmor[(int)side]);
	}

	/// <summary>Get the armor value of a certain armor side.</summary>
	public int GetArmor(UnitSide side) => _currentArmor[(int)side];

	/// <summary>Set the max armor value of a certain armor side.</summary>
	public void SetMaxArmor(int armor, UnitSide side)
	{
		_maxArmor[(int)side] = Clamp(armor, 0, UnitStats.BaseStatLimit);
		_currentArmor[(int)side] = Clamp(_currentArmor[(int)side], 0, _maxArmor[(int)side]);
	}

	/// <summary>Get the max armor value of a certain armor side.</summary>
	public int GetMaxArmor(UnitSide side) => _maxArmor[(int)side];

	/// <summary>Get total amount of fatal wounds this unit has.</summary>
	public int FatalWounds
	{
		get
		{
			int sum = 0;
			for (int i = 0; i < (int)BODYPART_MAX; ++i)
				sum += _fatalWounds[i];
			return sum;
		}
	}

	/// <summary>Little formula to calculate reaction score: (Reactions Stat) x (Current Time Units / Max TUs).</summary>
	public double ReactionScore
	{
		get
		{
			double score = ((double)BaseStats.reactions * (double)TimeUnits) / (double)BaseStats.tu;
			return score;
		}
	}

	/// <summary>Helper function preparing Time Units recovery at beginning of turn.</summary>
	/// <param name="tu">New time units for this turn.</param>
	private void PrepareTimeUnits(int tu)
	{
		if (!IsOut)
		{
			// Add to previous turn TU, if regen is less than normal unit need couple of turns to regen full bar
			SetValueMax(ref _tu, tu, 0, BaseStats.tu);

			// Apply reductions, if new TU == 0 then it could make not spend TU decay
			float encumbrance = (float)BaseStats.strength / (float)GetCarriedWeight();
			if (encumbrance < 1)
			{
				_tu = (int)(encumbrance * _tu);
			}
			// Each fatal wound to the left or right leg reduces the soldier's TUs by 10%.
			_tu -= (_tu * ((_fatalWounds[(int)BODYPART_LEFTLEG] + _fatalWounds[(int)BODYPART_RIGHTLEG]) * 10)) / 100;

			SetValueMax(ref _tu, 0, 0, BaseStats.tu);
		}
	}

	/// <summary>Helper function preparing Energy recovery at beginning of turn.</summary>
	/// <param name="energy">Energy grain this turn.</param>
	private void PrepareEnergy(int energy)
	{
		if (!IsOut)
		{
			// Each fatal wound to the body reduces the soldier's energy recovery by 10%.
			energy -= (_energy * (_fatalWounds[(int)BODYPART_TORSO] * 10)) / 100;

			SetValueMax(ref _energy, energy, 0, BaseStats.stamina);
		}
	}

	/// <summary>Helper function preparing Health recovery at beginning of turn.</summary>
	/// <param name="health">Health grain this turn.</param>
	private void PrepareHealth(int health)
	{
		// suffer from fatal wounds
		health -= FatalWounds;

		// suffer from fire
		if (!_hitByFire && _fire > 0)
		{
			health -= ReduceByResistance(Rng.Generate(Mod.FIRE_DAMAGE_RANGE[0], Mod.FIRE_DAMAGE_RANGE[1]), DT_IN);
			_fire--;
		}

		SetValueMax(ref _health, health, -UnitStats.OverkillMultipler * _stats.health, _stats.health);

		// if unit is dead, AI state should be gone
		if (_health <= 0 && _currentAIState != null)
		{
			_currentAIState = null;
		}
	}

	/// <summary>Helper function preparing Mana recovery at beginning of turn.</summary>
	/// <param name="mana">Mana gain this turn.</param>
	private void PrepareMana(int mana)
	{
		if (!IsOut)
		{
			SetValueMax(ref _mana, mana, 0, BaseStats.mana);
		}
	}

	/// <summary>Helper function preparing Stun recovery at beginning of turn.</summary>
	/// <param name="stun">Stun damage reduction this turn.</param>
	private void PrepareStun(int stun)
	{
		if (IsSmallUnit || !IsOut)
		{
			HealStun(stun);
		}
	}

	/// <summary>Helper function preparing Morale recovery at beginning of turn.</summary>
	/// <param name="morale">Morale grain this turn.</param>
	private void PrepareMorale(int morale)
	{
		if (!IsOut)
		{
			MoraleChange(morale);
			int chance = 100 - (2 * Morale);
			if (Rng.Percent(chance))
			{
				int berserkChance = _unitRules != null ? _unitRules.BerserkChance : -1; // -1 represents true 1/3 (33.33333...%)
				bool berserk;
				if (berserkChance == -1)
				{
					berserk = Rng.Generate(0, 2) == 0; // vanilla OG
				}
				else
				{
					berserk = Rng.Percent(berserkChance);
				}
				_status = berserk ? STATUS_BERSERK : STATUS_PANICKING; // 33% chance of berserk, panic can mean freeze or flee, but that is determined later
				_wantsToSurrender = true;
			}
			else
			{
				// successfully avoided panic
				// increase bravery experience counter
				if (chance > 1)
					AddBraveryExp();
			}
		}
		else
		{
			// knocked out units are willing to surrender if they wake up
			if (_status == STATUS_UNCONSCIOUS)
				_wantsToSurrender = true;
		}
	}

	/// <summary>Prepare for a new turn.</summary>
	/// <param name="fullProcess">False for a transition between stages (no damage or panic).</param>
	public void PrepareNewTurn(bool fullProcess = true)
	{
		if (IsIgnored)
		{
			return;
		}

		_isSurrendering = false;
		_unitsSpottedThisTurn.Clear();
		_meleeAttackedBy.Clear();

		_hitByFire = false;
		_dontReselect = false;
		_aiMedikitUsed = false;
		_motionPoints = 0;

		if (!IsOut)
		{
			IncTurnsSinceStunned();
		}

		// don't give it back its TUs or anything this round
		// because it's no longer a unit of the team getting TUs back
		if (_faction != _originalFaction)
		{
			_faction = _originalFaction;
			if (_faction == FACTION_PLAYER && _currentAIState != null)
			{
				_currentAIState = null;
			}
			return;
		}
		else
		{
			UpdateUnitStats(true, false);
		}

		// transition between stages, don't do damage or panic
		if (!fullProcess)
		{
			if (_kneeled)
			{
				// stand up if kneeling
				_kneeled = false;
			}
			return;
		}

		UpdateUnitStats(false, true);
	}

	/// <summary>Update stats of unit.</summary>
	/// <param name="tuAndEnergy">Recover time units and energy.</param>
	/// <param name="rest">Recover health, mana, morale and stun.</param>
	public void UpdateUnitStats(bool tuAndEnergy, bool rest)
	{
		// snapshot of current stats
		int TURecovery = 0;
		int ENRecovery = 0;

		if (tuAndEnergy)
		{
			// apply soldier bonuses
			if (_geoscapeSoldier != null)
			{
				foreach (var bonusRule in _geoscapeSoldier.GetBonuses(null))
				{
					TURecovery += bonusRule.GetTimeRecovery(this);
					ENRecovery += bonusRule.GetEnergyRecovery(this);
				}
			}

			//unit update will be done after other stats are calculated and updated
		}

		if (rest)
		{
			// snapshot of current stats
			int HPRecovery = 0;
			int MNRecovery = 0;
			int MRRecovery = 0;
			int STRecovery = 0;

			// apply soldier bonuses
			if (_geoscapeSoldier != null)
			{
				foreach (var bonusRule in _geoscapeSoldier.GetBonuses(null))
				{
					HPRecovery += bonusRule.GetHealthRecovery(this);
					MNRecovery += bonusRule.GetManaRecovery(this);
					MRRecovery += bonusRule.GetMoraleRecovery(this);
					STRecovery += bonusRule.GetStunRegeneration(this);
				}
			}

			// update stats
			PrepareHealth(_armor.GetHealthRecovery(this, HPRecovery));
			PrepareMana(_armor.GetManaRecovery(this, MNRecovery));
			PrepareMorale(_armor.GetMoraleRecovery(this, MRRecovery));
			PrepareStun(_armor.GetStunRegeneration(this, STRecovery));
		}

		if (tuAndEnergy)
		{
			// update stats
			PrepareTimeUnits(_armor.GetTimeRecovery(this, TURecovery));
			PrepareEnergy(_armor.GetEnergyRecovery(this, ENRecovery));
		}
	}

	/// <summary>Morale change with bounds check.</summary>
	/// <param name="change">can be positive or negative</param>
	public void MoraleChange(int change)
	{
		if (!IsFearable) return;

		_morale += change;
		if (_morale > 100)
			_morale = 100;
		if (_morale < 0)
			_morale = 0;
	}

	/// <summary>Get reduced morale change value by bravery.</summary>
	public int ReduceByBravery(int moraleChange) => (110 - _stats.bravery) * moraleChange / 100;

	/// <summary>Calculate power reduction by resistances.</summary>
	public int ReduceByResistance(int power, ItemDamageType resistType) => (int)MathF.Floor(power * _armor.GetDamageModifier(resistType));

	/// <summary>Gets or sets the amount of turns this unit is on fire. 0 = no fire.</summary>
	public int Fire
	{
		get => _fire;
		set
		{
			if (_specab != SPECAB_BURNFLOOR && _specab != SPECAB_BURN_AND_EXPLODE)
				_fire = value;
		}
	}

	/// <summary>Get the amount of fatal wound for a body part (in the range 0-5).</summary>
	public int GetFatalWound(UnitBodyPart part)
	{
		if (part < 0 || part >= BODYPART_MAX)
			return 0;
		return _fatalWounds[(int)part];
	}

	/// <summary>Set fatal wound amount of a body part (in the range 0-5).</summary>
	public void SetFatalWound(int wound, UnitBodyPart part)
	{
		if (part < 0 || part >= BODYPART_MAX)
			return;
		_fatalWounds[(int)part] = Clamp(wound, 0, UnitStats.BaseStatLimit);
	}

	/// <summary>Heal a fatal wound of the soldier</summary>
	/// <param name="part">the body part to heal</param>
	/// <param name="woundAmount">the amount of fatal wound healed</param>
	/// <param name="healthAmount">The amount of health to add to soldier health</param>
	public void Heal(UnitBodyPart part, int woundAmount, int healthAmount)
	{
		if (part < 0 || part >= BODYPART_MAX || _fatalWounds[(int)part] == 0)
		{
			return;
		}

		SetValueMax(ref _fatalWounds[(int)part], -woundAmount, 0, UnitStats.BaseStatLimit);
		SetValueMax(ref _health, healthAmount, Math.Min(_health, 1), BaseStats.health); //Hippocratic Oath: First do no harm
	}

	/// <summary>Restore soldier morale</summary>
	/// <param name="moraleAmount">additional morale boost.</param>
	/// <param name="painKillersStrength">how much of damage convert to morale.</param>
	public void PainKillers(int moraleAmount, float painKillersStrength)
	{
		int lostHealth = (int)((BaseStats.health - _health) * painKillersStrength);
		if (lostHealth > _moraleRestored)
		{
			_morale = Math.Min(100, (lostHealth - _moraleRestored + _morale));
			_moraleRestored = lostHealth;
		}
		MoraleChange(moraleAmount);
	}

	/// <summary>Restore soldier energy and reduce stun level, can restore mana too</summary>
	/// <param name="energy">The amount of energy to add</param>
	/// <param name="stun">The amount of stun level to reduce</param>
	/// <param name="mana">The amount of mana to add</param>
	public void Stimulant(int energy, int stun, int mana)
	{
		_energy += energy;
		if (_energy > BaseStats.stamina)
			_energy = BaseStats.stamina;
		HealStun(stun);
		SetValueMax(ref _mana, mana, 0, BaseStats.mana);
	}

	/// <summary>Get whether the unit is affected by fatal wounds. Normally only soldiers are affected by fatal wounds.</summary>
	public bool IsWoundable => !_armor.GetBleedImmune(!(_type == "SOLDIER" || (Options.alienBleeding && _originalFaction != FACTION_PLAYER)));

	/// <summary>Get whether the unit is affected by morale loss. Normally only small units are affected by morale loss.</summary>
	public bool IsFearable => !_armor.GetFearImmune();

	/// <summary>Set health to 0 - used when getting killed unconscious.</summary>
	public void Kill()
	{
		_health = 0;
	}

	/// <summary>Set health to 0 and set status dead - used when getting zombified.</summary>
	public void InstaKill()
	{
		_health = 0;
		_status = STATUS_DEAD;
		_turnsSinceStunned = 0;
	}

	/// <summary>Did this unit already take fire damage this turn? (used to avoid damaging large units multiple times.)</summary>
	public bool TookFireDamage() => _hitByFire;

	/// <summary>Toggle the state of the fire damage tracking boolean.</summary>
	public void ToggleFireDamage()
	{
		_hitByFire = !_hitByFire;
	}

	/// <summary>Set fire damage from environment.</summary>
	public void SetEnviFire(int damage)
	{
		if (_fireMaxHit < damage) _fireMaxHit = damage;
	}

	/// <summary>Set smoke damage from environment.</summary>
	public void SetEnviSmoke(int damage)
	{
		if (_smokeMaxHit < damage) _smokeMaxHit = damage;
	}

	/// <summary>Calculate smoke and fire damage from environment.</summary>
	public void CalculateEnviDamage(Mod mod, SavedBattleGame save)
	{
		if (_fireMaxHit != 0)
		{
			_hitByFire = true;
			Damage(new Position(0, 0, 0), _fireMaxHit, mod.GetDamageType(DT_IN), save, new BattleActionAttack());
			// try to set the unit on fire.
			if (Rng.Percent((int)(40 * Armor.GetDamageModifier(DT_IN))))
			{
				int burnTime = Rng.Generate(0, (int)(5.0f * Armor.GetDamageModifier(DT_IN)));
				if (Fire < burnTime)
				{
					Fire = burnTime;
				}
			}
		}

		if (_smokeMaxHit != 0)
		{
			Damage(new Position(0, 0, 0), _smokeMaxHit, mod.GetDamageType(DT_SMOKE), save, new BattleActionAttack());
		}

		_fireMaxHit = 0;
		_smokeMaxHit = 0;
	}

	/// <summary>
	/// Elevates the unit to grand galactic inquisitor status,
	/// meaning they will NOT take part in the current battle.
	/// </summary>
	public void GoToTimeOut()
	{
		_status = STATUS_IGNORE_ME;

		// 1. Problem:
		// Take 2 rookies to an alien colony, leave 1 behind, and teleport the other to the exit and abort.
		// Then let the aliens kill the rookie in the second stage.
		// The mission will be a success, alien colony destroyed and everything recovered! (which is unquestionably wrong)
		// ------------
		// 2. Solution:
		// Proper solution would be to fix this in the Debriefing, but (as far as I can say)
		// that would require a lot of changes, Debriefing simply is not prepared for this scenario.
		// ------------
		// 3. Workaround:
		// Knock out all the player units left behind in the earlier stages
		// so that they don't count as survivors when all player units in the later stage are killed.
		if (_originalFaction == FACTION_PLAYER)
		{
			_stunlevel = Math.Max(_health, 1);
		}
	}

	/// <summary>Recovers a unit's TUs and energy, taking a number of factors into consideration.</summary>
	public void RecoverTimeUnits()
	{
		UpdateUnitStats(true, false);
	}

	/// <summary>Was this unit just hit?</summary>
	public bool HitState => _hitByAnything;

	/// <summary>Reset the unit hit state.</summary>
	public void ResetHitState()
	{
		_hitByAnything = false;
	}

	/// <summary>Was this unit melee attacked by a given attacker this turn (both hit and miss count)?</summary>
	public bool WasMeleeAttackedBy(int attackerId) => _meleeAttackedBy.Contains(attackerId);

	/// <summary>Set the "melee attacked by" flag.</summary>
	public void SetMeleeAttackedBy(int attackerId)
	{
		if (!WasMeleeAttackedBy(attackerId))
		{
			_meleeAttackedBy.Add(attackerId);
		}
	}

	/// <summary>Set information on the unit's fatal blow.</summary>
	/// <param name="side">unit's side that was shot.</param>
	/// <param name="bodypart">unit's body part that was shot.</param>
	public void SetFatalShotInfo(UnitSide side, UnitBodyPart bodypart)
	{
		_fatalShotSide = side;
		_fatalShotBodyPart = bodypart;
	}

	/// <summary>Get information on the unit's fatal shot's side.</summary>
	public UnitSide FatalShotSide => _fatalShotSide;

	/// <summary>Get information on the unit's fatal shot's body part.</summary>
	public UnitBodyPart FatalShotBodyPart => _fatalShotBodyPart;
}
