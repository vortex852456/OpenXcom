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

using static Acom.BattleType;
using static Acom.GameDifficulty;
using static Acom.ItemDamageType;
using static Acom.SpecialTileType;
using static Acom.TilePart;
using static Acom.UnitFaction;

namespace Acom;

// Deployment of XCom units/items, aliens and civilians; power sources and other junk.
public partial class BattlescapeGenerator
{
	/// <summary>Deploys all the X-COM units and equipment based on the Geoscape base / craft.</summary>
	private void DeployXCOM(RuleStartingCondition startingCondition, RuleEnviroEffects enviro)
	{
		bool isPreview = _save.IsPreview;

		_save.ApplyEnviroEffects(enviro);

		RuleInventory ground = _inventorySlotGround;

		if (_craft != null)
		{
			_base = _craft.Base;
			_craft.ResetTemporaryCustomVehicleDeploymentFlags();
		}

		// we will need this during debriefing to show a list of recovered items
		// Note: saved info is required only because of base defense missions, other missions could work without a save too
		// IMPORTANT: the number of vehicles and their ammo has been messed up by Base::setupDefenses() already :( and will need to be handled separately later
		if (!isPreview && _base != null)
		{
			ItemContainer rememberMe = _save.BaseStorageItems;
			foreach (var pair in _base.StorageItems.Contents)
			{
				rememberMe.AddItem(pair.Key, pair.Value);
			}
		}

		// add vehicles that are in the craft - a vehicle is actually an item, which you will never see as it is converted to a unit
		// however the item itself becomes the weapon it "holds".
		if (!_baseInventory)
		{
			if (_craft != null)
			{
				foreach (var vehicle in _craft.Vehicles)
				{
					RuleItem item = vehicle.Rules;
					bool hwpDisabled = IsVehicleDisabled(startingCondition, item);
					if (hwpDisabled)
					{
						// send disabled vehicles back to base
						_base.StorageItems.AddItem(item, 1);
						// ammo too, if necessary
						if (item.VehicleClipAmmo != null)
						{
							// Calculate how much ammo needs to be added to the base.
							_base.StorageItems.AddItem(item.VehicleClipAmmo, item.VehicleClipsLoaded);
						}
					}
					else if (item.VehicleUnit.Armor.Size > 1 || Mod.EXTENDED_HWP_LOAD_ORDER == false)
					{
						// 2x2 HWPs first
						BattleUnit unit = AddXCOMVehicle(vehicle);
						if (unit != null && _save.SelectedUnit == null)
							_save.SelectedUnit = unit;
					}
				}
			}
			else if (_base != null)
			{
				// add vehicles that are in the base inventory
				foreach (var vehicle in _base.Vehicles)
				{
					BattleUnit unit = AddXCOMVehicle(vehicle);
					if (unit != null && _save.SelectedUnit == null)
						_save.SelectedUnit = unit;
				}
				// we only add vehicles from the craft in new battle mode,
				// otherwise the base's vehicle vector will already contain these
				// due to the geoscape calling base->setupDefenses()
				if (_game.SavedGame.MonthsPassed == -1)
				{
					foreach (var craft in _base.Crafts)
					{
						foreach (var vehicle in craft.Vehicles)
						{
							BattleUnit unit = AddXCOMVehicle(vehicle);
							if (unit != null && _save.SelectedUnit == null)
								_save.SelectedUnit = unit;
						}
					}
				}
			}
		}

		// enviro effects and starting conditions - armor transformation and replacement
		if (startingCondition != null || enviro != null)
		{
			foreach (var soldier in _base.Soldiers)
			{
				if (IsSoldierDeployed(soldier))
				{
					Armor transformedArmor = null;
					if (enviro != null)
						transformedArmor = enviro.GetArmorTransformation(soldier.Armor);
					if (transformedArmor != null)
					{
						soldier.TransformedArmor = soldier.Armor;
						soldier.SetArmor(transformedArmor);
					}
					else
					{
						Armor replacedArmor = null;
						if (startingCondition != null)
						{
							string replacedArmorType = startingCondition.GetArmorReplacement(soldier.Rules.Type, soldier.Armor.Type);
							replacedArmor = _game.Mod.GetArmor(replacedArmorType, true);
							if (replacedArmor != null && replacedArmor.Size > soldier.Armor.Size)
							{
								// cannot switch into a bigger armor size!
								replacedArmor = null;
							}
						}
						if (replacedArmor != null)
						{
							soldier.ReplacedArmor = soldier.Armor;
							soldier.SetArmor(replacedArmor);
						}
					}
				}
			}
		}

		// add soldiers that are in the craft or base (2x2 only)
		foreach (var soldier in _base.Soldiers)
		{
			if (soldier.Armor.Size == 1)
			{
				continue;
			}
			if (IsSoldierDeployed(soldier))
			{
				AddXCOMSoldier(soldier);
			}
		}

		// add remaining 1x1 craft vehicles
		if (!_baseInventory && Mod.EXTENDED_HWP_LOAD_ORDER)
		{
			if (_craft != null)
			{
				foreach (var vehicle in _craft.Vehicles)
				{
					RuleItem item = vehicle.Rules;
					bool hwpDisabled = IsVehicleDisabled(startingCondition, item);
					if (hwpDisabled)
					{
						// skip, already done earlier
					}
					else if (item.VehicleUnit.Armor.Size == 1)
					{
						// 1x1 HWPs last
						BattleUnit unit = AddXCOMVehicle(vehicle);
						if (unit != null && _save.SelectedUnit == null)
							_save.SelectedUnit = unit;
					}
				}
			}
		}

		// add soldiers that are in the craft or base (1x1 only)
		foreach (var soldier in _base.Soldiers)
		{
			if (soldier.Armor.Size > 1)
			{
				continue;
			}
			if (IsSoldierDeployed(soldier))
			{
				AddXCOMSoldier(soldier);
			}
		}

		// job's done
		_game.SavedGame.DisableSoldierEquipment = false;

		// Corner case: the base has some soldiers, but nowhere to spawn them (e.g. after a previous base defense destroyed everything but access lift)
		if (_save.Units.Count == 0 && _save.MissionType == "STR_BASE_DEFENSE")
		{
			// let's force-spawn a dummy unit and auto-fail the battle
			string firstRace = _game.Mod.AlienRacesList[0];
			AlienRace raceRule = _game.Mod.GetAlienRace(firstRace);
			string firstUnit = raceRule.GetMember(0);
			Unit unitRule = _game.Mod.GetUnit(firstUnit);
			BattleUnit unit = _save.CreateTempUnit(unitRule, FACTION_PLAYER, _unitSequence++);
			unit.SetSummonedPlayerUnit(true); // auto-fail battle
			_save.SelectedUnit = unit;

			Tile firstTile = _save.GetTile(0);
			_save.SetUnitPosition(unit, firstTile.Position);
			_save.Units.Add(unit);
			_craftInventoryTile = firstTile;
		}

		if (_save.Units.Count == 0)
		{
			throw new InvalidOperationException("Map generator encountered an error: no xcom units could be placed on the map.");
		}

		// maybe we should assign all units to the first tile of the skyranger before the inventory pre-equip and then reassign them to their correct tile afterwards?
		// fix: make them invisible, they are made visible afterwards.
		foreach (var bu in _save.Units)
		{
			if (bu.Faction == FACTION_PLAYER)
			{
				bu.SetInventoryTile(_craftInventoryTile);
				bu.Visible = false;
			}
		}

		if (isPreview)
		{
			// alright, that's enough
			return;
		}

		if (_craft != null)
		{
			// add items that are in the craft
			foreach (var pair in _craft.Items.Contents)
			{
				if (startingCondition != null && !startingCondition.IsItemPermitted(pair.Key.Type, _game.Mod, _craft))
				{
					// send disabled items back to base
					_base.StorageItems.AddItem(pair.Key, pair.Value);
				}
				else
				{
					for (int count = 0; count < pair.Value; count++)
					{
						_save.CreateItemForTile(pair.Key, _craftInventoryTile);
					}
				}
			}
		}
		else
		{
			// only use the items in the craft in new battle mode.
			if (_game.SavedGame.MonthsPassed != -1)
			{
				// add items that are in the base
				// (snapshot: the current entry may get removed while iterating)
				foreach (var pair in _base.StorageItems.Contents.ToList())
				{
					RuleItem rule = pair.Key;
					if (
						// is item allowed in base defense?
						rule.CanBeEquippedBeforeBaseDefense() &&
						// only put items in the battlescape that make sense (when the item got a sprite, it's probably ok)
						rule.IsInventoryItem &&
						// if item can be equip to craft then you should be able to use it in base defense
						// in some cases we forbid some items from craft but still allow them in base defense if normally they were available.
						(rule.IsUsefulBattlescapeItem || rule.CanBeEquippedToCraftInventory()) &&
						// we know how to use this item
						_game.SavedGame.IsResearched(rule.Requirements))
					{
						for (int count = 0; count < pair.Value; count++)
						{
							_save.CreateItemForTile(pair.Key, _craftInventoryTile);
						}
						if (!_baseInventory)
						{
							_base.StorageItems.RemoveItem(pair.Key, pair.Value);
						}
					}
				}
			}
			// add items from crafts in base
			foreach (var craft in _base.Crafts)
			{
				if (craft.Status == "STR_OUT")
					continue;
				foreach (var pair in craft.Items.Contents)
				{
					for (int count = 0; count < pair.Value; count++)
					{
						_save.CreateItemForTile(pair.Key, _craftInventoryTile);
					}
				}
			}
		}

		var tempItemList = new List<BattleItem>(_craftInventoryTile.Inventory);

		// equip soldiers based on equipment-layout
		foreach (BattleItem bi in tempItemList)
		{
			// set all the items on this tile as belonging to the XCOM faction.
			bi.XCOMProperty = true;
			// don't let the soldiers take extra ammo yet
			if (bi.Rules.BattleType == BT_AMMO && !Options.oxceAlternateCraftEquipmentManagement)
				continue;
			PlaceItemByLayout(bi, tempItemList);
		}

		// load fixed weapons based on equipment layout
		ReloadFixedWeaponsByLayout();

		// refresh list
		tempItemList = [.. _craftInventoryTile.Inventory];

		// sort it, so that ammo is loaded in a predictable and moddable manner
		SortStable(tempItemList, a => a.Rules.LoadOrder);

		// load weapons before loadouts take extra clips.
		LoadWeapons(tempItemList);

		// refresh list
		// (unsorts it again, which is not a problem)
		tempItemList = [.. _craftInventoryTile.Inventory];

		foreach (BattleItem bi in tempItemList)
		{
			// we only need to distribute extra ammo at this point.
			if (bi.Rules.BattleType != BT_AMMO)
				continue;
			PlaceItemByLayout(bi, tempItemList);
		}

		// refresh list
		tempItemList = [.. _craftInventoryTile.Inventory];

		// auto-equip soldiers (only soldiers without layout) and clean up moved items
		AutoEquip(_save.Units, _game.Mod, tempItemList, ground, _worldShade, _allowAutoLoadout, false);
	}

