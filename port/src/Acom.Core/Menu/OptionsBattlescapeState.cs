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
// Ported from: src/Menu/OptionsBattlescapeState.h, src/Menu/OptionsBattlescapeState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Screen that lets the user configure various Battlescape options.</summary>
public class OptionsBattlescapeState : OptionsBaseState
{
	private readonly Text _txtEdgeScroll, _txtDragScroll;
	private readonly ComboBox _cbxEdgeScroll, _cbxDragScroll;
	private readonly Text _txtScrollSpeed, _txtFireSpeed, _txtXcomSpeed, _txtAlienSpeed;
	private readonly Slider _slrScrollSpeed, _slrFireSpeed, _slrXcomSpeed, _slrAlienSpeed;
	private readonly Text _txtPathPreview;
	private readonly ToggleTextButton _btnArrows, _btnTuCost, _btnEnergyCost;
	private readonly Text _txtOptions;
	private readonly ToggleTextButton _btnTooltips, _btnDeaths;

	/// <summary>Initializes all the elements in the Battlescape Options screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsBattlescapeState(OptionsOrigin origin) : base(origin)
	{
		SetCategory(_btnBattlescape);

		// Create objects
		_txtEdgeScroll = new Text(114, 9, 94, 8);
		_cbxEdgeScroll = new ComboBox(this, 104, 16, 94, 18);

		_txtDragScroll = new Text(114, 9, 206, 8);
		_cbxDragScroll = new ComboBox(this, 104, 16, 206, 18);

		_txtScrollSpeed = new Text(114, 9, 94, 40);
		_slrScrollSpeed = new Slider(104, 16, 94, 50);

		_txtFireSpeed = new Text(114, 9, 206, 40);
		_slrFireSpeed = new Slider(104, 16, 206, 50);

		_txtXcomSpeed = new Text(114, 9, 94, 72);
		_slrXcomSpeed = new Slider(104, 16, 94, 82);

		_txtAlienSpeed = new Text(114, 9, 206, 72);
		_slrAlienSpeed = new Slider(104, 16, 206, 82);

		_txtPathPreview = new Text(114, 9, 94, 100);
		_btnArrows = new ToggleTextButton(104, 16, 94, 110);
		_btnTuCost = new ToggleTextButton(58, 16, 94, 128);
		_btnEnergyCost = new ToggleTextButton(44, 16, 154, 128);

		_txtOptions = new Text(114, 9, 206, 100);
		_btnTooltips = new ToggleTextButton(104, 16, 206, 110);
		_btnDeaths = new ToggleTextButton(104, 16, 206, 128);

		Add(_txtEdgeScroll, "text", "battlescapeMenu");
		Add(_txtDragScroll, "text", "battlescapeMenu");

		Add(_txtScrollSpeed, "text", "battlescapeMenu");
		Add(_slrScrollSpeed, "button", "battlescapeMenu");

		Add(_txtFireSpeed, "text", "battlescapeMenu");
		Add(_slrFireSpeed, "button", "battlescapeMenu");

		Add(_txtXcomSpeed, "text", "battlescapeMenu");
		Add(_slrXcomSpeed, "button", "battlescapeMenu");

		Add(_txtAlienSpeed, "text", "battlescapeMenu");
		Add(_slrAlienSpeed, "button", "battlescapeMenu");

		Add(_txtPathPreview, "text", "battlescapeMenu");
		Add(_btnArrows, "button", "battlescapeMenu");
		Add(_btnTuCost, "button", "battlescapeMenu");
		Add(_btnEnergyCost, "button", "battlescapeMenu");

		Add(_txtOptions, "text", "battlescapeMenu");
		Add(_btnTooltips, "button", "battlescapeMenu");
		Add(_btnDeaths, "button", "battlescapeMenu");

		Add(_cbxEdgeScroll, "button", "battlescapeMenu");
		Add(_cbxDragScroll, "button", "battlescapeMenu");

		CenterAllSurfaces();

		// Set up objects
		_txtEdgeScroll.SetText(Tr("STR_EDGE_SCROLL"));

		var edgeScrolls = new List<string>
		{
			Tr("STR_DISABLED"),
			Tr("STR_TRIGGER_SCROLL"),
			Tr("STR_AUTO_SCROLL"),
		};

		_cbxEdgeScroll.SetOptions(edgeScrolls);
		_cbxEdgeScroll.Selected = (int)Options.battleEdgeScroll;
		_cbxEdgeScroll.OnChange(CbxEdgeScrollChange);
		_cbxEdgeScroll.Tooltip = "STR_EDGE_SCROLL_DESC";
		_cbxEdgeScroll.OnMouseIn(TxtTooltipIn);
		_cbxEdgeScroll.OnMouseOut(TxtTooltipOut);

		_txtDragScroll.SetText(Tr("STR_DRAG_SCROLL"));

		var dragScrolls = new List<string>
		{
			Tr("STR_DISABLED"),
			Tr("STR_LEFT_MOUSE_BUTTON"),
			Tr("STR_MIDDLE_MOUSE_BUTTON"),
			Tr("STR_RIGHT_MOUSE_BUTTON"),
		};

		_cbxDragScroll.SetOptions(dragScrolls);
		_cbxDragScroll.Selected = Options.battleDragScrollButton;
		_cbxDragScroll.OnChange(CbxDragScrollChange);
		_cbxDragScroll.Tooltip = "STR_DRAG_SCROLL_DESC";
		_cbxDragScroll.OnMouseIn(TxtTooltipIn);
		_cbxDragScroll.OnMouseOut(TxtTooltipOut);

		_txtScrollSpeed.SetText(Tr("STR_SCROLL_SPEED"));

		_slrScrollSpeed.SetRange(2, 20);
		_slrScrollSpeed.Value = Options.battleScrollSpeed;
		_slrScrollSpeed.OnChange(SlrScrollSpeedChange);
		_slrScrollSpeed.Tooltip = "STR_SCROLL_SPEED_BATTLE_DESC";
		_slrScrollSpeed.OnMouseIn(TxtTooltipIn);
		_slrScrollSpeed.OnMouseOut(TxtTooltipOut);

		_txtFireSpeed.SetText(Tr("STR_FIRE_SPEED"));

		_slrFireSpeed.SetRange(1, 20);
		_slrFireSpeed.Value = Options.battleFireSpeed;
		_slrFireSpeed.OnChange(SlrFireSpeedChange);
		_slrFireSpeed.Tooltip = "STR_FIRE_SPEED_DESC";
		_slrFireSpeed.OnMouseIn(TxtTooltipIn);
		_slrFireSpeed.OnMouseOut(TxtTooltipOut);

		_txtXcomSpeed.SetText(Tr("STR_PLAYER_MOVEMENT_SPEED"));

		_slrXcomSpeed.SetRange(40, 1);
		_slrXcomSpeed.Value = Options.battleXcomSpeed;
		_slrXcomSpeed.OnChange(SlrXcomSpeedChange);
		_slrXcomSpeed.Tooltip = "STR_PLAYER_MOVEMENT_SPEED_DESC";
		_slrXcomSpeed.OnMouseIn(TxtTooltipIn);
		_slrXcomSpeed.OnMouseOut(TxtTooltipOut);

		_txtAlienSpeed.SetText(Tr("STR_COMPUTER_MOVEMENT_SPEED"));

		_slrAlienSpeed.SetRange(40, 1);
		_slrAlienSpeed.Value = Options.battleAlienSpeed;
		_slrAlienSpeed.OnChange(SlrAlienSpeedChange);
		_slrAlienSpeed.Tooltip = "STR_COMPUTER_MOVEMENT_SPEED_DESC";
		_slrAlienSpeed.OnMouseIn(TxtTooltipIn);
		_slrAlienSpeed.OnMouseOut(TxtTooltipOut);

		_txtPathPreview.SetText(Tr("STR_PATH_PREVIEW"));

		_btnArrows.Text = Tr("STR_PATH_ARROWS");
		_btnArrows.Pressed = (Options.battleNewPreviewPath & PathPreview.PATH_ARROWS) != 0;
		_btnArrows.OnMouseClick(BtnPathPreviewClick);
		_btnArrows.Tooltip = "STR_PATH_ARROWS_DESC";
		_btnArrows.OnMouseIn(TxtTooltipIn);
		_btnArrows.OnMouseOut(TxtTooltipOut);

		_btnTuCost.Text = Tr("STR_PATH_TIME_UNIT_COST");
		_btnTuCost.Pressed = (Options.battleNewPreviewPath & PathPreview.PATH_TU_COST) != 0;
		_btnTuCost.OnMouseClick(BtnPathPreviewClick);
		_btnTuCost.Tooltip = "STR_PATH_TIME_UNIT_COST_DESC";
		_btnTuCost.OnMouseIn(TxtTooltipIn);
		_btnTuCost.OnMouseOut(TxtTooltipOut);

		_btnEnergyCost.Text = Tr("STR_PATH_ENERGY_COST");
		_btnEnergyCost.Pressed = (Options.battleNewPreviewPath & PathPreview.PATH_ENERGY_COST) != 0;
		_btnEnergyCost.OnMouseClick(BtnPathPreviewClick);
		_btnEnergyCost.Tooltip = "STR_PATH_ENERGY_COST_DESC";
		_btnEnergyCost.OnMouseIn(TxtTooltipIn);
		_btnEnergyCost.OnMouseOut(TxtTooltipOut);

		_txtOptions.SetText(Tr("STR_USER_INTERFACE_OPTIONS"));

		_btnTooltips.Text = Tr("STR_TOOLTIPS");
		_btnTooltips.Pressed = Options.battleTooltips;
		_btnTooltips.OnMouseClick(BtnTooltipsClick);
		_btnTooltips.Tooltip = "STR_TOOLTIPS_DESC";
		_btnTooltips.OnMouseIn(TxtTooltipIn);
		_btnTooltips.OnMouseOut(TxtTooltipOut);

		_btnDeaths.Text = Tr("STR_DEATH_NOTIFICATIONS");
		_btnDeaths.Pressed = Options.battleNotifyDeath;
		_btnDeaths.OnMouseClick(BtnDeathsClick);
		_btnDeaths.Tooltip = "STR_DEATH_NOTIFICATIONS_DESC";
		_btnDeaths.OnMouseIn(TxtTooltipIn);
		_btnDeaths.OnMouseOut(TxtTooltipOut);
	}

