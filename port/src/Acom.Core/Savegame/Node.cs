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
// Ported from: src/Savegame/Node.h, src/Savegame/Node.cpp
#nullable disable

namespace Acom;

public enum NodeRank : uint
{
	NR_SCOUT = 0,
	NR_XCOM = 1,
	NR_SOLDIER = 2,
	NR_NAVIGATOR = 3,
	NR_LEADER = 4,
	NR_ENGINEER = 5,
	NR_MISC1 = 6,
	NR_MEDIC = 7,
	NR_MISC2 = 8,
}

/// <summary>
/// Represents a node/spawnpoint in the battlescape, loaded from RMP files.
/// </summary>
public class Node
{
	public const int CRAFTSEGMENT = 1000;
	public const int UFOSEGMENT = 2000;
	/// <summary>non-flying unit can not spawn here when this bit is set</summary>
	public const int TYPE_FLYING = 0x01;
	/// <summary>large unit can not spawn here when this bit is set</summary>
	public const int TYPE_SMALL = 0x02;
	/// <summary>an alien was shot here, stop patrolling to it like an idiot with a death wish</summary>
	public const int TYPE_DANGEROUS = 0x04;

	/// <summary>
	/// Maps alien ranks to node (.RMP) ranks: the order in which certain alien ranks spawn on certain node ranks.
	/// Note that they all can fall back to rank 0 nodes - which is scout (outside ufo).
	/// </summary>
	public static readonly int[,] nodeRank = {
		{ 4, 3, 5, 8, 7, 2, 0 }, //commander
		{ 4, 3, 5, 8, 7, 2, 0 }, //leader
		{ 5, 4, 3, 2, 7, 8, 0 }, //engineer
		{ 7, 6, 2, 8, 3, 4, 0 }, //medic
		{ 3, 4, 5, 2, 7, 8, 0 }, //navigator
		{ 2, 5, 3, 4, 6, 8, 0 }, //soldier
		{ 2, 5, 3, 4, 6, 8, 0 }, //terrorist
		{ 2, 5, 3, 4, 6, 8, 0 } }; //also terrorist

	private int _id;
	private Position _pos;
	private readonly int _segment;
	private List<int> _nodeLinks = [];
	private int _type;      // 19 = Unit type in Mapview 2
	private int _rank;      // 20 = Node rank in Mapview 2
	private int _flags;     // 21 = Patrol priority in Mapview 2
	private int _reserved;  // 22 = Base Attack in Mapview 2
	private int _priority;  // 23 = Spawn weight in Mapview 2
	private bool _allocated;
	private bool _dummy;

	/// <summary>Creates a Node.</summary>
	public Node()
	{
	}

	/// <summary>Initializes a Node.</summary>
	public Node(int id, Position pos, int segment, int type, int rank, int flags, int reserved, int priority)
	{
		_id = id;
		_pos = pos;
		_segment = segment;
		_type = type;
		_rank = rank;
		_flags = flags;
		_reserved = reserved;
		_priority = priority;
	}

	/// <summary>Loads the node from YAML.</summary>
	public void Load(YamlNodeReader reader)
	{
		reader.TryRead("id", ref _id);
		reader.TryRead("position", ref _pos);
		//reader.tryRead("segment", _segment);
		reader.TryRead("type", ref _type);
		reader.TryRead("rank", ref _rank);
		reader.TryRead("flags", ref _flags);
		reader.TryRead("reserved", ref _reserved);
		reader.TryRead("priority", ref _priority);
		reader.TryRead("allocated", ref _allocated);
		reader.TryRead("links", ref _nodeLinks);
		reader.TryRead("dummy", ref _dummy);
	}

	/// <summary>Saves the node to YAML.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.SetAsMap();
		writer.SetFlowStyle();
		writer.Write("id", _id);
		writer.Write("position", _pos);
		//writer.write("segment", _segment);
		writer.Write("type", _type);
		writer.Write("rank", _rank);
		writer.Write("flags", _flags);
		writer.Write("reserved", _reserved);
		writer.Write("priority", _priority);
		if (_allocated)
			writer.Write("allocated", _allocated);
		writer.Write("links", _nodeLinks);
		if (_dummy)
			writer.Write("dummy", _dummy);
	}

	/// <summary>Get the node's id.</summary>
	public int ID => _id;

	/// <summary>Get the node's paths.</summary>
	public List<int> NodeLinks => _nodeLinks;

	/// <summary>Get the rank of units that can spawn on this node.</summary>
	public NodeRank Rank => (NodeRank)_rank;

	/// <summary>Get the priority of this spawnpoint.</summary>
	public int Priority => _priority;

	/// <summary>Gets the Node's position.</summary>
	public Position Position => _pos;

	/// <summary>Gets the Node's segment.</summary>
	public int Segment => _segment;

	/// <summary>Gets/sets the Node's type.</summary>
	public int Type { get => _type; set => _type = value; }

	/// <summary>Gets "flags" variable, which is really the patrolling desirability value.</summary>
	public int Flags => _flags;

	// C++ operator< (compares _flags for patrol decisions) is unused and not ported.

	public bool IsAllocated => _allocated;

	public void AllocateNode() => _allocated = true;

	public void FreeNode() => _allocated = false;

	public bool IsTarget => _reserved == 5;

	public void SetDummy(bool dummy) => _dummy = dummy;

	public bool IsDummy => _dummy;
}
