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
// Ported from: src/Battlescape/BattleState.h, src/Battlescape/BattleState.cpp
#nullable disable

namespace Acom;

/// <summary>
/// This class sets the battlescape in a certain sub-state.
/// These states can be triggered by the player or the AI.
/// </summary>
public class BattleState
{
	protected BattlescapeGame _parent;
	protected BattleAction _action;

	/// <summary>Sets up a BattleState.</summary>
	/// <param name="parent">Pointer to the parent state.</param>
	/// <param name="action">Struct containing info about the action (copied, like the C++ value parameter).</param>
	public BattleState(BattlescapeGame parent, BattleAction action)
	{
		_parent = parent;
		_action = new BattleAction(action);
	}

	/// <summary>Sets up a BattleState.</summary>
	/// <param name="parent">Pointer to the parent state.</param>
	public BattleState(BattlescapeGame parent)
	{
		_parent = parent;
		_action = new BattleAction();
		_action.result = "";
		_action.targeting = false;
		_action.ClearTU();
	}

	/// <summary>Start the current BattleState.</summary>
	public virtual void Init()
	{
	}

	/// <summary>Called when the state gets popped out.</summary>
	public virtual void Deinit()
	{
	}

	/// <summary>Cancels the current BattleState.</summary>
	public virtual void Cancel()
	{
	}

	/// <summary>Runs any code the state needs to keep updating every game cycle.</summary>
	public virtual void Think()
	{
	}

	/// <summary>Gets the action result. Returns error messages or an empty string when everything went fine.</summary>
	public BattleAction Action => _action;
}