	/// <summary>Whether a craft HWP is disabled by the starting condition (the vehicle itself or its ammo).</summary>
	private bool IsVehicleDisabled(RuleStartingCondition startingCondition, RuleItem item)
	{
		bool hwpDisabled = false;
		if (startingCondition != null)
		{
			if (!startingCondition.IsVehiclePermitted(item.Type))
			{
				hwpDisabled = true; // HWP is disabled
			}
			else if (item.VehicleClipAmmo != null)
			{
				if (!startingCondition.IsItemPermitted(item.VehicleClipAmmo.Type, _game.Mod, _craft))
				{
					hwpDisabled = true; // HWP's ammo is disabled
				}
			}
		}
		return hwpDisabled;
	}

	/// <summary>Whether a soldier of the base takes part in the battle (in the craft, or able to defend the base).</summary>
	private bool IsSoldierDeployed(Soldier soldier)
	{
		return (_craft != null && soldier.Craft == _craft) ||
			(_craft == null && (soldier.HasFullHealth || soldier.CanDefendBase()) && (soldier.Craft == null || soldier.Craft.Status != "STR_OUT"));
	}

	/// <summary>Converts a geoscape soldier into a battle unit and adds it (shared part of the 2x2 and 1x1 soldier loops).</summary>
	private void AddXCOMSoldier(Soldier soldier)
	{
		// clear the soldier's equipment layout, we want to start fresh
		if (_game.SavedGame.DisableSoldierEquipment)
		{
			soldier.ClearEquipmentLayout();
		}
		BattleUnit unit = AddXCOMUnit(new BattleUnit(_game.Mod, soldier, _save.Depth, _save.StartingCondition));
		if (unit != null && _save.SelectedUnit == null)
			_save.SelectedUnit = unit;
	}

