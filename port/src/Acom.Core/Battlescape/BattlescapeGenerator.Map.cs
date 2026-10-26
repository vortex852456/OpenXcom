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

using System.Globalization;
using static Acom.BattleType;
using static Acom.MapDirection;
using static Acom.MapScriptCommand;
using static Acom.TilePart;
using static Acom.VerticalLevelType;

namespace Acom;

/// <summary>One 4-byte tile record of a MAP file at its map position: the terrain object IDs of the four tile parts.</summary>
internal readonly record struct MapTileRecord(int X, int Y, int Z, byte Floor, byte WestWall, byte NorthWall, byte Object)
{
	/// <summary>Gets the terrain object ID of a tile part (<see cref="TilePart"/> order).</summary>
	public byte this[int part] => part switch
	{
		0 => Floor,
		1 => WestWall,
		2 => NorthWall,
		_ => Object,
	};
}

/// <summary>A node read from an RMP file, before it becomes a <see cref="Node"/>.</summary>
internal sealed class RmpNodeRecord
{
	/// <summary>Culled node (outside the map block); becomes a dummy node.</summary>
	public bool IsDummy;
	public Position Position;
	public int Type, Rank, Flags, Reserved, Priority;
	/// <summary>The 5 links: node IDs (offset applied) or special values (-1 unused, -2 north, -3 east, -4 south, -5 west).</summary>
	public readonly List<int> Links = [];
}

// Map generation: map scripts, map blocks, MAP/RMP files, base defense maps, vertical levels and nodes.
public partial class BattlescapeGenerator
{
	/// <summary>Size of the MAP file header (size y, x, z as signed chars).</summary>
	internal const int MAP_HEADER_SIZE = 3;
	/// <summary>Size of one MAP tile record (floor, west wall, north wall, object).</summary>
	internal const int MAP_RECORD_SIZE = 4;
	/// <summary>Size of one RMP node record.</summary>
	internal const int RMP_RECORD_SIZE = 24;

	/// <summary>
	/// Reads the header of an XCom MAP file: the block size as three signed chars in the order y, x, z.
	/// </summary>
	/// <remarks>C++ leaves missing header bytes of a truncated file uninitialized; here they read as 0.</remarks>
	internal static (int sizeX, int sizeY, int sizeZ) ReadMapHeader(byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		int b(int i) => i < data.Length ? (sbyte)data[i] : 0;
		return (b(1), b(0), b(2));
	}

	/// <summary>
	/// Reads the tile records of an XCom MAP file (after the header) and assigns their map positions:
	/// x runs fastest, then y; after each layer z goes one level down from <paramref name="zTop"/>.
	/// A trailing incomplete record is ignored (like the C++ stream loop).
	/// </summary>
	/// <seealso href="http://www.ufopaedia.org/index.php?title=MAPS"/>
	internal static List<MapTileRecord> ReadMapTiles(byte[] data, int xoff, int yoff, int zTop, int sizeX, int sizeY)
	{
		ArgumentNullException.ThrowIfNull(data);
		var tiles = new List<MapTileRecord>();
		int x = xoff, y = yoff, z = zTop;
		for (int off = MAP_HEADER_SIZE; off + MAP_RECORD_SIZE <= data.Length; off += MAP_RECORD_SIZE)
		{
			tiles.Add(new MapTileRecord(x, y, z, data[off], data[off + 1], data[off + 2], data[off + 3]));

			x++;

			if (x == (sizeX + xoff))
			{
				x = xoff;
				y++;
			}
			if (y == (sizeY + yoff))
			{
				y = yoff;
				z--;
			}
		}
		return tiles;
	}

	/// <summary>
	/// Loads an XCom format MAP file into the tiles of the battlegame.
	/// Note: the Y-axis is in reverse order.
	/// </summary>
	/// <param name="mapblock">Pointer to MapBlock.</param>
	/// <param name="xoff">Mapblock offset in X direction.</param>
	/// <param name="yoff">Mapblock offset in Y direction.</param>
	/// <param name="zoff">Mapblock offset in Z direction.</param>
	/// <param name="terrain">Pointer to the Terrain rule.</param>
	/// <param name="mapDataSetOffset">Offset of the terrain's map data sets in the save.</param>
	/// <param name="discovered">Whether or not this mapblock is discovered (eg. landing site of the XCom plane).</param>
	/// <param name="craft">Whether this is the XCom craft map.</param>
	/// <param name="ufoIndex">Index of the UFO whose map this is (-1: none).</param>
	/// <returns>Height of the loaded mapblock (this is needed for spawnpoint calculation...).</returns>
	private int LoadMAP(MapBlock mapblock, int xoff, int yoff, int zoff, RuleTerrain terrain, int mapDataSetOffset, bool discovered = false, bool craft = false, int ufoIndex = -1)
	{
		int x = xoff, y = yoff, z = zoff;
		string filename = "MAPS/" + mapblock.Name + ".MAP";

		// Load file
		byte[] mapFile = FileMap.ReadAll(filename);

		var (sizex, sizey, sizez) = ReadMapHeader(mapFile);

		mapblock.SizeZ = sizez;

		if (sizez > _save.MapSizeZ)
		{
			throw new InvalidOperationException($"Height of map {filename} too big for this mission, block is {sizez}, expected: {_save.MapSizeZ}");
		}

		if (sizex != mapblock.SizeX ||
			sizey != mapblock.SizeY)
		{
			throw new InvalidOperationException($"Map block is not of the size specified {filename} is {sizex}x{sizey} , expected: {mapblock.SizeX}x{mapblock.SizeY}");
		}

		z += sizez - 1;

		for (int i = _mapsize_z - 1; i > 0; i--)
		{
			// check if there is already a layer - if so, we have to move Z up
			MapData floor = _save.GetTile(new Position(x, y, i)).GetMapData(O_FLOOR);
			if (floor != null && zoff == 0)
			{
				z += i;
				if (craft && _craftZ == 0)
				{
					_craftZ = i;
				}
				if (ufoIndex >= 0 && _ufoZ[ufoIndex] == 0)
				{
					_ufoZ[ufoIndex] = i;
				}
				break;
			}
		}

		if (z > (_save.MapSizeZ - 1))
		{
			if (_save.MissionType == "STR_BASE_DEFENSE")
			{
				// we'll already have gone through _base->isOverlappingOrOverflowing() by the time we hit this, possibly multiple times
				// let's just throw an exception and tell them to check the log, it'll have all the detail they'll need.
				throw new InvalidOperationException("Something is wrong with your base, check your log file for additional information.");
			}
			throw new InvalidOperationException("Something is wrong in your map definitions, craft/ufo map is too tall?");
		}

		foreach (var record in ReadMapTiles(mapFile, xoff, yoff, z, sizex, sizey))
		{
			Tile tile = _save.GetTile(new Position(record.X, record.Y, record.Z));
			for (int part = (int)O_FLOOR; part < (int)O_MAX; ++part)
			{
				uint terrainObjectID = record[part];
				if (terrainObjectID > 0)
				{
					int mapDataSetID = mapDataSetOffset;
					uint mapDataID = terrainObjectID;
					MapData md = terrain.GetMapData(ref mapDataID, ref mapDataSetID);
					if (mapDataSetOffset > 0) // ie: ufo or craft.
					{
						tile.SetMapData(null, -1, -1, O_OBJECT);
					}
					TilePart tp = (TilePart)part;
					tile.SetMapData(md, (int)mapDataID, mapDataSetID, tp);
				}
			}

			tile.SetDiscovered(discovered || mapblock.IsFloorRevealed(record.Z), O_FLOOR);
		}
		// (C++ throws "Invalid MAP file" when the stream ends with an error other than EOF, which cannot happen here.)

		// Add the craft offset to the positions of the items if we're loading a craft map
		// But don't do so if loading a verticalLevel, since the z offset of the craft is handled by that code
		if (craft && zoff == 0)
		{
			zoff += _craftZ;
		}
		// Add the ufo offset to the positions of the items if we're loading a ufo map
		// But don't do so if loading a verticalLevel, since the z offset of the ufo is handled by that code
		if (ufoIndex >= 0 && zoff == 0)
		{
			zoff += _ufoZ[ufoIndex];
		}

		if (_generateFuel)
		{
			// if one of the mapBlocks has an items array defined, don't deploy fuel algorithmically
			_generateFuel = mapblock.Items.Count == 0;
		}
		Position offset = new(xoff, yoff, zoff);
		foreach (var mapblockItemInfo in mapblock.Items)
		{
			bool hasPrime = mapblock.ItemsFuseTimers.TryGetValue(mapblockItemInfo.Key, out var prime);
			RuleItem rule = _game.Mod.GetItem(mapblockItemInfo.Key, true);
			if (rule.BattleType == BT_CORPSE)
			{
				throw new InvalidOperationException("Placing corpse items (battleType: 11) on the map is not allowed. Item: " + rule.Type + ", map block: " + mapblock.Name);
			}
			foreach (var itemPos in mapblockItemInfo.Value)
			{
				if (itemPos.x >= mapblock.SizeX || itemPos.y >= mapblock.SizeY || itemPos.z >= mapblock.SizeZ)
				{
					throw new InvalidOperationException($"Item {rule.Type} is outside of map block {mapblock.Name}, position: [{itemPos.x},{itemPos.y},{itemPos.z}], block size: [{mapblock.SizeX},{mapblock.SizeY},{mapblock.SizeZ}]");
				}
				BattleItem item = _save.CreateItemForTile(rule, _save.GetTile(itemPos + offset));
				if (hasPrime)
				{
					item.FuseTimer = Rng.Generate(prime.first, prime.second);
				}
			}
		}
		// randomized items
		foreach (var rngItems in mapblock.RandomizedItems)
		{
			if (rngItems.itemList.Count < 1)
			{
				continue; // skip empty definition
			}
			else
			{
				// mixed = false
				int index = Rng.Generate(0, rngItems.itemList.Count - 1);
				RuleItem rule = _game.Mod.GetItem(rngItems.itemList[index], true);

				for (int j = 0; j < rngItems.amount; ++j)
				{
					if (rngItems.mixed)
					{
						// mixed = true
						index = Rng.Generate(0, rngItems.itemList.Count - 1);
						rule = _game.Mod.GetItem(rngItems.itemList[index], true);
					}

					if (rule.BattleType == BT_CORPSE)
					{
						throw new InvalidOperationException("Placing corpse items (battleType: 11) on the map is not allowed. Item: " + rule.Type + ", map block: " + mapblock.Name);
					}
					Position rp = rngItems.position;
					if (rp.x >= mapblock.SizeX || rp.y >= mapblock.SizeY || rp.z >= mapblock.SizeZ)
					{
						throw new InvalidOperationException($"Random item {rule.Type} is outside of map block {mapblock.Name}, position: [{rp.x},{rp.y},{rp.z}], block size: [{mapblock.SizeX},{mapblock.SizeY},{mapblock.SizeZ}]");
					}
					BattleItem newRandItem = _save.CreateItemForTile(rule, _save.GetTile(rp + offset));
					if (rule.FuseTimerType != BattleFuseType.BFT_NONE && rngItems.fuseTimerMin > -1 && rngItems.fuseTimerMax > -1 && rngItems.fuseTimerMin <= rngItems.fuseTimerMax)
					{
						newRandItem.FuseTimer = Rng.Generate(rngItems.fuseTimerMin, rngItems.fuseTimerMax);
					}
				}
			}
		}
		// extended items
		foreach (var extItemDef in mapblock.ExtendedItems)
		{
			RuleItem extRule = _game.Mod.GetItem(extItemDef.type, true);
			if (extRule.BattleType == BT_CORPSE)
			{
				throw new InvalidOperationException("Placing corpse items (battleType: 11) on the map is not allowed. Item: " + extRule.Type + ", map block: " + mapblock.Name);
			}
			foreach (var extPos in extItemDef.pos)
			{
				if (extPos.x >= mapblock.SizeX || extPos.y >= mapblock.SizeY || extPos.z >= mapblock.SizeZ)
				{
					throw new InvalidOperationException($"Extended item {extRule.Type} is outside of map block {mapblock.Name}, position: [{extPos.x},{extPos.y},{extPos.z}], block size: [{mapblock.SizeX},{mapblock.SizeY},{mapblock.SizeZ}]");
				}
				BattleItem newExtItem = _save.CreateItemForTile(extRule, _save.GetTile(extPos + offset));
				if (extItemDef.fuseTimerMin > -1 && extItemDef.fuseTimerMax > -1 && extItemDef.fuseTimerMin <= extItemDef.fuseTimerMax)
				{
					newExtItem.FuseTimer = Rng.Generate(extItemDef.fuseTimerMin, extItemDef.fuseTimerMax);
				}
				foreach (var extAmmoDef in extItemDef.ammoDef)
				{
					RuleItem extAmmoRule = _game.Mod.GetItem(extAmmoDef.first, true);
					if (extAmmoRule != null)
					{
						if (extAmmoRule.BattleType == BT_CORPSE)
						{
							throw new InvalidOperationException("Placing corpse items (battleType: 11) on the map is not allowed. Ammo item: " + extAmmoRule.Type + ", map block: " + mapblock.Name);
						}
						BattleItem newExtAmmoItem = _save.CreateItemForTile(extAmmoRule, _save.GetTile(extPos + offset));
						if (extAmmoDef.second > 0 && extAmmoDef.second < newExtAmmoItem.AmmoQuantity)
						{
							newExtAmmoItem.AmmoQuantity = extAmmoDef.second;
						}
						if (newExtItem.IsWeaponWithAmmo)
						{
							int slotAmmo = extRule.GetSlotForAmmo(extAmmoRule);
							if (slotAmmo == -1)
							{
								throw new InvalidOperationException("Ammo is not compatible. Weapon: " + extRule.Type + ", ammo: " + extAmmoRule.Type + ", map block: " + mapblock.Name);
							}
							else
							{
								if (newExtItem.GetAmmoForSlot(slotAmmo) != null)
								{
									throw new InvalidOperationException("Weapon is already loaded. Weapon: " + extRule.Type + ", ammo: " + extAmmoRule.Type + ", map block: " + mapblock.Name);
								}
								else
								{
									// Put ammo in weapon
									newExtItem.SetAmmoForSlot(slotAmmo, newExtAmmoItem);
								}
							}
						}
						else
						{
							// ammo is not needed, crash or ignore?
						}
					}
				}
			}
		}

		if (mapblock.CraftInventoryTile.Count >= 3)
		{
			var coords = mapblock.CraftInventoryTile;
			Position pilePos = new(coords[0] + xoff, coords[1] + yoff, coords[2] + zoff);
			Tile pileTile = _save.GetTile(pilePos);
			_backupInventoryTiles.Add(pileTile);
		}

		return sizez;
	}

