namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class CompareConverterViewModel : BaseViewModel
{
	double sliderValue = 0.5;

	public double SliderValue
	{
		get => sliderValue;
		set => SetProperty(ref sliderValue, value);
	}
}