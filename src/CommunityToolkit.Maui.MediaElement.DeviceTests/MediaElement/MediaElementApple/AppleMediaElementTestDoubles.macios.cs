using System.Diagnostics.CodeAnalysis;
using AVFoundation;
using AVKit;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using CoreMedia;
using Foundation;
using MediaPlayer;
using Microsoft.Extensions.Logging;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// Test doubles for the iOS / Mac Catalyst MediaElement tests. They only open up protected members of the toolkit's classes
// (MediaManager's player, observers and Dispose(bool)) and replace native objects whose state cannot be produced on demand
// (AVPlayer.Status == Failed, a live stream's indefinite duration). Everything else runs against real AVFoundation.

/// <summary>A <see cref="MediaManager"/> that exposes its protected state.</summary>
sealed class AppleMediaManager(Microsoft.Maui.IMauiContext context, IMediaElement mediaElement, Microsoft.Maui.Dispatching.IDispatcher dispatcher)
	: MediaManager(context, mediaElement, dispatcher)
{
	public AVPlayer? NativePlayer
	{
		get => Player;
		set => Player = value;
	}

	public AVPlayerViewController? NativePlayerViewController
	{
		get => PlayerViewController;
		set => PlayerViewController = value;
	}

	public AVPlayerItem? NativePlayerItem
	{
		get => PlayerItem;
		set => PlayerItem = value;
	}

	public NSKeyValueObservingOptions NativeValueObserverOptions => ValueObserverOptions;
	public IDisposable? NativeCurrentItemErrorObserver => CurrentItemErrorObserver;
	public NSObject? NativeErrorObserver => ErrorObserver;
	public NSObject? NativeItemFailedToPlayToEndTimeObserver => ItemFailedToPlayToEndTimeObserver;
	public NSObject? NativePlaybackStalledObserver => PlaybackStalledObserver;
	public NSObject? NativePlayedToEndObserver => PlayedToEndObserver;
	public IDisposable? NativeRateObserver => RateObserver;
	public IDisposable? NativeStatusObserver => StatusObserver;
	public IDisposable? NativeTimeControlStatusObserver => TimeControlStatusObserver;
	public IDisposable? NativeVolumeObserver => VolumeObserver;
	public IDisposable? NativeMutedObserver => MutedObserver;

	public void DisposeCore(bool disposing) => Dispose(disposing);

	readonly List<Task> sourceUpdates = [];

	/// <summary>
	/// Completes when every <see cref="MediaManager.UpdateSource"/> call since the last call of this method has finished (the
	/// handler fires them without awaiting), and faults with their exceptions.
	/// </summary>
	public Task TakeSourceUpdates()
	{
		lock (sourceUpdates)
		{
			var pending = Task.WhenAll(sourceUpdates);
			sourceUpdates.Clear();
			return pending;
		}
	}

	/// <summary>
	/// When set, the next source update first waits for this task, then runs: an update that continues after an await, as the updates of Android and
	/// Windows do (since #3281 the Apple update is synchronous).
	/// </summary>
	public Task? DelayNextSourceUpdate { get; set; }

	protected override ValueTask PlatformUpdateSource()
	{
		if (DelayNextSourceUpdate is Task delay)
		{
			DelayNextSourceUpdate = null;
			var delayed = UpdateSourceAfter(delay);
			lock (sourceUpdates)
			{
				sourceUpdates.Add(delayed);
			}

			return new ValueTask(delayed);
		}

		Task task;
		try
		{
			task = base.PlatformUpdateSource().AsTask();
		}
		catch (Exception exception)
		{
			// Since #3281 PlatformUpdateSource is synchronous, so its exceptions reach the caller of UpdateSource (MapSource and the Source
			// setter) instead of its task. They are recorded as well, and still thrown as the product throws them.
			lock (sourceUpdates)
			{
				sourceUpdates.Add(Task.FromException(exception));
			}

			throw;
		}

		lock (sourceUpdates)
		{
			sourceUpdates.Add(task);
		}

		return new ValueTask(task);
	}

	async Task UpdateSourceAfter(Task delay)
	{
		await delay;
		await base.PlatformUpdateSource();
	}

	/// <summary>The cancellation source of the observed item's status handler (#3281), or null.</summary>
	public CancellationTokenSource? NativeItemCancellationTokenSource =>
		(CancellationTokenSource?)typeof(MediaManager).GetField("playerItemCancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(this);

	/// <summary>
	/// Runs the handler that the observer of the item's status calls (#3281: PlayerItemStatusChanged) for <paramref name="item"/>, as if
	/// the item had reported its status, and returns its task.
	/// </summary>
	public Task RunItemStatusHandler(AVPlayerItem item, CancellationToken token)
	{
		var method = typeof(MediaManager).GetMethod("PlayerItemStatusChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			?? throw new InvalidOperationException("This MediaManager predates #3281 and has no PlayerItemStatusChanged.");
		return ((ValueTask)method.Invoke(this, [item, token])!).AsTask();
	}
}

/// <summary>A <see cref="MediaElementHandler"/> whose <see cref="MediaManager"/> is an <see cref="AppleMediaManager"/>.</summary>
class AppleMediaElementHandler : MediaElementHandler
{
	public AppleMediaElementHandler()
	{
	}

	protected AppleMediaElementHandler(Microsoft.Maui.IPropertyMapper? mapper, Microsoft.Maui.CommandMapper? commandMapper) : base(mapper, commandMapper)
	{
	}

	public AppleMediaManager? Manager => MediaManager as AppleMediaManager;

	public MediaManager? ManagerOrNull
	{
		get => MediaManager;
		set => MediaManager = value;
	}

	public MauiMediaElement CreatePlatformViewDirectly() => CreatePlatformView();

	public void DisposeCore(bool disposing) => Dispose(disposing);

	/// <summary>The public disconnect (the handler's own overload is protected).</summary>
	public void Disconnect() => ((Microsoft.Maui.IElementHandler)this).DisconnectHandler();

	protected override MauiMediaElement CreatePlatformView()
	{
		if (MediaManager is null && MauiContext is not null && Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread() is Microsoft.Maui.Dispatching.IDispatcher dispatcher)
		{
			MediaManager = new AppleMediaManager(MauiContext, VirtualView, dispatcher);
		}

		return base.CreatePlatformView();
	}
}

/// <summary>
/// An <see cref="AppleMediaElementHandler"/> that the handler factory creates with the mappers a test puts into the static properties
/// (the factory only uses the parameterless constructor). The tests that use it run one at a time (DisableParallelization) and reset them.
/// </summary>
sealed class AppleMappedMediaElementHandler : AppleMediaElementHandler
{
	public AppleMappedMediaElementHandler() : base(PropertyMapperToUse, CommandMapperToUse)
	{
	}

	public static Microsoft.Maui.IPropertyMapper? PropertyMapperToUse { get; set; }

	public static Microsoft.Maui.CommandMapper? CommandMapperToUse { get; set; }
}

/// <summary>An <see cref="AVPlayer"/> whose status values can be forced; everything else is the real player.</summary>
sealed class AppleFakeAVPlayer : AVPlayer
{
	public AVPlayerStatus? StatusOverride { get; set; }
	public AVPlayerTimeControlStatus? TimeControlStatusOverride { get; set; }

	/// <summary>When true, <see cref="CurrentItem"/> is <see cref="CurrentItemOverride"/> (which may be null) instead of the real item.</summary>
	public bool UseCurrentItemOverride { get; set; }
	public AVPlayerItem? CurrentItemOverride { get; set; }

	/// <summary>
	/// When set, a seek with a completion handler reports this result instead of seeking. Like AVFoundation, it calls the completion later from
	/// the main queue (a native callback), never from inside the Seek call, so an exception thrown by the completion is handled the way the real
	/// player's callbacks handle it instead of reaching the caller of Seek.
	/// </summary>
	public bool? SeekResult { get; set; }

	/// <summary>When true, a seek with a completion handler records the request and never reports a result (a seek that takes forever).</summary>
	public bool HoldSeekCompletion { get; set; }

	/// <summary>The times of the seeks with a completion handler, with or without tolerances.</summary>
	public List<CMTime> SeekRequests { get; } = [];

	public int PauseCount { get; private set; }
	public List<AVPlayerItem?> ReplacedItems { get; } = [];

	/// <summary>Runs (once) right after the item was replaced, like an event handler that the replacement raises synchronously.</summary>
	public Action? AfterNextReplace { get; set; }

	/// <summary>Runs right after <see cref="Play"/> started the player, like a handler of the state change that Play raises.</summary>
	public Action? AfterPlay { get; set; }

	/// <summary>Runs right after a caller set <see cref="Rate"/>, like a handler of the rate change that it raises.</summary>
	public Action? AfterRateSet { get; set; }

	public int PlayCount { get; private set; }

	/// <summary>The rates that callers set (AVPlayer's own play and pause set the rate too; those are not recorded).</summary>
	public List<float> RatesSet { get; } = [];

	bool isInPlayOrPause;

	public override AVPlayerStatus Status => StatusOverride ?? base.Status;

	public override AVPlayerTimeControlStatus TimeControlStatus => TimeControlStatusOverride ?? base.TimeControlStatus;

	public override AVPlayerItem? CurrentItem => UseCurrentItemOverride ? CurrentItemOverride : base.CurrentItem;

	public override float Rate
	{
		get => base.Rate;
		set
		{
			if (isInPlayOrPause)
			{
				base.Rate = value;
				return;
			}

			RatesSet.Add(value);
			base.Rate = value;
			AfterRateSet?.Invoke();
		}
	}

	public override void Play()
	{
		PlayCount++;
		isInPlayOrPause = true;
		try
		{
			base.Play();
		}
		finally
		{
			isInPlayOrPause = false;
		}

		AfterPlay?.Invoke();
	}

	public override void Pause()
	{
		PauseCount++;
		isInPlayOrPause = true;
		try
		{
			base.Pause();
		}
		finally
		{
			isInPlayOrPause = false;
		}
	}

	public override void ReplaceCurrentItemWithPlayerItem(AVPlayerItem? item)
	{
		ReplacedItems.Add(item);
		base.ReplaceCurrentItemWithPlayerItem(item);
		var afterReplace = AfterNextReplace;
		AfterNextReplace = null;
		afterReplace?.Invoke();
	}

	public override void Seek(CMTime time, AVCompletion completion)
	{
		if (!InterceptSeek(time, completion))
		{
			base.Seek(time, completion);
		}
	}

	public override void Seek(CMTime time, CMTime toleranceBefore, CMTime toleranceAfter, AVCompletion completion)
	{
		if (!InterceptSeek(time, completion))
		{
			base.Seek(time, toleranceBefore, toleranceAfter, completion);
		}
	}

	// Records the seek, and answers it when the test says so: true when the real player must not seek
	bool InterceptSeek(CMTime time, AVCompletion completion)
	{
		SeekRequests.Add(time);
		if (HoldSeekCompletion)
		{
			return true;
		}

		if (SeekResult is bool result)
		{
			CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() => completion(result));
			return true;
		}

		return false;
	}
}

/// <summary>An <see cref="AVPlayerItem"/> whose timing and size can be forced (a live stream has an indefinite duration).</summary>
sealed class AppleFakeAVPlayerItem(AVAsset asset) : AVPlayerItem(asset)
{
	public CMTime? DurationOverride { get; set; }
	public CMTime? CurrentTimeOverride { get; set; }
	public NSValue[]? SeekableTimeRangesOverride { get; set; }
	public CoreGraphics.CGSize? PresentationSizeOverride { get; set; }
	public NSError? ErrorOverride { get; set; }

	public AVPlayerItemStatus? StatusOverride { get; set; }

	/// <summary>Statuses that the next reads of <see cref="Status"/> return, one per read, before it falls back to the usual value (an item whose status changes between two reads).</summary>
	public Queue<AVPlayerItemStatus>? StatusReads { get; set; }

	public override CMTime Duration => DurationOverride ?? base.Duration;

	public override AVPlayerItemStatus Status => StatusReads is { Count: > 0 } reads ? reads.Dequeue() : StatusOverride ?? base.Status;

	public override NSError? Error => ErrorOverride ?? base.Error;

	public override CMTime CurrentTime => CurrentTimeOverride ?? base.CurrentTime;

	public override NSValue[] SeekableTimeRanges => SeekableTimeRangesOverride ?? base.SeekableTimeRanges;

	public override CoreGraphics.CGSize PresentationSize => PresentationSizeOverride ?? base.PresentationSize;
}

/// <summary>
/// An <see cref="AVUrlAsset"/> whose video tracks load only when the test says so (<see cref="CompleteTrackLoad"/>,
/// <see cref="FailTrackLoad"/>), or never: MediaManager loads them through the managed LoadTracksWithMediaTypeAsync wrapper.
/// </summary>
sealed class AppleTrackLoadingAsset(NSUrl url) : AVUrlAsset(url)
{
	readonly TaskCompletionSource<NSArray<AVFoundation.AVMutableCompositionTrack>> tracks = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public int TrackLoads { get; private set; }

	/// <summary>Runs inside the track load call, before it returns (like an event that the load raises synchronously).</summary>
	public Action? DuringTrackLoad { get; set; }

#pragma warning disable CS0618, CS0672 // The product calls this obsolete string overload.
	public override Task<NSArray<AVFoundation.AVMutableCompositionTrack>> LoadTracksWithMediaTypeAsync(string mediaType)
#pragma warning restore CS0618, CS0672
	{
		TrackLoads++;
		DuringTrackLoad?.Invoke();
		return tracks.Task;
	}

	/// <summary>Completes the track load without a video track.</summary>
	public void CompleteTrackLoad() => tracks.TrySetResult(new NSArray<AVFoundation.AVMutableCompositionTrack>());

	public void FailTrackLoad(NSError error) => tracks.TrySetException(new NSErrorException(error));
}

/// <summary>
/// An <see cref="AVPlayerViewController"/> whose <c>View</c> is nil. Its content overlay view is a plain view: AVKit's own code asks
/// the controller for the overlay while it loads the (never loading) view, which recurses until the stack is exhausted on Mac Catalyst.
/// </summary>
sealed class AppleViewlessPlayerViewController : AVPlayerViewController
{
	UIView? contentOverlayView;

	[AllowNull]
	public override UIView View
	{
		get => null!;
		set { }
	}

	public override UIView? ContentOverlayView => contentOverlayView ??= new UIView();
}

/// <summary>
/// An <see cref="AVPlayerViewController"/> that records every parent UIKit reports to <c>DidMoveToParentViewController</c>: the parent it
/// was added to, or null when it was removed from one.
/// </summary>
sealed class AppleParentTrackingPlayerViewController : AVPlayerViewController
{
	public List<UIViewController?> MovedToParents { get; } = [];

	public override void DidMoveToParentViewController(UIViewController? parent)
	{
		base.DidMoveToParentViewController(parent);
		MovedToParents.Add(parent);
	}
}

/// <summary>An <see cref="AVPlayerViewController"/> whose <c>ContentOverlayView</c> is nil.</summary>
sealed class AppleOverlaylessPlayerViewController : AVPlayerViewController
{
	public override UIView? ContentOverlayView => null;
}

/// <summary>A remote-command event that carries a playback position (the lock screen's scrubber).</summary>
sealed class AppleChangePlaybackPositionEvent : MPChangePlaybackPositionCommandEvent
{
	readonly double positionTime;

	public AppleChangePlaybackPositionEvent(double positionTime) : base(NSObjectFlag.Empty)
	{
		this.positionTime = positionTime;
	}

	public override double PositionTime => positionTime;
}

/// <summary>A remote-command event of the base type (what play/pause/seek commands deliver).</summary>
sealed class AppleRemoteCommandEvent : MPRemoteCommandEvent
{
	public AppleRemoteCommandEvent() : base(NSObjectFlag.Empty)
	{
	}
}

/// <summary>Collects what the toolkit logs, so tests can assert on the warnings and errors it reports.</summary>
sealed class AppleCapturingLoggerFactory : ILoggerFactory
{
	readonly List<(LogLevel Level, string Message, Exception? Exception)> entries = [];

	public IReadOnlyList<(LogLevel Level, string Message, Exception? Exception)> Entries
	{
		get
		{
			lock (entries)
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

	sealed class CapturingLogger(AppleCapturingLoggerFactory owner) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			lock (owner.entries)
			{
				owner.entries.Add((logLevel, formatter(state, exception), exception));
			}
		}
	}
}

/// <summary>
/// A stream that records how the resource loader uses it and can be made non-seekable, short-reading or failing.
/// </summary>
sealed class AppleRecordingStream(Stream inner, bool canSeek = true) : Stream
{
	public List<string> Calls { get; } = [];
	public int MaxBytesPerRead { get; set; } = int.MaxValue;
	public bool ThrowOnRead { get; set; }
	public bool InnerWasDisposed { get; private set; }

	public override bool CanRead => inner.CanRead;
	public override bool CanSeek => canSeek && inner.CanSeek;
	public override bool CanWrite => false;
	public override long Length => CanSeek ? inner.Length : throw new NotSupportedException();

	public override long Position
	{
		get => inner.Position;
		set
		{
			Calls.Add($"Position={value}");
			inner.Position = value;
		}
	}

	public override void Flush()
	{
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Calls.Add($"Read@{inner.Position}x{count}");
		if (ThrowOnRead)
		{
			throw new IOException("The stream failed on purpose.");
		}

		return inner.Read(buffer, offset, Math.Min(count, MaxBytesPerRead));
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		Calls.Add($"Seek({offset},{origin})");
		return inner.Seek(offset, origin);
	}

	public override void SetLength(long value) => throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			InnerWasDisposed = true;
			inner.Dispose();
		}

		base.Dispose(disposing);
	}
}

