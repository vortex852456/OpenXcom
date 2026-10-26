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
// Ported from: src/Geoscape/BaseDefenseState.h, src/Geoscape/BaseDefenseState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

public enum BaseDefenseActionType : uint
{
	BDA_NONE = 0,
	BDA_FIRE = 1,
	BDA_RESOLVE = 2,
	BDA_DESTROY = 3,
	BDA_END = 4,
}

/// <summary>
/// Base Defense Screen for when ufos try to attack.
/// </summary>
public class BaseDefenseState : State
{
	private readonly TextButton _btnOk;
	private readonly TextButton _btnStart, _btnAbort;
	private readonly Window _window;
	private readonly InteractiveSurface _preview;
	private readonly Text _txtTitle, _txtInit;
	private readonly TextList _lstDefenses;
	private readonly Base _base;
	private readonly Ufo _ufo;
	private int _thinkcycles, _row, _passes, _gravShields, _defenses, _attacks, _explosionCount;
	private BaseDefenseActionType _action;
	private readonly Timer _timer;
	private readonly GeoscapeState _state;

	/// <summary>
	/// Initializes all the elements in the Base Defense screen.
	/// </summary>
	/// <param name="base">Pointer to the base being attacked.</param>
	/// <param name="ufo">Pointer to the attacking ufo.</param>
	/// <param name="state">Pointer to the Geoscape.</param>
	/// <param name="instaHyper">Show extra UFO info (hyperwave decoders in the base).</param>
	public BaseDefenseState(Base @base, Ufo ufo, GeoscapeState state, bool instaHyper)
	{
		_state = state;
		bool showUfo = _game.Mod.ShowUfoPreviewInBaseDefense();

		_base = @base;
		_action = BaseDefenseActionType.BDA_NONE;
		_row = -1;
		_passes = 0;
		_attacks = 0;
		_thinkcycles = 0;
		_ufo = ufo;

		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_txtTitle = new Text(300, 17, 16, 6);
		_txtInit = new Text(300, 10, 16, 24);
		_lstDefenses = new TextList(300, 128, 16, 40);
		_btnOk = new TextButton(120, 18, 100, 170);
		_btnStart = new TextButton(showUfo ? 148 - 13 : 148, 16, 8, 176);
		_btnAbort = new TextButton(showUfo ? 148 + 13 : 148, 16, showUfo ? 164 - 13 : 164, 176);
		_preview = new InteractiveSurface(160, 96, 151, 75);

		// Set palette
		string baseDefenseInterface = showUfo ? "baseDefense_geo" : "baseDefense";
		SetInterface(baseDefenseInterface);

		Add(_window, "window", baseDefenseInterface);
		Add(_btnOk, "button", baseDefenseInterface);
		Add(_btnStart, "button", baseDefenseInterface);
		Add(_btnAbort, "button", baseDefenseInterface);
		Add(_txtTitle, "text", baseDefenseInterface);
		Add(_txtInit, "text", baseDefenseInterface);
		Add(_lstDefenses, "text", baseDefenseInterface);
		Add(_preview);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, baseDefenseInterface);

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.Visible = false;

		_btnStart.Text = Tr("STR_START_FIRING");
		_btnStart.OnMouseClick(BtnStartClick);

		_btnAbort.Text = Tr("STR_SKIP_FIRING");
		_btnAbort.OnMouseClick(BtnOkClick);

		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_BASE_UNDER_ATTACK").Arg(_base.GetName(null)));
		_txtInit.Visible = false;

		_txtInit.SetText(Tr("STR_BASE_DEFENSES_INITIATED"));

		_lstDefenses.SetColumns(3, 134, 70, 50);
		_lstDefenses.SetFlooding(true);

