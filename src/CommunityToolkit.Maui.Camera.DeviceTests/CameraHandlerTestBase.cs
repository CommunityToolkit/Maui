namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Base class for CommunityToolkit.Maui.Camera device tests (dotnet/maui's ControlsHandlerTestBase counterpart). xUnit creates a
/// new instance per test, so every test gets its own MauiApp with UseMauiCommunityToolkitCamera() and the Controls handlers.
/// </summary>
public abstract class CameraHandlerTestBase : HandlerTestBase
{
	/// <summary>
	/// Builds this test's MauiApp. Call it first in a test (dotnet/maui's SetupBuilder convention); without it the MauiApp is
	/// built with defaults on first use of <see cref="HandlerTestBasement.MauiContext"/>.
	/// </summary>
	/// <param name="configureBuilder">
	/// Registers extra handlers or services. CameraView and CameraViewHandler resolve ICameraProvider from the running app
	/// (IPlatformApplication.Current.Services, see MauiProgram), not from this MauiApp.
	/// </param>
	protected void SetupBuilder(Action<MauiAppBuilder>? configureBuilder = null)
	{
		if (IsHandlerCreated)
		{
			throw new InvalidOperationException($"{nameof(SetupBuilder)} must run before the test uses {nameof(MauiContext)} or creates a handler.");
		}

		EnsureHandlerCreated(configureBuilder);
	}

	protected override MauiAppBuilder ConfigureBuilder(MauiAppBuilder mauiAppBuilder) =>
		base.ConfigureBuilder(mauiAppBuilder).UseMauiCommunityToolkitCamera();
}