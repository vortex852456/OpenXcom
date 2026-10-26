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
// Ported from: src/Geoscape/InterceptState.h, src/Geoscape/InterceptState.cpp
#nullable disable

using System.Text;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Intercept window that lets the player launch
/// crafts into missions from the Geoscape.
/// </summary>
public class InterceptState : State
{
	private readonly TextButton _btnCancel, _btnGotoBase;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtCraft, _txtStatus, _txtBase, _txtWeapons;
	private readonly TextList _lstCrafts;
	private readonly Globe _globe;
	private readonly Base _base;
	private readonly Target _target;
	private readonly List<Craft> _crafts = [];
	private readonly List<Craft> _selCrafts = [];

	/// <summary>
	/// Initializes all the elements in the Intercept window.
	/// </summary>
	/// <param name="globe">Pointer to the Geoscape globe.</param>
	/// <param name="useCustomSound">Play a custom sound when opened?</param>
	/// <param name="base">Pointer to base to show contained crafts (NULL to show all crafts).</param>
	/// <param name="target">Pointer to target to intercept (NULL to ask user for target).</param>
	public InterceptState(Globe globe, bool useCustomSound, Base @base = null, Target target = null)
	{
		_globe = globe;
		_base = @base;
		_target = target;

		const int WIDTH_CRAFT = 72;
		const int WIDTH_STATUS = 94;
		const int WIDTH_BASE = 74;
		const int WIDTH_WEAPONS = 48;

		_screen = false;

		if (useCustomSound)
		{
			var sounds = _game.Mod.SelectBaseSounds;
			int soundId = sounds.Count == 0 ? Mod.NO_SOUND : sounds[Rng.Generate(0, sounds.Count - 1)];
			if (soundId != Mod.NO_SOUND)
			{
				_customSound = _game.Mod.GetSound("GEO.CAT", soundId);
			}
		}

		int extraRows = Math.Clamp(Options.oxceInterceptTableSize, 8, 80) - 8;
		int extraHeight = 8 * extraRows;
		int offset = extraHeight / 2;

		// Create objects
		if (Options.oxceInterceptGuiMaintenanceTime > 0)
		{
			_window = new Window(this, 320, 140 + extraHeight, 0, 30 - offset, WindowPopup.POPUP_HORIZONTAL);
			_btnCancel = new TextButton(_base != null ? 142 : 288, 16, 16, 146 + offset);
			_btnGotoBase = new TextButton(142, 16, 162, 146 + offset);
			_txtTitle = new Text(300, 17, 10, 46 - offset);
			int x = 14;
			_txtCraft = new Text(WIDTH_CRAFT, 9, x, 70 - offset);
			x += WIDTH_CRAFT;
			_txtStatus = new Text(WIDTH_STATUS, 9, x, 70 - offset);
			x += WIDTH_STATUS;
			_txtBase = new Text(WIDTH_BASE, 9, x, 70 - offset);
			x += WIDTH_BASE;
			_txtWeapons = new Text(WIDTH_WEAPONS + 4, 17, x - 4, 62 - offset);
			_lstCrafts = new TextList(290, 64 + extraHeight, 12, 78 - offset);
		}
		else
		{
			_window = new Window(this, 320, 140 + extraHeight, 0, 30 - offset, WindowPopup.POPUP_HORIZONTAL);
			_btnCancel = new TextButton(_base != null ? 142 : 288, 16, 16, 146 + offset);
			_btnGotoBase = new TextButton(142, 16, 162, 146 + offset);
			_txtTitle = new Text(300, 17, 10, 46 - offset);
			_txtCraft = new Text(86, 9, 14, 70 - offset);
			_txtStatus = new Text(70, 9, 100, 70 - offset);
			_txtBase = new Text(80, 9, 170, 70 - offset);
			_txtWeapons = new Text(80, 17, 238, 62 - offset);
			_lstCrafts = new TextList(288, 64 + extraHeight, 8, 78 - offset);
		}

		// Set palette
		SetInterface("intercept");

		Add(_window, "window", "intercept");
		Add(_btnCancel, "button", "intercept");
		Add(_btnGotoBase, "button", "intercept");
		Add(_txtTitle, "text1", "intercept");
		Add(_txtCraft, "text2", "intercept");
		Add(_txtStatus, "text2", "intercept");
		Add(_txtBase, "text2", "intercept");
		Add(_txtWeapons, "text2", "intercept");
		Add(_lstCrafts, "list", "intercept");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "intercept");