	/// <summary>
	/// Reads the node records of an XCom RMP file. Nodes outside the map block are culled (become dummies, so the
	/// link numbers stay valid) and links to them are terminated.
	/// </summary>
	/// <param name="data">The file contents (24 bytes per node; a trailing incomplete record is ignored).</param>
	/// <param name="filename">File name for the log.</param>
	/// <param name="sizeX">Map block X size.</param>
	/// <param name="sizeY">Map block Y size.</param>
	/// <param name="sizeZ">Map block Z size (the file counts levels from the top).</param>
	/// <param name="xoff">Mapblock offset in X direction.</param>
	/// <param name="yoff">Mapblock offset in Y direction.</param>
	/// <param name="zoff">Mapblock vertical offset.</param>
	/// <param name="nodeOffset">Number of nodes already loaded (added to the link IDs).</param>
	/// <seealso href="http://www.ufopaedia.org/index.php?title=ROUTES"/>
	internal static List<RmpNodeRecord> ReadRmpNodes(byte[] data, string filename, int sizeX, int sizeY, int sizeZ, int xoff, int yoff, int zoff, int nodeOffset)
	{
		ArgumentNullException.ThrowIfNull(data);
		var nodes = new List<RmpNodeRecord>();
		var badNodes = new List<int>();
		int nodesAdded = 0;
		for (int off = 0; off + RMP_RECORD_SIZE <= data.Length; off += RMP_RECORD_SIZE)
		{
			int pos_x = data[off + 1];
			int pos_y = data[off];
			int pos_z = data[off + 2];
			var node = new RmpNodeRecord();
			if (pos_x >= 0 && pos_x < sizeX &&
				pos_y >= 0 && pos_y < sizeY &&
				pos_z >= 0 && pos_z < sizeZ)
			{
				node.Position = new Position(xoff + pos_x, yoff + pos_y, sizeZ - 1 - pos_z + zoff);
				node.Type     = data[off + 19];
				node.Rank     = data[off + 20];
				node.Flags    = data[off + 21];
				node.Reserved = data[off + 22];
				node.Priority = data[off + 23];
				for (int j = 0; j < 5; ++j)
				{
					int connectID = data[off + 4 + j * 3];
					// don't touch special values
					if (connectID <= 250)
					{
						connectID += nodeOffset;
					}
					// 255/-1 = unused, 254/-2 = north, 253/-3 = east, 252/-4 = south, 251/-5 = west
					else
					{
						connectID -= 256;
					}
					node.Links.Add(connectID);
				}
			}
			else
			{
				// since we use getNodes()->at(n) a lot, we have to push a dummy node into the vector to keep the connections sane,
				// or else convert the node vector to a map, either way is as much work, so i'm sticking with vector for faster operation.
				// this is because the "built in" nodeLinks reference each other by number, and it's gonna be implementational hell to try
				// to adjust those numbers retroactively, post-culling. far better to simply mark these culled nodes as dummies, and discount their use
				// that way, all the connections will still line up properly in the array.
				node.IsDummy = true;
				Logger.Info($"Bad node in RMP file: {filename} Node #{nodesAdded} is outside map boundaries at X:{pos_x} Y:{pos_y} Z:{pos_z}. Culling Node.");
				badNodes.Add(nodesAdded);
			}
			nodes.Add(node);
			nodesAdded++;
		}

		foreach (int badNodeId in badNodes)
		{
			int nodeCounter = nodesAdded;
			for (int n = nodes.Count - 1; n >= 0 && nodeCounter > 0; --n)
			{
				var node = nodes[n];
				if (!node.IsDummy)
				{
					for (int l = 0; l < node.Links.Count; ++l)
					{
						// C++ compares as size_t; negative (special) links never match
						if ((long)node.Links[l] - nodeOffset == badNodeId)
						{
							Logger.Info($"RMP file: {filename} Node #{nodeCounter - 1} is linked to Node #{badNodeId}, which was culled. Terminating Link.");
							node.Links[l] = -1;
						}
					}
				}
				nodeCounter--;
			}
		}

		// (C++ throws "Invalid RMP file" when the stream ends with an error other than EOF, which cannot happen here.)
		return nodes;
	}

	/// <summary>Loads an XCom format RMP file into the spawnpoints of the battlegame.</summary>
	/// <param name="mapblock">Pointer to MapBlock.</param>
	/// <param name="xoff">Mapblock offset in X direction.</param>
	/// <param name="yoff">Mapblock offset in Y direction.</param>
	/// <param name="zoff">Mapblock vertical offset.</param>
	/// <param name="segment">Mapblock segment.</param>
	private void LoadRMP(MapBlock mapblock, int xoff, int yoff, int zoff, int segment)
	{
		string filename = "ROUTES/" + mapblock.Name + ".RMP";
		// Load file
		byte[] mapFile = FileMap.ReadAll(filename);

		int nodeOffset = _save.Nodes.Count;
		foreach (var record in ReadRmpNodes(mapFile, filename, mapblock.SizeX, mapblock.SizeY, mapblock.SizeZ, xoff, yoff, zoff, nodeOffset))
		{
			Node node;
			if (!record.IsDummy)
			{
				node = new Node(_save.Nodes.Count, record.Position, segment, record.Type, record.Rank, record.Flags, record.Reserved, record.Priority);
				node.NodeLinks.AddRange(record.Links);
			}
			else
			{
				node = new Node();
				node.SetDummy(true);
			}
			_save.Nodes.Add(node);
		}
	}

	/// <summary>Checks if a new terrain is being requested by a command and loads it if necessary.</summary>
	/// <param name="terrain">Pointer to the terrain.</param>
	/// <returns>The MCD offset where the requested terrain begins.</returns>
	private int LoadExtraTerrain(RuleTerrain terrain)
	{
		int mapDataSetIDOffset;

		if (_loadedTerrains.TryGetValue(terrain, out int found))
		{
			// Found the terrain in the already-loaded list, get the offset
			mapDataSetIDOffset = found;
		}
		else
		{
			// Terrain not loaded yet, go ahead and load it
			mapDataSetIDOffset = _save.MapDataSets.Count; // new terrain's offset starts at the end of the already-loaded list

			foreach (var mds in terrain.MapDataSets)
			{
				mds.LoadData(_game.Mod.GetMCDPatch(mds.Name));
				_save.MapDataSets.Add(mds);
			}

			_loadedTerrains[terrain] = mapDataSetIDOffset;
		}

		return mapDataSetIDOffset;
	}

