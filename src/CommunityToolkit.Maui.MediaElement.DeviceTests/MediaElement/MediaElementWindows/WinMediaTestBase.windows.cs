using System.Collections.Concurrent;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Windows.Media;
using Windows.Media.Control;
using Windows.Media.Playback;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using MediaPlayerElement = Microsoft.UI.Xaml.Controls.MediaPlayerElement;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// Test doubles and the shared base class of the MediaElement-on-Windows tests.

/// <summary>
/// The real MediaElementHandler plus read access to the handler-owned MediaManager and to the native elements.
/// </summary>
public sealed class WinMediaHandler : MediaElementHandler
{
	public MediaManager? Manager => MediaManager;

	public MauiMediaElement MauiView => PlatformView;

	public MediaPlayerElement PlayerElement => GetPlayerElement(PlatformView);

	public MediaPlayer Player => PlayerElement.MediaPlayer;

	public MauiMediaElement CallCreatePlatformView() => CreatePlatformView();

	public void CallDispose(bool disposing) => Dispose(disposing);

	public static MediaPlayerElement GetPlayerElement(MauiMediaElement view) => view.Children.OfType<MediaPlayerElement>().Single();
}

/// <summary>
/// The real MediaManager, constructed without a handler, plus the protected members the tests need.
/// </summary>
public sealed class WinMediaManager(IMauiContext context, IMediaElement mediaElement, Microsoft.Maui.Dispatching.IDispatcher dispatcher)
	: MediaManager(context, mediaElement, dispatcher)
{
	public MediaPlayerElement? PlayerElement
	{
		get => Player;
		set => Player = value;
	}

	/// <summary>The manager's balance flag ("one request is outstanding"). The native state is <see cref="NativeDisplayRequest"/>.</summary>
	public bool IsDisplayRequestActive => WinMediaReflection.GetField<bool>(this, "displayActiveRequested");

	/// <summary>The native DisplayRequest the manager activates and releases; count its requests with <see cref="WinMediaDisplayRequests.CountActive"/>.</summary>
	public Windows.System.Display.DisplayRequest NativeDisplayRequest => DisplayRequest;

	public void CallDispose(bool disposing) => Dispose(disposing);
}

/// <summary>
/// The real MauiMediaElement plus access to its protected members (finalizer path, CurrentPage).
/// </summary>
public sealed class WinMediaView(MediaPlayerElement mediaPlayerElement) : MauiMediaElement(mediaPlayerElement)
{
	public void CallDispose(bool disposing) => Dispose(disposing);

	public static Page GetCurrentPage() => CurrentPage;
}

/// <summary>
/// The real MauiMediaElement, counting how often its finalizer path (Dispose(false)) ran to completion. Used to let the garbage collector run the
/// real finalizer of a view that was never disposed; an exception in it is not caught (it ends the process, which is what the product would do).
/// </summary>
public sealed class WinMediaFinalizerProbeView(MediaPlayerElement mediaPlayerElement) : MauiMediaElement(mediaPlayerElement)
{
	static int completedFinalizerPaths;

	public static int CompletedFinalizerPaths => Volatile.Read(ref completedFinalizerPaths);

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (!disposing)
		{
			Interlocked.Increment(ref completedFinalizerPaths);
		}
	}
}

/// <summary>
/// A MediaManager created without a handler: <see cref="Manager"/> drives the native <see cref="Element"/> directly.
/// </summary>
public sealed record WinMediaManagerFixture(WinMediaManager Manager, MediaElement View, MediaPlayerElement? Element, MediaPlayer? Player, WinMediaDispatcher? Dispatcher = null);

/// <summary>
/// Base class of the Windows MediaElement tests: the test MauiApp resolves <see cref="WinMediaHandler"/> for MediaElement, native objects are
/// released on the UI thread when the test ends, and tests skip visibly when Media Foundation is not available on the machine.
/// </summary>
public abstract class WinMediaTestBase : MediaElementHandlerTestBase
{
	/// <summary>Upper bound for events that need real decoding (open, play, seek, end).</summary>
	protected const int decodeTimeoutMilliseconds = 20000;

	/// <summary>
	/// Upper bound for the failed open of a source whose connection is refused (http://127.0.0.1:1): Media Foundation tries the connection again
	/// for about 16 s before it raises MediaFailed (16.2 to 22.9 s measured on a Windows 11 ARM64 VM and on the Windows Server 2025 runners), so
	/// <see cref="decodeTimeoutMilliseconds"/> is too short for it.
	/// </summary>
	protected const int refusedConnectionTimeoutMilliseconds = 60000;

