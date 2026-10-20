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
// Ported from: src/Savegame/SoldierAvatar.h, src/Savegame/SoldierAvatar.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Represents a soldier's avatar/paperdoll.
/// Avatar is a combination of soldier's gender, look and look variant.
/// </summary>
public class SoldierAvatar
{
	private readonly string _avatarName = "";
	private readonly SoldierGender _gender;
	private readonly SoldierLook _look;
	private readonly int _lookVariant;

	/// <summary>Creates a new empty avatar.</summary>
	public SoldierAvatar()
	{
		_gender = SoldierGender.GENDER_MALE;
		_look = SoldierLook.LOOK_BLONDE;
		_lookVariant = 0;
	}

	/// <summary>Creates a new avatar.</summary>
	public SoldierAvatar(string avatarName, SoldierGender gender, SoldierLook look, int lookVariant)
	{
		_avatarName = avatarName;
		_gender = gender;
		_look = look;
		_lookVariant = lookVariant;
	}

	/// <summary>Returns the avatar's name (code for translation).</summary>
	public string AvatarName => _avatarName;

	/// <summary>Returns the avatar's first component (gender).</summary>
	public SoldierGender Gender => _gender;

	/// <summary>Returns the avatar's second component (look).</summary>
	public SoldierLook Look => _look;

	/// <summary>Returns the avatar's third component (lookVariant).</summary>
	public int LookVariant => _lookVariant;
}
