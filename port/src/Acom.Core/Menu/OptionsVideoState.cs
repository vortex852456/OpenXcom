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
// Ported from: src/Menu/OptionsVideoState.h, src/Menu/OptionsVideoState.cpp
// Port status: partial (missing: SDL_ListModes and SDL_WM_GrabInput – the host provides FullscreenModes and applies the mouse grab; display mode, filters and OpenGL shaders are stored as options, their effect is a host matter)
#nullable disable

using System.Globalization;

namespace Acom;

#pragma warning disable CA1822 // handlers are instance members as in C++ (State._game is static)

/// <summary>Screen that lets the user configure various Video options.</summary>
public class OptionsVideoState : OptionsBaseState
{
	private const string GL_EXT = "OpenGL.shader";
	private const string GL_FOLDER = "Shaders/";
	private const string GL_STRING = "*";

	/// <summary>
	/// SDL_ListModes(NULL, SDL_FULLSCREEN) replacement, set by the host: the available fullscreen modes, largest first.
	/// null (the default) behaves like a failed SDL_ListModes: no modes, the resolution arrows are hidden.
	/// </summary>
	public static Func<IReadOnlyList<(int Width, int Height)>> FullscreenModes { get; set; }

	private readonly InteractiveSurface _displaySurface;
	private readonly Text _txtDisplayResolution, _txtDisplayX;
	private readonly TextEdit _txtDisplayWidth, _txtDisplayHeight;
	private readonly ArrowButton _btnDisplayResolutionUp, _btnDisplayResolutionDown;
	private readonly Text _txtLanguage, _txtFilter, _txtGeoScale, _txtBattleScale;
	private readonly ComboBox _cbxLanguage, _cbxFilter, _cbxGeoScale, _cbxBattleScale;
	private readonly Text _txtMode;
	private readonly ComboBox _cbxDisplayMode;
	private readonly Text _txtOptions;
	private readonly ToggleTextButton _btnLetterbox, _btnLockMouse, _btnRootWindowedMode;
	private readonly IReadOnlyList<(int Width, int Height)> _res;
	private int _resAmount, _resCurrent;
	private readonly List<string> _langs = [], _filters = [];
	private readonly List<int> _scales = [], _reverseScales = [];

