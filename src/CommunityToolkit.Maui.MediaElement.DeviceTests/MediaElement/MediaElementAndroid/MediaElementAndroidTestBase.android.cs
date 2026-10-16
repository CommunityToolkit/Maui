using System.Reflection;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Service.Notification;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using AViewGroup = Android.Views.ViewGroup;
using MauiPlatform = Microsoft.Maui.ApplicationModel.Platform;

namespace CommunityToolkit.Maui.DeviceTests;

// Media used by the Android tests lives in Resources/Raw/exoplayer/ (MauiAssets, so they are opened with FileSystem.OpenAppPackageFileAsync
// and played through MediaSource.FromResource). They are generated with ffmpeg, bit-exact, so they are deterministic:
//   tone.wav        3 s, 8 kHz mono PCM16 sine (ffmpeg -f lavfi -i sine=frequency=440:sample_rate=8000:duration=3 -ac 1 -c:a pcm_s16le -fflags +bitexact -flags:a +bitexact -map_metadata -1)
//   tone_short.wav  0.6 s of the same, 880 Hz: short enough to be played to its end inside a test
//   video.mp4       2 s, 64x48, 10 fps H.264 baseline (testsrc pattern, no audio)
//   art.png         8x8 solid-color PNG used as MetadataArtworkUrl
//   empty.png       an empty file (0 bytes): artwork asset that exists but has no content

/// <summary>
/// Shared plumbing of the MediaElement-on-Android tests: a MauiApp whose MediaElement handler exposes its MediaManager, hosting
/// helpers that always release the player (and the foreground service) again, native-view accessors, assets and waits.
/// </summary>
public abstract class MediaElementAndroidTestBase : MediaElementHandlerTestBase
{
	protected const string toneAsset = "exoplayer/tone.wav";
	protected const string shortToneAsset = "exoplayer/tone_short.wav";
	protected const string videoAsset = "exoplayer/video.mp4";
	protected const string artworkAsset = "exoplayer/art.png";

	protected static readonly TimeSpan toneDuration = TimeSpan.FromSeconds(3);
	protected static readonly TimeSpan shortToneDuration = TimeSpan.FromSeconds(0.6);

	// ExoPlayer waits (opening, buffering, a 0.6 s clip playing out) are bounded by this; on a failure the assertion message names what never happened.
	protected const int waitTimeout = 8000;

	// Waits that need the video decoder: on a loaded emulator host the software H.264 decoder can take far longer than 8 s to start.
	protected const int videoWaitTimeout = 30000;

	// Android sheds an UPDATE of an app's notification while the app's estimated notification rate is above 5 per second
	// (NotificationManagerService logs "Package enqueue rate is ... Shedding"; seen on the API 36 emulator). A foreground-service test posts the
	// service notification and updates it at once, so back-to-back service tests push the estimate up until a later test loses the media
	// notification it waits for. After such a test the teardown pauses until the estimate is back at that of an app that posted nothing for a
	// while: the estimator has no observable state, so there is no event to wait for, only time.
	const int notificationRatePause = 1500;

	readonly List<MediaElement> hostedElements = [];
	readonly List<string> temporaryFiles = [];
	bool usesForegroundService;

