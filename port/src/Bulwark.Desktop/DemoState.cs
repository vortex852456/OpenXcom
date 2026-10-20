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

using Bulwark.Engine;

namespace Bulwark.Desktop;

/// <summary>
/// Temporary start screen until the menu states are ported. Shows the rendering model:
/// a classic-density background (upscaled), an HD-density emblem and an interactive button.
/// All colours come from a generated palette; no original game assets are used.
/// </summary>
internal sealed class DemoState : State
{
	private readonly DemoButton _button;
	private int _tick;
	private readonly Surface _emblem;

	public DemoState()
	{
		SetStatePalette(CreateRampPalette());

		// Classic density: 1 buffer pixel per logical pixel, upscaled 4x on the screen.
		var background = new Surface(Screen.ORIGINAL_WIDTH, Screen.ORIGINAL_HEIGHT, 0, 0, scale: 1);
		for (int y = 0; y < background.Height; ++y)
			background.DrawRect(0, (short)y, (short)background.Width, 1, (byte)(0x80 + 15 - y * 12 / background.Height));
		for (int x = 0; x < background.Width; x += 20)
			background.DrawRect((short)x, 0, 1, (short)background.Height, 0x8C);
		Add(background);

		// HD density: drawn directly at 4 buffer pixels per logical pixel.
		_emblem = new Surface(96, 96, 112, 30, scale: 4);
		DrawEmblem(_emblem);
		Add(_emblem);

		_button = new DemoButton(96, 20, 112, 150);
		_button.OnMouseClick(_ => _button.NextColour());
		_button.OnMouseIn(_ => _button.Hover = true);
		_button.OnMouseOut(_ => _button.Hover = false);
		_button.OnKeyboardPress(_ => _game.Quit(), SDLKey.SDLK_ESCAPE);
		_button.Invalidate();
		Add(_button);
	}

	public override void Think()
	{
		base.Think();
		// Slowly cycle the emblem's colour group to show palette-based recolouring.
		if (++_tick % 120 == 0)
			DrawEmblem(_emblem, (_tick / 120) % 4);
	}

	/// <summary>16 colour groups x 16 shades, shade 0 = brightest, as the engine expects.</summary>
	private static Rgba[] CreateRampPalette()
	{
		(double R, double G, double B)[] hues =
		[
			(0.95, 0.95, 0.95), (0.95, 0.35, 0.30), (0.98, 0.65, 0.25), (0.98, 0.88, 0.35),
			(0.45, 0.85, 0.40), (0.30, 0.80, 0.75), (0.35, 0.60, 0.95), (0.55, 0.45, 0.95),
			(0.20, 0.35, 0.60), (0.85, 0.45, 0.80), (0.70, 0.55, 0.40), (0.55, 0.65, 0.55),
			(0.60, 0.60, 0.70), (0.90, 0.75, 0.60), (0.40, 0.75, 0.95), (0.75, 0.90, 0.50),
		];
		var palette = new Rgba[256];
		for (int g = 0; g < 16; ++g)
		{
			for (int s = 0; s < 16; ++s)
			{
				double light = 1.0 - s / 16.0;
				var (r, gr, b) = hues[g];
				palette[g * 16 + s] = new Rgba((byte)(r * light * 255), (byte)(gr * light * 255), (byte)(b * light * 255));
			}
		}
		palette[0] = new Rgba(0, 0, 0, 0);
		return palette;
	}

	private static readonly int[] s_emblemGroups = [0x40, 0x60, 0x20, 0x90];

	/// <summary>Draws a shaded ring at full buffer resolution.</summary>
	private static void DrawEmblem(Surface s, int variant = 0)
	{
		var pixels = s.Pixels;
		int size = s.PhysicalWidth;
		double c = size / 2.0, outer = size * 0.48, inner = size * 0.30;
		int group = s_emblemGroups[variant];
		for (int y = 0; y < size; ++y)
		{
			for (int x = 0; x < size; ++x)
			{
				double dx = x + 0.5 - c, dy = y + 0.5 - c;
				double d = Math.Sqrt(dx * dx + dy * dy);
				byte value = 0;
				if (d <= outer && d >= inner)
				{
					// Light from the top left: darker shades towards the bottom right.
					double t = (dx + dy) / (2 * outer) * 0.5 + 0.5;
					value = (byte)(group + Math.Clamp((int)(t * 12) + 1, 1, 14));
				}
				pixels[y * size + x] = value;
			}
		}
	}

	private sealed class DemoButton(int width, int height, int x, int y) : InteractiveSurface(width, height, x, y)
	{
		private int _colour = 6;
		private bool _hover;

		public bool Hover
		{
			get => _hover;
			set
			{
				_hover = value;
				Invalidate();
			}
		}

		public void NextColour()
		{
			_colour = _colour % 14 + 1;
			Invalidate();
		}

		public override void Draw()
		{
			base.Draw();
			byte face = (byte)(_colour * 16 + (_hover ? 2 : 5));
			DrawRect(0, 0, (short)Width, (short)Height, (byte)(_colour * 16 + 12));
			DrawRect(1, 1, (short)(Width - 2), (short)(Height - 2), face);
		}
	}
}
