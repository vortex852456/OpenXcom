using static Acom.Tests.Savegame.SavegameTestHelpers;

namespace Acom.Tests.Savegame;

public class SavedBattleGameTests
{
	private static SavedBattleGame NewBattle(int x, int y, int z)
	{
		var save = new SavedBattleGame(null, new Language());
		save.InitMap(x, y, z);
		return save;
	}

	private static string Emit(SavedBattleGame save)
	{
		var writer = new YamlRootNodeWriter();
		save.Save(writer);
		return writer.Emit();
	}

	[Fact]
	public void Tile_index_and_coordinates_are_inverse()
	{
		var save = NewBattle(5, 4, 3);
		Assert.Equal(60, save.MapSizeXYZ);
		for (int i = 0; i < save.MapSizeXYZ; ++i)
		{
			Position p = save.GetTileCoords(i);
			Assert.Equal(i, save.GetTileIndex(p));
			Assert.Same(save.GetTile(i), save.GetTile(p));
			Assert.Equal(p, save.GetTile(i).Position);
		}
		// z * (y * x) + y * x + x
		Assert.Equal(new Position(2, 1, 1), save.GetTileCoords(27));
		Assert.Equal(1 * 20 + 3 * 5 + 4, save.GetTileIndex(new Position(4, 3, 1)));
	}

	[Fact]
	public void Tiles_outside_the_map_are_null()
	{
		var save = NewBattle(5, 4, 3);
		Assert.Null(save.GetTile(new Position(-1, 0, 0)));
		Assert.Null(save.GetTile(new Position(0, -1, 0)));
		Assert.Null(save.GetTile(new Position(0, 0, -1)));
		Assert.Null(save.GetTile(new Position(5, 0, 0)));
		Assert.Null(save.GetTile(new Position(0, 4, 0)));
		Assert.Null(save.GetTile(new Position(0, 0, 3)));
		Assert.NotNull(save.GetTile(new Position(4, 3, 2)));
	}

	[Fact]
	public void Tiles_below_and_above()
	{
		var save = NewBattle(5, 4, 3);
		var middle = save.GetTile(new Position(2, 3, 1));
		Assert.Same(save.GetTile(new Position(2, 3, 0)), save.GetBelowTile(middle));
		Assert.Same(save.GetTile(new Position(2, 3, 2)), save.GetAboveTile(middle));
		Assert.Null(save.GetBelowTile(save.GetTile(new Position(2, 3, 0))));
		Assert.Null(save.GetAboveTile(save.GetTile(new Position(2, 3, 2))));
		Assert.Null(save.GetBelowTile(null));
		Assert.Null(save.GetAboveTile(null));
	}

	[Fact]
	public void Tile_search_is_an_11x11_grid()
	{
		var save = new SavedBattleGame(null, new Language());
		Assert.Equal(121, save.TileSearch.Count);
		Assert.Equal(new Position(-5, -5, 0), save.TileSearch[0]);
		Assert.Equal(new Position(0, 0, 0), save.TileSearch[60]);
		Assert.Equal(new Position(5, 5, 0), save.TileSearch[120]);
		Assert.Equal(new Position(-4, -5, 0), save.TileSearch[1]);
		Assert.Equal("TAC00.SCR", save.HiddenMovementBackground);
	}

	[Fact]
	public void Binary_tiles_are_written_like_cpp()
	{
		var save = NewBattle(3, 2, 2);
		save.GetTile(new Position(1, 0, 0)).Load(Yaml("{mapDataID: [1, -1, -1, -1], mapDataSetID: [0, -1, -1, -1], smoke: 2}"));
		save.GetTile(new Position(2, 1, 1)).Load(Yaml("{mapDataID: [-1, 4, -1, -1], mapDataSetID: [-1, 1, -1, -1], fire: 1, smoke: 7}"));

		var writer = new YamlRootNodeWriter();
		save.SaveTiles(writer);
		string text = writer.Emit();
		// C++ writes the field sizes as chars, double-quoted with hex escapes
		Assert.Contains("tileIndexSize: \"\\x04\"", text, StringComparison.Ordinal);
		Assert.Contains("tileTotalBytesPer: 23", text, StringComparison.Ordinal);
		Assert.Contains("tileIDSize: \"\\x02\"", text, StringComparison.Ordinal);
		Assert.Contains("tileBoolFieldsSize: \"\\x01\"", text, StringComparison.Ordinal);

		var r = Yaml(text);
		Assert.Equal(["tileIndexSize", "tileTotalBytesPer", "tileFireSize", "tileSmokeSize", "tileIDSize", "tileSetIDSize", "tileBoolFieldsSize", "totalTiles", "binTiles"], Keys(r));
		Assert.Equal(2, r["totalTiles"].ReadVal<int>()); // only tiles that are not void (here: smoke)
		byte[] data = r["binTiles"].ReadValBase64();
		Assert.Equal(2 * 23, data.Length);
		Assert.Equal(new byte[] { 1, 0, 0, 0, 1, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 2, 0, 0 }, data[..23]);
		Assert.Equal(11, data[23]); // index of (2, 1, 1)

		var copy = NewBattle(3, 2, 2);
		copy.LoadTiles(r);
		var tile = copy.GetTile(new Position(2, 1, 1));
		Assert.Equal((7, 1), (tile.Smoke, tile.Fire));
		int id = 0, setId = 0;
		tile.GetMapData(ref id, ref setId, TilePart.O_WESTWALL);
		Assert.Equal((4, 1), (id, setId));
		Assert.Equal(2, copy.GetTile(new Position(1, 0, 0)).Smoke);
		Assert.Equal(0, copy.GetTile(new Position(0, 0, 0)).Smoke);
	}

