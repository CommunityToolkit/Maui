#pragma warning disable CA1422 // The status bar APIs of UIKit are deprecated; the toolkit still uses them, so the tests read what UIKit reports.
#pragma warning disable CS0618 // Page.UseSafeArea is obsolete in .NET MAUI 10 (SafeAreaEdges replaces it), but StatusBarBehavior still reads it, so the tests set it.
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using CoreGraphics;
using Microsoft.Maui.Devices;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// StatusBar.SetBarSize is the only code that moves an existing status bar view, so these tests give the views a frame that the toolkit
// never produces: a frame that changed proves that SetBarSize ran, whatever frame it computed.
// The style: on .NET 10 the behavior sets the style of the whole app (StatusBar.SetStyle), and on .NET 11 Window.StatusBarTheme of its page's
// window. The tests show the page in a .NET MAUI window (RunInMauiWindowAsync) and read the style of its status bar.
public partial class StatusBarBehaviorTests
{
	static readonly CGRect sentinelFrame = new(7, 11, 13, 17);

	[Fact]
	public async Task AttachSizesTheStatusBarViewWhenTheColorIsAppliedOnAttach()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			page.Behaviors.Add(behavior); // the page is loaded: the behavior attaches synchronously

			Assert.True(behavior.IsAttached);
			AssertNoStatusBarViewHasTheSentinelFrame();
		});
	}

	[Fact]
	public async Task AttachDoesNotSizeTheStatusBarViewWhenTheColorIsAppliedOnNavigation()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior =>
		{
			behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;
			behavior.StatusBarColor = Colors.Green;
		});
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			page.Behaviors.Add(behavior);

			Assert.True(behavior.IsAttached);
			AssertEveryStatusBarViewHasTheSentinelFrame();
			AssertStatusBarShows(Colors.Red);

			StatusBarEventRaisers.RaiseNavigatedTo(page);

			AssertEveryStatusBarViewHasTheSentinelFrame();
			AssertStatusBarShows(Colors.Green);
		});
	}

	[Fact]
	public async Task AttachSizesTheStatusBarViewWithTheSafeAreaOfThePage()
	{
		SetupBuilder();
		var page = CreatePage();
		Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetUseSafeArea(page, true);
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			page.Behaviors.Add(behavior);

			AssertEveryStatusBarViewHasTheSafeAreaHeight();
		});
	}

	[Fact]
	public async Task AttachSizesTheStatusBarViewToTheStatusBarOfTheScene()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, static _ => InvokeOnMainThreadAsync(static () =>
		{
			foreach (var window in IosStatusBarViews.WindowsWithStatusBar())
			{
				var expected = IosStatusBarViews.SceneStatusBarFrame(window) ?? throw new InvalidOperationException("The window has no status bar frame.");
				Assert.Equal(expected.ToString(), Assert.Single(IosStatusBarViews.TaggedSubviews(window)).Frame.ToString());
			}
		}));
	}

	[Fact]
	public async Task PageSizeChangeSizesTheStatusBarViewWhileTheBehaviorIsAttached()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			page.Behaviors.Add(behavior);
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			ResizePage(page);

			AssertNoStatusBarViewHasTheSentinelFrame();
		});
	}

	[Fact]
	public async Task PageSizeChangeUsesTheSafeAreaOfThePage()
	{
		SetupBuilder();
		var page = CreatePage();
		Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetUseSafeArea(page, true);
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			page.Behaviors.Add(behavior);
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			ResizePage(page);

			AssertEveryStatusBarViewHasTheSafeAreaHeight();
		});
	}

	[Fact]
	public async Task PageSizeChangeSizesTheStatusBarViewEvenWhenTheColorIsAppliedOnNavigation()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo);
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			page.Behaviors.Add(behavior);
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			ResizePage(page);

			// The size hook only moves the view: the color is still the one that was there.
			AssertNoStatusBarViewHasTheSentinelFrame();
			AssertStatusBarShows(Colors.Red);
		});
	}

	[Fact]
	public async Task PageSizeChangeDoesNotSizeTheStatusBarViewAfterTheBehaviorDetached()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			page.Behaviors.Add(behavior);
			page.Behaviors.Remove(behavior);
			Assert.False(behavior.IsAttached);
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			ResizePage(page);

			AssertEveryStatusBarViewHasTheSentinelFrame();
		});
	}

	[Fact]
	public async Task DisplayInfoChangeSizesTheStatusBarViewOnlyWhileTheBehaviorIsAttached()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			page.Behaviors.Add(behavior);
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			StatusBarEventRaisers.RaiseMainDisplayInfoChanged();

			AssertNoStatusBarViewHasTheSentinelFrame();

			page.Behaviors.Remove(behavior);
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			StatusBarEventRaisers.RaiseMainDisplayInfoChanged();

			AssertEveryStatusBarViewHasTheSentinelFrame();
		});
	}

	[Fact]
	public async Task DisplayInfoChangeUsesTheSafeAreaOfThePage()
	{
		SetupBuilder();
		var page = CreatePage();
		Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetUseSafeArea(page, true);
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			page.Behaviors.Add(behavior);
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			StatusBarEventRaisers.RaiseMainDisplayInfoChanged();

			AssertEveryStatusBarViewHasTheSafeAreaHeight();
		});
	}

	[Fact]
	public async Task DisplayInfoChangeOfABehaviorWithoutPageDoesNothing()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();
		await AddRedStatusBarViewsWithTheSentinelFrameAsync();

		// The behavior only listens while it is attached to a page; calling its handler directly shows what the page check is for.
		await InvokeOnMainThreadAsync(() =>
		{
			StatusBarEventRaisers.InvokeBehaviorMember(behavior, "OnMainDisplayInfoChanged", null, new DisplayInfoChangedEventArgs(DeviceDisplay.Current.MainDisplayInfo));

			AssertEveryStatusBarViewHasTheSentinelFrame();
		});
	}

	[Fact]
	public async Task PageSizeChangedHandlerWithoutSenderThrowsArgumentNullException()
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(static () =>
			Assert.Throws<ArgumentNullException>(static () => StatusBarEventRaisers.InvokeBehaviorMember(null, "OnPageSizeChanged", null, EventArgs.Empty)));

		Assert.Equal("sender", exception.ParamName);
	}

	[Fact]
	public async Task PageSizeChangedHandlerWithASenderThatIsNotAPageThrowsInvalidCastException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(static () =>
			Assert.Throws<InvalidCastException>(static () => StatusBarEventRaisers.InvokeBehaviorMember(null, "OnPageSizeChanged", new object(), EventArgs.Empty)));
	}

	// UIKit takes the style from the view controllers: on .NET 11 those of .NET MAUI return the style of their window's theme, and on .NET 10 every
	// view controller that does not choose a style returns the style of the app.
	[Theory]
	[InlineData(StatusBarWindowPage.ContentPage)]
	[InlineData(StatusBarWindowPage.NavigationPage)]
	[InlineData(StatusBarWindowPage.Shell)]
	[InlineData(StatusBarWindowPage.TabbedPage)]
	[InlineData(StatusBarWindowPage.FlyoutPage)]
	[InlineData(StatusBarWindowPage.ModalPage)]
	public async Task StyleAppliedOnAttachReachesTheStatusBarOfTheWindow(StatusBarWindowPage kind)
	{
		SetupBuilder();
		var page = CreateStyleTestPage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarStyle = StatusBarStyle.DarkContent);
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await RunInMauiWindowAsync(kind, page, async mauiWindow =>
		{
			Assert.True(behavior.IsAttached);
#if NET11_0_OR_GREATER
			Assert.Equal(StatusBarTheme.Light, mauiWindow.Window.StatusBarTheme);
#endif

			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	// Each step changes the style that the step before set, and only the style: the status bar views keep the color that the test gave them,
	// not the behavior's.
	[Theory]
	[InlineData(StatusBarWindowPage.ContentPage)]
	[InlineData(StatusBarWindowPage.NavigationPage)]
	[InlineData(StatusBarWindowPage.Shell)]
	[InlineData(StatusBarWindowPage.TabbedPage)]
	[InlineData(StatusBarWindowPage.FlyoutPage)]
	[InlineData(StatusBarWindowPage.ModalPage)]
	public async Task StyleChangeWhileAttachedReachesTheStatusBarOfTheWindow(StatusBarWindowPage kind)
	{
		SetupBuilder();
		var page = CreateStyleTestPage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await RunInMauiWindowAsync(kind, page, async mauiWindow =>
		{
			Assert.True(behavior.IsAttached);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.Default);
			StatusBar.SetColor(Colors.Red);

			foreach (var (style, expected) in new[]
			{
				(StatusBarStyle.LightContent, UIStatusBarStyle.LightContent),
				(StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent),
				(StatusBarStyle.LightContent, UIStatusBarStyle.LightContent),
				(StatusBarStyle.Default, UIStatusBarStyle.Default),
			})
			{
				behavior.StatusBarStyle = style;

				await AssertStatusBarStyleAsync(mauiWindow, expected);
			}

			AssertStatusBarShows(Colors.Red);
		});
	}

	// .NET MAUI raises NavigatedTo itself when it shows the page of a NavigationPage, Shell, TabbedPage, FlyoutPage or modal page, so the test
	// first sets the window (.NET 10: the app) back to the default style: the style that follows comes from the navigation that the test raises.
	[Theory]
	[InlineData(StatusBarWindowPage.ContentPage)]
	[InlineData(StatusBarWindowPage.NavigationPage)]
	[InlineData(StatusBarWindowPage.Shell)]
	[InlineData(StatusBarWindowPage.TabbedPage)]
	[InlineData(StatusBarWindowPage.FlyoutPage)]
	[InlineData(StatusBarWindowPage.ModalPage)]
	public async Task StyleAppliedOnNavigationReachesTheStatusBarOfTheWindow(StatusBarWindowPage kind)
	{
		SetupBuilder();
		var page = CreateStyleTestPage();
		var behavior = await CreateBehaviorAsync(static behavior =>
		{
			behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;
			behavior.StatusBarStyle = StatusBarStyle.DarkContent;
		});
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await RunInMauiWindowAsync(kind, page, async mauiWindow =>
		{
			Assert.True(behavior.IsAttached);
#if NET11_0_OR_GREATER
			mauiWindow.Window.StatusBarTheme = StatusBarTheme.Default;
#else
			StatusBar.SetStyle(StatusBarStyle.Default);
#endif
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.Default);

			StatusBarEventRaisers.RaiseNavigatedTo(page);

#if NET11_0_OR_GREATER
			Assert.Equal(StatusBarTheme.Light, mauiWindow.Window.StatusBarTheme);
#endif
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	// The push is animated: .NET MAUI then raises NavigatedTo when the push finished, after the page loaded and the behavior attached.
	// Without the animation MAUI 11 RC1 raises NavigatedTo before the page loads, before the behavior attaches and starts listening.
	// On .NET 10 the behavior already set the style of the app when the test configured it, so the reset sets Default again.
	[Fact]
	public async Task StyleAppliedOnNavigationReachesTheStatusBarWhenANavigationPagePushesThePage()
	{
		SetupBuilder();
		var pushedPage = CreateStyleTestPage();
		var behavior = await CreateBehaviorAsync(static behavior =>
		{
			behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;
			behavior.StatusBarStyle = StatusBarStyle.DarkContent;
		});
		await ResetNativeStateAsync();
		pushedPage.Behaviors.Add(behavior);
		var navigatedTo = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		pushedPage.NavigatedTo += (_, _) => navigatedTo.TrySetResult();

		await RunInMauiWindowAsync(StatusBarWindowPage.NavigationPage, CreateStyleTestPage(), async mauiWindow =>
		{
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.Default);

			await Assert.IsType<NavigationPage>(mauiWindow.Window.Page).PushAsync(pushedPage, true);
			await navigatedTo.Task.WaitAsync(TimeSpan.FromSeconds(10));

			Assert.True(behavior.IsAttached);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

#if NET11_0_OR_GREATER
	[Fact]
	public async Task StyleChangesOnlyTheStatusBarThemeOfTheWindowOfThePage()
	{
		SetupBuilder();
		var page = CreateStyleTestPage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, page, async mauiWindow =>
		{
			var otherWindow = mauiWindow.Application.CreateWindowFor(CreateStyleTestPage());

			behavior.StatusBarStyle = StatusBarStyle.LightContent;

			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);
			Assert.Equal(StatusBarTheme.Default, otherWindow.StatusBarTheme);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);
		});
	}

	[Fact]
	public async Task StyleChangeWhileTheBehaviorIsNotAttachedChangesNoWindow()
	{
		SetupBuilder();
		var page = CreateStyleTestPage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, page, async mauiWindow =>
		{
			behavior.StatusBarStyle = StatusBarStyle.LightContent;

			Assert.False(behavior.IsAttached);
			Assert.Equal(StatusBarTheme.Default, mauiWindow.Window.StatusBarTheme);

			page.Behaviors.Add(behavior);

			Assert.True(behavior.IsAttached);
			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);

			page.Behaviors.Remove(behavior);
			Assert.False(behavior.IsAttached);

			behavior.StatusBarStyle = StatusBarStyle.DarkContent;

			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);
		});
	}

	// A page whose view is hosted by native code (as with .NET MAUI embedding or the runner's AttachAndRun) has no window.
	[Fact]
	public async Task StyleOfAPageWithoutAWindowChangesNoStatusBar()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarStyle = StatusBarStyle.DarkContent);
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, _ =>
		{
			Assert.True(behavior.IsAttached);
			Assert.Null(page.Window);

			behavior.StatusBarStyle = StatusBarStyle.LightContent;

			Assert.Equal(UIStatusBarStyle.Default, IosStatusBarViews.RunnerScene.StatusBarManager?.StatusBarStyle);
			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UndefinedStyleThrowsNotSupportedExceptionWhileTheBehaviorIsNotAttached()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync();

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<NotSupportedException>(() => behavior.StatusBarStyle = (StatusBarStyle)42));

		Assert.Equal("StatusBarStyle 42 is not yet supported on iOS", exception.Message);
	}

	// UIKit asks the view controllers for the style only when the app's Info.plist does not set UIViewControllerBasedStatusBarAppearance to
	// false (with false it shows the style of the whole app, which the runner ignores).
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(null!, false)]
	public async Task StyleTracesThatUIKitIgnoresItOnlyWhenInfoPlistTurnsViewControllerBasedStatusBarAppearanceOff(bool? viewControllerBasedStatusBarAppearance, bool isTraced)
	{
		SetupBuilder();
		var page = CreateStyleTestPage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, page, mauiWindow =>
		{
			var listener = CaptureTraceWithViewControllerBasedStatusBarAppearance(viewControllerBasedStatusBarAppearance, () => behavior.StatusBarStyle = StatusBarStyle.LightContent);

			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);
			Assert.Equal(isTraced, listener.Messages.Contains(styleIgnoredByUIKitMessage));
			return Task.CompletedTask;
		});
	}

	// With UIViewControllerBasedStatusBarAppearance set to false (the .NET 10 setup of the behavior) UIKit ignores the theme of the window and shows
	// the style of the whole app, which an app built with an iOS SDK before 27.0 takes from UIApplication.SetStatusBarStyle.
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(null!, false)]
	public async Task StyleSetsTheStyleOfTheWholeAppOnlyWhenInfoPlistTurnsViewControllerBasedStatusBarAppearanceOff(bool? viewControllerBasedStatusBarAppearance, bool setsTheStyleOfTheWholeApp)
	{
		SetupBuilder();
		var page = CreateStyleTestPage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, page, mauiWindow =>
		{
			var (listener, stylesOfTheWholeApp) = CaptureStylesOfTheWholeApp(viewControllerBasedStatusBarAppearance, isAppliedByUIKit: true, () => behavior.StatusBarStyle = StatusBarStyle.LightContent);

			UIStatusBarStyle[] expectedStylesOfTheWholeApp = setsTheStyleOfTheWholeApp ? [UIStatusBarStyle.LightContent] : [];
			Assert.Equal(expectedStylesOfTheWholeApp, stylesOfTheWholeApp);
			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);
			Assert.DoesNotContain(styleIgnoredByUIKitMessage, listener.Messages);
			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task StyleOfABehaviorWithoutAWindowDoesNotTraceThatUIKitIgnoresIt()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			var (listener, stylesOfTheWholeApp) = CaptureStylesOfTheWholeApp(false, isAppliedByUIKit: false, () => behavior.StatusBarStyle = StatusBarStyle.LightContent);

			Assert.False(behavior.IsAttached);
			Assert.Empty(stylesOfTheWholeApp);
			Assert.DoesNotContain(styleIgnoredByUIKitMessage, listener.Messages);
		});
	}
