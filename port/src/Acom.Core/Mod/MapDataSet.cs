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
// Ported from: src/Mod/MapDataSet.h, src/Mod/MapDataSet.cpp
#nullable disable

using System.Buffers.Binary;

namespace Acom;

/// <summary>
/// Represents a Terrain Map Datafile, which corresponds to an XCom MCD &amp; PCK file.
/// The list of map datafiles is stored in Mod, but referenced in RuleTerrain.
/// </summary>
/// <seealso href="http://www.ufopaedia.org/index.php?title=MCD"/>
public class MapDataSet
{
	/// <summary>Size of one record of the XCom MCD file format.</summary>
	internal const int MCD_RECORD_SIZE = 62;

	private static MapData s_blankTile;
	private static MapData s_scorchedTile;

	private readonly string _name;
	private readonly List<MapData> _objects = [];
	private SurfaceSet _surfaceSet;
	private bool _loaded;

	/// <summary>MapDataSet construction.</summary>
	public MapDataSet(string name)
	{
		_name = name;
	}

	/// <summary>Loads the LOFTEMPS.DAT into the ruleset voxeldata.</summary>
	/// <param name="filename">Filename of the DAT file.</param>
	/// <param name="voxelData">The ruleset voxel data.</param>
	public static void LoadLOFTEMPS(string filename, List<ushort> voxelData)
	{
		using var mapFile = FileMap.GetStream(filename);
		LoadLOFTEMPS(mapFile, voxelData);
	}

	/// <summary>Reads little-endian 16-bit values until the end of the stream (a trailing odd byte is ignored like in C++).</summary>
	internal static void LoadLOFTEMPS(Stream mapFile, List<ushort> voxelData)
	{
		Span<byte> value = stackalloc byte[2];
		while (mapFile.ReadAtLeast(value, 2, throwOnEndOfStream: false) == 2)
		{
			voxelData.Add(BinaryPrimitives.ReadUInt16LittleEndian(value));
		}
	}

	/// <summary>Gets the dataset name (used for MAP generation).</summary>
	public string Name => _name;

	/// <summary>Gets the dataset size in number of records.</summary>
	public int Size => _objects.Count;

	/// <summary>Gets the objects in this dataset.</summary>
	public List<MapData> ObjectsRaw => _objects;

	/// <summary>Gets an object in this dataset.</summary>
	/// <param name="i">Object index.</param>
	public MapData GetObject(int i)
	{
		if ((uint)i >= (uint)_objects.Count)
		{
			throw new InvalidOperationException("MCD " + _name + " has no object " + i);
		}
		return _objects[i];
	}

	/// <summary>Gets the surfaces in this dataset.</summary>
	public SurfaceSet Surfaceset => _surfaceSet;

	/// <summary>Loads terrain data in XCom format (MCD &amp; PCK files).</summary>
	/// <seealso href="http://www.ufopaedia.org/index.php?title=MCD"/>
	public void LoadData(MCDPatch patch, bool validate = true)
	{
		// prevents loading twice
		if (_loaded) return;
		_loaded = true;

		// Load Terrain Data from MCD file
		string fname = "TERRAIN/" + _name + ".MCD";
		using (var mapFile = FileMap.GetStream(fname))
		{
			LoadMcd(mapFile);
		}

		// apply any ruleset patches before validation
		if (patch != null)
		{
			patch.ModifyData(this);
		}

		// Validate MCD references
		if (validate)
		{
			for (int i = 0; i < _objects.Count; ++i)
			{
				if ((uint)_objects[i].DieMCD >= (uint)_objects.Count)
				{
					Logger.Info($"MCD {_name} object {i} has invalid DieMCD: {_objects[i].DieMCD}");
				}
				if ((uint)_objects[i].AltMCD >= (uint)_objects.Count)
				{
					Logger.Info($"MCD {_name} object {i} has invalid AltMCD: {_objects[i].AltMCD}");
				}
				if (_objects[i].Armor == 0)
				{
					Logger.Info($"MCD {_name} object {i} has 0 armor");
				}
			}
		}

		// Load terrain sprites/surfaces/PCK files into a surfaceset
		_surfaceSet = new SurfaceSet(32, 40);
		// SurfaceSet::loadPck opens the TAB before the PCK (which file is reported missing first)
		using var tab = FileMap.GetStream("TERRAIN/" + _name + ".TAB");
		using var pck = FileMap.GetStream("TERRAIN/" + _name + ".PCK");
		_surfaceSet.LoadPck(pck, tab);
	}

