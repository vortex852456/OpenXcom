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
// Ported from: src/Geoscape/ConfirmCydoniaState.h, src/Geoscape/ConfirmCydoniaState.cpp
#nullable disable

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>
/// Asks the player to confirm the final mission.
/// </summary>
public class ConfirmCydoniaState : State
{
	private readonly Window _window;
	private readonly Text _txtMessage;
	private readonly TextButton _btnNo, _btnYes;
	private readonly Craft _craft;

	/// <summary>
	/// Initializes all the elements in the Confirm Cydonia window.
	/// </summary>
	/// <param name="craft">Pointer to the craft.</param>
	public ConfirmCydoniaState(Craft craft)
	{
		_craft = craft;
		_screen = false;

		// Create objects
		_window = new Window(this, 256, 160, 32, 20);
		_btnYes = new TextButton(80, 20, 70, 142);
		_btnNo = new TextButton(80, 20, 170, 142);
		_txtMessage = new Text(224, 48, 48, 76);

		// Set palette
		SetInterface("confirmCydonia");

		Add(_window, "window", "confirmCydonia");
		Add(_btnYes, "button", "confirmCydonia");
		Add(_btnNo, "button", "confirmCydonia");
		Add(_txtMessage, "text", "confirmCydonia");

		CenterAllSurfaces();

		// Set up objects
		SetWindowBackground(_window, "confirmCydonia");

		_btnYes.Text = Tr("STR_YES");
		_btnYes.OnMouseClick(BtnYesClick);
		_btnYes.OnKeyboardPress(BtnYesClick, Options.keyOk);

		_btnNo.Text = Tr("STR_NO");
		_btnNo.OnMouseClick(BtnNoClick);
		_btnNo.OnKeyboardPress(BtnNoClick, Options.keyCancel);

		_txtMessage.Align = TextHAlign.ALIGN_CENTER;
		_txtMessage.SetBig();
		_txtMessage.SetWordWrap(true);
		_txtMessage.SetText(Tr("STR_ARE_YOU_SURE_CYDONIA"));
	}

	/// <summary>
	/// Starts the final mission.
	/// </summary>
	public void BtnYesClick(Action action)
	{
		_game.PopState();
		_game.PopState();

		SavedBattleGame bgame = new SavedBattleGame(_game.Mod, _game.Language);
		_game.SavedGame.SetBattleGame(bgame);
		BattlescapeGenerator bgen = new BattlescapeGenerator(_game);
		foreach (var ad in _game.Mod.DeploymentsList)
		{
			AlienDeployment deployment = _game.Mod.GetDeployment(ad);
			if (deployment.IsFinalDestination)
			{
				bgame.MissionType = ad;
				bgen.SetAlienRace(deployment.Race);
				break;
			}
		}
		bgen.SetCraft(_craft);
		bgen.Run();

		_game.PushState(new BriefingState(_craft, null));
	}

	/// <summary>
	/// Returns to the previous screen.
	/// </summary>
	public void BtnNoClick(Action action)
	{
		_game.PopState();
	}
}
