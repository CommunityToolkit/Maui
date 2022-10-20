namespace CommunityToolkit.Maui.Sample;

public partial class TouchPageSample : ContentPage
{
	bool nativeAnimationBorderless;

	public bool NativeAnimationBorderless
	{
		get => nativeAnimationBorderless;
		set
		{
			nativeAnimationBorderless = value;
			OnPropertyChanged();
		}
	}

	public int TouchCount { get; private set; }

	public int LongPressCount { get; private set; }

	public Command TouchCountCommand => new(TouchCountCommandExecute);

	public TouchPageSample()
	{
		InitializeComponent();
		BindingContext = this;
		img.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == "Source")
			{
				var v = img.Source;
			}
		};
	}


	void TouchCountCommandExecute()
	{
		TouchCount++;
		OnPropertyChanged("TouchCount");
	}
}