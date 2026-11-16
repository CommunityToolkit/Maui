using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Google.Android.Material.Shape;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Platform;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using Button = Microsoft.Maui.Controls.Button;
using Color = Microsoft.Maui.Graphics.Color;
using Font = Microsoft.Maui.Font;
using MaterialSnackbar = Google.Android.Material.Snackbar.Snackbar;
using View = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class SnackbarTests
{
	static string? ActionCannotBeClickedReason => null;

	static Task<IReadOnlyList<string>> GetVisibleMessagesAsync() =>
		Task.FromResult<IReadOnlyList<string>>([.. AlertsAndroid.GetSnackbarLayouts().Select(layout => AlertsAndroid.GetMessageView(layout).Text ?? string.Empty)]);

	static Task<IReadOnlyList<string>> GetVisibleActionTextsAsync() =>
		Task.FromResult<IReadOnlyList<string>>([.. AlertsAndroid.GetSnackbarLayouts().Select(layout => AlertsAndroid.GetActionView(layout).Text ?? string.Empty)]);

	static Task<bool> ClickVisibleActionAsync()
	{
		var layout = AlertsAndroid.GetSnackbarLayouts().LastOrDefault();
		var clicked = layout is not null && AlertsAndroid.GetActionView(layout).PerformClick();
		return Task.FromResult(clicked);
	}

	// Disposing the Java peer of a snackbar that is still shown would leave it on screen for good, and in Material's SnackbarManager, which
	// would dismiss it into the next test (see AlertsAndroid.RemoveSnackbarsThatCannotBeDismissedAsync). The dismissed Material snackbar
	// stays the toolkit's platform snackbar, which Dispose then disposes.
	static async Task TakeTheSnackbarOffScreenBeforeItIsDisposedAsync()
	{
		AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).Dismiss();
		await AssertEventually(() => AlertsAndroid.GetSnackbarLayouts().Count == 0 && !Snackbar.IsShown, timeout: 10000, message: "The snackbar did not go away before it was disposed.");
	}

	[Fact]
	public async Task SnackbarViewIsAddedToTheActivityContent()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("In the content", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
			Assert.Equal(Android.Resource.Id.Content, ((View)layout.Parent!).Id);
		});
	}

	[Fact]
	public async Task DurationIsPassedToTheMaterialSnackbarInMilliseconds()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Duration", duration: TimeSpan.FromMilliseconds(12345)).Show();

			Assert.Equal(12345, AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).Duration);
		});
	}

	// The default duration (3 s) comes from the constructor for an initializer and from Make for Make.
	[Fact]
	public async Task DefaultDurationOfThreeSecondsReachesTheMaterialSnackbar()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await new Snackbar { Text = "Constructor default" }.Show();
			Assert.Equal(3000, AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).Duration);

			await Snackbar.Make("Make default").Show();
			Assert.Equal(3000, AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).Duration);
		});
	}

	[Fact]
	public async Task DefaultVisualOptionsUseTheAlertDefaults()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Defaults", () => { }, "Act", longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
			var message = AlertsAndroid.GetMessageView(layout);
			var action = AlertsAndroid.GetActionView(layout);
			Assert.Equal(Colors.Black.ToPlatform().ToArgb(), message.CurrentTextColor);
			Assert.Equal(Colors.Black.ToPlatform().ToArgb(), action.CurrentTextColor);
			Assert.Equal(14 * AlertsAndroid.Density, message.TextSize, 0.5);
			Assert.Equal(14 * AlertsAndroid.Density, action.TextSize, 0.5);
			Assert.Equal(0, message.LetterSpacing);
			Assert.All(GetCornerRadii(), radius => Assert.Equal(4 * AlertsAndroid.Density, radius, 0.01));
			await AssertEventually(() => layout.Width > 0 && layout.Height > 0, timeout: 10000, message: "The snackbar was never laid out.");
			using var bitmap = AlertsAndroid.Render(layout);
			Assert.Equal(Colors.LightGray.ToPlatform().ToArgb(), bitmap.GetPixel(bitmap.Width / 2, 6));
		});
	}

	[Fact]
	public async Task VisualOptionsAreAppliedToTheSnackbarViews()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var options = new SnackbarOptions
			{
				TextColor = Color.FromRgb(10, 20, 200),
				Font = Font.OfSize("sans-serif", 20, FontWeight.Bold),
				CharacterSpacing = 2,
				ActionButtonTextColor = Color.FromRgb(30, 200, 40),
				ActionButtonFont = Font.OfSize("serif", 18, FontWeight.Regular, FontSlant.Italic)
			};

			await Snackbar.Make("Styled", () => { }, "Act", longDuration, options).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
			var message = AlertsAndroid.GetMessageView(layout);
			var action = AlertsAndroid.GetActionView(layout);
			Assert.Equal(Color.FromRgb(10, 20, 200).ToPlatform().ToArgb(), message.CurrentTextColor);
			Assert.Equal(20 * AlertsAndroid.Density, message.TextSize, 0.5);
			Assert.True(message.Typeface!.IsBold);

			// The amount of spacing is pinned by CharacterSpacingIsTheLetterSpacingOfTheMessageInEm.
			Assert.True(message.LetterSpacing > 0, $"The message is not spaced: LetterSpacing is {message.LetterSpacing}.");
			Assert.Equal(10, message.MaxLines);
			Assert.Equal(Color.FromRgb(30, 200, 40).ToPlatform().ToArgb(), action.CurrentTextColor);
			Assert.Equal(18 * AlertsAndroid.Density, action.TextSize, 0.5);
			Assert.True(action.Typeface!.IsItalic);
			Assert.False(action.Typeface.IsBold);
		});
	}

	// The expected values follow the unit documented on SnackbarOptions.CharacterSpacing; a MAUI Label with the same CharacterSpacing is
	// spaced by CharacterSpacing * 0.0624 em instead.
	[Theory]
	[InlineData(2.0, 20.0)]
	[InlineData(2.0, 10.0)]
	[InlineData(0.125, 14.0)]
	public async Task CharacterSpacingIsTheLetterSpacingOfTheMessageInEm(double characterSpacing, double fontSize)
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Spaced", () => { }, "Act", longDuration, new SnackbarOptions { Font = Font.OfSize("sans-serif", fontSize), CharacterSpacing = characterSpacing }).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var message = AlertsAndroid.GetMessageView(Assert.Single(AlertsAndroid.GetSnackbarLayouts()));
			Assert.Equal(fontSize * AlertsAndroid.Density, message.TextSize, 0.5);
			Assert.Equal(characterSpacing, message.LetterSpacing, EmCoefficientPrecision);
		});
	}

	[Fact]
	public async Task TypefaceComesFromTheFontManagerOfTheApplicationHandler()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var font = Font.OfSize("serif", 16, FontWeight.Bold, FontSlant.Italic);
			var fontManager = MauiContext.Services.GetRequiredService<IFontManager>();

			await Snackbar.Make("Typeface", () => { }, "Act", longDuration, new SnackbarOptions { Font = font, ActionButtonFont = font }).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
			Assert.Equal(fontManager.GetTypeface(font)!.Handle, AlertsAndroid.GetMessageView(layout).Typeface!.Handle);
			Assert.Equal(fontManager.GetTypeface(font)!.Handle, AlertsAndroid.GetActionView(layout).Typeface!.Handle);
		});
	}

	[Fact]
	public async Task FontSizeOfZeroKeepsTheDefaultTextSizes()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var nativeDefault = MaterialSnackbar.Make(AlertsAndroid.Content, "Native", MaterialSnackbar.LengthIndefinite)!.View;
			var defaultMessageSize = AlertsAndroid.GetMessageView(nativeDefault).TextSize;
			var defaultActionSize = AlertsAndroid.GetActionView(nativeDefault).TextSize;
			var options = new SnackbarOptions { Font = Font.OfSize("sans-serif", 0), ActionButtonFont = Font.OfSize("sans-serif", 0) };

			await Snackbar.Make("Default sizes", () => { }, "Act", longDuration, options).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
			Assert.Equal(defaultMessageSize, AlertsAndroid.GetMessageView(layout).TextSize, 0.01);
			Assert.Equal(defaultActionSize, AlertsAndroid.GetActionView(layout).TextSize, 0.01);
		});
	}

	[Fact]
	public async Task MessageIsAllowedToWrapOntoTenLines()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make(string.Join(' ', Enumerable.Repeat("A very long snackbar message that has to wrap.", 40)), duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var message = AlertsAndroid.GetMessageView(Assert.Single(AlertsAndroid.GetSnackbarLayouts()));
			Assert.Equal(10, message.MaxLines);
			Assert.Equal(10, message.LineCount);
		});
	}

	[Fact]
	public async Task BackgroundColorIsRenderedByTheSnackbar()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var options = new SnackbarOptions { BackgroundColor = Color.FromRgb(200, 10, 20) };

			await Snackbar.Make("Background", () => { }, "Act", longDuration, options).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
			await AssertEventually(() => layout.Width > 0 && layout.Height > 0, timeout: 10000, message: "The snackbar was never laid out.");
			using var bitmap = AlertsAndroid.Render(layout);
			Assert.Equal(new Android.Graphics.Color(200, 10, 20).ToArgb(), bitmap.GetPixel(bitmap.Width / 2, 6));
		});
	}

	[Fact]
	public async Task UniformCornerRadiusIsScaledByTheScreenDensityOnEveryCorner()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var options = new SnackbarOptions { CornerRadius = new CornerRadius(8) };

			await Snackbar.Make("Corners", () => { }, "Act", longDuration, options).Show();

			var radii = GetCornerRadii();
			Assert.Equal(8, radii.Length);
			Assert.All(radii, radius => Assert.Equal(8 * AlertsAndroid.Density, radius, 0.01));
		});
	}

	[Fact]
	public async Task CornerRadiiAreAppliedToTheMatchingCorners()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var options = new SnackbarOptions { CornerRadius = new CornerRadius(1, 2, 3, 4) };

			await Snackbar.Make("Corners", () => { }, "Act", longDuration, options).Show();

			var density = AlertsAndroid.Density;
			Assert.Equal(
				new[] { 1, 1, 2, 2, 4, 4, 3, 3 }.Select(radius => radius * density).ToArray(),
				GetCornerRadii());
		});
	}

	[Fact]
	public async Task NullAnchorLeavesTheMaterialAnchorViewUnset()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("No anchor", duration: longDuration).Show();

			Assert.Null(AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).AnchorView);
		});
	}

	[Fact]
	public async Task AnchorWithoutAPlatformViewLeavesTheMaterialAnchorViewUnset()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("Detached anchor", anchor: new Button(), duration: longDuration).Show();

			Assert.Null(AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).AnchorView);
		});
	}

	[Fact]
	public async Task AnchorWithAPlatformViewBecomesTheMaterialAnchorView()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		var anchor = new Button { Text = "Anchor", WidthRequest = 120, HeightRequest = 44 };

		await AttachAndRun(anchor, async handler =>
		{
			await Snackbar.Make("Anchored", anchor: anchor, duration: longDuration).Show();

			var anchorView = AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).AnchorView;
			Assert.NotNull(anchorView);
			Assert.Equal(((View)handler.PlatformView!).Handle, anchorView.Handle);
		});
	}

	[Fact]
	public async Task DisplaySnackbarExtensionUsesTheElementAsTheMaterialAnchorView()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		var anchor = new Button { Text = "Anchor", WidthRequest = 120, HeightRequest = 44 };

		await AttachAndRun(anchor, async handler =>
		{
			await anchor.DisplaySnackbar("From the extension", duration: longDuration);

			var anchorView = AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).AnchorView;
			Assert.NotNull(anchorView);
			Assert.Equal(((View)handler.PlatformView!).Handle, anchorView.Handle);
		});
	}

	// Material sets the bottom margin of an anchored snackbar, the distance from the bottom of its parent to the top of the anchor, when it
	// shows the snackbar, and sets it again after a layout pass only when its anchor layout listener is enabled. Without the listener the
	// snackbar stayed where it was shown: after a rotation to landscape the portrait margin was taller than the parent, so the snackbar was
	// laid out above the screen with no height while it was still shown.
	[Theory]
	[InlineData(120.0, 320.0)]
	[InlineData(320.0, 120.0)]
	public async Task AnchoredSnackbarStaysAboveItsAnchorWhenTheAnchorMoves(double spacerHeightWhenShown, double spacerHeightAfterwards)
	{
		SetupBuilder();
		await PreparePlatformAsync();
		var spacer = new BoxView { WidthRequest = 120, HeightRequest = spacerHeightWhenShown };
		var anchor = new Button { Text = "Anchor", WidthRequest = 120, HeightRequest = 44 };

		await AttachAndRun(new VerticalStackLayout { Children = { spacer, anchor } }, async _ =>
		{
			await Snackbar.Make("Follows its anchor", anchor: anchor, duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
			var anchorView = (View)anchor.Handler!.PlatformView!;
			var snackbarView = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
			var anchorTopWhenShown = GetTopOnScreen(anchorView);
			var gapWhenShown = anchorTopWhenShown - GetBottomOnScreen(snackbarView);
			Assert.True(gapWhenShown >= 0, $"The snackbar was shown over its anchor: its bottom is {-gapWhenShown} px below the top of the anchor.");

			spacer.HeightRequest = spacerHeightAfterwards;
			await AssertEventually(() => GetTopOnScreen(anchorView) != anchorTopWhenShown, timeout: 10000, message: "The anchor did not move.");

			var isAboveTheAnchor = await Wait(() => snackbarView.Height > 0 && GetTopOnScreen(anchorView) - GetBottomOnScreen(snackbarView) == gapWhenShown, timeout: 10000);
			Assert.True(isAboveTheAnchor, $"The snackbar did not follow its anchor, whose top moved from {anchorTopWhenShown} to {GetTopOnScreen(anchorView)} px on screen: the snackbar's bottom is at {GetBottomOnScreen(snackbarView)} px and its height is {snackbarView.Height} px, where it should end {gapWhenShown} px above the anchor, as when it was shown.");
		});
	}

	[Fact]
	public async Task DisplaySnackbarExtensionForwardsTheVisualOptionsAndTheDurationToThePlatform()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			VisualElement? anchor = null;
			var options = new SnackbarOptions
			{
				TextColor = Color.FromRgb(10, 20, 200),
				ActionButtonTextColor = Color.FromRgb(30, 200, 40)
			};

			await anchor.DisplaySnackbar("Styled by the extension", () => { }, "Act", TimeSpan.FromMilliseconds(12345), options);
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
			Assert.Equal(Color.FromRgb(10, 20, 200).ToPlatform().ToArgb(), AlertsAndroid.GetMessageView(layout).CurrentTextColor);
			Assert.Equal(Color.FromRgb(30, 200, 40).ToPlatform().ToArgb(), AlertsAndroid.GetActionView(layout).CurrentTextColor);
			Assert.Equal("Act", AlertsAndroid.GetActionView(layout).Text);
			Assert.Equal(12345, AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).Duration);
		});
	}

	[Fact]
	public async Task PageAnchorIsIgnored()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		var page = new ContentPage { Content = new Label { Text = "Page anchor" } };

		await AttachAndRun(page, async handler =>
		{
			Assert.NotNull(handler.PlatformView);

			await Snackbar.Make("Page anchor", anchor: page, duration: longDuration).Show();

			Assert.Null(AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).AnchorView);
		});
	}

	[Fact]
	public async Task SnackbarAboveAModalPageAddsTheNavigationBarHeightToTheBottomMargin()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var navigationBarHeight = GetNavigationBarHeight();
			Skip.IfNot(navigationBarHeight > 0, "The device reports a navigation bar height of 0, so the modal margin cannot be told apart.");
			var mainPage = (Page)Application.Current!.Windows[0].Page!;

			await Snackbar.Make("Baseline", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The baseline snackbar was never shown.");
			var baselineMargin = GetBottomMargin(Assert.Single(AlertsAndroid.GetSnackbarLayouts()));
			await Snackbar.Make("Done", duration: TimeSpan.FromMilliseconds(100)).Dismiss();
			await AssertEventually(() => AlertsAndroid.GetSnackbarLayouts().Count == 0, timeout: 10000, message: "The baseline snackbar did not go away.");

			var modalPage = new ContentPage { Content = new Label { Text = "Modal" } };
			modalPage.ToHandler(MauiContext);
			var modalView = (View)modalPage.Handler!.PlatformView!;
			var host = new FrameLayout(AlertsAndroid.Activity);
			AlertsAndroid.Content.AddView(host);
			host.AddView(modalView);
			try
			{
				await mainPage.Navigation.PushModalAsync(modalPage, false);
				Assert.Single(mainPage.Navigation.ModalStack);

				await Snackbar.Make("Over the modal", duration: longDuration).Show();
				await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The modal snackbar was never shown.");

				var modalMargin = GetBottomMargin(Assert.Single(AlertsAndroid.GetSnackbarLayouts()));
				Assert.Equal(baselineMargin + navigationBarHeight, modalMargin);
			}
			finally
			{
				host.RemoveAllViews();
				AlertsAndroid.Content.RemoveView(host);
				modalPage.Handler?.DisconnectHandler();
			}
		});
	}

	[Fact]
	public async Task ModalPageWithoutAPlatformViewIsTreatedLikeNoModalPage()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var mainPage = (Page)Application.Current!.Windows[0].Page!;
			await Snackbar.Make("Baseline", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The baseline snackbar was never shown.");
			var baselineMargin = GetBottomMargin(Assert.Single(AlertsAndroid.GetSnackbarLayouts()));
			await Snackbar.Make("Done").Dismiss();
			await AssertEventually(() => AlertsAndroid.GetSnackbarLayouts().Count == 0, timeout: 10000, message: "The baseline snackbar did not go away.");

			await mainPage.Navigation.PushModalAsync(new ContentPage(), false);
			Assert.Single(mainPage.Navigation.ModalStack);
			Assert.Null(mainPage.Navigation.ModalStack[^1].Handler);

			await Snackbar.Make("Modal without a view", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			Assert.Equal(baselineMargin, GetBottomMargin(Assert.Single(AlertsAndroid.GetSnackbarLayouts())));
		});
	}

	[Fact]
	public async Task WindowWithoutAPageDisplaysTheSnackbarInTheActivityContent()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			InstallTestApplication(null);

			await Snackbar.Make("No page", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			Assert.Equal(new[] { "No page" }, await GetVisibleMessagesAsync());
		});
	}

	[Fact]
	public async Task ShowWithoutAnApplicationThrowsBecauseTheFontManagerIsMissing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			RememberApplicationToRestore();
			Application.Current = null;

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Snackbar.Make("No application", duration: longDuration).Show());

			Assert.Equal("IFontManager Required", exception.Message);
			Assert.Empty(AlertsAndroid.GetSnackbarLayouts());
		});
	}

	[Fact]
	public async Task ShowWithAnApplicationWithoutAHandlerThrowsBecauseTheFontManagerIsMissing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			InstallTestApplication(new ContentPage(), installHandler: false);

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Snackbar.Make("No handler", duration: longDuration).Show());

			Assert.Equal("IFontManager Required", exception.Message);
		});
	}

	[Fact]
	public async Task ShowWithoutAnyMauiWindowFallsBackToTheActivityContent()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			InstallTestApplication(null, createWindow: false);

			await Snackbar.Make("No window", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			Assert.Equal(new[] { "No window" }, await GetVisibleMessagesAsync());
		});
	}

	[Fact]
	public async Task DisposeReleasesThePlatformSnackbarPeer()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = new Snackbar { Text = "Dispose", Duration = longDuration };
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
			var platformSnackbar = AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar());
			Assert.NotEqual(IntPtr.Zero, platformSnackbar.Handle);
			await TakeTheSnackbarOffScreenBeforeItIsDisposedAsync();

			snackbar.Dispose();

			Assert.Equal(IntPtr.Zero, platformSnackbar.Handle);
		});
	}

	[Fact]
	public async Task DisposeWithoutDisposingManagedStateKeepsThePlatformSnackbarPeer()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = new DisposableSnackbar { Text = "Finalizer path", Duration = longDuration };
			await snackbar.Show();
			var platformSnackbar = AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar());

			snackbar.CallDispose(false);

			Assert.NotEqual(IntPtr.Zero, platformSnackbar.Handle);
		});
	}

	[Fact]
	public async Task SecondDisposeOfTheSameSnackbarDoesNotDisposeALaterPlatformSnackbar()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = new Snackbar { Text = "First", Duration = longDuration };
			await first.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The first snackbar was never shown.");
			await TakeTheSnackbarOffScreenBeforeItIsDisposedAsync();
			first.Dispose();
			await new Snackbar { Text = "Second", Duration = longDuration }.Show();
			var secondPlatformSnackbar = AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar());

			first.Dispose();

			Assert.NotEqual(IntPtr.Zero, secondPlatformSnackbar.Handle);
		});
	}

	[Fact]
	public async Task DismissAfterTheNativePeerWasDisposedCompletesOnceTheSnackbarTimesOut()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			var snackbar = new Snackbar { Text = "Disposed peer", Duration = TimeSpan.FromMilliseconds(800) };
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
			snackbar.Dispose();

			await snackbar.Dismiss().WaitAsync(TimeSpan.FromSeconds(10));

			// The awaiting code may run before the callback has updated IsShown and raised Dismissed, so wait for them.
			await AssertEventually(() => recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: "Dismissed was not raised once the snackbar timed out.");
		});
	}

	[Fact]
	public async Task SnackbarDoesNotLeakOnceAnotherSnackbarHasReplacedIt()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		WeakReference snackbarReference = null!, platformReference = null!;

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = new Snackbar { Text = "Leak", Action = () => { }, Duration = longDuration };
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
			snackbarReference = new WeakReference(snackbar);
			platformReference = new WeakReference(GetPlatformSnackbar());
			await snackbar.Dismiss().WaitAsync(dismissTimeout);

			// The last snackbar stays referenced by the static platform snackbar, so show another one.
			await Snackbar.Make("Replacement", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The replacement was never shown.");
		});

		await AssertionExtensions.WaitForGC(snackbarReference, platformReference);
	}

	[Fact]
	public async Task ShowWithAHandlerWithoutAMauiContextThrowsBecauseTheFontManagerIsMissing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			InstallTestApplication(new ContentPage(), installHandler: false).Handler = new StubApplicationHandler(null);

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Snackbar.Make("No context", duration: longDuration).Show());

			Assert.Equal("IFontManager Required", exception.Message);
			Assert.Empty(AlertsAndroid.GetSnackbarLayouts());
		});
	}

	[Fact]
	public async Task ShowWithoutAFontManagerServiceThrowsFromTheServiceProvider()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var services = new ServiceCollection().BuildServiceProvider();
			InstallTestApplication(new ContentPage(), installHandler: false).Handler = new StubApplicationHandler(new DeviceTestMauiContext(services));

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Snackbar.Make("No service", duration: longDuration).Show());

			Assert.NotEqual("IFontManager Required", exception.Message);
			Assert.Contains(nameof(IFontManager), exception.Message);
			Assert.Empty(AlertsAndroid.GetSnackbarLayouts());
		});
	}

	[Fact]
	public async Task AnchorWhosePlatformViewIsNotAViewLeavesTheMaterialAnchorViewUnset()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var anchor = new StubView { Handler = new StubViewHandler(MauiContext) };
			Assert.NotNull(anchor.Handler.PlatformView);
			Assert.False(anchor.Handler.PlatformView is View);

			await Snackbar.Make("Odd anchor", anchor: anchor, duration: longDuration).Show();

			Assert.Null(AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).AnchorView);
		});
	}

	[Fact]
	public async Task ModalPageWhosePlatformViewIsNotAViewIsTreatedLikeNoModalPage()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var mainPage = (Page)Application.Current!.Windows[0].Page!;
			await Snackbar.Make("Baseline", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The baseline snackbar was never shown.");
			var baselineMargin = GetBottomMargin(Assert.Single(AlertsAndroid.GetSnackbarLayouts()));
			await Snackbar.Make("Done").Dismiss();
			await AssertEventually(() => AlertsAndroid.GetSnackbarLayouts().Count == 0, timeout: 10000, message: "The baseline snackbar did not go away.");

			var modalPage = new ContentPage { Handler = new StubViewHandler(MauiContext) };
			await mainPage.Navigation.PushModalAsync(modalPage, false);
			Assert.Single(mainPage.Navigation.ModalStack);
			Assert.False(mainPage.Navigation.ModalStack[^1].Handler!.PlatformView is View);

			await Snackbar.Make("Modal with a foreign handler", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			Assert.Equal(baselineMargin, GetBottomMargin(Assert.Single(AlertsAndroid.GetSnackbarLayouts())));
		});
	}

	// A modal page that sits in a CoordinatorLayout (as in Shell) gets a snackbar whose layout parameters are CoordinatorLayout.LayoutParams,
	// which the navigation bar compensation does not handle.
	[Fact]
	public async Task SnackbarAboveAModalPageInACoordinatorLayoutKeepsTheMarginsMaterialGaveIt()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			Skip.IfNot(GetNavigationBarHeight() > 0, "The device reports a navigation bar height of 0, so a compensated margin cannot be told apart.");
			var mainPage = (Page)Application.Current!.Windows[0].Page!;
			var modalPage = new ContentPage { Content = new Label { Text = "Modal in a coordinator" } };
			modalPage.ToHandler(MauiContext);
			var modalView = (View)modalPage.Handler!.PlatformView!;
			var coordinator = new CoordinatorLayout(AlertsAndroid.Activity);
			AlertsAndroid.Content.AddView(coordinator);
			coordinator.AddView(modalView);
			try
			{
				var nativeSnackbar = MaterialSnackbar.Make(modalView, "Native", MaterialSnackbar.LengthIndefinite)!;
				nativeSnackbar.Show();
				await AssertEventually(() => AlertsAndroid.GetSnackbarLayouts().Count == 1, timeout: 10000, message: "The native snackbar was never shown.");
				var nativeMargin = Assert.IsType<CoordinatorLayout.LayoutParams>(Assert.Single(AlertsAndroid.GetSnackbarLayouts()).LayoutParameters).BottomMargin;
				nativeSnackbar.Dismiss();
				await AssertEventually(() => AlertsAndroid.GetSnackbarLayouts().Count == 0, timeout: 10000, message: "The native snackbar did not go away.");

				await mainPage.Navigation.PushModalAsync(modalPage, false);
				await Snackbar.Make("Over the modal", duration: longDuration).Show();
				await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The modal snackbar was never shown.");

				var layoutParameters = Assert.IsType<CoordinatorLayout.LayoutParams>(Assert.Single(AlertsAndroid.GetSnackbarLayouts()).LayoutParameters);
				Assert.Equal(nativeMargin, layoutParameters.BottomMargin);
			}
			finally
			{
				coordinator.RemoveAllViews();
				AlertsAndroid.Content.RemoveView(coordinator);
				modalPage.Handler?.DisconnectHandler();
			}
		});
	}

	[Fact]
	public async Task ShowWithoutACurrentActivityThrowsBecauseThereIsNoParentView()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using (AlertsAndroid.WithoutCurrentActivity())
			{
				var exception = await Assert.ThrowsAsync<NotSupportedException>(() => Snackbar.Make("No activity", duration: longDuration).Show());

				Assert.Equal("Unable to retrieve Snackbar parent", exception.Message);
			}

			Assert.Empty(AlertsAndroid.GetSnackbarLayouts());
			Assert.False(Snackbar.IsShown);
		});
	}

	[Fact]
	public async Task CancellingTheTokenWhileDismissWaitsForTheAnimationThrowsOperationCanceled()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = Snackbar.Make("Waiting", duration: longDuration);
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
			using var cancellationTokenSource = new CancellationTokenSource();

			var dismissTask = snackbar.Dismiss(cancellationTokenSource.Token);
			Assert.False(dismissTask.IsCompleted, "Dismiss completed before Material reported the dismissal.");

			// Cancel, not CancelAsync: CancelAsync runs the token's callbacks on the thread pool, and while it awaits them the UI thread can
			// finish Material's dismissal first (a loaded emulator did), so that Dismiss completes before it sees the cancellation.
			cancellationTokenSource.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dismissTask);
		});
	}

	[Fact]
	public async Task DismissFromAnotherInstanceReturnsAtOnceAndDismissesTheShownSnackbar()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var recorder = new SnackbarEventRecorder();
			await Snackbar.Make("Shown", duration: longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var dismissTask = Snackbar.Make("Other instance").Dismiss();

			Assert.True(dismissTask.IsCompleted, "An instance that did not show the platform snackbar has nothing to wait for.");
			await dismissTask;
			await AssertEventually(() => AlertsAndroid.GetSnackbarLayouts().Count == 0 && recorder.DismissedCount == 1 && !Snackbar.IsShown, timeout: 10000, message: "The shown snackbar was not dismissed.");
		});
	}

	[Fact]
	public async Task DisposeAfterTheSnackbarWasDismissedDoesNotThrow()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var snackbar = Snackbar.Make("Dismissed, then disposed", duration: longDuration);
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
			await snackbar.Dismiss().WaitAsync(dismissTimeout);
			await AssertEventually(() => !Snackbar.IsShown, timeout: 10000, message: "The snackbar was never dismissed.");

			var exception = Record.Exception(snackbar.Dispose);

			Assert.Null(exception);
		});
	}

	// An app whose theme is Material 3 (Theme.Material3.*) gets snackbars whose background Material builds as a MaterialShapeDrawable,
	// not as the GradientDrawable of the Material Components themes. Here the snackbar's parent is a CoordinatorLayout themed with
	// Theme.Material3.DayNight that hosts a modal page; under a Material 3 activity theme every snackbar's parent is themed so.
	[Fact]
	public async Task BackgroundColorIsRenderedUnderAMaterial3Theme()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var mainPage = (Page)Application.Current!.Windows[0].Page!;
			var modalPage = new ContentPage { Content = new Label { Text = "Material 3" } };
			modalPage.ToHandler(MauiContext);
			var modalView = (View)modalPage.Handler!.PlatformView!;
			var coordinator = new CoordinatorLayout(new ContextThemeWrapper(AlertsAndroid.Activity, Resource.Style.Theme_Material3_DayNight));
			AlertsAndroid.Content.AddView(coordinator);
			coordinator.AddView(modalView);
			try
			{
				await mainPage.Navigation.PushModalAsync(modalPage, false);
				var options = new SnackbarOptions { BackgroundColor = Color.FromRgb(200, 10, 20), CornerRadius = new CornerRadius(8) };

				await Snackbar.Make("Material 3 parent", () => { }, "Act", longDuration, options).Show();
				await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

				var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
				await AssertEventually(() => layout.Width > 0 && layout.Height > 0, timeout: 10000, message: "The snackbar was never laid out.");
				using var bitmap = AlertsAndroid.Render(layout);
				var expected = new Android.Graphics.Color(200, 10, 20).ToArgb();
				var actual = bitmap.GetPixel(bitmap.Width / 2, 6);
				Assert.True(expected == actual, $"The snackbar is drawn in #{actual:X8} instead of #{expected:X8}; its background is a {layout.Background?.Class.Name}.");
			}
			finally
			{
				coordinator.RemoveAllViews();
				AlertsAndroid.Content.RemoveView(coordinator);
				modalPage.Handler?.DisconnectHandler();
			}
		});
	}

	// The setup is the one of BackgroundColorIsRenderedUnderAMaterial3Theme.
	[Fact]
	public async Task CornerRadiiAreAppliedToTheMatchingCornersUnderAMaterial3Theme()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var mainPage = (Page)Application.Current!.Windows[0].Page!;
			var modalPage = new ContentPage { Content = new Label { Text = "Material 3" } };
			modalPage.ToHandler(MauiContext);
			var modalView = (View)modalPage.Handler!.PlatformView!;
			var coordinator = new CoordinatorLayout(new ContextThemeWrapper(AlertsAndroid.Activity, Resource.Style.Theme_Material3_DayNight));
			AlertsAndroid.Content.AddView(coordinator);
			coordinator.AddView(modalView);
			try
			{
				await mainPage.Navigation.PushModalAsync(modalPage, false);
				var options = new SnackbarOptions { CornerRadius = new CornerRadius(1, 2, 3, 4) };

				await Snackbar.Make("Material 3 corners", () => { }, "Act", longDuration, options).Show();
				await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

				var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
				var shapeAppearance = Assert.IsType<MaterialShapeDrawable>(layout.Background, exactMatch: false).ShapeAppearanceModel;

				// The sizes are absolute: the bounds that GetCornerSize takes do not change them.
				using var bounds = new Android.Graphics.RectF(0, 0, 1000, 1000);
				var density = AlertsAndroid.Density;
				Assert.Equal(
					new[] { 1, 2, 4, 3 }.Select(radius => radius * density).ToArray(),
					new[] { shapeAppearance.TopLeftCornerSize, shapeAppearance.TopRightCornerSize, shapeAppearance.BottomRightCornerSize, shapeAppearance.BottomLeftCornerSize }.Select(cornerSize => cornerSize.GetCornerSize(bounds)).ToArray());
			}
			finally
			{
				coordinator.RemoveAllViews();
				AlertsAndroid.Content.RemoveView(coordinator);
				modalPage.Handler?.DisconnectHandler();
			}
		});
	}

	// Unlike Toast (and unlike iOS and Mac Catalyst), Snackbar can be shown from a background thread on Android: Material inflates and
	// styles views that are not attached yet, and its SnackbarManager attaches them on the main thread.
	[Fact]
	public async Task ShowOffTheUiThreadDisplaysTheSnackbar()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await Task.Run(() => Snackbar.Make("Off the UI thread", duration: longDuration).Show());

		await InvokeOnMainThreadAsync(async () =>
		{
			await AssertEventually(() => Snackbar.IsShown && AlertsAndroid.GetSnackbarLayouts().Count == 1, timeout: 10000, message: "The snackbar shown from a background thread was never displayed.");
			Assert.Equal(new[] { "Off the UI thread" }, await GetVisibleMessagesAsync());
		});
	}

	// Material's SnackbarManager posts the dismissal to the main thread, and Dismiss waits for the callback that reports it.
	[Fact]
	public async Task DismissOffTheUiThreadDismissesTheSnackbar()
	{
		SetupBuilder();
		await PreparePlatformAsync();
		using var recorder = new SnackbarEventRecorder();
		var snackbar = Snackbar.Make("Dismissed off the UI thread", duration: longDuration);
		await InvokeOnMainThreadAsync(async () =>
		{
			await snackbar.Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");
		});

		await Task.Run(() => snackbar.Dismiss()).WaitAsync(dismissTimeout);

		await InvokeOnMainThreadAsync(async () =>
		{
			await AssertEventually(() => AlertsAndroid.GetSnackbarLayouts().Count == 0 && !Snackbar.IsShown, timeout: 10000, message: "The snackbar dismissed from a background thread stayed on screen.");
			Assert.Equal(new[] { nameof(Snackbar.Shown), nameof(Snackbar.Dismissed) }, recorder.Sequence);
		});
	}

	// iOS and Mac Catalyst keep an empty, padded action button for this input (SnackbarTests.EmptyActionButtonTextKeepsAnEmptyActionButton):
	// the platforms differ.
	[Fact]
	public async Task EmptyActionButtonTextHidesTheActionButton()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await Snackbar.Make("No action text", () => { }, string.Empty, longDuration).Show();
			await AssertEventually(() => Snackbar.IsShown, timeout: 10000, message: "The snackbar was never shown.");

			var layout = Assert.Single(AlertsAndroid.GetSnackbarLayouts());
			Assert.Equal(ViewStates.Gone, AlertsAndroid.GetActionView(layout).Visibility);
			Assert.Equal("No action text", AlertsAndroid.GetMessageView(layout).Text);
		});
	}

	static float[] GetCornerRadii()
	{
		var background = Assert.IsType<GradientDrawable>(AlertsAndroid.GetNativeSnackbar(GetPlatformSnackbar()).View.Background);
		return background.GetCornerRadii() ?? throw new InvalidOperationException("The snackbar background has no corner radii.");
	}

	static int GetBottomMargin(View snackbarView) => Assert.IsType<FrameLayout.LayoutParams>(snackbarView.LayoutParameters).BottomMargin;

	static int GetTopOnScreen(View view)
	{
		var location = new int[2];
		view.GetLocationOnScreen(location);
		return location[1];
	}

	static int GetBottomOnScreen(View view) => GetTopOnScreen(view) + view.Height;

	static int GetNavigationBarHeight()
	{
		var resources = AlertsAndroid.Activity.Resources!;
		var identifier = resources.GetIdentifier("navigation_bar_height", "dimen", "android");
		return identifier > 0 ? resources.GetDimensionPixelSize(identifier) : 0;
	}

	// A subclass can reach the protected Dispose(bool): a Snackbar has no finalizer, so the false overload is otherwise unreachable.
	sealed class DisposableSnackbar : Snackbar
	{
		public void CallDispose(bool isDisposing) => Dispose(isDisposing);
	}
}