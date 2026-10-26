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
// Ported from: src/Mod/ModScript.h
// Parser constructors ported from: src/Savegame/BattleUnit.cpp, src/Savegame/BattleItem.cpp, src/Mod/RuleStatBonus.cpp,
// src/Mod/RuleSoldierBonus.cpp, src/Savegame/Country.cpp, src/Savegame/Ufo.cpp, src/Ufopaedia/StatsForNerdsState.cpp
#nullable disable

#pragma warning disable CA1034 // nested types mirror C++ nested types.
#pragma warning disable CA1711 // C++ names are kept.
#pragma warning disable CA1708 // C++ names: group types and group members differ by case.

using static Acom.ScriptParam;

namespace Acom;

/// <summary>Main class handling all script types used by the game.</summary>
public sealed class ModScript
{
	private readonly ScriptGlobal _shared;
	private readonly Mod _mod;

	private static ScriptArgName A(string name, ScriptParam type) => new(name, type);

	/// <summary>C++ <c>ScriptOutputArgs&lt;int&amp;, int&gt;</c>.</summary>
	private static ScriptArgName[] Output(string first, string second) => [A(first, IntRef), A(second, Int)];

	////////////////////////////////////////////////////////////
	//					common bindings
	////////////////////////////////////////////////////////////

	/// <summary>C++ <c>commonImpl</c> (BattleUnit.cpp).</summary>
	private static void CommonImpl(BindBase b, Mod mod)
	{
		b.AddCustomPtr<Mod>("rules", mod);

		b.AddCustomConst("blit_torso", (int)UnitBodyPart.BODYPART_TORSO);
		b.AddCustomConst("blit_leftarm", (int)UnitBodyPart.BODYPART_LEFTARM);
		b.AddCustomConst("blit_rightarm", (int)UnitBodyPart.BODYPART_RIGHTARM);
		b.AddCustomConst("blit_legs", (int)UnitBodyPartEx.BODYPART_LEGS);
		b.AddCustomConst("blit_collapse", (int)UnitBodyPartEx.BODYPART_COLLAPSING);

		b.AddCustomConst("blit_large_torso_0", (int)UnitBodyPartEx.BODYPART_LARGE_TORSO + 0);
		b.AddCustomConst("blit_large_torso_1", (int)UnitBodyPartEx.BODYPART_LARGE_TORSO + 1);
		b.AddCustomConst("blit_large_torso_2", (int)UnitBodyPartEx.BODYPART_LARGE_TORSO + 2);
		b.AddCustomConst("blit_large_torso_3", (int)UnitBodyPartEx.BODYPART_LARGE_TORSO + 3);
		b.AddCustomConst("blit_large_propulsion_0", (int)UnitBodyPartEx.BODYPART_LARGE_PROPULSION + 0);
		b.AddCustomConst("blit_large_propulsion_1", (int)UnitBodyPartEx.BODYPART_LARGE_PROPULSION + 1);
		b.AddCustomConst("blit_large_propulsion_2", (int)UnitBodyPartEx.BODYPART_LARGE_PROPULSION + 2);
		b.AddCustomConst("blit_large_propulsion_3", (int)UnitBodyPartEx.BODYPART_LARGE_PROPULSION + 3);
		b.AddCustomConst("blit_large_turret", (int)UnitBodyPartEx.BODYPART_LARGE_TURRET);
	}

	/// <summary>C++ <c>commonImpl</c> (BattleItem.cpp).</summary>
	private static void CommonItemImpl(BindBase b, Mod mod)
	{
		b.AddCustomPtr<Mod>("rules", mod);

		b.AddCustomConst("blit_item_righthand", (int)UnitBodyPartEx.BODYPART_ITEM_RIGHTHAND);
		b.AddCustomConst("blit_item_lefthand", (int)UnitBodyPartEx.BODYPART_ITEM_LEFTHAND);
		b.AddCustomConst("blit_item_floor", (int)UnitBodyPartEx.BODYPART_ITEM_FLOOR);
		b.AddCustomConst("blit_item_big", (int)UnitBodyPartEx.BODYPART_ITEM_INVENTORY);
	}

	/// <summary>C++ <c>battleActionImpl</c> (BattleUnit.cpp).</summary>
	private static void BattleActionImpl(BindBase b)
	{
		b.AddCustomConst("battle_action_aimshoot", (int)BattleActionType.BA_AIMEDSHOT); //TODO: fix name, it require some new logic in script to allow old typo for backward compatiblity
		b.AddCustomConst("battle_action_autoshoot", (int)BattleActionType.BA_AUTOSHOT);
		b.AddCustomConst("battle_action_snapshot", (int)BattleActionType.BA_SNAPSHOT);
		b.AddCustomConst("battle_action_walk", (int)BattleActionType.BA_WALK);
		b.AddCustomConst("battle_action_hit", (int)BattleActionType.BA_HIT);
		b.AddCustomConst("battle_action_throw", (int)BattleActionType.BA_THROW);
		b.AddCustomConst("battle_action_use", (int)BattleActionType.BA_USE);
		b.AddCustomConst("battle_action_mindcontrol", (int)BattleActionType.BA_MINDCONTROL);
		b.AddCustomConst("battle_action_panic", (int)BattleActionType.BA_PANIC);
		b.AddCustomConst("battle_action_cqb", (int)BattleActionType.BA_CQB);
	}

