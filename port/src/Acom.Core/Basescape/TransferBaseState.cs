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
// Ported from: src/Basescape/TransferBaseState.h, src/Basescape/TransferBaseState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window that lets the player pick the base
/// to transfer items to.
/// </summary>
public class TransferBaseState : State
{
	private readonly Base _base;
	private readonly DebriefingState _debriefingState;
	private readonly TextButton _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtFunds, _txtName, _txtArea;
	private readonly TextList _lstBases;
	private readonly List<Base> _bases = [];

	/// <summary>
	/// Initializes all the elements in the Select Destination Base window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="debriefingState">Pointer to the debriefing state, or null.</param>
	public TransferBaseState(Base @base, DebriefingState debriefingState)
	{
		_base = @base;
		_debriefingState = debriefingState;

		// Create objects
		_window = new Window(this, 280, 140, 20, 30);
		_btnCancel = new TextButton(264, 16, 28, 146);
		_txtTitle = new Text(270, 17, 25, 38);
		_txtFunds = new Text(250, 9, 30, 54);
		_txtName = new Text(130, 17, 28, 64);
		_txtArea = new Text(130, 17, 160, 64);
		_lstBases = new TextList(248, 64, 28, 80);

		// Set palette
		SetInterface("transferBaseSelect");

		Add(_window, "window", "transferBaseSelect");
		Add(_btnCancel, "button", "transferBaseSelect");
		Add(_txtTitle, "text", "transferBaseSelect");
		Add(_txtFunds, "text", "transferBaseSelect");
		Add(_txtName, "text", "transferBaseSelect");
		Add(_txtArea, "text", "transferBaseSelect");
		Add(_lstBases, "list", "transferBaseSelect");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "transferBaseSelect");

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELECT_DESTINATION_BASE"));

		_txtFunds.SetText(Tr("STR_CURRENT_FUNDS").Arg(Unicode.FormatFunding(_game.SavedGame.Funds)));

		_txtName.SetText(Tr("STR_NAME"));
		_txtName.SetBig();

		_txtArea.SetText(Tr("STR_AREA"));
		_txtArea.SetBig();

		_lstBases.SetColumns(2, 130, 116);
		_lstBases.SetSelectable(true);
		_lstBases.SetBackground(_window);
		_lstBases.Margin = 2;
		_lstBases.OnMouseClick(LstBasesClick);

		foreach (var xbase in _game.SavedGame.Bases)
		{
			if (xbase != _base)
			{
				// Get area
				string area = "";
				foreach (var region in _game.SavedGame.Regions)
				{
					if (region.Rules.InsideRegion(xbase.Longitude, xbase.Latitude))
					{
						area = Tr(region.Rules.Type);
						break;
					}
				}
				_lstBases.AddRow(2, xbase.GetName(null), Unicode.TOK_COLOR_FLIP + area);
				_bases.Add(xbase);
			}
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Shows the Transfer screen for the selected base.
	/// </summary>
	public void LstBasesClick(Action action)
	{
		_game.PushState(new TransferItemsState(_base, _bases[(int)_lstBases.SelectedRow], _debriefingState));
	}
}
