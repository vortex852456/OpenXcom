namespace Acom.Tests.Savegame;

/// <summary>Soldier logic that does not need the (not yet ported) soldier rules.</summary>
public class SoldierTests
{
	private static Soldier Blank(int health = 50, int mana = 40)
	{
		var soldier = new Soldier(null, null, 0); // id 0: no stat/name generation
		var stats = new UnitStats { health = (short)health, mana = (short)mana, firing = 60 };
		soldier.SetBothStats(ref stats);
		return soldier;
	}

	[Fact]
	public void Blank_soldier_defaults()
	{
		var soldier = Blank();
		Assert.Equal(SoldierRank.RANK_ROOKIE, soldier.Rank);
		Assert.Equal(SoldierGender.GENDER_MALE, soldier.Gender);
		Assert.Equal(SoldierLook.LOOK_BLONDE, soldier.Look);
		Assert.InRange(soldier.LookVariant, 0, RuleSoldier.LookVariantMax - 1);
		Assert.Equal(0, soldier.Id);
		Assert.NotNull(soldier.Diary);
		Assert.Null(soldier.Death);
		Assert.False(soldier.HasCallsign);
		Assert.Equal(1, sizeof(SoldierGender));
	}

	[Fact]
	public void Counters_and_stats()
	{
		var soldier = Blank();
		soldier.AddMissionCount();
		soldier.AddKillCount(3);
		soldier.AddStunCount(2);
		Assert.Equal(1, soldier.Missions);
		Assert.Equal(3, soldier.Kills);
		Assert.Equal(2, soldier.Stuns);

		soldier.CurrentStatsEditable.firing += 5;
		Assert.Equal(65, soldier.CurrentStats.firing);
		Assert.Equal(60, soldier.InitStats.firing);
		soldier.DailyDogfightExperienceCache.reactions = 2;
		Assert.Equal(2, soldier.DailyDogfightExperienceCache.reactions);
	}

	[Fact]
	public void Name_and_callsign_count_code_points()
	{
		var soldier = Blank();
		soldier.SetName("Jane Doe");
		Assert.Equal("Jane Doe", soldier.GetName());
		Assert.Equal("Jane Doe", soldier.GetName(true)); // no stat string yet

		soldier.SetCallsign("Ghost");
		Assert.True(soldier.HasCallsign);
		Assert.Equal("\"Ghost\"", soldier.GetCallsign());
		Assert.Equal("\"Gho\"", soldier.GetCallsign(3));
		soldier.SetCallsign("\U0001F600ab");
		Assert.Equal("\"\U0001F600a\"", soldier.GetCallsign(2));
	}

	[Fact]
	public void Wound_recovery()
	{
		var soldier = Blank(health: 50);
		Assert.False(soldier.IsWounded);
		soldier.SetWoundRecovery(5);
		Assert.True(soldier.IsWounded);
		Assert.False(soldier.HasFullHealth);
		Assert.Equal(5, soldier.WoundRecoveryInt);
		// 1 + 1 + 10% of 50 = 7 hp per day
		Assert.Equal(1, soldier.GetWoundRecovery(1.0f, 10.0f));
		Assert.Equal(5, soldier.GetWoundRecovery(0.0f, 0.0f));

		// 5 - 1 - 0.5 - 2% of 50
		soldier.HealWound(0.5f, 2.0f);
		Assert.Equal(3, soldier.WoundRecoveryInt);
		Assert.Equal(3, soldier.GetNeededRecoveryTime(new BaseSumDailyRecovery()));

		soldier.ReplenishStats(new BaseSumDailyRecovery { SickBayAbsoluteBonus = 10.0f });
		Assert.Equal(0, soldier.WoundRecoveryInt);
		Assert.False(soldier.IsWounded);

		soldier.SetWoundRecovery(-3);
		Assert.Equal(0, soldier.WoundRecoveryInt);
	}

	[Fact]
	public void Mana_and_health_recovery()
	{
		var soldier = Blank(health: 50, mana: 40);
		soldier.ManaMissing = -5;
		Assert.Equal(0, soldier.ManaMissing);
		Assert.Equal(0, soldier.GetManaRecovery(0));

		soldier.ManaMissing = 30;
		Assert.Equal(5, soldier.GetManaRecovery(7));
		Assert.Equal(-1, soldier.GetManaRecovery(0));
		soldier.ReplenishMana(10);
		Assert.Equal(20, soldier.ManaMissing);
		// negative recovery: capped at max(100, 2 * mana)
		soldier.ReplenishMana(-500);
		Assert.Equal(100, soldier.ManaMissing);
		soldier.ReplenishMana(1000);
		Assert.Equal(0, soldier.ManaMissing);

		soldier.HealthMissing = 12;
		Assert.Equal(3, soldier.GetHealthRecovery(5));
		soldier.ReplenishHealth(20);
		Assert.Equal(0, soldier.HealthMissing);

		// daily replenish without wounds: positive and negative mana recovery
		soldier.HealthMissing = 0;
		soldier.ReplenishStats(new BaseSumDailyRecovery { ManaRecovery = -15 });
		Assert.Equal(15, soldier.ManaMissing);
		soldier.ReplenishStats(new BaseSumDailyRecovery { ManaRecovery = 10 });
		Assert.Equal(5, soldier.ManaMissing);
	}

	[Fact]
	public void Die_clears_associations()
	{
		var soldier = Blank();
		soldier.EquipmentLayout.Add(new EquipmentLayoutItem(null, null, 0, 0, [], -1, false));
		soldier.PersonalEquipmentLayout.Add(new EquipmentLayoutItem(null, null, 0, 0, [], -1, false));
		soldier.SetPsiTraining(true);
		soldier.SetTraining(true);
		soldier.ReturnToTrainingWhenHealed = true;
		soldier.SetWoundRecovery(4);
		soldier.HealthMissing = 3;

		soldier.Die(null);
		Assert.False(soldier.IsInPsiTraining);
		Assert.False(soldier.IsInTraining);
		Assert.False(soldier.ReturnToTrainingWhenHealed);
		Assert.Equal(0, soldier.WoundRecoveryInt);
		Assert.Equal(0, soldier.HealthMissing);
		Assert.Empty(soldier.EquipmentLayout);
		Assert.Empty(soldier.PersonalEquipmentLayout);
		Assert.Null(soldier.Craft);
	}

	[Fact]
	public void Psi_training_per_day_without_training_resets_improvement()
	{
		var soldier = Blank();
		soldier.TrainPsi1Day();
		Assert.Equal(0, soldier.Improvement);
	}
}
