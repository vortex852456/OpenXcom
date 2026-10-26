using GameMod = Acom.Mod;

namespace Acom.Tests.Basescape;

/// <summary>Base grid logic of BaseView (placement errors, build queue) and the number helpers of the base screens.</summary>
[Collection("OptionsTests")]
public sealed class BaseViewTests : IDisposable
{
	private readonly string _dir = Directory.CreateTempSubdirectory("acom-baseview-").FullName + "/";
	private readonly GameMod _mod;

	public BaseViewTests()
	{
		Options.SetFoldersForTest(_dir, _dir, _dir);
		CrossPlatform.SetArgs([]);
		Options.Create();
		Options.ResetDefault(includeMods: false);
		GameMod.ResetGlobalStatics();
		_mod = new GameMod(createConverter: false);
		_mod._modData.Add(new ModData { name = "xcom1", offset = 0, size = 1000 });
		_mod._modCurrent = _mod._modData[0];
		_mod._sets["BASEBITS.PCK"] = new SurfaceSet(32, 40) { MaxSharedFrames = 50 };
	}

	public void Dispose()
	{
		CrossPlatform.SetArgs([]);
		Directory.Delete(_dir, recursive: true);
	}

	private RuleBaseFacility Rule(string type, string yaml)
	{
		var rule = new RuleBaseFacility(type, 0);
		rule.Load(new YamlRootNodeReader(yaml, "test").ToBase(), _mod);
		return rule;
	}

	private static BaseFacility Place(Base b, RuleBaseFacility rule, int x, int y, int buildTime = 0)
	{
		var fac = new BaseFacility(rule, b) { X = x, Y = y, BuildTime = buildTime };
		b.Facilities.Add(fac);
		return fac;
	}

	private static BaseView View(Base b, int gridX, int gridY)
	{
		var view = new BaseView(192, 192, 0, 8);
		view.SetBase(b);
		view.SetGridPosition(gridX, gridY);
		return view;
	}

	[Fact]
	public void Placement_needs_a_connection_inside_the_base()
	{
		var b = new Base(_mod);
		var lift = Rule("STR_LIFT", "lift: true\nbuildTime: 1");
		var small = Rule("STR_SMALL", "buildTime: 10");
		var hangar = Rule("STR_HANGAR", "size: 2\nbuildTime: 25");
		Place(b, lift, 2, 2);

		Assert.Equal(BasePlacementErrors.BPE_None, View(b, 3, 2).GetPlacementError(small));
		Assert.Equal(BasePlacementErrors.BPE_None, View(b, 2, 1).GetPlacementError(small));
		Assert.Equal(BasePlacementErrors.BPE_None, View(b, 0, 2).GetPlacementError(hangar)); // touches the lift on its right side
		Assert.Equal(BasePlacementErrors.BPE_NotConnected, View(b, 4, 4).GetPlacementError(small));
		Assert.Equal(BasePlacementErrors.BPE_NotConnected, View(b, 5, 5).GetPlacementError(hangar)); // outside the 6x6 grid
		// removing the lift would cut off the base (Base.IsAreaInUse)
		Assert.Equal(BasePlacementErrors.BPE_Used, View(b, 2, 2).GetPlacementError(small));
		// occupied squares: start facilities never build over, others need build-over rules
		Place(b, small, 3, 2);
		Assert.Equal(BasePlacementErrors.BPE_NotConnected, View(b, 3, 2).GetPlacementError(small, null, true));
		Assert.Equal(BasePlacementErrors.BPE_UpgradeDisallowed, View(b, 3, 2).GetPlacementError(small));
	}

