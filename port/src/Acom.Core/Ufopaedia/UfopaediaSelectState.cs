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
// Ported from: src/Ufopaedia/UfopaediaSelectState.h, src/Ufopaedia/UfopaediaSelectState.cpp
#nullable disable

using static Acom.SDLKey;

namespace Acom;

/// <summary>
/// UfopaediaSelectState is the screen that lists articles of a given type.
/// </summary>
public class UfopaediaSelectState : State
{
	protected string _section;
	protected Window _window;
	protected TextEdit _btnQuickSearch;
	protected Text _txtTitle;
	protected TextButton _btnOk;
	protected ComboBox _cbxFilter;
	protected TextList _lstSelection;
	protected List<ArticleDefinition> _article_list = [];
	protected List<ArticleDefinition> _filtered_article_list = [];
	protected int _lstScroll;
	protected byte _colorNormal;
	protected byte _colorNew;
	protected byte _colorHidden;
	protected bool _isCommendationsSection;

	public UfopaediaSelectState(string section, int heightOffset, int windowOffset)
	{
		_section = section;
		_lstScroll = 0;
		_isCommendationsSection = (_section == Ufopaedia.UFOPAEDIA_COMMENDATIONS);

		_screen = false;

		// set background window
		_window = new Window(this, 256, 180 + heightOffset, 32, 10 - windowOffset, WindowPopup.POPUP_NONE);
		_window.SetInnerColor(239); // almost black = darkest index from backpals.dat
		_btnQuickSearch = new TextEdit(this, 48, 9, 48, 30 - windowOffset);

		// set title
		_txtTitle = new Text(224, 17, 48, 26 - windowOffset);

		// set buttons
		_btnOk = new TextButton(108, 16, 164, 166 - windowOffset + heightOffset);
		_cbxFilter = new ComboBox(this, 108, 16, 48, 166 - windowOffset + heightOffset, true);
		int listHeightOffset = (heightOffset / 8) * 8; // multiple of 8
		_lstSelection = new TextList(224, 104 + listHeightOffset, 40, 50 - windowOffset);

		// Set palette
		SetInterface("ufopaedia");

		Add(_window, "window", "ufopaedia");
		Add(_btnQuickSearch, "button2", "ufopaedia");
		Add(_txtTitle, "text", "ufopaedia");
		Add(_btnOk, "button2", "ufopaedia");
		Add(_lstSelection, "list", "ufopaedia");
		Add(_cbxFilter, "button2", "ufopaedia");

		_colorNormal = _lstSelection.Color;
		_colorNew = Options.oxceHighlightNewTopics ? _lstSelection.SecondaryColor : _colorNormal;
		_colorHidden = (byte)_game.Mod.GetInterface("ufopaedia").GetElement("listExtended").color;

		CenterAllSurfaces();

		SetWindowBackground(_window, "ufopaedia");

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELECT_ITEM"));

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnMarkAllAsSeenClick, Options.keyMarkAllAsSeen);

		_lstSelection.SetColumns(1, 206);
		_lstSelection.SetSelectable(true);
		_lstSelection.SetBackground(_window);
		_lstSelection.Margin = 18;
		_lstSelection.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstSelection.OnMouseClick(LstSelectionClick, SDLMouse.SDL_BUTTON_LEFT);
		_lstSelection.OnMouseClick(LstSelectionClickRight, SDLMouse.SDL_BUTTON_RIGHT);

		List<string> filterOptions = [];
		filterOptions.Add("STR_FILTER_DEFAULT");
		filterOptions.Add(_isCommendationsSection ? "STR_NOT_AWARDED_YET" : "STR_SHOW_ONLY_NEW");
		filterOptions.Add("STR_FILTER_HIDDEN");
		_cbxFilter.SetOptions(filterOptions, true);
		_cbxFilter.OnChange(CbxFilterChange);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);
	}

	/// <summary>Initializes the state.</summary>
	public override void Init()
	{
		base.Init();
		LoadSelectionList(false);
	}

#pragma warning disable CA1822 // instance handlers in C++ (State._game is static here)
	/// <summary>Returns to the previous screen.</summary>
	protected void BtnOkClick(Action action)
	{
		_game.PopState();
	}
