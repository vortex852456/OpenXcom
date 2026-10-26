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
// Ported from: src/Battlescape/TileEngine.h, src/Battlescape/TileEngine.cpp
#nullable disable

global using MapSubset = Acom.AreaSubset<Acom.Position, short>;

using static Acom.ItemDamageType;
using static Acom.LightLayers;
using static Acom.MovementType;
using static Acom.TilePart;
using static Acom.UnitFaction;

namespace Acom;

/// <summary>
/// A utility class that modifies tile properties on a battlescape map. This includes lighting, destruction, smoke, fire, fog of war.
/// Note that this function does not handle any sounds or animations.
/// </summary>
/// <remarks>
/// The port is split into partial files: this one holds the shared state, the generic map helpers and the lighting;
/// <c>TileEngine.Visibility.cs</c> line of sight/fire, <c>TileEngine.Combat.cs</c> reactions, hits and explosions,
/// <c>TileEngine.Terrain.cs</c> blockage, doors, gravity and items.
/// OXCE script hooks are called like in C++; rules without loaded scripts (not loaded from a ruleset) use the
/// built-in default scripts of psi and melee attacks, which are implemented inline.
/// </remarks>
public partial class TileEngine
{
	/// <summary>Value representing non-existing position.</summary>
	public static readonly Position invalid = new(-1, -1, -1);

	/// <summary>Size of tile in voxels.</summary>
	public static readonly Position voxelTileSize = new(Position.TileXY, Position.TileXY, Position.TileZ);

	/// <summary>Half of size of tile in voxels.</summary>
	public static readonly Position voxelTileCenter = new(Position.TileXY / 2, Position.TileXY / 2, Position.TileZ / 2);

	/// <summary>Calculate distance of each step of trajectory.</summary>
	public static float TrajectoryStepSize(List<Position> voxelPath, int pos)
	{
		// C++ takes size_t: a negative index is out of range.
		if ((uint)pos < (uint)voxelPath.Count)
		{
			if (pos > 2)
			{
				return 0.5f * Position.Distance(voxelPath[pos], voxelPath[pos - 2]);
			}
			else if (pos != 0)
			{
				return Position.Distance(voxelPath[1], voxelPath[0]);
			}
		}
		return 0.0f;
	}

	/// <summary>Helper class storing cached visibility blockage data.</summary>
	private struct VisibilityBlockCache
	{
		public uint blockDir;

		public byte bigWall;

		public byte height;
	}

	/// <summary>Helper class storing reaction data (C++ value type, handled by reference like the C++ pointers into the vector).</summary>
	private sealed class ReactionScore
	{
		public BattleUnit unit;
		public BattleItem weapon;
		public BattleActionType attackType;
		public double reactionScore;
		public double reactionReduction;
		public int count;
	}

	private readonly SavedBattleGame _save;
	private readonly List<ushort> _voxelData;

	/// <summary>Cache for tile visibility and light propagation.</summary>
	private readonly VisibilityBlockCache[] _blockVisibility;
	/// <summary>Cache dedicated for speedup for light propagation calculation.</summary>
	private readonly uint[] _lightPropagationTerrainBlocking;
	/// <summary>Cache for marking tiles that need light updated.</summary>
	private readonly uint[] _lightPropagationTempNeedUpdate;

	private readonly RuleInventory _inventorySlotGround;
	private static readonly int[] heightFromCenter = [0, -2, +2, -4, +4, -6, +6, -8, +8, -12, +12];
	private bool _personalLighting;
	private Tile _cacheTile;
	private Tile _cacheTileBelow;
	private Position _cacheTilePos;
	private readonly int _maxViewDistance;        // 20 tiles by default
	private readonly int _maxViewDistanceSq;      // 20 * 20
	private readonly int _maxVoxelViewDistance;   // maxViewDistance * 16
	private readonly int _maxDarknessToSeeUnits;  // 9 by default
	private readonly int _maxStaticLightDistance;
	private readonly int _maxDynamicLightDistance;
	private readonly int _enhancedLighting;
	private Position _eventVisibilitySectorL, _eventVisibilitySectorR, _eventVisibilityObserverPos;
	private readonly List<BattleUnit> _movingUnitPrev = [];
	private BattleUnit _movingUnit;

