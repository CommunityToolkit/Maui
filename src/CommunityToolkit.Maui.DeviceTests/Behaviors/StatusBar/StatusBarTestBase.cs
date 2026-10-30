namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// The status bar belongs to the runner's window, which every test shares: <see cref="ResetNativeStateAsync"/> puts the platform window in
/// a known state before a test arranges its scenario and again when the test ends, so no test depends on what an earlier one left behind.
/// </summary>
public abstract partial class StatusBarTestBase : CommunityToolkitHandlerTestBase
{
	protected partial Task ResetNativeStateAsync();

	// The default timeout is generous because the device tests share a busy machine; a condition that is already true returns at once.
	protected Task AssertEventuallyOnMainThread(Func<bool> condition, string message, int timeout = 10000) =>
		AssertionExtensions.AssertEventuallyAsync(() => InvokeOnMainThreadAsync(condition), timeout, 50, message);

	// MAUI detaches a PlatformBehavior from the view's Unloaded event on the UI thread, after the view left the window, so the page's own
	// unloaded state (OnUnloadedAsync) can already be reached before the behavior was detached.
	protected Task WaitForDetachAsync(RecordingStatusBarBehavior behavior, int detachedCount = 1) =>
		AssertEventuallyOnMainThread(() => behavior.DetachedCount >= detachedCount && !behavior.IsAttached, $"The behavior was not detached {detachedCount} time(s) (DetachedCount = {behavior.DetachedCount}, IsAttached = {behavior.IsAttached}).");

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			await ResetNativeStateAsync();
		}
	}
}