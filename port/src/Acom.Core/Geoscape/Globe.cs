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
// Ported from: src/Geoscape/Globe.h, src/Geoscape/Globe.cpp
#nullable disable

namespace Acom;

/// <summary>
/// Interactive globe view of the world.
/// Takes a flat world map made out of land polygons with
/// polar coordinates and renders it as a 3D-looking globe
/// with cartesian coordinates that the player can interact with.
/// </summary>
/// <remarks>
/// HD: projection, markers and lines work on logical pixels like the original. The shading pass (C++ ShaderDraw)
/// runs on every buffer pixel but takes the sphere normal and the noise of its logical pixel, so classic surfaces
/// (scale 1) are shaded exactly like the original and HD textures keep their detail.
/// </remarks>
public class Globe : InteractiveSurface
{
	private const int NEAR_RADIUS = 25;
	private const int MAX_DRAW_RADAR_CIRCLE_RADIUS = 10000;
	private const int DOGFIGHT_ZOOM = 3;
	private const int CITY_MARKER = 8;
	private const double ROTATE_LONGITUDE = 0.10;
	private const double ROTATE_LATITUDE = 0.06;

#pragma warning disable CA2211 // public mutable statics like the C++ class; RuleGlobe/Mod set them (also via ref)
	public static byte OCEAN_COLOR;
	public static bool OCEAN_SHADING;
	public static byte COUNTRY_LABEL_COLOR;
	public static byte LINE_COLOR;
	public static byte CITY_LABEL_COLOR;
	public static byte BASE_LABEL_COLOR;
#pragma warning restore CA2211

	/// <summary>Shading tables and noise of the whole program (C++ file-level <c>static_data</c>).</summary>
	private static readonly GlobeStaticData s_staticData = new();

	/// <summary>C++ function-local static of drawDetail().</summary>
	private static bool s_canSwitchDebugType;

	private readonly RuleGlobe _rules;
	private short _cenX, _cenY;
	private double _cenLon, _cenLat, _rotLon, _rotLat, _hoverLon, _hoverLat;
	private double _craftLon, _craftLat, _craftRange;
	private int _zoom, _zoomOld, _zoomTexture;
	private readonly SurfaceSet _texture, _markerSet;
	private readonly Game _game;
	private readonly Surface _markers, _countries, _radars;
	private bool _hover, _craft;
	private int _blink;
	private readonly Timer _blinkTimer, _rotTimer;
	private readonly List<Polygon> _cacheLand = [];
	private readonly FastLineClip _clipper;
	private double _radius, _radiusStep;
	/// <summary>Normal of each pixel in earth globe per zoom level.</summary>
	private readonly List<Cord[]> _earthData = [];
	/// <summary>List of dimension of earth on screen per zoom level.</summary>
	private readonly List<double> _zoomRadius = [];

	private bool _isMouseScrolling, _isMouseScrolled;
	private int _xBeforeMouseScrolling, _yBeforeMouseScrolling;
	private double _lonBeforeMouseScrolling, _latBeforeMouseScrolling;
	private uint _mouseScrollingStartTime;
	private int _totalMouseMoveX, _totalMouseMoveY;
	private bool _mouseMovedOverThreshold;

	/// <summary>Vertex buffers of drawLand() (C++ uses Sint16 x[4], y[4] on the stack).</summary>
	private short[] _polyX = new short[4], _polyY = new short[4];

	/// <summary>Helper class for <see cref="Globe"/> for drawing earth globe with shadows.</summary>
	internal sealed class GlobeStaticData
	{
		public const int random_surf_size = 60;
		public const int random_multiplier_noise_bits = 4;
		public const int random_distance_noise_bits = 3;
		public const int random_value_noise_bits = 5;

		public const int shade_gradient_max = 256;
		public const int shade_step_max = 1 << random_value_noise_bits;
		/// <summary>Array of shading gradient.</summary>
		public readonly short[] shade_gradient = new short[shade_gradient_max];
		public readonly short[] shade_step = new short[shade_gradient_max];
		public readonly short[] shade_seq = new short[shade_gradient_max];
		public readonly short[] shade_diff = new short[shade_gradient_max];
		/// <summary>Size of x &amp; y of noise surface.</summary>
		public readonly short[] random_noise = new short[random_surf_size * random_surf_size];

		/// <summary>Normal vector of the sphere surface.</summary>
		/// <param name="ox">X cord of sphere center.</param>
		/// <param name="oy">Y cord of sphere center.</param>
		/// <param name="r">Radius of sphere.</param>
		/// <param name="x">X cord of point where we getting this vector.</param>
		/// <param name="y">Y cord of point where we getting this vector.</param>
		/// <returns>Normal vector of sphere surface (zero outside the sphere).</returns>
		public static Cord CircleNorm(double ox, double oy, double r, double x, double y)
		{
			double limit = r * r;
			double norm = 1.0 / r;
			Cord ret = new();
			ret.x = (x - ox);
			ret.y = (y - oy);
			double temp = (ret.x) * (ret.x) + (ret.y) * (ret.y);
			if (limit > temp)
			{
				ret.x *= norm;
				ret.y *= norm;
				ret.z = Math.Sqrt(limit - temp) * norm;
				return ret;
			}
			else
			{
				ret.x = 0.0;
				ret.y = 0.0;
				ret.z = 0.0;
				return ret;
			}
		}

		public static short ShadeCurve(int i)
		{
			const int shadeOffset = 15;
			int j = i - shade_gradient_max / 2;

			const int stepSize = 16;
			ReadOnlySpan<int> steps =
			[
				1,
				2,
				2,
				3,
				3,
				4,
				4,
				5,
				5,
				6,
				6,
				9,
				12,
				16,
				20,
				30,
			];

			int adjustemt = (j >= 0 ? 1 : 0);
			int d = (adjustemt != 0 ? 1 : -1);
			int offset = (adjustemt != 0 ? j + adjustemt : -j);
			int shadeFinal = shadeOffset + adjustemt;
			for (int k = 0; k < stepSize; ++k)
			{
				int p = steps[k];
				if (offset < p)
				{
					break;
				}
				shadeFinal += d;
				offset -= p;
			}
			return (short)shadeFinal;
		}

		private static int BitMask(int i)
		{
			return ((1 << i) - 1);
		}

		public static int GetMultiplierNoise(short n)
		{
			return ((n >> (random_value_noise_bits + random_distance_noise_bits)) & BitMask(random_multiplier_noise_bits));
		}

		public static int GetDistanceNoise(short n)
		{
			return ((n >> random_value_noise_bits) & BitMask(random_distance_noise_bits)) - random_distance_noise_bits / 2;
		}

		public static int GetValueNoise(short n)
		{
			return n & BitMask(random_value_noise_bits);
		}

		/// <summary>Initialization.</summary>
		public GlobeStaticData()
		{
			int iLastVal = ShadeCurve(0);
			int iLast = 0;
			//filling terminator gradient LUT
			for (int i = 0; i < shade_gradient_max; ++i)
			{
				int t = ShadeCurve(i);
				if (t != iLastVal)
				{
					for (int p = iLast; p < i; ++p)
					{
						shade_diff[p] = (short)(t - iLastVal);
						shade_step[p] = (short)(shade_step_max / (i - iLast));
						shade_seq[p] = (short)(shade_step_max * (p - iLast) / (i - iLast));
					}
					iLastVal = t;
					iLast = i;
				}
				shade_gradient[i] = (short)t;
			}

			int tLast = ShadeCurve(shade_gradient_max);
			for (int p = iLast; p < shade_gradient_max; ++p)
			{
				shade_diff[p] = (short)(tLast - iLastVal);
				shade_step[p] = (short)(shade_step_max / (shade_gradient_max - iLast));
				shade_seq[p] = (short)(shade_step_max * (p - iLast) / (shade_gradient_max - iLast));
			}

			var randomState = new Rng.RandomState();
			for (int i = 0; i < random_surf_size * random_surf_size; ++i)
				random_noise[i] = (short)randomState.Generate(0, BitMask(random_multiplier_noise_bits + random_distance_noise_bits + random_value_noise_bits));
		}
	}

	/// <summary>Shading tables and noise used by the globe (for tests).</summary>
	internal static GlobeStaticData StaticData => s_staticData;

	// CreateShadow helpers of the C++ file.

