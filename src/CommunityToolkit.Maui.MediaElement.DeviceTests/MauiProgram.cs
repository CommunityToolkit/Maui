using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests;

// Same shape as dotnet/maui's src/Controls/tests/DeviceTests/MauiProgram.cs. The runner source generator calls CreateMauiApp
// from the generated platform entry points; the headless runner (XHarness) and the visual runner (no arguments) share it.
public static class MauiProgram
{
	public static MauiApp CreateMauiApp() =>
		MauiProgramDefaults.CreateMauiApp(_ => new TestOptions
		{
			Assemblies = [typeof(MauiProgram).Assembly],
			SkipCategories = typeof(TestCategory).GetHeadlessSkipCategories(TestCategory.RequiresUserInteraction)
		}, ConfigureApp);

	// The runner's own MauiApp: IPlatformApplication.Current.Services, Application.Current and the app lifecycle events come
	// from here, so register what the toolkit resolves from the running app. Each test also builds its own MauiApp (SetupBuilder).
	// The foreground service stays off app-wide; tests that need it pass isAndroidForegroundServiceEnabled to SetupBuilder.
	static void ConfigureApp(MauiAppBuilder builder) =>
		builder.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false);
}