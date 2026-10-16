#pragma warning disable CA1422 // The status bar APIs of UIKit are deprecated; the toolkit still uses them, so the tests read what UIKit reports.
#pragma warning disable CS0618 // Page.UseSafeArea is obsolete in .NET MAUI 10 (SafeAreaEdges replaces it), but StatusBarBehavior still reads it, so the tests set it.
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using CoreGraphics;
using Microsoft.Maui.Devices;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// StatusBarBehavior's iOS-only code: it sizes the status bar view (StatusBar.SetBarSize, with the page's UseSafeArea setting) when it
// attaches to a loaded page, whenever the page's size changes and whenever the display changes, and stops doing so when it detaches.
// SetBarSize is the only code that moves an existing status bar view, so these tests give the views a frame that the toolkit never
// produces: a frame that changed proves that SetBarSize ran, whatever frame it computed.
// The status bar code changes every window of UIApplication.Windows, and that list is not only the runner's window: once any text input
// was first responder in the app run (another feature's test), UIKit keeps a second window of the scene listed for good. So the tests
// check each window's status bar view (AssertStatusBarShows and the helpers below) and never count the views of all windows together.
// The style: on .NET 10 the behavior calls StatusBar.SetStyle (UIApplication.SetStatusBarStyle, which the runner, a scene-based app, ignores);
// on .NET 11 it sets Window.StatusBarTheme of its page's window, which the tests show in a .NET MAUI window (RunInMauiWindowAsync).
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
			await GiveTheStatusBarViewsTheSentinelFrameAsync();

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
			await GiveTheStatusBarViewsTheSentinelFrameAsync(); // the views are red

			page.Behaviors.Add(behavior);

			Assert.True(behavior.IsAttached);
			AssertEveryStatusBarViewHasTheSentinelFrame();
			AssertStatusBarShows(Colors.Red);

			StatusBarEventRaisers.RaiseNavigatedTo(page); // applies the color, but only SetBarSize moves the view

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
			await GiveTheStatusBarViewsTheSentinelFrameAsync();

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
			await GiveTheStatusBarViewsTheSentinelFrameAsync();

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
			await GiveTheStatusBarViewsTheSentinelFrameAsync();

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
			await GiveTheStatusBarViewsTheSentinelFrameAsync();

			ResizePage(page);

			// The size hook is not gated by ApplyOn: it only moves the view, the color is still the one that was there.
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
			await GiveTheStatusBarViewsTheSentinelFrameAsync();

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
			await GiveTheStatusBarViewsTheSentinelFrameAsync();

			StatusBarEventRaisers.RaiseMainDisplayInfoChanged();

			AssertNoStatusBarViewHasTheSentinelFrame();

			page.Behaviors.Remove(behavior);
			await GiveTheStatusBarViewsTheSentinelFrameAsync();

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
			await GiveTheStatusBarViewsTheSentinelFrameAsync();

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
		await GiveTheStatusBarViewsTheSentinelFrameAsync();

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

#if NET11_0_OR_GREATER
	// .NET 11 (every iOS app is scene-based): the behavior sets Window.StatusBarTheme of its page's window, and UIKit takes the style from the
	// view controllers of .NET MAUI, which return the style of their window's theme.
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
			Assert.Equal(StatusBarTheme.Light, mauiWindow.Window.StatusBarTheme);

			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	// Each step changes the style that the step before set (LightContent after Default and after DarkContent, DarkContent after LightContent,
	// Default after LightContent), and only the style: the status bar views keep the color that the test gave them, not the behavior's.
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
	// first sets the window back to the default style: the style that follows comes from the navigation that the test raises.
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
			mauiWindow.Window.StatusBarTheme = StatusBarTheme.Default;
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.Default);

			StatusBarEventRaisers.RaiseNavigatedTo(page);

			Assert.Equal(StatusBarTheme.Light, mauiWindow.Window.StatusBarTheme);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	// A real navigation: the NavigationPage pushes the page with its animation, and .NET MAUI raises NavigatedTo when the push finished, after the
	// page loaded and the behavior attached. (Without the animation MAUI 11 RC1 raises NavigatedTo before the page loads, before the behavior
	// attaches and starts listening.)
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

	// The style belongs to the window of the behavior's page, not to the app: another window keeps its own.
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

	// The behavior has a status bar to change only while it is attached to a page of a window: before it is attached and after it was detached,
	// a new style changes no window.
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

			page.Behaviors.Add(behavior); // the page is loaded: the behavior attaches and applies its style

			Assert.True(behavior.IsAttached);
			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);

			page.Behaviors.Remove(behavior);
			Assert.False(behavior.IsAttached);

			behavior.StatusBarStyle = StatusBarStyle.DarkContent;

			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);
		});
	}

	// A page outside of any .NET MAUI window (its view hosted by native code, as with .NET MAUI embedding or the runner's AttachAndRun) has no
	// window whose status bar the behavior could change: it attaches, and its style changes nothing.
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

	// StatusBar.SetStyle throws for a StatusBarStyle that is not defined: the behavior does too, also while it has no window to set the style of.
	[Fact]
	public async Task UndefinedStyleThrowsNotSupportedExceptionWhileTheBehaviorIsNotAttached()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync();

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<NotSupportedException>(() => behavior.StatusBarStyle = (StatusBarStyle)42));

		Assert.Equal("StatusBarStyle 42 is not yet supported on iOS", exception.Message);
	}

	// UIKit asks the view controllers for the style only when the app's Info.plist does not set UIViewControllerBasedStatusBarAppearance to false
	// (with false the status bar keeps its style): the behavior still sets the theme of its window, and says that UIKit ignores it only for false.
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

	// The behavior reports the ignored style when it sets the theme of a window: without a window it sets none, and says nothing.
	[Fact]
	public async Task StyleOfABehaviorWithoutAWindowDoesNotTraceThatUIKitIgnoresIt()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			var listener = CaptureTraceWithViewControllerBasedStatusBarAppearance(false, () => behavior.StatusBarStyle = StatusBarStyle.LightContent);

			Assert.False(behavior.IsAttached);
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
			await GiveTheStatusBarViewsTheSentinelFrameAsync(); // the views are red

			await AssertUpdatesTheStatusBarAppearanceAsync(() => behavior.StatusBarStyle = StatusBarStyle.DarkContent, keepPresentingViewLoaded: true);

			// SetColor would have made the views transparent (the behavior's color): only SetStyle ran.
			AssertStatusBarShows(Colors.Red);
			Assert.True(behavior.IsAttached);
		});
	}
#endif

	// StatusBar.SetColor adds a status bar view to every window; the views get a frame that only SetBarSize replaces.
	static Task GiveTheStatusBarViewsTheSentinelFrameAsync() =>
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

	// With the page's UseSafeArea setting the status bar view is as high as the window's top safe area inset (SetBarSize(true)).
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