	/// <summary>Shadow value 0..31 of a point of the globe with normal <paramref name="earth"/>.</summary>
	internal static byte GetShadowValue(Cord earth, Cord sun, short noise)
	{
		Cord temp = earth;
		//diff
		temp -= sun;
		//norm
		temp.x *= temp.x;
		temp.y *= temp.y;
		temp.z *= temp.z;
		temp.x += temp.z + temp.y;
		//we have norm of distance between 2 vectors, now stored in `x`

		temp.x -= 2;
		temp.x *= 125.0;
		temp.x += GlobeStaticData.shade_gradient_max / 2;
		//random noise that go in any direction
		temp.x -= GlobeStaticData.GetDistanceNoise(noise);
		//random noise than increase with distance from middle of twilight
		temp.x += GlobeStaticData.GetMultiplierNoise(noise) * 4 * (temp.x - GlobeStaticData.shade_gradient_max / 2) / GlobeStaticData.shade_gradient_max;

		// std::modf
		double full = Math.Truncate(temp.x);
		double rem = temp.x - full;
		int offset = Math.Clamp((int)full, 0, GlobeStaticData.shade_gradient_max - 1);
		int i = s_staticData.shade_gradient[offset];

		int middle = (int)((s_staticData.shade_seq[offset] + s_staticData.shade_step[offset] * rem) - GlobeStaticData.shade_step_max / 2);
		i += middle / GlobeStaticData.shade_step_max;
		i += (GlobeStaticData.GetValueNoise(noise) < (middle % GlobeStaticData.shade_step_max)) ? 1 : 0;

		return (byte)Math.Clamp(i, 0, 31);
	}

	private static byte GetOceanShadow(byte shadow)
	{
		return unchecked((byte)(OCEAN_COLOR + shadow));
	}

	private static byte GetLandShadow(byte dest, byte shadow)
	{
		if (shadow == 0) return dest;
		int s = shadow / 3;
		int e = dest + s;
		int d = dest & ShaderDrawHelper.ColorGroup;
		if (e > d + ShaderDrawHelper.ColorShade)
			return (byte)(d + ShaderDrawHelper.ColorShade);
		return (byte)e;
	}

	private static bool IsOcean(byte dest)
	{
		return OCEAN_SHADING && dest >= OCEAN_COLOR && dest < OCEAN_COLOR + 32;
	}

	/// <summary>
	/// CreateShadow::func for the Scale x Scale buffer pixels of logical pixel (x, y); the shadow value
	/// only depends on the normal, the sun and the noise, so it is computed once per logical pixel.
	/// </summary>
	private static void ShadeBlock(Span<byte> pixels, int pitch, int scale, int x, int y, Cord earth, Cord sun, short noise)
	{
		byte shadow = 0;
		bool computed = false;
		for (int py = y * scale; py < (y + 1) * scale; ++py)
		{
			for (int px = x * scale; px < (x + 1) * scale; ++px)
			{
				ref byte dest = ref pixels[py * pitch + px];
				if (dest != 0 && earth.z != 0)
				{
					if (!computed)
					{
						shadow = GetShadowValue(earth, sun, noise);
						computed = true;
					}
					//this pixel is ocean
					if (IsOcean(dest))
					{
						dest = GetOceanShadow(shadow);
					}
					//this pixel is land
					else
					{
						dest = GetLandShadow(dest, shadow);
					}
				}
				else
				{
					dest = 0;
				}
			}
		}
	}

	/// <summary>Draws globe markers: copies non-transparent pixels, optionally with the next colour (blinking).</summary>
	private readonly struct MarkerOp(int add) : IPixelOp
	{
		public void Apply(ref byte dest, byte src)
		{
			if (src != 0)
			{
				dest = unchecked((byte)(src + add));
			}
		}
	}

	private static double Nautical(double x) => x * (1 / 60.0) * (Math.PI / 180.0);

	/// <summary>C++ conversion of a double to Sint16 (truncation; wraps like x86 for large values).</summary>
	private static short ToSint16(double v) => unchecked((short)(int)v);

	/// <summary>Sets up a globe with the specified size and position.</summary>
	/// <param name="game">Pointer to core game.</param>
	/// <param name="cenX">X position of the center of the globe.</param>
	/// <param name="cenY">Y position of the center of the globe.</param>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="x">X position in pixels.</param>
	/// <param name="y">Y position in pixels.</param>
	public Globe(Game game, int cenX, int cenY, int width, int height, int x = 0, int y = 0) : base(width, height, x, y)
	{
		ArgumentNullException.ThrowIfNull(game);
		_cenX = unchecked((short)cenX);
		_cenY = unchecked((short)cenY);
		_game = game;
		_blink = -1;

		_rules = game.Mod.Globe;
		_texture = new SurfaceSet(_game.Mod.GetSurfaceSet("TEXTURE.DAT"));
		_markerSet = _game.Mod.GetSurfaceSet("GlobeMarkers");

		_countries = new Surface(width, height, x, y);
		_markers = new Surface(width, height, x, y);
		_radars = new Surface(width, height, x, y);
		_clipper = new FastLineClip(x, x + width, y, y + height);

		// Animation timers
		_blinkTimer = new Timer(100);
		_blinkTimer.OnTimer((SurfaceHandler)Blink);
		_blinkTimer.Start();
		_rotTimer = new Timer(10);
		_rotTimer.OnTimer((SurfaceHandler)Rotate);

		_cenLon = _game.SavedGame.GlobeLongitude;
		_cenLat = _game.SavedGame.GlobeLatitude;
		_zoom = _game.SavedGame.GlobeZoom;
		_zoomOld = _zoom;

		SetupRadii(width, height);
		SetZoom(_zoom);

		CachePolygons();
	}

	/// <summary>
	/// Converts a polar point into a cartesian point for
	/// mapping a polygon onto the 3D-looking globe.
	/// </summary>
	/// <param name="lon">Longitude of the polar point.</param>
	/// <param name="lat">Latitude of the polar point.</param>
	/// <param name="x">Output X position.</param>
	/// <param name="y">Output Y position.</param>
	public void PolarToCart(double lon, double lat, ref short x, ref short y)
	{
		// Orthographic projection
		x = unchecked((short)(_cenX + ToSint16(Math.Floor(_radius * Math.Cos(lat) * Math.Sin(lon - _cenLon)))));
		y = unchecked((short)(_cenY + ToSint16(Math.Floor(_radius * (Math.Cos(_cenLat) * Math.Sin(lat) - Math.Sin(_cenLat) * Math.Cos(lat) * Math.Cos(lon - _cenLon))))));
	}

	/// <summary>Converts a polar point into a cartesian point (without rounding).</summary>
	public void PolarToCart(double lon, double lat, ref double x, ref double y)
	{
		// Orthographic projection
		x = _cenX + _radius * Math.Cos(lat) * Math.Sin(lon - _cenLon);
		y = _cenY + _radius * (Math.Cos(_cenLat) * Math.Sin(lat) - Math.Sin(_cenLat) * Math.Cos(lat) * Math.Cos(lon - _cenLon));
	}

	/// <summary>
	/// Converts a cartesian point into a polar point for
	/// mapping a globe click onto the flat world map.
	/// </summary>
	/// <param name="x">X position of the cartesian point.</param>
	/// <param name="y">Y position of the cartesian point.</param>
	/// <param name="lon">Output longitude (NaN outside the globe).</param>
	/// <param name="lat">Output latitude (NaN outside the globe).</param>
	public void CartToPolar(short x, short y, ref double lon, ref double lat)
	{
		// Orthographic projection
		x = unchecked((short)(x - _cenX));
		y = unchecked((short)(y - _cenY));

		double rho = Math.Sqrt((double)(x * x + y * y));
		double c = Math.Asin(rho / _radius);
		if (Target.AreSame(rho, 0.0))
		{
			lat = _cenLat;
			lon = _cenLon;
		}
		else
		{
			lat = Math.Asin((y * Math.Sin(c) * Math.Cos(_cenLat)) / rho + Math.Cos(c) * Math.Sin(_cenLat));
			lon = Math.Atan2(x * Math.Sin(c), (rho * Math.Cos(_cenLat) * Math.Cos(c) - y * Math.Sin(_cenLat) * Math.Sin(c))) + _cenLon;
		}

		// Keep between 0 and 2xPI
		while (lon < 0)
			lon += 2 * Math.PI;
		while (lon >= 2 * Math.PI)
			lon -= 2 * Math.PI;
	}

	/// <summary>
	/// Checks if a polar point is on the back-half of the globe,
	/// invisible to the player.
	/// </summary>
	/// <returns>True if it's on the back, False if it's on the front.</returns>
	private bool PointBack(double lon, double lat)
	{
		double c = Math.Cos(_cenLat) * Math.Cos(lat) * Math.Cos(lon - _cenLon) + Math.Sin(_cenLat) * Math.Sin(lat);

		return c < 0.0;
	}

