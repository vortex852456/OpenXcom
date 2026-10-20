using Acom.Tests.Engine;

#pragma warning disable CA1716 // namespace follows the source area (src/Mod), like Acom.Tests.<Area>
namespace Acom.Tests.Mod;
#pragma warning restore CA1716

/// <summary>Runs alone: map block selection draws from the global RNG that RngTests replays.</summary>

public class MapScriptTests
{
	private static MapScript LoadCommand(string yaml)
	{
		var command = new MapScript();
		command.Load(new YamlRootNodeReader(yaml, "test").ToBase());
		return command;
	}

	private static Dictionary<string, List<MapScript>> LoadRepoMapScripts()
	{
		var path = Path.Combine(LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "mapScripts.rul");
		var root = YamlRootNodeReader.FromFile(path);
		var result = new Dictionary<string, List<MapScript>>(StringComparer.Ordinal);
		foreach (var script in root["mapScripts"].Children())
		{
			var commands = new List<MapScript>();
			foreach (var commandReader in script["commands"].Children())
			{
				var command = new MapScript();
				command.Load(commandReader);
				commands.Add(command);
			}
			result[script["type"].ReadVal<string>()] = commands;
		}
		return result;
	}

	[Fact]
	public void Defaults_per_command_type()
	{
		var craft = LoadCommand("type: addCraft");
		Assert.Equal(MapScriptCommand.MSC_ADDCRAFT, craft.Type);
		Assert.Equal([1], craft.Groups);
		Assert.Empty(craft.CraftGroups);
		Assert.Equal(1, craft.SizeX);
		Assert.Equal(1, craft.SizeY);
		Assert.Equal(0, craft.SizeZ);
		Assert.Equal(100, craft.ChancesOfExecution);
		Assert.Equal(1, craft.Executions);
		Assert.True(craft.CanBeSkipped());
		Assert.Equal((int)MapBlockType.MT_NSROAD, craft.VerticalGroup);
		Assert.Equal((int)MapBlockType.MT_EWROAD, craft.HorizontalGroup);
		Assert.Equal((int)MapBlockType.MT_CROSSING, craft.CrossingGroup);

		var resize = LoadCommand("type: resize");
		Assert.Equal(MapScriptCommand.MSC_RESIZE, resize.Type);
		Assert.Equal(0, resize.SizeX);
		Assert.Equal(0, resize.SizeY);
		Assert.Empty(resize.Groups);
	}

	[Fact]
	public void Invalid_commands_throw()
	{
		Assert.Throws<InvalidOperationException>(() => LoadCommand("label: 1"));
		Assert.Throws<InvalidOperationException>(() => LoadCommand("type: addSomething"));
		Assert.Throws<InvalidOperationException>(() => LoadCommand("type: addLine"));
		Assert.Throws<InvalidOperationException>(() => LoadCommand("type: digTunnel"));
		Assert.Throws<InvalidOperationException>(() => LoadCommand("{type: addLine, direction: diagonal}"));
	}

	[Fact]
	public void Selection_lists_sizes_and_misc_fields()
	{
		var command = LoadCommand("""
			type: addBlock
			size: [2, 3, 1, 9]
			groups: [4, 5]
			blocks: [7, 8, 9]
			freqs: [5, 6, 7, 8]
			maxUses: 2
			label: -12
			conditionals: [-1, 2]
			executionChances: 40
			executions: 3
			canBeSkipped: false
			markAsReinforcementsBlock: true
			terrain: JUNGLE
			UFOName: STR_SMALL_SCOUT
			craftName: STR_SKYRANGER
			rects:
			  - [1, 2, 3, 4]
			  - [5, 6, 7, 8]
			""");
		Assert.Equal(2, command.SizeX);
		Assert.Equal(3, command.SizeY);
		Assert.Equal(1, command.SizeZ);
		// blocks win over groups (the groups are cleared)
		Assert.Empty(command.Groups);
		Assert.Equal([7, 8, 9], command.Blocks);
		Assert.Equal(12, command.Label);
		Assert.Equal([-1, 2], command.Conditionals);
		Assert.Equal(40, command.ChancesOfExecution);
		Assert.Equal(3, command.Executions);
		Assert.False(command.CanBeSkipped());
		Assert.True(command.MarkAsReinforcementsBlock());
		Assert.Equal(["JUNGLE"], command.RandomAlternateTerrain);
		Assert.Equal("STR_SMALL_SCOUT", command.UFOName);
		Assert.Equal("STR_SKYRANGER", command.CraftName);
		Assert.Equal(2, command.Rects.Count);
		Assert.Equal(new SDL_Rect(5, 6, 7, 8), command.Rects[1]);

		var square = LoadCommand("{type: fillArea, size: 2, conditionals: -3, randomTerrain: [A, B]}");
		Assert.Equal(2, square.SizeX);
		Assert.Equal(2, square.SizeY);
		Assert.Equal([-3], square.Conditionals);
		Assert.Equal(["A", "B"], square.RandomAlternateTerrain);
	}

