#pragma warning disable CA1716 // namespace mirrors the src/Mod area like the other test folders
namespace Acom.Tests.Mod;

public class ArticleDefinitionTests
{
	private static YamlNodeReader Node(string yaml) => new YamlRootNodeReader(yaml, "test").ToBase();

	/// <summary>The type_id switch of Mod::loadFile.</summary>
	private static ArticleDefinition Create(UfopaediaTypeId type) => type switch
	{
		UfopaediaTypeId.UFOPAEDIA_TYPE_UNIT => new ArticleDefinitionUnit(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_SOLDIER => new ArticleDefinitionSoldier(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT => new ArticleDefinitionCraft(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT_WEAPON => new ArticleDefinitionCraftWeapon(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_VEHICLE => new ArticleDefinitionVehicle(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_ITEM => new ArticleDefinitionItem(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_ARMOR => new ArticleDefinitionArmor(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_BASE_FACILITY => new ArticleDefinitionBaseFacility(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_TEXTIMAGE => new ArticleDefinitionTextImage(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_TEXT => new ArticleDefinitionText(),
		UfopaediaTypeId.UFOPAEDIA_TYPE_UFO => new ArticleDefinitionUfo(),
		>= UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD and <= UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_USO => new ArticleDefinitionTFTD(),
		_ => null!,
	};

	[Fact]
	public void Each_class_reports_its_type()
	{
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_UNIT, new ArticleDefinitionUnit().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_SOLDIER, new ArticleDefinitionSoldier().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT, new ArticleDefinitionCraft().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_CRAFT_WEAPON, new ArticleDefinitionCraftWeapon().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_VEHICLE, new ArticleDefinitionVehicle().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_ITEM, new ArticleDefinitionItem().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_ARMOR, new ArticleDefinitionArmor().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_BASE_FACILITY, new ArticleDefinitionBaseFacility().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_TEXTIMAGE, new ArticleDefinitionTextImage().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_TEXT, new ArticleDefinitionText().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_UFO, new ArticleDefinitionUfo().Type);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD, new ArticleDefinitionTFTD().Type);
	}

	[Fact]
	public void TFTD_article_reads_its_type_and_default_text_width()
	{
		var article = new ArticleDefinitionTFTD();
		article.Load(Node("id: STR_TEST\ntype_id: 14\nimage_id: TEST_CPAL\nweapon: STR_GUN"), 100);
		Assert.Equal(UfopaediaTypeId.UFOPAEDIA_TYPE_TFTD_ITEM, article.Type);
		Assert.Equal(157, article.text_width);
		Assert.True(article.customPalette);
		Assert.Equal("STR_GUN", article.weapon);
		Assert.Equal(100, article.ListOrder);
		Assert.Equal("STR_TEST", article.MainTitle);
	}

	[Fact]
	public void Pages_copy_the_first_page_and_check_ammo_slots()
	{
		var article = new ArticleDefinitionItem();
		const string yaml = """
			id: STR_TEST
			listOrder: 5
			text: STR_TEST_TEXT
			pages:
			  - {title: STR_PAGE_1}
			  - {ammoSlot: 1}
			  - {ammoSlot: 9, text: STR_PAGE_3}
			""";
		article.Load(Node(yaml), 100);
		Assert.Equal(5, article.ListOrder);
		Assert.Equal(3, article.NumberOfPages);
		Assert.Equal("STR_PAGE_1", article.GetTitleForPage(0));
		Assert.Equal("STR_TEST", article.GetTitleForPage(1));
		Assert.Equal("STR_TEST_TEXT", article.GetTextForPage(1));
		Assert.Equal("STR_PAGE_3", article.GetTextForPage(2));
		Assert.Equal(1, article.GetAmmoSlotForPage(1));
		Assert.Equal(0, article.GetAmmoSlotForPage(2)); // out of range: kept
		Assert.Equal(1, article.GetAmmoSlotPrevUsageForPage(2));
		Assert.Throws<InvalidOperationException>(() => article.GetTitleForPage(3));
		Assert.Throws<InvalidOperationException>(() => new ArticleDefinitionText().Load(Node("id: X\npages: {a: 1}"), 0));
	}

	[Fact]
	public void Loads_all_xcom1_articles()
	{
		var path = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "standard", "xcom1", "ufopaedia.rul");
		var root = new YamlRootNodeReader(File.ReadAllText(path), path);
		var articles = new Dictionary<string, ArticleDefinition>(StringComparer.Ordinal);
		int listOrder = 0;
		foreach (var node in root["ufopaedia"].Children())
		{
			var article = Create(node["type_id"].ReadVal<UfopaediaTypeId>());
			listOrder += 100;
			article.Load(node, listOrder);
			articles[article.id] = article;
		}
		Assert.True(articles.Count > 100);

		var craft = Assert.IsType<ArticleDefinitionCraft>(articles["STR_SKYRANGER"]);
		Assert.Equal("STR_XCOM_CRAFT_ARMAMENT", craft.section);
		Assert.Equal("UP004.SPK", craft.image_id);
		Assert.Equal((160, 5, 160, 60), (craft.rect_stats.x, craft.rect_stats.y, craft.rect_stats.width, craft.rect_stats.height));
		Assert.Equal(5, craft.rect_text.x);
		Assert.False(craft.customPalette);

		var text = Assert.IsType<ArticleDefinitionText>(articles["STR_ALIEN_ORIGINS"]);
		Assert.Equal(["STR_ALIEN_ORIGINS"], text._requires);
		Assert.Equal("STR_ALIEN_ORIGINS_UFOPEDIA", text.GetTextForPage(0));
		Assert.Equal(1, text.NumberOfPages);

		Assert.IsType<ArticleDefinitionArmor>(articles["STR_PERSONAL_ARMOR_UC"]);
	}
}
