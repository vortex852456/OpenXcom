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
// Ported from: src/Mod/MapScript.h, src/Mod/MapScript.cpp
#nullable disable

namespace Acom;

public enum MapDirection : uint
{
	MD_NONE = 0,
	MD_VERTICAL = 1,
	MD_HORIZONTAL = 2,
	MD_BOTH = 3,
}

public class MCDReplacement
{
	public int set;
	public int entry;
}

public class TunnelData
{
	public SortedDictionary<string, MCDReplacement> replacements;
	public int level;

	public TunnelData()
	{
		replacements = new SortedDictionary<string, MCDReplacement>(StringComparer.Ordinal);
		level = 0;
	}

	/// <summary>Gets the MCD replacement for a tile part type, or null.</summary>
	public MCDReplacement GetMCDReplacement(string type)
	{
		if (!replacements.TryGetValue(type, out var replacement))
		{
			return null;
		}
		return replacement;
	}
}

public enum MapScriptCommand
{
	MSC_UNDEFINED = -1,
	MSC_ADDBLOCK = 0,
	MSC_ADDLINE = 1,
	MSC_ADDCRAFT = 2,
	MSC_ADDUFO = 3,
	MSC_DIGTUNNEL = 4,
	MSC_FILLAREA = 5,
	MSC_CHECKBLOCK = 6,
	MSC_REMOVE = 7,
	MSC_RESIZE = 8,
}

public enum VerticalLevelType : uint
{
	VLT_GROUND = 0,
	VLT_MIDDLE = 1,
	VLT_CEILING = 2,
	VLT_EMPTY = 3,
	VLT_DECORATION = 4,
	VLT_CRAFT = 5,
	VLT_LINE = 6,
}

/// <summary>
/// Structure for containing multiple levels of map blocks inside a command.
/// A C++ value type: use the copy constructor where C++ copies it.
/// </summary>
public class VerticalLevel
{
	public VerticalLevelType levelType;
	public List<int> levelGroups;
	public List<int> levelBlocks;
	public int levelSizeX;
	public int levelSizeY;
	public int levelSizeZ;
	public int maxRepeats;
	public string levelTerrain;

	/// <summary>Default constructor.</summary>
	public VerticalLevel()
	{
		levelType = VerticalLevelType.VLT_MIDDLE;
		levelGroups = [];
		levelBlocks = [];
		levelSizeX = 1;
		levelSizeY = 1;
		levelSizeZ = -1;
		maxRepeats = -1;
		levelTerrain = "";
	}

	/// <summary>Copy constructor (C++ copies this struct by value).</summary>
	public VerticalLevel(VerticalLevel other)
	{
		ArgumentNullException.ThrowIfNull(other);
		levelType = other.levelType;
		levelGroups = [.. other.levelGroups];
		levelBlocks = [.. other.levelBlocks];
		levelSizeX = other.levelSizeX;
		levelSizeY = other.levelSizeY;
		levelSizeZ = other.levelSizeZ;
		maxRepeats = other.maxRepeats;
		levelTerrain = other.levelTerrain;
	}

	/// <summary>Load in the data for a VerticalLevel from YAML, has to load similar data to a full mapscript command.</summary>
	public void Load(YamlNodeReader reader)
	{
		string type = reader["type"].ReadVal("");
		if (type == "ground")
		{
			levelType = VerticalLevelType.VLT_GROUND;
		}
		else if (type == "middle")
		{
			levelType = VerticalLevelType.VLT_MIDDLE;
		}
		else if (type == "ceiling")
		{
			levelType = VerticalLevelType.VLT_CEILING;
		}
		else if (type == "empty")
		{
			levelType = VerticalLevelType.VLT_EMPTY;
		}
		else if (type == "decoration")
		{
			levelType = VerticalLevelType.VLT_DECORATION;
			levelSizeZ = 0;
		}
		else if (type == "craft")
		{
			levelType = VerticalLevelType.VLT_CRAFT;
		}
		else if (type == "line")
		{
			levelType = VerticalLevelType.VLT_LINE;
		}
		else
		{
			Logger.Warning($"'{type}' does not resolve into a valid verticalLevel type, loading as 'middle'.");
			levelType = VerticalLevelType.VLT_MIDDLE;
		}
		var size = reader["size"];
		if (size)
		{
			if (size.IsSeq)
			{
				int entry = 0;
				foreach (var child in size.Children())
				{
					int value = child.ReadVal(1);
					if (entry == 0)
						levelSizeX = value;
					else if (entry == 1)
						levelSizeY = value;
					else
						levelSizeZ = value;
					entry++;
					if (entry == 3)
					{
						break;
					}
				}
			}
			else
			{
				size.TryReadVal(ref levelSizeX);
				levelSizeY = levelSizeX;
			}
		}
		reader.TryRead("maxRepeats", ref maxRepeats);
		var groups = reader["groups"];
		if (groups)
		{
			levelGroups.Clear();
			if (groups.IsSeq)
			{
				foreach (var group in groups.Children())
				{
					levelGroups.Add(group.ReadVal(0));
				}
			}
			else
			{
				levelGroups.Add(groups.ReadVal(0));
			}
		}
		var blocks = reader["blocks"];
		if (blocks)
		{
			levelGroups.Clear(); // sic: C++ clears the groups here, not the blocks
			if (blocks.IsSeq)
			{
				foreach (var block in blocks.Children())
				{
					levelBlocks.Add(block.ReadVal(0));
				}
			}
			else
			{
				levelBlocks.Add(blocks.ReadVal(0));
			}
		}
		reader.TryRead("terrain", ref levelTerrain);
	}
}

