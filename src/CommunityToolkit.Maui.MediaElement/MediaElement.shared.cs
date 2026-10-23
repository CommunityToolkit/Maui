using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents an object used to render audio and video to the display.
/// </summary>
public partial class MediaElement : View, IMediaElement, IDisposable
{
	/// <summary>
	/// Bindable property for the <see cref="Aspect"/> property.
	/// </summary>
	public static readonly BindableProperty AspectProperty =
		BindableProperty.Create(nameof(Aspect), typeof(Aspect), typeof(MediaElement), MediaElementDefaults.Aspect);


	static readonly BindablePropertyKey currentStatePropertyKey = BindableProperty.CreateReadOnly(nameof(CurrentState), typeof(MediaElementState), typeof(MediaElement),
			MediaElementState.None, propertyChanged: OnCurrentStatePropertyChanged);

	/// <summary>
	/// Bindable property for the <see cref="CurrentState"/> property.
	/// </summary>
	public static readonly BindableProperty CurrentStateProperty = currentStatePropertyKey.BindableProperty;

	static readonly BindablePropertyKey durationPropertyKey =
		BindableProperty.CreateReadOnly(nameof(Duration), typeof(TimeSpan), typeof(MediaElement), MediaElementDefaults.Duration);

	/// <summary>
	/// Bindable property for the <see cref="Duration"/> property.
	/// </summary>
	public static readonly BindableProperty DurationProperty = durationPropertyKey.BindableProperty;

	/// <summary>
	/// Bindable property for the <see cref="ShouldAutoPlay"/> property.
	/// </summary>
	public static readonly BindableProperty ShouldAutoPlayProperty =
		BindableProperty.Create(nameof(ShouldAutoPlay), typeof(bool), typeof(MediaElement), MediaElementDefaults.ShouldAutoPlay);

	/// <summary>
	/// Bindable property for the <see cref="ShouldLoopPlayback"/> property.
	/// </summary>
	public static readonly BindableProperty ShouldLoopPlaybackProperty =
		BindableProperty.Create(nameof(ShouldLoopPlayback), typeof(bool), typeof(MediaElement), MediaElementDefaults.ShouldLoopPlayback);

	/// <summary>
	/// Bindable property for the <see cref="ShouldKeepScreenOn"/> property.
	/// </summary>
	public static readonly BindableProperty ShouldKeepScreenOnProperty =
		BindableProperty.Create(nameof(ShouldKeepScreenOn), typeof(bool), typeof(MediaElement), MediaElementDefaults.ShouldKeepScreenOn);

	/// <summary>
	/// Bindable property for the <see cref="ShouldMute"/> property.
	/// </summary>
	public static readonly BindableProperty ShouldMuteProperty =
		BindableProperty.Create(nameof(ShouldMute), typeof(bool), typeof(MediaElement), MediaElementDefaults.ShouldMute);


	static readonly BindablePropertyKey positionPropertyKey = BindableProperty.CreateReadOnly(nameof(Position), typeof(TimeSpan), typeof(MediaElement), TimeSpan.Zero);

	/// <summary>
	/// Bindable property for the <see cref="Position"/> property.
	/// </summary>
	public static readonly BindableProperty PositionProperty = positionPropertyKey.BindableProperty;

	/// <summary>
	/// Bindable property for the <see cref="ShouldShowPlaybackControls"/> property.
	/// </summary>
	public static readonly BindableProperty ShouldShowPlaybackControlsProperty =
		BindableProperty.Create(nameof(ShouldShowPlaybackControls), typeof(bool), typeof(MediaElement), MediaElementDefaults.ShouldShowPlaybackControls);

	/// <summary>
	/// Bindable property for the <see cref="Source"/> property.
	/// </summary>
	public static readonly BindableProperty SourceProperty =
		BindableProperty.Create(nameof(Source), typeof(MediaSource), typeof(MediaElement),
			propertyChanging: OnSourcePropertyChanging, propertyChanged: OnSourcePropertyChanged);

