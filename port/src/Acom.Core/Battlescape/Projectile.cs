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
// Ported from: src/Battlescape/Projectile.h, src/Battlescape/Projectile.cpp
// Port status: partial (missing: script hooks - vaporParticleAmmo/vaporParticleWeapon; the built-in default vaporParticleAmmo script is applied instead)
#nullable disable

using System.Diagnostics;

namespace Acom;

/// <summary>
/// A class that represents a projectile. Map is the owner of an instance of this class during its short life.
/// It calculates its own trajectory and then moves along this pre-calculated trajectory in voxel space.
/// </summary>
public class Projectile
{
	/// <summary>Offset of voxel path where item should be drop.</summary>
	public const int ItemDropVoxelOffset = -2;

	private readonly Mod _mod;
	private readonly SavedBattleGame _save;
	private readonly BattleAction _action;
	private readonly BattleItem _ammo;
	private readonly Position _origin;
	private Position _targetVoxel;
	private readonly List<Position> _trajectory = [];
	private int _position;
	private float _distance;
	private float _distanceMax;
	private readonly int _speed;
	private readonly int _bulletSprite;
	private readonly bool _reversed;
	private readonly int _vaporColor, _vaporDensity, _vaporProbability;

	/// <summary>
	/// Sets up a Projectile.
	/// </summary>
	/// <param name="mod">Pointer to mod.</param>
	/// <param name="save">Pointer to battle savegame.</param>
	/// <param name="action">An action (copied).</param>
	/// <param name="origin">Position the projectile originates from.</param>
	/// <param name="target">Position the projectile is targeting.</param>
	/// <param name="ammo">The ammo that produced this projectile, where applicable.</param>
	public Projectile(Mod mod, SavedBattleGame save, BattleAction action, Position origin, Position target, BattleItem ammo)
	{
		_mod = mod;
		_save = save;
		_action = new BattleAction(action);
		_ammo = ammo;
		_origin = origin;
		_targetVoxel = target;
		_position = 0;
		_distance = 0.0f;
		_bulletSprite = -1;
		_reversed = false;
		_vaporColor = -1;
		_vaporDensity = -1;
		_vaporProbability = 5;

		// this is the number of pixels the sprite will move between frames
		_speed = Options.battleFireSpeed;
		if (_action.weapon != null)
		{
			if (_action.type != BattleActionType.BA_THROW)
			{
				Debug.Assert(_ammo != null, "missing ammo for Projectile");

				// try to get all the required info from the ammo
				_bulletSprite = _ammo.Rules.BulletSprite;
				_vaporColor = _ammo.Rules.GetVaporColor(_save.Depth);
				_vaporDensity = _ammo.Rules.GetVaporDensity(_save.Depth);
				_vaporProbability = _ammo.Rules.GetVaporProbability(_save.Depth);
				_speed = Math.Max(1, _speed + _ammo.Rules.BulletSpeed);

				// the ammo didn't contain the info we wanted, see what the weapon has on offer.
				if (_bulletSprite == Mod.NO_SURFACE)
				{
					_bulletSprite = _action.weapon.Rules.BulletSprite;
				}
				if (_vaporColor == -1)
				{
					_vaporColor = _action.weapon.Rules.GetVaporColor(_save.Depth);
				}
				if (_vaporDensity == -1)
				{
					_vaporDensity = _action.weapon.Rules.GetVaporDensity(_save.Depth);
				}
				if (_vaporProbability == 5)
				{
					_vaporProbability = _action.weapon.Rules.GetVaporProbability(_save.Depth);
				}
				if (ammo == null || (ammo != _action.weapon || ammo.Rules.BulletSpeed == 0))
				{
					_speed = Math.Max(1, _speed + _action.weapon.Rules.BulletSpeed);
				}
			}
		}
		if ((target.x - origin.x) + (target.y - origin.y) >= 0)
		{
			_reversed = true;
		}
	}

	/// <summary>
	/// Calculates the trajectory for a straight path.
	/// </summary>
	/// <param name="accuracy">The unit's accuracy.</param>
	/// <returns>The objectnumber(0-3) or unit(4) or out of map (5) or -1 (no line of fire).</returns>
	public int CalculateTrajectory(double accuracy)
	{
		Position originVoxel = _save.TileEngine.GetOriginVoxel(_action, _save.GetTile(_origin));
		return CalculateTrajectory(accuracy, originVoxel);
	}

