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
// Ported from: src/Battlescape/AIModule.h, src/Battlescape/AIModule.cpp
#nullable disable

using static Acom.AIMode;
using static Acom.BattleActionMove;
using static Acom.BattleActionType;
using static Acom.BattleMediKitAction;
using static Acom.BattleMediKitType;
using static Acom.BattleType;
using static Acom.UnitBodyPart;
using static Acom.UnitFaction;
using static Acom.UnitStatus;
using static Acom.VoxelType;

namespace Acom;

public enum AIMode : uint
{
	AI_PATROL = 0,
	AI_AMBUSH = 1,
	AI_COMBAT = 2,
	AI_ESCAPE = 3,
}

public enum AIAttackWeight
{
	/// <summary>Base scale of attack weights.</summary>
	AIW_SCALE = 100,
	AIW_IGNORED = 0,
}

/// <summary>This class is used by the BattleUnit AI.</summary>
public class AIModule
{
	private SavedBattleGame _save;
	private BattleUnit _unit;
	private BattleUnit _aggroTarget;
	private int _knownEnemies, _visibleEnemies, _spottingEnemies;
	private int _escapeTUs, _ambushTUs;
	private bool _weaponPickedUp;
	private bool _rifle, _melee, _blaster, _grenade;
	private bool _traceAI, _didPsi;
	private AIMode _AIMode;
	private int _intelligence, _closestDist;
	private Node _fromNode, _toNode;
	private bool _foundBaseModuleToDestroy;
	private List<int> _reachable = new(), _reachableWithAttack = new(), _wasHitBy = new();
	private BattleActionType _reserve;
	private UnitFaction _targetFaction;

	private BattleAction _escapeAction, _ambushAction, _attackAction, _patrolAction, _psiAction;

	/// <summary>Sets up a BattleAIState.</summary>
	/// <param name="save">Pointer to the battle game.</param>
	/// <param name="unit">Pointer to the unit.</param>
	/// <param name="node">Pointer to the node the unit originates from.</param>
	public AIModule(SavedBattleGame save, BattleUnit unit, Node node)
	{
		_save = save;
		_unit = unit;
		_aggroTarget = null;
		_knownEnemies = 0;
		_visibleEnemies = 0;
		_spottingEnemies = 0;
		_escapeTUs = 0;
		_ambushTUs = 0;
		_weaponPickedUp = false;
		_rifle = false;
		_melee = false;
		_blaster = false;
		_grenade = false;
		_didPsi = false;
		_AIMode = AI_PATROL;
		_closestDist = 100;
		_fromNode = node;
		_toNode = null;
		_foundBaseModuleToDestroy = false;

		_traceAI = Options.traceAI;

		_reserve = BA_NONE;
		_intelligence = _unit.Intelligence;
		_escapeAction = new BattleAction();
		_ambushAction = new BattleAction();
		_attackAction = new BattleAction();
		_patrolAction = new BattleAction();
		_psiAction = new BattleAction();
		_targetFaction = FACTION_PLAYER;
		if (_unit.OriginalFaction == FACTION_NEUTRAL)
		{
			_targetFaction = FACTION_HOSTILE;
		}
	}

	/// <summary>Sets the target faction.</summary>
	public void SetTargetFaction(UnitFaction f)
	{
		_targetFaction = f;
	}

	/// <summary>Resets the unsaved AI state.</summary>
	public void Reset()
	{
		// these variables are not saved in save() and also not initiated in think()
		_escapeTUs = 0;
		_ambushTUs = 0;
	}

	/// <summary>Loads the AI state from a YAML file.</summary>
	/// <param name="reader">YAML node.</param>
	public void Load(YamlNodeReader reader)
	{
		int fromNodeID = reader["fromNode"].ReadVal(-1);
		int toNodeID = reader["toNode"].ReadVal(-1);
		_AIMode = reader["AIMode"].ReadVal(AI_PATROL);
		reader.TryRead("wasHitBy", ref _wasHitBy);
		reader.TryRead("weaponPickedUp", ref _weaponPickedUp);
		reader.TryRead("targetFaction", ref _targetFaction);

		// TODO: Figure out why AI are sometimes left with junk nodes
		if (fromNodeID >= 0 && fromNodeID < _save.Nodes.Count)
		{
			_fromNode = _save.Nodes[fromNodeID];
		}
		if (toNodeID >= 0 && toNodeID < _save.Nodes.Count)
		{
			_toNode = _save.Nodes[toNodeID];
		}
	}

	/// <summary>Saves the AI state to a YAML file.</summary>
	/// <param name="writer">YAML node.</param>
	public void Save(YamlNodeWriter writer)
	{
		writer.SetAsMap();
		writer.SetFlowStyle();
		writer.Write("fromNode", _fromNode != null ? _fromNode.ID : -1);
		writer.Write("toNode", _toNode != null ? _toNode.ID : -1);
		writer.Write("AIMode", (int)_AIMode);
		writer.Write("wasHitBy", _wasHitBy);
		if (_weaponPickedUp)
			writer.Write("weaponPickedUp", _weaponPickedUp);
		if (_unit.OriginalFaction == FACTION_HOSTILE && _unit.Faction == FACTION_NEUTRAL && _targetFaction == FACTION_HOSTILE)
		{
			writer.Write("targetFaction", _targetFaction);
		}
	}

	/// <summary>C++ prints bools as 0/1 in log lines.</summary>
	private static int LogBool(bool b) => b ? 1 : 0;

	/// <summary>Name of an AI mode for the trace log.</summary>
	private static string AIModeName(AIMode mode) => mode switch
	{
		AI_PATROL => "Patrol",
		AI_AMBUSH => "Ambush",
		AI_COMBAT => "Combat",
		AI_ESCAPE => "Escape",
		_ => "",
	};

	/// <summary>Mindless charge strategy. For mindless units. Consists of running around and charging nearest visible enemy.</summary>
	/// <param name="action">(possible) AI action to execute after thinking is done.</param>
	private void Dont_think(BattleAction action)
	{
		_melee = false;
		action.weapon = _unit.GetUtilityWeapon(BT_MELEE);

		if (_traceAI)
		{
			Logger.Info($"LEEROY: Unit {_unit.Id} of type {_unit.Type} is Leeroy...");
		}
		if (action.weapon != null)
		{
			if (action.weapon.Rules.BattleType == BT_MELEE)
			{
				if (_save.CanUseWeapon(action.weapon, _unit, false, BA_HIT))
				{
					_melee = true;
				}
			}
			else
			{
				action.weapon = null;
			}
		}

		bool canRun = _melee && _unit.Armor.AllowsRunning(false) && _unit.Energy > _unit.BaseStats.stamina * 0.4f;
		int visibleEnemiesToAttack = SelectNearestTargetLeeroy(canRun);
		if (_traceAI)
		{
			Logger.Info($"LEEROY: visibleEnemiesToAttack: {visibleEnemiesToAttack} _melee: {LogBool(_melee)}{(canRun ? " run" : "")}");
		}
		if ((visibleEnemiesToAttack > 0) && _melee)
		{
			if (_traceAI)
			{
				Logger.Info("LEEROY: LEEROYIN' at someone!");
			}
			MeleeActionLeeroy(canRun);
			action.type = _attackAction.type;
			action.run = _attackAction.run;
			action.target = _attackAction.target;
			// if this is a firepoint action, set our facing.
			action.finalFacing = _attackAction.finalFacing;
			action.UpdateTU();
		}
		else
		{
			if (_traceAI)
			{
				Logger.Info("LEEROY: No one to LEEROY!, patrolling...");
			}
			SetupPatrol();
			_unit.Charging = null;
			_reserve = BA_NONE;
			action.type = _patrolAction.type;
			action.target = _patrolAction.target;
		}
	}

	/// <summary>Tries to use self-target medikit if needed and desired (used for AI).</summary>
	/// <returns>Was it used?</returns>
	public bool Medikit_think(BattleMediKitType healOrStim)
	{
		// 1. sanity checks, division by zero
		BattleUnit self = _unit;

		if (self.BaseStats.stamina <= 0 || self.BaseStats.health <= 0)
		{
			return false;
		}

		// 2. quick unit checks (without RNG)
		int totalWounds = self.FatalWounds;
		int percentHealthLeft = Clamp((self.Health - self.Stunlevel) * 100 / self.BaseStats.health, 0, 100);
		int percentEnergyLeft = Clamp(self.Energy * 100 / self.BaseStats.stamina, 0, 100);

		if (healOrStim == BMT_HEAL)
		{
			if (totalWounds <= 0)
				return false;
		}
		else if (healOrStim == BMT_STIMULANT)
		{
			if (self.Stunlevel <= 0 && percentEnergyLeft >= 40)
				return false;
		}
		else
		{
			// unsupported medikit type
			return false;
		}

		// 3. quick item checks
		var usableMedikits = new List<BattleItem>();

		foreach (BattleItem item in self.Inventory)
		{
			RuleItem itemRule = item.Rules;
			if (itemRule.BattleType == BT_MEDIKIT &&
				(itemRule.MediKitType == healOrStim || itemRule.MediKitType == BMT_NORMAL) &&
				itemRule.AllowTargetSelf)
			{
				if (_save.Turn < itemRule.GetAIUseDelay(_save.Mod))
				{
					// can't use it yet, too soon
					continue;
				}
				usableMedikits.Add(item);
			}
		}
		if (usableMedikits.Count == 0)
		{
			// no compatible medikits available
			return false;
		}

		// 4. detailed unit checks (with RNG)
		bool wantsToHeal = false;
		bool wantsToStimStun = false;
		bool wantsToStimEnergy = false;

		if (healOrStim == BMT_HEAL)
		{
			if (totalWounds > 0)
			{
				if (self.Stunlevel + totalWounds >= self.Health)
				{
					// going to die or pass out unless we do something, so do something!
					wantsToHeal = true;
				}
				else
				{
					//  0% health left = 120% chance to heal
					// 15% health left =  60% chance to heal
					// 30% health left =   0% chance to heal (actually 5% chance because of random heal wish)
					int chanceToHeal = 120 - (percentHealthLeft * 4);
					if (chanceToHeal <= 0)
					{
						// 5% for random heal wish (it's not urgent, but you know damage accumulates over time)
						chanceToHeal = 5;
					}
					wantsToHeal = Rng.Percent(chanceToHeal);
				}
			}
			if (!wantsToHeal)
			{
				return false;
			}
		}
		else if (healOrStim == BMT_STIMULANT)
		{
			// 1. do we want to decrease stun level?
			if (self.Stunlevel > 0)
			{
				if (self.Stunlevel + totalWounds >= self.Health)
				{
					// going to die or pass out unless we do something, so do something!
					wantsToStimStun = true;
				}
				else
				{
					//  0% health left = 140% chance to stim
					// 10% health left =  70% chance to stim
					// 20% health left =   0% chance to stim
					int chanceToStim1 = 140 - (percentHealthLeft * 7);
					wantsToStimStun = chanceToStim1 > 0 ? Rng.Percent(chanceToStim1) : false;
				}
			}
			// 2. do we want to increase energy?
			if (percentEnergyLeft < 40)
			{
				//  0% energy left = 120% chance to stim
				// 20% energy left =  60% chance to stim
				// 40% energy left =   0% chance to stim
				int chanceToStim2 = 120 - (percentEnergyLeft * 3);
				wantsToStimEnergy = Rng.Percent(chanceToStim2);
			}
			if (!wantsToStimStun && !wantsToStimEnergy)
			{
				return false;
			}
		}

		// 5. let's do it
		bool used = false;

		foreach (BattleItem medikit in usableMedikits)
		{
			RuleItem medikitRule = medikit.Rules;
			{
				if ((wantsToHeal && medikit.HealQuantity > 0) ||
					(wantsToStimStun && medikit.StimulantQuantity > 0 && medikitRule.StunRecovery > 0) ||
					(wantsToStimEnergy && medikit.StimulantQuantity > 0 && medikitRule.EnergyRecovery > 0))
				{
					var medikitAction = new BattleAction();
					{
						medikitAction.weapon = medikit;
						medikitAction.type = BA_USE;
						medikitAction.actor = self;

						medikitAction.UpdateTU();

						// yes, hardcoded 4 TUs
						// AI throwing grenades does that for decades and nobody cares, so calm down
						// also, AI pays this cost each time, even if using the same medikit multiple times in a row
						medikitAction.Time += 4; // 4TUs for picking up the medikit

						// sigh, modders...
						//medikitAction.Health = 0;
						//medikitAction.Stun = 0;
					}
					if (!medikitAction.SpendTU())
					{
						// not enough TUs, try next item
						continue;
					}
					else
					{
						switch (healOrStim)
						{
							case BMT_HEAL:
								if (_traceAI)
								{
									Logger.Info($"  Using medikit (heal). TU*/HP/Stun/Wounds: {self.TimeUnits}/{self.Health}/{self.Stunlevel}/{totalWounds}");
								}
								for (int i = 0; i < (int)BODYPART_MAX; ++i)
								{
									if (self.GetFatalWound((UnitBodyPart)i) != 0)
									{
										_save.TileEngine.MedikitUse(medikitAction, self, BMA_HEAL, (UnitBodyPart)i);
										_save.TileEngine.MedikitRemoveIfEmpty(medikitAction);
										used = true;
										break;
									}
								}
								break;
							case BMT_STIMULANT:
								if (_traceAI)
								{
									if (wantsToStimStun)
									{
										Logger.Info($"  Using medikit (-stun). TU*/HP/Stun/Wounds: {self.TimeUnits}/{self.Health}/{self.Stunlevel}/{totalWounds}");
									}
									else
									{
										Logger.Info($"  Using medikit (+energy). TU*/Energy: {self.TimeUnits}/{self.Energy}");
									}
								}
								_save.TileEngine.MedikitUse(medikitAction, self, BMA_STIMULANT, BODYPART_TORSO);
								_save.TileEngine.MedikitRemoveIfEmpty(medikitAction);
								used = true;
								break;
							case BMT_PAINKILLER:
							case BMT_NORMAL:
								// not supported
								break;
						}
					}
				}
			}
			if (used)
			{
				// only one use per attempt
				break;
			}
		}

		// 6. if we used something, let's try again
		return used;
	}

