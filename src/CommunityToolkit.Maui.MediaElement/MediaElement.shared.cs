using System.ComponentModel;
using CommunityToolkit.Maui.MediaElement.Converters;

namespace CommunityToolkit.Maui.MediaElement;

/// <summary>
/// Represents an object that to render audio and video to the display.
/// </summary>
public class MediaElement : View, IMediaElement
{
	Microsoft.Maui.Dispatching.IDispatcherTimer? timer;

	internal event EventHandler? UpdateStatus;
	internal event EventHandler? PlayRequested;
	internal event EventHandler? PauseRequested;
	internal event EventHandler? PositionRequested;
	internal event EventHandler<MediaSeekRequestedEventArgs>? SeekRequested;
	internal event EventHandler? StopRequested;

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaElement"/> class.
	/// </summary>
	public MediaElement()
	{
		InitTimer();
	}

	/// <inheritdoc cref="IMediaElement.MediaEnded"/>
	public event EventHandler? MediaEnded;

	/// <inheritdoc cref="IMediaElement.MediaFailed(MediaFailedEventArgs)"/>
	public event EventHandler<MediaFailedEventArgs>? MediaFailed;

	/// <inheritdoc cref="IMediaElement.MediaOpened"/>
	public event EventHandler? MediaOpened;

	/// <inheritdoc cref="IMediaElement.SeekCompleted"/>
	public event EventHandler? SeekCompleted;

	/// <inheritdoc cref="IMediaElement.StateChanged"/>
	public event EventHandler<MediaStateChangedEventArgs>? StateChanged;

	/// <inheritdoc cref="IMediaElement.PositionChanged"/>
	public event EventHandler<MediaPositionChangedEventArgs>? PositionChanged;

	/// <summary>
	/// Backing store for the <see cref="AutoPlay"/> property.
	/// </summary>
	public static readonly BindableProperty AutoPlayProperty =
		BindableProperty.Create(nameof(AutoPlay), typeof(bool), typeof(MediaElement), false,
			propertyChanged: OnAutoPlayPropertyChanged);

	/// <summary>
	/// Backing store for the <see cref="CurrentState"/> property.
	/// </summary>
	public static readonly BindableProperty CurrentStateProperty =
		  BindableProperty.Create(nameof(CurrentState), typeof(MediaElementState), typeof(MediaElement),
			  MediaElementState.None);

	static readonly BindablePropertyKey durationPropertyKey =
		  BindableProperty.CreateReadOnly(nameof(Duration), typeof(TimeSpan), typeof(MediaElement),
			  TimeSpan.Zero);

	/// <summary>
	/// Backing store for the <see cref="Duration"/> property.
	/// </summary>
	public static readonly BindableProperty DurationProperty = durationPropertyKey.BindableProperty;

	/// <summary>
	/// Backing store for the <see cref="IsLooping"/> property.
	/// </summary>
	public static readonly BindableProperty IsLoopingProperty =
		  BindableProperty.Create(nameof(IsLooping), typeof(bool), typeof(MediaElement), false);

	/// <summary>
	/// Backing store for the <see cref="KeepScreenOn"/> property.
	/// </summary>
	public static readonly BindableProperty KeepScreenOnProperty =
		  BindableProperty.Create(nameof(KeepScreenOn), typeof(bool), typeof(MediaElement), false);

	/// <summary>
	/// Backing store for the <see cref="Position"/> property.
	/// </summary>
	public static readonly BindableProperty PositionProperty =
		  BindableProperty.Create(nameof(Position), typeof(TimeSpan), typeof(MediaElement), TimeSpan.Zero);

	/// <summary>
	/// Backing store for the <see cref="ShowsPlaybackControls"/> property.
	/// </summary>
	public static readonly BindableProperty ShowsPlaybackControlsProperty =
		  BindableProperty.Create(nameof(ShowsPlaybackControls), typeof(bool), typeof(MediaElement), true);

	/// <summary>
	/// Backing store for the <see cref="Source"/> property.
	/// </summary>
	public static readonly BindableProperty SourceProperty =
		BindableProperty.Create(nameof(Source), typeof(MediaSource), typeof(MediaElement), null,
			propertyChanging: OnSourcePropertyChanging, propertyChanged: OnSourcePropertyChanged);

