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
// Ported from: src/Battlescape/MedikitState.h, src/Battlescape/MedikitState.cpp
// Port status: partial (missing: Screen::resetDisplay)
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>Helper class for the medikit title.</summary>
internal sealed class MedikitTitle : Text
{
	/// <summary>Initializes a Medikit title.</summary>
	/// <param name="y">The title's y origin.</param>
	/// <param name="title">The title.</param>
	public MedikitTitle(int y, string title) : base(73 + 60, 9, 186 - 30, y)
	{
		SetText(title);
		SetHighContrast(true);
		Align = TextHAlign.ALIGN_CENTER;
	}
}

/// <summary>Helper class for the medikit value.</summary>
internal sealed class MedikitTxt : Text
{
	/// <summary>Initializes a Medikit text.</summary>
	/// <param name="y">The text's y origin.</param>
	public MedikitTxt(int y) : base(33, 17, 220, y)
	{
		// Note: we can't set setBig here. The needed font is only set when added to State
		SetColor(Acom.Palette.BlockOffset(1));
		SetHighContrast(true);
		Align = TextHAlign.ALIGN_CENTER;
	}
}

/// <summary>Helper class for the medikit button.</summary>
internal sealed class MedikitButton : InteractiveSurface
{
	/// <summary>Initializes a Medikit button.</summary>
	/// <param name="y">The button's y origin.</param>
	public MedikitButton(int y) : base(30, 20, 190, y)
	{
	}
}

/// <summary>
/// The Medikit User Interface. Medikit is an item for healing a soldier.
/// </summary>
public class MedikitState : State
{
	private readonly Surface _bg;
	private readonly MedikitView _medikitView;
	private readonly Text _pkText, _stimulantTxt, _healTxt, _partTxt, _woundTxt;
	private readonly InteractiveSurface _endButton, _stimulantButton, _pkButton, _healButton;
	private readonly BattleUnit _targetUnit;
	private readonly BattleItem _item;
	private readonly BattleAction _action;
	private readonly TileEngine _tileEngine;

	/// <summary>Initializes the Medikit State.</summary>
	/// <param name="targetUnit">The wounded unit.</param>
	/// <param name="action">The healing action.</param>
	/// <param name="tile">The tile engine.</param>
	public MedikitState(BattleUnit targetUnit, BattleAction action, TileEngine tile)
	{
		ArgumentNullException.ThrowIfNull(action);
		_targetUnit = targetUnit;
		_action = action;
		_tileEngine = tile;

		if (Options.maximizeInfoScreens)
		{
			Options.baseXResolution = Screen.ORIGINAL_WIDTH;
			Options.baseYResolution = Screen.ORIGINAL_HEIGHT;
			// missing: _game->getScreen()->resetDisplay(false);
		}

		_item = action.weapon;
		_bg = new Surface(320, 200);

		// Set palette
		_game.SavedGame.SavedBattle.SetPaletteByDepth(this);

		if (_game.Screen.DY > 50)
		{
			_screen = false;
			_bg.DrawRect(67, 44, 190, 100, (byte)(Acom.Palette.BlockOffset(15) + 15));
		}
		_partTxt = new Text(62, 9, 82, 120);
		_woundTxt = new Text(14, 9, 145, 120);
		_medikitView = new MedikitView(52, 58, 95, 60, _game, _targetUnit, _partTxt, _woundTxt);
		_endButton = new InteractiveSurface(20, 20, 220, 140);
		_stimulantButton = new MedikitButton(84);
		_pkButton = new MedikitButton(48);
		_healButton = new MedikitButton(120);
		_pkText = new MedikitTxt(52);
		_stimulantTxt = new MedikitTxt(88);
		_healTxt = new MedikitTxt(124);
		Add(_bg);
		Add(_medikitView, "body", "medikit", _bg);
		Add(_endButton, "buttonEnd", "medikit", _bg);
		Add(new MedikitTitle(37, Tr(_item.Rules.PainKillerActionName)), "textPK", "medikit", _bg);
		Add(new MedikitTitle(73, Tr(_item.Rules.StimulantActionName)), "textStim", "medikit", _bg);
		Add(new MedikitTitle(109, Tr(_item.Rules.HealActionName)), "textHeal", "medikit", _bg);
		Add(_healButton, "buttonHeal", "medikit", _bg);
		Add(_stimulantButton, "buttonStim", "medikit", _bg);
		Add(_pkButton, "buttonPK", "medikit", _bg);
		Add(_pkText, "numPK", "medikit", _bg);
		Add(_stimulantTxt, "numStim", "medikit", _bg);
		Add(_healTxt, "numHeal", "medikit", _bg);
		Add(_partTxt, "textPart", "medikit", _bg);
		Add(_woundTxt, "numWounds", "medikit", _bg);

		CenterAllSurfaces();

		Surface backgroundSprite = null;
		if (!string.IsNullOrEmpty(_item.Rules.MediKitCustomBackground))
		{
			backgroundSprite = _game.Mod.GetSurface(_item.Rules.MediKitCustomBackground, false);
		}
		if (backgroundSprite == null)
		{
			backgroundSprite = _game.Mod.GetSurface("MEDIBORD.PCK");
		}

		backgroundSprite.BlitNShade(_bg, 0, 0);
		_pkText.SetBig();
		_stimulantTxt.SetBig();
		_healTxt.SetBig();
		_partTxt.SetHighContrast(true);
		_woundTxt.SetHighContrast(true);
		_endButton.OnMouseClick(OnEndClick);
		_endButton.OnKeyboardPress(OnEndClick, Options.keyCancel);
		_healButton.OnMouseClick(OnHealClick);
		_healButton.OnKeyboardPress(OnHealClick, SDLKey.SDLK_3);
		_stimulantButton.OnMouseClick(OnStimulantClick);
		_stimulantButton.OnKeyboardPress(OnStimulantClick, SDLKey.SDLK_2);
		_pkButton.OnMouseClick(OnPainKillerClick);
		_pkButton.OnKeyboardPress(OnPainKillerClick, SDLKey.SDLK_1);
		Update();
	}

