using System.Runtime.CompilerServices;

namespace Acom.Tests.Savegame;

/// <summary>Helpers for Savegame tests whose rule classes (Mod/Rule*) may still be generated stubs.</summary>
internal static class SavegameTestHelpers
{
	/// <summary>
	/// A rule object without running its (possibly stubbed) constructor. Usable as identity key
	/// (e.g. ItemContainer contents) as long as none of its members is called.
	/// </summary>
	public static T Uninitialized<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

	/// <summary>Runs <paramref name="create"/>; skips the test while a needed unit is still a stub.</summary>
	public static T OrSkip<T>(Func<T> create)
	{
		try
		{
			return create();
		}
		catch (NotImplementedException e)
		{
			Assert.Skip($"needs a unit that is still a stub: {e.TargetSite?.DeclaringType?.Name}.{e.TargetSite?.Name}");
			return default!;
		}
	}

	/// <summary>Runs <paramref name="action"/>; skips the test while a needed unit is still a stub.</summary>
	public static void OrSkip(System.Action action) => OrSkip(() => { action(); return 0; });

	public static YamlNodeReader Yaml(string text) => new YamlRootNodeReader(text, "test").ToBase();

	/// <summary>Keys of a map node in document order.</summary>
	public static List<string> Keys(YamlNodeReader map) => map.Children().Select(c => c.ReadKey<string>()).ToList();
}
