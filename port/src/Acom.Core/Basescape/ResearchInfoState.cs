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
// Ported from: src/Basescape/ResearchInfoState.h, src/Basescape/ResearchInfoState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window which allows changing of the number of assigned scientist to a project.
/// </summary>
public class ResearchInfoState : State
{
	private readonly Base _base;
	private TextButton _btnOk;
	private TextButton _btnCancel;
	private ArrowButton _btnMore, _btnLess;
	private Window _window;
	private Text _txtTitle, _txtAvailableScientist, _txtAvailableSpace, _txtAllocatedScientist, _txtMore, _txtLess;
	private readonly ResearchProject _project;
	private readonly RuleResearch _rule;
	private Timer _timerMore, _timerLess;
	private InteractiveSurface _surfaceScientists;

	/// <summary>
	/// Initializes all the elements in the ResearchProject screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="rule">A RuleResearch which will be used to create a new ResearchProject</param>
	public ResearchInfoState(Base @base, RuleResearch rule)
	{
		_base = @base;
		_project = null;
		_rule = rule;

		if (Options.oxceFixedResearchCosts)
		{
			_project = new ResearchProject(rule, rule.Cost);
		}
		else
		{
			int rng = Rng.Generate(50, 150);
			int randomizedCost = rule.Cost * rng / 100;
			if (rule.Cost > 0)
			{
				randomizedCost = Math.Max(1, randomizedCost);
			}
			_project = new ResearchProject(rule, randomizedCost);
		}

		BuildUi();
	}

	/// <summary>
	/// Initializes all the elements in the ResearchProject screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="project">A ResearchProject to modify</param>
	public ResearchInfoState(Base @base, ResearchProject project)
	{
		_base = @base;
		_project = project;
		_rule = null;
		BuildUi();
	}

	/// <summary>
	/// Builds dialog.
	/// </summary>
	private void BuildUi()
	{
		_screen = false;

		_window = new Window(this, 230, 140, 45, 30);
		_txtTitle = new Text(210, 17, 61, 40);

		_txtAvailableScientist = new Text(210, 9, 61, 60);
		_txtAvailableSpace = new Text(210, 9, 61, 70);
		_txtAllocatedScientist = new Text(210, 17, 61, 80);
		_txtMore = new Text(110, 17, 85, 100);
		_txtLess = new Text(110, 17, 85, 120);
		_btnCancel = new TextButton(90, 16, 61, 145);
		_btnOk = new TextButton(90, 16, 169, 145);

		_btnMore = new ArrowButton(ArrowShape.ARROW_BIG_UP, 13, 14, 195, 100);
		_btnLess = new ArrowButton(ArrowShape.ARROW_BIG_DOWN, 13, 14, 195, 120);

		_surfaceScientists = new InteractiveSurface(230, 140, 45, 30);
		_surfaceScientists.OnMouseClick(HandleWheel, 0);

		// Set palette
		SetInterface("allocateResearch");

		Add(_surfaceScientists);
		Add(_window, "window", "allocateResearch");
		Add(_btnOk, "button2", "allocateResearch");
		Add(_btnCancel, "button2", "allocateResearch");
		Add(_txtTitle, "text", "allocateResearch");
		Add(_txtAvailableScientist, "text", "allocateResearch");
		Add(_txtAvailableSpace, "text", "allocateResearch");
		Add(_txtAllocatedScientist, "text", "allocateResearch");
		Add(_txtMore, "text", "allocateResearch");
		Add(_txtLess, "text", "allocateResearch");
		Add(_btnMore, "button1", "allocateResearch");
		Add(_btnLess, "button1", "allocateResearch");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "allocateResearch");

		_txtTitle.SetBig();

		_txtTitle.SetText(_rule != null ? Tr(_rule.Name) : Tr(_project.Rules.Name));

		_txtAllocatedScientist.SetBig();

		_txtMore.SetText(Tr("STR_INCREASE"));
		_txtLess.SetText(Tr("STR_DECREASE"));

		_txtMore.SetBig();
		_txtLess.SetBig();

		if (_rule != null)
		{
			_base.AddResearch(_project);
			if (_rule.IsHoldingNeededItem)
			{
				_base.StorageItems.RemoveItem(_rule.NeededItem, 1);
			}
		}
		SetAssignedScientist();
		_btnMore.OnMousePress(MorePress);
		_btnMore.OnMouseRelease(MoreRelease);
		_btnMore.OnMouseClick(MoreClick, 0);
		_btnLess.OnMousePress(LessPress);
		_btnLess.OnMouseRelease(LessRelease);
		_btnLess.OnMouseClick(LessClick, 0);

