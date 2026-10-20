using System.Reflection;
using static Acom.Tests.Savegame.SavegameTestHelpers;
using static Acom.UnitBodyPart;
using static Acom.UnitFaction;
using static Acom.UnitSide;
using static Acom.UnitStatus;

namespace Acom.Tests.Savegame;

/// <summary>BattleUnit logic that only needs the already ported rules (Armor, Unit, RuleDamageType).</summary>
[Collection(nameof(Acom.Tests.Mod.GlobalRng))]
public class BattleUnitTests
{
	/// <summary>A blank armor whose stat bonuses (recovery formulas) are compiled like after loading a ruleset.</summary>
	private static Armor TestArmor()
	{
		var armor = new Armor("STR_TEST_ARMOR", 0);
		foreach (var bonus in new[] { armor.TimeRecoveryRaw, armor.EnergyRecoveryRaw, armor.MoraleRecoveryRaw, armor.HealthRecoveryRaw, armor.ManaRecoveryRaw, armor.StunRegenerationRaw })
		{
			bonus.Load("STR_TEST_ARMOR", default, "recovery");
		}
		return armor;
	}

	private static void SetField<T>(BattleUnit unit, string name, T value) =>
		typeof(BattleUnit).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(unit, value);

	/// <summary>An alien with the given stats; current TU/energy/health/mana are filled like after construction.</summary>
	private static BattleUnit Alien(Armor? armor = null, short tu = 50, short stamina = 60, short health = 40, short bravery = 50, short strength = 30)
	{
		var unit = new BattleUnit(FACTION_HOSTILE, 1000001, armor ?? TestArmor(), null, new Unit("STR_TEST_ALIEN"), new BattleUnitStatistics());
		unit.BaseStats = new UnitStats { tu = tu, stamina = stamina, health = health, bravery = bravery, strength = strength, firing = 60 };
		// identity adjustment: only copies the stats into the current values (C++ does this in updateArmorFromNonSoldier)
		unit.AdjustStats(new StatAdjustment { aimMultiplier = 1.0, armorMultiplier = 1.0 });
		SetField(unit, "_standHeight", 20);
		SetField(unit, "_kneelHeight", 14);
		return unit;
	}

