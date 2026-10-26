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
// Ported from: src/Ufopaedia/ArticleStateTFTDItem.h, src/Ufopaedia/ArticleStateTFTDItem.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>TFTD-style article for items.</summary>
public class ArticleStateTFTDItem : ArticleStateTFTD
{
	protected TextList _lstInfo;
	protected Text _txtShotType;
	protected Text _txtAccuracy;
	protected Text _txtTuCost;
	protected Text[] _txtAmmoType = new Text[3];
	protected Text[] _txtAmmoDamage = new Text[3];
	protected Text _txtArrows;

	public ArticleStateTFTDItem(ArticleDefinitionTFTD defs, ArticleCommonState state) : base(defs, state)
	{
		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		RuleItem item = _game.Mod.GetItem(defs.weapon, false);
		if (item == null)
		{
			item = _game.Mod.GetItem(defs.id, true);
		}

		int ammoSlot = defs.GetAmmoSlotForPage(_state.current_page);
		int ammoSlotPrevUsage = defs.GetAmmoSlotPrevUsageForPage(_state.current_page);
		List<RuleItem> dummy = [];
		List<RuleItem> ammo_data = ammoSlot != RuleItem.AmmoSlotSelfUse ? item.GetCompatibleAmmoForSlot(ammoSlot) : dummy;

		// SHOT STATS TABLE (for firearms only)
		if (item.BattleType == BattleType.BT_FIREARM)
		{
			_txtShotType = new Text(53, 17, 8, 157);
			Add(_txtShotType);
			_txtShotType.SetColor(_textColor);
			_txtShotType.SetWordWrap(true);
			_txtShotType.SetText(Tr("STR_SHOT_TYPE"));

			_txtAccuracy = new Text(57, 17, 61, 157);
			Add(_txtAccuracy);
			_txtAccuracy.SetColor(_textColor);
			_txtAccuracy.SetWordWrap(true);
			_txtAccuracy.SetText(Tr("STR_ACCURACY_UC"));

			_txtTuCost = new Text(56, 17, 118, 157);
			Add(_txtTuCost);
			_txtTuCost.SetColor(_textColor);
			_txtTuCost.SetWordWrap(true);
			_txtTuCost.SetText(Tr("STR_TIME_UNIT_COST"));

			_lstInfo = new TextList(140, 55, 8, 170);
			Add(_lstInfo);

			_lstInfo.SetColor(_listColor2); // color for % data!
			_lstInfo.SetColumns(3, 70, 40, 30);

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

			int current_row = 0;
			addAttack(ref current_row, "STR_SHOT_TYPE_AUTO", item.CostAuto, item.FlatAuto, item.ConfigAuto, item);
			addAttack(ref current_row, "STR_SHOT_TYPE_SNAP", item.CostSnap, item.FlatSnap, item.ConfigSnap, item);
			addAttack(ref current_row, "STR_SHOT_TYPE_AIMED", item.CostAimed, item.FlatAimed, item.ConfigAimed, item);

			//optional melee
			addAttack(ref current_row, "STR_SHOT_TYPE_MELEE", item.CostMelee, item.FlatMelee, item.ConfigMelee, item);
		}

		// AMMO column
		for (int i = 0; i < 3; ++i)
		{
			_txtAmmoType[i] = new Text(120, 9, 168, 144 + i * 10);
			Add(_txtAmmoType[i]);
			_txtAmmoType[i].SetColor(_textColor);
			_txtAmmoType[i].SetWordWrap(true);

			_txtAmmoDamage[i] = new Text(20, 9, 300, 144 + i * 10);
			Add(_txtAmmoDamage[i]);
			_txtAmmoDamage[i].SetColor(_ammoColor);
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
		}

		switch (item.BattleType)
		{
			case BattleType.BT_FIREARM:
				if (item.HidePower) break;
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
				addAmmoDamagePower(0, item, item);
				break;
			default: break;
		}

		if (_txtAmmoType[0].GetText().Length != 0)
			_txtInfo.SetHeight(112);

		// multi-page indicator
		_txtArrows = new Text(32, 9, 277, 134);
		Add(_txtArrows);
		_txtArrows.SetColor(_arrowColor);
		_txtArrows.Align = TextHAlign.ALIGN_RIGHT;
		string ss2 = "";
		if (_state.HasPrevArticlePage) ss2 += "<<";
		if (_state.HasNextArticlePage) ss2 += " >>";
		_txtArrows.SetText(ss2);

		CenterAllSurfaces();
	}
}
