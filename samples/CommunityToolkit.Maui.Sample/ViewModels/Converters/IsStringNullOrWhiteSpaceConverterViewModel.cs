namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class IsStringNullOrWhiteSpaceConverterViewModel : BaseViewModel
{
	string? labelText;

	public string? LabelText
	{
		get => labelText;
		set => SetProperty(ref labelText, value);
	}
}