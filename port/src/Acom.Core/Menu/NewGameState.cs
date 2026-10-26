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
// Ported from: src/Menu/NewGameState.h, src/Menu/NewGameState.cpp
#nullable disable

using System.Runtime.CompilerServices;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window shown when the player starts a new game.
/// Lets the player pick the difficulty level.
/// </summary>
public class NewGameState : State
{
	private readonly Window _window;
	private readonly TextButton _btnBeginner, _btnExperienced, _btnVeteran, _btnGenius, _btnSuperhuman;
	private readonly ToggleTextButton _btnIronman;
	private readonly TextButton _btnOk, _btnCancel;
	private readonly Text _txtTitle, _txtIronman;
	private readonly StrongBox<TextButton> _difficulty = new();

	/// <summary>
	/// Initializes all the elements in the Difficulty window.
	/// </summary>
	public NewGameState()
	{
		// Create objects
		_window = new Window(this, 192, 180, 64, 10, WindowPopup.POPUP_VERTICAL);
		_btnBeginner = new TextButton(160, 18, 80, 32);
		_btnExperienced = new TextButton(160, 18, 80, 52);
		_btnVeteran = new TextButton(160, 18, 80, 72);
		_btnGenius = new TextButton(160, 18, 80, 92);
		_btnSuperhuman = new TextButton(160, 18, 80, 112);
		_btnIronman = new ToggleTextButton(78, 18, 80, 138);
		_btnOk = new TextButton(78, 16, 80, 164);
		_btnCancel = new TextButton(78, 16, 162, 164);
		_txtTitle = new Text(192, 9, 64, 20);
		_txtIronman = new Text(90, 24, 162, 135);

		switch (_game.Mod.StartingDifficulty)
		{
			case 0:
				_difficulty.Value = _btnBeginner;
				break;
			case 1:
				_difficulty.Value = _btnExperienced;
				break;
			case 2:
				_difficulty.Value = _btnVeteran;
				break;
			case 3:
				_difficulty.Value = _btnGenius;
				break;
			case 4:
				_difficulty.Value = _btnSuperhuman;
				break;
			default:
				_difficulty.Value = _btnBeginner;
				break;
		}

		// Set palette
		SetInterface("newGameMenu");

		Add(_window, "window", "newGameMenu");
		Add(_btnBeginner, "button", "newGameMenu");
		Add(_btnExperienced, "button", "newGameMenu");
		Add(_btnVeteran, "button", "newGameMenu");
		Add(_btnGenius, "button", "newGameMenu");
		Add(_btnSuperhuman, "button", "newGameMenu");
		Add(_btnIronman, "ironman", "newGameMenu");
		Add(_btnOk, "button", "newGameMenu");
		Add(_btnCancel, "button", "newGameMenu");
		Add(_txtTitle, "text", "newGameMenu");
		Add(_txtIronman, "ironman", "newGameMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "newGameMenu");

		_btnBeginner.Text = Tr("STR_1_BEGINNER");
		_btnBeginner.SetGroup(_difficulty);

		_btnExperienced.Text = Tr("STR_2_EXPERIENCED");
		_btnExperienced.SetGroup(_difficulty);

		_btnVeteran.Text = Tr("STR_3_VETERAN");
		_btnVeteran.SetGroup(_difficulty);

		_btnGenius.Text = Tr("STR_4_GENIUS");
		_btnGenius.SetGroup(_difficulty);

		_btnSuperhuman.Text = Tr("STR_5_SUPERHUMAN");
		_btnSuperhuman.SetGroup(_difficulty);

		_btnIronman.Text = Tr("STR_IRONMAN");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELECT_DIFFICULTY_LEVEL"));

		_txtIronman.SetWordWrap(true);
		_txtIronman.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtIronman.SetText(Tr("STR_IRONMAN_DESC"));
	}

	/// <summary>
	/// Starts the new game with the selected difficulty.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		GameDifficulty diff = GameDifficulty.DIFF_BEGINNER;
		if (_difficulty.Value == _btnBeginner)
		{
			diff = GameDifficulty.DIFF_BEGINNER;
		}
		else if (_difficulty.Value == _btnExperienced)
		{
			diff = GameDifficulty.DIFF_EXPERIENCED;
		}
		else if (_difficulty.Value == _btnVeteran)
		{
			diff = GameDifficulty.DIFF_VETERAN;
		}
		else if (_difficulty.Value == _btnGenius)
		{
			diff = GameDifficulty.DIFF_GENIUS;
		}
		else if (_difficulty.Value == _btnSuperhuman)
		{
			diff = GameDifficulty.DIFF_SUPERHUMAN;
		}

		// Reset touch flags
		_game.ResetTouchButtonFlags();

		SavedGame save = _game.Mod.NewSave(diff);
		save.Difficulty = diff;
		save.SetIronman(_btnIronman.Pressed);
		_game.SavedGame = save;

		GeoscapeState gs = new GeoscapeState();
		_game.SetState(gs);
		gs.Init();

		var @base = _game.SavedGame.Bases[^1];
		if (@base.Marker != -1)
		{
			// location known already
			@base.CalculateServices(save);

			// center and rotate 35 degrees down (to see the base location while typoing its name)
			gs.Globe.Center(@base.Longitude, @base.Latitude + 0.61);

			if (string.IsNullOrEmpty(@base.GetName(null)))
			{
				// fixed location, custom name
				_game.PushState(new BaseNameState(@base, gs.Globe, true, true));
			}
			else if (Options.customInitialBase)
			{
				// fixed location, fixed name
				_game.PushState(new PlaceLiftState(@base, gs.Globe, true));
			}
		}
		else
		{
			// custom location, custom name
			_game.PushState(new BuildNewBaseState(@base, gs.Globe, true));
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.SavedGame = null;
		_game.PopState();
	}
}
