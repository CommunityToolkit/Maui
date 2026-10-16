using CommunityToolkit.Maui.Alerts;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class SnackbarTests
{
	const string documentationUrl = "https://learn.microsoft.com/dotnet/communitytoolkit/maui/alerts/snackbar";

	// ShowPlatform computes the expiration synchronously between the two time stamps a test takes around Show; the margin only absorbs
	// clock reads.
	static readonly TimeSpan expirationMargin = TimeSpan.FromMilliseconds(100);

	static string? ActionCannotBeClickedReason =>
		"The button of a Windows app notification can only be pressed by a person (see PressingTheButtonOfTheNotificationRunsTheActionOnTheUiThreadAndDismissesTheSnackbar, a RequiresUserInteraction test, and the PressOfTheButton* tests, which hand the toolkit a press as NotificationInvoked does).";

	// Dispose only sets a flag on Windows: nothing has to be taken off screen first.
	static Task TakeTheSnackbarOffScreenBeforeItIsDisposedAsync() => Task.CompletedTask;

	static async Task<IReadOnlyList<string>> GetVisibleMessagesAsync() =>
		[.. (await AlertsWindows.GetDisplayedAsync()).Where(notification => notification.IsSnackbar).Select(notification => notification.Text)];

	static async Task<IReadOnlyList<string>> GetVisibleActionTextsAsync() =>
		[.. (await AlertsWindows.GetDisplayedAsync()).Where(notification => notification.IsSnackbar).Select(notification => Assert.Single(notification.Buttons).Content)];

	static Task<bool> ClickVisibleActionAsync() => throw new SkipException(ActionCannotBeClickedReason!);

	[Fact]
	public async Task ShowBuildsTheNotificationFromTheTextAndTheActionButton()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Payload text", () => { }, "Undo", longDuration).Show();

			var notification = AlertsWindows.Parse(AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown."));
			Assert.Equal(new[] { "Payload text" }, notification.Texts);
			var button = Assert.Single(notification.Buttons);
			Assert.Equal("Undo", button.Content);
			Assert.True(Guid.TryParse(button.SnackbarIdentifier, out _), $"The button has no snackbar identifier: {button.Arguments}");
		});
	}

	[Fact]
	public async Task ShowRegistersTheActionUnderTheIdentifierOfTheButton()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			Action action = () => { };

			await Snackbar.Make("Action", action, "Undo", longDuration).Show();

			var button = Assert.Single(AlertsWindows.Parse(AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown.")).Buttons);
			var registered = Assert.Single(AlertsWindows.GetRegisteredActions());
			Assert.Equal(button.SnackbarIdentifier, registered.Key);
			Assert.Same(action, registered.Value);
		});
	}

	[Fact]
	public async Task ShowWithoutAnActionRegistersNoActionForTheButton()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("No action", duration: longDuration).Show();

			var registered = Assert.Single(AlertsWindows.GetRegisteredActions());
			Assert.Null(registered.Value);
		});
	}

	[Fact]
	public async Task ExpirationIsTheTimeOfShowPlusTheDuration()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var duration = TimeSpan.FromSeconds(45);
			var before = DateTimeOffset.Now;

			await Snackbar.Make("Expiration", duration: duration).Show();

			var after = DateTimeOffset.Now;
			var expiration = (AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown.")).Expiration;
			Assert.InRange(expiration, before + duration - expirationMargin, after + duration + expirationMargin);
		});
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-2)]
	public async Task ZeroOrNegativeDurationSetsTheExpirationToTheTimeOfShow(int durationMilliseconds)
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var duration = TimeSpan.FromMilliseconds(durationMilliseconds);
			var before = DateTimeOffset.Now;

			await Snackbar.Make("Expires at once", duration: duration).Show();

			var after = DateTimeOffset.Now;
			var expiration = (AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown.")).Expiration;
			Assert.InRange(expiration, before + duration - expirationMargin, after + duration + expirationMargin);
		});
	}

	// Android hides the action and iOS and Mac Catalyst keep an empty, padded button for this input (SnackbarTests.EmptyActionButtonText*):
	// the platforms differ.
	[Fact]
	public async Task EmptyActionButtonTextBuildsAButtonWithoutContent()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			Action action = () => { };

			await Snackbar.Make("No action text", action, string.Empty, longDuration).Show();

			var notification = AlertsWindows.Parse(AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown."));
			Assert.Equal(new[] { "No action text" }, notification.Texts);
			var button = Assert.Single(notification.Buttons);
			Assert.Equal(string.Empty, button.Content);
			var registered = Assert.Single(AlertsWindows.GetRegisteredActions());
			Assert.Equal(button.SnackbarIdentifier, registered.Key);
			Assert.Same(action, registered.Value);
		});
	}

	// The default duration (3 s) comes from the constructor for an initializer and from Make for Make.
	[Fact]
	public async Task DefaultDurationOfThreeSecondsSetsTheExpiration()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var defaultDuration = TimeSpan.FromSeconds(3);

			var before = DateTimeOffset.Now;
			await new Snackbar { Text = "Constructor default" }.Show();
			var after = DateTimeOffset.Now;
			var expiration = (AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown.")).Expiration;
			Assert.InRange(expiration, before + defaultDuration - expirationMargin, after + defaultDuration + expirationMargin);

			before = DateTimeOffset.Now;
			await Snackbar.Make("Make default").Show();
			after = DateTimeOffset.Now;
			expiration = (AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown.")).Expiration;
			Assert.InRange(expiration, before + defaultDuration - expirationMargin, after + defaultDuration + expirationMargin);
		});
	}

	[Fact]
	public async Task DisplaySnackbarExtensionForwardsTheActionTextAndTheDurationToTheNotification()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			VisualElement? anchor = null;
			Action action = () => { };
			var duration = TimeSpan.FromSeconds(45);
			var before = DateTimeOffset.Now;

			await anchor.DisplaySnackbar("From the extension", action, "Do it", duration);

			var after = DateTimeOffset.Now;
			var notification = AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown.");
			Assert.Equal("Do it", Assert.Single(AlertsWindows.Parse(notification).Buttons).Content);
			Assert.Same(action, Assert.Single(AlertsWindows.GetRegisteredActions()).Value);
			Assert.InRange(notification.Expiration, before + duration - expirationMargin, after + duration + expirationMargin);
		});
	}

	// The Windows App SDK notification APIs can be called from any thread.
	[Fact]
	public async Task ShowOffTheUiThreadShowsTheNotification()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await Task.Run(() => Snackbar.Make("Off the UI thread", () => { }, "Undo", longDuration).Show());

		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.NotNull(AlertsWindows.GetPlatformSnackbar());
			Assert.Single(AlertsWindows.GetRegisteredActions());
			Assert.True(Snackbar.IsShown);
			await AssertEventually(async () => (await GetVisibleMessagesAsync()) is ["Off the UI thread"], timeout: 10000, message: "Windows does not list the notification that was shown from a background thread.");
		});
	}

	[Fact]
	public async Task DismissOffTheUiThreadOnAnotherInstanceRemovesTheNotification()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		await InvokeOnMainThreadAsync(() => Snackbar.Make("Shown", () => { }, "Undo", longDuration).Show());
		var notification = AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown.");
		var before = DateTimeOffset.Now;

		await Task.Run(() => Snackbar.Make("Other instance").Dismiss()).WaitAsync(dismissTimeout);

		var after = DateTimeOffset.Now;
		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.Null(AlertsWindows.GetPlatformSnackbar());
			Assert.Empty(AlertsWindows.GetRegisteredActions());
			Assert.InRange(notification.Expiration, before - expirationMargin, after + expirationMargin);
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "Windows still lists the notification that was dismissed from a background thread.");
		});
	}

	[Fact]
	public async Task ShowAgainFromAnotherInstanceExpiresTheOldNotificationAndReplacesItsAction()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			Action firstAction = () => { };
			Action secondAction = () => { };
			await Snackbar.Make("First", firstAction, "One", longDuration).Show();
			var first = AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("The first notification was not shown.");
			var before = DateTimeOffset.Now;

			await Snackbar.Make("Second", secondAction, "Two", longDuration).Show();

			var after = DateTimeOffset.Now;
			var second = AlertsWindows.GetPlatformSnackbar();
			Assert.NotNull(second);
			Assert.NotSame(first, second);
			Assert.InRange(first.Expiration, before - expirationMargin, after + expirationMargin);
			Assert.Same(secondAction, Assert.Single(AlertsWindows.GetRegisteredActions()).Value);
			await AssertEventually(async () => (await GetVisibleMessagesAsync()) is ["Second"], timeout: 10000, message: "Windows does not list only the second notification.");
		});
	}

	[Fact]
	public async Task DismissOnAnotherInstanceRemovesTheNotificationAndTheRegisteredAction()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Shown", () => { }, "Undo", longDuration).Show();
			var notification = AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown.");
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 1, timeout: 10000, message: "Windows does not list the notification that was shown.");
			var before = DateTimeOffset.Now;

			await Snackbar.Make("Other instance").Dismiss().WaitAsync(dismissTimeout);

			var after = DateTimeOffset.Now;
			Assert.Null(AlertsWindows.GetPlatformSnackbar());
			Assert.Empty(AlertsWindows.GetRegisteredActions());
			Assert.InRange(notification.Expiration, before - expirationMargin, after + expirationMargin);
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "Windows still lists the dismissed notification.");
		});
	}

	[Fact]
	public async Task DismissOnTheInstanceThatShowedTheNotificationCompletes()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = Snackbar.Make("Shown", duration: longDuration);
			await snackbar.Show();

			await snackbar.Dismiss().WaitAsync(dismissTimeout);

			Assert.Null(AlertsWindows.GetPlatformSnackbar());
		});
	}

	[Fact]
	public async Task ShowTwiceOnTheSameInstanceCompletes()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = Snackbar.Make("Shown twice", duration: longDuration);
			await snackbar.Show();

			await snackbar.Show().WaitAsync(dismissTimeout);

			Assert.Equal(new[] { "Shown twice" }, await GetVisibleMessagesAsync());
		});
	}

	// Two Dismiss calls from instances that did not show the notification are both in flight when the first RemoveAllAsync completes:
	// the second one finds the notification already gone after its await and must skip the expiration update.
	[Fact]
	public async Task ConcurrentDismissCallsOnOtherInstancesBothComplete()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Shown", duration: longDuration).Show();

			var first = Snackbar.Make("First dismiss").Dismiss();
			var second = Snackbar.Make("Second dismiss").Dismiss();
			await Task.WhenAll(first, second).WaitAsync(dismissTimeout);

			Assert.Null(AlertsWindows.GetPlatformSnackbar());
			Assert.Empty(AlertsWindows.GetRegisteredActions());
		});
	}

	// Only the dismissal that removes the notification raises Dismissed.
	[Fact]
	public async Task ConcurrentDismissCallsRaiseDismissedOnce()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("Shown", duration: longDuration).Show();

			var first = Snackbar.Make("First dismiss").Dismiss();
			var second = Snackbar.Make("Second dismiss").Dismiss();
			await Task.WhenAll(first, second).WaitAsync(dismissTimeout);

			Assert.Equal(new[] { nameof(Snackbar.Shown), nameof(Snackbar.Dismissed) }, recorder.Sequence);
			Assert.False(Snackbar.IsShown);
		});
	}

	// Windows removes an expired notification without telling the app: at the end of the snackbar's duration the toolkit dismisses it itself.
	[Fact]
	public async Task NotificationAndItsActionAreRemovedAtTheEndOfTheDuration()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Short lived", () => { }, "Undo", TimeSpan.FromMilliseconds(500)).Show();
			Assert.Single(AlertsWindows.GetRegisteredActions());

			await AssertEventually(() => !Snackbar.IsShown, timeout: 10000, message: "The snackbar never dismissed itself.");

			Assert.Null(AlertsWindows.GetPlatformSnackbar());
			Assert.Empty(AlertsWindows.GetRegisteredActions());
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "Windows still lists the notification after the end of its duration.");
		});
	}

	[Fact]
	public async Task DisposeLeavesTheNotificationAndItsActionInPlace()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = Snackbar.Make("Disposed", () => { }, "Undo", longDuration);
			await snackbar.Show();
			var notification = AlertsWindows.GetPlatformSnackbar();

			snackbar.Dispose();
			snackbar.Dispose();

			Assert.Same(notification, AlertsWindows.GetPlatformSnackbar());
			Assert.Single(AlertsWindows.GetRegisteredActions());
			await AssertEventually(async () => (await GetVisibleMessagesAsync()) is ["Disposed"], timeout: 10000, message: "Windows does not list the notification of the disposed snackbar.");
		});
	}

	[Fact]
	public async Task ConstructorThrowsWhileTheWindowsOptionIsNotEnabled()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(false));

		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.False(Options.ShouldEnableSnackbarOnWindows);

			var exception = Assert.Throws<InvalidOperationException>(() => new Snackbar());

			Assert.Contains("Package.appxmanifest", exception.Message);
			Assert.Contains(nameof(Options.SetShouldEnableSnackbarOnWindows), exception.Message);
			Assert.Equal(documentationUrl, exception.HelpLink);
			Assert.Throws<InvalidOperationException>(() => Snackbar.Make("Disabled"));
			VisualElement? anchor = null;
			await Assert.ThrowsAsync<InvalidOperationException>(() => anchor.DisplaySnackbar("Disabled"));
		});
	}

	[Fact]
	public async Task ConstructorSucceedsOnceTheWindowsOptionIsEnabledThroughTheBuilder()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(true));

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.True(Options.ShouldEnableSnackbarOnWindows);

			var snackbar = new Snackbar();

			Assert.Equal(TimeSpan.FromSeconds(3), snackbar.Duration);
		});
	}

	// A press of the notification's button reaches Snackbar.HandleSnackbarAction through NotificationInvoked, on a background thread, with
	// the arguments of the button. The action and the dismissal run on the UI thread because a Snackbar action usually updates the UI.
	// Only a person can press the button (PressingTheButtonOfTheNotificationRunsTheActionOnTheUiThreadAndDismissesTheSnackbar), so this
	// test hands the toolkit the press the way NotificationInvoked does.
	[Fact]
	public async Task PressOfTheButtonRunsTheActionThenDismissesTheSnackbarOnTheUiThread()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		bool? actionRanOnTheUiThread = null, dismissedOnTheUiThread = null;
		using var recorder = new SnackbarEventRecorder(() => dismissedOnTheUiThread = Microsoft.Maui.ApplicationModel.MainThread.IsMainThread);
		var arguments = await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Pressed", () =>
			{
				actionRanOnTheUiThread = Microsoft.Maui.ApplicationModel.MainThread.IsMainThread;
				recorder.Sequence.Add("Action");
			}, "Undo", longDuration).Show();

			return GetArgumentsOfAPressOfTheButton();
		});

		await Task.Run(() => Snackbar.HandleSnackbarAction(arguments));

		await InvokeOnMainThreadAsync(async () =>
		{
			await AssertEventually(() => recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: "The press of the button did not dismiss the snackbar.");
			Assert.Equal(new[] { nameof(Snackbar.Shown), "Action", nameof(Snackbar.Dismissed) }, recorder.Sequence);
			Assert.True(actionRanOnTheUiThread, "The action ran off the UI thread.");
			Assert.True(dismissedOnTheUiThread, "Dismissed was raised off the UI thread.");
			Assert.Null(AlertsWindows.GetPlatformSnackbar());
			Assert.Empty(AlertsWindows.GetRegisteredActions());
		});
	}

	[Fact]
	public async Task PressOfTheButtonOfASnackbarWithoutAnActionDismissesTheSnackbar()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		using var recorder = new SnackbarEventRecorder();
		var arguments = await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("No action", duration: longDuration).Show();

			return GetArgumentsOfAPressOfTheButton();
		});

		await Task.Run(() => Snackbar.HandleSnackbarAction(arguments));

		await InvokeOnMainThreadAsync(async () =>
		{
			await AssertEventually(() => recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: "The press of the button did not dismiss the snackbar.");
			Assert.Null(AlertsWindows.GetPlatformSnackbar());
			Assert.Empty(AlertsWindows.GetRegisteredActions());
		});
	}

	[Fact]
	public async Task SnackbarShownOffTheUiThreadRaisesDismissedOnTheUiThreadAtTheEndOfItsDuration()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		var dismissedOnTheUiThread = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var recorder = new SnackbarEventRecorder(() => dismissedOnTheUiThread.TrySetResult(Microsoft.Maui.ApplicationModel.MainThread.IsMainThread));

		await Task.Run(() => Snackbar.Make("Off the UI thread", () => { }, "Undo", TimeSpan.FromMilliseconds(500)).Show());

		Assert.True(await dismissedOnTheUiThread.Task.WaitAsync(dismissTimeout), "Dismissed was raised off the UI thread at the end of the Duration.");
		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.False(Snackbar.IsShown);
			Assert.Null(AlertsWindows.GetPlatformSnackbar());
			Assert.Empty(AlertsWindows.GetRegisteredActions());
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "Windows still lists the notification after the end of its duration.");
		});
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PressingTheButtonOfTheNotificationRunsTheActionOnTheUiThreadAndDismissesTheSnackbar()
	{
		await AlertsWindows.SkipWhenNotificationsAreUnavailableAsync();
		SetupBuilder();
		using var recorder = new SnackbarEventRecorder();
		var pressed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		await InvokeOnMainThreadAsync(() => Snackbar.Make("Press OK on this notification", () => pressed.TrySetResult(Microsoft.Maui.ApplicationModel.MainThread.IsMainThread), "OK", TimeSpan.FromSeconds(60)).Show());

		var ranOnTheUiThread = await pressed.Task.WaitAsync(TimeSpan.FromSeconds(60));

		Assert.True(ranOnTheUiThread, "The Snackbar action ran off the UI thread.");
		await InvokeOnMainThreadAsync(async () =>
		{
			await AssertEventually(() => recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: "Pressing the button did not dismiss the snackbar.");
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "Windows still lists the notification whose button was pressed.");
		});
	}

	static Dictionary<string, string> GetArgumentsOfAPressOfTheButton()
	{
		var button = Assert.Single(AlertsWindows.Parse(AlertsWindows.GetPlatformSnackbar() ?? throw new InvalidOperationException("No notification was shown.")).Buttons);

		return new Dictionary<string, string>
		{
			["snackbarIdentifier"] = button.SnackbarIdentifier ?? throw new InvalidOperationException($"The button has no snackbar identifier: {button.Arguments}")
		};
	}
}