	/// <summary>
	/// Builds the test's MauiApp with the toolkit's MediaElement handler replaced by <see cref="AndroidMediaElementProbeHandler"/>
	/// (a subclass: same behavior, plus access to the MediaManager).
	/// </summary>
	protected void SetupProbeBuilder(bool isAndroidForegroundServiceEnabled = false, Action<MediaElementOptions>? configureMediaElementOptions = null, Action<MauiAppBuilder>? configureBuilder = null)
	{
		usesForegroundService = isAndroidForegroundServiceEnabled || configureMediaElementOptions is not null;
		SetupBuilder(builder =>
		{
			builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<MediaElement, AndroidMediaElementProbeHandler>());
			configureBuilder?.Invoke(builder);
		}, isAndroidForegroundServiceEnabled, configureMediaElementOptions);
	}

	/// <summary>
	/// Puts the element into the runner's window, waits for it to be loaded, runs <paramref name="test"/> on the UI thread and
	/// always disconnects the handler afterwards (that releases the ExoPlayer and stops the foreground service).
	/// </summary>
	protected async Task HostAsync(MediaElement mediaElement, Func<AndroidMediaElementProbeHandler, Task> test)
	{
		hostedElements.Add(mediaElement);
		try
		{
			await AttachAndRun(mediaElement, async _ =>
			{
				await mediaElement.OnLoadedAsync();
				await test((AndroidMediaElementProbeHandler)mediaElement.Handler!);
			});
		}
		finally
		{
			await DisconnectAsync(mediaElement);
		}
	}

	/// <summary>Like the asynchronous <c>HostAsync</c> for a synchronous <paramref name="test"/>.</summary>
	protected Task HostAsync(MediaElement mediaElement, Action<AndroidMediaElementProbeHandler> test) =>
		HostAsync(mediaElement, handler =>
		{
			test(handler);
			return Task.CompletedTask;
		});

	/// <summary>
	/// Creates the handler on the UI thread without putting the view into a window (the lightest host: property mapping and
	/// player behavior need none), runs <paramref name="test"/> there and always disconnects the handler afterwards.
	/// </summary>
	protected async Task WithHandlerAsync(MediaElement mediaElement, Func<AndroidMediaElementProbeHandler, Task> test)
	{
		hostedElements.Add(mediaElement);
		try
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				var handler = CreateHandler<AndroidMediaElementProbeHandler>(mediaElement);
				await test(handler);
			});
		}
		finally
		{
			await DisconnectAsync(mediaElement);
		}
	}

	/// <summary>Like <see cref="WithHandlerAsync(MediaElement, Func{AndroidMediaElementProbeHandler, Task})"/> for a synchronous <paramref name="test"/>.</summary>
	protected Task WithHandlerAsync(MediaElement mediaElement, Action<AndroidMediaElementProbeHandler> test) =>
		WithHandlerAsync(mediaElement, handler =>
		{
			test(handler);
			return Task.CompletedTask;
		});

	/// <summary>Creates a 200x100 MediaElement on the UI thread (setting its Source off that thread would create its status timer there).</summary>
	protected static Task<MediaElement> CreateMediaElementAsync(Action<MediaElement>? configure = null) =>
		CreateMediaElementAsync(() => new MediaElement { WidthRequest = 200, HeightRequest = 100 }, configure);

	/// <summary>Creates the MediaElement with <paramref name="create"/> on the UI thread, for options that are init-only (AndroidViewType).</summary>
	protected static Task<MediaElement> CreateMediaElementAsync(Func<MediaElement> create, Action<MediaElement>? configure = null) =>
		InvokeOnMainThreadAsync(() =>
		{
			var mediaElement = create();
			configure?.Invoke(mediaElement);
			return mediaElement;
		});

	protected static Task DisconnectAsync(MediaElement mediaElement) =>
		InvokeOnMainThreadAsync(() => mediaElement.Handler?.DisconnectHandler());

	/// <summary>
	/// Disposes <paramref name="mediaElement"/> when the test ends, for an element that is used without HostAsync/WithHandlerAsync: setting a
	/// Source starts its 200 ms status timer, which would otherwise tick for the rest of the run.
	/// </summary>
	protected void DisposeWhenTheTestEnds(MediaElement mediaElement) => hostedElements.Add(mediaElement);

	/// <summary>
	/// Puts the MediaElement into a known state without going through the player. Listener tests start from a state that none of the
	/// mappings produces (Opening), so that every assertion of a mapped state shows that the mapping ran.
	/// </summary>
	protected static void SetCurrentState(MediaElement mediaElement, MediaElementState state) =>
		((IMediaElement)mediaElement).CurrentStateChanged(state);

	/// <summary>
	/// The video tests need the H.264 decoder. On the API 28 emulator the media codec service (omx@1.0-service) crashes now and then while it
	/// decodes (SIGSEGV, "&gt;&gt;&gt; media.codec &lt;&lt;&lt;" in logcat) and ExoPlayer fails the load with ERROR_CODE_DECODING_FAILED: 1 of 30
	/// loads of the 2 s test video, and it was seen in full runs too. None of the
	/// code these tests cover depends on the Android version (OnVideoSizeChanged, the resize modes, the TextureView), so they run on API 29 and
	/// later and on devices, and skip visibly on the old emulator instead of failing CI at random.
	/// </summary>
	protected static void SkipWhereTheVideoDecoderIsUnreliable() =>
		Skip.If(!OperatingSystem.IsAndroidVersionAtLeast(29) && IsEmulator(),
			"The API 28 emulator's media codec service crashes intermittently while decoding H.264 (ERROR_CODE_DECODING_FAILED); the video tests cover no version-specific code and run on API 29+ and on devices.");

	static bool IsEmulator() => Android.OS.Build.Hardware is "ranchu" or "goldfish";

	protected static MauiMediaElement GetMauiMediaElement(MediaElement mediaElement) =>
		(MauiMediaElement)(mediaElement.Handler?.PlatformView ?? throw new InvalidOperationException("The MediaElement has no handler."));

	// MauiMediaElement (CoordinatorLayout) > RelativeLayout > PlayerView
	protected static PlayerView GetPlayerView(MauiMediaElement platformView) =>
		(PlayerView)((AViewGroup)platformView.GetChildAt(0)!).GetChildAt(0)!;

	protected static PlayerView GetPlayerView(MediaElement mediaElement) => GetPlayerView(GetMauiMediaElement(mediaElement));

	protected static IExoPlayer GetPlayer(MediaElement mediaElement) => (IExoPlayer)GetPlayerView(mediaElement).Player!;

	// ---- MediaControlsService (the foreground service): what the system reports about it, and the toolkit's private state

	/// <summary>The class name that the manifest gives the service ([Service(Name = ...)] on MediaControlsService).</summary>
	protected const string mediaControlsServiceName = "communityToolkit.maui.media.services";

	/// <summary>The id that MediaControlsService gives its notification (foreground service and media notification share it).</summary>
	protected const int serviceNotificationId = 1;

	const string mediaStyleTemplate = "android.app.Notification$MediaStyle";

	/// <summary>The running MediaControlsService of this app as the ActivityManager reports it, or null when it is not running.</summary>
	protected static ActivityManager.RunningServiceInfo? FindMediaControlsService()
	{
		var context = MauiPlatform.AppContext;
		var activityManager = (ActivityManager)context.GetSystemService(Context.ActivityService)!;
#pragma warning disable CS0618, CA1422 // Deprecated for other apps' services: it still lists the caller's own, which is all that is asked here.
		var services = activityManager.GetRunningServices(200);
#pragma warning restore CS0618, CA1422
		return services?.FirstOrDefault(info => info.Service?.PackageName == context.PackageName && info.Service?.ClassName == mediaControlsServiceName);
	}

	/// <summary>The notifications of this app that are currently posted.</summary>
	protected static StatusBarNotification[] GetActiveNotifications()
	{
		var notificationManager = (NotificationManager)MauiPlatform.AppContext.GetSystemService(Context.NotificationService)!;
		return notificationManager.GetActiveNotifications() ?? [];
	}

	/// <summary>The notification that MediaControlsService posts (id 1), or null.</summary>
	protected static Notification? FindServiceNotification() =>
		GetActiveNotifications().FirstOrDefault(notification => notification.Id == serviceNotificationId)?.Notification;

	protected static bool IsMediaStyle(Notification? notification) => notification?.Extras?.GetString(Notification.ExtraTemplate) == mediaStyleTemplate;

	/// <summary>Polls until the service is gone and has no notification left (the system stops it a few moments after StopService).</summary>
	protected static Task WaitForMediaControlsServiceToStopAsync(int timeout = waitTimeout) =>
		WaitUntilAsync(() => FindMediaControlsService() is null && GetActiveNotifications().Length == 0, "MediaControlsService was never stopped (or its notification stayed).", timeout);

	// The manager's service connection and media session are private; the system's view of them is what the tests assert.
	private protected static BoundServiceConnection? GetServiceConnection(MediaManager manager) =>
		(BoundServiceConnection?)typeof(MediaManager).GetField("connection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);

	protected static MediaSession? GetMediaSession(MediaManager manager) =>
		(MediaSession?)typeof(MediaManager).GetField("session", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);

	protected static async Task<byte[]> ReadAssetAsync(string asset)
	{
		await using var input = await FileSystem.OpenAppPackageFileAsync(asset);
		using var output = new MemoryStream();
		await input.CopyToAsync(output);
		return output.ToArray();
	}

	/// <summary>Copies an asset to a uniquely named file in the cache directory (deleted when the test ends) and returns its path.</summary>
	protected async Task<string> CopyAssetToCacheAsync(string asset)
	{
		var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}_{Path.GetFileName(asset)}");
		await File.WriteAllBytesAsync(path, await ReadAssetAsync(asset));
		temporaryFiles.Add(path);
		return path;
	}

	/// <summary>
	/// Like AssertionExtensions.WaitForGC (up to 40 forced collections per reference), but names the survivors itself: WaitForGC describes a
	/// survivor with its ToString(), which throws ObjectDisposedException for a Java peer that was disposed (a leaked platform view).
	/// </summary>
	protected static async Task AssertCollectedAsync(params (string Name, WeakReference Reference)[] references)
	{
		var collected = await AssertionExtensions.WaitForCollect(references.Select(reference => reference.Reference).ToArray());

		Assert.True(collected, $"Still alive after the forced collections: {string.Join(", ", references.Where(reference => reference.Reference.IsAlive).Select(reference => reference.Name))}.");
	}

	/// <summary>Polls <paramref name="condition"/> every 25 ms until it holds; fails with <paramref name="message"/> after <paramref name="timeout"/> ms.</summary>
	protected static Task WaitUntilAsync(Func<bool> condition, string message, int timeout = waitTimeout) =>
		AssertEventually(condition, timeout, 25, message);

	protected static Task WaitForStateAsync(MediaElement mediaElement, MediaElementState state, int timeout = waitTimeout) =>
		WaitUntilAsync(() => mediaElement.CurrentState == state, $"The MediaElement never reached {state}.", timeout);

	/// <summary>
	/// Waits until the MediaElement's source is loaded: MediaOpened was raised and the player knows the duration. A MediaFailed ends the wait
	/// at once and fails with the player's error, instead of waiting for the timeout and reporting a missing duration.
	/// </summary>
	protected static async Task WaitUntilLoadedAsync(MediaElement mediaElement, MediaElementRecorder recorder, int timeout = waitTimeout)
	{
		var loaded = await Wait(() => recorder.FailureMessages.Count > 0 || (recorder.OpenedCount > 0 && mediaElement.Duration > TimeSpan.Zero), timeout, 25);

		AssertNoMediaFailure(recorder);
		Assert.True(loaded, recorder.OpenedCount == 0 ? "MediaOpened was never raised." : "The duration was never reported.");
	}

	/// <summary>Waits until the player reported the size of the video (OnVideoSizeChanged); a MediaFailed ends the wait at once.</summary>
	protected static async Task WaitUntilVideoSizeIsKnownAsync(MediaElement mediaElement, MediaElementRecorder recorder)
	{
		var known = await Wait(() => recorder.FailureMessages.Count > 0 || mediaElement.MediaWidth > 0, videoWaitTimeout, 25);

		AssertNoMediaFailure(recorder);
		Assert.True(known, "The video size was never reported.");
	}

	static void AssertNoMediaFailure(MediaElementRecorder recorder) =>
		Assert.True(recorder.FailureMessages.Count == 0, $"The media failed to load: {string.Join(" | ", recorder.FailureMessages)}");

	// Handlers are disconnected before the base class disposes the MauiApp, so a test that failed half way never leaves a player or
	// the foreground service running for the next test.
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			foreach (var mediaElement in hostedElements)
			{
				try
				{
					await DisconnectAsync(mediaElement);
				}
				catch (Exception ex) when (ex is ObjectDisposedException or Java.Lang.Throwable)
				{
					// Already released by the test itself.
				}

				// Stops the status timer that setting a Source started: it would tick (and keep the element alive) for the rest of the run.
				await InvokeOnMainThreadAsync(() => mediaElement.Dispose());
			}

			// Disconnecting stops MediaControlsService asynchronously: the next test must not start in the middle of that.
			await WaitForMediaControlsServiceToStopAsync();

			if (usesForegroundService)
			{
				await Task.Delay(notificationRatePause);
			}
		}
		finally
		{
			try
			{
				await base.DisposeAsyncCore();
			}
			finally
			{
				foreach (var file in temporaryFiles)
				{
					File.Delete(file);
				}
			}
		}
	}
}

