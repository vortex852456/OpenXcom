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
// Ported from: src/Battlescape/ItemSprite.h, src/Battlescape/ItemSprite.cpp
#nullable disable

namespace Acom;

/// <summary>
/// A class that renders a specific unit, given its render rules
/// combining the right frames from the surfaceset.
/// </summary>
public class ItemSprite
{
	private readonly SurfaceSet _itemSurface;
	private readonly int _animationFrame;
	private readonly Surface _dest;
	private readonly SavedBattleGame _save;
	/// <summary>Blit script worker (C++ creates one on the stack for every blit; ScriptFill resets it).</summary>
	private readonly ScriptWorkerBlit _work = new();

	/// <summary>Sets up a ItemSprite with the specified size and position.</summary>
	/// <param name="dest">Destination surface.</param>
	/// <param name="mod">Mod with the FLOOROB.PCK sprites.</param>
	/// <param name="_save">Current battle.</param>
	/// <param name="frame">Animation frame.</param>
#pragma warning disable CA1062 // a null mod is a programming error, C++ dereferences it the same way
	public ItemSprite(Surface dest, Mod mod, SavedBattleGame _save, int frame)
	{
		_itemSurface = mod.GetSurfaceSet("FLOOROB.PCK");
		_animationFrame = frame;
		_dest = dest;
		this._save = _save;
	}
#pragma warning restore CA1062

	/// <summary>
	/// Draws a item, using the drawing rules of the item or unit if it's corpse.
	/// This function is called by Map, for each item on the screen.
	/// </summary>
	public void Draw(BattleItem item, int x, int y, int shade)
	{
		ArgumentNullException.ThrowIfNull(item);
		Surface sprite = item.GetFloorSprite(_itemSurface, _save, _animationFrame, shade);
		if (sprite != null)
		{
			var work = _work;
			BattleItem.ScriptFill(work, item, _save, (int)UnitBodyPartEx.BODYPART_ITEM_FLOOR, _animationFrame, shade);
			work.ExecuteBlit(sprite, _dest, x, y, shade);
		}
	}

	/// <summary>Draws shadow of item.</summary>
	public void DrawShadow(BattleItem item, int x, int y)
	{
		ArgumentNullException.ThrowIfNull(item);
		Surface sprite = item.GetFloorSprite(_itemSurface, _save, _animationFrame, 16);
		if (sprite != null)
		{
			sprite.BlitNShade(_dest, x, y, 16);
		}
	}
}
