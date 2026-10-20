namespace Acom.Tests.Savegame;

public class TargetTests
{
	private sealed class Mover : MovingTarget
	{
		public override string Type => "STR_MOVER";

		public override int Marker => -1;
	}

	private static YamlNodeReader Reparse(YamlNodeWriter writer) => new YamlRootNodeReader(writer.Emit(), "test").ToBase();

	[Fact]
	public void Coordinates_wrap_around_the_globe()
	{
		var w = new Waypoint();
		w.Longitude = -Math.PI / 2;
		Assert.Equal(3 * Math.PI / 2, w.Longitude, 12);
		w.Longitude = 2 * Math.PI + 1;
		Assert.Equal(1, w.Longitude, 12);

		// travelling past the north pole continues on the other side
		w.Longitude = 0;
		w.Latitude = Math.PI / 2 + 0.1;
		Assert.Equal(Math.PI / 2 - 0.1, w.Latitude, 12);
		Assert.Equal(Math.PI, w.Longitude, 12);
		w.Latitude = -Math.PI / 2 - 0.2;
		Assert.Equal(-Math.PI / 2 + 0.2, w.Latitude, 12);
		Assert.Equal(0, w.Longitude, 12);
	}

	[Fact]
	public void Distance_is_the_great_circle_angle()
	{
		var w = new Waypoint();
		Assert.Equal(0.0, w.GetDistance(0, 0));
		Assert.Equal(Math.PI / 2, w.GetDistance(Math.PI / 2, 0), 12);
		Assert.Equal(Math.PI / 2, w.GetDistance(0, Math.PI / 2), 12);
		var other = new Waypoint { Longitude = Math.PI, Latitude = 0 };
		Assert.Equal(Math.PI, w.GetDistance(other), 12);
	}

	[Fact]
	public void Waypoint_yaml_roundtrip_keeps_cpp_keys()
	{
		var w = new Waypoint { Longitude = 1.25, Latitude = -0.5, Id = 3 };
		var writer = new YamlRootNodeWriter();
		w.Save(writer);
		Assert.Equal("lon: 1.25\nlat: -0.5\nid: 3", writer.Emit().Trim().ReplaceLineEndings("\n"));

		var loaded = new Waypoint();
		loaded.Load(Reparse(writer));
		Assert.Equal(1.25, loaded.Longitude);
		Assert.Equal(-0.5, loaded.Latitude);
		Assert.Equal(3, loaded.Id);
		Assert.Equal("STR_WAY_POINT", loaded.Type);
		Assert.Equal(6, loaded.Marker);
		Assert.Equal("STR_WAY_POINT_", loaded.MarkerName);

		var idWriter = new YamlRootNodeWriter();
		w.SaveId(idWriter);
		Assert.Equal("lon: 1.25\nlat: -0.5\ntype: STR_WAY_POINT\nid: 3", idWriter.Emit().Trim().ReplaceLineEndings("\n"));

		// name and id 0 are optional
		w.SetName("Alpha");
		w.Id = 0;
		var named = new YamlRootNodeWriter();
		w.Save(named);
		Assert.Equal("lon: 1.25\nlat: -0.5\nname: Alpha", named.Emit().Trim().ReplaceLineEndings("\n"));
		Assert.Equal("Alpha", w.GetName(null));
	}

	[Fact]
	public void Moving_target_follows_and_reaches_its_destination()
	{
		var dest = new Waypoint { Longitude = 0.0, Latitude = 0.01 };
		var m = new Mover();
		m.Speed = 3600;
		Assert.Equal(3600 * (1 / 60.0) * (Math.PI / 180.0) / 720.0, m.SpeedRadian);

		m.Destination = dest;
		Assert.Same(dest, m.Destination);
		Assert.Same(m, Assert.Single(dest.Followers));
		Assert.False(m.ReachedDestination());

		int steps = 0;
		while (!m.ReachedDestination() && steps < 100)
		{
			m.Move();
			++steps;
		}
		Assert.Equal(7, steps); // 6 full steps of 0.00145 rad, then it snaps onto the destination
		Assert.Equal(dest.Latitude, m.Latitude);

		m.Destination = null;
		Assert.Empty(dest.Followers);
		Assert.False(m.ReachedDestination());

		m.Destination = dest;
		m.Destroy();
		Assert.Empty(dest.Followers);
		Assert.Null(m.Destination);
	}

	[Fact]
	public void Moving_target_save_writes_destination_id_and_exact_speeds()
	{
		var dest = new Waypoint { Longitude = 0.5, Latitude = 0.25, Id = 2 };
		var m = new Mover { Longitude = 0.5, Latitude = 0.0 };
		m.Speed = 1000;
		m.Destination = dest;

		var writer = new YamlRootNodeWriter();
		m.Save(writer);
		var r = Reparse(writer);
		var keys = r.Children().Select(c => c.Key).ToArray();
		Assert.Equal(["lon", "lat", "dest", "speedLon", "speedLat", "speedRadian", "speed"], keys);
		Assert.Equal("STR_WAY_POINT", r["dest"]["type"].ReadVal<string>());
		Assert.Equal(2, r["dest"]["id"].ReadVal<int>());
		Assert.Equal(SerializationHelper.SerializeDouble(m.SpeedRadian), r["speedRadian"].ReadVal<string>());

		var loaded = new Mover();
		loaded.Load(r);
		Assert.Equal(m.SpeedRadian, loaded.SpeedRadian);
		Assert.Equal(1000, loaded.Speed);
		Assert.Null(loaded.Destination); // destinations are linked by the owner after loading
	}
}
