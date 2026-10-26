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
// Ported from: src/Battlescape/BattlescapeGenerator.h, src/Battlescape/BattlescapeGenerator.cpp
#nullable disable

using static Acom.SpecialTileType;
using static Acom.TilePart;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>
/// A utility class that generates the initial battlescape data. Taking into account mission type, craft and ufo involved, terrain type,...
/// </summary>
/// <remarks>
/// The class is split over several files: this one holds the setup, <c>run</c> and <c>nextStage</c>;
/// <c>BattlescapeGenerator.Deploy.cs</c> the unit/item deployment and <c>BattlescapeGenerator.Map.cs</c>
/// the map generation (map scripts, MAP/RMP loading, nodes).
/// </remarks>
public partial class BattlescapeGenerator
{
	private readonly Game _game;
	private readonly SavedBattleGame _save;
	private readonly Mod _mod;
	private readonly RuleInventory _inventorySlotGround;
	private Craft _craft;
	private RuleCraft _craftRules;
	private Ufo _ufo;
	private Base _base;
	private MissionSite _mission;
	private AlienBase _alienBase;
	private RuleTerrain _terrain, _baseTerrain, _globeTerrain, _alternateTerrain;
	private int _mapsize_x, _mapsize_y, _mapsize_z;
	private Texture _missionTexture, _globeTexture;
	private int _worldShade;
	private int _unitSequence;
	private Tile _craftInventoryTile;
	private string _alienRace = "";
	private AlienDeployment _alienCustomDeploy, _alienCustomMission;
	private int _alienItemLevel;
	private int _ufoDamagePercentage;
#pragma warning disable CS0414 // _ufoDeployed is write-only in C++ as well
	private bool _allowAutoLoadout, _baseInventory, _generateFuel, _craftDeployed, _ufoDeployed;
#pragma warning restore CS0414
	private int _craftZ;
	private SDL_Rect _craftPos;
	private readonly List<SDL_Rect> _ufoPos = [];
	private readonly List<int> _ufoZ = [];
	private int _markAsReinforcementsBlock;
	private int _blocksToDo;
	private MapBlock[][] _blocks = [];
	private bool[][] _landingzone = [];
	private int[][] _segments = [];
	private MapDirection[][] _drillMap = [];
	private MapBlock _dummy;
	private readonly List<SDL_Rect> _placedBlockRects = [];
	private List<VerticalLevel> _verticalLevels = [];
	private readonly Dictionary<RuleTerrain, int> _loadedTerrains = [];
	private readonly List<(MapBlock first, Position second)> _verticalLevelSegments = [];
	private readonly List<Tile> _backupInventoryTiles = [];

	/// <summary>Sets up a BattlescapeGenerator.</summary>
	/// <param name="game">Pointer to Game object.</param>
	public BattlescapeGenerator(Game game)
	{
		ArgumentNullException.ThrowIfNull(game);
		_game = game;
		_save = game.SavedGame.SavedBattle;
		_mod = _game.Mod;
		_generateFuel = true;
		_allowAutoLoadout = !Options.disableAutoEquip;
		if (_game.SavedGame.DisableSoldierEquipment)
		{
			_allowAutoLoadout = false;
		}
		_inventorySlotGround = _game.Mod.InventoryGround;
	}

	/// <summary>Sets up all our various arrays and whatnot according to the size of the map.</summary>
	private void Init(bool resetTerrain)
	{
		_verticalLevels.Clear();
		_loadedTerrains.Clear();
		_verticalLevelSegments.Clear();

		int blocksX = _mapsize_x / 10;
		int blocksY = _mapsize_y / 10;

		_markAsReinforcementsBlock = 0;
		ResetGrid(_save.ReinforcementsBlocks, blocksX, blocksY, 0);
		ResetGrid(_save.FlattenedMapTerrainNames, blocksX, blocksY, "");
		ResetGrid(_save.FlattenedMapBlockNames, blocksX, blocksY, "");

		_blocks = NewGrid<MapBlock>(blocksX, blocksY, null);
		_landingzone = NewGrid(blocksX, blocksY, false);
		_segments = NewGrid(blocksX, blocksY, 0);
		_drillMap = NewGrid(blocksX, blocksY, MapDirection.MD_NONE);

		_blocksToDo = blocksX * blocksY;
		// creates the tile objects
		_save.InitMap(_mapsize_x, _mapsize_y, _mapsize_z, resetTerrain);
		_save.InitUtilities(_mod);
	}

	/// <summary>Like C++ <c>v.clear(); v.resize(x, std::vector&lt;T&gt;(y, value));</c>.</summary>
	private static void ResetGrid<T>(List<List<T>> grid, int sizeX, int sizeY, T value)
	{
		grid.Clear();
		for (int x = 0; x < sizeX; ++x)
		{
			grid.Add([.. Enumerable.Repeat(value, sizeY)]);
		}
	}

