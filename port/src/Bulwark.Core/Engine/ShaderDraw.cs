/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 Bulwark contributors.
 *
 * This file is part of Bulwark, a C# port of OpenXcom Extended.
 *
 * Bulwark is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Bulwark is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Bulwark.  If not, see <http://www.gnu.org/licenses/>.
 */
// Ported from: src/Engine/ShaderDraw.h (helper::StandardShade, helper::ColorReplace)
// The generic ShaderDraw templates are replaced by the blit loop in Surface.BlitPixels.

namespace Bulwark.Engine;

/// <summary>
/// Per-pixel operations used by <see cref="Surface.BlitNShade(Surface, int, int, int, bool, int)"/>.
/// Palettes are organised as 16 colour groups of 16 shades each.
/// </summary>
public static class ShaderDrawHelper
{
	public const byte ColorGroup = 0xF0;
	public const byte ColorShade = 0x0F;

	/// <summary>Applies a shade to a pixel; index 0 is transparent.</summary>
	public static void StandardShade(ref byte dest, byte src, int shade)
	{
		if (src != 0)
		{
			byte newShade = unchecked((byte)(src + shade));
			if (((newShade ^ src) & ColorGroup) != 0)
				// so dark it would flip over to another color - make it black instead
				dest = ColorShade;
			else
				dest = newShade;
		}
	}

	/// <summary>Applies a shade and moves the pixel into another colour group (already shifted by 4).</summary>
	public static void ColorReplace(ref byte dest, byte src, int shade, int newColor)
	{
		if (src != 0)
		{
			byte newShade = unchecked((byte)((src & ColorShade) + shade));
			if ((newShade & ColorGroup) != 0)
				// so dark it would flip over to another color - make it black instead
				dest = ColorShade;
			else
				dest = unchecked((byte)(newColor | newShade));
		}
	}
}