	[Fact]
	public void Old_saves_without_bool_field_size_and_with_text_tiles()
	{
		var save = NewBattle(2, 2, 1);
		// key sizes missing: the default is 0 except for the bool field (1)
		save.LoadTiles(Yaml("{tiles: [{position: [1, 1, 0], mapDataID: [3, -1, -1, -1], mapDataSetID: [0, -1, -1, -1], fire: 2}]}"));
		Assert.Equal(2, save.GetTile(new Position(1, 1, 0)).Fire);
	}

	[Fact]
	public void Battle_save_roundtrip_in_cpp_key_order()
	{
		var save = NewBattle(4, 3, 2);
		save.MissionType = "STR_UFO_CRASH_RECOVERY";
		save.MissionTarget = "STR_UFO_1";
		save.MissionCraftOrBase = "STR_SKYRANGER-1";
		save.GlobalShade = 7;
		save.Depth = 1;
		save.Music = "GMTACTIC";
		save.AmbienceRandom = [3, 4];
		save.ReinforcementsMemory["STR_X"] = 2;
		save.ReinforcementsBlocks.Add([1, 2]);
		save.FlattenedMapBlockNames.Add(["A", "B"]);
		save.VIPSurvivalPercentage = 50;
		save.AddSavedVIP(10);
		save.SetObjectiveType((int)SpecialTileType.UFO_NAVIGATION);
		save.SetObjectiveCount(3);
		save.TUReserved = BattleActionType.BA_SNAPSHOT;
		save.GetTile(new Position(3, 2, 1)).Load(Yaml("{mapDataID: [1, 2, 3, 4], mapDataSetID: [0, 0, 0, 0], smoke: 1}"));
		var node = new Node(0, new Position(1, 1, 0), 0, 0, 1, 2, 0, 3);
		node.NodeLinks.Add(1);
		save.Nodes.Add(node);
		save.Nodes.Add(new Node(1, new Position(2, 1, 0), 0, 0, 0, 0, 0, 0));

		string text = Emit(save);
		var r = Yaml(text);
		Assert.Equal(
		[
			"vipEscapeType", "vipSurvivalPercentage", "vipsSaved", "vipsLost", "vipsWaitingOutside", "vipsSavedScore", "vipsLostScore", "vipsWaitingOutsideScore",
			"objectivesDestroyed", "objectivesNeeded", "objectiveType",
			"width", "length", "height", "missionType", "strTarget", "strCraftOrBase",
			"nameDisplay", "ecEnabledFriendly", "ecEnabledHostile", "ecEnabledNeutral", "alienCustomDeploy", "alienCustomMission", "alienItemLevel", "lastUsedMapScript",
			"reinforcementsDeployment", "reinforcementsRace", "reinforcementsItemLevel", "reinforcementsMemory", "reinforcementsBlocks",
			"flattenedMapTerrainNames", "flattenedMapBlockNames", "globalshade", "turn", "bughuntMinTurn", "animFrame", "bughuntMode", "selectedUnit", "undoUnit",
			"tileIndexSize", "tileTotalBytesPer", "tileFireSize", "tileSmokeSize", "tileIDSize", "tileSetIDSize", "tileBoolFieldsSize", "totalTiles", "binTiles",
			"nodes", "tuReserved", "kneelReserved", "depth", "ambience", "ambientVolume", "ambienceRandom",
			"minAmbienceRandomDelay", "maxAmbienceRandomDelay", "currentAmbienceDelay", "music", "baseItems",
			"turnLimit", "chronoTrigger", "cheatTurn", "togglePersonalLight", "toggleNightVision", "toggleBrightness",
		], Keys(r));
		Assert.Equal(-1, r["selectedUnit"].ReadVal<int>());
		Assert.Equal(8, r["tuReserved"].ReadVal<int>());
		Assert.Equal(0.5, r["ambientVolume"].ReadVal<double>());
		Assert.Contains("strCraftOrBase: \"STR_SKYRANGER-1\"", text, StringComparison.Ordinal);

		var loaded = new SavedBattleGame(null, new Language());
		loaded.Load(r, Uninitialized<Acom.Mod>(), Uninitialized<SavedGame>());
		Assert.Equal((4, 3, 2), (loaded.MapSizeX, loaded.MapSizeY, loaded.MapSizeZ));
		Assert.Equal("STR_UFO_1", loaded.MissionTarget);
		Assert.Equal(2, loaded.Nodes.Count);
		Assert.Equal([1], loaded.Nodes[0].NodeLinks);
		Assert.Equal(1, loaded.GetTile(new Position(3, 2, 1)).Smoke);
		Assert.Equal(50, loaded.VIPSurvivalPercentage);
		Assert.Equal(SpecialTileType.UFO_NAVIGATION, loaded.ObjectiveType);
		Assert.Equal(2, loaded.ReinforcementsMemory["STR_X"]);
		Assert.Equal(text, Emit(loaded));
	}

