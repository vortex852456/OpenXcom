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
// Ported from: src/Mod/ArticleDefinition.h, src/Mod/ArticleDefinition.cpp
#nullable disable

namespace Acom;

/// <summary>Define article types.</summary>
public enum UfopaediaTypeId : uint
{
	UFOPAEDIA_TYPE_UNKNOWN = 0,
	UFOPAEDIA_TYPE_CRAFT = 1,
	UFOPAEDIA_TYPE_CRAFT_WEAPON = 2,
	UFOPAEDIA_TYPE_VEHICLE = 3,
	UFOPAEDIA_TYPE_ITEM = 4,
	UFOPAEDIA_TYPE_ARMOR = 5,
	UFOPAEDIA_TYPE_BASE_FACILITY = 6,
	UFOPAEDIA_TYPE_TEXTIMAGE = 7,
	UFOPAEDIA_TYPE_TEXT = 8,
	UFOPAEDIA_TYPE_UFO = 9,
	UFOPAEDIA_TYPE_TFTD = 10,
	UFOPAEDIA_TYPE_TFTD_CRAFT = 11,
	UFOPAEDIA_TYPE_TFTD_CRAFT_WEAPON = 12,
	UFOPAEDIA_TYPE_TFTD_VEHICLE = 13,
	UFOPAEDIA_TYPE_TFTD_ITEM = 14,
	UFOPAEDIA_TYPE_TFTD_ARMOR = 15,
	UFOPAEDIA_TYPE_TFTD_BASE_FACILITY = 16,
	UFOPAEDIA_TYPE_TFTD_USO = 17,
	UFOPAEDIA_TYPE_SOLDIER = 18,
	UFOPAEDIA_TYPE_UNIT = 19,
}

/// <summary>Definition of one sub page of ufopedia article (a C++ value type: copy with <see cref="Copy"/>).</summary>
public class ArticlePage
{
	public string title = "";
	public string text = "";
	public int ammoSlot;

	/// <summary>Copy of this page (C++ copy constructor).</summary>
	public ArticlePage Copy() => (ArticlePage)MemberwiseClone();
}

/// <summary>
/// ArticleDefinition is the base class for all article types.
/// This class is used to store all information about articles
/// required to generate an ArticleState from.
/// </summary>
public class ArticleDefinition
{
	public const int PEDIA_STATUS_NEW = 0;
	public const int PEDIA_STATUS_NORMAL = 1;
	public const int PEDIA_STATUS_HIDDEN = 2;
	public const int PEDIA_STATUSES = 3;

	public string id = "";
	public string section = "";
	public List<string> _requires = [];
	public List<string> disabledBy = [];
	public bool customPalette;
	public bool hiddenCommendation;

	protected UfopaediaTypeId _type_id;
	protected List<ArticlePage> _pages = [];

	private int _listOrder;

	/// <summary>Constructor (protected, so this class cannot be instantiated directly).</summary>
	/// <param name="type_id">Article type of this instance.</param>
	protected ArticleDefinition(UfopaediaTypeId type_id)
	{
		customPalette = false;
		hiddenCommendation = false;
		_type_id = type_id;
		_listOrder = 0;
		_pages.Add(new ArticlePage());
	}

	/// <summary>Gets the article definition type. (Text, TextImage, Craft, ...)</summary>
	public UfopaediaTypeId Type => _type_id;

	/// <summary>Loads the article definition from a YAML file.</summary>
	/// <param name="reader">YAML node.</param>
	/// <param name="listOrder">The list weight for this article.</param>
	public virtual void Load(YamlNodeReader reader, int listOrder)
	{
		reader.TryRead("id", ref id);
		_pages[0].title = id;
		reader.TryRead("section", ref section);
		reader.TryRead("requires", ref _requires);
		reader.TryRead("disabledBy", ref disabledBy);
		reader.TryRead("hiddenCommendation", ref hiddenCommendation);
		//_type_id = (UfopaediaTypeId)node["type_id"].as<int>(_type_id);
		reader.TryRead("listOrder", ref _listOrder);
		if (_listOrder == 0)
		{
			_listOrder = listOrder;
		}

		void LoadPage(int offset, YamlNodeReader r)
		{
			if (r)
			{
				r.TryRead("title", ref _pages[offset].title);
				r.TryRead("text", ref _pages[offset].text);
				RuleItem.LoadAmmoSlotChecked(ref _pages[offset].ammoSlot, r["ammoSlot"], id);
			}
		}

		LoadPage(0, reader);
		if (reader["pages"] is { IsValid: true } pagesNode)
		{
			if (pagesNode.IsSeq)
			{
				int size = pagesNode.ChildrenCount;
				ArticlePage firstCopy = _pages[0].Copy();
				// std::vector::resize: all new pages are copy of old first page
				int newSize = Math.Max(1, size);
				if (_pages.Count > newSize)
				{
					_pages.RemoveRange(newSize, _pages.Count - newSize);
				}
				while (_pages.Count < newSize)
				{
					_pages.Add(firstCopy.Copy());
				}
				for (int i = 0; i < size; ++i)
				{
					LoadPage(i, pagesNode[i]);
				}
			}
			else
			{
				throw new InvalidOperationException("Unsupported type of node 'pages' for Article '" + id + "'");
			}
		}
	}

