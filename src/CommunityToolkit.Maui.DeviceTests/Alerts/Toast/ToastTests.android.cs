using System.Diagnostics;
using System.Reflection;
using Android.Widget;
using CommunityToolkit.Maui.Core;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using Toast = CommunityToolkit.Maui.Alerts.Toast;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ToastTests
{
	protected override bool ObservesToasts => true;

	async Task ShowAndWaitForDisplayAsync(IToast toast)
	{
		await toast.Show();
		await ToastObserver.WaitForDisplayAsync(GetNativeToast());
	}

	Task<bool> IsDisplayedAsync() => Task.FromResult(ToastObserver.Toasts.Any(toast => toast.IsDisplayed));

	Task<IReadOnlyList<string>> GetDisplayedTextsAsync() =>
		Task.FromResult<IReadOnlyList<string>>([.. ToastObserver.Toasts.Where(toast => toast.IsDisplayed).Select(toast => toast.Event?.Text ?? string.Empty)]);

	// A toast whose Java peer is disposed while it is displayed can no longer be cancelled: the system keeps it until it times out and
	// shows every later toast after it (see ToastObserver.WaitForToastsThatCannotBeCancelledAsync). So the tests cancel it natively first.
	async Task TakeTheToastOffScreenBeforeItIsDisposedAsync()
	{
		GetNativeToast().Cancel();
		await AssertEventually(() => !ToastObserver.Toasts.Any(toast => toast.IsDisplayed), timeout: 10000, message: "The toast did not go away before it was disposed.");
	}

	[Theory]
	[InlineData(ToastDuration.Short, ToastLength.Short)]
	[InlineData(ToastDuration.Long, ToastLength.Long)]
	public async Task DurationIsAppliedToThePlatformToast(ToastDuration duration, ToastLength expectedLength)
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Duration", duration));

			Assert.Equal(expectedLength, GetNativeToast().Duration);
		});
	}

	[Fact]
	public async Task TextAndPackageAreReportedBySystem()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("System text"));

			var toastEvent = Assert.Single(ToastObserver.Toasts).Event!;
			Assert.Equal("System text", toastEvent.Text);
			Assert.Equal(AlertsAndroid.Activity.PackageName, toastEvent.PackageName);
		});
	}

	[Theory]
	[InlineData(14.0)]
	[InlineData(22.0)]
	[InlineData(9.9)]
	public async Task TextSizeIsAppliedInWholeDipToTheWholeText(double textSize)
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Sized text", ToastDuration.Short, textSize));

			var sizeSpan = Assert.Single(Assert.Single(ToastObserver.Toasts).Event!.SizeSpans);
			Assert.Equal((int)textSize, sizeSpan.Size);
			Assert.True(sizeSpan.IsDip);
			Assert.Equal(0, sizeSpan.Start);
			Assert.Equal("Sized text".Length, sizeSpan.End);
		});
	}

	[Fact]
	public async Task EmptyTextIsDisplayedWithoutAnError()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var exception = await Record.ExceptionAsync(() => ShowAndWaitForDisplayAsync(Toast.Make(string.Empty)));

			Assert.Null(exception);
			Assert.Equal(string.Empty, Assert.Single(ToastObserver.Toasts).Event!.Text);
		});
	}

	[Fact]
	public async Task ShowCreatesANewPlatformToastEveryTimeAndCancelsTheOldOne()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("First", ToastDuration.Long));
			await ShowAndWaitForDisplayAsync(Toast.Make("Second", ToastDuration.Long));

			var first = ToastObserver.Toasts[0];
			var second = ToastObserver.Toasts[1];
			Assert.NotEqual(first.Platform.Handle, second.Platform.Handle);
			await AssertEventually(() => !first.IsDisplayed && second.IsDisplayed, timeout: 10000, message: "The first toast was not cancelled by the second.");
		});
	}

	// The system queues a toast behind the one that is displayed and shows it once that one has timed out (3.5 s for ToastLength.Long), so
	// a cancelled toast and one that timed out look the same once both are gone: the time it takes the next toasts to appear tells them
	// apart. Without the Cancel in Show, the second and the third toast would each wait about 3.5 s.
	[Fact]
	public async Task ShowCancelsTheShownToastSoThatTheNextToastAppearsAtOnce()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("One", ToastDuration.Long));

			var stopwatch = Stopwatch.StartNew();
			await ShowAndWaitForDisplayAsync(Toast.Make("Two", ToastDuration.Long));
			await ShowAndWaitForDisplayAsync(Toast.Make("Three", ToastDuration.Long));
			stopwatch.Stop();

			Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"The next two toasts took {stopwatch.Elapsed} to appear: the shown toast was not cancelled, so each one waited for the toast before it to time out.");
		});
	}

	[Fact]
	public async Task DisposeReleasesThePlatformToastPeer()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = new Toast { Text = "Dispose", Duration = ToastDuration.Long };
			await ShowAndWaitForDisplayAsync(toast);
			var platformToast = GetNativeToast();
			Assert.NotEqual(IntPtr.Zero, platformToast.Handle);
			await TakeTheToastOffScreenBeforeItIsDisposedAsync();

			toast.Dispose();

			Assert.Equal(IntPtr.Zero, platformToast.Handle);
		});
	}

	[Fact]
	public async Task DisposeWithoutDisposingManagedStateKeepsThePlatformToastPeer()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = new DisposableToast { Text = "Finalizer path", Duration = ToastDuration.Long };
			await ShowAndWaitForDisplayAsync(toast);
			var platformToast = GetNativeToast();

			toast.CallDispose(false);

			Assert.NotEqual(IntPtr.Zero, platformToast.Handle);
		});
	}

	[Fact]
	public async Task SecondDisposeOfTheSameToastDoesNotDisposeALaterPlatformToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = new Toast { Text = "First", Duration = ToastDuration.Long };
			await ShowAndWaitForDisplayAsync(first);
			await TakeTheToastOffScreenBeforeItIsDisposedAsync();
			first.Dispose();

			// A later toast: put a platform toast of its own where Show would have left it.
			var laterPlatformToast = Android.Widget.Toast.MakeText(AlertsAndroid.Activity, "Later", ToastLength.Long)!;
			SetPlatformToast(laterPlatformToast);

			first.Dispose();

			Assert.NotEqual(IntPtr.Zero, laterPlatformToast.Handle);
		});
	}

	// The next toast replaced (cancelled) the toast of the first instance, which is disposed afterwards: only its own platform toast goes,
	// so the toast that is displayed stays the toolkit's platform toast and its Dismiss cancels it. The system would hide it by itself
	// after 3.5 s (ToastLength.Long): Dismiss has to hide it well before.
	[Fact]
	public async Task DisposingAnOlderToastKeepsTheDisplayedToastDismissable()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = Toast.Make("First", ToastDuration.Long);
			await ShowAndWaitForDisplayAsync(first);
			var second = Toast.Make("Second", ToastDuration.Long);
			await ShowAndWaitForDisplayAsync(second);
			var secondPlatformToast = GetNativeToast();

			first.Dispose();

			Assert.Same(secondPlatformToast, GetPlatformToast());
			Assert.NotEqual(IntPtr.Zero, secondPlatformToast.Handle);

			var stopwatch = Stopwatch.StartNew();
			await second.Dismiss();
			await AssertEventually(async () => !await IsDisplayedAsync(), timeout: 10000, message: "The displayed toast stayed on screen after its Dismiss.");
			stopwatch.Stop();
			Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"The displayed toast went away {stopwatch.Elapsed} after its Dismiss: it was not cancelled but timed out.");
		});
	}

	// Without an activity, Show passes a null context to Toast.makeText, whose constructor reads the package name of the context.
	[Fact]
	public async Task ShowWithoutACurrentActivityThrowsAndDisplaysNothing()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			ResetToolkitAlertState();
			var eventsBefore = ToastObserver.Events.Count;

			using (AlertsAndroid.WithoutCurrentActivity())
			{
				await Assert.ThrowsAsync<Java.Lang.NullPointerException>(() => Toast.Make("No activity").Show());
			}

			Assert.Null(GetPlatformToast());
			Assert.Equal(eventsBefore, ToastObserver.Events.Count);
		});
	}

	// Android only lets a thread with a Looper (the UI thread) create a toast: Show called from a background thread fails before the
	// toolkit stores a platform toast. Android 11 and later report it with a NullPointerException, earlier versions with a RuntimeException.
	[Fact]
	public async Task ShowOffTheUiThreadThrowsBecauseTheThreadHasNoLooper()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		var exception = await Assert.ThrowsAnyAsync<Java.Lang.RuntimeException>(() => Task.Run(() => Toast.Make("Off the UI thread").Show()));

		Assert.Contains("Looper.prepare()", exception.Message);
		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(GetPlatformToast());
		});
	}

	// Dismiss, unlike Show, works off the UI thread: Toast.cancel() reaches the system's notification service (Android 11 and later) or
	// posts to the handler of the toast's own looper (earlier versions).
	[Fact]
	public async Task DismissOffTheUiThreadCancelsTheToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		var toast = Toast.Make("Cancelled off the UI thread", ToastDuration.Long);
		await InvokeOnMainThreadAsync(() => ShowAndWaitForDisplayAsync(toast));

		await Task.Run(() => toast.Dismiss());

		await InvokeOnMainThreadAsync(() => AssertEventually(async () => !await IsDisplayedAsync(), timeout: 10000, message: "The toast dismissed from a background thread stayed on screen."));
	}

	[Fact]
	public async Task ToastDoesNotLeakOnceAnotherToastHasReplacedIt()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		WeakReference toastReference = null!, platformReference = null!;

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = new Toast { Text = "Leak", Duration = ToastDuration.Long };
			await toast.Show();
			toastReference = new WeakReference(toast);
			platformReference = new WeakReference(GetPlatformToast());
			await toast.Dismiss();

			// The last toast stays referenced by the static platform toast, so show another one.
			await Toast.Make("Replacement", ToastDuration.Long).Show();
		});

		await AssertionExtensions.WaitForGC(toastReference, platformReference);
	}

	static void SetPlatformToast(Android.Widget.Toast? platformToast) =>
		typeof(Toast).GetProperty("PlatformToast", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, platformToast);

	static Android.Widget.Toast GetNativeToast() => GetPlatformToast() as Android.Widget.Toast ?? throw new InvalidOperationException("The toolkit has no platform toast.");

	// A subclass can reach the protected Dispose(bool): a Toast has no finalizer, so the false overload is otherwise unreachable.
	sealed class DisposableToast : Toast
	{
		public void CallDispose(bool isDisposing) => Dispose(isDisposing);
	}
}