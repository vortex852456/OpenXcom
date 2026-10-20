#pragma warning disable CA1716 // namespace mirrors src/Mod like the other test folders
namespace Acom.Tests.Mod;

public class UfoTrajectoryTests
{
	[Fact]
	public void Loads_all_trajectories_of_the_standard_ruleset()
	{
		var path = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "ufoTrajectories.rul");
		var trajectories = new List<UfoTrajectory>();
		foreach (var node in YamlRootNodeReader.FromFile(path)["ufoTrajectories"].Children())
		{
			var trajectory = new UfoTrajectory(node["id"].ReadVal<string>());
			trajectory.Load(node);
			trajectories.Add(trajectory);
		}

		Assert.Equal(11, trajectories.Count);
		Assert.Equal(UfoTrajectory.RETALIATION_ASSAULT_RUN, trajectories[^1].ID);

		// P0: groundTimer 3000, waypoints [5, 4, 100], [0, 3, 74], [0, 1, 28], [1, 1, 47], [5, 2, 100]
		var p0 = trajectories[0];
		Assert.Equal("P0", p0.ID);
		Assert.Equal(3000, p0.GroundTimer());
		Assert.Equal(5, p0.WaypointCount);
		Assert.Equal(5, p0.GetZone(0));
		Assert.Equal(1, p0.GetZone(3));
		Assert.Equal("STR_VERY_HIGH", p0.GetAltitude(0));
		Assert.Equal("STR_HIGH_UC", p0.GetAltitude(1));
		Assert.Equal("STR_VERY_LOW", p0.GetAltitude(2));
		Assert.Equal(740, p0.ApplySpeedPercentage(1, 1000));
		Assert.Equal(13, p0.ApplySpeedPercentage(2, 49)); // 49 * 28 / 100 truncates
	}

	[Fact]
	public void RefNode_is_loaded_first_and_then_overridden()
	{
		const string yaml = """
			base: &base
			  groundTimer: 10
			  waypoints:
			    - [0, 1, 50]
			trajectory:
			  refNode: *base
			  waypoints:
			    - [1, 2, 100]
			    - [2, 0, 25]
			""";
		var trajectory = new UfoTrajectory("T");
		trajectory.Load(new YamlRootNodeReader(yaml, "test")["trajectory"]);

		Assert.Equal(10, trajectory.GroundTimer());
		Assert.Equal(2, trajectory.WaypointCount);
		Assert.Equal(2, trajectory.GetZone(1));
		Assert.Equal("STR_LOW_UC", trajectory.GetAltitude(0));
		Assert.Equal("STR_GROUND", trajectory.GetAltitude(1));
		Assert.Equal(250, trajectory.ApplySpeedPercentage(1, 1000));
	}

	[Fact]
	public void Defaults_without_data()
	{
		var trajectory = new UfoTrajectory("T");
		trajectory.Load(new YamlRootNodeReader("{}", "test").ToBase());
		Assert.Equal(5, trajectory.GroundTimer());
		Assert.Equal(0, trajectory.WaypointCount);
	}
}
