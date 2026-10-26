using System.Reflection;
using Acom.Tests.Savegame;
using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Battlescape;

/// <summary>
/// Pathfinding on small synthetic one-level maps (floor TU cost 4, blocking objects/walls with TU cost 255).
/// The maps are built so that the cheapest path is unique, so the expected directions follow from the C++ rules.
/// </summary>
public class PathfindingMapTests
{
	private const int N = 0, NE = 1, E = 2, SE = 3, S = 4;

	private static MapData Part(TilePart type, int tuCost)
	{
		MapData md = TileTests.MapDataWithFlags(objectType: type);
		md.SetTUCosts(tuCost, tuCost, tuCost);
		return md;
	}

	/// <summary>A map with a walkable floor everywhere; <paramref name="blocked"/> tiles get a blocking object.</summary>
	private static SavedBattleGame NewMap(int x, int y, params (int x, int y)[] blocked)
	{
		var save = new SavedBattleGame(null, new Language());
		save.InitMap(x, y, 1);
		MapData floor = Part(TilePart.O_FLOOR, 4);
		MapData block = Part(TilePart.O_OBJECT, Pathfinding.INVALID_MOVE_COST);
		for (int i = 0; i < save.MapSizeXYZ; ++i)
		{
			save.GetTile(i).SetMapData(floor, 0, 0, TilePart.O_FLOOR);
		}
		foreach (var (bx, by) in blocked)
		{
			save.GetTile(new Position(bx, by, 0)).SetMapData(block, 1, 0, TilePart.O_OBJECT);
		}
		return save;
	}

	private static void SetField<T>(object obj, string name, T value) =>
		obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(obj, value);

	/// <summary>A player unit with default (100%) move cost modifiers, like after UpdateArmorFromNonSoldier.</summary>
	private static BattleUnit Unit(Position pos, int size = 1, int tu = 60, int energy = 90)
	{
		var armor = new Armor("STR_TEST_ARMOR", 0);
		SetField(armor, "_size", size);
		var unit = new BattleUnit(UnitFaction.FACTION_PLAYER, 1, armor, null, null, new BattleUnitStatistics());
		SetField(unit, "_movementType", armor.MovementType);
		SetField(unit, "_moveCostBase", armor.MoveCostBase);
		SetField(unit, "_moveCostBaseFly", armor.MoveCostBaseFly);
		SetField(unit, "_moveCostBaseClimb", armor.MoveCostBaseClimb);
		SetField(unit, "_moveCostBaseNormal", armor.MoveCostBaseNormal);
		SetField(unit, "_tu", tu);
		SetField(unit, "_energy", energy);
		unit.SetPosition(pos);
		return unit;
	}

	private static List<int> Dequeue(Pathfinding pf)
	{
		var dirs = new List<int>();
		for (int d = pf.DequeuePath(); d != -1; d = pf.DequeuePath())
		{
			dirs.Add(d);
		}
		return dirs;
	}

	private static Position Walk(Position start, List<int> dirs)
	{
		foreach (int d in dirs)
		{
			Position v = default;
			Pathfinding.DirectionToVector(d, ref v);
			start += v;
		}
		return start;
	}

	[Fact]
	public void Step_costs_follow_floor_cost_diagonal_bonus_and_armor_percentages()
	{
		var save = NewMap(4, 4);
		var pf = new Pathfinding(save);
		var unit = Unit(new Position(1, 1, 0));

		PathfindingStep straight = pf.GetTUCost(unit.Position, E, unit, null, BattleActionMove.BAM_NORMAL);
		Assert.Equal((4, 2), ((int)straight.cost.time, (int)straight.cost.energy)); // walk: 100% TU, 50% energy
		Assert.Equal(new Position(2, 1, 0), straight.pos);

		PathfindingStep diagonal = pf.GetTUCost(unit.Position, SE, unit, null, BattleActionMove.BAM_NORMAL);
		Assert.Equal((6, 3), ((int)diagonal.cost.time, (int)diagonal.cost.energy)); // (int)(4 * 1.5)

		PathfindingStep run = pf.GetTUCost(unit.Position, E, unit, null, BattleActionMove.BAM_RUN);
		Assert.Equal((3, 3), ((int)run.cost.time, (int)run.cost.energy)); // run: 75% / 75%

		// no grav lift and not flying: up/down is impossible, as is leaving the map
		Assert.Equal(Pathfinding.INVALID_MOVE_COST, pf.GetTUCost(unit.Position, Pathfinding.DIR_UP, unit, null, BattleActionMove.BAM_NORMAL).cost.time);
		Assert.Equal(Pathfinding.INVALID_MOVE_COST, pf.GetTUCost(new Position(0, 0, 0), N, unit, null, BattleActionMove.BAM_NORMAL).cost.time);
	}

	[Fact]
	public void Straight_line_uses_bresenham_and_sums_the_cost()
	{
		var save = NewMap(3, 5, (1, 0), (1, 1), (1, 2), (1, 3));
		var pf = new Pathfinding(save);
		var unit = Unit(new Position(0, 0, 0));

		pf.Calculate(unit, new Position(0, 3, 0), BattleActionMove.BAM_NORMAL);
		Assert.Equal(12, pf.TotalTUCost);
		Assert.Equal(S, pf.StartDirection);
		Assert.Equal([S, S, S], Dequeue(pf));
	}

