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
// Ported from: src/Ufopaedia/ArticleStateText.h, src/Ufopaedia/ArticleStateText.cpp
#nullable disable

namespace Acom;

/// <summary>ArticleStateText has only a caption and a text.</summary>
public class ArticleStateText : ArticleState
{
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected byte _buttonColor;
	protected byte _titleColor;
	protected byte _textColor1;
	protected byte _textColor2;

	public ArticleStateText(ArticleDefinitionText article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		// add screen elements
		_txtTitle = new Text(296, 17, 5, 23);
		_txtInfo = new Text(296, 150, 10, 48);

		// Set palette
		SetStandardPalette("PAL_UFOPAEDIA");

		_buttonColor = (byte)_game.Mod.GetInterface("articleText").GetElement("button").color;
		_titleColor = (byte)_game.Mod.GetInterface("articleText").GetElement("title").color;
		_textColor1 = (byte)_game.Mod.GetInterface("articleText").GetElement("text").color;
		_textColor2 = (byte)_game.Mod.GetInterface("articleText").GetElement("text").color2;

		InitLayout();

		// add other elements
		Add(_txtTitle);
		Add(_txtInfo);

		CenterAllSurfaces();

		// Set up objects
		_game.Mod.GetSurface("BACK10.SCR").BlitNShade(_bg, 0, 0);
		_btnOk.SetColor(_buttonColor);
		_btnPrev.SetColor(_buttonColor);
		_btnNext.SetColor(_buttonColor);

		_txtTitle.SetColor(_titleColor);
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr(article_defs.GetTitleForPage(_state.current_page)));

		_txtInfo.SetColor(_textColor1);
		_txtInfo.SetSecondaryColor(_textColor2);
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(article_defs.GetTextForPage(_state.current_page)));
	}
}
