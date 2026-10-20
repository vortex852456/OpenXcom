using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Acom.Tests.Interface;

/// <summary>
/// Text layout/drawing with the bin/common fonts and the drawing of TextButton, ToggleTextButton, Window, Frame,
/// Bar, ProgressBar, ArrowButton, NumberText and Cursor against the original widgets
/// (Golden/widgets.txt from port/tools/golden/widgets.cpp).
/// </summary>
[Collection(nameof(Engine.OptionsTests))] // Text.Draw reads Options.debugUi
public class WidgetGoldenTests
{
	private static readonly Lazy<(Font Big, Font Small)> s_fonts = new(LoadFonts);

	private static (Font, Font) LoadFonts()
	{
		var common = Path.Combine(Engine.LanguageTests.RepoRoot(), "bin", "common");
		var reader = YamlRootNodeReader.FromFile(Path.Combine(common, "Language", "Font.dat"));
		Font big = null!, small = null!;
		foreach (var fontReader in reader["fonts"].Children())
		{
			string id = fontReader["id"].ReadVal<string>();
			if (id != "FONT_BIG" && id != "FONT_SMALL")
				continue;
			var font = new Font();
			font.Load(fontReader, f => File.ReadAllBytes(Path.Combine(common, f)));
			if (id == "FONT_BIG")
				big = font;
			else
				small = font;
		}
		return (big, small);
	}

	private static int Int(string s) => int.Parse(s, CultureInfo.InvariantCulture);

	private static bool Bool(string s) => Int(s) != 0;

	private static byte Byte(string s) => (byte)Int(s);

	private static double Double(string s) => double.Parse(s, CultureInfo.InvariantCulture);

	private static string Hex(string s) => s == "-" ? string.Empty : Encoding.UTF8.GetString(Convert.FromHexString(s));

	private static ulong Fnv(Surface s)
	{
		ulong h = 14695981039346656037UL;
		for (int y = 0; y < s.Height; ++y)
		{
			for (int x = 0; x < s.Width; ++x)
			{
				h ^= s.GetPixel(x, y);
				h *= 1099511628211UL;
			}
		}
		return h;
	}

	private static string Hash(Surface s) => Fnv(s).ToString("x16", CultureInfo.InvariantCulture);

	private static string Join(IEnumerable<int> values) => values.Any() ? string.Join(",", values) : "-";

	/// <summary>
	/// The processed text as C++ prints it (code points in hex); code points outside the BMP are a single '?'
	/// in the port (documented deviation), so the reference is mapped the same way.
	/// </summary>
	private static string Processed(string text) => text.Length == 0 ? "-" : string.Join(".", text.Select(c => ((int)c).ToString("x", CultureInfo.InvariantCulture)));

	private static string ExpectedProcessed(string reference) => reference == "-" ? reference
		: string.Join(".", reference.Split('.').Select(c => int.Parse(c, NumberStyles.HexNumber, CultureInfo.InvariantCulture) > 0xFFFF ? "3f" : c));

	/// <summary>Replays a "text" line; returns the actual result fields after the arguments.</summary>
	private static string RunText(string[] a, Font big, Font small)
	{
		var lang = new Language(a[8] == "R" ? TextDirection.DIRECTION_RTL : TextDirection.DIRECTION_LTR,
			a[7] == "L" ? TextWrapping.WRAP_LETTERS : TextWrapping.WRAP_WORDS);
		Options.debugUi = Bool(a[17]);
		try
		{
			var t = new Text(Int(a[2]), Int(a[3]), 0, 0);
			t.InitText(big, small, lang);
			if (a[1] == "B")
				t.SetBig();
			t.SetWordWrap(Bool(a[4]), Bool(a[5]), Bool(a[6]));
			t.Align = (TextHAlign)Int(a[9]);
			t.VerticalAlign = (TextVAlign)Int(a[10]);
			t.SetColor(Byte(a[11]));
			t.SetSecondaryColor(Byte(a[12]));
			t.SetInvert(Bool(a[13]));
			t.SetHighContrast(Bool(a[14]));
			t.SetScrollable(Bool(a[15]));
			t.SetText(Hex(a[18]));
			int wheel = Int(a[16]);
			for (int i = 0; i < Math.Abs(wheel); ++i)
				t.MousePress(WidgetTestHelpers.Press(0, 0, wheel > 0 ? SDLMouse.SDL_BUTTON_WHEELDOWN : SDLMouse.SDL_BUTTON_WHEELUP), null!);
			t.Draw();
			return string.Join(' ', t.Font == big ? "B" : "S", t.NumLines, Join(t.LineWidths), Join(t.LineHeights),
				t.GetTextWidth(), t.GetTextHeight(), Processed(t.ProcessedText), Hash(t));
		}
		finally
		{
			Options.debugUi = false;
		}
	}

	private static Surface Background()
	{
		var background = new Surface(320, 200, 0, 0, 1);
		for (int y = 0; y < 200; ++y)
			for (int x = 0; x < 320; ++x)
				background.SetPixel(x, y, (byte)((x * 3 + y * 5) % 200 + (x % 7 == 0 ? 0 : 1)));
		return background;
	}

