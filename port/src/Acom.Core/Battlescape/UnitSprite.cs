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
// Ported from: src/Battlescape/UnitSprite.h, src/Battlescape/UnitSprite.cpp
// Port status: partial (missing: script hooks)
#nullable disable

using static Acom.MovementType;
using static Acom.SoldierGender;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

/// <summary>
/// A class that renders a specific unit, given its render rules
/// combining the right frames from the surfaceset.
/// </summary>
/// <remarks>
/// The OXCE scripts are not ported: sprite selection uses the default <c>selectUnitSprite</c>/<c>selectItemSprite</c>
/// scripts (index + offset), body parts are blitted with the default <c>recolorUnitSprite</c> script
/// (armor recolor + burn shade) and items with the default <c>recolorItemSprite</c> script (shade).
/// </remarks>
public class UnitSprite
{
	private const int BODYPART_TORSO = (int)UnitBodyPart.BODYPART_TORSO;
	private const int BODYPART_RIGHTARM = (int)UnitBodyPart.BODYPART_RIGHTARM;
	private const int BODYPART_LEFTARM = (int)UnitBodyPart.BODYPART_LEFTARM;
	private const int BODYPART_LEGS = (int)UnitBodyPartEx.BODYPART_LEGS;
	private const int BODYPART_COLLAPSING = (int)UnitBodyPartEx.BODYPART_COLLAPSING;
	private const int BODYPART_ITEM_RIGHTHAND = (int)UnitBodyPartEx.BODYPART_ITEM_RIGHTHAND;
	private const int BODYPART_ITEM_LEFTHAND = (int)UnitBodyPartEx.BODYPART_ITEM_LEFTHAND;
	private const int BODYPART_LARGE_TORSO = (int)UnitBodyPartEx.BODYPART_LARGE_TORSO;
	private const int BODYPART_LARGE_PROPULSION = (int)UnitBodyPartEx.BODYPART_LARGE_PROPULSION;
	private const int BODYPART_LARGE_TURRET = (int)UnitBodyPartEx.BODYPART_LARGE_TURRET;

	/// <summary>Placeholder for surface index for body part not used by normal OpenXcom.</summary>
	private const int InvalidSpriteIndex = -256;

	/// <summary>One graphic part of the unit: the frame, its body part and its offset.</summary>
	public class Part
	{
		public Surface src;
		public int bodyPart;
		public int offX;
		public int offY;

		public Part(int body, Surface s = null)
		{
			src = s;
			bodyPart = body;
			offX = 0;
			offY = 0;
		}
	}

	private BattleUnit _unit;
	private BattleItem _itemR, _itemL;
	private SurfaceSet _unitSurface;
	private readonly SurfaceSet _itemSurface, _fireSurface, _breathSurface, _facingArrowSurface;
	private readonly Surface _dest;
	private readonly SavedBattleGame _save;
	private readonly Mod _mod;
	private int _part, _drawingRoutine;
	private readonly int _animationFrame;
	private readonly bool _helmet;
	private readonly int _red, _blue;
	private int _x, _y, _shade, _burn;
	private GraphSubset _mask;

	/// <summary>Sets up a UnitSprite with the specified size and position.</summary>
	/// <param name="dest">Destination surface.</param>
	/// <param name="mod">Mod with the unit/item sprites.</param>
	/// <param name="save">Current battle.</param>
	/// <param name="frame">Animation frame.</param>
	/// <param name="helmet">Underwater (draws breathing bubbles, aquanaut torsos).</param>
	/// <param name="red">Colour of the facing indicator of neutral units.</param>
	/// <param name="blue">Colour of the facing indicator of hostile units.</param>
	public UnitSprite(Surface dest, Mod mod, SavedBattleGame save, int frame, bool helmet, int red, int blue)
	{
		ArgumentNullException.ThrowIfNull(mod);
		_itemSurface = mod.GetSurfaceSet("HANDOB.PCK");
		_fireSurface = mod.GetSurfaceSet("SMOKE.PCK");
		_breathSurface = mod.GetSurfaceSet("BREATH-1.PCK", false);
		_facingArrowSurface = mod.GetSurfaceSet("DETBLOB.DAT");
		_dest = dest;
		_save = save;
		_mod = mod;
		_animationFrame = frame;
		_helmet = helmet;
		_red = red;
		_blue = blue;
		_mask = new GraphSubset(0, 0);
	}

	/// <summary>Get item if can be visible on sprite.</summary>
	private static BattleItem GetIfVisible(BattleItem item)
	{
		if (item != null && (!item.Rules.IsFixed || item.Rules.FixedShow))
		{
			return item;
		}
		return null;
	}

	/// <summary>Get item sprite for item.</summary>
	/// <param name="p">Graphic part to fill.</param>
	/// <param name="item">item what we want draw.</param>
	/// <param name="dir">Offset of the frame (direction).</param>
	private void SelectItem(Part p, BattleItem item, int dir)
	{
		int index = item.Rules.HandSprite;

		//enforce compatibility with basic version
		if (_itemSurface.GetFrame(index + dir) == null)
		{
			throw new InvalidOperationException("Frame(s) missing in 'HANDOB.PCK' for item '" + item.Rules.Type + "'");
		}

		// default ModScript::SelectItemSprite: "add sprite_index sprite_offset; return sprite_index;" (script hooks)
		int result = index + dir;

		p.src = _itemSurface.GetFrame(result);
	}

	/// <summary>Get item sprite for unit body part.</summary>
	/// <param name="p">Graphic part to fill.</param>
	/// <param name="index">index of item sprite.</param>
	/// <param name="dir">Offset of the frame (direction, animation phase).</param>
	private void SelectUnit(Part p, int index, int dir)
	{
		var armor = _unit.Armor;

		//enforce compatibility with basic version
		if (InvalidSpriteIndex != index && _unitSurface.GetFrame(index + dir) == null)
		{
			throw new InvalidOperationException("Frame(s) missing in '" + armor.SpriteSheet + "' for armor '" + armor.Type + "'");
		}

		// default ModScript::SelectUnitSprite: "add sprite_index sprite_offset; return sprite_index;" (script hooks)
		int result = index + dir;

		p.src = _unitSurface.GetFrame(result);
	}

	/// <summary>Blit item sprite onto surface.</summary>
	/// <param name="item">item sprite, can be null.</param>
	private void BlitItem(Part item)
	{
		if (item.src == null)
		{
			return;
		}
		_dest.Lock();

		// default ModScript::RecolorItemSprite: "add_shade new_pixel shade" equals blitNShade for non-negative shades (script hooks)
		item.src.BlitNShade(_dest, _x + item.offX, _y + item.offY, _shade, _mask);

		_dest.Unlock();
	}

	/// <summary>Blit body sprite onto surface with optional recoloring.</summary>
	/// <param name="body">body part sprite, can be null.</param>
	private void BlitBody(Part body)
	{
		if (body.src == null)
		{
			return;
		}
		_dest.Lock();

		// default ModScript::RecolorUnitSprite: "unit.getRecolor new_pixel; add_burn_shade new_pixel burn shade;" (script hooks)
		BlitMasked(body.src, _dest, _x + body.offX, _y + body.offY, _mask, new RecolorBurnShadeOp(_unit.Recolor, _burn, _shade));

		_dest.Unlock();
	}

