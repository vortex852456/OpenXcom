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
// Ported from: src/Menu/OptionsFoldersState.h, src/Menu/OptionsFoldersState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Screen that shows the folders the game uses (data, user, saves, config).</summary>
public class OptionsFoldersState : OptionsBaseState
{
	private readonly Text _txtDataFolder, _txtUserFolder, _txtSaveFolder, _txtConfigFolder;
	private readonly Text _txtDataFolderPath1, _txtDataFolderPath2, _txtUserFolderPath, _txtSaveFolderPath, _txtConfigFolderPath;

	/// <summary>Initializes all the elements in the Folders Options screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsFoldersState(OptionsOrigin origin) : base(origin)
	{
		SetCategory(_btnFolders);

		// Create object
		_txtDataFolder = new Text(218, 9, 94, 8);
		_txtUserFolder = new Text(218, 9, 94, 59);
		_txtSaveFolder = new Text(218, 9, 94, 86);
		_txtConfigFolder = new Text(218, 9, 94, 121);

		_txtDataFolderPath1 = new Text(218, 17, 94, 18);
		_txtDataFolderPath2 = new Text(218, 17, 94, 38);
		_txtUserFolderPath = new Text(218, 17, 94, 69);
		_txtSaveFolderPath = new Text(218, 25, 94, 96);
		_txtConfigFolderPath = new Text(218, 17, 94, 131);

		Add(_txtDataFolder, "text1", "foldersMenu");
		Add(_txtUserFolder, "text1", "foldersMenu");
		Add(_txtSaveFolder, "text1", "foldersMenu");
		Add(_txtConfigFolder, "text1", "foldersMenu");

		Add(_txtDataFolderPath1, "text2", "foldersMenu");
		Add(_txtDataFolderPath2, "text2", "foldersMenu");
		Add(_txtUserFolderPath, "text2", "foldersMenu");
		Add(_txtSaveFolderPath, "text2", "foldersMenu");
		Add(_txtConfigFolderPath, "text2", "foldersMenu");

		CenterAllSurfaces();

		// Set up object
		_txtDataFolder.SetText(Tr("STR_DATA_FOLDER"));
		_txtUserFolder.SetText(Tr("STR_USER_FOLDER"));
		_txtSaveFolder.SetText(Tr("STR_SAVE_FOLDER"));
		_txtConfigFolder.SetText(Tr("STR_CONFIG_FOLDER"));

		const string file1 = "GEODATA/BACKPALS.DAT";
		const string file2 = "openxcom.png";

		string origDataFolder = "<error1>";
		if (FileMap.FileExists(file1))
		{
			origDataFolder = DataFolderOf(FileMap.At(file1).FullPath, file1);
		}
		string oxceDataFolder = "<error2>";
		if (FileMap.FileExists(file2))
		{
			oxceDataFolder = DataFolderOf(FileMap.At(file2).FullPath, file2);
		}

		SetUpPath(_txtDataFolderPath1, origDataFolder, "STR_DATA_FOLDER_DESC_1");
		SetUpPath(_txtDataFolderPath2, oxceDataFolder, "STR_DATA_FOLDER_DESC_2");
		SetUpPath(_txtUserFolderPath, Options.GetUserFolder(), "STR_USER_FOLDER_DESC");
		SetUpPath(_txtSaveFolderPath, Options.GetMasterUserFolder(), "STR_SAVE_FOLDER_DESC");
		SetUpPath(_txtConfigFolderPath, Options.GetConfigFolder(), "STR_CONFIG_FOLDER_DESC");
	}

	/// <summary>The path up to the relative file name (C++ substr(0, find(file)); the whole path if the name is not found).</summary>
	internal static string DataFolderOf(string fullpath, string file)
	{
		int pos = fullpath.IndexOf(file, StringComparison.Ordinal);
		return pos < 0 ? fullpath : fullpath[..pos];
	}

	private void SetUpPath(Text text, string path, string tooltip)
	{
		text.SetText(path);
		text.SetWordWrap(true);
		text.Tooltip = tooltip;
		text.OnMouseIn(TxtTooltipIn);
		text.OnMouseOut(TxtTooltipOut);
		text.OnMouseClick(TxtClick);
	}

	/// <summary>Opens the location of the appropriate folder.</summary>
	public void TxtClick(Action action)
	{
		var sender = (Text)action.Sender;
		string path = sender.GetText();
		if (Options.GetDataFolder().Length == 0 && path.Length < 10)
		{
			// "UFO/", "TFTD/", "common/", "standard/"
			path = CrossPlatform.GetExeFolder() + path;
		}
		CrossPlatform.OpenExplorer(path);
	}
}