	/// <summary>Initializes all the elements in the Video Options screen.</summary>
	/// <param name="origin">Game section that originated this state.</param>
	public OptionsVideoState(OptionsOrigin origin) : base(origin)
	{
		SetCategory(_btnVideo);

		// Create objects
		_displaySurface = new InteractiveSurface(110, 32, 94, 18);
		_txtDisplayResolution = new Text(114, 9, 94, 8);
		_txtDisplayWidth = new TextEdit(this, 40, 17, 94, 26);
		_txtDisplayX = new Text(16, 17, 132, 26);
		_txtDisplayHeight = new TextEdit(this, 40, 17, 144, 26);
		_btnDisplayResolutionUp = new ArrowButton(ArrowShape.ARROW_BIG_UP, 14, 14, 186, 18);
		_btnDisplayResolutionDown = new ArrowButton(ArrowShape.ARROW_BIG_DOWN, 14, 14, 186, 36);

		_txtLanguage = new Text(114, 9, 94, 52);
		_cbxLanguage = new ComboBox(this, 104, 16, 94, 62);

		_txtFilter = new Text(114, 9, 206, 52);
		_cbxFilter = new ComboBox(this, 104, 16, 206, 62);

		_txtMode = new Text(114, 9, 206, 22);
		_cbxDisplayMode = new ComboBox(this, 104, 16, 206, 32);

		_txtGeoScale = new Text(114, 9, 94, 82);
		_cbxGeoScale = new ComboBox(this, 104, 16, 94, 92);

		_txtBattleScale = new Text(114, 9, 94, 112);
		_cbxBattleScale = new ComboBox(this, 104, 16, 94, 122);

		_txtOptions = new Text(114, 9, 206, 82);
		_btnLetterbox = new ToggleTextButton(104, 16, 206, 92);
		_btnLockMouse = new ToggleTextButton(104, 16, 206, 110);
		_btnRootWindowedMode = new ToggleTextButton(104, 16, 206, 128);

		// Get available fullscreen modes
		_res = FullscreenModes?.Invoke();
		if (_res != null && _res.Count > 0)
		{
			int i;
			_resCurrent = -1;
			for (i = 0; i < _res.Count; ++i)
			{
				if (_resCurrent == -1 &&
					((_res[i].Width == Options.displayWidth && _res[i].Height <= Options.displayHeight) || _res[i].Width < Options.displayWidth))
				{
					_resCurrent = i;
				}
			}
			_resAmount = i;
		}
		else
		{
			_res = null;
			_resCurrent = -1;
			_resAmount = 0;
			_btnDisplayResolutionDown.Visible = false;
			_btnDisplayResolutionUp.Visible = false;
			Logger.Warning("Couldn't get display resolutions");
		}

		Add(_displaySurface);
		Add(_txtDisplayResolution, "text", "videoMenu");
		Add(_txtDisplayWidth, "resolution", "videoMenu");
		Add(_txtDisplayX, "resolution", "videoMenu");
		Add(_txtDisplayHeight, "resolution", "videoMenu");
		Add(_btnDisplayResolutionUp, "button", "videoMenu");
		Add(_btnDisplayResolutionDown, "button", "videoMenu");

		Add(_txtLanguage, "text", "videoMenu");
		Add(_txtFilter, "text", "videoMenu");

		Add(_txtMode, "text", "videoMenu");

		Add(_txtOptions, "text", "videoMenu");
		Add(_btnLetterbox, "button", "videoMenu");
		Add(_btnLockMouse, "button", "videoMenu");
		Add(_btnRootWindowedMode, "button", "videoMenu");

		Add(_cbxFilter, "button", "videoMenu");
		Add(_cbxDisplayMode, "button", "videoMenu");

		Add(_txtBattleScale, "text", "videoMenu");
		Add(_cbxBattleScale, "button", "videoMenu");

		Add(_txtGeoScale, "text", "videoMenu");
		Add(_cbxGeoScale, "button", "videoMenu");

		Add(_cbxLanguage, "button", "videoMenu");
		CenterAllSurfaces();

		// Set up objects
		_txtDisplayResolution.SetText(Tr("STR_DISPLAY_RESOLUTION"));

		_displaySurface.Tooltip = "STR_DISPLAY_RESOLUTION_DESC";
		_displaySurface.OnMouseIn(TxtTooltipIn);
		_displaySurface.OnMouseOut(TxtTooltipOut);

		_txtDisplayWidth.SetAlign(TextHAlign.ALIGN_CENTER);
		_txtDisplayWidth.SetBig();
		_txtDisplayWidth.SetConstraint(TextEditConstraint.TEC_NUMERIC_POSITIVE);
		_txtDisplayWidth.OnChange(TxtDisplayWidthChange);

		_txtDisplayX.Align = TextHAlign.ALIGN_CENTER;
		_txtDisplayX.SetBig();
		_txtDisplayX.SetText("x");

		_txtDisplayHeight.SetAlign(TextHAlign.ALIGN_CENTER);
		_txtDisplayHeight.SetBig();
		_txtDisplayHeight.SetConstraint(TextEditConstraint.TEC_NUMERIC_POSITIVE);
		_txtDisplayHeight.OnChange(TxtDisplayHeightChange);

		_txtDisplayWidth.Text = Options.displayWidth.ToString(CultureInfo.InvariantCulture);
		_txtDisplayHeight.Text = Options.displayHeight.ToString(CultureInfo.InvariantCulture);

		_btnDisplayResolutionUp.OnMouseClick(BtnDisplayResolutionUpClick);
		_btnDisplayResolutionDown.OnMouseClick(BtnDisplayResolutionDownClick);

		_txtMode.SetText(Tr("STR_DISPLAY_MODE"));

		_txtOptions.SetText(Tr("STR_DISPLAY_OPTIONS"));

		_btnLetterbox.Text = Tr("STR_LETTERBOXED");
		_btnLetterbox.Pressed = Options.keepAspectRatio;
		_btnLetterbox.OnMouseClick(BtnLetterboxClick);
		_btnLetterbox.Tooltip = "STR_LETTERBOXED_DESC";
		_btnLetterbox.OnMouseIn(TxtTooltipIn);
		_btnLetterbox.OnMouseOut(TxtTooltipOut);

		_btnLockMouse.Text = Tr("STR_LOCK_MOUSE");
		_btnLockMouse.Pressed = Options.captureMouse == SDL_GrabMode.SDL_GRAB_ON;
		_btnLockMouse.OnMouseClick(BtnLockMouseClick);
		_btnLockMouse.Tooltip = "STR_LOCK_MOUSE_DESC";
		_btnLockMouse.OnMouseIn(TxtTooltipIn);
		_btnLockMouse.OnMouseOut(TxtTooltipOut);

		_btnRootWindowedMode.Text = Tr("STR_FIXED_WINDOW_POSITION");
		_btnRootWindowedMode.Pressed = Options.rootWindowedMode;
		_btnRootWindowedMode.OnMouseClick(BtnRootWindowedModeClick);
		_btnRootWindowedMode.Tooltip = "STR_FIXED_WINDOW_POSITION_DESC";
		_btnRootWindowedMode.OnMouseIn(TxtTooltipIn);
		_btnRootWindowedMode.OnMouseOut(TxtTooltipOut);

		_txtLanguage.SetText(Tr("STR_DISPLAY_LANGUAGE"));

		var names = new List<string>();
		Language.GetList(_langs, names);
		_cbxLanguage.SetOptions(names);
		for (int i = 0; i < names.Count; ++i)
		{
			if (_langs[i] == Options.language)
			{
				_cbxLanguage.Selected = i;
				break;
			}
		}
		_cbxLanguage.OnChange(CbxLanguageChange);
		_cbxLanguage.Tooltip = "STR_DISPLAY_LANGUAGE_DESC";
		_cbxLanguage.OnMouseIn(TxtTooltipIn);
		_cbxLanguage.OnMouseOut(TxtTooltipOut);

		var filterNames = new List<string>
		{
			Tr("STR_DISABLED"),
			"Scale",
			"HQx",
			"xBRZ",
		};
		_filters.Add("");
		_filters.Add("");
		_filters.Add("");
		_filters.Add("");

		// the shaders stay selectable (the OpenGL build of the original); applying them is a host matter
		var filters = new List<string>(FileMap.FilterFiles(FileMap.GetVFolderContents(GL_FOLDER), GL_EXT));
		filters.Sort(Unicode.NaturalComparison);
		foreach (var file in filters)
		{
			string path = GL_FOLDER + file;
			string name = file[..(file.Length - GL_EXT.Length - 1)] + GL_STRING;
			filterNames.Add(UcWords(name));
			_filters.Add(path);
		}

		int selFilter = 0;
		if (Options.useOpenGL) // Screen::useOpenGL()
		{
			string path = Options.useOpenGLShader;
			for (int i = 0; i < _filters.Count; ++i)
			{
				if (_filters[i] == path)
				{
					selFilter = i;
					break;
				}
			}
		}
		else if (Options.useScaleFilter)
		{
			selFilter = 1;
		}
		else if (Options.useHQXFilter)
		{
			selFilter = 2;
		}
		else if (Options.useXBRZFilter)
		{
			selFilter = 3;
		}

		_txtFilter.SetText(Tr("STR_DISPLAY_FILTER"));

		_cbxFilter.SetOptions(filterNames);
		_cbxFilter.Selected = selFilter;
		_cbxFilter.OnChange(CbxFilterChange);
		_cbxFilter.Tooltip = "STR_DISPLAY_FILTER_DESC";
		_cbxFilter.OnMouseIn(TxtTooltipIn);
		_cbxFilter.OnMouseOut(TxtTooltipOut);

		var displayModes = new List<string>
		{
			Tr("STR_WINDOWED"),
			Tr("STR_FULLSCREEN"),
			Tr("STR_BORDERLESS"),
			Tr("STR_RESIZABLE"),
		};

		int displayMode = 0;
		if (Options.fullscreen)
		{
			displayMode = 1;
		}
		else if (Options.borderless)
		{
			displayMode = 2;
		}
		else if (Options.allowResize)
		{
			displayMode = 3;
		}

		_cbxDisplayMode.SetOptions(displayModes);
		_cbxDisplayMode.Selected = displayMode;
		_cbxDisplayMode.OnChange(UpdateDisplayMode);
		_cbxDisplayMode.Tooltip = "STR_DISPLAY_MODE_DESC";
		_cbxDisplayMode.OnMouseIn(TxtTooltipIn);
		_cbxDisplayMode.OnMouseOut(TxtTooltipOut);

		_txtGeoScale.SetText(Tr("STR_GEOSCAPE_SCALE"));

		var scales = new List<string>
		{
			"1x", // was 5 -> is 0
			"2x", // was 4 -> is 1
			"3x", // was 3 -> is 2
			"4x", // was 6 -> is 3
			"5x", // was 7 -> is 4
			"6x", // was 8 -> is 5
			"320x200", // was 0 -> is 6
			"480x300", // was 1 -> is 7
			"640x400", // was 2 -> is 8
			"8x",  // new 9
			"10x", // new 10
		};

		_scales.AddRange([6, 7, 8, 2, 1, 0, 3, 4, 5, 9, 10]);
		_reverseScales.AddRange([5, 4, 3, 6, 7, 8, 0, 1, 2, 9, 10]);

		if (Options.geoscapeScale < 0 || Options.geoscapeScale > 10) Options.geoscapeScale = 0;
		if (Options.battlescapeScale < 0 || Options.battlescapeScale > 10) Options.battlescapeScale = 0;

		_cbxGeoScale.SetOptions(scales);
		_cbxGeoScale.Selected = _scales[Options.geoscapeScale];
		_cbxGeoScale.OnChange(UpdateGeoscapeScale);
		_cbxGeoScale.Tooltip = "STR_GEOSCAPESCALE_SCALE_DESC";
		_cbxGeoScale.OnMouseIn(TxtTooltipIn);
		_cbxGeoScale.OnMouseOut(TxtTooltipOut);

		_txtBattleScale.SetText(Tr("STR_BATTLESCAPE_SCALE"));

		_cbxBattleScale.SetOptions(scales);
		_cbxBattleScale.Selected = _scales[Options.battlescapeScale];
		_cbxBattleScale.OnChange(UpdateBattlescapeScale);
		_cbxBattleScale.Tooltip = "STR_BATTLESCAPE_SCALE_DESC";
		_cbxBattleScale.OnMouseIn(TxtTooltipIn);
		_cbxBattleScale.OnMouseOut(TxtTooltipOut);
	}

