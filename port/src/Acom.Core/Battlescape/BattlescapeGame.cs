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
// Ported from: src/Battlescape/BattlescapeGame.h, src/Battlescape/BattlescapeGame.cpp
#nullable disable

using System.Text;
using static Acom.BattleActionType;
using static Acom.BattleType;
using static Acom.CursorType;
using static Acom.ItemDamageType;
using static Acom.LightLayers;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

public enum BattleActionMove
{
	BAM_NORMAL = 0,
	BAM_RUN = 1,
	BAM_STRAFE = 2,
	BAM_SNEAK = 3,
	BAM_MISSILE = 4,
}

/// <summary>Cost of a battle action (C++ value type: copy explicitly).</summary>
public class BattleActionCost : RuleItemUseCost
{
	public BattleActionType type;
	public BattleUnit actor;
	public BattleItem weapon;
	/// <summary>If defined, this is a skill action.</summary>
	public RuleSkill skillRules;

	/// <summary>Default constructor.</summary>
	public BattleActionCost()
	{
		type = BA_NONE;
	}

	/// <summary>Constructor from unit.</summary>
	public BattleActionCost(BattleUnit unit)
	{
		type = BA_NONE;
		actor = unit;
	}

	/// <summary>Constructor with update.</summary>
	public BattleActionCost(BattleActionType action, BattleUnit unit, BattleItem item)
	{
		type = action;
		actor = unit;
		weapon = item;
		UpdateTU();
	}

	/// <summary>Copy constructor.</summary>
	public BattleActionCost(BattleActionCost other) : base(other)
	{
		type = other.type;
		actor = other.actor;
		weapon = other.weapon;
		skillRules = other.skillRules;
	}

	/// <summary>Assigns only the cost part (C++ <c>*(RuleItemUseCost*)this = cost</c>).</summary>
	private void SetCost(RuleItemUseCost cost)
	{
		Time = cost.Time;
		Energy = cost.Energy;
		Morale = cost.Morale;
		Health = cost.Health;
		Stun = cost.Stun;
		Mana = cost.Mana;
	}

	/// <summary>Update value of TU and Energy.</summary>
	public void UpdateTU()
	{
		if (actor != null && skillRules != null)
		{
			SetCost(actor.GetActionTUs(type, skillRules));
		}
		else if (actor != null && weapon != null)
		{
			SetCost(actor.GetActionTUs(type, weapon));
		}
		else
		{
			ClearTU();
		}
	}

	/// <summary>Clean up action cost.</summary>
	public void ClearTU()
	{
		SetCost(new RuleItemUseCost());
	}

	/// <summary>Test if action can be performed.</summary>
	/// <param name="message">Optional message with error condition (C++ <c>std::string*</c>; null = no message).</param>
	/// <returns>Unit have enough stats to perform action.</returns>
	public bool HaveTU(ref string message) => HaveTUImpl(ref message, true);

	/// <summary>Test if action can be performed (no message).</summary>
	public bool HaveTU()
	{
		string dummy = null;
		return HaveTUImpl(ref dummy, false);
	}

	private bool HaveTUImpl(ref string message, bool wantMessage)
	{
		if (skillRules == null && Time <= 0)
		{
			//no action, no message
			return false;
		}
		if (actor.TimeUnits < Time)
		{
			if (wantMessage)
			{
				message = "STR_NOT_ENOUGH_TIME_UNITS";
			}
			return false;
		}
		if (actor.Energy < Energy)
		{
			if (wantMessage)
			{
				message = "STR_NOT_ENOUGH_ENERGY";
			}
			return false;
		}
		if (actor.Morale < Morale)
		{
			if (wantMessage)
			{
				message = "STR_NOT_ENOUGH_MORALE";
			}
			return false;
		}
		if (actor.Health <= Health)
		{
			if (wantMessage)
			{
				message = "STR_NOT_ENOUGH_HEALTH";
			}
			return false;
		}
		if (actor.Mana < Mana)
		{
			if (wantMessage)
			{
				message = "STR_NOT_ENOUGH_MANA";
			}
			return false;
		}
		if (actor.Health - actor.Stunlevel <= Stun + Health)
		{
			if (wantMessage)
			{
				message = "STR_NOT_ENOUGH_STUN";
			}
			return false;
		}
		return true;
	}

	/// <summary>Spend cost of action if unit have enough stats.</summary>
	/// <param name="message">Optional message with error condition.</param>
	/// <returns>Action was performed.</returns>
	public bool SpendTU(ref string message)
	{
		if (HaveTU(ref message))
		{
			actor.SpendCost(this);
			return true;
		}
		return false;
	}

	/// <summary>Spend cost of action if unit have enough stats (no message).</summary>
	public bool SpendTU()
	{
		if (HaveTU())
		{
			actor.SpendCost(this);
			return true;
		}
		return false;
	}
}

/// <summary>A battle action (C++ value type: copy with the copy constructor).</summary>
public class BattleAction : BattleActionCost
{
	public Position target;
	public List<Position> waypoints = [];
	public bool targeting;
	public int value;
	public string result = "";
	public bool strafe;
	public bool run;
	public bool sneak;
	public bool ignoreSpottedEnemies;
	public bool kneel;
	public int diff;
	public int autoShotCounter;
	public Position cameraPosition;
	/// <summary>Ignoring newly-spotted units.</summary>
	public bool desperate;
	public int finalFacing;
	public bool finalAction;
	/// <summary>First action of turn, second, etc.?</summary>
	public int number;
	/// <summary>Used to separate waypoint checks between confirm firing mode and the "spray" autoshot.</summary>
	public bool sprayTargeting;
	/// <summary>Preferred origin voxel (centre, left or right).</summary>
	public BattleActionOrigin relativeOrigin = BattleActionOrigin.CENTRE;
	/// <summary>Terrain melee.</summary>
	public int terrainMeleeTilePart;

	/// <summary>Default constructor.</summary>
	public BattleAction()
	{
		target = new Position(-1, -1, -1);
		targeting = false;
		value = 0;
		diff = 0;
		autoShotCounter = 0;
		cameraPosition = new Position(0, 0, -1);
		desperate = false;
		finalFacing = -1;
		finalAction = false;
		number = 0;
		sprayTargeting = false;
	}

	/// <summary>Copy constructor (C++ copies BattleAction by value, e.g. into Projectile).</summary>
	public BattleAction(BattleAction other) : base(other)
	{
		target = other.target;
		waypoints = new List<Position>(other.waypoints);
		targeting = other.targeting;
		value = other.value;
		result = other.result;
		strafe = other.strafe;
		run = other.run;
		sneak = other.sneak;
		ignoreSpottedEnemies = other.ignoreSpottedEnemies;
		kneel = other.kneel;
		diff = other.diff;
		autoShotCounter = other.autoShotCounter;
		cameraPosition = other.cameraPosition;
		desperate = other.desperate;
		finalFacing = other.finalFacing;
		finalAction = other.finalAction;
		number = other.number;
		sprayTargeting = other.sprayTargeting;
		relativeOrigin = other.relativeOrigin;
		terrainMeleeTilePart = other.terrainMeleeTilePart;
	}

	/// <summary>Get move type.</summary>
	public BattleActionMove MoveType => strafe ? BattleActionMove.BAM_STRAFE : run ? BattleActionMove.BAM_RUN : sneak ? BattleActionMove.BAM_SNEAK : BattleActionMove.BAM_NORMAL;
}

/// <summary>Count of different state of units to determine who wins.</summary>
public struct BattlescapeTally
{
	/// <summary>Number of live enemies (aliens and MC'ed soldiers).</summary>
	public int liveAliens;
	/// <summary>Number of live soldiers (only ones that are not MC'ed, including tanks, but not summoned units).</summary>
	public int liveSoldiers;
	/// <summary>Number of live soldiers on entrance tiles.</summary>
	public int inEntrance;
	/// <summary>Number of live soldiers on exit tiles.</summary>
	public int inExit;
	/// <summary>Number of live soldiers in the middle of the battlefield.</summary>
	public int inField;
	/// <summary>Number of live VIPs on entrance tiles.</summary>
	public int vipInEntrance;
	/// <summary>Number of live VIPs on exit tiles.</summary>
	public int vipInExit;
	/// <summary>Number of live VIPs in the middle of the battlefield.</summary>
	public int vipInField;
}

/// <summary>Battlescape game - the core game engine of the battlescape game.</summary>
public class BattlescapeGame
{
	private readonly SavedBattleGame _save;
	private readonly BattlescapeState _parentState;
	/// <summary>State queue; a null entry is an end turn request (as in C++).</summary>
	private readonly List<BattleState> _states = [];
	private readonly List<BattleState> _deleted = [];
	private bool _playerPanicHandled;
	private int _AIActionCounter;
	private readonly BattleAction _currentAction = new();
	private bool _AISecondMove, _playedAggroSound;
	private bool _endTurnRequested;
	private bool _endConfirmationHandled;
	private bool _allEnemiesNeutralized;

	// helper::SingleRun
	private bool _endTurnProcessedDone;
	private bool _triggerProcessedDone;

	private readonly List<InfoboxOKState> _infoboxQueue = [];

	/// <summary>Is debug mode enabled in the battlescape?</summary>
#pragma warning disable CA2211 // public static field mirrors C++ BattlescapeGame::_debugPlay
	public static bool _debugPlay;
#pragma warning restore CA2211

	private static bool TryRun(ref bool done)
	{
		if (done)
		{
			return false;
		}
		done = true;
		return true;
	}

	/// <summary>Initializes all the elements in the Battlescape screen.</summary>
	/// <param name="save">Pointer to the save game.</param>
	/// <param name="parentState">Pointer to the parent battlescape state.</param>
	public BattlescapeGame(SavedBattleGame save, BattlescapeState parentState)
	{
		_save = save;
		_parentState = parentState;
		_playerPanicHandled = true;
		_AIActionCounter = 0;
		_AISecondMove = false;
		_playedAggroSound = false;
		_endTurnRequested = false;
		_endConfirmationHandled = false;
		_allEnemiesNeutralized = false;

		if (_save.IsPreview)
		{
			_allEnemiesNeutralized = true; // just in case
		}

		_currentAction.actor = null;
		_currentAction.targeting = false;
		_currentAction.type = BA_NONE;
		_currentAction.skillRules = null;

		_debugPlay = false;

		CheckForCasualties(null, new BattleActionAttack(), true);
		CancelCurrentAction();
	}

	/// <summary>Checks for units panicking or falling and so on.</summary>
	public int Think()
	{
		int ret = -1;
		// nothing is happening - see if we need some alien AI or units panicking or what have you
		if (_states.Count == 0)
		{
			if (_save.UnitsFalling)
			{
				StatePushFront(new UnitFallBState(this));
				_save.UnitsFalling = false;
				return ret;
			}
			// it's a non player side (ALIENS or CIVILIANS)
			if (_save.Side != FACTION_PLAYER)
			{
				var sideBackup = _save.Side;
				_save.ResetUnitHitStates();
				if (!_debugPlay)
				{
					if (_save.SelectedUnit != null)
					{
						if (!HandlePanickingUnit(_save.SelectedUnit))
						{
							HandleAI(_save.SelectedUnit);

							// calculate AI progress
							int units = 0;
							int total = 0;
							foreach (var bu in _save.Units)
							{
								if (bu.Faction == sideBackup && !bu.IsOut)
								{
									units++;
									total += (bu.ReselectAllowed() && (bu.BaseStats.tu > 0)) ? bu.TimeUnits * 100 / bu.BaseStats.tu : 0;
								}
							}
							ret = units > 0 ? total / units : 0;
						}
					}
					else
					{
						if (_save.SelectNextPlayerUnit(true, _AISecondMove) == null)
						{
							if (!_save.DebugMode)
							{
								_endTurnRequested = true;
								StatePushBack(null); // end AI turn
							}
							else
							{
								_save.SelectNextPlayerUnit();
								_debugPlay = true;
							}
						}
					}
				}
			}
			else
			{
				// it's a player side && we have not handled all panicking units
				if (!_playerPanicHandled)
				{
					_playerPanicHandled = HandlePanickingPlayer();
					_save.BattleState.UpdateSoldierInfo();
				}
			}
		}

		return ret;
	}

	/// <summary>Initializes the Battlescape game.</summary>
	public void Init()
	{
		if (_save.Side == FACTION_PLAYER && _save.Turn > 1)
		{
			_playerPanicHandled = false;
		}
	}

