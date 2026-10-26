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
// Ported from: src/Battlescape/BriefingLightState.h, src/Battlescape/BriefingLightState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Briefing screen which displays basic info
/// about a mission site (alert/briefing only, no battle).
/// </summary>
public class BriefingLightState : State
{
	private readonly TextButton _btnOk;
	private readonly ToggleTextButton _btnArmors;
	private readonly Window _window;
	private readonly Text _txtTitle, _txtBriefing, _txtArmors;
	private readonly TextList _lstArmors;
	private readonly List<(string first, string second)> _armorNameList = [];

	/// <summary>Initializes all the elements in the BriefingLight screen.</summary>
	/// <param name="deployment">Pointer to the mission deployment.</param>
	public BriefingLightState(AlienDeployment deployment)
	{
		ArgumentNullException.ThrowIfNull(deployment);
		_screen = true;
		// Create objects
		_window = new Window(this, 320, 200, 0, 0);
		_btnOk = new TextButton(140, 18, 164, 164);
		_btnArmors = new ToggleTextButton(140, 18, 16, 164);
		_txtTitle = new Text(300, 32, 16, 24);
		_txtBriefing = new Text(288, 104, 16, 56);
		_txtArmors = new Text(288, 25, 16, 56);
		_lstArmors = new TextList(288, 80, 8, 81);

		string title = deployment.Type;
		string desc = deployment.AlertDescription;

		BriefingData data = deployment.BriefingData;
		SetStandardPalette("PAL_GEOSCAPE", data.palette);
		_window.SetBackground(_game.Mod.GetSurface(data.background));

		Add(_window, "window", "briefing");
		Add(_btnOk, "button", "briefing");
		Add(_btnArmors, "button", "briefing");
		Add(_txtTitle, "text", "briefing");
		Add(_txtBriefing, "text", "briefing");
		Add(_txtArmors, "text", "briefing");
		Add(_lstArmors, "text", "briefing");

		CenterAllSurfaces();

		// Set up objects
		_btnOk.Text = Tr("STR_OK");
		_btnOk.OnMouseClick(BtnOkClick);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyOk);
		_btnOk.OnKeyboardPress(BtnOkClick, Options.keyCancel);

		_btnArmors.Text = Tr("STR_WHAT_CAN_I_WEAR");
		_btnArmors.OnMouseClick(BtnArmorsClick);
		_btnArmors.Visible = false;

		_txtTitle.SetBig();
		_txtTitle.SetText(Tr(title));

		_txtBriefing.SetWordWrap(true);
		_txtBriefing.SetText(Tr(desc));

		_txtArmors.SetWordWrap(true);
		_txtArmors.Visible = false;

		_lstArmors.SetColumns(2, 148, 132);
		_lstArmors.SetSelectable(true);
		_lstArmors.SetBackground(_window);
		_lstArmors.Margin = 8;
		_lstArmors.Visible = false;

		CheckStartingCondition(deployment);

		_lstArmors.OnMouseClick(LstArmorsClick, SDLMouse.SDL_BUTTON_MIDDLE);
	}

	/// <summary>Checks the starting condition.</summary>
	private void CheckStartingCondition(AlienDeployment deployment)
	{
		RuleStartingCondition startingCondition = _game.Mod.GetStartingCondition(deployment.StartingCondition);
		if (startingCondition != null)
		{
			List<string> list = startingCondition.ForbiddenArmors.Count == 0 ? startingCondition.AllowedArmors : startingCondition.ForbiddenArmors;
			string messageCode = startingCondition.ForbiddenArmors.Count == 0 ? "STR_STARTING_CONDITION_ARMORS_ALLOWED" : "STR_STARTING_CONDITION_ARMORS_FORBIDDEN";
			if (list.Count != 0)
			{
				_txtArmors.SetText(Tr(messageCode).Arg("")); // passing empty argument, because it is obsolete since a list display was introduced
				_btnArmors.Visible = true;

				foreach (string armorType in list)
				{
					Armor armor = _game.Mod.GetArmor(armorType, false);
					ArticleDefinition article = _game.Mod.GetUfopaediaArticle(armor != null ? armor.UfopediaType : armorType, false);
					if (article != null && Ufopaedia.IsArticleAvailable(_game.SavedGame, article))
					{
						string translation = Tr(armorType);
						_armorNameList.Add((armorType, translation));
					}
				}
				if (_armorNameList.Count == 0)
				{
					// no suitable armor yet
					string translation = Tr("STR_UNKNOWN");
					_armorNameList.Add(("STR_UNKNOWN", translation));
				}
				// C++ std::sort (unstable) with Unicode::naturalCompare
				_armorNameList.Sort((a, b) => Unicode.NaturalCompare(a.second, b.second) ? -1 : (Unicode.NaturalCompare(b.second, a.second) ? 1 : 0));
				if (_armorNameList.Count % 2 != 0)
				{
					_armorNameList.Add(("", "")); // just padding, we want an even number of items in the list
				}
				int halfSize = _armorNameList.Count / 2;
				for (int i = 0; i < halfSize; ++i)
				{
					_lstArmors.AddRow(2, _armorNameList[i].second, _armorNameList[i + halfSize].second);
				}
			}
		}
	}

	/// <summary>Closes the window.</summary>
	public void BtnOkClick(Action action)
	{
		_game.PopState();
	}

	/// <summary>Shows allowed armors.</summary>
	public void BtnArmorsClick(Action action)
	{
		_txtArmors.Visible = _btnArmors.Pressed;
		_lstArmors.Visible = _btnArmors.Pressed;
		_txtBriefing.Visible = !_btnArmors.Pressed;
	}

	/// <summary>Shows corresponding Ufopaedia article.</summary>
	public void LstArmorsClick(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		int halfSize = _armorNameList.Count / 2;

		double mx = action.AbsoluteXMouse;
		if (mx < _btnOk.X)
		{
			halfSize = 0;
		}

		int idx = halfSize + (int)_lstArmors.SelectedRow;
		string armorType = _armorNameList[idx].first;
		Armor armor = _game.Mod.GetArmor(armorType, false);
		if (armor != null)
		{
			string articleId = armor.UfopediaType;
			Ufopaedia.OpenArticle(_game, articleId);
		}
	}
}
