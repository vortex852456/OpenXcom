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

namespace Acom.Placeholder;

/// <summary>What a sprite frame shows; decides the procedural shape.</summary>
public enum SpriteKind
{
	/// <summary>Round blob (icons, explosions, smoke, markers).</summary>
	Disc,
	/// <summary>Filled box with border (base facilities, items).</summary>
	Box,
	/// <summary>Standing figure (unit sprite sheets).</summary>
	Figure,
	/// <summary>Isometric terrain part, the frame index selects floor / west wall / north wall / object.</summary>
	Terrain,
	/// <summary>Outline of an isometric floor tile (battlescape cursor).</summary>
	Cursor,
	/// <summary>Opaque noise texture (globe textures, minimap tiles).</summary>
	Texture,
}

/// <summary>
/// Own palettes and simple procedural graphics: geometric shapes and hash noise on 16 colour groups x 16 shades.
/// Nothing is derived from original images; only sizes and frame counts come from the asset list.
/// </summary>
public static class Art
{
	/// <summary>
	/// Hues (degrees) of the 16 colour groups; group 0 is grey. Groups 12 and 13 form one 32-shade ocean ramp
	/// (the globe shades the ocean with oceanPalette 12 plus up to 31 shades).
	/// </summary>
	private static readonly double[] s_hues = [0, 0, 28, 50, 75, 110, 150, 175, 190, 210, 265, 300, 220, 220, 330, 200];

	/// <summary>Saturation of the 16 colour groups.</summary>
	private static readonly double[] s_saturation = [0, 0.8, 0.8, 0.8, 0.6, 0.7, 0.6, 0.6, 0.7, 0.7, 0.6, 0.6, 0.8, 0.8, 0.45, 0.25];

	/// <summary>
	/// A 256-colour palette of 16 ramps (group * 16 + shade, shade 0 = brightest, 15 = darkest) with 6-bit channels.
	/// Index 0 is black (transparent in the engine). The menu palettes (0-3) replace the groups that the
	/// interface rulesets use for text with short UI ramps (<see cref="ApplyUiRamps"/>); the battlescape palette (4)
	/// keeps plain 16-shade ramps everywhere because tiles and units are shaded within each group.
	/// </summary>
	/// <param name="variant">Palette number in PALETTES.DAT (0 geoscape, 1 basescape, 2 graphs, 3 ufopaedia, 4 battlescape); also shifts the hue a little.</param>
	public static Rgb6[] Palette(int variant)
	{
		var pal = new Rgb6[256];
		for (int g = 0; g < 16; ++g)
		{
			for (int s = 0; s < 16; ++s)
			{
				double v = g switch
				{
					12 => 1.0 - s / 33.0,
					13 => 1.0 - (16 + s) / 33.0,
					_ => 1.0 - s / 16.5,
				};
				double hue = (s_hues[g] + variant * 6) % 360;
				pal[g * 16 + s] = Hsv(hue, s_saturation[g], v);
			}
		}
		if (variant < 4)
			ApplyUiRamps(pal, variant);
		pal[0] = new Rgb6(0, 0, 0);
		return pal;
	}

	/// <summary>Brightness of the five steps of a UI ramp (text body, anti-aliasing, button face, outline, outer outline).</summary>
	private static readonly double[] s_uiValue = [1.0, 0.8, 0.58, 0.34, 0.18];

	/// <summary>
	/// Text and buttons draw colour <c>c</c> as <c>c+1</c> (glyph body, button highlight) to <c>c+5</c> (outline,
	/// button shadow), with the button face at <c>c+3</c>. A 16-shade ramp makes those five neighbours nearly the same
	/// colour, so the text colours used by the interface rulesets get their own five-step ramps from bright to dark:
	/// group 8 (text colours 128, 133, 138), group 13 outside the geoscape palette (208, 213, 218; in the geoscape
	/// palette it is the second half of the ocean ramp) and group 15 (239 and 244, in the basescape palette 241 and 246).
	/// Group 14 (window backgrounds, replaced by BACKPALS.DAT per screen) becomes a neutral dark-to-bright ramp.
	/// Globe textures, pictures and sprites avoid these groups (<see cref="ArtGroup"/>).
	/// </summary>
	private static void ApplyUiRamps(Rgb6[] pal, int variant)
	{
		UiRamps(pal, 128, [195, 45, 120], variant);
		if (variant != 0)
			UiRamps(pal, 208, [60, 185, 25], variant);
		UiRamps(pal, variant == 1 ? 241 : 239, [-1, 200, 95], variant);
		for (int s = 0; s < 16; ++s)
			pal[224 + s] = Hsv(30, 0.15, 0.08 + s / 15.0 * 0.62);
	}