	/// <summary>Handles the processing of the AI states of a unit.</summary>
	/// <param name="unit">Pointer to a unit.</param>
	public void HandleAI(BattleUnit unit)
	{
		var ss = new StringBuilder();

		if (unit.TimeUnits <= 5)
		{
			unit.DontReselect();
		}
		if (_AIActionCounter >= 2 || !unit.ReselectAllowed() || unit.TurnsSinceStunned == 0) //stun check for restoring OXC behavior that AI does not attack after waking up even having full TU
		{
			if (_save.SelectNextPlayerUnit(true, _AISecondMove) == null)
			{
				if (!_save.DebugMode)
				{
					_endTurnRequested = true;
					StatePushBack(null); // end AI turn
				}
				else
				{
					_save.SelectNextPlayerUnit();
					_debugPlay = true;
				}
			}
			if (_save.SelectedUnit != null)
			{
				_parentState.UpdateSoldierInfo();
				Map.Camera.CenterOnPosition(_save.SelectedUnit.Position);
				if (_save.SelectedUnit.Id <= unit.Id)
				{
					_AISecondMove = true;
				}
			}
			_AIActionCounter = 0;
			return;
		}

		unit.Visible = false; //Possible TODO: check number of player unit observers, then hide the unit if no one can see it. Should then be able to skip the next FOV call.

		_save.TileEngine.CalculateFOV(unit.Position, 1, false); // might need this populate _visibleUnit for a newly-created alien.
		// it might also help chryssalids realize they've zombified someone and need to move on
		// it should also hide units when they've killed the guy spotting them
		// it's also for good luck

		AIModule ai = unit.AIModule;
		if (ai == null)
		{
			// for some reason the unit had no AI routine assigned..
			unit.AIModule = new AIModule(_save, unit, null);
			ai = unit.AIModule;
		}
		_AIActionCounter++;
		if (_AIActionCounter == 1)
		{
			_playedAggroSound = false;
			unit.SetHiding(false);
			if (Options.traceAI) { Logger.Info($"#{unit.Id}--{unit.Type}"); }
		}

		var action = new BattleAction();
		action.actor = unit;
		action.number = _AIActionCounter;
		unit.Think(action);

		if (action.type == BA_RETHINK)
		{
			_parentState.Debug("Rethink");
			unit.Think(action);
		}

		_AIActionCounter = action.number;
		BattleItem weapon = unit.GetMainHandWeapon();
		bool pickUpWeaponsMoreActively = unit.PickUpWeaponsMoreActively;
		bool weaponPickedUp = false;
		bool walkToItem = false;
		if (weapon == null || !weapon.HaveAnyAmmo())
		{
			if (unit.OriginalFaction != FACTION_PLAYER)
			{
				if ((unit.OriginalFaction == FACTION_HOSTILE && unit.VisibleUnits.Count == 0) || pickUpWeaponsMoreActively)
				{
					weaponPickedUp = FindItem(action, pickUpWeaponsMoreActively, ref walkToItem);
				}
			}
		}
		if (pickUpWeaponsMoreActively && weaponPickedUp)
		{
			// you have just picked up a weapon... use it if you can!
			_parentState.Debug("Re-Rethink");
			unit.AIModule.SetWeaponPickedUp();
			unit.Think(action);
		}

		if (unit.Charging != null)
		{
			if (unit.HasAggroSound && !_playedAggroSound)
			{
				Mod.GetSoundByDepth((uint)_save.Depth, (uint)unit.RandomAggroSound).Play(-1, Map.GetSoundAngle(unit.Position));
				_playedAggroSound = true;
			}
		}
		if (action.type == BA_WALK)
		{
			ss.Append($"Walking to ({action.target.x},{action.target.y},{action.target.z})");
			_parentState.Debug(ss.ToString());

			var targetTile = _save.GetTile(action.target);
			if (targetTile != null)
			{
				_save.Pathfinding.Calculate(action.actor, action.target, BattleActionMove.BAM_NORMAL);
			}
			if (_save.Pathfinding.StartDirection != -1)
			{
				StatePushBack(new UnitWalkBState(this, action));
			}
			else if (walkToItem)
			{
				// impossible to walk to this tile, don't try to pick up an item from there for the rest of the turn
				targetTile.Dangerous = true;
			}
		}

		if (action.type == BA_SNAPSHOT || action.type == BA_AUTOSHOT || action.type == BA_AIMEDSHOT || action.type == BA_THROW || action.type == BA_HIT || action.type == BA_MINDCONTROL || action.type == BA_USE || action.type == BA_PANIC || action.type == BA_LAUNCH)
		{
			// Note: C++ ss.clear() only resets the stream flags, the text keeps accumulating.
			// The action type is an Uint8 enum and is streamed as a character.
			ss.Append($"Attack type={(char)(byte)action.type} target=({action.target.x},{action.target.y},{action.target.z}) weapon={action.weapon.Rules.Type}");
			_parentState.Debug(ss.ToString());
			action.UpdateTU();
			if (action.type == BA_MINDCONTROL || action.type == BA_PANIC || action.type == BA_USE)
			{
				StatePushBack(new PsiAttackBState(this, action));
			}
			else
			{
				StatePushBack(new UnitTurnBState(this, action));
				if (action.type == BA_HIT)
				{
					StatePushBack(new MeleeAttackBState(this, action));
				}
				else
				{
					StatePushBack(new ProjectileFlyBState(this, action));
				}
			}
		}

		if (action.type == BA_NONE)
		{
			_parentState.Debug("Idle");
			_AIActionCounter = 0;
			if (_save.SelectNextPlayerUnit(true, _AISecondMove) == null)
			{
				if (!_save.DebugMode)
				{
					_endTurnRequested = true;
					StatePushBack(null); // end AI turn
				}
				else
				{
					_save.SelectNextPlayerUnit();
					_debugPlay = true;
				}
			}
			if (_save.SelectedUnit != null)
			{
				_parentState.UpdateSoldierInfo();
				Map.Camera.CenterOnPosition(_save.SelectedUnit.Position);
				if (_save.SelectedUnit.Id <= unit.Id)
				{
					_AISecondMove = true;
				}
			}
		}
	}

	/// <summary>Toggles the Kneel/Standup status of the unit.</summary>
	/// <param name="bu">Pointer to a unit.</param>
	/// <returns>If the action succeeded.</returns>
	public bool Kneel(BattleUnit bu)
	{
		int tu = bu.KneelChangeCost;
		if (bu.Armor.AllowsKneeling(bu.Type == "SOLDIER") && !bu.IsFloating && ((!bu.IsKneeled && _save.KneelReserved) || CheckReservedTU(bu, tu, 0)))
		{
			var kneel = new BattleAction();
			kneel.type = BA_KNEEL;
			kneel.actor = bu;
			kneel.Time = unchecked((short)tu);
			if (kneel.SpendTU())
			{
				bu.Kneel(!bu.IsKneeled);
				// kneeling or standing up can reveal new terrain or units. I guess.
				TileEngine.CalculateFOV(bu.Position, 1, false); //Update unit FOV for everyone through this position, skip tiles.
				_parentState.UpdateSoldierInfo(); //This also updates the tile FOV of the unit, hence why it's skipped above.
				TileEngine.CheckReactionFire(bu, kneel);
				return true;
			}
			else
			{
				_parentState.Warning("STR_NOT_ENOUGH_TIME_UNITS");
			}
		}
		return false;
	}

	/// <summary>Ends the turn.</summary>
	private void EndTurn()
	{
		_debugPlay = _save.DebugMode && _parentState.Game.IsCtrlPressed() && (_save.Side != FACTION_NEUTRAL);
		_currentAction.type = BA_NONE;
		_currentAction.skillRules = null;
		Map.Waypoints.Clear();
		_currentAction.waypoints.Clear();
		_parentState.ShowLaunchButton(false);
		_currentAction.targeting = false;
		_AISecondMove = false;

		if (TryRun(ref _triggerProcessedDone))
		{
			if (_save.TileEngine.CloseUfoDoors() != 0 && Mod.SLIDING_DOOR_CLOSE != -1)
			{
				Mod.GetSoundByDepth((uint)_save.Depth, (uint)Mod.SLIDING_DOOR_CLOSE).Play(); // ufo door closed
			}

			// if all grenades explode we remove items that expire on that turn too.
			var forRemoval = new List<(BattleItem item, ExplosionBState expl)>();
			bool exploded = false;

			// check for hot grenades on the ground
			if (_save.Side != FACTION_NEUTRAL && !_save.IsPreview)
			{
				foreach (BattleItem item in _save.Items)
				{
					if (item.IsOwnerIgnored)
					{
						continue;
					}

					RuleItem rule = item.Rules;
					Tile tile = item.Tile;
					BattleUnit unit = item.Owner;
					if (tile == null && unit != null && item.FuseTimer != -1 && !_allEnemiesNeutralized)
					{
						int explodeAnyway = rule.GetExplodeInventory(Mod);
						if (explodeAnyway >= 2 || (explodeAnyway == 1 && item.Slot.Type != InventoryType.INV_HAND))
						{
							tile = unit.Tile;
						}
					}
					if (tile != null)
					{
						if (item.FuseTimeEvent())
						{
							if (rule.BattleType == BT_GRENADE) // it's a grenade to explode now
							{
								Position p = tile.Position.ToVoxel() + new Position(8, 8, -tile.TerrainLevel + (unit != null ? unit.Height / 2 : 0));
								forRemoval.Add((null, new ExplosionBState(this, new LastPositions(p), BattleActionAttack.GetBeforeShoot(BA_TRIGGER_TIMED_GRENADE, unit, item), null)));
								exploded = true;
							}
							else
							{
								forRemoval.Add((item, null));
							}
						}
					}
				}
				foreach (var (item, expl) in forRemoval)
				{
					if (expl != null)
					{
						StatePushNext(expl);
					}
					else if (item.IsSpecialWeapon)
					{
						// we can't remove special weapons, disable the fuse at least
						item.FuseTimer = -1;
					}
					else
					{
						_save.RemoveItem(item);
					}
				}
				if (exploded)
				{
					StatePushBack(null);
					return;
				}
			}
		}

		// check for terrain explosions
		Tile t = _save.TileEngine.CheckForTerrainExplosions();
		if (t != null)
		{
			Position p = t.Position.ToVoxel();
			StatePushNext(new ExplosionBState(this, new LastPositions(p), new BattleActionAttack(), t));
			StatePushBack(null);
			return;
		}

		if (TryRun(ref _endTurnProcessedDone))
		{
			if (_save.Side != FACTION_NEUTRAL)
			{
				foreach (BattleItem item in _save.Items)
				{
					if (item.IsOwnerIgnored)
					{
						continue;
					}
					item.FuseEndTurnUpdate();
				}
			}

			_save.EndTurn();
			t = _save.TileEngine.CheckForTerrainExplosions();
			if (t != null)
			{
				Position p = t.Position.ToVoxel();
				StatePushNext(new ExplosionBState(this, new LastPositions(p), new BattleActionAttack(), t));
				StatePushBack(null);
				return;
			}
		}

		_triggerProcessedDone = false;
		_endTurnProcessedDone = false;

		if (_save.Side == FACTION_PLAYER)
		{
			SetupCursor();
		}
		else
		{
			Map.SetCursorType(CT_NONE);
		}

		CheckForCasualties(null, new BattleActionAttack(), false, false);

		// fires could have been started, stopped or smoke could reveal/conceal units.
		_save.TileEngine.CalculateLighting(LL_FIRE, TileEngine.invalid, 0, true);
		_save.TileEngine.RecalculateFOV();

		// Calculate values
		BattlescapeTally tally = _save.BattleGame.TallyUnits();

		// if all units from either faction are killed - the mission is over.
		if (_save.AllObjectivesDestroyed() && _save.ObjectiveType == SpecialTileType.MUST_DESTROY)
		{
			_parentState.FinishBattle(false, tally.liveSoldiers);
			return;
		}
		if (_save.TurnLimit > 0 && _save.Turn > _save.TurnLimit)
		{
			switch (_save.ChronoTrigger)
			{
				case ChronoTrigger.FORCE_ABORT:
					_save.SetAborted(true);
					_parentState.FinishBattle(true, tally.inExit);
					return;
				case ChronoTrigger.FORCE_WIN:
				case ChronoTrigger.FORCE_WIN_SURRENDER:
					_parentState.FinishBattle(false, tally.liveSoldiers);
					return;
				case ChronoTrigger.FORCE_LOSE:
				default:
					// force mission failure
					_save.SetAborted(true);
					_parentState.FinishBattle(false, 0);
					return;
			}
		}

		if (tally.liveAliens > 0 && tally.liveSoldiers > 0)
		{
			ShowInfoBoxQueue();

			_parentState.UpdateSoldierInfo();

			if (PlayableUnitSelected())
			{
				Map.Camera.CenterOnPosition(_save.SelectedUnit.Position);
				SetupCursor();
			}
		}

		// "escort the VIPs" missions don't end when all aliens are neutralized
		// objective type MUST_DESTROY was already handled above
		bool killingAllAliensIsNotEnough = (_save.VIPSurvivalPercentage > 0 && _save.VIPEscapeType != EscapeType.ESCAPE_NONE);

		bool battleComplete = (!killingAllAliensIsNotEnough && tally.liveAliens == 0) || tally.liveSoldiers == 0;

		if ((_save.Side != FACTION_NEUTRAL || battleComplete)
			&& _endTurnRequested)
		{
			_parentState.Game.PushState(new NextTurnState(_save, _parentState));
		}
		_endTurnRequested = false;
	}

	/// <summary>Copies a kill record (C++ copy constructor of BattleUnitKills).</summary>
	private static BattleUnitKills CopyKills(BattleUnitKills k)
	{
		return new BattleUnitKills
		{
			name = k.name,
			type = k.type,
			rank = k.rank,
			race = k.race,
			weapon = k.weapon,
			weaponAmmo = k.weaponAmmo,
			faction = k.faction,
			status = k.status,
			mission = k.mission,
			turn = k.turn,
			id = k.id,
			side = k.side,
			bodypart = k.bodypart,
		};
	}

