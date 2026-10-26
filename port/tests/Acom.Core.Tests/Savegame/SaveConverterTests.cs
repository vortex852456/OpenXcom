namespace Acom.Tests.Savegame;

[Collection("OptionsTests")]
public sealed class SaveConverterTests : IDisposable
{
	private readonly string _dir = Directory.CreateTempSubdirectory("acom-saveconv-").FullName + "/";

	public SaveConverterTests()
	{
		Options.SetFoldersForTest(_dir, _dir, _dir);
		Options.language = "en-US";
		Options.wordwrap = TextWrapping.WRAP_AUTO;
	}

	public void Dispose() => Directory.Delete(_dir, recursive: true);

	[Fact]
	public void Graph_vectors_are_rotated_after_a_year_and_cut_before()
	{
		var data = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
		SaveConverter.GraphVector(data, 3, true);
		Assert.Equal([3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2], data);

		var first = new List<long> { 10, 20, 30, 40 };
		SaveConverter.GraphVector(first, 2, false);
		Assert.Equal([10, 20], first);
		SaveConverter.GraphVector(first, 3, false);
		Assert.Equal([10, 20, 0], first);
	}

	[Fact]
	public void Save_list_reads_SAVEINFO_dat()
	{
		// SAVEINFO.DAT of slot 3: name at 0x02, year/month(0-based)/day/hour/minute at 0x1C.., tactical flag at 0x26
		var info = new byte[0x28];
		System.Text.Encoding.ASCII.GetBytes("BASE ONE").CopyTo(info, 0x02);
		BitConverter.GetBytes((ushort)1999).CopyTo(info, 0x1C);
		BitConverter.GetBytes((ushort)0).CopyTo(info, 0x1E);
		BitConverter.GetBytes((ushort)2).CopyTo(info, 0x20);
		BitConverter.GetBytes((ushort)9).CopyTo(info, 0x22);
		BitConverter.GetBytes((ushort)7).CopyTo(info, 0x24);
		info[0x26] = 1;
		Directory.CreateDirectory(Options.GetMasterUserFolder() + "GAME_3");
		File.WriteAllBytes(Options.GetMasterUserFolder() + "GAME_3/SAVEINFO.DAT", info);

		var lang = new Language();
		lang.LoadStrings(new Dictionary<string, string> { ["STR_DATE_SECOND"] = "{0}nd", ["STR_JAN"] = "January" });

		var saves = Enumerable.Range(0, SaveConverter.NUM_SAVES).Select(_ => new SaveOriginal()).ToArray();
		SaveConverter.GetList(lang, saves);
		Assert.All(saves.Where((_, i) => i != 2), s => Assert.Equal(0, s.id));
		Assert.Equal(3, saves[2].id);
		Assert.Equal("BASE ONE", saves[2].name);
		Assert.Equal("2nd  January  1999", saves[2].date);
		Assert.Equal("9:07", saves[2].time);
		Assert.True(saves[2].tactical);
	}

	[Fact]
	public void Converter_rejects_folders_without_SAVEINFO()
	{
		var mod = new Acom.Mod(createConverter: true);
		Assert.Throws<InvalidOperationException>(() => new SaveConverter(5, mod));
	}
}
