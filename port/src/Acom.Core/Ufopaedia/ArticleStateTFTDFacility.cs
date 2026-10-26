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
// Ported from: src/Ufopaedia/ArticleStateTFTDFacility.h, src/Ufopaedia/ArticleStateTFTDFacility.cpp
#nullable disable

namespace Acom;

/// <summary>TFTD-style article for base facilities.</summary>
public class ArticleStateTFTDFacility : ArticleStateTFTD
{
	protected TextList _lstInfo;

	public ArticleStateTFTDFacility(ArticleDefinitionTFTD defs, ArticleCommonState state) : base(defs, state)
	{
		_txtInfo.SetHeight(112);

		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		RuleBaseFacility facility = _game.Mod.GetBaseFacility(defs.id, true);

		_lstInfo = new TextList(150, 50, 168, 150);
		Add(_lstInfo);

		_lstInfo.SetColor(_listColor1);
		_lstInfo.SetColumns(2, 104, 46);
		_lstInfo.SetDot(true);

		int row = 0;
		if (facility.DefenseValue > 0)
		{
			_lstInfo.Y = _lstInfo.Y - 16;
			_txtInfo.SetHeight(_txtInfo.Height - 16);
			_lstInfo.AddRow(2, Tr("STR_DEFENSE_VALUE"), facility.DefenseValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
			_lstInfo.SetCellColor(row++, 1, _listColor2);

			_lstInfo.AddRow(2, Tr("STR_HIT_RATIO"), Unicode.FormatPercentage(facility.HitRatio));
			_lstInfo.SetCellColor(row++, 1, _listColor2);
		}
		_lstInfo.AddRow(2, Tr("STR_CONSTRUCTION_TIME"), Tr("STR_DAY", facility.BuildTime));
		_lstInfo.SetCellColor(row++, 1, _listColor2);

		_lstInfo.AddRow(2, Tr("STR_CONSTRUCTION_COST"), Unicode.FormatFunding(facility.BuildCost));
		_lstInfo.SetCellColor(row++, 1, _listColor2);

		_lstInfo.AddRow(2, Tr("STR_MAINTENANCE_COST"), Unicode.FormatFunding(facility.MonthlyCost));
		_lstInfo.SetCellColor(row++, 1, _listColor2);

		CenterAllSurfaces();
	}
}
