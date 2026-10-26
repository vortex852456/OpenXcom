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
// Ported from: src/Menu/TestState.h, src/Menu/TestState.cpp
// Port status: partial (missing: script hooks – test case 3 exports the armor table without script tag columns)
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Metadata of a vanilla palette for the palette check test.</summary>
public class PaletteTestMetadata
{
	public string paletteName;
	public int firstIndexToCheck;
	public int lastIndexToCheck;
	public int maxTolerance;
	public string palettePath;
	public bool usesBackPals;

	public PaletteTestMetadata()
	{
		paletteName = "";
		firstIndexToCheck = 0;
		lastIndexToCheck = 255;
		maxTolerance = 0;
		palettePath = "";
		usesBackPals = false;
	}

	public PaletteTestMetadata(string _paletteName, int _firstIndexToCheck, int _lastIndexToCheck, int _maxTolerance, string _palettePath, bool _usesBackPals)
	{
		paletteName = _paletteName;
		firstIndexToCheck = _firstIndexToCheck;
		lastIndexToCheck = _lastIndexToCheck;
		maxTolerance = _maxTolerance;
		palettePath = _palettePath;
		usesBackPals = _usesBackPals;
	}
}

/// <summary>A state for testing most common modding mistakes.</summary>
public class TestState : State
{
	// items are (roughly) ordered by frequency of use... to save a few CPU cycles when looking for a match
	private static readonly SortedDictionary<int, PaletteTestMetadata> _paletteMetadataMap = new()
	{
		[1] = new PaletteTestMetadata("UFO_PAL_BATTLEPEDIA", 1, 255, 0, "Palettes/UFO-JASC/PAL_BATTLEPEDIA.pal", false),
		[2] = new PaletteTestMetadata("UFO_PAL_BATTLESCAPE", 1, 255, 0, "Palettes/UFO-JASC/PAL_BATTLESCAPE.pal", false),

		[3] = new PaletteTestMetadata("UFO_PAL_BATTLE_COMMON", 1, 239, 0, "Palettes/UFO-JASC/PAL_BATTLE_COMMON.pal", false), // ignore last 16 colors

		[4] = new PaletteTestMetadata("TFTD_PAL_BATTLESCAPE", 1, 254, 3, "Palettes/TFTD-JASC/PAL_BATTLESCAPE.pal", false), // ignore last color too

		[5] = new PaletteTestMetadata("UFO_PAL_UFOPAEDIA", 1, 255, 0, "Palettes/UFO-JASC/PAL_UFOPAEDIA.pal", false),
		[6] = new PaletteTestMetadata("TFTD_PAL_BASESCAPE", 1, 255, 0, "Palettes/TFTD-JASC/PAL_BASESCAPE.pal", true), // TFTD's ufopedia

		[7] = new PaletteTestMetadata("UFO_PAL_BASESCAPE", 1, 255, 0, "Palettes/UFO-JASC/PAL_BASESCAPE.pal", true),

		[8] = new PaletteTestMetadata("UFO_PAL_GEOSCAPE", 1, 255, 0, "Palettes/UFO-JASC/PAL_GEOSCAPE.pal", true),
		[9] = new PaletteTestMetadata("TFTD_PAL_GEOSCAPE", 1, 255, 0, "Palettes/TFTD-JASC/PAL_GEOSCAPE.pal", true),

		[10] = new PaletteTestMetadata("TFTD_PAL_BATTLESCAPE_1", 1, 254, 3, "Palettes/TFTD-JASC/PAL_BATTLESCAPE_1.pal", false),
		[11] = new PaletteTestMetadata("TFTD_PAL_BATTLESCAPE_2", 1, 254, 3, "Palettes/TFTD-JASC/PAL_BATTLESCAPE_2.pal", false),
		[12] = new PaletteTestMetadata("TFTD_PAL_BATTLESCAPE_3", 1, 254, 3, "Palettes/TFTD-JASC/PAL_BATTLESCAPE_3.pal", false),

		[13] = new PaletteTestMetadata("UFO_PAL_BACKGROUND_SAFE", 0, 255, 0, "Palettes/UFO-JASC-SAFE/PAL_BACKGROUND_SAFE.pal", false),
		[14] = new PaletteTestMetadata("TFTD_PAL_BACKGROUND_SAFE", 0, 255, 0, "Palettes/TFTD-JASC-SAFE/PAL_BACKGROUND_SAFE.pal", false),

		[15] = new PaletteTestMetadata("UFO_PAL_GRAPHS", 1, 255, 0, "Palettes/UFO-JASC/PAL_GRAPHS.pal", false),
		[16] = new PaletteTestMetadata("TFTD_PAL_GRAPHS", 1, 255, 0, "Palettes/TFTD-JASC/PAL_GRAPHS.pal", false),
	};

