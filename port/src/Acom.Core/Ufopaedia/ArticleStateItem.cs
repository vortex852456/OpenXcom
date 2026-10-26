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
// Ported from: src/Ufopaedia/ArticleStateItem.h, src/Ufopaedia/ArticleStateItem.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>
/// ArticleStateItem has a caption, text, preview image and a stats block.
/// The facility image is found using the RuleBasefacility class.
/// </summary>
public class ArticleStateItem : ArticleState
{
	protected Surface _image;
	protected NumberText _txtWeaponClipSize;
	protected Text _txtTitle;
	protected Text _txtWeight;
	protected Text _txtInfo;
	protected Text _txtAccuracyModifier;
	protected Text _txtPowerBonus;
	protected TextList _lstInfo;
	protected Text _txtShotType;
	protected Text _txtAccuracy;
	protected Text _txtTuCost;
	protected Text _txtDamage;
	protected Text _txtAmmo;
	protected Text[] _txtAmmoType = new Text[3];
	protected Text[] _txtAmmoDamage = new Text[3];
	protected Surface[] _imageAmmo = new Surface[3];
	protected NumberText[] _txtAmmoClipSize = new NumberText[3];
	protected Text _txtArrows;
	protected byte _buttonColor;
	protected byte _textColor;
	protected byte _textColor2;
	protected byte _listColor1;
	protected byte _listColor2;
	protected byte _ammoColor;
	protected byte _arrowColor;