	/// <summary>Checks for casualties and adjusts morale accordingly.</summary>
	/// <param name="damageType">Need to know this, for a HE explosion there is an instant death.</param>
	/// <param name="attack">This is needed for credits for the kill.</param>
	/// <param name="hiddenExplosion">Set to true for the explosions of UFO Power sources at start of battlescape.</param>
	/// <param name="terrainExplosion">Set to true for the explosions of terrain.</param>
	public void CheckForCasualties(RuleDamageType damageType, BattleActionAttack attack, bool hiddenExplosion = false, bool terrainExplosion = false)
	{
		BattleUnit origMurderer = attack.attacker;
		// If the victim was killed by the murderer's death explosion, fetch who killed the murderer and make HIM the murderer!
		if (origMurderer != null && (origMurderer.SpecialAbility == (int)SpecialAbility.SPECAB_EXPLODEONDEATH || origMurderer.SpecialAbility == (int)SpecialAbility.SPECAB_BURN_AND_EXPLODE)
			&& origMurderer.Status == STATUS_DEAD && origMurderer.MurdererId != 0)
		{
			foreach (var bu in _save.Units)
			{
				if (bu.Id == origMurderer.MurdererId)
				{
					origMurderer = bu;
					break;
				}
			}
		}

		// Fetch the murder weapon
		string tempWeapon = "STR_WEAPON_UNKNOWN", tempAmmo = "STR_WEAPON_UNKNOWN";
		if (origMurderer != null)
		{
			if (attack.weapon_item != null)
			{
				tempWeapon = attack.weapon_item.Rules.Name;
			}
			if (attack.damage_item != null)
			{
				// If the secondary melee data is used, represent this by setting the ammo to "__GUNBUTT".
				// Note: BT_MELEE items use their normal attack data rather than 'melee' data. So their 'ammo' should be the weapon itself.
				// (The following condition should match what is used in ExplosionBState::init to choose the damage power and type.)
				if (attack.type == BA_HIT && attack.damage_item.Rules.BattleType != BT_MELEE)
				{
					tempAmmo = "__GUNBUTT";
				}
				else
				{
					tempAmmo = attack.damage_item.Rules.Name;
				}
			}
		}

		foreach (var victim in _save.Units)
		{
			if (victim.IsIgnored) continue;
			BattleUnit murderer = origMurderer;

			var killStat = new BattleUnitKills();
			killStat.mission = _parentState.Game.SavedGame.MissionStatistics.Count;
			killStat.SetTurn(_save.Turn, _save.Side);
			killStat.SetUnitStats(victim);
			killStat.faction = victim.OriginalFaction;
			killStat.side = victim.FatalShotSide;
			killStat.bodypart = victim.FatalShotBodyPart;
			killStat.id = victim.Id;
			killStat.weapon = tempWeapon;
			killStat.weaponAmmo = tempAmmo;

			// Determine murder type
			if (victim.Status != STATUS_DEAD)
			{
				if (victim.Health <= 0)
				{
					killStat.status = STATUS_DEAD;
				}
				else if (victim.Stunlevel >= victim.Health && victim.Status != STATUS_UNCONSCIOUS)
				{
					killStat.status = STATUS_UNCONSCIOUS;
				}
			}

			// Assume that, in absence of a murderer and an explosion, the laster unit to hit the victim is the murderer.
			// Possible causes of death: bleed out, fire.
			// Possible causes of unconsciousness: wounds, smoke.
			// Assumption : The last person to hit the victim is the murderer.
			if (murderer == null && !terrainExplosion)
			{
				foreach (var bu in _save.Units)
				{
					if (bu.Id == victim.MurdererId)
					{
						murderer = bu;
						killStat.weapon = victim.MurdererWeapon;
						killStat.weaponAmmo = victim.MurdererWeaponAmmo;
						break;
					}
				}
			}

			if (murderer != null && killStat.status != STATUS_IGNORE_ME)
			{
				if (murderer.Faction == FACTION_PLAYER && murderer.OriginalFaction != FACTION_PLAYER)
				{
					// This must be a mind controlled unit. Find out who mind controlled him and award the kill to that unit.
					foreach (var bu in _save.Units)
					{
						if (bu.Id == murderer.MindControllerId && bu.GeoscapeSoldier != null)
						{
							if (!victim.IsCosmetic)
							{
								bu.Statistics.kills.Add(CopyKills(killStat));
								if (victim.Faction == FACTION_HOSTILE)
								{
									bu.Statistics.slaveKills++;
								}
							}
							victim.MurdererId = bu.Id;
							break;
						}
					}
				}
				else if (!murderer.Statistics.DuplicateEntry(killStat.status, victim.Id))
				{
					if (!victim.IsCosmetic)
					{
						murderer.Statistics.kills.Add(CopyKills(killStat));
					}
					victim.MurdererId = murderer.Id;
				}
			}

			bool noSound = false;
			if (victim.Status != STATUS_DEAD)
			{
				if (victim.Health <= 0)
				{
					int moraleLossModifierWhenKilled = _save.GetMoraleLossModifierWhenKilled(victim);

					if (murderer != null)
					{
						murderer.AddKillCount();
						victim.KilledBy(murderer.Faction);
						int modifier = murderer.Faction == FACTION_PLAYER ? _save.GetFactionMoraleModifier(true) : 100;

						// if there is a known murderer, he will get a morale bonus if he is of a different faction (what with neutral?)
						if ((victim.OriginalFaction == FACTION_PLAYER && murderer.Faction == FACTION_HOSTILE) ||
							(victim.OriginalFaction == FACTION_HOSTILE && murderer.Faction == FACTION_PLAYER))
						{
							murderer.MoraleChange(20 * modifier / 100);
						}
						// murderer will get a penalty with friendly fire
						if (victim.OriginalFaction == murderer.OriginalFaction)
						{
							// morale loss by friendly fire
							murderer.MoraleChange(-(2000 * moraleLossModifierWhenKilled / modifier / 100));
						}
						if (victim.OriginalFaction == FACTION_NEUTRAL)
						{
							if (murderer.OriginalFaction == FACTION_PLAYER)
							{
								// morale loss by xcom killing civilians
								murderer.MoraleChange(-(1000 * moraleLossModifierWhenKilled / modifier / 100));
							}
							else
							{
								murderer.MoraleChange(10);
							}
						}
					}

					if (victim.Faction != FACTION_NEUTRAL)
					{
						int modifier = _save.GetUnitMoraleModifier(victim);
						int loserMod = _save.GetFactionMoraleModifier(victim.OriginalFaction != FACTION_HOSTILE);
						int winnerMod = _save.GetFactionMoraleModifier(victim.OriginalFaction == FACTION_HOSTILE);
						foreach (var bu in _save.Units)
						{
							if (!bu.IsOut)
							{
								// the losing squad all get a morale loss
								if (bu.OriginalFaction == victim.OriginalFaction)
								{
									// morale loss by losing a team member (not counting mind-controlled units)
									int bravery = bu.ReduceByBravery(10);
									bu.MoraleChange(-(modifier * moraleLossModifierWhenKilled * 200 * bravery / loserMod / 100 / 100));

									if (victim.Faction == FACTION_HOSTILE && murderer != null)
									{
										murderer.TurnsSinceSpotted = 0;
									}
								}
								// the winning squad all get a morale increase
								else
								{
									bu.MoraleChange(10 * winnerMod / 100);
								}
							}
						}
					}
					if (damageType != null)
					{
						StatePushNext(new UnitDieBState(this, victim, damageType, noSound));
					}
					else
					{
						if (hiddenExplosion)
						{
							// this is instant death from UFO power sources, without screaming sounds
							noSound = true;
							StatePushNext(new UnitDieBState(this, victim, Mod.GetDamageType(DT_HE), noSound));
						}
						else
						{
							if (terrainExplosion)
							{
								// terrain explosion
								StatePushNext(new UnitDieBState(this, victim, Mod.GetDamageType(DT_HE), noSound));
							}
							else
							{
								// no murderer, and no terrain explosion, must be fatal wounds
								StatePushNext(new UnitDieBState(this, victim, Mod.GetDamageType(DT_NONE), noSound)); // DT_NONE = STR_HAS_DIED_FROM_A_FATAL_WOUND
							}
						}
					}
					// one of our own died, record the murderer instead of the victim
					if (victim.GeoscapeSoldier != null)
					{
						victim.Statistics.KIA = true;
						BattleUnitKills deathStat = CopyKills(killStat);
						if (murderer != null)
						{
							deathStat.SetUnitStats(murderer);
							deathStat.faction = murderer.OriginalFaction;
						}
						_parentState.Game.SavedGame.KillSoldier(false, victim.GeoscapeSoldier, deathStat);
					}
				}
				else if (victim.Stunlevel >= victim.Health && victim.Status != STATUS_UNCONSCIOUS)
				{
					// morale change when an enemy is stunned (only for the first time!)
					if (Mod.StunningImprovesMorale && murderer != null && !victim.Statistics.wasUnconcious)
					{
						if ((victim.OriginalFaction == FACTION_PLAYER && murderer.Faction == FACTION_HOSTILE) ||
							(victim.OriginalFaction == FACTION_HOSTILE && murderer.Faction == FACTION_PLAYER))
						{
							// the murderer gets a morale bonus if he is of a different faction (excluding neutrals)
							murderer.MoraleChange(20);

							foreach (var winner in _save.Units)
							{
								if (!winner.IsOut && winner.OriginalFaction == murderer.OriginalFaction)
								{
									// the winning squad gets a morale increase (the losing squad is NOT affected)
									winner.MoraleChange(10);
								}
							}
						}
					}

					victim.Statistics.wasUnconcious = true;
					noSound = true;
					StatePushNext(new UnitDieBState(this, victim, Mod.GetDamageType(DT_NONE), noSound)); // no damage type used there
				}
				else
				{
					// piggyback of cleanup after script that change move type
					if (victim.HaveNoFloorBelow() && victim.MovementType != MovementType.MT_FLY)
					{
						_save.AddFallingUnit(victim);
					}
				}
			}
		}

		BattleUnit selected = _save.SelectedUnit;
		if (_save.Side == FACTION_PLAYER)
		{
			_parentState.ResetUiButton();

			if (selected != null && !selected.IsOut)
			{
				_parentState.UpdateUiButton(selected);
			}
		}
	}

	/// <summary>Shows the infoboxes in the queue (if any).</summary>
	private void ShowInfoBoxQueue()
	{
		foreach (var infoboxOKState in _infoboxQueue)
		{
			_parentState.Game.PushState(infoboxOKState);
		}

		_infoboxQueue.Clear();
	}

	/// <summary>Sets up a mission complete notification.</summary>
	public void MissionComplete()
	{
		Game game = _parentState.Game;
		if (game.Mod.GetDeployment(_save.MissionType) != null)
		{
			string missionComplete = game.Mod.GetDeployment(_save.MissionType).ObjectivePopup;
			if (!string.IsNullOrEmpty(missionComplete))
			{
				_infoboxQueue.Add(new InfoboxOKState(game.Language.GetString(missionComplete)));
			}
		}
	}

	/// <summary>Handles the result of non target actions, like priming a grenade.</summary>
	public void HandleNonTargetAction()
	{
		if (!_currentAction.targeting)
		{
			string error = "";
			_currentAction.cameraPosition = new Position(0, 0, -1);
			if (!string.IsNullOrEmpty(_currentAction.result))
			{
				_parentState.Warning(_currentAction.result);
				_currentAction.result = "";
			}
			else if (_currentAction.type == BA_PRIME && _currentAction.value > -1)
			{
				if (_currentAction.SpendTU(ref error))
				{
					_parentState.Warning(_currentAction.weapon.Rules.PrimeActionMessage);
					_currentAction.weapon.FuseTimer = _currentAction.value;
					PlaySound(_currentAction.weapon.Rules.PrimeSound); // prime sound
					_save.TileEngine.CalculateLighting(LL_UNITS, _currentAction.actor.Position);
					_save.TileEngine.CalculateFOV(_currentAction.actor.Position, _currentAction.weapon.VisibilityUpdateRange, false);
				}
				else
				{
					_parentState.Warning(error);
				}
			}
			else if (_currentAction.type == BA_UNPRIME)
			{
				if (_currentAction.SpendTU(ref error))
				{
					_parentState.Warning(_currentAction.weapon.Rules.UnprimeActionMessage);
					_currentAction.weapon.FuseTimer = -1;
					PlaySound(_currentAction.weapon.Rules.UnprimeSound); // unprime sound
					_save.TileEngine.CalculateLighting(LL_UNITS, _currentAction.actor.Position);
					_save.TileEngine.CalculateFOV(_currentAction.actor.Position, _currentAction.weapon.VisibilityUpdateRange, false);
				}
				else
				{
					_parentState.Warning(error);
				}
			}
			else if (_currentAction.type == BA_USE)
			{
				TileEngine.UpdateGameStateAfterScript(BattleActionAttack.GetBeforeShoot(_currentAction), TileEngine.invalid);
			}
			else if (_currentAction.type == BA_HIT)
			{
				if (_currentAction.HaveTU(ref error))
				{
					StatePushBack(new MeleeAttackBState(this, _currentAction));
				}
				else
				{
					_parentState.Warning(error);
				}
			}
			if (_currentAction.type != BA_HIT) // don't clear the action type if we're meleeing, let the melee action state take care of that
			{
				_currentAction.type = BA_NONE;
			}
			_parentState.UpdateSoldierInfo();
		}

		SetupCursor();
	}

	/// <summary>Sets the cursor according to the selected action.</summary>
	public void SetupCursor()
	{
		if (_currentAction.targeting)
		{
			if (_currentAction.type == BA_THROW)
			{
				Map.SetCursorType(CT_THROW);
			}
			else if (_currentAction.type == BA_MINDCONTROL || _currentAction.type == BA_PANIC || _currentAction.type == BA_USE)
			{
				Map.SetCursorType(CT_PSI);
			}
			else if (_currentAction.type == BA_LAUNCH)
			{
				Map.SetCursorType(CT_WAYPOINT);
			}
			else
			{
				Map.SetCursorType(CT_AIM);
			}
		}
		else if (_currentAction.type != BA_HIT)
		{
			_currentAction.actor = _save.SelectedUnit;
			if (_currentAction.actor != null)
			{
				Map.SetCursorType(CT_NORMAL, _currentAction.actor.Armor.Size);
			}
			else
			{
				Map.SetCursorType(CT_NORMAL);
			}
		}
	}

	/// <summary>
	/// Determines whether a playable unit is selected. Normally only player side units can be selected, but in debug mode one can play with aliens too :)
	/// Is used to see if stats can be displayed.
	/// </summary>
	public bool PlayableUnitSelected()
	{
		return _save.SelectedUnit != null && (_save.Side == FACTION_PLAYER || _save.DebugMode);
	}

	/// <summary>Gives time slice to the front state.</summary>
	public void HandleState()
	{
		if (_states.Count != 0)
		{
			// end turn request?
			if (_states[0] == null)
			{
				_states.RemoveAt(0);
				EndTurn();
				return;
			}
			else
			{
				_states[0].Think();
			}
			Map.Invalidate(); // redraw map
		}
	}

	/// <summary>Pushes a state to the front of the queue and starts it.</summary>
	/// <param name="bs">Battlestate.</param>
	public void StatePushFront(BattleState bs)
	{
		_states.Insert(0, bs);
		bs.Init();
	}

