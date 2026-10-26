namespace Acom.Tests.Geoscape;

/// <summary>
/// Dogfight step logic (distance changes, weapon fire, hit and damage rolls) on a stand-in campaign with fixed seeds.
/// </summary>
[Collection(nameof(Acom.Tests.Engine.GameTests))]
public sealed class DogfightStateTests : IDisposable
{
	private const string Rules = """
		interfaces:
		  - type: dogfight
		    palette: PAL_GEOSCAPE
		    elements:
		      - { id: standoffButton, color: 1 }
		      - { id: cautiousButton, color: 1 }
		      - { id: standardButton, color: 1 }
		      - { id: aggressiveButton, color: 1 }
		      - { id: disengageButton, color: 1 }
		      - { id: ufoButton, color: 1 }
		      - { id: numbers, color: 2 }
		      - { id: distance, color: 2 }
		      - { id: oceanIndicator, color: 2 }
		      - { id: text, color: 2 }
		      - { id: minimizedNumber, color: 2 }
		      - { id: minimizeButtonDummy, color: 3 }
		      - { id: previewTop, x: 0, y: 96, w: 160, h: 10 }
		      - { id: previewMid, x: 0, y: 106, w: 160, h: 20 }
		      - { id: previewBot, x: 0, y: 126, w: 160, h: 10 }
		      - { id: craftRange, color: 160, color2: 176 }
		      - { id: radarRange, color: 112, color2: 128 }
		      - { id: damageRange, color: 32, color2: 40 }
		      - { id: radarDetail, color: 96, color2: 104 }
		      - { id: disabledWeapon, color: 64, color2: 65 }
		      - { id: disabledAmmo, color: 66 }
		      - { id: shieldRange, color: 192, color2: 200 }
		crafts:
		  - type: STR_TEST_CRAFT
		    weapons: 1
		    fuelMax: 1000
		    damageMax: 1000
		    speedMax: 2000
		    accel: 3
		craftWeapons:
		  - type: STR_TEST_CANNON
		    damage: 20
		    range: 20
		    accuracy: 70
		    reloadCautious: 12
		    reloadStandard: 8
		    reloadAggressive: 4
		    ammoMax: 100
		    projectileType: 2
		    projectileSpeed: 8
		ufos:
		  - type: STR_TEST_UFO
		    size: STR_MEDIUM_UC
		    power: 0
		    range: 0
		    score: 100
		    reload: 30
		    breakOffTime: 500
		    damageMax: 100000
		    speedMax: 1000
		""";

	private readonly GeoscapeTestGame _env = new(Rules);

	public void Dispose() => _env.Dispose();