	/// <summary>
	/// Calculates the trajectory for a straight path from a given origin voxel.
	/// </summary>
	/// <param name="accuracy">The unit's accuracy.</param>
	/// <param name="originVoxel">Start of the trajectory in voxel space.</param>
	/// <param name="excludeUnit">Exclude the shooter from the first line check.</param>
	/// <returns>The objectnumber(0-3) or unit(4) or out of map (5) or -1 (no line of fire).</returns>
	public int CalculateTrajectory(double accuracy, Position originVoxel, bool excludeUnit = true)
	{
		Tile targetTile = _save.GetTile(_action.target);
		BattleUnit bu = _action.actor;

		_distance = 0.0f;
		int test;
		if (excludeUnit)
		{
			test = (int)_save.TileEngine.CalculateLineVoxel(originVoxel, _targetVoxel, false, _trajectory, bu, null);
		}
		else
		{
			test = (int)_save.TileEngine.CalculateLineVoxel(originVoxel, _targetVoxel, false, _trajectory, null, null);
		}

		if (test != (int)VoxelType.V_EMPTY &&
			_trajectory.Count != 0 &&
			_action.actor.Faction == UnitFaction.FACTION_PLAYER &&
			_action.autoShotCounter == 1 &&
			(!_save.IsCtrlPressed(true) || !Options.forceFire) &&
			_save.BattleGame.PanicHandled &&
			_action.type != BattleActionType.BA_LAUNCH &&
			!_action.sprayTargeting)
		{
			Position hitPos = _trajectory[0].ToTile();
			if (test == (int)VoxelType.V_UNIT && _save.GetTile(hitPos) != null && _save.GetTile(hitPos).Unit == null) //no unit? must be lower
			{
				hitPos = new Position(hitPos.x, hitPos.y, hitPos.z - 1);
			}

			if (hitPos != _action.target && string.IsNullOrEmpty(_action.result))
			{
				if (test == (int)VoxelType.V_NORTHWALL)
				{
					if (hitPos.y - 1 != _action.target.y)
					{
						_trajectory.Clear();
						return (int)VoxelType.V_EMPTY;
					}
				}
				else if (test == (int)VoxelType.V_WESTWALL)
				{
					if (hitPos.x - 1 != _action.target.x)
					{
						_trajectory.Clear();
						return (int)VoxelType.V_EMPTY;
					}
				}
				else if (test == (int)VoxelType.V_UNIT)
				{
					BattleUnit hitUnit = _save.GetTile(hitPos).Unit;
					BattleUnit targetUnit = targetTile.Unit; // Note: hitPos could be 1 tile lower and hitUnit could be on both tiles; change in OXC?
					if (hitUnit != targetUnit)
					{
						_trajectory.Clear();
						return (int)VoxelType.V_EMPTY;
					}
				}
				else
				{
					_trajectory.Clear();
					return (int)VoxelType.V_EMPTY;
				}
			}
		}

		_trajectory.Clear();

		bool extendLine = true;
		// even guided missiles drift, but how much is based on
		// the shooter's faction, rather than accuracy.
		if (_action.type == BattleActionType.BA_LAUNCH)
		{
			if (_action.actor.Faction == UnitFaction.FACTION_PLAYER)
			{
				accuracy = 0.60;
			}
			else
			{
				accuracy = 0.55;
			}
			extendLine = _action.waypoints.Count <= 1;
		}

		// apply some accuracy modifiers.
		// This will results in a new target voxel
		ApplyAccuracy(originVoxel, ref _targetVoxel, accuracy, false, extendLine);

		// finally do a line calculation and store this trajectory.
		return (int)_save.TileEngine.CalculateLineVoxel(originVoxel, _targetVoxel, true, _trajectory, bu, null);
	}

