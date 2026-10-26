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
// Ported from: src/Battlescape/Map.h, src/Battlescape/Map.cpp
#nullable disable

using System.Runtime.InteropServices;
using System.Text;
using static Acom.BattleActionType;
using static Acom.TilePart;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

/*
  1) Map origin is top corner.
  2) X axis goes downright. (width of the map)
  3) Y axis goes downleft. (length of the map
  4) Z axis goes up (height of the map)

           0,0
            /\
           /  \
        y+ \  / x+
            \/

  Compass directions

         W  /\  N
           /  \
           \  /
         S  \/  E

  Unit directions

         6  /\  0
           /  \
           \  /
         4  \/  2

  Big units parts

            /\
           /0 \
          /\  /\
         /2 \/1 \
         \  /\  /
          \/3 \/
           \  /
            \/
 */

public enum CursorType : uint
{
	CT_NONE = 0,
	CT_NORMAL = 1,
	CT_AIM = 2,
	CT_PSI = 3,
	CT_WAYPOINT = 4,
	CT_THROW = 5,
}

/// <summary>Helper class that returns all important data about the unit movement.</summary>
public struct UnitWalkingOffset
{
	public Position ScreenOffset;
	public int NormalizedMovePhase;
	public int TerrainLevelOffset;
}

/// <summary>
/// Interactive map of the battlescape.
/// </summary>
/// <remarks>
/// HD: all positions are logical pixels; the map surface and the sprites may have any <see cref="Surface.Scale"/>,
/// blits map between pixel densities and shading/LUTs work on palette indices as in the original.
/// </remarks>
public class Map : InteractiveSurface
{
	private const int SCROLL_INTERVAL = 15;
	private const int FADE_INTERVAL = 23;
	private const int NIGHT_VISION_SHADE = 4;
	private const int NIGHT_VISION_MAX_SHADE = 8;
	private const int BULLET_SPRITES = 35;

	private static readonly List<byte> DummyTransparencies = [];

	private readonly Timer _scrollMouseTimer, _scrollKeyTimer, _obstacleTimer;
	private readonly Timer _fadeTimer;
	private int _fadeShade;
	private bool _nightVisionOn;
	private int _debugVisionMode;
	private int _nvColor;
	private readonly Game _game;
	private readonly SavedBattleGame _save;
	private readonly bool _isTFTD;
	private Surface _arrow;
	private readonly Surface _stunIndicator, _woundIndicator, _burnIndicator, _shockIndicator;
	private readonly bool _anyIndicator;
	private bool _isAltPressed, _isCtrlPressed;
	private readonly int _spriteWidth, _spriteHeight;
	private int _selectorX, _selectorY;
	private int _mouseX, _mouseY;
	private CursorType _cursorType;
	private int _cursorSize;
	private int _cacheActiveWeaponUfopediaArticleUnlocked; // -1 = unknown, 0 = locked, 1 = unlocked
	private bool _cacheIsCtrlPressed;
	private Position _cacheCursorPosition;
	private int _cacheHasLOS; // -1 = unknown, 0 = no LOS, 1 = has LOS
	private int _animFrame;
	private Projectile _projectile;
	private bool _followProjectile;
	private bool _projectileInFOV;
	private readonly List<Explosion> _explosions = [];
	private readonly List<List<Particle>> _vaporParticlesInit = [];
	private readonly List<List<Particle>> _vaporParticles = [];
	private bool _explosionInFOV, _launch;
	private readonly BattlescapeMessage _message;
	private readonly Camera _camera;
	private int _visibleMapHeight;
	private readonly List<Position> _waypoints = [];
	private bool _unitDying, _smoothingEngaged, _flashScreen;
	private readonly bool _smoothCamera;
	private readonly int _bgColor;
	private readonly bool _previewSettingArrows, _previewSettingTu, _previewSettingEnergy;
	private readonly Text _txtAccuracy;
	private SurfaceSet _projectileSet;
	private readonly int _iconHeight, _iconWidth, _messageColor;
	private readonly int _hostileBarColor, _neutralBarColor, _borderBarColor;
	private readonly List<byte> _transparencies;
	private bool _showObstacles;
	private readonly bool _showInfoOnCursor;

	/// <summary>Creates a new map at the specified position and size.</summary>
	/// <param name="game">Pointer to the core game.</param>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="x">X position in pixels.</param>
	/// <param name="y">Y position in pixels.</param>
	/// <param name="visibleMapHeight">Current visible map height.</param>
#pragma warning disable CA1062 // a null game is a programming error, C++ dereferences it the same way
	public Map(Game game, int width, int height, int x, int y, int visibleMapHeight) : base(width, height, x, y)
	{
		_game = game;
		_isTFTD = false;
		_arrow = null;
		_anyIndicator = false;
		_isAltPressed = false;
		_isCtrlPressed = false;
		_selectorX = 0;
		_selectorY = 0;
		_mouseX = 0;
		_mouseY = 0;
		_cursorType = CursorType.CT_NORMAL;
		_cursorSize = 1;
		_animFrame = 0;
		_projectile = null;
		_followProjectile = true;
		_projectileInFOV = false;
		_explosionInFOV = false;
		_launch = false;
		_visibleMapHeight = visibleMapHeight;
		_unitDying = false;
		_smoothingEngaged = false;
		_flashScreen = false;
		_bgColor = 15;
		_projectileSet = null;
		_showObstacles = false;
		_showInfoOnCursor = false;

		// TODO: extract to a better place later
		foreach (var pair in Options.mods)
		{
			if (pair.Active)
			{
				if (pair.Id == "xcom2")
				{
					_isTFTD = true;
					break;
				}
			}
		}

		_iconHeight = _game.Mod.GetInterface("battlescape").GetElement("icons").h;
		_iconWidth = _game.Mod.GetInterface("battlescape").GetElement("icons").w;
		_messageColor = _game.Mod.GetInterface("battlescape").GetElement("messageWindows").color;

		var itf = _game.Mod.GetInterface("battlescape").GetElement("thinkingProgressBar");
		_hostileBarColor = itf.color;
		_neutralBarColor = itf.color2;
		_borderBarColor = itf.border;

		PathPreview previewSetting = Options.battleNewPreviewPath;
		_smoothCamera = Options.battleSmoothCamera;
		if (Options.traceAI)
		{
			// turn everything on because we want to see the markers.
			previewSetting = PathPreview.PATH_ARROW_TU;
		}
		_previewSettingArrows = (previewSetting & PathPreview.PATH_ARROWS) != 0;
		_previewSettingTu = (previewSetting & PathPreview.PATH_TU_COST) != 0;
		_previewSettingEnergy = (previewSetting & PathPreview.PATH_ENERGY_COST) != 0;

		_save = _game.SavedGame.SavedBattle;
		if (_game.Mod.LUTs.Count > _save.Depth)
		{
			_transparencies = _game.Mod.LUTs[_save.Depth];
		}
		else
		{
			_transparencies = DummyTransparencies;
		}

		_spriteWidth = _game.Mod.GetSurfaceSet("BLANKS.PCK").GetFrame(0).Width;
		_spriteHeight = _game.Mod.GetSurfaceSet("BLANKS.PCK").GetFrame(0).Height;
		_message = new BattlescapeMessage(320, (visibleMapHeight < 200) ? visibleMapHeight : 200, 0, 0);
		_message.X = _game.Screen.DX;
		_message.Y = (visibleMapHeight - _message.Height) / 2;
		_message.SetTextColor((byte)_messageColor);
		_camera = new Camera(_spriteWidth, _spriteHeight, _save.MapSizeX, _save.MapSizeY, _save.MapSizeZ, this, visibleMapHeight);
		_scrollMouseTimer = new Timer(SCROLL_INTERVAL);
		_scrollMouseTimer.OnTimer((SurfaceHandler)ScrollMouse);
		_scrollKeyTimer = new Timer(SCROLL_INTERVAL);
		_scrollKeyTimer.OnTimer((SurfaceHandler)ScrollKey);
		_camera.SetScrollTimer(_scrollMouseTimer, _scrollKeyTimer);
		_obstacleTimer = new Timer(2500);
		_obstacleTimer.Stop();
		_obstacleTimer.OnTimer((SurfaceHandler)DisableObstacles);

		_showInfoOnCursor = (Options.oxceShowAccuracyOnCrosshair == 1 && Options.battleUFOExtenderAccuracy) || Options.oxceShowAccuracyOnCrosshair == 2;
		_txtAccuracy = new Text(44, 18, 0, 0);
		_txtAccuracy.SetSmall();
		_txtAccuracy.SetPalette(_game.Screen.Palette);
		_txtAccuracy.SetHighContrast(true);
		_txtAccuracy.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
		_cacheActiveWeaponUfopediaArticleUnlocked = -1;
		_cacheIsCtrlPressed = false;
		_cacheCursorPosition = TileEngine.invalid;
		_cacheHasLOS = -1;

		_nightVisionOn = false;
		if (Options.oxceToggleNightVisionType == 2)
		{
			// persisted per campaign
			_nightVisionOn = _game.SavedGame.ToggleNightVision;
		}
		else if (Options.oxceToggleNightVisionType == 1)
		{
			// persisted per battle
			_nightVisionOn = _save.ToggleNightVision;
		}

		_debugVisionMode = 0;
		if (Options.oxceToggleBrightnessType == 2)
		{
			// persisted per campaign
			_debugVisionMode = _game.SavedGame.ToggleBrightness;
		}
		else if (Options.oxceToggleBrightnessType == 1)
		{
			// persisted per battle
			_debugVisionMode = _save.ToggleBrightness;
		}

		_save.SetToggleNightVisionTemp(false);
		_save.SetToggleNightVisionColorTemp(0);
		_save.SetToggleBrightnessTemp(_debugVisionMode);

		_fadeShade = 16;
		_nvColor = 0;
		_fadeTimer = new Timer(FADE_INTERVAL);
		_fadeTimer.OnTimer((SurfaceHandler)FadeShade);
		_fadeTimer.Start();

		var enviro = _save.EnviroEffects;
		if (enviro != null)
		{
			_bgColor = enviro.MapBackgroundColor;
		}

		_stunIndicator = _game.Mod.GetSurface("FloorStunIndicator", false);
		_woundIndicator = _game.Mod.GetSurface("FloorWoundIndicator", false);
		_burnIndicator = _game.Mod.GetSurface("FloorBurnIndicator", false);
		_shockIndicator = _game.Mod.GetSurface("FloorShockIndicator", false);
		_anyIndicator = _stunIndicator != null || _woundIndicator != null || _burnIndicator != null || _shockIndicator != null;

		if (enviro != null)
		{
			if (!string.IsNullOrEmpty(enviro.MapShockIndicator))
			{
				_shockIndicator = _game.Mod.GetSurface(enviro.MapShockIndicator, false);
			}
		}

		for (int i = 0; i < _camera.MapSizeY * _camera.MapSizeX; ++i)
		{
			_vaporParticlesInit.Add([]);
			_vaporParticles.Add([]);
		}
	}
#pragma warning restore CA1062

