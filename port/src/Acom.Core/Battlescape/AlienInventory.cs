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
// Ported from: src/Battlescape/AlienInventory.h, src/Battlescape/AlienInventory.cpp
// Port status: partial (missing: script hooks)
#nullable disable

using static Acom.InventoryType;

namespace Acom;

/// <summary>
/// View of an alien inventory.
/// Lets the player view alien's equipment (in hand only).
/// </summary>
public class AlienInventory : InteractiveSurface
{
	private readonly Game _game;
	private readonly Surface _grid, _items;
	private BattleUnit _selUnit;
	private int _dynamicOffset;
	private int _animFrame;
	private readonly Timer _animTimer;

	/// <summary>Sets up an inventory with the specified size and position.</summary>
	public AlienInventory(Game game, int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		ArgumentNullException.ThrowIfNull(game);
		_game = game;
		_grid = new Surface(width, height, 0, 0);
		_items = new Surface(width, height, 0, 0);

		_animTimer = new Timer(100);
		_animTimer.OnTimer((SurfaceHandler)Animate);
		_animTimer.Start();
	}

	/// <summary>Replaces a certain amount of colors in the inventory's palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_grid.SetPalette(colors, firstcolor, ncolors);
		_items.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>The currently selected (i.e. displayed) unit.</summary>
	public BattleUnit SelectedUnit
	{
		get => _selUnit;
		set
		{
			_selUnit = value;
			_dynamicOffset = 0;
			if (value != null && value.IsBigUnit)
			{
				_dynamicOffset = _game.Mod.AlienInventoryOffsetBigUnit;
			}
		}
	}

	/// <summary>Draws the inventory elements.</summary>
	public override void Draw()
	{
		DrawGrid();
		DrawItems();
	}

	/// <summary>Draws the inventory grid.</summary>
	public void DrawGrid()
	{
		_grid.Clear();
		RuleInterface rule = _game.Mod.GetInterface("inventory");
		byte color = (byte)rule.GetElement("grid").color;

		foreach (var pair in _game.Mod.Inventories)
		{
			if (pair.Value.Type == INV_HAND)
			{
				SDL_Rect r;
				int rx = pair.Value.X;
				rx += _game.Mod.AlienInventoryOffsetX;

				if (pair.Value.IsRightHand)
					rx -= _dynamicOffset;
				else if (pair.Value.IsLeftHand)
					rx += _dynamicOffset;

				r.x = (short)rx;
				r.y = (short)pair.Value.Y;
				r.w = RuleInventory.HAND_W * RuleInventory.SLOT_W;
				r.h = RuleInventory.HAND_H * RuleInventory.SLOT_H;
				_grid.DrawRect(ref r, color);
				r.x++;
				r.y++;
				r.w -= 2;
				r.h -= 2;
				_grid.DrawRect(ref r, 0);
			}
			else
			{
				continue;
			}
		}
	}

	/// <summary>Draws the items contained in the alien's hands.</summary>
	public void DrawItems()
	{
		SavedBattleGame save = _game.SavedGame.SavedBattle;
		_items.Clear();
		if (_selUnit != null)
		{
			SurfaceSet texture = _game.Mod.GetSurfaceSet("BIGOBS.PCK");
			foreach (BattleItem item in _selUnit.Inventory)
			{
				if (item.Slot.Type == INV_HAND)
				{
					Surface frame = item.GetBigSprite(texture, save, _animFrame);

					if (frame == null)
						continue;

					int x = item.Slot.X + item.Rules.HandSpriteOffX;
					x += _game.Mod.AlienInventoryOffsetX;

					if (item.Slot.IsRightHand)
						x -= _dynamicOffset;
					else if (item.Slot.IsLeftHand)
						x += _dynamicOffset;

					int y = item.Slot.Y + item.Rules.HandSpriteOffY;

					// C++ blits through ScriptWorkerBlit (BattleItem::ScriptFill, BODYPART_ITEM_INVENTORY); without recolor
					// scripts that is a plain blitNShade (script hooks)
					frame.BlitNShade(_items, x, y, 0);
				}
				else
				{
					continue;
				}
			}
		}
	}

	/// <summary>Gets the inventory slot located in the specified mouse position.</summary>
	private RuleInventory GetSlotInPosition(ref int x, ref int y)
	{
		foreach (var pair in _game.Mod.Inventories)
		{
			if (pair.Value.CheckSlotInPosition(ref x, ref y))
			{
				return pair.Value;
			}
		}
		return null;
	}

	/// <summary>Blits the inventory elements.</summary>
	public override void Blit(Surface surface)
	{
		Clear();
		_grid.BlitNShade(this, 0, 0);
		_items.BlitNShade(this, 0, 0);
		base.Blit(surface);
	}

	/// <summary>Item info.</summary>
	public override void MouseClick(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_game.IsLeftClick(action))
		{
			_game.PopState();
		}
		else if (_game.IsRightClick(action))
		{
			_game.PopState();
		}
		else if (_game.IsMiddleClick(action))
		{
			if (_selUnit == null)
				return;

			int x = (int)Math.Floor(action.AbsoluteXMouse) - X,
				y = (int)Math.Floor(action.AbsoluteYMouse) - Y;

			if (x >= Screen.ORIGINAL_WIDTH / 2)
				x -= _dynamicOffset;
			else
				x += _dynamicOffset;

			x -= _game.Mod.AlienInventoryOffsetX;

			RuleInventory slot = GetSlotInPosition(ref x, ref y);
			if (slot != null)
			{
				if (slot.Type == INV_HAND)
				{
					BattleItem item = _selUnit.GetItem(slot, x, y);
					if (item != null)
					{
						string articleId = item.Rules.UfopediaType;
						Ufopaedia.OpenArticle(_game, articleId);
					}
				}
			}
		}
		base.MouseClick(action, state);
	}

	/// <summary>Handles timers.</summary>
	public override void Think()
	{
		_animTimer.Think(null, this);
	}

	/// <summary>Animate surface.</summary>
	public void Animate()
	{
		_animFrame++;

		DrawItems();
	}
}