		_btnCancel.Text = Tr("STR_CANCEL");
		_btnCancel.OnMouseClick(BtnCancelClick);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyCancel);
		_btnCancel.OnKeyboardPress(BtnCancelClick, Options.keyGeoIntercept);

		_btnGotoBase.Text = Tr("STR_GO_TO_BASE");
		_btnGotoBase.OnMouseClick(BtnGotoBaseClick);
		_btnGotoBase.Visible = _base != null;

		_txtTitle.Align = TextHAlign.ALIGN_CENTER;
		_txtTitle.SetBig();
		_txtTitle.SetText(Tr("STR_LAUNCH_INTERCEPTION"));

		_txtCraft.SetText(Tr("STR_CRAFT"));

		_txtStatus.SetText(Tr("STR_STATUS"));

		_txtBase.SetText(Tr("STR_BASE"));

		if (Options.oxceInterceptGuiMaintenanceTime > 0)
		{
			_txtWeapons.Align = TextHAlign.ALIGN_RIGHT;
		}
		_txtWeapons.SetText(Tr("STR_WEAPONS_CREW_HWPS"));

		if (Options.oxceInterceptGuiMaintenanceTime > 0)
		{
			_lstCrafts.SetColumns(4, WIDTH_CRAFT, WIDTH_STATUS, WIDTH_BASE, WIDTH_WEAPONS);
			_lstCrafts.SetAlign(TextHAlign.ALIGN_RIGHT, 3);
		}
		else
		{
			_lstCrafts.SetColumns(4, 86, 70, 80, 46);
		}
		_lstCrafts.SetSelectable(true);
		_lstCrafts.SetBackground(_window);
		if (Options.oxceInterceptGuiMaintenanceTime > 0)
		{
			_lstCrafts.Margin = 2;
		}
		else
		{
			_lstCrafts.Margin = 6;
		}
		_lstCrafts.OnMouseClick(LstCraftsLeftClick);
		_lstCrafts.OnMouseClick(LstCraftsRightClick, SDLMouse.SDL_BUTTON_RIGHT);
		_lstCrafts.OnMouseClick(LstCraftsMiddleClick, SDLMouse.SDL_BUTTON_MIDDLE);

		//clear list of selected crafts before creating a new wing
		_selCrafts.Clear();

		List<(Craft craft, double distance, Base xbase)> craftList = [];
		foreach (var xbase in _game.SavedGame.Bases)
		{
			if (_base != null && xbase != _base)
				continue;

			foreach (var xcraft in xbase.Crafts)
			{
				double xdistance = 0.0;
				if (_target != null) xdistance = xcraft.GetDistance(_target);
				craftList.Add((xcraft, xdistance, xbase));
			}
		}
		if (_target != null && Options.oxceGeoSortCraftByDistanceToTarget)
		{
			// std::stable_sort with operator< on the distance
			StableSortByDistance(craftList);
		}

		int row = 0;
		foreach (var tuple in craftList)
		{
			var xbase = tuple.xbase;
			var xcraft = tuple.craft;
			{
				StringBuilder ssStatus = new StringBuilder();
				string status = xcraft.Status;
				bool hasEnoughPilots = xcraft.ArePilotsOnboard(_game.Mod);
				if (status == "STR_OUT")
				{
					// QoL: let's give the player a bit more info
					if (xcraft.Destination == null || xcraft.IsAutoPatrolling)
					{
						ssStatus.Append((string)Tr("STR_PATROLLING"));
					}
					else if (xcraft.LowFuel || xcraft.MissionComplete || xcraft.Destination == (Target)xcraft.Base)
					{
						ssStatus.Append((string)Tr("STR_RETURNING"));
						//ssStatus << tr("STR_RETURNING_TO_BASE"); // vanilla craft info
					}
					else
					{
						Ufo u = xcraft.Destination as Ufo;
						MissionSite m = xcraft.Destination as MissionSite;
						AlienBase b = xcraft.Destination as AlienBase;
						Craft craftTarget = xcraft.Destination as Craft;
						if (u != null)
						{
							if (xcraft.IsInDogfight)
							{
								ssStatus.Append((string)Tr("STR_TAILING_UFO"));
							}
							else if (u.Status == Ufo.UfoStatus.FLYING)
							{
								ssStatus.Append((string)Tr("STR_INTERCEPTING"));
								//ssStatus << tr("STR_INTERCEPTING_UFO").arg(u->getId()); // vanilla craft info
							}
							else
							{
								ssStatus.Append((string)Tr("STR_EN_ROUTE"));
							}
						}
						else if (craftTarget != null)
						{
							ssStatus.Append((string)Tr("STR_ESCORTING"));
						}
						else if (m != null || b != null)
						{
							ssStatus.Append((string)Tr("STR_EN_ROUTE"));
						}
						else
						{
							ssStatus.Append((string)Tr(status)); // "STR_OUT"
						}
					}
				}
				else
				{
					if (!hasEnoughPilots && status == "STR_READY")
					{
						ssStatus.Append((string)Tr("STR_PILOT_MISSING"));
					}
					else
					{
						ssStatus.Append((string)Tr(status));
					}
				}

				if (status != "STR_READY" && status != "STR_OUT")
				{
					uint maintenanceHours = 0;
					if (Options.oxceInterceptGuiMaintenanceTime == 2 || xcraft.Status == "STR_REPAIRS")
					{
						maintenanceHours += xcraft.CalcRepairTime();
					}
					if (Options.oxceInterceptGuiMaintenanceTime == 2 || xcraft.Status == "STR_REFUELLING")
					{
						maintenanceHours += xcraft.CalcRefuelTime();
					}
					if (Options.oxceInterceptGuiMaintenanceTime == 2 || xcraft.Status == "STR_REARMING")
					{
						// Note: if the craft is already refueling, don't count any potential rearm time (can be > 0 if ammo is missing)
						if (xcraft.Status != "STR_REFUELLING")
						{
							maintenanceHours += xcraft.CalcRearmTime();
						}
					}

					int days = (int)(maintenanceHours / 24);
					int hours = (int)(maintenanceHours % 24);
					if (maintenanceHours > 0 && Options.oxceInterceptGuiMaintenanceTime > 0)
					{
						ssStatus.Append(" (");
						if (days > 0)
						{
							ssStatus.Append((string)Tr("STR_DAY_SHORT").Arg(days));
						}
						if (hours > 0)
						{
							if (days > 0)
							{
								ssStatus.Append('/');
							}
							ssStatus.Append((string)Tr("STR_HOUR_SHORT").Arg(hours));
						}
						ssStatus.Append(')');
					}
				}
				else
				{
					// ETA display
					if (Options.oxceShowETAMode > 0 && xcraft.Destination != null && Options.oxceInterceptGuiMaintenanceTime > 0)
					{
						if (Options.oxceShowETAMode == 1 && xcraft.Destination is MovingTarget mt && mt.Speed > 0)
						{
							// don't show ETA for moving targets (i.e. UFOs and crafts)
						}
						else
						{
							int speed = xcraft.CraftStats.speedMax;
							int distance = Craft.XcomDistance(xcraft.GetDistance(xcraft.Destination));
							int etaInHoursHelper = (distance + (speed / 2)) / speed;
							int days = etaInHoursHelper / 24;
							int hours = etaInHoursHelper % 24;
							ssStatus.Append(" (");
							if (days > 0) ssStatus.Append((string)Tr("STR_DAY_SHORT").Arg(days));
							if (hours > 0 || days == 0)
							{
								if (days > 0) ssStatus.Append('/');
								ssStatus.Append((string)Tr("STR_HOUR_SHORT").Arg(hours));
							}
							ssStatus.Append(')');
						}
					}
				}

				StringBuilder ss = new StringBuilder();
				AppendCount(ss, xcraft.GetNumWeapons());
				ss.Append('/');
				AppendCount(ss, xcraft.NumTotalSoldiers);
				ss.Append('/');
				AppendCount(ss, xcraft.NumTotalVehicles);

				_crafts.Add(xcraft);
				_lstCrafts.AddRow(4, xcraft.GetName(_game.Language), ssStatus.ToString(), xbase.GetName(null), ss.ToString());
				if (hasEnoughPilots && status == "STR_READY")
				{
					_lstCrafts.SetCellColor(row, 1, _lstCrafts.SecondaryColor);
				}
				if (_target != null)
				{
					bool craftReturning = xcraft.LowFuel || xcraft.MissionComplete;
					if (craftReturning)
					{
						var disabledColor = _game.Mod.GetInterface("intercept").GetElement("disabled").color;
						_lstCrafts.SetCellColor(row, 0, unchecked((byte)disabledColor));
					}
					else
					{
						bool craftAvailable = Options.craftLaunchAlways || status == "STR_READY" || status == "STR_OUT";
						if (craftAvailable)
						{
							double craftDistanceToTarget = tuple.distance;
							double baseDistanceToTarget = xcraft.Base.GetDistance(_target);
							if (craftDistanceToTarget + baseDistanceToTarget > xcraft.BaseRange * 2.0)
							{
								var disabledColor = _game.Mod.GetInterface("intercept").GetElement("disabled").color;
								_lstCrafts.SetCellColor(row, 0, unchecked((byte)disabledColor));
							}
						}
					}
				}
				row++;
			}
		}
	}

	/// <summary>Writes a highlighted count, or a plain 0.</summary>
	private static void AppendCount(StringBuilder ss, int count)
	{
		if (count > 0)
		{
			ss.Append(Unicode.TOK_COLOR_FLIP).Append(count.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(Unicode.TOK_COLOR_FLIP);
		}
		else
		{
			ss.Append('0');
		}
	}

	/// <summary>Stable insertion sort by distance (std::stable_sort with operator&lt;).</summary>
	private static void StableSortByDistance(List<(Craft craft, double distance, Base xbase)> list)
	{
		for (int i = 1; i < list.Count; ++i)
		{
			var item = list[i];
			int j = i - 1;
			while (j >= 0 && item.distance < list[j].distance)
			{
				list[j + 1] = list[j];
				--j;
			}
			list[j + 1] = item;
		}
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	public void BtnCancelClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>
	/// Goes to the base for the respective craft.
	/// </summary>
	public void BtnGotoBaseClick(Action action)
	{
		_game.PopState();
		_game.PushState(new BasescapeState(_base, _globe));
	}

	/// <summary>
	/// Pick a target for the selected craft.
	/// </summary>
	public void LstCraftsLeftClick(Action action)
	{
		// condition used in shift and non-shift paths
		bool allowStart(Craft c)
		{
			return c.Status == "STR_READY" || (
				 (c.Status == "STR_OUT" || Options.craftLaunchAlways) &&
				 !c.LowFuel &&
				 !c.MissionComplete);
		}

		int row = (int)_lstCrafts.SelectedRow;
		Craft c = _crafts[row];
		// add and remove crafts to the wing to be created
		if (_game.IsShiftPressed())
		{
			// add craft to the list when it is not included yet
			// limit to 4 (3+1 due to the dogfight window)
			// need more sanity checks?
			if (allowStart(c) && (_selCrafts.Count < 3) && !_selCrafts.Contains(c))
			{
				_selCrafts.Add(c);
				_lstCrafts.SetCellColor(row, 0, _lstCrafts.SecondaryColor);
			}
			// remove craft from the wing to be created when it is already included
			else if (_selCrafts.Remove(c))
			{
				_lstCrafts.SetCellColor(row, 0, _lstCrafts.Color);
			}
		}
		// add last craft to the wing (may already be part of the wing) and launch
		else
		{
			if (allowStart(c))
			{
				//remove itself from the vector assuming it is already inside, so it can be put in front of the vector
				_selCrafts.Remove(c);
				_selCrafts.Insert(0, c);
				_game.PopState();
				if (_target == null)
				{
					_game.PushState(new SelectDestinationState(_selCrafts, _globe));
				}
				else
				{
					_game.PushState(new ConfirmDestinationState(_selCrafts, _target));
				}
			}
		}
	}

	/// <summary>
	/// Centers on the selected craft.
	/// </summary>
	public void LstCraftsRightClick(Action action)
	{
		Craft c = _crafts[(int)_lstCrafts.SelectedRow];
		if (c.Status == "STR_OUT")
		{
			_globe.Center(c.Longitude, c.Latitude);
			_game.PopState();
		}
		else
		{
			_game.PopState();
			bool found = false;
			foreach (var xbase in _game.SavedGame.Bases)
			{
				if (_base != null && xbase != _base)
					continue;

				for (int ci = 0; ci < xbase.Crafts.Count; ++ci)
				{
					if (c == xbase.Crafts[ci])
					{
						_game.PushState(new CraftInfoState(xbase, ci));
						found = true;
						break;
					}
				}
				if (found) break;
			}
		}
	}

	/// <summary>
	/// Opens the corresponding Ufopaedia article.
	/// </summary>
	public void LstCraftsMiddleClick(Action action)
	{
		Craft c = _crafts[(int)_lstCrafts.SelectedRow];
		if (c != null)
		{
			string articleId = c.Rules.Type;
			Ufopaedia.OpenArticle(_game, articleId);
		}
	}
}
