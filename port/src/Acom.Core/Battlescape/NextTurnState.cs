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
// Ported from: src/Battlescape/NextTurnState.h, src/Battlescape/NextTurnState.cpp
#nullable disable

using System.Text;
using static Acom.MapBlockFilterType;
using static Acom.UnitBodyPart;
using static Acom.UnitFaction;
using static Acom.UnitSide;

namespace Acom;

/// <summary>
/// Screen which announces the next turn.
/// </summary>
public class NextTurnState : State
{
	private const int NEXT_TURN_DELAY = 500;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtTurn, _txtSide, _txtMessage, _txtMessage2, _txtMessage3, _txtMessageReinforcements;
	private readonly TextButton _btnBriefingReinforcements;
	private readonly SavedBattleGame _battleGame;
	private readonly BattlescapeState _state;
	private readonly Timer _timer;
	private readonly Surface _bg;
	private readonly int _currentTurn;
	private bool _showBriefing;
	private BriefingData _customBriefing = new();
	private List<List<int>> _compliantBlocksMap = [];
	private readonly List<Node> _compliantNodesList = [];
	private readonly List<Position> _compliantBlocksList = [];

	/// <summary>
	/// Initializes all the elements in the Next Turn screen.
	/// </summary>
	/// <param name="battleGame">Pointer to the saved game.</param>
	/// <param name="state">Pointer to the Battlescape state.</param>
	public NextTurnState(SavedBattleGame battleGame, BattlescapeState state)
	{
		ArgumentNullException.ThrowIfNull(battleGame);
		ArgumentNullException.ThrowIfNull(state);
		_battleGame = battleGame;
		_state = state;
		_timer = null;
		_currentTurn = 0;
		_showBriefing = false;

		if (_battleGame.IsPreview)
		{
			// skip everything, go straight to init()
			return;
		}

		// set random hidden movement/next turn background for this turn
		if (_battleGame.Side == FACTION_PLAYER)
		{
			bool change = false;
			if (Options.oxceHiddenMovementBackgroundChangeFrequency > 0)
			{
				if (_battleGame.Turn % Options.oxceHiddenMovementBackgroundChangeFrequency == 0)
				{
					change = true;
				}
			}
			if (change)
			{
				_battleGame.SetRandomHiddenMovementBackground(_game.Mod);
				state.Map.RefreshHiddenMovementBackground();
			}
		}

		// reset alien/civilian quick mode
		if (_battleGame.Side == FACTION_PLAYER)
		{
			if (Options.battleAlienSpeedOrig != -1)
			{
				Options.battleAlienSpeed = Options.battleAlienSpeedOrig;
				Options.battleAlienSpeedOrig = -1;
			}
		}

		_currentTurn = _battleGame.Turn < 1 ? 1 : _battleGame.Turn;

		// Create objects
		int y = state.Map.MessageY;

		_window = new Window(this, 320, 200, 0, 0);
		_txtMessageReinforcements = new Text(320, 33, 0, 8);
		_btnBriefingReinforcements = new TextButton(120, 16, 100, 45);
		_txtTitle = new Text(320, 17, 0, 68);
		_txtTurn = new Text(320, 17, 0, 92);
		_txtSide = new Text(320, 17, 0, 108);
		_txtMessage = new Text(320, 17, 0, 132);
		_txtMessage2 = new Text(320, 33, 0, 156);
		_txtMessage3 = new Text(320, 17, 0, 172);
		// as in C++: the height is the screen width too
		_bg = new Surface(_game.Screen.Width, _game.Screen.Width, 0, 0);

		// Set palette
		battleGame.SetPaletteByDepth(this);

		Add(_bg);
		Add(_window);
		Add(_txtMessageReinforcements, "messageWindows", "battlescape");
		Add(_btnBriefingReinforcements, "messageWindowButtons", "battlescape");
		Add(_txtTitle, "messageWindows", "battlescape");
		Add(_txtTurn, "messageWindows", "battlescape");
		Add(_txtSide, "messageWindows", "battlescape");
		Add(_txtMessage, "messageWindows", "battlescape");
		Add(_txtMessage2, "messageWindows", "battlescape");
		Add(_txtMessage3, "messageWindows", "battlescape");

		CenterAllSurfaces();

		_bg.X = 0;
		_bg.Y = 0;
		var rect = new SDL_Rect(0, 0, _bg.Width, _bg.Height);

		// Note: un-hardcoded the color from 15 to ruleset value, default 15
		int bgColor = 15;
		RuleEnviroEffects sc = _battleGame.EnviroEffects;
		if (sc != null)
		{
			bgColor = sc.MapBackgroundColor;
		}
		_bg.DrawRect(ref rect, (byte)(Acom.Palette.BlockOffset(0) + bgColor));
		// make this screen line up with the hidden movement screen
		_window.Y = y;
		_txtMessageReinforcements.Y = y + 8;
		_btnBriefingReinforcements.Y = y + 45;
		_txtTitle.Y = y + 68;
		_txtTurn.Y = y + 92;
		_txtSide.Y = y + 108;
		_txtMessage.Y = y + 132;
		_txtMessage2.Y = y + 156;
		_txtMessage3.Y = y + 172;

		// Set up objects
		_window.SetColor((byte)(Acom.Palette.BlockOffset(0) - 1));
		_window.SetHighContrast(true);
		_window.SetBackground(_game.Mod.GetSurface(_battleGame.HiddenMovementBackground));

		_txtMessageReinforcements.SetBig();
		_txtMessageReinforcements.Align = TextHAlign.ALIGN_CENTER;
		_txtMessageReinforcements.VerticalAlign = TextVAlign.ALIGN_BOTTOM;
		_txtMessageReinforcements.SetWordWrap(true);
		_txtMessageReinforcements.SetHighContrast(true);
		_txtMessageReinforcements.SetColor((byte)_game.Mod.GetInterface("inventory").GetElement("weight").color2); // red

		_btnBriefingReinforcements.Text = Tr("STR_TELL_ME_MORE");
		_btnBriefingReinforcements.SetHighContrast(true);
		_btnBriefingReinforcements.OnMouseClick(BtnBriefingReinforcementsClick);
		_btnBriefingReinforcements.Visible = false;

		_txtTitle.SetBig();
		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetHighContrast(true);
		_txtTitle.SetText(Tr("STR_OPENXCOM").Arg(Options.GetActiveMasterInfo().VersionDisplay));

		_txtTurn.SetBig();
		_txtTurn.Align = TextHAlign.ALIGN_CENTER;
		_txtTurn.SetHighContrast(true);
		var ss = new StringBuilder();
		ss.Append((string)Tr("STR_TURN").Arg(_currentTurn));
		if (battleGame.TurnLimit > 0)
		{
			ss.Append('/').Append(battleGame.TurnLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
			if (battleGame.TurnLimit - _currentTurn <= 3)
			{
				// gonna borrow the inventory's "over weight" colour when we're down to the last three turns
				_txtTurn.SetColor((byte)_game.Mod.GetInterface("inventory").GetElement("weight").color2);
			}
		}
		_txtTurn.SetText(ss.ToString());

		_txtSide.SetBig();
		_txtSide.Align = TextHAlign.ALIGN_CENTER;
		_txtSide.SetHighContrast(true);
		_txtSide.SetText(Tr("STR_SIDE").Arg(Tr(_battleGame.Side == FACTION_PLAYER ? "STR_XCOM" : "STR_ALIENS")));

		_txtMessage.SetBig();
		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.SetHighContrast(true);
		_txtMessage.SetText(Tr("STR_PRESS_BUTTON_TO_CONTINUE"));

		_txtMessage2.SetBig();
		_txtMessage2.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage2.SetHighContrast(true);
		_txtMessage2.SetText("");

		_txtMessage3.SetBig();
		_txtMessage3.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage3.SetHighContrast(true);
		_txtMessage3.SetText("");

		_state.ClearMouseScrollingState();

		// environmental effects
		string message = "";

		if (sc != null && !_battleGame.BattleGame.AreAllEnemiesNeutralized())
		{
			if (_battleGame.Side == FACTION_PLAYER)
			{
				// beginning of player's turn
				EnvironmentalCondition friendly = sc.GetEnvironmetalCondition("STR_FRIENDLY");

				bool showMessage = ApplyEnvironmentalConditionToFaction(FACTION_PLAYER, friendly);

				if (showMessage)
				{
					_txtMessage2.SetColor((byte)friendly.color);
					message = Tr(friendly.message);
					_txtMessage2.SetText(message);
				}
			}
			else
			{
				// beginning of alien's and neutral's turn
				bool anyoneStanding = false;
				foreach (BattleUnit bu in _battleGame.Units)
				{
					if (bu.OriginalFaction == FACTION_HOSTILE && !bu.IsOut && !bu.IsOutThresholdExceed)
					{
						anyoneStanding = true;
					}
				}

				if (anyoneStanding)
				{
					EnvironmentalCondition hostile = sc.GetEnvironmetalCondition("STR_HOSTILE");
					EnvironmentalCondition neutral = sc.GetEnvironmetalCondition("STR_NEUTRAL");

					bool showMessage = false;
					if (ApplyEnvironmentalConditionToFaction(FACTION_HOSTILE, hostile))
					{
						_txtMessage2.SetColor((byte)hostile.color);
						_txtMessage2.SetText(Tr(hostile.message));
						showMessage = true;
					}
					if (ApplyEnvironmentalConditionToFaction(FACTION_NEUTRAL, neutral))
					{
						_txtMessage3.SetColor((byte)neutral.color);
						_txtMessage3.SetText(Tr(neutral.message));
						showMessage = true;
					}

					if (showMessage)
					{
						// as in C++: ss.clear() only resets the stream flags, the turn text stays in front
						ss.Append((string)Tr(hostile.message)).Append((string)Tr(neutral.message));
						message = ss.ToString();
					}
				}
			}
		}

		if (string.IsNullOrEmpty(message))
		{
			_battleGame.AppendToHitLog(HitLogEntryType.HITLOG_NEW_TURN, _battleGame.Side);
		}
		else
		{
			_battleGame.AppendToHitLog(HitLogEntryType.HITLOG_NEW_TURN_WITH_MESSAGE, _battleGame.Side, message);
		}

		if (_battleGame.Side == FACTION_PLAYER)
		{
			CheckBugHuntMode();
			_state.BugHuntMessage();
		}

		string messageReinforcements = "";
		bool allowReinforcements = _battleGame.Side == FACTION_HOSTILE && _battleGame.Turn > 0;
		if (_battleGame.Side == FACTION_PLAYER && _battleGame.Turn == 0)
		{
			allowReinforcements = true;
		}
		if (allowReinforcements && !_battleGame.BattleGame.AreAllEnemiesNeutralized())
		{
			bool showAlert = DetermineReinforcements();
			if (showAlert && _battleGame.Turn > 0)
			{
				messageReinforcements = Tr("STR_REINFORCEMENTS_ALERT");
				_txtMessageReinforcements.SetText(messageReinforcements);
			}
		}

		if (Options.skipNextTurnScreen && string.IsNullOrEmpty(message) && string.IsNullOrEmpty(messageReinforcements))
		{
			_timer = new Timer(NEXT_TURN_DELAY);
			_timer.OnTimer((StateHandler)Close);
			_timer.Start();
		}
	}

	/// <summary>Initialization.</summary>
	public override void Init()
	{
		base.Init();

		if (_battleGame.IsPreview)
		{
			_battleGame.BattleGame.CleanupDeleted();
			_game.PopState();

			if (_battleGame.Side == FACTION_HOSTILE)
			{
				// end preview
				_state.FinishBattle(false, 42);
			}
			else
			{
				// start preview
				_state.BtnCenterClick(null);

				// Try to reactivate the touch buttons at the start of the player's turn
				if (_battleGame.Side == FACTION_PLAYER)
				{
					_state.ToggleTouchButtons(false, true);
				}
			}
		}
	}

	/// <summary>
	/// Checks if bug hunt mode should be activated or not.
	/// </summary>
	private void CheckBugHuntMode()
	{
		// too early for bug hunt
		if (_currentTurn < _battleGame.BughuntMinTurn) return;

		// bug hunt is already activated
		if (_battleGame.BughuntMode) return;

		int count = 0;
		foreach (BattleUnit bu in _battleGame.Units)
		{
			if (!bu.IsOut)
			{
				if (bu.OriginalFaction == FACTION_HOSTILE)
				{
					// we can see them, duh!
					if (bu.Visible) return;

					// VIPs are still in the game
					if (bu.RankInt <= _game.Mod.BughuntRank) return; // AR_COMMANDER = 0, AR_LEADER = 1, ...

					count++;
					// too many enemies are still in the game
					if (count > _game.Mod.BughuntMaxEnemies) return;

					bool hasWeapon = bu.LeftHandWeapon != null || bu.RightHandWeapon != null;
					bool hasLowMorale = bu.Morale < _game.Mod.BughuntLowMorale;
					bool hasTooManyTUsLeft = bu.TimeUnits > (bu.UnitRules.Stats.tu * _game.Mod.BughuntTimeUnitsLeft / 100);
					if (!hasWeapon || hasLowMorale || hasTooManyTUsLeft)
					{
						continue; // this unit is powerless, check next unit...
					}
					else
					{
						return; // this unit is OK, no bug hunt yet!
					}
				}
			}
		}

		_battleGame.BughuntMode = true; // if we made it this far, turn on the bug hunt!
	}

	/// <summary>
	/// Applies given environmental condition effects to a given faction.
	/// </summary>
	/// <returns>True, if any unit was affected (even if damage was zero).</returns>
	private bool ApplyEnvironmentalConditionToFaction(UnitFaction faction, EnvironmentalCondition condition)
	{
		if (!_battleGame.GetEnvironmentalConditionsEnabled(faction))
		{
			return false;
		}

		// Killing people before battle starts causes a crash
		// Panicking people before battle starts causes endless loop
		// Let's just avoid this instead of reworking everything
		if (faction == FACTION_PLAYER && _currentTurn <= 1)
		{
			return false;
		}

		// Note: there are limitations, since we're not using a projectile and nobody is actually shooting
		// 1. no power bonus based on shooting unit's stats (nobody is shooting)
		// 2. no power range reduction (there is no projectile, range = 0)
		// 3. no AOE damage from explosions (targets are damaged directly without affecting anyone/anything)
		// 4. no terrain damage

		bool showMessage = false;

		if (condition.chancePerTurn > 0 && condition.firstTurn <= _currentTurn && _currentTurn <= condition.lastTurn)
		{
			RuleItem weaponOrAmmo = _game.Mod.GetItem(condition.weaponOrAmmo);
			RuleDamageType type = weaponOrAmmo.DamageType;
			int power = weaponOrAmmo.Power; // no power bonus, no power range reduction

			UnitSide side = SIDE_FRONT;
			UnitBodyPart bodypart = BODYPART_TORSO;

			if (condition.side > -1)
			{
				side = (UnitSide)condition.side;
			}
			if (condition.bodyPart > -1)
			{
				bodypart = (UnitBodyPart)condition.bodyPart;
			}

			foreach (BattleUnit bu in _battleGame.Units)
			{
				if (bu.OriginalFaction == faction && bu.Status != UnitStatus.STATUS_DEAD && !bu.IsIgnored)
				{
					if (Rng.Percent(condition.chancePerTurn))
					{
						if (condition.side == -1)
						{
							side = (UnitSide)Rng.Generate((int)SIDE_FRONT, (int)SIDE_UNDER);
						}
						if (condition.bodyPart == -1)
						{
							bodypart = (UnitBodyPart)Rng.Generate((int)BODYPART_HEAD, (int)BODYPART_LEFTLEG);
						}
						bu.Damage(new Position(0, 0, 0), type.GetRandomDamage(power), type, _battleGame, new BattleActionAttack(), side, bodypart);
						showMessage = true;
					}
				}
			}
		}

		if (showMessage)
		{
			// now check for new casualties
			_battleGame.BattleGame.CheckForCasualties(null, new BattleActionAttack(), true, false);
			// revive units if damage could give hp or reduce stun
			//_battleGame->reviveUnconsciousUnits(true);
		}

		return showMessage;
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public override void Handle(Action action)
	{
		base.Handle(action);

		if (_battleGame.IsPreview)
		{
			// prevent NPE on Android/iOS
			return;
		}

		if (_btnBriefingReinforcements.Visible && action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
		{
			double mx = action.AbsoluteXMouse;
			double my = action.AbsoluteYMouse;
			if (mx >= _btnBriefingReinforcements.X && mx < _btnBriefingReinforcements.X + _btnBriefingReinforcements.Width &&
				my >= _btnBriefingReinforcements.Y && my < _btnBriefingReinforcements.Y + _btnBriefingReinforcements.Height)
			{
				// don't close on Briefing button click
				return;
			}
		}

		if (action.Details.type == SDLEventType.SDL_KEYDOWN || action.Details.type == SDLEventType.SDL_MOUSEBUTTONDOWN)
		{
			Close();
		}
	}

	/// <summary>
	/// Keeps the timer running.
	/// </summary>
	public override void Think()
	{
		_timer?.Think(this, null);
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void Close()
	{
		_battleGame.BattleGame.CleanupDeleted();
		_game.PopState();

		BattlescapeTally tally = _state.BattleGame.TallyUnits();

		if (_battleGame.BattleGame.AreAllEnemiesNeutralized())
		{
			// we don't care if someone was revived in the meantime, the decision to end the battle was already made!
			tally.liveAliens = 0;

			// mind control anyone who was revived (needed for correct recovery in the debriefing)
			foreach (BattleUnit bu in _battleGame.Units)
			{
				if (bu.OriginalFaction == FACTION_HOSTILE && !bu.IsOut)
				{
					bu.ConvertToFaction(FACTION_PLAYER);
				}
			}

			// reset needed because of the potential next stage in multi-stage missions
			_battleGame.BattleGame.ResetAllEnemiesNeutralized();
		}

		// not "escort the VIPs" missions, not the final mission and all aliens dead.
		bool killingAllAliensIsNotEnough = _battleGame.ObjectiveType == SpecialTileType.MUST_DESTROY || (_battleGame.VIPSurvivalPercentage > 0 && _battleGame.VIPEscapeType != EscapeType.ESCAPE_NONE);
		if ((!killingAllAliensIsNotEnough && tally.liveAliens == 0) || tally.liveSoldiers == 0)
		{
			_state.FinishBattle(false, tally.liveSoldiers);
		}
		else
		{
			_state.BtnCenterClick(null);

			// Try to reactivate the touch buttons at the start of the player's turn
			if (_battleGame.Side == FACTION_PLAYER)
			{
				_state.ToggleTouchButtons(false, true);
			}

			// Autosave every set amount of turns
			if ((_currentTurn == 1 || _currentTurn % Options.autosaveFrequency == 0) && _battleGame.Side == FACTION_PLAYER)
			{
				_state.Autosave(_currentTurn);
			}
		}
	}

	/// <summary>Update the resolution settings, we just resized the window.</summary>
	public override void Resize(ref int dX, ref int dY)
	{
		base.Resize(ref dX, ref dY);
		_bg.X = 0;
		_bg.Y = 0;
	}

	/// <summary>
	/// Shows the custom briefing for reinforcements.
	/// </summary>
	public void BtnBriefingReinforcementsClick(Action action)
	{
		if (_showBriefing)
		{
			_game.PushState(new BriefingState(null, null, true, _customBriefing));
		}
	}

	/// <summary>std::map::operator[] for the reinforcements memory: inserts 0 for a missing key.</summary>
	private static int MemoryAt(SortedDictionary<string, int> memory, string key)
	{
		if (!memory.TryGetValue(key, out int value))
		{
			memory[key] = 0;
		}
		return value;
	}

	/// <summary>
	/// Runs reinforcements logic.
	/// </summary>
	private bool DetermineReinforcements()
	{
		AlienDeployment deployment = _game.Mod.GetDeployment(_battleGame.ReinforcementsDeployment, true);

		int currentTurnReinforcements = _battleGame.Turn;

		if (deployment == null)
		{
			// for backwards-compatibility! this save does not contain the data needed for this functionality...
			return false;
		}

		bool showAlert = false;
		foreach (ReinforcementsData wave in deployment.ReinforcementsData)
		{
			// 1. check pre-requisites
			{
				if ((int)_game.SavedGame.Difficulty < wave.minDifficulty || (int)_game.SavedGame.Difficulty > wave.maxDifficulty)
				{
					continue;
				}
				if (wave.objectiveDestroyed)
				{
					if (!_battleGame.AllObjectivesDestroyed())
					{
						continue;
					}
				}
				else if (wave.turns.Count != 0)
				{
					if (!wave.turns.Contains(currentTurnReinforcements))
					{
						continue;
					}
				}
				else
				{
					if (currentTurnReinforcements < wave.minTurn || (wave.maxTurn != -1 && currentTurnReinforcements > wave.maxTurn))
					{
						continue;
					}
				}
				if (wave.executionOdds < 100 && !Rng.Percent(wave.executionOdds))
				{
					continue;
				}
				if (wave.maxRuns != -1 && MemoryAt(_battleGame.ReinforcementsMemory, wave.type) >= wave.maxRuns)
				{
					continue;
				}
			}

			// 2a. calculate compliant blocks
			{
				_compliantBlocksMap.Clear();
				_compliantBlocksList.Clear();

				int sizeX = _battleGame.MapSizeX / 10;
				int sizeY = _battleGame.MapSizeY / 10;

				if (wave.mapBlockFilterType == MFT_BY_MAPSCRIPT)
				{
					// we're done (copy, as in C++)
					_compliantBlocksMap = _battleGame.ReinforcementsBlocks.Select(col => new List<int>(col)).ToList();
				}
				else if (wave.mapBlockFilterType == MFT_BY_REINFORCEMENTS || wave.mapBlockFilterType == MFT_BY_BOTH_UNION || wave.mapBlockFilterType == MFT_BY_BOTH_INTERSECTION)
				{
					if (wave.spawnBlocks.Count == 0)
						ResizeBlocksMap(sizeX, sizeY, 1); // start with all true
					else
						ResizeBlocksMap(sizeX, sizeY, 0); // start with all false
				}
				else //if (wave.mapBlockFilterType == MFT_NONE)
				{
					ResizeBlocksMap(sizeX, sizeY, 1); // all true (and we're done)
				}

				if (wave.mapBlockFilterType == MFT_BY_REINFORCEMENTS || wave.mapBlockFilterType == MFT_BY_BOTH_UNION || wave.mapBlockFilterType == MFT_BY_BOTH_INTERSECTION)
				{
					if (wave.spawnBlocks.Count != 0)
					{
						foreach (string dir in wave.spawnBlocks)
						{
							if (dir == "EDGES")
							{
								for (int x = 0; x < sizeX; ++x) _compliantBlocksMap[x][0] = 1;
								for (int y = 0; y < sizeY; ++y) _compliantBlocksMap[0][y] = 1;
								for (int x = 0; x < sizeX; ++x) _compliantBlocksMap[x][sizeY - 1] = 1;
								for (int y = 0; y < sizeY; ++y) _compliantBlocksMap[sizeX - 1][y] = 1;
								break;
							}
							if (dir == "NORTH")
								for (int x = 0; x < sizeX; ++x) _compliantBlocksMap[x][0] = 1;
							else if (dir == "WEST")
								for (int y = 0; y < sizeY; ++y) _compliantBlocksMap[0][y] = 1;
							else if (dir == "SOUTH")
								for (int x = 0; x < sizeX; ++x) _compliantBlocksMap[x][sizeY - 1] = 1;
							else if (dir == "EAST")
								for (int y = 0; y < sizeY; ++y) _compliantBlocksMap[sizeX - 1][y] = 1;
							else if (dir == "NW")
								_compliantBlocksMap[0][0] = 1;
							else if (dir == "NE")
								_compliantBlocksMap[sizeX - 1][0] = 1;
							else if (dir == "SW")
								_compliantBlocksMap[0][sizeY - 1] = 1;
							else if (dir == "SE")
								_compliantBlocksMap[sizeX - 1][sizeY - 1] = 1;
						}
					}

					if (wave.mapBlockFilterType == MFT_BY_BOTH_UNION)
					{
						List<List<int>> toMerge = _battleGame.ReinforcementsBlocks;
						for (int x = 0; x < sizeX; ++x)
							for (int y = 0; y < sizeY; ++y)
								_compliantBlocksMap[x][y] = _compliantBlocksMap[x][y] + toMerge[x][y];
					}
					else if (wave.mapBlockFilterType == MFT_BY_BOTH_INTERSECTION)
					{
						List<List<int>> toMerge = _battleGame.ReinforcementsBlocks;
						for (int x = 0; x < sizeX; ++x)
							for (int y = 0; y < sizeY; ++y)
								_compliantBlocksMap[x][y] = _compliantBlocksMap[x][y] * toMerge[x][y];
					}
				}

				bool checkGroups = wave.spawnBlockGroups.Count != 0;

				for (int x = 0; x < sizeX; ++x)
				{
					for (int y = 0; y < sizeY; ++y)
					{
						if (_compliantBlocksMap[x][y] > 0)
						{
							if (checkGroups)
							{
								RuleTerrain terrain = _game.Mod.GetTerrain(_battleGame.FlattenedMapTerrainNames[x][y], false);
								if (terrain == null)
								{
									RuleCraft craft = _game.Mod.GetCraft(_battleGame.FlattenedMapTerrainNames[x][y], false);
									if (craft != null)
									{
										terrain = craft.BattlescapeTerrainData;
									}
								}
								if (terrain == null)
								{
									RuleUfo ufo = _game.Mod.GetUfo(_battleGame.FlattenedMapTerrainNames[x][y], false);
									if (ufo != null)
									{
										terrain = ufo.BattlescapeTerrainData;
									}
								}
								if (terrain != null)
								{
									MapBlock mapblock = terrain.GetMapBlock(_battleGame.FlattenedMapBlockNames[x][y]);
									if (mapblock != null)
									{
										bool groupMatched = false;
										foreach (int group in wave.spawnBlockGroups)
										{
											if (mapblock.IsInGroup(group))
											{
												groupMatched = true;
												break;
											}
										}
										if (!groupMatched)
										{
											continue; // don't add this map block into _compliantBlocksList
										}
									}
								}
							}
							_compliantBlocksList.Add(new Position(x, y, 0));
						}
					}
				}
			}

			// synchronize _compliantBlocksList and _compliantBlocksMap
			{
				for (int x = 0; x < _battleGame.MapSizeX / 10; ++x)
					for (int y = 0; y < _battleGame.MapSizeY / 10; ++y)
						_compliantBlocksMap[x][y] = 0;

				foreach (Position pos in _compliantBlocksList)
					_compliantBlocksMap[pos.x][pos.y] = 1;
			}

			// 2b. calculate compliant nodes
			_compliantNodesList.Clear();
			if (wave.useSpawnNodes)
			{
				bool checkBlocks = wave.mapBlockFilterType != MFT_NONE;
				bool checkNodeRanks = wave.spawnNodeRanks.Count != 0;
				bool checkZLevels = wave.spawnZLevels.Count != 0;

				foreach (Node node in _battleGame.Nodes)
				{
					if (node.IsDummy)
					{
						continue;
					}
					if (checkNodeRanks && !wave.spawnNodeRanks.Contains((int)node.Rank))
					{
						continue;
					}
					if (checkZLevels && !wave.spawnZLevels.Contains(node.Position.z))
					{
						continue;
					}
					if (checkBlocks && _compliantBlocksMap[node.Position.x / 10][node.Position.y / 10] == 0)
					{
						continue;
					}
					Tile tileToCheck = _battleGame.GetTile(node.Position);
					if (tileToCheck != null && tileToCheck.Unit != null)
					{
						continue;
					}
					if (wave.minDistanceFromXcomUnits > 1)
					{
						bool foundXcomUnitNearby = false;
						foreach (BattleUnit xcomUnit in _battleGame.Units)
						{
							if (xcomUnit.OriginalFaction == FACTION_PLAYER &&
								!xcomUnit.IsOut &&
								Position.DistanceSq(xcomUnit.Position, node.Position) < wave.minDistanceFromXcomUnits * wave.minDistanceFromXcomUnits)
							{
								foundXcomUnitNearby = true;
								break;
							}
						}
						if (foundXcomUnitNearby) continue;
					}
					if (wave.maxDistanceFromBorders > 0 && wave.maxDistanceFromBorders < 10)
					{
						if (node.Position.x >= wave.maxDistanceFromBorders &&
							node.Position.x < _battleGame.MapSizeX - wave.maxDistanceFromBorders &&
							node.Position.y >= wave.maxDistanceFromBorders &&
							node.Position.y < _battleGame.MapSizeY - wave.maxDistanceFromBorders)
						{
							continue;
						}
					}
					_compliantNodesList.Add(node);
				}

				// let's simulate selecting at random even when iterating sequentially
				Rng.Shuffle(_compliantNodesList);
			}

			// 3. deploy units
			bool success = DeployReinforcements(wave);

			// 4. post-processing
			if (success)
			{
				// remember success
				_battleGame.ReinforcementsMemory[wave.type] = MemoryAt(_battleGame.ReinforcementsMemory, wave.type) + 1;

				if (!string.IsNullOrEmpty(wave.briefing.title))
				{
					_customBriefing = wave.briefing.Clone();
					_showBriefing = true;
				}
				showAlert = true;
			}
		}
		if (_showBriefing)
		{
			_btnBriefingReinforcements.Visible = true;
		}

		return showAlert;
	}

	/// <summary>std::vector::resize(sizeX, std::vector&lt;int&gt;(sizeY, value)) on the (cleared) blocks map.</summary>
	private void ResizeBlocksMap(int sizeX, int sizeY, int value)
	{
		while (_compliantBlocksMap.Count > sizeX)
			_compliantBlocksMap.RemoveAt(_compliantBlocksMap.Count - 1);
		while (_compliantBlocksMap.Count < sizeX)
			_compliantBlocksMap.Add(Enumerable.Repeat(value, sizeY).ToList());
	}

	/// <summary>
	/// Deploys the reinforcements, according to the alien reinforcements deployment rules.
	/// </summary>
	private bool DeployReinforcements(ReinforcementsData wave)
	{
		AlienRace race = _game.Mod.GetAlienRace(_battleGame.ReinforcementsRace, true);
		int month = _battleGame.ReinforcementsItemLevel;
		bool success = false;

		foreach (DeploymentData dd in wave.data)
		{
			int quantity;

			switch (_game.SavedGame.Difficulty)
			{
				case GameDifficulty.DIFF_BEGINNER:
					quantity = dd.lowQty;
					break;
				case GameDifficulty.DIFF_EXPERIENCED:
					quantity = dd.medQty > 0 ? dd.lowQty + ((dd.medQty - dd.lowQty) / 2) : dd.lowQty;
					break;
				case GameDifficulty.DIFF_VETERAN:
					quantity = dd.medQty > 0 ? dd.medQty : dd.lowQty + ((dd.highQty - dd.lowQty) / 2);
					break;
				case GameDifficulty.DIFF_GENIUS:
					quantity = dd.medQty > 0 ? dd.medQty + ((dd.highQty - dd.medQty) / 2) : dd.lowQty + ((dd.highQty - dd.lowQty) / 2);
					break;
				case GameDifficulty.DIFF_SUPERHUMAN:
				default:
					quantity = dd.highQty;
					break;
			}

			quantity += Rng.Generate(0, dd.dQty);
			quantity += Rng.Generate(0, dd.extraQty);

			for (int i = 0; i < quantity; ++i)
			{
				string alienName = string.IsNullOrEmpty(dd.customUnitType) ? race.GetMember(dd.alienRank) : dd.customUnitType;
				Unit rule = _game.Mod.GetUnit(alienName, true);
				bool civilian = dd.percentageOutsideUfo != 0; // small misuse of an unused attribute ;) pls don't kill me
				BattleUnit unit = AddReinforcement(wave, rule, dd.alienRank, civilian);
				int itemLevel = _game.Mod.AlienItemLevels[month][Rng.Generate(0, 9)];
				if (unit != null)
				{
					success = true;
					_battleGame.InitUnit(unit, itemLevel);
					if (!rule.IsLivingWeapon)
					{
						if (dd.itemSets.Count == 0)
						{
							throw new InvalidOperationException("Reinforcements generator encountered an error: item set not defined");
						}
						if (itemLevel >= dd.itemSets.Count)
						{
							itemLevel = dd.itemSets.Count - 1;
						}
						foreach (string itemType in dd.itemSets[itemLevel].items)
						{
							RuleItem ruleItem = _game.Mod.GetItem(itemType);
							if (ruleItem != null)
							{
								_battleGame.CreateItemForUnit(ruleItem, unit);
							}
						}
						foreach (ItemSet iset in dd.extraRandomItems)
						{
							if (iset.items.Count == 0)
								continue;
							int pick = Rng.Generate(0, iset.items.Count - 1);
							RuleItem ruleItem = _game.Mod.GetItem(iset.items[pick]);
							if (ruleItem != null)
							{
								_battleGame.CreateItemForUnit(ruleItem, unit);
							}
						}
					}
				}
			}
		}

		return success;
	}

	/// <summary>
	/// Adds a reinforcements unit to the game and places him on a free spawnpoint.
	/// </summary>
	/// <returns>The created unit (null if it could not be placed).</returns>
	private BattleUnit AddReinforcement(ReinforcementsData wave, Unit rules, int alienRank, bool civilian)
	{
		BattleUnit unit = _battleGame.CreateTempUnit(rules, civilian ? FACTION_NEUTRAL : FACTION_HOSTILE);

		// 1. try nodes first
		bool unitPlaced = false;
		if (wave.useSpawnNodes && _compliantNodesList.Count != 0)
		{
			foreach (Node node in _compliantNodesList)
			{
				if (_battleGame.SetUnitPosition(unit, node.Position))
				{
					unit.AIModule.SetStartNode(node);
					unit.RankInt = alienRank;
					unit.Direction = Rng.Generate(0, 7);
					_battleGame.Units.Add(unit);
					unitPlaced = true;
					break;
				}
			}
		}

		// 2. then try random positions on compliant blocks
		if (!unitPlaced && _compliantBlocksList.Count != 0)
		{
			var tmpZList = new List<int>(wave.spawnZLevels); // copy
			if (tmpZList.Count == 0)
			{
				for (int z = 0; z < _battleGame.MapSizeZ; ++z)
				{
					tmpZList.Add(z);
				}
			}
			if (wave.randomizeZLevels)
			{
				Rng.Shuffle(tmpZList);
			}
			int tries = 100;
			int randomX;
			int randomY;
			int edgeX = (_battleGame.MapSizeX / 10) - 1;
			int edgeY = (_battleGame.MapSizeY / 10) - 1;
			while (!unitPlaced && tries != 0)
			{
				int randomBlockIndex = Rng.Generate(0, _compliantBlocksList.Count - 1);
				if (wave.maxDistanceFromBorders > 0 && wave.maxDistanceFromBorders < 10)
				{
					// Note: we assume that the modder has done the necessary work and allowed only border blocks to come this far
					if (_compliantBlocksList[randomBlockIndex].x == 0)
					{
						randomX = Rng.Generate(0, wave.maxDistanceFromBorders - 1);
					}
					else if (_compliantBlocksList[randomBlockIndex].x == edgeX)
					{
						randomX = Rng.Generate(10 - wave.maxDistanceFromBorders, 9);
					}
					else
					{
						randomX = Rng.Generate(0, 9); // modder fail
					}
					if (_compliantBlocksList[randomBlockIndex].y == 0)
					{
						randomY = Rng.Generate(0, wave.maxDistanceFromBorders - 1);
					}
					else if (_compliantBlocksList[randomBlockIndex].y == edgeY)
					{
						randomY = Rng.Generate(10 - wave.maxDistanceFromBorders, 9);
					}
					else
					{
						randomY = Rng.Generate(0, 9); // modder fail
					}
				}
				else
				{
					randomX = Rng.Generate(0, 9);
					randomY = Rng.Generate(0, 9);
				}
				var randomPos = new Position(_compliantBlocksList[randomBlockIndex].x * 10 + randomX, _compliantBlocksList[randomBlockIndex].y * 10 + randomY, 0);

				bool foundXcomUnitNearby = false;
				if (wave.minDistanceFromXcomUnits > 1)
				{
					foreach (BattleUnit xcomUnit in _battleGame.Units)
					{
						if (xcomUnit.OriginalFaction == FACTION_PLAYER &&
							!xcomUnit.IsOut &&
							Position.DistanceSq(xcomUnit.Position, randomPos) < wave.minDistanceFromXcomUnits * wave.minDistanceFromXcomUnits)
						{
							foundXcomUnitNearby = true;
							break;
						}
					}
				}
				if (!foundXcomUnitNearby)
				{
					foreach (int tryZ in tmpZList)
					{
						Position finalPos = randomPos + new Position(0, 0, tryZ);
						if (unit.MovementType != MovementType.MT_FLY)
						{
							Tile t = _battleGame.GetTile(finalPos);
							if (t == null || t.GetTUCost((int)TilePart.O_FLOOR, unit.MovementType) == Pathfinding.INVALID_MOVE_COST)
							{
								// non-flying units cannot spawn on e.g. a water tile (e.g. in the POLAR terrain)
								continue;
							}
						}
						if (_battleGame.SetUnitPosition(unit, finalPos))
						{
							unit.RankInt = alienRank;
							unit.Direction = Rng.Generate(0, 7);
							_battleGame.Units.Add(unit);
							unitPlaced = true;
							break;
						}
					}
				}
				--tries;
			}
		}

		// 3. finally just place it somewhere
		if (!unitPlaced && wave.forceSpawnNearFriend && PlaceReinforcementNearFriend(unit))
		{
			unit.RankInt = alienRank;
			unit.Direction = Rng.Generate(0, 7);
			_battleGame.Units.Add(unit);
			unitPlaced = true;
		}

		// 4. dude, seriously?
		if (!unitPlaced)
		{
			unit = null;
		}

		return unit;
	}

	/// <summary>
	/// Places a unit near a friendly unit.
	/// </summary>
	/// <returns>If we successfully placed the unit.</returns>
	private bool PlaceReinforcementNearFriend(BattleUnit unit)
	{
		if (_battleGame.Units.Count == 0)
		{
			return false;
		}
		for (int i = 0; i != 10; ++i)
		{
			Position entryPoint = TileEngine.invalid;
			int tries = 100;
			bool largeUnit = false;
			while (entryPoint == TileEngine.invalid && tries != 0)
			{
				BattleUnit k = _battleGame.Units[Rng.Generate(0, _battleGame.Units.Count - 1)];
				if (k.Faction == unit.Faction && k.Position != TileEngine.invalid && k.Armor.Size >= unit.Armor.Size)
				{
					entryPoint = k.Position;
					largeUnit = k.IsBigUnit;
				}
				--tries;
			}
			if (tries != 0 && _battleGame.PlaceUnitNearPosition(unit, entryPoint, largeUnit))
			{
				return true;
			}
		}
		return false;
	}
}
