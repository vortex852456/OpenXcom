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
// Ported from: src/Battlescape/MedikitView.h, src/Battlescape/MedikitView.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>
/// Display a view of unit wounds.
/// </summary>
public class MedikitView : InteractiveSurface
{
	private readonly Game _game;
	private int _selectedPart;
	private readonly BattleUnit _unit;
	private readonly Text _partTxt, _woundTxt;

	/// <summary>Initializes the Medikit view.</summary>
	/// <param name="w">The MinikitView width.</param>
	/// <param name="h">The MinikitView height.</param>
	/// <param name="x">The MinikitView x origin.</param>
	/// <param name="y">The MinikitView y origin.</param>
	/// <param name="game">Pointer to the core game.</param>
	/// <param name="unit">The wounded unit.</param>
	/// <param name="partTxt">A pointer to a Text. Will be updated with the selected body part.</param>
	/// <param name="woundTxt">A pointer to a Text. Will be updated with the amount of fatal wound.</param>
	public MedikitView(int w, int h, int x, int y, Game game, BattleUnit unit, Text partTxt, Text woundTxt) : base(w, h, x, y)
	{
		_game = game;
		_selectedPart = 0;
		_unit = unit;
		_partTxt = partTxt;
		_woundTxt = woundTxt;
		UpdateSelectedPart();
		_redraw = true;
	}

	/// <summary>Draws the medikit view.</summary>
	public override void Draw()
	{
		SurfaceSet set = _game.Mod.GetSurfaceSet("MEDIBITS.DAT");
		int fatal_wound = _unit.GetFatalWound((UnitBodyPart)_selectedPart);
		int green = 0;
		int red = 3;
		if (_game.Mod.GetInterface("medikit", false) != null && _game.Mod.GetInterface("medikit").GetElementOptional("body") != null)
		{
			green = _game.Mod.GetInterface("medikit").GetElement("body").color;
			red = _game.Mod.GetInterface("medikit").GetElement("body").color2;
		}
		Lock();
		for (int i = 0; i < set.TotalFrames; i++)
		{
			int wound = _unit.GetFatalWound((UnitBodyPart)i);
			Surface surface = set.GetFrame(i);
			int baseColor = wound != 0 ? red : green;
			surface.BlitNShade(this, 0, 0, 0, false, baseColor);
		}
		Unlock();

		_redraw = false;
		if (_selectedPart == -1)
		{
			return;
		}
		_partTxt.SetText(_game.Language.GetString(BattleUnit.PARTS_STRING[_selectedPart]));
		_woundTxt.SetText(fatal_wound.ToString(CultureInfo.InvariantCulture));
	}

	/// <summary>Handles clicks on the medikit view.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void MouseClick(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		SurfaceSet set = _game.Mod.GetSurfaceSet("MEDIBITS.DAT");
		int x = (int)(action.RelativeXMouse / action.XScale);
		int y = (int)(action.RelativeYMouse / action.YScale);
		for (int i = 0; i < set.TotalFrames; i++)
		{
			Surface surface = set.GetFrame(i);
			if (surface.GetPixel(x, y) != 0)
			{
				_selectedPart = i;
				_redraw = true;
				break;
			}
		}
	}

	/// <summary>Gets the selected body part.</summary>
	public int SelectedPart { get => _selectedPart; }

	/// <summary>
	/// Updates the selected body part.
	/// If there is a wounded body part, selects that.
	/// Otherwise does not change the selected part.
	/// </summary>
	public void UpdateSelectedPart()
	{
		for (int i = 0; i < (int)UnitBodyPart.BODYPART_MAX; ++i)
		{
			if (_unit.GetFatalWound((UnitBodyPart)i) != 0)
			{
				_selectedPart = i;
				break;
			}
		}
	}
}