	[Fact]
	public void Placement_next_to_unfinished_facilities_depends_on_the_build_queue()
	{
		var b = new Base(_mod);
		var lift = Rule("STR_LIFT", "lift: true\nbuildTime: 1");
		var small = Rule("STR_SMALL", "buildTime: 10");
		Place(b, lift, 0, 0);
		Place(b, small, 1, 0, buildTime: 5);

		Options.allowBuildingQueue = false;
		Assert.Equal(BasePlacementErrors.BPE_Queue, View(b, 2, 0).GetPlacementError(small));
		Options.allowBuildingQueue = true;
		Assert.Equal(BasePlacementErrors.BPE_None, View(b, 2, 0).GetPlacementError(small));
		Assert.True(View(b, 2, 0).IsQueuedBuilding(small));
		Assert.False(View(b, 0, 1).IsQueuedBuilding(small)); // next to the finished lift

		// moving a facility: it does not block itself, but others do
		var moved = b.Facilities[1];
		Assert.Equal(BasePlacementErrors.BPE_None, View(b, 1, 0).GetPlacementError(small, moved));
		Assert.Equal(BasePlacementErrors.BPE_NotConnected, View(b, 0, 0).GetPlacementError(small, moved));
	}

	[Fact]
	public void Queued_buildings_get_the_shortest_chain_of_build_times()
	{
		var b = new Base(_mod);
		var lift = Rule("STR_LIFT", "lift: true\nbuildTime: 1");
		var small = Rule("STR_SMALL", "buildTime: 10");
		Place(b, lift, 0, 0);
		var a = Place(b, small, 1, 0, buildTime: 4);       // being built
		var q1 = Place(b, small, 2, 0, buildTime: 100);    // queued behind a
		var q2 = Place(b, small, 3, 0, buildTime: 100);    // queued behind q1
		var q3 = Place(b, small, 0, 1, buildTime: 100);    // queued, but next to the lift (lift has no build time left)

		View(b, 0, 0).ReCalcQueuedBuildings();

		Assert.Equal(4, a.BuildTime);
		Assert.Equal(4 + 10, q1.BuildTime);
		Assert.Equal(4 + 10 + 10, q2.BuildTime);
		// the finished lift (build time 0) is not in the queue, so nothing shortens q3
		Assert.Equal(int.MaxValue, q3.BuildTime);
	}

	[Fact]
	public void Info_bars_scale_down_to_the_maximum_width()
	{
		var bar = new Bar(150, 5) { Scale = 0.5 };
		Options.oxceBaseInfoScaleEnabled = false;
		BaseInfoState.SetBar(bar, 300, 500, true);
		Assert.Equal(500, bar.Max);
		Assert.Equal(300, bar.Value);
		Assert.Equal(0.5, bar.Scale);

		Options.oxceBaseInfoScaleEnabled = true;
		BaseInfoState.SetBar(bar, 50, 200, true); // 200 * 0.5 < 140
		Assert.Equal(200, bar.Max);
		Assert.Equal(50, bar.Value);
		BaseInfoState.SetBar(bar, 299, 500, true); // integer division like C++
		Assert.Equal(140, bar.Max);
		Assert.Equal(299 * 140 / 500, bar.Value);
		Assert.Equal(1.0, bar.Scale);

		var personnel = new Bar(150, 5) { Scale = 1.0 };
		BaseInfoState.SetBar(personnel, 7, 140, false);
		Assert.Equal(140, personnel.Max);
		Assert.Equal(7, personnel.Value);
		Assert.Equal("3:12", BaseInfoState.Ratio(3, 12));
	}

	[Fact]
	public void Build_cost_item_names_are_cut_by_code_points()
	{
		Assert.Equal("Elerium: 5", PlaceFacilityState.ResourceLine("Elerium", 5));
		Assert.Equal("ABCDEFGHIJKLMNOPQRS: 5", PlaceFacilityState.ResourceLine("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 5));
		Assert.Equal("ABCDEFGHIJKLMNOPQ: 100", PlaceFacilityState.ResourceLine("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 100));
		Assert.Equal("\U0001F600BCDEFGHIJKLMNOPQR: 10", PlaceFacilityState.ResourceLine("\U0001F600BCDEFGHIJKLMNOPQRSTUVWXYZ", 10));
		Assert.Equal(("STR_CANNOT_BUILD_QUEUE_OFF", "BACK13.SCR"), PlaceFacilityState.PlacementErrorMessage(BasePlacementErrors.BPE_Queue));
		Assert.Equal(("STR_CANNOT_BUILD_HERE", "BACK01.SCR"), PlaceFacilityState.PlacementErrorMessage((BasePlacementErrors)99));
	}
}