	/// <summary>Runs any code the state needs to keep updating every AI cycle.</summary>
	/// <param name="action">(possible) AI action to execute after thinking is done.</param>
	public void Think(BattleAction action)
	{
		action.type = BA_RETHINK;
		action.actor = _unit;
		action.weapon = _unit.GetMainHandWeapon(false);
		_attackAction.diff = _save.BattleState.Game.SavedGame.DifficultyCoefficient;
		_attackAction.actor = _unit;
		_attackAction.run = false;
		_attackAction.weapon = action.weapon;
		_attackAction.number = action.number;
		_escapeAction.number = action.number;
		_knownEnemies = CountKnownTargets();
		_visibleEnemies = SelectNearestTarget();
		_spottingEnemies = GetSpottingUnits(_unit.Position);
		_melee = (_unit.GetUtilityWeapon(BT_MELEE) != null);
		_rifle = false;
		_blaster = false;
		_reachable = _save.Pathfinding.FindReachable(_unit, new BattleActionCost());
		_wasHitBy.Clear();
		_foundBaseModuleToDestroy = false;

		if (_unit.Charging != null && _unit.Charging.IsOut)
		{
			_unit.Charging = null;
		}

		if (_traceAI)
		{
			if (_unit.Faction == FACTION_HOSTILE)
			{
				Logger.Info($"Unit has {_visibleEnemies}/{_knownEnemies} known enemies visible, {_spottingEnemies} of whom are spotting him. ");
			}
			else
			{
				Logger.Info($"Civilian Unit has {_visibleEnemies} enemies visible, {_spottingEnemies} of whom are spotting him. ");
			}
			Logger.Info($"Currently using {AIModeName(_AIMode)} behaviour");
		}

		if (_unit.IsLeeroyJenkins)
		{
			Dont_think(action);
			return;
		}

		if (action.weapon != null)
		{
			RuleItem rule = action.weapon.Rules;
			if (_save.CanUseWeapon(action.weapon, _unit, false, BA_NONE)) // Note: ammo is not checked here
			{
				if (rule.BattleType == BT_FIREARM)
				{
					if (action.weapon.CurrentWaypoints != 0)
					{
						_blaster = true;
						_reachableWithAttack = _save.Pathfinding.FindReachable(_unit, new BattleActionCost(BA_AIMEDSHOT, _unit, action.weapon));
					}
					else
					{
						_rifle = true;
						_reachableWithAttack = _save.Pathfinding.FindReachable(_unit, new BattleActionCost(BA_SNAPSHOT, _unit, action.weapon));
					}
				}
				else if (rule.BattleType == BT_MELEE)
				{
					_melee = true;
					_reachableWithAttack = _save.Pathfinding.FindReachable(_unit, new BattleActionCost(BA_HIT, _unit, action.weapon));
				}
			}
			else
			{
				action.weapon = null;
			}
		}

		BattleItem grenadeItem = _unit.GetGrenadeFromBelt(_save);
		_grenade = grenadeItem != null;

		if (_spottingEnemies != 0 && _escapeTUs == 0)
		{
			SetupEscape();
		}

		if (_knownEnemies != 0 && !_melee && _ambushTUs == 0)
		{
			SetupAmbush();
		}

		SetupAttack();
		SetupPatrol();

		if (_psiAction.type != BA_NONE && !_didPsi && _save.Turn >= _psiAction.weapon.Rules.GetAIUseDelay(_save.Mod))
		{
			_didPsi = true;
			action.type = _psiAction.type;
			action.target = _psiAction.target;
			action.number -= 1;
			action.weapon = _psiAction.weapon;
			action.UpdateTU();
			return;
		}
		else
		{
			_didPsi = false;
		}

		bool evaluate = false;

		switch (_AIMode)
		{
			case AI_PATROL:
				evaluate = _spottingEnemies != 0 || _visibleEnemies != 0 || _knownEnemies != 0 || Rng.Percent(10);
				break;
			case AI_AMBUSH:
				evaluate = (!_rifle || _ambushTUs == 0 || _visibleEnemies != 0);
				break;
			case AI_COMBAT:
				evaluate = (_attackAction.type == BA_RETHINK);
				break;
			case AI_ESCAPE:
				evaluate = (_spottingEnemies == 0 || _knownEnemies == 0);
				break;
		}

		if (_weaponPickedUp)
		{
			evaluate = true;
			_weaponPickedUp = false;
		}
		else if (_spottingEnemies > 2
			|| _unit.Health < 2 * _unit.BaseStats.health / 3)
		{
			evaluate = true;
		}

		if (_save.IsCheating && _AIMode != AI_COMBAT)
		{
			evaluate = true;
		}

		if (evaluate)
		{
			EvaluateAIMode();
			if (_traceAI)
			{
				Logger.Info($"Re-Evaluated, now using {AIModeName(_AIMode)} behaviour");
			}
		}

		_reserve = BA_NONE;

		switch (_AIMode)
		{
			case AI_ESCAPE:
				_unit.Charging = null;
				action.type = _escapeAction.type;
				action.target = _escapeAction.target;
				// end this unit's turn.
				action.finalAction = true;
				// ignore new targets.
				action.desperate = true;
				// if armor allow runing then run way from there.
				action.run = _escapeAction.run;
				// spin 180 at the end of your route.
				_unit.SetHiding(true);
				break;
			case AI_PATROL:
				_unit.Charging = null;
				if (action.weapon != null && action.weapon.Rules.BattleType == BT_FIREARM)
				{
					switch (_unit.Aggression)
					{
						case 0:
							_reserve = BA_AIMEDSHOT;
							break;
						case 1:
							_reserve = BA_AUTOSHOT;
							break;
						case 2:
							_reserve = BA_SNAPSHOT;
							break;
						default:
							break;
					}
				}
				action.type = _patrolAction.type;
				action.target = _patrolAction.target;
				break;
			case AI_COMBAT:
				action.type = _attackAction.type;
				action.target = _attackAction.target;
				// this may have changed to a grenade.
				action.weapon = _attackAction.weapon;
				if (action.weapon != null && action.type == BA_THROW && action.weapon.Rules.IsGrenadeOrProxy)
				{
					_unit.SpendCost(_unit.GetActionTUs(BA_PRIME, action.weapon));
					_unit.SpendTimeUnits(4);
				}
				// if this is a firepoint action, set our facing.
				action.finalFacing = _attackAction.finalFacing;
				action.UpdateTU();
				// if this is a "find fire point" action, don't increment the AI counter.
				if (action.type == BA_WALK && _rifle && _unit.Armor.AllowsMoving()
					// so long as we can take a shot afterwards.
					&& new BattleActionCost(BA_SNAPSHOT, _unit, action.weapon).HaveTU())
				{
					action.number -= 1;
				}
				else if (action.type == BA_LAUNCH)
				{
					action.waypoints = new List<Position>(_attackAction.waypoints);
				}
				else if (action.type == BA_AIMEDSHOT || action.type == BA_AUTOSHOT)
				{
					action.kneel = _unit.Armor.AllowsKneeling(false);
				}
				break;
			case AI_AMBUSH:
				_unit.Charging = null;
				action.type = _ambushAction.type;
				action.target = _ambushAction.target;
				// face where we think our target will appear.
				action.finalFacing = _ambushAction.finalFacing;
				// end this unit's turn.
				action.finalAction = true;
				action.kneel = _unit.Armor.AllowsKneeling(false);
				break;
			default:
				break;
		}

		if (action.type == BA_WALK)
		{
			// if we're moving, we'll have to re-evaluate our escape/ambush position.
			if (action.target != _unit.Position)
			{
				_escapeTUs = 0;
				_ambushTUs = 0;
			}
			else
			{
				action.type = BA_NONE;
			}
		}
	}

	/// <summary>Sets the "was hit" flag to true.</summary>
	public void SetWasHitBy(BattleUnit attacker)
	{
		if (attacker.Faction != _unit.Faction && !GetWasHitBy(attacker.Id))
			_wasHitBy.Add(attacker.Id);
	}

	/// <summary>Sets the "unit picked up a weapon" flag.</summary>
	public void SetWeaponPickedUp()
	{
		_weaponPickedUp = true;
	}

	/// <summary>Gets whether the unit was hit.</summary>
	/// <returns>If it was hit.</returns>
	public bool GetWasHitBy(int attacker) => _wasHitBy.Contains(attacker);

	/// <summary>Set start node.</summary>
	public void SetStartNode(Node node)
	{
		_fromNode = node;
	}

	/// <summary>
	/// Sets up a patrol action.
	/// this is mainly going from node to node, moving about the map.
	/// handles node selection, and fills out the _patrolAction with useful data.
	/// </summary>
	public void SetupPatrol()
	{
		_patrolAction.ClearTU();
		if (_toNode != null && _unit.Position == _toNode.Position)
		{
			if (_traceAI)
			{
				Logger.Info("Patrol destination reached!");
			}
			// destination reached
			// head off to next patrol node
			_fromNode = _toNode;
			FreePatrolTarget();
			_toNode = null;
			// take a peek through window before walking to the next node
			int dir = _save.TileEngine.FaceWindow(_unit.Position);
			if (dir != -1 && dir != _unit.Direction)
			{
				_unit.LookAt(dir);
				while (_unit.Status == STATUS_TURNING)
				{
					_unit.Turn();
				}
			}
		}

		if (_fromNode == null)
		{
			// assume closest node as "from node"
			// on same level to avoid strange things, and the node has to match unit size or it will freeze
			int closest = 1000000;
			foreach (Node node in _save.Nodes)
			{
				if (node.IsDummy)
				{
					continue;
				}
				int d = Position.DistanceSq(_unit.Position, node.Position);
				if (_unit.Position.z == node.Position.z
					&& d < closest
					&& ((node.Type & Node.TYPE_SMALL) == 0 || _unit.Armor.Size == 1))
				{
					_fromNode = node;
					closest = d;
				}
			}
		}
		int triesLeft = 5;

		while (_toNode == null && triesLeft != 0)
		{
			triesLeft--;
			// look for a new node to walk towards
			bool scout = true;
			if (_save.MissionType != "STR_BASE_DEFENSE")
			{
				// after turn 20 or if the morale is low, everyone moves out the UFO and scout
				// also anyone standing in fire should also probably move
				if (_save.IsCheating || _fromNode == null || _fromNode.Rank == 0 ||
					(_save.GetTile(_unit.Position) != null && _save.GetTile(_unit.Position).Fire != 0))
				{
					scout = true;
				}
				else
				{
					scout = false;
				}
			}

			// in base defense missions, the smaller aliens walk towards target nodes - or if there, shoot objects around them
			else if (_unit.Armor.Size == 1 && _unit.OriginalFaction == FACTION_HOSTILE &&
					_attackAction.weapon != null &&
					_attackAction.weapon.Rules.AccuracySnap != 0 &&
					!_attackAction.weapon.Rules.ArcingShot &&
					_attackAction.weapon.GetAmmoForAction(BA_SNAPSHOT) != null &&
					!_attackAction.weapon.GetAmmoForAction(BA_SNAPSHOT).Rules.ArcingShot &&
					_attackAction.weapon.GetAmmoForAction(BA_SNAPSHOT).Rules.DamageType.IsDirect &&
					_attackAction.weapon.GetAmmoForAction(BA_SNAPSHOT).Rules.DamageType.ToTile > 0.01f)
			{
				// can i shoot an object?
				if (_fromNode.IsTarget &&
					_save.CanUseWeapon(_attackAction.weapon, _unit, false, BA_SNAPSHOT) &&
					_save.ModuleMap[_fromNode.Position.x / 10][_fromNode.Position.y / 10].second > 0)
				{
					// scan this room for objects to destroy
					int x = (_unit.Position.x / 10) * 10;
					int y = (_unit.Position.y / 10) * 10;
					for (int i = x; i < x + 9; i++)
						for (int j = y; j < y + 9; j++)
						{
							MapData md = _save.GetTile(new Position(i, j, 1)).GetMapData(TilePart.O_OBJECT);
							if (md != null && md.IsBaseModule)
							{
								_patrolAction.actor = _unit;
								_patrolAction.target = new Position(i, j, 1);
								_patrolAction.weapon = _attackAction.weapon;
								_patrolAction.type = BA_SNAPSHOT;
								_patrolAction.UpdateTU();
								_foundBaseModuleToDestroy = _save.Mod.AIDestroyBaseFacilities;
								return;
							}
						}
				}
				else
				{
					// find closest high value target which is not already allocated
					int closest = 1000000;
					BattleUnit nodeunit = null;
					foreach (Node node in _save.Nodes)
					{
						if (node.IsDummy)
						{
							continue;
						}

						nodeunit = _save.GetTile(node.Position).Unit;
						if (nodeunit != null && nodeunit.Faction == _unit.Faction)
						{
							continue;
						}

						if (node.IsTarget && !node.IsAllocated && _save.ModuleMap[node.Position.x / 10][node.Position.y / 10].second > 0)
						{
							int d = Position.DistanceSq(_unit.Position, node.Position);
							if (_toNode == null || (d < closest && node != _fromNode))
							{
								_toNode = node;
								closest = d;
							}
						}
					}
				}
			}

			if (_toNode == null)
			{
				_toNode = _save.GetPatrolNode(scout, _unit, _fromNode);
				if (_toNode == null)
				{
					_toNode = _save.GetPatrolNode(!scout, _unit, _fromNode);
				}
			}

			if (_toNode != null)
			{
				_save.Pathfinding.Calculate(_unit, _toNode.Position, BAM_NORMAL, null);
				if (_save.Pathfinding.StartDirection == -1)
				{
					_toNode = null;
				}
				_save.Pathfinding.AbortPath();
			}
		}

		if (_toNode != null)
		{
			_toNode.AllocateNode();
			_patrolAction.actor = _unit;
			_patrolAction.type = BA_WALK;
			_patrolAction.target = _toNode.Position;
		}
		else
		{
			_patrolAction.type = BA_RETHINK;
		}
	}