		if (showUfo)
		{
			RuleInterface dogfightInterface = _game.Mod.GetInterface("dogfight");

			SurfaceCrop crop = _game.Mod.GetSurface("INTERWIN.DAT").GetCrop();
			crop.X = 0;
			crop.Y = 0;
			crop.Crop.x = 0;
			crop.Crop.y = 0;
			crop.Crop.w = (ushort)_preview.Width;
			crop.Crop.h = (ushort)_preview.Height;
			//_preview->drawRect(crop.getCrop(), 15);
			//crop.blit(_preview);

			_preview.DrawRect(ref crop.Crop, 15);
			crop.Crop.y = (short)dogfightInterface.GetElement("previewTop").y;
			crop.Crop.h = (ushort)dogfightInterface.GetElement("previewTop").h;
			crop.Blit(_preview);

			crop.Y = _preview.Height - dogfightInterface.GetElement("previewBot").h;
			crop.Crop.y = (short)dogfightInterface.GetElement("previewBot").y;
			crop.Crop.h = (ushort)dogfightInterface.GetElement("previewBot").h;
			crop.Blit(_preview);

			if (string.IsNullOrEmpty(ufo.Rules.ModSprite))
			{
				crop.Crop.y = (short)(dogfightInterface.GetElement("previewMid").y + dogfightInterface.GetElement("previewMid").h * _ufo.Rules.Sprite);
				crop.Crop.h = (ushort)dogfightInterface.GetElement("previewMid").h;
			}
			else
			{
				crop = _game.Mod.GetSurface(ufo.Rules.ModSprite).GetCrop();
			}
			crop.X = dogfightInterface.GetElement("previewTop").x;
			crop.Y = dogfightInterface.GetElement("previewTop").h;
			crop.Blit(_preview);

			// extra info
			bool extraInfo = ufo.HyperDetected;
			if (!extraInfo && instaHyper)
			{
				foreach (var fac in _base.Facilities)
				{
					if (fac.BuildTime == 0 && fac.Rules.IsHyperwave)
					{
						extraInfo = true;
						break;
					}
				}
			}
			if (extraInfo)
			{
				string ss = Unicode.TOK_COLOR_FLIP + (string)Tr(_ufo.Rules.Type);
				_lstDefenses.AddRow(3, Tr("STR_CRAFT_TYPE"), ss, "");
				ss = Unicode.TOK_COLOR_FLIP + (string)Tr(_ufo.AlienRace);
				_lstDefenses.AddRow(3, Tr("STR_RACE"), ss, "");
				ss = Unicode.TOK_COLOR_FLIP + (string)Tr(_ufo.MissionType);
				_lstDefenses.AddRow(3, Tr("STR_MISSION"), ss, "");
			}
		}
		else
		{
			_preview.Visible = false;
		}

		_gravShields = _base.GravShields;
		_defenses = _base.Defenses.Count;
		_timer = new Timer(250);
		_timer.OnTimer((StateHandler)NextStep);

		_explosionCount = 0;

