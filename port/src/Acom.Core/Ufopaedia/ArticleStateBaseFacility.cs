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
// Ported from: src/Ufopaedia/ArticleStateBaseFacility.h, src/Ufopaedia/ArticleStateBaseFacility.cpp
#nullable disable

using System.Globalization;

namespace Acom;

/// <summary>ArticleStateBaseFacility has a caption, text, preview image and a stats block.</summary>
public class ArticleStateBaseFacility : ArticleState
{
	protected Surface _image;
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected TextList _lstInfo;

	public ArticleStateBaseFacility(ArticleDefinitionBaseFacility article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		ArticleDefinitionBaseFacility defs = article_defs;
		RuleBaseFacility facility = _game.Mod.GetBaseFacility(defs.id, true);

		// add screen elements
		_txtTitle = new Text(200, 17, 10, 24);

		// Set palette
		SetStandardPalette("PAL_BASESCAPE");

		InitLayout();

		// add other elements
		Add(_txtTitle);

		// Set up objects
		_game.Mod.GetSurface("BACK09.SCR").BlitNShade(_bg, 0, 0);
		_btnOk.SetColor(Acom.Palette.BlockOffset(4));
		_btnPrev.SetColor(Acom.Palette.BlockOffset(4));
		_btnNext.SetColor(Acom.Palette.BlockOffset(4));
		_btnInfo.SetColor(Acom.Palette.BlockOffset(4));
		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		_txtTitle.SetColor((byte)(Acom.Palette.BlockOffset(13) + 10));
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		// build preview image
		int tile_size = 32;
		_image = new Surface(
			tile_size * Math.Max(1, Mod.PEDIA_FACILITY_RENDER_PARAMETERS[0]),
			tile_size * Math.Max(1, Mod.PEDIA_FACILITY_RENDER_PARAMETERS[1]),
			232 + Mod.PEDIA_FACILITY_RENDER_PARAMETERS[2],
			16 + Mod.PEDIA_FACILITY_RENDER_PARAMETERS[3]);
		Add(_image);

		SurfaceSet graphic = _game.Mod.GetSurfaceSet("BASEBITS.PCK");
		Surface frame;
		int x_offset, y_offset;
		int x_pos, y_pos;
		int num;

		// calculate preview offset
		x_offset = (tile_size * Math.Max(0, Mod.PEDIA_FACILITY_RENDER_PARAMETERS[0] - facility.SizeX)) / 2;
		y_offset = (tile_size * Math.Max(0, Mod.PEDIA_FACILITY_RENDER_PARAMETERS[1] - facility.SizeY)) / 2;

		// render build preview
		num = 0;
		y_pos = y_offset;
		for (int y = 0; y < facility.SizeY; ++y)
		{
			x_pos = x_offset;
			for (int x = 0; x < facility.SizeX; ++x)
			{
				frame = graphic.GetFrame(facility.SpriteShape + num);
				frame.BlitNShade(_image, x_pos, y_pos);
				if (facility.SpriteEnabled)
				{
					frame = graphic.GetFrame(facility.SpriteFacility + num);
					frame.BlitNShade(_image, x_pos, y_pos);
				}
				x_pos += tile_size;
				num++;
			}
			y_pos += tile_size;
		}

		_txtInfo = new Text(300, 90, 10, 104);
		Add(_txtInfo);

		_txtInfo.SetColor((byte)(Acom.Palette.BlockOffset(13) + 10));
		_txtInfo.SetSecondaryColor(Acom.Palette.BlockOffset(13));
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

		_lstInfo = new TextList(200, 42, 10, 42);
		Add(_lstInfo);

		_lstInfo.SetColor((byte)(Acom.Palette.BlockOffset(13) + 10));
		_lstInfo.SetColumns(2, 140, 60);
		_lstInfo.SetDot(true);

		_lstInfo.AddRow(2, Tr("STR_CONSTRUCTION_TIME"), Tr("STR_DAY", facility.BuildTime));
		_lstInfo.SetCellColor(0, 1, (byte)(Acom.Palette.BlockOffset(13) + 0));

		_lstInfo.AddRow(2, Tr("STR_CONSTRUCTION_COST"), Unicode.FormatFunding(facility.BuildCost));
		_lstInfo.SetCellColor(1, 1, (byte)(Acom.Palette.BlockOffset(13) + 0));

		_lstInfo.AddRow(2, Tr("STR_MAINTENANCE_COST"), Unicode.FormatFunding(facility.MonthlyCost));
		_lstInfo.SetCellColor(2, 1, (byte)(Acom.Palette.BlockOffset(13) + 0));

		if (facility.DefenseValue > 0)
		{
			_lstInfo.AddRow(2, Tr("STR_DEFENSE_VALUE"), facility.DefenseValue.ToString(CultureInfo.InvariantCulture));
			_lstInfo.SetCellColor(3, 1, (byte)(Acom.Palette.BlockOffset(13) + 0));

			_lstInfo.AddRow(2, Tr("STR_HIT_RATIO"), Unicode.FormatPercentage(facility.HitRatio));
			_lstInfo.SetCellColor(4, 1, (byte)(Acom.Palette.BlockOffset(13) + 0));
		}

		CenterAllSurfaces();
	}
}
