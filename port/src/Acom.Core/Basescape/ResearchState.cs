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
// Ported from: src/Basescape/ResearchState.h, src/Basescape/ResearchState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Research screen that lets the player manage
/// all the researching operations of a base.
/// </summary>
public class ResearchState : State
{
	private readonly Base _base;
	private readonly TextButton _btnNew, _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtAvailable, _txtAllocated, _txtSpace, _txtProject, _txtScientists, _txtProgress;
	private readonly TextList _lstResearch;

	/// <summary>
	/// Initializes all the elements in the Research screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	public ResearchState(Base @base)
	{
		_base = @base;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnNew = new TextButton(148, 16, 8, 176);
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
		SetInterface("researchMenu");

		Add(_window, "window", "researchMenu");
		Add(_btnNew, "button", "researchMenu");
		Add(_btnOk, "button", "researchMenu");
		Add(_txtTitle, "text", "researchMenu");
		Add(_txtAvailable, "text", "researchMenu");
		Add(_txtAllocated, "text", "researchMenu");
		Add(_txtSpace, "text", "researchMenu");
		Add(_txtProject, "text", "researchMenu");
		Add(_txtScientists, "text", "researchMenu");
		Add(_txtProgress, "text", "researchMenu");
		Add(_lstResearch, "list", "researchMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "researchMenu");

		_btnNew.Text = Tr("STR_NEW_PROJECT");
		_btnNew.OnMouseClick(BtnNewClick);
		_btnNew.OnKeyboardPress(BtnNewClick, Options.keyToggleQuickSearch);
		_btnNew.OnKeyboardPress(OnCurrentGlobalResearchClick, Options.keyGeoGlobalResearch);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_CURRENT_RESEARCH"));

		_txtProject.SetWordWrap(true);
		_txtProject.SetText(Tr("STR_RESEARCH_PROJECT"));

		_txtScientists.SetWordWrap(true);
		_txtScientists.SetText(Tr("STR_SCIENTISTS_ALLOCATED_UC"));

		_txtProgress.SetText(Tr("STR_PROGRESS"));

		if (Options.oxceBaseResearchReorder)
		{
			_lstResearch.SetArrowColumn(192, ArrowOrientation.ARROW_VERTICAL);
		}
		_lstResearch.SetColumns(3, 158, 58, 70);
		_lstResearch.SetSelectable(true);
		_lstResearch.SetBackground(_window);
		_lstResearch.Margin = 2;
		_lstResearch.SetWordWrap(true);
		if (Options.oxceBaseResearchReorder)
		{
			_lstResearch.OnLeftArrowClick(LstResearchLeftArrowClick);
			_lstResearch.OnRightArrowClick(LstResearchRightArrowClick);
		}
		_lstResearch.OnMouseClick(OnSelectProject, SDLMouse.SDL_BUTTON_LEFT);
		_lstResearch.OnMouseClick(OnOpenTechTreeViewer, SDLMouse.SDL_BUTTON_MIDDLE);
		_lstResearch.OnMousePress(LstResearchMousePress);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens the list of possible ResearchProjects.
	/// </summary>
	public void BtnNewClick(Action action)
	{
		bool sortByCost = _game.IsCtrlPressed() && _game.IsAltPressed();
		_game.PushState(new NewResearchListState(_base, sortByCost));
	}

	/// <summary>
	/// Displays the info of the selected ResearchProject.
	/// </summary>
	public void OnSelectProject(Action action)
	{
		double mx = action.AbsoluteXMouse;
		if (mx >= _lstResearch.ArrowsLeftEdge && mx < _lstResearch.ArrowsRightEdge)
		{
			return;
		}

		var baseProjects = _base.Research;
		_game.PushState(new ResearchInfoState(_base, baseProjects[(int)_lstResearch.SelectedRow]));
	}

	/// <summary>
	/// Opens the TechTreeViewer for the corresponding topic.
	/// </summary>
	public void OnOpenTechTreeViewer(Action action)
	{
		double mx = action.AbsoluteXMouse;
		if (mx >= _lstResearch.ArrowsLeftEdge && mx < _lstResearch.ArrowsRightEdge)
		{
			return;
		}

		var baseProjects = _base.Research;
		RuleResearch selectedTopic = baseProjects[(int)_lstResearch.SelectedRow].Rules;
		_game.PushState(new TechTreeViewerState(selectedTopic, null));
	}

	/// <summary>
	/// Handles the mouse-wheels.
	/// </summary>
	public void LstResearchMousePress(Action action)
	{
		if (!_lstResearch.IsInsideNoScrollArea((int)action.AbsoluteXMouse))
		{
			return;
		}

		int change = Options.oxceResearchScrollSpeed;
		if (_game.IsCtrlPressed())
			change = Options.oxceResearchScrollSpeedWithCtrl;

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
		{
			ResearchProject selectedProject = _base.Research[(int)_lstResearch.SelectedRow];
			int remaining = selectedProject.Cost - selectedProject.Spent - selectedProject.Assigned;
			remaining = Math.Max(1, remaining);

			change = Math.Min(change, _base.AvailableScientists);
			change = Math.Min(change, _base.FreeLaboratories);
			change = Math.Min(change, remaining);

			if (change > 0)
			{
				selectedProject.Assigned = selectedProject.Assigned + change;
				_base.Scientists = _base.Scientists - change;
				FillProjectList(_lstResearch.Scroll);
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
		{
			ResearchProject selectedProject = _base.Research[(int)_lstResearch.SelectedRow];
			change = Math.Min(change, selectedProject.Assigned);
			if (change > 0)
			{
				selectedProject.Assigned = selectedProject.Assigned - change;
				_base.Scientists = _base.Scientists + change;
				FillProjectList(_lstResearch.Scroll);
			}
		}
	}

	/// <summary>
	/// Opens the Current Global Research UI.
	/// </summary>
	public void OnCurrentGlobalResearchClick(Action action)
	{
		_game.PushState(new GlobalResearchState(true));
	}

	/// <summary>
	/// Fills the list with Base ResearchProjects. Also updates count of available lab space and available/allocated scientists.
	/// </summary>
	public void FillProjectList(int scrl)
	{
		_lstResearch.ClearList();
		foreach (var proj in _base.Research)
		{
			string sstr = proj.Assigned.ToString(CultureInfo.InvariantCulture);
			RuleResearch r = proj.Rules;

			string wstr = Tr(r.Name);
			_lstResearch.AddRow(3, wstr, sstr, Tr(proj.ResearchProgress));
		}
		_txtAvailable.SetText(Tr("STR_SCIENTISTS_AVAILABLE").Arg(_base.AvailableScientists));
		_txtAllocated.SetText(Tr("STR_SCIENTISTS_ALLOCATED").Arg(_base.AllocatedScientists));
		_txtSpace.SetText(Tr("STR_LABORATORY_SPACE_AVAILABLE").Arg(_base.FreeLaboratories));

		if (scrl != 0)
			_lstResearch.ScrollTo(scrl);
	}

	/// <summary>
	/// Updates the research list
	/// after going to other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();
		FillProjectList(0);

		if (Options.oxceResearchScrollSpeed > 0 || Options.oxceResearchScrollSpeedWithCtrl > 0)
		{
			// 175 +/- 20
			_lstResearch.SetNoScrollArea(_txtAllocated.X - 5, _txtAllocated.X + 35);
		}
		else
		{
			_lstResearch.SetNoScrollArea(0, 0);
		}
	}

	/// <summary>
	/// Reorders a research topic up.
	/// </summary>
	public void LstResearchLeftArrowClick(Action action)
	{
		uint row = _lstResearch.SelectedRow;
		if (row > 0)
		{
			if (_game.IsLeftClick(action, true))
			{
				MoveTopicUp(action, row);
			}
			else if (_game.IsRightClick(action, true))
			{
				MoveTopicUp(action, row, true);
			}
		}
	}

	/// <summary>
	/// Moves a research topic up on the list.
	/// </summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="row">Selected research topic row.</param>
	/// <param name="max">Move the research topic to the top?</param>
	public void MoveTopicUp(Action action, uint row, bool max = false)
	{
		var topics = _base.Research;
		if (max)
		{
			var r = topics[(int)row];
			topics.RemoveAt((int)row);
			topics.Insert(0, r);
		}
		else
		{
			(topics[(int)row], topics[(int)row - 1]) = (topics[(int)row - 1], topics[(int)row]);
			if (row != _lstResearch.Scroll)
			{
				WarpMouse(action.LeftBlackBand + action.XMouse, action.TopBlackBand + action.YMouse - (ushort)(8 * action.YScale));
			}
			else
			{
				_lstResearch.ScrollUp(false);
			}
		}
		FillProjectList(_lstResearch.Scroll);
	}

	/// <summary>
	/// Reorders a research topic down.
	/// </summary>
	public void LstResearchRightArrowClick(Action action)
	{
		uint row = _lstResearch.SelectedRow;
		int numTopics = _base.Research.Count;
		if (0 < numTopics && row < numTopics - 1)
		{
			if (_game.IsLeftClick(action, true))
			{
				MoveTopicDown(action, row);
			}
			else if (_game.IsRightClick(action, true))
			{
				MoveTopicDown(action, row, true);
			}
		}
	}

	/// <summary>
	/// Moves a research topic down on the list.
	/// </summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="row">Selected research topic row.</param>
	/// <param name="max">Move the research topic to the bottom?</param>
	public void MoveTopicDown(Action action, uint row, bool max = false)
	{
		var topics = _base.Research;
		if (max)
		{
			var r = topics[(int)row];
			topics.RemoveAt((int)row);
			topics.Add(r);
		}
		else
		{
			(topics[(int)row], topics[(int)row + 1]) = (topics[(int)row + 1], topics[(int)row]);
			if (row != _lstResearch.VisibleRows - 1 + _lstResearch.Scroll)
			{
				WarpMouse(action.LeftBlackBand + action.XMouse, action.TopBlackBand + action.YMouse + (ushort)(8 * action.YScale));
			}
			else
			{
				_lstResearch.ScrollDown(false);
			}
		}
		FillProjectList(_lstResearch.Scroll);
	}

	private static void WarpMouse(int x, int y) => SDLMouse.SDL_WarpMouse((ushort)x, (ushort)y);
}
