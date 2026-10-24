using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Primitives;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Media.Playback;
using MediaPlayerElement = Microsoft.UI.Xaml.Controls.MediaPlayerElement;
using WinGrid = Microsoft.UI.Xaml.Controls.Grid;
using WinMediaSource = Windows.Media.Core.MediaSource;
using WinStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MauiMediaElementWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MauiMediaElementWindowsTests : WinMediaTestBase
{
	sealed record ViewFixture(WinMediaView View, MediaPlayerElement Element, MediaPlayer Player, Microsoft.UI.Xaml.Controls.MediaTransportControls StockControls);

	Task<ViewFixture> CreateViewAsync() =>
		InvokeOnMainThreadAsync(() =>
		{
			var player = new MediaPlayer();
			var element = new MediaPlayerElement();
			element.SetMediaPlayer(player);
			var stockControls = element.TransportControls;
			var view = new WinMediaView(element);

			// A view whose Dispose(false) ran (the finalizer path) is disposed already: Dispose() then does nothing, so the cleanup releases the
			// player itself.
			RegisterCleanup(() =>
			{
				view.Dispose();
				ReleasePlayer(player, element);
			});

			return new ViewFixture(view, element, player, stockControls);
		});

	// Leaves the view in the state of every view that was on screen with its controls enabled: OnTemplateLoaded has subscribed it to the
	// template's FullWindowButton.
	async Task ApplyTemplateAsync(MauiMediaElement view, MediaPlayerElement element)
	{
		await InvokeOnMainThreadAsync(() =>
		{
			view.Width = 320;
			view.Height = 180;
		});

		await HostAsync(view, async _ =>
		{
			element.AreTransportControlsEnabled = true;
			var controls = Assert.IsType<CustomTransportControls>(element.TransportControls);
			await AssertionExtensions.AssertEventually(() => controls.FullScreenButton.Name == "FullWindowButton", 5000, 50, "The FullWindowButton of the template was never found.");
		});
	}

	[Fact]
	public async Task ConstructorAddsThePlayerElementAsTheOnlyChild()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateViewAsync();

		await InvokeOnMainThreadAsync(() => Assert.Same(fixture.Element, Assert.Single(fixture.View.Children)));
	}

	[Fact]
	public async Task ConstructorDisablesTheStockControlsAndInstallsTheConfiguredCustomControls()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateViewAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.False(fixture.StockControls.IsEnabled);

			var controls = Assert.IsType<CustomTransportControls>(fixture.Element.TransportControls);
			Assert.True(controls.IsZoomButtonVisible);
			Assert.True(controls.IsZoomEnabled);
			Assert.True(controls.IsVolumeButtonVisible);
			Assert.True(controls.IsVolumeEnabled);
			Assert.True(controls.IsSeekBarVisible);
			Assert.True(controls.IsSeekEnabled);
			Assert.True(controls.IsEnabled);
			Assert.True(controls.IsRepeatButtonVisible);
			Assert.True(controls.IsRepeatEnabled);
			Assert.True(controls.IsNextTrackButtonVisible);
			Assert.True(controls.IsPreviousTrackButtonVisible);
			Assert.True(controls.IsFastForwardButtonVisible);
			Assert.True(controls.IsFastForwardEnabled);
			Assert.True(controls.IsFastRewindButtonVisible);
			Assert.True(controls.IsFastRewindEnabled);
			Assert.True(controls.IsPlaybackRateButtonVisible);
			Assert.True(controls.IsPlaybackRateEnabled);
			Assert.False(controls.IsCompact);
		});
	}

	[Fact]
	public async Task ConstructorMergesTheEmbeddedResourceDictionaryAndAppliesTheCustomStyle()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateViewAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Single(fixture.View.Resources.MergedDictionaries);
			Assert.True(fixture.View.Resources.TryGetValue("customTransportcontrols", out var style));
			var customStyle = Assert.IsType<WinStyle>(style, exactMatch: false);

			Assert.Same(customStyle, fixture.Element.TransportControls.Style);
		});
	}

	// Closing is not asserted here: the next two tests do, behind probes that check on plain objects that this machine makes "closed"
	// observable at all.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task DisposeDetachesThePlayerFromTheElement(bool playerHasAMediaSource)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateViewAsync();
		if (playerHasAMediaSource)
		{
			await InvokeOnMainThreadAsync(() => fixture.Player.Source = WinMediaSource.CreateFromUri(WinMediaAssets.VideoUri));
			Assert.IsType<WinMediaSource>(fixture.Player.Source);
		}
		else
		{
			Assert.Null(fixture.Player.Source);
		}

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.View.Dispose();

			Assert.Null(fixture.Element.MediaPlayer);
		});
	}

	[Fact]
	public async Task DisposeClosesTheNativePlayer()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var notObservable = await WinMediaProbe.DisposedPlayerNotObservableReason;
		Skip.If(notObservable is not null, $"Whether a player was closed cannot be observed on this machine ({notObservable}).");
		var fixture = await CreateViewAsync();
		Assert.False(WinMediaClosed.IsClosed(() => fixture.Player.Volume), "Precondition: the live player answers.");

		await InvokeOnMainThreadAsync(() => fixture.View.Dispose());

		Assert.True(WinMediaClosed.IsClosed(() => fixture.Player.Volume), "The MediaPlayer still answers after the view was disposed.");
	}

	[Fact]
	public async Task DisposeClosesTheMediaSourceOfThePlayer()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var notObservable = await WinMediaProbe.DisposedMediaSourceNotObservableReason;
		Skip.If(notObservable is not null, $"Whether a MediaSource was closed cannot be observed on this machine ({notObservable}).");
		var fixture = await CreateViewAsync();
		var mediaSource = WinMediaSource.CreateFromUri(WinMediaAssets.VideoUri);
		await InvokeOnMainThreadAsync(() => fixture.Player.Source = mediaSource);
		Assert.False(WinMediaClosed.IsClosed(mediaSource), "Precondition: the live MediaSource is not closed.");

		await InvokeOnMainThreadAsync(() => fixture.View.Dispose());

		Assert.True(WinMediaClosed.IsClosed(mediaSource), "The MediaSource of the player was not closed when the view was disposed.");
	}

	[Fact]
	public async Task DisposeTwiceDoesNotThrow()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateViewAsync();

		// The second call must return at the isDisposed check: the player is gone, so Pause() on it would throw.
		await InvokeOnMainThreadAsync(() =>
		{
			fixture.View.Dispose();
			fixture.View.Dispose();
		});
	}

	[Fact]
	public async Task DisposeWithDisposingFalseKeepsThePlayer()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateViewAsync();

		// Finalizers must not touch other managed objects, so the finalizer path (Dispose(false)) must leave the player alone.
		await InvokeOnMainThreadAsync(() => fixture.View.CallDispose(false));

		await InvokeOnMainThreadAsync(() => Assert.NotNull(fixture.Element.MediaPlayer));
		Assert.Equal(1.0, fixture.Player.Volume, 3);
	}

	// The real finalizer runs Dispose(false) on a thread that is not the UI thread.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeWithDisposingFalseFromAnotherThreadKeepsThePlayerAndDoesNotThrow(bool templateApplied)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateViewAsync();
		if (templateApplied)
		{
			await ApplyTemplateAsync(fixture.View, fixture.Element);
		}

		var failure = await Record.ExceptionAsync(() => Task.Run(() => fixture.View.CallDispose(false)));

		Assert.Null(failure);
		await InvokeOnMainThreadAsync(() => Assert.NotNull(fixture.Element.MediaPlayer));
		Assert.Equal(1.0, fixture.Player.Volume, 3);
	}

	// A view whose handler was never disconnected is never disposed, only finalized. Here it was on screen, so its FullScreenButton is the template's
	// button, to which the toolkit subscribed.
	[Fact]
	public async Task FinalizerOfAViewWhoseTemplateWasAppliedCompletes()
	{
		await AssertTheFinalizerOfAnAbandonedViewCompletesAsync(templateApplied: true);
	}

	// The same for a view that was never on screen: its FullScreenButton is the placeholder AppBarButton. Removing a Click handler from a button
	// without a Click event source first creates one through the button's finalizable CsWinRT object reference (Microsoft.WinUI 1.8
	// ButtonBase.Click and IButtonBaseMethods.Get_Click2 read ThisPtr), which the finalizer thread can have finalized before the view: the
	// finalizer path (Dispose(false)) must not touch the button at all.
	[Fact]
	public async Task FinalizerOfAViewWhoseTemplateWasNeverAppliedCompletes()
	{
		await AssertTheFinalizerOfAnAbandonedViewCompletesAsync(templateApplied: false);
	}

	// Dispose(false) is first tried on a pool thread with a view in the same state: an exception in the real finalizer is not caught and ends the
	// whole run (as it would for users), so the test must fail before it abandons a view.
	async Task AssertTheFinalizerOfAnAbandonedViewCompletesAsync(bool templateApplied)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var gate = await CreateViewAsync();
		if (templateApplied)
		{
			await ApplyTemplateAsync(gate.View, gate.Element);
		}

		var failure = await Record.ExceptionAsync(() => Task.Run(() => gate.View.CallDispose(false)));
		Assert.Null(failure);

		var completedBefore = WinMediaFinalizerProbeView.CompletedFinalizerPaths;
		var abandoned = await CreateAndAbandonAViewAsync(templateApplied);

		await AssertionExtensions.WaitForGC(abandoned);
		GC.WaitForPendingFinalizers();

		Assert.True(WinMediaFinalizerProbeView.CompletedFinalizerPaths > completedBefore, "The finalizer of the collected view did not complete Dispose(false).");
	}

	// Only a weak reference leaves this method, and it is not inlined, so nothing references the view when the collection starts. The player is
	// released with the view.
	[MethodImpl(MethodImplOptions.NoInlining)]
	async Task<WeakReference> CreateAndAbandonAViewAsync(bool templateApplied)
	{
		var (view, element) = await InvokeOnMainThreadAsync(() =>
		{
			var playerElement = new MediaPlayerElement { Width = 320, Height = 180 };
			playerElement.SetMediaPlayer(new MediaPlayer());
			return (new WinMediaFinalizerProbeView(playerElement) { Width = 320, Height = 180 }, playerElement);
		});

		if (templateApplied)
		{
			await ApplyTemplateAsync(view, element);
		}

		return new WeakReference(view);
	}

	[Fact]
	public async Task CurrentPageIsThePageOfTheFirstWindow()
	{
		SetupWindowsBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WinMediaApplicationScope();
			var page = new ContentPage();
			var application = new WinMediaFakeApplication(() => new Window(page));
			application.AddWindow();

			Assert.Same(page, WinMediaView.GetCurrentPage());
		});
	}

	[Fact]
	public async Task CurrentPageWithoutApplicationThrowsInvalidOperationException()
	{
		SetupWindowsBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WinMediaApplicationScope();
			Application.Current = null;

			var exception = Assert.Throws<InvalidOperationException>(() => WinMediaView.GetCurrentPage());

			Assert.Equal("Page cannot be null.", exception.Message);
		});
	}

	[Fact]
	public async Task CurrentPageOfAWindowWithoutPageThrowsInvalidOperationException()
	{
		SetupWindowsBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WinMediaApplicationScope();
			var application = new WinMediaFakeApplication(() => new Window());
			application.AddWindow();

			var exception = Assert.Throws<InvalidOperationException>(() => WinMediaView.GetCurrentPage());

			Assert.Equal("Page cannot be null.", exception.Message);
		});
	}

	[Fact]
	public async Task CurrentPageOfAnApplicationWithoutWindowThrowsInvalidOperationException()
	{
		SetupWindowsBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WinMediaApplicationScope();
			_ = new WinMediaFakeApplication();

			var exception = Assert.Throws<InvalidOperationException>(() => WinMediaView.GetCurrentPage());

			Assert.Equal("Page cannot be null.", exception.Message);
		});
	}

	[Fact]
	public void TryGetForegroundWindowReturnsNullOrTheWin32Handle()
	{
		// The foreground window can change between two calls: compare only when it did not.
		for (var attempt = 0; attempt < 20; attempt++)
		{
			var before = MauiMediaElement.GetForegroundWindow();
			var safe = MauiMediaElement.TryGetForegroundWindow();
			var after = MauiMediaElement.GetForegroundWindow();

			if (before == after)
			{
				Assert.Equal(before == IntPtr.Zero ? null : (IntPtr?)before, safe);
				return;
			}
		}

		throw new SkipException("The foreground window kept changing, so the two Win32 reads could not be compared.");
	}

	[Fact]
	public void NoForegroundWindowMakesTheFullScreenClickThrowInvalidOperationException()
	{
		Skip.If(MauiMediaElement.TryGetForegroundWindow() is not null, "This session has a foreground window. The null arm of TryGetForegroundWindow and the InvalidOperationException('No foreground window found.') of GetAppWindowForCurrentWindow need a session without one (locked or disconnected).");

		var exception = Assert.Throws<InvalidOperationException>(() => WinMediaReflection.InvokeStatic(typeof(MauiMediaElement), "GetAppWindowForCurrentWindow"));

		Assert.Equal("No foreground window found.", exception.Message);
	}

	sealed class FullScreenContext(WinMediaView view, MediaPlayerElement element, ContentPage page, AppWindow appWindow, Microsoft.UI.Xaml.Window platformWindow)
	{
		public WinMediaView View => view;

		public MediaPlayerElement Element => element;

		public ContentPage Page => page;

		public AppWindow AppWindow => appWindow;

		public Popup Popup => WinMediaReflection.GetField<Popup>(view, "popup");

		public IReadOnlyList<Popup> OpenPopups => Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(element.XamlRoot);

		// The element's window is a plain WinUI window, so no application window shows the element and MauiMediaElement falls back to the first window's
		// page (Application.Current.Windows.FirstOrDefault()?.Page): a fake application whose only window shows our page is current while the click runs.
		public void Click()
		{
			using var scope = new WinMediaApplicationScope();
			var application = new WinMediaFakeApplication(() => new Window(page));
			application.AddWindow();

			WinMediaReflection.Invoke(view, "OnFullScreenButtonClick", null, null);
		}

		public void ClickWithTheElementInTheSecondWindow(Page firstWindowPage)
		{
			using var scope = new WinMediaApplicationScope();
			var windows = new Queue<Window>([new Window(firstWindowPage), new Window(page) { Handler = new PlatformWindowHandler(platformWindow) }]);
			var application = new WinMediaFakeApplication(windows.Dequeue);
			application.AddWindow();
			application.AddWindow();

			WinMediaReflection.Invoke(view, "OnFullScreenButtonClick", null, null);
		}

		public Task WaitForPresenterAsync(Func<AppWindowPresenterKind, bool> predicate, string message) =>
			AssertionExtensions.AssertEventually(() => predicate(appWindow.Presenter.Kind), 5000, 50, message);
	}

	// The full-screen click finds the window that shows the element by the platform view of each window's handler, so the other members do nothing.
	sealed class PlatformWindowHandler(Microsoft.UI.Xaml.Window platformWindow) : IElementHandler
	{
		public object? PlatformView => platformWindow;

		public IElement? VirtualView { get; private set; }

		public IMauiContext? MauiContext => null;

		public void SetMauiContext(IMauiContext mauiContext)
		{
		}

		public void SetVirtualView(IElement view) => VirtualView = view;

		public void UpdateValue(string property)
		{
		}

		public void Invoke(string command, object? args = null)
		{
		}

		public void DisconnectHandler() => VirtualView = null;
	}

	async Task RunFullScreenScenarioAsync(Func<FullScreenContext, Task> scenario)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateViewAsync();
		await InvokeOnMainThreadAsync(() =>
		{
			fixture.View.Width = 320;
			fixture.View.Height = 180;
		});

		await HostAsync(fixture.View, async window =>
		{
			var handle = WinMediaWindowing.GetHandle(window);
			if (!await WinMediaWindowing.TryBringToForegroundAsync(handle))
			{
				throw new SkipException($"Windows did not bring the test window to the foreground, where the full-screen scenarios start (the window MauiMediaElement falls back to, the foreground window, is then the element's window as well): {WinMediaWindowing.DescribeForegroundWindow()}.");
			}

			var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle));
			var context = new FullScreenContext(fixture.View, fixture.Element, new ContentPage(), appWindow, window);
			try
			{
				await scenario(context);
			}
			finally
			{
				appWindow.SetPresenter(AppWindowPresenterKind.Default);
				context.Popup.IsOpen = false;
			}
		});
	}

	// The click runs against the real application, so the toolkit hides and restores the navigation bar of the runner's own page: its value is
	// captured first and put back afterwards, whatever happens in between.
	static async Task RunAgainstTheRunnerPageAsync(Func<Page, bool, Task> scenario)
	{
		Page runnerPage;
		try
		{
			runnerPage = WinMediaView.GetCurrentPage();
		}
		catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
		{
			throw new SkipException($"The runner app has no current page ({ex.Message}), so the toolkit's full-screen click cannot run against the real application.");
		}

		var runnerNavigationBarVisible = Shell.GetNavBarIsVisible(runnerPage);
		try
		{
			await scenario(runnerPage, runnerNavigationBarVisible);
		}
		finally
		{
			Shell.SetNavBarIsVisible(runnerPage, runnerNavigationBarVisible);
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FullScreenClickEntersFullScreenAndASecondClickRestoresTheViewAndTheNavigationBar(bool navigationBarVisible)
	{
		await RunFullScreenScenarioAsync(async context =>
		{
			var windowedKind = context.AppWindow.Presenter.Kind;
			Assert.NotEqual(AppWindowPresenterKind.FullScreen, windowedKind);
			Shell.SetNavBarIsVisible(context.Page, navigationBarVisible);

			context.Click();

			await context.WaitForPresenterAsync(kind => kind is AppWindowPresenterKind.FullScreen, "The window did not enter full screen.");
			Assert.False(Shell.GetNavBarIsVisible(context.Page));
			Assert.Empty(context.View.Children);
			var popup = Assert.Single(context.OpenPopups);
			Assert.Contains(context.Element, Assert.IsType<WinGrid>(popup.Child).Children);
			var display = DeviceDisplay.Current.MainDisplayInfo;
			Assert.Equal(display.Width / display.Density, context.Element.Width, 1);
			Assert.Equal(display.Height / display.Density, context.Element.Height, 1);

			context.Click();

			await context.WaitForPresenterAsync(kind => kind == windowedKind, "The window did not leave full screen.");
			Assert.Equal(navigationBarVisible, Shell.GetNavBarIsVisible(context.Page));
			Assert.Empty(context.OpenPopups);
			Assert.Same(context.Element, Assert.Single(context.View.Children));
			Assert.Equal(320, context.Element.Width);
			Assert.Equal(180, context.Element.Height);
		});
	}

	// The element is in the second window, so the first window, whose page CurrentPage returns, keeps its navigation bar.
	[Fact]
	public async Task FullScreenClickHidesTheNavigationBarOfThePageInTheElementsWindow()
	{
		await RunFullScreenScenarioAsync(async context =>
		{
			var windowedKind = context.AppWindow.Presenter.Kind;
			var firstWindowPage = new ContentPage();
			Shell.SetNavBarIsVisible(firstWindowPage, true);
			Shell.SetNavBarIsVisible(context.Page, true);

			context.ClickWithTheElementInTheSecondWindow(firstWindowPage);

			await context.WaitForPresenterAsync(kind => kind is AppWindowPresenterKind.FullScreen, "The window did not enter full screen.");
			Assert.False(Shell.GetNavBarIsVisible(context.Page));
			Assert.True(Shell.GetNavBarIsVisible(firstWindowPage));

			context.ClickWithTheElementInTheSecondWindow(firstWindowPage);

			await context.WaitForPresenterAsync(kind => kind == windowedKind, "The window did not leave full screen.");
			Assert.True(Shell.GetNavBarIsVisible(context.Page));
			Assert.True(Shell.GetNavBarIsVisible(firstWindowPage));
		});
	}

	[Fact]
	public async Task LeavingAFullScreenTheButtonDidNotStartKeepsTheElementAndTheNavigationBar()
	{
		await RunFullScreenScenarioAsync(async context =>
		{
			var windowedKind = context.AppWindow.Presenter.Kind;
			Shell.SetNavBarIsVisible(context.Page, true);
			context.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
			await context.WaitForPresenterAsync(kind => kind is AppWindowPresenterKind.FullScreen, "The window did not enter full screen.");

			var failure = Record.Exception(context.Click);

			Assert.Null(failure);
			await context.WaitForPresenterAsync(kind => kind == windowedKind, "The window did not leave full screen.");
			Assert.Same(context.Element, Assert.Single(context.View.Children));
			Assert.True(Shell.GetNavBarIsVisible(context.Page));
			Assert.Empty(context.OpenPopups);
		});
	}

	[Fact]
	public async Task ReenteringFullScreenAfterThePresenterWasResetKeepsThePopupAndRestoresTheNavigationBar()
	{
		await RunFullScreenScenarioAsync(async context =>
		{
			var windowedKind = context.AppWindow.Presenter.Kind;
			Shell.SetNavBarIsVisible(context.Page, true);
			context.Click();
			await context.WaitForPresenterAsync(kind => kind is AppWindowPresenterKind.FullScreen, "The window did not enter full screen.");
			context.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
			await context.WaitForPresenterAsync(kind => kind == windowedKind, "The window did not leave full screen.");

			var failure = Record.Exception(context.Click);

			Assert.Null(failure);
			await context.WaitForPresenterAsync(kind => kind is AppWindowPresenterKind.FullScreen, "The window did not enter full screen again.");
			Assert.Same(context.Popup, Assert.Single(context.OpenPopups));
			Assert.Contains(context.Element, Assert.IsType<WinGrid>(context.Popup.Child).Children);

			context.Click();

			await context.WaitForPresenterAsync(kind => kind == windowedKind, "The window did not leave full screen.");
			Assert.True(Shell.GetNavBarIsVisible(context.Page));
			Assert.Same(context.Element, Assert.Single(context.View.Children));
			Assert.Empty(context.OpenPopups);
		});
	}

	[Fact]
	public async Task ClickingTheFullWindowButtonOfTheTemplateTogglesFullScreen()
	{
		await RunFullScreenScenarioAsync(context => RunAgainstTheRunnerPageAsync(async (runnerPage, runnerNavigationBarVisible) =>
		{
			var windowedKind = context.AppWindow.Presenter.Kind;
			var controls = Assert.IsType<CustomTransportControls>(context.Element.TransportControls);

			// Once the template is applied, OnTemplateLoaded has subscribed MauiMediaElement to the template's own FullWindowButton.
			context.Element.AreTransportControlsEnabled = true;
			await AssertionExtensions.AssertEventually(() => controls.FullScreenButton.Name == "FullWindowButton", 5000, 50, "The FullWindowButton of the template was never found.");

			WinMediaAutomation.Invoke(controls.FullScreenButton);
			await context.WaitForPresenterAsync(kind => kind is AppWindowPresenterKind.FullScreen, "Clicking the FullWindowButton did not enter full screen.");
			Assert.False(Shell.GetNavBarIsVisible(runnerPage));

			WinMediaAutomation.Invoke(controls.FullScreenButton);
			await context.WaitForPresenterAsync(kind => kind == windowedKind, "Clicking the FullWindowButton again did not leave full screen.");
			Assert.Equal(runnerNavigationBarVisible, Shell.GetNavBarIsVisible(runnerPage));
		}));
	}

	// A press of the element's own full-screen button from the keyboard or assistive technology does not bring its window to the front first, so
	// another window of the app (here the runner's) can be in front of it. The press must still toggle the window that hosts the element.
	[Fact]
	public async Task FullScreenButtonTogglesTheWindowThatHostsTheElement()
	{
		await RunFullScreenScenarioAsync(context => RunAgainstTheRunnerPageAsync(async (_, _) =>
		{
			var controls = Assert.IsType<CustomTransportControls>(context.Element.TransportControls);
			context.Element.AreTransportControlsEnabled = true;
			await AssertionExtensions.AssertEventually(() => controls.FullScreenButton.Name == "FullWindowButton", 5000, 50, "The FullWindowButton of the template was never found.");

			var runnerHandle = WinMediaWindowing.GetHandle(TestWindow.PlatformWindow);
			if (!await WinMediaWindowing.TryBringToForegroundAsync(runnerHandle))
			{
				throw new SkipException($"Windows did not bring the runner's window to the foreground, so the element's window cannot be put behind another window of the app: {WinMediaWindowing.DescribeForegroundWindow()}.");
			}

			var runnerAppWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(runnerHandle));
			var runnerKind = runnerAppWindow.Presenter.Kind;
			Assert.NotEqual(AppWindowPresenterKind.FullScreen, runnerKind);
			try
			{
				WinMediaAutomation.Invoke(controls.FullScreenButton);

				await AssertionExtensions.AssertEventually(
					() => context.AppWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen || runnerAppWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen,
					5000, 50, "The click made no window full screen.");

				Assert.Equal(AppWindowPresenterKind.FullScreen, context.AppWindow.Presenter.Kind);
				Assert.Equal(runnerKind, runnerAppWindow.Presenter.Kind);
			}
			finally
			{
				runnerAppWindow.SetPresenter(runnerKind);
			}
		}));
	}
}