	/// <summary>Pushes a state as the next state after the current one.</summary>
	/// <param name="bs">Battlestate.</param>
	public void StatePushNext(BattleState bs)
	{
		if (_states.Count == 0)
		{
			_states.Insert(0, bs);
			bs.Init();
		}
		else
		{
			_states.Insert(1, bs);
		}
	}

	/// <summary>Pushes a state to the back.</summary>
	/// <param name="bs">Battlestate (null = end turn request).</param>
	public void StatePushBack(BattleState bs)
	{
		if (_states.Count == 0)
		{
			_states.Insert(0, bs);
			// end turn request?
			if (_states[0] == null)
			{
				_states.RemoveAt(0);
				EndTurn();
				return;
			}
			else
			{
				bs.Init();
			}
		}
		else
		{
			_states.Add(bs);
		}
	}

	/// <summary>
	/// Removes the current state.
	/// This is a very important function. It is called by a BattleState (walking, projectile is flying, explosions,...) at the moment this state has finished its action.
	/// Here we check the result of that action and do all the aftermath.
	/// The state is popped off the list.
	/// </summary>
	public void PopState()
	{
		if (Options.traceAI)
		{
			Logger.Info($"BattlescapeGame::popState() #{_AIActionCounter} with {(_save.SelectedUnit != null ? _save.SelectedUnit.TimeUnits : -9999)} TU");
		}
		bool actionFailed = false;

		if (_states.Count == 0) return;

		var first = _states[0];
		var action = new BattleAction(first.Action);

		if (action.actor != null && !string.IsNullOrEmpty(action.result) && action.actor.Faction == FACTION_PLAYER
			&& _playerPanicHandled && (_save.Side == FACTION_PLAYER || _debugPlay))
		{
			_parentState.Warning(action.result);
			actionFailed = true;
		}
		_deleted.Add(first);
		_states.RemoveAt(0);
		first.Deinit();

		// handle the end of this unit's actions
		if (action.actor != null && NoActionsPending(action.actor))
		{
			if (action.actor.Faction == FACTION_PLAYER)
			{
				if (_save.Side == FACTION_PLAYER)
				{
					// after throwing the cursor returns to default cursor, after shooting it stays in targeting mode and the player can shoot again in the same mode (autoshot,snap,aimed)
					if ((action.type == BA_THROW || action.type == BA_LAUNCH) && !actionFailed)
					{
						// clean up the waypoints
						if (action.type == BA_LAUNCH)
						{
							_currentAction.waypoints.Clear();
						}

						CancelCurrentAction(true);
					}
					_parentState.Game.Cursor.Visible = true;
					SetupCursor();
				}
			}
			else
			{
				if (_save.Side != FACTION_PLAYER && !_debugPlay)
				{
					// AI does three things per unit, before switching to the next, or it got killed before doing the second thing
					if (_AIActionCounter > 2 || _save.SelectedUnit == null || _save.SelectedUnit.IsOut)
					{
						_AIActionCounter = 0;
						if (_states.Count == 0 && _save.SelectNextPlayerUnit(true) == null)
						{
							if (!_save.DebugMode)
							{
								_endTurnRequested = true;
								StatePushBack(null); // end AI turn
							}
							else
							{
								_save.SelectNextPlayerUnit();
								_debugPlay = true;
							}
						}
						if (_save.SelectedUnit != null)
						{
							Map.Camera.CenterOnPosition(_save.SelectedUnit.Position);
						}
					}
				}
				else if (_debugPlay)
				{
					_parentState.Game.Cursor.Visible = true;
					SetupCursor();
				}
			}
		}

		if (_states.Count != 0)
		{
			// end turn request?
			if (_states[0] == null)
			{
				while (_states.Count != 0)
				{
					if (_states[0] == null)
						_states.RemoveAt(0);
					else
						break;
				}
				if (_states.Count == 0)
				{
					EndTurn();
					return;
				}
				else
				{
					_states.Add(null);
				}
			}
			// init the next state in queue
			_states[0].Init();
		}

		// the currently selected unit died or became unconscious or disappeared inexplicably
		if (_save.SelectedUnit == null || _save.SelectedUnit.IsOut)
		{
			CancelCurrentAction();
			Map.SetCursorType(CT_NORMAL, 1);
			_parentState.Game.Cursor.Visible = true;
			if (_save.Side == FACTION_PLAYER)
				_save.SelectedUnit = null;
			else
				_save.SelectNextPlayerUnit(true, true);
		}
		_parentState.UpdateSoldierInfo();
	}

	/// <summary>Determines whether there are any actions pending for the given unit.</summary>
	/// <param name="bu">BattleUnit.</param>
	/// <returns>True if there are no actions pending.</returns>
	private bool NoActionsPending(BattleUnit bu)
	{
		if (_states.Count == 0) return true;

		foreach (var battleState in _states)
		{
			if (battleState != null && battleState.Action.actor == bu)
				return false;
		}

		return true;
	}

	/// <summary>Sets the timer interval for think() calls of the state.</summary>
	/// <param name="interval">An interval in ms.</param>
	public void SetStateInterval(uint interval)
	{
		_parentState.SetStateInterval(interval);
	}

	/// <summary>Checks against reserved time units and energy units.</summary>
	/// <param name="bu">Pointer to the unit.</param>
	/// <param name="tu">Number of time units to check.</param>
	/// <param name="energy">Number of energy units to check.</param>
	/// <param name="justChecking">True to suppress error messages, false otherwise.</param>
	/// <returns>Whether or not we got enough time units.</returns>
	public bool CheckReservedTU(BattleUnit bu, int tu, int energy, bool justChecking = false)
	{
		var cost = new BattleActionCost();
		cost.actor = bu;
		cost.type = _save.TUReserved; // avoid changing _tuReserved in this method
		cost.weapon = bu.GetMainHandWeapon(false); // check TUs against slowest weapon if we have two weapons

		if (_save.Side != bu.Faction || _save.Side == FACTION_NEUTRAL)
		{
			return tu <= bu.TimeUnits;
		}

		if (_save.Side == FACTION_HOSTILE && !_debugPlay) // aliens reserve TUs as a percentage rather than just enough for a single action.
		{
			AIModule ai = bu.AIModule;
			if (ai != null)
			{
				cost.type = ai.ReserveMode;
			}
			cost.UpdateTU();
			cost.Energy = unchecked((short)(cost.Energy + energy));
			cost.Time = unchecked((short)tu); //override original
			switch (cost.type)
			{
				case BA_SNAPSHOT: cost.Time = unchecked((short)(cost.Time + (bu.BaseStats.tu / 3))); break; // 33%
				case BA_AUTOSHOT: cost.Time = unchecked((short)(cost.Time + ((bu.BaseStats.tu / 5) * 2))); break; // 40%
				case BA_AIMEDSHOT: cost.Time = unchecked((short)(cost.Time + (bu.BaseStats.tu / 2))); break; // 50%
				default: break;
			}
			return cost.Time <= 0 || cost.HaveTU();
		}

		cost.UpdateTU();
		// if the weapon has no autoshot, reserve TUs for snapshot
		if (cost.Time == 0 && cost.type == BA_AUTOSHOT)
		{
			cost.type = BA_SNAPSHOT;
			cost.UpdateTU();
		}
		// likewise, if we don't have a snap shot available, try aimed.
		if (cost.Time == 0 && cost.type == BA_SNAPSHOT)
		{
			cost.type = BA_AIMEDSHOT;
			cost.UpdateTU();
		}
		int tuKneel = (_save.KneelReserved && !bu.IsKneeled && bu.Armor.AllowsKneeling(bu.Type == "SOLDIER")) ? bu.KneelDownCost : 0;
		// no aimed shot available? revert to none.
		if (cost.Time == 0 && cost.type == BA_AIMEDSHOT)
		{
			if (tuKneel > 0)
			{
				cost.type = BA_KNEEL;
			}
			else
			{
				return true;
			}
		}

		cost.Time = unchecked((short)(cost.Time + tuKneel));

		//current TU is less that required for reserved shoot, we can't reserved anything.
		if (!cost.HaveTU() && !justChecking)
		{
			return true;
		}

		cost.Time = unchecked((short)(cost.Time + tu));
		cost.Energy = unchecked((short)(cost.Energy + energy));

		if ((cost.type != BA_NONE || _save.KneelReserved) && !cost.HaveTU())
		{
			if (!justChecking)
			{
				if (tuKneel != 0)
				{
					switch (cost.type)
					{
						case BA_KNEEL: _parentState.Warning("STR_TIME_UNITS_RESERVED_FOR_KNEELING"); break;
						default: _parentState.Warning("STR_TIME_UNITS_RESERVED_FOR_KNEELING_AND_FIRING"); break;
					}
				}
				else
				{
					switch (_save.TUReserved)
					{
						case BA_SNAPSHOT: _parentState.Warning("STR_TIME_UNITS_RESERVED_FOR_SNAP_SHOT"); break;
						case BA_AUTOSHOT: _parentState.Warning("STR_TIME_UNITS_RESERVED_FOR_AUTO_SHOT"); break;
						case BA_AIMEDSHOT: _parentState.Warning("STR_TIME_UNITS_RESERVED_FOR_AIMED_SHOT"); break;
						default: break;
					}
				}
			}
			return false;
		}

		return true;
	}

