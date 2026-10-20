namespace Acom.Tests.Savegame;

public class SerializationHelperTests
{
	[Fact]
	public void Integers_are_packed_little_endian_and_advance_the_buffer()
	{
		var buffer = new byte[7];
		Span<byte> w = buffer;
		SerializationHelper.SerializeInt(ref w, 1, 200);
		SerializationHelper.SerializeInt(ref w, 2, -2);
		SerializationHelper.SerializeInt(ref w, 4, -5);
		Assert.Equal(0, w.Length);
		Assert.Equal(new byte[] { 200, 0xFE, 0xFF, 0xFB, 0xFF, 0xFF, 0xFF }, buffer);

		ReadOnlySpan<byte> r = buffer;
		Assert.Equal(200, SerializationHelper.UnserializeInt(ref r, 1));
		Assert.Equal(-2, SerializationHelper.UnserializeInt(ref r, 2));
		Assert.Equal(-5, SerializationHelper.UnserializeInt(ref r, 4));
		Assert.Equal(0, r.Length);
	}

	[Fact]
	public void Two_byte_values_are_signed_like_Sint16()
	{
		var buffer = new byte[2];
		Span<byte> w = buffer;
		SerializationHelper.SerializeInt(ref w, 2, 40000);
		ReadOnlySpan<byte> r = buffer;
		Assert.Equal(40000 - 65536, SerializationHelper.UnserializeInt(ref r, 2));
	}

	[Theory]
	[InlineData(0.0, "0")]
	[InlineData(-0.0, "-0")]
	[InlineData(1.0, "1")]
	[InlineData(-2.5, "-2.5")]
	[InlineData(0.1, "0.10000000000000001")]
	[InlineData(1.0 / 3, "0.33333333333333331")]
	[InlineData(0.1 + 0.2, "0.30000000000000004")]
	[InlineData(1e-4, "0.0001")]
	[InlineData(1e-5, "1.0000000000000001e-05")]
	[InlineData(1e16, "10000000000000000")]
	[InlineData(1e17, "1e+17")]
	[InlineData(1e20, "1e+20")]
	[InlineData(123456.789, "123456.789")]
	[InlineData(1.5707963267948966, "1.5707963267948966")]
	public void Doubles_are_formatted_like_printf_17g(double value, string expected)
	{
		Assert.Equal(expected, SerializationHelper.SerializeDouble(value));
		Assert.Equal(value, double.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
	}

	[Fact]
	public void Base64_roundtrip()
	{
		byte[] data = [0, 1, 2, 250, 255];
		var writer = new YamlRootNodeWriter();
		writer.WriteBase64("data", data);
		Assert.Equal("data: AAEC+v8=", writer.Emit().Trim());
		var reader = new YamlRootNodeReader(writer.Emit(), "test");
		Assert.Equal(data, reader["data"].ReadValBase64());
	}
}
