namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class EqualsConverterViewModel : BaseViewModel
{
	private string? _inputValue;
	
	public string? InputValue
	{
		get => _inputValue;
		set => SetProperty(ref _inputValue, value);
	}
}
