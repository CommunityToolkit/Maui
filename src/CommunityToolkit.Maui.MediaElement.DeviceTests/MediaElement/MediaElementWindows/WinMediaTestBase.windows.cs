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

public sealed class WinMediaManager(IMauiContext context, IMediaElement mediaElement, Microsoft.Maui.Dispatching.IDispatcher dispatcher)
	: MediaManager(context, mediaElement, dispatcher)
{
	public MediaPlayerElement? PlayerElement
	{
		get => Player;
		set => Player = value;
	}

	public bool IsDisplayRequestActive => WinMediaReflection.GetField<bool>(this, "displayActiveRequested");

	public Windows.System.Display.DisplayRequest NativeDisplayRequest => DisplayRequest;

	public void CallDispose(bool disposing) => Dispose(disposing);
}

public sealed class WinMediaView(MediaPlayerElement mediaPlayerElement) : MauiMediaElement(mediaPlayerElement)
{
	public void CallDispose(bool disposing) => Dispose(disposing);

	public static Page GetCurrentPage() => CurrentPage;
}

public sealed class WinMediaFinalizerProbeView(MediaPlayerElement mediaPlayerElement) : MauiMediaElement(mediaPlayerElement)
{
	static int completedFinalizerPaths;

	public static int CompletedFinalizerPaths => Volatile.Read(ref completedFinalizerPaths);

	// An exception in the finalizer path is deliberately not caught: it ends the process, which is what the product would do.
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (!disposing)
		{
			Interlocked.Increment(ref completedFinalizerPaths);
		}
	}
}

public sealed record WinMediaManagerFixture(WinMediaManager Manager, MediaElement View, MediaPlayerElement? Element, MediaPlayer? Player, WinMediaDispatcher? Dispatcher = null);

public abstract class WinMediaTestBase : MediaElementHandlerTestBase
{
	protected const int decodeTimeoutMilliseconds = 20000;

	// Media Foundation tries a refused connection (http://127.0.0.1:1) again for about 16 s before it raises MediaFailed (16.2 to 22.9 s measured
	// on a Windows 11 ARM64 VM and on the Windows Server 2025 runners), so decodeTimeoutMilliseconds is too short for the failed open.
	protected const int refusedConnectionTimeoutMilliseconds = 60000;

	readonly Lock cleanupGate = new();
	readonly List<Action> cleanups = [];