	/// <summary>Closes the window on right-click.</summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.Handle(action);
		if (action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN && _game.IsRightClick(action))
		{
			OnEndClick(null);
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	private void OnEndClick(Action action)
	{
		if (Options.maximizeInfoScreens)
		{
			Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, true);
			// missing: _game->getScreen()->resetDisplay(false);
		}
		_game.PopState();
		_tileEngine.MedikitRemoveIfEmpty(_action);
	}

	/// <summary>Handler for clicking on the heal button.</summary>
	private void OnHealClick(Action action)
	{
		if (_item.HealQuantity == 0)
		{
			return;
		}

		if (_action.SpendTU(ref _action.result))
		{
			bool canContinueHealing = _tileEngine.MedikitUse(_action, _targetUnit, BattleMediKitAction.BMA_HEAL, (UnitBodyPart)_medikitView.SelectedPart);
			_medikitView.UpdateSelectedPart();
			_medikitView.Invalidate();
			Update();
			if (!canContinueHealing)
			{
				OnEndClick(null);
			}
		}
		else
		{
			OnEndClick(null);
		}
	}

	/// <summary>Handler for clicking on the stimulant button.</summary>
	private void OnStimulantClick(Action action)
	{
		if (_item.StimulantQuantity == 0)
		{
			return;
		}

		if (_action.SpendTU(ref _action.result))
		{
			bool canContinueHealing = _tileEngine.MedikitUse(_action, _targetUnit, BattleMediKitAction.BMA_STIMULANT, UnitBodyPart.BODYPART_TORSO);
			Update();
			if (!canContinueHealing)
			{
				OnEndClick(null);
			}
		}
		else
		{
			OnEndClick(null);
		}
	}

	/// <summary>Handler for clicking on the pain killer button.</summary>
	private void OnPainKillerClick(Action action)
	{
		if (_item.PainKillerQuantity == 0)
		{
			return;
		}

		if (_action.SpendTU(ref _action.result))
		{
			bool canContinueHealing = _tileEngine.MedikitUse(_action, _targetUnit, BattleMediKitAction.BMA_PAINKILLER, UnitBodyPart.BODYPART_TORSO);
			Update();
			if (!canContinueHealing)
			{
				OnEndClick(null);
			}
		}
		else
		{
			OnEndClick(null);
		}
	}

	/// <summary>Updates the medikit state.</summary>
	private void Update()
	{
		_pkText.SetText(_item.PainKillerQuantity.ToString(CultureInfo.InvariantCulture));
		_stimulantTxt.SetText(_item.StimulantQuantity.ToString(CultureInfo.InvariantCulture));
		_healTxt.SetText(_item.HealQuantity.ToString(CultureInfo.InvariantCulture));
		_medikitView.Invalidate();
	}
}