	/// <summary>Auto-equip a set of units.</summary>
	public static void AutoEquip(List<BattleUnit> units, Mod mod, List<BattleItem> craftInv, RuleInventory groundRuleInv, int worldShade, bool allowAutoLoadout, bool overrideEquipmentLayout)
	{
		ArgumentNullException.ThrowIfNull(units);
		ArgumentNullException.ThrowIfNull(mod);
		ArgumentNullException.ThrowIfNull(craftInv);
		for (int pass = 0; pass < 4; ++pass)
		{
			for (int iter = 0; iter < craftInv.Count;)
			{
				BattleItem bi = craftInv[iter];
				if (bi.Rules.InventoryHeight == 0 || bi.Rules.InventoryWidth == 0)
				{
					// don't autoequip hidden items, whatever they are
					++iter;
					continue;
				}
				if (bi.Slot == groundRuleInv)
				{
					bool add = false;

					switch (pass)
					{
					// priority 1: rifles.
					case 0:
						add = bi.Rules.IsRifle;
						break;
					// priority 2: pistols (assuming no rifles were found).
					case 1:
						add = bi.Rules.IsPistol;
						break;
					// priority 3: ammunition.
					case 2:
						add = bi.Rules.BattleType == BT_AMMO;
						break;
					// priority 4: leftovers.
					case 3:
						add = !bi.Rules.IsPistol &&
								!bi.Rules.IsRifle &&
								(bi.Rules.BattleType != BT_FLARE || worldShade > mod.MaxDarknessToSeeUnits);
						break;
					default:
						break;
					}

					if (add)
					{
						foreach (var bu in units)
						{
							if (!bu.HasInventory || bu.GeoscapeSoldier == null || (!overrideEquipmentLayout && bu.GeoscapeSoldier.EquipmentLayout.Count != 0))
							{
								continue;
							}
							// let's not be greedy, we'll only take a second extra clip
							// if everyone else has had a chance to take a first.
							bool allowSecondClip = pass == 3;
							if (bu.AddItem(bi, mod, allowSecondClip, allowAutoLoadout))
							{
								craftInv.RemoveAt(iter);
								add = false;
								break;
							}
						}
						if (!add)
						{
							continue;
						}
					}
				}
				++iter;
			}
		}
	}

	/// <summary>
	/// Adds an XCom vehicle to the game.
	/// Sets the correct turret depending on the ammo type.
	/// </summary>
	/// <param name="v">Pointer to the Vehicle.</param>
	/// <returns>Pointer to the spawned unit.</returns>
	private BattleUnit AddXCOMVehicle(Vehicle v)
	{
		Unit rule = v.Rules.VehicleUnit;
		BattleUnit unit = AddXCOMUnit(_save.CreateTempUnit(rule, FACTION_PLAYER, _unitSequence++));
		if (unit != null)
		{
			if (!_save.IsPreview)
			{
				_save.CreateItemForUnit(v.Rules, unit, true);
				if (v.Rules.VehicleClipAmmo != null)
				{
					BattleItem ammoItem = _save.CreateItemForUnit(v.Rules.VehicleClipAmmo, unit);
					if (ammoItem != null)
					{
						ammoItem.AmmoQuantity = v.Ammo;
					}
				}
			}
			unit.TurretType = v.Rules.TurretType;
		}
		return unit;
	}