	/// <summary>
	/// Calculates the trajectory for a curved path.
	/// </summary>
	/// <param name="accuracy">The unit's accuracy.</param>
	/// <returns>The voxel type that was hit (V_OUTOFBOUNDS when no trajectory is possible).</returns>
	public int CalculateThrow(double accuracy)
	{
		Tile targetTile = _save.GetTile(_action.target);

		Position originVoxel = _save.TileEngine.GetOriginVoxel(_action, null);
		Position targetVoxel;
		List<Position> targets = [];
		double curvature = 0.0; // uninitialized in C++ (only read after validateThrow set it)
		targetVoxel = _action.target.ToVoxel() + new Position(8, 8, 1 + -targetTile.TerrainLevel);
		targets.Clear();
		bool forced = false;

		if (_action.type == BattleActionType.BA_THROW)
		{
			targets.Add(targetVoxel);
		}
		else
		{
			BattleUnit tu = targetTile.GetOverlappingUnit(_save);
			if (Options.forceFire && _save.IsCtrlPressed(true) && _save.Side == UnitFaction.FACTION_PLAYER)
			{
				targets.Add(_action.target.ToVoxel() + new Position(0, 0, 12));
				forced = true;
			}
			else if (tu != null && ((_action.actor.Faction != UnitFaction.FACTION_PLAYER) ||
				tu.Visible))
			{ //unit
				targetVoxel.z = (short)(targetVoxel.z + tu.FloatHeight); //ground level is the base
				targets.Add(targetVoxel + new Position(0, 0, tu.Height / 2 + 1));
				targets.Add(targetVoxel + new Position(0, 0, 2));
				targets.Add(targetVoxel + new Position(0, 0, tu.Height - 1));
			}
			else if (targetTile.GetMapData(TilePart.O_OBJECT) != null)
			{
				targetVoxel = _action.target.ToVoxel() + new Position(8, 8, 0);
				targets.Add(targetVoxel + new Position(0, 0, 13));
				targets.Add(targetVoxel + new Position(0, 0, 8));
				targets.Add(targetVoxel + new Position(0, 0, 23));
				targets.Add(targetVoxel + new Position(0, 0, 2));
			}
			else if (targetTile.GetMapData(TilePart.O_NORTHWALL) != null)
			{
				targetVoxel = _action.target.ToVoxel() + new Position(8, 0, 0);
				targets.Add(targetVoxel + new Position(0, 0, 13));
				targets.Add(targetVoxel + new Position(0, 0, 8));
				targets.Add(targetVoxel + new Position(0, 0, 20));
				targets.Add(targetVoxel + new Position(0, 0, 3));
			}
			else if (targetTile.GetMapData(TilePart.O_WESTWALL) != null)
			{
				targetVoxel = _action.target.ToVoxel() + new Position(0, 8, 0);
				targets.Add(targetVoxel + new Position(0, 0, 13));
				targets.Add(targetVoxel + new Position(0, 0, 8));
				targets.Add(targetVoxel + new Position(0, 0, 20));
				targets.Add(targetVoxel + new Position(0, 0, 2));
			}
			else if (targetTile.GetMapData(TilePart.O_FLOOR) != null)
			{
				targets.Add(targetVoxel);
			}
		}

		_distance = 0.0f;
		int test = (int)VoxelType.V_OUTOFBOUNDS;
		foreach (Position pos in targets)
		{
			targetVoxel = pos;
			if (_save.TileEngine.ValidateThrow(_action, originVoxel, targetVoxel, _save.Depth, ref curvature, ref test, forced))
			{
				break;
			}
		}
		if (!forced && test == (int)VoxelType.V_OUTOFBOUNDS) return test; //no line of fire

		test = (int)VoxelType.V_OUTOFBOUNDS;
		int tries = 0;
		// finally do a line calculation and store this trajectory, make sure it's valid.
		while (test == (int)VoxelType.V_OUTOFBOUNDS && tries < 100)
		{
			++tries;
			Position deltas = targetVoxel;
			// apply some accuracy modifiers
			_trajectory.Clear();
			if (_action.type == BattleActionType.BA_THROW)
			{
				ApplyAccuracy(originVoxel, ref deltas, accuracy, true, false); //calling for best flavor
				deltas -= targetVoxel;
			}
			else
			{
				ApplyAccuracy(originVoxel, ref targetVoxel, accuracy, true, false); //arcing shot deviation
				deltas = new Position(0, 0, 0);
			}


			test = _save.TileEngine.CalculateParabolaVoxel(originVoxel, targetVoxel, true, _trajectory, _action.actor, curvature, deltas);
			if (forced) return (int)TilePart.O_OBJECT; //fake hit
			Position endPoint = GetPositionFromEnd(_trajectory, ItemDropVoxelOffset).ToTile();
			Tile endTile = _save.GetTile(endPoint);
			// check if the item would land on a tile with a blocking object
			if (_action.type == BattleActionType.BA_THROW
				&& endTile != null
				&& endTile.GetMapData(TilePart.O_OBJECT) != null
				&& endTile.GetMapData(TilePart.O_OBJECT).GetTUCost(MovementType.MT_WALK) == Pathfinding.INVALID_MOVE_COST
				&& !(endTile.IsBigWall && (endTile.GetMapData(TilePart.O_OBJECT).BigWall < 1 || endTile.GetMapData(TilePart.O_OBJECT).BigWall > 3)))
			{
				test = (int)VoxelType.V_OUTOFBOUNDS;
			}
		}
		return test;
	}