#else
	[Fact]
	public async Task StyleChangeWhileAttachedRequestsAStatusBarAppearanceUpdateWithoutChangingTheColor()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await AttachPageAndRun(page, async _ =>
		{
			page.Behaviors.Add(behavior);
			await AddRedStatusBarViewsWithTheSentinelFrameAsync();

			await AssertUpdatesTheStatusBarAppearanceAsync(() => behavior.StatusBarStyle = StatusBarStyle.DarkContent, keepPresentingViewLoaded: true);

			// SetColor would have made the views transparent (the behavior's color): only SetStyle ran.
			AssertStatusBarShows(Colors.Red);
			Assert.True(behavior.IsAttached);
		});
	}
#endif

	static Task AddRedStatusBarViewsWithTheSentinelFrameAsync() =>
		InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetColor(Colors.Red);

			var views = IosStatusBarViews.AllTaggedSubviews();
			Assert.NotEmpty(views);
			foreach (var view in views)
			{
				view.Frame = sentinelFrame;
			}
		});

	static void AssertEveryStatusBarViewHasTheSentinelFrame()
	{
		var views = IosStatusBarViews.AllTaggedSubviews();
		Assert.NotEmpty(views);
		Assert.All(views, static view => Assert.Equal(sentinelFrame.ToString(), view.Frame.ToString()));
	}

	static void AssertNoStatusBarViewHasTheSentinelFrame()
	{
		var views = IosStatusBarViews.AllTaggedSubviews();
		Assert.NotEmpty(views);
		Assert.All(views, static view => Assert.NotEqual(sentinelFrame.ToString(), view.Frame.ToString()));
	}

	static void AssertEveryStatusBarViewHasTheSafeAreaHeight()
	{
		var windows = IosStatusBarViews.WindowsWithStatusBar();
		Assert.NotEmpty(windows);
		foreach (var window in windows)
		{
			Assert.NotEqual(sentinelFrame.Height, window.SafeAreaInsets.Top);
			Assert.Equal(window.SafeAreaInsets.Top, Assert.Single(IosStatusBarViews.TaggedSubviews(window)).Frame.Height);
		}
	}

	static void ResizePage(Page page) =>
		page.Frame = new Rect(page.Frame.X, page.Frame.Y, page.Frame.Width + 10, page.Frame.Height + 10); // raises SizeChanged
}