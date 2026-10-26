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
// Ported from: src/Battlescape/ExtendedInventoryLinksState.h, src/Battlescape/ExtendedInventoryLinksState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Window with extended links (shortcuts) for the inventory screen.
/// </summary>
public class ExtendedInventoryLinksState : State
{
	private readonly TextButton _btnOk;
	private readonly TextButton _btnArmor, _btnAvatar, _btnEquipmentSave, _btnEquipmentLoad, _btnPersonalSave, _btnPersonalLoad, _btnNotes, _btnUfopedia, _btnAutoEquip;
	private readonly Window _window;
	private readonly Text _txtTitle;
	private readonly InventoryState _parent;
	private readonly SavedBattleGame _save;

	/// <summary>Initializes all the elements in the ExtendedInventoryLinksState screen.</summary>
	public ExtendedInventoryLinksState(InventoryState parent, SavedBattleGame save, bool inBase, bool beforeMission)
	{
		_parent = parent;
		_save = save;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, inBase ? 180 : 180 - 14, 32, inBase ? 10 : 10 + 14, WindowPopup.POPUP_BOTH);
		_txtTitle = new Text(220, 17, 50, inBase ? 33 : 33 + 23);
		if (Options.oxceFatFingerLinks)
		{
			_btnArmor = new TextButton(116, 25, 44, 50);
			_btnAvatar = new TextButton(116, 25, 161, 50);
			_btnEquipmentSave = new TextButton(116, 25, 44, 76);
			_btnEquipmentLoad = new TextButton(116, 25, 161, 76);
			_btnPersonalSave = new TextButton(116, 25, 44, 102);
			_btnPersonalLoad = new TextButton(116, 25, 161, 102);
			_btnNotes = new TextButton(116, 25, 44, 128);
			_btnUfopedia = new TextButton(116, 25, 161, 128);
			_btnAutoEquip = new TextButton(116, 25, 44, 154);
			_btnOk = new TextButton(116, 25, 161, 154);
		}
		else
		{
			_btnArmor = new TextButton(220, 12, 50, 50);
			_btnAvatar = new TextButton(220, 12, 50, 63);
			_btnEquipmentSave = new TextButton(220, 12, 50, 76);
			_btnEquipmentLoad = new TextButton(220, 12, 50, 89);
			_btnPersonalSave = new TextButton(220, 12, 50, 102);
			_btnPersonalLoad = new TextButton(220, 12, 50, 115);
			_btnNotes = new TextButton(220, 12, 50, 128);
			_btnUfopedia = new TextButton(220, 12, 50, 141);
			_btnAutoEquip = new TextButton(220, 12, 50, 154);
			_btnOk = new TextButton(220, 12, 50, 167);
		}

		// Set palette
		SetInterface("oxceLinks", false, _save);

		Add(_window, "window", "oxceLinks");
		Add(_txtTitle, "text", "oxceLinks");
		Add(_btnOk, "button", "oxceLinks");

		Add(_btnArmor, "button", "oxceLinks");
		Add(_btnAvatar, "button", "oxceLinks");
		Add(_btnEquipmentSave, "button", "oxceLinks");
		Add(_btnEquipmentLoad, "button", "oxceLinks");
		Add(_btnPersonalSave, "button", "oxceLinks");
		Add(_btnPersonalLoad, "button", "oxceLinks");
		Add(_btnNotes, "button", "oxceLinks");
		Add(_btnUfopedia, "button", "oxceLinks");
		Add(_btnAutoEquip, "button", "oxceLinks");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "oxceLinks");

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_EXTENDED_LINKS"));

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnArmor.Text = Tr("STR_INVENTORY_ARMOR");
		_btnArmor.OnMouseClick(BtnArmorClick);
		_btnArmor.Visible = inBase;

		_btnAvatar.Text = Tr("STR_INVENTORY_AVATAR");
		_btnAvatar.OnMouseClick(BtnAvatarClick);
		_btnAvatar.Visible = inBase;

		_btnEquipmentSave.Text = Tr("STR_SAVE_EQUIPMENT_TEMPLATE");
		_btnEquipmentSave.OnMouseClick(BtnEquipmentSaveClick);
		_btnEquipmentSave.Visible = beforeMission;

		_btnEquipmentLoad.Text = Tr("STR_LOAD_EQUIPMENT_TEMPLATE");
		_btnEquipmentLoad.OnMouseClick(BtnEquipmentLoadClick);
		_btnEquipmentLoad.Visible = beforeMission;

		_btnPersonalSave.Text = Tr("STR_SAVE_PERSONAL_EQUIPMENT");
		_btnPersonalSave.OnMouseClick(BtnPersonalSaveClick);
		_btnPersonalSave.Visible = beforeMission;

		_btnPersonalLoad.Text = Tr("STR_LOAD_PERSONAL_EQUIPMENT");
		_btnPersonalLoad.OnMouseClick(BtnPersonalLoadClick);
		_btnPersonalLoad.Visible = beforeMission;

		_btnNotes.Text = Tr("STR_NOTES");
		_btnNotes.OnMouseClick(BtnNotesClick);

		_btnUfopedia.Text = Tr("STR_UFOPAEDIA");
		_btnUfopedia.OnMouseClick(BtnUfopediaClick);

		_btnAutoEquip.Text = Tr("STR_AUTO_EQUIP");
		_btnAutoEquip.OnMouseClick(BtnAutoEquipClick);
		_btnAutoEquip.Visible = beforeMission;

		ApplyBattlescapeTheme("oxceLinks");
	}

	/// <summary>Opens the armor selection.</summary>
	public void BtnArmorClick(Action action)
	{
		_game.PopState();
		_parent.BtnArmorClick(null);
	}

	/// <summary>Opens the avatar selection.</summary>
	public void BtnAvatarClick(Action action)
	{
		_game.PopState();
		_parent.BtnArmorClickRight(null);
	}

	/// <summary>Opens the equipment template save window.</summary>
	public void BtnEquipmentSaveClick(Action action)
	{
		_game.PopState();
		_parent.BtnInventorySaveClick(null);
	}

	/// <summary>Opens the equipment template load window.</summary>
	public void BtnEquipmentLoadClick(Action action)
	{
		_game.PopState();
		_parent.BtnInventoryLoadClick(null);
	}

	/// <summary>Saves the personal equipment.</summary>
	public void BtnPersonalSaveClick(Action action)
	{
		_game.PopState();
		_parent.BtnCreatePersonalTemplateClick(null);
	}

	/// <summary>Loads the personal equipment.</summary>
	public void BtnPersonalLoadClick(Action action)
	{
		_game.PopState();
		_parent.BtnApplyPersonalTemplateClick(null);
	}

#pragma warning disable CA1822 // instance handler in C++ (State._game is static here)
	/// <summary>Opens the notes.</summary>
	public void BtnNotesClick(Action action)
	{
		_game.PopState();
		_game.PushState(new NotesState(OptionsOrigin.OPT_BATTLESCAPE));
	}
#pragma warning restore CA1822

	/// <summary>Opens the Ufopaedia.</summary>
	public void BtnUfopediaClick(Action action)
	{
		_game.PopState();
		_parent.BtnUfopaediaClick(null);
	}

	/// <summary>Auto-equips the selected unit.</summary>
	public void BtnAutoEquipClick(Action action)
	{
		_game.PopState();
		_parent.OnAutoequip(null);
	}

#pragma warning disable CA1822 // instance handler in C++ (State._game is static here)
	/// <summary>Returns to the previous screen.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}
#pragma warning restore CA1822
}
