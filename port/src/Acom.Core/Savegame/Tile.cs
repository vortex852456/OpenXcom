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
// Ported from: src/Savegame/Tile.h, src/Savegame/Tile.cpp
#nullable disable

namespace Acom;

public enum LightLayers : byte
{
	LL_AMBIENT = 0,
	LL_FIRE = 1,
	LL_ITEMS = 2,
	LL_UNITS = 3,
	LL_MAX = 4,
}

public enum TileUnitOverlapping
{
	/// <summary>Any unit overlapping tile will be returned</summary>
	TUO_NORMAL = 24,
	/// <summary>Only units that overlap by 2 or more voxel will be returned</summary>
	TUO_IGNORE_SMALL = 26,
	/// <summary>Take unit from lower even if it not overlap</summary>
	TUO_ALWAYS = 0,
}

/// <summary>
/// Basic element of which a battle map is build.
/// </summary>
/// <remarks>
/// C++ keeps the tiles by value in a vector and finds neighbours by pointer arithmetic;
/// here they are objects in an array of <see cref="SavedBattleGame"/> (see <see cref="SavedBattleGame.GetBelowTile"/>).
/// </remarks>
#pragma warning disable CA1708 // C++ names: struct SerializationKey and its static instance serializationKey
public partial class Tile
#pragma warning restore CA1708
{
	public const string ScriptName = "Tile";

	/// <summary>How many bytes various fields use in a serialized tile.</summary>
	public static readonly SerializationKey serializationKey = new()
	{
		index = 4,
		_mapDataSetID = 2, // four of these
		_mapDataID = 2, // four of these
		_smoke = 1,
		_fire = 1,
		boolFields = 1, // one 8-bit bool field
		totalBytes = 4 + 2 * 4 + 2 * 4 + 1 + 1 + 1, // total bytes to save one tile
	};

	public const int NOT_CALCULATED = -1;

	private const int O_MAX = (int)TilePart.O_MAX;

	protected SavedBattleGame _save;
	protected MapData[] _objects = new MapData[O_MAX];
	protected BattleUnit _unit;
	protected List<BattleItem> _inventory = [];
	protected Tile.TileMapDataCache _mapData = new();
	/// <summary>C++ SurfaceRaw&lt;const Uint8&gt;: the current frame of each part.</summary>
	protected Surface[] _currentSurface = new Surface[O_MAX];
	protected Tile.TileObjectCache[] _objectsCache = new Tile.TileObjectCache[O_MAX];
	protected Tile.TileCache _cache;
	protected Position _pos;
	protected byte[] _light = new byte[(int)LightLayers.LL_MAX];
	protected byte _fire;
	protected byte _smoke;
	protected byte _markerColor;
	protected byte _animationOffset;
	protected byte _obstacle;
	protected byte _explosiveType;
	protected short _explosive;
	protected short _visible;
	protected short _TUMarker = -1;
	protected short _EnergyMarker = -1;
	protected sbyte _preview = -1;
	protected byte _overlaps;

	/// <summary>Creates a tile.</summary>
	public Tile(Position pos, SavedBattleGame save)
	{
		_save = save;
		_pos = pos;
		for (int i = 0; i < O_MAX; ++i)
		{
			_objects[i] = null;
			_mapData.ID[i] = -1;
			_mapData.SetID[i] = -1;
			_objectsCache[i].currentFrame = 0;
		}
		for (int layer = 0; layer < (int)LightLayers.LL_MAX; layer++)
		{
			_light[layer] = 0;
		}
		for (int i = 0; i < O_MAX; ++i)
		{
			_objectsCache[i].discovered = false;
		}
		_cache.isNoFloor = true;
		_cache.isGravLift = false;
		_cache.isLadderOnObject = false;
		_cache.isLadderOnNorth = false;
		_cache.isLadderOnWest = false;
	}

	/// <summary>Load the tile from a YAML node.</summary>
	public void Load(YamlNodeReader reader)
	{
		//reader.tryRead("position", _position);
		for (int i = 0; i < 4; i++)
		{
			reader["mapDataID"][i].TryReadVal(ref _mapData.ID[i]);
			reader["mapDataSetID"][i].TryReadVal(ref _mapData.SetID[i]);
		}

		reader.TryRead("fire", ref _fire);
		reader.TryRead("smoke", ref _smoke);
		if (reader["discovered"] is { IsValid: true } discovered)
		{
			for (int i = 0; i < 3; i++)
			{
				int realTilePart = (i == 2 ? 0 : i - 1); //convert old convention to new one
				bool value = discovered[i].ReadVal<bool>();
				// C++ writes _objectsCache[-1] for i == 0 (outside the array, undefined behaviour); skipped here
				if (realTilePart >= 0)
					_objectsCache[realTilePart].discovered = value;
			}
		}
		if (reader["openDoorWest"])
		{
			_objectsCache[1].currentFrame = 7;
		}
		if (reader["openDoorNorth"])
		{
			_objectsCache[2].currentFrame = 7;
		}
		if (_fire != 0 || _smoke != 0)
		{
			_animationOffset = (byte)Rng.Seedless(0, 3);
		}
	}

	/// <summary>Load the tile from binary.</summary>
	/// <param name="buffer">Tile data (C++ passes a pointer that is not advanced for the caller).</param>
	/// <param name="serKey">Serialization key.</param>
	public void LoadBinary(ReadOnlySpan<byte> buffer, in Tile.SerializationKey serKey)
	{
		_mapData.ID[0] = SerializationHelper.UnserializeInt(ref buffer, serKey._mapDataID);
		_mapData.ID[1] = SerializationHelper.UnserializeInt(ref buffer, serKey._mapDataID);
		_mapData.ID[2] = SerializationHelper.UnserializeInt(ref buffer, serKey._mapDataID);
		_mapData.ID[3] = SerializationHelper.UnserializeInt(ref buffer, serKey._mapDataID);
		_mapData.SetID[0] = SerializationHelper.UnserializeInt(ref buffer, serKey._mapDataSetID);
		_mapData.SetID[1] = SerializationHelper.UnserializeInt(ref buffer, serKey._mapDataSetID);
		_mapData.SetID[2] = SerializationHelper.UnserializeInt(ref buffer, serKey._mapDataSetID);
		_mapData.SetID[3] = SerializationHelper.UnserializeInt(ref buffer, serKey._mapDataSetID);

		_smoke = unchecked((byte)SerializationHelper.UnserializeInt(ref buffer, serKey._smoke));
		_fire = unchecked((byte)SerializationHelper.UnserializeInt(ref buffer, serKey._fire));

		byte boolFields = unchecked((byte)SerializationHelper.UnserializeInt(ref buffer, serKey.boolFields));
		_objectsCache[(int)TilePart.O_WESTWALL].discovered = (boolFields & 1) != 0;
		_objectsCache[(int)TilePart.O_NORTHWALL].discovered = (boolFields & 2) != 0;
		_objectsCache[(int)TilePart.O_FLOOR].discovered = (boolFields & 4) != 0;
		_objectsCache[(int)TilePart.O_WESTWALL].currentFrame = (byte)((boolFields & 8) != 0 ? 7 : 0);
		_objectsCache[(int)TilePart.O_NORTHWALL].currentFrame = (byte)((boolFields & 0x10) != 0 ? 7 : 0);
		if (_fire != 0 || _smoke != 0)
		{
			_animationOffset = (byte)Rng.Seedless(0, 3);
		}
	}

	/// <summary>Saves the tile to a YAML node.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.Write("position", _pos);
		var ids = new List<int>(_mapData.ID);
		var setIds = new List<int>(_mapData.SetID);
		writer.Write("mapDataID", ids);
		writer.Write("mapDataSetID", setIds);
		if (_smoke != 0)
			writer.Write("smoke", _smoke);
		if (_fire != 0)
			writer.Write("fire", _fire);
		if (_objectsCache[(int)TilePart.O_FLOOR].discovered || _objectsCache[(int)TilePart.O_WESTWALL].discovered || _objectsCache[(int)TilePart.O_NORTHWALL].discovered)
		{
			throw new InvalidOperationException("Obsolete code");
		}
		if (IsUfoDoorOpen(TilePart.O_WESTWALL))
		{
			writer.Write("openDoorWest", true);
		}
		if (IsUfoDoorOpen(TilePart.O_NORTHWALL))
		{
			writer.Write("openDoorNorth", true);
		}
	}

	/// <summary>Saves the tile to binary and advances the buffer (C++ <c>Uint8**</c>).</summary>
	public void SaveBinary(ref Span<byte> buffer)
	{
		SerializationHelper.SerializeInt(ref buffer, serializationKey._mapDataID, _mapData.ID[0]);
		SerializationHelper.SerializeInt(ref buffer, serializationKey._mapDataID, _mapData.ID[1]);
		SerializationHelper.SerializeInt(ref buffer, serializationKey._mapDataID, _mapData.ID[2]);
		SerializationHelper.SerializeInt(ref buffer, serializationKey._mapDataID, _mapData.ID[3]);
		SerializationHelper.SerializeInt(ref buffer, serializationKey._mapDataSetID, _mapData.SetID[0]);
		SerializationHelper.SerializeInt(ref buffer, serializationKey._mapDataSetID, _mapData.SetID[1]);
		SerializationHelper.SerializeInt(ref buffer, serializationKey._mapDataSetID, _mapData.SetID[2]);
		SerializationHelper.SerializeInt(ref buffer, serializationKey._mapDataSetID, _mapData.SetID[3]);

		SerializationHelper.SerializeInt(ref buffer, serializationKey._smoke, _smoke);
		SerializationHelper.SerializeInt(ref buffer, serializationKey._fire, _fire);

		int boolFields = (_objectsCache[(int)TilePart.O_WESTWALL].discovered ? 1 : 0) + (_objectsCache[(int)TilePart.O_NORTHWALL].discovered ? 2 : 0) + (_objectsCache[(int)TilePart.O_FLOOR].discovered ? 4 : 0);
		boolFields |= IsUfoDoorOpen(TilePart.O_WESTWALL) ? 8 : 0; // west
		boolFields |= IsUfoDoorOpen(TilePart.O_NORTHWALL) ? 0x10 : 0; // north?
		SerializationHelper.SerializeInt(ref buffer, serializationKey.boolFields, boolFields);
	}

	/// <summary>Get the MapData pointer of a part of the tile.</summary>
	public MapData GetMapData(TilePart part) => _objects[(int)part];

	/// <summary>Get special tile type of floor part.</summary>
	public SpecialTileType FloorSpecialTileType => _objects[(int)TilePart.O_FLOOR] != null ? _objects[(int)TilePart.O_FLOOR].SpecialType : SpecialTileType.TILE;

	/// <summary>Get special tile type of object part.</summary>
	public SpecialTileType ObjectSpecialTileType => _objects[(int)TilePart.O_OBJECT] != null ? _objects[(int)TilePart.O_OBJECT].SpecialType : SpecialTileType.TILE;

	/// <summary>Get saved battle game that tile belongs.</summary>
	public SavedBattleGame SavedGame => _save;

	/// <summary>Set the MapData references of part 0 to 3.</summary>
	public void SetMapData(MapData dat, int mapDataID, int mapDataSetID, TilePart part)
	{
		int p = (int)part;
		_objects[p] = dat;
		_mapData.ID[p] = mapDataID;
		_mapData.SetID[p] = mapDataSetID;
		_objectsCache[p].isDoor = dat != null && dat.IsDoor;
		_objectsCache[p].isUfoDoor = dat != null && dat.IsUFODoor;
		_objectsCache[p].offsetY = dat != null ? unchecked((sbyte)dat.YOffset) : (sbyte)0;
		_objectsCache[p].isBackTileObject = dat != null && dat.IsBackTileObject;
		if (part == TilePart.O_FLOOR || part == TilePart.O_OBJECT)
		{
			int level = 0;

			var floor = _objects[(int)TilePart.O_FLOOR];
			if (floor != null)
			{
				level = floor.TerrainLevel;
				_cache.isNoFloor = floor.IsNoFloor;
				_cache.isGravLift = floor.IsGravLift;
			}
			else
			{
				_cache.isNoFloor = true;
				_cache.isGravLift = false;
			}
			// whichever's higher, but not the sum.
			var obj = _objects[(int)TilePart.O_OBJECT];
			if (obj != null)
			{
				level = Math.Min(obj.TerrainLevel, level);
				_cache.bigWall = obj.BigWall != 0;
			}
			else
			{
				_cache.bigWall = false;
			}
			_cache.terrainLevel = unchecked((sbyte)level);
		}
		if (part == TilePart.O_OBJECT)
		{
			_cache.isLadderOnObject = _objects[(int)TilePart.O_OBJECT] != null && _objects[(int)TilePart.O_OBJECT].IsGravLift;
		}
		if (part == TilePart.O_NORTHWALL)
		{
			_cache.isLadderOnNorth = _objects[(int)TilePart.O_NORTHWALL] != null && _objects[(int)TilePart.O_NORTHWALL].IsGravLift;
		}
		if (part == TilePart.O_WESTWALL)
		{
			_cache.isLadderOnWest = _objects[(int)TilePart.O_WESTWALL] != null && _objects[(int)TilePart.O_WESTWALL].IsGravLift;
		}
		UpdateSprite(part);
	}

	/// <summary>Get the MapData references of part 0 to 3.</summary>
	public void GetMapData(ref int mapDataID, ref int mapDataSetID, TilePart part)
	{
		mapDataID = _mapData.ID[(int)part];
		mapDataSetID = _mapData.SetID[(int)part];
	}

	/// <summary>Gets whether this tile has no objects. Note that we can have a unit or smoke on this tile.</summary>
	public bool IsVoid => _objects[0] == null && _objects[1] == null && _objects[2] == null && _objects[3] == null && _smoke == 0 && _inventory.Count == 0;

	/// <summary>Gets the TU cost to walk over a certain part of the tile.</summary>
	public int GetTUCost(int part, MovementType movementType)
	{
		if (_objects[part] != null)
		{
			if (_objectsCache[part].isUfoDoor && _objectsCache[part].currentFrame > 1)
				return 0;
			if (part == (int)TilePart.O_OBJECT && _objects[part].BigWall >= 4)
				return 0;
			return _objects[part].GetTUCost(movementType);
		}
		else
			return 0;
	}

	/// <summary>Whether this tile has a floor or not. If no object defined as floor, it has no floor.</summary>
	/// <param name="savedBattleGame">Save to get tile below to check if it can work as floor.</param>
	public bool HasNoFloor(SavedBattleGame savedBattleGame = null)
	{
		//There's no point in checking for "floor" below if we have floor in this tile already.
		if (_cache.isNoFloor)
		{
			if (_pos.z > 0 && savedBattleGame != null)
			{
				Tile tileBelow = savedBattleGame.GetBelowTile(this);
				if (tileBelow != null && tileBelow.TerrainLevel == -24)
					return false;
			}
		}

		return _cache.isNoFloor;
	}

	/// <summary>Checks if this tile has a GravLift floor.</summary>
	public bool HasGravLiftFloor => _cache.isGravLift;

	/// <summary>Check if this tile has a Ladder (similar to GravLift but on wall).</summary>
	public bool HasLadder => _cache.isLadderOnObject || _cache.isLadderOnNorth || _cache.isLadderOnWest;

	/// <summary>Check if this tile object part has a Ladder.</summary>
	public bool HasLadderOnObject => _cache.isLadderOnObject;

	/// <summary>Check if this tile wall part has a Ladder.</summary>
	public bool HasLadderOnNorthWall => _cache.isLadderOnNorth;

	/// <summary>Check if this tile wall part has a Ladder.</summary>
	public bool HasLadderOnWestWall => _cache.isLadderOnWest;

	/// <summary>Whether this tile has a big wall.</summary>
	public bool IsBigWall => _cache.bigWall;

	/// <summary>Gets the height of the terrain (dirt/stairs/etc.) on this tile (more negative values are higher).</summary>
	public int TerrainLevel => _cache.terrainLevel;

	/// <summary>Gets the tile's position.</summary>
	public Position Position => _pos;

	/// <summary>Gets the tile's footstep sound.</summary>
	public int GetFootstepSound(Tile tileBelow)
	{
		int sound = -1;

		var floor = _objects[(int)TilePart.O_FLOOR];
		var obj = _objects[(int)TilePart.O_OBJECT];
		if (floor != null)
			sound = floor.FootstepSound;
		if (obj != null && obj.BigWall <= 1 && obj.FootstepSound > -1)
			sound = obj.FootstepSound;
		if (floor == null && obj == null && tileBelow != null && tileBelow.TerrainLevel == -24 && tileBelow.GetMapData(TilePart.O_OBJECT) != null)
			sound = tileBelow.GetMapData(TilePart.O_OBJECT).FootstepSound;

		return sound;
	}

	/// <summary>
	/// Open a door on this tile.
	/// </summary>
	/// <returns>0(normal door), 1(ufo door) or -1 if no door opened or 3 if ufo door(=animated) is still opening 4 if not enough TUs</returns>
	public int OpenDoor(TilePart part, BattleUnit unit = null, BattleActionType reserve = BattleActionType.BA_NONE, bool rClick = false)
	{
		int p = (int)part;
		if (_objects[p] == null) return -1;

		// C++ default-constructs the cost; it is only read when there is a unit
		BattleActionCost cost = null;
		if (unit != null)
		{
			int tuCost = _objects[p].GetTUCost(unit.MovementType);
			cost = new BattleActionCost(reserve, unit, unit.GetMainHandWeapon(false));
			cost.Time = (short)(cost.Time + ((_save.KneelReserved && !unit.IsKneeled && unit.Armor.AllowsKneeling(unit.Type == "SOLDIER")) ? unit.KneelDownCost : 0)); // Sint16 like C++
			cost.Time = (short)(cost.Time + tuCost);
			if (!rClick)
			{
				cost.Energy = (short)(cost.Energy + tuCost / 2);
			}
		}

		if (_objectsCache[p].isDoor)
		{
			if (unit != null && unit.IsBigUnit) // don't allow double-wide units to open swinging doors due to engine limitations
				return -1;
			if (unit != null && cost.Time != 0 && !cost.HaveTU())
				return 4;
			if (_unit != null && _unit != unit && _unit.Position != Position)
				return -1;
			var alt = _objects[p].Dataset.GetObject(_objects[p].AltMCD);
			SetMapData(alt, _objects[p].AltMCD, _mapData.SetID[p], alt.ObjectType);
			SetMapData(null, -1, -1, part);
			return 0;
		}
		if (_objectsCache[p].isUfoDoor && _objectsCache[p].currentFrame == 0) // ufo door part 0 - door is closed
		{
			if (unit != null && cost.Time != 0 && !cost.HaveTU())
				return 4;
			_objectsCache[p].currentFrame = 1; // start opening door
			UpdateSprite(part);
			return 1;
		}
		if (_objectsCache[p].isUfoDoor && _objectsCache[p].currentFrame != 7) // ufo door != part 7 - door is still opening
		{
			return 3;
		}
		return -1;
	}

	/// <summary>Check if the ufo door is open or opening. Used for visibility/light blocking checks.</summary>
	public bool IsUfoDoorOpen(TilePart tp) => _objectsCache[(int)tp].isUfoDoor && _objectsCache[(int)tp].currentFrame != 0;

	/// <summary>Check if part is ufo door.</summary>
	public bool IsUfoDoor(TilePart tp) => _objectsCache[(int)tp].isUfoDoor;

	/// <summary>Check if part is door.</summary>
	public bool IsDoor(TilePart tp) => _objectsCache[(int)tp].isDoor;

	/// <summary>Check if an object should be drawn behind or in front of a unit.</summary>
	public bool IsBackTileObject(TilePart tp) => _objectsCache[(int)tp].isBackTileObject;

	/// <summary>Gets surface Y offset.</summary>
	public int GetYOffset(TilePart tp) => _objectsCache[(int)tp].offsetY;

	/// <summary>Close ufo door.</summary>
	public int CloseUfoDoor()
	{
		int retval = 0;

		for (int part = (int)TilePart.O_FLOOR; part <= (int)TilePart.O_NORTHWALL; ++part)
		{
			if (IsUfoDoorOpen((TilePart)part))
			{
				_objectsCache[part].currentFrame = 0;
				retval = 1;
				UpdateSprite((TilePart)part);
			}
		}

		return retval;
	}

	/// <summary>Sets the black fog of war status of this tile.</summary>
	public void SetDiscovered(bool flag, TilePart part)
	{
		int p = (int)part;
		if (_objectsCache[p].discovered != flag)
		{
			_objectsCache[p].discovered = flag;
			if (part == TilePart.O_FLOOR && flag)
			{
				_objectsCache[(int)TilePart.O_WESTWALL].discovered = true;
				_objectsCache[(int)TilePart.O_NORTHWALL].discovered = true;
			}
		}
	}

	/// <summary>Get the black fog of war state of this tile.</summary>
	public bool IsDiscovered(TilePart part)
	{
		if (Options.oxceDisableFogOfWar) return true;
		return _objectsCache[(int)part].discovered;
	}

	/// <summary>Reset the light amount on the tile. This is done before a light level recalculation.</summary>
	public void ResetLight(LightLayers layer)
	{
		_light[(int)layer] = 0;
	}

	/// <summary>Reset multiple layers of light from defined one.</summary>
	public void ResetLightMulti(LightLayers layer)
	{
		for (int l = (int)layer; l < (int)LightLayers.LL_MAX; l++)
		{
			_light[l] = 0;
		}
	}

	/// <summary>Add the light amount on the tile. Only add light if the current light is lower.</summary>
	public void AddLight(int light, LightLayers layer)
	{
		if (_light[(int)layer] < light)
			_light[(int)layer] = unchecked((byte)light);
	}

	/// <summary>Get current light amount of the tile.</summary>
	public int GetLight(LightLayers layer) => _light[(int)layer];

	/// <summary>Get max light of the given layer and all layers below it.</summary>
	public int GetLightMulti(LightLayers layer)
	{
		int light = 0;

		for (int l = (int)layer; l >= 0; --l)
		{
			if (_light[l] > light)
				light = _light[l];
		}

		return light;
	}

	/// <summary>
	/// Gets the tile's shade amount 0-15. It returns the brightest of all light layers.
	/// Shade level is the inverse of light level. So a maximum amount of light (15) returns shade level 0.
	/// </summary>
	public int Shade
	{
		get
		{
			int light = 0;

			for (int layer = 0; layer < (int)LightLayers.LL_MAX; layer++)
			{
				if (_light[layer] > light)
					light = _light[layer];
			}

			return Math.Max(0, 15 - light);
		}
	}

	/// <summary>
	/// Destroy a part on this tile. We first remove the old object, then replace it with the destroyed one.
	/// If the destroyed part is an explosive, set the tile's explosive value, which will trigger a chained explosion.
	/// </summary>
	/// <returns>True if the objective was destroyed.</returns>
	public bool Destroy(TilePart part, SpecialTileType type)
	{
		int p = (int)part;
		bool _objective = false;
		if (_objects[p] != null)
		{
			if (_objects[p].IsGravLift)
				return false;
			_objective = _objects[p].SpecialType == type;
			MapData originalPart = _objects[p];
			int originalMapDataSetID = _mapData.SetID[p];
			SetMapData(null, -1, -1, part);
			if (originalPart.DieMCD != 0)
			{
				MapData dead = originalPart.Dataset.GetObject(originalPart.DieMCD);
				SetMapData(dead, originalPart.DieMCD, originalMapDataSetID, dead.ObjectType);
			}
			if (originalPart.Explosive != 0)
			{
				SetExplosive(originalPart.Explosive, originalPart.ExplosiveType);
			}
		}
		/* check if the floor on the lowest level is gone */
		if (part == TilePart.O_FLOOR && Position.z == 0 && _objects[(int)TilePart.O_FLOOR] == null)
		{
			/* replace with scorched earth */
			SetMapData(MapDataSet.ScorchedEarthTile, 1, 0, TilePart.O_FLOOR);
		}
		return _objective;
	}

	/// <summary>Damage terrain - check against armor.</summary>
	/// <returns>True if the objective was destroyed.</returns>
	public bool Damage(TilePart part, int power, SpecialTileType type)
	{
		bool objective = false;
		if (power >= _objects[(int)part].Armor)
			objective = Destroy(part, type);
		return objective;
	}

	/// <summary>
	/// Set a "virtual" explosive on this tile. We mark a tile this way to detonate it later.
	/// The explosive power on the tile is some kind of moving MAXIMUM of the explosive rays that passes it.
	/// </summary>
	public void SetExplosive(int power, int damageType, bool force = false)
	{
		if (force || _explosive < power)
		{
			_explosive = unchecked((short)power);
			_explosiveType = unchecked((byte)damageType);
		}
	}

	/// <summary>Get explosive on this tile.</summary>
	public int Explosive => _explosive;

	/// <summary>Get explosive type on this tile.</summary>
	public int ExplosiveType => _explosiveType;

	/// <summary>
	/// Animate the tile. This means to advance the current frame for every object.
	/// Ufo doors are a bit special, they animated only when triggered.
	/// When ufo doors are on frame 0(closed) or frame 7(open) they are not animated further.
	/// </summary>
	public void Animate()
	{
		int newframe;
		for (int i = (int)TilePart.O_FLOOR; i < O_MAX; ++i)
		{
			if (_objects[i] != null)
			{
				if (_objectsCache[i].isUfoDoor && (_objectsCache[i].currentFrame == 0 || _objectsCache[i].currentFrame == 7)) // ufo door is static
				{
					continue;
				}
				newframe = _objectsCache[i].currentFrame + 1;
				if (_objectsCache[i].isUfoDoor && _objects[i].SpecialType == SpecialTileType.START_POINT && newframe == 3)
				{
					newframe = 7;
				}
				if (newframe == 8)
				{
					newframe = 0;
				}
				_objectsCache[i].currentFrame = (byte)(newframe & 0xF); // 4-bit field in C++
			}
			UpdateSprite((TilePart)i);
		}
	}

	/// <summary>Update cached value of sprite.</summary>
	public void UpdateSprite(TilePart part)
	{
		int p = (int)part;
		if (_objects[p] != null)
		{
			_currentSurface[p] = _objects[p].Dataset.Surfaceset.GetFrame(_objects[p].GetSprite(_objectsCache[p].currentFrame));
		}
		else
		{
			_currentSurface[p] = null;
		}
	}

	/// <summary>Get object sprites (C++ returns a raw view of the frame's pixels).</summary>
	public Surface GetSprite(TilePart part) => _currentSurface[(int)part];

	/// <summary>Get/set the (alive) unit on this tile.</summary>
	public BattleUnit Unit { get => _unit; set => _unit = value; }

	/// <summary>Get unit from this tile or from tile below if unit poke out.</summary>
	public BattleUnit GetOverlappingUnit(SavedBattleGame saveBattleGame, TileUnitOverlapping range = TileUnitOverlapping.TUO_NORMAL)
	{
		var bu = Unit;
		if (bu == null && _pos.z > 0 && HasNoFloor(saveBattleGame) && _objects[(int)TilePart.O_OBJECT] == null)
		{
			var tileBelow = saveBattleGame.GetBelowTile(this);
			bu = tileBelow.Unit;
			if (bu != null && bu.Height + bu.FloatHeight - tileBelow.TerrainLevel <= (int)range)
			{
				bu = null; // if the unit below has no voxels poking into the tile, don't select it.
			}
		}
		return bu;
	}

	/// <summary>Get/set the amount of turns this tile is on fire. 0 = no fire. Setting does not increment overlaps.</summary>
	public int Fire
	{
		get => _fire;
		set
		{
			_fire = (byte)Clamp(value, 0, 255);
			_animationOffset = (byte)Rng.Generate(0, 3);
		}
	}

	/// <summary>Add smoke, increments overlap.</summary>
	public void AddSmoke(int smoke)
	{
		if (_fire == 0)
		{
			if (_overlaps == 0)
			{
				_smoke = (byte)Clamp(_smoke + smoke, 1, 15);
			}
			else
			{
				_smoke = unchecked((byte)(_smoke + smoke));
			}
			_animationOffset = (byte)Rng.Generate(0, 3);
			AddOverlap();
		}
	}

	/// <summary>Get/set the amount of turns this tile is smoking. 0 = no smoke. Setting does not increment overlaps.</summary>
	public int Smoke
	{
		get => _smoke;
		set
		{
			_smoke = (byte)Clamp(value, 0, 255);
			_animationOffset = (byte)Rng.Generate(0, 3);
		}
	}

	/// <summary>
	/// Flammability of a tile is the lowest flammability of it's objects.
	/// The lower the value, the higher the chance the tile/object catches fire.
	/// </summary>
	public int Flammability
	{
		get
		{
			int flam = 255;

			for (int i = 0; i < 4; ++i)
				if (_objects[i] != null && (_objects[i].Flammable < flam))
					flam = _objects[i].Flammable;

			return flam;
		}
	}

	/// <summary>Fuel of a tile is the highest fuel of it's objects (how long to burn).</summary>
	public int Fuel
	{
		get
		{
			int fuel = 0;

			for (int i = 0; i < 4; ++i)
				if (_objects[i] != null && (_objects[i].Fuel > fuel))
					fuel = _objects[i].Fuel;

			return fuel;
		}
	}

	/// <summary>Flammability of the particular part of the tile.</summary>
	public int GetFlammability(TilePart part) => _objects[(int)part].Flammable;

	/// <summary>Fuel of particular part of the tile.</summary>
	public int GetFuel(TilePart part) => _objects[(int)part].Fuel;

	/// <summary>
	/// Ignite starts fire on a tile, it will burn &lt;fuel&gt; rounds. Fuel of a tile is the highest fuel of its objects.
	/// NOT the sum of the fuel of the objects!
	/// </summary>
	public void Ignite(int power)
	{
		if (Flammability != 255)
		{
			power = power - (Flammability / 10) + 15;
			if (power < 0)
			{
				power = 0;
			}
			if (Rng.Percent(power) && Fuel != 0)
			{
				if (_fire == 0)
				{
					_smoke = (byte)(15 - Clamp(Flammability / 10, 1, 12));
					_overlaps = 1;
					_fire = unchecked((byte)(Fuel + 1));
					_animationOffset = (byte)Rng.Generate(0, 3);
				}
			}
		}
	}

	/// <summary>Get the number of frames the fire or smoke animation is off-sync.</summary>
	public int AnimationOffset => _animationOffset;

	/// <summary>Add an item on the tile.</summary>
	public void AddItem(BattleItem item, RuleInventory ground)
	{
		ArgumentNullException.ThrowIfNull(item);
		item.Slot = ground;
		_inventory.Add(item);
		item.Tile = this;

		// Note: floorOb drawing optimisation
		if (item.Unit != null && _inventory.Count > 1)
		{
			(_inventory[0], _inventory[^1]) = (_inventory[^1], _inventory[0]);
		}
	}

	/// <summary>Remove an item from the tile.</summary>
	public void RemoveItem(BattleItem item)
	{
		ArgumentNullException.ThrowIfNull(item);
		_inventory.Remove(item);
		item.Tile = null;
	}

	/// <summary>Get the topmost item to draw on the battlescape, or null when no item.</summary>
	public BattleItem TopItem
	{
		get
		{
			// Note: floorOb drawing optimisation
			if (_inventory.Count > 100)
			{
				// this tile has a metric ton of junk, it doesn't matter what gets drawn, let's draw it quickly
				return _inventory[0];
			}

			int biggestWeight = -999;
			BattleItem biggestItem = null;
			foreach (var bi in _inventory)
			{
				// Note: floorOb drawing optimisation
				if (bi.Unit != null)
				{
					// any unit has the highest priority (btw. this is still backwards-compatible with both xcom1/xcom2, where corpses are the heaviest items)
					return bi;
				}
				int temp = bi.TotalWeight;
				if (temp > biggestWeight)
				{
					biggestWeight = temp;
					biggestItem = bi;
				}
			}
			return biggestItem;
		}
	}

	/// <summary>Apply environment damage to unit.</summary>
	private static void ApplyEnvi(BattleUnit unit, int smoke, int fire, bool smokeDamage)
	{
		if (unit != null)
		{
			if (fire != 0)
			{
				//and avoid setting fire elementals on fire
				if (unit.SpecialAbility != (int)SpecialAbility.SPECAB_BURNFLOOR && unit.SpecialAbility != (int)SpecialAbility.SPECAB_BURN_AND_EXPLODE)
				{
					// _smoke becomes our damage value
					unit.SetEnviFire(smoke);
				}
			}
			// no fire: must be smoke
			else if (smokeDamage)
			{
				// try to knock this guy out.
				unit.SetEnviSmoke(smoke / 4 + 1);
			}
		}
	}

	/// <summary>
	/// New turn preparations.
	/// Average out any smoke added by the number of overlaps.
	/// Apply fire/smoke damage to units as applicable.
	/// </summary>
	public void PrepareNewTurn(bool smokeDamage)
	{
		// we've received new smoke in this turn, but we're not on fire, average out the smoke.
		if (_overlaps != 0 && _smoke != 0 && _fire == 0)
		{
			_smoke = (byte)Clamp((_smoke / _overlaps) - 1, 0, 15);
		}
		// if we still have smoke/fire
		if (_smoke != 0)
		{
			ApplyEnvi(_unit, _smoke, _fire, smokeDamage);
			foreach (var bi in _inventory)
			{
				ApplyEnvi(bi.Unit, _smoke, _fire, smokeDamage);
			}
		}
		_overlaps = 0;
	}

	/// <summary>Get the inventory on this tile.</summary>
	public List<BattleItem> Inventory => _inventory;

	/// <summary>Get/set the marker color on this tile.</summary>
	public int MarkerColor { get => _markerColor; set => _markerColor = unchecked((byte)value); }

	/// <summary>Get the tile visible flag; the setter ADDS the value (C++ setVisible).</summary>
	public int Visible { get => _visible; set => _visible = unchecked((short)(_visible + value)); }

	/// <summary>Get/set the direction used for path previewing.</summary>
	public int Preview { get => _preview; set => _preview = unchecked((sbyte)value); }

	/// <summary>Get/set the number to be displayed for pathfinding preview.</summary>
	public int TUMarker { get => _TUMarker; set => _TUMarker = unchecked((short)value); }

	/// <summary>Get/set the number to be displayed for pathfinding preview.</summary>
	public int EnergyMarker { get => _EnergyMarker; set => _EnergyMarker = unchecked((short)value); }

	/// <summary>How many times has this tile been overlapped with smoke/fire (runtime only).</summary>
	public int Overlaps => _overlaps;

	/// <summary>Increment the overlap value on this tile.</summary>
	public void AddOverlap()
	{
		_overlaps = unchecked((byte)(_overlaps + 1));
	}

	/// <summary>Get/set the danger flag on this tile (so the AI will avoid it).</summary>
	public bool Dangerous { get => _cache.danger; set => _cache.danger = value; }

	/// <summary>Sets the flag of an obstacle for single part.</summary>
	public void SetObstacle(int part)
	{
		_obstacle |= (byte)(1 << part);
	}

	/// <summary>Gets single obstacle flag.</summary>
	public bool GetObstacle(int part) => (_obstacle & (1 << part)) != 0;

	/// <summary>Does the tile have obstacle flag set for at least one part?</summary>
	public bool IsObstacle => _obstacle != 0;

	/// <summary>Resets obstacle flag for all parts of the tile.</summary>
	public void ResetObstacle()
	{
		_obstacle = 0;
	}

	/// <summary>C++ Clamp (fmath.h): no exception when min &gt; max, unlike Math.Clamp.</summary>
	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	/// <summary>How many bytes to store for each variable or each member of array of the same name.</summary>
	public struct SerializationKey
	{
		/// <summary>for indexing the actual tile array</summary>
		public byte index;
		public byte _mapDataSetID;
		public byte _mapDataID;
		public byte _smoke;
		public byte _fire;
		public byte boolFields;
		/// <summary>per structure, including any data not mentioned here and accounting for all array members!</summary>
		public uint totalBytes;
	}

	/// <summary>Cache of ID for tile parts used to save and load.</summary>
	public class TileMapDataCache
	{
		public int[] ID = new int[O_MAX];
		public int[] SetID = new int[O_MAX];
	}

	/// <summary>Cached data that belongs to each tile object (C++ bit fields).</summary>
	public struct TileObjectCache
	{
		public sbyte offsetY;
		/// <summary>4 bits in C++</summary>
		public byte currentFrame;
		public bool discovered;
		public bool isUfoDoor;
		public bool isDoor;
		public bool isBackTileObject;
	}

	/// <summary>Cached data that belongs to whole tile (C++ bit fields).</summary>
	public struct TileCache
	{
		public sbyte terrainLevel;
		public bool isNoFloor;
		public bool isGravLift;
		public bool isLadderOnObject;
		public bool isLadderOnNorth;
		public bool isLadderOnWest;
		public bool bigWall;
		public bool danger;
	}
}
