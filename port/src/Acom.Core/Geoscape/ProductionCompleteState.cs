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
// Ported from: src/Geoscape/ProductionCompleteState.h, src/Geoscape/ProductionCompleteState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window used to notify the player when
/// a production is completed.
/// </summary>
public class ProductionCompleteState : State
{
	private readonly Base _base;
	private readonly GeoscapeState _state;

	private readonly SortedDictionary<string, int> _randomProductionInfo = new(StringComparer.Ordinal);
	private readonly List<string> _index = [];

	private readonly TextButton _btnOk, _btnGotoBase, _btnSummary;
	private readonly Window _window;
	private readonly Text _txtMessage, _txtItem, _txtQuantity;
	private readonly TextList _lstSummary;
	private readonly productionProgress_e _endType;

	/// <summary>
	/// Initializes all the elements in a Production Complete window.
	/// </summary>
	/// <param name="base">Pointer to base the production belongs to.</param>
	/// <param name="item">Item that finished producing.</param>
	/// <param name="state">Pointer to the Geoscape state.</param>
	/// <param name="endType">What ended the production.</param>
	/// <param name="production">Pointer to the production details.</param>
	public ProductionCompleteState(Base @base, string item, GeoscapeState state, productionProgress_e endType = productionProgress_e.PROGRESS_COMPLETE, Production production = null)
	{
		_base = @base;
		_state = state;
		_endType = endType;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 160, 32, 20, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(118, 18, 40, 154);
		_btnGotoBase = new TextButton(118, 18, 162, 154);
		_btnSummary = new TextButton(118, 18, 162, 154);
		_txtMessage = new Text(246, 110, 37, 35);
		_txtItem = new Text(160, 9, 47, 35);
		_txtQuantity = new Text(70, 9, 209, 35);
		_lstSummary = new TextList(224, 96, 39, 45);

		// Set palette
		SetInterface("geoManufactureComplete");

		Add(_window, "window", "geoManufactureComplete");
		Add(_btnOk, "button", "geoManufactureComplete");
		Add(_btnGotoBase, "button", "geoManufactureComplete");
		Add(_btnSummary, "button", "geoManufactureComplete");
		Add(_txtMessage, "text1", "geoManufactureComplete");
		Add(_txtItem, "text1", "geoManufactureComplete");
		Add(_txtQuantity, "text1", "geoManufactureComplete");
		Add(_lstSummary, "list", "geoManufactureComplete");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "geoManufactureComplete");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		if (_endType != productionProgress_e.PROGRESS_CONSTRUCTION)
		{
			_btnGotoBase.Text = Tr("STR_ALLOCATE_MANUFACTURE");
		}
		else
		{
			_btnGotoBase.Text = Tr("STR_GO_TO_BASE");
		}
		_btnGotoBase.OnMouseClick(BtnGotoBaseClick);

		_btnSummary.Text = Tr("STR_RANDOM_PRODUCTION_SUMMARY");
		_btnSummary.OnMouseClick(BtnSummaryClick);

