namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class ColorAnimationTestBase
{
	protected static Task EnsureAnimationsCanEndAsync() => Task.CompletedTask;

	static ValueTask PlatformDisposeAsync() => ValueTask.CompletedTask;
}