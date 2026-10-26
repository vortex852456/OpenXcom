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
// Ported from: src/Battlescape/InventoryState.h, src/Battlescape/InventoryState.cpp
// Port status: partial (missing: SDL_WarpMouse in refreshMouse)
#nullable disable

using System.Globalization;
using System.Text;
using static Acom.BattleActionType;
using static Acom.BattleType;
using static Acom.ItemDamageRandomType;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>
/// Screen which displays soldier's inventory.
/// </summary>
public class InventoryState : State
{
	private const int _templateBtnX = 288;
	private const int _createTemplateBtnY = 90;
	private const int _applyTemplateBtnY = 113;

	private readonly Surface _bg, _soldier;
	private readonly Text _txtItem, _txtAmmo, _txtWeight, _txtTus, _txtStatLine1, _txtStatLine2, _txtStatLine3, _txtStatLine4, _txtPosition;
	private readonly Text _txtNameStatic;
	private readonly TextEdit _txtName;
	private readonly TextEdit _btnQuickSearch;
	private readonly BattlescapeButton _btnOk, _btnPrev, _btnNext, _btnUnload, _btnGround, _btnRank, _btnArmor;
	private readonly BattlescapeButton _btnCreateTemplate, _btnApplyTemplate;
	private readonly BattlescapeButton _btnLinks;
	private readonly Surface _selAmmo;
	private readonly Inventory _inv;
	private readonly List<EquipmentLayoutItem> _curInventoryTemplate = [], _tempInventoryTemplate = [];
	private readonly SavedBattleGame _battleGame;
	private readonly bool _tu, _noCraft;
	private readonly BattlescapeState _parent;
	private readonly Base _base;
	/// <summary>C++ std::map&lt;Soldier*, Craft*&gt; (pointer keys, only used for lookups).</summary>
	private readonly Dictionary<Soldier, Craft> _backup = [];
	private bool _resetCustomDeploymentBackup;
	private string _currentTooltip = "";
	private string _currentDamageTooltip = "";
	private int _mouseHoverItemFrame;
	private BattleItem _mouseHoverItem;
	private BattleItem _currentDamageTooltipItem;
	private bool _reloadUnit;
	private int _globalLayoutIndex;
	private int _prev_key, _key_repeats;
	private bool _disposed;

	/// <summary>
	/// Initializes all the elements in the Inventory screen.
	/// </summary>
	/// <param name="tu">Does Inventory use up Time Units?</param>
	/// <param name="parent">Pointer to parent Battlescape.</param>
	/// <param name="base">Pointer to the base (if any).</param>
	/// <param name="noCraft">Pre-equipping in the base without a craft?</param>
	public InventoryState(bool tu, BattlescapeState parent, Base @base, bool noCraft = false)
	{
		_tu = tu;
		_noCraft = noCraft;
		_parent = parent;
		_base = @base;
		_resetCustomDeploymentBackup = false;
		_reloadUnit = false;
		_globalLayoutIndex = -1;

		_battleGame = _game.SavedGame.SavedBattle;

		if (Options.oxceAlternateCraftEquipmentManagement && !_tu && _base != null && _noCraft)
		{
			// deassign all soldiers
			foreach (Soldier soldier in _base.Soldiers)
			{
				_backup[soldier] = soldier.Craft;
				if (soldier.Craft != null && soldier.Craft.Status != "STR_OUT")
				{
					soldier.SetCraftAndMoveEquipment(null, _base, _game.SavedGame.MonthsPassed == -1);
				}
			}
		}

		if (Options.maximizeInfoScreens)
		{
			Options.baseXResolution = Screen.ORIGINAL_WIDTH;
			Options.baseYResolution = Screen.ORIGINAL_HEIGHT;
			_game.Screen.ResetDisplay(false);
		}
		else if (_battleGame.IsBaseCraftInventory)
		{
			Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, true);
			_game.Screen.ResetDisplay(false);
		}

		// Create objects
		_bg = new Surface(320, 200, 0, 0);
		_soldier = new Surface(320, 200, 0, 0);
		_txtPosition = new Text(70, 9, 65, 95);
		_txtNameStatic = new Text(210, 17, 28, 6);
		_txtName = new TextEdit(this, 210, 17, 28, 6);
		_txtItem = new Text(160, 9, 128, 140);
		_txtAmmo = new Text(66, 24, 254, 64);
		_btnOk = new BattlescapeButton(35, 22, 237, 1);
		_btnPrev = new BattlescapeButton(23, 22, 273, 1);
		_btnNext = new BattlescapeButton(23, 22, 297, 1);
		_btnUnload = new BattlescapeButton(32, 25, 288, 32);
		_btnGround = new BattlescapeButton(32, 15, 289, 137);
		_btnRank = new BattlescapeButton(26, 23, 0, 0);
		_btnArmor = new BattlescapeButton(RuleInventory.PAPERDOLL_W, RuleInventory.PAPERDOLL_H, RuleInventory.PAPERDOLL_X, RuleInventory.PAPERDOLL_Y);
		_btnCreateTemplate = new BattlescapeButton(32, 22, _templateBtnX, _createTemplateBtnY);
		_btnApplyTemplate = new BattlescapeButton(32, 22, _templateBtnX, _applyTemplateBtnY);
		Element pixelShift = _game.Mod.GetInterface("inventory").GetElementOptional("buttonLinks");
		if (pixelShift != null && pixelShift.TFTDMode)
		{
			_btnLinks = new BattlescapeButton(23, 22, 213, 0);
		}
		else
		{
			_btnLinks = new BattlescapeButton(23, 22, 213, 1);
		}
		_selAmmo = new Surface(RuleInventory.HAND_W * RuleInventory.SLOT_W, RuleInventory.HAND_H * RuleInventory.SLOT_H, 272, 88);
		_inv = new Inventory(_game, 320, 200, 0, 0, _parent == null);
		_txtTus = new Text(40, 9, 213, 24);
		_txtWeight = new Text(70, 9, 213, 24);
		_txtStatLine1 = new Text(70, 9, 213, 32);
		_txtStatLine2 = new Text(70, 9, 213, 40);
		_txtStatLine3 = new Text(70, 9, 213, 48);
		_txtStatLine4 = new Text(70, 9, 213, 56);
		_btnQuickSearch = new TextEdit(this, 40, 9, 244, 140);

		// Set palette
		SetStandardPalette("PAL_BATTLESCAPE");

		Add(_bg);

		// Set up objects
		_game.Mod.GetSurface("TAC01.SCR").BlitNShade(_bg, 0, 0);
		Add(_btnArmor, "buttonArmor", "inventory", _bg);

		Add(_soldier);
		Add(_btnQuickSearch, "textItem", "inventory");
		Add(_txtNameStatic, "textName", "inventory", _bg);
		Add(_txtName, "textName", "inventory", _bg);
		Add(_txtItem, "textItem", "inventory", _bg);
		Add(_txtAmmo, "textAmmo", "inventory", _bg);
		Add(_btnOk, "buttonOK", "inventory", _bg);
		Add(_btnPrev, "buttonPrev", "inventory", _bg);
		Add(_btnNext, "buttonNext", "inventory", _bg);
		Add(_btnUnload, "buttonUnload", "inventory", _bg);
		Add(_btnGround, "buttonGround", "inventory", _bg);
		Add(_btnRank, "rank", "inventory", _bg);
		Add(_btnCreateTemplate, "buttonCreate", "inventory", _bg);
		Add(_btnApplyTemplate, "buttonApply", "inventory", _bg);
		Add(_btnLinks, "buttonLinks", "inventory", _bg);
		Add(_selAmmo);
		Add(_inv);
		Add(_txtTus, "textTUs", "inventory", _bg);
		Add(_txtWeight, "textWeight", "inventory", _bg);
		Add(_txtStatLine1, "textStatLine1", "inventory", _bg);
		Add(_txtStatLine2, "textStatLine2", "inventory", _bg);
		Add(_txtStatLine3, "textStatLine3", "inventory", _bg);
		Add(_txtStatLine4, "textStatLine4", "inventory", _bg);
		Add(_txtPosition, "textSlot", "inventory", _bg);

		// move the TU display down to make room for the weight display
		if (Options.showMoreStatsInInventoryView)
		{
			_txtTus.Y = _txtTus.Y + 8;
		}

		CenterAllSurfaces();


		_txtPosition.SetHighContrast(true);

		_txtNameStatic.SetBig();
		_txtNameStatic.SetHighContrast(true);

		_txtName.SetBig();
		_txtName.SetHighContrast(true);
		_txtName.OnChange(EdtSoldierChange);
		_txtName.OnMousePress(EdtSoldierPress);

		if (Options.oxceLinksDisableTextEdit)
		{
			_txtName.Visible = false;
		}
		else
		{
			_txtNameStatic.Visible = false;
		}

		_txtTus.SetHighContrast(true);

		_txtWeight.SetHighContrast(true);

		_txtStatLine1.SetHighContrast(true);

		_txtStatLine2.SetHighContrast(true);

		_txtStatLine3.SetHighContrast(true);

		_txtStatLine4.SetHighContrast(true);

		_txtItem.SetHighContrast(true);

