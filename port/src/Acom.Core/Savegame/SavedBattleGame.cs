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
// Ported from: src/Savegame/SavedBattleGame.h, src/Savegame/SavedBattleGame.cpp
#nullable disable

namespace Acom;

/// <summary>
/// The battlescape data that gets written to disk when the game is saved.
/// A saved game holds all the variable info in a game like mapdata,
/// soldiers, items, etc.
/// </summary>
public partial class SavedBattleGame
{
	public const string ScriptName = "BattleGame";

	private readonly bool _isPreview;
	private SDL_Rect _craftPos;
	private int _craftZ;
	private Craft _craftForPreview;
	private readonly List<Position> _craftTiles = [];
	private BattlescapeState _battleState;
	private readonly Mod _rule;
	private int _mapsize_x, _mapsize_y, _mapsize_z;
	private readonly List<MapDataSet> _mapDataSets = [];
	/// <summary>C++ std::vector&lt;Tile&gt; (by value); neighbours are found by index instead of pointer arithmetic.</summary>
	private Tile[] _tiles = [];
	private BattleUnit _selectedUnit, _undoUnit, _lastSelectedUnit;
	private readonly List<Node> _nodes = [];
	private readonly List<BattleUnit> _units = [];
	private readonly List<BattleItem> _items = [], _deleted = [];
	private Pathfinding _pathfinding;
	private TileEngine _tileEngine;
	private string _missionType = "", _strTarget = "", _strCraftOrBase = "", _alienCustomDeploy = "", _alienCustomMission = "";
	private string _lastUsedMapScript = "";
	private int _alienItemLevel;
	private string _reinforcementsDeployment = "", _reinforcementsRace = "";
	private int _reinforcementsItemLevel;
	private SortedDictionary<string, int> _reinforcementsMemory = new(StringComparer.Ordinal);
	private List<List<int>> _reinforcementsBlocks = [];
	private List<List<string>> _flattenedMapTerrainNames = [];
	private List<List<string>> _flattenedMapBlockNames = [];
	private RuleStartingCondition _startingCondition;
	private RuleEnviroEffects _enviroEffects;
	private bool _ecEnabledFriendly, _ecEnabledHostile, _ecEnabledNeutral;
	private int _globalShade;
	private UnitFaction _side;
	private int _turn, _bughuntMinTurn;
	private int _animFrame;
	private bool _nameDisplay;
	private bool _debugMode, _bughuntMode;
	private bool _aborted;
	private bool _baseCraftInventory;
	private int _itemId;
	private EscapeType _vipEscapeType;
	private int _vipSurvivalPercentage, _vipsSaved, _vipsLost, _vipsWaitingOutside, _vipsSavedScore, _vipsLostScore, _vipsWaitingOutsideScore;
	private int _objectiveType, _objectivesDestroyed, _objectivesNeeded;
	// C++ _exposedUnits is never used and not ported.
	/// <summary>C++ std::list (push_front in <see cref="AddFallingUnit"/>).</summary>
	private readonly List<BattleUnit> _fallingUnits = [];
	private bool _unitsFalling, _cheating;
	private readonly List<Position> _tileSearch = [];
	private readonly List<Position> _storageSpace = [];
	private BattleActionType _tuReserved;
	private bool _kneelReserved;
	private List<List<(int first, int second)>> _baseModules = [];
	private readonly ItemContainer _baseItems;
	private int _depth, _ambience;
	private double _ambientVolume;
	private List<int> _ambienceRandom = [];
	private int _minAmbienceRandomDelay, _maxAmbienceRandomDelay, _currentAmbienceDelay;
	private readonly List<BattleItem> _recoverGuaranteed = [], _recoverConditional = [];
	private string _music = "";
	private int _turnLimit, _cheatTurn;
	private ChronoTrigger _chronoTrigger;
	private bool _beforeGame;
	private bool _togglePersonalLight, _toggleNightVision;
	private int _toggleBrightness;
	// final values of the toggles, read by the script bindings
	internal bool _togglePersonalLightTemp, _toggleNightVisionTemp;
	internal int _toggleBrightnessTemp, _toggleNightVisionColorTemp;
	private string _hiddenMovementBackground;
	private readonly HitLog _hitLog;

	/// <summary>Initializes a brand new battlescape saved game.</summary>
	public SavedBattleGame(Mod rule, Language lang, bool isPreview = false)
	{
		_isPreview = isPreview;
		_craftPos = new SDL_Rect();
		_craftZ = 0;
		_craftForPreview = null;
		_battleState = null;
		_rule = rule;
		_reinforcementsItemLevel = 0;
		_side = UnitFaction.FACTION_PLAYER;
		_bughuntMinTurn = 20;
		_vipEscapeType = EscapeType.ESCAPE_NONE;
		_objectiveType = -1;
		_tuReserved = BattleActionType.BA_NONE;
		_ambience = -1;
		_ambientVolume = 0.5;
		_minAmbienceRandomDelay = 20;
		_maxAmbienceRandomDelay = 60;
		_cheatTurn = 20;
		_chronoTrigger = ChronoTrigger.FORCE_LOSE;
		_beforeGame = true;
		_togglePersonalLight = true;

		for (int i = 0; i < 121; ++i)
		{
			_tileSearch.Add(new Position((i % 11) - 5, (i / 11) - 5, 0));
		}
		_baseItems = new ItemContainer();
		_hitLog = new HitLog(lang);

		SetRandomHiddenMovementBackground(_rule);
	}

	/// <summary>
	/// C++ destructor: unloads the terrain data of the map data sets. Call it where the C++ code deletes the battle save
	/// (units, items, nodes and the map utilities are left to the garbage collector).
	/// </summary>
	public void Destroy()
	{
		foreach (var mds in _mapDataSets)
		{
			mds.UnloadData();
		}
	}

	/// <summary>Reads a C++ <c>char</c> value: c4core takes the single character of the scalar.</summary>
	private static byte ReadChar(YamlNodeReader reader, byte defaultValue)
	{
		string s = null;
		if (reader.TryReadVal(ref s) && s != null && s.Length == 1)
			return unchecked((byte)s[0]);
		return defaultValue;
	}

	/// <summary>Writes a C++ <c>char</c> value: the raw character, double-quoted and escaped.</summary>
	private static void WriteChar(YamlNodeWriter writer, string key, byte value)
	{
		writer.Write(key, ((char)value).ToString()).SetAsQuotedAndEscaped();
	}

	/// <summary>Loads the saved battle game from a YAML file.</summary>
	/// <param name="node">YAML node.</param>
	/// <param name="mod">Mod for the saved game.</param>
	/// <param name="savedGame">Pointer to saved game.</param>
	public void Load(YamlNodeReader node, Mod mod, SavedGame savedGame)
	{
		ArgumentNullException.ThrowIfNull(mod);
		ArgumentNullException.ThrowIfNull(savedGame);
		var reader = node.UseIndex();
		int mapsize_x = reader["width"].ReadVal(_mapsize_x);
		int mapsize_y = reader["length"].ReadVal(_mapsize_y);
		int mapsize_z = reader["height"].ReadVal(_mapsize_z);
		InitMap(mapsize_x, mapsize_y, mapsize_z);

		reader.TryRead("missionType", ref _missionType);
		reader.TryRead("strTarget", ref _strTarget);
		reader.TryRead("strCraftOrBase", ref _strCraftOrBase);
		if (reader["startingConditionType"])
		{
			string startingConditionType = reader["startingConditionType"].ReadVal<string>();
			_startingCondition = mod.GetStartingCondition(startingConditionType);
		}
		if (reader["enviroEffectsType"])
		{
			string enviroEffectsType = reader["enviroEffectsType"].ReadVal<string>();
			_enviroEffects = mod.GetEnviroEffects(enviroEffectsType);
		}
		reader.TryRead("nameDisplay", ref _nameDisplay);
		reader.TryRead("ecEnabledFriendly", ref _ecEnabledFriendly);
		reader.TryRead("ecEnabledHostile", ref _ecEnabledHostile);
		reader.TryRead("ecEnabledNeutral", ref _ecEnabledNeutral);
		reader.TryRead("alienCustomDeploy", ref _alienCustomDeploy);
		reader.TryRead("alienCustomMission", ref _alienCustomMission);
		reader.TryRead("alienItemLevel", ref _alienItemLevel);
		reader.TryRead("lastUsedMapScript", ref _lastUsedMapScript);
		reader.TryRead("reinforcementsDeployment", ref _reinforcementsDeployment);
		reader.TryRead("reinforcementsRace", ref _reinforcementsRace);
		reader.TryRead("reinforcementsItemLevel", ref _reinforcementsItemLevel);
		var reinforcementsMemory = _reinforcementsMemory;
		if (reader.TryRead("reinforcementsMemory", ref reinforcementsMemory))
			_reinforcementsMemory = new SortedDictionary<string, int>(reinforcementsMemory, StringComparer.Ordinal);
		reader.TryRead("reinforcementsBlocks", ref _reinforcementsBlocks);
		reader.TryRead("flattenedMapTerrainNames", ref _flattenedMapTerrainNames);
		reader.TryRead("flattenedMapBlockNames", ref _flattenedMapBlockNames);
		reader.TryRead("globalshade", ref _globalShade);
		reader.TryRead("turn", ref _turn);
		reader.TryRead("bughuntMinTurn", ref _bughuntMinTurn);
		reader.TryRead("bughuntMode", ref _bughuntMode);
		reader.TryRead("depth", ref _depth);
		reader.TryRead("animFrame", ref _animFrame);
		int selectedUnitId = reader["selectedUnit"].ReadVal<int>();
		int undoUnitId = reader["undoUnit"].ReadVal(-1);

		foreach (var mdsReader in reader["mapdatasets"].Children())
		{
			string name = mdsReader.ReadVal<string>();
			MapDataSet mds = mod.GetMapDataSet(name);
			_mapDataSets.Add(mds);
		}

		LoadTiles(reader);

		if (_missionType == "STR_BASE_DEFENSE")
		{
			if (!reader.TryRead("moduleMap", ref _baseModules))
			{
				// backwards compatibility: imperfect solution, modules that were completely destroyed
				// prior to saving and updating builds will be counted as indestructible.
				CalculateModuleMap();
			}
		}
		LoadNodes(reader);

		// int -> BattleUnit; dictionaries for O(1) ID searching
		var unitIndex = new Dictionary<int, BattleUnit>();
		var itemIndex = new Dictionary<int, BattleItem>();

		BattleUnit findUnitById(YamlNodeReader r)
		{
			int id = r.ReadVal(-1);
			if (id == -1 || !unitIndex.TryGetValue(id, out var unit))
				return null;
			return unit;
		}

		// units 1st pass
		foreach (var unitReader in reader["units"].Children())
		{
			UnitFaction faction = unitReader["faction"].ReadVal<UnitFaction>();
			UnitFaction originalFaction = unitReader["originalFaction"].ReadVal(faction);
			int id = unitReader["id"].ReadVal<int>();
			BattleUnit unit;
			if (id < BattleUnit.MAX_SOLDIER_ID) // Unit is linked to a geoscape soldier
				unit = new BattleUnit(mod, savedGame.GetSoldier(id), _depth, null); // look up the matching soldier
			else
			{
				string type = unitReader["genUnitType"].ReadVal<string>();
				string armor = unitReader["genUnitArmor"].ReadVal<string>();
				// create a new Unit.
				if (mod.GetUnit(type) == null || mod.GetArmor(armor) == null)
					continue;
				unit = new BattleUnit(mod, mod.GetUnit(type), originalFaction, id, null, mod.GetArmor(armor), mod.GetStatAdjustment((int)savedGame.Difficulty), _depth, null);
			}
			unit.Load(unitReader, Mod);
			// Handling of special built-in weapons will be done during and after the load of items
			// unit->setSpecialWeapon(this, true);
			if (faction == UnitFaction.FACTION_PLAYER)
			{
				if ((unit.Id == selectedUnitId) || (_selectedUnit == null && !unit.IsOut))
					_selectedUnit = unit;
				if (unit.Id == undoUnitId)
					_undoUnit = unit;
			}
			else if (unit.Status != UnitStatus.STATUS_DEAD && !unit.IsIgnored)
			{
				if (unitReader["AI"] is { IsValid: true } ai)
				{
					AIModule aiModule = new AIModule(this, unit, null);
					aiModule.Load(ai);
					unit.AIModule = aiModule;
				}
			}
			unitIndex[id] = unit;
			_units.Add(unit);
		}

		(string first, List<BattleItem> second)[] itemKeysAndVectors =
		[
			("items", _items),
			("recoverConditional", _recoverConditional),
			("recoverGuaranteed", _recoverGuaranteed),
			("itemsSpecial", _items),
		];
		// items 1st pass
		foreach (var keyAndVector in itemKeysAndVectors)
		{
			foreach (var itemReader in reader[keyAndVector.first].Children())
			{
				string type = itemReader["type"].ReadVal<string>();
				if (mod.GetItem(type) == null)
				{
					Logger.Error($"Failed to load item {type}");
					continue;
				}
				int id = itemReader["id"].ReadVal<int>();
				BattleItem item = new BattleItem(mod.GetItem(type), ref id); //passing id as a pointer to serve as a counter is no longer used
				item.Load(itemReader, mod);

				if (findUnitById(itemReader["owner"]) is { } owner)
				{
					item.Owner = owner;
					if (item.IsSpecialWeapon)
						owner.AddLoadedSpecialWeapon(item);
					else
						owner.Inventory.Add(item);
				}
				item.PreviousOwner = findUnitById(itemReader["previousOwner"]);
				item.Unit = findUnitById(itemReader["unit"]);

				// match up items and tiles
				if (item.Slot != null && item.Slot.Type == InventoryType.INV_GROUND)
				{
					Position pos = itemReader["position"].ReadVal(new Position(-1, -1, -1));
					if (pos.x != -1)
						GetTile(pos).AddItem(item, item.Slot);
				}
				_itemId = Math.Max(_itemId, item.Id);
				itemIndex[item.Id] = item;
				keyAndVector.second.Add(item);
			}
		}
		_itemId++;

		// units 2nd pass
		foreach (var unitReader in reader["units"].Children())
		{
			if (findUnitById(unitReader["id"]) is { } bu) //not guaranteed that the unit was created
			{
				if (!bu.IsIgnored && bu.Status != UnitStatus.STATUS_DEAD)
					bu.SetSpecialWeapon(this, true); // Note: this is for backwards-compatibility with older saves
				bu.PreviousOwner = findUnitById(unitReader["previousOwner"]);
			}
		}

		// items 2nd pass
		foreach (var keyAndVector in itemKeysAndVectors)
		{
			foreach (var itemReader in reader[keyAndVector.first].Children())
			{
				string type = itemReader["type"].ReadVal<string>();
				if (mod.GetItem(type) == null)
					continue;
				BattleItem item = itemIndex[itemReader["id"].ReadVal<int>()];
				// if "ammoItemSlots" node exists, then link ammo items for all slots, else try the backwards-compatibility node
				if (itemReader["ammoItemSlots"] is { IsValid: true } slotsReader)
				{
					for (int slotIndex = 0; slotIndex < RuleItem.AmmoSlotMax; slotIndex++)
					{
						int itemId = slotsReader[slotIndex].ReadVal(-1);
						if (itemId > -1 && itemIndex.TryGetValue(itemId, out var ammo))
							item.SetAmmoForSlot(slotIndex, ammo);
					}
				}
				else if (itemReader["ammoItem"] is { IsValid: true } slotReader)
					item.SetAmmoForSlot(0, itemIndex[slotReader.ReadVal(0)]);
			}
		}

		// restore order like before save
		// is this necessary? who knows...
		foreach (var keyAndVector in itemKeysAndVectors)
		{
			keyAndVector.second.Sort((itemA, itemB) => itemA.Id.CompareTo(itemB.Id));
		}

		reader.TryRead("vipEscapeType", ref _vipEscapeType);
		reader.TryRead("vipSurvivalPercentage", ref _vipSurvivalPercentage);
		reader.TryRead("vipsSaved", ref _vipsSaved);
		reader.TryRead("vipsLost", ref _vipsLost);
		reader.TryRead("vipsWaitingOutside", ref _vipsWaitingOutside);
		reader.TryRead("vipsSavedScore", ref _vipsSavedScore);
		reader.TryRead("vipsLostScore", ref _vipsLostScore);
		reader.TryRead("vipsWaitingOutsideScore", ref _vipsWaitingOutsideScore);
		reader.TryRead("objectiveType", ref _objectiveType);
		reader.TryRead("objectivesDestroyed", ref _objectivesDestroyed);
		reader.TryRead("objectivesNeeded", ref _objectivesNeeded);
		reader.TryRead("tuReserved", ref _tuReserved);
		reader.TryRead("kneelReserved", ref _kneelReserved);
		reader.TryRead("ambience", ref _ambience);
		reader.TryRead("ambientVolume", ref _ambientVolume);
		reader.TryRead("ambienceRandom", ref _ambienceRandom);
		reader.TryRead("minAmbienceRandomDelay", ref _minAmbienceRandomDelay);
		reader.TryRead("maxAmbienceRandomDelay", ref _maxAmbienceRandomDelay);
		reader.TryRead("currentAmbienceDelay", ref _currentAmbienceDelay);
		reader.TryRead("music", ref _music);
		_baseItems.Load(reader["baseItems"], mod);
		reader.TryRead("turnLimit", ref _turnLimit);
		reader.TryRead("chronoTrigger", ref _chronoTrigger);
		reader.TryRead("cheatTurn", ref _cheatTurn);
		reader.TryRead("togglePersonalLight", ref _togglePersonalLight);
		reader.TryRead("toggleNightVision", ref _toggleNightVision);
		reader.TryRead("toggleBrightness", ref _toggleBrightness);
		if (_rule != null)
		{
			_scriptValues.Load(reader, _rule.ScriptGlobal);
		}

		// Sanity checks
		foreach (var unit in _units)
		{
			switch (unit.Status)
			{
				case UnitStatus.STATUS_STANDING:
				case UnitStatus.STATUS_DEAD:
				case UnitStatus.STATUS_UNCONSCIOUS:
				case UnitStatus.STATUS_IGNORE_ME:
					break;
				default:
					Logger.Error($"Save '{savedGame.Name}' is corrupted. Unit {unit.Type} (id: {unit.Id}) has an invalid 'status: {(int)unit.Status}'");
					break;
			}
		}
	}