	/// <summary>
	/// Adds a soldier to the game and places him on a free spawnpoint.
	/// Spawnpoints are either tiles in case of an XCom craft that landed.
	/// Or they are map nodes in case there's no craft.
	/// </summary>
	/// <param name="unit">Pointer to the unit.</param>
	/// <returns>Pointer to the spawned unit (null if it could not be placed).</returns>
	private BattleUnit AddXCOMUnit(BattleUnit unit)
	{
		if (_save.IsPreview && _save.CraftForPreview == null)
		{
			// base preview, one standard unit is enough
			if (_save.Units.Count > 0 && !_save.Units[^1].IsSummonedPlayerUnit)
			{
				return null; // C++ deletes the unit
			}
		}

		if (_baseInventory)
		{
			if (unit.HasInventory)
			{
				_save.Units.Add(unit);
				_save.InitUnit(unit);
				return unit;
			}
		}
		else
		{
			if (_craft == null || !_craftDeployed)
			{
				SetCustomCraftInventoryTile();

				Node node = _save.GetSpawnNode((int)NodeRank.NR_XCOM, unit);
				if (node != null)
				{
					_save.SetUnitPosition(unit, node.Position);
					_craftInventoryTile ??= _save.GetTile(node.Position);
					unit.Direction = Rng.Generate(0, 7);
					_save.Units.Add(unit);
					_save.InitUnit(unit);
					return unit;
				}
				else if (_save.MissionType != "STR_BASE_DEFENSE")
				{
					if (PlaceUnitNearFriend(unit))
					{
						_craftInventoryTile ??= _save.GetTile(unit.Position);
						unit.Direction = Rng.Generate(0, 7);
						_save.Units.Add(unit);
						_save.InitUnit(unit);
						return unit;
					}
				}
			}
			else if (_craft != null && _craft.HasCustomDeployment && _craft.Rules == _craftRules)
			{
				SetCustomCraftInventoryTile();

				bool canPlace = false;
				Position pos = default;
				int dir = 0;
				if (unit.GeoscapeSoldier != null)
				{
					var depl = _craft.CustomSoldierDeployment[unit.GeoscapeSoldier.Id]; // crashes if not found
					pos = depl.first + new Position(_craftPos.x * 10, _craftPos.y * 10, _craftZ);
					dir = depl.second;
					canPlace = true;
				}
				else
				{
					var deplVec = _craft.CustomVehicleDeployment;
					foreach (var depl in deplVec)
					{
						if (depl.type == unit.Type && !depl.used)
						{
							pos = depl.pos + new Position(_craftPos.x * 10, _craftPos.y * 10, _craftZ);
							dir = depl.dir;
							canPlace = true;
							depl.used = true; // mark as used, i.e. don't try the same for the next vehicle unit
							break;
						}
					}
				}
				if (!canPlace)
				{
					throw new InvalidOperationException("Unit generator encountered an error: custom craft deployment failed.");
				}
				else
				{
					for (int x = 0; x < unit.Armor.Size; ++x)
					{
						for (int y = 0; y < unit.Armor.Size; ++y)
						{
							canPlace = canPlace && CanPlaceXCOMUnit(_save.GetTile(pos + new Position(x, y, 0)));
						}
					}
				}
				if (canPlace)
				{
					if (_save.SetUnitPosition(unit, pos))
					{
						_save.Units.Add(unit);
						_save.InitUnit(unit);
						unit.Direction = dir;
						return unit;
					}
				}
			}
			else if (_craft != null && _save.HasCustomDeployment(_craftRules))
			{
				SetCustomCraftInventoryTile();

				var deploy = _save.GetCustomDeployment(_craftRules);
				foreach (var vec in deploy)
				{
					Position pos = new(vec[0] + (_craftPos.x * 10), vec[1] + (_craftPos.y * 10), vec[2] + _craftZ);
					int dir = vec[3];
					bool canPlace = true;
					for (int x = 0; x < unit.Armor.Size; ++x)
					{
						for (int y = 0; y < unit.Armor.Size; ++y)
						{
							canPlace = canPlace && CanPlaceXCOMUnit(_save.GetTile(pos + new Position(x, y, 0)));
						}
					}
					if (canPlace)
					{
						if (_save.SetUnitPosition(unit, pos))
						{
							_save.Units.Add(unit);
							_save.InitUnit(unit);
							unit.Direction = dir;
							return unit;
						}
					}
				}
			}
			else
			{
				SetCustomCraftInventoryTile();

				for (int i = 0; i < _mapsize_x * _mapsize_y * _mapsize_z; ++i)
				{
					if (CanPlaceXCOMUnit(_save.GetTile(i)))
					{
						if (_save.SetUnitPosition(unit, _save.GetTile(i).Position))
						{
							_save.Units.Add(unit);
							_save.InitUnit(unit);
							return unit;
						}
					}
				}
			}
		}
		return null; // C++ deletes the unit
	}

	/// <summary>Tries to set a custom craft inventory tile.</summary>
	private void SetCustomCraftInventoryTile()
	{
		if (_craftInventoryTile == null && _craft != null && _craftDeployed && _craftRules != null)
		{
			// Craft inventory tile position defined in the ruleset
			var coords = _craftRules.CraftInventoryTile;
			if (coords.Count >= 3)
			{
				Position craftInventoryTilePosition = new(coords[0] + (_craftPos.x * 10), coords[1] + (_craftPos.y * 10), coords[2] + _craftZ);
				CanPlaceXCOMUnit(_save.GetTile(craftInventoryTilePosition));
			}
		}
		if (_craftInventoryTile == null)
		{
			// Mapblock inventory tile position defined in the ruleset
			if (_backupInventoryTiles.Count != 0)
			{
				int pilePick = Rng.Generate(0, _backupInventoryTiles.Count - 1);
				Tile pileTile = _backupInventoryTiles[pilePick];
				_craftInventoryTile = pileTile; // no checks
			}
		}
	}

	/// <summary>Checks if a soldier/tank can be placed on a given tile.</summary>
	/// <param name="tile">The given tile.</param>
	/// <returns>Whether the unit can be placed here.</returns>
	private bool CanPlaceXCOMUnit(Tile tile)
	{
		// to spawn an xcom soldier, there has to be a tile, with a floor, with the starting point attribute and no object in the way
		if (tile != null &&
			tile.FloorSpecialTileType == START_POINT &&
			tile.GetMapData(O_OBJECT) == null &&
			tile.GetMapData(O_FLOOR) != null && // for clarity this is checked again, first time was in `getFloorSpecialTileType`
			tile.GetMapData(O_FLOOR).GetTUCost(MovementType.MT_WALK) != Pathfinding.INVALID_MOVE_COST)
		{
			_craftInventoryTile ??= tile;

			return true;
		}
		return false;
	}

