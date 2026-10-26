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
// Ported from: src/Battlescape/BattlescapeState.h, src/Battlescape/BattlescapeState.cpp
// Port status: partial (missing: saveAIMap/saveVoxelMap/saveVoxelView debug image dumps (no PNG encoder yet))
#nullable disable

using System.Runtime.CompilerServices;
using static Acom.BattleActionType;
using static Acom.UnitFaction;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Battlescape screen which shows the tactical battle.
/// </summary>
/// <remarks>The C++ destructor logic (palette reset) runs in <see cref="Dispose()"/>, called by <see cref="Acom.Game"/> when the state is deleted.</remarks>
public partial class BattlescapeState : State
{
	public const int DEFAULT_ANIM_SPEED = 100;
	private const int SPECIAL_BUTTONS_MAX = 3;
	private const int VISIBLE_MAX = 10;

	private readonly Surface _rank, _rankTiny;
	private readonly InteractiveSurface _icons;
	private readonly Map _map;
	private readonly BattlescapeButton _btnUnitUp, _btnUnitDown, _btnMapUp, _btnMapDown, _btnShowMap, _btnKneel;
	private readonly BattlescapeButton _btnInventory, _btnCenter, _btnNextSoldier, _btnNextStop, _btnShowLayers, _btnHelp;
	private readonly BattlescapeButton _btnEndTurn, _btnAbort, _btnLaunch, _btnPsi, _btnSpecial, _btnSkills;
	/// <summary>Pressed button of the reserve group (C++ BattlescapeButton* shared with the buttons).</summary>
	private readonly StrongBox<BattlescapeButton> _reserve = new();
	private readonly BattlescapeButton _btnCtrl, _btnAlt, _btnShift, _btnRMB, _btnMMB;
	private bool _touchButtonsEnabled;
	private readonly InteractiveSurface _btnStats;
	private readonly BattlescapeButton _btnReserveNone, _btnReserveSnap, _btnReserveAimed, _btnReserveAuto, _btnReserveKneel, _btnZeroTUs;
	private readonly InteractiveSurface _btnLeftHandItem, _btnRightHandItem;

	private readonly int[] _posSpecialActions = new int[SPECIAL_BUTTONS_MAX];

	private readonly string[] _txtVisibleUnitTooltip = new string[VISIBLE_MAX + 2];
	private readonly InteractiveSurface[] _btnVisibleUnit = new InteractiveSurface[VISIBLE_MAX];
	private readonly NumberText[] _numVisibleUnit = new NumberText[VISIBLE_MAX];
	private readonly BattleUnit[] _visibleUnit = new BattleUnit[VISIBLE_MAX];

	private readonly WarningMessage _warning;
	private readonly Text _txtName;
	private readonly NumberText _numTimeUnits, _numEnergy, _numHealth, _numMorale, _numLayers;
	private readonly List<NumberText> _numAmmoLeft = [], _numAmmoRight = [];
	private readonly List<NumberText> _numMedikitLeft = [], _numMedikitRight = [];
	private readonly NumberText _numTwoHandedIndicatorLeft, _numTwoHandedIndicatorRight;
	private readonly byte _twoHandedRed, _twoHandedGreen;
	private readonly Bar _barTimeUnits, _barEnergy, _barHealth, _barMorale, _barMana;
	private readonly bool _manaBarVisible;
	private readonly Timer _animTimer, _gameTimer;
	private readonly SavedBattleGame _save;
	private readonly Text _txtDebug, _txtTooltip;
	private readonly byte _tooltipDefaultColor;
	private readonly byte _medikitRed, _medikitGreen, _medikitBlue, _medikitOrange;
	private readonly List<State> _popups = [];
	private readonly BattlescapeGame _battleGame;
	private bool _firstInit, _paletteResetNeeded, _paletteResetRequested;
	private bool _isMouseScrolling, _isMouseScrolled;
	private int _xBeforeMouseScrolling, _yBeforeMouseScrolling;
	private Position _mapOffsetBeforeMouseScrolling;
	private uint _mouseScrollingStartTime;
	private int _totalMouseMoveX, _totalMouseMoveY;
	private bool _mouseMovedOverThreshold;
	private bool _mouseOverIcons;
	private string _currentTooltip = "";
	private Position _cursorPosition;
	private readonly byte _barHealthColor;
	private int _autosave;
	private int _numberOfDirectlyVisibleUnits, _numberOfEnemiesTotal, _numberOfEnemiesTotalPlusWounded;
	private readonly byte _indicatorTextColor, _indicatorGreen, _indicatorBlue, _indicatorPurple;
	private bool _disposed;

	// C++ function-local statics
	private static bool s_popped;
	private static int s_blinkDelta = 1, s_blinkColor = 32;
	private static byte s_healthColor, s_healthStep;
	private const byte HEALTH_MAX_COLOR = 3;