	/// <summary>Creates a <c>[sizeX][sizeY]</c> jagged array filled with a value.</summary>
	private static T[][] NewGrid<T>(int sizeX, int sizeY, T value)
	{
		var grid = new T[sizeX][];
		for (int x = 0; x < sizeX; ++x)
		{
			grid[x] = new T[sizeY];
			Array.Fill(grid[x], value);
		}
		return grid;
	}

	/// <summary>Sets the XCom craft involved in the battle.</summary>
	public void SetCraft(Craft craft)
	{
		ArgumentNullException.ThrowIfNull(craft);
		_craft = craft;
		_craftRules = _craft.Rules;
		_craft.SetInBattlescape(true);
	}

	/// <summary>Sets the ufo involved in the battle.</summary>
	public void SetUfo(Ufo ufo)
	{
		ArgumentNullException.ThrowIfNull(ufo);
		_ufo = ufo;
		_ufo.SetInBattlescape(true);
	}

	/// <summary>Sets the world texture where a ufo crashed. This is used to determine the terrain.</summary>
	/// <param name="missionTexture">Texture of the polygon on the globe (for UFOs). For mission sites, alien bases, etc. can be something else.</param>
	/// <param name="globeTexture">Texture of the polygon on the globe.</param>
	public void SetWorldTexture(Texture missionTexture, Texture globeTexture)
	{
		_missionTexture = missionTexture;
		_globeTexture = globeTexture;
	}

	/// <summary>Sets the world shade where a ufo crashed. This is used to determine the battlescape light level.</summary>
	public void SetWorldShade(int shade)
	{
		if (shade > 15) shade = 15;
		if (shade < 0) shade = 0;
		_worldShade = shade;
	}

	/// <summary>Sets the alien race on the mission. This is used to determine the various alien types to spawn.</summary>
	public void SetAlienRace(string alienRace)
	{
		_alienRace = alienRace;
	}

	/// <summary>
	/// Sets the alien item level. This is used to determine how advanced the equipment of the aliens will be.
	/// Note: this only applies to "New Battle" type games.
	/// </summary>
	public void SetAlienItemlevel(int alienItemLevel)
	{
		_alienItemLevel = alienItemLevel;
	}

	/// <summary>Sets the UFO damage percentage. Used to spawn less aliens during base defense.</summary>
	public void SetUfoDamagePercentage(int ufoDamagePercentage)
	{
		_ufoDamagePercentage = ufoDamagePercentage;
	}

	/// <summary>Set new weapon deploy for aliens that overrides weapon deploy data from mission type/ufo.</summary>
	public void SetAlienCustomDeploy(AlienDeployment alienCustomDeploy = null, AlienDeployment alienCustomBase = null)
	{
		_alienCustomDeploy = alienCustomDeploy;
		_alienCustomMission = alienCustomBase;
	}

	/// <summary>Sets the XCom base involved in the battle.</summary>
	public void SetBase(Base @base)
	{
		ArgumentNullException.ThrowIfNull(@base);
		_base = @base;
		_base.SetInBattlescape(true);
	}

	/// <summary>Sets the mission site involved in the battle.</summary>
	public void SetMissionSite(MissionSite mission)
	{
		ArgumentNullException.ThrowIfNull(mission);
		_mission = mission;
		_mission.SetInBattlescape(true);
	}

	/// <summary>Sets the alien base involved in the battle.</summary>
	public void SetAlienBase(AlienBase @base)
	{
		ArgumentNullException.ThrowIfNull(@base);
		_alienBase = @base;
		_alienBase.SetInBattlescape(true);
	}

	/// <summary>Sets the terrain to be used in battle generation.</summary>
	public void SetTerrain(RuleTerrain terrain)
	{
		_terrain = terrain;
	}

	/// <summary>Gets craft position.</summary>
	public SDL_Rect CraftPos => _craftPos;

	/// <summary>Gets craft elevation.</summary>
	public int CraftZ => _craftZ;