	public ArticleStateItem(ArticleDefinitionItem article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		ArticleDefinitionItem defs = article_defs;
		RuleItem item = _game.Mod.GetItem(defs.weapon, false);
		if (item == null)
		{
			item = _game.Mod.GetItem(defs.id, true);
		}

		int bottomOffset = 20;
		string accuracyModifier;
		bool isAccurracyModded = false;
		if (item.BattleType == BattleType.BT_MELEE)
		{
			accuracyModifier = AddRuleStatBonus(item.MeleeMultiplierRaw);
			isAccurracyModded = item.MeleeMultiplierRaw.IsModded;
		}
		else
		{
			accuracyModifier = AddRuleStatBonus(item.AccuracyMultiplierRaw);
			isAccurracyModded = item.AccuracyMultiplierRaw.IsModded;
		}

		string powerBonus = AddRuleStatBonus(item.DamageBonusRaw);

		if (!isAccurracyModded)
		{
			// don't show default accuracy multiplier
			bottomOffset = 9;
		}

		if (_game.Mod.ExtraNerdyPediaInfoType == 0)
		{
			// feature turned off
			bottomOffset = 0;
		}
		else if (item.BattleType == BattleType.BT_AMMO)
		{
			// don't show accuracy multiplier... even if someone mods it by mistake, it still makes no sense
			bottomOffset = 9;
		}
		else if (item.BattleType == BattleType.BT_FIREARM || item.BattleType == BattleType.BT_MELEE)
		{
			if (item.ClipSize != 0 || item.IgnoreAmmoPower)
			{
				// correct info loaded already... weapon has built-in ammo or ignores ammo power bonuses
			}
			else
			{
				// need to check power bonus on all compatible ammo
				bool first = true;
				bool allSame = true;
				for (int slot = 0; slot < RuleItem.AmmoSlotMax; ++slot)
				{
					foreach (var ammoItemRule in item.GetCompatibleAmmoForSlot(slot))
					{
						if (first)
						{
							powerBonus = AddRuleStatBonus(ammoItemRule.DamageBonusRaw);
							first = false;
						}
						else
						{
							string otherPowerBonus = AddRuleStatBonus(ammoItemRule.DamageBonusRaw);
							if (powerBonus != otherPowerBonus)
							{
								allSame = false;
								powerBonus = Tr("STR_MULTIPLE_DIFFERENT_BONUSES");
							}
						}
						if (!allSame) break;
					}
					if (!allSame) break;
				}
			}
		}
		else if (item.BattleType == BattleType.BT_PSIAMP)
		{
			// correct info loaded already
		}
		else
		{
			// nothing to show for other item types
			bottomOffset = 0;
		}

		if (powerBonus.Length == 0)
		{
			if (!isAccurracyModded)
			{
				// both power bonus and accuracy multiplier are vanilla, hide info completely
				bottomOffset = 0;
			}
			else
			{
				// display zero instead of empty string
				powerBonus = "0";
			}
		}

		// add screen elements
		_txtTitle = new Text(148, 32, 5, 24);
		_txtWeight = new Text(88, 8, 104, 55);

		// Set palette
		SetStandardPalette("PAL_BATTLEPEDIA");

		_buttonColor = (byte)_game.Mod.GetInterface("articleItem").GetElement("button").color;
		_arrowColor = _buttonColor;
		if (_game.Mod.GetInterface("articleItem").GetElementOptional("arrow") != null)
		{
			_arrowColor = (byte)_game.Mod.GetInterface("articleItem").GetElement("arrow").color;
		}
		_textColor = (byte)_game.Mod.GetInterface("articleItem").GetElement("text").color;
		_textColor2 = (byte)_game.Mod.GetInterface("articleItem").GetElement("text").color2;
		_listColor1 = (byte)_game.Mod.GetInterface("articleItem").GetElement("list").color;
		_listColor2 = (byte)_game.Mod.GetInterface("articleItem").GetElement("list").color2;
		_ammoColor = (byte)_game.Mod.GetInterface("articleItem").GetElement("ammoColor").color;

		InitLayout();

		// add other elements
		Add(_txtTitle);
		Add(_txtWeight, "weightText", "articleItem", _bg);

		// Set up objects
		_game.Mod.GetSurface("BACK08.SCR").BlitNShade(_bg, 0, 0);
		_btnOk.SetColor(_buttonColor);
		_btnPrev.SetColor(_buttonColor);
		_btnNext.SetColor(_buttonColor);
		_btnInfo.SetColor(_buttonColor);
		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		_txtTitle.SetColor(_textColor);
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		_txtWeight.SetColor(_textColor);
		_txtWeight.Align = TextHAlign.ALIGN_RIGHT;

		// IMAGE
		_image = new Surface(32, 48, 157, 5);
		Add(_image, "image", "articleItem", _bg);

		item.DrawHandSprite(_game.Mod.GetSurfaceSet("BIGOBS.PCK"), _image);

		_txtWeaponClipSize = new NumberText(30, 5, 157, 5);
		Add(_txtWeaponClipSize, "image", "articleItem", _bg);
		_txtWeaponClipSize.X = _txtWeaponClipSize.X + 2;
		_txtWeaponClipSize.SetColor(_textColor);
		_txtWeaponClipSize.Value = (uint)item.ClipSize;
		_txtWeaponClipSize.Visible = Options.oxcePediaShowClipSize && item.ClipSize > 0;

		int ammoSlot = defs.GetAmmoSlotForPage(_state.current_page);
		int ammoSlotPrevUsage = defs.GetAmmoSlotPrevUsageForPage(_state.current_page);
		List<RuleItem> dummy = [];
		List<RuleItem> ammo_data = ammoSlot != RuleItem.AmmoSlotSelfUse ? item.GetCompatibleAmmoForSlot(ammoSlot) : dummy;

		int weight = item.Weight;
		string weightLabel = Tr("STR_WEIGHT_PEDIA1").Arg(weight);
		if (ammo_data.Count != 0)
		{
			// Note: weight including primary ammo only!
			RuleItem ammo_rule = ammo_data[0];
			weightLabel = Tr("STR_WEIGHT_PEDIA2").Arg(weight).Arg(weight + ammo_rule.Weight);
		}
		_txtWeight.SetText(weight != 0 ? weightLabel : "");

		// SHOT STATS TABLE (for firearms and melee only)
		if (item.BattleType == BattleType.BT_FIREARM || item.BattleType == BattleType.BT_MELEE)
		{
			_txtShotType = new Text(100, 17, 8, 66);
			Add(_txtShotType);
			_txtShotType.SetColor(_textColor);
			_txtShotType.SetWordWrap(true);
			_txtShotType.SetText(Tr("STR_SHOT_TYPE"));

			_txtAccuracy = new Text(50, 17, 104, 66);
			Add(_txtAccuracy);
			_txtAccuracy.SetColor(_textColor);
			_txtAccuracy.SetWordWrap(true);
			_txtAccuracy.SetText(Tr("STR_ACCURACY_UC"));

			_txtTuCost = new Text(60, 17, 158, 66);
			Add(_txtTuCost);
			_txtTuCost.SetColor(_textColor);
			_txtTuCost.SetWordWrap(true);
			_txtTuCost.SetText(Tr("STR_TIME_UNIT_COST"));

			_lstInfo = new TextList(204, 55, 8, 82);
			Add(_lstInfo);

			_lstInfo.SetColor(_listColor2); // color for % data!
			_lstInfo.SetColumns(3, 100, 52, 52);
			_lstInfo.SetBig();
		}

		void addAttack(ref int row, string name, RuleItemUseCost cost, RuleItemUseFlat flat, RuleItemAction config, RuleItem weapon)
		{
			if (row < 3 && cost.Time > 0 && config.ammoSlot == ammoSlot)
			{
				string tu = Unicode.FormatPercentage(cost.Time);
				if (flat.Time)
				{
					tu = tu[..^1];
				}
				int range = Math.Min(config.range, weapon.MaxRange);
				string label = string.IsNullOrEmpty(config.shortName) ? Tr(name).Arg(config.shots).Arg(range) : Tr(config.shortName).Arg(config.shots).Arg(range);
				_lstInfo.AddRow(3,
					label,
					Unicode.FormatPercentage(config.accuracy),
					tu);
				_lstInfo.SetCellColor(row, 0, _listColor1);
				row++;
			}
		}

		if (item.BattleType == BattleType.BT_FIREARM)
		{
			int current_row = 0;
			addAttack(ref current_row, "STR_SHOT_TYPE_AUTO", item.CostAuto, item.FlatAuto, item.ConfigAuto, item);
			addAttack(ref current_row, "STR_SHOT_TYPE_SNAP", item.CostSnap, item.FlatSnap, item.ConfigSnap, item);
			addAttack(ref current_row, "STR_SHOT_TYPE_AIMED", item.CostAimed, item.FlatAimed, item.ConfigAimed, item);

			//optional melee
			addAttack(ref current_row, "STR_SHOT_TYPE_MELEE", item.CostMelee, item.FlatMelee, item.ConfigMelee, item);

			// text_info is BELOW the info table (table can have 0-3 rows)
			int shift = (3 - current_row) * 16;
			if (ammo_data.Count == 2 && current_row <= 1)
			{
				shift -= (2 - current_row) * 16;
			}
			_txtInfo = new Text((ammo_data.Count < 3 ? 300 : 180), 56 + shift - bottomOffset, 8, 138 - shift);
		}
		else if (item.BattleType == BattleType.BT_MELEE)
		{
			int current_row = 0;
			addAttack(ref current_row, "STR_SHOT_TYPE_MELEE", item.CostMelee, item.FlatMelee, item.ConfigMelee, item);

			// text_info is BELOW the info table (with 1 row only)
			_txtInfo = new Text(300, 88 - bottomOffset, 8, 106);
		}
		else
		{
			// text_info is larger and starts on top
			_txtInfo = new Text(300, 125 - bottomOffset, 8, 67);
		}

		Add(_txtInfo);

		_txtInfo.SetColor(_textColor);
		_txtInfo.SetSecondaryColor(_textColor2);
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

		// STATS FOR NERDS extract
		_txtAccuracyModifier = new Text(300, 9, 8, 174);
		_txtPowerBonus = new Text(300, 17, 8, 183);

		Add(_txtAccuracyModifier);
		Add(_txtPowerBonus);

		_txtAccuracyModifier.SetColor(_textColor);
		_txtAccuracyModifier.SetSecondaryColor(_listColor2);
		_txtAccuracyModifier.SetWordWrap(false);
		_txtAccuracyModifier.SetText(Tr("STR_ACCURACY_MODIFIER").Arg(accuracyModifier));
		_txtAccuracyModifier.Visible = bottomOffset >= 20;

		_txtPowerBonus.SetColor(_textColor);
		_txtPowerBonus.SetSecondaryColor(_listColor2);
		_txtPowerBonus.SetWordWrap(true);
		_txtPowerBonus.SetText(Tr("STR_POWER_BONUS").Arg(powerBonus));
		_txtPowerBonus.Visible = bottomOffset > 0;

		// AMMO column
		for (int i = 0; i < 3; ++i)
		{
			_txtAmmoType[i] = new Text(82, 16, 194, 20 + i * 49);
			Add(_txtAmmoType[i]);
			_txtAmmoType[i].SetColor(_textColor);
			_txtAmmoType[i].Align = TextHAlign.ALIGN_CENTER;
			_txtAmmoType[i].VerticalAlign = TextVAlign.ALIGN_MIDDLE;
			_txtAmmoType[i].SetWordWrap(true);

			_txtAmmoDamage[i] = new Text(82, 17, 194, 40 + i * 49);
			Add(_txtAmmoDamage[i]);
			_txtAmmoDamage[i].SetColor(_ammoColor);
			_txtAmmoDamage[i].Align = TextHAlign.ALIGN_CENTER;
			_txtAmmoDamage[i].SetBig();

			_imageAmmo[i] = new Surface(32, 48, 280, 16 + i * 49);
			Add(_imageAmmo[i]);

			_txtAmmoClipSize[i] = new NumberText(30, 5, 2 + 280, 2 + 16 + i * 49);
			Add(_txtAmmoClipSize[i]);
			_txtAmmoClipSize[i].SetColor(_textColor);
			_txtAmmoClipSize[i].Visible = false;
		}

		void addAmmoDamagePower(int pos, RuleItem rule, RuleItem weaponRule)
		{
			_txtAmmoType[pos].SetText(Tr(GetDamageTypeText(rule.DamageType.ResistType)));

			string ss;
			if (weaponRule.IgnoreAmmoPower)
			{
				ss = weaponRule.Power.ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				ss = rule.Power.ToString(CultureInfo.InvariantCulture);
			}
			if (rule.ShotgunPellets != 0)
			{
				ss += "x" + rule.ShotgunPellets.ToString(CultureInfo.InvariantCulture);
			}
			_txtAmmoDamage[pos].SetText(ss);
			_txtAmmoDamage[pos].SetColor((byte)GetDamageTypeTextColor(rule.DamageType.ResistType));
		}

		switch (item.BattleType)
		{
			case BattleType.BT_FIREARM:
				if (item.HidePower) break;
				_txtDamage = new Text(82, 10, 194, 7);
				Add(_txtDamage);
				_txtDamage.SetColor(_textColor);
				_txtDamage.Align = TextHAlign.ALIGN_CENTER;
				_txtDamage.SetText(Tr("STR_DAMAGE_UC"));

				_txtAmmo = new Text(50, 10, 268, 7);
				Add(_txtAmmo);
				_txtAmmo.SetColor(_textColor);
				_txtAmmo.Align = TextHAlign.ALIGN_CENTER;
				_txtAmmo.SetText(Tr("STR_AMMO"));

				if (ammo_data.Count == 0)
				{
					addAmmoDamagePower(0, item, item);
				}
				else
				{
					int maxShow = 3;
					int skipShow = maxShow * ammoSlotPrevUsage;
					int currShow = 0;
					foreach (var type in ammo_data)
					{
						ArticleDefinition ammo_article = _game.Mod.GetUfopaediaArticle(type.Type, true);
						if (Ufopaedia.IsArticleAvailable(_game.SavedGame, ammo_article))
						{
							if (skipShow > 0)
							{
								--skipShow;
								continue;
							}
							addAmmoDamagePower(currShow, type, item);
							type.DrawHandSprite(_game.Mod.GetSurfaceSet("BIGOBS.PCK"), _imageAmmo[currShow]);
							_txtAmmoClipSize[currShow].Value = (uint)type.ClipSize;
							_txtAmmoClipSize[currShow].Visible = Options.oxcePediaShowClipSize && type.ClipSize > 0;

							++currShow;
							if (currShow == maxShow)
							{
								break;
							}
						}
					}
				}
				break;
			case BattleType.BT_AMMO:
			case BattleType.BT_GRENADE:
			case BattleType.BT_PROXIMITYGRENADE:
			case BattleType.BT_MELEE:
				if (item.HidePower) break;
				_txtDamage = new Text(82, 10, 194, 7);
				Add(_txtDamage);
				_txtDamage.SetColor(_textColor);
				_txtDamage.Align = TextHAlign.ALIGN_CENTER;
				_txtDamage.SetText(Tr("STR_DAMAGE_UC"));

				addAmmoDamagePower(0, item, item);
				break;
			default: break;
		}

		// multi-page indicator
		_txtArrows = new Text(32, 9, 280, 183);
		Add(_txtArrows);
		_txtArrows.SetColor(_arrowColor);
		_txtArrows.Align = TextHAlign.ALIGN_RIGHT;
		string ss2 = "";
		if (_state.HasPrevArticlePage) ss2 += "<<";
		if (_state.HasNextArticlePage) ss2 += " >>";
		_txtArrows.SetText(ss2);

		CenterAllSurfaces();
	}

