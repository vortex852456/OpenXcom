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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Acom.Desktop;

using GameTime = Microsoft.Xna.Framework.GameTime; // Acom.GameTime is the in-game clock

/// <summary>
/// MonoGame host: owns the window and main loop, feeds input into the engine and
/// presents the engine's indexed screen as a GPU texture (nearest-neighbour, letterboxed).
/// </summary>
internal sealed class AcomHost : Microsoft.Xna.Framework.Game
{
	/// <summary>Pixel density of the screen buffer: 4 = HD (1280x800 for the 320x200 layout).</summary>
	private const int RenderScale = 4;

	private readonly GraphicsDeviceManager _graphics;
	private readonly MonoGameAudioDevice _audio = new();
	private SdlInput? _input;
	private readonly HostOptions _options;
	private Acom.Game? _game;
	private Texture2D? _texture;
	private SpriteBatch? _spriteBatch;
	private byte[] _upload = [];
	private long _uploadedFrame = -1;
	private int _frames;

	public AcomHost(HostOptions options)
	{
		_options = options;
		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = Screen.ORIGINAL_WIDTH * RenderScale,
			PreferredBackBufferHeight = Screen.ORIGINAL_HEIGHT * RenderScale,
			SynchronizeWithVerticalRetrace = true,
		};
		IsMouseVisible = false; // the game draws its own cursor (Interface/Cursor); C++ shows an invisible hardware cursor
		IsFixedTimeStep = true;
		TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60);
		Window.AllowUserResizing = true;
		Window.Title = "ACOM";
	}

	protected override void Initialize()
	{
		Surface.DefaultScale = RenderScale;
		var screen = new Screen(Screen.ORIGINAL_WIDTH, Screen.ORIGINAL_HEIGHT, RenderScale);
		// No options.cfg is loaded yet: start from the defaults of Options::create (audio format, volumes, ...)
		Options.Create();
		Options.ResetDefault(false);
		Mixer.Device = _audio; // Game.InitAudio opens it (or runs muted without audio hardware)
		_game = new Acom.Game(screen);
		_game.SetState(new DemoState());
		_input = new SdlInput();
		Window.ClientSizeChanged += (_, _) => UpdateWindowSize();
		base.Initialize();
		UpdateWindowSize();
	}

	protected override void LoadContent()
	{
		var surface = _game!.Screen.Surface;
		_texture = new Texture2D(GraphicsDevice, surface.PhysicalWidth, surface.PhysicalHeight, false, SurfaceFormat.Color);
		_upload = new byte[surface.PhysicalWidth * surface.PhysicalHeight * 4];
		_spriteBatch = new SpriteBatch(GraphicsDevice);
	}

	protected override void UnloadContent()
	{
		Mixer.CloseAudio();
		Mixer.Device = null;
		_audio.Dispose();
		_input?.Dispose();
		_texture?.Dispose();
		_spriteBatch?.Dispose();
	}

	private void UpdateWindowSize()
	{
		var bounds = Window.ClientBounds;
		if (_game != null && bounds.Width > 0 && bounds.Height > 0)
			_game.Screen.SetWindowSize(bounds.Width, bounds.Height);
	}

	protected override void Update(GameTime gameTime)
	{
		var game = _game!;
		_input!.Flush(game);
		game.Update();
		if (game.IsQuitting)
			Exit();
		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		var screen = _game!.Screen;
		_game.Render();
		if (screen.FrameNumber != _uploadedFrame)
		{
			screen.Frame.CopyTo(_upload);
			_texture!.SetData(_upload);
			_uploadedFrame = screen.FrameNumber;
		}

		GraphicsDevice.Clear(Color.Black);
		var (w, h) = screen.DisplaySize;
		_spriteBatch!.Begin(samplerState: SamplerState.PointClamp);
		_spriteBatch.Draw(_texture, new Rectangle(screen.CursorLeftBlackBand, screen.CursorTopBlackBand, w, h), Color.White);
		_spriteBatch.End();
		base.Draw(gameTime);

		if (_options.ScreenshotPath != null && ++_frames == _options.ScreenshotAfterFrames)
		{
			using (var file = File.Create(_options.ScreenshotPath))
				_texture!.SaveAsPng(file, _texture.Width, _texture.Height);
			Exit();
		}
	}
}

/// <summary>Command line options of the desktop host.</summary>
internal sealed record HostOptions(string? ScreenshotPath, int ScreenshotAfterFrames)
{
	/// <summary>Parses <c>--screenshot &lt;file.png&gt; [--frames &lt;n&gt;]</c> (used for automated checks).</summary>
	public static HostOptions Parse(string[] args)
	{
		string? screenshot = null;
		int frames = 30;
		for (int i = 0; i + 1 < args.Length; ++i)
		{
			if (args[i] == "--screenshot")
				screenshot = args[++i];
			else if (args[i] == "--frames")
				frames = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
		}
		return new HostOptions(screenshot, frames);
	}
}