	/// <summary>
	/// Try to set up an ambush action.
	/// The idea is to check within a 11x11 tile square for a tile which is not seen by our aggroTarget,
	/// but that can be reached by him. we then intuit where we will see the target first from our covered
	/// position, and set that as our final facing.
	/// Fills out the _ambushAction with useful data.
	/// </summary>
	public void SetupAmbush()
	{
		_ambushAction.type = BA_RETHINK;
		int bestScore = 0;
		_ambushTUs = 0;
		var path = new List<int>();

		if (SelectClosestKnownEnemy())
		{
			const int BASE_SYSTEMATIC_SUCCESS = 100;
			const int COVER_BONUS = 25;
			const int FAST_PASS_THRESHOLD = 80;
			Position origin = _save.TileEngine.GetSightOriginVoxel(_aggroTarget);

			// we'll use node positions for this, as it gives map makers a good degree of control over how the units will use the environment.
			foreach (Node node in _save.Nodes)
			{
				if (node.IsDummy)
				{
					continue;
				}
				Position pos = node.Position;
				Tile tile = _save.GetTile(pos);
				if (tile == null || Position.Distance2d(pos, _unit.Position) > 10 || pos.z != _unit.Position.z || tile.Dangerous ||
					!_reachableWithAttack.Contains(_save.GetTileIndex(pos)))
					continue; // just ignore unreachable tiles

				if (_traceAI)
				{
					// colour all the nodes in range purple.
					tile.Preview = 10;
					tile.MarkerColor = 13;
				}

				// make sure we can't be seen here.
				var target = new Position();
				if (!_save.TileEngine.CanTargetUnit(ref origin, tile, ref target, _aggroTarget, false, _unit) && GetSpottingUnits(pos) == 0)
				{
					_save.Pathfinding.Calculate(_unit, pos, BAM_NORMAL, null);
					int ambushTUs = _save.Pathfinding.TotalTUCost;
					// make sure we can move here
					if (_save.Pathfinding.StartDirection != -1)
					{
						int score = BASE_SYSTEMATIC_SUCCESS;
						score -= ambushTUs;

						// make sure our enemy can reach here too.
						_save.Pathfinding.Calculate(_aggroTarget, pos, BAM_NORMAL, null);

						if (_save.Pathfinding.StartDirection != -1)
						{
							// ideally we'd like to be behind some cover, like say a window or a low wall.
							if (_save.TileEngine.FaceWindow(pos) != -1)
							{
								score += COVER_BONUS;
							}
							if (score > bestScore)
							{
								path = _save.Pathfinding.CopyPath();
								bestScore = score;
								_ambushTUs = (pos == _unit.Position) ? 1 : ambushTUs;
								_ambushAction.target = pos;
								if (bestScore > FAST_PASS_THRESHOLD)
								{
									break;
								}
							}
						}
					}
				}
			}

			if (bestScore > 0)
			{
				_ambushAction.type = BA_WALK;
				// i should really make a function for this
				origin = _ambushAction.target.ToVoxel() +
					// 4 because -2 is eyes and 2 below that is the rifle (or at least that's my understanding)
					new Position(8, 8, _unit.Height + _unit.FloatHeight - _save.GetTile(_ambushAction.target).TerrainLevel - 4);
				Position currentPos = _aggroTarget.Position;
				_save.Pathfinding.SetUnit(_aggroTarget);
				int tries = path.Count;
				// hypothetically walk the target through the path.
				while (tries > 0)
				{
					currentPos = _save.Pathfinding.GetTUCost(currentPos, path[^1], _aggroTarget, null, BAM_NORMAL).pos;
					path.RemoveAt(path.Count - 1);
					Tile tile = _save.GetTile(currentPos);
					var target = new Position();
					// do a virtual fire calculation
					if (_save.TileEngine.CanTargetUnit(ref origin, tile, ref target, _unit, false, _aggroTarget))
					{
						// if we can virtually fire at the hypothetical target, we know which way to face.
						_ambushAction.finalFacing = _save.TileEngine.GetDirectionTo(_ambushAction.target, currentPos);
						break;
					}
					--tries;
				}
				if (_traceAI)
				{
					Logger.Info($"Ambush estimation will move to {_ambushAction.target}");
				}
				return;
			}
		}
		if (_traceAI)
		{
			Logger.Info("Ambush estimation failed");
		}
	}

	/// <summary>
	/// Try to set up a combat action.
	/// This will either be a psionic, grenade, or weapon attack,
	/// or potentially just moving to get a line of sight to a target.
	/// Fills out the _attackAction with useful data.
	/// </summary>
	public void SetupAttack()
	{
		_attackAction.type = BA_RETHINK;
		_psiAction.type = BA_NONE;

		bool sniperAttack = false;

		// if enemies are known to us but not necessarily visible, we can attack them with a blaster launcher or psi or a sniper attack.
		if (_knownEnemies != 0)
		{
			if (PsiAction())
			{
				// at this point we can save some time with other calculations - the unit WILL make a psionic attack this turn.
				return;
			}
			if (_blaster)
			{
				WayPointAction();
			}
			else if (_unit.UnitRules != null) // xcom soldiers (under mind control) lack unit rules!
			{
				// don't always act on spotter information unless modder says so
				if (Rng.Percent(_unit.UnitRules.SniperPercentage))
				{
					sniperAttack = SniperAction();
				}
			}
		}

		// if we CAN see someone, that makes them a viable target for "regular" attacks.
		// This is skipped if sniperAction has already chosen an attack action
		if (!sniperAttack && SelectNearestTarget() != 0)
		{
			// if we have both types of weapon, make a determination on which to use.
			if (_melee && _rifle)
			{
				SelectMeleeOrRanged();
			}
			if (_grenade)
			{
				GrenadeAction();
			}
			if (_melee)
			{
				MeleeAction();
			}
			if (_rifle)
			{
				ProjectileAction();
			}
		}

		if (_attackAction.type != BA_RETHINK)
		{
			if (_traceAI)
			{
				if (_attackAction.type != BA_WALK)
				{
					Logger.Info($"Attack estimation desires to shoot at {_attackAction.target}");
				}
				else
				{
					Logger.Info($"Attack estimation desires to move to {_attackAction.target}");
				}
			}
			return;
		}
		else if (_spottingEnemies != 0 || _unit.Aggression < Rng.Generate(0, 3))
		{
			// if enemies can see us, or if we're feeling lucky, we can try to spot the enemy.
			if (FindFirePoint())
			{
				if (_traceAI)
				{
					Logger.Info($"Attack estimation desires to move to {_attackAction.target}");
				}
				return;
			}
		}
		if (_traceAI)
		{
			Logger.Info("Attack estimation failed");
		}
	}

	/// <summary>
	/// Attempts to find cover, and move toward it.
	/// The idea is to check within a 11x11 tile square for a tile which is not seen by our aggroTarget.
	/// If there is no such tile, we run away from the target.
	/// Fills out the _escapeAction with useful data.
	/// </summary>
	public void SetupEscape()
	{
		int unitsSpottingMe = GetSpottingUnits(_unit.Position);
		int currentTilePreference = 15;
		int tries = -1;
		bool coverFound = false;
		SelectNearestTarget();
		_escapeTUs = 0;

		int dist = _aggroTarget != null ? Position.Distance2d(_unit.Position, _aggroTarget.Position) : 0;

		int bestTileScore = -100000;
		int score = -100000;
		var bestTile = new Position(0, 0, 0);
		bool run = false;

		Tile tile = null;

		// weights of various factors in choosing a tile to which to withdraw
		const int EXPOSURE_PENALTY = 10;
		const int FIRE_PENALTY = 40;
		const int BASE_SYSTEMATIC_SUCCESS = 100;
		const int BASE_DESPERATE_SUCCESS = 110;
		const int FAST_PASS_THRESHOLD = 100; // a score that's good enough to quit the while loop early; it's subjective, hand-tuned and may need tweaking

		var randomTileSearch = new List<Position>(_save.TileSearch);
		Rng.Shuffle(randomTileSearch);

		while (tries < 150 && !coverFound)
		{
			_escapeAction.target = _unit.Position; // start looking in a direction away from the enemy
			_escapeAction.run = _unit.Armor.AllowsRunning(false) && (tries & 1) != 0; // every odd try, i.e. roughly 50%

			if (_save.GetTile(_escapeAction.target) == null)
			{
				_escapeAction.target = _unit.Position; // cornered at the edge of the map perhaps?
			}

			score = 0;

			if (tries == -1)
			{
				// you know, maybe we should just stay where we are and not risk reaction fire...
				// or maybe continue to wherever we were running to and not risk looking stupid
				if (_save.GetTile(_unit.lastCover) != null)
				{
					_escapeAction.target = _unit.lastCover;
				}
			}
			else if (tries < 121)
			{
				// looking for cover
				_escapeAction.target.x += randomTileSearch[tries].x;
				_escapeAction.target.y += randomTileSearch[tries].y;
				score = BASE_SYSTEMATIC_SUCCESS;
				if (_escapeAction.target == _unit.Position)
				{
					if (unitsSpottingMe > 0)
					{
						// maybe don't stay in the same spot? move or something if there's any point to it?
						_escapeAction.target.x += (short)Rng.Generate(-20, 20);
						_escapeAction.target.y += (short)Rng.Generate(-20, 20);
					}
					else
					{
						score += currentTilePreference;
					}
				}
			}
			else
			{
				if (tries == 121)
				{
					if (_traceAI)
					{
						Logger.Info($"best score after systematic search was: {bestTileScore}");
					}
				}

				score = BASE_DESPERATE_SUCCESS; // ruuuuuuun
				_escapeAction.target = _unit.Position;
				_escapeAction.target.x += (short)Rng.Generate(-10, 10);
				_escapeAction.target.y += (short)Rng.Generate(-10, 10);
				_escapeAction.target.z = (short)(_unit.Position.z + Rng.Generate(-1, 1));
				if (_escapeAction.target.z < 0)
				{
					_escapeAction.target.z = 0;
				}
				else if (_escapeAction.target.z >= _save.MapSizeZ)
				{
					_escapeAction.target.z = _unit.Position.z;
				}
			}

			tries++;

			// THINK, DAMN YOU
			tile = _save.GetTile(_escapeAction.target);
			int distanceFromTarget = _aggroTarget != null ? Position.Distance2d(_aggroTarget.Position, _escapeAction.target) : 0;
			if (dist >= distanceFromTarget)
			{
				score -= (distanceFromTarget - dist) * 10;
			}
			else
			{
				score += (distanceFromTarget - dist) * 10;
			}
			int spotters = 0;
			if (tile == null)
			{
				score = -100001; // no you can't quit the battlefield by running off the map.
			}
			else
			{
				spotters = GetSpottingUnits(_escapeAction.target);
				if (!_reachable.Contains(_save.GetTileIndex(_escapeAction.target)))
					continue; // just ignore unreachable tiles

				if (_spottingEnemies != 0 || spotters != 0)
				{
					if (_spottingEnemies <= spotters)
					{
						score -= (1 + spotters - _spottingEnemies) * EXPOSURE_PENALTY; // that's for giving away our position
					}
					else
					{
						score += (_spottingEnemies - spotters) * EXPOSURE_PENALTY;
					}
				}
				if (tile.Fire != 0)
				{
					score -= FIRE_PENALTY;
				}
				if (tile.Dangerous)
				{
					score -= BASE_SYSTEMATIC_SUCCESS;
				}

				if (_traceAI)
				{
					tile.MarkerColor = score < 0 ? 3 : (score < FAST_PASS_THRESHOLD / 2 ? 8 : (score < FAST_PASS_THRESHOLD ? 9 : 5));
					tile.Preview = 10;
					tile.TUMarker = score;
				}
			}

			if (tile != null && score > bestTileScore)
			{
				// calculate TUs to tile; we could be getting this from findReachable() somehow but that would break something for sure...
				_save.Pathfinding.Calculate(_unit, _escapeAction.target, _escapeAction.MoveType, null);
				if (_escapeAction.target == _unit.Position || _save.Pathfinding.StartDirection != -1)
				{
					bestTileScore = score;
					bestTile = _escapeAction.target;
					run = _escapeAction.run;
					_escapeTUs = _save.Pathfinding.TotalTUCost;
					if (_escapeAction.target == _unit.Position)
					{
						_escapeTUs = 1;
					}
					if (_traceAI)
					{
						tile.MarkerColor = score < 0 ? 7 : (score < FAST_PASS_THRESHOLD / 2 ? 10 : (score < FAST_PASS_THRESHOLD ? 4 : 5));
						tile.Preview = 10;
						tile.TUMarker = score;
					}
				}
				_save.Pathfinding.AbortPath();
				if (bestTileScore > FAST_PASS_THRESHOLD) coverFound = true; // good enough, gogogo
			}
		}
		_escapeAction.target = bestTile;
		_escapeAction.run = run;
		if (_traceAI)
		{
			_save.GetTile(_escapeAction.target).MarkerColor = 13;
		}

		if (bestTileScore <= -100000)
		{
			if (_traceAI)
			{
				Logger.Info("Escape estimation failed.");
			}
			_escapeAction.type = BA_RETHINK; // do something, just don't look dumbstruck :P
			return;
		}
		else
		{
			if (_traceAI)
			{
				Logger.Info($"Escape estimation completed after {tries} tries, {Position.Distance2d(_unit.Position, bestTile)} squares or so away.");
			}
			_escapeAction.type = BA_WALK;
		}
	}

