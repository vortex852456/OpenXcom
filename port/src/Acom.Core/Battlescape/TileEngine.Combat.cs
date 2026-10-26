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
// Ported from: src/Battlescape/TileEngine.cpp (reaction fire, hits, explosions, psi/melee/medikit/skill actions)
// Port status: partial (missing: script hooks)
#nullable disable

using static Acom.BattleActionType;
using static Acom.BattleMediKitAction;
using static Acom.BattleType;
using static Acom.ExperienceTrainingMode;
using static Acom.HitLogEntryType;
using static Acom.LightLayers;
using static Acom.TilePart;
using static Acom.UnitFaction;
using static Acom.UnitStatus;
using static Acom.VoxelType;

namespace Acom;

public partial class TileEngine
{
	/// <summary>
	/// Checks if a sniper from the opposing faction sees this unit. The unit with the highest reaction score will be compared with the current unit's reaction score.
	/// If it's higher, a shot is fired when enough time units, a weapon and ammo are available.
	/// </summary>
	/// <param name="unit">The unit to check reaction fire upon.</param>
	/// <param name="originalAction">The action that triggered the reaction.</param>
	/// <returns>True if reaction fire took place.</returns>
	public bool CheckReactionFire(BattleUnit unit, BattleAction originalAction)
	{
		if (_save.IsPreview)
		{
			return false;
		}

		// reaction fire only triggered when the actioning unit is of the currently playing side, and is still on the map (alive)
		if (unit.Faction != _save.Side || unit.Tile == null)
		{
			return false;
		}

		List<ReactionScore> spotters = GetSpottingUnits(unit);
		bool result = false;

		// not mind controlled, or controlled by the player
		if (unit.Faction == unit.OriginalFaction
			|| unit.Faction != FACTION_HOSTILE)
		{
			// get the first man up to bat.
			ReactionScore reactor = GetReactor(spotters, unit);
			// start iterating through the possible reactors until the current unit is the one with the highest score.
			while (reactor != null)
			{
				if (reactor.count > 10 || !TryReaction(reactor, unit, originalAction))
				{
					for (int i = 0; i < spotters.Count; ++i)
					{
						if (spotters[i] == reactor)
						{
							spotters.RemoveAt(i);
							reactor = null;
							break;
						}
					}
					// can't make a reaction snapshot for whatever reason, boot this guy from the vector.
					// avoid setting result to true, but carry on, just cause one unit can't react doesn't mean the rest of the units in the vector (if any) can't
					reactor = GetReactor(spotters, unit);
					continue;
				}
				// nice shot, kid. don't get cocky.
				result = true;
				reactor.reactionScore -= reactor.reactionReduction;
				reactor.count += 1;
				reactor = GetReactor(spotters, unit);
			}
		}
		return result;
	}

	/// <summary>Creates a vector of units that can spot this unit.</summary>
	/// <param name="unit">The unit to check for spotters of.</param>
	/// <returns>A vector of units that can see this unit.</returns>
	private List<ReactionScore> GetSpottingUnits(BattleUnit unit)
	{
		var spotters = new List<ReactionScore>();
		Tile tile = unit.Tile;
		double threshold = unit.ReactionScore; // C++ stores the double score in an int
		threshold = (int)threshold;
		// no reaction on civilian turn.
		if (_save.Side != FACTION_NEUTRAL)
		{
			foreach (BattleUnit bu in _save.Units)
			{
				// not dead/unconscious
				if (!bu.IsOut &&
					// not dying or not about to pass out
					!bu.IsOutThresholdExceed &&
					// have any chances for reacting
					bu.ReactionScore >= threshold &&
					// not a friend
					bu.Faction != _save.Side &&
					// not a civilian, or a civilian shooting at bad non-ignored guys
					(bu.Faction != FACTION_NEUTRAL || (unit.Faction == FACTION_HOSTILE && !unit.IsIgnoredByAI)) &&
					// closer than 20 tiles
					Position.Distance2dSq(unit.Position, bu.Position) <= MaxViewDistanceSq)
				{
					var falseAction = new BattleAction();
					falseAction.type = BA_SNAPSHOT;
					falseAction.actor = bu;
					falseAction.target = unit.Position;
					Position originVoxel = GetOriginVoxel(falseAction, null);
					Position targetVoxel = default;
					AIModule ai = bu.AIModule;

					// Inquisitor's note regarding 'gotHit' variable
					// in vanilla, the 'hitState' flag is the only part of this equation that comes into play.
					// any time a unit takes damage, this flag is set, then it would be reset by a call to
					// a function analogous to SavedBattleGame::resetUnitHitStates(), any time:
					// 1: a unit was selected by being clicked on.
					// 2: either "next unit" button was pressed.
					// 3: the inventory screen was accessed. (i didn't look too far into this one, it's possible it's only called in the pre-mission equip screen)
					// 4: the same place where we call it, immediately before every move the AI makes.
					// this flag is responsible for units turning around to respond to hits, and is in keeping with the details listed on http://www.ufopaedia.org/index.php/Reaction_fire_triggers
					// we've gone for a slightly different implementation: AI units keep a list of which units have hit them and don't forget until the end of the player's turn.
					// this method is in keeping with the spirit of the original feature, but much less exploitable by players.
					// the hitState flag in our implementation allows player units to turn and react as they did in the original, (which is far less cumbersome than giving them all an AI module)
					// we don't extend the same "enhanced aggressor memory" courtesy to players, because in the original, they could only turn and react to damage immediately after it happened.
					// this is because as much as we want the player's soldiers dead, we don't want them to feel like we're being unfair about it.

					bool gotHit = (ai != null && ai.GetWasHitBy(unit.Id)) || (ai == null && bu.HitState);

					if (!gotHit && Mod.EXTENDED_MELEE_REACTIONS == 2)
					{
						// to allow melee reactions when attacked from any side, not just from the front
						gotHit = bu.WasMeleeAttackedBy(unit.Id);
					}

					// can actually see the target Tile, or we got hit
					if ((bu.CheckViewSector(unit.Position) || gotHit) &&
						// can actually target the unit
						CanTargetUnit(ref originVoxel, tile, ref targetVoxel, bu, false) &&
						// can actually see the unit
						Visible(bu, tile))
					{
						if (bu.Faction == FACTION_PLAYER)
						{
							unit.Visible = true;
						}
						bu.AddToVisibleUnits(unit);
						ReactionScore rs = DetermineReactionType(bu, unit);
						if (rs.attackType != BA_NONE)
						{
							int reactionFireThreshold = _save.BattleGame.Mod.GetReactionFireThreshold(bu.Faction);
							if (reactionFireThreshold > 0)
							{
								BattleItem weapon = rs.weapon;
								int accuracy = BattleUnit.GetFiringAccuracy(BattleActionAttack.GetBeforeShoot(rs.attackType, rs.unit, weapon), _save.BattleGame.Mod);
								int distanceSq = unit.Distance3dToUnitSq(bu);
								int distance = (int)Math.Ceiling(Math.Sqrt((float)distanceSq));

								{
									int upperLimit = 0, lowerLimit = 0;
									int dropoff = weapon.Rules.CalculateLimits(ref upperLimit, ref lowerLimit, _save.Depth, rs.attackType);

									if (distance > upperLimit)
									{
										accuracy -= (distance - upperLimit) * dropoff;
									}
									else if (distance < lowerLimit)
									{
										accuracy -= (lowerLimit - distance) * dropoff;
									}
								}

								bool outOfRange = weapon.Rules.IsOutOfRange(distanceSq);

								if (accuracy >= reactionFireThreshold && !outOfRange)
								{
									spotters.Add(rs);
								}
							}
							else
							{
								spotters.Add(rs);
							}
						}
					}
				}
			}
		}
		return spotters;
	}

	/// <summary>Gets the unit with the highest reaction score from the spotter vector.</summary>
	/// <param name="spotters">The vector of spotting units.</param>
	/// <param name="unit">The unit to check scores against.</param>
	/// <returns>The unit with the highest reactions.</returns>
	private static ReactionScore GetReactor(List<ReactionScore> spotters, BattleUnit unit)
	{
		ReactionScore best = null;
		foreach (ReactionScore i in spotters)
		{
			if (!i.unit.IsOut && !i.unit.Respawn && (best == null || i.reactionScore > best.reactionScore))
			{
				best = i;
			}
		}
		if (best != null && (unit.ReactionScore <= best.reactionScore))
		{
			if (best.unit.OriginalFaction == FACTION_PLAYER)
			{
				best.unit.AddReactionExp();
			}
		}
		else
		{
			best = null;
		}
		return best;
	}

