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

using Acom.Placeholder;

namespace Acom.Tests.Tools;

/// <summary>Placeholder palettes: engine shading ramps stay monotonic, interface text stays readable.</summary>
public sealed class PlaceholderPaletteTests
{
	private static double Luminance(Rgb6 c)
	{
		static double Lin(byte v)
		{
			double s = v / 63.0;
			return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
		}
		return 0.2126 * Lin(c.R) + 0.7152 * Lin(c.G) + 0.0722 * Lin(c.B);
	}

	private static double Contrast(Rgb6 a, Rgb6 b)
	{
		double la = Luminance(a), lb = Luminance(b);
		return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
	}

	private static void AssertDarkening(Rgb6[] pal, int first, int count)
	{
		for (int i = first + 1; i < first + count; ++i)
			Assert.True(Luminance(pal[i]) < Luminance(pal[i - 1]), $"index {i} is not darker than {i - 1}");
	}

	[Fact]
	public void Battlescape_palette_keeps_every_group_a_shading_ramp()
	{
		var pal = Art.Palette(4);
		for (int g = 0; g < 16; ++g)
			AssertDarkening(pal, g == 0 ? 1 : g * 16, g == 0 ? 15 : 16);
	}

	[Fact]
	public void Geoscape_palette_keeps_ocean_and_art_ramps()
	{
		var pal = Art.Palette(0);
		AssertDarkening(pal, 192, 32); // Globe::OCEAN_COLOR + 0..31
		for (int g = 0; g < 16; ++g)
		{
			int art = Art.ArtGroup(g);
			Assert.False(art is 8 or 13 or 14 or 15, $"group {g} maps to UI group {art}");
			Assert.True(art != 12 || g == 12, $"group {g} maps to the ocean group");
			AssertDarkening(pal, art == 0 ? 1 : art * 16, art == 0 ? 15 : 16);
		}
	}

	[Theory]
	[InlineData(0, 133)]
	[InlineData(0, 138)]
	[InlineData(0, 239)]
	[InlineData(0, 244)]
	[InlineData(1, 133)]
	[InlineData(1, 208)]
	[InlineData(1, 213)]
	[InlineData(1, 218)]
	[InlineData(1, 241)]
	[InlineData(1, 246)]
	[InlineData(3, 239)]
	[InlineData(3, 244)]
	public void Interface_text_colours_have_readable_glyphs(int variant, int color)
	{
		var pal = Art.Palette(variant);
		// Text draws glyph pixel n as color + n: body 1, outline 4/5; TextButton fills its face with color + 3.
		Assert.True(Contrast(pal[color + 1], pal[color + 4]) >= 4.0, "glyph body vs outline");
		Assert.True(Contrast(pal[color + 1], pal[color + 3]) >= 2.0, "glyph body vs button face");
		// Window backgrounds use the dark end of the BACKPALS ramps (group 14).
		var back = Art.BackPalettes();
		for (int r = 0; r < 8; ++r)
			Assert.True(Contrast(pal[color + 1], back[r * 16 + 5]) >= 4.5, $"glyph body vs background ramp {r}");
	}

	[Fact]
	public void Palettes_are_deterministic()
	{
		for (int v = 0; v < 5; ++v)
			Assert.Equal(Art.Palette(v), Art.Palette(v));
	}
}