	/// <summary>Counts how many targets, both xcom and civilian are known to this unit.</summary>
	/// <returns>How many targets are known to us.</returns>
	public int CountKnownTargets()
	{
		int knownEnemies = 0;

		if (_unit.Faction == FACTION_HOSTILE)
		{
			foreach (BattleUnit bu in _save.Units)
			{
				if (ValidTarget(bu, true, true))
				{
					++knownEnemies;
				}
			}
		}
		return knownEnemies;
	}

	/// <summary>Counts how many enemies (xcom only) are spotting any given position.</summary>
	/// <param name="pos">The Position to check for spotters.</param>
	/// <returns>Spotters.</returns>
	public int GetSpottingUnits(Position pos)
	{
		// if we don't actually occupy the position being checked, we need to do a virtual LOF check.
		bool checking = pos != _unit.Position;
		int tally = 0;
		foreach (BattleUnit bu in _save.Units)
		{
			if (ValidTarget(bu, false, false))
			{
				int dist = Position.Distance2d(pos, bu.Position);
				if (dist > 20) continue;
				Position originVoxel = _save.TileEngine.GetSightOriginVoxel(bu);
				originVoxel.z -= 2;
				var targetVoxel = new Position();
				if (checking)
				{
					if (_save.TileEngine.CanTargetUnit(ref originVoxel, _save.GetTile(pos), ref targetVoxel, bu, false, _unit))
					{
						tally++;
					}
				}
				else
				{
					if (_save.TileEngine.CanTargetUnit(ref originVoxel, _save.GetTile(pos), ref targetVoxel, bu, false, null))
					{
						tally++;
					}
				}
			}
		}
		return tally;
	}

	/// <summary>
	/// Selects the nearest known living target we can see/reach and returns the number of visible enemies.
	/// This function includes civilians as viable targets.
	/// </summary>
	/// <returns>Viable targets.</returns>
	public int SelectNearestTarget()
	{
		int tally = 0;
		_closestDist = 100;
		_aggroTarget = null;
		var target = new Position();
		foreach (BattleUnit bu in _save.Units)
		{
			if (ValidTarget(bu, true, true) &&
				_save.TileEngine.Visible(_unit, bu.Tile))
			{
				tally++;
				int dist = Position.Distance2d(_unit.Position, bu.Position);
				if (dist < _closestDist)
				{
					bool valid = false;
					if (_rifle || !_melee)
					{
						var action = new BattleAction();
						action.actor = _unit;
						action.weapon = _attackAction.weapon;
						action.target = bu.Position;
						Position origin = _save.TileEngine.GetOriginVoxel(action, null);
						valid = _save.TileEngine.CanTargetUnit(ref origin, bu.Tile, ref target, _unit, false, null);
					}
					else
					{
						if (SelectPointNearTarget(bu, _unit.TimeUnits))
						{
							int dir = _save.TileEngine.GetDirectionTo(_attackAction.target, bu.Position);
							var dest = new Position(); // C++ passes no destination (nullptr)
							valid = _save.TileEngine.ValidMeleeRange(_attackAction.target, dir, _unit, bu, ref dest);
						}
					}
					if (valid)
					{
						_closestDist = dist;
						_aggroTarget = bu;
					}
				}
			}
		}
		if (_aggroTarget != null)
		{
			return tally;
		}

		return 0;
	}

	/// <summary>
	/// Selects the nearest known living target we can see/reach and returns the number of visible enemies.
	/// This function includes civilians as viable targets.
	/// Note: Differs from selectNearestTarget() in calling selectPointNearTargetLeeroy().
	/// </summary>
	/// <returns>Viable targets.</returns>
	private int SelectNearestTargetLeeroy(bool canRun)
	{
		int tally = 0;
		_closestDist = 100;
		_aggroTarget = null;
		foreach (BattleUnit bu in _save.Units)
		{
			if (ValidTarget(bu, true, true) &&
				_save.TileEngine.Visible(_unit, bu.Tile))
			{
				tally++;
				int dist = Position.Distance2d(_unit.Position, bu.Position);
				if (dist < _closestDist)
				{
					bool valid = false;
					if (SelectPointNearTargetLeeroy(bu, canRun))
					{
						int dir = _save.TileEngine.GetDirectionTo(_attackAction.target, bu.Position);
						var dest = new Position(); // C++ passes no destination (nullptr)
						valid = _save.TileEngine.ValidMeleeRange(_attackAction.target, dir, _unit, bu, ref dest);
					}
					if (valid)
					{
						_closestDist = dist;
						_aggroTarget = bu;
					}
				}
			}
		}
		if (_aggroTarget != null)
		{
			return tally;
		}

		return 0;
	}

	/// <summary>Selects the nearest known living Xcom unit. used for ambush calculations.</summary>
	/// <returns>If we found one.</returns>
	public bool SelectClosestKnownEnemy()
	{
		_aggroTarget = null;
		int minDist = 255;
		foreach (BattleUnit bu in _save.Units)
		{
			if (ValidTarget(bu, true, false))
			{
				int dist = Position.Distance2d(bu.Position, _unit.Position);
				if (dist < minDist)
				{
					minDist = dist;
					_aggroTarget = bu;
				}
			}
		}
		return _aggroTarget != null;
	}

	/// <summary>Selects a random known living Xcom or civilian unit.</summary>
	/// <returns>If we found one.</returns>
	public bool SelectRandomTarget()
	{
		int farthest = -100;
		_aggroTarget = null;

		foreach (BattleUnit bu in _save.Units)
		{
			if (ValidTarget(bu, true, true))
			{
				int dist = Rng.Generate(0, 20) - Position.Distance2d(_unit.Position, bu.Position);
				if (dist > farthest)
				{
					farthest = dist;
					_aggroTarget = bu;
				}
			}
		}
		return _aggroTarget != null;
	}

	/// <summary>Selects a point near enough to our target to perform a melee attack.</summary>
	/// <param name="target">Pointer to a target.</param>
	/// <param name="maxTUs">Maximum time units the path to the target can cost.</param>
	/// <returns>True if a point was found.</returns>
	public bool SelectPointNearTarget(BattleUnit target, int maxTUs)
	{
		int size = _unit.Armor.Size;
		int sizeTarget = target.Armor.Size;
		int dirTarget = target.Direction;
		float dodgeChanceDiff = target.Armor.GetMeleeDodge(target) * target.Armor.MeleeDodgeBackPenalty * _attackAction.diff / 160.0f;
		bool returnValue = false;
		int distance = 1000;
		for (int z = -1; z <= 1; ++z)
		{
			for (int x = -size; x <= sizeTarget; ++x)
			{
				for (int y = -size; y <= sizeTarget; ++y)
				{
					if (x != 0 || y != 0) // skip the unit itself
					{
						Position checkPath = target.Position + new Position(x, y, z);
						if (_save.GetTile(checkPath) == null || !_reachable.Contains(_save.GetTileIndex(checkPath)))
							continue;
						int dir = _save.TileEngine.GetDirectionTo(checkPath, target.Position);
						var dest = new Position(); // C++ passes no destination (nullptr)
						bool valid = _save.TileEngine.ValidMeleeRange(checkPath, dir, _unit, target, ref dest);
						bool fitHere = _save.SetUnitPosition(_unit, checkPath, true);

						if (valid && fitHere && !_save.GetTile(checkPath).Dangerous)
						{
							_save.Pathfinding.Calculate(_unit, checkPath, BAM_NORMAL, null, maxTUs);

							//for 100% dodge diff and on 4th difficulty it will allow aliens to move 10 squares around to made attack from behind.
							int distanceCurrent = (int)(_save.Pathfinding.Path.Count - dodgeChanceDiff * _save.TileEngine.GetArcDirection(dir - 4, dirTarget));
							if (_save.Pathfinding.StartDirection != -1 && distanceCurrent < distance)
							{
								_attackAction.target = checkPath;
								returnValue = true;
								distance = distanceCurrent;
							}
							_save.Pathfinding.AbortPath();
						}
					}
				}
			}
		}
		return returnValue;
	}

	/// <summary>
	/// Selects a point near enough to our target to perform a melee attack.
	/// Note: Differs from selectPointNearTarget() in that it doesn't consider:
	///  - remaining TUs (charge even if not enough TUs to attack)
	///  - dangerous tiles (grenades? pfff!)
	///  - melee dodge (not intelligent enough to attack from behind)
	/// </summary>
	/// <param name="target">Pointer to a target.</param>
	/// <param name="canRun">Can the unit run?</param>
	/// <returns>True if a point was found.</returns>
	private bool SelectPointNearTargetLeeroy(BattleUnit target, bool canRun)
	{
		int size = _unit.Armor.Size;
		int targetsize = target.Armor.Size;
		bool returnValue = false;
		uint distance = 1000;
		for (int z = -1; z <= 1; ++z)
		{
			for (int x = -size; x <= targetsize; ++x)
			{
				for (int y = -size; y <= targetsize; ++y)
				{
					if (x != 0 || y != 0) // skip the unit itself
					{
						Position checkPath = target.Position + new Position(x, y, z);
						if (_save.GetTile(checkPath) == null)
							continue;
						int dir = _save.TileEngine.GetDirectionTo(checkPath, target.Position);
						var dest = new Position(); // C++ passes no destination (nullptr)
						bool valid = _save.TileEngine.ValidMeleeRange(checkPath, dir, _unit, target, ref dest);
						bool fitHere = _save.SetUnitPosition(_unit, checkPath, true);

						if (valid && fitHere)
						{
							_save.Pathfinding.Calculate(_unit, checkPath, canRun ? BAM_RUN : BAM_NORMAL, null, 100000); // disregard unit's TUs.
							if (_save.Pathfinding.StartDirection != -1 && (uint)_save.Pathfinding.Path.Count < distance)
							{
								_attackAction.target = checkPath;
								returnValue = true;
								distance = (uint)_save.Pathfinding.Path.Count;
							}
							_save.Pathfinding.AbortPath();
						}
					}
				}
			}
		}
		return returnValue;
	}

