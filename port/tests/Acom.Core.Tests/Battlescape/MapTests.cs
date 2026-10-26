using System.Reflection;
using static Acom.Tests.Battlescape.MapTestHelpers;
using static Acom.UnitStatus;

namespace Acom.Tests.Battlescape;

public class MapTests
{
	private static T GetField<T>(object obj, string name) =>
		(T)typeof(Map).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(obj)!;

	/// <summary>A bare map over a flat 10x10x2 battle with vapor lists and the given number of transparency LUT colours.</summary>
	private static Map VaporMap(int lutColors = 2)
	{
		var save = new SavedBattleGame(null, new Language());
		save.InitMap(10, 10, 2);
		Map map = BareMap(320, 200, save: save);
		var camera = new Camera(32, 40, 10, 10, 2, map, 164);
		SetField(map, typeof(Map), "_camera", camera);
		SetField(map, typeof(Map), "_transparencies", new List<byte>(new byte[lutColors * Acom.Mod.TransparenciesOpacityLevels * Acom.Mod.TransparenciesPaletteColors]));
		var init = new List<List<Particle>>();
		var vapor = new List<List<Particle>>();
		for (int i = 0; i < 100; ++i)
		{
			init.Add([]);
			vapor.Add([]);
		}
		SetField(map, typeof(Map), "_vaporParticlesInit", init);
		SetField(map, typeof(Map), "_vaporParticles", vapor);
		return map;
	}

	/// <summary>A particle in the middle of its tile; <paramref name="front"/> moves it past the tile diagonal (odd layer).</summary>
	private static Particle Vapor(int voxelZ, byte color = 1, bool front = false, byte opacity = 40) =>
		new(new Position(front ? 9 : 8, front ? 9 : 8, voxelZ), default, default, default, 0, color, opacity, 1);

	[Theory]
	[InlineData(0, 10, 0, 16, 0)]
	[InlineData(0, 10, 16, 16, 10)]
	[InlineData(0, 10, 8, 16, 5)]
	[InlineData(0, 10, -3, 16, 0)]
	[InlineData(0, 10, 99, 16, 10)]
	[InlineData(4, -8, 3, 8, 0)]   // (20 - 24) / 8 truncates
	[InlineData(16, 0, 7, 24, 11)]
	public void Interpolate_matches_fmath(int a, int b, int step, int max, int expected)
	{
		Assert.Equal(expected, Map.Interpolate(a, b, step, max));
	}

	[Fact]
	public void Arrow_bob_and_shade_pulse()
	{
		int[] bob = [0, 1, 2, 1, 0, 1, 2, 1];
		for (int f = 0; f < 16; ++f)
		{
			Assert.Equal(bob[f % 8], Map.GetArrowBobForFrame(f));
			Assert.Equal(2 + bob[f % 8] * 2 - 2, Map.GetShadePulseForFrame(0, f)); // clamped to 2..7 first
			Assert.Equal(7 + bob[f % 8] * 2 - 2, Map.GetShadePulseForFrame(15, f));
			Assert.Equal(5 + bob[f % 8] * 2 - 2, Map.GetShadePulseForFrame(5, f));
		}
	}

	[Theory]
	[InlineData(2, 3, 6, 3, 6)]     // straight move: x += 2, y += 1 per phase, normalized phase doubled
	[InlineData(0, 2, 4, -2, 4)]
	[InlineData(1, 3, 6, 0, 3)]     // diagonal: 16 phases, not doubled
	[InlineData(1, 11, -10, 0, 11)] // second half of a diagonal step: counted back from the end phase
	[InlineData(4, 6, 4, -2, 12)]   // second half of a straight step
	public void Walking_offset_follows_the_direction(int dir, int walkPhase, int x, int y, int normalized)
	{
		var save = new SavedBattleGame(null, new Language());
		save.InitMap(5, 5, 1);
		Map map = BareMap(320, 200, save: save);
		var armor = new Armor("STR_TEST_ARMOR", 0);
		var unit = new BattleUnit(UnitFaction.FACTION_PLAYER, 1, armor, null, null, new BattleUnitStatistics());
		unit.Direction = dir;
		SetField(unit, typeof(BattleUnit), "_status", STATUS_WALKING);
		SetField(unit, typeof(BattleUnit), "_walkPhase", walkPhase);
		unit.SetPosition(new Position(2, 2, 0));
		SetField(unit, typeof(BattleUnit), "_destination", new Position(2, 2, 0));

		UnitWalkingOffset o = map.CalculateWalkingOffset(unit);
		Assert.Equal(new Position(x, y, 0), o.ScreenOffset);
		Assert.Equal(normalized, o.NormalizedMovePhase);
		Assert.Equal(0, o.TerrainLevelOffset); // flat map without floors
	}