	protected string AddRuleStatBonus(RuleStatBonus value)
	{
		// Note: C++ misses an "else" after the "flatOne" branch here, so such a term also gets the generic suffix.
		return StatsForNerdsState.FormatRuleStatBonus(value, stat =>
		{
			if (_game.Mod.ExtraNerdyPediaInfoType > 1)
			{
				return Tr(StatsForNerdsState.shortTranslationMap[stat]);
			}
			else
			{
				return Tr(StatsForNerdsState.translationMap[stat]);
			}
		}, true);
	}

	protected int GetDamageTypeTextColor(ItemDamageType dt)
	{
		Element interfaceElement = null;
		int color = _ammoColor;
		switch (dt)
		{
			case ItemDamageType.DT_NONE:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTNone");
				break;
			case ItemDamageType.DT_AP:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTAP");
				break;
			case ItemDamageType.DT_IN:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTIN");
				break;
			case ItemDamageType.DT_HE:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTHE");
				break;
			case ItemDamageType.DT_LASER:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTLaser");
				break;
			case ItemDamageType.DT_PLASMA:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTPlasma");
				break;
			case ItemDamageType.DT_STUN:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTStun");
				break;
			case ItemDamageType.DT_MELEE:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTMelee");
				break;
			case ItemDamageType.DT_ACID:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTAcid");
				break;
			case ItemDamageType.DT_SMOKE:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDTSmoke");
				break;
			case ItemDamageType.DT_10:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT10");
				break;
			case ItemDamageType.DT_11:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT11");
				break;
			case ItemDamageType.DT_12:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT12");
				break;
			case ItemDamageType.DT_13:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT13");
				break;
			case ItemDamageType.DT_14:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT14");
				break;
			case ItemDamageType.DT_15:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT15");
				break;
			case ItemDamageType.DT_16:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT16");
				break;
			case ItemDamageType.DT_17:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT17");
				break;
			case ItemDamageType.DT_18:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT18");
				break;
			case ItemDamageType.DT_19:
				interfaceElement = _game.Mod.GetInterface("articleItem").GetElementOptional("ammoColorDT19");
				break;
			default:
				break;
		}
		if (interfaceElement != null)
		{
			color = interfaceElement.color;
		}
		return color;
	}
}
