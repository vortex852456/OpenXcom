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
// Ported from: src/Ufopaedia/ArticleStateUfo.h, src/Ufopaedia/ArticleStateUfo.cpp
#nullable disable

namespace Acom;

/// <summary>ArticleStateUfo has a caption, text, preview image and a stats block.</summary>
public class ArticleStateUfo : ArticleState
{
	protected Surface _image;
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected TextList _lstInfo;

	public ArticleStateUfo(ArticleDefinitionUfo article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		ArticleDefinitionUfo defs = article_defs;
		RuleUfo ufo = _game.Mod.GetUfo(defs.id, true);

		// add screen elements
		_txtTitle = new Text(155, 32, 5, 24);

		// Set palette
		SetStandardPalette("PAL_GEOSCAPE");

		InitLayout();

		// add other elements
		Add(_txtTitle);

		// Set up objects
		_game.Mod.GetSurface("BACK11.SCR").BlitNShade(_bg, 0, 0);
		_btnOk.SetColor((byte)(Acom.Palette.BlockOffset(8) + 5));
		_btnPrev.SetColor((byte)(Acom.Palette.BlockOffset(8) + 5));
		_btnNext.SetColor((byte)(Acom.Palette.BlockOffset(8) + 5));
		_btnInfo.SetColor((byte)(Acom.Palette.BlockOffset(8) + 5));
		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		_txtTitle.SetColor((byte)(Acom.Palette.BlockOffset(8) + 5));
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		_image = new Surface(160, 52, 160, 6);
		Add(_image);

		RuleInterface dogfightInterface = _game.Mod.GetInterface("dogfight");
		SurfaceCrop crop = _game.Mod.GetSurface("INTERWIN.DAT").GetCrop();
		crop.X = 0;
		crop.Y = 0;
		crop.Crop.x = 0;
		crop.Crop.y = 0;
		crop.Crop.w = (ushort)_image.Width;
		crop.Crop.h = (ushort)_image.Height;
		_image.DrawRect(ref crop.Crop, 15);
		crop.Blit(_image);

		if (string.IsNullOrEmpty(ufo.ModSprite))
		{
			crop.Crop.y = (short)(dogfightInterface.GetElement("previewMid").y + dogfightInterface.GetElement("previewMid").h * ufo.Sprite);
			crop.Crop.h = (ushort)dogfightInterface.GetElement("previewMid").h;
		}
		else
		{
			crop = _game.Mod.GetSurface(ufo.ModSprite).GetCrop();
		}
		crop.X = 0;
		crop.Y = 0;
		crop.Blit(_image);

		_txtInfo = new Text(300, 50, 10, 140);
		Add(_txtInfo);

		_txtInfo.SetColor((byte)(Acom.Palette.BlockOffset(8) + 5));
		_txtInfo.SetSecondaryColor((byte)(Acom.Palette.BlockOffset(8) + 10));
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

		_lstInfo = new TextList(310, 64, 10, 68);
		Add(_lstInfo);

		CenterAllSurfaces();

		_lstInfo.SetColor((byte)(Acom.Palette.BlockOffset(8) + 5));
		_lstInfo.SetColumns(2, 200, 110);
		//_lstInfo.SetCondensed(true);
		_lstInfo.SetBig();
		_lstInfo.SetDot(true);

		_lstInfo.AddRow(2, Tr("STR_DAMAGE_CAPACITY"), Unicode.FormatNumber(ufo.Stats.damageMax));

		_lstInfo.AddRow(2, Tr("STR_WEAPON_POWER"), Unicode.FormatNumber(ufo.WeaponPower));

		_lstInfo.AddRow(2, Tr("STR_WEAPON_RANGE"), Tr("STR_KILOMETERS").Arg(ufo.WeaponRange));

		_lstInfo.AddRow(2, Tr("STR_MAXIMUM_SPEED"), Tr("STR_KNOTS").Arg(Unicode.FormatNumber(ufo.Stats.speedMax)));
	}
}