	/// <summary>
	/// Bindable property for the <see cref="Speed"/> property.
	/// </summary>
	public static readonly BindableProperty SpeedProperty =
		BindableProperty.Create(nameof(Speed), typeof(double), typeof(MediaElement), MediaElementDefaults.Speed);

	static readonly BindablePropertyKey mediaHeightPropertyKey =
		BindableProperty.CreateReadOnly(nameof(MediaHeight), typeof(int), typeof(MediaElement), MediaElementDefaults.MediaHeight);

	/// <summary>
	/// Bindable property for the <see cref="MediaHeight"/> property.
	/// </summary>
	public static readonly BindableProperty MediaHeightProperty =
		mediaHeightPropertyKey.BindableProperty;

	static readonly BindablePropertyKey mediaWidthPropertyKey =
		BindableProperty.CreateReadOnly(nameof(MediaWidth), typeof(int), typeof(MediaElement), MediaElementDefaults.MediaWidth);

	/// <summary>
	/// Bindable property for the <see cref="MediaWidth"/> property.
	/// </summary>
	public static readonly BindableProperty MediaWidthProperty = mediaWidthPropertyKey.BindableProperty;

	/// <summary>
	/// Bindable property for the <see cref="Volume"/> property.
	/// </summary>
	public static readonly BindableProperty VolumeProperty =
		BindableProperty.Create(nameof(Volume), typeof(double), typeof(MediaElement), MediaElementDefaults.Volume, BindingMode.TwoWay);

	/// <summary>
	/// Bindable property for the <see cref="MetadataTitle"/> property.
	/// </summary>
	public static readonly BindableProperty MetadataTitleProperty = BindableProperty.Create(nameof(MetadataTitle), typeof(string), typeof(MediaElement), MediaElementDefaults.MetadataTitle);

	/// <summary>
	/// Bindable property for the <see cref="MetadataArtist"/> property.
	/// </summary>
	public static readonly BindableProperty MetadataArtistProperty = BindableProperty.Create(nameof(MetadataArtist), typeof(string), typeof(MediaElement), MediaElementDefaults.MetadataArtist);

	/// <summary>
	/// Bindable property for the <see cref="MetadataArtworkUrl"/> property.
	/// </summary>
	public static readonly BindableProperty MetadataArtworkUrlProperty = BindableProperty.Create(nameof(MetadataArtworkUrl), typeof(string), typeof(MediaElement), MediaElementDefaults.MetadataArtworkUrl);

	// An AsyncLocal follows the source update through its awaits (Android and Windows), and does not reach what runs outside of it: the events that
	// the player raises on its own and those of the app's own calls.
	static readonly AsyncLocal<SourceUpdate?> currentSourceUpdate = new();

	readonly WeakEventManager eventManager = new();
	readonly SemaphoreSlim seekToSemaphoreSlim = new(1, 1);

	bool isDisposed;
	IDispatcherTimer? timer;
	TaskCompletionSource seekCompletedTaskCompletionSource = new();

	/// <summary>
	/// Finalizer
	/// </summary>
	~MediaElement() => Dispose(false);

	/// <inheritdoc cref="IMediaElement.MediaEnded"/>
	public event EventHandler MediaEnded
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaElement.MediaFailed(MediaFailedEventArgs)"/>
	public event EventHandler<MediaFailedEventArgs> MediaFailed
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaElement.MediaOpened"/>
	public event EventHandler MediaOpened
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaElement.SeekCompleted"/>
	public event EventHandler SeekCompleted
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaElement.StateChanged"/>
	public event EventHandler<MediaStateChangedEventArgs> StateChanged
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaElement.PositionChanged"/>
	public event EventHandler<MediaPositionChangedEventArgs> PositionChanged
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	internal event EventHandler StatusUpdated
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	internal event EventHandler PlayRequested
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	internal event EventHandler PauseRequested
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	internal event EventHandler PositionRequested
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	internal event EventHandler<MediaSeekRequestedEventArgs> SeekRequested
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	internal event EventHandler StopRequested
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Gets the <see cref="MediaHeight"/> in pixels.
	/// </summary>
	public int MediaHeight => (int)GetValue(MediaHeightProperty);

