using System.ComponentModel;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents an object used to render audio and video to the display.
/// </summary>
public partial class MediaElement : View, IMediaElement, IDisposable
{
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
	[BindableProperty(DefaultValue = MediaElementDefaults.MediaHeight)]
	public partial int MediaHeight { get; }

	/// <summary>
	/// Gets the <see cref="MediaWidth"/> in pixels.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.MediaWidth)]
	public partial int MediaWidth { get; }
	
	/// <summary>
	/// Gets the current <see cref="Position"/> of the media playback.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.Position)]
	public partial TimeSpan Position { get; }
	
	/// <summary>
	/// Gets the <see cref="Duration"/> of the media.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.Duration)]
	public partial TimeSpan Duration { get; }

	/// <summary>
	/// Gets or sets the <see cref="AndroidViewType"/> of the media.
	/// </summary>
	public AndroidViewType AndroidViewType { get; init; } = MediaElementOptions.DefaultAndroidViewType;

	/// <summary>
	/// Gets or sets the <see cref="Aspect"/> ratio used to display the video content.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.Aspect)]
	public partial Aspect Aspect { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ShouldAutoPlay"/> indicating whether the media should play automatically.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.ShouldAutoPlay)]
	public partial bool ShouldAutoPlay { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ShouldLoopPlayback"/> indicating whether the media should loop playback.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.ShouldLoopPlayback)]
	public partial bool ShouldLoopPlayback { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ShouldKeepScreenOn"/> indicating whether the screen should be kept on during media playback.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.ShouldKeepScreenOn)]
	public partial bool ShouldKeepScreenOn { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ShouldMute"/> indicating whether the media should be muted.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.ShouldMute)]
	public partial bool ShouldMute { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ShouldShowPlaybackControls"/> indicating whether playback controls should be shown.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.ShouldShowPlaybackControls)]
	public partial bool ShouldShowPlaybackControls { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="Source"/> of the media.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnSourcePropertyChanged), PropertyChangingMethodName = nameof(OnSourcePropertyChanging))]
	[TypeConverter(typeof(MediaSourceConverter))]
	public partial MediaSource? Source { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="Speed"/> of the media playback.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.Speed)]
	public partial double Speed { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="MetadataTitle"/> of the media.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.MetadataTitle)]
	public partial string MetadataTitle { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="MetadataArtist"/> of the media.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.MetadataArtist)]
	public partial string MetadataArtist { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="MetadataArtworkUrl"/> of the media.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.MetadataArtworkUrl)]
	public partial string MetadataArtworkUrl { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="Volume"/> of the media.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.Volume, DefaultBindingMode = BindingMode.TwoWay, PropertyChangingMethodName = nameof(OnVolumeChanging))]
	public partial double Volume { get; set; }
	
	/// <summary>
	/// Gets or sets the <see cref="CurrentState"/> of the media.
	/// </summary>
	[BindableProperty(DefaultValue = MediaElementDefaults.CurrentState, PropertyChangedMethodName = nameof(OnCurrentStatePropertyChanged))]
	public partial MediaElementState CurrentState { get; private set; }
	
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

	internal void OnMediaEnded()
	{
		CurrentState = MediaElementState.Stopped;
		eventManager.HandleEvent(this, EventArgs.Empty, nameof(MediaEnded));
	}

	internal void OnMediaFailed(MediaFailedEventArgs args)
	{
		((IMediaElement)this).Duration = ((IMediaElement)this).Position = TimeSpan.Zero;

		CurrentState = MediaElementState.Failed;
		eventManager.HandleEvent(this, args, nameof(MediaFailed));
	}

	internal void OnMediaOpened()
	{
		InitializeTimer();
		eventManager.HandleEvent(this, EventArgs.Empty, nameof(MediaOpened));
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

	static void OnVolumeChanging(BindableObject bindable, object oldValue, object newValue)
	{
		var updatedVolume = (double)newValue;

		if (updatedVolume is < 0.0 or > 1.0)
		{
			throw new ArgumentOutOfRangeException(nameof(newValue), $"{nameof(Volume)} can not be less than 0.0 or greater than 1.0");
		}
	}
	
	void IMediaElement.MediaEnded() => OnMediaEnded();

	void IMediaElement.MediaFailed(MediaFailedEventArgs args) => OnMediaFailed(args);

	void IMediaElement.MediaOpened() => OnMediaOpened();

	void IMediaElement.SeekCompleted() => OnSeekCompleted();

	void IMediaElement.CurrentStateChanged(MediaElementState newState) => CurrentState = newState;

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

	void OnPositionChanged(MediaPositionChangedEventArgs mediaPositionChangedEventArgs) =>
		eventManager.HandleEvent(this, mediaPositionChangedEventArgs, nameof(PositionChanged));

	void OnStateChanged(MediaStateChangedEventArgs mediaStateChangedEventArgs) =>
		eventManager.HandleEvent(this, mediaStateChangedEventArgs, nameof(StateChanged));

	void OnPauseRequested() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(PauseRequested));

	void OnPlayRequested() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(PlayRequested));

	void OnStopRequested() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(StopRequested));

	void OnSeekCompleted() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(SeekCompleted));

	void OnPositionRequested() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(PositionRequested));

	void OnUpdateStatus() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(StatusUpdated));
}