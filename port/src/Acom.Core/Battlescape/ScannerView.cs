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
// Ported from: src/Battlescape/ScannerView.h, src/Battlescape/ScannerView.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Displays a view of units movement.
/// </summary>
public class ScannerView : InteractiveSurface
{
	private readonly Game _game;
	private readonly BattleUnit _unit;
	private int _frame;

	/// <summary>Initializes the Scanner view.</summary>
	/// <param name="w">The ScannerView width.</param>
	/// <param name="h">The ScannerView height.</param>
	/// <param name="x">The ScannerView x origin.</param>
	/// <param name="y">The ScannerView y origin.</param>
	/// <param name="game">Pointer to the core game.</param>
	/// <param name="unit">The current unit.</param>
	public ScannerView(int w, int h, int x, int y, Game game, BattleUnit unit) : base(w, h, x, y)
	{
		_game = game;
		_unit = unit;
		_frame = 0;
		_redraw = true;
	}

	/// <summary>Draws the ScannerView view.</summary>
	public override void Draw()
	{
		SurfaceSet set = _game.Mod.GetSurfaceSet("DETBLOB.DAT");
		Surface surface;

		Clear();

		Lock();
		SavedBattleGame battle = _game.SavedGame.SavedBattle;
		for (int x = -9; x < 10; x++)
		{
			for (int y = -9; y < 10; y++)
			{
				for (int z = 0; z < battle.MapSizeZ; z++)
				{
					Tile t = battle.GetTile(new Position(x, y, z) + new Position(_unit.Position.x, _unit.Position.y, 0));
					if (t != null && t.Unit != null && t.Unit.MotionPoints != 0)
					{
						int frame = t.Unit.MotionPoints / 5;
						if (frame >= 0)
						{
							t.Unit.ScannedTurn = battle.Turn;
							if (frame > 5) frame = 5;
							surface = set.GetFrame(frame + _frame);
							surface.BlitNShade(this, ((9 + x) * 8) - 4, ((9 + y) * 8) - 4, 0);
						}
					}
				}
			}
		}

		// the arrow of the direction the unit is pointed
		surface = set.GetFrame(7 + _unit.Direction);

		surface.BlitNShade(this, (9 * 8) - 4, (9 * 8) - 4, 0);
		Unlock();
	}

	/// <summary>Handles clicks on the scanner view (does nothing).</summary>
	public override void MouseClick(Action action, State state)
	{
	}

	/// <summary>Updates the scanner animation.</summary>
	public void Animate()
	{
		_frame++;
		if (_frame > 1)
		{
			_frame = 0;
		}
		_redraw = true;
	}
}