	readonly Lock cleanupGate = new();
	readonly List<Action> cleanups = [];

	/// <summary>
	/// Builds this test's MauiApp with <see cref="WinMediaHandler"/> as the MediaElement handler. Call it first in every test.
	/// Pass <paramref name="loggerFactory"/> to read back what the toolkit logs.
	/// </summary>
	protected void SetupWindowsBuilder(WinMediaLoggerFactory? loggerFactory = null) =>
		SetupBuilder(builder =>
		{
			builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<MediaElement, WinMediaHandler>());
			if (loggerFactory is not null)
			{
				builder.Services.AddSingleton<ILoggerFactory>(loggerFactory);
			}
		});

	/// <summary>
	/// Skips the test (visibly) when a Windows MediaPlayer cannot be created on this machine. A run that requires
	/// <see cref="DeviceTestCapabilities.MediaPlayback"/> (the Windows CI jobs, which install Media Foundation) fails instead, so that a runner
	/// without media support cannot turn most of the area into skips.
	/// </summary>
	protected static async Task RequireMediaPlayerAsync() => DeviceTestCapabilities.Require(DeviceTestCapabilities.MediaPlayback, await WinMediaProbe.PlayerUnavailableReason);

	/// <summary>
	/// Skips the test (visibly) when Media Foundation cannot open the H.264 test asset on this machine; fails instead in a run that requires
	/// <see cref="DeviceTestCapabilities.MediaPlayback"/>.
	/// </summary>
	protected static async Task RequireDecoderAsync() => DeviceTestCapabilities.Require(DeviceTestCapabilities.MediaPlayback, await WinMediaProbe.DecoderUnavailableReason);

	/// <summary>
	/// Skips the test (visibly) when this machine opens the H.264 test asset but renders no video: no NaturalVideoSizeChanged and no playback.
	/// For the tests that play media or wait for its natural size. A run that declares <see cref="DeviceTestCapabilities.VideoRendering"/>
	/// missing (the Windows CI jobs: a runner without a GPU may render nothing) skips them without probing; one that requires it fails instead.
	/// </summary>
	protected static async Task RequireVideoRenderingAsync()
	{
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.VideoRendering, await WinMediaProbe.VideoRenderingUnavailableReason);
	}

	/// <summary>
	/// Skips the test (visibly) for an environment reason that a probe on plain Windows objects reported (no-op for null). A capability probe
	/// that needs a MediaPlayer (or the decoder) answers with the player (or decoder) reason when there is none, so each Require*Async below
	/// that wraps such a probe first asks <see cref="RequireMediaPlayerAsync"/> or <see cref="RequireDecoderAsync"/>: that reason fails a run
	/// that requires <see cref="DeviceTestCapabilities.MediaPlayback"/> instead of becoming a skip, whatever order a test calls the requirements in.
	/// </summary>
	static void SkipFor(string? environmentReason, string consequence)
	{
		if (environmentReason is not null)
		{
			throw new SkipException($"{consequence} ({environmentReason})");
		}
	}

	/// <summary>Skips the test (visibly) when the system media controls of a plain MediaPlayer do not return the metadata and artwork assigned to them.</summary>
	protected static async Task RequireDisplayUpdaterAsync()
	{
		await RequireMediaPlayerAsync();
		SkipFor(await WinMediaProbe.DisplayUpdaterRoundTripFailure, "This machine's system media controls cannot be read back, so the metadata the toolkit publishes cannot be observed");
	}

	/// <summary>Skips the test (visibly) when the active requests of a plain DisplayRequest cannot be counted on this machine.</summary>
	protected static async Task RequireDisplayRequestCountingAsync() =>
		SkipFor(await WinMediaProbe.DisplayRequestCountingUnavailableReason, "The native DisplayRequest cannot be inspected on this machine");

	/// <summary>Skips the test (visibly) when this machine does not show the system media session of a plain MediaPlayer.</summary>
	protected static async Task RequireMediaSessionAsync()
	{
		await RequireMediaPlayerAsync();
		SkipFor(await WinMediaProbe.MediaSessionUnavailableReason, "The system media sessions cannot be observed on this machine");
	}

	/// <summary>Skips the test (visibly) when a plain MediaPlayer does not open the local HLS stream.</summary>
	protected static async Task RequireHlsAsync()
	{
		// Its probe plays video: a run that declares VideoRendering missing skips without playing any (on a Windows Server runner
		// without a GPU, the probe's playback ended the app with an access violation in CI run 38115049024).
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.HlsUnavailableReason, "This machine's MediaPlayer does not play the local HLS stream");
	}

	/// <summary>Skips the test (visibly) when a plain MediaPlayer does not report TimeSpan.MaxValue as the duration of a live playlist.</summary>
	protected static async Task RequireLiveDurationAsync()
	{
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.LiveDurationUnavailableReason, "This machine's MediaPlayer does not report an unknown duration for a live playlist");
	}

	/// <summary>Skips the test (visibly) when a plain MediaPlayer does not report Buffering while a segment is withheld.</summary>
	protected static async Task RequireStarvationBufferingAsync()
	{
		// Its probe plays video: a run that declares VideoRendering missing skips without playing any (on a Windows Server runner
		// without a GPU, the probe's playback ended the app with an access violation in CI run 38115049024).
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.StarvationBufferingUnavailableReason, "This machine's MediaPlayer does not report Buffering when an HLS stream runs out of data");
	}

	/// <summary>Skips the test (visibly) when a plain AdaptiveMediaSource does not fetch #EXT-X-BYTERANGE segments with their Range.</summary>
	protected static async Task RequireByteRangeAsync()
	{
		await RequireMediaPlayerAsync();
		SkipFor(await WinMediaProbe.ByteRangeUnavailableReason, "This machine's AdaptiveMediaSource does not fetch byte-range segments with their range");
	}

	/// <summary>Skips the test (visibly) when a plain MediaPlayer does not keep a playback rate of 0.</summary>
	protected static async Task RequireZeroRateAsync()
	{
		// Its probe plays video: a run that declares VideoRendering missing skips without playing any (on a Windows Server runner
		// without a GPU, the probe's playback ended the app with an access violation in CI run 38115049024).
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.ZeroRateUnavailableReason, "This machine's MediaPlayer does not keep a playback rate of 0");
	}

	/// <summary>Skips the test (visibly) when a plain MediaPlayer whose rate is 0 does not report Playing after Play().</summary>
	protected static async Task RequirePlayAtZeroRateAsync()
	{
		// Its probe plays video: a run that declares VideoRendering missing skips without playing any (on a Windows Server runner
		// without a GPU, the probe's playback ended the app with an access violation in CI run 38115049024).
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.PlayAtZeroRateUnavailableReason, "This machine's MediaPlayer does not report Playing while its playback rate is 0");
	}

	/// <summary>Skips the test (visibly) when a forgotten event unsubscription cannot be seen through the garbage collector on this machine.</summary>
	protected static async Task RequireEventSubscriptionControlAsync()
	{
		await RequireMediaPlayerAsync();
		SkipFor(await WinMediaProbe.EventSubscriptionControlFailure, "An event unsubscription cannot be proven through the garbage collector on this machine");
	}

	/// <summary>Asserts the manager's display-request flag and the native DisplayRequest's count of active requests. Call on the UI thread.</summary>
	protected static void AssertDisplayRequest(WinMediaManager manager, bool active)
	{
		Assert.Equal(active, manager.IsDisplayRequestActive);
		Assert.Equal(active ? 1 : 0, WinMediaDisplayRequests.CountActive(manager.NativeDisplayRequest));
	}

	/// <summary>
	/// Publishes the metadata of <paramref name="fixture"/> (whose title and artist must both be <paramref name="token"/>) through the manager,
	/// switches the player's system media controls to manual mode (the player's command manager would otherwise own them) and returns the
	/// system media session of this app. Skips (visibly) when the machine shows no session even for a plain MediaPlayer; fails when it shows
	/// that one but not the session the toolkit published.
	/// </summary>
	protected static async Task<GlobalSystemMediaTransportControlsSession> PublishSystemMediaSessionAsync(WinMediaManagerFixture fixture, string token)
	{
		await RequireMediaSessionAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var player = fixture.Player!;
			player.CommandManager.IsEnabled = false;
			var controls = player.SystemMediaTransportControls;
			controls.IsEnabled = true;
			controls.IsPlayEnabled = true;
			controls.IsPauseEnabled = true;
			controls.IsNextEnabled = true;
			controls.PlaybackStatus = MediaPlaybackStatus.Playing;

			// Creates the Metadata helper (it subscribes to ButtonPressed) and publishes the title.
			await (ValueTask)WinMediaReflection.Invoke(fixture.Manager, "UpdateMetadata")!;
		});

		var session = await WinMediaMediaSessions.FindAsync(token);
		Assert.True(session is not null, "This machine shows the system media session of a plain MediaPlayer, but not the one whose metadata the toolkit published.");
		return session;
	}

	/// <summary>Runs <paramref name="cleanup"/> on the UI thread when the test ends (last registered first).</summary>
	protected void RegisterCleanup(Action cleanup)
	{
		lock (cleanupGate)
		{
			cleanups.Add(cleanup);
		}
	}

	/// <summary>Completes after everything queued on the UI thread so far has run.</summary>
	protected static Task FlushDispatcherAsync() => InvokeOnMainThreadAsync(static () => { });

	/// <summary>The MediaFailed events of <paramref name="view"/>, in the order they were raised.</summary>
	protected static ConcurrentQueue<MediaFailedEventArgs> RecordFailures(MediaElement view)
	{
		var failures = new ConcurrentQueue<MediaFailedEventArgs>();
		view.MediaFailed += (_, e) => failures.Enqueue(e);
		return failures;
	}

	/// <summary>Polls <paramref name="condition"/> on the UI thread (for XAML objects) until it holds.</summary>
	protected static Task WaitUntilOnUiAsync(Func<bool> condition, string message, int timeoutMilliseconds = decodeTimeoutMilliseconds) =>
		AssertionExtensions.AssertEventuallyAsync(() => InvokeOnMainThreadAsync(condition), timeoutMilliseconds, 50, message);

	/// <summary>Polls <paramref name="condition"/> on the calling thread (for agile objects such as MediaPlayer and test servers).</summary>
	protected static Task WaitUntilAsync(Func<bool> condition, string message, int timeoutMilliseconds = decodeTimeoutMilliseconds) =>
		AssertionExtensions.AssertEventually(condition, timeoutMilliseconds, 50, message);

	/// <summary>
	/// Creates a MediaManager without handler. The manager, the MediaElement and the native player are released when the test ends.
	/// With <paramref name="dispatchRequired"/> (true) the manager gets a <see cref="WinMediaDispatcher"/> that reports IsDispatchRequired on every
	/// thread, so calls made on the UI thread take the dispatching arm. With <paramref name="recordDispatches"/> it gets one that forwards
	/// the real IsDispatchRequired and counts what was queued, so a test can see that the inline arm queued nothing. Without either it gets the
	/// real UI dispatcher. Only a fixture that opens no media may pass false: native media events arrive on other threads and must be
	/// dispatched.
	/// </summary>
	protected Task<WinMediaManagerFixture> CreateManagerAsync(Action<MediaElement>? configure = null, bool createPlatformView = true, bool? dispatchRequired = null, bool recordDispatches = false) =>
		InvokeOnMainThreadAsync(() =>
		{
			var view = new MediaElement();
			configure?.Invoke(view);

			// Resolve the view's dispatcher here, on the UI thread: MediaElement creates its status timer when the media opens, from a native thread.
			_ = view.Dispatcher;

			var countingDispatcher = dispatchRequired is not null || recordDispatches ? new WinMediaDispatcher(TestDispatcher.Current, dispatchRequired) : null;
			var manager = new WinMediaManager(MauiContext, view, countingDispatcher ?? TestDispatcher.Current);
			var element = createPlatformView ? manager.CreatePlatformView() : null;
			var player = element?.MediaPlayer;

			RegisterCleanup(() =>
			{
				manager.Dispose();
				view.Dispose();
				ReleasePlayer(player, element);
			});

			return new WinMediaManagerFixture(manager, view, element, player, countingDispatcher);
		});

	/// <summary>
	/// Creates the MediaElementHandler (a <see cref="WinMediaHandler"/>) for <paramref name="view"/> on the UI thread, without a window.
	/// The handler is disconnected and the view disposed when the test ends.
	/// </summary>
	protected async Task<WinMediaHandler> ConnectAsync(MediaElement view)
	{
		RegisterCleanup(() =>
		{
			((IElementHandler?)view.Handler)?.DisconnectHandler();
			view.Dispose();
		});

		return await CreateHandlerAsync<WinMediaHandler>(view);
	}

	/// <summary>
	/// Hosts a platform view in a new WinUI window (what AttachAndRun does for a MAUI view) and runs <paramref name="body"/> on the UI thread
	/// while the view is loaded. The window is closed afterwards. Always pass an explicit Task&lt;bool&gt; to AttachAndRun: with an async lambda
	/// the generic Func&lt;Window, T&gt; overload would run the body detached from the window's lifetime.
	/// </summary>
	protected Task HostAsync(FrameworkElement platformView, Func<WinUIWindow, Task> body) =>
		InvokeOnMainThreadAsync(() => platformView.AttachAndRun<bool>(async window =>
		{
			await body(window);
			return true;
		}, MauiContext));

	/// <summary>The native player of <paramref name="handler"/>, read on the UI thread (the XAML tree is thread-affine, the MediaPlayer itself is not).</summary>
	protected static Task<MediaPlayer> GetPlayerAsync(WinMediaHandler handler) => InvokeOnMainThreadAsync(() => handler.Player);

	/// <summary>A MediaElement with a size, as every handler test needs one (the Windows measure pass cannot work without).</summary>
	protected static MediaElement NewMediaElement(Action<MediaElement>? configure = null)
	{
		var view = new MediaElement { WidthRequest = 160, HeightRequest = 120 };
		configure?.Invoke(view);
		return view;
	}

	/// <summary>
	/// Stops, unloads and disposes a native player (the manager does not dispose the player; MauiMediaElement does). <paramref name="element"/>,
	/// the MediaPlayerElement that shows the player, lets go of it first: an element that was on screen keeps calling the player it holds from
	/// deferred callbacks on the UI thread, and such a call into a disposed player fails with E_ABORT, which ends the app (a stowed exception,
	/// 0xC000027B in CoreMessagingXP.dll). MauiMediaElement.Dispose detaches the player in the same UI-thread call that disposes it.
	/// </summary>
	protected static void ReleasePlayer(MediaPlayer? player, MediaPlayerElement? element = null)
	{
		element?.SetMediaPlayer(null);

		if (player is null)
		{
			return;
		}

		try
		{
			player.Pause();
			(player.Source as IDisposable)?.Dispose();
			player.Source = null;
			player.Dispose();
		}
		catch (Exception)
		{
			// The test already disposed the player.
		}
	}

	/// <summary>
	/// Sets <paramref name="source"/> on <paramref name="view"/> on the UI thread (a Source change starts and stops the MediaElement's status
	/// timer, which belongs there). A view without handler has no mapper, so pass its <paramref name="manager"/> to apply the source; the
	/// exceptions of the manager's UpdateSource then reach the caller.
	/// </summary>
	protected static Task SetSourceAsync(MediaElement view, CommunityToolkit.Maui.Views.MediaSource? source, MediaManager? manager = null) =>
		InvokeOnMainThreadAsync(async () =>
		{
			view.Source = source;
			if (manager is not null)
			{
				await manager.UpdateSource();
			}
		});

	/// <summary>
	/// Sets <paramref name="source"/> on <paramref name="view"/> and returns once MediaOpened was raised. A view without handler has no
	/// mapper, so pass its <paramref name="manager"/> to apply the source.
	/// </summary>
	protected static async Task OpenAsync(MediaElement view, CommunityToolkit.Maui.Views.MediaSource source, MediaManager? manager = null)
	{
		var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		void OnOpened(object? sender, EventArgs e) => opened.TrySetResult();

		view.MediaOpened += OnOpened;
		try
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				view.Source = source;
				if (manager is not null)
				{
					await manager.UpdateSource();
				}
			});
			await opened.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		}
		finally
		{
			view.MediaOpened -= OnOpened;
		}
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			Action[] pending;
			lock (cleanupGate)
			{
				pending = [.. cleanups];
				cleanups.Clear();
			}

			if (pending.Length > 0)
			{
				await InvokeOnMainThreadAsync(() =>
				{
					List<Exception>? failures = null;
					foreach (var cleanup in Enumerable.Reverse(pending))
					{
						try
						{
							cleanup();
						}
						catch (Exception ex)
						{
							(failures ??= []).Add(ex);
						}
					}

					if (failures is not null)
					{
						throw new AggregateException("Test cleanup failed.", failures);
					}
				});
			}
		}
		finally
		{
			await base.DisposeAsyncCore();
		}
	}
}