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
// Ported from: src/Geoscape/GeoscapeState.h, src/Geoscape/GeoscapeState.cpp
// Deviation: C++ containers keyed or sorted by pointer (std::map<Production*, ...>, std::map<const RuleBaseFacility*, int>,
// Collections::sortVector + sortVectorMakeUnique on rule pointers) iterate in address order, which is effectively
// unspecified; the port uses first-insertion order (duplicates removed) instead.
#nullable disable

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Geoscape screen which shows an overview of
/// the world and lets the player manage the game.
/// </summary>
public class GeoscapeState : State
{
	private readonly Surface _bg, _sideLine, _sidebar;
	private readonly Globe _globe;
	private readonly TextButton _btnIntercept, _btnBases, _btnGraphs, _btnUfopaedia, _btnOptions, _btnFunding;
	private readonly StrongBox<TextButton> _timeSpeed = new();
	private readonly TextButton _btn5Secs, _btn1Min, _btn5Mins, _btn30Mins, _btn1Hour, _btn1Day;
	private readonly TextButton _sideTop, _sideBottom;
	private readonly InteractiveSurface _btnRotateLeft, _btnRotateRight, _btnRotateUp, _btnRotateDown, _btnZoomIn, _btnZoomOut;
	private readonly Text _txtFunds, _txtHour, _txtHourSep, _txtMin, _txtMinSep, _txtSec, _txtWeekday, _txtDay, _txtMonth, _txtYear;
	private readonly Timer _gameTimer, _zoomInEffectTimer, _zoomOutEffectTimer, _dogfightStartTimer, _dogfightTimer;
	private bool _pause, _zoomInEffectDone, _zoomOutEffectDone;
	private readonly Text _txtDebug;
	private readonly ComboBox _cbxRegion, _cbxZone, _cbxArea, _cbxCountry;
	private readonly Text _txtSlacking;
	private readonly Text _txtTraining;
	private readonly List<State> _popups = [];
	private readonly List<DogfightState> _dogfights = [], _dogfightsToBeStarted = [];
	private readonly List<Craft> _activeCrafts = [];
	private int _minimizedDogfights;
	private int _slowdownCounter;

	private static double Nautical(double x) => x * (1 / 60.0) * (Math.PI / 180.0);

	private static int XcomDistance(double nautical) => (int)(nautical * 60.0 * (180.0 / Math.PI));

	private static int Clamp(int x, int min, int max) => Math.Min(Math.Max(x, min), max);

	/// <summary>
	/// Initializes all the elements in the Geoscape screen.
	/// </summary>
	public GeoscapeState()
	{
		_pause = false;
		_zoomInEffectDone = false;
		_zoomOutEffectDone = false;
		_minimizedDogfights = 0;
		_slowdownCounter = 0;

		int screenWidth = Options.baseXGeoscape;
		int screenHeight = Options.baseYGeoscape;

		// Create objects
		Surface hd = _game.Mod.GetSurface("ALTGEOBORD.SCR");
		_bg = new Surface(hd.Width, hd.Height, 0, 0);
		_sideLine = new Surface(64, screenHeight, screenWidth - 64, 0);
		_sidebar = new Surface(64, 200, screenWidth - 64, screenHeight / 2 - 100);

		_globe = new Globe(_game, (screenWidth - 64) / 2, screenHeight / 2, screenWidth - 64, screenHeight, 0, 0);
		_bg.X = (_globe.Width - _bg.Width) / 2;
		_bg.Y = (_globe.Height - _bg.Height) / 2;

		_btnIntercept = new TextButton(63, 11, screenWidth - 63, screenHeight / 2 - 100);
		_btnBases = new TextButton(63, 11, screenWidth - 63, screenHeight / 2 - 88);
		_btnGraphs = new TextButton(63, 11, screenWidth - 63, screenHeight / 2 - 76);
		_btnUfopaedia = new TextButton(63, 11, screenWidth - 63, screenHeight / 2 - 64);
		_btnOptions = new TextButton(63, 11, screenWidth - 63, screenHeight / 2 - 52);
		_btnFunding = new TextButton(63, 11, screenWidth - 63, screenHeight / 2 - 40);

		_btn5Secs = new TextButton(31, 13, screenWidth - 63, screenHeight / 2 + 12);
		_btn1Min = new TextButton(31, 13, screenWidth - 31, screenHeight / 2 + 12);
		_btn5Mins = new TextButton(31, 13, screenWidth - 63, screenHeight / 2 + 26);
		_btn30Mins = new TextButton(31, 13, screenWidth - 31, screenHeight / 2 + 26);
		_btn1Hour = new TextButton(31, 13, screenWidth - 63, screenHeight / 2 + 40);
		_btn1Day = new TextButton(31, 13, screenWidth - 31, screenHeight / 2 + 40);

		_btnRotateLeft = new InteractiveSurface(12, 10, screenWidth - 61, screenHeight / 2 + 76);
		_btnRotateRight = new InteractiveSurface(12, 10, screenWidth - 37, screenHeight / 2 + 76);
		_btnRotateUp = new InteractiveSurface(13, 12, screenWidth - 49, screenHeight / 2 + 62);
		_btnRotateDown = new InteractiveSurface(13, 12, screenWidth - 49, screenHeight / 2 + 87);
		_btnZoomIn = new InteractiveSurface(23, 23, screenWidth - 25, screenHeight / 2 + 56);
		_btnZoomOut = new InteractiveSurface(13, 17, screenWidth - 20, screenHeight / 2 + 82);

		int height = (screenHeight - Screen.ORIGINAL_HEIGHT) / 2 + 10;
		_sideTop = new TextButton(63, height, screenWidth - 63, _sidebar.Y - height - 1);
		_sideBottom = new TextButton(63, height, screenWidth - 63, _sidebar.Y + _sidebar.Height + 1);

		_txtHour = new Text(20, 16, screenWidth - 61, screenHeight / 2 - 26);
		_txtHourSep = new Text(4, 16, screenWidth - 41, screenHeight / 2 - 26);
		_txtMin = new Text(20, 16, screenWidth - 37, screenHeight / 2 - 26);
		_txtMinSep = new Text(4, 16, screenWidth - 17, screenHeight / 2 - 26);
		_txtSec = new Text(11, 8, screenWidth - 13, screenHeight / 2 - 20);
		_txtWeekday = new Text(59, 8, screenWidth - 61, screenHeight / 2 - 13);
		_txtDay = new Text(29, 8, screenWidth - 61, screenHeight / 2 - 6);
		_txtMonth = new Text(29, 8, screenWidth - 32, screenHeight / 2 - 6);
		_txtYear = new Text(59, 8, screenWidth - 61, screenHeight / 2 + 1);
		_txtFunds = new Text(59, 8, screenWidth - 61, screenHeight / 2 - 27);

		int slackingIndicatorOffset = _game.Mod.GetInterface("geoscape").GetElement("slackingIndicator").custom;
		_txtSlacking = new Text(59, 17, screenWidth - 61, screenHeight / 2 - 100 + slackingIndicatorOffset);
		int trainingIndicatorOffset = _game.Mod.GetInterface("geoscape").GetElement("trainingIndicator").custom;
		_txtTraining = new Text(59, 17, screenWidth - 61, screenHeight / 2 + 100 + trainingIndicatorOffset);

		_timeSpeed.Value = _btn5Secs;
		_gameTimer = new Timer((uint)Options.geoClockSpeed);

		_zoomInEffectTimer = new Timer((uint)Options.dogfightSpeed);
		_zoomOutEffectTimer = new Timer((uint)Options.dogfightSpeed);
		_dogfightStartTimer = new Timer((uint)Options.dogfightSpeed);
		_dogfightTimer = new Timer((uint)Options.dogfightSpeed);

		_txtDebug = new Text(254, 32, 0, 0);
		_cbxRegion = new ComboBox(this, 150, 16, 0, 36);
		_cbxZone = new ComboBox(this, 48, 16, 154, 36);
		_cbxArea = new ComboBox(this, 48, 16, 206, 36);
		_cbxCountry = new ComboBox(this, 150, 16, 0, 36);

		// Set palette
		SetInterface("geoscape");

		Add(_bg);
		Add(_sideLine);
		Add(_sidebar);
		Add(_globe);

		Add(_btnIntercept, "button", "geoscape");
		Add(_btnBases, "button", "geoscape");
		Add(_btnGraphs, "button", "geoscape");
		Add(_btnUfopaedia, "button", "geoscape");
		Add(_btnOptions, "button", "geoscape");
		Add(_btnFunding, "button", "geoscape");

		Add(_btn5Secs, "button", "geoscape");
		Add(_btn1Min, "button", "geoscape");
		Add(_btn5Mins, "button", "geoscape");
		Add(_btn30Mins, "button", "geoscape");
		Add(_btn1Hour, "button", "geoscape");
		Add(_btn1Day, "button", "geoscape");

		Add(_btnRotateLeft);
		Add(_btnRotateRight);
		Add(_btnRotateUp);
		Add(_btnRotateDown);
		Add(_btnZoomIn);
		Add(_btnZoomOut);

		Add(_sideTop, "button", "geoscape");
		Add(_sideBottom, "button", "geoscape");

		Add(_txtFunds, "text", "geoscape");
		Add(_txtHour, "text", "geoscape");
		Add(_txtHourSep, "text", "geoscape");
		Add(_txtMin, "text", "geoscape");
		Add(_txtMinSep, "text", "geoscape");
		Add(_txtSec, "text", "geoscape");
		Add(_txtWeekday, "text", "geoscape");
		Add(_txtDay, "text", "geoscape");
		Add(_txtMonth, "text", "geoscape");
		Add(_txtYear, "text", "geoscape");
		Add(_txtSlacking, "slackingIndicator", "geoscape");
		Add(_txtTraining, "trainingIndicator", "geoscape");

		Add(_txtDebug, "text", "geoscape");
		Add(_cbxRegion, "button", "geoscape");
		Add(_cbxZone, "button", "geoscape");
		Add(_cbxArea, "button", "geoscape");
		Add(_cbxCountry, "button", "geoscape");

		// Set up objects
		Surface geobord = _game.Mod.GetSurface("GEOBORD.SCR");
		geobord.X = _sidebar.X - geobord.Width + _sidebar.Width;
		geobord.Y = _sidebar.Y;
		_sidebar.Copy(geobord);
		_game.Mod.GetSurface("ALTGEOBORD.SCR").BlitNShade(_bg, 0, 0);

		_sideLine.DrawRect(0, 0, (short)_sideLine.Width, (short)_sideLine.Height, 15);

		Font geoBig = _game.Mod.GetFont("FONT_GEO_BIG");
		Font geoSmall = _game.Mod.GetFont("FONT_GEO_SMALL");

		_btnIntercept.InitText(geoBig, geoSmall, _game.Language);
		_btnIntercept.Text = Tr("STR_INTERCEPT");
		_btnIntercept.OnMouseClick(BtnInterceptClick);
		_btnIntercept.OnKeyboardPress(BtnInterceptClick, Options.keyGeoIntercept);
		_btnIntercept.OnKeyboardPress(BtnUfoTrackerClick, Options.keyGeoUfoTracker);
		_btnIntercept.OnKeyboardPress(BtnTechTreeViewerClick, Options.keyGeoTechTreeViewer);
		_btnIntercept.OnKeyboardPress(BtnSelectMusicTrackClick, Options.keySelectMusicTrack);
		_btnIntercept.OnKeyboardPress(BtnGlobalProductionClick, Options.keyGeoGlobalProduction);
		_btnIntercept.OnKeyboardPress(BtnGlobalResearchClick, Options.keyGeoGlobalResearch);
		_btnIntercept.OnKeyboardPress(BtnGlobalAlienContainmentClick, Options.keyGeoGlobalAlienContainment);
		_btnIntercept.OnKeyboardPress(BtnDogfightExperienceClick, Options.keyGeoDailyPilotExperience);
		_btnIntercept.SetGeoscapeButton(true);

		_btnBases.InitText(geoBig, geoSmall, _game.Language);
		_btnBases.Text = Tr("STR_BASES");
		_btnBases.OnMouseClick(BtnBasesClick);
		_btnBases.OnKeyboardPress(BtnBasesClick, Options.keyGeoBases);
		_btnBases.SetGeoscapeButton(true);

		_btnGraphs.InitText(geoBig, geoSmall, _game.Language);
		_btnGraphs.Text = Tr("STR_GRAPHS");
		_btnGraphs.OnMouseClick(BtnGraphsClick);
		_btnGraphs.OnKeyboardPress(BtnGraphsClick, Options.keyGeoGraphs);
		_btnGraphs.SetGeoscapeButton(true);

		_btnUfopaedia.InitText(geoBig, geoSmall, _game.Language);
		_btnUfopaedia.Text = Tr("STR_UFOPAEDIA_UC");
		_btnUfopaedia.OnMouseClick(BtnUfopaediaClick);
		_btnUfopaedia.OnKeyboardPress(BtnUfopaediaClick, Options.keyGeoUfopedia);
		_btnUfopaedia.SetGeoscapeButton(true);

		_btnOptions.InitText(geoBig, geoSmall, _game.Language);
		_btnOptions.Text = Tr("STR_OPTIONS_UC");
		_btnOptions.OnMouseClick(BtnOptionsClick);
		_btnOptions.OnKeyboardPress(BtnOptionsClick, Options.keyGeoOptions);
		_btnOptions.SetGeoscapeButton(true);

		_btnFunding.InitText(geoBig, geoSmall, _game.Language);
		_btnFunding.Text = Options.oxceLinks ? Tr("STR_EXTENDED_UC") : Tr("STR_FUNDING_UC");
		_btnFunding.OnMouseClick(BtnFundingClick);
		_btnFunding.OnKeyboardPress(BtnFundingClick, Options.keyGeoFunding);
		_btnFunding.SetGeoscapeButton(true);

		InitTimeButton(_btn5Secs, "STR_5_SECONDS", Options.keyGeoSpeed1, SDLKey.SDLK_KP1);
		InitTimeButton(_btn1Min, "STR_1_MINUTE", Options.keyGeoSpeed2, SDLKey.SDLK_KP2);
		InitTimeButton(_btn5Mins, "STR_5_MINUTES", Options.keyGeoSpeed3, SDLKey.SDLK_KP3);
		InitTimeButton(_btn30Mins, "STR_30_MINUTES", Options.keyGeoSpeed4, SDLKey.SDLK_KP4);
		InitTimeButton(_btn1Hour, "STR_1_HOUR", Options.keyGeoSpeed5, SDLKey.SDLK_KP5);
		InitTimeButton(_btn1Day, "STR_1_DAY", Options.keyGeoSpeed6, SDLKey.SDLK_KP6);

		_sideBottom.SetGeoscapeButton(true);
		_sideTop.SetGeoscapeButton(true);

		_btnRotateLeft.OnMousePress(BtnRotateLeftPress);
		_btnRotateLeft.OnMouseRelease(BtnRotateLeftRelease);
		_btnRotateLeft.OnKeyboardPress(BtnRotateLeftPress, Options.keyGeoLeft);
		_btnRotateLeft.OnKeyboardRelease(BtnRotateLeftRelease, Options.keyGeoLeft);

		_btnRotateRight.OnMousePress(BtnRotateRightPress);
		_btnRotateRight.OnMouseRelease(BtnRotateRightRelease);
		_btnRotateRight.OnKeyboardPress(BtnRotateRightPress, Options.keyGeoRight);
		_btnRotateRight.OnKeyboardRelease(BtnRotateRightRelease, Options.keyGeoRight);

		_btnRotateUp.OnMousePress(BtnRotateUpPress);
		_btnRotateUp.OnMouseRelease(BtnRotateUpRelease);
		_btnRotateUp.OnKeyboardPress(BtnRotateUpPress, Options.keyGeoUp);
		_btnRotateUp.OnKeyboardRelease(BtnRotateUpRelease, Options.keyGeoUp);

		_btnRotateDown.OnMousePress(BtnRotateDownPress);
		_btnRotateDown.OnMouseRelease(BtnRotateDownRelease);
		_btnRotateDown.OnKeyboardPress(BtnRotateDownPress, Options.keyGeoDown);
		_btnRotateDown.OnKeyboardRelease(BtnRotateDownRelease, Options.keyGeoDown);

		_btnZoomIn.OnMouseClick(BtnZoomInLeftClick, SDLMouse.SDL_BUTTON_LEFT);
		_btnZoomIn.OnMouseClick(BtnZoomInRightClick, SDLMouse.SDL_BUTTON_RIGHT);
		_btnZoomIn.OnKeyboardPress(BtnZoomInLeftClick, Options.keyGeoZoomIn);

		_btnZoomOut.OnMouseClick(BtnZoomOutLeftClick, SDLMouse.SDL_BUTTON_LEFT);
		_btnZoomOut.OnMouseClick(BtnZoomOutRightClick, SDLMouse.SDL_BUTTON_RIGHT);
		_btnZoomOut.OnKeyboardPress(BtnZoomOutLeftClick, Options.keyGeoZoomOut);

		_txtFunds.Align = TextHAlign.ALIGN_CENTER;
		_txtFunds.Visible = Options.showFundsOnGeoscape;

		_txtHour.SetBig();
		_txtHour.Align = TextHAlign.ALIGN_RIGHT;

		_txtHourSep.SetBig();
		_txtHourSep.SetText(":");

		_txtMin.SetBig();

		_txtMinSep.SetBig();
		_txtMinSep.SetText(":");

		_txtWeekday.Align = TextHAlign.ALIGN_CENTER;

		_txtDay.Align = TextHAlign.ALIGN_CENTER;

		_txtMonth.Align = TextHAlign.ALIGN_CENTER;

		_txtYear.Align = TextHAlign.ALIGN_CENTER;

		_txtSlacking.Align = TextHAlign.ALIGN_RIGHT;
		_txtTraining.Align = TextHAlign.ALIGN_RIGHT;

		if (Options.showFundsOnGeoscape)
		{
			_txtHour.Y = _txtHour.Y + 6;
			_txtHour.SetSmall();
			_txtHourSep.Y = _txtHourSep.Y + 6;
			_txtHourSep.SetSmall();
			_txtMin.Y = _txtMin.Y + 6;
			_txtMin.SetSmall();
			_txtMinSep.X = _txtMinSep.X - 10;
			_txtMinSep.Y = _txtMinSep.Y + 6;
			_txtMinSep.SetSmall();
			_txtSec.X = _txtSec.X - 10;
		}

		_gameTimer.OnTimer((StateHandler)TimeAdvance);
		_gameTimer.Start();

		_zoomInEffectTimer.OnTimer((StateHandler)ZoomInEffect);
		_zoomOutEffectTimer.OnTimer((StateHandler)ZoomOutEffect);
		_dogfightStartTimer.OnTimer((StateHandler)StartDogfight);
		_dogfightTimer.OnTimer((StateHandler)HandleDogfights);

		// debug helpers
		{
			var regionList = new List<string> { "All regions" };
			foreach (var r in _game.SavedGame.Regions)
			{
				regionList.Add(r.Rules.Type);
			}
			_cbxRegion.SetOptions(regionList, false);
			_cbxRegion.Visible = false;
			_cbxRegion.OnChange(CbxRegionChange);

			var zoneList = new List<string> { "All zones" };
			for (int z = 0; z < 20; ++z)
			{
				zoneList.Add(z.ToString(CultureInfo.InvariantCulture));
			}
			_cbxZone.SetOptions(zoneList, false);
			_cbxZone.Visible = false;
			_cbxZone.OnChange(CbxZoneChange);

			var areaList = new List<string> { "All areas" };
			for (int z = 0; z < 100; ++z)
			{
				areaList.Add(z.ToString(CultureInfo.InvariantCulture));
			}
			_cbxArea.SetOptions(areaList, false);
			_cbxArea.Visible = false;
			_cbxArea.OnChange(CbxAreaChange);

			var countryList = new List<string> { "All countries" };
			foreach (var c in _game.SavedGame.Countries)
			{
				countryList.Add(Tr(c.Rules.Type));
			}
			_cbxCountry.SetOptions(countryList, false);
			_cbxCountry.Visible = false;
			_cbxCountry.OnChange(CbxCountryChange);
		}

		TimeDisplay();
	}

