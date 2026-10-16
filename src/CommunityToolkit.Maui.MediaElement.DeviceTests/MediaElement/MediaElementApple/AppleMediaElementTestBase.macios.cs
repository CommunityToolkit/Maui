using System.Reflection;
using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using CoreMedia;
using Foundation;
using MediaPlayer;
using Microsoft.Extensions.Logging;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Base class of the iOS and Mac Catalyst MediaElement tests. <see cref="SetupApple"/> builds the test's MauiApp with the toolkit
/// handler replaced by <see cref="AppleMediaElementHandler"/> (so tests can reach the protected <see cref="MediaManager"/>) and a
/// logger that records what the toolkit logs. Media comes from the tiny files in Resources/Raw/avplayer, made with ffmpeg: a 2 s,
/// 64x48, 10 fps H.264 baseline video of the testsrc2 pattern (moov atom first, at the end, rotated 90 degrees through the display
/// matrix, and in a QuickTime container), a 1 s 440 Hz AAC tone, a 40x30 PNG poster, a well-formed MP4 without any track, and a
/// 160x120 H.264 video of 61 frames at 29.97 fps, whose duration is not a whole number of milliseconds.
/// </summary>
public abstract class AppleMediaElementTestBase : MediaElementHandlerTestBase
{
	/// <summary>2 s, 64x48, H.264 video without audio, moov atom first.</summary>
	internal const string VideoResource = "avplayer/video.mp4";

	/// <summary>The video with the display matrix rotated by 90 degrees (displayed as 48x64).</summary>
	internal const string RotatedVideoResource = "avplayer/video_rotated.mp4";

	/// <summary>1 s AAC audio, no video track.</summary>
	internal const string AudioResource = "avplayer/audio.m4a";

	/// <summary>A well-formed MP4 without any track.</summary>
	internal const string TracklessResource = "avplayer/trackless.mp4";

	/// <summary>
	/// 61061/30000 s (2035.3667 ms: 61 frames at 29.97 fps), 160x120, H.264 video without audio, key frames at 0 and 1.668 s: its duration is
	/// not a whole number of milliseconds.
	/// </summary>
	internal const string NtscVideoResource = "avplayer/video_ntsc.mp4";

	/// <summary>The audio clip, but stored in the bundle root.</summary>
	internal const string RootAudioResource = "root_audio.m4a";

	internal static readonly TimeSpan VideoDuration = TimeSpan.FromSeconds(2);

	internal AppleCapturingLoggerFactory Logs { get; } = new();

	// Runs after every test. Every MediaManager that updates its source creates a Metadata, which registers six targets on the process-wide
	// MPRemoteCommandCenter (disposing the manager removes them, but a Metadata that a test creates itself keeps them) and publishes its Now
	// Playing info on the process-wide MPNowPlayingInfoCenter (MetadataAppleTests.DisconnectingTheMediaElementClearsItsNowPlayingInfo).
	// Both are reset, so that no test leaves targets behind (they keep its Metadata and AVPlayer reachable and would answer a media key
	// pressed during a later test) or the Now Playing info of its media.
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				AppleRemoteCommands.RemoveAllTargets();