	[Fact]
	public void Base_defense_saves_the_module_map()
	{
		var save = NewBattle(20, 10, 1);
		save.MissionType = "STR_BASE_DEFENSE";
		var module = TileTests.MapDataWithFlags(baseModule: true, objectType: TilePart.O_OBJECT);
		save.GetTile(new Position(3, 3, 0)).SetMapData(module, 1, 0, TilePart.O_OBJECT);
		save.GetTile(new Position(4, 3, 0)).SetMapData(module, 1, 0, TilePart.O_OBJECT);
		save.GetTile(new Position(15, 5, 0)).SetMapData(module, 1, 0, TilePart.O_OBJECT);
		save.CalculateModuleMap();
		Assert.Equal(2, save.ModuleMap.Count);
		Assert.Equal([(2, 2)], save.ModuleMap[0]); // -1 + 2, then + 1
		Assert.Equal([(1, 1)], save.ModuleMap[1]);

		var r = Yaml(Emit(save));
		Assert.Equal([[(2, 2)], [(1, 1)]], r["moduleMap"].ReadVal<List<List<(int, int)>>>()); // pairs as [first, second]
		var loaded = new SavedBattleGame(null, new Language());
		loaded.Load(r, Uninitialized<Acom.Mod>(), Uninitialized<SavedGame>());
		Assert.Equal([(1, 1)], loaded.ModuleMap[1]);
	}

	[Fact]
	public void Animation_frame_wraps_around()
	{
		var save = new SavedBattleGame(null, new Language());
		save.Load(Yaml("{width: 1, length: 1, height: 1, selectedUnit: -1, animFrame: 705599}"), Uninitialized<Acom.Mod>(), Uninitialized<SavedGame>());
		Assert.Equal(705599, save.AnimFrame);
		save.NextAnimFrame();
		Assert.Equal(0, save.AnimFrame);
	}

	[Fact]
	public void Vip_statistics()
	{
		var save = new SavedBattleGame(null, new Language());
		save.VIPSurvivalPercentage = 60;
		save.VIPSurvivalPercentage = 40; // the biggest setting is kept
		Assert.Equal(60, save.VIPSurvivalPercentage);

		save.AddSavedVIP(10);
		save.AddLostVIP(5);
		save.AddWaitingOutsideVIP(20);
		save.CorrectVIPStats(false, false); // lost without retreat: everybody is lost
		Assert.Equal((0, 0), (save.SavedVIPs, save.SavedVIPsScore));
		Assert.Equal((3, -35), (save.LostVIPs, save.LostVIPsScore));

		var won = new SavedBattleGame(null, new Language());
		won.AddSavedVIP(10);
		won.AddWaitingOutsideVIP(20);
		won.CorrectVIPStats(true, false);
		Assert.Equal((2, 30), (won.SavedVIPs, won.SavedVIPsScore));
	}

	[Fact]
	public void Item_ids_and_removal()
	{
		var save = new SavedBattleGame(null, new Language());
		var a = save.CreateTempItem(null);
		var b = save.CreateTempItem(null);
		Assert.Equal((0, 1), (a.Id, b.Id));
		Assert.Equal(2, save.CurrentItemId);
		var c = new BattleItem(null, ref save.CurrentItemId);
		Assert.Equal(2, c.Id);
		Assert.Equal(3, save.CurrentItemId);

		save.Items.AddRange([a, b]);
		save.RemoveItem(a);
		Assert.Equal([b], save.Items);
		save.RemoveItem(a); // not in the list any more
		Assert.Equal([b], save.Items);
	}

	[Fact]
	public void Objectives_and_turn_counter()
	{
		var save = new SavedBattleGame(null, new Language());
		Assert.False(save.AllObjectivesDestroyed());
		Assert.Equal((SpecialTileType)(-1), save.ObjectiveType);
		Assert.Equal(UnitFaction.FACTION_PLAYER, save.Side);
		Assert.True(save.IsBeforeGame);
		Assert.Equal(20, save.BughuntMinTurn);
		Assert.Equal(ChronoTrigger.FORCE_LOSE, save.ChronoTrigger);
		Assert.True(save.TogglePersonalLight);
		Assert.True(save.GetEnvironmentalConditionsEnabled(UnitFaction.FACTION_NONE) == false);
		save.ApplyEnviroEffects(null);
		Assert.Null(save.EnviroEffects);
	}
}