	/// <summary>Switches an existing battlescape savegame to a new stage.</summary>
	public void NextStage()
	{
		// check if the unit is available in the next stage
		static bool isUnitStillActive(BattleUnit u) => !u.IsOut || u.Status == STATUS_UNCONSCIOUS;
		// check if the unit is on the exit tile
		var s = _save;
		bool isInExit(BattleUnit u)
		{
			Tile tmpTile = s.GetTile(u.Position);
			return u.IsInExitArea(END_POINT) || u.LiesInExitArea(tmpTile, END_POINT);
		}

		// preventively drop all units from soldier's inventory (makes handling easier)
		// 1. no alien/civilian living, dead or unconscious is allowed to transition
		// 2. no dead xcom unit is allowed to transition
		// 3. only living or unconscious xcom units can transition
		foreach (var bu in _save.Units)
		{
			if (bu.OriginalFaction == FACTION_PLAYER && !bu.IsOut)
			{
				var unitsToDrop = new List<BattleItem>();
				foreach (var bi in bu.Inventory)
				{
					if (bi.Unit != null)
					{
						unitsToDrop.Add(bi);
					}
				}
				foreach (var corpseItem in unitsToDrop)
				{
					_save.TileEngine.ItemDrop(bu.Tile, corpseItem, false);
				}
			}

			// scripts (or some bugs in the game) could make aliens or soldiers that have "unresolved" stun or death state.
			if (!bu.IsOut && bu.IsOutThresholdExceed)
			{
				bu.InstaFalling();
				if (bu.Tile != null)
				{
					_save.TileEngine.ItemDropInventory(bu.Tile, bu);
				}

				//spawn corpse/body for unit to recover
				for (int i = bu.Armor.TotalSize - 1; i >= 0; --i)
				{
					var corpse = _save.CreateItemForTile(bu.Armor.CorpseBattlescape[i], null, bu);
					_save.TileEngine.ItemDrop(bu.Tile, corpse, false);
				}
			}
		}

		int aliensAlive = 0;
		// send all enemy units, or those not in endpoint area (if aborted) to time out
		foreach (var bu in _save.Units)
		{
			bu.ClearVisibleUnits();
			bu.ClearVisibleTiles();

			if (bu.Status != STATUS_DEAD                               // if they're not dead
				&& ((bu.OriginalFaction == FACTION_PLAYER                 // and they're a soldier
				&& _save.IsAborted                                        // and you aborted
				&& !isInExit(bu))                                         // and they're not on the exit
				|| bu.OriginalFaction != FACTION_PLAYER))                 // or they're not a soldier
			{
				if (bu.OriginalFaction == FACTION_HOSTILE && !bu.IsOut)
				{
					if (bu.OriginalFaction == bu.Faction && !bu.IsSurrendering)
					{
						aliensAlive++;
					}
					else if (bu.Tile != null)
					{
						_save.TileEngine.ItemDropInventory(bu.Tile, bu);
					}
				}
				bu.GoToTimeOut();
				if (bu.AIModule != null)
				{
					bu.AIModule = null;
				}
			}
			bu.Fire = 0;
			bu.SetTile(null, _save);
			bu.SetPosition(TileEngine.invalid, false);
		}

		// send banned units to timeout
		if (_save.StartingCondition != null && _save.StartingCondition.ForbiddenArmorsInNextStage.Count != 0)
		{
			BattleUnit firstUnitToTimeout = null;
			bool everybodyInTimeout = true;
			foreach (var bu in _save.Units)
			{
				if (bu.OriginalFaction == FACTION_PLAYER && bu.Status != STATUS_DEAD && bu.Status != STATUS_IGNORE_ME)
				{
					if (bu.IsBannedInNextStage)
					{
						if (firstUnitToTimeout != null)
						{
							bu.GoToTimeOut();
							bu.AIModule = null;
						}
						else
						{
							firstUnitToTimeout = bu;
						}
					}
					else
					{
						everybodyInTimeout = false;
					}
				}
			}
			// should all remaining xcom units go to timeout, make one of them unconscious instead (to prevent a crash)
			if (firstUnitToTimeout != null)
			{
				if (everybodyInTimeout)
				{
					firstUnitToTimeout.HealStun(-firstUnitToTimeout.Health * 3);
					// don't "resolve" the unit status yet (to prevent a crash)
					// FIXME (C++): this is basically just an ugly hack, maybe it can be done better?
				}
				else
				{
					firstUnitToTimeout.GoToTimeOut();
					firstUnitToTimeout.AIModule = null;
				}
			}
		}

		// remove all items not belonging to our soldiers from the map.
		// sort items into two categories:
		// the ones that we are guaranteed to be able to take home, barring complete failure (ie: stuff on the ship)
		// and the ones that are scattered about on the ground, that will be recovered ONLY on success.
		// this does not include items in your soldier's hands.
		List<BattleItem> takeHomeGuaranteed = _save.GuaranteedRecoveredItems;
		List<BattleItem> takeHomeConditional = _save.ConditionalRecoveredItems;
		var takeToNextStage = new List<BattleItem>();
		var removeFromGame = new List<BattleItem>();
		var dummyForSpecialWeaponsRemovedElsewhere = new List<BattleItem>();

		bool autowin = false;
		if (_save.ChronoTrigger >= ChronoTrigger.FORCE_WIN && _save.Turn > _save.TurnLimit)
		{
			autowin = true;
		}

		// this will be `getItems` for next stage, allocate same size plus small buffer for new items
		var carryToNextStage = new List<BattleItem>(_save.Items.Count + 16);

		_save.ResetTurnCounter();

		foreach (var bi in _save.Items)
		{
			// first off: don't process ammo loaded into weapons. at least not at this level. ammo will be handled simultaneously.
			if (!bi.IsAmmo)
			{
				List<BattleItem> toContainer = removeFromGame;
				// if it's recoverable, and it's not owned by someone
				if (((bi.Unit != null && bi.Unit.GeoscapeSoldier != null) || bi.Rules.IsRecoverable) && bi.Owner == null)
				{
					// first off: don't count primed grenades on the floor
					if (bi.FuseTimer == -1)
					{
						// protocol 1: all defenders dead, recover all items.
						if (aliensAlive == 0 || autowin)
						{
							// any corpses or unconscious units get put in the skyranger, as well as any unresearched items
							if ((bi.Unit != null &&
								(bi.Unit.OriginalFaction != FACTION_PLAYER ||
								bi.Unit.Status == STATUS_DEAD))
								|| !_game.SavedGame.IsResearched(bi.Rules.Requirements))
							{
								toContainer = takeHomeGuaranteed;
							}
							// otherwise it comes with us to stage two
							else
							{
								toContainer = takeToNextStage;
							}
						}
						// protocol 2: some of the aliens survived, meaning we ran to the exit zone.
						// recover stuff depending on where it was at the end of the mission.
						else
						{
							Tile tile = bi.Tile;
							if (tile != null)
							{
								// on a tile at least, so i'll give you the benefit of the doubt on this and give it a conditional recovery at this point
								toContainer = takeHomeConditional;
								if (tile.GetMapData(O_FLOOR) != null)
								{
									// in the skyranger? it goes home.
									if (tile.FloorSpecialTileType == START_POINT)
									{
										toContainer = takeHomeGuaranteed;
									}
									// on the exit grid? it goes to stage two.
									else if (tile.FloorSpecialTileType == END_POINT)
									{
										// apply similar logic (for units) as in protocol 1
										if (bi.Unit != null &&
											(bi.Unit.OriginalFaction != FACTION_PLAYER ||
											bi.Unit.Status == STATUS_DEAD))
										{
											toContainer = takeHomeConditional;
										}
										else
										{
											toContainer = takeToNextStage;
										}
									}
								}
							}
						}
					}
				}

				if (bi.IsSpecialWeapon)
				{
					if (isUnitStillActive(bi.Owner))
					{
						// the owner of the weapon is still in the game
						toContainer = carryToNextStage;
					}
					else
					{
						// the item will be deleted (moved to the delete list) by the `removeSpecialWeapons` function, skip all operations in this function
						toContainer = dummyForSpecialWeaponsRemovedElsewhere;
					}
				}
				else
				{
					if (bi.IsOwnerIgnored)
					{
						// the unit was set to "timeout" in a previous stage or in this stage
						// in both cases we propagate this item to the next stage even if it will not be accessible
						// "timeout" aliens should have their inventory purged already
						toContainer = carryToNextStage;
					}
					else if (bi.Owner != null && bi.Owner.Faction == FACTION_PLAYER)
					{
						// if a soldier is already holding it, let's let him keep it
						toContainer = carryToNextStage;
						// Note: if a soldier is recovered at the end, his items will be recovered with him (regardless of whether that soldier was in timeout at the end or not)
					}
				}

				// at this point, we know what happens with the item, so let's apply it to any ammo as well.
				for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
				{
					BattleItem ammo = bi.GetAmmoForSlot(slot);
					if (ammo != null && ammo != bi)
					{
						// break any tile links, because all the tiles are about to disappear.
						ammo.Tile = null;
						toContainer.Add(ammo);
					}
				}
				// and now the actual item itself.
				bi.Tile = null;
				toContainer.Add(bi);
			}
		}

		// remove special weapons
		foreach (var bu in _save.Units)
		{
			if (!isUnitStillActive(bu))
			{
				bu.RemoveSpecialWeapons(_save);
			}
		}

		// anything in the "removeFromGame" vector will now be discarded - they're all dead to us now.
		foreach (var bi in removeFromGame)
		{
			// fixed weapons, or anything that's otherwise "equipped" will need to be de-equipped
			// from their owners to make sure we don't have any null pointers to worry about later
			bi.MoveToOwner(null);
			// C++ deletes the item here; the garbage collector takes care of it.
		}

		// rebuild it with only the items we want to keep active in battle for the next stage
		// here we add all the items that our soldiers are carrying, and we'll add the items on the
		// inventory tile after we've generated our map. everything else will either be in one of the
		// recovery arrays, or deleted from existence at this point.
		_save.Items.Clear();
		_save.Items.AddRange(carryToNextStage);

		_alienCustomDeploy = _game.Mod.GetDeployment(_save.AlienCustomDeploy);
		_alienCustomMission = _game.Mod.GetDeployment(_save.AlienCustomMission);

		if (_alienCustomDeploy != null) _alienCustomDeploy = _game.Mod.GetDeployment(_alienCustomDeploy.NextStage);
		if (_alienCustomMission != null) _alienCustomMission = _game.Mod.GetDeployment(_alienCustomMission.NextStage);

		_save.SetAlienCustom(_alienCustomDeploy != null ? _alienCustomDeploy.Type : "", _alienCustomMission != null ? _alienCustomMission.Type : "");

		AlienDeployment ruleDeploy = _alienCustomMission ?? _game.Mod.GetDeployment(_save.MissionType, true);
		_save.TurnLimit = ruleDeploy.TurnLimit;
		_save.ChronoTrigger = ruleDeploy.ChronoTrigger;
		_save.SetCheatTurn(ruleDeploy.CheatTurn);
		ruleDeploy.GetDimensions(ref _mapsize_x, ref _mapsize_y, ref _mapsize_z);
		var terrains = ruleDeploy.Terrains;
		int pick = Rng.Generate(0, terrains.Count - 1);
		_terrain = _game.Mod.GetTerrain(terrains[pick], true);
		SetDepth(ruleDeploy, true);
		_worldShade = ruleDeploy.Shade;

		RuleEnviroEffects enviro = _game.Mod.GetEnviroEffects(_terrain.EnviroEffects);
		RuleEnviroEffects temp = _game.Mod.GetEnviroEffects(ruleDeploy.EnviroEffects);
		if (temp != null)
		{
			enviro = temp;
		}
		_save.ApplyEnviroEffects(enviro);

		// enviro effects - armor transformation (no armor replacement! that is done only before the 1st stage)
		if (enviro != null)
		{
			foreach (var bu in _save.Units)
			{
				if (bu.OriginalFaction == FACTION_PLAYER && bu.GeoscapeSoldier != null)
				{
					Armor transformedArmor = enviro.GetArmorTransformation(bu.Armor);
					if (transformedArmor != null)
					{
						// remember the original armor (i.e. only if there were no transformations in earlier stage(s)!)
						if (bu.GeoscapeSoldier.TransformedArmor == null)
						{
							bu.GeoscapeSoldier.TransformedArmor = _game.Mod.GetArmor(bu.Armor.Type);
						}
						// change soldier's armor (needed for inventory view!)
						bu.GeoscapeSoldier.SetArmor(transformedArmor);
						// change battleunit's armor
						bu.UpdateArmorFromSoldier(_game.Mod, bu.GeoscapeSoldier, transformedArmor, _save.Depth, true, _save.StartingCondition);
						// remove old special built-in weapons and replace them with new fresh special built-in weapons
						// TODO? if this was a limited-use weapon, it will have full ammo again!
						bu.RemoveSpecialWeapons(_save);
						bu.SetSpecialWeapon(_save, false);
					}
				}
				else if (bu.OriginalFaction == FACTION_PLAYER)
				{
					// HWPs
					Armor transformedArmor = enviro.GetArmorTransformation(bu.Armor);
					if (transformedArmor != null)
					{
						// change battleunit's armor
						bu.UpdateArmorFromNonSoldier(_game.Mod, transformedArmor, _save.Depth, true, _save.StartingCondition);
					}
				}
			}
		}

		List<MapScript> script = GetMapScripts(ruleDeploy);

		// cleanup before map old map is destroyed
		foreach (var bu in _save.Units)
		{
			bu.ClearVisibleTiles();
			bu.ClearVisibleUnits();
		}

		GenerateMap(script, ruleDeploy.CustomUfoName, null);

		SetupObjectives(ruleDeploy);

		int highestSoldierID = 0;
		bool selectedFirstSoldier = false;
		int soldiersTotal = 0;
		int soldiersPlaced = 0;
		foreach (var bu in _save.Units)
		{
			if (bu.OriginalFaction == FACTION_PLAYER)
			{
				if (!bu.IsOut)
				{
					++soldiersTotal;
					bu.ResetTurnsSinceStunned();
					bu.ResetTurnsSince();
					if (!selectedFirstSoldier && bu.GeoscapeSoldier != null)
					{
						_save.SelectedUnit = bu;
						selectedFirstSoldier = true;
					}
					Node node = _save.GetSpawnNode((int)NodeRank.NR_XCOM, bu);
					if (node != null || PlaceUnitNearFriend(bu))
					{
						++soldiersPlaced;
						if (node != null)
						{
							_save.SetUnitPosition(bu, node.Position);
						}

						_craftInventoryTile ??= bu.Tile;

						bu.SetInventoryTile(_craftInventoryTile);
						bu.Visible = false;
						if (bu.Id > highestSoldierID)
						{
							highestSoldierID = bu.Id;
						}
						//reset TUs, regain energy, etc. but don't take damage or go berserk
						bu.PrepareNewTurn(false);
					}
				}
			}
		}
		if (soldiersPlaced == 0)
		{
			throw new InvalidOperationException("Map generator encountered an error: no xcom units could be placed on the map.");
		}
		else if (soldiersPlaced < soldiersTotal)
		{
			throw new InvalidOperationException($"Map generator encountered an error: not all xcom units could be placed on the map. Placed: {soldiersPlaced} of {soldiersTotal}.");
		}

		if (_save.SelectedUnit == null || _save.SelectedUnit.IsOut || _save.SelectedUnit.Faction != FACTION_PLAYER)
		{
			_save.SelectNextPlayerUnit();
		}
		RuleInventory ground = _inventorySlotGround;

		foreach (var bi in takeToNextStage)
		{
			_save.Items.Add(bi);
			if (bi.Slot == ground)
			{
				_craftInventoryTile.AddItem(bi, ground);
				bi.Unit?.SetPosition(_craftInventoryTile.Position);
			}
		}

		_unitSequence = _save.Units[^1].Id + 1;

		// Let's figure out what race we're up against.
		_alienRace = ruleDeploy.Race;

		if (string.IsNullOrEmpty(_alienRace))
		{
			foreach (var missionSite in _game.SavedGame.MissionSites)
			{
				if (missionSite.IsInBattlescape)
				{
					_alienRace = missionSite.AlienRace;
					break; // loop finished
				}
			}
		}

		if (string.IsNullOrEmpty(_alienRace))
		{
			foreach (var ab in _game.SavedGame.AlienBases)
			{
				if (ab.IsInBattlescape)
				{
					_alienRace = ab.AlienRace;
					break; // loop finished
				}
			}
		}

		int civilianSpawnNodeRank = ruleDeploy.CivilianSpawnNodeRank;
		bool markCiviliansAsVIP = ruleDeploy.MarkCiviliansAsVIP;

		// Special case: deploy civilians before aliens
		if (civilianSpawnNodeRank > 0)
		{
			DeployCivilians(markCiviliansAsVIP, civilianSpawnNodeRank, ruleDeploy.Civilians);
			foreach (var pair in ruleDeploy.CiviliansByType)
			{
				DeployCivilians(markCiviliansAsVIP, civilianSpawnNodeRank, pair.Value, true, pair.Key);
			}
		}

		int unitCount = _save.Units.Count;

		DeployAliens(_alienCustomDeploy != null && _alienCustomDeploy.DeploymentData.Count != 0 ? _alienCustomDeploy : ruleDeploy);

		if (unitCount == _save.Units.Count)
		{
			throw new InvalidOperationException("Map generator encountered an error: no alien units could be placed on the map.");
		}

		// Normal case: deploy civilians after aliens
		if (civilianSpawnNodeRank == 0)
		{
			DeployCivilians(markCiviliansAsVIP, civilianSpawnNodeRank, ruleDeploy.Civilians);
			foreach (var pair in ruleDeploy.CiviliansByType)
			{
				DeployCivilians(markCiviliansAsVIP, civilianSpawnNodeRank, pair.Value, true, pair.Key);
			}
		}

		_save.SetAborted(false);
		SetMusic(ruleDeploy, true);
		_save.GlobalShade = _worldShade;
		_save.TileEngine.CalculateLighting(LightLayers.LL_AMBIENT, TileEngine.invalid, 0, true);
	}

