#pragma warning disable CA1422 // UIApplication.SetStatusBarStyle/Windows are deprecated; the toolkit still uses them, so the tests read what UIKit reports.
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class StatusBarTests
{
	const string appearanceUpdateFailedMessage = "Unable to update Status Bar Appearance because Current UIViewController is null";

#if NET11_0_OR_GREATER
	const string windowsUnknownMessage = "Unable to set the Status Bar Style because the windows of the app are unknown: call UseMauiCommunityToolkit() in MauiProgram.cs";
#endif

	// UIApplication.Windows lists the windows of the app's connected scenes, and the runner has the one scene of the main screen, whose
	// StatusBarManager exists. A scene without one (an external display, CarPlay) only exists on a device that has such a screen.
	const string sceneWithoutStatusBarManagerMissing = "Needs a UIWindowScene without a StatusBarManager (the scene of an external display or CarPlay screen); the runner's only scene is the main screen's, which has one. SetColorSkipsListedWindowsWithoutASceneAndColorsTheOthers runs the same arm with a window that reports no scene.";

	static string UnsupportedStyleMessage => "StatusBarStyle 42 is not yet supported on iOS";

	[Theory]
	[InlineData(0xFFFF0000u)]
	[InlineData(0xFF0000FFu)]
	[InlineData(0x80336699u)]
	public async Task SetColorAddsAStatusBarViewWithTheColorToEveryWindow(uint argb)
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var color = Color.FromUint(argb);

		await InvokeOnMainThreadAsync(() =>
		{
			StatusBar.SetColor(color);

			var windows = IosStatusBarViews.WindowsWithStatusBar();
			Assert.NotEmpty(windows);
			foreach (var window in windows)
			{
				var statusBarView = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
				IosStatusBarViews.AssertColor(color, statusBarView.BackgroundColor);
				IosStatusBarViews.AssertColor(color, statusBarView.TintColor);
				Assert.Same(statusBarView, window.Subviews[^1]);
				Assert.Equal(IosStatusBarViews.SceneStatusBarFrame(window)?.ToString(), statusBarView.Frame.ToString());
			}
		});
	}

	[Fact]
	public async Task SetColorWithNullMakesTheStatusBarViewTransparent()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(static () => StatusBar.SetColor(Colors.Red));

		await InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetColor(null);

			foreach (var window in IosStatusBarViews.WindowsWithStatusBar())
			{
				var statusBarView = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
				IosStatusBarViews.AssertColor(Colors.Transparent, statusBarView.BackgroundColor);
				IosStatusBarViews.AssertColor(Colors.Transparent, statusBarView.TintColor);
			}
		});
	}

	[Fact]
	public async Task SetColorCalledAgainReusesTheStatusBarViewAndUpdatesItsColor()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetColor(Colors.Red);
			var window = IosStatusBarViews.WindowsWithStatusBar()[0];
			var first = Assert.Single(IosStatusBarViews.TaggedSubviews(window));

			StatusBar.SetColor(Colors.Green);

			var second = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
			Assert.Same(first, second);
			IosStatusBarViews.AssertColor(Colors.Green, second.BackgroundColor);
			IosStatusBarViews.AssertColor(Colors.Green, second.TintColor);
		});
	}

	[Fact]
	public async Task SetColorMovesTheStatusBarViewInFrontOfViewsAddedLater()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetColor(Colors.Red);
			var window = IosStatusBarViews.WindowsWithStatusBar()[0];
			var statusBarView = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
			using var coveringView = new UIView(window.Bounds);
			window.AddSubview(coveringView);
			Assert.Same(coveringView, window.Subviews[^1]);

			try
			{
				StatusBar.SetColor(Colors.Blue);

				Assert.Same(statusBarView, window.Subviews[^1]);
			}
			finally
			{
				coveringView.RemoveFromSuperview();
			}
		});
	}

	[Fact]
	public async Task SetColorRemovesDuplicateStatusBarViews()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetColor(Colors.Red);
			var window = IosStatusBarViews.WindowsWithStatusBar()[0];
			using var duplicate = new UIView(window.Bounds) { Tag = IosStatusBarViews.StatusBarTag };
			using var anotherDuplicate = new UIView(window.Bounds) { Tag = IosStatusBarViews.StatusBarTag };
			window.AddSubview(duplicate);
			window.AddSubview(anotherDuplicate);
			Assert.Equal(3, IosStatusBarViews.TaggedSubviews(window).Count);

			StatusBar.SetColor(Colors.Blue);

			var remaining = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
			IosStatusBarViews.AssertColor(Colors.Blue, remaining.BackgroundColor);
		});
	}

	[Fact]
	public async Task SetColorColorsEveryWindowOfTheApp()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			var secondWindow = IosStatusBarViews.AddWindow();

			try
			{
				StatusBar.SetColor(Colors.Red);

				var windows = IosStatusBarViews.WindowsWithStatusBar();
				Assert.Contains(windows, window => ReferenceEquals(window, secondWindow));
				Assert.True(windows.Count >= 2);
				foreach (var window in windows)
				{
					var statusBarView = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
					IosStatusBarViews.AssertColor(Colors.Red, statusBarView.BackgroundColor);
				}
			}
			finally
			{
				IosStatusBarViews.RemoveWindow(secondWindow);
			}
		});
	}

	// A window that UIApplication.Windows lists but that has no status bar frame (its scene has no StatusBarManager, or here: the window reports
	// no scene) gets no status bar view, and the windows after it still get theirs.
	[Fact]
	public async Task SetColorSkipsListedWindowsWithoutASceneAndColorsTheOthers()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () => WithAWindowWithoutSceneBeforeAnotherWindow(static (windowWithoutScene, laterWindow) =>
		{
			StatusBar.SetColor(Colors.Red);

			Assert.Empty(IosStatusBarViews.TaggedSubviews(windowWithoutScene));
			IosStatusBarViews.AssertColor(Colors.Red, Assert.Single(IosStatusBarViews.TaggedSubviews(laterWindow)).BackgroundColor);
		}));
	}

	[Fact]
	public async Task SetColorSkipsWindowsOfScenesWithoutAStatusBarManager()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var window = await InvokeOnMainThreadAsync(static () => IosStatusBarViews.Windows.FirstOrDefault(static candidate => candidate.WindowScene is not null && candidate.WindowScene.StatusBarManager is null));
		Skip.If(window is null, sceneWithoutStatusBarManagerMissing);

		await InvokeOnMainThreadAsync(() =>
		{
			StatusBar.SetColor(Colors.Red);

			Assert.Empty(IosStatusBarViews.TaggedSubviews(window));
			Assert.NotEmpty(IosStatusBarViews.AllTaggedSubviews());
		});
	}

	[Fact]
	public async Task SetColorDoesNotTakeOverANestedAppViewWithTheStatusBarTag()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () => WithANestedAppViewWithTheStatusBarTag(static (window, pageView, appView) =>
		{
			StatusBar.SetColor(Colors.Red);

			Assert.False(ReferenceEquals(window, appView.Superview), "SetColor took the app view with the status bar tag for its status bar view: it moved it out of the page into the window.");
			Assert.Same(pageView, appView.Superview);
			// By reference: xUnit compares two UIViews (IEnumerable of their subviews) as collections, so any view without subviews would match.
			Assert.DoesNotContain(IosStatusBarViews.TaggedSubviews(window), view => ReferenceEquals(view, appView));
			IosStatusBarViews.AssertColor(Colors.Red, Assert.Single(IosStatusBarViews.TaggedSubviews(window)).BackgroundColor);
		}));
	}

	[Fact]
	public async Task SetBarSizeAddsAStatusBarViewWithoutColorToEveryWindow()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetBarSize(false);

			var windows = IosStatusBarViews.WindowsWithStatusBar();
			Assert.NotEmpty(windows);
			foreach (var window in windows)
			{
				var statusBarView = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
				Assert.Null(statusBarView.BackgroundColor);
				Assert.Same(statusBarView, window.Subviews[^1]);
			}
		});
	}

	[Fact]
	public async Task SetBarSizeAddsAStatusBarViewToEveryWindowOfTheApp()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			var secondWindow = IosStatusBarViews.AddWindow();

			try
			{
				StatusBar.SetBarSize(false);

				var windows = IosStatusBarViews.WindowsWithStatusBar();
				Assert.Contains(windows, window => ReferenceEquals(window, secondWindow));
				Assert.True(windows.Count >= 2);
				foreach (var window in windows)
				{
					Assert.Single(IosStatusBarViews.TaggedSubviews(window));
				}
			}
			finally
			{
				IosStatusBarViews.RemoveWindow(secondWindow);
			}
		});
	}

	[Fact]
	public async Task SetBarSizeSkipsListedWindowsWithoutASceneAndSizesTheOthers()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () => WithAWindowWithoutSceneBeforeAnotherWindow(static (windowWithoutScene, laterWindow) =>
		{
			StatusBar.SetBarSize(false);

			Assert.Empty(IosStatusBarViews.TaggedSubviews(windowWithoutScene));
			Assert.Single(IosStatusBarViews.TaggedSubviews(laterWindow));
		}));
	}

	[Fact]
	public async Task SetBarSizeSkipsWindowsOfScenesWithoutAStatusBarManager()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var window = await InvokeOnMainThreadAsync(static () => IosStatusBarViews.Windows.FirstOrDefault(static candidate => candidate.WindowScene is not null && candidate.WindowScene.StatusBarManager is null));
		Skip.If(window is null, sceneWithoutStatusBarManagerMissing);

		await InvokeOnMainThreadAsync(() =>
		{
			StatusBar.SetBarSize(false);

			Assert.Empty(IosStatusBarViews.TaggedSubviews(window));
			Assert.NotEmpty(IosStatusBarViews.AllTaggedSubviews());
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SetBarSizeKeepsTheStatusBarViewAndItsColor(bool isUsingSafeArea)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			StatusBar.SetColor(Colors.Red);
			var window = IosStatusBarViews.WindowsWithStatusBar()[0];
			var statusBarView = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
			var subviewCount = window.Subviews.Length;

			StatusBar.SetBarSize(isUsingSafeArea);
			StatusBar.SetBarSize(isUsingSafeArea);

			Assert.Same(statusBarView, Assert.Single(IosStatusBarViews.TaggedSubviews(window)));
			IosStatusBarViews.AssertColor(Colors.Red, statusBarView.BackgroundColor);

			// SetBarSize found the existing view and brought it to the front again; it did not add a view of its own next to it.
			Assert.Same(statusBarView, window.Subviews[^1]);
			Assert.Equal(subviewCount, window.Subviews.Length);
		});
	}

	[Fact]
	public async Task SetBarSizeRemovesDuplicateStatusBarViews()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetColor(Colors.Red);
			var window = IosStatusBarViews.WindowsWithStatusBar()[0];
			var statusBarView = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
			using var duplicate = new UIView(window.Bounds) { Tag = IosStatusBarViews.StatusBarTag };
			using var anotherDuplicate = new UIView(window.Bounds) { Tag = IosStatusBarViews.StatusBarTag };
			window.AddSubview(duplicate);
			window.AddSubview(anotherDuplicate);
			Assert.Equal(3, IosStatusBarViews.TaggedSubviews(window).Count);

			StatusBar.SetBarSize(false);

			// It keeps the first status bar view (the one that has the color) and moves it to the front.
			Assert.Same(statusBarView, Assert.Single(IosStatusBarViews.TaggedSubviews(window)));
			Assert.Same(statusBarView, window.Subviews[^1]);
			IosStatusBarViews.AssertColor(Colors.Red, statusBarView.BackgroundColor);
		});
	}

	[Fact]
	public async Task SetBarSizeDoesNotTakeOverANestedAppViewWithTheStatusBarTag()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () => WithANestedAppViewWithTheStatusBarTag(static (window, pageView, appView) =>
		{
			var appViewFrame = appView.Frame;

			StatusBar.SetBarSize(false);

			Assert.False(ReferenceEquals(window, appView.Superview), "SetBarSize took the app view with the status bar tag for its status bar view: it moved it out of the page into the window.");
			Assert.Same(pageView, appView.Superview);
			Assert.Equal(appViewFrame.ToString(), appView.Frame.ToString());
			// By reference: xUnit compares two UIViews (IEnumerable of their subviews) as collections, so any view without subviews would match.
			Assert.DoesNotContain(IosStatusBarViews.TaggedSubviews(window), view => ReferenceEquals(view, appView));
			Assert.Single(IosStatusBarViews.TaggedSubviews(window));
		}));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SetBarSizeSizesTheStatusBarViewToTheStatusBar(bool isUsingSafeArea)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			StatusBar.SetColor(Colors.Red);

			StatusBar.SetBarSize(isUsingSafeArea);

			foreach (var window in IosStatusBarViews.WindowsWithStatusBar())
			{
				var sceneFrame = IosStatusBarViews.SceneStatusBarFrame(window) ?? throw new InvalidOperationException("The window has no status bar frame.");
				var expected = new CGRect(sceneFrame.X, sceneFrame.Y, sceneFrame.Width, isUsingSafeArea ? window.SafeAreaInsets.Top : sceneFrame.Height);
				var statusBarView = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
				Assert.Equal(expected.ToString(), statusBarView.Frame.ToString());
			}
		});
	}

