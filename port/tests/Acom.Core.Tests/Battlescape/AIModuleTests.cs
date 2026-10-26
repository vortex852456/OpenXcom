using System.Reflection;
using static Acom.AIMode;
using static Acom.BattleActionType;
using static Acom.BattleMediKitType;
using static Acom.Tests.Savegame.SavegameTestHelpers;
using static Acom.UnitFaction;
using static Acom.UnitStatus;

namespace Acom.Tests.Battlescape;

/// <summary>
/// Deterministic AIModule logic that runs without the (still stubbed) BattlescapeGame/TileEngine/Pathfinding:
/// the mode decision (odds and RNG consumption), early outs of the medikit logic, YAML save/load and helpers.
/// </summary>
[Collection(nameof(Acom.Tests.Mod.GlobalRng))]
public class AIModuleTests
{
	private static void SetField<TOwner, T>(TOwner owner, string name, T value) =>
		typeof(TOwner).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(owner, value);

	private static T GetField<TOwner, T>(TOwner owner, string name) =>
		(T)typeof(TOwner).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;

	private static Armor TestArmor()
	{
		var armor = new Armor("STR_TEST_ARMOR", 0);
		foreach (var bonus in new[] { armor.TimeRecoveryRaw, armor.EnergyRecoveryRaw, armor.MoraleRecoveryRaw, armor.HealthRecoveryRaw, armor.ManaRecoveryRaw, armor.StunRegenerationRaw })
		{
			bonus.Load("STR_TEST_ARMOR", default, "recovery");
		}
		return armor;
	}

	/// <summary>An alien with full TU/energy/health (aggression 0 from the blank unit rules).</summary>
	private static BattleUnit Alien(int id = 1000001)
	{
		var unit = new BattleUnit(FACTION_HOSTILE, id, TestArmor(), null, new Unit("STR_TEST_ALIEN"), new BattleUnitStatistics());
		unit.BaseStats = new UnitStats { tu = 50, stamina = 60, health = 40, bravery = 50, strength = 30, firing = 60 };
		unit.AdjustStats(new StatAdjustment { aimMultiplier = 1.0, armorMultiplier = 1.0 });
		return unit;
	}

	/// <summary>
	/// An AIModule without running the constructor (it needs BattleAction, still a stub); only the fields used by
	/// the tested functions are set.
	/// </summary>
	private static AIModule Module(BattleUnit unit, SavedBattleGame save)
	{
		var ai = Uninitialized<AIModule>();
		SetField(ai, "_unit", unit);
		SetField(ai, "_save", save);
		SetField(ai, "_wasHitBy", new List<int>());
		SetField(ai, "_targetFaction", FACTION_PLAYER);
		return ai;
	}

	/// <summary>
	/// Sets up a unit that knows one enemy, has a rifle and an escape route but no ambush, so the odds are
	/// patrol 33 (30 * 1.1), escape 3 (5 * 1.4 / 2), combat 16 (20 * 1.2 * 0.7), ambush 0.
	/// </summary>
	private static AIModule KnownEnemyModule(out Node toNode)
	{
		var ai = Module(Alien(), Uninitialized<SavedBattleGame>());
		SetField(ai, "_AIMode", AI_PATROL);
		SetField(ai, "_knownEnemies", 1);
		SetField(ai, "_escapeTUs", 5);
		SetField(ai, "_rifle", true);
		toNode = new Node();
		SetField(ai, "_toNode", toNode);
		return ai;
	}

	private static ulong FindSeed(Func<int, bool> wanted, int max)
	{
		for (ulong seed = 1; seed < 10000; ++seed)
		{
			Rng.Seed = seed;
			if (wanted(Rng.Generate(1, max)))
				return seed;
		}
		throw new InvalidOperationException("no seed found");
	}

	[Fact]
	public void Evaluate_without_known_enemies_patrols_and_rolls_once()
	{
		var ai = Module(Alien(), Uninitialized<SavedBattleGame>());
		SetField(ai, "_AIMode", AI_AMBUSH);
		SetField(ai, "_toNode", new Node());

		Rng.Seed = 1234;
		ai.EvaluateAIMode();
		ulong after = Rng.GlobalRandomState.Seed;

		// hostile unit without known enemies: only patrol odds remain, the single roll always picks patrol
		Assert.Equal(AI_PATROL, GetField<AIModule, AIMode>(ai, "_AIMode"));
		Rng.Seed = 1234;
		Rng.Generate(1, 30);
		Assert.Equal(Rng.GlobalRandomState.Seed, after);
	}