	/// <summary>
	/// Reads the records of an MCD file (62 bytes each) into new <see cref="MapData"/> objects.
	/// Like the C++ stream loop, an incomplete trailing record is ignored.
	/// </summary>
	internal void LoadMcd(Stream mapFile)
	{
		int objNumber = 0;
		var mcd = new byte[MCD_RECORD_SIZE];
		while (mapFile.ReadAtLeast(mcd, MCD_RECORD_SIZE, throwOnEndOfStream: false) == MCD_RECORD_SIZE)
		{
			var to = new MapData(this);
			_objects.Add(to);

			// field offsets of the packed C++ struct MCD
			const int Frame = 0, LOFT = 8, ScanG = 20;
			const int UFO_Door = 30, Stop_LOS = 31, No_Floor = 32, Big_Wall = 33, Gravlift = 34, Door = 35, Block_Fire = 36, Block_Smoke = 37;
			const int TU_Walk = 39, TU_Slide = 40, TU_Fly = 41, Armor = 42, HE_Block = 43, Die_MCD = 44, Flammable = 45, Alt_MCD = 46;
			const int T_Level = 48, P_Level = 49, Light_Block = 51, Footstep = 52, Tile_Type = 53, HE_Type = 54, HE_Strength = 55;
			const int Fuel = 57, Light_Source = 58, Target_Type = 59, Xcom_Base = 60;

			// set all the terrain object properties:
			for (int frame = 0; frame < 8; frame++)
			{
				to.SetSprite(frame, mcd[Frame + frame]);
			}
			to.YOffset = mcd[P_Level];
			to.SetSpecialType(mcd[Target_Type], (TilePart)mcd[Tile_Type]);
			to.SetTUCosts(mcd[TU_Walk], mcd[TU_Fly], mcd[TU_Slide]);
			to.SetFlags(mcd[UFO_Door] != 0, mcd[Stop_LOS] != 0, mcd[No_Floor] != 0, mcd[Big_Wall], mcd[Gravlift] != 0, mcd[Door] != 0, mcd[Block_Fire] != 0, mcd[Block_Smoke] != 0, mcd[Xcom_Base] != 0);
			to.TerrainLevel = (sbyte)mcd[T_Level];
			to.FootstepSound = mcd[Footstep];
			to.AltMCD = mcd[Alt_MCD];
			to.DieMCD = mcd[Die_MCD];
			to.SetBlockValue(mcd[Light_Block], mcd[Stop_LOS], mcd[HE_Block], mcd[Block_Smoke], mcd[Flammable], mcd[HE_Block]);
			to.LightSource = mcd[Light_Source];
			to.Armor = mcd[Armor];
			to.Flammable = mcd[Flammable];
			to.Fuel = mcd[Fuel];
			to.ExplosiveType = mcd[HE_Type];
			to.Explosive = mcd[HE_Strength];
			to.MiniMapIndex = BinaryPrimitives.ReadUInt16LittleEndian(mcd.AsSpan(ScanG, 2));

			for (int layer = 0; layer < 12; layer++)
			{
				int loft = mcd[LOFT + layer];
				to.SetLoftID(loft, layer);
			}

			// store the 2 tiles of blanks in a static - so they are accessible everywhere
			if (string.Equals(_name, "BLANKS", StringComparison.Ordinal))
			{
				if (objNumber == 0)
					s_blankTile = to;
				else if (objNumber == 1)
					s_scorchedTile = to;
			}
			objNumber++;
		}
	}

	/// <summary>Unloads the terrain data to free memory.</summary>
	public void UnloadData()
	{
		if (_loaded)
		{
			_objects.Clear();
			_surfaceSet = null;
			_loaded = false;
		}
	}

	/// <summary>Gets a blank floor tile.</summary>
	public static MapData BlankFloorTile => s_blankTile;

	/// <summary>Gets a scorched earth tile.</summary>
	public static MapData ScorchedEarthTile => s_scorchedTile;
}
