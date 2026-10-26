namespace Acom.Tests.Engine;

public class NumberFormatTests
{
	[Theory]
	[InlineData(0L, "0")]
	[InlineData(999L, "999")]
	[InlineData(1000L, "1 000")]
	[InlineData(-1234567L, "-1 234 567")]
	public void Numbers_get_nbsp_thousands_separators(long value, string expected)
	{
		Assert.Equal(expected, Unicode.FormatNumber(value));
	}

	[Fact]
	public void Currency_goes_in_front_if_one_byte_else_behind()
	{
		Assert.Equal("$1 000", Unicode.FormatNumber(1000, "$"));
		Assert.Equal("-$5", Unicode.FormatNumber(-5, "$"));
		Assert.Equal("1 000€", Unicode.FormatNumber(1000, "€"));
		Assert.Equal("42%", Unicode.FormatPercentage(42));
	}
}