	[Fact]
	public void A_star_finds_the_only_way_around_a_blocking_column()
	{
		// x = 1 is blocked except at the bottom row:
		//   S # E
		//   . # .
		//   . # .
		//   . # .
		//   . . .
		var save = NewMap(3, 5, (1, 0), (1, 1), (1, 2), (1, 3));
		var pf = new Pathfinding(save);
		var unit = Unit(new Position(0, 0, 0));

		pf.Calculate(unit, new Position(2, 0, 0), BattleActionMove.BAM_NORMAL);
		Assert.Equal([S, S, S, SE, NE, N, N, N], Dequeue(pf));

		// a blocked destination gives no path at all
		pf.Calculate(unit, new Position(1, 0, 0), BattleActionMove.BAM_NORMAL);
		Assert.Empty(pf.Path);
		// so does a destination outside the map
		pf.Calculate(unit, new Position(3, 0, 0), BattleActionMove.BAM_NORMAL);
		Assert.Empty(pf.Path);
	}

	[Fact]
	public void Max_tu_cost_limits_the_search()
	{
		var save = NewMap(3, 5, (1, 0), (1, 1), (1, 2), (1, 3));
		var pf = new Pathfinding(save);
		var unit = Unit(new Position(0, 0, 0));

		pf.Calculate(unit, new Position(2, 0, 0), BattleActionMove.BAM_NORMAL, null, 35);
		Assert.Empty(pf.Path);
		pf.Calculate(unit, new Position(2, 0, 0), BattleActionMove.BAM_NORMAL, null, 36);
		Assert.Equal(8, pf.Path.Count);
	}

	[Fact]
	public void Walls_block_their_side_including_diagonals()
	{
		var save = NewMap(3, 3);
		save.GetTile(new Position(1, 1, 0)).SetMapData(Part(TilePart.O_NORTHWALL, Pathfinding.INVALID_MOVE_COST), 2, 0, TilePart.O_NORTHWALL);
		var pf = new Pathfinding(save);
		var unit = Unit(new Position(1, 1, 0));

		Assert.True(pf.IsBlockedDirection(unit, save.GetTile(new Position(1, 1, 0)), N));
		Assert.True(pf.IsBlockedDirection(unit, save.GetTile(new Position(1, 0, 0)), S));
		Assert.True(pf.IsBlockedDirection(unit, save.GetTile(new Position(0, 1, 0)), NE)); // corner of the wall
		Assert.False(pf.IsBlockedDirection(unit, save.GetTile(new Position(1, 1, 0)), E));
		Assert.Equal(Pathfinding.INVALID_MOVE_COST, pf.GetTUCost(new Position(1, 1, 0), N, unit, null, BattleActionMove.BAM_NORMAL).cost.time);

		// the way north goes around the wall with three straight steps (the diagonals touch the wall)
		pf.Calculate(unit, new Position(1, 0, 0), BattleActionMove.BAM_NORMAL);
		List<int> dirs = Dequeue(pf);
		Assert.Equal(3, dirs.Count);
		Assert.DoesNotContain(dirs, d => (d & 1) != 0);
		Assert.Equal(new Position(1, 0, 0), Walk(new Position(1, 1, 0), dirs));
	}

	[Fact]
	public void Reachable_tiles_are_limited_by_time_units()
	{
		var save = NewMap(5, 5);
		var pf = new Pathfinding(save);
		var unit = Unit(new Position(2, 2, 0), tu: 8);

		List<int> tiles = pf.FindReachable(unit, Uninitialized<BattleActionCost>());
		Assert.Equal(save.GetTileIndex(new Position(2, 2, 0)), tiles[0]);
		var expected = new HashSet<Position>
		{
			new(2, 2, 0),
			new(1, 2, 0), new(3, 2, 0), new(2, 1, 0), new(2, 3, 0), // 4 TU
			new(1, 1, 0), new(3, 1, 0), new(1, 3, 0), new(3, 3, 0), // 6 TU
			new(0, 2, 0), new(4, 2, 0), new(2, 0, 0), new(2, 4, 0), // 8 TU
		};
		Assert.Equal(expected, tiles.Select(save.GetTileCoords).ToHashSet());
		Assert.Equal(expected.Count, tiles.Count);
	}

	[Fact]
	public void Large_units_need_a_gap_of_two_tiles()
	{
		// column x = 3 blocked except the bottom row
		var save = NewMap(6, 6, (3, 0), (3, 1), (3, 2), (3, 3), (3, 4));
		var pf = new Pathfinding(save);
		var tank = Unit(new Position(0, 0, 0), size: 2);

		pf.Calculate(tank, new Position(4, 0, 0), BattleActionMove.BAM_NORMAL);
		Assert.Empty(pf.Path);
		// the target is too close to the edge of the map for a 2x2 unit
		pf.Calculate(tank, new Position(5, 0, 0), BattleActionMove.BAM_NORMAL);
		Assert.Empty(pf.Path);

		// open a second tile of the gap
		save.GetTile(new Position(3, 4, 0)).SetMapData(null, -1, -1, TilePart.O_OBJECT);
		pf.Calculate(tank, new Position(4, 0, 0), BattleActionMove.BAM_NORMAL);
		List<int> dirs = Dequeue(pf);
		Assert.NotEmpty(dirs);
		Assert.Equal(new Position(4, 0, 0), Walk(new Position(0, 0, 0), dirs));

		// a small unit squeezes through the one tile gap
		save.GetTile(new Position(3, 4, 0)).SetMapData(Part(TilePart.O_OBJECT, Pathfinding.INVALID_MOVE_COST), 1, 0, TilePart.O_OBJECT);
		var soldier = Unit(new Position(0, 0, 0));
		pf.Calculate(soldier, new Position(4, 0, 0), BattleActionMove.BAM_NORMAL);
		Assert.Equal(new Position(4, 0, 0), Walk(new Position(0, 0, 0), Dequeue(pf)));
	}
}