	/// <summary>Sets up a TileEngine.</summary>
	/// <param name="save">Pointer to SavedBattleGame object.</param>
	/// <param name="mod">Mod with voxel data, view distances and lighting limits.</param>
	public TileEngine(SavedBattleGame save, Mod mod)
	{
		_save = save;
		_voxelData = mod.VoxelData;
		_inventorySlotGround = mod.InventoryGround;
		_personalLighting = true;
		_cacheTile = null;
		_cacheTileBelow = null;
		_maxViewDistance = mod.MaxViewDistance;
		_maxViewDistanceSq = _maxViewDistance * _maxViewDistance;
		_maxVoxelViewDistance = _maxViewDistance * 16;
		_maxDarknessToSeeUnits = mod.MaxDarknessToSeeUnits;
		_maxStaticLightDistance = mod.MaxStaticLightDistance;
		_maxDynamicLightDistance = mod.MaxDynamicLightDistance;
		_enhancedLighting = mod.EnhancedLighting;

		_blockVisibility = new VisibilityBlockCache[save.MapSizeXYZ];
		_lightPropagationTerrainBlocking = new uint[save.MapSizeXYZ];
		_lightPropagationTempNeedUpdate = new uint[save.MapSizeXYZ];
		_cacheTilePos = invalid;

		if (Options.oxceTogglePersonalLightType == 2)
		{
			// persisted per campaign
			SavedGame geosave = _save.GeoscapeSave;
			if (geosave != null)
			{
				_personalLighting = geosave.TogglePersonalLight;
			}
		}
		else if (Options.oxceTogglePersonalLightType == 1)
		{
			// persisted per battle
			_personalLighting = _save.TogglePersonalLight;
		}

		_save.SetTogglePersonalLightTemp(_personalLighting);
	}

	/// <summary>Get max distance that fire light can reach.</summary>
	public int MaxStaticLightDistance { get => _maxStaticLightDistance; }
	/// <summary>Get max distance that light can reach.</summary>
	public int MaxDynamicLightDistance { get => _maxDynamicLightDistance; }
	/// <summary>Get flags for enhanced lighting.</summary>
	public int EnhancedLighting { get => _enhancedLighting; }
	/// <summary>Get max view distance.</summary>
	public int MaxViewDistance { get => _maxViewDistance; }
	/// <summary>Get square of max view distance.</summary>
	public int MaxViewDistanceSq { get => _maxViewDistanceSq; }
	/// <summary>Get max view distance in voxel space.</summary>
	public int MaxVoxelViewDistance { get => _maxVoxelViewDistance; }
	/// <summary>Get threshold of darkness for LoS calculation.</summary>
	public int MaxDarknessToSeeUnits { get => _maxDarknessToSeeUnits; }

	// fmath.h helpers

	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	private static float Round(float x) => x < 0.0f ? MathF.Ceiling(x - 0.5f) : MathF.Floor(x + 0.5f);

	private static double Deg2Rad(double deg) => deg * Math.PI / 180.0;

	////////////////////////////////////////////////////////////
	//					generic helpers
	////////////////////////////////////////////////////////////