	/// <summary>
	/// Three five-step ramps at <paramref name="first"/> + 1, + 6 and + 11, each in its own hue (-1 = grey), clipped
	/// to the colour group of the first ramp. <paramref name="first"/> itself (the text colour, which window borders
	/// also use) becomes a dark shade when it lies in that group; between two ramps the last step of one is the
	/// "text colour" of the next.
	/// </summary>
	private static void UiRamps(Rgb6[] pal, int first, int[] hues, int variant)
	{
		int lo = (first + 1) / 16 * 16, hi = lo + 16;
		for (int r = 0; r < hues.Length; ++r)
		{
			int c = first + r * 5;
			double hue = hues[r] < 0 ? 0 : (hues[r] + variant * 6) % 360;
			double sat = hues[r] < 0 ? 0 : 0.65;
			if (r == 0 && c >= lo)
				pal[c] = Hsv(hue, sat, 0.12);
			for (int s = 0; s < 5 && c + 1 + s < hi; ++s)
				pal[c + 1 + s] = Hsv(hue, sat * (0.3 + 0.175 * s), s_uiValue[s]);
		}
	}

	/// <summary>BACKPALS.DAT: 8 window background ramps of 16 colours, dark to bright.</summary>
	public static Rgb6[] BackPalettes()
	{
		var pal = new Rgb6[128];
		for (int r = 0; r < 8; ++r)
			for (int s = 0; s < 16; ++s)
				pal[r * 16 + s] = Hsv(r * 45 + 15, 0.55, 0.06 + s / 15.0 * 0.64);
		return pal;
	}

	/// <summary>
	/// Colour group for pictures, sprites and globe textures: the UI groups of <see cref="ApplyUiRamps"/> (8, 13, 14, 15)
	/// are replaced by plain 16-shade ramps (never group 12, which the globe treats as ocean); others stay.
	/// </summary>
	public static int ArtGroup(int group) => (((group % 16) + 16) % 16) switch
	{
		8 => 9,
		13 => 10,
		14 => 11,
		15 => 0,
		var g => g,
	};

	private static Rgb6 Hsv(double h, double s, double v)
	{
		double c = v * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - c;
		(double r, double g, double b) = (int)(h / 60) switch
		{
			0 => (c, x, 0.0),
			1 => (x, c, 0.0),
			2 => (0.0, c, x),
			3 => (0.0, x, c),
			4 => (x, 0.0, c),
			_ => (c, 0.0, x),
		};
		static byte Ch(double f) => (byte)Math.Clamp((int)Math.Round(f * 63), 0, 63);
		return new Rgb6(Ch(r + m), Ch(g + m), Ch(b + m));
	}

	/// <summary>Palette index of a group and shade; never 0 (transparent) and never 254/255 (PCK control bytes).</summary>
	public static byte Color(int group, int shade)
	{
		group = ((group % 16) + 16) % 16;
		shade = Math.Clamp(shade, group == 0 ? 1 : 0, 15);
		int c = group * 16 + shade;
		return (byte)Math.Min(c, 253);
	}

