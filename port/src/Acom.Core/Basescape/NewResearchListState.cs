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
// Ported from: src/Basescape/NewResearchListState.h, src/Basescape/NewResearchListState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which displays possible research projects.
/// </summary>
public class NewResearchListState : TouchState
{
	private readonly Base _base;
	private readonly bool _sortByCost;
	private readonly TextButton _btnOK;
	private readonly ComboBox _cbxSort;
	private readonly ToggleTextButton _btnShowOnlyNew;
	private readonly TextEdit _btnQuickSearch;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstResearch;
	private int _lstScroll;
	private readonly byte _colorNormal, _colorNew, _colorHidden;
	private readonly bool _isSortingEnabled;
	private readonly List<RuleResearch> _projects = [];

	/// <summary>
	/// Initializes all the elements in the Research list screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="sortByCost">Should the list be sorted by cost or listOrder?</param>
	public NewResearchListState(Base @base, bool sortByCost)
	{
		_base = @base;
		_sortByCost = sortByCost;
		_lstScroll = 0;

		if (Options.IsPasswordCorrect())
		{
			_sortByCost = !_sortByCost;
		}

		_screen = false;

		_window = new Window(this, 230, 140, 45, 30, WindowPopup.POPUP_BOTH);
		_btnQuickSearch = new TextEdit(this, 48, 9, 53, 38);
		_btnOK = new TextButton(103, 16, 164, 146);
		_cbxSort = new ComboBox(this, 103, 16, 53, 146, true);
		_btnShowOnlyNew = new ToggleTextButton(103, 16, 53, 146);
		_txtTitle = new Text(214, 16, 53, 38);
		_lstResearch = new TextList(198, 88, 53, 54);

		TouchComponentsCreate(_txtTitle, true, -45, +30);

		// Set palette
		SetInterface("selectNewResearch");

		Add(_window, "window", "selectNewResearch");
		Add(_btnQuickSearch, "button", "selectNewResearch");
		Add(_btnOK, "button", "selectNewResearch");
		Add(_btnShowOnlyNew, "button", "selectNewResearch");
		Add(_txtTitle, "text", "selectNewResearch");
		Add(_lstResearch, "list", "selectNewResearch");
		Add(_cbxSort, "button", "selectNewResearch");

		TouchComponentsAdd("button2", "selectNewResearch", _window);

		_colorNormal = _lstResearch.Color;
		_colorNew = Options.oxceHighlightNewTopics ? _lstResearch.SecondaryColor : _colorNormal;
		_colorHidden = unchecked((byte)_game.Mod.GetInterface("selectNewResearch").GetElement("listExtended").color);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "selectNewResearch");

		TouchComponentsConfigure();

		_btnOK.Text = Tr("STR_OK");
		_btnOK.OnMouseClick(BtnOKClick);
		_btnOK.OnKeyboardPress(BtnOKClick, Options.keyCancel);
		_btnOK.OnKeyboardPress(BtnMarkAllAsSeenClick, Options.keyMarkAllAsSeen);

		_isSortingEnabled = _game.Mod.EnableNewResearchSorting;
		if (_isSortingEnabled)
		{
			_btnShowOnlyNew.Visible = false;
			List<string> sortOptions = [];
			sortOptions.Add("STR_SORT_DEFAULT");
			sortOptions.Add("STR_SORT_BY_COST");
			sortOptions.Add("STR_SORT_BY_NAME");
			sortOptions.Add("STR_SHOW_ONLY_NEW"); // this is a filter, replacement for the hidden "Show Only New" toggle button
			sortOptions.Add("STR_FILTER_HIDDEN"); // this is a filter
			_cbxSort.SetOptions(sortOptions, true);
			if (_sortByCost)
			{
				_cbxSort.Selected = 1;
			}
			_cbxSort.OnChange(CbxSortChange);
		}
		else
		{
			_cbxSort.Visible = false;
			_btnShowOnlyNew.Text = Tr("STR_SHOW_ONLY_NEW");
			_btnShowOnlyNew.OnMouseClick(BtnShowOnlyNewClick);
		}

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_NEW_RESEARCH_PROJECTS"));

