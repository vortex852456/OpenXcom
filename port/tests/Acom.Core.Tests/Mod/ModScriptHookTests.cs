using GameMod = Acom.Mod;

namespace Acom.Tests.Mod;

/// <summary>
/// Script hooks: the built-in default scripts compiled by the engine give the same results as the inline fallbacks
/// used for rules without loaded scripts, and scripts loaded from a ruleset change the hook outputs.
/// </summary>
public sealed class ModScriptHookTests
{
	private static readonly GameMod mod = new(createConverter: false);

	private static ScriptGroupContainer LoadDefaults(ScriptGroup group)
	{
		var c = group.CreateContainer();
		c.Load("test", new YamlRootNodeReader("{}", "test").ToBase(), group);
		return c;
	}

	[Fact]
	public void Default_vapor_particle_ammo_script_matches_inline_fallback()
	{
		var item = LoadDefaults(mod.ScriptParsers.battleItemScripts);
		var script = (ScriptContainerBase)item.Get(ModScript.VaporParticleAmmo);
		Assert.NotNull(script._parser);

		for (ulong seed = 1; seed < 50; ++seed)
		{
			var rngScript = new Rng.RandomState(seed);
			var rngInline = new Rng.RandomState(seed);
			int density = 48 + (int)(seed * 3);

			var arg = new ScriptOutputArgs(5, default(Position), default(Position), default(Position), Particle.SubVoxelAccuracy / 2, density, 40, 0);
			ModScript.Execute(script, arg, null, null, 10, 0, 0, default(Position), default(Position), default(Position), rngScript);

			Position offset = default, velocity = default;
			Projectile.DefaultVaporParticleAmmoScript(rngInline, ref offset, ref velocity, density);

			Assert.Equal(offset, (Position)arg.data[1]!);
			Assert.Equal(velocity, (Position)arg.data[2]!);
			Assert.Equal(rngInline.Seed, rngScript.Seed);
		}
	}

	[Fact]
	public void Default_psi_and_melee_item_scripts_match_inline_fallback()
	{
		var item = LoadDefaults(mod.ScriptParsers.battleItemScripts);
		var psi = (ScriptContainerEvents)item.Get(ModScript.TryPsiAttackItem);
		var melee = (ScriptContainerEvents)item.Get(ModScript.TryMeleeAttackItem);

		for (ulong seed = 1; seed < 50; ++seed)
		{
			int attack = (int)(seed * 7 % 120), defense = (int)(seed * 11 % 90), reduction = (int)(seed % 13), penalty = (int)(seed % 5);

			var rng = new Rng.RandomState(seed);
			int psiResult = ModScript.ScriptFunc1(psi, 0, null, null, null, null, attack, defense, (int)BattleActionType.BA_PANIC, rng, 5, reduction, null);
			var rngInline = new Rng.RandomState(seed);
			Assert.Equal(attack + rngInline.Generate(0, 55) - defense - reduction, psiResult);

			rng = new Rng.RandomState(seed);
			int meleeResult = ModScript.ScriptFunc1(melee, 0, null, null, null, null, attack, defense, (int)BattleActionType.BA_HIT, rng, 2, penalty, null);
			rngInline = new Rng.RandomState(seed);
			Assert.Equal(-rngInline.Generate(0, 99) + attack - defense + penalty, meleeResult);
		}
	}

	[Fact]
	public void Default_sprite_scripts_add_offset()
	{
		var unit = LoadDefaults(mod.ScriptParsers.battleUnitScripts);
		var item = LoadDefaults(mod.ScriptParsers.battleItemScripts);

		Assert.Equal(42 + 3, ModScript.ScriptFunc2((ScriptContainerEvents)unit.Get(ModScript.SelectUnitSprite), 42, 3, null, null, 0, 0, 0));
		Assert.Equal(17 + 5, ModScript.ScriptFunc2((ScriptContainerEvents)item.Get(ModScript.SelectItemSprite), 17, 5, null, null, 0, 0, 0));
	}

	[Fact]
	public void Armor_scripts_from_ruleset_change_hook_outputs()
	{
		var armor = new Armor("STR_TEST_ARMOR", 0);
		armor.Load(new YamlRootNodeReader(
			"type: STR_TEST_ARMOR\n" +
			"scripts:\n" +
			"  hitUnit: |\n" +
			"    mul power 2;\n" +
			"    set side 4;\n" +
			"    return power part side;\n" +
			"  damageUnit: |\n" +
			"    set to_health 7;\n" +
			"    add to_stun orig_power;\n" +
			"    return;\n", "test").ToBase(), mod);

		var hit = new ScriptOutputArgs(10, 1, 0);
		ModScript.Execute(armor.GetScript(ModScript.HitUnit), hit, null, null, null, null, null, null, 10, 0, 0);
		Assert.Equal(20, hit.GetInt(0));
		Assert.Equal(1, hit.GetInt(1));
		Assert.Equal(4, hit.GetInt(2));

		var damage = new ScriptOutputArgs(1, 2, 3, 4, 5, 6, 7, 8, 9);
		ModScript.Execute(armor.GetScript(ModScript.DamageUnit), damage, null, null, null, null, null, null, 15, 30, 0, 0, 0, 0);
		Assert.Equal(7, damage.GetInt(0));
		Assert.Equal(3 + 30, damage.GetInt(2));
		Assert.Equal(9, damage.GetInt(8));

		// script not defined: outputs unchanged
		var heal = new ScriptOutputArgs(1, 2, 3, 4, 5, 6, 7, 8, 9);
		ModScript.Execute(armor.GetScript(ModScript.HealUnit), heal, null, null, null, null, 0);
		Assert.Equal(new object?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, heal.data);
	}
}
