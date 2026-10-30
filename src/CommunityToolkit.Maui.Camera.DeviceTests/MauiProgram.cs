using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests;

// Same shape as dotnet/maui's src/Controls/tests/DeviceTests/MauiProgram.cs. The runner source generator calls CreateMauiApp from the
// generated platform entry points, for the headless and the visual runner.
public static class MauiProgram
{
	public static MauiApp CreateMauiApp() =>
		MauiProgramDefaults.CreateMauiApp(_ => new TestOptions
		{
			Assemblies = [typeof(MauiProgram).Assembly],
			SkipCategories = typeof(TestCategory).GetHeadlessSkipCategories(TestCategory.RequiresUserInteraction)
		}, ConfigureApp);

	// CameraViewHandler resolves ICameraProvider from IPlatformApplication.Current.Services, which belongs to this runner MauiApp, so the
	// camera must be registered here as well as in each test's own MauiApp (SetupBuilder).
	static void ConfigureApp(MauiAppBuilder builder) =>
		builder.UseMauiCommunityToolkitCamera();
}