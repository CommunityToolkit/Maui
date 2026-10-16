using Android.Widget;
using CommunityToolkit.Maui.Core.Platform;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using PlatformView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Clearing the focus of an EditText leaves its soft keyboard open, so the behaviors hide the keyboard themselves. The tests read whether
// the keyboard shows from the visible display frame; the AndroidX window insets (what the toolkit reads) only prepare and clean up.
public abstract partial class KeyboardDismissalTestBase
{
	protected static PlatformView GetPlatformView(InputView input) =>
		(input.Handler as IPlatformViewHandler)?.PlatformView ?? throw new InvalidOperationException("The view has no platform view.");

	protected static void SetPlatformText(PlatformView platformView, string text) => ((EditText)platformView).Text = text;

	protected static bool IsPlatformViewFocused(PlatformView platformView) => platformView.IsFocused;

	protected static void SkipWhenTheSoftKeyboardCannotShow() => SoftKeyboardHelpers.SkipWhenTheSoftKeyboardCannotShow();

	protected static async Task<VisibleFrame> ShowSoftKeyboardAsync(InputView input, PlatformView platformView)
	{
		var frame = await VisibleFrame.RecordWithoutKeyboardAsync(platformView);

		Assert.True(await input.ShowKeyboardAsync(), "ShowKeyboardAsync returned false.");
		Assert.True(await Wait(() => frame.ShowsKeyboard, SoftKeyboardHelpers.Timeout), $"The soft keyboard did not show: {frame}.");
		Assert.True(input.IsFocused, "The view does not have the focus while its soft keyboard shows.");

		return frame;
	}

	protected static async Task AssertSoftKeyboardIsDismissedAsync(VisibleFrame frame, bool isDismissed)
	{
		if (isDismissed)
		{
			Assert.True(await Wait(() => !frame.ShowsKeyboard, SoftKeyboardHelpers.Timeout), $"The soft keyboard is still showing: {frame}.");
		}
		else
		{
			Assert.False(await Wait(() => !frame.ShowsKeyboard, 1000), $"The soft keyboard was hidden: {frame}.");
		}
	}

	protected static Task EnsureSoftKeyboardHiddenAsync() => SoftKeyboardHelpers.EnsureHiddenAsync();

	// A failure here is not this test's: the next test that needs the keyboard hidden reports it.
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			_ = await SoftKeyboardHelpers.HideAsync();
		}
	}
}