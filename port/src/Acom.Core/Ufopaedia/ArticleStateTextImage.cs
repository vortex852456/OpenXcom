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
// Ported from: src/Ufopaedia/ArticleStateTextImage.h, src/Ufopaedia/ArticleStateTextImage.cpp
#nullable disable

namespace Acom;

/// <summary>ArticleStateTextImage has a title, text block and a background image.</summary>
public class ArticleStateTextImage : ArticleState
{
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected byte _buttonColor;
	protected byte _titleColor;
	protected byte _textColor1;
	protected byte _textColor2;

	public ArticleStateTextImage(ArticleDefinitionTextImage defs, ArticleCommonState state) : base(defs.id, state)
	{
		// add screen elements
		_txtTitle = new Text(defs.text_width, 48, 5, 22);

		// Set palette
		if (defs.customPalette)
		{
			SetCustomPalette(_game.Mod.GetSurface(defs.image_id).Palette, Mod.UFOPAEDIA_CURSOR);
		}
		else
		{
			SetStandardPalette("PAL_UFOPAEDIA");
		}

		_buttonColor = (byte)_game.Mod.GetInterface("articleTextImage").GetElement("button").color;
		_titleColor = (byte)_game.Mod.GetInterface("articleTextImage").GetElement("title").color;
		_textColor1 = (byte)_game.Mod.GetInterface("articleTextImage").GetElement("text").color;
		_textColor2 = (byte)_game.Mod.GetInterface("articleTextImage").GetElement("text").color2;

		InitLayout();

		// add other elements
		Add(_txtTitle);

		// Set up objects
		_game.Mod.GetSurface(defs.image_id).BlitNShade(_bg, 0, 0);
		_btnOk.SetColor(_buttonColor);
		_btnPrev.SetColor(_buttonColor);
		_btnNext.SetColor(_buttonColor);

		_txtTitle.SetColor(_titleColor);
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		int text_height = _txtTitle.GetTextHeight();

		if (defs.rect_text.width == 0)
		{
			_txtInfo = new Text(defs.text_width, 176 - text_height, 5, 23 + text_height);
		}
		else
		{
			_txtInfo = new Text(defs.rect_text.width, defs.rect_text.height, defs.rect_text.x, defs.rect_text.y);
		}
		Add(_txtInfo);

		_txtInfo.SetColor(_textColor1);
		_txtInfo.SetSecondaryColor(_textColor2);
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		if (defs.align_bottom)
		{
			_txtInfo.VerticalAlign = TextVAlign.ALIGN_BOTTOM;
		}
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

		CenterAllSurfaces();
	}
}