/// <summary>A command of a map generation script.</summary>
public class MapScript
{
	private MapScriptCommand _type;
	private bool _canBeSkipped, _markAsReinforcementsBlock;
	private readonly List<SDL_Rect> _rects = [];
	private List<int> _craftGroups = [];
	private List<int> _groups = [], _blocks = [], _frequencies = [], _maxUses = [], _conditionals = [];
	private int _verticalGroup, _horizontalGroup, _crossingGroup;
	private List<int> _groupsTemp = [], _blocksTemp = [], _frequenciesTemp = [], _maxUsesTemp = [];
	private int _sizeX, _sizeY, _sizeZ, _executionChances, _executions, _cumulativeFrequency, _label;
	private MapDirection _direction;
	private TunnelData _tunnelData;
	private string _ufoName = "", _craftName = "";
	private List<string> _randomTerrain = [];
	private List<VerticalLevel> _verticalLevels = [];

	public MapScript()
	{
		_type = MapScriptCommand.MSC_UNDEFINED;
		_canBeSkipped = true;
		_markAsReinforcementsBlock = false;
		_verticalGroup = (int)MapBlockType.MT_NSROAD;
		_horizontalGroup = (int)MapBlockType.MT_EWROAD;
		_crossingGroup = (int)MapBlockType.MT_CROSSING;
		_sizeX = 1;
		_sizeY = 1;
		_sizeZ = 0;
		_executionChances = 100;
		_executions = 1;
		_cumulativeFrequency = 0;
		_label = 0;
		_direction = MapDirection.MD_NONE;
		_tunnelData = null;
	}

