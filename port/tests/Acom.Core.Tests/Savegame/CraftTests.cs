namespace Acom.Tests.Savegame;

public class CraftTests
{
	[Theory]
	[InlineData(0.0, "0")]
	[InlineData(1.5, "1.5")]
	[InlineData(100.0, "100")]
	[InlineData(0.1, "0.10000000000000001")]
	[InlineData(3.14159, "3.1415899999999999")]
	[InlineData(-2.5e-7, "-2.4999999999999999e-07")]
	[InlineData(1e-5, "1.0000000000000001e-05")]
	[InlineData(1e20, "1e+20")]
	[InlineData(123456789012345678.0, "1.2345678901234568e+17")]
	[InlineData(double.PositiveInfinity, "inf")]
	[InlineData(double.NegativeInfinity, "-inf")]
	public void SerializeDouble_matches_printf_17g(double value, string expected)
	{
		Assert.Equal(expected, Craft.SerializeDouble(value));
	}

	[Fact]
	public void XcomDistance_truncates_nautical_miles()
	{
		Assert.Equal(3437, Craft.XcomDistance(1.0));
		Assert.Equal(0, Craft.XcomDistance(0.0));
		Assert.Equal(-3437, Craft.XcomDistance(-1.0));
	}
}
