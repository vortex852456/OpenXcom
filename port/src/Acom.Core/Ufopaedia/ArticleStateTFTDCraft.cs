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
// Ported from: src/Ufopaedia/ArticleStateTFTDCraft.h, src/Ufopaedia/ArticleStateTFTDCraft.cpp
#nullable disable

using System.Text;

namespace Acom;

/// <summary>TFTD-style article for X-COM craft.</summary>
public class ArticleStateTFTDCraft : ArticleStateTFTD
{
	protected Text _txtStats;

	public ArticleStateTFTDCraft(ArticleDefinitionTFTD defs, ArticleCommonState state) : base(defs, state)
	{
		_txtInfo.SetHeight(80);

		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		RuleCraft craft = _game.Mod.GetCraft(defs.id, true);
		_txtStats = new Text(131, 56, 187, 116);
		Add(_txtStats);

		_txtStats.SetColor(_textColor);
		_txtStats.SetSecondaryColor(_textColor2);

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
