using System.Reflection;

namespace Acom.Tests.Savegame;

public class TileTests
{
	private static YamlNodeReader Parse(string yaml) => new YamlRootNodeReader(yaml, "test").ToBase();

	private static string Emit(Tile tile)
	{
		var writer = new YamlRootNodeWriter();
		tile.Save(writer);
		return writer.Emit();
	}

	/// <summary>Tile parts update their sprite, which needs the terrain's surface set (normally loaded from files).</summary>
	internal static MapData MapDataWithFlags(bool isUfoDoor = false, bool baseModule = false, TilePart objectType = TilePart.O_FLOOR)
	{
		var mds = new MapDataSet("TEST");
		typeof(MapDataSet).GetField("_surfaceSet", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(mds, new SurfaceSet(32, 40));
		var md = new MapData(mds);
		md.SetFlags(isUfoDoor, false, false, 0, false, false, false, false, baseModule);
		md.ObjectType = objectType;
		return md;
	}

	private const int TileBytes = 4 * 2 + 4 * 2 + 1 + 1 + 1; // serialization key without the index

	[Fact]
	public void Yaml_roundtrip_of_text_tiles()
	{
		var tile = new Tile(new Position(1, 2, 0), null);
		tile.Load(Parse("{position: [9, 9, 9], mapDataID: [1, -1, 3, 70], mapDataSetID: [0, -1, 1, 2], smoke: 4, fire: 2}"));

		Assert.Equal(new Position(1, 2, 0), tile.Position); // position comes from the map, not the node
		Assert.Equal(4, tile.Smoke);
		Assert.Equal(2, tile.Fire);
		int id = 0, setId = 0;
		tile.GetMapData(ref id, ref setId, TilePart.O_OBJECT);
		Assert.Equal((70, 2), (id, setId));

		var copy = new Tile(new Position(1, 2, 0), null);
		copy.Load(Parse(Emit(tile)));
		Assert.Equal(Emit(tile), Emit(copy));
		Assert.Equal(["position", "mapDataID", "mapDataSetID", "smoke", "fire"], Parse(Emit(tile)).Children().Select(c => c.Key));
	}

	[Fact]
	public void Empty_tile_writes_only_position_and_ids()
	{
		var tile = new Tile(new Position(0, 0, 0), null);
		Assert.True(tile.IsVoid);
		var r = Parse(Emit(tile));
		Assert.Equal(["position", "mapDataID", "mapDataSetID"], r.Children().Select(c => c.Key));
		Assert.Equal([-1, -1, -1, -1], r["mapDataID"].ReadVal<List<int>>());
	}

	[Fact]
	public void Binary_layout_matches_cpp_serialization_key()
	{
		Assert.Equal(4, Tile.serializationKey.index);
		Assert.Equal(4u + TileBytes, Tile.serializationKey.totalBytes);

		var tile = new Tile(new Position(0, 0, 0), null);
		tile.Load(Parse("{mapDataID: [1, -1, 300, 4], mapDataSetID: [0, -1, 1, 2], smoke: 5, fire: 3}"));
		tile.SetDiscovered(true, TilePart.O_FLOOR); // also discovers both walls

		var buffer = new byte[TileBytes];
		Span<byte> ptr = buffer;
		tile.SaveBinary(ref ptr);
		Assert.Equal(0, ptr.Length);
		Assert.Equal(new byte[]
		{
			1, 0, 0xFF, 0xFF, 0x2C, 0x01, 4, 0, // mapDataID (Sint16)
			0, 0, 0xFF, 0xFF, 1, 0, 2, 0, // mapDataSetID (Sint16)
			5, 3, // smoke, fire
			1 | 2 | 4, // discovered west/north/floor, no open ufo doors
		}, buffer);

		var copy = new Tile(new Position(0, 0, 0), null);
		copy.LoadBinary(buffer, Tile.serializationKey);
		Assert.Equal(5, copy.Smoke);
		Assert.Equal(3, copy.Fire);
		Assert.True(copy.IsDiscovered(TilePart.O_FLOOR));
		Assert.True(copy.IsDiscovered(TilePart.O_WESTWALL));
		Assert.True(copy.IsDiscovered(TilePart.O_NORTHWALL));
		int id = 0, setId = 0;
		copy.GetMapData(ref id, ref setId, TilePart.O_NORTHWALL);
		Assert.Equal((300, 1), (id, setId));
	}

	[Fact]
	public void Discovered_tiles_can_not_be_saved_as_text()
	{
		var tile = new Tile(new Position(0, 0, 0), null);
		tile.SetDiscovered(true, TilePart.O_WESTWALL);
		Assert.Throws<InvalidOperationException>(() => Emit(tile));
	}

	[Fact]
	public void Old_discovered_flags_are_read_with_the_cpp_index_conversion()
	{
		var tile = new Tile(new Position(0, 0, 0), null);
		// C++ maps old indices 0,1,2 to parts -1 (out of bounds, skipped), 0, 0
		tile.Load(Parse("{discovered: [true, false, true]}"));
		Assert.True(tile.IsDiscovered(TilePart.O_FLOOR));
		Assert.False(tile.IsDiscovered(TilePart.O_WESTWALL));
		Assert.False(tile.IsDiscovered(TilePart.O_NORTHWALL));
	}

	[Fact]
	public void Open_ufo_door_is_saved_as_text_and_binary()
	{
		var tile = new Tile(new Position(0, 0, 0), null);
		tile.SetMapData(MapDataWithFlags(isUfoDoor: true, objectType: TilePart.O_WESTWALL), 5, 0, TilePart.O_WESTWALL);
		Assert.True(tile.IsUfoDoor(TilePart.O_WESTWALL));
		Assert.False(tile.IsUfoDoorOpen(TilePart.O_WESTWALL));
		Assert.Equal(-1, tile.OpenDoor(TilePart.O_NORTHWALL, null));
		Assert.Equal(1, tile.OpenDoor(TilePart.O_WESTWALL, null)); // starts opening
		Assert.Equal(3, tile.OpenDoor(TilePart.O_WESTWALL, null)); // still opening
		for (int i = 0; i < 10; ++i)
			tile.Animate();
		Assert.Equal(-1, tile.OpenDoor(TilePart.O_WESTWALL, null)); // open (frame 7) and static
		Assert.True(tile.IsUfoDoorOpen(TilePart.O_WESTWALL));
		Assert.False(tile.IsVoid);

		Assert.Equal(["position", "mapDataID", "mapDataSetID", "openDoorWest"], Parse(Emit(tile)).Children().Select(c => c.Key));

		var buffer = new byte[TileBytes];
		Span<byte> ptr = buffer;
		tile.SaveBinary(ref ptr);
		Assert.Equal(8, buffer[^1]);

		var copy = new Tile(new Position(0, 0, 0), null);
		copy.LoadBinary(buffer, Tile.serializationKey);
		copy.SetMapData(MapDataWithFlags(isUfoDoor: true, objectType: TilePart.O_WESTWALL), 5, 0, TilePart.O_WESTWALL);
		Assert.True(copy.IsUfoDoorOpen(TilePart.O_WESTWALL));
		Assert.Equal(1, copy.CloseUfoDoor());
		Assert.False(copy.IsUfoDoorOpen(TilePart.O_WESTWALL));
	}

	[Fact]
	public void Light_layers_and_shade()
	{
		var tile = new Tile(new Position(0, 0, 0), null);
		Assert.Equal(15, tile.Shade);
		tile.AddLight(4, LightLayers.LL_AMBIENT);
		tile.AddLight(10, LightLayers.LL_ITEMS);
		tile.AddLight(7, LightLayers.LL_ITEMS); // only raises
		Assert.Equal(10, tile.GetLight(LightLayers.LL_ITEMS));
		Assert.Equal(4, tile.GetLightMulti(LightLayers.LL_FIRE));
		Assert.Equal(10, tile.GetLightMulti(LightLayers.LL_UNITS));
		Assert.Equal(5, tile.Shade);
		tile.ResetLightMulti(LightLayers.LL_FIRE);
		Assert.Equal(11, tile.Shade);
		tile.AddLight(20, LightLayers.LL_UNITS);
		Assert.Equal(0, tile.Shade);
	}

	[Fact]
	public void Smoke_overlaps_are_averaged_at_new_turn()
	{
		var tile = new Tile(new Position(0, 0, 0), null);
		tile.AddSmoke(20); // first smoke of the turn is clamped to 1..15
		Assert.Equal(15, tile.Smoke);
		tile.AddSmoke(20); // further overlaps are summed up
		Assert.Equal(35, tile.Smoke);
		Assert.Equal(2, tile.Overlaps);
		tile.PrepareNewTurn(true);
		Assert.Equal(15, tile.Smoke); // clamp(35 / 2 - 1, 0, 15)
		Assert.Equal(0, tile.Overlaps);

		tile.Fire = 300;
		Assert.Equal(255, tile.Fire);
		tile.AddSmoke(5); // no smoke while burning
		Assert.Equal(15, tile.Smoke);
	}

	[Fact]
	public void Markers_and_flags()
	{
		var tile = new Tile(new Position(0, 0, 0), null);
		Assert.Equal(-1, tile.TUMarker);
		Assert.Equal(-1, tile.EnergyMarker);
		Assert.Equal(-1, tile.Preview);
		tile.Visible = 2; // C++ setVisible adds
		tile.Visible = 3;
		Assert.Equal(5, tile.Visible);
		tile.SetObstacle(2);
		Assert.True(tile.GetObstacle(2));
		Assert.False(tile.GetObstacle(1));
		Assert.True(tile.IsObstacle);
		tile.ResetObstacle();
		Assert.False(tile.IsObstacle);
		tile.SetExplosive(30, 2);
		tile.SetExplosive(10, 5); // lower power is ignored
		Assert.Equal((30, 2), (tile.Explosive, tile.ExplosiveType));
		tile.SetExplosive(0, 0, true);
		Assert.Equal(0, tile.Explosive);
		Assert.True(tile.HasNoFloor());
		tile.Dangerous = true;
		Assert.True(tile.Dangerous);
	}

	[Fact]
	public void Items_on_the_tile()
	{
		var tile = new Tile(new Position(0, 0, 0), null);
		int id = 0;
		var a = new BattleItem(null, ref id);
		var b = new BattleItem(null, ref id);
		tile.AddItem(a, null);
		tile.AddItem(b, null);
		Assert.Same(tile, a.Tile);
		Assert.Equal([a, b], tile.Inventory);
		Assert.False(tile.IsVoid);
		tile.RemoveItem(a);
		Assert.Null(a.Tile);
		Assert.Equal([b], tile.Inventory);
	}
}
