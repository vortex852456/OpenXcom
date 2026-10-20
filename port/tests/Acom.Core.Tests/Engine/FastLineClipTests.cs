using System.Globalization;

namespace Acom.Tests.Engine;

/// <summary>FastLineClip.LineClip against the C++ original (Golden/lineclip.txt from port/tools/golden/golden.cpp).</summary>
public class FastLineClipTests
{
	private static double D(string s) => double.Parse(s, CultureInfo.InvariantCulture);

	[Fact]
	public void LineClip_matches_cpp_reference()
	{
		var clip = new FastLineClip(10, 330, 20, 220);
		int rows = 0;
		foreach (var line in Golden.ReadLines("lineclip.txt"))
		{
			var f = Golden.Fields(line);
			double x0 = D(f[1]), y0 = D(f[2]), x1 = D(f[3]), y1 = D(f[4]);
			int visible = clip.LineClip(ref x0, ref y0, ref x1, ref y1);
			Assert.True(int.Parse(f[5], CultureInfo.InvariantCulture) == visible, line);
			Assert.True(D(f[6]) == x0 && D(f[7]) == y0 && D(f[8]) == x1 && D(f[9]) == y1, line);
			rows++;
		}
		Assert.True(rows > 4000);
	}
}