	/// <summary>Generates a map (set of tiles) for a new battlescape game.</summary>
	/// <param name="script">The script to use to build the map.</param>
	/// <param name="customUfoName">Custom UFO name to use for the dummy/blank 'addUFO' mapscript command.</param>
	/// <param name="startingCondition">The starting condition (may replace the craft).</param>
	private void GenerateMap(List<MapScript> script, string customUfoName, RuleStartingCondition startingCondition)
	{
		_backupInventoryTiles.Clear(); // just in case

		// reset ambient sound
		_save.AmbientSound = Mod.NO_SOUND;
		_save.AmbienceRandom = [];

		// set our ambient sound
		if (_terrain.Ambience != Mod.NO_SOUND)
		{
			_save.AmbientSound = _terrain.Ambience;
		}
		else
		{
			_save.AmbienceRandom = _terrain.AmbienceRandom;
			_save.MinAmbienceRandomDelay = _terrain.MinAmbienceRandomDelay;
			_save.MaxAmbienceRandomDelay = _terrain.MaxAmbienceRandomDelay;
			_save.ResetCurrentAmbienceDelay();
		}
		_save.AmbientVolume = _terrain.AmbientVolume;

		// set up our map generation vars
		_dummy = new MapBlock("dummy");

		Init(true);

		MapBlock craftMap = null;
		var ufoMaps = new List<MapBlock>();

		int mapDataSetIDOffset = 0;
		int craftDataSetIDOffset = 0;

		// create an array to track command success/failure
		var conditionals = new Dictionary<int, bool>();

		// Load in the default terrain data
		foreach (var mds in _terrain.MapDataSets)
		{
			mds.LoadData(_game.Mod.GetMCDPatch(mds.Name));
			_save.MapDataSets.Add(mds);
			mapDataSetIDOffset++;
		}

		_loadedTerrains[_terrain] = 0;

		RuleTerrain ufoTerrain = null;
		string consolidatedUfoType = "";
		// lets generate the map now and store it inside the tile objects

		// determine globe terrain from globe texture
		_globeTerrain = _terrain;
		if (_globeTexture != null && _craft != null)
		{
			// TODO (cosmetic): multiple attempts (e.g. several attacks on the same alien base) may generate different terrains from the same globe texture
			var tmpTerrain = _game.Mod.GetTerrain(_globeTexture.GetRandomTerrain(_craft), false);
			if (tmpTerrain != null)
			{
				_globeTerrain = tmpTerrain;
			}
		}

		// this mission type is "hard-coded" in terms of map layout
		ulong seed = Rng.Seed;
		_baseTerrain = _terrain;
		if (_save.MissionType == "STR_BASE_DEFENSE")
		{
			if (_mod.BaseDefenseMapFromLocation == 1)
			{
				Target target = _base;

				// Set RNG to make base generation dependent on base position
				double baseLon = target.Longitude;
				double baseLat = target.Latitude;
				if (baseLon == 0)
				{
					baseLon = 1.0;
				}
				if (baseLat == 0)
				{
					baseLat = 1.0;
				}
				// C++ converts double to uint64_t; for negative values x86-64 code goes through a signed conversion.
				ulong baseSeed = unchecked((ulong)(long)(baseLon * baseLat * 1e6));
				Rng.Seed = baseSeed;

				_baseTerrain = _game.Mod.GetTerrain(_globeTexture.GetRandomBaseTerrain(target), true);
				GenerateBaseMap();
			}
			else
			{
				GenerateBaseMap();
			}
		}

		//process script
		foreach (var command in script)
		{
			if (command.Label > 0 && conditionals.ContainsKey(command.Label))
			{
				throw new InvalidOperationException("Map generator encountered an error: multiple commands are sharing the same label.");
			}
			// C++ keeps a reference into the map (bool &success = conditionals[label]); written back below.
			conditionals[command.Label] = false;
			bool success = false;

			// if this command runs conditionally on the failures or successes of previous commands
			if (command.Conditionals.Count != 0)
			{
				bool execute = true;
				// compare the corresponding entries in the success/failure vector
				foreach (int condition in command.Conditionals)
				{
					// positive numbers indicate conditional on success, negative means conditional on failure
					// ie: [1, -2] means this command only runs if command 1 succeeded and command 2 failed.
					if (conditionals.ContainsKey(Math.Abs(condition)))
					{
						if ((condition > 0 && !conditionals[condition]) || (condition < 0 && conditionals[Math.Abs(condition)]))
						{
							execute = false;
							break;
						}
					}
					else
					{
						throw new InvalidOperationException("Map generator encountered an error: conditional command expected a label that did not exist before this command.");
					}
				}
				if (!execute)
				{
					continue;
				}
			}

			// if this command runs conditionally based on deployed craft's groups
			if (_craftDeployed && _craftRules != null)
			{
				if (command.CraftGroups.Count != 0)
				{
					bool execute = false;
					// compare the corresponding entries in the craft rules vector
					foreach (int grp in command.CraftGroups)
					{
						if (_craftRules.Groups.Contains(grp))
						{
							execute = true;
							break;
						}
					}
					if (!execute)
					{
						continue;
					}
				}
			}

			// if there's a chance a command won't execute by design, take that into account here.
			if (Rng.Percent(command.ChancesOfExecution))
			{
				// initialize the block selection arrays
				command.Init();

				// each command can be attempted multiple times, as randomization within the rects may occur
				for (int j = 0; j < command.Executions; ++j)
				{
					_ufoDeployed = false; // reset EACH time
					_alternateTerrain = null; // reset

					int x = 0, y = 0;
					MapBlock block;

					// Check if the command has vertical levels
					_verticalLevels.Clear();
					bool doLevels = PopulateVerticalLevels(command);
					int currentLevel = 0;
					// Special case among verticalLevels, we want to add the blocks using the "line" level, not the ground or the first level
					if (doLevels && command.Type == MSC_ADDLINE)
					{
						for (; currentLevel != _verticalLevels.Count; ++currentLevel)
						{
							if (_verticalLevels[currentLevel].levelType == VLT_LINE)
							{
								break;
							}
						}

						// How did you pass the populateVerticalLevels validation???
						if (currentLevel == _verticalLevels.Count)
						{
							throw new InvalidOperationException("Map generator encountered an error: An addLine command with verticalLevels doesn't contain a level for the line.");
						}
					}

					// Variable for holding positions of blocks added for use in loading vertical levels
					_placedBlockRects.Clear();

					RuleTerrain terrain = _terrain;
					if (command.RandomAlternateTerrain.Count != 0)
					{
						int pick = Rng.Generate(0, command.RandomAlternateTerrain.Count - 1);
						terrain = PickTerrain(command.RandomAlternateTerrain[pick]);
					}
					_alternateTerrain = terrain; // backup for later use

					if (doLevels)
					{
						// initNextLevel() << returns next block for use
						command.InitVerticalLevel(_verticalLevels[currentLevel]);
						terrain = _verticalLevels[currentLevel].levelTerrain == "" ? terrain : PickTerrain(_verticalLevels[currentLevel].levelTerrain);
					}

					_markAsReinforcementsBlock = command.MarkAsReinforcementsBlock() ? 1 : 0;

					switch (command.Type)
					{
					case MSC_ADDBLOCK:
						block = command.GetNextBlock(terrain);
						// select an X and Y position from within the rects, using an even distribution
						if (block != null && SelectPosition(command.Rects, ref x, ref y, block.SizeX, block.SizeY))
						{
							bool blockAdded = AddBlock(x, y, block, terrain);
							success = blockAdded || success;

							if (blockAdded && doLevels)
							{
								_placedBlockRects.Add(new SDL_Rect(x, y, block.SizeX, block.SizeY));

								LoadVerticalLevels(command);
							}
						}

						break;
					case MSC_ADDLINE:
						success = AddLine(command.Direction, command.Rects, terrain, command.VerticalGroup, command.HorizontalGroup, command.CrossingGroup);

						if (doLevels && success)
						{
							LoadVerticalLevels(command, true);
						}

						break;
					case MSC_ADDCRAFT:
						if (_craft != null)
						{
							RuleCraft craftRulesOverride = _save.Mod.GetCraft(command.CraftName);
							if (startingCondition != null)
							{
								craftRulesOverride = startingCondition.GetCraftReplacement(_craftRules, craftRulesOverride);
							}
							if (craftRulesOverride != null)
							{
								_craftRules = craftRulesOverride;
							}

							craftMap = _craftRules.BattlescapeTerrainData.GetRandomMapBlock(999, 999, 0, false);
							bool craftPlaced = AddCraft(craftMap, command, ref _craftPos, terrain);
							// Note: if label != 0, we assume that this may actually fail and will be handled by subsequent command(s)
							if (!craftPlaced && command.Label == 0)
							{
								throw new InvalidOperationException("Map generator encountered an error: Craft (MapBlock: "
									+ craftMap.Name
									+ ") could not be placed on the map. You may try turning on 'save scumming' to work around this issue.");
							}
							if (craftPlaced)
							{
								// by default addCraft adds blocks from group 1.
								// this can be overwritten in the command by defining specific groups or blocks
								// or this behaviour can be suppressed by leaving group 1 empty
								// this is intentional to allow for TFTD's cruise liners/etc
								// in this situation, you can end up with ANYTHING under your craft, so be careful
								LoadBlocksUnderCraft(_craftPos, terrain, doLevels);

								if (doLevels)
								{
									LoadVerticalLevels(command, true, craftMap);
								}

								_craftDeployed = true;
								success = true;
							}
						}
						break;
					case MSC_ADDUFO:
						// as above, note that the craft and the ufo will never be allowed to overlap.
						// significant difference here is that we accept a UFOName string here to choose the UFO map
						// and we store the UFO positions in a vector, which we iterate later when actually loading the
						// map and route data. this makes it possible to add multiple UFOs to a single map
						// IMPORTANTLY: all the UFOs must use _exactly_ the same MCD set.
						// this is fine for most UFOs but it does mean small scouts can't be combined with larger ones
						// unless some major alterations are done to the MCD sets and maps themselves beforehand
						// this is because serializing all the MCDs is an implementational nightmare from my perspective,
						// and modders can take care of all that manually on their end.
						if (_game.Mod.GetUfo(command.UFOName) != null)
						{
							ufoTerrain = _game.Mod.GetUfo(command.UFOName).BattlescapeTerrainData;
							consolidatedUfoType = command.UFOName;
						}
						else if (_ufo != null)
						{
							ufoTerrain = _ufo.Rules.BattlescapeTerrainData;
							consolidatedUfoType = _ufo.Rules.Type;
						}
						else if (!string.IsNullOrEmpty(customUfoName))
						{
							var customUfoRule = _game.Mod.GetUfo(customUfoName, true); // crash if it doesn't exist, let the modder know what's going on
							ufoTerrain = customUfoRule.BattlescapeTerrainData;
							consolidatedUfoType = customUfoName;
						}

						if (ufoTerrain != null)
						{
							MapBlock ufoMap = ufoTerrain.GetRandomMapBlock(999, 999, 0, false);
							SDL_Rect ufoPosTemp = default;
							bool ufoPlaced = AddCraft(ufoMap, command, ref ufoPosTemp, terrain);
							// Note: if label != 0, we assume that this may actually fail and will be handled by subsequent command(s)
							if (!ufoPlaced && command.Label == 0)
							{
								if (command.CanBeSkipped())
								{
									Logger.Info("Map generator encountered a recoverable problem: UFO (MapBlock: " + ufoMap.Name + ") could not be placed on the map. Command skipped.");
									break;
								}
								else
								{
									throw new InvalidOperationException("Map generator encountered an error: UFO (MapBlock: "
										+ ufoMap.Name
										+ ") could not be placed on the map. You may try turning on 'save scumming' to work around this issue.");
								}
							}
							if (ufoPlaced)
							{
								_ufoPos.Add(ufoPosTemp);
								ufoMaps.Add(ufoMap);
								LoadBlocksUnderCraft(ufoPosTemp, terrain, doLevels);

								_ufoZ.Add(0);

								if (doLevels)
								{
									LoadVerticalLevels(command, true, ufoMap);
								}

								_ufoDeployed = true;
								success = true;
							}
						}
						break;
					case MSC_DIGTUNNEL:
						DrillModules(command.TunnelData, command.Rects, command.Direction, terrain);
						success = true; // this command is fail-proof
						break;
					case MSC_FILLAREA:
						block = command.GetNextBlock(terrain);
						while (block != null)
						{
							if (SelectPosition(command.Rects, ref x, ref y, block.SizeX, block.SizeY))
							{
								// fill area will succeed if even one block is added
								success = AddBlock(x, y, block, terrain) || success;

								// Add to a list of rects for fill area to be able to do verticalLevels
								_placedBlockRects.Add(new SDL_Rect(x, y, block.SizeX, block.SizeY));
							}
							else
							{
								break;
							}
							block = command.GetNextBlock(terrain);
						}

						// Now load all the vertical levels
						if (doLevels && _placedBlockRects.Count != 0)
						{
							LoadVerticalLevels(command, true);
						}

						break;
					case MSC_CHECKBLOCK:
						foreach (var rect in command.Rects)
						{
							if (success) break; // loop finished
							for (x = rect.x; x != rect.x + rect.w && x != _mapsize_x / 10 && !success; ++x)
							{
								for (y = rect.y; y != rect.y + rect.h && y != _mapsize_y / 10 && !success; ++y)
								{
									if (command.Groups.Count != 0)
									{
										foreach (int grp in command.Groups)
										{
											if (success) break; // loop finished
											success = _blocks[x][y] != null && _blocks[x][y].IsInGroup(grp);
										}
									}
									else if (command.Blocks.Count != 0)
									{
										foreach (int blck in command.Blocks)
										{
											if (success) break; // loop finished
											if ((uint)blck < (uint)terrain.MapBlocks.Count)
											{
												success = _blocks[x][y] == terrain.MapBlocks[blck];
											}
										}
									}
									else
									{
										// wildcard, we don't care what block it is, we just wanna know if there's a block here
										success = _blocks[x][y] != null;
									}
								}
							}
						}
						break;
					case MSC_REMOVE:
						success = RemoveBlocks(command);
						break;
					case MSC_RESIZE:
						if (_save.MissionType == "STR_BASE_DEFENSE")
						{
							throw new InvalidOperationException("Map Generator encountered an error: Base defense map cannot be resized.");
						}
						if (_blocksToDo < (_mapsize_x / 10) * (_mapsize_y / 10))
						{
							throw new InvalidOperationException("Map Generator encountered an error: One does not simply resize the map after adding blocks.");
						}

						if (command.SizeX > 0 && command.SizeX != _mapsize_x / 10)
						{
							_mapsize_x = command.SizeX * 10;
						}
						if (command.SizeY > 0 && command.SizeY != _mapsize_y / 10)
						{
							_mapsize_y = command.SizeY * 10;
						}
						if (command.SizeZ > 0 && command.SizeZ != _mapsize_z)
						{
							_mapsize_z = command.SizeZ;
						}
						Init(false);
						break;
					default:
						break;
					}
				}
			}
			conditionals[command.Label] = success;
		}

		if (_blocksToDo != 0)
		{
			throw new InvalidOperationException("Map failed to fully generate.");
		}

		// Put the map data set ID offset to the end of the terrains in the save since we may have loaded more than the default
		mapDataSetIDOffset = _save.MapDataSets.Count;

		LoadNodes();

		if (ufoMaps.Count != 0 && ufoTerrain != null)
		{
			foreach (var mds in ufoTerrain.MapDataSets)
			{
				mds.LoadData(_game.Mod.GetMCDPatch(mds.Name));
				_save.MapDataSets.Add(mds);
				craftDataSetIDOffset++;
			}

			for (int i = 0; i < ufoMaps.Count; ++i)
			{
				LoadMAP(ufoMaps[i], _ufoPos[i].x * 10, _ufoPos[i].y * 10, _ufoZ[i], ufoTerrain, mapDataSetIDOffset, false, false, i);
				LoadRMP(ufoMaps[i], _ufoPos[i].x * 10, _ufoPos[i].y * 10, _ufoZ[i], Node.UFOSEGMENT);
				for (int j = 0; j < ufoMaps[i].SizeX / 10; ++j)
				{
					for (int k = 0; k < ufoMaps[i].SizeY / 10; k++)
					{
						_save.FlattenedMapTerrainNames[_ufoPos[i].x + j][_ufoPos[i].y + k] = consolidatedUfoType; // UFO name, not terrain name
						_save.FlattenedMapBlockNames[_ufoPos[i].x + j][_ufoPos[i].y + k] = ufoMaps[i].Name;
						_segments[_ufoPos[i].x + j][_ufoPos[i].y + k] = Node.UFOSEGMENT;
					}
				}
			}
		}

		if (craftMap != null)
		{
			_craftRules.BattlescapeTerrainData.RefreshMapDataSets(_craft.SkinIndex, _game.Mod); // change skin if needed
			foreach (var mds in _craftRules.BattlescapeTerrainData.MapDataSets)
			{
				mds.LoadData(_game.Mod.GetMCDPatch(mds.Name));
				_save.MapDataSets.Add(mds);
			}
			LoadMAP(craftMap, _craftPos.x * 10, _craftPos.y * 10, _craftZ, _craftRules.BattlescapeTerrainData, mapDataSetIDOffset + craftDataSetIDOffset, _craftRules.IsMapVisible, true);
			LoadRMP(craftMap, _craftPos.x * 10, _craftPos.y * 10, _craftZ, Node.CRAFTSEGMENT);
			for (int i = 0; i < craftMap.SizeX / 10; ++i)
			{
				for (int j = 0; j < craftMap.SizeY / 10; j++)
				{
					_save.FlattenedMapTerrainNames[_craftPos.x + i][_craftPos.y + j] = _craftRules.Type; // craft name, not terrain name
					_save.FlattenedMapBlockNames[_craftPos.x + i][_craftPos.y + j] = craftMap.Name;
					_segments[_craftPos.x + i][_craftPos.y + j] = Node.CRAFTSEGMENT;
				}
			}
			for (int i = (_craftPos.x * 10) - 1; i <= (_craftPos.x * 10) + craftMap.SizeX; ++i)
			{
				for (int j = (_craftPos.y * 10) - 1; j <= (_craftPos.y * 10) + craftMap.SizeY; j++)
				{
					for (int k = _mapsize_z - 1; k >= _craftZ; --k)
					{
						_save.GetTile(new Position(i, j, k))?.SetDiscovered(_craftRules.IsMapVisible, O_FLOOR);
					}
				}
			}
		}

		// C++ deletes _dummy here.

		// special hacks to fill in empty floors on level 0
		for (int x = 0; x < _mapsize_x; ++x)
		{
			for (int y = 0; y < _mapsize_y; ++y)
			{
				if (_save.GetTile(new Position(x, y, 0)).GetMapData(O_FLOOR) == null)
				{
					_save.GetTile(new Position(x, y, 0)).SetMapData(MapDataSet.ScorchedEarthTile, 1, 0, O_FLOOR);
				}
			}
		}

		AttachNodeLinks();

		if (_save.MissionType == "STR_BASE_DEFENSE" && _mod.BaseDefenseMapFromLocation == 1)
		{
			Rng.Seed = seed;
		}
	}