	/// <summary>Gets the list weight of the article.</summary>
	public int ListOrder => _listOrder;

	/// <summary>Get main title of page in ufopedia.</summary>
	public string MainTitle => _pages[0].title;

	/// <summary>Return number of pages.</summary>
	public int NumberOfPages => _pages.Count;

	public string GetTitleForPage(int page)
	{
		if ((uint)page >= (uint)_pages.Count)
		{
			throw new InvalidOperationException("Access to wrong page for article '" + id + "'");
		}
		return _pages[page].title;
	}

	public string GetTextForPage(int page)
	{
		if ((uint)page >= (uint)_pages.Count)
		{
			throw new InvalidOperationException("Access to wrong page for article '" + id + "'");
		}
		return _pages[page].text;
	}

	public int GetAmmoSlotForPage(int page)
	{
		if ((uint)page >= (uint)_pages.Count)
		{
			throw new InvalidOperationException("Access to wrong page for article '" + id + "'");
		}
		return _pages[page].ammoSlot;
	}

	public int GetAmmoSlotPrevUsageForPage(int page)
	{
		int ammoSlot = GetAmmoSlotForPage(page);
		int used = 0;
		for (int i = 0; i < page; ++i)
		{
			if (_pages[i].ammoSlot == ammoSlot)
			{
				++used;
			}
		}
		return used;
	}
}

public class ArticleDefinitionRect
{
	public int x;
	public int y;
	public int width;
	public int height;

	public ArticleDefinitionRect()
	{
		x = 0;
		y = 0;
		width = 0;
		height = 0;
	}

	/// <summary>Sets the rectangle parameters in a function.</summary>
	public void Set(int set_x, int set_y, int set_width, int set_height)
	{
		x = set_x;
		y = set_y;
		width = set_width;
		height = set_height;
	}

	/// <summary>YAML: map with x, y, width and height; missing keys keep their value (C++ read overload, deserialization only).</summary>
	public static bool YamlRead(YamlNodeReader reader, ref ArticleDefinitionRect val)
	{
		val ??= new ArticleDefinitionRect();
		reader.TryRead("x", ref val.x);
		reader.TryRead("y", ref val.y);
		reader.TryRead("width", ref val.width);
		reader.TryRead("height", ref val.height);
		return true;
	}
}

/// <summary>
/// ArticleDefinitionUnit defines articles for unit types, e.g. STR_SECTOID_LEADER.
/// They have a large background image, a stats block, an armor block and a description positioned differently.
/// </summary>
public class ArticleDefinitionUnit : ArticleDefinition
{
	public int unit_mode;
	public string image_id = "";
	public ArticleDefinitionRect rect_stats = new();
	public ArticleDefinitionRect rect_armor = new();
	public ArticleDefinitionRect rect_text = new();

	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionUnit() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_UNIT)
	{
		unit_mode = 0;
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
		reader.TryRead("unit_mode", ref unit_mode);
		reader.TryRead("image_id", ref image_id);
		if (image_id.Contains("_CPAL", StringComparison.Ordinal))
			customPalette = true;
		reader.TryRead("rect_stats", ref rect_stats);
		reader.TryRead("rect_armor", ref rect_armor);
		reader.TryRead("rect_text", ref rect_text);
	}
}

/// <summary>
/// ArticleDefinitionSoldier defines articles for soldier types, e.g. STR_SOLDIER.
/// They have a large background image, a stats block and a description positioned differently.
/// </summary>
public class ArticleDefinitionSoldier : ArticleDefinition
{
	public int psi_skill_mode;
	public string image_id = "";
	public ArticleDefinitionRect rect_stats = new();
	public ArticleDefinitionRect rect_text = new();

	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionSoldier() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_SOLDIER)
	{
		psi_skill_mode = 0;
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
		reader.TryRead("psi_skill_mode", ref psi_skill_mode);
		reader.TryRead("image_id", ref image_id);
		if (image_id.Contains("_CPAL", StringComparison.Ordinal))
			customPalette = true;
		reader.TryRead("rect_stats", ref rect_stats);
		reader.TryRead("rect_text", ref rect_text);
	}
}

/// <summary>
/// ArticleDefinitionCraft defines articles for craft, e.g. SKYRANGER.
/// They have a large background image, a stats block and a description positioned differently.
/// </summary>
public class ArticleDefinitionCraft : ArticleDefinition
{
	public string image_id = "";
	public ArticleDefinitionRect rect_stats = new();
	public ArticleDefinitionRect rect_text = new();

	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionCraft() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT)
	{
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
		reader.TryRead("image_id", ref image_id);
		if (image_id.Contains("_CPAL", StringComparison.Ordinal))
			customPalette = true;
		reader.TryRead("rect_stats", ref rect_stats);
		reader.TryRead("rect_text", ref rect_text);
	}
}

