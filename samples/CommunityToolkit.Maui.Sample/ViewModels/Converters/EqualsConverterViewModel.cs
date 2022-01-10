namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class EqualsConverterViewModel : BaseViewModel
{
	string _inputValue = string.Empty;

	public string InputValue
	{
		get => _inputValue;
		set => SetProperty(ref _inputValue, value);
	}
}