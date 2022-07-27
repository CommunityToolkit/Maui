using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Categories;

namespace CommunityToolkit.Maui.Helpers;

#pragma warning disable IDE0040 // Add accessibility modifiers
internal static class LogHelpers
#pragma warning restore IDE0040 // Add accessibility modifiers
{
	public static void WriteLine( string message, LogLevels level = LogLevels.Info,  [CallerFilePath]string? callerFilePath = null, [CallerMemberName] string memberName = null!)
	{
		if (!Manager.Current.IsLogEnabled)
		{
			return;
		}
		if (Manager.Current.LogLevel > level)
		{
			return;
		}

		var classFilename = Path.GetFileNameWithoutExtension(callerFilePath);
		if (string.IsNullOrWhiteSpace(memberName))
		{
			memberName = "";
		}
		Console.WriteLine($"** DEBUG ** {nameof(OnScreenSizeMarkup)} ({classFilename}.{memberName}): {message}");
	}
}