using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ToastTests
{
	// ShowPlatform computes the expiration synchronously between the two time stamps a test takes around Show; the margin only absorbs
	// clock reads, far below the 1.5 s between the Short and the Long duration.
	static readonly TimeSpan expirationMargin = TimeSpan.FromMilliseconds(100);

	static async Task ShowAndWaitForDisplayAsync(IToast toast)
	{
		await toast.Show();
		await AssertEventually(async () => await IsDisplayedAsync(), timeout: 10000, message: "Windows does not list the toast that was shown.");
	}

	static async Task<bool> IsDisplayedAsync() => (await GetToastsAsync()).Count > 0;

	static async Task<IReadOnlyList<string>> GetDisplayedTextsAsync() => [.. (await GetToastsAsync()).Select(notification => notification.Text)];

	// Dispose only sets a flag on Windows: nothing has to be taken off screen first.
	static Task TakeTheToastOffScreenBeforeItIsDisposedAsync() => Task.CompletedTask;

	// A Toast is the notification without a button, a Snackbar has one.
	static async Task<IReadOnlyList<WindowsNotification>> GetToastsAsync() => [.. (await AlertsWindows.GetDisplayedAsync()).Where(notification => !notification.IsSnackbar)];

	// A Toast is an app notification too, so it needs the same opt-in as Snackbar.
	[Fact]
	public async Task ShowThrowsWhileTheWindowsOptionIsNotEnabled()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(false));
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.False(Options.ShouldEnableSnackbarOnWindows);

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Toast.Make("Disabled").Show());

			Assert.Contains(nameof(Options.SetShouldEnableSnackbarOnWindows), exception.Message);
			Assert.Equal("https://learn.microsoft.com/dotnet/communitytoolkit/maui/alerts/toast", exception.HelpLink);
			Assert.False(await IsDisplayedAsync(), "A Toast was shown although the Windows option is off.");
		});
	}

	[Theory]
	[InlineData(ToastDuration.Short, false)]
	[InlineData(ToastDuration.Long, true)]
	public async Task ShowBuildsTheNotificationFromTheTextAndTheDuration(ToastDuration duration, bool isLongNotification)
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Toast.Make("Payload text", duration).Show();

			var notification = AlertsWindows.Parse(AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("No notification was shown."));
			Assert.Equal(new[] { "Payload text" }, notification.Texts);
			Assert.Empty(notification.Buttons);
			if (isLongNotification)
			{
				Assert.Equal("long", notification.Duration);
			}
			else
			{
				Assert.NotEqual("long", notification.Duration);
			}
		});
	}

	// The toolkit removes the toast at the end of its Duration (NotificationIsRemovedAtTheEndOfTheDurationBeforeItExpires): the notification
	// expires a minute later, which only takes it out of the Action Center when the app ends before the end of the Duration.
	[Theory]
	[InlineData(ToastDuration.Short, 2.0)]
	[InlineData(ToastDuration.Long, 3.5)]
	public async Task ExpirationIsAMinuteAfterTheEndOfTheDuration(ToastDuration duration, double durationSeconds)
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var before = DateTimeOffset.Now;

			await Toast.Make("Expiration", duration).Show();

			var after = DateTimeOffset.Now;
			var expiration = (AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("No notification was shown.")).Expiration;
			var expected = TimeSpan.FromSeconds(durationSeconds) + TimeSpan.FromMinutes(1);
			Assert.InRange(expiration, before + expected - expirationMargin, after + expected + expirationMargin);
		});
	}

	// Windows shows the banner of a notification for its own time (about 6 s, up to 25 s for a long one) and queues the next notification
	// of the app behind it. Removing the notification takes the banner off screen, but only while the notification has not expired, and an
	// expired notification that waits in the queue is never shown. So the toolkit removes the toast at the end of its Duration, before the
	// notification expires: Windows no longer lists it, and its expiration is still to come.
	[Theory]
	[InlineData(ToastDuration.Short, 2.0)]
	[InlineData(ToastDuration.Long, 3.5)]
	public async Task NotificationIsRemovedAtTheEndOfTheDurationBeforeItExpires(ToastDuration duration, double durationSeconds)
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var shown = DateTimeOffset.Now;
			await ShowAndWaitForDisplayAsync(Toast.Make("Removed at the end of its duration", duration));
			var notification = AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("No notification was shown.");

			await AssertEventually(async () => !await IsDisplayedAsync(), timeout: 10000, interval: 50, message: "Windows still lists the toast after the end of its duration.");

			var removed = DateTimeOffset.Now;
			Assert.True(removed >= shown + TimeSpan.FromSeconds(durationSeconds) - expirationMargin, $"The toast was gone {removed - shown} after Show, before the end of its duration.");
			Assert.True(removed < notification.Expiration, $"The toast expired at {notification.Expiration:O} instead of being removed before that (gone at {removed:O}): Windows keeps the banner of an expired notification on screen.");
		});
	}

	[Fact]
	public async Task RemovalAtTheEndOfTheDurationKeepsTheOtherNotificationsOfTheApp()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Removed at the end of its duration"));
			AppNotificationManager.Default.Show(new AppNotificationBuilder().AddText("Another notification of the app").BuildNotification());

			await AssertEventually(async () => (await GetDisplayedTextsAsync()) is ["Another notification of the app"], timeout: 10000, message: "Windows does not list only the other notification of the app after the end of the duration of the toast.");
		});
	}

	// Nothing in the Windows Toast needs the UI thread: the Windows App SDK notification APIs can be called from any thread.
	[Fact]
	public async Task ShowOffTheUiThreadShowsTheNotification()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await Task.Run(() => Toast.Make("Off the UI thread", ToastDuration.Long).Show());

		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.NotNull(AlertsWindows.GetPlatformToast());
			await AssertEventually(async () => (await GetDisplayedTextsAsync()) is ["Off the UI thread"], timeout: 10000, message: "Windows does not list the toast that was shown from a background thread.");
		});
	}

	[Fact]
	public async Task DismissOffTheUiThreadExpiresTheNotification()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		var toast = Toast.Make("Dismissed off the UI thread", ToastDuration.Long);
		await InvokeOnMainThreadAsync(() => ShowAndWaitForDisplayAsync(toast));
		var notification = AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("No notification was shown.");
		var before = DateTimeOffset.Now;

		await Task.Run(() => toast.Dismiss()).WaitAsync(TimeSpan.FromSeconds(10));

		var after = DateTimeOffset.Now;
		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.Null(AlertsWindows.GetPlatformToast());
			Assert.InRange(notification.Expiration, before - expirationMargin, after + expirationMargin);
			await AssertEventually(async () => !await IsDisplayedAsync(), timeout: 10000, message: "Windows still lists the toast that was dismissed from a background thread.");
		});
	}

	// The end of its Duration removes "First" a moment before "Second", so Windows briefly lists only "Second" even when "First" outlived
	// the removal by the second Show: the list is read as soon as "Second" appears.
	[Fact]
	public async Task ShowAgainExpiresTheOldNotificationAndKeepsOnlyTheNewOne()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Toast.Make("First", ToastDuration.Long).Show();
			var first = AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("The first notification was not shown.");
			var before = DateTimeOffset.Now;

			await Toast.Make("Second", ToastDuration.Long).Show();

			var after = DateTimeOffset.Now;
			var second = AlertsWindows.GetPlatformToast();
			Assert.NotNull(second);
			Assert.NotSame(first, second);
			Assert.InRange(first.Expiration, before - expirationMargin, after + expirationMargin);
			await AssertEventually(async () => (await GetDisplayedTextsAsync()).Contains("Second"), timeout: 10000, message: "Windows does not list the second toast.");
			Assert.Equal(new[] { "Second" }, await GetDisplayedTextsAsync());
		});
	}

	// The displayed toast gives both Show calls a notification to remove, so the second Show starts while the first one waits for that
	// removal, as when PopupsPage shows "Popup Opened" right after "Opening Popup". Had the second Show removed the notifications before
	// the first one showed its own, the second toast would wait behind the banner of the first one.
	[Fact]
	public async Task OverlappingShowCallsShowOnlyTheLaterToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Displayed", ToastDuration.Long));

			var first = Toast.Make("First", ToastDuration.Long).Show();
			var second = Toast.Make("Second", ToastDuration.Long).Show();
			await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));

			var platformToast = AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("No notification was shown.");
			Assert.Equal(new[] { "Second" }, AlertsWindows.Parse(platformToast).Texts);
			await AssertEventually(async () => (await GetDisplayedTextsAsync()).Contains("Second"), timeout: 10000, message: "Windows does not list the toast of the later Show.");
			Assert.Equal(new[] { "Second" }, await GetDisplayedTextsAsync());
		});
	}

	// The Dismiss starts while the Show waits for the removal of the displayed toast. The notification shown last tells when Windows lists
	// what was shown before it, such as a toast that the Show displayed after the Dismiss had removed the notifications.
	[Fact]
	public async Task DismissDuringAShowRemovesTheToastOfThatShow()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Displayed", ToastDuration.Long));

			var show = Toast.Make("Shown", ToastDuration.Long).Show();
			var dismiss = Toast.Make("Other instance").Dismiss();
			await Task.WhenAll(show, dismiss).WaitAsync(TimeSpan.FromSeconds(10));

			Assert.Null(AlertsWindows.GetPlatformToast());
			AppNotificationManager.Default.Show(new AppNotificationBuilder().AddText("Shown after the Dismiss").BuildNotification());
			await AssertEventually(async () => (await GetDisplayedTextsAsync()).Contains("Shown after the Dismiss"), timeout: 10000, message: "Windows does not list the notification shown after the Dismiss.");
			Assert.Equal(new[] { "Shown after the Dismiss" }, await GetDisplayedTextsAsync());
		});
	}

	// The displayed toast keeps the first Show waiting for its removal, so the token of the second Show is cancelled before the second Show
	// can remove anything.
	[Fact]
	public async Task ShowCancelledWhileAnEarlierShowRunsThrowsAndKeepsTheEarlierToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Displayed", ToastDuration.Long));
			using var cancellationTokenSource = new CancellationTokenSource();

			var first = Toast.Make("First", ToastDuration.Long).Show();
			var second = Toast.Make("Second", ToastDuration.Long).Show(cancellationTokenSource.Token);
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.WaitAsync(TimeSpan.FromSeconds(10)));
			await first.WaitAsync(TimeSpan.FromSeconds(10));
			var platformToast = AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("No notification was shown.");
			Assert.Equal(new[] { "First" }, AlertsWindows.Parse(platformToast).Texts);
			await AssertEventually(async () => (await GetDisplayedTextsAsync()).Contains("First"), timeout: 10000, message: "Windows does not list the toast of the first Show.");
			Assert.Equal(new[] { "First" }, await GetDisplayedTextsAsync());
		});
	}

	// The displayed toast keeps the first Show waiting for its removal, so the second Show waits for its turn. The first Show hands it the
	// turn as it completes, and only then is the token cancelled: SemaphoreSlim.WaitAsync returns the turn instead of throwing.
	[Fact]
	public async Task ShowCancelledOnceItsTurnCameThrowsAndKeepsTheEarlierToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Displayed", ToastDuration.Long));
			using var cancellationTokenSource = new CancellationTokenSource();

			var first = Toast.Make("First", ToastDuration.Long).Show();
			var second = Toast.Make("Second", ToastDuration.Long).Show(cancellationTokenSource.Token);
			await first.WaitAsync(TimeSpan.FromSeconds(10));
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.WaitAsync(TimeSpan.FromSeconds(10)));
			var platformToast = AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("The cancelled Show removed the toast of the first Show.");
			Assert.Equal(new[] { "First" }, AlertsWindows.Parse(platformToast).Texts);
			await AssertEventually(async () => (await GetDisplayedTextsAsync()).Contains("First"), timeout: 10000, message: "Windows does not list the toast of the first Show.");
			Assert.Equal(new[] { "First" }, await GetDisplayedTextsAsync());
		});
	}

	// As in ShowCancelledOnceItsTurnCameThrowsAndKeepsTheEarlierToast, the token is cancelled after the Show handed the Dismiss its turn.
	[Fact]
	public async Task DismissCancelledOnceItsTurnCameThrowsAndKeepsTheToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Displayed", ToastDuration.Long));
			using var cancellationTokenSource = new CancellationTokenSource();

			var show = Toast.Make("Shown", ToastDuration.Long).Show();
			var dismiss = Toast.Make("Other instance").Dismiss(cancellationTokenSource.Token);
			await show.WaitAsync(TimeSpan.FromSeconds(10));
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dismiss.WaitAsync(TimeSpan.FromSeconds(10)));
			var platformToast = AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("The cancelled Dismiss removed the toast.");
			Assert.Equal(new[] { "Shown" }, AlertsWindows.Parse(platformToast).Texts);
			await AssertEventually(async () => (await GetDisplayedTextsAsync()).Contains("Shown"), timeout: 10000, message: "Windows does not list the toast of the Show.");
			Assert.Equal(new[] { "Shown" }, await GetDisplayedTextsAsync());
		});
	}

	[Fact]
	public async Task DismissExpiresTheNotificationAndClearsThePlatformToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = Toast.Make("Dismissed", ToastDuration.Long);
			await ShowAndWaitForDisplayAsync(toast);
			var notification = AlertsWindows.GetPlatformToast() ?? throw new InvalidOperationException("No notification was shown.");
			var before = DateTimeOffset.Now;

			await toast.Dismiss();

			var after = DateTimeOffset.Now;
			Assert.Null(AlertsWindows.GetPlatformToast());
			Assert.InRange(notification.Expiration, before - expirationMargin, after + expirationMargin);
			await AssertEventually(async () => !await IsDisplayedAsync(), timeout: 10000, message: "Windows still lists the dismissed toast.");
		});
	}

	// The second Dismiss waits for the removal that the first one started, and then finds no toast left to remove.
	[Fact]
	public async Task ConcurrentDismissCallsBothComplete()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Toast.Make("Shown", ToastDuration.Long).Show();

			var first = Toast.Make("First dismiss").Dismiss();
			var second = Toast.Make("Second dismiss").Dismiss();
			await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));

			Assert.Null(AlertsWindows.GetPlatformToast());
		});
	}

	[Fact]
	public async Task DisposeLeavesTheNotificationInPlace()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = Toast.Make("Disposed", ToastDuration.Long);
			await ShowAndWaitForDisplayAsync(toast);
			var notification = AlertsWindows.GetPlatformToast();

			toast.Dispose();
			toast.Dispose();

			Assert.Same(notification, AlertsWindows.GetPlatformToast());
			Assert.Equal(new[] { "Disposed" }, await GetDisplayedTextsAsync());
		});
	}
}