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
// Ported from: src/Basescape/GlobalResearchDiaryState.h, src/Basescape/GlobalResearchDiaryState.cpp
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Research diary sorting modes.</summary>
public enum ReserachDiarySort : uint
{
	RESEARCH_DIARY_SORT_NONE = 0,
	RESEARCH_DIARY_SORT_NAME_ASC = 1,
	RESEARCH_DIARY_SORT_NAME_DESC = 2,
	RESEARCH_DIARY_SORT_DATE_ASC = 3,
	RESEARCH_DIARY_SORT_DATE_DESC = 4,
}

/// <summary>A research diary entry with its translated name and formatted date.</summary>
public class TranslatedResearchDiaryItem
{
	public ResearchDiaryEntry diaryEntry;
	public string name;
	public string upperName;
	public string date;
	public int sortOrder;

	public TranslatedResearchDiaryItem(ResearchDiaryEntry _diaryEntry, string _name, string _upperName, string _date, int _sortOrder)
	{
		diaryEntry = _diaryEntry;
		name = _name;
		upperName = _upperName;
		date = _date;
		sortOrder = _sortOrder;
	}
}

/// <summary>
/// Research Diary window that displays all discovered research.
/// </summary>
public class GlobalResearchDiaryState : State
{
	private readonly TextButton _btnOk;
	private readonly TextEdit _btnQuickSearch;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtName, _txtType, _txtDate;
	private readonly TextList _lstItems;
	private readonly ArrowButton _sortName, _sortDate;
	private readonly Text _txtTooltip;
	private bool _doNotReset;
	private readonly List<TranslatedResearchDiaryItem> _itemList = [];
	private readonly List<TranslatedResearchDiaryItem> _filteredItemList = [];
	private ReserachDiarySort _itemOrder;

	/// <summary>
	/// Initializes all the elements in the Research Diary window.
	/// </summary>
	public GlobalResearchDiaryState()
	{
		_doNotReset = false;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnQuickSearch = new TextEdit(this, 48, 9, 10, 19);
		_btnOk = new TextButton(304, 16, 8, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtName = new Text(142, 9, 10, 29);
		_txtType = new Text(48, 9, 193, 29);
		_txtDate = new Text(54, 9, 242, 29);
		_lstItems = new TextList(288, 96, 8, 38);
		_sortName = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 10, 29);
		_sortDate = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 242, 29);
		_txtTooltip = new Text(304, 40, 8, 135);

		// Set palette
		SetInterface("globalResearchDiary");