	/// <summary>C++ <c>moveTypesImpl</c> (BattleUnit.cpp).</summary>
	private static void MoveTypesImpl(BindBase b)
	{
		b.AddCustomConst("move_normal", (int)BattleActionMove.BAM_NORMAL);
		b.AddCustomConst("move_run", (int)BattleActionMove.BAM_RUN);
		b.AddCustomConst("move_strafe", (int)BattleActionMove.BAM_STRAFE);
		b.AddCustomConst("move_sneak", (int)BattleActionMove.BAM_SNEAK);
	}

	/// <summary>C++ <c>medikitBattleActionImpl</c> (BattleUnit.cpp).</summary>
	private static void MedikitBattleActionImpl(BindBase b)
	{
		b.AddCustomConst("medikit_action_heal", (int)BattleMediKitAction.BMA_HEAL);
		b.AddCustomConst("medikit_action_stimulant", (int)BattleMediKitAction.BMA_STIMULANT);
		b.AddCustomConst("medikit_action_painkiller", (int)BattleMediKitAction.BMA_PAINKILLER);
	}

	/// <summary>C++ <c>commonBattleUnitAnimations</c> (BattleUnit.cpp).</summary>
	private static void CommonBattleUnitAnimations(ScriptParserBase parser)
	{
		var bu = new Bind<BattleUnit>(parser, new BindBase.ExtensionBinding());

		bu.AddGet("isFloorAbove", u => u.FloorAbove, "check if floor is shown above unit");
		bu.AddGet("getBreathExhaleFrame", u => u.BreathExhaleFrame, "return animation frame of breath bubbles, -1 means no animation");
		bu.AddGet("getBreathInhaleFrame", u => u.BreathInhaleFrame, "return number of frames to next breath animation start, 0 means animation started, -1 no animation");

		SavedBattleGame.ScriptRegisterUnitAnimations(parser);
	}

	/// <summary>C++ <c>commonBattleItemAnimations</c> (BattleItem.cpp).</summary>
	private static void CommonBattleItemAnimations(ScriptParserBase parser)
	{
		SavedBattleGame.ScriptRegisterUnitAnimations(parser);
	}

	/// <summary>C++ <c>burnShadeScript</c> (BattleUnit.cpp): <c>(int&amp; curr, int burn, int shade)</c>.</summary>
	private static void BurnShadeScript(ref ScriptArgs a)
	{
		ref int curr = ref a.IntRef(0);
		byte d = unchecked((byte)curr);
		byte s = unchecked((byte)curr);
		ShaderDrawHelper.BurnShade(ref d, s, a.Int(1), a.Int(2));
		curr = d;
	}

	////////////////////////////////////////////////////////////
	//					unit script
	////////////////////////////////////////////////////////////

	public sealed class RecolorUnitParser : ScriptParserEvents
	{
		public RecolorUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			Output("new_pixel", "old_pixel"),
			[A("unit", Ptr<BattleUnit>()), A("battle_game", Ptr<SavedBattleGame>()), A("blit_part", Int), A("anim_frame", Int), A("shade", Int), A("burn", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomFunc("add_burn_shade", [IntRef, Int, Int], BurnShadeScript);

			CommonImpl(b, mod);
			CommonBattleUnitAnimations(this);

			b.AddCustomConst("blit_item_righthand", (int)UnitBodyPartEx.BODYPART_ITEM_RIGHTHAND);
			b.AddCustomConst("blit_item_lefthand", (int)UnitBodyPartEx.BODYPART_ITEM_LEFTHAND);
			b.AddCustomConst("blit_item_floor", (int)UnitBodyPartEx.BODYPART_ITEM_FLOOR);
			b.AddCustomConst("blit_item_big", (int)UnitBodyPartEx.BODYPART_ITEM_INVENTORY);

			SetDefault("unit.getRecolor new_pixel; add_burn_shade new_pixel burn shade; return new_pixel;");
		}
	}

	public sealed class SelectUnitParser : ScriptParserEvents
	{
		public SelectUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			Output("sprite_index", "sprite_offset"),
			[A("unit", Ptr<BattleUnit>()), A("battle_game", Ptr<SavedBattleGame>()), A("blit_part", Int), A("anim_frame", Int), A("shade", Int)])
		{
			var b = new BindBase(this);

			CommonImpl(b, mod);
			CommonBattleUnitAnimations(this);

			SetDefault("add sprite_index sprite_offset; return sprite_index;");
		}
	}

