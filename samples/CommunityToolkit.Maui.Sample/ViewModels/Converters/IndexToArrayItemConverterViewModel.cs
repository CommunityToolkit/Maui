using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class IndexToArrayItemConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	int selectedIndex;
}