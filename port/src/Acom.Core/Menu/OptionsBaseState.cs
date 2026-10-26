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
// Ported from: src/Menu/OptionsBaseState.h, src/Menu/OptionsBaseState.cpp
// Port status: partial (missing: Screen::resetDisplay and SDL_WM_GrabInput – display mode, filters and mouse grab are applied by the host)
#nullable disable

using System.Runtime.CompilerServices;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

public enum OptionsOrigin : uint
{
	OPT_MENU = 0,
	OPT_GEOSCAPE = 1,
	OPT_BATTLESCAPE = 2,
}

/// <summary>Options base state for common stuff across Options windows.</summary>
public class OptionsBaseState : State
{
	protected OptionsOrigin _origin;
	protected Window _window;
	protected TextButton _btnVideo;
	protected TextButton _btnAudio;
	protected TextButton _btnControls;
	protected TextButton _btnGeoscape;
	protected TextButton _btnBattlescape;
	protected TextButton _btnAdvanced;
	protected TextButton _btnFolders;
	protected TextButton _btnOk;
	protected TextButton _btnCancel;
	protected TextButton _btnDefault;
	protected Text _txtTooltip;
	protected string _currentTooltip = "";
	protected StrongBox<TextButton> _group = new(null);

	/// <summary>Initializes all the elements in the Options window.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsBaseState(OptionsOrigin origin)
	{
		_origin = origin;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);

		_btnVideo = new TextButton(80, 16, 8, 8);
		_btnAudio = new TextButton(80, 16, 8, 28);
		_btnControls = new TextButton(80, 16, 8, 48);
		_btnGeoscape = new TextButton(80, 16, 8, 68);
		_btnBattlescape = new TextButton(80, 16, 8, 88);
		_btnAdvanced = new TextButton(80, 16, 8, 108);
		_btnFolders = new TextButton(80, 16, 8, 128);

		_btnOk = new TextButton(100, 16, 8, 176);
		_btnCancel = new TextButton(100, 16, 110, 176);
		_btnDefault = new TextButton(100, 16, 212, 176);

		_txtTooltip = new Text(305, 25, 8, 148);

		// Set palette
		SetInterface("optionsMenu", false, _game.SavedGame?.SavedBattle);

		Add(_window, "window", "optionsMenu");

		Add(_btnVideo, "button", "optionsMenu");
		Add(_btnAudio, "button", "optionsMenu");
		Add(_btnControls, "button", "optionsMenu");
		Add(_btnGeoscape, "button", "optionsMenu");
		Add(_btnBattlescape, "button", "optionsMenu");
		Add(_btnAdvanced, "button", "optionsMenu");
		Add(_btnFolders, "button", "optionsMenu");

		Add(_btnOk, "button", "optionsMenu");
		Add(_btnCancel, "button", "optionsMenu");
		Add(_btnDefault, "button", "optionsMenu");

		Add(_txtTooltip, "tooltip", "optionsMenu");

		// Set up objects
		SetWindowBackground(_window, "optionsMenu");

