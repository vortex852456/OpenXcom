using System.Reflection;
using System.Runtime.CompilerServices;

namespace Acom.Tests.Battlescape;

/// <summary>
/// Builds a <see cref="Map"/> without its constructor (which needs a loaded mod and the still stubbed
/// BattlescapeMessage); only the fields used by Camera/selector/walking-offset logic are filled.
/// </summary>
internal static class MapTestHelpers
{
	public static void SetField(object obj, Type type, string name, object? value) =>
		type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(obj, value);

	public static Map BareMap(int width, int height, int spriteHeight = 40, SavedBattleGame? save = null)
	{
		var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
		SetField(map, typeof(Surface), "_width", width);
		SetField(map, typeof(Surface), "_height", height);
		SetField(map, typeof(Surface), "_scale", 1);
		SetField(map, typeof(Map), "_spriteHeight", spriteHeight);
		SetField(map, typeof(Map), "_spriteWidth", 32);
		SetField(map, typeof(Map), "_cursorType", CursorType.CT_NORMAL);
		SetField(map, typeof(Map), "_save", save);
		return map;
	}

	/// <summary>A camera on a bare map of the given screen size, linked back like Map's constructor does.</summary>
	public static Camera NewCamera(int mapX, int mapY, int mapZ, int screenW = 320, int screenH = 200, int visibleH = 164)
	{
		Map map = BareMap(screenW, screenH);
		var camera = new Camera(32, 40, mapX, mapY, mapZ, map, visibleH);
		SetField(map, typeof(Map), "_camera", camera);
		return camera;
	}
}
