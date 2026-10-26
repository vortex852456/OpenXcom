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
// Ported from: src/Menu/ListGamesState.h, src/Menu/ListGamesState.cpp
#nullable disable
#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

namespace Acom;

/// <summary>
/// Base class for saved game screens which
/// provides the common layout and listing.
/// </summary>
/// <remarks>The C++ __MOBILE__ layout (delete toggle button, shifted list) is not ported; the desktop layout is used.</remarks>
public class ListGamesState : State
{
	protected TextButton _btnCancel;
	protected Window _window;
	protected ToggleTextButton _btnDelete;
	protected Text _txtTitle;
	protected Text _txtName;
	protected Text _txtDate;
	protected Text _txtDelete;
	protected Text _txtDetails;
	protected TextList _lstSaves;
	protected ArrowButton _sortName;
	protected ArrowButton _sortDate;
	protected OptionsOrigin _origin;
	protected List<SaveInfo> _saves = [];
	protected uint _firstValidRow;
	protected bool _autoquick;
	protected bool _sortable;

	/// <summary>C++ compareSaveName: reserved saves first, then natural order of the display names.</summary>
	internal static bool CompareSaveName(SaveInfo a, SaveInfo b, bool reverse)
	{
		if (a.reserved == b.reserved)
		{
			return Unicode.NaturalCompare(a.displayName, b.displayName);
		}
		else
		{
			return reverse ? b.reserved : a.reserved;
		}
	}

	/// <summary>C++ compareSaveTimestamp: reserved saves first, then by file time.</summary>
	internal static bool CompareSaveTimestamp(SaveInfo a, SaveInfo b, bool reverse)
	{
		if (a.reserved == b.reserved)
		{
			return a.timestamp < b.timestamp;
		}
		else
		{
			return reverse ? b.reserved : a.reserved;
		}
	}

	/// <summary>
	/// Sorts a save list like the C++ std::sort calls: ascending over the list, or (descending)
	/// ascending over the reversed list.
	/// </summary>
	/// <remarks>Deviation: the sort is stable (std::sort leaves the order of equal elements unspecified).</remarks>
	internal static void SortSaves(List<SaveInfo> saves, SaveSort sort)
	{
		Func<SaveInfo, SaveInfo, bool, bool> less;
		bool reverse;
		switch (sort)
		{
			case SaveSort.SORT_NAME_ASC:
				less = CompareSaveName;
				reverse = false;
				break;
			case SaveSort.SORT_NAME_DESC:
				less = CompareSaveName;
				reverse = true;
				break;
			case SaveSort.SORT_DATE_ASC:
				less = CompareSaveTimestamp;
				reverse = false;
				break;
			case SaveSort.SORT_DATE_DESC:
				less = CompareSaveTimestamp;
				reverse = true;
				break;
			default:
				return;
		}
		int Compare(SaveInfo x, SaveInfo y) => less(x, y, reverse) ? -1 : less(y, x, reverse) ? 1 : 0;
		// Sorting the reverse range ascending = sorting the forward range descending.
		var comparer = reverse ? Comparer<SaveInfo>.Create((x, y) => Compare(y, x)) : Comparer<SaveInfo>.Create(Compare);
		var sorted = saves.OrderBy(s => s, comparer).ToList();
		saves.Clear();
		saves.AddRange(sorted);
	}