	/// <summary>Loads a map script command from YAML.</summary>
	public void Load(YamlNodeReader node)
	{
		var reader = node.UseIndex();
		string command = "";
		var type = reader["type"];
		if (type)
		{
			command = type.ReadVal("");
			if (command == "addBlock")
				_type = MapScriptCommand.MSC_ADDBLOCK;
			else if (command == "addLine")
				_type = MapScriptCommand.MSC_ADDLINE;
			else if (command == "addCraft")
			{
				_type = MapScriptCommand.MSC_ADDCRAFT;
				_groups.Add(1); // this is a default, and can be overridden
			}
			else if (command == "addUFO")
			{
				_type = MapScriptCommand.MSC_ADDUFO;
				_groups.Add(1); // this is a default, and can be overridden
			}
			else if (command == "digTunnel")
				_type = MapScriptCommand.MSC_DIGTUNNEL;
			else if (command == "fillArea")
				_type = MapScriptCommand.MSC_FILLAREA;
			else if (command == "checkBlock")
				_type = MapScriptCommand.MSC_CHECKBLOCK;
			else if (command == "removeBlock")
				_type = MapScriptCommand.MSC_REMOVE;
			else if (command == "resize")
			{
				_type = MapScriptCommand.MSC_RESIZE;
				_sizeX = _sizeY = 0; // defaults: don't resize anything unless specified.
			}
			else
			{
				throw new InvalidOperationException("Unknown command: " + command);
			}
		}
		else
		{
			throw new InvalidOperationException("Missing command type.");
		}

		foreach (var rectReader in reader["rects"].Children())
		{
			// SDL_Rect has Sint16/Uint16 fields: the values are truncated like in C++
			var rect = new SDL_Rect(rectReader[0].ReadVal<int>(), rectReader[1].ReadVal<int>(), rectReader[2].ReadVal<int>(), rectReader[3].ReadVal<int>());
			_rects.Add(rect);
		}
		var craftGroups = reader["craftGroups"];
		if (craftGroups)
		{
			craftGroups.TryReadVal(ref _craftGroups);
		}
		var tunnelData = reader["tunnelData"];
		if (tunnelData)
		{
			_tunnelData = new TunnelData();
			_tunnelData.level = tunnelData["level"].ReadVal(0);
			foreach (var mcdReplacement in tunnelData["MCDReplacements"].Children())
			{
				var replacement = new MCDReplacement();
				string replacementType = mcdReplacement["type"].ReadVal("");
				replacement.entry = mcdReplacement["entry"].ReadVal(-1);
				replacement.set = mcdReplacement["set"].ReadVal(-1);
				_tunnelData.replacements[replacementType] = replacement;
			}
		}
		var conditionals = reader["conditionals"];
		if (conditionals)
		{
			if (conditionals.IsSeq)
			{
				conditionals.TryReadVal(ref _conditionals);
			}
			else
			{
				_conditionals.Add(conditionals.ReadVal(0));
			}
		}
		var size = reader["size"];
		if (size)
		{
			if (size.IsSeq)
			{
				int entry = 0;
				foreach (var child in size.Children())
				{
					int value = child.ReadVal(1);
					if (entry == 0)
						_sizeX = value;
					else if (entry == 1)
						_sizeY = value;
					else
						_sizeZ = value;
					entry++;
					if (entry == 3)
					{
						break;
					}
				}
			}
			else
			{
				size.TryReadVal(ref _sizeX);
				_sizeY = _sizeX;
			}
		}

		var groups = reader["groups"];
		if (groups)
		{
			_groups.Clear();
			if (groups.IsSeq)
			{
				foreach (var group in groups.Children())
				{
					_groups.Add(group.ReadVal(0));
				}
			}
			else
			{
				_groups.Add(groups.ReadVal(0));
			}
		}
		int selectionSize = _groups.Count;
		var blocks = reader["blocks"];
		if (blocks)
		{
			_groups.Clear();
			if (blocks.IsSeq)
			{
				foreach (var block in blocks.Children())
				{
					_blocks.Add(block.ReadVal(0));
				}
			}
			else
			{
				_blocks.Add(blocks.ReadVal(0));
			}
			selectionSize = _blocks.Count;
		}

		Resize(_frequencies, selectionSize, 1);
		Resize(_maxUses, selectionSize, -1);

		var freqs = reader["freqs"];
		if (freqs)
		{
			if (freqs.IsSeq)
			{
				int entry = 0;
				foreach (var freq in freqs.Children())
				{
					if (entry == selectionSize)
						break;
					_frequencies[entry] = freq.ReadVal(1);
					entry++;
				}
			}
			else
			{
				_frequencies[0] = freqs.ReadVal(1);
			}
		}
		var maxUses = reader["maxUses"];
		if (maxUses)
		{
			if (maxUses.IsSeq)
			{
				int entry = 0;
				foreach (var maxUse in maxUses.Children())
				{
					if (entry == selectionSize)
						break;
					_maxUses[entry] = maxUse.ReadVal(-1);
					entry++;
				}
			}
			else
			{
				_maxUses[0] = maxUses.ReadVal(-1);
			}
		}

		var directionNode = reader["direction"];
		if (directionNode)
		{
			string direction = directionNode.ReadVal("");
			if (direction.Length != 0)
			{
				char dir = char.ToUpperInvariant(direction[0]);
				switch (dir)
				{
					case 'V':
						_direction = MapDirection.MD_VERTICAL;
						break;
					case 'H':
						_direction = MapDirection.MD_HORIZONTAL;
						break;
					case 'B':
						_direction = MapDirection.MD_BOTH;
						break;
					default:
						throw new InvalidOperationException("direction must be [V]ertical, [H]orizontal, or [B]oth, what does " + direction + " mean?");
				}
			}
		}

		if (_direction == MapDirection.MD_NONE)
		{
			if (_type == MapScriptCommand.MSC_DIGTUNNEL || _type == MapScriptCommand.MSC_ADDLINE)
			{
				throw new InvalidOperationException("no direction defined for " + command + " command, must be [V]ertical, [H]orizontal, or [B]oth");
			}
		}

		reader.TryRead("verticalGroup", ref _verticalGroup);
		reader.TryRead("horizontalGroup", ref _horizontalGroup);
		reader.TryRead("crossingGroup", ref _crossingGroup);
		reader.TryRead("canBeSkipped", ref _canBeSkipped);
		reader.TryRead("markAsReinforcementsBlock", ref _markAsReinforcementsBlock);
		reader.TryRead("executionChances", ref _executionChances);
		reader.TryRead("executions", ref _executions);
		reader.TryRead("UFOName", ref _ufoName);
		reader.TryRead("craftName", ref _craftName);
		if (reader["terrain"])
		{
			_randomTerrain.Clear();
			_randomTerrain.Add(reader["terrain"].ReadVal<string>());
		}
		reader.TryRead("randomTerrain", ref _randomTerrain);
		// take no chances, don't accept negative values here.
		_label = Math.Abs(reader["label"].ReadVal(_label));

		// Load any VerticalLevels into a map if we have them
		if (reader["verticalLevels"])
		{
			_verticalLevels.Clear();
			foreach (var levelReader in reader["verticalLevels"].Children())
			{
				if (levelReader["type"])
				{
					var level = new VerticalLevel();
					level.Load(levelReader);
					_verticalLevels.Add(level);
				}
			}
		}
	}

