using static Acom.Tests.Battlescape.MapTestHelpers;
using static Acom.UnitStatus;

namespace Acom.Tests.Battlescape;

/// <summary>
/// Frame selection of the drawing routines. Every frame i of the synthetic sprite sheet is a single pixel at column i,
/// so the columns set on the destination tell which frames were drawn and the row tells the y offset.
/// </summary>
public class UnitSpriteTests
{
	private const int Frames = 300;

	private static Acom.Mod TestMod()
	{
		var mod = new Acom.Mod();
		var sheet = new SurfaceSet(Frames, 10);
		for (int i = 0; i < Frames; ++i)
		{
			sheet.AddFrame(i).SetPixel(i, 0, 1);
		}
		mod._sets["TEST.PCK"] = sheet;
		mod._sets["HANDOB.PCK"] = new SurfaceSet(32, 40);
		mod._sets["SMOKE.PCK"] = new SurfaceSet(32, 40);
		mod._sets["DETBLOB.DAT"] = new SurfaceSet(16, 16);
		return mod;
	}

	private static BattleUnit TestUnit(int drawingRoutine, int dir, UnitStatus status = STATUS_STANDING, int walkPhase = 0, bool kneeled = false, SoldierGender gender = SoldierGender.GENDER_MALE)
	{
		var armor = new Armor("STR_TEST_ARMOR", 0);
		SetField(armor, typeof(Armor), "_spriteSheet", "TEST.PCK");
		SetField(armor, typeof(Armor), "_drawingRoutine", drawingRoutine);
		var unit = new BattleUnit(UnitFaction.FACTION_PLAYER, 1, armor, null, null, new BattleUnitStatistics());
		unit.Direction = dir;
		SetField(unit, typeof(BattleUnit), "_status", status);
		SetField(unit, typeof(BattleUnit), "_walkPhase", walkPhase);
		SetField(unit, typeof(BattleUnit), "_kneeled", kneeled);
		SetField(unit, typeof(BattleUnit), "_gender", gender);
		SetField(unit, typeof(BattleUnit), "_standHeight", 22);
		return unit;
	}

	/// <summary>Draws the unit and returns the drawn frames with their y offsets, in frame order.</summary>
	private static List<(int frame, int offY)> Draw(BattleUnit unit, int part = 0, int animFrame = 0)
	{
		var dest = new Surface(Frames, 10, 0, 0, 1);
		var sprite = new UnitSprite(dest, TestMod(), null, animFrame, false, 3, 14);
		sprite.Draw(unit, part, 0, 0, 0, new AreaSubset<GraphTag, int>(Frames, 10), false);
		var result = new List<(int frame, int offY)>();
		for (int x = 0; x < Frames; ++x)
		{
			for (int y = 0; y < 10; ++y)
			{
				if (dest.GetPixel(x, y) != 0)
				{
					result.Add((x, y));
				}
			}
		}
		return result;
	}

	[Theory]
	[InlineData(0)]
	[InlineData(2)]
	[InlineData(7)]
	public void Soldier_standing_uses_stand_frames(int dir)
	{
		// left arm 0, right arm 8, legs 16, male torso 32 (+ direction)
		Assert.Equal([(dir, 0), (8 + dir, 0), (16 + dir, 0), (32 + dir, 0)], Draw(TestUnit(0, dir)));
	}

	[Fact]
	public void Soldier_female_torso()
	{
		Assert.Equal([(3, 0), (11, 0), (19, 0), (267 + 3, 0)], Draw(TestUnit(0, 3, gender: SoldierGender.GENDER_FEMALE)));
	}

	[Fact]
	public void Soldier_kneeling_lowers_everything_but_legs()
	{
		Assert.Equal([(1, 4), (9, 4), (24 + 1, 0), (32 + 1, 4)], Draw(TestUnit(0, 1, kneeled: true)));
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(3, 2)]
	[InlineData(6, 5)]
	public void Soldier_walking_uses_walk_cycle(int dir, int phase)
	{
		int[] yoffWalk = [1, 0, -1, 0, 1, 0, -1, 0];
		int f = 24 * dir + phase;
		var drawn = Draw(TestUnit(0, dir, STATUS_WALKING, phase));
		// arms/legs come from the walk cycle, the fixed torso bobs up and down (negative offsets fall outside the surface)
		var expected = new List<(int, int)> { (40 + f, 0), (48 + f, 0), (56 + f, 0) };
		if (yoffWalk[phase] >= 0)
		{
			expected.Add((32 + dir, yoffWalk[phase]));
		}
		expected.Sort();
		Assert.Equal(expected, drawn);
	}

	[Fact]
	public void Soldier_collapsing_draws_only_the_death_frame()
	{
		var unit = TestUnit(0, 2, STATUS_COLLAPSING);
		SetField(unit, typeof(BattleUnit), "_fallPhase", 2);
		Assert.Equal([(264 + 2, 0)], Draw(unit));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(3)]
	[InlineData(7)]
	public void Floater_walking_has_five_frames_per_direction(int dir)
	{
		// routine 1: torso = 24 + 5 * dir + (int)(phase / 1.6), arms 8 / 0 (+ dir)
		for (int phase = 0; phase < 8; ++phase)
		{
			int torso = 24 + 5 * dir + (int)(phase / 1.6);
			var expected = new List<(int, int)> { (dir, 0), (8 + dir, 0), (torso, 0) };
			expected.Sort();
			Assert.Equal(expected, Draw(TestUnit(1, dir, STATUS_WALKING, phase)));
		}
	}

	[Theory]
	[InlineData(4, 2, false, 0, 0 + 2)]          // civilian standing
	[InlineData(4, 2, true, 5, 8 + 8 * 2 + 5)]   // civilian walking
	[InlineData(17, 1, false, 0, 64 + 2)]        // tftd civilian: stand frames are converted
	[InlineData(18, 6, true, 3, 76 + 8 * 6 + 3)]
	[InlineData(19, 5, true, 0, 8 + 5)]          // tentaculat
	public void Single_frame_routines(int routine, int dir, bool walking, int phase, int frame)
	{
		Assert.Equal([(frame, 0)], Draw(TestUnit(routine, dir, walking ? STATUS_WALKING : STATUS_STANDING, phase)));
	}

	[Theory]
	[InlineData(5, 1, 0, false, 0, 1 * 8 + 0)]
	[InlineData(5, 3, 2, true, 6, 32 + 3 * 4 + 2 * 16 + 3)]
	[InlineData(21, 2, 1, false, 5, 2 * 4 + 1 * 16 + 1)]
	[InlineData(12, 3, 0, false, 13, 3 * 8 + 5)]
	public void Large_unit_parts(int routine, int part, int dir, bool walking, int animFrame, int frame)
	{
		// sectopod standing: part * 8 + dir; walking: 32 + part * 4 + dir * 16 + (phase / 2) % 4
		// xarquid: part * 4 + dir * 16 + anim % 4; hallucinoid: part * 8 + anim % 8
		var unit = TestUnit(routine, dir, walking ? STATUS_WALKING : STATUS_STANDING, walking ? animFrame : 0);
		Assert.Equal([(frame, 0)], Draw(unit, part, animFrame));
	}

	[Fact]
	public void Unknown_routine_throws()
	{
		Assert.Throws<InvalidOperationException>(() => Draw(TestUnit(23, 0)));
	}
}
