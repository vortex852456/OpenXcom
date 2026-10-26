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
// Ported from: src/Menu/ModListState.h, src/Menu/ModListState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Mods window to manage the installed mods.</summary>
public class ModListState : State
{
	private readonly Window _window;
	private readonly Text _txtMaster;
	private readonly ComboBox _cbxMasters;
	private readonly TextList _lstMods;
	private readonly TextButton _btnOk, _btnCancel, _btnOpenFolder;
	private readonly Text _txtTooltip;
	private string _currentTooltip = "";
	private readonly List<ModInfo> _masters = [];
	private string _curMasterId = "";
	/// <summary>Copies of the shown entries of <see cref="Options.mods"/> (C++ holds the pairs by value).</summary>
	private readonly List<ModToggle> _mods = [];
	private int _curMasterIdx;

	/// <summary>Initializes all the elements in the Mod Options window.</summary>
	public ModListState()
	{
		_curMasterIdx = 0;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);

		_txtMaster = new Text(305, 9, 8, 8);
		_cbxMasters = new ComboBox(this, 305, 16, 8, 18);
		_lstMods = new TextList(288, 104, 8, 40);

		_btnOk = new TextButton(100, 16, 8, 176);
		_btnOpenFolder = new TextButton(100, 16, 110, 176);
		_btnCancel = new TextButton(100, 16, 212, 176);

		_txtTooltip = new Text(305, 25, 8, 148);

		// Set palette
		SetInterface("modsMenu");

		Add(_window, "window", "modsMenu");

		Add(_txtMaster, "text", "modsMenu");
		Add(_lstMods, "optionLists", "modsMenu");
		Add(_btnOk, "button2", "modsMenu");
		Add(_btnOpenFolder, "button2", "modsMenu");
		Add(_btnCancel, "button2", "modsMenu");
		Add(_txtTooltip, "tooltip", "modsMenu");

		Add(_cbxMasters, "button1", "modsMenu");

		CenterAllSurfaces();

		// how much room do we need for YES/NO
		var text = new Text(100, 9, 0, 0);
		text.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
		text.SetText(Tr("STR_YES"));
		int yes = text.GetTextWidth();
		text.SetText(Tr("STR_NO"));
		int no = text.GetTextWidth();

		int rightcol = Math.Max(yes, no) + 2;
		int arrowCol = 25;
		int leftcol = _lstMods.Width - (rightcol + arrowCol);

		// Set up objects
		SetWindowBackground(_window, "modsMenu");

		_txtMaster.SetText(Tr("STR_BASE_GAME"));

		// scan for masters
		//Options::refreshMods(); // TODO: uncomment (and properly test!) after mod.io integration is merged
		var modInfos = Options.GetModInfos();
		var masterNames = new List<string>();
		foreach (var pair in Options.mods)
		{
			string modId = pair.Id;
			if (!modInfos.TryGetValue(modId, out ModInfo modInfo))
			{
				continue;
			}
			if (!modInfo.IsMaster)
			{
				continue;
			}

			if (pair.Active)
			{
				_curMasterId = modId;
			}
			else if (_curMasterId.Length == 0)
			{
				++_curMasterIdx;
			}
			_masters.Add(modInfo);
			masterNames.Add(modInfo.Name);
		}

		_cbxMasters.SetOptions(masterNames);
		_cbxMasters.Selected = _curMasterIdx;
		_cbxMasters.OnChange(CbxMasterChange);
		_cbxMasters.OnMouseIn(TxtTooltipIn);
		_cbxMasters.OnMouseOut(TxtTooltipOut);
		_cbxMasters.OnMouseOver(CbxMasterHover);
		_cbxMasters.OnListMouseIn(TxtTooltipIn);
		_cbxMasters.OnListMouseOut(TxtTooltipOut);
		_cbxMasters.OnListMouseOver(CbxMasterHover);

