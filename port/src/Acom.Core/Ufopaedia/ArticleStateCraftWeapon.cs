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
// Ported from: src/Ufopaedia/ArticleStateCraftWeapon.h, src/Ufopaedia/ArticleStateCraftWeapon.cpp
#nullable disable

namespace Acom;

/// <summary>ArticleStateCraftWeapon has a caption, text, background image and a stats block.</summary>
public class ArticleStateCraftWeapon : ArticleState
{
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected TextList _lstInfo;
	protected byte _buttonColor;
	protected byte _textColor;
	protected byte _textColor2;
	protected byte _listColor1;
	protected byte _listColor2;

	public ArticleStateCraftWeapon(ArticleDefinitionCraftWeapon article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		ArticleDefinitionCraftWeapon defs = article_defs;
		RuleCraftWeapon weapon = _game.Mod.GetCraftWeapon(defs.id, true);
		CraftWeaponCategory category = CraftWeaponCategory.CWC_WEAPON;
		int offset = 0;
		if (weapon.HidePediaInfo)
		{
			if (weapon.TractorBeamPower > 0)
			{
				category = CraftWeaponCategory.CWC_TRACTOR_BEAM;
				offset = 32; // 2 * 16
			}
			else
			{
				category = CraftWeaponCategory.CWC_EQUIPMENT;
				offset = 80; // 5 * 16
			}
		}

		// add screen elements
		_txtTitle = new Text(200, 32, 5, 24);

		// Set palette
		if (defs.customPalette)
		{
			SetCustomPalette(_game.Mod.GetSurface(defs.image_id).Palette, Mod.BATTLESCAPE_CURSOR);
		}
		else
		{
			SetStandardPalette("PAL_BATTLEPEDIA");
		}

		_buttonColor = (byte)_game.Mod.GetInterface("articleCraftWeapon").GetElement("button").color;
		_textColor = (byte)_game.Mod.GetInterface("articleCraftWeapon").GetElement("text").color;
		_textColor2 = (byte)_game.Mod.GetInterface("articleCraftWeapon").GetElement("text").color2;
		_listColor1 = (byte)_game.Mod.GetInterface("articleCraftWeapon").GetElement("list").color;
		_listColor2 = (byte)_game.Mod.GetInterface("articleCraftWeapon").GetElement("list").color2;

		InitLayout();

		// add other elements
		Add(_txtTitle);

		// Set up objects
		_game.Mod.GetSurface(defs.image_id).BlitNShade(_bg, 0, 0);
		_btnOk.SetColor(_buttonColor);
		_btnPrev.SetColor(_buttonColor);
		_btnNext.SetColor(_buttonColor);
		_btnInfo.SetColor(_buttonColor);
		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		_txtTitle.SetColor(_textColor);
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		_txtInfo = new Text(310, 32 + offset, 5, 160 - offset);
		Add(_txtInfo);

		_txtInfo.SetColor(_textColor);
		_txtInfo.SetSecondaryColor(_textColor2);
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

		_lstInfo = new TextList(250, 111 - offset, 5, 80);
		Add(_lstInfo);

		_lstInfo.Visible = category != CraftWeaponCategory.CWC_EQUIPMENT;
		_lstInfo.SetColor(_listColor1);
		_lstInfo.SetColumns(2, 180, 70);
		_lstInfo.SetDot(true);
		_lstInfo.SetBig();

		if (category == CraftWeaponCategory.CWC_WEAPON)
		{
			_lstInfo.AddRow(2, Tr("STR_DAMAGE"), Unicode.FormatNumber(weapon.Damage));
			_lstInfo.SetCellColor(0, 1, _listColor2);

			_lstInfo.AddRow(2, Tr("STR_RANGE"), Tr("STR_KILOMETERS").Arg(weapon.Range));
			_lstInfo.SetCellColor(1, 1, _listColor2);

			_lstInfo.AddRow(2, Tr("STR_ACCURACY"), Unicode.FormatPercentage(weapon.Accuracy));
			_lstInfo.SetCellColor(2, 1, _listColor2);

			_lstInfo.AddRow(2, Tr("STR_RE_LOAD_TIME"), Tr("STR_SECONDS").Arg(weapon.StandardReload));
			_lstInfo.SetCellColor(3, 1, _listColor2);

			_lstInfo.AddRow(2, Tr("STR_ROUNDS"), Unicode.FormatNumber(weapon.AmmoMax));
			_lstInfo.SetCellColor(4, 1, _listColor2);
		}
		else if (category == CraftWeaponCategory.CWC_TRACTOR_BEAM)
		{
			_lstInfo.AddRow(2, Tr("STR_TRACTOR_BEAM_POWER"), Unicode.FormatNumber(weapon.TractorBeamPower));
			_lstInfo.SetCellColor(0, 1, _listColor2);

			_lstInfo.AddRow(2, Tr("STR_RANGE"), Tr("STR_KILOMETERS").Arg(weapon.Range));
			_lstInfo.SetCellColor(1, 1, _listColor2);
		}

		CenterAllSurfaces();
	}
}