	/// <summary>
	/// Initializes all the elements in the Battlescape screen.
	/// </summary>
	public BattlescapeState()
	{
		_touchButtonsEnabled = false;
		_manaBarVisible = false;
		_firstInit = true;
		_paletteResetNeeded = false;
		_paletteResetRequested = false;
		_isMouseScrolling = false;
		_isMouseScrolled = false;
		_xBeforeMouseScrolling = 0;
		_yBeforeMouseScrolling = 0;
		_totalMouseMoveX = 0;
		_totalMouseMoveY = 0;
		_mouseMovedOverThreshold = false;
		_mouseOverIcons = false;
		_autosave = 0;
		_numberOfDirectlyVisibleUnits = 0;
		_numberOfEnemiesTotal = 0;
		_numberOfEnemiesTotalPlusWounded = 0;

		_save = _game.SavedGame.SavedBattle;

		RuleInterface bsInterface = _game.Mod.GetInterface("battlescape");
		int screenWidth = Options.baseXResolution;
		int screenHeight = Options.baseYResolution;
		int iconsWidth = bsInterface.GetElement("icons").w;
		int iconsHeight = bsInterface.GetElement("icons").h;
		int visibleMapHeight = screenHeight - iconsHeight;
		int x = screenWidth / 2 - iconsWidth / 2;
		int y = screenHeight - iconsHeight;

		_indicatorTextColor = (byte)bsInterface.GetElement("visibleUnits").color;
		_indicatorGreen = (byte)bsInterface.GetElement("squadsightUnits").color;
		_indicatorBlue = (byte)bsInterface.GetElement("woundedUnits").color;
		_indicatorPurple = (byte)bsInterface.GetElement("passingOutUnits").color;

		_twoHandedRed = (byte)bsInterface.GetElement("twoHandedRed").color;
		_twoHandedGreen = (byte)bsInterface.GetElement("twoHandedGreen").color;

		_tooltipDefaultColor = (byte)bsInterface.GetElement("textTooltip").color;

		_medikitRed = (byte)bsInterface.GetElement("medikitRed").color;
		_medikitGreen = (byte)bsInterface.GetElement("medikitGreen").color;
		_medikitBlue = (byte)bsInterface.GetElement("medikitBlue").color;
		_medikitOrange = (byte)bsInterface.GetElement("medikitOrange").color;

		// Create buttonbar - this should be on the centerbottom of the screen
		_icons = new InteractiveSurface(iconsWidth, iconsHeight, x, y);

		// Create the battlemap view
		// the actual map height is the total height minus the height of the buttonbar
		_map = new Map(_game, screenWidth, screenHeight, 0, 0, visibleMapHeight);

		_numLayers = new NumberText(3, 5, x + 232, y + 6);
		_rank = new Surface(26, 23, x + 107, y + 33);

		// Create buttons
		_btnUnitUp = new BattlescapeButton(32, 16, x + 48, y);
		_btnUnitDown = new BattlescapeButton(32, 16, x + 48, y + 16);
		_btnMapUp = new BattlescapeButton(32, 16, x + 80, y);
		_btnMapDown = new BattlescapeButton(32, 16, x + 80, y + 16);
		_btnShowMap = new BattlescapeButton(32, 16, x + 112, y);
		_btnKneel = new BattlescapeButton(32, 16, x + 112, y + 16);
		_btnInventory = new BattlescapeButton(32, 16, x + 144, y);
		_btnCenter = new BattlescapeButton(32, 16, x + 144, y + 16);
		_btnNextSoldier = new BattlescapeButton(32, 16, x + 176, y);
		_btnNextStop = new BattlescapeButton(32, 16, x + 176, y + 16);
		_btnShowLayers = new BattlescapeButton(32, 16, x + 208, y);
		_btnHelp = new BattlescapeButton(32, 16, x + 208, y + 16);
		_btnEndTurn = new BattlescapeButton(32, 16, x + 240, y);
		_btnAbort = new BattlescapeButton(32, 16, x + 240, y + 16);
		_btnStats = new InteractiveSurface(164, 23, x + 107, y + 33);
		_btnReserveNone = new BattlescapeButton(17, 11, x + 60, y + 33);
		_btnReserveSnap = new BattlescapeButton(17, 11, x + 78, y + 33);
		_btnReserveAimed = new BattlescapeButton(17, 11, x + 60, y + 45);
		_btnReserveAuto = new BattlescapeButton(17, 11, x + 78, y + 45);
		_btnReserveKneel = new BattlescapeButton(10, 23, x + 96, y + 33);
		_btnZeroTUs = new BattlescapeButton(10, 23, x + 49, y + 33);
		_btnLeftHandItem = new InteractiveSurface(32, 48, x + 8, y + 4);
		_btnRightHandItem = new InteractiveSurface(32, 48, x + 280, y + 4);
		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			_numAmmoLeft.Add(new NumberText(30, 5, x + 8, y + 4 + 6 * slot));
			_numAmmoRight.Add(new NumberText(30, 5, x + 280, y + 4 + 6 * slot));
		}
		for (int slot = 0; slot < RuleItem.MedikitSlots; ++slot)
		{
			_numMedikitLeft.Add(new NumberText(30, 5, x + 9, y + 32 + 7 * slot));
			_numMedikitRight.Add(new NumberText(30, 5, x + 281, y + 32 + 7 * slot));
		}
		_numTwoHandedIndicatorLeft = new NumberText(10, 5, x + 36, y + 46);
		_numTwoHandedIndicatorRight = new NumberText(10, 5, x + 308, y + 46);
		int visibleUnitX = bsInterface.GetElement("visibleUnits").x;
		int visibleUnitY = bsInterface.GetElement("visibleUnits").y;
		for (int i = 0; i < VISIBLE_MAX; ++i)
		{
			_btnVisibleUnit[i] = new InteractiveSurface(15, 12, x + visibleUnitX, y + visibleUnitY - (i * 13));
			_numVisibleUnit[i] = new NumberText(15, 12, _btnVisibleUnit[i].X + 6, _btnVisibleUnit[i].Y + 4);
		}
		_numVisibleUnit[9].X = _numVisibleUnit[9].X - 2; // center number 10
		_warning = new WarningMessage(224, 24, x + 48, y + 32);
		_btnLaunch = new BattlescapeButton(32, 24, screenWidth - 32, 0); // we need screenWidth, because that is independent of the black bars on the screen
		_btnLaunch.Visible = false;
		_btnPsi = new BattlescapeButton(32, 24, screenWidth - 32, 25); // we need screenWidth, because that is independent of the black bars on the screen
		_btnPsi.Visible = false;
		_btnSpecial = new BattlescapeButton(32, 24, screenWidth - 32, 25); // we need screenWidth, because that is independent of the black bars on the screen
		_btnSpecial.Visible = false;
		_btnSkills = new BattlescapeButton(32, 24, screenWidth - 32, 25); // we need screenWidth, because that is independent of the black bars on the screen
		_btnSkills.Visible = false;

		{
			int posX = (screenWidth - 32);
			for (int i = 0; i < _posSpecialActions.Length; ++i)
			{
				_posSpecialActions[i] = posX;
				posX -= 32;
			}
		}

		// Reset touch flags
		_game.ResetTouchButtonFlags();

		_btnCtrl = new BattlescapeButton(32, 24, 2, 10);
		_btnAlt = new BattlescapeButton(32, 24, 2, 35);
		_btnShift = new BattlescapeButton(32, 24, 2, 60);
		_btnRMB = new BattlescapeButton(32, 24, 2, 85);
		_btnMMB = new BattlescapeButton(32, 24, 2, 110);

		// Create soldier stats summary
		_rankTiny = new Surface(7, 7, x + 135, y + 33);
		_txtName = new Text(136, 10, x + 135, y + 32);

		_manaBarVisible = _game.Mod.IsManaFeatureEnabled
			&& _game.Mod.IsManaBarEnabled
			&& _game.SavedGame.IsManaUnlocked(_game.Mod);
		int step = _manaBarVisible ? 3 : 4;