	/// <summary>fmath.h Deg2Rad.</summary>
	private static double Deg2Rad(double deg) => deg * Math.PI / 180.0;

	/// <summary>
	/// Calculates the new target in voxel space, based on the given accuracy modifier.
	/// </summary>
	/// <param name="origin">Start position of the trajectory in voxels.</param>
	/// <param name="target">Endpoint of the trajectory in voxels.</param>
	/// <param name="accuracy">Accuracy modifier.</param>
	/// <param name="keepRange">Whether range affects accuracy.</param>
	/// <param name="extendLine">should this line get extended to maximum distance?</param>
	private void ApplyAccuracy(Position origin, ref Position target, double accuracy, bool keepRange, bool extendLine)
	{
		int xdiff = origin.x - target.x;
		int ydiff = origin.y - target.y;
		int zdiff = origin.z - target.z;
		double realDistance = Math.Sqrt((double)(xdiff * xdiff) + (double)(ydiff * ydiff) + (double)(zdiff * zdiff));
		// maxRange is the maximum range a projectile shall ever travel in voxel space
		double maxRange = keepRange ? realDistance : 16 * 1000; // 1000 tiles
		maxRange = _action.type == BattleActionType.BA_HIT ? 46 : maxRange; // up to 2 tiles diagonally (as in the case of reaper v reaper)

		if (_action.type != BattleActionType.BA_HIT)
		{
			int upperLimit = 0, lowerLimit = 0;
			int dropoff = _action.weapon.Rules.CalculateLimits(ref upperLimit, ref lowerLimit, _save.Depth, _action.type);

			double distance = realDistance / 16; // distance in tiles, but still fractional
			double accuracyLoss = 0.0;
			if (distance > upperLimit)
			{
				accuracyLoss = (dropoff * (distance - upperLimit)) / 100;
			}
			else if (distance < lowerLimit)
			{
				accuracyLoss = (dropoff * (lowerLimit - distance)) / 100;
			}
			accuracy = Math.Max(0.0, accuracy - accuracyLoss);
		}

		int xDist = Math.Abs(origin.x - target.x);
		int yDist = Math.Abs(origin.y - target.y);
		int zDist = Math.Abs(origin.z - target.z);
		int xyShift, zShift;

		if (Options.oxceUniformShootingSpread) // Uniform shooting spread
		{
			if (xDist <= yDist)
				xyShift = xDist / 4 + yDist;
			else
				xyShift = xDist + yDist / 4;

			xyShift = (int)(xyShift * 0.839); // Constant to match average xyShift to vanilla
		}
		else
		{
			if (xDist / 2 <= yDist)				//yes, we need to add some x/y non-uniformity
				xyShift = xDist / 4 + yDist;	//and don't ask why, please. it's The Commandment
			else
				xyShift = (xDist + yDist) / 2;	//that's uniform part of spreading
		}

		if (xyShift <= zDist)				//slight z deviation
			zShift = xyShift / 2 + zDist;
		else
			zShift = xyShift + zDist / 2;

		// Apply penalty for having no LOS to target
		int noLOSAccuracyPenalty = _action.weapon.Rules.GetNoLOSAccuracyPenalty(_mod);
		if (noLOSAccuracyPenalty != -1)
		{
			Tile t = _save.GetTile(target.ToTile());
			if (t != null)
			{
				bool hasLOS;
				BattleUnit bu = _action.actor;
				BattleUnit targetUnit = t.Unit; // we can call TileEngine::visible() only if the target unit is on the same tile

				if (targetUnit != null)
				{
					hasLOS = _save.TileEngine.Visible(bu, t);
				}
				else
				{
					hasLOS = _save.TileEngine.IsTileInLOS(_action, t, false);
				}

				if (!hasLOS)
				{
					accuracy = accuracy * noLOSAccuracyPenalty / 100;
				}
			}
		}

		int deviation = (int)(Rng.Generate(0, 100) - (accuracy * 100));

		if (deviation >= 0)
			deviation += 50;				// add extra spread to "miss" cloud
		else
			deviation += 10;				//accuracy of 109 or greater will become 1 (tightest spread)

		deviation = Math.Max(1, zShift * deviation / 200);	//range ratio

		if (Options.oxceUniformShootingSpread)
		{
			// First, new target point is rolled as usual. Then, if it lies outside of outer circle (in square's corner)
			// it's rerolled inside inner circle

			const double SECONDARY_SPREAD_COEFF = 0.85; // Inner spread circle diameter compared to outer

			bool resultShifted = false;
			int dX = 0, dY = 0;

			for (int i = 0; i < 10; ++i) // Break from this cycle when proper target is found
			{
				dX = Rng.Generate(0, deviation) - deviation / 2;
				dY = Rng.Generate(0, deviation) - deviation / 2;

				int radiusSq = dX * dX + dY * dY;
				int deviateRadius = deviation / 2;
				int deviateRadiusSq = deviateRadius * deviateRadius;

				if (radiusSq <= deviateRadiusSq) break;  // If we inside of the spread circle - we're done!

				if (!resultShifted)
				{
					resultShifted = true;
					deviation = (int)(deviation * SECONDARY_SPREAD_COEFF); // Change spread radius for second+ attempts
				}
			}

			target.x = (short)(target.x + dX);
			target.y = (short)(target.y + dY);
		}

		else // Classic shooting spread
		{
			target.x = (short)(target.x + Rng.Generate(0, deviation) - deviation / 2);
			target.y = (short)(target.y + Rng.Generate(0, deviation) - deviation / 2);
		}

		target.z = (short)(target.z + Rng.Generate(0, deviation / 2) / 2 - deviation / 8);

		if (extendLine)
		{
			double rotation, tilt;
			rotation = Math.Atan2((double)(target.y - origin.y), (double)(target.x - origin.x)) * 180 / Math.PI;
			tilt = Math.Atan2((double)(target.z - origin.z),
				Math.Sqrt((double)(target.x - origin.x) * (double)(target.x - origin.x) + (double)(target.y - origin.y) * (double)(target.y - origin.y))) * 180 / Math.PI;
			// calculate new target
			// this new target can be very far out of the map, but we don't care about that right now
			double cos_fi = Math.Cos(Deg2Rad(tilt));
			double sin_fi = Math.Sin(Deg2Rad(tilt));
			double cos_te = Math.Cos(Deg2Rad(rotation));
			double sin_te = Math.Sin(Deg2Rad(rotation));
			target.x = (short)(int)(origin.x + maxRange * cos_te * cos_fi);
			target.y = (short)(int)(origin.y + maxRange * sin_te * cos_fi);
			target.z = (short)(int)(origin.z + maxRange * sin_fi);
		}
	}

