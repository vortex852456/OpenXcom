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
// Ported from: src/Mod/MapData.h, src/Mod/MapData.cpp
#nullable disable

namespace Acom;

public enum SpecialTileType
{
	TILE = 0,
	START_POINT = 1,
	UFO_POWER_SOURCE = 2,
	UFO_NAVIGATION = 3,
	UFO_CONSTRUCTION = 4,
	ALIEN_FOOD = 5,
	ALIEN_REPRODUCTION = 6,
	ALIEN_ENTERTAINMENT = 7,
	ALIEN_SURGERY = 8,
	EXAM_ROOM = 9,
	ALIEN_ALLOYS = 10,
	ALIEN_HABITAT = 11,
	DEAD_TILE = 12,
	END_POINT = 13,
	MUST_DESTROY = 14,
	DEATH_TRAPS = 200,
}

public enum MovementType
{
	MT_WALK = 0,
	MT_FLY = 1,
	MT_SLIDE = 2,
	MT_FLOAT = 3,
	MT_SINK = 4,
}

public enum VoxelType
{
	V_EMPTY = -1,
	V_FLOOR = 0,
	V_WESTWALL = 1,
	V_NORTHWALL = 2,
	V_OBJECT = 3,
	V_UNIT = 4,
	V_OUTOFBOUNDS = 5,
}

public enum TilePart
{
	O_FLOOR = 0,
	O_WESTWALL = 1,
	O_NORTHWALL = 2,
	O_OBJECT = 3,
	O_MAX = 4,
}

/// <summary>
/// MapData is the smallest piece of a Battlescape terrain, holding info about a certain object, wall, floor, ...
/// </summary>
/// <seealso cref="MapDataSet"/>
public class MapData
{
	public const int O_DUMMY = 999;

	private readonly MapDataSet _dataset;
	private SpecialTileType _specialType;
	private bool _isUfoDoor, _stopLOS, _isNoFloor, _isGravLift, _isDoor, _blockFire, _blockSmoke, _baseModule;
	private int _yOffset, _TUWalk, _TUFly, _TUSlide, _terrainLevel, _footstepSound, _dieMCD, _altMCD;
	private TilePart _objectType;
	private int _lightSource;
	private int _armor, _flammable, _fuel, _explosive, _explosiveType, _bigWall;
	private readonly int[] _sprite = new int[8];
	private readonly int[] _block = new int[6];
	private readonly int[] _loftID = new int[12];
	private ushort _miniMapIndex;

	/// <summary>Creates a new Map Data Object.</summary>
	/// <param name="dataset">The dataset this object belongs to.</param>
	public MapData(MapDataSet dataset)
	{
		_dataset = dataset;
		_specialType = SpecialTileType.TILE;
		_objectType = TilePart.O_FLOOR;
	}

	/// <summary>Gets the dataset this object belongs to.</summary>
	public MapDataSet Dataset => _dataset;

	/// <summary>Gets the sprite index for a certain frame (animation frame 0-7).</summary>
	public int GetSprite(int frameID) => _sprite[frameID];

	/// <summary>Sets the sprite index (in the surfaceset of the mapdataset) for a certain frame.</summary>
	public void SetSprite(int frameID, int value) => _sprite[frameID] = value;

	/// <summary>Gets whether this is an animated ufo door.</summary>
	public bool IsUFODoor => _isUfoDoor;

	/// <summary>Gets whether this is a floor.</summary>
	public bool IsNoFloor => _isNoFloor;

	/// <summary>
	/// Gets whether this is a big wall, which blocks all surrounding paths.
	/// 0: not a bigWall, 1: regular bigWall, 2: allows movement in ne/sw direction,
	/// 3: allows movement in nw/se direction, 4: acts as a west wall, 5: acts as a north wall,
	/// 6: acts as an east wall, 7: acts as a south wall, 8: acts as a south and east wall,
	/// 9: acts as a north and west wall.
	/// </summary>
	public int BigWall { get => _bigWall; set => _bigWall = value; }

	/// <summary>Gets whether this is a normal door.</summary>
	public bool IsDoor => _isDoor;

	/// <summary>Gets whether this is a grav lift.</summary>
	public bool IsGravLift => _isGravLift;

	/// <summary>Gets whether this should be drawn behind a unit or in front of a unit (i.e. if it works as a S or E wall).</summary>
	public bool IsBackTileObject => BigWall < 6 || BigWall == 9;

	/// <summary>Sets all kinds of flags.</summary>
	public void SetFlags(bool isUfoDoor, bool stopLOS, bool isNoFloor, int bigWall, bool isGravLift, bool isDoor, bool blockFire, bool blockSmoke, bool baseModule)
	{
		_isUfoDoor = isUfoDoor;
		_stopLOS = stopLOS;
		_isNoFloor = isNoFloor;
		_bigWall = bigWall;
		_isGravLift = isGravLift;
		_isDoor = isDoor;
		_blockFire = blockFire;
		_blockSmoke = blockSmoke;
		_baseModule = baseModule;
	}

