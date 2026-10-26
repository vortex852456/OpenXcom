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
// Ported from: src/Basescape/TechTreeSelectState.h, src/Basescape/TechTreeSelectState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which allows selecting a topic for the Tech Tree Viewer.
/// </summary>
/// <remarks>
/// Deviation: the "ASCRIPT"/"ESCRIPT"/"MSCRIPT" topic lists are std::unordered_set in C++ and are listed in hash
/// order there; here they keep the order in which the triggers are found.
/// </remarks>
public class TechTreeSelectState : State
{
	private readonly TechTreeViewerState _parent;
	private readonly TextEdit _btnQuickSearch;
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstTopics;
	private readonly List<string> _availableTopics = [];
	private int _firstManufacturingTopicIndex;
	private int _firstFacilitiesTopicIndex;
	private int _firstItemTopicIndex;
	private int _firstCraftTopicIndex;

	/// <summary>
	/// Initializes all the elements on the UI.
	/// </summary>
	/// <param name="parent">Pointer to parent state.</param>
	public TechTreeSelectState(TechTreeViewerState parent)
	{
		_parent = parent;

		_screen = false;

		_window = new Window(this, 230, 140, 45, 32);
		_btnQuickSearch = new TextEdit(this, 198, 9, 53, 52);
		_txtTitle = new Text(182, 9, 53, 42);
		_lstTopics = new TextList(198, 80, 53, 62);
		_btnOk = new TextButton(206, 16, 57, 145);

		// Set palette
		SetInterface("techTreeSelect");

		Add(_window, "window", "techTreeSelect");
		Add(_btnQuickSearch, "button", "techTreeSelect");
		Add(_txtTitle, "text", "techTreeSelect");
		Add(_btnOk, "button", "techTreeSelect");
		Add(_lstTopics, "list", "techTreeSelect");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "techTreeSelect");

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_AVAILABLE_TOPICS"));

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_lstTopics.SetColumns(1, 198);
		_lstTopics.SetSelectable(true);
		_lstTopics.SetBackground(_window);
		_lstTopics.Margin = 0;
		_lstTopics.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstTopics.OnMouseClick(OnSelectTopic);

		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.SetDrawBackground(false);
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = true;
		_btnQuickSearch.SetFocus(true);

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);
	}

	/// <summary>
	/// Initializes the screen (fills the lists).
	/// </summary>
	public override void Init()
	{
		base.Init();
		InitLists();
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
		InitLists();
	}

	/// <summary>
	/// Does the topic pass the quick search? ("SHAZAM" shows all undiscovered topics.)
	/// </summary>
	internal static bool MatchesSearch(string searchString, string translatedName, bool discovered)
	{
		if (searchString == "SHAZAM")
		{
			return !discovered;
		}
		return Unicode.UpperCase(translatedName).Contains(searchString, StringComparison.Ordinal);
	}

	/// <summary>
	/// Shows the filtered topics.
	/// </summary>
	private void InitLists()
	{
		string searchString = Unicode.UpperCase(_btnQuickSearch.Text);

		_firstManufacturingTopicIndex = 0;
		_firstFacilitiesTopicIndex = 0;
		_firstItemTopicIndex = 0;
		_firstCraftTopicIndex = 0;

		_availableTopics.Clear();
		_lstTopics.ClearList();

		if (searchString.Length < 3)
		{
			_lstTopics.AddRow(1, Tr("STR_QS_THREE_LETTERS_A"));
			_lstTopics.AddRow(1, Tr("STR_QS_THREE_LETTERS_B"));
			return;
		}

		int row = 0;
		HashSet<string> tmpList = new(StringComparer.Ordinal);
		List<string> tmpOrder = [];
		void insert(string s)
		{
			if (tmpList.Add(s)) tmpOrder.Add(s);
		}
		if (searchString == "ASCRIPT")
		{
			foreach (var arcScriptId in _game.Mod.ArcScriptList)
			{
				var arcScript = _game.Mod.GetArcScript(arcScriptId, false);
				if (arcScript != null)
				{
					foreach (var trigger in arcScript.ResearchTriggers)
					{
						insert(trigger.Key);
					}
				}
			}
		}
		else if (searchString == "ESCRIPT")
		{
			foreach (var eventScriptId in _game.Mod.EventScriptList)
			{
				var eventScript = _game.Mod.GetEventScript(eventScriptId, false);
				if (eventScript != null)
				{
					foreach (var trigger in eventScript.ResearchTriggers)
					{
						insert(trigger.Key);
					}
				}
			}
		}
		else if (searchString == "MSCRIPT")
		{
			foreach (var missionScriptId in _game.Mod.MissionScriptList)
			{
				var missionScript = _game.Mod.GetMissionScript(missionScriptId, false);
				if (missionScript != null)
				{
					foreach (var trigger in missionScript.ResearchTriggers)
					{
						insert(trigger.Key);
					}
				}
			}
		}
		if (tmpOrder.Count != 0)
		{
			foreach (var tmp in tmpOrder)
			{
				_availableTopics.Add(tmp);
				_lstTopics.AddRow(1, Tr(tmp));
				_lstTopics.SetRowColor(row, _parent.GetResearchColor(tmp));
				++row;
			}
			_firstManufacturingTopicIndex = row;
			_firstFacilitiesTopicIndex = row;
			_firstItemTopicIndex = row;
			_firstCraftTopicIndex = row;
			return;
		}

		foreach (var res in _game.Mod.ResearchList)
		{
			if (!MatchesSearch(searchString, Tr(res), _parent.IsDiscoveredResearch(res)))
			{
				continue;
			}

			_availableTopics.Add(res);
			_lstTopics.AddRow(1, Tr(res));
			_lstTopics.SetRowColor(row, _parent.GetResearchColor(res));
			++row;
		}

		_firstManufacturingTopicIndex = row;

		foreach (var manuf in _game.Mod.ManufactureList)
		{
			if (!MatchesSearch(searchString, Tr(manuf), _parent.IsDiscoveredManufacture(manuf)))
			{
				continue;
			}

			_availableTopics.Add(manuf);
			_lstTopics.AddRow(1, Tr(manuf) + Tr("STR_M_FLAG"));
			if (!_parent.IsDiscoveredManufacture(manuf))
			{
				_lstTopics.SetRowColor(row, _lstTopics.SecondaryColor);
			}
			++row;
		}

		_firstFacilitiesTopicIndex = row;

		foreach (var facType in _game.Mod.BaseFacilitiesList)
		{
			if (!MatchesSearch(searchString, Tr(facType), _parent.IsDiscoveredFacility(facType)))
			{
				continue;
			}

			_availableTopics.Add(facType);
			_lstTopics.AddRow(1, Tr(facType) + Tr("STR_F_FLAG"));
			if (!_parent.IsDiscoveredFacility(facType))
			{
				_lstTopics.SetRowColor(row, _lstTopics.SecondaryColor);
			}
			++row;
		}

		_firstItemTopicIndex = row;

		foreach (var itemType in _game.Mod.ItemsList)
		{
			if (!_parent.IsProtectedItem(itemType))
			{
				// items that are not protected at all are irrelevant for the Tech Tree Viewer!
				continue;
			}
			if (!MatchesSearch(searchString, Tr(itemType), _parent.IsProtectedAndDiscoveredItem(itemType)))
			{
				continue;
			}

			_availableTopics.Add(itemType);
			_lstTopics.AddRow(1, Tr(itemType) + Tr("STR_I_FLAG"));
			if (!_parent.IsProtectedAndDiscoveredItem(itemType))
			{
				_lstTopics.SetRowColor(row, _lstTopics.SecondaryColor);
			}
			++row;
		}

		_firstCraftTopicIndex = row;

		foreach (var craftType in _game.Mod.CraftsList)
		{
			if (!MatchesSearch(searchString, Tr(craftType), _parent.IsDiscoveredCraft(craftType)))
			{
				continue;
			}

			_availableTopics.Add(craftType);
			_lstTopics.AddRow(1, Tr(craftType) + Tr("STR_C_FLAG"));
			if (!_parent.IsDiscoveredCraft(craftType))
			{
				_lstTopics.SetRowColor(row, _lstTopics.SecondaryColor);
			}
			++row;
		}
	}

	/// <summary>
	/// Selects the topic.
	/// </summary>
	private void OnSelectTopic(Action action)
	{
		int index = (int)_lstTopics.SelectedRow;
		if (index >= _availableTopics.Count)
			return;

		string selectedTopic = _availableTopics[index];

		TTVMode topicType = TTVMode.TTV_RESEARCH;
		if (index >= _firstCraftTopicIndex)
		{
			topicType = TTVMode.TTV_CRAFTS;
		}
		else if (index >= _firstItemTopicIndex)
		{
			topicType = TTVMode.TTV_ITEMS;
		}
		else if (index >= _firstFacilitiesTopicIndex)
		{
			topicType = TTVMode.TTV_FACILITIES;
		}
		else if (index >= _firstManufacturingTopicIndex)
		{
			topicType = TTVMode.TTV_MANUFACTURING;
		}

		_parent.SetSelectedTopic(selectedTopic, topicType);

		_game.PopState();
	}
}
