namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class IndexToArrayItemConverterViewModel : BaseViewModel
{
	int _selectedIndex;

	public int SelectedIndex
	{
		get => _selectedIndex;
		set => SetProperty(ref _selectedIndex, value);
	}
}