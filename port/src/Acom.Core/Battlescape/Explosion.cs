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
// Ported from: src/Battlescape/Explosion.h, src/Battlescape/Explosion.cpp
#nullable disable

namespace Acom;

/// <summary>
/// A class that represents an explosion animation. Map is the owner of an instance of this class during its short life.
/// It represents both a bullet hit, as a real explosion animation.
/// </summary>
public class Explosion
{
	public const int HIT_FRAMES = 4;
	public const int EXPLODE_FRAMES = 8;
	public const int BULLET_FRAMES = 10;

	private readonly Position _position;
	private int _currentFrame;
	private readonly int _startFrame;
	private int _frameDelay;
	private readonly bool _big, _hit;
	private int _frames;

	/// <summary>Creates a new Explosion.</summary>
	/// <param name="position">Explosion center position in voxel x/y/z.</param>
	/// <param name="startFrame">A start frame number.</param>
	/// <param name="frameDelay">Delay before the animation starts.</param>
	/// <param name="big">Flag to indicate it is a bullet hit (false), or a real explosion (true).</param>
	/// <param name="hit">True for melee and psi attacks.</param>
	/// <param name="frames">Number of frames (non-positive: default for the explosion type).</param>
	public Explosion(Position position, int startFrame, int frameDelay = 0, bool big = false, bool hit = false, int frames = -1)
	{
		_position = position;
		_currentFrame = startFrame;
		_startFrame = startFrame;
		_frameDelay = frameDelay;
		_big = big;
		_hit = hit;
		_frames = frames;
	}

	/// <summary>Animates the explosion further.</summary>
	/// <returns>If the animation is finished (false).</returns>
	public bool Animate()
	{
		if (_frameDelay > 0)
		{
			_frameDelay--;
			return true;
		}

		if (_frames <= 0)
		{
			if (_hit)
			{
				_frames = HIT_FRAMES;
			}
			else if (_big)
			{
				_frames = EXPLODE_FRAMES;
			}
			else
			{
				_frames = BULLET_FRAMES;
			}
		}

		_currentFrame++;
		if (_currentFrame == _startFrame + _frames)
		{
			return false;
		}
		else
		{
			return true;
		}
	}

	/// <summary>Gets the current position in voxel space.</summary>
	public Position Position => _position;

	/// <summary>Gets the current frame in the animation (-1 while the animation has not started).</summary>
	public int CurrentFrame
	{
		get
		{
			if (_frameDelay > 0)
				return -1;
			return _currentFrame;
		}
	}

	/// <summary>Checks if this is a real explosion.</summary>
	public bool IsBig => _big;

	/// <summary>Checks if this is a melee or psi hit.</summary>
	public bool IsHit => _hit;
}
