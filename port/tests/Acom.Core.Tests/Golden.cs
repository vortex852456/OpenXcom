namespace Acom.Tests;

/// <summary>Access to golden reference files generated from the original C++ code.</summary>
internal static class Golden
{
	public static string[] ReadLines(string name)
	{
		return File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Golden", name));
	}

	public static string[] Fields(string line) => line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