	/// <summary>Replays a widget line (all but "text"); returns the hash of the drawn widget.</summary>
	private static string RunWidget(string[] a, Font big, Font small, Surface background)
	{
		Surface drawn;
		switch (a[0])
		{
			case "button":
			{
				var b = new TextButton(Int(a[1]), Int(a[2]), 0, 0);
				b.InitText(big, small, new Language(TextDirection.DIRECTION_LTR, TextWrapping.WRAP_WORDS));
				b.SetColor(Byte(a[3]));
				b.SetHighContrast(Bool(a[4]));
				b.SetGeoscapeButton(Bool(a[5]));
				if (Bool(a[6]))
					b.SetGroup(new StrongBox<TextButton>(b));
				b.Text = Hex(a[7]);
				b.Draw();
				drawn = b;
				break;
			}
			case "toggle":
			{
				var b = new ToggleTextButton(Int(a[1]), Int(a[2]), 0, 0);
				b.InitText(big, small, new Language(TextDirection.DIRECTION_LTR, TextWrapping.WRAP_WORDS));
				b.SetColor(Byte(a[3]));
				b.SetHighContrast(Bool(a[4]));
				b.SetGeoscapeButton(Bool(a[5]));
				if (Int(a[7]) >= 0)
					b.SetInvertColor(Byte(a[7]));
				b.Pressed = Bool(a[6]);
				b.Text = Hex(a[8]);
				b.Draw();
				drawn = b;
				break;
			}
			case "window":
			{
				var w = new Window(null!, Int(a[1]), Int(a[2]), Int(a[3]), Int(a[4]), (WindowPopup)Int(a[5]));
				for (int i = 0; i < Int(a[6]); ++i)
					w.Popup();
				w.SetColor(Byte(a[7]));
				w.SetHighContrast(Bool(a[8]));
				if (Bool(a[9]))
					w.SetThinBorder();
				w.SetInnerColor(Byte(a[10]));
				if (Bool(a[11]))
					w.SetBackground(background);
				if (Int(a[12]) != -9999)
					w.SetDX(Int(a[12]));
				if (Int(a[13]) != -9999)
					w.SetDY(Int(a[13]));
				w.Draw();
				drawn = w;
				break;
			}
			case "frame":
			{
				var f = new Frame(Int(a[1]), Int(a[2]), 0, 0);
				f.SetColor(Byte(a[3]));
				f.SetSecondaryColor(Byte(a[4]));
				f.SetHighContrast(Bool(a[5]));
				f.SetThickness(Int(a[6]));
				f.Draw();
				drawn = f;
				break;
			}
			case "bar":
			{
				var b = new Bar(Int(a[1]), Int(a[2]), 0, 0);
				b.SetColor(Byte(a[3]));
				b.SetSecondaryColor(Byte(a[4]));
				if (Int(a[5]) != 0)
					b.SetBorderColor(Byte(a[5]));
				b.Scale = Double(a[6]);
				b.Max = Double(a[7]);
				b.Value = Double(a[8]);
				b.Value2 = Double(a[9]);
				b.SetSecondValueOnTop(Bool(a[10]));
				b.Draw();
				drawn = b;
				break;
			}
			case "progress":
			{
				var p = new ProgressBar(Int(a[1]), Int(a[2]), 0, 0);
				p.SetColor(Byte(a[3]));
				p.SetBorderColor(Byte(a[4]));
				p.Value = Int(a[5]);
				p.Draw();
				drawn = p;
				break;
			}
			case "arrow":
			{
				var b = new ArrowButton((ArrowShape)Int(a[1]), Int(a[2]), Int(a[3]), 0, 0);
				b.SetColor(Byte(a[4]));
				b.Draw();
				drawn = b;
				break;
			}
			case "number":
			{
				var n = new NumberText(Int(a[1]), Int(a[2]), 0, 0);
				n.Value = uint.Parse(a[3], CultureInfo.InvariantCulture);
				n.SetColor(Byte(a[4]));
				n.SetBordered(Bool(a[5]));
				n.Draw();
				drawn = n;
				break;
			}
			case "cursor":
			{
				// C++ draws the lines with SDL_gfx through the palette; with its 3-3-2 colour cube every index maps to itself
				var c = new Cursor(Int(a[1]), Int(a[2]), 0, 0);
				c.SetColor(Byte(a[3]));
				c.Draw();
				drawn = c;
				break;
			}
			default:
				Assert.Fail("unknown case " + a[0]);
				return string.Empty;
		}
		return Hash(drawn);
	}

	[Fact]
	public void Text_layout_and_drawing_match_the_original()
	{
		var (big, small) = s_fonts.Value;
		var failed = new List<string>();
		int count = 0;
		foreach (var line in Golden.ReadLines("widgets.txt"))
		{
			var f = Golden.Fields(line);
			if (f[0] != "text")
				continue;
			++count;
			var expected = string.Join(' ', f[19..25].Append(ExpectedProcessed(f[25])).Append(f[26]));
			var actual = RunText(f, big, small);
			if (actual != expected)
				failed.Add(line + "\n  got " + actual);
		}
		Assert.True(count > 1500);
		Assert.True(failed.Count == 0, failed.Count + " mismatches:\n" + string.Join("\n", failed));
	}

	[Fact]
	public void Widget_drawing_matches_the_original()
	{
		var (big, small) = s_fonts.Value;
		var background = Background();
		var failed = new List<string>();
		var kinds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var line in Golden.ReadLines("widgets.txt"))
		{
			var f = Golden.Fields(line);
			if (f[0] == "text")
				continue;
			kinds.Add(f[0]);
			var actual = RunWidget(f, big, small, background);
			if (actual != f[^1])
				failed.Add(line + "\n  got " + actual);
		}
		Assert.Equal(9, kinds.Count);
		Assert.True(failed.Count == 0, failed.Count + " mismatches:\n" + string.Join("\n", failed));
	}
}
