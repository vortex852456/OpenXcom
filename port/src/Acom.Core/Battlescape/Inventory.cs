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
// Ported from: src/Battlescape/Inventory.h, src/Battlescape/Inventory.cpp
// Port status: partial (missing: script hooks)
#nullable disable

using System.Text;
using static Acom.BattleActionType;
using static Acom.BattleFuseType;
using static Acom.BattleType;
using static Acom.InventoryType;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>
/// Interactive view of an inventory.
/// Lets the player view and manage a soldier's equipment.
/// </summary>
public class Inventory : InteractiveSurface
{
	private readonly Game _game;
	private readonly Surface _grid, _items, _gridLabels, _selection;
	private readonly byte _twoHandedRed, _twoHandedGreen;
	private readonly WarningMessage _warning;
	private BattleUnit _selUnit;
	private BattleItem _selItem;
	private bool _tu;
	private readonly bool _base;
	private BattleItem _mouseOverItem;
	private int _groundOffset, _animFrame;
	/// <summary>C++ std::map&lt;int, std::map&lt;int, int&gt;&gt;; only used for lookups, so iteration order does not matter.</summary>
	private readonly Dictionary<int, Dictionary<int, int>> _stackLevel = [];
	private readonly List<List<bool>> _occupiedSlotsCache = [];
	private readonly Surface _stunIndicator, _woundIndicator, _burnIndicator, _shockIndicator;
	private readonly NumberText _stackNumber;
	private string _searchString = "";
	private readonly Timer _animTimer;
	private readonly int _depth, _groundSlotsX, _groundSlotsY;
	private int _xMax;
	private readonly RuleInventory _inventorySlotRightHand;
	private readonly RuleInventory _inventorySlotLeftHand;
	private readonly RuleInventory _inventorySlotBackPack;
	private readonly RuleInventory _inventorySlotBelt;
	private readonly RuleInventory _inventorySlotGround;

	/// <summary>
	/// Sets up an inventory with the specified size and position.
	/// </summary>
	/// <param name="game">Pointer to core game.</param>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="x">X position in pixels.</param>
	/// <param name="y">Y position in pixels.</param>
	/// <param name="base">Is the inventory being called from the basescape?</param>
	public Inventory(Game game, int width, int height, int x = 0, int y = 0, bool @base = false) : base(width, height, x, y)
	{
		ArgumentNullException.ThrowIfNull(game);
		_game = game;
		_tu = true;
		_base = @base;

		_twoHandedRed = (byte)_game.Mod.GetInterface("battlescape").GetElement("twoHandedRed").color;
		_twoHandedGreen = (byte)_game.Mod.GetInterface("battlescape").GetElement("twoHandedGreen").color;

		_depth = _game.SavedGame.SavedBattle.Depth;
		_grid = new Surface(width, height, 0, 0);
		_items = new Surface(width, height, 0, 0);
		_gridLabels = new Surface(width, height, 0, 0);
		_selection = new Surface(RuleInventory.HAND_W * RuleInventory.SLOT_W, RuleInventory.HAND_H * RuleInventory.SLOT_H, x, y);
		_warning = new WarningMessage(224, 24, 48, 176);
		_stackNumber = new NumberText(15, 15, 0, 0);
		_stackNumber.SetBordered(true);

		_warning.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
		_warning.SetColor((byte)_game.Mod.GetInterface("battlescape").GetElement("warning").color2);
		_warning.SetTextColor((byte)_game.Mod.GetInterface("battlescape").GetElement("warning").color);

		_animTimer = new Timer(100);
		_animTimer.OnTimer((SurfaceHandler)Animate);
		_animTimer.Start();

		_stunIndicator = _game.Mod.GetSurface("BigStunIndicator", false);
		_woundIndicator = _game.Mod.GetSurface("BigWoundIndicator", false);
		_burnIndicator = _game.Mod.GetSurface("BigBurnIndicator", false);
		_shockIndicator = _game.Mod.GetSurface("BigShockIndicator", false);

		SavedBattleGame battleSave = _game.SavedGame.SavedBattle;
		if (battleSave != null)
		{
			RuleEnviroEffects enviro = battleSave.EnviroEffects;
			if (enviro != null)
			{
				if (!string.IsNullOrEmpty(enviro.InventoryShockIndicator))
				{
					_shockIndicator = _game.Mod.GetSurface(enviro.InventoryShockIndicator, false);
				}
			}
		}

		_inventorySlotRightHand = _game.Mod.InventoryRightHand;
		_inventorySlotLeftHand = _game.Mod.InventoryLeftHand;
		_inventorySlotBackPack = _game.Mod.InventoryBackpack;
		_inventorySlotBelt = _game.Mod.InventoryBelt;
		_inventorySlotGround = _game.Mod.InventoryGround;

		_groundSlotsX = (Screen.ORIGINAL_WIDTH - _inventorySlotGround.X) / RuleInventory.SLOT_W;
		_groundSlotsY = (Screen.ORIGINAL_HEIGHT - _inventorySlotGround.Y) / RuleInventory.SLOT_H;
		_xMax = 0;
		for (int i = 0; i < _groundSlotsY; ++i)
		{
			_occupiedSlotsCache.Add(new List<bool>(new bool[_groundSlotsX * 2]));
		}
	}