	/// <summary>Like std::vector::resize(n, value).</summary>
	private static void Resize(List<int> list, int count, int value)
	{
		if (list.Count > count)
			list.RemoveRange(count, list.Count - count);
		while (list.Count < count)
			list.Add(value);
	}

	/// <summary>Initializes all the various scratch values and such for the command.</summary>
	public void Init()
	{
		_cumulativeFrequency = 0;
		_blocksTemp.Clear();
		_groupsTemp.Clear();
		_frequenciesTemp.Clear();
		_maxUsesTemp.Clear();

		foreach (int freq in _frequencies)
		{
			_cumulativeFrequency += freq;
		}
		_blocksTemp = [.. _blocks];
		_groupsTemp = [.. _groups];
		_frequenciesTemp = [.. _frequencies];
		_maxUsesTemp = [.. _maxUses];
	}

	/// <summary>Initializes scratch values for working in a vertical level.</summary>
	public void InitVerticalLevel(VerticalLevel level)
	{
		ArgumentNullException.ThrowIfNull(level);
		_cumulativeFrequency = 0;
		_blocksTemp.Clear();
		_groupsTemp.Clear();
		_frequenciesTemp.Clear();
		_maxUsesTemp.Clear();

		_blocks = [.. level.levelBlocks];
		_groups = [.. level.levelGroups];
		_cumulativeFrequency = Math.Max(_blocks.Count, _groups.Count);
		Resize(_frequenciesTemp, _cumulativeFrequency, 1);
		Resize(_maxUsesTemp, _cumulativeFrequency, -1);
		_blocksTemp = [.. _blocks];
		_groupsTemp = [.. _groups];
	}

	/// <summary>Gets what type of command this is.</summary>
	public MapScriptCommand Type => _type;

	/// <summary>Can this command be skipped if unsuccessful?</summary>
	public bool CanBeSkipped() => _canBeSkipped;

	/// <summary>Should blocks added by this command be used as reinforcements blocks?</summary>
	public bool MarkAsReinforcementsBlock() => _markAsReinforcementsBlock;

	/// <summary>Gets the rects, describing the areas this command applies to.</summary>
	public List<SDL_Rect> Rects => _rects;

	/// <summary>Gets the craft groups vector for iteration.</summary>
	public List<int> CraftGroups => _craftGroups;

	/// <summary>Gets the X size for this command.</summary>
	public int SizeX => _sizeX;

	/// <summary>Gets the Y size for this command.</summary>
	public int SizeY => _sizeY;

	/// <summary>Gets the Z size for this command.</summary>
	public int SizeZ => _sizeZ;

	/// <summary>Get the chances of this command executing.</summary>
	public int ChancesOfExecution => _executionChances;

	/// <summary>Gets the label for this command.</summary>
	public int Label => _label;

	/// <summary>Gets how many times this command repeats (1 repeat means 2 executions).</summary>
	public int Executions => _executions;

	/// <summary>Gets what conditions apply to this command.</summary>
	public List<int> Conditionals => _conditionals;

	/// <summary>Gets the groups vector for iteration.</summary>
	public List<int> Groups => _groups;

	/// <summary>Gets the blocks vector for iteration.</summary>
	public List<int> Blocks => _blocks;

	/// <summary>Gets the verticalGroup for this command.</summary>
	public int VerticalGroup => _verticalGroup;

	/// <summary>Gets the horizontalGroup for this command.</summary>
	public int HorizontalGroup => _horizontalGroup;