	/// <summary>
	/// Loads the blocks that were placed under a craft or UFO (the blocks marked by <see cref="AddCraft"/>),
	/// remembering their rects for the vertical levels (shared part of the addCraft and addUFO commands).
	/// </summary>
	private void LoadBlocksUnderCraft(SDL_Rect craftPos, RuleTerrain terrain, bool doLevels)
	{
		for (int x = craftPos.x; x < craftPos.x + craftPos.w; ++x)
		{
			for (int y = craftPos.y; y < craftPos.y + craftPos.h; ++y)
			{
				if (_blocks[x][y] != null)
				{
					bool visible = _save.MissionType == "STR_BASE_DEFENSE";

					int terrainMapDataSetIDOffset = LoadExtraTerrain(terrain);
					LoadMAP(_blocks[x][y], x * 10, y * 10, 0, terrain, terrainMapDataSetIDOffset, visible);

					if (doLevels)
					{
						_placedBlockRects.Add(new SDL_Rect(x, y, _blocks[x][y].SizeX, _blocks[x][y].SizeY));
					}
				}
			}
		}
	}

	/// <summary>Parses a number like C <c>atoi</c> (leading whitespace, optional sign, digits; 0 if none).</summary>
	private static int Atoi(string s)
	{
		int i = 0;
		while (i < s.Length && char.IsWhiteSpace(s[i])) ++i;
		int start = i;
		if (i < s.Length && (s[i] == '+' || s[i] == '-')) ++i;
		while (i < s.Length && char.IsAsciiDigit(s[i])) ++i;
		return int.TryParse(s.AsSpan(start, i - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value) ? value : 0;
	}

	/// <summary>
	/// Generates a map based on the base's layout.
	/// This doesn't drill or fill with dirt, the script must do that.
	/// </summary>
	private void GenerateBaseMap()
	{
		// add modules based on the base's layout
		foreach (var fac in _base.Facilities)
		{
			if (fac.IsBuiltOrHadPreviousFacility)
			{
				int num = 0;
				int xLimit = fac.X + fac.Rules.SizeX - 1;
				int yLimit = fac.Y + fac.Rules.SizeY - 1;

				// Do we use the normal method for placing items on the ground or an explicit definition?
				bool storageCheckerboard = fac.Rules.StorageTiles.Count == 0;

				// If the facility has a verticalLevels ruleset definition, load it as if it's an addBlock command
				if (fac.Rules.VerticalLevels.Count != 0)
				{
					// Create a mapscript command to handle picking blocks for the vertical level
					var command = new MapScript();

					// Get the vertical levels from the facility ruleset and create a list according to map size
					_verticalLevels.Clear();
					_verticalLevels = fac.Rules.VerticalLevels.ConvertAll(v => new VerticalLevel(v));
					command.SetVerticalLevels(_verticalLevels, fac.Rules.SizeX, fac.Rules.SizeY);
					PopulateVerticalLevels(command);

					VerticalLevel currentLevel = _verticalLevels[0];
					command.InitVerticalLevel(currentLevel);
					RuleTerrain terrain = currentLevel.levelTerrain == "" ? _terrain : PickTerrain(currentLevel.levelTerrain);
					_placedBlockRects.Clear();

					MapBlock block = command.GetNextBlock(terrain);
					if (block != null)
					{
						int x = fac.X;
						int y = fac.Y;
						AddBlock(x, y, block, terrain);

						_placedBlockRects.Add(new SDL_Rect(x, y, block.SizeX, block.SizeY));

						_alternateTerrain = null; // not used in this case
						LoadVerticalLevels(command);
					}
					else
					{
						throw new InvalidOperationException("Map generator encountered an error: facility " + fac.Rules.Type + " has no block on first verticalLevel.");
					}
				}

				for (int y = fac.Y; y <= yLimit; ++y)
				{
					for (int x = fac.X; x <= xLimit; ++x)
					{
						// lots of crazy stuff here, which is for the hangars (or other large base facilities one may create)
						// TODO: clean this mess up, make the mapNames a vector in the base module defs
						// also figure out how to do the terrain sets on a per-block basis.

						// Only use the mapName if we didn't load the map by verticalLevels
						if (fac.Rules.VerticalLevels.Count == 0)
						{
							string mapname = fac.Rules.MapName;
							string newname = mapname[..^2]; // strip of last 2 digits
							int mapnum = Atoi(mapname.Substring(mapname.Length - 2, 2)); // get number
							mapnum += num;
							if (mapnum < 10) newname += "0";
							newname += mapnum.ToString(CultureInfo.InvariantCulture);
							var block = _terrain.GetMapBlock(newname);
							if (block == null)
							{
								throw new InvalidOperationException("Map generator encountered an error: map block "
									+ newname
									+ " is not defined in terrain "
									+ _terrain.Name
									+ ".");
							}
							AddBlock(x, y, block, _terrain);
						}
						_drillMap[x][y] = MD_NONE;
						num++;
						if (fac.Rules.Storage > 0 && storageCheckerboard)
						{
							int groundLevel;
							for (groundLevel = _mapsize_z - 1; groundLevel >= 0; --groundLevel)
							{
								if (!_save.GetTile(new Position(x * 10, y * 10, groundLevel)).HasNoFloor(null))
									break;
							}
							// general stores - there is where the items are put
							for (int k = x * 10; k != (x + 1) * 10; ++k)
							{
								for (int l = y * 10; l != (y + 1) * 10; ++l)
								{
									// we only want every other tile, giving us a "checkerboard" pattern
									if ((k + l) % 2 == 0)
									{
										Tile t = _save.GetTile(new Position(k, l, groundLevel));
										Tile tEast = _save.GetTile(new Position(k + 1, l, groundLevel));
										Tile tSouth = _save.GetTile(new Position(k, l + 1, groundLevel));
										if (t != null && t.GetMapData(O_FLOOR) != null && t.GetMapData(O_OBJECT) == null &&
											tEast != null && tEast.GetMapData(O_WESTWALL) == null &&
											tSouth != null && tSouth.GetMapData(O_NORTHWALL) == null)
										{
											_save.StorageSpace.Add(new Position(k, l, groundLevel));
										}
									}
								}
							}
							// let's put the inventory tile on the lower floor, just to be safe.
							_craftInventoryTile ??= _save.GetTile(new Position((x * 10) + 5, (y * 10) + 5, groundLevel - 1));
						}
					}
				}

				// Extended handling for placing storage tiles by ruleset definition
				if (fac.Rules.Storage > 0 && !storageCheckerboard)
				{
					int x = fac.X;
					int y = fac.Y;
					bool validPos = false;
					foreach (var pos in fac.Rules.StorageTiles)
					{
						if (pos == TileEngine.invalid)
						{
							validPos = true;
							break;
						}

						if (pos.x < 0 || pos.x / 10 > fac.Rules.SizeX
							|| pos.y < 0 || pos.y / 10 > fac.Rules.SizeY
							|| pos.z < 0 || pos.z > _mapsize_z)
						{
							Logger.Error($"Tile position {pos} is outside the facility {fac.Rules.Type}, skipping placing items there.");
							continue;
						}

						Position tilePos = new((x * 10) + pos.x, (y * 10) + pos.y, pos.z);
						if (_save.GetTile(tilePos) == null)
						{
							Logger.Error($"Tile position {tilePos}, from the facility {fac.Rules.Type}, is outside the map; skipping placing items there.");
							continue;
						}

						validPos = true;
						_save.StorageSpace.Add(tilePos);

						// just to be safe, make sure we have a craft inventory tile
						_craftInventoryTile ??= _save.GetTile(tilePos);
					}

					// Crash gracefully with some information before we spawn a map where no items could be placed.
					if (!validPos)
					{
						throw new InvalidOperationException("Could not place items on given tiles in storage facility " + fac.Rules.Type);
					}
				}

				for (int x = fac.X; x <= xLimit; ++x)
				{
					_drillMap[x][yLimit] = MD_VERTICAL;
				}
				for (int y = fac.Y; y <= yLimit; ++y)
				{
					_drillMap[xLimit][y] = MD_HORIZONTAL;
				}
				_drillMap[xLimit][yLimit] = MD_BOTH;
			}
		}
		_save.CalculateModuleMap();
	}

	/// <summary>Populates the verticalLevels vector.</summary>
	/// <param name="command">The mapscript command from which to get the size.</param>
	/// <returns>Whether or not the command has levels.</returns>
	private bool PopulateVerticalLevels(MapScript command) => PopulateVerticalLevels(command, _verticalLevels);

	/// <summary>
	/// Fills <paramref name="levels"/> with copies of the command's vertical levels: the ground level first, then the
	/// "filler" levels (craft/line only for the matching command types), then the ceiling.
	/// </summary>
	/// <returns>Whether or not the command has levels.</returns>
	internal static bool PopulateVerticalLevels(MapScript command, List<VerticalLevel> levels)
	{
		ArgumentNullException.ThrowIfNull(command);
		ArgumentNullException.ThrowIfNull(levels);
		levels.Clear();

		if (command.VerticalLevels.Count == 0)
		{
			return false;
		}

		bool craftOrUfo = command.Type == MSC_ADDCRAFT || command.Type == MSC_ADDUFO;

		// First check for ground level
		bool levelFound = false;
		foreach (var verticalLevel in command.VerticalLevels)
		{
			if (verticalLevel.levelType == VLT_GROUND)
			{
				if (!levelFound)
				{
					levels.Add(new VerticalLevel(verticalLevel));
					// For consistency with the standard addCraft/addUFO commands, set default groups to 1 (LZ) if not defined
					if (craftOrUfo && verticalLevel.levelGroups.Count == 0)
					{
						levels[^1].levelGroups.Add(1);
					}
					levelFound = true;
				}
				else
				{
					Logger.Warning("Map generator encountered an error: a 'ground' verticalLevel can only be used once per command.");
				}
			}
		}

		// Next, find the "filler" levels
		levelFound = false;
		foreach (var verticalLevel in command.VerticalLevels)
		{
			switch (verticalLevel.levelType)
			{
				case VLT_CRAFT:
					if (craftOrUfo && !levelFound)
					{
						levels.Add(new VerticalLevel(verticalLevel));
						levels[^1].maxRepeats = 1;
						levelFound = true;
					}
					else if (craftOrUfo)
					{
						Logger.Warning("Map generator encountered an error: a 'craft' verticalLevel can only be used once per command.");
					}
					else
					{
						Logger.Warning("Map generator encountered an error: a 'craft' verticalLevel is only valid for addCraft and addUFO commands, skipping level.");
					}

					break;

				case VLT_LINE:
					if (command.Type == MSC_ADDLINE && !levelFound)
					{
						levels.Add(new VerticalLevel(verticalLevel));
						levelFound = true;
					}
					else if (command.Type == MSC_ADDLINE)
					{
						Logger.Warning("Map generator encountered an error: a 'line' verticalLevel can only be used once per command.");
					}
					else
					{
						Logger.Warning("Map generator encountered an error: a 'line' verticalLevel is only valid for addLine commands, skipping level.");
					}

					break;

				case VLT_MIDDLE:
				case VLT_EMPTY:
				case VLT_DECORATION:
					levels.Add(new VerticalLevel(verticalLevel));

					break;

				default:

					break;
			}
		}

		// Finally, add the "ceiling" level
		levelFound = false;
		foreach (var verticalLevel in command.VerticalLevels)
		{
			if (verticalLevel.levelType == VLT_CEILING)
			{
				if (!levelFound)
				{
					levels.Add(new VerticalLevel(verticalLevel));
					levelFound = true;
				}
				else
				{
					Logger.Warning("Map generator encountered an error: a 'ceiling' verticalLevel can only be used once per command.");
				}
			}
		}

		return true;
	}

	/// <summary>Gets a terrain from a terrain name for a command or vertical level, validates if there is no terrain name.</summary>
	/// <param name="terrainName">The name of the terrain to look for.</param>
	/// <returns>Pointer to the terrain.</returns>
	private RuleTerrain PickTerrain(string terrainName)
	{
		RuleTerrain terrain;

		if (terrainName == "baseTerrain")
		{
			terrain = _baseTerrain;
		}
		else if (terrainName == "globeTerrain")
		{
			terrain = _globeTerrain;
		}
		else if (terrainName != "")
		{
			//get the terrain according to the string name
			terrain = _game.Mod.GetTerrain(terrainName);
			if (terrain == null)
			{
				// make sure we get a terrain, and put an error in the log, continuing with generation
				Logger.Error("Map generator could not find alternate terrain " + terrainName + ", proceeding with terrain from alienDeployments or Geoscape texture.");
				terrain = _terrain;
			}
		}
		else
		{
			// no alternate terrain in command or verticalLevel, default to deployment terrain
			terrain = _terrain;
		}

		return terrain;
	}

	/// <summary>Loads maps from verticalLevels within a mapscript command.</summary>
	/// <param name="command">The mapScript command.</param>
	/// <param name="repopulateLevels">Whether the levels have to be repopulated for each placed block.</param>
	/// <param name="craftMap">The craft or UFO map (for the craft level).</param>
	private void LoadVerticalLevels(MapScript command, bool repopulateLevels = false, MapBlock craftMap = null)
	{
		foreach (var rect in _placedBlockRects)
		{
			// If we're using a command that adds multiple blocks per single execution, we need to make sure the _verticalLevels vector gets repopulated after each iteration.
			if (repopulateLevels)
			{
				PopulateVerticalLevels(command);
			}

			int x = rect.x;
			int y = rect.y;
			MapBlock block = _blocks[x][y];

			// C++ iterator into _verticalLevels
			int currentLevel = 0;
			RuleTerrain terrain = _terrain;
			if (_alternateTerrain != null)
			{
				terrain = _alternateTerrain; // alternate terrain defined on the map script command
			}

			int zOffset = 0;
			int zLevelsLeft = command.SizeZ;
			if (zLevelsLeft < 1)
			{
				zLevelsLeft = _mapsize_z;
			}
			else if (zLevelsLeft > _mapsize_z)
			{
				Logger.Warning($"Battlescape Generator has encountered an error: a mapscript command has height {command.SizeZ} while the map is only {_mapsize_z} tiles tall.");
				Logger.Warning("Reducing command size to map height");

				zLevelsLeft = _mapsize_z;
			}

			// Special handling for addline command: we need to check if the line is on ground level. If not, we need to re-load the z = 0 level and load the line later
			MapBlock lineBlock = null;
			if (command.Type == MSC_ADDLINE && _verticalLevels[currentLevel].levelType != VLT_LINE)
			{
				lineBlock = _blocks[x][y];

				ClearModule(x * 10, y * 10, 10, 10);
				++_blocksToDo; // this will be re-added with addBlock
				command.InitVerticalLevel(_verticalLevels[currentLevel]);
				RuleTerrain levelTerrain = PickTerrain(_verticalLevels[currentLevel].levelTerrain);
				block = command.GetNextBlock(levelTerrain);

				if (block != null)
				{
					AddBlock(x, y, block, levelTerrain);
				}
				else
				{
					Logger.Warning("Battlescape Generator has encountered an error: an addLine mapscript command has vertical levels but a mapblock for the ground level could not be loaded.");
				}
			}

			// For the first level, "ground" or "line" should only done once, so remove it. Otherwise, just advance the iterator
			int z = _verticalLevels[currentLevel].levelSizeZ == -1 ? block.SizeZ : _verticalLevels[currentLevel].levelSizeZ;
			zOffset += z;
			zLevelsLeft -= z;
			if (_verticalLevels[currentLevel].levelType == VLT_GROUND || _verticalLevels[currentLevel].levelType == VLT_LINE)
			{
				_verticalLevels.RemoveAt(currentLevel);
			}
			else
			{
				++currentLevel;
			}

			// Reserve the space for the ceiling level
			MapBlock ceilingBlock = null;
			RuleTerrain ceilingTerrain = terrain;
			if (_verticalLevels[^1].levelType == VLT_CEILING)
			{
				command.InitVerticalLevel(_verticalLevels[^1]);
				ceilingTerrain = _verticalLevels[^1].levelTerrain == "" ? terrain : PickTerrain(_verticalLevels[^1].levelTerrain);
				ceilingBlock = command.GetNextBlock(ceilingTerrain);
				if (ceilingBlock != null)
				{
					z = _verticalLevels[^1].levelSizeZ == -1 ? ceilingBlock.SizeZ : _verticalLevels[^1].levelSizeZ;
					if (z > zLevelsLeft)
					{
						ceilingBlock = null;
					}
					else
					{
						zLevelsLeft -= z;
						_verticalLevels.RemoveAt(_verticalLevels.Count - 1);
					}
				}
			}

			// Now load the rest of the levels
			int tries = 0;
			int maxTries = 20;

			// If a addLine command for some reason can't fit in the line itself, note this in the log for the modder to fix
			bool lineAdded = false;

			// Loop over the "filling" levels to build up the maps placed at this location
			while (_verticalLevels.Count > 0 && zLevelsLeft > 0 && tries <= maxTries)
			{
				// Since our goal is filling the space between the bottom and the max height, we repeat the list of levels until we're done
				if (currentLevel >= _verticalLevels.Count)
					currentLevel = 0;

				VerticalLevel level = _verticalLevels[currentLevel];

				// Determine what we're doing with the current level
				RuleTerrain levelTerrain = terrain;
				block = null;
				z = 0;

				switch (level.levelType)
				{
					case VLT_CRAFT:
						// Set the craft height offset if necessary
						if (command.Type == MSC_ADDCRAFT && zOffset > _craftZ)
						{
							_craftZ = zOffset;
						}
						else if (command.Type == MSC_ADDUFO && zOffset > _ufoZ[^1])
						{
							_ufoZ[^1] = zOffset;
						}

						z = level.levelSizeZ == -1 ? craftMap.SizeZ : level.levelSizeZ;

						if (z > zLevelsLeft)
						{
							throw new InvalidOperationException("Map Generator has encountered an error: craft or ufo map"
								+ craftMap.Name
								+ "could not be placed because either the map is too tall or the verticalLevels place it too high.");
						}

						break;

					case VLT_EMPTY:
						// We're just bumping the zOffset upwards
						z = level.levelSizeZ == -1 ? 1 : level.levelSizeZ;

						break;

					default:
						// All other level types: we're loading a map block
						if (level.levelTerrain != "")
						{
							levelTerrain = PickTerrain(level.levelTerrain);
						}

						if (command.Type == MSC_ADDLINE && level.levelType == VLT_LINE && lineBlock != null)
						{
							// We're re-loading a block for the addLine command
							block = lineBlock;
							lineAdded = true;
						}
						else
						{
							// We're loading a new block
							command.InitVerticalLevel(level);
							block = command.GetNextBlock(levelTerrain);
						}

						if (block != null)
						{
							z = level.levelSizeZ == -1 ? block.SizeZ : level.levelSizeZ;
						}
						else
						{
							++tries; // we failed to find an appropriate block for a command that must explicitly load one, so don't try too hard to keep finding one
							if (tries > maxTries)
							{
								Logger.Warning("Battlescape Generator has encountered an error: a mapscript command or base facility with vertical levels cannot find a map block with the right size/terrain/groups. The modder may want to check that the specified terrain or vertical level size/groups is correct.");
							}
						}

						break;
				}

				// Apply the current level if it fits before we reach the max height, otherwise try again with the current level
				if (z <= zLevelsLeft)
				{
					// Load a map if that's what we're doing
					if (block != null)
					{
						bool visible = _save.MissionType == "STR_BASE_DEFENSE";

						int terrainMapDataSetIDOffset = LoadExtraTerrain(levelTerrain);
						LoadMAP(block, x * 10, y * 10, zOffset, levelTerrain, terrainMapDataSetIDOffset, visible);
						_verticalLevelSegments.Add((block, new Position(x, y, zOffset)));
					}

					zLevelsLeft -= z;
					zOffset += z;

					// Move to the next level in the list and remove the current one if we've used it up while looping
					--level.maxRepeats;
					if (level.maxRepeats == 0)
					{
						_verticalLevels.RemoveAt(currentLevel);
					}
					else
					{
						++currentLevel;
					}
				}
				else
				{
					++tries;
				}
			}

			if (command.Type == MSC_ADDLINE && !lineAdded)
			{
				Logger.Warning("Battlescape Generator has encountered an error: a addLine mapscript command with vertical levels did not load the line itself. The modder may want to reduce the number of levels before the line.");
			}

			// Now we can finally load the ceiling level
			if (ceilingBlock != null)
			{
				bool visible = _save.MissionType == "STR_BASE_DEFENSE";

				int terrainMapDataSetIDOffset = LoadExtraTerrain(ceilingTerrain);
				LoadMAP(ceilingBlock, x * 10, y * 10, zOffset, ceilingTerrain, terrainMapDataSetIDOffset, visible);
				_verticalLevelSegments.Add((ceilingBlock, new Position(x, y, zOffset)));
			}
		}
	}

	/// <summary>Clears a module from the map.</summary>
	/// <param name="x">The x offset.</param>
	/// <param name="y">The y offset.</param>
	/// <param name="sizeX">How far along the x axis to clear.</param>
	/// <param name="sizeY">How far along the y axis to clear.</param>
	private void ClearModule(int x, int y, int sizeX, int sizeY)
	{
		for (int z = 0; z != _mapsize_z; ++z)
		{
			for (int dx = x; dx != x + sizeX; ++dx)
			{
				for (int dy = y; dy != y + sizeY; ++dy)
				{
					Tile tile = _save.GetTile(new Position(dx, dy, z));
					for (int i = (int)O_FLOOR; i < (int)O_MAX; i++)
						tile.SetMapData(null, -1, -1, (TilePart)i);
				}
			}
		}
	}

	/// <summary>Loads all the nodes from the map modules.</summary>
	private void LoadNodes()
	{
		int segment = 0;

		// Loading nodes for ground-level maps
		for (int itY = 0; itY < (_mapsize_y / 10); itY++)
		{
			for (int itX = 0; itX < (_mapsize_x / 10); itX++)
			{
				_segments[itX][itY] = segment;
				if (_blocks[itX][itY] != null && _blocks[itX][itY] != _dummy)
				{
					if (!(_blocks[itX][itY].IsInGroup((int)MapBlockType.MT_LANDINGZONE) && _landingzone[itX][itY] && _craftZ == 0))
					{
						LoadRMP(_blocks[itX][itY], itX * 10, itY * 10, 0, segment++);
					}
				}
			}
		}

		// Loading nodes for verticalLevels maps
		foreach (var pair in _verticalLevelSegments)
		{
			LoadRMP(pair.first, pair.second.x * 10, pair.second.y * 10, pair.second.z, segment++);
		}
	}

	/// <summary>Attaches all the nodes together in an intricate web of lies.</summary>
	private void AttachNodeLinks()
	{
		// Since standard vanilla maps are added first, the cutoff between nodes in
		// _segments and those loaded in verticalLevels is the largest value of _segements
		int lastSegmentsIndex = 0;
		for (int itY = 0; itY < (_mapsize_y / 10); itY++)
		{
			for (int itX = 0; itX < (_mapsize_x / 10); itX++)
			{
				if (_segments[itX][itY] > lastSegmentsIndex)
				{
					lastSegmentsIndex = _segments[itX][itY];
				}
			}
		}

		List<Node> nodes = _save.Nodes;

		// First pass is original code, connects all ground-level maps
		foreach (Node node in nodes)
		{
			if (node.IsDummy)
			{
				continue;
			}

			// Did we load this node from a verticalLevel?
			// If so, it's time to go to the next loop!
			if (node.Segment > lastSegmentsIndex)
			{
				break;
			}

			int segmentX = node.Position.x / 10;
			int segmentY = node.Position.y / 10;
			int[] neighbourSegments = new int[4];
			int[] neighbourDirections = [-2, -3, -4, -5];
			int[] neighbourDirectionsInverted = [-4, -5, -2, -3];

			if (segmentX == (_mapsize_x / 10) - 1)
				neighbourSegments[0] = -1;
			else
				neighbourSegments[0] = _segments[segmentX + 1][segmentY];
			if (segmentY == (_mapsize_y / 10) - 1)
				neighbourSegments[1] = -1;
			else
				neighbourSegments[1] = _segments[segmentX][segmentY + 1];
			if (segmentX == 0)
				neighbourSegments[2] = -1;
			else
				neighbourSegments[2] = _segments[segmentX - 1][segmentY];
			if (segmentY == 0)
				neighbourSegments[3] = -1;
			else
				neighbourSegments[3] = _segments[segmentX][segmentY - 1];

			List<int> links = node.NodeLinks;
			for (int j = 0; j < links.Count; ++j)
			{
				for (int n = 0; n < 4; n++)
				{
					if (links[j] == neighbourDirections[n])
					{
						foreach (Node k in nodes)
						{
							if (k.IsDummy)
							{
								continue;
							}
							if (k.Segment == neighbourSegments[n])
							{
								List<int> kLinks = k.NodeLinks;
								for (int l = 0; l < kLinks.Count; ++l)
								{
									if (kLinks[l] == neighbourDirectionsInverted[n])
									{
										kLinks[l] = node.ID;
										links[j] = k.ID;
									}
								}
							}
						}
					}
				}
			}
		}

		// Second pass to obfuscate even further by adding an additional layer of connections
		// Goes through _segmentsVertical to connect nodes from verticalLevels that would
		// be missed in the original code
		var neighbourDirectionsInvertedMap = new SortedDictionary<int, int>
		{
			[-2] = -4,
			[-3] = -5,
			[-4] = -2,
			[-5] = -3,
		};
		foreach (Node node in nodes)
		{
			// All the nodes before lastSegmentsIndex were already connected
			// verticalLevels start for segment values greater than lastSegmentsIndex
			if (node.IsDummy || node.Segment > lastSegmentsIndex)
			{
				continue;
			}

			int nodeX = node.Position.x / 10;
			int nodeY = node.Position.y / 10;
			int nodeZ = node.Position.z;
			// std::map: iterated in key order
			var neighbourDirections = new SortedDictionary<int, int[]>();

			// Make a list of directions in which to look for neighbouring nodes
			if (nodeX != 0)
			{
				neighbourDirections[-5] = [-1, 0, 0];
			}
			if (nodeX != (_mapsize_x / 10) - 1)
			{
				neighbourDirections[-3] = [1, 0, 0];
			}
			if (nodeY != 0)
			{
				neighbourDirections[-2] = [0, -1, 0];
			}
			if (nodeY != (_mapsize_y / 10) - 1)
			{
				neighbourDirections[-4] = [0, 1, 0];
			}
			if (nodeZ != 0)
			{
				neighbourDirections[-6] = [0, 0, -1];
			}
			if (nodeZ != _mapsize_z)
			{
				neighbourDirections[-1] = [0, 0, 1];
			}

			foreach (var j in neighbourDirections)
			{
				bool vertical = j.Key == -1 || j.Key == -6;
				int linkDirection = node.NodeLinks.IndexOf(j.Key);
				if (linkDirection != -1 || vertical)
				{
					foreach (Node k in nodes)
					{
						if (k.IsDummy)
						{
							continue;
						}

						int[] currentDirection = j.Value;
						int linkX = k.Position.x / 10 - currentDirection[0];
						int linkY = k.Position.y / 10 - currentDirection[1];
						int linkZ = k.Position.z - currentDirection[2];

						if (linkX == nodeX && linkY == nodeY && linkZ == nodeZ)
						{
							List<int> kLinks = k.NodeLinks;
							for (int l = 0; l < kLinks.Count; ++l)
							{
								if (neighbourDirectionsInvertedMap.TryGetValue(kLinks[l], out int inverted) && !vertical && inverted == node.NodeLinks[linkDirection])
								{
									kLinks[l] = node.ID;
									node.NodeLinks[linkDirection] = k.ID;
								}
							}

							if (vertical)
							{
								// Create a vertical link between nodes only if the nodes are within an x+y distance of 3 and the link isn't already there
								int xDistance = Math.Abs(node.Position.x - k.Position.x);
								int yDistance = Math.Abs(node.Position.y - k.Position.y);
								int xyDistance = xDistance + yDistance;
								if (xyDistance <= 3 && !kLinks.Contains(node.ID))
								{
									kLinks.Add(node.ID);
									node.NodeLinks.Add(k.ID);
								}
							}
						}
					}
				}
			}
		}
	}

	/// <summary>Selects a position for a map block.</summary>
	/// <param name="rects">The positions to select from, none meaning the whole map.</param>
	/// <param name="X">The x position for the block gets stored in this variable.</param>
	/// <param name="Y">The y position for the block gets stored in this variable.</param>
	/// <param name="sizeX">The x size of the block we want to add.</param>
	/// <param name="sizeY">The y size of the block we want to add.</param>
	/// <returns>If a valid position was selected or not.</returns>
	private bool SelectPosition(List<SDL_Rect> rects, ref int X, ref int Y, int sizeX, int sizeY)
		=> SelectPosition(_blocks, _mapsize_x, _mapsize_y, rects, ref X, ref Y, sizeX, sizeY);

	/// <summary>
	/// Selects an unused position for a map block of a given size (in tiles) within the rects (in blocks),
	/// using an even distribution over all valid positions. <paramref name="X"/>/<paramref name="Y"/> stay unchanged on failure.
	/// </summary>
	/// <param name="blocks">The block grid [x][y] (non-null = used).</param>
	/// <param name="mapsizeX">The map width in tiles.</param>
	/// <param name="mapsizeY">The map length in tiles.</param>
	/// <param name="rects">The positions to select from, none meaning the whole map.</param>
	/// <param name="X">The x position for the block gets stored in this variable.</param>
	/// <param name="Y">The y position for the block gets stored in this variable.</param>
	/// <param name="sizeX">The x size of the block we want to add.</param>
	/// <param name="sizeY">The y size of the block we want to add.</param>
	internal static bool SelectPosition(MapBlock[][] blocks, int mapsizeX, int mapsizeY, List<SDL_Rect> rects, ref int X, ref int Y, int sizeX, int sizeY)
	{
		ArgumentNullException.ThrowIfNull(blocks);
		ArgumentNullException.ThrowIfNull(rects);
		var available = new List<SDL_Rect>();
		var valid = new List<(int, int)>();
		SDL_Rect wholeMap = new(0, 0, mapsizeX / 10, mapsizeY / 10);
		sizeX /= 10;
		sizeY /= 10;
		if (rects.Count == 0)
		{
			available.Add(wholeMap);
		}
		else
		{
			available.AddRange(rects);
		}
		foreach (var rect in available)
		{
			if (sizeX > rect.w || sizeY > rect.h)
			{
				continue;
			}
			for (int x = rect.x; x + sizeX <= rect.x + rect.w && x + sizeX <= wholeMap.w; ++x)
			{
				for (int y = rect.y; y + sizeY <= rect.y + rect.h && y + sizeY <= wholeMap.h; ++y)
				{
					if (!valid.Contains((x, y)))
					{
						bool add = true;
						for (int xCheck = x; xCheck != x + sizeX; ++xCheck)
						{
							for (int yCheck = y; yCheck != y + sizeY; ++yCheck)
							{
								if (blocks[xCheck][yCheck] != null)
								{
									add = false;
								}
							}
						}
						if (add)
						{
							valid.Add((x, y));
						}
					}
				}
			}
		}
		if (valid.Count == 0)
		{
			return false;
		}
		var selection = valid[Rng.Generate(0, valid.Count - 1)];
		X = selection.Item1;
		Y = selection.Item2;
		return true;
	}

	/// <summary>Adds a craft (or UFO) to the map, and tries to add a landing zone type block underneath it.</summary>
	/// <param name="craftMap">The map for the craft in question.</param>
	/// <param name="command">The script command to pull info from.</param>
	/// <param name="craftPos">The position of the craft is stored here.</param>
	/// <param name="terrain">The terrain for the blocks under the craft.</param>
	/// <returns>If the craft was placed or not.</returns>
	private bool AddCraft(MapBlock craftMap, MapScript command, ref SDL_Rect craftPos, RuleTerrain terrain)
	{
		craftPos.w = (ushort)craftMap.SizeX;
		craftPos.h = (ushort)craftMap.SizeY;
		bool placed;
		int x = 0, y = 0;

		placed = SelectPosition(command.Rects, ref x, ref y, craftPos.w, craftPos.h);
		// if ok, allocate it
		if (placed)
		{
			if (_verticalLevels.Count != 0)
			{
				command.InitVerticalLevel(_verticalLevels[0]);
			}

			craftPos.x = (short)x;
			craftPos.y = (short)y;
			craftPos.w /= 10;
			craftPos.h /= 10;
			for (x = 0; x < craftPos.w; ++x)
			{
				for (y = 0; y < craftPos.h; ++y)
				{
					_landingzone[craftPos.x + x][craftPos.y + y] = true;
					MapBlock block = command.GetNextBlock(terrain);
					if (block != null && _blocks[craftPos.x + x][craftPos.y + y] == null)
					{
						_save.ReinforcementsBlocks[craftPos.x + x][craftPos.y + y] = _markAsReinforcementsBlock;
						_save.FlattenedMapTerrainNames[craftPos.x + x][craftPos.y + y] = terrain.Name;
						_save.FlattenedMapBlockNames[craftPos.x + x][craftPos.y + y] = block.Name;
						_blocks[craftPos.x + x][craftPos.y + y] = block;
						_blocksToDo--;
					}
				}
			}
		}

		return placed;
	}

	/// <summary>Draws a line along the map either horizontally, vertically or both.</summary>
	/// <param name="direction">The direction to draw the line.</param>
	/// <param name="rects">The positions to allow the line to be drawn in.</param>
	/// <param name="terrain">The terrain to take the blocks from.</param>
	/// <param name="verticalGroup">The block group of vertical lines.</param>
	/// <param name="horizontalGroup">The block group of horizontal lines.</param>
	/// <param name="crossingGroup">The block group of crossings.</param>
	/// <returns>If the blocks were added or not.</returns>
	private bool AddLine(MapDirection direction, List<SDL_Rect> rects, RuleTerrain terrain, int verticalGroup, int horizontalGroup, int crossingGroup)
	{
		if (direction == MD_BOTH)
		{
			if (AddLine(MD_VERTICAL, rects, terrain, verticalGroup, horizontalGroup, crossingGroup))
			{
				AddLine(MD_HORIZONTAL, rects, terrain, verticalGroup, horizontalGroup, crossingGroup);
				return true;
			}
			return false;
		}

		int tries = 0;
		bool placed = false;

		// road[0] = roadX, road[1] = roadY; C++ iterates one of them through a pointer
		int[] road = [0, 0];
		int iteratorValue = 0;
		int comparator = verticalGroup;
		int typeToAdd = horizontalGroup;
		int limit = _mapsize_x / 10;
		if (direction == MD_VERTICAL)
		{
			iteratorValue = 1;
			comparator = horizontalGroup;
			typeToAdd = verticalGroup;
			limit = _mapsize_y / 10;
		}
		while (!placed)
		{
			placed = SelectPosition(rects, ref road[0], ref road[1], 10, 10);
			for (road[iteratorValue] = 0; road[iteratorValue] < limit; road[iteratorValue] += 1)
			{
				if (placed && _blocks[road[0]][road[1]] != null && _blocks[road[0]][road[1]].IsInGroup(comparator) == false)
				{
					placed = false;
					break;
				}
			}
			if (tries++ > 20)
			{
				return false;
			}
		}
		road[iteratorValue] = 0;
		while (road[iteratorValue] < limit)
		{
			int roadX = road[0], roadY = road[1];
			if (_blocks[roadX][roadY] == null)
			{
				MapBlock randomMapBlock1 = terrain.GetRandomMapBlock(10, 10, typeToAdd);
				if (randomMapBlock1 == null)
				{
					throw new InvalidOperationException($"Map script command `addLine` failed (1). Terrain {terrain.Name}, block group {typeToAdd}, script {_save.LastUsedMapScript}");
				}
				AddBlock(roadX, roadY, randomMapBlock1, terrain);

				// Only add unique positions for loading vertical levels
				AddUniquePlacedBlockRect(roadX, roadY);
			}
			else if (_blocks[roadX][roadY].IsInGroup(comparator))
			{
				MapBlock randomMapBlock2 = terrain.GetRandomMapBlock(10, 10, crossingGroup);
				if (randomMapBlock2 == null)
				{
					throw new InvalidOperationException($"Map script command `addLine` failed (2). Terrain {terrain.Name}, block group {crossingGroup}, script {_save.LastUsedMapScript}");
				}
				_blocks[roadX][roadY] = randomMapBlock2;
				ClearModule(roadX * 10, roadY * 10, 10, 10);
				int terrainMapDataSetIDOffset = LoadExtraTerrain(terrain);
				LoadMAP(_blocks[roadX][roadY], roadX * 10, roadY * 10, 0, terrain, terrainMapDataSetIDOffset);

				// Only add unique positions for loading vertical levels
				AddUniquePlacedBlockRect(roadX, roadY);
			}
			road[iteratorValue] += 1;
		}
		return true;
	}

	/// <summary>Remembers a 1x1 block rect for the vertical levels, unless its position is already in the list.</summary>
	private void AddUniquePlacedBlockRect(int x, int y)
	{
		if (!_placedBlockRects.Exists(rect => rect.x == x && rect.y == y))
		{
			_placedBlockRects.Add(new SDL_Rect(x, y, 1, 1));
		}
	}

	/// <summary>Adds a single block to the map.</summary>
	/// <param name="x">The x position to add the block.</param>
	/// <param name="y">The y position to add the block.</param>
	/// <param name="block">The block to add.</param>
	/// <param name="terrain">Pointer to the terrain for the block.</param>
	/// <returns>If the block was added or not.</returns>
	private bool AddBlock(int x, int y, MapBlock block, RuleTerrain terrain)
	{
		int xSize = (block.SizeX - 1) / 10;
		int ySize = (block.SizeY - 1) / 10;

		for (int xd = 0; xd <= xSize; ++xd)
		{
			// (sic) C++ checks yd != ySize, so the last row of the block is not checked
			for (int yd = 0; yd != ySize; ++yd)
			{
				if (_blocks[x + xd][y + yd] != null)
					return false;
			}
		}

		for (int xd = 0; xd <= xSize; ++xd)
		{
			for (int yd = 0; yd <= ySize; ++yd)
			{
				_save.ReinforcementsBlocks[x + xd][y + yd] = _markAsReinforcementsBlock;
				_save.FlattenedMapTerrainNames[x + xd][y + yd] = terrain.Name;
				_save.FlattenedMapBlockNames[x + xd][y + yd] = block.Name;
				_blocks[x + xd][y + yd] = _dummy;
				_blocksToDo--;
			}
		}

		// mark the south edge of the block for drilling
		for (int xd = 0; xd <= xSize; ++xd)
		{
			_drillMap[x + xd][y + ySize] = MD_VERTICAL;
		}
		// then the east edge
		for (int yd = 0; yd <= ySize; ++yd)
		{
			_drillMap[x + xSize][y + yd] = MD_HORIZONTAL;
		}
		// then the far corner gets marked for both
		// this also marks 1x1 modules
		_drillMap[x + xSize][y + ySize] = MD_BOTH;

		_blocks[x][y] = block;
		bool visible = _save.MissionType == "STR_BASE_DEFENSE"; // yes, i'm hard coding these, big whoop, wanna fight about it?

		int terrainMapDataSetIDOffset = LoadExtraTerrain(terrain);
		LoadMAP(_blocks[x][y], x * 10, y * 10, 0, terrain, terrainMapDataSetIDOffset, visible);

		return true;
	}

	/// <summary>
	/// Drills a tunnel between existing map modules.
	/// Note that this drills all modules currently on the map,
	/// so it should take place BEFORE the dirt is added in base defenses.
	/// </summary>
	/// <param name="data">The wall replacements and level to dig on.</param>
	/// <param name="rects">The length/width of the tunnels themselves.</param>
	/// <param name="dir">The direction to drill.</param>
	/// <param name="terrain">Which terrain to use for the wall replacements.</param>
	private void DrillModules(TunnelData data, List<SDL_Rect> rects, MapDirection dir, RuleTerrain terrain)
	{
		// Make sure the terrain we're going to use for the replacements is loaded
		LoadExtraTerrain(terrain);

		MCDReplacement wWall = data.GetMCDReplacement("westWall");
		MCDReplacement nWall = data.GetMCDReplacement("northWall");
		MCDReplacement corner = data.GetMCDReplacement("corner");
		MCDReplacement floor = data.GetMCDReplacement("floor");
		SDL_Rect rect = new(3, 3, 3, 3);
		if (rects.Count != 0)
		{
			rect = rects[0];
		}

		for (int i = 0; i < (_mapsize_x / 10); ++i)
		{
			for (int j = 0; j < (_mapsize_y / 10); ++j)
			{
				if (_blocks[i][j] == null)
					continue;

				MapData md;

				if (dir != MD_VERTICAL)
				{
					// drill east
					if (i < (_mapsize_x / 10) - 1 && (_drillMap[i][j] == MD_HORIZONTAL || _drillMap[i][j] == MD_BOTH) && _blocks[i + 1][j] != null)
					{
						Tile tile;
						// remove stuff
						for (int k = rect.y; k != rect.y + rect.h; ++k)
						{
							tile = _save.GetTile(new Position((i * 10) + 9, (j * 10) + k, data.level));
							if (tile != null)
							{
								tile.SetMapData(null, -1, -1, O_WESTWALL);
								tile.SetMapData(null, -1, -1, O_OBJECT);
								if (floor != null)
								{
									md = terrain.MapDataSets[floor.set].GetObject(floor.entry);
									tile.SetMapData(md, floor.entry, floor.set, O_FLOOR);
								}

								tile = _save.GetTile(new Position((i + 1) * 10, (j * 10) + k, data.level));
								tile.SetMapData(null, -1, -1, O_WESTWALL);
								MapData obj = tile.GetMapData(O_OBJECT);
								if (obj != null && obj.GetTUCost(MovementType.MT_WALK) == 0)
								{
									tile.SetMapData(null, -1, -1, O_OBJECT);
								}
							}
						}

						if (nWall != null)
						{
							md = terrain.MapDataSets[nWall.set].GetObject(nWall.entry);
							tile = _save.GetTile(new Position((i * 10) + 9, (j * 10) + rect.y, data.level));
							tile.SetMapData(md, nWall.entry, nWall.set, O_NORTHWALL);
							tile = _save.GetTile(new Position((i * 10) + 9, (j * 10) + rect.y + rect.h, data.level));
							tile.SetMapData(md, nWall.entry, nWall.set, O_NORTHWALL);
						}

						if (corner != null)
						{
							md = terrain.MapDataSets[corner.set].GetObject(corner.entry);
							tile = _save.GetTile(new Position((i + 1) * 10, (j * 10) + rect.y, data.level));
							if (tile.GetMapData(O_NORTHWALL) == null)
								tile.SetMapData(md, corner.entry, corner.set, O_NORTHWALL);
						}
					}
				}

				if (dir != MD_HORIZONTAL)
				{
					// drill south
					if (j < (_mapsize_y / 10) - 1 && (_drillMap[i][j] == MD_VERTICAL || _drillMap[i][j] == MD_BOTH) && _blocks[i][j + 1] != null)
					{
						// remove stuff
						for (int k = rect.x; k != rect.x + rect.w; ++k)
						{
							Tile tile = _save.GetTile(new Position((i * 10) + k, (j * 10) + 9, data.level));
							if (tile != null)
							{
								tile.SetMapData(null, -1, -1, O_NORTHWALL);
								tile.SetMapData(null, -1, -1, O_OBJECT);
								if (floor != null)
								{
									md = terrain.MapDataSets[floor.set].GetObject(floor.entry);
									tile.SetMapData(md, floor.entry, floor.set, O_FLOOR);
								}

								tile = _save.GetTile(new Position((i * 10) + k, (j + 1) * 10, data.level));
								tile.SetMapData(null, -1, -1, O_NORTHWALL);
								MapData obj = tile.GetMapData(O_OBJECT);
								if (obj != null && obj.GetTUCost(MovementType.MT_WALK) == 0)
								{
									tile.SetMapData(null, -1, -1, O_OBJECT);
								}
							}
						}

						if (wWall != null)
						{
							md = terrain.MapDataSets[wWall.set].GetObject(wWall.entry);
							Tile tile = _save.GetTile(new Position((i * 10) + rect.x, (j * 10) + 9, data.level));
							tile.SetMapData(md, wWall.entry, wWall.set, O_WESTWALL);
							tile = _save.GetTile(new Position((i * 10) + rect.x + rect.w, (j * 10) + 9, data.level));
							tile.SetMapData(md, wWall.entry, wWall.set, O_WESTWALL);
						}

						if (corner != null)
						{
							md = terrain.MapDataSets[corner.set].GetObject(corner.entry);
							Tile tile = _save.GetTile(new Position((i * 10) + rect.x, (j + 1) * 10, data.level));
							if (tile.GetMapData(O_WESTWALL) == null)
								tile.SetMapData(md, corner.entry, corner.set, O_WESTWALL);
						}
					}
				}
			}
		}
	}

	/// <summary>Removes all blocks within a given set of rects, as defined in the command.</summary>
	/// <param name="command">Contains all the info we need.</param>
	/// <returns>Success of the removal.</returns>
	private bool RemoveBlocks(MapScript command)
	{
		var deleted = new List<(int, int)>();
		bool success = false;

		foreach (var rect in command.Rects)
		{
			for (int x = rect.x; x != rect.x + rect.w && x != _mapsize_x / 10; ++x)
			{
				for (int y = rect.y; y != rect.y + rect.h && y != _mapsize_y / 10; ++y)
				{
					if (_blocks[x][y] != null && _blocks[x][y] != _dummy)
					{
						var pos = (x, y);
						if (command.Groups.Count != 0)
						{
							foreach (int grp in command.Groups)
							{
								if (_blocks[x][y].IsInGroup(grp))
								{
									// the deleted vector should only contain unique entries
									if (!deleted.Contains(pos))
									{
										deleted.Add(pos);
									}
								}
							}
						}
						else if (command.Blocks.Count != 0)
						{
							foreach (int blck in command.Blocks)
							{
								// (sic) C++ only checks that the block index is valid, not that it is this block
								if ((uint)blck < (uint)_terrain.MapBlocks.Count)
								{
									// the deleted vector should only contain unique entries
									if (!deleted.Contains(pos))
									{
										deleted.Add(pos);
									}
								}
							}
						}
						else
						{
							// the deleted vector should only contain unique entries
							if (!deleted.Contains(pos))
							{
								deleted.Add(pos);
							}
						}
					}
				}
			}
		}
		foreach (var (x, y) in deleted)
		{
			ClearModule(x * 10, y * 10, _blocks[x][y].SizeX, _blocks[x][y].SizeY);

			int delx = _blocks[x][y].SizeX / 10;
			int dely = _blocks[x][y].SizeY / 10;

			for (int dx = x; dx != x + delx; ++dx)
			{
				for (int dy = y; dy != y + dely; ++dy)
				{
					_save.ReinforcementsBlocks[dx][dy] = 0;
					_save.FlattenedMapTerrainNames[dx][dy] = "";
					_save.FlattenedMapBlockNames[dx][dy] = "";
					_blocks[dx][dy] = null;
					_blocksToDo++;

					// Make sure vertical levels segment data is removed too
					_verticalLevelSegments.RemoveAll(it => it.second.x == dx && it.second.y == dy);
				}
			}
			// this command succeeds if even one block is removed.
			success = true;
		}
		return success;
	}
}
