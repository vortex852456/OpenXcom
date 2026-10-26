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
// Ported from: src/Menu/ModConfirmExtendedState.h, src/Menu/ModConfirmExtendedState.cpp
#nullable disable

namespace Acom;

/// <summary>Confirmation window when enabling mods that require OXCE.</summary>
public class ModConfirmExtendedState : State
{
	private readonly ModListState _state;
	private readonly bool _isMaster;
	private readonly TextButton _btnYes, _btnNo;
	private readonly Window _window;
	private readonly Text _txtTitle;

	/// <summary>Initializes all the elements in the Confirm OXCE screen.</summary>
	/// <param name="state">Pointer to the Options|Mod state.</param>
	/// <param name="modInfo">What exactly mod caused this question?</param>
	/// <param name="masterInfo">The active master mod (null when the master itself is checked).</param>
	public ModConfirmExtendedState(ModListState state, ModInfo modInfo, ModInfo masterInfo = null)
	{
		_state = state;
		_isMaster = modInfo.IsMaster;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 100, 32, 50, WindowPopup.POPUP_BOTH);
		_btnYes = new TextButton(60, 18, 60, 122);
		_btnNo = new TextButton(60, 18, 200, 122);
		_txtTitle = new Text(246, 50, 37, 64);

		// Set palette
		SetInterface("optionsMenu");

		Add(_window, "confirmDefaults", "optionsMenu");
		Add(_btnYes, "confirmDefaults", "optionsMenu");
		Add(_btnNo, "confirmDefaults", "optionsMenu");
		Add(_txtTitle, "confirmDefaults", "optionsMenu");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "optionsMenu");

		_btnYes.Text = Tr("STR_YES");
		_btnYes.OnMouseClick(BtnYesClick);
		if (!modInfo.IsEngineOk)
		{
			_btnYes.Visible = false;
		}

		_btnNo.Text = Tr("STR_CANCEL");
		_btnNo.OnMouseClick(BtnNoClick);

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		if (masterInfo != null && !modInfo.IsParentMasterOk(masterInfo))
		{
			_txtTitle.SetText(Tr("STR_MASTER_MOD_VERSION_REQUIRED_QUESTION").Arg(modInfo.RequiredMasterVersion).Arg(masterInfo.Version));
		}
		else if (modInfo.RequiredExtendedEngine != Version.OPENXCOM_VERSION_ENGINE)
		{
			_txtTitle.SetText(Tr("STR_OXCE_REQUIRED_QUESTION").Arg(modInfo.RequiredExtendedEngine));
		}
		else
		{
			_txtTitle.SetText(Tr("STR_VERSION_REQUIRED_QUESTION").Arg(modInfo.RequiredExtendedVersion));
		}
	}

	/// <summary>Closes the window. Enables the mod.</summary>
	/// <param name="action">Pointer to an action.</param>
	public void BtnYesClick(Action action)
	{
		_game.PopState();

		if (_isMaster)
		{
			_state.ChangeMasterMod();
		}
		else
		{
			_state.ToggleMod();
		}
	}

	/// <summary>Closes the window. Does not enable the mod.</summary>
	/// <param name="action">Pointer to an action.</param>
	public void BtnNoClick(Action action)
	{
		_game.PopState();

		if (_isMaster)
		{
			_state.RevertMasterMod();
		}
	}

	/// <summary>Check if master mod is not valid.</summary>
	public static bool IsMasterNotValid(ModInfo masterInfo)
	{
		return !masterInfo.IsEngineOk;
	}

	/// <summary>Check if mod is not valid.</summary>
	public static bool IsModNotValid(ModInfo modInfo, ModInfo masterInfo)
	{
		return !modInfo.IsMaster && // skip checking master mod
			(!modInfo.IsEngineOk || !modInfo.IsParentMasterOk(masterInfo));
	}

	/// <summary>Show confirmation when the master mod requires a different engine.</summary>
	public static bool TryShowMasterNotValidConfirmationState(ModListState state, ModInfo masterInfo)
	{
		if (IsMasterNotValid(masterInfo))
		{
			_game.PushState(new ModConfirmExtendedState(state, masterInfo));
			return true;
		}

		return false;
	}

	/// <summary>Show confirmation when the mod requires a different engine or master version.</summary>
	public static bool TryShowModNotValidConfirmationState(ModListState state, ModInfo modInfo, ModInfo masterInfo)
	{
		if (IsModNotValid(modInfo, masterInfo))
		{
			_game.PushState(new ModConfirmExtendedState(state, modInfo, masterInfo));
			return true;
		}

		return false;
	}
}
