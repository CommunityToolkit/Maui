using System.ComponentModel;
using CommunityToolkit.Maui.MediaView.Converters;

namespace CommunityToolkit.Maui.MediaView;

/// <summary>
/// Represents an object that to render audio and video to the display.
/// </summary>
public class MediaView : View, IMediaView
{
	readonly WeakEventManager eventManager = new();

	Microsoft.Maui.Dispatching.IDispatcherTimer? timer;

	static readonly BindablePropertyKey durationPropertyKey =
	  BindableProperty.CreateReadOnly(nameof(Duration), typeof(TimeSpan), typeof(MediaView), TimeSpan.Zero);

	/// <summary>
	/// Backing store for the <see cref="ShouldAutoPlay"/> property.
	/// </summary>
	public static readonly BindableProperty AutoPlayProperty =
		BindableProperty.Create(nameof(ShouldAutoPlay), typeof(bool), typeof(MediaView), false);

	/// <summary>
	/// Backing store for the <see cref="CurrentState"/> property.
	/// </summary>
	public static readonly BindableProperty CurrentStateProperty =
		  BindableProperty.Create(nameof(CurrentState), typeof(MediaViewState), typeof(MediaView), MediaViewState.None, propertyChanged: OnCurrentStatePropertyChanged);

	/// <summary>
	/// Backing store for the <see cref="Duration"/> property.
	/// </summary>
	public static readonly BindableProperty DurationProperty = durationPropertyKey.BindableProperty;

	/// <summary>
	/// Backing store for the <see cref="ShouldLoopPlayback"/> property.
	/// </summary>
	public static readonly BindableProperty IsLoopingProperty =
		  BindableProperty.Create(nameof(ShouldLoopPlayback), typeof(bool), typeof(MediaView), false);

	/// <summary>
	/// Backing store for the <see cref="ShouldKeepScreenOn"/> property.
	/// </summary>
	public static readonly BindableProperty KeepScreenOnProperty =
		  BindableProperty.Create(nameof(ShouldKeepScreenOn), typeof(bool), typeof(MediaView), false);

	/// <summary>
	/// Backing store for the <see cref="Position"/> property.
	/// </summary>
	public static readonly BindableProperty PositionProperty =
		  BindableProperty.Create(nameof(Position), typeof(TimeSpan), typeof(MediaView), TimeSpan.Zero);

	/// <summary>
	/// Backing store for the <see cref="ShouldShowPlaybackControls"/> property.
	/// </summary>
	public static readonly BindableProperty ShowsPlaybackControlsProperty =
		  BindableProperty.Create(nameof(ShouldShowPlaybackControls), typeof(bool), typeof(MediaView), true);

	/// <summary>
	/// Backing store for the <see cref="Source"/> property.
	/// </summary>
	public static readonly BindableProperty SourceProperty =
		BindableProperty.Create(nameof(Source), typeof(MediaSource), typeof(MediaView), null,
			propertyChanging: OnSourcePropertyChanging, propertyChanged: OnSourcePropertyChanged);

	/// <summary>
	/// Backing store for the <see cref="Speed"/> property.
	/// </summary>
	public static readonly BindableProperty SpeedProperty =
		  BindableProperty.Create(nameof(Speed), typeof(double), typeof(MediaView), 1.0);

	/// <summary>
	/// Backing store for the <see cref="MediaHeight"/> property.
	/// </summary>
	public static readonly BindableProperty VideoHeightProperty =
		BindableProperty.Create(nameof(MediaHeight), typeof(int), typeof(MediaView));

	/// <summary>
	/// Backing store for the <see cref="MediaWidth"/> property.
	/// </summary>
	public static readonly BindableProperty VideoWidthProperty =
		BindableProperty.Create(nameof(MediaWidth), typeof(int), typeof(MediaView));

	/// <summary>
	/// Backing store for the <see cref="Volume"/> property.
	/// </summary>
	public static readonly BindableProperty VolumeProperty =
		  BindableProperty.Create(nameof(Volume), typeof(double), typeof(MediaView), 1.0,
			  BindingMode.TwoWay, new BindableProperty.ValidateValueDelegate(ValidateVolume));

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaView"/> class.
	/// </summary>
	public MediaView()
	{
		InitializeTimer();
	}

