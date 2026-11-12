using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel;
using Windows.Media.Control;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.System.Display;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.DeviceTests;

// The WinMedia prefix keeps the type names unique in the shared CommunityToolkit.Maui.DeviceTests namespace of the app.

/// <summary>
/// The assets under Resources/Raw/mediaplayer are generated, not downloaded (ffmpeg 8.1, bit-exact, so regenerating gives the same bytes):
/// <code>
/// ffmpeg -f lavfi -i testsrc=size=128x96:rate=10:duration=2 -c:v libx264 -profile:v baseline -level 3.0 -pix_fmt yuv420p -g 5 -bf 0 -an
///        -movflags +faststart -map_metadata -1 -fflags +bitexact -flags:v +bitexact video.mp4
/// ffmpeg -f lavfi -i color=c=0x2060c0:s=16x16 -frames:v 1 -map_metadata -1 artwork.png
/// ffmpeg -i video.mp4 -c copy -bsf:v h264_mp4toannexb -f mpegts -fflags +bitexact -flags:v +bitexact -map_metadata -1
///        -muxdelay 0 -muxpreload 0 segment.ts
/// </code>
/// </summary>
static class WinMediaAssets
{
	/// <summary>Has no audio track, so playing it needs no audio endpoint.</summary>
	public const string VideoRelativePath = "mediaplayer/video.mp4";

	public const string CorruptRelativePath = "mediaplayer/corrupt.mp4";

	public const string ArtworkRelativePath = "mediaplayer/artwork.png";

	public const string SegmentRelativePath = "mediaplayer/segment.ts";

	public const int VideoWidth = 128;
	public const int VideoHeight = 96;

	public static readonly TimeSpan VideoDuration = TimeSpan.FromSeconds(2);

	public static string VideoPath => FullPath(VideoRelativePath);

	public static string CorruptPath => FullPath(CorruptRelativePath);

	public static string ArtworkPath => FullPath(ArtworkRelativePath);

	public static Uri VideoUri => new(VideoPath);

	/// <summary>
	/// Only for XAML (BitmapImage): RandomAccessStreamReference.CreateFromUri, which the toolkit uses for the thumbnail of the system media
	/// controls, accepts only http, https, ms-appx and ms-appdata, not this file: URI, so the metadata tests serve the artwork with
	/// <see cref="StartArtworkServer"/> instead.
	/// </summary>
	public static Uri ArtworkUri => new(ArtworkPath);

	public static Uri CorruptUri => new(CorruptPath);

	public const string ArtworkServerPath = "artwork.png";

	// Tests open media on a MediaPlayer through this, never SetUriSource: a MediaPlaybackItem becomes the player's current item when it is set,
	// while for a SetUriSource source the player looks that item up as the media opens, and Windows.Media.Playback.MediaPlayer.dll
	// 10.0.26100.33438 (the Windows Server 2025 runners, where Feature_Servicing_MediaPlayer_CurrentMediaPlaybackItem_RaceConditionFix is off)
	// can get null there and dereference it: the access violation at +0x3299d that ended CI runs 38115049024 and 39907681941.
	public static MediaPlaybackItem NewPlaybackItem(Uri source) => new(WinMediaSource.CreateFromUri(source));

	public static void DisposeSource(MediaPlayer player) =>
		(player.Source is MediaPlaybackItem item ? item.Source : player.Source as IDisposable)?.Dispose();

	public static byte[] ReadVideoBytes() => File.ReadAllBytes(VideoPath);

	public static byte[] ReadArtworkBytes() => File.ReadAllBytes(ArtworkPath);

	public static byte[] ReadSegmentBytes() => File.ReadAllBytes(FullPath(SegmentRelativePath));

