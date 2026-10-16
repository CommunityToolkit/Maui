using CommunityToolkit.Maui.DeviceTests.Runners;
#if WINDOWS
using CommunityToolkit.Maui.Maps;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// Same shape as dotnet/maui's src/Controls/tests/DeviceTests/MauiProgram.cs. The runner source generator calls CreateMauiApp from the
// generated platform entry points, for the headless runner (XHarness) and the visual runner (no arguments) alike.
public static class MauiProgram
{
#if MACCATALYST
	// macOS treats the test app, which XHarness starts in the background and nobody interacts with, as an idle background app and
	// coalesces its timers (App Nap): about a minute into a full run Task.Delay(50) took ~245 ms instead of ~52 ms, while the app stayed
	// active and the display link kept 120 fps. Every test that waits on a timer then ran 2-5x slower (GestureManagerEffectTests 46 s
	// instead of 16 s; the whole app ~10 min instead of ~6). A user-initiated, latency-critical activity keeps the timers precise for
	// the lifetime of the runner (UserInitiated alone was not enough). The token must stay referenced.
	static Foundation.NSObject? timerPrecisionActivity;
#endif

	public static MauiApp CreateMauiApp()
	{
#if MACCATALYST
		timerPrecisionActivity ??= Foundation.NSProcessInfo.ProcessInfo.BeginActivity(
			Foundation.NSActivityOptions.UserInitiated | Foundation.NSActivityOptions.LatencyCritical,
			"Running the CommunityToolkit.Maui device tests");
#endif

		return MauiProgramDefaults.CreateMauiApp(_ => new TestOptions
		{
			Assemblies = [typeof(MauiProgram).Assembly],
			SkipCategories = typeof(TestCategory).GetHeadlessSkipCategories(TestCategory.RequiresUserInteraction)
		}, ConfigureApp);
	}

	// IPlatformApplication.Current.Services, Application.Current and the app lifecycle events come from the runner's own MauiApp, so it
	// registers what the toolkit resolves from the running app. Each test also builds its own MauiApp (SetupBuilder).
	static void ConfigureApp(MauiAppBuilder builder)
	{
		builder.UseMauiCommunityToolkit(static options =>
		{
			// Needs the activation entries in Package.appxmanifest to register the Windows app-notification handler on launch.
			options.SetShouldEnableSnackbarOnWindows(true);
		});

#if WINDOWS
		// The Maps tests answer the request for the Bing control with a stand-in, so no real Bing Maps key is needed.
		builder.UseMauiCommunityToolkitMaps("device-tests-placeholder-key");
#endif
	}
}