	/// <summary>Changes the Edge Scroll option.</summary>
	public void CbxEdgeScrollChange(Action action)
	{
		Options.battleEdgeScroll = (ScrollType)_cbxEdgeScroll.Selected;
	}

	/// <summary>Changes the Drag Scroll option.</summary>
	public void CbxDragScrollChange(Action action)
	{
		Options.battleDragScrollButton = _cbxDragScroll.Selected;
	}

	/// <summary>Updates the scroll speed.</summary>
	public void SlrScrollSpeedChange(Action action)
	{
		Options.battleScrollSpeed = _slrScrollSpeed.Value;
	}

	/// <summary>Updates the fire speed.</summary>
	public void SlrFireSpeedChange(Action action)
	{
		Options.battleFireSpeed = _slrFireSpeed.Value;
	}

	/// <summary>Updates the X-COM movement speed.</summary>
	public void SlrXcomSpeedChange(Action action)
	{
		Options.battleXcomSpeed = _slrXcomSpeed.Value;
	}

	/// <summary>Updates the alien movement speed.</summary>
	public void SlrAlienSpeedChange(Action action)
	{
		Options.battleAlienSpeed = _slrAlienSpeed.Value;
	}

	/// <summary>Updates the path preview options.</summary>
	public void BtnPathPreviewClick(Action action)
	{
		var mode = PathPreview.PATH_NONE;
		if (_btnArrows.Pressed)
		{
			mode |= PathPreview.PATH_ARROWS;
		}
		if (_btnTuCost.Pressed)
		{
			mode |= PathPreview.PATH_TU_COST;
		}
		if (_btnEnergyCost.Pressed)
		{
			mode |= PathPreview.PATH_ENERGY_COST;
		}
		Options.battleNewPreviewPath = mode;
	}

	/// <summary>Updates the Tooltips option.</summary>
	public void BtnTooltipsClick(Action action)
	{
		Options.battleTooltips = _btnTooltips.Pressed;
	}

	/// <summary>Updates the Death Notifications option.</summary>
	public void BtnDeathsClick(Action action)
	{
		Options.battleNotifyDeath = _btnDeaths.Pressed;
	}
}
