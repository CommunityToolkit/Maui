namespace CommunityToolkit.Maui.DeviceTests;

public partial class TouchBehaviorLifecycleTests
{
	// A pointer handler left on the element hands the next pointer event (a mouse moving over the element is enough) to the disposed behavior,
	// which throws ObjectDisposedException inside a WinUI event handler. WinUI offers no way to observe the event subscriptions of an element,
	// and a test cannot raise pointer events (see NativePointerInputUnavailableReason), but a subscription is a reference from the element to
	// the behavior: the disposed behavior must be collectable while the element lives.
	[Fact]
	public async Task DisposeRemovesThePointerHandlersFromTheElement()
	{
		SetupBuilder();

		var (parent, child) = await InvokeOnMainThreadAsync(() => CreateDetachedPlatformViews());
		var behaviorReference = await InvokeOnMainThreadAsync(() => AttachAndDispose(child));

		await AssertionExtensions.WaitForGC(behaviorReference);
		GC.KeepAlive(parent);
		GC.KeepAlive(child);
	}

	// A method of its own, so that no local of the test keeps the behavior alive.
	static WeakReference AttachAndDispose(Microsoft.UI.Xaml.FrameworkElement platformView)
	{
		var behavior = new DirectTouchBehavior();
		behavior.AttachDirect(new Grid(), platformView);
		DisposeWhileAttached(behavior);

		return new WeakReference(behavior);
	}
}