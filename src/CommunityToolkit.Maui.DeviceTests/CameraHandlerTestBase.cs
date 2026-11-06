namespace CommunityToolkit.Maui.DeviceTests;

// The counterpart of dotnet/maui's ControlsHandlerTestBase.
public abstract class CameraHandlerTestBase : HandlerTestBase
{
	// CameraView and CameraViewHandler resolve ICameraProvider from the running app (IPlatformApplication.Current.Services, see MauiProgram),
	// not from the MauiApp that configureBuilder configures.
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