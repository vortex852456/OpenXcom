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
// Ported from: src/Battlescape/ActionMenuItem.h, src/Battlescape/ActionMenuItem.cpp
#nullable disable

namespace Acom;

/// <summary>
/// A class that represents a single box in the action popup menu on the battlescape.
/// It shows the possible actions of an item, their TU cost and accuracy.
/// Mouse over highlights the action, when clicked the action is sent to the parent state.
/// </summary>
public class ActionMenuItem : InteractiveSurface
{
	private bool _highlighted;
	private BattleActionType _action;
	private RuleSkill _skill;
	private int _tu;
	private readonly int _highlightModifier;
	private readonly Frame _frame;
	private readonly Text _txtDescription, _txtAcc, _txtTU;

	/// <summary>
	/// Sets up an Action menu item.
	/// </summary>
	/// <param name="id">The unique identifier of the menu item.</param>
	/// <param name="game">Pointer to the game.</param>
	/// <param name="x">Position on the x-axis.</param>
	/// <param name="y">Position on the y-axis.</param>
	public ActionMenuItem(int id, Game game, int x, int y) : base(272, 40, x + 24, y - (id * 40))
	{
		ArgumentNullException.ThrowIfNull(game);
		_highlighted = false;
		_action = BattleActionType.BA_NONE;
		_skill = null;
		_tu = 0;

		Font big = game.Mod.GetFont("FONT_BIG"), small = game.Mod.GetFont("FONT_SMALL");
		Language lang = game.Language;

		Element actionMenu = game.Mod.GetInterface("battlescape").GetElement("actionMenu");

		_highlightModifier = actionMenu.TFTDMode ? 12 : 3;

		_frame = new Frame(Width, Height, 0, 0);
		_frame.SetHighContrast(true);
		_frame.SetColor((byte)actionMenu.border);
		_frame.SetSecondaryColor((byte)actionMenu.color2);
		_frame.SetThickness(8);

		_txtDescription = new Text(200, 20, 10, 13);
		_txtDescription.InitText(big, small, lang);
		_txtDescription.SetBig();
		_txtDescription.SetHighContrast(true);
		_txtDescription.SetColor((byte)actionMenu.color);
		_txtDescription.Visible = true;

		_txtAcc = new Text(100, 20, 140, 13);
		_txtAcc.InitText(big, small, lang);
		_txtAcc.SetBig();
		_txtAcc.SetHighContrast(true);
		_txtAcc.SetColor((byte)actionMenu.color);

		_txtTU = new Text(80, 20, 210, 13);
		_txtTU.InitText(big, small, lang);
		_txtTU.SetBig();
		_txtTU.SetHighContrast(true);
		_txtTU.SetColor((byte)actionMenu.color);
	}

	/// <summary>
	/// Links with an action and fills in the text fields.
	/// </summary>
	/// <param name="action">The battlescape action.</param>
	/// <param name="description">The actions description.</param>
	/// <param name="accuracy">The actions accuracy, including the Acc&gt; prefix.</param>
	/// <param name="timeunits">The timeunits string, including the TUs&gt; prefix.</param>
	/// <param name="tu">The timeunits value.</param>
	public void SetAction(BattleActionType action, string description, string accuracy, string timeunits, int tu)
	{
		_action = action;
		_txtDescription.SetText(description);
		_txtAcc.SetText(accuracy);
		_txtTU.SetText(timeunits);
		_tu = tu;
		_redraw = true;
	}

	/// <summary>The skill that was linked to this menu item.</summary>
	public RuleSkill Skill { get => _skill; set => _skill = value; }

	/// <summary>Gets the action that was linked to this menu item.</summary>
	public BattleActionType Action => _action;

	/// <summary>Gets the action tus that were linked to this menu item.</summary>
	public int TUs => _tu;

	/// <summary>Whether the item is currently highlighted (mouse over).</summary>
	internal bool IsHighlighted => _highlighted;

	/// <summary>Replaces a certain amount of colors in the surface's palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_frame.SetPalette(colors, firstcolor, ncolors);
		_txtDescription.SetPalette(colors, firstcolor, ncolors);
		_txtAcc.SetPalette(colors, firstcolor, ncolors);
		_txtTU.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Draws the bordered box.</summary>
	public override void Draw()
	{
		// as in C++: no Surface::draw(), so _redraw stays set and the box is redrawn on every blit
		_frame.Blit(this);
		_txtDescription.Blit(this);
		_txtAcc.Blit(this);
		_txtTU.Blit(this);
	}

	/// <summary>Processes a mouse hover in event.</summary>
	public override void MouseIn(Action action, State state)
	{
		_highlighted = true;
		_frame.SetSecondaryColor((byte)(_frame.SecondaryColor - _highlightModifier));
		Draw();
		base.MouseIn(action, state);
	}

	/// <summary>Processes a mouse hover out event.</summary>
	public override void MouseOut(Action action, State state)
	{
		_highlighted = false;
		_frame.SetSecondaryColor((byte)(_frame.SecondaryColor + _highlightModifier));
		Draw();
		base.MouseOut(action, state);
	}
}
