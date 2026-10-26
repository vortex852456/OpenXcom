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
// Ported from: src/Savegame/Tile.h, src/Savegame/Tile.cpp (script binding)
#nullable disable

using System.Globalization;
using static Acom.ScriptParam;

namespace Acom;

public partial class Tile
{
	private static string DebugDisplayPosition(Position t)
	{
		return "Position(x: " + t.x.ToString(CultureInfo.InvariantCulture) + " y: " + t.y.ToString(CultureInfo.InvariantCulture) + " z: " + t.z.ToString(CultureInfo.InvariantCulture) + ")";
	}

	private static string DebugDisplayTile(Tile t)
	{
		if (t != null)
		{
			string s = ScriptName;
			s += "(x: " + t.Position.x.ToString(CultureInfo.InvariantCulture);
			s += " y: " + t.Position.y.ToString(CultureInfo.InvariantCulture);
			s += " z: " + t.Position.z.ToString(CultureInfo.InvariantCulture);
			s += " isVoid: " + (t.IsVoid ? "true" : "false");
			if (t.Unit != null)
			{
				s += " unit: \"" + t.Unit.Type + "\"";
			}
			s += ")";
			return s;
		}
		else
		{
			return "null";
		}
	}

	/// <summary>Register Tile (and Position) in script parser.</summary>
	public static void ScriptRegister(ScriptParserBase parser)
	{
		{
			const string name = "Position";
			parser.RegisterRawValueType<Position>(name);
			var rs = new BindValue<Position>(parser, name);

			rs.AddConstructor([Int, Int, Int], (ref ScriptArgs a) => { a.ValueRef<Position>(0) = new Position(a.Int(1), a.Int(2), a.Int(3)); });

			rs.AddField("getX", "setX", "addX", p => p.x, (ref Position p, int v) => p.x = unchecked((short)v));
			rs.AddField("getY", "setY", "addY", p => p.y, (ref Position p, int v) => p.y = unchecked((short)v));
			rs.AddField("getZ", "setZ", "addZ", p => p.z, (ref Position p, int v) => p.z = unchecked((short)v));

			var self = BindValue<Position>.Self;
			var selfRef = BindValue<Position>.SelfRef;
			rs.AddFreeFunction("mul", [selfRef, Int], (ref ScriptArgs a) => { ref var p = ref a.ValueRef<Position>(0); p = p * a.Int(1); });
			rs.AddFreeFunction("muldiv", [selfRef, Int, Int], (ref ScriptArgs a) =>
			{
				ref var p = ref a.ValueRef<Position>(0);
				int v = a.Int(1), d = a.Int(2);
				if (d != 0)
				{
					// to avoid overflow on Sint16 we multiply manually
					p = new Position(p.x * v / d, p.y * v / d, p.z * v / d);
				}
				else
				{
					p = default;
				}
			});
			rs.AddFreeFunction("mul", [selfRef, self], (ref ScriptArgs a) => { var v = a.Value<Position>(1); ref var p = ref a.ValueRef<Position>(0); p = p * v; });
			rs.AddFreeFunction("add", [selfRef, self], (ref ScriptArgs a) => { var v = a.Value<Position>(1); ref var p = ref a.ValueRef<Position>(0); p = p + v; });
			rs.AddFreeFunction("sub", [selfRef, self], (ref ScriptArgs a) => { var v = a.Value<Position>(1); ref var p = ref a.ValueRef<Position>(0); p = p - v; });

			rs.AddDebugValueDisplay(DebugDisplayPosition);
		}

		var t = new Bind<Tile>(parser);
		var tself = Bind<Tile>.Self;

		t.AddGet("getPosition.getX", x => x.Position.x);
		t.AddGet("getPosition.getY", x => x.Position.y);
		t.AddGet("getPosition.getZ", x => x.Position.z);
		t.Add("getPosition", [tself, ValueRef<Position>()], (ref ScriptArgs a) =>
		{
			var x = a.Ptr<Tile>(0);
			a.ValueRef<Position>(1) = x != null ? x.Position : default;
		});
		t.AddGet("getFire", x => x.Fire);
		t.AddGet("getSmoke", x => x.Smoke);
		t.AddGet("getShade", x => x.Shade);

		t.Add("isDiscovered", [tself, IntRef], (ref ScriptArgs a) =>
		{
			var x = a.Ptr<Tile>(0);
			a.IntRef(1) = x != null
				? ((x.IsDiscovered(TilePart.O_FLOOR) ? 1 : 0) << 0) | ((x.IsDiscovered(TilePart.O_WESTWALL) ? 1 : 0) << 1) | ((x.IsDiscovered(TilePart.O_NORTHWALL) ? 1 : 0) << 2)
				: 0;
		}, "Check what part of tile is discovered, 0x1 - floor, 0x2 - westwall, 0x4 - eastwall");
		t.Add("makeDiscovered", [Bind<Tile>.SelfE, Int], (ref ScriptArgs a) =>
		{
			var x = a.Ptr<Tile>(0);
			int i = a.Int(1);
			if (x != null)
			{
				if ((i & (1 << 0)) != 0) x.SetDiscovered(true, TilePart.O_FLOOR);
				if ((i & (1 << 1)) != 0) x.SetDiscovered(true, TilePart.O_WESTWALL);
				if ((i & (1 << 2)) != 0) x.SetDiscovered(true, TilePart.O_NORTHWALL);
			}
		}, "Make part of tile discovered, 0x1 - floor (will make both walls visible too), 0x2 - westwall, 0x4 - eastwall");

		t.AddGetPtr("getUnit", x => x.Unit);

		t.Add("getDistanceTile", [tself, IntRef, Ptr<Tile>()], (ref ScriptArgs a) =>
		{
			var x = a.Ptr<Tile>(0);
			var other = a.Ptr<Tile>(2);
			a.IntRef(1) = x != null && other != null ? (int)Position.Distance(x.Position, other.Position) : -1;
		});
		t.Add("getDistanceVoxel", [tself, IntRef, Ptr<Tile>()], (ref ScriptArgs a) =>
		{
			var x = a.Ptr<Tile>(0);
			var other = a.Ptr<Tile>(2);
			a.IntRef(1) = x != null && other != null ? (int)Position.Distance(x.Position.ToVoxel(), other.Position.ToVoxel()) : -1;
		});

		t.AddGet("getFloorSpecialTileType", x => (int)x.FloorSpecialTileType);
		t.AddGet("getObjectSpecialTileType", x => (int)x.ObjectSpecialTileType);

		t.AddDebugDisplay(DebugDisplayTile);

		t.AddCustomConst("STT_TILE", (int)SpecialTileType.TILE);
		t.AddCustomConst("STT_START_POINT", (int)SpecialTileType.START_POINT);
		t.AddCustomConst("STT_END_POINT", (int)SpecialTileType.END_POINT);
	}
}