		Add(_window, "window", "globalResearchDiary");
		Add(_btnQuickSearch, "button", "globalResearchDiary");
		Add(_btnOk, "button", "globalResearchDiary");
		Add(_txtTitle, "text", "globalResearchDiary");
		Add(_txtName, "text", "globalResearchDiary");
		Add(_txtType, "text", "globalResearchDiary");
		Add(_txtDate, "text", "globalResearchDiary");
		Add(_lstItems, "list", "globalResearchDiary");
		Add(_sortName, "text", "globalResearchDiary");
		Add(_sortDate, "text", "globalResearchDiary");
		Add(_txtTooltip, "text", "globalResearchDiary");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "globalResearchDiary");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_RESEARCH_DIARY"));

		_txtName.SetText(Tr("STR_NAME_UC"));

		_txtType.SetText(Tr("STR_TYPE"));
		_txtType.Align = TextHAlign.ALIGN_CENTER;

		_txtDate.SetText(Tr("STR_DATE_UC"));

		_lstItems.SetColumns(3, 204, 28, 54);
		_lstItems.SetSelectable(true);
		_lstItems.SetBackground(_window);
		_lstItems.Margin = 2;
		_lstItems.OnMouseClick(LstItemLClick, SDLMouse.SDL_BUTTON_LEFT);
		_lstItems.OnMouseClick(LstItemMClick, SDLMouse.SDL_BUTTON_MIDDLE);
		_lstItems.OnMouseOver(LstItemMouseOver);
		_lstItems.OnMouseOut(LstItemMouseOut);

		_sortName.X = _sortName.X + _txtName.GetTextWidth() + 4;
		_sortName.OnMouseClick(SortNameClick);

		_sortDate.X = _sortDate.X + _txtDate.GetTextWidth() + 4;
		_sortDate.OnMouseClick(SortDateClick);

		_itemOrder = ReserachDiarySort.RESEARCH_DIARY_SORT_NONE;

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);

		// translate only once
		var vec = _game.SavedGame.ResearchDiary;
		int sortOrder = vec.Count;
		for (int i = vec.Count - 1; i >= 0; --i)
		{
			var researchDiaryEntry = vec[i];

			string translation = Tr(researchDiaryEntry.research.Name);
			string upper = Unicode.UpperCase(translation);

			_itemList.Add(new TranslatedResearchDiaryItem(researchDiaryEntry, translation, upper, FormatDate(researchDiaryEntry), sortOrder));
			sortOrder--;
		}

		_txtTooltip.VerticalAlign = TextVAlign.ALIGN_MIDDLE;
		_txtTooltip.SetWordWrap(true);
	}

	/// <summary>
	/// Formats the date of a diary entry as YYYY-MM-DD.
	/// </summary>
	internal static string FormatDate(ResearchDiaryEntry entry)
	{
		StringBuilder ss3 = new StringBuilder();
		ss3.Append(entry.year.ToString(CultureInfo.InvariantCulture)).Append('-');
		if (entry.month < 10) ss3.Append('0');
		ss3.Append(entry.month.ToString(CultureInfo.InvariantCulture)).Append('-');
		if (entry.day < 10) ss3.Append('0');
		ss3.Append(entry.day.ToString(CultureInfo.InvariantCulture));
		return ss3.ToString();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Quick search toggle.
	/// </summary>
	public void BtnQuickSearchToggle(Action action)
	{
		if (_btnQuickSearch.Visible)
		{
			_btnQuickSearch.Text = "";
			_btnQuickSearch.Visible = false;
			BtnQuickSearchApply(action);
		}
		else
		{
			_btnQuickSearch.Visible = true;
			_btnQuickSearch.SetFocus(true);
		}
	}

	/// <summary>
	/// Quick search.
	/// </summary>
	public void BtnQuickSearchApply(Action action)
	{
		if (!Options.oxceInstantQuickSearch && action != null && action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			SDLKey sym = action.Details.key.keysym.sym;
			if (sym != SDLKey.SDLK_RETURN && sym != SDLKey.SDLK_KP_ENTER && sym != SDLKey.SDLK_ESCAPE)
				return;
		}
		InitList();
	}

	/// <summary>
	/// Reloads the item list.
	/// </summary>
	private void InitList()
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		// clear everything
		_filteredItemList.Clear();

		// find relevant items
		FilterItems(_itemList, searchString, _filteredItemList);

		SortList();
	}

	/// <summary>
	/// Adds the items matching the (upper-cased) quick search text by name or date to the filtered list.
	/// </summary>
	internal static void FilterItems(List<TranslatedResearchDiaryItem> itemList, string searchString, List<TranslatedResearchDiaryItem> filtered)
	{
		foreach (var origItem in itemList)
		{
			// quick search
			if (!string.IsNullOrEmpty(searchString))
			{
				if (!origItem.upperName.Contains(searchString, StringComparison.Ordinal) && !origItem.date.Contains(searchString, StringComparison.Ordinal))
				{
					continue;
				}
			}

			filtered.Add(origItem);
		}
	}

	/// <summary>
	/// Refreshes the item list.
	/// </summary>
	public override void Init()
	{
		base.Init();

		// coming back from TechTreeViewer/Ufopedia
		if (_doNotReset)
		{
			_doNotReset = false;
			return;
		}

		InitList();
	}

	/// <summary>
	/// Updates the sorting arrows based on the current setting.
	/// </summary>
	private void UpdateArrows()
	{
		_sortName.SetShape(ArrowShape.ARROW_NONE);
		_sortDate.SetShape(ArrowShape.ARROW_NONE);
		switch (_itemOrder)
		{
			case ReserachDiarySort.RESEARCH_DIARY_SORT_NONE:
				_sortDate.SetShape(ArrowShape.ARROW_SMALL_DOWN); // it is already sorted by date in the original vector
				break;
			case ReserachDiarySort.RESEARCH_DIARY_SORT_NAME_ASC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case ReserachDiarySort.RESEARCH_DIARY_SORT_NAME_DESC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			case ReserachDiarySort.RESEARCH_DIARY_SORT_DATE_ASC:
				_sortDate.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case ReserachDiarySort.RESEARCH_DIARY_SORT_DATE_DESC:
				_sortDate.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// Sorts the list.
	/// </summary>
	public void SortList()
	{
		UpdateArrows();

		SortItems(_filteredItemList, _itemOrder);

		UpdateList();
	}

	/// <summary>
	/// Sorts the items like the C++ std::stable_sort calls (descending = stable sort over reverse iterators).
	/// </summary>
	internal static void SortItems(List<TranslatedResearchDiaryItem> items, ReserachDiarySort order)
	{
		static bool compareItemName(TranslatedResearchDiaryItem a, TranslatedResearchDiaryItem b) => Unicode.NaturalCompare(a.name, b.name);
		static bool compareItemSortOrder(TranslatedResearchDiaryItem a, TranslatedResearchDiaryItem b) => a.sortOrder < b.sortOrder;

		switch (order)
		{
			case ReserachDiarySort.RESEARCH_DIARY_SORT_NONE:
				break;
			case ReserachDiarySort.RESEARCH_DIARY_SORT_NAME_ASC:
				StableSort(items, compareItemName);
				break;
			case ReserachDiarySort.RESEARCH_DIARY_SORT_NAME_DESC:
				items.Reverse();
				StableSort(items, compareItemName);
				items.Reverse();
				break;
			case ReserachDiarySort.RESEARCH_DIARY_SORT_DATE_ASC:
				StableSort(items, compareItemSortOrder);
				break;
			case ReserachDiarySort.RESEARCH_DIARY_SORT_DATE_DESC:
				items.Reverse();
				StableSort(items, compareItemSortOrder);
				items.Reverse();
				break;
		}
	}

	/// <summary>std::stable_sort with a "less" comparator.</summary>
	private static void StableSort<T>(List<T> items, Func<T, T, bool> less)
	{
		var sorted = items.OrderBy(x => x, Comparer<T>.Create((a, b) => less(a, b) ? -1 : less(b, a) ? 1 : 0)).ToList();
		items.Clear();
		items.AddRange(sorted);
	}

	/// <summary>
	/// Updates the list with the current diary items.
	/// </summary>
	public virtual void UpdateList()
	{
		_lstItems.ClearList();

		foreach (var item in _filteredItemList)
		{
			string ss2 = ((int)item.diaryEntry.source.type).ToString(CultureInfo.InvariantCulture);
			_lstItems.AddRow(3, item.name, ss2, item.date);
		}
	}

	/// <summary>
	/// Sorts the items by name.
	/// </summary>
	public void SortNameClick(Action action)
	{
		_itemOrder = _itemOrder == ReserachDiarySort.RESEARCH_DIARY_SORT_NAME_ASC ? ReserachDiarySort.RESEARCH_DIARY_SORT_NAME_DESC : ReserachDiarySort.RESEARCH_DIARY_SORT_NAME_ASC;
		SortList();
	}

	/// <summary>
	/// Sorts the items by date.
	/// </summary>
	public void SortDateClick(Action action)
	{
		_itemOrder = _itemOrder == ReserachDiarySort.RESEARCH_DIARY_SORT_DATE_ASC ? ReserachDiarySort.RESEARCH_DIARY_SORT_DATE_DESC : ReserachDiarySort.RESEARCH_DIARY_SORT_DATE_ASC;
		SortList();
	}

	/// <summary>
	/// Handles mouse L-clicks.
	/// </summary>
	public void LstItemLClick(Action action)
	{
		var selectedTopic = _filteredItemList[(int)_lstItems.SelectedRow].diaryEntry.research;
		_doNotReset = true;
		_game.PushState(new TechTreeViewerState(selectedTopic, null));
	}

	/// <summary>
	/// Handles mouse M-clicks.
	/// </summary>
	public void LstItemMClick(Action action)
	{
		var selectedTopic = _filteredItemList[(int)_lstItems.SelectedRow].diaryEntry.research;
		_doNotReset = true;
		Ufopaedia.OpenArticle(_game, selectedTopic.Name);
	}

	/// <summary>
	/// Shows where the topic under the mouse came from.
	/// </summary>
	public void LstItemMouseOver(Action action)
	{
		uint sel = _lstItems.SelectedRow;
		if (sel < _filteredItemList.Count)
		{
			var item = _filteredItemList[(int)sel];

			string sourceNameTranslated = "";
			if (item.diaryEntry.source.type != DiscoverySourceType.BASE) sourceNameTranslated = Tr(item.diaryEntry.source.name);

			string descTemplate = item.diaryEntry.source.TypeString;
			string desc = Tr(descTemplate).Arg(item.diaryEntry.research.Name).Arg(item.diaryEntry.source.name).Arg(sourceNameTranslated);

			_txtTooltip.SetText(desc);
		}
		else
		{
			_txtTooltip.SetText("");
		}
	}

	/// <summary>
	/// Clears the tooltip.
	/// </summary>
	public void LstItemMouseOut(Action action)
	{
		_txtTooltip.SetText("");
	}
}