	/// <summary>Gets the polygon containing a polar point, or null.</summary>
	private Polygon GetPolygonFromLonLat(double lon, double lat)
	{
		const double zDiscard = 0.75f;
		double coslat = Math.Cos(lat);
		double sinlat = Math.Sin(lat);

		foreach (var polygon in _rules.Polygons)
		{
			double x, y, z, x2, y2;
			double clat, clon;
			z = 0;
			for (int j = 0; j < polygon.Points; ++j)
			{
				z = coslat * Math.Cos(polygon.GetLatitude(j)) * Math.Cos(polygon.GetLongitude(j) - lon) + sinlat * Math.Sin(polygon.GetLatitude(j));
				if (z < zDiscard) break; //discarded
			}
			if (z < zDiscard) continue; //discarded

			bool odd = false;

			clat = polygon.GetLatitude(0); //initial point
			clon = polygon.GetLongitude(0);
			x = Math.Cos(clat) * Math.Sin(clon - lon);
			y = coslat * Math.Sin(clat) - sinlat * Math.Cos(clat) * Math.Cos(clon - lon);

			for (int j = 0; j < polygon.Points; ++j)
			{
				int k = (j + 1) % polygon.Points; //index of next point in poly
				clat = polygon.GetLatitude(k);
				clon = polygon.GetLongitude(k);

				x2 = Math.Cos(clat) * Math.Sin(clon - lon);
				y2 = coslat * Math.Sin(clat) - sinlat * Math.Cos(clat) * Math.Cos(clon - lon);
				if (((y > 0) != (y2 > 0)) && (0 < (x2 - x) * (0 - y) / (y2 - y) + x))
					odd = !odd;
				x = x2;
				y = y2;
			}
			if (odd) return polygon;
		}
		return null;
	}

	/// <summary>Sets a leftwards rotation speed and starts the timer.</summary>
	public void RotateLeft()
	{
		_rotLon = -ROTATE_LONGITUDE;
		if (!_rotTimer.IsRunning) _rotTimer.Start();
	}

	/// <summary>Sets a rightwards rotation speed and starts the timer.</summary>
	public void RotateRight()
	{
		_rotLon = ROTATE_LONGITUDE;
		if (!_rotTimer.IsRunning) _rotTimer.Start();
	}

	/// <summary>Sets a upwards rotation speed and starts the timer.</summary>
	public void RotateUp()
	{
		_rotLat = -ROTATE_LATITUDE;
		if (!_rotTimer.IsRunning) _rotTimer.Start();
	}

	/// <summary>Sets a downwards rotation speed and starts the timer.</summary>
	public void RotateDown()
	{
		_rotLat = ROTATE_LATITUDE;
		if (!_rotTimer.IsRunning) _rotTimer.Start();
	}

	/// <summary>Resets the rotation speed and timer.</summary>
	public void RotateStop()
	{
		_rotLon = 0.0;
		_rotLat = 0.0;
		_rotTimer.Stop();
	}

	/// <summary>Resets longitude rotation speed and timer.</summary>
	public void RotateStopLon()
	{
		_rotLon = 0.0;
		if (Target.AreSame(_rotLat, 0.0))
		{
			_rotTimer.Stop();
		}
	}

	/// <summary>Resets latitude rotation speed and timer.</summary>
	public void RotateStopLat()
	{
		_rotLat = 0.0;
		if (Target.AreSame(_rotLon, 0.0))
		{
			_rotTimer.Stop();
		}
	}

	/// <summary>Changes the current globe zoom factor.</summary>
	/// <param name="zoom">New zoom.</param>
	private void SetZoom(int zoom)
	{
		_zoom = Math.Clamp(zoom, 0, _zoomRadius.Count - 1);
		_zoomTexture = (2 - (int)Math.Floor(_zoom / 2.0)) * (_texture.TotalFrames / 3);
		_radius = _zoomRadius[_zoom];
		_game.SavedGame.GlobeZoom = _zoom;
		if (_isMouseScrolling)
		{
			_lonBeforeMouseScrolling = _cenLon;
			_latBeforeMouseScrolling = _cenLat;
			_totalMouseMoveX = 0; _totalMouseMoveY = 0;
		}
		Invalidate();
	}

	/// <summary>Increases the zoom level on the globe.</summary>
	public void ZoomIn()
	{
		if (_zoom < _zoomRadius.Count - 1)
		{
			SetZoom(_zoom + 1);
		}
	}

	/// <summary>Decreases the zoom level on the globe.</summary>
	public void ZoomOut()
	{
		if (_zoom > 0)
		{
			SetZoom(_zoom - 1);
		}
	}

	/// <summary>Zooms the globe out as far as possible.</summary>
	public void ZoomMin()
	{
		if (_zoom > 0)
		{
			SetZoom(0);
		}
	}

	/// <summary>Zooms the globe in as close as possible.</summary>
	public void ZoomMax()
	{
		if (_zoom < _zoomRadius.Count - 1)
		{
			SetZoom(_zoomRadius.Count - 1);
		}
	}

	/// <summary>Stores the zoom used before a dogfight.</summary>
	public void SaveZoomDogfight()
	{
		_zoomOld = _zoom;
	}

	/// <summary>Zooms the globe smoothly into dogfight level.</summary>
	/// <returns>Is the globe already zoomed in?</returns>
	public bool ZoomDogfightIn()
	{
		if (_zoom < DOGFIGHT_ZOOM)
		{
			double radiusNow = _radius;
			if (radiusNow + _radiusStep >= _zoomRadius[DOGFIGHT_ZOOM])
			{
				SetZoom(DOGFIGHT_ZOOM);
			}
			else
			{
				if (radiusNow + _radiusStep >= _zoomRadius[_zoom + 1])
					_zoom++;
				SetZoom(_zoom);
				_radius = radiusNow + _radiusStep;
			}
			return false;
		}
		return true;
	}

	/// <summary>Zooms the globe smoothly out of dogfight level.</summary>
	/// <returns>Is the globe already zoomed out?</returns>
	public bool ZoomDogfightOut()
	{
		if (_zoom > _zoomOld)
		{
			double radiusNow = _radius;
			if (radiusNow - _radiusStep <= _zoomRadius[_zoomOld])
			{
				SetZoom(_zoomOld);
			}
			else
			{
				if (radiusNow - _radiusStep <= _zoomRadius[_zoom - 1])
					_zoom--;
				SetZoom(_zoom);
				_radius = radiusNow - _radiusStep;
			}
			return false;
		}
		return true;
	}

	/// <summary>The current zoom (0-5).</summary>
	public int Zoom { get => _zoom; }

	/// <summary>
	/// Rotates the globe to center on a certain
	/// polar point on the world map.
	/// </summary>
	/// <param name="lon">Longitude of the point.</param>
	/// <param name="lat">Latitude of the point.</param>
	public void Center(double lon, double lat)
	{
		_cenLon = lon;
		_cenLat = lat;
		_game.SavedGame.GlobeLongitude = _cenLon;
		_game.SavedGame.GlobeLatitude = _cenLat;
		Invalidate();
	}

	/// <summary>Checks if a polar point is inside the globe's landmass.</summary>
	/// <param name="lon">Longitude of the point.</param>
	/// <param name="lat">Latitude of the point.</param>
	/// <returns>True if it's inside, False if it's outside.</returns>
	public bool InsideLand(double lon, double lat)
	{
		var polygon = GetPolygonFromLonLat(lon, lat);
		if (polygon == null)
		{
			return false;
		}
		var textureRule = _rules.GetTexture(polygon.Texture);
		if (textureRule != null && textureRule.IsCosmeticOcean)
		{
			return false;
		}
		return true;
	}

