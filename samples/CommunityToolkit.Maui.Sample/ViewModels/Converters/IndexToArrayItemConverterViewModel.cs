namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class IndexToArrayItemConverterViewModel : BaseViewModel
{
	private int selectedIndex;

	public IndexToArrayItemConverterViewModel()
	{
		selectedIndex = 0;
	}

	public int SelectedIndex
	{
		get => selectedIndex;
		set => SetProperty(ref selectedIndex, value);
	} 
}
