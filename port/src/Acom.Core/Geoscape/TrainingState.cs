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
// Ported from: src/Geoscape/TrainingState.h, src/Geoscape/TrainingState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // UI handlers stay instance members like in C++ (State._game is static here)

/// <summary>
/// Training screen that lets the player pick a base to assign soldiers for martial training.
/// </summary>
public class TrainingState : State
{
	private readonly TextButton _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly List<TextButton> _btnBases = [];
	private readonly List<Base> _bases = [];

	/// <summary>
	/// Initializes all the elements in the training screen.
	/// </summary>
	public TrainingState()
	{
		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_txtTitle = new Text(300, 17, 10, 16);
		_btnOk = new TextButton(160, 14, 80, 174);

		// Set palette
		SetInterface("martialTraining");

		Add(_window, "window", "martialTraining");
		Add(_btnOk, "button2", "martialTraining");
		Add(_txtTitle, "text", "martialTraining");

		// Set up objects
		SetWindowBackground(_window, "martialTraining");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_PHYSICAL_TRAINING"));

		int buttons = 0;
		foreach (var xbase in _game.SavedGame.Bases)
		{
			if (xbase.AvailableTraining != 0)
			{
				var btnBase = new TextButton(160, 14, 80, 40 + 16 * buttons);
				btnBase.SetColor((byte)(Acom.Palette.BlockOffset(15) + 6));
				btnBase.OnMouseClick(BtnBaseXClick);
				btnBase.Text = xbase.GetName(null);
				Add(btnBase, "button1", "martialTraining");
				_bases.Add(xbase);
				_btnBases.Add(btnBase);
				++buttons;
				if (buttons >= 8)
				{
					break;
				}
			}
		}

		CenterAllSurfaces();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Goes to the allocation screen for the corresponding base.
	/// </summary>
	public void BtnBaseXClick(Action action)
	{
		for (int i = 0; i < _btnBases.Count; ++i)
		{
			if (action.Sender == _btnBases[i])
			{
				_game.PushState(new AllocateTrainingState(_bases[i]));
				break;
			}
		}
	}
}
