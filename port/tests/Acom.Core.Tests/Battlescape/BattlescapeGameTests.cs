using System.Reflection;
using System.Runtime.CompilerServices;
using static Acom.BattleActionType;
using static Acom.UnitFaction;

namespace Acom.Tests.Battlescape;

/// <summary>BattleActionCost/BattleAction value semantics, action costs and the BattleState queue order.</summary>
public class BattlescapeGameTests
{
	private static void SetField<T>(object obj, string name, T value) =>
		obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(obj, value);

	/// <summary>An alien with TU 50, energy 60, health 40, morale 100 (the defaults after construction).</summary>
	private static BattleUnit Unit()
	{
		var armor = new Armor("STR_TEST_ARMOR", 0);
		var unit = new BattleUnit(FACTION_HOSTILE, 1000001, armor, null, new Unit("STR_TEST_ALIEN"), new BattleUnitStatistics());
		unit.BaseStats = new UnitStats { tu = 50, stamina = 60, health = 40, bravery = 50, strength = 30, firing = 60 };
		unit.AdjustStats(new StatAdjustment { aimMultiplier = 1.0, armorMultiplier = 1.0 });
		return unit;
	}

	private static string Check(BattleUnit unit, short time, short energy = 0, short morale = 0, short health = 0, short stun = 0, short mana = 0)
	{
		var cost = new BattleActionCost(unit) { Time = time, Energy = energy, Morale = morale, Health = health, Stun = stun, Mana = mana };
		string message = "unchanged";
		bool ok = cost.HaveTU(ref message);
		Assert.Equal(ok, cost.HaveTU());
		return ok ? "ok" : message;
	}

	[Fact]
	public void HaveTU_checks_the_stats_in_cpp_order()
	{
		var unit = Unit();
		Assert.Equal(50, unit.TimeUnits);
		Assert.Equal(60, unit.Energy);
		Assert.Equal(40, unit.Health);

		// no action cost and no skill: no action, no message
		Assert.Equal("unchanged", Check(unit, 0));
		Assert.Equal("ok", Check(unit, 50, energy: 60));
		Assert.Equal("STR_NOT_ENOUGH_TIME_UNITS", Check(unit, 51, energy: 1000));
		Assert.Equal("STR_NOT_ENOUGH_ENERGY", Check(unit, 10, energy: 61, morale: 1000));
		Assert.Equal("STR_NOT_ENOUGH_MORALE", Check(unit, 10, morale: (short)(unit.Morale + 1), health: 1000));
		// health cost equal to the current health would kill: not allowed
		Assert.Equal("STR_NOT_ENOUGH_HEALTH", Check(unit, 10, health: 40, mana: 1000));
		Assert.Equal("STR_NOT_ENOUGH_MANA", Check(unit, 10, mana: (short)(unit.Mana + 1)));
		// stun + health must stay below health - stunlevel
		Assert.Equal("STR_NOT_ENOUGH_STUN", Check(unit, 10, health: 20, stun: 20));
		Assert.Equal("ok", Check(unit, 10, health: 20, stun: 19));
	}

	[Fact]
	public void SpendTU_spends_only_when_affordable()
	{
		var unit = Unit();
		var cost = new BattleActionCost(unit) { Time = 20, Energy = 5 };
		Assert.True(cost.SpendTU());
		Assert.Equal(30, unit.TimeUnits);
		Assert.Equal(55, unit.Energy);

		cost.Time = 31;
		string message = "";
		Assert.False(cost.SpendTU(ref message));
		Assert.Equal("STR_NOT_ENOUGH_TIME_UNITS", message);
		Assert.Equal(30, unit.TimeUnits);

		cost.ClearTU();
		Assert.Equal(0, cost.Time);
		Assert.Equal(0, cost.Energy);
		// without actor or weapon updateTU clears the cost
		var noWeapon = new BattleActionCost(BA_SNAPSHOT, unit, null) { Time = 5 };
		noWeapon.UpdateTU();
		Assert.Equal(0, noWeapon.Time);
	}

	[Fact]
	public void BattleAction_defaults_and_copy_constructor()
	{
		var a = new BattleAction();
		Assert.Equal(new Position(-1, -1, -1), a.target);
		Assert.Equal(new Position(0, 0, -1), a.cameraPosition);
		Assert.Equal(-1, a.finalFacing);
		Assert.Equal(BA_NONE, a.type);
		Assert.Equal("", a.result);
		Assert.Empty(a.waypoints);
		Assert.Equal(BattleActionOrigin.CENTRE, a.relativeOrigin);

		a.type = BA_LAUNCH;
		a.Time = 12;
		a.Energy = 3;
		a.waypoints.Add(new Position(1, 2, 3));
		a.target = new Position(4, 5, 6);
		a.autoShotCounter = 2;
		a.result = "STR_X";
		a.terrainMeleeTilePart = 4;
		a.relativeOrigin = BattleActionOrigin.LEFT;

		var b = new BattleAction(a);
		Assert.Equal(BA_LAUNCH, b.type);
		Assert.Equal(12, b.Time);
		Assert.Equal(3, b.Energy);
		Assert.Equal(a.target, b.target);
		Assert.Equal(2, b.autoShotCounter);
		Assert.Equal("STR_X", b.result);
		Assert.Equal(4, b.terrainMeleeTilePart);
		Assert.Equal(BattleActionOrigin.LEFT, b.relativeOrigin);
		Assert.Equal(a.waypoints, b.waypoints);

		// value semantics: the copy owns its waypoint list and cost
		b.waypoints.RemoveAt(0);
		b.Time = 1;
		Assert.Single(a.waypoints);
		Assert.Equal(12, a.Time);
	}