	private readonly Window _window;
	private readonly TextButton _btnRun, _btnCancel;
	private readonly Text _txtPalette;
	private readonly Text _txtTitle, _txtTestCase, _txtDescription;
	private readonly ComboBox _cbxPalette;
	private readonly ComboBox _cbxPaletteAction;
	private readonly ComboBox _cbxTestCase;
	private readonly TextList _lstOutput;
	private readonly List<string> _paletteList = [];
	private readonly SortedDictionary<int, Palette> _vanillaPalettes = [];
	private readonly List<string> _testCases = [];

	/// <summary>C++ std::map::operator[]: the entry, inserting a default one if it is missing.</summary>
	private static PaletteTestMetadata Metadata(int index)
	{
		if (!_paletteMetadataMap.TryGetValue(index, out var data))
		{
			data = new PaletteTestMetadata();
			_paletteMetadataMap[index] = data;
		}
		return data;
	}

	/// <summary>Initializes all the elements in the test screen.</summary>
	public TestState()
	{
		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_txtTitle = new Text(300, 17, 10, 7);
		_txtPalette = new Text(66, 9, 10, 30);
		_cbxPalette = new ComboBox(this, 114, 16, 78, 26);
		_cbxPaletteAction = new ComboBox(this, 114, 16, 198, 26);
		_txtTestCase = new Text(66, 9, 10, 50);
		_cbxTestCase = new ComboBox(this, 234, 16, 78, 46);
		_txtDescription = new Text(300, 25, 10, 66);
		_lstOutput = new TextList(284, 80, 10, 94);
		_btnRun = new TextButton(146, 16, 10, 176);
		_btnCancel = new TextButton(146, 16, 164, 176);

		// Set palette
		SetInterface("tests");

		Add(_window, "window", "tests");
		Add(_txtTitle, "heading", "tests");
		Add(_txtPalette, "text", "tests");
		Add(_txtTestCase, "text", "tests");
		Add(_txtDescription, "heading", "tests");
		Add(_lstOutput, "text", "tests");
		Add(_btnRun, "button2", "tests");
		Add(_btnCancel, "button2", "tests");
		Add(_cbxTestCase, "button1", "tests"); // add as last (display over all other components)
		Add(_cbxPalette, "button1", "tests"); // add as last (display over all other components)
		Add(_cbxPaletteAction, "button1", "tests"); // add as last (display over all other components)

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "tests");

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_TEST_SCREEN"));

		_txtPalette.SetText(Tr("STR_PALETTE"));

		foreach (var pal in _game.Mod.Palettes)
		{
			if (!pal.Key.StartsWith("BACKUP_", StringComparison.Ordinal))
			{
				_paletteList.Add(pal.Key);
			}
		}

		_cbxPalette.SetOptions(_paletteList, true);

		var _actionList = new List<string>
		{
			"STR_PREVIEW",
			"STR_TINY_BORDERLESS",
			"STR_TINY_BORDER",
			"STR_SMALL_LOW_CONTRAST",
			"STR_SMALL_HIGH_CONTRAST",
			"STR_BIG_LOW_CONTRAST",
			"STR_BIG_HIGH_CONTRAST",
		};

		_cbxPaletteAction.SetOptions(_actionList, true);
		_cbxPaletteAction.OnChange(CbxPaletteAction);

		_txtTestCase.SetText(Tr("STR_TEST_CASE"));

		_testCases.Add("STR_BAD_NODES");
		_testCases.Add("STR_MCD_CHECK");
		_testCases.Add("STR_PALETTE_CHECK");
		_testCases.Add("STR_SCRIPT_TAGS");
		_testCases.Add("STR_MAP_RESOURCES");

		_cbxTestCase.SetOptions(_testCases, true);
		_cbxTestCase.OnChange(CbxTestCaseChange);

		_txtDescription.SetWordWrap(true);
		CbxTestCaseChange(null); // update description

		_lstOutput.SetColumns(1, 284);
		_lstOutput.SetBackground(_window);
		_lstOutput.SetWordWrap(true);

