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
// Ported from: src/Engine/TouchState.h, src/Engine/TouchState.cpp
#nullable disable

using System.Runtime.CompilerServices;

namespace Acom;

/// <summary>A State with touch buttons support.</summary>
public class TouchState : State
{
	protected bool _hideGroup100;
	protected Text _txtTitlePtr;
	protected TextButton _btnTouch;
	protected TextButton _btn1;
	protected TextButton _btn10;
	protected TextButton _btn100;
	/// <summary>Group variable of the 1-10-100 buttons (C++ <c>TextButton*</c> passed by address).</summary>
	protected StrongBox<TextButton> _owner100 = new();
	protected TextButton _btnLMB;
	protected TextButton _btnRMB;
	protected TextButton _btnMMB;
	/// <summary>Group variable of the LMB-RMB-MMB buttons (C++ <c>TextButton*</c> passed by address).</summary>
	protected StrongBox<TextButton> _ownerLRM = new();
	protected ToggleTextButton _btnCtrl;
	protected ToggleTextButton _btnAlt;
	protected ToggleTextButton _btnShift;

	/// <summary>Creates a new state.</summary>
	public TouchState()
	{
	}

	public void TouchComponentsCreate(Text txtTitlePtr, bool hideGroup100 = false, int horizontalOffset = 0, int verticalOffset = 0)
	{
		// Reset touch flags
		_game.ResetTouchButtonFlags();

		if (!Options.oxceBaseTouchButtons)
			return;

		_hideGroup100 = hideGroup100;
		_txtTitlePtr = txtTitlePtr;

		_btnTouch = new TextButton(40, 16, horizontalOffset + 273, 7 + verticalOffset);

		_btn1 = new TextButton(25, 16, horizontalOffset + 61 + 0 * 26, 7 + verticalOffset);
		_btn10 = new TextButton(25, 16, horizontalOffset + 61 + 1 * 26, 7 + verticalOffset);
		_btn100 = new TextButton(25, 16, horizontalOffset + 61 + 2 * 26, 7 + verticalOffset);
		_owner100.Value = _btn1;

		_btnLMB = new TextButton(25, 16, horizontalOffset + 65 + 3 * 26, 7 + verticalOffset);
		_btnRMB = new TextButton(25, 16, horizontalOffset + 65 + 4 * 26, 7 + verticalOffset);
		_btnMMB = new TextButton(25, 16, horizontalOffset + 65 + 5 * 26, 7 + verticalOffset);
		_ownerLRM.Value = _btnLMB;

		_btnCtrl = new ToggleTextButton(27, 16, horizontalOffset + 69 + 6 * 26, 7 + verticalOffset);
		_btnAlt = new ToggleTextButton(27, 16, horizontalOffset + 71 + 7 * 26, 7 + verticalOffset);
		_btnShift = new ToggleTextButton(32, 16, horizontalOffset + 73 + 8 * 26, 7 + verticalOffset);
	}

	public void TouchComponentsAdd(string id, string category, Surface parent)
	{
		if (!Options.oxceBaseTouchButtons)
			return;

		Add(_btnTouch, "touchButton", category, parent);

		Add(_btn1, id, category);
		Add(_btn10, id, category);
		Add(_btn100, id, category);

		Add(_btnLMB, id, category);
		Add(_btnRMB, id, category);
		Add(_btnMMB, id, category);

		Add(_btnCtrl, id, category);
		Add(_btnAlt, id, category);
		Add(_btnShift, id, category);
	}

