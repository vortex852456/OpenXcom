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
// Ported from: src/Battlescape/BattlescapeState.cpp (event handling, battle end, tooltips, resize)
#nullable disable

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using static Acom.BattleType;
using static Acom.UnitBodyPart;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

public partial class BattlescapeState
{
	/// <summary>
	/// Takes care of any events from the core game engine.
	/// </summary>
	public override void Handle(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (!_firstInit)
		{
			if (_game.Cursor.Visible || ((action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN || action.Details.type == SDLEventType.SDL_MOUSEBUTTONUP) && _game.IsRightClick(action)))
			{
				base.Handle(action);

				if (Options.touchEnabled == false && _isMouseScrolling && !Options.battleDragScrollInvert)
				{
					_map.SetSelectorPosition((int)((_cursorPosition.x - _game.Screen.CursorLeftBlackBand) / action.XScale), (int)((_cursorPosition.y - _game.Screen.CursorTopBlackBand) / action.YScale));
				}

				if (Options.oxceThumbButtons && action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
				{
					if (action.Details.button.button == SDLMouse.SDL_BUTTON_X1)
					{
						BtnNextSoldierClick(action);
					}
					else if (action.Details.button.button == SDLMouse.SDL_BUTTON_X2)
					{
						BtnPrevSoldierClick(action);
					}
				}

				if (action.Details.type == SDLEventType.SDL_KEYDOWN)
				{
					HandleKey(action);
				}
			}
		}
	}

	/// <summary>Hotkeys of <see cref="Handle"/> (keyboard part of the C++ handle()).</summary>
	private void HandleKey(Action action)
	{
		SDLKey key = action.Details.key.keysym.sym;
		bool ctrlPressed = _game.IsCtrlPressed();
		bool shiftPressed = _game.IsShiftPressed();
		bool altPressed = _game.IsAltPressed();

		// "shift-hotkey" - select without centering
		if (shiftPressed)
		{
			if (key == Options.keyBattleNextUnit)
			{
				BtnNextSoldierClick(action);
			}
			else if (key == Options.keyBattlePrevUnit)
			{
				BtnPrevSoldierClick(action);
			}
		}

		// "ctrl-b" - reopen briefing
		if (key == SDLKey.SDLK_b && ctrlPressed)
		{
			Craft ycraft = null;
			foreach (Base xbase in _game.SavedGame.Bases)
			{
				foreach (Craft xcraft in xbase.Crafts)
				{
					if (xcraft.IsInBattlescape)
					{
						ycraft = xcraft;
						break;
					}
				}
				if (ycraft != null) break;
			}

			_game.PushState(new BriefingState(ycraft, null, true));
		}
		// "ctrl-c" - camera: toggle show single map level
		else if (key == SDLKey.SDLK_c && ctrlPressed)
		{
			_map.Camera.ToggleShowSingleLayer();

			if (_map.Camera.ShowSingleLayer)
				WarningLongRaw(Tr("STR_SINGLE_MAP_LAYER_ACTIVATED"));
			else
				Warning("STR_SINGLE_MAP_LAYER_DEACTIVATED");
		}
		// "ctrl-f" - show fatal wounds
		else if (key == SDLKey.SDLK_f && ctrlPressed)
		{
			if (_save.Side == FACTION_PLAYER)
			{
				BattleUnit bu = _save.SelectedUnit;
				if (bu != null)
				{
					var ss = new StringBuilder();
					ss.Append((string)Tr("STR_FATAL_WOUNDS"));
					ss.Append('\n');
					for (int i = 0; i < (int)BODYPART_MAX; ++i)
					{
						if (bu.GetFatalWound((UnitBodyPart)i) != 0)
						{
							ss.Append('\n');
							ss.Append((string)_game.Language.GetString(BattleUnit.PARTS_STRING[i]));
						}
					}
					_game.PushState(new InfoboxState(ss.ToString()));
				}
			}
		}
		// "ctrl-h" - show hit log
		else if (key == SDLKey.SDLK_h && ctrlPressed)
		{
			if (_save.Side == FACTION_PLAYER)
			{
				if (Options.oxceDisableHitLog)
				{
					_game.PushState(new InfoboxState(Tr("STR_THIS_FEATURE_IS_DISABLED_4")));
				}
				else if (altPressed)
				{
					// turn diary
					_game.PushState(new TurnDiaryState(_save.HitLog));
				}
				else
				{
					// hit log
					string hitLogText = _save.HitLog.GetHitLogText();
					if (!string.IsNullOrEmpty(hitLogText))
						_game.PushState(new InfoboxState(hitLogText));
				}
			}
		}
		// "ctrl-Home" - reset default palettes
		else if (key == SDLKey.SDLK_HOME && ctrlPressed)
		{
			_paletteResetRequested = true;
			Init();
		}
		// "ctrl-End" - toggle between various debug vision/brightness modes
		else if (key == SDLKey.SDLK_END && ctrlPressed)
		{
			_map.ToggleDebugVisionMode();
		}
		// "ctrl-shift-Del" - clear TUs for all allied units
		else if (key == SDLKey.SDLK_DELETE && ctrlPressed && shiftPressed)
		{
			foreach (BattleUnit bu in _save.Units)
			{
				if (bu.Faction == _save.Side && !bu.IsOut)
				{
					bu.ClearTimeUnits();
				}
			}
			UpdateSoldierInfo();
		}
		// "ctrl-s" - switch xcom unit speed to max and back
		else if (key == SDLKey.SDLK_s && ctrlPressed)
		{
			if (_save.Side == FACTION_PLAYER)
			{
				if (Options.battleXcomSpeedOrig >= 1 && Options.battleXcomSpeedOrig <= 40)
				{
					Options.battleXcomSpeed = Options.battleXcomSpeedOrig;
					Options.battleXcomSpeedOrig = -1;
					Warning("STR_QUICK_MODE_DEACTIVATED");
				}
				else
				{
					Options.battleXcomSpeedOrig = Options.battleXcomSpeed;
					Options.battleXcomSpeed = 1;
					WarningLongRaw(Tr("STR_QUICK_MODE_ACTIVATED"));
				}
			}
			else
			{
				if (Options.battleAlienSpeedOrig >= 1 && Options.battleAlienSpeedOrig <= 40)
				{
					Options.battleAlienSpeed = Options.battleAlienSpeedOrig;
					Options.battleAlienSpeedOrig = -1;
					Warning("STR_QUICK_MODE_DEACTIVATED");
				}
				else
				{
					Options.battleAlienSpeedOrig = Options.battleAlienSpeed;
					Options.battleAlienSpeed = 1;
					Warning("STR_QUICK_MODE_ACTIVATED");
				}
			}
		}
		// "ctrl-x" - mute/unmute unit response sounds
		else if (key == SDLKey.SDLK_x && ctrlPressed)
		{
			if (_game.Mod.EnableUnitResponseSounds)
			{
				Options.oxceEnableUnitResponseSounds = !Options.oxceEnableUnitResponseSounds;
			}
		}
		// "ctrl-e" - experience log
		else if (key == SDLKey.SDLK_e && ctrlPressed)
		{
			if (altPressed)
			{
				_game.PushState(new NoExperienceState());
			}
			else if (shiftPressed)
			{
				_game.PushState(new ExperienceOverviewState(this));
			}
			else
			{
				var ss = new StringBuilder();
				ss.Append((string)Tr("STR_NO_EXPERIENCE_YET"));
				ss.Append("\n\n");
				bool first = true;
				foreach (BattleUnit bu in _save.Units)
				{
					if (bu.OriginalFaction == FACTION_PLAYER && !bu.IsOut)
					{
						if (bu.GeoscapeSoldier != null && !bu.HasGainedAnyExperience)
						{
							if (!first) ss.Append(", ");
							if (bu == _save.SelectedUnit)
								ss.Append(Unicode.TOK_COLOR_FLIP).Append(bu.GetName(_game.Language)).Append(Unicode.TOK_COLOR_FLIP);
							else
								ss.Append(bu.GetName(_game.Language));
							first = false;
						}
					}
				}
				_game.PushState(new InfoboxState(ss.ToString()));
			}
		}
		// "alt-c" - custom marker
		else if (key == SDLKey.SDLK_c && altPressed)
		{
			BattleUnit unitUnderTheCursor = GetUnitUnderTheCursor();
			// mark a friendly unit under the cursor
			if (unitUnderTheCursor != null && unitUnderTheCursor.Faction == FACTION_PLAYER && !unitUnderTheCursor.IsOut)
			{
				unitUnderTheCursor.CustomMarker = (unitUnderTheCursor.CustomMarker + 1) % 5; // rotate 4 colors + turned off
			}
		}
		// "ctrl-m" - melee damage preview
		else if (key == SDLKey.SDLK_m && ctrlPressed)
		{
			BattleUnit actor = _save.SelectedUnit;
			if (actor != null)
			{
				BattleItem leftWeapon = actor.LeftHandWeapon;

				BattleItem rightWeapon = actor.RightHandWeapon;

				BattleItem specialWeapon = FindEmptyHandSpecialWeapon(actor);

				BattleType type = BT_NONE;
				BattleItem anotherSpecialWeapon = actor.GetSpecialIconWeapon(ref type);
				if (anotherSpecialWeapon != null && anotherSpecialWeapon == specialWeapon)
				{
					anotherSpecialWeapon = null;
				}

				var ss = new StringBuilder();
				bool first = true;
				if (leftWeapon != null)
				{
					ss.Append(GetMeleeDamagePreview(actor, leftWeapon));
					first = false;
				}
				if (rightWeapon != null)
				{
					if (!first) ss.Append("\n\n");
					ss.Append(GetMeleeDamagePreview(actor, rightWeapon));
					first = false;
				}
				if (specialWeapon != null)
				{
					if (!first) ss.Append("\n\n");
					ss.Append(GetMeleeDamagePreview(actor, specialWeapon));
					first = false;
				}
				if (anotherSpecialWeapon != null)
				{
					if (!first) ss.Append("\n\n");
					ss.Append(GetMeleeDamagePreview(actor, anotherSpecialWeapon));
				}

				_game.PushState(new InfoboxState(ss.ToString()));
			}
		}
		// "ctrl-w" - warp unit
		else if ((_save.DebugMode || _save.IsPreview) && key == SDLKey.SDLK_w && ctrlPressed)
		{
			BattleUnit unit = _save.SelectedUnit;
			if (unit != null)
			{
				Position newPos = default;
				_map.GetSelectorPosition(ref newPos);
				if (_save.TileEngine.IsPositionValidForUnit(ref newPos, unit))
				{
					Debug("Beam me up Scotty");
					_save.Pathfinding.RemovePreview();

					unit.SetTile(_save.GetTile(newPos), _save);
					unit.SetPosition(newPos);

					//free refresh as bonus
					unit.UpdateUnitStats(true, false);
					_save.TileEngine.CalculateLighting(LightLayers.LL_UNITS);
					_battleGame.HandleState();
					UpdateSoldierInfo(true);
				}
			}
		}
		if (Options.debug)
		{
			// "ctrl-d" - enable debug mode
			if (key == SDLKey.SDLK_d && ctrlPressed)
			{
				_save.SetDebugMode();
				Debug("Debug Mode");
			}
			// "ctrl-v" - reset tile visibility
			else if (_save.DebugMode && key == SDLKey.SDLK_v && ctrlPressed)
			{
				Debug("Resetting tile visibility");
				_save.ResetTiles();
			}
			else if (_save.DebugMode && (key == SDLKey.SDLK_k || key == SDLKey.SDLK_j) && ctrlPressed)
			{
				bool stunOnly = (key == SDLKey.SDLK_j);
				BattleUnit unitUnderTheCursor = null;
				if (shiftPressed || altPressed)
				{
					unitUnderTheCursor = GetUnitUnderTheCursor();
				}
				if (shiftPressed)
				{
					// kill (ctrl-shift-k) or stun (ctrl-shift-j) just a single unit (under the cursor)
					if (unitUnderTheCursor != null && !unitUnderTheCursor.IsOut)
					{
						Debug("Bingo!");
						unitUnderTheCursor.Damage(new Position(0, 0, 0), 1000, _game.Mod.GetDamageType(stunOnly ? ItemDamageType.DT_STUN : ItemDamageType.DT_MELEE), _save, new BattleActionAttack());
					}
				}
				else
				{
					if (stunOnly)
					{
						// "ctrl-j" - stun all aliens
						Debug("Deploying Celine Dion album");
					}
					else
					{
						// "ctrl-k" - kill all aliens
						Debug("Influenza bacterium dispersed");
					}
					foreach (BattleUnit bu in _save.Units)
					{
						if (unitUnderTheCursor != null && unitUnderTheCursor == bu)
						{
							// kill (ctrl-alt-k) or stun (ctrl-alt-j) all aliens EXCEPT the one under the cursor
							continue;
						}
						if (bu.OriginalFaction == FACTION_HOSTILE && !bu.IsOut)
						{
							bu.Damage(new Position(0, 0, 0), 1000, _game.Mod.GetDamageType(stunOnly ? ItemDamageType.DT_STUN : ItemDamageType.DT_MELEE), _save, new BattleActionAttack());
						}
					}
				}
				_battleGame.CheckForCasualties(null, new BattleActionAttack(), true, false);
				_battleGame.HandleState();
			}
			else if (_save.DebugMode && (key == SDLKey.SDLK_m || key == SDLKey.SDLK_p) && ctrlPressed && shiftPressed)
			{
				BattleUnit unitUnderTheCursor = GetUnitUnderTheCursor();
				// mind control (ctrl-shift-m) or panic (ctrl-shift-p) just a single unit (under the cursor)
				if (unitUnderTheCursor != null && !unitUnderTheCursor.IsOut)
				{
					if (key == SDLKey.SDLK_p)
					{
						int moraleLoss = unitUnderTheCursor.ReduceByBravery(100);
						if (moraleLoss > 0)
						{
							Debug("Have you paid your taxes yet?");
							unitUnderTheCursor.MoraleChange(-moraleLoss);
							_game.PushState(new InfoboxState(_game.Language.GetString("STR_MORALE_ATTACK_SUCCESSFUL")));
						}
					}
					else
					{
						if (unitUnderTheCursor.Faction != FACTION_PLAYER)
						{
							Debug("My mind to your mind, my thoughts to your thoughts.");
							unitUnderTheCursor.ConvertToFaction(FACTION_PLAYER);
							//unitUnderTheCursor->recoverTimeUnits();
							unitUnderTheCursor.AllowReselect();
							unitUnderTheCursor.AbortTurn(); // resets unit status to STANDING
							_game.PushState(new InfoboxState(_game.Language.GetString("STR_MIND_CONTROL_SUCCESSFUL")));
						}
					}
				}
			}
			// f11 - voxel map dump
			else if (key == SDLKey.SDLK_F11)
			{
				SaveVoxelMap();
			}
			// f9 - ai
			else if (key == SDLKey.SDLK_F9 && Options.traceAI)
			{
				SaveAIMap();
			}
		}
		// quick save and quick load
		if (!_game.SavedGame.IsIronman && !_save.IsPreview)
		{
			if (key == Options.keyQuickSave)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_QUICK, _palette));
			}
			else if (key == Options.keyInstaSave)
			{
				_game.PushState(new SaveGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_INSTA, _palette));
			}
			else if (key == Options.keyQuickLoad)
			{
				_game.PushState(new LoadGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_QUICK, _palette));
			}
		}

		// voxel view dump
		if (key == Options.keyBattleVoxelView)
		{
			SaveVoxelView();
		}
	}

	/// <summary>The unit overlapping the tile under the map selector (repeated C++ block).</summary>
	private BattleUnit GetUnitUnderTheCursor()
	{
		Position newPos = default;
		_map.GetSelectorPosition(ref newPos);
		Tile tile = _save.GetTile(newPos);
		if (tile != null)
		{
			return tile.GetOverlappingUnit(_save);
		}
		return null;
	}

	/// <summary>
	/// Saves a map as used by the AI.
	/// </summary>
	public void SaveAIMap()
	{
		uint start = SDLTimer.SDL_GetTicks();
		BattleUnit unit = _save.SelectedUnit;
		if (unit == null) return;

		int w = _save.MapSizeX;
		int h = _save.MapSizeY;

		var img = new RgbImage(w * 8, h * 8);
		Logger.Info($"unit = {unit.Id}");

		Position tilePos = unit.Position;
		const int rw = 8, rh = 8;

		// (C++ runs a first loop over the discovered tiles here that does nothing)

		for (int y = 0; y < h; ++y)
		{
			tilePos.y = (short)y;
			for (int x = 0; x < w; ++x)
			{
				tilePos.x = (short)x;
				Tile t = _save.GetTile(tilePos);

				if (t == null) continue;
				if (!t.IsDiscovered(TilePart.O_FLOOR)) continue;

				int rx = x * rw;
				int ry = y * rh;

				if (t.GetTUCost((int)TilePart.O_FLOOR, MovementType.MT_FLY) != Pathfinding.INVALID_MOVE_COST && t.GetTUCost((int)TilePart.O_OBJECT, MovementType.MT_FLY) != Pathfinding.INVALID_MOVE_COST)
				{
					img.FillRect(rx, ry, rw, rh, 255, 0, 0x20);
					img.Character(rx, ry, '*', 0x7f, 0x7f, 0x7f, 0x7f);
				}
				else
				{
					if (t.Unit == null) img.FillRect(rx, ry, rw, rh, 0x50, 0x50, 0x50); // gray for blocked tile
				}

				for (int z = tilePos.z; z >= 0; --z)
				{
					Position pos = new(tilePos.x, tilePos.y, z);
					t = _save.GetTile(pos);
					BattleUnit wat = t.Unit;
					if (wat != null)
					{
						switch (wat.Faction)
						{
							case FACTION_HOSTILE:
								// #4080C0 is Volutar Blue
								img.Character(rx, ry, (tilePos.z - z) != 0 ? 'a' : 'A', 0x40, 0x80, 0xC0, 0xff);
								break;
							case FACTION_PLAYER:
								img.Character(rx, ry, (tilePos.z - z) != 0 ? 'x' : 'X', 255, 255, 127, 0xff);
								break;
							case FACTION_NEUTRAL:
								img.Character(rx, ry, (tilePos.z - z) != 0 ? 'c' : 'C', 255, 127, 127, 0xff);
								break;
							case FACTION_NONE:
							case FACTION_MAX:
								break;
						}
						break;
					}
					pos.z--;
					if (z > 0 && !t.HasNoFloor(_save)) break; // no seeing through floors
				}

				if (t.GetMapData(TilePart.O_NORTHWALL) != null && t.GetMapData(TilePart.O_NORTHWALL).GetTUCost(MovementType.MT_FLY) == Pathfinding.INVALID_MOVE_COST)
				{
					img.Line(rx, ry, rx + rw, ry, 0x50, 0x50, 0x50);
				}

				if (t.GetMapData(TilePart.O_WESTWALL) != null && t.GetMapData(TilePart.O_WESTWALL).GetTUCost(MovementType.MT_FLY) == Pathfinding.INVALID_MOVE_COST)
				{
					img.Line(rx, ry, rx, ry + rh, 0x50, 0x50, 0x50);
				}
			}
		}

		img.String(12, 12, "z = " + tilePos.z.ToString(CultureInfo.InvariantCulture), 0, 0, 0, 0x7f);

		string filename;
		int i = 0;
		do
		{
			filename = Options.GetMasterUserFolder() + "AIExposure" + i.ToString("D3", CultureInfo.InvariantCulture) + ".png";
			i++;
		}
		while (CrossPlatform.FileExists(filename));

		PngEncoder.WriteFile(filename, PngEncoder.EncodeRgb(img.Pixels, img.Width, img.Height));
		Logger.Info($"saveAIMap() completed in {SDLTimer.SDL_GetTicks() - start}ms.");
	}

	/// <summary>
	/// Saves a first-person voxel view of the battlescape.
	/// </summary>
	public void SaveVoxelView()
	{
		ReadOnlySpan<byte> pal =
		//			ground		west wall	north wall		object		enemy unit						xcom unit	neutral unit
		[0, 0, 0, 224, 224, 224, 192, 224, 255, 255, 224, 192, 128, 255, 128, 192, 0, 255, 0, 0, 0, 255, 255, 255, 224, 192, 0, 255, 64, 128];

		BattleUnit bu = _save.SelectedUnit;
		if (bu == null) return; //no unit selected
		var trajectory = new List<Position>();

		double ang_x, ang_y;
		bool black;
		Tile tile = null;
		var image = new List<byte>(512 * 512 * 3);
		int test;
		Position originVoxel = _save.TileEngine.GetSightOriginVoxel(bu);

		Position targetVoxel = default, hitPos = default;
		double dist = 0;
		bool debug = _save.DebugMode;
		double dir = ((double)bu.Direction + 4) / 4 * Math.PI;
		for (int y = -256 + 32; y < 256 + 32; ++y)
		{
			ang_y = (((double)y) / 640 * Math.PI + Math.PI / 2);
			for (int x = -256; x < 256; ++x)
			{
				if (Options.oxceFirstPersonViewFisheyeProjection)
				{
					ang_x = ((double)x / 1024) * Math.PI + dir;
					targetVoxel.x = (short)(originVoxel.x + (int)(-Math.Sin(ang_x) * 1024 * Math.Sin(ang_y)));
					targetVoxel.y = (short)(originVoxel.y + (int)(Math.Cos(ang_x) * 1024 * Math.Sin(ang_y)));
					targetVoxel.z = (short)(originVoxel.z + (int)(Math.Cos(ang_y) * 1024));
				}
				else
				{
					targetVoxel.x = (short)(originVoxel.x + (int)(-Math.Sin(dir + Math.PI / 2) * (x * 4) + Math.Cos(dir + Math.PI / 2) * (1024 + 512)));
					targetVoxel.y = (short)(originVoxel.y + (int)(Math.Cos(dir + Math.PI / 2) * (x * 4) + Math.Sin(dir + Math.PI / 2) * (1024 + 512)));
					targetVoxel.z = (short)(originVoxel.z + -y * 4);
				}

				trajectory.Clear();
				test = (int)_save.TileEngine.CalculateLineVoxel(originVoxel, targetVoxel, false, trajectory, bu, null, !debug) + 1;
				black = true;
				if (test != 0 && test != 6)
				{
					tile = _save.GetTile(trajectory[0].ToTile());
					if (debug
						|| (tile.IsDiscovered(TilePart.O_WESTWALL) && test == 2)
						|| (tile.IsDiscovered(TilePart.O_NORTHWALL) && test == 3)
						|| (tile.IsDiscovered(TilePart.O_FLOOR) && (test == 1 || test == 4))
						|| test == 5
						)
					{
						if (test == 5)
						{
							if (tile.Unit != null)
							{
								if (tile.Unit.Faction == FACTION_NEUTRAL) test = 9;
								else
								if (tile.Unit.Faction == FACTION_PLAYER) test = 8;
							}
							else
							{
								tile = _save.GetBelowTile(tile);
								if (tile != null && tile.Unit != null)
								{
									if (tile.Unit.Faction == FACTION_NEUTRAL) test = 9;
									else
									if (tile.Unit.Faction == FACTION_PLAYER) test = 8;
								}
							}
						}
						hitPos = trajectory[0];
						dist = Position.Distance(hitPos, originVoxel);
						black = false;
					}
				}

				if (black)
				{
					dist = 0;
				}
				else
				{
					if (dist > 1000) dist = 1000;
					if (dist < 1) dist = 1;
					dist = (1000 - (Math.Log(dist)) * 140) / 700;//140

					if (hitPos.x % 16 == 15)
					{
						dist *= 0.9;
					}
					if (hitPos.y % 16 == 15)
					{
						dist *= 0.9;
					}
					if (hitPos.z % 24 == 23)
					{
						dist *= 0.9;
					}
					if (dist > 1) dist = 1;
					if (tile != null) dist *= (16 - (double)tile.Shade) / 16;
				}

				image.Add((byte)(int)((double)(pal[test * 3 + 0]) * dist));
				image.Add((byte)(int)((double)(pal[test * 3 + 1]) * dist));
				image.Add((byte)(int)((double)(pal[test * 3 + 2]) * dist));
			}
		}

		string filename;
		int i = 0;
		do
		{
			filename = Options.GetMasterUserFolder() + "fpslook" + i.ToString("D3", CultureInfo.InvariantCulture) + ".png";
			i++;
		}
		while (CrossPlatform.FileExists(filename));

		PngEncoder.WriteFile(filename, PngEncoder.EncodeRgb(CollectionsMarshal.AsSpan(image), 512, 512));
	}

	/// <summary>
	/// Saves each layer of voxels on the bettlescape as a png.
	/// </summary>
	public void SaveVoxelMap()
	{
		ReadOnlySpan<byte> pal =
		[255, 255, 255, 224, 224, 224, 128, 160, 255, 255, 160, 128, 128, 255, 128, 192, 0, 255, 255, 255, 255, 255, 255, 255, 224, 192, 0, 255, 64, 128];

		Tile tile;
		int width = _save.MapSizeX * 16, height = _save.MapSizeY * 16;
		var image = new byte[width * height * 3];

		for (int z = 0; z < _save.MapSizeZ * 12; ++z)
		{
			int n = 0;
			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
					int test = (int)_save.TileEngine.VoxelCheck(new Position(x, y, z * 2), null, false) + 1;
					float dist = 1;
					if (x % 16 == 15)
					{
						dist *= 0.9f;
					}
					if (y % 16 == 15)
					{
						dist *= 0.9f;
					}

					// (compares the shifted value: V_OUTOFBOUNDS here means a unit voxel)
					if (test == (int)VoxelType.V_OUTOFBOUNDS)
					{
						tile = _save.GetTile(new Position(x / 16, y / 16, z / 12));
						if (tile.Unit != null)
						{
							if (tile.Unit.Faction == FACTION_NEUTRAL) test = 9;
							else
							if (tile.Unit.Faction == FACTION_PLAYER) test = 8;
						}
						else
						{
							tile = _save.GetBelowTile(tile);
							if (tile != null && tile.Unit != null)
							{
								if (tile.Unit.Faction == FACTION_NEUTRAL) test = 9;
								else
								if (tile.Unit.Faction == FACTION_PLAYER) test = 8;
							}
						}
					}

					image[n++] = (byte)(int)((float)pal[test * 3 + 0] * dist);
					image[n++] = (byte)(int)((float)pal[test * 3 + 1] * dist);
					image[n++] = (byte)(int)((float)pal[test * 3 + 2] * dist);
				}
			}

			string filename = Options.GetMasterUserFolder() + "voxel" + z.ToString("D2", CultureInfo.InvariantCulture) + ".png";
			PngEncoder.WriteFile(filename, PngEncoder.EncodeRgb(image, width, height));
		}
	}

	/// <summary>
	/// The 24-bit SDL surface of saveAIMap (R, G, B bytes per pixel) with the SDL/SDL_gfx calls it uses: clipped
	/// SDL_FillRect, lineRGBA (horizontal/vertical, opaque) and characterRGBA/stringRGBA (built-in 8x8 font, blended
	/// with the colour's alpha like an SDL alpha blit).
	/// </summary>
	private sealed class RgbImage(int width, int height)
	{
		public int Width { get; } = width;

		public int Height { get; } = height;

		public byte[] Pixels { get; } = new byte[width * height * 3];

		private void Plot(int x, int y, byte r, byte g, byte b, byte a)
		{
			if (x < 0 || y < 0 || x >= Width || y >= Height || a == 0)
				return;
			int i = (y * Width + x) * 3;
			if (a == 255)
			{
				Pixels[i] = r;
				Pixels[i + 1] = g;
				Pixels[i + 2] = b;
			}
			else
			{
				Pixels[i] = (byte)(Pixels[i] + (r - Pixels[i]) * a / 255);
				Pixels[i + 1] = (byte)(Pixels[i + 1] + (g - Pixels[i + 1]) * a / 255);
				Pixels[i + 2] = (byte)(Pixels[i + 2] + (b - Pixels[i + 2]) * a / 255);
			}
		}

		public void FillRect(int x, int y, int w, int h, byte r, byte g, byte b)
		{
			for (int iy = y; iy < y + h; ++iy)
				for (int ix = x; ix < x + w; ++ix)
					Plot(ix, iy, r, g, b, 255);
		}

		/// <summary>lineRGBA for horizontal and vertical lines (both end points drawn).</summary>
		public void Line(int x1, int y1, int x2, int y2, byte r, byte g, byte b)
		{
			for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); ++y)
				for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); ++x)
					Plot(x, y, r, g, b, 255);
		}

		public void Character(int x, int y, char c, byte r, byte g, byte b, byte a)
		{
			var glyph = GfxPrimitives.Glyph((byte)c);
			for (int iy = 0; iy < GfxPrimitives.CharSize; ++iy)
				for (int ix = 0; ix < GfxPrimitives.CharSize; ++ix)
					if ((glyph[iy] & (0x80 >> ix)) != 0)
						Plot(x + ix, y + iy, r, g, b, a);
		}

		public void String(int x, int y, string s, byte r, byte g, byte b, byte a)
		{
			foreach (char c in s)
			{
				Character(x, y, c, r, g, b, a);
				x += GfxPrimitives.CharSize;
			}
		}
	}

	/// <summary>
	/// Finishes up the current battle, shuts down the battlescape
	/// and presents the debriefing screen for the mission.
	/// </summary>
	/// <param name="abort">Was the mission aborted?</param>
	/// <param name="inExitArea">Number of soldiers in the exit area OR number of survivors when battle finished due to either all aliens or objective being destroyed.</param>
	public void FinishBattle(bool abort, int inExitArea)
	{
		bool isPreview = _save.IsPreview;

		while (!_game.IsState(this))
		{
			_game.PopState();
		}
		_game.Cursor.Visible = true;
		if (_save.AmbientSound != Mod.NO_SOUND)
		{
			_game.Mod.GetSoundByDepth(0, (uint)_save.AmbientSound).StopLoop();
		}

		// reset touch flags
		_game.ResetTouchButtonFlags();

		// dear civilians and summoned player units,
		// please drop all borrowed xcom equipment now, so that we can recover it
		// thank you!
		var itemsToDrop = new List<BattleItem>();
		foreach (BattleUnit unit in _save.Units)
		{
			bool relevantUnitType = unit.OriginalFaction == FACTION_NEUTRAL || unit.IsSummonedPlayerUnit;
			if (relevantUnitType && !unit.IsOut)
			{
				itemsToDrop.Clear();
				foreach (BattleItem item in unit.Inventory)
				{
					if (item.XCOMProperty || item.Unit != null)
					{
						itemsToDrop.Add(item);
					}
				}
				foreach (BattleItem xcomItem in itemsToDrop)
				{
					_save.TileEngine.ItemDrop(unit.Tile, xcomItem, false);
				}
			}
		}

		// let's count summoned player-controlled VIPs before we remove them :)
		_battleGame.TallySummonedVIPs();
		// this removes player-controlled VIPs (not civilian VIPs)
		_battleGame.RemoveSummonedPlayerUnits();

		AlienDeployment ruleDeploy = _game.Mod.GetDeployment(_save.MissionType);
		if (ruleDeploy == null)
		{
			foreach (Ufo ufo in _game.SavedGame.Ufos)
			{
				if (ufo.IsInBattlescape)
				{
					string ufoMissionName = ufo.Rules.Type;
					if (!string.IsNullOrEmpty(_save.AlienCustomMission))
					{
						// fake underwater UFO
						ufoMissionName = _save.AlienCustomMission;
					}
					ruleDeploy = _game.Mod.GetDeployment(ufoMissionName);
					break;
				}
			}
		}
		string nextStage = "";
		if (ruleDeploy != null)
		{
			nextStage = ruleDeploy.NextStage;
		}

		if (!string.IsNullOrEmpty(nextStage) && inExitArea != 0 && !isPreview)
		{
			// if there is a next mission stage + we have people in exit area OR we killed all aliens, load the next stage
			_popups.Clear();
			_save.MissionType = nextStage;
			var bgen = new BattlescapeGenerator(_game);
			bgen.NextStage();
			_game.PopState();
			_game.PushState(new BriefingState(null, null));
		}
		else
		{
			_popups.Clear();
			_animTimer.Stop();
			_gameTimer.Stop();
			_game.PopState();
			if (isPreview)
			{
				// skip Debriefing
				Options.baseXResolution = Options.baseXGeoscape;
				Options.baseYResolution = Options.baseYGeoscape;
				_game.Screen.ResetDisplay(false);

				// Restore the cursor in case something weird happened
				_game.Cursor.Visible = true;

				// delete SavedBattleGame
				_game.SavedGame.SetBattleGame(null);

				// unmark all craft and all bases (current craft would be enough, but better safe than sorry)
				foreach (Base xbase in _game.SavedGame.Bases)
				{
					xbase.SetInBattlescape(false);
					foreach (Craft craft in xbase.Crafts)
					{
						craft.SetInBattlescape(false);
					}
				}

				// reset the music
				_game.Mod.PlayMusic("GMGEO");
				return;
			}
			_game.PushState(new DebriefingState());
			string cutscene = "";
			if (ruleDeploy != null)
			{
				if (abort)
				{
					cutscene = ruleDeploy.AbortCutscene;
				}
				else if (inExitArea == 0)
				{
					cutscene = ruleDeploy.LoseCutscene;
				}
				else
				{
					cutscene = ruleDeploy.WinCutscene;
				}
			}
			if (!string.IsNullOrEmpty(cutscene))
			{
				// if cutscene is "wingame" or "losegame", then the DebriefingState
				// pushed above will get popped without being shown.  otherwise
				// it will get shown after the cutscene.
				_game.PushState(new CutsceneState(cutscene));

				RuleVideo videoRule = _game.Mod.GetVideo(cutscene, true);
				if (videoRule.WinGame)
				{
					_game.SavedGame.Ending = GameEnding.END_WIN;
				}
				else if (videoRule.LoseGame)
				{
					_game.SavedGame.Ending = GameEnding.END_LOSE;
				}
				// Autosave if game is over
				if (_game.SavedGame.Ending != GameEnding.END_NONE && _game.SavedGame.IsIronman)
				{
					_game.PushState(new SaveGameState(OptionsOrigin.OPT_BATTLESCAPE, SaveType.SAVE_IRONMAN, _palette));
				}
			}
		}
	}

	/// <summary>
	/// Shows a tooltip with extra information (used for medikit-type equipment).
	/// </summary>
	public void TxtTooltipInExtra(Action action, bool leftHand, bool special = false)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons() && Options.battleTooltips)
		{
			// no one selected... do normal tooltip
			if (!PlayableUnitSelected())
			{
				_currentTooltip = action.Sender.Tooltip;
				_txtTooltip.SetText(Tr(_currentTooltip));
				return;
			}

			BattleUnit selectedUnit = _save.SelectedUnit;
			BattleItem weapon;
			if (leftHand)
			{
				weapon = selectedUnit.LeftHandWeapon;
			}
			else if (special)
			{
				BattleType type = BT_NONE;
				weapon = selectedUnit.GetSpecialIconWeapon(ref type);
			}
			else
			{
				weapon = selectedUnit.RightHandWeapon;
			}

			// no weapon selected... do normal tooltip
			if (weapon == null)
			{
				_currentTooltip = action.Sender.Tooltip;
				_txtTooltip.SetText(Tr(_currentTooltip));
				return;
			}

			RuleItem weaponRule = weapon.Rules;

			// find the target unit
			if (weaponRule.BattleType == BT_MEDIKIT)
			{
				BattleUnit targetUnit = null;

				// search for target on the ground
				bool onGround = false;
				foreach (BattleUnit bu in _save.Units)
				{
					if (targetUnit != null) break; // loop finished
					// we can heal a unit that is at the same position, unconscious and healable(=woundable)
					if (bu.Position == selectedUnit.Position && bu != selectedUnit && bu.Status == STATUS_UNCONSCIOUS && (bu.IsWoundable || weaponRule.AllowTargetImmune) && weaponRule.AllowTargetGround)
					{
						if (bu.IsBigUnit)
						{
							// never EVER apply anything to 2x2 units on the ground
							continue;
						}
						if ((weaponRule.AllowTargetFriendGround && bu.OriginalFaction == FACTION_PLAYER) ||
							(weaponRule.AllowTargetNeutralGround && bu.OriginalFaction == FACTION_NEUTRAL) ||
							(weaponRule.AllowTargetHostileGround && bu.OriginalFaction == FACTION_HOSTILE))
						{
							targetUnit = bu;
							onGround = true;
						}
					}
				}

				// search for target in front of the selected unit
				if (targetUnit == null && weaponRule.AllowTargetStanding)
				{
					Position dest = default;
					if (_save.TileEngine.ValidMeleeRange(
						selectedUnit.Position,
						selectedUnit.Direction,
						selectedUnit,
						null, ref dest, false))
					{
						Tile tile = _save.GetTile(dest);
						if (tile != null && tile.Unit != null && (tile.Unit.IsWoundable || weaponRule.AllowTargetImmune))
						{
							if ((weaponRule.AllowTargetFriendStanding && tile.Unit.OriginalFaction == FACTION_PLAYER) ||
								(weaponRule.AllowTargetNeutralStanding && tile.Unit.OriginalFaction == FACTION_NEUTRAL) ||
								(weaponRule.AllowTargetHostileStanding && tile.Unit.OriginalFaction == FACTION_HOSTILE))
							{
								targetUnit = tile.Unit;
							}
						}
					}
				}

				_currentTooltip = action.Sender.Tooltip;
				var tooltipExtra = new StringBuilder();
				tooltipExtra.Append((string)Tr(_currentTooltip));

				// target unit found
				if (targetUnit != null)
				{
					if (targetUnit.OriginalFaction == FACTION_HOSTILE)
					{
						_txtTooltip.SetColor(Acom.Palette.BlockOffset(_medikitRed));
						tooltipExtra.Append((string)Tr("STR_TARGET_ENEMY"));
					}
					else if (targetUnit.OriginalFaction == FACTION_NEUTRAL)
					{
						_txtTooltip.SetColor(Acom.Palette.BlockOffset(_medikitOrange));
						tooltipExtra.Append((string)Tr("STR_TARGET_NEUTRAL"));
					}
					else if (targetUnit.OriginalFaction == FACTION_PLAYER)
					{
						_txtTooltip.SetColor(Acom.Palette.BlockOffset(_medikitGreen));
						tooltipExtra.Append((string)Tr("STR_TARGET_FRIEND"));
					}
					if (onGround) tooltipExtra.Append((string)Tr("STR_TARGET_ON_THE_GROUND"));
					_txtTooltip.SetText(tooltipExtra.ToString());
				}
				else
				{
					// target unit not found => selected unit is the target (if self-heal is possible)
					if (weaponRule.AllowTargetSelf)
					{
						_txtTooltip.SetColor(Acom.Palette.BlockOffset(_medikitBlue));
						tooltipExtra.Append((string)Tr("STR_TARGET_YOURSELF"));
						if (onGround) tooltipExtra.Append((string)Tr("STR_TARGET_ON_THE_GROUND"));
						_txtTooltip.SetText(tooltipExtra.ToString());
					}
					else
					{
						// cannot use the weapon (medikit) on anyone
						_currentTooltip = action.Sender.Tooltip;
						_txtTooltip.SetText(Tr(_currentTooltip));
					}
				}
			}
			else
			{
				// weapon is not of medikit battle type
				_currentTooltip = action.Sender.Tooltip;
				_txtTooltip.SetText(Tr(_currentTooltip));
			}
		}
	}

	/// <summary>Shows a tooltip with extra information (used for medikit-type equipment).</summary>
	public void TxtTooltipInExtraLeftHand(Action action)
	{
		TxtTooltipInExtra(action, true);
	}

	/// <summary>Shows a tooltip with extra information (used for medikit-type equipment).</summary>
	public void TxtTooltipInExtraRightHand(Action action)
	{
		TxtTooltipInExtra(action, false);
	}

	/// <summary>Shows a tooltip with extra information (used for medikit-type equipment).</summary>
	public void TxtTooltipInExtraSpecial(Action action)
	{
		TxtTooltipInExtra(action, false, true);
	}

	/// <summary>Shows a tooltip for the End Turn button.</summary>
	public void TxtTooltipInEndTurn(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons() && Options.battleTooltips)
		{
			_currentTooltip = action.Sender.Tooltip;

			var ss = new StringBuilder();
			ss.Append((string)Tr(_currentTooltip));
			ss.Append(' ');
			ss.Append(_save.Turn.ToString(System.Globalization.CultureInfo.InvariantCulture));
			if (_save.TurnLimit > 0)
			{
				ss.Append('/').Append(_save.TurnLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}

			_txtTooltip.SetText(ss.ToString());
		}
	}

	/// <summary>Shows a tooltip for the appropriate button.</summary>
	public void TxtTooltipIn(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (AllowButtons() && Options.battleTooltips)
		{
			_currentTooltip = action.Sender.Tooltip;
			_txtTooltip.SetText(Tr(_currentTooltip));
		}
	}

	/// <summary>Clears the tooltip text.</summary>
	public void TxtTooltipOut(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		// reset color
		_txtTooltip.SetColor(_tooltipDefaultColor);

		if (AllowButtons() && Options.battleTooltips)
		{
			if (_currentTooltip == action.Sender.Tooltip)
			{
				_txtTooltip.SetText("");
			}
		}
	}

	/// <summary>
	/// Updates the scale.
	/// </summary>
	/// <param name="dX">delta of X;</param>
	/// <param name="dY">delta of Y;</param>
	public override void Resize(ref int dX, ref int dY)
	{
		dX = Options.baseXResolution;
		dY = Options.baseYResolution;
		int divisor = 1;
		double pixelRatioY = 1.0;

		if (Options.nonSquarePixelRatio)
		{
			pixelRatioY = 1.2;
		}
		switch ((ScaleType)Options.battlescapeScale)
		{
			case ScaleType.SCALE_SCREEN_DIV_10:
				divisor = 10;
				break;
			case ScaleType.SCALE_SCREEN_DIV_8:
				divisor = 8;
				break;
			case ScaleType.SCALE_SCREEN_DIV_6:
				divisor = 6;
				break;
			case ScaleType.SCALE_SCREEN_DIV_5:
				divisor = 5;
				break;
			case ScaleType.SCALE_SCREEN_DIV_4:
				divisor = 4;
				break;
			case ScaleType.SCALE_SCREEN_DIV_3:
				divisor = 3;
				break;
			case ScaleType.SCALE_SCREEN_DIV_2:
				divisor = 2;
				break;
			case ScaleType.SCALE_SCREEN:
				break;
			default:
				dX = 0;
				dY = 0;
				return;
		}

		Options.baseXResolution = Math.Max(Screen.ORIGINAL_WIDTH, Options.displayWidth / divisor);
		Options.baseYResolution = Math.Max(Screen.ORIGINAL_HEIGHT, (int)(Options.displayHeight / pixelRatioY / divisor));

		dX = Options.baseXResolution - dX;
		dY = Options.baseYResolution - dY;
		_map.SetWidth(Options.baseXResolution);
		_map.SetHeight(Options.baseYResolution);
		_map.Camera.Resize();
		_map.Camera.JumpXY(dX / 2, dY / 2);

		foreach (Surface surf in _surfaces)
		{
			if (surf == _btnCtrl || surf == _btnAlt || surf == _btnShift || surf == _btnRMB || surf == _btnMMB)
			{
				continue;
			}
			if (surf != _map && surf != _btnPsi && surf != _btnLaunch && surf != _btnSpecial && surf != _btnSkills && surf != _txtDebug)
			{
				surf.X = surf.X + dX / 2;
				surf.Y = surf.Y + dY;
			}
			else if (surf != _map && surf != _txtDebug)
			{
				surf.X = surf.X + dX;
			}
		}

		for (int i = 0; i < _posSpecialActions.Length; ++i)
		{
			_posSpecialActions[i] += dX;
		}
	}

	/// <summary>
	/// Move the mouse back to where it started after we finish drag scrolling.
	/// </summary>
	public void StopScrolling(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (Options.battleDragScrollInvert)
		{
			SDLMouse.SDL_WarpMouse((ushort)_xBeforeMouseScrolling, (ushort)_yBeforeMouseScrolling);
			action.SetMouseAction(_xBeforeMouseScrolling, _yBeforeMouseScrolling, _map.X, _map.Y);
			_battleGame.SetupCursor();
			if (_battleGame.CurrentAction.actor == null && (_save.Side == FACTION_PLAYER || _save.DebugMode))
			{
				Map.SetCursorType(CursorType.CT_NORMAL);
			}
		}
		else
		{
			SDLMouse.SDL_WarpMouse((ushort)_cursorPosition.x, (ushort)_cursorPosition.y);
			action.SetMouseAction(_cursorPosition.x, _cursorPosition.y, _map.X, _map.Y);
			_map.SetSelectorPosition((int)action.AbsoluteXMouse, (int)action.AbsoluteYMouse);
		}
		// reset our "mouse position stored" flag
		_cursorPosition.z = 0;
	}
}