/// <summary>Records the events of a <see cref="MediaElement"/> so that a test can assert on what was raised and in which order.</summary>
public sealed class MediaElementRecorder : IDisposable
{
	readonly MediaElement mediaElement;
	readonly object gate = new();
	readonly List<(MediaElementState Previous, MediaElementState Current)> transitions = [];
	readonly List<string> failureMessages = [];
	int openedCount, endedCount, seekCompletedCount;

	public MediaElementRecorder(MediaElement mediaElement)
	{
		this.mediaElement = mediaElement;
		mediaElement.StateChanged += OnStateChanged;
		mediaElement.MediaOpened += OnMediaOpened;
		mediaElement.MediaEnded += OnMediaEnded;
		mediaElement.MediaFailed += OnMediaFailed;
		mediaElement.SeekCompleted += OnSeekCompleted;
	}

	public int OpenedCount => Read(() => openedCount);

	public int EndedCount => Read(() => endedCount);

	public int SeekCompletedCount => Read(() => seekCompletedCount);

	public IReadOnlyList<string> FailureMessages => Read(() => failureMessages.ToArray());

	public IReadOnlyList<(MediaElementState Previous, MediaElementState Current)> Transitions => Read(() => transitions.ToArray());

	public IReadOnlyList<MediaElementState> States => Read(() => transitions.Select(t => t.Current).ToArray());