/// <summary>A view handler that only hands out a platform view (a stand-in for a parent's real handler).</summary>
sealed class AppleStubElementHandler(object platformView) : Microsoft.Maui.IViewHandler
{
	public object PlatformView { get; } = platformView;

	public Microsoft.Maui.IElement? VirtualView { get; private set; }

	Microsoft.Maui.IView? Microsoft.Maui.IViewHandler.VirtualView => VirtualView as Microsoft.Maui.IView;

	public Microsoft.Maui.IMauiContext? MauiContext { get; private set; }

	public bool HasContainer { get; set; }

	public object? ContainerView => null;

	public void SetMauiContext(Microsoft.Maui.IMauiContext mauiContext) => MauiContext = mauiContext;

	public void SetVirtualView(Microsoft.Maui.IElement view) => VirtualView = view;

	public void UpdateValue(string property)
	{
	}

	public void Invoke(string command, object? args = null)
	{
	}

	public void DisconnectHandler() => VirtualView = null;

	public Microsoft.Maui.Graphics.Size GetDesiredSize(double widthConstraint, double heightConstraint) => Microsoft.Maui.Graphics.Size.Zero;

	public void PlatformArrange(Microsoft.Maui.Graphics.Rect frame)
	{
	}
}

/// <summary>A view whose safe area insets are set by the test, so inset arithmetic can be asserted.</summary>
sealed class AppleInsetView(UIEdgeInsets insets) : UIView
{
	public UIEdgeInsets Insets { get; set; } = insets;