		_lstResearch.SetColumns(1, 190);
		_lstResearch.SetSelectable(true);
		_lstResearch.SetBackground(_window);
		_lstResearch.Margin = 8;
		_lstResearch.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstResearch.OnMouseClick(OnClick, SDLMouse.SDL_BUTTON_LEFT);
		_lstResearch.OnMouseClick(OnClick, SDLMouse.SDL_BUTTON_RIGHT);
		_lstResearch.OnMouseClick(OnClick, SDLMouse.SDL_BUTTON_MIDDLE);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOK.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);
	}

	/// <summary>
	/// Initializes the screen (fills the list).
	/// </summary>
	public override void Init()
	{
		base.Init();
		FillProjectList(false);

		TouchComponentsRefresh();
	}

	/// <summary>
	/// LRM-click routing.
	/// </summary>
	private void OnClick(Action action)
	{
		if (_game.IsLeftClick(action, true))
		{
			OnSelectProject(action);
		}
		else if (_game.IsRightClick(action, true))
		{
			OnToggleProjectStatus(action);
		}
		else if (_game.IsMiddleClick(action, true))
		{
			OnOpenTechTreeViewer(action);
		}
	}

	/// <summary>
	/// Selects the RuleResearch to work on.
	/// </summary>
	private void OnSelectProject(Action action)
	{
		_lstScroll = _lstResearch.Scroll;
		_game.PushState(new ResearchInfoState(_base, _projects[(int)_lstResearch.SelectedRow]));
	}

	/// <summary>
	/// Toggles the status (new/normal/hidden) of the selected RuleResearch.
	/// </summary>
	private void OnToggleProjectStatus(Action action)
	{
		if (!Options.oxceHighlightNewTopics && !_isSortingEnabled)
		{
			// there are no statuses to toggle
			return;
		}

		// change status
		string rule = _projects[(int)_lstResearch.SelectedRow].Name;
		int oldState = _game.SavedGame.GetResearchRuleStatus(rule);
		int newState = NextResearchStatus(oldState, Options.oxceHighlightNewTopics, _isSortingEnabled);

		_game.SavedGame.SetResearchRuleStatus(rule, newState);

		if (newState == RuleResearch.RESEARCH_STATUS_HIDDEN)
		{
			_lstResearch.SetRowColor((int)_lstResearch.SelectedRow, _colorHidden);
		}
		else if (newState == RuleResearch.RESEARCH_STATUS_NEW)
		{
			_lstResearch.SetRowColor((int)_lstResearch.SelectedRow, _colorNew);
		}
		else
		{
			_lstResearch.SetRowColor((int)_lstResearch.SelectedRow, _colorNormal);
		}
	}

	/// <summary>
	/// The status a research topic gets when toggled with the right mouse button.
	/// </summary>
	internal static int NextResearchStatus(int oldState, bool highlightNewTopics, bool isSortingEnabled)
	{
		int newState = RuleResearch.RESEARCH_STATUS_NEW;

		if (oldState == RuleResearch.RESEARCH_STATUS_NEW)
		{
			newState = highlightNewTopics ? RuleResearch.RESEARCH_STATUS_NORMAL : RuleResearch.RESEARCH_STATUS_HIDDEN;
		}
		else if (oldState == RuleResearch.RESEARCH_STATUS_NORMAL)
		{
			newState = isSortingEnabled ? RuleResearch.RESEARCH_STATUS_HIDDEN : RuleResearch.RESEARCH_STATUS_NEW;
		}
		return newState;
	}

	/// <summary>
	/// Opens the TechTreeViewer for the corresponding topic.
	/// </summary>
	private void OnOpenTechTreeViewer(Action action)
	{
		_lstScroll = _lstResearch.Scroll;
		RuleResearch selectedTopic = _projects[(int)_lstResearch.SelectedRow];
		_game.PushState(new TechTreeViewerState(selectedTopic, null));
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOKClick(Action action)
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
		FillProjectList(false);
	}

	/// <summary>
	/// Updates the research list based on the selected option.
	/// </summary>
	public void CbxSortChange(Action action)
	{
		FillProjectList(false);
	}

	/// <summary>
	/// Filter to display only new items.
	/// </summary>
	public void BtnShowOnlyNewClick(Action action)
	{
		FillProjectList(false);
	}

	/// <summary>
	/// Marks all items as seen
	/// </summary>
	public void BtnMarkAllAsSeenClick(Action action)
	{
		FillProjectList(true);
	}

	/// <summary>
	/// Sorts the available projects like the C++ fillProjectList (std::sort, so equal keys keep the libstdc++ order).
	/// </summary>
	internal static void SortProjects(List<RuleResearch> projects, int selectedSort, bool sortByCost, Func<string, string> tr)
	{
		if (selectedSort == 1 || (selectedSort == 3 && sortByCost))
		{
			StdAlgorithm.Sort(projects, (a, b) => a.Cost < b.Cost);
		}
		else if (selectedSort == 2)
		{
			StdAlgorithm.Sort(projects, (a, b) => Unicode.NaturalCompare(tr(a.Name), tr(b.Name)));
		}
		else
		{
			// sort by list order
			StdAlgorithm.Sort(projects, (a, b) => a.ListOrder < b.ListOrder);
		}
	}

	/// <summary>
	/// Is the research topic filtered out of the list?
	/// The C++ code does this check inline in fillProjectList.
	/// </summary>
	/// <param name="rule">The research topic.</param>
	/// <param name="ruleStatus">Its research status (new/normal/hidden).</param>
	/// <param name="showOnlyNew">Is the [Show Only New] toggle button pressed?</param>
	/// <param name="selectedSort">Index of the sort/filter combo box.</param>
	/// <param name="searchString">Upper-cased quick search text.</param>
	/// <param name="tr">Translation of a string id.</param>
	internal static bool IsFilteredOut(RuleResearch rule, int ruleStatus, bool showOnlyNew, int selectedSort, string searchString, Func<string, string> tr)
	{
		// filter
		if (showOnlyNew || selectedSort == 3)
		{
			if (ruleStatus != RuleResearch.RESEARCH_STATUS_NEW)
			{
				return true;
			}
		}
		else if (selectedSort <= 2)
		{
			if (ruleStatus == RuleResearch.RESEARCH_STATUS_HIDDEN)
			{
				return true;
			}
		}
		else if (selectedSort == 4)
		{
			if (ruleStatus != RuleResearch.RESEARCH_STATUS_HIDDEN)
			{
				return true;
			}
		}

		// quick search
		if (!string.IsNullOrEmpty(searchString))
		{
			string projectName = Unicode.UpperCase(tr(rule.Name));
			if (!projectName.Contains(searchString, StringComparison.Ordinal))
			{
				return true;
			}
		}

		// EXPLANATION
		// -----------
		// Projects with "requires" can only be discovered/researched indirectly
		//  - this is because we can't reliably determine if they are unlocked or not
		// Example:
		//  - Alien Origins + Alien Leader => ALIEN_LEADER_PLUS is discovered
		//  - Alien Leader + Alien Origins => ALIEN_LEADER_PLUS is NOT discovered (you need to research another alien leader/commander)
		// If we wanted to allow also direct research of projects with "requires",
		// we would need to implement a slightly more complicated unlocking algorithm
		// and more importantly, we would need to remember the list of unlocked topics
		// in the save file (currently this is not done, the list is calculated on-the-fly).
		// Summary:
		//  - it would be possible to remove this condition, but more refactoring would be needed
		//  - for now, handling "requires" via zero-cost helpers (e.g. STR_LEADER_PLUS)... is enough
		return rule.Requirements.Count != 0;
	}

	/// <summary>
	/// Fills the list with possible ResearchProjects.
	/// </summary>
	public void FillProjectList(bool markAllAsSeen)
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		_projects.Clear();
		_lstResearch.ClearList();
		// Note: this is the *only* place where this method is called with considerDebugMode = true
		_game.SavedGame.GetAvailableResearchProjects(_projects, _game.Mod, _base, true);
		int selectedSort = _cbxSort.Selected;
		SortProjects(_projects, selectedSort, _sortByCost, id => Tr(id));
		int row = 0;
		bool hasUnseen = false;
		int i = 0;
		while (i < _projects.Count)
		{
			RuleResearch rule = _projects[i];
			int ruleStatus = _game.SavedGame.GetResearchRuleStatus(rule.Name);

			if (IsFilteredOut(rule, ruleStatus, _btnShowOnlyNew.Pressed, selectedSort, searchString, id => Tr(id)))
			{
				_projects.RemoveAt(i);
				continue;
			}

			string ss = $"{(string)Tr(rule.Name)} ({rule.Cost.ToString(CultureInfo.InvariantCulture)})";

			_lstResearch.AddRow(1, ss);
			if (markAllAsSeen)
			{
				// mark all (filtered) research items as normal
				_game.SavedGame.SetResearchRuleStatus(rule.Name, RuleResearch.RESEARCH_STATUS_NORMAL);
			}
			else
			{
				if (ruleStatus == RuleResearch.RESEARCH_STATUS_NEW)
				{
					_lstResearch.SetRowColor(row, _colorNew);
					hasUnseen = true;
				}
				else if (ruleStatus == RuleResearch.RESEARCH_STATUS_HIDDEN)
				{
					_lstResearch.SetRowColor(row, _colorHidden);
				}
			}
			row++;
			++i;
		}

		string label = Tr("STR_SHOW_ONLY_NEW");
		_btnShowOnlyNew.Text = (hasUnseen ? "* " : "") + label;
		if (_lstScroll > 0)
		{
			_lstResearch.ScrollTo(_lstScroll);
			_lstScroll = 0;
		}
	}
}
