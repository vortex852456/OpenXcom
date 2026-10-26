namespace Acom.Tests.Engine;

public class UnicodeTests
{
	[Fact]
	public void Code_points_count_surrogate_pairs_once()
	{
		Assert.Equal(0, Unicode.CodePointLength(""));
		Assert.Equal(3, Unicode.CodePointLength("abc"));
		Assert.Equal(3, Unicode.CodePointLength("a\U0001F600b"));
		Assert.Equal(2, Unicode.CodePointLength("\uD800x")); // lone surrogate
	}

	[Fact]
	public void Code_point_substr_like_cpp()
	{
		Assert.Equal("ab", Unicode.CodePointSubstr("abc", 0, 2));
		Assert.Equal("a\U0001F600", Unicode.CodePointSubstr("a\U0001F600b", 0, 2));
		Assert.Equal("\U0001F600b", Unicode.CodePointSubstr("a\U0001F600b", 1));
		Assert.Equal("abc", Unicode.CodePointSubstr("abc", 0, 99));
		Assert.Equal("", Unicode.CodePointSubstr("abc", 3));
		Assert.Equal("", Unicode.CodePointSubstr("abc", 1, 0));
		Assert.Throws<ArgumentOutOfRangeException>(() => Unicode.CodePointSubstr("abc", 4));
	}
}
