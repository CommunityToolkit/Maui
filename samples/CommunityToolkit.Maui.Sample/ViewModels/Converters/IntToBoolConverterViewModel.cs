namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class IntToBoolConverterViewModel : BaseViewModel
{
	int _index;

	public int Number
	{
		get => _index;
		set => SetProperty(ref _index, value);
	}
}