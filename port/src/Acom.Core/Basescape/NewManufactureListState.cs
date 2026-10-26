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
// Ported from: src/Basescape/NewManufactureListState.h, src/Basescape/NewManufactureListState.cpp
#nullable disable

using System.Globalization;
using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen which list possible productions.
/// </summary>
public class NewManufactureListState : TouchState
{
	private readonly Base _base;
	private bool _showRequirements, _refreshCategories, _doInit;
	private readonly TextButton _btnOk;
	private readonly ToggleTextButton _btnShowOnlyNew;
	private readonly TextEdit _btnQuickSearch;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtItem, _txtCategory;
	private readonly TextList _lstManufacture;
	private int _lstScroll;
	private readonly ComboBox _cbxCategory;
	private readonly ComboBox _cbxFilter;
	private readonly List<RuleManufacture> _possibleProductions = [];
	private readonly List<string> _catStrings = [];
	private readonly List<string> _displayedStrings = [];
	private readonly byte _colorNormal, _colorNew;
	private readonly byte _colorHidden, _colorFacilityRequired;

	/// <summary>
	/// Initializes all the elements in the productions list screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	public NewManufactureListState(Base @base)
	{
		_base = @base;
		_showRequirements = false;
		_refreshCategories = true;
		_doInit = true;
		_lstScroll = 0;

		_screen = false;

		_window = new Window(this, 320, 156, 0, 22, WindowPopup.POPUP_BOTH);
		_btnQuickSearch = new TextEdit(this, 48, 9, 10, 35);
		_btnOk = new TextButton(148, 16, 164, 154);
		_btnShowOnlyNew = new ToggleTextButton(148, 16, 8, 154);
		_txtTitle = new Text(320, 17, 0, 30);
		_txtItem = new Text(156, 9, 10, 62);
		_txtCategory = new Text(130, 9, 166, 62);
		_lstManufacture = new TextList(288, 80, 8, 70);
		_cbxFilter = new ComboBox(this, 146, 16, 10, 46);
		_cbxCategory = new ComboBox(this, 146, 16, 166, 46);

		TouchComponentsCreate(_txtTitle, true, -1, +22);

		// Set palette
		SetInterface("selectNewManufacture");

		Add(_window, "window", "selectNewManufacture");
		Add(_btnQuickSearch, "button", "selectNewManufacture");
		Add(_btnOk, "button", "selectNewManufacture");
		Add(_btnShowOnlyNew, "button", "selectNewManufacture");
		Add(_txtTitle, "text", "selectNewManufacture");
		Add(_txtItem, "text", "selectNewManufacture");
		Add(_txtCategory, "text", "selectNewManufacture");
		Add(_lstManufacture, "list", "selectNewManufacture");
		Add(_cbxFilter, "catBox", "selectNewManufacture");
		Add(_cbxCategory, "catBox", "selectNewManufacture");

		TouchComponentsAdd("button2", "selectNewManufacture", _window);

		_colorNormal = _lstManufacture.Color;
		_colorNew = Options.oxceHighlightNewTopics ? _lstManufacture.SecondaryColor : _colorNormal;
		_colorHidden = unchecked((byte)_game.Mod.GetInterface("selectNewManufacture").GetElement("listExtended").color);
		_colorFacilityRequired = unchecked((byte)_game.Mod.GetInterface("selectNewManufacture").GetElement("listExtended").color2);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "selectNewManufacture");

		TouchComponentsConfigure();

		_txtTitle.SetText(Tr("STR_PRODUCTION_ITEMS"));
		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;

		_txtItem.SetText(Tr("STR_ITEM"));

		_txtCategory.SetText(Tr("STR_CATEGORY"));

