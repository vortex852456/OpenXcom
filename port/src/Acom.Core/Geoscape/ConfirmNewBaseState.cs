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
// Ported from: src/Geoscape/ConfirmNewBaseState.h, src/Geoscape/ConfirmNewBaseState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window that allows the player
/// to confirm a new base on the globe.
/// </summary>
public class ConfirmNewBaseState : State
{
	private readonly Base _base;
	private readonly Globe _globe;
	private readonly Window _window;
	private readonly Text _txtCost, _txtArea;
	private readonly TextButton _btnOk, _btnCancel;
	private readonly int _cost;

	/// <summary>
	/// Initializes all the elements in the Confirm New Base window.
	/// </summary>
	/// <param name="base">Pointer to the base to place.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	public ConfirmNewBaseState(Base @base, Globe globe)
	{
		_base = @base;
		_globe = globe;
		_cost = 0;
		_screen = false;

		// Create objects
		_window = new Window(this, 224, 72, 16, 64);
		_btnOk = new TextButton(54, 12, 68, 104);
		_btnCancel = new TextButton(54, 12, 138, 104);
		_txtCost = new Text(120, 9, 68, 80);
		_txtArea = new Text(120, 9, 68, 90);

		// Set palette
		SetInterface("geoscape");

		Add(_window, "genericWindow", "geoscape");
		Add(_btnOk, "genericButton2", "geoscape");
		Add(_btnCancel, "genericButton2", "geoscape");
		Add(_txtCost, "genericText", "geoscape");
		Add(_txtArea, "genericText", "geoscape");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "geoscape");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		string area = "";
		foreach (var region in _game.SavedGame.Regions)
		{
			if (region.Rules.InsideRegion(_base.Longitude, _base.Latitude))
			{
				_cost = region.Rules.BaseCost;
				area = Tr(region.Rules.Type);
				break;
			}
		}

		_txtCost.SetText(Tr("STR_COST_").Arg(Unicode.FormatFunding(_cost)));
		_txtArea.SetText(Tr("STR_AREA_").Arg(area));
	}

	/// <summary>
	/// Go to the Place Access Lift screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		if (_game.SavedGame.Funds >= _cost)
		{
			_game.SavedGame.Funds = _game.SavedGame.Funds - _cost;
			_game.SavedGame.Bases.Add(_base);
			_game.PushState(new BaseNameState(_base, _globe, false, false));
		}
		else
		{
			RuleInterface geoscape = _game.Mod.GetInterface("geoscape");
			_game.PushState(new ErrorMessageState(Tr("STR_NOT_ENOUGH_MONEY"), _palette, unchecked((byte)geoscape.GetElement("genericWindow").color), "BACK01.SCR", geoscape.GetElement("palette").color));
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_globe.OnMouseOver(null);
		_game.PopState();
	}
}