	/// <summary>
	/// Calculates a line trajectory, using bresenham algorithm in 3D.
	/// </summary>
	/// <param name="origin">Origin.</param>
	/// <param name="target">Target.</param>
	/// <param name="posFunc">Function call for each step in primary direction of line.</param>
	/// <param name="driftFunc">Function call for each side step of line.</param>
	/// <returns>True if one of the callbacks stopped the line.</returns>
	internal static bool CalculateLineHelper(Position origin, Position target, Func<Position, bool> posFunc, Func<Position, bool> driftFunc)
	{
		int x, x0, x1, delta_x, step_x;
		int y, y0, y1, delta_y, step_y;
		int z, z0, z1, delta_z, step_z;
		bool swap_xy, swap_xz;
		int drift_xy, drift_xz;
		int cx, cy, cz;

		//start and end points
		x0 = origin.x; x1 = target.x;
		y0 = origin.y; y1 = target.y;
		z0 = origin.z; z1 = target.z;

		//'steep' xy Line, make longest delta x plane
		swap_xy = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
		if (swap_xy)
		{
			(x0, y0) = (y0, x0);
			(x1, y1) = (y1, x1);
		}

		//do same for xz
		swap_xz = Math.Abs(z1 - z0) > Math.Abs(x1 - x0);
		if (swap_xz)
		{
			(x0, z0) = (z0, x0);
			(x1, z1) = (z1, x1);
		}

		//delta is Length in each plane
		delta_x = Math.Abs(x1 - x0);
		delta_y = Math.Abs(y1 - y0);
		delta_z = Math.Abs(z1 - z0);

		//drift controls when to step in 'shallow' planes
		//starting value keeps Line centred
		drift_xy = (delta_x / 2);
		drift_xz = (delta_x / 2);

		//direction of line
		step_x = 1; if (x0 > x1) { step_x = -1; }
		step_y = 1; if (y0 > y1) { step_y = -1; }
		step_z = 1; if (z0 > z1) { step_z = -1; }

		//starting point
		y = y0;
		z = z0;

		//step through longest delta (which we have swapped to x)
		for (x = x0; ; x += step_x)
		{
			//copy position
			cx = x; cy = y; cz = z;

			//unswap (in reverse)
			if (swap_xz) (cx, cz) = (cz, cx);
			if (swap_xy) (cx, cy) = (cy, cx);
			if (posFunc(new Position(cx, cy, cz)))
			{
				return true;
			}

			if (x == x1) break;

			//update progress in other planes
			drift_xy = drift_xy - delta_y;
			drift_xz = drift_xz - delta_z;

			//step in y plane
			if (drift_xy < 0)
			{
				y = y + step_y;
				drift_xy = drift_xy + delta_x;

				cx = x; cz = z; cy = y;
				if (swap_xz) (cx, cz) = (cz, cx);
				if (swap_xy) (cx, cy) = (cy, cx);
				if (driftFunc(new Position(cx, cy, cz)))
				{
					return true;
				}
			}

			//same in z
			if (drift_xz < 0)
			{
				z = z + step_z;
				drift_xz = drift_xz + delta_x;

				cx = x; cz = z; cy = y;
				if (swap_xz) (cx, cz) = (cz, cx);
				if (swap_xy) (cx, cy) = (cy, cx);
				if (driftFunc(new Position(cx, cy, cz)))
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Calculates a parabola trajectory, calling <paramref name="posFunc"/> for each point until it returns true or the arc reaches the ground.
	/// </summary>
	internal static bool CalculateParabolaHelper(Position origin, Position target, double curvature, Position delta, Func<Position, bool> posFunc)
	{
		double ro = Position.Distance(target, origin);

		if (Target.AreSame(ro, 0.0)) return false;

		double fi = Math.Acos((double)(target.z - origin.z) / ro);
		double te = Math.Atan2((double)(target.y - origin.y), (double)(target.x - origin.x));

		te += (delta.x / ro) / 2 * Math.PI; //horizontal magic value
		fi += ((delta.z + delta.y) / ro) / 14 * Math.PI * curvature; //another magic value (vertical), to make it in line with fire spread

		double zA = Math.Sqrt(ro) * curvature;
		double zK = 4.0 * zA / ro / ro;

		int x = origin.x;
		int y = origin.y;
		int z = origin.z;
		int i = 8;

		while (z > 0)
		{
			x = (int)((double)origin.x + (double)i * Math.Cos(te) * Math.Sin(fi));
			y = (int)((double)origin.y + (double)i * Math.Sin(te) * Math.Sin(fi));
			z = (int)((double)origin.z + (double)i * Math.Cos(fi) - zK * ((double)i - ro / 2.0) * ((double)i - ro / 2.0) + zA);

			if (posFunc(new Position(x, y, z)))
			{
				return true;
			}

			++i;
		}

		return false;
	}

	/// <summary>Iterate through some subset of map tiles.</summary>
	/// <param name="save">Map data.</param>
	/// <param name="gs">Square subset of map area.</param>
	/// <param name="func">Call back with tile and its index.</param>
	/// <remarks>C++ walks a tile pointer through the contiguous tile array; the index walks the same way.</remarks>
	private static void IterateTilesIndexed(SavedBattleGame save, MapSubset gs, Action<Tile, int> func)
	{
		int totalSizeX = save.MapSizeX;
		int totalSizeY = save.MapSizeY;
		int totalSizeZ = save.MapSizeZ;

		gs = MapSubset.Intersection(gs, new MapSubset(totalSizeX, totalSizeY));
		if (gs.ToBoolean())
		{
			for (int z = 0; z < totalSizeZ; ++z)
			{
				var posStart = new Position(gs.beg_x, gs.beg_y, z);
				int indexStart = save.GetTileIndex(posStart);
				for (int stepsY = gs.SizeY(); stepsY != 0; --stepsY, indexStart += totalSizeX)
				{
					int index = indexStart;
					for (int stepX = gs.SizeX(); stepX != 0; --stepX, index += 1)
					{
						func(save.GetTile(index), index);
					}
				}
			}
		}
	}

	/// <summary>Iterate through some subset of map tiles.</summary>
	private static void IterateTiles(SavedBattleGame save, MapSubset gs, Action<Tile> func)
	{
		IterateTilesIndexed(save, gs, (tile, index) => func(tile));
	}

	/// <summary>Iterate through the tile indexes of some subset of map tiles.</summary>
	private static void IterateTileIndexes(SavedBattleGame save, MapSubset gs, Action<int> func)
	{
		int totalSizeX = save.MapSizeX;
		int totalSizeY = save.MapSizeY;
		int totalSizeZ = save.MapSizeZ;

		gs = MapSubset.Intersection(gs, new MapSubset(totalSizeX, totalSizeY));
		if (gs.ToBoolean())
		{
			for (int z = 0; z < totalSizeZ; ++z)
			{
				int indexStart = save.GetTileIndex(new Position(gs.beg_x, gs.beg_y, z));
				for (int stepsY = gs.SizeY(); stepsY != 0; --stepsY, indexStart += totalSizeX)
				{
					int index = indexStart;
					for (int stepX = gs.SizeX(); stepX != 0; --stepX, index += 1)
					{
						func(index);
					}
				}
			}
		}
	}

	/// <summary>Generate square subset of map using position and radius.</summary>
	/// <param name="position">Starting position.</param>
	/// <param name="radius">Radius of area.</param>
	/// <returns>Subset of map.</returns>
	internal static MapSubset MapArea(Position position, int radius)
	{
		return new MapSubset((position.x - radius, position.x + radius + 1), (position.y - radius, position.y + radius + 1));
	}

	internal static MapSubset MapAreaExpand(MapSubset gs, int radius)
	{
		return new MapSubset((gs.beg_x - radius, gs.end_x + radius), (gs.beg_y - radius, gs.end_y + radius));
	}

	private const int MaskBlockDirMul = 9;
	private const int MaskBlockDirOffset = MaskBlockDirMul + 1;

	/// <summary>Calculate byte mask that is used to access cached data.</summary>
	/// <param name="dir">Direction for 0 to 7 or -1 as no direction when we check direct up or down direction.</param>
	/// <param name="z">Value +1 as up, -1 as down, 0 as same level.</param>
	/// <returns>Mask corresponding given direction and level.</returns>
	internal static uint SelectBit(int dir, int z)
	{
		return 1u << (MaskBlockDirOffset + MaskBlockDirMul * z + dir);
	}

	internal static readonly uint MaskBlockDown = SelectBit(-1, -1);
	internal static readonly uint MaskBlockUp = SelectBit(-1, +1);

	internal static readonly uint MaskFire = SelectBit(+7, +1) << 1;
	internal static readonly uint MaskSmoke = SelectBit(+7, +1) << 2;

	private static bool GetBlockDir(in VisibilityBlockCache td, int dir, int z) => (td.blockDir & SelectBit(dir, z)) != 0;
	private static void AddBlockDir(ref VisibilityBlockCache td, int dir, int z, bool p) { if (p) td.blockDir |= SelectBit(dir, z); }

	private static bool GetBlockUp(in VisibilityBlockCache td) => (td.blockDir & MaskBlockUp) != 0;
	private static void AddBlockUp(ref VisibilityBlockCache td, bool p) { if (p) td.blockDir |= MaskBlockUp; }

	private static void AddBlockDown(ref VisibilityBlockCache td, bool p) { if (p) td.blockDir |= MaskBlockDown; }

	private static bool GetFire(in VisibilityBlockCache td) => (td.blockDir & MaskFire) != 0;
	private static void AddFire(ref VisibilityBlockCache td, bool p) { if (p) td.blockDir |= MaskFire; }

	private static bool GetSmoke(in VisibilityBlockCache td) => (td.blockDir & MaskSmoke) != 0;
	private static void AddSmoke(ref VisibilityBlockCache td, bool p) { if (p) td.blockDir |= MaskSmoke; }

	private static bool GetBigWallDir(in VisibilityBlockCache td, int dir) => (td.bigWall & (1u << dir)) != 0;
	private static void AddBigWallDir(ref VisibilityBlockCache td, int dir, bool p) { if (p) td.bigWall |= (byte)(1u << dir); }

	////////////////////////////////////////////////////////////
	//					lighting
	////////////////////////////////////////////////////////////

	/// <summary>Calculates sun shading for the whole terrain.</summary>
	private void CalculateSunShading(MapSubset gs)
	{
		int power = 15 - _save.GlobalShade;

		IterateTiles(
			_save,
			gs,
			tile =>
			{
				int currLight = power;

				// At night/dusk sun isn't dropping shades blocked by roofs
				if (_save.GlobalShade <= 4)
				{
					int block = 0;
					int x = tile.Position.x;
					int y = tile.Position.y;
					for (int z = _save.MapSizeZ - 1; z > tile.Position.z; z--)
					{
						block += Blockage(_save.GetTile(new Position(x, y, z)), O_FLOOR, DT_NONE);
						block += Blockage(_save.GetTile(new Position(x, y, z)), O_OBJECT, DT_NONE, Pathfinding.DIR_DOWN);
					}
					if (block > 0)
					{
						currLight -= 2;
					}
				}
				tile.AddLight(currLight, LL_AMBIENT);
			}
		);
	}

	/// <summary>Amount of light a fire generates from tile.</summary>
	private const int fireLightPower = 15;

	/// <summary>Amount of light a fire generates from unit.</summary>
	private const int unitFireLightPower = 15;

	/// <summary>Amount of light a fire generates from stunned unit.</summary>
	private const int unitFireLightPowerStunned = 10;

	/// <summary>Recalculates lighting for the terrain: fire.</summary>
	private void CalculateTerrainBackground(MapSubset gs)
	{
		// add lighting of fire
		IterateTiles(
			_save,
			MapAreaExpand(gs, MaxStaticLightDistance - 1),
			tile =>
			{
				int currLight = 0;

				if (tile.GetMapData(O_FLOOR) != null)
				{
					currLight = Math.Max(currLight, tile.GetMapData(O_FLOOR).LightSource);
				}
				if (tile.GetMapData(O_OBJECT) != null)
				{
					currLight = Math.Max(currLight, tile.GetMapData(O_OBJECT).LightSource);
				}
				if (tile.GetMapData(O_WESTWALL) != null)
				{
					currLight = Math.Max(currLight, tile.GetMapData(O_WESTWALL).LightSource);
				}
				if (tile.GetMapData(O_NORTHWALL) != null)
				{
					currLight = Math.Max(currLight, tile.GetMapData(O_NORTHWALL).LightSource);
				}

				// fires
				if (tile.Fire != 0)
				{
					currLight = Math.Max(currLight, unitFireLightPower);
				}

				if (currLight >= MaxStaticLightDistance)
				{
					currLight = MaxStaticLightDistance - 1;
				}
				AddLight(gs, tile.Position, currLight, LL_FIRE);
			}
		);
	}

	/// <summary>Recalculates lighting for the terrain: objects, items.</summary>
	private void CalculateTerrainItems(MapSubset gs)
	{
		// add lighting of terrain
		IterateTiles(
			_save,
			MapAreaExpand(gs, MaxDynamicLightDistance - 1),
			tile =>
			{
				int currLight = 0;

				foreach (BattleItem bi in tile.Inventory)
				{
					if (bi.Glow)
					{
						currLight = Math.Max(currLight, bi.GlowRange);
					}

					BattleUnit bu = bi.Unit;
					if (bu != null && bu.Fire != 0)
					{
						currLight = Math.Max(currLight, unitFireLightPowerStunned);
					}
				}

				if (currLight >= MaxDynamicLightDistance)
				{
					currLight = MaxDynamicLightDistance - 1;
				}
				AddLight(gs, tile.Position, currLight, LL_ITEMS);
			}
		);
	}

	/// <summary>Recalculates lighting for the units.</summary>
	private void CalculateUnitLighting(MapSubset gs)
	{
		foreach (BattleUnit unit in _save.Units)
		{
			if (unit.IsOut)
			{
				continue;
			}

			int currLight = 0;
			// add lighting of unit
			if (unit.Faction == FACTION_PLAYER)
			{
				currLight = Math.Max(currLight, _personalLighting ? unit.Armor.PersonalLightFriend : 0);
			}
			else if (unit.Faction == FACTION_HOSTILE)
			{
				currLight = Math.Max(currLight, unit.Armor.PersonalLightHostile);
			}
			else if (unit.Faction == FACTION_NEUTRAL)
			{
				currLight = Math.Max(currLight, unit.Armor.PersonalLightNeutral);
			}

			BattleItem[] handWeapons = [unit.LeftHandWeapon, unit.RightHandWeapon];
			foreach (BattleItem w in handWeapons)
			{
				if (w == null) continue;

				if (w.Glow)
				{
					currLight = Math.Max(currLight, w.GlowRange);
				}

				BattleUnit u = w.Unit;
				if (u != null && u.Fire != 0)
				{
					currLight = Math.Max(currLight, unitFireLightPowerStunned);
				}
			}
			// add lighting of units on fire
			if (unit.Fire != 0)
			{
				currLight = Math.Max(currLight, unitFireLightPower);
			}

			if (currLight >= MaxDynamicLightDistance)
			{
				currLight = MaxDynamicLightDistance - 1;
			}
			int size = unit.Armor.Size;
			Position pos = unit.Position;
			for (int x = 0; x < size; ++x)
			{
				for (int y = 0; y < size; ++y)
				{
					AddLight(gs, pos + new Position(x, y, 0), currLight, LL_UNITS);
				}
			}
		}
	}

	/// <summary>Recalculate all lighting in the whole map (C++ default arguments).</summary>
	public void CalculateLighting(LightLayers layer)
	{
		CalculateLighting(layer, invalid);
	}

	/// <summary>Recalculate all lighting in some area.</summary>
	public void CalculateLighting(LightLayers layer, Position position, int eventRadius = 0, bool terrianChanged = false)
	{
		var gsMap = new MapSubset(_save.MapSizeX, _save.MapSizeY);
		MapSubset gsDynamic = gsMap;
		MapSubset gsStatic = gsDynamic;

		if (position != invalid)
		{
			gsDynamic = MapArea(position, eventRadius + MaxDynamicLightDistance);
			gsStatic = MapArea(position, eventRadius + MaxStaticLightDistance);
		}

		if (terrianChanged)
		{
			IterateTiles(
				_save,
				position != invalid ? MapArea(position, eventRadius + 1) : gsMap,
				tile =>
				{
					Position currPos = tile.Position;
					int index = _save.GetTileIndex(currPos);
					MapData mapData = tile.GetMapData(O_OBJECT);
					ref VisibilityBlockCache cache = ref _blockVisibility[index];

					cache = default;
					cache.height = unchecked((byte)(-tile.TerrainLevel));
					if (mapData != null)
					{
						if (mapData.GetTUCost(MT_WALK) == Pathfinding.INVALID_MOVE_COST)
						{
							cache.height = 24;
						}
					}
					AddSmoke(ref cache, tile.Smoke > 0);
					AddFire(ref cache, tile.Fire > 0);
					AddBlockUp(ref cache, VerticalBlockage(tile, _save.GetAboveTile(tile), DT_NONE) > 127);
					AddBlockDown(ref cache, VerticalBlockage(tile, _save.GetBelowTile(tile), DT_NONE) > 127);
					for (int dir = 0; dir < 8; ++dir)
					{
						Position pos = default;
						Pathfinding.DirectionToVector(dir, ref pos);
						Tile tileNext = _save.GetTile(currPos + pos);
						int result = 0;

						result = HorizontalBlockage(tile, tileNext, DT_NONE, true);
						AddBigWallDir(ref cache, dir, (result == -1));

						result = HorizontalBlockage(tile, tileNext, DT_NONE);
						AddBlockDir(ref cache, dir, 0, (result > 127 || result == -1));

						tileNext = _save.GetTile(currPos + pos + new Position(0, 0, 1));
						AddBlockDir(ref cache, dir, 1, VerticalBlockage(tile, tileNext, DT_NONE) > 127);

						tileNext = _save.GetTile(currPos + pos + new Position(0, 0, -1));
						AddBlockDir(ref cache, dir, -1, VerticalBlockage(tile, tileNext, DT_NONE) > 127);
					}

					_lightPropagationTerrainBlocking[index] = cache.blockDir;
					//HACK: some times light can lit wall objects even if its can't propagate through them,
					// for simplicity we consider them transparent.
					// But if is already big wall then next step to big wall is blocked.
					if (tile.GetMapData(O_OBJECT) == null || tile.GetMapData(O_OBJECT).BigWall != 0)
					{
						for (int dir = 0; dir < 8; ++dir)
						{
							Position pos = default;
							Pathfinding.DirectionToVector(dir, ref pos);
							Tile tileNext = _save.GetTile(currPos + pos);
							int result = 0;

							result = HorizontalBlockage(tile, tileNext, DT_NONE, true);
							if (result == -1)
							{
								_lightPropagationTerrainBlocking[index] &= ~SelectBit(dir, 0);
							}
						}
					}
				}
			);
		}

		IterateTilesLightMaxBound(_save, position, eventRadius, MaxDynamicLightDistance, gsMap, _lightPropagationTempNeedUpdate, _lightPropagationTerrainBlocking);

		if (layer <= LL_FIRE)
		{
			IterateTilesIndexed(
				_save,
				gsStatic,
				(tile, index) =>
				{
					if (_lightPropagationTempNeedUpdate[index] != 0) tile.ResetLightMulti(layer);
				}
			);
		}

		IterateTilesIndexed(
			_save,
			gsDynamic,
			(tile, index) =>
			{
				if (_lightPropagationTempNeedUpdate[index] != 0) tile.ResetLightMulti(layer > LL_ITEMS ? layer : LL_ITEMS);
			}
		);

		if (layer <= LL_AMBIENT) CalculateSunShading(gsStatic);
		if (layer <= LL_FIRE) CalculateTerrainBackground(gsStatic);
		if (layer <= LL_ITEMS) CalculateTerrainItems(gsDynamic);
		if (layer <= LL_UNITS) CalculateUnitLighting(gsDynamic);
	}

	/// <summary>Adds circular light pattern starting from center and losing power with distance travelled.</summary>
	/// <param name="gs">Area that is updated.</param>
	/// <param name="center">Center.</param>
	/// <param name="power">Power.</param>
	/// <param name="layer">Light is separated in 4 layers: Ambient, Tiles, Items, Units.</param>
	private void AddLight(MapSubset gs, Position center, int power, LightLayers layer)
	{
		if (power <= 0)
		{
			return;
		}

		bool fire = layer == LL_FIRE;
		bool items = layer == LL_ITEMS;
		bool units = layer == LL_UNITS;
		bool ground = items || fire;
		int tileHeight = _save.GetTile(center).TerrainLevel;
		int divide = (fire ? 8 : 4);
		Position accuracy = voxelTileSize / divide;
		Position offsetCenter = (accuracy / 2 + new Position(-1, -1, (ground ? 0 : accuracy.z / 4) - tileHeight * accuracy.z / 24));
		Position offsetTarget = (accuracy / 2 + new Position(-1, -1, 0));
		bool clasicLighting = (EnhancedLighting & ((fire ? 1 : 0) | (items ? 2 : 0) | (units ? 4 : 0))) == 0;
		short topTargetVoxel = unchecked((short)(_save.MapSizeZ * accuracy.z - 1));
		short topCenterVoxel = unchecked((short)((GetBlockUp(_blockVisibility[_save.GetTileIndex(center)]) ? (center.z + 1) : _save.MapSizeZ) * accuracy.z - 1));
		int maxFirePower = Math.Min(15, MaxStaticLightDistance - 1);
		MapSubset gsInter = MapSubset.Intersection(gs, MapArea(center, power - 1));

		IterateTilesIndexed(
			_save,
			gsInter,
			(tile, idx) =>
			{
				Position target = tile.Position;
				Position diff = target - center;
				int distance = (int)Round(Position.Distance(target.ToVoxel(), center.ToVoxel()) / Position.TileXY);
				int targetLight = tile.GetLightMulti(layer);
				int currLight = power - distance;

				if (currLight <= targetLight)
				{
					return;
				}
				if (clasicLighting)
				{
					tile.AddLight(currLight, layer);
					return;
				}
				if (_lightPropagationTempNeedUpdate[idx] == 0)
				{
					return;
				}

				Position startVoxel = (center * accuracy) + offsetCenter;
				Position endVoxel = (target * accuracy) + offsetTarget + new Position(0, 0, Math.Max(0, (_blockVisibility[_save.GetTileIndex(target)].height - 1) / (2 * divide)));
				var offsetA = new Position(1, 0, 0);
				var offsetB = new Position(-1, 1, 0);
				if ((diff.x > 0) ^ (diff.y > 0))
				{
					offsetA = new Position(1, 1, 0);
					offsetB = new Position(-1, -1, 0);
				}

				startVoxel += offsetA;
				endVoxel += offsetA;
				Position lastTileA = center;
				Position lastTileB = center;
				int stepsA = 0;
				int stepsB = 0;
				int lightA = currLight;
				int lightB = currLight;

				//Do not peek out your head outside map
				startVoxel.z = Math.Min(startVoxel.z, topCenterVoxel);
				endVoxel.z = Math.Min(endVoxel.z, topTargetVoxel);

				bool CalculateBlock(Position point, ref Position lastPoint, ref int light, ref int steps)
				{
					int height = (point.z % accuracy.z) * divide;
					point = point / accuracy;
					if (light <= 0)
					{
						return true;
					}
					if (point == lastPoint)
					{
						return false;
					}

					Position difference = point - lastPoint;
					int dir = Pathfinding.VectorToDirection(difference);
					VisibilityBlockCache cache = _blockVisibility[_save.GetTileIndex(lastPoint)];

					bool result = GetBlockDir(cache, dir, difference.z);
					if (result && difference.z == 0 && GetBigWallDir(cache, dir))
					{
						if (point == target)
						{
							result = false;
						}
					}

					if (steps > 1)
					{
						if (GetFire(cache) && fire && light <= maxFirePower) //some tile on path have fire, skip further calculation because destination tile should be lighted by this fire.
						{
							result = true;
						}
						else if (GetSmoke(cache))
						{
							light -= 1;
						}
						if (height < cache.height)
						{
							light -= 2;
						}
					}
					++steps;
					lastPoint = point;
					if (result || light < targetLight)
					{
						light = 0;
						return true;
					}
					return false;
				}

				CalculateLineHelper(startVoxel, endVoxel,
					voxel =>
					{
						bool resultA = CalculateBlock(voxel, ref lastTileA, ref lightA, ref stepsA);
						bool resultB = CalculateBlock(voxel + offsetB, ref lastTileB, ref lightB, ref stepsB);
						return resultA && resultB;
					},
					voxel =>
					{
						return false;
					}
				);

				currLight = (lightA + lightB) / 2;
				if (currLight > targetLight)
				{
					tile.AddLight(currLight, layer);
				}
			}
		);
	}

	/// <summary>Toggles personal lighting on / off.</summary>
	public void TogglePersonalLighting()
	{
		_personalLighting = !_personalLighting;

		if (Options.oxceTogglePersonalLightType == 2)
		{
			// persisted per campaign
			SavedGame geosave = _save.GeoscapeSave;
			if (geosave != null)
			{
				geosave.TogglePersonalLight = _personalLighting;
			}
		}
		else if (Options.oxceTogglePersonalLightType == 1)
		{
			// persisted per battle
			_save.TogglePersonalLight = _personalLighting;
		}

		_save.SetTogglePersonalLightTemp(_personalLighting);
		CalculateLighting(LL_UNITS);
		RecalculateFOV();
	}
}