	/// <summary>
	/// Moves further in the trajectory.
	/// </summary>
	/// <returns>false if the trajectory is finished - no new position exists in the trajectory.</returns>
	public bool Move()
	{
		if (_position == 0)
		{
			_distanceMax = 0;
			for (int i = 0; i < _trajectory.Count; ++i)
			{
				_distanceMax += TileEngine.TrajectoryStepSize(_trajectory, i);
			}
		}

		for (int i = 0; i < _speed; ++i)
		{
			_position++;
			if (_position == _trajectory.Count)
			{
				_position--;
				return false;
			}

			_distance += TileEngine.TrajectoryStepSize(_trajectory, _position);

			if (_vaporColor != -1 && _ammo != null && _action.type != BattleActionType.BA_THROW)
			{
				AddVaporCloud();
			}
		}
		return true;
	}

	/// <summary>
	/// Get Position at offset from start from trajectory vector.
	/// </summary>
	/// <param name="trajectory">Vector that have trajectory.</param>
	/// <param name="pos">Offset counted from begining of trajectory.</param>
	/// <returns>Position in voxel space.</returns>
	public static Position GetPositionFromStart(List<Position> trajectory, int pos)
	{
		ArgumentNullException.ThrowIfNull(trajectory);
		if (pos >= 0 && pos < trajectory.Count)
			return trajectory[pos];
		else if (pos < 0)
			return trajectory[0];
		else
			return trajectory[trajectory.Count - 1];
	}

