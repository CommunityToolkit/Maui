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
	/// The current position of the playing media. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial TimeSpan Position { get; set; }

	/// <summary>
	/// Gets total duration of the loaded media. This is a bindable property.
	/// </summary>
	/// <remarks>Might not be available for some types, like live streams.</remarks>
	[BindableProperty]
	public partial TimeSpan Duration { get; set; }

	/// <summary>
	/// Gets or sets whether the media should start playing as soon as it's loaded.
	/// Default is <see langword="false"/>. This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultValue = true)]
	public partial bool ShouldAutoPlay { get; set; }

	/// <summary>
	/// Gets or sets if the video plays when reaches the end.
	/// Default is <see langword="false"/>. This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultValue = false)]
	public partial bool ShouldLoopPlayback { get; set; }

	/// <summary>
	/// Gets or sets if media playback prevents the device display from going to sleep.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>If media is paused, stopped or has completed playing, the display will turn off.</remarks>
	[BindableProperty(DefaultValue = false)]
	public partial bool ShouldKeepScreenOn { get; set; }

	/// <summary>
	/// Gets or sets if audio should be muted. This is a bindable property.
	/// </summary>
	/// <remarks>
	/// When <see cref="ShouldMute"/> is <see langword="true" />, changes to <see cref="Volume"/> are ignored.
	/// The new volume will be applied when <see cref="ShouldMute"/> is set to <see langword="false" /> again.
	/// When the user uses the platform player controls to influence the volume, it might still unmute.
	/// </remarks>
	[BindableProperty(DefaultValue = false)]
	public partial bool ShouldMute { get; set; }

	/// <summary>
	/// Gets or sets whether the player should show the platform playback controls.
	/// This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultValue = true)]
	public partial bool ShouldShowPlaybackControls { get; set; }

	/// <summary>
	/// Gets or sets the source of the media to play.
	/// This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnSourcePropertyChanged), PropertyChangingMethodName = nameof(OnSourcePropertyChanging))]
	[TypeConverter(typeof(MediaSourceConverter))]
	public partial MediaSource? Source { get; set; }

	/// <summary>
	/// Gets or sets the speed with which the media should be played.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>A value of 1 means normal speed.
	/// Anything more than 1 is faster speed, anything less than 1 is slower speed.</remarks>
	[BindableProperty]
	public partial double Speed { get; set; }

	static readonly BindablePropertyKey mediaHeightPropertyKey =
		BindableProperty.CreateReadOnly(nameof(MediaHeight), typeof(int), typeof(MediaElement), 0);

	/// <summary>
	/// Backing store for the <see cref="MediaHeight"/> property.
	/// </summary>
	public static readonly BindableProperty MediaHeightProperty =
		mediaHeightPropertyKey.BindableProperty;

	static readonly BindablePropertyKey mediaWidthPropertyKey =
		BindableProperty.CreateReadOnly(nameof(MediaWidth), typeof(int), typeof(MediaElement), 0);

	/// <summary>
	/// Backing store for the <see cref="MediaWidth"/> property.
	/// </summary>
	public static readonly BindableProperty MediaWidthProperty =
		mediaWidthPropertyKey.BindableProperty;

	/// <summary>
	/// Gets or sets the Title of the media.
	/// This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultValue = "")]
	public partial string MetadataTitle { get; set; }

	/// <summary>
	/// Gets or sets the Artist of the media.
	/// This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultValue = "")]
	public partial string MetadataArtist { get; set; }

	/// <summary>
	/// Gets or sets the Artwork Image Url of the media.
	/// This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultValue = "")]
	public partial string MetadataArtworkUrl { get; set; }

	/// <summary>
	/// Gets or sets how the media will be scaled to fit the display area.
	/// Default value is <see cref="Aspect.AspectFit"/>. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial Aspect Aspect { get; set; }

	/// <summary>
	/// The current state of the <see cref="MediaElement"/>. This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultValue =MediaElementState.None, PropertyChangingMethodName = nameof(OnCurrentStatePropertyChanged))]
	public partial MediaElementState CurrentState { get; set; }

	/// <summary>
	/// Read the MediaElementOptions set in on construction, cannot be changed after construction
	/// </summary>
	public AndroidViewType AndroidViewType { get; init; } = MediaElementOptions.DefaultAndroidViewType;

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
	/// Gets or sets the volume of the audio for the media.
	/// </summary>
	/// <remarks>
	/// <para>A value of 1 indicates full-volume, 0 is silence.</para>
	/// <para>When <see cref="ShouldMute"/> is <see langword="true" />, changes to <see cref="Volume"/> are ignored.
	/// The new volume will be applied when <see cref="ShouldMute"/> is set to <see langword="false" /> again.
	/// When the user uses the platform player controls to influence the volume, it might still unmute.</para>
	/// </remarks>
	[BindableProperty(DefaultValue = 1.0, PropertyChangedMethodName = nameof(ValidateVolume))]
	public partial double Volume { get; set; }
	TimeSpan IMediaElement.Position
	{
		get => (TimeSpan)GetValue(PositionProperty);
		set
		{
			var currentValue = (TimeSpan)GetValue(PositionProperty);

			if (currentValue != value)
			{
				SetValue(PositionProperty, value);
				OnPositionChanged(new(value));
			}
		}
	}

	/// <summary>
	/// Gets the height (in pixels) of the loaded media in pixels.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>Not reported for non-visual media, sometimes not available for live-streamed content on iOS and macOS.</remarks>
	public int MediaHeight => (int)GetValue(MediaHeightProperty);

	/// <summary>
	/// Gets the width (in pixels) of the loaded media in pixels.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>Not reported for non-visual media, sometimes not available for live-streamed content on iOS and macOS.</remarks>
	public int MediaWidth => (int)GetValue(MediaWidthProperty);

	int IMediaElement.MediaWidth
	{
		get => (int)GetValue(MediaWidthProperty);
		set => SetValue(MediaWidthProperty, value);
	}

	int IMediaElement.MediaHeight
	{
		get => (int)GetValue(MediaHeightProperty);
		set => SetValue(MediaHeightProperty, value);
	}

	/// <inheritdoc/>
	TaskCompletionSource IAsynchronousMediaElementHandler.SeekCompletedTCS => seekCompletedTaskCompletionSource;

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

	static void OnSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
		((MediaElement)bindable).OnSourcePropertyChanged((MediaSource?)newValue);

	static void OnSourcePropertyChanging(BindableObject bindable, object oldValue, object newValue) =>
		((MediaElement)bindable).OnSourcePropertyChanging((MediaSource?)oldValue);

	static void OnCurrentStatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var mediaElement = (MediaElement)bindable;
		var previousState = (MediaElementState)oldValue;
		var newState = (MediaElementState)newValue;

		mediaElement.OnStateChanged(new MediaStateChangedEventArgs(previousState, newState));
	}

	static void ValidateVolume(BindableObject bindable, object oldValue, object newValue)
	{
		var updatedVolume = (double)newValue;

		if (updatedVolume is < 0.0 or > 1.0)
		{
			throw new ArgumentOutOfRangeException(nameof(newValue), $"{nameof(Volume)} can not be less than 0.0 or greater than 1.0");
		}
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

	void OnSourcePropertyChanged(MediaSource? newValue)
	{
		ClearTimer();

		if (newValue is not null)
		{
			newValue.SourceChanged += OnSourceChanged;
			SetInheritedBindingContext(newValue, BindingContext);
		}

		InvalidateMeasure();
		InitializeTimer();
	}

	void OnSourcePropertyChanging(MediaSource? oldValue)
	{
		if (oldValue is null)
		{
			return;
		}

		oldValue.SourceChanged -= OnSourceChanged;
	}

	void IMediaElement.MediaEnded()
	{
		OnMediaEnded();
	}

	void IMediaElement.MediaFailed(MediaFailedEventArgs args)
	{
		OnMediaFailed(args);
	}

	void IMediaElement.MediaOpened()
	{
		OnMediaOpened();
	}

	void IMediaElement.SeekCompleted()
	{
		OnSeekCompleted();
	}

	void IMediaElement.CurrentStateChanged(MediaElementState newState) => CurrentState = newState;

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