	/// <summary>Uppercases all the words in a string (spaces, dashes and underscores become spaces).</summary>
	/// <param name="str">Source string.</param>
	/// <returns>Destination string.</returns>
	internal static string UcWords(string str)
	{
		var s = str.ToCharArray();
		for (int i = 0; i < s.Length; ++i)
		{
			if (i == 0)
				s[0] = ToUpper(s[0]);
			else if (s[i] == ' ' || s[i] == '-' || s[i] == '_')
			{
				s[i] = ' ';
				if (s.Length > i + 1)
					s[i + 1] = ToUpper(s[i + 1]);
			}
		}
		return new string(s);
	}

	/// <summary>C toupper in the "C" locale: only ASCII letters change.</summary>
	internal static char ToUpper(char c) => c is >= 'a' and <= 'z' ? (char)(c - 'a' + 'A') : c;

	/// <summary>Selects a bigger display resolution.</summary>
	public void BtnDisplayResolutionUpClick(Action action)
	{
		if (_resAmount == 0)
			return;
		if (_resCurrent <= 0)
		{
			_resCurrent = _resAmount - 1;
		}
		else
		{
			_resCurrent--;
		}
		UpdateDisplayResolution();
	}

	/// <summary>Selects a smaller display resolution.</summary>
	public void BtnDisplayResolutionDownClick(Action action)
	{
		if (_resAmount == 0)
			return;
		if (_resCurrent >= _resAmount - 1)
		{
			_resCurrent = 0;
		}
		else
		{
			_resCurrent++;
		}
		UpdateDisplayResolution();
	}

