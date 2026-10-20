using Acom.Engine;

namespace Acom.Tests.Engine;

public class YamlTests
{
	private static YamlRootNodeReader Root(string yaml) => new(yaml, "dummy");

	// --- ported from the self-tests at the end of src/Engine/Yaml.cpp ---

	[Fact]
	public void Single_and_multi_document_streams()
	{
		var reader = Root("foo: [1, 2]");
		Assert.True(reader["foo"].IsValid);
		Assert.False(reader["bar"].IsValid);
		Assert.True(reader.UseIndex()["foo"].IsValid);
		Assert.False(reader.UseIndex()["bar"].IsValid);

		reader = Root("---\nfoo: [1, 2]");
		Assert.True(reader["foo"].IsValid);
		Assert.True(reader.UseIndex()["foo"].IsValid);

		reader = Root("---\nfoo: [1, 2]\n---\nbar: 3");
		Assert.False(reader["foo"].IsValid);
		Assert.False(reader["bar"].IsValid);
		Assert.Throws<YamlReadException>(() => reader.UseIndex());
		Assert.True(reader[0]["foo"].IsValid);
		Assert.True(reader[1]["bar"].IsValid);
	}

	[Theory]
	[InlineData("map: ~", 0)]
	[InlineData("map:", 0)]
	[InlineData("map: {}", 0)]
	[InlineData("map: {foo: 1}", 1)]
	public void Index_accepts_maps_and_empty_values(string yaml, int count)
	{
		Assert.Equal(count, Root(yaml)["map"].UseIndex().ChildrenCount);
	}

	[Theory]
	[InlineData("map: []")]
	[InlineData("map: [2]")]
	[InlineData("map: 2")]
	[InlineData("map: bar")]
	public void Index_rejects_sequences_and_values(string yaml)
	{
		Assert.Throws<YamlReadException>(() => Root(yaml)["map"].UseIndex());
	}

	[Fact]
	public void Pairs_and_tuples_need_exact_length()
	{
		(int, int) p = default;
		Assert.False(Root("foo2: [1, 2]").ToBase().TryRead("foo", ref p));
		Assert.True(Root("foo: [1, 2]").ToBase().TryRead("foo", ref p));
		Assert.Equal((1, 2), p);
		Assert.Throws<YamlReadException>(() => Root("foo: [1, 2, 3]").ToBase().TryRead("foo", ref p));
		Assert.Throws<YamlReadException>(() => Root("foo: [1]").ToBase().TryRead("foo", ref p));
		Assert.Throws<YamlReadException>(() => Root("foo: 2").ToBase().TryRead("foo", ref p));

		(int, int, int) t = default;
		Assert.True(Root("foo: [1, 2, 32]").ToBase().TryRead("foo", ref t));
		Assert.Equal((1, 2, 32), t);
	}

	[Fact]
	public void Arrays_need_exact_length()
	{
		var a = new int[2];
		Assert.False(Root("foo2: [1, 2]").ToBase().TryRead("foo", ref a));
		Assert.True(Root("foo: [1, 2]").ToBase().TryRead("foo", ref a));
		Assert.Equal([1, 2], a);
		var b = new int[4];
		Root("foo: [1, 2, 42, 13]").ToBase().TryRead("foo", ref b);
		Assert.Equal([1, 2, 42, 13], b);
		Assert.Throws<YamlReadException>(() => Root("foo: [1, 2, 3]").ToBase().TryRead("foo", ref a));
		Assert.Throws<YamlReadException>(() => Root("foo: [1]").ToBase().TryRead("foo", ref a));
	}

	[Fact]
	public void Nullable_values()
	{
		int? n = null;
		Assert.True(Root("foo: 1").ToBase().TryRead("foo", ref n));
		Assert.Equal(1, n);
		Root("foo: -42").ToBase().TryRead("foo", ref n);
		Assert.Equal(-42, n);
		Root("foo: ~").ToBase().TryRead("foo", ref n);
		Assert.Null(n);
		Assert.Throws<YamlReadException>(() => Root("foo: []").ToBase().TryRead("foo", ref n));

		bool? b = null;
		Root("foo: true").ToBase().TryRead("foo", ref b);
		Assert.True(b);
		Root("foo: false").ToBase().TryRead("foo", ref b);
		Assert.False(b);
		Root("foo: ~").ToBase().TryRead("foo", ref b);
		Assert.Null(b);
	}

	// --- rapidyaml / c4core behaviour the rulesets rely on ---

	[Theory]
	[InlineData("0", false)]
	[InlineData("1", true)]
	[InlineData("true", true)]
	[InlineData("True", true)]
	[InlineData("FALSE", false)]
	[InlineData("2", true)]
	public void Bools_follow_c4core(string text, bool expected)
	{
		Assert.Equal(expected, Root($"v: {text}")["v"].ReadVal<bool>());
	}

	[Theory]
	[InlineData("42", 42)]
	[InlineData("-7", -7)]
	[InlineData("0x1F", 31)]
	[InlineData("0b101", 5)]
	[InlineData("0o17", 15)]
	public void Integers_follow_c4core(string text, int expected)
	{
		Assert.Equal(expected, Root($"v: {text}")["v"].ReadVal<int>());
	}