	/// <summary>Initializes the map.</summary>
	public void Init()
	{
		// load the tiny arrow into a surface
		int f = Acom.Palette.BlockOffset(1); // yellow
		int b = 15; // black
		int[] pixels = [0, 0, b, b, b, b, b, 0, 0,
						0, 0, b, f, f, f, b, 0, 0,
						0, 0, b, f, f, f, b, 0, 0,
						b, b, b, f, f, f, b, b, b,
						b, f, f, f, f, f, f, f, b,
						0, b, f, f, f, f, f, b, 0,
						0, 0, b, f, f, f, b, 0, 0,
						0, 0, 0, b, f, b, 0, 0, 0,
						0, 0, 0, 0, b, 0, 0, 0, 0];

		_arrow = new Surface(9, 9);
		_arrow.SetPalette(Palette);
		_arrow.Lock();
		for (int y = 0; y < 9; ++y)
			for (int x = 0; x < 9; ++x)
				_arrow.SetPixel(x, y, (byte)pixels[x + (y * 9)]);
		_arrow.Unlock();

		_projectile = null;
		if (_save.Depth == 0)
		{
			_projectileSet = _game.Mod.GetSurfaceSet("Projectiles");
		}
		else
		{
			_projectileSet = _game.Mod.GetSurfaceSet("UnderwaterProjectiles");
		}
	}

	/// <summary>Keeps the animation timers running.</summary>
	public override void Think()
	{
		_scrollMouseTimer.Think(null, this);
		_scrollKeyTimer.Think(null, this);
		_fadeTimer.Think(null, this);
		_obstacleTimer.Think(null, this);
	}

	/// <summary>Draws the whole map, part by part.</summary>
	public override void Draw()
	{
		if (!_redraw)
		{
			return;
		}

		// normally we'd call for a Surface::draw();
		// but we don't want to clear the background with colour 0, which is transparent (aka black)
		// we use colour 15 because that actually corresponds to the colour we DO want in all variations of the xcom and tftd palettes.
		// Note: un-hardcoded the color from 15 to ruleset value, default 15
		_redraw = false;
		Pixels.Fill(unchecked((byte)(Acom.Palette.BlockOffset(0) + _bgColor)));

		Tile t;

		_projectileInFOV = _save.DebugMode;
		if (_projectile != null)
		{
			t = _save.GetTile(_projectile.GetPosition(0).ToTile());
			if (_save.Side == FACTION_PLAYER || (t != null && t.Visible != 0))
			{
				_projectileInFOV = true;
			}
		}
		_explosionInFOV = _save.DebugMode;
		if (_explosions.Count != 0)
		{
			foreach (var explosion in _explosions)
			{
				if (explosion.IsBig)
				{
					_explosionInFOV = true;
					break;
				}
				t = _save.GetTile(explosion.Position.ToTile());
				if (t != null && t.Visible != 0)
				{
					_explosionInFOV = true;
					break;
				}
			}
		}

		if ((_save.SelectedUnit != null && _save.SelectedUnit.Visible) || _unitDying || _save.Side == FACTION_PLAYER || _save.DebugMode || _projectileInFOV || _explosionInFOV)
		{
			DrawTerrain(this);
		}
		else
		{
			_message.Blit(this);
		}
	}

	/// <summary>Updates the progress bar of the hidden movement screen.</summary>
	public void RefreshAIProgress(int progress)
	{
		if (_save.Side == FACTION_NEUTRAL)
		{
			_message.SetProgressBarColor((byte)_neutralBarColor, (byte)_borderBarColor);
		}
		else
		{
			_message.SetProgressBarColor((byte)_hostileBarColor, (byte)_borderBarColor);
		}
		_message.SetProgressValue(progress);
	}