	/// <summary>Picks the first soldier that is panicking.</summary>
	/// <returns>True when all panicking is over.</returns>
	private bool HandlePanickingPlayer()
	{
		foreach (var bu in _save.Units)
		{
			if (bu.Faction == FACTION_PLAYER &&
				bu.OriginalFaction == FACTION_PLAYER &&
				HandlePanickingUnit(bu))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Common function for handling panicking units.</summary>
	/// <returns>False when unit not in panicking mode.</returns>
	private bool HandlePanickingUnit(BattleUnit unit)
	{
		UnitStatus status = unit.Status;
		if (status != STATUS_PANICKING && status != STATUS_BERSERK) return false;
		_save.SelectedUnit = unit;
		_parentState.Map.SetCursorType(CT_NONE);

		// play panic/berserk sounds first
		bool soundPlayed = false;
		{
			List<int> sounds = [];
			if (unit.UnitRules != null)
			{
				// aliens, civilians, xcom HWPs
				if (status == STATUS_PANICKING)
					sounds = unit.UnitRules.PanicSounds;
				else
					sounds = unit.UnitRules.BerserkSounds;
			}
			else if (unit.GeoscapeSoldier != null)
			{
				// xcom soldiers (male/female)
				if (unit.GeoscapeSoldier.Gender == SoldierGender.GENDER_MALE)
				{
					if (status == STATUS_PANICKING)
						sounds = unit.GeoscapeSoldier.Rules.MalePanicSounds;
					else
						sounds = unit.GeoscapeSoldier.Rules.MaleBerserkSounds;
				}
				else
				{
					if (status == STATUS_PANICKING)
						sounds = unit.GeoscapeSoldier.Rules.FemalePanicSounds;
					else
						sounds = unit.GeoscapeSoldier.Rules.FemaleBerserkSounds;
				}
			}
			if (sounds.Count != 0)
			{
				soundPlayed = true;
				if (sounds.Count > 1)
					PlaySound(sounds[Rng.Generate(0, sounds.Count - 1)]);
				else
					PlaySound(sounds[0]);
			}
		}

		// show a little infobox with the name of the unit and "... is panicking"
		Game game = _parentState.Game;
		if (unit.Visible || !Options.noAlienPanicMessages)
		{
			Map.Camera.CenterOnPosition(unit.Position);
			if (status == STATUS_PANICKING)
			{
				game.PushState(new InfoboxState(game.Language.GetString("STR_HAS_PANICKED", unit.Gender).Arg(unit.GetName(game.Language))));
			}
			else
			{
				game.PushState(new InfoboxState(game.Language.GetString("STR_HAS_GONE_BERSERK", unit.Gender).Arg(unit.GetName(game.Language))));
			}
		}
		else if (soundPlayed)
		{
			// simulate a small pause by using an invisible infobox
			game.PushState(new InfoboxState(""));
		}

		bool flee = Rng.Percent(50);
		var ba = new BattleAction();
		ba.actor = unit;
		if (status == STATUS_PANICKING && flee) // 1/2 chance to freeze and 1/2 chance try to flee, STATUS_BERSERK is handled in the panic state.
		{
			BattleItem item = unit.RightHandWeapon;
			if (item != null)
			{
				DropItem(unit.Position, item, true);
			}
			item = unit.LeftHandWeapon;
			if (item != null)
			{
				DropItem(unit.Position, item, true);
			}
			// let's try a few times to get a tile to run to.
			for (int i = 0; i < 20; i++)
			{
				// C++ evaluation order of the constructor arguments is unspecified; GCC (and MSVC) evaluate them
				// right to left, so the y offset is rolled first (Clang would roll x first).
				int ry = Rng.Generate(-5, 5);
				int rx = Rng.Generate(-5, 5);
				ba.target = new Position(unit.Position.x + rx, unit.Position.y + ry, unit.Position.z);

				if (i >= 10 && ba.target.z > 0) // if we've had more than our fair share of failures, try going down.
				{
					ba.target.z--;
					if (i >= 15 && ba.target.z > 0) // still failing? try further down.
					{
						ba.target.z--;
					}
				}
				if (_save.GetTile(ba.target) != null) // sanity check the tile.
				{
					_save.Pathfinding.Calculate(ba.actor, ba.target, ba.MoveType);
					if (_save.Pathfinding.StartDirection != -1) // sanity check the path.
					{
						StatePushBack(new UnitWalkBState(this, ba));
						break;
					}
				}
			}
		}
		// Time units can only be reset after everything else occurs
		StatePushBack(new UnitPanicBState(this, ba.actor));

		return true;
	}

	/// <summary>Cancels the current action the user had selected (firing, throwing,..)</summary>
	/// <param name="bForce">Force the action to be cancelled.</param>
	/// <returns>Whether an action was cancelled or not.</returns>
	public bool CancelCurrentAction(bool bForce = false)
	{
		bool bPreviewed = Options.battleNewPreviewPath != PathPreview.PATH_NONE;

		if (_save.Pathfinding.RemovePreview() && bPreviewed) return true;

		if (_states.Count == 0 || bForce)
		{
			if (_currentAction.targeting)
			{
				if (_currentAction.type == BA_LAUNCH && _currentAction.waypoints.Count != 0)
				{
					_currentAction.waypoints.RemoveAt(_currentAction.waypoints.Count - 1);
					if (Map.Waypoints.Count != 0)
					{
						Map.Waypoints.RemoveAt(Map.Waypoints.Count - 1);
					}
					if (_currentAction.waypoints.Count == 0)
					{
						_parentState.ShowLaunchButton(false);
					}
					return true;
				}
				else if (_currentAction.type == BA_AUTOSHOT && _currentAction.sprayTargeting && _currentAction.waypoints.Count != 0)
				{
					_currentAction.waypoints.RemoveAt(_currentAction.waypoints.Count - 1);
					if (Map.Waypoints.Count != 0)
					{
						Map.Waypoints.RemoveAt(Map.Waypoints.Count - 1);
					}

					if (_currentAction.waypoints.Count == 0)
					{
						_currentAction.sprayTargeting = false;
						Map.Waypoints.Clear();
					}
					return true;
				}
				else
				{
					if (Options.battleConfirmFireMode && _currentAction.waypoints.Count != 0)
					{
						_currentAction.waypoints.RemoveAt(_currentAction.waypoints.Count - 1);
						Map.Waypoints.RemoveAt(Map.Waypoints.Count - 1);
						return true;
					}
					_currentAction.targeting = false;
					_currentAction.type = BA_NONE;
					_currentAction.skillRules = null;
					_currentAction.result = ""; // TODO
					SetupCursor();
					_parentState.Game.Cursor.Visible = true;
					return true;
				}
			}
		}
		else if (_states.Count != 0 && _states[0] != null)
		{
			_states[0].Cancel();
			return true;
		}

		return false;
	}

	/// <summary>Cancels all selected user actions.</summary>
	public void CancelAllActions()
	{
		_save.Pathfinding.RemovePreview();

		_currentAction.waypoints.Clear();
		Map.Waypoints.Clear();
		_parentState.ShowLaunchButton(false);

		_currentAction.targeting = false;
		_currentAction.type = BA_NONE;
		_currentAction.skillRules = null;
		_currentAction.result = ""; // TODO
		SetupCursor();
		_parentState.Game.Cursor.Visible = true;
	}

	/// <summary>Gets a pointer to access action members directly.</summary>
	public BattleAction CurrentAction => _currentAction;

	/// <summary>Determines whether an action is currently going on?</summary>
	public bool IsBusy => _states.Count != 0;

	/// <summary>Activates primary action (left click).</summary>
	/// <param name="pos">Position on the map.</param>
	public void PrimaryAction(Position pos)
	{
		bool bPreviewed = Options.battleNewPreviewPath != PathPreview.PATH_NONE;

		Map.ResetObstacles();

		if (_currentAction.targeting && _save.SelectedUnit != null)
		{
			if (_currentAction.type == BA_LAUNCH)
			{
				int maxWaypoints = _currentAction.weapon.CurrentWaypoints;
				if (_currentAction.waypoints.Count < maxWaypoints || maxWaypoints == -1)
				{
					_parentState.ShowLaunchButton(true);
					_currentAction.waypoints.Add(pos);
					Map.Waypoints.Add(pos);
				}
			}
			else if (_currentAction.sprayTargeting) // Special "spray" autoshot that allows placing shots between waypoints
			{
				int maxWaypoints = _currentAction.weapon.Rules.SprayWaypoints;
				if (_currentAction.waypoints.Count >= maxWaypoints ||
					(_save.IsCtrlPressed(true) && _save.IsShiftPressed(true)) ||
					(!Options.battleConfirmFireMode && _currentAction.waypoints.Count == maxWaypoints - 1))
				{
					// If we're firing early, pick one last waypoint.
					if (_currentAction.waypoints.Count < maxWaypoints)
					{
						_currentAction.waypoints.Add(pos);
						Map.Waypoints.Add(pos);
					}

					Map.SetCursorType(CT_NONE);

					// Populate the action's waypoints with the positions we want to fire at
					// Start from the last shot and move to the first, since we'll be using the last element first and then pop_back()
					int numberOfShots = _currentAction.weapon.Rules.ConfigAuto.shots;
					int numberOfWaypoints = _currentAction.waypoints.Count;
					_currentAction.waypoints.Clear();
					for (int i = numberOfShots - 1; i > 0; --i)
					{
						// Evenly space shots along the waypoints according to number of waypoints and the number of shots
						// Use voxel positions to get more uniform spacing
						// We add Position(8, 8, 12) to target middle of tile
						int waypointIndex = Math.Max(0, Math.Min(numberOfWaypoints - 1, i * (numberOfWaypoints - 1) / (numberOfShots - 1)));
						Position previousWaypoint = Map.Waypoints[waypointIndex].ToVoxel() + TileEngine.voxelTileCenter;
						Position nextWaypoint = Map.Waypoints[Math.Min(Map.Waypoints.Count - 1, waypointIndex + 1)].ToVoxel() + TileEngine.voxelTileCenter;
						Position targetPos = default;
						targetPos.x = (short)(previousWaypoint.x + (nextWaypoint.x - previousWaypoint.x) * (i * (numberOfWaypoints - 1) % (numberOfShots - 1)) / (numberOfShots - 1));
						targetPos.y = (short)(previousWaypoint.y + (nextWaypoint.y - previousWaypoint.y) * (i * (numberOfWaypoints - 1) % (numberOfShots - 1)) / (numberOfShots - 1));
						targetPos.z = (short)(previousWaypoint.z + (nextWaypoint.z - previousWaypoint.z) * (i * (numberOfWaypoints - 1) % (numberOfShots - 1)) / (numberOfShots - 1));

						_currentAction.waypoints.Add(targetPos);
					}
					_currentAction.waypoints.Add(Map.Waypoints[0].ToVoxel() + TileEngine.voxelTileCenter);
					_currentAction.target = _currentAction.waypoints[^1].ToTile();

					Map.Waypoints.Clear();
					_parentState.Game.Cursor.Visible = false;
					_currentAction.cameraPosition = Map.Camera.MapOffset;
					_states.Add(new ProjectileFlyBState(this, _currentAction));
					StatePushFront(new UnitTurnBState(this, _currentAction));
					_currentAction.sprayTargeting = false;
					_currentAction.waypoints.Clear();
				}
				else if (_currentAction.waypoints.Count < maxWaypoints)
				{
					_currentAction.waypoints.Add(pos);
					Map.Waypoints.Add(pos);
				}
			}
			else if (_currentAction.type == BA_AUTOSHOT &&
				_currentAction.weapon.Rules.SprayWaypoints > 0 &&
				_save.IsCtrlPressed(true) &&
				_save.IsShiftPressed(true) &&
				_currentAction.waypoints.Count == 0) // Starts the spray autoshot targeting
			{
				_currentAction.sprayTargeting = true;
				_currentAction.waypoints.Add(pos);
				Map.Waypoints.Add(pos);
			}
			else if (_currentAction.type == BA_USE && _currentAction.weapon.Rules.BattleType == BT_MINDPROBE)
			{
				var targetUnit = _save.SelectUnit(pos);
				if (targetUnit != null && targetUnit.Faction != _save.SelectedUnit.Faction && targetUnit.Visible)
				{
					if (!_currentAction.weapon.Rules.IsLOSRequired ||
						(_currentAction.actor.Faction == FACTION_PLAYER && targetUnit.Faction != FACTION_HOSTILE) ||
						_currentAction.actor.VisibleUnits.Contains(targetUnit))
					{
						string error = "";
						if (_currentAction.SpendTU(ref error))
						{
							_parentState.Game.Mod.GetSoundByDepth((uint)_save.Depth, (uint)_currentAction.weapon.Rules.HitSound).Play(-1, Map.GetSoundAngle(pos));
							_parentState.Game.PushState(new UnitInfoState(targetUnit, _parentState, false, true));
							CancelCurrentAction();
						}
						else
						{
							_parentState.Warning(error);
						}
					}
					else
					{
						_parentState.Warning("STR_LINE_OF_SIGHT_REQUIRED");
					}
				}
			}
			else if ((_currentAction.type == BA_PANIC || _currentAction.type == BA_MINDCONTROL || _currentAction.type == BA_USE) && _currentAction.weapon.Rules.BattleType == BT_PSIAMP)
			{
				var targetUnit = _save.SelectUnit(pos);
				if (targetUnit != null)
				{
					UnitFaction targetFaction = targetUnit.Faction;
					UnitFaction attackerFaction = _currentAction.actor.Faction;

					bool knowTarget = true;
					if (attackerFaction == FACTION_PLAYER || attackerFaction == FACTION_NEUTRAL)
					{
						knowTarget = targetUnit.Visible;
					}
					else if (attackerFaction == FACTION_HOSTILE) // for debugging
					{
						if (targetFaction != FACTION_HOSTILE)
						{
							knowTarget = _currentAction.actor.AIModule != null
								&& _currentAction.actor.AIModule.ValidTarget(targetUnit, false, true); // different flags than AI used because AI consider strategy
						}
						else
						{
							knowTarget = true;
						}
					}

					bool psiTargetAllowed = knowTarget && _currentAction.weapon.Rules.IsTargetAllowed(targetFaction, attackerFaction);
					if (_currentAction.type == BA_MINDCONTROL && attackerFaction == targetFaction)
					{
						// no mind controlling allies, unwanted side effects
						psiTargetAllowed = false;
					}
					else if (_currentAction.type == BA_PANIC && targetUnit.UnitRules != null && !targetUnit.UnitRules.CanPanic())
					{
						psiTargetAllowed = false;
					}
					else if (_currentAction.type == BA_MINDCONTROL && targetUnit.UnitRules != null && !targetUnit.UnitRules.CanBeMindControlled())
					{
						psiTargetAllowed = false;
					}

					if (psiTargetAllowed)
					{
						_currentAction.UpdateTU();
						_currentAction.target = pos;
						if (!_currentAction.weapon.Rules.IsLOSRequired ||
							(attackerFaction == FACTION_PLAYER && targetFaction != FACTION_HOSTILE) ||
							_currentAction.actor.VisibleUnits.Contains(targetUnit))
						{
							// get the sound/animation started
							Map.SetCursorType(CT_NONE);
							_parentState.Game.Cursor.Visible = false;
							_currentAction.cameraPosition = Map.Camera.MapOffset;
							StatePushBack(new PsiAttackBState(this, _currentAction));
						}
						else
						{
							_parentState.Warning("STR_LINE_OF_SIGHT_REQUIRED");
						}
					}
					else if (knowTarget)
					{
						//TODO: add `warning` that we can't target given unit
					}
				}
			}
			else if (Options.battleConfirmFireMode && (_currentAction.waypoints.Count == 0 || pos != _currentAction.waypoints[0]))
			{
				_currentAction.waypoints.Clear();
				_currentAction.waypoints.Add(pos);
				Map.Waypoints.Clear();
				Map.Waypoints.Add(pos);
			}
			else
			{
				_currentAction.target = pos;
				Map.SetCursorType(CT_NONE);

				if (Options.battleConfirmFireMode)
				{
					_currentAction.waypoints.Clear();
					Map.Waypoints.Clear();
				}

				_parentState.Game.Cursor.Visible = false;
				_currentAction.cameraPosition = Map.Camera.MapOffset;
				_states.Add(new ProjectileFlyBState(this, _currentAction));
				StatePushFront(new UnitTurnBState(this, _currentAction)); // first of all turn towards the target
			}
		}
		else
		{
			_currentAction.actor = _save.SelectedUnit;
			BattleUnit unit = _save.SelectUnit(pos);
			if (unit != null && unit == _save.SelectedUnit && (unit.Visible || _debugPlay))
			{
				PlayUnitResponseSound(unit, 3); // "annoyed" sound
			}
			if (unit != null && unit != _save.SelectedUnit && (unit.Visible || _debugPlay))
			{
				//  -= select unit =-
				if (unit.Faction == _save.Side)
				{
					_save.SelectedUnit = unit;
					_parentState.UpdateSoldierInfo();
					CancelCurrentAction();
					SetupCursor();
					_currentAction.actor = unit;
					PlayUnitResponseSound(unit, 0); // "select unit" sound
				}
			}
			else if (PlayableUnitSelected())
			{
				bool isCtrlPressed = Options.strafe && _save.IsCtrlPressed(true);
				bool isAltPressed = Options.strafe && _save.IsAltPressed(true);
				bool isShiftPressed = _save.IsShiftPressed(true);
				if (bPreviewed && (
					_currentAction.target != pos ||
					_save.Pathfinding.IsModifierCtrlUsed != isCtrlPressed ||
					_save.Pathfinding.IsModifierAltUsed != isAltPressed))
				{
					_save.Pathfinding.RemovePreview();
				}
				_currentAction.target = pos;
				_save.Pathfinding.Calculate(_currentAction.actor, _currentAction.target, BattleActionMove.BAM_NORMAL); // precalculate move

				_currentAction.strafe = false;
				_currentAction.run = false;
				_currentAction.sneak = false;

				if (isCtrlPressed)
				{
					if (_save.Pathfinding.Path.Count > 1 || isAltPressed)
					{
						_currentAction.run = _save.SelectedUnit.Armor.AllowsRunning(_save.SelectedUnit.IsSmallUnit);
					}
					else
					{
						_currentAction.strafe = _save.SelectedUnit.Armor.AllowsStrafing(_save.SelectedUnit.IsSmallUnit);
					}
				}
				else if (isAltPressed)
				{
					_currentAction.sneak = _save.SelectedUnit.Armor.AllowsSneaking(_save.SelectedUnit.IsSmallUnit);
				}

				// recalculate path after setting new move types
				if (BattleActionMove.BAM_NORMAL != _currentAction.MoveType)
				{
					_save.Pathfinding.Calculate(_currentAction.actor, _currentAction.target, _currentAction.MoveType);
				}

				// if running or shifting, ignore spotted enemies (i.e. don't stop)
				_currentAction.ignoreSpottedEnemies = (_currentAction.run && Mod.EXTENDED_RUNNING_COST) || isShiftPressed;

				if (bPreviewed && !_save.Pathfinding.PreviewPath() && _save.Pathfinding.StartDirection != -1)
				{
					_save.Pathfinding.RemovePreview();
					bPreviewed = false;
				}

				if (!bPreviewed && _save.Pathfinding.StartDirection != -1)
				{
					//  -= start walking =-
					Map.SetCursorType(CT_NONE);
					_parentState.Game.Cursor.Visible = false;
					StatePushBack(new UnitWalkBState(this, _currentAction));
					PlayUnitResponseSound(_currentAction.actor, 1); // "start moving" sound
				}
			}
		}
	}

	/// <summary>Activates secondary action (right click).</summary>
	/// <param name="pos">Position on the map.</param>
	public void SecondaryAction(Position pos)
	{
		//  -= turn to or open door =-
		_currentAction.target = pos;
		_currentAction.actor = _save.SelectedUnit;
		_currentAction.strafe = Options.strafe && _save.IsCtrlPressed(true) && _save.SelectedUnit.TurretType > -1;
		StatePushBack(new UnitTurnBState(this, _currentAction));
	}

	/// <summary>Handler for the blaster launcher button.</summary>
	public void LaunchAction()
	{
		_parentState.ShowLaunchButton(false);
		Map.Waypoints.Clear();
		_currentAction.target = _currentAction.waypoints[0];
		Map.SetCursorType(CT_NONE);
		_parentState.Game.Cursor.Visible = false;
		_currentAction.cameraPosition = Map.Camera.MapOffset;
		_states.Add(new ProjectileFlyBState(this, _currentAction));
		StatePushFront(new UnitTurnBState(this, _currentAction)); // first of all turn towards the target
	}

	/// <summary>Handler for the psi button.</summary>
	public void PsiButtonAction()
	{
		if (_currentAction.waypoints.Count != 0) // in case waypoints were set with a blaster launcher, avoid accidental misclick
			return;
		BattleItem item = _save.SelectedUnit.GetSpecialWeapon(BT_PSIAMP);
		_currentAction.type = BA_NONE;
		if (item.Rules.CostPanic.Time > 0)
		{
			_currentAction.type = BA_PANIC;
		}
		else if (item.Rules.CostUse.Time > 0)
		{
			_currentAction.type = BA_USE;
		}
		if (_currentAction.type != BA_NONE)
		{
			_currentAction.targeting = true;
			_currentAction.weapon = item;
			_currentAction.UpdateTU();
			SetupCursor();
		}
	}

	/// <summary>Handler for the psi attack result message.</summary>
	public void PsiAttackMessage(BattleActionAttack attack, BattleUnit victim)
	{
		if (victim != null)
		{
			Game game = Save.BattleState.Game;
			if (attack.attacker.Faction == FACTION_HOSTILE)
			{
				// show a little infobox with the name of the unit and "... is under alien control"
				if (attack.type == BA_MINDCONTROL)
					game.PushState(new InfoboxState(game.Language.GetString("STR_IS_UNDER_ALIEN_CONTROL", victim.Gender).Arg(victim.GetName(game.Language))));
			}
			else
			{
				// show a little infobox if it's successful
				if (attack.type == BA_PANIC)
					game.PushState(new InfoboxState(game.Language.GetString("STR_MORALE_ATTACK_SUCCESSFUL")));
				else if (attack.type == BA_MINDCONTROL)
				{
					if (attack.weapon_item.Rules.ConvertToCivilian() && victim.OriginalFaction == FACTION_HOSTILE)
						game.PushState(new InfoboxState(game.Language.GetString("STR_MIND_CONTROL_SUCCESSFUL_ALT")));
					else
						game.PushState(new InfoboxState(game.Language.GetString("STR_MIND_CONTROL_SUCCESSFUL")));
				}
				Save.BattleState.UpdateSoldierInfo();
			}
		}
	}

	/// <summary>Moves a unit up or down.</summary>
	/// <param name="unit">The unit.</param>
	/// <param name="dir">Direction DIR_UP or DIR_DOWN.</param>
	public void MoveUpDown(BattleUnit unit, int dir)
	{
		_currentAction.target = unit.Position;
		if (dir == Pathfinding.DIR_UP)
		{
			_currentAction.target.z++;
		}
		else
		{
			_currentAction.target.z--;
		}
		Map.SetCursorType(CT_NONE);
		_parentState.Game.Cursor.Visible = false;
		if (_save.SelectedUnit.IsKneeled)
		{
			Kneel(_save.SelectedUnit);
		}
		_save.Pathfinding.Calculate(_currentAction.actor, _currentAction.target, _currentAction.MoveType);
		StatePushBack(new UnitWalkBState(this, _currentAction));
	}

	/// <summary>Requests the end of the turn (waits for explosions etc to really end the turn).</summary>
	public void RequestEndTurn(bool askForConfirmation)
	{
		CancelCurrentAction();

		if (askForConfirmation)
		{
			if (_endConfirmationHandled)
				return;

			// check for fatal wounds
			int soldiersWithFatalWounds = 0;
			foreach (var bu in _save.Units)
			{
				if (bu.OriginalFaction == FACTION_PLAYER && bu.Status != STATUS_DEAD && bu.FatalWounds > 0)
					soldiersWithFatalWounds++;
			}

			if (soldiersWithFatalWounds > 0)
			{
				// confirm end of turn/mission
				_parentState.Game.PushState(new ConfirmEndMissionState(_save, soldiersWithFatalWounds, this));
				_endConfirmationHandled = true;
			}
			else
			{
				if (!_endTurnRequested)
				{
					_endTurnRequested = true;
					StatePushBack(null);
				}
			}
		}
		else
		{
			if (!_endTurnRequested)
			{
				_endTurnRequested = true;
				StatePushBack(null);
			}
		}
	}

	/// <summary>Sets the TU reserved type.</summary>
	/// <param name="tur">A BattleActionType.</param>
	public void SetTUReserved(BattleActionType tur)
	{
		_save.TUReserved = tur;
	}

	/// <summary>Drops an item to the floor and affects it with gravity.</summary>
	/// <param name="position">Position to spawn the item.</param>
	/// <param name="item">Pointer to the item.</param>
	/// <param name="removeItem">Bool whether to remove the item from the owner.</param>
	/// <param name="updateLight">Whether to update lighting.</param>
	public void DropItem(Position position, BattleItem item, bool removeItem = false, bool updateLight = true)
	{
		_save.TileEngine.ItemDrop(_save.GetTile(position), item, updateLight);
	}

	/// <summary>Converts a unit into a unit of another type.</summary>
	/// <param name="unit">The unit to convert.</param>
	/// <returns>Pointer to the new unit.</returns>
	public BattleUnit ConvertUnit(BattleUnit unit)
	{
		_parentState.ResetUiButton();

		return Save.ConvertUnit(unit);
	}

	/// <summary>Spawns a new unit mid-battle.</summary>
	/// <param name="item">Item that spawns the unit.</param>
	public void SpawnNewUnit(BattleItem item)
	{
		SpawnNewUnit(new BattleActionAttack(BA_NONE, null, item, item), item.Tile.Position);
	}

	/// <summary>Spawns a new unit mid-battle.</summary>
	/// <param name="attack">BattleActionAttack that calls to spawn the unit.</param>
	/// <param name="position">Tile position to try and spawn unit on.</param>
	public void SpawnNewUnit(BattleActionAttack attack, Position position)
	{
		if (attack.damage_item == null) // no idea how this happened, but make sure we have an item
			return;

		RuleItem item = attack.damage_item.Rules;
		Unit type = item.SpawnUnit;

		if (type == null)
			return;

		int chance = item.SpawnUnitChance;
		var conf = attack.weapon_item != null ? attack.weapon_item.GetActionConf(attack.type) : null;
		if (conf != null)
		{
			chance = conf.ammoSpawnUnitChanceOverride == -1 ? chance : conf.ammoSpawnUnitChanceOverride; // useIntNullable
		}

		if (!Rng.Percent(chance))
		{
			return;
		}

		BattleUnit owner = attack.attacker;
		if (owner == null)
		{
			owner = attack.damage_item.Owner;
			if (owner == null)
			{
				owner = attack.damage_item.PreviousOwner;
			}
		}

		// Check which faction the new unit will be
		UnitFaction faction;
		if (item.SpawnUnitFaction == FACTION_NONE && owner != null)
		{
			faction = owner.Faction;
		}
		else
		{
			switch ((int)item.SpawnUnitFaction)
			{
				case 0:
					faction = FACTION_PLAYER;
					break;
				case 1:
					faction = FACTION_HOSTILE;
					break;
				case 2:
					faction = FACTION_NEUTRAL;
					break;
				default:
					faction = FACTION_HOSTILE;
					break;
			}
		}

		if (_save.IsPreview && faction != FACTION_PLAYER)
		{
			return;
		}

		// Create the unit
		BattleUnit newUnit = _save.CreateTempUnit(type, faction);

		// Validate the position for the unit, checking if there's a surrounding tile if necessary
		int checkDirection = attack.attacker != null ? (attack.attacker.Direction + 4) % 8 : 0;
		bool positionValid = TileEngine.IsPositionValidForUnit(ref position, newUnit, true, checkDirection);
		if (positionValid) // Place the unit and initialize it in the battlescape
		{
			int unitDirection = attack.attacker != null ? attack.attacker.Direction : Rng.Generate(0, 7);
			// If this is a tank, arm it with its weapon
			if (Mod.GetItem(newUnit.Type) != null && Mod.GetItem(newUnit.Type).IsFixed)
			{
				RuleItem newUnitWeapon = Mod.GetItem(newUnit.Type);
				if (!_save.IsPreview)
				{
					_save.CreateItemForUnit(newUnitWeapon, newUnit, true);
					if (newUnitWeapon.VehicleClipAmmo != null)
					{
						RuleItem ammo = newUnitWeapon.VehicleClipAmmo;
						BattleItem ammoItem = _save.CreateItemForUnit(ammo, newUnit);
						if (ammoItem != null)
						{
							ammoItem.AmmoQuantity = newUnitWeapon.VehicleClipSize;
						}
					}
				}
				newUnit.TurretType = newUnitWeapon.TurretType;
			}

			// Pick the item sets if the unit has builtInWeaponSets
			int itemLevel = Mod.AlienItemLevels[_save.AlienItemLevel][Rng.Generate(0, 9)];

			// Initialize the unit and its position
			newUnit.SetTile(_save.GetTile(position), _save);
			newUnit.SetPosition(position);
			newUnit.Direction = unitDirection;
			newUnit.ClearTimeUnits();
			newUnit.PreviousOwner = owner;
			newUnit.Visible = faction == FACTION_PLAYER;
			_save.Units.Add(newUnit);
			_save.InitUnit(newUnit, itemLevel);

			TileEngine.ApplyGravity(newUnit.Tile);
			TileEngine.CalculateFOV(newUnit.Position); //happens fairly rarely, so do a full recalc for units in range to handle the potential unit visible cache issues.
		}
		// else: C++ deletes the temporary unit
	}

	/// <summary>Spawns a new item mid-battle.</summary>
	/// <param name="item">Item that spawns the item.</param>
	public void SpawnNewItem(BattleItem item)
	{
		SpawnNewItem(new BattleActionAttack(BA_NONE, null, item, item), item.Tile.Position);
	}

	/// <summary>Spawns a new item mid-battle.</summary>
	/// <param name="attack">BattleActionAttack that calls to spawn the item.</param>
	/// <param name="position">Tile position to try and spawn item on.</param>
	public void SpawnNewItem(BattleActionAttack attack, Position position)
	{
		if (attack.damage_item == null) // no idea how this happened, but make sure we have an item
			return;

		RuleItem item = attack.damage_item.Rules;
		RuleItem type = item.SpawnItem;

		if (type == null)
			return;

		int chance = item.SpawnItemChance;
		var conf = attack.weapon_item != null ? attack.weapon_item.GetActionConf(attack.type) : null;
		if (conf != null)
		{
			chance = conf.ammoSpawnItemChanceOverride == -1 ? chance : conf.ammoSpawnItemChanceOverride; // useIntNullable
		}

		if (!Rng.Percent(chance))
		{
			return;
		}

		BattleUnit owner = attack.attacker;
		if (owner == null)
		{
			owner = attack.damage_item.Owner;
			if (owner == null)
			{
				owner = attack.damage_item.PreviousOwner;
			}
		}

		// Create the item
		var newItem = _save.CreateTempItem(type);

		var tile = _save.GetTile(position);

		if (tile != null) // Place the item and initialize it in the battlescape
		{
			tile.AddItem(newItem, Mod.InventoryGround);
			newItem.PreviousOwner = owner;
			_save.Items.Add(newItem);
			_save.InitItem(newItem, owner);

			TileEngine.ApplyGravity(newItem.Tile);
			if (newItem.Glow)
			{
				tile = newItem.Tile; //item could drop down
				TileEngine.CalculateLighting(LL_ITEMS, tile.Position);
				TileEngine.CalculateFOV(tile.Position, newItem.VisibilityUpdateRange, false);
			}
		}
		// else: C++ deletes the temporary item
	}

	/// <summary>Spawns units from items primed before battle.</summary>
	public void SpawnFromPrimedItems()
	{
		var itemsSpawningUnits = new List<BattleItem>();

		foreach (var bi in _save.Items)
		{
			if (bi.IsOwnerIgnored || bi.Tile == null)
			{
				continue;
			}
			if ((bi.Rules.SpawnUnit != null || bi.Rules.SpawnItem != null) && !bi.XCOMProperty && !bi.IsSpecialWeapon)
			{
				if (bi.Rules.BattleType == BT_GRENADE && bi.FuseTimer == 0 && bi.IsFuseEnabled)
				{
					itemsSpawningUnits.Add(bi);
				}
			}
		}

		foreach (var item in itemsSpawningUnits)
		{
			SpawnNewUnit(item);
			SpawnNewItem(item);
			_save.RemoveItem(item);
		}
	}

	/// <summary>Removes spawned units that belong to the player to avoid dealing with recovery.</summary>
	public void RemoveSummonedPlayerUnits()
	{
		var resummonAsCivilians = new List<Unit>();

		var units = _save.Units;
		int buIt = 0;
		while (buIt != units.Count)
		{
			var bu = units[buIt];
			if (!bu.IsSummonedPlayerUnit)
			{
				++buIt;
			}
			else
			{
				if (bu.Status != STATUS_DEAD && bu.UnitRules != null)
				{
					if (bu.UnitRules.IsRecoverableAsCivilian)
					{
						resummonAsCivilians.Add(bu.UnitRules);
					}
				}

				if (bu.Status == STATUS_UNCONSCIOUS || bu.Status == STATUS_DEAD)
					_save.RemoveUnconsciousBodyItem(bu);

				//remove all items from unit
				bu.RemoveSpecialWeapons(_save);
				var invCopy = new List<BattleItem>(bu.Inventory);
				foreach (var bi in invCopy)
				{
					_save.RemoveItem(bi);
				}

				bu.SetTile(null, _save);
				_save.ClearUnitSelection(bu);
				units.RemoveAt(buIt);
			}
		}

		foreach (var unitType in resummonAsCivilians)
		{
			var newUnit = new BattleUnit(Mod,
				unitType,
				FACTION_NEUTRAL,
				_save.Units[^1].Id + 1,
				_save.EnviroEffects,
				unitType.Armor,
				null,
				Depth,
				_save.StartingCondition);

			// just bare minimum, this unit will never be used for anything except recovery (not even for scoring)
			newUnit.SetTile(null, _save);
			newUnit.SetPosition(TileEngine.invalid);
			newUnit.MarkAsResummonedFakeCivilian();
			_save.Units.Add(newUnit);
		}
	}

	/// <summary>Tally summoned player-controlled VIPs. We may still need to correct this in the Debriefing.</summary>
	public void TallySummonedVIPs()
	{
		EscapeType escapeType = _save.VIPEscapeType;
		foreach (var unit in _save.Units)
		{
			if (unit.IsVIP && unit.IsSummonedPlayerUnit)
			{
				if (unit.Status == STATUS_DEAD)
				{
					_save.AddLostVIP(unit.Value);
				}
				else if (escapeType == EscapeType.ESCAPE_EXIT)
				{
					if (unit.IsInExitArea(SpecialTileType.END_POINT))
						_save.AddSavedVIP(unit.Value);
					else
						_save.AddLostVIP(unit.Value);
				}
				else if (escapeType == EscapeType.ESCAPE_ENTRY)
				{
					if (unit.IsInExitArea(SpecialTileType.START_POINT))
						_save.AddSavedVIP(unit.Value);
					else
						_save.AddLostVIP(unit.Value);
				}
				else if (escapeType == EscapeType.ESCAPE_EITHER)
				{
					if (unit.IsInExitArea(SpecialTileType.START_POINT) || unit.IsInExitArea(SpecialTileType.END_POINT))
						_save.AddSavedVIP(unit.Value);
					else
						_save.AddLostVIP(unit.Value);
				}
				else //if (escapeType == ESCAPE_NONE)
				{
					if (unit.IsInExitArea(SpecialTileType.START_POINT))
						_save.AddSavedVIP(unit.Value); // waiting in craft, saved even if aborted
					else
						_save.AddWaitingOutsideVIP(unit.Value); // waiting outside, lost if aborted
				}
			}
		}
	}

	/// <summary>Gets the map.</summary>
	public Map Map => _parentState.Map;

	/// <summary>Gets the save.</summary>
	public SavedBattleGame Save => _save;

	/// <summary>Gets the tile engine.</summary>
	public TileEngine TileEngine => _save.TileEngine;

	/// <summary>Gets the pathfinding.</summary>
	public Pathfinding Pathfinding => _save.Pathfinding;

	/// <summary>Gets the mod.</summary>
	public Mod Mod => _parentState.Game.Mod;

	/// <summary>Returns whether panic has been handled.</summary>
	public bool PanicHandled => _playerPanicHandled;

	/// <summary>Tries to find an item and pick it up if possible.</summary>
	/// <returns>True if an item was picked up, false otherwise.</returns>
	public bool FindItem(BattleAction action, bool pickUpWeaponsMoreActively, ref bool walkToItem)
	{
		// terrorists don't have hands.
		if (action.actor.RankString != "STR_LIVE_TERRORIST" || pickUpWeaponsMoreActively)
		{
			// pick the best available item
			BattleItem targetItem = SurveyItems(action, pickUpWeaponsMoreActively);
			// make sure it's worth taking
			if (targetItem != null && WorthTaking(targetItem, action, pickUpWeaponsMoreActively))
			{
				// if we're already standing on it...
				if (targetItem.Tile.Position == action.actor.Position)
				{
					// try to pick it up
					if (TakeItemFromGround(targetItem, action) == 0)
					{
						// if it isn't loaded or it is ammo
						if (!targetItem.HaveAnyAmmo())
						{
							// try to load our weapon
							action.actor.ReloadAmmo();
						}
						if (targetItem.Glow)
						{
							_save.TileEngine.CalculateLighting(LL_ITEMS, action.actor.Position);
							_save.TileEngine.CalculateFOV(action.actor.Position, targetItem.VisibilityUpdateRange, false);
						}
						return true;
					}
				}
				else if (targetItem.Tile.Unit == null || targetItem.Tile.Unit.IsOut)
				{
					// if we're not standing on it, we should try to get to it.
					action.target = targetItem.Tile.Position;
					action.type = BA_WALK;
					walkToItem = true;
					if (pickUpWeaponsMoreActively)
					{
						// don't end the turn after walking 1-2 tiles... pick up a weapon and shoot!
						action.finalAction = false;
						action.desperate = false;
						action.actor.SetHiding(false);
					}
				}
			}
		}
		return false;
	}

	/// <summary>Searches through items on the map that were dropped on an alien turn, then picks the most "attractive" one.</summary>
	/// <param name="action">A pointer to the action being performed.</param>
	/// <param name="pickUpWeaponsMoreActively">Pick up weapons more actively.</param>
	/// <returns>The item to attempt to take.</returns>
	public BattleItem SurveyItems(BattleAction action, bool pickUpWeaponsMoreActively)
	{
		var droppedItems = new List<BattleItem>();

		// first fill a vector with items on the ground that were dropped on the alien turn, and have an attraction value.
		foreach (var bi in _save.Items)
		{
			if (bi.IsOwnerIgnored)
			{
				continue;
			}

			if (bi.Rules.Attraction != 0)
			{
				if (bi.TurnFlag || pickUpWeaponsMoreActively)
				{
					if (bi.Slot != null && bi.Slot.Type == InventoryType.INV_GROUND && bi.Tile != null && !bi.Tile.Dangerous)
					{
						droppedItems.Add(bi);
					}
				}
			}
		}

		BattleItem targetItem = null;
		int maxWorth = 0;

		// now select the most suitable candidate depending on attractiveness and distance
		// (are we still talking about items?)
		foreach (var bi in droppedItems)
		{
			if (bi.Tile.Dangerous)
			{
				continue;
			}
			int currentWorth = bi.Rules.Attraction / ((Position.Distance2d(action.actor.Position, bi.Tile.Position) * 2) + 1);
			if (currentWorth > maxWorth)
			{
				if (bi.Tile.GetTUCost((int)TilePart.O_OBJECT, action.actor.MovementType) == 255)
				{
					// Note: full pathfinding check will be done later, this is just a small optimisation
					bi.Tile.Dangerous = true;
					continue;
				}
				maxWorth = currentWorth;
				targetItem = bi;
			}
		}

		return targetItem;
	}

	/// <summary>
	/// Assesses whether this item is worth trying to pick up, taking into account how many units we see,
	/// whether or not the Weapon has ammo, and if we have ammo FOR it,
	/// or, if it's ammo, checks if we have the weapon to go with it,
	/// assesses the attraction value of the item and compares it with the distance to the object,
	/// then returns false anyway.
	/// </summary>
	/// <param name="item">The item to attempt to take.</param>
	/// <param name="action">A pointer to the action being performed.</param>
	/// <param name="pickUpWeaponsMoreActively">Pick up weapons more actively.</param>
	/// <returns>false.</returns>
#pragma warning disable CA1822 // instance method like in C++
	public bool WorthTaking(BattleItem item, BattleAction action, bool pickUpWeaponsMoreActively)
	{
		int worthToTake = 0;

		// don't even think about making a move for that gun if you can see a target, for some reason
		// (maybe this should check for enemies spotting the tile the item is on?)
		if (action.actor.VisibleUnits.Count == 0 || pickUpWeaponsMoreActively)
		{
			// retrieve an insignificantly low value from the ruleset.
			worthToTake = item.Rules.Attraction;

			// it's always going to be worth while to try and take a blaster launcher, apparently
			if (item.Rules.BattleType == BT_FIREARM && item.CurrentWaypoints == 0)
			{
				// we only want weapons that HAVE ammo, or weapons that we have ammo FOR
				bool ammoFound = true;
				if (!item.HaveAnyAmmo())
				{
					ammoFound = false;
					foreach (var bi in action.actor.Inventory)
					{
						if (bi.Rules.BattleType == BT_AMMO)
						{
							if (item.Rules.GetSlotForAmmo(bi.Rules) != -1)
							{
								ammoFound = true;
								break;
							}
						}
					}
				}
				if (!ammoFound)
				{
					return false;
				}
			}

			if (item.Rules.BattleType == BT_AMMO)
			{
				// similar to the above, but this time we're checking if the ammo is suitable for a weapon we have.
				bool weaponFound = false;
				foreach (var bi in action.actor.Inventory)
				{
					if (bi.Rules.BattleType == BT_FIREARM)
					{
						if (bi.Rules.GetSlotForAmmo(item.Rules) != -1)
						{
							weaponFound = true;
							break;
						}
					}
				}
				if (!weaponFound)
				{
					return false;
				}
			}
		}

		if (worthToTake != 0)
		{
			// use bad logic to determine if we'll have room for the item
			int freeSlots = 25;
			foreach (var bi in action.actor.Inventory)
			{
				freeSlots -= bi.Rules.InventoryHeight * bi.Rules.InventoryWidth;
			}
			int size = item.Rules.InventoryHeight * item.Rules.InventoryWidth;
			if (freeSlots < size)
			{
				return false;
			}
		}

		if (pickUpWeaponsMoreActively)
		{
			// Note: always true, the item must have passed this test already in surveyItems()
			return worthToTake > 0;
		}

		// return false for any item that we aren't standing directly on top of with an attraction value less than 6 (aka always)
		return (worthToTake - (Position.Distance2d(action.actor.Position, item.Tile.Position) * 2)) > 5;
	}
#pragma warning restore CA1822

	/// <summary>
	/// Picks the item up from the ground.
	/// At this point we've decided it's worth our while to grab this item, so we try to do just that.
	/// First we check to make sure we have time units, then that we have space (using horrifying logic)
	/// then we attempt to actually recover the item.
	/// </summary>
	/// <param name="item">The item to attempt to take.</param>
	/// <param name="action">A pointer to the action being performed.</param>
	/// <returns>0 if successful, 1 for no TUs, 2 for not enough room, 3 for "won't fit" and -1 for "something went horribly wrong".</returns>
	public int TakeItemFromGround(BattleItem item, BattleAction action)
	{
		const int success = 0;
		const int notEnoughTimeUnits = 1;
		const int notEnoughSpace = 2;
		const int couldNotFit = 3;
		int freeSlots = 25;

		// make sure we have time units
		if (action.actor.TimeUnits < 6)
		{
			return notEnoughTimeUnits;
		}
		else
		{
			// check to make sure we have enough space by checking all the sizes of items in our inventory
			foreach (var bi in action.actor.Inventory)
			{
				freeSlots -= bi.Rules.InventoryHeight * bi.Rules.InventoryWidth;
			}
			if (freeSlots < item.Rules.InventoryHeight * item.Rules.InventoryWidth)
			{
				return notEnoughSpace;
			}
			else
			{
				// check that the item will fit in our inventory, and if so, take it
				if (TakeItem(item, action))
				{
					return success;
				}
				else
				{
					return couldNotFit;
				}
			}
		}
	}

	/// <summary>Tries to fit an item into the unit's inventory, return false if you can't.</summary>
	/// <param name="item">The item to attempt to take.</param>
	/// <param name="action">A pointer to the action being performed.</param>
	/// <returns>Whether or not the item was successfully retrieved.</returns>
	public bool TakeItem(BattleItem item, BattleAction action)
	{
		bool placed = false;
		Mod mod = _parentState.Game.Mod;
		var rightWeapon = action.actor.RightHandWeapon;
		var leftWeapon = action.actor.LeftHandWeapon;
		var unit = action.actor;

		bool ReloadWeapon(BattleItem weapon, BattleItem i)
		{
			if (weapon != null && weapon.IsWeaponWithAmmo && !weapon.HaveAllAmmo())
			{
				int slot = weapon.Rules.GetSlotForAmmo(i.Rules);
				if (slot != -1)
				{
					var cost = new BattleActionCost(unit);
					cost.Time = unchecked((short)(cost.Time + (Mod.EXTENDED_ITEM_RELOAD_COST ? i.GetMoveToCost(weapon.Slot) : 0)));
					cost.Time = unchecked((short)(cost.Time + weapon.Rules.GetTULoad(slot)));
					if (cost.HaveTU() && weapon.GetAmmoForSlot(slot) == null)
					{
						weapon.SetAmmoForSlot(slot, i);
						cost.SpendTU();
						return true;
					}
				}
			}
			return false;
		}

		bool EquipItem(RuleInventory slot, BattleItem i)
		{
			var cost = new BattleActionCost(unit);
			cost.Time = unchecked((short)(cost.Time + i.GetMoveToCost(slot)));
			if (cost.HaveTU() && unit.FitItemToInventory(slot, i))
			{
				cost.SpendTU();
				return true;
			}
			return false;
		}

		switch (item.Rules.BattleType)
		{
			case BT_AMMO:
				// find equipped weapons that can be loaded with this ammo
				if (ReloadWeapon(rightWeapon, item))
				{
					placed = true;
				}
				else if (ReloadWeapon(leftWeapon, item))
				{
					placed = true;
				}
				else
				{
					placed = EquipItem(mod.InventoryBelt, item);
				}
				break;
			case BT_GRENADE:
			case BT_PROXIMITYGRENADE:
				placed = EquipItem(mod.InventoryBelt, item);
				break;
			case BT_FIREARM:
			case BT_MELEE:
				if (rightWeapon == null)
				{
					placed = EquipItem(mod.InventoryRightHand, item);
				}
				break;
			case BT_MEDIKIT:
			case BT_SCANNER:
				placed = EquipItem(mod.InventoryBackpack, item);
				break;
			case BT_MINDPROBE:
				if (leftWeapon == null)
				{
					placed = EquipItem(mod.InventoryLeftHand, item);
				}
				break;
			default: break;
		}
		return placed;
	}

	/// <summary>Returns the action type that is reserved.</summary>
	public BattleActionType ReservedAction => _save.TUReserved;

	/// <summary>Checks whether the unit surrenders (and remembers the decision).</summary>
	public bool IsSurrendering(BattleUnit bu)
	{
		// if we already decided to surrender this turn, don't change our decision (until next turn)
		if (bu.IsSurrendering)
		{
			return true;
		}

		int surrenderMode = Mod.SurrenderMode;

		// auto-surrender (e.g. units, which won't fight without their masters/controllers)
		if (surrenderMode > 0 && bu.UnitRules.AutoSurrender())
		{
			bu.SetSurrendering(true);
			return true;
		}

		// surrender under certain conditions
		if (surrenderMode == 0)
		{
			// turned off, no surrender
		}
		else if (surrenderMode == 1)
		{
			// all remaining enemy units can surrender and want to surrender now
			if (bu.UnitRules.CanSurrender() && (bu.Status == STATUS_PANICKING || bu.Status == STATUS_BERSERK))
			{
				bu.SetSurrendering(true);
			}
		}
		else if (surrenderMode == 2)
		{
			// all remaining enemy units can surrender and want to surrender now or wanted to surrender in the past
			if (bu.UnitRules.CanSurrender() && bu.WantsToSurrender())
			{
				bu.SetSurrendering(true);
			}
		}
		else if (surrenderMode == 3)
		{
			// all remaining enemy units have empty hands and want to surrender now or wanted to surrender in the past
			if (bu.LeftHandWeapon == null && bu.RightHandWeapon == null && bu.WantsToSurrender())
			{
				bu.SetSurrendering(true);
			}
		}

		return bu.IsSurrendering;
	}

	/// <summary>Tallies the living units in the game and, if required, converts units into their spawn unit.</summary>
	public BattlescapeTally TallyUnits()
	{
		var tally = new BattlescapeTally();

		foreach (var bu in _save.Units)
		{
			//TODO: add handling of stunned units for display purposes in AbortMissionState
			if (!bu.IsOut && (!bu.IsOutThresholdExceed || (bu.UnitRules != null && bu.UnitRules.SpawnUnit != null)))
			{
				if (bu.OriginalFaction == FACTION_HOSTILE)
				{
					if (Options.allowPsionicCapture && bu.Faction == FACTION_PLAYER && bu.Capturable)
					{
						// don't count psi-captured units
					}
					else if (IsSurrendering(bu) && bu.Capturable)
					{
						// don't count surrendered units
					}
					else
					{
						tally.liveAliens++;
					}
				}
				else if (bu.OriginalFaction == FACTION_PLAYER)
				{
					if (bu.IsSummonedPlayerUnit)
					{
						if (bu.IsVIP)
						{
							// used only for display purposes in AbortMissionState
							// count only player-controlled VIPs, not civilian VIPs!
							if (bu.IsInExitArea(SpecialTileType.START_POINT))
							{
								tally.vipInEntrance++;
							}
							else if (bu.IsInExitArea(SpecialTileType.END_POINT))
							{
								if (bu.IsBannedInNextStage)
								{
									// this guy would (theoretically) go into timeout
									tally.vipInField++;
								}
								else
								{
									tally.vipInExit++;
								}
							}
							else
							{
								tally.vipInField++;
							}
						}
						continue;
					}

					if (bu.IsInExitArea(SpecialTileType.START_POINT))
					{
						tally.inEntrance++;
					}
					else if (bu.IsInExitArea(SpecialTileType.END_POINT))
					{
						if (bu.IsBannedInNextStage)
						{
							// this guy will go into timeout
							tally.inField++;
						}
						else
						{
							tally.inExit++;
						}
					}
					else
					{
						tally.inField++;
					}

					if (bu.Faction == FACTION_PLAYER)
					{
						tally.liveSoldiers++;
					}
					else
					{
						tally.liveAliens++;
					}
				}
			}
		}

		return tally;
	}

	/// <summary>Converts infected units (zombies etc.).</summary>
	public bool ConvertInfected()
	{
		bool retVal = false;
		var forTransform = new List<BattleUnit>();
		foreach (var bu in _save.Units)
		{
			if (!bu.IsOutThresholdExceed && bu.Respawn)
			{
				retVal = true;
				bu.Respawn = false;
				if (Options.battleNotifyDeath && bu.Faction == FACTION_PLAYER)
				{
					Game game = _parentState.Game;
					game.PushState(new InfoboxState(game.Language.GetString("STR_HAS_BEEN_KILLED", bu.Gender).Arg(bu.GetName(game.Language))));
				}

				forTransform.Add(bu);
			}
		}

		foreach (var bu in forTransform)
		{
			ConvertUnit(bu);
		}
		return retVal;
	}

	/// <summary>Gets or sets the kneel reservation setting.</summary>
	public bool KneelReserved
	{
		get => _save.KneelReserved;
		set => _save.KneelReserved = value;
	}

	/// <summary>
	/// Checks if a unit has moved next to a proximity grenade.
	/// Checks one tile around the unit in every direction.
	/// For a large unit we check every tile it occupies.
	/// </summary>
	/// <param name="unit">Pointer to a unit.</param>
	/// <returns>2 if a proximity grenade was triggered, 1 if light was changed.</returns>
	public int CheckForProximityGrenades(BattleUnit unit)
	{
		if (_save.IsPreview)
		{
			return 0;
		}

		// death trap?
		Tile deathTrapTile = null;
		for (int sx = 0; sx < unit.Armor.Size; sx++)
		{
			for (int sy = 0; sy < unit.Armor.Size; sy++)
			{
				Tile t = _save.GetTile(unit.Position + new Position(sx, sy, 0));
				if (deathTrapTile == null && t != null && t.FloorSpecialTileType >= SpecialTileType.DEATH_TRAPS)
				{
					deathTrapTile = t;
				}
			}
		}
		if (deathTrapTile != null)
		{
			string ss = "STR_DEATH_TRAP_" + ((int)deathTrapTile.FloorSpecialTileType).ToString(System.Globalization.CultureInfo.InvariantCulture);
			var deathTrapRule = Mod.GetItem(ss);
			if (deathTrapRule != null &&
				deathTrapRule.IsTargetAllowed(unit.OriginalFaction, FACTION_PLAYER) && // FACTION_PLAYER for backward compatibility reasons
				(deathTrapRule.BattleType == BT_PROXIMITYGRENADE || deathTrapRule.BattleType == BT_MELEE))
			{
				BattleItem deathTrapItem = null;
				foreach (var item in deathTrapTile.Inventory)
				{
					if (item.Rules == deathTrapRule)
					{
						deathTrapItem = item;
						break;
					}
				}
				if (deathTrapItem == null)
				{
					deathTrapItem = _save.CreateItemForTile(deathTrapRule, deathTrapTile);
				}
				if (deathTrapRule.BattleType == BT_PROXIMITYGRENADE)
				{
					deathTrapItem.FuseTimer = 0;
					Position p = deathTrapTile.Position.ToVoxel() + new Position(8, 8, deathTrapTile.TerrainLevel);
					StatePushNext(new ExplosionBState(this, new LastPositions(p), BattleActionAttack.GetBeforeShoot(BA_TRIGGER_PROXY_GRENADE, null, deathTrapItem), null));
					return 2;
				}
				else if (deathTrapRule.BattleType == BT_MELEE)
				{
					Position p = deathTrapTile.Position.ToVoxel() + new Position(8, 8, 12);
					// EXPERIMENTAL: terrainMeleeTilePart = 4 (V_UNIT); no attacker
					StatePushNext(new ExplosionBState(this, new LastPositions(p), BattleActionAttack.GetBeforeShoot(BA_HIT, null, deathTrapItem), null, false, 0, 0, 4));
					return 2;
				}
			}
		}

		bool exploded = false;
		bool glow = false;
		int size = unit.Armor.Size + 1;
		for (int tx = -1; tx < size; tx++)
		{
			for (int ty = -1; ty < size; ty++)
			{
				Tile t = _save.GetTile(unit.Position + new Position(tx, ty, 0));
				if (t != null)
				{
					var forRemoval = new List<BattleItem>();
					foreach (BattleItem item in t.Inventory)
					{
						RuleItem ruleItem = item.Rules;
						bool g = item.Glow;
						if (item.FuseProximityEvent())
						{
							if (ruleItem.BattleType == BT_GRENADE || ruleItem.BattleType == BT_PROXIMITYGRENADE)
							{
								Position p = t.Position.ToVoxel() + new Position(8, 8, t.TerrainLevel);
								StatePushNext(new ExplosionBState(this, new LastPositions(p), BattleActionAttack.GetBeforeShoot(BA_TRIGGER_PROXY_GRENADE, null, item), null));
								exploded = true;
							}
							else
							{
								forRemoval.Add(item);
								if (g)
								{
									glow = true;
								}
							}
						}
						else
						{
							if (g != item.Glow)
							{
								glow = true;
							}
						}
					}
					foreach (BattleItem item in forRemoval)
					{
						_save.RemoveItem(item);
					}
				}
			}
		}
		return exploded ? 2 : glow ? 1 : 0;
	}

	/// <summary>Cleans up all the deleted states.</summary>
	public void CleanupDeleted()
	{
		_deleted.Clear();
	}

	/// <summary>Gets the depth of the battlescape.</summary>
	public int Depth => _save.Depth;

	/// <summary>Play sound on battlefield (with direction).</summary>
	public void PlaySound(int sound, Position pos)
	{
		if (sound != Mod.NO_SOUND)
		{
			_parentState.Game.Mod.GetSoundByDepth((uint)_save.Depth, (uint)sound).Play(-1, _parentState.Map.GetSoundAngle(pos));
		}
	}

	/// <summary>Play sound on battlefield.</summary>
	public void PlaySound(int sound)
	{
		if (sound != Mod.NO_SOUND)
		{
			_parentState.Game.Mod.GetSoundByDepth((uint)_save.Depth, (uint)sound).Play();
		}
	}

	/// <summary>Play unit response sound on battlefield.</summary>
	public void PlayUnitResponseSound(BattleUnit unit, int type)
	{
		if (!Mod.EnableUnitResponseSounds)
			return;

		if (!Options.oxceEnableUnitResponseSounds)
			return;

		if (unit == null)
			return;

		int chance = Mod.UNIT_RESPONSE_SOUNDS_FREQUENCY[type];
		if (chance < 100 && Rng.Seedless(0, 99) >= chance)
		{
			return;
		}

		List<int> sounds = [];
		if (type == 0)
			sounds = unit.SelectUnitSounds;
		else if (type == 1)
			sounds = unit.StartMovingSounds;
		else if (type == 2)
			sounds = unit.SelectWeaponSounds;
		else if (type == 3)
			sounds = unit.AnnoyedSounds;

		int sound = -1;
		if (sounds.Count != 0)
		{
			if (sounds.Count > 1)
				sound = sounds[Rng.Seedless(0, sounds.Count - 1)];
			else
				sound = sounds[0];
		}

		if (sound != Mod.NO_SOUND)
		{
			if (Mixer.Playing(4) == 0)
			{
				// use fixed channel, so that we can check if the unit isn't already/still talking
				Mod.GetSoundByDepth((uint)_save.Depth, (uint)sound).Play(4);
			}
		}
	}

	/// <summary>Gets a copy of the state queue (null entries are end turn requests).</summary>
	public List<BattleState> States => new(_states);

	/// <summary>Ends the turn if auto-end battle is enabled and all mission objectives are completed.</summary>
	public void AutoEndBattle()
	{
		if (_save.IsPreview)
		{
			return;
		}
		if (Options.battleAutoEnd)
		{
			if (_save.VIPSurvivalPercentage > 0 && _save.VIPEscapeType != EscapeType.ESCAPE_NONE)
			{
				return; // "escort the VIPs" missions don't end when all aliens are neutralized
			}
			bool end = false;
			bool askForConfirmation = false;
			if (_save.ObjectiveType == SpecialTileType.MUST_DESTROY)
			{
				end = _save.AllObjectivesDestroyed();
			}
			else
			{
				BattlescapeTally tally = TallyUnits();
				end = (tally.liveAliens == 0 || tally.liveSoldiers == 0);
				if (tally.liveAliens == 0)
				{
					_allEnemiesNeutralized = true; // remember that all aliens were neutralized (and the battle should end no matter what)
					askForConfirmation = true;
				}
			}
			if (end)
			{
				_save.SelectedUnit = null;
				CancelCurrentAction(true);
				RequestEndTurn(askForConfirmation);
			}
		}
	}

	/// <summary>Were all enemies neutralized?</summary>
	public bool AreAllEnemiesNeutralized() => _allEnemiesNeutralized;

	/// <summary>Resets the flag.</summary>
	public void ResetAllEnemiesNeutralized() => _allEnemiesNeutralized = false;
}
