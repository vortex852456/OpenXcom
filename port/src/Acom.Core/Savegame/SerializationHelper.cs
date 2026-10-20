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
// Ported from: src/Savegame/SerializationHelper.h, src/Savegame/SerializationHelper.cpp
// Also contains YamlNodeReader::readValBase64 / YamlNodeWriter::writeBase64 (src/Engine/Yaml.cpp) as extension
// methods, because the binary tile data of battle saves is their only user.
#nullable disable

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;

namespace Acom;

/// <summary>
/// Packing of integers into binary buffers (tile data of battle saves) and exact double formatting.
/// The C++ functions take <c>Uint8**</c> and advance the caller's pointer; here the span is advanced instead.
/// </summary>
public static class SerializationHelper
{
	/// <summary>Reads an integer of <paramref name="sizeKey"/> bytes (1, 2 or 4) and advances the buffer.</summary>
	/// <remarks>Size 1 is unsigned, size 2 signed (Sint16), size 4 unsigned (Uint32, wraps into int); little endian like x86.</remarks>
	public static int UnserializeInt(ref ReadOnlySpan<byte> buffer, byte sizeKey)
	{
		int ret = 0;
		switch (sizeKey)
		{
			case 1:
				ret = buffer[0];
				break;
			case 2:
				ret = BinaryPrimitives.ReadInt16LittleEndian(buffer);
				break;
			case 3:
				Debug.Fail("no.");
				break;
			case 4:
				ret = unchecked((int)BinaryPrimitives.ReadUInt32LittleEndian(buffer));
				break;
			default:
				Debug.Fail("get out.");
				break;
		}

		buffer = buffer[sizeKey..];

		return ret;
	}

	/// <summary>Writes an integer as <paramref name="sizeKey"/> bytes (1, 2 or 4) and advances the buffer.</summary>
	public static void SerializeInt(ref Span<byte> buffer, byte sizeKey, int value)
	{
		switch (sizeKey)
		{
			case 1:
				Debug.Assert(value < 256);
				buffer[0] = unchecked((byte)value);
				break;
			case 2:
				Debug.Assert(value < 65536);
				BinaryPrimitives.WriteInt16LittleEndian(buffer, unchecked((short)value));
				break;
			case 3:
				Debug.Fail("no.");
				break;
			case 4:
				BinaryPrimitives.WriteUInt32LittleEndian(buffer, unchecked((uint)value));
				break;
			default:
				Debug.Fail("get out.");
				break;
		}

		buffer = buffer[sizeKey..];
	}

	/// <summary>Formats a double like <c>std::ostream</c> with precision DBL_DIG + 2 (printf "%.17g"), so it round-trips.</summary>
	public static string SerializeDouble(double value)
	{
		if (double.IsNaN(value))
			return double.IsNegative(value) ? "-nan" : "nan";
		if (double.IsInfinity(value))
			return value > 0 ? "inf" : "-inf";
		// .NET "G17" uses the same fixed/scientific switch as %g and also strips trailing zeros
		return value.ToString("G17", CultureInfo.InvariantCulture).Replace("E", "e", StringComparison.Ordinal);
	}

	/// <summary>Reads a base64 scalar as binary data (C++ YamlNodeReader::readValBase64).</summary>
	public static byte[] ReadValBase64(this YamlNodeReader reader) => Convert.FromBase64String(reader.Val);

	/// <summary>Adds a key with binary data encoded as base64 (C++ YamlNodeWriter::writeBase64).</summary>
	public static YamlNodeWriter WriteBase64(this YamlNodeWriter writer, string key, ReadOnlySpan<byte> data)
	{
		ArgumentNullException.ThrowIfNull(writer);
		return writer.Write(key, Convert.ToBase64String(data));
	}
}