		_numTimeUnits = new NumberText(15, 5, x + 136, y + 42);
		_barTimeUnits = new Bar(102, 3, x + 170, y + 41);

		_numEnergy = new NumberText(15, 5, x + 154, y + 42);
		_barEnergy = new Bar(102, 3, x + 170, y + 41 + step);

		_numHealth = new NumberText(15, 5, x + 136, y + 50);
		_barHealth = new Bar(102, 3, x + 170, y + 41 + step * 2);

		_numMorale = new NumberText(15, 5, x + 154, y + 50);
		_barMorale = new Bar(102, 3, x + 170, y + 41 + step * 3);

		if (_manaBarVisible)
		{
			_barMana = new Bar(102, 3, x + 170, y + 41 + step * 4);
		}

		_txtDebug = new Text(300, 10, 20, 0);
		_txtTooltip = new Text(300, 10, x + 2, y - 10);

		// Palette transformations
		RuleEnviroEffects enviro = _save.EnviroEffects;
		if (enviro != null)
		{
			foreach (KeyValuePair<string, string> change in enviro.PaletteTransformations)
			{
				Palette origPal = _game.Mod.GetPalette(change.Key, false);
				Palette newPal = _game.Mod.GetPalette(change.Value, false);
				if (origPal != null && newPal != null)
				{
					origPal.CopyFrom(newPal);
					_paletteResetNeeded = true;
				}
			}
		}

		// Set palette
		_save.SetPaletteByDepth(this);

		if (bsInterface.GetElementOptional("pathfinding") != null)
		{
			Element pathing = bsInterface.GetElement("pathfinding");

			Pathfinding.green = pathing.color;
			Pathfinding.yellow = pathing.color2;
			Pathfinding.red = pathing.border;
		}

		Add(_map);
		Add(_icons);

		// Add in custom reserve buttons
		Surface icons = _game.Mod.GetSurface("ICONS.PCK");
		if (_game.Mod.GetSurface("TFTDReserve", false) != null)
		{
			Surface tftdIcons = _game.Mod.GetSurface("TFTDReserve");
			tftdIcons.BlitNShade(icons, 48, 176);
		}

		// there is some cropping going on here, because the icons image is 320x200 while we only need the bottom of it.
		SurfaceCrop crop = icons.GetCrop();
		crop.Crop.x = 0;
		crop.Crop.y = (short)(200 - iconsHeight);
		crop.Crop.w = (ushort)iconsWidth;
		crop.Crop.h = (ushort)iconsHeight;
		// we need to blit the icons before we add the battlescape buttons, as they copy the underlying parent surface.
		crop.Blit(_icons);

		// this is a hack to fix the single transparent pixel on TFTD's icon panel.
		if (bsInterface.GetElement("icons").TFTDMode)
		{
			_icons.SetPixel(46, 44, 8);
		}

		// custom OXCE links button
		if (Options.oxceLinks && _game.Mod.GetSurface("oxceLinks", false) != null)
		{
			Surface oxceLinks = _game.Mod.GetSurface("oxceLinks");
			oxceLinks.BlitNShade(_icons, 208, 0);

			_numLayers.Visible = false;
		}

