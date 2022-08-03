using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Helpers;

#pragma warning disable IDE0040 // Add accessibility modifiers
internal static class LogHelpers
#pragma warning restore IDE0040 // Add accessibility modifiers
{
	const string source = nameof(OnScreenSizeExtension);
	
	public static void Log( string message, LogLevel level = LogLevel.Information,  [CallerFilePath]string? callerFilePath = null, [CallerMemberName] string memberName = null!)
	{
		var logger = ServiceProvider.GetService<ILogger<OnScreenSizeExtension>>();
		
		if (!logger.IsEnabled(level))
		{
			return;
		}

		var classFilename = Path.GetFileNameWithoutExtension(callerFilePath);
		if (string.IsNullOrWhiteSpace(memberName))
		{
			memberName = "";
		}

		logger.Log(level, "*** {Source} ***  {ClassFilename}.{MemberName}: {Message}",source, classFilename,memberName,message);
	}
}