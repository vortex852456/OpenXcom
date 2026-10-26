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
// Ported from: src/Geoscape/ResearchRequiredState.h, src/Geoscape/ResearchRequiredState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window shown when the player researches a weapon
/// before the respective clip.
/// </summary>
public class ResearchRequiredState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextButton _btnOk;

	/// <summary>
	/// Initializes all the elements in the Research Required screen.
	/// </summary>
	/// <param name="item">Pointer to the researched weapon.</param>
	public ResearchRequiredState(RuleItem item)
	{
		_screen = false;

		// Create objects
		_window = new Window(this, 288, 180, 16, 10);
		_btnOk = new TextButton(160, 18, 80, 150);
		_txtTitle = new Text(288, 80, 16, 50);

		// Set palette
		SetInterface("geoResearchRequired");

		Add(_window, "window", "geoResearchRequired");
		Add(_btnOk, "button", "geoResearchRequired");
		Add(_txtTitle, "text1", "geoResearchRequired");

		CenterAllSurfaces();

		string weapon = item.Type;
		string clip = item.PrimaryCompatibleAmmo[0].Type;

		// Set up objects
		SetWindowBackground(_window, "geoResearchRequired");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtTitle.SetText(Tr("STR_YOU_NEED_TO_RESEARCH_ITEM_TO_PRODUCE_ITEM")
						   .Arg(Tr(clip))
						   .Arg(Tr(weapon)));
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