#if NET11_0_OR_GREATER
	// .NET 11: SetStyle sets Window.StatusBarTheme of the app's windows, and UIKit takes the style from the view
	// controllers of .NET MAUI, which return the style of their window's theme. Each step changes the style that the step before set: LightContent
	// after Default and after DarkContent, DarkContent after LightContent, Default after LightContent.
	[Theory]
	[InlineData(StatusBarWindowPage.ContentPage)]
	[InlineData(StatusBarWindowPage.NavigationPage)]
	[InlineData(StatusBarWindowPage.Shell)]
	[InlineData(StatusBarWindowPage.TabbedPage)]
	[InlineData(StatusBarWindowPage.FlyoutPage)]
	[InlineData(StatusBarWindowPage.ModalPage)]
	public async Task SetStyleChangesTheStatusBarStyleOfTheWindow(StatusBarWindowPage kind)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(kind, CreateStyleTestPage(), async mauiWindow =>
		{
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.Default);

			foreach (var (style, expected) in new[]
			{
				(StatusBarStyle.LightContent, UIStatusBarStyle.LightContent),
				(StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent),
				(StatusBarStyle.LightContent, UIStatusBarStyle.LightContent),
				(StatusBarStyle.Default, UIStatusBarStyle.Default),
			})
			{
				StatusBar.SetStyle(style);

				await AssertStatusBarStyleAsync(mauiWindow, expected);
			}
		});
	}

	// The style belongs to the whole app, as UIApplication's style did: every window gets the theme (a window without a UIWindow too, the
	// theme is its property).
	[Fact]
	public async Task SetStyleSetsTheStatusBarThemeOfEveryWindowOfTheApp()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			var secondWindow = mauiWindow.Application.CreateWindowFor(CreateStyleTestPage());

			StatusBar.SetStyle(StatusBarStyle.LightContent);

			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);
			Assert.Equal(StatusBarTheme.Dark, secondWindow.StatusBarTheme);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			StatusBar.SetStyle(StatusBarStyle.DarkContent);

			Assert.Equal(StatusBarTheme.Light, mauiWindow.Window.StatusBarTheme);
			Assert.Equal(StatusBarTheme.Light, secondWindow.StatusBarTheme);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	// The style is set on the windows that the app has at the time of the call: a window that opens afterwards keeps its own theme.
	[Fact]
	public async Task SetStyleLeavesAWindowThatOpensAfterTheCallAtItsOwnTheme()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			StatusBar.SetStyle(StatusBarStyle.LightContent);

			var laterWindow = mauiWindow.Application.CreateWindowFor(CreateStyleTestPage());

			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);
			Assert.Equal(StatusBarTheme.Default, laterWindow.StatusBarTheme);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);
		});
	}

	// Only CommunityToolkit.Maui (UseMauiCommunityToolkit) can connect StatusBar, which is in CommunityToolkit.Maui.Core, to the windows of
	// Microsoft.Maui.Controls. Without it SetStyle cannot reach a window, and says so.
	[Fact]
	public async Task SetStyleWithoutUseMauiCommunityToolkitTracesAndLeavesTheStatusBarAlone()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var setStatusBarThemeOfEveryWindow = StatusBar.SetStatusBarThemeOfEveryWindow;

		try
		{
			await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
			{
				StatusBar.SetStatusBarThemeOfEveryWindow = null;

				var listener = CapturingTraceListener.Capture(static () => StatusBar.SetStyle(StatusBarStyle.LightContent));

				Assert.Contains(windowsUnknownMessage, listener.Messages);
				Assert.Equal(StatusBarTheme.Default, mauiWindow.Window.StatusBarTheme);
				await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.Default);
			});
		}
		finally
		{
			StatusBar.SetStatusBarThemeOfEveryWindow = setStatusBarThemeOfEveryWindow;
		}
	}

	// The runner's MauiProgram already called UseMauiCommunityToolkit: the test takes the connection away first, and building the test's MauiApp
	// (UseMauiCommunityToolkit) has to make it again.
	[Fact]
	public void UseMauiCommunityToolkitConnectsSetStyleToTheWindowsOfTheApp()
	{
		var setStatusBarThemeOfEveryWindow = StatusBar.SetStatusBarThemeOfEveryWindow;
		StatusBar.SetStatusBarThemeOfEveryWindow = null;

		try
		{
			SetupBuilder();

			Assert.Equal(new Action<StatusBarTheme>(CommunityToolkit.Maui.Behaviors.StatusBarBehavior.SetStatusBarThemeOfEveryWindow), StatusBar.SetStatusBarThemeOfEveryWindow);
		}
		finally
		{
			StatusBar.SetStatusBarThemeOfEveryWindow = setStatusBarThemeOfEveryWindow;
		}
	}

	[Theory]
	[InlineData(StatusBarStyle.Default, StatusBarTheme.Default)]
	[InlineData(StatusBarStyle.LightContent, StatusBarTheme.Dark)]
	[InlineData(StatusBarStyle.DarkContent, StatusBarTheme.Light)]
	public void SetStyleMapsEveryStyleToTheMatchingStatusBarTheme(StatusBarStyle style, StatusBarTheme expected)
	{
		Assert.Equal(expected, StatusBar.ToStatusBarTheme(style));
	}

	[Fact]
	public void UndefinedStyleHasNoStatusBarTheme()
	{
		var exception = Assert.Throws<NotSupportedException>(static () => StatusBar.ToStatusBarTheme((StatusBarStyle)42));

		Assert.Equal(UnsupportedStyleMessage, exception.Message);
	}

	// UIKit asks the view controllers for the style only when the app's Info.plist does not set UIViewControllerBasedStatusBarAppearance to false
	// (with false it shows the style of the whole app, which the runner ignores): SetStyle still sets the theme of the windows, and says that UIKit
	// ignores the style only for false.
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(null!, false)]
	public async Task SetStyleTracesThatUIKitIgnoresTheStyleOnlyWhenInfoPlistTurnsViewControllerBasedStatusBarAppearanceOff(bool? viewControllerBasedStatusBarAppearance, bool isTraced)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), mauiWindow =>
		{
			var listener = CaptureTraceWithViewControllerBasedStatusBarAppearance(viewControllerBasedStatusBarAppearance, static () => StatusBar.SetStyle(StatusBarStyle.LightContent));

			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);
			Assert.Equal(isTraced, listener.Messages.Contains(styleIgnoredByUIKitMessage));
			return Task.CompletedTask;
		});
	}

	// With UIViewControllerBasedStatusBarAppearance set to false (the .NET 10 setup of StatusBarBehavior) UIKit ignores the theme of the windows and
	// shows the style of the whole app, which an app built with an iOS SDK before 27.0 takes from UIApplication.SetStatusBarStyle.
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(null!, false)]
	public async Task SetStyleSetsTheStyleOfTheWholeAppOnlyWhenInfoPlistTurnsViewControllerBasedStatusBarAppearanceOff(bool? viewControllerBasedStatusBarAppearance, bool setsTheStyleOfTheWholeApp)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), mauiWindow =>
		{
			var (listener, stylesOfTheWholeApp) = CaptureStylesOfTheWholeApp(viewControllerBasedStatusBarAppearance, isAppliedByUIKit: true, static () => StatusBar.SetStyle(StatusBarStyle.LightContent));

			UIStatusBarStyle[] expectedStylesOfTheWholeApp = setsTheStyleOfTheWholeApp ? [UIStatusBarStyle.LightContent] : [];
			Assert.Equal(expectedStylesOfTheWholeApp, stylesOfTheWholeApp);
			Assert.Equal(StatusBarTheme.Dark, mauiWindow.Window.StatusBarTheme);
			Assert.DoesNotContain(styleIgnoredByUIKitMessage, listener.Messages);
			return Task.CompletedTask;
		});
	}

	// An app built with the iOS 27 SDK or later ignores UIApplication.SetStatusBarStyle too, and only then does the status bar keep its style.
	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public async Task SetStyleTracesThatUIKitIgnoresTheStyleOnlyWhenUIKitIgnoresTheStyleOfTheWholeAppToo(bool isAppliedByUIKit, bool isTraced)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), _ =>
		{
			var (listener, stylesOfTheWholeApp) = CaptureStylesOfTheWholeApp(false, isAppliedByUIKit, static () => StatusBar.SetStyle(StatusBarStyle.DarkContent));

			Assert.Equal([UIStatusBarStyle.DarkContent], stylesOfTheWholeApp);
			Assert.Equal(isTraced, listener.Messages.Contains(styleIgnoredByUIKitMessage));
			return Task.CompletedTask;
		});
	}

	// The style of the whole app needs no window: CommunityToolkit.Maui.Core sets it without UseMauiCommunityToolkit, as on .NET 10.
	[Fact]
	public async Task SetStyleWithoutUseMauiCommunityToolkitSetsTheStyleOfTheWholeAppWhenInfoPlistTurnsViewControllerBasedStatusBarAppearanceOff()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var setStatusBarThemeOfEveryWindow = StatusBar.SetStatusBarThemeOfEveryWindow;

		try
		{
			await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), mauiWindow =>
			{
				StatusBar.SetStatusBarThemeOfEveryWindow = null;

				var (listener, stylesOfTheWholeApp) = CaptureStylesOfTheWholeApp(false, isAppliedByUIKit: true, static () => StatusBar.SetStyle(StatusBarStyle.LightContent));

				Assert.Equal([UIStatusBarStyle.LightContent], stylesOfTheWholeApp);
				Assert.DoesNotContain(windowsUnknownMessage, listener.Messages);
				Assert.Equal(StatusBarTheme.Default, mauiWindow.Window.StatusBarTheme);
				return Task.CompletedTask;
			});
		}
		finally
		{
			StatusBar.SetStatusBarThemeOfEveryWindow = setStatusBarThemeOfEveryWindow;
		}
	}

	// The runner keeps view controller-based status bar appearance, so UIKit ignores UIApplication.SetStatusBarStyle there, as it does in every app
	// built with the iOS 27 SDK: the style that the toolkit reads back from the scene has to show that.
	[Fact]
	public async Task TrySetApplicationStatusBarStyleReportsThatUIKitIgnoresTheStyleOfTheWholeApp()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.Default);

			Assert.False(StatusBar.TrySetApplicationStatusBarStyle(UIStatusBarStyle.LightContent));
			Assert.Equal(UIStatusBarStyle.Default, mauiWindow.Scene.StatusBarManager?.StatusBarStyle);
		});
	}

	// What SetStyle reads is the key of the app's Info.plist, which the runner does not set: its style changes are not reported as ignored.
	[Fact]
	public async Task SetStyleReadsViewControllerBasedStatusBarAppearanceFromTheInfoPlistOfTheApp()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		Assert.Null(Foundation.NSBundle.MainBundle.ObjectForInfoDictionary("UIViewControllerBasedStatusBarAppearance"));

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			Assert.Null(StatusBar.ReadViewControllerBasedStatusBarAppearance());

			var listener = CapturingTraceListener.Capture(static () => StatusBar.SetStyle(StatusBarStyle.DarkContent));

			Assert.DoesNotContain(styleIgnoredByUIKitMessage, listener.Messages);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}