	[Fact]
	public void Evaluate_splits_the_roll_into_escape_combat_and_patrol_ranges()
	{
		// escape: 1..3
		ulong seed = FindSeed(d => d <= 3, 52);
		var ai = KnownEnemyModule(out _);
		Rng.Seed = seed;
		ai.EvaluateAIMode();
		Assert.Equal(AI_ESCAPE, GetField<AIModule, AIMode>(ai, "_AIMode"));

		// patrol: 20..52 (kept because a patrol node is set)
		seed = FindSeed(d => d > 19, 52);
		ai = KnownEnemyModule(out _);
		Rng.Seed = seed;
		ai.EvaluateAIMode();
		Assert.Equal(AI_PATROL, GetField<AIModule, AIMode>(ai, "_AIMode"));

		// the boundary: 19 is still combat, 20 is patrol
		seed = FindSeed(d => d == 20, 52);
		ai = KnownEnemyModule(out _);
		Rng.Seed = seed;
		ai.EvaluateAIMode();
		Assert.Equal(AI_PATROL, GetField<AIModule, AIMode>(ai, "_AIMode"));
	}

	[Fact]
	public void Evaluate_aggressive_units_scale_combat_and_escape_odds()
	{
		// aggression 2: combat 24 * 1.4 = 33, escape 5 * 0.7 = 3 -> /2 = 1; total 33 + 1 + 33 = 67
		ulong seed = FindSeed(d => d == 1, 67);
		var ai = KnownEnemyModule(out _);
		SetField(GetField<AIModule, BattleUnit>(ai, "_unit"), "_aggression", 2);
		Rng.Seed = seed;
		ai.EvaluateAIMode();
		Assert.Equal(AI_ESCAPE, GetField<AIModule, AIMode>(ai, "_AIMode"));

		seed = FindSeed(d => d == 35, 67); // 2..34 would be combat, 35..67 patrol
		ai = KnownEnemyModule(out _);
		SetField(GetField<AIModule, BattleUnit>(ai, "_unit"), "_aggression", 2);
		Rng.Seed = seed;
		ai.EvaluateAIMode();
		Assert.Equal(AI_PATROL, GetField<AIModule, AIMode>(ai, "_AIMode"));
	}

	[Fact]
	public void Medikit_early_outs_do_not_touch_the_rng()
	{
		var unit = Alien();
		var ai = Module(unit, Uninitialized<SavedBattleGame>());
		Rng.Seed = 99;
		ulong before = Rng.GlobalRandomState.Seed;

		Assert.False(ai.Medikit_think(BMT_HEAL)); // no fatal wounds
		Assert.False(ai.Medikit_think(BMT_STIMULANT)); // no stun, full energy
		Assert.False(ai.Medikit_think(BMT_PAINKILLER)); // unsupported
		Assert.False(ai.Medikit_think(BMT_NORMAL)); // unsupported
		Assert.Equal(before, Rng.GlobalRandomState.Seed);

		unit.BaseStats = new UnitStats { tu = 50, stamina = 0, health = 40 };
		Assert.False(ai.Medikit_think(BMT_HEAL)); // division by zero guard
	}

	[Fact]
	public void Was_hit_by_ignores_own_faction_and_duplicates()
	{
		var ai = Module(Alien(), Uninitialized<SavedBattleGame>());
		var friend = Alien(1000002);
		var enemy = new BattleUnit(FACTION_PLAYER, 7, TestArmor(), null, new Unit("STR_TEST_X"), new BattleUnitStatistics());

		ai.SetWasHitBy(friend);
		ai.SetWasHitBy(enemy);
		ai.SetWasHitBy(enemy);

		Assert.False(ai.GetWasHitBy(1000002));
		Assert.True(ai.GetWasHitBy(7));
		Assert.Equal([7], GetField<AIModule, List<int>>(ai, "_wasHitBy"));
	}

	[Fact]
	public void Save_writes_the_cpp_flow_map()
	{
		var save = Uninitialized<SavedBattleGame>();
		var ai = Module(Alien(), save);
		SetField(ai, "_fromNode", new Node(3, new Position(1, 2, 0), 0, 0, 0, 0, 0, 0));
		SetField(ai, "_AIMode", AI_COMBAT);
		SetField(ai, "_wasHitBy", new List<int> { 4, 9 });
		ai.SetWeaponPickedUp();

		var writer = new YamlRootNodeWriter();
		ai.Save(writer);
		Assert.Equal("{fromNode: 3, toNode: -1, AIMode: 2, wasHitBy: [4, 9], weaponPickedUp: true}", writer.Emit().Trim());

		// a hostile unit turned neutral that targets hostiles also saves its target faction (as a number)
		var unit = Alien();
		SetField(unit, "_faction", FACTION_NEUTRAL);
		ai = Module(unit, save);
		ai.SetTargetFaction(FACTION_HOSTILE);
		writer = new YamlRootNodeWriter();
		ai.Save(writer);
		Assert.Equal("{fromNode: -1, toNode: -1, AIMode: 0, wasHitBy: [], targetFaction: 1}", writer.Emit().Trim());
	}