	/// <summary>
	/// Get Position at offset from end from trajectory vector.
	/// </summary>
	/// <param name="trajectory">Vector that have trajectory.</param>
	/// <param name="pos">Offset counted from ending of trajectory.</param>
	/// <returns>Position in voxel space.</returns>
	public static Position GetPositionFromEnd(List<Position> trajectory, int pos)
	{
		ArgumentNullException.ThrowIfNull(trajectory);
		return GetPositionFromStart(trajectory, trajectory.Count + pos - 1);
	}

	/// <summary>Gets the current position in voxel space.</summary>
	public Position GetPosition(int offset = 0) => GetPositionFromStart(_trajectory, _position + offset);

	/// <summary>Gets the two last position in voxel space.</summary>
	public LastPositions GetLastPositions(int offset = 0) => new(GetPosition(offset), GetPosition(offset + ItemDropVoxelOffset));

	/// <summary>Gets a particle reference from the projectile surfaces.</summary>
	/// <param name="i">Index.</param>
	/// <returns>Particle id.</returns>
	public int GetParticle(int i)
	{
		if (_bulletSprite != Mod.NO_SURFACE)
			return _bulletSprite + i;
		else
			return Mod.NO_SURFACE;
	}

	/// <summary>Gets the project tile item. Returns null when there is no item thrown.</summary>
	public BattleItem Item
	{
		get
		{
			if (_action.type == BattleActionType.BA_THROW)
				return _action.weapon;
			else
				return null;
		}
	}

	/// <summary>Skips to the end of the trajectory.</summary>
	public void SkipTrajectory()
	{
		while (Move())
		{
		}
	}

	/// <summary>
	/// Gets the Position of origin for the projectile.
	/// Instead of using the actor's position, the voxel origin translated to a tile position is used
	/// (a workaround for large units).
	/// </summary>
	public Position Origin => _trajectory[0].ToTile();

	/// <summary>
	/// Gets the INTENDED target for this projectile.
	/// It is important to note that we do not use the final position of the projectile here, but rather the targetted tile.
	/// </summary>
	public Position Target => _action.target;

	/// <summary>Gets distances that projectile have traveled until now.</summary>
	public float Distance => _distance;

	/// <summary>Is this projectile drawn back to front or front to back?</summary>
	public bool IsReversed => _reversed;

	/// <summary>fmath.h VectCrossProduct for Position (int math, truncated to Sint16).</summary>
	private static Position VectCrossProduct(Position a, Position b, int scale)
	{
		return new Position(
			(a.y * b.z - a.z * b.y) / scale,
			(a.z * b.x - a.x * b.z) / scale,
			(a.x * b.y - a.y * b.x) / scale);
	}

	/// <summary>fmath.h VectNormalize for Position (int math, truncated to Sint16).</summary>
	private static Position VectNormalize(Position a, int scale)
	{
		int dot = (a.x * a.x + a.y * a.y + a.z * a.z) / scale; // VectDotProduct
		if (dot > 0)
		{
			dot = (int)Math.Sqrt(dot * scale);
			return new Position(
				a.x * scale / dot,
				a.y * scale / dot,
				a.z * scale / dot);
		}
		return default;
	}