	public override UIEdgeInsets SafeAreaInsets => Insets;
}

/// <summary>A view controller whose view reports fixed safe area insets.</summary>
sealed class AppleInsetViewController(UIEdgeInsets insets) : UIViewController
{
	public override void LoadView() => View = new AppleInsetView(insets);
}

/// <summary>A media source type the platform manager does not know.</summary>
sealed class AppleUnknownMediaSource : MediaSource
{
}

/// <summary>
/// A synchronization context that records the exception an <c>async void</c> method posts to it (instead of the exception crashing the
/// app), so a test can assert what an <c>async void</c> handler throws.
/// </summary>
sealed class AppleCapturingSynchronizationContext : SynchronizationContext
{
	public Exception? Captured { get; private set; }

	public override void Post(SendOrPostCallback d, object? state)
	{
		try
		{
			d(state);
		}
		catch (Exception exception)
		{
			Captured = exception;
		}
	}

	/// <summary>Runs <paramref name="action"/> with this context as the current one and restores the previous context.</summary>
	public void Run(Action action)
	{
		var previous = Current;
		SetSynchronizationContext(this);
		try
		{
			action();
		}
		finally
		{
			SetSynchronizationContext(previous);
		}
	}
}

/// <summary>A content-information request that records what the resource loader sets (AVFoundation creates the real ones).</summary>
sealed class AppleFakeContentInformationRequest : AVAssetResourceLoadingContentInformationRequest
{
	string? contentType;
	long contentLength = -1;
	bool byteRangeAccessSupported;