	/// <summary>
	/// Backing store for the <see cref="Speed"/> property.
	/// </summary>
	public static readonly BindableProperty SpeedProperty =
		  BindableProperty.Create(nameof(Speed), typeof(double), typeof(MediaElement), 1.0,
			  propertyChanged: OnSpeedPropertyChanged);

	/// <summary>
	/// Backing store for the <see cref="VideoHeight"/> property.
	/// </summary>
	public static readonly BindableProperty VideoHeightProperty =
		BindableProperty.Create(nameof(VideoHeight), typeof(int), typeof(MediaElement));

	/// <summary>
	/// Backing store for the <see cref="VideoWidth"/> property.
	/// </summary>
	public static readonly BindableProperty VideoWidthProperty =
		BindableProperty.Create(nameof(VideoWidth), typeof(int), typeof(MediaElement));

	/// <summary>
	/// Backing store for the <see cref="Volume"/> property.
	/// </summary>
	public static readonly BindableProperty VolumeProperty =
		  BindableProperty.Create(nameof(Volume), typeof(double), typeof(MediaElement), 1.0,
			  BindingMode.TwoWay, new BindableProperty.ValidateValueDelegate(ValidateVolume));

	/// <summary>
	/// Gets or sets whether the media should start playing as soon as it's loaded.
	/// Default is <see langword="false"/>. This is a bindable property.
	/// </summary>
	public bool AutoPlay
	{
		get { return (bool)GetValue(AutoPlayProperty); }
		set { SetValue(AutoPlayProperty, value); }
	}

	/// <summary>
	/// The current state of the <see cref="MediaElement"/>. This is a bindable property.
	/// </summary>
	public MediaElementState CurrentState
	{
		get => (MediaElementState)GetValue(CurrentStateProperty);
		private set => SetValue(CurrentStateProperty, value);
	}

