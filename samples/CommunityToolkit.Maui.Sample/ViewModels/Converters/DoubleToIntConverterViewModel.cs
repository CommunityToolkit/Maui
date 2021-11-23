namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class DoubleToIntConverterViewModel : BaseViewModel
{
	double _index;

	public double Input
	{
		get => _index;
		set => SetProperty(ref _index, value);
	}
}