	/// <summary>
	/// Built-in default of the <c>vaporParticleAmmo</c> script (BattleItem.cpp, VaporParticleAmmoParser), used by every
	/// ammo item without its own script. Mod scripts are not supported yet.
	/// </summary>
	private static void DefaultVaporParticleAmmoScript(Rng.RandomState random, ref Position subvoxelOffset, ref Position subvoxelVelocity, int particleDensity)
	{
		int randMax = Particle.SubVoxelAccuracy; // set randMax subvoxel_scale;
		randMax = (int)((long)randMax * 3 / 2); // muldiv randMax 3 2;

		subvoxelOffset.x = (short)random.Generate(-randMax, randMax); // random.randomRangeSymmetric temp randMax;
		subvoxelOffset.y = (short)random.Generate(-randMax, randMax);
		subvoxelOffset.z = (short)random.Generate(-randMax, randMax);

		subvoxelVelocity.z = (short)(320 - particleDensity);
	}

	/// <summary>
	/// Adds a cloud of vapor at the projectile's current position.
	/// </summary>
	public void AddVaporCloud()
	{
		Rng.RandomState rng = Rng.GlobalRandomState.SubSequence();
		if (rng.Percent(_vaporProbability) == false)
		{
			return;
		}

		Position subvoxelForwardDirection = default;
		Position subvoxelRightDirection = default;
		Position subvoxelUpDirection = default;

		Position voxelPos = GetPosition();
		Position subvoxelPosFrom = GetPosition(-4) * Particle.SubVoxelAccuracy;
		Position subvoxelPosTo = GetPosition(+4) * Particle.SubVoxelAccuracy;
		Position subvoxelVector = subvoxelPosTo - subvoxelPosFrom;

		if (subvoxelVector == default)
		{
			// strange trajectory, use fixed directions
			subvoxelForwardDirection.x = Particle.SubVoxelAccuracy;
			subvoxelRightDirection.y = Particle.SubVoxelAccuracy;
			subvoxelUpDirection.z = Particle.SubVoxelAccuracy;
		}
		else if (Math.Abs(subvoxelVector.x) < 2 && Math.Abs(subvoxelVector.y) < 2)
		{
			// straight up trajectory
			subvoxelForwardDirection.z = Particle.SubVoxelAccuracy;
			subvoxelRightDirection.y = Particle.SubVoxelAccuracy;
			subvoxelUpDirection.x = -Particle.SubVoxelAccuracy;
		}
		else
		{
			// normalize vectors
			subvoxelForwardDirection = VectNormalize(subvoxelVector, Particle.SubVoxelAccuracy);

			subvoxelUpDirection.z = Particle.SubVoxelAccuracy;

			subvoxelRightDirection = VectNormalize(VectCrossProduct(subvoxelUpDirection, subvoxelForwardDirection, Particle.SubVoxelAccuracy), Particle.SubVoxelAccuracy);

			subvoxelUpDirection = VectCrossProduct(subvoxelForwardDirection, subvoxelRightDirection, Particle.SubVoxelAccuracy);
		}
		// The trajectory frame (forward/right/up, distance, distance max) is only input of the (missing) mod scripts.

		Position tilePos = voxelPos.ToTile();
		for (int i = 0; i != _vaporDensity; ++i)
		{
			int varporColor = _vaporColor; // "vapor_color"
			Position subVoxelOffset = default; // "subvoxel_offset"
			Position subVoxelVelocity = default; // "subvoxel_velocity"
			Position subVoxelAcceleration = default; // "subvoxel_acceleration"
			int drift = Particle.SubVoxelAccuracy / 2; // "subvoxel_drift"
			int density = rng.Generate(48, 224); // "particle_density"
			int particleLifetime = rng.Generate(32, 44); // "particle_lifetime"

			// script hooks missing: vaporParticleAmmo (default script below) and vaporParticleWeapon (no default)
			DefaultVaporParticleAmmoScript(rng, ref subVoxelOffset, ref subVoxelVelocity, density);

			byte size = 0;
			//size is initialized at 0
			if (density < 100)
			{
				size = 3;
			}
			else if (density < 125)
			{
				size = 2;
			}
			else if (density < 150)
			{
				size = 1;
			}

			if (varporColor >= 0)
			{
				Particle particle = new(voxelPos, subVoxelOffset, subVoxelVelocity, subVoxelAcceleration, (byte)drift, (byte)varporColor, (byte)particleLifetime, size);
				Position tileOffset = particle.UpdateScreenPosition();
				_save.BattleGame.Map.AddVaporParticle(tilePos + tileOffset, particle);
			}
		}
	}
}
