using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using CoreText;
using Foundation;
using UIKit;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ToastTests
{
	static async Task ShowAndWaitForDisplayAsync(IToast toast)
	{
		await toast.Show();
		Assert.NotEmpty(GetToastAlertViews());
	}

	static Task<bool> IsDisplayedAsync() => Task.FromResult(GetToastAlertViews().Count > 0);

	static Task<IReadOnlyList<string>> GetDisplayedTextsAsync() =>
		Task.FromResult<IReadOnlyList<string>>([.. GetToastAlertViews().Select(alertView => alertView.GetLabel().Text ?? string.Empty)]);

	// Disposing a toast that is displayed only disposes the managed wrapper of its label here: nothing has to be taken off screen first.
	static Task TakeTheToastOffScreenBeforeItIsDisposedAsync() => Task.CompletedTask;

	[Fact]
	public async Task ShowAddsOneAlertViewWithAMessageLabelAndNoActionButton()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Toast.Make("Structure").Show();

			var alertView = AlertsApple.GetSingleAlertView();
			Assert.Same(AlertsApple.KeyWindow, alertView.Superview);
			Assert.IsType<PaddedLabel>(Assert.Single(alertView.Children));
			Assert.Equal("Structure", alertView.GetLabel().Text);
			Assert.Null(alertView.GetActionButton());
		});
	}

	[Fact]
	public async Task ToastUsesTheAlertDefaultsForColorsCornersAndPadding()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Toast.Make("Defaults").Show();

			var alertView = AlertsApple.GetSingleAlertView();
			var label = alertView.GetLabel();
			AlertsApple.AssertColor(211 / 255d, 211 / 255d, 211 / 255d, 1, alertView.GetBackground().BackgroundColor);
			AlertsApple.AssertColor(0, 0, 0, 1, label.TextColor);
			Assert.Equal(14, (double)label.Font.PointSize);
			Assert.Equal(new CGRect(4, 4, 4, 4), alertView.VisualOptions.CornerRadius);
			Assert.Equal(4, (double)alertView.GetBackground().LeftPadding);
			Assert.Equal(14, (double)label.LeftPadding);
			Assert.Equal(14, (double)label.BottomPadding);
			Assert.Equal(0, (int)label.Lines);
			var attributes = label.AttributedText!.GetCoreTextAttributes(0, out _);
			Assert.Equal(0, attributes!.KerningAdjustment!.Value, 3);
		});
	}

	[Fact]
	public async Task TextSizeSetsTheFontSizeOfTheMessage()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Toast.Make("Big", ToastDuration.Short, 31).Show();

			Assert.Equal(31, (double)AlertsApple.GetSingleAlertView().GetLabel().Font.PointSize);
		});
	}

	// The duration of the toast is the interval of the NSTimer that hides it: Short is 2 s, Long 3.5 s. The fire date of the timer pins the
	// interval in both directions (the tests that wait for the toast to go away allow for a busy main thread).
	[Theory]
	[InlineData(ToastDuration.Short, 2.0)]
	[InlineData(ToastDuration.Long, 3.5)]
	public async Task DurationIsHandedToThePlatformToast(ToastDuration duration, double expectedSeconds)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var before = NSDate.Now.SecondsSinceReferenceDate;
			await Toast.Make("Duration", duration).Show();
			var after = NSDate.Now.SecondsSinceReferenceDate;

			var platformToast = Assert.IsType<PlatformToast>(GetPlatformToast(), exactMatch: false);
			Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), platformToast.Duration);
			AlertsApple.AssertTimerFiresAfterTheDuration(platformToast, TimeSpan.FromSeconds(expectedSeconds), before, after);
		});
	}

	[Fact]
	public async Task ToastIsCenteredAtTheBottomOfTheSafeAreaAndDoesNotFillTheWidth()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Toast.Make("Centered").Show();

			var alertView = AlertsApple.GetSingleAlertView();
			await AlertsApple.WaitForLayoutAsync(alertView);
			var safeArea = AlertsApple.KeyWindow.SafeAreaLayoutGuide.LayoutFrame;
			Assert.True(alertView.Frame.Width < safeArea.Width - 20, $"The toast is {alertView.Frame.Width} wide in a safe area of {safeArea.Width}.");
			AlertsApple.AssertClose(safeArea.GetMidX(), alertView.Frame.GetMidX());
			AlertsApple.AssertClose(safeArea.Bottom - 10, alertView.Frame.Bottom);
		});
	}

	// UIKit only accepts calls from the main thread (the binding checks it in Debug builds): Show called from a background thread fails
	// before anything is created or shown.
	[Fact]
	public async Task ShowOffTheUiThreadThrowsUIKitThreadAccessExceptionAndShowsNothing()
	{
		Skip.IfNot(UIApplication.CheckForIllegalCrossThreadCalls, "UIKit does not check the thread of its callers in this build.");
		SetupBuilder();

		await Assert.ThrowsAsync<UIKitThreadAccessException>(() => Task.Run(() => Toast.Make("Off the UI thread").Show()));

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(GetPlatformToast());
			Assert.Empty(AlertsApple.GetAlertViews());
		});
	}

	// Dismiss fails the same way off the UI thread (Alert.Dismiss removes the view through UIKit), and the toast stays.
	[Fact]
	public async Task DismissOffTheUiThreadThrowsUIKitThreadAccessExceptionAndKeepsTheToast()
	{
		Skip.IfNot(UIApplication.CheckForIllegalCrossThreadCalls, "UIKit does not check the thread of its callers in this build.");
		SetupBuilder();
		var toast = Toast.Make("Stays", ToastDuration.Long);
		await InvokeOnMainThreadAsync(() => ShowAndWaitForDisplayAsync(toast));

		await Assert.ThrowsAsync<UIKitThreadAccessException>(() => Task.Run(() => toast.Dismiss()));

		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.Equal(new[] { "Stays" }, await GetDisplayedTextsAsync());
			Assert.NotNull(GetPlatformToast());
		});
	}

	// Show dismisses the toast that is displayed before it shows the next one: the first one is gone at once, not once its own timer fires.
	[Fact]
	public async Task ShowRemovesTheDisplayedToastBeforeItShowsTheNextOne()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Toast.Make("First", ToastDuration.Long).Show();
			var first = Assert.IsType<PlatformToast>(GetPlatformToast(), exactMatch: false);
			var firstAlertView = Assert.Single(GetToastAlertViews());

			await Toast.Make("Second", ToastDuration.Long).Show();

			Assert.Null(firstAlertView.Superview);
			Assert.Null(AlertsApple.GetTimer(first));
			Assert.Equal(new[] { "Second" }, await GetDisplayedTextsAsync());
		});
	}

	[Fact]
	public async Task EmptyTextDisplaysAnEmptyLabel()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Toast.Make(string.Empty).Show();

			Assert.Equal(string.Empty, AlertsApple.GetSingleAlertView().GetLabel().Text);
		});
	}

	// The window tells the two durations apart (Long is 3.5 s); the extra time allows for a busy main thread.
	[Fact]
	public async Task ShortToastHidesItselfAfterAboutTwoSeconds()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var stopwatch = Stopwatch.StartNew();
			await Toast.Make("Short", ToastDuration.Short).Show();

			await AssertEventually(() => GetToastAlertViews().Count == 0, timeout: 10000, message: "The short toast never hid itself.");
			stopwatch.Stop();

			Assert.InRange(stopwatch.Elapsed.TotalSeconds, 1.9, 3.2);
		});
	}

	[Fact]
	public async Task LongToastHidesItselfAfterAboutThreeAndAHalfSeconds()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var stopwatch = Stopwatch.StartNew();
			await Toast.Make("Long", ToastDuration.Long).Show();

			await AssertEventually(() => GetToastAlertViews().Count == 0, timeout: 15000, message: "The long toast never hid itself.");
			stopwatch.Stop();

			Assert.InRange(stopwatch.Elapsed.TotalSeconds, 3.4, 8);
		});
	}

	[Fact]
	public async Task DismissLeavesTheSnackbarThatIsShownAlone()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Snackbar stays", () => { }, "Undo", TimeSpan.FromSeconds(30)).Show();
			await Toast.Make("Toast goes", ToastDuration.Long).Show();

			await Toast.Make("Other").Dismiss();

			Assert.Equal(new[] { "Snackbar stays" }, AlertsApple.GetAlertViews().Select(alertView => alertView.GetLabel().Text).ToArray());
		});
	}

	[Fact]
	public async Task ToastDoesNotLeakOnceAnotherToastHasReplacedIt()
	{
		SetupBuilder();
		WeakReference toastReference = null!, platformReference = null!, alertViewReference = null!;

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = new Toast { Text = "Leak", Duration = ToastDuration.Long };
			await toast.Show();
			toastReference = new WeakReference(toast);
			platformReference = new WeakReference(GetPlatformToast());
			alertViewReference = new WeakReference(AlertsApple.GetSingleAlertView());

			await toast.Dismiss();

			// The last toast stays referenced by the static platform toast, so show another one.
			await Toast.Make("Replacement", ToastDuration.Long).Show();
		});

		await AssertionExtensions.WaitForGC(toastReference, platformReference, alertViewReference);
	}

	// Toast.Dispose(true) disposes the platform toast, whose only visible trace is its own disposed state: the native label stays in the
	// AlertView (UIKit retains it), so the tests read PlatformToast's isDisposed flag.
	[Fact]
	public async Task DisposeDisposesThePlatformToast()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = new Toast { Text = "Dispose", Duration = ToastDuration.Long };
			await toast.Show();
			var platformToast = Assert.IsType<PlatformToast>(GetPlatformToast(), exactMatch: false);
			Assert.False(AlertsApple.IsDisposed(platformToast));

			toast.Dispose();

			Assert.True(AlertsApple.IsDisposed(platformToast));
		});
	}

	[Fact]
	public async Task DisposeWithoutDisposingManagedStateKeepsThePlatformToast()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = new DisposableToast { Text = "Finalizer path", Duration = ToastDuration.Long };
			await toast.Show();
			var platformToast = Assert.IsType<PlatformToast>(GetPlatformToast(), exactMatch: false);

			toast.CallDispose(false);

			Assert.False(AlertsApple.IsDisposed(platformToast));
		});
	}

	[Fact]
	public async Task SecondDisposeOfTheSameToastDoesNotDisposeALaterPlatformToast()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = new Toast { Text = "First", Duration = ToastDuration.Long };
			await first.Show();
			first.Dispose();
			await new Toast { Text = "Second", Duration = ToastDuration.Long }.Show();
			var laterPlatformToast = Assert.IsType<PlatformToast>(GetPlatformToast(), exactMatch: false);

			first.Dispose();

			Assert.False(AlertsApple.IsDisposed(laterPlatformToast));
			Assert.Equal(new[] { "Second" }, await GetDisplayedTextsAsync());
		});
	}

	// A Toast is the AlertView without an action button; a Snackbar has one.
	static List<AlertView> GetToastAlertViews() => [.. AlertsApple.GetAlertViews().Where(alertView => alertView.GetActionButton() is null)];

	// A subclass can reach the protected Dispose(bool): a Toast has no finalizer, so the false overload is otherwise unreachable.
	sealed class DisposableToast : Toast
	{
		public void CallDispose(bool isDisposing) => Dispose(isDisposing);
	}
}