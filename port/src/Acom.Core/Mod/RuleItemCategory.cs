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
// Ported from: src/Mod/RuleItemCategory.h, src/Mod/RuleItemCategory.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents an item category.
/// Contains info about list order.
/// </summary>
public class RuleItemCategory
{
	private readonly string _type;
	private string _replaceBy = "";
	private bool _hidden;
	private int _listOrder;
	private readonly List<string> _invOrder = [];

	/// <summary>Creates a blank ruleset for a certain item category.</summary>
	public RuleItemCategory(string type, int listOrder)
	{
		_type = type;
		_hidden = false;
		_listOrder = listOrder;
	}

	/// <summary>Loads the item category from YAML.</summary>
	public void Load(YamlNodeReader reader, Mod mod)
	{
		ArgumentNullException.ThrowIfNull(mod);
		var parent = reader["refNode"];
		if (parent)
		{
			Load(parent, mod);
		}

		reader.TryRead("replaceBy", ref _replaceBy);
		reader.TryRead("hidden", ref _hidden);

		reader.TryRead("listOrder", ref _listOrder);
		mod.LoadUnorderedNames(_type, _invOrder, reader["invOrder"]);
	}

	/// <summary>Gets the item category type. Each category has a unique type.</summary>
	public string Type { get => _type; }
	/// <summary>Gets the item category type, which should be used instead of this one.</summary>
	public string ReplaceBy { get => _replaceBy; }
	/// <summary>Indicates whether the category is hidden or visible.</summary>
	public bool IsHidden { get => _hidden; }
	/// <summary>Gets the list weight for this item category.</summary>
	public int ListOrder { get => _listOrder; }
	/// <summary>Gets the inventory slot order to be used for auto-equip and ctrl-click-equip.</summary>
	public List<string> InvOrder { get => _invOrder; }
}