	[Fact]
	public void Load_restores_fields_and_ignores_junk_nodes()
	{
		var save = Uninitialized<SavedBattleGame>();
		var nodes = new List<Node> { new Node(0, new Position(0, 0, 0), 0, 0, 0, 0, 0, 0), new Node(1, new Position(5, 5, 0), 0, 0, 0, 0, 0, 0) };
		SetField(save, "_nodes", nodes);
		var ai = Module(Alien(), save);

		ai.Load(Yaml("{fromNode: 1, toNode: 7, AIMode: 3, wasHitBy: [2], weaponPickedUp: true, targetFaction: 2}"));

		Assert.Same(nodes[1], GetField<AIModule, Node>(ai, "_fromNode"));
		Assert.Null(GetField<AIModule, Node>(ai, "_toNode"));
		Assert.Equal(AI_ESCAPE, GetField<AIModule, AIMode>(ai, "_AIMode"));
		Assert.True(ai.GetWasHitBy(2));
		Assert.True(GetField<AIModule, bool>(ai, "_weaponPickedUp"));
		Assert.Equal(FACTION_NEUTRAL, GetField<AIModule, UnitFaction>(ai, "_targetFaction"));

		// missing keys: AIMode defaults to patrol, nodes stay
		ai.Load(Yaml("{}"));
		Assert.Equal(AI_PATROL, GetField<AIModule, AIMode>(ai, "_AIMode"));
		Assert.Same(nodes[1], GetField<AIModule, Node>(ai, "_fromNode"));
	}

	[Fact]
	public void Proxy_grenade_shifts_sort_stable_by_distance()
	{
		var shifts = new List<(Position first, int second)>
		{
			(new Position(1, 0, 0), 5),
			(new Position(0, 1, 0), 2),
			(new Position(-1, 0, 0), 5),
			(new Position(0, -1, 0), 2),
		};
		AIModule.SortShifts(shifts);
		Assert.Equal(
			[new Position(0, 1, 0), new Position(0, -1, 0), new Position(1, 0, 0), new Position(-1, 0, 0)],
			shifts.Select(s => s.first));
	}

	[Fact]
	public void Copy_action_is_independent_of_the_original()
	{
		var action = Uninitialized<BattleAction>();
		action.type = BA_LAUNCH;
		action.Time = 12;
		action.target = new Position(1, 2, 3);
		action.waypoints = [new Position(4, 5, 6)];
		action.result = "STR_X";
		action.finalFacing = 5;
		action.number = 3;

		// the copy is created with the BattleAction constructor (BattlescapeGame is still a stub)
		BattleAction copy = OrSkip(() => BattleState.CopyAction(action));
		copy.waypoints.Add(new Position(7, 8, 9));
		copy.Time = 1;

		Assert.Equal(BA_LAUNCH, copy.type);
		Assert.Equal(new Position(1, 2, 3), copy.target);
		Assert.Equal("STR_X", copy.result);
		Assert.Equal(5, copy.finalFacing);
		Assert.Equal(3, copy.number);
		Assert.Single(action.waypoints);
		Assert.Equal(12, action.Time);
	}

	[Fact]
	public void Panic_state_think_without_berserk_restores_the_unit()
	{
		var unit = Alien();
		SetField(unit, "_status", STATUS_PANICKING);
		int morale = unit.Morale;
		var parent = Uninitialized<BattlescapeGame>();

		// the C++ base constructor clears the action (BattleAction is still a stub), so build the state without it
		var state = Uninitialized<UnitPanicBState>();
		SetField(state, "_unit", unit);
		SetField<BattleState, BattlescapeGame>(state, "_parent", parent);
		SetField(state, "_berserking", false);

		// not berserking: TUs are cleared and morale recovers by 15 before the state is popped
		Record.Exception(state.Think); // popState/setupCursor of the still stubbed BattlescapeGame
		Assert.Equal(0, unit.TimeUnits);
		Assert.Equal(morale + 15, unit.Morale);
	}
}