	/// <summary>
	/// Picks the map script of the terrain, overridden by the one of the deployment (shared by <c>run</c> and <c>nextStage</c>).
	/// </summary>
	private List<MapScript> GetMapScripts(AlienDeployment ruleDeploy)
	{
		string terrainMapScript = _terrain.RandomMapScript;
		List<MapScript> script = _game.Mod.GetMapScript(terrainMapScript);
		_save.LastUsedMapScript = terrainMapScript;

		string deployMapScript = ruleDeploy.RandomMapScript;
		if (_game.Mod.GetMapScript(deployMapScript) != null)
		{
			script = _game.Mod.GetMapScript(deployMapScript);
			_save.LastUsedMapScript = deployMapScript;
		}
		else if (!string.IsNullOrEmpty(deployMapScript))
		{
			throw new InvalidOperationException("Map generator encountered an error: " + deployMapScript + " script not found.");
		}
		if (script == null)
		{
			throw new InvalidOperationException("Map generator encountered an error: " + terrainMapScript + " script not found.");
		}
		return script;
	}

	/// <summary>Starts the generator; it fills up the battlescape savegame with data.</summary>
	public void Run()
	{
		bool isPreview = _save.IsPreview;

		_save.SetAlienCustom(_alienCustomDeploy != null ? _alienCustomDeploy.Type : "", _alienCustomMission != null ? _alienCustomMission.Type : "");

		// Note: this considers also fake underwater UFO deployment (via _alienCustomMission)
		AlienDeployment ruleDeploy = _alienCustomMission ?? _game.Mod.GetDeployment(_ufo != null ? _ufo.Rules.Type : _save.MissionType, true);

		_save.TurnLimit = ruleDeploy.TurnLimit;
		_save.ChronoTrigger = ruleDeploy.ChronoTrigger;
		_save.SetCheatTurn(ruleDeploy.CheatTurn);
		ruleDeploy.GetDimensions(ref _mapsize_x, ref _mapsize_y, ref _mapsize_z);

		_unitSequence = BattleUnit.MAX_SOLDIER_ID; // geoscape soldier IDs should stay below this number

		if (_terrain == null)
		{
			var terrains = ruleDeploy.Terrains;
			if (_missionTexture == null || _missionTexture.Terrain.Count == 0 || terrains.Count != 0)
			{
				if (terrains.Count != 0)
				{
					int pick = Rng.Generate(0, terrains.Count - 1);
					_terrain = _game.Mod.GetTerrain(terrains[pick], true);
				}
				else // trouble: no texture and no deployment terrain, most likely scenario is a UFO landing on water: use the first available terrain
				{
					Logger.Warning("Trouble: no texture and no deployment terrain, most likely scenario is a UFO landing on water: using the first available terrain...");
					_terrain = _game.Mod.GetTerrain(_game.Mod.TerrainList[0], true);
				}
			}
			else
			{
				Target target = _ufo;
				if (_mission != null) target = _mission;
				_terrain = _game.Mod.GetTerrain(_missionTexture.GetRandomTerrain(target), true);
			}
		}

		if (_terrain == null)
		{
			throw new InvalidOperationException("Map generator encountered an error: No valid terrain found.");
		}

		SetDepth(ruleDeploy, false);

		if (ruleDeploy.Shade != -1)
		{
			_worldShade = ruleDeploy.Shade;
		}
		else if (ruleDeploy.MinShade != -1 && _worldShade < ruleDeploy.MinShade)
		{
			_worldShade = ruleDeploy.MinShade;
		}
		else if (ruleDeploy.MaxShade != -1 && _worldShade > ruleDeploy.MaxShade)
		{
			_worldShade = ruleDeploy.MaxShade;
		}

		{
			int month;
			int monthsPassed = _game.SavedGame.MonthsPassed;
			if (monthsPassed != -1)
			{
				int levels = _game.Mod.AlienItemLevels.Count;
				// C++ compares as size_t
				month = unchecked((ulong)(long)monthsPassed > (ulong)(long)(levels - 1)) ? levels - 1 : monthsPassed;
			}
			else
			{
				month = _alienItemLevel;
			}

			_save.AlienItemLevel = month;
		}

		List<MapScript> script = GetMapScripts(ruleDeploy);

		if (isPreview && _craftRules != null)
		{
			// resize the map if needed
			foreach (var dims in _craftRules.BattlescapeTerrainData.MapBlocks)
			{
				if (dims.SizeX > _mapsize_x)
				{
					_mapsize_x = dims.SizeX;
				}
				if (dims.SizeY > _mapsize_y)
				{
					_mapsize_y = dims.SizeY;
				}
			}
		}

		RuleStartingCondition startingCondition = _game.Mod.GetStartingCondition(ruleDeploy.StartingCondition);
		if (startingCondition == null && _missionTexture != null)
		{
			startingCondition = _game.Mod.GetStartingCondition(_missionTexture.StartingCondition);
		}
		_save.StartingCondition = startingCondition;

		GenerateMap(script, ruleDeploy.CustomUfoName, startingCondition);

		if (isPreview && ruleDeploy.IsHidden)
		{
			_save.RevealMap();
		}

		SetupObjectives(ruleDeploy);

		RuleEnviroEffects enviro = _game.Mod.GetEnviroEffects(ruleDeploy.EnviroEffects);
		if (enviro == null && _terrain != null)
		{
			enviro = _game.Mod.GetEnviroEffects(_terrain.EnviroEffects);
		}
		DeployXCOM(isPreview ? null : startingCondition, isPreview ? null : enviro);

		int civilianSpawnNodeRank = ruleDeploy.CivilianSpawnNodeRank;
		bool markCiviliansAsVIP = ruleDeploy.MarkCiviliansAsVIP;

		// Special case: deploy civilians before aliens
		if (!isPreview && civilianSpawnNodeRank > 0)
		{
			DeployCivilians(markCiviliansAsVIP, civilianSpawnNodeRank, ruleDeploy.Civilians);
			foreach (var pair in ruleDeploy.CiviliansByType)
			{
				DeployCivilians(markCiviliansAsVIP, civilianSpawnNodeRank, pair.Value, true, pair.Key);
			}
		}

		int unitCount = _save.Units.Count;

		if (!isPreview)
		{
			DeployAliens(_alienCustomDeploy != null && _alienCustomDeploy.DeploymentData.Count != 0 ? _alienCustomDeploy : ruleDeploy);
		}

		if (!isPreview && unitCount == _save.Units.Count)
		{
			throw new InvalidOperationException("Map generator encountered an error: no alien units could be placed on the map.");
		}

		// Normal case: deploy civilians after aliens
		if (!isPreview && civilianSpawnNodeRank == 0)
		{
			DeployCivilians(markCiviliansAsVIP, civilianSpawnNodeRank, ruleDeploy.Civilians);
			foreach (var pair in ruleDeploy.CiviliansByType)
			{
				DeployCivilians(markCiviliansAsVIP, civilianSpawnNodeRank, pair.Value, true, pair.Key);
			}
		}

		if (!isPreview && _craftInventoryTile != null && ruleDeploy.NoWeaponPile)
		{
			SendItemsToLimbo();
		}

		if (!isPreview && _generateFuel)
		{
			FuelPowerSources();
		}

		SetMusic(ruleDeploy, false);
		// set shade (alien bases are a little darker, sites depend on world shade)
		_save.GlobalShade = _worldShade;

		_save.TileEngine.CalculateLighting(LightLayers.LL_AMBIENT, TileEngine.invalid, 0, true);

		if (!isPreview && _ufo != null && _ufo.Status == Ufo.UfoStatus.CRASHED)
		{
			ExplodePowerSources();
		}

		if (!isPreview)
		{
			ExplodeOtherJunk();
		}
	}