	/// <summary>Blits <paramref name="src"/> at (x, y) onto <paramref name="dest"/>, limited to <paramref name="mask"/> (destination area).</summary>
	private static void BlitMasked<TOp>(Surface src, Surface dest, int x, int y, GraphSubset mask, TOp op) where TOp : struct, Surface.IPixelOp
	{
		int sx0 = Math.Max(0, mask.beg_x - x), sy0 = Math.Max(0, mask.beg_y - y);
		int sx1 = Math.Min(src.Width, mask.end_x - x), sy1 = Math.Min(src.Height, mask.end_y - y);
		if (sx0 < sx1 && sy0 < sy1)
			src.BlitRect(dest, x, y, sx0, sy0, sx1, sy1, op);
	}

	/// <summary>
	/// The default unit recolor script per pixel: <c>getRecolorScript</c> (BattleUnit.cpp) followed by
	/// <c>helper::BurnShade</c>; a resulting 0 keeps the destination pixel (ScriptWorkerBlit::executeBlit).
	/// </summary>
	private readonly struct RecolorBurnShadeOp(List<(byte first, byte second)> recolor, int burn, int shade) : Surface.IPixelOp
	{
		public void Apply(ref byte dest, byte src)
		{
			if (src == 0)
			{
				return;
			}
			int pixel = src;

			// unit.getRecolor new_pixel
			int g = pixel & ShaderDrawHelper.ColorGroup;
			int s = pixel & ShaderDrawHelper.ColorShade;
			foreach (var p in recolor)
			{
				if (g == p.first)
				{
					pixel = s + p.second;
					break;
				}
			}

			// add_burn_shade new_pixel burn shade
			byte d = unchecked((byte)pixel);
			byte sp = d;
			if (sp != 0)
			{
				if (burn != 0)
				{
					byte tempBurn = unchecked((byte)((sp & ShaderDrawHelper.ColorShade) + burn));
					if (tempBurn > 26)
					{
						//nothing
					}
					else if (tempBurn > 15)
					{
						ShaderDrawHelper.StandardShade(ref d, ShaderDrawHelper.ColorShade, shade);
					}
					else
					{
						ShaderDrawHelper.StandardShade(ref d, unchecked((byte)((sp & ShaderDrawHelper.ColorGroup) + tempBurn)), shade);
					}
				}
				else
				{
					ShaderDrawHelper.StandardShade(ref d, sp, shade);
				}
			}

			if (d != 0) dest = d;
		}
	}

	/// <summary>
	/// Draws a unit, using the drawing rules of the unit.
	/// This function is called by Map, for each unit on the screen.
	/// </summary>
	public void Draw(BattleUnit unit, int part, int x, int y, int shade, GraphSubset mask, bool drawFacingIndicator)
	{
		ArgumentNullException.ThrowIfNull(unit);
		_x = x;
		_y = y;

		_unit = unit;
		_part = part;
		_shade = shade;
		_mask = mask;

		if (_unit.IsOut)
		{
			// unit is drawn as an item
			return;
		}

		var armor = _unit.Armor;

		_itemR = GetIfVisible(_unit.RightHandWeapon);
		_itemL = GetIfVisible(_unit.LeftHandWeapon);

		_unitSurface = _mod.GetSurfaceSet(armor.SpriteSheet);

		_drawingRoutine = armor.DrawingRoutine;

		_burn = 0;
		int overkill = _unit.OverKillDamage;
		int maxHp = _unit.BaseStats.health;
		if (overkill != 0)
		{
			if (overkill > maxHp)
			{
				_burn = 16 * (_unit.FallingPhase + 1) / armor.DeathFrames;
			}
			else
			{
				_burn = 16 * overkill * (_unit.FallingPhase + 1) / armor.DeathFrames / maxHp;
			}
		}

		// Call the matching routine
		switch (_drawingRoutine)
		{
		case 0: DrawRoutine0(); break;
		case 1: DrawRoutine1(); break;
		case 2: DrawRoutine2(); break;
		case 3: DrawRoutine3(); break;
		case 4: DrawRoutine4(); break;
		case 5: DrawRoutine5(); break;
		case 6: DrawRoutine6(); break;
		case 7: DrawRoutine7(); break;
		case 8: DrawRoutine8(); break;
		case 9: DrawRoutine9(); break;
		case 10: DrawRoutine0(); break;
		case 11: DrawRoutine11(); break;
		case 12: DrawRoutine12(); break;
		case 13: DrawRoutine0(); break;
		case 14: DrawRoutine0(); break;
		case 15: DrawRoutine0(); break;
		case 16: DrawRoutine16(); break;
		case 17: DrawRoutine4(); break;
		case 18: DrawRoutine4(); break;
		case 19: DrawRoutine19(); break;
		case 20: DrawRoutine20(); break;
		case 21: DrawRoutine21(); break;
		case 22: DrawRoutine3(); break;
		default: throw new InvalidOperationException("Unknown drawing routine " + _drawingRoutine + " for armor '" + armor.Type + "'"); // C++: out of bounds array access
		}
		// draw fire
		if (unit.Fire > 0)
		{
			_fireSurface.GetFrame(4 + (_animationFrame / 2) % 4).BlitNShade(_dest, _x, _y, 0, _mask);
		}
		if (_breathSurface != null && _helmet && unit.BreathExhaleFrame >= 0 && armor.DrawBubbles() && !unit.FloorAbove)
		{
			var tmpSurface = _breathSurface.GetFrame(unit.BreathExhaleFrame);
			if (tmpSurface != null)
			{
				// lower the bubbles for shorter or kneeling units.
				tmpSurface.BlitNShade(_dest, _x, _y - 30 + (22 - unit.Height), shade, _mask);
			}
		}
		if (drawFacingIndicator && part == 0)
		{
			// draw unit facing indicator
			var tmpSurface = _facingArrowSurface.GetFrame(7 + ((unit.Direction + 1) % 8));
			if (unit.OriginalFaction == FACTION_PLAYER)
			{
				tmpSurface.BlitNShade(_dest, _x, _y, 0);
			}
			else
			{
				Surface.BlitRaw(_dest, tmpSurface, _x, _y, 0, false, unit.OriginalFaction == FACTION_HOSTILE ? _blue : _red);
			}
		}
	}

	private static readonly int[] R0_YoffWalk = [1, 0, -1, 0, 1, 0, -1, 0]; // bobbing up and down
	private static readonly int[] R0_mutonYoffWalk = [1, 1, 0, 0, 1, 1, 0, 0]; // bobbing up and down (muton)
	private static readonly int[] R0_aquatoidYoffWalk = [1, 0, 0, 1, 2, 1, 0, 0]; // bobbing up and down (aquatoid)
	private static readonly int[] R0_offX = [8, 10, 7, 4, -9, -11, -7, -3]; // for the weapons
	private static readonly int[] R0_offY = [-6, -3, 0, 2, 0, -4, -7, -9]; // for the weapons
	private static readonly int[] R0_offX2 = [-8, 3, 5, 12, 6, -1, -5, -13]; // for the left handed weapons
	private static readonly int[] R0_offY2 = [1, -4, -2, 0, 3, 3, 5, 0]; // for the left handed weapons
	private static readonly int[] R0_offX3 = [0, 0, 2, 2, 0, 0, 0, 0]; // for the weapons (muton)
	private static readonly int[] R0_offY3 = [-3, -3, -1, -1, -1, -3, -3, -2]; // for the weapons (muton)
	private static readonly int[] R0_offX4 = [-8, 2, 7, 14, 7, -2, -4, -8]; // for the left handed weapons
	private static readonly int[] R0_offY4 = [-3, -3, -1, 0, 3, 3, 0, 1]; // for the left handed weapons
	private static readonly int[] R0_offX5 = [-1, 1, 1, 2, 0, -1, 0, 0]; // for the weapons (muton)
	private static readonly int[] R0_offY5 = [1, -1, -1, -1, -1, -1, -3, 0]; // for the weapons (muton)
	private static readonly int[] R0_offX6 = [0, 6, 6, 12, -4, -5, -5, -13]; // for the left handed rifles
	private static readonly int[] R0_offY6 = [-4, -4, -1, 0, 5, 0, 1, 0]; // for the left handed rifles
	private static readonly int[] R0_offX7 = [0, 6, 8, 12, 2, -5, -5, -13]; // for the left handed rifles (muton)
	private static readonly int[] R0_offY7 = [-4, -6, -1, 0, 3, 0, 1, 0]; // for the left handed rifles (muton)