	public sealed class SelectMoveSoundUnitParser : ScriptParserEvents
	{
		public SelectMoveSoundUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("sound_index", IntRef)],
			[A("unit", Ptr<BattleUnit>()), A("walking_phase", Int), A("unit_sound_index", Int), A("tile_sound_index", Int),
			A("base_tile_sound_index", Int), A("base_tile_sound_offset", Int), A("base_fly_sound_index", Int),
			A("move", Int)])
		{
			var b = new BindBase(this);

			CommonImpl(b, mod);
			CommonBattleUnitAnimations(this);

			MoveTypesImpl(b);
		}
	}

	public sealed class ReactionUnitParser : ScriptParserEvents
	{
		public ReactionUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			Output("reaction_chance", "distance"),
			[A("action_unit", Ptr<BattleUnit>()),
			A("reaction_unit", Ptr<BattleUnit>()), A("reaction_weapon", Ptr<BattleItem>()), A("reaction_battle_action", Int), A("reaction_count", Int),
			A("weapon", Ptr<BattleItem>()), A("skill", Ptr<RuleSkill>()), A("battle_action", Int), A("action_target", Ptr<BattleUnit>()),
			A("move", Int), A("arc_to_action_unit", Int), A("battle_game", Ptr<SavedBattleGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);

			MoveTypesImpl(b);
		}
	}

	public sealed class VisibilityUnitParser : ScriptParserEvents
	{
		public VisibilityUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("current_visibility", IntRef), A("default_visibility", Int), A("visibility_mode", TagRef<BattleUnitVisibility>())],
			[A("observer_unit", Ptr<BattleUnit>()), A("target_unit", Ptr<BattleUnit>()), A("target_tile", Ptr<Tile>()),
			A("distance", Int), A("distance_max", Int), A("distance_target_max", Int),
			A("smoke_density", Int), A("fire_density", Int),
			A("smoke_density_near_observer", Int), A("fire_density_near_observer", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	public sealed class AiCalculateTargetWeightParser : ScriptParserEvents
	{
		public AiCalculateTargetWeightParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			Output("current_target_weight", "default_target_weight"),
			[A("ai_unit", Ptr<BattleUnit>()), A("target_unit", Ptr<BattleUnit>()), A("battle_game", Ptr<SavedBattleGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	public sealed class HitUnitParser : ScriptParserEvents
	{
		public HitUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("power", IntRef), A("part", IntRef), A("side", IntRef)],
			[A("unit", PtrE<BattleUnit>()), A("damaging_item", PtrE<BattleItem>()), A("weapon_item", PtrE<BattleItem>()), A("attacker", PtrE<BattleUnit>()),
			A("battle_game", PtrE<SavedBattleGame>()), A("skill", Ptr<RuleSkill>()), A("orig_power", Int), A("damaging_type", Int), A("battle_action", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);
		}
	}

	public sealed class SkillUseUnitParser : ScriptParserEvents
	{
		public SkillUseUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("continue_action", IntRef), A("spend_tu", IntRef)],
			[A("actor", PtrE<BattleUnit>()), A("item", PtrE<BattleItem>()), A("battle_game", PtrE<SavedBattleGame>()), A("skill", Ptr<RuleSkill>()), A("battle_action", Int), A("have_tu", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);

			SetEmptyReturn();
		}
	}

	public sealed class TryPsiAttackUnitParser : ScriptParserEvents
	{
		public TryPsiAttackUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("psi_attack_success", IntRef)],
			[A("item", Ptr<BattleItem>()), A("attacker", Ptr<BattleUnit>()), A("victim", Ptr<BattleUnit>()), A("skill", Ptr<RuleSkill>()),
			A("attack_strength", Int), A("defense_strength", Int), A("battle_action", Int), A("battle_game", Ptr<SavedBattleGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);
		}
	}

	public sealed class TryMeleeAttackUnitParser : ScriptParserEvents
	{
		public TryMeleeAttackUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("melee_attack_success", IntRef)],
			[A("item", Ptr<BattleItem>()), A("attacker", Ptr<BattleUnit>()), A("victim", Ptr<BattleUnit>()), A("skill", Ptr<RuleSkill>()),
			A("attack_strength", Int), A("defense_strength", Int), A("battle_action", Int), A("battle_game", Ptr<SavedBattleGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);
		}
	}

	public sealed class DamageUnitParser : ScriptParserEvents
	{
		public DamageUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("to_health", IntRef), A("to_armor", IntRef), A("to_stun", IntRef), A("to_time", IntRef), A("to_energy", IntRef),
			A("to_morale", IntRef), A("to_wound", IntRef), A("to_transform", IntRef), A("to_mana", IntRef)],
			[A("unit", PtrE<BattleUnit>()), A("damaging_item", PtrE<BattleItem>()), A("weapon_item", PtrE<BattleItem>()), A("attacker", PtrE<BattleUnit>()),
			A("battle_game", PtrE<SavedBattleGame>()), A("skill", Ptr<RuleSkill>()), A("currPower", Int), A("orig_power", Int), A("part", Int), A("side", Int), A("damaging_type", Int), A("battle_action", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);

			SetEmptyReturn();
		}
	}

	public sealed class DamageSpecialUnitParser : ScriptParserEvents
	{
		public DamageSpecialUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("transform", Int), A("transform_chance", IntRef), A("self_destruct", Int), A("self_destruct_chance", IntRef),
			A("morale_loss", IntRef), A("fire", IntRef), A("attacker_turns_since_spotted", IntRef), A("attacker_turns_left_spotted_for_snipers", IntRef)],
			[A("unit", PtrE<BattleUnit>()), A("damaging_item", PtrE<BattleItem>()), A("weapon_item", PtrE<BattleItem>()), A("attacker", PtrE<BattleUnit>()),
			A("battle_game", PtrE<SavedBattleGame>()), A("skill", Ptr<RuleSkill>()), A("health_damage", Int), A("orig_power", Int), A("part", Int), A("side", Int), A("damaging_type", Int), A("battle_action", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);

			SetEmptyReturn();
		}
	}

	public sealed class HealUnitParser : ScriptParserEvents
	{
		public HealUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("medikit_action_type", IntRef), A("body_part", IntRef), A("wound_recovery", IntRef), A("health_recovery", IntRef), A("energy_recovery", IntRef),
			A("stun_recovery", IntRef), A("mana_recovery", IntRef), A("morale_recovery", IntRef), A("painkiller_recovery", IntRef)],
			[A("actor", PtrE<BattleUnit>()), A("item", PtrE<BattleItem>()), A("battle_game", PtrE<SavedBattleGame>()), A("target", PtrE<BattleUnit>()), A("battle_action", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);

			MedikitBattleActionImpl(b);

			SetEmptyReturn();
		}
	}

	public sealed class CreateUnitParser : ScriptParserEvents
	{
		public CreateUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[],
			[A("unit", PtrE<BattleUnit>()), A("battle_game", PtrE<SavedBattleGame>()), A("turn", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);
		}
	}

	public sealed class NewTurnUnitParser : ScriptParserEvents
	{
		public NewTurnUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[],
			[A("unit", PtrE<BattleUnit>()), A("battle_game", PtrE<SavedBattleGame>()), A("turn", Int), A("side", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	public sealed class ReturnFromMissionUnitParser : ScriptParserEvents
	{
		public ReturnFromMissionUnitParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("recovery_time", IntRef), A("mana_loss", Int), A("health_loss", Int), A("final_mana_loss", IntRef), A("final_health_loss", IntRef)],
			[A("unit", PtrE<BattleUnit>()), A("battle_game", PtrE<SavedBattleGame>()), A("soldier", PtrE<Soldier>()), A("statChange", Ptr<StatAdjustment>()), A("statPrevious", Ptr<StatAdjustment>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			SetEmptyReturn();
		}
	}

	public sealed class AwardExperienceParser : ScriptParserEvents
	{
		public AwardExperienceParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			Output("experience_multipler", "experience_type"),
			[A("attacker", Ptr<BattleUnit>()), A("unit", Ptr<BattleUnit>()), A("weapon", Ptr<BattleItem>()), A("battle_action", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			BattleActionImpl(b);
		}
	}

	public sealed class StatsForNerdsArmorParser : ScriptParserEvents
	{
		public StatsForNerdsArmorParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[],
			[A("rule", Ptr<Armor>()), A("stats_state", PtrE<StatsForNerdsState>()), A("geoscape_game", Ptr<SavedGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	////////////////////////////////////////////////////////////
	//					item script
	////////////////////////////////////////////////////////////

	public sealed class CreateItemParser : ScriptParserEvents
	{
		public CreateItemParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[],
			[A("item", PtrE<BattleItem>()), A("unit", PtrE<BattleUnit>()), A("battle_game", PtrE<SavedBattleGame>()), A("turn", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	public sealed class NewTurnItemParser : ScriptParserEvents
	{
		public NewTurnItemParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[],
			[A("item", PtrE<BattleItem>()), A("battle_game", PtrE<SavedBattleGame>()), A("turn", Int), A("side", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	public sealed class RecolorItemParser : ScriptParserEvents
	{
		public RecolorItemParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			Output("new_pixel", "old_pixel"),
			[A("item", Ptr<BattleItem>()), A("battle_game", Ptr<SavedBattleGame>()), A("blit_part", Int), A("anim_frame", Int), A("shade", Int)])
		{
			var b = new BindBase(this);

			CommonItemImpl(b, mod);
			CommonBattleItemAnimations(this);

			SetDefault("add_shade new_pixel shade; return new_pixel;");
		}
	}

	public sealed class SelectItemParser : ScriptParserEvents
	{
		public SelectItemParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			Output("sprite_index", "sprite_offset"),
			[A("item", Ptr<BattleItem>()), A("battle_game", Ptr<SavedBattleGame>()), A("blit_part", Int), A("anim_frame", Int), A("shade", Int)])
		{
			var b = new BindBase(this);

			CommonItemImpl(b, mod);
			CommonBattleItemAnimations(this);

			SetDefault("add sprite_index sprite_offset; return sprite_index;");
		}
	}

	public class VaporParticleBaseParser : ScriptParser
	{
		public VaporParticleBaseParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("vapor_color", IntRef), A("subvoxel_offset", ValueRef<Position>()), A("subvoxel_velocity", ValueRef<Position>()), A("subvoxel_acceleration", ValueRef<Position>()),
			A("subvoxel_drift", IntRef), A("particle_density", IntRef), A("particle_lifetime", IntRef), A("particle_number", Int)],
			[A("weapon", Ptr<BattleItem>()), A("ammo", Ptr<BattleItem>()), A("particle_number_max", Int), A("subvoxel_trajectory_distance", Int), A("subvoxel_trajectory_distance_max", Int),
			A("subvoxel_trajectory_forward", Value<Position>()), A("subvoxel_trajectory_right", Value<Position>()), A("subvoxel_trajectory_up", Value<Position>()), A("random", PtrE<Rng.RandomState>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
			b.AddCustomConst("subvoxel_scale", Particle.SubVoxelAccuracy);

			SetEmptyReturn();
			SetDescription("alter default behavior of vapor particle");
		}
	}

	public sealed class VaporParticleAmmoParser : VaporParticleBaseParser
	{
		public VaporParticleAmmoParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name, mod)
		{
			SetDescription("alter default behavior of vapor particle from ammo");
			SetDefault(
				"var int temp;\n" +
				"var int randMax;\n" +

				"set randMax subvoxel_scale;\n" +
				"muldiv randMax 3 2;\n" +


				"random.randomRangeSymmetric temp randMax;\n" +
				"subvoxel_offset.setX temp;\n" +
				"random.randomRangeSymmetric temp randMax;\n" +
				"subvoxel_offset.setY temp;\n" +
				"random.randomRangeSymmetric temp randMax;\n" +
				"subvoxel_offset.setZ temp;\n" +

				"set temp 320;\n" +
				"sub temp particle_density;\n" +
				"subvoxel_velocity.setZ temp;\n" +

				"return;"
			);
		}
	}

	public sealed class VaporParticleWeaponParser : VaporParticleBaseParser
	{
		public VaporParticleWeaponParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name, mod)
		{
			SetDescription("alter default behavior of vapor particle from firing weapon");
		}
	}

	public sealed class TryPsiAttackItemParser : ScriptParserEvents
	{
		public TryPsiAttackItemParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("psi_attack_success", IntRef)],
			[A("item", Ptr<BattleItem>()), A("attacker", Ptr<BattleUnit>()), A("victim", Ptr<BattleUnit>()), A("skill", Ptr<RuleSkill>()),
			A("attack_strength", Int), A("defense_strength", Int), A("battle_action", Int), A("random", PtrE<Rng.RandomState>()),
			A("distance", Int), A("distance_strength_reduction", Int), A("battle_game", Ptr<SavedBattleGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			SetDefault("var int r; random.randomRange r 0 55; add psi_attack_success attack_strength; add psi_attack_success r; sub psi_attack_success defense_strength; sub psi_attack_success distance_strength_reduction; return psi_attack_success;");
		}
	}

	public sealed class TryMeleeAttackItemParser : ScriptParserEvents
	{
		public TryMeleeAttackItemParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("melee_attack_success", IntRef)],
			[A("item", Ptr<BattleItem>()), A("attacker", Ptr<BattleUnit>()), A("victim", Ptr<BattleUnit>()), A("skill", Ptr<RuleSkill>()),
			A("attack_strength", Int), A("defense_strength", Int), A("battle_action", Int), A("random", PtrE<Rng.RandomState>()),
			A("arc_to_attacker", Int), A("defense_strength_penalty", Int), A("battle_game", Ptr<SavedBattleGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);

			SetDefault(
				"var int r;\n" +
				"random.randomRange r 0 99;\n" +
				"sub melee_attack_success r;\n" +
				"add melee_attack_success attack_strength;\n" +
				"sub melee_attack_success defense_strength;\n" +
				"add melee_attack_success defense_strength_penalty;\n" +
				"return melee_attack_success;\n"
			);
		}
	}

	public sealed class SellCostItemParser : ScriptParserEvents
	{
		public SellCostItemParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			Output("cost_current", "cost_base"),
			[A("item_rule", Ptr<RuleItem>()), A("geoscape_game", Ptr<SavedGame>()), A("difficulty_coefficient", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	public sealed class BuyCostItemParser : ScriptParserEvents
	{
		public BuyCostItemParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			Output("cost_current", "cost_base"),
			[A("item_rule", Ptr<RuleItem>()), A("geoscape_game", Ptr<SavedGame>()), A("difficulty_coefficient", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	public sealed class StatsForNerdsItemParser : ScriptParserEvents
	{
		public StatsForNerdsItemParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[],
			[A("rule", Ptr<RuleItem>()), A("stats_state", PtrE<StatsForNerdsState>()), A("geoscape_game", Ptr<SavedGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	////////////////////////////////////////////////////////////
	//					bonus stat script
	////////////////////////////////////////////////////////////

	public class BonusStatsBaseParser : ScriptParserEvents
	{
		/// <summary>Name of YAML node where config is stored.</summary>
		public string PropertyNodeName { get; }

		public BonusStatsBaseParser(ScriptGlobal shared, string name, string propertyNodeName, Mod mod) : base(shared, name,
			[A("bonus", IntRef)],
			[A("unit", Ptr<BattleUnit>()), A("external_bonuses", Int), A("weapon", Ptr<BattleItem>()), A("ammo", Ptr<BattleItem>()), A("battle_action", Int), A("skill", Ptr<RuleSkill>())])
		{
			PropertyNodeName = propertyNodeName;
			var bu = new Bind<BattleUnit>(this);

			RuleStatBonus.ScriptRegisterStats(bu);
		}
	}

	public sealed class BonusStatsParser(ScriptGlobal shared, string name, Mod mod) : BonusStatsBaseParser(shared, name + "BonusStats", name, mod);

	public sealed class BonusStatsRecoveryParser(ScriptGlobal shared, string name, Mod mod) : BonusStatsBaseParser(shared, name + "RecoveryBonusStats", name, mod);

	public sealed class BonusSoldierStatsRecoveryParser(ScriptGlobal shared, string name, Mod mod) : BonusStatsBaseParser(shared, name + "SoldierRecoveryBonusStats", name, mod);

	////////////////////////////////////////////////////////////
	//					country script
	////////////////////////////////////////////////////////////

	public sealed class NewMonthCountryParser : ScriptParserEvents
	{
		public NewMonthCountryParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("fundingChange", IntRef), A("satisfaction", IntRef), A("formPact", IntRef), A("cancelPact", IntRef)],
			[A("country", Ptr<Country>()), A("geoscape_game", Ptr<SavedGame>()), A("totalXcomScore", Int), A("totalAlienScore", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	////////////////////////////////////////////////////////////
	//					ufo script
	////////////////////////////////////////////////////////////

	public sealed class DetectUfoFromBaseParser : ScriptParserEvents
	{
		public DetectUfoFromBaseParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("detection_type", IntRef), A("detection_chance", IntRef)],
			[A("ufo", Ptr<Ufo>()), A("geoscape_game", Ptr<SavedGame>()),
			A("distance", Int), A("already_tracked", Int), A("radar_total_strength", Int), A("radar_max_distance", Int), A("hyperwave_total_strength", Int), A("hyperwave_max_distance", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	public sealed class DetectUfoFromCraftParser : ScriptParserEvents
	{
		public DetectUfoFromCraftParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[A("detection_type", IntRef), A("detection_chance", IntRef)],
			[A("ufo", Ptr<Ufo>()), A("geoscape_game", Ptr<SavedGame>()), A("craft", Ptr<Craft>()),
			A("distance", Int), A("already_tracked", Int), A("radar_total_strength", Int), A("radar_max_distance", Int)])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	public sealed class StatsForNerdsUfoParser : ScriptParserEvents
	{
		public StatsForNerdsUfoParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[],
			[A("rule", Ptr<RuleUfo>()), A("stats_state", PtrE<StatsForNerdsState>()), A("geoscape_game", Ptr<SavedGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	////////////////////////////////////////////////////////////
	//					craft script
	////////////////////////////////////////////////////////////

	public sealed class StatsForNerdsCraftParser : ScriptParserEvents
	{
		public StatsForNerdsCraftParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[],
			[A("rule", Ptr<RuleCraft>()), A("stats_state", PtrE<StatsForNerdsState>()), A("geoscape_game", Ptr<SavedGame>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	////////////////////////////////////////////////////////////
	//				soldier bonus stat script
	////////////////////////////////////////////////////////////

	public sealed class ApplySoldierBonusesParser : ScriptParserEvents
	{
		public ApplySoldierBonusesParser(ScriptGlobal shared, string name, Mod mod) : base(shared, name,
			[],
			[A("unit", PtrE<BattleUnit>()), A("save_game", PtrE<SavedBattleGame>()), A("soldier_bonus", Ptr<RuleSoldierBonus>())])
		{
			var b = new BindBase(this);

			b.AddCustomPtr<Mod>("rules", mod);
		}
	}

	////////////////////////////////////////////////////////////
	//					named scripts
	////////////////////////////////////////////////////////////

	// unit script (index in BattleUnitScripts)
	public const int RecolorUnitSprite = 0;
	public const int SelectUnitSprite = 1;
	public const int SelectMoveSoundUnit = 2;
	public const int ReactionUnitAction = 3;
	public const int ReactionUnitReaction = 4;
	public const int TryPsiAttackUnit = 5;
	public const int TryMeleeAttackUnit = 6;
	public const int HitUnit = 7;
	public const int DamageUnit = 8;
	public const int DamageSpecialUnit = 9;
	public const int HealUnit = 10;
	public const int CreateUnit = 11;
	public const int NewTurnUnit = 12;
	public const int ReturnFromMissionUnit = 13;
	public const int AwardExperience = 14;
	public const int VisibilityUnit = 15;
	public const int AiCalculateTargetWeight = 16;
	public const int StatsForNerdsArmor = 17;

	// item script (index in BattleItemScripts)
	public const int RecolorItemSprite = 0;
	public const int SelectItemSprite = 1;
	public const int VaporParticleAmmo = 2;
	public const int VaporParticleWeapon = 3;
	public const int ReactionWeaponAction = 4;
	public const int TryPsiAttackItem = 5;
	public const int TryMeleeAttackItem = 6;
	public const int HitUnitAmmo = 7;
	public const int DamageUnitAmmo = 8;
	public const int DamageSpecialUnitAmmo = 9;
	public const int CreateItem = 10;
	public const int NewTurnItem = 11;
	public const int SellCostItem = 12;
	public const int BuyCostItem = 13;
	public const int StatsForNerdsItem = 14;

	// bonus stat script (index in BonusStatsScripts)
	public const int PsiDefenceStatBonus = 0;
	public const int MeleeDodgeStatBonus = 1;
	public const int TimeRecoveryStatBonus = 2;
	public const int EnergyRecoveryStatBonus = 3;
	public const int MoraleRecoveryStatBonus = 4;
	public const int HealthRecoveryStatBonus = 5;
	public const int ManaRecoveryStatBonus = 6;
	public const int StunRecoveryStatBonus = 7;
	public const int TimeSoldierRecoveryStatBonus = 8;
	public const int EnergySoldierRecoveryStatBonus = 9;
	public const int MoraleSoldierRecoveryStatBonus = 10;
	public const int HealthSoldierRecoveryStatBonus = 11;
	public const int ManaSoldierRecoveryStatBonus = 12;
	public const int StunSoldierRecoveryStatBonus = 13;
	public const int DamageBonusStatBonus = 14;
	public const int MeleeBonusStatBonus = 15;
	public const int AccuracyMultiplierStatBonus = 16;
	public const int MeleeMultiplierStatBonus = 17;
	public const int ThrowMultiplierStatBonus = 18;
	public const int CloseQuarterMultiplierStatBonus = 19;

	// skill script
	public const int SkillUseUnit = 0;

	// country script
	public const int NewMonthCountry = 0;

	// ufo script
	public const int DetectUfoFromBase = 0;
	public const int DetectUfoFromCraft = 1;
	public const int StatsForNerdsUfo = 2;

	// craft script
	public const int StatsForNerdsCraft = 0;

	// soldier bonus stat script
	public const int ApplySoldierBonuses = 0;

	////////////////////////////////////////////////////////////
	//					groups
	////////////////////////////////////////////////////////////

	public sealed class BattleUnitScripts : ScriptGroup
	{
		public BattleUnitScripts(ScriptGlobal shared, Mod mod, string groupName)
		{
			Init(shared, groupName,
				new RecolorUnitParser(shared, "recolorUnitSprite", mod),
				new SelectUnitParser(shared, "selectUnitSprite", mod),
				new SelectMoveSoundUnitParser(shared, "selectMoveSoundUnit", mod),

				new ReactionUnitParser(shared, "reactionUnitAction", mod),
				new ReactionUnitParser(shared, "reactionUnitReaction", mod),

				new TryPsiAttackUnitParser(shared, "tryPsiAttackUnit", mod),
				new TryMeleeAttackUnitParser(shared, "tryMeleeAttackUnit", mod),
				new HitUnitParser(shared, "hitUnit", mod),
				new DamageUnitParser(shared, "damageUnit", mod),
				new DamageSpecialUnitParser(shared, "damageSpecialUnit", mod),
				new HealUnitParser(shared, "healUnit", mod),

				new CreateUnitParser(shared, "createUnit", mod),
				new NewTurnUnitParser(shared, "newTurnUnit", mod),
				new ReturnFromMissionUnitParser(shared, "returnFromMissionUnit", mod),

				new AwardExperienceParser(shared, "awardExperience", mod),

				new VisibilityUnitParser(shared, "visibilityUnit", mod),

				new AiCalculateTargetWeightParser(shared, "aiCalculateTargetWeight", mod),

				new StatsForNerdsArmorParser(shared, "statsForNerdsArmor", mod)
			);
		}
	}

	public sealed class BattleItemScripts : ScriptGroup
	{
		public BattleItemScripts(ScriptGlobal shared, Mod mod, string groupName)
		{
			Init(shared, groupName,
				new RecolorItemParser(shared, "recolorItemSprite", mod),
				new SelectItemParser(shared, "selectItemSprite", mod),
				new VaporParticleAmmoParser(shared, "vaporParticleAmmo", mod),
				new VaporParticleWeaponParser(shared, "vaporParticleWeapon", mod),

				new ReactionUnitParser(shared, "reactionWeaponAction", mod),

				new TryPsiAttackItemParser(shared, "tryPsiAttackItem", mod),
				new TryMeleeAttackItemParser(shared, "tryMeleeAttackItem", mod),
				new HitUnitParser(shared, "hitUnitAmmo", mod),
				new DamageUnitParser(shared, "damageUnitAmmo", mod),
				new DamageSpecialUnitParser(shared, "damageSpecialUnitAmmo", mod),

				new CreateItemParser(shared, "createItem", mod),
				new NewTurnItemParser(shared, "newTurnItem", mod),

				new SellCostItemParser(shared, "sellCostItem", mod),
				new BuyCostItemParser(shared, "buyCostItem", mod),

				new StatsForNerdsItemParser(shared, "statsForNerdsItem", mod)
			);
		}
	}

	public sealed class BonusStatsScripts : ScriptGroup
	{
		public BonusStatsScripts(ScriptGlobal shared, Mod mod, string groupName)
		{
			Init(shared, groupName,
				new BonusStatsParser(shared, "psiDefence", mod),
				new BonusStatsParser(shared, "meleeDodge", mod),

				new BonusStatsRecoveryParser(shared, "time", mod),
				new BonusStatsRecoveryParser(shared, "energy", mod),
				new BonusStatsRecoveryParser(shared, "morale", mod),
				new BonusStatsRecoveryParser(shared, "health", mod),
				new BonusStatsRecoveryParser(shared, "mana", mod),
				new BonusStatsRecoveryParser(shared, "stun", mod),

				new BonusSoldierStatsRecoveryParser(shared, "time", mod),
				new BonusSoldierStatsRecoveryParser(shared, "energy", mod),
				new BonusSoldierStatsRecoveryParser(shared, "morale", mod),
				new BonusSoldierStatsRecoveryParser(shared, "health", mod),
				new BonusSoldierStatsRecoveryParser(shared, "mana", mod),
				new BonusSoldierStatsRecoveryParser(shared, "stun", mod),

				new BonusStatsParser(shared, "damageBonus", mod),
				new BonusStatsParser(shared, "meleeBonus", mod),
				new BonusStatsParser(shared, "accuracyMultiplier", mod),
				new BonusStatsParser(shared, "meleeMultiplier", mod),
				new BonusStatsParser(shared, "throwMultiplier", mod),
				new BonusStatsParser(shared, "closeQuartersMultiplier", mod)
			);
		}

		/// <summary>Get parser by index (C++ <c>parsers.bonusStatsScripts.get&lt;ModScript::XStatBonus&gt;()</c>).</summary>
		public BonusStatsBaseParser Get(int i) => (BonusStatsBaseParser)Parsers[i];
	}

	public sealed class SkillScripts : ScriptGroup
	{
		public SkillScripts(ScriptGlobal shared, Mod mod, string groupName)
		{
			Init(shared, groupName, new SkillUseUnitParser(shared, "skillUseUnit", mod));
		}
	}

	public sealed class CountryScripts : ScriptGroup
	{
		public CountryScripts(ScriptGlobal shared, Mod mod, string groupName)
		{
			Init(shared, groupName, new NewMonthCountryParser(shared, "newMonthCountry", mod));
		}
	}

	public sealed class UfoScripts : ScriptGroup
	{
		public UfoScripts(ScriptGlobal shared, Mod mod, string groupName)
		{
			Init(shared, groupName,
				new DetectUfoFromBaseParser(shared, "detectUfoFromBase", mod),
				new DetectUfoFromCraftParser(shared, "detectUfoFromCraft", mod),

				new StatsForNerdsUfoParser(shared, "statsForNerdsUfo", mod)
			);
		}
	}

	public sealed class CraftScripts : ScriptGroup
	{
		public CraftScripts(ScriptGlobal shared, Mod mod, string groupName)
		{
			Init(shared, groupName, new StatsForNerdsCraftParser(shared, "statsForNerdsCraft", mod));
		}
	}

	public sealed class SoldierBonusScripts : ScriptGroup
	{
		public SoldierBonusScripts(ScriptGlobal shared, Mod mod, string groupName)
		{
			Init(shared, groupName, new ApplySoldierBonusesParser(shared, "applySoldierBonuses", mod));
		}
	}

	////////////////////////////////////////////////////////////
	//					members
	////////////////////////////////////////////////////////////

	public readonly BattleUnitScripts battleUnitScripts;
	public readonly BattleItemScripts battleItemScripts;
	public readonly BonusStatsScripts bonusStatsScripts;
	public readonly SkillScripts skillScripts;
	public readonly CountryScripts countryScripts;
	public readonly UfoScripts ufoScripts;
	public readonly CraftScripts craftScripts;
	public readonly SoldierBonusScripts soldierBonusScripts;

	internal ModScript(ScriptGlobal shared, Mod mod)
	{
		_shared = shared;
		_mod = mod;
		battleUnitScripts = new BattleUnitScripts(_shared, _mod, "unit");
		battleItemScripts = new BattleItemScripts(_shared, _mod, "item");
		bonusStatsScripts = new BonusStatsScripts(_shared, _mod, "bonuses");
		skillScripts = new SkillScripts(_shared, _mod, "skill");
		countryScripts = new CountryScripts(_shared, _mod, "country");
		ufoScripts = new UfoScripts(_shared, _mod, "ufo");
		craftScripts = new CraftScripts(_shared, _mod, "craft");
		soldierBonusScripts = new SoldierBonusScripts(_shared, _mod, "soldier");
	}

	/// <summary>Get shared state.</summary>
	public ScriptGlobal GetShared() => _shared;

	////////////////////////////////////////////////////////////
	//					helper functions
	////////////////////////////////////////////////////////////

	/// <summary>Run script with events, arguments stay unchanged if the container was never loaded.</summary>
	public static void Execute(ScriptContainerEventsBase c, ScriptOutputArgs output, params object[] args)
	{
		var parser = c?.Parser;
		if (parser == null)
		{
			return;
		}
		new ScriptWorker(parser.Signature, args).Execute(c, output);
	}

	/// <summary>Run script without events.</summary>
	public static void Execute(ScriptContainerBase c, ScriptOutputArgs output, params object[] args)
	{
		var parser = c?._parser;
		if (parser == null)
		{
			return;
		}
		new ScriptWorker(parser.Signature, args).Execute(c, output);
	}

	/// <summary>Script helper that call script that do not return any values.</summary>
	public static void ScriptCallback(ScriptContainerEventsBase c, params object[] args)
	{
		Execute(c, new ScriptOutputArgs(), args);
	}

	/// <summary>Script helper that call script that return one value and take one parameter.</summary>
	public static int ScriptFunc1(ScriptContainerEventsBase c, int first, params object[] args)
	{
		var arg = new ScriptOutputArgs(first);
		Execute(c, arg, args);
		return arg.GetFirst();
	}

	/// <summary>Script helper that call script that return one value and take two parameters.</summary>
	public static int ScriptFunc2(ScriptContainerEventsBase c, int first, int second, params object[] args)
	{
		var arg = new ScriptOutputArgs(first, second);
		Execute(c, arg, args);
		return arg.GetFirst();
	}

	////////////////////////////////////////////////////////////
	//					bindable types
	////////////////////////////////////////////////////////////

	/// <summary>All types with C++ <c>ScriptName</c> and <c>ScriptRegister</c>.</summary>
	internal static void RegisterBindTypes()
	{
		ScriptBindTable.Add<Mod>("RuleMod", Mod.ScriptRegister);
		ScriptBindTable.Add<Unit>("RuleUnit", Unit.ScriptRegister);
		ScriptBindTable.Add<StatAdjustment>("StatAdjustment", Unit.ScriptRegisterStatAdjustment);
		ScriptBindTable.Add<RuleItem>("RuleItem", RuleItem.ScriptRegister);
		ScriptBindTable.Add<Armor>("RuleArmor", Armor.ScriptRegister);
		ScriptBindTable.Add<RuleSkill>("RuleSkill", RuleSkill.ScriptRegister);
		ScriptBindTable.Add<RuleResearch>("RuleResearch", RuleResearch.ScriptRegister);
		ScriptBindTable.Add<RuleSoldier>("RuleSoldier", RuleSoldier.ScriptRegister);
		ScriptBindTable.Add<RuleInventory>("RuleInventory", RuleInventory.ScriptRegister);
		ScriptBindTable.Add<RuleCountry>("RuleCountry", RuleCountry.ScriptRegister);
		ScriptBindTable.Add<RuleCraft>("RuleCraft", RuleCraft.ScriptRegister);
		ScriptBindTable.Add<RuleUfo>("RuleUfo", RuleUfo.ScriptRegister);
		ScriptBindTable.Add<RuleSoldierBonus>("RuleSoldierBonus", RuleSoldierBonus.ScriptRegister);
		ScriptBindTable.Add<SavedGame>("GeoscapeGame", SavedGame.ScriptRegister);
		ScriptBindTable.Add<SavedBattleGame>("BattleGame", SavedBattleGame.ScriptRegister);
		ScriptBindTable.Add<BattleUnit>("BattleUnit", BattleUnit.ScriptRegister);
		ScriptBindTable.Add<BattleUnitVisibility>("BattleUnitVisibility", BattleUnit.ScriptRegisterVisibility);
		ScriptBindTable.Add<BattleItem>("BattleItem", BattleItem.ScriptRegister);
		ScriptBindTable.Add<Soldier>("GeoscapeSoldier", Soldier.ScriptRegister);
		ScriptBindTable.Add<Country>("Country", Country.ScriptRegister);
		ScriptBindTable.Add<Craft>("Craft", Craft.ScriptRegister);
		ScriptBindTable.Add<Ufo>("Ufo", Ufo.ScriptRegister);
		ScriptBindTable.Add<Tile>("Tile", Tile.ScriptRegister);
		ScriptBindTable.Add<StatsForNerdsState>("StatsForNerds", StatsForNerdsState.ScriptRegister);
	}
}