	/// <summary>Selects a target from a list of units seen by spotter units for out-of-LOS actions and populates _attackAction with the relevant data.</summary>
	/// <returns>True if we have a target selected.</returns>
	public bool SelectSpottedUnitForSniper()
	{
		_aggroTarget = null;

		// Create a list of spotted targets and the type of attack we'd like to use on each
		var spottedTargets = new List<(BattleUnit first, BattleAction second)>();

		// Get the TU costs for each available attack type
		var costAuto = new BattleActionCost(BA_AUTOSHOT, _attackAction.actor, _attackAction.weapon);
		var costSnap = new BattleActionCost(BA_SNAPSHOT, _attackAction.actor, _attackAction.weapon);
		var costAimed = new BattleActionCost(BA_AIMEDSHOT, _attackAction.actor, _attackAction.weapon);

		var costThrow = new BattleActionCost();
		// Only want to check throwing if we have a grenade, the default constructor (line above) conveniently returns false from haveTU()
		if (_grenade)
		{
			// We know we have a grenade, now we need to know if we have the TUs to throw it
			costThrow.type = BA_THROW;
			costThrow.actor = _attackAction.actor;
			costThrow.weapon = _unit.GetGrenadeFromBelt(_save);
			costThrow.UpdateTU();
			costThrow.Time += 4; // Vanilla TUs for AI picking up grenade from belt
			costThrow += _attackAction.actor.GetActionTUs(BA_PRIME, costThrow.weapon);
		}

		foreach (BattleUnit bu in _save.Units)
		{
			if (ValidTarget(bu, true, true) && bu.GetTurnsLeftSpottedForSnipersByFaction(_unit.Faction) != 0)
			{
				// Determine which firing mode to use based on how many hits we expect per turn and the unit's intelligence/aggression
				_aggroTarget = bu;
				_attackAction.type = BA_RETHINK;
				_attackAction.target = bu.Position;
				ExtendedFireModeChoice(costAuto, costSnap, costAimed, costThrow, true);

				BattleAction chosenAction = new BattleAction(_attackAction);
				if (chosenAction.type == BA_THROW)
					chosenAction.weapon = costThrow.weapon;

				if (_attackAction.type != BA_RETHINK)
				{
					spottedTargets.Add((bu, chosenAction));
				}
			}
		}

		int numberOfTargets = spottedTargets.Count;

		if (numberOfTargets != 0) // Now that we have a list of valid targets, pick one and return.
		{
			int pick = Rng.Generate(0, numberOfTargets - 1);
			_aggroTarget = spottedTargets[pick].first;
			_attackAction.target = _aggroTarget.Position;
			_attackAction.type = spottedTargets[pick].second.type;
			_attackAction.weapon = spottedTargets[pick].second.weapon;
		}
		else // We didn't find a suitable target
		{
			// Make sure we reset anything we might have changed while testing for targets
			_aggroTarget = null;
			_attackAction.type = BA_RETHINK;
			_attackAction.weapon = _unit.GetMainHandWeapon(false);
		}

		return _aggroTarget != null;
	}

	/// <summary>Scores a firing mode for a particular target based on a accuracy / TUs ratio.</summary>
	/// <param name="action">Pointer to the BattleAction determining the firing mode.</param>
	/// <param name="target">Pointer to the BattleUnit we're trying to target.</param>
	/// <param name="checkLOF">Set to true if you want to check for a valid line of fire.</param>
	/// <returns>The calculated score.</returns>
	public int ScoreFiringMode(BattleAction action, BattleUnit target, bool checkLOF)
	{
		// Sanity check first, if the passed action has no type or weapon, return 0.
		if (action.type == BA_NONE || action.weapon == null)
		{
			return 0;
		}
		RuleItem weapon = action.weapon.Rules;

		// Get base accuracy for the action
		int accuracy = BattleUnit.GetFiringAccuracy(BattleActionAttack.GetBeforeShoot(action), _save.Mod);
		int distanceSq = _unit.Distance3dToUnitSq(target);
		int distance = (int)MathF.Ceiling(MathF.Sqrt(distanceSq)); // C++ sqrt(float)

		{
			int upperLimit = 0, lowerLimit = 0;
			int dropoff = weapon.CalculateLimits(ref upperLimit, ref lowerLimit, _save.Depth, action.type);

			if (distance > upperLimit)
			{
				accuracy -= (distance - upperLimit) * dropoff;
			}
			else if (distance < lowerLimit)
			{
				accuracy -= (lowerLimit - distance) * dropoff;
			}
		}

		bool outOfRange = action.type == BA_THROW
			? weapon.IsOutOfThrowRange(distanceSq, _save.Depth)
			: weapon.IsOutOfRange(distanceSq);

		if (outOfRange)
		{
			accuracy = 0;
		}

		int numberOfShots = 1;
		if (action.type == BA_AIMEDSHOT)
		{
			numberOfShots = weapon.ConfigAimed.shots;
		}
		else if (action.type == BA_SNAPSHOT)
		{
			numberOfShots = weapon.ConfigSnap.shots;
		}
		else if (action.type == BA_AUTOSHOT)
		{
			numberOfShots = weapon.ConfigAuto.shots;
		}

		int tuCost = _unit.GetActionTUs(action.type, action.weapon).Time;
		// Need to include TU cost of getting grenade from belt + priming if we're checking throwing
		if (action.type == BA_THROW && _grenade)
		{
			// FIXME: why not just use action->weapon ?
			BattleItem grenadeItem = _unit.GetGrenadeFromBelt(_save);
			tuCost = _unit.GetActionTUs(action.type, grenadeItem).Time;
			tuCost += 4;
			tuCost += _unit.GetActionTUs(BA_PRIME, grenadeItem).Time;
		}
		int tuTotal = _unit.BaseStats.tu;

		// Return a score of zero if this firing mode doesn't exist for this weapon
		if (tuCost == 0)
		{
			return 0;
		}

		if (checkLOF)
		{
			Position origin = _save.TileEngine.GetOriginVoxel(action, null);
			var targetPosition = new Position();

			if (action.weapon.GetArcingShot(action.type) || action.type == BA_THROW)
			{
				targetPosition = target.Position.ToVoxel() + new Position(8, 8, (2 + -target.Tile.TerrainLevel));
				double curve = 0; // C++ passes no curve/voxelType (nullptr)
				int voxelType = 0;
				if (!_save.TileEngine.ValidateThrow(action, origin, targetPosition, _save.Depth, ref curve, ref voxelType))
				{
					return 0;
				}
			}
			else
			{
				if (!_save.TileEngine.CanTargetUnit(ref origin, target.Tile, ref targetPosition, _unit, false, target))
				{
					return 0;
				}
			}
		}

		return accuracy * numberOfShots * tuTotal / tuCost;
	}

	/// <summary>Selects an AI mode based on a number of factors, some RNG and the results of the rest of the determinations.</summary>
	public void EvaluateAIMode()
	{
		if ((_unit.Charging != null && _attackAction.type != BA_RETHINK))
		{
			_AIMode = AI_COMBAT;
			return;
		}
		// don't try to run away as often if we're a melee type, and really don't try to run away if we have a viable melee target, or we still have 50% or more TUs remaining.
		int escapeOdds = 15;
		if (_melee)
		{
			escapeOdds = 12;
		}
		if (_unit.Faction == FACTION_HOSTILE && (_unit.TimeUnits > _unit.BaseStats.tu / 2 || _unit.Charging != null))
		{
			escapeOdds = 5;
		}
		int ambushOdds = 12;
		int combatOdds = 20;
		// we're less likely to patrol if we see enemies.
		int patrolOdds = _visibleEnemies != 0 ? 15 : 30;

		// the enemy sees us, we should take retreat into consideration, and forget about patrolling for now.
		if (_spottingEnemies != 0)
		{
			patrolOdds = 0;
			if (_escapeTUs == 0)
			{
				SetupEscape();
			}
		}

		// melee/blaster units shouldn't consider ambush
		if (!_rifle || _ambushTUs == 0)
		{
			ambushOdds = 0;
			if (_melee)
			{
				combatOdds = (int)(combatOdds * 1.3);
			}
		}

		// if we KNOW there are enemies around...
		if (_knownEnemies != 0)
		{
			if (_knownEnemies == 1)
			{
				combatOdds = (int)(combatOdds * 1.2);
			}

			if (_escapeTUs == 0)
			{
				if (SelectClosestKnownEnemy())
				{
					SetupEscape();
				}
				else
				{
					escapeOdds = 0;
				}
			}
		}
		else if (_unit.Faction == FACTION_HOSTILE)
		{
			combatOdds = 0;
			escapeOdds = 0;
		}

		// take our current mode into consideration
		switch (_AIMode)
		{
			case AI_PATROL:
				patrolOdds = (int)(patrolOdds * 1.1);
				break;
			case AI_AMBUSH:
				ambushOdds = (int)(ambushOdds * 1.1);
				break;
			case AI_COMBAT:
				combatOdds = (int)(combatOdds * 1.1);
				break;
			case AI_ESCAPE:
				escapeOdds = (int)(escapeOdds * 1.1);
				break;
		}

		// take our overall health into consideration
		if (_unit.Health < _unit.BaseStats.health / 3)
		{
			escapeOdds = (int)(escapeOdds * 1.7);
			combatOdds = (int)(combatOdds * 0.6);
			ambushOdds = (int)(ambushOdds * 0.75);
		}
		else if (_unit.Health < 2 * (_unit.BaseStats.health / 3))
		{
			escapeOdds = (int)(escapeOdds * 1.4);
			combatOdds = (int)(combatOdds * 0.8);
			ambushOdds = (int)(ambushOdds * 0.8);
		}
		else if (_unit.Health < _unit.BaseStats.health)
		{
			escapeOdds = (int)(escapeOdds * 1.1);
		}

		// take our aggression into consideration
		switch (_unit.Aggression)
		{
			case 0:
				escapeOdds = (int)(escapeOdds * 1.4);
				combatOdds = (int)(combatOdds * 0.7);
				break;
			case 1:
				ambushOdds = (int)(ambushOdds * 1.1);
				break;
			case 2:
				combatOdds = (int)(combatOdds * 1.4);
				escapeOdds = (int)(escapeOdds * 0.7);
				break;
			default:
				combatOdds = (int)(combatOdds * Clamp(1.2 + (_unit.Aggression / 10.0), 0.1, 2.0));
				escapeOdds = (int)(escapeOdds * Clamp(0.9 - (_unit.Aggression / 10.0), 0.1, 2.0));
				break;
		}

		if (_AIMode == AI_COMBAT)
		{
			ambushOdds = (int)(ambushOdds * 1.5);
		}

		// factor in the spotters.
		if (_spottingEnemies != 0)
		{
			escapeOdds = 10 * escapeOdds * (_spottingEnemies + 10) / 100;
			combatOdds = 5 * combatOdds * (_spottingEnemies + 20) / 100;
		}
		else
		{
			escapeOdds /= 2;
		}

		// factor in visible enemies.
		if (_visibleEnemies != 0)
		{
			combatOdds = 10 * combatOdds * (_visibleEnemies + 10) / 100;
			if (_closestDist < 5)
			{
				ambushOdds = 0;
			}
		}
		// make sure we have an ambush lined up, or don't even consider it.
		if (_ambushTUs != 0)
		{
			ambushOdds = (int)(ambushOdds * 1.7);
		}
		else
		{
			ambushOdds = 0;
		}

		// factor in mission type
		if (_save.MissionType == "STR_BASE_DEFENSE")
		{
			escapeOdds = (int)(escapeOdds * 0.75);
			ambushOdds = (int)(ambushOdds * 0.6);
		}

		// no weapons, not psychic? don't pick combat or ambush
		if (!_melee && !_rifle && !_blaster && !_grenade && _unit.BaseStats.psiSkill == 0)
		{
			combatOdds = 0;
			ambushOdds = 0;
		}
		// generate a random number to represent our decision.
		int decision = Rng.Generate(1, Math.Max(1, patrolOdds + ambushOdds + escapeOdds + combatOdds));

		if (decision > escapeOdds)
		{
			if (decision > escapeOdds + ambushOdds)
			{
				if (decision > escapeOdds + ambushOdds + combatOdds)
				{
					_AIMode = AI_PATROL;
				}
				else
				{
					_AIMode = AI_COMBAT;
				}
			}
			else
			{
				_AIMode = AI_AMBUSH;
			}
		}
		else
		{
			_AIMode = AI_ESCAPE;
		}

		// if the aliens are cheating, or the unit is charging, enforce combat as a priority.
		if ((_unit.Faction == FACTION_HOSTILE && _save.IsCheating) || _unit.Charging != null)
		{
			_AIMode = AI_COMBAT;
		}

		// enforce the validity of our decision, and try fallback behaviour according to priority.
		if (_AIMode == AI_COMBAT)
		{
			Tile xtile = _save.GetTile(_attackAction.target);
			bool throwingGrenadeOrProxy = _attackAction.type == BA_THROW && _attackAction.weapon != null && _attackAction.weapon.Rules.IsGrenadeOrProxy;
			if (xtile != null && (xtile.Unit != null || throwingGrenadeOrProxy)) // https://openxcom.org/forum/index.php?topic=12145.0
			{
				if (_attackAction.type != BA_RETHINK)
				{
					return;
				}
				if (FindFirePoint())
				{
					return;
				}
			}
			else if (SelectRandomTarget() && FindFirePoint())
			{
				return;
			}
			_AIMode = AI_PATROL;
		}

		if (_AIMode == AI_PATROL)
		{
			if (_toNode != null || _foundBaseModuleToDestroy)
			{
				return;
			}
			// base defense mission protocol: patrol action becomes an attack action when base modules are sighted
			if (_patrolAction.type == BA_SNAPSHOT)
			{
				return;
			}
			_AIMode = AI_AMBUSH;
		}

		if (_AIMode == AI_AMBUSH)
		{
			if (_ambushTUs != 0)
			{
				return;
			}
			_AIMode = AI_ESCAPE;
		}
	}

