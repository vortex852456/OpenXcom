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
// Ported from: src/Engine/OptionInfo.h, src/Engine/OptionInfo.cpp

using System.Globalization;

namespace Acom;

public enum OptionType
{
	OPTION_BOOL,
	OPTION_INT,
	OPTION_STRING,
	OPTION_KEY,
}

public enum OptionOwner
{
	OPTION_OXC,
	OPTION_OXCE,
	OPTION_OTHER,
	OPTION_OWNER_MAX,
}

/// <summary>
/// Ties metadata to particular options to help in serializing and stuff.
/// The option variable must already exist; this info accesses it through getter/setter delegates
/// (the C++ version stores a pointer).
/// </summary>
public sealed class OptionInfo
{
	private readonly Func<object> _get;
	private readonly Action<object> _set;
	private readonly object _def;

	private OptionInfo(OptionOwner owner, string id, OptionType type, Func<object> get, Action<object> set, object def, string desc, string cat)
	{
		Owner = owner;
		Id = id;
		Type = type;
		_get = get;
		_set = set;
		_def = def;
		Description = desc;
		Category = cat;
	}

	/// <summary>Creates info for a boolean option.</summary>
	public OptionInfo(OptionOwner owner, string id, Func<bool> get, Action<bool> set, bool def, string desc = "", string cat = "")
		: this(owner, id, OptionType.OPTION_BOOL, () => get(), v => set((bool)v), def, desc, cat)
	{
	}

	/// <summary>Creates info for an integer option.</summary>
	public OptionInfo(OptionOwner owner, string id, Func<int> get, Action<int> set, int def, string desc = "", string cat = "")
		: this(owner, id, OptionType.OPTION_INT, () => get(), v => set((int)v), def, desc, cat)
	{
	}

	/// <summary>Creates info for a keyboard key option.</summary>
	public OptionInfo(OptionOwner owner, string id, Func<SDLKey> get, Action<SDLKey> set, SDLKey def, string desc = "", string cat = "")
		: this(owner, id, OptionType.OPTION_KEY, () => get(), v => set((SDLKey)v), def, desc, cat)
	{
	}

	/// <summary>Creates info for a string option.</summary>
	public OptionInfo(OptionOwner owner, string id, Func<string> get, Action<string> set, string def, string desc = "", string cat = "")
		: this(owner, id, OptionType.OPTION_STRING, () => get(), v => set((string)v), def, desc, cat)
	{
	}

	public string Id { get; }
	public OptionType Type { get; }
	public OptionOwner Owner { get; }
	public string Description { get; }
	public string Category { get; }

	/// <summary>Current value of a boolean option.</summary>
	public bool AsBool
	{
		get => (bool)Checked(OptionType.OPTION_BOOL, "a boolean");
		set => SetChecked(OptionType.OPTION_BOOL, "a boolean", value);
	}

	/// <summary>Current value of an integer option.</summary>
	public int AsInt
	{
		get => (int)Checked(OptionType.OPTION_INT, "an integer");
		set => SetChecked(OptionType.OPTION_INT, "an integer", value);
	}

	/// <summary>Current value of a key option.</summary>
	public SDLKey AsKey
	{
		get => (SDLKey)Checked(OptionType.OPTION_KEY, "a key");
		set => SetChecked(OptionType.OPTION_KEY, "a key", value);
	}

	/// <summary>Current value of a string option.</summary>
	public string AsString
	{
		get => (string)Checked(OptionType.OPTION_STRING, "a string");
		set => SetChecked(OptionType.OPTION_STRING, "a string", value);
	}

	private object Checked(OptionType type, string what)
	{
		if (Type != type)
			throw new InvalidOperationException($"{Id} is not {what}!");
		return _get();
	}

	private void SetChecked(OptionType type, string what, object value)
	{
		if (Type != type)
			throw new InvalidOperationException($"{Id} is not {what}!");
		_set(value);
	}

	/// <summary>Loads the option from a YAML map; a missing entry resets the option to its default.</summary>
	public void Load(YamlNodeReader reader)
	{
		var node = reader[Id];
		switch (Type)
		{
			case OptionType.OPTION_BOOL:
				_set(node.ReadVal((bool)_def));
				break;
			case OptionType.OPTION_INT:
				_set(node.ReadVal((int)_def));
				break;
			case OptionType.OPTION_KEY:
				var key = (SDLKey)node.ReadVal((int)(SDLKey)_def);
				// Modifier keys can't be bound on their own
				if (key is SDLKey.SDLK_LSHIFT or SDLKey.SDLK_LALT or SDLKey.SDLK_LCTRL or SDLKey.SDLK_RSHIFT or SDLKey.SDLK_RALT or SDLKey.SDLK_RCTRL)
					key = SDLKey.SDLK_UNKNOWN;
				_set(key);
				break;
			case OptionType.OPTION_STRING:
				_set(node.ReadVal((string)_def));
				break;
		}
	}

	/// <summary>Loads the option from a key/value map (command line arguments).</summary>
	public void Load(IReadOnlyDictionary<string, string> map, bool makeLowercase)
	{
		ArgumentNullException.ThrowIfNull(map);
		string id = makeLowercase ? Id.ToLowerInvariant() : Id;
		if (!map.TryGetValue(id, out var value))
			return;
		switch (Type)
		{
			case OptionType.OPTION_BOOL:
				// std::boolalpha accepts exactly "true" and "false"
				if (value is "true" or "false")
					_set(value == "true");
				else
					Logger.Warning($"Option {Id}: '{value}' is not true/false, ignored");
				break;
			case OptionType.OPTION_INT:
				_set(ParseLeadingInt(value));
				break;
			case OptionType.OPTION_KEY:
				_set((SDLKey)ParseLeadingInt(value));
				break;
			case OptionType.OPTION_STRING:
				_set(value);
				break;
		}
	}

	/// <summary>Like <c>std::stringstream &gt;&gt; int</c>: reads a leading integer, 0 on failure.</summary>
	private static int ParseLeadingInt(string value)
	{
		var s = value.AsSpan().TrimStart();
		int len = 0;
		if (len < s.Length && (s[len] == '-' || s[len] == '+'))
			len++;
		while (len < s.Length && char.IsAsciiDigit(s[len]))
			len++;
		return int.TryParse(s[..len], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int i) ? i : 0;
	}

	/// <summary>Writes the option to a YAML map.</summary>
	public void Save(YamlNodeWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		switch (Type)
		{
			case OptionType.OPTION_BOOL:
				writer.Write(Id, (bool)_get());
				break;
			case OptionType.OPTION_INT:
				writer.Write(Id, (int)_get());
				break;
			case OptionType.OPTION_KEY:
				writer.Write(Id, (int)(SDLKey)_get());
				break;
			case OptionType.OPTION_STRING:
				writer.Write(Id, (string)_get());
				break;
		}
	}

	/// <summary>Resets the option to its default value.</summary>
	public void Reset() => _set(_def);
}