	public static string FullPath(string relativePath) =>
		Path.Combine(WinMediaPackaging.ExpectedPackageFilePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

	public static WinMediaHttpServer StartArtworkServer()
	{
		var artwork = ReadArtworkBytes();
		return new WinMediaHttpServer
		{
			Handler = request => request.Path == "/" + ArtworkServerPath ? WinMediaResponse.ServeBytes(request, artwork, "image/png") : WinMediaResponse.NotFound(),
		}.Start();
	}

	// The HRESULTs, in order: RPC_E_SERVER_DIED_DNE, RPC_E_SERVER_DIED, RPC_E_DISCONNECTED and RPC_S_SERVER_UNAVAILABLE.
	public static bool IsOutOfProcessServerGone(Exception exception) =>
		exception is COMException { HResult: unchecked((int)0x80010012) or unchecked((int)0x80010007) or unchecked((int)0x80010108) or unchecked((int)0x800706BA) };

	/// <summary>
	/// The out-of-process stream behind a thumbnail fails now and then for reasons unrelated to what was published: the process that serves it
	/// went away (<see cref="IsOutOfProcessServerGone"/>; seen on the Windows Server 2025 runners), or the stream reported its file missing
	/// (FileNotFoundException, 0x80070002; seen in WinMediaProbe's DisplayUpdater probe on the Windows 11 VM, whose next run read the same artwork).
	/// </summary>
	public static bool IsTransientThumbnailReadFailure(Exception exception) =>
		IsOutOfProcessServerGone(exception) || exception is FileNotFoundException { HResult: unchecked((int)0x80070002) };

	public static async Task<byte[]> ReadAllBytesAsync(IRandomAccessStreamReference reference)
	{
		using var stream = await reference.OpenReadAsync();
		using var managedStream = stream.AsStreamForRead();
		using var copy = new MemoryStream();
		await managedStream.CopyToAsync(copy);
		return copy.ToArray();
	}
}

public enum WinMediaSeekOutsideTheMedia
{
	BeforeTheStart,

	BeyondTheEnd,

	TheEnd
}

static class WinMediaSeeks
{
	public static (TimeSpan Requested, TimeSpan Expected) OutsideTheMedia(WinMediaSeekOutsideTheMedia target, TimeSpan duration) => target switch
	{
		WinMediaSeekOutsideTheMedia.BeforeTheStart => (TimeSpan.FromSeconds(-1), TimeSpan.Zero),
		WinMediaSeekOutsideTheMedia.BeyondTheEnd => (duration + TimeSpan.FromSeconds(3), duration),
		WinMediaSeekOutsideTheMedia.TheEnd => (duration, duration),
		_ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
	};
}

/// <summary>
/// MauiMediaElement toggles the window that hosts the element and falls back to GetForegroundWindow when it cannot resolve that window, so the
/// full-screen tests bring their own window to the front first (Windows may refuse; the tests then skip visibly).
/// </summary>
static class WinMediaWindowing
{
	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

	[DllImport("kernel32.dll")]
	static extern uint GetCurrentThreadId();

	public static IntPtr GetHandle(Microsoft.UI.Xaml.Window window) => WinRT.Interop.WindowNative.GetWindowHandle(window);

	/// <summary>Call it on the thread of the window.</summary>
	public static async Task<bool> TryBringToForegroundAsync(IntPtr handle)
	{
		for (var attempt = 0; attempt < 20; attempt++)
		{
			var foreground = Maui.Core.Views.MauiMediaElement.TryGetForegroundWindow();
			if (foreground == handle)
			{
				return true;
			}

			// Windows lets a thread take the foreground only when its process owns it (or received the last input). With its input attached to
			// the thread of the window that holds the foreground, as in a long run where the windows of earlier tests came and went, it may.
			var foregroundThread = foreground is IntPtr foregroundWindow ? GetWindowThreadProcessId(foregroundWindow, out _) : 0;
			var thisThread = GetCurrentThreadId();
			var isAttached = foregroundThread is not 0 && foregroundThread != thisThread && AttachThreadInput(thisThread, foregroundThread, true);

			try
			{
				SetForegroundWindow(handle);
			}
			finally
			{
				if (isAttached)
				{
					AttachThreadInput(thisThread, foregroundThread, false);
				}
			}

			await Task.Delay(100);
		}

		return Maui.Core.Views.MauiMediaElement.TryGetForegroundWindow() == handle;
	}

