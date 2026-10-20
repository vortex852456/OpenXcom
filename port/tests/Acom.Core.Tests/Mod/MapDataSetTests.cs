#pragma warning disable CA1716 // namespace follows the source area (src/Mod), like Acom.Tests.<Area>
namespace Acom.Tests.Mod;
#pragma warning restore CA1716

public class MapDataSetTests
{
	/// <summary>A synthetic MCD record (62 bytes, layout of the packed C++ struct MCD).</summary>
	private static byte[] Record(byte seed)
	{
		var mcd = new byte[MapDataSet.MCD_RECORD_SIZE];
		for (int i = 0; i < 8; i++) mcd[i] = (byte)(seed + i);          // Frame[8]
		for (int i = 0; i < 12; i++) mcd[8 + i] = (byte)(100 + i);      // LOFT[12]
		mcd[20] = 0x34; mcd[21] = 0x12;                                  // ScanG (little endian)
		for (int i = 22; i < 30; i++) mcd[i] = 0xEE;                     // u23..u30 (unused)
		mcd[30] = 1;    // UFO_Door
		mcd[31] = 1;    // Stop_LOS
		mcd[32] = 0;    // No_Floor
		mcd[33] = 3;    // Big_Wall
		mcd[34] = 0;    // Gravlift
		mcd[35] = 1;    // Door
		mcd[36] = 1;    // Block_Fire
		mcd[37] = 0;    // Block_Smoke
		mcd[38] = 0xEE; // u39
		mcd[39] = 4;    // TU_Walk
		mcd[40] = 5;    // TU_Slide
		mcd[41] = 6;    // TU_Fly
		mcd[42] = 50;   // Armor
		mcd[43] = 7;    // HE_Block
		mcd[44] = 2;    // Die_MCD
		mcd[45] = 8;    // Flammable
		mcd[46] = 9;    // Alt_MCD
		mcd[47] = 0xEE; // u48
		mcd[48] = 0xF8; // T_Level (signed: -8)
		mcd[49] = 200;  // P_Level
		mcd[50] = 0xEE; // u51
		mcd[51] = 1;    // Light_Block
		mcd[52] = 3;    // Footstep
		mcd[53] = 1;    // Tile_Type (O_WESTWALL)
		mcd[54] = 2;    // HE_Type
		mcd[55] = 30;   // HE_Strength
		mcd[56] = 1;    // Smoke_Blockage (unused)
		mcd[57] = 11;   // Fuel
		mcd[58] = 1;    // Light_Source
		mcd[59] = 14;   // Target_Type (MUST_DESTROY)
		mcd[60] = 1;    // Xcom_Base
		mcd[61] = 0xEE; // u62
		return mcd;
	}

	[Fact]
	public void Mcd_record_is_parsed_like_the_packed_cpp_struct()
	{
		var set = new MapDataSet("TEST");
		using (var stream = new MemoryStream(Record(10)))
		{
			set.LoadMcd(stream);
		}
		Assert.Equal(1, set.Size);
		var data = set.GetObject(0);
		Assert.Same(set, data.Dataset);
		for (int i = 0; i < 8; i++) Assert.Equal(10 + i, data.GetSprite(i));
		for (int i = 0; i < 12; i++) Assert.Equal(100 + i, data.GetLoftID(i));
		Assert.Equal(0x1234, data.MiniMapIndex);
		Assert.True(data.IsUFODoor);
		Assert.False(data.IsNoFloor);
		Assert.Equal(3, data.BigWall);
		Assert.True(data.IsBackTileObject);
		Assert.False(data.IsGravLift);
		Assert.True(data.IsDoor);
		Assert.True(data.IsBaseModule);
		Assert.Equal(4, data.GetTUCost(MovementType.MT_WALK));
		Assert.Equal(5, data.GetTUCost(MovementType.MT_SLIDE));
		Assert.Equal(6, data.GetTUCost(MovementType.MT_FLY));
		Assert.Equal(0, data.GetTUCost(MovementType.MT_FLOAT));
		Assert.Equal(50, data.Armor);
		Assert.Equal(2, data.DieMCD);
		Assert.Equal(8, data.Flammable);
		Assert.Equal(9, data.AltMCD);
		Assert.Equal(-8, data.TerrainLevel);
		Assert.Equal(200, data.YOffset);
		Assert.Equal(3, data.FootstepSound);
		Assert.Equal(TilePart.O_WESTWALL, data.ObjectType);
		Assert.Equal(2, data.ExplosiveType);
		Assert.Equal(30, data.Explosive);
		Assert.Equal(11, data.Fuel);
		Assert.Equal(15, data.LightSource); // lamp posts (1) emit more light
		Assert.Equal(SpecialTileType.MUST_DESTROY, data.SpecialType);
		// block values: vision from Stop_LOS, HE from HE_Block, smoke from Block_Smoke
		Assert.Equal(255, data.GetBlock(ItemDamageType.DT_NONE));
		Assert.Equal(7, data.GetBlock(ItemDamageType.DT_HE));
		Assert.Equal(0, data.GetBlock(ItemDamageType.DT_SMOKE));
		Assert.Throws<InvalidOperationException>(() => set.GetObject(1));
		Assert.Throws<InvalidOperationException>(() => set.GetObject(-1));
	}

