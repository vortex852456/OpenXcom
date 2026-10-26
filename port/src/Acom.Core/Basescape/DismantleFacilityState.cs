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
// Ported from: src/Basescape/DismantleFacilityState.h, src/Basescape/DismantleFacilityState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window shown when the player tries to
/// dismantle a facility.
/// </summary>
public class DismantleFacilityState : State
{
	private readonly Base _base;
	private readonly BaseView _view;
	private readonly BaseFacility _fac;

	private readonly TextButton _btnOk, _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtFacility, _txtRefundValue;

	/// <summary>
	/// Initializes all the elements in a Dismantle Facility window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="view">Pointer to the baseview to update.</param>
	/// <param name="fac">Pointer to the facility to dismantle.</param>
	public DismantleFacilityState(Base @base, BaseView view, BaseFacility fac)
	{
		_base = @base;
		_view = view;
		_fac = fac;
		_screen = false;

		// Create objects
		_window = new Window(this, 152, 80, 20, 60);
		_btnOk = new TextButton(44, 16, 36, 115);
		_btnCancel = new TextButton(44, 16, 112, 115);
		_txtTitle = new Text(142, 9, 25, 75);
		_txtFacility = new Text(142, 9, 25, 85);
		_txtRefundValue = new Text(142, 9, 25, 100);

		// Set palette
		SetInterface("dismantleFacility");

		Add(_window, "window", "dismantleFacility");
		Add(_btnOk, "button", "dismantleFacility");
		Add(_btnCancel, "button", "dismantleFacility");
		Add(_txtTitle, "text", "dismantleFacility");
		Add(_txtFacility, "text", "dismantleFacility");
		Add(_txtRefundValue, "text", "dismantleFacility");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "dismantleFacility");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_DISMANTLE"));

		_txtFacility.Align = TextHAlign.ALIGN_CENTER;
		_txtFacility.SetText(Tr(_fac.Rules.Type));

		int refundValue = 0;
		if (_fac.BuildTime > _fac.Rules.BuildTime)
		{
			// if only queued... full refund (= build cost)
			refundValue = _fac.Rules.BuildCost;
		}
		else
		{
			// if already building or already built... partial refund (by default 0, used in mods)
			refundValue = _fac.Rules.RefundValue;
		}

		_txtRefundValue.Align = TextHAlign.ALIGN_CENTER;
		if (refundValue < 0)
		{
			_txtRefundValue.SetText(Tr("STR_REFUND_VALUE_NEGATIVE").Arg(Unicode.FormatFunding(-refundValue)));
			if (_game.SavedGame.Funds < -refundValue)
			{
				// cannot afford dismantle, expenses too high
				_btnOk.Visible = false;
			}
		}
		else
		{
			_txtRefundValue.SetText(Tr("STR_REFUND_VALUE").Arg(Unicode.FormatFunding(refundValue)));
		}
		_txtRefundValue.Visible = refundValue != 0;
	}

	/// <summary>
	/// Dismantles the facility and returns
	/// to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		if (!_fac.Rules.IsLift)
		{
			var itemCost = _fac.Rules.BuildCostItems;

			if (_fac.BuildTime > _fac.Rules.BuildTime)
			{
				// Give full refund if this is a (not yet started) queued build.
				_game.SavedGame.Funds = _game.SavedGame.Funds + _fac.Rules.BuildCost;
				foreach (var pair in itemCost)
				{
					_base.StorageItems.AddItem(_game.Mod.GetItem(pair.Key, true), pair.Value.first);
				}
			}
			else
			{
				// Give partial refund if this is a started build or a completed facility.
				_game.SavedGame.Funds = _game.SavedGame.Funds + _fac.Rules.RefundValue;
				foreach (var pair in itemCost)
				{
					_base.StorageItems.AddItem(_game.Mod.GetItem(pair.Key, true), pair.Value.second);
				}
			}
			if (_fac.Ammo > 0)
			{
				// Full refund of loaded ammo
				_base.StorageItems.AddItem(_fac.Rules.AmmoItem, _fac.Ammo);
				_fac.Ammo = 0;
			}

			var facilities = _base.Facilities;
			for (int facIt = 0; facIt < facilities.Count; ++facIt)
			{
				if (facilities[facIt] == _fac)
				{
					facilities.RemoveAt(facIt);
					// Determine if we leave behind any facilities when this one is removed
					if (_fac.BuildTime == 0 && _fac.Rules.LeavesBehindOnSell.Count != 0)
					{
						var facList = _fac.Rules.LeavesBehindOnSell;
						if (facList[0].PlaceSound != Mod.NO_SOUND)
						{
							_game.Mod.GetSound("GEO.CAT", facList[0].PlaceSound).Play();
						}
						// Make sure the size of the facilities left behind matches the one we removed
						if (facList[0].SizeX == _fac.Rules.SizeX && facList[0].SizeY == _fac.Rules.SizeY) // equal size facilities
						{
							facilities.Add(LeftBehind(facList[0], _fac.X, _fac.Y));
						}
						else
						{
							int j = 0;
							// Otherwise, assume the list of facilities is size 1, and just iterate over it to fill in the old facility's place
							for (int y = _fac.Y; y != _fac.Y + _fac.Rules.SizeY; ++y)
							{
								for (int x = _fac.X; x != _fac.X + _fac.Rules.SizeX; ++x)
								{
									facilities.Add(LeftBehind(facList[j], x, y));

									++j;
									if (j == facList.Count)
									{
										j = 0;
									}
								}
							}
						}
					}
					_view.ResetSelectedFacility();
					// Reset the basescape view in case new facilities were created by removing the old one
					_view.SetBase(_base);
					if (Options.allowBuildingQueue) _view.ReCalcQueuedBuildings();
					break;
				}
			}
		}
		// Remove whole base if it's the access lift
		else
		{
			_game.SavedGame.Bases.Remove(_base);
		}
		_game.PopState();
	}

	/// <summary>A facility left behind by the dismantled one (repeated block of C++ btnOkClick).</summary>
	private BaseFacility LeftBehind(RuleBaseFacility rule, int x, int y)
	{
		var fac = new BaseFacility(rule, _base);
		fac.X = x;
		fac.Y = y;
		if (_fac.Rules.RemovalTime <= -1)
		{
			fac.BuildTime = fac.Rules.BuildTime;
		}
		else
		{
			fac.BuildTime = _fac.Rules.RemovalTime;
		}
		if (fac.BuildTime != 0)
		{
			fac.IfHadPreviousFacility = true;
		}
		return fac;
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
}