	/// <summary>
	/// Gets the <see cref="MediaWidth"/> in pixels.
	/// </summary>
	public int MediaWidth => (int)GetValue(MediaWidthProperty);

	/// <summary>
	/// Gets the current <see cref="Position"/> of the media playback.
	/// </summary>
	public TimeSpan Position => (TimeSpan)GetValue(PositionProperty);

	/// <summary>
	/// Gets the <see cref="Duration"/> of the media.
	/// </summary>
	public TimeSpan Duration => (TimeSpan)GetValue(DurationProperty);

	/// <summary>
	/// Gets or sets the <see cref="AndroidViewType"/> of the media.
	/// </summary>
	public AndroidViewType AndroidViewType { get; init; } = MediaElementOptions.DefaultAndroidViewType;

	/// <summary>
	/// Gets or sets a value indicating whether Android Foreground Service is enabled.
	/// </summary>
	public bool IsAndroidForegroundServiceEnabled { get; init; } = MediaElementOptions.IsAndroidForegroundServiceEnabled;

	/// <summary>
	/// Gets or sets the <see cref="Aspect"/> ratio used to display the video content.
	/// </summary>
	public Aspect Aspect
	{
		get => (Aspect)GetValue(AspectProperty);
		set => SetValue(AspectProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ShouldAutoPlay"/> indicating whether the media should play automatically.
	/// </summary>
	public bool ShouldAutoPlay
	{
		get => (bool)GetValue(ShouldAutoPlayProperty);
		set => SetValue(ShouldAutoPlayProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ShouldLoopPlayback"/> indicating whether the media should loop playback.
	/// </summary>
	public bool ShouldLoopPlayback
	{
		get => (bool)GetValue(ShouldLoopPlaybackProperty);
		set => SetValue(ShouldLoopPlaybackProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ShouldKeepScreenOn"/> indicating whether the screen should be kept on during media playback.
	/// </summary>
	public bool ShouldKeepScreenOn
	{
		get => (bool)GetValue(ShouldKeepScreenOnProperty);
		set => SetValue(ShouldKeepScreenOnProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ShouldMute"/> indicating whether the media should be muted.
	/// </summary>
	public bool ShouldMute
	{
		get => (bool)GetValue(ShouldMuteProperty);
		set => SetValue(ShouldMuteProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ShouldShowPlaybackControls"/> indicating whether playback controls should be shown.
	/// </summary>
	public bool ShouldShowPlaybackControls
	{
		get => (bool)GetValue(ShouldShowPlaybackControlsProperty);
		set => SetValue(ShouldShowPlaybackControlsProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="Source"/> of the media.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A source without data (a <see cref="UriMediaSource"/> without <see cref="UriMediaSource.Uri"/>, a <see cref="FileMediaSource"/> or
	/// <see cref="ResourceMediaSource"/> without a path, a <see cref="StreamMediaSource"/> without a stream) empties the player and reports
	/// <see cref="MediaElementState.None"/>, like a <see langword="null"/> source. On iOS and Mac Catalyst, a <see cref="ResourceMediaSource"/> whose
	/// file is not in the app bundle, or whose path has no extension, is handled the same way, and a warning is logged. A kind of
	/// <see cref="MediaSource"/> that the platform does not support is reported through <see cref="MediaFailed"/>.
	/// </para>
	/// <para>
	/// A source that cannot be applied or played is reported through <see cref="MediaFailed"/>. The change of the source raises events while it
	/// applies the source: <see cref="StateChanged"/> (for example for <see cref="MediaElementState.Opening"/>), PropertyChanged (for example of
	/// <see cref="Position"/> or <see cref="Duration"/>), <see cref="MediaFailed"/> for a source that cannot be applied and, on Android,
	/// <see cref="MediaOpened"/>. An exception thrown by one of their handlers is not a failure of the source, and does not reach the code that set
	/// the source, which is often MAUI's (XAML, a binding): it is caught and logged, the change goes on and applies the source, and once the change
	/// has ended the first of these exceptions is reported through <see cref="MediaFailed"/>, without setting <see cref="CurrentState"/> to
	/// <see cref="MediaElementState.Failed"/>. The events that the player raises afterwards, for example <see cref="MediaOpened"/> on iOS, Mac
	/// Catalyst and Windows, are not part of the change.
	/// </para>
	/// </remarks>
	[TypeConverter(typeof(MediaSourceConverter))]
	public MediaSource? Source
	{
		get => (MediaSource?)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="Speed"/> of the media playback.
	/// </summary>
	public double Speed
	{
		get => (double)GetValue(SpeedProperty);
		set => SetValue(SpeedProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="MetadataTitle"/> of the media.
	/// </summary>
	public string MetadataTitle
	{
		get => (string)GetValue(MetadataTitleProperty);
		set => SetValue(MetadataTitleProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="MetadataArtist"/> of the media.
	/// </summary>
	public string MetadataArtist
	{
		get => (string)GetValue(MetadataArtistProperty);
		set => SetValue(MetadataArtistProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="MetadataArtworkUrl"/> of the media.
	/// </summary>
	public string MetadataArtworkUrl
	{
		get => (string)GetValue(MetadataArtworkUrlProperty);
		set => SetValue(MetadataArtworkUrlProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="Volume"/> of the media.
	/// </summary>
	public double Volume
	{
		get => (double)GetValue(VolumeProperty);
		set
		{
			switch (value)
			{
				case > 1:
					throw new ArgumentOutOfRangeException(nameof(value), value, $"The value of {nameof(Volume)} cannot be greater than {1}");
				case < 0:
					throw new ArgumentOutOfRangeException(nameof(value), value, $"The value of {nameof(Volume)} cannot be less than {0}");
				default:
					SetValue(VolumeProperty, value);
					break;
			}
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="CurrentState"/> of the media.
	/// </summary>
	public MediaElementState CurrentState
	{
		get => (MediaElementState)GetValue(CurrentStateProperty);
		private set => SetValue(currentStatePropertyKey, value);
	}

	/// <inheritdoc/>
	TaskCompletionSource IAsynchronousMediaElementHandler.SeekCompletedTCS => seekCompletedTaskCompletionSource;

	TimeSpan IMediaElement.Position
	{
		get => (TimeSpan)GetValue(PositionProperty);
		set
		{
			var currentValue = (TimeSpan)GetValue(PositionProperty);

			if (currentValue != value)
			{
				SetValue(positionPropertyKey, value);
				OnPositionChanged(new(value));
			}
		}
	}

	TimeSpan IMediaElement.Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
		set => SetValue(durationPropertyKey, value);
	}

	int IMediaElement.MediaWidth
	{
		get => (int)GetValue(MediaWidthProperty);
		set => SetValue(mediaWidthPropertyKey, value);
	}

	int IMediaElement.MediaHeight
	{
		get => (int)GetValue(MediaHeightProperty);
		set => SetValue(mediaHeightPropertyKey, value);
	}

	SourceUpdate? RunningSourceUpdate => currentSourceUpdate.Value is SourceUpdate sourceUpdate && sourceUpdate.IsRunningFor(this) ? sourceUpdate : null;

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc cref="IMediaElement.Pause"/>
	public void Pause()
	{
		OnPauseRequested();
		Handler?.Invoke(nameof(PauseRequested));
	}

	/// <inheritdoc cref="IMediaElement.Play"/>
	public void Play()
	{
		OnPlayRequested();
		Handler?.Invoke(nameof(PlayRequested));
	}

	/// <inheritdoc cref="IMediaElement.SeekTo(TimeSpan, CancellationToken)"/>
	public async Task SeekTo(TimeSpan position, CancellationToken token = default)
	{
		await seekToSemaphoreSlim.WaitAsync(token);

		try
		{
			MediaSeekRequestedEventArgs args = new(position);
			Handler?.Invoke(nameof(SeekRequested), args);

			await seekCompletedTaskCompletionSource.Task.WaitAsync(token);
		}
		finally
		{
			// When the token ended the wait, the seek can still end or fail later, and nothing awaits its task any more: the task is cancelled, so that a
			// later failure cannot fault it (an unobserved exception, TaskScheduler.UnobservedTaskException). A failure that came first is observed here.
			if (!seekCompletedTaskCompletionSource.TrySetCanceled(token))
			{
				_ = seekCompletedTaskCompletionSource.Task.Exception;
			}

			seekCompletedTaskCompletionSource = new();
			seekToSemaphoreSlim.Release();
		}
	}

	/// <inheritdoc cref="IMediaElement.Stop"/>
	public void Stop()
	{
		OnStopRequested();
		Handler?.Invoke(nameof(StopRequested));
	}

	void IMediaElement.MediaEnded() => OnMediaEnded();

	void IMediaElement.MediaFailed(MediaFailedEventArgs args) => OnMediaFailed(args);

	void IMediaElement.MediaOpened() => OnMediaOpened();

	void IMediaElement.SeekCompleted() => OnSeekCompleted();

	void IMediaElement.CurrentStateChanged(MediaElementState newState) => CurrentState = newState;

	// The code that sets the Source is often MAUI's (XAML, a binding), so the app cannot catch what its event handlers throw during the update. The
	// exception is caught where the event is raised (RaiseEvent, OnPropertyChanged), before it can end the update or leave a property that MAUI
	// was setting unable to change again.
	async ValueTask IMediaElement.RunSourceUpdate(Func<ValueTask> updateSource, ILogger logger)
	{
		var sourceUpdate = new SourceUpdate(this, logger);

		// An async method gives its caller back the caller's AsyncLocal values, so only the update and its continuations see this one.
		currentSourceUpdate.Value = sourceUpdate;
		try
		{
			await updateSource();
		}
		finally
		{
			sourceUpdate.End();
		}
	}

	internal void OnMediaEnded()
	{
		CurrentState = MediaElementState.Stopped;
		RaiseEvent(EventArgs.Empty, nameof(MediaEnded));
	}

	internal void OnMediaFailed(MediaFailedEventArgs args)
	{
		((IMediaElement)this).Duration = ((IMediaElement)this).Position = TimeSpan.Zero;

		CurrentState = MediaElementState.Failed;
		RaiseEvent(args, nameof(MediaFailed));
	}

	internal void OnMediaOpened()
	{
		InitializeTimer();
		RaiseEvent(EventArgs.Empty, nameof(MediaOpened));
	}

	/// <inheritdoc/>
	/// <remarks>
	/// While the change of <see cref="Source"/> applies the source, an exception thrown by a PropertyChanged handler is caught (see <see cref="Source"/>).
	/// </remarks>
	protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		try
		{
			base.OnPropertyChanged(propertyName);
		}
		catch (Exception exception) when (RunningSourceUpdate is SourceUpdate sourceUpdate)
		{
			sourceUpdate.Catch(exception);
		}
	}

	/// <inheritdoc/>
	protected override void OnBindingContextChanged()
	{
		if (Source is not null)
		{
			SetInheritedBindingContext(Source, BindingContext);
		}

		base.OnBindingContextChanged();
	}

	/// <inheritdoc/>
	protected virtual void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (disposing)
		{
			ClearTimer();
			seekToSemaphoreSlim.Dispose();
		}

		isDisposed = true;
	}

	static void OnSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var mediaElement = (MediaElement)bindable;
		var source = (MediaSource?)newValue;

		mediaElement.ClearTimer();

		if (source is not null)
		{
			source.SourceChanged += mediaElement.OnSourceChanged;
			SetInheritedBindingContext(source, mediaElement.BindingContext);
		}

		mediaElement.InvalidateMeasure();
		mediaElement.InitializeTimer();
	}

	static void OnSourcePropertyChanging(BindableObject bindable, object oldValue, object newValue)
	{
		var mediaElement = (MediaElement)bindable;
		var oldMediaSource = (MediaSource?)oldValue;

		oldMediaSource?.SourceChanged -= mediaElement.OnSourceChanged;
	}

	static void OnCurrentStatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var mediaElement = (MediaElement)bindable;
		var previousState = (MediaElementState)oldValue;
		var newState = (MediaElementState)newValue;

		mediaElement.OnStateChanged(new MediaStateChangedEventArgs(previousState, newState));
	}

	void OnTimerTick(object? sender, EventArgs e)
	{
		OnPositionRequested();
		OnUpdateStatus();
		Handler?.Invoke(nameof(StatusUpdated));
	}

	void InitializeTimer()
	{
		if (timer is not null)
		{
			return;
		}

		timer = Dispatcher.CreateTimer();
		timer.Interval = TimeSpan.FromMilliseconds(200);
		timer.Tick += OnTimerTick;
		timer.Start();
	}

	void ClearTimer()
	{
		if (timer is null)
		{
			return;
		}

		timer.Tick -= OnTimerTick;
		timer.Stop();
		timer = null;
	}

	void OnSourceChanged(object? sender, EventArgs eventArgs)
	{
		OnPropertyChanged(SourceProperty.PropertyName);
		InvalidateMeasure();
	}

	void OnPositionChanged(MediaPositionChangedEventArgs mediaPositionChangedEventArgs) => RaiseEvent(mediaPositionChangedEventArgs, nameof(PositionChanged));

	void OnStateChanged(MediaStateChangedEventArgs mediaStateChangedEventArgs) => RaiseEvent(mediaStateChangedEventArgs, nameof(StateChanged));

	void OnPauseRequested() => RaiseEvent(EventArgs.Empty, nameof(PauseRequested));

	void OnPlayRequested() => RaiseEvent(EventArgs.Empty, nameof(PlayRequested));

	void OnStopRequested() => RaiseEvent(EventArgs.Empty, nameof(StopRequested));

	void OnSeekCompleted() => RaiseEvent(EventArgs.Empty, nameof(SeekCompleted));

	void OnPositionRequested() => RaiseEvent(EventArgs.Empty, nameof(PositionRequested));

	void OnUpdateStatus() => RaiseEvent(EventArgs.Empty, nameof(StatusUpdated));

	void RaiseEvent(object eventArgs, string eventName)
	{
		try
		{
			eventManager.HandleEvent(this, eventArgs, eventName);
		}
		catch (Exception exception) when (RunningSourceUpdate is SourceUpdate sourceUpdate)
		{
			sourceUpdate.Catch(exception);
		}
	}

	sealed class SourceUpdate(MediaElement mediaElement, ILogger logger)
	{
		const string handlerExceptionMessage = "An event handler threw an exception while the source of the MediaElement was updated";

		// Code that captured the flow of the update, such as the continuation of an async event handler, can run after the update has ended: the
		// update then catches nothing more, and lets go of the MediaElement, which that code must not keep alive.
		MediaElement? runningFor = mediaElement;
		Exception? firstException;

		public bool IsRunningFor(MediaElement element) => ReferenceEquals(Volatile.Read(ref runningFor), element);

		public void Catch(Exception exception)
		{
			exception = Unwrap(exception);
			logger.LogError(exception, "{LogMessage}", Describe(exception));
			Interlocked.CompareExchange(ref firstException, exception, null);
		}

		public void End()
		{
			var element = Interlocked.Exchange(ref runningFor, null);
			var exception = Interlocked.Exchange(ref firstException, null);
			if (element is null || exception is null)
			{
				return;
			}

			// Not through OnMediaFailed: the source has not failed, so CurrentState, Duration and Position stay as the update set them.
			try
			{
				element.eventManager.HandleEvent(element, new MediaFailedEventArgs(Describe(exception)), nameof(MediaFailed));
			}
			catch (Exception mediaFailedException)
			{
				// Reported like the exceptions of the update, it would be reported again, and again.
				logger.LogError(Unwrap(mediaFailedException), "{LogMessage}", $"A {nameof(MediaFailed)} handler threw an exception while this was reported: {Describe(exception)}");
			}
		}

		static string Describe(Exception exception) => $"{handlerExceptionMessage}: {exception.GetType().FullName}: {exception.Message}";

		// MAUI's WeakEventManager invokes the handlers through reflection.
		static Exception Unwrap(Exception exception) => exception is TargetInvocationException { InnerException: Exception innerException } ? innerException : exception;
	}
}