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
// Ported from: src/Engine/SurfaceSet.h, src/Engine/SurfaceSet.cpp

namespace Acom;

/// <summary>
/// Container of a set of surfaces.
/// Used to manage single images that contain series of
/// frames inside, like animated sprites, making them easier
/// to access without constant cropping.
/// </summary>
public sealed class SurfaceSet
{
	private readonly List<Surface?> _frames = [];
	private readonly int _width, _height;
	private int _sharedFrames = int.MaxValue;

	/// <summary>Sets up a new empty surface set for frames of the specified size.</summary>
	public SurfaceSet(int width, int height)
	{
		_width = width;
		_height = height;
	}

	/// <summary>Frame width.</summary>
	public int Width => _width;

	/// <summary>Frame height.</summary>
	public int Height => _height;

	/// <summary>Number of frame slots (some may be empty).</summary>
	public int TotalFrames => _frames.Count;

	/// <summary>Number of frames shared by all mods (mod frame indexes start above it).</summary>
	public int MaxSharedFrames
	{
		get => _sharedFrames;
		set
		{
			if (value >= 0)
				_sharedFrames = value;
		}
	}

	/// <summary>
	/// Loads the contents of an X-Com set of PCK/TAB image files into the surface.
	/// The PCK file contains an RLE compressed image, while the TAB file contains
	/// the offsets to each frame in the image.
	/// </summary>
	/// <param name="pck">PCK data.</param>
	/// <param name="tab">TAB data, or null for a single-frame PCK.</param>
	public void LoadPck(Stream pck, Stream? tab = null)
	{
		ArgumentNullException.ThrowIfNull(pck);
		_frames.Clear();

		int nframes = 1;
		if (tab != null)
		{
			// Load TAB and get image offsets
			Span<byte> first = stackalloc byte[4];
			first.Clear();
			tab.ReadAtLeast(first, 4, throwOnEndOfStream: false);
			int off = BitConverter.ToInt32(first);
			long size = tab.Length;
			// 16-bit offsets (UFO) unless the first 32-bit offset is zero (TFTD)
			nframes = (int)(off != 0 ? size / 2 : size / 4);
		}
		for (int frame = 0; frame < nframes; ++frame)
			_frames.Add(new Surface(_width, _height, 0, 0, 1));

		int Read() => pck.ReadByte();
		for (int frame = 0; frame < nframes; ++frame)
		{
			var surface = _frames[frame]!;
			int x = 0, y = 0;
			int value = Read();
			if (value < 0)
				break;
			// Leading transparent rows
			for (int i = 0; i < value; ++i)
				for (int j = 0; j < _width; ++j)
					surface.SetPixelIterative(ref x, ref y, 0);

			while ((value = Read()) >= 0 && value != 255)
			{
				if (value == 254)
				{
					int n = Read();
					for (int i = 0; i < n; ++i)
						surface.SetPixelIterative(ref x, ref y, 0);
				}
				else
				{
					surface.SetPixelIterative(ref x, ref y, (byte)value);
				}
			}
		}
	}

	/// <summary>
	/// Loads the contents of an X-Com DAT image file into the surface.
	/// Unlike the PCK, a DAT file is an uncompressed image with no offsets
	/// so these have to be figured out manually, usually by splitting the
	/// image into equal portions.
	/// </summary>
	public void LoadDat(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		_frames.Clear();
		using var buffer = new MemoryStream();
		stream.CopyTo(buffer);
		var data = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
		int frameSize = _width * _height;
		int nframes = data.Length / frameSize;
		for (int i = 0; i < nframes; ++i)
		{
			var surface = new Surface(_width, _height, 0, 0, 1);
			surface.LoadRaw(data.Slice(i * frameSize, frameSize));
			_frames.Add(surface);
		}
	}

	/// <summary>Gets a frame, or null if the slot is empty or out of range.</summary>
	public Surface? GetFrame(int i) => (uint)i < (uint)_frames.Count ? _frames[i] : null;

	/// <summary>Creates (or replaces) a frame at the given index.</summary>
	public Surface AddFrame(int i)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(i);
		while (_frames.Count <= i)
			_frames.Add(null);
		var surface = new Surface(_width, _height);
		_frames[i] = surface;
		return surface;
	}

	/// <summary>Changes a set of colors on all frames' palettes.</summary>
	public void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		foreach (var frame in _frames)
			frame?.SetPalette(colors, firstcolor, ncolors);
	}
}