	protected void SetupWindowsBuilder(WinMediaLoggerFactory? loggerFactory = null) =>
		SetupBuilder(builder =>
		{
			builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<MediaElement, WinMediaHandler>());
			if (loggerFactory is not null)
			{
				builder.Services.AddSingleton<ILoggerFactory>(loggerFactory);
			}
		});

	// A run that requires DeviceTestCapabilities.MediaPlayback (the Windows CI jobs, which install Media Foundation) fails here instead of skipping,
	// so that a runner without media support cannot turn most of the area into skips.
	protected static async Task RequireMediaPlayerAsync() => DeviceTestCapabilities.Require(DeviceTestCapabilities.MediaPlayback, await WinMediaProbe.PlayerUnavailableReason);

	protected static async Task RequireDecoderAsync() => DeviceTestCapabilities.Require(DeviceTestCapabilities.MediaPlayback, await WinMediaProbe.DecoderUnavailableReason);

	// A run that declares VideoRendering missing (the Windows CI jobs: a runner without a GPU may render nothing) skips without probing.
	protected static async Task RequireVideoRenderingAsync()
	{
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.VideoRendering, await WinMediaProbe.VideoRenderingUnavailableReason);
	}

	// A probe that needs a MediaPlayer (or the decoder) answers with the player (or decoder) reason when there is none, so each Require*Async that
	// wraps such a probe first asks RequireMediaPlayerAsync or RequireDecoderAsync: that reason fails a run that requires MediaPlayback instead of
	// becoming a skip, whatever order a test calls the requirements in.
	static void SkipFor(string? environmentReason, string consequence)
	{
		if (environmentReason is not null)
		{
			throw new SkipException($"{consequence} ({environmentReason})");
		}
	}

	protected static async Task RequireDisplayUpdaterAsync()
	{
		await RequireMediaPlayerAsync();
		SkipFor(await WinMediaProbe.DisplayUpdaterRoundTripFailure, "This machine's system media controls cannot be read back, so the metadata the toolkit publishes cannot be observed");
	}

	protected static async Task RequireDisplayRequestCountingAsync() =>
		SkipFor(await WinMediaProbe.DisplayRequestCountingUnavailableReason, "The native DisplayRequest cannot be inspected on this machine");

	protected static async Task RequireMediaSessionAsync()
	{
		await RequireMediaPlayerAsync();
		SkipFor(await WinMediaProbe.MediaSessionUnavailableReason, "The system media sessions cannot be observed on this machine");
	}

	protected static async Task RequireHlsAsync()
	{
		// Its probe plays video, which a run that declares VideoRendering missing may not render, so that run skips without probing.
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.HlsUnavailableReason, "This machine's MediaPlayer does not play the local HLS stream");
	}

	protected static async Task RequireLiveDurationAsync()
	{
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.LiveDurationUnavailableReason, "This machine's MediaPlayer does not report an unknown duration for a live playlist");
	}

	protected static async Task RequireStarvationBufferingAsync()
	{
		// Its probe plays video, which a run that declares VideoRendering missing may not render, so that run skips without probing.
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.StarvationBufferingUnavailableReason, "This machine's MediaPlayer does not report Buffering when an HLS stream runs out of data");
	}

	protected static async Task RequireByteRangeAsync()
	{
		await RequireMediaPlayerAsync();
		SkipFor(await WinMediaProbe.ByteRangeUnavailableReason, "This machine's AdaptiveMediaSource does not fetch byte-range segments with their range");
	}

	protected static async Task RequireZeroRateAsync()
	{
		// Its probe plays video, which a run that declares VideoRendering missing may not render, so that run skips without probing.
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.ZeroRateUnavailableReason, "This machine's MediaPlayer does not keep a playback rate of 0");
	}

	protected static async Task RequirePlayAtZeroRateAsync()
	{
		// Its probe plays video, which a run that declares VideoRendering missing may not render, so that run skips without probing.
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.VideoRendering);
		await RequireDecoderAsync();
		SkipFor(await WinMediaProbe.PlayAtZeroRateUnavailableReason, "This machine's MediaPlayer does not report Playing while its playback rate is 0");
	}

	protected static async Task RequireEventSubscriptionControlAsync()
	{
		await RequireMediaPlayerAsync();
		SkipFor(await WinMediaProbe.EventSubscriptionControlFailure, "An event unsubscription cannot be proven through the garbage collector on this machine");
	}

	// Call on the UI thread.
	protected static void AssertDisplayRequest(WinMediaManager manager, bool active)
	{
		Assert.Equal(active, manager.IsDisplayRequestActive);
		Assert.Equal(active ? 1 : 0, WinMediaDisplayRequests.CountActive(manager.NativeDisplayRequest));
	}

	// The title and the artist of fixture's MediaElement must both be token.
	protected static async Task<GlobalSystemMediaTransportControlsSession> PublishSystemMediaSessionAsync(WinMediaManagerFixture fixture, string token)
	{
		await RequireMediaSessionAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var player = fixture.Player!;

			// The system media controls are driven by hand: the player's command manager would otherwise own them.
			player.CommandManager.IsEnabled = false;
			var controls = player.SystemMediaTransportControls;
			controls.IsEnabled = true;
			controls.IsPlayEnabled = true;
			controls.IsPauseEnabled = true;
			controls.IsNextEnabled = true;
			controls.PlaybackStatus = MediaPlaybackStatus.Playing;

			// Called directly because the manager runs it only on MediaOpened: it creates the Metadata helper (which subscribes to ButtonPressed) and
			// publishes the title.
			await (ValueTask)WinMediaReflection.Invoke(fixture.Manager, "UpdateMetadata")!;
		});

		var session = await WinMediaMediaSessions.FindAsync(token);
		Assert.True(session is not null, "This machine shows the system media session of a plain MediaPlayer, but not the one whose metadata the toolkit published.");
		return session;
	}

	protected void RegisterCleanup(Action cleanup)
	{
		lock (cleanupGate)
		{
			cleanups.Add(cleanup);
		}
	}

	protected static Task FlushDispatcherAsync() => InvokeOnMainThreadAsync(static () => { });

	protected static ConcurrentQueue<MediaFailedEventArgs> RecordFailures(MediaElement view)
	{
		var failures = new ConcurrentQueue<MediaFailedEventArgs>();
		view.MediaFailed += (_, e) => failures.Enqueue(e);
		return failures;
	}

	protected static void AssertReportsTheExceptionOfTheHandler(IEnumerable<MediaFailedEventArgs> failures, WinMediaLoggerFactory loggerFactory, Exception handlerException)
	{
		var failure = Assert.Single(failures);
		Assert.EndsWith($": {handlerException.GetType().FullName}: {handlerException.Message}", failure.ErrorMessage);
		Assert.Contains(loggerFactory.Entries, entry => entry.Level is LogLevel.Error && ReferenceEquals(entry.Exception, handlerException));
	}

	// XAML objects are thread-affine, so the condition is polled on the UI thread.
	protected static Task WaitUntilOnUiAsync(Func<bool> condition, string message, int timeoutMilliseconds = decodeTimeoutMilliseconds) =>
		AssertionExtensions.AssertEventuallyAsync(() => InvokeOnMainThreadAsync(condition), timeoutMilliseconds, 50, message);

	// Only for agile objects such as MediaPlayer and test servers: the condition is polled on the calling thread.
	protected static Task WaitUntilAsync(Func<bool> condition, string message, int timeoutMilliseconds = decodeTimeoutMilliseconds) =>
		AssertionExtensions.AssertEventually(condition, timeoutMilliseconds, 50, message);

	// Only a fixture that opens no media may pass dispatchRequired: false, because native media events arrive on other threads and must be dispatched.
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

	protected async Task<WinMediaHandler> ConnectAsync(MediaElement view)
	{
		RegisterCleanup(() =>
		{
			((IElementHandler?)view.Handler)?.DisconnectHandler();
			view.Dispose();
		});

		return await CreateHandlerAsync<WinMediaHandler>(view);
	}

	// AttachAndRun needs an explicit Task<bool> here: with an async lambda the generic Func<Window, T> overload would run the body detached from
	// the window's lifetime.
	protected Task HostAsync(FrameworkElement platformView, Func<WinUIWindow, Task> body) =>
		InvokeOnMainThreadAsync(() => platformView.AttachAndRun<bool>(async window =>
		{
			await body(window);
			return true;
		}, MauiContext));

	// Read on the UI thread: the XAML tree is thread-affine, the MediaPlayer itself is not.
	protected static Task<MediaPlayer> GetPlayerAsync(WinMediaHandler handler) => InvokeOnMainThreadAsync(() => handler.Player);

	// Every handler test needs a size: the Windows measure pass cannot work without one.
	protected static MediaElement NewMediaElement(Action<MediaElement>? configure = null)
	{
		var view = new MediaElement { WidthRequest = 160, HeightRequest = 120 };
		configure?.Invoke(view);
		return view;
	}

	// The manager does not dispose the player (MauiMediaElement does), so the tests do.
	protected static void ReleasePlayer(MediaPlayer? player, MediaPlayerElement? element = null)
	{
		// The element that shows the player lets go of it first: an element that was on screen keeps calling the player it holds from deferred
		// callbacks on the UI thread, and such a call into a disposed player fails with E_ABORT, which ends the app (a stowed exception,
		// 0xC000027B in CoreMessagingXP.dll). MauiMediaElement.Dispose detaches the player in the same UI-thread call that disposes it.
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

	// Runs on the UI thread because a Source change starts and stops the MediaElement's status timer, which belongs there. A view without a handler
	// has no mapper, so pass its manager to apply the source; the exceptions of the manager's UpdateSource then reach the caller.
	protected static Task SetSourceAsync(MediaElement view, CommunityToolkit.Maui.Views.MediaSource? source, MediaManager? manager = null) =>
		InvokeOnMainThreadAsync(async () =>
		{
			view.Source = source;
			if (manager is not null)
			{
				await manager.UpdateSource();
			}
		});

	// A view without a handler has no mapper, so pass its manager to apply the source.
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