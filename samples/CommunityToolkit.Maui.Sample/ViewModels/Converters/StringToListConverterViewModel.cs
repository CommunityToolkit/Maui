namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class StringToListConverterViewModel : BaseViewModel
{
	string labelText = "Item 1,Item 2,Item 3";

	public string LabelText
	{
		get => labelText;
		set => SetProperty(ref labelText, value);
	}
}
