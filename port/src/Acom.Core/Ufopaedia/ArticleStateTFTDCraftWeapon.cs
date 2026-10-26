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
// Ported from: src/Ufopaedia/ArticleStateTFTDCraftWeapon.h, src/Ufopaedia/ArticleStateTFTDCraftWeapon.cpp
#nullable disable

namespace Acom;

/// <summary>TFTD-style article for craft weapons.</summary>
public class ArticleStateTFTDCraftWeapon : ArticleStateTFTD
{
	protected TextList _lstInfo;

	public ArticleStateTFTDCraftWeapon(ArticleDefinitionTFTD defs, ArticleCommonState state) : base(defs, state)
	{
		_txtInfo.SetHeight(88);

		_btnInfo.Visible = _game.Mod.ShowPediaInfoButton;

		RuleCraftWeapon weapon = _game.Mod.GetCraftWeapon(defs.id, true);
		CraftWeaponCategory category = CraftWeaponCategory.CWC_WEAPON;
		int offset = 0;
		if (weapon.HidePediaInfo)
		{
			if (weapon.TractorBeamPower > 0)
			{
				category = CraftWeaponCategory.CWC_TRACTOR_BEAM;
				offset = 24; // 3 * 8
			}
			else
			{
				category = CraftWeaponCategory.CWC_EQUIPMENT;
				offset = 0;
			}
		}

		_lstInfo = new TextList(150, 50 - offset, 168, 126 + offset);
		Add(_lstInfo);

		_lstInfo.Visible = category != CraftWeaponCategory.CWC_EQUIPMENT;
		_lstInfo.SetColor(_listColor1);
		_lstInfo.SetColumns(2, 100, 68); // deliberately making this wider than the original to account for finish.
		_lstInfo.SetDot(true);

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
