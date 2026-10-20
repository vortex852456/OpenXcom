namespace Acom.Tests.Savegame;

[Collection(nameof(Acom.Tests.Engine.OptionsTests))] // Language reads Options.language / Options.wordwrap
public class HitLogTests
{
	[Fact]
	public void Entries_build_the_text_and_the_turn_diary()
	{
		Options.language = "en-US";
		Options.wordwrap = TextWrapping.WRAP_AUTO;
		var log = new HitLog(new Language()); // unknown ids are returned as they are

		log.AppendToHitLog(HitLogEntryType.HITLOG_NEW_TURN, UnitFaction.FACTION_PLAYER);
		Assert.Equal("STR_HIT_LOG_NEW_TURN", log.GetHitLogText());

		log.AppendToHitLog(HitLogEntryType.HITLOG_PLAYER_FIRING, UnitFaction.FACTION_PLAYER, "Rifle");
		log.AppendToHitLog(HitLogEntryType.HITLOG_NEW_SHOT, UnitFaction.FACTION_PLAYER);
		log.AppendToHitLog(HitLogEntryType.HITLOG_SMALL_DAMAGE, UnitFaction.FACTION_PLAYER);
		Assert.Equal("Rifle\n\nSTR_HIT_LOG_NEW_BULLETSTR_HIT_LOG_SMALL_DAMAGE", log.GetHitLogText());

		// reaction fire starts a new entry, but only once
		log.AppendToHitLog(HitLogEntryType.HITLOG_REACTION_FIRE, UnitFaction.FACTION_HOSTILE);
		log.AppendToHitLog(HitLogEntryType.HITLOG_REACTION_FIRE, UnitFaction.FACTION_HOSTILE);
		log.AppendToHitLog(HitLogEntryType.HITLOG_NEW_SHOT, UnitFaction.FACTION_HOSTILE);
		Assert.Equal("STR_HIT_LOG_REACTION_FIRE\n\nSTR_HIT_LOG_NEW_BULLET", log.GetHitLogText());

		// the player continues shooting: the last weapon is repeated
		log.AppendToHitLog(HitLogEntryType.HITLOG_NEW_SHOT, UnitFaction.FACTION_PLAYER);
		Assert.Equal("Rifle  STR_HIT_LOG_NEW_BULLET", log.GetHitLogText(true));
		Assert.Equal(
			["STR_HIT_LOG_NEW_TURN", "Rifle  STR_HIT_LOG_NEW_BULLETSTR_HIT_LOG_SMALL_DAMAGE", "STR_HIT_LOG_REACTION_FIRE  STR_HIT_LOG_NEW_BULLET"],
			log.TurnDiary);

		log.AppendToHitLog(HitLogEntryType.HITLOG_NEW_TURN_WITH_MESSAGE, UnitFaction.FACTION_PLAYER, "Turn 2");
		Assert.Empty(log.TurnDiary);
		Assert.Equal("Turn 2", log.GetHitLogText());
	}
}
