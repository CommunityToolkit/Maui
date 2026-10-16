namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Base class for CommunityToolkit.Maui device tests (dotnet/maui's ControlsHandlerTestBase counterpart). xUnit creates a new
/// instance per test, so every test gets its own MauiApp with UseMauiCommunityToolkit() and the Controls handlers.
/// </summary>
public abstract class CommunityToolkitHandlerTestBase : HandlerTestBase
{
	Action<Options>? configureToolkitOptions;
	StaticPropertySnapshot? toolkitOptionsSnapshot;

	/// <summary>
	/// Builds this test's MauiApp. Call it first in a test (dotnet/maui's SetupBuilder convention); without it the MauiApp is
	/// built with defaults on first use of <see cref="HandlerTestBasement.MauiContext"/>.
	/// </summary>
	/// <param name="configureBuilder">Registers extra handlers or services, e.g. <c>builder => builder.ConfigureMauiHandlers(h => h.AddHandler&lt;MyView, MyHandler&gt;())</c>.</param>
	/// <param name="configureToolkitOptions">
	/// Options passed to UseMauiCommunityToolkit. The toolkit stores them in static properties, so they apply to the whole app:
	/// the test class must be in the <see cref="TestCollections.DisableParallelization"/> collection, and the previous values are
	/// restored when the test ends.
	/// </param>
	protected void SetupBuilder(Action<MauiAppBuilder>? configureBuilder = null, Action<Options>? configureToolkitOptions = null)
	{
		if (IsHandlerCreated)
		{
			throw new InvalidOperationException($"{nameof(SetupBuilder)} must run before the test uses {nameof(MauiContext)} or creates a handler.");
		}

		if (configureToolkitOptions is not null)
		{
			TestCollections.EnsureInDisableParallelizationCollection(GetType(), "changes the toolkit's app-wide options");
			toolkitOptionsSnapshot = new StaticPropertySnapshot().Capture(typeof(Options)).Capture(typeof(Core.Options));
		}

		this.configureToolkitOptions = configureToolkitOptions;
		EnsureHandlerCreated(configureBuilder);
	}

	protected override MauiAppBuilder ConfigureBuilder(MauiAppBuilder mauiAppBuilder) =>
		base.ConfigureBuilder(mauiAppBuilder).UseMauiCommunityToolkit(configureToolkitOptions);

	// Runs after every test (HandlerTestBasement implements xUnit's IAsyncLifetime).
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			toolkitOptionsSnapshot?.Restore();
		}
	}
}