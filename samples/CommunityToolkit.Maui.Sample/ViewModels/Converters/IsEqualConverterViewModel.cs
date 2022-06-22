using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class IsEqualConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	string inputValue = string.Empty;
}