		_txtAmmo.Align = TextHAlign.ALIGN_CENTER;
		_txtAmmo.SetHighContrast(true);

		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyBattleInventory);
		_btnOk.OnKeyboardPress(BtnUfopaediaClick, Options.keyGeoUfopedia);
		_btnOk.OnKeyboardPress(BtnArmorClick, Options.keyInventoryArmor);
		_btnOk.OnKeyboardPress(BtnArmorClickRight, Options.keyInventoryAvatar);
		_btnOk.OnKeyboardPress(BtnInventoryLoadClick, Options.keyInventoryLoad);
		_btnOk.OnKeyboardPress(BtnInventorySaveClick, Options.keyInventorySave);
		_btnOk.OnKeyboardPress(BtnCreatePersonalTemplateClick, Options.keyInvSavePersonalEquipment);
		_btnOk.OnKeyboardPress(BtnApplyPersonalTemplateClick, Options.keyInvLoadPersonalEquipment);
		_btnOk.OnKeyboardPress(BtnShowPersonalTemplateClick, Options.keyInvShowPersonalEquipment);
		_btnOk.OnKeyboardPress(BtnDiaryLightClick, Options.keyInventoryDiaryLight);
		_btnOk.Tooltip = "STR_OK";
		_btnOk.OnMouseIn(TxtTooltipInExtraOK);
		_btnOk.OnMouseOut(TxtTooltipOut);
		_btnOk.OnKeyboardPress(InvMouseOver, SDLKey.SDLK_LALT);
		_btnOk.OnKeyboardRelease(InvMouseOver, SDLKey.SDLK_LALT);
		_btnOk.OnKeyboardPress(InvMouseOver, SDLKey.SDLK_RALT);
		_btnOk.OnKeyboardRelease(InvMouseOver, SDLKey.SDLK_RALT);

		_btnPrev.OnMouseClick(BtnPrevClick);
		_btnPrev.OnKeyboardPress(BtnPrevClick, Options.keyBattlePrevUnit);
		_btnPrev.Tooltip = "STR_PREVIOUS_UNIT";
		_btnPrev.OnMouseIn(TxtTooltipIn);
		_btnPrev.OnMouseOut(TxtTooltipOut);

		_btnNext.OnMouseClick(BtnNextClick);
		_btnNext.OnKeyboardPress(BtnNextClick, Options.keyBattleNextUnit);
		_btnNext.Tooltip = "STR_NEXT_UNIT";
		_btnNext.OnMouseIn(TxtTooltipIn);
		_btnNext.OnMouseOut(TxtTooltipOut);

		_btnUnload.OnMouseClick(BtnUnloadClick);
		_btnUnload.Tooltip = "STR_UNLOAD_WEAPON";
		_btnUnload.OnMouseIn(TxtTooltipIn);
		_btnUnload.OnMouseOut(TxtTooltipOut);

		_btnGround.OnMouseClick(BtnGroundClickForward, SDLMouse.SDL_BUTTON_LEFT);
		_btnGround.OnMouseClick(BtnGroundClickBackward, SDLMouse.SDL_BUTTON_RIGHT);
		_btnGround.Tooltip = "STR_SCROLL_RIGHT";
		_btnGround.OnMouseIn(TxtTooltipIn);
		_btnGround.OnMouseOut(TxtTooltipOut);
		_btnGround.OnKeyboardPress(BtnGroundClickBackward, Options.keyBattleLeft);
		_btnGround.OnKeyboardPress(BtnGroundClickForward, Options.keyBattleRight);

		_btnRank.OnMouseClick(BtnRankClick);
		_btnRank.Tooltip = "STR_UNIT_STATS";
		_btnRank.OnMouseIn(TxtTooltipIn);
		_btnRank.OnMouseOut(TxtTooltipOut);

		if (!_game.Mod.InventoryOverlapsPaperdoll)
		{
			_btnArmor.OnMouseClick(BtnArmorClick);
			_btnArmor.OnMouseClick(BtnArmorClickRight, SDLMouse.SDL_BUTTON_RIGHT);
			_btnArmor.OnMouseClick(BtnArmorClickMiddle, SDLMouse.SDL_BUTTON_MIDDLE);
			_btnArmor.OnMouseIn(TxtArmorTooltipIn);
			_btnArmor.OnMouseOut(TxtArmorTooltipOut);
		}

		_btnCreateTemplate.OnMouseClick(BtnCreateTemplateClick);
		_btnCreateTemplate.OnKeyboardPress(BtnCreateTemplateClick, Options.keyInvCreateTemplate);
		_btnCreateTemplate.Tooltip = "STR_CREATE_INVENTORY_TEMPLATE";
		_btnCreateTemplate.OnMouseIn(TxtTooltipIn);
		_btnCreateTemplate.OnMouseOut(TxtTooltipOut);

		_btnApplyTemplate.OnMouseClick(BtnApplyTemplateClick);
		_btnApplyTemplate.OnKeyboardPress(BtnApplyTemplateClick, Options.keyInvApplyTemplate);
		_btnApplyTemplate.OnKeyboardPress(OnClearInventory, Options.keyInvClear);
		_btnApplyTemplate.OnKeyboardPress(OnAutoequip, Options.keyInvAutoEquip);
		_btnApplyTemplate.Tooltip = "STR_APPLY_INVENTORY_TEMPLATE";
		_btnApplyTemplate.OnMouseIn(TxtTooltipIn);
		_btnApplyTemplate.OnMouseOut(TxtTooltipOut);

		_btnLinks.OnMouseClick(BtnLinksClick);
		_btnLinks.Tooltip = "STR_EXTENDED_LINKS";
		_btnLinks.OnMouseIn(TxtTooltipIn);
		_btnLinks.OnMouseOut(TxtTooltipOut);

		_btnQuickSearch.SetHighContrast(true);
		_btnQuickSearch.Text = ""; // redraw
		_btnQuickSearch.OnChange(BtnQuickSearchApply);
		_btnQuickSearch.Visible = Options.oxceQuickSearchButton;

		_btnOk.OnKeyboardRelease(BtnQuickSearchToggle, Options.keyToggleQuickSearch);

		_game.Mod.GetSurface("oxceLinksInv").BlitNShade(_btnLinks, 0, 0);
		_btnLinks.InitSurfaces();
		_btnLinks.Visible = Options.oxceLinks;

		// only use copy/paste buttons in setup (i.e. non-tu) mode
		if (_tu)
		{
			_btnCreateTemplate.Visible = false;
			_btnApplyTemplate.Visible = false;
		}
		else
		{
			UpdateTemplateButtons(true);
		}

		_inv.Draw();
		_inv.SetTuMode(_tu);
		_inv.SetSelectedUnit(_game.SavedGame.SavedBattle.SelectedUnit, true);
		_inv.OnMouseClick(InvClick, 0);
		_inv.OnMouseOver(InvMouseOver);
		_inv.OnMouseOut(InvMouseOut);

		if (_battleGame.DebugMode && _game.IsShiftPressed())
		{
			// replenish TUs
			BattleUnit unit = _inv.SelectedUnit;
			if (unit != null)
			{
				int missingTUs = unit.BaseStats.tu - unit.TimeUnits;
				unit.SpendTimeUnits(-missingTUs);
			}
		}

		_txtTus.Visible = _tu;
		_txtWeight.Visible = Options.showMoreStatsInInventoryView;
		_txtStatLine1.Visible = Options.showMoreStatsInInventoryView && !_tu;
		_txtStatLine2.Visible = Options.showMoreStatsInInventoryView && !_tu;
		_txtStatLine3.Visible = Options.showMoreStatsInInventoryView && !_tu;
		_txtStatLine4.Visible = Options.showMoreStatsInInventoryView && !_tu;
	}

	/// <summary>C++ Collections::deleteAll on an equipment layout.</summary>
	private static void ClearInventoryTemplate(List<EquipmentLayoutItem> inventoryTemplate)
	{
		inventoryTemplate.Clear();
	}

	/// <summary>Cleans up the Inventory state (C++ destructor).</summary>
	public override void Destroy()
	{
		if (_disposed)
			return;
		_disposed = true;

		ClearInventoryTemplate(_curInventoryTemplate);
		ClearInventoryTemplate(_tempInventoryTemplate);

		if (!_battleGame.IsBaseCraftInventory)
		{
			if (Options.maximizeInfoScreens)
			{
				Screen.UpdateScale(Options.battlescapeScale, ref Options.baseXBattlescape, ref Options.baseYBattlescape, true);
				_game.Screen.ResetDisplay(false);
			}

			//fix case when scripts could kill unit before inventory is closed
			BattleUnit unit = _battleGame.SelectedUnit;
			if (unit != null)
			{
				Tile inventoryTile = unit.Tile;
				_battleGame.TileEngine.ApplyGravity(inventoryTile);
			}
			_battleGame.TileEngine.CalculateLighting(LightLayers.LL_ITEMS); // dropping/picking up flares
			_battleGame.TileEngine.RecalculateFOV();
		}
		else
		{
			Screen.UpdateScale(Options.geoscapeScale, ref Options.baseXGeoscape, ref Options.baseYGeoscape, true);
			_game.Screen.ResetDisplay(false);
		}
	}

	/// <summary>Sets the global layout to load on the next init.</summary>
	public void SetGlobalLayoutIndex(int index, bool armorChanged)
	{
		_globalLayoutIndex = index;
		if (armorChanged)
		{
			_reloadUnit = true;
		}
	}

	/// <summary>Updates all soldier stats when the soldier changes.</summary>
	public override void Init()
	{
		base.Init();
		BattleUnit unit = _battleGame.SelectedUnit;

		// no selected unit, close inventory
		if (unit == null)
		{
			BtnOkClick(null);
			return;
		}
		// skip to the first unit with inventory
		if (!unit.HasInventory)
		{
			if (_parent != null)
			{
				_parent.SelectNextPlayerUnit(false, false, true, _tu);
			}
			else
			{
				_battleGame.SelectNextPlayerUnit(false, false, true);
			}
			// no available unit, close inventory
			if (_battleGame.SelectedUnit == null || !_battleGame.SelectedUnit.HasInventory)
			{
				// starting a mission with just vehicles
				BtnOkClick(null);
				return;
			}
			else
			{
				unit = _battleGame.SelectedUnit;
			}
		}

		_soldier.Clear();
		_btnRank.Clear();

		if (Options.oxceInventoryShowUnitSlot)
		{
			int unitSlot = 1;
			int totalSlots = 99;
			foreach (BattleUnit tmpUnit in _battleGame.Units)
			{
				if (tmpUnit == unit)
				{
					if (!_noCraft && _battleGame.MissionType != "STR_BASE_DEFENSE")
					{
						Craft tmpCraft = unit.GeoscapeSoldier?.Craft;
						if (tmpCraft != null)
						{
							totalSlots = tmpCraft.MaxUnitsClamped;
						}
					}
					break;
				}
				unitSlot += tmpUnit.Armor.TotalSize;
			}
			_txtPosition.SetText(Tr("STR_SLOT").Arg(unitSlot).Arg(totalSlots));
		}

		_txtNameStatic.SetBig();
		_txtNameStatic.SetText(unit.GetName(_game.Language));

		_txtName.SetBig();
		_txtName.Text = unit.GetName(_game.Language);

		_btnLinks.Visible = Options.oxceLinks;

		bool resetGroundOffset = _tu;
		if (unit.IsSummonedPlayerUnit)
		{
			resetGroundOffset = true; // this unit is likely not standing on the shared inventory tile, just re-arrange it every time
		}
		_inv.SetSelectedUnit(unit, resetGroundOffset);
		Soldier s = unit.GeoscapeSoldier;
		if (s != null)
		{
			// reload necessary after the change of armor
			if (_reloadUnit)
			{
				if (Options.oxceAlternateCraftEquipmentManagement && s.Armor != null && unit.Armor != null && s.Armor.Size > unit.Armor.Size)
				{
					_resetCustomDeploymentBackup = true;
				}

				// Step 0: update unit's armor
				unit.UpdateArmorFromSoldier(_game.Mod, s, s.Armor, _battleGame.Depth, false, null);

				// Step 1: remember the unit's equipment (incl. loaded fixed items)
				ClearInventoryTemplate(_tempInventoryTemplate);
				CreateInventoryTemplate(_tempInventoryTemplate);

				// Step 2: drop all items (and delete fixed items!!)
				Tile groundTile = unit.Tile;
				_battleGame.TileEngine.ItemDropInventory(groundTile, unit, true, true);

				// Step 3: equip fixed items // Note: the inventory must be *completely* empty before this step
				_battleGame.InitUnit(unit);

				// Step 4: re-equip original items (unless slots taken by fixed items)
				ApplyInventoryTemplate(_tempInventoryTemplate);

				// refresh ui
				_inv.ArrangeGround(); // calls drawItems() too

				// reset armor tooltip
				_currentTooltip = "";
				_txtItem.SetText("");

				// reload done
				_reloadUnit = false;
			}

			SurfaceSet texture = _game.Mod.GetSurfaceSet("SMOKE.PCK");
			Surface frame = texture.GetFrame(s.RankSpriteBattlescape);
			if (frame != null)
			{
				frame.BlitNShade(_btnRank, 0, 0);
			}

			if (s.Armor.HasLayersDefinition)
			{
				foreach (string layer in s.GetArmorLayers())
				{
					_game.Mod.GetSurface(layer, true).BlitNShade(_soldier, 0, 0);
				}
			}
			else
			{
				string look = s.Armor.SpriteInventory;
				string gender = s.Gender == SoldierGender.GENDER_MALE ? "M" : "F";
				Surface surf = null;

				for (int i = 0; i <= RuleSoldier.LookVariantBits; ++i)
				{
					string name = look + gender + ((int)s.Look + (s.LookVariant & (RuleSoldier.LookVariantMask >> i)) * 4).ToString(CultureInfo.InvariantCulture) + ".SPK";
					surf = _game.Mod.GetSurface(name, false);
					if (surf != null)
					{
						break;
					}
				}
				if (surf == null)
				{
					surf = _game.Mod.GetSurface(look + ".SPK", false);
				}
				if (surf == null)
				{
					surf = _game.Mod.GetSurface(look, true);
				}
				surf.BlitNShade(_soldier, 0, 0);
			}
		}
		else
		{
			Surface armorSurface = _game.Mod.GetSurface(unit.Armor.SpriteInventory, false);
			if (armorSurface == null)
			{
				armorSurface = _game.Mod.GetSurface(unit.Armor.SpriteInventory + ".SPK", false);
			}
			if (armorSurface == null)
			{
				armorSurface = _game.Mod.GetSurface(unit.Armor.SpriteInventory + "M0.SPK", false);
			}
			if (armorSurface != null)
			{
				armorSurface.BlitNShade(_soldier, 0, 0);
			}
		}

		// coming from InventoryLoad window...
		if (_globalLayoutIndex > -1)
		{
			LoadGlobalLayout(_globalLayoutIndex);
			_globalLayoutIndex = -1;

			// refresh ui
			_inv.ArrangeGround();
		}

		UpdateStats();
		RefreshMouse();
	}

	/// <summary>Disables the input, if not a soldier. Sets the name without a statstring otherwise.</summary>
	public void EdtSoldierPress(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_btnLinks.Visible)
		{
			double mx = action.AbsoluteXMouse;
			if (mx >= _btnLinks.X)
			{
				_txtName.SetFocus(false);
				return;
			}
			else
			{
				_btnLinks.Visible = false;
			}
		}

		{
			BattleUnit unit = _inv.SelectedUnit;
			if (unit != null)
			{
				Soldier s = unit.GeoscapeSoldier;
				if (s != null)
				{
					// set the soldier's name without a statstring
					_txtName.Text = s.GetName();
				}

			}
		}
	}

	/// <summary>Changes the soldier's name.</summary>
	public void EdtSoldierChange(Action action)
	{
		BattleUnit unit = _inv.SelectedUnit;
		if (unit != null)
		{
			Soldier s = unit.GeoscapeSoldier;
			if (s != null)
			{
				// set the soldier's name
				s.SetName(_txtName.Text);
				// also set the unit's name (with a statstring)
				unit.SetName(s.GetName(true));
			}
		}
	}

	/// <summary>Updates the soldier stats (Weight, TU).</summary>
	public void UpdateStats()
	{
		BattleUnit unit = _battleGame.SelectedUnit;

		_txtTus.SetText(Tr("STR_TIME_UNITS_SHORT").Arg(unit.TimeUnits));

		int weight = unit.GetCarriedWeight(_inv.SelectedItem);
		_txtWeight.SetText(Tr("STR_WEIGHT").Arg(weight).Arg(unit.BaseStats.strength));
		if (weight > unit.BaseStats.strength)
		{
			_txtWeight.SetSecondaryColor((byte)_game.Mod.GetInterface("inventory").GetElement("weight").color2);
		}
		else
		{
			_txtWeight.SetSecondaryColor((byte)_game.Mod.GetInterface("inventory").GetElement("weight").color);
		}

		int psiSkillWithoutAnyBonuses = unit.BaseStats.psiSkill;
		if (unit.GeoscapeSoldier != null)
		{
			psiSkillWithoutAnyBonuses = unit.GeoscapeSoldier.CurrentStats.psiSkill;
		}
		bool showPsiStrength = (psiSkillWithoutAnyBonuses > 0 || (Options.psiStrengthEval && _game.SavedGame.IsResearched(_game.Mod.PsiRequirements)));

		void updateStatLine(Text txtField, string elementId)
		{
			Element element = _game.Mod.GetInterface("inventory").GetElementOptional(elementId);
			if (element != null)
			{
				switch (element.custom)
				{
					case 1:
						txtField.SetText(Tr("STR_ACCURACY_SHORT").Arg(unit.BaseStats.firing));
						break;
					case 2:
						txtField.SetText(Tr("STR_REACTIONS_SHORT").Arg(unit.BaseStats.reactions));
						break;
					case 3:
						if (psiSkillWithoutAnyBonuses > 0)
							txtField.SetText(Tr("STR_PSIONIC_SKILL_SHORT").Arg(unit.BaseStats.psiSkill));
						else
							txtField.SetText("");
						break;
					case 4:
						if (showPsiStrength)
							txtField.SetText(Tr("STR_PSIONIC_STRENGTH_SHORT").Arg(unit.BaseStats.psiStrength));
						else
							txtField.SetText("");
						break;
					case 11:
						txtField.SetText(Tr("STR_FIRING_SHORT").Arg(unit.BaseStats.firing));
						break;
					case 12:
						txtField.SetText(Tr("STR_THROWING_SHORT").Arg(unit.BaseStats.throwing));
						break;
					case 13:
						txtField.SetText(Tr("STR_MELEE_SHORT").Arg(unit.BaseStats.melee));
						break;
					case 14:
						if (showPsiStrength)
						{
							txtField.SetText(Tr("STR_PSI_SHORT")
								.Arg(unit.BaseStats.psiStrength)
								.Arg(unit.BaseStats.psiSkill > 0 ? unit.BaseStats.psiSkill : 0));
						}
						else
						{
							txtField.SetText("");
						}
						break;
					default:
						txtField.SetText("");
						break;
				}
			}
		}

		updateStatLine(_txtStatLine1, "textStatLine1");
		updateStatLine(_txtStatLine2, "textStatLine2");
		updateStatLine(_txtStatLine3, "textStatLine3");
		updateStatLine(_txtStatLine4, "textStatLine4");
	}

	/// <summary>Saves the soldiers' equipment-layout.</summary>
	public void SaveEquipmentLayout()
	{
		foreach (BattleUnit bu in _battleGame.Units)
		{
			// we need X-Com soldiers only
			if (bu.GeoscapeSoldier == null) continue;

			List<EquipmentLayoutItem> layoutItems = bu.GeoscapeSoldier.EquipmentLayout;

			// clear the previous save
			if (layoutItems.Count != 0)
			{
				layoutItems.Clear();
			}

			// save the soldier's items
			// note: with using getInventory() we are skipping the ammos loaded, (they're not owned) because we handle the loaded-ammos separately (inside)
			foreach (BattleItem bi in bu.Inventory)
			{
				// skip fixed items
				if (bi.Rules.IsFixed)
				{
					bool loaded = bi.NeedsAmmoForSlot(0) && bi.GetAmmoForSlot(0) != null;
					if (!loaded) continue;
				}
				else if (!bi.Rules.CanBeEquippedToCraftInventory())
				{
					// skip other not-allowed items, which somehow made it here (e.g. were spawned instead of equipped)
					continue;
				}

				layoutItems.Add(new EquipmentLayoutItem(bi));
			}
		}
	}

	/// <summary>Index of the soldier in the base's soldier list (last match, like C++).</summary>
	private int FindSoldierIndex(Soldier s)
	{
		int soldierIndex = 0;
		List<Soldier> soldiers = _base.Soldiers;
		for (int i = 0; i < soldiers.Count; ++i)
		{
			if (soldiers[i].Id == s.Id)
			{
				soldierIndex = i;
			}
		}
		return soldierIndex;
	}

	/// <summary>Opens the Armor Selection GUI.</summary>
	public void BtnArmorClick(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			// but we can reuse this for quickly dropping an item (as a Ctrl+L-click alternative)
			if (Options.oxceInventoryDropItemOverPaperdoll)
			{
				if (_inv.QuickDrop())
				{
					// hide selected item info
					InvMouseOut(action);

					// refresh ui
					_inv.ArrangeGround();
					UpdateStats();
					RefreshMouse();

					// give audio feedback
					PlayDropSound();
				}
			}
			return;
		}

		// only allowed during base equipment
		if (_base == null)
		{
			return;
		}

		// equipment in the base
		BattleUnit unit = _battleGame.SelectedUnit;
		Soldier s = unit.GeoscapeSoldier;

		if (!(s.Craft != null && s.Craft.Status == "STR_OUT"))
		{
			int soldierIndex = FindSoldierIndex(s);

			_reloadUnit = true;
			_game.PushState(new SoldierArmorState(_base, soldierIndex, SoldierArmorOrigin.SA_BATTLESCAPE));
		}
	}

	/// <summary>Opens the Avatar Selection GUI.</summary>
	public void BtnArmorClickRight(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		// only allowed during base equipment
		if (_base == null)
		{
			return;
		}

		// equipment in the base
		BattleUnit unit = _battleGame.SelectedUnit;
		Soldier s = unit.GeoscapeSoldier;

		if (!(s.Craft != null && s.Craft.Status == "STR_OUT"))
		{
			int soldierIndex = FindSoldierIndex(s);

			_game.PushState(new SoldierAvatarState(_base, soldierIndex));
		}
	}

	/// <summary>Opens the Soldier Diary Light GUI.</summary>
	public void BtnDiaryLightClick(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		BattleUnit unit = _battleGame.SelectedUnit;
		Soldier s = unit.GeoscapeSoldier;
		_game.PushState(new SoldierDiaryLightState(s));
	}

	/// <summary>Opens Ufopaedia entry for the corresponding armor.</summary>
	public void BtnArmorClickMiddle(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		BattleUnit unit = _inv.SelectedUnit;
		if (unit != null)
		{
			string articleId = unit.Armor.UfopediaType;
			Ufopaedia.OpenArticle(_game, articleId);
		}
	}

	/// <summary>Saves the current equipment into a global layout.</summary>
	public void SaveGlobalLayout(int index, bool includingArmor)
	{
		List<EquipmentLayoutItem> tmpl = _game.SavedGame.GetGlobalEquipmentLayout(index);

		// clear current template
		ClearInventoryTemplate(tmpl);

		// create new template
		CreateInventoryTemplate(tmpl);

		// optionally save armor info too
		if (includingArmor && _battleGame.SelectedUnit.GeoscapeSoldier != null)
		{
			_game.SavedGame.SetGlobalEquipmentLayoutArmor(index, _battleGame.SelectedUnit.Armor.Type);
		}
		else
		{
			_game.SavedGame.SetGlobalEquipmentLayoutArmor(index, "");
		}
	}

	/// <summary>Applies a global layout.</summary>
	public void LoadGlobalLayout(int index)
	{
		List<EquipmentLayoutItem> tmpl = _game.SavedGame.GetGlobalEquipmentLayout(index);

		ApplyInventoryTemplate(tmpl);
	}

	/// <summary>Changes the armor to the one saved with a global layout.</summary>
	public bool LoadGlobalLayoutArmor(int index)
	{
		string armorName = _game.SavedGame.GetGlobalEquipmentLayoutArmor(index);
		return TryArmorChange(armorName);
	}

	/// <summary>Tries to change the armor of the selected soldier.</summary>
	/// <returns>True if the armor was changed.</returns>
	public bool TryArmorChange(string armorName)
	{
		Armor prev = null;
		Soldier soldier = null;
		BattleUnit unit = _inv.SelectedUnit;
		if (unit != null)
		{
			soldier = unit.GeoscapeSoldier;
			if (soldier != null)
			{
				prev = soldier.Armor;
			}
		}

		Armor next = null;
		{
			next = _game.Mod.GetArmor(armorName, false);
		}

		// check armor availability
		bool armorAvailable = false;
		if (prev != null && next != null && next != prev && soldier != null && _base != null)
		{
			armorAvailable = true;
			if (_game.SavedGame.MonthsPassed != -1)
			{
				// is the armor physically available?
				if (next.StoreItem != null && prev.StoreItem != next.StoreItem)
				{
					if (_base.StorageItems.GetItem(next.StoreItem) <= 0)
					{
						armorAvailable = false;
					}
				}
				// is the armor unlocked?
				if (next.RequiredResearch != null && !_game.SavedGame.IsResearched(next.RequiredResearch))
				{
					armorAvailable = false;
				}
			}
			if (armorAvailable)
			{
				// refresh soldier's _bonusCache, needed below in Armor::getCanBeUsedBy()
				//soldier->getBonuses(_game->getMod());

				// does the armor fit on the current unit?
				if (!next.GetCanBeUsedBy(soldier))
				{
					armorAvailable = false;
				}
			}
		}

		// change armor
		bool armorChanged = false;
		if (armorAvailable)
		{
			Craft craft = soldier.Craft;
			if (craft != null)
			{
				if (!craft.ValidateArmorChange(prev.Size, next.Size))
				{
					// STR_NOT_ENOUGH_CRAFT_SPACE
					return false;
				}
			}
			if (_game.SavedGame.MonthsPassed != -1)
			{
				if (prev.StoreItem != null)
				{
					_base.StorageItems.AddItem(prev.StoreItem);
				}
				if (next.StoreItem != null)
				{
					_base.StorageItems.RemoveItem(next.StoreItem);
				}
			}
			if (Options.oxceAlternateCraftEquipmentManagement && next.Size > prev.Size)
			{
				_resetCustomDeploymentBackup = true;
			}
			soldier.SetArmor(next, true);
			armorChanged = true;
		}

		return armorChanged;
	}

	/// <summary>Handles global equipment layout actions.</summary>
	public void BtnGlobalEquipmentLayoutClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		// cannot use this feature during the mission!
		if (_tu)
		{
			return;
		}

		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		// SDLK_0 = 48, SDLK_1 = 49, SDLK_9 = 57
		// SDLK_0 selects the 10-th inventory layout
		// by repeating a key you can load a layout from the next decade
		int sym = (int)action.Details.key.keysym.sym;
		int layout_no = (sym == 48) ? 10 : sym - 48;

		if (sym == _prev_key && !_game.IsCtrlPressed())
		{
			_key_repeats++;
		}
		else
		{
			_key_repeats = 0;
		}
		_prev_key = sym;

		int index = 10 * _key_repeats + layout_no - 1;

		if (index < 0 || index >= Options.oxceMaxEquipmentLayoutTemplates)
		{
			// do nothing if the layout index is out of bounds
			return;
		}

		if (_game.IsCtrlPressed())
		{
			// can't save layout >10 this way
			_prev_key = 0;
			_key_repeats = 0;
			SaveGlobalLayout(index, false);

			// give audio feedback
			PlayDropSound();
			RefreshMouse();
		}
		else
		{
			// simulate what happens when loading via the InventoryLoadState dialog
			bool armorChanged = LoadGlobalLayoutArmor(index);
			SetGlobalLayoutIndex(index, armorChanged);
			Init();

			// give audio feedback
			PlayDropSound();
		}
	}

	/// <summary>Opens the InventoryLoad screen.</summary>
	public void BtnInventoryLoadClick(Action action)
	{
		// cannot use this feature during the mission!
		if (_tu)
		{
			return;
		}

		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		_game.PushState(new InventoryLoadState(this));
	}

	/// <summary>Opens the InventorySave screen.</summary>
	public void BtnInventorySaveClick(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		_game.PushState(new InventorySaveState(this));
	}

	/// <summary>Opens the Ufopaedia.</summary>
	public void BtnUfopaediaClick(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		Ufopaedia.Open(_game);
	}

	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		if (_inv.SelectedItem != null)
			return;
		_game.PopState();
		if (!_tu)
		{
			if (_base != null || !Options.oxceAlternateCraftEquipmentManagement)
			{
				SaveEquipmentLayout();
			}
			if (Options.oxceAlternateCraftEquipmentManagement && !_tu && _base != null && _noCraft)
			{
				// assign all soldiers back, if possible
				foreach (Soldier soldier in _base.Soldiers)
				{
					// C++ operator[] inserts a null entry for unknown soldiers
					_backup.TryGetValue(soldier, out Craft c);
					_backup[soldier] = c;
					if (soldier.Craft == null && c != null && c.Status != "STR_OUT")
					{
						int space = c.SpaceAvailable;
						if (c.ValidateAddingSoldier(space, soldier) == CraftPlacementErrors.CPE_None)
						{
							soldier.SetCraftAndMoveEquipment(c, _base, _game.SavedGame.MonthsPassed == -1, _resetCustomDeploymentBackup);
						}
					}
				}
			}
			if (_parent != null)
			{
				_battleGame.StartFirstTurn();
			}
		}
	}

	/// <summary>Selects the previous soldier.</summary>
	public void BtnPrevClick(Action action)
	{
		if (_inv.SelectedItem != null)
		{
			return;
		}

		if (_parent != null)
		{
			_parent.SelectPreviousPlayerUnit(false, false, true);
		}
		else
		{
			_battleGame.SelectPreviousPlayerUnit(false, false, true);
		}
		Init();
	}

	/// <summary>Selects the next soldier.</summary>
	public void BtnNextClick(Action action)
	{
		if (_inv.SelectedItem != null)
		{
			return;
		}
		if (_parent != null)
		{
			_parent.SelectNextPlayerUnit(false, false, true);
		}
		else
		{
			_battleGame.SelectNextPlayerUnit(false, false, true);
		}
		Init();
	}

	/// <summary>Unloads the selected weapon.</summary>
	public void BtnUnloadClick(Action action)
	{
		if (_inv.Unload(false))
		{
			_txtItem.SetText("");
			_txtAmmo.SetText("");
			_selAmmo.Clear();
			UpdateStats();
			_game.Mod.GetSoundByDepth(0, (uint)Mod.ITEM_DROP).Play();
		}
	}

	/// <summary>Quick search toggle.</summary>
	public void BtnQuickSearchToggle(Action action)
	{
		if (_btnQuickSearch.Visible)
		{
			_btnQuickSearch.Text = "";
			_btnQuickSearch.Visible = false;
			BtnQuickSearchApply(action);
		}
		else
		{
			_btnQuickSearch.Visible = true;
			_btnQuickSearch.SetFocus(true);
		}
	}

	/// <summary>Quick search.</summary>
	public void BtnQuickSearchApply(Action action)
	{
		if (!Options.oxceInstantQuickSearch && action != null && action.Details.type == SDLEventType.SDL_KEYDOWN)
		{
			SDLKey sym = action.Details.key.keysym.sym;
			if (sym != SDLKey.SDLK_RETURN && sym != SDLKey.SDLK_KP_ENTER && sym != SDLKey.SDLK_ESCAPE)
				return;
		}
		_inv.SetSearchString(_btnQuickSearch.Text);
	}

	/// <summary>Shows more ground items / rearranges them.</summary>
	public void BtnGroundClickForward(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		bool scrollBackwards = _game.IsShiftPressed();
		if (Options.oxceInventorySplitScrollButton)
		{
			double mx = action.AbsoluteXMouse;
			if (mx <= _btnGround.X + (_btnGround.Width / 2.0))
			{
				// clicked on the left half of the button
				scrollBackwards = true;
			}
		}

		if (scrollBackwards)
		{
			// scroll backwards
			_inv.ArrangeGround(-1);
		}
		else
		{
			// scroll forward
			_inv.ArrangeGround(1);
		}
	}

	/// <summary>Shows more ground items / rearranges them.</summary>
	public void BtnGroundClickBackward(Action action)
	{
		// scroll backwards
		_inv.ArrangeGround(-1);
	}

	/// <summary>Shows the unit info screen.</summary>
	public void BtnRankClick(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		_game.PushState(new UnitInfoState(_battleGame.SelectedUnit, _parent, true, false));
	}

	/// <summary>Helper method for Create Template button.</summary>
	private void CreateInventoryTemplate(List<EquipmentLayoutItem> inventoryTemplate)
	{
		// copy inventory instead of just keeping a pointer to it.  that way
		// create/apply can be used as an undo button for a single unit and will
		// also work as expected if inventory is modified after 'create' is clicked
		foreach (BattleItem bi in _battleGame.SelectedUnit.Inventory)
		{
			// skip fixed items
			if (bi.Rules.IsFixed)
			{
				bool loaded = bi.NeedsAmmoForSlot(0) && bi.GetAmmoForSlot(0) != null;
				if (!loaded) continue;
			}
			else if (!bi.Rules.CanBeEquippedToCraftInventory())
			{
				// skip other not-allowed items, which somehow made it here (e.g. were spawned instead of equipped)
				continue;
			}

			inventoryTemplate.Add(new EquipmentLayoutItem(bi));
		}
	}

	/// <summary>Opens the extended links menu.</summary>
	public void BtnLinksClick(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			// but we can reuse this for ufopedia (as an M-click alternative)
			string articleId = _inv.SelectedItem.Rules.UfopediaType;
			Ufopaedia.OpenArticle(_game, articleId);

			return;
		}

		_game.PushState(new ExtendedInventoryLinksState(this, _battleGame, _base != null, !_tu));
	}

	/// <summary>Handler for clicking on the Create Template button.</summary>
	public void BtnCreateTemplateClick(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		// clear current template
		ClearInventoryTemplate(_curInventoryTemplate);

		// create new template
		CreateInventoryTemplate(_curInventoryTemplate);

		// give audio feedback
		PlayDropSound();
		RefreshMouse();
	}

	/// <summary>Saves the personal equipment template.</summary>
	public void BtnCreatePersonalTemplateClick(Action action)
	{
		// cannot use this feature during the mission!
		if (_tu)
		{
			return;
		}

		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		BattleUnit unit = _battleGame.SelectedUnit;
		if (unit != null && unit.GeoscapeSoldier != null)
		{
			List<EquipmentLayoutItem> personalTemplate = unit.GeoscapeSoldier.PersonalEquipmentLayout;

			// clear current personal template
			ClearInventoryTemplate(personalTemplate);

			// create new personal template
			CreateInventoryTemplate(personalTemplate);

			// optionally save armor info too
			if (Options.oxcePersonalLayoutIncludingArmor)
			{
				unit.GeoscapeSoldier.PersonalEquipmentArmor = _battleGame.SelectedUnit.Armor;
			}
			else
			{
				unit.GeoscapeSoldier.PersonalEquipmentArmor = null;
			}

			// give visual feedback
			_inv.ShowWarning(Tr("STR_PERSONAL_EQUIPMENT_SAVED"));

			// give audio feedback
			PlayDropSound();
			RefreshMouse();
		}
	}

	/// <summary>Helper method for Apply Template button.</summary>
	private void ApplyInventoryTemplate(List<EquipmentLayoutItem> inventoryTemplate)
	{
		BattleUnit unit = _battleGame.SelectedUnit;
		Tile groundTile = unit.Tile;
		List<BattleItem> groundInv = groundTile.Inventory;

		_battleGame.TileEngine.ItemDropInventory(groundTile, unit, true, false);

		// attempt to replicate inventory template by grabbing corresponding items
		// from the ground.  if any item is not found on the ground, display warning
		// message, but continue attempting to fulfill the template as best we can
		bool itemMissing = false;
		foreach (EquipmentLayoutItem equipmentLayoutItem in inventoryTemplate)
		{
			// search for template item in ground inventory
			bool found = false;

			bool[] needsAmmo = new bool[RuleItem.AmmoSlotMax];
			RuleItem[] targetAmmo = new RuleItem[RuleItem.AmmoSlotMax];
			BattleItem matchedWeapon = null;
			BattleItem[] matchedAmmo = new BattleItem[RuleItem.AmmoSlotMax];

			for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
			{
				targetAmmo[slot] = equipmentLayoutItem.GetAmmoItemForSlot(slot);
				needsAmmo[slot] = (targetAmmo[slot] != null);
				matchedAmmo[slot] = null;
			}

			foreach (BattleItem groundItem in groundInv)
			{
				// if we find the appropriate ammo, remember it for later for if we find
				// the right weapon but with the wrong ammo
				RuleItem groundItemRule = groundItem.Rules;

				bool skipAmmo = false;
				for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
				{
					if (needsAmmo[slot] && matchedAmmo[slot] == null && targetAmmo[slot] == groundItemRule)
					{
						matchedAmmo[slot] = groundItem;
						skipAmmo = true;
					}
				}
				if (skipAmmo)
				{
					continue;
				}

				if (equipmentLayoutItem.IsFixed == false && equipmentLayoutItem.ItemType == groundItemRule)
				{
					// if the loaded ammo doesn't match the template item's,
					// remember the weapon for later and continue scanning
					bool skipWeapon = false;
					for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
					{
						if (!groundItem.NeedsAmmoForSlot(slot))
						{
							continue;
						}
						BattleItem loadedAmmo = groundItem.GetAmmoForSlot(slot);
						if ((needsAmmo[slot] && (loadedAmmo == null || targetAmmo[slot] != loadedAmmo.Rules))
							|| (!needsAmmo[slot] && loadedAmmo != null))
						{
							// remember the last matched weapon for simplicity (but prefer empty weapons if any are found)
							if (matchedWeapon == null || matchedWeapon.GetAmmoForSlot(slot) != null)
							{
								matchedWeapon = groundItem;
							}
							skipWeapon = true;
						}
					}
					if (!skipWeapon)
					{
						matchedWeapon = groundItem;
						found = true; // found = true, even if not equipped
						break;
					}
				}
			}

			if (equipmentLayoutItem.IsFixed)
			{
				foreach (BattleItem fixedItem in unit.Inventory)
				{
					if (fixedItem.Rules.IsFixed == false)
					{
						// this is not a fixed item, continue searching...
						continue;
					}
					if (fixedItem.Slot == equipmentLayoutItem.Slot &&
						fixedItem.SlotX == equipmentLayoutItem.SlotX &&
						fixedItem.SlotY == equipmentLayoutItem.SlotY &&
						fixedItem.Rules == equipmentLayoutItem.ItemType)
					{
						// if the loaded ammo doesn't match the template item's,
						// remember the weapon for later and continue scanning
						bool skipWeapon = false;
						for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
						{
							if (!fixedItem.NeedsAmmoForSlot(slot))
							{
								continue;
							}
							BattleItem loadedAmmo = fixedItem.GetAmmoForSlot(slot);
							if ((needsAmmo[slot] && (loadedAmmo == null || targetAmmo[slot] != loadedAmmo.Rules))
								|| (!needsAmmo[slot] && loadedAmmo != null))
							{
								// remember the last matched weapon for simplicity (but prefer empty weapons if any are found)
								if (matchedWeapon == null || matchedWeapon.GetAmmoForSlot(slot) != null)
								{
									matchedWeapon = fixedItem;
								}
								skipWeapon = true;
							}
						}
						if (!skipWeapon)
						{
							matchedWeapon = fixedItem;
							found = true; // found = true, even if not equipped
							break;
						}
					}
				}
			}

			// if we failed to find an exact match, but found unloaded ammo and
			// the right weapon, unload the target weapon, load the right ammo, and use it
			if (!found && matchedWeapon != null)
			{
				found = true;
				bool allMatch = true;
				for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
				{
					allMatch &= (needsAmmo[slot] && matchedAmmo[slot] != null) || (!needsAmmo[slot]);
				}
				if (allMatch)
				{
					for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
					{
						if (matchedWeapon.NeedsAmmoForSlot(slot) && (!needsAmmo[slot] || matchedAmmo[slot] != null))
						{
							// unload the existing ammo (if any) from the weapon
							BattleItem loadedAmmo = matchedWeapon.SetAmmoForSlot(slot, matchedAmmo[slot]);
							if (loadedAmmo != null)
							{
								_battleGame.TileEngine.ItemDrop(groundTile, loadedAmmo, false);
							}
						}
					}
				}
				else
				{
					// nope we can't do it.
					found = false;
					matchedWeapon = null;
				}
			}

			if (!found)
			{
				itemMissing = true;
			}

			if (equipmentLayoutItem.IsFixed)
			{
				// we have loaded the fixed weapon (if possible) and we don't need to do anything else, it's already in the correct slot
				continue;
			}

			// check if the slot is not occupied already (e.g. by a fixed weapon)
			if (matchedWeapon != null && !Inventory.OverlapItems(
				unit,
				matchedWeapon,
				equipmentLayoutItem.Slot,
				equipmentLayoutItem.SlotX,
				equipmentLayoutItem.SlotY))
			{
				// move matched item from ground to the appropriate inventory slot
				matchedWeapon.MoveToOwner(unit);
				matchedWeapon.Slot = equipmentLayoutItem.Slot;
				matchedWeapon.SlotX = equipmentLayoutItem.SlotX;
				matchedWeapon.SlotY = equipmentLayoutItem.SlotY;
				matchedWeapon.FuseTimer = equipmentLayoutItem.FuseTimer;
			}
			else
			{
				// let the user know or not? probably not... should be obvious why
			}
		}

		if (itemMissing)
		{
			_inv.ShowWarning(Tr("STR_NOT_ENOUGH_ITEMS_FOR_TEMPLATE"));
		}
	}

	/// <summary>Handler for clicking the Apply Template button.</summary>
	public void BtnApplyTemplateClick(Action action)
	{
		// don't accept clicks when moving items
		// it's ok if the template is empty -- it will just result in clearing the
		// unit's inventory
		if (_inv.SelectedItem != null)
		{
			return;
		}

		ApplyInventoryTemplate(_curInventoryTemplate);

		// refresh ui
		_inv.ArrangeGround();
		UpdateStats();
		RefreshMouse();

		// give audio feedback
		PlayDropSound();
	}

	/// <summary>Applies the personal equipment template.</summary>
	public void BtnApplyPersonalTemplateClick(Action action)
	{
		// cannot use this feature during the mission!
		if (_tu)
		{
			return;
		}

		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		BattleUnit unit = _battleGame.SelectedUnit;
		if (unit != null && unit.GeoscapeSoldier != null)
		{
			// optionally load armor too
			if (Options.oxcePersonalLayoutIncludingArmor)
			{
				Armor newArmor = unit.GeoscapeSoldier.PersonalEquipmentArmor;
				if (newArmor != null && newArmor != unit.Armor)
				{
					bool success = TryArmorChange(newArmor.Type);

					if (success)
					{
						_reloadUnit = true;
						Init();
					}
					else
					{
						// FIXME: a better message? or no message?
						//_inv->showWarning(tr("STR_NOT_ENOUGH_ITEMS_FOR_TEMPLATE"));
					}
				}
			}

			List<EquipmentLayoutItem> personalTemplate = unit.GeoscapeSoldier.PersonalEquipmentLayout;

			if (personalTemplate.Count == 0)
			{
				_inv.ShowWarning(Tr("STR_PERSONAL_EQUIPMENT_NOT_DEFINED"));
			}
			else
			{
				ApplyInventoryTemplate(personalTemplate);
			}

			// refresh ui
			_inv.ArrangeGround();
			UpdateStats();
			RefreshMouse();

			// give audio feedback
			PlayDropSound();
		}
	}

	/// <summary>Shows the personal equipment template.</summary>
	public void BtnShowPersonalTemplateClick(Action action)
	{
		// don't accept clicks when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		BattleUnit unit = _battleGame.SelectedUnit;
		if (unit != null && unit.GeoscapeSoldier != null)
		{
			_game.PushState(new InventoryPersonalState(unit.GeoscapeSoldier));
		}
	}

	/// <summary>Refresh the hover status of the mouse.</summary>
	private static void RefreshMouse()
	{
		// C++ sends a mouse motion event to refresh any hover actions:
		// SDL_WarpMouse(x+1, y) and back to (x, y) to avoid cursor creep.
		// missing: the host cannot warp the pointer yet.
	}

	/// <summary>Handler for hitting the Clear Inventory hotkey.</summary>
	public void OnClearInventory(Action action)
	{
		// don't act when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		BattleUnit unit = _battleGame.SelectedUnit;
		Tile groundTile = unit.Tile;

		_battleGame.TileEngine.ItemDropInventory(groundTile, unit, true, false);

		// refresh ui
		_inv.ArrangeGround();
		UpdateStats();
		RefreshMouse();

		// give audio feedback
		PlayDropSound();
	}

	/// <summary>Handler for hitting the Auto-equip hotkey.</summary>
	public void OnAutoequip(Action action)
	{
		// don't act when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		BattleUnit unit = _battleGame.SelectedUnit;
		Tile groundTile = unit.Tile;
		List<BattleItem> groundInv = new List<BattleItem>(groundTile.Inventory); // C++ copies the vector
		Mod mod = _game.Mod;
		RuleInventory groundRuleInv = mod.InventoryGround;
		int worldShade = _battleGame.GlobalShade;

		List<BattleUnit> units = [unit];
		BattlescapeGenerator.AutoEquip(units, mod, groundInv, groundRuleInv, worldShade, true, true);

		// refresh ui
		_inv.ArrangeGround();
		UpdateStats();
		RefreshMouse();

		// give audio feedback
		PlayDropSound();
	}

	/// <summary>Updates item info.</summary>
	public void InvClick(Action action)
	{
		UpdateStats();
		_prev_key = 0;
		_key_repeats = 0;
	}

	/// <summary>Calculates item damage info.</summary>
	public void CalculateCurrentDamageTooltip()
	{
		// Differences against battlescape indicator:
		// 1. doesn't consider which action (auto/snap/aim/melee) is used... just takes ammo from primary slot
		// 2. doesn't show psi success chance (distance is unknown)
		// 3. doesn't consider range power reduction (distance is unknown)

		BattleUnit currentUnit = _inv.SelectedUnit;

		if (currentUnit == null)
			return;

		if (_currentDamageTooltipItem == null)
			return;

		BattleItem damageItem = _currentDamageTooltipItem;
		RuleItem weaponRule = _currentDamageTooltipItem.Rules;
		const int PRIMARY_SLOT = 0;

		// step 1: determine rule
		RuleItem rule;
		if (weaponRule.BattleType == BT_PSIAMP)
		{
			rule = weaponRule;
		}
		else if (_currentDamageTooltipItem.NeedsAmmoForSlot(PRIMARY_SLOT))
		{
			BattleItem ammo = _currentDamageTooltipItem.GetAmmoForSlot(PRIMARY_SLOT);
			if (ammo != null)
			{
				damageItem = ammo;
				rule = ammo.Rules;
			}
			else
			{
				rule = null; // empty weapon = no rule
			}
		}
		else
		{
			rule = weaponRule;
		}

		// step 2: check if unlocked
		if (_game.SavedGame.MonthsPassed == -1)
		{
			// new battle mode
		}
		else if (rule != null)
		{
			// instead of checking the weapon/ammo itself... we're checking their ufopedia articles here
			// same as for the battlescape indicator
			// it's arguable if this is the correct approach, but so far this is what we have
			ArticleDefinition article = _game.Mod.GetUfopaediaArticle(rule.Type, false);
			if (article != null && !Ufopaedia.IsArticleAvailable(_game.SavedGame, article))
			{
				// ammo/weapon locked
				rule = null;
			}
			if (rule != null && rule.Type != weaponRule.Type)
			{
				article = _game.Mod.GetUfopaediaArticle(weaponRule.Type, false);
				if (article != null && !Ufopaedia.IsArticleAvailable(_game.SavedGame, article))
				{
					// weapon locked
					rule = null;
				}
			}
		}

		// step 3: calculate and remember
		if (rule != null)
		{
			if (rule.BattleType != BT_CORPSE)
			{
				int totalDamage = 0;
				if (weaponRule.IgnoreAmmoPower)
				{
					totalDamage += weaponRule.GetPowerBonus(new BattleActionAttack(BA_NONE, currentUnit, _currentDamageTooltipItem, damageItem));
				}
				else
				{
					totalDamage += rule.GetPowerBonus(new BattleActionAttack(BA_NONE, currentUnit, _currentDamageTooltipItem, damageItem)); //TODO: find what exactly attack we can do
				}
				//totalDamage -= rule->getPowerRangeReduction(distance * 16);
				if (totalDamage < 0) totalDamage = 0;
				var ss = new StringBuilder();
				ss.Append(rule.DamageType.GetRandomDamage(totalDamage, 1).ToString(CultureInfo.InvariantCulture));
				ss.Append('-');
				ss.Append(rule.DamageType.GetRandomDamage(totalDamage, 2).ToString(CultureInfo.InvariantCulture));
				if (rule.DamageType.RandomType == DRT_UFO_WITH_TWO_DICE)
					ss.Append('*');
				_currentDamageTooltip = Tr("STR_DAMAGE_UC_").Arg(ss.ToString());
			}
		}
		else
		{
			_currentDamageTooltip = Tr("STR_DAMAGE_UC_").Arg(Tr("STR_UNKNOWN"));
		}
	}

	/// <summary>Shows item info.</summary>
	public void InvMouseOver(Action action)
	{
		if (_inv.SelectedItem != null)
		{
			return;
		}

		bool altPressed = _game.IsAltPressed();
		bool currentDamageTooltipItemChanged = false;

		BattleItem item = _inv.MouseOverItem;
		if (item != _mouseHoverItem)
		{
			_mouseHoverItemFrame = _inv.AnimFrame;
			_mouseHoverItem = item;
		}
		if (altPressed)
		{
			if (item != _currentDamageTooltipItem)
			{
				currentDamageTooltipItemChanged = true;
				_currentDamageTooltipItem = item;
				_currentDamageTooltip = "";
			}
		}
		else
		{
			_currentDamageTooltipItem = null;
			_currentDamageTooltip = "";
		}
		if (item != null)
		{
			string itemName;
			if (item.Unit != null && item.Unit.Status == STATUS_UNCONSCIOUS)
			{
				itemName = item.Unit.GetName(_game.Language);
			}
			else
			{
				SavedGame save = _game.SavedGame;
				if (save.IsResearched(item.Rules.Requirements))
				{
					string text = Tr(item.Rules.Name);
					for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
					{
						if (!item.NeedsAmmoForSlot(slot))
						{
							continue;
						}

						BattleItem ammo = item.GetAmmoForSlot(slot);
						if (ammo == null || !save.IsResearched(ammo.Rules.Requirements))
						{
							continue;
						}

						string ammoName = ammo.Rules.NameAsAmmo;
						if (!string.IsNullOrEmpty(ammoName))
						{
							text += " ";
							text += Tr(ammoName);
						}
					}
					if (altPressed)
					{
						if (currentDamageTooltipItemChanged)
						{
							CalculateCurrentDamageTooltip();
						}
						itemName = _currentDamageTooltip;
					}
					else
					{
						itemName = text;
					}
				}
				else
				{
					itemName = Tr("STR_ALIEN_ARTIFACT");
				}
			}

			if (!altPressed)
			{
				_txtItem.SetText(itemName + " [" + item.TotalWeight.ToString(CultureInfo.InvariantCulture) + "]");
			}
			else
			{
				_txtItem.SetText(itemName);
			}

			_selAmmo.Clear();
			bool hasSelfAmmo = item.Rules.BattleType != BT_AMMO && item.Rules.ClipSize > 0;
			if ((item.IsWeaponWithAmmo || hasSelfAmmo) && item.HaveAnyAmmo())
			{
				UpdateTemplateButtons(false);
				_txtAmmo.SetText("");
			}
			else
			{
				_mouseHoverItem = null;
				UpdateTemplateButtons(!_tu);
				string s = "";
				if (item.Rules.BattleType == BT_AMMO && (item.AmmoQuantity != 0 || item.Rules.IsAmmoRechargeable))
				{
					s = Tr("STR_AMMO_ROUNDS_LEFT").Arg(item.AmmoQuantity);
				}
				else if (item.Rules.BattleType == BT_MEDIKIT)
				{
					s = Tr("STR_MEDI_KIT_QUANTITIES_LEFT").Arg(item.PainKillerQuantity).Arg(item.StimulantQuantity).Arg(item.HealQuantity);
				}
				_txtAmmo.SetText(s);
			}
		}
		else
		{
			if (string.IsNullOrEmpty(_currentTooltip))
			{
				_txtItem.SetText("");
			}
			_txtAmmo.SetText("");
			_selAmmo.Clear();
			UpdateTemplateButtons(!_tu);
		}
	}

	/// <summary>Hides item info.</summary>
	public void InvMouseOut(Action action)
	{
		_txtItem.SetText("");
		_txtAmmo.SetText("");
		_selAmmo.Clear();
		_inv.MouseOverItem = null;
		_mouseHoverItem = null;
		_currentDamageTooltipItem = null;
		_currentDamageTooltip = "";
		UpdateTemplateButtons(!_tu);
	}

	/// <summary>Handler for hitting the [Move Ground Inventory To Base] hotkey.</summary>
	public void OnMoveGroundInventoryToBase(Action action)
	{
		// don't act when moving items
		if (_inv.SelectedItem != null)
		{
			return;
		}

		if (_base == null)
		{
			// equipment just before the mission (=after briefing) or during the mission
			return;
		}

		if (_noCraft)
		{
			// pre-equipping in the base, but *without* a craft
			return;
		}

		// ok, which craft?
		BattleUnit unit = _battleGame.SelectedUnit;
		Soldier s = unit.GeoscapeSoldier;
		Craft c = s.Craft;

		if (c == null || c.Status == "STR_OUT")
		{
			// we're either not in a craft or not in a hangar (should not happen, but just in case)
			return;
		}

		Tile groundTile = unit.Tile;
		List<BattleItem> groundInv = groundTile.Inventory;

		// step 1: move stuff from craft to base
		foreach (BattleItem bi in groundInv)
		{
			RuleItem weaponType = bi.Rules;
			// check all ammo slots first
			for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
			{
				if (bi.GetAmmoForSlot(slot) != null)
				{
					RuleItem ammoType = bi.GetAmmoForSlot(slot).Rules;
					// only real ammo
					if (weaponType != ammoType)
					{
						c.Items.RemoveItem(ammoType);
						_base.StorageItems.AddItem(ammoType);
					}
				}
			}
			// and the weapon as last
			c.Items.RemoveItem(weaponType);
			_base.StorageItems.AddItem(weaponType);
		}

		// step 2: clear ground
		while (groundInv.Count > 0)
		{
			BattleItem item = groundInv[0];
			item.Owner = null;
			groundInv.RemoveAt(0);
			_game.SavedGame.SavedBattle.RemoveItem(item);
		}

		// refresh ui
		_inv.ArrangeGround();
		UpdateStats();
		RefreshMouse();

		// give audio feedback
		PlayDropSound();
	}

	/// <summary>Takes care of any events from the core game engine.</summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.Handle(action);

		if (action.Details.type == SDLEventType.SDL_KEYDOWN && !_btnQuickSearch.IsFocused && !_txtName.IsFocused)
		{
			// "ctrl+1..9" - save equipment
			// "1..9" - load equipment
			if (action.Details.key.keysym.sym >= SDLKey.SDLK_0 && action.Details.key.keysym.sym <= SDLKey.SDLK_9)
			{
				{
					BtnGlobalEquipmentLayoutClick(action);
				}
			}
			else
			{
				_prev_key = 0;
				_key_repeats = 0;
			}

			if (action.Details.key.keysym.sym == Options.keyInvClear)
			{
				if (_game.IsCtrlPressed() && _game.IsAltPressed())
				{
					OnMoveGroundInventoryToBase(action);
				}
			}
		}

		if (Options.oxceThumbButtons && action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
		{
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_X1)
			{
				BtnNextClick(action);
			}
			else if (action.Details.button.button == SDLMouse.SDL_BUTTON_X2)
			{
				BtnPrevClick(action);
			}
		}
	}

	/// <summary>Cycle through loaded ammo in hover over item.</summary>
	public override void Think()
	{
		if (_mouseHoverItem != null)
		{
			int anim = _inv.AnimFrame;
			int seq = Math.Max(((anim - _mouseHoverItemFrame) / 10) - 1, 0); // `-1` cause that first item will be show bit more longer
			int modulo = 0;
			for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
			{
				bool showSelfAmmo = slot == 0 && _mouseHoverItem.Rules.ClipSize > 0;
				if ((_mouseHoverItem.NeedsAmmoForSlot(slot) || showSelfAmmo) && _mouseHoverItem.GetAmmoForSlot(slot) != null)
				{
					++modulo;
				}
			}
			if (modulo != 0)
			{
				seq %= modulo;
			}

			BattleItem firstAmmo = null;
			for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
			{
				bool showSelfAmmo = slot == 0 && _mouseHoverItem.Rules.ClipSize > 0;
				if ((_mouseHoverItem.NeedsAmmoForSlot(slot) || showSelfAmmo) && _mouseHoverItem.GetAmmoForSlot(slot) != null)
				{
					firstAmmo = _mouseHoverItem.GetAmmoForSlot(slot);
					if (slot >= seq)
					{
						break;
					}
				}
				else
				{
					// this will skip empty slot
					++seq;
				}
			}
			if (firstAmmo != null)
			{
				_txtAmmo.SetText(Tr("STR_AMMO_ROUNDS_LEFT").Arg(firstAmmo.AmmoQuantity));
				SDL_Rect r;
				r.x = 0;
				r.y = 0;
				r.w = RuleInventory.HAND_W * RuleInventory.SLOT_W;
				r.h = RuleInventory.HAND_H * RuleInventory.SLOT_H;
				_selAmmo.DrawRect(ref r, (byte)_game.Mod.GetInterface("inventory").GetElement("grid").color);
				r.x++;
				r.y++;
				r.w -= 2;
				r.h -= 2;
				_selAmmo.DrawRect(ref r, (byte)(Acom.Palette.BlockOffset(0) + 15));
				firstAmmo.Rules.DrawHandSprite(_game.Mod.GetSurfaceSet("BIGOBS.PCK"), _selAmmo, firstAmmo, _game.SavedGame.SavedBattle, anim);
			}
			else
			{
				_selAmmo.Clear();
			}
		}
		base.Think();
	}

	/// <summary>Shows a tooltip for the OK button.</summary>
	public void TxtTooltipInExtraOK(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_inv.SelectedItem == null && Options.battleTooltips)
		{
			_currentTooltip = action.Sender.Tooltip;

			var ss = new StringBuilder();
			ss.Append(Tr(_currentTooltip).ToString());

			if (!_tu && _base == null)
			{
				ss.Append(" - ");

				if (_battleGame.GlobalShade <= 0)
				{
					// day (0)
					ss.Append(Tr("STR_DAY").ToString());
				}
				else if (_battleGame.GlobalShade > _game.Mod.MaxDarknessToSeeUnits)
				{
					// night (10-15); note: this is configurable in the ruleset (in OXCE only)
					ss.Append(Tr("STR_NIGHT").ToString());
				}
				else
				{
					// dusk/dawn (1-9)
					ss.Append(Tr("STR_DAY").ToString());
					ss.Append('*');
				}
			}

			_txtItem.SetText(ss.ToString());
		}
	}

	/// <summary>Shows a tooltip for the appropriate button.</summary>
	public void TxtTooltipIn(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_inv.SelectedItem == null && Options.battleTooltips)
		{
			_currentTooltip = action.Sender.Tooltip;
			_txtItem.SetText(Tr(_currentTooltip));
		}
	}

	/// <summary>Clears the tooltip text.</summary>
	public void TxtTooltipOut(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_inv.SelectedItem == null && Options.battleTooltips)
		{
			if (_currentTooltip == action.Sender.Tooltip)
			{
				_currentTooltip = "";
				_txtItem.SetText("");
			}
		}
	}

	/// <summary>Shows a tooltip for the paperdoll's armor.</summary>
	public void TxtArmorTooltipIn(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_inv.SelectedItem == null)
		{
			BattleUnit unit = _inv.SelectedUnit;
			if (unit != null)
			{
				action.Sender.Tooltip = unit.Armor.Type;
				_currentTooltip = action.Sender.Tooltip;
				{
					var ss = new StringBuilder();

					if (unit.GeoscapeSoldier != null)
					{
						RuleSoldier soldierRules = unit.GeoscapeSoldier.Rules;
						if (soldierRules.ShowTypeInInventory)
						{
							ss.Append(Tr(soldierRules.Type).ToString());
							ss.Append(": ");
						}
					}

					ss.Append(Tr(_currentTooltip).ToString());
					if (unit.Armor.Weight != 0)
					{
						ss.Append(" [");
						ss.Append(unit.Armor.Weight.ToString(CultureInfo.InvariantCulture));
						ss.Append(']');
					}
					_txtItem.SetText(ss.ToString());
				}
			}
		}
	}

	/// <summary>Clears the armor tooltip text.</summary>
	public void TxtArmorTooltipOut(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_inv.SelectedItem == null)
		{
			if (_currentTooltip == action.Sender.Tooltip)
			{
				_currentTooltip = "";
				_txtItem.SetText("");
			}
		}
	}

	/// <summary>Update the visibility and icons for the template buttons.</summary>
	private void UpdateTemplateButtons(bool isVisible)
	{
		if (isVisible)
		{
			if (_curInventoryTemplate.Count == 0)
			{
				// use "empty template" icons
				_game.Mod.GetSurface("InvCopy").BlitNShade(_btnCreateTemplate, 0, 0);
				_game.Mod.GetSurface("InvPasteEmpty").BlitNShade(_btnApplyTemplate, 0, 0);
				_btnApplyTemplate.Tooltip = "STR_CLEAR_INVENTORY";
			}
			else
			{
				// use "active template" icons
				_game.Mod.GetSurface("InvCopyActive").BlitNShade(_btnCreateTemplate, 0, 0);
				_game.Mod.GetSurface("InvPaste").BlitNShade(_btnApplyTemplate, 0, 0);
				_btnApplyTemplate.Tooltip = "STR_APPLY_INVENTORY_TEMPLATE";
			}
			_btnCreateTemplate.InitSurfaces();
			_btnApplyTemplate.InitSurfaces();
		}
		else
		{
			_btnCreateTemplate.Clear();
			_btnApplyTemplate.Clear();
		}
	}

	/// <summary>Plays the item drop sound at the battle depth.</summary>
	private void PlayDropSound()
	{
		_game.Mod.GetSoundByDepth((uint)_battleGame.Depth, (uint)Mod.ITEM_DROP).Play();
	}
}
