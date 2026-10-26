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
// Ported from: src/Ufopaedia/ArticleStateSoldier.h, src/Ufopaedia/ArticleStateSoldier.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>ArticleStateSoldier has a caption, text and a stats block (min/max/caps of a soldier type).</summary>
public class ArticleStateSoldier : ArticleState
{
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected TextList _lstStats;

	private static string S(int v) => v.ToString(CultureInfo.InvariantCulture);

	public ArticleStateSoldier(ArticleDefinitionSoldier article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		ArticleDefinitionSoldier defs = article_defs;
		RuleSoldier soldier = _game.Mod.GetSoldier(defs.id, true);

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

		RuleInterface itf = _game.Mod.GetInterface("articleSoldier");
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

		int width = Math.Clamp(defs.rect_stats.width, 80, 320);
		_lstStats = new TextList(width, defs.rect_stats.height, defs.rect_stats.x, defs.rect_stats.y);
		Add(_lstStats);

		_lstStats.SetColor(listColor1);
		_lstStats.SetSecondaryColor(listColor2);
		_lstStats.SetColumns(4, width - 60, 20, 20, 20);
		_lstStats.SetDot(true);

		UnitStats min = soldier.MinStats;
		UnitStats max = soldier.MaxStats;
		UnitStats cap = soldier.StatCaps;

		_lstStats.AddRow(4, Tr("STR_TIME_UNITS"), S(min.tu), S(max.tu), S(cap.tu));
		_lstStats.AddRow(4, Tr("STR_STAMINA"), S(min.stamina), S(max.stamina), S(cap.stamina));
		_lstStats.AddRow(4, Tr("STR_HEALTH"), S(min.health), S(max.health), S(cap.health));
		_lstStats.AddRow(4, Tr("STR_BRAVERY"), S(min.bravery), S(max.bravery), S(cap.bravery));
		_lstStats.AddRow(4, Tr("STR_REACTIONS"), S(min.reactions), S(max.reactions), S(cap.reactions));
		_lstStats.AddRow(4, Tr("STR_FIRING_ACCURACY"), S(min.firing), S(max.firing), S(cap.firing));
		_lstStats.AddRow(4, Tr("STR_THROWING_ACCURACY"), S(min.throwing), S(max.throwing), S(cap.throwing));
		_lstStats.AddRow(4, Tr("STR_MELEE_ACCURACY"), S(min.melee), S(max.melee), S(cap.melee));
		_lstStats.AddRow(4, Tr("STR_STRENGTH"), S(min.strength), S(max.strength), S(cap.strength));
		if (_game.Mod.IsManaFeatureEnabled)
		{
			_lstStats.AddRow(4, Tr("STR_MANA_POOL"), S(min.mana), S(max.mana), S(cap.mana));
		}
		_lstStats.AddRow(4, Tr("STR_PSIONIC_STRENGTH"), S(min.psiStrength), S(max.psiStrength), S(cap.psiStrength));
		if (defs.psi_skill_mode == 0)
		{
			// not shown
			_lstStats.AddRow(4, Tr("STR_PSIONIC_SKILL"), "", "", S(cap.psiSkill));
		}
		else if (defs.psi_skill_mode == 1)
		{
			// shown processed
			_lstStats.AddRow(4, Tr("STR_PSIONIC_SKILL"), S(max.psiSkill), S(max.psiSkill * 3 / 2), S(cap.psiSkill));
		}
		else
		{
			// shown raw
			_lstStats.AddRow(4, Tr("STR_PSIONIC_SKILL"), S(min.psiSkill), S(max.psiSkill), S(cap.psiSkill));
		}

		CenterAllSurfaces();
	}
}