		_btnVideo.Text = Tr("STR_VIDEO");
		_btnVideo.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);

		_btnAudio.Text = Tr("STR_AUDIO");
		_btnAudio.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);

		_btnControls.Text = Tr("STR_CONTROLS");
		_btnControls.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);

		_btnGeoscape.Text = Tr("STR_GEOSCAPE_UC");
		_btnGeoscape.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);

		_btnBattlescape.Text = Tr("STR_BATTLESCAPE_UC");
		_btnBattlescape.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);

		_btnAdvanced.Text = Tr("STR_ADVANCED");
		_btnAdvanced.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);

		_btnFolders.Text = Tr("STR_FOLDERS");
		_btnFolders.OnMousePress(BtnGroupPress, SDLMouse.SDL_BUTTON_LEFT);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_btnDefault.Text = Tr("STR_RESTORE_DEFAULTS");
		_btnDefault.OnMouseClick(BtnDefaultClick);

		_txtTooltip.SetWordWrap(true);
	}

	/// <summary>Restarts the game states.</summary>
	public static void Restart(OptionsOrigin origin)
	{
		// Reset touch flags
		_game.ResetTouchButtonFlags();

		if (origin == OptionsOrigin.OPT_MENU)
		{
			_game.SetState(new MainMenuState());
		}
		else if (origin == OptionsOrigin.OPT_GEOSCAPE)
		{
			_game.SetState(new GeoscapeState());
		}
		else if (origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			BattlescapeState origBattleState = null;
			if (_game.SavedGame != null && _game.SavedGame.SavedBattle != null)
			{
				origBattleState = _game.SavedGame.SavedBattle.BattleState;
			}
			if (origBattleState != null)
			{
				// We need to reset palettes here already, can't wait for the destructor
				origBattleState.ResetPalettes();
			}

			_game.SetState(new GeoscapeState());
			var bs = new BattlescapeState();
			_game.PushState(bs);
			_game.SavedGame.SavedBattle.BattleState = bs;
			// Try to reactivate the touch buttons
			bs.ToggleTouchButtons(false, true);
		}
	}

	/// <summary>Initializes UI colors according to origin.</summary>
	public override void Init()
	{
		base.Init();
		if (_origin == OptionsOrigin.OPT_BATTLESCAPE)
		{
			ApplyBattlescapeTheme("optionsMenu");
		}
	}

	/// <summary>Handles the pressed-button state for the category buttons.</summary>
	/// <param name="button">Button to press.</param>
	public void SetCategory(TextButton button)
	{
		_group.Value = button;
		_btnVideo.SetGroup(_group);
		_btnAudio.SetGroup(_group);
		_btnControls.SetGroup(_group);
		_btnGeoscape.SetGroup(_group);
		_btnBattlescape.SetGroup(_group);
		_btnAdvanced.SetGroup(_group);
		_btnFolders.SetGroup(_group);
	}

	/// <summary>Saves the new options and returns to the proper origin screen.</summary>
	public void BtnOkClick(Action action)
	{
		Options.SwitchDisplay();
		int dX = Options.baseXResolution;
		int dY = Options.baseYResolution;
		Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, _origin == OptionsOrigin.OPT_BATTLESCAPE);
		Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, _origin != OptionsOrigin.OPT_BATTLESCAPE);
		dX = Options.baseXResolution - dX;
		dY = Options.baseYResolution - dY;
		Recenter(dX, dY);
		Options.Save();
		_game.LoadLanguages();
		_game.Screen.ResetDisplay(); // SDL_WM_GrabInput(Options::captureMouse) is a host matter
		_game.SetVolume(Options.soundVolume, Options.musicVolume, Options.uiVolume);
		if (Options.reload && _origin == OptionsOrigin.OPT_MENU)
		{
			_game.SetState(new StartState());
		}
		else
		{
			// Confirm any video options changes
			if (Options.displayWidth != Options.newDisplayWidth ||
				Options.displayHeight != Options.newDisplayHeight ||
				Options.useOpenGL != Options.newOpenGL ||
				Options.useScaleFilter != Options.newScaleFilter ||
				Options.useHQXFilter != Options.newHQXFilter ||
				Options.useOpenGLShader != Options.newOpenGLShader)
			{
				_game.PushState(new OptionsConfirmState(_origin));
			}
			else
			{
				Restart(_origin);
			}
		}
	}

	/// <summary>Loads previous options and returns to the previous screen.</summary>
	public void BtnCancelClick(Action action)
	{
		Options.reload = false;
		Options.Load();
		// missing: SDL_WM_GrabInput(Options::captureMouse); (host matter)
		Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, _origin == OptionsOrigin.OPT_BATTLESCAPE);
		Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, _origin != OptionsOrigin.OPT_BATTLESCAPE);
		_game.SetVolume(Options.soundVolume, Options.musicVolume, Options.uiVolume);
		_game.PopState();
	}

	/// <summary>Restores the Options to default settings.</summary>
	public void BtnDefaultClick(Action action)
	{
		_game.PushState(new OptionsDefaultsState(_origin, this));
	}

	/// <summary>Handler for clicking one of the grouping buttons.</summary>
	public void BtnGroupPress(Action action)
	{
		Surface sender = action.Sender;
		//if (sender != _group)
		{
			_game.PopState();
			if (sender == _btnVideo)
			{
				_game.PushState(new OptionsVideoState(_origin));
			}
			else if (sender == _btnAudio)
			{
				if (!Options.mute)
				{
					_game.PushState(new OptionsAudioState(_origin));
				}
				else
				{
					_game.PushState(new OptionsNoAudioState(_origin));
				}
			}
			else if (sender == _btnControls)
			{
				_game.PushState(new OptionsControlsState(_origin));
			}
			else if (sender == _btnGeoscape)
			{
				_game.PushState(new OptionsGeoscapeState(_origin));
			}
			else if (sender == _btnBattlescape)
			{
				_game.PushState(new OptionsBattlescapeState(_origin));
			}
			else if (sender == _btnAdvanced)
			{
				_game.PushState(new OptionsAdvancedState(_origin));
			}
			else if (sender == _btnFolders)
			{
				_game.PushState(new OptionsFoldersState(_origin));
			}
		}
	}

	/// <summary>Shows a tooltip for the appropriate button.</summary>
	public void TxtTooltipIn(Action action)
	{
		_currentTooltip = action.Sender.Tooltip;
		_txtTooltip.SetText(Tr(_currentTooltip));
	}

	/// <summary>Clears the tooltip text.</summary>
	public void TxtTooltipOut(Action action)
	{
		if (_currentTooltip == action.Sender.Tooltip)
		{
			_txtTooltip.SetText("");
		}
	}

	/// <summary>Update the resolution settings, we just resized the window.</summary>
	/// <param name="dX">delta of X;</param>
	/// <param name="dY">delta of Y;</param>
	public override void Resize(ref int dX, ref int dY)
	{
		Options.newDisplayWidth = Options.displayWidth;
		Options.newDisplayHeight = Options.displayHeight;
		base.Resize(ref dX, ref dY);
	}
}
