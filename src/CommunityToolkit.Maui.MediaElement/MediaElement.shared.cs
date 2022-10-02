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
		  BindableProperty.Create(nameof(CurrentState), typeof(MediaElementState), typeof(MediaElement), MediaElementState.Closed, propertyChanged: CurrentStateChanged);

	public static readonly BindableProperty DurationProperty =
		  BindableProperty.Create(nameof(Duration), typeof(TimeSpan), typeof(MediaElement), null);

	public static readonly BindableProperty IsLoopingProperty =
		  BindableProperty.Create(nameof(IsLooping), typeof(bool), typeof(MediaElement), false);

	public static readonly BindableProperty PositionProperty =
		  BindableProperty.Create(nameof(Position), typeof(TimeSpan), typeof(MediaElement), TimeSpan.Zero, propertyChanged: OnPositionPropertyChanged);

	public static readonly BindableProperty SourceProperty =
		BindableProperty.Create(nameof(Source), typeof(MediaSource), typeof(MediaElement), null,
			propertyChanged: OnSourcePropertyChanged);

	public static readonly BindableProperty SpeedProperty =
		  BindableProperty.Create(nameof(Speed), typeof(double), typeof(MediaElement), 1.0);

	public static readonly BindableProperty VideoHeightProperty =
		BindableProperty.Create(nameof(VideoHeight), typeof(int), typeof(MediaElement));

	public static readonly BindableProperty VideoWidthProperty =
		BindableProperty.Create(nameof(VideoWidth), typeof(int), typeof(MediaElement));

	public static readonly BindableProperty VolumeProperty =
		  BindableProperty.Create(nameof(Volume), typeof(double), typeof(MediaElement), 1.0, BindingMode.TwoWay, new BindableProperty.ValidateValueDelegate(ValidateVolume));

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

	public TimeSpan Duration { get; internal set; }

	public bool IsLooping
	{
		get => (bool)GetValue(IsLoopingProperty);
		set => SetValue(IsLoopingProperty, value);
	}

	public TimeSpan Position { get; set; }

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

	public void Pause() { }

	public void Play() { }

	public void Stop() { }

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

	internal event EventHandler<SeekRequested>? SeekRequested;

	static void CurrentStateChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var element = (MediaElement)bindable;

		switch ((MediaElementState)newValue)
		{
			// TODO
			//case MediaElementState.Playing:
			//	// Start a timer to poll the platform control position while playing
			//	Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			//	{
			//		if (!element.isSeeking)
			//		{
			//			Device.BeginInvokeOnMainThread(() =>
			//			{
			//				element.PositionRequested?.Invoke(element, EventArgs.Empty);
			//			});
			//		}

			//		return element.CurrentState == MediaElementState.Playing;
			//	});
			//	break;
		}
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

	static void OnSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((MediaElement)bindable).Source = (MediaSource?)newValue;
	}

	void RequestSeek(TimeSpan newPosition)
	{
		isSeeking = true;
		SeekRequested?.Invoke(this, new SeekRequested(newPosition));
	}

	static bool ValidateVolume(BindableObject o, object newValue)
	{
		var volume = (double)newValue;

		return volume >= 0.0 && volume <= 1.0;
	}
}
