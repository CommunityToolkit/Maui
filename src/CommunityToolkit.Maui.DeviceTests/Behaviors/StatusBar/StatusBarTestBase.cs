namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Base class of the status bar, navigation bar and window device tests. The status bar belongs to the runner's window, which is
/// shared by every test: <see cref="ResetNativeStateAsync"/> puts the platform window in a known state before a test arranges
/// its scenario and again when the test ends, so no test depends on what an earlier one left behind.
/// </summary>
public abstract partial class StatusBarTestBase : CommunityToolkitHandlerTestBase
{
	/// <summary>
	/// Resets the window state that the status bar tests change (Android: window flags, system bar colors and appearance, the
	/// overlay views; iOS: the status bar views in every window). Does nothing on the platforms where the toolkit has no status bar code.
	/// </summary>
	protected partial Task ResetNativeStateAsync();

	/// <summary>
	/// Polls <paramref name="condition"/> on the UI thread until it is true or the timeout (ms) elapsed. The default is generous: the
	/// device tests share a busy machine, and a condition that is already true returns at once.
	/// </summary>
	protected Task AssertEventuallyOnMainThread(Func<bool> condition, string message, int timeout = 10000) =>
		AssertionExtensions.AssertEventuallyAsync(() => InvokeOnMainThreadAsync(condition), timeout, 50, message);

	/// <summary>
	/// Waits until the behavior was detached <paramref name="detachedCount"/> times and is not attached any more. MAUI detaches a
	/// PlatformBehavior from the view's Unloaded event on the UI thread, after the view left the window; the page's own unloaded state
	/// (OnUnloadedAsync) can already be reached before that handler ran.
	/// </summary>
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