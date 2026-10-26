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
// Ported from: src/Basescape/CraftInfoState.h, src/Basescape/CraftInfoState.cpp
#nullable disable

using static Acom.SDLKey;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Craft Info screen that shows all the
/// info of a specific craft.
/// </summary>
public class CraftInfoState : State
{
	private readonly Base _base;
	private readonly int _craftId;
	private readonly Craft _craft;
	private readonly int _weaponNum;

	private readonly TextButton _btnOk, _btnCrew, _btnEquip, _btnArmor, _btnPilots, _btnNewBattle;
	private readonly TextButton[] _btnW = new TextButton[RuleCraft.WeaponMax];
	private readonly Window _window;
	private readonly TextEdit _edtCraft;
	private readonly Text _txtDamage, _txtShield, _txtFuel, _txtSkin;
	private readonly Text[] _txtWName = new Text[RuleCraft.WeaponMax], _txtWAmmo = new Text[RuleCraft.WeaponMax];
	private readonly InteractiveSurface _sprite;
	private readonly InteractiveSurface[] _weapon = new InteractiveSurface[RuleCraft.WeaponMax];
	private readonly Surface _crew, _equip;

	/// <summary>
	/// Initializes all the elements in the Craft Info screen.
	/// </summary>
	/// <param name="base">Pointer to the base to get info from.</param>
	/// <param name="craftId">ID of the selected craft.</param>
	public CraftInfoState(Base @base, int craftId)
	{
		_base = @base;
		_craftId = craftId;
		_craft = null;

		// Create objects
		if (_game.SavedGame.MonthsPassed != -1)
		{
			_window = new Window(this, 320, 200, 0, 0, WindowPopup.POPUP_BOTH);
		}
		else
		{
			_window = new Window(this, 320, 200, 0, 0, WindowPopup.POPUP_NONE);
		}

		_craft = _base.Crafts[_craftId];
		_weaponNum = _craft.Rules.Weapons;
		if (_weaponNum > RuleCraft.WeaponMax)
			_weaponNum = RuleCraft.WeaponMax;

		int showNewBattle = 0;
		if (_game.SavedGame.DebugMode && _game.SavedGame.MonthsPassed != -1)
		{
			// only the first craft can be used
			if (_craftId == 0 && _craft.Rules.IsForNewBattle)
			{
				showNewBattle = 1;
			}
		}

		const int top_row = 41;
		const int bottom = 125;
		const int bottom_row = 17;
		int top = _weaponNum > 2 ? 42 : 64;
		bool pilots = _craft.Rules.Pilots > 0;
		_btnOk = new TextButton((pilots ? 218 : 288) - showNewBattle * 98, 16, pilots ? 86 : 16, 176);
		_btnNewBattle = new TextButton(92, 16, 212, 176);
		for (int i = 0; i < _weaponNum; ++i)
		{
			int x = i % 2 != 0 ? 282 : 14;
			int y = top + (i / 2) * top_row;
			_btnW[i] = new TextButton(24, 32, x, y);
		}
		_btnCrew = new TextButton(64, 16, 16, bottom);
		_btnEquip = new TextButton(64, 16, 16, bottom + bottom_row);
		_btnArmor = new TextButton(64, 16, 16, bottom + 2 * bottom_row);
		_btnPilots = new TextButton(64, 16, 16, bottom + 3 * bottom_row);
		_edtCraft = new TextEdit(this, 140, 16, 80, 8);
		_txtDamage = new Text(100, 17, 14, 24);
		_txtShield = new Text(100, 17, 120, 24);
		_txtFuel = new Text(82, 17, 228, 24);
		_txtSkin = new Text(32, 9, 144, 46);
		for (int i = 0; i < _weaponNum; ++i)
		{
			int x = i % 2 != 0 ? 204 : 46;
			int y = top + (i / 2) * top_row;
			int d = i % 2 != 0 ? 20 : 0;
			_txtWName[i] = new Text(95, 16, x - d, y);
			_txtWAmmo[i] = new Text(75, 24, x, y + 16);
		}
		_sprite = new InteractiveSurface(32, 40, 144, 56);
		for (int i = 0; i < _weaponNum; ++i)
		{
			int x = i % 2 != 0 ? 184 : 121;
			int y = top + 16 + (i / 2) * top_row;
			_weapon[i] = new InteractiveSurface(15, 17, x, y);
		}
		_crew = new Surface(220, 18, 85, bottom - 1);
		_equip = new Surface(220, 18, 85, bottom + bottom_row);

		// Set palette
		SetInterface("craftInfo");

		Add(_window, "window", "craftInfo");
		Add(_btnOk, "button", "craftInfo");
		Add(_btnNewBattle, "button", "craftInfo");
		for (int i = 0; i < _weaponNum; ++i)
		{
			Add(_btnW[i], "button", "craftInfo");
		}
		Add(_btnCrew, "button", "craftInfo");
		Add(_btnEquip, "button", "craftInfo");
		Add(_btnArmor, "button", "craftInfo");
		Add(_btnPilots, "button", "craftInfo");
		Add(_edtCraft, "text1", "craftInfo");
		Add(_txtDamage, "text1", "craftInfo");
		Add(_txtShield, "text1", "craftInfo");
		Add(_txtFuel, "text1", "craftInfo");
		Add(_txtSkin, "text1", "craftInfo");
		for (int i = 0; i < _weaponNum; ++i)
		{
			Add(_txtWName[i], "text2", "craftInfo");
			Add(_txtWAmmo[i], "text3", "craftInfo");
			Add(_weapon[i]);
		}
		Add(_sprite);
		Add(_crew);
		Add(_equip);

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "craftInfo");

		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);
		_btnOk.OnKeyboardPress(BtnUfopediaClick, Options.keyGeoUfopedia);

		_btnNewBattle.Text = Tr("STR_NEW_BATTLE");
		_btnNewBattle.OnMouseClick(BtnNewBattleClick);
		_btnNewBattle.Visible = showNewBattle > 0;

		for (int i = 0; i < _weaponNum; ++i)
		{
			_btnW[i].Text = ((char)('1' + i)).ToString();
			_btnW[i].OnMouseClick(BtnWClick);
			_weapon[i].OnMouseClick(BtnWIconClick);
			_weapon[i].OnMouseClick(BtnWIconMiddleClick, SDLMouse.SDL_BUTTON_MIDDLE);
		}

		_sprite.OnMouseClick(BtnCraftIconClick);

		_btnCrew.Text = Tr("STR_CREW");
		_btnCrew.OnMouseClick(BtnCrewClick);

		_btnEquip.Text = Tr("STR_EQUIPMENT_UC");
		_btnEquip.OnMouseClick(BtnEquipClick);

		_btnArmor.Text = Tr("STR_ARMOR");
		_btnArmor.OnMouseClick(BtnArmorClick);

		_btnPilots.Text = Tr("STR_PILOTS");
		_btnPilots.OnMouseClick(BtnPilotsClick);
		_btnPilots.Visible = pilots;

		_edtCraft.SetBig();
		_edtCraft.SetAlign(TextHAlign.ALIGN_CENTER);
		_edtCraft.OnChange(EdtCraftChange);

		_txtSkin.Align = TextHAlign.ALIGN_CENTER;
		if (_craft.Rules.MaxSkinIndex > 0)
		{
			_txtSkin.SetText(Tr("STR_CRAFT_SKIN_ID").Arg(_craft.SkinIndex));
		}

		for (int i = 0; i < _weaponNum; ++i)
		{
			_txtWName[i].SetWordWrap(true);
		}
	}

	/// <summary>Descending lexicographic comparer for sprite index triples (C++ <c>std::greater&lt;std::array&lt;int, 3&gt;&gt;</c>).</summary>
	private sealed class GreaterIndexes : IComparer<(int, int, int)>
	{
		public int Compare((int, int, int) a, (int, int, int) b) => b.CompareTo(a);
	}

	/// <summary>Items grouped by their custom preview sprites.</summary>
	private sealed class SpriteGroup
	{
		public readonly Surface[] surfaces = new Surface[3];
		public long count;
	}

	/// <summary>
	/// The craft info can change
	/// after going into other screens.
	/// </summary>
	public override void Init()
	{
		base.Init();

		_edtCraft.Text = _craft.GetName(_game.Language);

		_sprite.Clear();
		SurfaceSet texture = _game.Mod.GetSurfaceSet("BASEBITS.PCK");
		texture.GetFrame(_craft.SkinSprite + 33).BlitNShade(_sprite, 0, 0);

		string firlsLine = Tr("STR_DAMAGE_UC_").Arg(Unicode.FormatPercentage(_craft.DamagePercentage));
		if (_craft.Status == "STR_REPAIRS" && _craft.Damage > 0)
		{
			int damageHours = (int)Math.Ceiling((double)_craft.Damage / _craft.Rules.RepairRate);
			firlsLine += FormatTime(damageHours);
		}
		_txtDamage.SetText(firlsLine);

		string secondLine = Tr("STR_FUEL").Arg(Unicode.FormatPercentage(_craft.FuelPercentage));
		if (_craft.Status == "STR_REFUELLING" && _craft.FuelMax - _craft.Fuel > 0)
		{
			int fuelHours = (int)Math.Ceiling((double)(_craft.FuelMax - _craft.Fuel) / _craft.Rules.RefuelRate / 2.0);
			secondLine += FormatTime(fuelHours);
		}
		_txtFuel.SetText(secondLine);

		string thirdLine = "";
		if (_craft.ShieldCapacity != 0)
		{
			thirdLine += Tr("STR_SHIELD").Arg(Unicode.FormatPercentage(_craft.ShieldPercentage));
			if (_craft.Shield < _craft.ShieldCapacity)
			{
				if (_craft.Rules.ShieldRechargeAtBase != 0)
				{
					int shieldHours = (int)Math.Ceiling((double)(_craft.ShieldCapacity - _craft.Shield) / _craft.Rules.ShieldRechargeAtBase);
					thirdLine += FormatTime(shieldHours);
				}
			}
		}
		_txtShield.SetText(thirdLine);

		if (_craft.Rules.MaxUnitsLimit > 0)
		{
			_crew.Clear();
			_equip.Clear();

			Surface frame1 = texture.GetFrame(38);

			SurfaceSet customArmorPreviews = _game.Mod.GetSurfaceSet("CustomArmorPreviews");
			int x = 0;
			foreach (var soldier in _base.Soldiers)
			{
				if (soldier.Craft == _craft)
				{
					foreach (int index in soldier.Armor.CustomArmorPreviewIndex)
					{
						Surface customFrame1 = customArmorPreviews.GetFrame(index);
						if (customFrame1 != null)
						{
							// modded armor previews
							customFrame1.BlitNShade(_crew, x, 0);
						}
						else
						{
							// vanilla
							frame1.BlitNShade(_crew, x, 0);
						}
						x += 10;
					}
				}
			}

			Surface frame2 = texture.GetFrame(40);

			SurfaceSet customItemPreviews = _game.Mod.GetSurfaceSet("CustomItemPreviews");
			x = 0;
			foreach (var vehicle in _craft.Vehicles)
			{
				foreach (int index in vehicle.Rules.CustomItemPreviewIndex)
				{
					Surface customFrame2 = customItemPreviews.GetFrame(index);
					if (customFrame2 != null)
					{
						// modded HWP/auxiliary previews
						customFrame2.BlitNShade(_equip, x, 0);
					}
					else
					{
						// vanilla
						frame2.BlitNShade(_equip, x, 0);
					}
					x += 10;
				}
			}

			Surface frame3 = texture.GetFrame(39);

			var itemsBySprite = new SortedDictionary<(int, int, int), SpriteGroup>(new GreaterIndexes());

			foreach (var item in _craft.Items.Contents)
			{
				// fill default values, then load values from config (clipped to 3 entries)
				int[] ind = [-1, -1, -1];
				var previews = item.Key.CustomItemPreviewIndex;
				for (int k = 0; k < ind.Length && k < previews.Count; ++k)
				{
					ind[k] = previews[k];
				}
				var key = (ind[0], ind[1], ind[2]);

				if (!itemsBySprite.TryGetValue(key, out var pos))
				{
					pos = new SpriteGroup();
					itemsBySprite[key] = pos;
				}

				// update surfaces if not set yet
				for (int k = 0; k < ind.Length; ++k)
				{
					if (pos.surfaces[k] != null || ind[k] < 0)
					{
						break;
					}

					pos.surfaces[k] = customItemPreviews.GetFrame(ind[k]);
				}

				pos.count += item.Value;
			}

			foreach (var pair in itemsBySprite)
			{
				var pos = pair.Value;
				if (pos.surfaces[0] != null)
				{
					// new logic for items grouped by sprite
					long i = 4, next = 8;

					// draw icons for next "fibonacci" item count
					do
					{
						foreach (var s in pos.surfaces)
						{
							if (s != null)
							{
								s.BlitNShade(_equip, x, 0);
								x += 10;
								long old = next;
								next = next + i;
								i = old; // calling this here make multi part sprites occupy similar size to single part ones
							}
							else
							{
								break;
							}
						}
					}
					while (i <= pos.count);
				}
				else
				{
					// classic behavior
					for (long i = 0; i < pos.count; i += 4)
					{
						frame3.BlitNShade(_equip, x, 0);
						x += 10;
					}
				}
			}
		}
		else
		{
			_crew.Visible = false;
			_equip.Visible = false;
			_btnCrew.Visible = false;
			_btnEquip.Visible = false;
			_btnArmor.Visible = false;
			_btnPilots.Visible = false;
		}

		for (int i = 0; i < _weaponNum; ++i)
		{
			CraftWeapon w1 = _craft.Weapons[i];

			_weapon[i].Clear();
			if (w1 != null)
			{
				Surface frame = texture.GetFrame(w1.Rules.Sprite + 48);
				frame.BlitNShade(_weapon[i], 0, 0);

				string weaponLine = "";
				if (w1.IsDisabled) weaponLine += "*";
				weaponLine += Unicode.TOK_COLOR_FLIP + Tr(w1.Rules.Type);
				_txtWName[i].SetText(weaponLine);
				weaponLine = "";
				if (w1.Rules.Tooltip.Length != 0)
				{
					weaponLine += Tr(w1.Rules.Tooltip)
						.Arg(w1.Ammo)                                // {0}
						.Arg(w1.Rules.AmmoMax)                       // {1}
						.Arg(w1.Rules.BonusStats.fuelMax)            // {2}
						.Arg(w1.Rules.BonusStats.speedMax)           // {3}
						.Arg(w1.Rules.BonusStats.damageMax)          // {4}
						.Arg(w1.Rules.BonusStats.armor)              // {5}
						.Arg(w1.Rules.BonusStats.shieldCapacity);    // {6}
				}
				else if (w1.Rules.AmmoMax != 0)
				{
					weaponLine += Tr("STR_AMMO_").Arg(w1.Ammo) + "\n" + Unicode.TOK_COLOR_FLIP;
					weaponLine += Tr("STR_MAX").Arg(w1.Rules.AmmoMax);
					if (_craft.Status == "STR_REARMING" && w1.Ammo < w1.Rules.AmmoMax && !w1.IsDisabled)
					{
						int rearmHours = (int)Math.Ceiling((double)(w1.Rules.AmmoMax - w1.Ammo) / w1.Rules.RearmRate);
						weaponLine += FormatTime(rearmHours);
					}
				}
				_txtWAmmo[i].SetText(weaponLine);
				if (!string.IsNullOrEmpty(_craft.Rules.GetFixedWeaponInSlot(i)))
				{
					_btnW[i].Visible = false; // cannot remove or change a fixed weapon
				}
			}
			else
			{
				_txtWName[i].SetText("");
				_txtWAmmo[i].SetText("");
			}
		}
	}

	/// <summary>
	/// Turns an amount of time into a
	/// day/hour string.
	/// </summary>
	/// <param name="total">Amount in hours.</param>
	private string FormatTime(int total)
	{
		int days = total / 24;
		int hours = total % 24;
		string ss = "\n(";
		if (days > 0)
		{
			ss += Tr("STR_DAY", days) + "/";
		}
		if (hours > 0)
		{
			ss += Tr("STR_HOUR", hours);
		}
		ss += ")";
		return ss;
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Opens the corresponding Ufopaedia craft article.
	/// </summary>
	public void BtnUfopediaClick(Action action)
	{
		if (_craft != null)
		{
			string articleId = _craft.Rules.Type;
			Ufopaedia.OpenArticle(_game, articleId);
		}
	}

	/// <summary>
	/// Updates the New Battle file (battle.cfg) and returns to the previous screen.
	/// </summary>
	public void BtnNewBattleClick(Action action)
	{
		_game.PopState();

		// C++ keeps these as size_t; the values are small indexes
		int mission = 0;
		int craft = 0;
		int darkness = 0;
		int terrain = 0;
		int alienRace = 0;
		int difficulty = 0;
		int alienTech = 0;

		string s = Options.GetMasterUserFolder() + "battle.cfg";
		if (!CrossPlatform.FileExists(s))
		{
			// nothing
		}
		else
		{
			try
			{
				var reader = YamlRootNodeReader.FromFile(s);
				mission = reader["mission"].ReadVal(0);
				//craft = reader["craft"].ReadVal(0);
				darkness = reader["darkness"].ReadVal(0);
				terrain = reader["terrain"].ReadVal(0);
				alienRace = reader["alienRace"].ReadVal(0);
				//difficulty = reader["difficulty"].ReadVal(0);
				alienTech = reader["alienTech"].ReadVal(0);
			}
			catch (YamlReadException e)
			{
				Logger.Warning(e.Message);
			}
		}

		// index of the craft type in the New Battle combobox
		int idx = 0;
		foreach (var craftType in _game.Mod.CraftsList)
		{
			RuleCraft rule = _game.Mod.GetCraft(craftType);
			if (rule.IsForNewBattle)
			{
				if (rule == _craft.Rules)
				{
					craft = idx;
					break;
				}
				++idx;
			}
			else
			{
				// don't increase the index, these crafts are not in the New Battle combobox
			}
		}

		// transfer also the difficulty
		difficulty = (int)_game.SavedGame.Difficulty;

		var writer = new YamlRootNodeWriter();
		writer.SetAsMap();
		writer.Write("mission", mission);
		writer.Write("craft", craft);
		writer.Write("darkness", darkness);
		writer.Write("terrain", terrain);
		writer.Write("alienRace", alienRace);
		writer.Write("difficulty", difficulty);
		writer.Write("alienTech", alienTech);
		_base.Save(writer["base"]);
		string yaml = writer.Emit();

		if (!CrossPlatform.WriteFile(s, yaml))
		{
			Logger.Warning($"Failed to save {s}");
		}
	}

	/// <summary>
	/// Goes to the Select Armament window
	/// for the weapons.
	/// </summary>
	public void BtnWClick(Action action)
	{
		for (int i = 0; i < _weaponNum; ++i)
		{
			if (action.Sender == _btnW[i])
			{
				_game.PushState(new CraftWeaponsState(_base, _craftId, i));
				return;
			}
		}
	}

	/// <summary>
	/// Toggles the enabled/disabled status of the weapon.
	/// </summary>
	public void BtnWIconClick(Action action)
	{
		for (int i = 0; i < _weaponNum; ++i)
		{
			if (action.Sender == _weapon[i])
			{
				CraftWeapon w1 = _craft.Weapons[i];
				if (w1 != null)
				{
					// Toggle the weapon status
					w1.SetDisabled(!w1.IsDisabled);

					// If we just enabled the weapon, we should begin rearming immediately.
					if (!w1.IsDisabled)
					{
						_craft.Checkup();
					}

					// Update the onscreen info.
					// Note: This method is overkill, since we only need to update a few things. But at least this ensures we haven't missed anything.
					Init();
				}
			}
		}
	}

	/// <summary>
	/// Opens the corresponding Ufopaedia craft weapon article.
	/// </summary>
	public void BtnWIconMiddleClick(Action action)
	{
		for (int i = 0; i < _weaponNum; ++i)
		{
			if (action.Sender == _weapon[i])
			{
				CraftWeapon w1 = _craft.Weapons[i];
				if (w1 != null)
				{
					RuleCraftWeapon rule = w1.Rules;
					if (rule != null)
					{
						string articleId = rule.UfopediaType;
						Ufopaedia.OpenArticle(_game, articleId);
					}
				}
			}
		}
	}

	/// <summary>
	/// Toggles the craft skin.
	/// </summary>
	public void BtnCraftIconClick(Action action)
	{
		if (_craft.Rules.MaxSkinIndex > 0)
		{
			int newIndex = _craft.SkinIndex + 1;
			if (newIndex > _craft.Rules.MaxSkinIndex)
			{
				newIndex = 0;
			}
			_craft.SkinIndex = newIndex;

			_txtSkin.SetText(Tr("STR_CRAFT_SKIN_ID").Arg(_craft.SkinIndex));

			_sprite.Clear();
			SurfaceSet texture = _game.Mod.GetSurfaceSet("BASEBITS.PCK");
			texture.GetFrame(_craft.SkinSprite + 33).BlitNShade(_sprite, 0, 0);
		}
	}

	/// <summary>
	/// Goes to the Select Squad screen.
	/// </summary>
	public void BtnCrewClick(Action action)
	{
		_game.PushState(new CraftSoldiersState(_base, _craftId));
	}

	/// <summary>
	/// Goes to the Select Equipment screen.
	/// </summary>
	public void BtnEquipClick(Action action)
	{
		_game.PushState(new CraftEquipmentState(_base, _craftId));
	}

	/// <summary>
	/// Goes to the Select Armor screen.
	/// </summary>
	public void BtnArmorClick(Action action)
	{
		_game.PushState(new CraftArmorState(_base, _craftId));
	}

	/// <summary>
	/// Goes to the Pilots Info screen.
	/// </summary>
	public void BtnPilotsClick(Action action)
	{
		_game.PushState(new CraftPilotsState(_base, _craftId));
	}

	/// <summary>
	/// Changes the Craft name.
	/// </summary>
	public void EdtCraftChange(Action action)
	{
		if (_edtCraft.Text == _craft.GetDefaultName(_game.Language))
		{
			_craft.SetName("");
		}
		else
		{
			_craft.SetName(_edtCraft.Text);
		}
		if (action.Details.key.keysym.sym == SDLK_RETURN ||
			action.Details.key.keysym.sym == SDLK_KP_ENTER)
		{
			_edtCraft.Text = _craft.GetName(_game.Language);
		}
	}
}
