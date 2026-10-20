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
// Ported from: src/Engine/CatFile.h, src/Engine/CatFile.cpp

namespace Acom.Engine;

/// <summary>
/// Read-only access to the items of an X-Com CAT archive.
/// </summary>
/// <remarks>
/// File format: <c>{offset_le32, size_le32}[N]; bytes[]</c>. The first offset determines the
/// table size (N = offset / 8). The stored sizes are unreliable (they may or may not include an
/// internal name chunk <c>{int8 size, char[size] name}</c>), so the true item size is calculated
/// from the offsets only.
/// </remarks>
public sealed class CatFile
{
	private readonly byte[] _data;
	private readonly List<(int Offset, int Size)> _items = [];

	/// <summary>Reads a CAT archive.</summary>
	/// <param name="stream">Archive data.</param>
	/// <param name="fileName">Name for diagnostics.</param>
	public CatFile(Stream stream, string fileName = "")
	{
		ArgumentNullException.ThrowIfNull(stream);
		FileName = fileName;
		using var buffer = new MemoryStream();
		stream.CopyTo(buffer);
		_data = buffer.ToArray();
		if (_data.Length < 4)
			return;

		uint offset0 = BitConverter.ToUInt32(_data, 0);
		if (offset0 >= (uint)_data.Length)
		{
			Logger.Warning($"Catfile({fileName}): first offset {offset0} >= file size {_data.Length}, not parsing.");
			return;
		}
		for (uint i = 0; i < offset0 / 8 && i * 8 + 4 <= (uint)_data.Length; ++i)
		{
			uint offset = BitConverter.ToUInt32(_data, (int)(i * 8));
			// reject bad data
			if (offset >= (uint)_data.Length)
			{
				Logger.Warning($"Catfile({fileName}): item {i} outside of the file: offset={offset} filesize={_data.Length}");
				continue;
			}
			_items.Add(((int)offset, 0));
		}
		int lastOffset = _data.Length;
		for (int i = _items.Count - 1; i >= 0; --i)
		{
			int thisOffset = _items[i].Offset;
			_items[i] = (thisOffset, lastOffset - thisOffset);
			lastOffset = thisOffset;
		}
	}

	/// <summary>Name of the archive (diagnostics only).</summary>
	public string FileName { get; }

	/// <summary>Number of items.</summary>
	public int Size => _items.Count;

	/// <summary>Gets the raw bytes of an item (including a name chunk, if present).</summary>
	public ReadOnlyMemory<byte> GetItem(int i)
	{
		if ((uint)i >= (uint)_items.Count)
		{
			Logger.Error($"Catfile<{FileName}>::getRWops({i}): >= size {_items.Count}");
			return ReadOnlyMemory<byte>.Empty;
		}
		var (offset, size) = _items[i];
		return _data.AsMemory(offset, Math.Max(0, size));
	}
}