	/// <summary>
	/// Find a position where we can see our target, and move there.
	/// check the 11x11 grid for a position nearby where we can potentially target him.
	/// </summary>
	/// <returns>True if a possible position was found.</returns>
	public bool FindFirePoint()
	{
		if (!SelectClosestKnownEnemy())
			return false;
		var randomTileSearch = new List<Position>(_save.TileSearch); // copy!
		Rng.Shuffle(randomTileSearch);
		var target = new Position();
		const int BASE_SYSTEMATIC_SUCCESS = 100;
		const int FAST_PASS_THRESHOLD = 125;
		bool waitIfOutsideWeaponRange = _unit.GeoscapeSoldier != null ? false : _unit.UnitRules.WaitIfOutsideWeaponRange();
		bool extendedFireModeChoiceEnabled = _save.Mod.AIExtendedFireModeChoice;
		int bestScore = 0;
		_attackAction.type = BA_RETHINK;
		foreach (Position randomPosition in randomTileSearch)
		{
			Position pos = _unit.Position + randomPosition;
			Tile tile = _save.GetTile(pos);
			if (tile == null ||
				!_reachableWithAttack.Contains(_save.GetTileIndex(pos)))
				continue;
			int score = 0;
			// i should really make a function for this
			Position origin = pos.ToVoxel() +
				// 4 because -2 is eyes and 2 below that is the rifle (or at least that's my understanding)
				new Position(8, 8, _unit.Height + _unit.FloatHeight - tile.TerrainLevel - 4);

			if (_save.TileEngine.CanTargetUnit(ref origin, _aggroTarget.Tile, ref target, _unit, false, null))
			{
				_save.Pathfinding.Calculate(_unit, pos, BAM_NORMAL, null);
				// can move here
				if (_save.Pathfinding.StartDirection != -1)
				{
					score = BASE_SYSTEMATIC_SUCCESS - GetSpottingUnits(pos) * 10;
					score += _unit.TimeUnits - _save.Pathfinding.TotalTUCost;
					if (!_aggroTarget.CheckViewSector(pos))
					{
						score += 10;
					}

					// Extended behavior: if we have a limited-range weapon, bump up the score for getting closer to the target, down for further
					if (!waitIfOutsideWeaponRange && extendedFireModeChoiceEnabled)
					{
						int distanceToTargetSq = _unit.Distance3dToUnitSq(_aggroTarget);
						int distanceToTarget = (int)MathF.Ceiling(MathF.Sqrt(distanceToTargetSq)); // C++ sqrt(float)
						if (_attackAction.weapon != null && _attackAction.weapon.Rules.IsOutOfRange(distanceToTargetSq)) // make sure we can get the ruleset before checking the range
						{
							int proposedDistance = Position.Distance2d(pos, _aggroTarget.Position);
							proposedDistance = Math.Max(proposedDistance, 1);
							score = score * distanceToTarget / proposedDistance;
						}
					}

					if (score > bestScore)
					{
						bestScore = score;
						_attackAction.target = pos;
						_attackAction.finalFacing = _save.TileEngine.GetDirectionTo(pos, _aggroTarget.Position);
						if (score > FAST_PASS_THRESHOLD)
						{
							break;
						}
					}
				}
			}
		}

		if (bestScore > 70)
		{
			_attackAction.type = BA_WALK;
			if (_traceAI)
			{
				Logger.Info($"Firepoint found at {_attackAction.target}, with a score of: {bestScore}");
			}
			return true;
		}
		if (_traceAI)
		{
			Logger.Info($"Firepoint failed, best estimation was: {_attackAction.target}, with a score of: {bestScore}");
		}

		return false;
	}

	/// <summary>
	/// Decides if it worth our while to create an explosion here.
	/// Return value in same range as number affected targets but not equal exactly to that value.
	/// </summary>
	/// <param name="targetPos">The target's position.</param>
	/// <param name="attackingUnit">The attacking unit.</param>
	/// <param name="radius">How big the explosion will be.</param>
	/// <param name="diff">Game difficulty.</param>
	/// <param name="grenade">Is the explosion coming from a grenade?</param>
	/// <returns>Value greater than zero if it is worthwhile creating an explosion in the target position. Bigger value better target.</returns>
	public int ExplosiveEfficacy(Position targetPos, BattleUnit attackingUnit, int radius, int diff, bool grenade = false)
	{
		Tile targetTile = _save.GetTile(targetPos);

		// don't throw grenades at flying enemies.
		if (grenade && targetPos.z > 0 && targetTile.HasNoFloor(_save))
		{
			return 0; // C++ returns false
		}

		if (diff == -1)
		{
			diff = _save.BattleState.Game.SavedGame.DifficultyCoefficient;
		}
		int distance = Position.Distance2d(attackingUnit.Position, targetPos);
		int injurylevel = attackingUnit.BaseStats.health - attackingUnit.Health;
		int desperation = (100 - attackingUnit.Morale) / 10;
		int enemiesAffected = 0;
		// if we're below 1/3 health, let's assume things are dire, and increase desperation.
		if (injurylevel > (attackingUnit.BaseStats.health / 3) * 2)
			desperation += 3;

		int efficacy = (int)AIAttackWeight.AIW_SCALE * desperation;

		// don't go kamikaze unless we're already doomed.
		if (Math.Abs(attackingUnit.Position.z - targetPos.z) <= Options.battleExplosionHeight && distance <= radius)
		{
			efficacy -= (int)AIAttackWeight.AIW_SCALE * 4;
		}

		// allow difficulty to have its influence
		efficacy += (int)AIAttackWeight.AIW_SCALE * diff / 2;

		// account for the unit we're targetting
		BattleUnit target = targetTile.Unit;
		if (target != null && !targetTile.Dangerous)
		{
			++enemiesAffected;
			efficacy += (int)GetTargetAttackWeight(target);
		}

		foreach (BattleUnit bu in _save.Units)
		{
			// don't grenade dead guys
			if (!bu.IsOut &&
				// don't count ourself twice
				bu != attackingUnit &&
				// don't count the target twice
				bu != target &&
				// don't count units that probably won't be affected cause they're out of range
				Math.Abs(bu.Position.z - targetPos.z) <= Options.battleExplosionHeight &&
				Position.Distance2d(bu.Position, targetPos) <= radius)
			{
				if (bu.Tile.Dangerous)
				{
					// don't count people who were already grenaded this turn
					continue;
				}

				AIAttackWeight weight = GetTargetAttackWeight(bu);

				if (weight == 0)
				{
					// AI do not know anything about this unit
					continue;
				}

				// trace a line from the grenade origin to the unit we're checking against
				Position voxelPosA = targetPos.ToVoxel() + TileEngine.voxelTileCenter;
				Position voxelPosB = bu.Position.ToVoxel() + TileEngine.voxelTileCenter;
				var traj = new List<Position>();
				VoxelType collidesWith = _save.TileEngine.CalculateLineVoxel(voxelPosA, voxelPosB, false, traj, target, bu);

				if (collidesWith == V_UNIT && traj[0].ToTile() == bu.Position)
				{
					if (bu.Faction == _targetFaction)
					{
						++enemiesAffected;
					}

					efficacy += (int)weight;
				}
			}
		}
		// don't throw grenades at single targets, unless morale is in the danger zone
		// or we're halfway towards panicking while bleeding to death.
		if (grenade && desperation < 6 && enemiesAffected < 2)
		{
			return 0;
		}

		if (enemiesAffected >= 10)
		{
			// Ignore loses if we can kill lot of enemies.
			return enemiesAffected;
		}
		else if (efficacy > 0)
		{
			// We kill more enemies than allies. Scale back to number of targets, can round down to zero
			return efficacy / (int)AIAttackWeight.AIW_SCALE;
		}
		else
		{
			return 0;
		}
	}

	/// <summary>
	/// Attempts to take a melee attack/charge an enemy we can see.
	/// Melee targetting: we can see an enemy, we can move to it so we're charging blindly toward an enemy.
	/// </summary>
	public void MeleeAction()
	{
		var attackCost = new BattleActionCost(BA_HIT, _unit, _unit.GetUtilityWeapon(BT_MELEE));
		if (!attackCost.HaveTU())
		{
			// cannot make a melee attack - consider some other behaviour, like running away, or standing motionless.
			return;
		}
		if (_aggroTarget != null && !_aggroTarget.IsOut)
		{
			if (_save.TileEngine.ValidMeleeRange(_unit, _aggroTarget, _save.TileEngine.GetDirectionTo(_unit.Position, _aggroTarget.Position)))
			{
				MeleeAttack();
				return;
			}
		}
		int chargeReserve = Math.Min(_unit.TimeUnits - attackCost.Time, 2 * (_unit.Energy - attackCost.Energy));
		int distance = (chargeReserve / 4) + 1;
		_aggroTarget = null;
		foreach (BattleUnit bu in _save.Units)
		{
			int newDistance = Position.Distance2d(_unit.Position, bu.Position);
			if (newDistance > 20 ||
				!ValidTarget(bu, true, true))
				continue;
			//pick closest living unit that we can move to
			if ((newDistance < distance || newDistance == 1) && !bu.IsOut)
			{
				if (newDistance == 1 || SelectPointNearTarget(bu, chargeReserve))
				{
					_aggroTarget = bu;
					_attackAction.type = BA_WALK;
					_unit.Charging = _aggroTarget;
					distance = newDistance;
				}
			}
		}
		if (_aggroTarget != null)
		{
			if (_save.TileEngine.ValidMeleeRange(_unit, _aggroTarget, _save.TileEngine.GetDirectionTo(_unit.Position, _aggroTarget.Position)))
			{
				MeleeAttack();
			}
		}
		if (_traceAI && _aggroTarget != null) { Logger.Info($"AIModule::meleeAction: [target]: {_aggroTarget.Id} at: {_attackAction.target}"); }
		if (_traceAI && _aggroTarget != null) { Logger.Info("CHARGE!"); }
	}

	/// <summary>
	/// Attempts to take a melee attack/charge an enemy we can see.
	/// Melee targetting: we can see an enemy, we can move to it so we're charging blindly toward an enemy.
	/// Note: Differs from meleeAction() in calling selectPointNearTargetLeeroy() and ignoring some more checks.
	/// </summary>
	private void MeleeActionLeeroy(bool canRun)
	{
		if (_aggroTarget != null && !_aggroTarget.IsOut)
		{
			if (_save.TileEngine.ValidMeleeRange(_unit, _aggroTarget, _save.TileEngine.GetDirectionTo(_unit.Position, _aggroTarget.Position)))
			{
				MeleeAttack();
				return;
			}
		}
		int distance = 1000;
		_aggroTarget = null;
		foreach (BattleUnit bu in _save.Units)
		{
			int newDistance = Position.Distance2d(_unit.Position, bu.Position);
			if (!ValidTarget(bu, true, true))
				continue;
			//pick closest living unit
			if ((newDistance < distance || newDistance == 1) && !bu.IsOut)
			{
				if (newDistance == 1 || SelectPointNearTargetLeeroy(bu, canRun))
				{
					_aggroTarget = bu;
					_attackAction.type = BA_WALK;
					_attackAction.run = canRun;
					_unit.Charging = _aggroTarget;
					distance = newDistance;
				}
			}
		}
		if (_aggroTarget != null)
		{
			if (_save.TileEngine.ValidMeleeRange(_unit, _aggroTarget, _save.TileEngine.GetDirectionTo(_unit.Position, _aggroTarget.Position)))
			{
				MeleeAttack();
			}
		}
		if (_traceAI && _aggroTarget != null) { Logger.Info($"AIModule::meleeAction: [target]: {_aggroTarget.Id} at: {_attackAction.target}"); }
		if (_traceAI && _aggroTarget != null) { Logger.Info("CHARGE!"); }
	}