#else
	// The runner ignores the style that SetStyle passes to UIApplication.SetStatusBarStyle (UIKit reports no change): its Info.plist keeps view
	// controller-based status bar appearance, and CI builds it with the iOS 27 SDK. So the mapping from StatusBarStyle is asserted where SetStyle takes it from.
	[Theory]
	[InlineData(StatusBarStyle.Default, UIStatusBarStyle.Default)]
	[InlineData(StatusBarStyle.LightContent, UIStatusBarStyle.LightContent)]
	[InlineData(StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent)]
	public void SetStyleMapsEveryStyleToTheMatchingUIStatusBarStyle(StatusBarStyle style, UIStatusBarStyle expected)
	{
		Assert.Equal(expected, StatusBar.ToUIStatusBarStyle(style));
	}

	[Fact]
	public void UndefinedStyleHasNoUIStatusBarStyle()
	{
		var exception = Assert.Throws<NotSupportedException>(static () => StatusBar.ToUIStatusBarStyle((StatusBarStyle)42));

		Assert.Equal(UnsupportedStyleMessage, exception.Message);
	}

	[Theory]
	[InlineData(StatusBarStyle.Default)]
	[InlineData(StatusBarStyle.LightContent)]
	[InlineData(StatusBarStyle.DarkContent)]
	public async Task SetStyleAcceptsEveryStyleAndUpdatesTheStatusBarAppearance(StatusBarStyle style)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await AssertUpdatesTheStatusBarAppearanceAsync(() => StatusBar.SetStyle(style));
	}
