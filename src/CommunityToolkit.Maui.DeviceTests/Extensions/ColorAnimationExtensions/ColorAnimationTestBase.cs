namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class ColorAnimationTestBase : CommunityToolkitHandlerTestBase
{
	// The animation name of the generated TextColorTo: nameof(TextColorTo).
	protected const string textColorToAnimationName = "TextColorTo";

	protected const uint animationLength = 100;

	// A stalled animation fails the test instead of hanging the whole run until the runner's timeout.
	protected static readonly TimeSpan animationTimeout = TimeSpan.FromSeconds(10);

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			await PlatformDisposeAsync();
		}
	}
}
