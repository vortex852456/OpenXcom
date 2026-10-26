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
// Ported from: src/Basescape/BasescapeState.h, src/Basescape/BasescapeState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Basescape screen that shows a base's layout
/// and lets the player manage their bases.
/// </summary>
/// <remarks>
/// The C++ destructor deletes a temporary base that is not in the saved game; the GC does that here.
/// </remarks>
public class BasescapeState : State
{
	private readonly BaseView _view;
	private readonly MiniBaseView _mini;
	private readonly Text _txtFacility, _txtLocation, _txtFunds;
	private readonly TextEdit _edtBase;
	private readonly TextButton _btnNewBase, _btnBaseInfo, _btnSoldiers, _btnCrafts, _btnFacilities, _btnResearch, _btnManufacture, _btnTransfer, _btnPurchase, _btnSell, _btnGeoscape;
	private Base _base;
	private readonly Globe _globe;

	/// <summary>
	/// Initializes all the elements in the Basescape screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	public BasescapeState(Base @base, Globe globe)
	{
		_base = @base;
		_globe = globe;

		// Create objects
		_txtFacility = new Text(192, 9, 0, 0);
		_view = new BaseView(192, 192, 0, 8);
		_mini = new MiniBaseView(128, 16, 192, 41);
		_edtBase = new TextEdit(this, 127, 17, 193, 0);
		_txtLocation = new Text(126, 9, 194, 16);
		_txtFunds = new Text(126, 9, 194, 24);
		_btnNewBase = new TextButton(128, 12, 192, 58);
		_btnBaseInfo = new TextButton(128, 12, 192, 71);
		_btnSoldiers = new TextButton(128, 12, 192, 84);
		_btnCrafts = new TextButton(128, 12, 192, 97);
		_btnFacilities = new TextButton(128, 12, 192, 110);
		_btnResearch = new TextButton(128, 12, 192, 123);
		_btnManufacture = new TextButton(128, 12, 192, 136);
		_btnTransfer = new TextButton(128, 12, 192, 149);
		_btnPurchase = new TextButton(128, 12, 192, 162);
		_btnSell = new TextButton(128, 12, 192, 175);
		_btnGeoscape = new TextButton(128, 12, 192, 188);

		// Set palette
		SetInterface("basescape");

		Add(_view, "baseView", "basescape");
		Add(_mini, "miniBase", "basescape");
		Add(_txtFacility, "textTooltip", "basescape");
		Add(_edtBase, "text1", "basescape");
		Add(_txtLocation, "text2", "basescape");
		Add(_txtFunds, "text3", "basescape");
		Add(_btnNewBase, "button", "basescape");
		Add(_btnBaseInfo, "button", "basescape");
		Add(_btnSoldiers, "button", "basescape");
		Add(_btnCrafts, "button", "basescape");
		Add(_btnFacilities, "button", "basescape");
		Add(_btnResearch, "button", "basescape");
		Add(_btnManufacture, "button", "basescape");
		Add(_btnTransfer, "button", "basescape");
		Add(_btnPurchase, "button", "basescape");
		Add(_btnSell, "button", "basescape");
		Add(_btnGeoscape, "button", "basescape");

		CenterAllSurfaces();

		// Set up objects
		if (_globe != null)
		{
			foreach (var xbase in _game.SavedGame.Bases)
			{
				int texture = 0, shade = 0;
				_globe.GetPolygonTextureAndShade(xbase.Longitude, xbase.Latitude, ref texture, ref shade);
				var globeTexture = _game.Mod.Globe.GetTexture(texture);
				xbase.GlobeTexture = globeTexture;
			}
		}

		var itf = _game.Mod.GetInterface("basescape").GetElementOptional("trafficLights");
		if (itf != null)
		{
			_view.SetOtherColors((byte)itf.color, (byte)itf.color2, (byte)itf.border, !itf.TFTDMode);
		}
		_view.SetTexture(_game.Mod.GetSurfaceSet("BASEBITS.PCK"));
		_view.OnMouseClick(ViewLeftClick, SDLMouse.SDL_BUTTON_LEFT);
		_view.OnMouseClick(ViewRightClick, SDLMouse.SDL_BUTTON_RIGHT);
		_view.OnMouseClick(ViewMiddleClick, SDLMouse.SDL_BUTTON_MIDDLE);
		_view.OnMouseOver(ViewMouseOver);
		_view.OnMouseOut(ViewMouseOut);

		_mini.SetTexture(_game.Mod.GetSurfaceSet("BASEBITS.PCK"));
		_mini.SetBases(_game.SavedGame.Bases);
		_mini.OnMouseClick(MiniLeftClick, SDLMouse.SDL_BUTTON_LEFT);
		_mini.OnMouseClick(MiniRightClick, SDLMouse.SDL_BUTTON_RIGHT);
		_mini.OnKeyboardPress(HandleKeyPress);

		_edtBase.SetBig();
		_edtBase.OnChange(EdtBaseChange);

		_btnNewBase.Text = Tr("STR_BUILD_NEW_BASE_UC");
		_btnNewBase.OnMouseClick(BtnNewBaseClick);
		_btnNewBase.OnKeyboardPress(BtnNewBaseClick, Options.keyBasescapeBuildNewBase);

		_btnBaseInfo.Text = Tr("STR_BASE_INFORMATION");
		_btnBaseInfo.OnMouseClick(BtnBaseInfoClick);
		_btnBaseInfo.OnKeyboardPress(BtnBaseInfoClick, Options.keyBasescapeBaseInfo);

		_btnSoldiers.Text = Tr("STR_SOLDIERS_UC");
		_btnSoldiers.OnMouseClick(BtnSoldiersClick);
		_btnSoldiers.OnKeyboardPress(BtnSoldiersClick, Options.keyBasescapeSoldiers);

		_btnCrafts.Text = Tr("STR_EQUIP_CRAFT");
		_btnCrafts.OnMouseClick(BtnCraftsClick);
		_btnCrafts.OnKeyboardPress(BtnCraftsClick, Options.keyBasescapeCrafts);

		_btnFacilities.Text = Tr("STR_BUILD_FACILITIES");
		_btnFacilities.OnMouseClick(BtnFacilitiesClick);
		_btnFacilities.OnKeyboardPress(BtnFacilitiesClick, Options.keyBasescapeFacilities);

		_btnResearch.Text = Tr("STR_RESEARCH");
		_btnResearch.OnMouseClick(BtnResearchClick);
		_btnResearch.OnKeyboardPress(BtnResearchClick, Options.keyBasescapeResearch);

		_btnManufacture.Text = Tr("STR_MANUFACTURE");
		_btnManufacture.OnMouseClick(BtnManufactureClick);
		_btnManufacture.OnKeyboardPress(BtnManufactureClick, Options.keyBasescapeManufacture);

		_btnTransfer.Text = Tr("STR_TRANSFER_UC");
		_btnTransfer.OnMouseClick(BtnTransferClick);
		_btnTransfer.OnKeyboardPress(BtnTransferClick, Options.keyBasescapeTransfer);

		_btnPurchase.Text = Tr("STR_PURCHASE_RECRUIT");
		_btnPurchase.OnMouseClick(BtnPurchaseClick);
		_btnPurchase.OnKeyboardPress(BtnPurchaseClick, Options.keyBasescapePurchase);

		_btnSell.Text = Tr("STR_SELL_SACK_UC");
		_btnSell.OnMouseClick(BtnSellClick);
		_btnSell.OnKeyboardPress(BtnSellClick, Options.keyBasescapeSell);

		_btnGeoscape.Text = Tr("STR_GEOSCAPE_UC");
		_btnGeoscape.OnMouseClick(BtnGeoscapeClick);
		_btnGeoscape.OnKeyboardPress(BtnGeoscapeClick, Options.keyCancel);
	}

