using CommunityToolkit.Maui.Maps;
using DeviceRunners.VisualRunners;

namespace CommunityToolkit.Maui.DeviceTests;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseVisualTestRunner(conf => conf
				.AddCliConfiguration()
				.AddConsoleResultChannel()
				.AddTestAssembly(typeof(MauiProgram).Assembly)
				.AddXunit())
			// Snackbar on Windows also requires the notification activation entries in Platforms/Windows/Package.appxmanifest
			.UseMauiCommunityToolkit(static options => options.SetShouldEnableSnackbarOnWindows(true))
			.UseMauiCommunityToolkitCamera()
			.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false)
#if WINDOWS
			// Registers the Windows map handler; the tests never render a map, so no real Bing Maps key is needed
			.UseMauiCommunityToolkitMaps("KEY")
#else
			.UseMauiMaps()
#endif
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		return builder.Build();
	}
}
