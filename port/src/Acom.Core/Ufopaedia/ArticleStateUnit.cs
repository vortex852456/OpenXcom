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
// Ported from: src/Ufopaedia/ArticleStateUnit.h, src/Ufopaedia/ArticleStateUnit.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>ArticleStateUnit has a caption, text, background image and stats/armor blocks of a battle unit.</summary>
public class ArticleStateUnit : ArticleState
{
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected Text _txtDifficulty;
	protected TextList _lstStats;
	protected TextList _lstArmor;

	private static string S(int v) => v.ToString(CultureInfo.InvariantCulture);

	public ArticleStateUnit(ArticleDefinitionUnit article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		ArticleDefinitionUnit defs = article_defs;
		Unit unit = _game.Mod.GetUnit(defs.id, true);

		// add screen elements
		_txtTitle = new Text(310, 17, 5, 23);

		// Set palette
		if (defs.customPalette)
		{
			SetCustomPalette(_game.Mod.GetSurface(defs.image_id).Palette, Mod.UFOPAEDIA_CURSOR);
		}
		else
		{
			SetStandardPalette("PAL_UFOPAEDIA");
		}

		RuleInterface itf = _game.Mod.GetInterface("articleUnit");
		byte buttonColor = (byte)itf.GetElement("button").color;
		byte titleColor1 = (byte)itf.GetElement("title").color;
		byte titleColor2 = (byte)itf.GetElement("title").color2;
		byte textColor1 = (byte)itf.GetElement("text").color;
		byte textColor2 = (byte)itf.GetElement("text").color2;
		byte listColor1 = (byte)itf.GetElement("list").color;
		byte listColor2 = (byte)itf.GetElement("list").color2;

		InitLayout();

		// add other elements
		Add(_txtTitle);

		// Set up objects
		_game.Mod.GetSurface(defs.image_id).BlitNShade(_bg, 0, 0);
		_btnOk.SetColor(buttonColor);
		_btnPrev.SetColor(buttonColor);
		_btnNext.SetColor(buttonColor);
		_btnInfo.SetColor(buttonColor);
		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		_txtTitle.SetColor(titleColor1);
		_txtTitle.SetSecondaryColor(titleColor2);
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		_txtInfo = new Text(defs.rect_text.width, defs.rect_text.height, defs.rect_text.x, defs.rect_text.y);
		Add(_txtInfo);

		_txtInfo.SetColor(textColor1);
		_txtInfo.SetSecondaryColor(textColor2);
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

		int widthStats = Math.Clamp(defs.rect_stats.width, 60, 320);
		_lstStats = new TextList(widthStats, defs.rect_stats.height, defs.rect_stats.x, defs.rect_stats.y);
		Add(_lstStats);

		_lstStats.SetColor(listColor1);
		_lstStats.SetSecondaryColor(listColor2);
		_lstStats.SetColumns(3, widthStats - 40, 20, 20);
		_lstStats.SetDot(true);

		int widthArmor = Math.Clamp(defs.rect_armor.width, 60, 320);
		_lstArmor = new TextList(widthArmor, defs.rect_armor.height, defs.rect_armor.x, defs.rect_armor.y);
		Add(_lstArmor);

		_lstArmor.SetColor(listColor1);
		_lstArmor.SetSecondaryColor(listColor2);
		_lstArmor.SetColumns(3, widthArmor - 40, 20, 20);
		_lstArmor.SetDot(true);

		if (defs.rect_armor.height == 0)
		{
			_lstArmor.Visible = false;
		}

		_txtDifficulty = new Text(160, 9, defs.rect_stats.x + widthStats - (defs.unit_mode == 2 ? 20 : 40), defs.rect_stats.y - 10);
		Add(_txtDifficulty);

		_txtDifficulty.SetColor(textColor1);
		_txtDifficulty.SetSecondaryColor(textColor2);

		UnitStats civ = unit.Stats;
		UnitStats alien = civ;

		int[] civArmor = new int[(int)UnitSide.SIDE_MAX];
		civArmor[(int)UnitSide.SIDE_FRONT] = unit.Armor.FrontArmor;
		civArmor[(int)UnitSide.SIDE_LEFT] = unit.Armor.LeftSideArmor;
		civArmor[(int)UnitSide.SIDE_RIGHT] = unit.Armor.RightSideArmor;
		civArmor[(int)UnitSide.SIDE_REAR] = unit.Armor.RearArmor;
		civArmor[(int)UnitSide.SIDE_UNDER] = unit.Armor.UnderArmor;
		int[] alienArmor = new int[(int)UnitSide.SIDE_MAX];
		alienArmor[(int)UnitSide.SIDE_FRONT] = civArmor[(int)UnitSide.SIDE_FRONT];
		alienArmor[(int)UnitSide.SIDE_LEFT] = civArmor[(int)UnitSide.SIDE_LEFT];
		alienArmor[(int)UnitSide.SIDE_RIGHT] = civArmor[(int)UnitSide.SIDE_RIGHT];
		alienArmor[(int)UnitSide.SIDE_REAR] = civArmor[(int)UnitSide.SIDE_REAR];
		alienArmor[(int)UnitSide.SIDE_UNDER] = civArmor[(int)UnitSide.SIDE_UNDER];

		if (_game.SavedGame != null)
		{
			if (defs.unit_mode != 1)
			{
				string diff;
				switch (_game.SavedGame.Difficulty)
				{
				case GameDifficulty.DIFF_SUPERHUMAN: diff = Tr("STR_5_SUPERHUMAN"); break;
				case GameDifficulty.DIFF_GENIUS: diff = Tr("STR_4_GENIUS"); break;
				case GameDifficulty.DIFF_VETERAN: diff = Tr("STR_3_VETERAN"); break;
				case GameDifficulty.DIFF_EXPERIENCED: diff = Tr("STR_2_EXPERIENCED"); break;
				default: diff = Tr("STR_1_BEGINNER"); break;
				}
				_txtDifficulty.SetText(diff);
			}

			// FACTION_HOSTILE scales with difficulty
			StatAdjustment adjustment = _game.Mod.GetStatAdjustment((int)_game.SavedGame.Difficulty);
			alien += UnitStats.Percent(alien, adjustment.statGrowth, adjustment.growthMultiplier);
			alien.firing = (short)(alien.firing * adjustment.aimMultiplier);
			alien += adjustment.statGrowthAbs;
			for (int i = 0; i < (int)UnitSide.SIDE_MAX; ++i)
			{
				alienArmor[i] = (int)(alienArmor[i] * adjustment.armorMultiplier);
				alienArmor[i] += adjustment.armorMultiplierAbs;
			}
		}

		// defs.unit_mode == 0  show as hostile (incl. difficulty)
		// defs.unit_mode == 1  show as neutral/allied
		// defs.unit_mode == 2  show as neutral/allied first, then also as hostile (incl. difficulty)
		int columns = defs.unit_mode > 1 ? 3 : 2;
		if (defs.unit_mode == 0)
		{
			civ = alien;
			for (int i = 0; i < (int)UnitSide.SIDE_MAX; ++i)
			{
				civArmor[i] = alienArmor[i];
			}
		}

		_lstStats.AddRow(columns, Tr("STR_TIME_UNITS"), S(civ.tu), S(alien.tu));
		_lstStats.AddRow(columns, Tr("STR_STAMINA"), S(civ.stamina), S(alien.stamina));
		_lstStats.AddRow(columns, Tr("STR_HEALTH"), S(civ.health), S(alien.health));
		_lstStats.AddRow(columns, Tr("STR_BRAVERY"), S(civ.bravery), S(alien.bravery));
		_lstStats.AddRow(columns, Tr("STR_REACTIONS"), S(civ.reactions), S(alien.reactions));
		_lstStats.AddRow(columns, Tr("STR_FIRING_ACCURACY"), S(civ.firing), S(alien.firing));
		_lstStats.AddRow(columns, Tr("STR_THROWING_ACCURACY"), S(civ.throwing), S(alien.throwing));
		_lstStats.AddRow(columns, Tr("STR_MELEE_ACCURACY"), S(civ.melee), S(alien.melee));
		_lstStats.AddRow(columns, Tr("STR_STRENGTH"), S(civ.strength), S(alien.strength));
		if (_game.Mod.IsManaFeatureEnabled)
		{
			_lstStats.AddRow(columns, Tr("STR_MANA_POOL"), S(civ.mana), S(alien.mana));
		}
		_lstStats.AddRow(columns, Tr("STR_PSIONIC_STRENGTH"), S(civ.psiStrength), S(alien.psiStrength));
		_lstStats.AddRow(columns, Tr("STR_PSIONIC_SKILL"), S(civ.psiSkill), S(alien.psiSkill));

		_lstArmor.AddRow(columns, Tr("STR_FRONT_ARMOR"), S(civArmor[(int)UnitSide.SIDE_FRONT]), S(alienArmor[(int)UnitSide.SIDE_FRONT]));
		_lstArmor.AddRow(columns, Tr("STR_LEFT_ARMOR"), S(civArmor[(int)UnitSide.SIDE_LEFT]), S(alienArmor[(int)UnitSide.SIDE_LEFT]));
		_lstArmor.AddRow(columns, Tr("STR_RIGHT_ARMOR"), S(civArmor[(int)UnitSide.SIDE_RIGHT]), S(alienArmor[(int)UnitSide.SIDE_RIGHT]));
		_lstArmor.AddRow(columns, Tr("STR_REAR_ARMOR"), S(civArmor[(int)UnitSide.SIDE_REAR]), S(alienArmor[(int)UnitSide.SIDE_REAR]));
		_lstArmor.AddRow(columns, Tr("STR_UNDER_ARMOR"), S(civArmor[(int)UnitSide.SIDE_UNDER]), S(alienArmor[(int)UnitSide.SIDE_UNDER]));

		CenterAllSurfaces();
	}
}
