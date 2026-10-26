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
// Ported from: src/Basescape/CraftWeaponsState.h, src/Basescape/CraftWeaponsState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Select Armament window that allows changing
/// of the armament equipped on a craft.
/// </summary>
public class CraftWeaponsState : State
{
	private readonly Base _base;
	private readonly Craft _craft;
	private readonly int _weapon;

	private readonly TextButton _btnCancel;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtArmament, _txtQuantity, _txtAmmunition, _txtCurrentWeapon;
	private readonly TextList _lstWeapons;
	private readonly List<RuleCraftWeapon> _weapons = [];

	private static string Str(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Initializes all the elements in the Craft Weapons window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="craft">ID of the selected craft.</param>
	/// <param name="weapon">ID of the selected weapon.</param>
	public CraftWeaponsState(Base @base, int craft, int weapon)
	{
		_base = @base;
		_craft = @base.Crafts[craft];
		_weapon = weapon;
		_screen = false;

		// Create objects
		_window = new Window(this, 220, 160, 50, 20, WindowPopup.POPUP_BOTH);
		_btnCancel = new TextButton(140, 16, 90, 156);
		_txtTitle = new Text(208, 17, 56, 28);
		_txtArmament = new Text(76, 9, 66, 52);
		_txtQuantity = new Text(50, 9, 140, 52);
		_txtAmmunition = new Text(68, 17, 200, 44);
		_lstWeapons = new TextList(188, 64, 58, 68);
		_txtCurrentWeapon = new Text(188, 9, 66, 140);

		// Set palette
		SetInterface("craftWeapons");

		Add(_window, "window", "craftWeapons");
		Add(_btnCancel, "button", "craftWeapons");
		Add(_txtTitle, "text", "craftWeapons");
		Add(_txtArmament, "text", "craftWeapons");
		Add(_txtQuantity, "text", "craftWeapons");
		Add(_txtAmmunition, "text", "craftWeapons");
		Add(_lstWeapons, "list", "craftWeapons");
		Add(_txtCurrentWeapon, "text", "craftWeapons");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftWeapons");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELECT_ARMAMENT"));

		_txtArmament.SetText(Tr("STR_ARMAMENT"));

		_txtQuantity.SetText(Tr("STR_QUANTITY_UC"));

		_txtAmmunition.SetText(Tr("STR_AMMUNITION_AVAILABLE"));
		_txtAmmunition.SetWordWrap(true);
		_txtAmmunition.VerticalAlign = TextVAlign.ALIGN_BOTTOM;

		string slotName = _craft.Rules.GetWeaponSlotString(weapon);
		CraftWeapon current = _craft.Weapons[_weapon];
		if (current != null)
		{
			_txtCurrentWeapon.SetText(Tr(slotName).Arg(Tr(current.Rules.Type)));
		}
		else
		{
			_txtCurrentWeapon.SetText(Tr(slotName).Arg(Tr("STR_NONE_UC")));
		}

		_lstWeapons.SetColumns(3, 94, 50, 36);
		_lstWeapons.SetSelectable(true);
		_lstWeapons.SetBackground(_window);
		_lstWeapons.Margin = 8;

		_lstWeapons.AddRow(1, Tr("STR_NONE_UC"));
		_weapons.Add(null);

		foreach (var craftWeaponType in _game.Mod.CraftWeaponsList)
		{
			RuleCraftWeapon w = _game.Mod.GetCraftWeapon(craftWeaponType);
			RuleCraft c = _craft.Rules;
			bool isResearched = _game.SavedGame.IsResearched(w.LauncherItem.Requirements);
			if (w.ClipItem != null)
			{
				isResearched = isResearched && _game.SavedGame.IsResearched(w.ClipItem.Requirements);
			}
			if (isResearched && _base.StorageItems.GetItem(w.LauncherItem) > 0 && c.IsValidWeaponSlot(weapon, w.WeaponType))
			{
				_weapons.Add(w);
				string ss = Str(_base.StorageItems.GetItem(w.LauncherItem));
				string ss2;
				if (w.ClipItem != null)
				{
					ss2 = Str(_base.StorageItems.GetItem(w.ClipItem));
				}
				else
				{
					ss2 = Tr("STR_NOT_AVAILABLE");
				}
				_lstWeapons.AddRow(3, Tr(w.Type), ss, ss2);
			}
		}
		_lstWeapons.OnMouseClick(LstWeaponsClick);
		_lstWeapons.OnMouseClick(LstWeaponsMiddleClick, SDLMouse.SDL_BUTTON_MIDDLE);
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Shows a craft capacity error and closes this window.</summary>
	private void ShowError(string message)
	{
		_game.PopState();
		_game.PushState(new ErrorMessageState(
			Tr(message),
			_palette,
			(byte)_game.Mod.GetInterface("craftWeapons").GetElement("errorMessage").color,
			"BACK14.SCR",
			_game.Mod.GetInterface("craftWeapons").GetElement("errorPalette").color)
		);
	}

