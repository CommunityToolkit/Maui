using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Base class for CommunityToolkit.Maui.MediaElement device tests (dotnet/maui's ControlsHandlerTestBase counterpart). xUnit creates
/// a new instance per test, so every test gets its own MauiApp with UseMauiCommunityToolkitMediaElement() and the Controls handlers.
/// </summary>
public abstract class MediaElementHandlerTestBase : HandlerTestBase
{
	bool isAndroidForegroundServiceEnabled;
	Action<MediaElementOptions>? configureMediaElementOptions;
	StaticPropertySnapshot? mediaElementOptionsSnapshot;

	/// <summary>
	/// Builds this test's MauiApp. Call it first in a test (dotnet/maui's SetupBuilder convention); without it the MauiApp is
	/// built with defaults on first use of <see cref="HandlerTestBasement.MauiContext"/>.
	/// </summary>
	/// <param name="configureBuilder">Registers extra handlers or services.</param>
	/// <param name="isAndroidForegroundServiceEnabled">Passed to UseMauiCommunityToolkitMediaElement.</param>
	/// <param name="configureMediaElementOptions">
	/// Options passed to UseMauiCommunityToolkitMediaElement. MediaElement stores them (and the foreground-service flag) in static
	/// properties, so they apply to the whole app: the test class must be in the <see cref="TestCollections.DisableParallelization"/>
	/// collection, and the previous values are restored when the test ends.
	/// </param>
	protected void SetupBuilder(Action<MauiAppBuilder>? configureBuilder = null, bool isAndroidForegroundServiceEnabled = false, Action<MediaElementOptions>? configureMediaElementOptions = null)
	{
		if (IsHandlerCreated)
		{
			throw new InvalidOperationException($"{nameof(SetupBuilder)} must run before the test uses {nameof(MauiContext)} or creates a handler.");
		}

		if (isAndroidForegroundServiceEnabled || configureMediaElementOptions is not null)
		{
			TestCollections.EnsureInDisableParallelizationCollection(GetType(), "changes MediaElement's app-wide options");
			mediaElementOptionsSnapshot = new StaticPropertySnapshot().Capture(typeof(MediaElementOptions));
		}

		this.isAndroidForegroundServiceEnabled = isAndroidForegroundServiceEnabled;
		this.configureMediaElementOptions = configureMediaElementOptions;
		EnsureHandlerCreated(configureBuilder);
	}

	protected override MauiAppBuilder ConfigureBuilder(MauiAppBuilder mauiAppBuilder) =>
		base.ConfigureBuilder(mauiAppBuilder).UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled, configureMediaElementOptions);

	// Runs after every test (HandlerTestBasement implements xUnit's IAsyncLifetime).
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