	[Fact]
	public void Vertical_levels_and_tunnel_data()
	{
		var command = LoadCommand("""
			type: digTunnel
			direction: both
			tunnelData:
			  level: 1
			  MCDReplacements:
			    - {type: westWall, set: 2, entry: 18}
			    - {type: floor, set: 1}
			verticalLevels:
			  - {type: ground, size: [2, 1], groups: [3, 4], terrain: globeTerrain}
			  - {type: decoration, blocks: 5, maxRepeats: 2}
			  - {groups: 1}
			  - {type: bogus}
			""");
		Assert.Equal(MapDirection.MD_BOTH, command.Direction);
		Assert.Equal(1, command.TunnelData.level);
		Assert.Equal(18, command.TunnelData.GetMCDReplacement("westWall").entry);
		Assert.Equal(2, command.TunnelData.GetMCDReplacement("westWall").set);
		Assert.Equal(-1, command.TunnelData.GetMCDReplacement("floor").entry);
		Assert.Null(command.TunnelData.GetMCDReplacement("northWall"));

		// levels without a type are skipped, unknown types load as 'middle'
		Assert.Equal(3, command.VerticalLevels.Count);
		var ground = command.VerticalLevels[0];
		Assert.Equal(VerticalLevelType.VLT_GROUND, ground.levelType);
		Assert.Equal(2, ground.levelSizeX);
		Assert.Equal(1, ground.levelSizeY);
		Assert.Equal(-1, ground.levelSizeZ);
		Assert.Equal([3, 4], ground.levelGroups);
		Assert.Equal("globeTerrain", ground.levelTerrain);
		var decoration = command.VerticalLevels[1];
		Assert.Equal(VerticalLevelType.VLT_DECORATION, decoration.levelType);
		Assert.Equal(0, decoration.levelSizeZ);
		Assert.Equal([5], decoration.levelBlocks);
		Assert.Equal(2, decoration.maxRepeats);
		Assert.Equal(VerticalLevelType.VLT_MIDDLE, command.VerticalLevels[2].levelType);
		Assert.True(RuleTerrain.UsesTextureBasedTerrain([command]));
	}

	[Fact]
	public void Repository_map_scripts_load()
	{
		var scripts = LoadRepoMapScripts();
		Assert.True(scripts.Count > 10);

		var urban = scripts["URBAN"];
		Assert.Equal(MapScriptCommand.MSC_ADDLINE, urban[1].Type);
		Assert.Equal(MapDirection.MD_VERTICAL, urban[1].Direction);
		Assert.Equal(1, urban[1].Label);
		Assert.Equal(50, urban[1].ChancesOfExecution);
		Assert.Equal(new SDL_Rect(1, 1, 4, 1), urban[1].Rects[0]);
		Assert.Equal([-1], urban[2].Conditionals);
		Assert.Equal(MapDirection.MD_HORIZONTAL, urban[2].Direction);
		Assert.Equal([-1, -2], urban[3].Conditionals);
		Assert.Equal(4, urban[4].Executions);
		Assert.Equal([3, 4, 10, 11, 12, 13, 14], urban[5].Blocks);

		var alienBase = scripts["ALIENBASE"];
		var tunnel = alienBase[4];
		Assert.Equal(MapScriptCommand.MSC_DIGTUNNEL, tunnel.Type);
		Assert.Equal(0, tunnel.TunnelData.level);
		Assert.Equal(63, tunnel.TunnelData.GetMCDReplacement("floor").entry);
		Assert.Equal(1, tunnel.TunnelData.GetMCDReplacement("floor").set);
		Assert.Equal([0, 1], alienBase[3].Groups);
	}
}

[Collection(nameof(GlobalRng))]
public class MapScriptBlockSelectionTests
{
	[Fact]
	public void Max_uses_remove_blocks_from_the_selection()
	{
		var terrain = new RuleTerrain("TEST");
		var a = new MapBlock("A");
		var b = new MapBlock("B");
		terrain.MapBlocks.Add(a);
		terrain.MapBlocks.Add(b);

		var command = new MapScript();
		command.Load(new YamlRootNodeReader("{type: fillArea, blocks: [0, 1], maxUses: [1, 1]}", "test").ToBase());
		command.Init();
		var first = command.GetNextBlock(terrain);
		var second = command.GetNextBlock(terrain);
		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.NotSame(first, second);
		Assert.Null(command.GetNextBlock(terrain));

		// group selection: no compliant block of group 7 -> null
		var groups = new MapScript();
		groups.Load(new YamlRootNodeReader("{type: addBlock, groups: 7}", "test").ToBase());
		groups.Init();
		Assert.Null(groups.GetNextBlock(terrain));
		// without groups the default group 0 is used, both 10x10 blocks qualify
		var any = new MapScript();
		any.Load(new YamlRootNodeReader("{type: addBlock, groups: []}", "test").ToBase());
		any.Init();
		Assert.Contains(any.GetNextBlock(terrain), new[] { a, b });
	}
}