	public void Dispose()
	{
		mediaElement.StateChanged -= OnStateChanged;
		mediaElement.MediaOpened -= OnMediaOpened;
		mediaElement.MediaEnded -= OnMediaEnded;
		mediaElement.MediaFailed -= OnMediaFailed;
		mediaElement.SeekCompleted -= OnSeekCompleted;
	}

	T Read<T>(Func<T> read)
	{
		lock (gate)
		{
			return read();
		}
	}

	void OnStateChanged(object? sender, MediaStateChangedEventArgs e) => Write(() => transitions.Add((e.PreviousState, e.NewState)));

	void OnMediaOpened(object? sender, EventArgs e) => Write(() => openedCount++);

	void OnMediaEnded(object? sender, EventArgs e) => Write(() => endedCount++);

	void OnMediaFailed(object? sender, MediaFailedEventArgs e) => Write(() => failureMessages.Add(e.ErrorMessage));

	void OnSeekCompleted(object? sender, EventArgs e) => Write(() => seekCompletedCount++);

	void Write(Action write)
	{
		lock (gate)
		{
			write();
		}
	}
}

/// <summary>
/// A JNI global reference to a Java object. Disposing a managed peer (what <c>Dispose()</c> on a view or player does) invalidates the
/// peer, so the state of a Java object that was released can only be read through a new peer that wraps the same object. This keeps the
/// Java object reachable and hands out such a peer.
/// </summary>
public sealed class JavaObjectReference : IDisposable
{
	nint globalReference;