				// The binding's setter accepts null and publishes no info, which is the state of a process that never published any.
				MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = null!;
			});
		}
	}

	/// <summary>Builds this test's MauiApp. Call it first in every test (it throws once a handler exists).</summary>
	protected void SetupApple(Action<MauiAppBuilder>? configureBuilder = null) =>
		SetupBuilder(builder =>
		{
			builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<MediaElement, AppleMediaElementHandler>());
			builder.Services.AddSingleton<ILoggerFactory>(Logs);
			configureBuilder?.Invoke(builder);
		});

	protected static MediaElement CreateMediaElement() => new() { WidthRequest = 200, HeightRequest = 100 };

	/// <summary>Absolute path of a bundled test file (<c>Resources/Raw/avplayer/&lt;name&gt;.&lt;extension&gt;</c>).</summary>
	protected static string BundlePath(string name, string extension) =>
		NSBundle.MainBundle.PathForResource(name, extension, "avplayer") ?? throw new FileNotFoundException($"{name}.{extension} is not in the app bundle.");

	protected static string BundleFileUrl(string name, string extension) => NSUrl.FromFilename(BundlePath(name, extension)).AbsoluteString!;

	/// <summary>
	/// Hosts <paramref name="mediaElement"/> in a <see cref="ContentPage"/> inside the runner's window (MauiMediaElement needs a
	/// parent page), runs <paramref name="action"/> on the UI thread once the page is loaded and always disconnects the handlers
	/// afterwards, which releases the AVPlayer.
	/// </summary>
	internal async Task HostAsync(MediaElement mediaElement, Func<AppleMediaElementHandler, Task> action)
	{
		var page = new ContentPage { Content = new VerticalStackLayout { mediaElement } };
		try
		{
			await AttachAndRun(page, async _ =>
			{
				await page.OnLoadedAsync();
				await action((AppleMediaElementHandler)mediaElement.Handler!);
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				mediaElement.Handler?.DisconnectHandler();
				page.Handler?.DisconnectHandler();
				mediaElement.Dispose();
			});
		}
	}

	/// <summary>
	/// Sets the source and waits until the media is opened (MediaOpened, after which the media size and the poster are set) and the
	/// platform player can play (and seek in) it.
	/// </summary>
	internal static async Task LoadAsync(MediaElement mediaElement, AppleMediaElementHandler handler, MediaSource source)
	{
		var opened = WhenOpenedOrFailedAsync(mediaElement, openedOnly: true);
		mediaElement.Source = source;
		await handler.Manager!.TakeSourceUpdates();
		await WaitAsync(opened, "MediaOpened was not raised.");
		await WaitUntilReadyAsync(handler);
	}

	/// <summary>
	/// Sets the source and waits until the update has settled: its PlatformUpdateSource calls have finished and, when it created an
	/// AVPlayerItem, the media was opened (MediaOpened) or has failed (MediaFailed). Returns the exception of the setter (since #3281
	/// PlatformUpdateSource runs synchronously, so its exceptions reach the code that sets Source) or of the update, or null.
	/// </summary>
	internal static async Task<Exception?> SetSourceAsync(MediaElement mediaElement, AppleMediaElementHandler handler, MediaSource? source)
	{
		var settled = WhenOpenedOrFailedAsync(mediaElement);
		var setterException = Record.Exception(() => mediaElement.Source = source);
		var updateException = await Record.ExceptionAsync(handler.Manager!.TakeSourceUpdates);
		var exception = setterException ?? updateException;
		if (exception is null && handler.Manager.NativePlayerItem is not null)
		{
			await WaitAsync(settled, "The media was neither opened nor failed.");
		}

		return exception;
	}

	/// <summary>
	/// A task that completes after the next MediaOpened (or MediaFailed) of <paramref name="mediaElement"/>, and only once the main-thread
	/// work that raised the event has finished, so whatever the product does right after the event (since #3281: the poster) has happened.
	/// Create it before the change that starts the update: before #3281 MediaOpened is raised inside the Source setter.
	/// </summary>
	internal static Task WhenOpenedOrFailedAsync(MediaElement mediaElement, bool openedOnly = false)
	{
		var waiter = new EventWaiter(mediaElement, openedOnly);

		// MediaElement's (MAUI's) WeakEventManager holds the handlers' target weakly: the element keeps the waiter alive instead.
		eventWaiters.GetOrCreateValue(mediaElement).Add(waiter);
		return waiter.Task;
	}

	static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MediaElement, System.Collections.Concurrent.ConcurrentBag<EventWaiter>> eventWaiters = new();

	sealed class EventWaiter
	{
		readonly TaskCompletionSource settled = new(TaskCreationOptions.RunContinuationsAsynchronously);
		readonly MediaElement mediaElement;

		public EventWaiter(MediaElement mediaElement, bool openedOnly)
		{
			this.mediaElement = mediaElement;
			mediaElement.MediaOpened += OnOpened;
			if (!openedOnly)
			{
				mediaElement.MediaFailed += OnFailed;
			}
		}

		public Task Task => settled.Task;

		void OnOpened(object? sender, EventArgs e) => Complete();

		void OnFailed(object? sender, MediaFailedEventArgs e) => Complete();

		void Complete()
		{
			mediaElement.MediaOpened -= OnOpened;
			mediaElement.MediaFailed -= OnFailed;
			settled.TrySetResult();
		}
	}

	/// <summary>Awaits <paramref name="task"/> for at most <paramref name="timeout"/> milliseconds; fails with <paramref name="message"/> after that.</summary>
	internal static async Task WaitAsync(Task task, string message, int timeout = 5000)
	{
		try
		{
			await task.WaitAsync(TimeSpan.FromMilliseconds(timeout));
		}
		catch (TimeoutException)
		{
			throw new TimeoutException(message);
		}
	}

	/// <summary>
	/// Completes once the blocks that are already queued on the main dispatch queue have run (it queues one more and waits for it). Since
	/// #3281 the item's status observer queues its handler there, so this lets an already queued handler run before a test asserts that
	/// it did nothing.
	/// </summary>
	internal static Task DrainMainQueueAsync()
	{
		var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() => drained.TrySetResult());
		return drained.Task;
	}

	internal static Task WaitUntilReadyAsync(AppleMediaElementHandler handler) =>
		AssertionExtensions.AssertEventually(
			() => handler.Manager!.NativePlayer is { Status: AVPlayerStatus.ReadyToPlay, CurrentItem: { Status: AVPlayerItemStatus.ReadyToPlay } item } && item.SeekableTimeRanges.Length > 0,
			timeout: 5000,
			interval: 20,
			message: "The media did not become ready to play.");

	protected static Task WaitUntilAsync(Func<bool> condition, string message, int timeout = 5000) =>
		AssertionExtensions.AssertEventually(condition, timeout: timeout, interval: 20, message: message);

	protected static ResourceMediaSource ResourceSource(string path) => (ResourceMediaSource)MediaSource.FromResource(path);

	/// <summary>The toolkit's <c>Metadata</c> instance of a manager (it keeps it in a private field).</summary>
	internal static Metadata GetMetadata(MediaManager manager) =>
		(Metadata)typeof(MediaManager).GetField("metaData", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!;

	internal static StreamAssetResourceLoader? GetStreamResourceLoader(MediaManager manager) =>
		(StreamAssetResourceLoader?)typeof(MediaManager).GetField("streamResourceLoader", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager);

	protected static double Seconds(CMTime time) => time.Seconds;

	/// <summary>Records the events a MediaElement raises, in order.</summary>
	protected sealed class MediaElementEvents
	{
		public MediaElementEvents(MediaElement mediaElement)
		{
			mediaElement.StateChanged += (_, args) => States.Add(args.NewState);
			mediaElement.MediaOpened += (_, _) => Opened++;
			mediaElement.MediaEnded += (_, _) => Ended++;
			mediaElement.MediaFailed += (_, args) => Failures.Add(args.ErrorMessage);
			mediaElement.SeekCompleted += (_, _) => SeekCompleted++;
		}

		public List<MediaElementState> States { get; } = [];

		public List<string> Failures { get; } = [];

		public int Opened { get; private set; }

		public int Ended { get; private set; }

		public int SeekCompleted { get; private set; }
	}
}