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
// Ported from: src/Ufopaedia/ArticleStateTFTD.h, src/Ufopaedia/ArticleStateTFTD.cpp
#nullable disable

namespace Acom;

/// <summary>Common base for the TFTD-style articles: background, optional image, title and text.</summary>
public class ArticleStateTFTD : ArticleState
{
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected byte _buttonColor;
	protected byte _textColor;
	protected byte _textColor2;
	protected byte _listColor1;
	protected byte _listColor2;
	protected byte _ammoColor;
	protected byte _arrowColor;

	public ArticleStateTFTD(ArticleDefinitionTFTD defs, ArticleCommonState state) : base(defs.id, state)
	{
		RuleInterface ruleInterface;
		switch (defs.Type)
		{
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD:
				ruleInterface = _game.Mod.GetInterface("articleTFTD");
				break;
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT:
				ruleInterface = _game.Mod.GetInterface("articleCraftTFTD");
				break;
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT_WEAPON:
				ruleInterface = _game.Mod.GetInterface("articleCraftWeaponTFTD");
				break;
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_VEHICLE:
				ruleInterface = _game.Mod.GetInterface("articleVehicleTFTD");
				break;
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ITEM:
				ruleInterface = _game.Mod.GetInterface("articleItemTFTD");
				break;
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ARMOR:
				ruleInterface = _game.Mod.GetInterface("articleArmorTFTD");
				break;
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_BASE_FACILITY:
				ruleInterface = _game.Mod.GetInterface("articleBaseFacilityTFTD");
				break;
			case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_USO:
				ruleInterface = _game.Mod.GetInterface("articleUsoTFTD");
				break;
			default:
				ruleInterface = _game.Mod.GetInterface("articleTFTD");
				break;
		}

		// Set palette
		if (defs.customPalette)
		{
			if (ruleInterface.Palette == "PAL_GEOSCAPE")
				_cursorColor = (byte)Mod.GEOSCAPE_CURSOR;
			else if (ruleInterface.Palette == "PAL_BASESCAPE")
				_cursorColor = (byte)Mod.BASESCAPE_CURSOR;
			else if (ruleInterface.Palette == "PAL_UFOPAEDIA")
				_cursorColor = (byte)Mod.UFOPAEDIA_CURSOR;
			else if (ruleInterface.Palette == "PAL_GRAPHS")
				_cursorColor = (byte)Mod.GRAPHS_CURSOR;
			else
				_cursorColor = (byte)Mod.BATTLESCAPE_CURSOR;

			SetCustomPalette(_game.Mod.GetSurface(defs.image_id).Palette, _cursorColor);
		}
		else
		{
			SetStandardPalette(ruleInterface.Palette);
		}

		_buttonColor = (byte)ruleInterface.GetElement("button").color;
		_textColor = (byte)ruleInterface.GetElement("text").color;
		_textColor2 = (byte)ruleInterface.GetElement("text").color2;
		_listColor1 = (byte)ruleInterface.GetElement("list").color;
		_listColor2 = (byte)ruleInterface.GetElement("list").color2;
		_arrowColor = _listColor2;
		if (ruleInterface.GetElementOptional("arrow") != null)
		{
			_arrowColor = (byte)ruleInterface.GetElement("arrow").color;
		}
		if (ruleInterface.GetElementOptional("ammoColor") != null)
		{
			_ammoColor = (byte)ruleInterface.GetElement("ammoColor").color;
		}

		_btnInfo.X = 183;
		_btnInfo.Y = 179;
		_btnInfo.SetHeight(10);
		_btnInfo.SetWidth(40);
		_btnInfo.SetColor(_buttonColor);
		_btnOk.X = 227;
		_btnOk.Y = 179;
		_btnOk.SetHeight(10);
		_btnOk.SetWidth(23);
		_btnOk.SetColor(_buttonColor);
		_btnPrev.X = 254;
		_btnPrev.Y = 179;
		_btnPrev.SetHeight(10);
		_btnPrev.SetWidth(23);
		_btnPrev.SetColor(_buttonColor);
		_btnNext.X = 281;
		_btnNext.Y = 179;
		_btnNext.SetHeight(10);
		_btnNext.SetWidth(23);
		_btnNext.SetColor(_buttonColor);

		InitLayout();

		// Step 1: background image
		string bgImageName = ruleInterface.GetBackgroundImage(_game.Mod, _game.SavedGame);
		if (!defs.customPalette)
		{
			_game.Mod.GetSurface(bgImageName).BlitNShade(_bg, 0, 0);
		}

		// Step 2: article image (optional)
		Surface image = _game.Mod.GetSurface(defs.image_id, false);
		if (image != null)
		{
			image.BlitNShade(_bg, 0, 0);
		}

		// Step 3: info button image
		Surface button = _game.Mod.GetSurface(bgImageName + "-InfoButton", false);
		if (!defs.customPalette && button != null && _game.Mod.ShowPediaInfoButton)
		{
			switch (defs.Type)
			{
				case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ITEM:
				case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ARMOR:
				case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_BASE_FACILITY:
				case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT:
				case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_CRAFT_WEAPON:
				case UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_USO:
					button.BlitNShade(_bg, 0, 0);
					break;
				default:
					break;
			}
		}

		_txtInfo = new Text(defs.text_width, 136, 320 - defs.text_width, 34);
		_txtTitle = new Text(284, 16, 36, 14);

		Add(_txtTitle);
		Add(_txtInfo);

		_txtTitle.SetColor(_textColor);
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		_txtInfo.SetColor(_textColor);
		_txtInfo.SetSecondaryColor(_textColor2);
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

		// all of the above are common to the TFTD articles.

		if (defs.Type == UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD)
		{
			// this command is contained in all the subtypes of this article,
			// and probably shouldn't run until all surfaces are added.
			// in the case of a simple image/title/text article,
			// we're done adding surfaces for now.
			CenterAllSurfaces();
		}
	}
}
