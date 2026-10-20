using Bulwark.Engine;

namespace Bulwark.Tests.Engine;

public class LanguagePluralityTests
{
	[Fact]
	public void Matches_cpp_reference_for_all_languages()
	{
		uint[] numbers = [.. Enumerable.Range(0, 231).Select(i => (uint)i), 1000, 1001, 1011, 1021, 1112, uint.MaxValue];
		var lines = Golden.ReadLines("plurality.txt");
		Assert.Equal(17, lines.Length);
		foreach (var line in lines)
		{
			var f = Golden.Fields(line);
			var plurality = LanguagePlurality.Create(f[0]);
			Assert.Equal(numbers.Length, f.Length - 1);
			for (int i = 0; i < numbers.Length; ++i)
			{
				Assert.Equal("_" + f[i + 1], plurality.GetSuffix(numbers[i]));
			}
		}
	}
}