	/// <summary>
	/// Attempts to fire a waypoint projectile at an enemy we, or one of our teammates sees.
	/// Waypoint targeting: pick from any units currently spotted by our allies.
	/// </summary>
	public void WayPointAction()
	{
		var attackCost = new BattleActionCost(BA_LAUNCH, _unit, _attackAction.weapon);
		if (!attackCost.HaveTU())
		{
			// cannot make a launcher attack - consider some other behaviour, like running away, or standing motionless.
			return;
		}
		_aggroTarget = null;
		foreach (BattleUnit bu in _save.Units)
		{
			if (_aggroTarget != null) break; // loop finished
			if (!ValidTarget(bu, true, true))
			{
				continue;
			}
			_save.Pathfinding.Calculate(_unit, bu.Position, BAM_MISSILE, bu, -1);
			BattleItem ammo = _attackAction.weapon.GetAmmoForAction(BA_LAUNCH);
			if (_save.Pathfinding.StartDirection != -1 &&
				ExplosiveEfficacy(bu.Position, _unit, ammo.Rules.GetExplosionRadius(new BattleActionAttack(BA_LAUNCH, _unit, _attackAction.weapon, ammo)), _attackAction.diff) != 0)
			{
				_aggroTarget = bu;
			}
			_save.Pathfinding.AbortPath();
		}

		if (_aggroTarget != null)
		{
			_attackAction.type = BA_LAUNCH;
			_attackAction.UpdateTU();
			if (!_attackAction.HaveTU())
			{
				_attackAction.type = BA_RETHINK;
				return;
			}
			_attackAction.waypoints.Clear();

			int PathDirection;
			VoxelType CollidesWith;
			int maxWaypoints = _attackAction.weapon.CurrentWaypoints;
			if (maxWaypoints == -1)
			{
				maxWaypoints = 6 + (_attackAction.diff * 2);
			}
			Position LastWayPoint = _unit.Position;
			Position LastPosition = _unit.Position;
			Position CurrentPosition = _unit.Position;
			var DirectionVector = new Position();

			_save.Pathfinding.Calculate(_unit, _aggroTarget.Position, BAM_MISSILE, _aggroTarget, -1);
			PathDirection = _save.Pathfinding.DequeuePath();
			while (PathDirection != -1 && _attackAction.waypoints.Count < maxWaypoints)
			{
				LastPosition = CurrentPosition;
				Pathfinding.DirectionToVector(PathDirection, ref DirectionVector);
				CurrentPosition = CurrentPosition + DirectionVector;
				var voxelPosA = new Position((CurrentPosition.x * 16) + 8, (CurrentPosition.y * 16) + 8, (CurrentPosition.z * 24) + 16);
				var voxelPosb = new Position((LastWayPoint.x * 16) + 8, (LastWayPoint.y * 16) + 8, (LastWayPoint.z * 24) + 16);
				CollidesWith = _save.TileEngine.CalculateLineVoxel(voxelPosA, voxelPosb, false, null, _unit, null);
				if (CollidesWith > V_EMPTY && CollidesWith < V_UNIT)
				{
					_attackAction.waypoints.Add(LastPosition);
					LastWayPoint = LastPosition;
				}
				else if (CollidesWith == V_UNIT)
				{
					BattleUnit target = _save.GetTile(CurrentPosition).GetOverlappingUnit(_save);
					if (target == _aggroTarget)
					{
						_attackAction.waypoints.Add(CurrentPosition);
						LastWayPoint = CurrentPosition;
					}
				}
				PathDirection = _save.Pathfinding.DequeuePath();
			}
			_attackAction.target = _attackAction.waypoints[0];
			if (LastWayPoint != _aggroTarget.Position)
			{
				_attackAction.type = BA_RETHINK;
			}
		}
	}

	/// <summary>Attempts to fire at an enemy spotted for us.</summary>
	public bool SniperAction()
	{
		if (_traceAI) { Logger.Info("Attempting sniper action..."); }

		if (SelectSpottedUnitForSniper())
		{
			_visibleEnemies = Math.Max(_visibleEnemies, 1); // Make sure we count at least our target as visible, otherwise we might not shoot!

			if (_traceAI) { Logger.Info($"Target for sniper found at ({_attackAction.target.x},{_attackAction.target.y},{_attackAction.target.z})."); }
			return true;
		}

		if (_traceAI) { Logger.Info("No valid target found or not enough TUs for sniper action."); }
		return false;
	}

	/// <summary>
	/// Attempts to fire at an enemy we can see.
	/// Regular targeting: we can see an enemy, we have a gun, let's try to shoot.
	/// </summary>
	public void ProjectileAction()
	{
		_attackAction.target = _aggroTarget.Position;
		void testEffect(BattleActionCost cost)
		{
			if (cost.HaveTU())
			{
				BattleActionAttack attack = BattleActionAttack.GetBeforeShoot(cost);
				if (attack.damage_item == null)
				{
					cost.ClearTU();
				}
				else
				{
					int radius = attack.damage_item.Rules.GetExplosionRadius(attack);
					if (radius != 0 && ExplosiveEfficacy(_attackAction.target, _unit, radius, _attackAction.diff) == 0)
					{
						cost.ClearTU();
					}
				}
			}
		}

		int distance = Position.Distance2d(_unit.Position, _attackAction.target);
		_attackAction.type = BA_RETHINK;

		var costAuto = new BattleActionCost(BA_AUTOSHOT, _attackAction.actor, _attackAction.weapon);
		var costSnap = new BattleActionCost(BA_SNAPSHOT, _attackAction.actor, _attackAction.weapon);
		var costAimed = new BattleActionCost(BA_AIMEDSHOT, _attackAction.actor, _attackAction.weapon);

		testEffect(costAuto);
		testEffect(costSnap);
		testEffect(costAimed);

		// Is the unit willingly waiting outside of weapon's range (e.g. ninja camouflaged in ambush)?
		bool waitIfOutsideWeaponRange = _unit.GeoscapeSoldier != null ? false : _unit.UnitRules.WaitIfOutsideWeaponRange();

		// Do we want to use the extended firing mode scoring?
		bool extendedFireModeChoiceEnabled = _save.Mod.AIExtendedFireModeChoice;
		if (!waitIfOutsideWeaponRange && extendedFireModeChoiceEnabled)
		{
			// Note: this will also check for the weapon's max range
			var costThrow = new BattleActionCost(); // Not actually checked here, just passed to extendedFireModeChoice as a necessary argument
			ExtendedFireModeChoice(costAuto, costSnap, costAimed, costThrow, false);
			return;
		}

		// Do we want to check if the weapon is in range?
		bool aiRespectsMaxRange = _save.Mod.AIRespectMaxRange;
		if (!waitIfOutsideWeaponRange && aiRespectsMaxRange)
		{
			// If we want to check and it's not in range, perhaps we should re-think shooting
			int distanceSq = _unit.Distance3dToPositionSq(_attackAction.target);
			if (_attackAction.weapon.Rules.IsOutOfRange(distanceSq))
			{
				return;
			}
		}

		// vanilla
		if (distance < 4)
		{
			if (costAuto.HaveTU())
			{
				_attackAction.type = BA_AUTOSHOT;
				return;
			}
			if (!costSnap.HaveTU())
			{
				if (costAimed.HaveTU())
				{
					_attackAction.type = BA_AIMEDSHOT;
				}
				return;
			}
			_attackAction.type = BA_SNAPSHOT;
			return;
		}

		if (distance > 12)
		{
			if (costAimed.HaveTU())
			{
				_attackAction.type = BA_AIMEDSHOT;
				return;
			}
			if (distance < 20 && costSnap.HaveTU())
			{
				_attackAction.type = BA_SNAPSHOT;
				return;
			}
		}

		if (costSnap.HaveTU())
		{
			_attackAction.type = BA_SNAPSHOT;
			return;
		}
		if (costAimed.HaveTU())
		{
			_attackAction.type = BA_AIMEDSHOT;
			return;
		}
		if (costAuto.HaveTU())
		{
			_attackAction.type = BA_AUTOSHOT;
		}
	}

	/// <summary>Chooses a firing mode for the AI based on expected number of hits per turn.</summary>
	public void ExtendedFireModeChoice(BattleActionCost costAuto, BattleActionCost costSnap, BattleActionCost costAimed, BattleActionCost costThrow, bool checkLOF = false)
	{
		var attackOptions = new List<BattleActionType>();
		if (costAimed.HaveTU())
		{
			attackOptions.Add(BA_AIMEDSHOT);
		}
		if (costAuto.HaveTU())
		{
			attackOptions.Add(BA_AUTOSHOT);
		}
		if (costSnap.HaveTU())
		{
			attackOptions.Add(BA_SNAPSHOT);
		}
		if (costThrow.HaveTU())
		{
			attackOptions.Add(BA_THROW);
		}

		BattleActionType chosenAction = BA_RETHINK;
		BattleAction testAction = new BattleAction(_attackAction);
		int score = 0;
		foreach (BattleActionType i in attackOptions)
		{
			testAction.type = i;
			if (i == BA_THROW)
			{
				if (_grenade)
				{
					testAction.weapon = _unit.GetGrenadeFromBelt(_save);
				}
				else
				{
					continue;
				}
			}
			else
			{
				testAction.weapon = _attackAction.weapon;
			}
			int newScore = ScoreFiringMode(testAction, _aggroTarget, checkLOF);

			// Add a random factor to the firing mode score based on intelligence
			// An intelligence value of 10 will decrease this random factor to 0
			// Default values for and intelligence value of 0 will make this a 50% to 150% roll
			int intelligenceModifier = _save.Mod.AIFireChoiceIntelCoeff * Math.Max(10 - _unit.Intelligence, 0);
			newScore = newScore * (100 + Rng.Generate(-intelligenceModifier, intelligenceModifier)) / 100;

			// More aggressive units get a modifier to the score for autoshots
			// Aggression = 0 lowers the score, aggro = 1 is no modifier, aggro > 1 bumps up the score by 5% (configurable) for each increment over 1
			if (i == BA_AUTOSHOT)
			{
				newScore = newScore * (100 + (_unit.Aggression - 1) * _save.Mod.AIFireChoiceAggroCoeff) / 100;
			}

			if (newScore > score)
			{
				score = newScore;
				chosenAction = i;
			}

			if (_traceAI)
			{
				Logger.Info($"Evaluate option {(int)i}, score = {newScore}");
			}
		}

		_attackAction.type = chosenAction;
	}

	/// <summary>Evaluates whether to throw a grenade at an enemy (or group of enemies) we can see.</summary>
	public void GrenadeAction()
	{
		// do we have a grenade on our belt?
		BattleItem grenade = _unit.GetGrenadeFromBelt(_save);
		var action = new BattleAction();
		action.weapon = grenade;
		action.type = BA_THROW;
		action.actor = _unit;

		action.UpdateTU();
		action.Time += 4; // 4TUs for picking up the grenade
		action += _unit.GetActionTUs(BA_PRIME, grenade);
		// do we have enough TUs to prime and throw the grenade?
		if (action.HaveTU())
		{
			int radius = grenade.Rules.GetExplosionRadius(BattleActionAttack.GetBeforeShoot(action));
			if (ExplosiveEfficacy(_aggroTarget.Position, _unit, radius, _attackAction.diff, true) != 0)
			{
				action.target = _aggroTarget.Position;
			}
			else if (!GetNodeOfBestEfficacy(action, radius))
			{
				return;
			}
			var shifts = new List<(Position first, int second)>();
			if (grenade.Rules.BattleType == BT_PROXIMITYGRENADE)
			{
				// let's try to not throw the proxy below xcom's feet, otherwise they'll just throw it straight back :)
				if (action.target.x < _save.MapSizeX - 1) shifts.Add((new Position(1, 0, 0), _unit.Distance3dToPositionSq(action.target + new Position(1, 0, 0))));
				if (action.target.y < _save.MapSizeY - 1) shifts.Add((new Position(0, 1, 0), _unit.Distance3dToPositionSq(action.target + new Position(0, 1, 0))));
				if (action.target.x > 0) shifts.Add((new Position(-1, 0, 0), _unit.Distance3dToPositionSq(action.target + new Position(-1, 0, 0))));
				if (action.target.y > 0) shifts.Add((new Position(0, -1, 0), _unit.Distance3dToPositionSq(action.target + new Position(0, -1, 0))));
				//RNG::shuffle(shifts);
				// std::sort is not stable; equal distances keep an unspecified order there. Insertion sort is stable here
				// and is what libstdc++ uses for ranges this short (at most 4 elements), so the order matches GCC.
				SortShifts(shifts);
				// PS: if someone wants to calculate a better target spot (based on multiple enemies, RNG, day of the week or position of the stars), be my guest
			}
			else
			{
				// normal grenade
				shifts.Add((new Position(0, 0, 0), 0));
			}
			Position originVoxel = _save.TileEngine.GetOriginVoxel(action, null);
			foreach ((Position first, int second) shift in shifts)
			{
				Position targetTile = action.target + shift.first;
				Position targetVoxel = targetTile.ToVoxel() + new Position(8, 8, (2 + -_save.GetTile(targetTile).TerrainLevel));
				// are we within range?
				double curve = 0; // C++ passes no curve/voxelType (nullptr)
				int voxelType = 0;
				if (_save.TileEngine.ValidateThrow(action, originVoxel, targetVoxel, _save.Depth, ref curve, ref voxelType))
				{
					_attackAction.weapon = grenade;
					_attackAction.target = targetTile;
					_attackAction.type = BA_THROW;
					_rifle = false;
					_melee = false;
					break;
				}
			}
		}
	}

	/// <summary>
	/// Sorts proximity grenade shifts by distance like libstdc++ <c>std::sort</c> does for at most 16 elements
	/// (plain insertion sort, equal elements keep their order).
	/// </summary>
	internal static void SortShifts(List<(Position first, int second)> shifts)
	{
		for (int i = 1; i < shifts.Count; ++i)
		{
			(Position first, int second) val = shifts[i];
			int j = i;
			while (j > 0 && val.second < shifts[j - 1].second)
			{
				shifts[j] = shifts[j - 1];
				--j;
			}
			shifts[j] = val;
		}
	}