	private (GeoscapeState geo, Craft craft, Ufo ufo) NewSetup()
	{
		var geo = new GeoscapeState();
		var craftRule = _env.Mod.GetCraft("STR_TEST_CRAFT");
		// sprite 0 (the rule loader needs a mod being loaded to resolve sprite offsets)
		typeof(RuleCraft).GetField("_sprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(craftRule, 0);
		var craft = new Craft(_env.Mod.GetCraft("STR_TEST_CRAFT"), _env.Save.Bases[0], 1);
		craft.Weapons[0] = new CraftWeapon(_env.Mod.GetCraftWeapon("STR_TEST_CANNON"), 100);
		_env.Save.Bases[0].Crafts.Add(craft);
		var ufo = new Ufo(_env.Mod.GetUfo("STR_TEST_UFO"), 1) { Longitude = GeoscapeTestGame.Rad(20), Latitude = GeoscapeTestGame.Rad(5) };
		var mission = new AlienMission(new RuleAlienMission("STR_TEST_MISSION"));
		mission.Race = "STR_TEST_RACE";
		ufo.SetMissionInfo(mission, new UfoTrajectory("P0"));
		ufo.Shield = ufo.CraftStats.shieldCapacity; // GeoscapeState fills the shield of a new UFO (the constructor leaves -1)
		_env.Save.Ufos.Add(ufo);
		craft.Destination = ufo;
		return (geo, craft, ufo);
	}

	/// <summary>A left click like GeoscapeState.handleDogfightMultiAction sends (it switches the mode button group).</summary>
	private static Action LeftPress()
	{
		var ev = new SDL_Event();
		ev.type = SDLEventType.SDL_MOUSEBUTTONDOWN;
		ev.button.button = SDLMouse.SDL_BUTTON_LEFT;
		return new Action(ev, 0.0, 0.0, 0, 0);
	}

	private static int Dist(DogfightState df) => GeoscapeTestGame.Field<int>(df, "_currentDist");

	private static int TargetDist(DogfightState df) => GeoscapeTestGame.Field<int>(df, "_targetDist");

	[Fact]
	public void Chance_to_hit_formulas()
	{
		// vanilla: (accuracy * (100 + 300 / (5 - size)) + 100) / 200
		Assert.Equal(70, DogfightState.CraftChanceToHit(70, 2, 0, 0, 0));
		Assert.Equal((70 * (100 + 300 / 5) + 100) / 200, DogfightState.CraftChanceToHit(70, 0, 0, 0, 0));
		Assert.Equal((70 * (100 + 300 / 1) + 100) / 200 - 10 + 5 + 3, DogfightState.CraftChanceToHit(70, 4, 10, 5, 3));
		Assert.Equal(60 - 5 + 10 - 4, DogfightState.UfoChanceToHit(60, 5, 10, 4, false));
		Assert.Equal((60 - 5 + 10 - 4) / 2, DogfightState.UfoChanceToHit(60, 5, 10, 4, true));
	}

	[Fact]
	public void Construction_sets_interception_order_and_escape_countdown_from_the_seed()
	{
		var (geo, craft, ufo) = NewSetup();
		Rng.Seed = 1234;
		int expectedEscape = Math.Max(1, 500 + Rng.Generate(0, 500) - 30 * _env.Save.DifficultyCoefficient);
		Rng.Seed = 1234;
		var df = new DogfightState(geo, craft, ufo);
		Assert.Equal(expectedEscape, ufo.EscapeCountdown);
		Assert.Equal(1, craft.InterceptionOrder);
		Assert.True(craft.IsInDogfight);
		Assert.False(df.IsUfoAttacking);
		Assert.Equal(640, Dist(df));
		Assert.Equal(DogfightState.STANDOFF_DIST, TargetDist(df));
	}

	[Fact]
	public void Distance_steps_follow_the_attack_mode()
	{
		var (geo, craft, ufo) = NewSetup();
		var df = new DogfightState(geo, craft, ufo);

		// standoff: approach at 2 per step (no pilots) until 560
		df.Update();
		Assert.Equal(638, Dist(df));
		for (int i = 0; i < 100; ++i)
			df.Update();
		Assert.Equal(560, Dist(df));

		// aggressive: minimum weapon range (20 km * 8), at most 64
		df.BtnAggressiveSimulateLeftPress(LeftPress());
		Assert.Equal(DogfightState.AGGRESSIVE_DIST, TargetDist(df));
		// standard: shortest weapon range with ammo
		df.BtnStandardSimulateLeftPress(LeftPress());
		Assert.Equal(20 * 8, TargetDist(df));
		// cautious: longest weapon range with ammo
		df.BtnCautiousSimulateLeftPress(LeftPress());
		Assert.Equal(20 * 8, TargetDist(df));

		// disengage: move away at 2 * acceleration bonus (no pilots = 2) per step
		df.BtnDisengageSimulateLeftPress(LeftPress());
		Assert.Equal(800, TargetDist(df));
		int before = Dist(df);
		df.Update();
		Assert.Equal(before + 4, Dist(df));
		Assert.False(df.DogfightEnded());

		// leaving the radar range while disengaging ends the dogfight
		for (int i = 0; i < 200 && !df.DogfightEnded(); ++i)
			df.Update();
		Assert.True(df.DogfightEnded());
		Assert.False(craft.IsInDogfight);
		Assert.Equal(0, craft.InterceptionOrder);
	}

	[Fact]
	public void First_hit_uses_the_seeded_hit_and_damage_rolls()
	{
		const ulong seed = 987654321;
		var (geo, craft, ufo) = NewSetup();

		// replay the RNG calls of the dogfight: escape countdown, then a hit roll per projectile until one hits,
		// then the vanilla 50-100% damage roll
		Rng.Seed = seed;
		Rng.Generate(0, 500);
		int chance = DogfightState.CraftChanceToHit(70, 2, 0, 0, 0);
		int misses = 0;
		while (!Rng.Percent(chance))
			++misses;
		int expectedDamage = Rng.Generate(20 / 2, 20);

		Rng.Seed = seed;
		var df = new DogfightState(geo, craft, ufo);
		df.BtnAggressiveSimulateLeftPress(LeftPress());
		int steps = 0;
		while (ufo.Damage == 0 && steps < 5000)
		{
			df.Update();
			++steps;
		}
		Assert.Equal(expectedDamage, ufo.Damage);
		Assert.True(100 - craft.Weapons[0].Ammo >= misses + 1); // every roll belongs to a fired round
		Assert.Equal(3, ufo.HitFrame);
		Assert.False(ufo.IsCrashed);
	}
}