		_btnRun.Text = Tr("STR_RUN");
		_btnRun.OnMouseClick(BtnRunClick);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);
	}

	/// <summary>Updates the test case description.</summary>
	public void CbxTestCaseChange(Action action)
	{
		_lstOutput.ClearList();

		int index = _cbxTestCase.Selected;
		_txtDescription.SetText(Tr(_testCases[index] + "_DESC"));
	}

	/// <summary>Runs the selected test case.</summary>
	public void BtnRunClick(Action action)
	{
		_lstOutput.ClearList();

		int index = _cbxTestCase.Selected;
		switch (index)
		{
			case 0: TestCase0(); break;
			case 1: TestCase1(); break;
			case 2: TestCase2(); break;
			case 3: TestCase3(); break;
			case 4: TestCase4(); break;
			default: break;
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Shows palette preview.</summary>
	public void CbxPaletteAction(Action action)
	{
		int index = _cbxPalette.Selected;
		string palette = _paletteList[index];

		var type = (PaletteActionType)_cbxPaletteAction.Selected;

		_game.PushState(new TestPaletteState(palette, type));
	}

	private static void Increment(SortedDictionary<string, int> map, string key)
	{
		map[key] = map.GetValueOrDefault(key) + 1;
	}

	/// <summary>Map resources: terrains, mapblocks and datasets that are missing or unused.</summary>
	private void TestCase4()
	{
		_lstOutput.AddRow(1, Tr("STR_TESTS_STARTING"));

		int total = 0;
		var mod = _game.Mod;

		// build a list of all terrains
		var terrainMap = new SortedDictionary<string, int>(StringComparer.Ordinal);
		foreach (var texturePair in mod.Globe.TexturesRaw)
		{
			foreach (var terrainCrit in texturePair.Value.Terrain)
				Increment(terrainMap, terrainCrit.name);
			foreach (var baseTerrainCrit in texturePair.Value.BaseTerrain)
				Increment(terrainMap, baseTerrainCrit.name);
		}
		foreach (var terrainName in mod.TerrainList)
		{
			Increment(terrainMap, terrainName);
		}
		foreach (var deployName in mod.DeploymentsList)
		{
			AlienDeployment deployRule = mod.GetDeployment(deployName);
			foreach (var terrainName in deployRule.Terrains)
				Increment(terrainMap, terrainName);
		}
		foreach (var mapScript in mod.MapScriptsRaw)
		{
			foreach (var mapScriptCommand in mapScript.Value)
			{
				foreach (var terrainName in mapScriptCommand.RandomAlternateTerrain)
				{
					Increment(terrainMap, terrainName);
				}
			}
		}

		// erase false positives
		terrainMap.Remove("");
		terrainMap.Remove("baseTerrain");
		terrainMap.Remove("globeTerrain");

		// 1. check terrain existence in ruleset
		Logger.Info("----------------------------------------------1. check terrain existence in ruleset");
		foreach (var pair in terrainMap)
		{
			RuleTerrain tRule = mod.GetTerrain(pair.Key);
			if (tRule == null)
			{
				++total;
				Logger.Info($"Terrain '{pair.Key}' does not exist!");
			}
		}

		// build a list of all mapblocks and mapdatasets
		var blockMap = new SortedDictionary<string, int>(StringComparer.Ordinal);
		var datasetMap = new SortedDictionary<string, int>(StringComparer.Ordinal);

		static void addMapblockAndDataset(RuleTerrain terrainRule, SortedDictionary<string, int> blockMapRef, SortedDictionary<string, int> datasetMapRef, int maxSkinIndex = 0)
		{
			if (terrainRule != null)
			{
				foreach (var mapblock in terrainRule.MapBlocks)
				{
					string uc = Unicode.UpperCase(mapblock.Name);
					Increment(blockMapRef, uc);
				}
				for (int skinIndex = 0; skinIndex <= maxSkinIndex; ++skinIndex)
				{
					foreach (var dataset in terrainRule.MapDataSets)
					{
						string uc = Unicode.UpperCase(dataset.Name);
						if (skinIndex > 0 && uc != "BLANKS")
						{
							uc = uc + "_" + skinIndex.ToString(CultureInfo.InvariantCulture);
						}
						Increment(datasetMapRef, uc);
					}
				}
			}
		}

		foreach (var pair in terrainMap)
		{
			RuleTerrain terrainRule = mod.GetTerrain(pair.Key);
			addMapblockAndDataset(terrainRule, blockMap, datasetMap);
		}
		foreach (var ufoName in mod.UfosList)
		{
			RuleUfo ufoRule = mod.GetUfo(ufoName);
			RuleTerrain terrainRule = ufoRule.BattlescapeTerrainData;
			addMapblockAndDataset(terrainRule, blockMap, datasetMap);
		}
		foreach (var craftName in mod.CraftsList)
		{
			RuleCraft craftRule = mod.GetCraft(craftName);
			RuleTerrain terrainRule = craftRule.BattlescapeTerrainData;
			addMapblockAndDataset(terrainRule, blockMap, datasetMap, craftRule.MaxSkinIndex);
		}

		// 2. check for existence of mapblock MAP and RMP files
		Logger.Info("----------------------------------------------2. check for existence of mapblock MAP and RMP files");

		static void checkExistence(SortedDictionary<string, int> mapRef, string dir, string ext, ref int totalRef)
		{
			foreach (var mapItem in mapRef)
			{
				string filename = dir + mapItem.Key + ext;
				if (!FileMap.FileExists(filename))
				{
					++totalRef;
					Logger.Info($"{filename} not found");
				}
			}
		}

		checkExistence(blockMap, "MAPS/", ".MAP", ref total);
		checkExistence(blockMap, "ROUTES/", ".RMP", ref total);

		// 3. check for existence of mapdataset MCD, PCK and TAB files
		Logger.Info("----------------------------------------------3. check for existence of mapdataset MCD, PCK and TAB files");

		checkExistence(datasetMap, "TERRAIN/", ".MCD", ref total);
		checkExistence(datasetMap, "TERRAIN/", ".PCK", ref total);
		checkExistence(datasetMap, "TERRAIN/", ".TAB", ref total);

		// 4. check for unused mapblock MAP and RMP files
		Logger.Info("----------------------------------------------4. check for unused mapblock MAP and RMP files");

		static void findUnusedFiles(SortedDictionary<string, int> mapRef, string dir, ref int totalRef)
		{
			var contents = FileMap.GetVFolderContents(dir);
			foreach (var name in contents)
			{
				string upper = Unicode.UpperCase(name); // copy
				string noExt = CrossPlatform.NoExt(upper);
				if (!mapRef.ContainsKey(noExt))
				{
					string fullpath = FileMap.At(dir + upper).FullPath;

					// exceptions
					if (fullpath.Contains("/standard/xcom1/", StringComparison.Ordinal)) continue;
					if (fullpath.Contains("/UFO/MAPS/", StringComparison.Ordinal)) continue;
					if (fullpath.Contains("/UFO/ROUTES/", StringComparison.Ordinal)) continue;
					if (fullpath.Contains("/TFTD/MAPS/", StringComparison.Ordinal)) continue;
					if (fullpath.Contains("/TFTD/ROUTES/", StringComparison.Ordinal)) continue;
					if (fullpath.Contains("/TFTD/TERRAIN/", StringComparison.Ordinal)) continue;

					++totalRef;
					Logger.Info($"{fullpath} not used anywhere.");
				}
			}
		}

		findUnusedFiles(blockMap, "MAPS/", ref total);
		findUnusedFiles(blockMap, "ROUTES/", ref total);

		// 5. check for unused mapdataset MCD, PCK and TAB files
		Logger.Info("----------------------------------------------5. check for unused mapdataset MCD, PCK and TAB files");

		findUnusedFiles(datasetMap, "TERRAIN/", ref total);

		AddResult(total, true);
	}

	/// <summary>Adds the result rows ("errors found"/"no errors") and optionally the "finished" row.</summary>
	private void AddResult(int total, bool finished)
	{
		if (total > 0)
		{
			_lstOutput.AddRow(1, Tr("STR_TESTS_ERRORS_FOUND").Arg(total));
			_lstOutput.AddRow(1, Tr("STR_DETAILED_INFO_IN_LOG_FILE"));
		}
		else
		{
			_lstOutput.AddRow(1, Tr("STR_TESTS_NO_ERRORS_FOUND"));
		}
		if (finished)
		{
			_lstOutput.AddRow(1, Tr("STR_TESTS_FINISHED"));
		}
	}

	/// <summary>Script tags: exports the armor script tags as CSV to the log.</summary>
	private void TestCase3()
	{
		_lstOutput.AddRow(1, Tr("STR_TESTS_STARTING"));

		// missing: script hooks – the armor tag values (getScriptValuesRaw) and tag names are not ported, so there are no tag columns
		var tagNames = new SortedDictionary<string, bool>(StringComparer.Ordinal);

		Logger.Info("----------------------------------------------");
		Logger.Info("Copy/paste into a CSV file");
		Logger.Info("----------------------------------------------");
		{
			var ssNames = new StringBuilder(";Armor");
			foreach (var n in tagNames)
			{
				ssNames.Append(';').Append(n.Key);
			}
			Logger.Info(ssNames.ToString());
		}
		foreach (var armorName in _game.Mod.ArmorsList)
		{
			_ = _game.Mod.GetArmor(armorName, true);
			var ss = new StringBuilder(";").Append(armorName);
			foreach (var t in tagNames)
			{
				ss.Append(';');
			}
			Logger.Info(ss.ToString());
		}
		Logger.Info("----------------------------------------------");
		Logger.Info("End of export");
		Logger.Info("----------------------------------------------");

		_lstOutput.AddRow(1, Tr("STR_TESTS_FINISHED"));
	}

	/// <summary>
	/// Reads "r g b" like <c>ss &gt;&gt; r; ss &gt;&gt; g; ss &gt;&gt; b;</c>: the first failed read stores 0,
	/// the reads after it leave their value unchanged.
	/// </summary>
	internal static void ReadRgb(string line, ref int r, ref int g, ref int b)
	{
		var tokens = (line ?? "").Split((char[])[' ', '\t', '\r', '\n', '\v', '\f'], StringSplitOptions.RemoveEmptyEntries);
		bool failed = false;
		r = ReadInt(tokens, 0, r, ref failed);
		g = ReadInt(tokens, 1, g, ref failed);
		b = ReadInt(tokens, 2, b, ref failed);
	}

	private static int ReadInt(string[] tokens, int index, int value, ref bool failed)
	{
		if (failed)
			return value;
		if (index < tokens.Length)
		{
			string t = tokens[index];
			int end = 0;
			if (end < t.Length && (t[end] == '+' || t[end] == '-'))
				end++;
			int digits = end;
			while (end < t.Length && char.IsAsciiDigit(t[end]))
				end++;
			if (end > digits)
			{
				if (int.TryParse(t.AsSpan(0, end), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int v))
					return v;
				failed = true;
				return t[0] == '-' ? int.MinValue : int.MaxValue;
			}
		}
		failed = true;
		return 0;
	}

	/// <summary>Palette check: compares the palettes of all extra sprites with the vanilla palettes.</summary>
	private void TestCase2()
	{
		_lstOutput.AddRow(1, Tr("STR_TESTS_STARTING"));
		if (_vanillaPalettes.Count == 0)
		{
			foreach (var item in _paletteMetadataMap)
			{
				_vanillaPalettes[item.Key] = new Palette();
				_vanillaPalettes[item.Key].InitBlack();

				// Load from JASC file
				if (FileMap.FileExists(item.Value.palettePath))
				{
					using var palFile = new StreamReader(FileMap.GetStream(item.Value.palettePath));
					palFile.ReadLine(); // header
					palFile.ReadLine(); // file format
					palFile.ReadLine(); // number of colors
					int r = 0, g = 0, b = 0;
					for (int j = 0; j < 256; ++j)
					{
						string line = palFile.ReadLine(); // j-th color index
						ReadRgb(line, ref r, ref g, ref b);
						_vanillaPalettes[item.Key].CopyColor(j, r, g, b); // raw RGB copy, no side effects!
					}
				}
				else
				{
					throw new InvalidOperationException(item.Value.palettePath + " not found");
				}
			}
		}

		int total = 0;
		foreach (var i in _game.Mod.ExtraSprites)
		{
			string sheetName = i.Key;
			if (sheetName.Contains("_CPAL", StringComparison.Ordinal))
			{
				// custom palettes cannot be matched, skip
				continue;
			}

			foreach (var spritePack in i.Value)
			{
				if (spritePack.SingleImage)
				{
					string relPath = SpriteAt(spritePack, 0);
					total += CheckPalette(relPath, spritePack.Width, spritePack.Height);
				}
				else
				{
					foreach (var j in spritePack.Sprites.ToList())
					{
						int startFrame = j.Key;
						string fileName = j.Value;
						if (fileName.EndsWith('/'))
						{
							var contents = FileMap.GetVFolderContents(fileName);
							foreach (var name in contents)
							{
								if (!ExtraSprites.IsImageFile(name))
									continue;
								try
								{
									string relPath = fileName + name;
									total += CheckPalette(relPath, spritePack.Width, spritePack.Height);
								}
								catch (Exception e) when (e is InvalidDataException or InvalidOperationException or IOException)
								{
									Logger.Warning(e.Message);
								}
							}
						}
						else
						{
							if (spritePack.SubX == 0 && spritePack.SubY == 0)
							{
								total += CheckPalette(fileName, spritePack.Width, spritePack.Height);
							}
							else
							{
								string relPath = SpriteAt(spritePack, startFrame);
								total += CheckPalette(relPath, spritePack.Width, spritePack.Height);
							}
						}
					}
				}
			}
		}

		AddResult(total, true);
	}

	/// <summary>C++ (*spritePack->getSprites())[index]: inserts an empty entry if it is missing.</summary>
	private static string SpriteAt(ExtraSprites spritePack, int index)
	{
		if (!spritePack.Sprites.TryGetValue(index, out string path))
		{
			path = "";
			spritePack.Sprites[index] = path;
		}
		return path;
	}

	/// <summary>Checks an image against the vanilla palettes; 1 if none matches completely.</summary>
	private int CheckPalette(string fullPath, int width, int height)
	{
		var image = new Surface(width, height);
		image.LoadImage(fullPath);

		// the loaded surface always has a 256 colour palette (as in C++, where the image is copied into an 8 bit surface),
		// so the "no palette" and "not 256 colors" errors of the original cannot happen

		int bestMatch = 0;
		int matchedPaletteIndex = 0;
		foreach (var item in _vanillaPalettes)
		{
			int match = MatchPalette(image, item.Key, item.Value);
			if (match > bestMatch)
			{
				bestMatch = match;
				matchedPaletteIndex = item.Key;
			}
			if (match == 100)
			{
				break;
			}
		}

		if (bestMatch < 100)
		{
			Logger.Info($"Best match: {bestMatch}%; palette: {Metadata(matchedPaletteIndex).paletteName}; path: {fullPath}");
			return 1;
		}

		return 0;
	}

	/// <summary>Percentage of the checked colours of the image palette that match a vanilla palette.</summary>
	internal static int MatchPalette(Surface image, int index, Palette test)
	{
		var colors = image.Palette;
		int matched = 0;

		var data = Metadata(index);
		int firstIndexToCheck = data.firstIndexToCheck;
		int lastIndexToCheck = data.lastIndexToCheck;
		int maxTolerance = data.maxTolerance;
		bool usesBackPals = data.usesBackPals;

		for (int i = firstIndexToCheck; i <= lastIndexToCheck; i++)
		{
			if (usesBackPals)
			{
				if (i >= 224 && i <= 239)
				{
					// don't check and consider matched
					matched++;
					continue;
				}
			}

			var t = test.GetColors(i)[0];
			byte rdiff = (byte)(colors[i].R > t.R ? colors[i].R - t.R : t.R - colors[i].R);
			byte gdiff = (byte)(colors[i].G > t.G ? colors[i].G - t.G : t.G - colors[i].G);
			byte bdiff = (byte)(colors[i].B > t.B ? colors[i].B - t.B : t.B - colors[i].B);

			if (rdiff <= maxTolerance && gdiff <= maxTolerance && bdiff <= maxTolerance)
			{
				matched++;
			}
		}

		return matched * 100 / (lastIndexToCheck - firstIndexToCheck + 1);
	}

	/// <summary>MCD check: armor, LOFTs, MCD references and movement costs of all map data.</summary>
	private void TestCase1()
	{
		_lstOutput.AddRow(1, Tr("STR_TESTS_STARTING"));
		_lstOutput.AddRow(1, Tr("STR_CHECKING_TERRAIN"));
		int total = 0;
		var mod = _game.Mod;
		var uniqueResults = new SortedDictionary<string, SortedSet<int>>(StringComparer.Ordinal);
		foreach (var terrainName in mod.TerrainList)
		{
			RuleTerrain terrainRule = mod.GetTerrain(terrainName);
			total += CheckMCD(terrainRule, uniqueResults);
		}
		_lstOutput.AddRow(1, Tr("STR_CHECKING_UFOS"));
		foreach (var ufoName in mod.UfosList)
		{
			RuleUfo ufoRule = mod.GetUfo(ufoName);
			RuleTerrain terrainRule = ufoRule.BattlescapeTerrainData;
			if (terrainRule == null)
			{
				continue;
			}
			total += CheckMCD(terrainRule, uniqueResults);
		}
		_lstOutput.AddRow(1, Tr("STR_CHECKING_CRAFT"));
		foreach (var craftName in mod.CraftsList)
		{
			RuleCraft craftRule = mod.GetCraft(craftName);
			RuleTerrain terrainRule = craftRule.BattlescapeTerrainData;
			if (terrainRule == null)
			{
				continue;
			}
			for (int skinIndex = 0; skinIndex <= craftRule.MaxSkinIndex; ++skinIndex)
			{
				terrainRule.RefreshMapDataSets(skinIndex, mod); // change skin
				total += CheckMCD(terrainRule, uniqueResults);
			}
		}
		AddResult(total, false);

		// summary (unique)
		if (total > 0)
		{
			Logger.Info("----------");
			Logger.Info("SUMMARY");
			Logger.Info("----------");
			foreach (var mapItem in uniqueResults)
			{
				string line = mapItem.Key + ": " + string.Join(", ", mapItem.Value.Select(v => v.ToString(CultureInfo.InvariantCulture)));
				Logger.Info(line);
			}
		}
		_lstOutput.AddRow(1, Tr("STR_TESTS_FINISHED"));
	}

	private static void AddUnique(SortedDictionary<string, SortedSet<int>> uniqueResults, string name, int index)
	{
		if (!uniqueResults.TryGetValue(name, out var set))
		{
			set = [];
			uniqueResults[name] = set;
		}
		set.Add(index);
	}

	private int CheckMCD(RuleTerrain terrainRule, SortedDictionary<string, SortedSet<int>> uniqueResults)
	{
		int MAX_LOFT_ID = (_game.Mod.VoxelData.Count / 16) - 1;
		int errors = 0;
		foreach (var myMapDataSet in terrainRule.MapDataSets)
		{
			int index = 0;
			myMapDataSet.LoadData(_game.Mod.GetMCDPatch(myMapDataSet.Name), false);
			int size = myMapDataSet.ObjectsRaw.Count;
			foreach (var myMapData in myMapDataSet.ObjectsRaw)
			{
				string where = $"terrain: {terrainRule.Name} dataset: {myMapDataSet.Name} object {index}";
				// Check for 0 armor
				if (myMapData.Armor == 0)
				{
					Logger.Error($"Error in {where} has armor: {myMapData.Armor}. Found using OXCE test cases.");
					errors++;
					AddUnique(uniqueResults, myMapDataSet.Name, index);
				}

				// Check for invalid LOFTemps
				for (int layer = 0; layer < 12; ++layer)
				{
					if (myMapData.GetLoftID(layer) < 0 || myMapData.GetLoftID(layer) > MAX_LOFT_ID)
					{
						Logger.Error($"Error in {where} has LoftID: {myMapData.GetLoftID(layer)}. MAX_LOFT_ID = {MAX_LOFT_ID}. Found using OXCE test cases.");
						errors++;
						AddUnique(uniqueResults, myMapDataSet.Name, index);
					}
				}

				// Validate MCD references
				if (myMapData.DieMCD >= size)
				{
					Logger.Error($"Error in {where} has invalid DieMCD: {myMapData.DieMCD}. Found using OXCE test cases.");
					errors++;
					AddUnique(uniqueResults, myMapDataSet.Name, index);
				}
				if (myMapData.AltMCD >= size)
				{
					Logger.Error($"Error in {where} has invalid AltMCD: {myMapData.AltMCD}. Found using OXCE test cases.");
					errors++;
					AddUnique(uniqueResults, myMapDataSet.Name, index);
				}

				if (myMapData.ObjectType == TilePart.O_FLOOR)
				{
					if (myMapData.IsNoFloor && myMapData.GetTUCost(MovementType.MT_WALK) > 0 && myMapData.GetTUCost(MovementType.MT_WALK) < 200 && myMapDataSet.Name != "BLANKS")
					{
						// Note: pathfinding and autoshots don't work on such ground tiles
						// (such objects were intended to be used only as holes on upper floors/roofs, they should NOT be used on ground floor)
						Logger.Warning($"Potential issue in {where} is a walkable floor tile with 'No_Floor' flag set to true.. Found using OXCE test cases.");
						errors++;
						AddUnique(uniqueResults, myMapDataSet.Name, index);
					}
					string at = $" terrain:{terrainRule.Name} dataset:{myMapDataSet.Name} index:{index}";
					if (myMapData.GetTUCost(MovementType.MT_WALK) < 1)
					{
						Logger.Info($"Zero movement cost on floor object:  walk:{myMapData.GetTUCost(MovementType.MT_WALK)}{at}. Found using OXCE test cases.");
						errors++;
						AddUnique(uniqueResults, myMapDataSet.Name, index);
					}
					if (myMapData.GetTUCost(MovementType.MT_FLY) < 1)
					{
						Logger.Info($"Zero movement cost on floor object:   fly:{myMapData.GetTUCost(MovementType.MT_FLY)}{at}. Found using OXCE test cases.");
						errors++;
						AddUnique(uniqueResults, myMapDataSet.Name, index);
					}
					if (myMapData.GetTUCost(MovementType.MT_SLIDE) < 1)
					{
						Logger.Info($"Zero movement cost on floor object: slide:{myMapData.GetTUCost(MovementType.MT_SLIDE)}{at}. Found using OXCE test cases.");
						errors++;
						AddUnique(uniqueResults, myMapDataSet.Name, index);
					}
				}
				index++;
			}
			//myMapDataSet->unloadData();
		}
		return errors;
	}

	/// <summary>Bad nodes: route nodes outside their map block.</summary>
	private void TestCase0()
	{
		_lstOutput.AddRow(1, Tr("STR_TESTS_STARTING"));
		_lstOutput.AddRow(1, Tr("STR_CHECKING_TERRAIN"));
		int total = 0;
		var mod = _game.Mod;
		foreach (var terrainName in mod.TerrainList)
		{
			RuleTerrain terrRule = mod.GetTerrain(terrainName);
			foreach (var mapblock in terrRule.MapBlocks)
			{
				total += CheckRMP(mapblock);
			}
		}
		_lstOutput.AddRow(1, Tr("STR_CHECKING_UFOS"));
		foreach (var ufoName in mod.UfosList)
		{
			RuleUfo ufoRule = mod.GetUfo(ufoName);
			if (ufoRule.BattlescapeTerrainData != null)
			{
				foreach (var mapblock in ufoRule.BattlescapeTerrainData.MapBlocks)
				{
					total += CheckRMP(mapblock);
				}
			}
		}
		_lstOutput.AddRow(1, Tr("STR_CHECKING_CRAFT"));
		foreach (var craftName in mod.CraftsList)
		{
			RuleCraft craftRule = mod.GetCraft(craftName);
			if (craftRule.BattlescapeTerrainData != null)
			{
				foreach (var mapblock in craftRule.BattlescapeTerrainData.MapBlocks)
				{
					total += CheckRMP(mapblock);
				}
			}
		}
		AddResult(total, true);
	}

	private static int CheckRMP(MapBlock mapblock)
	{
		// Z-dimension is not in the ruleset, it's hacked on-the-fly
		int mapblockSizeZ = LoadMAP(mapblock);

		string filename = "ROUTES/" + mapblock.Name + ".RMP";

		// Load file
		if (!FileMap.FileExists(filename))
		{
			throw new InvalidOperationException(filename + " not found");
		}
		byte[] data = FileMap.ReadAll(filename);

		int nodesAdded = 0;
		int errors = 0;
		// complete 24 byte records; a trailing partial record ends the loop at end of file, as in C++
		for (int offset = 0; offset + 24 <= data.Length; offset += 24)
		{
			int pos_x = data[offset + 1];
			int pos_y = data[offset + 0];
			int pos_z = data[offset + 2];
			if (pos_x >= 0 && pos_x < mapblock.SizeX &&
				pos_y >= 0 && pos_y < mapblock.SizeY &&
				pos_z >= 0 && pos_z < mapblockSizeZ) // using "mapblockSizeZ" instead of "mapblock->getSizeZ()"!!
			{
				// all ok
			}
			else
			{
				Logger.Info($"Bad node in RMP file: {filename} Node #{nodesAdded} is outside map boundaries at X:{pos_x} Y:{pos_y} Z:{pos_z}. Found using OXCE test cases.");
				errors++;
			}
			nodesAdded++;
		}

		return errors;
	}

	private static int LoadMAP(MapBlock mapblock)
	{
		string filename = "MAPS/" + mapblock.Name + ".MAP";

		// Load file
		if (!FileMap.FileExists(filename))
		{
			throw new InvalidOperationException(filename + " not found");
		}
		byte[] size = FileMap.ReadAll(filename);

		//sizey = (int)size[0];
		//sizex = (int)size[1];
		int sizez = size.Length > 2 ? (sbyte)size[2] : 0; // char is signed

		//mapblock->setSizeZ(sizez); // commented out, for testing purposes we don't need to HACK it like in real code

		return sizez;
	}
}
