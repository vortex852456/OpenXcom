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
// Ported from: src/Battlescape/TileEngine.cpp (light propagation helpers)
#nullable disable

using System.Runtime.CompilerServices;

namespace Acom;

public partial class TileEngine
{
	////////////////////////////////////////////////////////////
	//					light propagation
	////////////////////////////////////////////////////////////

	// Index to component of Pos (enum Axis)
	internal const int X = 0;
	internal const int Y = 1;
	internal const int Z = 2;
	internal const int AxisMax = 3;
	internal const int AxisInvalid = -1;

	// Index to component of Box (enum BoxAxis): B_X..B_Z = 0..2, E_X..E_Z = 3..5
	internal const int BoxAxisMax = AxisMax + AxisMax;

	// Index to vertex of box (enum BoxVertex): V_000 = 0b000 .. V_111 = 0b111
	private const int V_000 = 0b000;
	private const int V_001 = 0b001;
	private const int V_010 = 0b010;
	private const int V_011 = 0b011;
	private const int V_100 = 0b100;
	private const int V_111 = 0b111;

	/// <summary>3d position, similar to <see cref="Position"/> but optimized for uniformed processing (C++ <c>std::array&lt;int, 3&gt;</c>).</summary>
	[InlineArray(AxisMax)]
	internal struct Pos
	{
		private int _element0;

		public Pos(int x, int y, int z)
		{
			this[X] = x;
			this[Y] = y;
			this[Z] = z;
		}
	}

	/// <summary>
	/// 3d Box, effective contains two <see cref="Pos"/> (C++ <c>std::array&lt;int, 6&gt;</c>).
	/// Both ends will be visited iterated if is end point.
	/// But algorithm will skip first step in iteration.
	/// </summary>
	[InlineArray(BoxAxisMax)]
	internal struct Box
	{
		private int _element0;
	}

	private static Box FromPos(Pos a, Pos b)
	{
		Box r = default;
		r[0] = a[X];
		r[1] = a[Y];
		r[2] = a[Z];
		r[3] = b[X];
		r[4] = b[Y];
		r[5] = b[Z];
		return r;
	}

	private static Pos Add(Pos a, Pos b)
	{
		for (int i = 0; i < AxisMax; ++i)
		{
			a[i] += b[i];
		}
		return a;
	}

