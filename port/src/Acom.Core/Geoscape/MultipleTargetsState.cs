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
// Ported from: src/Geoscape/MultipleTargetsState.h, src/Geoscape/MultipleTargetsState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Displays a list of possible targets.
/// </summary>
public class MultipleTargetsState : State
{
	private const int MARGIN = 10;
	private const int SPACING = 4;
	private const int BUTTON_HEIGHT = 16;

	private readonly List<Target> _targets;
	private readonly List<Craft> _crafts;
	private readonly GeoscapeState _state;
	private readonly bool _useCustomSound;

	private readonly Window _window;
	private readonly List<TextButton> _btnTargets = [];

	/// <summary>
	/// Initializes all the elements in the Multiple Targets window.
	/// </summary>
	/// <param name="targets">List of targets to display.</param>
	/// <param name="crafts">Pointers to crafts if multiple targets are being picked by them.</param>
	/// <param name="state">Pointer to the Geoscape state.</param>
	/// <param name="useCustomSound">Play a custom sound when opened?</param>
	public MultipleTargetsState(List<Target> targets, List<Craft> crafts, GeoscapeState state, bool useCustomSound)
	{
		_targets = [.. targets];
		_crafts = [.. crafts]; // passed by value in C++
		_state = state;
		_useCustomSound = useCustomSound;
		_screen = false;

		if (_targets.Count > 1)
		{
			int winHeight = BUTTON_HEIGHT * _targets.Count + SPACING * (_targets.Count - 1) + MARGIN * 2;
			int winY = (200 - winHeight) / 2;
			int btnY = winY + MARGIN;

			// Create objects
			_window = new Window(this, 136, winHeight, 60, winY, WindowPopup.POPUP_VERTICAL);

			// Set palette
			SetInterface("multipleTargets");

			Add(_window, "window", "multipleTargets");

			// Set up objects
			SetWindowBackground(_window, "multipleTargets");

			int y = btnY;
			for (int i = 0; i < _targets.Count; ++i)
			{
				TextButton button = new TextButton(116, BUTTON_HEIGHT, 70, y);
				button.Text = _targets[i].GetName(_game.Language);
				button.OnMouseClick(BtnTargetClick);
				Add(button, "button", "multipleTargets");

				_btnTargets.Add(button);

				y += button.Height + SPACING;
			}
			_btnTargets[0].OnKeyboardPress(BtnCancelClick, Options.keyCancel);

			CenterAllSurfaces();
		}
	}

	/// <summary>
	/// Resets the palette and ignores the window
	/// if there's only one target.
	/// </summary>
	public override void Init()
	{
		if (_targets.Count == 1)
		{
			PopupTarget(_targets[0]);
		}
		else
		{
			base.Init();
		}
	}

	/// <summary>
	/// Displays the right popup for a specific target.
	/// </summary>
	/// <param name="target">Pointer to target.</param>
	public void PopupTarget(Target target)
	{
		_game.PopState();
		if (_crafts.Count == 0)
		{
			if (target is Base b)
			{
				_game.PushState(new InterceptState(_state.Globe, _useCustomSound, b, null));
			}
			else if (target is Craft c)
			{
				_game.PushState(new GeoscapeCraftState(c, _state.Globe, null, _useCustomSound));
			}
			else if (target is Ufo u)
			{
				_game.PushState(new UfoDetectedState(u, _state, false, u.HyperDetected));
			}
			else
			{
				_game.PushState(new TargetInfoState(target, _state.Globe));
			}
		}
		else
		{
			_game.PushState(new ConfirmDestinationState(_crafts, target));
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Pick a target to display.
	/// </summary>
	public void BtnTargetClick(Action action)
	{
		for (int i = 0; i < _btnTargets.Count; ++i)
		{
			if (action.Sender == _btnTargets[i])
			{
				PopupTarget(_targets[i]);
				break;
			}
		}
	}
}