	/// <summary>
	/// Drawing routine for XCom soldiers in overalls, sectoids (routine 0),
	/// mutons (routine 10),
	/// aquanauts (routine 13),
	/// calcinites, deep ones, gill men, lobster men, tasoths (routine 14),
	/// aquatoids (routine 15) (this one is no different, it just precludes breathing animations.
	/// </summary>
	private void DrawRoutine0()
	{
		Part torso = new(BODYPART_TORSO), legs = new(BODYPART_LEGS), leftArm = new(BODYPART_LEFTARM), rightArm = new(BODYPART_RIGHTARM), itemR = new(BODYPART_ITEM_RIGHTHAND), itemL = new(BODYPART_ITEM_LEFTHAND);
		// magic numbers
		const int legsStand = 16, legsKneel = 24;
		int maleTorso, femaleTorso, die, rarm1H, larm2H, rarm2H, rarmShoot, legsFloat, torsoHandsWeaponY = 0;
		if (_drawingRoutine <= 10)
		{
			die = 264; // ufo:eu death frame
			maleTorso = 32;
			femaleTorso = 267;
			rarm1H = 232;
			larm2H = 240;
			rarm2H = 248;
			rarmShoot = 256;
			legsFloat = 275;
		}
		else if (_drawingRoutine == 13)
		{
			if (_helmet)
			{
				die = 259; // aquanaut underwater death frame
				maleTorso = 32; // aquanaut underwater ion armour torso

				if (_unit.Armor.ForcedTorso == ForcedTorso.TORSO_USE_GENDER)
				{
					femaleTorso = 32; // aquanaut underwater plastic aqua armour torso
				}
				else
				{
					femaleTorso = 286; // aquanaut underwater magnetic ion armour torso
				}
				rarm1H = 248;
				larm2H = 232;
				rarm2H = rarmShoot = 240;
				legsFloat = 294;
			}
			else
			{
				die = 256; // aquanaut land death frame
				// aquanaut land torso
				maleTorso = 270;
				femaleTorso = 262;
				rarm1H = 248;
				larm2H = 232;
				rarm2H = rarmShoot = 240;
				legsFloat = 294;
			}
		}
		else
		{
			die = 256; // tftd unit death frame
			// tftd unit torso
			maleTorso = 32;
			femaleTorso = 262;
			rarm1H = 248;
			larm2H = 232;
			rarm2H = rarmShoot = 240;
			legsFloat = 294;
		}
		const int larmStand = 0, rarmStand = 8;
		const int legsWalk = 56;
		const int larmWalk = 40;
		const int rarmWalk = 48;
		const int offYKneel = 4;
		const int offXAiming = 0;
		const int soldierHeight = 22;

		int unitDir = _unit.Direction;
		int walkPhase = _unit.WalkingPhase;

		if (_unit.Status == STATUS_COLLAPSING)
		{
			Part coll = new(BODYPART_COLLAPSING);
			SelectUnit(coll, die, _unit.FallingPhase);
			BlitBody(coll);
			return;
		}
		if (_drawingRoutine == 0 || _helmet)
		{
			if ((_unit.Gender == GENDER_FEMALE && _unit.Armor.ForcedTorso != ForcedTorso.TORSO_ALWAYS_MALE)
				|| _unit.Armor.ForcedTorso == ForcedTorso.TORSO_ALWAYS_FEMALE)
			{
				SelectUnit(torso, femaleTorso, unitDir);
			}
			else
			{
				SelectUnit(torso, maleTorso, unitDir);
			}
		}
		else
		{
			if (_unit.Gender == GENDER_FEMALE)
			{
				SelectUnit(torso, femaleTorso, unitDir);
			}
			else
			{
				SelectUnit(torso, maleTorso, unitDir);
			}
		}


		// when walking, torso(fixed sprite) has to be animated up/down
		if (_unit.Status == STATUS_WALKING)
		{
			if (_drawingRoutine == 10)
				torsoHandsWeaponY = R0_mutonYoffWalk[walkPhase];
			else if (_drawingRoutine == 13 || _drawingRoutine == 14)
				torsoHandsWeaponY = R0_YoffWalk[walkPhase] + 1;
			else if (_drawingRoutine == 15)
				torsoHandsWeaponY = R0_aquatoidYoffWalk[walkPhase];
			else
				torsoHandsWeaponY = R0_YoffWalk[walkPhase];
			torso.offY = (torsoHandsWeaponY);
			SelectUnit(legs, legsWalk, 24 * unitDir + walkPhase);
			SelectUnit(leftArm, larmWalk, 24 * unitDir + walkPhase);
			SelectUnit(rightArm, rarmWalk, 24 * unitDir + walkPhase);
			if (_drawingRoutine == 10 && unitDir == 3)
			{
				leftArm.offY = (-1);
			}
		}
		else
		{
			if (_unit.IsKneeled)
			{
				SelectUnit(legs, legsKneel, unitDir);
			}
			else if (_unit.IsFloating && _unit.MovementType == MT_FLY)
			{
				SelectUnit(legs, legsFloat, unitDir);
			}
			else
			{
				SelectUnit(legs, legsStand, unitDir);
			}
			SelectUnit(leftArm, larmStand, unitDir);
			SelectUnit(rightArm, rarmStand, unitDir);
		}

		SortRifles();

		// holding an item
		if (_itemR != null)
		{
			// draw handob item
			if (_unit.Status == STATUS_AIMING && _itemR.Rules.IsTwoHanded)
			{
				int dir = (unitDir + 2) % 8;
				SelectItem(itemR, _itemR, dir);
				itemR.offX = (R0_offX[unitDir]);
				itemR.offY = (R0_offY[unitDir]);
			}
			else
			{
				SelectItem(itemR, _itemR, unitDir);
				if (_drawingRoutine == 10)
				{
					if (_itemR.Rules.IsTwoHanded)
					{
						itemR.offX = (R0_offX3[unitDir]);
						itemR.offY = (R0_offY3[unitDir]);
					}
					else
					{
						itemR.offX = (R0_offX5[unitDir]);
						itemR.offY = (R0_offY5[unitDir]);
					}
				}
				else
				{
					itemR.offX = (0);
					itemR.offY = (0);
				}
			}

			// draw arms holding the item
			if (_itemR.Rules.IsTwoHanded)
			{
				SelectUnit(leftArm, larm2H, unitDir);
				if (_unit.Status == STATUS_AIMING)
				{
					SelectUnit(rightArm, rarmShoot, unitDir);
				}
				else
				{
					SelectUnit(rightArm, rarm2H, unitDir);
				}
			}
			else
			{
				if (_drawingRoutine == 10)
					SelectUnit(rightArm, rarm2H, unitDir);
				else
					SelectUnit(rightArm, rarm1H, unitDir);
			}


			// the fixed arm(s) have to be animated up/down when walking
			if (_unit.Status == STATUS_WALKING)
			{
				itemR.offY = (itemR.offY + torsoHandsWeaponY);
				rightArm.offY = (torsoHandsWeaponY);
				if (_itemR.Rules.IsTwoHanded)
					leftArm.offY = (torsoHandsWeaponY);
			}
		}
		//if we are left handed or dual wielding...
		if (_itemL != null)
		{
			SelectUnit(leftArm, larm2H, unitDir);
			SelectItem(itemL, _itemL, unitDir);
			if (!_itemL.Rules.IsTwoHanded)
			{
				if (_drawingRoutine == 10)
				{
					itemL.offX = (R0_offX4[unitDir]);
					itemL.offY = (R0_offY4[unitDir]);
				}
				else
				{
					itemL.offX = (R0_offX2[unitDir]);
					itemL.offY = (R0_offY2[unitDir]);
				}
			}
			else
			{
				itemL.offX = (0);
				itemL.offY = (0);
				SelectUnit(rightArm, rarm2H, unitDir);
			}

			if (_unit.Status == STATUS_AIMING && _itemL.Rules.IsTwoHanded)
			{
				int dir = (unitDir + 2) % 8;
				SelectItem(itemL, _itemL, dir);
				if (_drawingRoutine == 10)
				{
					itemL.offX = (R0_offX7[unitDir]);
					itemL.offY = (R0_offY7[unitDir]);
				}
				else
				{
					itemL.offX = (R0_offX6[unitDir]);
					itemL.offY = (R0_offY6[unitDir]);
				}
				SelectUnit(rightArm, rarmShoot, unitDir);
			}

			if (_unit.Status == STATUS_WALKING)
			{
				itemL.offY = (itemL.offY + torsoHandsWeaponY);
				leftArm.offY = (torsoHandsWeaponY);
				if (_itemL.Rules.IsTwoHanded)
					rightArm.offY = (torsoHandsWeaponY);
			}
		}
		// offset everything but legs when kneeled
		if (_unit.IsKneeled)
		{
			int offsetTFTD = (_drawingRoutine == 13) ? 1 : 0; // tftd torsos are stubby.
			leftArm.offY = (offYKneel + offsetTFTD);
			rightArm.offY = (offYKneel + offsetTFTD);
			torso.offY = (offYKneel + offsetTFTD);
			itemR.offY = (itemR.offY + offYKneel + offsetTFTD);
			itemL.offY = (itemL.offY + offYKneel + offsetTFTD);
		}
		else if (_unit.Status != STATUS_WALKING)
		{
			leftArm.offY = (0);
			rightArm.offY = (0);
			torso.offY = (0);
		}

		// items are calculated for soldier height (22) - some aliens are smaller, so item is drawn lower.
		if (itemR.src != null)
		{
			itemR.offY = (itemR.offY + (soldierHeight - _unit.StandHeight));
		}
		if (itemL.src != null)
		{
			itemL.offY = (itemL.offY + (soldierHeight - _unit.StandHeight));
		}

		if (_unit.Status == STATUS_AIMING)
		{
			torso.offX = (offXAiming);
			legs.offX = (offXAiming);
			leftArm.offX = (offXAiming);
			rightArm.offX = (offXAiming);
			if (itemR.src != null)
				itemR.offX = (itemR.offX + offXAiming);
			if (itemL.src != null)
				itemL.offX = (itemL.offX + offXAiming);
		}
		else if (itemR.src == null && _drawingRoutine == 10 && _unit.Status == STATUS_WALKING && unitDir == 2)
		{
			rightArm.offX = (-6);
		}

		// blit order depends on unit direction, and whether we are holding a 2 handed weapon.
		switch (unitDir)
		{
		case 0: BlitItem(itemR); BlitItem(itemL); BlitBody(leftArm); BlitBody(legs); BlitBody(torso); BlitBody(rightArm); break;
		case 1: BlitBody(leftArm); BlitBody(legs); BlitItem(itemL); BlitBody(torso); BlitItem(itemR); BlitBody(rightArm); break;
		case 2: BlitBody(leftArm); BlitBody(legs); BlitBody(torso); BlitItem(itemL); BlitItem(itemR); BlitBody(rightArm); break;
		case 3:
			if (_unit.Status != STATUS_AIMING && ((_itemR != null && _itemR.Rules.IsTwoHanded) || (_itemL != null && _itemL.Rules.IsTwoHanded)))
			{
				BlitBody(legs); BlitBody(torso); BlitBody(leftArm); BlitItem(itemR); BlitItem(itemL); BlitBody(rightArm);
			}
			else
			{
				BlitBody(legs); BlitBody(torso); BlitBody(leftArm); BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL);
			}
			break;
		case 4: BlitBody(legs); BlitBody(rightArm); BlitBody(torso); BlitBody(leftArm); BlitItem(itemR); BlitItem(itemL); break;
		case 5:
			if (_unit.Status != STATUS_AIMING && ((_itemR != null && _itemR.Rules.IsTwoHanded) || (_itemL != null && _itemL.Rules.IsTwoHanded)))
			{
				BlitBody(rightArm); BlitBody(legs); BlitBody(torso); BlitBody(leftArm); BlitItem(itemR); BlitItem(itemL);
			}
			else
			{
				BlitBody(rightArm); BlitBody(legs); BlitItem(itemR); BlitItem(itemL); BlitBody(torso); BlitBody(leftArm);
			}
			break;
		case 6: BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); BlitBody(legs); BlitBody(torso); BlitBody(leftArm); break;
		case 7:
			if (_unit.Status != STATUS_AIMING && ((_itemR != null && _itemR.Rules.IsTwoHanded) || (_itemL != null && _itemL.Rules.IsTwoHanded)))
			{
				BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); BlitBody(leftArm); BlitBody(legs); BlitBody(torso);
			}
			else
			{
				BlitItem(itemR); BlitItem(itemL); BlitBody(leftArm); BlitBody(rightArm); BlitBody(legs); BlitBody(torso);
			}
			break;
		}
	}

	private static readonly int[] R1_yoffWalk = [0, 0, 0, 0, 0, 0, 0, 0]; // bobbing up and down
	private static readonly int[] R1_offX = [8, 10, 7, 4, -9, -11, -7, -3]; // for the weapons
	private static readonly int[] R1_offY = [-6, -3, 0, 2, 0, -4, -7, -9]; // for the weapons
	private static readonly int[] R1_offX2 = [-8, 3, 7, 13, 6, -3, -5, -13]; // for the weapons
	private static readonly int[] R1_offY2 = [1, -4, -1, 0, 3, 3, 5, 0]; // for the weapons
	private static readonly int[] R1_offX3 = [0, 6, 6, 12, -4, -5, -5, -13]; // for the left handed rifles
	private static readonly int[] R1_offY3 = [-4, -4, -1, 0, 5, 0, 1, 0]; // for the left handed rifles

	/// <summary>Drawing routine for floaters.</summary>
	private void DrawRoutine1()
	{
		Part torso = new(BODYPART_TORSO), leftArm = new(BODYPART_LEFTARM), rightArm = new(BODYPART_RIGHTARM), itemR = new(BODYPART_ITEM_RIGHTHAND), itemL = new(BODYPART_ITEM_LEFTHAND);
		// magic numbers
		const int stand = 16, walk = 24, die = 64;
		const int larm = 8, rarm = 0, larm2H = 67, rarm2H = 75, rarmShoot = 83, rarm1H = 91; // note that arms are switched vs "normal" sheets
		const int offXAiming = 0;

		if (_unit.Status == STATUS_COLLAPSING)
		{
			Part coll = new(BODYPART_COLLAPSING);
			SelectUnit(coll, die, _unit.FallingPhase);
			BlitBody(coll);
			return;
		}

		int unitDir = _unit.Direction;
		int walkPhase = _unit.WalkingPhase;

		SelectUnit(leftArm, larm, unitDir);
		SelectUnit(rightArm, rarm, unitDir);
		// when walking, torso(fixed sprite) has to be animated up/down
		if (_unit.Status == STATUS_WALKING)
		{
			SelectUnit(torso, walk, (int)((5 * unitDir) + (walkPhase / 1.6))); // floater only has 5 walk animations instead of 8
			torso.offY = (R1_yoffWalk[walkPhase]);
		}
		else
		{
			SelectUnit(torso, stand, unitDir);
		}

		SortRifles();

		// holding an item
		if (_itemR != null)
		{
			// draw handob item
			if (_unit.Status == STATUS_AIMING && _itemR.Rules.IsTwoHanded)
			{
				int dir = (_unit.Direction + 2) % 8;
				SelectItem(itemR, _itemR, dir);
				itemR.offX = (R1_offX[unitDir]);
				itemR.offY = (R1_offY[unitDir]);
			}
			else
			{
				SelectItem(itemR, _itemR, unitDir);
				itemR.offX = (0);
				itemR.offY = (0);
			}
			// draw arms holding the item
			if (_itemR.Rules.IsTwoHanded)
			{
				SelectUnit(leftArm, larm2H, unitDir);
				if (_unit.Status == STATUS_AIMING)
				{
					SelectUnit(rightArm, rarmShoot, unitDir);
				}
				else
				{
					SelectUnit(rightArm, rarm2H, unitDir);
				}
			}
			else
			{
				SelectUnit(rightArm, rarm1H, unitDir);
			}
		}

		//if we are left handed or dual wielding...
		if (_itemL != null)
		{
			SelectUnit(leftArm, larm2H, unitDir);
			SelectItem(itemL, _itemL, unitDir);
			if (!_itemL.Rules.IsTwoHanded)
			{
				itemL.offX = (R1_offX2[unitDir]);
				itemL.offY = (R1_offY2[unitDir]);
			}
			else
			{
				itemL.offX = (0);
				itemL.offY = (0);
				SelectUnit(rightArm, rarm2H, unitDir);
			}

			if (_unit.Status == STATUS_AIMING && _itemL.Rules.IsTwoHanded)
			{
				int dir = (unitDir + 2) % 8;
				SelectItem(itemL, _itemL, dir);
				itemL.offX = (R1_offX3[unitDir]);
				itemL.offY = (R1_offY3[unitDir]);
				SelectUnit(rightArm, rarmShoot, unitDir);
			}

			if (_unit.Status == STATUS_WALKING)
			{
				leftArm.offY = (R1_yoffWalk[walkPhase]);
				itemL.offY = (itemL.offY + R1_yoffWalk[walkPhase]);
				if (_itemL.Rules.IsTwoHanded)
					rightArm.offY = (R1_yoffWalk[walkPhase]);
			}
		}

		if (_unit.Status != STATUS_WALKING)
		{
			leftArm.offY = (0);
			rightArm.offY = (0);
			torso.offY = (0);
		}
		if (_unit.Status == STATUS_AIMING)
		{
			torso.offX = (offXAiming);
			leftArm.offX = (offXAiming);
			rightArm.offX = (offXAiming);
			if (itemR.src != null)
				itemR.offX = (itemR.offX + offXAiming);
			if (itemL.src != null)
				itemL.offX = (itemL.offX + offXAiming);
		}
		// blit order depends on unit direction.
		switch (unitDir)
		{
		case 0: BlitItem(itemR); BlitItem(itemL); BlitBody(leftArm); BlitBody(torso); BlitBody(rightArm); break;
		case 1: BlitBody(leftArm); BlitBody(torso); BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); break;
		case 2: BlitBody(leftArm); BlitBody(torso); BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); break;
		case 3: BlitBody(torso); BlitBody(leftArm); BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); break;
		case 4: BlitBody(torso); BlitBody(leftArm); BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); break;
		case 5: BlitBody(rightArm); BlitBody(torso); BlitBody(leftArm); BlitItem(itemR); BlitItem(itemL); break;
		case 6: BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); BlitBody(torso); BlitBody(leftArm); break;
		case 7: BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); BlitBody(leftArm); BlitBody(torso); break;
		}
	}

	private static readonly int[] R2_offX = [-2, -7, -5, 0, 5, 7, 2, 0]; // hovertank offsets
	private static readonly int[] R2_offy = [-1, -3, -4, -5, -4, -3, -1, -1]; // hovertank offsets

	/// <summary>Drawing routine for XCom tanks.</summary>
	private void DrawRoutine2()
	{
		Part s = new(BODYPART_LARGE_TORSO + _part);

		int hoverTank = _unit.OriginalMovementType == MT_FLY ? 32 : 0;
		int turret = _unit.TurretType;

		// draw the animated propulsion below the hwp
		if (hoverTank != 0)
		{
			if (_part > 0)
			{
				Part p = new(BODYPART_LARGE_PROPULSION + _part);
				SelectUnit(p, 104 + ((_part - 1) * 8), _animationFrame % 8);
				BlitBody(p);
			}
			else
			{
				// draw nothing, can be override by script
				Part p = new(BODYPART_LARGE_PROPULSION + _part);
				SelectUnit(p, InvalidSpriteIndex, _animationFrame % 8);
				BlitBody(p);
			}
		}

		// draw the tank itself
		SelectUnit(s, hoverTank + (_part * 8), _unit.Direction);
		BlitBody(s);

		// draw the turret, together with the last part
		if (_part == 3 && turret != -1)
		{
			Part t = new(BODYPART_LARGE_TURRET);
			SelectUnit(t, 64 + (turret * 8), _unit.TurretDirection);
			int turretOffsetX = 0;
			int turretOffsetY = -4;
			if (hoverTank != 0)
			{
				turretOffsetX += R2_offX[_unit.Direction];
				turretOffsetY += R2_offy[_unit.Direction];
			}
			t.offX = (turretOffsetX);
			t.offY = (turretOffsetY);
			BlitBody(t);
		}

	}

	/// <summary>
	/// Drawing routine for cyberdiscs. (3)
	/// and helicopters (22)
	/// </summary>
	private void DrawRoutine3()
	{
		Part s = new(BODYPART_LARGE_TORSO + _part);

		// draw the animated propulsion below the hwp
		if (_drawingRoutine == 3)
		{
			DrawPropulsion3();
		}

		SelectUnit(s, (_part * 8), _unit.Direction);

		BlitBody(s);

		// draw the animated propulsion above the hwp
		if (_drawingRoutine == 22)
		{
			DrawPropulsion3();
		}
	}

	/// <summary>The (identical) propulsion blocks of drawRoutine3.</summary>
	private void DrawPropulsion3()
	{
		if (_part > 0)
		{
			Part p = new(BODYPART_LARGE_PROPULSION + _part);
			SelectUnit(p, 32 + ((_part - 1) * 8), _animationFrame % 8);
			BlitBody(p);
		}
		else
		{
			// draw nothing, can be override by script
			Part p = new(BODYPART_LARGE_PROPULSION + _part);
			SelectUnit(p, InvalidSpriteIndex, _animationFrame % 8);
			BlitBody(p);
		}
	}

	private static readonly int[] R4_offX = [8, 10, 7, 4, -9, -11, -7, -3]; // for the weapons
	private static readonly int[] R4_offY = [-6, -3, 0, 2, 0, -4, -7, -9]; // for the weapons
	private static readonly int[] R4_offX2 = [-8, 3, 5, 12, 6, -1, -5, -13]; // for the weapons
	private static readonly int[] R4_offY2 = [1, -4, -2, 0, 3, 3, 5, 0]; // for the weapons
	private static readonly int[] R4_offX3 = [0, 6, 6, 12, -4, -5, -5, -13]; // for the left handed rifles
	private static readonly int[] R4_offY3 = [-4, -4, -1, 0, 5, 0, 1, 0]; // for the left handed rifles
	private static readonly int[] R4_standConvert = [3, 2, 1, 0, 7, 6, 5, 4]; // array for converting stand frames for some tftd civilians

	/// <summary>
	/// Drawing routine for civilians, ethereals, zombies (routine 4),
	/// tftd civilians, tftd zombies (routine 17), more tftd civilians (routine 18).
	/// Very easy: first 8 is standing positions, then 8 walking sequences of 8, finally death sequence of 3
	/// </summary>
	private void DrawRoutine4()
	{
		Part s = new(BODYPART_TORSO), itemR = new(BODYPART_ITEM_RIGHTHAND), itemL = new(BODYPART_ITEM_LEFTHAND);
		int stand = 0, walk = 8, die = 72;
		const int offXAiming = 0;

		if (_drawingRoutine == 17) // tftd civilian - first set
		{
			stand = 64;
			walk = 0;
		}
		else if (_drawingRoutine == 18) // tftd civilian - second set
		{
			stand = 140;
			walk = 76;
			die = 148;
		}

		int unitDir = _unit.Direction;

		if (_unit.Status == STATUS_COLLAPSING)
		{
			Part coll = new(BODYPART_COLLAPSING);
			SelectUnit(coll, die, _unit.FallingPhase);
			BlitBody(coll);
			return;
		}
		else if (_unit.Status == STATUS_WALKING)
		{
			SelectUnit(s, walk, (8 * unitDir) + _unit.WalkingPhase);
		}
		else if (_drawingRoutine != 17)
		{
			SelectUnit(s, stand, unitDir);
		}
		else
		{
			SelectUnit(s, stand, R4_standConvert[unitDir]);
		}

		SortRifles();

		if (_itemR != null && !_itemR.Rules.IsFixed)
		{
			// draw handob item
			if (_unit.Status == STATUS_AIMING && _itemR.Rules.IsTwoHanded)
			{
				int dir = (unitDir + 2) % 8;
				SelectItem(itemR, _itemR, dir);
				itemR.offX = (R4_offX[unitDir]);
				itemR.offY = (R4_offY[unitDir]);
			}
			else
			{
				if (_itemR.Slot.IsRightHand)
				{
					SelectItem(itemR, _itemR, unitDir);
					itemR.offX = (0);
					itemR.offY = (0);
				}
				else
				{
					SelectItem(itemR, _itemR, unitDir);
					itemR.offX = (R4_offX2[unitDir]);
					itemR.offY = (R4_offY2[unitDir]);
				}
			}
		}

		//if we are dual wielding...
		if (_itemL != null && !_itemL.Rules.IsFixed)
		{
			SelectItem(itemL, _itemL, unitDir);
			if (!_itemL.Rules.IsTwoHanded)
			{
				itemL.offX = (R4_offX2[unitDir]);
				itemL.offY = (R4_offY2[unitDir]);
			}
			else
			{
				itemL.offX = (0);
				itemL.offY = (0);
			}

			if (_unit.Status == STATUS_AIMING && _itemL.Rules.IsTwoHanded)
			{
				int dir = (unitDir + 2) % 8;
				SelectItem(itemL, _itemL, dir);
				itemL.offX = (R4_offX3[unitDir]);
				itemL.offY = (R4_offY3[unitDir]);
			}
		}

		if (_unit.Status == STATUS_AIMING)
		{
			s.offX = (offXAiming);
			if (itemR.src != null)
				itemR.offX = (itemR.offX + offXAiming);
			if (itemL.src != null)
				itemL.offX = (itemL.offX + offXAiming);
		}
		switch (unitDir)
		{
		case 0: BlitItem(itemL); BlitItem(itemR); BlitBody(s); break;
		case 1: BlitItem(itemL); BlitBody(s); BlitItem(itemR); break;
		case 2: BlitBody(s); BlitItem(itemL); BlitItem(itemR); break;
		case 3: BlitBody(s); BlitItem(itemR); BlitItem(itemL); break;
		case 4: BlitBody(s); BlitItem(itemR); BlitItem(itemL); break;
		case 5: BlitItem(itemR); BlitBody(s); BlitItem(itemL); break;
		case 6: BlitItem(itemR); BlitBody(s); BlitItem(itemL); break;
		case 7: BlitItem(itemR); BlitItem(itemL); BlitBody(s); break;
		}
	}

	/// <summary>Drawing routine for sectopods and reapers.</summary>
	private void DrawRoutine5()
	{
		Part s = new(BODYPART_LARGE_TORSO + _part);

		if (_unit.Status == STATUS_WALKING)
		{
			SelectUnit(s, 32 + (_part * 4), (_unit.Direction * 16) + ((_unit.WalkingPhase / 2) % 4));
		}
		else
		{
			SelectUnit(s, 0 + (_part * 8), _unit.Direction);
		}

		BlitBody(s);
	}

	private static readonly int[] R6_yoffWalk = [3, 3, 2, 1, 0, 0, 1, 2]; // bobbing up and down
	private static readonly int[] R6_xoffWalka = [0, 0, 1, 2, 3, 3, 2, 1];
	private static readonly int[] R6_xoffWalkb = [0, 0, -1, -2, -3, -3, -2, -1];
	private static readonly int[] R6_yoffStand = [2, 1, 1, 0, 0, 0, 0, 0];
	private static readonly int[] R6_offX = [8, 10, 5, 2, -8, -10, -5, -2]; // for the weapons
	private static readonly int[] R6_offY = [-6, -3, 0, 0, 2, -3, -7, -9]; // for the weapons
	private static readonly int[] R6_offX2 = [-8, 2, 7, 13, 7, 0, -3, -15]; // for the weapons
	private static readonly int[] R6_offY2 = [1, -4, -2, 0, 3, 3, 5, 0]; // for the weapons
	private static readonly int[] R6_offX3 = [0, 6, 6, 12, -4, -5, -5, -13]; // for the left handed rifles
	private static readonly int[] R6_offY3 = [-4, -4, -1, 0, 5, 0, 1, 0]; // for the left handed rifles

	/// <summary>Drawing routine for snakemen.</summary>
	private void DrawRoutine6()
	{
		Part torso = new(BODYPART_TORSO), legs = new(BODYPART_LEGS), leftArm = new(BODYPART_LEFTARM), rightArm = new(BODYPART_RIGHTARM), itemR = new(BODYPART_ITEM_RIGHTHAND), itemL = new(BODYPART_ITEM_LEFTHAND);
		// magic numbers
		const int Torso = 24, legsStand = 16, die = 96;
		const int larmStand = 0, rarmStand = 8, rarm1H = 99, larm2H = 107, rarm2H = 115, rarmShoot = 123;
		const int legsWalk = 32;
		const int offXAiming = 0;

		if (_unit.Status == STATUS_COLLAPSING)
		{
			Part coll = new(BODYPART_COLLAPSING);
			SelectUnit(coll, die, _unit.FallingPhase);
			BlitBody(coll);
			return;
		}

		int unitDir = _unit.Direction;
		int walkPhase = _unit.WalkingPhase;

		SelectUnit(torso, Torso, unitDir);
		SelectUnit(leftArm, larmStand, unitDir);
		SelectUnit(rightArm, rarmStand, unitDir);


		// when walking, torso(fixed sprite) has to be animated up/down
		if (_unit.Status == STATUS_WALKING)
		{
			int xoffWalk = 0;
			if (unitDir < 3)
				xoffWalk = R6_xoffWalka[walkPhase];
			if (unitDir < 7 && unitDir > 3)
				xoffWalk = R6_xoffWalkb[walkPhase];
			torso.offY = (R6_yoffWalk[walkPhase]);
			torso.offX = (xoffWalk);
			SelectUnit(legs, legsWalk, 8 * unitDir + walkPhase);
			rightArm.offY = (R6_yoffWalk[walkPhase]);
			leftArm.offY = (R6_yoffWalk[walkPhase]);
			rightArm.offX = (xoffWalk);
			leftArm.offX = (xoffWalk);
		}
		else
		{
			SelectUnit(legs, legsStand, unitDir);
		}

		SortRifles();

		// holding an item
		if (_itemR != null)
		{
			// draw handob item
			if (_unit.Status == STATUS_AIMING && _itemR.Rules.IsTwoHanded)
			{
				int dir = (unitDir + 2) % 8;
				SelectItem(itemR, _itemR, dir);
				itemR.offX = (R6_offX[unitDir]);
				itemR.offY = (R6_offY[unitDir]);
			}
			else
			{
				SelectItem(itemR, _itemR, unitDir);
				itemR.offX = (0);
				itemR.offY = (0);
				if (!_itemR.Rules.IsTwoHanded)
				{
					itemR.offY = (R6_yoffStand[unitDir]);
				}
			}


			// draw arms holding the item
			if (_itemR.Rules.IsTwoHanded)
			{
				SelectUnit(leftArm, larm2H, unitDir);
				if (_unit.Status == STATUS_AIMING)
				{
					SelectUnit(rightArm, rarmShoot, unitDir);
				}
				else
				{
					SelectUnit(rightArm, rarm2H, unitDir);
				}
			}
			else
			{
				SelectUnit(rightArm, rarm1H, unitDir);
			}


			// the fixed arm(s) have to be animated up/down when walking
			if (_unit.Status == STATUS_WALKING)
			{
				itemR.offY = (R6_yoffWalk[walkPhase]);
				rightArm.offY = (R6_yoffWalk[walkPhase]);
				if (_itemR.Rules.IsTwoHanded)
					leftArm.offY = (R6_yoffWalk[walkPhase]);
			}
		}
		//if we are left handed or dual wielding...
		if (_itemL != null)
		{
			SelectUnit(leftArm, larm2H, unitDir);
			SelectItem(itemL, _itemL, unitDir);
			if (!_itemL.Rules.IsTwoHanded)
			{
				itemL.offX = (R6_offX2[unitDir]);
				itemL.offY = (R6_offY2[unitDir]);
			}
			else
			{
				itemL.offX = (0);
				itemL.offY = (0);
				if (!_itemL.Rules.IsTwoHanded)
				{
					itemL.offY = (R6_yoffStand[unitDir]);
				}
				SelectUnit(rightArm, rarm2H, unitDir);
			}

			if (_unit.Status == STATUS_AIMING && _itemL.Rules.IsTwoHanded)
			{
				int dir = (unitDir + 2) % 8;
				SelectItem(itemL, _itemL, dir);
				itemL.offX = (R6_offX3[unitDir]);
				itemL.offY = (R6_offY3[unitDir]);
				SelectUnit(rightArm, rarmShoot, unitDir);
			}

			if (_unit.Status == STATUS_WALKING)
			{
				leftArm.offY = (R6_yoffWalk[walkPhase]);
				itemL.offY = (R6_offY2[unitDir] + R6_yoffWalk[walkPhase]);
				if (_itemL.Rules.IsTwoHanded)
					rightArm.offY = (R6_yoffWalk[walkPhase]);
			}
		}
		// offset everything but legs when kneeled
		if (_unit.Status != STATUS_WALKING)
		{
			leftArm.offY = (0);
			rightArm.offY = (0);
			torso.offY = (0);
		}
		if (_unit.Status == STATUS_AIMING)
		{
			torso.offX = (offXAiming);
			legs.offX = (offXAiming);
			leftArm.offX = (offXAiming);
			rightArm.offX = (offXAiming);
			if (itemR.src != null)
				itemR.offX = (itemR.offX + offXAiming);
			if (itemL.src != null)
				itemL.offX = (itemL.offX + offXAiming);
		}

		// blit order depends on unit direction.
		switch (unitDir)
		{
		case 0: BlitItem(itemR); BlitItem(itemL); BlitBody(leftArm); BlitBody(legs); BlitBody(torso); BlitBody(rightArm); break;
		case 1: BlitBody(leftArm); BlitBody(legs); BlitItem(itemL); BlitBody(torso); BlitItem(itemR); BlitBody(rightArm); break;
		case 2: BlitBody(leftArm); BlitBody(legs); BlitBody(torso); BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); break;
		case 3: BlitBody(legs); BlitBody(torso); BlitBody(leftArm); BlitBody(rightArm); BlitItem(itemR); BlitItem(itemL); break;
		case 4: BlitBody(rightArm); BlitBody(legs); BlitBody(torso); BlitBody(leftArm); BlitItem(itemR); BlitItem(itemL); break;
		case 5: BlitBody(rightArm); BlitBody(legs); BlitBody(torso); BlitBody(leftArm); BlitItem(itemR); BlitItem(itemL); break;
		case 6: BlitBody(rightArm); BlitBody(legs); BlitItem(itemR); BlitItem(itemL); BlitBody(torso); BlitBody(leftArm); break;
		case 7: BlitItem(itemR); BlitItem(itemL); BlitBody(leftArm); BlitBody(rightArm); BlitBody(legs); BlitBody(torso); break;
		}
	}

	private static readonly int[] R7_yoffWalk = [1, 0, -1, 0, 1, 0, -1, 0]; // bobbing up and down

	/// <summary>Drawing routine for chryssalid.</summary>
	private void DrawRoutine7()
	{
		Part torso = new(BODYPART_TORSO), legs = new(BODYPART_LEGS), leftArm = new(BODYPART_LEFTARM), rightArm = new(BODYPART_RIGHTARM);
		// magic numbers
		const int Torso = 24, legsStand = 16, die = 224;
		const int larmStand = 0, rarmStand = 8;
		const int legsWalk = 48;
		const int larmWalk = 32;
		const int rarmWalk = 40;

		if (_unit.Status == STATUS_COLLAPSING)
		{
			Part coll = new(BODYPART_COLLAPSING);
			SelectUnit(coll, die, _unit.FallingPhase);
			BlitBody(coll);
			return;
		}

		int unitDir = _unit.Direction;
		int walkPhase = _unit.WalkingPhase;

		SelectUnit(torso, Torso, unitDir);


		// when walking, torso(fixed sprite) has to be animated up/down
		if (_unit.Status == STATUS_WALKING)
		{
			torso.offY = (R7_yoffWalk[walkPhase]);
			SelectUnit(legs, legsWalk, 24 * unitDir + walkPhase);
			SelectUnit(leftArm, larmWalk, 24 * unitDir + walkPhase);
			SelectUnit(rightArm, rarmWalk, 24 * unitDir + walkPhase);
		}
		else
		{

			SelectUnit(legs, legsStand, unitDir);
			SelectUnit(leftArm, larmStand, unitDir);
			SelectUnit(rightArm, rarmStand, unitDir);
			leftArm.offY = (0);
			rightArm.offY = (0);
			torso.offY = (0);
		}

		// blit order depends on unit direction
		switch (unitDir)
		{
		case 0: BlitBody(leftArm); BlitBody(legs); BlitBody(torso); BlitBody(rightArm); break;
		case 1: BlitBody(leftArm); BlitBody(legs); BlitBody(torso); BlitBody(rightArm); break;
		case 2: BlitBody(leftArm); BlitBody(legs); BlitBody(torso); BlitBody(rightArm); break;
		case 3: BlitBody(legs); BlitBody(torso); BlitBody(leftArm); BlitBody(rightArm); break;
		case 4: BlitBody(rightArm); BlitBody(legs); BlitBody(torso); BlitBody(leftArm); break;
		case 5: BlitBody(rightArm); BlitBody(legs); BlitBody(torso); BlitBody(leftArm); break;
		case 6: BlitBody(rightArm); BlitBody(legs); BlitBody(torso); BlitBody(leftArm); break;
		case 7: BlitBody(leftArm); BlitBody(rightArm); BlitBody(legs); BlitBody(torso); break;
		}
	}

	private static readonly int[] R8_Pulsate = [0, 1, 2, 3, 4, 3, 2, 1];

	/// <summary>Drawing routine for silacoids.</summary>
	private void DrawRoutine8()
	{
		Part legs = new(BODYPART_TORSO);
		// magic numbers
		const int Body = 0, aim = 5, die = 6;

		SelectUnit(legs, Body, R8_Pulsate[_animationFrame % 8]);

		if (_unit.Status == STATUS_COLLAPSING)
		{
			Part coll = new(BODYPART_COLLAPSING);
			SelectUnit(coll, die, _unit.FallingPhase);
			BlitBody(coll);
			return;
		}

		if (_unit.Status == STATUS_AIMING)
			SelectUnit(legs, aim, 0);

		BlitBody(legs);
	}

	/// <summary>Drawing routine for celatids.</summary>
	private void DrawRoutine9()
	{
		Part torso = new(BODYPART_TORSO);
		// magic numbers
		const int Body = 0, die = 25;

		SelectUnit(torso, Body, _animationFrame % 8);

		if (_unit.Status == STATUS_COLLAPSING)
		{
			Part coll = new(BODYPART_COLLAPSING);
			SelectUnit(coll, die, _unit.FallingPhase);
			BlitBody(coll);
			return;
		}

		BlitBody(torso);
	}

	private static readonly int[] R11_offTurretX = [-2, -6, -5, 0, 5, 6, 2, 0]; // turret offsets
	private static readonly int[] R11_offTurretYAbove = [5, 3, 0, 0, 0, 3, 5, 4]; // turret offsets
	private static readonly int[] R11_offTurretYBelow = [-11, -13, -16, -16, -16, -13, -11, -12]; // turret offsets

	/// <summary>Drawing routine for tftd tanks.</summary>
	private void DrawRoutine11()
	{
		int body = 0;
		int animFrame = _unit.WalkingPhase % 4;
		if (_unit.OriginalMovementType == MT_FLY)
		{
			body = 128;
			animFrame = _animationFrame % 4;
		}

		Part s = new(BODYPART_LARGE_TORSO + _part);
		SelectUnit(s, body + (_part * 4), 16 * _unit.Direction + animFrame);
		s.offY = (4);
		BlitBody(s);

		int turret = _unit.TurretType;
		// draw the turret, overlapping all 4 parts
		if ((_part == 3 || _part == 0) && turret != -1 && !_unit.FloorAbove)
		{
			Part t = new(BODYPART_LARGE_TURRET);
			SelectUnit(t, 256 + (turret * 8), _unit.TurretDirection);
			t.offX = (R11_offTurretX[_unit.Direction]);
			if (_part == 3)
				t.offY = (R11_offTurretYBelow[_unit.Direction]);
			else
				t.offY = (R11_offTurretYAbove[_unit.Direction]);
			BlitBody(t);
		}

	}

	/// <summary>Drawing routine for hallucinoids.</summary>
	private void DrawRoutine12()
	{
		Part s = new(BODYPART_LARGE_TORSO + _part);

		SelectUnit(s, (_part * 8), _animationFrame % 8);

		BlitBody(s);
	}

	/// <summary>Drawing routine for biodrones.</summary>
	private void DrawRoutine16()
	{
		Part s = new(BODYPART_TORSO);
		// magic numbers
		const int die = 8;

		SelectUnit(s, 0, _animationFrame % 8);

		if ((_unit.Status == STATUS_COLLAPSING))
		{
			Part coll = new(BODYPART_COLLAPSING);
			SelectUnit(coll, die, _unit.FallingPhase);
			BlitBody(coll);
			return;
		}

		BlitBody(s);
	}

	/// <summary>Drawing routine for tentaculats.</summary>
	private void DrawRoutine19()
	{
		Part s = new(BODYPART_TORSO);
		// magic numbers
		const int stand = 0, move = 8, die = 16;

		if (_unit.Status == STATUS_COLLAPSING)
		{
			Part coll = new(BODYPART_COLLAPSING);
			SelectUnit(coll, die, _unit.FallingPhase);
			BlitBody(coll);
			return;
		}
		else if (_unit.Status == STATUS_WALKING)
		{
			SelectUnit(s, move, _unit.Direction);
		}
		else
		{
			SelectUnit(s, stand, _unit.Direction);
		}

		BlitBody(s);
	}

	/// <summary>Drawing routine for triscenes.</summary>
	private void DrawRoutine20()
	{
		Part s = new(BODYPART_LARGE_TORSO + _part);

		if (_unit.Status == STATUS_WALKING)
		{
			SelectUnit(s, (_part * 5), (_unit.WalkingPhase / 2 % 4) + 5 * (4 * _unit.Direction));
		}
		else
		{
			SelectUnit(s, (_part * 5), 5 * (4 * _unit.Direction));
		}

		BlitBody(s);
	}

	/// <summary>Drawing routine for xarquids.</summary>
	private void DrawRoutine21()
	{
		Part s = new(BODYPART_LARGE_TORSO + _part);

		SelectUnit(s, (_part * 4), (_unit.Direction * 16) + (_animationFrame % 4));

		BlitBody(s);
	}

	/// <summary>Determines which weapons to display in the case of two-handed weapons.</summary>
	private void SortRifles()
	{
		if (_itemR != null && _itemR.Rules.IsTwoHanded)
		{
			if (_itemL != null && _itemL.Rules.IsTwoHanded)
			{
				_itemR = _unit.GetActiveHand(_itemL, _itemR);
				_itemL = null;
			}
			else if (_unit.Status != STATUS_AIMING)
			{
				_itemL = null;
			}
		}
		else if (_itemL != null && _itemL.Rules.IsTwoHanded)
		{
			if (_unit.Status != STATUS_AIMING)
			{
				_itemR = null;
			}
		}
	}
}