	/// <summary>Replaces a certain amount of colors in the inventory's palette.</summary>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);
		_grid.SetPalette(colors, firstcolor, ncolors);
		_items.SetPalette(colors, firstcolor, ncolors);
		_gridLabels.SetPalette(colors, firstcolor, ncolors);
		_selection.SetPalette(colors, firstcolor, ncolors);
		_warning.SetPalette(colors, firstcolor, ncolors);
		_stackNumber.SetPalette(Palette);
	}

	/// <summary>
	/// Changes the inventory's Time Units mode.
	/// When True, inventory actions cost soldier time units (for battle).
	/// When False, inventory actions don't cost anything (for pre-equip).
	/// </summary>
	public void SetTuMode(bool tu)
	{
		_tu = tu;
	}

	/// <summary>Returns the currently selected (i.e. displayed) unit.</summary>
	public BattleUnit SelectedUnit { get => _selUnit; }

	/// <summary>Changes the unit to display the inventory of.</summary>
	public void SetSelectedUnit(BattleUnit unit, bool resetGroundOffset)
	{
		_selUnit = unit;
		if (resetGroundOffset)
		{
			_groundOffset = 9999;
			ArrangeGround(1);
		}
	}

	/// <summary>Draws the inventory elements.</summary>
	public override void Draw()
	{
		DrawGrid();
		DrawItems();
	}

	/// <summary>Draws the inventory grid for item placement.</summary>
	public void DrawGrid()
	{
		_grid.Clear();
		RuleInterface rule = _game.Mod.GetInterface("inventory");
		byte color = (byte)rule.GetElement("grid").color;

		foreach (var invPair in _game.Mod.Inventories)
		{
			RuleInventory ruleInv = invPair.Value;

			// Draw grid
			if (ruleInv.Type == INV_SLOT)
			{
				foreach (var slotInfo in ruleInv.Slots)
				{
					SDL_Rect r;
					r.x = (short)(ruleInv.X + RuleInventory.SLOT_W * slotInfo.x);
					r.y = (short)(ruleInv.Y + RuleInventory.SLOT_H * slotInfo.y);
					r.w = RuleInventory.SLOT_W + 1;
					r.h = RuleInventory.SLOT_H + 1;
					_grid.DrawRect(ref r, color);
					r.x++;
					r.y++;
					r.w -= 2;
					r.h -= 2;
					_grid.DrawRect(ref r, 0);
				}
			}
			else if (ruleInv.Type == INV_HAND)
			{
				SDL_Rect r;
				r.x = (short)ruleInv.X;
				r.y = (short)ruleInv.Y;
				r.w = RuleInventory.HAND_W * RuleInventory.SLOT_W;
				r.h = RuleInventory.HAND_H * RuleInventory.SLOT_H;
				_grid.DrawRect(ref r, color);
				r.x++;
				r.y++;
				r.w -= 2;
				r.h -= 2;
				_grid.DrawRect(ref r, 0);
			}
			else if (ruleInv.Type == INV_GROUND)
			{
				for (int x = ruleInv.X; x <= 320; x += RuleInventory.SLOT_W)
				{
					for (int y = ruleInv.Y; y <= 200; y += RuleInventory.SLOT_H)
					{
						SDL_Rect r;
						r.x = (short)x;
						r.y = (short)y;
						r.w = RuleInventory.SLOT_W + 1;
						r.h = RuleInventory.SLOT_H + 1;
						_grid.DrawRect(ref r, color);
						r.x++;
						r.y++;
						r.w -= 2;
						r.h -= 2;
						_grid.DrawRect(ref r, 0);
					}
				}
			}
		}
		DrawGridLabels();
	}

	/// <summary>Draws the inventory grid labels.</summary>
	public void DrawGridLabels(bool showTuCost = false)
	{
		_gridLabels.Clear();

		Text text = new Text(90, 9, 0, 0);
		text.SetPalette(_gridLabels.Palette);
		text.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);

		RuleInterface rule = _game.Mod.GetInterface("inventory");

		text.SetColor((byte)rule.GetElement("textSlots").color);
		text.SetHighContrast(true);

		// Note: iterating over the (sorted) invs vector instead of invs map, because we want to consider listOrder here
		foreach (string invName in _game.Mod.InvsList)
		{
			RuleInventory i = _game.Mod.GetInventory(invName, true);
			// Draw label
			text.X = i.X;
			text.Y = i.Y - text.Font.Height - text.Font.Spacing;
			if (showTuCost && _selItem != null && _selItem.Slot != i)
			{
				var ss = new StringBuilder();
				ss.Append(_game.Language.GetString(i.Id).Arg(1 + _groundOffset / _groundSlotsX).Arg(1 + _xMax / _groundSlotsX).ToString());
				ss.Append(':');
				ss.Append(_selItem.GetMoveToCost(i).ToString(System.Globalization.CultureInfo.InvariantCulture));
				text.SetText(ss.ToString());
			}
			else
			{
				text.SetText(_game.Language.GetString(i.Id).Arg(1 + _groundOffset / _groundSlotsX).Arg(1 + _xMax / _groundSlotsX));
			}
			text.Blit(_gridLabels);
		}
	}

	private static readonly int[] Pulsate = [0, 1, 2, 3, 4, 3, 2, 1];

	/// <summary>Draws the items contained in the soldier's inventory.</summary>
	public void DrawItems()
	{
		SavedBattleGame save = _game.SavedGame.SavedBattle;
		Surface tempSurface = _game.Mod.GetSurfaceSet("SCANG.DAT").GetFrame(6);
		void primers(int x, int y, bool a)
		{
			tempSurface.BlitNShade(_items, x, y, Pulsate[_animFrame % 8], false, a ? 0 : 32);
		}
		void indicators(Surface surf, int x, int y)
		{
			surf.BlitNShade(_items, x, y, Pulsate[_animFrame % 8]);
		}

		_items.Clear();
		byte color = (byte)_game.Mod.GetInterface("inventory").GetElement("numStack").color;
		byte color2 = (byte)_game.Mod.GetInterface("inventory").GetElement("numStack").color2;
		if (_selUnit != null)
		{
			SurfaceSet texture = _game.Mod.GetSurfaceSet("BIGOBS.PCK");
			// Soldier items
			foreach (BattleItem invItem in _selUnit.Inventory)
			{
				Surface frame = invItem.GetBigSprite(texture, save, _animFrame);

				if (invItem == _selItem || frame == null)
					continue;

				int x, y;
				if (invItem.Slot.Type == INV_SLOT)
				{
					x = (invItem.Slot.X + invItem.SlotX * RuleInventory.SLOT_W);
					y = (invItem.Slot.Y + invItem.SlotY * RuleInventory.SLOT_H);
				}
				else if (invItem.Slot.Type == INV_HAND)
				{
					x = (invItem.Slot.X + invItem.Rules.HandSpriteOffX);
					y = (invItem.Slot.Y + invItem.Rules.HandSpriteOffY);
				}
				else
				{
					continue;
				}
				// C++ blits through ScriptWorkerBlit (BattleItem::ScriptFill, BODYPART_ITEM_INVENTORY); without recolor
				// scripts that is a plain blitNShade (script hooks)
				frame.BlitNShade(_items, x, y, 0);

				// two-handed indicator
				if (invItem.Slot.Type == INV_HAND)
				{
					if (invItem.Rules.IsTwoHanded || invItem.Rules.IsBlockingBothHands)
					{
						NumberText text = new NumberText(10, 5, 0, 0);
						text.SetPalette(Palette);
						text.SetColor(invItem.Rules.IsBlockingBothHands ? _twoHandedRed : _twoHandedGreen);
						text.SetBordered(false);
						text.X = invItem.Slot.X + RuleInventory.HAND_W * RuleInventory.SLOT_W - 5;
						text.Y = invItem.Slot.Y + RuleInventory.HAND_H * RuleInventory.SLOT_H - 7;
						text.Value = 2;
						text.Blit(_items);
					}
				}

				// grenade primer indicators
				if (invItem.FuseTimer >= 0 && invItem.Rules.InventoryWidth > 0)
				{
					primers(x, y, invItem.IsFuseEnabled);
				}
			}

			Surface stackLayer = new Surface(Width, Height, 0, 0);
			stackLayer.SetPalette(Palette);

			// Ground items
			int fatalWounds = 0;
			List<List<bool>> occupiedSlots = ClearOccupiedSlotsCache();
			foreach (BattleItem groundItem in _selUnit.Tile.Inventory)
			{
				Surface frame = groundItem.GetBigSprite(texture, save, _animFrame);
				// note that you can make items invisible by setting their width or height to 0 (for example used with tank corpse items)
				if (groundItem == _selItem || groundItem.Rules.InventoryHeight == 0 || groundItem.Rules.InventoryWidth == 0 || frame == null)
					continue;

				// check if item is in visible range
				if (groundItem.SlotX < _groundOffset || groundItem.SlotX >= _groundOffset + _groundSlotsX)
					continue;

				// check if something was draw here before
				List<bool> row = occupiedSlots[groundItem.SlotY];
				int col = groundItem.SlotX - _groundOffset;
				if (row[col])
				{
					continue;
				}
				else
				{
					row[col] = true;
				}

				int x, y;
				x = (groundItem.Slot.X + (groundItem.SlotX - _groundOffset) * RuleInventory.SLOT_W);
				y = (groundItem.Slot.Y + groundItem.SlotY * RuleInventory.SLOT_H);
				// C++ blits through ScriptWorkerBlit (script hooks), see above
				frame.BlitNShade(_items, x, y, 0);

				// grenade primer indicators
				if (groundItem.FuseTimer >= 0 && groundItem.Rules.InventoryWidth > 0)
				{
					primers(x, y, groundItem.IsFuseEnabled);
				}

				// fatal wounds
				fatalWounds = 0;
				if (groundItem.Unit != null)
				{
					// don't show on dead units
					if (groundItem.Unit.Status == STATUS_UNCONSCIOUS && groundItem.Unit.IndicatorsAreEnabled())
					{
						fatalWounds = groundItem.Unit.FatalWounds;
						if (_burnIndicator != null && groundItem.Unit.Fire > 0)
						{
							indicators(_burnIndicator, x, y);
						}
						else if (_woundIndicator != null && fatalWounds > 0)
						{
							indicators(_woundIndicator, x, y);
						}
						else if (_shockIndicator != null && groundItem.Unit.HasNegativeHealthRegen)
						{
							indicators(_shockIndicator, x, y);
						}
						else if (_stunIndicator != null)
						{
							indicators(_stunIndicator, x, y);
						}
					}
				}
				if (fatalWounds > 0)
				{
					_stackNumber.X = (groundItem.Slot.X + ((groundItem.SlotX + groundItem.Rules.InventoryWidth) - _groundOffset) * RuleInventory.SLOT_W) - 4;
					if (fatalWounds > 9)
					{
						_stackNumber.X = _stackNumber.X - 4;
					}
					_stackNumber.Y = (groundItem.Slot.Y + (groundItem.SlotY + groundItem.Rules.InventoryHeight) * RuleInventory.SLOT_H) - 6;
					_stackNumber.Value = (uint)fatalWounds;
					_stackNumber.Draw();
					_stackNumber.SetColor(color2);
					_stackNumber.Blit(stackLayer);
				}

				// item stacking
				if (GetStackLevel(groundItem.SlotX, groundItem.SlotY) > 1)
				{
					_stackNumber.X = (groundItem.Slot.X + ((groundItem.SlotX + groundItem.Rules.InventoryWidth) - _groundOffset) * RuleInventory.SLOT_W) - 4;
					if (GetStackLevel(groundItem.SlotX, groundItem.SlotY) > 9)
					{
						_stackNumber.X = _stackNumber.X - 4;
					}
					_stackNumber.Y = (groundItem.Slot.Y + (groundItem.SlotY + groundItem.Rules.InventoryHeight) * RuleInventory.SLOT_H) - 6;
					_stackNumber.Value = (uint)GetStackLevel(groundItem.SlotX, groundItem.SlotY);
					_stackNumber.Draw();
					_stackNumber.SetColor(color);
					_stackNumber.Blit(stackLayer);
				}
			}

			stackLayer.BlitNShade(_items, 0, 0);
		}
	}

	/// <summary>Draws the selected item.</summary>
	public void DrawSelectedItem()
	{
		if (_selItem != null)
		{
			_selection.Clear();
			_selItem.Rules.DrawHandSprite(_game.Mod.GetSurfaceSet("BIGOBS.PCK"), _selection, _selItem, _game.SavedGame.SavedBattle, _animFrame);
		}
	}

	/// <summary>Reads <c>_stackLevel[x][y]</c> (0 when missing, like the inserting C++ operator[]).</summary>
	private int GetStackLevel(int x, int y)
	{
		return _stackLevel.TryGetValue(x, out var col) && col.TryGetValue(y, out int v) ? v : 0;
	}

	/// <summary><c>_stackLevel[x][y] += delta</c>.</summary>
	private void AddStackLevel(int x, int y, int delta)
	{
		if (!_stackLevel.TryGetValue(x, out var col))
		{
			col = [];
			_stackLevel[x] = col;
		}
		col[y] = (col.TryGetValue(y, out int v) ? v : 0) + delta;
	}

	/// <summary>Clear all occupied slots markers.</summary>
	private List<List<bool>> ClearOccupiedSlotsCache()
	{
		foreach (List<bool> v in _occupiedSlotsCache)
		{
			for (int i = 0; i < v.Count; ++i)
			{
				v[i] = false;
			}
		}
		return _occupiedSlotsCache;
	}

	/// <summary>Moves an item to a specified slot in the selected player's inventory.</summary>
	private void MoveItem(BattleItem item, RuleInventory slot, int x, int y)
	{
		_game.SavedGame.SavedBattle.TileEngine.ItemMoveInventory(_selUnit.Tile, _selUnit, item, slot, x, y);
	}

	/// <summary>Checks if an item in a certain slot position would overlap with any other inventory item.</summary>
	/// <param name="unit">Pointer to current unit.</param>
	/// <param name="item">Pointer to battle item.</param>
	/// <param name="slot">Inventory slot, or NULL if none.</param>
	/// <param name="x">X position in slot.</param>
	/// <param name="y">Y position in slot.</param>
	/// <returns>If there's overlap.</returns>
	public static bool OverlapItems(BattleUnit unit, BattleItem item, RuleInventory slot, int x = 0, int y = 0)
	{
		ArgumentNullException.ThrowIfNull(unit);
		ArgumentNullException.ThrowIfNull(slot);
		if (slot.Type != INV_GROUND)
		{
			foreach (BattleItem bi in unit.Inventory)
			{
				if (bi.Slot == slot && bi.OccupiesSlot(x, y, item))
				{
					return true;
				}
			}
		}
		else if (unit.Tile != null)
		{
			foreach (BattleItem bi in unit.Tile.Inventory)
			{
				if (bi.OccupiesSlot(x, y, item))
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Gets the inventory slot located in the specified mouse position.</summary>
	/// <param name="x">Mouse X position. Returns the slot's X position.</param>
	/// <param name="y">Mouse Y position. Returns the slot's Y position.</param>
	/// <returns>Slot rules, or NULL if none.</returns>
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

	/// <summary>The item currently grabbed by the player (NULL if none); fixed items cannot be grabbed.</summary>
	public BattleItem SelectedItem
	{
		get => _selItem;
		set
		{
			_selItem = (value != null && !value.Rules.IsFixed) ? value : null;
			if (_selItem != null)
			{
				if (_selItem.Slot.Type == INV_GROUND)
				{
					AddStackLevel(_selItem.SlotX, _selItem.SlotY, -1);
				}
			}
			else
			{
				_selection.Clear();
			}
			DrawSelectedItem();
			DrawItems();
			if (_tu)
			{
				DrawGridLabels(!Options.oxceDisableInventoryTuCost);
			}
		}
	}

	/// <summary>Sets the search string.</summary>
	public void SetSearchString(string searchString)
	{
		_searchString = Unicode.UpperCase(searchString ?? "");
		ArrangeGround(1);
	}

	/// <summary>The item currently under mouse cursor (NULL if none).</summary>
	public BattleItem MouseOverItem
	{
		get => _mouseOverItem;
		set => _mouseOverItem = (value != null && !(value.Rules.IsFixed && value.Rules.BattleType == BT_NONE)) ? value : null;
	}

	/// <summary>Handles timers.</summary>
	public override void Think()
	{
		_warning.Think();
		_animTimer.Think(null, this);
	}

	/// <summary>Blits the inventory elements.</summary>
	public override void Blit(Surface surface)
	{
		Clear();
		_grid.BlitNShade(this, 0, 0);
		_items.BlitNShade(this, 0, 0);
		_gridLabels.BlitNShade(this, 0, 0);
		_selection.BlitNShade(this, _selection.X, _selection.Y);
		_warning.Blit(this);
		base.Blit(surface);
	}

	/// <summary>Moves the selected item.</summary>
	public override void MouseOver(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		_selection.X = (int)Math.Floor(action.AbsoluteXMouse) - _selection.Width / 2 - X;
		_selection.Y = (int)Math.Floor(action.AbsoluteYMouse) - _selection.Height / 2 - Y;
		if (_selUnit == null)
			return;

		int x = (int)Math.Floor(action.AbsoluteXMouse) - X,
			y = (int)Math.Floor(action.AbsoluteYMouse) - Y;
		RuleInventory slot = GetSlotInPosition(ref x, ref y);
		if (slot != null)
		{
			if (slot.Type == INV_GROUND)
			{
				x += _groundOffset;
			}
			BattleItem item = _selUnit.GetItem(slot, x, y);
			MouseOverItem = item;
		}
		else
		{
			MouseOverItem = null;
		}

		_selection.X = (int)Math.Floor(action.AbsoluteXMouse) - _selection.Width / 2 - X;
		_selection.Y = (int)Math.Floor(action.AbsoluteYMouse) - _selection.Height / 2 - Y;
		base.MouseOver(action, state);
	}

	/// <summary>Plays the item drop sound.</summary>
	private void PlayDropSound()
	{
		_game.Mod.GetSoundByDepth((uint)_depth, (uint)Mod.ITEM_DROP).Play();
	}

	/// <summary>Picks up / drops an item.</summary>
	public override void MouseClick(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (_game.IsLeftClick(action))
		{
			if (_selUnit == null)
				return;
			// Pickup item
			if (_selItem == null)
			{
				int x = (int)Math.Floor(action.AbsoluteXMouse) - X,
					y = (int)Math.Floor(action.AbsoluteYMouse) - Y;
				RuleInventory slot = GetSlotInPosition(ref x, ref y);
				if (slot != null)
				{
					if (slot.Type == INV_GROUND)
					{
						x += _groundOffset;
					}
					BattleItem item = _selUnit.GetItem(slot, x, y);
					if (item != null)
					{
						if (_game.IsShiftPressed())
						{
							bool quickUnload = false;
							bool allowed = true;
							// Quick-unload check
							if (!_tu)
							{
								// Outside of the battlescape, quick-unload:
								// - the weapon is never moved from its original slot
								// - the ammo always drops on the ground
								quickUnload = true;
							}
							else
							{
								if (item.Slot.Type != INV_HAND)
								{
									// During the battle, only weapons held in hand can be shift-unloaded
									allowed = false;
								}
							}
							if (allowed)
							{
								_selItem = item; // don't worry, we'll unselect it later!
								if (Unload(quickUnload))
								{
									PlayDropSound();
								}
								_selItem = null; // see, I told you!
							}
						}
						else if (item.Rules.IsFixed)
						{
							// do nothing!
						}
						else if (_game.IsCtrlPressed())
						{
							RuleInventory newSlot = _inventorySlotGround;
							string warning = "STR_NOT_ENOUGH_SPACE";
							bool placed = false;

							if (slot.Type == INV_GROUND)
							{
								// B1 - default slot by item
								if (!placed)
								{
									AddStackLevel(item.SlotX, item.SlotY, -1);

									if (item.Rules.DefaultInventorySlot != null && item.Rules.DefaultInventorySlot.Type != INV_GROUND)
									{
										newSlot = item.Rules.DefaultInventorySlot;

										placed = FitItem(newSlot, item, ref warning);
									}
								}

								// B2 - slot order by item category
								if (!placed)
								{
									RuleItemCategory cat = item.Rules.GetFirstCategoryWithInvOrder(_game.Mod);
									if (cat != null)
									{
										foreach (string s in cat.InvOrder)
										{
											if (placed)
											{
												break; // loop finished
											}
											newSlot = _game.Mod.GetInventory(s);
											if (newSlot.Type == INV_GROUND)
											{
												continue;
											}
											placed = FitItem(newSlot, item, ref warning);
										}
									}
								}

								// A1 - vanilla default attempt
								if (!placed)
								{
									// reset
									AddStackLevel(item.SlotX, item.SlotY, +1);
									newSlot = _inventorySlotGround;

									switch (item.Rules.BattleType)
									{
										case BT_FIREARM:
											newSlot = _inventorySlotRightHand;
											break;
										case BT_MINDPROBE:
										case BT_PSIAMP:
										case BT_MELEE:
										case BT_CORPSE:
											newSlot = _inventorySlotLeftHand;
											break;
										default:
											if (item.Rules.InventoryHeight > 2)
											{
												newSlot = _inventorySlotBackPack;
											}
											else
											{
												newSlot = _inventorySlotBelt;
											}
											break;
									}
								}
							}

							if (newSlot.Type != INV_GROUND)
							{
								// A1 - vanilla default attempt
								if (!placed)
								{
									AddStackLevel(item.SlotX, item.SlotY, -1);

									placed = FitItem(newSlot, item, ref warning);
								}

								if (!placed)
								{
									if (Mod.EXTENDED_INVENTORY_SLOT_SORTING)
									{
										// B3 - fallback: slot order by listOrder
										foreach (string s in _game.Mod.InvsList)
										{
											if (placed)
											{
												break; // loop finished
											}
											newSlot = _game.Mod.GetInventory(s);
											if (newSlot.Type == INV_GROUND)
											{
												continue;
											}
											placed = FitItem(newSlot, item, ref warning);
										}
									}
									else
									{
										// A2 - fallback: vanilla alphabetical slot order
										foreach (var wildCard in _game.Mod.Inventories)
										{
											if (placed)
											{
												break; // loop finished
											}
											newSlot = wildCard.Value;
											if (newSlot.Type == INV_GROUND)
											{
												continue;
											}
											placed = FitItem(newSlot, item, ref warning);
										}
									}
								}
								if (!placed)
								{
									AddStackLevel(item.SlotX, item.SlotY, +1);
								}
							}
							else
							{
								if (!_tu || _selUnit.SpendTimeUnits(item.GetMoveToCost(newSlot)))
								{
									placed = true;
									MoveItem(item, newSlot, 0, 0);
									PlayDropSound();
									ArrangeGround();
								}
								else
								{
									warning = "STR_NOT_ENOUGH_TIME_UNITS";
								}
							}

							if (!placed)
							{
								_warning.ShowMessage(_game.Language.GetString(warning));
							}
						}
						else
						{
							SelectedItem = item;
							if (item.FuseTimer >= 0)
							{
								_warning.ShowMessage(_game.Language.GetString(item.Rules.PrimeActionMessage));
							}
						}
					}
				}
			}
			// Drop item
			else
			{
				int x = _selection.X + (RuleInventory.HAND_W - _selItem.Rules.InventoryWidth) * RuleInventory.SLOT_W / 2 + RuleInventory.SLOT_W / 2,
					y = _selection.Y + (RuleInventory.HAND_H - _selItem.Rules.InventoryHeight) * RuleInventory.SLOT_H / 2 + RuleInventory.SLOT_H / 2;
				RuleInventory slot = GetSlotInPosition(ref x, ref y);
				if (slot != null)
				{
					if (slot.Type == INV_GROUND)
					{
						x += _groundOffset;
					}
					BattleItem item = _selUnit.GetItem(slot, x, y);

					bool canStack = slot.Type == INV_GROUND && CanBeStacked(item, _selItem);

					// Check if this inventory section supports the item
					if (!_selItem.Rules.CanBePlacedIntoInventorySection(slot))
					{
						_warning.ShowMessage(_game.Language.GetString("STR_CANNOT_PLACE_ITEM_INTO_THIS_SECTION"));
					}
					// Put item in empty slot, or stack it, if possible.
					else if (item == null || item == _selItem || canStack)
					{
						if (!OverlapItems(_selUnit, _selItem, slot, x, y) && slot.FitItemInSlot(_selItem.Rules, x, y))
						{
							if (!_tu || _selUnit.SpendTimeUnits(_selItem.GetMoveToCost(slot)))
							{
								MoveItem(_selItem, slot, x, y);
								if (slot.Type == INV_GROUND)
								{
									AddStackLevel(x, y, +1);
								}
								SelectedItem = null;
								PlayDropSound();
							}
							else
							{
								_warning.ShowMessage(_game.Language.GetString("STR_NOT_ENOUGH_TIME_UNITS"));
							}
						}
						else if (canStack)
						{
							if (!_tu || _selUnit.SpendTimeUnits(_selItem.GetMoveToCost(slot)))
							{
								MoveItem(_selItem, slot, item.SlotX, item.SlotY);
								AddStackLevel(item.SlotX, item.SlotY, +1);
								SelectedItem = null;
								PlayDropSound();
							}
							else
							{
								_warning.ShowMessage(_game.Language.GetString("STR_NOT_ENOUGH_TIME_UNITS"));
							}
						}
					}
					// Put item in weapon
					else if (item.IsWeaponWithAmmo)
					{
						int slotAmmo = item.Rules.GetSlotForAmmo(_selItem.Rules);
						if (slotAmmo == -1)
						{
							_warning.ShowMessage(_game.Language.GetString("STR_WRONG_AMMUNITION_FOR_THIS_WEAPON"));
						}
						else
						{
							// 4. the cost of loading the weapon with the new ammo (from the offhand)
							int tuCost = item.Rules.GetTULoad(slotAmmo);

							if (Mod.EXTENDED_ITEM_RELOAD_COST && _selItem.Slot.Type != INV_HAND)
							{
								// 3. the cost of moving the new ammo from the current slot to the offhand
								// Note: the cost for left/right hand might *NOT* be the same, but using the right hand "by definition"
								tuCost += _selItem.GetMoveToCost(_inventorySlotRightHand);
							}

							BattleItem weaponRightHand = _selUnit.RightHandWeapon;
							BattleItem weaponLeftHand = _selUnit.LeftHandWeapon;

							RuleInventory oldAmmoGoesTo = _inventorySlotGround;
							if (weaponRightHand == null || _selItem == weaponRightHand)
							{
								oldAmmoGoesTo = _inventorySlotRightHand;
							}
							else if (weaponLeftHand == null || _selItem == weaponLeftHand)
							{
								oldAmmoGoesTo = _inventorySlotLeftHand;
							}

							bool canLoad = true;
							if (item.GetAmmoForSlot(slotAmmo) != null)
							{
								int tuUnload = item.Rules.GetTUUnload(slotAmmo);
								if (_game.IsShiftPressed() && (!_tu || tuUnload != 0))
								{
									// Quick-swap check
									if (!_tu)
									{
										// Outside of the battlescape, the old ammo always drops on the ground
										oldAmmoGoesTo = _inventorySlotGround;
									}
									else
									{
										// During the battle, only weapons held in hand can use ammo quick-swap
										if (item.Slot.Type != INV_HAND)
										{
											canLoad = false;
										}
									}

									// 1. the cost of unloading the old ammo (to the offhand)
									tuCost += tuUnload;
									if (oldAmmoGoesTo == _inventorySlotGround)
									{
										// 2. the cost of dropping the old ammo on the ground (from the offhand)
										// Note: the cost for left/right hand is (should be) the same, so just using the right hand
										tuCost += _inventorySlotRightHand.GetCost(_inventorySlotGround);
									}
								}
								else
								{
									canLoad = false;
									_warning.ShowMessage(_game.Language.GetString("STR_WEAPON_IS_ALREADY_LOADED"));
								}
							}
							if (canLoad)
							{
								if (!_tu || _selUnit.SpendTimeUnits(tuCost))
								{
									bool arrangeFloor = false;
									BattleItem oldAmmo = item.SetAmmoForSlot(slotAmmo, _selItem);
									if (oldAmmo != null)
									{
										MoveItem(oldAmmo, oldAmmoGoesTo, 0, 0);
										if (oldAmmoGoesTo == _inventorySlotGround)
										{
											arrangeFloor = true;
										}
									}

									int sound = _selItem.Rules.ReloadSound;
									if (sound == Mod.NO_SOUND)
									{
										sound = item.Rules.ReloadSound;
									}
									if (sound == Mod.NO_SOUND)
									{
										sound = Mod.ITEM_RELOAD;
									}

									SelectedItem = null;
									_game.Mod.GetSoundByDepth((uint)_depth, (uint)sound).Play();
									if (arrangeFloor || item.Slot.Type == INV_GROUND)
									{
										ArrangeGround();
									}
								}
								else
								{
									_warning.ShowMessage(_game.Language.GetString("STR_NOT_ENOUGH_TIME_UNITS"));
								}
							}
						}
					}
					// else swap the item positions?
				}
				else
				{
					// try again, using the position of the mouse cursor, not the item (slightly more intuitive for stacking)
					x = (int)Math.Floor(action.AbsoluteXMouse) - X;
					y = (int)Math.Floor(action.AbsoluteYMouse) - Y;
					slot = GetSlotInPosition(ref x, ref y);
					if (slot != null && slot.Type == INV_GROUND)
					{
						x += _groundOffset;
						BattleItem item = _selUnit.GetItem(slot, x, y);
						if (CanBeStacked(item, _selItem))
						{
							if (!_tu || _selUnit.SpendTimeUnits(_selItem.GetMoveToCost(slot)))
							{
								MoveItem(_selItem, slot, item.SlotX, item.SlotY);
								AddStackLevel(item.SlotX, item.SlotY, +1);
								SelectedItem = null;
								PlayDropSound();
							}
							else
							{
								_warning.ShowMessage(_game.Language.GetString("STR_NOT_ENOUGH_TIME_UNITS"));
							}
						}
					}
				}
			}
		}
		else if (_game.IsRightClick(action))
		{
			if (_selItem == null)
			{
				if (!_base || Options.includePrimeStateInSavedLayout)
				{
					if (!_tu)
					{
						int x = (int)Math.Floor(action.AbsoluteXMouse) - X,
							y = (int)Math.Floor(action.AbsoluteYMouse) - Y;
						RuleInventory slot = GetSlotInPosition(ref x, ref y);
						if (slot != null)
						{
							if (slot.Type == INV_GROUND)
							{
								x += _groundOffset;
							}
							BattleItem item = _selUnit.GetItem(slot, x, y);
							if (item != null)
							{
								BattleFuseType fuseType = item.Rules.FuseTimerType;
								if (fuseType != BFT_NONE)
								{
									if (item.FuseTimer == -1)
									{
										if (item.Rules.CostPrime.Time > 0)
										{
											// Prime that grenade!
											if (fuseType == BFT_SET)
											{
												_game.PushState(new PrimeGrenadeState(null, true, item));
											}
											else
											{
												_warning.ShowMessage(_game.Language.GetString(item.Rules.PrimeActionMessage));
												item.FuseTimer = item.Rules.FuseTimerDefault;
												ArrangeGround();
												PlaySound(item.Rules.PrimeSound); // prime sound
											}
										}
									}
									else
									{
										if (item.Rules.CostUnprime.Time > 0 /* && !item->getRules()->getUnprimeActionName().empty() */ )
										{
											_warning.ShowMessage(_game.Language.GetString(item.Rules.UnprimeActionMessage));
											item.FuseTimer = -1;  // Unprime the grenade
											ArrangeGround();
											PlaySound(item.Rules.UnprimeSound); // unprime sound
										}
									}
								}
							}
						}
					}
					else
					{
						_game.PopState(); // Closes the inventory window on right-click (if not in preBattle equip screen!)
					}
				}
			}
			else
			{
				if (_selItem.Slot.Type == INV_GROUND)
				{
					AddStackLevel(_selItem.SlotX, _selItem.SlotY, +1);
				}
				// Return item to original position
				SelectedItem = null;
			}
		}
		else if (_game.IsMiddleClick(action))
		{
			if (_selUnit == null)
				return;

			int x = (int)Math.Floor(action.AbsoluteXMouse) - X,
				y = (int)Math.Floor(action.AbsoluteYMouse) - Y;
			RuleInventory slot = GetSlotInPosition(ref x, ref y);
			if (slot != null)
			{
				if (slot.Type == INV_GROUND)
				{
					x += _groundOffset;
				}
				BattleItem item = _selUnit.GetItem(slot, x, y);
				if (item != null)
				{
					string articleId = item.Rules.UfopediaType;
					Ufopaedia.OpenArticle(_game, articleId);
				}
			}
		}
		base.MouseClick(action, state);
	}

	/// <summary>Quickly drops the selected item on the ground.</summary>
	/// <returns>The success of the item being dropped.</returns>
	public bool QuickDrop()
	{
		if (_selUnit != null && _selItem != null)
		{
			if (!_tu || _selUnit.SpendTimeUnits(_selItem.GetMoveToCost(_inventorySlotGround)))
			{
				MoveItem(_selItem, _inventorySlotGround, 0, 0);
				SelectedItem = null;
				return true;
			}
			else
			{
				_warning.ShowMessage(_game.Language.GetString("STR_NOT_ENOUGH_TIME_UNITS"));
			}
		}

		return false;
	}

	/// <summary>
	/// Unloads the selected weapon, placing the gun on the right hand and the ammo on the left hand.
	/// Or if only one hand is free, the gun is placed in the hand and the ammo is placed on the ground.
	/// </summary>
	/// <param name="quickUnload">Quick unload using specific rules (the rules are different in and outside of the battlescape)</param>
	/// <returns>The success of the weapon being unloaded.</returns>
	public bool Unload(bool quickUnload = false)
	{
		// Must be holding an item
		if (_selItem == null)
		{
			// mobile support: https://openxcom.org/forum/index.php?topic=12880.0
			if (Options.oxceInventoryUnloadFixedWeapons)
			{
				if (_selUnit == null) return false;
				BattleItem[] weapons = [_selUnit.RightHandWeapon, _selUnit.LeftHandWeapon];
				foreach (BattleItem item in weapons)
				{
					if (item == null) continue;
					if (!item.Rules.IsFixed) continue;
					if (!item.HaveAnyAmmo()) continue;
					{
						_selItem = item; // don't worry, we'll unselect it later!
						bool success = Unload(!_tu);
						_selItem = null; // see, I told you!
						if (success) return true;
					}
				}
			}

			return false;
		}

		BattleType type = _selItem.Rules.BattleType;
		bool grenade = type == BT_GRENADE || type == BT_PROXIMITYGRENADE;
		bool weapon = type == BT_FIREARM || type == BT_MELEE;
		int slotForAmmoUnload = -1;
		int toForAmmoUnload = 0;

		// Item should be able to unload or unprimed.
		if (grenade)
		{
			// Item must be primed
			if (_selItem.FuseTimer == -1)
			{
				return false;
			}
			if (_selItem.Rules.FuseTimerType == BFT_NONE)
			{
				return false;
			}
			if (_selItem.Rules.CostUnprime.Time == 0 /* || _selItem->getRules()->getUnprimeActionName().empty() */ )
			{
				return false;
			}
		}
		else if (weapon)
		{
			// Item must be loaded
			bool showError = false;
			bool checkSlot(int slot)
			{
				if (!_selItem.NeedsAmmoForSlot(slot))
				{
					return false;
				}

				int tu = _selItem.Rules.GetTUUnload(slot);
				if (tu == 0 && !_tu)
				{
					return false;
				}

				BattleItem ammo = _selItem.GetAmmoForSlot(slot);
				if (ammo != null)
				{
					toForAmmoUnload = tu;
					slotForAmmoUnload = slot;
					return true;
				}
				else
				{
					showError = true;
					return false;
				}
			}
			// History lesson:
			// The old shift-unload logic by Yankes (i.e. unload ammo slots in reverse order) was removed from here,
			// because the new shift-unload logic by Fonzo/Ohartenstein now has a completely different meaning (i.e. quick-unload)
			{
				for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
				{
					if (checkSlot(slot))
					{
						break;
					}
				}
			}
			if (slotForAmmoUnload == -1)
			{
				if (showError)
				{
					_warning.ShowMessage(_game.Language.GetString("STR_NO_AMMUNITION_LOADED"));
				}
				return false;
			}
		}
		else
		{
			// not weapon or grenade, can't use unload button
			return false;
		}

		// Simplified logic for quick-unload outside of the battlescape
		if (quickUnload && !_tu)
		{
			// noop(); // 1. do not move the weapon at all!
			if (grenade)
			{
				_selItem.FuseTimer = -1;
				_warning.ShowMessage(_game.Language.GetString(_selItem.Rules.UnprimeActionMessage));
				PlaySound(_selItem.Rules.UnprimeSound); // unprime sound
				SelectedItem = null;
			}
			else
			{
				BattleItem oldAmmo = _selItem.SetAmmoForSlot(slotForAmmoUnload, null);
				MoveItem(oldAmmo, _inventorySlotGround, 0, 0); // 2. + 3. always drop the ammo on the ground
				SelectedItem = null; // calling before arrangeGround() to prevent undesired drawing of TU costs
				ArrangeGround();
			}
			return true;
		}

		// Check which hands are free.
		RuleInventory FirstFreeHand = _inventorySlotRightHand;
		RuleInventory SecondFreeHand = _inventorySlotLeftHand;

		foreach (BattleItem bi in _selUnit.Inventory)
		{
			if (bi.Slot.Type == INV_HAND && bi != _selItem)
			{
				if (bi.Slot == SecondFreeHand)
				{
					SecondFreeHand = null;
				}
				if (bi.Slot == FirstFreeHand)
				{
					FirstFreeHand = null;
				}
			}
		}

		if (FirstFreeHand == null)
		{
			FirstFreeHand = SecondFreeHand;
			SecondFreeHand = null;
		}
		if (FirstFreeHand == null)
		{
			_warning.ShowMessage(_game.Language.GetString("STR_ONE_HAND_MUST_BE_EMPTY"));
			return false;
		}

		BattleActionCost cost = new BattleActionCost(BA_NONE, _selUnit, _selItem);
		if (grenade)
		{
			cost.type = BA_UNPRIME;
			cost.UpdateTU();
		}
		else
		{
			// 2. unload (= move the ammo to the second free hand)
			cost.Time = (short)(cost.Time + toForAmmoUnload);

			if (SecondFreeHand == null)
			{
				// 3. drop the ammo on the ground (if the second hand is not free)
				cost.Time = (short)(cost.Time + FirstFreeHand.GetCost(_inventorySlotGround));
			}
		}
		if (cost.HaveTU() && _selItem.Slot.Type != INV_HAND)
		{
			// 1. move the weapon to the first free hand
			cost.Time = (short)(cost.Time + _selItem.GetMoveToCost(FirstFreeHand));
		}

		string err = "";
		if (!_tu || cost.SpendTU(ref err))
		{
			MoveItem(_selItem, FirstFreeHand, 0, 0); // 1.
			if (grenade)
			{
				_selItem.FuseTimer = -1;
				_warning.ShowMessage(_game.Language.GetString(_selItem.Rules.UnprimeActionMessage));
				PlaySound(_selItem.Rules.UnprimeSound); // unprime sound
			}
			else
			{
				BattleItem oldAmmo = _selItem.SetAmmoForSlot(slotForAmmoUnload, null);
				if (SecondFreeHand != null)
				{
					MoveItem(oldAmmo, SecondFreeHand, 0, 0); // 2.
				}
				else
				{
					MoveItem(oldAmmo, _inventorySlotGround, 0, 0); // 2. + 3.
					ArrangeGround();
				}
			}
			SelectedItem = null;
			return true;
		}
		else
		{
			if (!string.IsNullOrEmpty(err))
			{
				_warning.ShowMessage(_game.Language.GetString(err));
			}
			return false;
		}
	}

	/// <summary>Checks whether the given item is visible with the current search string.</summary>
	/// <returns>True if item should be shown. False otherwise.</returns>
	public bool IsInSearchString(BattleItem item)
	{
		ArgumentNullException.ThrowIfNull(item);
		if (_searchString.Length == 0)
		{
			// No active search string.
			return true;
		}

		string itemLocalName;
		if (!_game.SavedGame.IsResearched(item.Rules.Requirements))
		{
			// Alien artifact, shouldn't match on the real name.
			itemLocalName = _game.Language.GetString("STR_ALIEN_ARTIFACT");
		}
		else
		{
			itemLocalName = _game.Language.GetString(item.Rules.Name);
		}
		itemLocalName = Unicode.UpperCase(itemLocalName);
		if (itemLocalName.Contains(_searchString, StringComparison.Ordinal))
		{
			// Name match.
			return true;
		}

		// If present in the Ufopaedia, check categories for a match as well.
		ArticleDefinition articleID = _game.Mod.GetUfopaediaArticle(item.Rules.Type);
		if (articleID != null && Ufopaedia.IsArticleAvailable(_game.SavedGame, articleID))
		{
			foreach (string itemCategoryName in item.Rules.Categories)
			{
				string catLocalName = Unicode.UpperCase(_game.Language.GetString(itemCategoryName));
				if (catLocalName.Contains(_searchString, StringComparison.Ordinal))
				{
					// Category match
					return true;
				}
			}

			for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
			{
				if (item.GetAmmoForSlot(slot) != null)
				{
					foreach (string itemAmmoCategoryName in item.GetAmmoForSlot(slot).Rules.Categories)
					{
						string catLocalName = Unicode.UpperCase(_game.Language.GetString(itemAmmoCategoryName));
						if (catLocalName.Contains(_searchString, StringComparison.Ordinal))
						{
							// Category match
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Arranges items on the ground for the inventory display.
	/// Since items on the ground aren't assigned to anyone, they don't actually have permanent slot positions.
	/// </summary>
	/// <param name="alterOffset">Whether to alter the ground offset and in which direction.</param>
	public void ArrangeGround(int alterOffset = 0)
	{
		RuleInventory ground = _inventorySlotGround;

		int slotsX = _groundSlotsX;
		int slotsY = _groundSlotsY;
		_xMax = 0;
		_stackLevel.Clear();

		if (_selUnit != null)
		{
			// Create chart of free slots for later rapid lookup.
			List<List<bool>> occupiedSlots = ClearOccupiedSlotsCache();
			_xMax = ArrangeGroundItems(_selUnit.Tile.Inventory, ground, slotsX, slotsY, occupiedSlots, IsInSearchString, (sx, sy) => AddStackLevel(sx, sy, +1));
		}
		if (alterOffset > 0)
		{
			if (_xMax >= _groundOffset + slotsX)
			{
				_groundOffset += slotsX;
			}
			else
			{
				_groundOffset = 0;
			}
		}
		else if (alterOffset < 0)
		{
			// one step back
			_groundOffset -= slotsX;

			// if too much, as many steps forward as possible
			if (_groundOffset < 0)
			{
				while (_xMax >= _groundOffset + slotsX)
				{
					_groundOffset += slotsX;
				}
			}
		}
		DrawItems();
		DrawGridLabels(!Options.oxceDisableInventoryTuCost);
	}

	/// <summary>
	/// Placement part of <see cref="ArrangeGround"/> (the C++ <c>if (_selUnit != 0)</c> block), split off so it can be tested
	/// without a game: moves all ground items out of the way, groups them into stacks and places the stacks top-left first.
	/// </summary>
	/// <returns>The new <c>_xMax</c>.</returns>
	internal static int ArrangeGroundItems(List<BattleItem> groundInventory, RuleInventory ground, int slotsX, int slotsY, List<List<bool>> occupiedSlots, Func<BattleItem, bool> isInSearchString, System.Action<int, int> addStackLevel)
	{
		int xMax = 0;
		int x = 0;
		int y = 0;
		bool donePlacing;
		bool canPlace;
		// Lists of items of the same type (C++ unordered_map; only used for lookups)
		var typeItemLists = new Dictionary<string, List<List<BattleItem>>>(StringComparer.Ordinal);
		var itemListOrder = new List<BattleItem>(); // Placement order of item type stacks.
		var startIndexCacheX = new List<List<int>>(); // Cache for skipping to last known available position of a given size.

		// Move items out of the way and find which stack they'll end up in within the inventory.
		foreach (BattleItem bi in groundInventory)
		{
			// first move all items out of the way - a big number in X direction
			bi.Slot = ground;
			bi.SlotX = 1000000;
			bi.SlotY = 0;

			// Only handle visible items from this point on.
			if (bi.Rules.InventoryWidth != 0)
			{
				// Each item type has a list of stacks. Find / create a suitable one for this item.
				if (!typeItemLists.TryGetValue(bi.Rules.Type, out var itemList))
				{
					// New item type, create a list of item stacks for it.
					typeItemLists[bi.Rules.Type] = [[bi]];
					itemListOrder.Add(bi);
					// Figure out the largest item size present:
					x = Math.Max(x, bi.Rules.InventoryWidth);
					y = Math.Max(y, bi.Rules.InventoryHeight);
				}
				else
				{
					// With the item type found. Find which stack of this item type to add the item to.
					bool stacked = false;
					foreach (List<BattleItem> itemStack in itemList)
					{
						if (CanBeStacked(bi, itemStack[0]))
						{
							itemStack.Add(bi);
							stacked = true;
							break;
						}
					}
					if (!stacked)
					{
						// Not able to be stacked with previously placed items of this type. Make a new stack containing only this item.
						itemList.Add([bi]);
					}
				}
			}
		}
		// Create the cache of last placed index for a given item size.
		for (int i = 0; i < y + 1; ++i)
		{
			startIndexCacheX.Add(new List<int>(new int[x + 1]));
		}

		// Before we place the items, we should sort the itemListOrder vector using the 'list order' of the items.
		StdAlgorithm.Sort(itemListOrder, (a, b) => a.Rules.ListOrder < b.Rules.ListOrder);

		// Now for each item type, find the most topleft position that is not occupied and will fit.
		foreach (BattleItem bi2 in itemListOrder)
		{
			// Fetch the list of item stacks for this item type. Then place each stack.
			List<List<BattleItem>> itemList = typeItemLists[bi2.Rules.Type];
			foreach (List<BattleItem> itemStack in itemList)
			{
				BattleItem itemTypeSample = itemStack[0]; // Grab a sample of the stack of item type we're trying to place.
				if (!isInSearchString(itemTypeSample))
				{
					// quick search
					// Not a match with the active search string, skip this item stack. (Will remain outside the visible inventory)
					break;
				}

				int invWidth = itemTypeSample.Rules.InventoryWidth;
				int invHeight = itemTypeSample.Rules.InventoryHeight;

				// Start searching at the x value where we last placed an item of this size.
				// But also don't let more than half a screen behind the furtherst item (because we want to keep similar items together).
				x = Math.Max(xMax - slotsX / 2, startIndexCacheX[invHeight][invWidth]);
				y = 0;
				donePlacing = false;
				while (!donePlacing)
				{
					canPlace = true; // Assume we can put the item type here, if one of the following checks fails, we can't.
					for (int xd = 0; xd < invWidth && canPlace; xd++)
					{
						if ((x + xd) % slotsX < x % slotsX)
						{
							canPlace = false;
						}
						else
						{
							for (int yd = 0; yd < invHeight && canPlace; yd++)
							{
								// If there's room and no item, we can place it here. (No need to check for stackability as nothing stackable has been placed)
								canPlace = !occupiedSlots[y + yd][x + xd];
							}
						}
					}
					if (canPlace) // Found a place for this item stack.
					{
						xMax = Math.Max(xMax, x + invWidth);
						if ((x + startIndexCacheX[0].Count) >= occupiedSlots[0].Count)
						{
							// Filled enough for the widest item to potentially request occupancy checks outside of current cache. Expand slot cache.
							int newCacheSize = occupiedSlots[0].Count * 2;
							foreach (List<bool> j in occupiedSlots)
							{
								// std::vector::resize: grows with zeros (or shrinks)
								if (j.Count < newCacheSize)
								{
									j.AddRange(new bool[newCacheSize - j.Count]);
								}
								else if (j.Count > newCacheSize)
								{
									j.RemoveRange(newCacheSize, j.Count - newCacheSize);
								}
							}
						}
						// Reserve the slots this item will occupy.
						for (int xd = 0; xd < invWidth && canPlace; xd++)
						{
							for (int yd = 0; yd < invHeight && canPlace; yd++)
							{
								occupiedSlots[y + yd][x + xd] = true;
							}
						}

						// Place all items from this stack in the location we just reserved.
						foreach (BattleItem bi3 in itemStack)
						{
							bi3.SlotX = x;
							bi3.SlotY = y;
							addStackLevel(x, y);
						}
						donePlacing = true;
					}
					if (!donePlacing)
					{
						y++;
						if (y > slotsY - invHeight)
						{
							y = 0;
							x++;
						}
					}
				}
				// Now update the shortcut cache so that items to follow are able to skip a decent chunk of their search,
				// as there can be no spot before this x-address with available slots for items that are larger in one or more dimensions.
				int firstPossibleX = invHeight * 2 > slotsY ? x + invWidth : x;
				for (int offsetY = invHeight; offsetY < startIndexCacheX.Count; ++offsetY)
				{
					for (int offsetX = invWidth; offsetX < startIndexCacheX[offsetY].Count; ++offsetX)
					{
						startIndexCacheX[offsetY][offsetX] = Math.Max(firstPossibleX, startIndexCacheX[offsetY][offsetX]);
					}
				}
			} // end stacks for this item type
		} // end of item types
		return xMax;
	}

	/// <summary>Attempts to place the item in the inventory slot.</summary>
	/// <param name="newSlot">Where to place the item.</param>
	/// <param name="item">Item to be placed.</param>
	/// <param name="warning">Warning message if item could not be placed.</param>
	/// <returns>True, if the item was successfully placed in the inventory.</returns>
	public bool FitItem(RuleInventory newSlot, BattleItem item, ref string warning)
	{
		ArgumentNullException.ThrowIfNull(newSlot);
		ArgumentNullException.ThrowIfNull(item);
		// Check if this inventory section supports the item
		if (!item.Rules.CanBePlacedIntoInventorySection(newSlot))
		{
			warning = "STR_CANNOT_PLACE_ITEM_INTO_THIS_SECTION";
			return false;
		}

		bool placed = false;
		int maxSlotX = 0;
		int maxSlotY = 0;
		foreach (RuleSlot slotInfo in newSlot.Slots)
		{
			if (slotInfo.x > maxSlotX) maxSlotX = slotInfo.x;
			if (slotInfo.y > maxSlotY) maxSlotY = slotInfo.y;
		}
		for (int y2 = 0; y2 <= maxSlotY && !placed; ++y2)
		{
			for (int x2 = 0; x2 <= maxSlotX && !placed; ++x2)
			{
				if (!OverlapItems(_selUnit, item, newSlot, x2, y2) && newSlot.FitItemInSlot(item.Rules, x2, y2))
				{
					if (!_tu || _selUnit.SpendTimeUnits(item.GetMoveToCost(newSlot)))
					{
						placed = true;
						MoveItem(item, newSlot, x2, y2);
						PlayDropSound();
						DrawItems();
					}
					else
					{
						warning = "STR_NOT_ENOUGH_TIME_UNITS";
					}
				}
			}
		}
		return placed;
	}

	/// <summary>Checks if two items can be stacked on one another.</summary>
	/// <returns>True, if the items can be stacked on one another.</returns>
	public static bool CanBeStacked(BattleItem itemA, BattleItem itemB)
	{
		//both items actually exist
		if (itemA == null || itemB == null) return false;

		//both items have the same ruleset
		if (itemA.Rules != itemB.Rules) return false;

		for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
		{
			BattleItem ammoA = itemA.GetAmmoForSlot(slot);
			BattleItem ammoB = itemB.GetAmmoForSlot(slot);
			// or they both have ammo
			if (ammoA != null && ammoB != null)
			{
				// and the same ammo type
				if (ammoA.Rules != ammoB.Rules) return false;
				// and the same ammo quantity
				if (ammoA.AmmoQuantity != ammoB.AmmoQuantity) return false;
			}
			else if (ammoA != null || ammoB != null)
			{
				return false;
			}
		}

		return (
			// and neither is set to explode
			itemA.FuseTimer == -1 && itemB.FuseTimer == -1 &&
			// and neither is a corpse or unconscious unit
			itemA.Unit == null && itemB.Unit == null &&
			// and if it's a medikit, it has the same number of charges
			itemA.PainKillerQuantity == itemB.PainKillerQuantity &&
			itemA.HealQuantity == itemB.HealQuantity &&
			itemA.StimulantQuantity == itemB.StimulantQuantity);
	}

	/// <summary>Shows a warning message.</summary>
	public void ShowWarning(string msg)
	{
		_warning.ShowMessage(msg);
	}

	/// <summary>Animate surface.</summary>
	public void Animate()
	{
		if (_tu)
		{
			SavedBattleGame save = _game.SavedGame.SavedBattle;
			save.NextAnimFrame();
			_animFrame = save.AnimFrame;
		}
		else
		{
			_animFrame++;
		}

		DrawItems();
		DrawSelectedItem();
	}

	/// <summary>Get current animation frame for inventory.</summary>
	public int AnimFrame { get => _animFrame; }

	/// <summary>Play a sound.</summary>
	private void PlaySound(int sound)
	{
		if (sound != Mod.NO_SOUND)
		{
			_game.Mod.GetSoundByDepth((uint)_depth, (uint)sound).Play();
		}
	}
}
