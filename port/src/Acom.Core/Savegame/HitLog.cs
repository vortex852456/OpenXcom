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
// Ported from: src/Savegame/HitLog.h, src/Savegame/HitLog.cpp
#nullable disable

using System.Text;

namespace Acom;

public enum HitLogEntryType
{
	HITLOG_EMPTY = 0,
	HITLOG_NEW_TURN = 1,
	HITLOG_NEW_TURN_WITH_MESSAGE = 2,
	HITLOG_PLAYER_FIRING = 3,
	HITLOG_REACTION_FIRE = 4,
	HITLOG_NEW_SHOT = 5,
	HITLOG_NO_DAMAGE = 6,
	HITLOG_SMALL_DAMAGE = 7,
	HITLOG_BIG_DAMAGE = 8,
}

/// <summary>Text log of the shots and hits of the current turn (shown in the battlescape).</summary>
public class HitLog
{
	private readonly StringBuilder _ss = new();
	private readonly List<string> _turnDiary = [];
	private readonly string _newTurn, _reactionFire, _newShot, _noDamage, _smallDamage, _bigDamage;
	private HitLogEntryType _lastEventType;
	private UnitFaction _lastFaction;
	private string _lastPlayerWeapon = "";

	/// <summary>Creates a new hit log.</summary>
	public HitLog(Language lang)
	{
		ArgumentNullException.ThrowIfNull(lang);
		_lastEventType = HitLogEntryType.HITLOG_EMPTY;
		_lastFaction = UnitFaction.FACTION_PLAYER;
		// cache
		_newTurn = lang.GetString("STR_HIT_LOG_NEW_TURN");
		_reactionFire = lang.GetString("STR_HIT_LOG_REACTION_FIRE");
		_newShot = lang.GetString("STR_HIT_LOG_NEW_BULLET");
		_noDamage = lang.GetString("STR_HIT_LOG_NO_DAMAGE");
		_smallDamage = lang.GetString("STR_HIT_LOG_SMALL_DAMAGE");
		_bigDamage = lang.GetString("STR_HIT_LOG_BIG_DAMAGE");
	}

	/// <summary>Clears the hit log. And updates the turn diary.</summary>
	private void ClearHitLog(bool resetTurnDiary, bool ignoreLastEntry = false)
	{
		if (resetTurnDiary)
		{
			_turnDiary.Clear();
		}
		else if (!ignoreLastEntry)
		{
			_turnDiary.Add(GetHitLogText(true));
		}

		_ss.Clear();
	}

	/// <summary>Appends a given entry to the hit log.</summary>
	/// <param name="type">Type of hit log entry.</param>
	/// <param name="faction">Faction of the actor.</param>
	public void AppendToHitLog(HitLogEntryType type, UnitFaction faction)
	{
		switch (type)
		{
			case HitLogEntryType.HITLOG_NEW_TURN:
				ClearHitLog(true);
				_ss.Append(_newTurn);
				break;
			case HitLogEntryType.HITLOG_REACTION_FIRE:
				if (_lastEventType != HitLogEntryType.HITLOG_REACTION_FIRE) // don't produce duplicates
				{
					ClearHitLog(false);
					_ss.Append(_reactionFire);
					_ss.Append("\n\n");
				}
				break;
			case HitLogEntryType.HITLOG_NEW_SHOT:
				if (_lastFaction != faction && faction == UnitFaction.FACTION_PLAYER)
				{
					// player continues shooting (without selecting a weapon) after enemy reaction fire
					ClearHitLog(false);
					_ss.Append(_lastPlayerWeapon); // this is why we needed to remember it :)
					_ss.Append("\n\n");
				}
				_ss.Append(_newShot);
				break;
			case HitLogEntryType.HITLOG_NO_DAMAGE:
				_ss.Append(_noDamage);
				break;
			case HitLogEntryType.HITLOG_SMALL_DAMAGE:
				_ss.Append(_smallDamage);
				break;
			case HitLogEntryType.HITLOG_BIG_DAMAGE:
				_ss.Append(_bigDamage);
				break;
			default:
				break;
		}

		_lastEventType = type;
		_lastFaction = faction;
	}

	/// <summary>Appends a given entry with text to the hit log.</summary>
	/// <param name="type">Type of hit log entry.</param>
	/// <param name="faction">Faction of the actor.</param>
	/// <param name="text">Text to append.</param>
	public void AppendToHitLog(HitLogEntryType type, UnitFaction faction, string text)
	{
		switch (type)
		{
			case HitLogEntryType.HITLOG_NEW_TURN_WITH_MESSAGE:
				ClearHitLog(true);
				_ss.Append(text);
				break;
			case HitLogEntryType.HITLOG_PLAYER_FIRING:
				ClearHitLog(false, _lastEventType == HitLogEntryType.HITLOG_PLAYER_FIRING); // don't produce duplicates and irrelevant diary entries
				_lastPlayerWeapon = text; // remember this, we may need it again if we are interrupted by reaction fire
				_ss.Append(_lastPlayerWeapon);
				_ss.Append("\n\n");
				break;
			default:
				break;
		}

		_lastEventType = type;
		_lastFaction = faction;
	}

	/// <summary>Gets the hit log text.</summary>
	/// <param name="convert">Convert line breaks to spaces?</param>
	public string GetHitLogText(bool convert = false)
	{
		if (convert)
		{
			return _ss.ToString().Replace('\n', ' ');
		}
		return _ss.ToString();
	}

	/// <summary>Gets the turn diary.</summary>
	public List<string> TurnDiary => _turnDiary;
}