	/// <summary>Initializes all the elements in the Saved Game screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	/// <param name="firstValidRow">First row containing saves.</param>
	/// <param name="autoquick">Show auto/quick saved games?</param>
	public ListGamesState(OptionsOrigin origin, int firstValidRow, bool autoquick)
	{
		_origin = origin;
		_firstValidRow = unchecked((uint)firstValidRow);
		_autoquick = autoquick;
		_sortable = true;
		_screen = false;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0, WindowPopup.POPUP_BOTH);
		_btnCancel = new TextButton(80, 16, 120, 172);
		_txtTitle = new Text(310, 17, 5, 7);
		_txtDelete = new Text(310, 9, 5, 23);
		_txtName = new Text(150, 9, 16, 32);
		_txtDate = new Text(110, 9, 204, 32);
		_lstSaves = new TextList(288, 112, 8, 42);
		_txtDetails = new Text(288, 16, 16, 156);
		_sortName = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 16, 32);
		_sortDate = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 204, 32);
		_btnDelete = new ToggleTextButton(288, 16, 16, 23);

		// Set palette
		SetInterface("geoscape", true, _game.SavedGame?.SavedBattle);

		Add(_window, "window", "saveMenus");
		Add(_btnCancel, "button", "saveMenus");
		Add(_txtTitle, "text", "saveMenus");
		Add(_txtDelete, "text", "saveMenus");
		Add(_txtName, "text", "saveMenus");
		Add(_txtDate, "text", "saveMenus");
		Add(_lstSaves, "list", "saveMenus");
		Add(_txtDetails, "text", "saveMenus");
		Add(_sortName, "text", "saveMenus");
		Add(_sortDate, "text", "saveMenus");
		Add(_btnDelete, "button", "saveMenus");

		// Set up objects
		SetWindowBackground(_window, "saveMenus");

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;

		_btnDelete.Visible = false;
		_txtDelete.Align = TextHAlign.ALIGN_CENTER;
		_txtDelete.SetText(Tr("STR_RIGHT_CLICK_TO_DELETE"));

		_txtName.SetText(Tr("STR_NAME"));

		_txtDate.SetText(Tr("STR_DATE"));

		_lstSaves.SetColumns(3, 188, 60, 40);
		_lstSaves.SetSelectable(true);
		_lstSaves.SetBackground(_window);
		_lstSaves.Margin = 8;
		_lstSaves.OnMouseOver(LstSavesMouseOver);
		_lstSaves.OnMouseOut(LstSavesMouseOut);
		_lstSaves.OnMousePress(LstSavesPress);

		_txtDetails.SetWordWrap(true);
		_txtDetails.SetText(Tr("STR_DETAILS").Arg(""));

		_sortName.X = _sortName.X + _txtName.GetTextWidth() + 5;
		_sortName.OnMouseClick(SortNameClick);

		_sortDate.X = _sortDate.X + _txtDate.GetTextWidth() + 5;
		_sortDate.OnMouseClick(SortDateClick);

		UpdateArrows();
	}

	/// <summary>Errors the C++ code catches as OpenXcom::Exception / YAML::Exception.</summary>
	internal static bool IsSaveError(Exception e) => e is InvalidOperationException or YamlReadException or IOException or UnauthorizedAccessException;

	/// <summary>Refreshes the saves list.</summary>
	public override void Init()
	{
		base.Init();

		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("saveMenus");
		}

		try
		{
			_saves = SavedGame.GetList(_game.Language, _autoquick);
			_lstSaves.ClearList();
			SortList(Options.saveOrder);
		}
		catch (Exception e) when (IsSaveError(e))
		{
			Logger.Error(e.Message);
		}
	}

	/// <summary>Updates the sorting arrows based on the current setting.</summary>
	protected void UpdateArrows()
	{
		_sortName.SetShape(ArrowShape.ARROW_NONE);
		_sortDate.SetShape(ArrowShape.ARROW_NONE);
		switch (Options.saveOrder)
		{
			case SaveSort.SORT_NAME_ASC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case SaveSort.SORT_NAME_DESC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			case SaveSort.SORT_DATE_ASC:
				_sortDate.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case SaveSort.SORT_DATE_DESC:
				_sortDate.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
		}
	}

	/// <summary>Sorts the save game list.</summary>
	/// <param name="sort">Order to sort the games in.</param>
	public void SortList(SaveSort sort)
	{
		SortSaves(_saves, sort);
		UpdateList();
	}

	/// <summary>Updates the save game list with the current list of available savegames.</summary>
	public virtual void UpdateList()
	{
		int row = 0;
		byte color = _lstSaves.SecondaryColor;
		foreach (var saveInfo in _saves)
		{
			_lstSaves.AddRow(3, saveInfo.displayName, saveInfo.isoDate, saveInfo.isoTime);
			if (saveInfo.reserved && _origin != OptionsOrigin.OPT_BATTLESCAPE)
			{
				_lstSaves.SetRowColor(row, color);
			}
			row++;
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Shows the details of the currently hovered save.</summary>
	public void LstSavesMouseOver(Action action)
	{
		int sel = unchecked((int)(_lstSaves.SelectedRow - _firstValidRow));
		string wstr = "";
		if (sel >= 0 && sel < _saves.Count)
		{
			wstr = _saves[sel].details;
		}
		_txtDetails.SetText(Tr("STR_DETAILS").Arg(wstr));
	}

	/// <summary>Clears the details.</summary>
	public void LstSavesMouseOut(Action action)
	{
		_txtDetails.SetText(Tr("STR_DETAILS").Arg(""));
	}

	/// <summary>Deletes the selected save.</summary>
	public virtual void LstSavesPress(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if ((action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT || _btnDelete.Pressed) && _lstSaves.SelectedRow >= _firstValidRow)
		{
			_game.PushState(new DeleteGameState(_origin, _saves[(int)(_lstSaves.SelectedRow - _firstValidRow)].fileName));
		}
	}

	/// <summary>Sorts the saves by name.</summary>
	public void SortNameClick(Action action)
	{
		if (_sortable)
		{
			if (Options.saveOrder == SaveSort.SORT_NAME_ASC)
			{
				Options.saveOrder = SaveSort.SORT_NAME_DESC;
			}
			else
			{
				Options.saveOrder = SaveSort.SORT_NAME_ASC;
			}
			UpdateArrows();
			_lstSaves.ClearList();
			SortList(Options.saveOrder);
		}
	}

	/// <summary>Sorts the saves by date.</summary>
	public void SortDateClick(Action action)
	{
		if (_sortable)
		{
			if (Options.saveOrder == SaveSort.SORT_DATE_ASC)
			{
				Options.saveOrder = SaveSort.SORT_DATE_DESC;
			}
			else
			{
				Options.saveOrder = SaveSort.SORT_DATE_ASC;
			}
			UpdateArrows();
			_lstSaves.ClearList();
			SortList(Options.saveOrder);
		}
	}

	/// <summary>Disables the sort buttons.</summary>
	public void DisableSort()
	{
		_sortable = false;
	}
}