	/// <summary>Updates the display resolution based on the selection.</summary>
	private void UpdateDisplayResolution()
	{
		_txtDisplayWidth.Text = _res[_resCurrent].Width.ToString(CultureInfo.InvariantCulture);
		_txtDisplayHeight.Text = _res[_resCurrent].Height.ToString(CultureInfo.InvariantCulture);

		Options.newDisplayWidth = _res[_resCurrent].Width;
		Options.newDisplayHeight = _res[_resCurrent].Height;
	}

	/// <summary>Reads a leading decimal integer like <c>ss &gt;&gt; std::dec &gt;&gt; i</c>: 0 without digits, clamped on overflow.</summary>
	internal static int ParseLeadingInt(string text)
	{
		int i = 0;
		while (i < text.Length && char.IsWhiteSpace(text[i]))
			i++;
		int start = i;
		if (i < text.Length && (text[i] == '+' || text[i] == '-'))
			i++;
		int digits = i;
		while (i < text.Length && text[i] >= '0' && text[i] <= '9')
			i++;
		if (i == digits)
			return 0;
		if (!int.TryParse(text.AsSpan(start, i - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
			return text[start] == '-' ? int.MinValue : int.MaxValue;
		return value;
	}

	/// <summary>Updates the resolution mode after the width or height was typed in.</summary>
	private void UpdateResCurrent()
	{
		if (_res != null)
		{
			int i;
			_resCurrent = -1;
			for (i = 0; i < _res.Count; ++i)
			{
				if (_resCurrent == -1 &&
					((_res[i].Width == Options.newDisplayWidth && _res[i].Height <= Options.newDisplayHeight) || _res[i].Width < Options.newDisplayWidth))
				{
					_resCurrent = i;
				}
			}
		}
	}

	/// <summary>Changes the Display Width option.</summary>
	public void TxtDisplayWidthChange(Action action)
	{
		Options.newDisplayWidth = ParseLeadingInt(_txtDisplayWidth.Text);
		// Update resolution mode
		UpdateResCurrent();
	}

	/// <summary>Changes the Display Height option.</summary>
	public void TxtDisplayHeightChange(Action action)
	{
		Options.newDisplayHeight = ParseLeadingInt(_txtDisplayHeight.Text);
		// Update resolution mode
		UpdateResCurrent();
	}

	/// <summary>Changes the Language option.</summary>
	public void CbxLanguageChange(Action action)
	{
		Options.language = _langs[_cbxLanguage.Selected];
	}

	/// <summary>Changes the Filter options.</summary>
	public void CbxFilterChange(Action action)
	{
		switch (_cbxFilter.Selected)
		{
			case 0:
				Options.newOpenGL = false;
				Options.newScaleFilter = false;
				Options.newHQXFilter = false;
				Options.newXBRZFilter = false;
				break;
			case 1:
				Options.newOpenGL = false;
				Options.newScaleFilter = true;
				Options.newHQXFilter = false;
				Options.newXBRZFilter = false;
				break;
			case 2:
				Options.newOpenGL = false;
				Options.newScaleFilter = false;
				Options.newHQXFilter = true;
				Options.newXBRZFilter = false;
				break;
			case 3:
				Options.newOpenGL = false;
				Options.newScaleFilter = false;
				Options.newHQXFilter = false;
				Options.newXBRZFilter = true;
				break;
			default:
				Options.newOpenGL = true;
				Options.newScaleFilter = false;
				Options.newHQXFilter = false;
				Options.newXBRZFilter = false;
				Options.newOpenGLShader = _filters[_cbxFilter.Selected];
				break;
		}
	}

	/// <summary>Changes the Display Mode options.</summary>
	public void UpdateDisplayMode(Action action)
	{
		switch (_cbxDisplayMode.Selected)
		{
			case 0:
				Options.newFullscreen = false;
				Options.newBorderless = false;
				Options.newAllowResize = false;
				break;
			case 1:
				Options.newFullscreen = true;
				Options.newBorderless = false;
				Options.newAllowResize = false;
				break;
			case 2:
				Options.newFullscreen = false;
				Options.newBorderless = true;
				Options.newAllowResize = false;
				break;
			case 3:
				Options.newFullscreen = false;
				Options.newBorderless = false;
				Options.newAllowResize = true;
				break;
			default:
				break;
		}
	}

	/// <summary>Changes the Letterboxing option.</summary>
	public void BtnLetterboxClick(Action action)
	{
		Options.keepAspectRatio = _btnLetterbox.Pressed;
	}

	/// <summary>Changes the Lock Mouse option.</summary>
	public void BtnLockMouseClick(Action action)
	{
		Options.captureMouse = _btnLockMouse.Pressed ? SDL_GrabMode.SDL_GRAB_ON : SDL_GrabMode.SDL_GRAB_OFF;
		// missing: SDL_WM_GrabInput(Options::captureMouse); (host matter)
	}

	/// <summary>Ask user where he wants to root screen.</summary>
	public void BtnRootWindowedModeClick(Action action)
	{
		if (_btnRootWindowedMode.Pressed)
		{
			_game.PushState(new SetWindowedRootState(_origin, this));
		}
		else
		{
			Options.newRootWindowedMode = false;
		}
	}

	/// <summary>Updates the Battlescape scale.</summary>
	public void UpdateBattlescapeScale(Action action)
	{
		Options.newBattlescapeScale = _reverseScales[_cbxBattleScale.Selected];
	}

	/// <summary>Changes the geoscape scale.</summary>
	public void UpdateGeoscapeScale(Action action)
	{
		Options.newGeoscapeScale = _reverseScales[_cbxGeoScale.Selected];
	}

	/// <summary>Updates the scale.</summary>
	/// <param name="dX">delta of X;</param>
	/// <param name="dY">delta of Y;</param>
	public override void Resize(ref int dX, ref int dY)
	{
		base.Resize(ref dX, ref dY);
		_txtDisplayWidth.Text = Options.displayWidth.ToString(CultureInfo.InvariantCulture);
		_txtDisplayHeight.Text = Options.displayHeight.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>Takes care of any events from the core game engine.</summary>
	public override void Handle(Action action)
	{
		base.Handle(action);
		if (action.Details.type == SDLEventType.SDL_KEYDOWN && action.Details.key.keysym.sym == SDLKey.SDLK_g && _game.IsCtrlPressed())
		{
			_btnLockMouse.Pressed = Options.captureMouse == SDL_GrabMode.SDL_GRAB_ON;
		}
	}

	/// <summary>Unpresses Fixed Borderless Pos button.</summary>
	public void UnpressRootWindowedMode()
	{
		_btnRootWindowedMode.Pressed = false;
	}
}