	public JavaObjectReference(Java.Lang.Object javaObject) => globalReference = JNIEnv.NewGlobalRef(javaObject.Handle);

	/// <summary>A new managed peer for the referenced Java object.</summary>
	public T GetPeer<T>() where T : class, IJavaObject =>
		Java.Lang.Object.GetObject<T>(globalReference, JniHandleOwnership.DoNotTransfer) ?? throw new InvalidOperationException("The Java object is gone.");

	public void Dispose()
	{
		if (globalReference != 0)
		{
			JNIEnv.DeleteGlobalRef(globalReference);
			globalReference = 0;
		}
	}
}

/// <summary>An <see cref="ILoggerFactory"/> that keeps what the toolkit logs, to assert on error logging.</summary>
public sealed class CapturingLoggerFactory : ILoggerFactory
{
	readonly object gate = new();
	readonly List<(LogLevel Level, string Category, string Message)> entries = [];

	public IReadOnlyList<(LogLevel Level, string Category, string Message)> Entries
	{
		get
		{
			lock (gate)
			{
				return entries.ToArray();
			}
		}
	}

	public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

	public void AddProvider(ILoggerProvider provider)
	{
	}

	public void Dispose()
	{
	}

	sealed class CapturingLogger(CapturingLoggerFactory factory, string category) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			lock (factory.gate)
			{
				factory.entries.Add((logLevel, category, formatter(state, exception)));
			}
		}
	}
}