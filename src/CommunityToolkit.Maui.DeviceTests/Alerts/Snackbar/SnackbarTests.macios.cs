using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using CoreText;
using Foundation;
using UIKit;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using Font = Microsoft.Maui.Font;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class SnackbarTests
{
	// The action button of the native snackbar can be activated from a test.
	static string? ActionCannotBeClickedReason => null;

	// Disposing a snackbar that is shown only unsubscribes its action button here: nothing has to be taken off screen first.
	static Task TakeTheSnackbarOffScreenBeforeItIsDisposedAsync() => Task.CompletedTask;

	[Fact]
	public async Task ShowAddsOneAlertViewToTheKeyWindowWithTheLabelBeforeTheButton()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Structure", () => { }, "Undo", longDuration).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			Assert.Same(AlertsApple.KeyWindow, alertView.Superview);
			Assert.Equal(2, alertView.Children.Count);
			Assert.IsType<PaddedLabel>(alertView.Children[0]);
			Assert.IsType<PaddedButton>(alertView.Children[1]);
			Assert.Equal("Structure", alertView.GetLabel().Text);
			Assert.Equal("Undo", alertView.GetActionButton()!.Title(UIControlState.Normal));
			Assert.Equal(new UIView[] { alertView.Children[0], alertView.Children[1] }, alertView.GetContainer().ArrangedSubviews);
		});
	}

	// The default duration (3 s) comes from the constructor for an initializer and from Make for Make; it becomes the NSTimer interval.
	[Fact]
	public async Task DefaultDurationOfThreeSecondsReachesThePlatformSnackbar()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await new Snackbar { Text = "Constructor default" }.Show();
			Assert.Equal(TimeSpan.FromSeconds(3), Assert.IsType<PlatformSnackbar>(GetPlatformSnackbar(), exactMatch: false).Duration);

			await Snackbar.Make("Make default").Show();
			Assert.Equal(TimeSpan.FromSeconds(3), Assert.IsType<PlatformSnackbar>(GetPlatformSnackbar(), exactMatch: false).Duration);
		});
	}

	[Fact]
	public async Task VisualOptionsAreAppliedToTheAlertViewParts()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			InstallApplicationHandler();
			var options = new SnackbarOptions
			{
				BackgroundColor = Color.FromRgb(200, 10, 20),
				TextColor = Color.FromRgb(10, 20, 200),
				Font = Font.OfSize("Courier", 22),
				CharacterSpacing = 3,
				ActionButtonTextColor = Color.FromRgb(30, 200, 40),
				ActionButtonFont = Font.OfSize("Georgia", 18)
			};

			await Snackbar.Make("Styled", () => { }, "Act", longDuration, options).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			var label = alertView.GetLabel();
			AlertsApple.AssertColor(200 / 255d, 10 / 255d, 20 / 255d, 1, alertView.GetBackground().BackgroundColor);
			AlertsApple.AssertColor(10 / 255d, 20 / 255d, 200 / 255d, 1, label.TextColor);
			Assert.Equal("Courier", label.Font.FamilyName);
			Assert.Equal(22, (double)label.Font.PointSize);
			// The amount of kerning is pinned by CharacterSpacingKernsTheMessageBy100TimesTheValueOverThePointSize: here only that it is applied.
			var attributes = label.AttributedText!.GetCoreTextAttributes(0, out _);
			Assert.True(attributes!.KerningAdjustment > 0, $"The message is not spaced: KerningAdjustment is {attributes.KerningAdjustment}.");
			var button = alertView.GetActionButton()!;
			AlertsApple.AssertColor(30 / 255d, 200 / 255d, 40 / 255d, 1, button.TitleColor(UIControlState.Normal));
			Assert.Equal("Georgia", button.TitleLabel.Font.FamilyName);
			Assert.Equal(18, (double)button.TitleLabel.Font.PointSize);
		});
	}

	// SnackbarOptions.CharacterSpacing kerns the message by 100 * CharacterSpacing / the point size of the message font, in points: the unit
	// documented on SnackbarOptions.CharacterSpacing (a MAUI Label with the same CharacterSpacing is kerned by CharacterSpacing points instead).
	[Theory]
	[InlineData(3.0, 24.0, 12.5)]
	[InlineData(3.0, 12.0, 25.0)]
	[InlineData(0.5, 20.0, 2.5)]
	public async Task CharacterSpacingKernsTheMessageBy100TimesTheValueOverThePointSize(double characterSpacing, double fontSize, double expectedKerning)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			InstallApplicationHandler();

			await Snackbar.Make("Spaced", () => { }, "Act", longDuration, new SnackbarOptions { Font = Font.OfSize("Courier", fontSize), CharacterSpacing = characterSpacing }).Show();

			var label = AlertsApple.GetSingleAlertView().GetLabel();
			Assert.Equal(fontSize, (double)label.Font.PointSize);
			Assert.Equal(expectedKerning, label.AttributedText!.GetCoreTextAttributes(0, out _)!.KerningAdjustment!.Value, 3);
		});
	}

	[Fact]
	public async Task DefaultVisualOptionsUseTheAlertDefaults()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Defaults", duration: longDuration).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			AlertsApple.AssertColor(211 / 255d, 211 / 255d, 211 / 255d, 1, alertView.GetBackground().BackgroundColor);
			AlertsApple.AssertColor(0, 0, 0, 1, alertView.GetLabel().TextColor);
			Assert.Equal(14, (double)alertView.GetLabel().Font.PointSize);
			AlertsApple.AssertColor(0, 0, 0, 1, alertView.GetActionButton()!.TitleColor(UIControlState.Normal));
			Assert.Equal(14, (double)alertView.GetActionButton()!.TitleLabel.Font.PointSize);
			var attributes = alertView.GetLabel().AttributedText!.GetCoreTextAttributes(0, out _);
			Assert.Equal(0, attributes!.KerningAdjustment!.Value, 3);
		});
	}

	[Fact]
	public async Task PaddingIsTheLargestCornerRadiusPlusTheDefaultPadding()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var options = new SnackbarOptions { CornerRadius = new CornerRadius(1, 2, 3, 40) };

			await Snackbar.Make("Padding", () => { }, "Undo", longDuration, options).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			Assert.Equal(50, (double)alertView.GetLabel().LeftPadding);
			Assert.Equal(50, (double)alertView.GetLabel().BottomPadding);
			Assert.Equal(50, (double)alertView.GetActionButton()!.RightPadding);
			Assert.Equal(new UIEdgeInsets(50, 50, 50, 50), alertView.GetActionButton()!.GetContentEdgeInsets());
		});
	}

	[Theory]
	[InlineData(AlertCorner.TopLeft)]
	[InlineData(AlertCorner.TopRight)]
	[InlineData(AlertCorner.BottomLeft)]
	[InlineData(AlertCorner.BottomRight)]
	public async Task CornerRadiusRoundsOnlyTheMatchingCorner(AlertCorner roundedCorner)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var options = new SnackbarOptions
			{
				CornerRadius = new CornerRadius(
					roundedCorner is AlertCorner.TopLeft ? 20 : 0,
					roundedCorner is AlertCorner.TopRight ? 20 : 0,
					roundedCorner is AlertCorner.BottomLeft ? 20 : 0,
					roundedCorner is AlertCorner.BottomRight ? 20 : 0)
			};

			await Snackbar.Make("Corners", () => { }, "Undo", longDuration, options).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			await AlertsApple.WaitForLayoutAsync(alertView);
			var background = alertView.GetBackground();
			await AssertEventually(() => background.Layer.Mask is not null && background.Bounds.Width > 0, timeout: 10000, message: "The rounded background was never laid out.");
			var bounds = background.Bounds;
			var points = new Dictionary<AlertCorner, CGPoint>
			{
				[AlertCorner.TopLeft] = new(1, 1),
				[AlertCorner.TopRight] = new(bounds.Width - 1.5, 1),
				[AlertCorner.BottomLeft] = new(1, bounds.Height - 1.5),
				[AlertCorner.BottomRight] = new(bounds.Width - 1.5, bounds.Height - 1.5)
			};

			foreach (var (corner, point) in points)
			{
				Assert.Equal(corner != roundedCorner, AlertsApple.IsInsideMask(background, point));
			}
		});
	}

	[Fact]
	public async Task SnackbarFillsTheSafeAreaWidth()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Full width", () => { }, "Undo", longDuration).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			await AlertsApple.WaitForLayoutAsync(alertView);
			var safeArea = AlertsApple.KeyWindow.SafeAreaLayoutGuide.LayoutFrame;
			AlertsApple.AssertClose(safeArea.Left + 10, alertView.Frame.Left);
			AlertsApple.AssertClose(safeArea.Right - 10, alertView.Frame.Right);
			AlertsApple.AssertClose(safeArea.Bottom - 10, alertView.Frame.Bottom);
		});
	}

	[Fact]
	public async Task LongMessagesWrapOntoSeveralLines()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Short", () => { }, "Undo", longDuration).Show();
			var shortAlertView = AlertsApple.GetSingleAlertView();
			await AlertsApple.WaitForLayoutAsync(shortAlertView);
			var shortHeight = shortAlertView.Frame.Height;

			await Snackbar.Make(string.Join(' ', Enumerable.Repeat("A very long snackbar message that has to wrap.", 40)), () => { }, "Undo", longDuration).Show();
			var longAlertView = AlertsApple.GetSingleAlertView();
			await AlertsApple.WaitForLayoutAsync(longAlertView);

			Assert.Equal(0, (int)longAlertView.GetLabel().Lines);
			Assert.True(longAlertView.Frame.Height > shortHeight + 30, $"A long message should wrap onto several lines: {longAlertView.Frame.Height} is not taller than {shortHeight} plus two more lines.");
		});
	}

	// A message taller than the screen: the view stays between the top and the bottom of the safe area (AlertView keeps its top at least
	// 10 points below the top of the safe area) instead of growing upwards off screen.
	[Fact]
	public async Task VeryLongSnackbarStaysWithinTheSafeArea()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make(string.Join(' ', Enumerable.Repeat("A very long snackbar message that has to wrap.", 400)), () => { }, "Undo", longDuration).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			await AlertsApple.WaitForLayoutAsync(alertView);
			var safeArea = AlertsApple.KeyWindow.SafeAreaLayoutGuide.LayoutFrame;

			Assert.True(alertView.Frame.Top >= safeArea.Top + 10 - 0.6, $"The alert view starts at {alertView.Frame.Top}, above the top of the safe area ({safeArea.Top}) plus 10.");
			AlertsApple.AssertClose(safeArea.Bottom - 10, alertView.Frame.Bottom);
		});
	}

	[Fact]
	public async Task EmptyMessageDisplaysAnEmptyLabel()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make(string.Empty, () => { }, "Undo", longDuration).Show();

			Assert.Equal(string.Empty, AlertsApple.GetSingleAlertView().GetLabel().Text);
		});
	}

	// iOS and Mac Catalyst keep the (padded) action button of a snackbar whose action text is empty, while Android hides the action
	// (SnackbarTests.EmptyActionButtonTextHidesTheActionButton): the platforms differ for this input.
	[Fact]
	public async Task EmptyActionButtonTextKeepsAnEmptyActionButton()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("No action text", () => { }, string.Empty, longDuration).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			var button = Assert.IsType<PaddedButton>(alertView.Children[1]);
			Assert.Equal(string.Empty, Assert.IsType<PlatformSnackbar>(GetPlatformSnackbar(), exactMatch: false).ActionButtonText);
			Assert.False(button.Hidden);
			Assert.Contains(button, alertView.GetContainer().ArrangedSubviews);
			Assert.Equal("No action text", alertView.GetLabel().Text);
		});
	}

	// UIKit only accepts calls from the main thread (the binding checks it in Debug builds): Show called from a background thread fails
	// before anything is created or shown.
	[Fact]
	public async Task ShowOffTheUiThreadThrowsUIKitThreadAccessExceptionAndShowsNothing()
	{
		Skip.IfNot(UIApplication.CheckForIllegalCrossThreadCalls, "UIKit does not check the thread of its callers in this build.");
		SetupBuilder();

		await Assert.ThrowsAsync<UIKitThreadAccessException>(() => Task.Run(() => Snackbar.Make("Off the UI thread", duration: longDuration).Show()));

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(GetPlatformSnackbar());
			Assert.False(Snackbar.IsShown);
			Assert.Empty(AlertsApple.GetAlertViews());
		});
	}

	// Dismiss fails the same way off the UI thread (Alert.Dismiss removes the view through UIKit), and the snackbar stays shown.
	[Fact]
	public async Task DismissOffTheUiThreadThrowsUIKitThreadAccessExceptionAndKeepsTheSnackbar()
	{
		Skip.IfNot(UIApplication.CheckForIllegalCrossThreadCalls, "UIKit does not check the thread of its callers in this build.");
		SetupBuilder();
		using var recorder = new SnackbarEventRecorder();
		var snackbar = Snackbar.Make("Stays", duration: longDuration);
		await InvokeOnMainThreadAsync(() => snackbar.Show());

		await Assert.ThrowsAsync<UIKitThreadAccessException>(() => Task.Run(() => snackbar.Dismiss()));

		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.Equal(new[] { "Stays" }, await GetVisibleMessagesAsync());
			Assert.True(Snackbar.IsShown);
			Assert.NotNull(GetPlatformSnackbar());
			Assert.Equal(new[] { nameof(Snackbar.Shown) }, recorder.Sequence);
		});
	}

	// ShowPlatform dismisses the snackbar that is shown before it checks the token again: a token cancelled while that happens (here by a
	// Dismissed handler, which the Apple dismissal raises synchronously) stops the new snackbar.
	[Fact]
	public async Task ShowWithATokenCancelledWhileTheShownSnackbarIsDismissedThrowsAndShowsNothing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("First", duration: longDuration).Show();
			using var cancellationTokenSource = new CancellationTokenSource();
			using var recorder = new SnackbarEventRecorder(afterDismissed: cancellationTokenSource.Cancel);

			await Assert.ThrowsAsync<OperationCanceledException>(() => Snackbar.Make("Second", duration: longDuration).Show(cancellationTokenSource.Token));

			Assert.Equal(new[] { nameof(Snackbar.Dismissed) }, recorder.Sequence);
			Assert.Null(GetPlatformSnackbar());
			Assert.False(Snackbar.IsShown);
			Assert.Empty(AlertsApple.GetAlertViews());
		});
	}

	[Fact]
	public async Task ShowWithoutAnchorLeavesTheAnchorViewNull()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("No anchor", duration: longDuration).Show();

			Assert.Null(AlertsApple.GetSingleAlertView().AnchorView);
		});
	}

	[Fact]
	public async Task ShowWithAnAnchorWithoutAPlatformViewLeavesTheAnchorViewNull()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Detached anchor", anchor: new Button(), duration: longDuration).Show();

			Assert.Null(AlertsApple.GetSingleAlertView().AnchorView);
		});
	}

	[Fact]
	public async Task ShowWithAnAnchorWhosePlatformViewIsNotAUIViewLeavesTheAnchorViewNull()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var anchor = new StubView { Handler = new StubViewHandler(MauiContext) };
			Assert.NotNull(anchor.Handler.PlatformView);
			Assert.False(anchor.Handler.PlatformView is UIView);

			await Snackbar.Make("Odd anchor", anchor: anchor, duration: longDuration).Show();

			Assert.Null(AlertsApple.GetSingleAlertView().AnchorView);
		});
	}

	[Fact]
	public async Task DisplaySnackbarExtensionForwardsTheVisualOptionsAndTheDurationToThePlatform()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			VisualElement? anchor = null;
			var options = new SnackbarOptions
			{
				BackgroundColor = Color.FromRgb(200, 10, 20),
				TextColor = Color.FromRgb(10, 20, 200),
				ActionButtonTextColor = Color.FromRgb(30, 200, 40)
			};

			var before = NSDate.Now.SecondsSinceReferenceDate;
			await anchor.DisplaySnackbar("Styled by the extension", () => { }, "Act", TimeSpan.FromMilliseconds(12345), options);
			var after = NSDate.Now.SecondsSinceReferenceDate;

			var alertView = AlertsApple.GetSingleAlertView();
			AlertsApple.AssertColor(200 / 255d, 10 / 255d, 20 / 255d, 1, alertView.GetBackground().BackgroundColor);
			AlertsApple.AssertColor(10 / 255d, 20 / 255d, 200 / 255d, 1, alertView.GetLabel().TextColor);
			AlertsApple.AssertColor(30 / 255d, 200 / 255d, 40 / 255d, 1, alertView.GetActionButton()!.TitleColor(UIControlState.Normal));
			Assert.Equal("Act", alertView.GetActionButton()!.Title(UIControlState.Normal));
			var platformSnackbar = Assert.IsType<PlatformSnackbar>(GetPlatformSnackbar(), exactMatch: false);
			Assert.Equal(TimeSpan.FromMilliseconds(12345), platformSnackbar.Duration);
			AlertsApple.AssertTimerFiresAfterTheDuration(platformSnackbar, TimeSpan.FromMilliseconds(12345), before, after);
		});
	}

	[Fact]
	public async Task ShowAnchoredToAPageIgnoresTheAnchor()
	{
		SetupBuilder();
		var page = new ContentPage { Content = new Label { Text = "Page anchor" } };

		await AttachAndRun(page, async handler =>
		{
			Assert.NotNull(handler.PlatformView);

			await Snackbar.Make("Page anchor", anchor: page, duration: longDuration).Show();

			Assert.Null(AlertsApple.GetSingleAlertView().AnchorView);
		});
	}

	[Fact]
	public async Task DisplaySnackbarExtensionUsesTheElementAsTheAnchorView()
	{
		SetupBuilder();
		var anchor = new Button { Text = "Anchor", WidthRequest = 120, HeightRequest = 44, Margin = new Thickness(0, 400, 0, 0), HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start };
		var layout = new VerticalStackLayout { Children = { anchor } };

		await AttachAndRun(layout, async handler =>
		{
			var platformAnchor = (UIView)anchor.Handler!.PlatformView!;

			await anchor.DisplaySnackbar("From the extension", duration: longDuration);

			var alertView = AlertsApple.GetSingleAlertView();
			Assert.Same(platformAnchor, alertView.AnchorView);
			alertView.Dismiss();
		});
	}

	[Fact]
	public async Task ShowAnchoredToAViewNearTheTopPlacesTheSnackbarBelowIt()
	{
		SetupBuilder();
		var anchor = new Button { Text = "Anchor", WidthRequest = 120, HeightRequest = 44, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start };

		await AttachAndRun(anchor, async handler =>
		{
			var platformAnchor = (UIView)handler.PlatformView!;

			await Snackbar.Make("Below the anchor", () => { }, "Undo", longDuration, anchor: anchor).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			await AlertsApple.WaitForLayoutAsync(alertView);
			Assert.Same(platformAnchor, alertView.AnchorView);
			var anchorBottom = platformAnchor.ConvertRectToView(platformAnchor.Bounds, AlertsApple.KeyWindow).Bottom;
			AlertsApple.AssertClose(anchorBottom + 10, alertView.Frame.Top);
			alertView.Dismiss();
		});
	}

	[Fact]
	public async Task ShowAnchoredToAViewFurtherDownPlacesTheSnackbarAboveIt()
	{
		SetupBuilder();
		var anchor = new Button { Text = "Anchor", WidthRequest = 120, HeightRequest = 44, Margin = new Thickness(0, 400, 0, 0), HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start };
		var layout = new VerticalStackLayout { Children = { anchor } };

		await AttachAndRun(layout, async handler =>
		{
			var platformAnchor = (UIView)anchor.Handler!.PlatformView!;

			await Snackbar.Make("Above the anchor", () => { }, "Undo", longDuration, anchor: anchor).Show();

			var alertView = AlertsApple.GetSingleAlertView();
			await AlertsApple.WaitForLayoutAsync(alertView);
			Assert.Same(platformAnchor, alertView.AnchorView);
			var anchorTop = platformAnchor.ConvertRectToView(platformAnchor.Bounds, AlertsApple.KeyWindow).Top;
			AlertsApple.AssertClose(anchorTop, alertView.Frame.Bottom);
			alertView.Dismiss();
		});
	}

	[Fact]
	public async Task DisposeStopsTheActionButtonFromInvokingTheAction()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var actionCount = 0;
			var snackbar = new Snackbar { Text = "Dispose", Action = () => actionCount++, Duration = longDuration };
			await snackbar.Show();

			snackbar.Dispose();
			Assert.True(await ClickVisibleActionAsync(), "No snackbar action button was found to click.");

			Assert.Equal(0, actionCount);
		});
	}

	[Fact]
	public async Task DisposeWithoutDisposingManagedStateKeepsTheActionButtonWorking()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var actionCount = 0;
			var snackbar = new DisposableSnackbar { Text = "Finalizer path", Action = () => actionCount++, Duration = longDuration };
			await snackbar.Show();

			snackbar.CallDispose(false);
			Assert.True(await ClickVisibleActionAsync(), "No snackbar action button was found to click.");

			Assert.Equal(1, actionCount);
		});
	}

	[Fact]
	public async Task SecondDisposeOfTheSameSnackbarDoesNotDisposeALaterPlatformSnackbar()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = new Snackbar { Text = "First", Duration = longDuration };
			await first.Show();
			first.Dispose();
			var actionCount = 0;
			await new Snackbar { Text = "Second", Action = () => actionCount++, Duration = longDuration }.Show();

			first.Dispose();
			Assert.True(await ClickVisibleActionAsync(), "No snackbar action button was found to click.");

			Assert.Equal(1, actionCount);
		});
	}

	[Fact]
	public async Task SnackbarDoesNotLeakAfterItIsDismissed()
	{
		SetupBuilder();
		WeakReference snackbarReference = null!, platformReference = null!, alertViewReference = null!;

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = new Snackbar { Text = "Leak", Action = () => { }, Duration = longDuration };
			await snackbar.Show();
			snackbarReference = new WeakReference(snackbar);
			platformReference = new WeakReference(GetPlatformSnackbar());
			alertViewReference = new WeakReference(AlertsApple.GetSingleAlertView());

			await snackbar.Dismiss().WaitAsync(dismissTimeout);
		});

		await AssertionExtensions.WaitForGC(snackbarReference, platformReference, alertViewReference);
	}

	// A snackbar that dismissed itself (its timer fired) is not kept alive either: the toolkit forgets and disposes it.
	[Fact]
	public async Task SnackbarDoesNotLeakAfterItDismissedItself()
	{
		SetupBuilder();
		WeakReference snackbarReference = null!, platformReference = null!, alertViewReference = null!;

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			var snackbar = new Snackbar { Text = "Leak", Action = () => { }, Duration = TimeSpan.FromMilliseconds(300) };
			await snackbar.Show();
			snackbarReference = new WeakReference(snackbar);
			platformReference = new WeakReference(GetPlatformSnackbar());
			alertViewReference = new WeakReference(AlertsApple.GetSingleAlertView());

			await AssertEventually(() => recorder.DismissedCount == 1, timeout: 10000, message: "The snackbar never dismissed itself.");
		});

		await AssertionExtensions.WaitForGC(snackbarReference, platformReference, alertViewReference);
	}

	// A subclass can reach the protected Dispose(bool): a Snackbar has no finalizer, so the false overload is otherwise unreachable.
	sealed class DisposableSnackbar : Snackbar
	{
		public void CallDispose(bool isDisposing) => Dispose(isDisposing);
	}

	// A Snackbar is the AlertView that has an action button; a Toast has none.
	static IEnumerable<AlertView> GetSnackbarAlertViews() => AlertsApple.GetAlertViews().Where(alertView => alertView.GetActionButton() is not null);

	static Task<IReadOnlyList<string>> GetVisibleMessagesAsync() =>
		Task.FromResult<IReadOnlyList<string>>([.. GetSnackbarAlertViews().Select(alertView => alertView.GetLabel().Text ?? string.Empty)]);

	static Task<IReadOnlyList<string>> GetVisibleActionTextsAsync() =>
		Task.FromResult<IReadOnlyList<string>>([.. GetSnackbarAlertViews().Select(alertView => alertView.GetActionButton()!.Title(UIControlState.Normal) ?? string.Empty)]);

	static Task<bool> ClickVisibleActionAsync()
	{
		var button = GetSnackbarAlertViews().FirstOrDefault()?.GetActionButton();
		button?.SendActionForControlEvents(UIControlEvent.TouchUpInside);
		return Task.FromResult(button is not null);
	}
}