	/// <summary>Gets the crossingGroup for this command.</summary>
	public int CrossingGroup => _crossingGroup;

	/// <summary>Gets the direction this command goes (for lines and tunnels).</summary>
	public MapDirection Direction => _direction;

	/// <summary>Gets the mcd replacement data for tunnel replacements.</summary>
	public TunnelData TunnelData => _tunnelData;

	/// <summary>
	/// Gets a random group number from the array, accounting for frequencies and max uses.
	/// If no groups or blocks are defined, this command will return the "default" group,
	/// If all the max uses are used up, it will return "undefined".
	/// </summary>
	private int GetGroupNumber()
	{
		if (_groups.Count == 0)
		{
			return (int)MapBlockType.MT_DEFAULT;
		}
		if (_cumulativeFrequency > 0)
		{
			int pick = Rng.Generate(0, _cumulativeFrequency - 1);
			for (int i = 0; i != _groupsTemp.Count; ++i)
			{
				if (pick < _frequenciesTemp[i])
				{
					int retVal = _groupsTemp[i];

					if (_maxUsesTemp[i] > 0)
					{
						if (--_maxUsesTemp[i] == 0)
						{
							_groupsTemp.RemoveAt(i);
							_cumulativeFrequency -= _frequenciesTemp[i];
							_frequenciesTemp.RemoveAt(i);
							_maxUsesTemp.RemoveAt(i);
						}
					}
					return retVal;
				}
				pick -= _frequenciesTemp[i];
			}
		}
		return (int)MapBlockType.MT_UNDEFINED;
	}

	/// <summary>
	/// Gets a random block number from the array, accounting for frequencies and max uses.
	/// If no blocks are defined, it will use a group instead.
	/// </summary>
	private int GetBlockNumber()
	{
		if (_cumulativeFrequency > 0)
		{
			int pick = Rng.Generate(0, _cumulativeFrequency - 1);
			for (int i = 0; i != _blocksTemp.Count; ++i)
			{
				if (pick < _frequenciesTemp[i])
				{
					int retVal = _blocksTemp[i];

					if (_maxUsesTemp[i] > 0)
					{
						if (--_maxUsesTemp[i] == 0)
						{
							_blocksTemp.RemoveAt(i);
							_cumulativeFrequency -= _frequenciesTemp[i];
							_frequenciesTemp.RemoveAt(i);
							_maxUsesTemp.RemoveAt(i);
						}
					}
					return retVal;
				}
				pick -= _frequenciesTemp[i];
			}
		}
		return (int)MapBlockType.MT_UNDEFINED;
	}

	/// <summary>Gets a random map block from a given terrain, using either the groups or the blocks defined.</summary>
	/// <param name="terrain">The terrain to pick a block from.</param>
	/// <returns>A randomly chosen map block, given the options available (or null).</returns>
	public MapBlock GetNextBlock(RuleTerrain terrain)
	{
		ArgumentNullException.ThrowIfNull(terrain);
		if (_blocks.Count == 0)
		{
			return terrain.GetRandomMapBlock(_sizeX * 10, _sizeY * 10, GetGroupNumber());
		}
		int result = GetBlockNumber();
		if (result < terrain.MapBlocks.Count && result != (int)MapBlockType.MT_UNDEFINED)
		{
			return terrain.MapBlocks[result];
		}
		return null;
	}

	/// <summary>Gets the UFO's name (for setUFO).</summary>
	public string UFOName => _ufoName;

	/// <summary>Gets the craft's name (for addCraft).</summary>
	public string CraftName => _craftName;

	/// <summary>Gets the alternate terrain list for this command.</summary>
	public List<string> RandomAlternateTerrain => _randomTerrain;

	/// <summary>Gets the vertical levels defined in a map script command.</summary>
	public List<VerticalLevel> VerticalLevels => _verticalLevels;

	/// <summary>
	/// For use only with base defense maps as a special case, set _verticalLevels directly for a new MapScript.
	/// </summary>
	/// <param name="verticalLevels">The vertical levels (copied, like the C++ vector assignment).</param>
	/// <param name="sizeX">The X-size of the facility whose VerticalLevels are being loaded.</param>
	/// <param name="sizeY">The Y-size of the facility whose VerticalLevels are being loaded.</param>
	public void SetVerticalLevels(List<VerticalLevel> verticalLevels, int sizeX, int sizeY)
	{
		ArgumentNullException.ThrowIfNull(verticalLevels);
		_verticalLevels = verticalLevels.ConvertAll(v => new VerticalLevel(v));
		_sizeX = sizeX;
		_sizeY = sizeY;
	}
}