	/// <summary>Checks the validity of a snap shot performed here.</summary>
	/// <param name="unit">The unit to check sight from.</param>
	/// <param name="target">The unit to check sight TO.</param>
	/// <returns>The reaction (attack type <c>BA_NONE</c> if there is none).</returns>
	private ReactionScore DetermineReactionType(BattleUnit unit, BattleUnit target)
	{
		var reaction = new ReactionScore
		{
			unit = unit,
			weapon = null,
			attackType = BA_NONE,
			reactionScore = unit.ReactionScore,
			reactionReduction = 0.0,
			count = 1,
		};

		// to avoid 0 that cause infinite loop we set minimal reaction handled by logic, should correctly handle units with 1 point in reaction and 1/1000 TU
		if (reaction.reactionScore <= 0.001)
		{
			return reaction;
		}

		static void SetReaction(ReactionScore re, BattleActionType type, BattleItem weapon)
		{
			re.attackType = type;
			re.weapon = weapon;
			re.reactionReduction = 1.0 * new BattleActionCost(type, re.unit, weapon).Time * re.unit.BaseStats.reactions / re.unit.BaseStats.tu;
		}

		var reactionWeapons = new List<BattleItem>();
		// 1. first try the preferred weapon (player units only... to prevent abuse)
		bool isPlayer = (unit.Faction == FACTION_PLAYER);
		if (isPlayer)
		{
			if (unit.WeaponForReactions is BattleItem preferredWeapon)
			{
				reactionWeapons.Add(preferredWeapon);
			}
		}
		// 2. then prioritize melee
		if (unit.GetUtilityWeapon(BT_MELEE) is BattleItem meleeWeapon)
		{
			reactionWeapons.Add(meleeWeapon);
		}
		// 3. then the rest (AI: quickest weapon, Player: last selected/main weapon)
		if (unit.GetMainHandWeapon(!isPlayer, true) is BattleItem otherWeapon)
		{
			reactionWeapons.Add(otherWeapon);
		}

		int tempDirection = unit.Direction;
		if (Mod.EXTENDED_MELEE_REACTIONS == 2)
		{
			// temporarily face the target to allow melee reactions when attacked from any side, not just from the front
			tempDirection = GetDirectionTo(unit.Position, target.Position);
		}

		BattleItem disabledLeft = null;
		BattleItem disabledRight = null;
		// player units only... to prevent abuse
		if (isPlayer)
		{
			BattleItem leftHandItem = unit.LeftHandWeapon;
			BattleItem rightHandItem = unit.RightHandWeapon;
			BattleItem emptyHandItem = null;
			if ((leftHandItem == null && unit.IsLeftHandDisabledForReactions) || (rightHandItem == null && unit.IsRightHandDisabledForReactions))
			{
				BattleType[] typesToCheck = [BT_MELEE, BT_PSIAMP, BT_FIREARM/*, BT_MEDIKIT, BT_SCANNER, BT_MINDPROBE*/];
				foreach (BattleType type in typesToCheck)
				{
					emptyHandItem = unit.GetSpecialWeapon(type);
					if (emptyHandItem != null && emptyHandItem.Rules.IsSpecialUsingEmptyHand)
					{
						break;
					}
					emptyHandItem = null;
				}
			}
			disabledLeft = unit.IsLeftHandDisabledForReactions ? (leftHandItem ?? emptyHandItem) : null;
			disabledRight = unit.IsRightHandDisabledForReactions ? (rightHandItem ?? emptyHandItem) : null;
		}

		foreach (BattleItem weapon in reactionWeapons)
		{
			if (weapon == disabledLeft || weapon == disabledRight)
			{
				// the player doesn't want to react with this weapon
				continue;
			}

			if (_save.CanUseWeapon(weapon, unit, false, BA_HIT))
			{
				// has a weapon capable of melee and is in melee range
				if (ValidMeleeRange(unit, target, tempDirection) &&
					weapon.GetAmmoForAction(BA_HIT) != null &&
					new BattleActionCost(BA_HIT, unit, weapon).HaveTU())
				{
					SetReaction(reaction, BA_HIT, weapon);
					return reaction;
				}
			}
			if (_save.CanUseWeapon(weapon, unit, false, BA_SNAPSHOT))
			{
				// has a gun capable of snap shot with ammo
				if (weapon.Rules.BattleType == BT_FIREARM &&
					!weapon.Rules.IsOutOfRange(unit.Distance3dToUnitSq(target)) &&
					weapon.GetAmmoForAction(BA_SNAPSHOT) != null &&
					new BattleActionCost(BA_SNAPSHOT, unit, weapon).HaveTU())
				{
					SetReaction(reaction, BA_SNAPSHOT, weapon);
					return reaction;
				}
			}
		}

		return reaction;
	}

