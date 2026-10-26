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
// Ported from: src/Geoscape/TrainingFinishedState.h, src/Geoscape/TrainingFinishedState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window shown when soldiers finish (psi) training.
/// </summary>
public class TrainingFinishedState : State
{
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly TextList _lstPossibilities;
	private readonly TextButton _btnOpen, _btnOk;
	private readonly Base _base;
	private readonly bool _psi;

	/// <summary>
	/// Initializes all the elements in the TrainingFinished screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="list">List of soldiers who finished training.</param>
	/// <param name="psi">Psi training or physical training?</param>
	public TrainingFinishedState(Base @base, List<Soldier> list, bool psi)
	{
		_base = @base;
		_psi = psi;
		_screen = false;

		// Create objects
		_window = new Window(this, 288, 180, 16, 10);
		_btnOk = new TextButton(160, 14, 80, 149);
		_btnOpen = new TextButton(160, 14, 80, 165);
		_txtTitle = new Text(288, 33, 16, 20);
		_lstPossibilities = new TextList(250, 80, 35, 60);

		// Set palette
		SetInterface("trainingFinished");

		Add(_window, "window", "trainingFinished");
		Add(_btnOk, "button", "trainingFinished");
		Add(_btnOpen, "button", "trainingFinished");
		Add(_txtTitle, "text1", "trainingFinished");
		Add(_lstPossibilities, "text2", "trainingFinished");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "trainingFinished");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOpen.Text = Tr(_psi ? "STR_PSI_TRAINING" : "STR_TRAINING");
		_btnOpen.OnMouseClick(BtnOpenClick);
		_btnOpen.OnKeyboardPress(BtnOpenClick, Options.keyOk);
		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr(_psi ? "STR_PSI_TRAINING_FINISHED" : "STR_TRAINING_FINISHED").Arg(@base.GetName(null)));

		_lstPossibilities.SetColumns(1, 250);
		_lstPossibilities.SetBig();
		_lstPossibilities.SetAlign(TextHAlign.ALIGN_CENTER);
		_lstPossibilities.SetScrolling(true, 0);
		foreach (var soldier in list)
		{
			_lstPossibilities.AddRow(1, soldier.GetName());
		}
	}

	/// <summary>
	/// Closes the screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens the AllocateTrainingState/AllocatePsiTrainingState.
	/// </summary>
	public void BtnOpenClick(Action action)
	{
		_game.PopState();
		if (_psi)
		{
			_game.PushState(new AllocatePsiTrainingState(_base));
		}
		else
		{
			_game.PushState(new AllocateTrainingState(_base));
		}
	}
}
