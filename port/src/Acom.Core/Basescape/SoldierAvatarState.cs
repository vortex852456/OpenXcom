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
// Ported from: src/Basescape/SoldierAvatarState.h, src/Basescape/SoldierAvatarState.cpp
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)
#pragma warning disable IDE0060 // handler signatures keep the unused Action parameter like C++

/// <summary>
/// Select Avatar window that allows changing
/// of the soldier's avatar.
/// </summary>
public class SoldierAvatarState : State
{
	private readonly Base _base;
	private readonly int _soldier; // C++ size_t
	private readonly TextButton _btnCancel, _btnOk;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtType;
	private readonly Surface _soldierSurface;
	private readonly TextList _lstAvatar;
	private readonly List<SoldierAvatar> _avatars = [];
	private readonly SoldierAvatar _origAvatar;

	/// <summary>
	/// Initializes all the elements in the Soldier Avatar window.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="soldier">ID of the selected soldier.</param>
	public SoldierAvatarState(Base @base, int soldier)
	{
		_base = @base;
		_soldier = soldier;
		_screen = false;

		// Create objects
		_window = new Window(this, 240, 160, 40, 24, WindowPopup.POPUP_BOTH);
		_soldierSurface = new Surface(320, 200, 0, 0);
		_btnCancel = new TextButton(100, 16, 55, 160);
		_btnOk = new TextButton(100, 16, 165, 160);
		_txtTitle = new Text(182, 16, 69, 32);
		_txtType = new Text(90, 9, 122, 58);
		_lstAvatar = new TextList(132, 80, 115, 72);

		// Set palette
		SetStandardPalette("PAL_BATTLESCAPE");

		Add(_window, "window", "soldierAvatar");
		Add(_soldierSurface);
		Add(_btnCancel, "button", "soldierAvatar");
		Add(_btnOk, "button", "soldierAvatar");
		Add(_txtTitle, "text", "soldierAvatar");
		Add(_txtType, "text", "soldierAvatar");
		Add(_lstAvatar, "list", "soldierAvatar");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "soldierAvatar");

		_btnCancel.Text = Tr("STR_CANCEL_UC");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		Soldier s = _base.Soldiers[_soldier];
		_origAvatar = new SoldierAvatar("original", s.Gender, s.Look, s.LookVariant);
		InitPreview(s);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr("STR_SELECT_AVATAR_FOR").Arg(s.GetName()));

		_txtType.SetText(Tr("STR_TYPE"));

		_lstAvatar.SetColumns(1, 125);
		_lstAvatar.SetSelectable(true);
		_lstAvatar.SetBackground(_window);
		_lstAvatar.Margin = 8;

		string prefix = "STR_AVATAR_NAME_";
		string Name(int n) => prefix + n.ToString(CultureInfo.InvariantCulture);
		for (int variant = 0; variant <= _game.Mod.MaxLookVariant; ++variant)
		{
			if (!_game.IsAltPressed())
			{
				_avatars.Add(new SoldierAvatar(Name(variant * 8 + 1), SoldierGender.GENDER_MALE, SoldierLook.LOOK_BLONDE, variant));
				_avatars.Add(new SoldierAvatar(Name(variant * 8 + 2), SoldierGender.GENDER_MALE, SoldierLook.LOOK_BROWNHAIR, variant));
				_avatars.Add(new SoldierAvatar(Name(variant * 8 + 3), SoldierGender.GENDER_MALE, SoldierLook.LOOK_ORIENTAL, variant));
				_avatars.Add(new SoldierAvatar(Name(variant * 8 + 4), SoldierGender.GENDER_MALE, SoldierLook.LOOK_AFRICAN, variant));
			}
			if (!_game.IsCtrlPressed())
			{
				_avatars.Add(new SoldierAvatar(Name(variant * 8 + 5), SoldierGender.GENDER_FEMALE, SoldierLook.LOOK_BLONDE, variant));
				_avatars.Add(new SoldierAvatar(Name(variant * 8 + 6), SoldierGender.GENDER_FEMALE, SoldierLook.LOOK_BROWNHAIR, variant));
				_avatars.Add(new SoldierAvatar(Name(variant * 8 + 7), SoldierGender.GENDER_FEMALE, SoldierLook.LOOK_ORIENTAL, variant));
				_avatars.Add(new SoldierAvatar(Name(variant * 8 + 8), SoldierGender.GENDER_FEMALE, SoldierLook.LOOK_AFRICAN, variant));
			}
		}

		foreach (var soldierAvatar in _avatars)
		{
			_lstAvatar.AddRow(1, Tr(soldierAvatar.AvatarName));
		}
		_lstAvatar.OnMouseClick(LstAvatarClick);

		// switch to battlescape theme if called from inventory
		ApplyBattlescapeTheme("soldierAvatar");
	}

	/// <summary>
	/// Shows the preview
	/// </summary>
	/// <param name="s">Soldier.</param>
	private void InitPreview(Soldier s)
	{
		_soldierSurface.Clear();

		if (s == null)
		{
			return;
		}

		if (s.Armor.HasLayersDefinition)
		{
			foreach (var layer in s.GetArmorLayers())
			{
				var surf = _game.Mod.GetSurface(layer, true);
				surf.BlitNShade(_soldierSurface, 0, 0);
			}
		}
		else
		{
			string look = s.Armor.SpriteInventory;
			string gender = s.Gender == SoldierGender.GENDER_MALE ? "M" : "F";
			Surface surf = null;

			for (int i = 0; i <= RuleSoldier.LookVariantBits; ++i)
			{
				string ss = look + gender
					+ ((int)s.Look + (s.LookVariant & (RuleSoldier.LookVariantMask >> i)) * 4).ToString(CultureInfo.InvariantCulture)
					+ ".SPK";
				surf = _game.Mod.GetSurface(ss, false);
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
			surf.BlitNShade(_soldierSurface, 0, 0);
		}
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		Soldier soldier = _base.Soldiers[_soldier];

		// revert the avatar to original
		soldier.Gender = _origAvatar.Gender;
		soldier.Look = _origAvatar.Look;
		soldier.LookVariant = _origAvatar.LookVariant;

		_game.PopState();
	}

	/// <summary>
	/// Changes the avatar on the soldier and returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Changes the avatar on the soldier and returns to the previous screen.
	/// </summary>
	public void LstAvatarClick(Action action)
	{
		Soldier soldier = _base.Soldiers[_soldier];

		// change the avatar
		int row = (int)_lstAvatar.SelectedRow;
		soldier.Gender = _avatars[row].Gender;
		soldier.Look = _avatars[row].Look;
		soldier.LookVariant = _avatars[row].LookVariant;

		InitPreview(soldier);
	}
}
