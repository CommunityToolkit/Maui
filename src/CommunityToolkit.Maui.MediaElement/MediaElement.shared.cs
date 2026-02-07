using System.ComponentModel;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;

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
	[TypeConverter(typeof(MediaSourceConverter))]
	public MediaSource? Source
	{
		get => (MediaSource)GetValue(SourceProperty);
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