	/// <summary>Part of <see cref="Load"/>: the tiles, either the binary (base64) block or old-style text tiles.</summary>
	internal void LoadTiles(YamlNodeReader reader)
	{
		if (!reader["tileTotalBytesPer"])
		{
			// binary tile data not found, load old-style text tiles :(
			foreach (var tile in reader["tiles"].Children())
			{
				Position pos = tile["position"].ReadVal<Position>();
				GetTile(pos).Load(tile);
			}
		}
		else
		{
			// load key to how the tile data was saved
			Tile.SerializationKey serKey = default;
			int totalTiles = reader["totalTiles"].ReadVal<int>();

			serKey.index = ReadChar(reader["tileIndexSize"], serKey.index);
			serKey.totalBytes = reader["tileTotalBytesPer"].ReadVal(serKey.totalBytes);
			serKey._fire = ReadChar(reader["tileFireSize"], serKey._fire);
			serKey._smoke = ReadChar(reader["tileSmokeSize"], serKey._smoke);
			serKey._mapDataID = ReadChar(reader["tileIDSize"], serKey._mapDataID);
			serKey._mapDataSetID = ReadChar(reader["tileSetIDSize"], serKey._mapDataSetID);
			serKey.boolFields = ReadChar(reader["tileBoolFieldsSize"], 1); // boolean flags used to be stored in an unmentioned byte (Uint8) :|

			// load binary tile data!
			byte[] binTiles = reader["binTiles"].ReadValBase64();

			ReadOnlySpan<byte> ptr = binTiles.AsSpan(0, (int)(totalTiles * serKey.totalBytes));

			while (!ptr.IsEmpty)
			{
				int index = SerializationHelper.UnserializeInt(ref ptr, serKey.index);
				System.Diagnostics.Debug.Assert(index >= 0 && index < _mapsize_x * _mapsize_z * _mapsize_y);
				_tiles[index].LoadBinary(ptr, serKey); // loadBinary's privileges to advance *r have been revoked
				ptr = ptr[(int)(serKey.totalBytes - serKey.index)..]; // r is now incremented strictly by totalBytes in case there are obsolete fields present in the data
			}
		}
	}

	/// <summary>Part of <see cref="Load"/>: the route nodes.</summary>
	internal void LoadNodes(YamlNodeReader reader)
	{
		foreach (var nodeConfig in reader["nodes"].Children())
		{
			Node n = new Node();
			n.Load(nodeConfig);
			_nodes.Add(n);
		}
	}

	/// <summary>Loads the resources required by the map in the battle save.</summary>
	public void LoadMapResources(Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		foreach (var mds in _mapDataSets)
		{
			mds.LoadData(mod.GetMCDPatch(mds.Name));
		}

		int mdsID = 0, mdID = 0;

		for (int i = 0; i < _mapsize_z * _mapsize_y * _mapsize_x; ++i)
		{
			for (int part = (int)TilePart.O_FLOOR; part < (int)TilePart.O_MAX; part++)
			{
				TilePart tp = (TilePart)part;
				_tiles[i].GetMapData(ref mdID, ref mdsID, tp);
				if (mdID != -1 && mdsID != -1)
				{
					_tiles[i].SetMapData(_mapDataSets[mdsID].GetObject(mdID), mdID, mdsID, tp);
				}
				else
				{
					_tiles[i].SetMapData(null, -1, -1, tp);
				}
			}
		}

		InitUtilities(mod);
		// matches up tiles and units
		ResetUnitTiles();
		TileEngine.CalculateLighting(LightLayers.LL_AMBIENT, TileEngine.invalid, 0, true);
		TileEngine.RecalculateFOV();
	}

	/// <summary>Saves the saved battle game to a YAML file.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		if (_vipSurvivalPercentage > 0)
		{
			writer.Write("vipEscapeType", _vipEscapeType);
			writer.Write("vipSurvivalPercentage", _vipSurvivalPercentage);
			writer.Write("vipsSaved", _vipsSaved);
			writer.Write("vipsLost", _vipsLost);
			writer.Write("vipsWaitingOutside", _vipsWaitingOutside);
			writer.Write("vipsSavedScore", _vipsSavedScore);
			writer.Write("vipsLostScore", _vipsLostScore);
			writer.Write("vipsWaitingOutsideScore", _vipsWaitingOutsideScore);
		}
		if (_objectivesNeeded != 0)
		{
			writer.Write("objectivesDestroyed", _objectivesDestroyed);
			writer.Write("objectivesNeeded", _objectivesNeeded);
			writer.Write("objectiveType", _objectiveType);
		}
		writer.Write("width", _mapsize_x);
		writer.Write("length", _mapsize_y);
		writer.Write("height", _mapsize_z);
		writer.Write("missionType", _missionType);
		writer.Write("strTarget", _strTarget);
		writer.Write("strCraftOrBase", _strCraftOrBase).SetAsQuotedAndEscaped();
		if (_startingCondition != null)
		{
			writer.Write("startingConditionType", _startingCondition.Type);
		}
		if (_enviroEffects != null)
		{
			writer.Write("enviroEffectsType", _enviroEffects.Type);
		}
		writer.Write("nameDisplay", _nameDisplay);
		writer.Write("ecEnabledFriendly", _ecEnabledFriendly);
		writer.Write("ecEnabledHostile", _ecEnabledHostile);
		writer.Write("ecEnabledNeutral", _ecEnabledNeutral);
		writer.Write("alienCustomDeploy", _alienCustomDeploy);
		writer.Write("alienCustomMission", _alienCustomMission);
		writer.Write("alienItemLevel", _alienItemLevel);
		writer.Write("lastUsedMapScript", _lastUsedMapScript);
		writer.Write("reinforcementsDeployment", _reinforcementsDeployment);
		writer.Write("reinforcementsRace", _reinforcementsRace);
		writer.Write("reinforcementsItemLevel", _reinforcementsItemLevel);
		writer.Write("reinforcementsMemory", _reinforcementsMemory);
		writer.Write("reinforcementsBlocks", _reinforcementsBlocks);
		writer.Write("flattenedMapTerrainNames", _flattenedMapTerrainNames);
		writer.Write("flattenedMapBlockNames", _flattenedMapBlockNames);
		writer.Write("globalshade", _globalShade);
		writer.Write("turn", _turn);
		writer.Write("bughuntMinTurn", _bughuntMinTurn);
		writer.Write("animFrame", _animFrame);
		writer.Write("bughuntMode", _bughuntMode);
		writer.Write("selectedUnit", (_selectedUnit != null ? _selectedUnit.Id : -1));
		writer.Write("undoUnit", (_undoUnit != null ? _undoUnit.Id : -1));

		writer.Write("mapdatasets", _mapDataSets,
			(w, mds) => w.Write(mds.Name));
		SaveTiles(writer);

