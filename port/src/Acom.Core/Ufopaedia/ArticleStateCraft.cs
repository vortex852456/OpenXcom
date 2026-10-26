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
// Ported from: src/Ufopaedia/ArticleStateCraft.h, src/Ufopaedia/ArticleStateCraft.cpp
#nullable disable

using System.Text;

namespace Acom;

/// <summary>ArticleStateCraft has a caption, text, background image and a stats block.</summary>
public class ArticleStateCraft : ArticleState
{
	protected Text _txtTitle;
	protected Text _txtInfo;
	protected Text _txtStats;

	public ArticleStateCraft(ArticleDefinitionCraft article_defs, ArticleCommonState state) : base(article_defs.id, state)
	{
		ArticleDefinitionCraft defs = article_defs;
		RuleCraft craft = _game.Mod.GetCraft(defs.id, true);

		// add screen elements
		_txtTitle = new Text(210, 32, 5, 24);

		// Set palette
		if (defs.customPalette)
		{
			SetCustomPalette(_game.Mod.GetSurface(defs.image_id).Palette, Mod.UFOPAEDIA_CURSOR);
		}
		else
		{
			SetStandardPalette("PAL_UFOPAEDIA");
		}

		InitLayout();

		// add other elements
		Add(_txtTitle);

		// Set up objects
		_game.Mod.GetSurface(defs.image_id).BlitNShade(_bg, 0, 0);
		_btnOk.SetColor((byte)(Acom.Palette.BlockOffset(15) - 1));
		_btnPrev.SetColor((byte)(Acom.Palette.BlockOffset(15) - 1));
		_btnNext.SetColor((byte)(Acom.Palette.BlockOffset(15) - 1));
		_btnInfo.SetColor((byte)(Acom.Palette.BlockOffset(15) - 1));
		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		_txtTitle.SetColor((byte)(Acom.Palette.BlockOffset(14) + 15));
		_txtTitle.SetBig();
		_txtTitle.SetWordWrap(true);
		_txtTitle.SetText(Tr(defs.GetTitleForPage(_state.current_page)));

		_txtInfo = new Text(defs.rect_text.width, defs.rect_text.height, defs.rect_text.x, defs.rect_text.y);
		Add(_txtInfo);

		_txtInfo.SetColor((byte)(Acom.Palette.BlockOffset(14) + 15));
		_txtInfo.SetSecondaryColor((byte)(Acom.Palette.BlockOffset(15) + 4));
		_txtInfo.SetWordWrap(true);
		_txtInfo.SetScrollable(true);
		_txtInfo.SetText(Tr(defs.GetTextForPage(_state.current_page)));

		_txtStats = new Text(defs.rect_stats.width, defs.rect_stats.height, defs.rect_stats.x, defs.rect_stats.y);
		Add(_txtStats);

		_txtStats.SetColor((byte)(Acom.Palette.BlockOffset(14) + 15));
		_txtStats.SetSecondaryColor((byte)(Acom.Palette.BlockOffset(15) + 4));

		var ss = new StringBuilder();
		ss.Append(Tr("STR_MAXIMUM_SPEED_UC").Arg(Unicode.FormatNumber(craft.MaxSpeed))).Append('\n');
		ss.Append(Tr("STR_ACCELERATION").Arg(craft.Acceleration)).Append('\n');
		int range;
		switch (_game.Mod.PediaReplaceCraftFuelWithRangeType)
		{
			// Both max range alone and average range get rounded
			case 0:
			case 2:
				range = craft.CalculateRange(_game.Mod.PediaReplaceCraftFuelWithRangeType);
				if (range == -1)
				{
					ss.Append(Tr("STR_MAXIMUM_RANGE").Arg(Tr("STR_INFINITE_RANGE"))).Append('\n');
					break;
				}

				// Round the answer to
				if (range < 100)
				{
					// don't round if it's small!
				}
				else if (range < 1000)
				{
					// nearest 10 nautical miles
					range += 10 / 2;
					range -= range % 10;
				}
				else
				{
					// nearest 100 nautical miles
					range += 100 / 2;
					range -= range % 100;
				}
				ss.Append(Tr("STR_MAXIMUM_RANGE").Arg(Unicode.FormatNumber(range))).Append('\n');
				break;
			// Min-maxxers can fret over exact numbers
			case 1:
				if (craft.CalculateRange(0) == -1)
				{
					ss.Append(Tr("STR_MAXIMUM_RANGE").Arg(Tr("STR_INFINITE_RANGE"))).Append('\n');
					break;
				}
				ss.Append(Tr("STR_MINIMUM_RANGE").Arg(Unicode.FormatNumber(craft.CalculateRange(1)))).Append('\n');
				ss.Append(Tr("STR_MAXIMUM_RANGE").Arg(Unicode.FormatNumber(craft.CalculateRange(0)))).Append('\n');
				break;
			default:
				ss.Append(Tr("STR_FUEL_CAPACITY").Arg(Unicode.FormatNumber(craft.MaxFuel))).Append('\n');
				break;
		}
		ss.Append(Tr("STR_WEAPON_PODS").Arg(craft.Weapons)).Append('\n');
		ss.Append(Tr("STR_DAMAGE_CAPACITY_UC").Arg(Unicode.FormatNumber(craft.MaxDamage))).Append('\n');
		if (craft.MaxUnits == craft.MaxUnitsLimit)
		{
			ss.Append(Tr("STR_CARGO_SPACE").Arg(craft.MaxUnits)).Append('\n');
		}
		else
		{
			ss.Append(Tr("STR_CARGO_SPACE").Arg(FormattableString.Invariant($"{craft.MaxUnits}/{craft.MaxUnitsLimit}"))).Append('\n');
		}
		if (craft.Pilots > 0)
		{
			ss.Append(Tr("STR_COCKPIT_CAPACITY").Arg(craft.Pilots)).Append('\n');
		}
		if (craft.MaxVehiclesAndLargeSoldiers == craft.MaxVehiclesAndLargeSoldiersLimit)
		{
			ss.Append(Tr("STR_HWP_CAPACITY").Arg(craft.MaxVehiclesAndLargeSoldiers));
		}
		else
		{
			ss.Append(Tr("STR_HWP_CAPACITY").Arg(FormattableString.Invariant($"{craft.MaxVehiclesAndLargeSoldiers}/{craft.MaxVehiclesAndLargeSoldiersLimit}")));
		}
		_txtStats.SetText(ss.ToString());

		CenterAllSurfaces();
	}
}
