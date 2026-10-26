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
// Ported from: src/Ufopaedia/ArticleStateTFTDUso.h, src/Ufopaedia/ArticleStateTFTDUso.cpp
#nullable disable

namespace Acom;

/// <summary>TFTD-style article for alien craft (USOs).</summary>
public class ArticleStateTFTDUso : ArticleStateTFTD
{
	protected TextList _lstInfo;

	public ArticleStateTFTDUso(ArticleDefinitionTFTD defs, ArticleCommonState state) : base(defs, state)
	{
		_txtInfo.SetHeight(112);

		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		RuleUfo ufo = _game.Mod.GetUfo(defs.id, true);

		_lstInfo = new TextList(150, 50, 168, 142);
		Add(_lstInfo);

		_lstInfo.SetColor(_listColor1);
		_lstInfo.SetColumns(2, 95, 55);
		_lstInfo.SetDot(true);

		_lstInfo.AddRow(2, Tr("STR_DAMAGE_CAPACITY"), Unicode.FormatNumber(ufo.Stats.damageMax));

		_lstInfo.AddRow(2, Tr("STR_WEAPON_POWER"), Unicode.FormatNumber(ufo.WeaponPower));

		_lstInfo.AddRow(2, Tr("STR_WEAPON_RANGE"), Tr("STR_KILOMETERS").Arg(ufo.WeaponRange));

		_lstInfo.AddRow(2, Tr("STR_MAXIMUM_SPEED"), Tr("STR_KNOTS").Arg(Unicode.FormatNumber(ufo.Stats.speedMax)));

		for (int i = 0; i != 4; ++i)
		{
			_lstInfo.SetCellColor(i, 1, _listColor2);
		}

		CenterAllSurfaces();
	}
}