	/// <summary>
	/// Creates a mini-battle-save for managing inventory from the Geoscape.
	/// Kids, don't try this at home!
	/// </summary>
	/// <param name="craft">Pointer to craft to manage.</param>
	public void RunInventory(Craft craft)
	{
		// we need to fake a map for soldier placement
		_baseInventory = true;
		_mapsize_x = 2;
		_mapsize_y = 2;
		_mapsize_z = 1;
		_save.InitMap(_mapsize_x, _mapsize_y, _mapsize_z);
		_save.InitUtilities(_mod, true);
		// C++ creates (and deletes again) an unused dummy MapDataSet/MapData here.
		_craftInventoryTile = _save.GetTile(0);

		// ok now generate the battle items for inventory
		if (craft != null) SetCraft(craft);
		DeployXCOM(null, null);
	}

	/// <summary>Sets up the objectives for the map.</summary>
	/// <param name="ruleDeploy">The deployment data we're gleaning data from.</param>
	public void SetupObjectives(AlienDeployment ruleDeploy)
	{
		ArgumentNullException.ThrowIfNull(ruleDeploy);
		// reset bug hunt mode (necessary for multi-stage missions)
		_save.BughuntMode = false;
		// set global min turn
		_save.BughuntMinTurn = _game.Mod.BughuntMinTurn;
		// set min turn override per deployment (if defined)
		if (ruleDeploy.BughuntMinTurn > 0)
		{
			_save.BughuntMinTurn = ruleDeploy.BughuntMinTurn;
		}

		// used for "escort the VIPs" and "protect the VIPs" missions
		_save.VIPSurvivalPercentage = ruleDeploy.VIPSurvivalPercentage;
		_save.VIPEscapeType = ruleDeploy.EscapeType;

		int targetType = ruleDeploy.ObjectiveType;

		if (targetType > -1)
		{
			int objectives = ruleDeploy.ObjectivesRequired;
			int actualCount = 0;

			for (int i = 0; i < _save.MapSizeXYZ; ++i)
			{
				for (int j = (int)O_FLOOR; j < (int)O_MAX; ++j)
				{
					TilePart tp = (TilePart)j;
					MapData md = _save.GetTile(i).GetMapData(tp);
					if (md != null && (int)md.SpecialType == targetType)
					{
						actualCount++;
					}
				}
			}

			if (actualCount > 0)
			{
				_save.SetObjectiveType(targetType);

				if (actualCount < objectives || objectives == 0)
				{
					_save.SetObjectiveCount(actualCount);
				}
				else
				{
					_save.SetObjectiveCount(objectives);
				}
			}
		}
	}