		_lstManufacture.SetColumns(3, 156, 120, 10);
		_lstManufacture.SetSelectable(true);
		_lstManufacture.SetBackground(_window);
		_lstManufacture.Margin = 2;
		_lstManufacture.OnMouseClick(LstProdClick, SDLMouse.SDL_BUTTON_LEFT);
		_lstManufacture.OnMouseClick(LstProdClick, SDLMouse.SDL_BUTTON_RIGHT);
		_lstManufacture.OnMouseClick(LstProdClick, SDLMouse.SDL_BUTTON_MIDDLE);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnMarkAllAsSeenClick, Options.keyMarkAllAsSeen);

		_btnShowOnlyNew.Text = Tr("STR_SHOW_ONLY_NEW");
		_btnShowOnlyNew.OnMouseClick(BtnShowOnlyNewClick);

		List<string> filterOptions = [];
		filterOptions.Add("STR_FILTER_DEFAULT");
		filterOptions.Add("STR_FILTER_DEFAULT_SUPPLIES_OK");
		filterOptions.Add("STR_FILTER_DEFAULT_NO_SUPPLIES");
		filterOptions.Add("STR_FILTER_FACILITY_REQUIRED");
		filterOptions.Add("STR_FILTER_HIDDEN");
		_cbxFilter.SetOptions(filterOptions, true);
		if (Options.oxceManufactureFilterSuppliesOK)
		{
			_cbxFilter.Selected = (int)ManufacturingFilterType.MANU_FILTER_DEFAULT_SUPPLIES_OK;
		}
		_cbxFilter.OnChange(CbxFilterChange);

		_catStrings.Add("STR_ALL_ITEMS");
		_cbxCategory.SetOptions(_catStrings, true);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);
	}

	/// <summary>
	/// Initializes state (fills list of possible productions).
	/// </summary>
	public override void Init()
	{
		base.Init();

		if (_doInit)
		{
			FillProductionList(_refreshCategories);
		}
		_doInit = true;
		_refreshCategories = true;

		TouchComponentsRefresh();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// LRM-click routing.
	/// </summary>
	public void LstProdClick(Action action)
	{
		if (_game.IsLeftClick(action, true))
		{
			LstProdClickLeft(action);
		}
		else if (_game.IsRightClick(action, true))
		{
			LstProdClickRight(action);
		}
		else if (_game.IsMiddleClick(action, true))
		{
			LstProdClickMiddle(action);
		}
	}

	/// <summary>
	/// Opens the Production settings screen.
	/// </summary>
	public void LstProdClickLeft(Action action)
	{
		_lstScroll = _lstManufacture.Scroll;

		ManufacturingFilterType basicFilter = (ManufacturingFilterType)_cbxFilter.Selected;
		if (basicFilter == ManufacturingFilterType.MANU_FILTER_FACILITY_REQUIRED)
			return;

		RuleManufacture rule = _game.Mod.GetManufacture(_displayedStrings[(int)_lstManufacture.SelectedRow]);

		// check and display error messages only further down the chain
		_refreshCategories = false;
		if (_game.IsCtrlPressed() && rule != null && rule.ProducedCraft == null)
		{
			// Ctrl+Click: start 1 item immediately; Ctrl+Shift+Click: start max possible items
			int quantity = 1;
			if (_game.IsShiftPressed())
			{
				ItemContainer itemContainer = _base.StorageItems;
				quantity = int.MaxValue;
				foreach (var iter in rule.RequiredItems)
				{
					quantity = Math.Min(quantity, itemContainer.GetItem(iter.Key) / iter.Value);
				}
			}
			if (quantity > 0)
			{
				Production production = new Production(rule, quantity);
				_base.AddProduction(production);

				if (quantity == int.MaxValue)
					production.InfiniteAmount = true;

				int requiredSpace = Math.Max(1, rule.RequiredSpace);
				int available = Math.Min(Math.Min(1, _base.AvailableEngineers), _base.AvailableWorkshops - requiredSpace);
				production.AssignedEngineers = available;
				_base.Engineers = _base.AvailableEngineers - production.AssignedEngineers;

				production.StartItem(_base, _game.SavedGame, _game.Mod);

				int oldState = _game.SavedGame.GetManufactureRuleStatus(rule.Name);
				if (oldState == RuleManufacture.MANU_STATUS_NEW)
					_game.SavedGame.SetManufactureRuleStatus(rule.Name, RuleManufacture.MANU_STATUS_NORMAL);

				FillProductionList(false);
			}
		}
		else
		{
			_game.PushState(new ManufactureStartState(_base, rule));
		}
	}

	/// <summary>
	/// Changes the status (new -> normal -> hidden -> new).
	/// </summary>
	public void LstProdClickRight(Action action)
	{
		ManufacturingFilterType basicFilter = (ManufacturingFilterType)_cbxFilter.Selected;
		if (basicFilter == ManufacturingFilterType.MANU_FILTER_FACILITY_REQUIRED)
		{
			// display either category or requirements
			_showRequirements = !_showRequirements;
			RuleBaseFacilityFunctions baseFunc = _base.GetProvidedBaseFunc(default);

			for (int row = 0; row < _lstManufacture.Texts; ++row)
			{
				RuleManufacture info = _game.Mod.GetManufacture(_displayedStrings[row]);
				if (info != null)
				{
					if (_showRequirements)
					{
						StringBuilder ss = new StringBuilder();
						int count = 0;
						List<string> missed = _game.Mod.GetBaseFunctionNames(~baseFunc & info.RequireBaseFunc);
						foreach (var name in missed)
						{
							if (count > 0)
							{
								ss.Append(", ");
							}
							ss.Append((string)Tr(name));
							count++;
						}
						_lstManufacture.SetCellText(row, 1, ss.ToString());
					}
					else
					{
						_lstManufacture.SetCellText(row, 1, Tr(info.Category));
					}
				}
			}
		}
		else
		{
			// change status
			string rule = _displayedStrings[(int)_lstManufacture.SelectedRow];
			int oldState = _game.SavedGame.GetManufactureRuleStatus(rule);
			int newState = NextManufactureStatus(oldState, Options.oxceHighlightNewTopics);
			_game.SavedGame.SetManufactureRuleStatus(rule, newState);

			if (newState == RuleManufacture.MANU_STATUS_HIDDEN)
			{
				_lstManufacture.SetRowColor((int)_lstManufacture.SelectedRow, _colorHidden);
			}
			else if (newState == RuleManufacture.MANU_STATUS_NEW)
			{
				_lstManufacture.SetRowColor((int)_lstManufacture.SelectedRow, _colorNew);
			}
			else
			{
				_lstManufacture.SetRowColor((int)_lstManufacture.SelectedRow, _colorNormal);
			}
		}
	}

	/// <summary>
	/// The status a manufacture topic gets when toggled with the right mouse button.
	/// </summary>
	internal static int NextManufactureStatus(int oldState, bool highlightNewTopics)
	{
		int newState = (oldState + 1) % RuleManufacture.MANU_STATUSES;
		if (!highlightNewTopics)
		{
			// only switch between hidden and not hidden
			newState = (oldState == RuleManufacture.MANU_STATUS_HIDDEN) ? RuleManufacture.MANU_STATUS_NORMAL : RuleManufacture.MANU_STATUS_HIDDEN;
		}
		return newState;
	}

	/// <summary>
	/// Opens the TechTreeViewer for the corresponding topic.
	/// </summary>
	public void LstProdClickMiddle(Action action)
	{
		_doInit = false;

		string articleId = _displayedStrings[(int)_lstManufacture.SelectedRow];
		if (_game.IsCtrlPressed(true))
		{
			Ufopaedia.OpenArticle(_game, articleId);
		}
		else
		{
			RuleManufacture selectedTopic = _game.Mod.GetManufacture(articleId);
			_game.PushState(new TechTreeViewerState(null, selectedTopic));
		}
	}

	/// <summary>
	/// Updates the production list to match the basic filter
	/// </summary>
	public void CbxFilterChange(Action action)
	{
		FillProductionList(true);
	}

	/// <summary>
	/// Updates the production list to match the category filter
	/// </summary>
	public void CbxCategoryChange(Action action)
	{
		FillProductionList(false);
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
		FillProductionList(false);
	}

	/// <summary>
	/// Filter to display only new items.
	/// </summary>
	public void BtnShowOnlyNewClick(Action action)
	{
		FillProductionList(false);
	}

	/// <summary>
	/// Marks all items as seen
	/// </summary>
	public void BtnMarkAllAsSeenClick(Action action)
	{
		ManufacturingFilterType basicFilter = (ManufacturingFilterType)_cbxFilter.Selected;
		if (basicFilter == ManufacturingFilterType.MANU_FILTER_FACILITY_REQUIRED)
			return;

		foreach (var name in _displayedStrings)
		{
			// mark all (new) manufacture items as normal
			if (_game.SavedGame.GetManufactureRuleStatus(name) != RuleManufacture.MANU_STATUS_HIDDEN)
			{
				_game.SavedGame.SetManufactureRuleStatus(name, RuleManufacture.MANU_STATUS_NORMAL);
			}
		}

		FillProductionList(false);
	}

	/// <summary>
	/// Is the production filtered out by the basic filter, the [Show Only New] button or the quick search?
	/// The C++ code does these checks inline in fillProductionList.
	/// </summary>
	internal static bool IsFilteredOut(RuleManufacture manuf, int status, ManufacturingFilterType basicFilter, bool showOnlyNew, string searchString, Func<string, string> tr)
	{
		// filter
		bool isHidden = status == RuleManufacture.MANU_STATUS_HIDDEN;
		if (basicFilter == ManufacturingFilterType.MANU_FILTER_DEFAULT && isHidden)
			return true;
		if (basicFilter == ManufacturingFilterType.MANU_FILTER_DEFAULT_SUPPLIES_OK && isHidden)
			return true;
		if (basicFilter == ManufacturingFilterType.MANU_FILTER_DEFAULT_NO_SUPPLIES && isHidden)
			return true;
		if (basicFilter == ManufacturingFilterType.MANU_FILTER_HIDDEN && !isHidden)
			return true;

		bool isNew = status == RuleManufacture.MANU_STATUS_NEW;
		if (showOnlyNew)
		{
			if (!isNew)
			{
				return true;
			}
		}

		// quick search
		if (!string.IsNullOrEmpty(searchString))
		{
			string projectName = Unicode.UpperCase(tr(manuf.Name));
			if (!projectName.Contains(searchString, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// How many units can be produced with the current funds and stored items (at most 10).
	/// </summary>
	internal static int GetProductionPossible(RuleManufacture manuf, long funds, ItemContainer itemContainer)
	{
		// supplies calculation
		int productionPossible = 10; // max
		if (manuf.ManufactureCost > 0)
		{
			long byFunds = funds / manuf.ManufactureCost;
			if (byFunds < 10L)
			{
				int byFundsInt = (int)byFunds;
				productionPossible = Math.Min(productionPossible, byFundsInt);
			}
		}
		foreach (var iter in manuf.RequiredItems)
		{
			productionPossible = Math.Min(productionPossible, itemContainer.GetItem(iter.Key) / iter.Value);
		}
		return productionPossible;
	}

	/// <summary>
	/// Fills the list of possible productions.
	/// </summary>
	public void FillProductionList(bool refreshCategories)
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		if (refreshCategories)
		{
			_cbxCategory.OnChange(null);
			_cbxCategory.Selected = 0;
		}

		_showRequirements = false;

		_lstManufacture.ClearList();
		_possibleProductions.Clear();
		ManufacturingFilterType basicFilter = (ManufacturingFilterType)_cbxFilter.Selected;
		_game.SavedGame.GetAvailableProductions(_possibleProductions, _game.Mod, _base, basicFilter);
		_displayedStrings.Clear();

		ItemContainer itemContainer = _base.StorageItems;
		int row = 0;
		bool hasUnseen = false;
		foreach (var manuf in _possibleProductions)
		{
			if ((manuf.Category == _catStrings[_cbxCategory.Selected]) || (_catStrings[_cbxCategory.Selected] == "STR_ALL_ITEMS"))
			{
				int status = _game.SavedGame.GetManufactureRuleStatus(manuf.Name);
				if (IsFilteredOut(manuf, status, basicFilter, _btnShowOnlyNew.Pressed, searchString, id => Tr(id)))
					continue;
				bool isHidden = status == RuleManufacture.MANU_STATUS_HIDDEN;
				bool isNew = status == RuleManufacture.MANU_STATUS_NEW;

				int productionPossible = GetProductionPossible(manuf, _game.SavedGame.Funds, itemContainer);
				string ss;
				if (productionPossible <= 0)
				{
					if (basicFilter == ManufacturingFilterType.MANU_FILTER_DEFAULT_SUPPLIES_OK)
						continue;
					ss = "-";
				}
				else
				{
					if (basicFilter == ManufacturingFilterType.MANU_FILTER_DEFAULT_NO_SUPPLIES)
						continue;
					if (productionPossible < 10)
					{
						ss = productionPossible.ToString(CultureInfo.InvariantCulture);
					}
					else
					{
						ss = "+";
					}
				}

				_lstManufacture.AddRow(3, Tr(manuf.Name), Tr(manuf.Category), ss);
				_displayedStrings.Add(manuf.Name);

				// colors
				if (basicFilter == ManufacturingFilterType.MANU_FILTER_FACILITY_REQUIRED)
				{
					_lstManufacture.SetRowColor(row, _colorFacilityRequired);
				}
				else
				{
					if (isHidden)
					{
						_lstManufacture.SetRowColor(row, _colorHidden);
					}
					else if (isNew)
					{
						_lstManufacture.SetRowColor(row, _colorNew);
						hasUnseen = true;
					}
				}
				row++;
			}
		}

		string label = Tr("STR_SHOW_ONLY_NEW");
		_btnShowOnlyNew.Text = (hasUnseen ? "* " : "") + label;
		if (_lstScroll > 0)
		{
			_lstManufacture.ScrollTo(_lstScroll);
			_lstScroll = 0;
		}

		if (refreshCategories)
		{
			_catStrings.Clear();
			_catStrings.Add("STR_ALL_ITEMS");

			for (int r = 0; r < _lstManufacture.Texts; ++r)
			{
				RuleManufacture info = _game.Mod.GetManufacture(_displayedStrings[r]);
				if (info != null)
				{
					bool addCategory = true;
					for (int x = 0; x < _catStrings.Count; ++x)
					{
						if (info.Category == _catStrings[x])
						{
							addCategory = false;
							break;
						}
					}
					if (addCategory)
					{
						_catStrings.Add(info.Category);
					}
				}
			}

			_cbxCategory.SetOptions(_catStrings, true);
			_cbxCategory.OnChange(CbxCategoryChange);
		}
	}
}
