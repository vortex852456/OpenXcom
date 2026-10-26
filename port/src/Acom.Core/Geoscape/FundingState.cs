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
// Ported from: src/Geoscape/FundingState.h, src/Geoscape/FundingState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

public enum FundingCountrySort : uint
{
	FC_NONE = 0,
	FC_NAME_ASC = 1,
	FC_NAME_DESC = 2,
	FC_FUNDING_ASC = 3,
	FC_FUNDING_DESC = 4,
	FC_CHANGE_ASC = 5,
	FC_CHANGE_DESC = 6,
}

/// <summary>A row of the funding screen.</summary>
public class FundingCountry
{
#pragma warning disable CA1051, IDE1006 // C++ struct fields
	public string name;
	public int funding;
	public int change;
#pragma warning restore CA1051, IDE1006

	public FundingCountry(string _name, int _funding, int _change)
	{
		name = _name;
		funding = _funding;
		change = _change;
	}
}

/// <summary>
/// Funding screen accessible from the Geoscape
/// that shows all the countries' funding.
/// </summary>
public class FundingState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtCountry, _txtFunding, _txtChange;
	private readonly TextList _lstCountries;
	private readonly ArrowButton _sortName, _sortFunding, _sortChange;

	private readonly List<FundingCountry> _fundingCountryList = [];
	private FundingCountrySort _fundingCountryOrder;

	/// <summary>The rows in their current order.</summary>
	internal List<FundingCountry> FundingCountryList => _fundingCountryList;

	/// <summary>
	/// Initializes all the elements in the Funding screen.
	/// </summary>
	public FundingState()
	{
		_screen = false;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0, WindowPopup.POPUP_BOTH);
		_btnOk = new TextButton(50, 12, 135, 180);
		_txtTitle = new Text(320, 17, 0, 8);
		_txtCountry = new Text(100, 9, 32, 30);
		_txtFunding = new Text(100, 9, 140, 30);
		_txtChange = new Text(72, 9, 240, 30);
		_lstCountries = new TextList(260, 136, 32, 40);
		_sortName = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 32, 30);
		_sortFunding = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 140, 30);
		_sortChange = new ArrowButton(ArrowShape.ARROW_NONE, 11, 8, 240, 30);

		// Set palette
		SetInterface("fundingWindow");

		Add(_window, "window", "fundingWindow");
		Add(_btnOk, "button", "fundingWindow");
		Add(_txtTitle, "text1", "fundingWindow");
		Add(_txtCountry, "text2", "fundingWindow");
		Add(_txtFunding, "text2", "fundingWindow");
		Add(_txtChange, "text2", "fundingWindow");
		Add(_lstCountries, "list", "fundingWindow");
		Add(_sortName, "text2", "fundingWindow");
		Add(_sortFunding, "text2", "fundingWindow");
		Add(_sortChange, "text2", "fundingWindow");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "fundingWindow");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyGeoFunding);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_INTERNATIONAL_RELATIONS"));

		_txtCountry.SetText(Tr("STR_COUNTRY"));

		_txtFunding.SetText(Tr("STR_FUNDING"));

		_txtChange.SetText(Tr("STR_CHANGE"));

		_lstCountries.SetColumns(3, 108, 100, 52);
		_lstCountries.SetDot(true);

		_sortName.X = _sortName.X + _txtCountry.GetTextWidth() + 4;
		_sortName.OnMouseClick(SortNameClick);

		_sortFunding.X = _sortFunding.X + _txtFunding.GetTextWidth() + 4;
		_sortFunding.OnMouseClick(SortFundingClick);

		_sortChange.X = _sortChange.X + _txtChange.GetTextWidth() + 4;
		_sortChange.OnMouseClick(SortChangeClick);

		_fundingCountryOrder = FundingCountrySort.FC_NONE;

		foreach (var country in _game.SavedGame.Countries)
		{
			_fundingCountryList.Add(new FundingCountry(
				Tr(country.Rules.Type),
				country.Funding[^1],
				country.Funding.Count > 1 ? country.Funding[^1] - country.Funding[country.Funding.Count - 2] : 0));
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Refreshes the funding countries list.
	/// </summary>
	public override void Init()
	{
		base.Init();

		SortList();
	}

	/// <summary>
	/// Updates the sorting arrows based on the current setting.
	/// </summary>
	private void UpdateArrows()
	{
		_sortName.SetShape(ArrowShape.ARROW_NONE);
		_sortFunding.SetShape(ArrowShape.ARROW_NONE);
		_sortChange.SetShape(ArrowShape.ARROW_NONE);
		switch (_fundingCountryOrder)
		{
			case FundingCountrySort.FC_NONE:
				break;
			case FundingCountrySort.FC_NAME_ASC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case FundingCountrySort.FC_NAME_DESC:
				_sortName.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			case FundingCountrySort.FC_FUNDING_ASC:
				_sortFunding.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case FundingCountrySort.FC_FUNDING_DESC:
				_sortFunding.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			case FundingCountrySort.FC_CHANGE_ASC:
				_sortChange.SetShape(ArrowShape.ARROW_SMALL_UP);
				break;
			case FundingCountrySort.FC_CHANGE_DESC:
				_sortChange.SetShape(ArrowShape.ARROW_SMALL_DOWN);
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// std::stable_sort over the list (or, descending, over its reverse iterators) with a "less" predicate.
	/// </summary>
	internal static void StableSort(List<FundingCountry> list, Func<FundingCountry, FundingCountry, bool> less, bool reverse)
	{
		if (reverse)
			list.Reverse();
		var comparer = Comparer<FundingCountry>.Create((a, b) => less(a, b) ? -1 : less(b, a) ? 1 : 0);
		var sorted = list.OrderBy(c => c, comparer).ToList(); // OrderBy is stable
		list.Clear();
		list.AddRange(sorted);
		if (reverse)
			list.Reverse();
	}

	/// <summary>
	/// Sorts the funding countries list.
	/// </summary>
	public void SortList()
	{
		UpdateArrows();

		static bool compareName(FundingCountry a, FundingCountry b) => Unicode.NaturalCompare(a.name, b.name);
		static bool compareFunding(FundingCountry a, FundingCountry b) => a.funding < b.funding;
		static bool compareChange(FundingCountry a, FundingCountry b) => a.change < b.change;

		switch (_fundingCountryOrder)
		{
			case FundingCountrySort.FC_NONE:
				break;
			case FundingCountrySort.FC_NAME_ASC:
				StableSort(_fundingCountryList, compareName, false);
				break;
			case FundingCountrySort.FC_NAME_DESC:
				StableSort(_fundingCountryList, compareName, true);
				break;
			case FundingCountrySort.FC_FUNDING_ASC:
				StableSort(_fundingCountryList, compareFunding, false);
				break;
			case FundingCountrySort.FC_FUNDING_DESC:
				StableSort(_fundingCountryList, compareFunding, true);
				break;
			case FundingCountrySort.FC_CHANGE_ASC:
				StableSort(_fundingCountryList, compareChange, false);
				break;
			case FundingCountrySort.FC_CHANGE_DESC:
				StableSort(_fundingCountryList, compareChange, true);
				break;
		}

		UpdateList();
	}

	/// <summary>
	/// Updates the funding countries list.
	/// </summary>
	public virtual void UpdateList()
	{
		_lstCountries.ClearList();
		foreach (var country in _fundingCountryList)
		{
			string ss = Unicode.TOK_COLOR_FLIP + Unicode.FormatFunding(country.funding) + Unicode.TOK_COLOR_FLIP;

			string ss2 = "";
			if (country.change != 0) ss2 += Unicode.TOK_COLOR_FLIP;
			if (country.change > 0) ss2 += '+';
			ss2 += Unicode.FormatFunding(country.change);
			if (country.change != 0) ss2 += Unicode.TOK_COLOR_FLIP;

			_lstCountries.AddRow(3, country.name, ss, ss2);
		}
		_lstCountries.AddRow(2, Tr("STR_TOTAL_UC"), Unicode.FormatFunding(_game.SavedGame.CountryFunding));
		_lstCountries.SetRowColor(_game.SavedGame.Countries.Count, _txtCountry.Color);
	}

	/// <summary>
	/// Sorts the funding countries by name.
	/// </summary>
	public void SortNameClick(Action action)
	{
		_fundingCountryOrder = _fundingCountryOrder == FundingCountrySort.FC_NAME_ASC ? FundingCountrySort.FC_NAME_DESC : FundingCountrySort.FC_NAME_ASC;
		SortList();
	}

	/// <summary>
	/// Sorts the funding countries by funding.
	/// </summary>
	public void SortFundingClick(Action action)
	{
		_fundingCountryOrder = _fundingCountryOrder == FundingCountrySort.FC_FUNDING_ASC ? FundingCountrySort.FC_FUNDING_DESC : FundingCountrySort.FC_FUNDING_ASC;
		SortList();
	}

	/// <summary>
	/// Sorts the funding countries by funding change.
	/// </summary>
	public void SortChangeClick(Action action)
	{
		_fundingCountryOrder = _fundingCountryOrder == FundingCountrySort.FC_CHANGE_ASC ? FundingCountrySort.FC_CHANGE_DESC : FundingCountrySort.FC_CHANGE_ASC;
		SortList();
	}
}