	/// <summary>
	/// Attempts a psionic attack on an enemy we "know of".
	/// Psionic targetting: pick from any of the "exposed" units.
	/// Exposed means they have been previously spotted, and are therefore "known" to the AI,
	/// regardless of whether we can see them or not, because we're psychic.
	/// </summary>
	/// <returns>True if a psionic attack is performed.</returns>
	public bool PsiAction()
	{
		BattleItem item = _unit.GetUtilityWeapon(BT_PSIAMP);
		if (item == null)
		{
			return false;
		}

		const int costLength = 3;
		BattleActionCost[] cost =
		[
			new BattleActionCost(BA_USE, _unit, item),
			new BattleActionCost(BA_PANIC, _unit, item),
			new BattleActionCost(BA_MINDCONTROL, _unit, item),
		];
		bool have = false;
		for (int j = 0; j < costLength; ++j)
		{
			if (cost[j].Time > 0)
			{
				cost[j].Time += (short)_escapeTUs;
				cost[j].Energy += (short)(_escapeTUs / 2);
				have |= cost[j].HaveTU();
			}
		}
		bool LOSRequired = item.Rules.IsLOSRequired;

		_aggroTarget = null;
		// don't let mind controlled soldiers mind control other soldiers.
		if (_unit.OriginalFaction == _unit.Faction
			// and we have the required 25 TUs and can still make it to cover
			&& have
			// and we didn't already do a psi action this round
			&& !_didPsi)
		{
			int weightToAttack = 0;
			BattleActionType typeToAttack = BA_NONE;

			foreach (BattleUnit bu in _save.Units)
			{
				// don't target tanks
				if (bu.Armor.Size == 1 &&
					ValidTarget(bu, true, false) &&
					// they must be player units
					bu.OriginalFaction != _unit.Faction &&
					(!LOSRequired ||
					_unit.VisibleUnits.Contains(bu)))
				{
					BattleUnit victim = bu;
					if (item.Rules.IsOutOfRange(_unit.Distance3dToUnitSq(victim)))
					{
						continue;
					}
					for (int j = 0; j < costLength; ++j)
					{
						// can't use this attack.
						if (!cost[j].HaveTU())
						{
							continue;
						}

						int weightToAttackMe = _save.TileEngine.PsiAttackCalculate(new BattleActionAttack(cost[j].type, _unit, item, item), victim);

						// low chance we hit this target.
						if (weightToAttackMe < 0)
						{
							continue;
						}

						// different bonus per attack.
						if (cost[j].type == BA_MINDCONTROL)
						{
							// target cannot be mind controlled
							if (victim.UnitRules != null && !victim.UnitRules.CanBeMindControlled()) continue;

							int controlOdds = 40;
							int morale = victim.Morale;
							int bravery = victim.ReduceByBravery(10);
							if (bravery > 6)
								controlOdds -= 15;
							if (bravery < 4)
								controlOdds += 15;
							if (morale >= 40)
							{
								if (morale - 10 * bravery < 50)
									controlOdds -= 15;
							}
							else
							{
								controlOdds += 15;
							}
							if (morale == 0)
							{
								controlOdds = 100;
							}
							if (Rng.Percent(controlOdds))
							{
								weightToAttackMe += 60;
							}
							else
							{
								continue;
							}
						}
						else if (cost[j].type == BA_USE)
						{
							if (Rng.Percent(80 - _attackAction.diff * 10)) // Star gods have mercy on us.
							{
								continue;
							}
							var attack = new BattleActionAttack(BA_USE, _unit, item, item);
							int radius = item.Rules.GetExplosionRadius(attack);
							if (radius > 0)
							{
								int efficity = ExplosiveEfficacy(victim.Position, _unit, radius, _attackAction.diff);
								if (efficity != 0)
								{
									weightToAttackMe += 2 * efficity * _intelligence; //bonus for boom boom.
								}
								else
								{
									continue;
								}
							}
							else
							{
								weightToAttackMe += item.Rules.GetPowerBonus(attack);
							}
						}
						else if (cost[j].type == BA_PANIC)
						{
							// target cannot be panicked
							if (victim.UnitRules != null && !victim.UnitRules.CanPanic()) continue;

							weightToAttackMe += 40;
						}

						if (weightToAttackMe > weightToAttack)
						{
							typeToAttack = cost[j].type;
							weightToAttack = weightToAttackMe;
							_aggroTarget = victim;
						}
					}
				}
			}

			if (_aggroTarget == null || weightToAttack == 0) return false;

			if (_visibleEnemies != 0 && _attackAction.weapon != null)
			{
				BattleActionType[] actions =
				[
					BA_AIMEDSHOT,
					BA_AUTOSHOT,
					BA_SNAPSHOT,
					BA_HIT,
				];
				foreach (BattleActionType action in actions)
				{
					BattleItem ammo = _attackAction.weapon.GetAmmoForAction(action);
					if (ammo == null)
					{
						continue;
					}

					int weightPower = ammo.Rules.GetPowerBonus(new BattleActionAttack(action, _attackAction.actor, _attackAction.weapon, ammo));
					if (action == BA_HIT)
					{
						// prefer psi over melee
						weightPower /= 2;
					}
					else
					{
						// prefer machine guns
						weightPower *= _attackAction.weapon.GetActionConf(action).shots;
					}
					if (weightPower >= weightToAttack)
					{
						return false;
					}
				}
			}
			else if (Rng.Generate(35, 155) >= weightToAttack)
			{
				return false;
			}

			if (_traceAI)
			{
				Logger.Info("making a psionic attack this turn");
			}

			_psiAction.type = typeToAttack;
			_psiAction.target = _aggroTarget.Position;
			_psiAction.weapon = item;
			return true;
		}
		return false;
	}

	/// <summary>Performs a melee attack action.</summary>
	public void MeleeAttack()
	{
		_unit.LookAt(_aggroTarget.Position + new Position(_unit.Armor.Size - 1, _unit.Armor.Size - 1, 0), false);
		while (_unit.Status == STATUS_TURNING)
			_unit.Turn();
		if (_traceAI) { Logger.Info($"Attack unit: {_aggroTarget.Id}"); }
		_attackAction.target = _aggroTarget.Position;
		_attackAction.type = BA_HIT;
		_attackAction.weapon = _unit.GetUtilityWeapon(BT_MELEE);
	}

	/// <summary>How much given unit is worth as target of attack.</summary>
	/// <param name="target">The unit to weigh.</param>
	/// <returns>The attack weight.</returns>
	public AIAttackWeight GetTargetAttackWeight(BattleUnit target)
	{
		AIAttackWeight weight = AIAttackWeight.AIW_IGNORED;

		if (target.Faction == _unit.Faction)
		{
			// friendly target have negative weight, used for AoE attacks.
			weight = target.GetAITargetWeightAsFriendly(_save.Mod);
		}
		else if (
			_intelligence < target.GetTurnsSinceSpottedByFaction(_unit.Faction) &&
			(!_unit.IsSniper || target.GetTurnsLeftSpottedForSnipersByFaction(_unit.Faction) == 0))
		{
			// ignore units that we don't "know" about...
			// ... unless we are a sniper and the spotters know about them
			weight = AIAttackWeight.AIW_IGNORED;
		}
		else if (target.Faction == FACTION_HOSTILE || _unit.Faction == FACTION_HOSTILE)
		{
			if (target.Faction == _targetFaction)
			{
				// enemy unit, full weight
				weight = target.GetAITargetWeightAsHostile(_save.Mod);
			}
			else
			{
				// if its not xcom unit then its civilian, less value that xcom
				weight = target.GetAITargetWeightAsHostileCivilians(_save.Mod);
			}
		}
		else if (target.Faction == FACTION_NEUTRAL || _unit.Faction == FACTION_NEUTRAL)
		{
			// if its not alien then its xcom or civilian, humans do not shoot each other, usually...
			weight = target.GetAITargetWeightAsNeutral(_save.Mod);
		}

		weight = (AIAttackWeight)ModScript.ScriptFunc2(
			_unit.Armor.GetScript(ModScript.AiCalculateTargetWeight),
			(int)weight, (int)weight,
			_unit, target, _save
		);

		return weight;
	}

	/// <summary>Validates a target.</summary>
	/// <param name="target">The target we want to validate.</param>
	/// <param name="assessDanger">Do we care if this unit was previously targetted with a grenade?</param>
	/// <param name="includeCivs">Do we include civilians in the threat assessment?</param>
	/// <returns>Whether this target is someone we would like to kill.</returns>
	public bool ValidTarget(BattleUnit target, bool assessDanger, bool includeCivs)
	{
		// ignore units that:
		// 1. are dead/unconscious
		// 2. are dangerous (they have been grenaded)
		// 3. are hostile/neutral units marked as ignored by the AI
		if (target.IsOut ||
			(assessDanger && target.Tile.Dangerous) ||
			(target.Faction != FACTION_PLAYER && target.IsIgnoredByAI))
		{
			return false;
		}

		if (includeCivs)
		{
			return GetTargetAttackWeight(target) > AIAttackWeight.AIW_IGNORED;
		}
		else
		{
			return GetTargetAttackWeight(target) > _save.Mod.AITargetWeightThreatThreshold;
		}
	}

	/// <summary>Checks the alien's reservation setting.</summary>
	public BattleActionType ReserveMode => _reserve;

	/// <summary>
	/// We have a dichotomy on our hands: we have a ranged weapon and melee capability.
	/// let's make a determination on which one we'll be using this round.
	/// </summary>
	public void SelectMeleeOrRanged()
	{
		BattleItem range = _attackAction.weapon;
		BattleItem melee = _unit.GetUtilityWeapon(BT_MELEE);

		if (melee == null || !melee.HaveAnyAmmo())
		{
			// no idea how we got here, but melee is definitely out of the question.
			_melee = false;
			return;
		}
		if (range == null || !range.HaveAnyAmmo())
		{
			_rifle = false;
			return;
		}

		RuleItem meleeRule = melee.Rules;

		int meleeOdds = 10;

		int dmg = _aggroTarget.ReduceByResistance(meleeRule.GetPowerBonus(BattleActionAttack.GetBeforeShoot(BA_HIT, _unit, melee)), meleeRule.DamageType.ResistType);

		if (dmg > 50)
		{
			meleeOdds += (dmg - 50) / 2;
		}
		if (_visibleEnemies > 1)
		{
			meleeOdds -= 20 * (_visibleEnemies - 1);
		}

		if (meleeOdds > 0 && _unit.Health >= 2 * _unit.BaseStats.health / 3)
		{
			if (_unit.Aggression == 0)
			{
				meleeOdds -= 20;
			}
			else if (_unit.Aggression > 1)
			{
				meleeOdds += 10 * _unit.Aggression;
			}

			if (Rng.Percent(meleeOdds))
			{
				_rifle = false;
				_attackAction.weapon = melee;
				_reachableWithAttack = _save.Pathfinding.FindReachable(_unit, new BattleActionCost(BA_HIT, _unit, melee));
				return;
			}
		}
		_melee = false;
	}

	/// <summary>Checks nearby nodes to see if they'd make good grenade targets.</summary>
	/// <param name="action">Contains our details one weapon and user, and we set the target for it here.</param>
	/// <param name="radius">Explosion radius.</param>
	/// <returns>If we found a viable node or not.</returns>
	public bool GetNodeOfBestEfficacy(BattleAction action, int radius)
	{
		int bestScore = 2;
		Position originVoxel = _save.TileEngine.GetSightOriginVoxel(_unit);
		var targetVoxel = new Position();
		foreach (Node node in _save.Nodes)
		{
			if (node.IsDummy)
			{
				continue;
			}
			int dist = Position.Distance2d(node.Position, _unit.Position);
			if (dist <= 20 && dist > radius &&
				_save.TileEngine.CanTargetTile(ref originVoxel, _save.GetTile(node.Position), (int)TilePart.O_FLOOR, ref targetVoxel, _unit, false))
			{
				int nodePoints = 0;
				foreach (BattleUnit bu in _save.Units)
				{
					dist = Position.Distance2d(node.Position, bu.Position);
					if (!bu.IsOut && dist < radius)
					{
						Position targetOriginVoxel = _save.TileEngine.GetSightOriginVoxel(bu);
						if (_save.TileEngine.CanTargetTile(ref targetOriginVoxel, _save.GetTile(node.Position), (int)TilePart.O_FLOOR, ref targetVoxel, bu, false))
						{
							if ((_unit.Faction == FACTION_HOSTILE && bu.Faction != FACTION_HOSTILE) ||
								(_unit.Faction == FACTION_NEUTRAL && bu.Faction == FACTION_HOSTILE))
							{
								if (bu.GetTurnsSinceSpottedByFaction(_unit.Faction) <= _intelligence)
								{
									nodePoints++;
								}
							}
							else
							{
								nodePoints -= 2;
							}
						}
					}
				}
				if (nodePoints > bestScore)
				{
					bestScore = nodePoints;
					action.target = node.Position;
				}
			}
		}
		return bestScore > 2;
	}

	/// <summary>Gets the current targetted unit.</summary>
	public BattleUnit Target => _aggroTarget;

	/// <summary>Frees up the destination node for another Unit to select.</summary>
	public void FreePatrolTarget()
	{
		_toNode?.FreeNode();
	}

	/// <summary>C++ <c>Clamp</c> from fmath.h.</summary>
	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	/// <summary>C++ <c>Clamp</c> from fmath.h.</summary>
	private static double Clamp(double x, double min, double max) => Math.Min(Math.Max(x, min), max);
}
