namespace CommunityToolkit.Maui.Sample.Pages.Views;

/// <summary>
/// Added a custom slider as a workaround for a bug in the regular Slider. See: https://github.com/dotnet/maui/issues/12285
/// Original implementation by David Britch: https://github.com/davidbritch/dotnet-maui-videoplayer/blob/main/src/VideoDemos/Controls/PositionSlider.cs
/// </summary>
public class PositionSlider : Slider
{
	public static readonly BindableProperty DurationProperty =
		BindableProperty.Create(nameof(Duration), typeof(TimeSpan), typeof(PositionSlider), new TimeSpan(1),
			propertyChanged: (bindable, oldValue, newValue) =>
			{
				double seconds = ((TimeSpan)newValue).TotalSeconds;
				((Slider)bindable).Maximum = seconds <= 0 ? 1 : seconds;
			});

	public static readonly BindableProperty PositionProperty =
		BindableProperty.Create(nameof(Position), typeof(TimeSpan), typeof(PositionSlider), TimeSpan.Zero,
			propertyChanged: (bindable, oldValue, newValue) =>
			{
				double seconds = ((TimeSpan)newValue).TotalSeconds;
				((Slider)bindable).Value = seconds;
			});

	public TimeSpan Duration
	{
		get { return (TimeSpan)GetValue(DurationProperty); }
		set { SetValue(DurationProperty, value); }
	}

	public TimeSpan Position
	{
		get { return (TimeSpan)GetValue(PositionProperty); }
		set { SetValue(PositionProperty, value); }
	}

	public PositionSlider()
	{
		PropertyChanged += (sender, args) =>
		{
			if (args.PropertyName == nameof(Value))
			{
				TimeSpan newPosition = TimeSpan.FromSeconds(Value);
				if (Math.Abs(newPosition.TotalSeconds - Position.TotalSeconds) / Duration.TotalSeconds > 0.01)
				{
					Position = newPosition;
				}
			}
		};
	}
}