	/// <summary>Deploys the aliens, according to the alien deployment rules.</summary>
	/// <param name="deployment">Pointer to the deployment rules.</param>
	private void DeployAliens(AlienDeployment deployment)
	{
		// race defined by deployment if there is one.
		string tmpRace = deployment.Race;
		if (!string.IsNullOrEmpty(tmpRace) && _game.SavedGame.MonthsPassed > -1)
		{
			_alienRace = tmpRace;
		}

		if (_save.Depth > 0 && !_alienRace.Contains("_UNDERWATER", StringComparison.Ordinal))
		{
			_alienRace += "_UNDERWATER";
		}

		AlienRace race = _game.Mod.GetAlienRace(_alienRace);
		if (race == null)
		{
			throw new InvalidOperationException("Map generator encountered an error: Unknown race: " + _alienRace + " defined in deployment: " + deployment.Type);
		}

		// save for later use
		_save.ReinforcementsDeployment = deployment.Type;
		_save.ReinforcementsRace = race.Id;
		_save.ReinforcementsItemLevel = _save.AlienItemLevel;
		// and reset memory at each stage
		_save.ReinforcementsMemory.Clear();

		foreach (var dd in deployment.DeploymentData)
		{
			int quantity;

			switch (_game.SavedGame.Difficulty)
			{
			case DIFF_BEGINNER:
				quantity = dd.lowQty;
				break;
			case DIFF_EXPERIENCED:
				quantity = dd.medQty > 0 ? dd.lowQty + ((dd.medQty - dd.lowQty) / 2) : dd.lowQty;
				break;
			case DIFF_VETERAN:
				quantity = dd.medQty > 0 ? dd.medQty : dd.lowQty + ((dd.highQty - dd.lowQty) / 2);
				break;
			case DIFF_GENIUS:
				quantity = dd.medQty > 0 ? dd.medQty + ((dd.highQty - dd.medQty) / 2) : dd.lowQty + ((dd.highQty - dd.lowQty) / 2);
				break;
			case DIFF_SUPERHUMAN:
			default:
				quantity = dd.highQty;
				break;
			}

			quantity += _mod.IsDemigod ? dd.dQty : Rng.Generate(0, dd.dQty); // maximium spawns for demigod
			quantity += _mod.IsDemigod ? dd.extraQty : Rng.Generate(0, dd.extraQty);

			for (int i = 0; i < quantity; ++i)
			{
				// if the UFO was damaged, each alien (after the first) has a chance to not spawn (during base defense)
				if (_ufoDamagePercentage > 0)
				{
					if (i > 0 && Rng.Percent(_ufoDamagePercentage))
					{
						continue;
					}
				}

				string alienName = string.IsNullOrEmpty(dd.customUnitType) ? race.GetMember(dd.alienRank) : dd.customUnitType;

				bool outside = Rng.Percent(dd.percentageOutsideUfo);
				if (_ufo == null && !deployment.ForcePercentageOutsideUfo)
				{
					outside = false;
				}
				Unit rule = _game.Mod.GetUnit(alienName, true);
				BattleUnit unit = AddAlien(rule, dd.alienRank, outside);
				int itemLevel = _game.Mod.AlienItemLevels[_save.AlienItemLevel][Rng.Generate(0, 9)];
				if (unit != null)
				{
					_save.InitUnit(unit, itemLevel);
					if (!rule.IsLivingWeapon)
					{
						if (dd.itemSets.Count == 0)
						{
							throw new InvalidOperationException("Unit generator encountered an error: item set not defined");
						}
						// C++ compares as size_t
						if ((uint)itemLevel >= (uint)dd.itemSets.Count)
						{
							itemLevel = dd.itemSets.Count - 1;
						}
						foreach (var itemType in dd.itemSets[itemLevel].items)
						{
							RuleItem ruleItem = _game.Mod.GetItem(itemType);
							if (ruleItem != null)
							{
								_save.CreateItemForUnit(ruleItem, unit);
							}
						}
						foreach (var iset in dd.extraRandomItems)
						{
							if (iset.items.Count == 0)
								continue;
							int pick = Rng.Generate(0, iset.items.Count - 1);
							RuleItem ruleItem = _game.Mod.GetItem(iset.items[pick]);
							if (ruleItem != null)
							{
								_save.CreateItemForUnit(ruleItem, unit);
							}
						}
					}
				}
			}
		}
	}

	/// <summary>Node rank to search for a spawn point: <c>Node::nodeRank[alienRank][i]</c>.</summary>
	/// <remarks>Read through <see cref="Array"/> so this compiles against both the <c>int[]</c> Node skeleton and the ported <c>int[,]</c> table.</remarks>
	private static int SpawnNodeRank(int alienRank, int i) => (int)((Array)Node.nodeRank).GetValue(alienRank, i);