	[Fact]
	public void MoveType_prefers_strafe_then_run_then_sneak()
	{
		var a = new BattleAction();
		Assert.Equal(BattleActionMove.BAM_NORMAL, a.MoveType);
		a.sneak = true;
		Assert.Equal(BattleActionMove.BAM_SNEAK, a.MoveType);
		a.run = true;
		Assert.Equal(BattleActionMove.BAM_RUN, a.MoveType);
		a.strafe = true;
		Assert.Equal(BattleActionMove.BAM_STRAFE, a.MoveType);
	}

	/// <summary>Reference values from the C++ ProjectileFlyBState::getMaxThrowDistance (g++).</summary>
	[Theory]
	[InlineData(1, 35, -24, 1584)]
	[InlineData(1, 50, 0, 1608)]
	[InlineData(1, 70, 2, 4000)]
	[InlineData(3, 20, 26, 263)]
	[InlineData(3, 35, 50, 458)]
	[InlineData(3, 70, -24, 784)]
	[InlineData(5, 20, 0, 141)]
	[InlineData(5, 35, 2, 239)]
	[InlineData(5, 50, 26, 354)]
	[InlineData(5, 70, 50, 591)]
	[InlineData(10, 35, -24, 93)]
	[InlineData(10, 50, 0, 168)]
	[InlineData(10, 70, 2, 239)]
	[InlineData(20, 20, 26, 48)]
	[InlineData(20, 35, 50, 88)]
	[InlineData(20, 70, -24, 93)]
	[InlineData(40, 20, 0, 24)]
	[InlineData(40, 35, 2, 39)]
	[InlineData(40, 50, 26, 64)]
	[InlineData(40, 70, 50, 88)]
	public void Max_throw_distance_matches_cpp(int weight, int strength, int level, int expected)
	{
		Assert.Equal(expected, ProjectileFlyBState.GetMaxThrowDistance(weight, strength, level));
	}

	private sealed class FakeState(BattlescapeGame parent, string name, List<string> log) : BattleState(parent)
	{
		public string Name { get; } = name;

		public override void Init() => log.Add("init " + Name);
	}

	/// <summary>A BattlescapeGame with only the state queue set up (the real constructor needs the whole battlescape).</summary>
	private static BattlescapeGame QueueOnlyGame()
	{
		var game = (BattlescapeGame)RuntimeHelpers.GetUninitializedObject(typeof(BattlescapeGame));
		SetField(game, "_states", new List<BattleState>());
		SetField(game, "_deleted", new List<BattleState>());
		return game;
	}

	private static List<string> Names(BattlescapeGame game) => game.States.Select(s => ((FakeState)s).Name).ToList();

	[Fact]
	public void State_queue_order_and_init_calls()
	{
		var game = QueueOnlyGame();
		var log = new List<string>();
		Assert.False(game.IsBusy);

		// pushing into an empty queue starts the state immediately
		game.StatePushBack(new FakeState(game, "a", log));
		Assert.Equal(["init a"], log);
		Assert.True(game.IsBusy);

		// back/next into a non-empty queue: no init yet
		game.StatePushBack(new FakeState(game, "b", log));
		game.StatePushNext(new FakeState(game, "c", log));
		Assert.Equal(["init a"], log);
		Assert.Equal(["a", "c", "b"], Names(game));

		// push front always starts the state
		game.StatePushFront(new FakeState(game, "d", log));
		Assert.Equal(["init a", "init d"], log);
		Assert.Equal(["d", "a", "c", "b"], Names(game));

		// next is always second
		game.StatePushNext(new FakeState(game, "e", log));
		Assert.Equal(["d", "e", "a", "c", "b"], Names(game));

		// States returns a copy like the C++ std::list by value
		game.States.Clear();
		Assert.Equal(5, game.States.Count);
	}

	[Fact]
	public void StatePushNext_into_empty_queue_starts_the_state()
	{
		var game = QueueOnlyGame();
		var log = new List<string>();
		game.StatePushNext(new FakeState(game, "a", log));
		Assert.Equal(["init a"], log);
		Assert.Equal(["a"], Names(game));
	}

	[Fact]
	public void BattleState_without_action_has_cleared_cost()
	{
		var state = new FakeState(null!, "x", []);
		Assert.Equal("", state.Action.result);
		Assert.False(state.Action.targeting);
		Assert.Equal(0, state.Action.Time);

		var action = new BattleAction { type = BA_HIT, target = new Position(1, 1, 0) };
		action.waypoints.Add(new Position(2, 2, 0));
		var withAction = new MeleeAttackBState(null!, action);
		// the state keeps its own copy of the action
		action.waypoints.Clear();
		action.type = BA_NONE;
		Assert.Equal(BA_HIT, withAction.Action.type);
		Assert.Single(withAction.Action.waypoints);
	}
}
