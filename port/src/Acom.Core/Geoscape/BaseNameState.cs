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
// Ported from: src/Geoscape/BaseNameState.h, src/Geoscape/BaseNameState.cpp
#nullable disable

using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window used to input a name for a new base.
/// Player's first base uses this window
/// straight away while subsequent bases only
/// use it after selecting a position on the globe.
/// </summary>
public class BaseNameState : State
{
	private readonly Base _base;
	private readonly Globe _globe;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextEdit _edtName;
	private readonly TextButton _btnOk;
	private readonly bool _first;
	private readonly bool _fixedLocation;

	/// <summary>
	/// Initializes all the elements in a Base Name window.
	/// </summary>
	/// <param name="base">Pointer to the base to name.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	/// <param name="first">Is this the first base in the game?</param>
	/// <param name="fixedLocation">Is this the first base in the game on a fixed location?</param>
	public BaseNameState(Base @base, Globe globe, bool first, bool fixedLocation)
	{
		_base = @base;
		_globe = globe;
		_first = first;
		_fixedLocation = fixedLocation;
		_globe.OnMouseOver(null);

		_screen = false;

		// Create objects
		_window = new Window(this, 192, 80, 32, 60, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(162, 12, 47, 118);
		_txtTitle = new Text(182, 17, 37, 70);
		_edtName = new TextEdit(this, 127, 16, 59, 94);

		// Set palette
		SetInterface("baseNaming");

		Add(_window, "window", "baseNaming");
		Add(_btnOk, "button", "baseNaming");
		Add(_txtTitle, "text", "baseNaming");
		Add(_edtName, "text", "baseNaming");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "baseNaming");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		//_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		//something must be in the name before it is acceptable
		_btnOk.Visible = false;

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_BASE_NAME"));

		if (_game.Mod.BaseNamesFirst.Count != 0)
		{
			StringBuilder ss = new StringBuilder();
			int pickFirst = Rng.Seedless(0, _game.Mod.BaseNamesFirst.Count - 1);
			ss.Append(_game.Mod.BaseNamesFirst[pickFirst]);
			if (_game.Mod.BaseNamesMiddle.Count != 0)
			{
				int pickMiddle = Rng.Seedless(0, _game.Mod.BaseNamesMiddle.Count - 1);
				ss.Append(' ').Append(_game.Mod.BaseNamesMiddle[pickMiddle]);
			}
			if (_game.Mod.BaseNamesLast.Count != 0)
			{
				int pickLast = Rng.Seedless(0, _game.Mod.BaseNamesLast.Count - 1);
				ss.Append(' ').Append(_game.Mod.BaseNamesLast[pickLast]);
			}
			_edtName.Text = ss.ToString();
			_btnOk.Visible = true;
		}
		_edtName.SetBig();
		_edtName.SetFocus(true, false);
		_edtName.OnChange(EdtNameChange);
	}

	/// <summary>
	/// Updates the base name and disables the OK button
	/// if no name is entered.
	/// </summary>
	public void EdtNameChange(Action action)
	{
		if (action.Details.key.keysym.sym == SDLKey.SDLK_RETURN ||
			action.Details.key.keysym.sym == SDLKey.SDLK_KP_ENTER)
		{
			if (_edtName.Text.Length != 0)
			{
				BtnOkClick(action);
			}
		}
		else
		{
			_btnOk.Visible = _edtName.Text.Length != 0;
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		if (_edtName.Text.Length != 0)
		{
			_base.SetName(_edtName.Text);
			_game.PopState(); // pop BaseNameState
			if (!_fixedLocation)
			{
				_game.PopState(); // pop ConfirmNewBaseState or BuildNewBaseState
				if (!_first)
				{
					_game.PopState(); // pop BuildNewBaseState
				}
			}
			if (!_first || Options.customInitialBase)
			{
				_game.PushState(new PlaceLiftState(_base, _globe, _first));
			}
		}
	}
}