		Add(_rank, "rank", "battlescape", _icons);
		Add(_rankTiny, "rank", "battlescape", _icons);
		Add(_btnUnitUp, "buttonUnitUp", "battlescape", _icons);
		Add(_btnUnitDown, "buttonUnitDown", "battlescape", _icons);
		Add(_btnMapUp, "buttonMapUp", "battlescape", _icons);
		Add(_btnMapDown, "buttonMapDown", "battlescape", _icons);
		Add(_btnShowMap, "buttonShowMap", "battlescape", _icons);
		Add(_btnKneel, "buttonKneel", "battlescape", _icons);
		Add(_btnInventory, "buttonInventory", "battlescape", _icons);
		Add(_btnCenter, "buttonCenter", "battlescape", _icons);
		Add(_btnNextSoldier, "buttonNextSoldier", "battlescape", _icons);
		Add(_btnNextStop, "buttonNextStop", "battlescape", _icons);
		Add(_btnShowLayers, "buttonShowLayers", "battlescape", _icons);
		Add(_numLayers, "numLayers", "battlescape", _icons);
		Add(_btnHelp, "buttonHelp", "battlescape", _icons);
		Add(_btnEndTurn, "buttonEndTurn", "battlescape", _icons);
		Add(_btnAbort, "buttonAbort", "battlescape", _icons);
		Add(_btnStats, "buttonStats", "battlescape", _icons);
		Add(_txtName, "textName", "battlescape", _icons);
		// need to do this here, because of TFTD
		if (_game.Mod.GetSurface("AvatarBackground", false) != null)
		{
			// put tiny rank icon where name used to be
			_rankTiny.X = _txtName.X;
			_rankTiny.Y = _txtName.Y + 1;
			// move name more to the right
			_txtName.SetWidth(_txtName.Width - 8);
			_txtName.X = _txtName.X + 8;
		}
		Add(_numTimeUnits, "numTUs", "battlescape", _icons);
		Add(_numEnergy, "numEnergy", "battlescape", _icons);
		Add(_numHealth, "numHealth", "battlescape", _icons);
		Add(_numMorale, "numMorale", "battlescape", _icons);
		Add(_barTimeUnits, "barTUs", "battlescape", _icons);
		Add(_barEnergy, "barEnergy", "battlescape", _icons);
		Add(_barHealth, "barHealth", "battlescape", _icons);
		Add(_barMorale, "barMorale", "battlescape", _icons);
		if (_manaBarVisible)
		{
			Add(_barMana, "barMana", "battlescape", _icons);
		}
		Add(_btnReserveNone, "buttonReserveNone", "battlescape", _icons);
		Add(_btnReserveSnap, "buttonReserveSnap", "battlescape", _icons);
		Add(_btnReserveAimed, "buttonReserveAimed", "battlescape", _icons);
		Add(_btnReserveAuto, "buttonReserveAuto", "battlescape", _icons);
		Add(_btnReserveKneel, "buttonReserveKneel", "battlescape", _icons);
		Add(_btnZeroTUs, "buttonZeroTUs", "battlescape", _icons);
		Add(_btnLeftHandItem, "buttonLeftHand", "battlescape", _icons);
		Add(_btnRightHandItem, "buttonRightHand", "battlescape", _icons);
		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			Add(_numAmmoLeft[slot], "numAmmoLeft", "battlescape", _icons);
			Add(_numAmmoRight[slot], "numAmmoRight", "battlescape", _icons);
		}
		for (int slot = 0; slot < RuleItem.MedikitSlots; ++slot)
		{
			Add(_numMedikitLeft[slot], "numMedikitLeft", "battlescape", _icons);
			Add(_numMedikitRight[slot], "numMedikitRight", "battlescape", _icons);
		}
		Add(_numTwoHandedIndicatorLeft, "numTwoHandedIndicatorLeft", "battlescape", _icons);
		Add(_numTwoHandedIndicatorRight, "numTwoHandedIndicatorRight", "battlescape", _icons);
		for (int i = 0; i < VISIBLE_MAX; ++i)
		{
			Add(_btnVisibleUnit[i]);
			Add(_numVisibleUnit[i]);
		}
		Add(_warning, "warning", "battlescape", _icons);
		Add(_txtDebug);
		Add(_txtTooltip, "textTooltip", "battlescape", _icons);
		Add(_btnLaunch);
		_game.Mod.GetSurfaceSet("SPICONS.DAT").GetFrame(0).BlitNShade(_btnLaunch, 0, 0);
		Add(_btnPsi);
		_game.Mod.GetSurfaceSet("SPICONS.DAT").GetFrame(1).BlitNShade(_btnPsi, 0, 0);
		Add(_btnSpecial);
		_game.Mod.GetSurfaceSet("SPICONS.DAT").GetFrame(1).BlitNShade(_btnSpecial, 0, 0); // use psi button for default
		Add(_btnSkills);
		_game.Mod.GetSurfaceSet("SPICONS.DAT").GetFrame(1).BlitNShade(_btnSkills, 0, 0); // use psi button for default

		Add(_btnCtrl);
		Add(_btnAlt);
		Add(_btnShift);
		Add(_btnRMB);
		Add(_btnMMB);

		SurfaceSet touch = _game.Mod.GetSurfaceSet("Touch");
		touch.GetFrame(0).BlitNShade(_btnCtrl, 0, 0);
		touch.GetFrame(2).BlitNShade(_btnAlt, 0, 0);
		touch.GetFrame(4).BlitNShade(_btnShift, 0, 0);
		touch.GetFrame(6).BlitNShade(_btnRMB, 0, 0);
		touch.GetFrame(8).BlitNShade(_btnMMB, 0, 0);

		_btnCtrl.InitSurfaces(touch.GetFrame(1));
		_btnAlt.InitSurfaces(touch.GetFrame(3));
		_btnShift.InitSurfaces(touch.GetFrame(5));
		_btnRMB.InitSurfaces(touch.GetFrame(7));
		_btnMMB.InitSurfaces(touch.GetFrame(9));

		// Set up objects
		_map.Init();
		_map.OnMouseOver(MapOver);
		_map.OnMousePress(MapPress);
		_map.OnMouseClick(MapClick, 0);
		_map.OnMouseIn(MapIn);

		_numLayers.SetColor((byte)(Acom.Palette.BlockOffset(1) - 2));
		_numLayers.Value = 1;
		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			_numAmmoLeft[slot].Value = 999;
			_numAmmoRight[slot].Value = 999;
		}
		for (int slot = 0; slot < RuleItem.MedikitSlots; ++slot)
		{
			_numMedikitLeft[slot].Value = 999;
			_numMedikitRight[slot].Value = 999;
		}
		_numTwoHandedIndicatorLeft.Value = 2;
		_numTwoHandedIndicatorRight.Value = 2;

		_icons.OnMouseIn(MouseInIcons);
		_icons.OnMouseOut(MouseOutIcons);

		SetUpButton(_btnUnitUp, BtnUnitUpClick, Options.keyBattleUnitUp, "STR_UNIT_LEVEL_ABOVE");
		SetUpButton(_btnUnitDown, BtnUnitDownClick, Options.keyBattleUnitDown, "STR_UNIT_LEVEL_BELOW");
		SetUpButton(_btnMapUp, BtnMapUpClick, Options.keyBattleLevelUp, "STR_VIEW_LEVEL_ABOVE");
		SetUpButton(_btnMapDown, BtnMapDownClick, Options.keyBattleLevelDown, "STR_VIEW_LEVEL_BELOW");
		SetUpButton(_btnShowMap, BtnShowMapClick, Options.keyBattleMap, "STR_MINIMAP");
		SetUpButton(_btnKneel, BtnKneelClick, Options.keyBattleKneel, "STR_KNEEL");
		_btnKneel.AllowToggleInversion();
		SetUpButton(_btnInventory, BtnInventoryClick, Options.keyBattleInventory, "STR_INVENTORY");
		SetUpButton(_btnCenter, BtnCenterClick, Options.keyBattleCenterUnit, "STR_CENTER_SELECTED_UNIT");

		_btnNextSoldier.OnMouseClick(BtnNextSoldierClick, SDLMouse.SDL_BUTTON_LEFT);
		_btnNextSoldier.OnMouseClick(BtnPrevSoldierClick, SDLMouse.SDL_BUTTON_RIGHT);
		_btnNextSoldier.OnKeyboardPress(BtnNextSoldierClick, Options.keyBattleNextUnit);
		_btnNextSoldier.OnKeyboardPress(BtnPrevSoldierClick, Options.keyBattlePrevUnit);
		_btnNextSoldier.Tooltip = "STR_NEXT_UNIT";
		_btnNextSoldier.OnMouseIn(TxtTooltipIn);
		_btnNextSoldier.OnMouseOut(TxtTooltipOut);

		if (Options.oxceSwapDontReselectActions)
		{
			_btnNextStop.OnMouseClick(BtnNextStopMClick, SDLMouse.SDL_BUTTON_LEFT);
			_btnNextStop.OnMouseClick(BtnNextStopRClick, SDLMouse.SDL_BUTTON_RIGHT);
			_btnNextStop.OnMouseClick(BtnNextStopLClick, SDLMouse.SDL_BUTTON_MIDDLE);
			_btnNextStop.OnKeyboardPress(BtnNextStopMClick, Options.keyBattleDeselectUnit);
		}
		else
		{
			_btnNextStop.OnMouseClick(BtnNextStopLClick, SDLMouse.SDL_BUTTON_LEFT);
			_btnNextStop.OnMouseClick(BtnNextStopRClick, SDLMouse.SDL_BUTTON_RIGHT);
			_btnNextStop.OnMouseClick(BtnNextStopMClick, SDLMouse.SDL_BUTTON_MIDDLE);
			_btnNextStop.OnKeyboardPress(BtnNextStopLClick, Options.keyBattleDeselectUnit);
		}
		_btnNextStop.Tooltip = "STR_DESELECT_UNIT";
		_btnNextStop.OnMouseIn(TxtTooltipIn);
		_btnNextStop.OnMouseOut(TxtTooltipOut);

		_btnShowLayers.OnMouseClick(BtnShowLayersClick);
		_btnShowLayers.Tooltip = Options.oxceLinks ? "STR_EXTENDED_LINKS" : "STR_MULTI_LEVEL_VIEW";
		_btnShowLayers.OnMouseIn(TxtTooltipIn);
		_btnShowLayers.OnMouseOut(TxtTooltipOut);
		_btnShowLayers.OnKeyboardPress(BtnShowLayersClickOrig, Options.keyBattleShowLayers);
		_btnShowLayers.OnKeyboardPress(BtnUfopaediaClick, Options.keyGeoUfopedia);

		_btnHelp.OnMouseClick(BtnHelpClick);
		if (!_save.IsPreview)
		{
			_btnHelp.OnKeyboardPress(BtnHelpClick, Options.keyBattleOptions);
		}
		_btnHelp.Tooltip = "STR_OPTIONS";
		_btnHelp.OnMouseIn(TxtTooltipIn);
		_btnHelp.OnMouseOut(TxtTooltipOut);

		_btnEndTurn.OnMouseClick(BtnEndTurnClick);
		_btnEndTurn.OnKeyboardPress(BtnEndTurnClick, Options.keyBattleEndTurn);
		if (_save.IsPreview)
		{
			_btnEndTurn.OnKeyboardPress(BtnEndTurnClick, Options.keyBattleOptions); // Esc
		}
		_btnEndTurn.Tooltip = "STR_END_TURN";
		_btnEndTurn.OnMouseIn(TxtTooltipInEndTurn);
		_btnEndTurn.OnMouseOut(TxtTooltipOut);

		SetUpButton(_btnAbort, BtnAbortClick, Options.keyBattleAbort, "STR_ABORT_MISSION");

		_btnStats.OnMouseClick(BtnStatsClick);
		_btnStats.OnMouseClick(BtnStatsClick, SDLMouse.SDL_BUTTON_RIGHT);
		_btnStats.OnKeyboardPress(BtnStatsClick, Options.keyBattleStats);
		_btnStats.Tooltip = "STR_UNIT_STATS";
		_btnStats.OnMouseIn(TxtTooltipIn);
		_btnStats.OnMouseOut(TxtTooltipOut);

		_btnLeftHandItem.OnMouseClick(BtnLeftHandItemClick);
		_btnLeftHandItem.OnMouseClick(BtnLeftHandItemClick, SDLMouse.SDL_BUTTON_RIGHT);
		_btnLeftHandItem.OnMouseClick(BtnLeftHandItemClick, SDLMouse.SDL_BUTTON_MIDDLE);
		_btnLeftHandItem.OnKeyboardPress(BtnLeftHandItemClick, Options.keyBattleUseLeftHand);
		_btnLeftHandItem.Tooltip = "STR_USE_LEFT_HAND";
		_btnLeftHandItem.OnMouseIn(TxtTooltipInExtraLeftHand);
		_btnLeftHandItem.OnMouseOut(TxtTooltipOut);

		_btnRightHandItem.OnMouseClick(BtnRightHandItemClick);
		_btnRightHandItem.OnMouseClick(BtnRightHandItemClick, SDLMouse.SDL_BUTTON_RIGHT);
		_btnRightHandItem.OnMouseClick(BtnRightHandItemClick, SDLMouse.SDL_BUTTON_MIDDLE);
		_btnRightHandItem.OnKeyboardPress(BtnRightHandItemClick, Options.keyBattleUseRightHand);
		_btnRightHandItem.Tooltip = "STR_USE_RIGHT_HAND";
		_btnRightHandItem.OnMouseIn(TxtTooltipInExtraRightHand);
		_btnRightHandItem.OnMouseOut(TxtTooltipOut);

		SetUpButton(_btnReserveNone, BtnReserveClick, Options.keyBattleReserveNone, "STR_DONT_RESERVE_TIME_UNITS");
		SetUpButton(_btnReserveSnap, BtnReserveClick, Options.keyBattleReserveSnap, "STR_RESERVE_TIME_UNITS_FOR_SNAP_SHOT");
		SetUpButton(_btnReserveAimed, BtnReserveClick, Options.keyBattleReserveAimed, "STR_RESERVE_TIME_UNITS_FOR_AIMED_SHOT");
		SetUpButton(_btnReserveAuto, BtnReserveClick, Options.keyBattleReserveAuto, "STR_RESERVE_TIME_UNITS_FOR_AUTO_SHOT");
		SetUpButton(_btnReserveKneel, BtnReserveKneelClick, Options.keyBattleReserveKneel, "STR_RESERVE_TIME_UNITS_FOR_KNEEL");
		_btnReserveKneel.AllowToggleInversion();

		_btnZeroTUs.OnMouseClick(BtnZeroTUsClick, SDLMouse.SDL_BUTTON_RIGHT);
		_btnZeroTUs.OnKeyboardPress(BtnZeroTUsClick, Options.keyBattleZeroTUs);
		_btnZeroTUs.Tooltip = "STR_EXPEND_ALL_TIME_UNITS";
		_btnZeroTUs.OnMouseIn(TxtTooltipIn);
		_btnZeroTUs.OnMouseOut(TxtTooltipOut);
		_btnZeroTUs.AllowClickInversion();

		// shortcuts without a specific button
		_btnStats.OnKeyboardPress(BtnReloadClick, Options.keyBattleReload);
		_btnStats.OnKeyboardPress(BtnSelectMusicTrackClick, Options.keySelectMusicTrack);
		_btnStats.OnKeyboardPress(BtnPersonalLightingClick, Options.keyBattlePersonalLighting);
		_btnStats.OnKeyboardPress(BtnNightVisionClick, Options.keyNightVisionToggle);
		//_btnStats->onKeyboardPress((ActionHandler)&BattlescapeState::btnTouchButtonsClick, SDLK_t); // for debugging only

		// automatic night vision
		if (_save.GlobalShade > Options.oxceAutoNightVisionThreshold)
		{
			bool ignore = (enviro != null && enviro.IgnoreAutoNightVisionUserSetting());
			if (!ignore)
			{
				// turn personal lights off
				//_save->getTileEngine()->togglePersonalLighting();
				// turn night vision on
				_map.EnableNightVision();
			}
		}

		SDLKey[] buttons = [Options.keyBattleCenterEnemy1,
							Options.keyBattleCenterEnemy2,
							Options.keyBattleCenterEnemy3,
							Options.keyBattleCenterEnemy4,
							Options.keyBattleCenterEnemy5,
							Options.keyBattleCenterEnemy6,
							Options.keyBattleCenterEnemy7,
							Options.keyBattleCenterEnemy8,
							Options.keyBattleCenterEnemy9,
							Options.keyBattleCenterEnemy10];
		for (int i = 0; i < VISIBLE_MAX; ++i)
		{
			_btnVisibleUnit[i].OnMouseClick(BtnVisibleUnitClick);
			_btnVisibleUnit[i].OnMouseClick(BtnVisibleUnitClick, SDLMouse.SDL_BUTTON_RIGHT);
			_btnVisibleUnit[i].OnKeyboardPress(BtnVisibleUnitClick, buttons[i]);
			_txtVisibleUnitTooltip[i] = "STR_CENTER_ON_ENEMY_" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
			_btnVisibleUnit[i].Tooltip = _txtVisibleUnitTooltip[i];
			_btnVisibleUnit[i].OnMouseIn(TxtTooltipIn);
			_btnVisibleUnit[i].OnMouseOut(TxtTooltipOut);
			_numVisibleUnit[i].SetColor(_indicatorTextColor);
			_numVisibleUnit[i].Value = (uint)(i + 1);
		}
		_txtVisibleUnitTooltip[VISIBLE_MAX] = "STR_CENTER_ON_WOUNDED_FRIEND";
		_txtVisibleUnitTooltip[VISIBLE_MAX + 1] = "STR_CENTER_ON_DIZZY_FRIEND";

		_warning.SetColor((byte)bsInterface.GetElement("warning").color2);
		_warning.SetTextColor((byte)bsInterface.GetElement("warning").color);
		_btnLaunch.OnMouseClick(BtnLaunchClick);
		_btnPsi.OnMouseClick(BtnPsiClick);

		_btnSpecial.OnMouseClick(BtnSpecialClick);
		_btnSpecial.OnMouseClick(BtnSpecialClick, SDLMouse.SDL_BUTTON_MIDDLE);
		_btnSpecial.OnKeyboardPress(BtnSpecialClick, Options.keyBattleUseSpecial);
		_btnSpecial.Tooltip = "STR_USE_SPECIAL_ITEM";
		_btnSpecial.OnMouseIn(TxtTooltipInExtraSpecial);
		_btnSpecial.OnMouseOut(TxtTooltipOut);

		_btnSkills.OnMouseClick(BtnSkillsClick);
		_btnSkills.OnKeyboardPress(BtnSkillsClick, Options.keyBattleUseSpecial);

		_btnCtrl.OnMouseClick(BtnCtrlClick);
		_btnAlt.OnMouseClick(BtnAltClick);
		_btnShift.OnMouseClick(BtnShiftClick);
		_btnRMB.OnMouseClick(BtnRMBClick);
		_btnMMB.OnMouseClick(BtnMMBClick);

		_btnCtrl.AllowToggleInversion();
		_btnAlt.AllowToggleInversion();
		_btnShift.AllowToggleInversion();
		_btnRMB.AllowToggleInversion();
		_btnMMB.AllowToggleInversion();

		_btnCtrl.Visible = false;
		_btnAlt.Visible = false;
		_btnShift.Visible = false;
		_btnRMB.Visible = false;
		_btnMMB.Visible = false;

		_txtName.SetHighContrast(true);

		_barTimeUnits.Scale = 1.0;
		_barEnergy.Scale = 1.0;
		_barHealth.Scale = 1.0;
		_barMorale.Scale = 1.0;
		if (_manaBarVisible)
		{
			_barMana.Scale = 1.0;
		}

		_txtDebug.SetColor(Acom.Palette.BlockOffset(8));
		_txtDebug.SetHighContrast(true);

		_txtTooltip.SetHighContrast(true);

		_btnReserveNone.SetGroup(_reserve);
		_btnReserveSnap.SetGroup(_reserve);
		_btnReserveAimed.SetGroup(_reserve);
		_btnReserveAuto.SetGroup(_reserve);

		// Set music
		if (!Options.oxcePlayBriefingMusicDuringEquipment)
		{
			if (string.IsNullOrEmpty(_save.Music))
			{
				_game.Mod.PlayMusic("GMTACTIC");
			}
			else
			{
				_game.Mod.PlayMusic(_save.Music);
			}
		}

		_animTimer = new Timer(DEFAULT_ANIM_SPEED, true);
		_animTimer.OnTimer((StateHandler)Animate);

		_gameTimer = new Timer(DEFAULT_ANIM_SPEED, true);
		_gameTimer.OnTimer((StateHandler)HandleState);

		_battleGame = new BattlescapeGame(_save, this);

		_barHealthColor = _barHealth.Color;
	}

	/// <summary>Click handler, hotkey and tooltip of a regular icon button (the repeated C++ setup block).</summary>
	private void SetUpButton(InteractiveSurface button, ActionHandler click, SDLKey key, string tooltip)
	{
		button.OnMouseClick(click);
		button.OnKeyboardPress(click, key);
		button.Tooltip = tooltip;
		button.OnMouseIn(TxtTooltipIn);
		button.OnMouseOut(TxtTooltipOut);
	}

	/// <summary>
	/// Deletes the battlescapestate (C++ destructor): restores the palettes.
	/// </summary>
	public override void Destroy()
	{
		if (_disposed)
			return;
		_disposed = true;
		ResetPalettes();
	}

	/// <summary>Restores the palettes changed by the environmental effects.</summary>
	public void ResetPalettes()
	{
		if (_paletteResetNeeded)
		{
			foreach (KeyValuePair<string, Palette> origPal in _game.Mod.Palettes)
			{
				if (origPal.Key.StartsWith("PAL_", StringComparison.Ordinal))
				{
					string backupName = "BACKUP_" + origPal.Key;
					Palette backupPal = _game.Mod.GetPalette(backupName, false);
					if (backupPal != null)
					{
						origPal.Value.CopyFrom(backupPal);
					}
				}
			}
			_paletteResetNeeded = false;
		}
	}

	/// <summary>
	/// Initializes the battlescapestate.
	/// </summary>
	public override void Init()
	{
		if (_paletteResetRequested)
		{
			_paletteResetRequested = false;

			ResetPalettes();
			_save.SetPaletteByDepth(this);
			foreach (Surface surface in _surfaces)
			{
				surface.SetPalette(_palette);
			}
		}

		if (_save.AmbientSound != Mod.NO_SOUND)
		{
			_game.Mod.GetSoundByDepth((uint)_save.Depth, (uint)_save.AmbientSound).Loop();
			_game.SetVolume(Options.soundVolume, Options.musicVolume, Options.uiVolume);
		}

		base.Init();
		_animTimer.Start();
		_gameTimer.Start();
		_map.SetFocus(true);
		_map.Draw();
		_battleGame.Init();
		UpdateSoldierInfo();

		switch (_save.TUReserved)
		{
			case BA_SNAPSHOT:
				_reserve.Value = _btnReserveSnap;
				break;
			case BA_AIMEDSHOT:
				_reserve.Value = _btnReserveAimed;
				break;
			case BA_AUTOSHOT:
				_reserve.Value = _btnReserveAuto;
				break;
			default:
				_reserve.Value = _btnReserveNone;
				break;
		}
		if (_firstInit)
		{
			// Set music
			if (Options.oxcePlayBriefingMusicDuringEquipment)
			{
				if (string.IsNullOrEmpty(_save.Music))
				{
					_game.Mod.PlayMusic("GMTACTIC");
				}
				else
				{
					_game.Mod.PlayMusic(_save.Music);
				}
			}

			if (!PlayableUnitSelected())
			{
				SelectNextPlayerUnit();
			}
			if (PlayableUnitSelected())
			{
				_battleGame.SetupCursor();
				_map.Camera.CenterOnPosition(_save.SelectedUnit.Position);
			}
			_firstInit = false;
			_btnReserveNone.SetGroup(_reserve);
			_btnReserveSnap.SetGroup(_reserve);
			_btnReserveAimed.SetGroup(_reserve);
			_btnReserveAuto.SetGroup(_reserve);
		}
		_txtTooltip.SetText("");
		_btnReserveKneel.Toggle(_save.KneelReserved);
		_battleGame.KneelReserved = _save.KneelReserved;
		if (_autosave > 0 && !_save.IsPreview)
		{
			int currentTurn = _autosave;
			_autosave = 0;
			if (_game.SavedGame.IsIronman)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_IRONMAN, _palette));
			}
			else if (Options.autosave)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_AUTO_BATTLESCAPE, _palette, currentTurn));
			}
		}
	}

	/// <summary>
	/// Runs the timers and handles popups.
	/// </summary>
	public override void Think()
	{
		if (_gameTimer.IsRunning)
		{
			if (_popups.Count == 0)
			{
				base.Think();
				int ret = _battleGame.Think();
				if (ret > -1)
				{
					_map.RefreshAIProgress(100 - ret); // progress = 100 - ret;
				}
				_animTimer.Think(this, null);
				_gameTimer.Think(this, null);
				if (s_popped)
				{
					_battleGame.HandleNonTargetAction();
					s_popped = false;
				}
			}
			else
			{
				// Handle popups
				_game.PushState(_popups[0]);
				_popups.RemoveAt(0);
				s_popped = true;
				return;
			}
		}
	}

	/// <summary>True if the drag-scroll mouse button is not held anymore (SDL_GetMouseState).</summary>
	private static bool IsDragScrollButtonReleased()
		=> (_game.MouseButtonState & SDLMouse.SDL_BUTTON(Options.battleDragScrollButton)) == 0;

	/// <summary>Was the drag-scroll too short (in time and distance), so it was a click?</summary>
	private bool IsDragScrollTooShort()
		=> !_mouseMovedOverThreshold && unchecked((int)(SDLTimer.SDL_GetTicks() - _mouseScrollingStartTime)) <= Options.dragScrollTimeTolerance;

	/// <summary>
	/// Processes any mouse moving over the map.
	/// </summary>
	public void MapOver(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_isMouseScrolling && action.Details.type == SDLEventType.SDL_MOUSEMOTION)
		{
			// The following is the workaround for a rare problem where sometimes
			// the mouse-release event is missed for any reason.
			// (checking: is the dragScroll-mouse-button still pressed?)
			// However if the SDL is also missed the release event, then it is to no avail :(
			if (IsDragScrollButtonReleased())
			{ // so we missed again the mouse-release :(
				// Check if we have to revoke the scrolling, because it was too short in time, so it was a click
				if (IsDragScrollTooShort())
				{
					_map.Camera.MapOffset = _mapOffsetBeforeMouseScrolling;
				}
				_isMouseScrolled = _isMouseScrolling = false;
				StopScrolling(action);
				return;
			}

			_isMouseScrolled = true;

			if (Options.touchEnabled == false)
			{
				// Set the mouse cursor back
				SDLEvents.SDL_EventState(SDLEventType.SDL_MOUSEMOTION, SDLEvents.SDL_IGNORE);
				SDLMouse.SDL_WarpMouse((ushort)(_game.Screen.WindowWidth / 2), (ushort)(_game.Screen.WindowHeight / 2 - _map.IconHeight / 2));
				SDLEvents.SDL_EventState(SDLEventType.SDL_MOUSEMOTION, SDLEvents.SDL_ENABLE);
			}

			// Check the threshold
			_totalMouseMoveX += action.Details.motion.xrel;
			_totalMouseMoveY += action.Details.motion.yrel;
			if (!_mouseMovedOverThreshold)
			{
				_mouseMovedOverThreshold = ((Math.Abs(_totalMouseMoveX) > Options.dragScrollPixelTolerance) || (Math.Abs(_totalMouseMoveY) > Options.dragScrollPixelTolerance));
			}

			// Scrolling
			if (Options.battleDragScrollInvert)
			{
				_map.Camera.MapOffset = _mapOffsetBeforeMouseScrolling;
				int scrollX = -(int)((double)_totalMouseMoveX / action.XScale);
				int scrollY = -(int)((double)_totalMouseMoveY / action.YScale);
				Position delta2 = _map.Camera.MapOffset;
				_map.Camera.ScrollXY(scrollX, scrollY, true);
				delta2 = _map.Camera.MapOffset - delta2;

				// Keep the limits...
				if (scrollX != delta2.x || scrollY != delta2.y)
				{
					_totalMouseMoveX = -(int)(delta2.x * action.XScale);
					_totalMouseMoveY = -(int)(delta2.y * action.YScale);
				}

				if (Options.touchEnabled == false)
				{
					action.Details.motion.x = _xBeforeMouseScrolling;
					action.Details.motion.y = _yBeforeMouseScrolling;
				}
				_map.SetCursorType(CursorType.CT_NONE);
			}
			else
			{
				Position delta = _map.Camera.MapOffset;
				_map.Camera.MapOffset = _mapOffsetBeforeMouseScrolling;
				int scrollX = (int)((double)_totalMouseMoveX / action.XScale);
				int scrollY = (int)((double)_totalMouseMoveY / action.YScale);
				Position delta2 = _map.Camera.MapOffset;
				_map.Camera.ScrollXY(scrollX, scrollY, true);
				delta2 = _map.Camera.MapOffset - delta2;
				delta = _map.Camera.MapOffset - delta;

				// Keep the limits...
				if (scrollX != delta2.x || scrollY != delta2.y)
				{
					_totalMouseMoveX = (int)(delta2.x * action.XScale);
					_totalMouseMoveY = (int)(delta2.y * action.YScale);
				}

				int barWidth = _game.Screen.CursorLeftBlackBand;
				int barHeight = _game.Screen.CursorTopBlackBand;
				int cursorX = _cursorPosition.x + Round(delta.x * action.XScale);
				int cursorY = _cursorPosition.y + Round(delta.y * action.YScale);
				_cursorPosition.x = (short)Clamp(cursorX, barWidth, _game.Screen.WindowWidth - barWidth - Round(action.XScale));
				_cursorPosition.y = (short)Clamp(cursorY, barHeight, _game.Screen.WindowHeight - barHeight - Round(action.YScale));

				if (Options.touchEnabled == false)
				{
					action.Details.motion.x = _cursorPosition.x;
					action.Details.motion.y = _cursorPosition.y;
				}
			}

			// We don't want to look the mouse-cursor jumping :)
			_game.Cursor.Handle(action);
		}
	}

	/// <summary>fmath.h Round: rounds half away from zero.</summary>
	private static int Round(double x) => (int)(x < 0.0 ? Math.Ceiling(x - 0.5) : Math.Floor(x + 0.5));

	/// <summary>fmath.h Clamp.</summary>
	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	/// <summary>
	/// Processes any presses on the map.
	/// </summary>
	public void MapPress(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		// don't handle mouseclicks over the buttons (it overlaps with map surface)
		if (_mouseOverIcons) return;

		if (action.Details.button.button == Options.battleDragScrollButton)
		{
			_isMouseScrolling = true;
			_isMouseScrolled = false;
			// SDL_GetMouseState(&x, &y): the pointer's window position, which is that of this event
			_xBeforeMouseScrolling = action.Details.button.x;
			_yBeforeMouseScrolling = action.Details.button.y;
			_mapOffsetBeforeMouseScrolling = _map.Camera.MapOffset;
			if (!Options.battleDragScrollInvert && _cursorPosition.z == 0)
			{
				// C++ reads motion.x/y, which alias button.x/y in the SDL event union
				_cursorPosition.x = (short)action.Details.button.x;
				_cursorPosition.y = (short)action.Details.button.y;
				// the Z is irrelevant to our mouse position, but we can use it as a boolean to check if the position is set or not
				_cursorPosition.z = 1;
			}
			_totalMouseMoveX = 0; _totalMouseMoveY = 0;
			_mouseMovedOverThreshold = false;
			_mouseScrollingStartTime = SDLTimer.SDL_GetTicks();
		}
	}

	/// <summary>
	/// Processes any clicks on the map to
	/// command units.
	/// </summary>
	public void MapClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		// The following is the workaround for a rare problem where sometimes
		// the mouse-release event is missed for any reason.
		// However if the SDL is also missed the release event, then it is to no avail :(
		// (this part handles the release if it is missed and now an other button is used)
		if (_isMouseScrolling)
		{
			if (action.Details.button.button != Options.battleDragScrollButton
				&& IsDragScrollButtonReleased())
			{   // so we missed again the mouse-release :(
				// Check if we have to revoke the scrolling, because it was too short in time, so it was a click
				if (IsDragScrollTooShort())
				{
					_map.Camera.MapOffset = _mapOffsetBeforeMouseScrolling;
				}
				_isMouseScrolled = _isMouseScrolling = false;
				StopScrolling(action);
			}
		}

		// DragScroll-Button release: release mouse-scroll-mode
		if (_isMouseScrolling)
		{
			// While scrolling, other buttons are ineffective
			if (action.Details.button.button == Options.battleDragScrollButton)
			{
				_isMouseScrolling = false;
				StopScrolling(action);
			}
			else
			{
				return;
			}
			// Check if we have to revoke the scrolling, because it was too short in time, so it was a click
			if (IsDragScrollTooShort())
			{
				_isMouseScrolled = false;
				StopScrolling(action);
			}
			if (_isMouseScrolled) return;
		}

		// right-click aborts walking state
		if (_game.IsRightClick(action))
		{
			if (_battleGame.CancelCurrentAction())
			{
				return;
			}
		}

		// don't handle mouseclicks over the buttons (it overlaps with map surface)
		if (_mouseOverIcons) return;

		// don't accept leftclicks if there is no cursor or there is an action busy
		if (_map.CursorType == CursorType.CT_NONE || _battleGame.IsBusy) return;

		Position pos = default;
		_map.GetSelectorPosition(ref pos);

		if (_save.DebugMode)
		{
			Debug($"Clicked {pos}");
		}

		if (_save.GetTile(pos) != null) // don't allow to click into void
		{
			if (_game.IsRightClick(action, true) && PlayableUnitSelected())
			{
				_battleGame.SecondaryAction(pos);
			}
			else if (_game.IsLeftClick(action, true))
			{
				_battleGame.PrimaryAction(pos);
			}
			else if (_game.IsMiddleClick(action, true))
			{
				_battleGame.CancelCurrentAction();
				BattleUnit bu = _save.SelectUnit(pos);
				if (bu != null && (bu.Visible || _save.DebugMode))
				{
					if (_save.DebugMode && _game.IsCtrlPressed())
					{
						// mind probe
						Popup(new UnitInfoState(bu, this, false, true));
					}
					else
					{
						_game.PushState(new AlienInventoryState(bu));
					}
				}
			}
		}
	}

	/// <summary>
	/// Handles mouse entering the map surface.
	/// </summary>
	public void MapIn(Action action)
	{
		_isMouseScrolling = false;
		_map.SetButtonsPressed((byte)Options.battleDragScrollButton, false);
	}
}