	public AppleFakeContentInformationRequest() : base(NSObjectFlag.Empty)
	{
	}

	public override string? ContentType
	{
		get => contentType;
		set => contentType = value;
	}

	public override long ContentLength
	{
		get => contentLength;
		set => contentLength = value;
	}

	public override bool ByteRangeAccessSupported
	{
		get => byteRangeAccessSupported;
		set => byteRangeAccessSupported = value;
	}
}

/// <summary>A data request that records the responses of the resource loader.</summary>
sealed class AppleFakeDataRequest(long requestedOffset, nint requestedLength, long currentOffset = 0) : AVAssetResourceLoadingDataRequest(NSObjectFlag.Empty)
{
	public List<byte[]> Responses { get; } = [];

	/// <summary>The data objects the resource loader responded with (to check that it released them).</summary>
	public List<NSData> ResponseData { get; } = [];

	public override long RequestedOffset => requestedOffset;

	public override nint RequestedLength => requestedLength;

	public override long CurrentOffset => currentOffset;

	public override void Respond(NSData responseData)
	{
		ResponseData.Add(responseData);
		Responses.Add(responseData.ToArray());
	}
}

/// <summary>A loading request with the requests a test wants the resource loader to see.</summary>
sealed class AppleFakeLoadingRequest(AVAssetResourceLoadingContentInformationRequest? contentInformationRequest, AVAssetResourceLoadingDataRequest? dataRequest)
	: AVAssetResourceLoadingRequest(NSObjectFlag.Empty)
{
	/// <summary>How often the request was finished successfully.</summary>
	public int FinishCount { get; private set; }

	/// <summary>How often the request was finished with an error.</summary>
	public int FinishWithErrorCount { get; private set; }

	/// <summary>How often the request was finished, successfully or with an error.</summary>
	public int TotalFinishCount => FinishCount + FinishWithErrorCount;

	public override AVAssetResourceLoadingContentInformationRequest? ContentInformationRequest => contentInformationRequest;

	public override AVAssetResourceLoadingDataRequest? DataRequest => dataRequest;

	public override void FinishLoading() => FinishCount++;

	public override void FinishLoadingWithError(NSError? error) => FinishWithErrorCount++;
}