		writer.Write("nodes", _nodes,
			(w, n) => n.Save(w.Write()));
		if (_missionType == "STR_BASE_DEFENSE")
			writer.Write("moduleMap", _baseModules);
		writer.Write("units", _units,
			(w, bu) => bu.Save(w.Write(), _rule?.ScriptGlobal));
		writer.Write("items", _items,
			(w, bi) => { if (!bi.IsSpecialWeapon) bi.Save(w.Write(), _rule?.ScriptGlobal); });
		writer.Write("itemsSpecial", _items,
			(w, bi) => { if (bi.IsSpecialWeapon) bi.Save(w.Write(), _rule?.ScriptGlobal); });
		writer.Write("tuReserved", (int)_tuReserved);
		writer.Write("kneelReserved", _kneelReserved);
		writer.Write("depth", _depth);
		writer.Write("ambience", _ambience);
		writer.Write("ambientVolume", _ambientVolume);
		writer.Write("ambienceRandom", _ambienceRandom);
		writer.Write("minAmbienceRandomDelay", _minAmbienceRandomDelay);
		writer.Write("maxAmbienceRandomDelay", _maxAmbienceRandomDelay);
		writer.Write("currentAmbienceDelay", _currentAmbienceDelay);
		writer.Write("recoverGuaranteed", _recoverGuaranteed,
			(w, bi) => bi.Save(w.Write(), _rule?.ScriptGlobal));
		writer.Write("recoverConditional", _recoverConditional,
			(w, bi) => bi.Save(w.Write(), _rule?.ScriptGlobal));
		writer.Write("music", _music);
		_baseItems.Save(writer["baseItems"]);
		writer.Write("turnLimit", _turnLimit);
		writer.Write("chronoTrigger", _chronoTrigger);
		writer.Write("cheatTurn", _cheatTurn);
		writer.Write("togglePersonalLight", _togglePersonalLight);
		writer.Write("toggleNightVision", _toggleNightVision);
		writer.Write("toggleBrightness", _toggleBrightness);
		if (_rule != null)
		{
			_scriptValues.Save(writer, _rule.ScriptGlobal);
		}
	}

	/// <summary>Part of <see cref="Save"/>: the field sizes and the packed binary tile data (base64).</summary>
	internal void SaveTiles(YamlNodeWriter writer)
	{
		// (the C++ "#if 0" text tile variant is not ported)
		var key = Tile.serializationKey;
		// first, write out the field sizes we're going to use to write the tile data
		WriteChar(writer, "tileIndexSize", key.index);
		writer.Write("tileTotalBytesPer", key.totalBytes);
		WriteChar(writer, "tileFireSize", key._fire);
		WriteChar(writer, "tileSmokeSize", key._smoke);
		WriteChar(writer, "tileIDSize", key._mapDataID);
		WriteChar(writer, "tileSetIDSize", key._mapDataSetID);
		WriteChar(writer, "tileBoolFieldsSize", key.boolFields);

		long tileDataSize = (long)key.totalBytes * _mapsize_z * _mapsize_y * _mapsize_x;
		byte[] tileData = new byte[tileDataSize];
		Span<byte> ptr = tileData;

		for (int i = 0; i < _mapsize_z * _mapsize_y * _mapsize_x; ++i)
		{
			if (!_tiles[i].IsVoid)
			{
				SerializationHelper.SerializeInt(ref ptr, key.index, i);
				_tiles[i].SaveBinary(ref ptr);
			}
			else
			{
				tileDataSize -= key.totalBytes;
			}
		}
		writer.Write("totalTiles", tileDataSize / key.totalBytes); // not strictly necessary, just convenient
		writer.WriteBase64("binTiles", tileData.AsSpan(0, (int)tileDataSize));
	}

	/// <summary>Initializes the array of tiles and creates a pathfinding object.</summary>
	public void InitMap(int mapsize_x, int mapsize_y, int mapsize_z, bool resetTerrain = true)
	{
		// Clear old map data
		_nodes.Clear();

		if (resetTerrain)
		{
			_mapDataSets.Clear();
		}

		// Create tile objects
		_mapsize_x = mapsize_x;
		_mapsize_y = mapsize_y;
		_mapsize_z = mapsize_z;

		_tiles = new Tile[_mapsize_z * _mapsize_y * _mapsize_x];
		for (int i = 0; i < _mapsize_z * _mapsize_y * _mapsize_x; ++i)
		{
			_tiles[i] = new Tile(GetTileCoords(i), this);
		}
	}

	/// <summary>Initializes the map utilities.</summary>
	public void InitUtilities(Mod mod, bool craftInventory = false)
	{
		_baseCraftInventory = craftInventory;
		_pathfinding = craftInventory ? null : new Pathfinding(this);
		_tileEngine = new TileEngine(this, mod);
	}

	/// <summary>Gets if this is craft pre-equip phase in base view.</summary>
	public bool IsBaseCraftInventory => _baseCraftInventory;

	/// <summary>Gets the game's mapdata files.</summary>
	public List<MapDataSet> MapDataSets => _mapDataSets;

	/// <summary>Gets/sets the mission type.</summary>
	public string MissionType { get => _missionType; set => _missionType = value; }

	/// <summary>Gets/sets the mission target.</summary>
	public string MissionTarget { get => _strTarget; set => _strTarget = value; }

	/// <summary>Gets/sets the mission craft/base.</summary>
	public string MissionCraftOrBase { get => _strCraftOrBase; set => _strCraftOrBase = value; }

	/// <summary>
	/// Returns the list of items in the base storage rooms BEFORE the mission.
	/// Does NOT return items assigned to craft or in transfer.
	/// </summary>
	public ItemContainer BaseStorageItems => _baseItems;

	/// <summary>Gets/sets the starting conditions.</summary>
	public RuleStartingCondition StartingCondition { get => _startingCondition; set => _startingCondition = value; }

	/// <summary>Applies the enviro effects.</summary>
	public void ApplyEnviroEffects(RuleEnviroEffects enviroEffects)
	{
		_enviroEffects = enviroEffects;

		_ecEnabledFriendly = false;
		_ecEnabledHostile = false;
		_ecEnabledNeutral = false;

		if (_enviroEffects != null)
		{
			_ecEnabledFriendly = Rng.Percent(_enviroEffects.GetEnvironmetalCondition("STR_FRIENDLY").globalChance);
			_ecEnabledHostile = Rng.Percent(_enviroEffects.GetEnvironmetalCondition("STR_HOSTILE").globalChance);
			_ecEnabledNeutral = Rng.Percent(_enviroEffects.GetEnvironmetalCondition("STR_NEUTRAL").globalChance);
		}
	}

	/// <summary>Gets the enviro effects.</summary>
	public RuleEnviroEffects EnviroEffects => _enviroEffects;

	/// <summary>Are environmental conditions (for a given faction) enabled?</summary>
	public bool GetEnvironmentalConditionsEnabled(UnitFaction faction)
	{
		if (faction == UnitFaction.FACTION_PLAYER)
		{
			return _ecEnabledFriendly;
		}
		else if (faction == UnitFaction.FACTION_HOSTILE)
		{
			return _ecEnabledHostile;
		}
		else if (faction == UnitFaction.FACTION_NEUTRAL)
		{
			return _ecEnabledNeutral;
		}
		return false;
	}

	/// <summary>Sets the custom alien data.</summary>
	public void SetAlienCustom(string deploy, string mission)
	{
		_alienCustomDeploy = deploy;
		_alienCustomMission = mission;
	}

	/// <summary>Gets the custom alien deploy.</summary>
	public string AlienCustomDeploy => _alienCustomDeploy;

	/// <summary>Gets the custom mission definition.</summary>
	public string AlienCustomMission => _alienCustomMission;

	/// <summary>Gets/sets the last used map script.</summary>
	public string LastUsedMapScript { get => _lastUsedMapScript; set => _lastUsedMapScript = value; }

	/// <summary>Gets/sets the alien item level.</summary>
	public int AlienItemLevel { get => _alienItemLevel; set => _alienItemLevel = value; }

	/// <summary>Gets/sets the alien deployment to use for reinforcements.</summary>
	public string ReinforcementsDeployment { get => _reinforcementsDeployment; set => _reinforcementsDeployment = value; }

	/// <summary>Gets/sets the alien race to use for reinforcements.</summary>
	public string ReinforcementsRace { get => _reinforcementsRace; set => _reinforcementsRace = value; }

	/// <summary>Gets/sets the alien item level to use for reinforcements.</summary>
	public int ReinforcementsItemLevel { get => _reinforcementsItemLevel; set => _reinforcementsItemLevel = value; }

	/// <summary>Gets the memory used for reinforcements.</summary>
	public SortedDictionary<string, int> ReinforcementsMemory => _reinforcementsMemory;

	/// <summary>Gets the map blocks used for reinforcements.</summary>
	public List<List<int>> ReinforcementsBlocks => _reinforcementsBlocks;

	/// <summary>Gets the flattened map block terrain names (or craft/ufo names) generated by the BattlescapeGenerator.</summary>
	public List<List<string>> FlattenedMapTerrainNames => _flattenedMapTerrainNames;

	/// <summary>Gets the flattened map block names generated by the BattlescapeGenerator.</summary>
	public List<List<string>> FlattenedMapBlockNames => _flattenedMapBlockNames;

	/// <summary>Gets/sets the global shade.</summary>
	public int GlobalShade { get => _globalShade; set => _globalShade = value; }

	/// <summary>Gets the list of nodes.</summary>
	public List<Node> Nodes => _nodes;

	/// <summary>Gets the list of items.</summary>
	public List<BattleItem> Items => _items;

	/// <summary>Gets the list of units.</summary>
	public List<BattleUnit> Units => _units;

	/// <summary>Gets terrain size x.</summary>
	public int MapSizeX => _mapsize_x;

	/// <summary>Gets terrain size y.</summary>
	public int MapSizeY => _mapsize_y;

	/// <summary>Gets terrain size z.</summary>
	public int MapSizeZ => _mapsize_z;

	/// <summary>Gets terrain x*y*z.</summary>
	public int MapSizeXYZ => _mapsize_x * _mapsize_y * _mapsize_z;

	/// <summary>Is this just a craft or base deployment preview?</summary>
	public bool IsPreview => _isPreview;

	/// <summary>Sets craft position.</summary>
	public void SetCraftPos(SDL_Rect craftPos)
	{
		_craftPos = craftPos;
	}

	/// <summary>Sets craft elevation.</summary>
	public void SetCraftZ(int craftZ)
	{
		_craftZ = craftZ;
	}

	/// <summary>Gets/sets craft for preview.</summary>
	public Craft CraftForPreview { get => _craftForPreview; set => _craftForPreview = value; }

	/// <summary>Pre-calculate all valid tiles for later use in map drawing.</summary>
	public void CalculateCraftTiles()
	{
		if (_craftForPreview != null && _craftForPreview.Rules.Deployment.Count != 0 && !_craftForPreview.Rules.UseAllStartTiles())
		{
			foreach (var vec in _craftForPreview.Rules.Deployment)
			{
				if (vec.Count >= 3)
				{
					Position tmp = new Position(vec[0] + _craftPos.x * 10, vec[1] + _craftPos.y * 10, vec[2] + _craftZ);
					_craftTiles.Add(tmp);
				}
			}
		}
		else
		{
			for (int i = 0; i < MapSizeXYZ; ++i)
			{
				var tile = GetTile(i);
				if (tile != null &&
					tile.FloorSpecialTileType == SpecialTileType.START_POINT &&
					tile.GetMapData(TilePart.O_OBJECT) == null &&
					tile.GetMapData(TilePart.O_FLOOR) != null && // for clarity this is checked again, first time was in `getFloorSpecialTileType`
					tile.GetMapData(TilePart.O_FLOOR).GetTUCost(MovementType.MT_WALK) != Pathfinding.INVALID_MOVE_COST)
				{
					_craftTiles.Add(tile.Position);
				}
			}
		}
	}

	/// <summary>Gets craft tiles.</summary>
	public List<Position> CraftTiles => _craftTiles;

	/// <summary>Converts coordinates into a unique index.</summary>
	public int GetTileIndex(Position pos) => pos.z * _mapsize_y * _mapsize_x + pos.y * _mapsize_x + pos.x;

	/// <summary>Converts a tile index to coordinates.</summary>
	public Position GetTileCoords(int index)
	{
		Position p;
		p.z = unchecked((short)(index / (_mapsize_y * _mapsize_x)));
		p.y = unchecked((short)((index % (_mapsize_y * _mapsize_x)) / _mapsize_x));
		p.x = unchecked((short)((index % (_mapsize_y * _mapsize_x)) % _mapsize_x));
		return p;
	}

	/// <summary>Gets the Tile at a given position on the map, or null outside the map.</summary>
	public Tile GetTile(Position pos)
	{
		if (pos.x < 0 || pos.y < 0 || pos.z < 0
			|| pos.x >= _mapsize_x || pos.y >= _mapsize_y || pos.z >= _mapsize_z)
			return null;

		return _tiles[GetTileIndex(pos)];
	}

	/// <summary>Gets the tile at an index, less than <see cref="MapSizeXYZ"/>.</summary>
	public Tile GetTile(int i) => _tiles[i];

	/// <summary>Get tile that is below current one.</summary>
	public Tile GetBelowTile(Tile tile)
	{
		if (tile == null || tile.Position.z == 0)
		{
			return null;
		}
		// difference of indices between layers is equal `_mapsize_y * _mapsize_x`
		return _tiles[GetTileIndex(tile.Position) - _mapsize_y * _mapsize_x];
	}

	/// <summary>Get tile that is over current one.</summary>
	public Tile GetAboveTile(Tile tile)
	{
		if (tile == null || tile.Position.z == _mapsize_z - 1)
		{
			return null;
		}
		// difference of indices between layers is equal `_mapsize_y * _mapsize_x`
		return _tiles[GetTileIndex(tile.Position) + _mapsize_y * _mapsize_x];
	}

	/// <summary>Gets/sets the currently selected unit.</summary>
	public BattleUnit SelectedUnit { get => _selectedUnit; set => _selectedUnit = value; }

	/// <summary>Gets/sets the "undo" unit.</summary>
	public BattleUnit UndoUnit { get => _undoUnit; set => _undoUnit = value; }

	/// <summary>Clear state that given unit is selected.</summary>
	public void ClearUnitSelection(BattleUnit unit)
	{
		if (_selectedUnit == unit)
		{
			_selectedUnit = null;
		}
		if (_undoUnit == unit)
		{
			_undoUnit = null;
		}
		if (_lastSelectedUnit == unit)
		{
			_lastSelectedUnit = null;
		}
	}

	/// <summary>Selects the previous player unit.</summary>
	public BattleUnit SelectPreviousPlayerUnit(bool checkReselect = false, bool setReselect = false, bool checkInventory = false)
		=> SelectPlayerUnit(-1, checkReselect, setReselect, checkInventory);

	/// <summary>Selects the next player unit.</summary>
	public BattleUnit SelectNextPlayerUnit(bool checkReselect = false, bool setReselect = false, bool checkInventory = false)
		=> SelectPlayerUnit(+1, checkReselect, setReselect, checkInventory);

	/// <summary>Selects the next player unit in a certain direction.</summary>
	/// <param name="dir">Direction to select, eg. -1 for previous and 1 for next.</param>
	/// <returns>New selected BattleUnit, null if none can be selected.</returns>
	private BattleUnit SelectPlayerUnit(int dir, bool checkReselect = false, bool setReselect = false, bool checkInventory = false)
	{
		if (_selectedUnit != null && setReselect)
		{
			_selectedUnit.DontReselect();
		}
		if (_units.Count == 0)
		{
			return null;
		}

		// iterators as indices; _units.Count is end()
		int begin = 0, end = 0;
		if (dir > 0)
		{
			begin = 0;
			end = _units.Count - 1;
		}
		else if (dir < 0)
		{
			begin = _units.Count - 1;
			end = 0;
		}

		int i = _units.IndexOf(_selectedUnit);
		if (i < 0)
			i = _units.Count;
		do
		{
			// no unit selected
			if (i == _units.Count)
			{
				i = begin;
				continue;
			}
			if (i != end)
			{
				i += dir;
			}
			// reached the end, wrap-around
			else
			{
				i = begin;
			}
			// back to where we started... no more units found
			if (_units[i] == _selectedUnit)
			{
				if (checkReselect && !_selectedUnit.ReselectAllowed())
					_selectedUnit = null;
				return _selectedUnit;
			}
			else if (_selectedUnit == null && i == begin)
			{
				return _selectedUnit;
			}
		}
		while (!_units[i].IsSelectable(_side, checkReselect, checkInventory));

		_selectedUnit = _units[i];
		return _selectedUnit;
	}

	/// <summary>Selects the next closest player unit.</summary>
	/// <returns>New selected BattleUnit, null if none can be selected.</returns>
	public BattleUnit SelectNextPlayerUnitByDistance(bool checkReselect = false, bool setReselect = false, bool checkInventory = false)
	{
		BattleUnit backup = _selectedUnit;
		if (_selectedUnit != null && setReselect)
		{
			_selectedUnit.DontReselect();
			_selectedUnit = null;
		}

		var candidates = new List<(int first, BattleUnit second)>();
		foreach (var unit in _units)
		{
			if (unit != _selectedUnit && unit.IsSelectable(_side, checkReselect, checkInventory))
			{
				int distance = backup != null ? backup.Distance3dToUnitSq(unit) : 0;
				candidates.Add((distance, unit));
			}
		}

		if (candidates.Count != 0)
		{
			// std::sort is not stable; for the usual few units (<= 16) libstdc++ does an insertion sort, which keeps equal distances in order
			var sorted = candidates.OrderBy(c => c.first).ToList();

			_selectedUnit = sorted[0].second;
		}
		return _selectedUnit;
	}

	/// <summary>Selects the unit at the given position on the map, or null when none is found.</summary>
	public BattleUnit SelectUnit(Position pos)
	{
		BattleUnit bu = GetTile(pos).Unit;

		if (bu != null && bu.IsOut)
		{
			return null;
		}
		else
		{
			return bu;
		}
	}

	/// <summary>Gets the pathfinding object.</summary>
	public Pathfinding Pathfinding => _pathfinding;

	/// <summary>Gets the terrain modifier object.</summary>
	public TileEngine TileEngine => _tileEngine;

	/// <summary>Gets the side currently playing.</summary>
	public UnitFaction Side => _side;

	/// <summary>Test if weapon is usable by unit.</summary>
	public bool CanUseWeapon(BattleItem weapon, BattleUnit unit, bool isBerserking, BattleActionType actionType)
	{
		string message = null;
		return CanUseWeapon(weapon, unit, isBerserking, actionType, ref message);
	}

	/// <summary>Test if weapon is usable by unit.</summary>
	/// <param name="message">Set to the reason if the weapon can not be used (only for some reasons; else unchanged).</param>
	public bool CanUseWeapon(BattleItem weapon, BattleUnit unit, bool isBerserking, BattleActionType actionType, ref string message)
	{
		if (weapon == null || unit == null) return false;

		RuleItem rule = weapon.Rules;

		BattleItem ammoItem;
		if (actionType != BattleActionType.BA_NONE)
		{
			// Applies to:
			// 1. action type selected by the player from the UI
			// 2. leeroy jenkins AI
			// 3. all reaction fire
			// 4. all unit berserking
			ammoItem = weapon.GetAmmoForActionConst(actionType);
		}
		else
		{
			// Applies to:
			// 5. standard AI - action type (and thus ammoItem) is unknown when the check is done
			ammoItem = null;
		}

		if (unit.Faction == UnitFaction.FACTION_HOSTILE && Turn < rule.GetAIUseDelay(Mod))
		{
			return false;
		}
		if (unit.OriginalFaction == UnitFaction.FACTION_PLAYER && !GeoscapeSave.IsResearched(rule.Requirements))
		{
			return false;
		}
		if (rule.IsPsiRequired && unit.BaseStats.psiSkill <= 0)
		{
			return false;
		}
		if (rule.IsManaRequired && unit.OriginalFaction == UnitFaction.FACTION_PLAYER)
		{
			if (!_rule.IsManaFeatureEnabled || !GeoscapeSave.IsManaUnlocked(_rule))
			{
				return false;
			}
		}
		if (Depth == 0)
		{
			if (rule.IsWaterOnly || (ammoItem != null && ammoItem.Rules.IsWaterOnly))
			{
				message = "STR_UNDERWATER_EQUIPMENT";
				return false;
			}
		}
		else // if (getDepth() != 0)
		{
			if (rule.IsLandOnly || (ammoItem != null && ammoItem.Rules.IsLandOnly))
			{
				message = "STR_LAND_EQUIPMENT";
				return false;
			}
		}
		if (rule.IsBlockingBothHands && unit.Faction == UnitFaction.FACTION_PLAYER && !isBerserking && unit.LeftHandWeapon != null && unit.RightHandWeapon != null)
		{
			message = "STR_MUST_USE_BOTH_HANDS";
			return false;
		}
		return true;
	}

	/// <summary>Gets the current turn number.</summary>
	public int Turn => _turn;

	/// <summary>Gets/sets the bug hunt turn number.</summary>
	public int BughuntMinTurn { get => _bughuntMinTurn; set => _bughuntMinTurn = value; }

	/// <summary>Start first turn of battle.</summary>
	public void StartFirstTurn()
	{
		// this should be first tile with all items, even if unit is in reality on other tile.
		Tile inventoryTile = SelectedUnit.Tile;

		RandomizeItemLocations(inventoryTile);

		ResetUnitTiles();

		// check what unit is still on this tile after reset.
		if (inventoryTile.Unit != null)
		{
			// make sure we select the unit closest to the ramp.
			SelectedUnit = inventoryTile.Unit;
		}

		// initialize xcom units for battle
		foreach (var bu in Units)
		{
			if (bu.OriginalFaction != UnitFaction.FACTION_PLAYER || bu.IsOut)
			{
				continue;
			}

			bu.PrepareNewTurn(false);
		}

		_turn = 1;

		NewTurnUpdateScripts();
	}

	/// <summary>Scripts that are run at begining of new turn.</summary>
	private void NewTurnUpdateScripts()
	{
		if (_isPreview)
		{
			return;
		}

		foreach (var bu in _units)
		{
			if (bu.IsIgnored)
			{
				continue;
			}

			ModScript.ScriptCallback(bu.Armor.GetScript(ModScript.NewTurnUnit), bu, this, Turn, (int)_side);
		}

		foreach (var item in _items)
		{
			if (item.IsOwnerIgnored)
			{
				continue;
			}

			ModScript.ScriptCallback(item.Rules.GetScriptEvents(ModScript.NewTurnItem), item, this, Turn, (int)_side);
		}

		ReviveUnconsciousUnits(false);

		BattleGame.ConvertInfected();
	}

	/// <summary>Tallies the units in the craft deployment preview.</summary>
	public BattlescapeTally TallyUnitsForPreview()
	{
		BattlescapeTally tally = default;

		bool custom = _isPreview && _craftForPreview != null && _craftTiles.Count != 0;
		Position tmp;

		foreach (var unit in _units)
		{
			if (unit.OriginalFaction == UnitFaction.FACTION_PLAYER)
			{
				if (unit.IsSummonedPlayerUnit)
				{
					continue;
				}
				if (custom)
				{
					bool placementOk = true;
					for (int x = 0; x < unit.Armor.Size; ++x)
					{
						for (int y = 0; y < unit.Armor.Size; ++y)
						{
							tmp = new Position(x + unit.Position.x, y + unit.Position.y, unit.Position.z);
							bool found = false;
							foreach (var pos in _craftTiles)
							{
								if (pos == tmp)
								{
									found = true;
									break;
								}
							}
							if (!found)
							{
								placementOk = false;
							}
						}
					}
					if (placementOk)
					{
						tally.inEntrance++;
					}
					else
					{
						tally.inField++;
					}
				}
				else
				{
					if (unit.IsInExitArea(SpecialTileType.START_POINT))
					{
						tally.inEntrance++;
					}
					else
					{
						tally.inField++;
					}
				}
			}
		}

		return tally;
	}

	/// <summary>Saves the custom craft deployment.</summary>
	public void SaveCustomCraftDeployment()
	{
		_craftForPreview.ResetCustomDeployment();
		var customSoldierDeployment = _craftForPreview.CustomSoldierDeployment;
		var customVehicleDeployment = _craftForPreview.CustomVehicleDeployment;

		Position tmp;
		foreach (var unit in _units)
		{
			if (unit.OriginalFaction == UnitFaction.FACTION_PLAYER)
			{
				if (unit.IsSummonedPlayerUnit)
				{
					continue;
				}
				tmp = new Position(unit.Position.x - _craftPos.x * 10, unit.Position.y - _craftPos.y * 10, unit.Position.z - _craftZ);
				if (unit.GeoscapeSoldier != null)
				{
					customSoldierDeployment[unit.GeoscapeSoldier.Id] = (tmp, unit.Direction);
				}
				else
				{
					VehicleDeploymentData v = new VehicleDeploymentData();
					v.type = unit.Type;
					v.pos = tmp;
					v.dir = unit.Direction;
					v.used = false; // irrelevant now, this will be used only in BattlescapeGenerator::addXCOMUnit()
					customVehicleDeployment.Add(v);
				}
			}
		}
	}

	/// <summary>Saves the custom RuleCraft deployment. Invalidates corresponding custom craft deployments.</summary>
	public void SaveDummyCraftDeployment()
	{
		var save = GeoscapeSave;

		// don't forget to invalidate custom deployments of all real craft of this type
		foreach (var xbase in save.Bases)
		{
			foreach (var xcraft in xbase.Crafts)
			{
				if (xcraft.Rules == _craftForPreview.Rules)
				{
					xcraft.ResetCustomDeployment();
				}
			}
		}

		var data = save.CustomRuleCraftDeployments;

		if (IsCtrlPressed(true))
		{
			// delete
			data.Remove(_craftForPreview.Rules.Type);
		}
		else
		{
			// save
			var customDeployment = new List<List<int>>();
			Position tmp;
			foreach (var unit in _units)
			{
				if (unit.OriginalFaction == UnitFaction.FACTION_PLAYER)
				{
					if (unit.IsSummonedPlayerUnit)
					{
						continue;
					}
					tmp = new Position(unit.Position.x - _craftPos.x * 10, unit.Position.y - _craftPos.y * 10, unit.Position.z - _craftZ);
					if (unit.GeoscapeSoldier != null)
					{
						customDeployment.Add([tmp.x, tmp.y, tmp.z, unit.Direction]);
					}
				}
			}
			data[_craftForPreview.Rules.Type] = customDeployment;
		}
	}

	/// <summary>Does the given craft type have a custom deployment?</summary>
	public bool HasCustomDeployment(RuleCraft rule)
	{
		ArgumentNullException.ThrowIfNull(rule);
		// first check the fallback
		if (rule.Deployment.Count != 0)
		{
			return true;
		}
		// then the override
		var save = GeoscapeSave;
		var data = save.CustomRuleCraftDeployments;
		if (data.Count != 0)
		{
			if (data.ContainsKey(rule.Type))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Gets a custom deployment for the given craft type.</summary>
	public List<List<int>> GetCustomDeployment(RuleCraft rule)
	{
		ArgumentNullException.ThrowIfNull(rule);
		// first try the override
		var save = GeoscapeSave;
		var data = save.CustomRuleCraftDeployments;
		if (data.Count != 0)
		{
			if (data.TryGetValue(rule.Type, out var found))
			{
				return found;
			}
		}
		// this is the fallback
		return rule.Deployment;
	}

	/// <summary>Ends the current turn and progresses to the next one.</summary>
	public void EndTurn()
	{
		// reset turret direction for all hostile and neutral units (as it may have been changed during reaction fire)
		foreach (var bu in _units)
		{
			if (bu.OriginalFaction != UnitFaction.FACTION_PLAYER)
			{
#pragma warning disable CA2245 // C++ setDirection(getDirection()): the setter sets more than just the direction
				bu.Direction = bu.Direction; // this is not pointless, the method sets more than just the direction
#pragma warning restore CA2245
			}
		}

		if (_side == UnitFaction.FACTION_PLAYER)
		{
			if (_selectedUnit != null && _selectedUnit.OriginalFaction == UnitFaction.FACTION_PLAYER)
				_lastSelectedUnit = _selectedUnit;
			else
				_lastSelectedUnit = null;
			_selectedUnit = null;
			_undoUnit = null;
			_side = UnitFaction.FACTION_HOSTILE;
		}
		else if (_side == UnitFaction.FACTION_HOSTILE)
		{
			_selectedUnit = null;
			_undoUnit = null;
			_side = UnitFaction.FACTION_NEUTRAL;
			// if there is no neutral team, we skip this and instantly prepare the new turn for the player
			// (C++ keeps that variant commented out)
		}
		else if (_side == UnitFaction.FACTION_NEUTRAL)
		{
			PrepareNewTurn();
			_turn++;
			_side = UnitFaction.FACTION_PLAYER;
			if (_lastSelectedUnit != null && _lastSelectedUnit.IsSelectable(UnitFaction.FACTION_PLAYER, false, false))
				_selectedUnit = _lastSelectedUnit;
			else
				SelectNextPlayerUnit();
			while (_selectedUnit != null && _selectedUnit.Faction != UnitFaction.FACTION_PLAYER)
				SelectNextPlayerUnit();

			_undoUnit = null;
			_lastSelectedUnit = null;
		}

		BattlescapeTally tally = _battleState.BattleGame.TallyUnits();

		if ((_turn > _cheatTurn / 2 && tally.liveAliens <= 2) || _turn > _cheatTurn)
		{
			_cheating = true;
		}

		if (_side == UnitFaction.FACTION_PLAYER)
		{
			// update the "number of turns since last spotted" and the number of turns left sniper AI knows about player units
			foreach (var bu in _units)
			{
				if (bu.IsIgnored)
				{
					continue;
				}

				bu.UpdateTurnsSince();
				if (_cheating && bu.Faction == UnitFaction.FACTION_PLAYER && !bu.IsOut)
				{
					bu.TurnsSinceSpotted = 0;
				}
				bu.AIModule?.Reset(); // clean up AI state
			}
		}

		// hide all aliens (VOF calculations below will turn them visible again)
		foreach (var bu in _units)
		{
			if (bu.IsIgnored)
			{
				continue;
			}

			// Disclaimer: hardcoded!
			if (bu.Faction == UnitFaction.FACTION_NEUTRAL && bu.OriginalFaction == UnitFaction.FACTION_HOSTILE)
			{
				if (_side != UnitFaction.FACTION_PLAYER)
				{
					// a hostile converted to a neutral during the player turn, just keep them mostly "as is" during the hostile/neutral turn...
					if (_side == UnitFaction.FACTION_HOSTILE)
					{
						bu.UpdateUnitStats(false, true);
					}
					bu.Visible = false;
				}
				else
				{
					// ...and convert them back at the beginning of the player turn
					bu.PrepareNewTurn();

					// don't forget to rewire them back to attack the player again
					bu.AIModule?.SetTargetFaction(UnitFaction.FACTION_PLAYER);
				}
				// skip the standard logic
				continue;
			}

			if (bu.Faction == _side)
			{
				bu.PrepareNewTurn();
			}
			else if (bu.OriginalFaction == _side)
			{
				bu.UpdateUnitStats(false, true);
			}
			if (bu.Faction != UnitFaction.FACTION_PLAYER)
			{
				bu.Visible = false;
			}
		}

		//scripts update
		NewTurnUpdateScripts();

		//fov check will be done by `BattlescapeGame::endTurn`

		if (_side != UnitFaction.FACTION_PLAYER)
			SelectNextPlayerUnit();
	}

	/// <summary>Get current animation frame number.</summary>
	public int AnimFrame => _animFrame;

	/// <summary>Increase animation frame with wrap around 705600.</summary>
	public void NextAnimFrame()
	{
		_animFrame = (_animFrame + 1) % (64 * 3 * 3 * 5 * 5 * 7 * 7);
	}

	/// <summary>Turns on debug mode.</summary>
	public void SetDebugMode()
	{
		RevealMap();

		_debugMode = true;
	}

	public void RevealMap()
	{
		for (int i = 0; i < _mapsize_z * _mapsize_y * _mapsize_x; ++i)
		{
			_tiles[i].SetDiscovered(true, TilePart.O_FLOOR);
		}
	}

	/// <summary>Gets the current debug mode.</summary>
	public bool DebugMode => _debugMode;

	/// <summary>Gets/sets the bug hunt mode.</summary>
	public bool BughuntMode { get => _bughuntMode; set => _bughuntMode = value; }

	/// <summary>Resets all the units to their current standing tile(s).</summary>
	public void ResetUnitTiles()
	{
		foreach (var bu in _units)
		{
			if (!bu.IsOut)
			{
				bu.SetTile(GetTile(bu.Position), this);
			}
			if (bu.Faction == UnitFaction.FACTION_PLAYER)
			{
				bu.Visible = true;
			}
		}
		_beforeGame = false;
	}

	/// <summary>Add item to delete list, usually when removing item from game or build in weapons.</summary>
	public void DeleteList(BattleItem item)
	{
		_deleted.Add(item);
	}

	/// <summary>Removes an item from the game. Eg. when ammo item is depleted.</summary>
	public void RemoveItem(BattleItem item)
	{
		ArgumentNullException.ThrowIfNull(item);
		static bool purge(List<BattleItem> inventory, BattleItem forDelete)
		{
			int i = inventory.IndexOf(forDelete);
			if (i >= 0)
			{
				inventory.RemoveAt(i);
				return true;
			}
			return false;
		}

		if (item.IsSpecialWeapon)
		{
			// we cannot remove it because load() would create a new one
			// only when a unit is killed or set to "timeout", we can remove its items.
			return;
		}

		if (!purge(_items, item))
		{
			return;
		}

		// due to strange design, the item has to be removed from the tile it is on too (if it is on a tile)
		item.MoveToOwner(null);

		DeleteList(item);

		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			var ammo = item.GetAmmoForSlot(slot);
			if (ammo != null && ammo != item)
			{
				if (purge(_items, ammo))
				{
					DeleteList(ammo);
				}
			}
		}
	}

	/// <summary>Add built-in items from list to unit.</summary>
	public void AddFixedItems(BattleUnit unit, List<RuleItem> @fixed)
	{
		ArgumentNullException.ThrowIfNull(@fixed);
		if (@fixed.Count != 0)
		{
			var ammo = new List<RuleItem>();
			foreach (var ruleItem in @fixed)
			{
				if (ruleItem != null)
				{
					if (ruleItem.BattleType == BattleType.BT_AMMO)
					{
						ammo.Add(ruleItem);
						continue;
					}
					CreateItemForUnit(ruleItem, unit, true);
				}
			}
			foreach (var ruleItem in ammo)
			{
				CreateItemForUnit(ruleItem, unit, true);
			}
		}
	}

	/// <summary>Create all fixed items for new created unit.</summary>
	public void InitUnit(BattleUnit unit, int itemLevel = 0)
	{
		ArgumentNullException.ThrowIfNull(unit);
		if (_isPreview)
		{
			return;
		}

		unit.SetSpecialWeapon(this, false);
		Unit rule = unit.UnitRules;
		Armor armor = unit.Armor;
		// Built in weapons: the unit has this weapon regardless of loadout or what have you.
		AddFixedItems(unit, armor.BuiltInWeapons);

		// For aliens and HWP
		if (rule != null)
		{
			var buildin = rule.BuiltInWeapons;
			if (buildin.Count != 0)
			{
				int idx = itemLevel >= buildin.Count ? buildin.Count - 1 : itemLevel;
				// Built in weapons: the unit has this weapon regardless of loadout or what have you.
				AddFixedItems(unit, buildin[idx]);
			}

			var buildin2 = rule.WeightedBuiltInWeapons;
			if (buildin2.Count != 0)
			{
				int idx2 = itemLevel >= buildin2.Count ? buildin2.Count - 1 : itemLevel;
				var weights = buildin2[idx2];
				var weaponSetRule = _rule.GetWeaponSet(weights.Choose());
				AddFixedItems(unit, weaponSetRule.Weapons);
			}

			// terrorist alien's equipment is a special case - they are fitted with a weapon which is the alien's name with suffix _WEAPON
			if (rule.IsLivingWeapon)
			{
				string terroristWeapon = rule.Race.Substring(4);
				terroristWeapon += "_WEAPON";
				RuleItem ruleItem = _rule.GetItem(terroristWeapon);
				if (ruleItem != null)
				{
					BattleItem item = CreateItemForUnit(ruleItem, unit);
					if (item != null)
					{
						unit.TurretType = item.Rules.TurretType;
					}
				}
			}
		}

		ModScript.ScriptCallback(armor.GetScript(ModScript.CreateUnit), unit, this, Turn);

		var solder = unit.GeoscapeSoldier;
		if (solder != null)
		{
			foreach (var bonus in solder.GetBonuses(null))
			{
				ModScript.ScriptCallback(bonus.GetScript(ModScript.ApplySoldierBonuses), unit, this, bonus);
			}
		}
	}

	/// <summary>Init new created item.</summary>
	public void InitItem(BattleItem item, BattleUnit unit = null)
	{
		if (_isPreview)
		{
			return;
		}

		ModScript.ScriptCallback(item.Rules.GetScriptEvents(ModScript.CreateItem), item, unit, this, Turn);
	}

	/// <summary>Create new item for unit.</summary>
	public BattleItem CreateItemForUnit(RuleItem rule, BattleUnit unit, bool fixedWeapon = false)
	{
		ArgumentNullException.ThrowIfNull(unit);
		if (_isPreview)
		{
			return null;
		}

		BattleItem item = new BattleItem(rule, ref _itemId);
		if (!unit.AddItem(item, _rule, false, fixedWeapon, fixedWeapon))
		{
			item = null;
		}
		else
		{
			_items.Add(item);
			InitItem(item, unit);
		}
		return item;
	}

	/// <summary>Create new special built-in item for unit.</summary>
	public BattleItem CreateItemForUnitSpecialBuiltin(RuleItem rule, BattleUnit unit)
	{
		if (_isPreview)
		{
			return null;
		}

		BattleItem item = new BattleItem(rule, ref _itemId);
		item.Owner = unit;
		item.Slot = null;
		_items.Add(item);
		InitItem(item, unit);
		return item;
	}

	/// <summary>Create new item for tile.</summary>
	public BattleItem CreateItemForTile(string type, Tile tile) => CreateItemForTile(_rule.GetItem(type, true), tile);

	/// <summary>Create new item for tile.</summary>
	public BattleItem CreateItemForTile(RuleItem rule, Tile tile, BattleUnit corpseFor = null)
	{
		// Note: this is allowed also in preview mode; for items spawned from map blocks (and friendly units spawned from such items)

		BattleItem item = new BattleItem(rule, ref _itemId);
		if (tile != null)
		{
			RuleInventory ground = _rule.InventoryGround;
			tile.AddItem(item, ground);
		}
		item.Unit = corpseFor;
		_items.Add(item);
		InitItem(item);
		return item;
	}

	/// <summary>Create new temporary item. Item is not added to unit list and is not initialized fully.</summary>
	public BattleItem CreateTempItem(RuleItem rule) => new BattleItem(rule, ref _itemId);

	/// <summary>Create new temporary unit. Unit is not added to unit list and is not initialized fully.</summary>
	/// <param name="rules">Unit config</param>
	/// <param name="faction">Faction of unit</param>
	/// <param name="nextUnitId">Optional next id of unit, if not given then it try get value based on last unit.</param>
	public BattleUnit CreateTempUnit(Unit rules, UnitFaction faction, int nextUnitId = -1)
	{
		ArgumentNullException.ThrowIfNull(rules);
		BattleUnit newUnit = new BattleUnit(
			Mod,
			rules,
			faction,
			nextUnitId > 0 ? nextUnitId : Units[^1].Id + 1,
			EnviroEffects,
			rules.Armor,
			faction == UnitFaction.FACTION_HOSTILE ? _rule.GetStatAdjustment((int)GeoscapeSave.Difficulty) : null,
			Depth,
			StartingCondition);

		if (faction == UnitFaction.FACTION_PLAYER)
		{
			//Tanks are created with predefined `nextUnitId` value, if we do not have this value, its summoned unit.
			if (nextUnitId <= 0)
			{
				newUnit.SetSummonedPlayerUnit(true);
			}
		}
		else
		{
			newUnit.AIModule = new AIModule(this, newUnit, null);
		}

		return newUnit;
	}

	/// <summary>Converts a unit into a unit of another type.</summary>
	/// <returns>The new unit.</returns>
	public BattleUnit ConvertUnit(BattleUnit unit)
	{
		ArgumentNullException.ThrowIfNull(unit);
		// only ever respawn once
		unit.AlreadyRespawned = true;

		bool visible = unit.Visible;

		ClearUnitSelection(unit);

		// in case the unit was unconscious
		RemoveUnconsciousBodyItem(unit);

		unit.InstaKill();

		var tile = unit.Tile;
		if (tile == null)
		{
			Position pos = unit.Position;
			if (pos != TileEngine.invalid)
			{
				tile = GetTile(pos);
			}
		}

		// in case of unconscious unit someone could stand on top of it, or take current unit to inventory, then we skip spawning anything
		if (tile == null || (tile.Unit != null && tile.Unit != unit))
		{
			return null;
		}

		TileEngine.ItemDropInventory(tile, unit, false, true);

		// remove unit-tile link
		unit.SetTile(null, this);

		Unit type = unit.SpawnUnit;

		BattleUnit newUnit = CreateTempUnit(type, unit.SpawnUnitFaction);
		newUnit.SpawnUnitFaction = unit.SpawnUnitFaction; // allied/neutral zombie should spawn an allied/neutral chryssalid?

		newUnit.ClearTimeUnits();
		newUnit.Visible = visible;
		newUnit.SetTile(tile, this);
		newUnit.SetPosition(unit.Position);
		newUnit.Direction = unit.Direction;
		Units.Add(newUnit);
		InitUnit(newUnit);

		TileEngine.CalculateFOV(newUnit.Position);  //happens fairly rarely, so do a full recalc for units in range to handle the potential unit visible cache issues.
		TileEngine.ApplyGravity(newUnit.Tile);
		newUnit.DontReselect();
		return newUnit;
	}

	/// <summary>Sets whether the mission was aborted or successful.</summary>
	public void SetAborted(bool flag)
	{
		_aborted = flag;
	}

	/// <summary>Returns whether the mission was aborted or successful.</summary>
	public bool IsAborted => _aborted;

	/// <summary>Sets the number of objectives to be destroyed.</summary>
	public void SetObjectiveCount(int counter)
	{
		_objectivesNeeded = counter;
		_objectivesDestroyed = 0;
	}

	/// <summary>Increments the objective counter.</summary>
	public void AddDestroyedObjective()
	{
		if (!AllObjectivesDestroyed())
		{
			_objectivesDestroyed++;
			if (AllObjectivesDestroyed())
			{
				if (ObjectiveType == SpecialTileType.MUST_DESTROY)
				{
					_battleState.BattleGame.AutoEndBattle();
				}
				else
				{
					_battleState.BattleGame.MissionComplete();
				}
			}
		}
	}

	/// <summary>Returns whether the objectives are destroyed.</summary>
	public bool AllObjectivesDestroyed() => (_objectivesNeeded > 0 && _objectivesDestroyed == _objectivesNeeded);

	/// <summary>Gets the current item ID counter (C++ returns <c>int*</c>; pass it as <c>ref save.CurrentItemId</c>).</summary>
	public ref int CurrentItemId => ref _itemId;

	/// <summary>Finds a fitting node where a unit can spawn.</summary>
	/// <param name="nodeRank">Rank of the node (this is not the rank of the alien!).</param>
	/// <param name="unit">Pointer to the unit (to get its position).</param>
	/// <returns>The chosen node.</returns>
	public Node GetSpawnNode(int nodeRank, BattleUnit unit)
	{
		ArgumentNullException.ThrowIfNull(unit);
		int highestPriority = -1;
		var compliantNodes = new List<Node>();

		foreach (var node in Nodes)
		{
			if (node.IsDummy)
			{
				continue;
			}
			if ((int)node.Rank == nodeRank								// ranks must match
				&& ((node.Type & Node.TYPE_SMALL) == 0
					|| unit.IsSmallUnit)								// the small unit bit is not set or the unit is small
				&& ((node.Type & Node.TYPE_FLYING) == 0
					|| unit.MovementType == MovementType.MT_FLY)		// the flying unit bit is not set or the unit can fly
				&& node.Priority > 0									// priority 0 is no spawn place
				&& SetUnitPosition(unit, node.Position, true))			// check if not already occupied
			{
				if (node.Priority > highestPriority)
				{
					highestPriority = node.Priority;
					compliantNodes.Clear(); // drop the last nodes, as we found a higher priority now
				}
				if (node.Priority == highestPriority)
				{
					compliantNodes.Add(node);
				}
			}
		}

		if (compliantNodes.Count == 0) return null;

		int n = Rng.Generate(0, compliantNodes.Count - 1);

		return compliantNodes[n];
	}

	/// <summary>Finds a fitting node where a unit can patrol to.</summary>
	/// <param name="scout">Is the unit scouting?</param>
	/// <param name="unit">Pointer to the unit (to get its position).</param>
	/// <param name="fromNode">Pointer to the node the unit is at.</param>
	/// <returns>The chosen node.</returns>
	public Node GetPatrolNode(bool scout, BattleUnit unit, Node fromNode)
	{
		ArgumentNullException.ThrowIfNull(unit);
		var compliantNodes = new List<Node>();
		Node preferred = null;

		if (fromNode == null)
		{
			if (Options.traceAI) { Logger.Info("This alien got lost. :("); }
			fromNode = Nodes[Rng.Generate(0, Nodes.Count - 1)];
			while (fromNode.IsDummy)
			{
				fromNode = Nodes[Rng.Generate(0, Nodes.Count - 1)];
			}
		}

		// scouts roam all over while all others shuffle around to adjacent nodes at most:
		int end = scout ? Nodes.Count : fromNode.NodeLinks.Count;

		for (int i = 0; i < end; ++i)
		{
			if (!scout && fromNode.NodeLinks[i] < 1) continue;

			Node n = Nodes[scout ? i : fromNode.NodeLinks[i]];
			if (!n.IsDummy																				// don't consider dummy nodes.
				&& (n.Flags > 0 || n.Rank > 0 || scout)													// for non-scouts we find a node with a desirability above 0
				&& ((n.Type & Node.TYPE_SMALL) == 0 || unit.IsSmallUnit)								// the small unit bit is not set or the unit is small
				&& ((n.Type & Node.TYPE_FLYING) == 0 || unit.MovementType == MovementType.MT_FLY)		// the flying unit bit is not set or the unit can fly
				&& !n.IsAllocated																		// check if not allocated
				&& (n.Type & Node.TYPE_DANGEROUS) == 0													// don't go there if an alien got shot there; stupid behavior like that
				&& SetUnitPosition(unit, n.Position, true)												// check if not already occupied
				&& GetTile(n.Position) != null && GetTile(n.Position).Fire == 0							// you are not a firefighter; do not patrol into fire
				&& (unit.Faction != UnitFaction.FACTION_HOSTILE || !GetTile(n.Position).Dangerous)		// aliens don't run into a grenade blast
				&& (!scout || n != fromNode)															// scouts push forward
				&& n.Position.x > 0 && n.Position.y > 0)
			{
				if (preferred == null
					|| (unit.RankInt >= 0 &&
						(int)preferred.Rank == Node.nodeRank[unit.RankInt, 0] &&
						preferred.Flags < n.Flags)
					|| preferred.Flags < n.Flags)
				{
					preferred = n;
				}
				compliantNodes.Add(n);
			}
		}

		if (compliantNodes.Count == 0)
		{
			if (Options.traceAI) { Logger.Info((scout ? "Scout " : "Guard") + " found on patrol node! XXX XXX XXX"); }
			if (unit.IsBigUnit && !scout)
			{
				return GetPatrolNode(true, unit, fromNode); // move dammit
			}
			else
				return null;
		}

		if (scout)
		{
			// scout picks a random destination:
			return compliantNodes[Rng.Generate(0, compliantNodes.Count - 1)];
		}
		else
		{
			if (preferred == null) return null;

			// non-scout patrols to highest value unoccupied node that's not fromNode
			if (Options.traceAI) { Logger.Info($"Choosing node flagged {preferred.Flags}"); }
			return preferred;
		}
	}

	/// <summary>Carries out new turn preparations such as fire and smoke spreading.</summary>
	public void PrepareNewTurn()
	{
		var tilesOnFire = new List<Tile>();
		var tilesOnSmoke = new List<Tile>();

		// prepare a list of tiles on fire
		for (int i = 0; i < _mapsize_x * _mapsize_y * _mapsize_z; ++i)
		{
			if (GetTile(i).Fire > 0)
			{
				tilesOnFire.Add(GetTile(i));
			}
		}

		// first: fires spread
		foreach (var tileOnFire in tilesOnFire)
		{
			// if we haven't added fire here this turn
			if (tileOnFire.Overlaps == 0)
			{
				// reduce the fire timer
				tileOnFire.Fire = tileOnFire.Fire - 1;

				// if we're still burning
				if (tileOnFire.Fire != 0)
				{
					// propagate in four cardinal directions (0, 2, 4, 6)
					for (int dir = 0; dir <= 6; dir += 2)
					{
						Position pos = default;
						Pathfinding.DirectionToVector(dir, ref pos);
						Tile t = GetTile(tileOnFire.Position + pos);
						// if there's no wall blocking the path of the flames...
						if (t != null && TileEngine.HorizontalBlockage(tileOnFire, t, ItemDamageType.DT_IN) == 0)
						{
							// attempt to set this tile on fire
							t.Ignite(tileOnFire.Smoke);
						}
					}
				}
				// fire has burnt out
				else
				{
					tileOnFire.Smoke = 0;
					// burn this tile, and any object in it, if it's not fireproof/indestructible.
					if (tileOnFire.GetMapData(TilePart.O_OBJECT) != null)
					{
						if (tileOnFire.GetMapData(TilePart.O_OBJECT).Flammable != 255 && tileOnFire.GetMapData(TilePart.O_OBJECT).Armor != 255)
						{
							if (tileOnFire.Destroy(TilePart.O_OBJECT, ObjectiveType))
							{
								AddDestroyedObjective();
							}
							if (tileOnFire.Destroy(TilePart.O_FLOOR, ObjectiveType))
							{
								AddDestroyedObjective();
							}
						}
					}
					else if (tileOnFire.GetMapData(TilePart.O_FLOOR) != null)
					{
						if (tileOnFire.GetMapData(TilePart.O_FLOOR).Flammable != 255 && tileOnFire.GetMapData(TilePart.O_FLOOR).Armor != 255)
						{
							if (tileOnFire.Destroy(TilePart.O_FLOOR, ObjectiveType))
							{
								AddDestroyedObjective();
							}
						}
					}
					TileEngine.ApplyGravity(tileOnFire);
				}
			}
		}

		// prepare a list of tiles on fire/with smoke in them (smoke acts as fire intensity)
		for (int i = 0; i < _mapsize_x * _mapsize_y * _mapsize_z; ++i)
		{
			if (GetTile(i).Smoke > 0)
			{
				tilesOnSmoke.Add(GetTile(i));
			}
			GetTile(i).Dangerous = false;
		}

		// now make the smoke spread.
		foreach (var tileOnSmoke in tilesOnSmoke)
		{
			// smoke and fire follow slightly different rules.
			if (tileOnSmoke.Fire == 0)
			{
				// reduce the smoke counter
				tileOnSmoke.Smoke = tileOnSmoke.Smoke - 1;
				// if we're still smoking
				if (tileOnSmoke.Smoke != 0)
				{
					// spread in four cardinal directions
					for (int dir = 0; dir <= 6; dir += 2)
					{
						Position pos = default;
						Pathfinding.DirectionToVector(dir, ref pos);
						Tile t = GetTile(tileOnSmoke.Position + pos);
						// as long as there are no walls blocking us
						if (t != null && TileEngine.HorizontalBlockage(tileOnSmoke, t, ItemDamageType.DT_SMOKE) == 0)
						{
							// only add smoke to empty tiles, or tiles with no fire, and smoke that was added this turn
							if (t.Smoke == 0 || (t.Fire == 0 && t.Overlaps != 0))
							{
								t.AddSmoke(tileOnSmoke.Smoke);
							}
						}
					}
				}
			}
			else
			{
				// smoke from fire spreads upwards one level if there's no floor blocking it.
				Position pos = new Position(0, 0, 1);
				Tile t = GetTile(tileOnSmoke.Position + pos);
				if (t != null && t.HasNoFloor(this))
				{
					// only add smoke equal to half the intensity of the fire
					t.AddSmoke(tileOnSmoke.Smoke / 2);
				}
				// then it spreads in the four cardinal directions.
				for (int dir = 0; dir <= 6; dir += 2)
				{
					Pathfinding.DirectionToVector(dir, ref pos);
					t = GetTile(tileOnSmoke.Position + pos);
					if (t != null && TileEngine.HorizontalBlockage(tileOnSmoke, t, ItemDamageType.DT_SMOKE) == 0)
					{
						t.AddSmoke(tileOnSmoke.Smoke / 2);
					}
				}
			}
		}

		if (tilesOnFire.Count != 0 || tilesOnSmoke.Count != 0)
		{
			// do damage to units, average out the smoke, etc.
			for (int i = 0; i < _mapsize_x * _mapsize_y * _mapsize_z; ++i)
			{
				if (GetTile(i).Smoke != 0)
					GetTile(i).PrepareNewTurn(Depth == 0);
			}
		}

		Mod mod = State.GamePtr.Mod; // static State::_game in C++ (see GeoscapeSave)
		foreach (var bu in Units)
		{
			bu.CalculateEnviDamage(mod, this);
		}

		//fov and light udadates are done in `BattlescapeGame::endTurn`
	}

	/// <summary>
	/// Checks for units that are unconscious and revives them if they shouldn't be.
	/// Revived units need a tile to stand on. If the unit's current position is occupied, then
	/// all directions around the tile are searched for a free tile to place the unit in.
	/// If no free tile is found the unit stays unconscious.
	/// </summary>
	public void ReviveUnconsciousUnits(bool noTU = false)
	{
		foreach (var bu in Units)
		{
			if (bu.IsSmallUnit && !bu.IsIgnored)
			{
				Position originalPosition = bu.Position;
				if (originalPosition == new Position(-1, -1, -1))
				{
					foreach (var bi in _items)
					{
						if (bi.Unit != null && bi.Unit == bu && bi.Owner != null)
						{
							originalPosition = bi.Owner.Position;
						}
					}
				}
				if (bu.Status == UnitStatus.STATUS_UNCONSCIOUS && !bu.IsOutThresholdExceed)
				{
					Tile targetTile = GetTile(originalPosition);
					bool largeUnit = targetTile != null && targetTile.Unit != null && targetTile.Unit != bu && targetTile.Unit.IsBigUnit;
					if (PlaceUnitNearPosition(bu, originalPosition, largeUnit))
					{
						// recover from unconscious
						bu.NotificationShown = 0;
						bu.Turn(false); // makes the unit stand up again
						bu.Kneel(false);
						bu.SetAlreadyExploded(false);
						if (noTU)
						{
							bu.ClearTimeUnits();
						}
						else
						{
							bu.UpdateUnitStats(true, false);
							if (Mod.TURecoveryWakeUpNewTurn < 100)
							{
								int newTU = bu.TimeUnits * Mod.TURecoveryWakeUpNewTurn / 100;
								bu.TimeUnits = newTU;
							}
						}
						RemoveUnconsciousBodyItem(bu);
					}
				}
			}
		}
	}

	/// <summary>Removes the body item that corresponds to the unit.</summary>
	public void RemoveUnconsciousBodyItem(BattleUnit bu)
	{
		ArgumentNullException.ThrowIfNull(bu);
		int size = bu.Armor.Size;
		size *= size;
		// remove the unconscious body item corresponding to this unit
		var items = Items;
		for (int iter = 0; iter < items.Count;)
		{
			if (items[iter].Unit == bu)
			{
				RemoveItem(items[iter]); //TODO: if used on anything other that corpse it will crash as it could remove MORE than one item from `getItems()` and them go pass `!= end()`
				if (--size == 0) break;
			}
			else
			{
				++iter;
			}
		}
	}

	/// <summary>Places units on the map. Handles large units that are placed on multiple tiles.</summary>
	/// <param name="bu">The unit to be placed.</param>
	/// <param name="position">The position to place the unit.</param>
	/// <param name="testOnly">If true then just checks if the unit can be placed at the position.</param>
	/// <returns>True if the unit could be successfully placed.</returns>
	public bool SetUnitPosition(BattleUnit bu, Position position, bool testOnly = false)
	{
		ArgumentNullException.ThrowIfNull(bu);
		int size = bu.Armor.Size - 1;
		Position zOffset = new Position(0, 0, 0);
		// first check if the tiles are occupied
		for (int x = size; x >= 0; x--)
		{
			for (int y = size; y >= 0; y--)
			{
				Tile t = GetTile(position + new Position(x, y, 0) + zOffset);
				if (t == null ||
					(t.Unit != null && t.Unit != bu) ||
					t.GetTUCost((int)TilePart.O_OBJECT, bu.MovementType) == Pathfinding.INVALID_MOVE_COST ||
					(t.HasNoFloor(this) && bu.MovementType != MovementType.MT_FLY) ||
					(t.GetMapData(TilePart.O_OBJECT) != null && t.GetMapData(TilePart.O_OBJECT).BigWall != 0 && t.GetMapData(TilePart.O_OBJECT).BigWall <= 3))
				{
					return false;
				}
				// move the unit up to the next level (desert and seabed terrains)
				if (t != null && t.TerrainLevel == -24)
				{
					zOffset.z += 1;
					x = size;
					y = size + 1;
				}
			}
		}

		if (size > 0)
		{
			Pathfinding.SetUnit(bu); //TODO: remove as was required by `isBlockedDirection`
			for (int dir = 2; dir <= 4; ++dir)
			{
				if (Pathfinding.IsBlockedDirection(bu, GetTile(position + zOffset), dir))
					return false;
			}
		}

		if (testOnly) return true;

		bu.SetTile(GetTile(position + zOffset), this);
		bu.SetPosition(position + zOffset);

		return true;
	}

	/// <summary>Adds this unit to the vector of falling units, if it doesn't already exist.</summary>
	/// <returns>Was the unit added?</returns>
	public bool AddFallingUnit(BattleUnit unit)
	{
		bool add = true;
		foreach (var bu in _fallingUnits)
		{
			if (unit == bu)
			{
				add = false;
				break;
			}
		}
		if (add)
		{
			_fallingUnits.Insert(0, unit);
			_unitsFalling = true;
		}
		return add;
	}

	/// <summary>Gets all units in the battlescape that are falling.</summary>
	public List<BattleUnit> FallingUnits => _fallingUnits;

	/// <summary>Gets/sets the switch that says "there are units falling, start the fall state".</summary>
	public bool UnitsFalling { get => _unitsFalling; set => _unitsFalling = value; }

	/// <summary>Gets/sets the BattlescapeState.</summary>
	public BattlescapeState BattleState { get => _battleState; set => _battleState = value; }

	/// <summary>Gets the BattlescapeGame.</summary>
	public BattlescapeGame BattleGame => _battleState.BattleGame;

	/// <summary>Is BattlescapeState busy?</summary>
	public bool IsBattlescapeStateBusy
	{
		get
		{
			if (_battleState != null)
			{
				return _battleState.IsBusy;
			}
			return false;
		}
	}

	/// <summary>Is CTRL pressed?</summary>
	public bool IsCtrlPressed(bool considerTouchButtons = false)
	{
		if (_battleState != null)
		{
			return _battleState.Game.IsCtrlPressed(considerTouchButtons);
		}
		return false;
	}

	/// <summary>Is ALT pressed?</summary>
	public bool IsAltPressed(bool considerTouchButtons = false)
	{
		if (_battleState != null)
		{
			return _battleState.Game.IsAltPressed(considerTouchButtons);
		}
		return false;
	}

	/// <summary>Is SHIFT pressed?</summary>
	public bool IsShiftPressed(bool considerTouchButtons = false)
	{
		if (_battleState != null)
		{
			return _battleState.Game.IsShiftPressed(considerTouchButtons);
		}
		return false;
	}

	/// <summary>Gets the highest ranked, living XCom unit.</summary>
	public BattleUnit HighestRankedXCom
	{
		get
		{
			BattleUnit highest = null;
			foreach (var bu in _units)
			{
				if (bu.OriginalFaction == UnitFaction.FACTION_PLAYER && !bu.IsOut)
				{
					if (highest == null || bu.RankInt > highest.RankInt)
					{
						highest = bu;
					}
				}
			}
			return highest;
		}
	}

	/// <summary>Gets morale modifier of unit.</summary>
#pragma warning disable CA1822 // instance method in C++, kept for callers
	public int GetUnitMoraleModifier(BattleUnit unit)
#pragma warning restore CA1822
	{
		ArgumentNullException.ThrowIfNull(unit);
		int result = 100;

		if (unit.OriginalFaction == UnitFaction.FACTION_PLAYER)
		{
			// C++ switch with fallthrough from the rank down to 2
			int rank = unit.RankInt;
			if (rank == 5)
				result += 25;
			if (rank >= 4 && rank <= 5)
				result += 20;
			if (rank >= 3 && rank <= 5)
				result += 10;
			if (rank >= 2 && rank <= 5)
				result += 20;
		}

		return result;
	}

	/// <summary>Gets the morale loss modifier (by unit type) of the killed unit.</summary>
#pragma warning disable CA1822 // instance method in C++, kept for callers
	public int GetMoraleLossModifierWhenKilled(BattleUnit unit)
#pragma warning restore CA1822
	{
		int result = 100;

		if (unit == null)
			return result;

		if (unit.GeoscapeSoldier != null)
		{
			result = unit.GeoscapeSoldier.Rules.MoraleLossWhenKilled;
		}
		else if (unit.UnitRules != null)
		{
			result = unit.UnitRules.MoraleLossWhenKilled;
		}

		// it's a morale loss, there's no way I am allowing modders to make it a morale gain!
		return Math.Max(0, result);
	}

	/// <summary>
	/// Gets the morale modifier for XCom based on the highest ranked, living XCom unit,
	/// or Alien modifier based on they number.
	/// </summary>
	/// <param name="player">modifier for player or hostile?</param>
	public int GetFactionMoraleModifier(bool player)
	{
		if (player)
		{
			BattleUnit leader = HighestRankedXCom;
			int result = 100;
			if (leader != null)
			{
				// C++ switch with fallthrough from the rank down to 2
				int rank = leader.RankInt;
				if (rank == 5)
					result += 25;
				if (rank >= 4 && rank <= 5)
					result += 10;
				if (rank >= 3 && rank <= 5)
					result += 5;
				if (rank >= 2 && rank <= 5)
					result += 10;
			}
			return result;
		}
		else
		{
			int number = 0;
			foreach (var bu in _units)
			{
				if (bu.OriginalFaction == UnitFaction.FACTION_HOSTILE && !bu.IsOut)
				{
					++number;
				}
			}
			return Math.Max(6 * number, 100);
		}
	}

	/// <summary>
	/// Checks whether anyone on a particular faction is looking at the unit.
	/// Similar to getSpottingUnits() but returns a bool and stops searching if one positive hit is found.
	/// </summary>
	/// <returns>True when the unit can be seen.</returns>
	public bool EyesOnTarget(UnitFaction faction, BattleUnit unit)
	{
		foreach (var bu in Units)
		{
			if (bu.Faction != faction) continue;

			var vis = bu.VisibleUnits;
			if (vis.Contains(unit)) return true;
			// aliens know the location of all XCom agents sighted by all other aliens due to sharing locations over their space-walkie-talkies
		}

		return false;
	}

	/// <summary>Places a unit on or near a position.</summary>
	/// <param name="unit">The unit to place.</param>
	/// <param name="entryPoint">The position around which to attempt to place the unit.</param>
	/// <param name="largeFriend">Is the unit on the entry point a large unit?</param>
	/// <returns>True if the unit was successfully placed.</returns>
	public bool PlaceUnitNearPosition(BattleUnit unit, Position entryPoint, bool largeFriend)
	{
		ArgumentNullException.ThrowIfNull(unit);
		if (SetUnitPosition(unit, entryPoint))
		{
			return true;
		}

		int me = 0 - unit.Armor.Size;
		int you = largeFriend ? 2 : 1;
		int[] xArray = [0, you, you, you, 0, me, me, me];
		int[] yArray = [me, me, 0, you, you, you, 0, me];
		for (int dir = 0; dir <= 7; ++dir)
		{
			Position offset = new Position(xArray[dir], yArray[dir], 0);
			Tile t = GetTile(entryPoint + offset);
			if (t != null && !Pathfinding.IsBlockedDirection(unit, GetTile(entryPoint + (offset / 2)), dir)
				&& SetUnitPosition(unit, entryPoint + offset))
			{
				return true;
			}
		}

		if (unit.MovementType == MovementType.MT_FLY)
		{
			Tile t = GetTile(entryPoint + new Position(0, 0, 1));
			if (t != null && t.HasNoFloor(this) && SetUnitPosition(unit, entryPoint + new Position(0, 0, 1)))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Resets the turn counter.</summary>
	public void ResetTurnCounter()
	{
		_turn = 0;
		_cheating = false;
		_side = UnitFaction.FACTION_PLAYER;
		_beforeGame = true;
	}

	/// <summary>Resets visibility of all the tiles on the map.</summary>
	public void ResetTiles()
	{
		for (int i = 0; i != MapSizeXYZ; ++i)
		{
			_tiles[i].SetDiscovered(false, TilePart.O_WESTWALL);
			_tiles[i].SetDiscovered(false, TilePart.O_NORTHWALL);
			_tiles[i].SetDiscovered(false, TilePart.O_FLOOR);
		}
	}

	/// <summary>Gets an 11x11 grid of positions (-5 to +5) for use in AI functions.</summary>
	public List<Position> TileSearch => _tileSearch;

	/// <summary>Check if the names or the callsigns of the soldiers should be displayed.</summary>
	public bool IsNameDisplay => _nameDisplay;

	/// <summary>Sets displaying the name (true) or the callsign (false).</summary>
	public void SetNameDisplay(bool displayName)
	{
		_nameDisplay = displayName;
	}

	/// <summary>Is the AI allowed to cheat?</summary>
	public bool IsCheating => _cheating;

	/// <summary>Gets/sets the TU reserved type.</summary>
	public BattleActionType TUReserved { get => _tuReserved; set => _tuReserved = value; }

	/// <summary>Gets/sets the kneel reservation setting (reserve extra TUs to kneel).</summary>
	public bool KneelReserved { get => _kneelReserved; set => _kneelReserved = value; }

	/// <summary>Gives access to the "storage space" vector, for distribution of items in base defense missions.</summary>
	public List<Position> StorageSpace => _storageSpace;

	/// <summary>Move all the leftover items in base defense missions to random locations in the storage facilities.</summary>
	/// <param name="t">the tile where all our goodies are initially stored.</param>
	public void RandomizeItemLocations(Tile t)
	{
		ArgumentNullException.ThrowIfNull(t);
		// remove position of Tile t from the vector (because of potential endless loop)
		_storageSpace.RemoveAll(p => p == t.Position);

		if (_storageSpace.Count != 0)
		{
			var inventory = t.Inventory;
			for (int iter = 0; iter < inventory.Count;)
			{
				BattleItem bi = inventory[iter];
				if (bi.Slot.Type == InventoryType.INV_GROUND)
				{
					GetTile(_storageSpace[Rng.Generate(0, _storageSpace.Count - 1)]).AddItem(bi, bi.Slot);
					inventory.RemoveAt(iter);
				}
				else
				{
					++iter;
				}
			}
		}
	}

	/// <summary>
	/// The base module destruction map: how many destructible base modules remain at any given grid reference
	/// in the basescape, using [x][y] format. -1 for "no items" 0 for "destroyed" and any actual number represents how many left.
	/// </summary>
	public List<List<(int first, int second)>> ModuleMap => _baseModules;

	/// <summary>
	/// Calculate the number of map modules remaining by counting the map objects
	/// on the top floor who have the baseModule flag set. We store this data in the grid
	/// as outlined in the comments above, in pairs representing initial and current values.
	/// </summary>
	public void CalculateModuleMap()
	{
		// std::vector::resize(n, value)
		int sizeX = _mapsize_x / 10;
		if (_baseModules.Count > sizeX)
			_baseModules.RemoveRange(sizeX, _baseModules.Count - sizeX);
		while (_baseModules.Count < sizeX)
			_baseModules.Add(Enumerable.Repeat((-1, -1), _mapsize_y / 10).ToList());

		for (int x = 0; x != _mapsize_x; ++x)
		{
			for (int y = 0; y != _mapsize_y; ++y)
			{
				for (int z = 0; z != _mapsize_z; ++z)
				{
					Tile tile = GetTile(new Position(x, y, z));
					if (tile != null && tile.GetMapData(TilePart.O_OBJECT) != null && tile.GetMapData(TilePart.O_OBJECT).IsBaseModule)
					{
						var module = _baseModules[x / 10][y / 10];
						module.first += module.first > 0 ? 1 : 2;
						module.second = module.first;
						_baseModules[x / 10][y / 10] = module;
					}
				}
			}
		}
	}

	/// <summary>A shortcut to the geoscape save.</summary>
	/// <remarks>
	/// C++ reads the static State::_game through <c>_battleState</c>, which also works while the battle is generated
	/// and no BattlescapeState exists yet; the port uses the static game pointer directly.
	/// </remarks>
#pragma warning disable CA1822 // instance member as in C++
	public SavedGame GeoscapeSave => State.GamePtr.SavedGame;
#pragma warning restore CA1822

	/// <summary>Gets/sets the depth of the battlescape game (0-3).</summary>
	public int Depth { get => _depth; set => _depth = value; }

	/// <summary>Uses the depth variable to choose a palette.</summary>
	public void SetPaletteByDepth(State state)
	{
		ArgumentNullException.ThrowIfNull(state);
		if (_depth == 0)
		{
			state.SetStandardPalette("PAL_BATTLESCAPE");
		}
		else
		{
			state.SetStandardPalette("PAL_BATTLESCAPE_" + _depth);
		}
	}

	/// <summary>Gets/sets the ambient battlescape sound effect.</summary>
	public int AmbientSound { get => _ambience; set => _ambience = value; }

	/// <summary>Gets/sets the random ambient sound effects (the setter copies the list).</summary>
	public List<int> AmbienceRandom { get => _ambienceRandom; set => _ambienceRandom = new List<int>(value); }

	/// <summary>Gets/sets the minimum delay for the random ambient sound effect.</summary>
	public int MinAmbienceRandomDelay { get => _minAmbienceRandomDelay; set => _minAmbienceRandomDelay = value; }

	/// <summary>Gets/sets the maximum delay for the random ambient sound effect.</summary>
	public int MaxAmbienceRandomDelay { get => _maxAmbienceRandomDelay; set => _maxAmbienceRandomDelay = value; }

	/// <summary>Gets/sets the current delay for the random ambient sound effect.</summary>
	public int CurrentAmbienceDelay { get => _currentAmbienceDelay; set => _currentAmbienceDelay = value; }

	public void DecreaseCurrentAmbienceDelay()
	{
		_currentAmbienceDelay--;
	}

	/// <summary>Reset the current random ambient sound delay.</summary>
	public void ResetCurrentAmbienceDelay()
	{
		_currentAmbienceDelay = Rng.Seedless(_minAmbienceRandomDelay * 10, _maxAmbienceRandomDelay * 10);
		if (_currentAmbienceDelay < 10)
			_currentAmbienceDelay = 10; // at least 1 second
	}

	/// <summary>Play a random ambient sound.</summary>
	public void PlayRandomAmbientSound()
	{
		if (_ambienceRandom.Count != 0)
		{
			int soundIndex = Rng.Seedless(0, _ambienceRandom.Count - 1);
			Mod.GetSoundByDepth(unchecked((uint)_depth), unchecked((uint)_ambienceRandom[soundIndex])).Play(3); // use fixed ambience channel; don't check if previous sound is still playing or not
		}
	}

	/// <summary>Gets ruleset.</summary>
	public Mod Mod => _rule;

	/// <summary>Get the list of items we're guaranteed to take with us (ie: items that were in the skyranger).</summary>
	public List<BattleItem> GuaranteedRecoveredItems => _recoverGuaranteed;

	/// <summary>Get the list of items we're not guaranteed to take with us (ie: items that were NOT in the skyranger).</summary>
	public List<BattleItem> ConditionalRecoveredItems => _recoverConditional;

	/// <summary>Gets/sets the music track for the current battle.</summary>
	public string Music { get => _music; set => _music = value; }

	/// <summary>Gets/sets the VIP escape type.</summary>
	public EscapeType VIPEscapeType { get => _vipEscapeType; set => _vipEscapeType = value; }

	/// <summary>
	/// Gets/sets the percentage of VIPs that must survive in order to accomplish the mission.
	/// If the mission has multiple stages, the biggest setting is used at the end (the setter keeps the maximum).
	/// </summary>
	public int VIPSurvivalPercentage { get => _vipSurvivalPercentage; set => _vipSurvivalPercentage = Math.Max(_vipSurvivalPercentage, value); }

	/// <summary>Increase the saved VIPs counter and score.</summary>
	public void AddSavedVIP(int score)
	{
		_vipsSaved++;
		_vipsSavedScore += score;
	}

	/// <summary>Gets the saved VIPs counter.</summary>
	public int SavedVIPs => _vipsSaved;

	/// <summary>Gets the saved VIPs total score.</summary>
	public int SavedVIPsScore => _vipsSavedScore;

	/// <summary>Increase the lost VIPs counter and score.</summary>
	public void AddLostVIP(int score)
	{
		_vipsLost++;
		_vipsLostScore -= score;
	}

	/// <summary>Gets the lost VIPs counter.</summary>
	public int LostVIPs => _vipsLost;

	/// <summary>Gets the lost VIPs total score.</summary>
	public int LostVIPsScore => _vipsLostScore;

	/// <summary>Increase the waiting outside VIPs counter and score.</summary>
	public void AddWaitingOutsideVIP(int score)
	{
		_vipsWaitingOutside++;
		_vipsWaitingOutsideScore += score;
	}

	/// <summary>Corrects the VIP stats based on the final mission outcome.</summary>
	public void CorrectVIPStats(bool success, bool retreated)
	{
		if (success)
		{
			// if we won, all waiting VIPs are saved
			_vipsSaved += _vipsWaitingOutside;
			_vipsWaitingOutside = 0;

			_vipsSavedScore += _vipsWaitingOutsideScore;
			_vipsWaitingOutsideScore = 0;
		}
		else
		{
			// if we lost, all waiting VIPs are lost
			_vipsLost += _vipsWaitingOutside;
			_vipsWaitingOutside = 0;

			_vipsLostScore -= _vipsWaitingOutsideScore;
			_vipsWaitingOutsideScore = 0;

			if (retreated)
			{
				// if we retreated, keep all VIPs waiting in the craft alive
			}
			else
			{
				// if nobody from xcom survived, all VIPs are lost too
				_vipsLost += _vipsSaved;
				_vipsSaved = 0;

				_vipsLostScore -= _vipsSavedScore;
				_vipsSavedScore = 0;
			}
		}
	}

	/// <summary>Set the objective type for the current battle.</summary>
	public void SetObjectiveType(int type)
	{
		_objectiveType = type;
	}

	/// <summary>Get the objective type for the current battle.</summary>
	public SpecialTileType ObjectiveType => (SpecialTileType)_objectiveType;

	/// <summary>Gets/sets the ambient sound effect volume.</summary>
	public double AmbientVolume { get => _ambientVolume; set => _ambientVolume = value; }

	/// <summary>Gets/sets the maximum number of turns we have before this mission ends.</summary>
	public int TurnLimit { get => _turnLimit; set => _turnLimit = value; }

	/// <summary>Gets/sets the action type to perform when the timer expires.</summary>
	public ChronoTrigger ChronoTrigger { get => _chronoTrigger; set => _chronoTrigger = value; }

	/// <summary>Sets the turn at which the players become exposed to the AI.</summary>
	public void SetCheatTurn(int turn)
	{
		_cheatTurn = turn;
	}

	/// <summary>Check whether the battle has actually commenced or not.</summary>
	public bool IsBeforeGame => _beforeGame;

	/// <summary>Gets/sets personal light toggle.</summary>
	public bool TogglePersonalLight { get => _togglePersonalLight; set => _togglePersonalLight = value; }

	/// <summary>Gets/sets night vision toggle.</summary>
	public bool ToggleNightVision { get => _toggleNightVision; set => _toggleNightVision = value; }

	/// <summary>Gets/sets brightness toggle.</summary>
	public int ToggleBrightness { get => _toggleBrightness; set => _toggleBrightness = value; }

	/// <summary>Sets final value for personal light toggle (for scripts).</summary>
	public void SetTogglePersonalLightTemp(bool togglePersonalLight)
	{
		_togglePersonalLightTemp = togglePersonalLight;
	}

	/// <summary>Sets final value for night vision toggle (for scripts).</summary>
	public void SetToggleNightVisionTemp(bool toggleNightVision)
	{
		_toggleNightVisionTemp = toggleNightVision;
	}

	/// <summary>Sets final value for night vision color toggle (for scripts).</summary>
	public void SetToggleNightVisionColorTemp(int toggleNightColorVision)
	{
		_toggleNightVisionColorTemp = toggleNightColorVision;
	}

	/// <summary>Sets final value for brightness toggle (for scripts).</summary>
	public void SetToggleBrightnessTemp(int toggleBrightness)
	{
		_toggleBrightnessTemp = toggleBrightness;
	}

	/// <summary>Randomly chooses hidden movement background.</summary>
	public void SetRandomHiddenMovementBackground(Mod mod)
	{
		if (mod != null && mod.HiddenMovementBackgrounds.Count != 0)
		{
			int rng = Rng.Seedless(0, mod.HiddenMovementBackgrounds.Count - 1);
			_hiddenMovementBackground = mod.HiddenMovementBackgrounds[rng];
		}
		else
		{
			_hiddenMovementBackground = "TAC00.SCR";
		}
	}

	/// <summary>Gets the hidden movement background ID.</summary>
	public string HiddenMovementBackground => _hiddenMovementBackground;

	/// <summary>Appends a given entry to the hit log. Works only during the player's turn.</summary>
	public void AppendToHitLog(HitLogEntryType type, UnitFaction faction)
	{
		if (_side != UnitFaction.FACTION_PLAYER) return;
		_hitLog.AppendToHitLog(type, faction);
	}

	/// <summary>Appends a given entry to the hit log. Works only during the player's turn.</summary>
	public void AppendToHitLog(HitLogEntryType type, UnitFaction faction, string text)
	{
		if (_side != UnitFaction.FACTION_PLAYER) return;
		_hitLog.AppendToHitLog(type, faction, text);
	}

	/// <summary>Gets the hit log.</summary>
	public HitLog HitLog => _hitLog;

	/// <summary>Resets all unit hit state flags.</summary>
	public void ResetUnitHitStates()
	{
		foreach (var bu in _units)
		{
			bu.ResetHitState();
		}
	}
}
