using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

// The counterpart of dotnet/maui's ControlsHandlerTestBase.
public abstract class MediaElementHandlerTestBase : HandlerTestBase
{
	bool isAndroidForegroundServiceEnabled;
	Action<MediaElementOptions>? configureMediaElementOptions;
	StaticPropertySnapshot? mediaElementOptionsSnapshot;

	protected void SetupBuilder(Action<MauiAppBuilder>? configureBuilder = null, bool isAndroidForegroundServiceEnabled = false, Action<MediaElementOptions>? configureMediaElementOptions = null)
	{
		if (IsHandlerCreated)
		{
			throw new InvalidOperationException($"{nameof(SetupBuilder)} must run before the test uses {nameof(MauiContext)} or creates a handler.");
		}

		if (isAndroidForegroundServiceEnabled || configureMediaElementOptions is not null)
		{
			// MediaElement stores these options (and the foreground-service flag) in static properties, so they apply to the whole app, not just
			// this test's MauiApp.
			TestCollections.EnsureInDisableParallelizationCollection(GetType(), "changes MediaElement's app-wide options");
			mediaElementOptionsSnapshot = new StaticPropertySnapshot().Capture(typeof(MediaElementOptions));
		}

		this.isAndroidForegroundServiceEnabled = isAndroidForegroundServiceEnabled;
		this.configureMediaElementOptions = configureMediaElementOptions;
		EnsureHandlerCreated(configureBuilder);
	}

	protected override MauiAppBuilder ConfigureBuilder(MauiAppBuilder mauiAppBuilder) =>
		base.ConfigureBuilder(mauiAppBuilder).UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled, configureMediaElementOptions);

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			mediaElementOptionsSnapshot?.Restore();
		}
	}
}