	/// <summary>
	/// Equips the weapon on the craft and returns to the previous screen.
	/// </summary>
	public void LstWeaponsClick(Action action)
	{
		CraftWeapon current = _craft.Weapons[_weapon];

		RuleCraftWeapon refWeapon = _weapons[(int)_lstWeapons.SelectedRow];
		RuleCraftWeapon currWeapon = current?.Rules;
		{
			int refCapBonus1 = refWeapon != null ? refWeapon.BonusStats.soldiers : 0;
			int currCapBonus1 = currWeapon != null ? currWeapon.BonusStats.soldiers : 0;
			int diff1 = (refCapBonus1 - currCapBonus1);
			if (diff1 != 0)
			{
				if ((_craft.MaxUnitsRaw - _craft.SpaceUsed + diff1) < 0)
				{
					ShowError("STR_NOT_ENOUGH_CARGO_SPACE");
					return;
				}
			}
		}
		{
			int refCapBonus2 = refWeapon != null ? refWeapon.BonusStats.vehicles : 0;
			int currCapBonus2 = currWeapon != null ? currWeapon.BonusStats.vehicles : 0;
			int diff2 = (refCapBonus2 - currCapBonus2);
			if (diff2 != 0)
			{
				if ((_craft.MaxVehiclesAndLargeSoldiersRaw - _craft.NumVehiclesAndLargeSoldiers + diff2) < 0)
				{
					ShowError("STR_NOT_ENOUGH_HWP_CAPACITY");
					return;
				}
			}
		}
		{
			int refCapBonus3 = refWeapon != null ? refWeapon.BonusStats.maxItems : 0;
			int currCapBonus3 = currWeapon != null ? currWeapon.BonusStats.maxItems : 0;
			int diff3 = (refCapBonus3 - currCapBonus3);

			double refCapBonus4 = refWeapon != null ? refWeapon.BonusStats.maxStorageSpace : 0.0;
			double currCapBonus4 = currWeapon != null ? currWeapon.BonusStats.maxStorageSpace : 0.0;
			double diff4 = (refCapBonus4 - currCapBonus4);
			bool diff4_b = !Target.AreSame(refCapBonus4, currCapBonus4); // floating math

			int totalItems = 0;
			double totalItemStorageSize = 0.0;
			if (diff3 != 0 || diff4_b)
			{
				foreach (var itemType in _game.Mod.ItemsList)
				{
					RuleItem rule = _game.Mod.GetItem(itemType);

					Unit isVehicle = rule.VehicleUnit;
					int cQty = 0;
					if (isVehicle != null)
					{
						cQty = _craft.GetVehicleCount(itemType);
					}
					else
					{
						cQty = _craft.Items.GetItem(rule);
						totalItems += cQty;
						totalItemStorageSize += cQty * rule.Size;
					}
				}
			}
			if (diff3 != 0)
			{
				if ((_craft.MaxItemsRaw - totalItems + diff3) < 0)
				{
					ShowError("STR_NOT_ENOUGH_STORAGE_SPACE_1");
					return;
				}
			}
			if (diff4_b)
			{
				if ((_craft.MaxStorageSpaceRaw - totalItemStorageSize + diff4) < 0.0)
				{
					ShowError("STR_NOT_ENOUGH_STORAGE_SPACE_2");
					return;
				}
			}
		}

		// Remove current weapon
		if (current != null)
		{
			_base.StorageItems.AddItem(current.Rules.LauncherItem);
			_base.StorageItems.AddItem(current.Rules.ClipItem, current.ClipsLoaded);
			_craft.AddCraftStats(-current.Rules.BonusStats);
			// Make sure any extra shield is removed from craft too when the shield capacity decreases (exploit protection)
			_craft.Shield = _craft.Shield;
			_craft.Weapons[_weapon] = null;
		}

		// Equip new weapon
		if (_weapons[(int)_lstWeapons.SelectedRow] != null)
		{
			CraftWeapon sel = new CraftWeapon(_weapons[(int)_lstWeapons.SelectedRow], 0);
			_craft.AddCraftStats(sel.Rules.BonusStats);
			_base.StorageItems.RemoveItem(sel.Rules.LauncherItem);
			_craft.Weapons[_weapon] = sel;
		}

		_craft.Checkup();
		_game.PopState();
	}

	/// <summary>
	/// Opens the corresponding Ufopaedia article.
	/// </summary>
	public void LstWeaponsMiddleClick(Action action)
	{
		RuleCraftWeapon rule = _weapons[(int)_lstWeapons.SelectedRow];

		if (rule != null)
		{
			string articleId = rule.UfopediaType;
			Ufopaedia.OpenArticle(_game, articleId);
		}
	}
}
