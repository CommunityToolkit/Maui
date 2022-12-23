using System.ComponentModel;
using CommunityToolkit.Maui.MediaElement.Converters;

namespace CommunityToolkit.Maui.MediaElement;

public class MediaElement : View, IMediaElement
{
	Microsoft.Maui.Dispatching.IDispatcherTimer? timer;

	internal event EventHandler? UpdateStatus;
	internal event EventHandler? PlayRequested;
	internal event EventHandler? PauseRequested;
	internal event EventHandler? PositionRequested;
	internal event EventHandler<SeekRequestedEventArgs>? SeekRequested;
	internal event EventHandler? StopRequested;

	public MediaElement()
	{
		InitTimer();
	}

	/// <summary>
	/// Occurs when the media has reached the end successfully.
	/// </summary>
	/// <remarks>This is not triggered when the media fails during playback.</remarks>
	public event EventHandler? MediaEnded;

	/// <summary>
	/// Occurs when the media fails to load or fails during playback.
	/// </summary>
	public event EventHandler<MediaFailedEventArgs>? MediaFailed;

	/// <summary>
	/// Occurs when the media has opened successfully and is ready for playback.
	/// </summary>
	public event EventHandler? MediaOpened;

	/// <inheritdoc/>
	public event EventHandler? SeekCompleted;

	/// <inheritdoc/>
	public event EventHandler<MediaStateChangedEventArgs>? StateChanged;

	/// <inheritdoc/>
	public event EventHandler<MediaPositionEventArgs>? PositionChanged;

	public static readonly BindableProperty AutoPlayProperty =
		BindableProperty.Create(nameof(AutoPlay), typeof(bool), typeof(MediaElement), false,
			propertyChanged: OnAutoPlayPropertyChanged);

	public static readonly BindableProperty CurrentStateProperty =
		  BindableProperty.Create(nameof(CurrentState), typeof(MediaElementState), typeof(MediaElement),
			  MediaElementState.None);

	static readonly BindablePropertyKey durationPropertyKey =
		  BindableProperty.CreateReadOnly(nameof(Duration), typeof(TimeSpan), typeof(MediaElement),
			  TimeSpan.Zero);

	public static readonly BindableProperty DurationProperty = durationPropertyKey.BindableProperty;

	public static readonly BindableProperty IsLoopingProperty =
		  BindableProperty.Create(nameof(IsLooping), typeof(bool), typeof(MediaElement), false);

	public static readonly BindableProperty KeepScreenOnProperty =
		  BindableProperty.Create(nameof(KeepScreenOn), typeof(bool), typeof(MediaElement), false);

	public static readonly BindableProperty PositionProperty =
		  BindableProperty.Create(nameof(Position), typeof(TimeSpan), typeof(MediaElement), TimeSpan.Zero);

	public static readonly BindableProperty ShowsPlaybackControlsProperty =
		  BindableProperty.Create(nameof(ShowsPlaybackControls), typeof(bool), typeof(MediaElement), true);

	public static readonly BindableProperty SourceProperty =
		BindableProperty.Create(nameof(Source), typeof(MediaSource), typeof(MediaElement), null,
			propertyChanging: OnSourcePropertyChanging, propertyChanged: OnSourcePropertyChanged);

	public static readonly BindableProperty SpeedProperty =
		  BindableProperty.Create(nameof(Speed), typeof(double), typeof(MediaElement), 1.0,
			  propertyChanged: OnSpeedPropertyChanged);

	public static readonly BindableProperty VideoHeightProperty =
		BindableProperty.Create(nameof(VideoHeight), typeof(int), typeof(MediaElement));

	public static readonly BindableProperty VideoWidthProperty =
		BindableProperty.Create(nameof(VideoWidth), typeof(int), typeof(MediaElement));

