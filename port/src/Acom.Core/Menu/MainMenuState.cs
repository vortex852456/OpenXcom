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
// Ported from: src/Menu/MainMenuState.h, src/Menu/MainMenuState.cpp
// Port status: partial (missing: Screen::resetDisplay; the Windows-only update check and self-updater are left out on purpose, see oxceUpdateCheck in docs/architecture/csharp-port.md)
// Deviation: the title names ACOM and the OXCE version it is based on.
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Helper state that delays creation of the main menu until the state is first initialized.
/// </summary>
public class GoToMainMenuState : State
{
	private readonly bool _updateCheck;

	public GoToMainMenuState(bool updateCheck = false)
	{
		_updateCheck = updateCheck;
	}

	public override void Init()
	{
		Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, true);
		// missing: _game->getScreen()->resetDisplay(false);
		_game.SetState(new MainMenuState(_updateCheck));
	}
}

/// <summary>
/// Main Menu window displayed when first starting the game.
/// </summary>
public class MainMenuState : State
{
	private readonly TextButton _btnNewGame, _btnNewBattle, _btnLoad, _btnOptions, _btnMods, _btnQuit, _btnUpdate;
	private readonly Window _window;
	private readonly Text _txtUpdateInfo, _txtTitle;

	/// <summary>
	/// Initializes all the elements in the Main Menu window.
	/// </summary>
	/// <param name="updateCheck">Perform update check?</param>
#pragma warning disable IDE0060 // updateCheck is only used by the Windows update check, which is not ported
	public MainMenuState(bool updateCheck = false)
#pragma warning restore IDE0060
	{
		// Create objects
		_window = new Window(this, 256, 160, 32, 20, WindowPopup.POPUP_BOTH);
		_btnNewGame = new TextButton(92, 20, 64, 90);
		_btnNewBattle = new TextButton(92, 20, 164, 90);
		_btnLoad = new TextButton(92, 20, 64, 118);
		_btnOptions = new TextButton(92, 20, 164, 118);
		_btnMods = new TextButton(92, 20, 64, 146);
		_btnQuit = new TextButton(92, 20, 164, 146);
		_btnUpdate = new TextButton(72, 16, 209, 27);
		_txtUpdateInfo = new Text(320, 17, 0, 11);
		_txtTitle = new Text(256, 30, 32, 45);

		// Set palette
		SetInterface("mainMenu");

		Add(_window, "window", "mainMenu");
		Add(_btnNewGame, "button", "mainMenu");
		Add(_btnNewBattle, "button", "mainMenu");
		Add(_btnLoad, "button", "mainMenu");
		Add(_btnOptions, "button", "mainMenu");
		Add(_btnMods, "button", "mainMenu");
		Add(_btnQuit, "button", "mainMenu");
		Add(_btnUpdate, "button", "mainMenu");
		Add(_txtUpdateInfo, "text", "mainMenu");
		Add(_txtTitle, "text", "mainMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "mainMenu");

		_btnNewGame.Text = Tr("STR_NEW_GAME");
		_btnNewGame.OnMouseClick(BtnNewGameClick);

		_btnNewBattle.Text = Tr("STR_NEW_BATTLE");
		_btnNewBattle.OnMouseClick(BtnNewBattleClick);

		_btnLoad.Text = Tr("STR_LOAD_SAVED_GAME");
		_btnLoad.OnMouseClick(BtnLoadClick);

		_btnOptions.Text = Tr("STR_OPTIONS");
		_btnOptions.OnMouseClick(BtnOptionsClick);

		_btnMods.Text = Tr("STR_MODS");
		_btnMods.OnMouseClick(BtnModsClick);

		_btnQuit.Text = Tr("STR_QUIT");
		_btnQuit.OnMouseClick(BtnQuitClick);

		_btnUpdate.Text = Tr("STR_UPDATE");
		_btnUpdate.OnMouseClick(BtnUpdateClick);
		_btnUpdate.Visible = false;

		_txtUpdateInfo.Align = TextHAlign.ALIGN_CENTER;
		_txtUpdateInfo.SetWordWrap(true);
		_txtUpdateInfo.SetText(Tr("STR_LATEST_VERSION_INFO"));
		_txtUpdateInfo.Visible = false;

		// missing on purpose: the Windows-only update check (oxceUpdateCheck)

		if (!_btnUpdate.Visible)
		{
			int problemCode = 0;
			// standard
			ModInfo xcomInfo = Options.GetXcomRulesetInfo();
			if (xcomInfo != null && CrossPlatform.IsLowerThanRequiredVersion(xcomInfo.Version))
			{
				problemCode = 1;
				Logger.Error("ERROR: The content of the 'standard' folder is too old!");
				_txtUpdateInfo.SetText("ERROR: The content of the 'standard' folder is too old!");
			}
			// common
			try
			{
				YamlRootNodeReader reader = FileMap.GetYaml("dont-touch.me");
				if (reader["version"])
				{
					if (CrossPlatform.IsLowerThanRequiredVersion(reader["version"].ReadVal<string>()))
					{
						problemCode = 2;
						Logger.Error("ERROR: The content of the 'common' folder is too old!");
						_txtUpdateInfo.SetText("ERROR: The content of the 'common' folder is too old!");
					}
				}
			}
			catch (Exception e) when (e is FileNotFoundException or YamlReadException or InvalidOperationException)
			{
				problemCode = 3;
				Logger.Error("ERROR: The content of the 'common' folder is too old!!");
				_txtUpdateInfo.SetText("ERROR: The content of the 'common' folder is too old!!");
				Logger.Error(e.Message);
			}
			// result
			if (problemCode > 0)
			{
				_txtUpdateInfo.SetColor(81); // hardcoded, readable both in xcom1 and xcom2
				_txtUpdateInfo.Visible = true;
			}
		}

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		string title = Tr("STR_OPENXCOM").Arg(Options.GetActiveMasterInfo().VersionDisplay) + Unicode.TOK_NL_SMALL
			+ $"ACOM {Version.ACOM_VERSION} (OpenXcom {Version.OPENXCOM_VERSION_SHORT})";
		_txtTitle.SetText(title);
	}

	public override void Init()
	{
		base.Init();
		if (Options.GetLoadLastSave() && !string.IsNullOrEmpty(Options.GetLoadThisSave()))
		{
			Logger.Info("Loading saved game passed as parameter");
			BtnLoadClick(null);
		}
		else if (Options.GetLoadLastSave() && SavedGame.GetList(_game.Language, true).Count > 0)
		{
			Logger.Info("Loading last saved game");
			BtnLoadClick(null);
		}
	}

	/// <summary>
	/// Opens the New Game window.
	/// </summary>
	public void BtnNewGameClick(Action action)
	{
		_game.PushState(new NewGameState());
	}

	/// <summary>
	/// Opens the New Battle screen.
	/// </summary>
	public void BtnNewBattleClick(Action action)
	{
		_game.PushState(new NewBattleState());
	}

	/// <summary>
	/// Opens the Load Game screen.
	/// </summary>
	public void BtnLoadClick(Action action)
	{
		_game.PushState(new ListLoadState(OptionsOrigin.OPT_MENU));
	}

	/// <summary>
	/// Opens the Options screen.
	/// </summary>
	public void BtnOptionsClick(Action action)
	{
		Options.BackupDisplay();
		_game.PushState(new OptionsVideoState(OptionsOrigin.OPT_MENU));
	}

	/// <summary>
	/// Opens the Mods screen.
	/// </summary>
	public void BtnModsClick(Action action)
	{
		_game.PushState(new ModListState());
	}

	/// <summary>
	/// Quits the game.
	/// </summary>
	public void BtnQuitClick(Action action)
	{
		_game.Quit();
	}

	/// <summary>
	/// Updates OpenXcom to the newest version (Windows only in C++, not ported: the button is never shown).
	/// </summary>
	public void BtnUpdateClick(Action action)
	{
	}

	/// <summary>
	/// Updates the scale.
	/// </summary>
	/// <param name="dX">delta of X;</param>
	/// <param name="dY">delta of Y;</param>
	public override void Resize(ref int dX, ref int dY)
	{
		dX = Options.baseXResolution;
		dY = Options.baseYResolution;
		Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, true);
		dX = Options.baseXResolution - dX;
		dY = Options.baseYResolution - dY;
		base.Resize(ref dX, ref dY);
	}
}