	/// <summary>Gets the amount of blockage (0-255) of a certain type.</summary>
	public int GetBlock(ItemDamageType type)
	{
		if (type == ItemDamageType.DT_NONE)
			return _block[1];
		else if (type == ItemDamageType.DT_SMOKE)
			return _block[3];
		else if (type > ItemDamageType.DT_NONE && type < ItemDamageType.DAMAGE_TYPES)
			return _block[2];
		return 0;
	}

	/// <summary>Sets the amount of blockage for all types.</summary>
	public void SetBlockValue(int lightBlock, int visionBlock, int HEBlock, int smokeBlock, int fireBlock, int gasBlock)
	{
		_block[0] = lightBlock; // not used...
		_block[1] = visionBlock == 1 ? 255 : 0;
		_block[2] = HEBlock;
		_block[3] = smokeBlock == 1 ? 256 : 0;
		_block[4] = fireBlock;
		_block[5] = gasBlock;
	}

	/// <summary>Sets the amount of HE blockage.</summary>
	public void SetHEBlock(int HEBlock) => _block[2] = HEBlock;

	/// <summary>Gets/sets the offset on the Y axis (in pixels) when drawing this object.</summary>
	public int YOffset { get => _yOffset; set => _yOffset = value; }

	/// <summary>Gets/sets the type of tile.</summary>
	public TilePart ObjectType { get => _objectType; set => _objectType = value; }

	/// <summary>Gets info about special tile types.</summary>
	public SpecialTileType SpecialType => _specialType;

	/// <summary>Sets a special tile type and object type.</summary>
	public void SetSpecialType(int value, TilePart otype)
	{
		_specialType = (SpecialTileType)value;
		_objectType = otype;
	}

	/// <summary>Gets the TU cost to move over the object.</summary>
	public int GetTUCost(MovementType movementType)
	{
		switch (movementType)
		{
			case MovementType.MT_WALK:
				return _TUWalk;
			case MovementType.MT_FLY:
				return _TUFly;
			case MovementType.MT_SLIDE:
				return _TUSlide;
			default:
				break;
		}
		return 0;
	}

	/// <summary>Sets the TU cost to move over the object.</summary>
	public void SetTUCosts(int walk, int fly, int slide)
	{
		_TUWalk = walk;
		_TUFly = fly;
		_TUSlide = slide;
	}

	/// <summary>Gets/sets the value added to the graphical Y offset of units or objects on this tile.</summary>
	public int TerrainLevel { get => _terrainLevel; set => _terrainLevel = value; }

	/// <summary>Gets/sets the index to the footstep sound.</summary>
	public int FootstepSound { get => _footstepSound; set => _footstepSound = value; }

	/// <summary>Gets/sets the alternative object ID.</summary>
	public int AltMCD { get => _altMCD; set => _altMCD = value; }

	/// <summary>Gets/sets the dead object ID.</summary>
	public int DieMCD { get => _dieMCD; set => _dieMCD = value; }

	/// <summary>
	/// Gets the amount of light the object is emitting (lamp posts have 1, but they should emit more light).
	/// The setter stores the raw MCD value.
	/// </summary>
	public int LightSource
	{
		get
		{
			if (_lightSource == 1)
				return 15;
			else
				return _lightSource - 1;
		}
		set => _lightSource = value;
	}

	/// <summary>Gets/sets the amount of armor.</summary>
	public int Armor { get => _armor; set => _armor = value; }

	/// <summary>Gets/sets the amount of flammable (how flammable this object is).</summary>
	public int Flammable { get => _flammable; set => _flammable = value; }

	/// <summary>Gets/sets the amount of fuel.</summary>
	public int Fuel { get => _fuel; set => _fuel = value; }

	/// <summary>Gets the loft index for a certain layer.</summary>
	public int GetLoftID(int layer) => _loftID[layer];

	/// <summary>Sets the loft index for a certain layer.</summary>
	public void SetLoftID(int loft, int layer) => _loftID[layer] = loft;

	/// <summary>Gets/sets the amount of explosive.</summary>
	public int Explosive { get => _explosive; set => _explosive = value; }

	/// <summary>Gets/sets the type of explosive.</summary>
	public int ExplosiveType { get => _explosiveType; set => _explosiveType = value; }

	/// <summary>Gets/sets the SCANG.DAT index for the minimap.</summary>
	public ushort MiniMapIndex { get => _miniMapIndex; set => _miniMapIndex = value; }

	/// <summary>Sets the TUWalk value.</summary>
	public void SetTUWalk(int TUWalk) => _TUWalk = TUWalk;

	/// <summary>Sets the TUFly value.</summary>
	public void SetTUFly(int TUFly) => _TUFly = TUFly;

	/// <summary>Sets the TUSlide value.</summary>
	public void SetTUSlide(int TUSlide) => _TUSlide = TUSlide;

	/// <summary>Checks if this is an xcom base object.</summary>
	public bool IsBaseModule => _baseModule;

	/// <summary>Sets the "no floor" flag.</summary>
	public void SetNoFloor(bool isNoFloor) => _isNoFloor = isNoFloor;

	/// <summary>Sets the "stops LOS" flag.</summary>
	public void SetStopLOS(bool stopLOS)
	{
		_stopLOS = stopLOS;
		_block[1] = stopLOS ? 255 : 0;
	}
}