#endif

	[Fact]
	public async Task SetColorUpdatesTheStatusBarAppearance()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await AssertUpdatesTheStatusBarAppearanceAsync(static () => StatusBar.SetColor(Colors.Red));
	}

	[Fact]
	public async Task SetBarSizeUpdatesTheStatusBarAppearance()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await AssertUpdatesTheStatusBarAppearanceAsync(static () => StatusBar.SetBarSize(false));
	}

	// The status bar code asks each window's own root view controller (not the app's current one) to update the status bar appearance. On .NET 11
	// SetStyle leaves that to the WindowHandler of each .NET MAUI window, whose StatusBarTheme it sets.
	[Fact]
	public async Task StatusBarCallsAskTheRootViewControllerOfEveryWindow()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			var rootViewController = new QueryCountingViewController();
			var secondWindow = IosStatusBarViews.AddWindow(rootViewController);

			try
			{
				foreach (var (name, call) in new (string Name, Action Call)[]
				{
					("SetColor", static () => StatusBar.SetColor(Colors.Red)),
					("SetBarSize", static () => StatusBar.SetBarSize(false)),
#if !NET11_0_OR_GREATER
					("SetStyle", static () => StatusBar.SetStyle(StatusBarStyle.LightContent)),
#endif
				})
				{
					var before = rootViewController.AppearanceUpdateRequests;

					call();

					Assert.True(rootViewController.AppearanceUpdateRequests > before, $"{name} did not ask the second window's root view controller to update the status bar appearance.");
				}
			}
			finally
			{
				IosStatusBarViews.RemoveWindow(secondWindow);
			}
		});
	}

	[Fact]
	public async Task StatusBarAppearanceUpdateReachesTheTopmostPresentedViewController()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var rootViewController = await InvokeOnMainThreadAsync(static () => TestWindow.PlatformWindow.RootViewController ?? throw new InvalidOperationException("The runner's window has no root view controller."));
		var first = new QueryCountingViewController();
		var second = new QueryCountingViewController();

		try
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				await rootViewController.PresentViewControllerAsync(first, false);
				await first.PresentViewControllerAsync(second, false);
			});
			await AssertEventuallyOnMainThread(() => ReferenceEquals(TopmostPresentedViewController(rootViewController), second), "The view controllers were not presented.");

			var (before, after) = await InvokeOnMainThreadAsync(() =>
			{
				var requestsBefore = second.AppearanceUpdateRequests;
				StatusBar.SetColor(Colors.Red);
				return (requestsBefore, second.AppearanceUpdateRequests);
			});

			// UIKit re-reads the status bar style of the topmost presented view controller whichever view controller was asked (and passes the request
			// on to the presenting view controllers), so the walk down the presented chain is only visible in the topmost view controller receiving
			// the request itself: without the walk only the root view controller would get it.
			Assert.True(after > before, "The topmost presented view controller did not receive SetNeedsStatusBarAppearanceUpdate.");
		}
		finally
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				await second.DismissViewControllerAsync(false);
				await first.DismissViewControllerAsync(false);
			});
		}
	}

	[Fact]
	public async Task SetColorWithoutAnyViewControllerTracesAndStillColorsTheStatusBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		var listener = await InvokeOnMainThreadAsync(static () =>
		{
			var window = TestWindow.PlatformWindow;
			var rootViewController = window.RootViewController;
			window.RootViewController = null;

			try
			{
				return CapturingTraceListener.Capture(static () => StatusBar.SetColor(Colors.Red));
			}
			finally
			{
				window.RootViewController = rootViewController;
			}
		});

		Assert.Contains(appearanceUpdateFailedMessage, listener.Messages);
		await InvokeOnMainThreadAsync(static () =>
		{
			var window = IosStatusBarViews.WindowsWithStatusBar()[0];
			IosStatusBarViews.AssertColor(Colors.Red, Assert.Single(IosStatusBarViews.TaggedSubviews(window)).BackgroundColor);
		});
	}