	public static string DescribeForegroundWindow()
	{
		if (Maui.Core.Views.MauiMediaElement.TryGetForegroundWindow() is not IntPtr foreground)
		{
			return "there is no foreground window";
		}

		GetWindowThreadProcessId(foreground, out var processId);

		try
		{
			using var process = Process.GetProcessById((int)processId);
			return $"the foreground window belongs to {process.ProcessName} ({(processId == Environment.ProcessId ? "this app" : $"process {processId}")})";
		}
		catch (ArgumentException)
		{
			return $"the foreground window belongs to process {processId}, which has ended";
		}
	}
}

/// <summary>
/// Asking the session of this app to play, pause or skip raises SystemMediaTransportControls.ButtonPressed on the app's own controls exactly as
/// a key press does, without touching any other app: the session is found by a unique title that the test published itself.
/// </summary>
static class WinMediaMediaSessions
{
	/// <summary>
	/// The system media session service runs out of process: an answer that never comes must end the test (or the lookup), not hang the whole run.
	/// </summary>
	public static Task<T> Bounded<T>(Windows.Foundation.IAsyncOperation<T> operation, int timeoutMilliseconds = 15000) =>
		operation.AsTask().WaitAsync(TimeSpan.FromMilliseconds(timeoutMilliseconds));

	public static async Task<GlobalSystemMediaTransportControlsSession?> FindAsync(string title, int timeoutMilliseconds = 15000)
	{
		GlobalSystemMediaTransportControlsSessionManager manager;
		try
		{
			manager = await Bounded(GlobalSystemMediaTransportControlsSessionManager.RequestAsync(), timeoutMilliseconds);
		}
		catch (Exception)
		{
			// The media session service is not part of this Windows installation or this session, or it did not answer in time.
			return null;
		}

		var stopwatch = Stopwatch.StartNew();
		do
		{
			foreach (var session in manager.GetSessions())
			{
				try
				{
					var properties = await Bounded(session.TryGetMediaPropertiesAsync(), 5000);
					if (properties.Title == title)
					{
						return session;
					}
				}
				catch (Exception)
				{
					// A session of another app that went away (or did not answer) while it was being read.
				}
			}

			await Task.Delay(100);
		}
		while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds);

		return null;
	}
}

static class WinMediaAutomation
{
	public static void Invoke(Microsoft.UI.Xaml.Controls.AppBarButton button)
	{
		var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(button);
		var invoker = (Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke);
		invoker.Invoke();
	}
}

/// <summary>
/// Asks the Win32 package API rather than the toolkit's AppPackageService, which is the code under test.
/// </summary>
static class WinMediaPackaging
{
	const int appModelErrorNoPackage = 15700;

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, IntPtr packageFullName);

	public static bool IsPackaged
	{
		get
		{
			uint length = 0;
			return GetCurrentPackageFullName(ref length, IntPtr.Zero) != appModelErrorNoPackage;
		}
	}

	public static string ExpectedPackageFilePath => IsPackaged ? Package.Current.InstalledPath : AppContext.BaseDirectory;
}

/// <summary>
/// MediaManager decides between "run inline" and "dispatch" with IsDispatchRequired. With <c>isDispatchRequired: true</c> the double reports
/// true on every thread, so a test can reach the dispatching arm from the UI thread, where reading the XAML player is allowed. <c>false</c> is
/// only for a fixture that opens no media: it would send the native media events (raised on other threads) down the inline arm, which reads
/// XAML objects.
/// </summary>
public sealed class WinMediaDispatcher(Microsoft.Maui.Dispatching.IDispatcher inner, bool? isDispatchRequired = null) : Microsoft.Maui.Dispatching.IDispatcher
{
	int dispatchCount;
	int playerEventDispatchCount;

	public bool IsDispatchRequired => isDispatchRequired ?? inner.IsDispatchRequired;

