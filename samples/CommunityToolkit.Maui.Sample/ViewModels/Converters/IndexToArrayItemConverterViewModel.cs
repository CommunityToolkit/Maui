namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class IndexToArrayItemConverterViewModel : BaseViewModel
{
	int selectedIndex;

	public int SelectedIndex
	{
		get => selectedIndex;
		set => SetProperty(ref selectedIndex, value);
	}
}