	/// <summary>Checks if a polar point is inside the fakeUnderwater texture.</summary>
	/// <param name="lon">Longitude of the point.</param>
	/// <param name="lat">Latitude of the point.</param>
	/// <returns>True if it's inside, False if it's outside.</returns>
	public bool InsideFakeUnderwaterTexture(double lon, double lat)
	{
		var polygon = GetPolygonFromLonLat(lon, lat);
		if (polygon == null)
		{
			return false;
		}
		var textureRule = _rules.GetTexture(polygon.Texture);
		if (textureRule != null && textureRule.IsFakeUnderwater)
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// Switches the amount of detail shown on the globe.
	/// With detail on, country and city details are shown when zoomed in.
	/// </summary>
	public void ToggleDetail()
	{
		Options.globeDetail = !Options.globeDetail;
		DrawDetail();
	}

	/// <summary>
	/// Checks if a certain target is near a certain cartesian point
	/// (within a circled area around it) over the globe.
	/// </summary>
	/// <param name="target">Pointer to target.</param>
	/// <param name="x">X coordinate of point.</param>
	/// <param name="y">Y coordinate of point.</param>
	/// <returns>True if it's near, false otherwise.</returns>
	private bool TargetNear(Target target, int x, int y)
	{
		short tx = 0, ty = 0;
		if (PointBack(target.Longitude, target.Latitude))
			return false;
		PolarToCart(target.Longitude, target.Latitude, ref tx, ref ty);

		int dx = x - tx;
		int dy = y - ty;
		return (dx * dx + dy * dy <= NEAR_RADIUS);
	}

	/// <summary>
	/// Returns a list of all the targets currently near a certain
	/// cartesian point over the globe.
	/// </summary>
	/// <param name="x">X coordinate of point.</param>
	/// <param name="y">Y coordinate of point.</param>
	/// <param name="craft">Only get craft targets (unused, like in C++).</param>
	/// <param name="currentCraft">Craft to leave out.</param>
	/// <returns>List of pointers to targets.</returns>
	public List<Target> GetTargets(int x, int y, bool craft, Craft currentCraft)
	{
		var v = new List<Target>();
		{
			foreach (var xbase in _game.SavedGame.Bases)
			{
				if (xbase.Longitude == 0.0 && xbase.Latitude == 0.0)
					continue;

				if (TargetNear(xbase, x, y))
				{
					v.Add(xbase);
				}

				foreach (var xcraft in xbase.Crafts)
				{
					if (xcraft == currentCraft)
						continue;
					if (xcraft.Longitude == xbase.Longitude && xcraft.Latitude == xbase.Latitude && xcraft.Destination == null)
						continue;

					if (TargetNear(xcraft, x, y))
					{
						v.Add(xcraft);
					}
				}
			}
		}
		foreach (var ufo in _game.SavedGame.Ufos)
		{
			if (!ufo.Detected || ufo.Status == Ufo.UfoStatus.IGNORE_ME)
				continue;

			if (TargetNear(ufo, x, y))
			{
				v.Add(ufo);
			}
		}
		foreach (var wp in _game.SavedGame.Waypoints)
		{
			if (TargetNear(wp, x, y))
			{
				v.Add(wp);
			}
		}
		foreach (var site in _game.SavedGame.MissionSites)
		{
			if (TargetNear(site, x, y))
			{
				v.Add(site);
			}
		}
		foreach (var ab in _game.SavedGame.AlienBases)
		{
			if (!ab.IsDiscovered)
			{
				continue;
			}
			if (TargetNear(ab, x, y))
			{
				v.Add(ab);
			}
		}
		return v;
	}

	/// <summary>
	/// Takes care of pre-calculating all the polygons currently visible
	/// on the globe and caching them so they only need to be recalculated
	/// when the globe is actually moved.
	/// </summary>
	public void CachePolygons()
	{
		Cache(_rules.Polygons, _cacheLand);
	}

	/// <summary>Caches a set of polygons.</summary>
	/// <param name="polygons">List of polygons.</param>
	/// <param name="cache">Cache.</param>
	private void Cache(List<Polygon> polygons, List<Polygon> cache)
	{
		// Clear existing cache
		cache.Clear();

		// Pre-calculate values to cache
		foreach (var polygon in polygons)
		{
			// Is quad on the back face?
			double closest = 0.0;
			double z;
			double furthest = 0.0;
			for (int j = 0; j < polygon.Points; ++j)
			{
				z = Math.Cos(_cenLat) * Math.Cos(polygon.GetLatitude(j)) * Math.Cos(polygon.GetLongitude(j) - _cenLon) + Math.Sin(_cenLat) * Math.Sin(polygon.GetLatitude(j));
				if (z > closest)
					closest = z;
				else if (z < furthest)
					furthest = z;
			}
			if (-furthest > closest)
				continue;

			var p = new Polygon(polygon);

			// Convert coordinates
			for (int j = 0; j < p.Points; ++j)
			{
				short x = 0, y = 0;
				PolarToCart(p.GetLongitude(j), p.GetLatitude(j), ref x, ref y);
				p.SetX(j, x);
				p.SetY(j, y);
			}

			cache.Add(p);
		}
	}

	/// <summary>Replaces a certain amount of colors in the palette of the globe.</summary>
	/// <param name="colors">The set of colors.</param>
	/// <param name="firstcolor">Offset of the first color to replace.</param>
	/// <param name="ncolors">Amount of colors to replace.</param>
	public override void SetPalette(ReadOnlySpan<Rgba> colors, int firstcolor = 0, int ncolors = 256)
	{
		base.SetPalette(colors, firstcolor, ncolors);

		_texture.SetPalette(colors, firstcolor, ncolors);

		_countries.SetPalette(colors, firstcolor, ncolors);
		_markers.SetPalette(colors, firstcolor, ncolors);
		_radars.SetPalette(colors, firstcolor, ncolors);
	}

	/// <summary>Keeps the animation timers running.</summary>
	public override void Think()
	{
		_blinkTimer.Think(null, this);
		_rotTimer.Think(null, this);
	}

	/// <summary>Makes the globe markers blink.</summary>
	public void Blink()
	{
		_blink = -_blink;

		DrawMarkers();
	}

	/// <summary>
	/// Rotates the globe by a set amount. Necessary
	/// since the globe keeps rotating while a button
	/// is pressed down.
	/// </summary>
	public void Rotate()
	{
		_cenLon += _rotLon * ((110 - Options.geoScrollSpeed) / 100.0) / (_zoom + 1);
		_cenLat += _rotLat * ((110 - Options.geoScrollSpeed) / 100.0) / (_zoom + 1);
		_game.SavedGame.GlobeLongitude = _cenLon;
		_game.SavedGame.GlobeLatitude = _cenLat;
		Invalidate();
	}

	/// <summary>Draws the whole globe, part by part.</summary>
	public override void Draw()
	{
		if (_redraw)
		{
			CachePolygons();
		}
		base.Draw();
		DrawOcean();
		DrawLand();
		DrawRadars();
		DrawFlights();
		DrawShadow();
		DrawMarkers();
		DrawDetail();
	}

	/// <summary>Renders the ocean, shading it according to the time of day.</summary>
	public void DrawOcean()
	{
		Lock();
		DrawCircle(_cenX + 1, _cenY, (int)(_radius + 20), OCEAN_COLOR);
		Unlock();
	}

	/// <summary>
	/// Renders the land, taking all the visible world polygons
	/// and texturing and shading them accordingly.
	/// </summary>
	public void DrawLand()
	{
		foreach (var polygon in _cacheLand)
		{
			int n = polygon.Points;
			if (_polyX.Length < n)
			{
				_polyX = new short[n];
				_polyY = new short[n];
			}
			// Convert coordinates
			for (int j = 0; j < n; ++j)
			{
				_polyX[j] = polygon.GetX(j);
				_polyY[j] = polygon.GetY(j);
			}

			// Apply textures according to zoom and shade
			DrawTexturedPolygon(_polyX, _polyY, n, _texture.GetFrame(polygon.Texture + _zoomTexture), 0, 0);
		}
	}

	/// <summary>Gets the position of the sun from a point on the globe.</summary>
	/// <param name="lon">Longitude of position.</param>
	/// <param name="lat">Latitude of position.</param>
	/// <returns>Position of sun.</returns>
	private Cord GetSunDirection(double lon, double lat)
	{
		var time = _game.SavedGame.Time;
		double curTime = time.Daylight;
		double rot = curTime * 2 * Math.PI;
		double sun;

		if (Options.globeSeasons)
		{
			ReadOnlySpan<int> MonthDays1 = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365];
			ReadOnlySpan<int> MonthDays2 = [0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366];

			int year = time.Year;
			int month = time.Month - 1;
			int day = time.Day - 1;

			double tm = (double)((time.Hour * 60
				+ time.Minute) * 60
				+ time.Second) / 86400; //day fraction is also taken into account

			double CurDay;
			if (year % 4 == 0 && !(year % 100 == 0 && year % 400 != 0))
				CurDay = (MonthDays2[month] + day + tm) / 366 - 0.219; //spring equinox (start of astronomic year)
			else
				CurDay = (MonthDays1[month] + day + tm) / 365 - 0.219;
			if (CurDay < 0) CurDay += 1.0;

			sun = -0.261 * Math.Sin(CurDay * 2 * Math.PI);
		}
		else
			sun = 0;

		var sun_direction = new Cord(Math.Cos(rot + lon), Math.Sin(rot + lon) * -Math.Sin(lat), Math.Sin(rot + lon) * Math.Cos(lat));

		var pole = new Cord(0, Math.Cos(lat), Math.Sin(lat));

		if (sun > 0)
			sun_direction *= 1.0 - sun;
		else
			sun_direction *= 1.0 + sun;

		pole *= sun;
		sun_direction += pole;
		double norm = sun_direction.Norm();
		//norm should be always greater than 0
		norm = 1.0 / norm;
		sun_direction *= norm;
		return sun_direction;
	}

