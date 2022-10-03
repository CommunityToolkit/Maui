using System.ComponentModel;
using CommunityToolkit.Maui.MediaElement.Converters;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElement : View, IMediaElement
{
	bool isSeeking = false;

	public static readonly BindableProperty AutoPlayProperty =
		BindableProperty.Create(nameof(AutoPlay), typeof(bool), typeof(MediaElement), false,
			propertyChanged: OnAutoPlayPropertyChanged);

	public static readonly BindableProperty CurrentStateProperty =
		  BindableProperty.Create(nameof(CurrentState), typeof(MediaElementState), typeof(MediaElement),
			  MediaElementState.Closed);

	public static readonly BindableProperty DurationProperty =
		  BindableProperty.Create(nameof(Duration), typeof(TimeSpan), typeof(MediaElement), null);

	public static readonly BindableProperty IsLoopingProperty =
		  BindableProperty.Create(nameof(IsLooping), typeof(bool), typeof(MediaElement), false);

	public static readonly BindableProperty PositionProperty =
		  BindableProperty.Create(nameof(Position), typeof(TimeSpan), typeof(MediaElement), TimeSpan.Zero,
			  propertyChanged: OnPositionPropertyChanged);

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
		internal set => SetValue(CurrentStateProperty, value);
	}

	public TimeSpan Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
		internal set => SetValue(DurationProperty, value);
	}

	public bool IsLooping
	{
		get => (bool)GetValue(IsLoopingProperty);
		set => SetValue(IsLoopingProperty, value);
	}

	public TimeSpan Position
	{
		get
		{
			PositionRequested?.Invoke(this, EventArgs.Empty);
			return (TimeSpan)GetValue(PositionProperty);
		}

		set
		{
			var currentValue = (TimeSpan)GetValue(PositionProperty);

			if (Math.Abs(value.Subtract(currentValue).TotalMilliseconds) > 300 && !isSeeking)
			{
				RequestSeek(value);
			}

			SetValue(PositionProperty, value);
		}
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

	public void Play() => StateRequested?.Invoke(this, new StateRequested(MediaElementState.Playing));

	public void Pause() => StateRequested?.Invoke(this, new StateRequested(MediaElementState.Paused));

	public void Stop() => StateRequested?.Invoke(this, new StateRequested(MediaElementState.Stopped));

	public event EventHandler? MediaEnded;

	public event EventHandler? MediaFailed;

	public event EventHandler? MediaOpened;

	internal void OnMediaEnded()
	{
		SetValue(CurrentStateProperty, MediaElementState.Stopped);
		MediaEnded?.Invoke(this, EventArgs.Empty);
	}

	internal void OnMediaFailed() => MediaFailed?.Invoke(this, EventArgs.Empty);

	internal void OnMediaOpened() => MediaOpened?.Invoke(this, EventArgs.Empty);

	internal event EventHandler? PositionRequested;

	public event EventHandler? SeekCompleted;

	internal event EventHandler<SeekRequested>? SeekRequested;

	internal event EventHandler<StateRequested>? StateRequested;

	internal void OnSeekCompleted()
	{
		isSeeking = false;
		SeekCompleted?.Invoke(this, EventArgs.Empty);
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

	static void OnPositionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var element = (MediaElement)bindable;

		var oldval = (TimeSpan)oldValue;
		var newval = (TimeSpan)newValue;

		if (Math.Abs(newval.Subtract(oldval).TotalMilliseconds) > 300 && !element.isSeeking)
		{
			element.RequestSeek(newval);
		}
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

	void RequestSeek(TimeSpan newPosition)
	{
		// TODO should we set media to pause for smoother seeking?
		isSeeking = true;
		SeekRequested?.Invoke(this, new SeekRequested(newPosition));
	}

	static bool ValidateVolume(BindableObject o, object newValue)
	{
		var volume = (double)newValue;

		return volume >= 0.0 && volume <= 1.0;
	}
}
