namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class MultiConverterViewModel : BaseViewModel
{
	string enteredName = "Steven";

	public string EnteredName
	{
		get => enteredName;
		set => SetProperty(ref enteredName, value);
	}
}