	/// <summary>
	/// Excludes the updates that the player's VolumeChanged and IsMutedChanged events queue (<see cref="PlayerEventDispatchCount"/>): the player
	/// raises those events on a worker thread, also for a volume or mute that the manager wrote inline or applied when it connected, so they
	/// arrive at any time (one arrived between an inline volume write and the test's count in CI run 38106699644).
	/// </summary>
	public int DispatchCount => Volatile.Read(ref dispatchCount);

	public int PlayerEventDispatchCount => Volatile.Read(ref playerEventDispatchCount);

	/// <summary>
	/// Runs on the calling thread right before an action is queued, so a test can act at the moment the manager hands work to the UI thread
	/// (for example, subscribe to the AdaptiveMediaSource the manager has just created, before the player can request anything from it).
	/// </summary>
	public Action? BeforeDispatch { get; set; }

	public bool Dispatch(Action action)
	{
		if (IsPlayerEventUpdate(action))
		{
			Interlocked.Increment(ref playerEventDispatchCount);
		}
		else
		{
			Interlocked.Increment(ref dispatchCount);
		}

		BeforeDispatch?.Invoke();
		return inner.Dispatch(action);
	}

	public bool DispatchDelayed(TimeSpan delay, Action action) => inner.DispatchDelayed(delay, action);

	public Microsoft.Maui.Dispatching.IDispatcherTimer CreateTimer() => inner.CreateTimer();

	// The compiler names a lambda after the method that contains it (<OnMediaElementVolumeChanged>b__…), and MediaManager's handlers of the
	// player's VolumeChanged and IsMutedChanged events each queue one. MediaManagerPropertyWindowsTests.PlayerVolumeAndMuteEventsAreCountedApart
	// fails if these names stop matching.
	static bool IsPlayerEventUpdate(Action action) =>
		action.Method.Name.Contains("OnMediaElementVolumeChanged", StringComparison.Ordinal)
		|| action.Method.Name.Contains("OnMediaElementIsMutedChanged", StringComparison.Ordinal);
}

/// <summary>
/// Keeps what is queued instead of running it, so that a test decides when the queued work runs (for example after the platform view was
/// disposed) and runs it inside Record.Exception: an exception thrown by work on the real dispatcher queue ends the app
/// (microsoft-ui-xaml#8940: not even Application.UnhandledException can catch it).
/// </summary>
public sealed class WinMediaCapturingDispatcher(bool isDispatchRequired = false) : Microsoft.Maui.Dispatching.IDispatcher
{
	readonly System.Collections.Concurrent.ConcurrentQueue<Action> queued = new();

	public bool IsDispatchRequired => isDispatchRequired;

	public IReadOnlyList<Action> TakeQueued()
	{
		var taken = new List<Action>();
		while (queued.TryDequeue(out var action))
		{
			taken.Add(action);
		}

		return taken;
	}

	public bool Dispatch(Action action)
	{
		queued.Enqueue(action);
		return true;
	}

	public bool DispatchDelayed(TimeSpan delay, Action action) => Dispatch(action);

	public Microsoft.Maui.Dispatching.IDispatcherTimer CreateTimer() =>
		throw new NotSupportedException("WinMediaCapturingDispatcher has no timer: the code under test was expected to queue work only.");
}

/// <summary>
/// Create it after the manager created the platform view: the manager subscribed first, so its handler for an event has run when this one
/// counts it. A test that applies a source the platform can open, and does not wait for the open itself, awaits
/// <see cref="UntilOpensEndedAsync"/> before it ends. Otherwise the manager's MediaOpened handler, which runs on a worker thread, can queue
/// SetDuration behind the test's cleanup; the cleanup disposes the player, and the queued read of the closed player then throws on the UI
/// thread, which ends the app and with it every later test of the launch.
/// </summary>
public sealed class WinMediaOpenWatcher
{
	int endedOpens;

	public WinMediaOpenWatcher(Windows.Media.Playback.MediaPlayer player)
	{
		player.MediaOpened += (_, _) => Interlocked.Increment(ref endedOpens);
		player.MediaFailed += (_, _) => Interlocked.Increment(ref endedOpens);
	}