	[Fact]
	public void Trailing_partial_record_is_ignored_and_blanks_are_stored()
	{
		var bytes = new List<byte>();
		bytes.AddRange(Record(0));
		bytes.AddRange(Record(20));
		bytes.AddRange(new byte[10]);
		var blanks = new MapDataSet("BLANKS");
		using (var stream = new MemoryStream([.. bytes]))
		{
			blanks.LoadMcd(stream);
		}
		Assert.Equal(2, blanks.Size);
		Assert.Same(blanks.GetObject(0), MapDataSet.BlankFloorTile);
		Assert.Same(blanks.GetObject(1), MapDataSet.ScorchedEarthTile);
		Assert.Equal(20, MapDataSet.ScorchedEarthTile.GetSprite(0));
	}

	[Fact]
	public void Mcd_patch_modifies_the_records()
	{
		var set = new MapDataSet("PATCHED");
		using (var stream = new MemoryStream([.. Record(0), .. Record(0)]))
		{
			set.LoadMcd(stream);
		}
		var patch = new MCDPatch();
		patch.Load(new YamlRootNodeReader("""
			type: PATCHED
			data:
			  - MCDIndex: 1
			    bigWall: 2
			    TUWalk: 9
			    TUFly: 8
			    TUSlide: 7
			    deathTile: 0
			    terrainHeight: -4
			    specialType: 5
			    explosive: 66
			    armor: 255
			    flammability: 3
			    fuel: 4
			    footstepSound: 6
			    HEBlock: 12
			    noFloor: true
			    LOFTS: [1, 2, 3]
			    stopLOS: false
			    objectType: 3
			""", "test").ToBase());
		patch.ModifyData(set);

		var untouched = set.GetObject(0);
		Assert.Equal(3, untouched.BigWall);
		var data = set.GetObject(1);
		Assert.Equal(2, data.BigWall);
		Assert.Equal(9, data.GetTUCost(MovementType.MT_WALK));
		Assert.Equal(8, data.GetTUCost(MovementType.MT_FLY));
		Assert.Equal(7, data.GetTUCost(MovementType.MT_SLIDE));
		Assert.Equal(0, data.DieMCD);
		Assert.Equal(-4, data.TerrainLevel);
		Assert.Equal(SpecialTileType.ALIEN_FOOD, data.SpecialType);
		Assert.Equal(66, data.Explosive);
		Assert.Equal(255, data.Armor);
		Assert.Equal(3, data.Flammable);
		Assert.Equal(4, data.Fuel);
		Assert.Equal(6, data.FootstepSound);
		Assert.Equal(12, data.GetBlock(ItemDamageType.DT_HE));
		Assert.True(data.IsNoFloor);
		Assert.Equal([1, 2, 3, 103], Enumerable.Range(0, 4).Select(data.GetLoftID));
		Assert.Equal(0, data.GetBlock(ItemDamageType.DT_NONE));
		// objectType is applied after specialType (which keeps the old object type)
		Assert.Equal(TilePart.O_OBJECT, data.ObjectType);
	}

	[Fact]
	public void Loftemps_are_little_endian_words()
	{
		var voxels = new List<ushort>();
		using (var stream = new MemoryStream([0x01, 0x80, 0xFF, 0x00, 0x07]))
		{
			MapDataSet.LoadLOFTEMPS(stream, voxels);
		}
		Assert.Equal([0x8001, 0x00FF], voxels);
	}

	[Fact]
	public void Terrain_map_data_ids_run_over_all_data_sets()
	{
		var first = new MapDataSet("FIRST");
		using (var stream = new MemoryStream([.. Record(0), .. Record(1)]))
		{
			first.LoadMcd(stream);
		}
		var second = new MapDataSet("SECOND");
		using (var stream = new MemoryStream([.. Record(2), .. Record(3), .. Record(4)]))
		{
			second.LoadMcd(stream);
		}
		var terrain = new RuleTerrain("TEST");
		terrain.MapDataSets.Add(first);
		terrain.MapDataSets.Add(second);

		uint id = 3;
		int set = 0;
		Assert.Same(second.GetObject(1), terrain.GetMapData(ref id, ref set));
		Assert.Equal(1u, id);
		Assert.Equal(1, set);

		// broken references fall back to the first object of the first set
		id = 5;
		set = 0;
		Assert.Same(first.GetObject(0), terrain.GetMapData(ref id, ref set));
		Assert.Equal(0u, id);
		Assert.Equal(0, set);
	}
}