/// <summary>
/// ArticleDefinitionCraftWeapon defines articles for craft weapons, e.g. STINGRAY, AVALANCHE.
/// They have a large background image and a stats block.
/// </summary>
public class ArticleDefinitionCraftWeapon : ArticleDefinition
{
	public string image_id = "";

	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionCraftWeapon() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT_WEAPON)
	{
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
		reader.TryRead("image_id", ref image_id);
		if (image_id.Contains("_CPAL", StringComparison.Ordinal))
			customPalette = true;
	}
}

/// <summary>ArticleDefinitionText defines articles with only text, e.g. ALIEN RESEARCH.</summary>
public class ArticleDefinitionText : ArticleDefinition
{
	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionText() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_TEXT)
	{
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
	}
}

/// <summary>
/// ArticleDefinitionTextImage defines articles with text on the left and
/// an image on the right side of the screen, e.g. ALIEN LIFEFORMS, UFO COMPONENTS.
/// </summary>
public class ArticleDefinitionTextImage : ArticleDefinition
{
	public string image_id = "";
	public int text_width;
	public bool align_bottom;
	public ArticleDefinitionRect rect_text = new();

	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionTextImage() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_TEXTIMAGE)
	{
		text_width = 0;
		align_bottom = false;
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
		reader.TryRead("image_id", ref image_id);
		if (image_id.Contains("_CPAL", StringComparison.Ordinal))
			customPalette = true;
		reader.TryRead("text_width", ref text_width);
		reader.TryRead("align_bottom", ref align_bottom);
		reader.TryRead("rect_text", ref rect_text);
	}
}

/// <summary>
/// ArticleDefinitionTFTD defines TFTD-style articles with text on the left and
/// an image on the right side of the screen.
/// </summary>
public class ArticleDefinitionTFTD : ArticleDefinition
{
	public string image_id = "";
	public int text_width;
	public string weapon = "";

	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionTFTD() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD)
	{
		text_width = 0;
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
		reader.TryRead("type_id", ref _type_id);
		reader.TryRead("image_id", ref image_id);
		if (image_id.Contains("_CPAL", StringComparison.Ordinal))
			customPalette = true;
		text_width = reader["text_width"].ReadVal(157); // 95% of these won't need to be defined, so let's give it a default
		reader.TryRead("weapon", ref weapon);
	}
}

/// <summary>
/// ArticleDefinitionBaseFacility defines articles for base facilities, e.g. Access lift.
/// They have an image (found in BASEBITS.PCK), a stats block and a description.
/// </summary>
public class ArticleDefinitionBaseFacility : ArticleDefinition
{
	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionBaseFacility() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_BASE_FACILITY)
	{
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
	}
}

/// <summary>
/// ArticleDefinitionItem defines articles for all Items, e.g. Weapons, Ammo, Equipment, etc.
/// They have an image (found in BIGOBS.PCK), an optional stats block, maybe ammo and a description.
/// </summary>
public class ArticleDefinitionItem : ArticleDefinition
{
	public string weapon = "";

	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionItem() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_ITEM)
	{
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
		reader.TryRead("weapon", ref weapon);
	}
}

/// <summary>
/// ArticleDefinitionUfo defines articles for UFOs, e.g. Small Scout, Terror Ship, etc.
/// They have an image (found in INTERWIN.DAT), a stats block and a description.
/// </summary>
public class ArticleDefinitionUfo : ArticleDefinition
{
	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionUfo() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_UFO)
	{
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
	}
}

/// <summary>
/// ArticleDefinitionArmor defines articles for Armor, e.g. Personal Armor, Flying Suit, etc.
/// They have an image (found in MAN_*.SPK) and a stats block.
/// </summary>
public class ArticleDefinitionArmor : ArticleDefinition
{
	public string image_id = "";

	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionArmor() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_ARMOR)
	{
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
		reader.TryRead("image_id", ref image_id);
		if (image_id.Contains("_CPAL", StringComparison.Ordinal))
			customPalette = true;
	}
}

/// <summary>
/// ArticleDefinitionVehicle defines articles for Vehicles, e.g. Tanks, etc.
/// They have a text description and a stats block.
/// </summary>
public class ArticleDefinitionVehicle : ArticleDefinition
{
	public string image_id = "";
	public string weapon = "";

	/// <summary>Constructor (only setting type of base class).</summary>
	public ArticleDefinitionVehicle() : base(UfopaediaTypeId.UFOPAEDIA_TYPE_VEHICLE)
	{
	}

	public override void Load(YamlNodeReader reader, int listOrder)
	{
		base.Load(reader, listOrder);
		reader.TryRead("image_id", ref image_id);
		if (image_id.Contains("_CPAL", StringComparison.Ordinal))
			customPalette = true;
		reader.TryRead("weapon", ref weapon);
	}
}