	/// <summary>Full-screen style picture: shaded noise, diagonal bands, a bevelled border and a centred emblem.</summary>
	public static Image Picture(int width, int height, uint seed, int group, bool emblem)
	{
		group = ArtGroup(group);
		var img = new Image(width, height);
		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				int band = ((x + y) / 24) % 2;
				int shade = 9 + band + Noise.Range(seed, 3, x / 2, y / 2);
				img[x, y] = Color(group, shade);
			}
		}
		Border(img, 0, 0, width, height, group, 3);
		if (emblem)
		{
			int r = Math.Min(width, height) / 4;
			int sides = 3 + (int)(seed % 5);
			Polygon(img, width / 2, height / 2, r, sides, Color(ArtGroup(group + 3), 4), Color(ArtGroup(group + 3), 1));
		}
		return img;
	}

	/// <summary>
	/// Window background (BACK*.SCR): dark noise and diagonal bands in group 14, the range that each screen
	/// recolours with its BACKPALS.DAT ramp (dark to bright), so text in the bright UI ramps stands out.
	/// </summary>
	public static Image Backdrop(int width, int height, uint seed)
	{
		var img = new Image(width, height);
		for (int y = 0; y < height; ++y)
			for (int x = 0; x < width; ++x)
				img[x, y] = Color(14, 2 + ((x + y) / 24) % 2 + Noise.Range(seed, 3, x / 2, y / 2));
		return img;
	}

	/// <summary>Draws a bevelled frame: bright top/left, dark bottom/right.</summary>
	public static void Border(Image img, int x0, int y0, int w, int h, int group, int thickness)
	{
		ArgumentNullException.ThrowIfNull(img);
		for (int t = 0; t < thickness; ++t)
		{
			for (int x = x0 + t; x < x0 + w - t; ++x)
			{
				img.Plot(x, y0 + t, Color(group, 2 + t));
				img.Plot(x, y0 + h - 1 - t, Color(group, 13 - t));
			}
			for (int y = y0 + t; y < y0 + h - t; ++y)
			{
				img.Plot(x0 + t, y, Color(group, 2 + t));
				img.Plot(x0 + w - 1 - t, y, Color(group, 13 - t));
			}
		}
	}

	/// <summary>Filled regular polygon with outline.</summary>
	public static void Polygon(Image img, int cx, int cy, int r, int sides, byte fill, byte outline)
	{
		ArgumentNullException.ThrowIfNull(img);
		for (int y = cy - r; y <= cy + r; ++y)
		{
			for (int x = cx - r; x <= cx + r; ++x)
			{
				double d = PolyDistance(x - cx, y - cy, sides);
				if (d <= r)
					img.Plot(x, y, d > r - 2 ? outline : fill);
			}
		}
	}

	private static double PolyDistance(double dx, double dy, int sides)
	{
		double a = Math.Atan2(dy, dx) + Math.PI / 2;
		double sector = 2 * Math.PI / sides;
		double local = ((a % sector) + sector) % sector - sector / 2;
		return Math.Sqrt(dx * dx + dy * dy) * Math.Cos(local) / Math.Cos(Math.PI / sides);
	}

	/// <summary>Filled ellipse with outline.</summary>
	public static void Ellipse(Image img, double cx, double cy, double rx, double ry, byte fill, byte outline)
	{
		ArgumentNullException.ThrowIfNull(img);
		for (int y = (int)(cy - ry) - 1; y <= (int)(cy + ry) + 1; ++y)
		{
			for (int x = (int)(cx - rx) - 1; x <= (int)(cx + rx) + 1; ++x)
			{
				double dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
				double d = dx * dx + dy * dy;
				if (d <= 1)
					img.Plot(x, y, d > 0.7 ? outline : fill);
			}
		}
	}

	/// <summary>Filled rectangle.</summary>
	public static void Rect(Image img, int x0, int y0, int w, int h, byte color)
	{
		ArgumentNullException.ThrowIfNull(img);
		for (int y = y0; y < y0 + h; ++y)
			for (int x = x0; x < x0 + w; ++x)
				img.Plot(x, y, color);
	}

	/// <summary>One frame of a sprite set.</summary>
	public static Image Sprite(SpriteKind kind, int width, int height, int frame, uint seed)
	{
		var img = new Image(width, height);
		int group = 1 + (int)(seed % 14);
		switch (kind)
		{
			case SpriteKind.Disc:
			{
				double r = Math.Min(width, height) * (0.2 + 0.25 * ((frame % 8) / 7.0));
				Ellipse(img, width / 2.0, height / 2.0, r, r, Color(ArtGroup(group + frame / 8), 5), Color(ArtGroup(group + frame / 8), 1));
				break;
			}
			case SpriteKind.Box:
			{
				int bw = Math.Max(4, width - 4 - (frame % 4) * 3), bh = Math.Max(4, Math.Min(height, width) - 4 - (frame % 3) * 3);
				int x0 = (width - bw) / 2, y0 = (Math.Min(height, width) - bh) / 2;
				Rect(img, x0, y0, bw, bh, Color(ArtGroup(group + frame % 5), 7));
				Border(img, x0, y0, bw, bh, ArtGroup(group + frame % 5), 1);
				break;
			}
			case SpriteKind.Figure:
			{
				// direction (frame % 8) shifts the light side, the animation phase bobs the figure
				int dir = frame % 8, phase = frame / 8 % 4;
				int g = ArtGroup(group + frame / 64), head = ArtGroup(group + frame / 64 + 2);
				double cx = width / 2.0, bob = phase % 2;
				Ellipse(img, cx, height - 4, 7, 3, Color(0, 12), Color(0, 12)); // shadow
				Rect(img, (int)cx - 5, height - 22 + (int)bob, 10, 16, Color(g, 6));
				Rect(img, (int)cx - 5 + (dir < 4 ? 0 : 5), height - 22 + (int)bob, 5, 16, Color(g, 9));
				Ellipse(img, cx, height - 27 + bob, 4, 4, Color(head, 3), Color(head, 6));
				break;
			}
			case SpriteKind.Terrain:
				TerrainPart(img, frame % 4, group + frame / 4 % 3);
				break;
			case SpriteKind.Cursor:
				Diamond(img, width / 2, height - 8, width / 2 - 1, 7, Color(1 + frame % 4 * 2, 2), outlineOnly: true);
				break;
			case SpriteKind.Texture:
				for (int y = 0; y < height; ++y)
					for (int x = 0; x < width; ++x)
						img[x, y] = Color(ArtGroup(group), 3 + Noise.Range(seed, 4, frame, x / 3, y / 3));
				break;
		}
		return img;
	}

	/// <summary>Opaque noise texture in one colour group (globe textures: shades 2..7, so the globe can still darken them).</summary>
	public static Image Texture(int width, int height, int group, int frame, uint seed)
	{
		group = ArtGroup(group);
		var img = new Image(width, height);
		for (int y = 0; y < height; ++y)
			for (int x = 0; x < width; ++x)
				img[x, y] = Color(group, 2 + Noise.Range(seed, 3, frame, x / 2, y / 2) + (x / 8 + y / 8) % 2 * 2);
		return img;
	}

	/// <summary>Isometric floor (0), west wall (1), north wall (2) or object (3) on a 32x40 tile.</summary>
	private static void TerrainPart(Image img, int part, int group)
	{
		int cx = img.Width / 2, by = img.Height - 8; // centre of the floor diamond
		int hw = img.Width / 2, hh = 8;
		switch (part)
		{
			case 0:
				Diamond(img, cx, by, hw, hh, Color(group, 8), outlineOnly: false);
				break;
			case 1:
			case 2:
			{
				// a 24 pixel high wall along the upper-left (west) or upper-right (north) edge of the floor diamond
				for (int i = 0; i <= hw; ++i)
				{
					int x = part == 1 ? cx - i : cx + i;
					int yEdge = by - hh + i / 2;
					for (int h = 0; h < 24; ++h)
						img.Plot(x, yEdge - h, Color(group + 1, h == 23 || i == 0 || i == hw ? 2 : 6 + (part - 1) * 2));
				}
				break;
			}
			default:
				Rect(img, cx - 7, by - 18, 14, 16, Color(group + 2, 7));
				Border(img, cx - 7, by - 18, 14, 16, group + 2, 1);
				break;
		}
	}

	private static void Diamond(Image img, int cx, int cy, int hw, int hh, byte color, bool outlineOnly)
	{
		for (int y = -hh; y <= hh; ++y)
		{
			int span = hw - Math.Abs(y) * hw / hh;
			for (int x = -span; x <= span; ++x)
			{
				if (!outlineOnly || Math.Abs(x) >= span - 1)
					img.Plot(cx + x, cy + y, color);
			}
		}
	}
}
