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

// Views/MauiMediaElement.windows.cs: the construction of the platform view (resource dictionary, custom style, custom transport controls),
// Dispose (including the finalizer path and a real finalizer run), CurrentPage / TryGetForegroundWindow and the full-screen toggle. The view
// is created around a real MediaPlayerElement; the full-screen tests host it in a real WinUI window, bring that window to the foreground (the
// code under test toggles the window that hosts the element and falls back to the foreground window) and skip visibly when Windows refuses.
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

	// Hosts the view (at the size the full-screen scenarios use) until its transport controls have applied the toolkit's template:
	// OnTemplateLoaded has then subscribed the view to the template's FullWindowButton (the state of every view that was on screen with its
	// controls enabled).
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
			// ResourceDictionary.windows.xaml is embedded in the MediaElement assembly and loaded with XamlReader.
			Assert.Single(fixture.View.Resources.MergedDictionaries);
			Assert.True(fixture.View.Resources.TryGetValue("customTransportcontrols", out var style));
			var customStyle = Assert.IsType<WinStyle>(style, exactMatch: false);

			Assert.Same(customStyle, fixture.Element.TransportControls.Style);
		});
	}

	// Dispose(true) detaches the player from the element in every case. That the player and its MediaSource are really closed is asserted by the
	// next two tests, behind probes that check on plain objects that this machine makes "closed" observable at all.
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

		// The finalizer path: no managed resource may be released (finalizers must not touch other managed objects).
		await InvokeOnMainThreadAsync(() => fixture.View.CallDispose(false));

		await InvokeOnMainThreadAsync(() => Assert.NotNull(fixture.Element.MediaPlayer));
		Assert.Equal(1.0, fixture.Player.Volume, 3);
	}

	// The real finalizer scenario: Dispose(false) on a thread that is not the UI thread, for a view whose template was never applied (the
	// placeholder button) and for one that was on screen (the toolkit subscribed to the template's FullWindowButton).
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

	// A view that is never disposed (a handler that was never disconnected) is finalized: the finalizer must complete. Here the view was on
	// screen, so FullScreenButton is the template's button, to which the toolkit subscribed.
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

	// Dispose(false) is first tried on a pool thread with a view in the same state; if it threw there, the finalizer would throw as well and end
	// the whole run, so the test fails before it abandons a view. An exception in the real finalizer is not caught: the run ends, which is what
	// users would see.
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

	// Creates a view, with its own MediaPlayer like every view the handler creates, that nothing references afterwards, optionally after it
	// was on screen long enough for the template to apply. Only a weak reference leaves this method; the player is released with the view.
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

	// The fallback of the full-screen click for an element whose window cannot be resolved. Runs wherever there is no foreground window (a
	// locked or disconnected session, which a test cannot create); everywhere else it is a visible skip.
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

		// MauiMediaElement hides the navigation bar of the current page of the application's window that shows the element, and of the first
		// window's (Application.Current.Windows.FirstOrDefault()?.Page) when no window does. The element's window is a plain WinUI window, so a fake
		// application whose only window shows our page is current while the click runs.
		public void Click()
		{
			using var scope = new WinMediaApplicationScope();
			var application = new WinMediaFakeApplication(() => new Window(page));
			application.AddWindow();

			WinMediaReflection.Invoke(view, "OnFullScreenButtonClick", null, null);
		}

		// The same click in an application with two windows: the first shows another page, the second shows our page in the element's window (its
		// handler's platform window is the one that hosts the element).
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

	// The handler of a MAUI window shown in a given WinUI window. Of a window's handler, the full-screen click reads only the platform view.
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

	// The full-screen click against the real application (no fake): the toolkit then hides and restores the navigation bar of the runner's own
	// page. Its value is captured first and put back afterwards, whatever happens in between.
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
			// The navigation bar returns to what it was before, whatever that was.
			Assert.Equal(navigationBarVisible, Shell.GetNavBarIsVisible(context.Page));
			Assert.Empty(context.OpenPopups);
			Assert.Same(context.Element, Assert.Single(context.View.Children));
			Assert.Equal(320, context.Element.Width);
			Assert.Equal(180, context.Element.Height);
		});
	}

	// Two windows of the application, the element in the second: full screen hides, and the second click restores, the navigation bar of the page
	// in the element's window. The first window, whose page CurrentPage returns, keeps its navigation bar.
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

	// The window is full screen for a reason other than the toolkit's button (F11 in the app, another control, the user): the next click takes
	// the leave branch while the element is still the view's child and the toolkit's popup is closed.
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

	// The button entered full screen, then something else (F11, the user, another control) put the window back: the toolkit's popup is still open
	// with the element in it, and the next click takes the enter branch again.
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

	// Two windows of the same app: the element's window and, in front of it, the runner's window. A press of the element's own full-screen
	// button (from the keyboard or assistive technology, which does not bring the window to the front first) must toggle the window that hosts
	// the element.
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