	/// <summary>Replaces a certain amount of colors in the surface's palette.</summary>
	/// <param name="colors">Pointer to the set of colors.</param>
	/// <param name="firstcolor">Offset of the first color to replace.</param>
	/// <param name="ncolors">Amount of colors to replace.</param>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		foreach (var mds in _save.MapDataSets)
		{
			mds.Surfaceset.SetPalette(colors, firstcolor, ncolors);
		}
		_message.SetPalette(colors, firstcolor, ncolors);
		RefreshHiddenMovementBackground();
		_message.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
		_message.SetText(_game.Language.GetString("STR_HIDDEN_MOVEMENT"), _game.Language.GetString("STR_THINKING"));
	}

	/// <summary>Sets the background of the hidden movement screen.</summary>
	public void RefreshHiddenMovementBackground()
	{
		_message.SetBackground(_game.Mod.GetSurface(_save.HiddenMovementBackground));
	}

	/// <summary>Get shade of wall.</summary>
	/// <param name="part">For what wall do calculations.</param>
	/// <param name="tileFrot">Tile of wall.</param>
	/// <returns>Current shade of wall.</returns>
	private int GetWallShade(TilePart part, Tile tileFrot)
	{
		int shade;
		if (tileFrot.IsDiscovered(O_FLOOR))
		{
			shade = ReShade(tileFrot);
		}
		else
		{
			shade = 16;
		}
		if (part != O_FLOOR)
		{
			if ((tileFrot.IsDoor(part) || tileFrot.IsUfoDoor(part)) && tileFrot.IsDiscovered(part))
			{
				Position offset =
					part == O_NORTHWALL ? new Position(1, 0, 0) :
					part == O_WESTWALL ? new Position(0, 1, 0) :
						throw new InvalidOperationException("Unsupported tile part for wall shade");

				Tile tileBehind = _save.GetTile(tileFrot.Position - offset);

				shade = Math.Min(ReShade(tileFrot), tileBehind != null ? tileBehind.Shade + 5 : 16);
			}
		}
		return shade;
	}

	/// <summary>Check two positions if have same XY cords.</summary>
	private static bool PositionHaveSameXY(Position a, Position b)
	{
		return a.x == b.x && a.y == b.y;
	}

	/// <summary>Check two positions if have same XY cords (within a range).</summary>
	private static bool PositionInRangeXY(Position a, Position b, int diff)
	{
		return Math.Abs(a.x - b.x) <= diff && Math.Abs(a.y - b.y) <= diff;
	}

	private static readonly int[] ArrowBobOffsets = [0, 1, 2, 1, 0, 1, 2, 1];

	private static readonly int[] ArrowColorsUFO = [6, 3, 14, 4]; // white,    red, blue, green
	private static readonly int[] ArrowColorsTFTD = [4, 11, 16, 6]; // white, orange, blue, green

	private static readonly Position[] BackPos =
	[
		new(0, -1, 0),
		new(-1, -1, 0),
		new(-1, 0, 0),
	];

	private static readonly Position[] FrontPos =
	[
		new(-1, +1, 0),
		new(0, +1, 0),
		new(+1, +1, 0),
		new(+1, 0, 0),
		new(+1, -1, 0),
	];

	private static readonly int[] PixelMaskArray = [0, 2, 1, 3];

	internal static int GetArrowBobForFrame(int frame)
	{
		return ArrowBobOffsets[frame % 8];
	}

	internal static int GetShadePulseForFrame(int shade, int frame)
	{
		if (shade > 7) shade = 7;
		if (shade < 2) shade = 2;
		shade += (ArrowBobOffsets[frame % 8] * 2 - 2);
		return shade;
	}

	/// <summary>fmath.h Interpolate: step 0 (or less) gives a, stepMax (or more) gives b.</summary>
	internal static int Interpolate(int a, int b, int step, int stepMax)
	{
		step = Math.Clamp(step, 0, stepMax);
		return (a * (stepMax - step) + b * (step)) / stepMax;
	}

	/// <summary>
	/// <c>Surface::blitRaw</c> with a raw surface made from a possibly missing frame: an empty source draws nothing
	/// (the port's <see cref="Surface.BlitRaw"/> takes a surface and rejects null).
	/// </summary>
	private static new void BlitRaw(Surface dest, Surface src, int x, int y, int shade, bool half = false, int newBaseColor = 0)
	{
		if (src != null)
		{
			Surface.BlitRaw(dest, src, x, y, shade, half, newBaseColor);
		}
	}

	/// <summary>
	/// Draws one vapor particle: the 2x2 pixel mask {0, 2 / 1, 3} at (x, y), every pixel whose mask value is at least
	/// the particle size goes through the transparency LUT (C++ ShaderDrawFunc with ShaderMove of a SurfaceRaw).
	/// HD: a logical pixel covers Scale x Scale buffer pixels, each one is looked up.
	/// </summary>
	private void DrawVapor(int x, int y, int size, List<byte> lut, int lutOffset)
	{
		int scale = Scale;
		int pitch = PhysicalWidth;
		Span<byte> pixels = Pixels;
		for (int my = 0; my < 2; ++my)
		{
			int ly = y + my;
			if (ly < 0 || ly >= Height)
				continue;
			for (int mx = 0; mx < 2; ++mx)
			{
				int lx = x + mx;
				if (lx < 0 || lx >= Width)
					continue;
				if (size <= PixelMaskArray[mx + my * 2])
				{
					for (int py = 0; py < scale; ++py)
					{
						int row = (ly * scale + py) * pitch + lx * scale;
						for (int px = 0; px < scale; ++px)
						{
							ref byte dest = ref pixels[row + px];
							dest = lut[lutOffset + dest];
						}
					}
				}
			}
		}
	}

	/// <summary>Draw part of unit graphic that overlap current tile.</summary>
	private void DrawUnit(UnitSprite unitSprite, Tile unitTile, Tile currTile, Position currTileScreenPosition, bool topLayer, BattleUnit movingUnit = null)
	{
		const int tileFoorWidth = 32;
		const int tileFoorHeight = 16;
		const int tileHeight = 40;

		if (unitTile == null)
		{
			return;
		}
		BattleUnit bu = unitTile.GetOverlappingUnit(_save, TileUnitOverlapping.TUO_ALWAYS);
		Position unitOffset = default;
		bool unitFromBelow = false;
		bool unitFromAbove = false;
		if (bu != null)
		{
			if (bu != unitTile.Unit)
			{
				unitFromBelow = true;
			}
		}
		else if (movingUnit != null && unitTile == currTile)
		{
			var upperTile = _save.GetAboveTile(unitTile);
			if (upperTile != null && upperTile.HasNoFloor(_save))
			{
				bu = upperTile.Unit;
			}
			if (bu != movingUnit)
			{
				return;
			}
			unitFromAbove = true;
		}
		else
		{
			return;
		}

		if (!(bu.Visible || _save.DebugMode
			|| (Options.oxceEnemyUnitsAlwaysVisible && bu.Faction != FACTION_PLAYER)))
		{
			return;
		}

		unitOffset.x = (short)(unitTile.Position.x - bu.Position.x);
		unitOffset.y = (short)(unitTile.Position.y - bu.Position.y);
		int part = unitOffset.x + unitOffset.y * 2;

		bool moving = bu.Status == STATUS_WALKING || bu.Status == STATUS_FLYING;
		int bonusWidth = moving ? 0 : tileFoorWidth;
		int topMargin = 0;
		int bottomMargin = 0;

		//if unit is from below then we draw only part that in in tile
		if (unitFromBelow)
		{
			bottomMargin = -tileFoorHeight / 2;
			topMargin = tileFoorHeight;
		}
		else if (topLayer)
		{
			topMargin = 2 * tileFoorHeight;
		}
		else
		{
			Tile top = _save.GetAboveTile(unitTile);
			if (top != null && top.GetOverlappingUnit(_save, TileUnitOverlapping.TUO_ALWAYS) == bu)
			{
				topMargin = -tileFoorHeight / 2;
			}
			else
			{
				topMargin = tileFoorHeight;
			}
		}

		GraphSubset mask = new GraphSubset(tileFoorWidth + bonusWidth, tileHeight + topMargin + bottomMargin).Offset(currTileScreenPosition.x - bonusWidth / 2, currTileScreenPosition.y - topMargin);

		if (moving)
		{
			GraphSubset leftMask = mask.Offset(-tileFoorWidth / 2, 0);
			GraphSubset rightMask = mask.Offset(+tileFoorWidth / 2, 0);
			int direction = bu.Direction;
			Position partCurr = currTile.Position;
			Position partDest = bu.Destination + unitOffset;
			Position partLast = bu.LastPosition + unitOffset;
			bool isTileDestPos = PositionHaveSameXY(partDest, partCurr);
			bool isTileLastPos = PositionHaveSameXY(partLast, partCurr);

			if (unitFromAbove && partLast != unitTile.Position)
			{
				//this tile is below moving unit and it do not change levels, nothing to draw
				return;
			}

			//adjusting mask
			if (PositionHaveSameXY(partLast, partDest))
			{
				if (currTile == unitTile)
				{
					//no change
				}
				else
				{
					//nothing to draw
					return;
				}
			}
			else if (isTileDestPos)
			{
				//unit is moving to this tile
				switch (direction)
				{
				case 0:
				case 1:
					mask = GraphSubset.Intersection(mask, rightMask);
					break;
				case 2:
					//no change
					break;
				case 3:
					//no change
					break;
				case 4:
					//no change
					break;
				case 5:
				case 6:
					mask = GraphSubset.Intersection(mask, leftMask);
					break;
				case 7:
					//nothing to draw
					return;
				}
			}
			else if (isTileLastPos)
			{
				//unit is exiting this tile
				switch (direction)
				{
				case 0:
					//no change
					break;
				case 1:
				case 2:
					mask = GraphSubset.Intersection(mask, leftMask);
					break;
				case 3:
					//nothing to draw
					return;
				case 4:
				case 5:
					mask = GraphSubset.Intersection(mask, rightMask);
					break;
				case 6:
					//no change
					break;
				case 7:
					//no change
					break;
				}
			}
			else
			{
				Position leftPos = partCurr + new Position(-1, 0, 0);
				Position rightPos = partCurr + new Position(0, -1, 0);
				if (!topLayer && (partDest.z > partCurr.z || partLast.z > partCurr.z))
				{
					//unit change layers, it will be drawn by upper layer not lower.
					return;
				}
				else if (
					(direction == 1 && (partDest == rightPos || partLast == leftPos)) ||
					(direction == 5 && (partDest == leftPos || partLast == rightPos)))
				{
					mask = new GraphSubset(tileFoorWidth, tileHeight + 2 * tileFoorHeight).Offset(currTileScreenPosition.x, currTileScreenPosition.y - 2 * tileFoorHeight);
				}
				else
				{
					//unit is not moving close to tile
					return;
				}
			}
		}
		else if (unitTile != currTile || unitFromAbove)
		{
			return;
		}

		Position tileScreenPosition = default;
		_camera.ConvertMapToScreen(unitTile.Position + new Position(0, 0, (unitFromBelow ? -1 : 0) + (unitFromAbove ? 1 : 0)), ref tileScreenPosition);
		tileScreenPosition += _camera.MapOffset;

		//get shade helpers
		int GetTileShade(Tile tile)
		{
			return tile != null ? (tile.IsDiscovered(O_FLOOR) ? ReShade(tile) : 16) : 16;
		}
		int GetMixedTileShade(Tile tile, int heightOffset, bool below)
		{
			int shadeLower = 0;
			int shadeUpper = 0;
			if (below)
			{
				shadeLower = GetTileShade(_save.GetBelowTile(tile));
				shadeUpper = GetTileShade(tile);
			}
			else
			{
				shadeLower = GetTileShade(tile);
				shadeUpper = GetTileShade(_save.GetAboveTile(tile));
			}

			return Interpolate(shadeLower, shadeUpper, -heightOffset, Position.TileZ);
		}

		// draw unit
		int shade = 0;
		UnitWalkingOffset offsets = CalculateWalkingOffset(bu);
		if (moving)
		{
			Position start = bu.Position;
			Position end = bu.Destination;
			short minLevel = Math.Min(start.z, end.z); // Sint16
			int startShade = GetMixedTileShade(_save.GetTile(start), start.z == minLevel ? offsets.TerrainLevelOffset : 0, false);
			int endShade = GetMixedTileShade(_save.GetTile(end), end.z == minLevel ? offsets.TerrainLevelOffset : 0, false);
			shade = Interpolate(startShade, endShade, offsets.NormalizedMovePhase, 16);
		}
		else
		{
			shade = GetMixedTileShade(currTile, offsets.TerrainLevelOffset, unitFromBelow);
			if (_showObstacles && unitTile.GetObstacle(4))
			{
				shade = GetShadePulseForFrame(shade, _animFrame);
			}
		}
		if (_debugVisionMode == 1)
		{
			shade = Math.Min(+NIGHT_VISION_SHADE, shade);
		}
		unitSprite.Draw(bu, part, tileScreenPosition.x + offsets.ScreenOffset.x, tileScreenPosition.y + offsets.ScreenOffset.y, shade, mask, _isAltPressed && !_isCtrlPressed);
	}

	/// <summary>True if the unit is visible to the player (or shown anyway).</summary>
	private bool IsUnitShown(BattleUnit unit)
	{
		return unit != null && (unit.Visible || _save.DebugMode || (Options.oxceEnemyUnitsAlwaysVisible && unit.Faction != FACTION_PLAYER));
	}

	/// <summary>
	/// Draw the terrain.
	/// Keep this function as optimised as possible. It's big to minimise overhead of function calls.
	/// </summary>
	/// <param name="surface">The surface to draw on.</param>
	private void DrawTerrain(Surface surface)
	{
		_isAltPressed = _game.IsAltPressed(true);
		_isCtrlPressed = _game.IsCtrlPressed(true);
		int frameNumber = 0;
		Surface tmpSurface;
		Tile tile;
		int beginX = 0, endX = _save.MapSizeX - 1;
		int beginY = 0, endY = _save.MapSizeY - 1;
		int beginZ = 0, endZ = _save.MapSizeZ - 1;
		Position mapPosition, screenPosition = default, bulletPositionScreen = default, movingUnitPosition = default;
		int bulletLowX = 16000, bulletLowY = 16000, bulletLowZ = 16000, bulletHighX = 0, bulletHighY = 0, bulletHighZ = 0;
		int dummy = 0;
		BattleUnit movingUnit = _save.TileEngine.MovingUnit;
		int tileShade, tileColor, obstacleShade;
		UnitSprite unitSprite = new(surface, _game.Mod, _save, _animFrame, _save.Depth != 0,
			_isTFTD ? ArrowColorsTFTD[1] : ArrowColorsUFO[1], _isTFTD ? ArrowColorsTFTD[2] : ArrowColorsUFO[2]);
		ItemSprite itemSprite = new(surface, _game.Mod, _save, _animFrame);

		int halfAnimFrame = (_animFrame / 2) % 4;
		int halfAnimFrameRest = (_animFrame % 2);

		NumberText _numWaypid = null;

		// if we got bullet, get the highest x and y tiles to draw it on
		if (_projectile != null && _explosions.Count == 0)
		{
			int part = _projectile.Item != null ? 0 : BULLET_SPRITES - 1;
			for (int i = 0; i <= part; ++i)
			{
				if (_projectile.GetPosition(1 - i).x < bulletLowX)
					bulletLowX = _projectile.GetPosition(1 - i).x;
				if (_projectile.GetPosition(1 - i).y < bulletLowY)
					bulletLowY = _projectile.GetPosition(1 - i).y;
				if (_projectile.GetPosition(1 - i).z < bulletLowZ)
					bulletLowZ = _projectile.GetPosition(1 - i).z;
				if (_projectile.GetPosition(1 - i).x > bulletHighX)
					bulletHighX = _projectile.GetPosition(1 - i).x;
				if (_projectile.GetPosition(1 - i).y > bulletHighY)
					bulletHighY = _projectile.GetPosition(1 - i).y;
				if (_projectile.GetPosition(1 - i).z > bulletHighZ)
					bulletHighZ = _projectile.GetPosition(1 - i).z;
			}
			// divide by 16 to go from voxel to tile position
			bulletLowX = bulletLowX / 16;
			bulletLowY = bulletLowY / 16;
			bulletLowZ = bulletLowZ / 24;
			bulletHighX = bulletHighX / 16;
			bulletHighY = bulletHighY / 16;
			bulletHighZ = bulletHighZ / 24;

			// if the projectile is outside the viewport - center it back on it
			_camera.ConvertVoxelToScreen(_projectile.GetPosition(), ref bulletPositionScreen);

			if (_projectileInFOV && _followProjectile)
			{
				Position newCam = _camera.MapOffset;
				if (newCam.z != bulletHighZ) //switch level
				{
					newCam.z = (short)bulletHighZ;
					if (_projectileInFOV)
					{
						_camera.MapOffset = newCam;
						_camera.ConvertVoxelToScreen(_projectile.GetPosition(), ref bulletPositionScreen);
					}
				}
				if (_smoothCamera)
				{
					if (_launch)
					{
						_launch = false;
						if ((bulletPositionScreen.x < 1 || bulletPositionScreen.x > surface.Width - 1 ||
							bulletPositionScreen.y < 1 || bulletPositionScreen.y > _visibleMapHeight - 1))
						{
							_camera.CenterOnPosition(new Position(bulletLowX, bulletLowY, bulletHighZ), false);
							_camera.ConvertVoxelToScreen(_projectile.GetPosition(), ref bulletPositionScreen);
						}
					}
					if (!_smoothingEngaged)
					{
						if (bulletPositionScreen.x < 1 || bulletPositionScreen.x > surface.Width - 1 ||
							bulletPositionScreen.y < 1 || bulletPositionScreen.y > _visibleMapHeight - 1)
						{
							_smoothingEngaged = true;
						}
					}
					else
					{
						_camera.JumpXY(surface.Width / 2 - bulletPositionScreen.x, _visibleMapHeight / 2 - bulletPositionScreen.y);
					}
				}
				else
				{
					bool enough;
					do
					{
						enough = true;
						if (bulletPositionScreen.x < 0)
						{
							_camera.JumpXY(+surface.Width, 0);
							enough = false;
						}
						else if (bulletPositionScreen.x > surface.Width)
						{
							_camera.JumpXY(-surface.Width, 0);
							enough = false;
						}
						else if (bulletPositionScreen.y < 0)
						{
							_camera.JumpXY(0, +_visibleMapHeight);
							enough = false;
						}
						else if (bulletPositionScreen.y > _visibleMapHeight)
						{
							_camera.JumpXY(0, -_visibleMapHeight);
							enough = false;
						}
						_camera.ConvertVoxelToScreen(_projectile.GetPosition(), ref bulletPositionScreen);
					}
					while (!enough);
				}
			}
		}

		// get corner map coordinates to give rough boundaries in which tiles to redraw are
		_camera.ConvertScreenToMap(0, 0, ref beginX, ref dummy);
		_camera.ConvertScreenToMap(surface.Width, 0, ref dummy, ref beginY);
		_camera.ConvertScreenToMap(surface.Width + _spriteWidth, surface.Height + _spriteHeight, ref endX, ref dummy);
		_camera.ConvertScreenToMap(0, surface.Height + _spriteHeight, ref dummy, ref endY);
		beginY -= (_camera.ViewLevel * 2);
		beginX -= (_camera.ViewLevel * 2);
		if (beginX < 0)
			beginX = 0;
		if (beginY < 0)
			beginY = 0;

		if (!_camera.ShowAllLayers)
		{
			endZ = Math.Min(endZ, _camera.ViewLevel);
		}
		if (_camera.ShowSingleLayer)
		{
			beginZ = _camera.ViewLevel;
			endZ = _camera.ViewLevel;
		}


		bool pathfinderTurnedOn = _save.Pathfinding.IsPathPreviewed;

		if (_waypoints.Count != 0 || (pathfinderTurnedOn && (_previewSettingTu || _previewSettingEnergy)))
		{
			_numWaypid = new NumberText(15, 15, 20, 30);
			_numWaypid.SetPalette(Palette);
			_numWaypid.SetColor(pathfinderTurnedOn ? (byte)(_messageColor + 1) : Acom.Palette.BlockOffset(1));
		}

		if (movingUnit != null)
		{
			movingUnitPosition = movingUnit.Position;
		}

		surface.Lock();
		Position cameraPos = _camera.MapOffset;
		for (int itZ = beginZ; itZ <= endZ; itZ++)
		{
			bool topLayer = itZ == endZ;
			for (int itY = beginY; itY < endY; itY++)
			{
				mapPosition = new Position(beginX, itY, itZ);
				// C++ walks the tile array with a pointer (tile++); the tiles of a row are consecutive
				int tileIndex = _save.GetTileIndex(mapPosition);
				for (int itX = beginX; itX < endX; itX++, mapPosition.x++, tileIndex++)
				{
					tile = _save.GetTile(tileIndex);
					_camera.ConvertMapToScreen(mapPosition, ref screenPosition);
					screenPosition += cameraPos;

					// only render cells that are inside the surface
					if (screenPosition.x > -_spriteWidth && screenPosition.x < surface.Width + _spriteWidth &&
						screenPosition.y > -_spriteHeight && screenPosition.y < surface.Height + _spriteHeight)
					{
						bool isUnitMovingNearby = movingUnit != null && PositionInRangeXY(movingUnitPosition, mapPosition, 2);

						if (tile.IsDiscovered(O_FLOOR))
						{
							tileShade = ReShade(tile);
							obstacleShade = tileShade;
							if (_showObstacles)
							{
								if (tile.IsObstacle)
								{
									obstacleShade = GetShadePulseForFrame(tileShade, _animFrame);
								}
							}
						}
						else
						{
							tileShade = 16;
							obstacleShade = 16;
						}

						tileColor = tile.MarkerColor;

						// Draw floor
						tmpSurface = tile.GetSprite(O_FLOOR);
						if (tmpSurface != null)
						{
							if (tile.GetObstacle((int)O_FLOOR))
								BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_FLOOR), obstacleShade, false, _nvColor);
							else
								BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_FLOOR), tileShade, false, _nvColor);
						}

						var unit = tile.Unit;

						// Draw cursor back
						if (_cursorType != CursorType.CT_NONE && _selectorX > itX - _cursorSize && _selectorY > itY - _cursorSize && _selectorX < itX + 1 && _selectorY < itY + 1 && !_save.BattleState.MouseOverIcons)
						{
							if (_camera.ViewLevel == itZ)
							{
								if (_cursorType != CursorType.CT_AIM)
								{
									if (IsUnitShown(unit))
										frameNumber = halfAnimFrameRest; // yellow box
									else
										frameNumber = 0; // red box
								}
								else
								{
									if (IsUnitShown(unit))
										frameNumber = 7 + halfAnimFrame; // yellow animated crosshairs
									else
										frameNumber = 6; // red static crosshairs
								}
								tmpSurface = _game.Mod.GetSurfaceSet("CURSOR.PCK").GetFrame(frameNumber);
								BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y, 0);
							}
							else if (_camera.ViewLevel > itZ)
							{
								frameNumber = 2; // blue box
								tmpSurface = _game.Mod.GetSurfaceSet("CURSOR.PCK").GetFrame(frameNumber);
								BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y, 0);
							}
						}

						if (isUnitMovingNearby)
						{
							// special handling for a moving unit in background of tile.
							for (int b = 0; b < BackPos.Length; ++b)
							{
								DrawUnit(unitSprite, _save.GetTile(mapPosition + BackPos[b]), tile, screenPosition, topLayer);
							}
						}

						// Draw walls
						{
							// Draw west wall
							tmpSurface = tile.GetSprite(O_WESTWALL);
							if (tmpSurface != null)
							{
								int wallShade = GetWallShade(O_WESTWALL, tile);
								if (tile.GetObstacle((int)O_WESTWALL))
									BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_WESTWALL), obstacleShade, false, _nvColor);
								else
									BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_WESTWALL), wallShade, false, _nvColor);
							}
							// Draw north wall
							tmpSurface = tile.GetSprite(O_NORTHWALL);
							if (tmpSurface != null)
							{
								int wallShade = GetWallShade(O_NORTHWALL, tile);
								if (tile.GetObstacle((int)O_NORTHWALL))
									BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_NORTHWALL), obstacleShade, tile.GetSprite(O_WESTWALL) != null, _nvColor);
								else
									BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_NORTHWALL), wallShade, tile.GetSprite(O_WESTWALL) != null, _nvColor);
							}
							// Draw object
							tmpSurface = tile.GetSprite(O_OBJECT);
							if (tmpSurface != null)
							{
								if (tile.IsBackTileObject(O_OBJECT))
								{
									if (tile.GetObstacle((int)O_OBJECT))
										BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_OBJECT), obstacleShade, false, _nvColor);
									else
										BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_OBJECT), tileShade, false, _nvColor);
								}
							}
							// draw an item on top of the floor (if any)
							BattleItem item = tile.TopItem;
							if (item != null)
							{
								itemSprite.Draw(item,
									screenPosition.x,
									screenPosition.y + tile.TerrainLevel,
									tileShade
								);
								if (_anyIndicator)
								{
									BattleUnit itemUnit = item.Unit;
									if (itemUnit != null && itemUnit.Status == STATUS_UNCONSCIOUS && itemUnit.IndicatorsAreEnabled())
									{
										if (_burnIndicator != null && itemUnit.Fire > 0)
										{
											_burnIndicator.BlitNShade(surface,
												screenPosition.x,
												screenPosition.y + tile.TerrainLevel,
												tileShade);
										}
										else if (_woundIndicator != null && itemUnit.FatalWounds > 0)
										{
											_woundIndicator.BlitNShade(surface,
												screenPosition.x,
												screenPosition.y + tile.TerrainLevel,
												tileShade);
										}
										else if (_shockIndicator != null && itemUnit.HasNegativeHealthRegen)
										{
											_shockIndicator.BlitNShade(surface,
												screenPosition.x,
												screenPosition.y + tile.TerrainLevel,
												tileShade);
										}
										else if (_stunIndicator != null)
										{
											_stunIndicator.BlitNShade(surface,
												screenPosition.x,
												screenPosition.y + tile.TerrainLevel,
												tileShade);
										}
									}
								}
							}
						}

						// check if we got bullet && it is in Field Of View
						if (_projectile != null && _projectileInFOV)
						{
							tmpSurface = null;
							BattleItem item = _projectile.Item;
							if (item != null)
							{
								Position voxelPos = _projectile.GetPosition();
								// draw shadow on the floor
								voxelPos.z = (short)_save.TileEngine.CastedShade(voxelPos);
								if (voxelPos.x / 16 >= itX &&
									voxelPos.y / 16 >= itY &&
									voxelPos.x / 16 <= itX + 1 &&
									voxelPos.y / 16 <= itY + 1 &&
									voxelPos.z / 24 == itZ &&
									_save.TileEngine.IsVoxelVisible(voxelPos))
								{
									_camera.ConvertVoxelToScreen(voxelPos, ref bulletPositionScreen);

									itemSprite.DrawShadow(item,
										bulletPositionScreen.x - 16,
										bulletPositionScreen.y - 26
									);
								}

								voxelPos = _projectile.GetPosition();
								// draw thrown object
								if (voxelPos.x / 16 >= itX &&
									voxelPos.y / 16 >= itY &&
									voxelPos.x / 16 <= itX + 1 &&
									voxelPos.y / 16 <= itY + 1 &&
									voxelPos.z / 24 == itZ &&
									_save.TileEngine.IsVoxelVisible(voxelPos))
								{
									_camera.ConvertVoxelToScreen(voxelPos, ref bulletPositionScreen);

									itemSprite.Draw(item,
										bulletPositionScreen.x - 16,
										bulletPositionScreen.y - 26,
										tileShade
									);
								}
							}
							else
							{
								// draw bullet on the correct tile
								if (itX >= bulletLowX && itX <= bulletHighX && itY >= bulletLowY && itY <= bulletHighY)
								{
									int begin = 0;
									int end = BULLET_SPRITES;
									int direction = 1;
									if (_projectile.IsReversed)
									{
										begin = BULLET_SPRITES - 1;
										end = -1;
										direction = -1;
									}

									for (int i = begin; i != end; i += direction)
									{
										tmpSurface = _projectileSet.GetFrame(_projectile.GetParticle(i));
										if (tmpSurface != null)
										{
											Position voxelPos = _projectile.GetPosition(1 - i);
											// draw shadow on the floor
											voxelPos.z = (short)_save.TileEngine.CastedShade(voxelPos);
											if (voxelPos.x / 16 == itX &&
												voxelPos.y / 16 == itY &&
												voxelPos.z / 24 == itZ &&
												_save.TileEngine.IsVoxelVisible(voxelPos))
											{
												_camera.ConvertVoxelToScreen(voxelPos, ref bulletPositionScreen);
												bulletPositionScreen.x = (short)(bulletPositionScreen.x - tmpSurface.Width / 2);
												bulletPositionScreen.y = (short)(bulletPositionScreen.y - tmpSurface.Height / 2);
												BlitRaw(surface, tmpSurface, bulletPositionScreen.x, bulletPositionScreen.y, 16, false, _nvColor);
											}

											// draw bullet itself
											voxelPos = _projectile.GetPosition(1 - i);
											if (voxelPos.x / 16 == itX &&
												voxelPos.y / 16 == itY &&
												voxelPos.z / 24 == itZ &&
												_save.TileEngine.IsVoxelVisible(voxelPos))
											{
												_camera.ConvertVoxelToScreen(voxelPos, ref bulletPositionScreen);
												bulletPositionScreen.x = (short)(bulletPositionScreen.x - tmpSurface.Width / 2);
												bulletPositionScreen.y = (short)(bulletPositionScreen.y - tmpSurface.Height / 2);
												BlitRaw(surface, tmpSurface, bulletPositionScreen.x, bulletPositionScreen.y, 0, false, _nvColor);
											}
										}
									}
								}
							}
						}

						//draw particle clouds
						int vaporScreenOriginX = screenPosition.x + _spriteWidth / 2;
						int vaporScreenOriginY = screenPosition.y + _spriteHeight - _spriteWidth / 2 + tile.Position.ToVoxel().z;

						//draw particle clouds behind solder
						foreach (Particle p in GetVaporParticle(tile, 0))
						{
							int vaporX = vaporScreenOriginX + p.OffsetX;
							int vaporY = vaporScreenOriginY + p.OffsetY;
							int transparetOffsets =
								(p.Color * Mod.TransparenciesOpacityLevels * Mod.TransparenciesPaletteColors)
								+ (p.Opacity * Mod.TransparenciesPaletteColors);

							DrawVapor(vaporX, vaporY, p.Size, _transparencies, transparetOffsets);
						}

						unit = tile.Unit;
						// Draw soldier from this tile, below or above
						DrawUnit(unitSprite, tile, tile, screenPosition, topLayer, isUnitMovingNearby ? movingUnit : null);

						if (isUnitMovingNearby)
						{
							// special handling for a moving unit in foreground of tile.
							for (int f = 0; f < FrontPos.Length; ++f)
							{
								DrawUnit(unitSprite, _save.GetTile(mapPosition + FrontPos[f]), tile, screenPosition, topLayer);
							}
						}

						// Draw smoke/fire
						if (tile.Smoke != 0 && tile.IsDiscovered(O_FLOOR))
						{
							frameNumber = 0;
							int shade = 0;
							if (tile.Fire == 0)
							{
								if (_save.Depth > 0)
								{
									frameNumber += Mod.UNDERWATER_SMOKE_OFFSET;
								}
								else
								{
									frameNumber += Mod.SMOKE_OFFSET;
								}
								if (Mod.EXTENDED_SMOKE_OFFSET == 0)
								{
									frameNumber += (int)Math.Floor((tile.Smoke / 6.0) - 0.1); // see http://www.ufopaedia.org/images/c/cb/Smoke.gif
								}
								else if (Mod.EXTENDED_SMOKE_OFFSET == 1)
								{
									frameNumber += (int)Math.Floor((tile.Smoke / 6.0) - 0.1) * 4;
								}
								else // if (Mod::EXTENDED_SMOKE_OFFSET == 2)
								{
									frameNumber += (tile.Smoke - 1) / 5 * 4;
								}
								shade = tileShade;
							}

							if (halfAnimFrame + tile.AnimationOffset > 3)
							{
								frameNumber += halfAnimFrame + tile.AnimationOffset - 4;
							}
							else
							{
								frameNumber += halfAnimFrame + tile.AnimationOffset;
							}
							tmpSurface = _game.Mod.GetSurfaceSet("SMOKE.PCK").GetFrame(frameNumber);
							BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y, shade, false, _nvColor);
						}

						//draw particle clouds on front of solder
						foreach (Particle p in GetVaporParticle(tile, topLayer ? 3 : 1))
						{
							int vaporX = vaporScreenOriginX + p.OffsetX;
							int vaporY = vaporScreenOriginY + p.OffsetY;
							int transparetOffsets =
								(p.Color * Mod.TransparenciesOpacityLevels * Mod.TransparenciesPaletteColors)
								+ (p.Opacity * Mod.TransparenciesPaletteColors);

							DrawVapor(vaporX, vaporY, p.Size, _transparencies, transparetOffsets);
						}

						// Draw Path Preview
						if (_previewSettingArrows && tile.Preview != -1 && tile.IsDiscovered(O_FLOOR))
						{
							if (itZ > 0 && tile.HasNoFloor(_save))
							{
								tmpSurface = _game.Mod.GetSurfaceSet("Pathfinding").GetFrame(11);
								if (tmpSurface != null)
								{
									BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y + 2, 0, false, tile.MarkerColor);
								}
							}
							tmpSurface = _game.Mod.GetSurfaceSet("Pathfinding").GetFrame(tile.Preview);
							if (tmpSurface != null)
							{
								BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y + tile.TerrainLevel, 0, false, tileColor);
							}
						}

						{
							// Draw object
							tmpSurface = tile.GetSprite(O_OBJECT);
							if (tmpSurface != null)
							{
								if (!tile.IsBackTileObject(O_OBJECT))
								{
									if (tile.GetObstacle((int)O_OBJECT))
										BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_OBJECT), obstacleShade, false, _nvColor);
									else
										BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - tile.GetYOffset(O_OBJECT), tileShade, false, _nvColor);
								}
							}
						}
						// Draw cursor front
						if (_cursorType != CursorType.CT_NONE && _selectorX > itX - _cursorSize && _selectorY > itY - _cursorSize && _selectorX < itX + 1 && _selectorY < itY + 1 && !_save.BattleState.MouseOverIcons)
						{
							if (_camera.ViewLevel == itZ)
							{
								if (_cursorType != CursorType.CT_AIM)
								{
									if (IsUnitShown(unit))
										frameNumber = 3 + halfAnimFrameRest; // yellow box
									else
										frameNumber = 3; // red box
								}
								else
								{
									if (IsUnitShown(unit))
										frameNumber = 7 + halfAnimFrame; // yellow animated crosshairs
									else
										frameNumber = 6; // red static crosshairs
								}
								tmpSurface = _game.Mod.GetSurfaceSet("CURSOR.PCK").GetFrame(frameNumber);
								BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y, 0);

								// UFO extender accuracy: display adjusted accuracy value on crosshair in real-time.
								if (_cursorType >= CursorType.CT_AIM && _showInfoOnCursor && (_cursorType != CursorType.CT_THROW || !Options.oxceDisableInfoOnThrowCursor))
								{
									DrawCursorInfo(surface, tile, unit, itX, itY, itZ, screenPosition);
								}
							}
							else if (_camera.ViewLevel > itZ)
							{
								frameNumber = 5; // blue box
								tmpSurface = _game.Mod.GetSurfaceSet("CURSOR.PCK").GetFrame(frameNumber);
								BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y, 0);
							}
							if (!_isAltPressed && _cursorType > CursorType.CT_AIM && _camera.ViewLevel == itZ)
							{
								bool ignore = false;
								if (_cursorType == CursorType.CT_PSI || _cursorType == CursorType.CT_WAYPOINT)
								{
									BattleAction action = _save.BattleGame.CurrentAction;
									int distanceSq = action.actor.Distance3dToPositionSq(new Position(itX, itY, itZ));
									if (action.weapon.Rules.IsOutOfRange(distanceSq))
									{
										// weapon doesn't work at this distance, just draw a normal cursor with a red 0% hint text
										ignore = true;
										_txtAccuracy.SetColor((byte)(Acom.Palette.BlockOffset((byte)(Pathfinding.red - 1)) - 1));
										_txtAccuracy.SetText("0%");
										_txtAccuracy.Draw();
										_txtAccuracy.BlitNShade(surface, screenPosition.x, screenPosition.y, 0);
									}
								}
								if (!ignore)
								{
									int[] frame = [0, 0, 0, 11, 13, 15];
									tmpSurface = _game.Mod.GetSurfaceSet("CURSOR.PCK").GetFrame(frame[(int)_cursorType] + (_animFrame / 4) % 2);
									BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y, 0);
								}
							}
						}

						// Draw waypoints if any on this tile
						int waypid = 1;
						int waypXOff = 2;
						int waypYOff = 2;

						foreach (var waypoint in _waypoints)
						{
							if (waypoint == mapPosition)
							{
								if (waypXOff == 2 && waypYOff == 2)
								{
									tmpSurface = _game.Mod.GetSurfaceSet("CURSOR.PCK").GetFrame(7);
									BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y, 0);
								}
								if (_save.BattleGame.CurrentAction.type == BA_LAUNCH || _save.BattleGame.CurrentAction.sprayTargeting)
								{
									_numWaypid.Value = (uint)waypid;
									_numWaypid.Draw();
									_numWaypid.BlitNShade(surface, screenPosition.x + waypXOff, screenPosition.y + waypYOff, 0);

									waypXOff += waypid > 9 ? 8 : 6;
									if (waypXOff >= 26)
									{
										waypXOff = 2;
										waypYOff += 8;
									}
								}
							}
							waypid++;
						}
					}
				}
			}
		}
		if (pathfinderTurnedOn)
		{
			_numWaypid?.SetBordered(true); // give it a border for the pathfinding display, makes it more visible on snow, etc.
			for (int itZ = beginZ; itZ <= endZ; itZ++)
			{
				for (int itX = beginX; itX <= endX; itX++)
				{
					for (int itY = beginY; itY <= endY; itY++)
					{
						mapPosition = new Position(itX, itY, itZ);
						_camera.ConvertMapToScreen(mapPosition, ref screenPosition);
						screenPosition += _camera.MapOffset;

						// only render cells that are inside the surface
						if (screenPosition.x > -_spriteWidth && screenPosition.x < surface.Width + _spriteWidth &&
							screenPosition.y > -_spriteHeight && screenPosition.y < surface.Height + _spriteHeight)
						{
							tile = _save.GetTile(mapPosition);
							if (tile == null || !tile.IsDiscovered(O_FLOOR) || tile.Preview == -1)
								continue;
							int adjustment = -tile.TerrainLevel;
							if (_previewSettingArrows)
							{
								if (itZ > 0 && tile.HasNoFloor(_save))
								{
									tmpSurface = _game.Mod.GetSurfaceSet("Pathfinding").GetFrame(23);
									if (tmpSurface != null)
									{
										BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y + 2, 0, false, tile.MarkerColor);
									}
								}
								int overlay = tile.Preview + 12;
								tmpSurface = _game.Mod.GetSurfaceSet("Pathfinding").GetFrame(overlay);
								if (tmpSurface != null)
								{
									BlitRaw(surface, tmpSurface, screenPosition.x, screenPosition.y - adjustment, 0, false, tile.MarkerColor);
								}
							}

							if ((_previewSettingTu || _previewSettingEnergy) && (tile.TUMarker > -1 || tile.EnergyMarker > -1))
							{
								int off = tile.TUMarker > 9 ? 5 : 3;
								int offE = tile.EnergyMarker > 9 ? 5 : 3;
								int mcolor = _previewSettingArrows ? 0 : tile.MarkerColor;
								if (_previewSettingArrows)
								{
									adjustment += 7;
								}
								if (_save.SelectedUnit != null && _save.SelectedUnit.IsBigUnit)
								{
									adjustment += 1;
									if (!_previewSettingArrows)
									{
										adjustment += 7;
									}
								}
								if (_previewSettingTu)
								{
									_numWaypid.Value = unchecked((uint)tile.TUMarker);
									_numWaypid.Draw();
									if (_previewSettingEnergy)
									{
										// TU
										_numWaypid.BlitNShade(surface, screenPosition.x + 16 - off, screenPosition.y + (22 - adjustment), 0, false, mcolor);
										// and Energy
										_numWaypid.Value = unchecked((uint)tile.EnergyMarker);
										_numWaypid.Draw();
										_numWaypid.BlitNShade(surface, screenPosition.x + 16 - offE, screenPosition.y + (29 - adjustment), 0, false, mcolor);
									}
									else
									{
										// only TU
										_numWaypid.BlitNShade(surface, screenPosition.x + 16 - off, screenPosition.y + (29 - adjustment), 0, false, mcolor);
									}
								}
								else if (_previewSettingEnergy)
								{
									// only Energy
									_numWaypid.Value = unchecked((uint)tile.EnergyMarker);
									_numWaypid.Draw();
									_numWaypid.BlitNShade(surface, screenPosition.x + 16 - offE, screenPosition.y + (29 - adjustment), 0, false, mcolor);
								}
							}
						}
					}
				}
			}
			_numWaypid?.SetBordered(false); // make sure we remove the border in case it's being used for missile waypoints.
		}

		var selectedUnit = _save.SelectedUnit;
		if (selectedUnit != null && (_save.Side == FACTION_PLAYER || _save.DebugMode) && selectedUnit.Position.z <= _camera.ViewLevel)
		{
			_camera.ConvertMapToScreen(selectedUnit.Position, ref screenPosition);
			screenPosition += _camera.MapOffset;
			Position offset = CalculateWalkingOffset(selectedUnit).ScreenOffset;
			if (selectedUnit.IsBigUnit)
			{
				offset.y += 4;
			}
			offset.y = (short)(offset.y + Position.TileZ - (selectedUnit.Height + selectedUnit.FloatHeight));
			if (selectedUnit.IsKneeled)
			{
				offset.y -= 2;
			}
			if (CursorType != CursorType.CT_NONE)
			{
				_arrow.BlitNShade(surface, screenPosition.x + offset.x + (_spriteWidth / 2) - (_arrow.Width / 2), screenPosition.y + offset.y - _arrow.Height + GetArrowBobForFrame(_animFrame), 0);
			}
		}

		// Draw motion scanner arrows
		if (_isAltPressed && _save.Side == FACTION_PLAYER && CursorType != CursorType.CT_NONE)
		{
			foreach (var myUnit in _save.Units)
			{
				bool motionScan = myUnit.ScannedTurn == _save.Turn && myUnit.Faction != FACTION_PLAYER && !myUnit.IsOut;
				bool customMarker = myUnit.CustomMarker > 0 && myUnit.Faction == FACTION_PLAYER && !myUnit.IsOut;
				if (motionScan || customMarker)
				{
					Position temp = myUnit.Position;
					temp.z = (short)_camera.ViewLevel;
					_camera.ConvertMapToScreen(temp, ref screenPosition);
					screenPosition += _camera.MapOffset;
					Position offset = default;
					//calculateWalkingOffset(myUnit, &offset);
					if (myUnit.IsBigUnit)
					{
						offset.y += 4;
					}
					if (motionScan)
					{
						offset.y += Position.TileZ - /*myUnit->getHeight()*/ 21; // no spoilers
					}
					else if (customMarker)
					{
						offset.y = (short)(offset.y + Position.TileZ - (myUnit.Height + myUnit.FloatHeight));
					}
					if (myUnit.IsKneeled)
					{
						offset.y -= 2;
					}
					if (motionScan)
					{
						_arrow.BlitNShade(
							surface,
							screenPosition.x + offset.x + (_spriteWidth / 2) - (_arrow.Width / 2),
							screenPosition.y + offset.y - _arrow.Height + GetArrowBobForFrame(_animFrame),
							0);
					}
					else if (customMarker)
					{
						BlitRaw(
							surface,
							_arrow,
							screenPosition.x + offset.x + (_spriteWidth / 2) - (_arrow.Width / 2),
							screenPosition.y + offset.y - _arrow.Height + GetArrowBobForFrame(_animFrame),
							0,
							false,
							_isTFTD ? ArrowColorsTFTD[myUnit.CustomMarker % 4] : ArrowColorsUFO[myUnit.CustomMarker % 4]);
					}
				}
			}
		}
		_numWaypid = null;

		// Draw craft deployment preview arrows
		if (_isAltPressed && _save.IsPreview && CursorType != CursorType.CT_NONE)
		{
			foreach (var pos in _save.CraftTiles)
			{
				if (pos.z == _camera.ViewLevel)
				{
					_camera.ConvertMapToScreen(pos, ref screenPosition);
					screenPosition += _camera.MapOffset;
					screenPosition.y += 2; // based on vanilla soldier standHeight
					_arrow.BlitNShade(
						surface,
						screenPosition.x + (_spriteWidth / 2) - (_arrow.Width / 2),
						screenPosition.y - _arrow.Height + GetArrowBobForFrame(_animFrame),
						0);
				}
			}
		}

		// check if we got big explosions
		if (_explosionInFOV)
		{
			// big explosions cause the screen to flash as bright as possible before any explosions are actually drawn.
			// this causes everything to look like EGA for a single frame.
			if (_flashScreen)
			{
				// Note: like C++ this only advances on non-zero pixels; unreachable because SetBlastFlash always clears the flag.
				for (int x = 0, y = 0; x < surface.Width && y < surface.Height;)
				{
					byte pixel = surface.GetPixel(x, y);
					if (pixel != 0)
					{
						pixel = (byte)((pixel & 0xF0) + 1); //avoid 0 pixel
						surface.SetPixelIterative(ref x, ref y, pixel);
					}
				}
				_flashScreen = false;
			}
			else
			{
				foreach (var explosion in _explosions)
				{
					_camera.ConvertVoxelToScreen(explosion.Position, ref bulletPositionScreen);
					if (explosion.IsBig)
					{
						if (explosion.CurrentFrame >= 0)
						{
							tmpSurface = _game.Mod.GetSurfaceSet("X1.PCK").GetFrame(explosion.CurrentFrame);
							if (tmpSurface != null)
								BlitRaw(surface, tmpSurface, bulletPositionScreen.x - (tmpSurface.Width / 2), bulletPositionScreen.y - (tmpSurface.Height / 2), 0, false, _nvColor);
						}
					}
					else if (explosion.IsHit)
					{
						tmpSurface = _game.Mod.GetSurfaceSet("HIT.PCK").GetFrame(explosion.CurrentFrame);
						BlitRaw(surface, tmpSurface, bulletPositionScreen.x - 15, bulletPositionScreen.y - 25, 0, false, _nvColor);
					}
					else
					{
						tmpSurface = _game.Mod.GetSurfaceSet("SMOKE.PCK").GetFrame(explosion.CurrentFrame);
						BlitRaw(surface, tmpSurface, bulletPositionScreen.x - 15, bulletPositionScreen.y - 15, 0, false, _nvColor);
					}
				}
			}
		}

		surface.Unlock();
	}

	/// <summary>
	/// UFO extender accuracy: displays the adjusted accuracy value (and with Alt the damage/psi info) on the crosshair
	/// (inlined in C++ drawTerrain).
	/// </summary>
	private void DrawCursorInfo(Surface surface, Tile tile, BattleUnit unit, int itX, int itY, int itZ, Position screenPosition)
	{
		BattleAction action = _save.BattleGame.CurrentAction;
		RuleItem weapon = action.weapon.Rules;
		var ss = new StringBuilder();
		BattleActionAttack attack = BattleActionAttack.GetBeforeShoot(action);
		int distanceSq = action.actor.Distance3dToPositionSq(new Position(itX, itY, itZ));
		int distance = (int)Math.Ceiling(MathF.Sqrt(distanceSq));

		if (_cursorType == CursorType.CT_AIM || _cursorType == CursorType.CT_THROW)
		{
			int accuracy = BattleUnit.GetFiringAccuracy(attack, _game.Mod);

			{
				int upperLimit = 0, lowerLimit = 0;
				int dropoff = weapon.CalculateLimits(ref upperLimit, ref lowerLimit, _save.Depth, action.type);

				// at this point, let's assume the shot is adjusted and set the text amber.
				_txtAccuracy.SetColor((byte)(Acom.Palette.BlockOffset((byte)(Pathfinding.yellow - 1)) - 1));

				if (distance > upperLimit)
				{
					accuracy -= (distance - upperLimit) * dropoff;
				}
				else if (distance < lowerLimit)
				{
					accuracy -= (lowerLimit - distance) * dropoff;
				}
				else
				{
					// no adjustment made? set it to green.
					_txtAccuracy.SetColor((byte)(Acom.Palette.BlockOffset((byte)(Pathfinding.green - 1)) - 1));
				}
			}

			// Include LOS penalty for tiles in the unit's current view range
			// Don't recalculate LOS for outside of the current FOV
			int noLOSAccuracyPenalty = action.weapon.Rules.GetNoLOSAccuracyPenalty(_game.Mod);
			if (noLOSAccuracyPenalty != -1)
			{
				bool hasLOS = false;
				if (new Position(itX, itY, itZ) == _cacheCursorPosition && _isCtrlPressed == _cacheIsCtrlPressed && _cacheHasLOS != -1)
				{
					// use cached result
					hasLOS = (_cacheHasLOS == 1);
				}
				else
				{
					// recalculate
					if (IsUnitShown(unit))
					{
						hasLOS = _save.TileEngine.Visible(action.actor, tile);
					}
					else
					{
						hasLOS = _save.TileEngine.IsTileInLOS(action, tile, true);
					}
					// remember
					_cacheIsCtrlPressed = _isCtrlPressed;
					_cacheCursorPosition = new Position(itX, itY, itZ);
					_cacheHasLOS = hasLOS ? 1 : 0;
				}

				if (!hasLOS)
				{
					accuracy = accuracy * noLOSAccuracyPenalty / 100;
					_txtAccuracy.SetColor((byte)(Acom.Palette.BlockOffset((byte)(Pathfinding.yellow - 1)) - 1));
				}
			}

			bool outOfRange = action.type == BA_THROW
				? weapon.IsOutOfThrowRange(distanceSq, _save.Depth)
				: weapon.IsOutOfRange(distanceSq);

			// zero accuracy or out of range: set it red.
			if (accuracy <= 0 || outOfRange)
			{
				accuracy = 0;
				_txtAccuracy.SetColor((byte)(Acom.Palette.BlockOffset((byte)(Pathfinding.red - 1)) - 1));
			}
			ss.Append(accuracy);
			ss.Append('%');
		}

		//TODO: merge this code with `InventoryState::calculateCurrentDamageTooltip` as 90% is same or should be same
		// display additional damage and psi-effectiveness info
		if (_isAltPressed)
		{
			// step 1: determine rule
			RuleItem rule;
			if (weapon.BattleType == BattleType.BT_PSIAMP)
			{
				rule = weapon;
			}
			else if (action.weapon.NeedsAmmoForAction(action.type))
			{
				var ammo = attack.damage_item;
				if (ammo != null)
				{
					rule = ammo.Rules;
				}
				else
				{
					rule = null; // empty weapon = no rule
				}
			}
			else
			{
				rule = weapon;
			}

			// step 2: check if unlocked
			if (_cacheActiveWeaponUfopediaArticleUnlocked == -1)
			{
				_cacheActiveWeaponUfopediaArticleUnlocked = 0;
				if (_game.SavedGame.MonthsPassed == -1)
				{
					_cacheActiveWeaponUfopediaArticleUnlocked = 1; // new battle mode
				}
				else if (rule != null)
				{
					_cacheActiveWeaponUfopediaArticleUnlocked = 1; // assume unlocked
					ArticleDefinition article = _game.Mod.GetUfopaediaArticle(rule.Type, false);
					if (article != null && !Ufopaedia.IsArticleAvailable(_game.SavedGame, article))
					{
						_cacheActiveWeaponUfopediaArticleUnlocked = 0; // ammo/weapon locked
					}
					if (rule.Type != weapon.Type)
					{
						article = _game.Mod.GetUfopaediaArticle(weapon.Type, false);
						if (article != null && !Ufopaedia.IsArticleAvailable(_game.SavedGame, article))
						{
							_cacheActiveWeaponUfopediaArticleUnlocked = 0; // weapon locked
						}
					}
				}
			}

			// step 3: calculate and draw
			if (rule != null && _cacheActiveWeaponUfopediaArticleUnlocked == 1)
			{
				if (rule.BattleType == BattleType.BT_PSIAMP)
				{
					float attackStrength = BattleUnit.GetPsiAccuracy(attack);
					float defenseStrength = 30.0f; // indicator ignores: +victim->getArmor()->getPsiDefence(victim);

					float dis = Position.Distance(action.actor.Position.ToVoxel(), new Position(itX, itY, itZ).ToVoxel());
					int min = (int)(attackStrength - defenseStrength - rule.GetPsiAccuracyRangeReduction(dis));
					int max = min + 55;
					if (max <= 0)
					{
						ss.Append("0%");
					}
					else
					{
						ss.Append(min).Append('-').Append(max).Append('%');
					}
				}
				if (rule.BattleType != BattleType.BT_PSIAMP || action.type == BA_USE)
				{
					int totalDamage = 0;
					if (weapon.IgnoreAmmoPower)
					{
						totalDamage += weapon.GetPowerBonus(attack);
						totalDamage = (int)(totalDamage - weapon.GetPowerRangeReduction(distance * 16));
					}
					else
					{
						totalDamage += rule.GetPowerBonus(attack);
						totalDamage = (int)(totalDamage - rule.GetPowerRangeReduction(distance * 16));
					}
					if (totalDamage < 0) totalDamage = 0;
					if (_cursorType != CursorType.CT_WAYPOINT)
						ss.Append('\n');
					ss.Append(rule.DamageType.GetRandomDamage(totalDamage, 1));
					ss.Append('-');
					ss.Append(rule.DamageType.GetRandomDamage(totalDamage, 2));
					if (rule.DamageType.RandomType == ItemDamageRandomType.DRT_UFO_WITH_TWO_DICE)
						ss.Append('*');
				}
			}
			else
			{
				ss.Append("\n?-?");
			}
		}

		_txtAccuracy.SetText(ss.ToString());
		_txtAccuracy.Draw();
		_txtAccuracy.BlitNShade(surface, screenPosition.x, screenPosition.y, 0);
	}

	/// <summary>Handles mouse presses on the map.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void MousePress(Action action, State state)
	{
		base.MousePress(action, state);
		_camera.MousePress(action, state);
	}

	/// <summary>Handles mouse releases on the map.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void MouseRelease(Action action, State state)
	{
		base.MouseRelease(action, state);
		_camera.MouseRelease(action, state);
	}

	/// <summary>Handles keyboard presses on the map.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void KeyboardPress(Action action, State state)
	{
		base.KeyboardPress(action, state);
		_camera.KeyboardPress(action, state);
	}

	/// <summary>Handles map vision toggle mode.</summary>
	public void EnableNightVision()
	{
		_nightVisionOn = true;
		_debugVisionMode = 0;
		PersistToggles();
	}

	/// <summary>Toggles the night-vision mode.</summary>
	public void ToggleNightVision()
	{
		_nightVisionOn = !_nightVisionOn;
		_debugVisionMode = 0;
		PersistToggles();
	}

	/// <summary>Cycles the debug brightness modes.</summary>
	public void ToggleDebugVisionMode()
	{
		_debugVisionMode = (_debugVisionMode + 1) % 3;
		_nightVisionOn = false;
		PersistToggles();
	}

	/// <summary>Stores the vision toggles in the campaign or the battle, depending on the options.</summary>
	public void PersistToggles()
	{
		if (Options.oxceToggleNightVisionType == 2)
		{
			// persisted per campaign
			_game.SavedGame.ToggleNightVision = _nightVisionOn;
		}
		else if (Options.oxceToggleNightVisionType == 1)
		{
			// persisted per battle
			_save.ToggleNightVision = _nightVisionOn;
		}

		if (Options.oxceToggleBrightnessType == 2)
		{
			// persisted per campaign
			_game.SavedGame.ToggleBrightness = _debugVisionMode;
		}
		else if (Options.oxceToggleBrightnessType == 1)
		{
			// persisted per battle
			_save.ToggleBrightness = _debugVisionMode;
		}

		_save.SetToggleBrightnessTemp(_debugVisionMode);
	}

	/// <summary>Handles fade-in and fade-out shade modification.</summary>
	/// <param name="tile">Tile whose (original) shade is modified.</param>
	public int ReShade(Tile tile)
	{
		ArgumentNullException.ThrowIfNull(tile);
		// when modders just don't know where to stop...
		if (_debugVisionMode > 0)
		{
			if (_debugVisionMode == 1)
			{
				// Reaver's tests
				return tile.Shade / 2;
			}
			// Meridian's debug helper
			return 0;
		}

		// no night vision
		if (_nvColor == 0)
		{
			return tile.Shade;
		}

		// already bright enough
		if ((tile.Shade <= NIGHT_VISION_SHADE))
		{
			return tile.Shade;
		}

		// hybrid night vision (local)
		foreach (var bu in _save.Units)
		{
			if (bu.Faction == FACTION_PLAYER && !bu.IsOut)
			{
				if (Position.Distance2dSq(tile.Position, bu.Position) <= bu.MaxViewDistanceAtDarkSquared)
				{
					return tile.Shade > _fadeShade ? _fadeShade : tile.Shade;
				}
			}
		}

		// hybrid night vision (global)
		return Math.Min(+NIGHT_VISION_MAX_SHADE, tile.Shade);
	}

	/// <summary>Handles keyboard releases on the map.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void KeyboardRelease(Action action, State state)
	{
		base.KeyboardRelease(action, state);
		_camera.KeyboardRelease(action, state);
	}

	/// <summary>Handles mouse over events on the map.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void MouseOver(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.MouseOver(action, state);
		_camera.MouseOver(action, state);
		_mouseX = (int)action.AbsoluteXMouse;
		_mouseY = (int)action.AbsoluteYMouse;
		SetSelectorPosition(_mouseX, _mouseY);
	}

	/// <summary>Sets the selector to a certain tile on the map.</summary>
	/// <param name="mx">mouse x position.</param>
	/// <param name="my">mouse y position.</param>
	public void SetSelectorPosition(int mx, int my)
	{
		int oldX = _selectorX, oldY = _selectorY;

		_camera.ConvertScreenToMap(mx, my + _spriteHeight / 4, ref _selectorX, ref _selectorY);

		if (oldX != _selectorX || oldY != _selectorY)
		{
			_redraw = true;
		}
	}

	/// <summary>Handles animating tiles. 8 Frames per animation.</summary>
	/// <param name="redraw">Redraw the battlescape?</param>
	public void Animate(bool redraw)
	{
		_save.NextAnimFrame();
		_animFrame = _save.AnimFrame;

		// random ambient sounds
		{
			if (_save.AmbienceRandom.Count != 0)
			{
				_save.DecreaseCurrentAmbienceDelay();
				if (_save.CurrentAmbienceDelay <= 0)
				{
					_save.ResetCurrentAmbienceDelay();
					_save.PlayRandomAmbientSound();
				}
			}
		}

		// animate tiles
		for (int i = 0; i < _save.MapSizeXYZ; ++i)
		{
			_save.GetTile(i).Animate();
		}

		// animate vapor
		for (int i = 0; i < _vaporParticles.Count; ++i)
		{
			var v = _vaporParticles[i];
			int posX = i % _camera.MapSizeX;
			int posY = i / _camera.MapSizeX;

			v.RemoveAll(
				p =>
				{
					if (p.Animate())
					{
						Position tileOffset = p.UpdateScreenPosition();
						if (tileOffset != new Position(0, 0, 0))
						{
							AddVaporParticle(new Position(posX, posY, 0) + tileOffset, p);
							return true;
						}
						return false;
					}
					else
					{
						return true;
					}
				}
			);
		}

		// init vapor vector
		for (int i = 0; i < _vaporParticlesInit.Count; ++i)
		{
			var vi = _vaporParticlesInit[i];
			var vDest = _vaporParticles[i];
			if (vi.Count == 0)
			{
				continue;
			}

			if (vDest.Count == 0)
			{
				// vi.swap(vDest)
				_vaporParticles[i] = vi;
				_vaporParticlesInit[i] = vDest;
				vi = vDest;
			}
			else
			{
				vDest.InsertRange(0, vi);
			}


			vi.Clear();
		}

		foreach (var tilePar in _vaporParticles)
		{
			if (tilePar.Count == 0)
			{
				tilePar.Clear();
			}
			else
			{
				SortByLayer(tilePar);
			}
		}

		// animate certain units (large flying units have a propulsion animation)
		foreach (var bu in _save.Units)
		{
			Position pos = bu.Position;

			// skip units that do not have position
			if (pos == TileEngine.invalid)
			{
				continue;
			}

			if (_save.Depth > 0)
			{
				bu.FloorAbove = false;

				// make sure this unit isn't obscured by the floor above him, otherwise it looks weird.
				if (_camera.ViewLevel > pos.z)
				{
					for (int z = Math.Min(_camera.ViewLevel, _save.MapSizeZ - 1); z != pos.z; --z)
					{
						if (!_save.GetTile(new Position(pos.x, pos.y, z)).HasNoFloor(null))
						{
							bu.FloorAbove = true;
							break;
						}
					}
				}
			}

			bu.Breathe();
		}

		if (redraw) _redraw = true;
	}

	/// <summary>
	/// Sorts the particles of a tile by layer (C++ std::sort by getLayerZ).
	/// Deviation: stable (insertion sort); libstdc++ also uses insertion sort for up to 16 elements,
	/// larger lists may order particles of the same layer differently there.
	/// </summary>
	internal static void SortByLayer(List<Particle> list)
	{
		for (int i = 1; i < list.Count; ++i)
		{
			Particle p = list[i];
			int j = i - 1;
			while (j >= 0 && p.LayerZ < list[j].LayerZ)
			{
				list[j + 1] = list[j];
				--j;
			}
			list[j + 1] = p;
		}
	}

	/// <summary>Gets the currently selected position.</summary>
	/// <param name="pos">Pointer to a position.</param>
	public void GetSelectorPosition(ref Position pos)
	{
		pos.x = (short)_selectorX;
		pos.y = (short)_selectorY;
		pos.z = (short)_camera.ViewLevel;
	}

	/// <summary>Calculates the offset of a soldier, when it is walking in the middle of 2 tiles.</summary>
	/// <param name="unit">Pointer to BattleUnit.</param>
	/// <returns>The offset.</returns>
	public UnitWalkingOffset CalculateWalkingOffset(BattleUnit unit)
	{
		ArgumentNullException.ThrowIfNull(unit);
		UnitWalkingOffset result = default;

		int[] offsetX = [1, 1, 1, 0, -1, -1, -1, 0];
		int[] offsetY = [1, 0, -1, -1, -1, 0, 1, 1];
		int phase = unit.WalkingPhase + unit.DiagonalWalkingPhase;
		int dir = unit.Direction;
		int midphase = 4 + 4 * (dir % 2);
		int endphase = 8 + 8 * (dir % 2);
		int size = unit.Armor.Size;

		result.ScreenOffset.x = 0;
		result.ScreenOffset.y = 0;

		if (size > 1)
		{
			if (dir < 1 || dir > 5)
				midphase = endphase;
			else if (dir == 5)
				midphase = 12;
			else if (dir == 1)
				midphase = 5;
			else
				midphase = 1;
		}
		if (unit.VerticalDirection != 0)
		{
			midphase = 4;
			endphase = 8;
		}
		else if ((unit.Status == STATUS_WALKING || unit.Status == STATUS_FLYING))
		{
			if (phase < midphase)
			{
				result.ScreenOffset.x = (short)(phase * 2 * offsetX[dir]);
				result.ScreenOffset.y = (short)(-phase * offsetY[dir]);
			}
			else
			{
				result.ScreenOffset.x = (short)((phase - endphase) * 2 * offsetX[dir]);
				result.ScreenOffset.y = (short)(-(phase - endphase) * offsetY[dir]);
			}
		}

		result.NormalizedMovePhase = endphase == 16 ? phase : phase * 2;

		// If we are walking in between tiles, interpolate it's terrain level.
		if (unit.Status == STATUS_WALKING || unit.Status == STATUS_FLYING)
		{
			Position posCurr = unit.Position;
			Position posDest = unit.Destination;
			Position posLast = unit.LastPosition;
			if (phase < midphase)
			{
				int fromLevel = GetTerrainLevel(posCurr, size);
				int toLevel = GetTerrainLevel(posDest, size);
				if (posCurr.z > posDest.z)
				{
					// going down a level, so toLevel 0 becomes +24, -8 becomes  16
					toLevel += Position.TileZ * (posCurr.z - posDest.z);
				}
				else if (posCurr.z < posDest.z)
				{
					// going up a level, so toLevel 0 becomes -24, -8 becomes -16
					toLevel = -Position.TileZ * (posDest.z - posCurr.z) + Math.Abs(toLevel);
				}
				result.TerrainLevelOffset = Interpolate(fromLevel, toLevel, phase, endphase);
			}
			else
			{
				// from phase 4 onwards the unit behind the scenes already is on the destination tile
				// we have to get it's last position to calculate the correct offset
				int fromLevel = GetTerrainLevel(posLast, size);
				int toLevel = GetTerrainLevel(posDest, size);
				if (posLast.z > posDest.z)
				{
					// going down a level, so fromLevel 0 becomes -24, -8 becomes -32
					fromLevel -= Position.TileZ * (posLast.z - posDest.z);
				}
				else if (posLast.z < posDest.z)
				{
					// going up a level, so fromLevel 0 becomes +24, -8 becomes 16
					fromLevel = Position.TileZ * (posDest.z - posLast.z) - Math.Abs(fromLevel);
				}
				result.TerrainLevelOffset = Interpolate(fromLevel, toLevel, phase, endphase);
			}
		}
		else
		{
			result.TerrainLevelOffset = GetTerrainLevel(unit.Position, size);
		}
		result.ScreenOffset.y = (short)(result.ScreenOffset.y + result.TerrainLevelOffset);
		return result;
	}

	/// <summary>
	/// Terrainlevel goes from 0 to -24. For a larger sized unit, we need to pick the highest terrain level, which is the lowest number...
	/// </summary>
	/// <param name="pos">Position.</param>
	/// <param name="size">Size of the unit we want to get the level from.</param>
	/// <returns>terrainlevel.</returns>
	private int GetTerrainLevel(Position pos, int size)
	{
		int lowestlevel = 0;

		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				int l = _save.GetTile(pos + new Position(x, y, 0)).TerrainLevel;
				if (l < lowestlevel)
					lowestlevel = l;
			}
		}

		return lowestlevel;
	}

	/// <summary>Sets the 3D cursor to selection/aim mode.</summary>
	/// <param name="type">Cursor type.</param>
	/// <param name="size">Size of cursor.</param>
	public void SetCursorType(CursorType type, int size = 1)
	{
		// reset cursor indicator cache
		_cacheActiveWeaponUfopediaArticleUnlocked = -1;
		_cacheIsCtrlPressed = false;
		_cacheCursorPosition = TileEngine.invalid;
		_cacheHasLOS = -1;

		_cursorType = type;
		if (_cursorType == CursorType.CT_NORMAL)
			_cursorSize = size;
		else
			_cursorSize = 1;
	}

	/// <summary>Gets the cursor type.</summary>
	public CursorType CursorType => _cursorType;

	/// <summary>The current projectile sprite on the map (null if none); setting one starts the camera follow.</summary>
	public Projectile Projectile
	{
		get => _projectile;
		set
		{
			_projectile = value;
			if (value != null && Options.battleSmoothCamera)
			{
				_launch = true;
			}
		}
	}

	/// <summary>Follow projectile flag.</summary>
	public bool FollowProjectile { get => _followProjectile; set => _followProjectile = value; }

	/// <summary>Gets alt pressed flag.</summary>
	public bool IsAltPressed => _isAltPressed;

	/// <summary>Gets ctrl pressed flag.</summary>
	public bool IsCtrlPressed => _isCtrlPressed;

	/// <summary>Add new vapor particle.</summary>
	/// <param name="pos">Tile position of particle.</param>
	/// <param name="particle">Particle to add (copied, C++ takes it by value).</param>
	public void AddVaporParticle(Position pos, Particle particle)
	{
		ArgumentNullException.ThrowIfNull(particle);
		if (_transparencies.Count < (particle.Color + 1) * Mod.TransparenciesOpacityLevels * Mod.TransparenciesPaletteColors)
		{
			return;
		}
		if (pos.x >= _camera.MapSizeX || pos.y >= _camera.MapSizeY)
		{
			return;
		}
		if (pos.x < 0 || pos.y < 0)
		{
			return;
		}

		var v = _vaporParticlesInit[_camera.MapSizeX * pos.y + pos.x];

		// as there will usually be more than one Particle, we prepare more space
		if (v.Capacity < 64)
		{
			v.Capacity = 64;
		}

		v.Add(new Particle(particle));
	}

	/// <summary>Get all vapor for tile.</summary>
	/// <param name="tile">current tile.</param>
	/// <param name="topLayer">bit 0: upper half of the tile layer; bit 1: also return particles that belong to upper tiles.</param>
	/// <returns>range of particles that should be drawn (valid until the next <see cref="Animate"/>).</returns>
	public ReadOnlySpan<Particle> GetVaporParticle(Tile tile, int topLayer)
	{
		ArgumentNullException.ThrowIfNull(tile);
		Position pos = tile.Position;
		ReadOnlySpan<Particle> v = CollectionsMarshal.AsSpan(_vaporParticles[_camera.MapSizeX * pos.y + pos.x]);
		int startZ = pos.z * Particle.LayerAccuracy + (topLayer & 1);
		int endZ = startZ + Particle.LayerAccuracy / 2;
		int s = PartitionPoint(v, 0, startZ);
		int e = (topLayer & 2) != 0 ? v.Length : PartitionPoint(v, s, endZ);
		return v[s..e];
	}

	/// <summary>std::partition_point with "layerZ &lt; limit" over [begin, end of span).</summary>
	private static int PartitionPoint(ReadOnlySpan<Particle> v, int begin, int limit)
	{
		int first = begin;
		int count = v.Length - begin;
		while (count > 0)
		{
			int step = count / 2;
			int it = first + step;
			if (v[it].LayerZ < limit)
			{
				first = it + 1;
				count -= step + 1;
			}
			else
			{
				count = step;
			}
		}
		return first;
	}

	/// <summary>Gets a list of explosion sprites on the map.</summary>
	public List<Explosion> Explosions => _explosions;

	/// <summary>Gets the pointer to the camera.</summary>
	public Camera Camera => _camera;

	/// <summary>Timers only work on surfaces so we have to pass this on to the camera object.</summary>
	public void ScrollMouse()
	{
		_camera.ScrollMouse();
	}

	/// <summary>Timers only work on surfaces so we have to pass this on to the camera object.</summary>
	public void ScrollKey()
	{
		_camera.ScrollKey();
	}

	/// <summary>Modify the fade shade level if fade's in progress.</summary>
	public void FadeShade()
	{
		bool hold = _game.IsKeyDown(Options.keyNightVisionHold);
		if ((_nightVisionOn && !hold) || (!_nightVisionOn && hold))
		{
			_nvColor = Options.oxceNightVisionColor;
			_save.SetToggleNightVisionTemp(true);
			_save.SetToggleNightVisionColorTemp(_nvColor);
			if (_fadeShade > NIGHT_VISION_SHADE) // 0 = max brightness
			{
				--_fadeShade;
			}
		}
		else
		{
			if (_nvColor != 0)
			{
				if (_fadeShade < _save.GlobalShade)
				{
					// gradually fade away
					++_fadeShade;
				}
				else
				{
					// and at the end turn off night vision
					_nvColor = 0;
					_save.SetToggleNightVisionTemp(false);
					_save.SetToggleNightVisionColorTemp(0);
				}
			}
		}
	}

	/// <summary>Gets a list of waypoints on the map.</summary>
	public List<Position> Waypoints => _waypoints;

	/// <summary>Sets mouse-buttons' pressed state.</summary>
	/// <param name="button">Index of the button.</param>
	/// <param name="pressed">The state of the button.</param>
	public void SetButtonsPressed(byte button, bool pressed)
	{
		SetButtonPressed(button, pressed);
	}

	/// <summary>Sets the unitDying flag.</summary>
	/// <param name="flag">True if the unit is dying.</param>
	public void SetUnitDying(bool flag)
	{
		_unitDying = flag;
	}

	/// <summary>Updates the selector to the last-known mouse position.</summary>
	public void RefreshSelectorPosition()
	{
		SetSelectorPosition(_mouseX, _mouseY);
	}

	/// <summary>Special handling for setting the height of the map viewport.</summary>
	/// <param name="height">the new base screen height.</param>
	public override void SetHeight(int height)
	{
		base.SetHeight(height);
		_visibleMapHeight = height - _iconHeight;
		_message.SetHeight((_visibleMapHeight < 200) ? _visibleMapHeight : 200);
		_message.Y = (_visibleMapHeight - _message.Height) / 2;
	}

	/// <summary>Special handling for setting the width of the map viewport.</summary>
	/// <param name="width">the new base screen width.</param>
	public override void SetWidth(int width)
	{
		int dX = width - Width;
		base.SetWidth(width);
		_message.X = _message.X + dX / 2;
	}

	/// <summary>Get the hidden movement screen's vertical position.</summary>
	public int MessageY => _message.Y;

	/// <summary>Get the icon height.</summary>
	public int IconHeight => _iconHeight;

	/// <summary>Get the icon width.</summary>
	public int IconWidth => _iconWidth;

	/// <summary>
	/// Returns the angle(left/right balance) of a sound effect,
	/// based off a map position.
	/// </summary>
	/// <param name="pos">the map position to calculate the sound angle from.</param>
	/// <returns>the angle of the sound (280 to 440).</returns>
	public int GetSoundAngle(Position pos)
	{
		int midPoint = Width / 2;
		Position relativePosition = default;

		_camera.ConvertMapToScreen(pos, ref relativePosition);
		// cap the position to the screen edges relative to the center,
		// negative values indicating a left-shift, and positive values shifting to the right.
		relativePosition.x = (short)Math.Clamp((relativePosition.x + _camera.MapOffset.x) - midPoint, -midPoint, midPoint);

		// convert the relative distance to a relative increment of an 80 degree angle
		// we use +- 80 instead of +- 90, so as not to go ALL the way left or right
		// which would effectively mute the sound out of one speaker.
		// since Mix_SetPosition uses modulo 360, we can't feed it a negative number, so add 360 instead.
		return (int)(360 + (relativePosition.x / (midPoint / 80.0)));
	}

	/// <summary>Reset the camera smoothing bool.</summary>
	public void ResetCameraSmoothing()
	{
		_smoothingEngaged = false;
	}

	/// <summary>The "explosion flash" bool: should the screen be rendered in EGA this frame? (setting always clears it).</summary>
	public bool BlastFlash
	{
		get => _flashScreen;
		set
		{
			_flashScreen = value;

			// Meridian: no frikin flashing!!
			_flashScreen = false;
		}
	}

	/// <summary>Resets obstacle markers.</summary>
	public void ResetObstacles()
	{
		for (int z = 0; z < _save.MapSizeZ; z++)
			for (int y = 0; y < _save.MapSizeY; y++)
				for (int x = 0; x < _save.MapSizeX; x++)
				{
					Tile tile = _save.GetTile(new Position(x, y, z));
					tile?.ResetObstacle();
				}
		_showObstacles = false;
	}

	/// <summary>Enables obstacle markers.</summary>
	public void EnableObstacles()
	{
		_showObstacles = true;
		if (_obstacleTimer != null)
		{
			_obstacleTimer.Stop();
			_obstacleTimer.Start();
		}
	}

	/// <summary>Disables obstacle markers.</summary>
	public void DisableObstacles()
	{
		_showObstacles = false;
		_obstacleTimer?.Stop();
	}
}
