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
// Ported from: src/Geoscape/BaseDestroyedState.h, src/Geoscape/BaseDestroyedState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Screen shown when an undefended base is destroyed (or damaged by missiles).
/// </summary>
public class BaseDestroyedState : State
{
	private readonly Window _window;
	private readonly Text _txtMessage;
	private readonly TextButton _btnOk;
	private readonly TextList _lstDestroyedFacilities;
	private readonly Base _base;
	private readonly bool _missiles, _partialDestruction;

	/// <summary>
	/// Initializes all the elements in the Base Destroyed window.
	/// </summary>
	public BaseDestroyedState(Base @base, Ufo ufo, bool missiles, bool partialDestruction)
	{
		_base = @base;
		_missiles = missiles;
		_partialDestruction = partialDestruction;
		_screen = false;

		int soundId = ufo.Rules.HitSound;
		if (soundId != Mod.NO_SOUND)
		{
			_customSound = _game.Mod.GetSound("GEO.CAT", soundId);
		}

		// Create objects
		_window = new Window(this, 256, 160, 32, 20);
		_btnOk = new TextButton(100, 20, 110, 142);
		_txtMessage = new Text(224, 48, 48, _partialDestruction ? 42 : 76);
		_lstDestroyedFacilities = new TextList(208, 40, 48, 92);

		// Set palette
		SetInterface("baseDestroyed");

		Add(_window, "window", "baseDestroyed");
		Add(_btnOk, "button", "baseDestroyed");
		Add(_txtMessage, "text", "baseDestroyed");
		Add(_lstDestroyedFacilities, "text", "baseDestroyed");

		CenterAllSurfaces();

		// Set up objects
		if (ufo.Rules.HitImage.Length == 0)
		{
			SetWindowBackground(_window, "baseDestroyed");
		}
		else
		{
			SetWindowBackgroundImage(_window, ufo.Rules.HitImage);
		}

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.SetBig();
		_txtMessage.SetWordWrap(true);
		_txtMessage.SetText(Tr("STR_THE_ALIENS_HAVE_DESTROYED_THE_UNDEFENDED_BASE").Arg(_base.GetName(null)));
		if (_missiles)
		{
			if (_partialDestruction)
			{
				_txtMessage.SetText(Tr("STR_ALIEN_MISSILES_HAVE_DAMAGED_OUR_BASE").Arg(_base.GetName(null)));
			}
			else
			{
				_txtMessage.SetText(Tr("STR_ALIEN_MISSILES_HAVE_DESTROYED_OUR_BASE").Arg(_base.GetName(null)));
			}
		}

		_lstDestroyedFacilities.SetColumns(2, 162, 14);
		_lstDestroyedFacilities.SetBackground(_window);
		_lstDestroyedFacilities.SetSelectable(true);
		_lstDestroyedFacilities.Margin = 8;
		_lstDestroyedFacilities.Visible = false;

		if (_missiles && _partialDestruction)
		{
			foreach (var each in _base.DestroyedFacilitiesCache)
			{
				_lstDestroyedFacilities.AddRow(2, Tr(each.Key.Type), each.Value.ToString(CultureInfo.InvariantCulture));
			}
			_lstDestroyedFacilities.Visible = true;
		}

		if (_partialDestruction)
		{
			// don't remove the alien mission yet, there might be more attacks coming
			return;
		}

		AlienMission am = _base.RetaliationMission;
		if (am == null)
		{
			// backwards-compatibility
			RuleRegion regionRule = _game.SavedGame.Regions[0].Rules; // wrong, but that's how it is in OXC
			foreach (var region in _game.SavedGame.Regions)
			{
				if (region.Rules.InsideRegion(_base.Longitude, _base.Latitude))
				{
					regionRule = region.Rules;
					break;
				}
			}
			am = _game.SavedGame.FindAlienMission(regionRule.Type, MissionObjective.OBJECTIVE_RETALIATION);
		}
		_game.SavedGame.DeleteRetaliationMission(am, _base);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();

		if (_partialDestruction)
		{
			if (_game.SavedGame.MonthsPassed > -1 && Options.storageLimitsEnforced && _base != null && _base.StoresOverfull())
			{
				_game.PushState(new SellState(_base, null, OptionsOrigin.OPT_BATTLESCAPE));
				RuleInterface debriefing = _game.Mod.GetInterface("debriefing");
				_game.PushState(new ErrorMessageState(Tr("STR_STORAGE_EXCEEDED").Arg(_base.GetName(null)), _palette, unchecked((byte)debriefing.GetElement("errorMessage").color), "BACK01.SCR", debriefing.GetElement("errorPalette").color));
			}

			// the base was damaged, but survived
			return;
		}

		var bases = _game.SavedGame.Bases;
		for (int xbaseIt = 0; xbaseIt < bases.Count; ++xbaseIt)
		{
			Base xbase = bases[xbaseIt];
			if (xbase == _base)
			{
				_game.SavedGame.StopHuntingXcomCrafts(xbase); // destroyed together with the base
				// C++ `delete xbase`: ~Base deletes its crafts (~MovingTarget, ~Target), then ~Target of the base itself
				foreach (var xcraft in xbase.Crafts)
				{
					xcraft.Destroy();
				}
				xbase.Destroy();
				bases.RemoveAt(xbaseIt);
				break;
			}
		}
	}
}