#if !NET11_0_OR_GREATER
	[Fact]
	public async Task StatusBarAppearanceUpdateReportsWhetherEveryWindowWasUpdated()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		// StatusBar.SetStyle ignores the result; the all-windows update reports it (the private TryUpdateStatusBarAppearance()).
		var tryUpdateAllWindows = typeof(StatusBar).GetMethod("TryUpdateStatusBarAppearance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, Type.EmptyTypes)
			?? throw new InvalidOperationException("StatusBar no longer has a private static TryUpdateStatusBarAppearance().");

		var results = await InvokeOnMainThreadAsync(() =>
		{
			var withViewControllers = (bool)tryUpdateAllWindows.Invoke(null, null)!;

			var window = TestWindow.PlatformWindow;
			var rootViewController = window.RootViewController;
			window.RootViewController = null;
			try
			{
				var withoutViewControllers = CapturingTraceListener.Capture(() => Assert.False((bool)tryUpdateAllWindows.Invoke(null, null)!));
				return (withViewControllers, withoutViewControllers.Messages);
			}
			finally
			{
				window.RootViewController = rootViewController;
			}
		});

		Assert.True(results.withViewControllers);
		Assert.Contains(appearanceUpdateFailedMessage, results.Messages);
	}
#endif

	[Fact]
	public async Task StatusBarAppearanceUpdateWithoutAWindowUsesTheCurrentViewController()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var tryUpdateWindow = typeof(StatusBar).GetMethod("TryUpdateStatusBarAppearance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, [typeof(UIWindow)])
			?? throw new InvalidOperationException("StatusBar no longer has a private static TryUpdateStatusBarAppearance(UIWindow?).");
		var updatedWithViewController = false;

		// With a view controller on screen, which has to be found without a window to take the root view controller from.
		await AssertUpdatesTheStatusBarAppearanceAsync(() => updatedWithViewController = (bool)tryUpdateWindow.Invoke(null, [null])!);

		Assert.True(updatedWithViewController);

		// Without any view controller the failure is traced and reported, not thrown.
		var result = await InvokeOnMainThreadAsync(() =>
		{
			var window = TestWindow.PlatformWindow;
			var rootViewController = window.RootViewController;
			window.RootViewController = null;

			try
			{
				var updated = true;
				var listener = CapturingTraceListener.Capture(() => updated = (bool)tryUpdateWindow.Invoke(null, [null])!);
				return (updated, listener.Messages);
			}
			finally
			{
				window.RootViewController = rootViewController;
			}
		});

		Assert.False(result.updated);
		Assert.Contains(appearanceUpdateFailedMessage, result.Messages);
	}

	[Fact]
	public async Task SetColorFallsBackToTheCurrentViewControllerForWindowsWithoutRootViewController()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		var listener = await InvokeOnMainThreadAsync(static () =>
		{
			var windowWithoutRootViewController = IosStatusBarViews.AddWindow();

			try
			{
				return CapturingTraceListener.Capture(static () => StatusBar.SetColor(Colors.Red));
			}
			finally
			{
				IosStatusBarViews.RemoveWindow(windowWithoutRootViewController);
			}
		});

		Assert.DoesNotContain(appearanceUpdateFailedMessage, listener.Messages);
	}

	// Adds a window that reports no scene while the action runs, followed by an ordinary window of the runner's scene, and removes both again.
	static void WithAWindowWithoutSceneBeforeAnotherWindow(Action<UIWindow, UIWindow> action)
	{
		var windowWithoutScene = new SceneHidingWindow(IosStatusBarViews.RunnerScene) { Frame = new CGRect(0, 0, 100, 100), Hidden = false };
		var laterWindow = IosStatusBarViews.AddWindow();

		try
		{
			windowWithoutScene.HidesScene = true;

			// UIApplication.Windows still lists the window, before the other one: the status bar code has to skip it and go on.
			var windows = IosStatusBarViews.Windows.ToList();
			Assert.Contains(windows, window => ReferenceEquals(window, windowWithoutScene));
			Assert.True(windows.IndexOf(windowWithoutScene) < windows.IndexOf(laterWindow), "UIApplication.Windows lists the window without scene after the other test window.");
			Assert.Null(IosStatusBarViews.SceneStatusBarFrame(windowWithoutScene));

			action(windowWithoutScene, laterWindow);
		}
		finally
		{
			windowWithoutScene.HidesScene = false;
			IosStatusBarViews.RemoveWindow(laterWindow);
			IosStatusBarViews.RemoveWindow(windowWithoutScene);
		}
	}

	// Puts a small view with the status bar tag into the page area of the runner's window (inside the root view controller's view), as an app
	// view could carry the same tag, runs the action and removes the view again.
	static void WithANestedAppViewWithTheStatusBarTag(Action<UIWindow, UIView, UIView> action)
	{
		var window = TestWindow.PlatformWindow;
		var pageView = window.RootViewController?.View ?? throw new InvalidOperationException("The runner's window has no root view.");
		using var appView = new UIView(new CGRect(0, 0, 10, 10)) { Tag = IosStatusBarViews.StatusBarTag };
		pageView.AddSubview(appView);

		try
		{
			action(window, pageView, appView);
		}
		finally
		{
			appView.RemoveFromSuperview();
		}
	}
}