#pragma warning restore CA1822

	/// <summary>Opens the selected article.</summary>
	protected void LstSelectionClick(Action action)
	{
		_lstScroll = _lstSelection.Scroll;
		Ufopaedia.OpenArticle(_game, _filtered_article_list[(int)_lstSelection.SelectedRow]);
	}

	/// <summary>Toggles the topic status.</summary>
	protected void LstSelectionClickRight(Action action)
	{
		// change status
		string rule = _filtered_article_list[(int)_lstSelection.SelectedRow].id;
		int oldState = _game.SavedGame.GetUfopediaRuleStatus(rule);
		int newState = (oldState + 1) % ArticleDefinition.PEDIA_STATUSES;
		if (!Options.oxceHighlightNewTopics)
		{
			// only switch between hidden and not hidden
			newState = (oldState == ArticleDefinition.PEDIA_STATUS_HIDDEN) ? ArticleDefinition.PEDIA_STATUS_NORMAL : ArticleDefinition.PEDIA_STATUS_HIDDEN;
		}
		_game.SavedGame.SetUfopediaRuleStatus(rule, newState);

		if (newState == ArticleDefinition.PEDIA_STATUS_HIDDEN)
		{
			_lstSelection.SetRowColor((int)_lstSelection.SelectedRow, _colorHidden);
		}
		else if (newState == ArticleDefinition.PEDIA_STATUS_NEW)
		{
			_lstSelection.SetRowColor((int)_lstSelection.SelectedRow, _colorNew);
		}
		else
		{
			_lstSelection.SetRowColor((int)_lstSelection.SelectedRow, _colorNormal);
		}
	}

	/// <summary>Quick search toggle.</summary>
	protected void BtnQuickSearchToggle(Action action)
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

	/// <summary>Quick search.</summary>
	protected void BtnQuickSearchApply(Action action)
	{
		if (!Options.oxceInstantQuickSearch && action != null && action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			SDLKey sym = action.Details.key.keysym.sym;
			if (sym != SDLK_RETURN && sym != SDLK_KP_ENTER && sym != SDLK_ESCAPE)
				return;
		}

		LoadSelectionList(false);
	}

	/// <summary>Updates the list to match the filter.</summary>
	protected void CbxFilterChange(Action action)
	{
		LoadSelectionList(false);
	}

	/// <summary>Marks all items as seen.</summary>
	protected void BtnMarkAllAsSeenClick(Action action)
	{
		LoadSelectionList(true);
	}

	protected void LoadSelectionList(bool markAllAsSeen)
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		_lstSelection.ClearList();
		_article_list.Clear();
		Ufopaedia.List(_game.SavedGame, _game.Mod, _section, _article_list);

		_filtered_article_list.Clear();

		int selectedFilter = _cbxFilter.Selected;
		int row = 0;
		bool hasUnseen = false;
		int ruleStatus = 0;
		foreach (var articleDef in _article_list)
		{
			ruleStatus = _game.SavedGame.GetUfopediaRuleStatus(articleDef.id);

			// filter
			if (selectedFilter == 0)
			{
				if (ruleStatus == ArticleDefinition.PEDIA_STATUS_HIDDEN)
				{
					continue;
				}
			}
			else if (selectedFilter == 2)
			{
				if (ruleStatus != ArticleDefinition.PEDIA_STATUS_HIDDEN)
				{
					continue;
				}
			}
			else if (selectedFilter == 1)
			{
				if (_isCommendationsSection)
				{
					if (Ufopaedia.IsAwardedCommendation(_game.SavedGame, articleDef))
					{
						continue;
					}
				}
				else
				{
					if (ruleStatus != ArticleDefinition.PEDIA_STATUS_NEW)
					{
						continue;
					}
				}
			}

			// quick search
			if (!string.IsNullOrEmpty(searchString))
			{
				string projectName = Unicode.UpperCase(Tr(articleDef.MainTitle));
				if (!projectName.Contains(searchString, StringComparison.Ordinal))
				{
					continue;
				}
			}

			_filtered_article_list.Add(articleDef);
			_lstSelection.AddRow(1, Tr(articleDef.MainTitle));

			if (markAllAsSeen)
			{
				// remember all listed articles as seen/normal
				_game.SavedGame.SetUfopediaRuleStatus(articleDef.id, ArticleDefinition.PEDIA_STATUS_NORMAL);
			}
			else
			{
				if (ruleStatus == ArticleDefinition.PEDIA_STATUS_NEW)
				{
					_lstSelection.SetCellColor(row, 0, _colorNew);
					hasUnseen = true;
				}
				else if (ruleStatus == ArticleDefinition.PEDIA_STATUS_HIDDEN)
				{
					_lstSelection.SetCellColor(row, 0, _colorHidden);
				}
			}
			row++;
		}

		if (!_isCommendationsSection)
		{
			string label = Tr("STR_OK");
			_btnOk.Text = (hasUnseen ? "* " : "") + label;
		}

		if (_lstScroll > 0)
		{
			_lstSelection.ScrollTo(_lstScroll);
			_lstScroll = 0;
		}
	}
}