	public int EndedOpens => Volatile.Read(ref endedOpens);

	public async Task UntilOpensEndedAsync(int count, int timeoutMilliseconds = 20000)
	{
		await AssertionExtensions.AssertEventually(() => EndedOpens >= count, timeoutMilliseconds, 50,
			$"The native player did not finish opening its source (MediaOpened or MediaFailed): {EndedOpens} of {count} opens ended.");

		// Completes only after the UI-thread work queued so far has run.
		await CommunityToolkit.Maui.DeviceTests.Runners.TestDispatcher.Current.DispatchAsync(static () => { });
	}
}

/// <summary>
/// A closed (disposed) MediaSource reports MediaSourceState.Closed or answers with an error; other closed WinRT objects answer every call with
/// an error (RO_E_CLOSED). Whether this machine makes closing observable is checked once by the WinMediaProbe.Disposed*NotObservableReason
/// probes, with a live object as the control, before a test relies on it.
/// </summary>
static class WinMediaClosed
{
	public static bool IsClosed(Func<object?> read)
	{
		try
		{
			_ = read();
			return false;
		}
		catch (Exception)
		{
			return true;
		}
	}

	public static bool IsClosed(WinMediaSource source)
	{
		try
		{
			return source.State is Windows.Media.Core.MediaSourceState.Closed;
		}
		catch (Exception)
		{
			return true;
		}
	}
}

/// <summary>
/// Windows.System.Display.DisplayRequest has no query API: RequestRelease throws when the object has no active request (Windows Developer
/// Blog, 2016-05-24, "How to prevent screen locks in your UWP apps"), so the active requests can be counted by releasing them one at a time
/// until the platform refuses and then activating as many again. WinMediaProbe.DisplayRequestCountingUnavailableReason checks both halves of
/// that premise on a plain DisplayRequest first. Call it on the thread that created the request (DisplayRequest is not agile).
/// </summary>
static class WinMediaDisplayRequests
{
	const int maximumCount = 16;

	public static int CountActive(DisplayRequest request)
	{
		var count = 0;
		while (true)
		{
			try
			{
				request.RequestRelease();
			}
			catch (Exception)
			{
				break;
			}

			if (++count == maximumCount)
			{
				throw new InvalidOperationException($"DisplayRequest.RequestRelease succeeded {maximumCount} times in a row, so the active requests cannot be counted.");
			}
		}

		for (var i = 0; i < count; i++)
		{
			request.RequestActive();
		}

		return count;
	}
}

sealed class WinMediaNonSeekableStream(byte[] content) : Stream
{
	readonly MemoryStream inner = new(content, writable: false);

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public override void Flush()
	{
	}

	public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			inner.Dispose();
		}

		base.Dispose(disposing);
	}
}

/// <summary>
/// Used where the Windows platform exposes no observable for what the toolkit decided, or where a private handler cannot be reached
/// otherwise. A renamed member fails the test loudly (MissingMemberException) instead of silently passing.
/// </summary>
static class WinMediaReflection
{
	const BindingFlags allMembers = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	public static T GetField<T>(object target, string name)
	{
		var field = FindField(target.GetType(), name) ?? throw new MissingFieldException(target.GetType().FullName, name);
		return (T)field.GetValue(target)!;
	}

	public static T GetProperty<T>(object target, string name)
	{
		for (var current = target.GetType(); current is not null; current = current.BaseType)
		{
			var property = current.GetProperty(name, allMembers | BindingFlags.DeclaredOnly);
			if (property is not null)
			{
				return (T)property.GetValue(target)!;
			}
		}

		throw new MissingMemberException(target.GetType().FullName, name);
	}

	public static void SetField(object target, string name, object? value)
	{
		var field = FindField(target.GetType(), name) ?? throw new MissingFieldException(target.GetType().FullName, name);
		field.SetValue(target, value);
	}

