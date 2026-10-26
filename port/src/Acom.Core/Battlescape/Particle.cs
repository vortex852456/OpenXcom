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
// Ported from: src/Battlescape/Particle.h, src/Battlescape/Particle.cpp
#nullable disable

namespace Acom;

/// <summary>
/// A vapor particle (C++ value type: copy with the copy constructor).
/// </summary>
public class Particle
{
	public const int SubVoxelAccuracy = 256;
	public const int LayerAccuracy = 2;

	private Position _subVoxelPos;
	private Position _screenData;
	private Position _subVoxelVelocity;
	private readonly Position _subVoxelAcceleration;
	private readonly byte _drift;
	private byte _layerZ;
	private readonly byte _color;
	private byte _opacity;
	private readonly byte _size;

	/// <summary>Create a particle.</summary>
	/// <param name="voxelPos">Position in voxel space.</param>
	/// <param name="subVoxelOffset">Offset in sub voxel space.</param>
	/// <param name="subVoxelVel">Velocity in sub voxel space.</param>
	/// <param name="subVoxelAcc">Acceleration in sub voxel space.</param>
	/// <param name="drift">Random movement per frame in sub voxel space.</param>
	/// <param name="color">The color set to use from the transparency LUTs.</param>
	/// <param name="opacity">Starting opacity (lifetime) of the particle.</param>
	/// <param name="size">Size of the particle.</param>
	public Particle(Position voxelPos, Position subVoxelOffset, Position subVoxelVel, Position subVoxelAcc, byte drift, byte color, byte opacity, byte size)
	{
		_drift = drift;
		_color = color;
		_opacity = opacity;
		_size = size;
		_layerZ = (byte)((voxelPos.z / Position.TileZ) << 1);

		_subVoxelPos = voxelPos.ClipVoxel() * SubVoxelAccuracy;
		_subVoxelPos += subVoxelOffset;

		_subVoxelVelocity = subVoxelVel;
		_subVoxelAcceleration = subVoxelAcc;
	}

	/// <summary>Default copy constructor.</summary>
	public Particle(Particle other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_subVoxelPos = other._subVoxelPos;
		_screenData = other._screenData;
		_subVoxelVelocity = other._subVoxelVelocity;
		_subVoxelAcceleration = other._subVoxelAcceleration;
		_drift = other._drift;
		_layerZ = other._layerZ;
		_color = other._color;
		_opacity = other._opacity;
		_size = other._size;
	}

	/// <summary>Animates the particle.</summary>
	/// <returns>If the particle is still alive (opacity above zero).</returns>
	public bool Animate()
	{
		_opacity--;

		_subVoxelPos.x = (short)(_subVoxelPos.x + Rng.Seedless(-_drift, _drift));
		_subVoxelPos.y = (short)(_subVoxelPos.y + Rng.Seedless(-_drift, _drift));
		_subVoxelPos.z = (short)(_subVoxelPos.z + Rng.Seedless(-_drift, _drift));
		_subVoxelPos += _subVoxelVelocity;
		_subVoxelVelocity += _subVoxelAcceleration;

		if (_opacity == 0)
		{
			return false;
		}
		return true;
	}

	/// <summary>Updates the screen data, keeps the particle inside its tile.</summary>
	/// <returns>Offset (-1, 0 or +1 per axis) of the tile the particle moved to.</returns>
	public Position UpdateScreenPosition()
	{
		Position one = new(1, 1, 1);
		Position scale = one.ToVoxel() * SubVoxelAccuracy;

		// this convert postion to -1, 0 or +1 depending if _subVoxelPos is outside current tile
		Position tileOffset = (_subVoxelPos + scale) / scale - one;

		// keep values inside one tile
		if (tileOffset.x != 0)
		{
			_subVoxelPos.x = (short)(_subVoxelPos.x - tileOffset.x * Position.TileXY * SubVoxelAccuracy);
		}
		if (tileOffset.y != 0)
		{
			_subVoxelPos.y = (short)(_subVoxelPos.y - tileOffset.y * Position.TileXY * SubVoxelAccuracy);
		}
		if (tileOffset.z != 0)
		{
			_subVoxelPos.z = (short)(_subVoxelPos.z - tileOffset.z * Position.TileZ * SubVoxelAccuracy);
			_layerZ = (byte)(_layerZ + tileOffset.z * LayerAccuracy);
		}

		// voxel closer to front of screen are consider on higher layer
		_layerZ &= 0xFE; //cut smallest bit
		_layerZ |= (byte)(_subVoxelPos.x + _subVoxelPos.y > Position.TileXY * SubVoxelAccuracy ? 1 : 0);

		Position v = _subVoxelPos / SubVoxelAccuracy;
		_screenData.x = (short)(v.x - v.y);
		_screenData.y = (short)((v.x / 2) + (v.y / 2) - v.z - TileZ * Position.TileZ);
		_screenData.z = (short)Math.Min((_opacity + 7) / 10, Mod.TransparenciesOpacityLevels - 1);

		return tileOffset;
	}

	/// <summary>Get the size value.</summary>
	public int Size => _size;

	/// <summary>Get the color.</summary>
	public byte Color => _color;

	/// <summary>Get the opacity.</summary>
	public byte Opacity => (byte)_screenData.z;

	/// <summary>Gets screen offset X relative to tile.</summary>
	public int OffsetX => _screenData.x;

	/// <summary>Gets screen offset Y relative to tile.</summary>
	public int OffsetY => _screenData.y;

	/// <summary>Get layer position of particle.</summary>
	public int LayerZ => _layerZ;

	/// <summary>Get tile position of particle.</summary>
	public int TileZ => _layerZ / LayerAccuracy;
}
