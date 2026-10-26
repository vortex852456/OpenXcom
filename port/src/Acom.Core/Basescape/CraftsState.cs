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
// Ported from: src/Basescape/CraftsState.h, src/Basescape/CraftsState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Equip Craft screen that lets the player
/// manage all the crafts in a base.
/// </summary>
public class CraftsState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtBase, _txtName, _txtStatus, _txtWeapon, _txtCrew, _txtHwp;
	private readonly TextList _lstCrafts;
	private readonly Base _base;

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Initializes all the elements in the Equip Craft screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	public CraftsState(Base @base)
	{
		_base = @base;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(288, 16, 16, 176);
		_txtTitle = new Text(298, 17, 16, 8);
		_txtBase = new Text(298, 17, 16, 24);
		_txtName = new Text(94, 9, 16, 40);
		_txtStatus = new Text(50, 9, 110, 40);
		_txtWeapon = new Text(50, 17, 160, 40);
		_txtCrew = new Text(58, 9, 210, 40);
		_txtHwp = new Text(46, 9, 268, 40);
		_lstCrafts = new TextList(288, 112, 8, 58);

		// Set palette
		SetInterface("craftSelect");

		Add(_window, "window", "craftSelect");
		Add(_btnOk, "button", "craftSelect");
		Add(_txtTitle, "text", "craftSelect");
		Add(_txtBase, "text", "craftSelect");
		Add(_txtName, "text", "craftSelect");
		Add(_txtStatus, "text", "craftSelect");
		Add(_txtWeapon, "text", "craftSelect");
		Add(_txtCrew, "text", "craftSelect");
		Add(_txtHwp, "text", "craftSelect");
		Add(_lstCrafts, "list", "craftSelect");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftSelect");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_INTERCEPTION_CRAFT"));

		_txtBase.SetBig();
		_txtBase.SetText(Tr("STR_BASE_").Arg(_base.GetName(null)));

		_txtName.SetText(Tr("STR_NAME_UC"));

		_txtStatus.SetText(Tr("STR_STATUS"));

		_txtWeapon.SetText(Tr("STR_WEAPON_SYSTEMS"));
		_txtWeapon.SetWordWrap(true);

		_txtCrew.SetText(Tr("STR_CREW"));

		_txtHwp.SetText(Tr("STR_HWPS"));
		_lstCrafts.SetColumns(5, 94, 68, 44, 46, 28);
		_lstCrafts.SetSelectable(true);
		_lstCrafts.SetBackground(_window);
		_lstCrafts.Margin = 8;
		_lstCrafts.OnMouseClick(LstCraftsClick);
		_lstCrafts.OnMouseClick(LstCraftsClick, SDLMouse.SDL_BUTTON_RIGHT);
		_lstCrafts.OnMouseClick(LstCraftsClick, SDLMouse.SDL_BUTTON_MIDDLE);
	}

	/// <summary>
	/// The soldier names can change
	/// after going into other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();

		InitList(0);
	}

	/// <summary>
	/// Shows the crafts in a list at specified offset/scroll.
	/// </summary>
	private void InitList(int scrl)
	{
		_lstCrafts.ClearList();
		foreach (var craft in _base.Crafts)
		{
			string ss = Str(craft.GetNumWeapons()) + "/" + Str(craft.Rules.Weapons);
			_lstCrafts.AddRow(5, craft.GetName(_game.Language), Tr(craft.Status), ss, Str(craft.NumTotalSoldiers), Str(craft.NumTotalVehicles));
		}

		if (scrl != 0)
			_lstCrafts.ScrollTo(scrl);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();

		if (_game.SavedGame.MonthsPassed > -1 && Options.storageLimitsEnforced && _base.StoresOverfull())
		{
			_game.PushState(new SellState(_base, null));
			_game.PushState(new ErrorMessageState(Tr("STR_STORAGE_EXCEEDED").Arg(_base.GetName(null)), _palette, (byte)_game.Mod.GetInterface("craftSelect").GetElement("errorMessage").color, "BACK01.SCR", _game.Mod.GetInterface("craftSelect").GetElement("errorPalette").color));
		}
	}

	/// <summary>
	/// Shows the selected craft's info.
	/// </summary>
	public void LstCraftsClick(Action action)
	{
		var crafts = _base.Crafts;
		int row = (int)_lstCrafts.SelectedRow;

		if (_game.IsLeftClick(action))
		{
			if (crafts[row].Status != "STR_OUT")
			{
				_game.PushState(new CraftInfoState(_base, row));
			}
		}
		else if (_game.IsRightClick(action))
		{
			bool shift = _game.IsShiftPressed();
			if (shift && row < (crafts.Count - 1))
			{
				// move craft down in the list
				(crafts[row], crafts[row + 1]) = (crafts[row + 1], crafts[row]);

				// warp mouse
				if (row != _lstCrafts.VisibleRows - 1 + _lstCrafts.Scroll)
				{
					SDLMouse.SDL_WarpMouse((ushort)(action.LeftBlackBand + action.XMouse), (ushort)(action.TopBlackBand + action.YMouse + (ushort)(8 * action.YScale)));
				}
				else
				{
					_lstCrafts.ScrollDown(false);
				}

				// reload the UI
				InitList(_lstCrafts.Scroll);
			}
			if (!shift && row > 0)
			{
				// move craft up in the list
				(crafts[row], crafts[row - 1]) = (crafts[row - 1], crafts[row]);

				// warp mouse
				if (row != _lstCrafts.Scroll)
				{
					SDLMouse.SDL_WarpMouse((ushort)(action.LeftBlackBand + action.XMouse), (ushort)(action.TopBlackBand + action.YMouse - (ushort)(8 * action.YScale)));
				}
				else
				{
					_lstCrafts.ScrollUp(false);
				}

				// reload the UI
				InitList(_lstCrafts.Scroll);
			}
		}
		else if (_game.IsMiddleClick(action))
		{
			string articleId = crafts[row].Rules.Type;
			Ufopaedia.OpenArticle(_game, articleId);
		}
	}
}
