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
// Ported from: src/Geoscape/DogfightErrorState.h, src/Geoscape/DogfightErrorState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

/// <summary>
/// Displays an error message related to an interception
/// (e.g. the craft cannot reach the UFO).
/// </summary>
public class DogfightErrorState : State
{
	private readonly Craft _craft;
	private readonly Window _window;
	private readonly Text _txtCraft, _txtMessage;
	private readonly TextButton _btnIntercept, _btnBase;

	/// <summary>
	/// Initializes all the elements in a Dogfight Error window.
	/// </summary>
	/// <param name="craft">Pointer to the craft.</param>
	/// <param name="msg">Error message.</param>
	public DogfightErrorState(Craft craft, string msg)
	{
		_craft = craft;
		_screen = false;

		// Create objects
		_window = new Window(this, 208, 120, 24, 48, WindowPopup.POPUP_BOTH);
		_btnIntercept = new TextButton(180, 12, 38, 128);
		_btnBase = new TextButton(180, 12, 38, 144);
		_txtCraft = new Text(198, 16, 29, 63);
		_txtMessage = new Text(198, 20, 29, 94);

		// Set palette
		SetInterface("dogfightInfo");

		Add(_window, "window", "dogfightInfo");
		Add(_btnIntercept, "button", "dogfightInfo");
		Add(_btnBase, "button", "dogfightInfo");
		Add(_txtCraft, "text", "dogfightInfo");
		Add(_txtMessage, "text", "dogfightInfo");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "dogfightInfo");

		_btnIntercept.Text = Tr("STR_CONTINUE_INTERCEPTION_PURSUIT");
		_btnIntercept.OnMouseClick(BtnInterceptClick);
		_btnIntercept.OnKeyboardPress(BtnInterceptClick, Options.keyCancel);

		_btnBase.Text = Tr("STR_RETURN_TO_BASE");
		_btnBase.OnMouseClick(BtnBaseClick);
		_btnBase.OnKeyboardPress(BtnBaseClick, Options.keyOk);

		_txtCraft.Align = TextHAlign.ALIGN_CENTER;
		_txtCraft.SetBig();
		_txtCraft.SetText(_craft.GetName(_game.Language));

		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.SetWordWrap(true);
		_txtMessage.SetText(msg);
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnInterceptClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Returns the craft to base.
	/// </summary>
	public void BtnBaseClick(Action action)
	{
		_craft.ReturnToBase();
		_game.PopState();
	}
}