	/// <summary>Adds an alien to the game and places him on a free spawnpoint.</summary>
	/// <param name="rules">Pointer to the Unit which holds info about the alien.</param>
	/// <param name="alienRank">The rank of the alien, used for spawn point search.</param>
	/// <param name="outside">Whether the alien should spawn outside or inside the UFO.</param>
	/// <returns>Pointer to the created unit.</returns>
	private BattleUnit AddAlien(Unit rules, int alienRank, bool outside)
	{
		BattleUnit unit = _save.CreateTempUnit(rules, FACTION_HOSTILE, _unitSequence++);
		Node node = null;

		// safety to avoid index out of bounds errors
		if (alienRank > 7)
			alienRank = 7;
		/* following data is the order in which certain alien ranks spawn on certain node ranks */
		/* note that they all can fall back to rank 0 nodes - which is scout (outside ufo) */

		for (int i = 0; i < 7 && node == null; ++i)
		{
			if (outside)
				node = _save.GetSpawnNode(0, unit); // when alien is instructed to spawn outside, we only look for node 0 spawnpoints
			else
				node = _save.GetSpawnNode(SpawnNodeRank(alienRank, i), unit);
		}

		int aliensFacingCraftOdds = 20 * _game.SavedGame.DifficultyCoefficient;
		{
			int diff = (int)_game.SavedGame.Difficulty;
			var custom = _game.Mod.AliensFacingCraftOdds;
			if (custom.Count > diff)
			{
				aliensFacingCraftOdds = custom[diff];
			}
		}

		if (node != null && _save.SetUnitPosition(unit, node.Position))
		{
			unit.AIModule.SetStartNode(node);
			unit.RankInt = alienRank;
			int dir = _save.TileEngine.FaceWindow(node.Position);
			Position craft = _game.SavedGame.SavedBattle.Units[0].Position;
			if (Position.Distance2d(node.Position, craft) <= 20 && Rng.Percent(aliensFacingCraftOdds))
				dir = unit.DirectionTo(craft);
			if (dir != -1)
				unit.Direction = dir;
			else
				unit.Direction = Rng.Generate(0, 7);

			// we only add a unit if it has a node to spawn on.
			// (stops them spawning at 0,0,0)
			_save.Units.Add(unit);
		}
		else
		{
			// DEMIGOD DIFFICULTY: screw the player: spawn as many aliens as possible.
			if ((_game.Mod.IsDemigod || Mod.EXTENDED_FORCE_SPAWN) && PlaceUnitNearFriend(unit))
			{
				unit.RankInt = alienRank;
				int dir = _save.TileEngine.FaceWindow(unit.Position);
				Position craft = _game.SavedGame.SavedBattle.Units[0].Position;
				if (Position.Distance2d(unit.Position, craft) <= 20 && Rng.Percent(aliensFacingCraftOdds))
					dir = unit.DirectionTo(craft);
				if (dir != -1)
					unit.Direction = dir;
				else
					unit.Direction = Rng.Generate(0, 7);

				_save.Units.Add(unit);
			}
			else
			{
				unit = null; // C++ deletes the unit
			}
		}

		return unit;
	}

	/// <summary>Adds a civilian to the game and places him on a free spawnpoint.</summary>
	/// <param name="rules">Pointer to the Unit which holds info about the civilian.</param>
	/// <param name="nodeRank">The node rank to spawn on.</param>
	/// <returns>Pointer to the created unit.</returns>
	private BattleUnit AddCivilian(Unit rules, int nodeRank)
	{
		BattleUnit unit = _save.CreateTempUnit(rules, FACTION_NEUTRAL, _unitSequence++);
		Node node = _save.GetSpawnNode(nodeRank, unit);

		if (node != null)
		{
			_save.SetUnitPosition(unit, node.Position);
			unit.AIModule.SetStartNode(node);
			unit.Direction = Rng.Generate(0, 7);

			// we only add a unit if it has a node to spawn on.
			// (stops them spawning at 0,0,0)
			_save.Units.Add(unit);
		}
		else if (PlaceUnitNearFriend(unit))
		{
			unit.Direction = Rng.Generate(0, 7);
			_save.Units.Add(unit);
		}
		else
		{
			unit = null; // C++ deletes the unit
		}
		return unit;
	}

	/// <summary>Places an item on an XCom soldier based on equipment layout.</summary>
	/// <param name="item">Pointer to the Item.</param>
	/// <param name="itemList">The items to look for layout ammo in.</param>
	/// <returns>Whether the item was placed.</returns>
	private bool PlaceItemByLayout(BattleItem item, List<BattleItem> itemList)
	{
		if (item.Slot == _inventorySlotGround)
		{
			// find the first soldier with a matching layout-slot
			foreach (var unit in _save.Units)
			{
				// skip the vehicles, we need only X-Com soldiers WITH equipment-layout
				if (unit.GeoscapeSoldier == null || unit.GeoscapeSoldier.EquipmentLayout.Count == 0)
				{
					continue;
				}

				// find the first matching layout-slot which is not already occupied
				foreach (var layoutItem in unit.GeoscapeSoldier.EquipmentLayout)
				{
					// fixed items will be handled elsewhere
					if (layoutItem.IsFixed) continue;

					if (item.Rules != layoutItem.ItemType) continue;

					// we need to check all "slot boxes" for overlap (not just top left)
					bool overlaps = false;
					for (int x = 0; x < item.Rules.InventoryWidth; ++x)
					{
						for (int y = 0; y < item.Rules.InventoryHeight; ++y)
						{
							if (!overlaps && unit.GetItem(layoutItem.Slot, x + layoutItem.SlotX, y + layoutItem.SlotY) != null)
							{
								overlaps = true;
							}
						}
					}
					if (overlaps) continue;

					int toLoad = CountLayoutAmmo(layoutItem);

					if (toLoad != 0)
					{
						// maybe we find the layout-ammo on the ground to load it with
						toLoad = LoadLayoutAmmo(item, layoutItem, itemList, toLoad);
					}

					// only place the weapon onto the soldier when it's loaded with its layout-ammo (if any)
					if (toLoad == 0 || item.HaveAnyAmmo())
					{
						item.MoveToOwner(unit);
						item.Slot = layoutItem.Slot;
						item.SlotX = layoutItem.SlotX;
						item.SlotY = layoutItem.SlotY;
						if (Options.includePrimeStateInSavedLayout && item.Rules.FuseTimerType != BattleFuseType.BFT_NONE)
						{
							item.FuseTimer = layoutItem.FuseTimer;
						}
						return true;
					}
				}
			}
		}
		return false;
	}