	/// <summary>Sets up one of the time speed buttons (same steps for each button in C++).</summary>
	private void InitTimeButton(TextButton btn, string text, SDLKey key, SDLKey keypadKey)
	{
		btn.InitText(_game.Mod.GetFont("FONT_GEO_BIG"), _game.Mod.GetFont("FONT_GEO_SMALL"), _game.Language);
		btn.SetBig();
		btn.Text = Tr(text);
		btn.SetGroup(_timeSpeed);
		btn.OnKeyboardPress(BtnTimerClick, key);
		btn.OnKeyboardPress(BtnTimerClick, keypadKey);
		btn.SetGeoscapeButton(true);
	}

	/// <summary>
	/// Handle blitting of Geoscape and Dogfights.
	/// </summary>
	public override void Blit()
	{
		base.Blit();
		foreach (var dfs in _dogfights)
		{
			dfs.Blit();
		}
	}

	/// <summary>
	/// Handle key shortcuts.
	/// </summary>
	public override void Handle(Action action)
	{
		if (_dogfights.Count == _minimizedDogfights)
		{
			base.Handle(action);
		}

		if (action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			SDLKey sym = action.Details.key.keysym.sym;
			if (_dogfights.Count != 0 && _dogfights.Count > _minimizedDogfights)
			{
				if (sym == SDLKey.SDLK_1)
				{
					Options.dogfightSpeed = 50;
					_dogfightTimer.SetInterval((uint)Options.dogfightSpeed);
				}
				else if (sym == SDLKey.SDLK_2)
				{
					Options.dogfightSpeed = 35;
					_dogfightTimer.SetInterval((uint)Options.dogfightSpeed);
				}
				else if (sym == SDLKey.SDLK_3)
				{
					Options.dogfightSpeed = 20;
					_dogfightTimer.SetInterval((uint)Options.dogfightSpeed);
				}
			}
			// "ctrl-d" - enable debug mode
			if (Options.debug && sym == SDLKey.SDLK_d && _game.IsCtrlPressed())
			{
				BtnDebugClick(null);
			}
			if (Options.debug && _game.SavedGame.DebugMode && _game.IsCtrlPressed())
			{
				// "ctrl-1"
				if (sym == SDLKey.SDLK_1)
				{
					_txtDebug.SetText("I'M A BILLIONAIRE! ALMOST...");
					_game.SavedGame.Funds = 999999999;
				}
				// "ctrl-2"
				if (sym == SDLKey.SDLK_2)
				{
					_txtDebug.SetText("ALL FACILITY CONSTRUCTION COMPLETED");
					foreach (var xbase in _game.SavedGame.Bases)
					{
						foreach (var facility in xbase.Facilities)
						{
							facility.BuildTime = 0;
							facility.IfHadPreviousFacility = false;
						}
					}
				}
				// "ctrl-3"
				if (sym == SDLKey.SDLK_3)
				{
					_txtDebug.SetText("+50 SCIENTISTS/ENGINEERS");
					foreach (var xbase in _game.SavedGame.Bases)
					{
						xbase.Scientists = xbase.Scientists + 50;
						xbase.Engineers = xbase.Engineers + 50;
					}
				}
				// "ctrl-4"
				if (sym == SDLKey.SDLK_4)
				{
					_txtDebug.SetText("+2 ALL ITEMS");
					foreach (var xbase in _game.SavedGame.Bases)
					{
						foreach (var itemType in _game.Mod.ItemsList)
						{
							var item = _game.Mod.GetItem(itemType);
							if (item != null && item.IsRecoverable && !item.IsAlien && item.SellCost > 0)
							{
								xbase.StorageItems.AddItem(item, 2);
							}
						}
					}
				}
				// "ctrl-5"
				if (sym == SDLKey.SDLK_5)
				{
					_txtDebug.SetText("+2 ALL LIVE ALIENS");
					foreach (var xbase in _game.SavedGame.Bases)
					{
						foreach (var itemType in _game.Mod.ItemsList)
						{
							var item = _game.Mod.GetItem(itemType);
							if (item != null && item.IsRecoverable && item.IsAlien && item.SellCost > 0)
							{
								xbase.StorageItems.AddItem(item, 2);
							}
						}
					}
				}
				// "ctrl-6"
				if (sym == SDLKey.SDLK_6)
				{
					_txtDebug.SetText("XCOM/ALIEN ACTIVITY FOR THIS MONTH RESET");
					int invertedEntry = _game.SavedGame.FundsList.Count - 1;
					foreach (var region in _game.SavedGame.Regions)
					{
						region.ActivityXcom[invertedEntry] = 0;
						region.ActivityAlien[invertedEntry] = 0;
					}
					foreach (var country in _game.SavedGame.Countries)
					{
						country.ActivityXcom[invertedEntry] = 0;
						country.ActivityAlien[invertedEntry] = 0;
					}
				}
				// "ctrl-7"
				if (sym == SDLKey.SDLK_7)
				{
					_txtDebug.SetText("BIG BROTHER SEES ALL");
					foreach (var ufo in _game.SavedGame.Ufos)
					{
						ufo.Detected = true;
						ufo.HyperDetected = true;
					}
					foreach (var ab in _game.SavedGame.AlienBases)
					{
						ab.SetDiscovered(true);
					}
				}
				// "ctrl-a"
				if (sym == SDLKey.SDLK_a)
				{
					_txtDebug.SetText("SOLDIER DIARIES DELETED");
					foreach (var xbase in _game.SavedGame.Bases)
					{
						foreach (var soldier in xbase.Soldiers)
						{
							soldier.ResetDiary();
						}
					}
				}
				// "ctrl-c"
				if (sym == SDLKey.SDLK_c)
				{
					_txtDebug.SetText("SOLDIER COMMENDATIONS DELETED");
					foreach (var xbase in _game.SavedGame.Bases)
					{
						foreach (var soldier in xbase.Soldiers)
						{
							soldier.Diary.SoldierCommendations.Clear();
						}
					}
				}
			}
			// quick save and quick load
			if (!_game.SavedGame.IsIronman)
			{
				if (sym == Options.keyQuickSave)
				{
					Popup(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_QUICK, _palette));
				}
				else if (sym == Options.keyInstaSave)
				{
					Popup(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_INSTA, _palette));
				}
				else if (sym == Options.keyQuickLoad)
				{
					Popup(new LoadGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_QUICK, _palette));
				}
			}
		}
		if (_dogfights.Count != 0)
		{
			foreach (var dfs in _dogfights)
			{
				dfs.Handle(action);
			}
			_minimizedDogfights = MinimizedDogfightsCount();
		}
	}

	/// <summary>
	/// Updates the timer display and resets the palette
	/// since it's bound to change on other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();
		TimeDisplay();
		UpdateSlackingIndicator();

		_globe.OnMouseClick(GlobeClick);
		_globe.OnMouseOver(null);
		_globe.RotateStop();
		_globe.SetFocus(true);
		_globe.Draw();

		// Pop up save screen if it's a new ironman game
		if (_game.SavedGame.IsIronman && string.IsNullOrEmpty(_game.SavedGame.Name))
		{
			Popup(new ListSaveState(OptionsOrigin.OPT_GEOSCAPE));
		}

		// Set music if it's not already playing
		if (_dogfights.Count == 0 && !_dogfightStartTimer.IsRunning)
		{
			if (_game.SavedGame.MonthsPassed == -1)
			{
				_game.Mod.PlayMusic("GMGEO", 1);
			}
			else
			{
				_game.Mod.PlayMusic("GMGEO");
			}
		}
		else
		{
			_game.Mod.PlayMusic("GMINTER");
		}
		_globe.SetNewBaseHover(false);

		// run once
		if (_game.SavedGame.MonthsPassed == -1 &&
			// as long as there's a base
			_game.SavedGame.Bases.Count != 0 &&
			// and it has a name (THIS prevents it from running prior to the base being placed.)
			!string.IsNullOrEmpty(_game.SavedGame.Bases[0].GetName(_game.Language)))
		{
			_game.SavedGame.AddMonth();
			_game.SavedGame.IncreaseDaysPassed();
			DetermineAlienMissions();
			_game.SavedGame.Funds = _game.SavedGame.Funds - (_game.SavedGame.BaseMaintenance - _game.SavedGame.Bases[0].PersonnelMaintenance);
		}
	}

	/// <summary>
	/// Runs the game timer and handles popups.
	/// </summary>
	public override void Think()
	{
		base.Think();

		_zoomInEffectTimer.Think(this, null);
		_zoomOutEffectTimer.Think(this, null);
		_dogfightStartTimer.Think(this, null);

		if (_popups.Count == 0 && _dogfights.Count == 0 && (!_zoomInEffectTimer.IsRunning || _zoomInEffectDone) && (!_zoomOutEffectTimer.IsRunning || _zoomOutEffectDone))
		{
			// Handle timers
			_gameTimer.Think(this, null);
		}
		else
		{
			if (_dogfights.Count != 0 || _minimizedDogfights != 0)
			{
				// If all dogfights are minimized rotate the globe, etc.
				if (_dogfights.Count == _minimizedDogfights)
				{
					_pause = false;
					_gameTimer.Think(this, null);
				}
				_dogfightTimer.Think(this, null);
			}
			if (_popups.Count != 0)
			{
				// Handle popups
				_globe.RotateStop();
				_game.PushState(_popups[0]);
				_popups.RemoveAt(0);
			}
		}
	}

	/// <summary>
	/// Updates the Geoscape clock with the latest
	/// game time and date in human-readable format. (+Funds)
	/// </summary>
	public void TimeDisplay()
	{
		var save = _game.SavedGame;
		if (Options.showFundsOnGeoscape)
		{
			if (Options.oxceGeoShowScoreInsteadOfFunds)
			{
				// it's a cheat (you're not supposed to see this info in real time), for debugging only
				_txtFunds.SetText(save.GetCurrentScore(save.MonthsPassed + 1).ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				_txtFunds.SetText(Unicode.FormatFunding(save.Funds));
			}
		}

		_txtSec.SetText(save.Time.Second.ToString("D2", CultureInfo.InvariantCulture));
		_txtMin.SetText(save.Time.Minute.ToString("D2", CultureInfo.InvariantCulture));
		_txtHour.SetText(save.Time.Hour.ToString(CultureInfo.InvariantCulture));
		_txtDay.SetText(save.Time.GetDayString(_game.Language));
		_txtWeekday.SetText(Tr(save.Time.WeekdayString));
		_txtMonth.SetText(Tr(save.Time.MonthString));
		_txtYear.SetText(save.Time.Year.ToString(CultureInfo.InvariantCulture));
	}

	/// <summary>
	/// Advances the game timer according to
	/// the timer speed set, and calls the respective
	/// triggers. The timer always advances in "5 secs"
	/// cycles, regardless of the speed, otherwise it might
	/// skip important steps. Instead, it just keeps advancing
	/// the timer until the next speed step (eg. the next day
	/// on 1 Day speed) or until an event occurs, since updating
	/// the screen on each step would become cumbersomely slow.
	/// </summary>
	public void TimeAdvance()
	{
		int timeSpan = 0;
		if (_timeSpeed.Value == _btn5Secs)
		{
			if (Options.oxceGeoSlowdownFactor > 1)
			{
				_slowdownCounter--;
				if (_slowdownCounter > 0)
				{
					// wait
					_globe.Draw();
					return;
				}
				else
				{
					_slowdownCounter = Clamp(Options.oxceGeoSlowdownFactor, 2, 100);
				}
			}
			timeSpan = 1;
		}
		else if (_timeSpeed.Value == _btn1Min)
		{
			timeSpan = 12;
		}
		else if (_timeSpeed.Value == _btn5Mins)
		{
			timeSpan = 12 * 5;
		}
		else if (_timeSpeed.Value == _btn30Mins)
		{
			timeSpan = 12 * 5 * 6;
		}
		else if (_timeSpeed.Value == _btn1Hour)
		{
			timeSpan = 12 * 5 * 6 * 2;
		}
		else if (_timeSpeed.Value == _btn1Day)
		{
			timeSpan = 12 * 5 * 6 * 2 * 24;
		}

		for (int i = 0; i < timeSpan && !_pause; ++i)
		{
			TimeTrigger trigger = _game.SavedGame.Time.Advance();
			// C++ switch with fallthrough: each trigger also runs all the shorter ones
			if (trigger == TimeTrigger.TIME_1MONTH)
			{
				Time1Month();
			}
			if (trigger >= TimeTrigger.TIME_1DAY)
			{
				Time1Day();
			}
			if (trigger >= TimeTrigger.TIME_1HOUR)
			{
				Time1Hour();
			}
			if (trigger >= TimeTrigger.TIME_30MIN)
			{
				Time30Minutes();
			}
			if (trigger >= TimeTrigger.TIME_10MIN)
			{
				Time10Minutes();
			}
			Time5Seconds();
		}

		_pause = _dogfightsToBeStarted.Count != 0 || _zoomInEffectTimer.IsRunning || _zoomOutEffectTimer.IsRunning;

		TimeDisplay();
		_globe.Draw();
	}

	/// <summary>
	/// Update list of active crafts.
	/// </summary>
	/// <returns>Updated list.</returns>
	private List<Craft> UpdateActiveCrafts()
	{
		_activeCrafts.Clear();
		foreach (var xbase in _game.SavedGame.Bases)
		{
			foreach (var xcraft in xbase.Crafts)
			{
				if (xcraft.Status == "STR_OUT" && !xcraft.IsDestroyed)
				{
					_activeCrafts.Add(xcraft);
				}
			}
		}
		return _activeCrafts;
	}

	/// <summary>Collections::deleteAll for the dogfight lists (DogfightState has no destructor work the port needs).</summary>
	private static void DeleteAllDogfights(List<DogfightState> list) => list.Clear();

	/// <summary>
	/// Collections::deleteIf: tests the items in order, calls <paramref name="destroy"/> (the C++ delete) on each
	/// removed item right after its test, and keeps the order of the others.
	/// </summary>
	private static void DeleteIf<T>(List<T> list, Func<T, bool> func, Action<T> destroy = null)
	{
		int newEnd = 0;
		for (int i = 0; i < list.Count; ++i)
		{
			T t = list[i];
			if (func(t))
			{
				destroy?.Invoke(t);
			}
			else
			{
				list[newEnd++] = t;
			}
		}
		list.RemoveRange(newEnd, list.Count - newEnd);
	}

	/// <summary>
	/// Interrupts all other (regular) interceptions before a hunter-killer attack to prevent a dead-lock.
	/// </summary>
	private void InterruptAllDogfights()
	{
		foreach (var f in _dogfights)
		{
			if (f.Craft != null) { f.Craft.SetInDogfight(false); f.Craft.InterceptionOrder = 0; }
		}
		foreach (var g in _dogfightsToBeStarted)
		{
			if (g.Craft != null) { g.Craft.SetInDogfight(false); g.Craft.InterceptionOrder = 0; }
		}
		DeleteAllDogfights(_dogfights);
		DeleteAllDogfights(_dogfightsToBeStarted);
		_minimizedDogfights = 0;
	}

	/// <summary>Counts the hunter-killer dogfights (running and waiting).</summary>
	private int CountHkDogfights()
	{
		int hkDogfights = 0;
		foreach (var f in _dogfights) if (f.IsUfoAttacking) { hkDogfights++; }
		foreach (var g in _dogfightsToBeStarted) if (g.IsUfoAttacking) { hkDogfights++; }
		return hkDogfights;
	}

	/// <summary>
	/// Takes care of any game logic that has to
	/// run every game second, like craft movement.
	/// </summary>
	public void Time5Seconds()
	{
		var save = _game.SavedGame;
		// If in "slow mode", handle UFO hunting and escorting logic every 5 seconds, not only every 10 minutes
		if ((_timeSpeed.Value == _btn5Secs || _timeSpeed.Value == _btn1Min) && _game.Mod.HunterKillerFastRetarget)
		{
			UfoHuntingAndEscorting();
		}

		// Game over if there are no more bases.
		if (save.Bases.Count == 0)
		{
			save.Ending = GameEnding.END_LOSE;
		}
		if (save.Ending == GameEnding.END_LOSE)
		{
			_game.PushState(new CutsceneState(_game.Mod.LoseDefeatCutscene));
			if (save.IsIronman)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_IRONMAN, _palette));
			}
			return;
		}

		var activeCrafts = UpdateActiveCrafts();

		// Handle UFO logic
		bool ufoIsAttacking = false;
		var ufos = save.Ufos;
		for (int ufoIdx = 0; ufoIdx < ufos.Count; ++ufoIdx)
		{
			Ufo ufo = ufos[ufoIdx];
			switch (ufo.Status)
			{
				case Ufo.UfoStatus.FLYING:
					ufo.Think();
					if (ufo.ReachedDestination() && !ufo.IsEscorting)
					{
						Craft c = ufo.Destination as Craft;
						if (c != null && !c.IsDestroyed && ufo.IsHunting)
						{
							// Check if some other HK already attacked before us (at the very same moment)
							// If yes, wait... not more than 1 HK interception allowed at a time.
							if (CountHkDogfights() >= 1)
							{
								continue;
							}

							// If not, interrupt all other (regular) interceptions to prevent a dead-lock (and other possible side effects)
							InterruptAllDogfights();

							// Start the dogfight
							{
								// Main target
								_dogfightsToBeStarted.Add(new DogfightState(this, c, ufo, true));

								// Start fighting escorts and other craft as well (if they are in escort range)
								if (_game.Mod.EscortsJoinFightAgainstHK)
								{
									int secondaryTargets = 0;
									foreach (var craft in activeCrafts)
									{
										if (!craft.IsIgnoredByHK && craft != c)
										{
											// craft is close enough and has at least one loaded weapon
											if (craft.GetNumWeapons(true) > 0 && craft.GetDistance(c) < Nautical(_game.Mod.EscortRange))
											{
												// only up to 4 dogfights = 1 main + 3 secondary
												if (secondaryTargets < 3)
												{
													// Note: push_front() is used so that main target is attacked first
													_dogfightsToBeStarted.Insert(0, new DogfightState(this, craft, ufo, true));
													secondaryTargets++;
												}
											}
										}
									}
								}

								if (!_dogfightStartTimer.IsRunning)
								{
									_pause = true;
									TimerReset();
									_globe.Center(c.Longitude, c.Latitude);
									StartDogfight();
									_dogfightStartTimer.Start();
								}
								_game.Mod.PlayMusic("GMINTER");

								// Don't process certain craft logic (moving and reaching destination)
								ufoIsAttacking = true;
							}
							// Don't handle other logic for this UFO, just continue with the next one
							continue;
						}

						int count = save.MissionSites.Count;
						AlienMission mission = ufo.Mission;
						bool detected = ufo.Detected;
						mission.UfoReachedWaypoint(ufo, _game, _globe);
						if (Options.oxceUfoLandingAlert && ufo.Status == Ufo.UfoStatus.LANDED && ufo.Detected && ufo.LandId != 0)
						{
							string msg = Tr("STR_UFO_HAS_LANDED").Arg(ufo.GetName(_game.Language));
							Popup(new CraftErrorState(this, msg, true, ufo));
						}
						if (detected != ufo.Detected && ufo.Followers.Count != 0)
						{
							if (!(ufo.Trajectory.ID == UfoTrajectory.RETALIATION_ASSAULT_RUN && ufo.Status == Ufo.UfoStatus.LANDED))
								Popup(new UfoLostState(ufo.GetName(_game.Language)));
						}
						if (count < save.MissionSites.Count)
						{
							MissionSite site = save.MissionSites[^1];
							site.Detected = true;
							Popup(new MissionDetectedState(site, this));
						}
						// If UFO was destroyed, don't spawn missions
						if (ufo.Status == Ufo.UfoStatus.DESTROYED)
							return;
						if (ufo.Destination is Base @base)
						{
							mission.WaveCountdown = 30 * (Rng.Generate(0, 400) + 48);
							ufo.Destination = null;
							@base.SetupDefenses(mission);
							TimerReset();
							if (@base.Defenses.Count != 0 && !ufo.Mission.Rules.IgnoreBaseDefenses())
							{
								bool instaHyper = ufo.Rules.IsInstaHyper || mission.Rules.IsInstaHyper;
								Popup(new BaseDefenseState(@base, ufo, this, instaHyper));
								return; // don't allow multiple simultaneous attacks in the same game tick
							}
							else
							{
								HandleBaseDefense(@base, ufo);
								return;
							}
						}
					}
					// Init UFO shields
					if (ufo.Shield == -1)
					{
						ufo.Shield = ufo.CraftStats.shieldCapacity;
					}
					// Recharge UFO shields
					else if (ufo.Shield < ufo.CraftStats.shieldCapacity)
					{
						int shieldRechargeInGeoscape = ufo.CraftStats.shieldRechargeInGeoscape;
						if (shieldRechargeInGeoscape == -1)
						{
							ufo.Shield = ufo.CraftStats.shieldCapacity;
						}
						else if (shieldRechargeInGeoscape > 0)
						{
							int total = shieldRechargeInGeoscape / 100;
							if (Rng.Percent(shieldRechargeInGeoscape % 100))
								total++;
							ufo.Shield = ufo.Shield + total;
						}
					}
					break;
				case Ufo.UfoStatus.LANDED:
					ufo.Think();
					if (ufo.SecondsRemaining == 0)
					{
						AlienMission mission = ufo.Mission;
						bool detected = ufo.Detected;
						mission.UfoLifting(ufo, save);
						if (detected != ufo.Detected && ufo.Followers.Count != 0)
						{
							Popup(new UfoLostState(ufo.GetName(_game.Language)));
						}
					}
					break;
				case Ufo.UfoStatus.CRASHED:
					ufo.Think();
					if (ufo.SecondsRemaining == 0)
					{
						ufo.Detected = false;
						ufo.Status = Ufo.UfoStatus.DESTROYED;
					}
					break;
				case Ufo.UfoStatus.DESTROYED:
				case Ufo.UfoStatus.IGNORE_ME:
					// Nothing to do
					break;
			}
		}

		// Handle craft logic
		foreach (var xbase in save.Bases)
		{
			var crafts = xbase.Crafts;
			for (int craftIt = 0; craftIt < crafts.Count;)
			{
				Craft xcraft = crafts[craftIt];
				if (xcraft.IsDestroyed)
				{
					foreach (var country in save.Countries)
					{
						if (country.Rules.InsideCountry(xcraft.Longitude, xcraft.Latitude))
						{
							country.AddActivityXcom(-xcraft.Rules.Score);
							break;
						}
					}
					foreach (var region in save.Regions)
					{
						if (region.Rules.InsideRegion(xcraft.Longitude, xcraft.Latitude))
						{
							region.AddActivityXcom(-xcraft.Rules.Score);
							break;
						}
					}
					//if (_ufoIsAttacking)
					{
						// Note: this was moved from DogfightState.cpp, as it was not 100% reliable there
						xcraft.EvacuateCrew(_game.Mod);
					}
					// if a transport craft has been shot down, kill all the soldiers on board.
					if (xcraft.Rules.MaxUnitsLimit > 0)
					{
						var soldiers = xbase.Soldiers;
						for (int soldierIt = 0; soldierIt < soldiers.Count;)
						{
							Soldier soldier = soldiers[soldierIt];
							if (soldier.Craft == xcraft)
							{
								soldierIt = save.KillSoldier(true, soldier);
							}
							else
							{
								++soldierIt;
							}
						}
					}
					save.StopHuntingXcomCraft(xcraft); // craft destroyed in dogfight
					Craft craft = crafts[craftIt];
					craftIt = xbase.RemoveCraft(craft, false);
					craft.Destroy();
					save.IncreaseCraftLostDogfight();
					continue;
				}
				if (xcraft.Destination != null)
				{
					if (xcraft.Destination is Ufo u)
					{
						if (!u.Detected)
						{
							if (u.Trajectory.ID == UfoTrajectory.RETALIATION_ASSAULT_RUN && (u.Status == Ufo.UfoStatus.LANDED || u.Status == Ufo.UfoStatus.DESTROYED))
							{
								xcraft.ReturnToBase();
							}
							else
							{
								var w = new Waypoint();
								w.Longitude = u.Longitude;
								w.Latitude = u.Latitude;
								w.Id = u.Id;
								xcraft.Destination = null;
								Popup(new GeoscapeCraftState(xcraft, _globe, w, false));
							}
						}
						if (u.Status == Ufo.UfoStatus.LANDED && xcraft.IsInDogfight)
						{
							xcraft.SetInDogfight(false);
						}
						else if (u.Status == Ufo.UfoStatus.DESTROYED || u.Status == Ufo.UfoStatus.IGNORE_ME)
						{
							xcraft.ReturnToBase();
						}
					}
					else
					{
						if (xcraft.IsInDogfight)
						{
							xcraft.SetInDogfight(false);
						}
					}
				}

				if (!ufoIsAttacking)
				{
					bool returnedToBase = xcraft.Think();
					if (returnedToBase)
					{
						save.StopHuntingXcomCraft(xcraft); // hiding in the base is good enough, obviously
					}
				}

				// Handle craft shield recharge
				if (!ufoIsAttacking && xcraft.Shield < xcraft.CraftStats.shieldCapacity)
				{
					int shieldRechargeInGeoscape = xcraft.CraftStats.shieldRechargeInGeoscape;
					if (shieldRechargeInGeoscape == -1)
					{
						xcraft.Shield = xcraft.CraftStats.shieldCapacity;
					}
					else if (shieldRechargeInGeoscape > 0)
					{
						int total = xcraft.CraftStats.shieldRechargeInGeoscape / 100;
						if (Rng.Percent(xcraft.CraftStats.shieldRechargeInGeoscape % 100))
							total++;
						xcraft.Shield = xcraft.Shield + total;
					}
				}

				if (!ufoIsAttacking && xcraft.ReachedDestination())
				{
					bool suppressLanding = Options.oxceGeoSuppressLandingWithoutEquipment && xcraft.Items.Empty();
					Target dest = xcraft.Destination;
					Ufo u = dest as Ufo;
					Waypoint w = dest as Waypoint;
					MissionSite m = dest as MissionSite;
					AlienBase b = dest as AlienBase;
					Craft x = dest as Craft;
					if (u != null)
					{
						switch (u.Status)
						{
							case Ufo.UfoStatus.FLYING:
								// Not more than 4 interceptions at a time... but hunter-killers are always allowed
								if (!u.IsHunterKiller && _dogfights.Count + _dogfightsToBeStarted.Count >= 4)
								{
									++craftIt;
									continue;
								}
								// Can we actually fight it
								if (!xcraft.IsInDogfight && u.Speed <= xcraft.CraftStats.speedMax)
								{
									if (u.IsHunterKiller)
									{
										// Check if some other HK already attacked before us (at the very same moment)
										// If yes, wait... not more than 1 HK interception allowed at a time.
										if (CountHkDogfights() >= 1)
										{
											++craftIt;
											continue;
										}

										// If not, interrupt all other (regular) interceptions to prevent a dead-lock (and other possible side effects)
										InterruptAllDogfights();

										// Don't process certain craft logic (moving and reaching destination)
										ufoIsAttacking = true;
									}

									// Main target
									var dogfight = new DogfightState(this, xcraft, u, u.IsHunterKiller);
									_dogfightsToBeStarted.Add(dogfight);

									if (u.IsHunterKiller && _game.Mod.EscortsJoinFightAgainstHK)
									{
										// Start fighting escorts and other craft as well (if they are in escort range)
										int secondaryTargets = 0;
										foreach (var craft in activeCrafts)
										{
											if (!craft.IsIgnoredByHK && craft != xcraft)
											{
												// craft is close enough and has at least one loaded weapon
												if (craft.GetNumWeapons(true) > 0 && craft.GetDistance(xcraft) < Nautical(_game.Mod.EscortRange))
												{
													// only up to 4 dogfights = 1 main + 3 secondary
													if (secondaryTargets < 3)
													{
														// Note: push_front() is used so that main target is attacked first
														_dogfightsToBeStarted.Insert(0, new DogfightState(this, craft, u, u.IsHunterKiller));
														secondaryTargets++;
													}
												}
											}
										}
									}

									// Ignore these restrictions when fighting against a HK, otherwise it's very easy to avoid being attacked
									if (!u.IsHunterKiller)
									{
										if (xcraft.Rules.IsWaterOnly && u.AltitudeInt > xcraft.Rules.MaxAltitude)
										{
											Popup(new DogfightErrorState(xcraft, Tr("STR_UNABLE_TO_ENGAGE_DEPTH")));
											dogfight.SetMinimized(true);
											dogfight.WaitForAltitude = true;
										}
										else if (xcraft.Rules.IsWaterOnly && !_globe.InsideLand(xcraft.Longitude, xcraft.Latitude))
										{
											Popup(new DogfightErrorState(xcraft, Tr("STR_UNABLE_TO_ENGAGE_AIRBORNE")));
											dogfight.SetMinimized(true);
											dogfight.WaitForPoly = true;
										}
									}

									if (!_dogfightStartTimer.IsRunning)
									{
										_pause = true;
										TimerReset();
										_globe.Center(xcraft.Longitude, xcraft.Latitude);
										StartDogfight();
										_dogfightStartTimer.Start();
									}
									_game.Mod.PlayMusic("GMINTER");
								}
								break;
							case Ufo.UfoStatus.LANDED:
							case Ufo.UfoStatus.CRASHED:
							case Ufo.UfoStatus.DESTROYED: // Just before expiration
								if (xcraft.NumTotalUnits > 0 && xcraft.Rules.AllowLanding && !suppressLanding)
								{
									if (!xcraft.IsInDogfight)
									{
										// look up polygons texture
										int texture = 0, shade = 0;
										_globe.GetPolygonTextureAndShade(u.Longitude, u.Latitude, ref texture, ref shade);
										TimerReset();
										Texture globeTexture = _game.Mod.Globe.GetTexture(texture);
										Popup(new ConfirmLandingState(xcraft, globeTexture, globeTexture, shade));
									}
								}
								else if (u.Status != Ufo.UfoStatus.LANDED)
								{
									xcraft.ReturnToBase();
								}
								break;
							case Ufo.UfoStatus.IGNORE_ME:
								break;
						}
					}
					else if (w != null)
					{
						if (!xcraft.IsAutoPatrolling)
						{
							Popup(new CraftPatrolState(xcraft, _globe));
						}
						xcraft.Destination = null;
					}
					else if (m != null)
					{
						if (xcraft.NumTotalUnits > 0 && xcraft.Rules.AllowLanding && !suppressLanding)
						{
							// look up polygons texture
							int texture = 0, shade = 0;
							_globe.GetPolygonTextureAndShade(m.Longitude, m.Latitude, ref texture, ref shade);
							TimerReset();
							Texture globeTexture = _game.Mod.Globe.GetTexture(texture);
							if (globeTexture.IsCosmeticOcean)
							{
								// Unlike xcom bases, alien bases and ufo crash/land sites, mission sites can actually happen on ocean (=non-polygon part of the globe)
								// e.g. TFTD surface attacks
								// For backwards-compatibility, let's use texture = -1 instead of any cosmetic ocean texture
								globeTexture = _game.Mod.Globe.GetTexture(-1);
							}
							Texture missionTexture = _game.Mod.Globe.GetTexture(m.Texture);
							if (missionTexture == null)
							{
								missionTexture = globeTexture;
							}
							Popup(new ConfirmLandingState(xcraft, missionTexture, globeTexture, shade));
						}
						else
						{
							xcraft.ReturnToBase();
						}
					}
					else if (b != null)
					{
						if (b.IsDiscovered)
						{
							if (xcraft.NumTotalUnits > 0 && xcraft.Rules.AllowLanding && !suppressLanding)
							{
								int texture = 0, shade = 0;
								_globe.GetPolygonTextureAndShade(b.Longitude, b.Latitude, ref texture, ref shade);
								TimerReset();
								Texture globeTexture = _game.Mod.Globe.GetTexture(texture);
								Popup(new ConfirmLandingState(xcraft, globeTexture, globeTexture, shade));
							}
							else
							{
								xcraft.ReturnToBase();
							}
						}
					}
					else if (x != null)
					{
						if (x.Status != "STR_OUT" || x.IsDestroyed)
						{
							xcraft.ReturnToBase();
						}
					}
				}
				++craftIt;
			}
		}

		// Clean up dead UFOs and end dogfights which were minimized.
		DeleteIf(save.Ufos,
			ufo =>
			{
				if (ufo.Status == Ufo.UfoStatus.DESTROYED)
				{
					DeleteIf(_dogfights, dogfight => dogfight.Ufo == ufo);
					return true;
				}
				else
				{
					return false;
				}
			},
			ufo => ufo.Destroy());

		// Check any dogfights waiting to open
		foreach (var dfs in _dogfights)
		{
			if (dfs.IsMinimized)
			{
				if ((dfs.WaitForPoly && _globe.InsideLand(dfs.Ufo.Longitude, dfs.Ufo.Latitude)) ||
					(dfs.WaitForAltitude && dfs.Ufo.AltitudeInt <= dfs.Craft.Rules.MaxAltitude))
				{
					_pause = true; // the USO reached the sea during this interval period, stop the timer and let handleDogfights() take it from there.
					TimerReset();
				}
			}
		}

		// Clean up unused waypoints
		DeleteIf(save.Waypoints, way => way.Followers.Count == 0, way => way.Destroy());
	}

	/// <summary>
	/// Functor that attempt to detect an XCOM base (C++ class DetectXCOMBase).
	/// Only UFOs within detection range of the base have a chance to detect it.
	/// </summary>
	/// <param name="xbase">The target base.</param>
	/// <param name="ufo">The UFO attempting detection.</param>
	/// <returns>If the base is detected by <paramref name="ufo"/>.</returns>
	private static bool DetectXcomBase(Base xbase, Ufo ufo)
	{
		if (ufo.TrajectoryPoint <= 1) return false;
		if (ufo.Trajectory.GetZone(ufo.TrajectoryPoint) == 5) return false;
		if ((ufo.Mission.Rules.Objective != MissionObjective.OBJECTIVE_RETALIATION && !Options.aggressiveRetaliation) ||	// only UFOs on retaliation missions actively scan for bases
			ufo.Trajectory.ID == UfoTrajectory.RETALIATION_ASSAULT_RUN || 										// UFOs attacking a base don't detect!
			ufo.IsCrashed ||																								// Crashed UFOs don't detect!
			ufo.Status == Ufo.UfoStatus.IGNORE_ME ||
			xbase.GetDistance(ufo) >= Nautical(ufo.CraftStats.sightRange))											// UFOs have a detection range of 80 XCOM units. - we use a great circle formula and nautical miles.
		{
			return false;
		}
		return Rng.Percent(xbase.DetectionChance);
	}

	/// <summary>
	/// Takes care of any game logic that has to
	/// run every game ten minutes, like fuel consumption.
	/// </summary>
	public void Time10Minutes()
	{
		var save = _game.SavedGame;
		foreach (var xbase in save.Bases)
		{
			// Fuel consumption for XCOM craft.
			foreach (var xcraft in xbase.Crafts)
			{
				if (xcraft.Status == "STR_OUT")
				{
					int escortSpeed = 0;
					{
						if (xcraft.Destination is Craft escortee)
						{
							if (xcraft.GetDistance(escortee) < Nautical(_game.Mod.EscortRange))
							{
								escortSpeed = escortee.Speed;
							}
						}
					}
					xcraft.ConsumeFuel(escortSpeed);
					if (!xcraft.LowFuel && xcraft.Fuel <= xcraft.FuelLimit)
					{
						xcraft.LowFuel = true;
						xcraft.ReturnToBase();
						if (!xcraft.IsAutoPatrolling)
						{
							Popup(new LowFuelState(xcraft, this));
						}
					}

					if (xcraft.Destination == null && xcraft.CraftStats.sightRange > 0)
					{
						double range = Nautical(xcraft.CraftStats.sightRange);
						foreach (var ab in save.AlienBases)
						{
							if (xcraft.GetDistance(ab) <= range)
							{
								if (Rng.Percent((int)(50 - (xcraft.GetDistance(ab) / range) * 50)) && !ab.IsDiscovered)
								{
									ab.SetDiscovered(true);
								}
							}
						}
					}
				}
			}
		}
		if (Options.aggressiveRetaliation)
		{
			// Detect as many bases as possible.
			foreach (var xbase in save.Bases)
			{
				// Find a UFO that detected this base, if any.
				if (FindDetectingUfo(save.Ufos, xbase))
				{
					// Base found
					xbase.SetRetaliationTarget(true);
				}
			}
		}
		else
		{
			// Only remember last base in each region.
			// (C++ std::map<const Region*, Base*>: the order does not matter, a null region is a valid key)
			var discovered = new Dictionary<Region, Base>();
			Base discoveredWithoutRegion = null;
			foreach (var xbase in save.Bases)
			{
				// Find a UFO that detected this base, if any.
				if (FindDetectingUfo(save.Ufos, xbase))
				{
					Region region = save.LocateRegion(xbase);
					if (region == null)
						discoveredWithoutRegion = xbase;
					else
						discovered[region] = xbase;
				}
			}
			// Now mark the bases as discovered.
			if (discoveredWithoutRegion != null)
			{
				discoveredWithoutRegion.SetRetaliationTarget(true);
			}
			foreach (var pair in discovered)
			{
				pair.Value.SetRetaliationTarget(true);
			}
		}

		// Handle alien bases detecting xcom craft and generating hunt missions
		BaseHunting();

		// Handle UFO re-targeting (i.e. hunting and escorting) logic
		UfoHuntingAndEscorting();
	}

	/// <summary>std::find_if with DetectXCOMBase: stops at the first UFO that detects the base.</summary>
	private static bool FindDetectingUfo(List<Ufo> ufos, Base xbase)
	{
		foreach (var ufo in ufos)
		{
			if (DetectXcomBase(xbase, ufo))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Handles UFO re-targeting (hunting and escorting).
	/// </summary>
	public void UfoHuntingAndEscorting()
	{
		var activeCrafts = UpdateActiveCrafts();

		foreach (var ufo in _game.SavedGame.Ufos)
		{
			if (ufo.IsHunterKiller && ufo.Status == Ufo.UfoStatus.FLYING)
			{
				// current target and attraction
				int newAttraction = int.MaxValue;
				Craft newTarget = null;
				Craft originalTarget = null;
				if (ufo.IsHunting)
				{
					originalTarget = ufo.TargetedXcomCraft;
				}
				if (originalTarget != null && !originalTarget.IsIgnoredByHK)
				{
					if (ufo.InsideRadarRange(originalTarget))
					{
						newTarget = originalTarget;
						newAttraction = newTarget.GetHunterKillerAttraction(ufo.HuntMode);
					}
				}

				// look for more attractive target
				foreach (var craft in activeCrafts)
				{
					if (!craft.IsIgnoredByHK && !craft.Rules.IsUndetectable)
					{
						int tmpAttraction = craft.GetHunterKillerAttraction(ufo.HuntMode);
						if (tmpAttraction < newAttraction && ufo.InsideRadarRange(craft))
						{
							newTarget = craft;
							newAttraction = tmpAttraction;
						}
					}
				}

				if (newTarget != null)
				{
					if (newTarget != originalTarget)
					{
						// set new target
						ufo.TargetedXcomCraft = newTarget;
						// TODO: rethink: always reveal the hunting UFO (even outside of radar range?)
						ufo.Detected = true;
						if (ufo.Id == 0)
						{
							ufo.Id = _game.SavedGame.GetId("STR_UFO");
						}
						// inform the player
						if (ufo.Rules.HuntAlertSound != Mod.NO_SOUND)
						{
							_game.Mod.GetSound("GEO.CAT", ufo.Rules.HuntAlertSound).Play();
						}
						if (Options.oxceGeoSuppressRedundantHKAlert && newTarget.Destination == ufo)
						{
							// Empty by design
							// If the player sent an interceptor after the HK, the alert is redundant
						}
						else
						{
							string msg = Tr("STR_UFO_STARTED_HUNTING")
								.Arg(ufo.GetName(_game.Language))
								.Arg(newTarget.GetName(_game.Language));
							Popup(new CraftErrorState(this, msg));
						}
					}
				}
				else if (originalTarget != null)
				{
					// stop hunting
					ufo.ResetOriginalDestination(originalTarget);
				}

				// If we are not preoccupied by hunting, let's see if there is still anyone left to escort
				if (ufo.IsEscort && !ufo.IsHunting && !ufo.IsEscorting)
				{
					// Find a UFO to escort
					foreach (var ufoToBeEscorted in _game.SavedGame.Ufos)
					{
						// From the same mission
						if (ufoToBeEscorted.Mission.Id == ufo.Mission.Id)
						{
							// But not another hunter-killer, we escort only normal UFOs
							if (!ufoToBeEscorted.IsHunterKiller)
							{
								ufo.SetEscortedUfo(ufoToBeEscorted);
								break;
							}
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Handles alien bases detecting xcom craft and generating hunt missions.
	/// </summary>
	public void BaseHunting()
	{
		var activeCrafts = UpdateActiveCrafts();
		var save = _game.SavedGame;

		// index loop: a started mission may add alien bases (C++ would invalidate its iterator)
		var alienBases = save.AlienBases;
		for (int abIdx = 0; abIdx < alienBases.Count; ++abIdx)
		{
			AlienBase ab = alienBases[abIdx];
			if (ab.Deployment.BaseDetectionRange > 0)
			{
				// Increase counter by 10 minutes
				ab.MinutesSinceLastHuntMissionGeneration = ab.MinutesSinceLastHuntMissionGeneration + 10;

				// Check counter
				if (ab.MinutesSinceLastHuntMissionGeneration >= ab.Deployment.HuntMissionMaxFrequency)
				{
					// Look for nearby craft
					bool started = false;
					foreach (var craft in activeCrafts)
					{
						// Craft is flying (i.e. not in base)
						if (craft.Status == "STR_OUT" && !craft.IsDestroyed && !craft.Rules.IsUndetectable && !craft.IsIgnoredByHK)
						{
							// Craft is close enough and RNG is in our favour
							if (craft.GetDistance(ab) < Nautical(ab.Deployment.BaseDetectionRange) && Rng.Percent(ab.Deployment.BaseDetectionChance))
							{
								// Generate a hunt mission
								string huntMission = ab.Deployment.GenerateHuntMission(save.MonthsPassed);
								if (_game.Mod.GetAlienMission(huntMission) != null)
								{
									// Spawn hunt mission for this base.
									RuleAlienMission rule = _game.Mod.GetAlienMission(huntMission);
									var mission = new AlienMission(rule);
									mission.SetRegion(save.LocateRegion(ab).Rules.Type, _game.Mod);
									mission.Id = save.GetId("ALIEN_MISSIONS");
									if (!ab.Deployment.IsHuntMissionRaceFromAlienBase && rule.HasRaceWeights)
									{
										mission.Race = rule.GenerateRace(save.MonthsPassed);
									}
									else
									{
										mission.Race = ab.AlienRace;
									}
									mission.AlienBase = ab;
									int targetArea = -1;
									if (mission.Rules.Objective == MissionObjective.OBJECTIVE_SITE)
									{
										int missionZone = mission.Rules.SpawnZone;
										RuleRegion regionRules = _game.Mod.GetRegion(mission.Region);
										List<MissionArea> areas = regionRules.MissionZones[missionZone].areas;
										if (areas.Count != 0)
										{
											targetArea = Rng.Generate(0, areas.Count - 1);
										}
									}
									mission.SetMissionSiteZoneArea(targetArea);
									mission.Start(_game, _globe);
									save.AlienMissions.Add(mission);

									// Start immediately
									mission.Think(_game, _globe);

									// Reset counter
									ab.MinutesSinceLastHuntMissionGeneration = 0;
									started = true;
									break;
								}
								else if (huntMission != "")
								{
									throw new InvalidOperationException("Alien Base tried to generate undefined hunt mission: " + huntMission);
								}
							}
						}
						if (started) break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Process a MissionSite.
	/// This function object will count down towards expiring a MissionSite, and handle expired MissionSites.
	/// </summary>
	/// <param name="site">Pointer to mission site.</param>
	/// <returns>Has mission site expired?</returns>
	public bool ProcessMissionSite(MissionSite site)
	{
		var save = _game.SavedGame;
		bool removeSite = site.SecondsRemaining < 30 * 60;
		if (!removeSite)
		{
			site.SecondsRemaining = site.SecondsRemaining - 30 * 60;
		}
		else
		{
			bool noFollowers = site.Followers.Count == 0;
			if (site.Rules.DespawnEvenIfTargeted())
			{
				foreach (var follower in site.CraftFollowers)
				{
					follower.ReturnToBase();
				}
				if (!noFollowers)
				{
					Popup(new UfoLostState(site.GetName(_game.Language)));
				}
			}
			else
			{
				removeSite = noFollowers; // CHEEKY EXPLOIT
			}
		}
		if (removeSite)
		{
			// Unlock research defined in alien deployment, if the mission site despawned
			RuleResearch research = _game.Mod.GetResearch(site.Deployment.UnlockedResearchOnDespawn);
			save.HandleResearchUnlockedByMissions(research, _game.Mod, site.Deployment);

			// Increase counters
			save.IncreaseCustomCounter(site.Deployment.CounterDespawn);
			save.IncreaseCustomCounter(site.Deployment.CounterFailure); // despawn is also a type of failure
			save.IncreaseCustomCounter(site.Deployment.CounterAll);
			// Decrease counters
			save.DecreaseCustomCounter(site.Deployment.DecreaseCounterDespawn);
			save.DecreaseCustomCounter(site.Deployment.DecreaseCounterFailure); // despawn is also a type of failure
			save.DecreaseCustomCounter(site.Deployment.DecreaseCounterAll);

			// Generate a despawn event
			var eventRules = _game.Mod.GetEvent(site.Deployment.ChooseDespawnEvent());
			bool canSpawn = save.CanSpawnInstantEvent(eventRules);
			if (canSpawn)
			{
				TimerReset();
				Popup(new GeoscapeEventState(eventRules));
			}
		}

		int score = removeSite ? site.Deployment.DespawnPenalty : site.Deployment.Points;

		Region region = save.LocateRegion(site);
		if (region != null)
		{
			region.AddActivityAlien(score);
		}
		foreach (var country in save.Countries)
		{
			if (country.Rules.InsideCountry(site.Longitude, site.Latitude))
			{
				country.AddActivityAlien(score);
				break;
			}
		}

		Ufo ufo = site.Ufo;
		if (removeSite && ufo != null)
		{
			// "reactivate" the corresponding Ufo
			site.Ufo = null;
			ufo.Mission.UfoLifting(ufo, save);
		}

		return removeSite;
	}

	/// <summary>
	/// Takes care of any game logic that has to
	/// run every game half hour, like UFO detection.
	/// </summary>
	public void Time30Minutes()
	{
		var save = _game.SavedGame;
		// Decrease mission countdowns
		var missions = save.AlienMissions;
		for (int amIdx = 0; amIdx < missions.Count; ++amIdx)
		{
			AlienMission am = missions[amIdx];
			int abCount = save.AlienBases.Count;

			am.Think(_game, _globe);

			if (abCount < save.AlienBases.Count)
			{
				AlienBase newAlienBase = save.AlienBases[^1];
				if (!newAlienBase.IsDiscovered && am.Rules.ShowAlienBase())
				{
					newAlienBase.SetDiscovered(true);
					Popup(new AlienBaseState(newAlienBase, this));
				}
			}

			if (am.Rules.Objective == MissionObjective.OBJECTIVE_RETALIATION && am.IsOver)
			{
				foreach (var xcomBase in save.Bases)
				{
					if (xcomBase.RetaliationMission == am)
					{
						xcomBase.RetaliationMission = null;
					}
				}
			}
		}

		// Remove finished missions
		DeleteIf(save.AlienMissions, am => am.IsOver);

		// Handle crashed UFOs expiration
		foreach (var ufo in save.Ufos)
		{
			if (ufo.Status == Ufo.UfoStatus.CRASHED)
			{
				if (ufo.SecondsRemaining >= 30 * 60)
				{
					ufo.SecondsRemaining = ufo.SecondsRemaining - 30 * 60;
					continue;
				}
				// Marked expired UFOs for removal.
				ufo.Status = Ufo.UfoStatus.DESTROYED;
			}
		}

		// Handle craft maintenance and alien base detection
		foreach (var xbase in save.Bases)
		{
			foreach (var xcraft in xbase.Crafts)
			{
				if (xcraft.Status == "STR_REFUELLING")
				{
					string item = xcraft.Refuel();

					if (string.IsNullOrEmpty(item))
					{
						// notification
						if (xcraft.Status == "STR_READY" && xcraft.Rules.NotifyWhenRefueled())
						{
							string msg = Tr("STR_CRAFT_IS_READY").Arg(xcraft.GetName(_game.Language)).Arg(xbase.GetName(_game.Language));
							Popup(new CraftErrorState(this, msg));
						}
						// auto-patrol
						if (xcraft.Status == "STR_READY" && xcraft.Rules.CanAutoPatrol())
						{
							if (xcraft.IsAutoPatrolling)
							{
								var w = new Waypoint();
								w.Longitude = xcraft.LongitudeAuto;
								w.Latitude = xcraft.LatitudeAuto;
								if (w != null && w.Id == 0)
								{
									w.Id = save.GetId("STR_WAY_POINT");
									save.Waypoints.Add(w);
								}
								xcraft.Destination = w;
								xcraft.Status = "STR_OUT";
							}
						}
					}
					else
					{
						string msg = Tr("STR_NOT_ENOUGH_ITEM_TO_REFUEL_CRAFT_AT_BASE")
											.Arg(Tr(item))
											.Arg(xcraft.GetName(_game.Language))
											.Arg(xbase.GetName(_game.Language));
						Popup(new CraftErrorState(this, msg));
					}
				}
			}
		}

		// can be updated by previous loop
		var activeCrafts = UpdateActiveCrafts();

		// Handle UFO detection and give aliens points
		foreach (var ufo in save.Ufos)
		{
			// instant retaliation missions are ignored (UFOs shouldn't be detected)
			if (ufo.Mission.Rules.Objective == MissionObjective.OBJECTIVE_INSTANT_RETALIATION)
			{
				continue;
			}

			int points = ufo.Rules.MissionScore; //one point per UFO in-flight per half hour
			switch (ufo.Status)
			{
				case Ufo.UfoStatus.LANDED:
				case Ufo.UfoStatus.FLYING:
					if (ufo.Status == Ufo.UfoStatus.LANDED)
					{
						points *= 2;
					}
					// Get area
					foreach (var region in save.Regions)
					{
						if (region.Rules.InsideRegion(ufo.Longitude, ufo.Latitude))
						{
							region.AddActivityAlien(points);
							break;
						}
					}
					// Get country
					foreach (var country in save.Countries)
					{
						if (country.Rules.InsideCountry(ufo.Longitude, ufo.Latitude))
						{
							country.AddActivityAlien(points);
							break;
						}
					}

					// Detection ufo state
					UfoDetection(ufo, activeCrafts);

					break;
				case Ufo.UfoStatus.CRASHED:
				case Ufo.UfoStatus.DESTROYED:
				case Ufo.UfoStatus.IGNORE_ME:
					break;
			}
		}

		// Processes MissionSites
		DeleteIf(save.MissionSites, ProcessMissionSite, site => site.Destroy());

		// Decrease event countdowns and pop up if needed
		foreach (var ge in save.GeoscapeEvents)
		{
			ge.Think();

			if (ge.IsOver)
			{
				bool interrupted = false;
				if (!string.IsNullOrEmpty(ge.Rules.InterruptResearch))
				{
					if (save.IsResearched(ge.Rules.InterruptResearch, false))
					{
						interrupted = true;
					}
				}
				if (!interrupted)
				{
					TimerReset();
					Popup(new GeoscapeEventState(ge.Rules));
				}
			}
		}

		// Remove finished events
		DeleteIf(save.GeoscapeEvents, ge => ge.IsOver);
	}

	/// <summary>
	/// Logic responsible for detecting ufo and its tracking.
	/// </summary>
	public void UfoDetection(Ufo ufo, List<Craft> activeCrafts)
	{
		static bool maskTest(UfoDetection value, UfoDetection mask) => (value & mask) == mask;
		static UfoDetection maskBitOr(UfoDetection value, UfoDetection mask) => value | mask;

		var detected = Acom.UfoDetection.DETECTION_NONE;
		var alreadyTracked = ufo.Detected;
		var save = _game.SavedGame;

		foreach (var xbase in save.Bases)
		{
			detected = maskBitOr(detected, xbase.Detect(ufo, save, alreadyTracked));
		}

		foreach (var craft in activeCrafts)
		{
			detected = maskBitOr(detected, craft.Detect(ufo, save, alreadyTracked));
		}

		if (!alreadyTracked)
		{
			if (maskTest(detected, Acom.UfoDetection.DETECTION_RADAR))
			{
				if (maskTest(detected, Acom.UfoDetection.DETECTION_HYPERWAVE))
				{
					ufo.HyperDetected = true;
				}
				ufo.Detected = true;
				// don't show if player said he doesn't want to see this UFO anymore
				if (!save.IsUfoOnIgnoreList(ufo.Id) && !ufo.Rules.IsNoAlert)
				{
					Popup(new UfoDetectedState(ufo, this, true, ufo.HyperDetected));
				}
			}
		}
		else
		{
			if (maskTest(detected, Acom.UfoDetection.DETECTION_HYPERWAVE))
			{
				ufo.HyperDetected = true;
			}
			// TODO: rethink: hunting UFOs stay visible even outside of radar range?
			if (!maskTest(detected, Acom.UfoDetection.DETECTION_RADAR) && !ufo.IsHunting)
			{
				ufo.Detected = false;
				ufo.HyperDetected = false;
				if (ufo.Followers.Count != 0)
				{
					Popup(new UfoLostState(ufo.GetName(_game.Language)));
				}
			}
		}
	}

	/// <summary>
	/// Takes care of any game logic that has to
	/// run every game hour, like transfers.
	/// </summary>
	public void Time1Hour()
	{
		var save = _game.SavedGame;
		// Handle craft maintenance
		foreach (var xbase in save.Bases)
		{
			foreach (var xcraft in xbase.Crafts)
			{
				if (xcraft.Status == "STR_REPAIRS")
				{
					xcraft.Repair();
				}
				else if (xcraft.Status == "STR_REARMING")
				{
					var ammo = xcraft.Rearm();
					if (ammo != null)
					{
						string msg = Tr("STR_NOT_ENOUGH_ITEM_TO_REARM_CRAFT_AT_BASE")
										   .Arg(Tr(ammo.Type))
										   .Arg(xcraft.GetName(_game.Language))
										   .Arg(xbase.GetName(_game.Language));
						Popup(new CraftErrorState(this, msg));
					}
				}
				if (xcraft.ShieldCapacity > 0 && xcraft.Status != "STR_OUT")
				{
					// Recharge craft shields in parallel (no wait for repair/rearm/refuel)
					xcraft.Shield = xcraft.Shield + xcraft.Rules.ShieldRechargeAtBase;
				}
			}
		}

		// Handle base defenses maintenance
		foreach (var xbase in save.Bases)
		{
			foreach (var facility in xbase.Facilities)
			{
				var ammo = facility.Rearm();
				if (ammo != null)
				{
					string msg = Tr("STR_NOT_ENOUGH_ITEM_TO_REARM_FACILITY_AT_BASE")
						.Arg(Tr(ammo.Type))
						.Arg(Tr(facility.Rules.Type))
						.Arg(xbase.GetName(_game.Language));
					Popup(new CraftErrorState(this, msg));
				}
			}
		}

		// Handle transfers
		bool window = false;
		foreach (var xbase in save.Bases)
		{
			foreach (var transfer in xbase.Transfers)
			{
				transfer.Advance(xbase);
				if (!window && transfer.Hours <= 0)
				{
					window = true;
				}
			}
		}
		if (window)
		{
			Popup(new ItemsArrivingState(this));
		}
		// Handle Production
		foreach (var xbase in save.Bases)
		{
			// C++ std::map<Production*, productionProgress_e>: pointer order; the port keeps the production order
			var toRemove = new List<(Production first, productionProgress_e second)>();
			foreach (var prod in xbase.Productions)
			{
				toRemove.Add((prod, prod.Step(xbase, save, _game.Mod, _game.Language)));
			}
			foreach (var pair in toRemove)
			{
				if (pair.second > productionProgress_e.PROGRESS_NOT_COMPLETE)
				{
					Popup(new ProductionCompleteState(xbase, Tr(pair.first.Rules.Name), this, pair.second, pair.first));
					xbase.RemoveProduction(pair.first);
				}
			}

			if (Options.storageLimitsEnforced)
			{
				var geoscapeInterface = _game.Mod.GetInterface("geoscape");
				if (xbase.StoresOverfull())
				{
					TimerReset();
					Popup(new ErrorMessageState(Tr("STR_STORAGE_EXCEEDED").Arg(xbase.GetName(_game.Language)), _palette, (byte)geoscapeInterface.GetElement("errorMessage").color, "BACK13.SCR", geoscapeInterface.GetElement("errorPalette").color));
					Popup(new SellState(xbase, null));
				}
				if (!save.AlienContainmentChecked)
				{
					var prisonTypes = new SortedDictionary<int, int>();
					foreach (var item in xbase.StorageItems.Contents)
					{
						RuleItem rule = item.Key;
						if (rule.IsAlien)
						{
							prisonTypes.TryGetValue(rule.PrisonType, out int n);
							prisonTypes[rule.PrisonType] = n + 1;
						}
					}
					foreach (var pair in prisonTypes)
					{
						int prisonType = pair.Key;
						if (xbase.GetUsedContainment(prisonType) > xbase.GetAvailableContainment(prisonType))
						{
							TimerReset();
							Popup(new ErrorMessageState(
								TrAlt("STR_CONTAINMENT_EXCEEDED", prisonType).Arg(xbase.GetName(_game.Language)),
								_palette,
								(byte)geoscapeInterface.GetElement("errorMessage").color,
								"BACK01.SCR",
								geoscapeInterface.GetElement("errorPalette").color));
							Popup(new ManageAlienContainmentState(xbase, prisonType, OptionsOrigin.OPT_GEOSCAPE));
							break;
						}
					}
				}
			}
		}
		save.AlienContainmentChecked = true; // check only once after reload

		bool postpone = false;
		foreach (var msite in save.MissionSites)
		{
			if (!msite.Detected)
			{
				postpone = true;
				msite.Detected = true;
				if (msite.SecondsRemaining < 3600)
				{
					msite.SecondsRemaining = 3600; // minimum 1 hour since detection
				}
				Popup(new MissionDetectedState(msite, this));
				break; // only one popup per hour!
			}
		}
		if (postpone)
		{
			foreach (var msite in save.MissionSites)
			{
				if (!msite.Detected)
				{
					msite.SecondsRemaining = msite.SecondsRemaining + 3600; // +1 hour
				}
			}
		}
		UpdateSlackingIndicator();
	}

	/// <summary>
	/// Check and create supply mission for the given base (C++ class GenerateSupplyMission).
	/// There is a 6/101 chance of the mission spawning.
	/// </summary>
	/// <param name="base">A pointer to the alien base.</param>
	private void GenerateSupplyMission(AlienBase @base)
	{
		Mod mod = _game.Mod;
		SavedGame save = _game.SavedGame;

		string missionName = @base.Deployment.ChooseGenMissionType();
		if (mod.GetAlienMission(missionName) != null)
		{
			if (@base.GenMissionCount < @base.Deployment.GenMissionLimit && Rng.Percent(@base.Deployment.GenMissionFrequency))
			{
				//Spawn supply mission for this base.
				RuleAlienMission rule = mod.GetAlienMission(missionName);
				var mission = new AlienMission(rule);
				string targetRegion;
				if (Rng.Percent(rule.TargetBaseOdds))
				{
					// 1. target a random xcom base region
					var regionsWithXcomBases = new List<string>();
					foreach (var xbase in save.Bases)
					{
						regionsWithXcomBases.Add(save.LocateRegion(xbase).Rules.Type);
					}
					targetRegion = regionsWithXcomBases[Rng.Generate(0, regionsWithXcomBases.Count - 1)];
				}
				else if (rule.HasRegionWeights)
				{
					// 2. target one of the defined (weighted) regions
					targetRegion = rule.GenerateRegion(save.MonthsPassed);
				}
				else
				{
					// 3. target the region of the alien base (vanilla default)
					targetRegion = save.LocateRegion(@base).Rules.Type;
				}
				mission.SetRegion(targetRegion, mod);
				mission.Id = save.GetId("ALIEN_MISSIONS");
				if (!@base.Deployment.IsGenMissionRaceFromAlienBase && rule.HasRaceWeights)
				{
					mission.Race = rule.GenerateRace(save.MonthsPassed);
				}
				else
				{
					mission.Race = @base.AlienRace;
				}
				mission.AlienBase = @base;
				int targetArea = -1;
				if (mission.Rules.Objective == MissionObjective.OBJECTIVE_SITE)
				{
					int missionZone = mission.Rules.SpawnZone;
					RuleRegion regionRules = mod.GetRegion(mission.Region);
					List<MissionArea> areas = regionRules.MissionZones[missionZone].areas;
					if (areas.Count != 0)
					{
						targetArea = Rng.Generate(0, areas.Count - 1);
					}
				}
				mission.SetMissionSiteZoneArea(targetArea);
				mission.Start(_game, _globe);
				@base.GenMissionCount = @base.GenMissionCount + 1; // increase counter, used to check mission limit
				save.AlienMissions.Add(mission);
			}
		}
		else if (!string.IsNullOrEmpty(missionName))
		{
			throw new InvalidOperationException("Alien Base tried to generate undefined mission: " + missionName);
		}
	}

	/// <summary>Collections::sortVector + sortVectorMakeUnique (see the deviation note at the top of the file).</summary>
	private static void MakeUnique<T>(List<T> list) where T : class
	{
		var seen = new HashSet<T>(ReferenceEqualityComparer.Instance);
		list.RemoveAll(x => !seen.Add(x));
	}

	/// <summary>
	/// Takes care of any game logic that has to
	/// run every game day, like constructions.
	/// </summary>
	public void Time1Day()
	{
		_game.SavedGame.IncreaseDaysPassed();

		SavedGame saveGame = _game.SavedGame;
		Mod mod = _game.Mod;
		bool psiStrengthEval = Options.psiStrengthEval && saveGame.IsResearched(mod.PsiRequirements);

		void addResearchDiaryEntryForBase(RuleResearch discoveredResearch, DiscoverySourceType sourceType, Base sourceBase, RuleResearch sourceResearch)
		{
			if (!saveGame.IsResearched(discoveredResearch) && !saveGame.IsResearchRuleStatusDisabled(discoveredResearch.Name))
			{
				var entry = new ResearchDiaryEntry(discoveredResearch);
				entry.SetDate(saveGame.Time);
				entry.source.type = sourceType;
				if (sourceType == DiscoverySourceType.BASE)
				{
					entry.source.name = sourceBase.GetName(_game.Language);
				}
				else // sourceType == DiscoverySourceType::FREE_FROM
				{
					entry.source.research = sourceResearch;
					entry.source.name = sourceResearch.Name;
				}
				saveGame.AddResearchDiaryEntry(entry);
			}
		}

		foreach (var xbase in saveGame.Bases)
		{
			// Handle facility construction
			// C++ std::map<const RuleBaseFacility*, int>: pointer order; the port keeps the order of first completion
			var finishedFacilities = new List<(RuleBaseFacility first, int second)>();
			foreach (var facility in xbase.Facilities)
			{
				if (facility.BuildTime > 0)
				{
					facility.Build();
					if (facility.BuildTime == 0)
					{
						int idx = finishedFacilities.FindIndex(p => p.first == facility.Rules);
						if (idx < 0)
							finishedFacilities.Add((facility.Rules, 1));
						else
							finishedFacilities[idx] = (facility.Rules, finishedFacilities[idx].second + 1);
					}
				}
			}
			foreach (var pair in finishedFacilities)
			{
				if (pair.second > 1)
				{
					string ssf = Tr(pair.first.Type) + " (x" + pair.second.ToString(CultureInfo.InvariantCulture) + ")";
					Popup(new ProductionCompleteState(xbase, ssf, this, productionProgress_e.PROGRESS_CONSTRUCTION));
				}
				else
				{
					Popup(new ProductionCompleteState(xbase, Tr(pair.first.Type), this, productionProgress_e.PROGRESS_CONSTRUCTION));
				}
			}

			// Handle science project
			// 1. gather finished research
			var finished = new List<ResearchProject>();
			foreach (var proj in xbase.Research)
			{
				if (proj.Step())
				{
					finished.Add(proj);
				}
			}
			// 2. remember available research before adding new finished research
			var before = new List<RuleResearch>();
			if (finished.Count != 0)
			{
				saveGame.GetAvailableResearchProjects(before, mod, xbase);
				foreach (var fp in finished)
				{
					if (fp.Rules.IsRepeatable)
					{
						RuleResearch nonconst = mod.GetResearch(fp.Rules.Name);
						before.Add(nonconst);
					}
				}
			}
			// 3. add finished research, including lookups and getonefrees (up to 4x)
			var topicsToCheck = new List<RuleResearch>();
			foreach (var finishedProject in finished)
			{
				ResearchProject project = finishedProject;
				RuleResearch bonus = null;
				RuleResearch research = project.Rules;

				// 3a. remove finished research from the base where it was researched
				xbase.RemoveResearch(project);
				project = null;

				// 3b. add core research to research diary (before the getonefrees)
				addResearchDiaryEntryForBase(research, DiscoverySourceType.BASE, xbase, null);
				if (research.Lookup is RuleResearch lookupResearch)
					addResearchDiaryEntryForBase(lookupResearch, DiscoverySourceType.BASE, xbase, null);
				// 3c. handle getonefrees (topic+lookup)
				if ((bonus = saveGame.SelectGetOneFree(research)) != null)
				{
					addResearchDiaryEntryForBase(bonus, DiscoverySourceType.FREE_FROM, null, research);
					saveGame.AddFinishedResearch(bonus, mod, xbase);
					if (bonus.Lookup is RuleResearch bonusLookup)
					{
						addResearchDiaryEntryForBase(bonusLookup, DiscoverySourceType.FREE_FROM, null, research);
						saveGame.AddFinishedResearch(bonusLookup, mod, xbase);
					}
				}
				// 3d. determine and remember if the ufopedia article should pop up again or not
				// Note: because different topics may lead to the same lookup
				RuleResearch newResearch = research;
				if (saveGame.IsResearched(research.Lookup ?? research, false))
				{
					newResearch = null;
				}
				// 3e. handle core research (topic+lookup)
				saveGame.AddFinishedResearch(research, mod, xbase);
				if (research.Lookup != null)
				{
					saveGame.AddFinishedResearch(research.Lookup, mod, xbase);
				}
				// 3e. handle cutscene
				if (!string.IsNullOrEmpty(research.Cutscene))
				{
					Popup(new CutsceneState(research.Cutscene));
					if (saveGame.Ending == GameEnding.END_NONE)
					{
						RuleVideo videoRule = _game.Mod.GetVideo(research.Cutscene, true);
						if (videoRule.WinGame) saveGame.Ending = GameEnding.END_WIN;
						if (videoRule.LoseGame) saveGame.Ending = GameEnding.END_LOSE;
					}
				}
				if (bonus != null && !string.IsNullOrEmpty(bonus.Cutscene))
				{
					Popup(new CutsceneState(bonus.Cutscene));
					if (saveGame.Ending == GameEnding.END_NONE)
					{
						RuleVideo videoRule = _game.Mod.GetVideo(bonus.Cutscene, true);
						if (videoRule.WinGame) saveGame.Ending = GameEnding.END_WIN;
						if (videoRule.LoseGame) saveGame.Ending = GameEnding.END_LOSE;
					}
				}
				// 3e. handle research complete popup + ufopedia article popups (topic+bonus)
				Popup(new ResearchCompleteState(newResearch, bonus, research, xbase));
				// 3f. reset timer
				TimerReset();
				// 3g. warning if weapon is researched before its clip
				if (newResearch != null)
				{
					RuleItem item = mod.GetItem(newResearch.Name);
					if (item != null && item.BattleType == BattleType.BT_FIREARM && item.PrimaryCompatibleAmmo.Count != 0)
					{
						RuleManufacture man = mod.GetManufacture(item.Type);
						if (man != null && man.Requirements.Count != 0)
						{
							var req = man.Requirements;
							RuleItem ammo = item.PrimaryCompatibleAmmo[0];
							if (req.Exists(r => r.Name == ammo.Type) && !saveGame.IsResearched(req, true))
							{
								Popup(new ResearchRequiredState(item));
							}
						}
					}
				}
				// 3h. inform about new possible research
				var after = new List<RuleResearch>();
				saveGame.GetAvailableResearchProjects(after, mod, xbase);
				var newPossibleResearch = new List<RuleResearch>();
				saveGame.GetNewlyAvailableResearchProjects(before, after, newPossibleResearch);
				Popup(new NewPossibleResearchState(xbase, newPossibleResearch));
				// 3i. inform about new possible manufacture, purchase, craft and facilities
				var newPossibleManufacture = new List<RuleManufacture>();
				saveGame.GetDependableManufacture(newPossibleManufacture, research, mod, xbase);
				if (bonus != null)
				{
					saveGame.GetDependableManufacture(newPossibleManufacture, bonus, mod, xbase);
				}
				if (newPossibleManufacture.Count != 0)
				{
					MakeUnique(newPossibleManufacture);
					Popup(new NewPossibleManufactureState(xbase, newPossibleManufacture));
				}
				var newPossiblePurchase = new List<RuleItem>();
				_game.SavedGame.GetDependablePurchase(newPossiblePurchase, research, _game.Mod);
				if (bonus != null)
				{
					_game.SavedGame.GetDependablePurchase(newPossiblePurchase, bonus, _game.Mod);
				}
				if (newPossiblePurchase.Count != 0)
				{
					MakeUnique(newPossiblePurchase);
					Popup(new NewPossiblePurchaseState(xbase, newPossiblePurchase));
				}
				var newPossibleCraft = new List<RuleCraft>();
				_game.SavedGame.GetDependableCraft(newPossibleCraft, research, _game.Mod);
				if (bonus != null)
				{
					_game.SavedGame.GetDependableCraft(newPossibleCraft, bonus, _game.Mod);
				}
				if (newPossibleCraft.Count != 0)
				{
					MakeUnique(newPossibleCraft);
					Popup(new NewPossibleCraftState(xbase, newPossibleCraft));
				}
				var newPossibleFacilities = new List<RuleBaseFacility>();
				_game.SavedGame.GetDependableFacilities(newPossibleFacilities, research, _game.Mod);
				if (bonus != null)
				{
					_game.SavedGame.GetDependableFacilities(newPossibleFacilities, bonus, _game.Mod);
				}
				if (newPossibleFacilities.Count != 0)
				{
					MakeUnique(newPossibleFacilities);
					Popup(new NewPossibleFacilityState(xbase, _globe, newPossibleFacilities));
				}

				topicsToCheck.Add(research);
				if (bonus != null)
				{
					topicsToCheck.Add(bonus);
				}
			}
			if (topicsToCheck.Count != 0)
			{
				MakeUnique(topicsToCheck);
				// Side effects:
				// 1. remove obsolete research projects from all bases
				// 2. handle items spawned by research
				// 3. handle events spawned by research
				saveGame.HandlePrimaryResearchSideEffects(topicsToCheck, _game.Mod, xbase);
			}

			// Handle soldier wounds and martial training
			BaseSumDailyRecovery recovery = xbase.SumRecoveryPerDay;
			var trainingFinishedList = new List<Soldier>();
			foreach (var soldier in xbase.Soldiers)
			{
				soldier.ReplenishStats(recovery);

				if (soldier.IsInTraining)
				{
					soldier.TrainPhys(_game.Mod.CustomTrainingFactor);
					soldier.CalcStatString(_game.Mod.StatStrings, psiStrengthEval);
					if (soldier.IsFullyTrained)
					{
						soldier.SetTraining(false);
						trainingFinishedList.Add(soldier);
					}
				}
				else
				{
					if (soldier.ReturnToTrainingWhenHealed && !soldier.IsWounded)
					{
						if (!soldier.IsFullyTrained && xbase.FreeTrainingSpace > 0)
						{
							soldier.SetTraining(true);
						}
						// only ever try to return once
						soldier.ReturnToTrainingWhenHealed = false;
					}
				}
			}
			if (trainingFinishedList.Count != 0)
			{
				Popup(new TrainingFinishedState(xbase, trainingFinishedList, false));
			}
			// Handle psionic training
			if (xbase.AvailablePsiLabs > 0 && Options.anytimePsiTraining)
			{
				var psiTrainingFinishedList = new List<Soldier>();
				foreach (var s in xbase.Soldiers)
				{
					s.TrainPsi1Day();
					s.CalcStatString(_game.Mod.StatStrings, psiStrengthEval);
					if (s.IsInPsiTraining && s.IsFullyPsiTrained)
					{
						s.SetPsiTraining(false);
						psiTrainingFinishedList.Add(s);
					}
				}
				if (psiTrainingFinishedList.Count != 0)
				{
					Popup(new TrainingFinishedState(xbase, psiTrainingFinishedList, true));
				}
			}
		}

		// check and remove disabled projects from ongoing research
		foreach (var xbase in _game.SavedGame.Bases)
		{
			var obsolete = new List<ResearchProject>();
			foreach (var proj in xbase.Research)
			{
				if (_game.SavedGame.IsResearchRuleStatusDisabled(proj.Rules.Name))
				{
					obsolete.Add(proj);
				}
			}
			foreach (var proj in obsolete)
			{
				xbase.RemoveResearch(proj);
			}
		}

		// check and interrupt alien missions if necessary (based on discovered research)
		foreach (var am in saveGame.AlienMissions)
		{
			var researchName = am.Rules.InterruptResearch;
			if (!string.IsNullOrEmpty(researchName))
			{
				var research = mod.GetResearch(researchName, true);
				if (saveGame.IsResearched(research, false)) // ignore debug mode
				{
					am.SetInterrupted(true);
				}
			}
		}

		// check and self-destruct alien bases if necessary (based on discovered research)
		var alienBases = saveGame.AlienBases;
		for (int abIt = 0; abIt < alienBases.Count;)
		{
			AlienBase ab = alienBases[abIt];
			var selfDestructCode = ab.Deployment.BaseSelfDestructCode;
			if (!string.IsNullOrEmpty(selfDestructCode))
			{
				var research = mod.GetResearch(selfDestructCode, true);
				if (saveGame.IsResearched(research, false)) // ignore debug mode
				{
					saveGame.ClearLinksForAlienBase(ab, _game.Mod);
					ab.Destroy();
					alienBases.RemoveAt(abIt);
				}
				else
				{
					++abIt;
				}
			}
			else
			{
				++abIt;
			}
		}

		// handle regional and country points for alien bases
		foreach (var ab in saveGame.AlienBases)
		{
			foreach (var region in saveGame.Regions)
			{
				if (region.Rules.InsideRegion(ab.Longitude, ab.Latitude))
				{
					region.AddActivityAlien(ab.Deployment.Points);
					break;
				}
			}
			foreach (var country in saveGame.Countries)
			{
				if (country.Rules.InsideCountry(ab.Longitude, ab.Latitude))
				{
					country.AddActivityAlien(ab.Deployment.Points);
					break;
				}
			}
		}

		// Handle resupply of alien bases.
		foreach (var ab in saveGame.AlienBases)
		{
			GenerateSupplyMission(ab);
		}

		// Handle alien base detection (by xcom base facilities).
		foreach (var alienBase in _game.SavedGame.AlienBases)
		{
			if (alienBase.IsDiscovered) continue;
			foreach (var xcomBase in _game.SavedGame.Bases)
			{
				int distance = XcomDistance(xcomBase.GetDistance(alienBase));
				foreach (var facility in xcomBase.Facilities)
				{
					if (facility.BuildTime == 0 && facility.Rules.SightRange > distance)
					{
						int chanceToDetect = facility.Rules.SightChance; // static % defined by the modder
						if (chanceToDetect == 0)
						{
							chanceToDetect = 50 - (distance * 50 / facility.Rules.SightRange); // dynamic 0-50% based on relative distance
						}
						if (Rng.Percent(chanceToDetect))
						{
							alienBase.SetDiscovered(true);
							Popup(new AlienBaseState(alienBase, this));
						}
					}
				}
			}
		}

		// clear the daily dogfight experience cache
		foreach (var xbase in saveGame.Bases)
		{
			foreach (var soldier in xbase.Soldiers)
			{
				soldier.ResetDailyDogfightExperienceCache();
			}
		}

		// Autosave 3 times a month
		bool performGeoAutosave = false;
		int day = saveGame.Time.Day;
		if (Options.oxceGeoAutosaveFrequency == 0 && (day == 10 || day == 20))
		{
			// OXC backwards-compatibility
			performGeoAutosave = true;
		}
		else if (Options.oxceGeoAutosaveFrequency >= 1 && Options.oxceGeoAutosaveFrequency <= 10)
		{
			// every X-th day
			performGeoAutosave = saveGame.DaysPassed % Options.oxceGeoAutosaveFrequency == 0;
		}
		if (performGeoAutosave)
		{
			if (saveGame.IsIronman)
			{
				Popup(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_IRONMAN, _palette));
			}
			else if (Options.autosave)
			{
				Popup(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_AUTO_GEOSCAPE, _palette, saveGame.DaysPassed));
			}
		}
		else if (saveGame.Ending != GameEnding.END_NONE && saveGame.IsIronman)
		{
			_game.PushState(new SaveGameState(OptionsOrigin.OPT_GEOSCAPE, SaveType.SAVE_IRONMAN, _palette));
		}

		// pay attention to your maintenance player!
		if (_game.SavedGame.Time.IsLastDayOfMonth)
		{
			int month = _game.SavedGame.MonthsPassed;
			int currentScore = _game.SavedGame.GetCurrentScore(month + 1);
			int performanceBonus = mod.GetPerformanceBonus(currentScore);
			if (performanceBonus < 0)
			{
				performanceBonus = 0; // bonus only, no malus
			}

			long funds = _game.SavedGame.Funds;
			long income = _game.SavedGame.CountryFunding + performanceBonus;
			long maintenance = _game.SavedGame.BaseMaintenance;
			long projection = funds + income - maintenance;
			if (projection < 0)
			{
				projection = Math.Abs(projection);
				projection = ((projection / 100000) + 1) * 100000; // round up to 100k
				string msg = Tr("STR_ECONOMY_WARNING")
					.Arg(Unicode.FormatFunding(funds))
					.Arg(Unicode.FormatFunding(income))
					.Arg(Unicode.FormatFunding(maintenance))
					.Arg(Unicode.FormatFunding(projection));
				Popup(new CraftErrorState(this, msg, false));
			}
		}
	}

	/// <summary>
	/// Takes care of any game logic that has to
	/// run every game month, like funding.
	/// </summary>
	public void Time1Month()
	{
		_game.SavedGame.AddMonth();

		// Determine alien mission for this month.
		DetermineAlienMissions();

		// Handle Psi-Training and initiate a new retaliation mission, if applicable
		if (!Options.anytimePsiTraining)
		{
			bool psiStrengthEval = Options.psiStrengthEval && _game.SavedGame.IsResearched(_game.Mod.PsiRequirements);
			foreach (var xbase in _game.SavedGame.Bases)
			{
				if (xbase.AvailablePsiLabs > 0)
				{
					foreach (var soldier in xbase.Soldiers)
					{
						if (soldier.IsInPsiTraining)
						{
							soldier.TrainPsi();
							soldier.CalcStatString(_game.Mod.StatStrings, psiStrengthEval);
						}
					}
				}
			}
		}

		// Handle funding
		TimerReset();
		_game.SavedGame.MonthlyFunding();
		Popup(new MonthlyReportState(_globe));

		// Handle Xcom Operatives discovering bases
		if (_game.SavedGame.AlienBases.Count != 0 && Rng.Percent(_game.Mod.ChanceToDetectAlienBaseEachMonth))
		{
			foreach (var ab in _game.SavedGame.AlienBases)
			{
				if (!ab.IsDiscovered)
				{
					ab.SetDiscovered(true);
					Popup(new AlienBaseState(ab, this));
					break;
				}
			}
		}
	}

	/// <summary>
	/// Slows down the timer back to minimum speed,
	/// for when important events occur.
	/// </summary>
	public void TimerReset()
	{
		var ev = new SDL_Event();
		ev.button.button = SDLMouse.SDL_BUTTON_LEFT;
		var act = new Action(ev, _game.Screen.XScale, _game.Screen.YScale, _game.Screen.CursorTopBlackBand, _game.Screen.CursorLeftBlackBand);
		_btn5Secs.MousePress(act, this);
	}

	/// <summary>
	/// Adds a new popup window to the queue
	/// (this prevents popups from overlapping)
	/// and pauses the game timer respectively.
	/// </summary>
	/// <param name="state">Pointer to popup state.</param>
	public void Popup(State state)
	{
		_pause = true;
		_popups.Add(state);
	}

	/// <summary>
	/// Returns a pointer to the Geoscape globe for
	/// access by other substates.
	/// </summary>
	public Globe Globe => _globe;

	/// <summary>
	/// Processes any left-clicks on globe markers,
	/// or right-clicks to scroll the globe.
	/// </summary>
	public void GlobeClick(Action action)
	{
		int mouseX = (int)Math.Floor(action.AbsoluteXMouse), mouseY = (int)Math.Floor(action.AbsoluteYMouse);

		// Clicking markers on the globe
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			List<Target> v = _globe.GetTargets(mouseX, mouseY, false, null);
			if (v.Count != 0)
			{
				// Pass empty vector
				var crafts = new List<Craft>();
				_game.PushState(new MultipleTargetsState(v, crafts, this, true));
			}
		}

		if (_game.SavedGame.DebugMode)
		{
			double lon = 0, lat = 0;
			int texture = 0, shade = 0;
			_globe.CartToPolar((short)mouseX, (short)mouseY, ref lon, ref lat);
			double lonDeg = lon / Math.PI * 180, latDeg = lat / Math.PI * 180;
			_globe.GetPolygonTextureAndShade(lon, lat, ref texture, ref shade);
			var ss = new StringBuilder();
			ss.Append("rad: ").Append(Str(lon)).Append(", ").Append(Str(lat)).Append('\n');
			ss.Append("deg: ").Append(Str(lonDeg)).Append(", ").Append(Str(latDeg)).Append('\n');
			ss.Append("texture: ").Append(texture.ToString(CultureInfo.InvariantCulture)).Append(", shade: ").Append(shade.ToString(CultureInfo.InvariantCulture)).Append('\n');

			_txtDebug.SetText(ss.ToString());
		}
	}

	/// <summary>Formats a double like the default std::ostream (6 significant digits).</summary>
	private static string Str(double value) => LocalizedText.FormatLikeOstream(value);

	/// <summary>
	/// Opens the Intercept window.
	/// </summary>
	public void BtnInterceptClick(Action action)
	{
		if (ButtonsDisabled())
		{
			return;
		}
		_game.PushState(new InterceptState(_globe, false, null, null));
	}

	/// <summary>
	/// Opens the UFO Tracker window.
	/// </summary>
	public void BtnUfoTrackerClick(Action action)
	{
		_game.PushState(new UfoTrackerState(this, _globe));
	}

	/// <summary>
	/// Opens the TechTreeViewer window.
	/// </summary>
	public void BtnTechTreeViewerClick(Action action)
	{
		_game.PushState(new TechTreeViewerState(null, null, null, null));
	}

	/// <summary>
	/// Opens the jukebox.
	/// </summary>
	public void BtnSelectMusicTrackClick(Action action)
	{
		_game.PushState(new SelectMusicTrackState(SelectMusicTrackOrigin.SMT_GEOSCAPE));
	}

	/// <summary>
	/// Opens the Current Global Production.
	/// </summary>
	public void BtnGlobalProductionClick(Action action)
	{
		_game.PushState(new GlobalManufactureState(false));
	}

	/// <summary>
	/// Opens the Current Global Research.
	/// </summary>
	public void BtnGlobalResearchClick(Action action)
	{
		_game.PushState(new GlobalResearchState(false));
	}

	/// <summary>
	/// Opens the Global Alien Containment overview.
	/// </summary>
	public void BtnGlobalAlienContainmentClick(Action action)
	{
		_game.PushState(new GlobalAlienContainmentState(false));
	}

	/// <summary>
	/// Opens the Dogfight Experience screen.
	/// </summary>
	public void BtnDogfightExperienceClick(Action action)
	{
		_game.PushState(new DogfightExperienceState());
	}

	/// <summary>
	/// Toggles debug mode.
	/// </summary>
	public void BtnDebugClick(Action action)
	{
		var save = _game.SavedGame;
		save.SetDebugMode();
		if (save.DebugMode)
		{
			_txtDebug.SetText("DEBUG MODE");
		}
		else
		{
			_txtDebug.SetText("");
		}
		_cbxRegion.Visible = save.DebugMode && save.debugType >= 1;
		_cbxZone.Visible = save.DebugMode && save.debugType == 2;
		_cbxArea.Visible = save.DebugMode && save.debugType == 2;
		_cbxCountry.Visible = save.DebugMode && save.debugType == 0;
	}

	/// <summary>
	/// Goes to the Basescape screen.
	/// </summary>
	public void BtnBasesClick(Action action)
	{
		if (ButtonsDisabled())
		{
			return;
		}
		TimerReset();
		var save = _game.SavedGame;
		if (save.Bases.Count != 0)
		{
			if (Options.oxceGeoGoToNearestBase)
			{
				var xbaseSorting = new List<(int first, double second)>();
				int baseIdx = 0;
				foreach (var xbase in save.Bases)
				{
					double xdistance = xbase.GetDistance(save.GlobeLongitude, save.GlobeLatitude);
					xbaseSorting.Add((baseIdx, xdistance));
					baseIdx++;
				}
				// std::stable_sort
				var sorted = xbaseSorting.OrderBy(p => p.second).ToList();
				save.SetSelectedBase(sorted[0].first);
			}
			_game.PushState(new BasescapeState(save.SelectedBase, _globe));
		}
		else
		{
			_game.PushState(new BasescapeState(null, _globe));
		}
	}

	/// <summary>
	/// Goes to the Graphs screen.
	/// </summary>
	public void BtnGraphsClick(Action action)
	{
		if (ButtonsDisabled())
		{
			return;
		}
		_game.PushState(new GraphsState());
	}

	/// <summary>
	/// Goes to the Ufopaedia window.
	/// </summary>
	public void BtnUfopaediaClick(Action action)
	{
		if (ButtonsDisabled())
		{
			return;
		}
		Ufopaedia.Open(_game);
	}

	/// <summary>
	/// Opens the Options window.
	/// </summary>
	public void BtnOptionsClick(Action action)
	{
		if (ButtonsDisabled())
		{
			return;
		}
		_game.PushState(new PauseState(OptionsOrigin.OPT_GEOSCAPE));
	}

	/// <summary>
	/// Goes to the Funding screen.
	/// </summary>
	public void BtnFundingClick(Action action)
	{
		if (ButtonsDisabled())
		{
			return;
		}
		if (Options.oxceLinks)
		{
			_game.PushState(new ExtendedGeoscapeLinksState(this));
		}
		else
		{
			_game.PushState(new FundingState());
		}
	}

	/// <summary>Starts rotating the globe to the left.</summary>
	public void BtnRotateLeftPress(Action action) => _globe.RotateLeft();

	/// <summary>Stops rotating the globe to the left.</summary>
	public void BtnRotateLeftRelease(Action action) => _globe.RotateStopLon();

	/// <summary>Starts rotating the globe to the right.</summary>
	public void BtnRotateRightPress(Action action) => _globe.RotateRight();

	/// <summary>Stops rotating the globe to the right.</summary>
	public void BtnRotateRightRelease(Action action) => _globe.RotateStopLon();

	/// <summary>Starts rotating the globe upwards.</summary>
	public void BtnRotateUpPress(Action action) => _globe.RotateUp();

	/// <summary>Stops rotating the globe upwards.</summary>
	public void BtnRotateUpRelease(Action action) => _globe.RotateStopLat();

	/// <summary>Starts rotating the globe downwards.</summary>
	public void BtnRotateDownPress(Action action) => _globe.RotateDown();

	/// <summary>Stops rotating the globe downwards.</summary>
	public void BtnRotateDownRelease(Action action) => _globe.RotateStopLat();

	/// <summary>Zooms into the globe.</summary>
	public void BtnZoomInLeftClick(Action action) => _globe.ZoomIn();

	/// <summary>Zooms the globe maximum.</summary>
	public void BtnZoomInRightClick(Action action) => _globe.ZoomMax();

	/// <summary>Zooms out of the globe.</summary>
	public void BtnZoomOutLeftClick(Action action) => _globe.ZoomOut();

	/// <summary>Zooms the globe minimum.</summary>
	public void BtnZoomOutRightClick(Action action) => _globe.ZoomMin();

	/// <summary>
	/// Zoom in effect for dogfights.
	/// </summary>
	public void ZoomInEffect()
	{
		if (_globe.ZoomDogfightIn())
		{
			_zoomInEffectDone = true;
			_zoomInEffectTimer.Stop();
		}
	}

	/// <summary>
	/// Zoom out effect for dogfights.
	/// </summary>
	public void ZoomOutEffect()
	{
		if (_globe.ZoomDogfightOut())
		{
			_zoomOutEffectDone = true;
			_zoomOutEffectTimer.Stop();
			Init();
		}
	}

	/// <summary>
	/// Dogfight logic. Moved here to have the code clean.
	/// </summary>
	public void HandleDogfights()
	{
		// Handle dogfights logic.
		_minimizedDogfights = 0;

		foreach (var dfs in _dogfights)
		{
			dfs.Ufo.InterceptionProcessed = false;
		}

		for (int dfsIt = 0; dfsIt < _dogfights.Count;)
		{
			DogfightState dfs = _dogfights[dfsIt];
			if (dfs.IsMinimized)
			{
				if (dfs.WaitForPoly && _globe.InsideLand(dfs.Ufo.Longitude, dfs.Ufo.Latitude))
				{
					dfs.SetMinimized(false);
					dfs.WaitForPoly = false;
				}
				else if (dfs.WaitForAltitude && dfs.Ufo.AltitudeInt <= dfs.Craft.Rules.MaxAltitude)
				{
					dfs.SetMinimized(false);
					dfs.WaitForAltitude = false;
				}
				else
				{
					_minimizedDogfights++;
				}
			}
			else
			{
				_globe.RotateStop();
			}
			dfs.Think();
			if (dfs.DogfightEnded())
			{
				if (dfs.IsMinimized)
				{
					_minimizedDogfights--;
				}
				_dogfights.RemoveAt(dfsIt);
			}
			else
			{
				++dfsIt;
			}
		}
		if (_dogfights.Count == 0)
		{
			_dogfightTimer.Stop();
			_zoomOutEffectTimer.Start();
		}
	}

	/// <summary>
	/// Goes through all active dogfight instances and tries to perform the same action.
	/// </summary>
	/// <param name="button">Action to perform.</param>
	public void HandleDogfightMultiAction(int button)
	{
		foreach (var dfs in _dogfights)
		{
			if (dfs.IsMinimized)
				continue;

			var ev = new SDL_Event();
			ev.type = SDLEventType.SDL_MOUSEBUTTONDOWN;
			ev.button.button = SDLMouse.SDL_BUTTON_LEFT;
			var a = new Action(ev, 0.0, 0.0, 0, 0);

			switch (button)
			{
				case 0:
					dfs.BtnStandoffSimulateLeftPress(a);
					break;
				case 1:
					dfs.BtnCautiousSimulateLeftPress(a);
					break;
				case 2:
					dfs.BtnStandardSimulateLeftPress(a);
					break;
				case 3:
					dfs.BtnAggressiveSimulateLeftPress(a);
					break;
				case 4:
					dfs.BtnDisengageSimulateLeftPress(a);
					break;
				default:
					break;
			}
		}
	}

	/// <summary>
	/// Goes through all dogfight instances and tries to award pilot experience.
	/// This is called each time any UFO takes any damage in dogfight... very ugly, but I couldn't find a better place for it.
	/// </summary>
	public void HandleDogfightExperience()
	{
		foreach (var dfs in _dogfights)
		{
			dfs.AwardExperienceToPilots();
		}
		foreach (var dfs in _dogfightsToBeStarted)
		{
			dfs.AwardExperienceToPilots();
		}
	}

	/// <summary>
	/// Gets the number of minimized dogfights.
	/// </summary>
	public int MinimizedDogfightsCount()
	{
		int minimizedDogfights = 0;
		foreach (var dfs in _dogfights)
		{
			if (dfs.IsMinimized)
			{
				++minimizedDogfights;
			}
		}
		return minimizedDogfights;
	}

	/// <summary>
	/// Starts a new dogfight.
	/// </summary>
	public void StartDogfight()
	{
		if (_globe.Zoom < 3)
		{
			if (!_zoomInEffectTimer.IsRunning)
			{
				_globe.SaveZoomDogfight();
				_globe.RotateStop();
				_zoomInEffectTimer.Start();
			}
		}
		else
		{
			_dogfightStartTimer.Stop();
			_zoomInEffectTimer.Stop();
			_dogfightTimer.Start();
			TimerReset();
			while (_dogfightsToBeStarted.Count != 0)
			{
				_dogfights.Add(_dogfightsToBeStarted[^1]);
				_dogfightsToBeStarted.RemoveAt(_dogfightsToBeStarted.Count - 1);
				_dogfights[^1].InterceptionNumber = FirstFreeDogfightSlot;
				_dogfights[^1].SetInterceptionsCount(_dogfights.Count + _dogfightsToBeStarted.Count);
			}
			// Set correct number of interceptions for every dogfight.
			foreach (var dfs in _dogfights)
			{
				dfs.SetInterceptionsCount(_dogfights.Count);
			}
		}
	}

	/// <summary>
	/// Returns the first free dogfight slot.
	/// </summary>
	public int FirstFreeDogfightSlot
	{
		get
		{
			int slotNo = 1;
			foreach (var dfs in _dogfights)
			{
				if (dfs.InterceptionNumber == slotNo)
				{
					++slotNo;
				}
			}
			return slotNo;
		}
	}

	/// <summary>
	/// Handle base defense
	/// </summary>
	/// <param name="base">Base to defend.</param>
	/// <param name="ufo">Ufo attacking base.</param>
	public void HandleBaseDefense(Base @base, Ufo ufo)
	{
		// Get the shade and texture for the globe at the location of the base, using the ufo position
		int texture = 0, shade = 0;
		double baseLon = ufo.Longitude;
		double baseLat = ufo.Latitude;
		_globe.GetPolygonTextureAndShade(baseLon, baseLat, ref texture, ref shade);

		int ufoDamagePercentage = 0;
		if (_game.Mod.LessAliensDuringBaseDefense)
		{
			ufoDamagePercentage = ufo.DamagePercentage;
		}

		// Whatever happens in the base defense, the UFO has finished its duty
		ufo.Status = Ufo.UfoStatus.DESTROYED;

		// instant retaliation mission only spawns one UFO and then ends
		if (ufo.Mission.Rules.Objective == MissionObjective.OBJECTIVE_INSTANT_RETALIATION)
		{
			ufo.Mission.SetInterrupted(true);
		}

		if (ufo.Rules.MissilePower != 0)
		{
			if (ufo.Rules.MissilePower < 0)
			{
				// It's a nuclear warhead... Skynet knows no mercy
				Popup(new BaseDestroyedState(@base, ufo, true, false));
			}
			else
			{
				// This is an overkill, since we may not lose any hangar/craft, but doing it properly requires tons of changes
				_game.SavedGame.StopHuntingXcomCrafts(@base);

				// This can either damage facilities (=replace them with some other facilities); or also destroy facilities
				@base.DamageFacilities(ufo);

				// don't forget to reset pre-cached stuff
				@base.CleanupDefenses(true);

				// let the player know that some facilities were destroyed, but the base survived
				Popup(new BaseDestroyedState(@base, ufo, true, true));
			}

			// continue mayhem?
			if (ufo.Rules.MissileStopChance > 0 && Rng.Percent(ufo.Rules.MissileStopChance))
			{
				ufo.Mission.SetInterrupted(true);
			}
		}
		else if (@base.GetAvailableSoldiers(true, true) > 0 || @base.Vehicles.Count != 0)
		{
			var bgame = new SavedBattleGame(_game.Mod, _game.Language);
			_game.SavedGame.SetBattleGame(bgame);
			bgame.MissionType = "STR_BASE_DEFENSE";
			var bgen = new BattlescapeGenerator(_game);
			bgen.SetBase(@base);
			bgen.SetAlienCustomDeploy(_game.Mod.GetDeployment(ufo.CraftStats.missionCustomDeploy));
			bgen.SetAlienRace(ufo.AlienRace);
			bgen.SetWorldShade(shade);
			Texture globeTexture = _game.Mod.Globe.GetTexture(texture);
			bgen.SetWorldTexture(globeTexture, globeTexture);
			bgen.SetUfoDamagePercentage(ufoDamagePercentage);
			bgen.Run();
			_pause = true;
			_game.PushState(new BriefingState(null, @base));
		}
		else
		{
			// Please garrison your bases in future
			Popup(new BaseDestroyedState(@base, ufo, false, false));
		}
	}

	/// <summary>
	/// The trigger checks shared by arc scripts, mission scripts and event scripts ("level two" conditions).
	/// </summary>
	private static bool CheckScriptTriggers(SavedGame save, AlienStrategy strategy, Mod mod,
		SortedDictionary<string, bool> researchTriggers, int counterMin, int counterMax, string missionVarName, string missionMarkerName,
		SortedDictionary<string, bool> itemTriggers, SortedDictionary<string, bool> facilityTriggers, SortedDictionary<string, bool> soldierTypeTriggers,
		SortedDictionary<string, bool> xcomBaseInRegionTriggers, SortedDictionary<string, bool> xcomBaseInCountryTriggers, SortedDictionary<string, bool> pactCountryTriggers,
		HashSet<string> xcomBaseRegions, HashSet<string> xcomBaseCountries, HashSet<string> pactCountries)
	{
		// level two condition check: make sure we meet any research requirements, if any.
		bool triggerHappy = true;
		foreach (var trigger in researchTriggers)
		{
			triggerHappy = save.IsResearched(trigger.Key) == trigger.Value;
			if (!triggerHappy)
				break;
		}
		if (triggerHappy)
		{
			// check counters
			if (counterMin > 0)
			{
				if (!string.IsNullOrEmpty(missionVarName) && counterMin > strategy.GetMissionsRun(missionVarName))
				{
					triggerHappy = false;
				}
				if (!string.IsNullOrEmpty(missionMarkerName) && counterMin > save.GetLastId(missionMarkerName))
				{
					triggerHappy = false;
				}
			}
			if (triggerHappy && counterMax != -1)
			{
				if (!string.IsNullOrEmpty(missionVarName) && counterMax < strategy.GetMissionsRun(missionVarName))
				{
					triggerHappy = false;
				}
				if (!string.IsNullOrEmpty(missionMarkerName) && counterMax < save.GetLastId(missionMarkerName))
				{
					triggerHappy = false;
				}
			}
		}
		if (triggerHappy)
		{
			// item requirements
			foreach (var triggerItem in itemTriggers)
			{
				triggerHappy = save.IsItemObtained(triggerItem.Key, mod) == triggerItem.Value;
				if (!triggerHappy)
					break;
			}
		}
		if (triggerHappy)
		{
			// facility requirements
			foreach (var triggerFacility in facilityTriggers)
			{
				triggerHappy = save.IsFacilityBuilt(triggerFacility.Key) == triggerFacility.Value;
				if (!triggerHappy)
					break;
			}
		}
		if (triggerHappy)
		{
			// soldier type requirements
			foreach (var triggerSoldierType in soldierTypeTriggers)
			{
				triggerHappy = save.IsSoldierTypeHired(triggerSoldierType.Key) == triggerSoldierType.Value;
				if (!triggerHappy)
					break;
			}
		}
		if (triggerHappy)
		{
			// xcom base requirements
			foreach (var triggerXcomBase in xcomBaseInRegionTriggers)
			{
				bool found = xcomBaseRegions.Contains(triggerXcomBase.Key);
				triggerHappy = found == triggerXcomBase.Value;
				if (!triggerHappy)
					break;
			}
		}
		if (triggerHappy)
		{
			// xcom base requirements by country
			foreach (var triggerXcomBase2 in xcomBaseInCountryTriggers)
			{
				bool found = xcomBaseCountries.Contains(triggerXcomBase2.Key);
				triggerHappy = found == triggerXcomBase2.Value;
				if (!triggerHappy)
					break;
			}
		}
		if (triggerHappy)
		{
			// country with pact
			foreach (var triggerPact in pactCountryTriggers)
			{
				bool found = pactCountries.Contains(triggerPact.Key);
				triggerHappy = found == triggerPact.Value;
				if (!triggerHappy)
					break;
			}
		}
		return triggerHappy;
	}

	/// <summary>Enables one arc research (and its lookup) and opens its article.</summary>
	private void EnableArc(SavedGame save, Mod mod, Base hq, RuleResearch ruleResearch)
	{
		save.AddFinishedResearch(ruleResearch, mod, hq, true);
		if (ruleResearch != null)
		{
			if (ruleResearch.Lookup != null)
			{
				save.AddFinishedResearch(ruleResearch.Lookup, mod, hq, true);
				Ufopaedia.OpenArticle(_game, ruleResearch.Lookup.Name);
			}
			else
			{
				Ufopaedia.OpenArticle(_game, ruleResearch.Name);
			}
		}
	}

	/// <summary>
	/// Determine the alien missions to start this month.
	/// </summary>
	public void DetermineAlienMissions(bool isNewMonth = true, RuleEvent eventRules = null)
	{
		SavedGame save = _game.SavedGame;
		AlienStrategy strategy = save.AlienStrategy;
		Mod mod = _game.Mod;
		int month = _game.SavedGame.MonthsPassed;
		int currentScore = save.GetCurrentScore(month); // _monthsPassed was already increased by 1
		int performanceBonus = mod.GetPerformanceBonus(currentScore);
		if (performanceBonus < 0)
		{
			performanceBonus = 0; // bonus only, no malus
		}
		long currentFunds = save.Funds;
		if (isNewMonth)
		{
			currentFunds += save.CountryFunding + performanceBonus - save.BaseMaintenance; // peek into the next month
		}
		var availableMissions = new List<RuleMissionScript>();
		var conditions = new Dictionary<int, bool>();

		var xcomBaseRegions = new HashSet<string>(StringComparer.Ordinal);
		var xcomBaseCountries = new HashSet<string>(StringComparer.Ordinal);
		var pactCountries = new HashSet<string>(StringComparer.Ordinal);
		foreach (var xcomBase in save.Bases)
		{
			var region = save.LocateRegion(xcomBase);
			if (region != null)
			{
				xcomBaseRegions.Add(region.Rules.Type);
			}
			var country = save.LocateCountry(xcomBase);
			if (country != null)
			{
				xcomBaseCountries.Add(country.Rules.Type);
			}
		}
		foreach (var country in save.Countries)
		{
			if (country.Pact)
			{
				pactCountries.Add(country.Rules.Type);
			}
		}

		// sorry to interrupt, but before we start determining the actual monthly missions, let's determine and/or adjust our overall game plan
		if (isNewMonth)
		{
			var relevantArcScripts = new List<RuleArcScript>();

			// first we need to build a list of "valid" commands
			foreach (var scriptName in mod.ArcScriptList)
			{
				RuleArcScript arcScript = mod.GetArcScript(scriptName);

				// level one condition check: make sure we're within our time constraints
				if (arcScript.FirstMonth <= month &&
					(arcScript.LastMonth >= month || arcScript.LastMonth == -1) &&
					// and make sure we satisfy the difficulty restrictions
					(month < 1 || arcScript.MinScore <= currentScore) &&
					(month < 1 || arcScript.MaxScore >= currentScore) &&
					(month < 1 || arcScript.MinFunds <= currentFunds) &&
					(month < 1 || arcScript.MaxFunds >= currentFunds) &&
					arcScript.MinDifficulty <= (int)save.Difficulty &&
					arcScript.MaxDifficulty >= (int)save.Difficulty)
				{
					bool triggerHappy = CheckScriptTriggers(save, strategy, mod,
						arcScript.ResearchTriggers, arcScript.CounterMin, arcScript.CounterMax, arcScript.MissionVarName, arcScript.MissionMarkerName,
						arcScript.ItemTriggers, arcScript.FacilityTriggers, arcScript.SoldierTypeTriggers,
						arcScript.XcomBaseInRegionTriggers, arcScript.XcomBaseInCountryTriggers, arcScript.PactCountryTriggers,
						xcomBaseRegions, xcomBaseCountries, pactCountries);
					// level three condition check: does random chance favour this command's execution?
					if (triggerHappy && Rng.Percent(arcScript.ExecutionOdds))
					{
						relevantArcScripts.Add(arcScript);
					}
				}
			}

			// start processing command array
			foreach (var arcCommand in relevantArcScripts)
			{
				// to remember stuff we can still enable
				var disabledSeqArcs = new List<string>();
				WeightedOptions disabledRngArcs;

				int arcsEnabled = 0;
				// level four condition check: check maxArcs (duplicates count, arcs enabled by other commands or in any other way count too!)
				{
					foreach (var seqArc in arcCommand.SequentialArcs)
					{
						if (save.IsResearched(seqArc))
							++arcsEnabled;
						else
							disabledSeqArcs.Add(seqArc);
					}
					var tmp = new WeightedOptions(arcCommand.RandomArcs); // copy for the iterator, because of getNames()
					disabledRngArcs = new WeightedOptions(tmp); // copy for us to modify
					foreach (var rngArc in tmp.Names)
					{
						if (save.IsResearched(rngArc))
						{
							++arcsEnabled;
							disabledRngArcs.Set(rngArc, 0); // delete
						}
					}
				}
				Base hq = save.Bases[0];
				bool canAddOneMore = arcCommand.MaxArcs == -1 || arcCommand.MaxArcs > arcsEnabled;
				if (canAddOneMore && disabledSeqArcs.Count != 0)
				{
					var ruleResearchSeq = mod.GetResearch(disabledSeqArcs[0], true); // take first
					EnableArc(save, mod, hq, ruleResearchSeq);
					++arcsEnabled;
				}
				canAddOneMore = arcCommand.MaxArcs == -1 || arcCommand.MaxArcs > arcsEnabled;
				if (canAddOneMore && !disabledRngArcs.Empty())
				{
					var ruleResearchRng = mod.GetResearch(disabledRngArcs.Choose(), true); // take random
					EnableArc(save, mod, hq, ruleResearchRng);
					++arcsEnabled; // for good measure :)
				}
			}
		}

		// well, here it is, ladies and gents, the nuts and bolts behind the geoscape mission scheduling.
		List<string> scriptList = isNewMonth ? mod.MissionScriptList : mod.AdhocScriptList;

		// first we need to build a list of "valid" commands
		foreach (var missionScriptName in scriptList)
		{
			RuleMissionScript command = isNewMonth ? mod.GetMissionScript(missionScriptName) : mod.GetAdhocScript(missionScriptName);

			// level zero condition check: filter adhoc mission scripts by tags
			if (!isNewMonth && eventRules != null)
			{
				bool matchFound = false;
				foreach (var atag in eventRules.AdhocMissionScriptTags)
				{
					foreach (var btag in command.AdhocMissionScriptTags)
					{
						if (atag == btag) matchFound = true;
						break; // C++: only the first tag of the command is compared
					}
					if (matchFound) break;
				}
				if (!matchFound) continue;
			}

			// level one condition check: make sure we're within our time constraints
			if (command.FirstMonth <= month &&
				(command.LastMonth >= month || command.LastMonth == -1) &&
				// make sure we haven't hit our run limit, if we have one
				(command.MaxRuns == -1 || command.MaxRuns > strategy.GetMissionsRun(command.VarName)) &&
				// and make sure we satisfy the difficulty restrictions
				(month < 1 || command.MinScore <= currentScore) &&
				(month < 1 || command.MaxScore >= currentScore) &&
				(month < 1 || command.MinFunds <= currentFunds) &&
				(month < 1 || command.MaxFunds >= currentFunds) &&
				command.MinDifficulty <= (int)save.Difficulty &&
				command.MaxDifficulty >= (int)save.Difficulty)
			{
				bool triggerHappy = CheckScriptTriggers(save, strategy, mod,
					command.ResearchTriggers, command.CounterMin, command.CounterMax, command.MissionVarName, command.MissionMarkerName,
					command.ItemTriggers, command.FacilityTriggers, command.SoldierTypeTriggers,
					command.XcomBaseInRegionTriggers, command.XcomBaseInCountryTriggers, command.PactCountryTriggers,
					xcomBaseRegions, xcomBaseCountries, pactCountries);
				// levels one and two passed: insert this command into the array.
				if (triggerHappy)
				{
					availableMissions.Add(command);
				}
			}
		}

		// start processing command array.
		foreach (var command in availableMissions)
		{
			bool process = true;
			bool success = false;
			// level three condition check: make sure our conditionals are met, if any. this list is dynamic, and must be checked here.
			foreach (int condition in command.Conditionals)
			{
				if (!process)
				{
					break; // loop finished
				}
				bool found = conditions.TryGetValue(Math.Abs(condition), out bool foundValue);
				// just an FYI: if you add a 0 to your conditionals, this flag will never resolve to true, and your command will never run.
				process = !found || (foundValue && condition > 0) || (!foundValue && condition < 0);
			}
			if (command.Label > 0 && conditions.ContainsKey(command.Label))
			{
				var ss = new StringBuilder();
				ss.Append("Mission generator encountered an error: multiple commands: ").Append(command.Type).Append(" and ");
				foreach (var command2 in availableMissions)
				{
					if (command.Label == command2.Label && command2 != command)
					{
						ss.Append(command2.Type).Append(", ");
					}
				}
				ss.Append("are sharing the same label: ").Append(command.Label.ToString(CultureInfo.InvariantCulture));
				throw new InvalidOperationException(ss.ToString());
			}
			// level four condition check: does random chance favour this command's execution?
			if (process)
			{
				bool rngret = Rng.Percent(command.ExecutionOdds);
				if (Options.verboseLogging && Options.oxceGeoscapeDebugLogMaxEntries > 0)
				{
					save.GeoscapeDebugLog.Add(string.Create(CultureInfo.InvariantCulture,
						$"month: {month} script: {command.Type} odds: {command.ExecutionOdds} rng: {(rngret ? 1 : 0)}"));
				}
				if (rngret)
				{
					// good news, little command pointer! you're FDA approved! off to the main processing facility with you!
					success = ProcessCommand(command);
				}
			}
			if (command.Label > 0)
			{
				// tsk, tsk. you really should be careful with these unique labels, they're supposed to be unique.
				if (conditions.ContainsKey(command.Label))
				{
					throw new InvalidOperationException("Error in mission scripts: " + command.Type + ". Two or more commands sharing the same label. That's bad, Mmmkay?");
				}
				// keep track of what happened to this command, so others may reference it.
				conditions[command.Label] = success;
			}
		}

		// after the mission scripts, it's time for the event scripts
		if (isNewMonth)
		{
			var relevantEventScripts = new List<RuleEventScript>();

			// first we need to build a list of "valid" commands
			foreach (var scriptName in mod.EventScriptList)
			{
				RuleEventScript eventScript = mod.GetEventScript(scriptName);

				// level one condition check: make sure we're within our time constraints
				if (eventScript.FirstMonth <= month &&
					(eventScript.LastMonth >= month || eventScript.LastMonth == -1) &&
					// and make sure we satisfy the difficulty restrictions
					(month < 1 || eventScript.MinScore <= currentScore) &&
					(month < 1 || eventScript.MaxScore >= currentScore) &&
					(month < 1 || eventScript.MinFunds <= currentFunds) &&
					(month < 1 || eventScript.MaxFunds >= currentFunds) &&
					eventScript.MinDifficulty <= (int)save.Difficulty &&
					eventScript.MaxDifficulty >= (int)save.Difficulty)
				{
					bool triggerHappy = CheckScriptTriggers(save, strategy, mod,
						eventScript.ResearchTriggers, eventScript.CounterMin, eventScript.CounterMax, eventScript.MissionVarName, eventScript.MissionMarkerName,
						eventScript.ItemTriggers, eventScript.FacilityTriggers, eventScript.SoldierTypeTriggers,
						eventScript.XcomBaseInRegionTriggers, eventScript.XcomBaseInCountryTriggers, eventScript.PactCountryTriggers,
						xcomBaseRegions, xcomBaseCountries, pactCountries);
					// level three condition check: does random chance favour this command's execution?
					if (triggerHappy && Rng.Percent(eventScript.ExecutionOdds))
					{
						relevantEventScripts.Add(eventScript);
					}
				}
			}

			// now, let's process the relevant event scripts
			foreach (var eventCommand in relevantEventScripts)
			{
				var toBeGenerated = new List<RuleEvent>();

				// 1. sequentially generated one-time events (cannot repeat)
				{
					var possibleSeqEvents = new List<string>();
					foreach (var seqEvent in eventCommand.OneTimeSequentialEvents)
					{
						if (!save.WasEventGenerated(seqEvent))
							possibleSeqEvents.Add(seqEvent); // insert
					}
					if (possibleSeqEvents.Count != 0)
					{
						var seqEventRules = mod.GetEvent(possibleSeqEvents[0], true); // take first
						toBeGenerated.Add(seqEventRules);
					}
				}

				// 2. randomly generated one-time events (cannot repeat)
				{
					var tmp = new WeightedOptions(eventCommand.OneTimeRandomEvents); // copy for the iterator, because of getNames()
					var possibleRngEvents = new WeightedOptions(tmp); // copy for us to modify
					foreach (var rngEvent in tmp.Names)
					{
						if (save.WasEventGenerated(rngEvent))
							possibleRngEvents.Set(rngEvent, 0); // delete
					}
					if (!possibleRngEvents.Empty())
					{
						var rngEventRules = mod.GetEvent(possibleRngEvents.Choose(), true); // take random
						toBeGenerated.Add(rngEventRules);
					}
				}

				// 3. randomly generated repeatable events
				{
					var repeatableEventRules = mod.GetEvent(eventCommand.Generate(save.MonthsPassed), false);
					if (repeatableEventRules != null)
					{
						toBeGenerated.Add(repeatableEventRules);
					}
				}

				// 4. generate
				foreach (var toGenerate in toBeGenerated)
				{
					save.SpawnEvent(toGenerate);
				}
			}
		}

		// Alien base upgrades happen only AFTER the first game month
		if (isNewMonth && month > 0)
		{
			foreach (var alienBase in save.AlienBases)
			{
				int baseAgeInMonths = month - alienBase.StartMonth;
				string upgradeId = alienBase.Deployment.GenerateAlienBaseUpgrade(baseAgeInMonths);
				AlienDeployment upgrade = mod.GetDeployment(upgradeId, false);
				if (upgrade != null && upgrade != alienBase.Deployment)
				{
					var ss = new StringBuilder();
					ss.Append("month: ").Append(month.ToString(CultureInfo.InvariantCulture));
					ss.Append(" baseId: ").Append(alienBase.Id.ToString(CultureInfo.InvariantCulture));
					ss.Append(" baseType: ").Append(alienBase.Type);
					if (alienBase.Deployment.ResetAlienBaseAgeAfterUpgrade() || upgrade.ResetAlienBaseAge())
					{
						// reset base age to zero
						alienBase.StartMonth = month;
						ss.Append("; base age was reset;");
					}
					ss.Append(" old deployment: ").Append(alienBase.Deployment.Type);
					alienBase.Deployment = upgrade;
					ss.Append(" new deployment: ").Append(alienBase.Deployment.Type);
					var upgradeRace = mod.GetAlienRace(upgrade.UpgradeRace, false);
					if (upgradeRace != null)
					{
						ss.Append(" old race: ").Append(alienBase.AlienRace);
						alienBase.AlienRace = upgradeRace.Id;
						ss.Append(" new race: ").Append(alienBase.AlienRace);
					}
					if (Options.oxceGeoscapeDebugLogMaxEntries > 0)
					{
						save.GeoscapeDebugLog.Add(ss.ToString());
					}
				}
			}
		}

		// Alien race evolution
		if (isNewMonth && month > 0)
		{
			foreach (var alienBase in save.AlienBases)
			{
				if (alienBase.Deployment.AlienRaceEvolution.Count != 0)
				{
					var ss = new StringBuilder();
					ss.Append("month: ").Append(month.ToString(CultureInfo.InvariantCulture));
					ss.Append(" baseId: ").Append(alienBase.Id.ToString(CultureInfo.InvariantCulture));
					ss.Append(" baseType: ").Append(alienBase.Type);
					ss.Append(" deployment: ").Append(alienBase.Deployment.Type);
					ss.Append(" old race: ").Append(alienBase.AlienRace);
					int tries = 0;
					while (AttemptAlienRaceEvolution(month, alienBase))
					{
						ss.Append(" new race: ").Append(alienBase.AlienRace);
						if (++tries >= 100)
						{
							throw new InvalidOperationException("Alien race evolution: endless loop detected. It's not my fault. Crashing now!");
						}
					}
					ss.Append(" end.");
					if (Options.oxceGeoscapeDebugLogMaxEntries > 0)
					{
						save.GeoscapeDebugLog.Add(ss.ToString());
					}
				}
			}
		}
	}

	/// <summary>
	/// Try to perform alien race evolution.
	/// </summary>
	/// <returns>whether the attempt was successful or not.</returns>
	private bool AttemptAlienRaceEvolution(int month, AlienBase ab)
	{
		foreach (var tuple in ab.Deployment.AlienRaceEvolution)
		{
			if (tuple.Item1 <= month && tuple.Item2 == ab.AlienRace)
			{
				var newRace = _game.Mod.GetAlienRace(tuple.Item3, false);
				if (newRace != null)
				{
					ab.AlienRace = newRace.Id;
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Processes a directive to start up a mission, if possible.
	/// </summary>
	/// <param name="command">the directive from which to read information.</param>
	/// <returns>whether the command successfully produced a new mission.</returns>
	private bool ProcessCommand(RuleMissionScript command)
	{
		SavedGame save = _game.SavedGame;
		AlienStrategy strategy = save.AlienStrategy;
		Mod mod = _game.Mod;
		int month = _game.SavedGame.MonthsPassed;
		string targetRegion = "";
		RuleAlienMission missionRules;
		string missionType = "";
		string missionRace;
		int targetZoneNumber = -1;
		int targetAreaNumber = -1;

		// terror mission type deal? this will require special handling.
		if (command.SiteType)
		{
			// we know for a fact that this command has mission weights defined, otherwise this flag could not be set.
			missionType = command.Generate(month, GenerationType.GEN_MISSION);
			List<string> missions = command.GetMissionTypes(month);
			int maxMissions = missions.Count;
			bool targetBase = Rng.Percent(command.TargetBaseOdds);
			int currPos = 0;
			for (; currPos != maxMissions; ++currPos)
			{
				if (missions[currPos] == missionType)
				{
					break;
				}
			}

			// let's build a list of regions with spawn zones to pick from
			var validAreas = new List<(string first, int second)>();

			// this is actually a bit of a cheat, we ARE using the mission weights as defined, but we'll try them all if the one we pick first isn't valid.
			for (int h = 0; h != maxMissions; ++h)
			{
				// we'll use the regions listed in the command, if any, otherwise check all the regions in the ruleset looking for matches
				List<string> regions = command.HasRegionWeights ? command.GetRegions(month) : new List<string>(mod.RegionsList);
				missionRules = mod.GetAlienMission(missionType, true);
				targetZoneNumber = missionRules.SpawnZone;

				if (targetBase)
				{
					var regionsToKeep = new List<string>();
					//if we're targetting a base, we ignore regions that don't contain bases, simple.
					foreach (var xbase in save.Bases)
					{
						regionsToKeep.Add(save.LocateRegion(xbase.Longitude, xbase.Latitude).Rules.Type);
					}
					regions.RemoveAll(regionName => !regionsToKeep.Contains(regionName));
				}

				// -----------------------------------------------------------
				// Summary of mission site spawning algorithms (objective: 3)
				// -----------------------------------------------------------

				// Type 1:
				// - no UFOs involved
				// - only 1 wave
				// - the wave specifies the alien deployment directly (e.g. `ufo: STR_ARTIFACT_SITE_P1 # spawn this site directly`)
				// - example (1): STR_ALIEN_ARTIFACT (TFTD)
				// Support for non-point areas: yes, without any additional ruleset changes required
				MissionWave wave = missionRules.GetWave(0);
				bool spawnMissionSiteDirectly = mod.GetDeployment(wave.ufoType) != null && mod.GetUfo(wave.ufoType) == null && !string.IsNullOrEmpty(mod.GetDeployment(wave.ufoType).MarkerName);

				// Type 2:
				// - no UFOs involved
				// - only 1 wave
				// - the wave does NOT specify the alien deployment directly (e.g. `ufo: dummy #don't spawn a ufo, we only want the site`)
				//   -> option A: alien deployment is chosen randomly = from the area's texture definition
				//   -> option B: alien deployment is specified by the mission's `siteType` (overrides option A if both are defined)
				// - example (2A): STR_ALIEN_SHIP_ATTACK (TFTD)
				// - example (2B): none in vanilla, only mods
				// Support for non-point areas: yes, without any additional ruleset changes required

				// Type 3:
				// - with UFOs waves
				// - only 1 wave with `objective: true`
				// - the wave does NOT specify the alien deployment (because it already specifies the UFO type)
				//   -> option A: alien deployment is chosen randomly = from the area's texture definition
				//   -> option B: alien deployment is specified by the mission's `siteType` (overrides option A if both are defined)
				// - example (3A): STR_ALIEN_SURFACE_ATTACK (TFTD)
				// - example (3B): none in vanilla, only mods
				// Support for non-point areas: yes, but it is recommended to use one more wave attribute: `objectiveOnTheLandingSite: true`
				//   -> false: UFO always lands in the top-left corner of the area; site spawns randomly inside the area
				//   ->  true: UFO lands randomly inside the area; site spawns exactly on the UFO landing site
				bool spawnMissionSiteBySiteType = !string.IsNullOrEmpty(missionRules.SiteType);

				// -----------------------------------------------
				// End of the summary
				// -----------------------------------------------

				for (int regionNameIt = 0; regionNameIt < regions.Count;)
				{
					string regionName = regions[regionNameIt];
					// we don't want the same mission running in any given region twice simultaneously, so prune the list as needed.
					bool processThisRegion = true;
					foreach (var am in save.AlienMissions)
					{
						if (am.Rules.Type == missionRules.Type && am.Region == regionName)
						{
							processThisRegion = false;
							break;
						}
					}
					if (!processThisRegion)
					{
						regions.RemoveAt(regionNameIt);
						continue;
					}
					// ok, we found a region that doesn't have our mission in it, let's see if it has an appropriate landing zone.
					// if it does, let's add it to our list of valid areas, taking note of which mission area(s) matched.
					RuleRegion region = mod.GetRegion(regionName, true);
					if (region.MissionZones.Count > targetZoneNumber)
					{
						if (targetZoneNumber < 0)
						{
							throw new InvalidOperationException("Cannot spawn mission site, invalid spawn zone! Command: " + command.Type + "; mission: " + missionRules.Type);
						}
						List<MissionArea> areas = region.MissionZones[targetZoneNumber].areas;
						int counter = 0;
						foreach (var area in areas)
						{
							// validMissionLocation checks to make sure this city/whatever hasn't been used by the last n missions using this varName
							// this prevents the same location getting hit more than once every n missions.
							if (area.IsPoint && strategy.ValidMissionLocation(command.VarName, region.Type, counter))
							{
								validAreas.Add((region.Type, counter));
							}
							else if (!area.IsPoint && (area.texture < 0 || spawnMissionSiteBySiteType || spawnMissionSiteDirectly))
							{
								validAreas.Add((region.Type, counter));
							}
							counter++;
						}
					}
					++regionNameIt;
				}

				// oh bother, we couldn't find anything valid, this mission won't run this month.
				if (validAreas.Count == 0)
				{
					if (maxMissions > 1 && ++currPos == maxMissions)
					{
						currPos = 0;
					}
					missionType = missions[currPos];
				}
				else
				{
					break;
				}
			}

			if (validAreas.Count == 0)
			{
				// now we're in real trouble, we've managed to make it out of the loop and we still don't have any valid choices
				// this command cannot run this month, we have failed, forgive us senpai.
				return false;
			}

			// everything went according to plan: we can now pick a city/whatever to attack.
			while (targetAreaNumber == -1)
			{
				if (command.HasRegionWeights)
				{
					// if we have a weighted region list, we know we have at least one valid choice for this mission
					targetRegion = command.Generate(month, GenerationType.GEN_REGION);
				}
				else
				{
					// if we don't have a weighted list, we'll select a region at random from the ruleset,
					// validate that it's in our list, and pick one of its cities at random
					// this will give us an even distribution between regions regardless of the number of cities.
					targetRegion = mod.RegionsList[Rng.Generate(0, mod.RegionsList.Count - 1)];
				}

				// we need to know the range of the region within our vector, in order to randomly select a city from it
				int min = -1;
				int max = -1;
				int curr = 0;
				foreach (var pair in validAreas)
				{
					if (pair.first == targetRegion)
					{
						if (min == -1)
						{
							min = curr;
						}
						max = curr;
					}
					else if (min > -1)
					{
						// if we've stopped detecting matches, we're done looking.
						break;
					}
					++curr;
				}
				if (min != -1)
				{
					// we have our random range, we can make a selection, and we're done.
					targetAreaNumber = validAreas[Rng.Generate(min, max)].second;
				}
			}
			// now add that city to the list of sites we've hit, store the array, etc.
			strategy.AddMissionLocation(command.VarName, targetRegion, targetAreaNumber, command.RepeatAvoidance);
		}
		else if (Rng.Percent(command.TargetBaseOdds))
		{
			// build a list of the mission types we're dealing with, if any
			List<string> types = command.GetMissionTypes(month);
			// now build a list of regions with bases in.
			var regionsMaster = new List<string>();
			foreach (var xbase in save.Bases)
			{
				regionsMaster.Add(save.LocateRegion(xbase).Rules.Type);
			}
			// no defined mission types? then we'll prune the region list to ensure we only have a region that can generate a mission.
			if (types.Count == 0)
			{
				regionsMaster.RemoveAll(regionName => !strategy.ValidMissionRegion(regionName));
				// no valid missions in any base regions? oh dear, i guess we failed.
				if (regionsMaster.Count == 0)
				{
					return false;
				}
				// pick a random region from our list
				targetRegion = regionsMaster[Rng.Generate(0, regionsMaster.Count - 1)];
			}
			else
			{
				// we don't care about regional mission distributions, we're targetting a base with whatever mission we pick, so let's pick now
				// we'll iterate the mission list, starting at a random point, and wrapping around to the beginning
				int max = types.Count;
				int entry = Rng.Generate(0, max - 1);
				List<string> regions;

				for (int i = 0; i != max; ++i)
				{
					regions = new List<string>(regionsMaster);
					foreach (var am in save.AlienMissions)
					{
						// if the mission types match
						if (types[entry] == am.Rules.Type)
						{
							// and the regions match: prune the entry from the list
							regions.RemoveAll(regionName => regionName == am.Region);
						}
					}

					// we have a valid list of regions containing bases, pick one.
					if (regions.Count != 0)
					{
						missionType = types[entry];
						targetRegion = regions[Rng.Generate(0, regions.Count - 1)];
						break;
					}
					// otherwise, try the next mission in the list.
					if (max > 1 && ++entry == max)
					{
						entry = 0;
					}
				}
			}
		}
		// now the easy stuff
		else if (!command.HasRegionWeights)
		{
			// no regionWeights means we pick from the table
			targetRegion = strategy.ChooseRandomRegion(mod);
		}
		else
		{
			// otherwise, let the command dictate the region.
			targetRegion = command.Generate(month, GenerationType.GEN_REGION);
		}

		if (string.IsNullOrEmpty(targetRegion))
		{
			// something went horribly wrong, we should have had at LEAST a region by now.
			return false;
		}

		// we're bound to end up with typos, so let's throw an exception instead of simply returning false
		// that way, the modder can fix their mistake
		if (mod.GetRegion(targetRegion) == null)
		{
			throw new InvalidOperationException("Error processing mission script named: " + command.Type + ", region named: " + targetRegion + " is not defined");
		}

		if (string.IsNullOrEmpty(missionType)) // ie: not a terror mission, not targetting a base, or otherwise not already chosen
		{
			if (!command.HasMissionWeights)
			{
				// no weights means let the strategy pick
				missionType = strategy.ChooseRandomMission(targetRegion);
			}
			else
			{
				// otherwise the command gives us the weights.
				missionType = command.Generate(month, GenerationType.GEN_MISSION);
			}
		}

		if (string.IsNullOrEmpty(missionType))
		{
			// something went horribly wrong, we didn't manage to choose a mission type
			return false;
		}

		missionRules = mod.GetAlienMission(missionType);

		// we're bound to end up with typos, so let's throw an exception instead of simply returning false
		// that way, the modder can fix their mistake
		if (missionRules == null)
		{
			throw new InvalidOperationException("Error processing mission script named: " + command.Type + ", mission type: " + missionType + " is not defined");
		}

		// do i really need to comment this? shouldn't it be obvious what's happening here?
		if (!command.HasRaceWeights)
		{
			missionRace = missionRules.GenerateRace(month);
		}
		else
		{
			missionRace = command.Generate(month, GenerationType.GEN_RACE);
		}

		if (string.IsNullOrEmpty(missionRace))
		{
			throw new InvalidOperationException("Error processing mission script named: " + command.Type + ", mission type: " + missionType + " has no available races");
		}

		// we're bound to end up with typos, so let's throw an exception instead of simply returning false
		// that way, the modder can fix their mistake
		if (mod.GetAlienRace(missionRace) == null)
		{
			throw new InvalidOperationException("Error processing mission script named: " + command.Type + ", race: " + missionRace + " is not defined");
		}

		// ok, we've derived all the variables we need to start up our mission, let's do magic to turn those values into a mission
		var mission = new AlienMission(missionRules);
		mission.Race = missionRace;
		mission.Id = _game.SavedGame.GetId("ALIEN_MISSIONS");
		mission.SetRegion(targetRegion, _game.Mod);
		mission.SetMissionSiteZoneArea(targetAreaNumber);
		strategy.AddMissionRun(command.VarName);
		mission.Start(_game, _globe, command.Delay);
		_game.SavedGame.AlienMissions.Add(mission);
		// if this flag is set, we want to delete it from the table so it won't show up again until the schedule resets.
		if (command.UseTable)
		{
			strategy.RemoveMission(targetRegion, missionType);
		}

		if (Options.oxceGeoscapeDebugLogMaxEntries > 0)
		{
			save.GeoscapeDebugLog.Add(string.Create(CultureInfo.InvariantCulture,
				$"month: {month} script: {command.Type} id: {mission.Id} type: {mission.Rules.Type} race: {mission.Race} region: {targetRegion} targetZone: {targetZoneNumber} targetArea: {targetAreaNumber}"));
		}

		// we did it, we can go home now.
		return true;
	}

	/// <summary>
	/// Handler for clicking on a timer button.
	/// </summary>
	/// <param name="action">pointer to the mouse action.</param>
	public void BtnTimerClick(Action action)
	{
		var ev = new SDL_Event();
		ev.type = SDLEventType.SDL_MOUSEBUTTONDOWN;
		ev.button.button = SDLMouse.SDL_BUTTON_LEFT;
		var a = new Action(ev, 0.0, 0.0, 0, 0);
		action.Sender.MousePress(a, this);
	}

	/// <summary>
	/// Updates the scale.
	/// </summary>
	/// <param name="dX">delta of X;</param>
	/// <param name="dY">delta of Y;</param>
	public override void Resize(ref int dX, ref int dY)
	{
		if (_game.SavedGame.SavedBattle != null)
			return;
		dX = Options.baseXResolution;
		dY = Options.baseYResolution;
		int divisor = 1;
		double pixelRatioY = 1.0;

		if (Options.nonSquarePixelRatio)
		{
			pixelRatioY = 1.2;
		}
		switch ((ScaleType)Options.geoscapeScale)
		{
			case ScaleType.SCALE_SCREEN_DIV_10:
				divisor = 10;
				break;
			case ScaleType.SCALE_SCREEN_DIV_8:
				divisor = 8;
				break;
			case ScaleType.SCALE_SCREEN_DIV_6:
				divisor = 6;
				break;
			case ScaleType.SCALE_SCREEN_DIV_5:
				divisor = 5;
				break;
			case ScaleType.SCALE_SCREEN_DIV_4:
				divisor = 4;
				break;
			case ScaleType.SCALE_SCREEN_DIV_3:
				divisor = 3;
				break;
			case ScaleType.SCALE_SCREEN_DIV_2:
				divisor = 2;
				break;
			case ScaleType.SCALE_SCREEN:
				break;
			default:
				dX = 0;
				dY = 0;
				return;
		}

		Options.baseXResolution = Math.Max(Screen.ORIGINAL_WIDTH, Options.displayWidth / divisor);
		Options.baseYResolution = Math.Max(Screen.ORIGINAL_HEIGHT, (int)(Options.displayHeight / pixelRatioY / divisor));

		dX = Options.baseXResolution - dX;
		dY = Options.baseYResolution - dY;

		_globe.Resize();

		foreach (var surface in _surfaces)
		{
			if (surface != _globe)
			{
				surface.X = surface.X + dX;
				surface.Y = surface.Y + dY / 2;
			}
		}

		_bg.X = (_globe.Width - _bg.Width) / 2;
		_bg.Y = (_globe.Height - _bg.Height) / 2;

		int height = (Options.baseYResolution - Screen.ORIGINAL_HEIGHT) / 2 + 10;
		_sideTop.SetHeight(height);
		_sideTop.Y = _sidebar.Y - height - 1;
		_sideBottom.SetHeight(height);
		_sideBottom.Y = _sidebar.Y + _sidebar.Height + 1;

		_sideLine.SetHeight(Options.baseYResolution);
		_sideLine.Y = 0;
		_sideLine.DrawRect(0, 0, (short)_sideLine.Width, (short)_sideLine.Height, 15);
	}

	private bool ButtonsDisabled()
	{
		return _zoomInEffectTimer.IsRunning || _zoomOutEffectTimer.IsRunning;
	}

	private void UpdateSlackingIndicator()
	{
		if (Options.oxceGeoEnableTrainingIndicator)
		{
			int freeGym = 0;
			int freePsi = 0;
			foreach (var xcomBase in _game.SavedGame.Bases)
			{
				int facilityGym = xcomBase.FreeTrainingSpace;
				if (facilityGym > 0)
				{
					int soldGym = 0;
					foreach (var soldier in xcomBase.Soldiers)
					{
						bool isTraining = soldier.IsInTraining;
						bool isQueued = !isTraining && soldier.ReturnToTrainingWhenHealed;
						bool isDone = soldier.IsFullyTrained;

						if (isTraining || isQueued || isDone)
						{
							// ignore this guy
						}
						else
						{
							// can train, or can be queued for training
							soldGym++;
						}
						if (soldGym >= facilityGym) break;
					}
					freeGym += soldGym;
				}

				int facilityPsi = xcomBase.FreePsiLabs;
				if (facilityPsi > 0)
				{
					int soldPsi = 0;
					foreach (var soldier in xcomBase.Soldiers)
					{
						bool isTraining = soldier.IsInPsiTraining;
						bool isDone = soldier.IsFullyPsiTrained;
						bool isNotEligible = soldier.Rules.TrainingStatCaps.psiSkill <= 0;

						if (isTraining || isDone || isNotEligible)
						{
							// ignore this guy
						}
						else
						{
							// can train
							soldPsi++;
						}
						if (soldPsi >= facilityPsi) break;
					}
					freePsi += soldPsi;
				}
			}
			if (freeGym > 0 || freePsi > 0)
			{
				_txtTraining.SetText(Tr("STR_TRAINING_INDICATOR").Arg(freePsi).Arg(freeGym));
			}
			else
			{
				_txtTraining.SetText("");
			}
		}

		if (!Options.oxceEnableSlackingIndicator)
			return;

		int scientistsSlacking = 0;
		int engineersSlacking = 0;
		foreach (var xcomBase in _game.SavedGame.Bases)
		{
			scientistsSlacking += xcomBase.AvailableScientists;
			engineersSlacking += xcomBase.AvailableEngineers;
		}
		if (scientistsSlacking > 0 || engineersSlacking > 0)
		{
			_txtSlacking.SetText(Tr("STR_SLACKING_INDICATOR").Arg(scientistsSlacking).Arg(engineersSlacking));
		}
		else
		{
			_txtSlacking.SetText("");
		}
	}

	private void CbxRegionChange(Action action)
	{
		int index = _cbxRegion.Selected;
		if (index < 1)
		{
			_game.SavedGame.debugRegion = null;
		}
		else
		{
			_game.SavedGame.debugRegion = _game.SavedGame.Regions[index - 1];
		}
		UpdateZoneInfo();
	}

	private void CbxZoneChange(Action action)
	{
		_game.SavedGame.debugZone = _cbxZone.Selected;
		UpdateZoneInfo();
	}

	private void CbxAreaChange(Action action)
	{
		_game.SavedGame.debugArea = _cbxArea.Selected;
		UpdateZoneInfo();
	}

	private void UpdateZoneInfo()
	{
		var ss = new StringBuilder();
		var save = _game.SavedGame;
		if (save.debugRegion != null)
		{
			var regionRule = save.debugRegion.Rules;
			if (save.debugType >= 1)
			{
				ss.Append("region: ").Append(Tr(regionRule.Type)).Append(" [").Append(regionRule.Type).Append(']').Append('\n');
			}
			if (save.debugType == 2)
			{
				// C++ compares the int with size_t: a negative selection never matches
				if (save.debugZone > 0 && save.debugZone <= regionRule.MissionZones.Count)
				{
					var selectedZone = regionRule.MissionZones[save.debugZone - 1];
					ss.Append("zone: ").Append((save.debugZone - 1).ToString(CultureInfo.InvariantCulture)).Append('\n');
					if (save.debugArea > 0 && save.debugArea <= selectedZone.areas.Count)
					{
						var selectedArea = selectedZone.areas[save.debugArea - 1];
						ss.Append("area: ").Append((save.debugArea - 1).ToString(CultureInfo.InvariantCulture));
						ss.Append(", texture: ").Append(selectedArea.texture.ToString(CultureInfo.InvariantCulture));
						ss.Append(", name: ").Append(Tr(selectedArea.name)).Append(" [").Append(selectedArea.name).Append(']').Append('\n');
						if (selectedArea.IsPoint)
						{
							ss.Append("point = [").Append(Str(selectedArea.lonMin / Math.PI * 180)).Append(", ").Append(Str(selectedArea.latMin / Math.PI * 180)).Append(']');
							int texture = 0, shade = 0;
							_globe.GetPolygonTextureAndShade(selectedArea.lonMin, selectedArea.latMin, ref texture, ref shade);
							ss.Append(", globe texture: ").Append(texture.ToString(CultureInfo.InvariantCulture));
						}
						else
						{
							ss.Append("rect = [").Append(Str(selectedArea.lonMin / Math.PI * 180)).Append(", ").Append(Str(selectedArea.lonMax / Math.PI * 180));
							ss.Append(", ").Append(Str(selectedArea.latMin / Math.PI * 180)).Append(", ").Append(Str(selectedArea.latMax / Math.PI * 180)).Append(']');
						}
					}
					else
					{
						ss.Append("total areas: ").Append(selectedZone.areas.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
					}
				}
				else
				{
					ss.Append("total zones: ").Append(regionRule.MissionZones.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
				}
			}
		}
		_txtDebug.SetText(ss.ToString());
	}

	private void CbxCountryChange(Action action)
	{
		int index = _cbxCountry.Selected;
		if (index < 1)
		{
			_game.SavedGame.debugCountry = null;
		}
		else
		{
			_game.SavedGame.debugCountry = _game.SavedGame.Countries[index - 1];
		}

		var ss = new StringBuilder();
		var save = _game.SavedGame;
		if (save.debugCountry != null)
		{
			var countryRule = save.debugCountry.Rules;
			if (save.debugType == 0)
			{
				ss.Append("country: ").Append(Tr(countryRule.Type)).Append(" [").Append(countryRule.Type).Append(']').Append('\n');
			}
		}
		_txtDebug.SetText(ss.ToString());
	}
}
