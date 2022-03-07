namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class InvertedBoolConverterViewModel : BaseViewModel
{
	bool isToggled;

	public bool IsToggled
	{
		get => isToggled;
		set => SetProperty(ref isToggled, value);
	}
}
