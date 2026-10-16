using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

// Snackbar on every platform. The shared tests drive the public API and read what the user would see from the platform partials:
// GetVisibleMessagesAsync, GetVisibleActionTextsAsync and ClickVisibleActionAsync look at the native views (Android: the Material
// SnackbarLayout in the activity, Apple: the AlertView in the key window, Windows: the app notifications of the app).
// The pure logic of Snackbar (argument checks, defaults, the weak Anchor) is tested by CommunityToolkit.Maui.UnitTests.
[Category(TestCategory.Snackbar)]
[Collection(TestCollections.DisableParallelization)]
public partial class SnackbarTests : AlertsTestBase
{
	static readonly TimeSpan longDuration = TimeSpan.FromSeconds(30);

	// An awaited Dismiss of the instance that shows the snackbar waits for the platform to report the dismissal: the timeout turns a
	// dismissal that is never reported into a failure instead of a run that hangs until the app's timeout.
	static readonly TimeSpan dismissTimeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task ShowDisplaysTheMessageAndTheActionTextNatively()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Hello snackbar", () => { }, "Undo", longDuration).Show();

			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 1, timeout: 10000, message: "The snackbar was never displayed.");
			Assert.Equal(new[] { "Hello snackbar" }, await GetVisibleMessagesAsync());
			Assert.Equal(new[] { "Undo" }, await GetVisibleActionTextsAsync());
		});
	}

	[Fact]
	public async Task ShowWithoutActionTextDisplaysTheDefaultActionText()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Default action text", duration: longDuration).Show();

			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 1, timeout: 10000, message: "The snackbar was never displayed.");
			Assert.Equal(new[] { "OK" }, await GetVisibleActionTextsAsync());
		});
	}

	[Fact]
	public async Task ShowRaisesShownOnceAndSetsIsShown()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			Assert.False(Snackbar.IsShown);

			await Snackbar.Make("Shown event", duration: longDuration).Show();

			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "Snackbar.IsShown never became true.");
			Assert.Equal(1, recorder.ShownCount);
			Assert.Equal(0, recorder.DismissedCount);
		});
	}

	[Fact]
	public async Task DismissRemovesTheSnackbarRaisesDismissedAndClearsIsShown()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			var snackbar = Snackbar.Make("Dismiss me", duration: longDuration);
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			await snackbar.Dismiss().WaitAsync(dismissTimeout);

			await AssertEventually(async () => !Snackbar.IsShown && (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "The snackbar was not dismissed.");
			Assert.Equal(1, recorder.ShownCount);
			Assert.Equal(1, recorder.DismissedCount);
		});
	}

	[Fact]
	public async Task DismissingTwiceRaisesDismissedOnlyOnce()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			var snackbar = Snackbar.Make("Dismissed twice", duration: longDuration);
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
			await snackbar.Dismiss().WaitAsync(dismissTimeout);
			await AssertEventually(() => recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: "The snackbar was not dismissed.");

			await snackbar.Dismiss().WaitAsync(dismissTimeout);

			// An event that the second Dismiss raises would arrive before the Shown event of the next snackbar.
			await Snackbar.Make("Sentinel", duration: longDuration).Show();
			await AssertEventually(() => recorder.ShownCount == 2, timeout: 10000, message: "The sentinel snackbar was never shown.");
			Assert.Equal(new[] { nameof(Snackbar.Shown), nameof(Snackbar.Dismissed), nameof(Snackbar.Shown) }, recorder.Sequence);
		});
	}

	[Fact]
	public async Task DismissWithNothingShownCompletesWithoutRaisingDismissed()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			ResetToolkitAlertState();

			await Snackbar.Make("Nothing to dismiss", duration: longDuration).Dismiss();

			Assert.False(Snackbar.IsShown);
			Assert.Empty(await GetVisibleMessagesAsync());
			Assert.Equal(0, recorder.DismissedCount);
		});
	}

	[Fact]
	public async Task ShowReplacesTheSnackbarThatIsAlreadyShown()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("First", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The first snackbar was never shown.");

			await Snackbar.Make("Second", duration: longDuration).Show();

			await AssertEventually(async () => (await GetVisibleMessagesAsync()) is ["Second"] && recorder.ShownCount == 2, timeout: 15000, message: "The second snackbar did not replace the first one.");
			Assert.Equal(nameof(Snackbar.Shown), recorder.Sequence[^1]);
			Assert.True(Snackbar.IsShown);
		});
	}

	[Fact]
	public async Task ShowReplacingASnackbarRaisesDismissedForTheReplacedOne()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("First", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The first snackbar was never shown.");

			await Snackbar.Make("Second", duration: longDuration).Show();

			await AssertEventually(() => recorder.ShownCount == 2 && recorder.DismissedCount >= 1, timeout: 15000, message: "The replaced snackbar never raised Dismissed.");
			Assert.Equal(nameof(Snackbar.Dismissed), recorder.Sequence[1]);
		});
	}

	[Fact]
	public async Task ShowReplacingASnackbarRaisesDismissedExactlyOnceForTheReplacedOne()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("First", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The first snackbar was never shown.");

			await Snackbar.Make("Second", duration: longDuration).Show();

			await AssertEventually(async () => (await GetVisibleMessagesAsync()) is ["Second"] && recorder.ShownCount == 2, timeout: 15000, message: "The second snackbar did not replace the first one.");
			Assert.Equal(new[] { nameof(Snackbar.Shown), nameof(Snackbar.Dismissed), nameof(Snackbar.Shown) }, recorder.Sequence);
			Assert.True(Snackbar.IsShown);
		});
	}

	// The only path on which Show waits for a dismissal: a second Show of the instance that shows the snackbar dismisses it first and,
	// on Android, waits for the dismissal that Material reports to that instance before it shows the new snackbar. A new instance never
	// waits (see the replacing tests above).
	[Fact]
	public async Task ShowTwiceOnTheSameInstanceRaisesDismissedOnceBetweenTheShows()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			var snackbar = Snackbar.Make("Shown twice", duration: longDuration);
			await snackbar.Show();
			await AssertEventually(() => recorder.ShownCount == 1, timeout: 10000, message: "The first Show never raised Shown.");

			await snackbar.Show().WaitAsync(dismissTimeout);

			await AssertEventually(() => recorder.ShownCount == 2, timeout: 10000, message: "The second Show never raised Shown.");
			Assert.Equal(new[] { nameof(Snackbar.Shown), nameof(Snackbar.Dismissed), nameof(Snackbar.Shown) }, recorder.Sequence);
			await AssertEventually(async () => (await GetVisibleMessagesAsync()) is ["Shown twice"], timeout: 10000, message: "Exactly one snackbar should be displayed after the second Show.");
			Assert.True(Snackbar.IsShown);
		});
	}

	[Fact]
	public async Task ClickingTheActionInvokesTheActionOnce()
	{
		Skip.If(ActionCannotBeClickedReason is not null, ActionCannotBeClickedReason ?? string.Empty);
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var actionCount = 0;
			await Snackbar.Make("Click me", () => actionCount++, "Undo", longDuration).Show();
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 1, timeout: 10000, message: "The snackbar was never displayed.");

			Assert.True(await ClickVisibleActionAsync(), "No snackbar action was found to click.");

			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "The snackbar stayed on screen after its action was clicked.");
			Assert.Equal(1, actionCount);
		});
	}

	[Fact]
	public async Task ClickingTheActionWithoutAnActionDoesNotThrowAndRemovesTheSnackbar()
	{
		Skip.If(ActionCannotBeClickedReason is not null, ActionCannotBeClickedReason ?? string.Empty);
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("No action", null, "Undo", longDuration).Show();
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 1, timeout: 10000, message: "The snackbar was never displayed.");

			Assert.True(await ClickVisibleActionAsync(), "No snackbar action was found to click.");

			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "The snackbar stayed on screen after its action was clicked.");
		});
	}

	[Fact]
	public async Task ClickingTheActionRaisesDismissedAndClearsIsShown()
	{
		Skip.If(ActionCannotBeClickedReason is not null, ActionCannotBeClickedReason ?? string.Empty);
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("Click me", () => { }, "Undo", longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			Assert.True(await ClickVisibleActionAsync(), "No snackbar action was found to click.");

			await AssertEventually(() => !Snackbar.IsShown && recorder.DismissedCount == 1, timeout: 10000, message: "Clicking the action did not dismiss the snackbar.");
		});
	}

	// Clicking the action dismisses the snackbar and raises Dismissed: showing the next snackbar must not dismiss it, and raise Dismissed
	// for it, a second time.
	[Fact]
	public async Task ShowAfterClickingTheActionDoesNotRaiseDismissedAgain()
	{
		Skip.If(ActionCannotBeClickedReason is not null, ActionCannotBeClickedReason ?? string.Empty);
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("Click me", () => { }, "Undo", longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
			Assert.True(await ClickVisibleActionAsync(), "No snackbar action was found to click.");
			await AssertEventually(() => recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: "Clicking the action did not dismiss the snackbar.");

			await Snackbar.Make("Next", duration: longDuration).Show();

			await AssertEventually(() => recorder.ShownCount == 2, timeout: 10000, message: "The next snackbar was never shown.");
			Assert.Equal(new[] { nameof(Snackbar.Shown), nameof(Snackbar.Dismissed), nameof(Snackbar.Shown) }, recorder.Sequence);
		});
	}

	// The action itself dismisses the snackbar (here through another instance): the click must not dismiss it, and raise Dismissed for it,
	// a second time.
	[Fact]
	public async Task ClickingAnActionThatDismissesTheSnackbarRaisesDismissedOnce()
	{
		Skip.If(ActionCannotBeClickedReason is not null, ActionCannotBeClickedReason ?? string.Empty);
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("Click me", () => _ = Snackbar.Make("Other instance").Dismiss(), "Close", longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			Assert.True(await ClickVisibleActionAsync(), "No snackbar action was found to click.");

			await AssertEventually(async () => recorder.DismissedCount >= 1 && !Snackbar.IsShown && (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "Clicking the action did not dismiss the snackbar.");

			// A second Dismissed for the clicked snackbar would arrive before the Shown event of the next snackbar.
			await Snackbar.Make("Sentinel", duration: longDuration).Show();
			await AssertEventually(() => recorder.ShownCount == 2, timeout: 10000, message: "The sentinel snackbar was never shown.");
			Assert.Equal(new[] { nameof(Snackbar.Shown), nameof(Snackbar.Dismissed), nameof(Snackbar.Shown) }, recorder.Sequence);
		});
	}

	// The "Undo" pattern: the action shows the next snackbar, which dismisses the clicked one. The click must not dismiss the clicked
	// snackbar, and raise Dismissed for it, a second time: the next snackbar stays shown.
	[Fact]
	public async Task ClickingAnActionThatShowsTheNextSnackbarRaisesDismissedOnceAndKeepsTheNextSnackbarShown()
	{
		Skip.If(ActionCannotBeClickedReason is not null, ActionCannotBeClickedReason ?? string.Empty);
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("Click me", () => _ = Snackbar.Make("Next", duration: longDuration).Show(), "Undo", longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			Assert.True(await ClickVisibleActionAsync(), "No snackbar action was found to click.");

			await AssertEventually(async () => recorder.ShownCount == 2 && (await GetVisibleMessagesAsync()) is ["Next"], timeout: 15000, message: "The next snackbar did not replace the clicked one.");
			Assert.Equal(new[] { nameof(Snackbar.Shown), nameof(Snackbar.Dismissed), nameof(Snackbar.Shown) }, recorder.Sequence);
			Assert.True(Snackbar.IsShown);
		});
	}

	[Fact]
	public async Task ShowWithACancelledTokenThrowsOperationCanceledAndShowsNothing()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAsync<OperationCanceledException>(() => Snackbar.Make("Cancelled", duration: longDuration).Show(cancellationTokenSource.Token));

			Assert.Empty(await GetVisibleMessagesAsync());
			Assert.False(Snackbar.IsShown);
			Assert.Equal(0, recorder.ShownCount);
		});
	}

	[Fact]
	public async Task DismissWithACancelledTokenThrowsAndKeepsTheSnackbarShown()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = Snackbar.Make("Keep me", duration: longDuration);
			await snackbar.Show();
			await AssertEventually(async () => Snackbar.IsShown && (await GetVisibleMessagesAsync()).Count == 1, timeout: 10000, message: "The snackbar was never displayed.");
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => snackbar.Dismiss(cancellationTokenSource.Token));

			Assert.Equal(new[] { "Keep me" }, await GetVisibleMessagesAsync());
			Assert.True(Snackbar.IsShown);
		});
	}

	[Fact]
	public async Task DismissWithACancelledTokenAndNothingShownThrowsOperationCanceled()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			ResetToolkitAlertState();
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Snackbar.Make("Nothing shown", duration: longDuration).Dismiss(cancellationTokenSource.Token));
		});
	}

	[Fact]
	public async Task SnackbarDismissesItselfAfterItsDuration()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			var stopwatch = Stopwatch.StartNew();

			await Snackbar.Make("Short lived", duration: TimeSpan.FromMilliseconds(700)).Show();

			await AssertEventually(() => recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: "The snackbar never dismissed itself.");
			stopwatch.Stop();

			// The platform timer runs for the whole duration: a snackbar whose duration was ignored would go away at once (or much later).
			Assert.True(stopwatch.ElapsedMilliseconds >= 600, $"The snackbar went away after {stopwatch.ElapsedMilliseconds} ms although its duration is 700 ms.");
			Assert.Equal(1, recorder.ShownCount);
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 0, timeout: 10000, message: "The snackbar stayed on screen after it was dismissed.");
		});
	}

	// A snackbar that went away by itself has raised Dismissed already: showing the next snackbar must not dismiss it, and raise
	// Dismissed for it, a second time.
	[Fact]
	public async Task ShowAfterTheSnackbarDismissedItselfDoesNotRaiseDismissedAgain()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("Short lived", duration: TimeSpan.FromMilliseconds(300)).Show();
			await AssertEventually(() => recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: "The snackbar never dismissed itself.");

			await Snackbar.Make("Next", duration: longDuration).Show();

			await AssertEventually(() => recorder.ShownCount == 2, timeout: 10000, message: "The next snackbar was never shown.");
			Assert.Equal(new[] { nameof(Snackbar.Shown), nameof(Snackbar.Dismissed), nameof(Snackbar.Shown) }, recorder.Sequence);
		});
	}

	// A duration of zero (or less) means that the snackbar does not stay. The limit, 1.5 s, is Material's shortest timed length
	// (LENGTH_SHORT): a snackbar that goes away at once is gone well before it, animations included. Material reads -1 and -2 ms as its
	// LENGTH_SHORT and LENGTH_INDEFINITE constants; iOS and Mac Catalyst (NSTimer) use 0.1 ms for an interval that is not positive.
	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-2)]
	public async Task ZeroOrNegativeDurationSnackbarDismissesItselfAtOnce(int durationMilliseconds)
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			var stopwatch = Stopwatch.StartNew();

			await Snackbar.Make("Zero duration", duration: TimeSpan.FromMilliseconds(durationMilliseconds)).Show();

			await AssertEventually(() => recorder.ShownCount == 1 && recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: $"The snackbar whose duration is {durationMilliseconds} ms never dismissed itself.");
			stopwatch.Stop();
			Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1.5), $"The snackbar went away after {stopwatch.ElapsedMilliseconds} ms although its duration is {durationMilliseconds} ms.");
		});
	}

	[Fact]
	public async Task DisplaySnackbarExtensionShowsTheSnackbarWithoutAnAnchor()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			VisualElement? anchor = null;

			await anchor.DisplaySnackbar("From the extension", () => { }, "Do it", longDuration);

			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 1, timeout: 10000, message: "The snackbar was never displayed.");
			Assert.Equal(new[] { "From the extension" }, await GetVisibleMessagesAsync());
			Assert.Equal(new[] { "Do it" }, await GetVisibleActionTextsAsync());
		});
	}

	[Fact]
	public async Task DisplaySnackbarExtensionForwardsTheActionToTheActionButton()
	{
		Skip.If(ActionCannotBeClickedReason is not null, ActionCannotBeClickedReason ?? string.Empty);
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var actionCount = 0;
			VisualElement? anchor = null;
			await anchor.DisplaySnackbar("From the extension", () => actionCount++, "Do it", longDuration);
			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 1, timeout: 10000, message: "The snackbar was never displayed.");

			Assert.True(await ClickVisibleActionAsync(), "No snackbar action was found to click.");

			await AssertEventually(() => actionCount == 1, timeout: 10000, message: "The extension's action was not invoked.");
		});
	}

	[Fact]
	public async Task DisplaySnackbarExtensionWithAnAnchorThatHasNoPlatformViewShowsTheSnackbar()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var anchor = new Button();

			await anchor.DisplaySnackbar("Detached anchor", duration: longDuration);

			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 1, timeout: 10000, message: "The snackbar was never displayed.");
			Assert.Equal(new[] { "Detached anchor" }, await GetVisibleMessagesAsync());
		});
	}

	[Fact]
	public async Task DisplaySnackbarExtensionWithACancelledTokenThrowsOperationCanceled()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();
			VisualElement? anchor = null;

			await Assert.ThrowsAsync<OperationCanceledException>(() => anchor.DisplaySnackbar("Cancelled", token: cancellationTokenSource.Token));

			Assert.Empty(await GetVisibleMessagesAsync());
		});
	}

	[Fact]
	public async Task DisposeTwiceDoesNotThrow()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = new Snackbar { Text = "Dispose twice", Duration = longDuration };
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
			await TakeTheSnackbarOffScreenBeforeItIsDisposedAsync();

			var exception = Record.Exception(() =>
			{
				snackbar.Dispose();
				snackbar.Dispose();
			});

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task EmptyMessageIsDisplayed()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make(string.Empty, () => { }, "Undo", longDuration).Show();

			await AssertEventually(async () => (await GetVisibleMessagesAsync()) is [""], timeout: 10000, message: "The snackbar with an empty message was never displayed.");
			Assert.Equal(new[] { "Undo" }, await GetVisibleActionTextsAsync());
		});
	}

	[Fact]
	public async Task DisposeBeforeAnySnackbarWasShownDoesNotThrow()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			ResetToolkitAlertState();
			Assert.Null(GetPlatformSnackbar());

			var exception = Record.Exception(() => new Snackbar { Text = "Never shown" }.Dispose());

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task RemovedShownHandlerIsNoLongerCalled()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var removed = new SnackbarEventRecorder();
			removed.Dispose();
			using var active = new SnackbarEventRecorder();

			await Snackbar.Make("Shown events", duration: longDuration).Show();

			await AssertEventually(() => active.ShownCount == 1, timeout: 10000, message: "The handler that stayed subscribed never received Shown.");
			Assert.Equal(0, removed.ShownCount);
		});
	}

	[Fact]
	public async Task RemovedDismissedHandlerIsNoLongerCalled()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var removed = new SnackbarEventRecorder();
			removed.Dispose();
			using var active = new SnackbarEventRecorder();
			var snackbar = Snackbar.Make("Dismissed events", duration: longDuration);
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			await snackbar.Dismiss().WaitAsync(dismissTimeout);

			await AssertEventually(() => active.DismissedCount == 1, timeout: 10000, message: "The handler that stayed subscribed never received Dismissed.");
			Assert.Equal(0, removed.DismissedCount);
		});
	}

	[Fact]
	public async Task DismissOnAnotherInstanceDismissesTheShownSnackbar()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("Shown", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			await Snackbar.Make("Other instance").Dismiss();

			await AssertEventually(async () => (await GetVisibleMessagesAsync()).Count == 0 && !Snackbar.IsShown && recorder.DismissedCount == 1, timeout: 10000, message: "The shown snackbar was not dismissed.");
		});
	}
}