		_txtItem.SetText(Tr("STR_ITEM"));
		_txtItem.Visible = false;

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));
		_txtQuantity.Visible = false;

		_lstSummary.SetColumns(2, 162, 46);
		_lstSummary.SetBackground(_window);
		_lstSummary.SetSelectable(true);
		_lstSummary.Margin = 8;
		_lstSummary.Visible = false;
		_lstSummary.OnMouseClick(LstSummaryClick, SDLMouse.SDL_BUTTON_RIGHT);

		if (production != null && production.Rules.RandomProducedItems.Count != 0)
		{
			_btnGotoBase.Visible = false;
			// local copy is necessary
			foreach (var each in production.RandomProductionInfo)
			{
				_randomProductionInfo[each.Key] = each.Value;
			}
			_index.Capacity = _randomProductionInfo.Count;
			foreach (var each in production.RandomProductionInfo)
			{
				_lstSummary.AddRow(2, Tr(each.Key), each.Value.ToString(CultureInfo.InvariantCulture));
				_index.Add(each.Key);
			}
		}
		else
		{
			_btnSummary.Visible = false;
		}

		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtMessage.SetBig();
		_txtMessage.SetWordWrap(true);
		string s = "";
		switch (_endType)
		{
			case productionProgress_e.PROGRESS_CONSTRUCTION:
				s = Tr("STR_CONSTRUCTION_OF_FACILITY_AT_BASE_IS_COMPLETE").Arg(item).Arg(@base.GetName(null));
				break;
			case productionProgress_e.PROGRESS_COMPLETE:
				s = Tr("STR_PRODUCTION_OF_ITEM_AT_BASE_IS_COMPLETE").Arg(item).Arg(@base.GetName(null));
				break;
			case productionProgress_e.PROGRESS_NOT_ENOUGH_MONEY:
				s = Tr("STR_NOT_ENOUGH_MONEY_TO_PRODUCE_ITEM_AT_BASE").Arg(item).Arg(@base.GetName(null));
				break;
			case productionProgress_e.PROGRESS_NOT_ENOUGH_MATERIALS:
				s = Tr("STR_NOT_ENOUGH_SPECIAL_MATERIALS_TO_PRODUCE_ITEM_AT_BASE").Arg(item).Arg(@base.GetName(null));
				break;
			case productionProgress_e.PROGRESS_NOT_ENOUGH_LIVING_SPACE:
				s = Tr("STR_NOT_ENOUGH_LIVING_SPACE_AT_BASE").Arg(item).Arg(@base.GetName(null));
				break;
			default:
				System.Diagnostics.Debug.Assert(false);
				break;
		}
		_txtMessage.SetText(s);
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Goes to the base for the respective production.
	/// </summary>
	public void BtnGotoBaseClick(Action action)
	{
		_state.TimerReset();
		_game.PopState();
		if (_endType != productionProgress_e.PROGRESS_CONSTRUCTION)
		{
			_game.PushState(new ManufactureState(_base));
		}
		else
		{
			_game.PushState(new BasescapeState(_base, _state.Globe));
		}
	}

	/// <summary>
	/// Displays the random production info.
	/// </summary>
	public void BtnSummaryClick(Action action)
	{
		_btnSummary.Visible = false;
		_txtMessage.Visible = false;

		_btnGotoBase.Visible = true;
		_txtItem.Visible = true;
		_txtQuantity.Visible = true;
		_lstSummary.Visible = true;
	}

	/// <summary>
	/// Sells the selected item(s)... unless they were sold, transferred or otherwise disposed of earlier already.
	/// </summary>
	public void LstSummaryClick(Action action)
	{
		// 1. remember stuff
		var scrollPos = _lstSummary.Scroll;
		int sel = (int)_lstSummary.SelectedRow;
		string itemName = _index[sel];
		int itemCount = MapGet(itemName);

		// 2. deal with it
		var itemRule = _game.Mod.GetItem(itemName, false);
		if (itemRule != null)
		{
			// check if we sold something in the meantime
			if (_base.StorageItems.GetItem(itemRule) < itemCount)
			{
				itemCount = _base.StorageItems.GetItem(itemRule);
				_randomProductionInfo[itemName] = MapGet(itemName) - itemCount; // just decrease amount by the maximum we can sell
			}
			else
			{
				// remove completely
				_index.RemoveAt(sel);
			}

			if (itemCount > 0)
			{
				long adjustedSellValue = itemRule.GetSellCostAdjusted(_base, _game.SavedGame);
				adjustedSellValue *= itemCount;
				_game.SavedGame.Funds = _game.SavedGame.Funds + adjustedSellValue;
				_base.StorageItems.RemoveItem(itemRule, itemCount);
			}
		}

		// 3. refresh UI
		_lstSummary.ClearList();
		foreach (var name in _index)
		{
			_lstSummary.AddRow(2, Tr(name), MapGet(name).ToString(CultureInfo.InvariantCulture));
		}
		_lstSummary.ScrollTo(scrollPos);
	}

	/// <summary>C++ std::map::operator[]: inserts a zero entry for a missing key.</summary>
	private int MapGet(string key)
	{
		if (!_randomProductionInfo.TryGetValue(key, out int v))
		{
			_randomProductionInfo[key] = 0;
		}
		return v;
	}
}
