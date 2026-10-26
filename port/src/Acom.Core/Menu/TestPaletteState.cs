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
// Ported from: src/Menu/TestPaletteState.h, src/Menu/TestPaletteState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

public enum PaletteActionType : uint
{
	PAT_PREVIEW = 0,
	PAT_TINY_BORDERLESS = 1,
	PAT_TINY_BORDER = 2,
	PAT_SMALL_LOW = 3,
	PAT_SMALL_HIGH = 4,
	PAT_BIG_LOW = 5,
	PAT_BIG_HIGH = 6,
}

/// <summary>Shows the colours of a palette (as swatches or as numbered text in its colours).</summary>
public class TestPaletteState : State
{
	private readonly Surface _bg;
	private readonly TextButton _btnCancel;

	/// <summary>Creates the Test state.</summary>
	public TestPaletteState(string palette, PaletteActionType action)
	{
		// Create objects
		_bg = new Surface(320, 200, 0, 0);
		_btnCancel = new TextButton(60, 9, 240, 190);

		// Set palette
		SetStandardPalette(palette);
		int maxColors = _game.Mod.GetPalette(palette).ColorCount;

		if (_game.IsCtrlPressed())
		{
			// export the palette
			string exportName = palette + ".jasc.pal";
			_game.Mod.GetPalette(palette).SavePalJasc(exportName);
		}

		Add(_bg);
		Add(_btnCancel);

		CenterAllSurfaces();

		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		// basic palette
		if (action == PaletteActionType.PAT_PREVIEW)
		{
			var surf = new Surface(20, 11, 0, 0);
			surf.SetPalette(_bg.Palette);
			for (int row = 0; row < 16; ++row)
			{
				for (int column = 0; column < 16; ++column)
				{
					int index = row * 16 + column;
					if (index >= maxColors) return;
					surf.DrawRect(0, 0, 20, 11, (byte)index);
					surf.BlitNShade(_bg, column * 20, row * 11);
				}
			}
			return;
		}

		// small digits without/with border
		if (action == PaletteActionType.PAT_TINY_BORDERLESS || action == PaletteActionType.PAT_TINY_BORDER)
		{
			var text = new NumberText(25, 9, 0, 0);
			text.SetPalette(_bg.Palette);
			text.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
			text.SetBordered(action == PaletteActionType.PAT_TINY_BORDER);
			for (int row = 0; row < 22; ++row)
			{
				for (int column = 0; column < 12; ++column)
				{
					int index = row * 12 + column;
					if (index >= maxColors) return;
					text.SetColor((byte)index);
					text.X = column * 26;
					text.Y = row * 9;
					text.Value = (uint)index;
					text.Blit(_bg);
				}
			}
			return;
		}

		// big text without/with high contrast
		if (action == PaletteActionType.PAT_BIG_LOW || action == PaletteActionType.PAT_BIG_HIGH)
		{
			var text = new Text(13, 17, 0, 0);
			text.SetPalette(_bg.Palette);
			text.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
			text.SetHighContrast(action == PaletteActionType.PAT_BIG_HIGH);
			text.SetBig();
			for (int row = 0; row < 11; ++row)
			{
				for (int column = 0; column < 24; ++column)
				{
					int index = row * 24 + column;
					if (index >= maxColors) return;
					text.SetColor((byte)index);
					text.X = column * 13;
					text.Y = row * 17;
					text.SetText((index % 10).ToString(CultureInfo.InvariantCulture));
					text.Blit(_bg);
				}
			}
			return;
		}

		// normal text without/with high contrast
		{
			var text = new Text(25, 9, 0, 0);
			text.SetPalette(_bg.Palette);
			text.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
			text.SetHighContrast(action == PaletteActionType.PAT_SMALL_HIGH);
			for (int row = 0; row < 22; ++row)
			{
				for (int column = 0; column < 12; ++column)
				{
					int index = row * 12 + column;
					if (index >= maxColors) return;
					text.SetColor((byte)index);
					text.X = column * 26;
					text.Y = row * 9;
					text.SetText(index.ToString(CultureInfo.InvariantCulture));
					text.Blit(_bg);
				}
			}
		}
	}

	/// <summary>Returns to the previous screen.</summary>
	/// <param name="action">Pointer to an action.</param>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}
}