	/// <inheritdoc cref="IMediaView.MediaEnded"/>
	public event EventHandler MediaEnded
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaView.MediaFailed(MediaFailedEventArgs)"/>
	public event EventHandler<MediaFailedEventArgs> MediaFailed
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaView.MediaOpened"/>
	public event EventHandler MediaOpened
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaView.SeekCompleted"/>
	public event EventHandler SeekCompleted
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaView.StateChanged"/>
	public event EventHandler<MediaStateChangedEventArgs> StateChanged
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="IMediaView.PositionChanged"/>
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
	/// The current position of the playing media. This is a bindable property.
	/// </summary>
	public TimeSpan Position => (TimeSpan)GetValue(PositionProperty);

	/// <summary>
	/// Gets total duration of the loaded media. This is a bindable property.
	/// </summary>
	/// <remarks>Might not be available for some types, like live streams.</remarks>
	public TimeSpan Duration => (TimeSpan)GetValue(DurationProperty);

	/// <summary>
	/// Gets or sets whether the media should start playing as soon as it's loaded.
	/// Default is <see langword="false"/>. This is a bindable property.
	/// </summary>
	public bool ShouldAutoPlay
	{
		get => (bool)GetValue(AutoPlayProperty);
		set => SetValue(AutoPlayProperty, value);
	}

	/// <summary>
	/// Gets or sets if the video will play when reaches the end.
	/// Default is <see langword="false"/>. This is a bindable property.
	/// </summary>
	public bool ShouldLoopPlayback
	{
		get => (bool)GetValue(IsLoopingProperty);
		set => SetValue(IsLoopingProperty, value);
	}

	/// <summary>
	/// Gets or sets if media playback will prevent the device display from going to sleep.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>If media is paused, stopped or has completed playing, the display will turn off.</remarks>
	public bool ShouldKeepScreenOn
	{
		get => (bool)GetValue(KeepScreenOnProperty);
		set => SetValue(KeepScreenOnProperty, value);
	}

	/// <summary>
	/// Gets or sets whether the player should show the platform playback controls.
	/// This is a bindable property.
	/// </summary>
	public bool ShouldShowPlaybackControls
	{
		get => (bool)GetValue(ShowsPlaybackControlsProperty);
		set => SetValue(ShowsPlaybackControlsProperty, value);
	}

	/// <summary>
	/// Gets or sets the source of the media to play.
	/// This is a bindable property.
	/// </summary>
	[TypeConverter(typeof(MediaSourceConverter))]
	public MediaSource? Source
	{
		get => (MediaSource)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>
	/// Gets or sets the volume of the audio for the media.
	/// </summary>
	/// <remarks>A value of 1 means full volume, 0 is silence.</remarks>
	public double Volume
	{
		get => (double)GetValue(VolumeProperty);
		set => SetValue(VolumeProperty, value);
	}

	/// <summary>
	/// Gets or sets the speed with which the media should be played.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>A value of 1 means normal speed.
	/// Anything more than 1 is faster speed, anything less than 1 is slower speed.</remarks>
	public double Speed
	{
		get => (double)GetValue(SpeedProperty);
		set => SetValue(SpeedProperty, value);
	}

	/// <summary>
	/// Gets the height (in pixels) of the loaded media in pixels.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>Not reported for non-visual media.</remarks>
	public int MediaHeight
	{
		get => (int)GetValue(VideoHeightProperty);
		internal set => SetValue(VideoHeightProperty, value);
	}

	/// <summary>
	/// Gets the width (in pixels) of the loaded media in pixels.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>Not reported for non-visual media.</remarks>
	public int MediaWidth
	{
		get => (int)GetValue(VideoWidthProperty);
		internal set => SetValue(VideoWidthProperty, value);
	}

	/// <summary>
	/// The current state of the <see cref="MediaView"/>. This is a bindable property.
	/// </summary>
	public MediaViewState CurrentState
	{
		get => (MediaViewState)GetValue(CurrentStateProperty);
		private set => SetValue(CurrentStateProperty, value);
	}

	TimeSpan IMediaView.Position
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

	TimeSpan IMediaView.Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
		set => SetValue(durationPropertyKey, value);
	}