	/// <summary>Attempts to perform a reaction snap shot.</summary>
	/// <param name="reaction">The reacting unit and its weapon.</param>
	/// <param name="target">The unit to check sight TO.</param>
	/// <param name="originalAction">The action that triggered the reaction.</param>
	/// <returns>True if the action should (theoretically) succeed.</returns>
	private bool TryReaction(ReactionScore reaction, BattleUnit target, BattleAction originalAction)
	{
		var action = new BattleAction();
		action.cameraPosition = _save.BattleState.Map.Camera.MapOffset;
		action.actor = reaction.unit;
		action.weapon = reaction.weapon;
		action.type = reaction.attackType;

		if (!_save.CanUseWeapon(action.weapon, action.actor, false, action.type))
		{
			return false;
		}

		action.target = target.Position;
		action.UpdateTU();

		BattleUnit unit = action.actor;
		BattleItem ammo = action.weapon.GetAmmoForAction(action.type);
		if (ammo != null && action.HaveTU())
		{
			action.targeting = true;

			// hostile units will go into an "aggro" state when they react.
			if (unit.Faction == FACTION_HOSTILE)
			{
				AIModule ai = unit.AIModule;
				if (ai == null)
				{
					// should not happen, but just in case...
					ai = new AIModule(_save, unit, null);
					unit.AIModule = ai;
				}

				int radius = ammo.Rules.GetExplosionRadius(new BattleActionAttack(action.type, action.actor, action.weapon, ammo));
				if (action.type != BA_HIT && radius > 0 &&
					ai.ExplosiveEfficacy(action.target, unit, radius, -1) == 0)
				{
					action.targeting = false;
				}
			}

			if (action.targeting)
			{
				int meleeReactionChance = Mod.EXTENDED_MELEE_REACTIONS > 0 ? 100 : 0;
				int reactionChance = BA_HIT != originalAction.type ? 100 : meleeReactionChance;

				// script hooks ModScript::ReactionWeaponAction/ReactionUnitAction/ReactionUnitReaction not ported:
				// the unmodified chance is used (their extra inputs move type, distance, arc and original target are not needed)

				if (Rng.Percent(reactionChance))
				{
					_save.AppendToHitLog(HITLOG_REACTION_FIRE, unit.Faction);

					if (action.type == BA_HIT)
					{
						_save.BattleGame.StatePushBack(new MeleeAttackBState(_save.BattleGame, action));
					}
					else
					{
						_save.BattleGame.StatePushBack(new ProjectileFlyBState(_save.BattleGame, action));
					}
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Handling of hitting tile.</summary>
	/// <param name="tile">targeted tile.</param>
	/// <param name="damage">power of hit.</param>
	/// <param name="type">damage type of hit.</param>
	/// <returns>whether a smoke (1) or fire (2) effect was produced.</returns>
	public int HitTile(Tile tile, int damage, RuleDamageType type)
	{
		if (damage >= type.SmokeThreshold)
		{
			// smoke from explosions always stay 6 to 14 turns - power of a smoke grenade is 60
			if (tile.Smoke < _save.BattleGame.Mod.TooMuchSmokeThreshold && tile.TerrainLevel > -24)
			{
				tile.Fire = 0;
				if (damage >= type.SmokeThreshold * 2)
					tile.Smoke = Rng.Generate(7, 15); // for SmokeThreshold == 0
				else
					tile.Smoke = Rng.Generate(7, 15) * (damage - type.SmokeThreshold) / type.SmokeThreshold;
				return 1;
			}
		}
		else if (damage >= type.FireThreshold)
		{
			if (!tile.IsVoid)
			{
				if (tile.Fire == 0 && (tile.GetMapData(O_FLOOR) != null || tile.GetMapData(O_OBJECT) != null))
				{
					if (damage >= type.FireThreshold * 2)
						tile.Fire = tile.Fuel + 1; // for FireThreshold == 0
					else
						tile.Fire = tile.Fuel * (damage - type.FireThreshold) / type.FireThreshold + 1;
					tile.Smoke = Math.Max(1, Math.Min(15 - (tile.Flammability / 10), 12));
					return 2;
				}
			}
		}
		return 0;
	}

	/// <summary>Handling of experience training.</summary>
	/// <param name="attack">Attacker and weapon causing the damage.</param>
	/// <param name="target">targeted unit.</param>
	/// <param name="rangeAtack">is ranged attack or not?</param>
	/// <returns>Was experience awarded or not?</returns>
	public bool AwardExperience(BattleActionAttack attack, BattleUnit target, bool rangeAtack)
	{
		if (_save.IsPreview)
		{
			return false;
		}

		BattleUnit unit = attack.attacker;
		BattleItem weapon = attack.weapon_item;

		if (target == null)
		{
			return false;
		}

		if (weapon == null)
		{
			return false;
		}

		// Mana experience - this is a temporary/experimental approach, can be improved later after modder feedback
		unit.AddManaExp(weapon.Rules.ManaExperience);

		ExperienceTrainingMode expType = weapon.Rules.ExperienceTrainingMode;
		System.Action expFuncA = null;
		System.Action expFuncB = null;
		int expMultiply = 100;

		if (expType > ETM_DEFAULT)
		{
			// can train psi strength and psi skill only if psi skill is already > 0
			if (expType >= ETM_PSI_STRENGTH && expType <= ETM_PSI_STRENGTH_OR_SKILL_2X)
			{
				// cannot use "unit->getBaseStats()->psiSkill", because armor can give +psiSkill bonus
				if (unit.GeoscapeSoldier != null && unit.GeoscapeSoldier.CurrentStats.psiSkill <= 0)
					return false;
			}

			switch (weapon.Rules.ExperienceTrainingMode)
			{
				case ETM_MELEE_100: expFuncA = unit.AddMeleeExp; break;
				case ETM_MELEE_50: expMultiply = 50; expFuncA = unit.AddMeleeExp; break;
				case ETM_MELEE_33: expMultiply = 33; expFuncA = unit.AddMeleeExp; break;
				case ETM_FIRING_100: expFuncA = unit.AddFiringExp; break;
				case ETM_FIRING_50: expMultiply = 50; expFuncA = unit.AddFiringExp; break;
				case ETM_FIRING_33: expMultiply = 33; expFuncA = unit.AddFiringExp; break;
				case ETM_THROWING_100: expFuncA = unit.AddThrowingExp; break;
				case ETM_THROWING_50: expMultiply = 50; expFuncA = unit.AddThrowingExp; break;
				case ETM_THROWING_33: expMultiply = 33; expFuncA = unit.AddThrowingExp; break;
				case ETM_FIRING_AND_THROWING: expFuncA = unit.AddFiringExp; expFuncB = unit.AddThrowingExp; break;
				case ETM_FIRING_OR_THROWING: if (Rng.Percent(50)) { expFuncA = unit.AddFiringExp; } else { expFuncA = unit.AddThrowingExp; } break;
				case ETM_REACTIONS: expMultiply = 100; expFuncA = unit.AddReactionExp; break;
				case ETM_REACTIONS_AND_MELEE: expFuncA = unit.AddReactionExp; expFuncB = unit.AddMeleeExp; break;
				case ETM_REACTIONS_AND_FIRING: expFuncA = unit.AddReactionExp; expFuncB = unit.AddFiringExp; break;
				case ETM_REACTIONS_AND_THROWING: expFuncA = unit.AddReactionExp; expFuncB = unit.AddThrowingExp; break;
				case ETM_REACTIONS_OR_MELEE: if (Rng.Percent(50)) { expFuncA = unit.AddReactionExp; } else { expFuncA = unit.AddMeleeExp; } break;
				case ETM_REACTIONS_OR_FIRING: if (Rng.Percent(50)) { expFuncA = unit.AddReactionExp; } else { expFuncA = unit.AddFiringExp; } break;
				case ETM_REACTIONS_OR_THROWING: if (Rng.Percent(50)) { expFuncA = unit.AddReactionExp; } else { expFuncA = unit.AddThrowingExp; } break;
				case ETM_BRAVERY: expFuncA = unit.AddBraveryExp; break;
				case ETM_BRAVERY_2X: expMultiply = 200; expFuncA = unit.AddBraveryExp; break;
				case ETM_BRAVERY_AND_REACTIONS: expFuncA = unit.AddBraveryExp; expFuncB = unit.AddReactionExp; break;
				case ETM_BRAVERY_OR_REACTIONS: if (Rng.Percent(50)) { expFuncA = unit.AddBraveryExp; } else { expFuncA = unit.AddReactionExp; } break;
				case ETM_BRAVERY_OR_REACTIONS_2X: expMultiply = 200; if (Rng.Percent(50)) { expFuncA = unit.AddBraveryExp; } else { expFuncA = unit.AddReactionExp; } break;
				case ETM_PSI_STRENGTH: expFuncA = unit.AddPsiStrengthExp; break;
				case ETM_PSI_STRENGTH_2X: expMultiply = 200; expFuncA = unit.AddPsiStrengthExp; break;
				case ETM_PSI_SKILL: expFuncA = unit.AddPsiSkillExp; break;
				case ETM_PSI_SKILL_2X: expMultiply = 200; expFuncA = unit.AddPsiSkillExp; break;
				case ETM_PSI_STRENGTH_AND_SKILL: expFuncA = unit.AddPsiStrengthExp; expFuncB = unit.AddPsiSkillExp; break;
				case ETM_PSI_STRENGTH_AND_SKILL_2X: expMultiply = 200; expFuncA = unit.AddPsiStrengthExp; expFuncB = unit.AddPsiSkillExp; break;
				case ETM_PSI_STRENGTH_OR_SKILL: if (Rng.Percent(50)) { expFuncA = unit.AddPsiStrengthExp; } else { expFuncA = unit.AddPsiSkillExp; } break;
				case ETM_PSI_STRENGTH_OR_SKILL_2X: expMultiply = 200; if (Rng.Percent(50)) { expFuncA = unit.AddPsiStrengthExp; } else { expFuncA = unit.AddPsiSkillExp; } break;
				case ETM_NOTHING:
				default:
					return false;
			}
		}
		else
		{
			// GRENADES AND PROXIES
			if (weapon.Rules.BattleType == BT_GRENADE || weapon.Rules.BattleType == BT_PROXIMITYGRENADE)
			{
				if (Mod.EXTENDED_EXPERIENCE_AWARD_SYSTEM)
				{
					expType = ETM_THROWING_100;
					expFuncA = unit.AddThrowingExp; // e.g. acid grenade, stun grenade, HE grenade, smoke grenade, proxy grenade, ...
				}
				else
				{
					expType = ETM_FIRING_100;
					expFuncA = unit.AddFiringExp; // vanilla compatibility
				}
			}
			// MELEE
			else if (weapon.Rules.BattleType == BT_MELEE)
			{
				expType = ETM_MELEE_100;
				expFuncA = unit.AddMeleeExp; // e.g. cattle prod, cutlass, rope, ...
			}
			// MEDIKITS
			else if (weapon.Rules.BattleType == BT_MEDIKIT)
			{
				// medikits don't train anything by default
				return false;
			}
			// FIREARMS and other
			else
			{
				if (!rangeAtack)
				{
					expType = ETM_MELEE_100;
					expFuncA = unit.AddMeleeExp; // e.g. rifle/shotgun gun butt, ...
				}
				else if (!Mod.EXTENDED_EXPERIENCE_AWARD_SYSTEM)
				{
					expType = ETM_FIRING_100;
					expFuncA = unit.AddFiringExp; // vanilla compatibility
				}
				else if (weapon.GetArcingShot(attack.type))
				{
					expType = ETM_THROWING_100;
					expFuncA = unit.AddThrowingExp; // e.g. flamethrower, javelins, combat bow, grenade launcher, black powder bomb, stick grenade, acid flask, apple, ...
				}
				else
				{
					int maxRange = weapon.Rules.MaxRange;
					if (maxRange > 10)
					{
						expType = ETM_FIRING_100;
						expFuncA = unit.AddFiringExp; // e.g. harpoon gun, shotgun, assault rifle, rocket launcher, small launcher, heavy cannon, blaster launcher, ...
					}
					else if (maxRange > 1)
					{
						expType = ETM_THROWING_100;
						expFuncA = unit.AddThrowingExp; // e.g. throwing knives, zapper, ...
					}
					else if (maxRange == 1)
					{
						expType = ETM_MELEE_100;
						expFuncA = unit.AddMeleeExp; // e.g. hammer, chainsaw, fusion torch, ...
					}
					else
					{
						return false; // what is this? no training!
					}
				}
			}
		}
		_ = expType; // only an input of the (not ported) AwardExperience script hook

		if (weapon.Rules.BattleType != BT_MEDIKIT)
		{
			// only enemies count, not friends or neutrals
			if (target.OriginalFaction != FACTION_HOSTILE) expMultiply = 0;

			if (Mod.EXTENDED_EXPERIENCE_AWARD_SYSTEM)
			{
				// mind-controlled enemies don't count though!
				if (target.Faction != FACTION_HOSTILE) expMultiply = 0;
			}
		}

		// script hook ModScript::AwardExperience (armor) not ported: expMultiply is unmodified

		for (int i = expMultiply / 100; i > 0; --i)
		{
			expFuncA?.Invoke();
			expFuncB?.Invoke();
		}
		if (Rng.Percent(expMultiply % 100))
		{
			expFuncA?.Invoke();
			expFuncB?.Invoke();
		}

		return true;
	}

	/// <summary>Handling of hitting unit.</summary>
	/// <param name="attack">Hitter, weapon and clip causing the damage.</param>
	/// <param name="target">targeted unit.</param>
	/// <param name="relative">angle of hit.</param>
	/// <param name="damage">power of hit.</param>
	/// <param name="type">damage type of hit.</param>
	/// <param name="rangeAtack">is ranged attack or not?</param>
	/// <returns>Did unit get hit?</returns>
	public bool HitUnit(BattleActionAttack attack, BattleUnit target, Position relative, int damage, RuleDamageType type, bool rangeAtack = true)
	{
		if (_save.IsPreview)
		{
			return false;
		}
		if (target == null || target.Health <= 0)
		{
			return false;
		}

		int healthOrig = target.Health;
		int stunLevelOrig = target.Stunlevel;

		target.Damage(relative, damage, type, _save, attack);

		int healthDamage = healthOrig - target.Health;
		int stunDamage = target.Stunlevel - stunLevelOrig;

		// hit log
		if (attack.attacker != null)
		{
			if (healthDamage > 0 || stunDamage > 0)
			{
				int damagePercent = ((healthDamage + stunDamage) * 100) / target.BaseStats.health;
				if (damagePercent <= 20)
				{
					_save.AppendToHitLog(HITLOG_SMALL_DAMAGE, attack.attacker.Faction);
				}
				else
				{
					_save.AppendToHitLog(HITLOG_BIG_DAMAGE, attack.attacker.Faction);
				}
			}
			else
			{
				_save.AppendToHitLog(HITLOG_NO_DAMAGE, attack.attacker.Faction);
			}
		}

		// single place for firing/throwing/melee experience training
		if (attack.attacker != null && attack.attacker.OriginalFaction == FACTION_PLAYER)
		{
			AwardExperience(attack, target, rangeAtack);
		}

		// Use case: an xcom soldier throwing a smoke grenade on a dying unit should not override the previously remembered murderer
		bool isRelevant = true;
		if (attack.attacker != null
			&& healthDamage <= 0
			&& target.MurdererId > 0
			&& (target.Fire > 0 || target.FatalWounds > 0 || target.HasNegativeHealthRegen))
		{
			isRelevant = false;
		}

		if (isRelevant && attack.attacker != null)
		{
			// Record the last unit to hit our victim. If a victim dies without warning*, this unit gets the credit.
			// *Because the unit died in a fire or bled out.
			target.MurdererId = attack.attacker.Id;
			target.MurdererWeapon = "STR_WEAPON_UNKNOWN";
			target.MurdererWeaponAmmo = "STR_WEAPON_UNKNOWN";
			if (attack.weapon_item != null)
			{
				target.MurdererWeapon = attack.weapon_item.Rules.Name;
			}
			if (attack.damage_item != null)
			{
				target.MurdererWeaponAmmo = attack.damage_item.Rules.Name;
			}
		}

		return true;
	}

	/// <summary>Decrements the base module counter of the module map cell (C++ <c>getModuleMap()[x][y].second--</c>).</summary>
	private void DecrementModuleMap(int x, int y)
	{
		List<List<(int first, int second)>> moduleMap = _save.ModuleMap;
		(int first, int second) cell = moduleMap[x][y];
		cell.second--;
		moduleMap[x][y] = cell;
	}

	/// <summary>Handles bullet/weapon hits. A bullet/weapon hits a voxel.</summary>
	/// <param name="attack">The unit and items that caused the hit.</param>
	/// <param name="center">Center of the explosion in voxelspace.</param>
	/// <param name="power">Power of the explosion.</param>
	/// <param name="type">The damage type of the explosion.</param>
	/// <param name="rangeAtack">Is ranged attack or not?</param>
	/// <param name="terrainMeleeTilePart">Tile part hit by a terrain melee attack (0 = none).</param>
	public void Hit(BattleActionAttack attack, Position center, int power, RuleDamageType type, bool rangeAtack = true, int terrainMeleeTilePart = 0)
	{
		bool terrainChanged = false; //did the hit destroy a tile thereby changing line of sight?
		int effectGenerated = 0; //did the hit produce smoke (1), fire/light (2) or disabled a unit (3) ?
		Position tilePos = center.ToTile();
		Tile tile = _save.GetTile(tilePos);
		if (tile == null || power <= 0)
		{
			return;
		}

		VoxelCheckFlush();
		VoxelType part = (terrainMeleeTilePart > 0) ? (VoxelType)terrainMeleeTilePart : VoxelCheck(center, attack.attacker);
		int damage = type.GetRandomDamage(power);
		int tileFinalDamage = type.GetTileFinalDamage(type.GetRandomDamageForTile(power, damage));
		if (part >= V_FLOOR && part <= V_OBJECT)
		{
			bool nothing = true;
			if (terrainMeleeTilePart == 0 && (part == V_FLOOR || part == V_OBJECT))
			{
				foreach (BattleItem bi in tile.Inventory)
				{
					if (HitUnit(attack, bi.Unit, new Position(0, 0, 0), damage, type, rangeAtack))
					{
						if (bi.Glow) effectGenerated = 2; //Any glowing corpses?
						nothing = false;
						break;
					}
				}
			}
			if (nothing)
			{
				var tp = (TilePart)part;
				//Do we need to update the visibility of units due to smoke/fire?
				effectGenerated = HitTile(tile, damage, type);
				//If a tile was destroyed we may have revealed new areas for one or more observers
				if (tileFinalDamage >= tile.GetMapData(tp).Armor) terrainChanged = true;

				if (part == V_OBJECT && _save.MissionType == "STR_BASE_DEFENSE")
				{
					if (tileFinalDamage >= tile.GetMapData(O_OBJECT).Armor && tile.GetMapData(O_OBJECT).IsBaseModule)
					{
						DecrementModuleMap((center.x / 16) / 10, (center.y / 16) / 10);
					}
				}
				if (tile.Damage(tp, tileFinalDamage, _save.ObjectiveType))
				{
					_save.AddDestroyedObjective();
				}
			}
		}
		else if (part == V_UNIT)
		{
			BattleUnit bu = tile.GetOverlappingUnit(_save);
			if (bu != null && bu.Health > 0)
			{
				int verticaloffset = 0;
				if (bu != tile.Unit)
				{
					verticaloffset = 24;
				}
				int sz = bu.Armor.Size * 8;
				Position target = bu.Position.ToVoxel() + new Position(sz, sz, bu.FloatHeight - tile.TerrainLevel);
				Position relative = (center - target) - new Position(0, 0, verticaloffset);

				HitUnit(attack, bu, relative, damage, type, rangeAtack);
				if (bu.Fire != 0)
				{
					effectGenerated = 2;
				}
			}
		}
		//Recalculate relevant item/unit locations and visibility depending on what happened during the hit
		if (terrainChanged || effectGenerated != 0)
		{
			ApplyGravity(tile);
			LightLayers layer = LL_ITEMS;
			if (part == V_FLOOR && _save.GetTile(tilePos - new Position(0, 0, 1)) != null)
			{
				layer = LL_AMBIENT; // roof destroyed, update sunlight in this tile column
			}
			else if (terrainChanged || effectGenerated != 0)
			{
				layer = LL_FIRE; // spawned fire or smoke that can block light.
			}
			CalculateLighting(layer, tilePos, 1, true);
			CalculateFOV(tilePos, 1, true, terrainChanged); //append any new units or tiles revealed by the terrain change
		}
		else
		{
			// script could affect visibility of units, fast check if something is changed.
			CalculateFOV(tilePos, 1, false); //skip updating of tiles
		}
		//Note: If bu was knocked out this will have no effect on unit visibility quite yet, as it is not marked as out
		//and will continue to block visibility at this point in time.
	}

	/// <summary>
	/// Handles explosions.
	/// HE, smoke and fire explodes in a circular pattern on 1 level only. HE however damages floor tiles of the above level. Not the units on it.
	/// HE destroys an object if its armor is lower than the explosive power, then it's HE blockage is applied for further propagation.
	/// See http://www.ufopaedia.org/index.php?title=Explosions for more info.
	/// </summary>
	/// <param name="attack">The unit and items that caused the explosion.</param>
	/// <param name="center">Center of the explosion in voxelspace.</param>
	/// <param name="power">Power of the explosion.</param>
	/// <param name="type">The damage type of the explosion.</param>
	/// <param name="maxRadius">The maximum radius of the explosion.</param>
	/// <param name="rangeAtack">Is ranged attack or not?</param>
	public void Explode(BattleActionAttack attack, Position center, int power, RuleDamageType type, int maxRadius, bool rangeAtack = true)
	{
		Position centetTile = center.ToTile();
		int hitSide = 0;
		int diagonalWall = 0;
		int power_;
		// C++ std::map<Tile*, int>: tiles live in one array, so pointer order is tile index order
		var tilesAffected = new SortedDictionary<int, int>();
		var toRemove = new List<BattleItem>();

		if (type.FireBlastCalc)
		{
			power /= 2;
		}

		int exHeight = Clamp(Options.battleExplosionHeight, 0, 3);
		int vertdec = 1000; //default flat explosion

		switch (exHeight)
		{
			case 1:
				vertdec = (int)(3.0f * type.RadiusReduction);
				break;
			case 2:
				vertdec = (int)(1.0f * type.RadiusReduction);
				break;
			case 3:
				vertdec = (int)(0.5f * type.RadiusReduction);
				break;
		}

		Tile origin = _save.GetTile(centetTile);
		Tile dest = null;
		if (origin.IsBigWall) //pre-calculations for bigwall deflection
		{
			diagonalWall = origin.GetMapData(O_OBJECT).BigWall;
			if (diagonalWall == BIGWALLNWSE) //  3 |
				hitSide = (center.x % 16 - center.y % 16) > 0 ? 1 : -1;
			if (diagonalWall == BIGWALLNESW) //  2 --
				hitSide = (center.x % 16 + center.y % 16 - 15) > 0 ? 1 : -1;
		}

		for (int fi = -90; fi <= 90; fi += 5)
		{
			// raytrace every 3 degrees makes sure we cover all tiles in a circle.
			for (int te = 0; te <= 360; te += 3)
			{
				double cos_te = Math.Cos(Deg2Rad(te));
				double sin_te = Math.Sin(Deg2Rad(te));
				double sin_fi = Math.Sin(Deg2Rad(fi));
				double cos_fi = Math.Cos(Deg2Rad(fi));

				origin = _save.GetTile(centetTile);
				dest = origin;
				double l = 0;
				int tileX, tileY, tileZ;
				power_ = power;
				while (power_ > 0 && l <= maxRadius)
				{
					if (power_ > 0)
					{
						int key = _save.GetTileIndex(dest.Position);
						bool inserted = tilesAffected.TryAdd(key, 0); // check if we had this tile already affected

						int tileDmg = type.GetTileFinalDamage(power_);
						if (tileDmg > tilesAffected[key])
						{
							tilesAffected[key] = tileDmg;
						}
						if (inserted)
						{
							int damage = type.GetRandomDamage(power_);
							BattleUnit bu = dest.GetOverlappingUnit(_save);

							toRemove.Clear();
							if (bu != null)
							{
								if (dest.Position == centetTile)
								{
									// direct hit, similar to ground zero but AI will remember attacker, done for compatibility
									HitUnit(attack, bu, new Position(0, 0, 0), damage, type, rangeAtack);
								}
								else if (
										(
											Position.Distance2dSq(dest.Position, centetTile) < 4
											&& dest.Position.z == centetTile.z
										)
										|| dest.Position.z > centetTile.z
									)
								{
									// ground zero effect is in effect, or unit is above explosion
									HitUnit(attack, bu, new Position(0, 0, -1), damage, type, rangeAtack);
								}
								else
								{
									// directional damage relative to explosion position.
									// units above the explosion will be hit in the legs, units lateral to or below will be hit in the torso
									HitUnit(attack, bu, centetTile + new Position(0, 0, 5) - dest.Position, damage, type, rangeAtack);
								}

								// Affect all items and units in inventory
								int itemDamage = bu.OverKillDamage;
								if (itemDamage > 0)
								{
									foreach (BattleItem bi in bu.Inventory)
									{
										if (!HitUnit(attack, bi.Unit, new Position(0, 0, 0), itemDamage, type, rangeAtack) && type.GetItemFinalDamage(itemDamage) > bi.Rules.Armor)
										{
											toRemove.Add(bi);
										}
									}
								}
							}
							// Affect all items and units on ground
							foreach (BattleItem bi in dest.Inventory)
							{
								if (!HitUnit(attack, bi.Unit, new Position(0, 0, 0), damage, type) && type.GetItemFinalDamage(damage) > bi.Rules.Armor)
								{
									toRemove.Add(bi);
								}
							}
							foreach (BattleItem bi in toRemove)
							{
								_save.RemoveItem(bi);
							}

							HitTile(dest, damage, type);
						}
					}

					l += 1.0;

					tileX = (int)Math.Floor(centetTile.x + 0.5 + l * sin_te * cos_fi);
					tileY = (int)Math.Floor(centetTile.y + 0.5 + l * cos_te * cos_fi);
					tileZ = (int)Math.Floor(centetTile.z + 0.5 + l * sin_fi);

					origin = dest;
					dest = _save.GetTile(new Position(tileX, tileY, tileZ));

					if (dest == null) break; // out of map!

					// blockage by terrain is deducted from the explosion power
					power_ = (int)(power_ - type.RadiusReduction); // explosive damage decreases by 10 per tile
					if (origin.Position.z != tileZ)
						power_ -= vertdec; //3d explosion factor

					if (type.FireBlastCalc)
					{
						int dir = 0;
						Pathfinding.VectorToDirection(origin.Position - dest.Position, ref dir);
						if (dir != -1 && (dir % 2) != 0) power_ = (int)(power_ - 0.5f * type.RadiusReduction); // diagonal movement costs an extra 50% for fire.
					}
					if (l > 0.5)
					{
						if (l > 1.5)
						{
							power_ -= VerticalBlockage(origin, dest, type.ResistType, false) * 2;
							power_ -= HorizontalBlockage(origin, dest, type.ResistType, false) * 2;
						}
						else //tricky bigwall deflection /Volutar
						{
							bool skipObject = diagonalWall == 0;
							if (diagonalWall == BIGWALLNESW) // --
							{
								if (hitSide < 0 && te >= 135 && te < 315)
									skipObject = true;
								if (hitSide > 0 && (te < 135 || te > 315))
									skipObject = true;
							}
							if (diagonalWall == BIGWALLNWSE) // |
							{
								if (hitSide > 0 && te >= 45 && te < 225)
									skipObject = true;
								if (hitSide < 0 && (te < 45 || te > 225))
									skipObject = true;
							}
							power_ -= VerticalBlockage(origin, dest, type.ResistType, skipObject) * 2;
							power_ -= HorizontalBlockage(origin, dest, type.ResistType, skipObject) * 2;

						}
					}
				}
			}
		}

		// now detonate the tiles affected by explosion
		if (type.ToTile > 0.0f)
		{
			foreach (KeyValuePair<int, int> pair in tilesAffected)
			{
				Tile affected = _save.GetTile(pair.Key);
				if (Detonate(affected, pair.Value))
				{
					_save.AddDestroyedObjective();
				}
				ApplyGravity(affected);
				Tile j = _save.GetTile(affected.Position + new Position(0, 0, 1));
				if (j != null)
					ApplyGravity(j);
			}
		}
		CalculateLighting(LL_AMBIENT, centetTile, maxRadius + 1, true); // roofs could have been destroyed and fires could have been started
		CalculateFOV(centetTile, maxRadius + 1, true, true);
		if (attack.attacker != null && Position.Distance2d(centetTile, attack.attacker.Position) > maxRadius + 1)
		{
			// unit is away from blast but its visibility can be affected by scripts.
			CalculateFOV(centetTile, 1, false);
		}
	}

	private static readonly TilePart[] detonateParts = [O_FLOOR, O_WESTWALL, O_NORTHWALL, O_FLOOR, O_WESTWALL, O_NORTHWALL, O_OBJECT, O_OBJECT, O_OBJECT]; //6th is the object of current

	/// <summary>
	/// Applies the explosive power to the tile parts. This is where the actual destruction takes place.
	/// Must affect 9 objects (6 box sides and the object inside plus 2 outer walls).
	/// </summary>
	/// <param name="tile">Tile affected.</param>
	/// <param name="explosive">Power of the explosion on this tile.</param>
	/// <returns>True if the objective was destroyed.</returns>
	public bool Detonate(Tile tile, int explosive)
	{
		if (explosive == 0) return false; // no damage applied for this tile
		bool objective = false;
		var tiles = new Tile[9];
		TilePart[] parts = detonateParts;
		Position pos = tile.Position;

		tiles[0] = _save.GetTile(new Position(pos.x, pos.y, pos.z + 1)); //ceiling
		tiles[1] = _save.GetTile(new Position(pos.x + 1, pos.y, pos.z)); //east wall
		tiles[2] = _save.GetTile(new Position(pos.x, pos.y + 1, pos.z)); //south wall
		tiles[3] = tiles[4] = tiles[5] = tiles[6] = tile;
		tiles[7] = _save.GetTile(new Position(pos.x, pos.y - 1, pos.z)); //north bigwall
		tiles[8] = _save.GetTile(new Position(pos.x - 1, pos.y, pos.z)); //west bigwall

		int remainingPower, fireProof, fuel;
		bool destroyed, bigwalldestroyed = true, skipnorthwest = false;
		for (int i = 8; i >= 0; --i)
		{
			if (tiles[i] == null || tiles[i].GetMapData(parts[i]) == null)
				continue; //skip out of map and emptiness
			int bigwall = tiles[i].GetMapData(parts[i]).BigWall;
			if (i > 6 && !((bigwall == 1) || (bigwall == 8) || (i == 8 && bigwall == 6) || (i == 7 && bigwall == 7)))
				continue;
			if ((bigwall != 0)) skipnorthwest = true;
			if (!bigwalldestroyed && i < 6) //when ground shouldn't be destroyed
				continue;
			if (skipnorthwest && (i == 2 || i == 1)) continue;
			remainingPower = explosive;
			destroyed = false;
			int volume = 0;
			TilePart currentpart = parts[i], currentpart2;
			int diemcd;
			fireProof = tiles[i].GetFlammability(currentpart);
			fuel = tiles[i].GetFuel(currentpart) + 1;
			// get the volume of the object by checking it's loftemps objects.
			for (int j = 0; j < 12; j++)
			{
				if (tiles[i].GetMapData(currentpart).GetLoftID(j) != 0)
					++volume;
			}
			if (i == 6 &&
				(bigwall == 2 || bigwall == 3) && //diagonals
				tiles[i].GetMapData(currentpart).Armor > remainingPower) //not enough to destroy
			{
				bigwalldestroyed = false;
			}
			// iterate through tile armor and destroy if can
			while (tiles[i].GetMapData(currentpart) != null &&
					tiles[i].GetMapData(currentpart).Armor <= remainingPower &&
					tiles[i].GetMapData(currentpart).Armor != 255)
			{
				if (i == 6 && (bigwall == 2 || bigwall == 3)) //diagonals for the current tile
				{
					bigwalldestroyed = true;
				}
				if (i == 6 && (bigwall == 6 || bigwall == 7 || bigwall == 8)) //n/w/nw
				{
					skipnorthwest = false;
				}
				remainingPower -= tiles[i].GetMapData(currentpart).Armor;
				destroyed = true;
				if (_save.MissionType == "STR_BASE_DEFENSE" &&
					tiles[i].GetMapData(currentpart).IsBaseModule)
				{
					DecrementModuleMap(tile.Position.x / 10, tile.Position.y / 10);
				}
				//this trick is to follow transformed object parts (object can become a ground)
				diemcd = tiles[i].GetMapData(currentpart).DieMCD;
				if (diemcd != 0)
					currentpart2 = tiles[i].GetMapData(currentpart).Dataset.GetObject(diemcd).ObjectType;
				else
					currentpart2 = currentpart;
				if (tiles[i].Destroy(currentpart, _save.ObjectiveType))
					objective = true;
				currentpart = currentpart2;
				if (tiles[i].GetMapData(currentpart) != null) // take new values
				{
					fireProof = tiles[i].GetFlammability(currentpart);
					fuel = tiles[i].GetFuel(currentpart) + 1;
				}
			}
			// set tile on fire
			if (fireProof < remainingPower)
			{
				if (tiles[i].GetMapData(O_FLOOR) != null || tiles[i].GetMapData(O_OBJECT) != null)
				{
					tiles[i].Fire = fuel;
					tiles[i].Smoke = Clamp(15 - (fireProof / 10), 1, 12);
				}
			}
			// add some smoke if tile was destroyed and not set on fire
			if (destroyed)
			{
				if (tiles[i].Fire != 0 && tiles[i].GetMapData(O_FLOOR) == null && tiles[i].GetMapData(O_OBJECT) == null)
				{
					tiles[i].Fire = 0;// if the object set the floor on fire, and the floor was subsequently destroyed, the fire needs to go out
				}

				if (tiles[i].Fire == 0)
				{
					int smoke = Rng.Generate(1, (volume / 2) + 3) + (volume / 2);
					if (smoke > tiles[i].Smoke)
					{
						tiles[i].Smoke = Clamp(smoke, 0, 15);
					}
				}
			}
		}
		return objective;
	}

	/// <summary>
	/// Checks for chained explosions.
	/// Chained explosions are explosions which occur after an explosive map object is destroyed.
	/// May be due a direct hit, other explosion or fire.
	/// </summary>
	/// <returns>tile on which a explosion occurred.</returns>
	public Tile CheckForTerrainExplosions()
	{
		if (_save.IsPreview)
		{
			return null;
		}

		for (int i = 0; i < _save.MapSizeXYZ; ++i)
		{
			if (_save.GetTile(i).Explosive != 0)
			{
				return _save.GetTile(i);
			}
		}
		return null;
	}

	/// <summary>Calculate strength of psi attack based on range and victim.</summary>
	/// <param name="attack">Type of attack, attacking unit and attack item.</param>
	/// <param name="victim">Attacked unit.</param>
	/// <returns>Value greater than zero mean successful attack.</returns>
#pragma warning disable CA1822 // instance member in C++, called through the TileEngine instance
	public int PsiAttackCalculate(BattleActionAttack attack, BattleUnit victim)
#pragma warning restore CA1822
	{
		if (victim == null)
			return 0;

		BattleUnit attacker = attack.attacker;
		BattleItem weapon = attack.weapon_item;

		int attackStrength = BattleUnit.GetPsiAccuracy(attack);
		int defenseStrength = 30 + victim.Armor.GetPsiDefence(victim);

		float dis = Position.Distance(attacker.Position.ToVoxel(), victim.Position.ToVoxel());

		Rng.RandomState rng = Rng.GlobalRandomState.SubSequence();
		int psiAttackResult = 0;

		// ModScript::TryPsiAttackItem: the item script is not ported, its built-in default script (BattleItem.cpp) is applied:
		// "var int r; random.randomRange r 0 55; add psi_attack_success attack_strength; add psi_attack_success r;
		//  sub psi_attack_success defense_strength; sub psi_attack_success distance_strength_reduction; return psi_attack_success;"
		{
			int r = rng.Generate(0, 55);
			int distanceStrengthReduction = (int)weapon.Rules.GetPsiAccuracyRangeReduction(dis);
			psiAttackResult += attackStrength;
			psiAttackResult += r;
			psiAttackResult -= defenseStrength;
			psiAttackResult -= distanceStrengthReduction;
		}

		// ModScript::TryPsiAttackUnit (armor, no default script) not ported: result unmodified

		return psiAttackResult;
	}

	/// <summary>Attempts a panic or mind control action.</summary>
	/// <param name="attack">The attack.</param>
	/// <param name="victim">The attacked unit.</param>
	/// <returns>Whether it failed or succeeded.</returns>
	public bool PsiAttack(BattleActionAttack attack, BattleUnit victim)
	{
		if (_save.IsPreview)
		{
			return false;
		}

		if (victim == null)
			return false;

		// Mana experience - this is a temporary/experimental approach, can be improved later after modder feedback
		attack.attacker.AddManaExp(attack.weapon_item.Rules.ManaExperience);

		bool isDefaultExpTrainingMode = (attack.weapon_item.Rules.ExperienceTrainingMode == ETM_DEFAULT);
		bool isNaturallyPsiCapable = true;
		if (attack.attacker.GeoscapeSoldier != null && attack.attacker.GeoscapeSoldier.CurrentStats.psiSkill <= 0)
		{
			isNaturallyPsiCapable = false;
		}
		bool isPsiRequired = attack.weapon_item.Rules.IsPsiRequired;

		if (isDefaultExpTrainingMode)
		{
			if (isNaturallyPsiCapable)
			{
				attack.attacker.AddPsiSkillExp();
			}
		}
		if (Options.allowPsiStrengthImprovement && isPsiRequired)
		{
			victim.AddPsiStrengthExp(); // experience for the victim, not the attacker
		}

		if (PsiAttackCalculate(attack, victim) > 0)
		{
			if (isDefaultExpTrainingMode)
			{
				if (isNaturallyPsiCapable)
				{
					attack.attacker.AddPsiSkillExp();
					attack.attacker.AddPsiSkillExp();
				}
			}
			else if (attack.type == BA_PANIC || attack.type == BA_MINDCONTROL)
			{
				// Note: BA_USE is handled elsewhere
				AwardExperience(attack, victim, false);
			}

			var killStat = new BattleUnitKills();
			killStat.SetUnitStats(victim);
			killStat.SetTurn(_save.Turn, _save.Side);
			killStat.weapon = attack.weapon_item.Rules.Name;
			killStat.weaponAmmo = attack.weapon_item.Rules.Name; //Psi weapons got no ammo, just filling up the field
			killStat.faction = victim.OriginalFaction;
			killStat.mission = _save.GeoscapeSave.MissionStatistics.Count;
			killStat.id = victim.Id;

			// C++ pushes copies of killStat; the local is not used afterwards, so it is added itself
			if (attack.type == BA_PANIC)
			{
				int moraleLoss = victim.ReduceByBravery(100);
				if (moraleLoss > 0)
					victim.MoraleChange(-moraleLoss);
				victim.MindControllerId = attack.attacker.Id;

				// Award Panic battle unit kill
				if (!attack.attacker.Statistics.DuplicateEntry(STATUS_PANICKING, victim.Id))
				{
					killStat.status = STATUS_PANICKING;
					if (!victim.IsCosmetic)
					{
						attack.attacker.Statistics.kills.Add(killStat);
					}
				}
			}
			else if (attack.type == BA_MINDCONTROL)
			{
				// Award MC battle unit kill
				if (!attack.attacker.Statistics.DuplicateEntry(STATUS_TURNING, victim.Id))
				{
					killStat.status = STATUS_TURNING;
					if (!victim.IsCosmetic)
					{
						attack.attacker.Statistics.kills.Add(killStat);
					}
				}
				victim.MindControllerId = attack.attacker.Id;
				if (attack.weapon_item.Rules.ConvertToCivilian() && victim.OriginalFaction == FACTION_HOSTILE)
				{
					victim.ConvertToFaction(FACTION_NEUTRAL);
					if (victim.AIModule != null)
					{
						// rewire them to attack hostiles
						victim.AIModule.SetTargetFaction(FACTION_HOSTILE);
					}
				}
				else
				{
					victim.ConvertToFaction(attack.attacker.Faction);
					CalculateLighting(LL_UNITS, victim.Position);
					CalculateFOV(victim.Position); //happens fairly rarely, so do a full recalc for units in range to handle the potential unit visible cache issues.
				}
				victim.RecoverTimeUnits();
				victim.AllowReselect();
				victim.AbortTurn(); // resets unit status to STANDING
				// if all units from either faction are mind controlled - auto-end the mission.
				if (_save.Side == FACTION_PLAYER && Options.allowPsionicCapture)
				{
					_save.BattleGame.AutoEndBattle();
				}
			}
			return true;
		}
		else
		{
			if (Options.allowPsiStrengthImprovement && isPsiRequired)
			{
				victim.AddPsiStrengthExp(); // experience for the victim, not the attacker
			}
			return false;
		}
	}

	/// <summary>Calculate success rate of melee attack action.</summary>
	/// <param name="attack">Type of attack, attacking unit and attack item.</param>
	/// <param name="victim">Attacked unit.</param>
	/// <returns>Value greater than zero mean successful attack.</returns>
	public int MeleeAttackCalculate(BattleActionAttack attack, BattleUnit victim)
	{
		if (victim == null)
			return 0;

		int attackStrength = BattleUnit.GetFiringAccuracy(attack, _save.BattleGame.Mod);
		int defenseStrength = victim.Armor.GetMeleeDodge(victim);
		int arc = GetArcDirection(GetDirectionTo(victim.PositionVexels, attack.attacker.PositionVexels), victim.Direction);
		int defenseStrengthPenalty = Clamp((int)(defenseStrength * (arc * victim.Armor.MeleeDodgeBackPenalty / 4.0f)), 0, Math.Max(0, defenseStrength));

		Rng.RandomState rng = Rng.GlobalRandomState.SubSequence();

		int meleeAttackResult = 0;

		// ModScript::TryMeleeAttackItem: the item script is not ported, its built-in default script (BattleItem.cpp) is applied:
		// "var int r; random.randomRange r 0 99; sub melee_attack_success r; add melee_attack_success attack_strength;
		//  sub melee_attack_success defense_strength; add melee_attack_success defense_strength_penalty; return melee_attack_success;"
		{
			int r = rng.Generate(0, 99);
			meleeAttackResult -= r;
			meleeAttackResult += attackStrength;
			meleeAttackResult -= defenseStrength;
			meleeAttackResult += defenseStrengthPenalty;
		}

		// ModScript::TryMeleeAttackUnit (armor, no default script) not ported: result unmodified

		return meleeAttackResult;
	}

	/// <summary>Attempts a melee attack action.</summary>
	/// <param name="attack">The attack.</param>
	/// <param name="victim">The attacked unit.</param>
	/// <param name="terrainMeleeTilePart">Tile part hit by a terrain melee attack (0 = none).</param>
	/// <returns>Whether it failed or succeeded.</returns>
	public bool MeleeAttack(BattleActionAttack attack, BattleUnit victim, int terrainMeleeTilePart = 0)
	{
		if (terrainMeleeTilePart > 0)
		{
			// terrain melee doesn't miss
			return true;
		}
		if (attack.type != BA_CQB)
		{
			// hit log - new melee attack
			_save.AppendToHitLog(HITLOG_NEW_SHOT, attack.attacker.Faction);

			// to allow melee reactions when attacked from any side, not just from the front
			if (victim != null && Mod.EXTENDED_MELEE_REACTIONS == 2)
			{
				victim.SetMeleeAttackedBy(attack.attacker.Id);
			}
		}

		return MeleeAttackCalculate(attack, victim) > 0;
	}

	/// <summary>Remove the medikit from the game if consumable and empty.</summary>
	public void MedikitRemoveIfEmpty(BattleAction action)
	{
		// remove item if completely used up
		if (action.weapon.Rules.IsConsumable)
		{
			if (action.weapon.PainKillerQuantity == 0 && action.weapon.StimulantQuantity == 0 && action.weapon.HealQuantity == 0)
			{
				_save.RemoveItem(action.weapon);
			}
		}
	}

	/// <summary>Try using medikit heal ability.</summary>
	/// <returns>True if healing can continue.</returns>
	public bool MedikitUse(BattleAction action, BattleUnit target, BattleMediKitAction originalMedikitAction, UnitBodyPart bodyPart)
	{
		if (_save.IsPreview)
		{
			return false;
		}

		var attack = new BattleActionAttack();
		attack.type = action.type;
		attack.attacker = action.actor;
		attack.weapon_item = action.weapon;
		attack.damage_item = action.weapon;

		bool canContinueHealing = true;

		RuleItem rule = action.weapon.Rules;

		BattleMediKitType type = rule.MediKitType;

		action.weapon.SpendHealingItemUse(originalMedikitAction);

		// ModScript::HealUnit (armor) not ported: the script arguments keep their initial values
		int medikitAction = (int)originalMedikitAction;
		int healthRecovery = rule.HealthRecovery;
		int woundRecovery = rule.WoundRecovery;
		int energyRecovery = rule.EnergyRecovery;
		int stunRecovery = rule.StunRecovery;
		int manaRecovery = rule.ManaRecovery;
		int moraleRecovery = rule.MoraleRecovery;
		float painkillerRecovery = (int)(rule.PainKillerRecovery * 100.0f) / 100.0f; // round trip through the int script argument

		// 0 = normal, 1 = heal, 2 = stim, 4 = pain
		if ((medikitAction & (int)BMA_PAINKILLER) != 0)
		{
			target.PainKillers(moraleRecovery, painkillerRecovery);
		}

		if ((medikitAction & (int)BMA_STIMULANT) != 0)
		{
			target.Stimulant(energyRecovery, stunRecovery, manaRecovery);
		}

		if ((medikitAction & (int)BMA_HEAL) != 0)
		{
			if (target.GetFatalWound(bodyPart) != 0)
			{
				// award experience only if healed body part has a fatal wound (to prevent abuse)
				AwardExperience(attack, target, false);
			}

			target.Heal(bodyPart, woundRecovery, healthRecovery);
		}

		_save.BattleGame.PlaySound(action.weapon.Rules.HitSound);

		if (type == BattleMediKitType.BMT_NORMAL) // normal medikit usage, track statistics
		{
			if ((medikitAction & (int)BMA_PAINKILLER) != 0)
			{
				action.actor.Statistics.appliedPainKill++;
			}
			if ((medikitAction & (int)BMA_STIMULANT) != 0)
			{
				action.actor.Statistics.appliedStimulant++;
			}
			if ((medikitAction & (int)BMA_HEAL) != 0)
			{
				action.actor.Statistics.woundsHealed++;
			}

			if (target.Status == STATUS_UNCONSCIOUS && !target.IsOutThresholdExceed)
			{
				if (target.OriginalFaction == FACTION_PLAYER)
				{
					action.actor.Statistics.revivedSoldier++;
				}
				else if (target.OriginalFaction == FACTION_HOSTILE)
				{
					action.actor.Statistics.revivedHostile++;
				}
				else
				{
					action.actor.Statistics.revivedNeutral++;
				}
				// if the unit has revived and has no more wounds, we cannot continue healing
				if (target.FatalWounds == 0)
				{
					canContinueHealing = false;
				}
			}
		}

		// check for casualties, revive unconscious unit (+ change status), re-calc fov & lighting
		UpdateGameStateAfterScript(attack, action.actor.Position);

		return canContinueHealing;
	}

	/// <summary>Executes the skillUseUnit script hook and determines further steps.</summary>
	/// <returns>True if the current action should be continued, false if the action should be cancelled (usually because the script took care of any effects itself).</returns>
	public bool SkillUse(BattleAction action, RuleSkill skill)
	{
		if (_save.IsPreview)
		{
			return false;
		}

		bool continueAction = true;
		bool spendTu = false;
		string message = "";
		bool hasTu = action.HaveTU(ref message);

		// ModScript::SkillUseUnit (skill) not ported: continueAction and spendTu keep their initial values
		_ = skill;

		if (spendTu)
		{
			action.actor.SpendCost(action);
		}

		if (!hasTu && !string.IsNullOrEmpty(message))
		{
			action.result = message;
		}

		return continueAction;
	}

	/// <summary>Tries to conceal a unit. Only works if no unit of another faction is watching.</summary>
	public bool TryConcealUnit(BattleUnit unit)
	{
		foreach (BattleUnit bu in _save.Units)
		{
			if (bu.Faction != unit.Faction && bu.HasVisibleUnit(unit))
			{
				return false;
			}
		}

		unit.TurnsSinceSpotted = 255;
		unit.TurnsLeftSpottedForSnipers = 0;

		return true;
	}

	/// <summary>Validates the melee range between two units.</summary>
	/// <param name="attacker">The attacking unit.</param>
	/// <param name="target">The unit we want to attack.</param>
	/// <param name="dir">Direction to check.</param>
	/// <returns>True when the range is valid.</returns>
	public bool ValidMeleeRange(BattleUnit attacker, BattleUnit target, int dir)
	{
		Position unused = default; // C++ passes no destination (nullptr)
		return ValidMeleeRange(attacker.Position, dir, attacker, target, ref unused);
	}

	/// <summary>Validates the melee range between a tile and a unit.</summary>
	/// <param name="pos">Position to check from.</param>
	/// <param name="direction">Direction to check.</param>
	/// <param name="attacker">The attacking unit.</param>
	/// <param name="target">The unit we want to attack, 0 for any unit.</param>
	/// <param name="dest">Destination position.</param>
	/// <param name="preferEnemy">Prefer enemies over friends (false: the most wounded friend, for medikits).</param>
	/// <returns>True when the range is valid.</returns>
	public bool ValidMeleeRange(Position pos, int direction, BattleUnit attacker, BattleUnit target, ref Position dest, bool preferEnemy = true)
	{
		if (direction < 0 || direction > 7)
		{
			return false;
		}
		var potentialTargets = new List<BattleUnit>();
		BattleUnit chosenTarget = null;
		Position p = default;
		int size = attacker.Armor.Size - 1;
		int meleeOriginVoxelVerticalOffset = attacker.Armor.MeleeOriginVoxelVerticalOffset; // add some sanity checks or trust the modders?
		Pathfinding.DirectionToVector(direction, ref p);
		for (int x = 0; x <= size; ++x)
		{
			for (int y = 0; y <= size; ++y)
			{
				Tile origin = _save.GetTile(pos + new Position(x, y, 0));
				Tile targetTile = _save.GetTile(pos + new Position(x, y, 0) + p);

				if (targetTile != null && origin != null)
				{
					Tile aboveTargetTile = _save.GetAboveTile(targetTile);
					Tile belowTargetTile = _save.GetBelowTile(targetTile);

					if (origin.TerrainLevel <= -16 && aboveTargetTile != null && !aboveTargetTile.HasNoFloor(_save))
					{
						targetTile = aboveTargetTile;
					}
					else if (belowTargetTile != null && targetTile.HasNoFloor(_save) && targetTile.Unit == null && belowTargetTile.TerrainLevel <= -16)
					{
						targetTile = belowTargetTile;
					}
					if (targetTile.Unit != null)
					{
						if (target == null || targetTile.Unit == target)
						{
							Position originVoxel = origin.Position.ToVoxel()
								+ new Position(8, 8, attacker.Height + attacker.FloatHeight - 4 - origin.TerrainLevel + meleeOriginVoxelVerticalOffset);
							Position targetVoxel = default;
							if (CanTargetUnit(ref originVoxel, targetTile, ref targetVoxel, attacker, false))
							{
								dest = targetTile.Position;
								if (target != null)
								{
									return true;
								}
								else
								{
									potentialTargets.Add(targetTile.Unit);
								}
							}
						}
					}
				}
			}
		}

		foreach (BattleUnit bu in potentialTargets)
		{
			// if there's actually something THERE, we'll chalk this up as a success.
			if (chosenTarget == null)
			{
				chosenTarget = bu;
			}
			// but if there's a target of a different faction, we'll prioritize them.
			else if ((preferEnemy && bu.Faction != attacker.Faction)
			// or, if we're using a medikit, prioritize whichever friend is wounded the most.
			|| (!preferEnemy && bu.Faction == attacker.Faction &&
			bu.FatalWounds > chosenTarget.FatalWounds))
			{
				chosenTarget = bu;
			}
		}

		if (chosenTarget != null)
		{
			dest = chosenTarget.Position;
		}

		return chosenTarget != null;
	}

	/// <summary>Validates the terrain melee range.</summary>
	public bool ValidTerrainMeleeRange(BattleAction action)
	{
		if (Mod.EXTENDED_TERRAIN_MELEE <= 0)
		{
			// turned off
			return false;
		}

		action.terrainMeleeTilePart = 0;

		if (action.weapon != null)
		{
			RuleItem wRule = action.weapon.Rules;
			if (wRule.BattleType == BT_MELEE)
			{
				// check primary damage type
				if (wRule.DamageType.ToTile == 0.0) return false;
			}
			else
			{
				// check secondary damage type
				if (wRule.MeleeType.ToTile == 0.0) return false;
			}
		}

		Position pos = action.actor.Position;
		int direction = action.actor.Direction;
		BattleUnit attacker = action.actor;

		if (direction < 0 || direction > 7)
		{
			return false;
		}
		if (direction % 2 != 0)
		{
			// diagonal directions are not supported
			return false;
		}
		Position p = default;
		Pathfinding.DirectionToVector(direction, ref p);

		Tile originTile = _save.GetTile(pos);
		Tile originTile2 = originTile;
		if (originTile != null && originTile.TerrainLevel <= -16)
		{
			// if we are on the upper part of stairs, target one tile above
			pos += new Position(0, 0, 1);
			originTile = _save.GetTile(pos);
		}
		Tile neighbouringTile = _save.GetTile(pos + p);
		Tile neighbouringTile2 = null;
		int size = attacker.Armor.Size;
		if (size > 1)
		{
			if (direction == 0)
			{
				// North
				originTile2 = _save.GetTile(pos + new Position(1, 0, 0));
				neighbouringTile2 = _save.GetTile(pos + p + new Position(1, 0, 0));
			}
			else if (direction == 2)
			{
				// East
				neighbouringTile = _save.GetTile(pos + p + new Position(1, 0, 0));
				neighbouringTile2 = _save.GetTile(pos + p + new Position(1, 1, 0));
			}
			else if (direction == 4)
			{
				// South
				neighbouringTile = _save.GetTile(pos + p + new Position(0, 1, 0));
				neighbouringTile2 = _save.GetTile(pos + p + new Position(1, 1, 0));
			}
			else if (direction == 6)
			{
				// West
				originTile2 = _save.GetTile(pos + new Position(0, 1, 0));
				neighbouringTile2 = _save.GetTile(pos + p + new Position(0, 1, 0));
			}
			if (neighbouringTile2 == null || originTile2 == null)
			{
				return false;
			}
		}
		if (originTile != null && neighbouringTile != null)
		{
			static bool SetTarget(Tile tt, TilePart tp, BattleAction aa, int dir = -1)
			{
				MapData obj = tt.GetMapData(tp);
				if (obj != null)
				{
					if (dir > -1 && tp == O_OBJECT)
					{
						int bigWall = obj.BigWall;
						if (dir == 0 /*north*/ && bigWall != BIGWALLNORTH && bigWall != BIGWALLWESTANDNORTH) return false;
						if (dir == 2 /*east */ && bigWall != BIGWALLEAST && bigWall != BIGWALLEASTANDSOUTH) return false;
						if (dir == 4 /*south*/ && bigWall != BIGWALLSOUTH && bigWall != BIGWALLEASTANDSOUTH) return false;
						if (dir == 6 /*west */ && bigWall != BIGWALLWEST && bigWall != BIGWALLWESTANDNORTH) return false;
					}
					if (tp != O_OBJECT && !obj.IsDoor && !obj.IsUFODoor && tt.GetTUCost((int)tp, MovementType.MT_WALK) != Pathfinding.INVALID_MOVE_COST)
					{
						// it is possible to walk through this (rubble) wall... no need to attack it
						return false;
					}
					bool isHighEnough = false;
					for (int i = Mod.EXTENDED_TERRAIN_MELEE; i < 12; ++i)
					{
						if (obj.GetLoftID(i) > 0)
						{
							isHighEnough = true;
							break;
						}
					}
					if (isHighEnough)
					{
						aa.target = tt.Position;
						aa.terrainMeleeTilePart = (int)tp;
						return true;
					}
				}
				return false;
			}

			if (SetTarget(originTile, O_OBJECT, action, direction))
			{
				// All directions: target the object (marked as big wall) on the current tile
				return true;
			}
			if (size > 1)
			{
				if (SetTarget(originTile2, O_OBJECT, action, direction))
				{
					// All directions
					return true;
				}
			}

			if (direction == 0 && SetTarget(originTile, O_NORTHWALL, action))
			{
				// North: target the north wall of the same tile
				return true;
			}
			else if (direction == 2 && SetTarget(neighbouringTile, O_WESTWALL, action))
			{
				// East: target the west wall of the neighbouring tile
				return true;
			}
			else if (direction == 4 && SetTarget(neighbouringTile, O_NORTHWALL, action))
			{
				// South: target the north wall of the neighbouring tile
				return true;
			}
			else if (direction == 6 && SetTarget(originTile, O_WESTWALL, action))
			{
				// West: target the west wall of the same tile
				return true;
			}
			if (size > 1)
			{
				if (direction == 0 && SetTarget(originTile2, O_NORTHWALL, action))
				{
					// North
					return true;
				}
				else if (direction == 2 && SetTarget(neighbouringTile2, O_WESTWALL, action))
				{
					// East
					return true;
				}
				else if (direction == 4 && SetTarget(neighbouringTile2, O_NORTHWALL, action))
				{
					// South
					return true;
				}
				else if (direction == 6 && SetTarget(originTile2, O_WESTWALL, action))
				{
					// West
					return true;
				}
			}

			if (SetTarget(neighbouringTile, O_OBJECT, action))
			{
				// All directions: target the object on the neighbouring tile
				return true;
			}
			if (size > 1)
			{
				if (SetTarget(neighbouringTile2, O_OBJECT, action))
				{
					// All directions
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>Validates a throw action.</summary>
	/// <param name="action">The action to validate.</param>
	/// <param name="originVoxel">The origin point of the action.</param>
	/// <param name="targetVoxel">The target point of the action.</param>
	/// <param name="depth">Battlescape depth.</param>
	/// <param name="curve">The curvature of the throw (C++ optional pointer: written on success).</param>
	/// <param name="voxelType">The type of voxel at which this parabola terminates (C++ optional pointer).</param>
	/// <param name="forced">Accept the first curve regardless of where it lands.</param>
	/// <returns>Validity of action.</returns>
	public bool ValidateThrow(BattleAction action, Position originVoxel, Position targetVoxel, int depth, ref double curve, ref int voxelType, bool forced = false)
	{
		bool foundCurve = false;
		double curvature = 0.5;
		if (action.type == BA_THROW)
		{
			curvature = Math.Max(0.48, 1.73 / Math.Sqrt(Math.Sqrt((double)(action.actor.BaseStats.strength) / (double)(action.weapon.TotalWeight))) + (action.actor.IsKneeled ? 0.1 : 0.0));
		}
		else
		{
			// arcing projectile weapons assume a fixed strength and weight.(70 and 10 respectively)
			// curvature should be approximately 1.06358350461 at this point.
			curvature = 1.73 / Math.Sqrt(Math.Sqrt(70.0 / 10.0)) + (action.actor.IsKneeled ? 0.1 : 0.0);
		}

		Tile targetTile = _save.GetTile(action.target);
		Position targetPos = targetVoxel.ToTile();
		// object blocking - can't throw here
		if (action.type == BA_THROW
			&& targetTile != null
			&& targetTile.GetMapData(O_OBJECT) != null
			&& targetTile.GetMapData(O_OBJECT).GetTUCost(MovementType.MT_WALK) == Pathfinding.INVALID_MOVE_COST
			&& !(targetTile.IsBigWall
			&& (targetTile.GetMapData(O_OBJECT).BigWall < 1
			|| targetTile.GetMapData(O_OBJECT).BigWall > 3)))
		{
			return false;
		}
		// out of range - can't throw here
		if (ProjectileFlyBState.ValidThrowRange(action, originVoxel, targetTile, depth) == false)
		{
			return false;
		}

		// thows should be around 10 tiles far, make one allocation that fit 99% cases with some margin
		var trajectory = new List<Position>(16 * 20);
		// we try 8 different curvatures to try and reach our goal.
		int test = (int)V_OUTOFBOUNDS;
		while (!foundCurve && curvature < 5.0)
		{
			trajectory.Clear();
			test = CalculateParabolaVoxel(originVoxel, targetVoxel, true, trajectory, action.actor, curvature, new Position(0, 0, 0));
			//position that item hit
			Position hitPos = (trajectory[^1] + new Position(0, 0, 1)).ToTile();
			//position where item will land
			Position tilePos = Projectile.GetPositionFromEnd(trajectory, Projectile.ItemDropVoxelOffset).ToTile();
			if (forced || (test != (int)V_OUTOFBOUNDS && tilePos == targetPos))
			{
				voxelType = test;
				foundCurve = true;
			}
			else
			{
				curvature += 0.5;
				if (test != (int)V_OUTOFBOUNDS && action.actor.Faction == FACTION_PLAYER) //obstacle indicator is only for player
				{
					Tile hitTile = _save.GetTile(hitPos);
					hitTile?.SetObstacle(test);
				}
			}
		}
		if (curvature >= 5.0)
		{
			return false;
		}
		curve = curvature;

		return true;
	}

	/// <summary>
	/// Update game state after script hook execution. We need check state of units
	/// and handle special cases like deaths or unit tranformation.
	/// For now we assume that Light and FOV is affected only in small area,
	/// this mean we could glitch if multiple units are affected by script logic.
	/// </summary>
	/// <param name="battleActionAttack">Data of action that triggeted script hook.</param>
	/// <param name="pos">Postion to update light and Fov, can be invalid if we do not update light.</param>
	public void UpdateGameStateAfterScript(BattleActionAttack battleActionAttack, Position pos)
	{
		_save.BattleGame.CheckForCasualties(null, battleActionAttack, false, false);

		_save.ReviveUnconsciousUnits(true);

		_save.BattleGame.ConvertInfected();

		if (pos != invalid)
		{
			// limit area of the following calls to the Position pos
			CalculateLighting(LL_ITEMS, pos, 2, true);
			CalculateFOV(pos, 1, false);
		}
	}
}