		_timerMore = new Timer(250);
		_timerMore.OnTimer((StateHandler)More);
		_timerLess = new Timer(250);
		_timerLess.OnTimer((StateHandler)Less);

		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		if (_rule != null)
		{
			_btnOk.Text = Tr("STR_START_PROJECT");
			_btnCancel.Text = Tr("STR_CANCEL_UC");
			_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);
		}
		else
		{
			_btnOk.Text = Tr("STR_OK");
			_btnCancel.Text = Tr("STR_CANCEL_PROJECT");
			_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		}
		_btnCancel.OnMouseClick(BtnCancelClick);

		if (_rule != null)
		{
			// mark new/hidden as normal
			_game.SavedGame.SetResearchRuleStatus(_rule.Name, RuleResearch.RESEARCH_STATUS_NORMAL);
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
	/// Returns to the previous screen, removing the current project from the active
	/// research list.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_base.RemoveResearch(_project);
		_game.PopState();
	}

	/// <summary>
	/// Updates count of assigned/free scientists and available lab space.
	/// </summary>
	private void SetAssignedScientist()
	{
		_txtAvailableScientist.SetText(Tr("STR_SCIENTISTS_AVAILABLE_UC").Arg(_base.AvailableScientists));
		_txtAvailableSpace.SetText(Tr("STR_LABORATORY_SPACE_AVAILABLE_UC").Arg(_base.FreeLaboratories));
		_txtAllocatedScientist.SetText(Tr("STR_SCIENTISTS_ALLOCATED").Arg(_project.Assigned));
	}

	/// <summary>
	/// Increases or decreases the scientists according the mouse-wheel used.
	/// </summary>
	public void HandleWheel(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP) MoreByValue(Options.changeValueByMouseWheel);
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN) LessByValue(Options.changeValueByMouseWheel);
	}

	/// <summary>
	/// Starts the timeMore timer.
	/// </summary>
	public void MorePress(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT) _timerMore.Start();
	}

	/// <summary>
	/// Stops the timeMore timer.
	/// </summary>
	public void MoreRelease(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_timerMore.SetInterval(250);
			_timerMore.Stop();
		}
	}

	/// <summary>
	/// Allocates scientists to the current project;
	/// one scientist on left-click, all scientists on right-click.
	/// </summary>
	public void MoreClick(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
			MoreByValue(int.MaxValue);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
			MoreByValue(1);
	}

	/// <summary>
	/// Starts the timeLess timer.
	/// </summary>
	public void LessPress(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT) _timerLess.Start();
	}

	/// <summary>
	/// Stops the timeLess timer.
	/// </summary>
	public void LessRelease(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			_timerLess.SetInterval(250);
			_timerLess.Stop();
		}
	}

	/// <summary>
	/// Removes scientists from the current project;
	/// one scientist on left-click, all scientists on right-click.
	/// </summary>
	public void LessClick(Action action)
	{
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
			LessByValue(int.MaxValue);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
			LessByValue(1);
	}

	/// <summary>
	/// Adds one scientist to the project if possible.
	/// </summary>
	public void More()
	{
		_timerMore.SetInterval(50);
		MoreByValue(1);
	}

	/// <summary>
	/// Adds the given number of scientists to the project if possible.
	/// </summary>
	/// <param name="change">Number of scientists to add.</param>
	public void MoreByValue(int change)
	{
		if (0 >= change) return;
		int freeScientist = _base.AvailableScientists;
		int freeSpaceLab = _base.FreeLaboratories;
		int remaining = _project.Cost - _project.Spent - _project.Assigned;
		remaining = Math.Max(1, remaining);

		if (freeScientist > 0 && freeSpaceLab > 0 && remaining > 0)
		{
			change = Math.Min(Math.Min(Math.Min(freeScientist, freeSpaceLab), remaining), change);
			_project.Assigned = _project.Assigned + change;
			_base.Scientists = _base.Scientists - change;
			SetAssignedScientist();
		}
	}

	/// <summary>
	/// Removes one scientist from the project if possible.
	/// </summary>
	public void Less()
	{
		_timerLess.SetInterval(50);
		LessByValue(1);
	}

	/// <summary>
	/// Removes the given number of scientists from the project if possible.
	/// </summary>
	/// <param name="change">Number of scientists to subtract.</param>
	public void LessByValue(int change)
	{
		if (0 >= change) return;
		int assigned = _project.Assigned;
		if (assigned > 0)
		{
			change = Math.Min(assigned, change);
			_project.Assigned = assigned - change;
			_base.Scientists = _base.Scientists + change;
			SetAssignedScientist();
		}
	}

	/// <summary>
	/// Runs state functionality every cycle (used to update the timer).
	/// </summary>
	public override void Think()
	{
		base.Think();

		_timerLess.Think(this, null);
		_timerMore.Think(this, null);
	}
}