		_lstMods.SetArrowColumn(leftcol + 1, ArrowOrientation.ARROW_VERTICAL);
		_lstMods.SetColumns(3, leftcol, arrowCol, rightcol);
		_lstMods.SetAlign(TextHAlign.ALIGN_RIGHT, 1);
		_lstMods.SetSelectable(true);
		_lstMods.SetBackground(_window);
		_lstMods.SetWordWrap(true);
		_lstMods.OnMouseClick(LstModsClick);
		_lstMods.OnLeftArrowClick(LstModsLeftArrowClick);
		_lstMods.OnRightArrowClick(LstModsRightArrowClick);
		_lstMods.OnMousePress(LstModsMousePress);
		_lstMods.OnMouseIn(TxtTooltipIn);
		_lstMods.OnMouseOut(TxtTooltipOut);
		_lstMods.OnMouseOver(LstModsHover);
		LstModsRefresh(0);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);

		_btnOpenFolder.Text = Tr("STR_OPEN_MODS_FOLDER");
		_btnOpenFolder.OnMouseClick(BtnOpenFolderClick);

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);

		_txtTooltip.SetWordWrap(true);
	}

	public string MakeTooltip(ModInfo modInfo)
	{
		return Tr("STR_MODS_TOOLTIP").Arg(modInfo.VersionDisplay).Arg(modInfo.Author).Arg(modInfo.Description);
	}

	public void CbxMasterHover(Action action)
	{
		_txtTooltip.SetText(MakeTooltip(_masters[_cbxMasters.HoveredListIdx]));
	}

	public void CbxMasterChange(Action action)
	{
		ModInfo masterModInfo = _masters[_cbxMasters.Selected];

		// when changing a master mod, check if it requires OXCE
		if (ModConfirmExtendedState.TryShowMasterNotValidConfirmationState(this, masterModInfo))
		{
			return;
		}

		ChangeMasterMod();
	}

	public void ChangeMasterMod()
	{
		string masterId = _masters[_cbxMasters.Selected].Id;
		foreach (var pair in Options.mods)
		{
			if (masterId == pair.Id)
			{
				pair.Active = true;
			}
			else if (_curMasterId == pair.Id)
			{
				pair.Active = false;
			}
		}
		Options.reload = true;

		_curMasterIdx = _cbxMasters.Selected;
		_curMasterId = masterId;
		LstModsRefresh(0);
	}

	public void RevertMasterMod()
	{
		_cbxMasters.Selected = _curMasterIdx;
	}

	public void LstModsRefresh(int scrollLoc)
	{
		_lstMods.ClearList();
		_mods.Clear();

		// only show mods that work with the current master
		foreach (var pair in Options.mods)
		{
			if (!Options.GetModInfos().TryGetValue(pair.Id, out ModInfo modInfo))
			{
				continue;
			}
			if (modInfo.IsMaster || !modInfo.CanActivate(_curMasterId))
			{
				continue;
			}

			string modName = modInfo.Name;
			_lstMods.AddRow(3, modName, "", pair.Active ? Tr("STR_YES") : Tr("STR_NO"));
			_mods.Add(new ModToggle(pair.Id, pair.Active));
		}

		_lstMods.ScrollTo(scrollLoc);
	}

	public void LstModsHover(Action action)
	{
		uint selectedRow = _lstMods.SelectedRow;
		if (uint.MaxValue != selectedRow)
		{
			_txtTooltip.SetText(MakeTooltip(Options.GetModInfos()[_mods[(int)selectedRow].Id]));
		}
	}

	/// <summary>Handler for clicking an item on the menu.</summary>
	public void LstModsClick(Action action)
	{
		if (action.AbsoluteXMouse >= _lstMods.ArrowsLeftEdge &&
			action.AbsoluteXMouse <= _lstMods.ArrowsRightEdge)
		{
			// don't count an arrow click as a mod enable toggle
			return;
		}

		ModToggle mod = _mods[(int)_lstMods.SelectedRow];

		// when activating a mod, check if it requires OXCE
		if (!mod.Active)
		{
			ModInfo modInfo = Options.GetModInfos()[mod.Id];
			ModInfo masterInfo = _masters[_cbxMasters.Selected];

			if (ModConfirmExtendedState.TryShowModNotValidConfirmationState(this, modInfo, masterInfo))
			{
				return;
			}
		}

		// if deactivating, or if not OXCE mod
		ToggleMod();
	}

	public void ToggleMod()
	{
		ModToggle mod = _mods[(int)_lstMods.SelectedRow];

		foreach (var pair in Options.mods)
		{
			if (mod.Id != pair.Id)
			{
				continue;
			}

			mod.Active = !mod.Active;
			pair.Active = mod.Active;
			_lstMods.SetCellText((int)_lstMods.SelectedRow, 2, mod.Active ? Tr("STR_YES") : Tr("STR_NO"));

			break;
		}
		Options.reload = true;
	}

	/// <summary>Handler for clicking the left reordering button.</summary>
	public void LstModsLeftArrowClick(Action action)
	{
		uint row = _lstMods.SelectedRow;
		if (row <= 0)
		{
			return;
		}

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			MoveModUp(action, row);
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			MoveModUp(action, row, true);
		}
	}

	/// <summary>Moves a copy of <paramref name="srcMod"/> above <paramref name="destMod"/> in <see cref="Options.mods"/>, then removes the old entry.</summary>
	internal static void MoveAbove(ModToggle srcMod, ModToggle destMod)
	{
		// insert copy of srcMod above destMod
		for (int i = 0; i < Options.mods.Count; ++i)
		{
			if (destMod.Id == Options.mods[i].Id)
			{
				Options.mods.Insert(i, new ModToggle(srcMod.Id, srcMod.Active));
				break;
			}
		}

		// remove old copy of srcMod in separate loop since the insert above invalidated the iterator
		for (int i = Options.mods.Count - 1; i >= 0; --i)
		{
			if (srcMod.Id == Options.mods[i].Id)
			{
				Options.mods.RemoveAt(i);
				break;
			}
		}
	}

	/// <summary>Moves a mod up.</summary>
	public void MoveModUp(Action action, uint row, bool max = false)
	{
		if (max)
		{
			MoveAbove(_mods[(int)row], _mods[0]);
			// don't change the scroll position
			LstModsRefresh(_lstMods.Scroll);
		}
		else
		{
			// calculate target scroll pos
			int curScrollPos = _lstMods.Scroll;
			int targetScrollPos = 0;
			for (uint i = 0; i < row - 1; ++i)
			{
				targetScrollPos += _lstMods.GetNumTextLines((int)i);
			}
			if (curScrollPos < targetScrollPos)
			{
				int ydiff = _lstMods.GetTextHeight((int)row - 1);
				WarpMouse(action.LeftBlackBand + action.XMouse,
					action.TopBlackBand + action.YMouse - (ushort)(ydiff * action.YScale));
			}
			else
			{
				int ydiff = _lstMods.GetRowY((int)row) - _lstMods.Y;
				WarpMouse(action.LeftBlackBand + action.XMouse,
					action.TopBlackBand + action.YMouse - (ushort)(ydiff * action.YScale));
				_lstMods.ScrollTo(targetScrollPos);
			}

			MoveAbove(_mods[(int)row], _mods[(int)row - 1]);
			LstModsRefresh(_lstMods.Scroll);
		}
		Options.reload = true;
	}

	/// <summary>Handler for clicking the right reordering button.</summary>
	public void LstModsRightArrowClick(Action action)
	{
		uint row = _lstMods.SelectedRow;
		int numMods = _mods.Count;
		if (0 >= numMods || row >= numMods - 1)
		{
			return;
		}

		if (action.Details.button.button == SDLMouse.SDL_BUTTON_LEFT)
		{
			MoveModDown(action, row);
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
		{
			MoveModDown(action, row, true);
		}
	}

	/// <summary>Moves a copy of <paramref name="srcMod"/> below <paramref name="destMod"/> in <see cref="Options.mods"/>, then removes the old entry.</summary>
	internal static void MoveBelow(ModToggle srcMod, ModToggle destMod)
	{
		// insert copy of srcMod below destMod
		for (int i = Options.mods.Count - 1; i >= 0; --i)
		{
			if (destMod.Id == Options.mods[i].Id)
			{
				Options.mods.Insert(i + 1, new ModToggle(srcMod.Id, srcMod.Active));
				break;
			}
		}

		// remove old copy of srcMod in separate loop since the insert above invalidated the iterator
		for (int i = 0; i < Options.mods.Count; ++i)
		{
			if (srcMod.Id == Options.mods[i].Id)
			{
				Options.mods.RemoveAt(i);
				break;
			}
		}
	}

	/// <summary>Moves a mod down.</summary>
	public void MoveModDown(Action action, uint row, bool max = false)
	{
		if (max)
		{
			MoveBelow(_mods[(int)row], _mods[^1]);
			// don't change the scroll position
			LstModsRefresh(_lstMods.Scroll);
		}
		else
		{
			// calculate target scroll pos
			int curScrollPos = _lstMods.Scroll;
			int targetScrollPos = 0;
			for (uint i = 0; i <= row + 1; ++i)
			{
				if (i == row)
				{
					// don't count the current row -- it will be moved down
					continue;
				}
				targetScrollPos += _lstMods.GetNumTextLines((int)i);
			}
			if (curScrollPos + _lstMods.VisibleRows > targetScrollPos)
			{
				int ydiff = _lstMods.GetTextHeight((int)row + 1);
				WarpMouse(action.LeftBlackBand + action.XMouse,
					action.TopBlackBand + action.YMouse + (ushort)(ydiff * action.YScale));
			}
			else
			{
				int ydiff = _lstMods.Y + _lstMods.Height - (_lstMods.GetRowY((int)row) + _lstMods.GetTextHeight((int)row));
				WarpMouse(action.LeftBlackBand + action.XMouse,
					action.TopBlackBand + action.YMouse + (ushort)(ydiff * action.YScale));
				_lstMods.ScrollTo(targetScrollPos - _lstMods.VisibleRows + 1);
			}

			MoveBelow(_mods[(int)row], _mods[(int)row + 1]);
			LstModsRefresh(_lstMods.Scroll);
		}
		Options.reload = true;
	}

	/// <summary>Handler for pressing-down a mouse-button in the list.</summary>
	public void LstModsMousePress(Action action)
	{
		if (Options.changeValueByMouseWheel == 0)
			return;
		uint row = _lstMods.SelectedRow;
		int numMods = _mods.Count;
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP &&
			row > 0)
		{
			if (action.AbsoluteXMouse >= _lstMods.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstMods.ArrowsRightEdge)
			{
				MoveModUp(action, row);
			}
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN &&
				 0 < numMods && row < numMods - 1)
		{
			if (action.AbsoluteXMouse >= _lstMods.ArrowsLeftEdge &&
				action.AbsoluteXMouse <= _lstMods.ArrowsRightEdge)
			{
				MoveModDown(action, row);
			}
		}
	}

	/// <summary>SDL_WarpMouse: moves the mouse pointer to the given window position (truncated to Uint16 like the C++ call).</summary>
	private static void WarpMouse(int x, int y) => SDLMouse.SDL_WarpMouse((ushort)x, (ushort)y);

	/// <summary>Handler for showing tooltip.</summary>
	public void TxtTooltipIn(Action action)
	{
		_currentTooltip = action.Sender.Tooltip;
		_txtTooltip.SetText(Tr(_currentTooltip));
	}

	/// <summary>Handler for hiding tooltip.</summary>
	public void TxtTooltipOut(Action action)
	{
		if (_currentTooltip == action.Sender.Tooltip)
		{
			_txtTooltip.SetText("");
		}
	}

	/// <summary>Restarts game with new mods.</summary>
	/// <param name="action">Pointer to an action.</param>
	public void BtnOkClick(Action action)
	{
		Options.Save();
		if (Options.reload)
		{
			_game.SetState(new StartState());
		}
		else
		{
			_game.PopState();
		}
	}

	/// <summary>Opens the mods folder.</summary>
	/// <param name="action">Pointer to an action.</param>
	public void BtnOpenFolderClick(Action action)
	{
		string path = Options.GetUserFolder() + "mods";
		CrossPlatform.OpenExplorer(path);
	}

	/// <summary>Ignores mod changes and returns to the previous screen.</summary>
	/// <param name="action">Pointer to an action.</param>
	public void BtnCancelClick(Action action)
	{
		Options.reload = false;
		Options.Load();
		_game.PopState();
	}
}
