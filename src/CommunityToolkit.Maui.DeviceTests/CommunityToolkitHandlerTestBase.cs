namespace CommunityToolkit.Maui.DeviceTests;

public abstract class CommunityToolkitHandlerTestBase : HandlerTestBase
{
	Action<Options>? configureToolkitOptions;
	StaticPropertySnapshot? toolkitOptionsSnapshot;

	protected void SetupBuilder(Action<MauiAppBuilder>? configureBuilder = null, Action<Options>? configureToolkitOptions = null)
	{
		if (IsHandlerCreated)
		{
			throw new InvalidOperationException($"{nameof(SetupBuilder)} must run before the test uses {nameof(MauiContext)} or creates a handler.");
		}

		if (configureToolkitOptions is not null)
		{
			// The toolkit stores its options in static properties, so they apply to the whole app and outlive this test's MauiApp.
			TestCollections.EnsureInDisableParallelizationCollection(GetType(), "changes the toolkit's app-wide options");
			toolkitOptionsSnapshot = new StaticPropertySnapshot().Capture(typeof(Options)).Capture(typeof(Core.Options));
		}

		this.configureToolkitOptions = configureToolkitOptions;
		EnsureHandlerCreated(configureBuilder);
	}

	protected override MauiAppBuilder ConfigureBuilder(MauiAppBuilder mauiAppBuilder) =>
		base.ConfigureBuilder(mauiAppBuilder).UseMauiCommunityToolkit(configureToolkitOptions);

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