	/// <inheritdoc cref="IMediaView.Pause"/>
	public void Pause()
	{
		OnPauseRequested();
		Handler?.Invoke(nameof(PauseRequested));
	}

	/// <inheritdoc cref="IMediaView.Play"/>
	public void Play()
	{
		InitializeTimer();
		OnPlayRequested();
		Handler?.Invoke(nameof(PlayRequested));
	}

	/// <inheritdoc cref="IMediaView.SeekTo(TimeSpan)"/>
	public void SeekTo(TimeSpan position)
	{
		MediaSeekRequestedEventArgs args = new(position);
		Handler?.Invoke(nameof(SeekRequested), args);
	}

	/// <inheritdoc cref="IMediaView.Stop"/>
	public void Stop()
	{
		ClearTimer();
		OnStopRequested();
		Handler?.Invoke(nameof(StopRequested));
	}

	internal void OnMediaEnded()
	{
		ClearTimer();
		CurrentState = MediaViewState.Stopped;
		eventManager.HandleEvent(this, EventArgs.Empty, nameof(MediaEnded));
	}

	internal void OnMediaFailed(MediaFailedEventArgs args)
	{
		((IMediaView)this).Duration = ((IMediaView)this).Position = TimeSpan.Zero;

		CurrentState = MediaViewState.Failed;
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

	static void OnSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
		((MediaView)bindable).OnSourcePropertyChanged((MediaSource?)newValue);

	static void OnSourcePropertyChanging(BindableObject bindable, object oldValue, object newValue) =>
		((MediaView)bindable).OnSourcePropertyChanging((MediaSource?)oldValue);

	static void OnCurrentStatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var MediaView = (MediaView)bindable;
		var previousState = (MediaViewState)oldValue;
		var newState = (MediaViewState)newValue;

		MediaView.OnStateChanged(new MediaStateChangedEventArgs(previousState, newState));
	}

	static bool ValidateVolume(BindableObject o, object newValue)
	{
		var volume = (double)newValue;

		return volume >= 0.0 && volume <= 1.0;
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
		if (newValue is not null)
		{
			newValue.SourceChanged += OnSourceChanged;
			SetInheritedBindingContext(newValue, BindingContext);
		}

		InvalidateMeasure();
	}

	void OnSourcePropertyChanging(MediaSource? oldValue)
	{
		if (oldValue is null)
		{
			return;
		}

		oldValue.SourceChanged -= OnSourceChanged;
	}

	void IMediaView.MediaEnded()
	{
		OnMediaEnded();
	}

	void IMediaView.MediaFailed(MediaFailedEventArgs args)
	{
		OnMediaFailed(args);
	}

	void IMediaView.MediaOpened()
	{
		OnMediaOpened();
	}

	void IMediaView.SeekCompleted()
	{
		OnSeekCompeted();
	}

	void IMediaView.CurrentStateChanged(MediaViewState newState) => CurrentState = newState;

	void OnPositionChanged(MediaPositionChangedEventArgs mediaPositionChangedEventArgs) =>
		eventManager.HandleEvent(this, mediaPositionChangedEventArgs, nameof(PositionChanged));

	void OnStateChanged(MediaStateChangedEventArgs mediaStateChangedEventArgs) =>
		eventManager.HandleEvent(this, mediaStateChangedEventArgs, nameof(StateChanged));

	void OnPauseRequested() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(PauseRequested));

	void OnPlayRequested() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(PlayRequested));

	void OnStopRequested() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(StopRequested));

	void OnSeekCompeted() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(SeekCompleted));

	void OnPositionRequested() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(PositionRequested));

	void OnUpdateStatus() => eventManager.HandleEvent(this, EventArgs.Empty, nameof(StatusUpdated));
}