	[Theory]
	[InlineData("1.5")]
	[InlineData("abc")]
	[InlineData("3000000000")]
	public void Invalid_integers_throw(string text)
	{
		Assert.Throws<YamlReadException>(() => Root($"v: {text}")["v"].ReadVal<int>());
	}

	[Fact]
	public void Duplicate_keys_return_first_match()
	{
		var reader = Root("a: 1\nb: 2\na: 3").ToBase();
		Assert.Equal(1, reader["a"].ReadVal<int>());
		Assert.Equal(1, reader.UseIndex()["a"].ReadVal<int>());
		Assert.Equal(3, reader.Children().Count);
		Assert.Equal(2, reader.UseIndex().ChildrenCount);
	}

	[Fact]
	public void Merge_keys_follow_rapidyaml()
	{
		const string yaml = """
			base: &base {a: 1, b: 2, c: 3}
			item:
			  a: 10
			  <<: *base
			  c: 30
			""";
		var item = Root(yaml)["item"];
		Assert.Equal(1, item["a"].ReadVal<int>());   // merge overrides earlier key
		Assert.Equal(2, item["b"].ReadVal<int>());
		Assert.Equal(30, item["c"].ReadVal<int>());  // later key overrides merge
		Assert.Equal(["a", "b", "c"], item.Children().Select(c => c.Key));
	}

	[Fact]
	public void Aliases_are_copies_with_their_own_keys()
	{
		var reader = Root("x: &v [1, 2]\ny: *v");
		Assert.Equal([1, 2], reader["y"].ReadVal<List<int>>());
		Assert.Equal("y", reader["y"].Key);
	}

	[Fact]
	public void Collections_strings_tags_and_null()
	{
		var reader = Root("list: [3, 4]\nmap: {1: a, 2: b}\nempty:\ntext: \"\"\ntagged: !add [1]\nscalarAsList: 5").ToBase();
		Assert.Equal([3, 4], reader["list"].ReadVal<List<int>>());
		Assert.Equal(new Dictionary<int, string> { [1] = "a", [2] = "b" }, reader["map"].ReadVal<Dictionary<int, string>>());
		Assert.True(reader["empty"].HasNullVal);
		Assert.Equal("", reader["empty"].ReadVal<string>());
		Assert.False(reader["text"].HasNullVal);
		Assert.True(reader["tagged"].HasValTagNamed("!add"));
		Assert.Empty(reader["scalarAsList"].ReadVal<List<int>>()); // like C++: vector from scalar is empty
		Assert.Throws<YamlReadException>(() => reader["list"].ReadVal<string>());
	}

	[Fact]
	public void Errors_name_file_line_and_column()
	{
		var e = Assert.Throws<YamlReadException>(() => new YamlRootNodeReader("a: 1\nb: x", "some/dir/test.rul")["b"].ReadVal<int>());
		Assert.StartsWith("test.rul:2:4:", e.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Writer_round_trips()
	{
		var root = new YamlRootNodeWriter();
		root.Write("name", "Alpha base");
		root.Write("count", 3);
		root.Write("ratio", 0.8);
		root.Write("flag", true);
		root.Write("list", new List<int> { 1, 2 }).SetFlowStyle();
		root.TryWrite("skipped", 5, 5);
		root.Write("items", new List<string> { "a", "b" }, (w, s) => w.Write().Write("type", s));
		var quoted = root.Write("escaped", "line\u0001");
		quoted.SetAsQuotedAndEscaped();
		root["section"].Write("x", -1);

		string text = root.Emit();
		Assert.Contains("name: Alpha base", text, StringComparison.Ordinal);
		Assert.Contains("ratio: 0.8", text, StringComparison.Ordinal);
		Assert.Contains("flag: true", text, StringComparison.Ordinal);
		Assert.Contains("list: [1, 2]", text, StringComparison.Ordinal);
		Assert.Contains("  - type: a", text, StringComparison.Ordinal);
		Assert.DoesNotContain("skipped", text, StringComparison.Ordinal);

		var back = new YamlRootNodeReader(text, "roundtrip").ToBase();
		Assert.Equal("Alpha base", back["name"].ReadVal<string>());
		Assert.Equal(0.8, back["ratio"].ReadVal<double>());
		Assert.Equal([1, 2], back["list"].ReadVal<List<int>>());
		Assert.Equal("b", back["items"][1]["type"].ReadVal<string>());
		Assert.Equal("line\u0001", back["escaped"].ReadVal<string>());
		Assert.Equal(-1, back["section"]["x"].ReadVal<int>());
	}

	[Fact]
	public void Reader_emit_reproduces_content()
	{
		var reader = Root("a: 1\nb:\n  - x\n  - y\n");
		var again = new YamlRootNodeReader(reader.ToBase().Emit(), "again").ToBase();
		Assert.Equal(["x", "y"], again["b"].ReadVal<List<string>>());
	}
}
