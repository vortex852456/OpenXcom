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
// Ported from: src/Battlescape/PathfindingNode.h, src/Battlescape/PathfindingNode.cpp
#nullable disable

namespace Acom;

/// <summary>Time unit and energy cost of a path (Sint16 fields like in C++, arithmetic wraps).</summary>
public struct PathfindingCost
{
	public short time;
	public short energy;

	/// <summary>Constructor from ints (truncated to 16 bit like the C++ Sint16 fields).</summary>
	public PathfindingCost(int t, int e)
	{
		time = unchecked((short)t);
		energy = unchecked((short)e);
	}

	public static PathfindingCost operator +(PathfindingCost a, PathfindingCost c) => new(a.time + c.time, a.energy + c.energy);

	/// <summary>Named alternative of <c>operator+</c>.</summary>
	public static PathfindingCost Add(PathfindingCost a, PathfindingCost c) => a + c;

	public static bool operator <=(PathfindingCost a, PathfindingCost c) => a.time <= c.time && a.energy <= c.energy;

	public static bool operator >=(PathfindingCost a, PathfindingCost c) => a.time >= c.time && a.energy >= c.energy;

	/// <summary>Both components smaller (a partial order, not a strict weak ordering).</summary>
	public static bool operator <(PathfindingCost a, PathfindingCost c) => a.time < c.time && a.energy < c.energy;

	public static bool operator >(PathfindingCost a, PathfindingCost c) => a.time > c.time && a.energy > c.energy;
}

/// <summary>One step of a path.</summary>
public struct PathfindingStep
{
	/// <summary>Real cost of moving one step.</summary>
	public PathfindingCost cost;
	/// <summary>Pathfinding penalty for given step.</summary>
	public PathfindingCost penalty;
	/// <summary>Final position of one step.</summary>
	public Position pos;
}

/// <summary>A node of the pathfinding graph (one per tile).</summary>
public class PathfindingNode
{
	private readonly Position _pos;
	private PathfindingCost _tuCost;
	private PathfindingNode _prevNode;
	private int _prevDir;
	/// <summary>Approximate cost to reach goal position.</summary>
	private short _tuGuess;
	/// <summary>Is best path find for this tile.</summary>
	private bool _checked;
	/// <summary>Invasive field needed by PathfindingOpenSet (C++ friend).</summary>
	internal byte _openentry;

	/// <summary>Creates a new PathfindingNode class.</summary>
	public PathfindingNode(Position pos)
	{
		_pos = pos;
		_prevNode = null;
		_prevDir = 0;
		_tuGuess = 0;
		_checked = false;
		_openentry = 0;
	}

	/// <summary>Gets the node position.</summary>
	public Position Position => _pos;

	/// <summary>Resets the node.</summary>
	public void Reset()
	{
		_checked = false;
		_openentry = 0;
	}

	/// <summary>Is checked?</summary>
	public bool IsChecked => _checked;

	/// <summary>Marks the node as checked.</summary>
	public void SetChecked() => _checked = true;

	/// <summary>Gets the TU cost.</summary>
	public PathfindingCost GetTUCost(bool missile) => missile ? default : _tuCost;

	/// <summary>Gets the previous node.</summary>
	public PathfindingNode PrevNode => _prevNode;

	/// <summary>Gets the previous walking direction.</summary>
	public int PrevDir => _prevDir;

	/// <summary>Is this node already in a PathfindingOpenSet?</summary>
	public bool InOpenSet() => _openentry != 0;

	/// <summary>Gets the approximate cost to reach the target position.</summary>
	public int TUGuess => _tuGuess;

	/// <summary>Connects to previous node along the path.</summary>
	public void Connect(PathfindingCost cost, PathfindingNode prevNode, int prevDir, Position target)
	{
		_tuCost = cost;
		_prevNode = prevNode;
		_prevDir = prevDir;
		if (!InOpenSet()) // Otherwise we have this already.
		{
			_tuGuess = (short)(4 * Position.Distance(target, _pos)); // float -> Sint16 truncates
		}
	}

	/// <summary>Connects to previous node along a visit.</summary>
	public void Connect(PathfindingCost cost, PathfindingNode prevNode, int prevDir)
	{
		_tuCost = cost;
		_prevNode = prevNode;
		_prevDir = prevDir;
		_tuGuess = 0;
	}
}

/// <summary>Comparator for sorting nodes by cost (C++ functor).</summary>
public static class MinNodeCosts
{
	/// <summary>Compares nodes a and b; true if node a must come before b.</summary>
	public static bool Compare(PathfindingNode a, PathfindingNode b) => a.GetTUCost(false) < b.GetTUCost(false);
}