	/// <summary>Counts the ammo slots of a layout item that request ammo.</summary>
	private static int CountLayoutAmmo(EquipmentLayoutItem layoutItem)
	{
		int toLoad = 0;
		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			if (layoutItem.GetAmmoItemForSlot(slot) != null)
			{
				++toLoad;
			}
		}
		return toLoad;
	}

	/// <summary>Loads the layout ammo found on the ground into a weapon; returns how many ammo slots are still to load.</summary>
	/// <remarks>
	/// For fixed weapons C++ iterates the live tile inventory, from which loaded ammo gets erased; the index loop
	/// keeps that vector behaviour (the element after an erased one is skipped) instead of failing like a C# enumerator.
	/// </remarks>
	private int LoadLayoutAmmo(BattleItem weapon, EquipmentLayoutItem layoutItem, List<BattleItem> itemList, int toLoad)
	{
		for (int k = 0; k < itemList.Count; ++k)
		{
			BattleItem ammo = itemList[k];
			if (ammo.Slot == _inventorySlotGround)
			{
				for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
				{
					if (ammo.Rules == layoutItem.GetAmmoItemForSlot(slot))
					{
						if (weapon.SetAmmoPreMission(ammo))
						{
							--toLoad;
						}
						// even if item was not loaded other slots can't use it either
						break;
					}
				}
				if (toLoad == 0)
				{
					break;
				}
			}
		}
		return toLoad;
	}

	/// <summary>Reloads fixed weapons on XCom soldiers based on equipment layout.</summary>
	private void ReloadFixedWeaponsByLayout()
	{
		// go through all soldiers
		foreach (var unit in _save.Units)
		{
			// skip the vehicles, we need only X-Com soldiers WITH equipment-layout
			if (unit.GeoscapeSoldier == null || unit.GeoscapeSoldier.EquipmentLayout.Count == 0)
			{
				continue;
			}

			// find fixed weapons in the layout
			foreach (var layoutItem in unit.GeoscapeSoldier.EquipmentLayout)
			{
				if (layoutItem.IsFixed == false) continue;

				// find matching fixed weapon in the inventory
				BattleItem fixedItem = null;
				foreach (var item in unit.Inventory)
				{
					if (item.Slot == layoutItem.Slot &&
						item.SlotX == layoutItem.SlotX &&
						item.SlotY == layoutItem.SlotY &&
						item.Rules == layoutItem.ItemType)
					{
						fixedItem = item;
						break;
					}
				}
				if (fixedItem == null) continue;

				int toLoad = CountLayoutAmmo(layoutItem);

				if (toLoad != 0)
				{
					// maybe we find the layout-ammo on the ground to load it with
					LoadLayoutAmmo(fixedItem, layoutItem, _craftInventoryTile.Inventory, toLoad);
				}
			}
		}
	}

	/// <summary>Loads all XCom weaponry before anything else is distributed.</summary>
	private void LoadWeapons(List<BattleItem> itemList)
	{
		// let's try to load this weapon, whether we equip it or not.
		foreach (BattleItem i in itemList)
		{
			if (i.IsWeaponWithAmmo &&
				!i.HaveAllAmmo() &&
				!i.Rules.IsFixed)
			{
				foreach (BattleItem j in itemList)
				{
					if (j.Slot == _inventorySlotGround && i.SetAmmoPreMission(j))
					{
						if (i.HaveAllAmmo())
						{
							break;
						}
					}
				}
			}
		}
	}

	/// <summary>Hide the "weapon pile".</summary>
	private void SendItemsToLimbo()
	{
		List<BattleItem> takeHomeGuaranteed = _save.GuaranteedRecoveredItems;
		foreach (var bi in _craftInventoryTile.Inventory)
		{
			bi.Tile = null;
			bi.FuseTimer = -1;
			takeHomeGuaranteed.Add(bi);

			for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
			{
				BattleItem ammo = bi.GetAmmoForSlot(slot);
				if (ammo != null && ammo != bi)
				{
					ammo.Tile = null;
					ammo.FuseTimer = -1;
					takeHomeGuaranteed.Add(ammo);
				}
			}
		}
		_craftInventoryTile.Inventory.Clear();

		// still need to clean up the full item list
		// (C++: sort both lists by address and std::set_difference; the multiset difference is the same)
		List<BattleItem> allItems = _save.Items;
		var remove = new Dictionary<BattleItem, int>(ReferenceEqualityComparer.Instance);
		foreach (var bi in takeHomeGuaranteed)
		{
			remove[bi] = remove.GetValueOrDefault(bi) + 1;
		}
		var difference = new List<BattleItem>();
		foreach (var bi in allItems)
		{
			if (remove.TryGetValue(bi, out int n) && n > 0)
			{
				remove[bi] = n - 1;
			}
			else
			{
				difference.Add(bi);
			}
		}

		allItems.Clear();
		allItems.AddRange(difference);

		// beautify
		SortStable(allItems, a => a.Id);
		SortStable(takeHomeGuaranteed, a => a.Id);
	}

	/// <summary>Fill power sources with an alien fuel object.</summary>
	private void FuelPowerSources()
	{
		for (int i = 0; i < _save.MapSizeXYZ; ++i)
		{
			if (_save.GetTile(i).ObjectSpecialTileType == UFO_POWER_SOURCE)
			{
				_save.CreateItemForTile(_game.Mod.AlienFuelName, _save.GetTile(i));
			}
		}
	}

	/// <summary>When a UFO crashes, there is a 75% chance for each power source to explode.</summary>
	private void ExplodePowerSources()
	{
		for (int i = 0; i < _save.MapSizeXYZ; ++i)
		{
			if (_save.GetTile(i).ObjectSpecialTileType == UFO_POWER_SOURCE && Rng.Percent(75))
			{
				Position tilePos = _save.GetTile(i).Position;
				Position pos = new(tilePos.x * 16, tilePos.y * 16, (tilePos.z * 24) + 12);
				_save.TileEngine.Explode(new BattleActionAttack(), pos, 180 + Rng.Generate(0, 70), _save.Mod.GetDamageType(DT_HE), 10);
			}
		}
		Tile t = _save.TileEngine.CheckForTerrainExplosions();
		while (t != null)
		{
			int power = t.Explosive;
			t.SetExplosive(0, 0, true);
			Position p = t.Position.ToVoxel() + new Position(8, 8, 0);
			_save.TileEngine.Explode(new BattleActionAttack(), p, power, _game.Mod.GetDamageType(DT_HE), power / 10);
			t = _save.TileEngine.CheckForTerrainExplosions();
		}
	}

	/// <summary>Terraforming.</summary>
	private void ExplodeOtherJunk()
	{
		var itemsToGoBoom = new List<BattleItem>();
		var explosionParams = new List<(Tile tile, RuleItem rule)>();

		foreach (var bi in _save.Items)
		{
			if (bi.IsOwnerIgnored || bi.Tile == null)
			{
				continue;
			}
			if ((bi.Rules.SpawnUnit == null && bi.Rules.SpawnItem == null) && !bi.XCOMProperty && !bi.IsSpecialWeapon)
			{
				// fuseTimer == 0 cannot be used, because it is already used for "explode after the first player turn"
				// fuseTimer == -1 is also used, means "unprimed grenade"
				if (bi.Rules.BattleType == BT_GRENADE && bi.FuseTimer == -2)
				{
					itemsToGoBoom.Add(bi);
					explosionParams.Add((bi.Tile, bi.Rules));
				}
			}
		}

		foreach (var item in itemsToGoBoom)
		{
			_save.RemoveItem(item);
		}

		foreach (var (tile, rule) in explosionParams)
		{
			Position p = tile.Position.ToVoxel() + new Position(8, 8, -tile.TerrainLevel);
			_save.TileEngine.Explode(
				new BattleActionAttack(),
				p,
				rule.Power,
				rule.DamageType,
				rule.GetExplosionRadius(new BattleActionAttack())
			);
		}

		Tile t = _save.TileEngine.CheckForTerrainExplosions();
		while (t != null)
		{
			ItemDamageType DT = t.ExplosiveType switch
			{
				0 => DT_HE,
				5 => DT_IN,
				6 => DT_STUN,
				_ => DT_SMOKE,
			};
			int power = t.Explosive;
			t.SetExplosive(0, 0, true);
			Position p = t.Position.ToVoxel() + new Position(8, 8, 0);
			_save.TileEngine.Explode(new BattleActionAttack(), p, power, _game.Mod.GetDamageType(DT), power / 10);
			t = _save.TileEngine.CheckForTerrainExplosions();
		}
	}

	/// <summary>Spawns civilians on a terror mission.</summary>
	/// <param name="markAsVIP">Whether to mark the civilians as VIPs.</param>
	/// <param name="nodeRank">The node rank to spawn on.</param>
	/// <param name="max">Maximum number of civilians to spawn.</param>
	/// <param name="roundUp">Whether to round up the minimum number.</param>
	/// <param name="civilianType">Specific civilian type (empty: random from the terrain).</param>
	private void DeployCivilians(bool markAsVIP, int nodeRank, int max, bool roundUp = false, string civilianType = "")
	{
		if (max != 0)
		{
			// inevitably someone will point out that ufopaedia says 0-16 civilians.
			// to that person: this is only partially true;
			// 0 civilians would only be a possibility if there were already 80 units,
			// or no spawn nodes for civilians, but it would always try to spawn at least 8.
			int number = Rng.Generate(roundUp ? (max + 1) / 2 : max / 2, max);

			if (number > 0)
			{
				for (int i = 0; i < number; ++i)
				{
					Unit rule;
					if (civilianType.Length > 0)
					{
						rule = _game.Mod.GetUnit(civilianType, true);
					}
					else
					{
						var civilianTypes = _terrain.CivilianTypes;
						int pick = Rng.Generate(0, civilianTypes.Count - 1);
						rule = _game.Mod.GetUnit(civilianTypes[pick], true);
					}
					BattleUnit civ = AddCivilian(rule, nodeRank);
					if (civ != null)
					{
						if (markAsVIP) civ.MarkAsVIP();
						int itemLevel = _game.Mod.AlienItemLevels[_save.AlienItemLevel][Rng.Generate(0, 9)];
						// Built in weapons: civilians may have levelled item lists with randomized distribution
						// following the same basic rules as the alien item levels.
						_save.InitUnit(civ, itemLevel);
					}
				}
			}
		}
	}

	/// <summary>Places a unit near a friendly unit.</summary>
	/// <param name="unit">Pointer to the unit in question.</param>
	/// <returns>If we successfully placed the unit.</returns>
	private bool PlaceUnitNearFriend(BattleUnit unit)
	{
		if (_save.Units.Count == 0)
		{
			return false;
		}
		for (int i = 0; i != 10; ++i)
		{
			Position entryPoint = TileEngine.invalid;
			int tries = 100;
			bool largeUnit = false;
			while (entryPoint == TileEngine.invalid && tries != 0)
			{
				BattleUnit k = _save.Units[Rng.Generate(0, _save.Units.Count - 1)];
				if (k.Faction == unit.Faction && k.Position != TileEngine.invalid && k.Armor.Size >= unit.Armor.Size)
				{
					entryPoint = k.Position;
					largeUnit = k.IsBigUnit;
				}
				--tries;
			}
			if (tries != 0 && _save.PlaceUnitNearPosition(unit, entryPoint, largeUnit))
			{
				return true;
			}
		}
		return false;
	}
}