	public void TouchComponentsConfigure()
	{
		if (!Options.oxceBaseTouchButtons)
			return;

		_btn1.Text = Tr("STR_BUTTON_1");
		_btn10.Text = Tr("STR_BUTTON_10");
		_btn100.Text = Tr("STR_BUTTON_100");
		_btnLMB.Text = Tr("STR_BUTTON_LMB");
		_btnRMB.Text = Tr("STR_BUTTON_RMB");
		_btnMMB.Text = Tr("STR_BUTTON_MMB");
		_btnCtrl.Text = Tr("STR_BUTTON_CTRL");
		_btnAlt.Text = Tr("STR_BUTTON_ALT");
		_btnShift.Text = Tr("STR_BUTTON_SHIFT");

		_btn1.SetGroup(_owner100);
		_btn10.SetGroup(_owner100);
		_btn100.SetGroup(_owner100);
		_btnLMB.SetGroup(_ownerLRM);
		_btnRMB.SetGroup(_ownerLRM);
		_btnMMB.SetGroup(_ownerLRM);

		_btn1.OnMousePress(BtnGroup100Press);
		_btn10.OnMousePress(BtnGroup100Press);
		_btn100.OnMousePress(BtnGroup100Press);
		_btnLMB.OnMousePress(BtnGroupLRMPress);
		_btnRMB.OnMousePress(BtnGroupLRMPress);
		_btnMMB.OnMousePress(BtnGroupLRMPress);

		_btn1.Visible = false;
		_btn10.Visible = false;
		_btn100.Visible = false;
		_btnLMB.Visible = false;
		_btnRMB.Visible = false;
		_btnMMB.Visible = false;
		_btnCtrl.Visible = false;
		_btnAlt.Visible = false;
		_btnShift.Visible = false;

		_btnTouch.Text = Tr("STR_BUTTON_TOUCH");
		_btnTouch.Visible = Options.oxceBaseTouchButtons;
		_btnTouch.OnMouseClick(BtnTouchClick);

		_btnCtrl.OnMouseClick(BtnCtrlClick);
		_btnAlt.OnMouseClick(BtnAltClick);
		_btnShift.OnMouseClick(BtnShiftClick);
	}

	public void TouchComponentsRefresh()
	{
		if (!Options.oxceBaseTouchButtons)
			return;

		_owner100.Value = _game.ScrollStep == 100 ? _btn100 : (_game.ScrollStep == 10 ? _btn10 : _btn1);

		_ownerLRM.Value = _game.MMBFlag ? _btnMMB : (_game.RMBFlag ? _btnRMB : _btnLMB);

		_btnCtrl.Pressed = _game.CtrlPressedFlag;
		_btnAlt.Pressed = _game.AltPressedFlag;
		_btnShift.Pressed = _game.ShiftPressedFlag;
	}

	/// <summary>Handler for clicking the Touch button.</summary>
	public void BtnTouchClick(Action action)
	{
		if (_txtTitlePtr != null)
		{
			_txtTitlePtr.Visible = false;
		}
		_btnTouch.Visible = false;

		if (!_hideGroup100)
		{
			_btn1.Visible = true;
			_btn10.Visible = true;
			_btn100.Visible = true;
		}
		_btnCtrl.Visible = true;
		_btnAlt.Visible = true;
		_btnShift.Visible = true;
		_btnLMB.Visible = true;
		_btnRMB.Visible = true;
		_btnMMB.Visible = true;
	}

	/// <summary>Handler for clicking 1-10-100 group buttons.</summary>
	public void BtnGroup100Press(Action action)
	{
		int step = (_owner100.Value == _btn100 ? 100 : (_owner100.Value == _btn10 ? 10 : 1));
		_game.ScrollStep = step;
	}

	/// <summary>Handler for clicking LMB-RMB-MMB group buttons.</summary>
	public void BtnGroupLRMPress(Action action)
	{
		_game.RMBFlag = _ownerLRM.Value == _btnRMB;
		_game.MMBFlag = _ownerLRM.Value == _btnMMB;
	}

	/// <summary>Handler for clicking the CTRL button.</summary>
	public void BtnCtrlClick(Action action)
	{
		_game.CtrlPressedFlag = _btnCtrl.Pressed;
	}

	/// <summary>Handler for clicking the ALT button.</summary>
	public void BtnAltClick(Action action)
	{
		_game.AltPressedFlag = _btnAlt.Pressed;
	}

	/// <summary>Handler for clicking the SHIFT button.</summary>
	public void BtnShiftClick(Action action)
	{
		_game.ShiftPressedFlag = _btnShift.Pressed;
	}
}
