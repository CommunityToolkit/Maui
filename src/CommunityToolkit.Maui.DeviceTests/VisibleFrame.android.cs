using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// An oracle that shares no code with the toolkit (which asks AndroidX for the IME window insets): the window manager's visible display
// frame of the window loses the soft keyboard's height at its bottom, whatever the soft input mode (pan or resize) is. The keyboard counts
// as showing while the frame is more than 100 dp shorter (more than any navigation bar, less than any soft keyboard).
public sealed class VisibleFrame
{
	const int minimumKeyboardHeightInDp = 100;
	const int steadyPollInterval = 50;
	const int steadyPollsBeforeTheKeyboardShows = 10;

	readonly AView view;
	readonly int bottomWithoutKeyboard;
	readonly float minimumKeyboardHeight;

	VisibleFrame(AView view)
	{
		Assert.True(view.IsAttachedToWindow, "The visible frame is read from a view that is in a window.");
		Assert.False(SoftKeyboardHelpers.IsShowing(), "A soft keyboard was showing when the visible frame without keyboard was recorded.");
		this.view = view;
		bottomWithoutKeyboard = Bottom(view);
		minimumKeyboardHeight = MinimumKeyboardHeight(view);
	}

	public bool ShowsKeyboard => ShowsKeyboardAt(Bottom(view));

	// A test cannot rely on the one before it for the baseline: a soft keyboard that one asked for can still come up after this test started.
	public static async Task<VisibleFrame> RecordWithoutKeyboardAsync(AView view)
	{
		Assert.True(await SoftKeyboardHelpers.HideWhenShowingAsync(), $"The soft keyboard was still showing {SoftKeyboardHelpers.Timeout} ms after it was asked to hide, so no visible frame without keyboard can be recorded: {SoftKeyboardHelpers.Describe()}.");

		return new VisibleFrame(view);
	}

	// Before the keyboard is known to be hidden there is no recorded frame to compare with, so the bottom of the window stands in for it.
	public static bool KeyboardCoversTheBottomOf(AView decorView) => WindowBottom(decorView) - Bottom(decorView) > MinimumKeyboardHeight(decorView);

	public static string Describe(AView decorView) => $"visible frame bottom {Bottom(decorView)} px of a window that ends at {WindowBottom(decorView)} px, keyboard threshold {MinimumKeyboardHeight(decorView)} px";

	// The window can count a keyboard as showing before the input method has laid out its own window for this input: the window manager's
	// show is still pending (API 36 with a slow input method), and the frame loses the keyboard while the input method lays it out. A test
	// that typed then would read that as a hidden keyboard, so the keyboard only counts as shown once its frame has stayed the same for a while.
	public Task<bool> WaitUntilTheKeyboardShowsSteadilyAsync(int timeout)
	{
		var steadyPolls = 0;
		var previousBottom = bottomWithoutKeyboard;

		return Wait(() =>
		{
			var bottom = Bottom(view);
			steadyPolls = ShowsKeyboardAt(bottom) && bottom == previousBottom ? steadyPolls + 1 : 0;
			previousBottom = bottom;

			return steadyPolls >= steadyPollsBeforeTheKeyboardShows;
		}, timeout, steadyPollInterval);
	}

	public override string ToString() =>
		$"visible frame bottom {Bottom(view)} px, {bottomWithoutKeyboard} px without keyboard, keyboard threshold {minimumKeyboardHeight} px";

	static float MinimumKeyboardHeight(AView view) => minimumKeyboardHeightInDp * (view.Resources?.DisplayMetrics?.Density ?? 1);

	static int Bottom(AView view)
	{
		using var frame = new Android.Graphics.Rect();
		view.GetWindowVisibleDisplayFrame(frame);

		return frame.Bottom;
	}

	static int WindowBottom(AView decorView)
	{
		var location = new int[2];
		decorView.GetLocationOnScreen(location);

		return location[1] + decorView.Height;
	}

	bool ShowsKeyboardAt(int bottom) => bottomWithoutKeyboard - bottom > minimumKeyboardHeight;
}