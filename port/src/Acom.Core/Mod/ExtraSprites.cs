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
// Ported from: src/Mod/ExtraSprites.h, src/Mod/ExtraSprites.cpp
#nullable disable

namespace Acom;

/// <summary>For adding a set of extra sprite data to the game.</summary>
public class ExtraSprites
{
	private string _type = "";
	private SortedDictionary<int, string> _sprites = [];
	private ModData _current;
	private int _width, _height;
	private bool _singleImage;
	private int _subX, _subY;
	private bool _loaded;

	/// <summary>Creates a blank set of extra sprite data.</summary>
	public ExtraSprites()
	{
		_current = null;
		_width = 320;
		_height = 200;
		_singleImage = false;
		_subX = 0;
		_subY = 0;
		_loaded = false;
	}

	/// <summary>Loads the extra sprite set from YAML.</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="current">The data of the associated mod.</param>
	public void Load(YamlNodeReader reader, ModData current)
	{
		reader.TryRead("type", ref _type);
		if (_type.Length == 0)
		{
			string typeSingle = "";
			reader.TryRead("typeSingle", ref typeSingle);
			if (typeSingle.Length != 0)
			{
				_type = typeSingle;
				_singleImage = true;
			}
			string fileSingle = "";
			reader.TryRead("fileSingle", ref fileSingle);
			if (fileSingle.Length != 0)
			{
				_sprites[0] = fileSingle;
			}
		}
		reader.TryRead("files", ref _sprites);
		reader.TryRead("width", ref _width);
		reader.TryRead("height", ref _height);
		reader.TryRead("singleImage", ref _singleImage);
		reader.TryRead("subX", ref _subX);
		reader.TryRead("subY", ref _subY);
		_current = current;
	}

	/// <summary>Gets the filename that this sprite represents.</summary>
	public string Type => _type;

	/// <summary>Gets the list of sprites defined by this mod.</summary>
	public SortedDictionary<int, string> Sprites => _sprites;

	/// <summary>Gets the width of the surfaces (used for single images and new spritesets).</summary>
	public int Width => _width;

	/// <summary>Gets the height of the surfaces (used for single images and new spritesets).</summary>
	public int Height => _height;

	/// <summary>Returns whether this is a single surface as opposed to a set of surfaces.</summary>
	public bool SingleImage => _singleImage;

	/// <summary>Gets the x subdivision.</summary>
	public int SubX => _subX;

	/// <summary>Gets the y subdivision.</summary>
	public int SubY => _subY;

	/// <summary>Returns if the sprite is loaded.</summary>
	public bool IsLoaded => _loaded;

	private static readonly string[] s_exts = ["PNG", "GIF", "BMP", "LBM", "IFF", "PCX", "TGA", "TIF", "TIFF"];

	/// <summary>Determines if an image file is an acceptable format for the game.</summary>
	public static bool IsImageFile(string filename)
	{
		for (int i = 0; i < s_exts.Length; ++i)
		{
			if (CrossPlatform.CompareExt(filename, s_exts[i]))
				return true;
		}
		return false;
	}

	/// <summary>Loads the external sprite into a new or existing surface.</summary>
	/// <param name="surface">Existing surface.</param>
	/// <returns>New surface.</returns>
	public Surface LoadSurface(Surface surface)
	{
		if (!_singleImage)
			return surface;

		_loaded = true;

		if (surface == null)
		{
			Logger.Verbose("Creating new single image: " + _type);
		}
		else
		{
			Logger.Verbose("Adding/Replacing single image: " + _type);
		}
		surface = new Surface(_width, _height);
		surface.LoadImage(_sprites.First().Value);
		return surface;
	}

	/// <summary>Loads the external sprite into a new or existing surface set.</summary>
	/// <param name="set">Existing surface set.</param>
	/// <returns>New surface set.</returns>
	public SurfaceSet LoadSurfaceSet(SurfaceSet set)
	{
		if (_singleImage)
			return set;

		_loaded = true;
		bool subdivision = _subX != 0 && _subY != 0;
		int surfaceSetX = subdivision ? _subX : _width;
		int surfaceSetY = subdivision ? _subY : _height;

		if (set == null)
		{
			Logger.Verbose("Creating new surface set: " + _type);
			set = new SurfaceSet(surfaceSetX, surfaceSetY);
		}
		else
		{
			Logger.Verbose("Adding/Replacing items in surface set: " + _type);

			if (set.TotalFrames == 0 && (set.Width != surfaceSetX || set.Height != surfaceSetY))
			{
				Logger.Verbose("Resize empty set to: " + surfaceSetX + " x " + surfaceSetY);
				int shared = set.MaxSharedFrames;
				// C++ assigns a new set in place (*set = SurfaceSet(...)); the caller stores the returned set
				set = new SurfaceSet(surfaceSetX, surfaceSetY);
				set.MaxSharedFrames = shared;
			}
		}

		foreach (var pair in _sprites)
		{
			int startFrame = pair.Key;
			string fileName = pair.Value;
			if (fileName.EndsWith('/'))
			{
				Logger.Verbose("Loading surface set from folder: " + fileName + " starting at frame: " + startFrame);
				int offset = startFrame;
				var contents = new List<string>(FileMap.GetVFolderContents(fileName));
				contents.Sort(Unicode.NaturalComparison);
				foreach (var name in contents)
				{
					if (!IsImageFile(name))
						continue;
					try
					{
						GetFrame(set, offset).LoadImage(fileName + name);
						offset++;
					}
					catch (Exception e) when (e is InvalidOperationException or InvalidDataException or IOException)
					{
						Logger.Warning(e.Message);
					}
				}
			}
			else
			{
				if (!subdivision)
				{
					GetFrame(set, startFrame).LoadImage(fileName);
				}
				else
				{
					var temp = new Surface(_width, _height);
					temp.LoadImage(fileName);
					int xDivision = _width / _subX;
					int yDivision = _height / _subY;
					int frames = xDivision * yDivision;
					Logger.Verbose("Subdividing into " + frames + " frames.");
					int offset = startFrame;

					for (int y = 0; y != yDivision; ++y)
					{
						for (int x = 0; x != xDivision; ++x)
						{
							Surface frame = GetFrame(set, offset);
							// for some reason regular blit() doesn't work here how i want it, so i use this function instead.
							temp.BlitNShade(frame, 0 - (x * _subX), 0 - (y * _subY), 0);
							++offset;
						}
					}
				}
			}
		}
		return set;
	}

	private Surface GetFrame(SurfaceSet set, int index)
	{
		int indexWithOffset = index;
		if (indexWithOffset >= set.MaxSharedFrames)
		{
			if (indexWithOffset >= _current.size)
			{
				throw new InvalidOperationException("ExtraSprites '" + _type + "' frame '" + indexWithOffset + "' exceeds mod '" + _current.name + "' size limit " + _current.size);
			}
			indexWithOffset += _current.offset;
		}
		else if (indexWithOffset < 0)
		{
			throw new InvalidOperationException("ExtraSprites '" + _type + "' frame '" + indexWithOffset + "' in mod '" + _current.name + "' is not allowed.");
		}

		Surface frame = set.GetFrame(indexWithOffset);
		if (frame != null)
		{
			Logger.Verbose("Replacing frame: " + index + ", using index: " + indexWithOffset);
			frame.Clear();
		}
		else
		{
			Logger.Verbose("Adding frame: " + index + ", using index: " + indexWithOffset);
			frame = set.AddFrame(indexWithOffset);
		}
		return frame;
	}

	/// <summary>Gets mod data that define this surface.</summary>
	public ModData ModOwner => _current;
}