	public static readonly BindableProperty VolumeProperty =
		  BindableProperty.Create(nameof(Volume), typeof(double), typeof(MediaElement), 1.0,
			  BindingMode.TwoWay, new BindableProperty.ValidateValueDelegate(ValidateVolume));

	public bool AutoPlay
	{
		get { return (bool)GetValue(AutoPlayProperty); }
		set { SetValue(AutoPlayProperty, value); }
	}

	public MediaElementState CurrentState
	{
		get => (MediaElementState)GetValue(CurrentStateProperty);
		private set => SetValue(CurrentStateProperty, value);
	}

	public TimeSpan Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
	}

	public bool IsLooping
	{
		get => (bool)GetValue(IsLoopingProperty);
		set => SetValue(IsLoopingProperty, value);
	}

	public bool KeepScreenOn
	{
		get => (bool)GetValue(KeepScreenOnProperty);
		set => SetValue(KeepScreenOnProperty, value);
	}

	public TimeSpan Position
	{
		get => (TimeSpan)GetValue(PositionProperty);
	}

	public bool ShowsPlaybackControls
	{
		get => (bool)GetValue(ShowsPlaybackControlsProperty);
		set => SetValue(ShowsPlaybackControlsProperty, value);
	}

	[TypeConverter(typeof(MediaSourceConverter))]
	public MediaSource? Source
	{
		get { return (MediaSource)GetValue(SourceProperty); }
		set { SetValue(SourceProperty, value); }
	}

	public double Speed
	{
		get => (double)GetValue(SpeedProperty);
		set => SetValue(SpeedProperty, value);
	}

	public int VideoHeight
	{
		get => (int)GetValue(VideoHeightProperty);
		internal set => SetValue(VideoHeightProperty, value);
	}

	public int VideoWidth
	{
		get => (int)GetValue(VideoWidthProperty);
		internal set => SetValue(VideoWidthProperty, value);
	}

	public double Volume
	{
		get => (double)GetValue(VolumeProperty);
		set => SetValue(VolumeProperty, value);
	}

	public void Play()
	{
		InitTimer();
		PlayRequested?.Invoke(this, EventArgs.Empty);
		Handler?.Invoke(nameof(MediaElement.PlayRequested));
	}

	public void Pause()
	{
		PauseRequested?.Invoke(this, EventArgs.Empty);
		Handler?.Invoke(nameof(MediaElement.PauseRequested));
	}

	public void SeekTo(TimeSpan position)
	{
		SeekRequestedEventArgs args = new(position);
		Handler?.Invoke(nameof(MediaElement.SeekRequested), args);
	}

	public void Stop()
	{
		ClearTimer();
		StopRequested?.Invoke(this, EventArgs.Empty);
		Handler?.Invoke(nameof(MediaElement.StopRequested));
	}

	void OnTimerTick(object? sender, EventArgs e)
	{
		if (Source is not null)
		{
			PositionRequested?.Invoke(this, EventArgs.Empty);
		}

		UpdateStatus?.Invoke(this, EventArgs.Empty);
		Handler?.Invoke(nameof(MediaElement.UpdateStatus));
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

	static void OnSourcePropertyChanged(BindableObject bindable, object oldvalue, object newvalue) =>
			((MediaElement)bindable).OnSourcePropertyChanged((MediaSource)newvalue);

	void OnSourcePropertyChanged(MediaSource newvalue)
	{
		if (newvalue is not null)
		{
			newvalue.SourceChanged += OnSourceChanged;
			SetInheritedBindingContext(newvalue, BindingContext);
		}

		InvalidateMeasure();
	}

	static void OnSourcePropertyChanging(BindableObject bindable, object oldvalue, object newvalue) =>
			((MediaElement)bindable).OnSourcePropertyChanging((MediaSource)oldvalue);

	void OnSourcePropertyChanging(MediaSource oldvalue)
	{
		if (oldvalue is null)
		{
			return;
		}

		oldvalue.SourceChanged -= OnSourceChanged;
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