	public static object? Invoke(object target, string name, params object?[] arguments) =>
		InvokeCore(target.GetType(), target, name, arguments);

	public static object? InvokeStatic(Type type, string name, params object?[] arguments) =>
		InvokeCore(type, null, name, arguments);

	static object? InvokeCore(Type type, object? target, string name, object?[] arguments)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			var method = current.GetMethod(name, allMembers | BindingFlags.DeclaredOnly);
			if (method is null)
			{
				continue;
			}

			try
			{
				return method.Invoke(target, arguments);
			}
			catch (TargetInvocationException ex) when (ex.InnerException is not null)
			{
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
		}

		throw new MissingMethodException(type.FullName, name);
	}

	static FieldInfo? FindField(Type type, string name)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			var field = current.GetField(name, allMembers | BindingFlags.DeclaredOnly);
			if (field is not null)
			{
				return field;
			}
		}

		return null;
	}
}

/// <summary>
/// An async void method posts its exception to the SynchronizationContext that was current when it started; on the UI thread that would
/// crash the test app, so tests call such methods inside this scope.
/// Only valid for async void methods that finish synchronously (no await that really yields) or throw before their first await.
/// </summary>
sealed class WinMediaAsyncVoidCapture : SynchronizationContext, IDisposable
{
	readonly SynchronizationContext? previous = Current;

	public WinMediaAsyncVoidCapture() => SetSynchronizationContext(this);

	public List<Exception> Exceptions { get; } = [];

	public override void Post(SendOrPostCallback d, object? state)
	{
		try
		{
			d(state);
		}
		catch (Exception ex)
		{
			Exceptions.Add(ex);
		}
	}

	public override void Send(SendOrPostCallback d, object? state) => Post(d, state);

	public void Dispose() => SetSynchronizationContext(previous);
}

/// <summary>The toolkit reports adaptive-streaming download failures through Trace.WriteLine.</summary>
sealed class WinMediaTraceListener : TraceListener
{
	readonly Lock gate = new();
	readonly List<string> messages = [];

	public IReadOnlyList<string> Messages
	{
		get
		{
			lock (gate)
			{
				return [.. messages];
			}
		}
	}

	public override void Write(string? message) => Add(message);

	public override void WriteLine(string? message) => Add(message);

	void Add(string? message)
	{
		if (message is null)
		{
			return;
		}

		lock (gate)
		{
			messages.Add(message);
		}
	}
}

/// <summary>
/// Constructing an Application replaces <see cref="Application.Current"/>, so create the scope first and the fake application after it.
/// </summary>
sealed class WinMediaApplicationScope : IDisposable
{
	readonly Application? previous = Application.Current;

	public void Dispose() => Application.Current = previous;
}

/// <summary>
/// An Application without a platform handler: nothing attaches a platform window to its window, so the window's Handler stays null.
/// </summary>
sealed class WinMediaFakeApplication : Application
{
	readonly Func<Window>? createWindow;

	public WinMediaFakeApplication(Func<Window>? createWindow = null) => this.createWindow = createWindow;

	public Window AddWindow() => (Window)((IApplication)this).CreateWindow(null);

	protected override Window CreateWindow(IActivationState? activationState) =>
		createWindow?.Invoke() ?? throw new InvalidOperationException("This fake application was created without a window factory.");
}

public sealed class WinMediaLoggerFactory : ILoggerFactory
{
	readonly Lock gate = new();
	readonly List<(LogLevel Level, string Message, Exception? Exception)> entries = [];

	public IReadOnlyList<(LogLevel Level, string Message, Exception? Exception)> Entries
	{
		get
		{
			lock (gate)
			{
				return [.. entries];
			}
		}
	}

	public void AddProvider(ILoggerProvider provider)
	{
	}

	public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

	public void Dispose()
	{
	}

	sealed class CapturingLogger(WinMediaLoggerFactory owner) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			lock (owner.gate)
			{
				owner.entries.Add((logLevel, formatter(state, exception), exception));
			}
		}
	}
}