	/// <summary>Sets the depth based on the terrain or the provided AlienDeployment rule.</summary>
	/// <param name="ruleDeploy">The deployment data we're gleaning data from.</param>
	/// <param name="nextStage">Whether the mission is progressing to the next stage.</param>
	private void SetDepth(AlienDeployment ruleDeploy, bool nextStage)
	{
		if (_save.Depth > 0 && !nextStage)
		{
			// new battle menu will have set the depth already
			return;
		}

		if (ruleDeploy.MaxDepth > 0)
		{
			_save.Depth = Rng.Generate(ruleDeploy.MinDepth, ruleDeploy.MaxDepth);
		}
		else if (_terrain.MaxDepth > 0 || nextStage)
		{
			_save.Depth = Rng.Generate(_terrain.MinDepth, _terrain.MaxDepth);
		}
	}

	/// <summary>Sets the background music based on the terrain or the provided AlienDeployment rule.</summary>
	/// <param name="ruleDeploy">The deployment data we're gleaning data from.</param>
	/// <param name="nextStage">Whether the mission is progressing to the next stage.</param>
	private void SetMusic(AlienDeployment ruleDeploy, bool nextStage)
	{
		if (ruleDeploy.Music.Count != 0)
		{
			_save.Music = ruleDeploy.Music[Rng.Generate(0, ruleDeploy.Music.Count - 1)];
		}
		else if (_terrain.Music.Count != 0)
		{
			_save.Music = _terrain.Music[Rng.Generate(0, _terrain.Music.Count - 1)];
		}
		else if (nextStage)
		{
			_save.Music = "";
		}
	}

	/// <summary>
	/// Sorts like C++ <c>std::sort</c>. Deviation: the C++ sort is not stable, so the order of equal
	/// elements is implementation-defined; this sort is stable (equal elements keep their order).
	/// </summary>
	private static void SortStable<T>(List<T> list, Func<T, int> key)
	{
		var sorted = list.OrderBy(key).ToList();
		list.Clear();
		list.AddRange(sorted);
	}
}