	/// <summary>
	/// The player can change the selected base
	/// or change info on other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();

		SetBase(_base);
		_view.SetBase(_base);
		_mini.Draw();
		_edtBase.Text = _base.GetName(null);

		// Get area
		foreach (var region in _game.SavedGame.Regions)
		{
			if (region.Rules.InsideRegion(_base.Longitude, _base.Latitude))
			{
				_txtLocation.SetText(Tr(region.Rules.Type));
				break;
			}
		}

		_txtFunds.SetText(Tr("STR_FUNDS").Arg(Unicode.FormatFunding(_game.SavedGame.Funds)));

		_btnNewBase.Visible = _game.SavedGame.Bases.Count < MiniBaseView.MAX_BASES;

		if (!string.IsNullOrEmpty(_game.Mod.NewBaseUnlockResearch))
		{
			bool newBasesUnlocked = _game.SavedGame.IsResearched(_game.Mod.NewBaseUnlockResearch, true);
			if (!newBasesUnlocked)
			{
				_btnNewBase.Visible = false;
			}
		}
	}

	/// <summary>
	/// Changes the base currently displayed on screen.
	/// </summary>
	public void SetBase(Base @base)
	{
		var bases = _game.SavedGame.Bases;
		if (bases.Count != 0)
		{
			// Check if base still exists
			bool exists = false;
			for (int i = 0; i < bases.Count; ++i)
			{
				if (bases[i] == @base)
				{
					_base = @base;
					_mini.SetSelectedBase(i);
					_game.SavedGame.SetSelectedBase(i);
					exists = true;
					break;
				}
			}
			// If base was removed, select first one
			if (!exists)
			{
				_base = bases[0];
				_mini.SetSelectedBase(0);
				_game.SavedGame.SetSelectedBase(0);
			}
		}
		else
		{
			// Use a blank base for special case when player has no bases
			_base = new Base(_game.Mod);
			_mini.SetSelectedBase(0);
			_game.SavedGame.SetSelectedBase(0);
		}
	}

	/// <summary>Goes to the Build New Base screen.</summary>
	public void BtnNewBaseClick(Action action)
	{
		Base @base = new Base(_game.Mod);
		_game.PopState();
		_game.PushState(new BuildNewBaseState(@base, _globe, false));
	}

	/// <summary>Goes to the Base Info screen.</summary>
	public void BtnBaseInfoClick(Action action)
	{
		_game.PushState(new BaseInfoState(_base, this));
	}

	/// <summary>Goes to the Soldiers screen.</summary>
	public void BtnSoldiersClick(Action action)
	{
		_game.PushState(new SoldiersState(_base));
	}

	/// <summary>Goes to the Crafts screen.</summary>
	public void BtnCraftsClick(Action action)
	{
		_game.PushState(new CraftsState(_base));
	}

	/// <summary>Opens the Build Facilities window.</summary>
	public void BtnFacilitiesClick(Action action)
	{
		_game.PushState(new BuildFacilitiesState(_base, this));
	}

	/// <summary>Goes to the Research screen.</summary>
	public void BtnResearchClick(Action action)
	{
		_game.PushState(new ResearchState(_base));
	}

	/// <summary>Goes to the Manufacture screen.</summary>
	public void BtnManufactureClick(Action action)
	{
		_game.PushState(new ManufactureState(_base));
	}

	/// <summary>Goes to the Purchase screen.</summary>
	public void BtnPurchaseClick(Action action)
	{
		_game.PushState(new PurchaseState(_base));
	}

	/// <summary>Goes to the Sell screen.</summary>
	public void BtnSellClick(Action action)
	{
		_game.PushState(new SellState(_base, null));
	}

	/// <summary>Goes to the Select Destination Base window.</summary>
	public void BtnTransferClick(Action action)
	{
		_game.PushState(new TransferBaseState(_base, null));
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnGeoscapeClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Processes clicking on facilities.</summary>
	public void ViewLeftClick(Action action)
	{
		BaseFacility fac = _view.SelectedFacility;
		if (fac != null)
		{
			if (_game.IsCtrlPressed() && Options.IsPasswordCorrect())
			{
				// Ctrl + left click on a base facility allows moving it
				_game.PushState(new PlaceFacilityState(_base, fac.Rules, fac));
			}
			else
			{
				if (fac.Rules.IsLift && _base.Facilities.Count > 1)
				{
					// Note: vehicles will not be deployed in the base preview
					if (_base.GetAvailableSoldiers(true, true) > 0/* || !_base->getVehicles()->empty()*/)
					{
						int texture = 0, shade = 0;
						_globe.GetPolygonTextureAndShade(_base.Longitude, _base.Latitude, ref texture, ref shade);
						var globeTexture = _game.Mod.Globe.GetTexture(texture);

						var bgame = new SavedBattleGame(_game.Mod, _game.Language, true);
						_game.SavedGame.SetBattleGame(bgame);
						var bgen = new BattlescapeGenerator(_game);
						bgame.MissionType = "STR_BASE_DEFENSE";
						bgen.SetBase(_base);
						bgen.SetWorldTexture(globeTexture, globeTexture);
						bgen.Run();

						_game.PushState(new BriefingState(null, _base));
					}
					return;
				}
				byte errorColor1 = (byte)_game.Mod.GetInterface("basescape").GetElement("errorMessage").color;
				int errorColor2 = _game.Mod.GetInterface("basescape").GetElement("errorPalette").color;
				// Is facility in use?
				BasePlacementErrors placementErrorCode = fac.InUse();
				if (placementErrorCode != BasePlacementErrors.BPE_None)
				{
					switch (placementErrorCode)
					{
					case BasePlacementErrors.BPE_Used_Stores:
						_game.PushState(new ErrorMessageState(Tr("STR_FACILITY_IN_USE_STORAGE"), _palette, errorColor1, "BACK13.SCR", errorColor2));
						break;
					case BasePlacementErrors.BPE_Used_Quarters:
						_game.PushState(new ErrorMessageState(Tr("STR_FACILITY_IN_USE_QUARTERS"), _palette, errorColor1, "BACK13.SCR", errorColor2));
						break;
					case BasePlacementErrors.BPE_Used_Laboratories:
						_game.PushState(new ErrorMessageState(Tr("STR_FACILITY_IN_USE_LABORATORIES"), _palette, errorColor1, "BACK13.SCR", errorColor2));
						break;
					case BasePlacementErrors.BPE_Used_Workshops:
						_game.PushState(new ErrorMessageState(Tr("STR_FACILITY_IN_USE_WORKSHOPS"), _palette, errorColor1, "BACK13.SCR", errorColor2));
						break;
					case BasePlacementErrors.BPE_Used_Hangars:
						_game.PushState(new ErrorMessageState(Tr("STR_FACILITY_IN_USE_HANGARS"), _palette, errorColor1, "BACK13.SCR", errorColor2));
						break;
					case BasePlacementErrors.BPE_Used_PsiLabs:
						_game.PushState(new ErrorMessageState(Tr("STR_FACILITY_IN_USE_PSI_LABS"), _palette, errorColor1, "BACK13.SCR", errorColor2));
						break;
					case BasePlacementErrors.BPE_Used_Gyms:
						_game.PushState(new ErrorMessageState(Tr("STR_FACILITY_IN_USE_GYMS"), _palette, errorColor1, "BACK13.SCR", errorColor2));
						break;
					case BasePlacementErrors.BPE_Used_AlienContainment:
						_game.PushState(new ErrorMessageState(Tr("STR_FACILITY_IN_USE_PRISONS"), _palette, errorColor1, "BACK13.SCR", errorColor2));
						break;
					default:
						_game.PushState(new ErrorMessageState(Tr("STR_FACILITY_IN_USE"), _palette, errorColor1, "BACK13.SCR", errorColor2));
						break;
					}
				}
				// Would base become disconnected?
				else if (_base.GetDisconnectedFacilities(fac).Count != 0 && fac.Rules.LeavesBehindOnSell.Count == 0)
				{
					_game.PushState(new ErrorMessageState(Tr("STR_CANNOT_DISMANTLE_FACILITY"), _palette, errorColor1, "BACK13.SCR", errorColor2));
				}
				// Is this facility being built from a dismantled one or building over a previous building?
				else if (fac.BuildTime > 0 && fac.IfHadPreviousFacility)
				{
					_game.PushState(new ErrorMessageState(Tr("STR_CANNOT_DISMANTLE_FACILITY_UPGRADING"), _palette, errorColor1, "BACK13.SCR", errorColor2));
				}
				else
				{
					_game.PushState(new DismantleFacilityState(_base, _view, fac));
				}
			}
		}
	}

	/// <summary>Processes right clicking on facilities.</summary>
	public void ViewRightClick(Action action)
	{
		BaseFacility f = _view.SelectedFacility;
		if (f == null)
		{
			_game.PushState(new BaseInfoState(_base, this));
		}
		else if (f.Rules.RightClickActionType != 0)
		{
			switch (f.Rules.RightClickActionType)
			{
				case 1: _game.PushState(new ManageAlienContainmentState(_base, f.Rules.PrisonType, OptionsOrigin.OPT_GEOSCAPE)); break;
				case 2: _game.PushState(new ManufactureState(_base)); break;
				case 3: _game.PushState(new ResearchState(_base)); break;
				case 4: _game.PushState(new AllocateTrainingState(_base)); break;
				case 5: if (Options.anytimePsiTraining) _game.PushState(new AllocatePsiTrainingState(_base)); break;
				case 6: _game.PushState(new SoldiersState(_base)); break;
				case 7: _game.PushState(new SellState(_base, null)); break;
				default: _game.PopState(); break;
			}
		}
		else if (f.Rules.IsMindShield)
		{
			if (f.BuildTime == 0)
			{
				f.Disabled = !f.Disabled;
				_view.Draw();
				_mini.Draw();
			}
		}
		else if (f.Rules.Crafts > 0)
		{
			if (f.CraftForDrawing == null)
			{
				_game.PushState(new CraftsState(_base));
			}
			else
				for (int craft = 0; craft < _base.Crafts.Count; ++craft)
				{
					if (f.CraftForDrawing == _base.Crafts[craft])
					{
						_game.PushState(new CraftInfoState(_base, craft));
						break;
					}
				}
		}
		else if (f.Rules.Storage > 0)
		{
			_game.PushState(new SellState(_base, null));
		}
		else if (f.Rules.Personnel > 0)
		{
			_game.PushState(new SoldiersState(_base));
		}
		else if (f.Rules.PsiLaboratories > 0 && Options.anytimePsiTraining && _base.AvailablePsiLabs > 0)
		{
			_game.PushState(new AllocatePsiTrainingState(_base));
		}
		else if (f.Rules.TrainingFacilities > 0 && _base.AvailableTraining > 0)
		{
			_game.PushState(new AllocateTrainingState(_base));
		}
		else if (f.Rules.Laboratories > 0)
		{
			_game.PushState(new ResearchState(_base));
		}
		else if (f.Rules.Workshops > 0)
		{
			_game.PushState(new ManufactureState(_base));
		}
		else if (f.Rules.Aliens > 0)
		{
			_game.PushState(new ManageAlienContainmentState(_base, f.Rules.PrisonType, OptionsOrigin.OPT_GEOSCAPE));
		}
		else if (f.Rules.IsLift || f.Rules.RadarRange > 0)
		{
			_game.PopState();
		}
	}

	/// <summary>Opens the corresponding Ufopaedia article.</summary>
	public void ViewMiddleClick(Action action)
	{
		BaseFacility f = _view.SelectedFacility;
		if (f != null)
		{
			string articleId = f.Rules.UfopediaType;
			Ufopaedia.OpenArticle(_game, articleId);
		}
	}

	/// <summary>Displays the name of the facility the mouse is over.</summary>
	public void ViewMouseOver(Action action)
	{
		BaseFacility f = _view.SelectedFacility;
		var ss = new System.Text.StringBuilder();
		if (f != null)
		{
			if (f.Rules.Crafts == 0 || f.BuildTime > 0)
			{
				ss.Append(Tr(f.Rules.Type));
			}
			else
			{
				ss.Append(Tr(f.Rules.Type));
				if (f.CraftForDrawing != null)
				{
					ss.Append(' ').Append(Tr("STR_CRAFT_").Arg(f.CraftForDrawing.GetName(_game.Language)));
				}
			}
		}
		_txtFacility.SetText(ss.ToString());
	}

	/// <summary>Clears the facility name.</summary>
	public void ViewMouseOut(Action action)
	{
		_txtFacility.SetText("");
	}

	/// <summary>Selects a new base to display.</summary>
	public void MiniLeftClick(Action action)
	{
		int @base = _mini.HoveredBase;
		if (@base < _game.SavedGame.Bases.Count)
		{
			_base = _game.SavedGame.Bases[@base];
			Init();
		}
	}

	/// <summary>Moves the current base to the left in the list of bases.</summary>
	public void MiniRightClick(Action action)
	{
		int baseIndex = _mini.HoveredBase;

		if (baseIndex > 0 && baseIndex < _game.SavedGame.Bases.Count)
		{
			var bases = _game.SavedGame.Bases;

			// only able to move the currently selected base
			if (bases[baseIndex] == _base)
			{
				(bases[baseIndex], bases[baseIndex - 1]) = (bases[baseIndex - 1], bases[baseIndex]);
				Init();
			}
		}
	}

	/// <summary>Selects a new base to display.</summary>
	public void HandleKeyPress(Action action)
	{
		if (action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			SDLKey[] baseKeys = [
				Options.keyBaseSelect1,
				Options.keyBaseSelect2,
				Options.keyBaseSelect3,
				Options.keyBaseSelect4,
				Options.keyBaseSelect5,
				Options.keyBaseSelect6,
				Options.keyBaseSelect7,
				Options.keyBaseSelect8
			];
			SDLKey key = action.Details.key.keysym.sym;
			// C++ reads past the key array with more than 8 bases
			for (int i = 0; i < _game.SavedGame.Bases.Count && i < baseKeys.Length; ++i)
			{
				if (key == baseKeys[i])
				{
					_base = _game.SavedGame.Bases[i];
					Init();
					break;
				}
			}
		}
	}

	/// <summary>Changes the Base name.</summary>
	public void EdtBaseChange(Action action)
	{
		_base.SetName(_edtBase.Text);
	}
}
