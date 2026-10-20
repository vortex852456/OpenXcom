using System.Globalization;

namespace Acom.Tests.Engine;

public class ShaderDrawTests
{
	/// <summary>Compares every (src, shade, colour group) combination with the C++ helpers.</summary>
	[Fact]
	public void Shading_matches_cpp_reference()
	{
		int rows = 0;
		foreach (var line in Golden.ReadLines("shade.txt"))
		{
			var f = Golden.Fields(line);
			var expected = Convert.FromHexString(f[^1]);
			Assert.Equal(256, expected.Length);
			for (int src = 0; src < 256; ++src)
			{
				byte dest = 0xAB;
				if (f[0] == "standard")
				{
					ShaderDrawHelper.StandardShade(ref dest, (byte)src, int.Parse(f[1], CultureInfo.InvariantCulture));
				}
				else
				{
					int newColor = (int.Parse(f[1], CultureInfo.InvariantCulture) - 1) << 4;
					ShaderDrawHelper.ColorReplace(ref dest, (byte)src, int.Parse(f[2], CultureInfo.InvariantCulture), newColor);
				}
				Assert.Equal(expected[src], dest);
			}
			rows++;
		}
		Assert.Equal(16 + 3 * 16, rows);
	}
}
