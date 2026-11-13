#if ANDROID
using Android.Widget;
using AView = Android.Views.View;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// At the app root because tests of several folders use these helpers.
static class FocusHelpers
{
	// Dispose it after AttachAndRun: removing it while the hosted view is still in the window hands the focus back to that view.
	public static async Task<IAsyncDisposable> AddClearedFocusTargetAsync()
	{
#if ANDROID
		return await ClearedFocusTarget.AddAsync();
#else
		await Task.CompletedTask;
		return NoClearedFocusTarget.Instance;
#endif
	}

#if ANDROID
	sealed class ClearedFocusTarget(AView view, FrameLayout content) : IAsyncDisposable
	{
		public static Task<ClearedFocusTarget> AddAsync() => TestDispatcher.Current.DispatchAsync(() =>
		{
			var content = SoftKeyboardHelpers.RunnerActivity.FindViewById<FrameLayout>(Android.Resource.Id.Content)
				?? throw new InvalidOperationException("The content of the runner's window is not available.");

			// Before Android 9, and outside touch mode from it, View.clearFocus hands the focus to the first focusable view of the window.
			// MAUI deliberately has no focusable page layout to take it (dotnet/maui#11881), and index 0 puts this view before the hosted input.
			var view = new AView(SoftKeyboardHelpers.RunnerActivity) { Focusable = true, FocusableInTouchMode = true };
			content.AddView(view, 0, new FrameLayout.LayoutParams(1, 1));

			return new ClearedFocusTarget(view, content);
		});

		public async ValueTask DisposeAsync() => await TestDispatcher.Current.DispatchAsync(() =>
		{
			content.RemoveView(view);
			view.Dispose();
		});
	}
#else
	sealed class NoClearedFocusTarget : IAsyncDisposable
	{
		public static NoClearedFocusTarget Instance { get; } = new();

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
#endif
}