		if (_ufo.Rules.MissilePower != 0)
		{
			if (showUfo)
			{
				_btnAbort.Visible = false;
			}
			else
			{
				BtnStartClick(null);
			}
		}
	}

	/// <summary>
	/// Runs the timer (C++ does not call State::think() here).
	/// </summary>
	public override void Think()
	{
		_timer.Think(this, null);
	}

	/// <summary>
	/// Applies the damage of a base defense facility to the UFO.
	/// </summary>
	/// <returns>True if the UFO shield went down.</returns>
	private bool ApplyDamage(RuleBaseFacility rule)
	{
		bool shieldDown = false;
		int power = rule.DefenseValue;

		if (rule.UnifiedDamageFormula() && rule.AmmoItem != null)
		{
			// unified damage formula
			int damage = rule.AmmoItem.DamageType.GetRandomDamage(power);

			if (_ufo.Shield != 0)
			{
				int shieldDamage = damage * rule.ShieldDamageModifier / 100;
				if (rule.ShieldDamageModifier == 0)
				{
					damage = 0;
				}
				else
				{
					// scale down by bleed-through factor and scale up by shield-effectiveness factor
					damage = Math.Max(0, shieldDamage - _ufo.Shield) * _ufo.CraftStats.shieldBleedThrough / rule.ShieldDamageModifier;
				}
				_ufo.Shield = _ufo.Shield - shieldDamage;
				if (_ufo.Shield == 0)
				{
					shieldDown = true;
				}
			}
			damage = Math.Max(0, damage - _ufo.CraftStats.armor);
			_ufo.SetDamage(_ufo.Damage + damage, _game.Mod);
		}
		else
		{
			// vanilla dmg formula: 50-150%
			int dmg = power / 2 + Rng.Generate(0, power);

			if (_ufo.Shield > 0)
			{
				int shieldDmg = dmg * rule.ShieldDamageModifier / 100;
				if (rule.ShieldDamageModifier == 0)
				{
					dmg = 0;
				}
				else
				{
					// scale down by bleed-through factor NOT performed for backwards-compatibility
					// only scale up by shield-effectiveness factor
					dmg = Math.Max(0, shieldDmg - _ufo.Shield) * 100 / rule.ShieldDamageModifier;
				}
				_ufo.Shield = _ufo.Shield - shieldDmg;
				if (_ufo.Shield == 0)
				{
					shieldDown = true;
				}
			}
			_ufo.SetDamage(_ufo.Damage + dmg, _game.Mod);
		}

		return shieldDown;
	}

	/// <summary>
	/// Advances the base defense sequence by one step.
	/// </summary>
	public void NextStep()
	{
		if (_thinkcycles == -1)
			return;

		++_thinkcycles;

		if (_thinkcycles == 1)
		{
			_txtInit.Visible = true;
			return;
		}

		if (_thinkcycles > 1)
		{
			switch (_action)
			{
				case BaseDefenseActionType.BDA_DESTROY:
					if (_explosionCount == 0)
					{
						_lstDefenses.AddRow(2, Tr("STR_UFO_DESTROYED"), " ", " ");
						++_row;
						if (_row > 14)
						{
							_lstDefenses.ScrollDown(true);
						}
					}
					_game.Mod.GetSound("GEO.CAT", Mod.UFO_EXPLODE).Play();
					if (++_explosionCount == 3)
					{
						_action = BaseDefenseActionType.BDA_END;
					}
					return;
				case BaseDefenseActionType.BDA_END:
					_btnOk.Visible = true;
					_thinkcycles = -1;
					return;
				default:
					break;
			}
			if (_attacks == _defenses && _passes == _gravShields)
			{
				_action = BaseDefenseActionType.BDA_END;
				return;
			}
			else if (_attacks == _defenses && _passes < _gravShields)
			{
				_lstDefenses.AddRow(3, Tr("STR_GRAV_SHIELD_REPELS_UFO"), " ", " ");
				if (_row > 14)
				{
					_lstDefenses.ScrollDown(true);
				}
				++_row;
				++_passes;
				_attacks = 0;
				return;
			}

			BaseFacility def = _base.Defenses[_attacks];
			RuleItem ammo = def.Rules.AmmoItem;
			int ammoNeeded = def.Rules.AmmoNeeded;
			bool hasOwnAmmo = def.Rules.AmmoMax > 0;
			bool spendAmmo = false;

			int chanceToHit = def.Rules.HitRatio; // vanilla xcom
			chanceToHit -= _ufo.CraftStats.avoidBonus2;

			switch (_action)
			{
				case BaseDefenseActionType.BDA_NONE:
					_lstDefenses.AddRow(3, Tr(def.Rules.Type), " ", " ");
					++_row;
					_action = BaseDefenseActionType.BDA_FIRE;
					if (_row > 14)
					{
						_lstDefenses.ScrollDown(true);
					}
					return;
				case BaseDefenseActionType.BDA_FIRE:
					if (hasOwnAmmo && def.Ammo < ammoNeeded)
					{
						_lstDefenses.SetCellText(_row, 1, Tr("STR_NO_AMMO"));
					}
					else if (!hasOwnAmmo && ammo != null && _base.StorageItems.GetItem(ammo) < ammoNeeded)
					{
						_lstDefenses.SetCellText(_row, 1, Tr("STR_NO_AMMO"));
					}
					else
					{
						_lstDefenses.SetCellText(_row, 1, Tr("STR_FIRING"));
						_game.Mod.GetSound("GEO.CAT", def.Rules.FireSound).Play();
					}
					_timer.SetInterval(333);
					_action = BaseDefenseActionType.BDA_RESOLVE;
					return;
				case BaseDefenseActionType.BDA_RESOLVE:
					if (hasOwnAmmo && def.Ammo < ammoNeeded)
					{
						//_lstDefenses->setCellText(_row, 2, tr("STR_NO_AMMO"));
					}
					else if (!hasOwnAmmo && ammo != null && _base.StorageItems.GetItem(ammo) < ammoNeeded)
					{
						//_lstDefenses->setCellText(_row, 2, tr("STR_NO_AMMO"));
					}
					else if (!Rng.Percent(chanceToHit))
					{
						spendAmmo = true;
						_lstDefenses.SetCellText(_row, 2, Tr("STR_MISSED"));
					}
					else
					{
						spendAmmo = true;
						_lstDefenses.SetCellText(_row, 2, Tr("STR_HIT"));
						_game.Mod.GetSound("GEO.CAT", def.Rules.HitSound).Play();
						bool shieldDown = ApplyDamage(def.Rules);
						if (shieldDown)
						{
							_lstDefenses.AddRow(3, Tr("STR_UFO_SHIELD_DOWN"), " ", " ");
							++_row;
							if (_row > 14)
							{
								_lstDefenses.ScrollDown(true);
							}
						}
					}
					if (spendAmmo && ammoNeeded > 0)
					{
						if (hasOwnAmmo)
						{
							def.Ammo = def.Ammo - ammoNeeded;
							def.ResetAmmoMissingReported();
						}
						else if (!hasOwnAmmo && ammo != null)
						{
							_base.StorageItems.RemoveItem(ammo, ammoNeeded);
						}
					}
					if (_ufo.Status == Ufo.UfoStatus.DESTROYED)
						_action = BaseDefenseActionType.BDA_DESTROY;
					else
						_action = BaseDefenseActionType.BDA_NONE;
					++_attacks;
					_timer.SetInterval(250);
					return;
				default:
					break;
			}
		}
	}

	/// <summary>
	/// Starts base defense.
	/// </summary>
	public void BtnStartClick(Action action)
	{
		_preview.Visible = false;
		_lstDefenses.ClearList();
		_btnStart.Visible = false;
		_btnAbort.Visible = false;
		_timer.Start();
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_timer.Stop();
		_game.PopState();
		if (_ufo.Status != Ufo.UfoStatus.DESTROYED)
		{
			_state.HandleBaseDefense(_base, _ufo);
		}
		else
		{
			_base.CleanupDefenses(true);

			// instant retaliation mission only spawns one UFO and then ends
			if (_ufo.Mission.Rules.Objective == MissionObjective.OBJECTIVE_INSTANT_RETALIATION)
			{
				_ufo.Mission.SetInterrupted(true);
			}

			// aliens are not stupid and should stop trying eventually
			if (_ufo.Mission.Rules.Objective == MissionObjective.OBJECTIVE_RETALIATION && Rng.Percent(_game.Mod.ChanceToStopRetaliation))
			{
				// unmark base...
				_base.SetRetaliationTarget(false);

				AlienMission am = _base.RetaliationMission;
				if (am == null)
				{
					// backwards-compatibility
					RuleRegion regionRule = _game.SavedGame.Regions[0].Rules; // wrong, but that's how it is in OXC
					foreach (var region in _game.SavedGame.Regions)
					{
						if (region.Rules.InsideRegion(_base.Longitude, _base.Latitude))
						{
							regionRule = region.Rules;
							break;
						}
					}
					am = _game.SavedGame.FindAlienMission(regionRule.Type, MissionObjective.OBJECTIVE_RETALIATION);
				}

				if (am != null && am.Rules.IsMultiUfoRetaliation)
				{
					// Remember that more UFOs may be coming
					am.SetMultiUfoRetaliationInProgress(true);
				}
				else
				{
					// Delete the mission and any live UFOs
					_game.SavedGame.DeleteRetaliationMission(am, _base);
				}
			}
		}
	}
}
