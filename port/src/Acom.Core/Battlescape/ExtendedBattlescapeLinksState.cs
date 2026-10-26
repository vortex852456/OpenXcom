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
// Ported from: src/Battlescape/ExtendedBattlescapeLinksState.h, src/Battlescape/ExtendedBattlescapeLinksState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Window with additional battlescape links (OXCE).
/// </summary>
public class ExtendedBattlescapeLinksState : State
{
	private readonly TextButton _btnOk;
	private readonly TextButton _btnTouch, _btnNightVision, _btnPersonalLights, _btnBrightness, _btnTurnDiary, _btnBriefing, _btnNotes, _btnMusic;
	private readonly TextButton _btnKillAll;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly BattlescapeState _parent;
	private readonly SavedBattleGame _save;

	/// <summary>Initializes all the elements in the ExtendedBattlescapeLinksState screen.</summary>
	public ExtendedBattlescapeLinksState(BattlescapeState parent, SavedBattleGame save)
	{
		_parent = parent;
		_save = save;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 180, 32, 10, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(220, 17, 50, 33);
		if (Options.oxceFatFingerLinks)
		{
			_btnTouch = new TextButton(116, 25, 44, 50);
			_btnNightVision = new TextButton(116, 25, 161, 50);
			_btnPersonalLights = new TextButton(116, 25, 44, 76);
			_btnBrightness = new TextButton(116, 25, 161, 76);
			_btnTurnDiary = new TextButton(116, 25, 44, 102);
			_btnBriefing = new TextButton(116, 25, 161, 102);
			_btnNotes = new TextButton(116, 25, 44, 128);
			_btnMusic = new TextButton(116, 25, 161, 128);
			_btnKillAll = new TextButton(116, 25, 44, 154);
			_btnOk = new TextButton(116, 25, 161, 154);
		}
		else
		{
			_btnTouch = new TextButton(220, 12, 50, 50);
			_btnNightVision = new TextButton(220, 12, 50, 63);
			_btnPersonalLights = new TextButton(220, 12, 50, 76);
			_btnBrightness = new TextButton(220, 12, 50, 89);
			_btnTurnDiary = new TextButton(220, 12, 50, 102);
			_btnBriefing = new TextButton(220, 12, 50, 115);
			_btnNotes = new TextButton(220, 12, 50, 128);
			_btnMusic = new TextButton(220, 12, 50, 141);
			_btnKillAll = new TextButton(220, 12, 50, 154);
			_btnOk = new TextButton(220, 12, 50, 167);
		}

		// Set palette
		SetInterface("oxceLinks", false, _save);

		Add(_window, "window", "oxceLinks");
		Add(_txtTitle, "text", "oxceLinks");
		Add(_btnOk, "button", "oxceLinks");

		Add(_btnTouch, "button", "oxceLinks");
		Add(_btnNightVision, "button", "oxceLinks");
		Add(_btnPersonalLights, "button", "oxceLinks");
		Add(_btnBrightness, "button", "oxceLinks");
		Add(_btnTurnDiary, "button", "oxceLinks");
		Add(_btnBriefing, "button", "oxceLinks");
		Add(_btnNotes, "button", "oxceLinks");
		Add(_btnMusic, "button", "oxceLinks");
		Add(_btnKillAll, "button", "oxceLinks");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "oxceLinks");

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_EXTENDED_LINKS"));

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnTouch.Text = Tr("STR_TOGGLE_TOUCH_BUTTONS");
		_btnTouch.OnMouseClick(BtnTouchClick);

		_btnNightVision.Text = Tr("STR_TOGGLE_NIGHT_VISION");
		_btnNightVision.OnMouseClick(BtnNightVisionClick);

		_btnPersonalLights.Text = Tr("STR_TOGGLE_PERSONAL_LIGHTING");
		_btnPersonalLights.OnMouseClick(BtnPersonalLightsClick);

		_btnBrightness.Text = Tr("STR_TOGGLE_BRIGHTNESS");
		_btnBrightness.OnMouseClick(BtnBrightnessClick);

		_btnTurnDiary.Text = Tr("STR_HIT_LOG");
		_btnTurnDiary.OnMouseClick(BtnTurnDiaryClick);

		_btnBriefing.Text = Tr("STR_BRIEFING");
		_btnBriefing.OnMouseClick(BtnBriefingClick);

		_btnNotes.Text = Options.oxceReplaceNotesLink ? Tr("STR_NO_EXPERIENCE_YET") : Tr("STR_NOTES");
		_btnNotes.OnMouseClick(BtnNotesClick);

		_btnMusic.Text = Tr("STR_SELECT_MUSIC_TRACK");
		_btnMusic.OnMouseClick(BtnMusicClick);

		if (Options.debug)
		{
			_btnKillAll.Text = _save.DebugMode ? Tr("STR_DEBUG_KILL_ALL_ALIENS") : Tr("STR_TOGGLE_DEBUG_MODE");
		}
		else
		{
			_btnKillAll.Text = Tr("STR_MULTI_LEVEL_VIEW");
		}
		_btnKillAll.OnMouseClick(BtnKillAllClick);

		ApplyBattlescapeTheme("oxceLinks");
	}

	public void BtnTouchClick(Action action)
	{
		_game.PopState();
		_parent.ToggleTouchButtons(false, false);
	}

	public void BtnNightVisionClick(Action action)
	{
		_game.PopState();
		_parent.BtnNightVisionClick(null);
	}

	public void BtnPersonalLightsClick(Action action)
	{
		_game.PopState();
		_parent.BtnPersonalLightingClick(null);
	}

	public void BtnBrightnessClick(Action action)
	{
		_game.PopState();
		_parent.Map.ToggleDebugVisionMode();
	}

	public void BtnTurnDiaryClick(Action action)
	{
		_game.PopState();
		if (Options.oxceDisableHitLog)
		{
			_game.PushState(new InfoboxState(Tr("STR_THIS_FEATURE_IS_DISABLED_4")));
		}
		else
		{
			// turn diary
			_game.PushState(new TurnDiaryState(_save.HitLog));
		}
	}

	public void BtnBriefingClick(Action action)
	{
		_game.PopState();
		_game.PushState(new BriefingState(null, null, true));
	}

	public void BtnNotesClick(Action action)
	{
		_game.PopState();

		if (Options.oxceReplaceNotesLink)
			_game.PushState(new NoExperienceState());
		else
			_game.PushState(new NotesState(OptionsOrigin.OPT_BATTLESCAPE));
	}

	public void BtnMusicClick(Action action)
	{
		_game.PopState();
		_parent.BtnSelectMusicTrackClick(null);
	}

	public void BtnKillAllClick(Action action)
	{
		_game.PopState();

		if (!Options.debug)
		{
			_parent.BtnShowLayersClickOrig(null);
			return;
		}

		if (_save.DebugMode)
		{
			// kill all
			_parent.Debug("Influenza bacterium dispersed");
			foreach (BattleUnit unit in _save.Units)
			{
				if (unit.OriginalFaction == UnitFaction.FACTION_HOSTILE && !unit.IsOut)
				{
					unit.Damage(new Position(0, 0, 0), 1000, _game.Mod.GetDamageType(ItemDamageType.DT_MELEE), _save, new BattleActionAttack());
				}
			}
			_save.BattleGame.CheckForCasualties(null, new BattleActionAttack(), true, false);
			_save.BattleGame.HandleState();
		}
		else
		{
			// enable debug mode
			_save.SetDebugMode();
			_parent.Debug("Debug Mode");
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
}