	/// <summary>Draws the shadow (day/night terminator) over the globe.</summary>
	public void DrawShadow()
	{
		Cord sun = GetSunDirection(_cenLon, _cenLat);
		short[] noise = s_staticData.random_noise;
		const int ns = GlobeStaticData.random_surf_size;
		int w = Width, h = Height, scale = Scale, pitch = PhysicalWidth;
		Span<byte> pixels = Pixels;

		if (Options.globeSurfaceCache)
		{
			// ShaderMove of the normals by (_cenX - w/2, _cenY - h/2), clipped to this surface; noise repeats from (0, 0)
			Cord[] earth = _earthData[_zoom];
			int moveX = _cenX - w / 2, moveY = _cenY - h / 2;
			int x0 = Math.Max(0, moveX), x1 = Math.Min(w, moveX + w);
			int y0 = Math.Max(0, moveY), y1 = Math.Min(h, moveY + h);

			Lock();
			for (int y = y0; y < y1; ++y)
			{
				for (int x = x0; x < x1; ++x)
				{
					ShadeBlock(pixels, pitch, scale, x, y, earth[(y - moveY) * w + (x - moveX)], sun, noise[(y % ns) * ns + x % ns]);
				}
			}
			Unlock();
		}
		else
		{
			// the radius is passed as `const int&` to CreateShadowWithoutCache::func in C++, so it is truncated
			int radius = (int)_zoomRadius[_zoom];

			Lock();
			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					Cord earth = GlobeStaticData.CircleNorm(0.0, 0.0, radius, x - _cenX, y - _cenY);
					ShadeBlock(pixels, pitch, scale, x, y, earth, sun, noise[(y % ns) * ns + x % ns]);
				}
			}
			Unlock();
		}
	}

	/// <summary>Special "transparent" line: shades the pixels of <paramref name="src"/> along it onto <paramref name="surface"/>.</summary>
	private void XuLine(Surface surface, Surface src, double x1, double y1, double x2, double y2, int shade)
	{
		if (_clipper.LineClip(ref x1, ref y1, ref x2, ref y2) != 1) return; //empty line

		double deltax = x2 - x1, deltay = y2 - y1;
		bool inv;
		short tcol;
		double len, x0, y0, SX, SY;
		if (Math.Abs((int)y2 - (int)y1) > Math.Abs((int)x2 - (int)x1))
		{
			len = Math.Abs((int)y2 - (int)y1);
			inv = false;
		}
		else
		{
			len = Math.Abs((int)x2 - (int)x1);
			inv = true;
		}

		if (y2 < y1)
		{
			SY = -1;
		}
		else if (Target.AreSame(deltay, 0.0))
		{
			SY = 0;
		}
		else
		{
			SY = 1;
		}

		if (x2 < x1)
		{
			SX = -1;
		}
		else if (Target.AreSame(deltax, 0.0))
		{
			SX = 0;
		}
		else
		{
			SX = 1;
		}

		x0 = x1; y0 = y1;
		if (inv)
			SY = (deltay / len);
		else
			SX = (deltax / len);

		while (len > 0)
		{
			tcol = src.GetPixel((int)x0, (int)y0);
			if (tcol != 0)
			{
				if (IsOcean((byte)tcol))
				{
					tcol = GetOceanShadow(unchecked((byte)(shade + 8)));
				}
				else
				{
					tcol = GetLandShadow((byte)tcol, unchecked((byte)(shade * 3)));
				}
				surface.SetPixel((int)x0, (int)y0, (byte)tcol);
			}
			x0 += SX;
			y0 += SY;
			len -= 1.0;
		}
	}

	/// <summary>Draws the radar ranges of player bases, player craft, alien bases and UFO hunter-killers on the globe.</summary>
	public void DrawRadars()
	{
		_radars.Clear();

		if (!Options.globeRadarLines)
			return;

		double tr, range;
		double lat, lon;
		var ranges = new List<double>();

		_radars.Lock();

		// Draw craft range
		if (_craft)
		{
			if (_craftRange < Math.PI)
			{
				DrawGlobeCircle(_craftLat, _craftLon, _craftRange, 64);
				DrawGlobeCircle(_craftLat, _craftLon, _craftRange - 0.025, 64, 2);
			}
		}

		if (_hover)
		{
			foreach (var facType in _game.Mod.BaseFacilitiesList)
			{
				range = Nautical(_game.Mod.GetBaseFacility(facType).RadarRange);
				DrawGlobeCircle(_hoverLat, _hoverLon, range, 48);
				if (Options.globeAllRadarsOnBaseBuild) ranges.Add(range);
			}
		}

		// Draw radars around bases
		foreach (var xbase in _game.SavedGame.Bases)
		{
			lat = xbase.Latitude;
			lon = xbase.Longitude;
			// Cheap hack to hide bases when they haven't been placed yet
			if (!(Target.AreSame(lon, 0.0) && Target.AreSame(lat, 0.0)) /* &&
				!pointBack(xbase->getLongitude(), xbase->getLatitude())*/)
			{
				if (_hover && Options.globeAllRadarsOnBaseBuild)
				{
					for (int j = 0; j < ranges.Count; j++) DrawGlobeCircle(lat, lon, ranges[j], 48);
				}
				else
				{
					range = 0;
					foreach (var fac in xbase.Facilities)
					{
						if (fac.BuildTime == 0)
						{
							tr = fac.Rules.RadarRange;
							if (tr < MAX_DRAW_RADAR_CIRCLE_RADIUS && tr > range) range = tr;
						}
					}
					range = Nautical(range);

					if (range > 0) DrawGlobeCircle(lat, lon, range, 48);
				}
			}

			// Draw radars around player craft
			foreach (var xcraft in xbase.Crafts)
			{
				if (xcraft.Status != "STR_OUT")
					continue;
				lat = xcraft.Latitude;
				lon = xcraft.Longitude;
				range = Nautical(xcraft.CraftStats.radarRange);

				if (range > 0) DrawGlobeCircle(lat, lon, range, 24);
			}
		}

		if (_game.Mod.DrawEnemyRadarCircles > 0)
		{
			// Draw radars around UFO hunter-killers
			foreach (var ufo in _game.SavedGame.Ufos)
			{
				if (ufo.IsHunterKiller && ufo.Detected && ufo.Status != Ufo.UfoStatus.IGNORE_ME)
				{
					if (_game.Mod.DrawEnemyRadarCircles == 1 && !ufo.HyperDetected)
					{
						continue;
					}
					lat = ufo.Latitude;
					lon = ufo.Longitude;
					range = Nautical(ufo.CraftStats.radarRange);

					if (range > 0) DrawGlobeCircle(lat, lon, range, 24);
				}
			}

			// Draw radars around alien bases
			foreach (var ab in _game.SavedGame.AlienBases)
			{
				if (ab.Deployment.BaseDetectionRange > 0 && ab.IsDiscovered)
				{
					lat = ab.Latitude;
					lon = ab.Longitude;
					range = Nautical(ab.Deployment.BaseDetectionRange);

					if (range > 0) DrawGlobeCircle(lat, lon, range, 24);
				}
			}
		}

		_radars.Unlock();
	}

	/// <summary>Draws a globe range circle.</summary>
	private void DrawGlobeCircle(double lat, double lon, double radius, int segments, int frac = 1)
	{
		double x = 0, y = 0, x2 = 0, y2 = 0;
		double lat1, lon1;
		double seg = Math.PI / ((double)segments / 2);
		int i = 0;
		for (double az = 0; az <= Math.PI * 2 + 0.01; az += seg) //48 circle segments
		{
			//calculating sphere-projected circle
			lat1 = Math.Asin(Math.Sin(lat) * Math.Cos(radius) + Math.Cos(lat) * Math.Sin(radius) * Math.Cos(az));
			lon1 = lon + Math.Atan2(Math.Sin(az) * Math.Sin(radius) * Math.Cos(lat), Math.Cos(radius) - Math.Sin(lat) * Math.Sin(lat1));
			PolarToCart(lon1, lat1, ref x, ref y);
			if (Target.AreSame(az, 0.0)) //first vertex is for initialization only
			{
				x2 = x;
				y2 = y;
				continue;
			}
			if (!PointBack(lon1, lat1) && i % frac == 0)
				XuLine(_radars, this, x, y, x2, y2, 6);
			x2 = x; y2 = y;
			i++;
		}
	}

	/// <summary>Turns on new base hover mode.</summary>
	public void SetNewBaseHover(bool hover)
	{
		_hover = hover;
	}

	/// <summary>Sets hover base position.</summary>
	public void SetNewBaseHoverPos(double lon, double lat)
	{
		_hoverLon = lon;
		_hoverLat = lat;
	}

	/// <summary>Draws a line along a meridian or a parallel on the globe surface.</summary>
	private void DrawVHLine(Surface surface, double lon1, double lat1, double lon2, double lat2, byte color)
	{
		double sx = lon2 - lon1;
		double sy = lat2 - lat1;
		double ln1, lt1, ln2, lt2;
		int seg;
		short x1 = 0, y1 = 0, x2 = 0, y2 = 0;

		if (sx < 0) sx += 2 * Math.PI;

		if (Math.Abs(sx) < 0.01)
		{
			seg = (int)Math.Abs(sy / (2 * Math.PI) * 48);
			if (seg == 0) ++seg;
		}
		else
		{
			seg = (int)Math.Abs(sx / (2 * Math.PI) * 96);
			if (seg == 0) ++seg;
		}

		sx /= seg;
		sy /= seg;

		for (int i = 0; i < seg; ++i)
		{
			ln1 = lon1 + sx * i;
			lt1 = lat1 + sy * i;
			ln2 = lon1 + sx * (i + 1);
			lt2 = lat1 + sy * (i + 1);

			if (!PointBack(ln2, lt2) && !PointBack(ln1, lt1))
			{
				PolarToCart(ln1, lt1, ref x1, ref y1);
				PolarToCart(ln2, lt2, ref x2, ref y2);
				surface.DrawLine(x1, y1, x2, y2, color);
			}
		}
	}

	/// <summary>Draws the four sides of a lon/lat rectangle (debug mode).</summary>
	private void DrawDebugArea(double lon1, double lat1, double lon2, double lat2, int color)
	{
		byte c = unchecked((byte)color);
		DrawVHLine(_countries, lon1, lat1, lon2, lat1, c);
		DrawVHLine(_countries, lon1, lat2, lon2, lat2, c);
		DrawVHLine(_countries, lon1, lat1, lon1, lat2, c);
		DrawVHLine(_countries, lon2, lat1, lon2, lat2, c);
	}

	/// <summary>
	/// Draws the details of the countries on the globe,
	/// based on the current zoom level.
	/// </summary>
	public void DrawDetail()
	{
		_countries.Clear();

		if (!Options.globeDetail)
			return;

		// Draw the country borders
		if (_zoom >= 1)
		{
			// Lock the surface
			_countries.Lock();

			foreach (var polyline in _rules.Polylines)
			{
				short x0 = 0, y0 = 0, x1 = 0, y1 = 0;
				for (int j = 0; j < polyline.Points - 1; ++j)
				{
					// Don't draw if polyline is facing back
					if (PointBack(polyline.GetLongitude(j), polyline.GetLatitude(j)) || PointBack(polyline.GetLongitude(j + 1), polyline.GetLatitude(j + 1)))
						continue;

					// Convert coordinates
					PolarToCart(polyline.GetLongitude(j), polyline.GetLatitude(j), ref x0, ref y0);
					PolarToCart(polyline.GetLongitude(j + 1), polyline.GetLatitude(j + 1), ref x1, ref y1);

					_countries.DrawLine(x0, y0, x1, y1, LINE_COLOR);
				}
			}

			// Unlock the surface
			_countries.Unlock();
		}

		// Draw the country names
		if (_zoom >= 2)
		{
			var label = new Text(150, 9, 0, 0);
			label.SetPalette(Palette);
			label.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
			label.Align = TextHAlign.ALIGN_CENTER;

			short x = 0, y = 0;
			foreach (var country in _game.SavedGame.Countries)
			{
				// Don't draw if label is facing back
				if (PointBack(country.Rules.LabelLongitude, country.Rules.LabelLatitude))
					continue;

				// Convert coordinates
				PolarToCart(country.Rules.LabelLongitude, country.Rules.LabelLatitude, ref x, ref y);

				label.X = x - 75;
				label.Y = y;
				label.SetText(_game.Language.GetString(country.Rules.Type));
				label.SetColor(COUNTRY_LABEL_COLOR);
				if (country.Rules.LabelColor > 0)
				{
					label.SetColor(unchecked((byte)country.Rules.LabelColor));
				}
				label.Blit(_countries);
			}
		}

		// Draw extra globe labels
		{
			var label = new Text(120, 18, 0, 0);
			label.SetPalette(Palette);
			label.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
			label.Align = TextHAlign.ALIGN_CENTER;

			short x = 0, y = 0;
			foreach (var extraLabelType in _game.Mod.ExtraGlobeLabelsList)
			{
				RuleCountry rule = _game.Mod.GetExtraGlobeLabel(extraLabelType, true);
				if (_zoom >= rule.ZoomLevel)
				{
					// Don't draw if label is facing back
					if (PointBack(rule.LabelLongitude, rule.LabelLatitude))
						continue;

					// Convert coordinates
					PolarToCart(rule.LabelLongitude, rule.LabelLatitude, ref x, ref y);

					label.X = x - 60;
					label.Y = y;
					label.SetText(_game.Language.GetString(rule.Type));
					label.SetColor(COUNTRY_LABEL_COLOR);
					if (rule.LabelColor > 0)
					{
						label.SetColor(unchecked((byte)rule.LabelColor));
					}
					label.Blit(_countries);
				}
			}
		}

		// Draw the city and base markers
		if (_zoom >= 3)
		{
			var label = new Text(100, 9, 0, 0);
			label.SetPalette(Palette);
			label.InitText(_game.Mod.GetFont("FONT_BIG"), _game.Mod.GetFont("FONT_SMALL"), _game.Language);
			label.Align = TextHAlign.ALIGN_CENTER;
			label.SetColor(CITY_LABEL_COLOR);

			short x = 0, y = 0;
			foreach (var region in _game.SavedGame.Regions)
			{
				foreach (var city in region.Rules.Cities)
				{
					DrawTarget(city, _countries);

					// Don't draw if city is facing back
					if (PointBack(city.Longitude, city.Latitude))
						continue;

					// Convert coordinates
					PolarToCart(city.Longitude, city.Latitude, ref x, ref y);

					label.X = x - 50;
					label.Y = y + 2;
					label.SetText(city.GetName(_game.Language));
					label.Blit(_countries);
				}
			}
			// Draw bases names
			foreach (var xbase in _game.SavedGame.Bases)
			{
				if (xbase.Marker == -1 || PointBack(xbase.Longitude, xbase.Latitude))
					continue;
				PolarToCart(xbase.Longitude, xbase.Latitude, ref x, ref y);
				label.X = x - 50;
				label.Y = y + 2;
				label.SetColor(BASE_LABEL_COLOR);
				label.SetText(xbase.GetName(_game.Language));
				label.Blit(_countries);
			}
		}

		var save = _game.SavedGame;
		if (save.DebugMode)
		{
			int color;
			s_canSwitchDebugType = true;
			if (save.debugType == 0)
			{
				color = 0;
				foreach (var country in save.Countries)
				{
					if (save.debugCountry != null && save.debugCountry != country)
						continue;

					color += 10;
					for (int k = 0; k != country.Rules.LatMax.Count; ++k)
					{
						double lon2 = country.Rules.LonMax[k];
						double lon1 = country.Rules.LonMin[k];
						double lat2 = country.Rules.LatMax[k];
						double lat1 = country.Rules.LatMin[k];

						DrawDebugArea(lon1, lat1, lon2, lat2, color);
					}
				}
			}
			else if (save.debugType == 1)
			{
				color = 0;
				foreach (var region in save.Regions)
				{
					if (save.debugRegion != null && save.debugRegion != region)
						continue;

					color += 10;
					for (int k = 0; k != region.Rules.LatMax.Count; ++k)
					{
						double lon2 = region.Rules.LonMax[k];
						double lon1 = region.Rules.LonMin[k];
						double lat2 = region.Rules.LatMax[k];
						double lat1 = region.Rules.LatMin[k];

						DrawDebugArea(lon1, lat1, lon2, lat2, color);
					}
				}
			}
			else if (save.debugType == 2)
			{
				foreach (var region in save.Regions)
				{
					if (save.debugRegion != null && save.debugRegion != region)
						continue;

					color = -1;
					int zoneNumber = 0;
					foreach (var missionZone in region.Rules.MissionZones)
					{
						++zoneNumber;
						if (save.debugZone > 0 && save.debugZone != zoneNumber)
							continue;

						color += 2;
						int areaNumber = 0;
						foreach (var missionArea in missionZone.areas)
						{
							++areaNumber;
							if (save.debugArea > 0 && save.debugArea != areaNumber)
								continue;

							double lon2 = missionArea.lonMax;
							double lon1 = missionArea.lonMin;
							double lat2 = missionArea.latMax;
							double lat1 = missionArea.latMin;

							DrawDebugArea(lon1, lat1, lon2, lat2, color);
						}
					}
				}
			}
		}
		else
		{
			if (s_canSwitchDebugType)
			{
				++save.debugType;
				if (save.debugType > 2) save.debugType = 0;
				s_canSwitchDebugType = false;
			}
		}
	}

	/// <summary>Draws a flight path (great circle) as a shaded line.</summary>
	private void DrawPath(Surface surface, double lon1, double lat1, double lon2, double lat2)
	{
		double length;
		short count;
		double x1 = 0, y1 = 0, x2 = 0, y2 = 0;
		CordPolar p1, p2;
		var a = new Cord(new CordPolar(lon1, lat1));
		var b = new Cord(new CordPolar(lon2, lat2));

		if ((-b).AreSame(a))
			return;

		b -= a;

		//longer path have more parts
		length = b.Norm();
		length *= length * 15;
		count = ToSint16(length + 1);
		b /= count;
		p1 = new CordPolar(a);
		PolarToCart(p1.lon, p1.lat, ref x1, ref y1);
		for (int i = 0; i < count; ++i)
		{
			a += b;
			p2 = new CordPolar(a);
			PolarToCart(p2.lon, p2.lat, ref x2, ref y2);

			if (!PointBack(p1.lon, p1.lat) && !PointBack(p2.lon, p2.lat))
			{
				XuLine(surface, this, x1, y1, x2, y2, 8);
			}

			p1 = p2;
			x1 = x2;
			y1 = y2;
		}
	}

	/// <summary>Draws the flight paths of player craft (and hunting UFOs) flying on the globe.</summary>
	public void DrawFlights()
	{
		//_radars->clear();

		if (!Options.globeFlightPaths)
			return;

		// Lock the surface
		_radars.Lock();

		// Draw the craft flight paths
		foreach (var xbase in _game.SavedGame.Bases)
		{
			foreach (var xcraft in xbase.Crafts)
			{
				// Hide crafts docked at base
				if (xcraft.Status != "STR_OUT" || xcraft.Destination == null /*|| pointBack(xcraft->getLongitude(), xcraft->getLatitude())*/)
					continue;

				double lon1 = xcraft.Longitude;
				double lat1 = xcraft.Latitude;
				double lon2 = xcraft.Destination.Longitude;
				double lat2 = xcraft.Destination.Latitude;

				if (xcraft.IsMeetCalculated)
				{
					lon2 = xcraft.MeetLongitude;
					lat2 = xcraft.MeetLatitude;
				}
				DrawPath(_radars, lon1, lat1, lon2, lat2);

				if (xcraft.IsMeetCalculated)
				{
					lon1 = xcraft.Destination.Longitude;
					lat1 = xcraft.Destination.Latitude;

					DrawPath(_radars, lon1, lat1, lon2, lat2);
				}
			}
		}

		// Draw the hunting UFO flight paths
		foreach (var ufo in _game.SavedGame.Ufos)
		{
			if (ufo.Destination != null && (ufo.IsHunting || _game.SavedGame.DebugMode) && ufo.Detected && ufo.Status != Ufo.UfoStatus.IGNORE_ME)
			{
				double lon1 = ufo.Longitude;
				double lon2 = ufo.Destination.Longitude;
				double lat1 = ufo.Latitude;
				double lat2 = ufo.Destination.Latitude;

				DrawPath(_radars, lon1, lat1, lon2, lat2);
			}
		}

		// Unlock the surface
		_radars.Unlock();
	}

	/// <summary>Draws the marker for a specified target on the globe.</summary>
	/// <param name="target">Pointer to globe target.</param>
	/// <param name="surface">Surface to draw on.</param>
	private void DrawTarget(Target target, Surface surface)
	{
		if (target.Marker != -1 && !PointBack(target.Longitude, target.Latitude))
		{
			short x = 0, y = 0;
			PolarToCart(target.Longitude, target.Latitude, ref x, ref y);
			int i = target.Marker;
			Surface marker = _markerSet.GetFrame(i);
			int mx = x - marker.Width / 2, my = y - marker.Height / 2;

			// ShaderDrawFunc over the marker placed at (mx, my): blinking markers use the next colour
			var op = new MarkerOp(i == CITY_MARKER || _blink > 0 ? 0 : 1);
			marker.BlitRect(surface, mx, my, 0, 0, marker.Width, marker.Height, op);
		}
	}

	/// <summary>
	/// Draws the markers of all the various things going
	/// on around the world on top of the globe.
	/// </summary>
	public void DrawMarkers()
	{
		_markers.Clear();
		_markers.Lock();
		// Draw the base markers
		foreach (var xbase in _game.SavedGame.Bases)
		{
			DrawTarget(xbase, _markers);
		}

		// Draw the waypoint markers
		foreach (var wp in _game.SavedGame.Waypoints)
		{
			DrawTarget(wp, _markers);
		}

		// Draw the mission site markers
		foreach (var site in _game.SavedGame.MissionSites)
		{
			DrawTarget(site, _markers);
		}

		// Draw the alien base markers
		foreach (var ab in _game.SavedGame.AlienBases)
		{
			DrawTarget(ab, _markers);
		}

		// Draw the UFO markers
		foreach (var ufo in _game.SavedGame.Ufos)
		{
			if (ufo.Status == Ufo.UfoStatus.IGNORE_ME) continue;
			DrawTarget(ufo, _markers);
		}

		// Draw the craft markers
		foreach (var xbase in _game.SavedGame.Bases)
		{
			foreach (var xcraft in xbase.Crafts)
			{
				DrawTarget(xcraft, _markers);
			}
		}
		_markers.Unlock();
	}

	/// <summary>Blits the globe onto another surface.</summary>
	/// <param name="surface">Pointer to another surface.</param>
	public override void Blit(Surface surface)
	{
		base.Blit(surface);
		_radars.Blit(surface);
		_countries.Blit(surface);
		_markers.Blit(surface);
	}

	/// <summary>Mouse position (logical) as the C++ <c>(Sint16)floor(...)</c> arguments of cartToPolar.</summary>
	private void MouseToPolar(Action action, ref double lon, ref double lat)
	{
		CartToPolar(ToSint16(Math.Floor(action.AbsoluteXMouse)), ToSint16(Math.Floor(action.AbsoluteYMouse)), ref lon, ref lat);
	}

	/// <summary>Whether the drag-scroll button is held (C++ <c>SDL_GetMouseState(0, 0) &amp; SDL_BUTTON(...)</c>).</summary>
	private bool IsDragScrollButtonDown()
	{
		return 0 != (_game.MouseButtonState & SDLMouse.SDL_BUTTON(Options.geoDragScrollButton));
	}

	/// <summary>Whether the drag-scrolling was so short in time that it counts as a click.</summary>
	private bool IsDragScrollTooShort()
	{
		return (!_mouseMovedOverThreshold) && (unchecked((int)(SDLTimer.SDL_GetTicks() - _mouseScrollingStartTime)) <= (Options.dragScrollTimeTolerance));
	}

	/// <summary>Ignores any mouse hovers that are outside the globe.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void MouseOver(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		double lon = 0, lat = 0;
		MouseToPolar(action, ref lon, ref lat);

		if (_isMouseScrolling && action.Details.type == SDLEventType.SDL_MOUSEMOTION)
		{
			// The following is the workaround for a rare problem where sometimes
			// the mouse-release event is missed for any reason.
			// (checking: is the dragScroll-mouse-button still pressed?)
			// However if the SDL is also missed the release event, then it is to no avail :(
			if (!IsDragScrollButtonDown())
			{ // so we missed again the mouse-release :(
				// Check if we have to revoke the scrolling, because it was too short in time, so it was a click
				if (IsDragScrollTooShort())
				{
					Center(_lonBeforeMouseScrolling, _latBeforeMouseScrolling);
				}
				_isMouseScrolled = _isMouseScrolling = false;
				StopScrolling(action);
				return;
			}

			_isMouseScrolled = true;

			if (Options.touchEnabled == false)
			{
				// Set the mouse cursor back
				SDLEvents.SDL_EventState(SDLEventType.SDL_MOUSEMOTION, SDLEvents.SDL_IGNORE);
				SDLMouse.SDL_WarpMouse((ushort)((_game.Screen.WindowWidth - 100) / 2), (ushort)(_game.Screen.WindowHeight / 2));
				SDLEvents.SDL_EventState(SDLEventType.SDL_MOUSEMOTION, SDLEvents.SDL_ENABLE);
			}

			// Check the threshold
			_totalMouseMoveX += action.Details.motion.xrel;
			_totalMouseMoveY += action.Details.motion.yrel;

			if (!_mouseMovedOverThreshold)
				_mouseMovedOverThreshold = ((Math.Abs(_totalMouseMoveX) > Options.dragScrollPixelTolerance) || (Math.Abs(_totalMouseMoveY) > Options.dragScrollPixelTolerance));

			// Scrolling
			if (Options.geoDragScrollInvert)
			{
				double newLon = ((double)_totalMouseMoveX / action.XScale) * ROTATE_LONGITUDE / (_zoom + 1) / 2;
				double newLat = ((double)_totalMouseMoveY / action.YScale) * ROTATE_LATITUDE / (_zoom + 1) / 2;
				Center(_lonBeforeMouseScrolling + newLon / (Options.geoScrollSpeed / 10), _latBeforeMouseScrolling + newLat / (Options.geoScrollSpeed / 10));
			}
			else
			{
				double newLon = -action.Details.motion.xrel * ROTATE_LONGITUDE / (_zoom + 1) / 2;
				double newLat = -action.Details.motion.yrel * ROTATE_LATITUDE / (_zoom + 1) / 2;
				Center(_cenLon + newLon / (Options.geoScrollSpeed / 10), _cenLat + newLat / (Options.geoScrollSpeed / 10));
			}

			if (Options.touchEnabled == false)
			{
				// We don't want to see the mouse-cursor jumping :)
				action.SetMouseAction(_xBeforeMouseScrolling, _yBeforeMouseScrolling, X, Y);
				action.Details.motion.x = _xBeforeMouseScrolling; action.Details.motion.y = _yBeforeMouseScrolling;
			}

			_game.Cursor?.Handle(action);
		}

		if (Options.touchEnabled == false &&
			_isMouseScrolling &&
			(action.Details.motion.x != _xBeforeMouseScrolling ||
			action.Details.motion.y != _yBeforeMouseScrolling))
		{
			action.SetMouseAction(_xBeforeMouseScrolling, _yBeforeMouseScrolling, X, Y);
			action.Details.motion.x = _xBeforeMouseScrolling; action.Details.motion.y = _yBeforeMouseScrolling;
		}
		// Check for errors
		if (!double.IsNaN(lat) && !double.IsNaN(lon))
		{
			base.MouseOver(action, state);
		}
	}

	/// <summary>Ignores any mouse clicks that are outside the globe.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void MousePress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		double lon = 0, lat = 0;
		MouseToPolar(action, ref lon, ref lat);

		if (action.Details.button.button == Options.geoDragScrollButton)
		{
			_isMouseScrolling = true;
			_isMouseScrolled = false;
			// SDL_GetMouseState(&x, &y): the pointer's window position, which is that of this event
			_xBeforeMouseScrolling = action.Details.button.x;
			_yBeforeMouseScrolling = action.Details.button.y;
			_lonBeforeMouseScrolling = _cenLon;
			_latBeforeMouseScrolling = _cenLat;
			_totalMouseMoveX = 0; _totalMouseMoveY = 0;
			_mouseMovedOverThreshold = false;
			_mouseScrollingStartTime = SDLTimer.SDL_GetTicks();
		}
		// Check for errors
		if (!double.IsNaN(lat) && !double.IsNaN(lon))
		{
			base.MousePress(action, state);
		}
	}

	/// <summary>Ignores any mouse clicks that are outside the globe.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void MouseRelease(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		double lon = 0, lat = 0;
		MouseToPolar(action, ref lon, ref lat);
		if (action.Details.button.button == Options.geoDragScrollButton)
		{
			StopScrolling(action);
		}
		// Check for errors
		if (!double.IsNaN(lat) && !double.IsNaN(lon))
		{
			base.MouseRelease(action, state);
		}
	}

	/// <summary>
	/// Ignores any mouse clicks that are outside the globe
	/// and handles globe rotation and zooming.
	/// </summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void MouseClick(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELUP)
		{
			ZoomIn();
		}
		else if (action.Details.button.button == SDLMouse.SDL_BUTTON_WHEELDOWN)
		{
			ZoomOut();
		}

		double lon = 0, lat = 0;
		MouseToPolar(action, ref lon, ref lat);

		// The following is the workaround for a rare problem where sometimes
		// the mouse-release event is missed for any reason.
		// However if the SDL is also missed the release event, then it is to no avail :(
		// (this part handles the release if it is missed and now an other button is used)
		if (_isMouseScrolling)
		{
			if (action.Details.button.button != Options.geoDragScrollButton
				&& !IsDragScrollButtonDown())
			{ // so we missed again the mouse-release :(
				// Check if we have to revoke the scrolling, because it was too short in time, so it was a click
				if (IsDragScrollTooShort())
				{
					Center(_lonBeforeMouseScrolling, _latBeforeMouseScrolling);
				}
				_isMouseScrolled = _isMouseScrolling = false;
				StopScrolling(action);
			}
		}

		// DragScroll-Button release: release mouse-scroll-mode
		if (_isMouseScrolling)
		{
			// While scrolling, other buttons are ineffective
			if (action.Details.button.button == Options.geoDragScrollButton)
			{
				_isMouseScrolling = false;
				StopScrolling(action);
			}
			else
			{
				return;
			}
			// Check if we have to revoke the scrolling, because it was too short in time, so it was a click
			if (IsDragScrollTooShort())
			{
				_isMouseScrolled = false;
				StopScrolling(action);
				Center(_lonBeforeMouseScrolling, _latBeforeMouseScrolling);
			}
			if (_isMouseScrolled) return;
		}

		// Check for errors
		if (!double.IsNaN(lat) && !double.IsNaN(lon))
		{
			base.MouseClick(action, state);
			if (action.Details.button.button == SDLMouse.SDL_BUTTON_RIGHT)
			{
				Center(lon, lat);
			}
		}
	}

	/// <summary>Handles globe keyboard shortcuts.</summary>
	/// <param name="action">Pointer to an action.</param>
	/// <param name="state">State that the action handlers belong to.</param>
	public override void KeyboardPress(Action action, State state)
	{
		ArgumentNullException.ThrowIfNull(action);
		base.KeyboardPress(action, state);
		if (action.Details.key.keysym.sym == Options.keyGeoToggleDetail)
		{
			ToggleDetail();
		}
		if (action.Details.key.keysym.sym == Options.keyGeoToggleRadar)
		{
			ToggleRadarLines();
		}
	}

	/// <summary>Gets the polygon's texture and the shade at a given point.</summary>
	/// <param name="lon">Longitude of the point.</param>
	/// <param name="lat">Latitude of the point.</param>
	/// <param name="texture">Texture ID, -1 when no polygon is found.</param>
	/// <param name="shade">Shade (battlescape levels 0..15).</param>
	public void GetPolygonTextureAndShade(double lon, double lat, ref int texture, ref int shade)
	{
		///this is shade conversion from 0..31 levels of geoscape to battlescape levels 0..15
		ReadOnlySpan<int> worldshades =
		[
			0, 0, 0, 0, 1, 1, 2, 2,
			3, 3, 4, 4, 5, 5, 6, 6,
			7, 7, 8, 8, 9, 9, 10, 11,
			11, 12, 12, 13, 13, 14, 15, 15,
		];

		shade = worldshades[GetShadowValue(new Cord(0.0, 0.0, 1.0), GetSunDirection(lon, lat), 0)];
		Polygon t = GetPolygonFromLonLat(lon, lat);
		texture = (t == null) ? -1 : t.Texture;
	}

	/// <summary>Turns radar lines on or off.</summary>
	public void ToggleRadarLines()
	{
		Options.globeRadarLines = !Options.globeRadarLines;
		DrawRadars();
	}

	/// <summary>Resizes the geoscape.</summary>
	public void Resize()
	{
		Surface[] surfaces = [this, _markers, _countries, _radars];
		int width = Options.baseXGeoscape - 64;
		int height = Options.baseYGeoscape;

		for (int i = 0; i < 4; ++i)
		{
			surfaces[i].SetWidth(width);
			surfaces[i].SetHeight(height);
			surfaces[i].Invalidate();
		}
		_clipper.Wxrig = width;
		_clipper.Wybot = height;
		_cenX = unchecked((short)(width / 2));
		_cenY = unchecked((short)(height / 2));
		SetupRadii(width, height);
		Invalidate();
	}

	/// <summary>Sets up the radius of earth at the various zoom levels.</summary>
	/// <param name="width">The new width of the globe.</param>
	/// <param name="height">The new height of the globe.</param>
	private void SetupRadii(int width, int height)
	{
		_zoomRadius.Clear();

		_zoomRadius.Add(0.45 * height);
		_zoomRadius.Add(0.60 * height);
		_zoomRadius.Add(0.90 * height);
		_zoomRadius.Add(1.40 * height);
		_zoomRadius.Add(2.25 * height);
		_zoomRadius.Add(3.60 * height);

		_radius = _zoomRadius[_zoom];
		_radiusStep = (_zoomRadius[DOGFIGHT_ZOOM] - _zoomRadius[0]) / 10.0;

		if (Options.globeSurfaceCache)
		{
			while (_earthData.Count < _zoomRadius.Count)
				_earthData.Add(null);
			//filling normal field for each radius

			for (int r = 0; r < _zoomRadius.Count; ++r)
			{
				var data = new Cord[width * height];
				for (int j = 0; j < height; ++j)
					for (int i = 0; i < width; ++i)
					{
						data[width * j + i] = GlobeStaticData.CircleNorm(width / 2, height / 2, _zoomRadius[r], i + .5, j + .5);
					}
				_earthData[r] = data;
			}
		}
		else
		{
			_earthData.Clear();
		}
	}

	/// <summary>Moves the mouse back to where it started after we finish drag scrolling.</summary>
	/// <param name="action">Pointer to an action.</param>
	public void StopScrolling(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		SDLMouse.SDL_WarpMouse((ushort)_xBeforeMouseScrolling, (ushort)_yBeforeMouseScrolling);
		action.SetMouseAction(_xBeforeMouseScrolling, _yBeforeMouseScrolling, X, Y);
	}

	/// <summary>Sets craft range mode.</summary>
	public void SetCraftRange(double lon, double lat, double range)
	{
		_craft = (range > 0.0);
		_craftLon = lon;
		_craftLat = lat;
		_craftRange = range;
	}
}