	[Fact]
	public void Standing_unit_has_no_walking_offset()
	{
		var save = new SavedBattleGame(null, new Language());
		save.InitMap(5, 5, 1);
		Map map = BareMap(320, 200, save: save);
		var unit = new BattleUnit(UnitFaction.FACTION_PLAYER, 1, new Armor("STR_TEST_ARMOR", 0), null, null, new BattleUnitStatistics());
		unit.Direction = 2;
		SetField(unit, typeof(BattleUnit), "_walkPhase", 3);
		unit.SetPosition(new Position(1, 1, 0));
		UnitWalkingOffset o = map.CalculateWalkingOffset(unit);
		Assert.Equal(default, o.ScreenOffset);
		Assert.Equal(6, o.NormalizedMovePhase);
	}

	[Fact]
	public void Add_vapor_particle_copies_and_checks_bounds()
	{
		Map map = VaporMap(lutColors: 2);
		var p = Vapor(4, opacity: 3);
		map.AddVaporParticle(new Position(3, 4, 0), p);
		map.AddVaporParticle(new Position(10, 0, 0), p);  // outside
		map.AddVaporParticle(new Position(0, -1, 0), p);  // outside
		map.AddVaporParticle(new Position(1, 1, 0), Vapor(4, color: 2)); // no LUT for colour 2

		var init = GetField<List<List<Particle>>>(map, "_vaporParticlesInit");
		Assert.Single(init[4 * 10 + 3]);
		Assert.Equal(1, init.Sum(l => l.Count));
		Assert.NotSame(p, init[43][0]);
		Assert.Equal(64, init[43].Capacity);

		// the stored copy is independent of the caller's particle
		p.Animate();
		p.UpdateScreenPosition();
		init[43][0].UpdateScreenPosition();
		Assert.NotEqual(p.Opacity, init[43][0].Opacity);
	}

	[Fact]
	public void Vapor_particles_are_selected_by_layer()
	{
		Map map = VaporMap();
		var vapor = GetField<List<List<Particle>>>(map, "_vaporParticles");
		var list = vapor[0];
		// layerZ = 2 * (voxel z / TileZ) + (in front of the tile diagonal ? 1 : 0)
		foreach (var (z, front) in new[] { (30, true), (5, false), (48, false), (0, true), (24, false), (0, false) })
		{
			var p = Vapor(z, front: front);
			p.UpdateScreenPosition();
			list.Add(p);
		}
		Map.SortByLayer(list);
		Assert.Equal([0, 0, 1, 2, 3, 4], list.Select(p => p.LayerZ));

		var save = GetField<SavedBattleGame>(map, "_save");
		Tile ground = save.GetTile(new Position(0, 0, 0));
		Tile upper = save.GetTile(new Position(0, 0, 1));
		Assert.Equal([0, 0], map.GetVaporParticle(ground, 0).ToArray().Select(p => p.LayerZ));
		Assert.Equal([1], map.GetVaporParticle(ground, 1).ToArray().Select(p => p.LayerZ));
		Assert.Equal([1, 2, 3, 4], map.GetVaporParticle(ground, 3).ToArray().Select(p => p.LayerZ));
		Assert.Equal([2], map.GetVaporParticle(upper, 0).ToArray().Select(p => p.LayerZ));
		Assert.Equal([3, 4], map.GetVaporParticle(upper, 3).ToArray().Select(p => p.LayerZ));
	}

	[Fact]
	public void Sort_by_layer_is_stable()
	{
		var a = Vapor(1);
		var b = Vapor(30);
		var c = Vapor(20);
		foreach (var p in new[] { a, b, c })
		{
			p.UpdateScreenPosition();
		}
		var list = new List<Particle> { b, a, c };
		Map.SortByLayer(list);
		Assert.Equal([a, c, b], list);
	}

	[Fact]
	public void Cursor_type_size_only_for_normal_cursor()
	{
		Map map = BareMap(320, 200);
		map.SetCursorType(CursorType.CT_NORMAL, 2);
		Assert.Equal(2, GetField<int>(map, "_cursorSize"));
		map.SetCursorType(CursorType.CT_AIM, 2);
		Assert.Equal(1, GetField<int>(map, "_cursorSize"));
		Assert.Equal(CursorType.CT_AIM, map.CursorType);
	}
}
