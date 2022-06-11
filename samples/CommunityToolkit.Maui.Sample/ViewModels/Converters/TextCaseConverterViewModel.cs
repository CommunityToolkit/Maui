namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class TextCaseConverterViewModel : BaseViewModel
{
	string input = string.Empty;

	public string Input
	{
		get => input;
		set => SetProperty(ref input, value);
	}
}