	private static bool IsIntersectingWith(Box target, Box limit)
	{
		for (int i = 0; i < AxisMax; ++i)
		{
			if ((target[i] > limit[i + AxisMax]) || (limit[i] > target[i + AxisMax]))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>Crop first argument to second, both need not empty intersection others wise it will return wrong answer.</summary>
	private static void IntersectWithUnchecked(ref Box target, Box limit)
	{
		for (int i = 0; i < AxisMax; ++i)
		{
			target[i] = Math.Max(target[i], limit[i]);
			target[i + AxisMax] = Math.Min(target[i + AxisMax], limit[i + AxisMax]);
		}
	}

	private static void Expand(ref Box box)
	{
		for (int i = 0; i < BoxAxisMax; ++i)
		{
			box[i] += i < AxisMax ? -1 : 1;
		}
	}

	private static readonly int[] diffToAxis =
	[
		AxisInvalid,
		X,  // 0b001
		Y,  // 0b010
		AxisInvalid,
		Z,  // 0b100
		AxisInvalid,
		AxisInvalid,
		AxisInvalid,
	];

	/// <summary>One edge direction of a box side (C++ <c>ConfigSide::Direction</c>).</summary>
	internal readonly struct ConfigDirection
	{
		public readonly int axis;
		public readonly int dir;
		public readonly int first;
		public readonly int last;

		public ConfigDirection(int axis, int dir, int first, int last)
		{
			this.axis = axis;
			this.dir = dir;
			this.first = first;
			this.last = last;
		}

		public static ConfigDirection Fill(int from, int to)
		{
			bool asc = from < to;
			int axis = diffToAxis[from ^ to];

			return new ConfigDirection(
				axis,
				asc ? +1 : -1,
				axis + (asc ? 0 : AxisMax),
				axis + (asc ? AxisMax : 0)
			);
		}
	}

	/// <summary>Iteration order of one box side (C++ <c>ConfigSide</c>).</summary>
	internal readonly struct ConfigSide
	{
		public readonly ConfigDirection i, j, k;

		public ConfigSide(ConfigDirection i, ConfigDirection j, ConfigDirection k)
		{
			this.i = i;
			this.j = j;
			this.k = k;
		}

		public static ConfigSide Fill(int start, int ii, int jj)
		{
			// `kk` is determined as vertex that lie on orthogonal line to surface defined by vertexes `start`, `ii` and `jj`.
			int kk = start ^ (V_111 - (start ^ jj) - (start ^ ii));

			return new ConfigSide(
				ConfigDirection.Fill(start, ii),
				ConfigDirection.Fill(start, jj),
				ConfigDirection.Fill(start, kk)
			);
		}
	}

	private const int SquareLoopSize = 4;

	/// <summary>Object with definition of operation order.</summary>
	internal static readonly ConfigSide[] propagationSequence = CreatePropagationSequence();

	private static ConfigSide[] CreatePropagationSequence()
	{
		var s = new ConfigSide[24];

		static int Up(int e) => e + V_100;
		static int Curr(int[] a, int x) => a[x];
		static int Next(int[] a, int x) => a[(x + 1) % SquareLoopSize];
		static int Prev(int[] a, int x) => a[(x - 1 + SquareLoopSize) % SquareLoopSize];

		int total = 0;

		int[] floor_loop =
		[
			V_000,
			V_001,
			V_011,
			V_010,
		];
		for (int j = 0; j < SquareLoopSize; ++j, ++total)
		{
			s[total] = ConfigSide.Fill(
				Curr(floor_loop, j),
				Next(floor_loop, j),
				Prev(floor_loop, j)
			);
		}
		for (int j = 0; j < SquareLoopSize; ++j, ++total)
		{
			s[total] = ConfigSide.Fill(
				Up(Curr(floor_loop, j)),
				Up(Next(floor_loop, j)),
				Up(Prev(floor_loop, j))
			);
		}
		for (int i = 0; i < SquareLoopSize; ++i)
		{
			int[] side =
			[
				Curr(floor_loop, i),
				Next(floor_loop, i),
				Up(Next(floor_loop, i)),
				Up(Curr(floor_loop, i)),
			];
			for (int j = 0; j < SquareLoopSize; ++j, ++total)
			{
				s[total] = ConfigSide.Fill(
					Curr(side, j),
					Next(side, j),
					Prev(side, j)
				);
			}
		}

		return s;
	}

	/// <summary>Callback for each position visited by the volume iteration.</summary>
	internal delegate void PosCallback(Pos p);

	private static void IterateSide(int k, Box b, Box e, ConfigSide c, PosCallback f)
	{
		Pos p = default;

		p[c.k.axis] = k;
		// iterateEdge(b, e, c.j, ...)
		int beginJ = b[c.j.first];
		int endJ = e[c.j.last];
		for (int pj = beginJ; pj != endJ; pj += c.j.dir)
		{
			p[c.j.axis] = pj;
			// iterateEdge(b, e, c.i, ...)
			int beginI = b[c.i.first];
			int endI = e[c.i.last];
			for (int pi = beginI; pi != endI; pi += c.i.dir)
			{
				p[c.i.axis] = pi;
				f(p);
			}
		}
	}

	private static void IterateSurface(ref Box box, Box limit, PosCallback f)
	{
		Box beginBox = box;

		Expand(ref box);

		Box crop = box;
		Box endBox = box;

		Expand(ref endBox);

		IntersectWithUnchecked(ref crop, limit);
		IntersectWithUnchecked(ref beginBox, limit);
		IntersectWithUnchecked(ref endBox, limit);

		foreach (ConfigSide c in propagationSequence)
		{
			//check if plane of side is inside of limit box
			if (box[c.k.first] == crop[c.k.first])
			{
				IterateSide(box[c.k.first], beginBox, endBox, c, f);
			}
		}
	}

	internal static void IterateVolume(Pos start, int eventRadius, int range, MapSubset gs, int end_z, PosCallback f)
	{
		Box b = FromPos(start, start);
		Box limit = default;
		limit[0] = gs.beg_x;
		limit[1] = gs.beg_y;
		limit[2] = 0;
		limit[3] = gs.end_x - 1;
		limit[4] = gs.end_y - 1;
		limit[5] = end_z - 1;

		//check that make sure that `intersectWithUnchecked` is guarantee to work correctly
		if (!IsIntersectingWith(b, limit))
		{
			return;
		}

		if (eventRadius == 0)
		{
			f(start);
		}
		else
		{
			//expanding range without update as it should be already updated, only edge need processing, this is why we "transfer" one step to next loop.
			range++;
			eventRadius--;
			while (eventRadius-- > 0)
			{
				Expand(ref b);
			}
		}

		while (range-- > 0)
		{
			IterateSurface(ref b, limit, f);
		}
	}

	private const int dir_max = 9;
	private static readonly int[] dir_x = [0, 0, +1, +1, +1, 0, -1, -1, -1,];
	private static readonly int[] dir_y = [0, -1, -1, 0, +1, +1, +1, 0, -1,];
	private static readonly int[] dir_z = [0, 0, 0, 0, 0, 0, 0, 0, 0,];

	private const int dir_level_max = 3;
	private static readonly int[] dir_level_x = [0, 0, 0];
	private static readonly int[] dir_level_y = [0, 0, 0];
	private static readonly int[] dir_level_z = [-1, 0, +1];

	private static Pos GetPosOffsetByDirections(int dir)
	{
		return new Pos(
			dir_x[dir + 1],
			dir_y[dir + 1],
			dir_z[dir + 1]
		);
	}

	private static Pos GetPosUpDown(int dir)
	{
		return new Pos(
			dir_level_x[dir + 1],
			dir_level_y[dir + 1],
			dir_level_z[dir + 1]
		);
	}

	/// <summary>One of the 27 neighbour directions of a tile.</summary>
	internal struct DirConfig
	{
		public sbyte level;
		public sbyte dir;
		public Pos offset;
		public uint mask;
		public uint next;
	}

	private static bool IsSameCubFace(Pos a, Pos b)
	{
		for (int i = 0; i < AxisMax; ++i)
		{
			if (a[i] != 0 && a[i] == b[i])
			{
				return true;
			}
		}
		return false;
	}

	private static int GetChebyshevDistance(Pos a, Pos b)
	{
		int dis = 0;
		for (int i = 0; i < AxisMax; ++i)
		{
			dis = Math.Max(Math.Abs(a[i] - b[i]), dis);
		}
		return dis;
	}

	internal const int dir3dMax = dir_level_max * dir_max;
	internal const uint dir3dStartMask = (1u << dir3dMax) - 1;

	/// <summary>Object with definition of directions to check.</summary>
	internal static readonly DirConfig[] directions = CreateDirections();

	private static DirConfig[] CreateDirections()
	{
		var array = new DirConfig[dir3dMax];

		for (int i = 0; i < dir3dMax; ++i)
		{
			ref DirConfig a = ref array[i];
			a.level = (sbyte)((i / dir_max) - 1);
			a.dir = (sbyte)((i % dir_max) - 1);
			a.offset = Add(GetPosOffsetByDirections(a.dir), GetPosUpDown(a.level));
			a.mask = SelectBit(a.dir, a.level);
		}

		for (int i = 0; i < dir3dMax; ++i)
		{
			ref DirConfig a = ref array[i];
			for (int j = 0; j < dir3dMax; ++j)
			{
				DirConfig b = array[j];

				if (IsSameCubFace(a.offset, b.offset) && GetChebyshevDistance(a.offset, b.offset) <= 1)
				{
					a.next |= b.mask;
				}
			}
		}

		return array;
	}

	/// <summary>Marks the tiles whose light needs an update: everything in reach of the event, propagated around terrain blocking.</summary>
	/// <param name="save">Map data.</param>
	private static void IterateTilesLightMaxBound(SavedBattleGame save, Position position, int eventRadius, int maxRange, MapSubset gsMap, uint[] work, uint[] blockCache)
	{
		if (position == invalid)
		{
			IterateTileIndexes(save, gsMap,
				idx =>
				{
					work[idx] = dir3dStartMask;
				}
			);

			return;
		}

		IterateTileIndexes(save, gsMap,
			idx =>
			{
				work[idx] = 0x0;
			}
		);
		IterateTilesIndexed(save, MapArea(position, eventRadius),
			(tile, idx) =>
			{
				//TODO: on multi level maps we skip far levels to speed calculations,
				// but sometimes we could skip even more levels, for `position` and `eventRadius` we lose
				// detailed info what tiles were in reality affected,
				// in most cases most updates are limited to one level.
				if (Math.Abs(tile.Position.z - position.z) <= eventRadius)
				{
					work[idx] = dir3dStartMask;
				}
			}
		);

		int map_mul_y = save.MapSizeX;
		int map_mul_z = map_mul_y * save.MapSizeY;
		int Index(Pos pp)
		{
			return pp[X] + pp[Y] * map_mul_y + pp[Z] * map_mul_z;
		}
		void Callback(Pos pp)
		{
			int idx = Index(pp);
			uint c = work[idx];

			uint check = c & ~blockCache[idx];
			if (check != 0)
			{
				foreach (DirConfig d in directions)
				{
					if ((d.mask & check) != 0)
					{
						work[Index(Add(pp, d.offset))] |= c & d.next;
					}
				}
			}
		}

		IterateVolume(new Pos(position.x, position.y, position.z), eventRadius, maxRange, gsMap, save.MapSizeZ, Callback);
	}
}
