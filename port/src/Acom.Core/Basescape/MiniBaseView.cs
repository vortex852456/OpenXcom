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
// Ported from: src/Basescape/MiniBaseView.h, src/Basescape/MiniBaseView.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Mini view of a base.
/// Takes all the bases and displays their layout
/// and allows players to swap between them.
/// </summary>
public class MiniBaseView : InteractiveSurface
{
	private const int MINI_SIZE = 14;

	private List<Base> _bases;
	private SurfaceSet _texture;
	private int _base, _hoverBase;
	private byte _red, _green, _blue;

	public const int MAX_BASES = 8;

	/// <summary>
	/// Sets up a mini base view with the specified size and position.
	/// </summary>
	public MiniBaseView(int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
	}

	/// <summary>
	/// Changes the current list of bases to display.
	/// </summary>
	public void SetBases(List<Base> bases)
	{
		_bases = bases;
		_redraw = true;
	}

	/// <summary>
	/// Changes the texture to use for drawing
	/// the various base elements.
	/// </summary>
	public void SetTexture(SurfaceSet texture)
	{
		_texture = texture;
	}

	/// <summary>
	/// Returns the base the mouse cursor is currently over (ID of the base).
	/// </summary>
	public int HoveredBase => _hoverBase;

	/// <summary>
	/// Changes the base that is currently selected on
	/// the mini base view.
	/// </summary>
	public void SetSelectedBase(int @base)
	{
		_base = @base;
		_redraw = true;
	}

	/// <summary>
	/// Draws the view of all the bases with facilities
	/// in varying colors.
	/// </summary>
	public override void Draw()
	{
		base.Draw();
		for (int i = 0; i < MAX_BASES; ++i)
		{
			// Draw base squares
			if (i == _base)
			{
				var r = new SDL_Rect(i * (MINI_SIZE + 2), 0, MINI_SIZE + 2, MINI_SIZE + 2);
				DrawRect(ref r, 1);
			}
			_texture.GetFrame(41).BlitNShade(this, i * (MINI_SIZE + 2), 0);

			// Draw facilities
			if (i < _bases.Count)
			{
				SDL_Rect r;
				Lock();
				foreach (var fac in _bases[i].Facilities)
				{
					int color;
					if (fac.Disabled)
						color = _blue;
					else if (fac.BuildTime == 0)
						color = _green;
					else
						color = _red;

					r.x = (short)(i * (MINI_SIZE + 2) + 2 + fac.X * 2);
					r.y = (short)(2 + fac.Y * 2);
					r.w = (ushort)(fac.Rules.SizeX * 2);
					r.h = (ushort)(fac.Rules.SizeY * 2);
					DrawRect(ref r, (byte)(color + 3));
					r.x++;
					r.y++;
					r.w--;
					r.h--;
					DrawRect(ref r, (byte)(color + 5));
					r.x--;
					r.y--;
					DrawRect(ref r, (byte)(color + 2));
					r.x++;
					r.y++;
					r.w--;
					r.h--;
					DrawRect(ref r, (byte)(color + 3));
					r.x--;
					r.y--;
					SetPixel(r.x, r.y, (byte)(color + 1));
				}
				Unlock();
			}
		}
	}

	/// <summary>
	/// Selects the base the mouse is over.
	/// </summary>
	public override void MouseOver(Action action, State state)
	{
		_hoverBase = (int)Math.Floor(action.RelativeXMouse / ((MINI_SIZE + 2) * action.XScale));
		// C++ stores this in a size_t: a negative index wraps to a huge one that matches no base
		if (_hoverBase < 0)
			_hoverBase = int.MaxValue;
		base.MouseOver(action, state);
	}

	public override void SetColor(byte color)
	{
		_green = color;
	}

	public override void SetSecondaryColor(byte color)
	{
		_red = color;
	}

	public override void SetBorderColor(byte color)
	{
		_blue = color;
	}
}