	[Fact]
	public void Direction_to_neighbours_and_far_points()
	{
		var unit = Alien();
		unit.SetPosition(new Position(10, 10, 0));
		int[] expected = [0, 1, 2, 3, 4, 5, 6, 7];
		(int x, int y)[] offsets = [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)];
		for (int i = 0; i < offsets.Length; ++i)
		{
			Assert.Equal(expected[i], unit.DirectionTo(new Position(10 + offsets[i].x, 10 + offsets[i].y, 0)));
		}
		// atan2(3, 1) = 71.6 degrees: between the 1/8 boundaries of direction 2
		Assert.Equal(2, unit.DirectionTo(new Position(13, 9, 5)));
		// atan2(1, 3) = 18.4 degrees: still direction 0
		Assert.Equal(0, unit.DirectionTo(new Position(11, 7, 0)));
		// the own position: atan2(+0, -0) = pi like in C
		Assert.Equal(4, unit.DirectionTo(new Position(10, 10, 0)));
	}

	[Fact]
	public void Turning_takes_the_shorter_way_and_stops_on_target()
	{
		var unit = Alien();
		unit.Direction = 0;
		unit.LookAt(3);
		Assert.Equal(STATUS_TURNING, unit.Status);
		unit.Turn();
		Assert.Equal(1, unit.Direction);
		unit.Turn();
		unit.Turn();
		Assert.Equal(3, unit.Direction);
		Assert.Equal(STATUS_STANDING, unit.Status);

		// 0 -> 6 turns counter-clockwise through 7, the turret does not follow without a turret
		unit.Direction = 0;
		unit.LookAt(6);
		unit.Turn();
		Assert.Equal(7, unit.Direction);
		Assert.Equal(0, unit.TurretDirection);
		unit.Turn();
		Assert.Equal(6, unit.Direction);
		Assert.Equal(STATUS_STANDING, unit.Status);

		// a difference of exactly 4 turns clockwise, -4 also clockwise
		unit.Direction = 0;
		unit.LookAt(4);
		unit.Turn();
		Assert.Equal(1, unit.Direction);
		unit.Direction = 4;
		unit.LookAt(0);
		unit.Turn();
		Assert.Equal(5, unit.Direction);

		// with a turret the turret follows the body
		unit.TurretType = 0;
		unit.Direction = 2;
		unit.LookAt(1);
		unit.Turn();
		Assert.Equal(1, unit.Direction);
		Assert.Equal(1, unit.TurretDirection);

		// turret only
		unit.LookAt(new Position(unit.Position.x + 1, unit.Position.y + 1, 0), true);
		Assert.Equal(3, unit.TurretToDirection);
		unit.Turn(true);
		unit.Turn(true);
		Assert.Equal(3, unit.TurretDirection);
		Assert.Equal(1, unit.Direction);
		Assert.Equal(STATUS_STANDING, unit.Status);

		// invalid directions are ignored, forced ones are set directly
		unit.LookAt(8);
		Assert.Equal(STATUS_STANDING, unit.Status);
		unit.LookAt(5, true);
		Assert.Equal(5, unit.Direction);
		Assert.Equal(STATUS_STANDING, unit.Status);
		unit.LookAt(5);
		unit.Turn();
		Assert.Equal(STATUS_STANDING, unit.Status);
	}

	[Fact]
	public void View_sector_is_a_quarter_cone()
	{
		var unit = Alien();
		unit.SetPosition(new Position(10, 10, 0));
		unit.Direction = 0; // north: y decreases
		Assert.True(unit.CheckViewSector(new Position(10, 5, 0)));
		Assert.True(unit.CheckViewSector(new Position(14, 6, 0)));
		Assert.False(unit.CheckViewSector(new Position(15, 6, 0)));
		Assert.False(unit.CheckViewSector(new Position(10, 15, 0)));
		unit.Direction = 3; // south-east
		Assert.True(unit.CheckViewSector(new Position(12, 13, 0)));
		Assert.False(unit.CheckViewSector(new Position(9, 13, 0)));
	}

	[Fact]
	public void Hit_location_from_relative_position_and_rng_order()
	{
		var unit = Alien();
		unit.Direction = 0;

		(UnitSide side, UnitBodyPart part) Hit(int x, int y, int z, Rng.RandomState rand)
		{
			var side = SIDE_FRONT;
			var part = BODYPART_TORSO;
			unit.CalculateHitLocation(new Position(x, y, z), rand, ref side, ref part);
			return (side, part);
		}

		Rng.Seed = 42;
		var rand = new Rng.RandomState(7);
		Assert.Equal((SIDE_UNDER, BODYPART_TORSO), Hit(0, 0, -2, rand));
		Assert.Equal((SIDE_FRONT, BODYPART_HEAD), Hit(0, -5, 20, rand));
		Assert.Equal((SIDE_RIGHT, BODYPART_RIGHTARM), Hit(5, 0, 10, rand));
		Assert.Equal((SIDE_LEFT, BODYPART_LEFTLEG), Hit(-5, 0, 2, rand));
		Assert.Equal((SIDE_REAR, BODYPART_TORSO), Hit(0, 5, 10, rand));
		Assert.Equal(42UL, Rng.Seed); // no random side so far
		Assert.Equal(7UL, rand.Seed);

		// a leg hit from the front/rear picks the leg with the damage sub-sequence
		var expectedRand = new Rng.RandomState(7);
		var leg = (UnitBodyPart)expectedRand.Generate((int)BODYPART_RIGHTLEG, (int)BODYPART_LEFTLEG);
		Assert.Equal((SIDE_REAR, leg), Hit(0, 5, 0, rand));
		Assert.Equal(expectedRand.Seed, rand.Seed);

		// diagonal hits pick the side with the global generator (2:1 for the front)
		var expectedGlobal = new Rng.RandomState(42);
		var side = expectedGlobal.Generate(0, 2) < 2 ? SIDE_FRONT : SIDE_RIGHT;
		Assert.Equal(side, Hit(5, -5, 10, rand).side);
		Assert.Equal(expectedGlobal.Seed, Rng.Seed);

		// kneeling lowers the head, the facing rotates the sides
		unit.Kneel(true);
		Assert.Equal((SIDE_FRONT, BODYPART_HEAD), Hit(0, -5, 14, rand));
		unit.Kneel(false);
		unit.Direction = 2;
		Assert.Equal((SIDE_FRONT, BODYPART_TORSO), Hit(5, 0, 10, rand));
	}

	[Fact]
	public void Resistance_bravery_morale_and_armor_bounds()
	{
		var armor = TestArmor();
		var unit = Alien(armor, bravery: 30);
		Assert.Equal(30, unit.ReduceByResistance(30, ItemDamageType.DT_AP));
		var modifiers = (float[])typeof(Armor).GetField("_damageModifier", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(armor)!;
		modifiers[(int)ItemDamageType.DT_AP] = 0.55f;
		Assert.Equal(16, unit.ReduceByResistance(30, ItemDamageType.DT_AP)); // floor(16.5)

		Assert.Equal(8, unit.ReduceByBravery(10)); // (110 - 30) * 10 / 100
		Assert.Equal(0, unit.Morale);
		unit.MoraleChange(150);
		Assert.Equal(100, unit.Morale);
		unit.MoraleChange(-130);
		Assert.Equal(0, unit.Morale);

		Assert.Equal(0, unit.GetMaxArmor(SIDE_FRONT));
		unit.SetArmor(5, SIDE_FRONT);
		Assert.Equal(0, unit.GetArmor(SIDE_FRONT)); // clamped to max armor
		unit.SetMaxArmor(12, SIDE_FRONT);
		unit.SetArmor(20, SIDE_FRONT);
		Assert.Equal(12, unit.GetArmor(SIDE_FRONT));
		unit.SetMaxArmor(8, SIDE_FRONT);
		Assert.Equal(8, unit.GetArmor(SIDE_FRONT));

		unit.AdjustStats(new StatAdjustment
		{
			statGrowth = new UnitStats { health = 50 },
			growthMultiplier = 1,
			aimMultiplier = 1.5,
			armorMultiplier = 1.25,
			armorMultiplierAbs = 1,
		});
		Assert.Equal(60, unit.Health); // +50% of 40
		Assert.Equal(90, unit.BaseStats.firing);
		Assert.Equal(11, unit.GetMaxArmor(SIDE_FRONT)); // 8 * 1.25 + 1
		Assert.Equal(1, unit.GetMaxArmor(SIDE_REAR));
		Assert.Equal(11, unit.GetArmor(SIDE_FRONT));
	}

	[Fact]
	public void Wounds_healing_painkillers_and_stimulants()
	{
		var unit = Alien(health: 40, stamina: 60);
		unit.SetFatalWound(2, BODYPART_RIGHTARM);
		unit.SetFatalWound(1, BODYPART_HEAD);
		unit.SetFatalWound(3, BODYPART_MAX); // ignored
		Assert.Equal(3, unit.FatalWounds);
		Assert.Equal(0, unit.GetFatalWound(BODYPART_MAX));
		Assert.Equal(90, unit.GetAccuracyModifier()); // 25 * health% + 75, -10 per head wound (arm wounds only count for the weapon's hand)

		unit.HealStun(-10); // stun damage
		Assert.Equal(10, unit.Stunlevel);
		Assert.False(unit.IsOutThresholdExceed);

		unit.SpendEnergy(50);
		unit.Stimulant(30, 4, 0);
		Assert.Equal(40, unit.Energy);
		unit.Stimulant(30, 0, 0);
		Assert.Equal(60, unit.Energy); // capped at stamina
		Assert.Equal(6, unit.Stunlevel);

		unit.Heal(BODYPART_RIGHTARM, 1, 5);
		Assert.Equal(1, unit.GetFatalWound(BODYPART_RIGHTARM));
		Assert.Equal(40, unit.Health); // capped at max health
		unit.Heal(BODYPART_LEFTLEG, 1, 5); // no wound there: nothing happens
		Assert.Equal(0, unit.GetFatalWound(BODYPART_LEFTLEG));

		unit.MoraleChange(20);
		SetField(unit, "_health", 30);
		unit.PainKillers(5, 2.0f); // 10 lost health * 2 = 20 morale restored + 5
		Assert.Equal(45, unit.Morale);
		unit.PainKillers(0, 2.0f); // already restored
		Assert.Equal(45, unit.Morale);
	}

	[Fact]
	public void New_turn_recovers_tu_and_energy_with_wound_penalties()
	{
		var unit = Alien(tu: 50, stamina: 60, health: 40);
		unit.SetFatalWound(1, BODYPART_LEFTLEG);
		unit.SetFatalWound(1, BODYPART_TORSO);
		unit.MoraleChange(100);
		Assert.True(unit.SpendTimeUnits(30));
		Assert.False(unit.SpendTimeUnits(30));
		Assert.True(unit.SpendEnergy(25));
		unit.HealStun(-3);

		Rng.Seed = 5;
		unit.PrepareNewTurn();
		Assert.Equal(45, unit.TimeUnits); // full 50, -10% for one leg wound
		Assert.Equal(60, unit.Energy); // 35 + 30 - 10% of 35
		Assert.Equal(38, unit.Health); // two fatal wounds
		Assert.Equal(2, unit.Stunlevel);
		Assert.Equal(STATUS_STANDING, unit.Status);
		Assert.Equal(0, unit.ExpStats.bravery); // panic chance -100 gives no experience
		var expected = new Rng.RandomState(5);
		expected.Generate(0, 99); // RNG::percent of the panic check
		Assert.Equal(expected.Seed, Rng.Seed);
	}

	[Fact]
	public void New_turn_panic_uses_berserk_chance()
	{
		var unit = Alien();
		Assert.Equal(0, unit.Morale);
		Rng.Seed = 11;
		unit.PrepareNewTurn();
		var expected = new Rng.RandomState(11);
		Assert.True(expected.Generate(0, 99) < 100);
		var status = expected.Generate(0, 2) == 0 ? STATUS_BERSERK : STATUS_PANICKING;
		Assert.Equal(status, unit.Status);
		Assert.True(unit.WantsToSurrender());
		Assert.Equal(expected.Seed, Rng.Seed);
	}

	[Fact]
	public void Spotting_counters_saturate_like_uint8()
	{
		var unit = Alien();
		Assert.Equal(255, unit.TurnsSinceSpotted);
		unit.UpdateTurnsSince();
		Assert.Equal(255, unit.TurnsSinceSpotted);
		Assert.Equal(0, unit.TurnsLeftSpottedForSnipers);
		unit.SetTurnsSinceSpottedByFaction(FACTION_PLAYER, 300);
		Assert.Equal(255, unit.GetTurnsSinceSpottedByFaction(FACTION_PLAYER));
		unit.SetTurnsSinceSpottedByFaction(FACTION_NONE, 1); // out of range: ignored
		Assert.Equal(0, unit.GetTurnsSinceSpottedByFaction(FACTION_NONE));
		unit.TurnsSinceSpotted = 258; // plain setter truncates
		Assert.Equal(2, unit.TurnsSinceSpotted);
		unit.SetTurnsLeftSpottedForSnipersByFaction(FACTION_HOSTILE, 2);
		unit.UpdateTurnsSince();
		Assert.Equal(3, unit.TurnsSinceSpotted);
		Assert.Equal(1, unit.TurnsLeftSpottedForSnipers);
		unit.ResetTurnsSince();
		Assert.Equal(255, unit.TurnsSinceSpotted);
		Assert.Equal(0, unit.TurnsLeftSpottedForSnipers);
	}

	[Fact]
	public void Yaml_round_trip_of_a_minimal_unit()
	{
		var armor = TestArmor();
		var unit = Alien(armor);
		unit.SetPosition(new Position(3, 4, 1));
		unit.Direction = 5;
		unit.Kneel(true);
		unit.SetMaxArmor(12, SIDE_FRONT);
		unit.SetArmor(7, SIDE_FRONT);
		unit.SetFatalWound(2, BODYPART_LEFTLEG);
		unit.AddFiringExp();
		unit.AddFiringExp();
		unit.AddPsiSkillExp();
		unit.SetTurnsSinceSpottedByFaction(FACTION_PLAYER, 4);
		unit.SetMeleeAttackedBy(7);
		unit.MurdererWeapon = "STR_RIFLE";
		unit.ToggleLeftHandForReactions(false);

		var writer = new YamlRootNodeWriter();
		unit.Save(writer);
		var saved = writer.ToReader();
		var keys = Keys(saved);
		Assert.Equal(["id", "genUnitType", "genUnitArmor", "faction", "status", "position", "direction", "directionTurret", "tu", "health", "mana", "stunlevel", "energy", "morale", "kneeled", "armor", "fatalWounds", "fire"], keys.Take(18));
		Assert.DoesNotContain("wantsToSurrender", keys);
		Assert.DoesNotContain("floating", keys);
		Assert.DoesNotContain("turnsSinceSpottedByCivilian", keys);
		Assert.DoesNotContain("AI", keys);
		Assert.Contains("turnsSinceSpottedByXcom", keys);
		Assert.Contains("moveCost", keys); // C++ leaves the move costs at {0, 0} until the armor is applied
		Assert.Equal("STR_TEST_ARMOR", saved["genUnitArmor"].ReadVal<string>());
		Assert.Equal("1", saved["faction"].Val);
		Assert.Equal([7, 0, 0, 0, 0], saved["armor"].ReadVal<List<int>>());
		Assert.Equal(2, saved["expFiring"].ReadVal<int>());
		Assert.Equal("STR_LEFT_HAND", saved["preferredHandForReactions"].ReadVal<string>());

		// SavedBattleGame reads genUnitType/genUnitArmor/originalFaction and creates the unit, then BattleUnit::load does the rest
		var copy = new BattleUnit(FACTION_HOSTILE, 0, armor, null, new Unit("STR_TEST_ALIEN"), new BattleUnitStatistics());
		copy.Load(saved, null);
		Assert.Equal(new Position(3, 4, 1), copy.Position);
		Assert.Equal(5, copy.Direction);
		Assert.True(copy.IsKneeled);
		Assert.Equal(0, copy.GetMaxArmor(SIDE_FRONT)); // max armor comes from the rules, not the save
		Assert.Equal(7, copy.GetArmor(SIDE_FRONT));
		Assert.Equal(2, copy.GetFatalWound(BODYPART_LEFTLEG));
		Assert.Equal(4, copy.GetTurnsSinceSpottedByFaction(FACTION_PLAYER));
		Assert.True(copy.WasMeleeAttackedBy(7));
		Assert.True(copy.IsLeftHandPreferredForReactions);

		var again = new YamlRootNodeWriter();
		copy.Save(again);
		Assert.Equal(writer.Emit(), again.Emit());
	}

	[Fact]
	public void Load_truncates_uint8_counters_like_try_read_as_int()
	{
		var unit = Alien();
		unit.Load(Yaml("{turnsSinceSpotted: 300, turnsLeftSpottedForSnipersByCivilian: -1, turnsSinceStunned: 3, recolor: [[16, 5], [32, 7]]}"), null);
		Assert.Equal(44, unit.TurnsSinceSpotted);
		Assert.Equal(255, unit.GetTurnsLeftSpottedForSnipersByFaction(FACTION_NEUTRAL));
		Assert.Equal(3, unit.TurnsSinceStunned);
		Assert.Equal([((byte)16, (byte)5), ((byte)32, (byte)7)], unit.Recolor);
	}

	[Fact]
	public void Damage_reduces_armor_and_health()
	{
		var unit = Alien(health: 40);
		unit.SetMaxArmor(10, SIDE_FRONT);
		unit.SetArmor(10, SIDE_FRONT);
		var type = new RuleDamageType { RandomStun = false, RandomWound = false, ToWound = 0.0f, ToStun = 0.0f };
		// SavedBattleGame is still a stub: an uninitialized one is a plain battle (no preview, no battle state)
		int done = OrSkip(() => unit.Damage(new Position(0, -5, 20), 30, type, Uninitialized<SavedBattleGame>(), new BattleActionAttack()));
		Assert.Equal(20, done);
		Assert.Equal(20, unit.Health);
		Assert.Equal(8, unit.GetArmor(SIDE_FRONT)); // round(20 * 0.1)
		Assert.Equal(SIDE_FRONT, unit.FatalShotSide);
		Assert.Equal(BODYPART_HEAD, unit.FatalShotBodyPart);
		Assert.True(unit.HitState);
	}
}