	/// <summary>
	/// Gets total duration of the loaded media. This is a bindable property.
	/// </summary>
	/// <remarks>Might not be available for some types, like live streams.</remarks>
	public TimeSpan Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
	}

	/// <summary>
	/// Gets or sets if the video will play when reaches the end.
	/// Default is <see langword="false"/>. This is a bindable property.
	/// </summary>
	public bool IsLooping
	{
		get => (bool)GetValue(IsLoopingProperty);
		set => SetValue(IsLoopingProperty, value);
	}

	/// <summary>
	/// Gets or sets if media playback will prevent the device display from going to sleep.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>If media is paused, stopped or has completed playing, the display will turn off.</remarks>
	public bool KeepScreenOn
	{
		get => (bool)GetValue(KeepScreenOnProperty);
		set => SetValue(KeepScreenOnProperty, value);
	}

	/// <inheritdoc cref="IMediaElement.Pause"/>
	public void Pause()
	{
		PauseRequested?.Invoke(this, EventArgs.Empty);
		Handler?.Invoke(nameof(PauseRequested));
	}

	/// <inheritdoc cref="IMediaElement.Play"/>
	public void Play()
	{
		InitTimer();
		PlayRequested?.Invoke(this, EventArgs.Empty);
		Handler?.Invoke(nameof(PlayRequested));
	}

	/// <summary>
	/// The current position of the playing media. This is a bindable property.
	/// </summary>
	public TimeSpan Position
	{
		get => (TimeSpan)GetValue(PositionProperty);
	}

	/// <inheritdoc cref="IMediaElement.SeekTo(TimeSpan)"/>
	public void SeekTo(TimeSpan position)
	{
		MediaSeekRequestedEventArgs args = new(position);
		Handler?.Invoke(nameof(SeekRequested), args);
	}

	/// <summary>
	/// Gets or sets whether the player should show the platform playback controls.
	/// This is a bindable property.
	/// </summary>
	public bool ShowsPlaybackControls
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
		get { return (MediaSource)GetValue(SourceProperty); }
		set { SetValue(SourceProperty, value); }
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

	/// <inheritdoc cref="IMediaElement.Stop"/>
	public void Stop()
	{
		ClearTimer();
		StopRequested?.Invoke(this, EventArgs.Empty);
		Handler?.Invoke(nameof(StopRequested));
	}

	/// <summary>
	/// Gets the height (in pixels) of the loaded media in pixels.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>Not reported for non-visual media.</remarks>
	public int VideoHeight
	{
		get => (int)GetValue(VideoHeightProperty);
		internal set => SetValue(VideoHeightProperty, value);
	}

	/// <summary>
	/// Gets the width (in pixels) of the loaded media in pixels.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>Not reported for non-visual media.</remarks>
	public int VideoWidth
	{
		get => (int)GetValue(VideoWidthProperty);
		internal set => SetValue(VideoWidthProperty, value);
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

	void OnTimerTick(object? sender, EventArgs e)
	{
		PositionRequested?.Invoke(this, EventArgs.Empty);
		UpdateStatus?.Invoke(this, EventArgs.Empty);
		Handler?.Invoke(nameof(UpdateStatus));
	}

	void InitTimer()
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

	internal void OnMediaEnded()
	{
		ClearTimer();
		CurrentState = MediaElementState.Stopped;
		MediaEnded?.Invoke(this, EventArgs.Empty);
	}

	internal void OnMediaFailed(MediaFailedEventArgs args)
	{
		((IMediaElement)this).Duration = ((IMediaElement)this).Position = TimeSpan.Zero;

		var previousState = CurrentState;
		CurrentState = MediaElementState.Failed;

		StateChanged?.Invoke(this, new(previousState, CurrentState));

		MediaFailed?.Invoke(this, args);
	}

	internal void OnMediaOpened()
	{
		InitTimer();
		MediaOpened?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc/>
	protected override void OnBindingContextChanged()
	{
		if (Source != null)
		{
			SetInheritedBindingContext(Source, BindingContext);
		}

		base.OnBindingContextChanged();
	}

	static void OnAutoPlayPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((MediaElement)bindable).AutoPlay = (bool)newValue;
	}

	void OnSourceChanged(object? sender, EventArgs eventArgs)
	{
		OnPropertyChanged(SourceProperty.PropertyName);
		InvalidateMeasure();
	}

	static void OnSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
			((MediaElement)bindable).OnSourcePropertyChanged((MediaSource)newValue);

	void OnSourcePropertyChanged(MediaSource newValue)
	{
		if (newValue is not null)
		{
			newValue.SourceChanged += OnSourceChanged;
			SetInheritedBindingContext(newValue, BindingContext);
		}

		InvalidateMeasure();
	}

	static void OnSourcePropertyChanging(BindableObject bindable, object oldValue, object newValue) =>
			((MediaElement)bindable).OnSourcePropertyChanging((MediaSource)oldValue);

	void OnSourcePropertyChanging(MediaSource oldValue)
	{
		if (oldValue is null)
		{
			return;
		}

		oldValue.SourceChanged -= OnSourceChanged;
	}

	static void OnSpeedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((MediaElement)bindable).Speed = (double)newValue;
	}

	static bool ValidateVolume(BindableObject o, object newValue)
	{
		var volume = (double)newValue;

		return volume >= 0.0 && volume <= 1.0;
	}

	TimeSpan IMediaElement.Position
	{
		get => (TimeSpan)GetValue(PositionProperty);
		set
		{
			var currentValue = (TimeSpan)GetValue(PositionProperty);

			if (currentValue != value)
			{
				SetValue(PositionProperty, value);
				PositionChanged?.Invoke(this, new(value));
			}
		}
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
		SeekCompleted?.Invoke(this, EventArgs.Empty);
	}

	TimeSpan IMediaElement.Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
		set => SetValue(durationPropertyKey, value);
	}

	void IMediaElement.CurrentStateChanged(MediaElementState newState)
	{
		if (CurrentState != newState)
		{
			var previousState = CurrentState;
			CurrentState = newState;

			StateChanged?.Invoke(this, new(previousState, CurrentState));
		}
	}
}