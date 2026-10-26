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
// Ported from: src/Basescape/GlobalResearchState.h, src/Basescape/GlobalResearchState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Global Research screen that provides overview
/// of the ongoing research operations in all the bases.
/// </summary>
public class GlobalResearchState : State
{
	private readonly TextButton _btnOk;
	private readonly TextButton _btnDiary;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtAvailable, _txtAllocated, _txtSpace, _txtProject, _txtScientists, _txtProgress;
	private readonly TextList _lstResearch;
	private readonly List<Base> _bases = [];
	private readonly List<RuleResearch> _topics = [];
	private readonly bool _openedFromBasescape;

	/// <summary>
	/// Initializes all the elements in the GlobalResearch screen.
	/// </summary>
	public GlobalResearchState(bool openedFromBasescape)
	{
		_openedFromBasescape = openedFromBasescape;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnDiary = new TextButton(148, 16, 8, 176);
		_btnOk = new TextButton(148, 16, 164, 176);
		_txtTitle = new Text(310, 17, 5, 8);
		_txtAvailable = new Text(150, 9, 10, 24);
		_txtAllocated = new Text(150, 9, 160, 24);
		_txtSpace = new Text(300, 9, 10, 34);
		_txtProject = new Text(110, 17, 10, 44);
		_txtScientists = new Text(106, 17, 120, 44);
		_txtProgress = new Text(84, 9, 226, 44);
		_lstResearch = new TextList(288, 112, 8, 62);

		// Set palette
		SetInterface("globalResearchMenu");

		Add(_window, "window", "globalResearchMenu");
		Add(_btnDiary, "button", "globalResearchMenu");
		Add(_btnOk, "button", "globalResearchMenu");
		Add(_txtTitle, "text", "globalResearchMenu");
		Add(_txtAvailable, "text", "globalResearchMenu");
		Add(_txtAllocated, "text", "globalResearchMenu");
		Add(_txtSpace, "text", "globalResearchMenu");
		Add(_txtProject, "text", "globalResearchMenu");
		Add(_txtScientists, "text", "globalResearchMenu");
		Add(_txtProgress, "text", "globalResearchMenu");
		Add(_lstResearch, "list", "globalResearchMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "globalResearchMenu");

		_btnDiary.Text = Tr("STR_RESEARCH_DIARY");
		_btnDiary.OnMouseClick(BtnDiaryClick);
		_btnDiary.OnKeyboardPress(BtnDiaryClick, Options.keyGeoGlobalResearch);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_RESEARCH_OVERVIEW"));

		_txtProject.SetWordWrap(true);
		_txtProject.SetText(Tr("STR_RESEARCH_PROJECT"));

		_txtScientists.SetWordWrap(true);
		_txtScientists.SetText(Tr("STR_SCIENTISTS_ALLOCATED_UC"));

		_txtProgress.SetText(Tr("STR_PROGRESS"));

		_lstResearch.SetColumns(3, 158, 58, 70);
		_lstResearch.SetSelectable(true);
		_lstResearch.SetBackground(_window);
		_lstResearch.Margin = 2;
		_lstResearch.SetWordWrap(true);
		_lstResearch.OnMouseClick(OnSelectBase, SDLMouse.SDL_BUTTON_LEFT);
		_lstResearch.OnMouseClick(OnOpenTechTreeViewer, SDLMouse.SDL_BUTTON_MIDDLE);
	}

	/// <summary>
	/// Displays the Research Diary UI.
	/// </summary>
	public void BtnDiaryClick(Action action)
	{
		_game.PushState(new GlobalResearchDiaryState());
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Goes to the base's research screen when clicking its project
	/// </summary>
	public void OnSelectBase(Action action)
	{
		Base @base = _bases[(int)_lstResearch.SelectedRow];

		if (@base != null)
		{
			// close this window
			_game.PopState();

			// close Research UI (goes back to BaseView)
			if (_openedFromBasescape)
			{
				_game.PopState();
			}

			// open new window
			_game.PushState(new ResearchState(@base));
		}
	}

	/// <summary>
	/// Opens the TechTreeViewer for the corresponding topic.
	/// </summary>
	public void OnOpenTechTreeViewer(Action action)
	{
		RuleResearch selectedTopic = _topics[(int)_lstResearch.SelectedRow];

		if (selectedTopic != null)
		{
			_game.PushState(new TechTreeViewerState(selectedTopic, null));
		}
	}

	/// <summary>
	/// Updates the research list
	/// after going to other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();
		FillProjectList();
	}

	/// <summary>
	/// Fills the list with ResearchProjects from all bases. Also updates total count of available lab space and available/allocated scientists.
	/// </summary>
	public void FillProjectList()
	{
		_bases.Clear();
		_topics.Clear();
		_lstResearch.ClearList();

		int availableScientists = 0;
		int allocatedScientists = 0;
		int freeLaboratories = 0;

		foreach (Base xbase in _game.SavedGame.Bases)
		{
			var baseProjects = xbase.Research;
			if (baseProjects.Count != 0 || xbase.Scientists > 0)
			{
				string baseName = xbase.GetName(_game.Language);
				_lstResearch.AddRow(3, baseName, "", "");
				_lstResearch.SetRowColor(_lstResearch.LastRowIndex, _lstResearch.SecondaryColor);

				// dummy
				_bases.Add(null);
				_topics.Add(null);
			}
			foreach (var proj in baseProjects)
			{
				string sstr = proj.Assigned.ToString(CultureInfo.InvariantCulture);
				RuleResearch r = proj.Rules;

				string wstr = Tr(r.Name);
				_lstResearch.AddRow(3, wstr, sstr, Tr(proj.ResearchProgress));

				_bases.Add(xbase);
				_topics.Add(r);
			}
			if (baseProjects.Count == 0 && xbase.Scientists > 0)
			{
				_lstResearch.AddRow(3, Tr("STR_NONE"), "", "");

				_bases.Add(xbase);
				_topics.Add(null);
			}

			availableScientists += xbase.AvailableScientists;
			allocatedScientists += xbase.AllocatedScientists;
			freeLaboratories += xbase.FreeLaboratories;
		}

		_txtAvailable.SetText(Tr("STR_SCIENTISTS_AVAILABLE").Arg(availableScientists));
		_txtAllocated.SetText(Tr("STR_SCIENTISTS_ALLOCATED").Arg(allocatedScientists));
		_txtSpace.SetText(Tr("STR_LABORATORY_SPACE_AVAILABLE").Arg(freeLaboratories));
	}
}
